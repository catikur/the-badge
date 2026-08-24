using System;
using TheBadge.Sim.Commands;

namespace TheBadge.CommandBus
{
    /// <summary>Denetim kaydı — CB 7.4 izlenebilirlik. Kabul edilen komutun kaydı YÜRÜTÜCÜYE
    /// verilir (aşağıdaki `Execute` imzası) ki durum geçişiyle AYNI transaction'da kalıcı olsun
    /// (CB 5.2: durum + event + audit ya birlikte kalıcı olur ya hiç olmaz). Bu arayüz yalnız
    /// durum DEĞİŞTİRMEYEN sonuçlar (redler, tekrar oynatılan yanıtlar) içindir.</summary>
    public interface IAuditSink
    {
        void Record(CommandEnvelope env, CommandOutcome outcome, bool abuseFlag, long receivedAtUnixMs);
    }

    /// <summary>Yürütücü — PASS sonrası durum geçişini yapan taraf (CB 5.2). Yürütme TEK
    /// transaction'dır ve **denetim kaydı bu transaction'ın İÇİNDEdir**: `auditRecord` durum ve
    /// event'lerle birlikte kalıcı olur. İnceleme düzeltmesi — önce audit yürütmeden SONRA ayrı
    /// çağrılıyordu, yani audit yazımı başarısız olursa durum audit'siz kalıyordu ve "hep ya da
    /// hiç" sözleşmesi delinmiş oluyordu. Uygulamaları modüllerle gelir (K3-K5).</summary>
    public interface ICommandExecutor
    {
        RejectionReason Execute(CommandEnvelope env, ActionDef action, IPayloadView payload,
                                AuditRecord auditRecord, out string detail);
    }

    /// <summary>Yürütmeyle birlikte kalıcı olacak denetim kaydı (CB 7.4).</summary>
    public readonly struct AuditRecord
    {
        public readonly Guid CommandId;
        public readonly string ActionType;
        public readonly CommandSource Source;
        public readonly long UserId;
        public readonly byte TeamIdx;
        public readonly long ReceivedAtUnixMs;   // HOST saati — istemcinin IssuedAtUnixMs'i değil
        public readonly Guid? SuggestionId;      // LLM kaynaklıysa öneri izi (CB 7.4)
        public AuditRecord(CommandEnvelope e, long receivedAtUnixMs)
        {
            CommandId = e.CommandId; ActionType = e.ActionType; Source = e.Source;
            UserId = e.UserId; TeamIdx = e.TeamIdx; ReceivedAtUnixMs = receivedAtUnixMs;
            SuggestionId = e.SuggestionId;
        }
    }

    /// <summary>TEK KAPI'nın hub ucu — CLAUDE.md değişmez #1. Oyun durumunu değiştiren her eylem
    /// buradan geçer: UI, LLM, otomasyon — istisnasız. Sıra CB 2.1'dir: rezervasyon → 4 kapı →
    /// yürütme (audit dahil) → sonuç.
    ///
    /// ZAMAN VE KİMLİK SÖZLEŞMESİ: rate limit ve idempotency penceresi HOST'un alış saatiyle
    /// (`receivedAtUnixMs`), kota kimliği de HOST'un oturumundan (`authenticatedUserId`) gelir.
    /// Zarfın `IssuedAtUnixMs` ve `UserId` alanları İSTEMCİ verisidir ve yalnız METADATA'dır —
    /// güvenilseydi, her partiyi ileri tarihli ya da yeni kimlikli göndererek rate limit sayaçları
    /// sıfırlanabilirdi. İkisi de ZORUNLU parametredir: host'un unutabileceği bir varsayılan
    /// bırakılmaz (inceleme düzeltmeleri P1 + güvenlik turu, 2026-08-24).
    ///
    /// IDEMPOTENCY doğrulamadan ÖNCEdir: yeniden doğrulamak, aradaki durum değişimi yüzünden
    /// aynı komuta farklı yanıt üretebilir ve retry'yi güvensiz kılardı (CB 8.1).</summary>
    public sealed class CommandBus
    {
        readonly IBandProvider bands;
        readonly IValidationContext ctx;
        readonly IRateLimiter rate;
        readonly IdempotencyStore idem;
        readonly IAuditSink audit;
        readonly long budamaAraligiMs;
        readonly object budamaKilidi = new object();
        long sonBudamaMs = long.MinValue;

        public CommandBus(IBandProvider bands, IValidationContext ctx, IRateLimiter rate,
                          IdempotencyStore idem, IAuditSink audit = null,
                          long budamaAraligiMs = 5L * 60 * 1000)
        {
            this.bands = bands ?? throw new ArgumentNullException(nameof(bands));
            this.ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
            this.rate = rate;
            this.idem = idem ?? throw new ArgumentNullException(nameof(idem));
            this.audit = audit;
            this.budamaAraligiMs = budamaAraligiMs;
        }

