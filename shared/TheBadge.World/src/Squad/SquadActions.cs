using System;
using TheBadge.CommandBus;
using TheBadge.Sim.Commands;
using TheBadge.Sim.Match;

namespace TheBadge.World
{
    /// <summary>MAÇ KOMUT KÖPRÜSÜ — CB 4.2'nin maç bağlamlı aksiyonları `GameState`e değil maç
    /// motorunun kuyruğuna gider (ME 5.4/14.2). `MatchCommands.cs` bunu zaten öngörüyordu:
    /// "Komutlar Command Bus zarfından çözülüp bu kayıtlara çevrilir". Host bunu ME'nin
    /// `CommandQueue`una bağlar; dünya paketi ME'ye doğrudan bağımlı olmaz.</summary>
    public interface IMatchCommandSink
    {
        void Enqueue(MatchCommand cmd);
    }

    /// <summary>CB 4.2 KADRO VE TAKTİK AKSİYONLARI — 9 aksiyon.
    ///
    /// İKİ HEDEFLİ YÖNLENDİRME: `squad.*` aksiyonları hem Hub hem Maç bağlamında geçerlidir
    /// (CB 4.2 tablosu). Hub'da KALICI kurulum düzenlenir (`GameState`), maçta CANLI müdahale
    /// yapılır (ME kuyruğu). Ayrım zarfın `MatchTick`inden gelir (CB 3.1: "0 = hub komutu") —
    /// aynı ayrım kapı 3'ün bağlam kesişiminde de kullanılıyor, yani tek kaynak.
    ///
    /// ARAYÜZ BOŞLUĞU (K4 bulgusu, 2026-08-29): CB 4.2 `set_player_anchor` ve `set_player_role`
    /// için "Hub + Maç" diyor, ama ME komut kümesinde (`MatchCommands.cs`) bu ikisinin karşılığı
    /// YOK; `InstructionCmd` var ama `PlayerInstr` kataloğu boş (`None = 0`, "M-müdahale dilimi
    /// genişletir"). Bu üç yol maç bağlamında SESSİZCE HİÇBİR ŞEY YAPMAK yerine açık sebeple
    /// reddedilir — sahte başarı üretmemek bu projenin tekrar eden dersi. Borç DECISIONS'ta.</summary>
    public static class SquadActions
    {
        public static readonly string[] TonEnum = { "sakinlestir", "atesle", "uyar" };

        /// <summary>Maç bağlamında ME karşılığı OLMAYAN aksiyonlar — reddedilir, no-op edilmez.</summary>
        static bool MacKarsiligiYok(string actionType)
            => actionType == "squad.set_player_anchor"
            || actionType == "squad.set_player_role"
            || actionType == "squad.set_instruction";

        public static void Baglan(WorldContext ctx, WorldExecutor exec, WorldRules kural, IMatchCommandSink macKuyrugu)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            if (exec == null) throw new ArgumentNullException(nameof(exec));
            if (kural == null) throw new ArgumentNullException(nameof(kural));
            // `macKuyrugu` null OLABİLİR: hub-only host (ör. offline menü) maç komutu almaz.
            // O hâlde maç bağlamlı aksiyon reddedilir — sessizce yutulmaz.

            ctx.RegisterRule("squad.set_captain", new KaptanKurali());
            ctx.RegisterRule("squad.save_tactic_preset", new PresetKurali());
            // Maç karşılığı olmayan üçü SARMALANIR — kural okuyup üstüne yazmak yerine önce
            // bestelenip bir kez kaydedilir (çifte kayıt zaten reddediliyor).
            ctx.RegisterRule("squad.set_player_anchor", new MacKarsiligiKurali(null));
            ctx.RegisterRule("squad.set_player_role", new MacKarsiligiKurali(null));
            ctx.RegisterRule("squad.set_instruction", new MacKarsiligiKurali(new TalimatKurali()));

            // Kuyruk YÜRÜTÜCÜye bağlanır: yayınlama commit'in parçasıdır (inceleme bulgusu, P1).
            exec.MacKuyruguBagla(macKuyrugu);

