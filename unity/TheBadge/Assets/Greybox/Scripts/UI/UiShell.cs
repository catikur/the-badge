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
            BuildFlash(root);
            BuildBanner(root);
            BuildPreMatch(root);
            BuildPostMatch(root);
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
