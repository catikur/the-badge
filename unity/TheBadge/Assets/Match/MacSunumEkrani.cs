using System;
using System.Text;
using TheBadge.CommandBus;      // CommandOutcome (bus reddinin sebebi buradan gelir)
using TheBadge.Sim.Commands;
using TheBadge.Sim.Match;
using UnityEngine;
using UnityEngine.UIElements;
// TAKMA AD ZORUNLU: `UnityEngine.EventType` (editör girdi olayları) ile motorun
// `TheBadge.Sim.Match.EventType`i aynı adı taşıyor ve iki ad alanı da açık; takma ad
// olmadan derleyici CS0104 ile "belirsiz referans" diyor.
using OlayTipi = TheBadge.Sim.Match.EventType;

namespace TheBadge.Match
{
    /// <summary>MAÇ SUNUM EKRANI — 5G-a / S2. Gerçek `MatchEngine`in üstünde, placeholder
    /// art'la, canlı maç sunumu. UI Toolkit (K2 kararı); portre, dikey saha (FAZ 00.5).
    ///
    /// BU SINIF DURUMU OKUR, YAZMAZ (Kural 3): `MatchState`e erişimi yok — `MacKosucu`nun
    /// salt-okunur yüzeyinden okur. Müdahale TEK KAPI'dan geçer (`MacKomutKoprusu`), motorun
    /// kuyruğuna doğrudan yazan tek satır yoktur.
    ///
    /// İKİ AYRI RED YOLU DA GÖSTERİLİR (Kural 2): bus reddi SEBEBİYLE (`CommandOutcome.Reason`
    /// + `.Detail`), motorun geç reddi en az SAYAÇ olarak. Hiçbir red sessizce yutulmaz.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MacSunumEkrani : MonoBehaviour
    {
        enum Hiz { Bir = 0, Iki = 1, Atla = 2 }

        [Tooltip("Ekranın tüm ayarlanabilir sayıları — [KALİBRE] adayı (bkz. MacSunumAyarlari).")]
        public MacSunumAyarlari ayarlar = new MacSunumAyarlari();

        [Tooltip("Maç tohumu. Aynı tohum + aynı müdahaleler = aynı maç (Kural 6).")]
        public ulong tohum = 20260906UL;

        [Tooltip("Komut zarfının kimliği. Kapı 3 kulüp sahipliğini bununla doğrular.")]
        public long userId = 42;

        BalansKaynagi balans;
        MacKosucu kosucu;
        MacKomutKoprusu koprusu;

        Hiz hiz = Hiz.Bir;
        float tickBirikimi;
        float duraklamaKalan;
        float redMesajiKalan;
        int macNo;
        bool bitisGosterildi;

        // ---- telemetri (TASK-003). Ekran olay ADI bilmez; `MacTelemetri`ye çağrı yapar.
        MacTelemetri telemetri;
        float macGercekSure;          // maçın GERÇEK zamanda izlenme süresi (kapı metriği)
        uint sonIlerlemeTick;
        byte sonDevre;
        int sonKritikAnSayisi, sonTaktikDegisiklik;
        Label telemetriUyariEt;
        VisualElement telemetriUyariKutu;

        // Kadranların İSTENEN değeri (UI niyeti). Motorun UYGULADIĞI değer ayrı gösterilir:
        // komut ME 14.2'ye göre güvenli bir anda uygulanır, o yüzden ikisi bir süre ayrışır ve
        // bu ayrışma kullanıcıdan SAKLANMAZ.
        readonly int[] istenen = new int[4];
        static readonly string[] KadranAd = { "MENTALİTE", "TEMPO", "PRES", "HAT" };

        int busRedSayaci, motorRedOncekiDeger;
        string sonRedMetni = "";

        readonly SahaNoktasi[] ajanlar = new SahaNoktasi[22];
        readonly StringBuilder sb = new StringBuilder(256);

        // ---- görsel öğeler
        Label skorEt, saatEt, sayaclarEt, redEt, taktikEtkiEt, duraklamaEt;
        VisualElement seritEv, seritBe, seritDep, saha, duraklamaKatman, bitisKatman;
        Label seritEt;
        readonly VisualElement[] nokta = new VisualElement[22];
        VisualElement topNokta;
        readonly Label[] spikerSatir = new Label[16];
        readonly Label[] kadranDeger = new Label[4];
        Button[] hizDugme;
        Label bitisEt;

        float seritEvGosterilen, seritBeGosterilen, seritDepGosterilen;

        void OnEnable()
        {
            ayarlar.Dogrula();
            balans = BalansKaynagi.Yukle();
            // TELEMETRİ EKRANDAN ÖNCE: kurulumu başarısız olursa `ArayuzKur` uyarı şeridini
            // açabilsin. Kurulum İSTİSNA ATMAZ — telemetri oynanışı düşürmez (Kural 3).
            telemetri = MacTelemetri.Kur(TelemetriDizini(), Application.version, SystemInfo.deviceModel);
            ArayuzKur();
            MacBaslat();
        }

        /// <summary>Telemetri dizini. Ayarlardaki geçersiz kılma DOLUYSA o kullanılır — kabul
        /// ölçütünün "yazılamayan bir dizine yönlendir" senaryosu bu kapıdan koşulur.</summary>
        string TelemetriDizini()
            => string.IsNullOrEmpty(ayarlar.telemetriDiziniGecersizKil)
                ? System.IO.Path.Combine(Application.persistentDataPath, "telemetry")
                : ayarlar.telemetriDiziniGecersizKil;

        void OnDisable()
        {
            // TEMİZ ÇIKIŞ işareti. Yokluğu terk demektir — o yüzden buna GÜVENİLMEZ, yalnız
            // yazılır (TASK-003: terk çıkarsanır, raporlanmaz).
            telemetri?.SeansBitti(macNo + 1);
            telemetri?.Dispose();
            telemetri = null;
        }

        // ------------------------------------------------------------------ MAÇ YAŞAM DÖNGÜSÜ

        void MacBaslat()
        {
            // ÖNCEKİ MAÇ BİTMEDEN yeni maça geçildiyse onu `tamamlandi=0` ile kapat: "bir maç
            // daha" sinyali ancak biten maçlarla sayılabilir, yarıda bırakılan da bir veridir.
            if (kosucu != null && !kosucu.Bitti)
                telemetri?.MacBitti(macNo, kosucu.EvGol, kosucu.DeplasmanGol, macGercekSure,
                                    kosucu.KritikAnSayisi, koprusu?.MotoraIletilen ?? 0,
                                    busRedSayaci, kosucu.MotorRedSayaci, false);

            kosucu = new MacKosucu(balans.Sim, tohum + (ulong)macNo, ayarlar.kritikAnOrneklemeTick);
            // KÖPRÜ MOTORUN KENDİ KUYRUĞUNA BAĞLANIR — komut ancak dört kapıdan geçince oraya
            // düşer. Kuyruğun kendisi bu sınıfa hiç verilmez (bkz. `MacKosucu.KopruKur`).
            koprusu = kosucu.KopruKur(balans, userId);
            for (int i = 0; i < 4; i++) istenen[i] = 0;
            busRedSayaci = 0; motorRedOncekiDeger = 0; sonRedMetni = "";
            tickBirikimi = 0; duraklamaKalan = 0; redMesajiKalan = 0;
            seritEvGosterilen = seritBeGosterilen = seritDepGosterilen = 1f / 3f;
            duraklamaKatman.style.display = DisplayStyle.None;
            bitisKatman.style.display = DisplayStyle.None;
            bitisGosterildi = false;
            macGercekSure = 0f; sonIlerlemeTick = 0; sonDevre = 0;
            sonKritikAnSayisi = 0; sonTaktikDegisiklik = 0;
            telemetri?.MacBasladi(macNo, tohum + (ulong)macNo);
            // KOŞULLU YAZILAN ETİKETLER ELLE TEMİZLENİR. `Ciz()` her kare yazdığı etiketleri
            // zaten tazeliyor; bu ikisi ise YALNIZ olay olunca yazılıyor, yani yeni maça
            // önceki maçın satırı olarak taşınıyorlardı. `taktikEtkiEt` turun ölçtüğü
            // "aynı tick" sinyalini taşıdığı için bu, yeni tohuma AİT SANILAN bir sayı
            // gösterirdi — gözlem turunda yanlış veri (inceleme bulgusu, cursor).
            taktikEtkiEt.text = "";
            duraklamaEt.text = "";
            // HIZ 1x'E DÖNER. Maç ATLA'dayken bitirilip "bir maç daha" denince yeni maç da
            // anında bitiyordu — gözlem turunda "bir maç daha" sinyalini ölçmek imkânsız olurdu
            // (Play modunda görüldü). OTOMATİK: telemetriye kullanıcı eylemi olarak yazılmaz.
            HizSec(Hiz.Bir, kullaniciEylemi: false);
        }

        void Update()
        {
            if (kosucu == null) return;

            if (redMesajiKalan > 0) redMesajiKalan -= Time.deltaTime;

            if (kosucu.Bitti) { BitisGoster(); Ciz(); return; }

            // MAÇIN GERÇEK ZAMANDA İZLENME SÜRESİ — kapı metriği ("maç başına izleme süresi").
            // Duraklama da izlemedir, o yüzden erken dönüşlerden ÖNCE sayılır.
            macGercekSure += Time.deltaTime;

            // DURAKLAMA: tick İŞLENMEZ. Motor yavaşlatılmaz, sunum durur — maç sonucu
            // etkilenmez (Kural 6).
            if (duraklamaKalan > 0)
            {
                duraklamaKalan -= Time.deltaTime;
                if (duraklamaKalan <= 0) duraklamaKatman.style.display = DisplayStyle.None;
                Ciz();
                return;
            }

            if (hiz == Hiz.Atla)
            {
                // ATLA: duraklamaya girilmez ama kritik anlar YİNE SAYILIR — ritim raporu
                // hızdan bağımsız kalsın diye.
                // TELEMETRİ HER TICK'TE İŞLENİR, toplu DEĞİL (inceleme bulgusu, codex P1).
                // Önce 4000 tick'lik döngüyü tek bir bayrağa indirip döngüden SONRA bir kez
                // işliyordum; `MacKosucu` yalnız EN SON kritik anı taşıdığı için aradakiler
                // kayboluyordu — commit'lenen örnekte `kritik_an` sıra 2'den başlıyordu, sıra 1
                // yutulmuştu. Bu, turun çekirdek oranının (duraklama başına müdahale) PAYDASINI
                // bozar. `TelemetriTick` zaten değişim-tetiklemeli, bu yüzden her tick çağırmak
                // hem doğru hem ucuz.
                for (int n = 0; n < ayarlar.atlaTickTavani && !kosucu.Bitti; n++)
                    TelemetriTick(kosucu.Ilerlet(1));
                Ciz();
                return;
            }

            int carpan = hiz == Hiz.Iki ? ayarlar.hizCarpani2x : 1;
            tickBirikimi += Time.deltaTime * ayarlar.tickHizi1x * carpan;
            int butce = (int)tickBirikimi;
            if (butce > 0)
            {
                tickBirikimi -= butce;
                bool kritikAn = kosucu.Ilerlet(butce);
                TelemetriTick(kritikAn);
                if (kritikAn && ayarlar.duraklamaSaniye > 0f)
                {
                    // `> 0` ŞARTI ZORUNLU: süre 0 iken katman `Flex` yapılıyor ama aşağıdaki
                    // geri sayım hiç çalışmadığı için bir daha KAPANMIYORDU — maç kalıcı bir
                    // "KRİTİK AN" perdesinin arkasında akardı (inceleme bulgusu, codex).
                    // 0 geçerli bir ayardır ve "duraklama yok" demektir: kritik anlar yine
                    // SAYILIR (ritim raporu bozulmaz), yalnız sunum durmaz. Bu, turda
                    // "duraklama gerçekten gerekli mi" sorusunu ölçmenin yolu.
                    duraklamaKalan = ayarlar.duraklamaSaniye;
                    duraklamaKatman.style.display = DisplayStyle.Flex;
                    duraklamaEt.text = $"KRİTİK AN  ·  sıçrama {kosucu.SonSicrama:0.000}\n"
                                     + $"{kosucu.KritikAnSayisi}. duraklama";
                }
            }
            Ciz();
        }

        /// <summary>Tick bloğundan sonra düşen telemetri olayları. TEK YERDEN çağrılır ki
        /// hız modları arasında olay kümesi ayrışmasın (ATLA'da da aynı satırlar düşer).</summary>
        void TelemetriTick(bool kritikAnAtesledi)
        {
            if (telemetri == null) return;

            // KRİTİK AN — `mudahale` ile eşleştirildiğinde turun en kritik oranını verir.
            if (kritikAnAtesledi && kosucu.KritikAnSayisi != sonKritikAnSayisi)
            {
                sonKritikAnSayisi = kosucu.KritikAnSayisi;
                telemetri.KritikAn(macNo, kosucu.KritikAnSayisi, kosucu.SonSicrama,
                                   kosucu.Tick, kosucu.Olasilik.Ev);
            }

            // ŞERİDİN HAREKETİ — motor taktiği UYGULADIĞI an; gönderim anında "sonra" yok.
            if (kosucu.TaktikEtkisiVar && kosucu.TaktikDegisiklik != sonTaktikDegisiklik)
            {
                sonTaktikDegisiklik = kosucu.TaktikDegisiklik;
                telemetri.TaktikUygulandi(macNo, kosucu.TaktikTick,
                                          kosucu.TaktikOncesi.Ev, kosucu.TaktikSonrasi.Ev);
            }

            // FAZ — devre değişimi olduğu ANDA yazılır ("nerede bıraktı" okuması için).
            if (kosucu.Devre != sonDevre)
            {
                sonDevre = kosucu.Devre;
                telemetri.Faz(macNo, "devre_" + sonDevre, kosucu.Tick);
            }

            // İLERLEME — terk terminal olaya bağlanamadığı için konum SON SATIRDAN okunur.
            uint aralik = (uint)(ayarlar.telemetriIlerlemeDakika * 60 * MatchEngine.TicksPerSecond);
            if (kosucu.Tick >= sonIlerlemeTick + aralik)
            {
                sonIlerlemeTick = kosucu.Tick;
                telemetri.Ilerleme(macNo, kosucu.Tick, kosucu.EvGol, kosucu.DeplasmanGol,
                                   kosucu.Devre, macGercekSure);
            }
        }

        void BitisGoster()
        {
            if (bitisGosterildi) return;
            bitisGosterildi = true;
            telemetri?.Faz(macNo, "mac_sonu", kosucu.Tick);
            telemetri?.MacBitti(macNo, kosucu.EvGol, kosucu.DeplasmanGol, macGercekSure,
                                kosucu.KritikAnSayisi, koprusu.MotoraIletilen,
                                busRedSayaci, kosucu.MotorRedSayaci, true);
            bitisKatman.style.display = DisplayStyle.Flex;
            // "(beklenen 8-12)" DİYE YAZMIYORUZ: 8-12 bir ORTALAMA (K1, 200 maç). Tek maçta
            // 24 maçlık ölçümde 4 ile 17 arası görüldü ve ikisi de sağlıklı. Bandı tek maçın
            // yanına yazmak, gözlem turundaki kişiye normal bir maçı "bozuk" diye okuturdu.
            bitisEt.text = $"MAÇ SONU  {kosucu.EvGol} - {kosucu.DeplasmanGol}\n\n"
                         + $"kritik an duraklaması: {kosucu.KritikAnSayisi}"
                         + "   (ortalama 8-12; tek maçta 4-17 normal)\n"
                         + $"uygulanan taktik değişikliği: {kosucu.TaktikDegisiklik}\n"
                         + $"bus reddi: {busRedSayaci}   ·   motor geç reddi: {kosucu.MotorRedSayaci}\n"
                         + $"tohum: {tohum + (ulong)macNo}";
        }

        // ------------------------------------------------------------------ MÜDAHALE (TEK KAPI)

        /// <summary>Kadranı `delta` kadar oynatıp DÖRDÜNÜ birden gönderir. Katalog aksiyonu
        /// dört alanı da zorunlu ister; eksik alan `SchemaViolation` olurdu.</summary>
        void KadranOynat(int idx, int delta)
        {
            int v = istenen[idx] + delta;
            if (v < ayarlar.kadranMin) v = ayarlar.kadranMin;
            if (v > ayarlar.kadranMax) v = ayarlar.kadranMax;
            istenen[idx] = v;
            Gonder(istenen[0], istenen[1], istenen[2], istenen[3]);
        }

        /// <summary>BANT DIŞI DENEME — kabul ölçütü "bant dışı bir delta ile iki red yolu da
        /// elle denenip raporlanır" diyor. Bu düğme bilerek bandı aşan bir değer gönderir;
        /// beklenen sonuç bus'ın `ParamOutOfBand` reddi ve sebebin ekranda görünmesi.</summary>
        void BantDisiDene()
            => Gonder(ayarlar.bantDisiTestDegeri, istenen[1], istenen[2], istenen[3]);

        void Gonder(int mentalite, int tempo, int pres, int hat)
        {
            // MatchTick > 0 ZORUNLU: 0 "hub komutu" demektir (CB 3.1) ve kalıcı taktiği
            // düzenlerdi. +1, komutun bu tick'ten SONRA uygulanacağını söyler (ME 14.2).
            uint macTick = kosucu.Tick + 1;
            // Host saati — bus'ın hız sınırı ve tekilleştirme penceresi için. Simülasyona
            // GİRMEZ; `MatchState`e dokunmaz, determinizmi etkilemez.
            long saat = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            double evOnce = kosucu.Olasilik.Ev;
            // DURAKLAMA PENCERESİ: müdahale duraklama sırasında mı geldi, yoksa an geçip
            // gitti mi? `kritik_an` ile bu alan birlikte okunduğunda turun çekirdek sorusu
            // ("duraklamaların kaçında oyuncu bir şey yaptı") tek sorguyla cevaplanır.
            bool duraklamada = duraklamaKalan > 0f;

            CommandOutcome o = koprusu.TaktikGonder(mentalite, tempo, pres, hat, macTick, saat);

            telemetri?.Mudahale(macNo, macTick, mentalite, tempo, pres, hat,
                                o.Ok, o.Ok ? null : o.Reason.ToString(), duraklamada, evOnce);

            if (!o.Ok)
            {
                // ---- BİRİNCİ RED YOLU: BUS. Komut motora HİÇ ULAŞMAZ, `RejectedCommands`
                // DEĞİŞMEZ. Kullanıcıya gösterilecek olan budur (CB 11.1).
                busRedSayaci++;
                sonRedMetni = $"BUS REDDİ — {o.Reason}" + (string.IsNullOrEmpty(o.Detail) ? "" : $" · {o.Detail}");
                redMesajiKalan = ayarlar.redMesajiSaniye;
            }
        }

        /// <summary>İKİNCİ RED YOLU: MOTOR GEÇ REDDİ. Bus kabul etti, komut kuyruğa düştü, ama
        /// uygulama anında motor reddetti (bant dışı delta, hak bitti, konuşma bekleme süresi).
        /// SEBEP TAŞIMAZ — sayacın arttığını görmek yeter; sessizce yutulmuyor olması şart.</summary>
        void MotorRedKontrol()
        {
            int simdi = kosucu.MotorRedSayaci;
            if (simdi <= motorRedOncekiDeger) return;
            int fark = simdi - motorRedOncekiDeger;
            motorRedOncekiDeger = simdi;
            sonRedMetni = $"MOTOR GEÇ REDDİ ×{fark} — bus geçti, uygulama anında düştü (sebep taşınmıyor)";
            redMesajiKalan = ayarlar.redMesajiSaniye;
        }

        // ------------------------------------------------------------------ ÇİZİM

        void Ciz()
        {
            MotorRedKontrol();
            TelemetriUyarisiTazele();

            int sn = (int)(kosucu.Tick / (uint)MatchEngine.TicksPerSecond);
            skorEt.text = $"EV  {kosucu.EvGol} - {kosucu.DeplasmanGol}  DEP";
            saatEt.text = $"{sn / 60:00}:{sn % 60:00}   ·   {kosucu.Devre}. devre   ·   {kosucu.Faz}";

            var u = kosucu.Olasilik;
            float k = ayarlar.seritAnimasyonSaniye <= 0f
                ? 1f : Mathf.Clamp01(Time.deltaTime / ayarlar.seritAnimasyonSaniye);
            seritEvGosterilen = Mathf.Lerp(seritEvGosterilen, (float)u.Ev, k);
            seritBeGosterilen = Mathf.Lerp(seritBeGosterilen, (float)u.Beraberlik, k);
            seritDepGosterilen = Mathf.Lerp(seritDepGosterilen, (float)u.Deplasman, k);
            seritEv.style.flexGrow = seritEvGosterilen;
            seritBe.style.flexGrow = seritBeGosterilen;
            seritDep.style.flexGrow = seritDepGosterilen;
            // HAM değerler yazıyla: çubuk yumuşatılıyor, SAYI yumuşatılmıyor. Kabul ölçütünün
            // ölçtüğü büyüklük `AnlikOlasilik`ın kendisi; animasyon onu geciktirmemeli.
            seritEt.text = $"G %{u.Ev * 100:0.0}    B %{u.Beraberlik * 100:0.0}    M %{u.Deplasman * 100:0.0}";

            if (kosucu.TaktikEtkisiVar)
            {
                var a = kosucu.TaktikOncesi; var b = kosucu.TaktikSonrasi;
                taktikEtkiEt.text = $"son müdahale (tick {kosucu.TaktikTick}, AYNI TICK): "
                                  + $"G %{a.Ev * 100:0.0} → %{b.Ev * 100:0.0}";
            }

            sayaclarEt.text = $"duraklama {kosucu.KritikAnSayisi}   ·   uygulanan taktik {kosucu.TaktikDegisiklik}"
                            + $"   ·   motora iletilen {koprusu.MotoraIletilen}"
                            + $"   ·   bus reddi {busRedSayaci}   ·   motor geç reddi {kosucu.MotorRedSayaci}";

            redEt.text = redMesajiKalan > 0 ? sonRedMetni : "";
            redEt.style.display = redMesajiKalan > 0 ? DisplayStyle.Flex : DisplayStyle.None;

            for (int i = 0; i < 4; i++)
            {
                int uygulanan = i == 0 ? kosucu.EvMentalite : i == 1 ? kosucu.EvTempo
                              : i == 2 ? kosucu.EvPres : kosucu.EvHat;
                kadranDeger[i].text = istenen[i] == uygulanan
                    ? $"{istenen[i]:+0;-0;0}"
                    : $"{istenen[i]:+0;-0;0}  (uygulanan {uygulanan:+0;-0;0})";
            }

            SahaCiz();
            SpikerCiz();
        }

        /// <summary>Telemetri bozulunca BANT DIŞI uyarıyı gösterir. Her karede bakılır çünkü
        /// bozulma oturumun ORTASINDA da olabilir; bir kez kurulup bırakılan bir kontrol o
        /// senaryoyu kaçırırdı.</summary>
        void TelemetriUyarisiTazele()
        {
            bool bozuk = telemetri != null && !telemetri.Calisiyor;
            telemetriUyariKutu.style.display = bozuk ? DisplayStyle.Flex : DisplayStyle.None;
            if (bozuk && telemetriUyariEt.text.Length == 0)
                telemetriUyariEt.text = "⚠ TELEMETRİ YAZILAMIYOR — TUR ÖLÇÜLEMEZ\n" + telemetri.HataMesaji;
        }

        void SahaCiz()
        {
            kosucu.AjanlariKopyala(ajanlar);
            for (int i = 0; i < 22; i++)
            {
                var a = ajanlar[i];
                nokta[i].style.display = a.Sahada ? DisplayStyle.Flex : DisplayStyle.None;
                if (!a.Sahada) continue;
                Yerlestir(nokta[i], a.Xmm, a.Ymm);
                float parlak = 0.45f + 0.55f * (a.Ek / 1000f);        // enerji → parlaklık
                // İKİ TAKIM AÇIK SEÇİK AYRILIR. İlk denemede deplasman koyu gri-maviydi ve
                // enerji parlaklığı düşünce çim üstünde ev takımından ayırt edilemiyordu —
                // ekran görüntüsünde görüldü. Placeholder "renkli şekil" olabilir ama
                // OYNANABİLİR olmak zorunda: kimin nerede olduğu okunmayan bir maç sunumu
                // gözlem turunda sunumu değil kafa karışıklığını ölçerdi.
                Color taban = i < 11 ? new Color(0.96f, 0.97f, 1f) : new Color(0.40f, 0.62f, 1f);
                nokta[i].style.backgroundColor = new Color(taban.r * parlak, taban.g * parlak, taban.b * parlak);
            }
            var t = kosucu.Top;
            Yerlestir(topNokta, t.Xmm, t.Ymm);
        }

        /// <summary>DİKEY SAHA (portre): maçın X'i (uzunluk, ±52.500 mm) ekranda YUKARI-AŞAĞI,
        /// Y'si (genişlik, ±34.000 mm) SAĞA-SOLA. Ev takımı yukarı hücum eder.</summary>
        static void Yerlestir(VisualElement e, int xmm, int ymm)
        {
            float solPct = (ymm + 34000f) / 68000f * 100f;
            float ustPct = (52500f - xmm) / 105000f * 100f;
            e.style.left = new Length(solPct, LengthUnit.Percent);
            e.style.top = new Length(ustPct, LengthUnit.Percent);
        }

        void SpikerCiz()
        {
            int gosterilecek = Mathf.Min(ayarlar.spikerSatirSayisi, spikerSatir.Length);
            // GERİYE DOĞRU tarayıp SON `gosterilecek` KAYDA DEĞER olayı topluyoruz. Ham akış
            // pas ağırlıklı (her pas bir olay): filtresiz altı satırın altısı da "pas tamam"
            // oluyordu ve spiker akışı bir spiker akışı değil, bir sayaç oluyordu.
            int yazilan = 0;
            for (int idx = kosucu.OlaySayisi - 1; idx >= 0 && yazilan < gosterilecek; idx--)
            {
                var o = kosucu.Olay(idx);
                if (!KayitaDeger(o.Kind)) continue;
                int s = gosterilecek - 1 - yazilan;          // en yeni EN ALTTA
                sb.Clear();
                sb.Append(o.Minute).Append("'  ").Append(TarafAdi(o.TeamIdx)).Append("  ").Append(OlayAdi(o.Kind));
                spikerSatir[s].text = sb.ToString();
                // Gol ve kart vurgulanır: spiker akışının okunabilirliği greybox'ın blok
                // ritmini taşıyan şeydi.
                spikerSatir[s].style.color = o.Kind == OlayTipi.Goal ? new Color(1f, 0.85f, 0.3f)
                    : o.Kind == OlayTipi.RedCard ? new Color(1f, 0.45f, 0.4f)
                    : new Color(0.78f, 0.82f, 0.88f);
                yazilan++;
            }
            for (int s = 0; s < gosterilecek - yazilan; s++) spikerSatir[s].text = "";
        }

        /// <summary>Spiker akışına GİRECEK olaylar. Top akışı olayları (pas, çalım, top
        /// kazanma, taç, aut) dışarıda: dakikada onlarca üretiliyorlar ve altı satırlık akışı
        /// tamamen doldurup şut/gol/kart gibi ANLAMLI olayları görünmez yapıyorlar.
        ///
        /// SAYILARI ETKİLEMEZ: bu yalnız GÖRÜNÜRLÜK filtresidir, motorun olay kaydına
        /// dokunmaz ve `EventCount` olduğu gibi kalır.</summary>
        static bool KayitaDeger(OlayTipi t)
        {
            switch (t)
            {
                case OlayTipi.PassCompleted:
                case OlayTipi.PassIntercepted:
                case OlayTipi.TouchError:
                case OlayTipi.DribblePast:
                case OlayTipi.TackleWon:
                case OlayTipi.BallOut:
                case OlayTipi.ThrowIn:
                // FAZ DEĞİŞİMİ İÇ DURUM İŞARETİDİR, spiker cümlesi değil: kickoff ↔ ölü top ↔
                // duran top geçişlerinde sürekli düşüyor ve ilk denemede altı satırın DÖRDÜNÜ
                // dolduruyordu (ekran görüntüsünde görüldü). Devre/maç sonu zaten skor
                // satırındaki `Faz` alanında görünüyor.
                case OlayTipi.PhaseChange:
                case OlayTipi.None:
                    return false;
                default:
                    return true;
            }
        }

        static string TarafAdi(byte teamIdx) => teamIdx == 0 ? "EV " : teamIdx == 1 ? "DEP" : "—  ";

        /// <summary>Placeholder spiker sözlüğü. GERÇEK DİL/LLM AKIŞI DEĞİL (brif Out: röportaj
        /// ve hikaye S3/5G-b); burada olay tipinin okunur adı yeter.</summary>
        static string OlayAdi(OlayTipi t)
        {
            switch (t)
            {
                case OlayTipi.Goal: return "GOL!";
                case OlayTipi.AssistRecorded: return "asist";
                case OlayTipi.CrossDelivered: return "orta";
                case OlayTipi.AdvantagePlayed: return "avantaj";
                case OlayTipi.InjuryOccurred: return "sakatlık";
                case OlayTipi.MomentumShift: return "momentum kaydı";
                case OlayTipi.StaminaAlert: return "kondisyon uyarısı";
                case OlayTipi.ShotOnTarget: return "isabetli şut";
                case OlayTipi.ShotOffTarget: return "auta şut";
                case OlayTipi.ShotBlocked: return "şut bloklandı";
                case OlayTipi.Save: return "kaleci kurtardı";
                case OlayTipi.Parry: return "kaleci çeldi";
                case OlayTipi.Post: return "direk!";
                case OlayTipi.BigChanceMissed: return "net fırsat kaçtı";
                case OlayTipi.CornerAwarded: return "korner";
                case OlayTipi.FreeKickAwarded: return "serbest vuruş";
                case OlayTipi.PenaltyAwarded: return "PENALTI";
                case OlayTipi.Offside: return "ofsayt";
                case OlayTipi.FoulCommitted: return "faul";
                case OlayTipi.YellowCard: return "sarı kart";
                case OlayTipi.RedCard: return "KIRMIZI KART";
                case OlayTipi.VarReviewStarted: return "VAR incelemesi";
                case OlayTipi.VarDecision: return "VAR kararı";
                case OlayTipi.Substitution: return "oyuncu değişikliği";
                case OlayTipi.TacticChange: return "taktik değişikliği";
                case OlayTipi.MotivationTalk: return "kenardan konuşma";
                case OlayTipi.PhaseChange: return "faz değişti";
                case OlayTipi.TackleWon: return "top kazanıldı";
                case OlayTipi.DribblePast: return "çalım";
                case OlayTipi.ThrowIn: return "taç";
                default: return t.ToString();
            }
        }

        // ------------------------------------------------------------------ ARAYÜZ KURULUMU

        static readonly Color Zemin = new Color(0.06f, 0.07f, 0.09f);
        static readonly Color Panel = new Color(0.11f, 0.13f, 0.16f);
        static readonly Color Cim = new Color(0.10f, 0.22f, 0.14f);
        static readonly Color Metin = new Color(0.88f, 0.91f, 0.95f);
        static readonly Color Sonuk = new Color(0.55f, 0.60f, 0.68f);

        void ArayuzKur()
        {
            var kok = GetComponent<UIDocument>().rootVisualElement;
            kok.Clear();
            kok.style.flexDirection = FlexDirection.Column;
            kok.style.backgroundColor = Zemin;
            kok.style.flexGrow = 1;
            kok.style.paddingLeft = 8; kok.style.paddingRight = 8;
            kok.style.paddingTop = 8; kok.style.paddingBottom = 8;

            // ---- TELEMETRİ UYARI ŞERİDİ (TASK-003 Kural 3): BANT DIŞI bildirim.
            // Hatayı telemetri dosyasına yazmak aynı bozuk yazıcıyı kullanmak olurdu ve dosya
            // yalnızca EKSİK görünürdü — ölçümün başarısız olduğuna dair iz kalmazdı. Gözlemci
            // odada olduğu için en hızlı sinyal EKRANDIR. Kalıcı iz `PlayerPrefs` + yanına
            // yazılmaya ÇALIŞILAN `telemetry_error.txt` (bkz. `MacTelemetri.Kur`).
            // Şerit kalıcıdır: süreyle kaybolmaz, oturum boşa gitmeden fark edilsin.
            // ŞERİT HER ZAMAN KURULUR, GİZLİ BAŞLAR. Önce yalnız kurulum anında yaratılıyordu;
            // oysa yazma OTURUM ORTASINDA da bozulabiliyor (disk dolar) ve o senaryoda uyarı
            // hiç belirmezdi (inceleme bulgusu, codex P1). Görünürlüğü `Ciz()` her karede
            // tazeler; şerit kalıcıdır, süreyle kaybolmaz.
            telemetriUyariKutu = Kutu(kok, new Color(0.55f, 0.10f, 0.10f));
            telemetriUyariEt = Yazi(telemetriUyariKutu, "", 12, Color.white);
            telemetriUyariEt.style.whiteSpace = WhiteSpace.Normal;
            telemetriUyariKutu.style.display = DisplayStyle.None;

            // ---- üst: skor + saat
            var ust = Kutu(kok, Panel);
            skorEt = Yazi(ust, "", 30, Metin); skorEt.style.unityTextAlign = TextAnchor.MiddleCenter;
            saatEt = Yazi(ust, "", 13, Sonuk); saatEt.style.unityTextAlign = TextAnchor.MiddleCenter;

            // ---- canlı üç sonuçlu kazanma şeridi
            var seritKutu = Kutu(kok, Panel);
            Yazi(seritKutu, "KAZANMA OLASILIĞI", 10, Sonuk);
            var serit = new VisualElement();
            serit.style.flexDirection = FlexDirection.Row;
            serit.style.height = 22;
            serit.style.marginTop = 3; serit.style.marginBottom = 3;
            seritEv = SeritParca(serit, new Color(0.30f, 0.72f, 0.45f));   // Galibiyet
            seritBe = SeritParca(serit, new Color(0.55f, 0.57f, 0.62f));   // Beraberlik
            seritDep = SeritParca(serit, new Color(0.80f, 0.36f, 0.34f));  // Mağlubiyet
            seritKutu.Add(serit);
            seritEt = Yazi(seritKutu, "", 12, Metin);
            taktikEtkiEt = Yazi(seritKutu, "", 10, new Color(0.55f, 0.80f, 1f));

            // ---- dikey saha (placeholder: renkli şekiller)
            saha = new VisualElement();
            saha.style.flexGrow = 1;
            saha.style.minHeight = 260;
            saha.style.backgroundColor = Cim;
            saha.style.marginTop = 6; saha.style.marginBottom = 6;
            saha.style.position = Position.Relative;
            saha.style.overflow = Overflow.Hidden;
            kok.Add(saha);
            SahaCizgileri(saha);
            for (int i = 0; i < 22; i++) nokta[i] = Nokta(saha, 10, Color.white);
            topNokta = Nokta(saha, 7, new Color(1f, 0.85f, 0.3f));

            // Duraklama katmanı — sahanın üstünde, K1 kararının GÖRÜNÜR karşılığı.
            duraklamaKatman = new VisualElement();
            duraklamaKatman.style.position = Position.Absolute;
            duraklamaKatman.style.left = 0; duraklamaKatman.style.right = 0;
            duraklamaKatman.style.top = 0; duraklamaKatman.style.bottom = 0;
            duraklamaKatman.style.backgroundColor = new Color(0.05f, 0.06f, 0.09f, 0.72f);
            duraklamaKatman.style.justifyContent = Justify.Center;
            duraklamaKatman.style.display = DisplayStyle.None;
            duraklamaEt = Yazi(duraklamaKatman, "", 18, new Color(1f, 0.88f, 0.45f));
            duraklamaEt.style.unityTextAlign = TextAnchor.MiddleCenter;
            duraklamaEt.style.whiteSpace = WhiteSpace.Normal;
            saha.Add(duraklamaKatman);

            // Maç sonu katmanı
            bitisKatman = new VisualElement();
            bitisKatman.style.position = Position.Absolute;
            bitisKatman.style.left = 0; bitisKatman.style.right = 0;
            bitisKatman.style.top = 0; bitisKatman.style.bottom = 0;
            bitisKatman.style.backgroundColor = new Color(0.04f, 0.05f, 0.07f, 0.92f);
            bitisKatman.style.justifyContent = Justify.Center;
            bitisKatman.style.alignItems = Align.Center;
            bitisKatman.style.display = DisplayStyle.None;
            bitisEt = Yazi(bitisKatman, "", 15, Metin);
            bitisEt.style.unityTextAlign = TextAnchor.MiddleCenter;
            bitisEt.style.whiteSpace = WhiteSpace.Normal;
            // "BİR MAÇ DAHA" — gözlem turunun ÖLÇTÜĞÜ sinyal bu. Maç ≈ 6 dk, kişi başı ≥15 dk
            // serbest oynama isteniyor; her maç arası Play modunu yeniden başlatmak turu
            // bozardı. Brifin madde listesinde yok, kapının amacında var (raporda işaretli).
            var yeni = Dugme(bitisKatman, "BİR MAÇ DAHA", () => { macNo++; MacBaslat(); }, "bir-mac-daha");
            yeni.style.marginTop = 14;
            saha.Add(bitisKatman);

            // ---- spiker akışı
            var spiker = Kutu(kok, Panel);
            Yazi(spiker, "SPİKER", 10, Sonuk);
            for (int i = 0; i < spikerSatir.Length; i++)
            {
                spikerSatir[i] = Yazi(spiker, "", 11, Sonuk);
                spikerSatir[i].style.display = i < ayarlar.spikerSatirSayisi ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // ---- taktik dört kadran (TEK KAPI'dan geçer)
            var taktik = Kutu(kok, Panel);
            Yazi(taktik, $"TAKTİK  ({ayarlar.kadranMin:+0;-0;0} … {ayarlar.kadranMax:+0;-0;0})", 10, Sonuk);
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                var satir = new VisualElement();
                satir.style.flexDirection = FlexDirection.Row;
                satir.style.alignItems = Align.Center;
                satir.style.marginTop = 2;
                var ad = Yazi(satir, KadranAd[i], 11, Metin);
                ad.style.width = 92;
                Dugme(satir, "−", () => KadranOynat(idx, -1), $"kadran-{idx}-eksi");
                kadranDeger[i] = Yazi(satir, "0", 12, new Color(0.55f, 0.80f, 1f));
                kadranDeger[i].style.width = 150;
                kadranDeger[i].style.unityTextAlign = TextAnchor.MiddleCenter;
                Dugme(satir, "+", () => KadranOynat(idx, +1), $"kadran-{idx}-arti");
                taktik.Add(satir);
            }
            var bantSatir = new VisualElement();
            bantSatir.style.flexDirection = FlexDirection.Row;
            bantSatir.style.marginTop = 4;
            Dugme(bantSatir, $"bant dışı dene ({ayarlar.bantDisiTestDegeri:+0;-0;0})", BantDisiDene, "bant-disi-dene");
            taktik.Add(bantSatir);

            redEt = Yazi(taktik, "", 11, new Color(1f, 0.55f, 0.45f));
            redEt.style.whiteSpace = WhiteSpace.Normal;
            redEt.style.display = DisplayStyle.None;

            // ---- alt: hız + sayaçlar
            var alt = Kutu(kok, Panel);
            var hizSatir = new VisualElement();
            hizSatir.style.flexDirection = FlexDirection.Row;
            hizDugme = new Button[3];
            hizDugme[0] = Dugme(hizSatir, "1x", () => HizSec(Hiz.Bir), "hiz-1x");
            hizDugme[1] = Dugme(hizSatir, "2x", () => HizSec(Hiz.Iki), "hiz-2x");
            hizDugme[2] = Dugme(hizSatir, "ATLA", () => HizSec(Hiz.Atla), "hiz-atla");
            alt.Add(hizSatir);
            sayaclarEt = Yazi(alt, "", 10, Sonuk);
            sayaclarEt.style.whiteSpace = WhiteSpace.Normal;
            HizSec(Hiz.Bir, kullaniciEylemi: false);
        }

        /// <summary>`kullaniciEylemi=false` YALNIZ otomatik sıfırlama içindir (maç başlangıcı).
        /// Sıkılma metriği (< 3/maç) hız değişimlerini sayıyor; `MacBaslat`ın 1x'e dönüşünü
        /// kullanıcı eylemi gibi yazmak her maça SAHTE bir sıkılma sinyali ekliyordu —
        /// commit'lenen örnekte `match_start match=1`in hemen ardından `speed_set hiz=Bir
        /// tick=0` görünüyordu (inceleme bulgusu, codex P1).</summary>
        void HizSec(Hiz h, bool kullaniciEylemi = true)
        {
            if (kullaniciEylemi && hiz != h && kosucu != null)
                telemetri?.HizDegisti(macNo, h.ToString(), kosucu.Tick);
            hiz = h;
            for (int i = 0; i < hizDugme.Length; i++)
                hizDugme[i].style.backgroundColor = (int)h == i
                    ? new Color(0.20f, 0.42f, 0.62f) : new Color(0.18f, 0.20f, 0.24f);
        }

        // ---- küçük yardımcılar (UXML/USS yok — K2: arayüz C#'ta kurulabilir)

        static VisualElement Kutu(VisualElement ebeveyn, Color arka)
        {
            var v = new VisualElement();
            v.style.backgroundColor = arka;
            v.style.paddingLeft = 8; v.style.paddingRight = 8;
            v.style.paddingTop = 6; v.style.paddingBottom = 6;
            v.style.marginBottom = 4;
            ebeveyn.Add(v);
            return v;
        }

        static Label Yazi(VisualElement ebeveyn, string metin, int boyut, Color renk)
        {
            var l = new Label(metin);
            l.style.fontSize = boyut;
            l.style.color = renk;
            ebeveyn.Add(l);
            return l;
        }

        /// <summary>`ad` VERİLİR: öğeler adlandırılmış olmadan ne UXML/USS ile stillenebilir
        /// ne de otomatik bir kayıtta sürülebilir (DoD-G 3: şeridin müdahalede oynadığı an
        /// görünmeli — o kaydı almak için düğmeye programla basmak gerekiyor).</summary>
        static Button Dugme(VisualElement ebeveyn, string metin, Action tikla, string ad = null)
        {
            var b = new Button(() => tikla()) { text = metin };
            if (ad != null) b.name = ad;
            b.style.height = 30;
            b.style.minWidth = 46;
            b.style.marginRight = 4;
            b.style.backgroundColor = new Color(0.18f, 0.20f, 0.24f);
            b.style.color = Metin;
            b.style.borderTopWidth = 0; b.style.borderBottomWidth = 0;
            b.style.borderLeftWidth = 0; b.style.borderRightWidth = 0;
            ebeveyn.Add(b);
            return b;
        }

        static VisualElement SeritParca(VisualElement ebeveyn, Color renk)
        {
            var v = new VisualElement();
            v.style.backgroundColor = renk;
            v.style.flexGrow = 1f / 3f;
            ebeveyn.Add(v);
            return v;
        }

        static VisualElement Nokta(VisualElement ebeveyn, int cap, Color renk)
        {
            var v = new VisualElement();
            v.style.position = Position.Absolute;
            v.style.width = cap; v.style.height = cap;
            v.style.marginLeft = -cap / 2f; v.style.marginTop = -cap / 2f;
            v.style.backgroundColor = renk;
            v.style.borderTopLeftRadius = cap / 2f; v.style.borderTopRightRadius = cap / 2f;
            v.style.borderBottomLeftRadius = cap / 2f; v.style.borderBottomRightRadius = cap / 2f;
            ebeveyn.Add(v);
            return v;
        }

        /// <summary>Orta çizgi + iki ceza sahası. YÖN İPUCU: ev takımı YUKARI hücum eder;
        /// hangi kalenin bizim olduğunu göstermeyen bir dikey saha, oyuncuyu şeridin hangi
        /// yöne kaydığını okumaktan alıkoyar.</summary>
        static void SahaCizgileri(VisualElement saha)
        {
            var renk = new Color(0.8f, 0.9f, 0.8f, 0.30f);

            var orta = new VisualElement();
            orta.style.position = Position.Absolute;
            orta.style.left = 0; orta.style.right = 0;
            orta.style.top = new Length(50, LengthUnit.Percent);
            orta.style.height = 1;
            orta.style.backgroundColor = renk;
            saha.Add(orta);

            // Ceza sahası ≈ 16,5 m × 40,3 m → sahanın %15,7 boyu, %59,3 eni (ME saha ölçüsü
            // 105×68 m; oranlar oradan türetildi, ekrana göre uydurulmadı).
            void CezaSahasi(bool ust)
            {
                var k = new VisualElement();
                k.style.position = Position.Absolute;
                k.style.left = new Length(20.35f, LengthUnit.Percent);
                k.style.width = new Length(59.3f, LengthUnit.Percent);
                k.style.height = new Length(15.7f, LengthUnit.Percent);
                if (ust) k.style.top = 0; else k.style.bottom = 0;
                k.style.borderTopWidth = 1; k.style.borderBottomWidth = 1;
                k.style.borderLeftWidth = 1; k.style.borderRightWidth = 1;
                k.style.borderTopColor = renk; k.style.borderBottomColor = renk;
                k.style.borderLeftColor = renk; k.style.borderRightColor = renk;
                saha.Add(k);
            }
            CezaSahasi(true);      // rakip kalesi — ev takımı buraya hücum eder
            CezaSahasi(false);     // kendi kalemiz
        }
    }
}
