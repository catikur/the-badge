using System;
using System.Globalization;
using System.Threading;
using NUnit.Framework;
using TheBadge.Match;

namespace TheBadge.Match.Tests
{
    /// <summary>BALANCE OKUMA — ekranın ayarlanabilir sayıları GERÇEKTEN dosyadan geliyor mu.
    /// Kural 4'ün (magic number yok) ölçülebilir yarısı budur: bant tablosu okunamıyorsa
    /// komutlar sessizce `ParamOutOfBand`a düşer ve sebep yanlış yerde aranır.</summary>
    public sealed class BalansOkumaTests
    {
        [Test]
        public void BantTablosuDosyadanOkunuyor()
        {
            var b = BalansKaynagi.Yukle();
            Assert.Greater(b.Bantlar.Sayi, 0, "bant tablosu boş");

            // Taktik dört kadranının bandı katalogla aynı olmalı (CB 4.2: delta [-2, +2]).
            foreach (string anahtar in new[] { "squad.mentalite", "squad.tempo", "squad.pres", "squad.hat" })
            {
                Assert.IsTrue(b.Bantlar.TryGetBand(anahtar, out double min, out double max),
                    "bant bulunamadı: " + anahtar);
                Assert.AreEqual(-2d, min, 1e-9, anahtar + " alt sınırı");
                Assert.AreEqual(2d, max, 1e-9, anahtar + " üst sınırı");
            }

            Assert.IsFalse(b.Bantlar.TryGetBand("yok.boyle.bir.bant", out _, out _),
                "olmayan bant için true döndü — sessiz geçiş");
        }

        [Test]
        public void HizSiniriVeDunyaKurallariOkunuyor()
        {
            var b = BalansKaynagi.Yukle();
            Assert.IsTrue(b.HizSiniri.ContainsKey(TheBadge.CommandBus.RateClass.Tactic),
                "Tactic hız sınıfı okunmadı");
            Assert.Greater(b.DunyaKurallari.taktik.adim, 0, "world.balance taktik.adim okunmadı");
            Assert.Greater(b.Sim.canliOlasilik.kritikAnEsigi, 0d,
                "sim.balance canliOlasilik.kritikAnEsigi okunmadı — kritik an dedektörü çalışmaz");
        }

        [Test]
        public void Json_OndalikAyraciKulturdenEtkilenmez()
        {
            // TÜRKÇE YERELDE ondalık ayracı virgüldür; `double.Parse` varsayılanı "0.04"ü
            // 4 diye okurdu ve kritik an eşiği 100 katına çıkardı — maç boyunca sıfır
            // duraklama. Bu, sessizce yanlış çalışan bir ekran olurdu.
            var onceki = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
                var kok = BasitJson.Ayristir("{\"a\": 0.04, \"b\": [-2, 2], \"c\": 1e-3}");
                Assert.AreEqual(0.04d, BasitJson.Sayi(BasitJson.Yol(kok, "a")), 1e-12);
                Assert.AreEqual(0.001d, BasitJson.Sayi(BasitJson.Yol(kok, "c")), 1e-12);
                var d = BasitJson.Dizi(BasitJson.Yol(kok, "b"));
                Assert.AreEqual(-2d, BasitJson.Sayi(d[0]), 1e-12);
            }
            finally { Thread.CurrentThread.CurrentCulture = onceki; }
        }

        [Test]
        public void Json_BozukGirdiSessizGecmez()
        {
            Assert.Throws<FormatException>(() => BasitJson.Ayristir("{\"a\": 1,}"));
            Assert.Throws<FormatException>(() => BasitJson.Ayristir("{\"a\": 1} artık"));
            // Tekrarlı anahtar: hangi tanımın geçerli olduğunu dosya SIRASINA bırakmak,
            // bir bandın sessizce başkasıyla değişmesi demekti.
            Assert.Throws<FormatException>(() => BasitJson.Ayristir("{\"a\": 1, \"a\": 2}"));
        }

        [Test]
        public void Ayarlar_BantDisiTestDegeriGercektenBantDisi()
        {
            var a = new MacSunumAyarlari();
            a.Dogrula();                                   // varsayılanlar tutarlı olmalı

            // Bandın İÇİNDE bir "bant dışı test değeri" kabul ölçütünü sessizce boşa çıkarırdı:
            // düğmeye basılır, komut geçer ve "red yolu denendi" sanılır.
            a.bantDisiTestDegeri = 1;
            Assert.Throws<ArgumentException>(() => a.Dogrula());
        }
    }
}
