using System;
using TheBadge.CommandBus;
using TheBadge.Sim.Commands;

namespace TheBadge.World
{
    /// <summary>Persona diyaloğu ve basın yanıtı için SUNUM kanalı. Metin üretimi LLM'in işi ve
    /// bu paketin DIŞINDA; buraya yalnız SINIFLANMIŞ sonuç düşer (CB 7.3: "kapalı enum'lara
    /// SINIFLANIR; metnin yaratıcılığı mekaniği asla taşırmaz").</summary>
    public interface IPersonaSink
    {
        void KonusmaAyarlandi(Guid commandId, int personaId, byte tonIndeksi, long userId);
        void BasinYaniti(Guid commandId, int soruId, byte cevapSinifi, long userId);
    }

    /// <summary>CB 4.3-4.4 SON DÖRT AKSİYON — `social.arrange_talk`, `social.press_response`,
    /// `staff.hire`, `staff.activate_premium`. Bunlarla katalog 32/32 KAPANIR.
    ///
    /// social.* Tier 0'dır ve LLM hattının çıkışıdır: mekanik etki BANTLI ve ENUM'ludur, metin
    /// yalnız sunumda yaşar. staff.* Tier 1-2'dir ve kalıcı durumu düzenler.</summary>
    public static class SocialStaffActions
    {
        public static readonly string[] TonEnum = { "sakinlestir", "atesle", "uyar" };

        public static void Baglan(WorldContext ctx, WorldExecutor exec, WorldRules kural, IPersonaSink persona)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            if (exec == null) throw new ArgumentNullException(nameof(exec));
            if (kural == null) throw new ArgumentNullException(nameof(kural));

            exec.PersonaKanalBagla(persona);

            ctx.RegisterRule("staff.hire", new PersonelKurali(kural));
            ctx.RegisterRule("staff.activate_premium", new PremiumKurali());

            exec.RegisterHandler("social.arrange_talk", new KonusmaHandler());
            exec.RegisterHandler("social.press_response", new BasinHandler());
            exec.RegisterHandler("staff.hire", new PersonelHandler(kural));
            exec.RegisterHandler("staff.activate_premium", new PremiumHandler());
        }

        // ---------------------------------------------------------------- KAPI 3

        sealed class PersonelKurali : IActionRule
        {
            readonly WorldRules kural;
            public PersonelKurali(WorldRules k) { kural = k; }
            public RejectionReason Check(GameState st, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("tip", out long tip)) return RejectionReason.SchemaViolation;
                // AYNI TİPTEN İKİNCİ personel alınmaz: iki "finans direktörü"nün etkisi
                // toplanır mı çarpılır mı belirsiz kalırdı — sessiz belirsizlik yerine net red.
                for (int i = 0; i < st.Club.Personel.Length; i++)
                    if (st.Club.Personel[i].Tip == (byte)tip)
                    { detail = "bu tipte personel zaten var"; return RejectionReason.StateConflict; }
                if (BosSlot(st) < 0) { detail = "personel kadrosu dolu"; return RejectionReason.StateConflict; }
                return RejectionReason.None;
            }
        }

        sealed class PremiumKurali : IActionRule
        {
            public RejectionReason Check(GameState st, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("envanterId", out long eid)) return RejectionReason.SchemaViolation;
                if (st.Club.AktifPremiumId == (int)eid)
                { detail = "bu envanter zaten aktif"; return RejectionReason.StateConflict; }
                return RejectionReason.None;
            }
        }

        // ---------------------------------------------------------------- YÜRÜTÜCÜLER

        /// <summary>Persona konuşması — kalıcı durumu DEĞİŞTİRMEZ. Mekanik etki (moral) maç
        /// motorunun ve hikaye katmanının işidir; burada olan, SINIFLANMIŞ tonun kanala
        /// düşmesidir. Ton `TonEnum` dışına çıkamaz: LLM ne yazarsa yazsın, mekaniğe giren şey
        /// üç değerden biridir (CB 7.3).</summary>
        sealed class KonusmaHandler : IActionHandler
        {
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("personaId", out long pid) || !p.TryGetText("ton", out string ton))
                    return RejectionReason.SchemaViolation;
                // ENUM DENETİMİ BURADA YAPILMAZ: katalog `ton`u `ParamType.Enum` + `TonEnum` ile
                // tanımlıyor ve KAPI 1 (şema) enum dışını bizden ÖNCE eliyor. Buraya bir denetim
                // daha koymak ölü koddu — bu dilimde dördüncü kez aynı tuzak (K5'te sahiplik,
                // K5'te kadro sınırı, K6'da... ). `EnumIndex` yalnız İNDEKSE çevirmek için var.
                int ti = TransferActions.EnumIndex(TonEnum, ton);
                if (ti < 0) { detail = "ton: " + ton; return RejectionReason.SchemaViolation; }  // ulaşılmaz; savunma
                j.PersonaKonusma(env.CommandId, (int)pid, (byte)ti, env.UserId);
                return RejectionReason.None;
            }
        }

        sealed class BasinHandler : IActionHandler
        {
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("soruId", out long sid) || !p.TryGetInt("cevapSinifi", out long cs))
                    return RejectionReason.SchemaViolation;
                j.PersonaBasin(env.CommandId, (int)sid, (byte)cs, env.UserId);
                return RejectionReason.None;
            }
        }

        sealed class PersonelHandler : IActionHandler
        {
            readonly WorldRules kural;
            public PersonelHandler(WorldRules k) { kural = k; }
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("tip", out long tip) || !p.TryGetInt("tier", out long tier)
                    || !p.TryGetInt("sureYil", out long yil))
                    return RejectionReason.SchemaViolation;
                int slot = BosSlot(st);
                if (slot < 0) { detail = "personel kadrosu dolu"; return RejectionReason.StateConflict; }
                j.Set(MutTarget.Personel, slot, StaffField.Tip, tip);
                j.Set(MutTarget.Personel, slot, StaffField.Tier, tier);
                // Yıl→hafta çevrimi sezon uzunluğundan gelir [KALİBRE]; kodda sabit YOK
                // (K5'te 52 sabitiyle yapılan hatanın aynısı yapılmasın).
                j.Set(MutTarget.Personel, slot, StaffField.KalanHafta, yil * kural.yapi.sezonHaftaSayisi);
                j.Emit(new WorldEvent(WorldEventType.PersonelAlindi, (int)tip, tier, st.Takvim.Sezon, st.Takvim.Hafta));
                return RejectionReason.None;
            }
        }

        sealed class PremiumHandler : IActionHandler
        {
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("envanterId", out long eid)) return RejectionReason.SchemaViolation;
                j.Set(MutTarget.Kulup, 0, ClubField.AktifPremium, eid);
                j.Emit(new WorldEvent(WorldEventType.PremiumAktif, (int)eid, 0, st.Takvim.Sezon, st.Takvim.Hafta));
                return RejectionReason.None;
            }
        }

        internal static int BosSlot(GameState st)
        {
            for (int i = 0; i < st.Club.Personel.Length; i++)
                if (st.Club.Personel[i].Tip == 0) return i;
            return -1;
        }
    }
}
