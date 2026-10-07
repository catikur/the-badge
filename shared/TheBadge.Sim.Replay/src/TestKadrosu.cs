using TheBadge.Sim.Determinism;
using TheBadge.Sim.Match;

namespace TheBadge.Sim.Replay
{
    /// <summary>Test kadrosu — golden replay setinin ve Checks kapılarının ORTAK kadro tanımı
    /// (ADR-004). Checks'teki `BuildSheetSide` buraya taşındı ki sunucu ve istemci aynı kadroyu
    /// kursun; tek alanın değişmesi 50 golden kaydının hepsini değiştirir ve yeniden üretim ister
    /// (ME 17.4).</summary>
    public static class TestKadrosu
    {
        /// <summary>Gerçekçi 4-4-2 çapalı 16 kişilik kadro (11 + 5 yedek). Ev −x yarı sahada,
        /// deplasman aynalı; nitelikler tohumdan deterministik türetilir (üretim kadroları FAZ 04
        /// veri katmanından gelir). <paramref name="idEntity"/> 0 ise <paramref name="entity"/>
        /// kullanılır: ayna kadroda nitelikler aynı, PlayerId farklı. <paramref name="offset"/> güç
        /// kademesidir (LOD 2 regresyonu, M16 dağılımları); 0 = üretim kadrosu.</summary>
        public static TeamSheet Kur(ulong seed, uint entity, bool home, uint idEntity = 0, int offset = 0)
        {
            if (idEntity == 0) idEntity = entity;
            var sheet = new TeamSheet { Starters = new PlayerEntry[11], Bench = new PlayerEntry[5] };
            int sign = home ? -1 : 1;
            for (int i = 0; i < 16; i++)
            {
                byte V(uint salt)
                {
                    // Zar gerekçesi: kadro niteliği bir kurulum kararıdır — DECISION domain (ME 3.1)
                    int v = 35 + (int)(Rng.Rand01(seed, Domain.Decision, entity, (uint)i, salt) * 50) + offset;
                    return (byte)(v < 1 ? 1 : v > 100 ? 100 : v);
                }
                int ax, ay;
                if (i == 0) { ax = 48000; ay = 0; }                                   // KL
                else if (i < 5) { ax = 33000; ay = (i - 1) * 16000 - 24000; }         // DF hattı
                else if (i < 9) { ax = 12000; ay = (i - 5) * 16000 - 24000; }         // OS hattı
                else { ax = 3000; ay = i == 9 ? -8000 : 8000; }                       // FV ikilisi
                var e = new PlayerEntry
                {
                    PlayerId = (short)(idEntity * 100 + i),
                    Name = "Test-" + idEntity.ToString(System.Globalization.CultureInfo.InvariantCulture)
                           + "-" + i.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    RoleId = (byte)(i == 0 ? 1 : i < 5 ? 2 : i < 9 ? 3 : 4),
                    AnchorXmm = sign * ax,
                    AnchorYmm = ay,
                    // TÜM nitelikler doldurulur — eksik bırakılan nitelik 0 olur ve o alt sistem
                    // (kaleci 1v1'i, hava topu, faul agresifliği) sessizce ölür; M4'te yakalandı
                    Attributes = new PlayerAttributes
                    {
                        Passing = V(1), Finishing = V(2), Dribbling = V(7), Tackling = V(8),
                        Heading = V(16), FirstTouch = V(9), Crossing = V(22), SetPieces = V(18),
                        Positioning = V(10), Decisions = V(23), Composure = V(12), Aggression = V(19),
                        Workrate = V(24), Vision = V(11),
                        Pace = V(3), Acceleration = V(13), Stamina = V(4), Strength = V(14),
                        Agility = V(15), JumpReach = V(17),
                        Reflexes = V(5), Handling = V(6), OneOnOne = V(20), AerialCommand = V(21),
                        Kicking = V(25), Throwing = V(26)
                    }
                };
                if (i < 11) sheet.Starters[i] = e; else sheet.Bench[i - 11] = e;
            }
            return sheet;
        }
    }
}
