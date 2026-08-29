using System;
using TheBadge.Sim.Determinism;

namespace TheBadge.World
{
    /// <summary>HAFTALIK EKONOMİ İLERLEMESİ — GDD 4.2/4.4, sözleşme `docs/ECONOMY_MAP.md`.
    ///
    /// DETERMİNİZM: tek rastgelelik kaynağı seyirci varyansıdır ve o da sayaç-RNG'dendir
    /// (ME 3.1 deseni) — `System.Random` YOK, `DateTime` YOK. Aynı save seed + aynı hafta =
    /// aynı seyirci. Domain olarak `Crowd` seçildi: seyirci sayısı taraftar davranışıdır ve maç
    /// simülasyonunun karar/fizik akışlarıyla ÇAKIŞMAMALIDIR (ayrı akış olmasaydı ekonomi
    /// çekilişi maç içi çekilişleri kaydırırdı).
    ///
    /// NEDEN `Gauss01` DEĞİL (K3 bulgusu, 2026-08-25): `Rng.Gauss01` 12 çekilişi
    /// `[16·salt, 16·salt+12)` salt aralığında topluyor; bu küme bit-0 ve bit-1 çevirmeleri
    /// altında KAPALI olduğu için seed'in ya da tick'in o bitini çevirmek salt'ları yalnız kendi
    /// aralarında yer değiştiriyor ve TOPLAM DEĞİŞMİYOR. Ölçüm: komşu tick'lerde %47, bit-0
    /// farklı seed'lerde %94 çarpışma (`Rand01`'de sıfır). Ekonomi bu yüzden tek `Rand01`
    /// çekilişini simetrik üniforma dönüştürerek kullanır — üniformun sd'si a/√3 olduğundan
    /// genlik `sigma·√3` seçilir, böylece istenen standart sapma korunur.
    /// `Gauss01`'in kendisi FAZ 03 borcudur (DECISIONS: bekleyen kararlar) — buradan düzeltmek
    /// tüm golden replay'leri ve M16-E kalibrasyonunu kaydırırdı.
    ///
    /// TEK KAPI: bu sınıf durumu DOĞRUDAN değiştirmez — tüm yazmaları `WorldJournal`a kuyruklar,
    /// uygulamayı `WorldExecutor` yapar (CLAUDE.md değişmez #1).</summary>
    public static class EconomyTick
    {
        /// <summary>Bir haftayı hesaplar ve yazmalarını journal'a kuyruklar. Dönen `WeekLedger`
        /// yalnız RAPORdur (kasa hareketi journal'dadır).</summary>
        public static WeekLedger Hafta(GameState st, EconomyBalance eco, WorldRules kural,
                                       ulong saveSeed, WeekResult sonuc, bool evMaci, WorldJournal j)
        {
            if (st == null) throw new ArgumentNullException(nameof(st));
            if (eco == null) throw new ArgumentNullException(nameof(eco));
            if (j == null) throw new ArgumentNullException(nameof(j));

            var L = new WeekLedger();
            uint hafta = (uint)(st.Takvim.Sezon * 1000 + st.Takvim.Hafta);   // sezon+hafta = tekil tick adresi

            // ---------- GELİR: MAÇ GÜNÜ (yalnız ev maçında) ----------
            if (evMaci)
            {
                int seyirci = Seyirci(st, eco, saveSeed, hafta);
                L.Seyirci = seyirci;
                L.BiletTl = BiletGeliri(st, eco, seyirci);
                L.BufeTl = KisiBasiGelir(seyirci, eco.macGunu.bufeHarcamaTaban,
                                         OrtalamaKurus(st.Fiyat.BufeKurus) / 100.0,
                                         eco.macGunu.bufeReferansFiyat, eco.macGunu.bufeElastikiyet);
                L.MagazaTl = KisiBasiGelir(seyirci, eco.macGunu.magazaHarcamaTaban,
                                           OrtalamaKurus(st.Fiyat.MagazaKurus) / 100.0,
                                           eco.macGunu.magazaReferansFiyat, eco.macGunu.magazaElastikiyet);
            }

            // ---------- GELİR: SEZON BAŞI KOMBİNE (1. hafta, peşin) ----------
            if (st.Takvim.Hafta == 1) L.KombineTl = KombineGeliri(st, eco);

            // ---------- GELİR: DÜZENLİ ----------
            L.YayinTl = eco.gelir.yayinHaftalik;
            L.SponsorTl = st.Club.SponsorHaftalikTl > 0 ? st.Club.SponsorHaftalikTl : eco.gelir.sponsorHaftalikTaban;
            if (sonuc == WeekResult.Galibiyet) L.PrimTl = eco.gelir.galibiyetPrimi;
            else if (sonuc == WeekResult.Beraberlik) L.PrimTl = eco.gelir.beraberlikPrimi;

            // ---------- GİDER ----------
            L.MaasTl = st.Club.HaftalikMaasGiderTl;
            L.BakimTl = BakimGideri(st, eco);
            L.PersonelTl = eco.gider.personelHaftalik;
            L.IsletmeTl = eco.gider.genelIsletmeHaftalik;

            // ---------- KREDİ: faiz (sink) + anapara (bilanço aktarımı) ----------
            KrediIsle(st, eco, j, ref L);

            // ---------- İNŞAAT İLERLEMESİ ----------
            InsaatIsle(st, eco, j);

            // ---------- KASA + TAKVİM ----------
            j.KasaDelta(L.NetTl);
            if (L.ToplamGelir != 0)
                j.Emit(new WorldEvent(WorldEventType.KasaDegisti, 0, L.NetTl, st.Takvim.Sezon, st.Takvim.Hafta));

            // Form: sonuç formu iter (seyirci modelinin girdisi). Bant 0-100.
            int form = st.Club.Form
                       + (sonuc == WeekResult.Galibiyet ? 6 : sonuc == WeekResult.Beraberlik ? 1 : sonuc == WeekResult.Maglubiyet ? -6 : 0);
            if (form < 0) form = 0; if (form > 100) form = 100;
            if (form != st.Club.Form) j.Set(MutTarget.Kulup, 0, ClubField.Form, form);

            // Hafta ilerler; sezon sonunda 1'e döner ve sezon artar.
            int yeniHafta = st.Takvim.Hafta + 1;
            if (yeniHafta > kural.yapi.sezonHaftaSayisi)
            {
                j.Set(MutTarget.Takvim, 0, CalendarField.Hafta, 1);
                j.Set(MutTarget.Takvim, 0, CalendarField.Sezon, st.Takvim.Sezon + 1);
            }
            else j.Set(MutTarget.Takvim, 0, CalendarField.Hafta, yeniHafta);
            j.Emit(new WorldEvent(WorldEventType.HaftaIlerledi, 0, L.Seyirci, st.Takvim.Sezon, st.Takvim.Hafta));

            return L;
        }

