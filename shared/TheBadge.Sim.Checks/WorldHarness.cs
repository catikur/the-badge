using System;
using System.Collections.Generic;
using TheBadge.CommandBus;
using TheBadge.Sim.Commands;
using TheBadge.World;

namespace TheBadge.Checks
{
    /// <summary>Denetim + olay toplayıcısı — audit'in YÜRÜTME içinde geldiğini ve hash'lerin
    /// dolduğunu kanıtlar (CB 5.2 / 9.1).</summary>
    public sealed class CollectingAuditSink : IWorldAuditSink
    {
        public readonly List<WorldAuditEntry> Kayitlar = new List<WorldAuditEntry>();
        public readonly List<WorldEvent> Olaylar = new List<WorldEvent>();
        public void Persist(WorldAuditEntry entry, IReadOnlyList<WorldEvent> events)
        {
            Kayitlar.Add(entry);
            for (int i = 0; i < events.Count; i++) Olaylar.Add(events[i]);
        }
        public void Clear() { Kayitlar.Clear(); Olaylar.Clear(); }
    }

    /// <summary>Test handler'ı — K3-K5 gelene kadar journal mekanizmasını sınar. Davranışı
    /// alanlarla ayarlanır: kasa deltası, oyuncu yazması, kasıtlı red, kasıtlı GEÇERSİZ yazma.</summary>
    public sealed class TestHandler : IActionHandler
    {
        public long KasaDelta;
        public RejectionReason Result = RejectionReason.None;
        public bool GecersizYazma;          // aralık dışı yazma üretir (handler hatası taklidi)
        public int OyuncuIndex = -1;
        public byte OyuncuAlan = PlayerField.Moral;
        public long OyuncuDeger;
        public WorldEventType Olay = WorldEventType.None;
        public Action<WorldJournal> Ozel;   // serbest journal kurgusu (zincirleme yazma sınamaları)
        int cagri;
        public int Cagrilar => System.Threading.Volatile.Read(ref cagri);

        public RejectionReason Apply(GameState st, WorldJournal journal, CommandEnvelope env,
                                     ActionDef action, IPayloadView payload, out string detail)
        {
            System.Threading.Interlocked.Increment(ref cagri);
            detail = null;
            if (Result != RejectionReason.None) { detail = "test reddi"; return Result; }
            if (KasaDelta != 0) journal.KasaDelta(KasaDelta);
            if (OyuncuIndex >= 0) journal.OyuncuSet(OyuncuIndex, OyuncuAlan, OyuncuDeger);
            if (GecersizYazma) journal.Set(MutTarget.Oyuncu, 0, PlayerField.Moral, 300);  // 0-100 dışı
            if (Olay != WorldEventType.None)
                journal.Emit(new WorldEvent(Olay, 0, KasaDelta, st.Takvim.Sezon, st.Takvim.Hafta));
            Ozel?.Invoke(journal);
            return RejectionReason.None;
        }
    }

    /// <summary>Aksiyona özgü Kapı 3 kuralı sahtesi — K3-K5 seaminin sınandığı yer.</summary>
    public sealed class TestRule : IActionRule
    {
        public RejectionReason Sonuc = RejectionReason.None;
        public int Cagrilar;
        public RejectionReason Check(GameState st, CommandEnvelope env, ActionDef action, IPayloadView payload, out string detail)
        { Cagrilar++; detail = Sonuc == RejectionReason.None ? null : "test kuralı"; return Sonuc; }
    }

    /// <summary>Kapı 3 yarışını DETERMİNİSTİK yapan sarmalayıcı. Yarışı şansa bırakan bir test,
    /// hata varken de yeşil kalabilir — nitekim ilk sürümü öyle oldu: tekrar denetimi kaldırıldığı
    /// hâlde kapı yeşil kaldı. Burada bariyer, TÜM iş parçacıkları Kapı 3'ü geçene kadar hiçbirinin
    /// yürütmeye geçmemesini garantiler; yani "doğrula-sonra-yürüt" penceresi her koşuda açılır.
    ///
    /// Bariyer YALNIZ iş parçacığının İLK çağrısında (bus doğrulaması) beklenir; yürütücünün
    /// kilit altındaki TEKRAR denetimi (ikinci çağrı) geçer — yoksa kilitlenirdi.</summary>
    public sealed class BarrierContext : IValidationContext
    {
        readonly IValidationContext ic;
        readonly System.Threading.Barrier bariyer;
        [ThreadStatic] static int derinlik;
        public BarrierContext(IValidationContext inner, int katilimci)
        { ic = inner; bariyer = new System.Threading.Barrier(katilimci); }
        public bool IsContextActive(Context context) => ic.IsContextActive(context);
        public long ResolveTeamKey(CommandEnvelope env) => ic.ResolveTeamKey(env);
        public RejectionReason CheckOwnershipAndState(CommandEnvelope env, ActionDef action, IPayloadView payload, out string detail)
        {
            var r = ic.CheckOwnershipAndState(env, action, payload, out detail);
            if (derinlik++ == 0) bariyer.SignalAndWait();   // durum kilidi BURADA tutulmuyor
            return r;
        }
    }

    /// <summary>Fırlatan denetim sinki — CB 5.2'nin bellek ayağını sınar: audit yazımı
    /// başarısızsa durum İLERLEMEMİŞ olmalı.</summary>
    public sealed class ThrowingAuditSink : IWorldAuditSink
    {
        public int Cagrilar;
        public void Persist(WorldAuditEntry entry, IReadOnlyList<WorldEvent> events)
        { Cagrilar++; throw new InvalidOperationException("denetim deposu erişilemez"); }
    }

    /// <summary>K3 REFERANS KULÜP — ekonomi sözleşmesinin (ECONOMY_MAP) ölçüldüğü senaryo.
    /// "İyi yönetilen orta ölçekli kulüp": tier 3 stadyum (30.000), 22 kişilik kadro, tesisler
    /// makul seviyede, fiyatlar referans bandında. Katsayılar bu kulübü 1,05-1,15 bandında
    /// TUTMALIDIR — fixture sabit, kalibre edilen `economy.balance.json`tır.</summary>
    public static class EkonomiFixture
    {
        public const int Kapasite = 30000;
        public const int KadroSayisi = 22;
        public const long OyuncuHaftalikMaas = 70_700;

