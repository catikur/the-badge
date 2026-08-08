using System.IO;
using TheBadge.Greybox.View;
using TheBadge.Sim.Config;
using TheBadge.Sim.Determinism;
using TheBadge.Sim.Match;
using UnityEngine;

namespace TheBadge.Greybox.EngineDev
{
    /// <summary>
    /// FAZ 03 MOTOR TEST EKRANI — geliştirici sahnesi (EngineDev.unity): gerçek MatchEngine'i
    /// Unity içinde koşar ve durumu ham haliyle gösterir. SANAT/UX DEĞİLDİR; amaç her motor
    /// diliminin gözle doğrulanabilmesi (Atilla kararı, 2026-08-08). Kadrolar prosedürel test
    /// verisidir (Checks ile aynı desen); gerçek kadro/veri katmanı FAZ 04.
    /// Balance, Editor'de repo kökünden okunur (dev ekranı build'e girmez).
    /// </summary>
    public sealed class EngineDevBootstrap : MonoBehaviour
    {
        MatchEngine engine;
        MatchState state;
        CommandQueue queue;
        SimBalance bal;
        Transform[] dots;
        Transform ballDot;
        float acc;
        int speed = 1;
        string loadError;

        void Awake()
        {
            Application.targetFrameRate = 60;

            var camGo = new GameObject("DevCam");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 38f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.16f, 0.10f);
            cam.nearClipPlane = -50f;
            cam.farClipPlane = 50f;

            // Balance: Editor'de repo kökü — Assets → unity/TheBadge → unity → kök (3 yukarı)
            string path = Path.Combine(Application.dataPath, "..", "..", "..", "balance", "sim.balance.json");
            if (!File.Exists(path)) { loadError = "balance/sim.balance.json bulunamadı: " + path; return; }
            bal = JsonUtility.FromJson<SimBalance>(File.ReadAllText(path));

            var cfg = new MatchConfig
            {
                Seed = 20260808UL,
                EngineVersion = "dev",
                Home = BuildSheet(1, home: true),
                Away = BuildSheet(2, home: false)
            };
            queue = new CommandQueue();
            engine = new MatchEngine(cfg.Seed, queue, cfg, bal);
            state = MatchEngine.CreateInitialState(cfg);

            BuildPitchLines();
            dots = new Transform[22];
            for (int i = 0; i < 22; i++)
            {
                var sr = SpriteFactory.NewSprite("P" + i, transform, SpriteFactory.Circle(),
                    i < 11 ? new Color(0.92f, 0.92f, 0.95f) : new Color(0.25f, 0.3f, 0.38f), 10);
                sr.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
                dots[i] = sr.transform;
            }
            var ball = SpriteFactory.NewSprite("Ball", transform, SpriteFactory.Circle(), new Color(1f, 0.85f, 0.3f), 20);
            ball.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            ballDot = ball.transform;
        }

        void BuildPitchLines()
        {
            // Saha çerçevesi: 105×68 m — ince çizgiler (dev okunabilirliği)
            void Line(string n, float x, float y, float w, float h)
            {
                var sr = SpriteFactory.NewSprite(n, transform, SpriteFactory.Solid(), new Color(0.8f, 0.9f, 0.8f, 0.35f), 1);
                sr.transform.localPosition = new Vector3(x, y, 0f);
                sr.transform.localScale = new Vector3(w, h, 1f);
            }
            Line("Ust", 0f, 34f, 105f, 0.15f);
            Line("Alt", 0f, -34f, 105f, 0.15f);
            Line("Sol", -52.5f, 0f, 0.15f, 68f);
            Line("Sag", 52.5f, 0f, 0.15f, 68f);
            Line("Orta", 0f, 0f, 0.15f, 68f);
        }

        static TeamSheet BuildSheet(uint entity, bool home)
        {
            // Checks'teki test kadrosu deseninin aynısı (BuildSheetSide) — dev ekranı kopyası
            var sheet = new TeamSheet { Starters = new PlayerEntry[11], Bench = new PlayerEntry[5] };
            int sign = home ? -1 : 1;
            for (int i = 0; i < 16; i++)
            {
                byte V(uint salt) => (byte)(35 + (int)(Rng.Rand01(998877UL, Domain.Decision, entity, (uint)i, salt) * 50));
                int ax, ay;
                if (i == 0) { ax = 48000; ay = 0; }
                else if (i < 5) { ax = 33000; ay = (i - 1) * 16000 - 24000; }
                else if (i < 9) { ax = 12000; ay = (i - 5) * 16000 - 24000; }
                else { ax = 3000; ay = i == 9 ? -8000 : 8000; }
                var e = new PlayerEntry
                {
                    PlayerId = (short)(entity * 100 + i),
                    Name = $"Dev-{entity}-{i}",
                    RoleId = (byte)(i == 0 ? 1 : i < 5 ? 2 : i < 9 ? 3 : 4),
                    AnchorXmm = sign * ax,
                    AnchorYmm = ay,
                    Attributes = new PlayerAttributes
                    {
                        Passing = V(1), Finishing = V(2), Dribbling = V(7), Tackling = V(8),
                        FirstTouch = V(9), Positioning = V(10), Vision = V(11), Composure = V(12),
                        Pace = V(3), Acceleration = V(13), Stamina = V(4), Strength = V(14), Agility = V(15),
                        Reflexes = V(5), Handling = V(6)
                    }
                };
                if (i < 11) sheet.Starters[i] = e; else sheet.Bench[i - 11] = e;
            }
            return sheet;
        }

        void Update()
        {
            if (engine == null) return;
            acc += Time.deltaTime * speed;
            float step = MatchEngine.TickMs / 1000f;
            int guard = 0;
            while (acc >= step && guard++ < 200)
            {
                acc -= step;
                engine.Tick(ref state);
            }
            for (int i = 0; i < 22; i++)
                dots[i].localPosition = new Vector3(state.Agents[i].X / 1000f, state.Agents[i].Y / 1000f, 0f);
            ballDot.localPosition = new Vector3(state.Ball.X / 1000f, state.Ball.Y / 1000f, -0.1f);
            float zScale = 0.9f + state.Ball.Z / 1000f * 0.12f; // yükseklik ipucu
            ballDot.localScale = new Vector3(zScale, zScale, 1f);
        }

        void OnGUI()
        {
            if (loadError != null) { GUILayout.Label(loadError); return; }
            if (engine == null) return;
            int sec = (int)(state.Tick / (uint)MatchEngine.TicksPerSecond);
            GUILayout.Label($"MOTOR TEST — M2 | tick {state.Tick}  ({sec / 60:00}:{sec % 60:00})  faz {state.Phase}  hız {speed}x");
            GUILayout.Label($"pas {engine.PassAttempts} (tamam {engine.PassCompletions}) · tackle {engine.Tackles} · taç/aut {engine.OutOfBounds} · sahiplik değişimi {engine.PossessionChanges}");
            GUILayout.Label($"top sahibi: {(state.Ball.OwnerId < 0 ? "serbest" : (state.Ball.OwnerId < 11 ? "EV #" : "DEP #") + state.Ball.OwnerId)}  ·  checksum 0x{state.LastChecksum:X}");
            GUILayout.Label("(gol/şut/kaleci modeli M3'te — bu ekran ham motor durumudur)");
            if (GUILayout.Button($"Hız: {speed}x → değiştir")) speed = speed == 1 ? 5 : speed == 5 ? 25 : 1;
        }
    }
}
