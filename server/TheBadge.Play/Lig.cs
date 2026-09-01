using System;
using System.Collections.Generic;
using TheBadge.Sim.Match;
using TheBadge.World;

namespace TheBadge.Play
{
    /// <summary>Bir lig kulübü. Oyuncunun kulübü (index 0) dünya durumunda YAŞAR — ekonomisi,
    /// kadrosu, komutları gerçektir. Rakipler yalnız KADRO + PUAN taşır: dünya katmanı bugün tek
    /// kulübü modelliyor ve olmayan bir çok-kulüp ekonomisini burada uydurmak, oyuncuya gerçek
    /// gibi görünen sahte bir sistem gösterirdi.</summary>
    public sealed class Kulup
    {
        public int Id;
        public string Ad;
        public byte GucTaban;              // rakip kadro üretiminin merkezi
        public TeamSheet Ev, Deplasman;    // ev/deplasman çapaları aynalı olduğu için iki kadro
        /// <summary>Ham kadro — kadrolar HER HAFTA bundan YENİDEN kurulur. İlk yazımda iki
        /// `TeamSheet` başlangıçta bir kez kuruluyor ve sezon boyu aynen kullanılıyordu: oyuncunun
        /// 11'i yorulurken rakipler bütün sezon 90 kondisyonda kalıyor ve maç 1'den sonra her
        /// rakibe sessiz bir form üstünlüğü doğuyordu (inceleme bulgusu, Bugbot).</summary>
        public int[] OyuncuId; public byte[] OyuncuRol, OyuncuGuc, OyuncuKond, OyuncuMoral;
        public int O, G, B, M, AG, YG;
        public int Puan => G * 3 + B;
        public int Averaj => AG - YG;
    }

    /// <summary>Fikstür maçı — hangi hafta, kim ev sahibi.</summary>
    public struct Mac
    {
        public int Hafta, Ev, Dep;
    }

    public static class LigKurucu
    {
        /// <summary>20 kulüp × çift devreli lig = 38 hafta — `world.balance.json`daki
        /// `sezonHaftaSayisi` ile BİREBİR. Sayı buradan uydurulmadı; sezon uzunluğu zaten
        /// 20 takımlı bir ligi tarif ediyordu.</summary>
        public const int KulupSayisi = 20;

        /// <summary>Kadroların maça giriş kondisyonu. OYUNCU ve RAKİP AYNI değeri kullanır —
        /// rakip kadroları kondisyonsuz kurmak, köprünün "ayarlanmamış = tam enerji" nöbetçisine
        /// düşüyor ve her rakibe sessiz bir kondisyon avantajı veriyordu (inceleme bulgusu, P1):
        /// oyuncunun 11'i 955 enerjiyle, rakibin 11'i 1000 ile çıkıyordu.</summary>
        public const byte VarsayilanKondisyon = 90;
        /// <summary>Aynı gerekçe momentum için: moral verilmezse `BaslangicMomentum` 0 kalır ve
        /// oyuncunun takımı moralinden gelen momentumla, rakip nötr momentumla sahaya çıkardı.</summary>
        public const byte VarsayilanMoral = 60;

        static readonly string[] Adlar =
        {
            "Demirkale FK", "Yeşilvadi SK", "Karadeniz Fırtına", "Altınboynuz",
            "Taşhan Birliği", "Mavi Liman SK", "Bozkır Atmaca", "Gümüşpınar",
            "Kartaltepe", "Akdeniz Yıldız", "Çelikhisar", "Beyaztuğ SK",
            "Dağköy Şimşek", "Kuzey Kapısı", "Alacahöyük FK", "Deniztaşı SK",
            "Yedikuyu Birlik", "Turnalı SK", "Kızılkaya FK", "Son Umut SK"
        };

        /// <summary>Kulüpler. Index 0 OYUNCUNUNdur ve `oyuncuAdi` ile adlandırılır. Rakip güçleri
        /// 52-78 arasında DETERMİNİSTİK dağıtılır: lig hem yenilebilir hem de tepesi zor olsun.</summary>
        public static Kulup[] Kur(string oyuncuAdi, SquadBalance sqBal, byte oyuncuGucu)
        {
            var k = new Kulup[KulupSayisi];
            for (int i = 0; i < KulupSayisi; i++)
            {
                byte g = i == 0 ? oyuncuGucu : (byte)(52 + (i * 37) % 27);
                // AD ÇAKIŞMASI: oyuncu kulübünü `Adlar` içindeki bir adla adlandırırsa iki kulüp
                // aynı adı taşır ve skor tablosunda "X 6-0 X" görünür — ilk koşuda tam olarak bu
                // oldu. Çakışan rakip adı işaretlenir; sessizce aynı ada izin vermek, oyuncunun
                // kendi kendisiyle oynadığını sandığı bir ekran demekti.
                string ad = i == 0 ? oyuncuAdi
                          : string.Equals(Adlar[i], oyuncuAdi, StringComparison.Ordinal) ? Adlar[i] + " (rakip)"
                          : Adlar[i];
                k[i] = new Kulup { Id = i + 1, Ad = ad, GucTaban = g };
                if (i > 0) { KadroDizileriKur(k[i], g); KadrolariYenile(k[i], sqBal); }
            }
            return k;
        }

