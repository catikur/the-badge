using System;
using TheBadge.Sim.Match;

namespace TheBadge.World
{
    /// <summary>`balance/squad.balance.json` POCO'su — K11 kadro köprüsü [KALİBRE].
    /// Çekirdek JSON ayrıştırmaz; host doldurur (`EconomyBalance` ile aynı desen).</summary>
    [Serializable]
    public sealed class SquadBalance
    {
        public int surum;
        public string aciklama;
        public string[] nitelikSirasi = new string[0];
        public string[] hatlar = new string[0];
        public HatProfilleri hatProfilleri = new HatProfilleri();
        public int[] rolHat = new int[0];
        public DizilisCfg dizilis = new DizilisCfg();
        public int yedekSayisi;

        [Serializable] public sealed class HatProfilleri
        {
            public NitelikAgirliklari kaleci = new NitelikAgirliklari();
            public NitelikAgirliklari defans = new NitelikAgirliklari();
            public NitelikAgirliklari ortasaha = new NitelikAgirliklari();
            public NitelikAgirliklari forvet = new NitelikAgirliklari();
        }

        /// <summary>26 nitelik ağırlığı. Alan adları `PlayerAttributes` ile BİREBİR aynıdır;
        /// isim uyuşmazlığı sessiz sıfıra düşmek demektir (aşağıdaki `Validate` bunu yakalar).</summary>
        [Serializable] public sealed class NitelikAgirliklari
        {
            public double Passing, Finishing, Dribbling, Tackling, Heading, FirstTouch, Crossing, SetPieces;
            public double Positioning, Decisions, Composure, Aggression, Workrate, Vision;
            public double Pace, Acceleration, Stamina, Strength, Agility, JumpReach;
            public double Reflexes, Handling, OneOnOne, AerialCommand, Kicking, Throwing;

            public double[] Dizi() => new[]
            {
                Passing, Finishing, Dribbling, Tackling, Heading, FirstTouch, Crossing, SetPieces,
                Positioning, Decisions, Composure, Aggression, Workrate, Vision,
                Pace, Acceleration, Stamina, Strength, Agility, JumpReach,
                Reflexes, Handling, OneOnOne, AerialCommand, Kicking, Throwing
            };
        }

        [Serializable] public sealed class DizilisCfg
        {
            public string ad, aciklama;
            public int[] hatSayilari = new int[0];
            public int[] capX = new int[0];
            public int[][] capY = new int[0][];
        }

        public NitelikAgirliklari Profil(int hat)
        {
            switch (hat)
            {
                case 0: return hatProfilleri.kaleci;
                case 1: return hatProfilleri.defans;
                case 2: return hatProfilleri.ortasaha;
                default: return hatProfilleri.forvet;
            }
        }

        /// <summary>Yapılandırma tutarlılığı. SIFIR AĞIRLIK YASAK: `TeamSheet` kurucusundaki
        /// yorumun anlattığı ders (M4) — eksik/sıfır nitelik o alt sistemi (kaleci 1v1'i, hava
        /// topu, faul agresifliği) SESSİZCE öldürür. JSON'da bir alan adı yanlış yazılırsa
        /// deserializasyon onu 0 bırakır ve hata çok sonra, yanlış yerde görünürdü.</summary>
        public void Validate()
        {
            if (nitelikSirasi.Length != 26) throw new ArgumentException("squad.balance: nitelikSirasi 26 uzunlukta olmalı.");
            if (hatlar.Length != 4) throw new ArgumentException("squad.balance: 4 hat olmalı (kaleci·defans·ortasaha·forvet).");
            if (rolHat.Length != 32) throw new ArgumentException("squad.balance: rolHat 32 uzunlukta olmalı (rolId 1-32).");
            for (int i = 0; i < rolHat.Length; i++)
                if (rolHat[i] < 0 || rolHat[i] > 3) throw new ArgumentException($"squad.balance: rolHat[{i}] 0-3 dışı.");
            for (int h = 0; h < 4; h++)
            {
                var w = Profil(h).Dizi();
                for (int a = 0; a < w.Length; a++)
                    if (!(w[a] > 0)) throw new ArgumentException(
                        $"squad.balance: hat {hatlar[h]} niteliği {nitelikSirasi[a]} sıfır/negatif — sıfır nitelik alt sistemi sessizce öldürür.");
            }
            if (dizilis.hatSayilari.Length != 4 || dizilis.capX.Length != 4 || dizilis.capY.Length != 4)
                throw new ArgumentException("squad.balance: dizilis dizileri 4 hat uzunluğunda olmalı.");
            int toplam = 0;
            for (int h = 0; h < 4; h++)
            {
                toplam += dizilis.hatSayilari[h];
                if (dizilis.capY[h] == null || dizilis.capY[h].Length != dizilis.hatSayilari[h])
                    throw new ArgumentException($"squad.balance: dizilis.capY[{h}] hat sayısıyla uyuşmuyor.");
            }
            if (toplam != 11) throw new ArgumentException($"squad.balance: diziliş {toplam} oyuncu tanımlıyor, 11 olmalı.");
            if (yedekSayisi < 0 || yedekSayisi > 9) throw new ArgumentException("squad.balance: yedekSayisi 0-9 olmalı.");
        }
    }

