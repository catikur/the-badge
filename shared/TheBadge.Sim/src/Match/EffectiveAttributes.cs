using System;
using TheBadge.Sim.Config;

namespace TheBadge.Sim.Match
{
    /// <summary>
    /// Efektif nitelik çarpan tabloları — ME Spec 6.2'nin DETERMİNİSTİK hali.
    /// Math.Pow platformlar arası bit garantisi vermez (ME 3.2'nin sin/cos LUT gerekçesiyle aynı);
    /// bu yüzden çarpanlar balance YÜKLENİRKEN bir kez hesaplanır ve Q16 sabit noktaya KUANTALANIR:
    /// tick yolunda yalnız tamsayı LUT + IEEE-kesin küçük çarpımlar kalır. Enerji 10'luk adımlarla
    /// indekslenir (101 giriş; çözünürlük ~0.003 — A_eff zaten tamsayıya yuvarlanır).
    /// </summary>
    public sealed class AttributeLuts
    {
        readonly int[] kondQ16;    // [101] — energy/10 indeksli M_kondisyon (Q16)
        readonly int[] moralQ16;   // [21]  — momentum(-10..+10)+10 indeksli M_moral (Q16)

        AttributeLuts(int[] kond, int[] moral) { kondQ16 = kond; moralQ16 = moral; }

        /// <summary>Balance'tan LUT kurulumu — soğuk yol, maç başına bir kez (host init).</summary>
        public static AttributeLuts Build(SimBalance bal)
        {
            var a = bal.attribute;
            var kond = new int[101];
            for (int i = 0; i <= 100; i++)
            {
                double e01 = i / 100.0; // energy/1000 (10'luk adım)
                double m = a.kondisyonTaban + a.kondisyonKuvvet * Math.Pow(e01, a.kondisyonUs);
                kond[i] = (int)Math.Round(m * 65536.0);
            }
            var moral = new int[21];
            for (int m10 = -10; m10 <= 10; m10++)
                moral[m10 + 10] = (int)Math.Round((1.0 + m10 * a.moralCarpanPerMomentum) * 65536.0);
            return new AttributeLuts(kond, moral);
        }

        public int KondQ16(ushort energy)
        {
            int i = energy / 10;
            if (i > 100) i = 100;
            return kondQ16[i];
        }

        public int MoralQ16(sbyte momentum)
        {
            int i = momentum + 10;
            if (i < 0) i = 0; else if (i > 20) i = 20;
            return moralQ16[i];
        }
    }

    /// <summary>A_eff = taban × M_kondisyon × M_moral × M_hava × M_zemin → [1,100] — ME Spec 6.2.
    /// Her kullanımda TÜRETİLİR; kalıcı durumda tutulmaz, taban değer mutasyona uğramaz (ME 5.2/6.2).
    /// Hava/zemin çarpanları 12.4 dilimine dek nötr (1.0) geçilir — API bağlamı şimdiden alır.</summary>
    public static class EffectiveAttributes
    {
        public static int Compute(byte baseValue, ushort energy, sbyte momentum, AttributeLuts luts,
                                  double weatherMul = 1.0, double pitchMul = 1.0)
        {
            // Q16 değerler tam temsil edilir; küçük tamsayıların double çarpımı IEEE-kesindir —
            // determinizm LUT kuantalamasında güvence altına alındı (sınıf başlığı).
            double eff = baseValue
                         * (luts.KondQ16(energy) / 65536.0)
                         * (luts.MoralQ16(momentum) / 65536.0)
                         * weatherMul * pitchMul;
            int r = (int)Math.Round(eff);
            return r < 1 ? 1 : (r > 100 ? 100 : r);
        }
    }
}
