using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TheBadge.Sim.Config;
using TheBadge.Sim.Core;

namespace TheBadge.Sim.Replay
{
    /// <summary>DETERMİNİZM SONDASI — P0 ikinci aşaması (ADR-004, DECISIONS P0 (b)). Bir çalışma
    /// zamanının (sunucu .NET, Editor Mono, cihaz IL2CPP) sunucuyla BİT-EŞİT olup olmadığını dört
    /// katmanda ölçer; her katman bir öncekinin sapmasını ayırır:
    ///  1. Balance BAYTLARI — dosyanın ham bayt özeti golden setin pinlediğiyle aynı mı (ME 3.3).
    ///  2. Balance DEĞERLERİ — okunmuş nesnenin dökümü sunucununkiyle aynı mı (JSON ayrıştırıcısı).
    ///  3. P0 pinleri — ızgara girdileri, DetMath'in yedi fonksiyonu, TrigLut, dönüşüm fikstürleri.
    ///  4. 50 golden replay — sekiz alanın kanonik kaydı (ME 17.4).
    /// Platformun kendi araçlarına (dosya okuma, JSON) dokunmaz; girdileri çağıran yükler.</summary>
    public static class DeterminizmSondasi
    {
        public static SondaRaporu Kos(SimBalance bal, byte[] balanceBaytlari, byte[] bantBaytlari,
                                      string goldenJson, string sunucuBalansDokumu, string platform)
        {
            var r = new SondaRaporu(platform);
            var c = CultureInfo.InvariantCulture;

            GoldenSet set;
            try { set = GoldenSetOkuyucu.Oku(goldenJson); }
            catch (FormatException ex) { r.Ekle("GoldenSet", false, ex.Message); return r; }
            r.Ekle("GoldenSet", set.Surum == GoldenReplay.Surum,
                   "sürüm " + set.Surum + (set.Surum == GoldenReplay.Surum ? "" : " ≠ " + GoldenReplay.Surum));

            // 1) Ham bayt özetleri — farklıysa dosya başka (ör. satır sonu dönüştürülmüş)
            ulong balHash = XxHash64.Hash(balanceBaytlari), bantHash = XxHash64.Hash(bantBaytlari);
            bool baytOk = balHash == set.BalanceHash && bantHash == set.BandsHash;
            r.Ekle("BalansBaytlari", baytOk, baytOk
                ? "sim.balance 0x" + balHash.ToString("X16", c) + " · command.bands 0x" + bantHash.ToString("X16", c)
                : "sim.balance 0x" + balHash.ToString("X16", c) + " (golden 0x" + set.BalanceHash.ToString("X16", c) +
                  ") · command.bands 0x" + bantHash.ToString("X16", c) + " (golden 0x" + set.BandsHash.ToString("X16", c) + ")");

            // 2) Balance değerleri — JSON ayrıştırıcısının bit düzeyindeki tanığı
            string yerel = BalansDokumu.Metin(bal);
            var fark = BalansDokumu.Fark(sunucuBalansDokumu, yerel, 8);
            int satir = 0;
            foreach (char ch in yerel) if (ch == '\n') satir++;
            r.Ekle("BalansDegerleri", fark.Count == 0, fark.Count == 0
                ? satir.ToString(c) + " alan sunucuyla bit-eşit (özet 0x" + BalansDokumu.Ozet(yerel).ToString("X16", c) + ")"
                : "farklı alanlar: " + string.Join(" | ", fark));

            // 3) P0 pinleri
            var g = P0Pinleri.IzgarayiKur();
            ulong izg = P0Pinleri.IzgaraOzeti(g);
            r.Ekle("P0Izgara", izg == P0Pinleri.IzgaraPin,
                   "girdi özeti 0x" + izg.ToString("X16", c) +
                   (izg == P0Pinleri.IzgaraPin ? "" : " ≠ pin 0x" + P0Pinleri.IzgaraPin.ToString("X16", c) +
                                                       " — girdiler bile farklı (taban + ölçek·U birleştirilmiş olabilir)"));
            var pinFark = new List<string>();
            foreach (var (ad, pin) in P0Pinleri.DetMathPinleri)
            {
                ulong oz = P0Pinleri.BitOzeti(P0Pinleri.DetMathCiktilari(ad, g));
                if (oz != pin) pinFark.Add(ad + " 0x" + oz.ToString("X16", c) + " ≠ 0x" + pin.ToString("X16", c));
            }
            r.Ekle("P0DetMath", pinFark.Count == 0, pinFark.Count == 0
                ? P0Pinleri.DetMathPinleri.Length.ToString(c) + " fonksiyonun çıktı özeti pinle bit-eşit"
                : string.Join(" · ", pinFark));
            ulong lut = P0Pinleri.TrigLutOzeti();
            r.Ekle("P0TrigLut", lut == P0Pinleri.TrigLutPin, "0x" + lut.ToString("X16", c) +
                   (lut == P0Pinleri.TrigLutPin ? " pinle eşit" : " ≠ pin 0x" + P0Pinleri.TrigLutPin.ToString("X16", c)));
            var donFark = new List<string>();
            var fikstur = P0Pinleri.DonusumFiksturleri();
            foreach (var (ad, olc, bek) in fikstur)
                if (olc != bek) donFark.Add(ad + " = " + olc.ToString(c) + " ≠ " + bek.ToString(c));
            r.Ekle("P0Donusum", donFark.Count == 0, donFark.Count == 0
                ? fikstur.Length.ToString(c) + " fikstür doyuruyor" : string.Join(" · ", donFark));

            // 4) 50 golden replay — golden setin pinlediği bayt özetleriyle kurulur ki 1. katmandaki
            // bir dosya farkı (satır sonu) replay karşılaştırmasını kirletmesin
            int esit = 0;
            var replayFark = new List<string>();
            for (int idx = 0; idx < GoldenReplay.Sayi; idx++)
            {
                string olculen = GoldenReplay.Oynat(idx, set.BalanceHash, set.BandsHash, bal).Kayit();
                if (olculen == set.Kayitlar[idx]) { esit++; continue; }
                if (replayFark.Count < 3)
                    replayFark.Add("#" + idx.ToString(c) + " golden [" + set.Kayitlar[idx] + "] ölçülen [" + olculen + "]");
            }
            r.Ekle("GoldenReplay", esit == GoldenReplay.Sayi,
                   esit.ToString(c) + "/" + GoldenReplay.Sayi.ToString(c) + " bit-eşit" +
                   (replayFark.Count == 0 ? "" : " · ilk farklar: " + string.Join(" | ", replayFark)));
            return r;
        }
    }

    /// <summary>Sondanın raporu: katman başına geçti/kaldı + ayrıntı. Metin kültürden bağımsızdır.</summary>
    public sealed class SondaRaporu
    {
        public readonly string Platform;
        public readonly List<(string katman, bool gecti, string ayrinti)> Katmanlar =
            new List<(string katman, bool gecti, string ayrinti)>();

        public SondaRaporu(string platform) { Platform = platform; }

        public bool Gecti
        {
            get
            {
                if (Katmanlar.Count == 0) return false;
                foreach (var k in Katmanlar) if (!k.gecti) return false;
                return true;
            }
        }

        public void Ekle(string katman, bool gecti, string ayrinti) => Katmanlar.Add((katman, gecti, ayrinti));

        public string Metin()
        {
            var b = new StringBuilder();
            b.Append("DETERMINIZM SONDASI [").Append(Platform).Append("]: ")
             .Append(Gecti ? "GECTI — sunucuyla bit-eşit" : "KALDI").Append('\n');
            foreach (var (katman, gecti, ayrinti) in Katmanlar)
                b.Append(gecti ? "  [PASS] " : "  [FAIL] ").Append(katman).Append(" — ").Append(ayrinti).Append('\n');
            return b.ToString();
        }
    }
}