            exec.RegisterHandler("squad.set_player_anchor", new AnchorHandler());
            exec.RegisterHandler("squad.set_player_role", new RolHandler());
            exec.RegisterHandler("squad.set_instruction", new TalimatHandler());
            exec.RegisterHandler("squad.set_team_tactic", new TaktikHandler(kural, macKuyrugu));
            exec.RegisterHandler("squad.save_tactic_preset", new PresetHandler());
            exec.RegisterHandler("squad.set_captain", new KaptanHandler());
            exec.RegisterHandler("squad.set_training_plan", new AntrenmanHandler());
            exec.RegisterHandler("match.substitution", new DegisiklikHandler(macKuyrugu));
            exec.RegisterHandler("match.motivation_talk", new MotivasyonHandler(macKuyrugu));
        }

        // ---------------------------------------------------------------- KAPI 3 KURALLARI

        /// <summary>Maç bağlamında ME karşılığı olmayan aksiyonları reddeder; hub'da varsa
        /// sarmalanan kurala devreder. Reddin KAPI 3'ten gelmesi bilinçli: komut yürütücüye hiç
        /// ulaşmadan düşer, yani ön-doğrulama da aynı cevabı verir (istemci UI'ı doğru gösterir).</summary>
        sealed class MacKarsiligiKurali : IActionRule
        {
            readonly IActionRule ic;
            public MacKarsiligiKurali(IActionRule inner) { ic = inner; }
            public RejectionReason Check(GameState st, CommandEnvelope env, ActionDef action, IPayloadView p, out string detail)
            {
                if (env.MatchTick > 0 && MacKarsiligiYok(action.ActionType))
                {
                    detail = "maç içi karşılığı yok (ME komut kümesi borcu, DECISIONS)";
                    return RejectionReason.StateConflict;
                }
                if (ic != null) return ic.Check(st, env, action, p, out detail);
                detail = null; return RejectionReason.None;
            }
        }

