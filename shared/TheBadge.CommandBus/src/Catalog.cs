using System;
using System.Collections.Generic;

namespace TheBadge.CommandBus
{
    /// <summary>Onay katmanı — CB Spec 6. Tier KATALOGDA sabittir ve KAYNAKTAN BAĞIMSIZDIR:
    /// LLM kaynaklı komut tier'ını asla düşüremez, UI'dan gelen Tier 2 de aynı onayı ister.</summary>
    public enum Tier : byte { T0 = 0, T1 = 1, T2 = 2 }

    /// <summary>Aksiyonun geçerli olduğu bağlam — CB Spec 4 tablolarının "Bağlam" sütunu.
    /// Bayrak: bir aksiyon hem hub'da hem maçta geçerli olabilir (ör. squad.set_player_role).</summary>
    [Flags]
    public enum Context : byte { None = 0, Hub = 1, Match = 2, Online = 4 }

    /// <summary>Rate limit sınıfı — CB Spec 5.1 tablosu. Limitler balance'tan okunur.</summary>
    public enum RateClass : byte { Tactic = 0, Economic = 1, MatchCmd = 2, OnlineSocial = 3, ModB = 4 }

    /// <summary>Parametre tipi — şema doğrulamasının (CB 3.2) tip ayağı.</summary>
    public enum ParamType : byte { Int = 0, Number = 1, Text = 2, Enum = 3, Bool = 4 }

    /// <summary>Katalog parametre tanımı. Bant DEĞERLERİ burada DEĞİL, `balance/command.bands.json`
    /// dosyasındadır (CB 5: "tüm bant değerleri balance JSON'dan okunur") — kodda magic number yok.
    /// Buradaki `BandKey` o dosyadaki anahtardır; boşsa parametrenin sayısal bandı yoktur.</summary>
    public sealed class ParamDef
    {
        public readonly string Name;
        public readonly ParamType Type;
        public readonly bool Required;
        public readonly string BandKey;      // balance/command.bands.json anahtarı (Int/Number için)
        public readonly string[] EnumValues; // Enum için kabul edilen değerler (katalog sabiti)
        public readonly int MaxLength;       // Text için üst sınır (CB 3.2: metin ≤ 40 karakter)

        public ParamDef(string name, ParamType type, bool required = true,
                        string bandKey = null, string[] enumValues = null, int maxLength = 0)
        {
            Name = name; Type = type; Required = required;
            BandKey = bandKey; EnumValues = enumValues; MaxLength = maxLength;
        }
    }

    /// <summary>Katalog aksiyonu — CB Spec 4 tablolarının bir satırı.</summary>
    public sealed class ActionDef
    {
        public readonly string ActionType;
        public readonly Tier Tier;
        public readonly Context Context;
        public readonly RateClass RateClass;
        public readonly ParamDef[] Params;

        public ActionDef(string actionType, Tier tier, Context context, RateClass rateClass, params ParamDef[] ps)
        { ActionType = actionType; Tier = tier; Context = context; RateClass = rateClass; Params = ps ?? new ParamDef[0]; }
    }

    /// <summary>IntentAction kataloğu v1 — CB Spec 4 (32 aksiyon). Katalog KODDA sabittir ve
    /// sürümlenir: istemci desteklenmeyen sürüm gönderirse `UnsupportedCatalogVersion` alır (3.2).
    /// Bant DEĞERLERİ balance dosyasındadır; burada yalnız anahtarları durur.</summary>
    public static class Catalog
    {
        /// <summary>Katalog sürümü. Aksiyon EKLEME minor, parametre/bant değişikliği major
        /// (BRIEF_FAZ04_ACILIS §4.3 önerisi; Atilla kararı sonrası bu satır kesinleşir).</summary>
        public const ushort Version = 1;

        static readonly string[] TribunEnum = { "kuzey", "guney", "dogu", "bati", "vip" };
        static readonly string[] UrunEnum = { "yiyecek", "icecek", "atistirmalik" };
        static readonly string[] MerchEnum = { "forma", "atki", "hatira" };
        static readonly string[] TeklifCevabi = { "kabul", "ret", "karsiTeklif" };
        static readonly string[] TonEnum = { "sakinlestir", "atesle", "uyar" };
        static readonly string[] RaporSebebi = { "hakaret", "hile", "spam", "diger" };
        static readonly string[] PaylasimHedefi = { "lig", "arkadas", "genel" };

