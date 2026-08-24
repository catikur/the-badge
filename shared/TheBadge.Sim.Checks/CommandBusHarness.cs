using System;
using System.Collections.Generic;
using TheBadge.CommandBus;
using TheBadge.Sim.Commands;

namespace TheBadge.Checks
{
    /// <summary>Test payload görünümü — host'un JSON ayrıştırıcısının yerini tutar (çekirdek
    /// JSON parse etmez; CB K1 tasarım notu). Alan sırası ekleme sırasıdır: fazladan alan
    /// tespiti ve red determinizmi bu sırayla denetlenir.</summary>
    public sealed class TestPayload : IPayloadView
    {
        readonly List<string> names = new List<string>();
        readonly Dictionary<string, object> vals = new Dictionary<string, object>(StringComparer.Ordinal);

        public TestPayload Set(string name, object v)
        {
            if (!vals.ContainsKey(name)) names.Add(name);
            vals[name] = v; return this;
        }
        public TestPayload Remove(string name) { names.Remove(name); vals.Remove(name); return this; }
        public TestPayload Copy()
        {
            var p = new TestPayload();
            for (int i = 0; i < names.Count; i++) p.Set(names[i], vals[names[i]]);
            return p;
        }

        public IReadOnlyList<string> FieldNames => names;

        public bool TryGetNumber(string name, out double value)
        {
            value = 0;
            if (!vals.TryGetValue(name, out var o)) return false;
            switch (o) { case double d: value = d; return true; case long l: value = l; return true;
                         case int i: value = i; return true; default: return false; }
        }
        public bool TryGetInt(string name, out long value)
        {
            value = 0;
            if (!vals.TryGetValue(name, out var o)) return false;
            switch (o) { case long l: value = l; return true; case int i: value = i; return true;
                         // JSON'da 3.0 tamsayıdır; 3.5 DEĞİLDİR — tip kapısı bunu ayırır
                         case double d when d == Math.Floor(d): value = (long)d; return true;
                         default: return false; }
        }
        public bool TryGetText(string name, out string value)
        {
            value = null;
            if (!vals.TryGetValue(name, out var o)) return false;
            if (o is string s) { value = s; return true; }
            return false;
        }
        public bool TryGetBool(string name, out bool value)
        {
            value = false;
            if (!vals.TryGetValue(name, out var o)) return false;
            if (o is bool b) { value = b; return true; }
            return false;
        }
    }

    /// <summary>Bant sağlayıcı — `balance/command.bands.json`'dan doldurulur (host işi).</summary>
    public sealed class TestBands : IBandProvider
    {
        readonly Dictionary<string, (double min, double max)> b = new Dictionary<string, (double, double)>(StringComparer.Ordinal);
        public void Add(string key, double min, double max) => b[key] = (min, max);
        public bool Has(string key) => b.ContainsKey(key);
        public bool TryGetBand(string bandKey, out double min, out double max)
        {
            min = max = 0;
            if (bandKey == null || !b.TryGetValue(bandKey, out var v)) return false;
            min = v.min; max = v.max; return true;
        }
    }

    /// <summary>Kapı 3 sahtesi — K2-K5 modülleri gerçeğini getirene kadar denetlenebilir davranış.</summary>
    public sealed class TestContext : IValidationContext
    {
        public Context Active = Context.Hub | Context.Match | Context.Online;
        public RejectionReason Next = RejectionReason.None;   // sıradaki komut için zorlanan sonuç
        public int Calls;
        /// <summary>Host'un ürettiği kararlı takım kimliği. Testte "maç + taraf" ile modellenir:
        /// aynı takımı yöneten farklı kullanıcılar AYNI anahtarı alır.</summary>
        public long TeamKey = 1000;
        public bool IsContextActive(Context context) => (Active & context) != 0;
        public RejectionReason CheckOwnershipAndState(CommandEnvelope env, ActionDef action, IPayloadView payload)
        { Calls++; return Next; }
        public long ResolveTeamKey(CommandEnvelope env) => TeamKey + env.TeamIdx;
    }

    /// <summary>Yürütücü sahtesi — kaç kez yürütüldüğünü sayar (idempotency kanıtı).</summary>
    public sealed class TestExecutor : ICommandExecutor
    {
        int executions;
        public int Executions => System.Threading.Volatile.Read(ref executions);
        public RejectionReason Result = RejectionReason.None;
        public RejectionReason Execute(CommandEnvelope env, ActionDef action, IPayloadView payload,
                                       AuditRecord auditRecord, out string detail)
        { System.Threading.Interlocked.Increment(ref executions); detail = null; return Result; }
    }

    /// <summary>Denetim kaydının YÜRÜTME transaction'ının içinde geldiğini kanıtlar (CB 5.2).</summary>
    public sealed class AuditCapturingExecutor : ICommandExecutor
    {
        public bool Gordu;
        public AuditRecord Kayit;
        public RejectionReason Execute(CommandEnvelope env, ActionDef action, IPayloadView payload,
                                       AuditRecord auditRecord, out string detail)
        { Gordu = true; Kayit = auditRecord; detail = null; return RejectionReason.None; }
    }
}
