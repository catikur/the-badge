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
    /// BURADA NE OLUYOR: maçta OYNAYAN yorulur, OYNAMAYAN dinlenir; moral sonuca göre kayar.
    /// Hepsi journal üzerinden — `EconomyTick` ile aynı desen (bu bir oyuncu KOMUTU değil, haftalık
    /// dünya işleyişi; Tek Kapı komutlar içindir).</summary>
    public static class MacSonrasi
    {
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

                int yeniKond = sahadaydi ? p.Kondisyon - ms.oynayanDusus
                                         : p.Kondisyon + ms.dinlenenArtis;
                if (yeniKond < ms.kondisyonTaban) yeniKond = ms.kondisyonTaban;
                if (yeniKond > 100) yeniKond = 100;
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