        public static GameState Kur(WorldRules rules, EconomyBalance eco, long clubId, long ownerUserId)
        {
            var st = WorldFixture.Kur(rules, clubId, ownerUserId, KadroSayisi, 2, 2, 20_000_000);
            st.Club.StadyumKapasite = Kapasite;
            st.Club.Form = 50;
            // Tesisler: stadyum tier 3 + dört tesis tier 2 (bakım gideri tier toplamına bağlı)
            st.Club.TesisTier[EconomyTick.StadyumTesisId] = 3;
            for (int i = 2; i <= 5; i++) st.Club.TesisTier[i] = 2;
            // Maaş gideri kadroyla tutarlı
            for (int i = 0; i < st.Oyuncular.Length; i++)
                if (st.Oyuncular[i].ClubId == clubId) st.Oyuncular[i].HaftalikMaasTl = OyuncuHaftalikMaas;
            st.Club.HaftalikMaasGiderTl = KadroSayisi * OyuncuHaftalikMaas;
            // K5 değerleme girdileri: güç/potansiyel/yaş DETERMİNİSTİK dağıtılır (indeksten
            // türetilir, RNG yok) — böylece transfer kapıları sabit bir kadro üzerinde ölçer.
            for (int i = 0; i < st.Oyuncular.Length; i++)
            {
                st.Oyuncular[i].Guc = (byte)(45 + (i * 7) % 45);          // 45-89
                st.Oyuncular[i].Potansiyel = (byte)System.Math.Min(99, st.Oyuncular[i].Guc + (i * 3) % 12);
                st.Oyuncular[i].Yas = (byte)(19 + (i * 5) % 17);          // 19-35
                st.Oyuncular[i].SozlesmeKalanHafta = (ushort)(26 + (i * 11) % 130);
            }
            // Fiyatlar referans seviyesinde (kuruş)
            for (int t = 0; t < 5; t++) st.Fiyat.BiletKurus[t] = eco.tribun.referansFiyat[t] * 100;
            st.Fiyat.KombineKurus = (int)(eco.kombine.referansFiyat * 100);
            for (int i = 0; i < 3; i++) st.Fiyat.BufeKurus[i] = (int)(eco.macGunu.bufeReferansFiyat * 100);
            for (int i = 0; i < 3; i++) st.Fiyat.MagazaKurus[i] = (int)(eco.macGunu.magazaReferansFiyat * 100);
            return st;
        }
    }

    /// <summary>Sezon simülatörü — haftalık tick'i TEK KAPI'dan geçirerek koşturur (doğrudan
    /// durum mutasyonu yok). Sonuç dizisi ECONOMY_MAP sözleşmesinin ölçüldüğü veridir.</summary>
    public static class EkonomiKosu
    {
        /// <summary>`sezon` sezon boyunca haftalık tick. Maç sonuçları DETERMİNİSTİK bir
        /// örüntüden gelir (rastgelelik ekonomiyi değil, ölçümü bulanıklaştırırdı): sırayla
        /// G-B-M-G-B-M... → %33 galibiyet, %33 beraberlik, %33 mağlubiyet.</summary>
        public static WeekLedger Kos(GameState st, EconomyBalance eco, WorldRules kural,
                                     ulong saveSeed, int sezon, TransferBalance tb, out int iflasSezonu)
        {
            var toplam = new WeekLedger();
            var j = new WorldJournal();
            iflasSezonu = -1;
            int hafta = 0;
            for (int s = 0; s < sezon; s++)
            {
                for (int h = 0; h < kural.yapi.sezonHaftaSayisi; h++, hafta++)
                {
                    var sonuc = (WeekResult)(byte)(1 + (hafta % 3));      // G, B, M döngüsü
                    bool evMaci = (hafta % 2) == 0;
                    j.Clear();
                    var L = EconomyTick.Hafta(st, eco, kural, saveSeed, sonuc, evMaci, tb, j);
                    if (!j.Validate(st, out string hata))
                        throw new InvalidOperationException("ekonomi journal geçersiz: " + hata);
                    j.Apply(st);
                    toplam.Topla(L);
                    if (iflasSezonu < 0 && st.Club.KasaTl <= eco.iflas.esikTl) iflasSezonu = s + 1;
                }
            }
            return toplam;
        }
    }

    /// <summary>KADEMELİ İNŞAAT KOŞUSU — capex sözleşmesinin ölçüldüğü senaryo (K10-D).
    ///
    /// NEDEN AYRI BİR KOŞU: `EkonomiKosu.Kos` hiç inşaat YAPMAZ, dolayısıyla `InsaatTl` sıfırdır
    /// ve `K3EkonomiSozlesmesi`nin ölçtüğü 1,05-1,15 bandı yalnız İŞLETME dengesidir. ECONOMY_MAP
    /// "İnşaat + tesis bakımı"nı sink sayıyor; capex ölçülmediği sürece o satır kapısız kalıyordu.
    ///
    /// POLİTİKA PARAMETRESİZDİR — bilerek. Bir "kasa rezervi" eşiği koysaydım, kapının verdiği
    /// cevabı o eşiği oynatarak istediğim yere getirebilirdim; ölçtüğünü değil ayarını raporlayan
    /// bir kapı olurdu. Burada kural tek cümledir: SLOT BOŞSA VE PARA YETİYORSA YAPILIR. Bu,
    /// herhangi bir kulübün fiziksel olarak inşa edebileceği EN HIZLI yoldur; merdiven süresi de
    /// bu yüzden bir ALT SINIRdır (gerçek oyuncu daha yavaş gider, daha hızlı gidemez).
    ///
    /// KREDİ YOK: merdiven işletme fazlasından finanse edilir. Kredi açılsaydı süre kredinin
    /// vade/faiz ayarına bağlanırdı ve capex sorusu kredi sorusuna dönerdi.
    ///
    /// TEK KAPI: inşaat komutu doğrudan uygulanmaz, Command Bus'tan geçer (CLAUDE.md 1).</summary>
    public static class KademeliInsaatKosu
    {
        /// <summary>Referans kulübün KENDİ tesis seti — `EkonomiFixture` tam da bunları kuruyor
        /// (1 = stadyum tier 3, 2-5 tier 2). Merdiven "64 tesisin hepsi" DEĞİLDİR: o, derinlik
        /// yerine genişliği ölçerdi ve referans kulübün kimliğiyle ilgisi olmazdı.</summary>
        public static readonly int[] ReferansTesisler = { 1, 2, 3, 4, 5 };

