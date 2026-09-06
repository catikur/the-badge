using System;
using System.IO;
using TheBadge.Greybox.Sim;
using UnityEngine;

namespace TheBadge.Match
{
    /// <summary>PLAYTEST TELEMETRİSİ — olay kümesi TEK YERDE (TASK-003 Kural 4).
    ///
    /// Ekran olay adı ya da alan adı BİLMEZ; yalnız bu sınıfın metotlarını çağırır. Sonraki
    /// turda şema değişirse değişen tek dosya burasıdır.
    ///
    /// YAZICI YENİDEN YAZILMADI (TASK-003 Kural 2): `TheBadge.Greybox.Sim.TelemetryLog`
    /// arşivden çıkarılıp `Assets/Services/`e taşındı ve olduğu gibi kullanılıyor — 88 satır,
    /// UnityEngine'siz, her satırda flush. Buradaki iş yazıcı değil, SORULARDAN TÜRETİLEN
    /// olay kümesi.
    ///
    /// PII YOK (Kural 1): oturum kimliği rastgele bir dizedir, takma ad bile yazılmaz. Mülakat
    /// formuyla eşleşme bu kimlik üzerinden yapılır.
    ///
    /// TERK SİNYALİ TERMİNAL OLAYA BAĞLI DEĞİL: `SeansBitti` yazılır ama yalnız TEMİZ ÇIKIŞ
    /// işaretidir. Oyuncu uygulamayı öldürürse o satır hiç yazılmaz — tam da terk senaryosunda.
    /// Terk ÇIKARSANIR: son satırı `session_end` olmayan oturum, ya da eşleşen `match_end`i
    /// olmayan bir `match_start`. `ilerleme` satırları "nerede bıraktı"yı taşır.</summary>
    public sealed class MacTelemetri : IDisposable
    {
        // ---- OLAY ADLARI (tek kaynak) ----
        const string OMacBasladi = "match_start";
        const string OMacBitti = "match_end";
        const string OHiz = "speed_set";
        const string OKritikAn = "kritik_an";
        const string OMudahale = "mudahale";
        const string OTaktikUygulandi = "taktik_uygulandi";
        const string OIlerleme = "ilerleme";
        const string OFaz = "faz";
        const string OSeansBitti = "session_end";

        /// <summary>Telemetri yazılamadığında bırakılan kalıcı işaret (Kural 3). Ekrandaki
        /// uyarı odadaki gözlemci içindir; bu bayrak SONRADAN bakan için.</summary>
        public const string HataBayragiAnahtari = "thebadge_telemetri_hatasi";

        readonly TelemetryLog log;

        /// <summary>Yazıcı kuruldu mu. `false` ise ekran BANT DIŞI uyarı gösterir — hatayı
        /// `session_end`e yazmak aynı bozuk yazıcıyı kullanmak olurdu ve dosya yalnızca EKSİK
        /// görünürdü; ölçümün başarısız olduğuna dair hiçbir iz kalmazdı (Kural 3).</summary>
        public bool Calisiyor => log != null;
        public string HataMesaji { get; private set; }
        public string DosyaYolu { get; private set; }
        public string SeansId { get; private set; }

        MacTelemetri(TelemetryLog log, string seansId, string yol)
        { this.log = log; SeansId = seansId; DosyaYolu = yol; }

        MacTelemetri(string hata) { HataMesaji = hata; }

        /// <summary>Kurar; BAŞARISIZ OLURSA İSTİSNA ATMAZ — telemetri oynanışı düşürmez
        /// (Kural 3). Ama sessizce de geçmez: `Calisiyor` false döner, `HataMesaji` dolar ve
        /// kalıcı bayrak bırakılır.</summary>
        public static MacTelemetri Kur(string dizin, string uygulamaSurumu, string cihaz)
        {
            // PII YOK: rastgele kimlik. `Guid` burada rastgelelik için kullanılıyor ve
            // SİMÜLASYONA GİRMEZ — telemetri maç sonucuna dokunmaz.
            string seansId = Guid.NewGuid().ToString("N").Substring(0, 12);
            try
            {
                var l = new TelemetryLog(dizin, seansId, uygulamaSurumu, cihaz);
                PlayerPrefs.DeleteKey(HataBayragiAnahtari);
                return new MacTelemetri(l, seansId, l.FilePath);
            }
            catch (Exception e)
            {
                string mesaj = e.GetType().Name + ": " + e.Message;
                // KALICI İŞARET — iki kanal, çünkü biri tam da bu senaryoda çalışmayabilir.
                // `PlayerPrefs` dizin yazılamasa da çalışır; yanına dosya bırakmayı da DENERİZ
                // ama başarısızlığı yutarız (zaten yazamadığımız için buradayız).
                PlayerPrefs.SetString(HataBayragiAnahtari, mesaj);
                PlayerPrefs.Save();
                try { File.WriteAllText(Path.Combine(dizin, "telemetry_error.txt"), mesaj); }
                catch { /* beklenen: dizin zaten yazılamıyor */ }
                Debug.LogWarning("[TASK-003] TELEMETRİ YAZILAMIYOR — " + mesaj);
                return new MacTelemetri(mesaj);
            }
        }

        // ------------------------------------------------------------------ OLAYLAR
        // Her metodun karşılığı TASK-003'ün "turun cevaplaması gereken soru" tablosundadır.

