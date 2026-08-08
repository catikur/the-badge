using System;
using System.Collections.Generic;

namespace TheBadge.Greybox.Sim
{
    /// <summary>Vinyetin tek karesi: 22 oyuncu + top + yükseklik.</summary>
    public struct VignetteFrame
    {
        public Vec2[] Players;   // 22
        public Vec2 Ball;
        public float BallH;
        public bool GoalMoment;  // vurgu paketi bu karede tetiklenir
    }

    /// <summary>
    /// Highlight vinyeti üretici — Model Maçı'nda gol blokları için 2D sahne kaydı (Sahneleme §0).
    /// FlowSim'i headless koşar, istenen takımın golünü arar, golden önceki N saniyeyi kare
    /// kare döndürür. Sahiplik/sahne kuralları FlowSim'de olduğu gibi geçerlidir.
    /// Saf C#: headless test edilebilir.
    /// </summary>
    public static class VignetteRecorder
    {
        const float Dt = 0.05f; // MatchDirector.SimStep ile aynı — oynatma 1:1

        /// <summary>scorerTeam: 0 = biz (ev). Gol bulunamazsa en iyi anın (şut) kaydını döndürür;
        /// hiç an yoksa null (sunum vinyetsiz devam eder).</summary>
        public static List<VignetteFrame> RecordGoal(GreyboxBalance bal, ulong seed, int scorerTeam,
                                                     int usTacticId, int themTacticId,
                                                     float usStrength, float themStrength)
        {
            int keepFrames = (int)(bal.model.vinyetKayitSn / Dt);
            int maxSteps = (int)(bal.model.vinyetMaxSimSn / Dt);

            List<VignetteFrame> best = null;
            for (uint attempt = 0; attempt < 5; attempt++)
            {
                // Denemeler farklı tohum akışlarıyla — Domain.Crowd (yalnız sunum kozmetiği)
                ulong s = TheBadge.Sim.Determinism.Rng.Hash64(seed, 8, 900 + (uint)scorerTeam, attempt, 4);
                var setup = new MatchSetup
                {
                    Seed = s,
                    HomeTacticId = scorerTeam == 0 ? usTacticId : themTacticId,
                    AwayTacticId = scorerTeam == 0 ? themTacticId : usTacticId,
                    // Gol HER ZAMAN ev yönünde aranır → ev, GOL ATACAK TARAFI temsil eder ve
                    // güçlendirilir (iterasyon 10 düzeltmesi: rakip golünde eğim yanlış taraftaydı
                    // → gol bulunamıyor, vinyet atlanıyordu)
                    HomeStrength = (scorerTeam == 0 ? usStrength : themStrength) + 8f,
                    AwayStrength = scorerTeam == 0 ? themStrength : usStrength
                };
                var sim = new FlowSim(bal, setup);
                var ring = new Queue<VignetteFrame>(keepFrames + 4);
                List<VignetteFrame> shotFallback = null;

                for (int step = 0; step < maxSteps && !sim.IsFinished; step++)
                {
                    sim.Step(Dt);
                    var f = Capture(sim);
                    if (ring.Count >= keepFrames) ring.Dequeue();
                    ring.Enqueue(f);

                    while (sim.TryDequeueEvent(out var e))
                    {
                        // Vinyette gol her zaman EV yönünde aranır (setup çevrildi)
                        if (e.Type == FlowEventType.Goal && e.Team == 0)
                        {
                            var frames = new List<VignetteFrame>(ring);
                            if (frames.Count > 0)
                            {
                                var last = frames[frames.Count - 1];
                                last.GoalMoment = true;
                                frames[frames.Count - 1] = last;
                            }
                            // KUTLAMA da kaydedilir: top ağlarda, takım skorerde kümelenir —
                            // sevinç TAM izlenir, ekran aceleyle kapanmaz (Atilla, iterasyon 9)
                            int celebSteps = (int)(bal.model.vinyetKutlamaSn / Dt);
                            for (int cs = 0; cs < celebSteps && !sim.IsFinished; cs++)
                            {
                                sim.Step(Dt);
                                frames.Add(Capture(sim));
                                while (sim.TryDequeueEvent(out _)) { }
                            }
                            if (scorerTeam == 1) MirrorFrames(frames); // rakip golü: sahneyi aynala
                            return frames;
                        }
                        if ((e.Type == FlowEventType.Shot || e.Type == FlowEventType.CornerHeader) && e.Team == 0 && shotFallback == null)
                            shotFallback = new List<VignetteFrame>(ring);
                    }
                }
                if (best == null && shotFallback != null)
                {
                    if (scorerTeam == 1) MirrorFrames(shotFallback);
                    best = shotFallback;
                }
            }
            return best;
        }

        static VignetteFrame Capture(FlowSim sim)
        {
            var f = new VignetteFrame { Players = new Vec2[22], Ball = sim.BallPos, BallH = sim.BallHeight };
            for (int i = 0; i < 22; i++) f.Players[i] = sim.GetPlayer(i).Pos;
            return f;
        }

        /// <summary>Rakip golü: sahne dikeyde aynalanır VE takım blokları değiştirilir —
        /// gol atan taraf ekranda doğru renk ve doğru kaleyle görünsün.</summary>
        static void MirrorFrames(List<VignetteFrame> frames)
        {
            for (int k = 0; k < frames.Count; k++)
            {
                var f = frames[k];
                var swapped = new Vec2[22];
                for (int i = 0; i < 11; i++)
                {
                    swapped[i] = new Vec2(f.Players[11 + i].X, FlowSim.PitchL - f.Players[11 + i].Y);
                    swapped[11 + i] = new Vec2(f.Players[i].X, FlowSim.PitchL - f.Players[i].Y);
                }
                f.Players = swapped;
                f.Ball = new Vec2(f.Ball.X, FlowSim.PitchL - f.Ball.Y);
                frames[k] = f;
            }
        }
    }
}
