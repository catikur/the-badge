using System;

namespace TheBadge.World
{
    /// <summary>`balance/economy.balance.json` çekirdek POCO'su. Host doldurur (çekirdek JSON
    /// parse etmez). Sözleşmesi `docs/ECONOMY_MAP.md`: sezon source/sink oranı 1,05-1,15 ve
    /// maaş sink'i toplam sink'in %45-60'ı. Tüm tutarlar TAM ₺.</summary>
    [Serializable]
    public sealed class EconomyBalance
    {
        public int surum;
        public TribunCfg tribun = new TribunCfg();
        public SeyirciCfg seyirci = new SeyirciCfg();
        public MacGunuCfg macGunu = new MacGunuCfg();
        public KombineCfg kombine = new KombineCfg();
        public GelirCfg gelir = new GelirCfg();
        public GiderCfg gider = new GiderCfg();
        public KrediCfg kredi = new KrediCfg();
        public InsaatCfg insaat = new InsaatCfg();
        public IflasCfg iflas = new IflasCfg();

        [Serializable] public sealed class TribunCfg
        {
            public int[] kapasitePayiBinde = new int[0];   // toplam 1000
            public int[] referansFiyat = new int[0];       // ₺
        }
        [Serializable] public sealed class SeyirciCfg
        {
            public double tabanDoluluk, minDoluluk, fiyatElastikiyet, formEtkisi, tesisEtkisi, varyansSigma;
        }
        [Serializable] public sealed class MacGunuCfg
        {
            public double bufeHarcamaTaban, bufeReferansFiyat, bufeElastikiyet;
            public double magazaHarcamaTaban, magazaReferansFiyat, magazaElastikiyet;
            public double evMacOrani;
        }
        [Serializable] public sealed class KombineCfg { public double referansFiyat, elastikiyet, kapasiteOrani; }
        [Serializable] public sealed class GelirCfg
        {
            public long yayinHaftalik, sponsorHaftalikTaban, galibiyetPrimi, beraberlikPrimi;
        }
        [Serializable] public sealed class GiderCfg
        {
            public long tesisBakimHaftalikTierBasi, personelHaftalik, genelIsletmeHaftalik;
        }
        [Serializable] public sealed class KrediCfg { public int yillikFaizBp; public double aylikTaksitBolen; }
        [Serializable] public sealed class InsaatCfg
        {
            public long tierMaliyetTaban;
            public double tierMaliyetCarpan, iptalIadeOrani;
            public int[] tierSureHafta = new int[0];
            public int[] kapasiteTier = new int[0];
        }
        [Serializable] public sealed class IflasCfg { public long esikTl; }

        /// <summary>Yapılandırmanın kendi tutarlılığı — kurulumda bir kez. Eksik balance sessizce
        /// kabul edilirse hata çok sonra ve yanlış yerde görünür.</summary>
        public void Validate()
        {
            if (tribun.kapasitePayiBinde.Length != 5 || tribun.referansFiyat.Length != 5)
                throw new ArgumentException("economy.balance: tribun dizileri 5 uzunlukta olmalı.");
            int toplam = 0;
            for (int i = 0; i < 5; i++) toplam += tribun.kapasitePayiBinde[i];
            if (toplam != 1000) throw new ArgumentException($"economy.balance: kapasitePayiBinde toplamı 1000 olmalı (şu an {toplam}).");
            if (seyirci.tabanDoluluk <= 0 || seyirci.tabanDoluluk > 1) throw new ArgumentException("economy.balance: tabanDoluluk (0,1] olmalı.");
            if (seyirci.minDoluluk < 0 || seyirci.minDoluluk > seyirci.tabanDoluluk)
                throw new ArgumentException("economy.balance: 0 ≤ minDoluluk ≤ tabanDoluluk olmalı.");
            if (insaat.tierSureHafta.Length < 6 || insaat.kapasiteTier.Length < 6)
                throw new ArgumentException("economy.balance: insaat tier dizileri en az 6 uzunlukta olmalı (index = tier).");
            if (kredi.yillikFaizBp < 0) throw new ArgumentException("economy.balance: faiz negatif olamaz.");
        }

        /// <summary>Tesis tier'ının inşaat maliyeti — üstel: taban × çarpan^(tier-1).</summary>
        public long TierMaliyet(int hedefTier)
        {
            double m = insaat.tierMaliyetTaban;
            for (int i = 1; i < hedefTier; i++) m *= insaat.tierMaliyetCarpan;
            return (long)Math.Round(m, MidpointRounding.AwayFromZero);
        }
    }
}
