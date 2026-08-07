using System;
using System.Collections.Generic;
using System.IO;
using TheBadge.Greybox.Loop;
using TheBadge.Greybox.Sim;
using TheBadge.Greybox.UI;
using TheBadge.Greybox.View;
using TheBadge.Sim.Commands;
using UnityEngine;

namespace TheBadge.Greybox
{
    /// <summary>
    /// Greybox giriş noktası — sahnedeki TEK obje. MODEL MAÇI deneyimini yönetir
    /// (Sahneleme §0, Fun Gate pivotu): Maç öncesi → Blok blok model maçı (müdahaleli,
    /// gol bloklarında 2D vinyet) → Maç sonu ekonomisi → Sonraki maç.
    /// Tüm kullanıcı eylemleri (taktik, tempo, bilet, sonraki maç) Tek Kapı'dan geçer.
    /// </summary>
    public sealed class GreyboxBootstrap : MonoBehaviour
    {
        const string AppVersion = "greybox-0.2.0-model";

        GreyboxBalance bal;
        GreyboxState state;
        GreyboxCommandBus bus;
        TelemetryLog telemetry;
        UiShell ui;
        PitchView pitch;
        CameraRig camRig;
        ModelMatchDirector director;

        string opponentName = "";
        string awayShort = "";
        MatchSetup currentSetup;
        bool matchRunning;
        float postShownAt;
        bool priceDirty;
        int matchesEndedThisSession;
        readonly List<float> momentumHistory = new List<float>();
        readonly List<string> goalLog = new List<string>();
        readonly List<string> moveLog = new List<string>();

        static string HomeShort => "ROZET";

        void Awake()
        {
            Application.targetFrameRate = 60; // greybox akıcılık hedefi 60fps (DoD-G)

            var txt = Resources.Load<TextAsset>("greybox.balance");
            if (txt == null)
            {
                Debug.LogError("greybox.balance.json Resources altında bulunamadı — kurulum bozuk.");
                enabled = false;
                return;
            }
            bal = JsonUtility.FromJson<GreyboxBalance>(txt.text);

            state = SaveService.LoadOrNew(bal);
            if (state.worldSeed == 0)
                state.worldSeed = DateTime.Now.Ticks; // sim dışı tohum üretimi — determinizm borcu FAZ 03
            state.sessionCount++;
            SaveService.Save(state);

            bus = new GreyboxCommandBus(bal, state);
            bus.Applied += _ => SaveService.Save(state);

            telemetry = new TelemetryLog(
                Path.Combine(Application.persistentDataPath, "telemetry"),
                DateTime.Now.ToString("yyyyMMdd_HHmmss"),
                AppVersion,
                SystemInfo.deviceModel);
            telemetry.Event("session_state")
                .Num("session_no", state.sessionCount)
                .Num("match_index", state.matchIndex)
                .Num("money", state.money)
                .Num("price", state.ticketPrice).Send();

            camRig = CameraRig.Create(bal);
            pitch = PitchView.Create(bal);
            ui = UiShell.Create(bal);
            director = gameObject.AddComponent<ModelMatchDirector>();
            director.Init(bal);

            WireUi();
            WireDirector();
            ShowPreMatch();
        }

        // ---------------------------------------------------------------- kablolama

        void WireUi()
        {
            ui.OnTacticSelected = id =>
            {
                if (bus.Send(GreyboxCommandBus.ActSelectTactic, GreyboxJson.Payload("tacticId", id)) == RejectionReason.None)
                    ui.SetTacticHighlight(state.tacticId);
            };
            ui.OnStartMatch = StartMatch;
            ui.OnSpeedSelected = s =>
            {
                director.SetSpeed(s);
                ui.SetModelSpeedHighlight(s);
                telemetry.Event("speed").Num("match", state.matchIndex).Num("speed", s).Send();
            };
            ui.OnSkipPressed = () =>
            {
                director.SkipCurrent();
                telemetry.Event("skip").Num("match", state.matchIndex)
                         .Num("block", director.Model != null ? director.Model.CurrentBlock : -1).Send();
            };
            ui.OnPriceChanged = p =>
            {
                if (bus.Send(GreyboxCommandBus.ActSetTicketPrice, GreyboxJson.Payload("price", p)) == RejectionReason.None)
                {
                    priceDirty = true;
                    ui.UpdateProjection(ProjectionLine(state.ticketPrice));
                }
            };
            ui.OnNextMatch = NextMatch;

            // Maç içi müdahaleler — Tek Kapı'dan (Model Maçı çekirdek dopamini)
            ui.OnTacticCycle = () => Intervene(GreyboxCommandBus.ActModelTactic,
                GreyboxJson.Payload("tacticId", (director.Model.TacticId + 1) % bal.taktikler.Length),
                "Taktik değişti: " + bal.taktikler[(director.Model.TacticId + 1) % bal.taktikler.Length].ad);
            ui.OnTempoRaise = () => Intervene(GreyboxCommandBus.ActModelTempo,
                GreyboxJson.Payload("mode", (int)TempoMode.Yukselt), "Tempo yükseldi — risk iki yönlü arttı");
            ui.OnTempoLock = () => Intervene(GreyboxCommandBus.ActModelTempo,
                GreyboxJson.Payload("mode", (int)TempoMode.Kilitlen), "Kilitlendik — maç soğutuluyor");
        }