        /// <summary>AMORTİ EDİLMİŞ BUDAMA — güvenlik incelemesi bulgusu (2026-08-24): bus
        /// idempotency deposunu hiç budamıyordu, yani süresi dolmuş kayıtlar yalnız aynı Id
        /// tekrar geldiğinde temizleniyordu; benzersiz Id akışında depo süresiz büyürdü.
        /// Budama ASILI rezervasyonlara DOKUNMAZ (`asiliRezervasyonMs` verilmez) — devralma
        /// yasağı operatör denetiminde kalır.</summary>
        void BelkiBuda(long nowUnixMs)
        {
            lock (budamaKilidi)
            {
                if (sonBudamaMs != long.MinValue && nowUnixMs - sonBudamaMs < budamaAraligiMs) return;
                sonBudamaMs = nowUnixMs;
            }
            idem.Prune(nowUnixMs);
        }

        /// <summary>Komutu işler. `receivedAtUnixMs` HOST saatidir (istemci saati DEĞİL).
        /// `executor` ZORUNLUDUR: yürütücüsüz çağrı, durum değişmediği hâlde "başarılı" sonuç
        /// üretip idempotency deposuna yazardı ve gerçek yürütücüyle yapılan retry bu SAHTE
        /// başarıyı tekrar oynatırdı (inceleme düzeltmesi, P1) — bu bir kablolama hatasıdır,
        /// sessizce yutulmaz. Yalnız doğrulama için `Validate` kullanın.</summary>
        public CommandOutcome Submit(CommandEnvelope env, IPayloadView payload,
                                     ICommandExecutor executor, long receivedAtUnixMs,
                                     long authenticatedUserId)
        {
            if (env == null) return new CommandOutcome(RejectionReason.SchemaViolation, "zarf yok");
            if (executor == null) throw new ArgumentNullException(nameof(executor),
                "Yürütücüsüz Submit durum değiştirmez ama başarı raporlar; yalnız doğrulama için Validate kullanın.");

            BelkiBuda(receivedAtUnixMs);

            // 1) ATOMİK rezervasyon (CB 8.1) — eşzamanlı iki çağrı aynı Id'yi yürütemez
            var rez = idem.TryReserve(env.CommandId, receivedAtUnixMs, out var onceki, out var jeton);
            if (rez == ReserveResult.Completed)
            {
                audit?.Record(env, onceki, false, receivedAtUnixMs);
                return onceki;
            }
            if (rez == ReserveResult.InFlight)
            {
                var mesgul = new CommandOutcome(RejectionReason.DuplicateCommand, "işlem sürüyor");
                audit?.Record(env, mesgul, false, receivedAtUnixMs);
                return mesgul;   // rezervasyon BİZİM değil → depoya yazmayız
            }

            try
            {
                // 2) Dört kapı (CB 5)
                var action = Catalog.Find(env.ActionType);
                var v = Validator.Validate(env, action, payload, bands, ctx, rate, receivedAtUnixMs, authenticatedUserId);
                if (!v.Ok)
                {
                    var red = new CommandOutcome(v.Reason, v.Detail);
                    // yurutuldu: FALSE — doğrulamada düşen komut KISA dedup penceresine yazılır
                    idem.Complete(env.CommandId, jeton, receivedAtUnixMs, red, yurutuldu: false);
                    bool abuse = v.Reason == RejectionReason.RateLimited
                                 && rate != null && rate.ConsumeAbuseFlag(authenticatedUserId, receivedAtUnixMs);
                    audit?.Record(env, red, abuse, receivedAtUnixMs);   // red durum değiştirmez
                    return red;
                }

                // 3) Yürütme — denetim kaydı YÜRÜTME TRANSACTION'ININ İÇİNDE (CB 5.2)
                var yr = executor.Execute(env, action, payload, new AuditRecord(env, receivedAtUnixMs), out string detay);
                var sonuc = new CommandOutcome(yr, yr == RejectionReason.None ? null : (detay ?? env.ActionType));
                idem.Complete(env.CommandId, jeton, receivedAtUnixMs, sonuc);
                return sonuc;
            }
            catch
            {
                idem.Release(env.CommandId, jeton);   // yalnız KENDİ rezervasyonumuzu bırakırız
                throw;
            }
        }

        /// <summary>Durum DEĞİŞTİRMEDEN yalnız doğrulama (istemci ön-doğrulaması, öneri kartı
        /// önizlemesi). Rate limit sayacını TÜKETMEZ — ön-doğrulama kullanıcının hakkını yemez.</summary>
        public ValidationResult Validate(CommandEnvelope env, IPayloadView payload, long receivedAtUnixMs,
                                         long authenticatedUserId)
            => Validator.Validate(env, Catalog.Find(env?.ActionType), payload, bands, ctx, null,
                                  receivedAtUnixMs, authenticatedUserId);

        /// <summary>Onay katmanı — CB Spec 6. Tier KATALOGDAN gelir, KAYNAKTAN DEĞİL:
        /// LLM kaynaklı komut tier'ını asla düşüremez. Sunum katmanı bunu okur.</summary>
        public static Tier RequiredTier(string actionType)
        {
            var a = Catalog.Find(actionType);
            return a == null ? Tier.T2 : a.Tier;   // bilinmeyen aksiyon en yüksek onayı ister
        }
    }
}
