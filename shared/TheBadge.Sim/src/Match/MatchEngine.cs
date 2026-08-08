using System;
using TheBadge.Sim.Config;
using TheBadge.Sim.Core;
using TheBadge.Sim.Determinism;

namespace TheBadge.Sim.Match
{
    /// <summary>
    /// FAZ 03 motoru — M2: ME Spec 4.2 pipeline'ında gerçek çekirdek: kademeli utility karar
    /// (7.2 indirgenmiş aday seti), topsuz anchor-omurgalı konumlanma (7.4 alt kümesi), sahiplik/
    /// kontrol-tackle düelloları (4.3 + 6.3-6.4), pas nişan hatası (6.5 çekirdeği), ajan kinematiği
    /// ve top fiziği (8.1-8.3). M2 bilinçli sınırları: şut/gol ve kaleci modeli YOK (M3);
    /// taç/aut tek adımlık DEAD_BALL restart'ıdır; algı grid'i ve tam BT M-karar ilerisinde.
    /// Determinizm: yalnız int kalıcı durum + QuantizeMm; sin/cos TrigLut; ajan sırası sabit;
    /// tüm zarlar Rng domain akışlarından (gerekçeler çağrı yerlerinde).
    /// </summary>
    public sealed class MatchEngine
    {
        public const int TickMs = 100;                    // ME Spec 3.4 (LOD 0)
        public const int TicksPerSecond = 1000 / TickMs;
        public const uint ChecksumCadenceTicks = 600;     // 60 sn'de bir xxHash64 — ME Spec 3.2
        public const uint HalfTicks = 45 * 60 * 10;       // 45 dk × 60 sn × 10 Hz = 27.000 tick
        public const int PitchHalfXmm = 52500;            // 105×68 m saha, merkez orijin (mimari sabit)
        public const int PitchHalfYmm = 34000;            // ev sahibi +x yönüne hücum eder
        const double Dt = TickMs / 1000.0;
        const double G = 9.81;                            // yerçekimi (ME 8.3 — fizik sabiti)

        readonly ulong seed;
        readonly CommandQueue queue;
        readonly SimBalance bal;
        readonly AttributeLuts luts;
        readonly PlayerAttributes[] attrs = new PlayerAttributes[22];
        readonly RefereeProfile referee;

        public const int GoalHalfWidthMm = 3660;  // kale 7,32 m — direkler y ±3660 (fiziksel sabit)
        public const int GoalHeightMm = 2440;     // üst direk 2,44 m

        // Tanı sayaçları — event log (ME 15) gelene dek dev ekranı/Checks tüketir.
        // Duruma ve hash'e GİRMEZLER; davranışı etkilemezler.
        public int PassAttempts, PassCompletions, Tackles, OutOfBounds, PossessionChanges;
        public int Shots, Saves;
        public int Fouls, Advantages, Yellows, Reds, Corners, GoalKicks, ThrowIns, Penalties, FreeKicks, Blocks, Offsides;
        public int Injuries, ThroughPasses;
        public double XgHome, XgAway; // xG KAYIT gerçeği (ME 15.2) — sonuç üretimine girmez
        int pendingPassTeam = -1; // pas sonrası ilk kontrol aynı takımsa tamamlanmış sayılır
        // Devre başı taban değerleri (uzatma hesabı, ME 3.4) — koşu boyunca deterministik türer;
        // durum serileştirmesi (replay resume) M-replay diliminde bunları da taşıyacak
        int halfCardsBase, halfGoalsBase; uint halfStoppageBase;
        short pendingOffsidePlayer = -1; // pas anında ofsayt konumundaki alıcı (ME 10.5)
        readonly double[] energyAccum = new double[22]; // kesirli drenaj birikimi (Energy tamsayıdır)
        readonly bool[] sprinting = new bool[22];
        readonly uint[] sprintCooldownUntil = new uint[22];
        double momentumAccumHome, momentumAccumAway; // momentum sönümü birikimi (ME 12.3)

        /// <summary>Rng kökü — replay dörtlüsü üyesi (ME 3.3).</summary>
        public ulong Seed => seed;

        public MatchEngine(ulong seed, CommandQueue queue, MatchConfig cfg, SimBalance balance)
        {
            this.seed = seed;
            this.queue = queue ?? throw new ArgumentNullException(nameof(queue));
            bal = balance ?? throw new ArgumentNullException(nameof(balance));
            luts = AttributeLuts.Build(balance);
            referee = cfg != null ? cfg.Referee : RefereeProfile.Default;
            if (cfg != null)
                for (int i = 0; i < 11; i++)
                {
                    attrs[i] = cfg.Home.Starters[i].Attributes;
                    attrs[11 + i] = cfg.Away.Starters[i].Attributes;
                }
        }

        public static MatchState CreateInitialState()
        {
            var s = new MatchState
            {
                Tick = 0,
                Phase = MatchPhase.Kickoff,
                Half = 1,
                SetPiece = SetPieceType.Kickoff,
                SetPieceTaker = -1,
                Agents = new PlayerAgentState[22] // tek tahsis — sıcak yol zero-alloc (ME 16.2)
            };
            s.Ball.OwnerId = -1;
            s.Ball.LastTouchTeam = 2;
            for (short i = 0; i < 22; i++)
            {
                s.Agents[i] = new PlayerAgentState
                {
                    Id = i,
                    TeamIdx = (byte)(i < 11 ? 0 : 1),
                    Energy = 1000,
                    Injury = InjuryState.None
                };
            }
            return s;
        }

        /// <summary>Kadrodan kurulum (ME 5.2): ev Starters[i]→Agents[i], deplasman→Agents[11+i];
        /// santra sadeleştirmesi: ev forveti (slot 10) topun başına alınır (tam santra sahnesi M-durum).</summary>
        public static MatchState CreateInitialState(MatchConfig cfg)
        {
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));
            cfg.Home.Validate("Home");
            cfg.Away.Validate("Away");
            for (int i = 0; i < 11; i++)
                for (int j = 0; j < 11; j++)
                    if (cfg.Home.Starters[i].PlayerId == cfg.Away.Starters[j].PlayerId)
                        throw new ArgumentException(
                            $"PlayerId {cfg.Home.Starters[i].PlayerId} iki takımda birden.");

