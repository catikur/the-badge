namespace TheBadge.Sim.Match
{
    /// <summary>Event tipleri — ME Spec 15.1 tablosu birebir (6 kategori). Değerler kategori
    /// başına bloklanmıştır: yeni tip eklemek eski kayıtların anlamını KAYDIRMAZ (event log
    /// FAZ 04'te kalıcı yazılır — LLM röportajı, Hikaye Motoru ve Panorama'nın tek kaynağı).</summary>
    public enum EventType : ushort
    {
        None = 0,

        // --- Top akışı
        PassCompleted = 1,
        PassIntercepted = 2,
        CrossDelivered = 3,
        TouchError = 4,
        DribblePast = 5,
        TackleWon = 6,
        BallOut = 7,

        // --- Şut zinciri
        ShotOnTarget = 20,
        ShotOffTarget = 21,
        ShotBlocked = 22,
        Goal = 23,
        Save = 24,
        Parry = 25,
        Post = 26,
        BigChanceMissed = 27,
        AssistRecorded = 28,

        // --- Duran top
        CornerAwarded = 40,
        FreeKickAwarded = 41,
        PenaltyAwarded = 42,
        ThrowIn = 43,
        Offside = 44,

        // --- Disiplin
        FoulCommitted = 60,
        YellowCard = 61,
        RedCard = 62,
        AdvantagePlayed = 63,

        // --- VAR
        VarReviewStarted = 80,
        VarDecision = 81,

        // --- Durum
        PhaseChange = 100,
        Substitution = 101,
        TacticChange = 102,
        MotivationTalk = 103,
        InjuryOccurred = 104,
        MomentumShift = 105,
        StaminaAlert = 106
    }

    /// <summary>Event bayrakları — ME Spec 15.1 `Flags` alanı.</summary>
    [System.Flags]
    public enum EventFlags : byte
    {
        None = 0,
        BigChance = 1,        // xG eşiği üstü fırsat
        FastBreak = 2,        // kontra penceresinde gerçekleşti (ME 7.2 geçiş)
        SetPieceKaynakli = 4, // duran toptan doğdu
        VarAdayi = 8          // gri bantta — VAR incelemesine aday
    }

    /// <summary>Maç olayı — ME Spec 15.1 şeması birebir.
    /// KAYITTIR, DURUM DEĞİLDİR: `MatchEngine.StateHash` bu yapıya BAKMAZ ve simülasyon event
    /// log'undan okumaz. Tek yönlü akış determinizmin ön koşuludur — log'a bakan bir karar,
    /// halka tamponun taşma noktasında davranışı değiştirirdi.</summary>
    public struct MatchEvent
    {
        public uint Tick;      // simülasyon zamanı
        public ushort Type;    // EventType
        public short ActorA;   // birincil oyuncu (yoksa -1)
        public short ActorB;   // ikincil oyuncu (pas alıcısı, faul mağduru…) (yoksa -1)
        public byte TeamIdx;   // 0 ev · 1 deplasman · 2 nötr
        public int X, Y;       // mm konum
        public int AuxData;    // tipe özel (şiddet skoru ×1000, VAR sınıfı, kart tipi…)
        public float Xg;       // yalnız şut tiplerinde dolu (ME 15.2 kaydı)
        public byte Flags;     // EventFlags

        /// <summary>Dakika (sunum ve highlight için) — 600 tick = 1 dk (ME 3.4).</summary>
        public int Minute => (int)(Tick / 600);
        public EventType Kind => (EventType)Type;
        public bool Has(EventFlags f) => (Flags & (byte)f) != 0;
    }

    /// <summary>Takım istatistik satırı — ME Spec 15.4 "temel istatistik satırı".</summary>
    public struct MatchStatLine
    {
        public int Goals, Shots, ShotsOnTarget, Corners, Fouls, Yellows, Reds, Offsides;
        public int Passes, PassesCompleted;
        public double Xg;
        public double PossessionPct;   // topa sahip olunan tick oranı (%)
    }

    /// <summary>Maç sonu veri paketi — ME Spec 15.4. Röportaj promptu (GDD 5.5), Hikaye Motoru
    /// beat üretimi (GDD 7.2) ve Panorama seçici (GDD 8.4) BU paketi tüketir.
    /// HAM EVENT LOG LLM'E ASLA VERİLMEZ (spec 15.4: token + determinizm disiplini) — paket
    /// bilinçli olarak sınırlıdır: en yüksek 10 an + iki eğri + özetler.</summary>
    public sealed class MatchSummaryPacket
    {
        public int HomeGoals, AwayGoals;
        public MatchStatLine Home, Away;

        /// <summary>Highlight puanına (ME 15.3) göre sıralı en yüksek 10 an.</summary>
        public MatchEvent[] TopEvents = new MatchEvent[0];
        public double[] TopScores = new double[0];

        /// <summary>ZAMAN ÇİZELGESİ İŞARETLERİ — sunum katmanının bastığı anlar.
        ///
        /// EŞİKTEN DEĞİL, EN YÜKSEK N'DEN beslenir (M14 bulgusunun kararı, 2026-08-31). ME 15.3'ün
        /// `H > eşik` ölçütü ölçümde maç başına 0,5-0,8 işaret veriyordu — yani maçların yarısında
        /// zaman çizelgesi BOŞ kalıyordu. Eşiği düşürmek spec'e dokunmak olurdu; bunun yerine eşik
        /// OLDUĞU GİBİ kalıyor ve `HighlightCount`u beslemeyi sürdürüyor (o, ME 15.3'ün tanımıdır),
        /// çizelge ise sabit sayıda en yüksek andan doluyor. İki büyüklük AYRI: biri "kaç an
        /// eşiği geçti", öteki "kullanıcıya kaç işaret gösterilir". ME 17.5 "ayar sahası" ilkesi.</summary>
        public MatchEvent[] TimelineMarks = new MatchEvent[0];
        public double[] TimelineScores = new double[0];

        /// <summary>Dakika başına örneklem [90] — ME 15.3 "90 nokta örneklem".</summary>
        public sbyte[] MomentumHome = new sbyte[0];
        public sbyte[] MomentumAway = new sbyte[0];
        public float[] WinProbHome = new float[0];

        /// <summary>5G S2 — ÜÇ SONUÇLU canlı olasılık, dakika başına [90]. `WinProbHome`un yerine
        /// GEÇMEZ: o, ME 15.3 highlight sıralamasının çekirdeğidir (gol farkı + dakika, göreli
        /// sıçrama için yeterli). Bunlar SUNUM yüzeyidir — güç farkına, kırmızı karta ve oyuncu
        /// değişikliğine duyarlı, `LiveWinProb` ile kalibre (`S2WinProbKalibrasyon`).
        /// Üçü her dakikada 1'e toplanır.</summary>
        public float[] WinProb3Home = new float[0];
        public float[] WinProb3Draw = new float[0];
        public float[] WinProb3Away = new float[0];

        /// <summary>Aktif hikaye arkına dokunan olaylar — FAZ 04 Hikaye Motoru bağlanınca dolar
        /// (ME 15.3 "hikaye_ilgisi" ile aynı kanca). FAZ 03'te bilinçli olarak BOŞ.</summary>
        public MatchEvent[] ArcEvents = new MatchEvent[0];

        public RefereeProfile Referee;
        public WeatherKind Weather;
        public byte PitchTier;
        public double WindMS;

        /// <summary>H > eşik olan an sayısı (zaman çizelgesi işareti — GDD 5.6).</summary>
        public int HighlightCount;
        public uint TotalTicks;
        public int EventCount;
        public int EventsDropped;   // halka tamponu taştıysa kaç olay düştü (0 olmalı)
    }
}
