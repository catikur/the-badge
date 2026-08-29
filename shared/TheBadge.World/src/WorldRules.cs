using System;

namespace TheBadge.World
{
    /// <summary>`balance/world.balance.json`'ın çekirdek POCO'su. ÇEKİRDEK JSON PARSE ETMEZ —
    /// bağımlılıksızlık kuralı (CLAUDE.md); host doldurur, alan adları JSON anahtarlarıyla
    /// birebirdir. Varsayılanlar 0/boş: balance yüklenmemişse durum GÖRÜNÜR bozulur.</summary>
    [Serializable]
    public sealed class WorldRules
    {
        public int surum;
        public Kapi3Cfg kapi3 = new Kapi3Cfg();
        public YapiCfg yapi = new YapiCfg();

        [Serializable]
        public sealed class Kapi3Cfg
        {
            /// <summary>Transfer penceresinin AÇIK olmasını gerektiren aksiyonlar. Kodda sabit
            /// liste yerine yapılandırma: hangi işlemin pencereye tabi olduğu bir TASARIM
            /// kararıdır ve K5'te (Transfer) kesinleşir — kod değişmeden ayarlanabilir.</summary>
            public string[] pencereGerektiren = new string[0];
        }

        [Serializable]
        public sealed class YapiCfg
        {
            public int insaatSlotSayisi;      // [KALİBRE] eşzamanlı inşaat sayısı
            public int krediSlotSayisi;       // [KALİBRE] eşzamanlı kredi sayısı
            public int tesisSayisi;           // tesisId üst sınırı (bant `tycoon.tesisId` ile uyumlu)
            public int kadroMin;              // [KALİBRE] altına düşüren fesih/satış reddedilir
            public int kadroMax;              // [KALİBRE]
            public int sezonHaftaSayisi;      // [KALİBRE]
            public int macBasinaDegisiklik;   // [KALİBRE] ME 14.2 değişiklik hakkı
            public int sponsorTeklifSlotSayisi;  // [KALİBRE] eşzamanlı sponsor teklifi
        }

        /// <summary>Aksiyon transfer penceresi istiyor mu. Liste küçüktür (tek haneli); sırasız
        /// yapı KULLANILMAZ (ME 3.2 disiplini) — doğrusal tarama yeterlidir.</summary>
        public bool RequiresTransferWindow(string actionType)
        {
            var l = kapi3.pencereGerektiren;
            if (l == null || actionType == null) return false;
            for (int i = 0; i < l.Length; i++)
                if (string.Equals(l[i], actionType, StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>Yapılandırmanın kendi tutarlılığı — kurulumda bir kez. Eksik/çelişkili
        /// balance sessizce kabul edilirse hata çok sonra ve yanlış yerde görünür.</summary>
        public void Validate()
        {
            if (yapi.insaatSlotSayisi <= 0) throw new ArgumentException("world.balance: yapi.insaatSlotSayisi > 0 olmalı.");
            if (yapi.krediSlotSayisi <= 0) throw new ArgumentException("world.balance: yapi.krediSlotSayisi > 0 olmalı.");
            if (yapi.tesisSayisi <= 0) throw new ArgumentException("world.balance: yapi.tesisSayisi > 0 olmalı.");
            if (yapi.kadroMin <= 0 || yapi.kadroMax < yapi.kadroMin)
                throw new ArgumentException("world.balance: 0 < kadroMin ≤ kadroMax olmalı.");
            if (yapi.sezonHaftaSayisi <= 0) throw new ArgumentException("world.balance: yapi.sezonHaftaSayisi > 0 olmalı.");
            if (yapi.macBasinaDegisiklik < 0) throw new ArgumentException("world.balance: yapi.macBasinaDegisiklik ≥ 0 olmalı.");
            if (yapi.sponsorTeklifSlotSayisi <= 0) throw new ArgumentException("world.balance: yapi.sponsorTeklifSlotSayisi > 0 olmalı.");
        }
    }
}
