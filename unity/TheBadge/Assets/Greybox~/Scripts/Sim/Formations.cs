namespace TheBadge.Greybox.Sim
{
    /// <summary>
    /// Formasyon çapa noktaları — normalize koordinat (x: 0-1 saha genişliği, y: 0-1 saha boyu,
    /// kendi kalesinden rakip kaleye doğru). Davranışsal katsayılar greybox.balance.json'dadır;
    /// buradaki değerler kalibrasyon değil GEOMETRİ (içerik) olduğundan kodda yaşar — plan kararı.
    /// İndeks 0 = kaleci; sıra sabittir (ajan güncelleme sırası kuralıyla uyumlu).
    /// </summary>
    public static class Formations
    {
        static readonly float[][] F442 =
        {
            new[] { 0.50f, 0.045f },
            new[] { 0.15f, 0.200f }, new[] { 0.38f, 0.185f }, new[] { 0.62f, 0.185f }, new[] { 0.85f, 0.200f },
            new[] { 0.15f, 0.420f }, new[] { 0.40f, 0.400f }, new[] { 0.60f, 0.400f }, new[] { 0.85f, 0.420f },
            new[] { 0.40f, 0.600f }, new[] { 0.60f, 0.600f }
        };

        static readonly float[][] F433 =
        {
            new[] { 0.50f, 0.045f },
            new[] { 0.15f, 0.210f }, new[] { 0.38f, 0.195f }, new[] { 0.62f, 0.195f }, new[] { 0.85f, 0.210f },
            new[] { 0.30f, 0.400f }, new[] { 0.50f, 0.370f }, new[] { 0.70f, 0.400f },
            new[] { 0.20f, 0.620f }, new[] { 0.50f, 0.660f }, new[] { 0.80f, 0.620f }
        };

        static readonly float[][] F532 =
        {
            new[] { 0.50f, 0.045f },
            new[] { 0.12f, 0.200f }, new[] { 0.30f, 0.170f }, new[] { 0.50f, 0.160f }, new[] { 0.70f, 0.170f }, new[] { 0.88f, 0.200f },
            new[] { 0.28f, 0.380f }, new[] { 0.50f, 0.360f }, new[] { 0.72f, 0.380f },
            new[] { 0.42f, 0.560f }, new[] { 0.58f, 0.560f }
        };

        /// <summary>Formasyon adına göre 11 çapa (bilinmeyen ad → 442).</summary>
        public static float[][] Get(string formasyon)
        {
            switch (formasyon)
            {
                case "433": return F433;
                case "532": return F532;
                default: return F442;
            }
        }
    }
}
