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

    /// <summary>Hava koşulu — ME Spec 12.4. Lig takvim seed'inden DETERMİNİSTİK atanır (FAZ 04);
    /// tüm çarpanlar balance JSON'dadır (hava.*), kodda sabit yoktur.</summary>
    public enum WeatherKind : byte { Kuru = 0, Yagmur = 1, Kar = 2, Sicak = 3 }

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

        // M13 — hava ve zemin (ME 12.4). Zemin tier'ı Tycoon bakım yatırımının sahaya yansımasıdır
        // (GDD 4.3); rüzgar uzun top/orta sapmasına vektör olarak girer.
        public WeatherKind Weather = WeatherKind.Kuru;
        public byte PitchTier = 3;        // 1-2 kötü · 3 nötr · 4-5 iyi
        public double WindMS;             // rüzgar hızı (m/sn)
        public double WindDirX = 1, WindDirY;  // rüzgar yön birim vektörü

        // M15 — detay seviyesi (ME 16.1). Online maçlar ZORUNLU olarak Lod0'dır (16.3 replay
        // + highlight sözleşmesi); Lod2 yalnız arka plan dünya simülasyonunda kullanılır.
        public LodLevel Lod = LodLevel.Lod0;
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