        void Intervene(string action, byte[] payload, string feedLine)
        {
            if (!matchRunning || director.Model == null) return;
            var before = director.Model.ComputeWinProb();
            var r = bus.Send(action, payload);
            if (r == RejectionReason.NoChargesLeft)
            {
                ui.PushFeed("— Hamle hakkın bitti —");
                return;
            }
            if (r != RejectionReason.None) return;

            var after = director.Model.ComputeWinProb();
            ui.SetWinProb(after);
            ui.SetInterventionState(TacticName(director.Model.TacticId), (int)director.Model.Tempo, director.Model.MovesLeft);
            ui.PushFeed($"⚡ {feedLine}  (G %{before.Win * 100f:0} → %{after.Win * 100f:0})");
            moveLog.Add($"{feedLine} (G %{before.Win * 100f:0} → %{after.Win * 100f:0})");
            ui.SetStatsLine(StatsLine());
            telemetry.Event("intervention").Num("match", state.matchIndex)
                     .Str("action", action)
                     .Num("win_before", before.Win).Num("win_after", after.Win)
                     .Num("moves_left", director.Model.MovesLeft).Send();
        }

        string TacticName(int id)
        {
            for (int i = 0; i < bal.taktikler.Length; i++)
                if (bal.taktikler[i].id == id) return bal.taktikler[i].ad;
            return bal.taktikler[0].ad;
        }

        float FormNet()
        {
            float net = 0f;
            for (int i = 0; i < state.lastResults.Length; i++) net += state.lastResults[i];
            return net;
        }

        string StatsLine()
        {
            var mo = director.Model;
            return $"xG {mo.XgUs:0.0} - {mo.XgThem:0.0}   ·   Tehlike {mo.DangerUs}-{mo.DangerThem}   ·   Hamle {bal.model.hamleHakki - mo.MovesLeft}/{bal.model.hamleHakki}";
        }

        string BuildStatsDetail()
        {
            var mo = director.Model;
            if (mo == null) return "";
            var f = mo.Factors(us: true);
            var sb = new System.Text.StringBuilder(600);
            sb.AppendLine($"{GreyboxWorld.PlayerClubName} {mo.GoalsUs} - {mo.GoalsThem} {opponentName}");
            sb.AppendLine();
            sb.AppendLine($"Beklenen gol (model-xG):  {mo.XgUs:0.00}  -  {mo.XgThem:0.00}");
            sb.AppendLine($"Tehlikeli atak:  {mo.DangerUs}  -  {mo.DangerThem}");
            sb.AppendLine($"Momentum (şu an):  {(mo.Momentum >= 0 ? "+" : "")}{mo.Momentum:0.00}");
            sb.AppendLine($"Taktik: {TacticName(mo.TacticId)}  ·  Tempo modu: {(mo.Tempo == TempoMode.Yukselt ? "Yüksek" : mo.Tempo == TempoMode.Kilitlen ? "Kilit" : "Normal")}");
            sb.AppendLine($"Kalan hamle: {mo.MovesLeft}/{bal.model.hamleHakki}");
            sb.AppendLine();
            sb.AppendLine("GOLLER");
            if (goalLog.Count == 0) sb.AppendLine("  — henüz gol yok —");
            foreach (var g in goalLog) sb.AppendLine("  ⚽ " + g);
            sb.AppendLine();
            sb.AppendLine("MÜDAHALELER");
            if (moveLog.Count == 0) sb.AppendLine("  — henüz hamle yapılmadı —");
            foreach (var mv in moveLog) sb.AppendLine("  ⚡ " + mv);
            sb.AppendLine();
            sb.AppendLine($"Sıradaki blok etkenleri (BİZ): güç ×{f.Guc:0.00} · taktik ×{f.Taktik:0.00} · faz ×{f.Faz:0.00}");
            sb.AppendLine($"momentum ×{f.Momentum:0.00} · skor ×{f.Skor:0.00} · ev ×{f.Ev:0.00} · form ×{f.Form:0.00}");
            return sb.ToString();
        }