        /// <summary>Maaş giderinin haftalık gelire oranı tavanı — ECONOMY_MAP "maaş sink'i toplam
        /// sink'in %45-60'ı" kuralının politika karşılığı. Üst uçtan biraz aşağıda tutulur ki
        /// politika bandın kendisini zorlamasın.</summary>
        public const double MaasPayiTavani = 0.55;

        /// <summary>Kadro dönüşü için asgari güç farkı. Sıfır olsaydı kulüp bir puanlık fark için
        /// her hafta oyuncu feshedip alır, sink'i politikanın gürültüsü doldururdu.</summary>
        public const int GucFarkiEsigi = 6;

        /// <summary>Tavan tier — `eco.insaat.tierSureHafta` uzunluğundan türetilir, elle yazılmaz.</summary>
        public static int MaxTier(EconomyBalance eco) => eco.insaat.tierSureHafta.Length - 1;

        /// <summary>Referans kulübün NAKİT REZERVİ, hafta cinsinden işletme gideri. Politikanın
        /// bir parçası (balance değil): burası oyun verisi değil, ölçülen SENARYOdur. Bir sezonluk
        /// işletme tamponu — kredisiz politikanın makul karşılığı.</summary>
        /// <summary>Referans kulübün NAKİT REZERVİ, hafta cinsinden işletme gideri. Politikanın
        /// parçası (balance DEĞİL): burası oyun verisi değil, ölçülen SENARYOdur.
        ///
        /// NEDEN VAR: ilk politika "para yetiyorsa yap"tı ve kasayı dibe vuruyordu (ölçüm: en
        /// düşük kasa −5,8M₺ — referans kulüp düzenli olarak eksideydi). Rezervle +20,0M₺.
        /// Ölçüme etkisi de var ve gizlenmiyor: rezervsiz politika kaçınılmaz olarak PARA
        /// SINIRLIdır, para sınırlı kulüpte gelirin tamamı capex'e gider ve inşaat penceresi
        /// source/sink 1,00'a çakılır. 22 kalibrasyon noktasında (doygunluk × ücret × tier
        /// maliyeti) pencere hiç 1,05'e ulaşmadı; ECONOMY_MAP bandının orada tutması bakiyenin
        /// değil POLİTİKANIN işiymiş.
        ///
        /// 28 HAFTA NEREDEN: rezerv × tier maliyeti ızgarası süpürüldü (12/20/28/38 × 1,00/0,85/0,70).
        /// Dört şartı (pencere ∈ bant, durağan ∈ bant, merdiven ∈ [6,24], fakir kulüp de ∈ [6,24])
        /// aynı anda sağlayan TEK nokta 28 hafta + tier ×0,70 çıktı.</summary>
        public const int RezervHafta = 28;

        /// <summary>HEDEFİN SAHİPLİK DURUMUNA GÖRE DOĞRU AKSİYON — serbest oyuncu (`ClubId == 0`)
        /// için `sign_free_agent`, sahipli oyuncu için `propose_offer`. Ayrı bir metot çünkü
        /// KAPI bunu ölçüyor: koşucu ayrımı yapmadığında serbest hedefe her hafta `propose_offer`
        /// gidiyor, bus `NotOwned` ile sessizce reddediyor ve transfer sink'i KİLİTLENİYOR
        /// (inceleme bulgusu, P1). Çağrı yerinde gömülü bir üçlü ifade ölçülemezdi.</summary>
        public static string TransferAksiyonu(long hedefClubId)
            => hedefClubId == 0 ? "transfer.sign_free_agent" : "transfer.propose_offer";

        public struct Sonuc
        {
            public WeekLedger Toplam;          // TÜM koşunun toplamı
            public WeekLedger MerdivenToplam;  // YALNIZ merdiven penceresi (1. sezon → tamamlanma)
            public int BaslatilanInsaat;
            public int MerdivenSezon;          // merdivenin tamamlandığı sezon; -1 = tamamlanmadı
            public int IflasSezonu;            // -1 = iflas yok
            public long MinKasaTl;             // koşu boyunca görülen en düşük kasa
            public int BeklenmeyenRed;         // kapı reddi (InsufficientFunds/StateConflict dışı)
            /// <summary>SEZON SEZON ledger. Capex YUMRULUdur: tek bir tier adımı bir sezonun
            /// gelirinin %10-40'ı kadar olabilir, ertesi sezon sıfır. Bu yüzden ECONOMY_MAP
            /// oranı sezon sezon okunduğunda savrulur; kapı PENCERE ORTALAMASINI ölçer ve bu
            /// liste savrulmanın rapor edilmesini sağlar (ortalamayı "her sezon böyle" diye
            /// okumak, kapının iddiasını ölçtüğünden geniş yapardı).</summary>
            public List<WeekLedger> Sezonlar;
            public int PiyasayaGiren;          // havuza katılan oyuncu (K12-C)
            public int Transfer;               // tamamlanan alım (K12-C)
            /// <summary>Süresi dolmuş yuvaya rağmen politikanın DEVAM ettiği hafta sayısı.
            /// Sıfırsa ya senaryoda hiç teklif süresi dolmuyor (kapı boşa koşuyor) ya da
            /// donma geri gelmiştir — ikisi de kapının bilmesi gereken bir şey.</summary>
            public int OluYuvaGecildi;
            /// <summary>Politikanın açık teklif yüzünden ÜST ÜSTE durduğu en uzun hafta serisi.
            /// Sağlıklı hâlde `teklifGecerlilikHafta` mertebesindedir; donmuş politikada koşu
            /// boyu büyür (ölçüldü: 494 hafta).</summary>
            public int EnUzunEngelliSeri;
            public int Fesih;                  // kadro dönüşü için fesih (K12-C)
        }

