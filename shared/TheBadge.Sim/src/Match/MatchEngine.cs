using System;
using TheBadge.Sim.Core;

namespace TheBadge.Sim.Match
{
    /// <summary>
    /// FAZ 03 M0 — motor İSKELETİ: ME Spec 4.2 tick pipeline'ı SABİT aşama sırasıyla kurulu.
    /// Algı/karar/aksiyon aşamaları bilerek boştur (M1-M3 dilimleri doldurur); fizik yalnız
    /// tamsayı disiplin iskeletidir (top serbest ilerleme); checksum kadansı gerçektir (ME 3.2).
    /// Δt = 100 ms (LOD 0, ME 3.4) — mimari sabittir, balance değeri DEĞİLDİR.
    /// Determinizm sözleşmesi: aynı seed + aynı başlangıç durumu + aynı komut zaman çizelgesi
    /// = bit düzeyinde aynı durum hash'i (Checks/MatchSkeleton* kapıları).
    /// </summary>
    public sealed class MatchEngine
    {
        public const int TickMs = 100;                    // ME Spec 3.4 (LOD 0)
        public const int TicksPerSecond = 1000 / TickMs;
        public const uint ChecksumCadenceTicks = 600;     // 60 sn'de bir xxHash64 — ME Spec 3.2

        readonly ulong seed;
        readonly CommandQueue queue;

        /// <summary>Rng domain akışlarının kökü — M1+ aşamaları tüketir (ME 3.1); replay dörtlüsü üyesi (ME 3.3).</summary>
        public ulong Seed => seed;

        public MatchEngine(ulong seed, CommandQueue queue)
        {
            this.seed = seed;
            this.queue = queue ?? throw new ArgumentNullException(nameof(queue));
        }

        /// <summary>Başlangıç durumu: 22 ajan slotu (0-10 ev, 11-21 deplasman), top santrada serbest.
        /// Diziliş/kadro yükleme M1 (TeamSheet — ME 5.2/6.1) diliminde gelir.</summary>
        public static MatchState CreateInitialState()
        {
            var s = new MatchState
            {
                Tick = 0,
                Phase = MatchPhase.Kickoff,
                Agents = new PlayerAgentState[22] // maç başında TEK tahsis — sıcak yol zero-alloc (ME 16.2)
            };
            s.Ball.OwnerId = -1;
            for (short i = 0; i < 22; i++)
            {
                s.Agents[i] = new PlayerAgentState
                {
                    Id = i,
                    TeamIdx = (byte)(i < 11 ? 0 : 1),
                    Energy = 1000,           // ME 12.1 tavanı
                    Injury = InjuryState.None
                };
            }
            return s;
        }

        /// <summary>Bir tick — aşama sırası SABİT (ME Spec 4.2); sıralı güncelleme kuralı (ME 3.2).</summary>
        public void Tick(ref MatchState st)
        {
            queue.ApplyDue(st.Tick, ref st);   // 1) müdahaleler (Bölüm 14)
            PerceptionPass(ref st);            // 2) uzamsal grid — M1 doldurur (ME 4.2)
            DecisionPass(ref st);              // 3) kademeli karar (agentId mod 5) — M2 (ME 4.2/7)
            ActionResolutionPass(ref st);      // 4) düello/pas/şut — M2/M3 (ME 6-10)
            PhysicsPass(ref st);               // 5) hareket entegrasyonu — M0'da minimal top
            EventAndStatePass(ref st);         // 6) event log/durum/checksum/faz — M0'da checksum+tick
        }

        // Bilerek boş: M1 uzamsal grid (12×8 hücre) ve algı bütçesini kurar — ME 4.2.
        void PerceptionPass(ref MatchState st) { }

        // Bilerek boş: M2 kademeli Utility kararını kurar (yalnız sırası gelen ajanlar) — ME 4.2/7.
        void DecisionPass(ref MatchState st) { }

        // Bilerek boş: M2/M3 düello çözücüleri (IActionResolver) — ME 6-10.
        void ActionResolutionPass(ref MatchState st) { }

