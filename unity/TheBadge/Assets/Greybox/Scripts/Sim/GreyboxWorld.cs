using TheBadge.Sim.Determinism;

namespace TheBadge.Greybox.Sim
{
    /// <summary>
    /// Greybox kurgusal evren verisi — GDD 10 gereği tüm isimler kurgusaldır,
    /// gerçek kulüp/oyuncu benzerliği yoktur. FAZ 05 içerik hattı gelene dek yer tutucu.
    /// </summary>
    public static class GreyboxWorld
    {
        public const string PlayerClubName = "Rozet SK"; // oyuncunun kulübü ("The Badge" göndermesi)

        public static readonly string[] OpponentClubs =
        {
            "Kuzey Feneri SK", "Anadolu Şimşekleri", "Liman İdmanyurdu", "Toros Kartalları",
            "Çelikkent Gücü", "Boğaz FK", "Efeler Birliği", "Yıldız Ovası SK"
        };

        /// <summary>Maç öncesi panelde gösterilen 11 kişilik kurgusal kadro (sıra: formasyon indeksi).</summary>
        public static readonly string[] Squad =
        {
            "K. Denizli (KL)",
            "E. Demirbaş", "T. Yalçınkaya", "M. Korkmazer", "S. Aydoğdu",
            "B. Çevikel", "H. Sarpkan", "O. Gürbüzer", "C. Akıncıoğlu",
            "A. Bozdoğan", "V. Şahinkaya"
        };

        /// <summary>Maç günü kurulumunu seed'ten türetir: rakip, gücü ve taktiği tekrarlanabilir olsun.</summary>
        public static MatchSetup BuildMatch(GreyboxBalance bal, ulong worldSeed, int matchIndex, int playerTacticId)
        {
            uint m = (uint)matchIndex;
            // Rakip seçimi/gücü — Domain.Decision: greybox dünya üretimi (kozmetik akış)
            double rStrength = Rng.Rand01(worldSeed, Domain.Decision, 1000, m, 1);
            double rTactic = Rng.Rand01(worldSeed, Domain.Decision, 1000, m, 2);

            var setup = new MatchSetup
            {
                Seed = Rng.Hash64(worldSeed, (uint)Domain.Decision, 2000, m, 3),
                HomeTacticId = playerTacticId,
                AwayTacticId = (int)(rTactic * bal.taktikler.Length) % bal.taktikler.Length,
                HomeStrength = bal.takimlar.oyuncuTakimGucu,
                AwayStrength = (float)(bal.takimlar.rakipGucMin +
                                       rStrength * (bal.takimlar.rakipGucMax - bal.takimlar.rakipGucMin))
            };
            return setup;
        }

        public static string OpponentName(int matchIndex) =>
            OpponentClubs[(matchIndex - 1) % OpponentClubs.Length];
    }
}
