using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using TheBadge.Sim.Core;
using TheBadge.Sim.Determinism;

namespace TheBadge.Sim.Replay
{
    /// <summary>P0 PİNLERİ — ME 3.2 sayısal disiplininin bit düzeyindeki tanıkları (DECISIONS P0 (a)).
    /// Değerler linux-x64'te ölçülüp sabitlendi; macOS arm64 (.NET) aynı biti verdi. Burada TEK
    /// yerde durur ki sunucu kapısı (Checks) ve istemci sondası (Unity Mono / IL2CPP, ADR-004) aynı
    /// pine karşı ölçsün. Bir pin kırılırsa o platform farklı bit üretiyor demektir — pini güncellemek
    /// DEĞİL, nedeni bulmak gerekir. Pin yalnız ölçülen kodun KENDİSİ bilerek değişince güncellenir.
    ///
    /// Ölçüm ızgarası libm'siz ve TAM üretilir (Rng: tamsayı işlem + 2^-53 ölçeği). Ancak
    /// `taban + ölçek * U` biçimindeki girdiler de bir çarpma-toplamadır: bir derleyici bunu
    /// birleştirirse (FMA) önce GİRDİLER kayar. <see cref="IzgaraPin"/> bu yüzden ayrı tutulur —
    /// sapma girdide mi DetMath'te mi, ayırt edilsin.</summary>
    public static class P0Pinleri
    {
        public const ulong IzgaraTohum = 0x50A0DE7A0A7EUL;
        public const uint IzgaraN = 4096;

        /// <summary>DetMath'in yedi fonksiyonunun ızgara çıktı özeti (linux-x64, 2026-10-05).</summary>
        public static readonly (string ad, ulong pin)[] DetMathPinleri =
        {
            ("Exp", 0x3392CD200C6E5906UL), ("Log", 0xA00F621B927BE80FUL), ("Pow", 0x1C2F53C261730C9EUL),
            ("Sin", 0x910641454B06C9D2UL), ("Cos", 0x0F625A9C9967E60CUL), ("Tan", 0xFE7030AA07848E9AUL),
            ("Atan2", 0xA005FC21A9BDA698UL),
        };

        /// <summary>ME 3.2'nin 4096 girişli Q16 sinüs tablosunun özeti (linux-x64, 2026-10-05).</summary>
        public const ulong TrigLutPin = 0x151EDD4A6B53DC23UL;

        /// <summary>Ölçüm ızgarasının GİRDİLERİNİN özeti (linux-x64, 2026-10-07).</summary>
        public const ulong IzgaraPin = 0x2024780C6F6583E8UL;

        /// <summary>Ölçüm ızgarası — fonksiyon başına sınır değerler + 4096 tohumlu girdi.</summary>
        public sealed class Izgara
        {
            public readonly List<double> ExpX = new List<double>(), LogX = new List<double>(),
                                         TrigX = new List<double>(), TanX = new List<double>();
            public readonly List<(double x, double y)> PowXY = new List<(double x, double y)>();
            public readonly List<(double y, double x)> AtanYX = new List<(double y, double x)>();
        }

        static double U(uint akis, uint i) => Rng.Rand01(IzgaraTohum, Domain.Chaos, i, 0, akis);
        static double Iki(int k) => BitConverter.Int64BitsToDouble((long)(k + 1023) << 52);   // 2^k, TAM

        public static Izgara IzgarayiKur()
        {
            // Zar gerekçesi: ölçüm girdisi oyun durumuna dokunmaz; Chaos akışı yalnız tohumlu dağılım için
            var g = new Izgara();
            g.ExpX.AddRange(new[] { 0.0, -0.0, 1.0, -1.0, 1e-300, -1e-300, 0.34657359027997264,
                                    -0.34657359027997264, 700.0, -700.0, -708.4, -740.0, 709.7, -745.0 });
            for (uint i = 0; i < IzgaraN; i++) g.ExpX.Add(-50.0 + 100.0 * U(1, i));
            g.LogX.AddRange(new[] { 1.0, 2.0, 0.5, 1.4142135623730951, 1.4142135623730954, 1.4142135623730949,
                                    1e-310, double.Epsilon, 1e300, double.MaxValue, 0.9999999999999999,
                                    1.0000000000000002 });
            for (uint i = 0; i < IzgaraN; i++) g.LogX.Add((1.0 + U(2, i)) * Iki((int)(U(3, i) * 40.0) - 20));
            g.PowXY.AddRange(new[] { (0.0, 2.2), (1.0, 7.0), (0.5, 0.0), (0.25, 0.5), (1e-300, 0.1), (0.999, 3.3) });
            for (uint i = 0; i < IzgaraN; i++) g.PowXY.Add((U(4, i), 0.1 + 3.9 * U(5, i)));
            g.TrigX.AddRange(new[] { 0.0, -0.0, Math.PI / 4, Math.PI / 2, Math.PI, 1e5, -1e5, 1e-10 });
            for (uint i = 0; i < IzgaraN; i++) g.TrigX.Add(-8.0 * Math.PI + 16.0 * Math.PI * U(6, i));
            g.TanX.AddRange(new[] { 0.0, Math.PI / 4, -Math.PI / 4, 1.5, 1e-10 });
            for (uint i = 0; i < IzgaraN; i++) g.TanX.Add(-1.5 + 3.0 * U(7, i));
            g.AtanYX.AddRange(new[] { (0.0, 1.0), (0.0, -1.0), (-0.0, -1.0), (1.0, 0.0), (-1.0, 0.0),
                                      (0.0, 0.0), (0.0, -0.0), (1e-300, 1.0), (1.0, 1e-300), (-3.0, -4.0) });
            for (uint i = 0; i < IzgaraN; i++) g.AtanYX.Add((-60.0 + 120.0 * U(8, i), -60.0 + 120.0 * U(9, i)));
            return g;
        }

