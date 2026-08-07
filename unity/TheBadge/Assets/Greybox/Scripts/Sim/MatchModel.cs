using System;
using System.Collections.Generic;
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

    /// <summary>Blok olasılığının ETKEN DÖKÜMÜ — "neye göre?" sorusunun şeffaf cevabı
    /// (docs/GREYBOX_MODEL.md). Her alan çarpan; Sonuc = Taban × hepsi (clamp'li).</summary>
    public struct FactorSnapshot
    {
        public float Taban;
        public float Guc;        // kadro gücü farkı (tanh doygun)
        public float Taktik;     // etkileşim matrisi × tempo/şut iştahı
        public float Faz;        // maç fazı (son bloklarda gol artar)
        public float Momentum;
        public float Skor;       // geride risk / önde kontrol
        public float TempoModu;  // müdahale çarpanı
        public float Ev;         // ev sahibi avantajı
        public float Form;       // son 5 maç
        public float Yorgunluk;  // takım enerjisi etkisi (ME Spec 12.1 vekili) — İt.11
        public float Eksik;      // kırmızı/sakatlık eksik etkisi (kendi hücum + rakip savunma)
        public float Sonuc;      // clamp'lenmiş nihai olasılık
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
        readonly float usFormNet;
        GreyboxBalance.TacticCfg usTactic;
        readonly GreyboxBalance.TacticCfg themTactic;
        readonly GreyboxBalance.SquadCfg sq;
        readonly GreyboxBalance.EventCfg ev;

        public int BlockCount => m.blokSayisi;
        public int CurrentBlock { get; private set; }   // sıradaki (henüz oynanmamış) blok
        public int GoalsUs { get; private set; }
        public int GoalsThem { get; private set; }
        public float Momentum { get; private set; }
        public TempoMode Tempo { get; private set; } = TempoMode.Normal;
        public int MovesLeft { get; private set; }
        public bool IsFinished => CurrentBlock >= m.blokSayisi;
        public int TacticId => usTactic.id;

        // Maç istatistikleri (iterasyon 10): model-xG = blok olasılıklarının toplamı
        public float XgUs { get; private set; }
        public float XgThem { get; private set; }
        public int DangerUs { get; private set; }
        public int DangerThem { get; private set; }
        public int LastDangerSide { get; private set; } = -1; // 0 biz / 1 rakip (son Danger bloğu)

        // Kadro katmanı (iterasyon 11 — Öneri İt.11 A1-A3): isimli 11+5, enerji/kart/sakatlık
        public Squad SquadUs { get; private set; }
        public Squad SquadThem { get; private set; }
        /// <summary>Kalan oyuncu değişikliği hakkı — GDD 12.4 standardı, hamle hakkından AYRI.</summary>
        public int SubsLeft { get; private set; }
        int themSubsLeft;
        /// <summary>Bizim sakatlıkta zorunlu karar bekleniyor: değiştir ya da eksik devam.
        /// True iken ResolveNext çağrılamaz — karar Tek Kapı komutuyla çözülür.</summary>
        public bool HasPendingDecision { get; private set; }
        public Incident PendingIncident { get; private set; }
        readonly List<Incident> blockIncidents = new List<Incident>();
        /// <summary>Son bloğun kart/sakatlık olayları (feed + panel) — her ResolveNext tazeler.</summary>
        public IReadOnlyList<Incident> LastBlockIncidents => blockIncidents;
        /// <summary>Son gol bloğunda golü atan oyuncu (kozmetik atıf) — gol yoksa null.</summary>
        public string LastScorerName { get; private set; }

        /// <summary>Blok başlangıç dakikası (gösterim: 10 blok → 9'ar dk).</summary>
        public int BlockMinute(int block) => (int)Math.Round(90.0 * block / m.blokSayisi);

        public MatchModel(GreyboxBalance balance, MatchSetup setup)
        {
            bal = balance;
            m = balance.model;
            seed = setup.Seed;
            usStrength = setup.HomeStrength;
            themStrength = setup.AwayStrength;
            usFormNet = setup.HomeFormNet;
            usTactic = FindTactic(setup.HomeTacticId);
            themTactic = FindTactic(setup.AwayTacticId);
            MovesLeft = m.hamleHakki;
            sq = balance.squad;
            ev = balance.olay;
            SquadUs = Squad.Generate(setup.Seed, 0, sq.enerjiBaslangic);
            SquadThem = Squad.Generate(setup.Seed, 1, sq.enerjiBaslangic);
            SubsLeft = sq.degisiklikHakki;
            themSubsLeft = sq.degisiklikHakki;
        }

        GreyboxBalance.TacticCfg FindTactic(int id)
        {
            for (int i = 0; i < bal.taktikler.Length; i++)
                if (bal.taktikler[i].id == id) return bal.taktikler[i];
            return bal.taktikler[0];
        }

        // ---------------------------------------------------------------- olasılık modeli
        // KRİTER MODELİ (docs/GREYBOX_MODEL.md): p = Taban × Güç × Taktik × Faz × Momentum
        //                                          × Skor × TempoModu × Ev × Form → clamp.
        // Her etken [KALİBRE-G model.*] anahtarıyla ayarlanır; FactorSnapshot ile şeffaftır.

        /// <summary>Bir tarafın blok gol olasılığının tam etken dökümü (sıradaki blok, güncel enerji).</summary>
        public FactorSnapshot Factors(bool us) =>
            FactorsAt(us, Math.Min(CurrentBlock, m.blokSayisi - 1),
                      SquadUs.TeamEnergyMean(), SquadThem.TeamEnergyMean());

        /// <summary>Parametrik etken hesabı — DP projeksiyonu ileri blokları faz+enerji ile çağırır.
        /// Skor/momentum/eksikler mevcut değerlerinde sabittir (stokastik — dürüst yaklaşıklık).</summary>
        FactorSnapshot FactorsAt(bool us, int blockIdx, float eUs, float eThem)
        {
            var atk = us ? usTactic : themTactic;
            var def = us ? themTactic : usTactic;
            float strDiff = us ? usStrength - themStrength : themStrength - usStrength;
            float mom = us ? Momentum : -Momentum;
            int diff = us ? GoalsUs - GoalsThem : GoalsThem - GoalsUs;

            var f = new FactorSnapshot { Taban = m.pGolTabani };

            // 1) Kadro gücü — tanh ile doygun: uç farklar patlamaz
            f.Guc = 1f + m.gucEtkiMax * (float)Math.Tanh(strDiff / m.gucOlcek);

            // 2) Taktik: etkileşim matrisi (hücum eden × savunan) × tempo/şut iştahı inceliği
            float matchup = MatchupFactor(atk.id, def.id);
            f.Taktik = matchup
                       * (1f + (atk.tempo - 1f) * m.taktikTempoEtki)
                       * (1f + (atk.sutIstahi - 1f) * m.taktikSutEtki)
                       * (1f - (def.pres - 1f) * m.taktikPresSavunmaEtki);

            // 3) Maç fazı: son bloklarda gol frekansı yükselir (gerçek futbol istatistiği)
            f.Faz = m.fazCarpanlar != null && m.fazCarpanlar.Length > blockIdx
                ? m.fazCarpanlar[blockIdx] : 1f;

            // 4) Momentum
            f.Momentum = 1f + mom * m.momentumEtki;

            // 5) Skor durumu: geride olan riske girer, önde olan soğutur
            f.Skor = diff < 0 ? m.gerideRiskCarpan : diff > 0 ? m.ondeKontrolCarpan : 1f;

            // 6) Müdahale modu (tempo)
            f.TempoModu = Tempo == TempoMode.Yukselt ? (us ? m.tempoYukseltBiz : m.tempoYukseltRakip)
                        : Tempo == TempoMode.Kilitlen ? (us ? m.kilitlenBiz : m.kilitlenRakip)
                        : 1f;

            // 7) Ev sahibi avantajı (greybox'ta oyuncu hep ev sahibi)
            f.Ev = us ? 1f + m.evAvantaj : 1f - m.evAvantaj * 0.5f;

            // 8) Form: son 5 maçın net galibiyeti (yalnız oyuncu tarafı bilinir)
            f.Form = us ? 1f + usFormNet * m.formEtkiCarpan : 1f;

            // 9) Yorgunluk: takım enerjisi güç etkisini kademeli düşürür — ME Spec 12.1
            //    M_kondisyon'un blok ölçekli vekili (taze takımda ×1.0, bitkinlikte tabana iner)
            float eSelf = us ? eUs : eThem;
            f.Yorgunluk = sq.yorgunlukGucTaban
                          + (1f - sq.yorgunlukGucTaban) * (eSelf / sq.enerjiBaslangic);

            // 10) Eksik oyuncu: kırmızı/değiştirilmemiş sakatlık hücumu düşürür, rakip eksiği
            //     bizim hücumu güçlendirir (savunma seyrekleşir)
            int missSelf = us ? SquadUs.MissingCount() : SquadThem.MissingCount();
            int missOpp = us ? SquadThem.MissingCount() : SquadUs.MissingCount();
            f.Eksik = PowInt(sq.eksikHucumCarpan, missSelf)
                      * PowInt(sq.rakipEksikSavunmaCarpan, missOpp);

            f.Sonuc = Clamp(f.Taban * f.Guc * f.Taktik * f.Faz * f.Momentum * f.Skor
                            * f.TempoModu * f.Ev * f.Form * f.Yorgunluk * f.Eksik,
                            m.pGolMin, m.pGolMax);
            return f;
        }

        static float PowInt(float b, int e)
        {
            float r = 1f;
            for (int i = 0; i < e; i++) r *= b;
            return r;
        }

        float MatchupFactor(int atkId, int defId)
        {
            if (m.taktikMatchup == null || m.taktikMatchup.Length < 9) return 1f;
            int r = Math.Max(0, Math.Min(2, atkId));
            int c = Math.Max(0, Math.Min(2, defId));
            return m.taktikMatchup[r * 3 + c];
        }

        float PGoal(bool us) => Factors(us).Sonuc;

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
            if (HasPendingDecision)
                throw new InvalidOperationException(
                    "Sakatlık kararı bekleniyor — önce model.substitution ya da model.continue_short (Tek Kapı).");

            var pv = PreviewNext();
            uint tick = (uint)CurrentBlock;
            XgUs += pv.PGoalUs;   // model-xG: gösterilen olasılıkların birikimi
            XgThem += pv.PGoalThem;
            double r = Rng.Rand01(seed, Domain.Duel, 500, tick, 1);
            BlockOutcome outcome;
            if (r < pv.PGoalUs) { outcome = BlockOutcome.GoalUs; GoalsUs++; }
            else if (r < pv.PGoalUs + pv.PGoalThem) { outcome = BlockOutcome.GoalThem; GoalsThem++; }
            else
            {
                double rd = Rng.Rand01(seed, Domain.Duel, 501, tick, 2);
                outcome = rd < (pv.PGoalUs + pv.PGoalThem) * m.tehlikeCarpan * 0.5
                    ? BlockOutcome.Danger : BlockOutcome.Quiet;
                if (outcome == BlockOutcome.Danger)
                {
                    // Tehlikenin tarafı: olasılık oranıyla — Domain.Duel
                    double rs = Rng.Rand01(seed, Domain.Duel, 503, tick, 5);
                    LastDangerSide = rs < pv.PGoalUs / Math.Max(1e-4f, pv.PGoalUs + pv.PGoalThem) ? 0 : 1;
                    if (LastDangerSide == 0) DangerUs++; else DangerThem++;
                }
            }

            // Gol atfı — KOZMETİK (skor Duel akışında çoktan belirlendi): Domain.Crowd
            LastScorerName = outcome == BlockOutcome.GoalUs ? AttributeGoal(SquadUs, 0, tick)
                           : outcome == BlockOutcome.GoalThem ? AttributeGoal(SquadThem, 1, tick)
                           : null;

            // Momentum güncellemesi — Domain.Chaos: blok salınımı
            float g = (float)Rng.Gauss01(seed, Domain.Chaos, 502, tick, 3);
            Momentum += g * m.momentumBlokGurultu - Momentum * m.momentumSonum;
            if (outcome == BlockOutcome.GoalUs) Momentum += m.momentumGolDelta;
            if (outcome == BlockOutcome.GoalThem) Momentum -= m.momentumGolDelta;
            Momentum = Clamp(Momentum, -1f, 1f);

            DrainEnergy();
            RollIncidents(tick);

            CurrentBlock++;
            return outcome;
        }

        // ---------------------------------------------------------------- kadro: yorgunluk + olaylar (İt.11)

        /// <summary>Mevcut tempo/taktikle blok başına takım drenaj oranı — deterministik (zarsız),
        /// DP projeksiyonu aynı oranı kullanır (İt.11 A1).</summary>
        public float DrainRate(int team)
        {
            var tac = team == 0 ? usTactic : themTactic;
            float own = Tempo == TempoMode.Yukselt ? sq.drenajTempoYukselt
                      : Tempo == TempoMode.Kilitlen ? sq.drenajKilitlen : 1f;
            // Bizim tempomuz maçın temposudur: rakibe kısmen yansır [drenajRakipEtki]
            float tempoMul = team == 0 ? own : 1f + (own - 1f) * sq.drenajRakipEtki;
            float taktikMul = 1f + (tac.tempo - 1f) * sq.drenajTaktikEtki;
            return sq.yorgunlukBlokDrenaj * tempoMul * taktikMul;
        }

        void DrainEnergy()
        {
            for (int t = 0; t < 2; t++)
            {
                var squad = t == 0 ? SquadUs : SquadThem;
                float rate = DrainRate(t);
                for (int i = 0; i < squad.Players.Length; i++)
                {
                    var p = squad.Players[i];
                    if (!p.OnPitch) continue;
                    p.Energy = Math.Max(0f,
                        p.Energy - rate * (p.Pos == PlayerPos.GK ? sq.gkDrenajCarpan : 1f));
                }
            }
        }

        /// <summary>Kart + sakatlık olayları — ME Spec 11.2/12.2'nin blok ölçekli vekilleri.
        /// Ayrı domain akışları (Referee/Injury): skor zarına (Duel) DOKUNMAZ, determinizm korunur.
        /// Takım sırası sabittir (0 sonra 1) — iterasyon sırası belirsizliği yok.</summary>
        void RollIncidents(uint tick)
        {
            blockIncidents.Clear();
            for (int t = 0; t < 2; t++)
            {
                var squad = t == 0 ? SquadUs : SquadThem;
                // Agresiflik: bizde tempo müdahalesi; rakipte taktik presi (vekil)
                bool aggressive = t == 0 ? Tempo == TempoMode.Yukselt : themTactic.pres > 1.05f;

                // Sarı kart — Domain.Referee (bantlar maç TOPLAMI → taraf başına yarısı)
                double pS = ev.sariMacBasi * 0.5 / m.blokSayisi
                            * (aggressive ? ev.kartTempoYukseltCarpan : 1f);
                if (Rng.Rand01(seed, Domain.Referee, (uint)(600 + t), tick, 1) < pS)
                {
                    var victim = PickCardVictim(squad, t, tick, salt: 2, aggressive);
                    if (victim != null)
                    {
                        victim.Yellow++;
                        bool second = victim.Yellow >= 2;
                        if (second) { victim.SentOff = true; victim.OnPitch = false; }
                        blockIncidents.Add(new Incident
                        {
                            Type = second ? IncidentType.SecondYellowRed : IncidentType.Yellow,
                            Team = t, PlayerId = victim.Id, Block = CurrentBlock, AutoSubInId = -1
                        });
                    }
                }

                // Direkt kırmızı — Domain.Referee
                double pK = ev.kirmiziMacBasi * 0.5 / m.blokSayisi;
                if (Rng.Rand01(seed, Domain.Referee, (uint)(600 + t), tick, 3) < pK)
                {
                    var victim = PickCardVictim(squad, t, tick, salt: 4, aggressive: false);
                    if (victim != null)
                    {
                        victim.SentOff = true;
                        victim.OnPitch = false;
                        blockIncidents.Add(new Incident
                        {
                            Type = IncidentType.RedDirect, Team = t, PlayerId = victim.Id,
                            Block = CurrentBlock, AutoSubInId = -1
                        });
                    }
                }

                // Sakatlık — Domain.Injury; yorgun takım daha riskli (ME 12.2 M_yorgunluk vekili)
                float eMean = squad.TeamEnergyMean();
                double pI = ev.sakatlikMacBasi * 0.5 / m.blokSayisi
                            * (1.0 + (1.0 - eMean / sq.enerjiBaslangic) * ev.sakatlikYorgunlukEtki);
                if (Rng.Rand01(seed, Domain.Injury, (uint)(610 + t), tick, 1) < pI)
                {
                    var victim = PickInjuryVictim(squad, t, tick);
                    if (victim != null)
                    {
                        victim.Injured = true;
                        victim.OnPitch = false;
                        var inc = new Incident
                        {
                            Type = IncidentType.Injury, Team = t, PlayerId = victim.Id,
                            Block = CurrentBlock, AutoSubInId = -1
                        };
                        if (t == 0)
                        {
                            // ZORUNLU KARAR ANI (Öneri İt.11 A2): değiştir ya da eksik devam.
                            // Hak/yedek yoksa karar yok — eksik devam (feed bunu söyler).
                            if (CanSubstituteAny())
                            {
                                HasPendingDecision = true;
                                PendingIncident = inc;
                            }
                        }
                        else
                        {
                            inc.AutoResolved = true;
                            inc.AutoSubInId = AutoResolveThem(victim);
                        }
                        blockIncidents.Add(inc);
                    }
                }
            }
        }

        SquadPlayer PickCardVictim(Squad squad, int team, uint tick, uint salt, bool aggressive)
        {
            float total = 0f;
            for (int i = 0; i < squad.Players.Length; i++)
                total += CardWeight(squad.Players[i], aggressive);
            if (total <= 0f) return null;
            double roll = Rng.Rand01(seed, Domain.Referee, (uint)(600 + team), tick, salt) * total;
            for (int i = 0; i < squad.Players.Length; i++)
            {
                var p = squad.Players[i];
                float w = CardWeight(p, aggressive);
                if (w <= 0f) continue;
                roll -= w;
                if (roll <= 0) return p;
            }
            return null;
        }

        float CardWeight(SquadPlayer p, bool aggressive)
        {
            if (!p.OnPitch || p.Pos == PlayerPos.GK) return 0f;
            float w = ev.kartMevkiAgirlik[(int)p.Pos - 1];
            if (aggressive && p.Yellow > 0) w *= ev.ikinciSariAgirlik; // kartlı oyuncu agresif tempoda risk
            return w;
        }

        SquadPlayer PickInjuryVictim(Squad squad, int team, uint tick)
        {
            float total = 0f;
            for (int i = 0; i < squad.Players.Length; i++)
                total += InjuryWeight(squad.Players[i]);
            if (total <= 0f) return null;
            double roll = Rng.Rand01(seed, Domain.Injury, (uint)(610 + team), tick, 2) * total;
            for (int i = 0; i < squad.Players.Length; i++)
            {
                var p = squad.Players[i];
                float w = InjuryWeight(p);
                if (w <= 0f) continue;
                roll -= w;
                if (roll <= 0) return p;
            }
            return null;
        }

        float InjuryWeight(SquadPlayer p)
        {
            if (!p.OnPitch || p.Pos == PlayerPos.GK) return 0f;
            // Bireysel yorgunluk sakatlık yatkınlığıdır — ME 12.2 vekili (aynı [KALİBRE-G] etki)
            return 1f + (1f - p.Energy / sq.enerjiBaslangic) * ev.sakatlikYorgunlukEtki;
        }

        bool CanSubstituteAny()
        {
            if (SubsLeft <= 0) return false;
            for (int i = 11; i < SquadUs.Players.Length; i++)
            {
                var p = SquadUs.Players[i];
                if (!p.OnPitch && !p.Injured && !p.SentOff && p.Pos != PlayerPos.GK) return true;
            }
            return false;
        }

        /// <summary>Rakip sakatlığı otomatik çözer: aynı mevkiden yedek, yoksa ilk uygun, yoksa eksik.</summary>
        int AutoResolveThem(SquadPlayer victim)
        {
            if (themSubsLeft <= 0) return -1;
            int pick = -1;
            for (int i = 11; i < SquadThem.Players.Length; i++)
            {
                var p = SquadThem.Players[i];
                if (p.OnPitch || p.Injured || p.SentOff || p.Pos == PlayerPos.GK) continue;
                if (pick < 0) pick = i;
                if (p.Pos == victim.Pos) { pick = i; break; }
            }
            if (pick < 0) return -1;
            var sub = SquadThem.Players[pick];
            sub.OnPitch = true;
            sub.Energy = sq.tazeBacakEnerji;
            themSubsLeft--;
            return pick;
        }

        /// <summary>Gol atfı — kozmetik (Domain.Crowd), forvet ağırlıklı [golMevkiAgirlik].</summary>
        string AttributeGoal(Squad squad, int team, uint tick)
        {
            float total = 0f;
            for (int i = 0; i < squad.Players.Length; i++)
            {
                var p = squad.Players[i];
                if (p.OnPitch && p.Pos != PlayerPos.GK) total += ev.golMevkiAgirlik[(int)p.Pos - 1];
            }
            if (total <= 0f) return null;
            double roll = Rng.Rand01(seed, Domain.Crowd, (uint)(620 + team), tick, 7) * total;
            for (int i = 0; i < squad.Players.Length; i++)
            {
                var p = squad.Players[i];
                if (!p.OnPitch || p.Pos == PlayerPos.GK) continue;
                roll -= ev.golMevkiAgirlik[(int)p.Pos - 1];
                if (roll <= 0) { p.Goals++; return p.Name; }
            }
            return null;
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

        /// <summary>Oyuncu değişikliği (Tek Kapı: model.substitution) — sakatlık kararını da çözer.
        /// Hak [squad.degisiklikHakki] hamle hakkından AYRIDIR (GDD 12.4 standart 3'e hiza).</summary>
        public bool TrySubstitute(int outId, int inId)
        {
            if (IsFinished || SubsLeft <= 0) return false;
            var pOut = SquadUs.Find(outId);
            var pIn = SquadUs.Find(inId);
            if (pOut == null || pIn == null) return false;
            bool resolvesInjury = HasPendingDecision && PendingIncident.PlayerId == outId;
            if (!pOut.OnPitch && !resolvesInjury) return false; // sakat oyuncu zaten dışarıda: yeri doldurulur
            if (pIn.OnPitch || pIn.SentOff || pIn.Injured) return false;
            if (pOut.Pos == PlayerPos.GK || pIn.Pos == PlayerPos.GK) return false; // greybox: kaleci değişmez
            pOut.OnPitch = false;
            pIn.OnPitch = true;
            pIn.Energy = sq.tazeBacakEnerji; // taze bacak — enerji/güç görünür toparlanır
            SubsLeft--;
            if (resolvesInjury) HasPendingDecision = false;
            return true;
        }

        /// <summary>Sakatlıkta "eksik devam" kararı (Tek Kapı: model.continue_short) — hak yakmaz;
        /// takım eksik kalır, bedeli Eksik etkeni öder.</summary>
        public bool TryContinueShort()
        {
            if (!HasPendingDecision) return false;
            HasPendingDecision = false;
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
            int offset = remaining;                    // fark indeksi kaydırması
            int size = remaining * 2 + 1;
            var dist = new double[size];
            dist[offset] = 1.0;                        // fark 0'dan başla (kalan bloklar için)

            // İt.11: faz eğrisi ve enerji drenajı blok blok İLERİ projeksiyon edilir — ikisi de
            // deterministik olduğundan şerit "bu gidişat sürerse" sorusuna daha dürüst cevap verir.
            float eU = SquadUs.TeamEnergyMean(), eT = SquadThem.TeamEnergyMean();
            float dU = DrainRate(0), dT = DrainRate(1);

            for (int b = 0; b < remaining; b++)
            {
                int blockIdx = Math.Min(CurrentBlock + b, m.blokSayisi - 1);
                float pU = FactorsAt(us: true, blockIdx, eU, eT).Sonuc;
                float pT = FactorsAt(us: false, blockIdx, eU, eT).Sonuc;
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
                eU = Math.Max(0f, eU - dU);
                eT = Math.Max(0f, eT - dT);
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