    /// <summary>KADRO → TEAMSHEET KÖPRÜSÜ (K11). Tycoon katmanıyla maç motoru arasındaki DİKİŞ.
    ///
    /// NEDEN VARDI OLMASI GEREKİYORDU: `PlayerState.Guc`'un kendi XML yorumu "Maç motoru bunları
    /// HENÜZ kullanmıyor" diyordu ve doğruydu — maç motoru sentetik `TeamSheet`lerle, dünya
    /// katmanı da sentetik G-B-M sonuç döngüsüyle test ediliyordu. İki yarı hiç birbirine
    /// bağlanmamıştı; oyunun oynanabilir olması tam olarak bu köprüyü gerektiriyor.
    ///
    /// DETERMİNİZM: seçim ve sıralama tamamen kanonik. RNG YOK — köprü bir eşlemedir, bir üretim
    /// değil; rastgelelik girse aynı kadro iki maçta farklı 11 verirdi.
    ///
    /// NE HARİTALANMIYOR (bilinçli, DECISIONS'ta kayıtlı): `Kondisyon` ve `Moral`. Motor her maça
    /// `Energy = 1000` ile başlıyor (MatchEngine:324) ve morali kendi `momentum`u üzerinden
    /// işliyor; maç ÖNCESİ yorgunluk/moral taşımak ME 12.1'de başlangıç enerjisi kavramı ister.
    /// Bunları niteliklere karıştırmak ÇİFT SAYIM olurdu: motor zaten maç içinde enerjiyi
    /// düşürüp `EffectiveAttributes` ile niteliği ölçekliyor.</summary>
    public static class SquadBridge
    {
        /// <summary>Kulübün kadrosundan diziliş kadrosu kurar. Kadro dizilişi karşılayamıyorsa
        /// `null` döner ve `hata` sebebi söyler — SESSİZ bir yedek 11 üretmez (eksik kadroyla
        /// maça çıkmak bir oyun kuralıdır, köprünün gizleyeceği bir ayrıntı değil).</summary>
        public static TeamSheet Kur(GameState st, long clubId, SquadBalance bal, bool evSahibi,
                                    out string hata)
        {
            if (st == null) throw new ArgumentNullException(nameof(st));
            if (bal == null) throw new ArgumentNullException(nameof(bal));
            hata = null;
            int isaret = evSahibi ? -1 : 1;   // ev sahibi KENDİ kalesi -x'te; +x yönüne hücum eder

            // 1) KULÜBÜN OYUNCULARI, hatlara ayrılmış. `st.Oyuncular` PlayerId'ye göre artan
            //    (GameState.Validate bunu zorlar), dolayısıyla bu gruplar da kanonik sıradadır.
            var hatta = new System.Collections.Generic.List<int>[4];
            for (int h = 0; h < 4; h++) hatta[h] = new System.Collections.Generic.List<int>();
            for (int i = 0; i < st.Oyuncular.Length; i++)
            {
                if (st.Oyuncular[i].ClubId != clubId) continue;
                int rol = st.Oyuncular[i].RolId;
                if (rol < 1 || rol > bal.rolHat.Length) { hata = $"oyuncu {st.Oyuncular[i].PlayerId} rolId {rol} kapsam dışı"; return null; }
                hatta[bal.rolHat[rol - 1]].Add(i);
            }

            // 2) HER HAT KENDİ İÇİNDE GÜCE GÖRE AZALAN; eşitlikte PlayerId ARTAN.
            //    Eşitlik kuralı şart: `Guc` bayt, 22 kişilik kadroda eşitlik sık ve sıralama
            //    kararsız olsaydı aynı kadro iki koşuda farklı 11 verirdi (determinizm ihlali).
            for (int h = 0; h < 4; h++)
                hatta[h].Sort((x, y) =>
                {
                    int c = st.Oyuncular[y].Guc.CompareTo(st.Oyuncular[x].Guc);
                    return c != 0 ? c : st.Oyuncular[x].PlayerId.CompareTo(st.Oyuncular[y].PlayerId);
                });

            for (int h = 0; h < 4; h++)
                if (hatta[h].Count < bal.dizilis.hatSayilari[h])
                {
                    hata = $"{bal.hatlar[h]} hattında {hatta[h].Count} oyuncu var, diziliş {bal.dizilis.hatSayilari[h]} istiyor";
                    return null;
                }

            var sheet = new TeamSheet { Starters = new PlayerEntry[11] };
            int yaz = 0;
            // BOOL DİZİ, HashSet DEĞİL: proje sırasız yapıya karşı temkinli (CLAUDE.md).
            // Burada yalnız `Contains` kullanılsa da, dizinin niyeti okunurken tartışma bırakmaz.
            var kullanildi = new bool[st.Oyuncular.Length];
            for (int h = 0; h < 4; h++)
                for (int k = 0; k < bal.dizilis.hatSayilari[h]; k++, yaz++)
                {
                    int idx = hatta[h][k];
                    kullanildi[idx] = true;
                    sheet.Starters[yaz] = Giris(st, idx, bal, h,
                                                isaret * bal.dizilis.capX[h], bal.dizilis.capY[h][k]);
                }

            // 3) YEDEKLER: kalanlardan, hat sırası korunarak, her hattın en iyisinden. Kanonik
            //    sıra: hat artan, hat içinde güç azalan (yukarıdaki sıralama zaten öyle).
            var yedekler = new System.Collections.Generic.List<PlayerEntry>();
            for (int h = 0; h < 4 && yedekler.Count < bal.yedekSayisi; h++)
                for (int k = 0; k < hatta[h].Count && yedekler.Count < bal.yedekSayisi; k++)
                {
                    int idx = hatta[h][k];
                    if (kullanildi[idx]) continue;
                    yedekler.Add(Giris(st, idx, bal, h, isaret * bal.dizilis.capX[h], bal.dizilis.capY[h][0]));
                }
            sheet.Bench = yedekler.ToArray();
            sheet.Validate(evSahibi ? "ev" : "deplasman");
            return sheet;
        }