        public static Sonuc Kos(GameState st, EconomyBalance eco, WorldRules kural, ulong saveSeed,
                                int sezon, IBandProvider bantlar,
                                Dictionary<RateClass, RateLimitCfg[]> rlCfg, long ownerUserId,
                                // `tb` ZORUNLU: ekonomi tick'i sezon başı ücret gözden geçirmesi
                                // için istiyor (K13-A). Varsayılan bırakmak, piyasasız koşuların
                                // enflasyonu sessizce atlaması demekti.
                                TransferBalance tb, MarketBalance mb = null)
        {
            var depo = new WorldStore(st);
            var ctx = new WorldContext(depo, kural);
            var exec = new WorldExecutor(depo, ctx);
            TycoonActions.Baglan(ctx, exec, eco);
            // PİYASA AÇIKSA transfer aksiyonları da bağlanır ve pencere AÇILIR — `propose_offer`
            // ve `respond_offer` transfer penceresi ister (`world.balance.pencereGerektiren`).
            bool piyasa = mb != null && tb != null;
            if (piyasa)
            {
                TransferActions.Baglan(ctx, exec, kural, tb, saveSeed);
                st.Takvim.Pencere = TransferWindow.Yaz;
            }
            var bus = new CommandBus.CommandBus(bantlar, ctx,
                new SlidingWindowRateLimiter(rlCfg, 8, 300_000), new IdempotencyStore());

            var r = new Sonuc { MerdivenSezon = -1, IflasSezonu = -1, MinKasaTl = st.Club.KasaTl,
                                Sezonlar = new List<WeekLedger>(sezon) };
            int maxTier = MaxTier(eco);
            var j = new WorldJournal();
            int hafta = 0, engelliSeri = 0;
            // Host saati HAFTAYLA ilerler: aynı ana yığılan komutlar rate limiter'a takılırdı ve
            // kapı ekonomiyi değil hız sınırını ölçerdi.
            const long HaftaMs = 7L * 24 * 60 * 60 * 1000;
            long host = 1_700_000_000_000L;

            for (int s = 0; s < sezon; s++)
            {
                // SEZON BAŞI HAVUZ GİRİŞİ. Journal'dan GEÇMEZ: dizi büyütmek yapısal bir dünya
                // olayıdır, `WorldJournal` alan yazması için var. `EconomyTick`in journal
                // kullanması onu bir OYUNCU EYLEMİ yapmaz — Tek Kapı komutlar içindir.
                if (piyasa) r.PiyasayaGiren += TransferMarket.SezonBasiGiris(st, kural, mb, tb, saveSeed, s + 1);
                var sezonL = new WeekLedger();
                for (int h = 0; h < kural.yapi.sezonHaftaSayisi; h++, hafta++)
                {
                    // ---- 1) İNŞAAT KARARI (Tek Kapı) ----
                    for (int t = 0; t < ReferansTesisler.Length; t++)
                    {
                        int tesis = ReferansTesisler[t];
                        if (st.FreeConstructionSlot() < 0) break;          // boş slot yok
                        if (InsaattaMi(st, tesis)) continue;               // zaten yapılıyor
                        int mevcut = st.Club.TesisTier[tesis];
                        if (mevcut >= maxTier) continue;                   // tavan
                        long maliyet = eco.TierMaliyet(mevcut + 1);
                        // NAKİT REZERVİ: kulüp kasayı SIFIRA kadar harcamaz. İlk politika
                        // "para yetiyorsa yap"tı ve referans kulüp her tier'da kasayı dibe
                        // vuruyordu (ölçüldü: en düşük kasa −5,8M₺, yani düzenli olarak eksiye
                        // düşüyordu). Bu bir referans kulüp davranışı değil; gerçek kulüp
                        // işletme gideri için tampon tutar.
                        //
                        // ÖLÇÜM SONUCU DA BUNA BAĞLI: rezervsiz politika kaçınılmaz olarak
                        // PARA SINIRLIdır ve para sınırlı bir kulüpte source/sink inşaat
                        // penceresi boyunca 1,00'a çakılır — gelirin tamamı capex'e gider.
                        // 22 kalibrasyon noktası ölçüldü (doygunluk × ücret × tier maliyeti);
                        // pencere oranı hiçbirinde 1,05'e ulaşmadı. Yani ECONOMY_MAP bandının
                        // inşaat penceresinde tutması, bakiyenin değil POLİTİKANIN işiydi.
                        long rezerv = RezervHafta * (eco.gider.personelHaftalik + eco.gider.genelIsletmeHaftalik
                                                     + st.Club.HaftalikMaasGiderTl);
                        if (!st.CanAfford(maliyet + rezerv)) continue;      // para yok → BEKLE (kredi yok)

                        var zarf = new CommandEnvelope
                        {
                            CommandId = KomutId(hafta, tesis), CatalogVersion = Catalog.Version,
                            Source = CommandSource.UI, ActionType = "tycoon.start_construction",
                            IssuedAtUnixMs = host, MatchTick = 0, UserId = ownerUserId,
                            SaveSlotId = 1, TeamIdx = 0, PayloadJson = new byte[0]
                        };
                        var yuk = new TestPayload().Set("tesisId", (double)tesis)
                                                   .Set("hedefTier", (double)(mevcut + 1));
                        var o = bus.Submit(zarf, yuk, exec, host, ownerUserId);
                        if (o.Ok) r.BaslatilanInsaat++;
                        else if (o.Reason != RejectionReason.InsufficientFunds
                                 && o.Reason != RejectionReason.StateConflict) r.BeklenmeyenRed++;
                    }

                    // ---- 1b) TRANSFER KARARI (Tek Kapı) + KARŞI TARAF SÜRÜCÜSÜ ----
                    // ÖNCELİK: ÖNCE MERDİVEN, SONRA KADRO. İki sink tek fazla için yarışıyor ve
                    // önceliksiz politika ikisini de yarım bırakıyordu (ölçüm: 6 alım fazlanın
                    // tamamını kalıcı maaş yükü olarak yedi, merdiven hiç bitmedi, kasa −13M).
                    // Sıralama ekonomik gerçekten geliyor: CAPEX SONLUdur ve geliri KALICI olarak
                    // büyütür; transfer SONSUZ bir sink'tir ve geliri büyütmez. Tersini yapan
                    // kulüp kendi büyüme motorunu kapatır.
                    if (piyasa && MerdivenBitti(st, eco))
                    {
                        // Açık teklifimiz yoksa ve para varsa, kadroyu güçlendirecek EN İYİ
                        // hedefe teklif. Bütçe = kasa (kredisiz politika, capex ile aynı ilke).
                        // AÇIK TEKLİF = CANLI TEKLİF. Süresi DOLMUŞ bir yuva politikayı bloke
                        // ETMEZ: `propose_offer` ölü yuvayı zaten geri kazanır (K5), oysa ilk
                        // yazımda `TeklifId != 0` yeterliydi ve `TransferTick` süresi dolmuş
                        // teklife BİLEREK dokunmadığı için yuva sonsuza dek dolu kalıyordu.
                        // Sonuç: ilk teklif süresi dolduktan sonra kulüp bir daha ne teklif
                        // veriyor, ne fesih yapıyor, ne kabul ediyordu — politika DONUYORDU.
                        // ÖLÇÜM: 13 sezonluk piyasalı koşuda açık teklifli 508 haftanın 494'ü
                        // tam olarak bu durumdaydı (inceleme bulgusu, Bugbot). Yani K12-C'nin
                        // "merdiven sonrası 1,911" ölçümü DONMUŞ bir transfer politikasını
                        // ölçüyordu. (Bugbot bulgusu, orta şiddet — ölçüldü, gerçek çıktı.)
                        bool acikTeklif = false, oluYuva = false;
                        for (int t = 0; t < st.Club.TransferTeklifleri.Length; t++)
                        {
                            if (st.Club.TransferTeklifleri[t].TeklifId == 0) continue;
                            if (TransferActions.SureDoldu(st, st.Club.TransferTeklifleri[t])) oluYuva = true;
                            else acikTeklif = true;
                        }
                        if (!acikTeklif && oluYuva) r.OluYuvaGecildi++;
                        // ENGELLİ SERİ: politika açık teklif yüzünden kaç HAFTA ÜST ÜSTE durdu?
                        // Sağlıklı hâlde bu, teklifin ömrüyle sınırlıdır; donduğunda sınırsız
                        // büyür. Kapının ölçtüğü sayı budur — "kaç transfer oldu" değil, çünkü
                        // transfer sayısı senaryonun zenginliğine de bağlı.
                        if (acikTeklif) { engelliSeri++; if (engelliSeri > r.EnUzunEngelliSeri) r.EnUzunEngelliSeri = engelliSeri; }
                        else engelliSeri = 0;
                        // MAAŞ BÜTÇESİ — ECONOMY_MAP'in KENDİ kuralı ("maaş sink'i toplam sink'in
                        // %45-60'ı"). İlk yazımda politika sınırsızdı: kulüp hem inşa edip hem
                        // transfer yapmaya çalışıp iflas etti, merdiven hiç bitmedi (kasa −13M).
                        // Asıl sink BEDEL değil ÜCRET: 6 alım bile kalıcı maaş yükü olarak
                        // fazlanın tamamını yiyordu. Bütçesiz bir politika, piyasayı ölçmek yerine
                        // politikanın kendi çılgınlığını ölçerdi.
                        long haftalikGelir = 0;
                        if (r.Sezonlar.Count > 0)
                            haftalikGelir = r.Sezonlar[r.Sezonlar.Count - 1].ToplamGelir / kural.yapi.sezonHaftaSayisi;
                        else haftalikGelir = r.Toplam.ToplamGelir / System.Math.Max(1, hafta);
                        long maasTavani = (long)(haftalikGelir * MaasPayiTavani);

                        // KADRO DÖNÜŞÜ: kadro TAVANDAYSA ve piyasada belirgin daha iyisi varsa
                        // en zayıfı FESHET, yer aç. Bu olmadan transfer sink'i BİR KEZLİKtir —
                        // kadro dolunca alım durur ve kasa şişer (ölçüm: merdiven sonrası oran
                        // 1,88'de kaldı, kasa 3,3 milyar). Gerçek kulüp kadroyu döndürür; fesih
                        // bedeli de ECONOMY_MAP'in transfer sink'ine yazılır.
                        if (piyasa && !acikTeklif && st.Club.KasaTl > 0
                            && st.Club.HaftalikMaasGiderTl < maasTavani)
                        {
                            int kadroSayisi = 0, enZayifIdx = -1;
                            for (int q = 0; q < st.Oyuncular.Length; q++)
                                if (st.Oyuncular[q].ClubId == st.Club.ClubId)
                                {
                                    kadroSayisi++;
                                    if (enZayifIdx < 0 || st.Oyuncular[q].Guc < st.Oyuncular[enZayifIdx].Guc)
                                        enZayifIdx = q;
                                }
                            if (kadroSayisi >= kural.yapi.kadroMax && kadroSayisi > kural.yapi.kadroMin
                                && enZayifIdx >= 0)
                            {
                                // Piyasada en zayıfımızdan BELİRGİN daha iyisi var mı (tavan geçici
                                // olarak +1 sayılarak sorulur — yer açmadan hedef görünmezdi).
                                int aday = TransferMarket.EnIyiHedef(st, st.Club.ClubId, tb, st.Club.KasaTl,
                                                                     kural.yapi.kadroMax + 1);
                                if (aday >= 0 && st.Oyuncular[aday].Guc > st.Oyuncular[enZayifIdx].Guc + GucFarkiEsigi)
                                {
                                    var zf = new CommandEnvelope
                                    {
                                        CommandId = KomutId(hafta, 700 + enZayifIdx % 90), CatalogVersion = Catalog.Version,
                                        Source = CommandSource.UI, ActionType = "transfer.release_player",
                                        IssuedAtUnixMs = host, MatchTick = 0, UserId = ownerUserId,
                                        SaveSlotId = 1, TeamIdx = 0, PayloadJson = new byte[0]
                                    };
                                    var sf = bus.Submit(zf, new TestPayload().Set("oyuncuId",
                                                 (double)st.Oyuncular[enZayifIdx].PlayerId), exec, host, ownerUserId);
                                    if (sf.Ok) r.Fesih++;
                                }
                            }
                        }

                        if (!acikTeklif && st.Club.KasaTl > 0 && st.Club.HaftalikMaasGiderTl < maasTavani)
                        {
                            int hedef = TransferMarket.EnIyiHedef(st, st.Club.ClubId, tb, st.Club.KasaTl,
                                                                  kural.yapi.kadroMax);
                            if (hedef >= 0)
                            {
                                var hp = st.Oyuncular[hedef];
                                long bedel = Valuation.PiyasaDegeri(hp, tb);
                                long maas = Valuation.MaasTalebi(hp, tb);
                                // Yeni maaş bütçeyi aşıyorsa teklif YOK — kadro tavanı gibi bu da
                                // bir yönetim kuralı, piyasanın değil.
                                if (st.Club.HaftalikMaasGiderTl + maas > maasTavani) goto transferSonu;
                                // YOL AYRIMI: serbest oyuncu `propose_offer`ı `NotOwned` ile
                                // reddeder; doğru aksiyon `sign_free_agent`tir. Ayrım yapılmazsa
                                // aynı serbest oyuncu her hafta seçilir, teklif sessizce düşer ve
                                // transfer sink'i KİLİTLENİR (inceleme bulgusu, P1).
                                bool serbestOyuncu = hp.ClubId == 0;
                                string aksiyon = TransferAksiyonu(hp.ClubId);
                                var z = new CommandEnvelope
                                {
                                    CommandId = KomutId(hafta, 900 + hedef % 90), CatalogVersion = Catalog.Version,
                                    Source = CommandSource.UI,
                                    ActionType = aksiyon,
                                    IssuedAtUnixMs = host, MatchTick = 0, UserId = ownerUserId,
                                    SaveSlotId = 1, TeamIdx = 0, PayloadJson = new byte[0]
                                };
                                var syuk = serbestOyuncu
                                    ? new TestPayload().Set("oyuncuId", (double)hp.PlayerId)
                                                       .Set("maas", (double)maas).Set("sureYil", 3L)
                                    : new TestPayload().Set("hedefOyuncuId", (double)hp.PlayerId)
                                                       .Set("bedel", (double)bedel).Set("maas", (double)maas);
                                var so0 = bus.Submit(z, syuk, exec, host, ownerUserId);
                                if (so0.Ok && serbestOyuncu) r.Transfer++;
                                // RED SESSİZ GEÇMEZ: aynı hedefe her hafta düşen bir teklif,
                                // sink'in kilitlendiğini gizlerdi. Kapı bunu sayar.
                                else if (!so0.Ok) r.BeklenmeyenRed++;
                            }
                        }
                        transferSonu:
                        // Sıra bizdeyse ve bedel karşılanabiliyorsa KABUL.
                        for (int t = 0; t < st.Club.TransferTeklifleri.Length; t++)
                        {
                            var o = st.Club.TransferTeklifleri[t];
                            if (o.TeklifId == 0 || !o.SiraTeklifEdende) continue;
                            if (o.TeklifEdenClubId != st.Club.ClubId) continue;   // biz ALICIYIZ
                            if (!st.CanAfford(o.BedelTl)) continue;
                            var z2 = new CommandEnvelope
                            {
                                CommandId = KomutId(hafta, 800 + o.TeklifId % 90), CatalogVersion = Catalog.Version,
                                Source = CommandSource.UI, ActionType = "transfer.respond_offer",
                                IssuedAtUnixMs = host, MatchTick = 0, UserId = ownerUserId,
                                SaveSlotId = 1, TeamIdx = 0, PayloadJson = new byte[0]
                            };
                            var so = bus.Submit(z2, new TestPayload().Set("teklifId", (double)o.TeklifId)
                                                                      .Set("cevap", "kabul"),
                                                exec, host, ownerUserId);
                            if (so.Ok) r.Transfer++;
                        }
                        // KARŞI TARAF: teklife cevap verir (K6 sürücüsü).
                        j.Clear();
                        TransferTick.Ilerlet(st, tb, kural, saveSeed, j);
                        if (j.Validate(st, out _)) j.Apply(st);
                    }

                    // ---- 2) HAFTALIK EKONOMİ TICK'İ ----
                    var sonuc = (WeekResult)(byte)(1 + (hafta % 3));      // G, B, M döngüsü
                    bool evMaci = (hafta % 2) == 0;
                    j.Clear();
                    var L = EconomyTick.Hafta(st, eco, kural, saveSeed, sonuc, evMaci, tb, j);
                    if (!j.Validate(st, out string hata))
                        throw new InvalidOperationException("kademeli koşu journal geçersiz: " + hata);
                    j.Apply(st);
                    r.Toplam.Topla(L);
                    sezonL.Topla(L);
                    if (r.MerdivenSezon < 0) r.MerdivenToplam.Topla(L);
                    if (st.Club.KasaTl < r.MinKasaTl) r.MinKasaTl = st.Club.KasaTl;
                    if (r.IflasSezonu < 0 && st.Club.KasaTl <= eco.iflas.esikTl) r.IflasSezonu = s + 1;
                    host += HaftaMs;
                }
                r.Sezonlar.Add(sezonL);
                if (r.MerdivenSezon < 0 && MerdivenBitti(st, eco)) r.MerdivenSezon = s + 1;
            }
            return r;
        }