        /// <summary>Seyirci sayısı — GDD 4.2: "kapasite × doluluk; doluluk takım başarısına ve
        /// bilet fiyatına duyarlıdır". Tesis tier'ı da hafifçe etkiler (GDD 4.1 konfor).</summary>
        public static int Seyirci(GameState st, EconomyBalance eco, ulong saveSeed, uint hafta)
        {
            double fiyatOrani = OrtalamaBiletOrani(st, eco);
            double d = eco.seyirci.tabanDoluluk
                       * (1.0 - eco.seyirci.fiyatElastikiyet * (fiyatOrani - 1.0))
                       * (1.0 + eco.seyirci.formEtkisi * ((st.Club.Form / 100.0) - 0.5) * 2.0)
                       * (1.0 + eco.seyirci.tesisEtkisi * (OrtalamaTesisTier(st) - 1.0));
            // Varyans: sayaç-RNG (ME 3.1) — save seed + hafta adresi. Simetrik üniform:
            // sd = genlik/√3 olduğu için genlik = sigma·√3 (yukarıdaki `Gauss01` notu).
            const double Kok3 = 1.7320508075688772;
            d += (Rng.Rand01(saveSeed, Domain.Crowd, 0, hafta, 1) * 2.0 - 1.0)
                 * eco.seyirci.varyansSigma * Kok3;
            if (d < eco.seyirci.minDoluluk) d = eco.seyirci.minDoluluk;
            if (d > 1.0) d = 1.0;
            return (int)Math.Round(st.Club.StadyumKapasite * d, MidpointRounding.AwayFromZero);
        }

