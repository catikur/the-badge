using NUnit.Framework;
using TheBadge.Greybox.Sim;
using UnityEngine;

namespace TheBadge.Greybox.Tests
{
    /// <summary>
    /// FlowSim EditMode kapısı — headless harness'ın Unity içi aynası.
    /// Bantlar gevşektir: amaç pacing regresyonunu ve akış kilitlenmesini yakalamak,
    /// sayı mikro-ayarını değil (o iş playtest + balance dosyasınındır).
    /// </summary>
    public class FlowSimTests
    {
        static GreyboxBalance LoadBalance()
        {
            var txt = Resources.Load<TextAsset>("greybox.balance");
            Assert.IsNotNull(txt, "greybox.balance.json Resources altında olmalı");
            var bal = JsonUtility.FromJson<GreyboxBalance>(txt.text);
            // JsonUtility sessiz kısmi yükleme riskine karşı örnek alan kontrolleri
            Assert.AreEqual(3, bal.taktikler.Length, "3 taktik preseti bekleniyor (Brif K3)");
            Assert.Greater(bal.clock.macSuresiSaniye, 60f);
            Assert.Greater(bal.ekonomi.kapasite, 0);
            Assert.Greater(bal.shot.pGol, 0f);
            return bal;
        }

        static FlowSim RunToEnd(GreyboxBalance bal, MatchSetup setup, out int steps)
        {
            var sim = new FlowSim(bal, setup);
            const float dt = 1f / 60f;
            steps = 0;
            int maxSteps = 60 * 60 * 10; // 10 dk gerçek zaman emniyeti
            while (!sim.IsFinished && steps < maxSteps)
            {
                sim.Step(dt);
                steps++;
                while (sim.TryDequeueEvent(out _)) { }
                var b = sim.BallPos;
                Assert.IsTrue(b.X > -4f && b.X < 72f && b.Y > -5f && b.Y < 110f,
                    $"top saha dışına taştı: {b.X:0.0},{b.Y:0.0}");
            }
            return sim;
        }

        [Test]
        public void MaclarBiterVePacingBandaOturur()
        {
            var bal = LoadBalance();
            int totalGoals = 0, totalShots = 0, totalCorners = 0;
            const int N = 12;
            for (int i = 0; i < N; i++)
            {
                var setup = new MatchSetup
                {
                    Seed = 5000UL + (ulong)i * 977UL,
                    HomeTacticId = i % 3,
                    AwayTacticId = (i + 1) % 3,
                    HomeStrength = 60f,
                    AwayStrength = 52f + (i % 5) * 4f
                };
                var sim = RunToEnd(bal, setup, out _);
                Assert.IsTrue(sim.IsFinished, "maç emniyet tavanına takıldı (akış kilitlenmesi?)");
                Assert.GreaterOrEqual(sim.MatchMinute, 89.9f);
                totalGoals += sim.HomeScore + sim.AwayScore;
                totalShots += sim.Stats.TotalShots;
                totalCorners += sim.Stats.TotalCorners;
            }
            float avgGoals = (float)totalGoals / N;
            float avgShots = (float)totalShots / N;
            float avgCorners = (float)totalCorners / N;
            Assert.That(avgGoals, Is.InRange(1.0f, 4.5f), "ortalama gol bandı");
            Assert.That(avgShots, Is.InRange(6f, 24f), "ortalama şut bandı");
            Assert.That(avgCorners, Is.InRange(1f, 14f), "ortalama korner bandı");
        }

        [Test]
        public void AyniSeedAyniSonuc_DeterminizmLite()
        {
            // Tam determinizm FAZ 03 kapısıdır (Brif K5); bu test yalnız System.Random
            // benzeri kaçak rastgelelik sızmasını yakalar.
            var bal = LoadBalance();
            var setup = new MatchSetup
            { Seed = 777UL, HomeTacticId = 1, AwayTacticId = 2, HomeStrength = 60f, AwayStrength = 60f };
            var a = RunToEnd(bal, setup, out _);
            var b = RunToEnd(bal, setup, out _);
            Assert.AreEqual(a.HomeScore, b.HomeScore);
            Assert.AreEqual(a.AwayScore, b.AwayScore);
            Assert.AreEqual(a.Stats.TotalShots, b.Stats.TotalShots);
            Assert.AreEqual(a.Stats.TotalCorners, b.Stats.TotalCorners);
        }

        [Test]
        public void SkipHedefi_OnemliAnPenceresineUlasir()
        {
            var bal = LoadBalance();
            var sim = new FlowSim(bal, new MatchSetup
            { Seed = 31337UL, HomeTacticId = 0, AwayTacticId = 0, HomeStrength = 60f, AwayStrength = 60f });

            // MatchDirector.SkipToKeyMoment ile aynı döngü deseni
            float spent = 0f;
            while (!sim.IsFinished && !sim.InKeyMoment && spent < bal.clock.macSuresiSaniye * 4f)
            {
                sim.Step(bal.clock.skipDilimSaniye);
                spent += bal.clock.skipDilimSaniye;
            }
            Assert.IsTrue(sim.InKeyMoment || sim.IsFinished,
                "skip ne önemli ana ne maç sonuna ulaştı — akış üretimi bozuk");
        }
    }
}
