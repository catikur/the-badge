using System;

namespace TheBadge.Sim.Core
{
    /// <summary>
    /// Deterministik matematik — ME Spec 3.2 (cross-platform bit eşitliği), P0 (a).
    ///
    /// System.Math'in transandantal fonksiyonları (Exp, Log, Pow, Sin, Cos, Tan, Atan2) işletim
    /// sisteminin libm'ine gider ve son bitte platforma göre değişir. P0'ın ölçümü: macOS arm64'te
    /// 50 golden replay'in 4'ü saptı, 3'ünde maç SONUCU değişti (DECISIONS, P0). Q16'ya kuantalamak
    /// farkı küçültür ama kaldırmaz: bir yuvarlama sınırına düşen değer yine taraf değiştirir.
    ///
    /// Buradaki fonksiyonlar yalnız IEEE-754'ün DOĞRU YUVARLANAN temel işlemlerini (+ − × ÷) ve
    /// TAM işlemleri (bit okuma/kurma, Math.Floor, Math.Abs) kullanır; işlem sırası sabittir ve
    /// libm çağrısı yoktur. Sonuçlar doğru yuvarlanmış DEĞİLDİR (birkaç ulp), ama her platformda
    /// AYNI biti üretir — sim için gereken budur. Katsayılar fdlibm/musl'dan (kamu malı) alındı.
    ///
    /// Kapılar (Sim.Checks): P0DetMathBitPin (çıktı özetleri pinli — macOS CI aynı biti üretmeli),
    /// P0DetMathDogruluk (System.Math'e göre ulp sınırı), P0LibmYasagi (Sim'de libm çağrısı yok).
    /// Tahsis yok (ME 16.2). Tanım kümesi dışındaki girdi SESSİZCE NaN üretmez, fırlatır: NaN'ın
    /// tamsayıya çevrimi platforma göre değişir (x64 int.MinValue, arm64 0) — o da bir sapma olurdu.
    /// </summary>
    public static class DetMath
    {
        // --- Sabitler: bitten kurulur, ondalık yazımın yuvarlanmasına güvenilmez ---
        // ln2 Cody–Waite ayrımı: Ln2Hi'nin alt 32 biti sıfır → |k| < 2^20 için k*Ln2Hi TAM.
        static readonly double Ln2Hi = Bits(0x3FE62E42FEE00000L);
        static readonly double Ln2Lo = Bits(0x3DEA39EF35793C76L);
        static readonly double InvLn2 = Bits(0x3FF71547652B82FEL);
        static readonly double Sqrt2 = Bits(0x3FF6A09E667F3BCDL);
        static readonly double Two54 = Bits(0x4350000000000000L);
        // π/2 iki parçalı: Pio2_1'in alt 33 biti sıfır → |n| < 2^20 için n*Pio2_1 TAM.
        static readonly double Pio2_1 = Bits(0x3FF921FB54400000L);
        static readonly double Pio2_1t = Bits(0x3DD0B4611A626331L);
        static readonly double InvPio2 = Bits(0x3FE45F306DC9C883L);
        static readonly double PiHi = Bits(0x400921FB54442D18L);
        static readonly double PiLo = Bits(0x3CA1A62633145C07L);
        static readonly double Pio4 = Bits(0x3FE921FB54442D18L);

        /// <summary>Sin/Cos/Tan için izin verilen en büyük |x| (radyan). Simin açıları küçüktür
        /// (şut sapması, LUT kuruluşu); bu sınır argüman indirgemesinin doğruluğunu korur.</summary>
        public const double TrigMaxArg = 1.0e5;

        static double Bits(long b) => BitConverter.Int64BitsToDouble(b);

        // 2^k, k ∈ [-1022, 1023] — bitten, TAM.
        static double Pow2(int k) => BitConverter.Int64BitsToDouble((long)(k + 1023) << 52);

        // v · 2^k: iki adımda; olası alt-normal sonuçta TEK yuvarlama son çarpımdadır.
        static double ScaleB(double v, int k)
        {
            if (k > 1023) { v *= 2.0; k -= 1; }                       // Exp'te k en çok 1024
            if (k < -1022) return v * Pow2(k + 54) * Pow2(-54);
            return v * Pow2(k);
        }

        // ============================ Exp / Log / Pow ============================

