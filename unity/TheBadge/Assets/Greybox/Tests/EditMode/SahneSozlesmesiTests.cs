using NUnit.Framework;
using TheBadge.Greybox.Sim;
using UnityEngine;

namespace TheBadge.Greybox.Tests
{
    /// <summary>
    /// Sahne sözleşmesi kapısı — GREYBOX_SAHNELEME.md bölüm 8.
    /// Kod, senaryo dokümanına karşı denetlenir: santra düdüğü anında herkes kendi
    /// yarısında + rakip çember dışında; korner ortası anında kutu dolu; gol topu ağda.
    /// Diziliş emniyeti (timeout) ile başlayan sahneler denetimden muaftır.
    /// </summary>
    public class SahneSozlesmesiTests
    {
        static GreyboxBalance LoadBalance()
        {
            var txt = Resources.Load<TextAsset>("greybox.balance");
            Assert.IsNotNull(txt);
            return JsonUtility.FromJson<GreyboxBalance>(txt.text);
        }

        static bool KickoffOk(FlowSim sim)
        {
            var center = new Vec2(34f, FlowSim.PitchL * 0.5f);
            for (int t = 0; t < 2; t++)
                for (int i = 0; i < 11; i++)
                {
                    var p = sim.GetPlayer(t * 11 + i).Pos;
                    bool ownHalf = t == 0 ? p.Y <= FlowSim.PitchL * 0.5f + 2f : p.Y >= FlowSim.PitchL * 0.5f - 2f;
                    if (!ownHalf) return false;
                    if (t != sim.Possession && Vec2.Distance(p, center) < 8f) return false;
                }
            return true;
        }

        static bool CornerOk(FlowSim sim)
        {
            int atk = sim.Possession;
            float gy = atk == 0 ? FlowSim.PitchL : 0f;
            int atkIn = 0, defIn = 0;
            for (int i = 1; i < 11; i++)
            {
                var pa = sim.GetPlayer(atk * 11 + i).Pos;
                if (Mathf.Abs(pa.X - 34f) <= 22f && Mathf.Abs(pa.Y - gy) <= 18.5f) atkIn++;
                var pd = sim.GetPlayer((1 - atk) * 11 + i).Pos;
                if (Mathf.Abs(pd.X - 34f) <= 22f && Mathf.Abs(pd.Y - gy) <= 18.5f) defIn++;
            }
            return atkIn >= 4 && defIn >= 4;
        }

        [Test]
        public void SahneSozlesmesi_SantraKornerVeGolDenetimi()
        {
            var bal = LoadBalance();
            int kickoffAudits = 0, cornerAudits = 0, violations = 0;

            for (int m = 0; m < 8; m++)
            {
                var sim = new FlowSim(bal, new MatchSetup
                {
                    Seed = 9000UL + (ulong)m * 613UL,
                    HomeTacticId = m % 3,
                    AwayTacticId = (m + 2) % 3,
                    HomeStrength = 60f,
                    AwayStrength = 50f + (m % 4) * 6f
                });

                const float dt = 1f / 60f;
                int steps = 0, maxSteps = 60 * 60 * 10;
                var prevPhase = sim.Phase;
                int prevTimeouts = sim.StagingTimeouts;

                while (!sim.IsFinished && steps < maxSteps)
                {
                    sim.Step(dt);
                    steps++;
                    bool timedOut = sim.StagingTimeouts > prevTimeouts;
                    prevTimeouts = sim.StagingTimeouts;

                    if (prevPhase == FlowPhase.KickOff && sim.Phase != FlowPhase.KickOff && !timedOut)
                    {
                        kickoffAudits++;
                        if (!KickoffOk(sim)) violations++;
                    }
                    if (prevPhase == FlowPhase.CornerSetup && sim.Phase == FlowPhase.CornerCross && !timedOut)
                    {
                        cornerAudits++;
                        if (!CornerOk(sim)) violations++;
                    }
                    prevPhase = sim.Phase;

                    while (sim.TryDequeueEvent(out var e))
                    {
                        if (e.Type != FlowEventType.Goal) continue;
                        var b = sim.BallPos;
                        float goalLine = e.Team == 0 ? FlowSim.PitchL : 0f;
                        float behind = e.Team == 0 ? b.Y - goalLine : goalLine - b.Y;
                        if (Mathf.Abs(b.X - 34f) > 4.4f || behind < 0.5f || behind > 2.4f) violations++;
                    }
                }
                Assert.IsTrue(sim.IsFinished, "maç bitmedi");
            }

            Assert.AreEqual(0, violations, "sahne sözleşmesi ihlali");
            Assert.Greater(kickoffAudits, 8, "santra denetimi hiç koşmadı mı?");
            Assert.Greater(cornerAudits, 0, "korner denetimi hiç koşmadı mı?");
        }
    }
}
