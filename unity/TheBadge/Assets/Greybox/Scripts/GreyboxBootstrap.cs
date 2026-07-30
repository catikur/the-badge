using System;
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
    /// Greybox giriş noktası — sahnedeki TEK obje. Kamera, saha, UI ve maç sürücüsünü
    /// runtime'da kurar; core loop'u yönetir: Maç öncesi → Maç → Maç sonu → Sonraki maç (Brif K3).
    /// Kullanıcı eylemleri UI callback'i → CommandEnvelope → GreyboxCommandBus yolunu izler (Tek Kapı).
    /// </summary>
    public sealed class GreyboxBootstrap : MonoBehaviour
    {
        const string AppVersion = "greybox-0.1.0";

        GreyboxBalance bal;
        GreyboxState state;
        GreyboxCommandBus bus;
        TelemetryLog telemetry;
        UiShell ui;
        PitchView pitch;
        CameraRig camRig;
        MatchDirector director;

        string opponentName = "";
        string awayShort = "";
        bool matchRunning;
        float postShownAt;
        bool priceDirty;
        int matchesEndedThisSession;

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
                state.worldSeed = DateTime.Now.Ticks; // sim dışı tohum üretimi — determinizm borcu FAZ 03 (Brif K5)
            state.sessionCount++;
            SaveService.Save(state);

            bus = new GreyboxCommandBus(bal, state);
            bus.Applied += _ => SaveService.Save(state); // uygulanan her komut kalıcı

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
            director = gameObject.AddComponent<MatchDirector>();
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
                ui.SetSpeedHighlight(s);
            };
            ui.OnSkipPressed = () => director.SkipToKeyMoment();
            ui.OnPriceChanged = p =>
            {
                if (bus.Send(GreyboxCommandBus.ActSetTicketPrice, GreyboxJson.Payload("price", p)) == RejectionReason.None)
                {
                    priceDirty = true;
                    ui.UpdateProjection(ProjectionLine(state.ticketPrice));
                }
            };
            ui.OnNextMatch = NextMatch;
        }

        void WireDirector()
        {
            director.EventRaised += OnFlowEvent;
            director.MatchFinished += OnMatchFinished;
            director.SpeedChanged += s =>
                telemetry.Event("speed").Num("match", state.matchIndex)
                         .Num("minute", director.Sim != null ? director.Sim.MatchMinute : 0f)
                         .Num("speed", s).Send();
            director.Skipped += (fromMin, toMin) =>
                telemetry.Event("skip").Num("match", state.matchIndex)
                         .Num("minute", fromMin).Num("to_minute", toMin).Send();
        }

        void Update()
        {
            if (!matchRunning || director.Sim == null) return;
            pitch.Render(director.Sim);
            ui.SetScoreLine(HomeShort, awayShort,
                director.Sim.HomeScore, director.Sim.AwayScore, director.Sim.MatchMinute);
        }

        // ---------------------------------------------------------------- core loop adımları

        void ShowPreMatch()
        {
            // Rakip önizlemesi StartMatch'teki kurulumla AYNI tohumdan gelir → tutarlı
            var setup = GreyboxWorld.BuildMatch(bal, (ulong)state.worldSeed, state.matchIndex, state.tacticId);
            opponentName = GreyboxWorld.OpponentName(state.matchIndex);
            awayShort = opponentName.Split(' ')[0].ToUpperInvariant();

            var proj = TycoonEconomy.Project(bal.ekonomi, state.ticketPrice, state.lastResults, 0);
            string ticketLine = $"Bilet {state.ticketPrice:0} kr → tahmini {proj.Attendance:N0} seyirci (doluluk %{proj.Occupancy * 100f:0})";

            ui.ShowPreMatch(state.matchIndex, opponentName, setup.AwayStrength, state.money,
                GreyboxWorld.Squad, bal.taktikler, state.tacticId, ticketLine);
        }

        void StartMatch()
        {
            ui.HidePreMatch();
            ui.ShowHud();

            var setup = GreyboxWorld.BuildMatch(bal, (ulong)state.worldSeed, state.matchIndex, state.tacticId);
            director.StartMatch(setup);
            matchRunning = true;

            telemetry.Event("match_start")
                .Num("match", state.matchIndex)
                .Str("opp", opponentName)
                .Num("opp_strength", setup.AwayStrength)
                .Str("tactic", bal.taktikler[Mathf.Clamp(state.tacticId, 0, bal.taktikler.Length - 1)].ad)
                .Num("price", state.ticketPrice).Send();

            ui.SetScoreLine(HomeShort, awayShort, 0, 0, 0f);
            ui.Banner("MAÇ BAŞLIYOR", 1.2f);
        }

        void OnFlowEvent(FlowEvent e, bool duringSkip)
        {
            switch (e.Type)
            {
                case FlowEventType.Goal:
                    string club = e.Team == 0 ? HomeShort : awayShort;
                    telemetry.Event("goal").Num("match", state.matchIndex)
                             .Num("minute", e.Minute).Str("team", e.Team == 0 ? "home" : "away")
                             .Str("score", $"{e.HomeScore}-{e.AwayScore}").Send();
                    if (duringSkip)
                    {
                        ui.Ticker($"{Mathf.FloorToInt(e.Minute)}' GOL — {club} (atlanırken)");
                    }
                    else
                    {
                        camRig.Shake();                       // vurgu: titreme (Brif K2)
                        ui.GoalFlash($"{club}  ·  {e.HomeScore} - {e.AwayScore}");
                    }
                    break;

                case FlowEventType.Shot:
                    if (!duringSkip) ui.Ticker("Şut!");
                    break;
                case FlowEventType.CornerHeader:
                    if (!duringSkip) ui.Ticker("Kafa vuruşu!");
                    break;
                case FlowEventType.Save:
                    if (!duringSkip) ui.Ticker("Kurtarış!");
                    break;
                case FlowEventType.ShotWide:
                    if (!duringSkip) ui.Ticker("Auta!");
                    break;
                case FlowEventType.Corner:
                    if (!duringSkip) ui.Ticker("Korner " + (e.Team == 0 ? HomeShort : awayShort));
                    break;
                case FlowEventType.HalfTime:
                    if (!duringSkip) ui.Banner("DEVRE ARASI", Mathf.Max(0.8f, bal.clock.devreArasiSaniye - 0.4f));
                    break;
                case FlowEventType.SecondHalfKickOff:
                    if (!duringSkip) ui.Ticker("İkinci yarı başladı");
                    break;
            }
        }

        void OnMatchFinished()
        {
            matchRunning = false;
            matchesEndedThisSession++;
            var sim = director.Sim;
            int hs = sim.HomeScore, aw = sim.AwayScore;
            int result = hs > aw ? 1 : (hs == aw ? 0 : -1);

            var proj = state.Settle(bal, result);   // gelir kasaya + form penceresine (sistem akışı, komut değil)
            SaveService.Save(state);

            telemetry.Event("match_end")
                .Num("match", state.matchIndex)
                .Str("score", $"{hs}-{aw}")
                .Str("result", result > 0 ? "W" : result == 0 ? "D" : "L")
                .Num("watch_real_sec", director.WatchRealSeconds)
                .Num("skips", director.SkipCount)
                .Num("speed_changes", director.SpeedChangeCount)
                .Num("shots", sim.Stats.TotalShots)
                .Num("corners", sim.Stats.TotalCorners)
                .Num("attendance", proj.Attendance)
                .Num("income", proj.Total)
                .Num("money_after", state.money).Send();

            ui.HideHud();
            string lineA = $"Seyirci: {proj.Attendance:N0} (%{proj.Occupancy * 100f:0})  ·  Bilet geliri: {UiWidgets.Money(proj.TicketIncome)}";
            string lineB = $"Sonuç primi: {UiWidgets.Money(proj.ResultBonus)}  ·  Toplam: +{UiWidgets.Money(proj.Total)}";
            ui.ShowPostMatch(hs, aw, result, lineA, lineB, state.money, state.ticketPrice,
                ProjectionLine(state.ticketPrice));

            postShownAt = Time.realtimeSinceStartup;
            priceDirty = false;
            director.StopMatch();
            pitch.Render(null); // son kare sahada kalır; null güvenli
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
