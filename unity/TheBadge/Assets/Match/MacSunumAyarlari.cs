using System;

namespace TheBadge.Match
{
    /// <summary>MAÇ SUNUM EKRANININ TÜM AYARLANABİLİR SAYILARI — TASK-002 Kural 4'ün karşılığı:
    /// koda gömülü magic number yok, hepsi BURADA toplanır ve **[KALİBRE] adayıdır**.
    ///
    /// NEDEN `balance/sim.balance.json` DEĞİL: bu sayılar simülasyonu DEĞİL sunumu ayarlar.
    /// `sim.balance.json` config_hash kapsamındadır (CLAUDE.md değişmez #4) ve sezon içinde
    /// donuktur; kare hızı ya da spiker satır sayısı oraya girerse determinizm kimliğini
    /// gereksiz yere kirletir — ekran ayarı değişince replay kimliği bozulmamalı. Brif de
    /// "tek bir yerde toplanır ve [KALİBRE] ADAYI olarak işaretlenir" diyor: aday, henüz
    /// balance dosyası değil. Kalıcı yer mülakatlı gözlem turundan SONRA seçilir (o tur bu
    /// sayıların hangilerinin gerçekten ayar istediğini ölçecek).
    ///
    /// SİMÜLASYON TARAFININ SAYISI BURAYA GİRMEZ: kritik an eşiği `sim.balance.json`daki
    /// `canliOlasilik.kritikAnEsigi`dir ve oradan OKUNUR (bkz. `MacKosucu`) — kopyası yok.</summary>
    [Serializable]
    public sealed class MacSunumAyarlari
    {
        /// <summary>[KALİBRE adayı] 1x hızda saniyede kaç motor tick'i işlenir.
        ///
        /// TÜRETİLDİ, SEÇİLMEDİ: GAME_THESIS Session Shape "maç 5-8 dk" diyor. Bir maç
        /// 2×27.000 tick (ME 3.4: `HalfTicks = 45×60×10`) + uzatma ≈ 57.000 tick. 150 tick/sn
        /// → ≈ 6,3 dk, bandın ortası. Motorun kendi gerçek zamanı 10 tick/sn'dir (TickMs=100),
        /// yani 1x burada "gerçek zaman" DEĞİL "tezin oturum şekli"dir; motor test sahnesi
        /// (EngineDev) gerçek zamanlı koşmaya devam eder.
        ///
        /// Kritik an duraklamaları bu akışı 8-12 bloğa bölen şeydir (K1 kararı); yani sayı tek
        /// başına değil, `duraklamaSaniye` ile birlikte ritmi belirler.</summary>
        public int tickHizi1x = 150;

        /// <summary>[KALİBRE adayı] 2x çarpanı. Brif hız kontrolünü 1x / 2x / atla diye veriyor.</summary>
        public int hizCarpani2x = 2;

        /// <summary>[KALİBRE adayı] "Atla" modunda kare başına en çok kaç tick işlenir.
        /// Kare başına tavan olmadan maçın kalanı tek karede koşar ve editör donar; bu sayı
        /// atlamayı GÖRÜNÜR (ilerleyen bir çubuk) tutar.</summary>
        public int atlaTickTavani = 4000;

        /// <summary>[KALİBRE adayı] Kritik an dedektörünün örnekleme aralığı (motor tick'i).
        /// 30 tick = 3 maç saniyesi — K1 ölçümünün kadansı (DECISIONS 2026-09-05). Dedektör
        /// kadanstan BAĞIMSIZ ölçüldü (1 sn ↔ 30 sn arasında 10,0 ↔ 9,5), yani bu sayı ritmi
        /// değiştirmez; ölçümle aynı kadansta kalmak sadece raporu karşılaştırılabilir yapar.</summary>
        public int kritikAnOrneklemeTick = 30;

        /// <summary>[KALİBRE adayı] Kritik an duraklamasının gerçek zamanda süresi (sn).
        /// Duraklama motoru DURDURMAZ, sunumu durdurur: tick işlenmez, yani maç sonucu
        /// etkilenmez (Kural 6).</summary>
        public float duraklamaSaniye = 1.6f;

        /// <summary>[KALİBRE adayı] Spiker akışında görünen en fazla satır.</summary>
        public int spikerSatirSayisi = 6;

        /// <summary>[KALİBRE adayı] Şeridin yeni değere yürüme süresi (sn). 0 = anında.
        /// DİKKAT: bu YALNIZ görsel yumuşatmadır. Kabul ölçütü "taktik değişikliği şeridi
        /// ANINDA oynatıyor (aynı tick)" diyor — ölçülen büyüklük `AnlikOlasilik`ın kendisidir
        /// ve o aynı tick'te değişir; animasyon onu geciktirmez, yalnız çizer. Bu ayrımı
        /// görünür tutmak için ekran şeridin HAM hedef değerini de yazar.</summary>
        public float seritAnimasyonSaniye = 0.25f;

