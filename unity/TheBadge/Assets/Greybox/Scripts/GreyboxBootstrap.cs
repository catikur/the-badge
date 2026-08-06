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

        void WireDirector()
        {
            director.BlockPreviewShown += (pv, strip) =>
            {
                ui.SetScoreBlockLine(HomeShort, awayShort, director.Model.GoalsUs, director.Model.GoalsThem,
                    pv.Index, director.Model.BlockCount, director.Model.BlockMinute(pv.Index));
                ui.SetWinProb(strip);
                ui.ShowBlockCard(pv.Index, director.Model.BlockCount,
                    director.Model.BlockMinute(pv.Index), director.Model.BlockMinute(pv.Index + 1),
                    pv.PGoalUs, pv.PGoalThem);
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
                        break;
                    case BlockOutcome.GoalThem:
                        ui.PushFeed($"{minute}' ⚽ Gol yedik... {awayShort} skoru yakaladı ({director.Model.GoalsUs}-{director.Model.GoalsThem})");
                        break;
                    case BlockOutcome.Danger:
                        ui.PushFeed($"{minute}' Tehlikeli dakikalar — pozisyonlar karşılıklı, gol yok");
                        break;
                    default:
                        ui.PushFeed($"{minute}' Kontrollü oyun, orta saha mücadelesi");
                        break;
                }
                ui.SetWinProb(strip);
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

            currentSetup = GreyboxWorld.BuildMatch(bal, (ulong)state.worldSeed, state.matchIndex, state.tacticId);
            director.StartMatch(currentSetup);
            bus.ActiveModel = director.Model;
            matchRunning = true;

            ui.SetModelSpeedHighlight(1);
            ui.SetInterventionState(TacticName(director.Model.TacticId), (int)director.Model.Tempo, director.Model.MovesLeft);
            ui.SetWinProb(director.Model.ComputeWinProb());
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
