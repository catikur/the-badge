using System;
using TheBadge.Sim.Determinism;

namespace TheBadge.World
{
    /// <summary>Pazarlık kararı — CB 4.3 `transfer.respond_offer` cevap kümesiyle aynı.</summary>
    public enum PazarlikKarari : byte { Ret = 0, Kabul = 1, KarsiTeklif = 2 }

    /// <summary>DEĞERLEME + PAZARLIK — GDD 17 FAZ 04 "Valuation algoritması, negotiation logic".
    ///
    /// DETERMİNİZM: saf fonksiyonlar; durum okur, yazmaz. Rastgelelik yalnız `Rng.Rand01` ile ve
    /// yalnız pazarlık kişiliğinde. `Gauss01` KULLANILMAZ: yazıldığında çakışma borcu açıktı
    /// (komşu tick'lerde ve bit0-farklı tohumlarda aynı çekiliş kümesi). Borç K8'de ödendi
    /// (2026-08-30) ama bu çağrı yeri `Rand01` tabanlı KALIYOR — geçmek transfer sonuçlarını
    /// kaydırır ve karşılığında bir şey kazandırmaz. K3 ekonomi tick'i de aynı yerde duruyor.
    ///
    /// Ara hesap `double`, SONUÇ tamsayı (₺). Kalıcı durumda float taşınmaz (CLAUDE.md).</summary>
    public static class Valuation
    {
        /// <summary>Piyasa değeri (₺) — güç eğrisi × potansiyel primi × yaş çarpanı × sözleşme çarpanı.
        /// MONOTONLUK sözleşmesi (kapı bunu ölçer): diğerleri sabitken güç ↑ → değer ↑,
        /// potansiyel ↑ → değer ↑ (ya da eşit), sözleşme kalanı ↑ → değer ↑ (ya da eşit).</summary>
        public static long PiyasaDegeri(in PlayerState p, TransferBalance tb)
        {
            if (tb == null) throw new ArgumentNullException(nameof(tb));
            var d = tb.degerleme;

            // Güç eğrisi DIŞBÜKEY: 90 güçlü oyuncu 45 güçlünün iki katı değil, kat kat pahalıdır.
            double gucKat = Math.Pow(p.Guc / d.gucOlcek, d.gucUsteli);

            // Potansiyel primi: yalnız POZİTİF fark (potansiyel < güç ise prim 0, ceza değil —
            // "tavanına ulaşmış oyuncu" cezalandırılmaz, yalnız prim almaz).
            int fark = p.Potansiyel > p.Guc ? p.Potansiyel - p.Guc : 0;
            double potKat = 1.0 + (fark / d.gucOlcek) * d.potansiyelPrimOran;

            // Yaş çarpanı: zirveye kadar prim, sonrasında ceza; ALT SINIR var (yaşlı oyuncunun
            // değeri sıfıra inmez — sözleşmesi devredilebilir bir varlıktır).
            double yasKat;
            if (p.Yas <= d.zirveYas) yasKat = 1.0 + (d.zirveYas - p.Yas) * d.gencPrimYilBasina;
            else yasKat = 1.0 - (p.Yas - d.zirveYas) * d.yasliCezaYilBasina;
            if (yasKat < d.yasTabanCarpan) yasKat = d.yasTabanCarpan;

            // Sözleşme çarpanı: bitmeye yakın oyuncu ucuzlar (yakında bedelsiz olacak).
            // Bir tam yıl ve üstü = 1.0; sıfır hafta = tabanOran; arası doğrusal.
            double yil = p.SozlesmeKalanHafta / (double)d.sozlesmeTamYilHafta;
            double sozKat = yil >= 1.0 ? 1.0
                          : d.sozlesmeBitisTabanOran + (1.0 - d.sozlesmeBitisTabanOran) * yil;

            double deger = d.tabanTl * gucKat * potKat * yasKat * sozKat;
            if (deger < 0) deger = 0;
            if (deger > d.tavanTl) deger = d.tavanTl;
            return (long)deger;
        }

        /// <summary>Serbest oyuncunun haftalık maaş talebi (₺) — bedel yok, maaş var.</summary>
        /// <param name="kulupOlcegi">Kulübün ölçeği (1,0 = referans kulüp). K13-A: oyuncu büyük
        /// kulüpten daha çok ister; ölçek `EconomyTick.KulupOlcegi` ile ETKİN kapasiteden gelir.
        /// Varsayılan 1,0 — transfer pazarlığı gibi ölçekten bağımsız çağrılar davranış
        /// değiştirmez, yalnız sezon başı ücret gözden geçirmesi ölçek geçirir.</param>
        public static long MaasTalebi(in PlayerState p, TransferBalance tb, double kulupOlcegi = 1.0)
        {
            long deger = PiyasaDegeri(p, tb);
            double m = deger * tb.pazarlik.maasTalepOran;
            if (kulupOlcegi > 1.0) m *= kulupOlcegi;
            return (long)m;
        }

