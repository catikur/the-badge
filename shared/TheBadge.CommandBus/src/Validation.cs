using System;
using System.Collections.Generic;
using TheBadge.Sim.Commands;

namespace TheBadge.CommandBus
{
    /// <summary>Payload görünümü — ÇEKİRDEK JSON PARSE ETMEZ (TheBadge.Sim'deki bağımlılıksızlık
    /// kuralının aynısı; ME 3.3 BalanceHash deseni). Host ham `PayloadJson`'ı kendi JSON aracıyla
    /// ayrıştırır ve bu görünümü verir; ŞEMA SIKILIĞI çekirdekte denetlenir — yani doğrulama
    /// mantığı sunucuda ve Unity'de BİREBİR aynı koddur.</summary>
    public interface IPayloadView
    {
        /// <summary>Payload'daki TÜM alan adları — fazladan alan tespiti için (CB 3.2 sıkı mod).</summary>
        IReadOnlyList<string> FieldNames { get; }
        bool TryGetNumber(string name, out double value);
        bool TryGetInt(string name, out long value);
        bool TryGetText(string name, out string value);
        bool TryGetBool(string name, out bool value);
    }

    /// <summary>Kapı 3 bağlamı — CB Spec 5 "bağlam, sahiplik, kaynak, hak". Uygulamaları
    /// K2-K5 modülleriyle gelir (dünya durumu, finans, transfer penceresi, değişiklik hakkı).
    /// Çekirdek yalnız ARAYÜZÜ tanımlar: bus modüllere bağımlı olmaz, modüller bus'a bağlanır.</summary>
    public interface IValidationContext
    {
        /// <summary>Komutun bağlamı (Hub/Maç/Online) şu an geçerli mi.</summary>
        bool IsContextActive(Context context);
        /// <summary>Sahiplik/kaynak/hak denetimi. Geçerse `RejectionReason.None` döner.</summary>
        RejectionReason CheckOwnershipAndState(CommandEnvelope env, ActionDef action, IPayloadView payload);

        /// <summary>KARARLI takım kimliği — CB 5.1 maç içi limiti "10/dk/TAKIM" der ve bu kimlik
        /// zarftan TÜRETİLEMEZ: `TeamIdx` yalnız ev/deplasman'dır, aynı takımı yöneten iki
        /// kullanıcıyı aynı kovaya sokmaz. Host (maç/lig bağlamını bilen taraf) üretir.</summary>
        long ResolveTeamKey(CommandEnvelope env);
    }

    /// <summary>Bant sağlayıcı — değerler `balance/command.bands.json`'dan gelir (CB 5:
    /// "tüm bant değerleri balance JSON'dan okunur"). Host doldurur; kodda magic number yok.</summary>
    public interface IBandProvider
    {
        /// <summary>Anahtar için [min, max] bandı. Anahtar YOKSA false → bu bir yapılandırma
        /// hatasıdır ve komut `ParamOutOfBand` ile reddedilir (sessiz geçiş YOK).</summary>
        bool TryGetBand(string bandKey, out double min, out double max);
    }

    /// <summary>Doğrulama sonucu. Red halinde sebep + hangi parametrede olduğu taşınır.</summary>
    public readonly struct ValidationResult
    {
        public readonly RejectionReason Reason;
        public readonly string Detail;      // parametre adı / bağlam ipucu — audit log'a girer
        public bool Ok => Reason == RejectionReason.None;
        public ValidationResult(RejectionReason reason, string detail = null) { Reason = reason; Detail = detail; }
        public static readonly ValidationResult Pass = new ValidationResult(RejectionReason.None);
    }

