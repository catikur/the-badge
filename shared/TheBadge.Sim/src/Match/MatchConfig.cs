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

    /// <summary>Chaos seviyesi — ME Spec 13.2. Lig/oda kurulumunun seçimidir (GDD 3.4);
    /// sigma tablosu balance'tadır (chaos.*). Varsayılan Orta (13.2 "Default").</summary>
    public enum ChaosLevel : byte { Dusuk = 0, Orta = 1, Yuksek = 2 }

    /// <summary>Maç kurulum girdisi — ME Spec 5.2'nin M1-M4 alt kümesi.
    /// Pitch/Weather/Lod/Chaos alanları kendi dilimlerinde eklenir (ME 12.4, 13, 16.1).
    /// ConfigHash: balance + motor sürümü + kurulumun kanonik özeti (ME 3.3) — replay dörtlüsü üyesi.</summary>
    public sealed class MatchConfig
    {
        public ulong Seed;
        /// <summary>ME 3.3 config_hash — `ConfigHash.Compute(cfg, BalanceHash)` ile doldurulur.
        /// Replay dörtlüsünün kimlik üyesi; motor bunu OKUMAZ (sonuç üretimine girmez).</summary>
        public ulong ConfigHash;
        /// <summary>Balance dosyasının HAM bayt özeti — HOST doldurur (çekirdek JSON parse etmez,
        /// CLAUDE.md bağımlılıksızlık kuralı). config_hash'in girdisi; ME 3.3 sapma notu
        /// `Config/ConfigHash.cs` başlığındadır.</summary>
        public ulong BalanceHash;

        /// <summary>`balance/command.bands.json` ham bayt özeti — config_hash İÇİ
        /// (Atilla kararı, 2026-08-25). Gerekçe: bantlar hangi komutun KABUL edildiğini belirler
        /// → komut zaman çizelgesini → replay'i. Bant değişip hash sabit kalsaydı, aynı zaman
        /// çizelgesi farklı oynar ve "eski replay yeni parametrelerle sessizce oynamaz" güvencesi
        /// delinirdi. `BalanceHash` deseninin ikinci dosyaya genişletilmesi (ME 3.3 sapma notu
        /// burada da geçerli: özeti host hesaplar, çekirdek JSON parse etmez).</summary>
        public ulong CommandBandsHash;
        public string EngineVersion;
        public TeamSheet Home, Away;
        public RefereeProfile Referee = RefereeProfile.Default;

        // M13 — hava ve zemin (ME 12.4). Zemin tier'ı Tycoon bakım yatırımının sahaya yansımasıdır
        // (GDD 4.3); rüzgar uzun top/orta sapmasına vektör olarak girer.
        public WeatherKind Weather = WeatherKind.Kuru;
        public byte PitchTier = 3;        // 1-2 kötü · 3 nötr · 4-5 iyi
        public double WindMS;             // rüzgar hızı (m/sn)
        public double WindDirX = 1, WindDirY;  // rüzgar yön birim vektörü

        // M16-D2 — chaos seviyesi (ME 13.2): 5 enjeksiyon noktasının şiddeti.
        public ChaosLevel Chaos = ChaosLevel.Orta;

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
