namespace TheBadge.Sim.Match
{
    /// <summary>
    /// Nitelik seti (1-100 ölçeği) — ME Spec 6.1 tablosu BİREBİR.
    /// Taban değerler SALT-OKUNUR taşınır (TeamSheet); maç içi bağlam çarpanları
    /// EffectiveAttributes üzerinden her kullanımda türetilir, taban ASLA mutasyona uğramaz (ME 5.2).
    /// Kaleci grubu saha oyuncularında da vardır (düşük değerli) — kaleci modeli Bölüm 9-11 tüketir.
    /// </summary>
    public struct PlayerAttributes
    {
        // Teknik — ME 6.1
        public byte Passing, Finishing, Dribbling, Tackling, Heading, FirstTouch, Crossing, SetPieces;

        // Zihinsel — ME 6.1
        public byte Positioning, Decisions, Composure, Aggression, Workrate, Vision;

        // Fiziksel — ME 6.1
        public byte Pace, Acceleration, Stamina, Strength, Agility, JumpReach;

        // Kaleci — ME 6.1 (Reflexes t_react'i, Handling tutuşu, Kicking/Throwing dağıtımı besler; ME 9-11)
        public byte Reflexes, Handling, OneOnOne, AerialCommand, Kicking, Throwing;
    }
}
