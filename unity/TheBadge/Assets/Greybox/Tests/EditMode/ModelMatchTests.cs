using NUnit.Framework;
using TheBadge.Greybox.Loop;
using TheBadge.Greybox.Sim;
using TheBadge.Sim.Commands;
using UnityEngine;

namespace TheBadge.Greybox.Tests
{
    /// <summary>Model Maçı kapısı — Sahneleme §0: olasılıklar, DP kazanma şeridi, müdahaleler.</summary>
    public class ModelMatchTests
    {
        static GreyboxBalance Bal()
        {
            var txt = Resources.Load<TextAsset>("greybox.balance");
            Assert.IsNotNull(txt);
            var b = JsonUtility.FromJson<GreyboxBalance>(txt.text);
            Assert.Greater(b.model.blokSayisi, 0, "model bölümü yüklenmeli");
            return b;
        }

        static MatchSetup Setup(ulong seed, float us = 60f, float them = 60f, int tac = 0) =>
            new MatchSetup { Seed = seed, HomeTacticId = tac, AwayTacticId = 0, HomeStrength = us, AwayStrength = them };

        /// <summary>Bloğu oynatır; sakatlık kararı düşerse "eksik devam" politikasıyla çözer (İt.11).</summary>
        static void Advance(MatchModel m)
        {
            m.ResolveNext();
            if (m.HasPendingDecision) m.TryContinueShort();
        }

        [Test]
        public void KazanmaSeridi_Toplami1_VeSimetrik()
        {
            var m = new MatchModel(Bal(), Setup(11));
            var p = m.ComputeWinProb();
            Assert.AreEqual(1f, p.Win + p.Draw + p.Loss, 1e-3f, "dağılım 1'e toplanmalı");
            Assert.AreEqual(p.Win, p.Loss, 0.02f, "eşit güçte simetrik olmalı");
        }

        [Test]
        public void GucluTakim_KazanmaOlasiligiYuksek()
        {
            var strong = new MatchModel(Bal(), Setup(12, us: 72f, them: 48f)).ComputeWinProb();
            var weak = new MatchModel(Bal(), Setup(12, us: 48f, them: 72f)).ComputeWinProb();
            Assert.Greater(strong.Win, strong.Loss + 0.15f, "güçlü taraf belirgin favori olmalı");
            Assert.Greater(weak.Loss, weak.Win + 0.15f);
        }

        [Test]
        public void TempoYukselt_IkiYondeDeOlasilikArtirir()
        {
            var bal = Bal();
            var m = new MatchModel(bal, Setup(13));
            var before = m.PreviewNext();
            Assert.IsTrue(m.TrySetTempo(TempoMode.Yukselt));
            var after = m.PreviewNext();
            Assert.Greater(after.PGoalUs, before.PGoalUs, "tempo bizim golü artırmalı");
            Assert.Greater(after.PGoalThem, before.PGoalThem, "risk iki yönlü olmalı");
        }

        [Test]
        public void HamleHakki_SinirliVeBusUzerindenReddedilir()
        {
            var bal = Bal();
            var st = GreyboxState.NewGame(bal);
            var bus = new GreyboxCommandBus(bal, st);
            var m = new MatchModel(bal, Setup(14));
            bus.ActiveModel = m;

            int uses = 0;
            var modes = new[] { TempoMode.Yukselt, TempoMode.Kilitlen, TempoMode.Normal, TempoMode.Yukselt };
            for (int i = 0; i < bal.model.hamleHakki; i++)
            {
                var r = bus.Send(GreyboxCommandBus.ActModelTempo, GreyboxJson.Payload("mode", (int)modes[i]));
                Assert.AreEqual(RejectionReason.None, r, $"hamle {i} geçmeli");
                uses++;
            }
            Assert.AreEqual(bal.model.hamleHakki, uses);
            Assert.AreEqual(0, m.MovesLeft);
            var last = bus.Send(GreyboxCommandBus.ActModelTempo, GreyboxJson.Payload("mode", (int)modes[3]));
            Assert.AreEqual(RejectionReason.NoChargesLeft, last, "hak bitince NoChargesLeft (CB Spec 11.1)");
        }

