using System;

namespace TheBadge.Greybox.Sim
{
    /// <summary>Bilet gelir projeksiyonu — UI slider önizlemesi ve maç sonu hesabı aynı formülü kullanır.</summary>
    public struct IncomeProjection
    {
        public float Occupancy;     // 0-1 doluluk
        public int Attendance;      // seyirci sayısı
        public long TicketIncome;   // bilet geliri
        public int ResultBonus;     // sonuç primi (yalnız maç sonunda anlamlı)
        public long Total;
    }

    /// <summary>
    /// Greybox tycoon mini-modeli — GDD 4.2: gelir = kapasite × doluluk × fiyat;
    /// doluluk takım başarısına (form) ve bilet fiyatına duyarlıdır.
    /// Deterministik saf fonksiyon: EditMode testleri ve headless harness aynı sonucu doğrular.
    /// </summary>
    public static class TycoonEconomy
    {
        /// <summary>Son 5 sonuçtan (1 galibiyet, 0 beraberlik, -1 mağlubiyet) talep etkisi.</summary>
        public static float FormDemand(GreyboxBalance.EkonomiCfg cfg, int[] lastResults)
        {
            float d = 0f;
            if (lastResults == null) return 0f;
            for (int i = 0; i < lastResults.Length; i++)
            {
                if (lastResults[i] > 0) d += cfg.formEtkiGalibiyet;
                else if (lastResults[i] < 0) d -= cfg.formEtkiMaglubiyet;
            }
            return d;
        }

        public static float Occupancy(GreyboxBalance.EkonomiCfg cfg, float price, int[] lastResults)
        {
            float priceDelta = (price - cfg.refFiyat) / cfg.refFiyat;
            float occ = cfg.talepTaban - cfg.fiyatEsneklik * priceDelta + FormDemand(cfg, lastResults);
            return Clamp(occ, cfg.dolulukMin, 1f);
        }

        /// <summary>result: 1 galibiyet, 0 beraberlik, -1 mağlubiyet; projeksiyon için 0 geç, bonus'u yok say.</summary>
        public static IncomeProjection Project(GreyboxBalance.EkonomiCfg cfg, float price, int[] lastResults, int result)
        {
            var p = new IncomeProjection();
            p.Occupancy = Occupancy(cfg, price, lastResults);
            p.Attendance = (int)Math.Round(cfg.kapasite * p.Occupancy);
            p.TicketIncome = (long)Math.Round(p.Attendance * price);
            p.ResultBonus = result > 0 ? cfg.galibiyetPrimi : (result == 0 ? cfg.beraberlikPrimi : cfg.maglubiyetPrimi);
            p.Total = p.TicketIncome + p.ResultBonus;
            return p;
        }

        static float Clamp(float v, float lo, float hi) => v < lo ? lo : (v > hi ? hi : v);
    }
}
