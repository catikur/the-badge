using System;
using System.Collections.Generic;
using TheBadge.Sim.Determinism;

namespace TheBadge.Greybox.Sim
{
    /// <summary>
    /// Greybox akış simülasyonu — Brif K2: "izlenebilirlik HİSSİ" prototipi.
    /// ME Spec'in maç motoru DEĞİLDİR; LOD/duello/fizik modelleri bilinçli olarak yoktur.
    /// UnityEngine'e referans vermez → headless derlenip pacing kanıtı üretilebilir.
    ///
    /// Zaman modeli: pozisyonlar GERÇEK saniyede ilerler (futbol hızında hareket),
    /// maç saati ise sıkıştırılmıştır: 90 dk = clock.macSuresiSaniye aktif saniye.
    /// Rastgelelik TheBadge.Sim.Rng iledir (Brif K5 serbestisi); domain seçimleri
    /// greybox bağlamında kozmetik akıştır, ME Spec 3.1 domain disiplini FAZ 03 borcudur.
    /// </summary>
    public sealed class FlowSim
    {
        public const float PitchW = 68f;
        public const float PitchL = 105f;
        const float MaxStepSn = 0.05f;   // entegrasyon dilimi: hız/skip'ten bağımsız kararlı hareket

        readonly GreyboxBalance bal;
        readonly GreyboxBalance.TacticCfg[] tacticByTeam = new GreyboxBalance.TacticCfg[2];
        readonly float[] strengthByTeam = new float[2];
        readonly float[][][] anchorsByTeam = new float[2][][];
        readonly ulong seed;

        // --- durum ---
        public FlowPhase Phase { get; private set; }
        public int HomeScore { get; private set; }
        public int AwayScore { get; private set; }
        public MatchStats Stats { get; } = new MatchStats();
        public int Possession => possession;
        public Vec2 BallPos => ballPos;
        public bool IsFinished => Phase == FlowPhase.FullTime;
        public int Half => half;

        /// <summary>Skip hedefi: şut/korner/gol içeren "önemli an" penceresi (Brif K2 hız kontrolleri).</summary>
        public bool InKeyMoment =>
            Phase == FlowPhase.ChanceBuild || Phase == FlowPhase.ShotTravel ||
            Phase == FlowPhase.CornerSetup || Phase == FlowPhase.CornerCross ||
            Phase == FlowPhase.GoalCelebration;

        /// <summary>Maç dakikası (gösterim; 90 üstü yalnız son pozisyon taşmasıdır).</summary>
        public float MatchMinute => Math.Min(94f, activeSeconds / bal.clock.macSuresiSaniye * 90f);

        float activeSeconds;          // yalnız aktif oyun fazlarında ilerler
        int half = 1;
        int possession;               // 0 ev, 1 deplasman
        float phaseTimer;             // bekleme fazları geri sayımı
        float dwellTimer;             // açık oyunda karar arası bekleme
        int chanceActions;            // ChanceBuild içindeki hızlı aksiyon sayacı
        int pendingShotOutcome = -1;  // 0 gol, 1 kurtarış, 2 dışarı, 3 korner sekmesi
        bool pendingLongBallRisk;
        int lastScorerTeam = -1;
        float momentum;               // + ev sahibi lehine, [-1, 1]

        Vec2 ballPos, ballTarget;
        float ballSpeed;
        bool ballMoving;

        readonly PlayerDot[] players = new PlayerDot[22];
        readonly Vec2[] wanderOffset = new Vec2[22];

        uint decisionTick;            // karar başına artar (Rng adresi)
        uint noiseTick;               // periyodik gürültü örneklemesi (momentum/wander)
        float noiseAccum;

        readonly Queue<FlowEvent> events = new Queue<FlowEvent>(32);