        /// <summary>e^x. Aralık indirgemesi x = k·ln2 + r (Cody–Waite), e^r derece-13 Taylor
        /// (|r| ≲ 0.347 → kesme hatası < 5e-18), sonra 2^k ile ölçek.</summary>
        public static double Exp(double x)
        {
            if (double.IsNaN(x)) throw new ArgumentOutOfRangeException(nameof(x), "DetMath.Exp: NaN");
            if (x > 709.782712893383973096) return double.PositiveInfinity;
            if (x < -745.133219101941108420) return 0.0;
            double kd = Math.Floor(x * InvLn2 + 0.5);   // en yakın tamsayı (±1 kayma zararsız)
            int k = (int)kd;
            double r = (x - kd * Ln2Hi) - kd * Ln2Lo;
            double p = 1.0 + r * (1.0 + r * (1.0 / 2 + r * (1.0 / 6 + r * (1.0 / 24 + r * (1.0 / 120
                     + r * (1.0 / 720 + r * (1.0 / 5040 + r * (1.0 / 40320 + r * (1.0 / 362880
                     + r * (1.0 / 3628800 + r * (1.0 / 39916800 + r * (1.0 / 479001600
                     + r * (1.0 / 6227020800.0)))))))))))));
            return ScaleB(p, k);
        }

        /// <summary>ln x, x &gt; 0. x = m·2^e (m ∈ [√½, √2)), ln m = 2·atanh(s), s = (m−1)/(m+1).</summary>
        public static double Log(double x)
        {
            if (!(x > 0.0)) throw new ArgumentOutOfRangeException(nameof(x), "DetMath.Log: x > 0 olmalı");
            if (double.IsPositiveInfinity(x)) return x;
            long b = BitConverter.DoubleToInt64Bits(x);
            int e = (int)(b >> 52);
            if (e == 0)                                  // alt-normal: 2^54 ile normalleştir
            {
                b = BitConverter.DoubleToInt64Bits(x * Two54);
                e = (int)(b >> 52) - 54;
            }
            e -= 1023;
            double m = BitConverter.Int64BitsToDouble((b & 0x000FFFFFFFFFFFFFL) | 0x3FF0000000000000L);
            if (m > Sqrt2) { m *= 0.5; e += 1; }
            double f = m - 1.0;                          // Sterbenz: TAM
            double s = f / (2.0 + f);
            double z = s * s;                            // z ≤ 0.0295 → z^11/23 < 1e-18
            double rz = z * (1.0 / 3 + z * (1.0 / 5 + z * (1.0 / 7 + z * (1.0 / 9 + z * (1.0 / 11
                      + z * (1.0 / 13 + z * (1.0 / 15 + z * (1.0 / 17 + z * (1.0 / 19 + z * (1.0 / 21))))))))));
            double lnm = 2.0 * s + 2.0 * s * rz;
            double ed = e;
            return ed * Ln2Hi + (ed * Ln2Lo + lnm);
        }

        /// <summary>x^y, x ≥ 0 (simin bütün kullanımları negatif olmayan taban: enerji oranı,
        /// hız oranı). Negatif taban tanım dışıdır ve fırlatır.</summary>
        public static double Pow(double x, double y)
        {
            if (double.IsNaN(x) || double.IsNaN(y)) throw new ArgumentOutOfRangeException(nameof(x), "DetMath.Pow: NaN");
            if (x < 0.0) throw new ArgumentOutOfRangeException(nameof(x), "DetMath.Pow: x ≥ 0 olmalı");
            if (y == 0.0 || x == 1.0) return 1.0;
            if (x == 0.0) return y > 0.0 ? 0.0 : double.PositiveInfinity;
            return Exp(y * Log(x));
        }

        // ============================ Sin / Cos / Tan ============================

        // musl __sin/__cos çekirdekleri (|r| ≤ π/4, kuyruk y = 0).
        const double S1 = -1.66666666666666324348e-01, S2 = 8.33333333332248946124e-03,
                     S3 = -1.98412698298579493134e-04, S4 = 2.75573137070700676789e-06,
                     S5 = -2.50507602534068634195e-08, S6 = 1.58969099521155010221e-10;
        const double C1 = 4.16666666666666019037e-02, C2 = -1.38888888888741095749e-03,
                     C3 = 2.48015872894767294178e-05, C4 = -2.75573143513906633035e-07,
                     C5 = 2.08757232129817482790e-09, C6 = -1.13596475577881948265e-11;

        static double KSin(double x)
        {
            double z = x * x, w = z * z;
            double r = S2 + z * (S3 + z * S4) + z * w * (S5 + z * S6);
            double v = z * x;
            return x + v * (S1 + z * r);
        }

        static double KCos(double x)
        {
            double z = x * x, w = z * z;
            double r = z * (C1 + z * (C2 + z * C3)) + w * w * (C4 + z * (C5 + z * C6));
            double hz = 0.5 * z;
            double ww = 1.0 - hz;
            return ww + (((1.0 - ww) - hz) + z * r);
        }

        // x = n·(π/2) + r, |r| ≲ π/4. |x| ≤ TrigMaxArg; dışı (ve NaN) fırlatır.
        static void Reduce(double x, out int n, out double r)
        {
            if (!(Math.Abs(x) <= TrigMaxArg))
                throw new ArgumentOutOfRangeException(nameof(x), "DetMath trig: |x| ≤ 1e5 olmalı");
            if (Math.Abs(x) <= Pio4) { n = 0; r = x; return; }
            double nd = Math.Floor(x * InvPio2 + 0.5);
            n = (int)nd;
            r = (x - nd * Pio2_1) - nd * Pio2_1t;
        }

