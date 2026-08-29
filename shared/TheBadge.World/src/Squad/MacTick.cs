using System;
using TheBadge.Sim.Match;

namespace TheBadge.World
{
    /// <summary>MAÇ YAŞAM DÖNGÜSÜ — maç başı/sonu kancaları. `EconomyTick` ile aynı sözleşme:
    /// durumu DOĞRUDAN değiştirmez, yazmalarını `WorldJournal`a kuyruklar, uygulamayı host
    /// `Validate` + `Apply` ile yapar (CLAUDE.md değişmez #1).
    ///
    /// NEDEN VAR (inceleme bulgusu, P1 — iki bağımsız inceleyici): `KalanDegisiklikHakki`
    /// yalnız dünya kurulumunda doluyor, sonra YALNIZ azalıyordu. Alanın adı `macBasinaDegisiklik`
    /// yani "her maç" diyor, ama hiçbir yol hakkı geri doldurmuyordu: bir save'de toplam
    /// `macBasinaDegisiklik` değişiklikten sonra HER maç `NoChargesLeft` ile reddedilirdi.</summary>
    public static class MacTick
    {
        /// <summary>Maç başı — takıma ait maç kapsamlı sayaçları tazeler. Host, maç başlatırken
        /// TAM BİR KEZ çağırır; journal'ı doğrulayıp uygular.</summary>
        public static void Basla(GameState st, WorldRules kural, WorldJournal j)
        {
            if (st == null) throw new ArgumentNullException(nameof(st));
            if (kural == null) throw new ArgumentNullException(nameof(kural));
            if (j == null) throw new ArgumentNullException(nameof(j));

            // HAK SAYISI TEK KAYNAK: motor `MatchEngine.MaxSubs` kadarını onurlandırır; balance
            // bundan FAZLA hak verirse dünya "oldu" der, motor sessizce reddeder (inceleme
            // bulgusu). `K4DegisiklikHakkiKaynagi` kapısı ikisinin eşitliğini zorunlu tutar.
            j.Set(MutTarget.Mac, 0, MatchField.KalanDegisiklikHakki, kural.yapi.macBasinaDegisiklik);
        }

        /// <summary>Balance ile motorun onurlandırdığı tavan aynı mı? Host AÇILIŞTA çağırır —
        /// kablolama hatası maç başlamadan görünür olur.</summary>
        public static bool HakTavaniTutarli(WorldRules kural)
            => kural != null && kural.yapi.macBasinaDegisiklik == MatchEngine.MaxSubs;
    }
}
