using System;
using System.Collections;
using System.Collections.Generic;
using TheBadge.Greybox.Sim;
using UnityEngine;

namespace TheBadge.Greybox.Loop
{
    /// <summary>
    /// MODEL MAÇI yönetmeni — Sahneleme §0 (Fun Gate pivotu).
    /// Akış her blokta: KART (olasılıklar ekranda) → gerilim beklemesi → ZAR → sonuç sunumu
    /// (+ gol bloklarında 2D vinyet) → müdahale penceresi → sonraki blok.
    /// Hız (1x/2x) yalnız sunum temposunu değiştirir; skip bekleme ve vinyeti atlar.
    /// </summary>
    public sealed class ModelMatchDirector : MonoBehaviour
    {
        public event Action<BlockPreview, WinProb> BlockPreviewShown;
        public event Action<int, BlockOutcome, WinProb> BlockResolved; // blockIndex, sonuç, yeni şerit
        public event Action<VignetteFrame> VignetteFramePlayed;
        public event Action<bool> VignetteToggled;                     // başladı/bitti
        public event Action DecisionRequired;   // sakatlıkta zorunlu karar — akış DURUR (İt.11 A2)
        public event Action DecisionResolved;   // karar Tek Kapı'dan çözüldü — akış sürer
        public event Action MatchFinished;

        GreyboxBalance bal;
        MatchModel model;
        MatchSetup setup;
        Coroutine flow;
        bool skipRequested;

        public int Speed { get; private set; } = 1;
        public MatchModel Model => model;
        public float WatchRealSeconds { get; private set; }
        public int SkipCount { get; private set; }
        public int SpeedChangeCount { get; private set; }
        bool running;

        public void Init(GreyboxBalance balance) => bal = balance;

        public void StartMatch(MatchSetup matchSetup)
        {
            setup = matchSetup;
            model = new MatchModel(bal, matchSetup);
            Speed = 1;
            WatchRealSeconds = 0f;
            SkipCount = 0;
            SpeedChangeCount = 0;
            skipRequested = false;
            running = true;
            flow = StartCoroutine(RunMatch());
        }

        public void StopMatch()
        {
            running = false;
            if (flow != null) StopCoroutine(flow);
            flow = null;
            model = null;
        }

        public void SetSpeed(int s)
        {
            if (!running || s == Speed) return;
            Speed = s;
            SpeedChangeCount++;
        }

        /// <summary>▶▶: mevcut bloğun beklemesini/vinyetini atlar (bloklar yine sırayla oynar).</summary>
        public void SkipCurrent()
        {
            if (!running) return;
            skipRequested = true;
            SkipCount++;
        }

        void Update()
        {
            if (running) WatchRealSeconds += Time.deltaTime;
        }

        IEnumerator RunMatch()
        {
            while (!model.IsFinished)
            {
                var preview = model.PreviewNext();
                var stripBefore = model.ComputeWinProb();
                BlockPreviewShown?.Invoke(preview, stripBefore);

                // Kart ekranda: gerilim + okuma süresi (müdahale bu pencerede serbest)
                yield return WaitScaled(bal.model.gerilimBeklemeSn + bal.model.blokOynatmaSn * 0.5f);

                var outcome = model.ResolveNext();
                var stripAfter = model.ComputeWinProb();
                BlockResolved?.Invoke(preview.Index, outcome, stripAfter);

                if ((outcome == BlockOutcome.GoalUs || outcome == BlockOutcome.GoalThem) && !skipRequested)
                    yield return PlayVignette(outcome == BlockOutcome.GoalUs ? 0 : 1, (uint)preview.Index);

                // Sakatlıkta ZORUNLU karar: akış oyuncu Tek Kapı'dan karar verene dek bekler.
                // Skip bu beklemeyi ATLAYAMAZ — karar anı deneyimin çekirdeğidir (İt.11 A2).
                if (model.HasPendingDecision)
                {
                    DecisionRequired?.Invoke();
                    while (running && model.HasPendingDecision) yield return null;
                    DecisionResolved?.Invoke();
                }

                yield return WaitScaled(bal.model.blokOynatmaSn * 0.5f);
                skipRequested = false;
            }
            running = false;
            MatchFinished?.Invoke();
        }

        IEnumerator WaitScaled(float seconds)
        {
            float t = 0f;
            while (t < seconds && !skipRequested)
            {
                t += Time.deltaTime * Speed;
                yield return null;
            }
        }

        IEnumerator PlayVignette(int scorerTeam, uint blockIndex)
        {
            // Kayıt: headless FlowSim golü arar (sahiplik/sahne kuralları aynen) — Sahneleme §0
            List<VignetteFrame> frames = VignetteRecorder.RecordGoal(
                bal, setup.Seed + blockIndex * 977UL, scorerTeam,
                model.TacticId, setup.AwayTacticId, setup.HomeStrength, setup.AwayStrength);
            if (frames == null || frames.Count == 0) yield break;

            VignetteToggled?.Invoke(true);
            const float frameDt = 0.05f;
            float acc = 0f;
            int fi = 0;
            while (fi < frames.Count && !skipRequested)
            {
                acc += Time.deltaTime * Speed;
                while (acc >= frameDt && fi < frames.Count)
                {
                    acc -= frameDt;
                    VignetteFramePlayed?.Invoke(frames[fi]);
                    fi++;
                }
                yield return null;
            }
            // Son karede nefes: sahne aniden kapanmaz (Atilla — "hiçbir şey için acele etme")
            yield return WaitScaled(0.9f);
            VignetteToggled?.Invoke(false);
        }
    }
}
