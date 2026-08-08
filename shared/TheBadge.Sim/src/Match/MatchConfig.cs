namespace TheBadge.Sim.Match
{
    /// <summary>Hakem profili — ME Spec 11.1. HomeBias YOKTUR (adalet ilkesi, spec sabiti).
    /// Lig hakem havuzundan REFEREE domain'iyle seçim FAZ 04 veri katmanında.</summary>
    public struct RefereeProfile
    {
        public byte Strictness;          // 0-100 — foul eşiğini kaydırır (11.2)
        public byte AdvantageTendency;   // 0-100 — avantaj oynatma eğilimi
        public byte Consistency;         // 0-100 — gri bant kararlılığı

        public static RefereeProfile Default => new RefereeProfile
        { Strictness = 50, AdvantageTendency = 50, Consistency = 60 };
    }

    /// <summary>Maç kurulum girdisi — ME Spec 5.2'nin M1-M4 alt kümesi.
    /// Pitch/Weather/Lod/Chaos alanları kendi dilimlerinde eklenir (ME 12.4, 13, 16.1).
    /// ConfigHash: balance + motor sürümü + kurulumun kanonik özeti (ME 3.3) — replay dörtlüsü üyesi.</summary>
    public sealed class MatchConfig
    {
        public ulong Seed;
        public ulong ConfigHash;
        public string EngineVersion;
        public TeamSheet Home, Away;
        public RefereeProfile Referee = RefereeProfile.Default;
    }

    /// <summary>Maç sonucu — headless koşunun çıktısı (ME Spec 5.1 IMatchEngine.Run).
    /// Tam veri paketi (event log, xG dökümü) 15.4 diliminde genişler.</summary>
    public sealed class MatchResult
    {
        public int HomeGoals, AwayGoals;
        public uint TotalTicks;
        public uint StoppageTicks;
        public int Shots, Saves, Fouls, Yellows, Reds, Corners, Penalties;
        public double XgHome, XgAway;
        public ulong FinalChecksum;
    }
}
