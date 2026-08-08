namespace TheBadge.Sim.Match
{
    /// <summary>Maç kurulum girdisi — ME Spec 5.2'nin M1 alt kümesi.
    /// Pitch/Weather/Referee/Lod/Chaos alanları kendi dilimlerinde eklenir (ME 12.4, 11.1, 13, 16.1).
    /// ConfigHash: balance + motor sürümü + kurulumun kanonik özeti (ME 3.3) — replay dörtlüsü üyesi.</summary>
    public sealed class MatchConfig
    {
        public ulong Seed;
        public ulong ConfigHash;
        public string EngineVersion;
        public TeamSheet Home, Away;
    }
}