        /// <summary>MERDİVEN SONRASI DURAĞAN ORAN — merdiven bittikten sonra `sezon` sezon daha
        /// koşup source/sink'i ölçer. `st` KOŞULMUŞ durumdur (merdiven tamamlanmış olmalı).</summary>
        public static double MerdivenSonrasiOran(GameState st, EconomyBalance eco, WorldRules kural, ulong saveSeed,
                                                 int sezon, IBandProvider bantlar,
                                                 Dictionary<RateClass, RateLimitCfg[]> rlCfg, long ownerUserId,
                                                 TransferBalance tb)
        {
            var toplam = new WeekLedger();
            var j = new WorldJournal();
            int hafta = 0;
            for (int s = 0; s < sezon; s++)
                for (int h = 0; h < kural.yapi.sezonHaftaSayisi; h++, hafta++)
                {
                    j.Clear();
                    var L = EconomyTick.Hafta(st, eco, kural, saveSeed,
                                              (WeekResult)(byte)(1 + (hafta % 3)), (hafta % 2) == 0, tb, j);
                    j.Validate(st, out _); j.Apply(st);
                    toplam.Topla(L);
                }
            return toplam.ToplamGider == 0 ? 0 : (double)toplam.ToplamGelir / toplam.ToplamGider;
        }