        [Test]
        public void AyniSeed_AyniMac_DeterminizmLite()
        {
            var bal = Bal();
            var a = new MatchModel(bal, Setup(777));
            var b = new MatchModel(bal, Setup(777));
            while (!a.IsFinished) { Advance(a); Advance(b); }
            Assert.AreEqual(a.GoalsUs, b.GoalsUs);
            Assert.AreEqual(a.GoalsThem, b.GoalsThem);
        }

        [Test]
        public void ModelPacing_GolBandiVeKalibrasyon()
        {
            var bal = Bal();
            int totalGoals = 0, wins = 0;
            float predictedWin = 0f;
            const int N = 300;
            for (int i = 0; i < N; i++)
            {
                var m = new MatchModel(bal, Setup(9000UL + (ulong)i * 313UL));
                predictedWin += m.ComputeWinProb().Win;
                while (!m.IsFinished) Advance(m);
                totalGoals += m.GoalsUs + m.GoalsThem;
                if (m.GoalsUs > m.GoalsThem) wins++;
            }
            float avgGoals = (float)totalGoals / N;
            float realizedWin = (float)wins / N;
            Assert.That(avgGoals, Is.InRange(2.0f, 3.4f), "blok modeli gol bandı");
            Assert.AreEqual(predictedWin / N, realizedWin, 0.12f,
                "kazanma şeridi KALİBRE olmalı: tahmin ≈ gerçekleşme (Sahneleme §0 'kesin DP')");
        }

        [Test]
        public void Yorgunluk_TempoYukseltDrenajiArtirir()
        {
            var bal = Bal();
            var normal = new MatchModel(bal, Setup(2077));
            var pressed = new MatchModel(bal, Setup(2077));
            Assert.IsTrue(pressed.TrySetTempo(TempoMode.Yukselt));
            for (int b = 0; b < 6; b++) { Advance(normal); Advance(pressed); }
            float eN = normal.SquadUs.TeamEnergyMean();
            float eP = pressed.SquadUs.TeamEnergyMean();
            Assert.Less(eN, bal.squad.enerjiBaslangic, "enerji blok blok düşmeli (İt.11 A1)");
            Assert.Less(eP, eN, "tempo yükseltmenin gerçek bir yorgunluk bedeli olmalı");
        }

        [Test]
        public void TazeBacak_GolOlasiliginiYukseltir()
        {
            var bal = Bal();
            var m = new MatchModel(bal, Setup(909));
            for (int b = 0; b < 6 && !m.IsFinished; b++) Advance(m);
            float pBefore = m.Factors(us: true).Sonuc;
            int outP = -1, inP = -1;
            for (int k = 1; k < 11 && outP < 0; k++)
                if (m.SquadUs.Players[k].OnPitch) outP = k;
            for (int k = 11; k < 16 && inP < 0; k++)
            {
                var p = m.SquadUs.Players[k];
                if (!p.OnPitch && !p.Injured && !p.SentOff) inP = k;
            }
            Assert.IsTrue(m.TrySubstitute(outP, inP), "değişiklik geçmeli");
            Assert.Greater(m.Factors(us: true).Sonuc, pBefore,
                "taze bacak takım enerjisini ve gol olasılığını yükseltmeli (İt.11 A3)");
        }

