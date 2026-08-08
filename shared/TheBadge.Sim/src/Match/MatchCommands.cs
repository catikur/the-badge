namespace TheBadge.Sim.Match
{
    /// <summary>Taktik delta — CB Spec katalog `squad.set_team_tactic {mentalite, tempo, pres, hat}`
    /// alanlarının maç içi karşılığı (ME Spec 5.4/14.2). Bantlar M-müdahale diliminde doğrulanır.</summary>
    public readonly struct TacticDelta
    {
        public readonly sbyte Mentalite, Tempo, Pres, Hat;
        public TacticDelta(sbyte mentalite, sbyte tempo, sbyte pres, sbyte hat)
        { Mentalite = mentalite; Tempo = tempo; Pres = pres; Hat = hat; }
    }

    /// <summary>Bireysel talimat kataloğu — M-müdahale dilimi genişletir (ME 14.2).</summary>
    public enum PlayerInstr : byte { None = 0 }

    /// <summary>Motivasyon konuşması tonları — ME Spec 14.3.</summary>
    public enum ToneType : byte { Sakinlestir = 0, Atesle = 1, Uyar = 2 }

    /// <summary>Maç içi komut sözleşmeleri — ME Spec 5.4. Komutlar Command Bus zarfından
    /// (CB Spec 3.1) çözülüp bu kayıtlara çevrilir; uygulanma ANLARI ME 14.2'ye tabidir.</summary>
    public abstract record MatchCommand(uint IssueTick, byte TeamIdx);

    public sealed record SubstitutionCmd(uint IssueTick, byte TeamIdx, short OutId, short InId)
        : MatchCommand(IssueTick, TeamIdx);

    public sealed record TacticChangeCmd(uint IssueTick, byte TeamIdx, TacticDelta Delta)
        : MatchCommand(IssueTick, TeamIdx);

    public sealed record InstructionCmd(uint IssueTick, byte TeamIdx, short PlayerId, PlayerInstr Instr)
        : MatchCommand(IssueTick, TeamIdx);

    public sealed record MotivationCmd(uint IssueTick, byte TeamIdx, ToneType Tone)
        : MatchCommand(IssueTick, TeamIdx);
}
