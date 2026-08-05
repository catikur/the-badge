using System;
using TheBadge.Greybox.Sim;
using UnityEngine;

namespace TheBadge.Greybox.Loop
{
    /// <summary>
    /// Maç sürücüsü — FlowSim'i gerçek zamanla besler; hız (1x/2x), önemli ana atlama
    /// ve gol vurgusu (slow-mo) burada yaşar (Brif K2). Hız/skip SUNUM durumudur,
    /// oyun durumu değildir → Tek Kapı komutu gerektirmez (CB Spec kapsam notu).
    /// Time.timeScale'e dokunulmaz; yavaşlatma yalnız sim dt'sine uygulanır ki UI akıcı kalsın.
    /// </summary>
    public sealed class MatchDirector : MonoBehaviour
    {
        public event Action<FlowEvent, bool> EventRaised; // bool: skip sırasında mı geldi
        public event Action MatchFinished;
        public event Action<int> SpeedChanged;
        public event Action<float, float> Skipped;        // atlanan aralık (dk → dk)

        /// <summary>Sabit sim adımı — sunum her hızda bu adımla ilerler, kareler arası interpolasyon
        /// pürüzsüzlüğü sağlar (Sahneleme §2: hız değişimi top-oyuncu ilişkisini bozamaz).</summary>
        public const float SimStep = 0.05f;

        GreyboxBalance bal;
        FlowSim sim;
        float slowmoTimer;
        float simAccum;
        readonly Vec2[] prevPos = new Vec2[23]; // 0..21 oyuncular, 22 top
        bool running;

        public int Speed { get; private set; } = 1;
        public FlowSim Sim => sim;
        public float WatchRealSeconds { get; private set; }
        public int SkipCount { get; private set; }
        public int SpeedChangeCount { get; private set; }
        public bool SlowmoActive => slowmoTimer > 0f;

        /// <summary>Son sim adımından bu yana biriken oranda (0..1) önceki→şimdiki karışım.</summary>
        public float InterpAlpha => Mathf.Clamp01(simAccum / SimStep);
        public Vec2 PrevPos(int i) => prevPos[i];

        public void Init(GreyboxBalance balance) => bal = balance;

        void Snapshot()
        {
            for (int i = 0; i < 22; i++) prevPos[i] = sim.GetPlayer(i).Pos;
            prevPos[22] = sim.BallPos;
        }

        void ResetInterp()
        {
            if (sim == null) return;
            Snapshot();
            simAccum = 0f;
        }

        public void StartMatch(MatchSetup setup)
        {
            sim = new FlowSim(bal, setup);
            Speed = 1;
            slowmoTimer = 0f;
            WatchRealSeconds = 0f;
            SkipCount = 0;
            SpeedChangeCount = 0;
            running = true;
            ResetInterp();
        }

        public void StopMatch()
        {
            running = false;
            sim = null;
        }

        public void SetSpeed(int s)
        {
            if (!running || s == Speed) return;
            Speed = s;
            SpeedChangeCount++;
            SpeedChanged?.Invoke(s);
        }

        /// <summary>Sonraki önemli ana (şut/korner penceresi) ya da devre/maç sonuna atlar.</summary>
        public void SkipToKeyMoment()
        {
            if (!running || sim == null || sim.IsFinished || sim.InKeyMoment) return;
            SkipCount++;
            float fromMin = sim.MatchMinute;
            float budget = bal.clock.macSuresiSaniye * 4f; // emniyet tavanı: donmuş akışta sonsuz döngü koruması
            float spent = 0f;
            while (!sim.IsFinished && !sim.InKeyMoment && spent < budget)
            {
                sim.Step(bal.clock.skipDilimSaniye);
                spent += bal.clock.skipDilimSaniye;
                DrainEvents(duringSkip: true);
            }
            Skipped?.Invoke(fromMin, sim.MatchMinute);
            ResetInterp(); // atlama sonrası dev kare-arası karışım olmasın
            if (sim.IsFinished) FinishMatch();
        }

        void Update()
        {
            if (!running || sim == null) return;

            WatchRealSeconds += Time.deltaTime;

            float factor = Speed;
            if (slowmoTimer > 0f)
            {
                slowmoTimer -= Time.deltaTime;
                factor = bal.vurgu.slowmoCarpan; // gol vurgusu: dünya yavaşlar, UI normal akar
            }

            // Sabit adımlı sim: hız yalnız adım SIKLIĞINI değiştirir, adımın kendisini değil
            simAccum += Time.deltaTime * factor;
            int guard = 0;
            while (simAccum >= SimStep && guard++ < 400 && !sim.IsFinished)
            {
                Snapshot();
                sim.Step(SimStep);
                simAccum -= SimStep;
                DrainEvents(duringSkip: false);
            }

            if (sim.IsFinished) FinishMatch();
        }

        void DrainEvents(bool duringSkip)
        {
            while (sim != null && sim.TryDequeueEvent(out var e))
            {
                if (!duringSkip && e.Type == FlowEventType.Goal)
                    slowmoTimer = bal.vurgu.slowmoSureSn;
                EventRaised?.Invoke(e, duringSkip);
            }
        }

        void FinishMatch()
        {
            if (!running) return;
            running = false;
            MatchFinished?.Invoke();
        }
    }
}