        /// <summary>Blok kartı etken satırı: 1'den sapan çarpanlar — "neye göre?" şeffaflığı.</summary>
        string FactorLine()
        {
            var f = director.Model.Factors(us: true);
            var parts = new List<string>(6);
            AddFactor(parts, "güç", f.Guc);
            AddFactor(parts, "taktik", f.Taktik);
            AddFactor(parts, "faz", f.Faz);
            AddFactor(parts, "momentum", f.Momentum);
            AddFactor(parts, "skor", f.Skor);
            AddFactor(parts, "tempo", f.TempoModu);
            AddFactor(parts, "form", f.Form);
            return parts.Count == 0 ? "etkenler dengede" : "etkenler: " + string.Join(" · ", parts);
        }

        static void AddFactor(List<string> parts, string name, float v)
        {
            if (Mathf.Abs(v - 1f) < 0.015f) return;
            parts.Add($"{name} ×{v:0.00}");
        }

        void WireDirector()
        {
            director.BlockPreviewShown += (pv, strip) =>
            {
                ui.SetScoreBlockLine(HomeShort, awayShort, director.Model.GoalsUs, director.Model.GoalsThem,
                    pv.Index, director.Model.BlockCount, director.Model.BlockMinute(pv.Index));
                ui.SetWinProb(strip);
                ui.ShowBlockCard(pv.Index, director.Model.BlockCount,
                    director.Model.BlockMinute(pv.Index), director.Model.BlockMinute(pv.Index + 1),
                    pv.PGoalUs, pv.PGoalThem, FactorLine());
                momentumHistory.Add(pv.Momentum);
                ui.SetMomentumHistory(momentumHistory);
            };

            director.BlockResolved += (idx, outcome, strip) =>
            {
                int minute = director.Model.BlockMinute(idx + 1);
                switch (outcome)
                {
                    case BlockOutcome.GoalUs:
                        ui.PushFeed($"{minute}' ⚽ GOOOL! {HomeShort} ağları havalandırdı! ({director.Model.GoalsUs}-{director.Model.GoalsThem})");
                        goalLog.Add($"{minute}' — {HomeShort} ({director.Model.GoalsUs}-{director.Model.GoalsThem})");
                        break;
                    case BlockOutcome.GoalThem:
                        ui.PushFeed($"{minute}' ⚽ Gol yedik... {awayShort} skoru yakaladı ({director.Model.GoalsUs}-{director.Model.GoalsThem})");
                        goalLog.Add($"{minute}' — {awayShort} ({director.Model.GoalsUs}-{director.Model.GoalsThem})");
                        break;
                    case BlockOutcome.Danger:
                        ui.PushFeed(director.Model.LastDangerSide == 0
                            ? $"{minute}' Tehlikeli atak — {HomeShort} baskın, son dokunuş eksik!"
                            : $"{minute}' {awayShort} tehlikeli geldi — savunma son anda sıyırdı!");
                        break;
                    default:
                        ui.PushFeed($"{minute}' Kontrollü oyun, orta saha mücadelesi");
                        break;
                }
                ui.SetWinProb(strip);
                ui.SetStatsLine(StatsLine());
                ui.SetScoreBlockLine(HomeShort, awayShort, director.Model.GoalsUs, director.Model.GoalsThem,
                    idx + 1, director.Model.BlockCount, minute);
                telemetry.Event("block_result").Num("match", state.matchIndex)
                         .Num("block", idx).Str("outcome", outcome.ToString())
                         .Num("win", strip.Win).Send();
            };

            director.VignetteToggled += on =>
            {
                pitch.gameObject.SetActive(on);
                ui.SetModelWidgetsVisible(!on);
                if (!on) ui.ShowModelScreen();
            };

            director.VignetteFramePlayed += f =>
            {
                pitch.RenderFrame(f);
                if (f.GoalMoment)
                {
                    camRig.Shake();
                    ui.GoalFlash($"{HomeShort} {director.Model.GoalsUs} - {director.Model.GoalsThem} {awayShort}");
#if UNITY_IOS || UNITY_ANDROID
                    if (bal.vurgu.titresimAktif) Handheld.Vibrate();
#endif
                }
            };

            director.MatchFinished += OnMatchFinished;
        }

        // ---------------------------------------------------------------- core loop adımları

        void ShowPreMatch()
        {
            var setup = GreyboxWorld.BuildMatch(bal, (ulong)state.worldSeed, state.matchIndex, state.tacticId);
            opponentName = GreyboxWorld.OpponentName(state.matchIndex);
            awayShort = opponentName.Split(' ')[0].ToUpperInvariant();

            var proj = TycoonEconomy.Project(bal.ekonomi, state.ticketPrice, state.lastResults, 0);
            string ticketLine = $"Bilet {state.ticketPrice:0} kr → tahmini {proj.Attendance:N0} seyirci (doluluk %{proj.Occupancy * 100f:0})";

            pitch.gameObject.SetActive(true); // arka fon sahası
            ui.ShowPreMatch(state.matchIndex, opponentName, setup.AwayStrength, state.money,
                GreyboxWorld.Squad, bal.taktikler, state.tacticId, ticketLine);
        }

