using System;
using System.Collections;
using TheBadge.Greybox.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace TheBadge.Greybox.UI
{
    /// <summary>
    /// Greybox UI kabuğu — Brif K3 core loop'unun üç yüzü:
    /// Maç öncesi (taktik + kadro) → Maç HUD'u (skor/dakika/hız) → Maç sonu (gelir + bilet slider'ı).
    /// Durum DEĞİŞTİRMEZ: tüm kullanıcı eylemleri callback'lerle Bootstrap'a, oradan Command Bus'a akar.
    /// </summary>
    public sealed class UiShell : MonoBehaviour
    {
        // Bootstrap tarafından bağlanan eylem kancaları (Tek Kapı'ya giden yol)
        public Action<int> OnTacticSelected;
        public Action OnStartMatch;
        public Action<int> OnSpeedSelected;
        public Action OnSkipPressed;
        public Action<float> OnPriceChanged;
        public Action OnNextMatch;
        // Model Maçı müdahaleleri (Sahneleme §0)
        public Action OnTacticCycle;
        public Action OnTempoRaise;
        public Action OnTempoLock;

        GreyboxBalance bal;
        Color accent, panelBg, btnBg, btnFg, good, bad;

        // HUD
        RectTransform hudRoot;
        Text scoreText, minuteText, tickerText;
        Button speed1Btn, speed2Btn, skipBtn;
        Coroutine tickerCo;

        // Paneller
        RectTransform preRoot, postRoot;
        Text preTitle, preOpponent, preStrength, preMoney, preSquad, preTicket;
        Image[] tacticBtnBg;
        Text[] tacticBtnLabel;

        Text postScore, postResult, postIncomeA, postIncomeB, postMoney, postProjection;
        Slider priceSlider;

        // Vurgu
        Image flashImg;
        Text flashBig, flashSmall;
        RectTransform bannerRoot;
        Text bannerText;
        Coroutine flashCo, bannerCo;

        // Model Maçı ekranı (Sahneleme §0)
        RectTransform modelRoot;
        Text mScoreLine, mBlockCard, mMovesLabel, mFeedText, mTacticBtnLabel, mTempoRaiseLabel, mTempoLockLabel;
        RectTransform stripWin, stripDraw, stripLoss;
        Text stripWinT, stripDrawT, stripLossT;
        readonly System.Collections.Generic.List<RectTransform> momBars = new System.Collections.Generic.List<RectTransform>();
        readonly System.Collections.Generic.List<string> feedLines = new System.Collections.Generic.List<string>();
        Coroutine stripCo;
        Button mSpeed1, mSpeed2;
        const float StripW = 940f;

        public static UiShell Create(GreyboxBalance bal)
        {
            var canvas = UiWidgets.MakeCanvas("GreyboxCanvas");
            var shell = canvas.gameObject.AddComponent<UiShell>();
            shell.bal = bal;
            shell.BuildAll(canvas.transform);
            return shell;
        }

        void BuildAll(Transform root)
        {
            accent = View.SpriteFactory.Hex(bal.renkler.evKaleci, new Color(1f, 0.83f, 0.31f));
            panelBg = new Color(0.03f, 0.09f, 0.05f, 0.93f);
            btnBg = new Color(1f, 1f, 1f, 0.13f);
            btnFg = new Color(0.95f, 0.97f, 0.93f);
            good = new Color(0.56f, 0.86f, 0.42f);
            bad = new Color(0.94f, 0.45f, 0.38f);

            BuildHud(root);
            BuildModelScreen(root);
            BuildFlash(root);
            BuildBanner(root);
            BuildPreMatch(root);
            BuildPostMatch(root);
        }

        // ---------------------------------------------------------------- Model Maçı ekranı

        void BuildModelScreen(Transform root)
        {
            modelRoot = UiWidgets.MakeRect("ModelScreen", root);
            UiWidgets.Stretch(modelRoot);

            mScoreLine = UiWidgets.MakeText("ScoreLine", modelRoot, "", 52, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiWidgets.TopBlock((RectTransform)mScoreLine.transform, 48f, 1000f, 64f);

            // Kazanma şeridi: G/B/M üç bölmeli bar — her olayda animasyonla kayar
            var stripBg = UiWidgets.MakeRect("WinStrip", modelRoot);
            UiWidgets.TopBlock(stripBg, 128f, StripW, 66f);
            var stripBgImg = stripBg.gameObject.AddComponent<Image>();
            stripBgImg.color = new Color(1f, 1f, 1f, 0.07f);
            stripWin = MakeStripSegment(stripBg, good, out stripWinT);
            stripDraw = MakeStripSegment(stripBg, new Color(1f, 1f, 1f, 0.30f), out stripDrawT);
            stripLoss = MakeStripSegment(stripBg, bad, out stripLossT);

            var momLabel = UiWidgets.MakeText("MomLabel", modelRoot, "MOMENTUM", 28, new Color(1f, 1f, 1f, 0.5f));
            UiWidgets.TopBlock((RectTransform)momLabel.transform, 214f, 1000f, 36f);
            var momRow = UiWidgets.MakeRect("MomRow", modelRoot);
            UiWidgets.TopBlock(momRow, 252f, StripW, 150f);
            for (int i = 0; i < 12; i++)
            {
                var bar = UiWidgets.MakeRect("Bar" + i, momRow);
                bar.anchorMin = bar.anchorMax = new Vector2((i + 0.5f) / 12f, 0.5f);
                bar.pivot = new Vector2(0.5f, 0.5f);
                bar.sizeDelta = new Vector2(StripW / 12f - 10f, 8f);
                var img = bar.gameObject.AddComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0.35f);
                momBars.Add(bar);
            }

            mFeedText = UiWidgets.MakeText("Feed", modelRoot, "", 33, new Color(0.93f, 0.96f, 0.9f), TextAnchor.UpperLeft);
            UiWidgets.TopBlock((RectTransform)mFeedText.transform, 430f, StripW, 430f);
            mFeedText.lineSpacing = 1.25f;

            mBlockCard = UiWidgets.MakeText("BlockCard", modelRoot, "", 42, accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiWidgets.TopBlock((RectTransform)mBlockCard.transform, 900f, 1000f, 110f);

            // Müdahale barı — hamleler Tek Kapı'dan geçer
            var tac = UiWidgets.MakeButton("MTactic", modelRoot, "", 32, btnBg, btnFg, () => OnTacticCycle?.Invoke());
            var tRt = (RectTransform)tac.transform;
            UiWidgets.TopBlock(tRt, 1050f, 300f, 116f);
            tRt.anchoredPosition = new Vector2(-330f, -1050f);
            mTacticBtnLabel = tac.GetComponentInChildren<Text>();

            var raise = UiWidgets.MakeButton("MRaise", modelRoot, "TEMPO ↑", 32, btnBg, btnFg, () => OnTempoRaise?.Invoke());
            UiWidgets.TopBlock((RectTransform)raise.transform, 1050f, 300f, 116f);
            mTempoRaiseLabel = raise.GetComponentInChildren<Text>();

            var lockB = UiWidgets.MakeButton("MLock", modelRoot, "KİLİTLEN", 32, btnBg, btnFg, () => OnTempoLock?.Invoke());
            var lRt = (RectTransform)lockB.transform;
            UiWidgets.TopBlock(lRt, 1050f, 300f, 116f);
            lRt.anchoredPosition = new Vector2(330f, -1050f);
            mTempoLockLabel = lockB.GetComponentInChildren<Text>();

            mMovesLabel = UiWidgets.MakeText("Moves", modelRoot, "", 32, new Color(1f, 1f, 1f, 0.7f));
            UiWidgets.TopBlock((RectTransform)mMovesLabel.transform, 1180f, 1000f, 40f);

            mSpeed1 = UiWidgets.MakeButton("MSpeed1", modelRoot, "1x", 40, btnBg, btnFg, () => OnSpeedSelected?.Invoke(1));
            UiWidgets.BottomBlock((RectTransform)mSpeed1.transform, 64f, 240f, 110f);
            ((RectTransform)mSpeed1.transform).anchoredPosition = new Vector2(-330f, 64f);
            mSpeed2 = UiWidgets.MakeButton("MSpeed2", modelRoot, "2x", 40, btnBg, btnFg, () => OnSpeedSelected?.Invoke(2));
            UiWidgets.BottomBlock((RectTransform)mSpeed2.transform, 64f, 240f, 110f);
            var skip2 = UiWidgets.MakeButton("MSkip", modelRoot, "▶▶ Atla", 36, btnBg, btnFg, () => OnSkipPressed?.Invoke());
            UiWidgets.BottomBlock((RectTransform)skip2.transform, 64f, 340f, 110f);
            ((RectTransform)skip2.transform).anchoredPosition = new Vector2(330f, 64f);

            modelRoot.gameObject.SetActive(false);
        }

        RectTransform MakeStripSegment(RectTransform parent, Color c, out Text label)
        {
            var seg = UiWidgets.MakeRect("Seg", parent);
            seg.anchorMin = new Vector2(0f, 0f);
            seg.anchorMax = new Vector2(0f, 1f);
            seg.pivot = new Vector2(0f, 0.5f);
            seg.sizeDelta = new Vector2(0f, 0f);
            var img = seg.gameObject.AddComponent<Image>();
            var cc = c; cc.a = Mathf.Max(0.55f, c.a);
            img.color = cc;
            label = UiWidgets.MakeText("L", seg, "", 30, Color.black, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiWidgets.Stretch((RectTransform)label.transform);
            return seg;
        }

        public void ShowModelScreen()
        {
            feedLines.Clear();
            mFeedText.text = "";
            modelRoot.gameObject.SetActive(true);
        }

        public void HideModelScreen() => modelRoot.gameObject.SetActive(false);
        public void SetModelWidgetsVisible(bool on) => modelRoot.gameObject.SetActive(on); // vinyet sırasında gizle

        public void SetScoreBlockLine(string home, string away, int gu, int gt, int blockIdx, int blockCount, int minute)
            => mScoreLine.text = $"{home}  {gu} - {gt}  {away}    ·    {minute}'  (Blok {Mathf.Min(blockIdx + 1, blockCount)}/{blockCount})";

        public void SetWinProb(WinProb p)
        {
            if (stripCo != null) StopCoroutine(stripCo);
            stripCo = StartCoroutine(AnimateStrip(p));
        }

        IEnumerator AnimateStrip(WinProb p)
        {
            float w0 = stripWin.sizeDelta.x, d0 = stripDraw.sizeDelta.x, l0 = stripLoss.sizeDelta.x;
            float w1 = StripW * p.Win, d1 = StripW * p.Draw, l1 = StripW * p.Loss;
            for (float t = 0f; t < 0.55f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 0.55f);
                LayoutStrip(Mathf.Lerp(w0, w1, k), Mathf.Lerp(d0, d1, k), Mathf.Lerp(l0, l1, k));
                yield return null;
            }
            LayoutStrip(w1, d1, l1);
            stripWinT.text = p.Win >= 0.12f ? $"G %{p.Win * 100f:0}" : "";
            stripDrawT.text = p.Draw >= 0.12f ? $"B %{p.Draw * 100f:0}" : "";
            stripLossT.text = p.Loss >= 0.12f ? $"M %{p.Loss * 100f:0}" : "";
        }

        void LayoutStrip(float w, float d, float l)
        {
            stripWin.sizeDelta = new Vector2(w, 0f);
            stripWin.anchoredPosition = Vector2.zero;
            stripDraw.sizeDelta = new Vector2(d, 0f);
            stripDraw.anchoredPosition = new Vector2(w, 0f);
            stripLoss.sizeDelta = new Vector2(l, 0f);
            stripLoss.anchoredPosition = new Vector2(w + d, 0f);
        }

        public void ShowBlockCard(int blockIdx, int blockCount, int minFrom, int minTo, float pUs, float pThem, string factorLine)
            => mBlockCard.text = $"BLOK {blockIdx + 1}/{blockCount}  ·  {minFrom}'-{minTo}'\n" +
                                 $"Gol ihtimali — BİZ %{pUs * 100f:0}  ·  RAKİP %{pThem * 100f:0}\n" +
                                 $"<size=26>{factorLine}</size>";

        public void PushFeed(string line)
        {
            feedLines.Add(line);
            if (feedLines.Count > 9) feedLines.RemoveAt(0);
            mFeedText.text = string.Join("\n", feedLines);
        }

        public void SetMomentumHistory(System.Collections.Generic.IReadOnlyList<float> hist)
        {
            for (int i = 0; i < momBars.Count; i++)
            {
                int hIdx = hist.Count - momBars.Count + i;
                float v = hIdx >= 0 && hIdx < hist.Count ? hist[hIdx] : 0f;
                float hPx = Mathf.Max(8f, Mathf.Abs(v) * 70f);
                momBars[i].sizeDelta = new Vector2(momBars[i].sizeDelta.x, hPx);
                momBars[i].anchoredPosition = new Vector2(0f, v * 35f);
                momBars[i].GetComponent<Image>().color = v >= 0f
                    ? new Color(good.r, good.g, good.b, 0.85f)
                    : new Color(bad.r, bad.g, bad.b, 0.85f);
            }
        }

        public void SetInterventionState(string tacticName, int tempoMode, int movesLeft)
        {
            mTacticBtnLabel.text = $"TAKTİK\n{tacticName}";
            mTempoRaiseLabel.text = tempoMode == 1 ? "TEMPO ↑ ✓" : "TEMPO ↑";
            mTempoLockLabel.text = tempoMode == 2 ? "KİLİTLEN ✓" : "KİLİTLEN";
            mMovesLabel.text = $"Kalan hamle: {movesLeft}";
        }

        public void SetModelSpeedHighlight(int speed)
        {
            mSpeed1.image.color = speed == 1 ? accent : btnBg;
            mSpeed1.GetComponentInChildren<Text>().color = speed == 1 ? Color.black : btnFg;
            mSpeed2.image.color = speed == 2 ? accent : btnBg;
            mSpeed2.GetComponentInChildren<Text>().color = speed == 2 ? Color.black : btnFg;
        }

        // ---------------------------------------------------------------- HUD

        void BuildHud(Transform root)
        {
            hudRoot = UiWidgets.MakeRect("HUD", root);
            UiWidgets.Stretch(hudRoot);

            scoreText = UiWidgets.MakeText("Score", hudRoot, "", 64, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiWidgets.TopBlock((RectTransform)scoreText.transform, 44f, 1000f, 76f);

            minuteText = UiWidgets.MakeText("Minute", hudRoot, "", 40, accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiWidgets.TopBlock((RectTransform)minuteText.transform, 124f, 600f, 50f);

            tickerText = UiWidgets.MakeText("Ticker", hudRoot, "", 34, new Color(1f, 1f, 1f, 0.85f));
            UiWidgets.TopBlock((RectTransform)tickerText.transform, 182f, 900f, 44f);

            speed1Btn = UiWidgets.MakeButton("Speed1", hudRoot, "1x", 42, btnBg, btnFg, () => OnSpeedSelected?.Invoke(1));
            UiWidgets.BottomBlock((RectTransform)speed1Btn.transform, 64f, 240f, 116f);
            ((RectTransform)speed1Btn.transform).anchoredPosition = new Vector2(-330f, 64f);

            speed2Btn = UiWidgets.MakeButton("Speed2", hudRoot, "2x", 42, btnBg, btnFg, () => OnSpeedSelected?.Invoke(2));
            UiWidgets.BottomBlock((RectTransform)speed2Btn.transform, 64f, 240f, 116f);

            skipBtn = UiWidgets.MakeButton("Skip", hudRoot, "▶▶ Önemli An", 38, btnBg, btnFg, () => OnSkipPressed?.Invoke());
            UiWidgets.BottomBlock((RectTransform)skipBtn.transform, 64f, 380f, 116f);
            ((RectTransform)skipBtn.transform).anchoredPosition = new Vector2(330f, 64f);

            hudRoot.gameObject.SetActive(false);
        }

        public void ShowHud()
        {
            hudRoot.gameObject.SetActive(true);
            Ticker("");
            SetSpeedHighlight(1);
        }

        public void HideHud() => hudRoot.gameObject.SetActive(false);

        public void SetScoreLine(string home, string away, int hs, int aw, float minute)
        {
            scoreText.text = $"{home}  {hs} - {aw}  {away}";
            minuteText.text = $"{Mathf.FloorToInt(minute)}'";
        }

        public void SetSpeedHighlight(int speed)
        {
            var onC = accent;
            var offC = btnBg;
            speed1Btn.image.color = speed == 1 ? onC : offC;
            speed1Btn.GetComponentInChildren<Text>().color = speed == 1 ? Color.black : btnFg;
            speed2Btn.image.color = speed == 2 ? onC : offC;
            speed2Btn.GetComponentInChildren<Text>().color = speed == 2 ? Color.black : btnFg;
        }

        public void Ticker(string msg)
        {
            if (tickerCo != null) StopCoroutine(tickerCo);
            tickerText.text = msg;
            if (!string.IsNullOrEmpty(msg) && hudRoot.gameObject.activeInHierarchy)
                tickerCo = StartCoroutine(FadeTicker());
        }

        IEnumerator FadeTicker()
        {
            var c = tickerText.color; c.a = 0.95f; tickerText.color = c;
            yield return new WaitForSeconds(2.2f);
            for (float t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                c.a = Mathf.Lerp(0.95f, 0f, t / 0.5f);
                tickerText.color = c;
                yield return null;
            }
            tickerText.text = "";
        }

        // ---------------------------------------------------------------- Gol vurgusu + banner

        void BuildFlash(Transform root)
        {
            flashImg = UiWidgets.MakePanel("GoalFlash", root, new Color(1f, 1f, 1f, 0f));
            flashImg.raycastTarget = false;

            flashBig = UiWidgets.MakeText("GoalBig", flashImg.transform, "GOL!", 170, accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiWidgets.TopBlock((RectTransform)flashBig.transform, 700f, 1000f, 220f);
            var outline = flashBig.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(4f, -4f);

            flashSmall = UiWidgets.MakeText("GoalSmall", flashImg.transform, "", 54, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiWidgets.TopBlock((RectTransform)flashSmall.transform, 930f, 1000f, 80f);
            var o2 = flashSmall.gameObject.AddComponent<Outline>();
            o2.effectColor = new Color(0f, 0f, 0f, 0.85f);
            o2.effectDistance = new Vector2(3f, -3f);

            flashImg.gameObject.SetActive(false);
        }

        public void GoalFlash(string detail)
        {
            if (flashCo != null) StopCoroutine(flashCo);
            flashCo = StartCoroutine(GoalFlashCo(detail));
        }

        IEnumerator GoalFlashCo(string detail)
        {
            flashImg.gameObject.SetActive(true);
            flashSmall.text = detail;
            float dur = bal.vurgu.slowmoSureSn + 0.7f;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                float k = t / dur;
                var c = flashImg.color;
                c.a = Mathf.Lerp(bal.vurgu.golFlasSureSn > 0f ? 0.42f : 0f, 0f, Mathf.Clamp01(t / Mathf.Max(0.01f, bal.vurgu.golFlasSureSn)));
                flashImg.color = c;
                float s = 0.6f + 0.55f * Mathf.Sin(Mathf.Clamp01(k * 1.4f) * Mathf.PI * 0.5f);
                flashBig.transform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            flashImg.gameObject.SetActive(false);
        }

        void BuildBanner(Transform root)
        {
            bannerRoot = UiWidgets.MakeRect("Banner", root);
            UiWidgets.TopBlock(bannerRoot, 820f, 1080f, 140f);
            var bg = bannerRoot.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.66f);
            bg.raycastTarget = false;
            bannerText = UiWidgets.MakeText("BannerText", bannerRoot, "", 62, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiWidgets.Stretch((RectTransform)bannerText.transform);
            bannerRoot.gameObject.SetActive(false);
        }

        public void Banner(string text, float seconds)
        {
            if (bannerCo != null) StopCoroutine(bannerCo);
            bannerCo = StartCoroutine(BannerCo(text, seconds));
        }

        IEnumerator BannerCo(string text, float seconds)
        {
            bannerRoot.gameObject.SetActive(true);
            bannerText.text = text;
            yield return new WaitForSeconds(seconds);
            bannerRoot.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- Maç öncesi

        void BuildPreMatch(Transform root)
        {
            var panel = UiWidgets.MakePanel("PreMatch", root, panelBg);
            preRoot = (RectTransform)panel.transform;

            preTitle = UiWidgets.MakeText("Title", preRoot, "", 72, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiWidgets.TopBlock((RectTransform)preTitle.transform, 120f, 1000f, 90f);

            preOpponent = UiWidgets.MakeText("Opponent", preRoot, "", 46, accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiWidgets.TopBlock((RectTransform)preOpponent.transform, 224f, 1000f, 60f);

            preStrength = UiWidgets.MakeText("Strength", preRoot, "", 34, new Color(1f, 1f, 1f, 0.75f));
            UiWidgets.TopBlock((RectTransform)preStrength.transform, 290f, 1000f, 44f);

            preMoney = UiWidgets.MakeText("Money", preRoot, "", 40, good, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiWidgets.TopBlock((RectTransform)preMoney.transform, 350f, 1000f, 52f);

            var tacLabel = UiWidgets.MakeText("TacticLabel", preRoot, "TAKTİK PRESETİ", 34, new Color(1f, 1f, 1f, 0.6f));
            UiWidgets.TopBlock((RectTransform)tacLabel.transform, 438f, 1000f, 44f);

            tacticBtnBg = new Image[3];
            tacticBtnLabel = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                int captured = i;
                var b = UiWidgets.MakeButton("Tactic" + i, preRoot, "", 34, btnBg, btnFg, () => OnTacticSelected?.Invoke(captured));
                var rt = (RectTransform)b.transform;
                UiWidgets.TopBlock(rt, 496f, 320f, 120f);
                rt.anchoredPosition = new Vector2((i - 1) * 344f, -496f);
                tacticBtnBg[i] = b.image;
                tacticBtnLabel[i] = b.GetComponentInChildren<Text>();
            }

            var squadLabel = UiWidgets.MakeText("SquadLabel", preRoot, "İLK 11 — ROZET SK", 34, new Color(1f, 1f, 1f, 0.6f));
            UiWidgets.TopBlock((RectTransform)squadLabel.transform, 668f, 1000f, 44f);

            preSquad = UiWidgets.MakeText("Squad", preRoot, "", 34, new Color(0.92f, 0.95f, 0.9f), TextAnchor.UpperCenter);
            UiWidgets.TopBlock((RectTransform)preSquad.transform, 720f, 900f, 560f);
            preSquad.lineSpacing = 1.18f;

            preTicket = UiWidgets.MakeText("Ticket", preRoot, "", 36, accent);
            UiWidgets.TopBlock((RectTransform)preTicket.transform, 1310f, 1000f, 48f);

            var start = UiWidgets.MakeButton("Start", preRoot, "MAÇA BAŞLA", 46, accent, Color.black, () => OnStartMatch?.Invoke());
            UiWidgets.BottomBlock((RectTransform)start.transform, 96f, 660f, 140f);

            preRoot.gameObject.SetActive(false);
        }

        public void ShowPreMatch(int matchIndex, string opponent, float oppStrength, long money,
                                 string[] squad, GreyboxBalance.TacticCfg[] tactics, int selectedTactic,
                                 string ticketLine)
        {
            preTitle.text = $"MAÇ GÜNÜ #{matchIndex}";
            preOpponent.text = $"{GreyboxWorld.PlayerClubName}  —  {opponent}";
            preStrength.text = $"Rakip gücü: {oppStrength:0}  ·  Bizim güç: {bal.takimlar.oyuncuTakimGucu:0}";
            preMoney.text = "Kasa: " + UiWidgets.Money(money);
            preSquad.text = string.Join("\n", squad);
            for (int i = 0; i < 3 && i < tactics.Length; i++)
                tacticBtnLabel[i].text = $"{tactics[i].ad}\n({FormationPretty(tactics[i].formasyon)})";
            SetTacticHighlight(selectedTactic);
            preTicket.text = ticketLine;
            preRoot.gameObject.SetActive(true);
        }

        static string FormationPretty(string f)
        {
            if (string.IsNullOrEmpty(f)) return "";
            var chars = f.ToCharArray();
            return string.Join("-", Array.ConvertAll(chars, c => c.ToString()));
        }

        public void SetTacticHighlight(int selected)
        {
            for (int i = 0; i < 3; i++)
            {
                bool on = i == selected;
                tacticBtnBg[i].color = on ? accent : btnBg;
                tacticBtnLabel[i].color = on ? Color.black : btnFg;
            }
        }

        public void HidePreMatch() => preRoot.gameObject.SetActive(false);

        // ---------------------------------------------------------------- Maç sonu

        void BuildPostMatch(Transform root)
        {
            var panel = UiWidgets.MakePanel("PostMatch", root, panelBg);
            postRoot = (RectTransform)panel.transform;

            var title = UiWidgets.MakeText("Title", postRoot, "MAÇ SONU", 56, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UiWidgets.TopBlock((RectTransform)title.transform, 130f, 1000f, 70f);

            postScore = UiWidgets.MakeText("Score", postRoot, "", 116, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiWidgets.TopBlock((RectTransform)postScore.transform, 220f, 1000f, 150f);

            postResult = UiWidgets.MakeText("Result", postRoot, "", 56, good, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiWidgets.TopBlock((RectTransform)postResult.transform, 392f, 1000f, 70f);

            postIncomeA = UiWidgets.MakeText("IncomeA", postRoot, "", 36, new Color(0.92f, 0.95f, 0.9f));
            UiWidgets.TopBlock((RectTransform)postIncomeA.transform, 520f, 1000f, 48f);

            postIncomeB = UiWidgets.MakeText("IncomeB", postRoot, "", 36, new Color(0.92f, 0.95f, 0.9f));
            UiWidgets.TopBlock((RectTransform)postIncomeB.transform, 576f, 1000f, 48f);

            postMoney = UiWidgets.MakeText("Money", postRoot, "", 44, good, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiWidgets.TopBlock((RectTransform)postMoney.transform, 660f, 1000f, 56f);

            var priceLabel = UiWidgets.MakeText("PriceLabel", postRoot, "BİLET FİYATI — SONRAKİ MAÇ", 34, new Color(1f, 1f, 1f, 0.6f));
            UiWidgets.TopBlock((RectTransform)priceLabel.transform, 850f, 1000f, 44f);

            priceSlider = UiWidgets.MakeSlider("PriceSlider", postRoot, bal.ekonomi.fiyatMin, bal.ekonomi.fiyatMax,
                bal.ekonomi.refFiyat, v => OnPriceChanged?.Invoke(v));
            var srt = (RectTransform)priceSlider.transform;
            UiWidgets.TopBlock(srt, 910f, 780f, 130f);

            postProjection = UiWidgets.MakeText("Projection", postRoot, "", 38, accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiWidgets.TopBlock((RectTransform)postProjection.transform, 1050f, 1000f, 96f);

            var next = UiWidgets.MakeButton("Next", postRoot, "SONRAKİ MAÇ  ▶", 46, accent, Color.black, () => OnNextMatch?.Invoke());
            UiWidgets.BottomBlock((RectTransform)next.transform, 96f, 660f, 140f);

            postRoot.gameObject.SetActive(false);
        }

        public void ShowPostMatch(int hs, int aw, int result, string incomeLineA, string incomeLineB,
                                  long money, float currentPrice, string projectionText)
        {
            postScore.text = $"{hs} - {aw}";
            postResult.text = result > 0 ? "GALİBİYET!" : result == 0 ? "BERABERLİK" : "MAĞLUBİYET";
            postResult.color = result > 0 ? good : result == 0 ? new Color(1f, 1f, 1f, 0.8f) : bad;
            postIncomeA.text = incomeLineA;
            postIncomeB.text = incomeLineB;
            postMoney.text = "Kasa: " + UiWidgets.Money(money);
            priceSlider.SetValueWithoutNotify(currentPrice);
            postProjection.text = projectionText;
            postRoot.gameObject.SetActive(true);
        }

        public void UpdateProjection(string text) => postProjection.text = text;

        public void HidePostMatch() => postRoot.gameObject.SetActive(false);
    }
}
