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
        public int SpinY;        // falso bileşeni (mm/sn2 eşleniği) — M3+ orta/frikik kullanır (ME 8.3)
        public short OwnerId;    // -1 = serbest top ("aynı anda iki sahip" yapısal imkansız, ME 4.3)
        public byte LastTouchTeam; // 0/1; 2 = henüz dokunulmadı — taç/aut sahipliği bundan türer (M2)
        public byte Flight;      // M3 şut uçuşu: 0 serbest · 1 karara bağlı (kimse alamaz — 9.2
                                 // analitik çözüm sahneleniyor) · 2 kaleci tutuşu (yalnız savunan GK alır)
    }

    /// <summary>Ajan kalıcı durumu — ME Spec 5.3 şeması birebir; float alan YOK.</summary>
    public struct PlayerAgentState
    {
        public short Id;
        public byte TeamIdx;             // 0 ev / 1 deplasman
        public byte RoleId;              // rol tablosu M-karar diliminde (ME 7.4)
        public int X, Y, Vx, Vy;         // mm, mm/sn
        public int AnchorX, AnchorY;     // kullanıcının serbest diziliş çapası (taktik girdisi)
        public int TargetX, TargetY;     // karar çıktısı hedef nokta (mm) — tick'ler arası taşınır (M2)
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
    /// <summary>Duran top türü — ME Spec 10; 0 = açık oyun.</summary>
    public enum SetPieceType : byte
    {
        None = 0, ThrowIn = 1, GoalKick = 2, Corner = 3, FreeKick = 4, Penalty = 5, Kickoff = 6
    }

    public struct MatchState
    {
        public uint Tick;
        public MatchPhase Phase;
        public int HomeGoals, AwayGoals;
        public BallState Ball;
        public PlayerAgentState[] Agents;   // [22] — maç başında bir kez tahsis (zero-alloc sıcak yol, ME 16.2)
        public TeamRuntime HomeRt, AwayRt;
        public ulong LastChecksum;          // 600 tick kadansıyla yazılır (ME 3.2); hash'e DAHİL DEĞİL

        // M4 — duran top + saat (ME 10, 3.4)
        public SetPieceType SetPiece;       // bekleyen duran top türü
        public byte SetPieceTeam;           // kullanacak takım
        public short SetPieceTaker;         // kullanacak oyuncu slotu (-1 = henüz atanmadı)
        public uint StoppageTicks;          // duraklama birikimi → uzatma (ME 3.4)
        public byte Half;                   // 1 ilk devre, 2 ikinci devre
        public uint HalfEndTick;            // bu devrenin bitiş tick'i (uzatma dahil hesaplanır)
    }
}
