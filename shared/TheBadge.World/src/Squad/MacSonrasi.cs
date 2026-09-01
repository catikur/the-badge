using System;
using TheBadge.Sim.Match;

namespace TheBadge.World
{
    /// <summary>MAÇ SONRASI KADRO DURUMU — K12-B'nin eksik yarısı (inceleme bulgusu, P2).
    ///
    /// K12-B `Kondisyon` ve `Moral`i motora TAŞIDI ama hiçbir şey onları DEĞİŞTİRMİYORDU: repo
    /// genelinde bu alanlara oynanış tarafından yazan tek bir çağrı yoktu. Sonuç, iddia ettiğimden
    /// dardı — eşleme çalışıyordu, ama her oyuncu her maça aynı 90/60 ile giriyordu ve
    /// **rotasyon oyunda yine hiçbir şey değiştirmiyordu.** Mekanizmayı kurup döngüyü bağlamamak,
    /// çalışan bir kapı ve çalışmayan bir oyun demekti.
    ///
    /// BURADA NE OLUYOR: HERKES toparlanır (100'e olan açığın bir yüzdesi kadar, tavanla sınırlı),
    /// maçta OYNAYAN bunun üstüne yorgunluk yer; moral sonuca göre kayar. Hepsi journal üzerinden —
    /// `EconomyTick` ile aynı desen (bu bir oyuncu KOMUTU değil, haftalık dünya işleyişi; Tek Kapı
    /// komutlar içindir).
    ///
    /// MODELİN ŞEKLİ NEDEN ORANLI: ilk yazımda "oynayan −14, oynamayan +9" idi — yani oynayan
    /// oyuncu hiç toparlanmıyordu. Bu bir yorgunluk eğrisi değil bir CIRCIRdı: düzenli ilk 11 beş
    /// maçta tabana çakılıyor ve sezonun kalanını orada geçiriyordu. Oyunu 6 hafta oynadığımda
    /// takım ligin SONUNCUSU oldu (1 puan, 3-16); aynı seed'de yorgunluk kapatılınca 13. sıra
    /// (6 puan, 6-8). Kapılar yeşildi çünkü yönü ve tabanı ölçüyorlardı, DENGEYİ değil.
    /// Oranlı toparlanma bir denge noktası kurar: her hafta oynayan
    /// `100 − oynayanDusus×100/toparlanmaYuzde` civarında oturur (bugün ~60), dinlenen 100'e
    /// tırmanır. Rotasyon böylece bir ZORUNLULUK değil bir TERCİH olur.</summary>
    public static class MacSonrasi
    {
        /// <summary>BİR OYUNCUNUN HAFTALIK KONDİSYON ADIMI — tek aritmetik, iki çağıran.
        ///
        /// TOPARLANMA HERKESE: 100'e olan açığın yüzdesi, tavanla sınırlı. Oynayan bunun ÜSTÜNE
        /// yorgunluğu yer; böylece düzenli oynayan bir DENGEYE oturur, tabana çakılmaz.
        ///
        /// NEDEN AYRI METOT: dünya kulübü `GameState`te yaşıyor, lig rakipleri yaşamıyor — ama
        /// ikisinin yorgunluğu AYNI olmak zorunda. İlk yazımda yalnız oyuncunun kulübü yoruluyordu
        /// ve rakipler bütün sezon 90 kondisyonda kalıyordu: maç 1'den sonra her rakibe sessiz bir
        /// form üstünlüğü (inceleme bulgusu, Bugbot — düzelttiğim BAŞLANGIÇ asimetrisinin
        /// SÜRÜKLENEN hâli). İki ayrı aritmetik yazmak aynı hatayı üçüncü kez davet ederdi.</summary>
        public static byte YeniKondisyon(byte kondisyon, bool oynadi, SquadBalance bal)
        {
            if (bal == null) throw new ArgumentNullException(nameof(bal));
            var ms = bal.macSonrasi;
            int acik = 100 - kondisyon;
            // EN AZ 1: tam sayı bölmesi tepeye yakın sıfıra düşüyor ve toparlanma orada
            // KİLİTLENİYOR — kapı bunu yakaladı, dinlenen oyuncu 98'de takılı kalıyordu.
            int toparlanma = acik <= 0 ? 0 : Math.Max(1, (acik * ms.toparlanmaYuzde) / 100);
            if (toparlanma > ms.toparlanmaTavani) toparlanma = ms.toparlanmaTavani;
            int yeni = kondisyon + toparlanma - (oynadi ? ms.oynayanDusus : 0);
            if (yeni < ms.kondisyonTaban) yeni = ms.kondisyonTaban;
            if (yeni > 100) yeni = 100;
            return (byte)yeni;
        }

        /// <summary>Maçtan sonra kadro durumunu işler. `sahada` = maça çıkan kadro (ilk 11 +
        /// kulübe); kulübedekiler de "kadroda" sayılır ama YORULMAZ — yalnız ilk 11 yorulur.
        /// Kadroda olmayan (kadro dışı) oyuncular da dinlenir.</summary>
        public static void Isle(GameState st, long clubId, TeamSheet sahada, WeekResult sonuc,
                                SquadBalance bal, WorldJournal j)
        {
            if (st == null) throw new ArgumentNullException(nameof(st));
            if (bal == null) throw new ArgumentNullException(nameof(bal));
            if (j == null) throw new ArgumentNullException(nameof(j));
            var ms = bal.macSonrasi;

            // İlk 11'in kimlikleri — küçük n, O(11) arama yeterli ve sırasız yapı KULLANILMAZ.
            var oynayan = new int[sahada?.Starters?.Length ?? 0];
            for (int i = 0; i < oynayan.Length; i++) oynayan[i] = sahada.Starters[i].PlayerId;

            int moralDelta = sonuc == WeekResult.Galibiyet ? ms.moralGalibiyet
                           : sonuc == WeekResult.Beraberlik ? ms.moralBeraberlik
                                                            : ms.moralMaglubiyet;

            for (int i = 0; i < st.Oyuncular.Length; i++)
            {
                var p = st.Oyuncular[i];
                if (p.ClubId != clubId) continue;

                bool sahadaydi = false;
                for (int k = 0; k < oynayan.Length; k++) if (oynayan[k] == p.PlayerId) { sahadaydi = true; break; }

                byte yeniKond = YeniKondisyon(p.Kondisyon, sahadaydi, bal);
                if (yeniKond != p.Kondisyon) j.OyuncuSet(i, PlayerField.Kondisyon, yeniKond);

                // MORAL SONUCA GÖRE — ama yalnız MAÇA ÇIKANLAR için tam, çıkmayanlar için yarım.
                // Kulübede oturan oyuncu sonucu yaşar ama kadro dışı kalan kadar etkilenmez;
                // ayrım olmasaydı rotasyon moral tarafında da görünmez olurdu.
                int delta = sahadaydi ? moralDelta : moralDelta / 2;
                int yeniMoral = p.Moral + delta;
                if (yeniMoral < 0) yeniMoral = 0;
                if (yeniMoral > 100) yeniMoral = 100;
                if (yeniMoral != p.Moral) j.OyuncuSet(i, PlayerField.Moral, yeniMoral);
            }
        }
    }
}