        /// <summary>Bilet geliri — tribün başına kapasite payı × o tribünün fiyatı.</summary>
        static long BiletGeliri(GameState st, EconomyBalance eco, int seyirci)
        {
            long toplam = 0;
            for (int t = 0; t < 5; t++)
            {
                double pay = eco.tribun.kapasitePayiBinde[t] / 1000.0;
                double kisi = seyirci * pay;
                toplam += (long)Math.Round(kisi * (st.Fiyat.BiletKurus[t] / 100.0), MidpointRounding.AwayFromZero);
            }
            return toplam;
        }

        /// <summary>Kombine geliri — sezon başında peşin. Kapasitenin bir oranı kadar kombine
        /// satılır; talep fiyata duyarlıdır.</summary>
        static long KombineGeliri(GameState st, EconomyBalance eco)
        {
            double fiyat = st.Fiyat.KombineKurus / 100.0;
            if (fiyat <= 0) return 0;
            double oran = fiyat / eco.kombine.referansFiyat;
            double talep = eco.kombine.kapasiteOrani * (1.0 - eco.kombine.elastikiyet * (oran - 1.0));
            if (talep < 0) talep = 0;
            if (talep > 1) talep = 1;
            return (long)Math.Round(st.Club.StadyumKapasite * talep * fiyat, MidpointRounding.AwayFromZero);
        }

        /// <summary>Kişi başı harcama kalemleri (büfe, mağaza) — fiyat arttıkça kişi başı ADET
        /// düşer ama ciro tepe noktasına kadar artar; klasik elastikiyet.</summary>
        static long KisiBasiGelir(int seyirci, double taban, double fiyat, double referans, double elastikiyet)
        {
            if (fiyat <= 0 || referans <= 0) return 0;
            double oran = fiyat / referans;
            double harcama = taban * oran * (1.0 - elastikiyet * (oran - 1.0));
            if (harcama < 0) harcama = 0;
            return (long)Math.Round(seyirci * harcama, MidpointRounding.AwayFromZero);
        }

        static long BakimGideri(GameState st, EconomyBalance eco)
        {
            long tierToplam = 0;
            for (int i = 0; i < st.Club.TesisTier.Length; i++) tierToplam += st.Club.TesisTier[i];
            return tierToplam * eco.gider.tesisBakimHaftalikTierBasi;
        }

