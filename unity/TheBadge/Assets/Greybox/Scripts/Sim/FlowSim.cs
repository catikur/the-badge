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
        int carrierIdx = -1;          // topun sahibi oyuncu (görsel inandırıcılık — İterasyon 1)
        int pendingReceiverIdx = -1;  // havadaki pasın hedef oyuncusu
        Vec2 celebPos;                // gol sevinci kümelenme noktası (İterasyon 2)
        float stageHold;              // diziliş sağlandıktan sonra düdük/orta öncesi nefes sayacı
        bool kickoffPassPending;      // santra pası: ilk karar geriye/yana kısa pas (Sahneleme 1)

        /// <summary>Diziliş emniyeti kaç kez devreye girdi (sahne sözleşmesi telemetrisi).</summary>
        public int StagingTimeouts { get; private set; }
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
                    // Sahneleme 1: düdük, diziliş koşulu sağlanmadan ÇALMAZ (süre değil yerleşim)
                    MovePlayers(h);
                    phaseTimer -= h;
                    if (KickoffReady())
                    {
                        stageHold += h;
                        if (stageHold >= bal.pace.santraBeklemeSn) Whistle();
                    }
                    else stageHold = 0f;
                    if (Phase == FlowPhase.KickOff && phaseTimer <= 0f)
                    {
                        StagingTimeouts++; // kilitlenme emniyeti (Sahneleme kök ilkesi)
                        Whistle();
                    }
                    break;

                case FlowPhase.OpenPlay:
                case FlowPhase.ChanceBuild:
                case FlowPhase.ShotTravel:
                case FlowPhase.CornerSetup:
                case FlowPhase.CornerCross:
                case FlowPhase.GoalKick:
                    // Diziliş duraklamalarında maç saati DURUR — 90 dakika saf akışa aittir
                    // (sahneleme beklemeleri aksiyon yoğunluğunu düşürmesin)
                    if (!((Phase == FlowPhase.CornerSetup && !ballMoving) || Phase == FlowPhase.GoalKick))
                        activeSeconds += h;
                    UpdateMomentum(h);
                    MoveBall(h);
                    MovePlayers(h);
                    if (Phase == FlowPhase.CornerSetup && !ballMoving)
                    {
                        // Sahneleme 5: kutu dolmadan orta GELMEZ
                        phaseTimer -= h;
                        if (CornerReady())
                        {
                            stageHold += h;
                            if (stageHold >= bal.corner.dizilisSn) StartCornerCross();
                        }
                        else stageHold = 0f;
                        if (Phase == FlowPhase.CornerSetup && phaseTimer <= 0f)
                        {
                            StagingTimeouts++;
                            StartCornerCross();
                        }
                    }
                    if (Phase == FlowPhase.GoalKick)
                    {
                        // Sahneleme 4: kaleci topun başına gelip savunma açılmadan oyun başlamaz
                        phaseTimer -= h;
                        if (GoalKickReady())
                        {
                            stageHold += h;
                            if (stageHold >= bal.pace.santraBeklemeSn) ResumeFromGoalKick();
                        }
                        else stageHold = 0f;
                        if (Phase == FlowPhase.GoalKick && phaseTimer <= 0f)
                        {
                            StagingTimeouts++;
                            ResumeFromGoalKick();
                        }
                    }
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

            // Sahneleme 1: santra pası geriye/yana kısa pastır, kapılamaz
            if (kickoffPassPending)
            {
                kickoffPassPending = false;
                int back = PickReceiver(possession, 2);
                if (back < 0) back = PickReceiver(possession, 1);
                if (back >= 0)
                {
                    pendingReceiverIdx = back;
                    Vec2 t0 = ClampToPitch(players[back].Pos + new Vec2(
                        ((float)R(Domain.Decision, 5) - 0.5f) * 1.2f,
                        ((float)R(Domain.Decision, 4) - 0.5f) * 1.2f));
                    SendBall(t0, PassSpeed(Vec2.Distance(ballPos, t0)));
                    return;
                }
            }

            var myTac = tacticByTeam[possession];
            var oppTac = tacticByTeam[1 - possession];
            float p = Progress(possession, ballPos);

            // Top kaybı — Domain.Decision: akış kararı gürültüsü (greybox bağlamı).
            // Güç farkı top tutmayı eğer (İterasyon 1: sonuç güce daha bağımlı olsun).
            float strengthKeep = 1f - Clamp(
                (strengthByTeam[possession] - strengthByTeam[1 - possession]) * bal.flow.gucTutmaCarpan,
                -0.25f, 0.25f);
            float pLoss = bal.flow.pTopKaybiTaban
                          * (1f + (oppTac.pres - 1f) * bal.flow.pTopKaybiPresEtki)
                          * (1f - 0.3f * MomAdv(possession))
                          * strengthKeep;
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
                Emit(FlowEventType.ChanceStart, possession);
                PassTowardsGoal();
                return;
            }

            // Pas seçimi: alıcı GERÇEK bir takım arkadaşıdır — top oyuncudan oyuncuya
            // gider (İterasyon 1: "top oyunculardan çok uzakta" geri bildirimi).
            float wF = bal.flow.pIleriPas * myTac.tempo;
            float wS = bal.flow.pYanPas;
            float wB = bal.flow.pGeriPas;
            float wL = bal.flow.pUzunTop;
            float r = (float)R(Domain.Decision, 3) * (wF + wS + wB + wL);
            int kind = r < wF ? 0 : (r < wF + wS ? 1 : (r < wF + wS + wB ? 2 : 3));

            int receiver = PickReceiver(possession, kind);
            if (receiver >= 0)
            {
                pendingReceiverIdx = receiver;
                pendingLongBallRisk = kind == 3;
                Vec2 t = players[receiver].Pos + new Vec2(
                    ((float)R(Domain.Decision, 5) - 0.5f) * 1.2f,
                    ((float)R(Domain.Decision, 4) - 0.5f) * 1.2f);
                t = ClampToPitch(t);
                SendBall(t, PassSpeed(Vec2.Distance(ballPos, t)));
                return;
            }

            // Uygun alıcı yoksa geometrik geri dönüş yolu (eski davranış)
            float dir = AttackDir(possession);
            float dy = kind == 2
                ? -Lerp(6f, 12f, (float)R(Domain.Decision, 4)) * dir
                : Lerp(bal.flow.ilerlemeMinM, bal.flow.ilerlemeMaxM, (float)R(Domain.Decision, 4)) * dir;
            float dx = ((float)R(Domain.Decision, 5) - 0.5f) * bal.flow.genislikMaxM;
            SendBall(ClampToPitch(ballPos + new Vec2(dx, dy)), PassSpeed(MathF.Abs(dy) + MathF.Abs(dx) * 0.5f));
        }

        /// <summary>Ceza sahası çevresindeki en uygun hücumcuya pas (ChanceBuild akışı).</summary>
        void PassTowardsGoal()
        {
            int receiver = PickReceiverNearGoal(possession);
            if (receiver >= 0)
            {
                pendingReceiverIdx = receiver;
                Vec2 t = players[receiver].Pos + new Vec2(
                    ((float)R(Domain.Decision, 15) - 0.5f) * 1.2f,
                    ((float)R(Domain.Decision, 16) - 0.5f) * 1.2f);
                t = ClampToPitch(t);
                SendBall(t, PassSpeed(Vec2.Distance(ballPos, t)));
            }
            else
            {
                SendBall(BoxPoint(possession, 3), PassSpeed(12f));
            }
        }

        /// <summary>Pas türüne uyan takım arkadaşını seçer; yoksa -1 (geometrik geri dönüş).</summary>
        int PickReceiver(int team, int kind)
        {
            float dir = AttackDir(team);
            int best = -1;
            float bestScore = -1f;
            for (int i = 1; i < 11; i++) // kaleciye dönüş pası greybox'ta yok
            {
                int idx = team * 11 + i;
                if (idx == carrierIdx) continue;
                Vec2 pp = players[idx].Pos;
                float ahead = (pp.Y - ballPos.Y) * dir;
                float dist = Vec2.Distance(pp, ballPos);
                if (dist < 4f || dist > 42f) continue;

                bool fits = kind switch
                {
                    0 => ahead > 3f && dist <= 26f,   // ileri pas
                    1 => MathF.Abs(ahead) <= 6f,      // yan pas
                    2 => ahead < -3f && dist <= 20f,  // geri pas
                    _ => ahead > 18f                  // uzun top
                };
                if (!fits) continue;

                // Yakınlık + jitter; ileri pasta derinlik bonusu — Domain.Decision
                float score = 1f / (1f + dist * 0.05f)
                              + (float)Rng.Rand01(seed, Domain.Decision, (uint)idx, decisionTick, 40) * 0.6f
                              + (kind == 0 ? ahead * 0.02f : 0f);
                if (score > bestScore) { bestScore = score; best = idx; }
            }
            return best;
        }

        /// <summary>Rakip kaleye en yakın uygun hücumcu (ChanceBuild alıcısı); yoksa -1.</summary>
        int PickReceiverNearGoal(int team)
        {
            float gy = team == 0 ? PitchL : 0f;
            int best = -1;
            float bestScore = -1f;
            for (int i = 1; i < 11; i++)
            {
                int idx = team * 11 + i;
                if (idx == carrierIdx) continue;
                float dGoal = MathF.Abs(gy - players[idx].Pos.Y) + MathF.Abs(34f - players[idx].Pos.X) * 0.4f;
                if (dGoal > 45f) continue;
                float score = 1f / (1f + dGoal * 0.05f)
                              + (float)Rng.Rand01(seed, Domain.Decision, (uint)idx, decisionTick, 41) * 0.5f;
                if (score > bestScore) { bestScore = score; best = idx; }
            }
            return best;
        }

        int NearestOutfield(int team, Vec2 pos)
        {
            int best = team * 11 + 1;
            float bestD = float.MaxValue;
            for (int i = 1; i < 11; i++)
            {
                int idx = team * 11 + i;
                float d = Vec2.Distance(players[idx].Pos, pos);
                if (d < bestD) { bestD = d; best = idx; }
            }
            return best;
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

            // Ceza sahası çevresinde hızlı pas — yine gerçek bir alıcıya
            PassTowardsGoal();
        }

        void TakeShot(bool header)
        {
            int shooter = possession;
            decisionTick++;
            pendingReceiverIdx = -1;
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

            // Hedef derinliği sonuca göre: gol AĞLARIN İÇİNDE biter, kurtarış kaleci önünde,
            // aut/sekme çizgiyi geçer (İterasyon 2 — "top ağlara gitmiyor").
            float depth = pendingShotOutcome == 0 ? 1.7f
                        : pendingShotOutcome == 1 ? -1.4f
                        : 1.0f;
            float goalY = shooter == 0 ? PitchL + depth : 0f - depth;
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
                    ballMoving = false; // top ağlarda kalır, kutlama biterken santraya taşınır
                    carrierIdx = -1;
                    // Kutlama noktası: gol atılan kaleye yakın, köşeye çekik (İterasyon 2)
                    celebPos = new Vec2(
                        Clamp(34f + Sign(ballPos.X - 34f) * 12f, 8f, PitchW - 8f),
                        (shooter == 0 ? PitchL : 0f) - AttackDir(shooter) * 8.5f);
                    break;

                case 1: // Kurtarış — kaleci topu tutar (Sahne 4), bazen kornere çeler
                    if (shooter == 0) Stats.HomeOnTarget++; else Stats.AwayOnTarget++;
                    Emit(FlowEventType.Save, 1 - shooter);
                    if (R(Domain.Duel, 23) < bal.shot.pKornerKurtarisSonrasi)
                    {
                        BeginCorner(shooter); // kaleci topu kornere tokatladı
                        break;
                    }
                    possession = 1 - shooter;
                    carrierIdx = possession * 11; // top kalecide, dağıtım ondan
                    ballMoving = false;
                    Phase = FlowPhase.OpenPlay;
                    dwellTimer = bal.pace.gkTutmaSn; // kaleci topu tutar, sonra kısa pasla başlatır
                    break;

                case 2: // Aut — KALE VURUŞU sahnesi (Sahne 4)
                    Emit(FlowEventType.ShotWide, shooter);
                    BeginGoalKick(1 - shooter);
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
            pendingReceiverIdx = -1;
            if (team == 0) Stats.HomeCorners++; else Stats.AwayCorners++;
            Emit(FlowEventType.Corner, team);
            float cy = team == 0 ? PitchL : 0f;
            float cx = ballPos.X < PitchW * 0.5f ? 0.6f : PitchW - 0.6f;
            Phase = FlowPhase.CornerSetup;
            SendBall(new Vec2(cx, cy), bal.ball.ortaHiziMS * 0.7f);
            carrierIdx = NearestOutfield(team, new Vec2(cx, cy)); // korner kullanıcısı köşeye gider
        }

        void StartCornerCross()
        {
            decisionTick++;
            float dir = AttackDir(possession);
            // Orta hedefi: ceza sahasındaki bir hücumcu — Domain.SetPiece (duran top akışı)
            int target = PickReceiverNearGoal(possession);
            float tx, ty;
            if (target >= 0)
            {
                pendingReceiverIdx = target;
                tx = players[target].Pos.X + ((float)R(Domain.SetPiece, 13) - 0.5f) * 3f;
                ty = players[target].Pos.Y + ((float)R(Domain.SetPiece, 14) - 0.5f) * 3f;
            }
            else
            {
                tx = 34f + ((float)R(Domain.SetPiece, 13) - 0.5f) * 12f;
                ty = (possession == 0 ? PitchL : 0f) - dir * Lerp(6f, 11f, (float)R(Domain.SetPiece, 14));
            }
            Phase = FlowPhase.CornerCross;
            SendBall(new Vec2(tx, ty), bal.ball.ortaHiziMS);
        }

        void ResolveCorner()
        {
            decisionTick++;
            if (pendingReceiverIdx >= 0 && pendingReceiverIdx / 11 == possession)
                carrierIdx = pendingReceiverIdx; // ortayı karşılayan oyuncu
            pendingReceiverIdx = -1;
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
            pendingReceiverIdx = -1;
            Phase = FlowPhase.OpenPlay;
            ballMoving = false;
            dwellTimer = 0.3f;
            if (jitter)
                ballPos = ClampToPitch(ballPos + new Vec2(((float)R(Domain.Physics, 20) - 0.5f) * 6f,
                                                          ((float)R(Domain.Physics, 22) - 0.5f) * 6f));
            carrierIdx = NearestOutfield(possession, ballPos); // topu kapan oyuncu
        }

        void BeginKickOff(int team, FlowEventType evt)
        {
            possession = team;
            chanceActions = 0;
            pendingReceiverIdx = -1;
            ballPos = new Vec2(PitchW * 0.5f, PitchL * 0.5f);
            ballMoving = false;
            Phase = FlowPhase.KickOff;
            phaseTimer = bal.pace.dizilisEmniyetSn; // kilitlenme emniyeti; düdüğü DİZİLİŞ verir
            stageHold = 0f;
            carrierIdx = team * 11 + 9; // forvet santra başında bekler
            Emit(evt, team);
        }

        void Whistle()
        {
            kickoffPassPending = true;
            Phase = FlowPhase.OpenPlay;
            dwellTimer = 0.25f;
        }

        /// <summary>Sahneleme 1 diziliş koşulu: herkes kendi yarısında, santra kullanmayan
        /// takım orta yuvarlağın dışında, forvet topun başında (Kural 8).</summary>
        bool KickoffReady()
        {
            Vec2 center = new Vec2(34f, PitchL * 0.5f);
            for (int t = 0; t < 2; t++)
            {
                for (int i = 0; i < 11; i++)
                {
                    Vec2 p = players[t * 11 + i].Pos;
                    bool ownHalf = t == 0 ? p.Y <= PitchL * 0.5f + 1.2f : p.Y >= PitchL * 0.5f - 1.2f;
                    if (!ownHalf) return false;
                    if (t != possession && Vec2.Distance(p, center) < 8.6f) return false;
                }
            }
            return Vec2.Distance(players[carrierIdx].Pos, ballPos) < 1.8f;
        }

        /// <summary>Sahneleme 5 diziliş koşulu: hücumdan ≥5 ve savunmadan ≥5 oyuncu kutuda,
        /// korner kullanıcısı topun başında.</summary>
        bool CornerReady()
        {
            int atkIn = 0, defIn = 0;
            for (int i = 1; i < 11; i++)
            {
                int ai = possession * 11 + i;
                if (ai != carrierIdx && InPenaltyBox(players[ai].Pos, possession)) atkIn++;
                if (InPenaltyBox(players[(1 - possession) * 11 + i].Pos, possession)) defIn++;
            }
            return atkIn >= 5 && defIn >= 5 && Vec2.Distance(players[carrierIdx].Pos, ballPos) < 2f;
        }

        /// <summary>atkTeam'in hücum ettiği kalenin ceza sahası içinde mi (1 m tolerans)?</summary>
        bool InPenaltyBox(Vec2 p, int atkTeam)
        {
            float gy = atkTeam == 0 ? PitchL : 0f;
            return MathF.Abs(p.X - 34f) <= 21.15f && MathF.Abs(p.Y - gy) <= 17.5f;
        }

        /// <summary>Sahne 4: aut sonrası kale vuruşu — top kale sahasına, kaleci başına.</summary>
        void BeginGoalKick(int team)
        {
            possession = team;
            chanceActions = 0;
            pendingReceiverIdx = -1;
            float gy = team == 0 ? 5.5f : PitchL - 5.5f;
            ballPos = new Vec2(34f + ((float)R(Domain.Physics, 24) - 0.5f) * 11f, gy);
            ballMoving = false;
            carrierIdx = team * 11; // kaleci kullanır
            Phase = FlowPhase.GoalKick;
            phaseTimer = bal.pace.dizilisEmniyetSn * 0.75f;
            stageHold = 0f;
        }

        /// <summary>Kale vuruşu koşulu: kaleci topun başında, rakip ceza sahası boşaldı.</summary>
        bool GoalKickReady()
        {
            if (Vec2.Distance(players[carrierIdx].Pos, ballPos) > 2f) return false;
            for (int i = 0; i < 11; i++)
                if (InPenaltyBox(players[(1 - possession) * 11 + i].Pos, 1 - possession)) return false;
            return true;
        }

        void ResumeFromGoalKick()
        {
            Phase = FlowPhase.OpenPlay;
            dwellTimer = 0.35f; // kaleci kısa pasla oyunu başlatır
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
                    AssignCarrierOnArrival();
                    dwellTimer = Lerp(bal.pace.aksiyonAralikMinSn, bal.pace.aksiyonAralikMaxSn,
                                      (float)R(Domain.Decision, 7)) / tacticByTeam[possession].tempo;
                    break;
                case FlowPhase.ChanceBuild:
                    AssignCarrierOnArrival();
                    DecideChanceBuild();
                    break;
                case FlowPhase.ShotTravel: ResolveShot(); break;
                case FlowPhase.CornerSetup:
                    phaseTimer = bal.pace.dizilisEmniyetSn; // top köşede: diziliş koşulu beklenir
                    stageHold = 0f;
                    break;
                case FlowPhase.CornerCross: ResolveCorner(); break;
            }
        }

        /// <summary>Pas varınca top alıcının ayağına bağlanır; alıcı yoksa en yakın oyuncuya.</summary>
        void AssignCarrierOnArrival()
        {
            carrierIdx = pendingReceiverIdx >= 0 && pendingReceiverIdx / 11 == possession
                ? pendingReceiverIdx
                : NearestOutfield(possession, ballPos);
            pendingReceiverIdx = -1;
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
                    if (Phase == FlowPhase.CornerSetup) vmax *= 1.2f; // kutuya diziliş koşusu
                    if (Phase == FlowPhase.GoalCelebration && t == lastScorerTeam) vmax *= 1.25f; // sevinç sprinti
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
                // Gol sevinci: atan takım skorer noktasında KÜMELENİR (İterasyon 2);
                // yiyen takım santra dizilişine döner. Kaleci sevince katılmaz.
                if (team != lastScorerTeam || i == 0) return AnchorWorld(team, i);
                float ang = i * 0.63f;
                float rad = 1.1f + (i % 3) * 0.7f;
                return new Vec2(
                    Clamp(celebPos.X + MathF.Cos(ang) * rad, 1.2f, PitchW - 1.2f),
                    Clamp(celebPos.Y + MathF.Sin(ang) * rad, 1.2f, PitchL - 1.2f));
            }

            Vec2 anchor = AnchorWorld(team, i);

            if (i == 0)
            {
                // Kale vuruşunda kaleci topun başına gelir (Sahne 4)
                if (Phase == FlowPhase.GoalKick && team == possession)
                    return BallSpot();
                // Kaleci: kale önünde topu izler
                float gx = Clamp(34f + (ballPos.X - 34f) * 0.22f, 30.5f, 37.5f);
                float gy = team == 0 ? 1.6f : PitchL - 1.6f;
                return new Vec2(gx, gy);
            }

            if (Phase == FlowPhase.KickOff || Phase == FlowPhase.HalfTimeBreak)
                return KickoffTarget(team, i, idx);

            // Kale vuruşu sahnesi: herkes dizilişine açılır, pres yok (Sahne 4)
            if (Phase == FlowPhase.GoalKick)
                return anchor;

            // Topun sahibi topla oynar — pas beklerken/taşırken dairenin dibinde durur (İterasyon 1)
            if (team == possession && idx == carrierIdx &&
                (Phase == FlowPhase.OpenPlay || Phase == FlowPhase.ChanceBuild || Phase == FlowPhase.CornerSetup))
                return BallSpot();

            // Pas havadayken ALICI buluşma noktasına koşar — top boş alana düşmez (İterasyon 2)
            if (ballMoving && idx == pendingReceiverIdx)
                return new Vec2(Clamp(ballTarget.X, 1.2f, PitchW - 1.2f), Clamp(ballTarget.Y, 1.2f, PitchL - 1.2f));

            // Korner sahnelemesi: hücum kutuya doluşur, savunma kutuda adam tutar (İterasyon 2)
            if (Phase == FlowPhase.CornerSetup || Phase == FlowPhase.CornerCross)
                return CornerStagingTarget(team, i, idx);

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

        // Korner diziliş noktaları — ceza sahası içi ızgara (geometri; kalibrasyon değil)
        static readonly float[][] CornerBoxSpots =
        {
            new[] { 26f, 7f }, new[] { 34f, 6f }, new[] { 42f, 7f },
            new[] { 30f, 11f }, new[] { 38f, 11f }, new[] { 34f, 14.5f }
        };
        static readonly float[][] CornerEdgeSpots = { new[] { 26f, 19f }, new[] { 42f, 19f } };

        /// <summary>Korner sahnesi hedefi: hücum kutu içi/kutu önü, savunma gol tarafında markaj,
        /// 2 savunmacı kontra için yarı sahada bekler. Korner kullanıcısı köşede kalır.</summary>
        Vec2 CornerStagingTarget(int team, int i, int idx)
        {
            int atk = possession;
            float gy = atk == 0 ? PitchL : 0f;            // hücum edilen kale çizgisi
            float dirIn = atk == 0 ? -1f : 1f;            // kaleden sahaya doğru yön

            if (team == atk)
            {
                if (idx == carrierIdx) return players[idx].Pos; // korner kullanıcısı köşede bekler
                // Sıra: kutu içi 6 nokta → kutu önü 2 → geride 2 guard
                int k = i - 1 - (carrierIdx / 11 == atk && carrierIdx % 11 < i ? 1 : 0);
                if (k < 6)
                    return new Vec2(CornerBoxSpots[k][0], gy + dirIn * CornerBoxSpots[k][1]);
                if (k < 8)
                    return new Vec2(CornerEdgeSpots[k - 6][0], gy + dirIn * CornerEdgeSpots[k - 6][1]);
                return new Vec2(k == 8 ? 26f : 42f, gy + dirIn * 50f); // kontra sigortası
            }

            // Savunan takım: kutu içinde adam tutar (gol tarafında 2m); forvetler kontrada bekler
            if (i >= 9)
                return new Vec2(i == 9 ? 28f : 40f, gy + dirIn * 56f);
            int m = i - 1;
            float[] spot = m < 6 ? CornerBoxSpots[m] : CornerEdgeSpots[m - 6];
            return new Vec2(spot[0] + (i % 2 == 0 ? 1.1f : -1.1f), gy + dirIn * MathF.Max(2.5f, spot[1] - 2f));
        }

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

        /// <summary>Sahneleme 1 diziliş hedefi: kendi yarı saha kilidi + rakip çember dışı +
        /// santra takımının forvetleri topun başında/çember kenarında.</summary>
        Vec2 KickoffTarget(int team, int i, int idx)
        {
            if (team == possession && idx == carrierIdx) return BallSpot();
            float half = PitchL * 0.5f;
            Vec2 a = AnchorWorld(team, i);
            a.Y = team == 0 ? MathF.Min(a.Y, half - 3f) : MathF.Max(a.Y, half + 3f); // Kural 8

            if (team == possession && i == 10) // ikinci forvet çember kenarında
                return new Vec2(38f, half - AttackDir(team) * 2.2f);

            if (team != possession)
            {
                // Santra kullanmayan takım orta yuvarlağın dışında bekler
                Vec2 c = new Vec2(34f, half);
                Vec2 d = a - c;
                float m = d.Magnitude;
                if (m < 10.5f)
                {
                    a = m < 0.01f ? c + new Vec2(0f, -AttackDir(team) * 10.5f) : c + d * (10.5f / m);
                    a.Y = team == 0 ? MathF.Min(a.Y, half - 0.8f) : MathF.Max(a.Y, half + 0.8f);
                }
            }
            return a;
        }

        Vec2 BallSpot() => new Vec2(Clamp(ballPos.X, 1.2f, PitchW - 1.2f), Clamp(ballPos.Y, 1.2f, PitchL - 1.2f));

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

        // Şut hedefi ağların içine kadar gidebilsin diye gevşek sınır
        Vec2 ClampToPitchLoose(Vec2 v) => new Vec2(Clamp(v.X, 0.4f, PitchW - 0.4f), Clamp(v.Y, -2.2f, PitchL + 2.2f));

        double R(Domain d, uint salt) => Rng.Rand01(seed, d, (uint)possession, decisionTick, salt);

        void Emit(FlowEventType type, int team) =>
            events.Enqueue(new FlowEvent(type, team, MatchMinute, HomeScore, AwayScore));

        static float Lerp(float a, float b, float t) => a + (b - a) * t;
        static Vec2 Lerp2(Vec2 a, Vec2 b, float t) => new Vec2(Lerp(a.X, b.X, t), Lerp(a.Y, b.Y, t));
        static float Clamp(float v, float lo, float hi) => v < lo ? lo : (v > hi ? hi : v);
        static float Sign(float v) => v < 0f ? -1f : 1f;
    }
}
