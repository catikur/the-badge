namespace TheBadge.Sim.Match
{
    /// <summary>Sakatlık durumu — ME Spec 12.2 dilimi (M-durum) sınıfları genişletir;
    /// kalıcı durumda tamsayı disiplini gereği byte tabanlı enum. — ME Spec 5.3</summary>
    public enum InjuryState : byte
    {
        None = 0
        // Hafif / Orta / Ağır sınıfları 12.2 diliminde eklenir (şiddet dağılımı [KALİBRE])
    }

    /// <summary>Top durumu — yalnız tamsayı (mm, mm/sn): ME Spec 3.2 + 5.2.</summary>
    public struct BallState
    {
        public int X, Y, Z;      // mm
        public int Vx, Vy, Vz;   // mm/sn
        public int SpinY;        // falso bileşeni (mm/sn2 eşleniği) — fizik dilimi kullanır (ME 8)
        public short OwnerId;    // -1 = serbest top ("aynı anda iki sahip" yapısal imkansız, ME 4.3)
    }

    /// <summary>Ajan kalıcı durumu — ME Spec 5.3 şeması birebir; float alan YOK.</summary>
    public struct PlayerAgentState
    {
        public short Id;
        public byte TeamIdx;             // 0 ev / 1 deplasman
        public byte RoleId;              // rol tablosu M-karar diliminde (ME 7.4)
        public int X, Y, Vx, Vy;         // mm, mm/sn
        public int AnchorX, AnchorY;     // kullanıcının serbest diziliş çapası (taktik girdisi)
        public ushort Energy;            // 0-1000 (0,1 hassasiyet) — ME 12.1
        public sbyte Momentum;           // -10..+10 — ME 12.3
        public byte YellowCards;
        public bool SentOff;
        public InjuryState Injury;
        public byte CurrentAction;       // aksiyon kataloğu M-karar diliminde
        public uint ActionUntilTick;
    }

    /// <summary>Takım koşu-zamanı durumu — ME Spec 5.2 (hat yüksekliği, pres modu, momentum).</summary>
    public struct TeamRuntime
    {
        public int LineHeightMm;         // savunma hattı (mm) — taktik delta hedefi (ME 14.2)
        public byte PressMode;
        public sbyte Momentum;           // -10..+10 takım momentumu (ME 12.3)
    }

    /// <summary>Maçın KALICI durumu — yalnız tamsayı alanlar (ME Spec 3.2/5.2).
    /// Checksum bu yapının kanonik serileştirmesinden alınır (MatchEngine.StateHash).</summary>
    public struct MatchState
    {
        public uint Tick;
        public MatchPhase Phase;
        public int HomeGoals, AwayGoals;
        public BallState Ball;
        public PlayerAgentState[] Agents;   // [22] — maç başında bir kez tahsis (zero-alloc sıcak yol, ME 16.2)
        public TeamRuntime HomeRt, AwayRt;
        public ulong LastChecksum;          // 600 tick kadansıyla yazılır (ME 3.2); hash'e DAHİL DEĞİL
    }
}
