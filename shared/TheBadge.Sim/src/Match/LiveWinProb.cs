using System;
using TheBadge.Sim.Config;

namespace TheBadge.Sim.Match
{
    /// <summary>CANLI KAZANMA OLASILIĞI — üç sonuçlu (galibiyet / beraberlik / mağlubiyet).
    ///
    /// NEDEN VAR (5G S2 bulgusu, 2026-09-04): greybox'ın fun hipotezi "karar ver → kazanma
    /// ihtimali DEĞİŞSİN → sonucu yaşa" idi ve greybox bunu KALİBRE bir şeritle sunuyordu.
    /// Motordaki `MatchEngine.WinProb(golFarki, dakika)` bu iş için yazılmamıştı — imzasında
    /// başka girdi YOK. Ölçüldü (3 senaryo × 300 maç): kaç vuruşunda üç senaryoda da tam %50
    /// diyor, gerçek ev galibiyeti oranı %80,7 ile %5,3 arasında değişirken; dengeli maçta
    /// dakikaların %51'inde donuk; en kalabalık kalibrasyon kovası 18,5 puan sapıyor. Yani
    /// greybox'ın şeridi motora olduğu gibi taşınsaydı çekirdek vaat SESSİZCE ölürdü.
    ///
    /// ME 15.3'ün `WinProb`una DOKUNULMADI: o, highlight sıralamasının çekirdeği ve orada
    /// yalnız GÖRECELİ sıçrama gerekir — işini yapıyor. Onu değiştirmek highlight sıralamasını,
    /// dolayısıyla röportaj/Panorama girdisini ölçümsüz değiştirmek olurdu.
    ///
    /// MODEL: kalan sürede her takımın gol sayısı Poisson; oran güç FARKINDAN türer.
    ///   λ = lambdaTaban × exp(gucKatsayisi × (kendiGuc − rakipGuc)) × (kalanDk / 90)
    /// Katsayılar motorun KENDİ davranışından oturtuldu (49 eşleşme × 300 maç, ofset −12..+12;
    /// log-lineer en küçük kareler, R² = 0,977). Sonuç, iki Poisson'un konvolüsyonuyla üç
    /// sonuca dağıtılır — Monte Carlo YOK, kapalı toplam (determinizm + ucuzluk).
    ///
    /// EV AVANTAJI YOK: ölçümde fark=0 eşleşmelerinde ev %33,9 / beraberlik %31,6 / deplasman
    /// %35,1 çıktı — motorda ev avantajı YOK. Modele uydurma bir ev katsayısı KONMADI.
    ///
    /// KALİBRASYON YETMEZ, AYIRT EDİCİLİK DE ÖLÇÜLÜR (5G S2 diş ölçümünün dersi): kapıyı önce
    /// yalnız kalibrasyonla yazdım, sonra `gucKatsayisi`yi 0 yapıp modeli GÜCE KÖR hale getirdim
    /// — ve kapı yine geçti. Sebebi şu: simetrik bir popülasyonda taban oranı basan bir model
    /// MARJİNAL olarak kalibredir ve tamamen işe yaramazdır. Bu yüzden kapı ayrıca kaç vuruşunda
    /// (bilginin YALNIZ güçten geldiği an) Brier BECERİ payını ölçer; güce kör model orada 0 verir.
    ///
    /// SUNUM YÜZEYİDİR, SİMÜLASYON GİRDİSİ DEĞİL: bu sınıf durumu OKUR, asla yazmaz. `Math.Exp`
    /// kullanır (platformlar arası bit-eşitliği garanti değildir) — bu yüzden sonucu `MatchState`e
    /// ve `StateHash`e ASLA girmez, yalnız `MatchSummaryPacket`e yazılır. Replay kimliği
    /// (ME 3.3 dörtlüsü) bu dosyadan etkilenmez.</summary>
    public static class LiveWinProb
    {
        public struct Sonuc
        {
            public double Ev, Beraberlik, Deplasman;
        }

        /// <summary>Üç sonucun olasılığı. `golFarki` = ev − deplasman (BUGÜNKÜ skor),
        /// `kalanDk` = kalan dakika (0 → skor kesinleşmiş demektir).</summary>
        public static Sonuc Hesapla(SimBalance bal, double gucEv, double gucDep,
                                    int golFarki, double kalanDk)
        {
            var c = bal.canliOlasilik;
            double f = kalanDk / 90.0;
            if (f < 0) f = 0;

            double fark = gucEv - gucDep;
            double lamEv = c.lambdaTaban * Math.Exp(c.gucKatsayisi * fark) * f;
            double lamDep = c.lambdaTaban * Math.Exp(-c.gucKatsayisi * fark) * f;

            int n = c.maxEkGol;
            // Poisson pmf'leri yinelemeli: p[k] = p[k-1] × λ/k (faktöriyel taşması yok)
            Span<double> pEv = stackalloc double[n + 1];
            Span<double> pDep = stackalloc double[n + 1];
            Doldur(pEv, lamEv, n);
            Doldur(pDep, lamDep, n);

            double ev = 0, be = 0, de = 0;
            for (int i = 0; i <= n; i++)
            {
                double pi = pEv[i];
                if (pi <= 0) continue;
                for (int j = 0; j <= n; j++)
                {
                    double p = pi * pDep[j];
                    int d = golFarki + i - j;
                    if (d > 0) ev += p; else if (d == 0) be += p; else de += p;
                }
            }
            // KESME TELAFİSİ: n'de kesilen kuyruk kütlesi geri dağıtılır (üçü toplamı 1 olmalı;
            // toplamayan bir olasılık şeridi kullanıcıya yalan söyler).
            double t = ev + be + de;
            if (t <= 0) return new Sonuc { Ev = 0, Beraberlik = 1, Deplasman = 0 };
            return new Sonuc { Ev = ev / t, Beraberlik = be / t, Deplasman = de / t };
        }

        static void Doldur(Span<double> p, double lam, int n)
        {
            p[0] = Math.Exp(-lam);
            for (int k = 1; k <= n; k++) p[k] = p[k - 1] * lam / k;
        }
    }
}