        /// <summary>[KALİBRE adayı] Red mesajının ekranda kalma süresi (sn). İki red yolu da
        /// (bus reddi + motor geç reddi, Kural 2) bu süre boyunca görünür.</summary>
        public float redMesajiSaniye = 4f;

        /// <summary>[KALİBRE adayı] Taktik kadranlarının bandı. Katalog `squad.mentalite` vb.
        /// bantları [-2, +2] veriyor (`balance/command.bands.json`); UI bunu AŞMAZ ama band
        /// dışını ELLE denemek kabul ölçütünün bir maddesi (bant dışı delta ile iki red yolu
        /// da denenir), o yüzden ekranda ayrı bir "bant dışı dene" düğmesi var ve o düğme
        /// bilerek `bantDisiTestDegeri`ni gönderir.</summary>
        public int kadranMin = -2, kadranMax = 2;

        /// <summary>[KALİBRE adayı] "Bant dışı dene" düğmesinin gönderdiği değer — bus'ın
        /// `ParamOutOfBand` reddini kullanıcıya SEBEBİYLE göstermek için (Kural 2).</summary>
        public int bantDisiTestDegeri = 5;

        /// <summary>[KALİBRE adayı] Telemetrinin "nerede bıraktı" çözünürlüğü: kaç MAÇ
        /// dakikasında bir `ilerleme` satırı düşsün. Terk terminal bir olaya bağlanamadığı için
        /// (uygulama öldürülünce `session_end` yazılmaz) konum son yazılan satırdan okunur —
        /// bu sayı o okumanın hassasiyetidir. 5 dk → maç başına ~18 satır.</summary>
        public int telemetriIlerlemeDakika = 5;

        /// <summary>TEST KAPISI, normalde BOŞ. Doluysa telemetri bu dizine yazılır.
        /// TASK-003'ün kabul ölçütü "yazılamayan bir dizine yönlendirip denenir ve uyarının
        /// çıktığı raporlanır" diyor; o senaryoyu elle koşturmanın yolu budur. Boş bırakılırsa
        /// `Application.persistentDataPath/telemetry` kullanılır.</summary>
        public string telemetriDiziniGecersizKil = "";

        /// <summary>Doğrulama — sessiz bozuk ayar yok (projenin tekrar eden dersi).</summary>
        public void Dogrula()
        {
            if (tickHizi1x <= 0) throw new ArgumentException("MacSunumAyarlari: tickHizi1x > 0 olmalı.");
            if (hizCarpani2x <= 1) throw new ArgumentException("MacSunumAyarlari: hizCarpani2x > 1 olmalı.");
            if (atlaTickTavani <= 0) throw new ArgumentException("MacSunumAyarlari: atlaTickTavani > 0 olmalı.");
            if (kritikAnOrneklemeTick <= 0) throw new ArgumentException("MacSunumAyarlari: kritikAnOrneklemeTick > 0 olmalı.");
            // 0 SERBEST: "duraklama yok" demektir ve geçerli bir kalibrasyon (kritik anlar yine
            // sayılır, yalnız sunum durmaz). Ekran bu durumda vurgu katmanını hiç AÇMAZ.
            if (duraklamaSaniye < 0) throw new ArgumentException("MacSunumAyarlari: duraklamaSaniye ≥ 0 olmalı.");
            // 0 YASAK — ve bu, `duraklamaSaniye`den bilerek FARKLI: red mesajı 0 saniye
            // görünürse komut reddedilir ama kullanıcı sebebini HİÇ göremez, yani red sessizce
            // yutulmuş olur. CB 11.1 ve TASK-002 Kural 2 bunu açıkça yasaklıyor; bir kalibrasyon
            // değeri anayasa kuralını kapatabiliyorsa o değer serbest değildir
            // (inceleme bulgusu, codex).
            if (redMesajiSaniye <= 0) throw new ArgumentException(
                "MacSunumAyarlari: redMesajiSaniye > 0 olmalı — 0/negatif değer reddi sessizce yutar (CB 11.1).");
            if (spikerSatirSayisi <= 0) throw new ArgumentException("MacSunumAyarlari: spikerSatirSayisi > 0 olmalı.");
            if (seritAnimasyonSaniye < 0) throw new ArgumentException("MacSunumAyarlari: seritAnimasyonSaniye ≥ 0 olmalı.");
            if (kadranMin > kadranMax) throw new ArgumentException("MacSunumAyarlari: kadranMin ≤ kadranMax olmalı.");
            if (bantDisiTestDegeri >= kadranMin && bantDisiTestDegeri <= kadranMax)
                throw new ArgumentException("MacSunumAyarlari: bantDisiTestDegeri BANDIN DIŞINDA olmalı, yoksa red yolu denenmiş olmaz.");
            if (telemetriIlerlemeDakika <= 0) throw new ArgumentException(
                "MacSunumAyarlari: telemetriIlerlemeDakika > 0 olmalı — 0/negatif 'nerede bıraktı' okumasını kör eder.");
        }
    }
}