        public FlowSim(GreyboxBalance balance, MatchSetup matchSetup)
        {
            bal = balance;
            seed = matchSetup.Seed;
            tacticByTeam[0] = FindTactic(matchSetup.HomeTacticId);
            tacticByTeam[1] = FindTactic(matchSetup.AwayTacticId);
            strengthByTeam[0] = matchSetup.HomeStrength;
            strengthByTeam[1] = matchSetup.AwayStrength;
            anchorsByTeam[0] = Formations.Get(tacticByTeam[0].formasyon);
            anchorsByTeam[1] = Formations.Get(tacticByTeam[1].formasyon);

            for (int t = 0; t < 2; t++)
                for (int i = 0; i < 11; i++)
                {
                    int idx = t * 11 + i;
                    players[idx].Team = t;
                    players[idx].IsKeeper = i == 0;
                    players[idx].Pos = AnchorWorld(t, i);
                    players[idx].Target = players[idx].Pos;
                }

            BeginKickOff(0, FlowEventType.KickOff);
        }

        GreyboxBalance.TacticCfg FindTactic(int id)
        {
            for (int i = 0; i < bal.taktikler.Length; i++)
                if (bal.taktikler[i].id == id) return bal.taktikler[i];
            return bal.taktikler[0];
        }

        public PlayerDot GetPlayer(int i) => players[i];

        public bool TryDequeueEvent(out FlowEvent e)
        {
            if (events.Count > 0) { e = events.Dequeue(); return true; }
            e = default;
            return false;
        }

        // ---------------------------------------------------------------- zaman

        public void Step(float dt)
        {
            while (dt > 0f && !IsFinished)
            {
                float h = Math.Min(dt, MaxStepSn);
                Integrate(h);
                dt -= h;
            }
        }

        void Integrate(float h)
        {
            switch (Phase)
            {
                case FlowPhase.KickOff:
                    MovePlayers(h);
                    phaseTimer -= h;
                    if (phaseTimer <= 0f)
                    {
                        Phase = FlowPhase.OpenPlay;
                        dwellTimer = 0.2f;
                    }
                    break;

                case FlowPhase.OpenPlay:
                case FlowPhase.ChanceBuild:
                case FlowPhase.ShotTravel:
                case FlowPhase.CornerSetup:
                case FlowPhase.CornerCross:
                    activeSeconds += h;
                    UpdateMomentum(h);
                    MoveBall(h);
                    MovePlayers(h);
                    if (Phase == FlowPhase.OpenPlay && !ballMoving)
                    {
                        dwellTimer -= h;
                        // top "taşınıyor" hissi: karar beklerken hücum yönüne hafif sürüklenme
                        ballPos = Vec2.MoveTowards(ballPos,
                            ClampToPitch(ballPos + new Vec2(0f, AttackDir(possession) * 3f)),
                            bal.ball.tasimaHiziMS * 0.14f * h);
                        if (dwellTimer <= 0f) DecideOpenPlay();
                    }
                    CheckHalfTransitions();
                    break;

                case FlowPhase.GoalCelebration:
                    MovePlayers(h);
                    phaseTimer -= h;
                    if (phaseTimer <= 0f)
                        BeginKickOff(1 - lastScorerTeam, FlowEventType.KickOff);
                    break;

                case FlowPhase.HalfTimeBreak:
                    phaseTimer -= h;
                    if (phaseTimer <= 0f)
                    {
                        half = 2;
                        BeginKickOff(1, FlowEventType.SecondHalfKickOff); // ikinci yarı santrası deplasmanın
                    }
                    break;

                case FlowPhase.FullTime:
                    break;
            }
        }

        void CheckHalfTransitions()
        {
            if (Phase != FlowPhase.OpenPlay) return; // pozisyon ortasında düdük çalınmaz
            if (half == 1 && MatchMinute >= 45f)
            {
                Phase = FlowPhase.HalfTimeBreak;
                phaseTimer = bal.clock.devreArasiSaniye;
                Emit(FlowEventType.HalfTime, -1);
            }
            else if (half == 2 && MatchMinute >= 90f)
            {
                Phase = FlowPhase.FullTime;
                Emit(FlowEventType.FullTime, -1);
            }
        }

