namespace TheBadge.Sim.Core
{
    /// <summary>Kalıcı durum TAMSAYIDIR (mm) — ME Spec 3.2. Ara hesap double, sonuç kuantalanır.</summary>
    public static class Units
    {
        public const int MmPerMeter = 1000;
        public static int QuantizeMm(double meters) => (int)System.Math.Round(meters * MmPerMeter);
        public static double ToMeters(int mm) => mm / 1000.0;
    }
}