    /// <summary>4 KAPILI DOĞRULAMA ZİNCİRİ — CB Spec 5. Kapılar DETERMİNİSTİK sırayla çalışır;
    /// ilk başarısızlık zinciri durdurur (aynı zarf + aynı bağlam = aynı red sebebi).</summary>
    public static class Validator
    {
        public static ValidationResult Validate(CommandEnvelope env, ActionDef action,
            IPayloadView payload, IBandProvider bands, IValidationContext ctx, IRateLimiter rate,
            long receivedAtUnixMs)
        {
            if (env == null) return new ValidationResult(RejectionReason.SchemaViolation, "zarf yok");

            // ---- KAPI 1: katalog + şema ----
            // KAYNAK denetimi (CB 2.2): AUTO v1'de KAPALIDIR ve tanımsız enum değeri kabul edilmez.
            // İnceleme düzeltmesi: kaynak hiç doğrulanmıyordu, `Auto` zarfı UI komutu gibi
            // yürüyebiliyordu — kaynağa özgü tier/rate politikası yazılana dek reddedilir.
            if (env.Source != CommandSource.UI && env.Source != CommandSource.LLM)
                return new ValidationResult(RejectionReason.SchemaViolation, "kaynak: " + env.Source);
            if (!Catalog.SupportsVersion(env.CatalogVersion))
                return new ValidationResult(RejectionReason.UnsupportedCatalogVersion, env.CatalogVersion.ToString());
            if (action == null)
                return new ValidationResult(RejectionReason.UnknownAction, env.ActionType);
            if (payload == null)
                return new ValidationResult(RejectionReason.SchemaViolation, "payload yok");

            // Zorunlu alanlar + tipler
            for (int i = 0; i < action.Params.Length; i++)
            {
                var p = action.Params[i];
                bool varMi = HasField(payload, p.Name);
                if (!varMi)
                {
                    if (p.Required) return new ValidationResult(RejectionReason.SchemaViolation, p.Name);
                    continue;
                }
                if (!TypeOk(payload, p, out string tipHatasi))
                    return new ValidationResult(RejectionReason.SchemaViolation, tipHatasi);
            }
            // SIKI MOD (CB 3.2): fazladan alan = SchemaViolation
            var names = payload.FieldNames;
            for (int i = 0; i < names.Count; i++)
                if (!Tanimli(action, names[i]))
                    return new ValidationResult(RejectionReason.SchemaViolation, "fazladan alan: " + names[i]);

            // ---- KAPI 2: parametre bandı (balance) ----
            for (int i = 0; i < action.Params.Length; i++)
            {
                var p = action.Params[i];
                if (p.BandKey == null) continue;
                if (!HasField(payload, p.Name)) continue;       // isteğe bağlı, yok
                if (!payload.TryGetNumber(p.Name, out double v))
                    return new ValidationResult(RejectionReason.SchemaViolation, p.Name);
                // Bant anahtarı tanımsızsa SESSİZ GEÇMEZ: yapılandırma hatası da reddir
                if (!bands.TryGetBand(p.BandKey, out double min, out double max))
                    return new ValidationResult(RejectionReason.ParamOutOfBand, p.Name + " (bant tanımsız: " + p.BandKey + ")");
                if (v < min || v > max)
                    return new ValidationResult(RejectionReason.ParamOutOfBand, p.Name);
            }

            // ---- KAPI 3: bağlam, sahiplik, kaynak, hak ----
            if (ctx == null) return new ValidationResult(RejectionReason.StateConflict, "bağlam yok");
            // Zarfın SEÇTİĞİ bağlam ile aksiyonun izin verdiği bayrakların KESİŞİMİ.
            // İnceleme düzeltmesi: aktiflik denetimi aksiyonun BİRLEŞİK bayraklarıyla yapılıyordu,
            // yani hem hub hem maç geçerli bir aksiyon (ör. squad.set_player_role) maç damgasıyla
            // gelse bile "hub açık" olduğu için geçiyordu. Artık maç damgalı komut MAÇ bağlamının
            // açık olmasını ister.
            var etkin = action.Context & KomutBaglami(env);
            if (etkin == Context.None)
                return new ValidationResult(RejectionReason.StateConflict, "yanlış bağlam");
            if (!ctx.IsContextActive(etkin))
                return new ValidationResult(RejectionReason.StateConflict, "bağlam kapalı");
            var g3 = ctx.CheckOwnershipAndState(env, action, payload);
            if (g3 != RejectionReason.None) return new ValidationResult(g3);

            // ---- KAPI 4: rate limit ----
            // Saat HOST'undur: `IssuedAtUnixMs` istemci verisidir ve ileri tarihli gönderilerek
            // pencere sıfırlanabilirdi (inceleme düzeltmesi, P1). Takım kimliği de anahtara girer:
            // CB 5.1 maç içi limiti "10/dk/TAKIM" der (aynı takımı paylaşan kullanıcılar tek
            // sayaçta, farklı takımları yöneten kullanıcı ayrı sayaçlarda).
            long teamKey = action.RateClass == RateClass.MatchCmd ? ctx.ResolveTeamKey(env) : 0;
            if (rate != null && !rate.Allow(env.UserId, teamKey, action.RateClass, env.Source, receivedAtUnixMs))
                return new ValidationResult(RejectionReason.RateLimited, action.RateClass.ToString());

            return ValidationResult.Pass;
        }

        /// <summary>Zarfın kendi bağlamı: `MatchTick > 0` maç komutudur (CB 3.1: "0 = hub komutu").
        /// Online aksiyonlar hub bağlamında da geçerlidir — ayrımı kapı 3'ün `IsContextActive`'i yapar.</summary>
        static Context KomutBaglami(CommandEnvelope env)
            => env.MatchTick > 0 ? Context.Match : (Context.Hub | Context.Online);

        static bool Tanimli(ActionDef a, string field)
        {
            for (int i = 0; i < a.Params.Length; i++) if (string.Equals(a.Params[i].Name, field, StringComparison.Ordinal)) return true;
            return false;
        }

        static bool HasField(IPayloadView p, string name)
        {
            var n = p.FieldNames;
            for (int i = 0; i < n.Count; i++) if (string.Equals(n[i], name, StringComparison.Ordinal)) return true;
            return false;
        }

        static bool TypeOk(IPayloadView p, ParamDef def, out string hata)
        {
            hata = def.Name;
            switch (def.Type)
            {
                case ParamType.Int:
                    return p.TryGetInt(def.Name, out _);
                case ParamType.Number:
                    return p.TryGetNumber(def.Name, out _);
                case ParamType.Bool:
                    return p.TryGetBool(def.Name, out _);
                case ParamType.Text:
                    {
                        if (!p.TryGetText(def.Name, out string s) || s == null) return false;
                        if (def.MaxLength > 0 && s.Length > def.MaxLength) { hata = def.Name + " (uzunluk)"; return false; }
                        // CB 3.2: kontrol karakterlerinden arındırılmış olmalı
                        for (int i = 0; i < s.Length; i++)
                            if (s[i] < 0x20 || s[i] == 0x7F) { hata = def.Name + " (kontrol karakteri)"; return false; }
                        return true;
                    }
                case ParamType.Enum:
                    {
                        if (!p.TryGetText(def.Name, out string s) || s == null) return false;
                        var vals = def.EnumValues;
                        if (vals == null) return false;
                        for (int i = 0; i < vals.Length; i++) if (string.Equals(vals[i], s, StringComparison.Ordinal)) return true;
                        hata = def.Name + " (enum dışı)";
                        return false;
                    }
                default: return false;
            }
        }
    }
}
