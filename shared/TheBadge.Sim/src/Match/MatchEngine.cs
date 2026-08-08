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
        public const int PitchHalfXmm = 52500;            // 105×68 m saha, merkez orijin (mimari sabit)
        public const int PitchHalfYmm = 34000;            // ev sahibi +x yönüne hücum eder
        const double Dt = TickMs / 1000.0;
        const double G = 9.81;                            // yerçekimi (ME 8.3 — fizik sabiti)

        readonly ulong seed;
        readonly CommandQueue queue;
        readonly SimBalance bal;
        readonly AttributeLuts luts;
        readonly PlayerAttributes[] attrs = new PlayerAttributes[22];

        public const int GoalHalfWidthMm = 3660;  // kale 7,32 m — direkler y ±3660 (fiziksel sabit)
        public const int GoalHeightMm = 2440;     // üst direk 2,44 m

        // Tanı sayaçları — event log (ME 15) gelene dek dev ekranı/Checks tüketir.
        // Duruma ve hash'e GİRMEZLER; davranışı etkilemezler.
        public int PassAttempts, PassCompletions, Tackles, OutOfBounds, PossessionChanges;
        public int Shots, Saves;
        public double XgHome, XgAway; // xG KAYIT gerçeği (ME 15.2) — sonuç üretimine girmez
        int pendingPassTeam = -1; // pas sonrası ilk kontrol aynı takımsa tamamlanmış sayılır

        /// <summary>Rng kökü — replay dörtlüsü üyesi (ME 3.3).</summary>
        public ulong Seed => seed;

        public MatchEngine(ulong seed, CommandQueue queue, MatchConfig cfg, SimBalance balance)
        {
            this.seed = seed;
            this.queue = queue ?? throw new ArgumentNullException(nameof(queue));
            bal = balance ?? throw new ArgumentNullException(nameof(balance));
            luts = AttributeLuts.Build(balance);
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
            s.Agents[10].X = -600; s.Agents[10].Y = 0; // santra forveti topun başında (kontrol yarıçapı içi)
            s.Agents[10].TargetX = -600; s.Agents[10].TargetY = 0;
            return s;
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
                if (a.SentOff) continue;
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
                if (j == i || st.Agents[j].SentOff) continue;
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

            // Zar gerekçesi: karar gürültüsü DECISION domain'i — "yanlış tercih" chaos'u (ME 7.2/13.2)
            double Noise(uint salt) =>
                bal.chaos.decisionSigma.orta * Rng.Gauss01(seed, Domain.Decision, (uint)(100 + agentId), tick, salt);

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
                    double proxy = closeness * closeness * (0.4 + 0.6 * central) * (0.5 + 0.5 * fin);
                    double s3 = u.wThreat * u.sutTehditCarpan * proxy
                                + u.wRisk * proxy + u.wVar * Noise(14);
                    if (s3 > best) { best = s3; bestKind = 3; bestTarget = -1; }
                }
            }

            if (bestKind == 3) ExecuteShot(ref st, i);
            else if (bestKind == 2) ExecutePass(ref st, i, bestTarget);
            else if (bestKind == 1)
            {
                int dir = a.TeamIdx == 0 ? 1 : -1;
                a.TargetX = ClampX(a.X + dir * (int)(u.dribbleIleriM * 1000));
                a.TargetY = a.Y;
            }
            else { a.TargetX = a.X; a.TargetY = a.Y; }
            a.ActionUntilTick = st.Tick + (uint)u.kararKilidiTicks;
        }

        /// <summary>Pas — ME 6.5 çekirdeği: menzil ters formülüyle güç (8.2), nişan hatası sigma;
        /// kesişim EMERGENT'tır (koridor rakipleri fiziksel olarak topa ulaşabilir).</summary>
        void ExecutePass(ref MatchState st, int i, int j)
        {
            ref var a = ref st.Agents[i];
            double dxM = (st.Agents[j].X - a.X) / 1000.0, dyM = (st.Agents[j].Y - a.Y) / 1000.0;
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
            // Alıcı topu karşılamaya koşar
            st.Agents[j].TargetX = ClampX(st.Ball.X + (int)(dxM * 400));
            st.Agents[j].TargetY = ClampY(st.Ball.Y + (int)(dyM * 400));
        }

        /// <summary>Şut — ME 6.4/8.3 + kurtarış ANALİTİK ön-çözümü (9.2): sonuç topun gerçek
        /// uçuşuyla sahnelenir (gol → çizgiyi geçer; tut → kaleciye uçar; çeldi → dışa sapar).
        /// Kayıt xG'si 15.2 formülüyle AYNEN hesaplanır (ln/atan yalnız kayıtta — sonuca girmez).</summary>
        void ExecuteShot(ref MatchState st, int i)
        {
            ref var a = ref st.Agents[i];
            int gx = a.TeamIdx == 0 ? PitchHalfXmm : -PitchHalfXmm;
            double dxM = (gx - a.X) / 1000.0, dyM = (0 - a.Y) / 1000.0;
            double dGoal = Math.Sqrt(dxM * dxM + dyM * dyM);
            if (dGoal < 1.0) return;

            // Nişan: kale düzleminde hedef y — Finishing kompoziti sigma'yı daraltır
            double fin = Composite(i, 0.55, attrs[i].Finishing, 0.25, attrs[i].Composure, 0.2, attrs[i].FirstTouch);
            double sigmaM = bal.shotExec.sutSigmaTabanM * (1.0 - fin / 125.0)
                            * (1.0 + dGoal * bal.shotExec.sutSigmaMesafePerM);
            // Zar gerekçesi: şut yürütme hatası fizikseldir — PHYSICS domain (ME 3.1)
            double aimY = sigmaM * 1000.0 * Rng.Gauss01(seed, Domain.Physics, (uint)(200 + i), st.Tick, 41);

            double planeDx = (gx - a.X) / 1000.0;
            double tPlane = Math.Abs(planeDx) / bal.shotExec.sutHiziMS;
            double interY = aimY; // kale düzleminde kesişim: merkeze (y=0) nişan + sapma

            Shots++;
            RecordXg(ref st, i, dGoal);

            bool insidePosts = Math.Abs(interY) <= GoalHalfWidthMm;
            double vy = ((interY - a.Y) / 1000.0) / tPlane;
            double vx = planeDx / tPlane;
            byte flight = insidePosts ? (byte)1 : (byte)0; // karara bağlı gol yolu / dışarı serbest

            if (insidePosts)
            {
                // 9.2 analitik kurtarış — kaleci: rakip takım slot 0
                int gk = a.TeamIdx == 0 ? 11 : 0;
                double tReact = bal.gk.tReactBase + (100 - Eff(gk, attrs[gk].Reflexes)) * bal.gk.tReactPerReflexEksik;
                double reach = bal.gk.reachBase + Eff(gk, attrs[gk].Agility) / 100.0 * bal.gk.reachAgilityFactor;
                double gkDist = Math.Abs(st.Agents[gk].Y - interY) / 1000.0;
                double tTraverse = gkDist / (reach / bal.gk.dalisSureCarpan);
                double marj = tPlane - (tReact + tTraverse);
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
                        // Çeldi: dışa sapar, top SERBEST — köşe-önü tehlike (korner sahnesi M-duran-top)
                        int sgn = interY >= 0 ? 1 : -1;
                        int ang = TrigLut.AngleIndexFromRad(sgn * bal.gk.cildirmaAcisiDeg * Math.PI / 180.0);
                        TrigLut.Rotate(vx, vy, ang, out vx, out vy);
                        flight = 0;
                    }
                }
                // kurtaramadı → hız aynen: top çizgiyi direkler arasından geçer → EventAndState GOL sayar
            }
            // dışarı nişan → aut/degaj restart'ı doğal akışta

            st.Ball.Vx = Units.QuantizeMm(vx);
            st.Ball.Vy = Units.QuantizeMm(vy);
            st.Ball.Vz = 0; // alçak/sert şut — yüksek şut ve direk bandı M-duran-top/ince ayar
            st.Ball.OwnerId = -1;
            st.Ball.LastTouchTeam = a.TeamIdx;
            st.Ball.Flight = flight;
            pendingPassTeam = -1;
        }

        /// <summary>xG KAYIT gerçeği — ME 15.2 birebir (ln/atan burada serbest: sonuca girmez).</summary>
        void RecordXg(ref MatchState st, int i, double dGoal)
        {
            ref var a = ref st.Agents[i];
            int gx = a.TeamIdx == 0 ? PitchHalfXmm : -PitchHalfXmm;
            double p1x = (gx - a.X) / 1000.0, p1y = (GoalHalfWidthMm - a.Y) / 1000.0;
            double p2y = (-GoalHalfWidthMm - a.Y) / 1000.0;
            double ang = Math.Abs(Math.Atan2(p1y, Math.Abs(p1x)) - Math.Atan2(p2y, Math.Abs(p1x)));
            int pres = Math.Min(3, NearOpponents(ref st, a.X, a.Y, a.TeamIdx, 1200));
            var g = bal.shot.xg;
            double z = g.b0 + g.bLnDist * Math.Log(Math.Max(1.0, dGoal) / 10.0) + g.bAngle * ang + g.bPres * pres;
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

            // Anchor OMURGA (ME 7.4: w_anchor en yüksek tekil ağırlık): taban = anchor + faz ofseti,
            // üstüne wTop oranında top çekimi — kalan vektörler (boşluk/markaj/ofsayt) M-karar ilerisi
            double bx = a.AnchorX + (attacking ? dir * o.fazIleriM * 1000.0 : -dir * o.savunmaCekilmeM * 1000.0);
            double by = a.AnchorY;
            double tx = bx + o.wTop * (st.Ball.X - bx);
            double ty = by + o.wTop * (st.Ball.Y - by);

            a.TargetX = ClampX(Units.QuantizeMm(tx / 1000.0));
            a.TargetY = ClampY(Units.QuantizeMm(ty / 1000.0));
        }

        // ---------------------------------------------------------------- aksiyon çözümü (ME 4.3, 6.3-6.4)

        void ActionResolutionPass(ref MatchState st)
        {
            // 1) Serbest top kontrolü — ilk ulaşan alır; aynı tick'te iki aday → kontrol düellosu (4.3)
            // M3: karara bağlanmış şut uçuşu (Flight=1) ALINAMAZ — 9.2 çözümü sahneleniyor;
            // tutuş uçuşunu (Flight=2) yalnız savunan kaleci alır
            if (st.Ball.OwnerId < 0 && st.Ball.Z < 400 && st.Ball.Flight != 1)
            {
                int c1 = -1, c2 = -1; long d1 = long.MaxValue, d2 = long.MaxValue;
                long r2 = (long)(bal.possession.kontrolYaricapM * 1000) * (long)(bal.possession.kontrolYaricapM * 1000);
                int onlyGk = st.Ball.Flight == 2 ? (st.Ball.LastTouchTeam == 0 ? 11 : 0) : -1;
                for (int i = 0; i < 22; i++) // sıra sabit
                {
                    if (st.Agents[i].SentOff) continue;
                    if (onlyGk >= 0 && i != onlyGk) continue;
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
                    if (d.SentOff || d.TeamIdx == c.TeamIdx || st.Tick < d.ActionUntilTick) continue;
                    long dx = d.X - c.X, dy = d.Y - c.Y;
                    if (dx * dx + dy * dy > r2) continue;

                    double atk = Composite(i, 0.6, attrs[i].Tackling, 0.25, attrs[i].Positioning, 0.15, attrs[i].Strength);
                    double def = Composite(st.Ball.OwnerId, 0.5, attrs[st.Ball.OwnerId].Dribbling, 0.3, attrs[st.Ball.OwnerId].Agility, 0.2, attrs[st.Ball.OwnerId].Strength);
                    d.ActionUntilTick = st.Tick + (uint)bal.possession.tackleCooldownTicks;
                    // Zar gerekçesi: top kapma düellosu — DUEL domain (ME 6.3-6.4); foul üretimi M-hakem
                    if (DuelWin(atk, def, (uint)(300 + i), st.Tick, 32))
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
                    break;
                }
            }
        }

        void ClaimBall(ref MatchState st, int i)
        {
            ref var a = ref st.Agents[i];
            if (st.Ball.OwnerId == a.Id) return;
            st.Ball.OwnerId = a.Id;
            st.Ball.Flight = 0;
            st.Ball.Vx = a.Vx; st.Ball.Vy = a.Vy; st.Ball.Vz = 0; st.Ball.Z = 0;
            if (pendingPassTeam >= 0)
            {
                if (pendingPassTeam == a.TeamIdx) PassCompletions++;
                pendingPassTeam = -1;
            }
            if (st.Ball.LastTouchTeam != 2 && st.Ball.LastTouchTeam != a.TeamIdx) PossessionChanges++;
            st.Ball.LastTouchTeam = a.TeamIdx;
            if (st.Phase == MatchPhase.Kickoff || st.Phase == MatchPhase.DeadBall)
                st.Phase = MatchPhase.OpenPlay;
        }

        // ---------------------------------------------------------------- fizik (ME 8)

        void PhysicsPass(ref MatchState st)
        {
            // Ajanlar — sıra sabit; ivme sınırlı hedef takibi (ME 8.1); dönüş/Agility incelikleri M-ileri
            for (int i = 0; i < 22; i++)
            {
                ref var a = ref st.Agents[i];
                if (a.SentOff) continue;

                double vMax = bal.move.vMaxBase + bal.move.vMaxPaceSpan * Eff(i, attrs[i].Pace) / 100.0;
                if (st.Ball.OwnerId == a.Id)
                    vMax *= bal.move.dribbleCarpanBase + bal.move.dribbleCarpanPerPuan * Eff(i, attrs[i].Dribbling);
                double aMax = bal.move.aMaxBase + bal.move.aMaxAccelSpan * Eff(i, attrs[i].Acceleration) / 100.0;

                double dxM = (a.TargetX - a.X) / 1000.0, dyM = (a.TargetY - a.Y) / 1000.0;
                double dist = Math.Sqrt(dxM * dxM + dyM * dyM);
                double desX = 0, desY = 0;
                if (dist > 0.05)
                {
                    double sp = Math.Min(vMax, dist / Dt * 0.5); // varışta yavaşlama
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

        // ---------------------------------------------------------------- durum/sınır (M2)

        void EventAndStatePass(ref MatchState st)
        {
            // Çizgi geçişleri: GOL (direkler arası, üst direk altı) ya da taç/aut restart'ı (M2/M3)
            if (st.Ball.OwnerId < 0 &&
                (Math.Abs(st.Ball.X) > PitchHalfXmm || Math.Abs(st.Ball.Y) > PitchHalfYmm))
            {
                bool goal = Math.Abs(st.Ball.X) > PitchHalfXmm &&
                            Math.Abs(st.Ball.Y) <= GoalHalfWidthMm && st.Ball.Z <= GoalHeightMm;
                if (goal)
                {
                    byte scorer = st.Ball.X > 0 ? (byte)0 : (byte)1; // +x çizgisi = ev hücum yönü
                    if (scorer == 0) st.HomeGoals++; else st.AwayGoals++;
                    KickoffRestart(ref st, startTeam: scorer == 0 ? (byte)1 : (byte)0);
                    st.Tick++;
                    if (st.Tick % ChecksumCadenceTicks == 0) st.LastChecksum = StateHash(in st);
                    return;
                }
                OutOfBounds++;
                pendingPassTeam = -1;
                byte toTeam = st.Ball.LastTouchTeam == 0 ? (byte)1 : (byte)0;
                st.Ball.X = ClampX(st.Ball.X); st.Ball.Y = ClampY(st.Ball.Y); st.Ball.Z = 0;
                st.Ball.Vx = st.Ball.Vy = st.Ball.Vz = 0;
                st.Ball.Flight = 0;
                st.Phase = MatchPhase.DeadBall;
                // Kullanan takımın en yakını topa yönlendirilir (ışınlama YOK — yürür, ME 4.1)
                int nearest = -1; long nd = long.MaxValue;
                for (int i = 0; i < 22; i++)
                {
                    if (st.Agents[i].TeamIdx != toTeam || st.Agents[i].SentOff) continue;
                    long dx = st.Agents[i].X - st.Ball.X, dy = st.Agents[i].Y - st.Ball.Y;
                    long dd = dx * dx + dy * dy;
                    if (dd < nd) { nd = dd; nearest = i; }
                }
                if (nearest >= 0)
                {
                    st.Agents[nearest].TargetX = st.Ball.X;
                    st.Agents[nearest].TargetY = st.Ball.Y;
                }
                st.Ball.LastTouchTeam = toTeam; // kullanım hakkı
            }

            st.Tick++;
            if (st.Tick % ChecksumCadenceTicks == 0)
                st.LastChecksum = StateHash(in st);
        }

        /// <summary>Gol sonrası santra — yiyen takım başlatır (tam santra sahnesi M-duran-top).</summary>
        void KickoffRestart(ref MatchState st, byte startTeam)
        {
            st.Ball.X = 0; st.Ball.Y = 0; st.Ball.Z = 0;
            st.Ball.Vx = st.Ball.Vy = st.Ball.Vz = 0;
            st.Ball.OwnerId = -1;
            st.Ball.LastTouchTeam = startTeam;
            st.Ball.Flight = 0;
            st.Phase = MatchPhase.Kickoff;
            pendingPassTeam = -1;
            int fw = startTeam == 0 ? 10 : 21;
            st.Agents[fw].TargetX = 0; st.Agents[fw].TargetY = 0;
        }

        // ---------------------------------------------------------------- yardımcılar

        int Eff(int i, byte baseVal) =>
            EffectiveAttributes.Compute(baseVal, st_energy(i), st_momentum(i), luts);
        // M2: enerji drenajı ve momentum M-durum diliminde canlanır; şimdilik kurulum değerleri
        ushort st_energy(int i) => 1000;
        sbyte st_momentum(int i) => 0;

        double Composite(int i, double w1, byte a1, double w2, byte a2, double w3, byte a3) =>
            w1 * Eff(i, a1) + w2 * Eff(i, a2) + w3 * Eff(i, a3);

        /// <summary>Genel düello — ME 6.3: P = clamp(pTaban + k×margin/100, min, max).
        /// Chaos gürültüsü M-chaos diliminde eklenir (sigma tablosu 13.2).</summary>
        bool DuelWin(double atkEff, double defEff, uint entity, uint tick, uint salt)
        {
            double p = bal.duel.pTabanDefault + bal.duel.kDuel * (atkEff - defEff) / 100.0;
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
                if (j == i || j % 11 == 0 || st.Agents[j].TeamIdx != st.Agents[i].TeamIdx || st.Agents[j].SentOff) continue; // kaleci pres sayımı dışı
                long ox = st.Agents[j].X - st.Ball.X, oy = st.Agents[j].Y - st.Ball.Y;
                long other = ox * ox + oy * oy;
                if (other < mine || (other == mine && j < i)) rank++;
            }
            return rank;
        }

        int NearOpponents(ref MatchState st, int x, int y, byte team) =>
            NearOpponents(ref st, x, y, team, (int)(bal.pass.presYaricapM * 1000));

        int NearOpponents(ref MatchState st, int x, int y, byte team, int radiusMm)
        {
            long r2 = (long)radiusMm * radiusMm;
            int n = 0;
            for (int i = 0; i < 22; i++)
            {
                if (st.Agents[i].TeamIdx == team || st.Agents[i].SentOff) continue;
                long dx = st.Agents[i].X - x, dy = st.Agents[i].Y - y;
                if (dx * dx + dy * dy <= r2) n++;
            }
            return n;
        }

        /// <summary>Pas koridorundaki rakip sayısı — hızlı P_kayıp tahmini (ME 7.2; tam kesişim
        /// taraması 6.5'in fiziksel akışında EMERGENT olarak zaten yaşar).</summary>
        int CorridorOpponents(ref MatchState st, int x1, int y1, int x2, int y2, byte team)
        {
            double dx = x2 - x1, dy = y2 - y1;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1) return 0;
            int n = 0;
            for (int i = 0; i < 22; i++)
            {
                if (st.Agents[i].TeamIdx == team || st.Agents[i].SentOff) continue;
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
