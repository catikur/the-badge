using System;
using System.Collections.Generic;
using TheBadge.Sim.Commands;

namespace TheBadge.CommandBus
{
    /// <summary>Komut sonucu — idempotency deposunda saklanan yanıt (CB 8.1: aynı Id ikinci kez
    /// gelirse komut YENİDEN YÜRÜTÜLMEZ, ÖNCEKİ YANIT aynen döner).</summary>
    public readonly struct CommandOutcome
    {
        public readonly RejectionReason Reason;   // None = kabul edildi ve yürütüldü
        public readonly string Detail;
        public readonly bool Replayed;            // bu yanıt depodan mı geldi
        public bool Ok => Reason == RejectionReason.None;
        public CommandOutcome(RejectionReason reason, string detail, bool replayed = false)
        { Reason = reason; Detail = detail; Replayed = replayed; }
        public CommandOutcome AsReplay() => new CommandOutcome(Reason, Detail, true);
    }

    /// <summary>Idempotency deposu — CB Spec 8.1. `CommandId` 24 saatlik dedup penceresinde
    /// tutulur; at-least-once istemci retry'si exactly-once etkisi verir. Zaman DIŞARIDAN
    /// verilir (`DateTime.Now` yok) — test edilebilirlik ve determinizm.</summary>
    public sealed class IdempotencyStore
    {
        readonly Dictionary<Guid, (long at, CommandOutcome outcome)> kayit
            = new Dictionary<Guid, (long, CommandOutcome)>();
        readonly long pencereMs;

        public IdempotencyStore(long pencereMs = 24L * 60 * 60 * 1000) { this.pencereMs = pencereMs; }

        public int Count => kayit.Count;

        /// <summary>Daha önce görüldüyse önceki yanıtı döner (Replayed = true).</summary>
        public bool TryGet(Guid commandId, long nowUnixMs, out CommandOutcome onceki)
        {
            onceki = default;
            if (!kayit.TryGetValue(commandId, out var k)) return false;
            if (nowUnixMs - k.at >= pencereMs) { kayit.Remove(commandId); return false; }
            onceki = k.outcome.AsReplay();
            return true;
        }

        public void Store(Guid commandId, long nowUnixMs, CommandOutcome outcome)
            => kayit[commandId] = (nowUnixMs, outcome);

        /// <summary>Pencere dışına düşen kayıtları atar (çağrı sıklığı host'un işi).</summary>
        public int Prune(long nowUnixMs)
        {
            var silinecek = new List<Guid>();
            foreach (var kv in kayit) if (nowUnixMs - kv.Value.at >= pencereMs) silinecek.Add(kv.Key);
            for (int i = 0; i < silinecek.Count; i++) kayit.Remove(silinecek[i]);
            return silinecek.Count;
        }
    }
}