        /// <summary>Rakip kadro dizileri. Mevki içi güç dağılımı indeksten türetilir; RNG yok,
        /// lig her açılışta aynı. Kondisyon/moral OYUNCUNUNKİYLE aynı varsayılandan başlar.</summary>
        static void KadroDizileriKur(Kulup k, byte taban)
        {
            const int N = 18;
            k.OyuncuId = new int[N]; k.OyuncuRol = new byte[N]; k.OyuncuGuc = new byte[N];
            k.OyuncuKond = new byte[N]; k.OyuncuMoral = new byte[N];
            // 2 KL · 6 DF · 6 OS · 4 FV — `rolHat`: 1 KL · 2-8 DF · 9-20 OS · 21-32 FV
            byte[] roller = { 1, 1, 2, 3, 4, 5, 6, 7, 9, 10, 11, 12, 13, 14, 21, 22, 23, 24 };
            for (int i = 0; i < N; i++)
            {
                k.OyuncuId[i] = k.Id * 1000 + i;
                k.OyuncuRol[i] = roller[i];
                int d = ((k.Id * 13 + i * 29) % 15) - 7;     // -7..+7 sapma
                int v = taban + d;
                k.OyuncuGuc[i] = (byte)(v < 30 ? 30 : v > 95 ? 95 : v);
                k.OyuncuKond[i] = VarsayilanKondisyon;
                k.OyuncuMoral[i] = VarsayilanMoral;
            }
        }

        /// <summary>Rakip kadrosu — OYUNCUNUN kadrosuyla AYNI köprüden geçer (`SquadBridge`).
        /// Ayrı bir üretici yazmak, iki takımın farklı kurallarla sahaya çıkması demekti.</summary>
        public static void KadrolariYenile(Kulup k, SquadBalance bal)
        {
            k.Ev = Kur1(k, bal, true);
            k.Deplasman = Kur1(k, bal, false);
        }

        static TeamSheet Kur1(Kulup k, SquadBalance bal, bool ev)
        {
            var s = SquadBridge.KurDizi(bal, ev, k.OyuncuId, k.OyuncuRol, k.OyuncuGuc,
                                        out string hata, k.OyuncuKond, k.OyuncuMoral);
            if (s == null) throw new InvalidOperationException($"rakip kadro kurulamadı: {hata}");
            return s;
        }

        /// <summary>RAKİBİN HAFTA SONU — oyuncunun kulübüyle AYNI aritmetik (`MacSonrasi`).
        /// Sahaya çıkan 11 yorulur, kalanlar toparlanır; sonra kadrolar YENİDEN kurulur ki
        /// yorgunluk bir sonraki haftanın SEÇİMİNE de yansısın (etkin güç).
        ///
        /// Bu olmadan rakipler bütün sezon 90 kondisyonda (enerji 955) kalıyor, oyuncunun 11'i
        /// dengesine (60 → ~700) iniyordu: maç 1'den sonra her rakibe sessiz bir form üstünlüğü.
        /// Düzelttiğim BAŞLANGIÇ asimetrisinin sürüklenen hâliydi (inceleme bulgusu, Bugbot).</summary>
        public static void HaftaSonu(Kulup k, SquadBalance bal)
        {
            if (k.Ev == null || k.OyuncuId == null) return;
            for (int i = 0; i < k.OyuncuId.Length; i++)
            {
                bool oynadi = false;
                for (int t = 0; t < k.Ev.Starters.Length; t++)
                    if (k.Ev.Starters[t].PlayerId == k.OyuncuId[i]) { oynadi = true; break; }
                k.OyuncuKond[i] = MacSonrasi.YeniKondisyon(k.OyuncuKond[i], oynadi, bal);
            }
            KadrolariYenile(k, bal);
        }

        /// <summary>Çift devreli fikstür (circle method). İlk devre 19 hafta, ikinci devre aynı
        /// eşleşmeler ev/deplasman TERS. Toplam 38 = sezon uzunluğu.</summary>
        public static List<Mac> Fikstur(int n)
        {
            var takim = new int[n];
            for (int i = 0; i < n; i++) takim[i] = i;
            var mac = new List<Mac>();
            int tur = n - 1;
            for (int r = 0; r < tur; r++)
            {
                for (int i = 0; i < n / 2; i++)
                {
                    int a = takim[i], b = takim[n - 1 - i];
                    // Tur parite: aynı takım her hafta ev sahibi olmasın.
                    bool duz = ((r + i) % 2) == 0;
                    mac.Add(new Mac { Hafta = r + 1, Ev = duz ? a : b, Dep = duz ? b : a });
                    mac.Add(new Mac { Hafta = r + 1 + tur, Ev = duz ? b : a, Dep = duz ? a : b });
                }
                // 0 sabit, geri kalan sağa döner
                int son = takim[n - 1];
                for (int i = n - 1; i > 1; i--) takim[i] = takim[i - 1];
                takim[1] = son;
            }
            mac.Sort((x, y) => x.Hafta != y.Hafta ? x.Hafta.CompareTo(y.Hafta) : x.Ev.CompareTo(y.Ev));
            return mac;
        }

        /// <summary>Puan durumu sıralaması: puan → averaj → attığı gol → ad (kanonik).</summary>
        public static Kulup[] PuanDurumu(Kulup[] k)
        {
            var s = (Kulup[])k.Clone();
            Array.Sort(s, (x, y) =>
            {
                int c = y.Puan.CompareTo(x.Puan); if (c != 0) return c;
                c = y.Averaj.CompareTo(x.Averaj); if (c != 0) return c;
                c = y.AG.CompareTo(x.AG); if (c != 0) return c;
                return string.CompareOrdinal(x.Ad, y.Ad);
            });
            return s;
        }

        public static void SonucIsle(Kulup ev, Kulup dep, int eg, int dg)
        {
            ev.O++; dep.O++;
            ev.AG += eg; ev.YG += dg; dep.AG += dg; dep.YG += eg;
            if (eg > dg) { ev.G++; dep.M++; }
            else if (eg < dg) { dep.G++; ev.M++; }
            else { ev.B++; dep.B++; }
        }
    }
}
