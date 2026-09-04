using TheBadge.Sim.Config;

namespace TheBadge.Sim.Match
{
    /// <summary>TAKIM GÜCÜ — TEK TANIM (0-100). Ağırlıklar `sim.balance.json → lod.guc` [KALİBRE].
    ///
    /// NEDEN AYRI DOSYA: bu büyüklüğün iki tüketicisi var — LOD 2 çözücüsü (ME 16.1) ve canlı
    /// kazanma olasılığı (`LiveWinProb`). İkisi kendi kopyasını taşısaydı, ağırlıklardan biri
    /// değiştiğinde diğeri sessizce eski tanımla çalışırdı. Bu projenin tekrar eden hatası tam
    /// olarak budur (K11 dikişi, S1 asmdef↔csproj sürüklenmesi): iki taraf ayrı ayrı doğru,
    /// aradaki tanım kaymış. `Lod2Resolver.TeamStrength` buraya devrediyor.
    ///
    /// KALECİ AYRI BİLEŞEN: nitelik seti saha oyuncusuyla ortak değildir (Reflexes/Handling
    /// saha oyuncusunda anlamsız).
    ///
    /// EKSİK OYUNCU SIFIR SAYILIR: saha payı DAİMA 10'a bölünür, sahadaki oyuncu sayısına değil.
    /// Kırmızı kart gören takımın gücü böylece düşer; gerçek sayıya bölmek ortalamayı sabit
    /// tutar ve on kişi kalmak GÜCÜ HİÇ ETKİLEMEZDİ.</summary>
    public static class TeamRating
    {
        /// <summary>Kaleci bileşeni (ağırlıklı nitelik toplamı).</summary>
        public static double Kaleci(SimBalance bal, in PlayerAttributes a)
        {
            var g = bal.lod.guc.kaleci;
            return g.reflexes * a.Reflexes + g.handling * a.Handling
                 + g.oneOnOne * a.OneOnOne + g.aerialCommand * a.AerialCommand;
        }

        /// <summary>Tek saha oyuncusunun bileşeni.</summary>
        public static double SahaOyuncusu(SimBalance bal, in PlayerAttributes a)
        {
            var o = bal.lod.guc.sahaOyuncusu;
            return o.passing * a.Passing + o.finishing * a.Finishing + o.tackling * a.Tackling
                 + o.pace * a.Pace + o.positioning * a.Positioning + o.decisions * a.Decisions
                 + o.firstTouch * a.FirstTouch + o.strength * a.Strength;
        }

        /// <summary>Kaleci puanı + saha oyuncusu TOPLAMI → takım gücü.
        /// `sahaToplami` eksik (atılmış/ağır sakat) oyuncuları İÇERMEZ; bölen yine 10'dur.</summary>
        public static double Birlestir(SimBalance bal, double kaleciPuani, double sahaToplami)
        {
            double kp = bal.lod.guc.kaleciPayi;
            return kp * kaleciPuani + (1.0 - kp) * (sahaToplami / 10.0);
        }

        /// <summary>Maç ÖNCESİ güç: ilk 11'den (Starters[0] kaleci).</summary>
        public static double FromSheet(SimBalance bal, TeamSheet sheet)
        {
            double saha = 0;
            for (int i = 1; i < 11; i++) saha += SahaOyuncusu(bal, sheet.Starters[i].Attributes);
            return Birlestir(bal, Kaleci(bal, sheet.Starters[0].Attributes), saha);
        }
    }
}
