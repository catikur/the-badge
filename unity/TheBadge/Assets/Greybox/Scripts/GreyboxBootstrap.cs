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
        readonly List<string> incidentLog = new List<string>(); // kart/sakatlık günlüğü (İt.11)

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

            // Kadro müdahaleleri (İt.11) — değişiklik hakkı hamleden AYRI havuz
            ui.OnOpenSubPicker = OpenSubPicker;
            ui.OnSubstitution = DoSubstitution;
            ui.OnContinueShort = DoContinueShort;
        }

        static string PosName(PlayerPos pos) =>
            pos == PlayerPos.GK ? "KL" : pos == PlayerPos.DF ? "DF" : pos == PlayerPos.MF ? "OS" : "FV";

        string PlayerRow(SquadPlayer p)
        {
            // Güç + enerji birlikte: "yorgun yıldız mı, taze vasat mı?" kararı görünür (İt.12)
            string marks = p.Yellow > 0 ? " [S]" : "";
            return $"{PosName(p.Pos)}  {p.Name} {p.Guc:0}  ·  %{p.Energy / bal.squad.enerjiBaslangic * 100f:0}{marks}";
        }

        void BuildSquadOptions(out int[] outIds, out string[] outLabels, out int[] inIds, out string[] inLabels)
        {
            var mo = director.Model;
            var outs = new List<SquadPlayer>(10);
            var ins = new List<SquadPlayer>(5);
            foreach (var p in mo.SquadUs.Players)
            {
                if (p.Pos == PlayerPos.GK) continue;
                if (p.OnPitch) outs.Add(p);
                else if (p.Id >= 11 && !p.Injured && !p.SentOff) ins.Add(p);
            }
            outs.Sort((a, b) => a.Energy.CompareTo(b.Energy)); // en yorgun üstte — koç gözü
            outIds = new int[outs.Count]; outLabels = new string[outs.Count];
            for (int i = 0; i < outs.Count; i++) { outIds[i] = outs[i].Id; outLabels[i] = PlayerRow(outs[i]); }
            inIds = new int[ins.Count]; inLabels = new string[ins.Count];
            for (int i = 0; i < ins.Count; i++) { inIds[i] = ins[i].Id; inLabels[i] = PlayerRow(ins[i]); }
        }

        void OpenSubPicker()
        {
            var mo = director.Model;
            if (!matchRunning || mo == null || mo.HasPendingDecision) return; // sakatlıkta kendi paneli açık
            if (mo.SubsLeft <= 0) { ui.PushFeed("— Değişiklik hakkın bitti —"); return; }
            BuildSquadOptions(out var outIds, out var outLabels, out var inIds, out var inLabels);
            if (inIds.Length == 0) { ui.PushFeed("— Kulübede uygun yedek kalmadı —"); return; }
            ui.ShowSubPicker(outIds, outLabels, inIds, inLabels);
        }

        void DoSubstitution(int outId, int inId)
        {
            var mo = director.Model;
            if (!matchRunning || mo == null) return;
            var before = mo.ComputeWinProb();
            var pOut = mo.SquadUs.Find(outId);
            var pIn = mo.SquadUs.Find(inId);
            var r = bus.Send(GreyboxCommandBus.ActModelSub, GreyboxJson.Payload2("out", outId, "in", inId));
            ui.HideSubPicker();
            if (r == RejectionReason.NoChargesLeft) { ui.PushFeed("— Değişiklik hakkın bitti —"); return; }
            if (r != RejectionReason.None || pOut == null || pIn == null) return;

            var after = mo.ComputeWinProb();
            int minute = mo.BlockMinute(Mathf.Min(mo.CurrentBlock, mo.BlockCount));
            ui.SetWinProb(after);
            ui.SetSubState(mo.SubsLeft);
            ui.SetStatsLine(StatsLine());
            ui.PushFeed($"{minute}' DEĞİŞİKLİK: {pOut.Name} → {pIn.Name}  (G %{before.Win * 100f:0} → %{after.Win * 100f:0})");
            moveLog.Add($"Değişiklik: {pOut.Name} → {pIn.Name} (G %{before.Win * 100f:0} → %{after.Win * 100f:0})");
            telemetry.Event("substitution").Num("match", state.matchIndex)
                     .Str("out", pOut.Name).Str("in", pIn.Name)
                     .Num("win_before", before.Win).Num("win_after", after.Win)
                     .Num("subs_left", mo.SubsLeft).Send();
        }

        void DoContinueShort()
        {
            var mo = director.Model;
            if (mo == null) return;
            if (bus.Send(GreyboxCommandBus.ActModelContinueShort, null) != RejectionReason.None) return;
            var after = mo.ComputeWinProb();
            ui.SetWinProb(after);
            ui.SetStatsLine(StatsLine());
            ui.PushFeed($"— Eksik devam ediyoruz; hücum zayıfladı (G %{after.Win * 100f:0}) —");
            moveLog.Add("Sakatlıkta eksik devam kararı");
            telemetry.Event("continue_short").Num("match", state.matchIndex).Num("win_after", after.Win).Send();
        }

        void AnnounceIncident(Incident inc, int minute)
        {
            var mo = director.Model;
            var squad = inc.Team == 0 ? mo.SquadUs : mo.SquadThem;
            string name = squad.Find(inc.PlayerId) != null ? squad.Find(inc.PlayerId).Name : "?";
            string side = inc.Team == 0 ? HomeShort : awayShort;
            string line;
            switch (inc.Type)
            {
                case IncidentType.Yellow:
                    line = $"{minute}' SARI KART — {name} ({side})";
                    break;
                case IncidentType.SecondYellowRed:
                    line = $"{minute}' İKİNCİ SARIDAN KIRMIZI! {name} ({side}) — {(inc.Team == 0 ? "10 kişiyiz!" : "rakip 10 kişi!")}";
                    break;
                case IncidentType.RedDirect:
                    line = $"{minute}' KIRMIZI KART! {name} ({side}) — {(inc.Team == 0 ? "10 kişiyiz!" : "rakip 10 kişi!")}";
                    break;
                default: // Injury
                    if (inc.Team == 0)
                        line = mo.HasPendingDecision
                            ? $"{minute}' SAKATLIK — {name} devam edemiyor; karar bekleniyor..."
                            : $"{minute}' SAKATLIK — {name} çıktı; hak/yedek yok, eksik devam ediyoruz";
                    else
                    {
                        var subIn = inc.AutoSubInId >= 0 ? mo.SquadThem.Find(inc.AutoSubInId) : null;
                        line = subIn != null
                            ? $"{minute}' Rakipte sakatlık: {name} çıktı, {subIn.Name} girdi"
                            : $"{minute}' Rakipte sakatlık: {name} çıktı — rakip eksik kaldı";
                    }
                    break;
            }
            ui.PushFeed(line);
            incidentLog.Add(line);
            telemetry.Event("incident").Num("match", state.matchIndex)
                     .Str("type", inc.Type.ToString()).Num("team", inc.Team)
                     .Str("player", name).Num("block", inc.Block).Send();
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
            float ePct = mo.SquadUs.TeamEnergyMean() / bal.squad.enerjiBaslangic * 100f;
            return $"xG {mo.XgUs:0.0}-{mo.XgThem:0.0} · Teh {mo.DangerUs}-{mo.DangerThem} · E %{ePct:0}" +
                   $" · H {bal.model.hamleHakki - mo.MovesLeft}/{bal.model.hamleHakki}" +
                   $" · D {bal.squad.degisiklikHakki - mo.SubsLeft}/{bal.squad.degisiklikHakki}";
        }

        static string CardLine(Squad s)
        {
            int y = 0, r = 0;
            foreach (var p in s.Players) { y += p.Yellow; if (p.SentOff) r++; }
            return $"{y}S/{r}K";
        }

        static int InjuryCount(Squad s)
        {
            int n = 0;
            foreach (var p in s.Players) if (p.Injured) n++;
            return n;
        }

        string SquadRow(SquadPlayer p)
        {
            string status = p.SentOff ? "  · KIRMIZI" : p.Injured ? "  · SAKAT"
                          : p.OnPitch ? "" : (p.Id >= 11 ? "  · yedek" : "  · çıktı");
            string marks = "";
            for (int k = 0; k < p.Yellow; k++) marks += " [S]";
            if (p.Goals > 0) marks += $"  ⚽{p.Goals}";
            return $"{PosName(p.Pos)}  {p.Name} {p.Guc:0}  ·  %{p.Energy / bal.squad.enerjiBaslangic * 100f:0}{marks}{status}";
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
            sb.AppendLine($"Kart:  {CardLine(mo.SquadUs)}  -  {CardLine(mo.SquadThem)}");
            sb.AppendLine($"Sakatlık:  {InjuryCount(mo.SquadUs)}  -  {InjuryCount(mo.SquadThem)}");
            sb.AppendLine($"Takım enerjisi:  %{mo.SquadUs.TeamEnergyMean() / bal.squad.enerjiBaslangic * 100f:0}  -  %{mo.SquadThem.TeamEnergyMean() / bal.squad.enerjiBaslangic * 100f:0}");
            mo.GetRatings(out float rAtkU, out float rDefU, out float rAtkT, out float rDefT);
            sb.AppendLine($"Hücum / Savunma reytingi:  {rAtkU:0}/{rDefU:0}  -  {rAtkT:0}/{rDefT:0}");
            sb.AppendLine($"Momentum (şu an):  {(mo.Momentum >= 0 ? "+" : "")}{mo.Momentum:0.00}");
            sb.AppendLine($"Taktik: {TacticName(mo.TacticId)}  ·  Tempo modu: {(mo.Tempo == TempoMode.Yukselt ? "Yüksek" : mo.Tempo == TempoMode.Kilitlen ? "Kilit" : "Normal")}");
            sb.AppendLine($"Hamle: {bal.model.hamleHakki - mo.MovesLeft}/{bal.model.hamleHakki}  ·  Değişiklik: {bal.squad.degisiklikHakki - mo.SubsLeft}/{bal.squad.degisiklikHakki}");
            sb.AppendLine();
            sb.AppendLine("GOLLER");
            if (goalLog.Count == 0) sb.AppendLine("  — henüz gol yok —");
            foreach (var g in goalLog) sb.AppendLine("  ⚽ " + g);
            sb.AppendLine();
            sb.AppendLine("OLAYLAR (kart / sakatlık)");
            if (incidentLog.Count == 0) sb.AppendLine("  — olay yok —");
            foreach (var ev in incidentLog) sb.AppendLine("  ▪ " + ev);
            sb.AppendLine();
            sb.AppendLine("MÜDAHALELER");
            if (moveLog.Count == 0) sb.AppendLine("  — henüz hamle yapılmadı —");
            foreach (var mv in moveLog) sb.AppendLine("  ⚡ " + mv);
            sb.AppendLine();
            sb.AppendLine("KADRO — ROZET SK (enerji · durum)");
            foreach (var p in mo.SquadUs.Players) sb.AppendLine("  " + SquadRow(p));
            sb.AppendLine();
            sb.AppendLine($"Sıradaki blok etkenleri (BİZ): güç ×{f.Guc:0.00} · taktik ×{f.Taktik:0.00} · faz ×{f.Faz:0.00}");
            sb.AppendLine($"momentum ×{f.Momentum:0.00} · skor ×{f.Skor:0.00} · ev ×{f.Ev:0.00} · form ×{f.Form:0.00}");
            sb.AppendLine("(güç etkeni = Hücum reytingimiz vs rakip Savunma reytingi; enerji,");
            sb.AppendLine(" eksikler ve KALECİ bu reytinglerin içindedir — GREYBOX_MODEL.md v3)");
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
                string scorer = director.Model.LastScorerName;
                switch (outcome)
                {
                    case BlockOutcome.GoalUs:
                        ui.PushFeed($"{minute}' ⚽ GOOOL! {scorer ?? HomeShort} ağları havalandırdı! ({director.Model.GoalsUs}-{director.Model.GoalsThem})");
                        goalLog.Add($"{minute}' {scorer ?? HomeShort} — {HomeShort} ({director.Model.GoalsUs}-{director.Model.GoalsThem})");
                        break;
                    case BlockOutcome.GoalThem:
                        ui.PushFeed($"{minute}' ⚽ Gol yedik... {scorer ?? awayShort} ({awayShort}) skoru yakaladı ({director.Model.GoalsUs}-{director.Model.GoalsThem})");
                        goalLog.Add($"{minute}' {scorer ?? awayShort} — {awayShort} ({director.Model.GoalsUs}-{director.Model.GoalsThem})");
                        break;
                    case BlockOutcome.Danger:
                        // Kaleci artık İSİMLE kurtarır (İt.12 lezzeti — Players[0] = GK)
                        ui.PushFeed(director.Model.LastDangerSide == 0
                            ? $"{minute}' Tehlikeli atak — {HomeShort} baskın, {director.Model.SquadThem.Players[0].Name} açıyı kapattı!"
                            : $"{minute}' {awayShort} tehlikeli geldi — {director.Model.SquadUs.Players[0].Name} kurtardı!");
                        break;
                    default:
                        ui.PushFeed($"{minute}' Kontrollü oyun, orta saha mücadelesi");
                        break;
                }
                // Kart/sakatlık olayları (İt.11): feed + olay günlüğü + telemetri
                foreach (var inc in director.Model.LastBlockIncidents)
                    AnnounceIncident(inc, minute);
                ui.SetWinProb(strip);
                ui.SetStatsLine(StatsLine());
                ui.SetSubState(director.Model.SubsLeft);
                ui.SetScoreBlockLine(HomeShort, awayShort, director.Model.GoalsUs, director.Model.GoalsThem,
                    idx + 1, director.Model.BlockCount, minute);
                telemetry.Event("block_result").Num("match", state.matchIndex)
                         .Num("block", idx).Str("outcome", outcome.ToString())
                         .Num("win", strip.Win).Send();
            };

            // Sakatlıkta zorunlu karar — akış panel çözülene dek durur (İt.11 A2)
            director.DecisionRequired += () =>
            {
                var mo = director.Model;
                var inc = mo.PendingIncident;
                var injured = mo.SquadUs.Find(inc.PlayerId);
                int minute = mo.BlockMinute(Mathf.Min(inc.Block + 1, mo.BlockCount));
                BuildSquadOptions(out _, out _, out var inIds, out var inLabels);
                ui.ShowIncidentDecision(
                    $"SAKATLIK — {minute}'",
                    $"{injured.Name} ({PosName(injured.Pos)}) devam edemiyor.\nDeğişiklik hakkı: {mo.SubsLeft} — kimi alalım?",
                    inc.PlayerId, inIds, inLabels);
            };
            director.DecisionResolved += () => ui.HideIncidentPanel();

            director.VignetteToggled += on =>
            {
                pitch.gameObject.SetActive(on);
                // ShowModelScreen ÇAĞRILMAZ: o feed/istatistiği sıfırlar (yalnız maç başında);
                // vinyet dönüşünde ekran içeriğiyle birlikte geri gelir (İt.11 düzeltmesi)
                ui.SetModelWidgetsVisible(!on);
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

            // Kadro maç modeliyle AYNI tohumdan üretilir — maç öncesi isim/güçler maç içiyle birebir
            var squadPreview = Squad.Generate(setup.Seed, 0, bal.squad, setup.HomeStrength);
            var rows = new string[12];
            for (int i = 0; i < 11; i++)
                rows[i] = $"{PosName(squadPreview.Players[i].Pos)}  {squadPreview.Players[i].Name}  {squadPreview.Players[i].Guc:0}";
            var benchNames = new List<string>(5);
            for (int i = 11; i < 16; i++)
                benchNames.Add($"{squadPreview.Players[i].Name} {squadPreview.Players[i].Guc:0}");
            rows[11] = "Yedek: " + string.Join(", ", benchNames);

            pitch.gameObject.SetActive(true); // arka fon sahası
            ui.ShowPreMatch(state.matchIndex, opponentName, setup.AwayStrength, state.money,
                rows, bal.taktikler, state.tacticId, ticketLine);
        }

        void StartMatch()
        {
            ui.HidePreMatch();
            pitch.gameObject.SetActive(false); // ana ekran MODEL (Sahneleme §0)
            ui.ShowModelScreen();
            momentumHistory.Clear();
            goalLog.Clear();
            moveLog.Clear();
            incidentLog.Clear();
            ui.StatsDetailProvider = BuildStatsDetail;

            currentSetup = GreyboxWorld.BuildMatch(bal, (ulong)state.worldSeed, state.matchIndex, state.tacticId);
            currentSetup.HomeFormNet = FormNet(); // son 5 maç formu model etkeni (GREYBOX_MODEL.md)
            director.StartMatch(currentSetup);
            bus.ActiveModel = director.Model;
            matchRunning = true;

            ui.SetModelSpeedHighlight(1);
            ui.SetInterventionState(TacticName(director.Model.TacticId), (int)director.Model.Tempo, director.Model.MovesLeft);
            ui.SetSubState(director.Model.SubsLeft);
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
                .Num("subs_used", bal.squad.degisiklikHakki - model.SubsLeft)
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
