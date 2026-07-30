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

        GreyboxBalance bal;
        FlowSim sim;
        float slowmoTimer;
        bool running;

        public int Speed { get; private set; } = 1;
        public FlowSim Sim => sim;
        public float WatchRealSeconds { get; private set; }
        public int SkipCount { get; private set; }
        public int SpeedChangeCount { get; private set; }
        public bool SlowmoActive => slowmoTimer > 0f;

        public void Init(GreyboxBalance balance) => bal = balance;

        public void StartMatch(MatchSetup setup)
        {
            sim = new FlowSim(bal, setup);
            Speed = 1;
            slowmoTimer = 0f;
            WatchRealSeconds = 0f;
            SkipCount = 0;
            SpeedChangeCount = 0;
            running = true;
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

            sim.Step(Time.deltaTime * factor);
            DrainEvents(duringSkip: false);

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
