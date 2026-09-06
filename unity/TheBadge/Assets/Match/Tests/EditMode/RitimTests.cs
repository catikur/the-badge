using System.Text;
using NUnit.Framework;
using TheBadge.Match;

namespace TheBadge.Match.Tests
{
    /// <summary>DURAKLAMA RİTMİ — sunum tarafının K1 kararına dayanan iddiaları.
    ///
    /// İŞ BÖLÜMÜ: motorun ritim iddiası (eşik 0,04 → 9,9 an/maç, %0 boş maç) `Sim.Checks`teki
    /// `S2KritikAnRitmi`nindir ve buraya KOPYALANMAZ. Burada ölçülen iki şey SUNUMUN kendi işi:
    ///
    ///   1. HİÇBİR MAÇ BOŞ DEĞİL — ekran bu özelliğe dayanıyor. Sıfır duraklamalı bir maç
    ///      "kritik anlarda dur" tasarımını sessizce hiçbir şeye çevirirdi.
    ///   2. ÖRNEKLEME KADANSI SEÇİMİ — dedektörü ne sıklıkla çağıracağı brifte SUNUMA
    ///      bırakılmış bir karar (`kritikAnOrneklemeTick`). Kadans bağımsızlığı K1'de motor
    ///      için ölçüldü; burada BENİM çağrı desenimde de korunduğu doğrulanır.
    ///
    /// TOHUMLAR SABİT: bu testler rastgele değil, deterministiktir — aynı tohumlar her koşuda
    /// aynı sayıyı verir. Savrulan bir kapı olmadıkları için sayıları raporlanabilir.</summary>
    public sealed class RitimTests
    {
        const int OrneklemeTick = 30;      // 3 maç saniyesi — K1 ölçümünün kadansı
        const ulong TohumTaban = 20260906UL;

        static int DuraklamaSayisi(BalansKaynagi b, ulong tohum, int ornekleme)
        {
            var k = new MacKosucu(b.Sim, tohum, ornekleme);
            int n = 0;
            while (!k.Bitti && n++ < 200_000) k.Ilerlet(1);
            Assert.IsTrue(k.Bitti, "maç emniyet tavanında bitmedi (tohum " + tohum + ")");
            return k.KritikAnSayisi;
        }

        [Test]
        public void HicbirMacBosKalmaz()
        {
            var b = BalansKaynagi.Yukle();
            const int N = 24;
            int toplam = 0, bos = 0, enAz = int.MaxValue, enCok = 0;
            var dagilim = new StringBuilder();

            for (int i = 0; i < N; i++)
            {
                int d = DuraklamaSayisi(b, TohumTaban + (ulong)i, OrneklemeTick);
                toplam += d;
                if (d == 0) bos++;
                if (d < enAz) enAz = d;
                if (d > enCok) enCok = d;
                dagilim.Append(d).Append(' ');
            }
            double ortalama = (double)toplam / N;
            UnityEngine.Debug.Log($"[TASK-002] {N} maç · ortalama {ortalama:0.00} duraklama · "
                                + $"aralık {enAz}-{enCok} · boş maç {bos}\n dağılım: {dagilim}");

            Assert.AreEqual(0, bos, "sıfır duraklamalı maç çıktı — ekranın ritmi o maçta hiç yok");

            // GENİŞ BANT, BİLEREK: dar bandı `S2KritikAnRitmi` tutuyor ve o 200 maç üzerinden
            // ölçüyor. Buradaki 24 maçlık ölçümün işi motoru yeniden ölçmek DEĞİL, sunum
            // tarafında ritmin tamamen kaydığını (ör. eşik yanlış okundu, örnekleme bozuldu)
            // yakalamak. Bugünkü değer 11,04 — kayıt için.
            Assert.That(ortalama, Is.InRange(6.0, 15.0),
                $"duraklama ritmi tanınmayacak kadar kaymış (ortalama {ortalama:0.00})");
        }

        [Test]
        public void OrneklemeKadansindanBagimsiz()
        {
            // K1'in iddiası: dedektörün tabanı YALNIZ ateşlendiğinde sıfırlanır, o yüzden sık
            // örnekleme aynı sıçramayı daha ERKEN yakalar, DAHA ÇOK değil. Bu bir tasarım
            // iddiasıdır ve ölçülmeden kabul edilmez — sunum kare hızını serbestçe seçebiliyor
            // olmasının dayanağı tam olarak budur.
            var b = BalansKaynagi.Yukle();
            const int N = 8;
            int[] kadanslar = { 10, 30, 100, 300 };        // 1 sn → 30 sn, 30 kat aralık
            double enKucuk = double.MaxValue, enBuyuk = 0;

            foreach (int ornekleme in kadanslar)
            {
                int toplam = 0;
                for (int i = 0; i < N; i++) toplam += DuraklamaSayisi(b, TohumTaban + (ulong)i, ornekleme);
                double ort = (double)toplam / N;
                UnityEngine.Debug.Log($"[TASK-002] örnekleme {ornekleme} tick ({ornekleme / 10.0:0.0} sn) "
                                    + $"→ ortalama {ort:0.00} duraklama/maç");
                if (ort < enKucuk) enKucuk = ort;
                if (ort > enBuyuk) enBuyuk = ort;
            }

            // 30 KAT kadans aralığında sayı en fazla %25 oynamalı. Bugünkü fark %6.
            Assert.That(enBuyuk / enKucuk, Is.LessThan(1.25),
                $"kadans bağımsızlığı bozuldu: {enKucuk:0.00} ↔ {enBuyuk:0.00}");
        }
    }
}