        /// <summary>Kredi: FAİZ sink'tir, ANAPARA bilanço aktarımıdır (WeekLedger notu).
        /// Taksit ayda bir (4 haftada bir) işler.</summary>
        static void KrediIsle(GameState st, EconomyBalance eco, WorldJournal j, ref WeekLedger L)
        {
            if (st.Takvim.Hafta % 4 != 0) return;
            for (int i = 0; i < st.Club.Krediler.Length; i++)
            {
                var k = st.Club.Krediler[i];
                if (k.KrediId == 0 || k.AnaparaTl <= 0) continue;
                long faiz = (long)Math.Round(k.AnaparaTl * (k.FaizBp / 10000.0) / 12.0, MidpointRounding.AwayFromZero);
                long anapara = k.KalanAy > 0
                    ? (long)Math.Round(k.AnaparaTl / (double)k.KalanAy / eco.kredi.aylikTaksitBolen, MidpointRounding.AwayFromZero)
                    : k.AnaparaTl;
                if (anapara > k.AnaparaTl) anapara = k.AnaparaTl;
                L.FaizTl += faiz;
                L.AnaparaOdemeTl += anapara;
                j.Set(MutTarget.Kredi, i, LoanField.Anapara, k.AnaparaTl - anapara);
                int kalan = k.KalanAy > 0 ? k.KalanAy - 1 : 0;
                j.Set(MutTarget.Kredi, i, LoanField.KalanAy, kalan);
                if (k.AnaparaTl - anapara <= 0)
                {
                    j.Set(MutTarget.Kredi, i, LoanField.KrediId, 0);   // slot boşalır
                    j.Emit(new WorldEvent(WorldEventType.KrediOdendi, k.KrediId, k.AnaparaTl, st.Takvim.Sezon, st.Takvim.Hafta));
                }
            }
        }

        /// <summary>İnşaat ilerlemesi — bitince tesis tier'ı yükselir ve stadyum kapasitesi
        /// (stadyum tesisiyse) yeni tier'a çıkar. Maliyet BAŞLANGIÇTA tahsil edilir (K3-B).</summary>
        static void InsaatIsle(GameState st, EconomyBalance eco, WorldJournal j)
        {
            for (int i = 0; i < st.Club.InsaatSlot.Length; i++)
            {
                var c = st.Club.InsaatSlot[i];
                if (c.InsaatId == 0) continue;
                if (c.KalanHafta > 1) { j.Set(MutTarget.Insaat, i, ConstructionField.KalanHafta, c.KalanHafta - 1); continue; }
                // BİTTİ
                j.Set(MutTarget.Tesis, c.TesisId, FacilityField.Tier, c.HedefTier);
                if (c.TesisId == StadyumTesisId && c.HedefTier < eco.insaat.kapasiteTier.Length)
                    j.Set(MutTarget.Kulup, 0, ClubField.StadyumKapasite, eco.insaat.kapasiteTier[c.HedefTier]);
                j.Set(MutTarget.Insaat, i, ConstructionField.InsaatId, 0);
                j.Set(MutTarget.Insaat, i, ConstructionField.KalanHafta, 0);
                j.Set(MutTarget.Insaat, i, ConstructionField.TesisId, 0);
                j.Set(MutTarget.Insaat, i, ConstructionField.HedefTier, 0);
                j.Set(MutTarget.Insaat, i, ConstructionField.ToplamMaliyet, 0);
            }
        }

        /// <summary>Stadyumun tesis kimliği — kapasite progression'ı (GDD 4.1) bu tesise bağlıdır.</summary>
        public const int StadyumTesisId = 1;

        static double OrtalamaBiletOrani(GameState st, EconomyBalance eco)
        {
            double agirlikliFiyat = 0, agirlikliRef = 0;
            for (int t = 0; t < 5; t++)
            {
                double pay = eco.tribun.kapasitePayiBinde[t] / 1000.0;
                agirlikliFiyat += pay * (st.Fiyat.BiletKurus[t] / 100.0);
                agirlikliRef += pay * eco.tribun.referansFiyat[t];
            }
            return agirlikliRef <= 0 ? 1.0 : agirlikliFiyat / agirlikliRef;
        }

        static double OrtalamaTesisTier(GameState st)
        {
            long toplam = 0; int n = 0;
            for (int i = 1; i < st.Club.TesisTier.Length; i++) { toplam += st.Club.TesisTier[i]; n++; }
            return n == 0 ? 1.0 : (double)toplam / n;
        }

        static double OrtalamaKurus(int[] a)
        {
            if (a == null || a.Length == 0) return 0;
            long t = 0;
            for (int i = 0; i < a.Length; i++) t += a[i];
            return (double)t / a.Length;
        }
    }
}