        /// <summary>Fesih bedeli (₺) — kalan sözleşme × haftalık maaş × çarpan, asgari tabanlı.
        /// GDD 4.2: oyuncu göndermek BEDAVA değildir; aksi hâlde maaş yükü sıfır maliyetle atılırdı.</summary>
        public static long FesihBedeli(in PlayerState p, TransferBalance tb)
        {
            double b = p.SozlesmeKalanHafta * (double)p.HaftalikMaasTl * tb.fesih.kalanHaftaCarpani;
            long v = (long)b;
            return v < tb.fesih.asgariTl ? tb.fesih.asgariTl : v;
        }

        /// <summary>ALICI kulübün kararı — satıcınınkinin TERSİ, aynası değil.
        ///
        /// Satıcı "yeterince YÜKSEK"i kabul eder; alıcı "yeterince DÜŞÜK"ü. İlk yazımda tek
        /// rutin iki tarafa da kullanılıyordu ve alıcı, istenen fiyat ne kadar yüksekse o kadar
        /// istekli oluyordu: kullanıcı gelen teklife FAHİŞ bir karşı teklif verip AI'ya kabul
        /// ettirebiliyordu (inceleme bulgusu, P1 — para basma yolu).
        ///
        /// `istenenBedel` satıcının istediği fiyattır; alıcı bunu değerine göre tartar.</summary>
        public static PazarlikKarari AliciKarari(in PlayerState p, long istenenBedel, byte tur,
                                                 TransferBalance tb, ulong saveSeed, out long karsiBedel)
        {
            karsiBedel = 0;
            long deger = PiyasaDegeri(p, tb);
            var pz = tb.pazarlik;

            double u = Rng.Rand01(saveSeed, Domain.Decision, (uint)p.PlayerId, tur, 1);
            double salinim = (u * 2.0 - 1.0) * pz.kisilikSalinimOran;

            double kabulTavani = deger * (pz.aliciKabulEsigiOran + salinim);
            double redTabani = deger * (pz.aliciRedEsigiOran + salinim);

            if (istenenBedel <= kabulTavani) return PazarlikKarari.Kabul;     // ucuz → al
            if (istenenBedel > redTabani || tur >= pz.maxTur) return PazarlikKarari.Ret;  // fahiş → çekil

            double hedef = deger * (pz.aliciKarsiTeklifHedefOran + salinim);
            // Alıcının karşı teklifi istenen bedelden YÜKSEK olamaz: olsaydı "pazarlık" satıcıyı
            // daha ÇOK istemeye davet ederdi.
            if (hedef > istenenBedel) hedef = istenenBedel;
            if (hedef < 0) hedef = 0;
            karsiBedel = (long)hedef;
            return PazarlikKarari.KarsiTeklif;
        }

        /// <summary>Satıcı kulübün kararı. `saveSeed` + oyuncu + tur pazarlığı ADRESLER: aynı
        /// girdi her zaman aynı kararı verir, çağrı sırasından bağımsız (ME 3.1 sayaç-RNG).
        ///
        /// DOMAIN SEÇİMİ — `Decision`: bu bir AJAN KARARIdır (satıcı kulübün pazarlık tutumu),
        /// fiziksel bir olay ya da düello değil. `Chaos` reddedildi: kaos akışı maç içi sapma
        /// içindir ve transfer kararına bağlamak iki alanı aynı sayaç uzayında çakıştırırdı.
        ///
        /// `istenenBedelTl` > 0 ise oyuncu transfer listesindedir ve o bedel değerin YERİNE
        /// geçer — sahibinin açıkladığı fiyat, hesaplanan değerden önce gelir.</summary>
        public static PazarlikKarari Karar(in PlayerState p, long teklifBedel, byte tur,
                                           TransferBalance tb, ulong saveSeed, out long karsiBedel)
        {
            karsiBedel = 0;
            long deger = p.IstenenBedelTl > 0 ? p.IstenenBedelTl : PiyasaDegeri(p, tb);
            var pz = tb.pazarlik;

            // Kişilik salınımı: aynı oyuncu için tur boyunca DEĞİŞİR (tur `tick` adresidir),
            // ama aynı (seed, oyuncu, tur) her zaman aynı sayıyı verir.
            double u = Rng.Rand01(saveSeed, Domain.Decision, (uint)p.PlayerId, tur, 0);
            double salinim = (u * 2.0 - 1.0) * pz.kisilikSalinimOran;

            double kabulEsik = deger * (pz.kabulEsigiOran + salinim);
            double redEsik = deger * (pz.redEsigiOran + salinim);

            if (teklifBedel >= kabulEsik) return PazarlikKarari.Kabul;
            if (teklifBedel < redEsik || tur >= pz.maxTur) return PazarlikKarari.Ret;

            double hedef = deger * (pz.karsiTeklifHedefOran + salinim);
            // Karşı teklif TEKLİFTEN düşük olamaz: düşükse zaten kabul edilirdi ve "pazarlık"
            // alıcıyı daha AZ ödemeye davet ederdi.
            if (hedef < teklifBedel) hedef = teklifBedel;
            if (hedef > tb.degerleme.tavanTl) hedef = tb.degerleme.tavanTl;
            karsiBedel = (long)hedef;
            return PazarlikKarari.KarsiTeklif;
        }
    }
}