        /// <summary>M0 fiziği: top serbest uçuşta sabit hızla ilerler. Ara hesap double,
        /// kalıcı durum int + QuantizeMm (ME 3.2). Sürtünme/sekme/yerçekimi/spin M2 (ME 8).</summary>
        void PhysicsPass(ref MatchState st)
        {
            const double dt = TickMs / 1000.0;
            st.Ball.X = Units.QuantizeMm(Units.ToMeters(st.Ball.X) + Units.ToMeters(st.Ball.Vx) * dt);
            st.Ball.Y = Units.QuantizeMm(Units.ToMeters(st.Ball.Y) + Units.ToMeters(st.Ball.Vy) * dt);
            st.Ball.Z = Units.QuantizeMm(Units.ToMeters(st.Ball.Z) + Units.ToMeters(st.Ball.Vz) * dt);
        }

        /// <summary>M0: tick ilerletme + checksum kadansı. Event log (ME 15) ve durum modeli
        /// (stamina/moral — ME 12) kendi dilimlerinde bu aşamaya eklenir.</summary>
        void EventAndStatePass(ref MatchState st)
        {
            st.Tick++;
            if (st.Tick % ChecksumCadenceTicks == 0)
                st.LastChecksum = StateHash(in st);
        }

        /// <summary>Kanonik durum hash'i — xxHash64 (ME Spec 3.2). Alan sırası SABİT sözleşmedir;
        /// alan ekleyen her dilim burayı ve Checks golden değerini birlikte günceller.
        /// LastChecksum hash'e DAHİL DEĞİLDİR (kendi kendine referans olmaz).</summary>
        public static ulong StateHash(in MatchState st)
        {
            // Boyut: başlık 13 + top 30 + 22×39 ajan + 2×6 takım = 913 bayt
            Span<byte> buf = stackalloc byte[1024];
            int o = 0;
            W32(buf, ref o, st.Tick);
            buf[o++] = (byte)st.Phase;
            W32(buf, ref o, (uint)st.HomeGoals);
            W32(buf, ref o, (uint)st.AwayGoals);

            W32(buf, ref o, (uint)st.Ball.X); W32(buf, ref o, (uint)st.Ball.Y); W32(buf, ref o, (uint)st.Ball.Z);
            W32(buf, ref o, (uint)st.Ball.Vx); W32(buf, ref o, (uint)st.Ball.Vy); W32(buf, ref o, (uint)st.Ball.Vz);
            W32(buf, ref o, (uint)st.Ball.SpinY);
            W16(buf, ref o, (ushort)st.Ball.OwnerId);

            for (int i = 0; i < st.Agents.Length; i++)
            {
                ref readonly var a = ref st.Agents[i];
                W16(buf, ref o, (ushort)a.Id);
                buf[o++] = a.TeamIdx;
                buf[o++] = a.RoleId;
                W32(buf, ref o, (uint)a.X); W32(buf, ref o, (uint)a.Y);
                W32(buf, ref o, (uint)a.Vx); W32(buf, ref o, (uint)a.Vy);
                W32(buf, ref o, (uint)a.AnchorX); W32(buf, ref o, (uint)a.AnchorY);
                W16(buf, ref o, a.Energy);
                buf[o++] = (byte)a.Momentum;
                buf[o++] = a.YellowCards;
                buf[o++] = a.SentOff ? (byte)1 : (byte)0;
                buf[o++] = (byte)a.Injury;
                buf[o++] = a.CurrentAction;
                W32(buf, ref o, a.ActionUntilTick);
            }

            W32(buf, ref o, (uint)st.HomeRt.LineHeightMm);
            buf[o++] = st.HomeRt.PressMode;
            buf[o++] = (byte)st.HomeRt.Momentum;
            W32(buf, ref o, (uint)st.AwayRt.LineHeightMm);
            buf[o++] = st.AwayRt.PressMode;
            buf[o++] = (byte)st.AwayRt.Momentum;

            return XxHash64.Hash(buf.Slice(0, o));
        }

        static void W16(Span<byte> b, ref int o, ushort v)
        { b[o++] = (byte)v; b[o++] = (byte)(v >> 8); }

        static void W32(Span<byte> b, ref int o, uint v)
        { b[o++] = (byte)v; b[o++] = (byte)(v >> 8); b[o++] = (byte)(v >> 16); b[o++] = (byte)(v >> 24); }
    }
}
