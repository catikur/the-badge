using UnityEngine;

namespace TheBadge.Greybox.View
{
    /// <summary>
    /// Greybox placeholder sprite üretimi — daire/dikdörtgen/halka, tamamı runtime.
    /// Asset üretimi FAZ 05'e kilitli (Anayasa 4G.5); burada tek piksel sanat yoktur.
    /// </summary>
    public static class SpriteFactory
    {
        public const float PixelsPerUnit = 32f;
        static Sprite circle, ring, solid;

        public static Sprite Circle()
        {
            if (circle == null) circle = MakeCircle(64, filled: true, thickness: 0f);
            return circle;
        }

        /// <summary>Orta yuvarlak için içi boş halka.</summary>
        public static Sprite Ring()
        {
            if (ring == null) ring = MakeCircle(128, filled: false, thickness: 3.5f);
            return ring;
        }

        public static Sprite Solid()
        {
            if (solid == null)
            {
                var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                var px = new Color32[16];
                for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
                tex.SetPixels32(px);
                tex.Apply(false, false);
                tex.hideFlags = HideFlags.HideAndDontSave;
                solid = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            }
            return solid;
        }

        static Sprite MakeCircle(int size, bool filled, float thickness)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            float r = size * 0.5f - 1.5f;
            float cx = size * 0.5f - 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cx) * (y - cx));
                    float a = filled
                        ? Mathf.Clamp01(r - d + 0.5f)                                  // dolu daire, yumuşak kenar
                        : Mathf.Clamp01(thickness * 0.5f - Mathf.Abs(d - (r - thickness)) + 0.5f); // halka
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            // pixelsPerUnit: doku çapı 1 dünya birimi olsun → transform.scale = metre cinsi çap
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        public static Color Hex(string hex, Color fallback)
        {
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : fallback;
        }

        public static SpriteRenderer NewSprite(string name, Transform parent, Sprite sprite, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }
    }
}
