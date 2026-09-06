using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TheBadge.Match;

namespace TheBadge.Match.Tests
{
    /// <summary>PLAYTEST TELEMETRİSİ — TASK-003 kabul ölçütlerinin otomatikleştirilebilen kısmı.
    ///
    /// Elle koşulması gereken iki madde burada DEĞİL, raporda: (a) uygulamayı maç ortasında
    /// kapatınca satırların durması, (b) yazılamayan dizinde ekranda uyarının çıkması. İkisi de
    /// Play modu davranışı; burada ölçülen ŞEMA ve DOSYA sözleşmesi.</summary>
    public sealed class TelemetriTests
    {
        string dizin;

        [SetUp]
        public void Kur()
        {
            dizin = Path.Combine(Path.GetTempPath(), "tb_telemetri_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        }

        [TearDown]
        public void Temizle()
        {
            try { if (Directory.Exists(dizin)) Directory.Delete(dizin, true); } catch { }
        }

        /// <summary>Tablodaki her soru için en az bir olay + her satır geçerli JSON.</summary>
        [Test]
        public void TumOlaylarYaziliyor_HerSatirGecerliJson()
        {
            string yol;
            using (var t = MacTelemetri.Kur(dizin, "test-0.1", "harness"))
            {
                Assert.IsTrue(t.Calisiyor, "yazıcı kurulamadı: " + t.HataMesaji);
                yol = t.DosyaYolu;

                t.MacBasladi(0, 20260906UL);
                t.HizDegisti(0, "Iki", 600);
                t.KritikAn(0, 1, 0.048, 5442, 0.474);
                t.Mudahale(0, 5443, 2, 0, -1, 1, true, null, true, 0.41);
                t.TaktikUygulandi(0, 5442, 0.41, 0.473);
                t.Mudahale(0, 6000, 5, 0, 0, 0, false, "ParamOutOfBand", false, 0.47);
                t.Ilerleme(0, 30000, 1, 0, 1, 42.5);
                t.Faz(0, "devre_2", 27000);
                t.MacBitti(0, 1, 1, 380.0, 11, 2, 1, 0, true);
                t.SeansBitti(1);
            }

            var satirlar = File.ReadAllLines(yol).Where(l => l.Length > 0).ToList();
            Assert.Greater(satirlar.Count, 0, "hiç satır yazılmamış");

            // HER SATIR GEÇERLİ JSON (kabul ölçütü) — kendi okuyucumuzla ayrıştırılıyor.
            var tipler = new List<string>();
            foreach (string satir in satirlar)
            {
                object kok = null;
                Assert.DoesNotThrow(() => kok = BasitJson.Ayristir(satir), "geçersiz JSON satırı: " + satir);
                var n = BasitJson.Nesne(kok);
                Assert.IsTrue(n.ContainsKey("t"), "satırda olay tipi yok: " + satir);
                Assert.IsTrue(n.ContainsKey("sid"), "satırda oturum kimliği yok: " + satir);
                Assert.IsTrue(n.ContainsKey("ts"), "satırda zaman damgası yok: " + satir);
                tipler.Add((string)n["t"]);
            }

            // TASK-003 tablosundaki her soru için en az bir olay
            foreach (string beklenen in new[] { "session_start", "match_start", "speed_set",
                                               "kritik_an", "mudahale", "taktik_uygulandi",
                                               "ilerleme", "faz", "match_end", "session_end" })
                Assert.Contains(beklenen, tipler, "olay hiç yazılmamış: " + beklenen);
        }

        [Test]
        public void OturumKimligi_DosyaAdindaVeHerSatirda_PiiYok()
        {
            using (var t = MacTelemetri.Kur(dizin, "test-0.1", "harness"))
            {
                Assert.IsTrue(t.Calisiyor);
                t.MacBasladi(0, 1UL);
                t.Dispose();

                string ad = Path.GetFileName(t.DosyaYolu);
                StringAssert.Contains(t.SeansId, ad, "oturum kimliği dosya adında değil");

                foreach (string satir in File.ReadAllLines(t.DosyaYolu).Where(l => l.Length > 0))
                    StringAssert.Contains(t.SeansId, satir, "satırda oturum kimliği yok");

                // PII YOK: kimlik rastgele bir dize, cihaz/kullanıcı adı taşımıyor.
                Assert.AreEqual(12, t.SeansId.Length, "oturum kimliği beklenen biçimde değil");
                Assert.IsTrue(t.SeansId.All(Uri.IsHexDigit), "oturum kimliği rastgele onaltılık olmalı");
            }
        }

        /// <summary>Kural 3: yazıcı kurulamazsa OYUN DÜŞMEZ ama sessizce de geçmez.</summary>
        [Test]
        public void YazilamayanDizin_OyunuDusurmez_AmaSessizceGecmez()
        {
            // Bir DOSYAyı dizin yolu gibi kullan: `Directory.CreateDirectory` bunda patlar.
            Directory.CreateDirectory(dizin);
            string engel = Path.Combine(dizin, "engel");
            File.WriteAllText(engel, "bu bir dosya, dizin degil");
            string imkansiz = Path.Combine(engel, "telemetry");

            MacTelemetri t = null;
            Assert.DoesNotThrow(() => t = MacTelemetri.Kur(imkansiz, "test-0.1", "harness"),
                "telemetri kurulumu istisna sızdırdı — oyunu düşürürdü (Kural 3)");

            Assert.IsFalse(t.Calisiyor, "yazılamayan dizinde yazıcı çalışıyor göründü");
            Assert.IsNotEmpty(t.HataMesaji ?? "", "hata mesajı boş — sessizce yutulmuş");

            // Kalıcı işaret bırakılmalı ki sonradan bakan da anlasın.
            Assert.IsTrue(UnityEngine.PlayerPrefs.HasKey(MacTelemetri.HataBayragiAnahtari),
                "kalıcı hata bayrağı bırakılmamış");

            // Olay çağrıları da patlamamalı: ekran telemetri yokken de koşmaya devam eder.
            Assert.DoesNotThrow(() =>
            {
                t.MacBasladi(0, 1UL);
                t.KritikAn(0, 1, 0.05, 100, 0.5);
                t.MacBitti(0, 0, 0, 1.0, 0, 0, 0, 0, true);
                t.SeansBitti(1);
                t.Dispose();
            }, "kırık telemetride olay çağrısı patladı");

            UnityEngine.PlayerPrefs.DeleteKey(MacTelemetri.HataBayragiAnahtari);
        }

        /// <summary>Terk ÇIKARSANABİLİR olmalı: `session_end` yoksa ve `match_start`ın eşleşen
        /// `match_end`i yoksa, son `ilerleme` satırı nerede bırakıldığını söyler.</summary>
        [Test]
        public void TerkCikarsanabiliyor_TerminalOlayaGerekYok()
        {
            string yol;
            var t = MacTelemetri.Kur(dizin, "test-0.1", "harness");
            Assert.IsTrue(t.Calisiyor);
            yol = t.DosyaYolu;
            t.MacBasladi(0, 1UL);
            t.Ilerleme(0, 18000, 1, 0, 1, 20.0);
            t.Ilerleme(0, 36000, 1, 1, 2, 40.0);
            t.Dispose();                       // `SeansBitti` BİLEREK çağrılmadı = terk

            var satirlar = File.ReadAllLines(yol).Where(l => l.Length > 0)
                .Select(l => BasitJson.Nesne(BasitJson.Ayristir(l))).ToList();
            var tipler = satirlar.Select(n => (string)n["t"]).ToList();

            Assert.IsFalse(tipler.Contains("session_end"), "temiz çıkış işareti var — senaryo yanlış kurulmuş");
            Assert.IsTrue(tipler.Contains("match_start"), "match_start yok");
            Assert.IsFalse(tipler.Contains("match_end"), "match_end var — terk senaryosu değil");

            // "Nerede bıraktı" son ilerleme satırından okunuyor.
            var sonIlerleme = satirlar.Last(n => (string)n["t"] == "ilerleme");
            Assert.AreEqual(36000d, BasitJson.Sayi(sonIlerleme["tick"]), 1e-9);
            Assert.AreEqual("1-1", (string)sonIlerleme["score"]);
        }

        [Test]
        public void Ayarlar_IlerlemeAraligiSifirKabulEdilmez()
        {
            // 0/negatif aralık "nerede bıraktı" okumasını kör eder.
            var a = new MacSunumAyarlari { telemetriIlerlemeDakika = 0 };
            Assert.Throws<ArgumentException>(() => a.Dogrula());
        }
    }
}