        // ---------------------------------------------------------------- akış kararları

        void DecideOpenPlay()
        {
            decisionTick++;
            pendingLongBallRisk = false;
            var myTac = tacticByTeam[possession];
            var oppTac = tacticByTeam[1 - possession];
            float p = Progress(possession, ballPos);

            // Top kaybı — Domain.Decision: akış kararı gürültüsü (greybox bağlamı)
            float pLoss = bal.flow.pTopKaybiTaban
                          * (1f + (oppTac.pres - 1f) * bal.flow.pTopKaybiPresEtki)
                          * (1f - 0.3f * MomAdv(possession));
            if (p > 0.8f) pLoss *= 1.15f; // kalabalık son bölge direnci
            if (R(Domain.Decision, 1) < pLoss)
            {
                Turnover(jitter: true);
                return;
            }

            // Final üçlüde ceza sahasına giriş → "önemli an" penceresi açılır
            if (p > 0.68f && R(Domain.Decision, 2) < bal.flow.pCezaSahasinaGiris * myTac.tempo)
            {
                Phase = FlowPhase.ChanceBuild;
                chanceActions = 0;
                SendBall(BoxPoint(possession, 3), PassSpeed(12f));
                return;
            }

            // Pas seçimi
            float wF = bal.flow.pIleriPas * myTac.tempo;
            float wS = bal.flow.pYanPas;
            float wB = bal.flow.pGeriPas;
            float wL = bal.flow.pUzunTop;
            float r = (float)R(Domain.Decision, 3) * (wF + wS + wB + wL);
            float dir = AttackDir(possession);
            float dx, dy;

            if (r < wF)
            {
                dy = Lerp(bal.flow.ilerlemeMinM, bal.flow.ilerlemeMaxM, (float)R(Domain.Decision, 4)) * dir;
                dx = ((float)R(Domain.Decision, 5) - 0.5f) * bal.flow.genislikMaxM;
            }
            else if (r < wF + wS)
            {
                dy = ((float)R(Domain.Decision, 4) - 0.3f) * 6f * dir;
                dx = ((float)R(Domain.Decision, 5) - 0.5f) * 2f * bal.flow.genislikMaxM;
            }
            else if (r < wF + wS + wB)
            {
                dy = -Lerp(6f, 12f, (float)R(Domain.Decision, 4)) * dir;
                dx = ((float)R(Domain.Decision, 5) - 0.5f) * 8f;
            }
            else
            {
                dy = Lerp(24f, 36f, (float)R(Domain.Decision, 4)) * dir;
                dx = ((float)R(Domain.Decision, 5) - 0.5f) * 1.6f * bal.flow.genislikMaxM;
                pendingLongBallRisk = true; // uzun top: varışta kapılma riski
            }

            SendBall(ClampToPitch(ballPos + new Vec2(dx, dy)), PassSpeed(MathF.Abs(dy) + MathF.Abs(dx) * 0.5f));
        }

        void DecideChanceBuild()
        {
            decisionTick++;
            chanceActions++;
            var myTac = tacticByTeam[possession];

            float pShot = bal.shot.pSutChanceBuild * myTac.sutIstahi;
            if (chanceActions >= 3) pShot = 0.75f; // pozisyon uzadı: ya şut ya dağılır

            if (R(Domain.Decision, 9) < pShot)
            {
                TakeShot(header: false);
                return;
            }

            if (chanceActions >= 3)
            {
                // Pozisyon dağıldı: savunma uzaklaştırdı — bazen kornere
                if (R(Domain.Decision, 17) < 0.22f)
                    BeginCorner(possession);
                else
                    Turnover(jitter: false);
                return;
            }

            // Ceza sahası çevresinde hızlı pas
            SendBall(BoxPoint(possession, 15), PassSpeed(8f));
        }