        /// <summary>Fonksiyon <paramref name="ad"/>'ın ızgaradaki DetMath çıktıları (pin sırasıyla aynı).</summary>
        public static List<double> DetMathCiktilari(string ad, Izgara g)
        {
            var o = new List<double>();
            switch (ad)
            {
                case "Exp": foreach (var x in g.ExpX) o.Add(DetMath.Exp(x)); break;
                case "Log": foreach (var x in g.LogX) o.Add(DetMath.Log(x)); break;
                case "Pow": foreach (var p in g.PowXY) o.Add(DetMath.Pow(p.x, p.y)); break;
                case "Sin": foreach (var x in g.TrigX) o.Add(DetMath.Sin(x)); break;
                case "Cos": foreach (var x in g.TrigX) o.Add(DetMath.Cos(x)); break;
                case "Tan": foreach (var x in g.TanX) o.Add(DetMath.Tan(x)); break;
                case "Atan2": foreach (var p in g.AtanYX) o.Add(DetMath.Atan2(p.y, p.x)); break;
                default: throw new ArgumentException("bilinmeyen fonksiyon: " + ad);
            }
            return o;
        }

        /// <summary>Değerlerin bit düzeyinde özeti: little-endian int64 bitleri → xxHash64.</summary>
        public static ulong BitOzeti(IReadOnlyList<double> v)
        {
            var b = new byte[v.Count * 8];
            for (int i = 0; i < v.Count; i++)
                BinaryPrimitives.WriteInt64LittleEndian(b.AsSpan(i * 8), BitConverter.DoubleToInt64Bits(v[i]));
            return XxHash64.Hash(b);
        }

        /// <summary>Izgara GİRDİLERİNİN özeti (fonksiyonların ızgara sırasıyla art arda).</summary>
        public static ulong IzgaraOzeti(Izgara g)
        {
            var hepsi = new List<double>();
            hepsi.AddRange(g.ExpX); hepsi.AddRange(g.LogX);
            foreach (var p in g.PowXY) { hepsi.Add(p.x); hepsi.Add(p.y); }
            hepsi.AddRange(g.TrigX); hepsi.AddRange(g.TanX);
            foreach (var p in g.AtanYX) { hepsi.Add(p.y); hepsi.Add(p.x); }
            return BitOzeti(hepsi);
        }

        /// <summary>ME 3.2 sinüs tablosunun özeti: little-endian int32 girişler → xxHash64.</summary>
        public static ulong TrigLutOzeti()
        {
            var tb = new byte[TrigLut.Size * 4];
            for (int i = 0; i < TrigLut.Size; i++)
                BinaryPrimitives.WriteInt32LittleEndian(tb.AsSpan(i * 4), TrigLut.SinQ16(i));
            return XxHash64.Hash(tb);
        }

        /// <summary>P0'ın KÖK NEDENİ (2026-10-05): kayan→tamsayı dönüşümü platforma bağlıydı (NaN:
        /// x64 int.MinValue, arm64 0). Dönüşüm doyurur; IL2CPP'nin C++'ında ham dönüşüm tanımsız
        /// davranış olduğundan bu fikstürler orada da tanıktır.</summary>
        public static (string ad, int olculen, int beklenen)[] DonusumFiksturleri() => new[]
        {
            ("ToInt32Sat(NaN)",          DetMath.ToInt32Sat(double.NaN), 0),
            ("ToInt32Sat(+∞)",           DetMath.ToInt32Sat(double.PositiveInfinity), int.MaxValue),
            ("ToInt32Sat(−∞)",           DetMath.ToInt32Sat(double.NegativeInfinity), int.MinValue),
            ("ToInt32Sat(3e9)",          DetMath.ToInt32Sat(3e9), int.MaxValue),
            ("ToInt32Sat(−3e9)",         DetMath.ToInt32Sat(-3e9), int.MinValue),
            ("ToInt32Sat(1.9)",          DetMath.ToInt32Sat(1.9), 1),
            ("ToInt32Sat(−1.9)",         DetMath.ToInt32Sat(-1.9), -1),
            ("ToInt32Sat(2147483647.5)", DetMath.ToInt32Sat(2147483647.5), int.MaxValue),
            ("QuantizeMm(NaN)",          Units.QuantizeMm(double.NaN), 0),
            ("QuantizeMm(−∞)",           Units.QuantizeMm(double.NegativeInfinity), int.MinValue),
            ("QuantizeMm(1.25)",         Units.QuantizeMm(1.25), 1250),
        };
    }
}
