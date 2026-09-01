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
        public KalibrasyonCfg kalibrasyon = new KalibrasyonCfg();
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

        /// <summary>Köprü kadrosuyla motorun kalmak zorunda olduğu bantlar — K11 kapısı okur.
        /// `sutTavani` bir BORÇ tavanıdır: hedef `sutHedefi`, bugünkü ölçüm onun üstünde.</summary>
        [Serializable] public sealed class KalibrasyonCfg
        {
            public string aciklama;
            public double[] golBandi = new double[0];
            public double[] kartBandi = new double[0];
            public double kornerAlt, sutTavani, sutHedefi, kirmiziTavani;
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
            if (kalibrasyon.golBandi.Length != 2 || kalibrasyon.kartBandi.Length != 2)
                throw new ArgumentException("squad.balance: kalibrasyon bantları [alt,üst] olmalı.");
            if (kalibrasyon.sutTavani < kalibrasyon.sutHedefi)
                throw new ArgumentException("squad.balance: şut tavanı hedefin ALTINA inemez — hedefe düştüğünde borç kapanmıştır, tavan kaldırılmalı.");
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
            // SAKAT OYUNCU SAHAYA ÇIKMAZ (`SakatlikHafta` 0 = sağlam; `SquadActions` kaptanlıkta
            // aynı ölçütü kullanıyor). Süzme BURADA yapılır, `KurDizi`de değil: `KurDizi`nin
            // sözleşmesi "bunlar UYGUN oyuncular"dır ve rakip kulüpler gibi sakatlık modellemeyen
            // çağıranlar o sözleşmeyi zaten karşılar. Süzme sonrası hat dolmuyorsa köprü `null`
            // döner — eksik kadroyla maça çıkmak bir oyun kuralıdır, gizlenecek bir ayrıntı değil.
            int n = 0;
            for (int i = 0; i < st.Oyuncular.Length; i++)
                if (st.Oyuncular[i].ClubId == clubId && st.Oyuncular[i].SakatlikHafta == 0) n++;
            var id = new int[n]; var rol = new byte[n]; var guc = new byte[n];
            for (int i = 0, k = 0; i < st.Oyuncular.Length; i++)
            {
                if (st.Oyuncular[i].ClubId != clubId || st.Oyuncular[i].SakatlikHafta > 0) continue;
                id[k] = st.Oyuncular[i].PlayerId; rol[k] = st.Oyuncular[i].RolId; guc[k] = st.Oyuncular[i].Guc; k++;
            }
            return KurDizi(bal, evSahibi, id, rol, guc, out hata);
        }

        /// <summary>ÇEKİRDEK — kadroyu dizilerden kurar. `GameState` almayan bir yol gerekiyordu:
        /// rakip kulüpler (lig doldurma) dünya durumunda YAŞAMIYOR, ama onların kadrosu da AYNI
        /// eşlemeden geçmeli. İki ayrı diziliş mantığı yazmak, iki takımın farklı kurallarla
        /// sahaya çıkması demekti.
        ///
        /// SÖZLEŞME: gelen diziler YALNIZ UYGUN (sakat olmayan) oyuncuları taşır. Süzme
        /// `Kur(GameState,…)` yolunda yapılır; sakatlık modellemeyen çağıranlar (lig doldurma)
        /// bu sözleşmeyi zaten karşılar.
        ///
        /// GİRDİ SIRASI ÖNEMSİZDİR ama SONUÇ KANONİKTİR: hat içi sıralama güce göre azalan,
        /// eşitlikte PlayerId artan. Eşitlik kuralı şart — `Guc` bayt, eşitlik sık ve kararsız
        /// sıralama aynı kadroya iki koşuda farklı 11 verirdi (determinizm ihlali).</summary>
        public static TeamSheet KurDizi(SquadBalance bal, bool evSahibi,
                                        int[] playerId, byte[] rolId, byte[] guc, out string hata)
        {
            if (bal == null) throw new ArgumentNullException(nameof(bal));
            if (playerId == null || rolId == null || guc == null) throw new ArgumentNullException(nameof(playerId));
            if (playerId.Length != rolId.Length || playerId.Length != guc.Length)
                throw new ArgumentException("SquadBridge: playerId/rolId/guc dizileri aynı uzunlukta olmalı.");
            hata = null;
            int isaret = evSahibi ? -1 : 1;   // ev sahibi KENDİ kalesi -x'te; +x yönüne hücum eder

            var hatta = new System.Collections.Generic.List<int>[4];
            for (int h = 0; h < 4; h++) hatta[h] = new System.Collections.Generic.List<int>();
            for (int i = 0; i < playerId.Length; i++)
            {
                int r = rolId[i];
                if (r < 1 || r > bal.rolHat.Length) { hata = $"oyuncu {playerId[i]} rolId {r} kapsam dışı"; return null; }
                hatta[bal.rolHat[r - 1]].Add(i);
            }
            for (int h = 0; h < 4; h++)
                hatta[h].Sort((x, y) =>
                {
                    int c = guc[y].CompareTo(guc[x]);
                    return c != 0 ? c : playerId[x].CompareTo(playerId[y]);
                });

            for (int h = 0; h < 4; h++)
                if (hatta[h].Count < bal.dizilis.hatSayilari[h])
                {
                    hata = $"{bal.hatlar[h]} hattında {hatta[h].Count} oyuncu var, diziliş {bal.dizilis.hatSayilari[h]} istiyor";
                    return null;
                }

            // KİMLİK GENİŞLİĞİ: dünyada `PlayerId` int, motorda `short` (`PlayerEntry.PlayerId`).
            // Sessiz `(short)` dönüşümü 32767 üstündeki iki oyuncuyu AYNI kimliğe düşürebilir ve
            // motor onları "iki takımda birden" sanır — ya da daha kötüsü, aynı takımda çakışırlar.
            // Kırpmayı köprü YAKALAR; motorun kurulum hatasına bırakmak, sebebi kadro üretimine
            // kadar geri izlemeyi gerektirirdi.
            for (int i = 0; i < playerId.Length; i++)
                if (playerId[i] < short.MinValue || playerId[i] > short.MaxValue)
                {
                    hata = $"PlayerId {playerId[i]} motorun short kimlik aralığı dışında ({short.MinValue}..{short.MaxValue})";
                    return null;
                }

            var sheet = new TeamSheet { Starters = new PlayerEntry[11] };
            // BOOL DİZİ, HashSet DEĞİL: proje sırasız yapıya karşı temkinli (CLAUDE.md).
            var kullanildi = new bool[playerId.Length];
            int yaz = 0;
            for (int h = 0; h < 4; h++)
                for (int k = 0; k < bal.dizilis.hatSayilari[h]; k++, yaz++)
                {
                    int i = hatta[h][k];
                    kullanildi[i] = true;
                    sheet.Starters[yaz] = Giris(playerId[i], h, guc[i], bal,
                                                isaret * bal.dizilis.capX[h], bal.dizilis.capY[h][k]);
                }

            // YEDEKLER: HAT HAT DEĞİL, SIRAYLA (round-robin). Hat hat doldurmak, 18 kişilik
            // varsayılan kadroda 5 yuvayı ilk üç hatta harcıyor ve FORVET YEDEĞİ HİÇ KALMIYORDU
            // (inceleme bulgusu, Codex P2). `AutoSubstitute` rol eşleşmesine +1000 veriyor
            // (MatchEngine:2775), yani forvet sakatlanınca yerine mevkisi tutmayan biri giriyordu.
            // Sıralı dağıtım her hatta en az bir yedek garanti eder.
            var yedekler = new System.Collections.Generic.List<PlayerEntry>();
            var hatYedek = new int[4];              // hat başına kaç yedek yazıldı
            var sonraki = new int[4];               // hat içinde nereye kadar bakıldı
            bool eklendi = true;
            while (yedekler.Count < bal.yedekSayisi && eklendi)
            {
                eklendi = false;
                for (int h = 0; h < 4 && yedekler.Count < bal.yedekSayisi; h++)
                {
                    while (sonraki[h] < hatta[h].Count && kullanildi[hatta[h][sonraki[h]]]) sonraki[h]++;
                    if (sonraki[h] >= hatta[h].Count) continue;
                    int i = hatta[h][sonraki[h]++];
                    kullanildi[i] = true;
                    yedekler.Add(Giris(playerId[i], h, guc[i], bal,
                                       isaret * bal.dizilis.capX[h], YedekCapaY(bal, h, hatYedek[h]++)));
                    eklendi = true;
                }
            }
            sheet.Bench = yedekler.ToArray();
            sheet.Validate(evSahibi ? "ev" : "deplasman");
            return sheet;
        }

        /// <summary>MOTOR ROLÜ DÜNYA ROLÜ DEĞİLDİR. Dünya `rolId`si 1-32 bandında bir GDD 3.2
        /// rol kataloğudur; motorunki bir HAT kodudur ve anlamı MatchEngine'de sabittir:
        /// 1 kaleci · 2 defans · 3 orta saha · 4 forvet (`RoleId <= 2` savunur, `>= 3` ileri
        /// koşar, `> 3` markaja inmez — MatchEngine:620/1439/1986). İlk yazımda dünya rolünü
        /// OLDUĞU GİBİ geçirmiştim; motor 4-32 arasını forvet saydığı için takım 14 forvetle
        /// sahaya çıkıyordu. Ölçüm açıktı ve gizlenemezdi: maç başına 40-58 şut, 8-4 skorlar.
        /// İki kimlik uzayı arasındaki çeviri `rolHat` hattıdır: hat + 1 = motor rolü.</summary>
        static PlayerEntry Giris(int playerId, int hat, byte guc, SquadBalance bal, int ax, int ay)
        {
            var w = bal.Profil(hat).Dizi();
            byte N(int a) => Olcekle(guc, w[a]);
            return new PlayerEntry
            {
                PlayerId = (short)playerId,
                Name = null,                      // sunum verisi — köprünün işi değil (ME 5.2: Name MatchState'e girmez)
                RoleId = (byte)(hat + 1),   // hat → motor rolü (1 KL · 2 DF · 3 OS · 4 FV)
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

        /// <summary>YEDEĞİN ÇAPASI — hattın MERKEZİNDEN dışa doğru. Motor değişiklikte GELEN
        /// oyuncunun çapasını slota KOPYALIYOR (`ApplyPendingSubs`, MatchEngine:2896), yani
        /// yedeğin çapası oyuncunun sahada duracağı yeri belirliyor. Hepsine `capY[h][0]`
        /// vermek, sağ bek sakatlanınca yerine gireni EN SOLA taşıyordu (inceleme bulgusu,
        /// Codex P2). Köprü kimin sakatlanacağını bilemez; bilinebilecek en az yanlış seçim
        /// merkezdir — ortalama yer değiştirmeyi küçültür. Aynı hattan ikinci yedek bir sonraki
        /// merkeze yakın yuvayı alır.</summary>
        static int YedekCapaY(SquadBalance bal, int hat, int sira)
        {
            var y = bal.dizilis.capY[hat];
            if (y.Length == 1) return y[0];
            // |y|'ye göre artan sıralama — merkez önce. Kanonik: eşitlikte indeks artan.
            var sirali = new int[y.Length];
            for (int i = 0; i < y.Length; i++) sirali[i] = i;
            Array.Sort(sirali, (a, b) =>
            {
                int c = Math.Abs(y[a]).CompareTo(Math.Abs(y[b]));
                return c != 0 ? c : a.CompareTo(b);
            });
            return y[sirali[sira % y.Length]];
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
