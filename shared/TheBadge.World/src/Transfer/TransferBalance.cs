using System;

namespace TheBadge.World
{
    /// <summary>`balance/transfer.balance.json` karşılığı — GDD 17 FAZ 04 "Valuation algoritması
    /// + negotiation logic". Tüm katsayılar [KALİBRE]; kodda transfer sabiti YOKTUR.
    /// config_hash İÇİdir: sezon içinde donuk (CLAUDE.md değişmez #4).</summary>
    [Serializable]
    public sealed class TransferBalance
    {
        public int surum;
        public DegerlemeCfg degerleme = new DegerlemeCfg();
        public PazarlikCfg pazarlik = new PazarlikCfg();
        public FesihCfg fesih = new FesihCfg();

        [Serializable] public sealed class DegerlemeCfg
        {
            public long tabanTl;
            public double gucUsteli;             // güç eğrisinin üsteli (dışbükey: yıldız primi)
            public double gucOlcek;              // güç normalizasyon böleni
            public double potansiyelPrimOran;    // (potansiyel-güç) farkının katkısı
            public int zirveYas;
            public double gencPrimYilBasina;
            public double yasliCezaYilBasina;
            public double yasTabanCarpan;        // yaş çarpanının ALT sınırı (değer sıfırlanmaz)
            public int sozlesmeTamYilHafta;
            public double sozlesmeBitisTabanOran; // sözleşmesi biten oyuncunun bedel tabanı
            public long tavanTl;                 // katalog bandı `transfer.bedel` ile aynı tavan
        }
        [Serializable] public sealed class PazarlikCfg
        {
            public double kabulEsigiOran;        // değerin bu oranı ve üstü → kabul
            public double redEsigiOran;          // bu oranın altı → ret (pazarlık bile yok)
            public double karsiTeklifHedefOran;  // karşı teklif değerin bu katı
            public double kisilikSalinimOran;    // ±oran, deterministik RNG ile
            public double maasTalepOran;         // haftalık maaş talebi = değer × oran
            public int maxTur;
            public int teklifGecerlilikHafta;
            // ALICI tarafı eşikleri — satıcınınkilerin AYNASI DEĞİL, TERSİ. Satıcı "yeterince
            // YÜKSEK"i kabul eder; alıcı "yeterince DÜŞÜK"ü. Aynı rutini iki tarafa da kullanmak,
            // alıcının fahiş fiyatı kabul etmesine yol açıyordu (inceleme bulgusu, P1).
            public double aliciKabulEsigiOran;       // değerin bu katı ve ALTI → kabul
            public double aliciRedEsigiOran;         // değerin bu katı ve ÜSTÜ → ret
            public double aliciKarsiTeklifHedefOran; // alıcı AŞAĞI pazarlık eder
        }
        [Serializable] public sealed class FesihCfg
        {
            public double kalanHaftaCarpani;     // fesih bedeli = kalan hafta × maaş × çarpan
            public long asgariTl;
        }

        /// <summary>Bozuk yapılandırma AÇILIŞTA patlar — sessiz varsayılan yok (K2 dersi).</summary>
        public void Validate()
        {
            if (degerleme.tabanTl <= 0) throw new ArgumentException("transfer.balance: degerleme.tabanTl > 0 olmalı.");
            if (degerleme.gucOlcek <= 0) throw new ArgumentException("transfer.balance: degerleme.gucOlcek > 0 olmalı.");
            if (degerleme.gucUsteli <= 0) throw new ArgumentException("transfer.balance: degerleme.gucUsteli > 0 olmalı.");
            if (degerleme.zirveYas <= 0) throw new ArgumentException("transfer.balance: degerleme.zirveYas > 0 olmalı.");
            if (degerleme.yasTabanCarpan <= 0 || degerleme.yasTabanCarpan > 1)
                throw new ArgumentException("transfer.balance: degerleme.yasTabanCarpan (0,1] olmalı.");
            if (degerleme.sozlesmeTamYilHafta <= 0) throw new ArgumentException("transfer.balance: degerleme.sozlesmeTamYilHafta > 0 olmalı.");
            if (degerleme.sozlesmeBitisTabanOran <= 0 || degerleme.sozlesmeBitisTabanOran > 1)
                throw new ArgumentException("transfer.balance: degerleme.sozlesmeBitisTabanOran (0,1] olmalı.");
            if (degerleme.tavanTl <= 0) throw new ArgumentException("transfer.balance: degerleme.tavanTl > 0 olmalı.");
            // Eşik SIRASI anlamlıdır: ret eşiği kabul eşiğinin altında olmalı, yoksa pazarlık
            // penceresi kapanır ve karşı teklif ASLA üretilmez (sessizce ölü kod).
            if (!(pazarlik.redEsigiOran < pazarlik.kabulEsigiOran))
                throw new ArgumentException("transfer.balance: pazarlik.redEsigiOran < kabulEsigiOran olmalı.");
            if (pazarlik.karsiTeklifHedefOran < pazarlik.kabulEsigiOran)
                throw new ArgumentException("transfer.balance: pazarlik.karsiTeklifHedefOran ≥ kabulEsigiOran olmalı.");
            if (pazarlik.kisilikSalinimOran < 0 || pazarlik.kisilikSalinimOran >= pazarlik.kabulEsigiOran)
                throw new ArgumentException("transfer.balance: pazarlik.kisilikSalinimOran [0, kabulEsigiOran) olmalı.");
            if (pazarlik.maxTur <= 0) throw new ArgumentException("transfer.balance: pazarlik.maxTur > 0 olmalı.");
            if (pazarlik.teklifGecerlilikHafta <= 0) throw new ArgumentException("transfer.balance: pazarlik.teklifGecerlilikHafta > 0 olmalı.");
            if (pazarlik.maasTalepOran <= 0) throw new ArgumentException("transfer.balance: pazarlik.maasTalepOran > 0 olmalı.");
            // Alıcı eşiklerinin SIRASI satıcınınkinin TERSİdir: kabul eşiği ret eşiğinin ALTINDA
            // olmalı. Ters kurulursa pazarlık penceresi kapanır ve alıcı hiç karşı teklif vermez.
            if (!(pazarlik.aliciKabulEsigiOran < pazarlik.aliciRedEsigiOran))
                throw new ArgumentException("transfer.balance: pazarlik.aliciKabulEsigiOran < aliciRedEsigiOran olmalı.");
            if (pazarlik.aliciKarsiTeklifHedefOran <= 0 || pazarlik.aliciKarsiTeklifHedefOran > pazarlik.aliciKabulEsigiOran)
                throw new ArgumentException("transfer.balance: pazarlik.aliciKarsiTeklifHedefOran (0, aliciKabulEsigiOran] olmalı.");
            if (fesih.kalanHaftaCarpani < 0) throw new ArgumentException("transfer.balance: fesih.kalanHaftaCarpani ≥ 0 olmalı.");
            if (fesih.asgariTl < 0) throw new ArgumentException("transfer.balance: fesih.asgariTl ≥ 0 olmalı.");
        }
    }
}
