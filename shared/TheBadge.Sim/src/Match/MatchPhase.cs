namespace TheBadge.Sim.Match
{
    /// <summary>Maç faz makinesi — ME Spec 4.1. Faz geçişleri yalnız event üretir;
    /// UI fazı event log'dan okur (Interrupt Abstraction, GDD 15.1).</summary>
    public enum MatchPhase : byte
    {
        Kickoff = 0,
        OpenPlay = 1,
        DeadBall = 2,
        SetPiece = 3,
        Penalty = 4,
        HalfTime = 5,
        FullTime = 6,
        VarReview = 7
    }
}