            var s = CreateInitialState();
            for (int i = 0; i < 11; i++)
            {
                ApplyEntry(ref s.Agents[i], cfg.Home.Starters[i]);
                ApplyEntry(ref s.Agents[11 + i], cfg.Away.Starters[i]);
            }
            // Santra: ev sahibi başlar; forvet topun başında (ölü top kilidi Flight=3, ME 10)
            s.Agents[10].X = -600; s.Agents[10].Y = 0;
            s.Agents[10].TargetX = -600; s.Agents[10].TargetY = 0;
            s.SetPiece = SetPieceType.Kickoff;
            s.SetPieceTeam = 0;
            s.SetPieceTaker = 10;
            s.Ball.Flight = 3;
            s.Ball.LastTouchTeam = 0;
            return s;
        }

        /// <summary>Maç bitti mi — FullTime fazı (ME 4.1).</summary>
        public static bool IsFinished(in MatchState st) => st.Phase == MatchPhase.FullTime;

        /// <summary>Headless tam maç koşusu — ME Spec 5.1 IMatchEngine.Run karşılığı.
        /// Maç FullTime'da kendi kendine biter; emniyet tavanı sonsuz döngüyü keser.</summary>
        public MatchResult Run(ref MatchState st, uint maxTicks = 80000)
        {
            while (!IsFinished(in st) && st.Tick < maxTicks) Tick(ref st);
            return new MatchResult
            {
                HomeGoals = st.HomeGoals, AwayGoals = st.AwayGoals,
                TotalTicks = st.Tick, StoppageTicks = st.StoppageTicks,
                Shots = Shots, Saves = Saves, Fouls = Fouls, Yellows = Yellows, Reds = Reds,
                Corners = Corners, Penalties = Penalties,
                XgHome = XgHome, XgAway = XgAway,
                FinalChecksum = StateHash(in st)
            };
        }

        static void ApplyEntry(ref PlayerAgentState a, PlayerEntry e)
        {
            a.RoleId = e.RoleId;
            a.AnchorX = e.AnchorXmm; a.AnchorY = e.AnchorYmm;
            a.X = e.AnchorXmm; a.Y = e.AnchorYmm;
            a.TargetX = e.AnchorXmm; a.TargetY = e.AnchorYmm;
        }

        /// <summary>Bir tick — aşama sırası SABİT (ME Spec 4.2).</summary>
        public void Tick(ref MatchState st)
        {
            if (st.Phase == MatchPhase.FullTime) return; // maç bitti — durum donar
            RefreshStateCache(ref st);         // 0) A_eff bağlam önbelleği (enerji/momentum/sakatlık)
            queue.ApplyDue(st.Tick, ref st);   // 1) müdahaleler (Bölüm 14)
            PerceptionPass(ref st);            // 2) uzamsal grid — M-karar ilerisi doldurur
            DecisionPass(ref st);              // 3) kademeli karar (agentId mod 5) — ME 4.2
            ActionResolutionPass(ref st);      // 4) kontrol/tackle düelloları + yapıştırma
            PhysicsPass(ref st);               // 5) kinematik + top fiziği (ME 8)
            EventAndStatePass(ref st);         // 6) sınır/faz/checksum
        }

        void PerceptionPass(ref MatchState st) { } // 12×8 grid + algı bütçesi M-karar ilerisinde

        // ---------------------------------------------------------------- karar (ME 7.2/7.4 çekirdek)

        void DecisionPass(ref MatchState st)
        {
            for (int i = 0; i < 22; i++) // sıra sabit (ME 3.2)
            {
                if ((uint)(i % 5) != st.Tick % 5) continue; // kademeli karar — ME 4.2
                ref var a = ref st.Agents[i];
                if (!a.Active) continue;
                if (st.Ball.OwnerId == a.Id) OnBallDecision(ref st, i);
                else OffBallTarget(ref st, i);
            }
        }

        void OnBallDecision(ref MatchState st, int i)
        {
            ref var a = ref st.Agents[i];
            if (st.Tick < a.ActionUntilTick) return; // karar kilidi (ME 7.2 — titreme önlenir)

            var u = bal.utility;
            // Vision aday genişliği (ME 7.2): düşük vizyon en iyi seçeneği hiç GÖREMEYEBİLİR
            int maxCand = u.adayTabani + Eff(i, attrs[i].Vision) / u.adayVisionBolen;

            // Pas adayları: takım arkadaşları mesafe sıralı (sabit seçim sıralaması — sırasız yapı yok)
            Span<int> candIdx = stackalloc int[10];
            Span<long> candD2 = stackalloc long[10];
            int candN = 0;
            int t0 = a.TeamIdx == 0 ? 0 : 11;
            for (int j = t0; j < t0 + 11; j++)
            {
                if (j == i || !st.Agents[j].Active) continue;
                long dx = st.Agents[j].X - a.X, dy = st.Agents[j].Y - a.Y;
                long d2 = dx * dx + dy * dy;
                int k = candN < 10 ? candN++ : -1;
                if (k < 0) // en uzak olanla değiş (10 slot yeter: 10 takım arkadaşı)
                    for (int m = 0; m < candN; m++) if (candD2[m] > d2) { k = m; break; }
                if (k >= 0) { candIdx[k] = j; candD2[k] = d2; }
            }
            // mesafeye göre araya-sokmalı sıralama (küçük N, tahsissiz, deterministik)
            for (int m = 1; m < candN; m++)
                for (int n = m; n > 0 && candD2[n] < candD2[n - 1]; n--)
                {
                    (candD2[n], candD2[n - 1]) = (candD2[n - 1], candD2[n]);
                    (candIdx[n], candIdx[n - 1]) = (candIdx[n - 1], candIdx[n]);
                }
            if (candN > maxCand) candN = maxCand;

            double curXt = XtAt(a.X, a.Y, a.TeamIdx);
            double best = double.MinValue;
            int bestKind = 0, bestTarget = -1; // 0 tut, 1 dribble, 2 pas
            uint tick = st.Tick;
            int agentId = i;

            // Zar gerekçesi: karar gürültüsü DECISION domain'i — "yanlış tercih" chaos'u (ME 7.2/13.2).
            // Sigma ölçekleri: yorgun beyin hatası (Energy<250 → +%20, ME 12.1) ve takım momentumu
            // (±%15, ME 12.3 → 7.6 momentum yayılımı).
            double sigmaMul = (a.Energy < bal.stamina.yorgunlukEsik ? 1.2 : 1.0)
                              * (1.0 - momentumCache[agentId] / 10.0 * bal.momentum.decisionSigmaEtkiYuzde / 100.0);
            double Noise(uint salt) =>
                bal.chaos.decisionSigma.orta * sigmaMul
                * Rng.Gauss01(seed, Domain.Decision, (uint)(100 + agentId), tick, salt);

            // Tut (HoldShield) — P_kayıp düşük ama tehdit üretmez
            {
                double s0 = u.wRisk * (1.0 - 0.15) + u.wVar * Noise(1);
                if (s0 > best) { best = s0; bestKind = 0; bestTarget = -1; }
            }
            // Dribble: rakip kalesine doğru ilerleme
            {
                int dir = a.TeamIdx == 0 ? 1 : -1;
                int nxMm = ClampX(a.X + dir * (int)(u.dribbleIleriM * 1000));
                double dXt = XtAt(nxMm, a.Y, a.TeamIdx) - curXt;
                double pLoss = u.kayipTaban + NearOpponents(ref st, a.X, a.Y, a.TeamIdx) * 0.12;
                double s1 = u.wThreat * dXt + u.wRisk * (1.0 - Math.Min(0.9, pLoss)) + u.wVar * Noise(2);
                if (s1 > best) { best = s1; bestKind = 1; bestTarget = -1; }
            }
            // Kısa paslar
            for (int c = 0; c < candN; c++)
            {
                int j = candIdx[c];
                double dM = Math.Sqrt((double)candD2[c]) / 1000.0;
                double dXt = XtAt(st.Agents[j].X, st.Agents[j].Y, a.TeamIdx) - curXt;
                double pLoss = u.kayipTaban + u.kayipMesafePerM * dM
                               + u.kayipKoridorRakip * CorridorOpponents(ref st, a.X, a.Y, st.Agents[j].X, st.Agents[j].Y, a.TeamIdx);
                double sc = u.wThreat * dXt + u.wRisk * (1.0 - Math.Min(0.95, pLoss)) + u.wVar * Noise((uint)(3 + c));
                if (sc > best) { best = sc; bestKind = 2; bestTarget = j; }
            }

            // Şut adayı (M3): kale menzilindeyse — tehdit VEKİLİ rasyoneldir (ln/atan karar
            // yolunda YASAK determinizm gereği); kayıt xG'si (15.2) ExecuteShot'ta ayrı hesaplanır
            {
                int gx = a.TeamIdx == 0 ? PitchHalfXmm : -PitchHalfXmm;
                double dxG = (gx - a.X) / 1000.0, dyG = (0 - a.Y) / 1000.0;
                double dGoal = Math.Sqrt(dxG * dxG + dyG * dyG);
                if (dGoal <= bal.shotExec.sutMaxMesafeM)
                {
                    double closeness = 1.0 - dGoal / bal.shotExec.sutMaxMesafeM;
                    double central = 1.0 - Math.Min(1.0, Math.Abs((double)a.Y) / PitchHalfYmm);
                    double fin = Composite(i, 0.55, attrs[i].Finishing, 0.25, attrs[i].Composure, 0.2, attrs[i].FirstTouch) / 100.0;
                    // Baskı: kalabalık ceza sahasında şut iştahı düşer (ME 15.2 pres katsayısının
                    // karar tarafındaki karşılığı) — bu terim olmadan motor kalabalığa doğru şut yağdırıyordu
                    int presN = Math.Min(2, NearOpponents(ref st, a.X, a.Y, a.TeamIdx, 1500));
                    // Mesafe üssü [KALİBRE]: 2.0 iken yalnız 6-8 m'den şut çıkıyor (dönüşüm gerçek
                    // dışı yüksek); düşük üs uzaktan şutu da aday yapar → mesafe dağılımı gerçekçileşir
                    double proxy = Math.Pow(closeness, u.sutMesafeUs) * (0.4 + 0.6 * central)
                                   * (0.5 + 0.5 * fin) * (1.0 - u.sutBaskiCezasi * presN);
                    double s3 = u.wThreat * u.sutTehditCarpan * proxy
                                + u.wRisk * proxy + u.wVar * Noise(14);
                    if (s3 > best) { best = s3; bestKind = 3; bestTarget = -1; }
                }
            }

            // ARA PAS (ThroughPass) — ME 7.2 aday seti: en ileri takım arkadaşının ÖNÜNDEKİ boşluğa.
            // Koşu ofsayt kısıtıyla serbest bırakılır (7.4) → ofsayt üretiminin gerçek kaynağı (10.5).
            {
                int runner = -1; int bestAdv = int.MinValue;
                int dirF = a.TeamIdx == 0 ? 1 : -1;
                for (int c = 0; c < candN; c++)
                {
                    int j = candIdx[c];
                    if (st.Agents[j].RoleId < 3) continue;             // orta saha/forvet koşar
                    int adv = st.Agents[j].X * dirF;
                    if (adv > bestAdv) { bestAdv = adv; runner = j; }
                }
                if (runner >= 0 && bestAdv * dirF >= 0)
                {
                    int tgtX = ClampX(st.Agents[runner].X + dirF * (int)(u.araPasIleriM * 1000));
                    double dXt = XtAt(tgtX, st.Agents[runner].Y, a.TeamIdx) - curXt;
                    double pLoss = u.kayipTaban + u.araPasRisk
                                   + u.kayipKoridorRakip * CorridorOpponents(ref st, a.X, a.Y, tgtX, st.Agents[runner].Y, a.TeamIdx);
                    double s4 = u.wThreat * dXt + u.wRisk * (1.0 - Math.Min(0.95, pLoss)) + u.wVar * Noise(13);
                    if (s4 > best) { best = s4; bestKind = 4; bestTarget = runner; }
                }
            }

            if (bestKind == 4)
            {
                // Boşluğa pas + koşu: alıcı ileri fırlar (ofsayt riski gerçek)
                int dirF = a.TeamIdx == 0 ? 1 : -1;
                ExecutePass(ref st, i, bestTarget, aheadMm: (int)(u.araPasIleriM * 1000) * dirF);
                ThroughPasses++;
                // Koşu zamanlaması hatası → ofsayt (ME 10.5: "koşu zamanlama hatası = Positioning
                // düellosu"). Positioning yüksek oyuncu daha az erken çıkar. DECISION domain.
                double pMistime = u.araPasKotuZamanlama * (1.0 - Eff(bestTarget, attrs[bestTarget].Positioning) / 200.0);
                if (Rng.Rand01(seed, Domain.Decision, (uint)(100 + bestTarget), st.Tick, 15) < pMistime)
                {
                    // Erken çıkış: yan hakem bayrağı pas ANINDA kalkar → oyun hemen durur.
                    // (Alıcının topa dokunmasını beklemek, koşucu topa varamadığında ihlali
                    // yutuyordu; gözlenen sonuç aynı: savunmaya frikik.)
                    Offsides++;
                    byte defT = a.TeamIdx == 0 ? (byte)1 : (byte)0;
                    pendingOffsidePlayer = -1;
                    AwardSetPiece(ref st, SetPieceType.FreeKick, defT,
                                  st.Agents[bestTarget].X, st.Agents[bestTarget].Y);
                    return;
                }
                st.Agents[bestTarget].TargetX = ClampX(st.Agents[bestTarget].X + dirF * (int)(u.araPasIleriM * 1000));
            }
            else if (bestKind == 3) ExecuteShot(ref st, i);
            else if (bestKind == 2) ExecutePass(ref st, i, bestTarget);
            else if (bestKind == 1)
            {
                // DRİBLİNG DÜELLOSU (ME 6.4): önünde savunucu varsa adam geçmek bedelsiz değildir.
                // Bu düello olmadan taşıyıcı ceza sahasına kadar yürüyor, tüm şutlar 6-8 m'den
                // çıkıyor ve dönüşüm oranı gerçek dışı yükseliyordu (M4 kalibrasyon bulgusu).
                int marker = NearestOpponentInFront(ref st, i, (int)(bal.possession.driblinYaricapM * 1000));
                if (marker >= 0)
                {
                    double dAtk = Composite(i, 0.5, attrs[i].Dribbling, 0.3, attrs[i].Agility, 0.2, attrs[i].Pace);
                    double dDef = Composite(marker, 0.5, attrs[marker].Tackling, 0.3, attrs[marker].Positioning, 0.2, attrs[marker].Strength);
                    if (!DuelWin(dAtk, dDef, (uint)(900 + i), st.Tick, 34, bal.duel.pTabanDriblin))
                    {
                        // Adam geçilemedi: top savunucuya döner (temiz müdahale)
                        Tackles++;
                        st.Ball.OwnerId = -1;
                        st.Ball.LastTouchTeam = st.Agents[marker].TeamIdx;
                        pendingPassTeam = -1;
                        st.Ball.Vx = 0; st.Ball.Vy = 0;
                        st.Ball.X = st.Agents[marker].X; st.Ball.Y = st.Agents[marker].Y;
                        a.ActionUntilTick = st.Tick + (uint)bal.possession.tackleCooldownTicks;
                        return;
                    }
                }
                int dir = a.TeamIdx == 0 ? 1 : -1;
                a.TargetX = ClampX(a.X + dir * (int)(u.dribbleIleriM * 1000));
                a.TargetY = a.Y;
            }
            else { a.TargetX = a.X; a.TargetY = a.Y; }
            a.ActionUntilTick = st.Tick + (uint)u.kararKilidiTicks;
        }

        /// <summary>Pas — ME 6.5 çekirdeği: menzil ters formülüyle güç (8.2), nişan hatası sigma;
        /// kesişim EMERGENT'tır (koridor rakipleri fiziksel olarak topa ulaşabilir).</summary>
        void ExecutePass(ref MatchState st, int i, int j, int aheadMm = 0)
        {
            ref var a = ref st.Agents[i];
            double dxM = (st.Agents[j].X + aheadMm - a.X) / 1000.0, dyM = (st.Agents[j].Y - a.Y) / 1000.0;
            double dM = Math.Sqrt(dxM * dxM + dyM * dyM);
            if (dM < 0.5) return;

            var p = bal.pass;
            double v0 = Math.Sqrt(2.0 * bal.physics.aRollKuru * dM); // d = v²/2a tersi (ME 8.2)
            if (v0 < p.groundSpeedMin) v0 = p.groundSpeedMin;
            if (v0 > p.groundSpeedMax) v0 = p.groundSpeedMax;

            int passingEff = Eff(i, attrs[i].Passing);
            double sigmaDeg = p.sigma0Deg * (1.0 - passingEff / 125.0)
                              * (1.0 + dM * p.distFactorPerM)
                              * (1.0 + p.presFactorPerRakip * NearOpponents(ref st, a.X, a.Y, a.TeamIdx));
            // Zar gerekçesi: nişan sapması fiziksel yürütme hatasıdır — PHYSICS domain (ME 3.1/6.5)
            double errRad = sigmaDeg * Math.PI / 180.0
                            * Rng.Gauss01(seed, Domain.Physics, (uint)(200 + i), st.Tick, 21);
            TrigLut.Rotate(dxM / dM, dyM / dM, TrigLut.AngleIndexFromRad(errRad), out double ux, out double uy);

            st.Ball.Vx = Units.QuantizeMm(v0 * ux);
            st.Ball.Vy = Units.QuantizeMm(v0 * uy);
            st.Ball.Vz = 0; // yerden pas; havadan top/orta M3+ (ME 8.3)
            st.Ball.OwnerId = -1;
            st.Ball.LastTouchTeam = a.TeamIdx;
            PassAttempts++;
            pendingPassTeam = a.TeamIdx;
            // Ofsayt üretimi — ME 10.5: pas ANINDA alıcının konumu son savunucu çizgisiyle
            // karşılaştırılır; ihlal alıcı topa dokununca düdükle biter (VAR marjı 11.4'te)
            int oline = OffsideLineX(ref st, a.TeamIdx);
            bool beyond = a.TeamIdx == 0 ? st.Agents[j].X > oline : st.Agents[j].X < oline;
            pendingOffsidePlayer = beyond ? (short)j : (short)-1;
            // Alıcı topu karşılamaya koşar
            st.Agents[j].TargetX = ClampX(st.Ball.X + (int)(dxM * 400));
            st.Agents[j].TargetY = ClampY(st.Ball.Y + (int)(dyM * 400));
        }

        /// <summary>Şut — ME 6.4/8.3 + kurtarış ANALİTİK ön-çözümü (9.2): sonuç topun gerçek
        /// uçuşuyla sahnelenir (gol → çizgiyi geçer; tut → kaleciye uçar; çeldi → dışa sapar).
        /// Kayıt xG'si 15.2 formülüyle AYNEN hesaplanır (ln/atan yalnız kayıtta — sonuca girmez).</summary>
        void ExecuteShot(ref MatchState st, int i, bool header = false)
        {
            ref var a = ref st.Agents[i];
            int gx = a.TeamIdx == 0 ? PitchHalfXmm : -PitchHalfXmm;
            double dxM = (gx - a.X) / 1000.0, dyM = (0 - a.Y) / 1000.0;
            double dGoal = Math.Sqrt(dxM * dxM + dyM * dyM);
            if (dGoal < 1.0) return;

            // Nişan: kale düzleminde hedef y — Finishing kompoziti sigma'yı daraltır;
            // kafa vuruşunda Heading kompoziti geçerlidir (ME 6.4 aksiyon eşlemesi)
            double fin = header
                ? Composite(i, 0.55, attrs[i].Heading, 0.25, attrs[i].Composure, 0.2, attrs[i].JumpReach)
                : Composite(i, 0.55, attrs[i].Finishing, 0.25, attrs[i].Composure, 0.2, attrs[i].FirstTouch);
            // Nişan hatası AÇISALDIR (ME 6.5 pas modeliyle aynı fizik): kale düzlemindeki sapma
            // mesafeyle büyür — mutlak metre sapma uzaktan şutu gerçek dışı isabetli yapıyordu.
            // Kritik dakika baskısı — ME 12.3: dk>80 ve fark ≤1 iken nişan sapmasına eklenir;
            // Composure yüksek oyuncu etkinin %60'ını söndürür ("büyük maç oyuncusu")
            double baski = CriticalPressure(in st, i);
            double sigmaRad = bal.shotExec.sutSigmaTabanDeg * (1.0 + baski) * Math.PI / 180.0 * (1.0 - fin / 125.0);
            double sigmaPlaneM = dGoal * Math.Tan(sigmaRad);
            // Nişan noktası: kaleciyi geçmek için direk dibi (merkez değil) — taraf DECISION akışından
            double side = Rng.Rand01(seed, Domain.Decision, (uint)(200 + i), st.Tick, 44) < 0.5 ? -1.0 : 1.0;
            double aimTarget = side * GoalHalfWidthMm * bal.shotExec.nisanDirekOrani;
            // Zar gerekçesi: şut yürütme hatası fizikseldir — PHYSICS domain (ME 3.1)
            double aimY = aimTarget + sigmaPlaneM * 1000.0
                          * Rng.Gauss01(seed, Domain.Physics, (uint)(200 + i), st.Tick, 41);

            double planeDx = (gx - a.X) / 1000.0;
            double tPlane = Math.Abs(planeDx) / bal.shotExec.sutHiziMS;
            double interY = aimY; // kale düzleminde kesişim

            // Blok: şut koridorunda savunucu varsa top ona çarpar (ME 15.1 ShotBlocked) — serbest top
            int blocker = NearestCorridorOpponent(ref st, a.X, a.Y, gx, Units.QuantizeMm(interY / 1000.0), a.TeamIdx);
            if (blocker >= 0 &&
                Rng.Rand01(seed, Domain.Duel, (uint)(200 + i), st.Tick, 45) < bal.shotExec.blokOlasilik)
            {
                Shots++;
                RecordXg(ref st, i, dGoal, header);
                Blocks++;
                double bdx = (gx - a.X) / 1000.0, bdy = (interY - a.Y) / 1000.0;
                double bd = Math.Max(0.5, Math.Sqrt(bdx * bdx + bdy * bdy));
                // Sekme yönü rastgele (bloklar geri de seker, kale arkasına da) — PHYSICS domain;
                // arkaya sekenler korner üretir (gerçek futbolun ana korner kaynağı)
                int bang = (int)(Rng.Rand01(seed, Domain.Physics, (uint)(200 + i), st.Tick, 46) * TrigLut.Size);
                TrigLut.Rotate(bdx / bd, bdy / bd, bang, out double blx, out double bly);
                st.Ball.OwnerId = -1;
                st.Ball.Flight = 0;
                st.Ball.LastTouchTeam = a.TeamIdx == 0 ? (byte)1 : (byte)0; // savunucudan sekti
                // Top BLOKÇUNUN üzerinden seker — şutçunun ayağında kalmaz (rebound çorbası önlendi)
                st.Ball.X = ClampX(st.Agents[blocker].X + Units.QuantizeMm(blx * 2.5));
                st.Ball.Y = st.Agents[blocker].Y + Units.QuantizeMm(bly * 2.5);
                st.Ball.Vx = Units.QuantizeMm(blx * 9.0);
                st.Ball.Vy = Units.QuantizeMm(bly * 9.0);
                st.Ball.Vz = 0;
                a.ActionUntilTick = st.Tick + (uint)bal.possession.tackleCooldownTicks;
                pendingPassTeam = -1;
                return;
            }

            Shots++;
            RecordXg(ref st, i, dGoal, header);

            bool insidePosts = Math.Abs(interY) <= GoalHalfWidthMm;
            double vy = ((interY - a.Y) / 1000.0) / tPlane;
            double vx = planeDx / tPlane;
            byte flight = insidePosts ? (byte)1 : (byte)0; // karara bağlı gol yolu / dışarı serbest
            bool parried = false;
            int deflectFrom = -1; // topun sekerek çıktığı oyuncu (kaleci/blokçu) — konum taşınır

            if (insidePosts)
            {
                // 9.2 analitik kurtarış — kaleci: rakip takım slot 0
                int gk = a.TeamIdx == 0 ? 11 : 0;
                double tReact = bal.gk.tReactBase + (100 - Eff(gk, attrs[gk].Reflexes)) * bal.gk.tReactPerReflexEksik;
                double reach = bal.gk.reachBase + Eff(gk, attrs[gk].Agility) / 100.0 * bal.gk.reachAgilityFactor;
                double gkDist = Math.Abs(st.Agents[gk].Y - interY) / 1000.0;
                double tTraverse = gkDist / (reach / bal.gk.dalisSureCarpan);
                double marj = tPlane - (tReact + tTraverse);
                // Yakın mesafe kapatma (ME 9.3 1v1): kaleci yayılıp açıyı kapatır — OneOnOne
                // niteliği burada devreye girer. Bu terim olmadan ceza sahası içi şutlar
                // fizik gereği kurtarılamıyor ve dönüşüm oranı %30'un altına inmiyordu.
                if (dGoal < bal.gk.yakinMesafeM)
                    marj += (1.0 - dGoal / bal.gk.yakinMesafeM)
                            * (Eff(gk, attrs[gk].OneOnOne) / 100.0) * bal.gk.yakinKapatmaKatsayi;
                // Lojistik P_save Q16'ya kuantalanır (exp platform payı — LUT gerekçesiyle aynı)
                double pSave = 1.0 / (1.0 + Math.Exp(-bal.gk.logisticSlope * marj));
                if (pSave < bal.gk.saveClampMin) pSave = bal.gk.saveClampMin;
                if (pSave > bal.gk.saveClampMax) pSave = bal.gk.saveClampMax;
                pSave = (int)(pSave * 65536.0) / 65536.0;

                // Zar gerekçesi: şut-kaleci ikili mücadelesi — DUEL domain (ME 6.3/9.2)
                if (Rng.Rand01(seed, Domain.Duel, (uint)(400 + gk), st.Tick, 42) < pSave)
                {
                    Saves++;
                    int handling = Eff(gk, attrs[gk].Handling);
                    // Zar gerekçesi: tutuş kontrolü Handling'e karşı — DUEL domain (9.2)
                    bool catches = Rng.Rand01(seed, Domain.Duel, (uint)(400 + gk), st.Tick, 43) < handling / 130.0;
                    if (catches)
                    {
                        // Tut: top kaleciye uçar, varışta yalnız o alır (Flight=2, ışınlama yok)
                        double gdx = (st.Agents[gk].X - a.X) / 1000.0, gdy = (st.Agents[gk].Y - a.Y) / 1000.0;
                        double gd = Math.Max(0.5, Math.Sqrt(gdx * gdx + gdy * gdy));
                        vx = gdx / gd * bal.shotExec.sutHiziMS;
                        vy = gdy / gd * bal.shotExec.sutHiziMS;
                        flight = 2;
                    }
                    else
                    {
                        // Çeldi: kaleci topu KUTUDAN UZAĞA, direk dışına çeler — çoğu zaman
                        // korner çıkar (gerçek futbol; içeri düşen çelme rebound çorbası yapıyordu).
                        // Son dokunan KALECİ olduğu için çizgiyi geçerse KORNER verilir.
                        int sgn = interY >= 0 ? 1 : -1;
                        double outAng = bal.gk.cildirmaAcisiDeg * Math.PI / 180.0;
                        vx = Math.Sign(planeDx) * bal.shotExec.sutHiziMS * Math.Cos(outAng) * 0.7;
                        vy = sgn * bal.shotExec.sutHiziMS * Math.Sin(outAng);
                        flight = 0;
                        parried = true;
                        // Top KALECİDE çelinir — şutçunun ayağının dibinde DEĞİL. Bu konum
                        // taşıması olmadan çelinen topu ertesi tick yine şutçu alıyordu
                        // (rebound çorbası: şut ve gol sayıları gerçek dışı şişiyordu).
                        deflectFrom = gk;
                    }
                }
                // kurtaramadı → hız aynen: top çizgiyi direkler arasından geçer → EventAndState GOL sayar
            }
            // dışarı nişan → aut/degaj restart'ı doğal akışta

            st.Ball.Vx = Units.QuantizeMm(vx);
            st.Ball.Vy = Units.QuantizeMm(vy);
            st.Ball.Vz = 0; // alçak/sert şut — yüksek şut ve direk bandı M-duran-top/ince ayar
            st.Ball.OwnerId = -1;
            if (deflectFrom >= 0)
            {
                // Sekme noktası + sekme yönünde 3,5 m: aksi halde çelen kaleci topu ertesi tick
                // kendi kontrol yarıçapında bulup geri alıyor, korner hiç çıkmıyordu
                double dn = Math.Max(0.5, Math.Sqrt(vx * vx + vy * vy));
                st.Ball.X = ClampX(st.Agents[deflectFrom].X + Units.QuantizeMm(vx / dn * 3.5));
                st.Ball.Y = st.Agents[deflectFrom].Y + Units.QuantizeMm(vy / dn * 3.5);
            }
            st.Ball.LastTouchTeam = parried ? (a.TeamIdx == 0 ? (byte)1 : (byte)0) : a.TeamIdx;
            // Şutçu topu anında geri alamaz (vuruş sonrası toparlanma)
            a.ActionUntilTick = st.Tick + (uint)bal.possession.tackleCooldownTicks;
            st.Ball.Flight = flight;
            pendingPassTeam = -1;
        }

        /// <summary>xG KAYIT gerçeği — ME 15.2 birebir (ln/atan burada serbest: sonuca girmez).</summary>
        void RecordXg(ref MatchState st, int i, double dGoal, bool header = false)
        {
            ref var a = ref st.Agents[i];
            int gx = a.TeamIdx == 0 ? PitchHalfXmm : -PitchHalfXmm;
            double p1x = (gx - a.X) / 1000.0, p1y = (GoalHalfWidthMm - a.Y) / 1000.0;
            double p2y = (-GoalHalfWidthMm - a.Y) / 1000.0;
            double ang = Math.Abs(Math.Atan2(p1y, Math.Abs(p1x)) - Math.Atan2(p2y, Math.Abs(p1x)));
            int pres = Math.Min(3, NearOpponents(ref st, a.X, a.Y, a.TeamIdx, 1200));
            var g = bal.shot.xg;
            double z = g.b0 + g.bLnDist * Math.Log(Math.Max(1.0, dGoal) / 10.0) + g.bAngle * ang
                       + g.bPres * pres + (header ? g.bHeader : 0.0);
            double xg = 1.0 / (1.0 + Math.Exp(-z));
            if (a.TeamIdx == 0) XgHome += xg; else XgAway += xg;
        }

        /// <summary>Topsuz hedef — ME 7.4 alt kümesi: Anchor OMURGA; hücumda ileri itme,
        /// savunmada top-kale hattına çekilme; üstüne top çekimi karışımı. Boşluk/markaj/ofsayt
        /// vektörleri M-karar ilerisinde.</summary>
        void OffBallTarget(ref MatchState st, int i)
        {
            ref var a = ref st.Agents[i];
            var o = bal.offball;

            // Duran topu kullanacak oyuncu topa yürür (ışınlama YOK — ME 4.1)
            if (st.SetPiece != SetPieceType.None && i == st.SetPieceTaker)
            {
                a.TargetX = ClampX(st.Ball.X);
                a.TargetY = ClampY(st.Ball.Y);
                return;
            }

            // Korner dizilişi (ME 10.2): hücum kutuya doluşur, savunma gol tarafında karşılar.
            // Tam bölge/markaj çözücüsü (en iyi 3 hava topçusu, 7.5 eşlemesi) M-ileri dilimde.
            if (st.SetPiece == SetPieceType.Corner && i % 11 != 0)
            {
                bool inBoxAttack = a.TeamIdx == st.SetPieceTeam;
                int gx = st.Ball.X > 0 ? PitchHalfXmm : -PitchHalfXmm;
                int inward = st.Ball.X > 0 ? -1 : 1;
                int slot = i % 11;
                if (inBoxAttack)
                {
                    a.TargetX = ClampX(gx + inward * (6000 + (slot % 3) * 4000));
                    a.TargetY = ClampY((slot % 5 - 2) * 4500);
                }
                else
                {
                    a.TargetX = ClampX(gx + inward * (3500 + (slot % 3) * 3500));
                    a.TargetY = ClampY((slot % 5 - 2) * 3800);
                }
                return;
            }

            // Kaleci (slot 0/11): ME 9.1 pozisyonlama — top-kale açıortayında derinlik clamp'i;
            // prese/kovalamaya KATILMAZ (çıkış kararı 9.3 M-duran-top diliminde)
            if (i % 11 == 0)
            {
                int ogx = a.TeamIdx == 0 ? -PitchHalfXmm : PitchHalfXmm;
                double bdx = (st.Ball.X - ogx) / 1000.0, bdy = st.Ball.Y / 1000.0;
                double bd = Math.Max(0.5, Math.Sqrt(bdx * bdx + bdy * bdy));
                double depth = bal.gk.derinlikTaban + bal.gk.derinlikPerM * bd;
                if (depth > bal.gk.derinlikMax) depth = bal.gk.derinlikMax;
                a.TargetX = ClampX(Units.QuantizeMm(ogx / 1000.0 + bdx / bd * depth));
                a.TargetY = ClampY(Units.QuantizeMm(bdy / bd * depth));
                return;
            }

            // Pres tetiği (ME 7.6: "en yakın 2 ajan PressNode'a") + serbest top kovalama:
            // top rakipte ya da serbestse ve takımımda topa en yakın 2 kişiden biriysem → topa git.
            // Tam tetik seti (geri pas, tuzak bölgesi, pres şiddeti talimatı) M-taktik diliminde.
            int owner = st.Ball.OwnerId;
            bool oppOrFree = owner < 0 || st.Agents[owner].TeamIdx != a.TeamIdx;
            if (oppOrFree && NearestRankToBall(ref st, i) < 2)
            {
                a.TargetX = ClampX(st.Ball.X);
                a.TargetY = ClampY(st.Ball.Y);
                return;
            }

            int ownerTeam = owner >= 0 ? st.Agents[owner].TeamIdx : st.Ball.LastTouchTeam;
            bool attacking = ownerTeam == a.TeamIdx;
            int dir = a.TeamIdx == 0 ? 1 : -1;

            double bx, by;
            if (attacking)
            {
                // Hücum: anchor omurgası + ileri itme (ME 7.4-A) + GENİŞLİK: kanat rolleri
                // touchline'a açılır. Bu vektör olmadan oyun merkeze sıkışıyor ve taç üretimi
                // sıfıra iniyordu (M4 borcu).
                bx = a.AnchorX + dir * o.fazIleriM * 1000.0;
                by = a.AnchorY;
                if (Math.Abs(a.AnchorY) > o.kanatAnchorEsikMm)
                    by += Math.Sign(a.AnchorY) * o.kanatGenislikM * 1000.0;
            }
            else
            {
                // SAVUNMA BLOKU — ME 7.4-B/7.6: görev vektörü = top-kendi kalesi çizgisine iniş
                // (kanal kapama) + hat hizalama; role göre derinlik. Bu vektör olmadan savunma
                // alanı kapatmıyor, taşıyıcı ceza sahasına kadar yürüyordu.
                int ownGoalX = a.TeamIdx == 0 ? -PitchHalfXmm : PitchHalfXmm;
                double depth = a.RoleId <= 2 ? o.hatDerinlikDf
                             : a.RoleId == 3 ? o.hatDerinlikMf : o.hatDerinlikFw;
                bx = ownGoalX + (st.Ball.X - ownGoalX) * depth;
                by = a.AnchorY * o.hatYanAnchor + st.Ball.Y * (1.0 - o.hatYanAnchor);

                // MARKAJ (ME 7.5): görevli savunucu, hedefinin gol tarafına iner — bölgesel
                // duruşun üstüne biner. Atama top kaybında koordinatörle yapılır (AssignMarking).
                if (a.MarkTarget >= 0 && st.Agents[a.MarkTarget].Active)
                {
                    ref var t = ref st.Agents[a.MarkTarget];
                    double gx2 = ownGoalX;
                    bx = t.X + (gx2 - t.X) * o.markajGolTarafi;
                    by = t.Y + (0 - t.Y) * o.markajGolTarafi * 0.5;
                }
            }
            double tx = bx + o.wTop * (st.Ball.X - bx);
            double ty = by + o.wTop * (st.Ball.Y - by);

            int targetX = Units.QuantizeMm(tx / 1000.0);
            // OFSAYT KISITI — ME 7.4: hücumda hedef x, rakip son savunucu çizgisinin en fazla
            // 0,3 m gerisine kırpılır. Bu kısıt olmadan forvetler kaleye demirliyor (kamp) ve
            // şut/gol üretimi gerçek dışı patlıyordu.
            if (attacking)
            {
                int line = OffsideLineX(ref st, a.TeamIdx);
                if (a.TeamIdx == 0) { if (targetX > line + 300) targetX = line + 300; }
                else { if (targetX < line - 300) targetX = line - 300; }
            }

            a.TargetX = ClampX(targetX);
            a.TargetY = ClampY(Units.QuantizeMm(ty / 1000.0));
        }

        // ---------------------------------------------------------------- aksiyon çözümü (ME 4.3, 6.3-6.4)

        void ActionResolutionPass(ref MatchState st)
        {
            // 0) Havadan gelen orta iniyor mu — hava topu düellosu (ME 10.2 çözüm zinciri)
            if (st.Ball.Flight == 4 && st.Ball.OwnerId < 0 &&
                st.Ball.Z <= bal.setpiece.havaTopuYukseklikM * 1000 && st.Ball.Vz < 0)
                ResolveAerial(ref st);

            // 1) Serbest top kontrolü — ilk ulaşan alır; aynı tick'te iki aday → kontrol düellosu (4.3)
            // Uçuş kilitleri: 1 = karara bağlı şut (kimse alamaz), 2 = kaleci tutuşu (yalnız o),
            // 3 = ölü top (yalnız kullanacak takım), 4 = havadaki orta (düello çözer)
            if (st.Ball.OwnerId < 0 && st.Ball.Z < 400 && st.Ball.Flight != 1 && st.Ball.Flight != 4)
            {
                int c1 = -1, c2 = -1; long d1 = long.MaxValue, d2 = long.MaxValue;
                long r2 = (long)(bal.possession.kontrolYaricapM * 1000) * (long)(bal.possession.kontrolYaricapM * 1000);
                int onlyGk = st.Ball.Flight == 2 ? (st.Ball.LastTouchTeam == 0 ? 11 : 0) : -1;
                byte deadBallTeam = st.Ball.Flight == 3 ? st.SetPieceTeam : (byte)2;
                for (int i = 0; i < 22; i++) // sıra sabit
                {
                    if (!st.Agents[i].Active) continue;
                    if (onlyGk >= 0 && i != onlyGk) continue;
                    if (deadBallTeam != 2 && st.Agents[i].TeamIdx != deadBallTeam) continue;
                    long dx = st.Agents[i].X - st.Ball.X, dy = st.Agents[i].Y - st.Ball.Y;
                    long dd = dx * dx + dy * dy;
                    if (dd > r2) continue;
                    if (dd < d1) { c2 = c1; d2 = d1; c1 = i; d1 = dd; }
                    else if (dd < d2) { c2 = i; d2 = dd; }
                }
                if (c1 >= 0)
                {
                    int winner = c1;
                    if (c2 >= 0 && st.Agents[c1].TeamIdx != st.Agents[c2].TeamIdx)
                    {
                        // Kontrol düellosu kompoziti (ME 6.4): 0.5 Accel + 0.3 Agility + 0.2 Strength
                        double atk = Composite(c1, 0.5, attrs[c1].Acceleration, 0.3, attrs[c1].Agility, 0.2, attrs[c1].Strength);
                        double def = Composite(c2, 0.5, attrs[c2].Acceleration, 0.3, attrs[c2].Agility, 0.2, attrs[c2].Strength);
                        // Zar gerekçesi: ikili mücadele — DUEL domain (ME 3.1/6.3)
                        if (!DuelWin(atk, def, (uint)(300 + c1), st.Tick, 31)) winner = c2;
                    }
                    ClaimBall(ref st, winner);
                }
            }

            // 2) Taşıyıcıya yapıştırma (top ayakta taşınır)
            if (st.Ball.OwnerId >= 0)
            {
                ref var c = ref st.Agents[st.Ball.OwnerId];
                st.Ball.X = c.X; st.Ball.Y = c.Y; st.Ball.Z = 0;
                st.Ball.Vx = c.Vx; st.Ball.Vy = c.Vy; st.Ball.Vz = 0;

                // 3) Tackle girişimi — ilk uygun savunucu (sıra sabit), tek girişim/tick (ME 6.4)
                long r2 = (long)(bal.possession.tackleYaricapM * 1000) * (long)(bal.possession.tackleYaricapM * 1000);
                for (int i = 0; i < 22; i++)
                {
                    ref var d = ref st.Agents[i];
                    if (!d.Active || d.TeamIdx == c.TeamIdx || st.Tick < d.ActionUntilTick) continue;
                    long dx = d.X - c.X, dy = d.Y - c.Y;
                    if (dx * dx + dy * dy > r2) continue;
                    // Yalnız EN YAKIN savunucu dalar; diğerleri jokeyler (ME 7.6 pres tetiği ruhu).
                    // Aksi halde her presçi ayrı ayrı daldığından müdahale/faul sıklığı gerçek dışıydı.
                    if (NearestRankToBall(ref st, i) != 0) continue;

                    double atk = Composite(i, 0.6, attrs[i].Tackling, 0.25, attrs[i].Positioning, 0.15, attrs[i].Strength);
                    double def = Composite(st.Ball.OwnerId, 0.5, attrs[st.Ball.OwnerId].Dribbling, 0.3, attrs[st.Ball.OwnerId].Agility, 0.2, attrs[st.Ball.OwnerId].Strength);
                    d.ActionUntilTick = st.Tick + (uint)bal.possession.tackleCooldownTicks;
                    // Zar gerekçesi: top kapma düellosu — DUEL domain (ME 6.3-6.4).
                    // P_taban düello TİPİNE göre değişir (ME 6.3: 0,42-0,55 bandı "tipe göre");
                    // top kapma tabanı ayrı [KALİBRE] anahtardır — varsayılanla oynandığında
                    // maç başına müdahale sayısı gerçek dışıydı (pinball etkisi).
                    if (DuelWin(atk, def, (uint)(300 + i), st.Tick, 32, bal.duel.pTabanTackle))
                    {
                        Tackles++;
                        st.Ball.OwnerId = -1;
                        st.Ball.LastTouchTeam = d.TeamIdx;
                        pendingPassTeam = -1;
                        // Kazanılan top açığa çıkar — yön DUEL akışından (sunum değil sonuç durumu)
                        int ang = (int)(Rng.Rand01(seed, Domain.Duel, (uint)(300 + i), st.Tick, 33) * TrigLut.Size);
                        TrigLut.Rotate(1.0, 0.0, ang, out double lx, out double ly);
                        st.Ball.Vx = Units.QuantizeMm(bal.possession.tackleLooseHizMS * lx);
                        st.Ball.Vy = Units.QuantizeMm(bal.possession.tackleLooseHizMS * ly);
                        c.ActionUntilTick = st.Tick + (uint)bal.possession.tackleCooldownTicks;
                    }
                    else
                    {
                        // KAYBEDİLEN müdahale şiddet skoru üretir — ME 11.2
                        ResolveFoul(ref st, defender: i, victim: st.Ball.OwnerId, atkEff: atk, defEff: def);
                    }
                    break;
                }
            }
        }

        /// <summary>Foul tespiti + kart — ME Spec 11.2 birebir.
        /// s = 0,4×margin_açığı + 0,25×hız + 0,2×arkadan_mı + 0,15×ayak_yüksekliği (0-1);
        /// ayak yüksekliği motorda modellenmediğinden (Z sadece topta) payı 0 geçilir — bilinçli
        /// M4 sınırı. Aggression > 70 → +0,05. Eşik: 0,30 − (Strictness−50)×0,002; gri bant ±0,06
        /// REFEREE çekilişiyle çözülür (tartışmalı pozisyonlar buradan doğar).</summary>
        void ResolveFoul(ref MatchState st, int defender, int victim, double atkEff, double defEff)
        {
            ref var d = ref st.Agents[defender];
            // margin_açığı 0-1 normalize: 50 puanlık nitelik açığı tavan sayılır (100 puanlık açık
            // futbolda görülmez; /100 normalizasyonu şiddet skorunu ölü bölgede bırakıyordu)
            double marginGap = Math.Min(1.0, Math.Max(0.0, (defEff - atkEff) / 50.0));
            double vMaxRef = bal.move.vMaxBase + bal.move.vMaxPaceSpan;
            double speed = Math.Min(1.0, Math.Sqrt((double)d.Vx * d.Vx + (double)d.Vy * d.Vy) / 1000.0 / vMaxRef);
            // Arkadan mı: müdahale vektörü taşıyıcının gidiş yönüyle aynı yarım düzlemdeyse
            ref var c = ref st.Agents[victim];
            double apx = c.X - d.X, apy = c.Y - d.Y;
            double fromBehind = (apx * c.Vx + apy * c.Vy) > 0 ? 1.0 : 0.0;
            double s = 0.4 * marginGap + 0.25 * speed + 0.2 * fromBehind;
            if (Eff(defender, attrs[defender].Aggression) > 70) s += 0.05;
            // Ceza sahasında savunucu AYAKTA kalır, dalmaz — şiddet skoru bu ihtiyatla ölçeklenir.
            // Model eki (spec dışı davranış gerçeği): bu çarpan olmadan penaltı sıklığı 1,2/maç
            // çıkıyordu (gerçek ~0,25). [KALİBRE referee.cezaSahasiIhtiyatCarpan]
            if (InPenaltyBox(st.Ball.X, st.Ball.Y, d.TeamIdx)) s *= bal.referee.cezaSahasiIhtiyatCarpan;

            double esik = bal.referee.foulEsikTaban - (referee.Strictness - 50) * bal.referee.strictnessCarpan;
            double band = bal.referee.griBantOrta;
            bool foul;
            if (s > esik + band) foul = true;
            else if (s < esik - band) foul = false;
            else
            {
                // Gri bant — REFEREE domain; Consistency yüksek hakem eşiğe daha sadık
                double lean = (s - (esik - band)) / (2 * band);
                double pull = 0.5 + (lean - 0.5) * (0.5 + referee.Consistency / 200.0);
                foul = Rng.Rand01(seed, Domain.Referee, (uint)(500 + defender), st.Tick, 51) < pull;
            }
            if (!foul) return;

            // Avantaj (11.2): mağdur topa sahipse ve hakem eğilimi tutarsa oyun devam eder
            if (st.Ball.OwnerId == victim &&
                Rng.Rand01(seed, Domain.Referee, (uint)(500 + defender), st.Tick, 52) < referee.AdvantageTendency / 100.0)
            { Advantages++; return; }

            Fouls++;
            // Sert müdahale sakatlık riski doğurur — ME 11.3 → 12.2 bağlantısı
            if (s > bal.injury.sertMudahaleEsik) TryInjure(ref st, victim, bal.injury.pTabanMudahale);
            if (s > bal.referee.kirmiziEsik)
            {
                d.SentOff = true; Reds++;
            }
            else if (s > bal.referee.sariEsik)
            {
                d.YellowCards++; Yellows++;
                if (d.YellowCards >= 2) { d.SentOff = true; Reds++; } // ikinci sarı otomatiği
            }
            if (d.SentOff) { d.Vx = 0; d.Vy = 0; }

            // Duran top: ceza sahası içinde penaltı, dışında frikik (10.3/10.4)
            byte forTeam = c.TeamIdx;
            bool inBox = InPenaltyBox(st.Ball.X, st.Ball.Y, defendingTeam: d.TeamIdx);
            if (inBox) AwardPenalty(ref st, forTeam);
            else { FreeKicks++; AwardSetPiece(ref st, SetPieceType.FreeKick, forTeam, st.Ball.X, st.Ball.Y); }
        }

        /// <summary>Markaj atama çözücüsü — ME Spec 7.5: tehditler xT + Pace ile skorlanır,
        /// en yüksek tehditten başlayarak en yakın uygun savunucu greedy atanır; kalanlar bölgesel.
        /// Adam adama TALİMATI (kullanıcı kilidi) M6 müdahale katmanında bağlanır.
        /// Sabit tarama sırası + eşitlik bozucu indeks → deterministik (ME 3.2).</summary>
        void AssignMarking(ref MatchState st, byte defendingTeam)
        {
            int d0 = defendingTeam == 0 ? 0 : 11;
            int a0 = defendingTeam == 0 ? 11 : 0;
            byte attTeam = defendingTeam == 0 ? (byte)1 : (byte)0;
            for (int i = d0; i < d0 + 11; i++) st.Agents[i].MarkTarget = -1;

            Span<int> threats = stackalloc int[11];
            Span<double> scores = stackalloc double[11];
            int tn = 0;
            for (int j = a0; j < a0 + 11; j++)
            {
                if (!st.Agents[j].Active || j % 11 == 0) continue;
                scores[tn] = XtAt(st.Agents[j].X, st.Agents[j].Y, attTeam)
                             + 0.2 * (Eff(j, attrs[j].Pace) / 100.0);
                threats[tn] = j;
                tn++;
            }
            // Tehdide göre azalan sırala (küçük N, tahsissiz, deterministik)
            for (int m = 1; m < tn; m++)
                for (int n = m; n > 0 && scores[n] > scores[n - 1]; n--)
                {
                    (scores[n], scores[n - 1]) = (scores[n - 1], scores[n]);
                    (threats[n], threats[n - 1]) = (threats[n - 1], threats[n]);
                }

            Span<bool> used = stackalloc bool[11];
            int assigned = 0;
            for (int k = 0; k < tn && assigned < bal.offball.markajSayisi; k++)
            {
                int t = threats[k];
                int best = -1; long bd = long.MaxValue;
                for (int i = d0; i < d0 + 11; i++)
                {
                    if (i % 11 == 0 || used[i - d0] || !st.Agents[i].Active) continue;
                    if (st.Agents[i].RoleId > 3) continue; // forvet markaja inmez
                    long dx = st.Agents[i].X - st.Agents[t].X, dy = st.Agents[i].Y - st.Agents[t].Y;
                    long dd = dx * dx + dy * dy;
                    if (dd < bd) { bd = dd; best = i; }
                }
                if (best < 0) break;
                used[best - d0] = true;
                st.Agents[best].MarkTarget = (short)t;
                assigned++;
            }
        }

        /// <summary>Kritik dakika baskısı — ME Spec 12.3:
        /// c = (dk−80)/10 × (2−|fark|)/2; Composure etkinin %60'ını söndürür.</summary>
        double CriticalPressure(in MatchState st, int i)
        {
            double dk = st.Tick / (double)TicksPerSecond / 60.0;
            if (dk <= 80) return 0;
            int fark = Math.Abs(st.HomeGoals - st.AwayGoals);
            if (fark > 1) return 0;
            double c = Math.Min(1.0, (dk - 80) / 10.0) * (2 - fark) / 2.0;
            double comp = Eff(i, attrs[i].Composure) / 100.0;
            return c * (1.0 - 0.6 * comp) * bal.momentum.baskiNisanCarpan;
        }

        /// <summary>Sakatlık üretimi — ME Spec 12.2.
        /// p = p_taban(olay) × M_yorgunluk × M_yatkınlık; M_yorgunluk = 1 + max(0, 300−Energy)/300 × 1,5.
        /// Şiddet dağılımı [KALİBRE injury.siddetDagilimi]; Hafif sahada kalır (nitelik −5),
        /// üstü sahayı terk eder. M_yatkınlık oyuncu profili FAZ 04 veri katmanında (şimdilik 1,0).</summary>
        void TryInjure(ref MatchState st, int victim, double pBase)
        {
            ref var v = ref st.Agents[victim];
            if (!v.Active) return;
            double mYorgun = 1.0 + Math.Max(0, 300 - v.Energy) / 300.0 * 1.5;
            double p = pBase * mYorgun;
            // Zar gerekçesi: sakatlık kendi domain akışından — INJURY (ME 3.1)
            if (Rng.Rand01(seed, Domain.Injury, (uint)(950 + victim), st.Tick, 81) >= p) return;

            double r = Rng.Rand01(seed, Domain.Injury, (uint)(950 + victim), st.Tick, 82);
            var dist = bal.injury.siddetDagilimi;
            double acc = 0; int sev = 1;
            for (int k = 0; k < dist.Length; k++) { acc += dist[k]; if (r < acc) { sev = k + 1; break; } }
            v.Injury = (InjuryState)Math.Min(4, sev);
            Injuries++;
            if (v.Injury > InjuryState.Hafif)
            {
                v.Vx = 0; v.Vy = 0;
                if (st.Ball.OwnerId == victim) { st.Ball.OwnerId = -1; st.Ball.Flight = 0; }
                // Oyun durur: sakatlanan takıma duran top (oyuncu değişikliği M6 müdahale katmanında)
                AwardSetPiece(ref st, SetPieceType.FreeKick, v.TeamIdx, st.Ball.X, st.Ball.Y);
            }
        }

        /// <summary>Ceza sahası testi (16,5 m derinlik, 40,32 m genişlik) — savunan takımın sahası.</summary>
        static bool InPenaltyBox(int x, int y, byte defendingTeam)
        {
            int goalX = defendingTeam == 0 ? -PitchHalfXmm : PitchHalfXmm;
            return Math.Abs(x - goalX) <= 16500 && Math.Abs(y) <= 20160;
        }

        void ClaimBall(ref MatchState st, int i)
        {
            ref var a = ref st.Agents[i];
            if (st.Ball.OwnerId == a.Id) return;

            // Ofsayt düdüğü — ME 10.5: ihlalli alıcı topa dokunduğunda oyun durur
            if (pendingOffsidePlayer == i)
            {
                pendingOffsidePlayer = -1;
                Offsides++;
                byte def = a.TeamIdx == 0 ? (byte)1 : (byte)0;
                AwardSetPiece(ref st, SetPieceType.FreeKick, def, a.X, a.Y);
                return;
            }
            pendingOffsidePlayer = -1;

            st.Ball.OwnerId = a.Id;
            st.Ball.Flight = 0;
            st.Ball.Vx = a.Vx; st.Ball.Vy = a.Vy; st.Ball.Vz = 0; st.Ball.Z = 0;
            if (pendingPassTeam >= 0)
            {
                if (pendingPassTeam == a.TeamIdx) PassCompletions++;
                pendingPassTeam = -1;
            }
            if (st.Ball.LastTouchTeam != 2 && st.Ball.LastTouchTeam != a.TeamIdx)
            {
                PossessionChanges++;
                AssignMarking(ref st, defendingTeam: st.Ball.LastTouchTeam); // ME 7.5: geçiş anında
            }
            st.Ball.LastTouchTeam = a.TeamIdx;

            // Duran top kullanıldı: bayrak HER durumda temizlenir — topu atanan kullanıcı yerine
            // takım arkadaşı alırsa bayrak asılı kalıyor ve korner dizilişi sonsuza donuyordu
            // (M4 kilit düzeltmesi). Korner ANINDA ortaya gider (ME 10.2).
            if (st.SetPiece != SetPieceType.None)
            {
                bool corner = st.SetPiece == SetPieceType.Corner && a.TeamIdx == st.SetPieceTeam;
                st.SetPiece = SetPieceType.None;
                st.SetPieceTaker = -1;
                if (corner) { ExecuteCross(ref st, i); return; }
            }
            if (st.Phase == MatchPhase.Kickoff || st.Phase == MatchPhase.DeadBall ||
                st.Phase == MatchPhase.SetPiece)
                st.Phase = MatchPhase.OpenPlay;
        }

        // ---------------------------------------------------------------- fizik (ME 8)

        void PhysicsPass(ref MatchState st)
        {
            // Ajanlar — sıra sabit; ivme sınırlı hedef takibi (ME 8.1); dönüş/Agility incelikleri M-ileri
            for (int i = 0; i < 22; i++)
            {
                ref var a = ref st.Agents[i];
                if (!a.Active) continue;

                double vMax = bal.move.vMaxBase + bal.move.vMaxPaceSpan * Eff(i, attrs[i].Pace) / 100.0;
                if (st.Ball.OwnerId == a.Id)
                    vMax *= bal.move.dribbleCarpanBase + bal.move.dribbleCarpanPerPuan * Eff(i, attrs[i].Dribbling);
                double aMax = bal.move.aMaxBase + bal.move.aMaxAccelSpan * Eff(i, attrs[i].Acceleration) / 100.0;

                double dxM = (a.TargetX - a.X) / 1000.0, dyM = (a.TargetY - a.Y) / 1000.0;
                double dist = Math.Sqrt(dxM * dxM + dyM * dyM);
                double desX = 0, desY = 0;
                if (dist > 0.05)
                {
                    // SEYİR YOĞUNLUĞU: futbolcu her an tam gaz koşmaz — topa/göreve yakınken
                    // sprint, uzakken jog. Bu olmadan 22 ajan 90 dakika boyunca v_max'ta koşuyor
                    // (sprint sayacı ve stamina modeli gerçek dışı kalıyordu).
                    long bdx = st.Ball.X - a.X, bdy = st.Ball.Y - a.Y;
                    bool urgent = st.Ball.OwnerId == a.Id || i == st.SetPieceTaker
                                  || bdx * bdx + bdy * bdy < (long)bal.move.sprintYaricapM * 1000 * (long)(bal.move.sprintYaricapM * 1000);
                    double intensity = urgent ? 1.0 : bal.move.seyirYogunlugu;
                    double sp = Math.Min(vMax * intensity, dist / Dt * 0.5); // varışta yavaşlama
                    desX = dxM / dist * sp; desY = dyM / dist * sp;
                }
                double vx = a.Vx / 1000.0, vy = a.Vy / 1000.0;
                double dvx = desX - vx, dvy = desY - vy;
                double dv = Math.Sqrt(dvx * dvx + dvy * dvy), dvMax = aMax * Dt;
                if (dv > dvMax) { dvx = dvx / dv * dvMax; dvy = dvy / dv * dvMax; }
                vx += dvx; vy += dvy;
                a.Vx = Units.QuantizeMm(vx); a.Vy = Units.QuantizeMm(vy);
                a.X = ClampX(Units.QuantizeMm(a.X / 1000.0 + vx * Dt));
                a.Y = ClampY(Units.QuantizeMm(a.Y / 1000.0 + vy * Dt));

                // STAMINA — ME 12.1: ΔE = k_e × (v/v_max)^2,2 × M_workrate (+ pres eki);
                // ölü topta toparlanma. Hava/zemin çarpanları 12.4 dilimine dek nötr.
                double spd = Math.Sqrt(vx * vx + vy * vy);
                var sc = bal.stamina;
                if (st.Phase == MatchPhase.OpenPlay)
                {
                    double wr = 0.8 + 0.4 * (Eff(i, attrs[i].Workrate) / 100.0);
                    double drain = sc.kE * (luts.DrenajQ16(spd / Math.Max(0.1, vMax)) / 65536.0) * wr;
                    if (st.Ball.OwnerId >= 0 && st.Agents[st.Ball.OwnerId].TeamIdx != a.TeamIdx
                        && NearestRankToBall(ref st, i) < 2)
                        drain += sc.presEkMaliyet;   // pres ek maliyeti (12.1)
                    // Energy tamsayıdır (ME 3.2): kesirli drenaj biriktirilir, tam birim düşülür
                    energyAccum[i] += drain;
                    int whole = (int)energyAccum[i];
                    if (whole > 0)
                    {
                        energyAccum[i] -= whole;
                        int e = a.Energy - whole;
                        a.Energy = (ushort)(e < 0 ? 0 : e);
                    }
                    // Sprint sayacı: v_max'ın %85'i üzeri efor (ME 12.1 — Panorama/antrenman verisi)
                    // Sprint sayacı histerezis + 2 sn soğuma: aksi halde hız eşiğinde salınım
                    // maç başına on binlerce "sprint" sayıyordu (Panorama verisi yanıltıcı olurdu)
                    if (spd > vMax * 0.85 && !sprinting[i] && st.Tick >= sprintCooldownUntil[i])
                    {
                        a.Sprints++; sprinting[i] = true;
                        sprintCooldownUntil[i] = st.Tick + 20;
                        // Yorgunken sprint sakatlık tetiğidir — ME 12.2 tetik listesi
                        if (a.Energy < sc.yorgunlukEsik) TryInjure(ref st, i, bal.injury.pTabanSprint);
                    }
                    else if (spd < vMax * 0.6) sprinting[i] = false;
                }
                else
                {
                    // Ölü topta toparlanma: +2/sn (ME 12.1) — aynı birikim mantığı, ters yön
                    energyAccum[i] -= sc.deadBallRecoveryPerSn * Dt;
                    if (energyAccum[i] <= -1.0)
                    {
                        int back = (int)(-energyAccum[i]);
                        energyAccum[i] += back;
                        int e = a.Energy + back;
                        a.Energy = (ushort)(e > 1000 ? 1000 : e);
                    }
                }
            }

            // Top — sahipliyken yapıştırıldı; serbest topa 8.2/8.3 fiziği
            if (st.Ball.OwnerId < 0)
            {
                double vx = st.Ball.Vx / 1000.0, vy = st.Ball.Vy / 1000.0, vz = st.Ball.Vz / 1000.0;
                double zM = st.Ball.Z / 1000.0;
                if (zM > 0.0 || vz > 0.0)
                {
                    vz -= G * Dt; // balistik (ME 8.3); Magnus M3+ (orta/frikik)
                    double sp = Math.Sqrt(vx * vx + vy * vy + vz * vz);
                    double drag = 1.0 - bal.physics.dragK * sp * Dt;
                    if (drag < 0) drag = 0;
                    vx *= drag; vy *= drag; vz *= drag;
                    zM += vz * Dt;
                    if (zM <= 0.0)
                    {
                        zM = 0.0;
                        vz = -vz * bal.physics.sekmeEKuru;   // sekme (ME 8.3)
                        vx *= bal.physics.sekmeYatayCarpan;
                        vy *= bal.physics.sekmeYatayCarpan;
                        if (vz < 0.8) vz = 0.0;              // küçük sekmeler söner
                    }
                }
                else
                {
                    double sp = Math.Sqrt(vx * vx + vy * vy); // yerde sürtünme (ME 8.2)
                    if (sp > 0)
                    {
                        double ns = sp - bal.physics.aRollKuru * Dt;
                        if (ns < 0.05) ns = 0;
                        vx *= ns / sp; vy *= ns / sp;
                    }
                }
                st.Ball.Vx = Units.QuantizeMm(vx); st.Ball.Vy = Units.QuantizeMm(vy); st.Ball.Vz = Units.QuantizeMm(vz);
                st.Ball.X = Units.QuantizeMm(st.Ball.X / 1000.0 + vx * Dt);
                st.Ball.Y = Units.QuantizeMm(st.Ball.Y / 1000.0 + vy * Dt);
                st.Ball.Z = Units.QuantizeMm(zM);
            }
        }

        // ---------------------------------------------------------------- durum/sınır/saat (M2-M4)

        void EventAndStatePass(ref MatchState st)
        {
            // Çizgi geçişleri: GOL / korner / kale vuruşu / taç (ME 10.1-10.2)
            if (st.Ball.OwnerId < 0 &&
                (Math.Abs(st.Ball.X) > PitchHalfXmm || Math.Abs(st.Ball.Y) > PitchHalfYmm))
            {
                bool crossedGoalLine = Math.Abs(st.Ball.X) > PitchHalfXmm;
                bool goal = crossedGoalLine &&
                            Math.Abs(st.Ball.Y) <= GoalHalfWidthMm && st.Ball.Z <= GoalHeightMm;
                if (goal)
                {
                    byte scorer = st.Ball.X > 0 ? (byte)0 : (byte)1; // +x çizgisi = ev hücum yönü
                    if (scorer == 0) st.HomeGoals++; else st.AwayGoals++;
                    AddMomentum(ref st, scorer, bal.momentum.golDelta);       // ME 12.3: gol +4
                    AddMomentum(ref st, scorer == 0 ? (byte)1 : (byte)0, -bal.momentum.golDelta);
                    KickoffRestart(ref st, startTeam: scorer == 0 ? (byte)1 : (byte)0);
                }
                else if (crossedGoalLine)
                {
                    // Kaleyi kaçıran top: son dokunan HÜCUM edense kale vuruşu, SAVUNANsa korner
                    byte defending = st.Ball.X > 0 ? (byte)1 : (byte)0;
                    if (st.Ball.LastTouchTeam == defending)
                    {
                        Corners++;
                        byte attacking = defending == 0 ? (byte)1 : (byte)0;
                        int cx = st.Ball.X > 0 ? PitchHalfXmm : -PitchHalfXmm;
                        int cy = st.Ball.Y >= 0 ? PitchHalfYmm : -PitchHalfYmm;
                        AwardSetPiece(ref st, SetPieceType.Corner, attacking, cx, cy);
                    }
                    else
                    {
                        GoalKicks++;
                        int goalX = defending == 0 ? -PitchHalfXmm : PitchHalfXmm;
                        int inward = defending == 0 ? 5500 : -5500;
                        AwardSetPiece(ref st, SetPieceType.GoalKick, defending, goalX + inward, 0);
                    }
                }
                else
                {
                    ThrowIns++;
                    byte toTeam = st.Ball.LastTouchTeam == 0 ? (byte)1 : (byte)0;
                    AwardSetPiece(ref st, SetPieceType.ThrowIn, toTeam, st.Ball.X, ClampY(st.Ball.Y));
                }
                OutOfBounds++;
            }

            AdvanceClock(ref st);

            if (st.Tick % ChecksumCadenceTicks == 0)
                st.LastChecksum = StateHash(in st);
        }

        /// <summary>Maç saati + devreler + uzatma — ME Spec 3.4.
        /// Uzatma = clamp(round(0,55×durak_dk + kart×0,3 + gol×0,35), 1, 9) dk; duraklama birikimi
        /// duran top hazırlıklarından gelir (sıkıştırılmış çözüm). Devre arası saha değişimi
        /// (anchor aynalama) bilinçli olarak M-ileri dilimde.</summary>
        /// <summary>Momentum deltası — ME 12.3, [−10, +10] bandında kırpılır.</summary>
        void AddMomentum(ref MatchState st, byte team, int delta)
        {
            ref var rt = ref team == 0 ? ref st.HomeRt : ref st.AwayRt;
            int m = rt.Momentum + delta;
            rt.Momentum = (sbyte)(m < -10 ? -10 : (m > 10 ? 10 : m));
        }

        /// <summary>Momentum sönümü — ME 12.3: dakikada 0,3 hızla 0'a döner.</summary>
        void DecayMomentum(ref MatchState st)
        {
            if (st.Tick % 600 != 0) return; // dakikada bir (600 tick = 60 sn)
            momentumAccumHome += bal.momentum.sonumPerDk;
            momentumAccumAway += bal.momentum.sonumPerDk;
            if (momentumAccumHome >= 1.0 && st.HomeRt.Momentum != 0)
            {
                momentumAccumHome -= 1.0;
                st.HomeRt.Momentum -= (sbyte)Math.Sign(st.HomeRt.Momentum);
            }
            if (momentumAccumAway >= 1.0 && st.AwayRt.Momentum != 0)
            {
                momentumAccumAway -= 1.0;
                st.AwayRt.Momentum -= (sbyte)Math.Sign(st.AwayRt.Momentum);
            }
        }

        void AdvanceClock(ref MatchState st)
        {
            st.Tick++;
            DecayMomentum(ref st);
            if (st.Phase == MatchPhase.FullTime) return;

            uint normalEnd = st.Half == 1 ? HalfTicks : HalfTicks * 2;
            if (st.HalfEndTick == 0 && st.Tick >= normalEnd)
            {
                int cards = Yellows + Reds - halfCardsBase;
                int goals = st.HomeGoals + st.AwayGoals - halfGoalsBase;
                double durakDk = (st.StoppageTicks - halfStoppageBase) / (double)TicksPerSecond / 60.0;
                var e = bal.extraTime;
                int dk = (int)Math.Round(e.durakCarpan * durakDk + e.kartCarpan * cards + e.golCarpan * goals);
                if (dk < e.minDk) dk = e.minDk;
                if (dk > e.maxDk) dk = e.maxDk;
                st.HalfEndTick = normalEnd + (uint)dk * 60 * (uint)TicksPerSecond;
            }

            if (st.HalfEndTick != 0 && st.Tick >= st.HalfEndTick)
            {
                if (st.Half == 1)
                {
                    st.Half = 2;
                    st.HalfEndTick = 0;
                    halfCardsBase = Yellows + Reds;
                    halfGoalsBase = st.HomeGoals + st.AwayGoals;
                    halfStoppageBase = st.StoppageTicks;
                    st.Phase = MatchPhase.HalfTime;
                    // Devre arası toparlanması: +150 (tavan 1000) — ME 12.1
                    for (int i = 0; i < 22; i++)
                    {
                        int e = st.Agents[i].Energy + (int)bal.stamina.devreArasi;
                        st.Agents[i].Energy = (ushort)(e > 1000 ? 1000 : e);
                    }
                    KickoffRestart(ref st, startTeam: 1); // ikinci devreyi deplasman başlatır
                }
                else
                {
                    st.Phase = MatchPhase.FullTime;
                    st.Ball.OwnerId = -1;
                    st.Ball.Vx = st.Ball.Vy = st.Ball.Vz = 0;
                    st.Ball.Flight = 0;
                }
            }
        }

        // ---------------------------------------------------------------- duran toplar (ME 10)

        /// <summary>Duran top ver: top ÖLÜ (Flight=3 kilidi — yalnız kullanacak takım alır),
        /// kullanacak oyuncu topa YÜRÜR (ışınlama yok). Hazırlık süresi maç saatinden düşer
        /// (sıkıştırılmış çözüm, ME 3.4) → uzatma hesabına girer.</summary>
        void AwardSetPiece(ref MatchState st, SetPieceType type, byte forTeam, int x, int y)
        {
            st.SetPiece = type;
            st.SetPieceTeam = forTeam;
            st.Ball.X = ClampX(x); st.Ball.Y = ClampY(y); st.Ball.Z = 0;
            st.Ball.Vx = st.Ball.Vy = st.Ball.Vz = 0;
            st.Ball.OwnerId = -1;
            st.Ball.LastTouchTeam = forTeam;
            st.Ball.Flight = 3;
            st.Phase = type == SetPieceType.Corner || type == SetPieceType.FreeKick
                ? MatchPhase.SetPiece : MatchPhase.DeadBall;
            pendingPassTeam = -1;
            st.StoppageTicks += (uint)bal.setpiece.hazirlikTicks;

            int taker = PickTaker(ref st, type, forTeam);
            st.SetPieceTaker = (short)taker;
            if (taker >= 0)
            {
                st.Agents[taker].TargetX = st.Ball.X;
                st.Agents[taker].TargetY = st.Ball.Y;
                st.Agents[taker].ActionUntilTick = 0; // hazır olunca hemen karar verebilsin
            }
        }

        /// <summary>Kullanacak oyuncu: kale vuruşunda kaleci; diğerlerinde topa en yakın saha oyuncusu.</summary>
        int PickTaker(ref MatchState st, SetPieceType type, byte team)
        {
            if (type == SetPieceType.GoalKick) return team == 0 ? 0 : 11;
            int best = -1; long bd = long.MaxValue;
            int t0 = team == 0 ? 0 : 11;
            for (int i = t0; i < t0 + 11; i++)
            {
                if (!st.Agents[i].Active || i % 11 == 0) continue; // kaleci diğer duran topları kullanmaz
                long dx = st.Agents[i].X - st.Ball.X, dy = st.Agents[i].Y - st.Ball.Y;
                long d = dx * dx + dy * dy;
                if (d < bd) { bd = d; best = i; }
            }
            return best;
        }

        /// <summary>Korner ortası — ME 10.2 çözüm zincirinin başı: top hedef bölgeye HAVADAN uçar
        /// (Flight=4), inişte hava topu düellosu çözülür. Falso/bölge seçimi ince ayarı M-ileri.</summary>
        void ExecuteCross(ref MatchState st, int taker)
        {
            ref var a = ref st.Agents[taker];
            int gx = a.TeamIdx == 0 ? PitchHalfXmm : -PitchHalfXmm;
            int dirIn = a.TeamIdx == 0 ? -1 : 1;
            double tx = gx + dirIn * bal.setpiece.kornerHedefDerinlikM * 1000.0;
            // Hedef y: SetPieces niteliği sapmayı daraltır — PHYSICS domain (yürütme hatası)
            double ty = 3000.0 * Rng.Gauss01(seed, Domain.Physics, (uint)(600 + taker), st.Tick, 61)
                        * (1.0 - Eff(taker, attrs[taker].SetPieces) / 150.0);

            double dxM = (tx - a.X) / 1000.0, dyM = (ty - a.Y) / 1000.0;
            double dM = Math.Max(1.0, Math.Sqrt(dxM * dxM + dyM * dyM));
            double v = bal.setpiece.kornerOrtaHiziMS;
            double tF = dM / v;
            st.Ball.Vx = Units.QuantizeMm(dxM / tF);
            st.Ball.Vy = Units.QuantizeMm(dyM / tF);
            st.Ball.Vz = Units.QuantizeMm(0.5 * G * tF); // hedefte yere inecek balistik yay (ME 8.3)
            st.Ball.Z = 100;
            st.Ball.OwnerId = -1;
            st.Ball.LastTouchTeam = a.TeamIdx;
            st.Ball.Flight = 4;
            st.SetPiece = SetPieceType.None;
            st.SetPieceTaker = -1;
            st.Phase = MatchPhase.OpenPlay;
        }

        /// <summary>Hava topu düellosu — ME 6.4 kompoziti (0,4 Heading + 0,35 JumpReach + 0,25 Strength);
        /// kazanan hücumcu kafa şutu (9.2 kurtarış zinciriyle), savunmacı uzaklaştırma (ME 10.2).</summary>
        void ResolveAerial(ref MatchState st)
        {
            long r = (long)(bal.setpiece.havaTopuYaricapM * 1000);
            long r2 = r * r;
            int atk = -1, def = -1; long da = long.MaxValue, dd = long.MaxValue;
            byte crossTeam = st.Ball.LastTouchTeam;
            for (int i = 0; i < 22; i++)
            {
                if (!st.Agents[i].Active) continue;
                long dx = st.Agents[i].X - st.Ball.X, dy = st.Agents[i].Y - st.Ball.Y;
                long d = dx * dx + dy * dy;
                if (d > r2) continue;
                if (st.Agents[i].TeamIdx == crossTeam) { if (d < da) { da = d; atk = i; } }
                else if (d < dd) { dd = d; def = i; }
            }
            if (atk < 0 && def < 0) { st.Ball.Flight = 0; return; } // kimse yok: top serbest düşer

            bool attackerWins;
            if (atk < 0) attackerWins = false;
            else if (def < 0) attackerWins = true;
            else
            {
                double ae = Composite(atk, 0.4, attrs[atk].Heading, 0.35, attrs[atk].JumpReach, 0.25, attrs[atk].Strength);
                double de = Composite(def, 0.4, attrs[def].Heading, 0.35, attrs[def].JumpReach, 0.25, attrs[def].Strength);
                // Zar gerekçesi: hava topu ikili mücadelesi — DUEL domain (ME 6.3)
                attackerWins = DuelWin(ae, de, (uint)(700 + atk), st.Tick, 62);
            }

            st.Ball.Flight = 0;
            if (attackerWins)
            {
                st.Ball.OwnerId = -1;
                st.Ball.Z = 0;
                ExecuteShot(ref st, atk, header: true);
            }
            else
            {
                // Uzaklaştırma: kendi kalesinden UZAĞA sert vuruş (ikinci top kargaşası doğal doğar)
                ref var d2 = ref st.Agents[def];
                int away = d2.TeamIdx == 0 ? 1 : -1;
                st.Ball.Z = 0;
                st.Ball.Vx = Units.QuantizeMm(away * bal.setpiece.uzaklastirmaHizMS * 0.9);
                st.Ball.Vy = Units.QuantizeMm(bal.setpiece.uzaklastirmaHizMS * 0.3 *
                    Rng.Gauss01(seed, Domain.Physics, (uint)(700 + def), st.Tick, 63));
                st.Ball.Vz = 0;
                st.Ball.LastTouchTeam = d2.TeamIdx;
            }
        }

        /// <summary>Penaltı — ME 10.4 nişan/tahmin matrisi; SETPIECE domain, sıkıştırılmış çözüm.</summary>
        void AwardPenalty(ref MatchState st, byte forTeam)
        {
            Penalties++;
            st.StoppageTicks += (uint)bal.setpiece.hazirlikTicks;
            st.Phase = MatchPhase.Penalty;

            // Şutçu: takımın en iyi bitiricisi (sahada, kaleci hariç) — sabit tarama sırası
            int shooter = -1; int bestFin = -1;
            int t0 = forTeam == 0 ? 0 : 11;
            for (int i = t0; i < t0 + 11; i++)
            {
                if (!st.Agents[i].Active || i % 11 == 0) continue;
                int f = Eff(i, attrs[i].Finishing);
                if (f > bestFin) { bestFin = f; shooter = i; }
            }
            int gk = forTeam == 0 ? 11 : 0;
            if (shooter < 0) { RestartAfterMiss(ref st, forTeam); return; }

            var pc = bal.setpiece.penalty;
            // Şutçu yönü: Composure düşükse "güvenli orta" bias'ı artar (10.4)
            double comp = Eff(shooter, attrs[shooter].Composure) / 100.0;
            double pCenter = 0.30 - 0.18 * comp;
            double rs = Rng.Rand01(seed, Domain.SetPiece, (uint)(800 + shooter), st.Tick, 71);
            int aim = rs < pCenter ? 1 : (rs < pCenter + (1.0 - pCenter) * 0.5 ? 0 : 2); // 0 sol, 1 orta, 2 sağ
            // Kaleci tahmini: karma strateji (geçmiş penaltı ağırlıkları FAZ 04 hafızasıyla gelir)
            int guess = (int)(Rng.Rand01(seed, Domain.SetPiece, (uint)(800 + gk), st.Tick, 72) * 3);
            if (guess > 2) guess = 2;

            double pGoal = aim == 1 && guess == 1 ? pc.ortaOrta
                         : aim == guess ? pc.dogruTahmin : pc.yanlisTahmin;
            bool post = Rng.Rand01(seed, Domain.SetPiece, (uint)(800 + shooter), st.Tick, 73) < pc.direk;
            bool goal = !post && Rng.Rand01(seed, Domain.SetPiece, (uint)(800 + shooter), st.Tick, 74) < pGoal;

            Shots++;
            XgRecordPenalty(forTeam, pc.hedefOrtalama);
            if (goal)
            {
                if (forTeam == 0) st.HomeGoals++; else st.AwayGoals++;
                KickoffRestart(ref st, startTeam: forTeam == 0 ? (byte)1 : (byte)0);
            }
            else
            {
                if (!post) Saves++;
                RestartAfterMiss(ref st, forTeam);
            }
        }

        void XgRecordPenalty(byte team, double xg)
        { if (team == 0) XgHome += xg; else XgAway += xg; }

        /// <summary>Penaltı kaçtı/kurtarıldı → savunan takıma kale vuruşu (kornere çelme M-ileri).</summary>
        void RestartAfterMiss(ref MatchState st, byte attackTeam)
        {
            byte def = attackTeam == 0 ? (byte)1 : (byte)0;
            int goalX = def == 0 ? -PitchHalfXmm : PitchHalfXmm;
            int inward = def == 0 ? 5500 : -5500;
            GoalKicks++;
            AwardSetPiece(ref st, SetPieceType.GoalKick, def, goalX + inward, 0);
        }

        /// <summary>Gol sonrası santra — yiyen takım başlatır (tam santra sahnesi M-duran-top).</summary>
        void KickoffRestart(ref MatchState st, byte startTeam)
        {
            st.Ball.X = 0; st.Ball.Y = 0; st.Ball.Z = 0;
            st.Ball.Vx = st.Ball.Vy = st.Ball.Vz = 0;
            st.Ball.OwnerId = -1;
            st.Ball.LastTouchTeam = startTeam;
            st.Ball.Flight = 3;                 // ölü top kilidi: yalnız başlayan takım alır
            st.SetPiece = SetPieceType.Kickoff;
            st.SetPieceTeam = startTeam;
            st.Phase = MatchPhase.Kickoff;
            pendingPassTeam = -1;
            int fw = startTeam == 0 ? 10 : 21;
            st.SetPieceTaker = (short)fw;
            st.Agents[fw].TargetX = 0; st.Agents[fw].TargetY = 0;
            st.Agents[fw].ActionUntilTick = 0;
        }

        // ---------------------------------------------------------------- yardımcılar

        /// <summary>A_eff — ME 6.2. Enerji ve momentum tick başında önbelleğe alınır: Eff() derin
        /// çağrı yollarında state'e erişemez ve `ref struct` geçişi sıcak yolu kirletirdi.
        /// Önbellek her tick'in başında tek yerden tazelenir → determinizm korunur (M5).
        /// Hafif sakatlık nitelikleri −5 düşürür (ME 12.2 tablosu).</summary>
        int Eff(int i, byte baseVal)
        {
            int b = baseVal - injuryPenalty[i];
            if (b < 1) b = 1;
            return EffectiveAttributes.Compute((byte)b, energyCache[i], momentumCache[i], luts);
        }

        readonly ushort[] energyCache = new ushort[22];
        readonly sbyte[] momentumCache = new sbyte[22];
        readonly byte[] injuryPenalty = new byte[22];

        void RefreshStateCache(ref MatchState st)
        {
            for (int i = 0; i < 22; i++)
            {
                energyCache[i] = st.Agents[i].Energy;
                momentumCache[i] = st.Agents[i].TeamIdx == 0 ? st.HomeRt.Momentum : st.AwayRt.Momentum;
                injuryPenalty[i] = st.Agents[i].Injury == InjuryState.Hafif ? (byte)5 : (byte)0;
            }
        }

        double Composite(int i, double w1, byte a1, double w2, byte a2, double w3, byte a3) =>
            w1 * Eff(i, a1) + w2 * Eff(i, a2) + w3 * Eff(i, a3);

        /// <summary>Genel düello — ME 6.3: P = clamp(pTaban + k×margin/100, min, max).
        /// Chaos gürültüsü M-chaos diliminde eklenir (sigma tablosu 13.2).</summary>
        bool DuelWin(double atkEff, double defEff, uint entity, uint tick, uint salt, double pTaban = -1)
        {
            double p = (pTaban < 0 ? bal.duel.pTabanDefault : pTaban) + bal.duel.kDuel * (atkEff - defEff) / 100.0;
            if (p < bal.duel.clampMin) p = bal.duel.clampMin;
            if (p > bal.duel.clampMax) p = bal.duel.clampMax;
            return Rng.Rand01(seed, Domain.Duel, entity, tick, salt) < p;
        }

        /// <summary>Takım içinde topa yakınlık sırası (0 = en yakın); eşitlik düşük indeksle
        /// bozulur — deterministik (ME 3.2 sıralı güncelleme kuralıyla uyumlu).</summary>
        int NearestRankToBall(ref MatchState st, int i)
        {
            long dx = st.Agents[i].X - st.Ball.X, dy = st.Agents[i].Y - st.Ball.Y;
            long mine = dx * dx + dy * dy;
            int rank = 0;
            for (int j = 0; j < 22; j++)
            {
                if (j == i || j % 11 == 0 || st.Agents[j].TeamIdx != st.Agents[i].TeamIdx || !st.Agents[j].Active) continue; // kaleci pres sayımı dışı
                long ox = st.Agents[j].X - st.Ball.X, oy = st.Agents[j].Y - st.Ball.Y;
                long other = ox * ox + oy * oy;
                if (other < mine || (other == mine && j < i)) rank++;
            }
            return rank;
        }

        /// <summary>Taşıyıcının ÖNÜNDEKİ (kale yönünde) en yakın rakip — dribling düellosu markörü.</summary>
        int NearestOpponentInFront(ref MatchState st, int carrier, int radiusMm)
        {
            ref var a = ref st.Agents[carrier];
            int dir = a.TeamIdx == 0 ? 1 : -1;
            long r2 = (long)radiusMm * radiusMm;
            int best = -1; long bd = long.MaxValue;
            for (int i = 0; i < 22; i++)
            {
                if (!st.Agents[i].Active || st.Agents[i].TeamIdx == a.TeamIdx) continue;
                long dx = st.Agents[i].X - a.X, dy = st.Agents[i].Y - a.Y;
                if (dx * dir < -500) continue;             // arkada kalan savunucu engel değildir
                long d = dx * dx + dy * dy;
                if (d > r2 || d >= bd) continue;
                bd = d; best = i;
            }
            return best;
        }

        /// <summary>Ofsayt çizgisi — ME 10.5/7.4: sondan İKİNCİ savunucu ile topun ilerideki olanı.
        /// (Son savunucu genelde kalecidir; kural gereği ikinci savunucu esas alınır.)</summary>
        int OffsideLineX(ref MatchState st, byte attackingTeam)
        {
            int d0 = attackingTeam == 0 ? 11 : 0;
            int first = int.MinValue, second = int.MinValue; // team 0 için "ileri" = büyük x
            for (int i = d0; i < d0 + 11; i++)
            {
                if (!st.Agents[i].Active) continue;
                int v = attackingTeam == 0 ? st.Agents[i].X : -st.Agents[i].X;
                if (v > first) { second = first; first = v; }
                else if (v > second) second = v;
            }
            if (second == int.MinValue) second = first;
            int lineSigned = second;
            int ballSigned = attackingTeam == 0 ? st.Ball.X : -st.Ball.X;
            if (ballSigned > lineSigned) lineSigned = ballSigned;
            return attackingTeam == 0 ? lineSigned : -lineSigned;
        }

        int NearOpponents(ref MatchState st, int x, int y, byte team) =>
            NearOpponents(ref st, x, y, team, (int)(bal.pass.presYaricapM * 1000));

        int NearOpponents(ref MatchState st, int x, int y, byte team, int radiusMm)
        {
            long r2 = (long)radiusMm * radiusMm;
            int n = 0;
            for (int i = 0; i < 22; i++)
            {
                if (st.Agents[i].TeamIdx == team || !st.Agents[i].Active) continue;
                long dx = st.Agents[i].X - x, dy = st.Agents[i].Y - y;
                if (dx * dx + dy * dy <= r2) n++;
            }
            return n;
        }

        /// <summary>Pas koridorundaki rakip sayısı — hızlı P_kayıp tahmini (ME 7.2; tam kesişim
        /// taraması 6.5'in fiziksel akışında EMERGENT olarak zaten yaşar).</summary>
        /// <summary>Şut koridorundaki en yakın rakip (blokçu) — yoksa -1.</summary>
        int NearestCorridorOpponent(ref MatchState st, int x1, int y1, int x2, int y2, byte team)
        {
            double dx = x2 - x1, dy = y2 - y1;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1) return -1;
            int best = -1; double bt = double.MaxValue;
            for (int i = 0; i < 22; i++)
            {
                if (st.Agents[i].TeamIdx == team || !st.Agents[i].Active) continue;
                double px = st.Agents[i].X - x1, py = st.Agents[i].Y - y1;
                double t = (px * dx + py * dy) / (len * len);
                if (t < 0.05 || t > 0.95) continue;
                double ex = px - t * dx, ey = py - t * dy;
                if (ex * ex + ey * ey > 2000.0 * 2000.0) continue;
                if (t < bt) { bt = t; best = i; }
            }
            return best;
        }

        int CorridorOpponents(ref MatchState st, int x1, int y1, int x2, int y2, byte team)
        {
            double dx = x2 - x1, dy = y2 - y1;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1) return 0;
            int n = 0;
            for (int i = 0; i < 22; i++)
            {
                if (st.Agents[i].TeamIdx == team || !st.Agents[i].Active) continue;
                double px = st.Agents[i].X - x1, py = st.Agents[i].Y - y1;
                double t = (px * dx + py * dy) / (len * len);
                if (t < 0.05 || t > 0.95) continue;
                double ex = px - t * dx, ey = py - t * dy;
                if (ex * ex + ey * ey <= 2000.0 * 2000.0) n++; // koridor yarı genişliği 2 m
            }
            return n;
        }

        /// <summary>xT okuması — 12×8 ayrıştırılabilir tablo [KALİBRE xt.*]; deplasman aynalanır.</summary>
        double XtAt(int xMm, int yMm, byte team)
        {
            int col = (int)((long)(xMm + PitchHalfXmm) * 12 / (PitchHalfXmm * 2L + 1));
            if (col < 0) col = 0; else if (col > 11) col = 11;
            if (team == 1) col = 11 - col;
            int row = (int)((long)(yMm + PitchHalfYmm) * 8 / (PitchHalfYmm * 2L + 1));
            if (row < 0) row = 0; else if (row > 7) row = 7;
            return bal.xt.kolon[col] * bal.xt.satir[row];
        }

        static int ClampX(int v) => v < -PitchHalfXmm ? -PitchHalfXmm : (v > PitchHalfXmm ? PitchHalfXmm : v);
        static int ClampY(int v) => v < -PitchHalfYmm ? -PitchHalfYmm : (v > PitchHalfYmm ? PitchHalfYmm : v);

        /// <summary>Kanonik durum hash'i — xxHash64 (ME 3.2). Alan sırası SABİT sözleşme; alan
        /// ekleyen dilim burayı ve golden'ı BİRLİKTE günceller (M2: Target çifti + LastTouchTeam).</summary>
        public static ulong StateHash(in MatchState st)
        {
            Span<byte> buf = stackalloc byte[1280];
            int o = 0;
            W32(buf, ref o, st.Tick);
            buf[o++] = (byte)st.Phase;
            W32(buf, ref o, (uint)st.HomeGoals);
            W32(buf, ref o, (uint)st.AwayGoals);

            W32(buf, ref o, (uint)st.Ball.X); W32(buf, ref o, (uint)st.Ball.Y); W32(buf, ref o, (uint)st.Ball.Z);
            W32(buf, ref o, (uint)st.Ball.Vx); W32(buf, ref o, (uint)st.Ball.Vy); W32(buf, ref o, (uint)st.Ball.Vz);
            W32(buf, ref o, (uint)st.Ball.SpinY);
            W16(buf, ref o, (ushort)st.Ball.OwnerId);
            buf[o++] = st.Ball.LastTouchTeam;
            buf[o++] = st.Ball.Flight;
            buf[o++] = (byte)st.SetPiece;
            buf[o++] = st.SetPieceTeam;
            W16(buf, ref o, (ushort)st.SetPieceTaker);
            W32(buf, ref o, st.StoppageTicks);
            buf[o++] = st.Half;
            W32(buf, ref o, st.HalfEndTick);

            for (int i = 0; i < st.Agents.Length; i++)
            {
                ref readonly var a = ref st.Agents[i];
                W16(buf, ref o, (ushort)a.Id);
                buf[o++] = a.TeamIdx;
                buf[o++] = a.RoleId;
                W32(buf, ref o, (uint)a.X); W32(buf, ref o, (uint)a.Y);
                W32(buf, ref o, (uint)a.Vx); W32(buf, ref o, (uint)a.Vy);
                W32(buf, ref o, (uint)a.AnchorX); W32(buf, ref o, (uint)a.AnchorY);
                W32(buf, ref o, (uint)a.TargetX); W32(buf, ref o, (uint)a.TargetY);
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