        static PlayerEntry Giris(GameState st, int idx, SquadBalance bal, int hat, int ax, int ay)
        {
            var p = st.Oyuncular[idx];
            var w = bal.Profil(hat).Dizi();
            byte N(int a) => Olcekle(p.Guc, w[a]);
            return new PlayerEntry
            {
                PlayerId = (short)p.PlayerId,
                Name = null,                      // sunum verisi — köprünün işi değil (ME 5.2: Name MatchState'e girmez)
                RoleId = p.RolId,
                AnchorXmm = ax,
                AnchorYmm = ay,
                // TÜM 26 nitelik doldurulur; `SquadBalance.Validate` sıfır ağırlığı zaten reddeder.
                Attributes = new PlayerAttributes
                {
                    Passing = N(0), Finishing = N(1), Dribbling = N(2), Tackling = N(3),
                    Heading = N(4), FirstTouch = N(5), Crossing = N(6), SetPieces = N(7),
                    Positioning = N(8), Decisions = N(9), Composure = N(10), Aggression = N(11),
                    Workrate = N(12), Vision = N(13),
                    Pace = N(14), Acceleration = N(15), Stamina = N(16), Strength = N(17),
                    Agility = N(18), JumpReach = N(19),
                    Reflexes = N(20), Handling = N(21), OneOnOne = N(22), AerialCommand = N(23),
                    Kicking = N(24), Throwing = N(25)
                }
            };
        }

        /// <summary>`Guc` × ağırlık → 1-99 nitelik. TABAN 1: sıfır nitelik alt sistemi sessizce
        /// öldürür. TAVAN 99: 100 motorun bant tanımının dışında kalan bir uç değer.</summary>
        public static byte Olcekle(byte guc, double agirlik)
        {
            int v = (int)Math.Round(guc * agirlik, MidpointRounding.AwayFromZero);
            return (byte)(v < 1 ? 1 : v > 99 ? 99 : v);
        }
    }
}
