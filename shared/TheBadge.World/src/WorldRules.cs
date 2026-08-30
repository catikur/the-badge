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
        public TaktikCfg taktik = new TaktikCfg();

        [Serializable]
        public sealed class Kapi3Cfg
        {
            /// <summary>Transfer penceresinin AÇIK olmasını gerektiren aksiyonlar. Kodda sabit
            /// liste yerine yapılandırma: hangi işlemin pencereye tabi olduğu bir TASARIM
            /// kararıdır ve K5'te (Transfer) kesinleşir — kod değişmeden ayarlanabilir.</summary>
            public string[] pencereGerektiren = new string[0];
        }

        /// <summary>Taktik delta → mutlak dönüşümü. Katalog [-2, +2] DELTA verir (CB 4.2);
        /// kalıcı durum 0-100 MUTLAK tutar. Bir delta adımı `adim` puan kaydırır ve sonuç
        /// [min, max] aralığına kırpılır — kodda sabit yok.</summary>
        [Serializable]
        public sealed class TaktikCfg { public int adim, min, max; }

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
            public int presetSlotSayisi;         // [KALİBRE] kayıtlı taktik şablonu (CB bandı 1-20)
            public int talimatYuvaSayisi;        // [KALİBRE] oyuncu başına eşzamanlı talimat
            public int transferTeklifSlotSayisi; // [KALİBRE] eşzamanlı açık transfer teklifi
            public int transferTeklifIdMax;      // [KALİBRE] katalog bandı `transfer.teklifId` ÜST SINIRIYLA aynı olmalı
            public int cevrimdisiKuyrukTavani;   // [KALİBRE] CB 8.3 offline kuyruk uzunluğu
            public int personelSlotSayisi;       // [KALİBRE] eşzamanlı personel
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
            if (yapi.presetSlotSayisi <= 0) throw new ArgumentException("world.balance: yapi.presetSlotSayisi > 0 olmalı.");
            if (yapi.talimatYuvaSayisi <= 0) throw new ArgumentException("world.balance: yapi.talimatYuvaSayisi > 0 olmalı.");
            if (yapi.transferTeklifSlotSayisi <= 0) throw new ArgumentException("world.balance: yapi.transferTeklifSlotSayisi > 0 olmalı.");
            if (yapi.cevrimdisiKuyrukTavani <= 0) throw new ArgumentException("world.balance: yapi.cevrimdisiKuyrukTavani > 0 olmalı.");
            if (yapi.personelSlotSayisi <= 0) throw new ArgumentException("world.balance: yapi.personelSlotSayisi > 0 olmalı.");
            if (yapi.transferTeklifIdMax < yapi.transferTeklifSlotSayisi)
                throw new ArgumentException("world.balance: yapi.transferTeklifIdMax ≥ transferTeklifSlotSayisi olmalı.");
            if (taktik.adim <= 0) throw new ArgumentException("world.balance: taktik.adim > 0 olmalı.");
            if (taktik.min >= taktik.max) throw new ArgumentException("world.balance: taktik.min < taktik.max olmalı.");
        }
    }
}