        public static bool MerdivenBitti(GameState st, EconomyBalance eco)
        {
            int maxTier = MaxTier(eco);
            for (int t = 0; t < ReferansTesisler.Length; t++)
                if (st.Club.TesisTier[ReferansTesisler[t]] < maxTier) return false;
            return true;
        }

        static bool InsaattaMi(GameState st, int tesis)
        {
            for (int i = 0; i < st.Club.InsaatSlot.Length; i++)
                if (st.Club.InsaatSlot[i].InsaatId != 0 && st.Club.InsaatSlot[i].TesisId == tesis) return true;
            return false;
        }

        /// <summary>Deterministik CommandId — `Guid.NewGuid()` koşuyu tekrar edilemez yapardı
        /// (dedup deposu ve denetim kaydı koşudan koşuya değişirdi).</summary>
        static Guid KomutId(int hafta, int tesis)
        {
            var b = new byte[16];
            b[0] = (byte)hafta; b[1] = (byte)(hafta >> 8); b[2] = (byte)(hafta >> 16); b[3] = (byte)(hafta >> 24);
            b[4] = (byte)tesis; b[5] = 0xC4; b[6] = 0x9E; b[7] = 0x10;
            return new Guid(b);
        }
    }

    /// <summary>Maç kuyruğu casusu — köprünün ME komutunu GERÇEKTEN ürettiğini ölçer.</summary>
    public sealed class SpyMatchSink : IMatchCommandSink
    {
        public readonly List<TheBadge.Sim.Match.MatchCommand> Komutlar = new List<TheBadge.Sim.Match.MatchCommand>();
        public void Enqueue(TheBadge.Sim.Match.MatchCommand cmd) => Komutlar.Add(cmd);
    }