        [Test]
        public void Degisiklik_BusNegatifSenaryolarVeHakSiniri()
        {
            // CB Spec 10.1 disiplini: yeni katalog aksiyonu 4 negatif senaryoyla gelir
            var bal = Bal();
            var st = GreyboxState.NewGame(bal);
            var bus = new GreyboxCommandBus(bal, st);
            var m = new MatchModel(bal, Setup(4242));
            bus.ActiveModel = m;

            Assert.AreEqual(RejectionReason.ParamOutOfBand,
                bus.Send(GreyboxCommandBus.ActModelSub, GreyboxJson.Payload2("out", 99, "in", 14)), "bant dışı id");
            Assert.AreEqual(RejectionReason.StateConflict,
                bus.Send(GreyboxCommandBus.ActModelSub, GreyboxJson.Payload2("out", 0, "in", 11)), "kaleci değişmez");
            Assert.AreEqual(RejectionReason.StateConflict,
                bus.Send(GreyboxCommandBus.ActModelSub, GreyboxJson.Payload2("out", 9, "in", 9)), "giren sahada olamaz");
            Assert.AreEqual(RejectionReason.StateConflict,
                bus.Send(GreyboxCommandBus.ActModelContinueShort, null), "bekleyen karar yokken eksik devam olmaz");

            Assert.AreEqual(RejectionReason.None, bus.Send(GreyboxCommandBus.ActModelSub, GreyboxJson.Payload2("out", 9, "in", 14)));
            Assert.AreEqual(RejectionReason.None, bus.Send(GreyboxCommandBus.ActModelSub, GreyboxJson.Payload2("out", 10, "in", 15)));
            Assert.AreEqual(RejectionReason.None, bus.Send(GreyboxCommandBus.ActModelSub, GreyboxJson.Payload2("out", 5, "in", 12)));
            Assert.AreEqual(RejectionReason.NoChargesLeft,
                bus.Send(GreyboxCommandBus.ActModelSub, GreyboxJson.Payload2("out", 6, "in", 13)),
                "değişiklik hakkı bitince NoChargesLeft (hamleden ayrı havuz)");
            Assert.AreEqual(0, m.SubsLeft);
        }

        [Test]
        public void SakatlikKarari_AkisiKilitler()
        {
            var bal = Bal();
            bool lockedSeen = false;
            for (ulong s = 1; s < 400 && !lockedSeen; s++)
            {
                var m = new MatchModel(bal, Setup(s * 7919UL));
                while (!m.IsFinished && !m.HasPendingDecision) m.ResolveNext();
                if (!m.HasPendingDecision) continue;
                Assert.Throws<System.InvalidOperationException>(() => m.ResolveNext(),
                    "karar bekleyen model ilerleyemez — çözüm yalnız Tek Kapı komutuyla");
                Assert.IsTrue(m.TryContinueShort());
                lockedSeen = true;
            }
            Assert.IsTrue(lockedSeen, "400 tohumda en az bir sakatlık karar anı üremeli");
        }

        [Test]
        public void OlayDizisi_Deterministik()
        {
            var bal = Bal();
            string Run(ulong seed)
            {
                var m = new MatchModel(bal, Setup(seed, tac: 1));
                var sb = new System.Text.StringBuilder();
                while (!m.IsFinished)
                {
                    m.ResolveNext();
                    foreach (var inc in m.LastBlockIncidents)
                        sb.Append($"{inc.Block}:{inc.Type}:{inc.Team}:{inc.PlayerId};");
                    if (m.HasPendingDecision) m.TryContinueShort();
                }
                return sb.ToString();
            }
            Assert.AreEqual(Run(31337), Run(31337), "aynı seed = aynı kart/sakatlık dizisi");
        }

        [Test]
        public void VinyetUretici_GolKaresiDondurur()
        {
            var bal = Bal();
            var frames = VignetteRecorder.RecordGoal(bal, 4242, 0, 0, 1, 60f, 55f);
            Assert.IsNotNull(frames, "bizim gol vinyeti üretilmeli");
            Assert.Greater(frames.Count, 20, "en az ~1 sn kayıt");
            var framesThem = VignetteRecorder.RecordGoal(bal, 4243, 1, 0, 1, 60f, 55f);
            Assert.IsNotNull(framesThem, "rakip gol vinyeti üretilmeli");
        }
    }
}