        void StartMatch()
        {
            ui.HidePreMatch();
            pitch.gameObject.SetActive(false); // ana ekran MODEL (Sahneleme §0)
            ui.ShowModelScreen();
            momentumHistory.Clear();
            goalLog.Clear();
            moveLog.Clear();
            ui.StatsDetailProvider = BuildStatsDetail;

            currentSetup = GreyboxWorld.BuildMatch(bal, (ulong)state.worldSeed, state.matchIndex, state.tacticId);
            currentSetup.HomeFormNet = FormNet(); // son 5 maç formu model etkeni (GREYBOX_MODEL.md)
            director.StartMatch(currentSetup);
            bus.ActiveModel = director.Model;
            matchRunning = true;

            ui.SetModelSpeedHighlight(1);
            ui.SetInterventionState(TacticName(director.Model.TacticId), (int)director.Model.Tempo, director.Model.MovesLeft);
            ui.SetWinProb(director.Model.ComputeWinProb());
            ui.SetStatsLine(StatsLine());
            ui.PushFeed($"Maç başladı: {GreyboxWorld.PlayerClubName} — {opponentName} (rakip gücü {currentSetup.AwayStrength:0})");

            telemetry.Event("match_start")
                .Num("match", state.matchIndex)
                .Str("opp", opponentName)
                .Num("opp_strength", currentSetup.AwayStrength)
                .Str("tactic", TacticName(state.tacticId))
                .Num("price", state.ticketPrice).Send();
        }

        void OnMatchFinished()
        {
            matchRunning = false;
            matchesEndedThisSession++;
            bus.ActiveModel = null;
            var model = director.Model;
            int gu = model.GoalsUs, gt = model.GoalsThem;
            int result = gu > gt ? 1 : (gu == gt ? 0 : -1);

            var proj = state.Settle(bal, result); // gelir kasaya + form penceresi (sistem akışı)
            SaveService.Save(state);

            telemetry.Event("match_end")
                .Num("match", state.matchIndex)
                .Str("score", $"{gu}-{gt}")
                .Str("result", result > 0 ? "W" : result == 0 ? "D" : "L")
                .Num("watch_real_sec", director.WatchRealSeconds)
                .Num("skips", director.SkipCount)
                .Num("speed_changes", director.SpeedChangeCount)
                .Num("moves_used", bal.model.hamleHakki - model.MovesLeft)
                .Num("attendance", proj.Attendance)
                .Num("income", proj.Total)
                .Num("money_after", state.money).Send();

            ui.HideModelScreen();
            string lineA = $"Seyirci: {proj.Attendance:N0} (%{proj.Occupancy * 100f:0})  ·  Bilet geliri: {UiWidgets.Money(proj.TicketIncome)}";
            string lineB = $"Sonuç primi: {UiWidgets.Money(proj.ResultBonus)}  ·  Toplam: +{UiWidgets.Money(proj.Total)}";
            ui.ShowPostMatch(gu, gt, result, lineA, lineB, state.money, state.ticketPrice,
                ProjectionLine(state.ticketPrice));

            postShownAt = Time.realtimeSinceStartup;
            priceDirty = false;
            director.StopMatch();
        }

        void NextMatch()
        {
            if (priceDirty)
                telemetry.Event("ticket_price_set").Num("match", state.matchIndex)
                         .Num("new_price", state.ticketPrice).Send();
            telemetry.Event("next_match_click").Num("match", state.matchIndex)
                     .Num("since_end_sec", Time.realtimeSinceStartup - postShownAt).Send();

            bus.Send(GreyboxCommandBus.ActNextMatch, null);
            ui.HidePostMatch();
            ShowPreMatch();
        }

        string ProjectionLine(float price)
        {
            var proj = TycoonEconomy.Project(bal.ekonomi, price, state.lastResults, 0);
            return $"{price:0} kr → ~{proj.Attendance:N0} seyirci (%{proj.Occupancy * 100f:0})\n~{UiWidgets.Money(proj.TicketIncome)} bilet geliri";
        }

        // ---------------------------------------------------------------- oturum kapanışı

        void OnApplicationPause(bool paused)
        {
            if (paused && telemetry != null)
                telemetry.Event("session_pause").Num("matches_played", matchesEndedThisSession).Send();
        }

        void OnDestroy()
        {
            if (telemetry == null) return;
            telemetry.Event("session_end")
                .Num("matches_played", matchesEndedThisSession)
                .Num("money", state != null ? state.money : 0).Send();
            telemetry.Dispose();
            telemetry = null;
        }
    }
}