        void TakeShot(bool header)
        {
            int shooter = possession;
            decisionTick++;
            if (shooter == 0) Stats.HomeShots++; else Stats.AwayShots++;
            Emit(header ? FlowEventType.CornerHeader : FlowEventType.Shot, shooter);

            // Şut sonucu — Domain.Duel: hücum-savunma düellosunun greybox özeti
            float strengthDiff = strengthByTeam[shooter] - strengthByTeam[1 - shooter];
            float pGol = header ? bal.corner.pGolKafa : bal.shot.pGol;
            pGol += strengthDiff * bal.shot.gucEtkiCarpan + MomAdv(shooter) * bal.shot.momentumEtki;
            pGol = Clamp(pGol, 0.05f, 0.42f);

            float r = (float)R(Domain.Duel, 10);
            if (r < pGol) pendingShotOutcome = 0;
            else
            {
                float wK = bal.shot.pKurtarma, wD = bal.shot.pDisari, wC = bal.shot.pKornerSekmesi;
                if (header) { wC *= 0.5f; } // kafa vuruşu daha az sekme üretir
                float r2 = (float)R(Domain.Duel, 18) * (wK + wD + wC);
                pendingShotOutcome = r2 < wK ? 1 : (r2 < wK + wD ? 2 : 3);
            }

            // Hedef: kale ağzı (direk arası ~7.32m); dışarı ise direk dışına
            float goalY = shooter == 0 ? PitchL + 0.8f : -0.8f;
            float tx = pendingShotOutcome == 2
                ? 34f + Sign((float)R(Domain.Duel, 11) - 0.5f) * Lerp(4.6f, 7.5f, (float)R(Domain.Duel, 19))
                : 34f + ((float)R(Domain.Duel, 11) - 0.5f) * 6.6f;

            Phase = FlowPhase.ShotTravel;
            ballTarget = new Vec2(tx, goalY);
            ballSpeed = Lerp(bal.ball.sutHiziMinMS, bal.ball.sutHiziMaxMS, (float)R(Domain.Duel, 12));
            ballMoving = true;
        }

        void ResolveShot()
        {
            int shooter = possession;
            switch (pendingShotOutcome)
            {
                case 0: // GOL
                    if (shooter == 0) { HomeScore++; Stats.HomeOnTarget++; }
                    else { AwayScore++; Stats.AwayOnTarget++; }
                    lastScorerTeam = shooter;
                    momentum = Clamp(momentum + (shooter == 0 ? bal.momentum.golBoost : -bal.momentum.golBoost), -1f, 1f);
                    Emit(FlowEventType.Goal, shooter);
                    Phase = FlowPhase.GoalCelebration;
                    phaseTimer = bal.pace.kutlamaSuresiSn;
                    ballMoving = false;
                    break;

                case 1: // Kurtarış — top kalecide ya da kornere çelindi
                    if (shooter == 0) Stats.HomeOnTarget++; else Stats.AwayOnTarget++;
                    Emit(FlowEventType.Save, 1 - shooter);
                    if (R(Domain.Duel, 23) < bal.shot.pKornerKurtarisSonrasi)
                    {
                        BeginCorner(shooter); // kaleci topu kornere tokatladı
                        break;
                    }
                    possession = 1 - shooter;
                    ballPos = KeeperPoint(possession);
                    ballMoving = false;
                    Phase = FlowPhase.OpenPlay;
                    dwellTimer = 0.45f;
                    break;

                case 2: // Dışarı — kale vuruşu
                    Emit(FlowEventType.ShotWide, shooter);
                    possession = 1 - shooter;
                    ballPos = KeeperPoint(possession) + new Vec2(((float)R(Domain.Physics, 20) - 0.5f) * 8f, 0f);
                    ballMoving = false;
                    Phase = FlowPhase.OpenPlay;
                    dwellTimer = 0.45f;
                    break;

                default: // Kornere sekme
                    BeginCorner(shooter);
                    break;
            }
            pendingShotOutcome = -1;
        }