        static readonly ActionDef[] All =
        {
            // --- CB 4.1 Tycoon (9) ---
            new ActionDef("tycoon.set_ticket_price", Tier.T1, Context.Hub, RateClass.Economic,
                new ParamDef("tribun", ParamType.Enum, true, null, TribunEnum),
                new ParamDef("fiyat", ParamType.Number, true, "tycoon.biletFiyat")),
            new ActionDef("tycoon.set_season_ticket_price", Tier.T1, Context.Hub, RateClass.Economic,
                new ParamDef("fiyat", ParamType.Number, true, "tycoon.kombineFiyat")),
            new ActionDef("tycoon.set_concession_price", Tier.T1, Context.Hub, RateClass.Economic,
                new ParamDef("urun", ParamType.Enum, true, null, UrunEnum),
                new ParamDef("fiyat", ParamType.Number, true, "tycoon.bufeFiyat")),
            new ActionDef("tycoon.set_merch_price", Tier.T1, Context.Hub, RateClass.Economic,
                new ParamDef("urun", ParamType.Enum, true, null, MerchEnum),
                new ParamDef("fiyat", ParamType.Number, true, "tycoon.magazaFiyat")),
            new ActionDef("tycoon.start_construction", Tier.T2, Context.Hub, RateClass.Economic,
                new ParamDef("tesisId", ParamType.Int, true, "tycoon.tesisId"),
                new ParamDef("hedefTier", ParamType.Int, true, "tycoon.tesisTier")),
            new ActionDef("tycoon.cancel_construction", Tier.T2, Context.Hub, RateClass.Economic,
                new ParamDef("insaatId", ParamType.Int, true, "tycoon.insaatId")),
            new ActionDef("tycoon.take_loan", Tier.T2, Context.Hub, RateClass.Economic,
                new ParamDef("miktar", ParamType.Number, true, "tycoon.krediMiktar"),
                new ParamDef("vadeAy", ParamType.Int, true, "tycoon.krediVadeAy")),
            new ActionDef("tycoon.repay_loan", Tier.T2, Context.Hub, RateClass.Economic,
                new ParamDef("krediId", ParamType.Int, true, "tycoon.krediId"),
                new ParamDef("miktar", ParamType.Number, true, "tycoon.krediOdeme")),
            new ActionDef("tycoon.sign_sponsor", Tier.T2, Context.Hub, RateClass.Economic,
                new ParamDef("teklifId", ParamType.Int, true, "tycoon.teklifId")),

            // --- CB 4.2 Kadro ve taktik (9) ---
            new ActionDef("squad.set_player_anchor", Tier.T0, Context.Hub | Context.Match, RateClass.Tactic,
                new ParamDef("oyuncuId", ParamType.Int, true, "squad.oyuncuId"),
                new ParamDef("x", ParamType.Int, true, "squad.anchorX"),
                new ParamDef("y", ParamType.Int, true, "squad.anchorY")),
            new ActionDef("squad.set_player_role", Tier.T0, Context.Hub | Context.Match, RateClass.Tactic,
                new ParamDef("oyuncuId", ParamType.Int, true, "squad.oyuncuId"),
                new ParamDef("rolId", ParamType.Int, true, "squad.rolId")),
            new ActionDef("squad.set_instruction", Tier.T0, Context.Hub | Context.Match, RateClass.Tactic,
                new ParamDef("oyuncuId", ParamType.Int, true, "squad.oyuncuId"),
                new ParamDef("talimatId", ParamType.Int, true, "squad.talimatId"),
                new ParamDef("deger", ParamType.Int, true, "squad.talimatDeger")),
            new ActionDef("squad.set_team_tactic", Tier.T0, Context.Hub | Context.Match, RateClass.Tactic,
                new ParamDef("mentalite", ParamType.Int, true, "squad.mentalite"),
                new ParamDef("tempo", ParamType.Int, true, "squad.tempo"),
                new ParamDef("pres", ParamType.Int, true, "squad.pres"),
                new ParamDef("hat", ParamType.Int, true, "squad.hat")),
            new ActionDef("squad.save_tactic_preset", Tier.T0, Context.Hub, RateClass.Tactic,
                new ParamDef("ad", ParamType.Text, true, null, null, 40),
                new ParamDef("slot", ParamType.Int, true, "squad.presetSlot")),
            new ActionDef("squad.set_captain", Tier.T0, Context.Hub, RateClass.Tactic,
                new ParamDef("oyuncuId", ParamType.Int, true, "squad.oyuncuId")),
            new ActionDef("squad.set_training_plan", Tier.T1, Context.Hub, RateClass.Tactic,
                new ParamDef("planId", ParamType.Int, true, "squad.planId"),
                new ParamDef("yogunluk", ParamType.Int, true, "squad.yogunluk")),
            new ActionDef("match.substitution", Tier.T1, Context.Match, RateClass.MatchCmd,
                new ParamDef("cikanId", ParamType.Int, true, "match.sahaSlot"),
                new ParamDef("girenId", ParamType.Int, true, "match.kulubeIndeks")),
            new ActionDef("match.motivation_talk", Tier.T0, Context.Match, RateClass.MatchCmd,
                new ParamDef("ton", ParamType.Enum, true, null, TonEnum)),

            // --- CB 4.3 Transfer ve personel (7) ---
            new ActionDef("transfer.list_player", Tier.T2, Context.Hub, RateClass.Economic,
                new ParamDef("oyuncuId", ParamType.Int, true, "squad.oyuncuId"),
                new ParamDef("istenenBedel", ParamType.Number, true, "transfer.bedel")),
            new ActionDef("transfer.propose_offer", Tier.T2, Context.Hub, RateClass.Economic,
                new ParamDef("hedefOyuncuId", ParamType.Int, true, "squad.oyuncuId"),
                new ParamDef("bedel", ParamType.Number, true, "transfer.bedel"),
                new ParamDef("maas", ParamType.Number, true, "transfer.maas")),
            new ActionDef("transfer.respond_offer", Tier.T2, Context.Hub, RateClass.Economic,
                new ParamDef("teklifId", ParamType.Int, true, "transfer.teklifId"),
                new ParamDef("cevap", ParamType.Enum, true, null, TeklifCevabi),
                new ParamDef("karsiBedel", ParamType.Number, false, "transfer.bedel")),
            new ActionDef("transfer.sign_free_agent", Tier.T2, Context.Hub, RateClass.Economic,
                new ParamDef("oyuncuId", ParamType.Int, true, "squad.oyuncuId"),
                new ParamDef("maas", ParamType.Number, true, "transfer.maas"),
                new ParamDef("sureYil", ParamType.Int, true, "transfer.sozlesmeYil")),
            new ActionDef("transfer.release_player", Tier.T2, Context.Hub, RateClass.Economic,
                new ParamDef("oyuncuId", ParamType.Int, true, "squad.oyuncuId")),
            new ActionDef("staff.hire", Tier.T2, Context.Hub, RateClass.Economic,
                new ParamDef("tip", ParamType.Int, true, "staff.tip"),
                new ParamDef("tier", ParamType.Int, true, "staff.tier"),
                new ParamDef("sureYil", ParamType.Int, true, "staff.sureYil")),
            new ActionDef("staff.activate_premium", Tier.T1, Context.Hub, RateClass.Economic,
                new ParamDef("envanterId", ParamType.Int, true, "staff.envanterId")),

            // --- CB 4.4 İletişim ve online (7) ---
            new ActionDef("social.arrange_talk", Tier.T0, Context.Hub, RateClass.Tactic,
                new ParamDef("personaId", ParamType.Int, true, "social.personaId"),
                new ParamDef("ton", ParamType.Enum, true, null, TonEnum)),
            new ActionDef("social.press_response", Tier.T0, Context.Hub, RateClass.Tactic,
                new ParamDef("soruId", ParamType.Int, true, "social.soruId"),
                new ParamDef("cevapSinifi", ParamType.Int, true, "social.cevapSinifi")),
            new ActionDef("league.create", Tier.T2, Context.Online, RateClass.OnlineSocial,
                new ParamDef("chaos", ParamType.Int, true, "league.chaos"),
                new ParamDef("hiz", ParamType.Int, true, "league.hiz"),
                new ParamDef("butce", ParamType.Number, true, "league.butce"),
                new ParamDef("saatDilimi", ParamType.Int, true, "league.saatDilimi")),
            new ActionDef("league.join", Tier.T1, Context.Online, RateClass.OnlineSocial,
                new ParamDef("ligId", ParamType.Int, true, "league.ligId"),
                new ParamDef("sifre", ParamType.Text, false, null, null, 40)),
            new ActionDef("league.set_rules", Tier.T2, Context.Online, RateClass.OnlineSocial,
                new ParamDef("ligId", ParamType.Int, true, "league.ligId"),
                new ParamDef("chaos", ParamType.Int, false, "league.chaos"),
                new ParamDef("hiz", ParamType.Int, false, "league.hiz")),
            new ActionDef("replay.share_clip", Tier.T1, Context.Online, RateClass.OnlineSocial,
                new ParamDef("macId", ParamType.Int, true, "replay.macId"),
                new ParamDef("pencereSn", ParamType.Int, true, "replay.pencereSn"),
                new ParamDef("hedef", ParamType.Enum, true, null, PaylasimHedefi)),
            new ActionDef("social.report_player", Tier.T1, Context.Online, RateClass.OnlineSocial,
                new ParamDef("hedefUserId", ParamType.Int, true, "social.userId"),
                new ParamDef("sebep", ParamType.Enum, true, null, RaporSebebi),
                new ParamDef("notlar", ParamType.Text, false, null, null, 40)),
        };

        static readonly Dictionary<string, ActionDef> Map = Build();

        static Dictionary<string, ActionDef> Build()
        {
            var d = new Dictionary<string, ActionDef>(StringComparer.Ordinal);
            for (int i = 0; i < All.Length; i++) d.Add(All[i].ActionType, All[i]);
            return d;
        }

        public static int Count => All.Length;
        public static IReadOnlyList<ActionDef> Actions => All;

        /// <summary>Aksiyonu bulur. Sürüm denetimi AYRI adımdır (kapı 1'de sırayla yapılır).</summary>
        public static ActionDef Find(string actionType)
            => actionType != null && Map.TryGetValue(actionType, out var a) ? a : null;

        public static bool SupportsVersion(ushort version) => version == Version;
    }
}
