using TheBadge.Greybox.Sim;
using UnityEngine;

namespace TheBadge.Greybox.View
{
    /// <summary>
    /// Greybox saha görünümü — Brif K2: SADECE placeholder (daireler + dikdörtgen saha).
    /// Sim koordinatı (0..68, 0..105) → dünya koordinatı (merkez orijin) çevirisi burada.
    /// Sunum katmanı sim durumunu yalnız OKUR (UNITY_SETUP.md kuralı).
    /// </summary>
    public sealed class PitchView : MonoBehaviour
    {
        const int OrderGrass = 0, OrderLines = 1, OrderDots = 5, OrderBall = 8;
        const float LineW = 0.35f;

        GreyboxBalance bal;
        readonly Transform[] dots = new Transform[22];
        Transform ball;

        public static PitchView Create(GreyboxBalance bal)
        {
            var go = new GameObject("PitchView");
            var view = go.AddComponent<PitchView>();
            view.bal = bal;
            view.Build();
            return view;
        }

        static Vector3 W(float x, float y) => new Vector3(x - FlowSim.PitchW * 0.5f, y - FlowSim.PitchL * 0.5f, 0f);

        void Build()
        {
            Color grass = SpriteFactory.Hex(bal.renkler.saha, new Color(0.18f, 0.49f, 0.27f));
            Color line = SpriteFactory.Hex(bal.renkler.cizgi, new Color(0.86f, 0.93f, 0.78f));
            line.a = 0.9f;

            var g = SpriteFactory.NewSprite("Grass", transform, SpriteFactory.Solid(), grass, OrderGrass);
            g.transform.localScale = new Vector3(FlowSim.PitchW + 6f, FlowSim.PitchL + 6f, 1f);

            // Çim şeritleri: hafif ton farkıyla canlılık (placeholder sınırları içinde)
            Color stripe = grass * 1.06f; stripe.a = 1f;
            for (int i = 0; i < 6; i++)
            {
                var s = SpriteFactory.NewSprite("Stripe" + i, transform, SpriteFactory.Solid(), stripe, OrderGrass);
                s.transform.localScale = new Vector3(FlowSim.PitchW, FlowSim.PitchL / 12f, 1f);
                s.transform.localPosition = W(FlowSim.PitchW * 0.5f, FlowSim.PitchL * (i * 2 + 0.5f) / 12f);
            }

            // Saha çizgileri
            Bar("TouchL", 0f, FlowSim.PitchL * 0.5f, LineW, FlowSim.PitchL + LineW, line);
            Bar("TouchR", FlowSim.PitchW, FlowSim.PitchL * 0.5f, LineW, FlowSim.PitchL + LineW, line);
            Bar("GoalLineB", FlowSim.PitchW * 0.5f, 0f, FlowSim.PitchW + LineW, LineW, line);
            Bar("GoalLineT", FlowSim.PitchW * 0.5f, FlowSim.PitchL, FlowSim.PitchW + LineW, LineW, line);
            Bar("Halfway", FlowSim.PitchW * 0.5f, FlowSim.PitchL * 0.5f, FlowSim.PitchW, LineW, line);

            var ring = SpriteFactory.NewSprite("CenterCircle", transform, SpriteFactory.Ring(), line, OrderLines);
            ring.transform.localScale = new Vector3(18.3f, 18.3f, 1f);
            ring.transform.localPosition = W(FlowSim.PitchW * 0.5f, FlowSim.PitchL * 0.5f);

            // Ceza sahaları (40.3 x 16.5) + kale ağızları
            PenaltyBox(0f, line);
            PenaltyBox(FlowSim.PitchL, line);
            GoalMouth(0f, line);
            GoalMouth(FlowSim.PitchL, line);

            // Oyuncular + top
            Color home = SpriteFactory.Hex(bal.renkler.evTakim, Color.white);
            Color homeGk = SpriteFactory.Hex(bal.renkler.evKaleci, Color.yellow);
            Color away = SpriteFactory.Hex(bal.renkler.depTakim, new Color(0.2f, 0.28f, 0.31f));
            Color awayGk = SpriteFactory.Hex(bal.renkler.depKaleci, Color.cyan);
            float d = bal.players.oyuncuYaricapM * 2f;
            for (int i = 0; i < 22; i++)
            {
                bool isHome = i < 11;
                bool isGk = i % 11 == 0;
                var c = isHome ? (isGk ? homeGk : home) : (isGk ? awayGk : away);
                var sr = SpriteFactory.NewSprite((isHome ? "H" : "A") + (i % 11), transform, SpriteFactory.Circle(), c, OrderDots);
                sr.transform.localScale = new Vector3(d, d, 1f);
                dots[i] = sr.transform;
            }

            var b = SpriteFactory.NewSprite("Ball", transform, SpriteFactory.Circle(),
                SpriteFactory.Hex(bal.renkler.top, Color.white), OrderBall);
            float bd = bal.players.topYaricapM * 2f;
            b.transform.localScale = new Vector3(bd, bd, 1f);
            ball = b.transform;

            // Topa okunabilirlik için koyu dış halka (placeholder, asset değil)
            var outline = SpriteFactory.NewSprite("BallOutline", ball, SpriteFactory.Ring(), new Color(0f, 0f, 0f, 0.55f), OrderBall - 1);
            outline.transform.localScale = new Vector3(1.35f, 1.35f, 1f);
        }

        void Bar(string name, float cx, float cy, float w, float h, Color c)
        {
            var sr = SpriteFactory.NewSprite(name, transform, SpriteFactory.Solid(), c, OrderLines);
            sr.transform.localScale = new Vector3(w, h, 1f);
            sr.transform.localPosition = W(cx, cy);
        }

        void PenaltyBox(float goalY, Color line)
        {
            float dir = goalY <= 0.01f ? 1f : -1f;
            float depth = 16.5f, halfW = 20.15f, cx = FlowSim.PitchW * 0.5f;
            Bar("BoxFront", cx, goalY + dir * depth, halfW * 2f, LineW, line);
            Bar("BoxSideL", cx - halfW, goalY + dir * depth * 0.5f, LineW, depth, line);
            Bar("BoxSideR", cx + halfW, goalY + dir * depth * 0.5f, LineW, depth, line);
            var spot = SpriteFactory.NewSprite("PenSpot", transform, SpriteFactory.Circle(), line, OrderLines);
            spot.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            spot.transform.localPosition = W(cx, goalY + dir * 11f);
        }

        void GoalMouth(float goalY, Color line)
        {
            float dir = goalY <= 0.01f ? -1f : 1f;
            // Ağ: çizginin arkasında belirgin derinlikte yarı saydam kutu — gol topu İÇİNDE biter (İterasyon 2)
            Color net = line; net.a = 0.32f;
            Bar("GoalNet", FlowSim.PitchW * 0.5f, goalY + dir * 1.35f, 7.32f + 1.2f, 2.5f, net);
            // Kale ağzı: çizgi üstünde parlak bar (direkler arası okunsun)
            Color mouth = line; mouth.a = 1f;
            Bar("GoalMouth", FlowSim.PitchW * 0.5f, goalY, 7.32f + 0.9f, 0.55f, mouth);
        }

        /// <summary>Her frame sim pozisyonlarını uygular (sim zaten yumuşak hareket üretir).</summary>
        public void Render(FlowSim sim)
        {
            if (sim == null) return;
            for (int i = 0; i < 22; i++)
            {
                var p = sim.GetPlayer(i);
                dots[i].localPosition = W(p.Pos.X, p.Pos.Y);
            }
            ball.localPosition = W(sim.BallPos.X, sim.BallPos.Y);
        }
    }
}
