namespace TheBadge.Sim.Core
{
    /// <summary>Kalıcı durum TAMSAYIDIR (mm) — ME Spec 3.2. Ara hesap double, sonuç kuantalanır.</summary>
    public static class Units
    {
        public const int MmPerMeter = 1000;
        /// <summary>Metre → mm (int). Dönüşüm DetMath.ToInt32Sat'tan geçer: ham <c>(int)</c> NaN'da x64'te
        /// int.MinValue, arm64'te 0 veriyordu — P0'ın macOS sapmasının kök nedeni (DECISIONS, P0 (a)).</summary>
        public static int QuantizeMm(double meters) => DetMath.ToInt32Sat(System.Math.Round(meters * MmPerMeter));
        public static double ToMeters(int mm) => mm / 1000.0;
    }
}
