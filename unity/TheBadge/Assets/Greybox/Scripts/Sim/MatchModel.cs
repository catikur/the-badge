using System;
using TheBadge.Sim.Determinism;

namespace TheBadge.Greybox.Sim
{
    /// <summary>Blok sonucu — Model Maçı (Sahneleme §0).</summary>
    public enum BlockOutcome { Quiet = 0, Danger = 1, GoalUs = 2, GoalThem = 3 }

    /// <summary>Müdahale türleri — Tek Kapı komutlarıyla tetiklenir.</summary>
    public enum TempoMode { Normal = 0, Yukselt = 1, Kilitlen = 2 }

    public struct BlockPreview
    {
        public int Index;          // 0 tabanlı blok no
        public float PGoalUs;      // bu blokta bizim gol olasılığımız (EKRANDA gösterilir)
        public float PGoalThem;
        public float Momentum;     // -1..1 (+ biz)
    }

    public struct WinProb
    {
        public float Win, Draw, Loss;
    }

    /// <summary>
    /// MODEL MAÇI motoru — Fun Gate pivotu (Sahneleme §0, DECISIONS 2026-08-02).
    /// Maç N aksiyon bloğudur; her blokta olasılıklar AÇIK hesaplanır (önce gösterilir,
    /// sonra zar döner). Kazanma şeridi kalan bloklar üzerinden KESİN dağılımla (DP) bulunur.
    /// Saf C#: headless test edilebilir; UnityEngine yok. Rastgelelik TheBadge.Sim.Rng.
    /// </summary>
    public sealed class MatchModel
    {
        readonly GreyboxBalance bal;
        readonly GreyboxBalance.ModelCfg m;
        readonly ulong seed;
        readonly float usStrength, themStrength;
        GreyboxBalance.TacticCfg usTactic;
        readonly GreyboxBalance.TacticCfg themTactic;

        public int BlockCount => m.blokSayisi;
        public int CurrentBlock { get; private set; }   // sıradaki (henüz oynanmamış) blok
        public int GoalsUs { get; private set; }
        public int GoalsThem { get; private set; }
        public float Momentum { get; private set; }
        public TempoMode Tempo { get; private set; } = TempoMode.Normal;
        public int MovesLeft { get; private set; }
        public bool IsFinished => CurrentBlock >= m.blokSayisi;
        public int TacticId => usTactic.id;

        /// <summary>Blok başlangıç dakikası (gösterim: 10 blok → 9'ar dk).</summary>
        public int BlockMinute(int block) => (int)Math.Round(90.0 * block / m.blokSayisi);

        public MatchModel(GreyboxBalance balance, MatchSetup setup)
        {
            bal = balance;
            m = balance.model;
            seed = setup.Seed;
            usStrength = setup.HomeStrength;
            themStrength = setup.AwayStrength;
            usTactic = FindTactic(setup.HomeTacticId);
            themTactic = FindTactic(setup.AwayTacticId);
            MovesLeft = m.hamleHakki;
        }

        GreyboxBalance.TacticCfg FindTactic(int id)
        {
            for (int i = 0; i < bal.taktikler.Length; i++)
                if (bal.taktikler[i].id == id) return bal.taktikler[i];
            return bal.taktikler[0];
        }

        // ---------------------------------------------------------------- olasılık modeli

        /// <summary>Bir tarafın blok gol olasılığı — tüm etkenler açık (Sahneleme §0).</summary>
        float PGoal(bool us)
        {
            var atk = us ? usTactic : themTactic;
            var def = us ? themTactic : usTactic;
            float strDiff = us ? usStrength - themStrength : themStrength - usStrength;
            float mom = us ? Momentum : -Momentum;

            float p = m.pGolTabani;
            p += strDiff * m.gucEtkiCarpan * m.pGolTabani;                      // kadro gücü
            p *= 1f + (atk.tempo - 1f) * m.taktikTempoEtki;                     // hücum temposu
            p *= 1f + (atk.sutIstahi - 1f) * m.taktikSutEtki;                   // şut iştahı
            p *= 1f - (def.pres - 1f) * m.taktikPresSavunmaEtki;                // rakip pres/savunma
            p *= 1f + mom * m.momentumEtki;                                     // momentum
            p *= Tempo == TempoMode.Yukselt ? (us ? m.tempoYukseltBiz : m.tempoYukseltRakip)
               : Tempo == TempoMode.Kilitlen ? (us ? m.kilitlenBiz : m.kilitlenRakip)
               : 1f;                                                            // müdahale modu
            return Clamp(p, m.pGolMin, m.pGolMax);
        }