    /// <summary>Online yayın casusu — K6. Klip ve rapor ayrı listelerde tutulur ki
    /// "yayınlandı mı" ve "hangisi yayınlandı" ayrı ayrı ölçülebilsin.</summary>
    /// <summary>YALNIZ belirli bir kaydı patlatan kanal. `SpyOnlineSink.Patlat` hepsini birden
    /// patlatır ve o yüzden SIRA iddiasını ÖLÇEMEZ: hepsi patlayınca "başta takıldı" ile "hepsini
    /// denedi, hepsi patladı" aynı sonucu verir. Sıra korunuyor mu sorusunu ancak ilki patlarken
    /// arkadakiler BAŞARILI OLABİLİYORKEN sorabilirsin.</summary>
    public sealed class SecmeliPatlayanSink : TheBadge.World.IOnlineSink
    {
        public readonly List<(System.Guid cid, int macId)> Klipler = new List<(System.Guid, int)>();
        public int PatlayanMacId = -1;
        public void KlipPaylas(System.Guid commandId, int macId, int pencereSn, byte hedef, long userId)
        {
            if (macId == PatlayanMacId) throw new InvalidOperationException($"mac {macId} icin ag hatasi (test)");
            Klipler.Add((commandId, macId));
        }
        public void OyuncuRaporla(System.Guid commandId, long hedefUserId, byte sebep, string notlar, long userId) { }
    }

    /// <summary>Durumu DEĞİŞTİREN ve AYNI komutta yayın YAPAN teste özel handler. Katalogda böyle
    /// bir aksiyon yok (mevcut yayıncı aksiyonlar durumu değiştirmiyor), ama `WorldExecutor`in
    /// commit SIRASI bu birleşimde anlam kazanıyor: olaylar yayınlardan ÖNCE önbelleğe yazılırsa,
    /// yayın patlayıp durum geri alındığında önbellekte hayalet olaylar kalır.</summary>
    public sealed class HemDegistirHemYayinla : TheBadge.World.IActionHandler
    {
        public RejectionReason Apply(TheBadge.World.GameState st, TheBadge.World.WorldJournal j,
                                     CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
        {
            detail = null;
            j.Set(TheBadge.World.MutTarget.Kulup, 0, TheBadge.World.ClubField.Form, 55);
            j.Emit(new TheBadge.World.WorldEvent(TheBadge.World.WorldEventType.TaktikGuncellendi, 0, 55,
                                                 st.Takvim.Sezon, st.Takvim.Hafta));
            j.PersonaKonusma(env.CommandId, 1, 0, env.UserId);
            return RejectionReason.None;
        }
    }

    /// <summary>Olay kanalı patlatan sink — sözleşme "FIRLATMAMALI, fırlatırsa yutulur" diyor.
    /// Bu tip o sözleşmeyi sınar: patlayan bir kanal komutu DÜŞÜRMEMELİ ve durumu geri ALMAMALI.</summary>
    public sealed class PatlayanOlayKanali : TheBadge.World.IKomutOlaySinki
    {
        public void Yaz(System.Guid commandId, long userId, long anUnixMs,
                        IReadOnlyList<TheBadge.World.WorldEvent> olaylar)
            => throw new InvalidOperationException("olay kanali patladi (test)");
    }

    public sealed class PatlayanPersona : TheBadge.World.IPersonaSink
    {
        public void KonusmaAyarlandi(System.Guid commandId, int personaId, byte tonIndeksi, long userId)
            => throw new InvalidOperationException("persona kanali patladi (test)");
        public void BasinYaniti(System.Guid commandId, int soruId, byte cevapSinifi, long userId)
            => throw new InvalidOperationException("persona kanali patladi (test)");
    }

    public sealed class SessizPersona : TheBadge.World.IPersonaSink
    {
        public void KonusmaAyarlandi(System.Guid commandId, int personaId, byte tonIndeksi, long userId) { }
        public void BasinYaniti(System.Guid commandId, int soruId, byte cevapSinifi, long userId) { }
    }

    public sealed class SpyOnlineSink : TheBadge.World.IOnlineSink
    {
        public readonly List<(System.Guid cid, int macId, int pencereSn, byte hedef, long userId)> Klipler
            = new List<(System.Guid, int, int, byte, long)>();
        public readonly List<(System.Guid cid, long hedefUserId, byte sebep, string notlar, long userId)> Raporlar
            = new List<(System.Guid, long, byte, string, long)>();
        /// <summary>true ise yayın PATLAR — işlem güvenliği ölçümü için.</summary>
        public bool Patlat;
        public void KlipPaylas(System.Guid commandId, int macId, int pencereSn, byte hedef, long userId)
        {
            if (Patlat) throw new InvalidOperationException("ağ zaman aşımı (test)");
            Klipler.Add((commandId, macId, pencereSn, hedef, userId));
        }
        public void OyuncuRaporla(System.Guid commandId, long hedefUserId, byte sebep, string notlar, long userId)
        {
            if (Patlat) throw new InvalidOperationException("ağ zaman aşımı (test)");
            Raporlar.Add((commandId, hedefUserId, sebep, notlar, userId));
        }
    }