        /// <summary>Kapı metriği 1 ("bir maç daha"): `match_start` / `match_end` çiftleri
        /// ve zaman damgaları. Tohum da yazılır ki bir oturum yeniden üretilebilsin.</summary>
        public void MacBasladi(int macNo, ulong tohum)
        {
            if (log == null) return;
            log.Event(OMacBasladi).Num("match", macNo).Str("seed", tohum.ToString()).Send();
        }

        /// <summary>`tamamlandi=false` → oyuncu maçı bitirmeden yeni maça geçti/bıraktı.
        /// `izlemeSn` GERÇEK zaman (kapı metriği: maç başına izleme süresi).</summary>
        public void MacBitti(int macNo, int evGol, int depGol, double izlemeSn,
                             int duraklama, int mudahale, int busRed, int motorRed, bool tamamlandi)
        {
            if (log == null) return;
            log.Event(OMacBitti)
               .Num("match", macNo).Str("score", evGol + "-" + depGol)
               .Num("watch_real_sec", izlemeSn)
               .Num("duraklama", duraklama).Num("mudahale", mudahale)
               .Num("bus_red", busRed).Num("motor_red", motorRed)
               .Num("tamamlandi", tamamlandi ? 1 : 0)
               .Send();
        }

        /// <summary>Kapı metriği 2 (sıkılma işareti): hız değişimi. "atla" bir SKIP sinyalidir.</summary>
        public void HizDegisti(int macNo, string hiz, uint tick)
        {
            if (log == null) return;
            log.Event(OHiz).Num("match", macNo).Str("hiz", hiz).Num("tick", tick).Send();
        }

        /// <summary>Duraklama ANI. `mudahale` ile birlikte okunduğunda turun EN KRİTİK oranını
        /// verir: duraklamaların kaçında oyuncu bir şey yaptı, kaçında geçip gitti. Greybox'ta
        /// bu ölçülmemişti ve %40'ın nedenini bilmememizin sebeplerinden biri buydu.</summary>
        public void KritikAn(int macNo, int sira, double sicrama, uint tick, double ev)
        {
            if (log == null) return;
            log.Event(OKritikAn).Num("match", macNo).Num("sira", sira)
               .Num("sicrama", sicrama).Num("tick", tick).Num("ev", ev).Send();
        }

        /// <summary>Müdahale GÖNDERİMİ. `duraklamada` alanı `kritik_an` ile eşleştirmeyi
        /// tek satırdan okunur kılar. Red hâlinde sebep de yazılır — iki red yolu (bus/motor)
        /// ekranda ayrıldığı gibi logda da ayrılır.</summary>
        public void Mudahale(int macNo, uint tick, int mentalite, int tempo, int pres, int hat,
                             bool kabul, string redSebebi, bool duraklamada, double evOnce)
        {
            if (log == null) return;
            log.Event(OMudahale).Num("match", macNo).Num("tick", tick)
               .Num("mentalite", mentalite).Num("tempo", tempo).Num("pres", pres).Num("hat", hat)
               .Num("kabul", kabul ? 1 : 0).Str("red", redSebebi ?? "")
               .Num("duraklamada", duraklamada ? 1 : 0)
               .Num("ev_once", evOnce)
               .Send();
        }

        /// <summary>Şeridin HAREKETİ. `mudahale`den AYRI bir olay, çünkü gönderim anında
        /// "sonra" değeri HENÜZ YOKTUR: motor komutu ME 14.2'nin güvenli anında uygular.
        /// İkisini tek satıra sıkıştırmak, ölçülmemiş bir sayıyı ölçülmüş gibi yazmak olurdu.
        /// `ev_once`/`ev_sonra` aynı tick'in iki yanıdır.</summary>
        public void TaktikUygulandi(int macNo, uint tick, double evOnce, double evSonra)
        {
            if (log == null) return;
            log.Event(OTaktikUygulandi).Num("match", macNo).Num("tick", tick)
               .Num("ev_once", evOnce).Num("ev_sonra", evSonra)
               .Num("fark", evSonra - evOnce).Send();
        }

        /// <summary>"NEREDE BIRAKTI" — düzenli aralıklarla düşen ilerleme satırı. Terk
        /// terminal bir olaya bağlanamadığı için (uygulama öldürülünce yazılmaz) konum
        /// SON YAZILAN satırdan okunur; bu olay o çözünürlüğü sağlar.</summary>
        public void Ilerleme(int macNo, uint tick, int evGol, int depGol, byte devre, double izlemeSn)
        {
            if (log == null) return;
            log.Event(OIlerleme).Num("match", macNo).Num("tick", tick)
               .Str("score", evGol + "-" + depGol).Num("devre", devre)
               .Num("watch_real_sec", izlemeSn).Send();
        }

        /// <summary>Faz/ekran geçişi — OLDUĞU ANDA yazılır (devre arası, maç sonu ekranı...).</summary>
        public void Faz(int macNo, string ad, uint tick)
        {
            if (log == null) return;
            log.Event(OFaz).Num("match", macNo).Str("ad", ad).Num("tick", tick).Send();
        }

        /// <summary>TEMİZ ÇIKIŞ işareti — bağımlılık DEĞİL. Yokluğu terk demektir.</summary>
        public void SeansBitti(int toplamMac)
        {
            if (log == null) return;
            log.Event(OSeansBitti).Num("mac_sayisi", toplamMac).Send();
        }

        public void Dispose() => log?.Dispose();
    }
}
