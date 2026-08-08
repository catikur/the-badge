using System;

namespace TheBadge.Sim.Core
{
    /// <summary>Trigonometri LUT — ME Spec 3.2: sin/cos 4096 girişli Q16 tamsayı tablo;
    /// Math.Sin/Cos sim mantığında YASAK (platform bit farkı). Tablo statik kuruluşta üretilip
    /// Q16'ya kuantalanır (AttributeLuts ile aynı gerekçe); golden kapılar sapmayı yakalar.</summary>
    public static class TrigLut
    {
        public const int Size = 4096;                 // tam tur; indeks = açı × Size / 2π
        static readonly int[] sinQ16 = Build();

        static int[] Build()
        {
            var t = new int[Size];
            for (int i = 0; i < Size; i++)
                t[i] = (int)Math.Round(Math.Sin(2.0 * Math.PI * i / Size) * 65536.0);
            return t;
        }

        public static int SinQ16(int idx) => sinQ16[idx & (Size - 1)];
        public static int CosQ16(int idx) => sinQ16[(idx + Size / 4) & (Size - 1)];

        /// <summary>Radyandan tablo indeksine (soğuk yol — karar anında bir kez).</summary>
        public static int AngleIndexFromRad(double rad) =>
            (int)Math.Round(rad * Size / (2.0 * Math.PI)) & (Size - 1);

        /// <summary>Vektörü LUT açısıyla döndürür — ara hesap double, sonucu çağıran kuantalar.</summary>
        public static void Rotate(double x, double y, int angleIdx, out double rx, out double ry)
        {
            double c = CosQ16(angleIdx) / 65536.0, s = SinQ16(angleIdx) / 65536.0;
            rx = x * c - y * s;
            ry = x * s + y * c;
        }
    }
}