        void BeginCorner(int team)
        {
            possession = team;
            chanceActions = 0;
            if (team == 0) Stats.HomeCorners++; else Stats.AwayCorners++;
            Emit(FlowEventType.Corner, team);
            float cy = team == 0 ? PitchL : 0f;
            float cx = ballPos.X < PitchW * 0.5f ? 0.6f : PitchW - 0.6f;
            Phase = FlowPhase.CornerSetup;
            SendBall(new Vec2(cx, cy), bal.ball.ortaHiziMS * 0.7f);
        }

        void StartCornerCross()
        {
            decisionTick++;
            float dir = AttackDir(possession);
            // Orta hedefi: penaltı noktası çevresi — Domain.SetPiece (duran top akışı)
            float tx = 34f + ((float)R(Domain.SetPiece, 13) - 0.5f) * 12f;
            float ty = (possession == 0 ? PitchL : 0f) - dir * Lerp(6f, 11f, (float)R(Domain.SetPiece, 14));
            Phase = FlowPhase.CornerCross;
            SendBall(new Vec2(tx, ty), bal.ball.ortaHiziMS);
        }

        void ResolveCorner()
        {
            decisionTick++;
            if (R(Domain.SetPiece, 13) < bal.corner.pKafaSut)
            {
                TakeShot(header: true);
                return;
            }
            // Savunma uzaklaştırdı
            Turnover(jitter: false);
            float dir = AttackDir(possession); // turnover sonrası yeni sahip savunan takım
            ballPos = ClampToPitch(ballPos + new Vec2(((float)R(Domain.Physics, 20) - 0.5f) * 20f, dir * 20f));
        }

        void Turnover(bool jitter)
        {
            possession = 1 - possession;
            chanceActions = 0;
            Phase = FlowPhase.OpenPlay;
            ballMoving = false;
            dwellTimer = 0.3f;
            if (jitter)
                ballPos = ClampToPitch(ballPos + new Vec2(((float)R(Domain.Physics, 20) - 0.5f) * 6f,
                                                          ((float)R(Domain.Physics, 22) - 0.5f) * 6f));
        }

        void BeginKickOff(int team, FlowEventType evt)
        {
            possession = team;
            chanceActions = 0;
            ballPos = new Vec2(PitchW * 0.5f, PitchL * 0.5f);
            ballMoving = false;
            Phase = FlowPhase.KickOff;
            phaseTimer = bal.pace.santraBeklemeSn;
            Emit(evt, team);
        }

        // ---------------------------------------------------------------- top ve oyuncular

        void SendBall(Vec2 target, float speed)
        {
            ballTarget = ClampToPitchLoose(target);
            ballSpeed = speed;
            ballMoving = true;
        }

        void MoveBall(float h)
        {
            if (!ballMoving) return;
            ballPos = Vec2.MoveTowards(ballPos, ballTarget, ballSpeed * h);
            if (Vec2.Distance(ballPos, ballTarget) > 0.01f) return;

            ballMoving = false;
            switch (Phase)
            {
                case FlowPhase.OpenPlay:
                    if (pendingLongBallRisk)
                    {
                        pendingLongBallRisk = false;
                        // Uzun top varışta kapılabilir — Domain.Decision
                        if (R(Domain.Decision, 8) < 0.38f) { Turnover(jitter: true); return; }
                    }
                    dwellTimer = Lerp(bal.pace.aksiyonAralikMinSn, bal.pace.aksiyonAralikMaxSn,
                                      (float)R(Domain.Decision, 7)) / tacticByTeam[possession].tempo;
                    break;
                case FlowPhase.ChanceBuild: DecideChanceBuild(); break;
                case FlowPhase.ShotTravel: ResolveShot(); break;
                case FlowPhase.CornerSetup: StartCornerCross(); break;
                case FlowPhase.CornerCross: ResolveCorner(); break;
            }
        }