    /// <summary>Persona kanal casusu — K7.</summary>
    public sealed class SpyPersonaSink : TheBadge.World.IPersonaSink
    {
        public readonly List<(System.Guid cid, int personaId, byte ton, long userId)> Konusmalar
            = new List<(System.Guid, int, byte, long)>();
        public readonly List<(System.Guid cid, int soruId, byte sinif, long userId)> Basinlar
            = new List<(System.Guid, int, byte, long)>();
        public void KonusmaAyarlandi(System.Guid cid, int personaId, byte ton, long userId)
            => Konusmalar.Add((cid, personaId, ton, userId));
        public void BasinYaniti(System.Guid cid, int soruId, byte sinif, long userId)
            => Basinlar.Add((cid, soruId, sinif, userId));
    }

    /// <summary>K2 dünya durumu kurulum yardımcıları.</summary>
    /// <summary>ROL DAĞILIMI OLAN kadro — K11 köprüsü için. `WorldFixture` herkese `RolId = 1`
    /// veriyor (o fikstürün derdi sahiplik denetimleriydi, diziliş değil); köprü ise hat başına
    /// oyuncu ister. Güç indeksten TÜRETİLİR (RNG yok): kapı, seçimin GÜCE göre yapıldığını
    /// ölçebilsin diye güçler bilinerek dağıtılır.</summary>
    public static class KadroFixture
    {
        // rolId → hat eşlemesi `squad.balance.json`da: 1 KL · 2-8 DF · 9-20 OS · 21-32 FV
        public const int Kaleci = 2, Defans = 6, Ortasaha = 6, Forvet = 4;   // 18 kişilik kadro

        public static GameState Kur(WorldRules rules, long clubId, long ownerUserId, long kasaTl = 20_000_000)
        {
            var st = GameState.Olustur(rules, clubId, ownerUserId);
            st.Club.KasaTl = kasaTl;
            st.Club.StadyumKapasite = 30000;
            var list = new List<PlayerState>();
            // KİMLİK TABANI KULÜPTEN TÜRETİLİR. Sabit 100 tabanı iki kulübe aynı PlayerId'leri
            // veriyordu ve motor bunu "PlayerId 101 iki takımda birden" diye reddetti — doğru
            // yakaladı, ama fikstürün işi motora geçersiz veri göndermemek. Aralık motorun
            // `short` kimlik genişliğinde kalır (SquadBridge bunu ayrıca denetler).
            int pid = 1000 + (int)(clubId % 200) * 50;
            Instruction[] Yuva() => new Instruction[rules.yapi.talimatYuvaSayisi];
            void Ekle(int adet, int rolTaban, int rolAralik)
            {
                for (int i = 0; i < adet; i++)
                {
                    // Güç 48-88 arası, indeksten deterministik. Aynı hattaki oyuncular FARKLI
                    // güçte olmalı ki "en iyisi seçildi mi" ölçülebilsin.
                    byte guc = (byte)(48 + (pid * 7) % 41);
                    list.Add(new PlayerState
                    {
                        PlayerId = pid, ClubId = clubId, HaftalikMaasTl = 40_000,
                        SozlesmeKalanHafta = 100, Moral = 60, Kondisyon = 90,
                        RolId = (byte)(rolTaban + (i % rolAralik)),
                        Guc = guc, Potansiyel = (byte)System.Math.Min(99, guc + 6),
                        Yas = (byte)(20 + (pid % 15)), Talimatlar = Yuva()
                    });
                    pid++;
                }
            }
            Ekle(Kaleci, 1, 1); Ekle(Defans, 2, 7); Ekle(Ortasaha, 9, 12); Ekle(Forvet, 21, 12);
            st.Oyuncular = list.ToArray();
            st.Club.HaftalikMaasGiderTl = list.Count * 40_000L;
            st.Validate();
            return st;
        }
    }

    public static class WorldFixture
    {
        /// <summary>Kanonik kadro: PlayerId artan. `yabanciSayisi` kadar oyuncu BAŞKA kulüpte,
        /// `serbestSayisi` kadarı serbest (ClubId 0) — sahiplik denetimlerinin üç kolu için.</summary>
        public static GameState Kur(WorldRules rules, long clubId, long ownerUserId,
                                    int kendi, int yabanci, int serbest, long kasaTl)
        {
            var st = GameState.Olustur(rules, clubId, ownerUserId);
            st.Club.KasaTl = kasaTl;
            st.Club.StadyumKapasite = 20000;
            var list = new List<PlayerState>();
            int pid = 100;
            Instruction[] Yuva() => new Instruction[rules.yapi.talimatYuvaSayisi];
            for (int i = 0; i < kendi; i++)
                list.Add(new PlayerState { PlayerId = pid++, ClubId = clubId, HaftalikMaasTl = 10000,
                                           SozlesmeKalanHafta = 100, Moral = 60, Kondisyon = 90, RolId = 1,
                                           Talimatlar = Yuva() });
            for (int i = 0; i < yabanci; i++)
                list.Add(new PlayerState { PlayerId = pid++, ClubId = clubId + 1, HaftalikMaasTl = 12000,
                                           SozlesmeKalanHafta = 100, Moral = 60, Kondisyon = 90, RolId = 1,
                                           Talimatlar = Yuva() });
            for (int i = 0; i < serbest; i++)
                list.Add(new PlayerState { PlayerId = pid++, ClubId = 0, HaftalikMaasTl = 0,
                                           SozlesmeKalanHafta = 0, Moral = 50, Kondisyon = 80, RolId = 1,
                                           Talimatlar = Yuva() });
            st.Oyuncular = list.ToArray();
            st.Validate();
            return st;
        }

        /// <summary>Kadrodaki KENDİ oyuncumuzun ilk PlayerId'si.</summary>
        public static int IlkKendi(GameState st)
        {
            for (int i = 0; i < st.Oyuncular.Length; i++)
                if (st.Oyuncular[i].ClubId == st.Club.ClubId) return st.Oyuncular[i].PlayerId;
            return -1;
        }
        public static int IlkYabanci(GameState st)
        {
            for (int i = 0; i < st.Oyuncular.Length; i++)
                if (st.Oyuncular[i].ClubId != 0 && st.Oyuncular[i].ClubId != st.Club.ClubId) return st.Oyuncular[i].PlayerId;
            return -1;
        }
        public static int IlkSerbest(GameState st)
        {
            for (int i = 0; i < st.Oyuncular.Length; i++)
                if (st.Oyuncular[i].ClubId == 0) return st.Oyuncular[i].PlayerId;
            return -1;
        }
    }
}
