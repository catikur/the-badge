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

    /// <summary>Rezervasyon sonucu — CB 8.1'in "exactly-once etkisi" iddiasının taşıyıcısı.</summary>
    public enum ReserveResult : byte
    {
        Reserved = 0,   // bu çağrı sahibi: yürütmeye devam et, sonra Complete çağır
        Completed = 1,  // daha önce tamamlanmış: ÖNCEKİ yanıt döner
        InFlight = 2    // BAŞKA bir çağrı şu an yürütüyor: DuplicateCommand
    }

    /// <summary>Idempotency deposu — CB Spec 8.1. `CommandId` 24 saatlik dedup penceresinde
    /// tutulur; at-least-once istemci retry'si exactly-once etkisi verir.
    ///
    /// EŞZAMANLILIK (inceleme düzeltmesi): önce "bak, sonra sakla" deseni iki RPC işleyicisinin
    /// AYNI Id'yi birlikte yürütmesine izin veriyordu — iddia edilen exactly-once sağlanmıyordu.
    /// Artık REZERVASYON atomiktir (`TryReserve` → yürüt → `Complete`) ve tüm erişim kilitlidir;
    /// sunucu paralel istek işlerken sözleşme bozulmaz.
    ///
    /// ZAMAN: `nowUnixMs` HOST'un ALIŞ saatidir, zarfın `IssuedAtUnixMs` alanı DEĞİL — istemci
    /// saati güvenilmezdir (aynı incelemenin P1 bulgusu).</summary>
    public sealed class IdempotencyStore
    {
        readonly object kilit = new object();
        readonly Dictionary<Guid, (long at, bool done, CommandOutcome outcome)> kayit
            = new Dictionary<Guid, (long, bool, CommandOutcome)>();
        readonly long pencereMs;
        readonly long ucusSuresiMs;   // rezervasyon bu kadar sürede tamamlanmazsa düşer (çökme payı)

        public IdempotencyStore(long pencereMs = 24L * 60 * 60 * 1000, long ucusSuresiMs = 30_000)
        { this.pencereMs = pencereMs; this.ucusSuresiMs = ucusSuresiMs; }

        public int Count { get { lock (kilit) return kayit.Count; } }

        /// <summary>ATOMİK rezervasyon: ya sahiplik alınır, ya önceki yanıt döner, ya "sürüyor".</summary>
        public ReserveResult TryReserve(Guid commandId, long nowUnixMs, out CommandOutcome onceki)
        {
            onceki = default;
            lock (kilit)
            {
                if (kayit.TryGetValue(commandId, out var k))
                {
                    if (k.done)
                    {
                        if (nowUnixMs - k.at < pencereMs) { onceki = k.outcome.AsReplay(); return ReserveResult.Completed; }
                        kayit.Remove(commandId);            // pencere doldu, yeniden yürütülebilir
                    }
                    else if (nowUnixMs - k.at < ucusSuresiMs)
                    {
                        return ReserveResult.InFlight;      // başka çağrı yürütüyor
                    }
                    // uçuş süresi aşıldı: önceki çağrı çökmüş sayılır, rezervasyon devralınır
                }
                kayit[commandId] = (nowUnixMs, false, default);
                return ReserveResult.Reserved;
            }
        }

        /// <summary>Rezervasyonu sonuçla kapatır. Yalnız `Reserved` alan çağrı çağırmalıdır.</summary>
        public void Complete(Guid commandId, long nowUnixMs, CommandOutcome outcome)
        {
            lock (kilit) kayit[commandId] = (nowUnixMs, true, outcome);
        }

        /// <summary>Rezervasyonu geri alır (yürütme başlamadan iptal — ör. beklenmeyen istisna).</summary>
        public void Release(Guid commandId)
        {
            lock (kilit)
            {
                if (kayit.TryGetValue(commandId, out var k) && !k.done) kayit.Remove(commandId);
            }
        }

        /// <summary>Pencere dışına düşen kayıtları atar (çağrı sıklığı host'un işi).</summary>
        public int Prune(long nowUnixMs)
        {
            lock (kilit)
            {
                var silinecek = new List<Guid>();
                foreach (var kv in kayit)
                {
                    long yas = nowUnixMs - kv.Value.at;
                    if (kv.Value.done ? yas >= pencereMs : yas >= ucusSuresiMs) silinecek.Add(kv.Key);
                }
                for (int i = 0; i < silinecek.Count; i++) kayit.Remove(silinecek[i]);
                return silinecek.Count;
            }
        }
    }
}