        public static double Sin(double x)
        {
            Reduce(x, out int n, out double r);
            switch (n & 3)
            {
                case 0: return KSin(r);
                case 1: return KCos(r);
                case 2: return -KSin(r);
                default: return -KCos(r);
            }
        }

        public static double Cos(double x)
        {
            Reduce(x, out int n, out double r);
            switch (n & 3)
            {
                case 0: return KCos(r);
                case 1: return -KSin(r);
                case 2: return -KCos(r);
                default: return KSin(r);
            }
        }

        /// <summary>tan x = sin/cos çekirdeklerinin TEK bölümü (tek n'de −cos/sin).</summary>
        public static double Tan(double x)
        {
            Reduce(x, out int n, out double r);
            return (n & 1) == 0 ? KSin(r) / KCos(r) : -KCos(r) / KSin(r);
        }

        // ============================ Atan / Atan2 ============================

        // musl atan: kırılma noktaları 7/16, 11/16, 19/16, 39/16 ve derece-11 tek polinom.
        static readonly double[] AtanHi =
        {
            Bits(0x3FDDAC670561BB4FL), Bits(0x3FE921FB54442D18L),
            Bits(0x3FEF730BD281F69BL), Bits(0x3FF921FB54442D18L),
        };
        static readonly double[] AtanLo =
        {
            Bits(0x3C7A2B7F222F65E2L), Bits(0x3C81A62633145C07L),
            Bits(0x3C7007887AF0CBBDL), Bits(0x3C91A62633145C07L),
        };
        const double AT0 = 3.33333333333329318027e-01, AT1 = -1.99999999998764832476e-01,
                     AT2 = 1.42857142725034663711e-01, AT3 = -1.11111104054623557880e-01,
                     AT4 = 9.09088713343650656196e-02, AT5 = -7.69187620504482999495e-02,
                     AT6 = 6.66107313738753120669e-02, AT7 = -5.83357013379057348645e-02,
                     AT8 = 4.97687799461593236017e-02, AT9 = -3.65315727442169155270e-02,
                     AT10 = 1.62858201153657823623e-02;

        public static double Atan(double x)
        {
            if (double.IsNaN(x)) throw new ArgumentOutOfRangeException(nameof(x), "DetMath.Atan: NaN");
            bool neg = x < 0.0;
            double ax = Math.Abs(x);
            if (ax >= 7.3786976294838206464e19)            // 2^66: atan = ±π/2
                return neg ? -(AtanHi[3] + AtanLo[3]) : AtanHi[3] + AtanLo[3];
            int id;
            if (ax < 0.4375) { id = -1; }
            else if (ax < 0.6875) { id = 0; ax = (2.0 * ax - 1.0) / (2.0 + ax); }
            else if (ax < 1.1875) { id = 1; ax = (ax - 1.0) / (ax + 1.0); }
            else if (ax < 2.4375) { id = 2; ax = (ax - 1.5) / (1.0 + 1.5 * ax); }
            else { id = 3; ax = -1.0 / ax; }
            double z = ax * ax, w = z * z;
            double s1 = z * (AT0 + w * (AT2 + w * (AT4 + w * (AT6 + w * (AT8 + w * AT10)))));
            double s2 = w * (AT1 + w * (AT3 + w * (AT5 + w * (AT7 + w * AT9))));
            double res = id < 0 ? ax - ax * (s1 + s2)
                                : AtanHi[id] - ((ax * (s1 + s2) - AtanLo[id]) - ax);
            return neg ? -res : res;
        }

        /// <summary>atan2(y, x) ∈ [−π, π]. Sonlu girdiler; ±0 ayrımı: x = +0 ve y = 0 → y.</summary>
        public static double Atan2(double y, double x)
        {
            if (double.IsNaN(x) || double.IsNaN(y)) throw new ArgumentOutOfRangeException(nameof(x), "DetMath.Atan2: NaN");
            if (double.IsInfinity(x) || double.IsInfinity(y))
                throw new ArgumentOutOfRangeException(nameof(x), "DetMath.Atan2: sonlu girdi");
            if (y == 0.0)
            {
                if (x > 0.0 || (x == 0.0 && BitConverter.DoubleToInt64Bits(x) >= 0)) return y;
                return BitConverter.DoubleToInt64Bits(y) < 0 ? -(PiHi + PiLo) : PiHi + PiLo;
            }
            if (x == 0.0) return y > 0.0 ? AtanHi[3] + AtanLo[3] : -(AtanHi[3] + AtanLo[3]);
            double z = Atan(Math.Abs(y / x));
            if (x > 0.0) return y > 0.0 ? z : -z;
            return y > 0.0 ? PiHi - (z - PiLo) : (z - PiLo) - PiHi;
        }
    }
}
