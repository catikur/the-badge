using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheBadge.Greybox.UI
{
    /// <summary>
    /// Kodla uGUI üretim yardımcıları. FAZ 02'nin UI Toolkit ekran seti kapsam DIŞI (Brif);
    /// greybox UI'ı tamamen buradan kurulur ki elle yazılmış sahne/prefab YAML'ı gerekmesin.
    /// </summary>
    public static class UiWidgets
    {
        public static Font DefaultFont =>
            font != null ? font : font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        static Font font;

        public static Canvas MakeCanvas(string name)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            // Portre kilidi olan UI'da GENİŞLİK eşlenir (0): 19.5:9 gibi uzun ekranlarda
            // 0.5 karışımı genişlikten taşırıyordu (İterasyon 1 — "yazılar sığmıyor").
            scaler.matchWidthOrHeight = 0f;
            go.AddComponent<GraphicRaycaster>();

            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                // Input System paketi aktif (activeInputHandler=1); varsayılan aksiyonlarla dokunma/fare çalışır
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>(); // paket yoksa eski Input Manager geri dönüş yolu
#endif
            }
            return canvas;
        }

        public static RectTransform MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static Image MakePanel(string name, Transform parent, Color color)
        {
            var rt = MakeRect(name, parent);
            Stretch(rt);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Text MakeText(string name, Transform parent, string content, int size, Color color,
                                    TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var rt = MakeRect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = DefaultFont;
            t.text = content;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button MakeButton(string name, Transform parent, string label, int fontSize,
                                        Color bg, Color fg, Action onClick)
        {
            var rt = MakeRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = bg;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            var txt = MakeText("Label", rt, label, fontSize, fg, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch((RectTransform)txt.transform);
            return btn;
        }

        public static Slider MakeSlider(string name, Transform parent, float min, float max, float value,
                                        Action<float> onChanged)
        {
            var rt = MakeRect(name, parent);
            var slider = rt.gameObject.AddComponent<Slider>();

            // İnce ray + küçük tutamaç: eski 56×88 beyaz blok ekranı domine ediyordu (İt.12 görsel düzeltme)
            var bg = MakeRect("Background", rt);
            bg.anchorMin = new Vector2(0f, 0.5f);
            bg.anchorMax = new Vector2(1f, 0.5f);
            bg.pivot = new Vector2(0.5f, 0.5f);
            bg.sizeDelta = new Vector2(0f, 12f);
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.color = new Color(1f, 1f, 1f, 0.18f);

            var fillArea = MakeRect("Fill Area", rt);
            fillArea.anchorMin = new Vector2(0f, 0.5f);
            fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.pivot = new Vector2(0.5f, 0.5f);
            fillArea.sizeDelta = new Vector2(-12f, 12f);
            var fill = MakeRect("Fill", fillArea);
            var fillImg = fill.gameObject.AddComponent<Image>();
            fillImg.color = new Color(1f, 0.83f, 0.31f, 0.9f);
            fill.sizeDelta = new Vector2(10f, 0f);

            var handleArea = MakeRect("Handle Slide Area", rt);
            Stretch(handleArea);
            handleArea.offsetMin = new Vector2(22f, 0f);
            handleArea.offsetMax = new Vector2(-22f, 0f);
            var handle = MakeRect("Handle", handleArea);
            handle.sizeDelta = new Vector2(44f, 44f);
            var handleImg = handle.gameObject.AddComponent<Image>();
            handleImg.sprite = null;
            handleImg.color = new Color(1f, 0.83f, 0.31f);
            var handleOutline = handle.gameObject.AddComponent<Outline>();
            handleOutline.effectColor = new Color(0f, 0f, 0f, 0.55f);
            handleOutline.effectDistance = new Vector2(2f, -2f);

            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = true;
            slider.SetValueWithoutNotify(value);
            if (onChanged != null) slider.onValueChanged.AddListener(v => onChanged(v));
            return slider;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>Üst-orta çapalı blok: y üstten piksel, boyut sabit.</summary>
        public static void TopBlock(RectTransform rt, float yFromTop, float width, float height)
        {
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(0f, -yFromTop);
        }

        /// <summary>Alt-orta çapalı blok.</summary>
        public static void BottomBlock(RectTransform rt, float yFromBottom, float width, float height)
        {
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(0f, yFromBottom);
        }

        public static string Money(long v) => v.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("tr-TR")) + " kr";
    }
}
