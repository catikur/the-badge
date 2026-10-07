using System.Globalization;
using TheBadge.Sim.Config;
using TheBadge.Sim.Match;

namespace TheBadge.Sim.Replay
{
    /// <summary>GOLDEN REPLAY SETİ — ME 17.4 + ME 3.3 replay dörtlüsü { engineVersion, config_hash,
    /// seed, komut zaman çizelgesi }. Aynı dörtlü = BİT-EŞİT oynatım. Kurulum TEK KAYNAKTAN
    /// türetilir: üretici (`gen-replays`), sunucu kapısı (Checks) ve istemci sondası (Unity, ADR-004)
    /// AYNI fonksiyonu çağırır — "üretici ile kapı farklı evreni ölçer" hatası yapısal olarak
    /// imkânsızdır, platformlar arasında da.</summary>
    public static class GoldenReplay
    {
        /// <summary>Set sürümü: golden dosyasının "surum" alanı ve kurulumun EngineVersion'ı.</summary>
        public const string Surum = "m17-golden-v1";

        /// <summary>Setteki replay sayısı — ME 17.4: "50 arşiv golden replay".</summary>
        public const int Sayi = 50;

        /// <summary>Replay <paramref name="idx"/>'in kurulumu. Kurulum çeşitliliği tohumdan
        /// TÜRETİLİR: 50 replay hava/zemin/rüzgar/chaos/hakem kombinasyonlarını tarar.</summary>
        public static (MatchConfig cfg, CommandQueue q) Kur(int idx, ulong balanceHash, ulong bandsHash)
        {
            ulong sd = 0x5EED0000UL + (ulong)idx * 7919UL;
            var cfg = new MatchConfig
            {
                Seed = sd,
                EngineVersion = Surum,
                BalanceHash = balanceHash,
                CommandBandsHash = bandsHash,
                Home = TestKadrosu.Kur(300, 7, home: true, offset: (idx % 5) * 6 - 12),
                Away = TestKadrosu.Kur(300, 7, home: false, idEntity: 8, offset: ((idx / 5) % 5) * 6 - 12),
                Weather = (WeatherKind)(idx % 4),
                PitchTier = (byte)(1 + idx % 5),
                WindMS = (idx % 3) * 6.0,
                WindDirX = (idx % 2) == 0 ? 1.0 : 0.0,
                WindDirY = (idx % 2) == 0 ? 0.0 : 1.0,
                Chaos = (ChaosLevel)(idx % 3),
                Referee = new RefereeProfile
                { Strictness = (byte)(35 + idx % 40), AdvantageTendency = 50, Consistency = 60 }
            };
            cfg.ConfigHash = ConfigHash.Compute(cfg, balanceHash, bandsHash);

            // KOMUT ZAMAN ÇİZELGESİ — dörtlünün dördüncü üyesi. Üç komut ailesi de temsil edilir
            // (taktik / motivasyon / değişiklik) ki replay yalnız fizik değil MÜDAHALE yolunu da pinlesin.
            var q = new CommandQueue();
            q.Enqueue(new TacticChangeCmd((uint)(3000 + idx * 37), (byte)(idx % 2),
                      new TacticDelta((sbyte)((idx % 3) - 1), 0, 0, (sbyte)((idx % 3) - 1))));
            q.Enqueue(new MotivationCmd((uint)(27000 + idx * 11), (byte)((idx + 1) % 2), (ToneType)(idx % 3)));
            // SubstitutionCmd sözleşmesi (CanExecuteSub): OutId = SAHA SLOTU (0-10 ev / 11-21 deplasman),
            // InId = KULÜBE İNDEKSİ (0..Bench.Length-1) — PlayerId DEĞİL (inceleme bulgusu, Codex).
            int subLo = (idx % 2) * 11;
            q.Enqueue(new SubstitutionCmd((uint)(36000 + idx * 53), (byte)(idx % 2),
                      (short)(subLo + 5 + idx % 5), (short)(idx % 5)));
            return (cfg, q);
        }

        /// <summary>Replay <paramref name="idx"/>'i oynatır; bit-eşitliğin denetlendiği alanları döndürür.</summary>
        public static ReplayCiktisi Oynat(int idx, ulong balanceHash, ulong bandsHash, SimBalance bal)
        {
            var (cfg, q) = Kur(idx, balanceHash, bandsHash);
            var e = new MatchEngine(cfg.Seed, q, cfg, bal) { AutoManage = true };
            var st = MatchEngine.CreateInitialState(cfg);
            var r = e.Run(ref st);
            return new ReplayCiktisi(cfg.ConfigHash, MatchEngine.StateHash(in st), r.HomeGoals, r.AwayGoals,
                                     r.TotalTicks, q.AppliedTraceHash, q.AppliedCount, (uint)e.RejectedCommands,
                                     (uint)e.SubsMade);
        }

        /// <summary>REPLAY KAYDI — bit-eşitlikte denetlenen 8 alanın KANONİK yazımı. Golden kaydı,
        /// ölçülen çıktı, platform borcu ve istemci sondası AYNI yazımdan geçer; eşitlik = bu
        /// yazımın eşitliği. Kültürden bağımsızdır (Türkçe yerelde de aynı metin).</summary>
        public static string Kayit(ulong cfg, ulong st, string skor, uint tick, ulong iz, uint uyg, uint red, uint sub)
        {
            var c = CultureInfo.InvariantCulture;
            return "configHash 0x" + cfg.ToString("X16", c) + " · stateHash 0x" + st.ToString("X16", c) +
                   " · skor " + skor + " · tick " + tick.ToString(c) + " · komutIz 0x" + iz.ToString("X16", c) +
                   " · uygulanan " + uyg.ToString(c) + " · reddedilen " + red.ToString(c) +
                   " · degisiklik " + sub.ToString(c);
        }

        /// <summary>Skor alanının kanonik yazımı: "ev-deplasman".</summary>
        public static string Skor(int ev, int dep)
            => ev.ToString(CultureInfo.InvariantCulture) + "-" + dep.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Bir replay oynatımının kimlik alanları (ME 3.3 dörtlüsünün çıktı tarafı).</summary>
    public readonly struct ReplayCiktisi
    {
        public readonly ulong ConfigHash, StateHash, KomutIz;
        public readonly int Ev, Dep;
        public readonly uint Tick, Uygulanan, Reddedilen, Degisiklik;

        public ReplayCiktisi(ulong configHash, ulong stateHash, int ev, int dep, uint tick, ulong komutIz,
                             uint uygulanan, uint reddedilen, uint degisiklik)
        {
            ConfigHash = configHash; StateHash = stateHash; Ev = ev; Dep = dep; Tick = tick;
            KomutIz = komutIz; Uygulanan = uygulanan; Reddedilen = reddedilen; Degisiklik = degisiklik;
        }

        /// <summary>Kanonik kayıt (<see cref="GoldenReplay.Kayit"/>).</summary>
        public string Kayit() => GoldenReplay.Kayit(ConfigHash, StateHash, GoldenReplay.Skor(Ev, Dep), Tick,
                                                    KomutIz, Uygulanan, Reddedilen, Degisiklik);
    }
}