        sealed class KaptanKurali : IActionRule
        {
            public RejectionReason Check(GameState st, CommandEnvelope env, ActionDef action, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("oyuncuId", out long pid)) return RejectionReason.SchemaViolation;
                int i = st.IndexOfPlayer((int)pid);
                if (i < 0 || st.Oyuncular[i].SakatlikHafta > 0)
                { detail = "kaptan sakat ya da kadroda değil"; return RejectionReason.StateConflict; }
                return RejectionReason.None;
            }
        }

        sealed class PresetKurali : IActionRule
        {
            public RejectionReason Check(GameState st, CommandEnvelope env, ActionDef action, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("slot", out long slot)) return RejectionReason.SchemaViolation;
                if (st.PresetIndex((int)slot) < 0) { detail = "preset slotu kapsam dışı"; return RejectionReason.StateConflict; }
                return RejectionReason.None;
            }
        }

        sealed class TalimatKurali : IActionRule
        {
            public RejectionReason Check(GameState st, CommandEnvelope env, ActionDef action, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("oyuncuId", out long pid) || !p.TryGetInt("talimatId", out long tid))
                    return RejectionReason.SchemaViolation;
                int i = st.IndexOfPlayer((int)pid);
                if (i < 0) return RejectionReason.NotOwned;
                if (st.InstructionSlot(i, (byte)tid) < 0)
                { detail = "talimat yuvası dolu"; return RejectionReason.StateConflict; }
                return RejectionReason.None;
            }
        }

        // ---------------------------------------------------------------- YÜRÜTÜCÜLER

        sealed class AnchorHandler : IActionHandler
        {
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("oyuncuId", out long pid) || !p.TryGetInt("x", out long x) || !p.TryGetInt("y", out long y))
                    return RejectionReason.SchemaViolation;
                int i = st.IndexOfPlayer((int)pid);
                if (i < 0) return RejectionReason.NotOwned;
                j.OyuncuSet(i, PlayerField.AnchorX, x);
                j.OyuncuSet(i, PlayerField.AnchorY, y);
                j.Emit(new WorldEvent(WorldEventType.TaktikGuncellendi, (int)pid, (x << 20) ^ y, st.Takvim.Sezon, st.Takvim.Hafta));
                return RejectionReason.None;
            }
        }

        sealed class RolHandler : IActionHandler
        {
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("oyuncuId", out long pid) || !p.TryGetInt("rolId", out long rol))
                    return RejectionReason.SchemaViolation;
                int i = st.IndexOfPlayer((int)pid);
                if (i < 0) return RejectionReason.NotOwned;
                j.OyuncuSet(i, PlayerField.RolId, rol);
                j.Emit(new WorldEvent(WorldEventType.TaktikGuncellendi, (int)pid, rol, st.Takvim.Sezon, st.Takvim.Hafta));
                return RejectionReason.None;
            }
        }

        sealed class TalimatHandler : IActionHandler
        {
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("oyuncuId", out long pid) || !p.TryGetInt("talimatId", out long tid)
                    || !p.TryGetInt("deger", out long deger))
                    return RejectionReason.SchemaViolation;
                int i = st.IndexOfPlayer((int)pid);
                if (i < 0) return RejectionReason.NotOwned;
                int yuva = st.InstructionSlot(i, (byte)tid);
                if (yuva < 0) { detail = "talimat yuvası dolu"; return RejectionReason.StateConflict; }
                // ADRES ŞERİDİ TEK KAYNAK: journal çözerken `Oyuncular[0].Talimatlar.Length`
                // kullanıyor; burada oyuncunun KENDİ uzunluğunu kullanmak, diziler bir gün
                // ayrışırsa yazmayı sessizce BAŞKA oyuncuya düşürürdü. Aynı ifadeyi kullan.
                int serit = st.Oyuncular[0].Talimatlar.Length;
                int adres = i * serit + yuva;
                j.Set(MutTarget.Talimat, adres, InstructionField.TalimatId, tid);
                j.Set(MutTarget.Talimat, adres, InstructionField.Deger, deger);
                j.Emit(new WorldEvent(WorldEventType.TaktikGuncellendi, (int)pid, (tid << 8) ^ deger, st.Takvim.Sezon, st.Takvim.Hafta));
                return RejectionReason.None;
            }
        }

        /// <summary>Takım taktiği — TEK aksiyon, İKİ hedef. Hub'da kalıcı taktiği kaydırır
        /// (delta → mutlak, balance'taki adımla, kırpmalı); maçta ME kuyruğuna `TacticChangeCmd`
        /// olarak gider (ME 14.2 uygulanma anları motorun işi).</summary>
        sealed class TaktikHandler : IActionHandler
        {
            readonly WorldRules kural; readonly IMatchCommandSink kuyruk;
            public TaktikHandler(WorldRules k, IMatchCommandSink q) { kural = k; kuyruk = q; }
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("mentalite", out long men) || !p.TryGetInt("tempo", out long tem)
                    || !p.TryGetInt("pres", out long pres) || !p.TryGetInt("hat", out long hat))
                    return RejectionReason.SchemaViolation;

                if (env.MatchTick > 0)
                {
                    if (kuyruk == null) { detail = "maç kuyruğu bağlı değil"; return RejectionReason.StateConflict; }
                    j.MacKomutu(new TacticChangeCmd(env.MatchTick, env.TeamIdx,
                        new TacticDelta((sbyte)men, (sbyte)tem, (sbyte)pres, (sbyte)hat)));
                    return RejectionReason.None;
                }

                long Kaydir(byte mevcut, long delta)
                {
                    long v = mevcut + delta * kural.taktik.adim;
                    if (v < kural.taktik.min) v = kural.taktik.min;
                    if (v > kural.taktik.max) v = kural.taktik.max;
                    return v;
                }
                j.Set(MutTarget.Taktik, 0, TacticField.Mentalite, Kaydir(st.Taktik.Mentalite, men));
                j.Set(MutTarget.Taktik, 0, TacticField.Tempo, Kaydir(st.Taktik.Tempo, tem));
                j.Set(MutTarget.Taktik, 0, TacticField.Pres, Kaydir(st.Taktik.Pres, pres));
                j.Set(MutTarget.Taktik, 0, TacticField.Hat, Kaydir(st.Taktik.Hat, hat));
                j.Emit(new WorldEvent(WorldEventType.TaktikGuncellendi, 0, men, st.Takvim.Sezon, st.Takvim.Hafta));
                return RejectionReason.None;
            }
        }

        sealed class PresetHandler : IActionHandler
        {
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetText("ad", out string ad) || !p.TryGetInt("slot", out long slot))
                    return RejectionReason.SchemaViolation;
                int i = st.PresetIndex((int)slot);
                if (i < 0) { detail = "preset slotu kapsam dışı"; return RejectionReason.StateConflict; }
                // Şablon ANLIK taktiği dondurur (GDD 3.3 "Özel Kaydetme")
                j.Set(MutTarget.Preset, i, PresetField.Slot, slot);
                j.Set(MutTarget.Preset, i, PresetField.Mentalite, st.Taktik.Mentalite);
                j.Set(MutTarget.Preset, i, PresetField.Tempo, st.Taktik.Tempo);
                j.Set(MutTarget.Preset, i, PresetField.Pres, st.Taktik.Pres);
                j.Set(MutTarget.Preset, i, PresetField.Hat, st.Taktik.Hat);
                j.PresetAd(i, ad);
                j.Emit(new WorldEvent(WorldEventType.TaktikGuncellendi, (int)slot, 0, st.Takvim.Sezon, st.Takvim.Hafta));
                return RejectionReason.None;
            }
        }

        sealed class KaptanHandler : IActionHandler
        {
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("oyuncuId", out long pid)) return RejectionReason.SchemaViolation;
                j.Set(MutTarget.Kulup, 0, ClubField.Kaptan, pid);
                j.Emit(new WorldEvent(WorldEventType.TaktikGuncellendi, (int)pid, 0, st.Takvim.Sezon, st.Takvim.Hafta));
                return RejectionReason.None;
            }
        }

        sealed class AntrenmanHandler : IActionHandler
        {
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("planId", out long plan) || !p.TryGetInt("yogunluk", out long yog))
                    return RejectionReason.SchemaViolation;
                j.Set(MutTarget.Kulup, 0, ClubField.AntrenmanPlan, plan);
                j.Set(MutTarget.Kulup, 0, ClubField.AntrenmanYogunluk, yog);
                j.Emit(new WorldEvent(WorldEventType.TaktikGuncellendi, (int)plan, yog, st.Takvim.Sezon, st.Takvim.Hafta));
                return RejectionReason.None;
            }
        }

        /// <summary>Oyuncu değişikliği — komut journal'da BEKLETİLİR, kuyruğa yürütücü yazar.
        /// `kuyruk` alanı burada YALNIZ "bağlı mı" denetimi içindir; yayınlama commit'in parçası.
        /// (Değişiklik hakkı kalıcı durumda azalır, o yüzden bu handler yazma da üretir.)
        /// `cikanId` SAHA SLOTU (0-21), `girenId` KULÜBE İNDEKSİdir (0-9) — M17 replay
        /// incelemesinde bu sözleşme bir kez yanlış kurulmuştu, bir daha kurulmasın diye
        /// bant adları (`match.sahaSlot`, `match.kulubeIndeks`) da bunu söylüyor.</summary>
        sealed class DegisiklikHandler : IActionHandler
        {
            readonly IMatchCommandSink kuyruk;
            public DegisiklikHandler(IMatchCommandSink q) { kuyruk = q; }
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("cikanId", out long cikan) || !p.TryGetInt("girenId", out long giren))
                    return RejectionReason.SchemaViolation;
                if (kuyruk == null) { detail = "maç kuyruğu bağlı değil"; return RejectionReason.StateConflict; }
                j.MacKomutu(new SubstitutionCmd(env.MatchTick, env.TeamIdx, (short)cikan, (short)giren));
                // Değişiklik HAKKI kalıcı durumda azalır (Kapı 3 `NoChargesLeft` bunu okuyor)
                j.Set(MutTarget.Mac, 0, MatchField.KalanDegisiklikHakki, st.KalanDegisiklikHakki - 1);
                return RejectionReason.None;
            }
        }

        sealed class MotivasyonHandler : IActionHandler
        {
            readonly IMatchCommandSink kuyruk;
            public MotivasyonHandler(IMatchCommandSink q) { kuyruk = q; }
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetText("ton", out string ton)) return RejectionReason.SchemaViolation;
                int idx = TycoonActions.EnumIndex(TonEnum, ton);
                if (idx < 0) { detail = "ton: " + ton; return RejectionReason.SchemaViolation; }
                if (kuyruk == null) { detail = "maç kuyruğu bağlı değil"; return RejectionReason.StateConflict; }
                j.MacKomutu(new MotivationCmd(env.MatchTick, env.TeamIdx, (ToneType)idx));
                return RejectionReason.None;
            }
        }
    }
}