        void MovePlayers(float h)
        {
            // Sabit güncelleme sırası: takım 0 → 1, formasyon indeksi artan (CLAUDE.md deseni)
            int pressIdx = FindPresser();
            for (int t = 0; t < 2; t++)
            {
                for (int i = 0; i < 11; i++)
                {
                    int idx = t * 11 + i;
                    Vec2 target = ComputeTarget(t, i, idx == pressIdx);
                    players[idx].Target = target;
                    float vmax = bal.players.vMaxMS * (players[idx].IsKeeper ? 0.75f : 1f);
                    if (idx == pressIdx) vmax *= bal.players.presHizCarpan;
                    if (Phase == FlowPhase.KickOff) vmax *= 1.35f; // dizilişe hızlı dönüş
                    players[idx].Pos = Vec2.MoveTowards(players[idx].Pos, target, vmax * h);
                }
            }
        }

        int FindPresser()
        {
            if (Phase != FlowPhase.OpenPlay && Phase != FlowPhase.ChanceBuild) return -1;
            int defTeam = 1 - possession;
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 1; i < 11; i++) // kaleci pres yapmaz
            {
                int idx = defTeam * 11 + i;
                float d = Vec2.Distance(players[idx].Pos, ballPos);
                if (d < bestD) { bestD = d; best = idx; }
            }
            return best;
        }

        Vec2 ComputeTarget(int team, int i, bool isPresser)
        {
            var tac = tacticByTeam[team];
            float dir = AttackDir(team);
            int idx = team * 11 + i;

            if (Phase == FlowPhase.GoalCelebration)
            {
                // Gol atan takım orta yuvarlağa koşar; yiyen takım dizilişine döner
                return team == lastScorerTeam
                    ? new Vec2(PitchW * 0.5f + (i - 5) * 1.6f, PitchL * 0.5f + dir * -6f)
                    : AnchorWorld(team, i);
            }

            Vec2 anchor = AnchorWorld(team, i);

            if (i == 0)
            {
                // Kaleci: kale önünde topu izler
                float gx = Clamp(34f + (ballPos.X - 34f) * 0.22f, 30.5f, 37.5f);
                float gy = team == 0 ? 1.6f : PitchL - 1.6f;
                return new Vec2(gx, gy);
            }

            if (Phase == FlowPhase.KickOff || Phase == FlowPhase.HalfTimeBreak)
                return anchor;

            // Hat yüksekliği + hücum/savunma blok kayması
            float shift = (tac.hatYuksekligi - 0.5f) * 24f;
            shift += team == possession ? bal.players.hucumKaymaM * BlockFactor(i) : -bal.players.savunmaKaymaM;
            Vec2 target = new Vec2(anchor.X, anchor.Y + dir * shift);

            if (team != possession)
            {
                // Savunma topa doğru daralır
                target.X = Lerp(target.X, ballPos.X, 0.22f);
                if (isPresser) return ballPos; // en yakın adam topa çıkar
            }
            else
            {
                // Topa yakın hücum oyuncuları destek verir
                float d = Vec2.Distance(anchor, ballPos);
                if (d < bal.players.topCekimYaricapM * 2.2f)
                {
                    target.X = Lerp(target.X, ballPos.X, 0.45f);
                    target.Y = Lerp(target.Y, ballPos.Y, 0.35f);
                }
                if (Phase == FlowPhase.ChanceBuild && i >= 8)
                {
                    // Forvetler ceza sahasına dalar
                    target = Lerp2(target, BoxPoint(team, (uint)(30 + i)), 0.6f);
                }
            }

            // Canlılık: periyodik amaçsız salınım — Domain.Crowd (yalnız kozmetik)
            uint wTick = (uint)((activeSeconds + i * 0.7f) / bal.players.wanderPeriyotSn);
            float wx = ((float)Rng.Rand01(seed, Domain.Crowd, (uint)idx, wTick, 25) - 0.5f) * 2f * bal.players.wanderGenlikM;
            float wy = ((float)Rng.Rand01(seed, Domain.Crowd, (uint)idx, wTick, 26) - 0.5f) * 2f * bal.players.wanderGenlikM;
            wanderOffset[idx] = Vec2.MoveTowards(wanderOffset[idx], new Vec2(wx, wy), 1.2f * MaxStepSn);
            target += wanderOffset[idx];

            target.X = Clamp(target.X, 1.2f, PitchW - 1.2f);
            target.Y = Clamp(target.Y, 1.2f, PitchL - 1.2f);
            return target;
        }

