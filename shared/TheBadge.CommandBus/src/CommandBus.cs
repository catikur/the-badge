using System;
using TheBadge.Sim.Commands;

namespace TheBadge.CommandBus
{
    /// <summary>Yürütücü — PASS sonrası durum geçişini yapan taraf (CB 5.2: yürütme TEK
    /// transaction'dır; durum geçişi + event + audit ya birlikte kalıcı olur ya hiç olmaz).
    /// Uygulamaları modüllerle gelir (K3 tycoon, K4 squad, K5 transfer).</summary>
    public interface ICommandExecutor
    {
        /// <summary>Aksiyonu yürütür. Yürütme sırasında ortaya çıkan durum çakışmaları
        /// `StateConflict` ile döner — kapı 3 ile yürütme arasında durum değişmiş olabilir.</summary>
        RejectionReason Execute(CommandEnvelope env, ActionDef action, IPayloadView payload, out string detail);
    }

    /// <summary>Denetim kaydı — CB 7.4 izlenebilirlik. Her komut (kabul VE red) buraya düşer.</summary>
    public interface IAuditSink
    {
        void Record(CommandEnvelope env, CommandOutcome outcome, bool abuseFlag);
    }

    /// <summary>TEK KAPI'nın hub ucu — CLAUDE.md değişmez #1. Oyun durumunu değiştiren her eylem
    /// buradan geçer: UI, LLM, otomasyon — istisnasız. Sıra CB Spec 2.1 komut yaşam döngüsüdür:
    /// idempotency → 4 kapı → yürütme → audit.
    ///
    /// TASARIM NOTU: idempotency doğrulamadan ÖNCEdir. Aynı CommandId ikinci kez geldiğinde
    /// komut yeniden DOĞRULANMAZ da yürütülmez de — spec "önceki yanıt aynen döner" der (8.1);
    /// yeniden doğrulamak, aradaki durum değişimi yüzünden aynı komuta farklı yanıt üretebilirdi
    /// ve retry'yi güvensiz kılardı.</summary>
    public sealed class CommandBus
    {
        readonly IBandProvider bands;
        readonly IValidationContext ctx;
        readonly IRateLimiter rate;
        readonly IdempotencyStore idem;
        readonly IAuditSink audit;

        public CommandBus(IBandProvider bands, IValidationContext ctx, IRateLimiter rate,
                          IdempotencyStore idem, IAuditSink audit = null)
        {
            this.bands = bands ?? throw new ArgumentNullException(nameof(bands));
            this.ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
            this.rate = rate;
            this.idem = idem ?? throw new ArgumentNullException(nameof(idem));
            this.audit = audit;
        }

        public CommandOutcome Submit(CommandEnvelope env, IPayloadView payload, ICommandExecutor executor)
        {
            if (env == null) return new CommandOutcome(RejectionReason.SchemaViolation, "zarf yok");
            long now = env.IssuedAtUnixMs;

            // 1) Idempotency (CB 8.1)
            if (idem.TryGet(env.CommandId, now, out var onceki))
            {
                audit?.Record(env, onceki, false);
                return onceki;
            }

            // 2) Dört kapı (CB 5)
            var action = Catalog.Find(env.ActionType);
            var v = Validator.Validate(env, action, payload, bands, ctx, rate);
            if (!v.Ok)
            {
                var red = new CommandOutcome(v.Reason, v.Detail);
                idem.Store(env.CommandId, now, red);
                bool abuse = v.Reason == RejectionReason.RateLimited
                             && rate != null && rate.ConsumeAbuseFlag(env.UserId, now);
                audit?.Record(env, red, abuse);
                return red;
            }

            // 3) Yürütme (CB 5.2)
            RejectionReason yr = executor == null
                ? RejectionReason.None
                : executor.Execute(env, action, payload, out _);
            string detay = null;
            if (executor != null && yr != RejectionReason.None) detay = env.ActionType;
            var sonuc = new CommandOutcome(yr, detay);
            idem.Store(env.CommandId, now, sonuc);
            audit?.Record(env, sonuc, false);
            return sonuc;
        }

        /// <summary>Onay katmanı — CB Spec 6. Tier KATALOGDAN gelir, KAYNAKTAN DEĞİL:
        /// LLM kaynaklı komut tier'ını asla düşüremez. Sunum katmanı bunu okur.</summary>
        public static Tier RequiredTier(string actionType)
        {
            var a = Catalog.Find(actionType);
            return a == null ? Tier.T2 : a.Tier;   // bilinmeyen aksiyon en yüksek onayı ister
        }
    }
}