        /// <summary>Sıradaki bloğun kartı — zar dönmeden ÖNCE ekranda gösterilir.</summary>
        public BlockPreview PreviewNext() => new BlockPreview
        {
            Index = CurrentBlock,
            PGoalUs = PGoal(us: true),
            PGoalThem = PGoal(us: false),
            Momentum = Momentum
        };

        /// <summary>Bloğu oynatır (zar döner) ve sonucu döndürür. Domain.Duel: blok düellosu.</summary>
        public BlockOutcome ResolveNext()
        {
            var pv = PreviewNext();
            uint tick = (uint)CurrentBlock;
            double r = Rng.Rand01(seed, Domain.Duel, 500, tick, 1);
            BlockOutcome outcome;
            if (r < pv.PGoalUs) { outcome = BlockOutcome.GoalUs; GoalsUs++; }
            else if (r < pv.PGoalUs + pv.PGoalThem) { outcome = BlockOutcome.GoalThem; GoalsThem++; }
            else
            {
                double rd = Rng.Rand01(seed, Domain.Duel, 501, tick, 2);
                outcome = rd < (pv.PGoalUs + pv.PGoalThem) * m.tehlikeCarpan * 0.5
                    ? BlockOutcome.Danger : BlockOutcome.Quiet;
            }

            // Momentum güncellemesi — Domain.Chaos: blok salınımı
            float g = (float)Rng.Gauss01(seed, Domain.Chaos, 502, tick, 3);
            Momentum += g * m.momentumBlokGurultu - Momentum * m.momentumSonum;
            if (outcome == BlockOutcome.GoalUs) Momentum += m.momentumGolDelta;
            if (outcome == BlockOutcome.GoalThem) Momentum -= m.momentumGolDelta;
            Momentum = Clamp(Momentum, -1f, 1f);

            CurrentBlock++;
            return outcome;
        }

        // ---------------------------------------------------------------- müdahale (Tek Kapı'dan çağrılır)

        public bool TrySetTactic(int tacticId)
        {
            if (MovesLeft <= 0 || IsFinished) return false;
            var t = FindTactic(tacticId);
            if (t.id == usTactic.id) return false;
            usTactic = t;
            MovesLeft--;
            return true;
        }

        public bool TrySetTempo(TempoMode mode)
        {
            if (MovesLeft <= 0 || IsFinished || mode == Tempo) return false;
            Tempo = mode;
            MovesLeft--;
            return true;
        }

        // ---------------------------------------------------------------- kazanma şeridi (kesin DP)

        /// <summary>
        /// Kalan bloklar üzerinden KESİN G/B/M dağılımı. Blok sonuçları bağımsız üçlü
        /// (bizde gol / rakipte gol / yok) varsayılır; skor farkı dağılımı DP ile taşınır.
        /// Momentum/tempo İLERİYE dönük mevcut değerleriyle sabitlenir (dürüst yaklaşıklık:
        /// ekrandaki sayı, "şu anki gidişat sürerse" olasılığıdır).
        /// </summary>
        public WinProb ComputeWinProb()
        {
            int remaining = m.blokSayisi - CurrentBlock;
            float pU = PGoal(us: true), pT = PGoal(us: false);
            int offset = remaining;                    // fark indeksi kaydırması
            int size = remaining * 2 + 1;
            var dist = new double[size];
            dist[offset] = 1.0;                        // fark 0'dan başla (kalan bloklar için)

            for (int b = 0; b < remaining; b++)
            {
                var next = new double[size];
                for (int dIdx = 0; dIdx < size; dIdx++)
                {
                    double pr = dist[dIdx];
                    if (pr <= 0) continue;
                    next[Math.Min(size - 1, dIdx + 1)] += pr * pU;             // bizde gol
                    next[Math.Max(0, dIdx - 1)] += pr * pT;                    // rakipte gol
                    next[dIdx] += pr * (1.0 - pU - pT);                        // sessiz
                }
                dist = next;
            }

            int baseDiff = GoalsUs - GoalsThem;
            double w = 0, dr = 0, l = 0;
            for (int dIdx = 0; dIdx < size; dIdx++)
            {
                int final = baseDiff + (dIdx - offset);
                if (final > 0) w += dist[dIdx];
                else if (final == 0) dr += dist[dIdx];
                else l += dist[dIdx];
            }
            return new WinProb { Win = (float)w, Draw = (float)dr, Loss = (float)l };
        }

        static float Clamp(float v, float lo, float hi) => v < lo ? lo : (v > hi ? hi : v);
    }
}