        static float BlockFactor(int i) => i >= 8 ? 1.0f : (i >= 5 ? 0.75f : 0.45f); // forvet > orta > savunma

        void UpdateMomentum(float h)
        {
            noiseAccum += h;
            while (noiseAccum >= 0.25f)
            {
                noiseAccum -= 0.25f;
                noiseTick++;
                // Momentum dalgası — Domain.Chaos: maçın kaos/salınım rengi (greybox)
                float g = (float)Rng.Gauss01(seed, Domain.Chaos, 99, noiseTick, 21);
                float drift = (strengthByTeam[0] - strengthByTeam[1]) * bal.momentum.gucFarkiCarpan;
                momentum += (g * bal.momentum.sigma - momentum * bal.momentum.sonum + drift) * 0.25f;
                momentum = Clamp(momentum, -1f, 1f);
            }
        }

        // ---------------------------------------------------------------- yardımcılar

        float MomAdv(int team) => team == 0 ? momentum : -momentum;

        static float AttackDir(int team) => team == 0 ? 1f : -1f; // ev üst kaleye (y=105) hücum eder

        static float Progress(int team, Vec2 pos) => team == 0 ? pos.Y / PitchL : 1f - pos.Y / PitchL;

        Vec2 AnchorWorld(int team, int i)
        {
            float[] a = anchorsByTeam[team][i];
            float x = a[0] * PitchW;
            float y = team == 0 ? a[1] * PitchL : PitchL - a[1] * PitchL;
            return new Vec2(x, y);
        }

        Vec2 KeeperPoint(int team) => new Vec2(34f, team == 0 ? 5.5f : PitchL - 5.5f);

        Vec2 BoxPoint(int team, uint salt)
        {
            decisionTick++;
            float dir = AttackDir(team);
            float gy = team == 0 ? PitchL : 0f;
            float tx = 34f + ((float)R(Domain.Decision, salt) - 0.5f) * 26f;
            float ty = gy - dir * Lerp(5f, 17f, (float)R(Domain.Decision, salt + 1));
            return new Vec2(tx, ty);
        }

        float PassSpeed(float dist) =>
            Lerp(bal.ball.pasHiziMinMS, bal.ball.pasHiziMaxMS, Clamp(dist / 35f, 0f, 1f));

        Vec2 ClampToPitch(Vec2 v) => new Vec2(Clamp(v.X, 2f, PitchW - 2f), Clamp(v.Y, 1.5f, PitchL - 1.5f));

        // Şut hedefi kale çizgisini geçebilsin diye gevşek sınır
        Vec2 ClampToPitchLoose(Vec2 v) => new Vec2(Clamp(v.X, 0.4f, PitchW - 0.4f), Clamp(v.Y, -1f, PitchL + 1f));

        double R(Domain d, uint salt) => Rng.Rand01(seed, d, (uint)possession, decisionTick, salt);

        void Emit(FlowEventType type, int team) =>
            events.Enqueue(new FlowEvent(type, team, MatchMinute, HomeScore, AwayScore));

        static float Lerp(float a, float b, float t) => a + (b - a) * t;
        static Vec2 Lerp2(Vec2 a, Vec2 b, float t) => new Vec2(Lerp(a.X, b.X, t), Lerp(a.Y, b.Y, t));
        static float Clamp(float v, float lo, float hi) => v < lo ? lo : (v > hi ? hi : v);
        static float Sign(float v) => v < 0f ? -1f : 1f;
    }
}
