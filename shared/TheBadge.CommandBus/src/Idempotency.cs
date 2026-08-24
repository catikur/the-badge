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

    /// <summary>Rezervasyon sahiplik jetonu. `Complete`/`Release` YALNIZ jetonu eşleşen çağrıdan
    /// kabul edilir — gecikmiş bir çağrının başkasının rezervasyonunu kapatması/silmesi
    /// (inceleme bulgusu) böylece imkansızdır. Varsayılan (0) geçersizdir.</summary>
    public readonly struct ReservationToken
    {
        public readonly ulong Value;
        public ReservationToken(ulong v) { Value = v; }
        public bool IsValid => Value != 0;
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
        readonly Dictionary<Guid, (long at, bool done, ulong tok, CommandOutcome outcome)> kayit
            = new Dictionary<Guid, (long, bool, ulong, CommandOutcome)>();
        readonly long pencereMs;
        ulong sonrakiJeton = 1;

        public IdempotencyStore(long pencereMs = 24L * 60 * 60 * 1000)
        { this.pencereMs = pencereMs; }

        public int Count { get { lock (kilit) return kayit.Count; } }

        /// <summary>ATOMİK rezervasyon: ya sahiplik alınır, ya önceki yanıt döner, ya "sürüyor".
        ///
        /// UÇUŞ SÜRESİ DEVRALMASI YOKTUR (inceleme düzeltmesi): önceki sürümde bir rezervasyon
        /// belli süre sonra "çökmüş sayılıp" devralınıyordu; ama ilk çağrı hâlâ `Execute` içindeyse
        /// İKİ yürütme birden durum değiştirebiliyordu — yani exactly-once iddiası, tam da onu
        /// korumak için yazılmış kolda deliniyordu. Canlılık uğruna GÜVENLİK feda edilmez:
        /// asılı kalan rezervasyon `Prune` ile (operatör denetiminde) temizlenir, o ana kadar
        /// retry'ler `DuplicateCommand` alır — istemci için güvenli, durum için bozulmasız.</summary>
        public ReserveResult TryReserve(Guid commandId, long nowUnixMs,
                                        out CommandOutcome onceki, out ReservationToken token)
        {
            onceki = default; token = default;
            lock (kilit)
            {
                if (kayit.TryGetValue(commandId, out var k))
                {
                    if (k.done)
                    {
                        if (nowUnixMs - k.at < pencereMs) { onceki = k.outcome.AsReplay(); return ReserveResult.Completed; }
                        kayit.Remove(commandId);            // pencere doldu, yeniden yürütülebilir
                    }
                    else
                    {
                        return ReserveResult.InFlight;      // başka çağrı yürütüyor — DEVRALMA YOK
                    }
                }
                ulong t = sonrakiJeton++;
                kayit[commandId] = (nowUnixMs, false, t, default);
                token = new ReservationToken(t);
                return ReserveResult.Reserved;
            }
        }

        /// <summary>Rezervasyonu sonuçla kapatır. Jeton eşleşmezse HİÇBİR ŞEY yapmaz —
        /// gecikmiş bir çağrı başkasının sonucunu ezemez.</summary>
        public bool Complete(Guid commandId, ReservationToken token, long nowUnixMs, CommandOutcome outcome)
        {
            lock (kilit)
            {
                if (!token.IsValid) return false;
                if (!kayit.TryGetValue(commandId, out var k) || k.done || k.tok != token.Value) return false;
                kayit[commandId] = (nowUnixMs, true, k.tok, outcome);
                return true;
            }
        }

        /// <summary>Rezervasyonu geri alır (yürütme sırasında istisna). Jeton eşleşmezse
        /// hiçbir şey yapmaz — gecikmiş bir çağrı başkasının rezervasyonunu silemez.</summary>
        public bool Release(Guid commandId, ReservationToken token)
        {
            lock (kilit)
            {
                if (!token.IsValid) return false;
                if (!kayit.TryGetValue(commandId, out var k) || k.done || k.tok != token.Value) return false;
                kayit.Remove(commandId);
                return true;
            }
        }

        /// <summary>Pencere dışına düşen TAMAMLANMIŞ kayıtları atar. `asiliRezervasyonMs` verilirse
        /// o yaştan eski ASILI rezervasyonlar da temizlenir — bu, çökmüş bir işleyicinin bıraktığı
        /// kilidi açmanın TEK yoludur ve operatör denetimindedir (otomatik devralma yok).</summary>
        public int Prune(long nowUnixMs, long asiliRezervasyonMs = 0)
        {
            lock (kilit)
            {
                var silinecek = new List<Guid>();
                foreach (var kv in kayit)
                {
                    long yas = nowUnixMs - kv.Value.at;
                    if (kv.Value.done) { if (yas >= pencereMs) silinecek.Add(kv.Key); }
                    else if (asiliRezervasyonMs > 0 && yas >= asiliRezervasyonMs) silinecek.Add(kv.Key);
                }
                for (int i = 0; i < silinecek.Count; i++) kayit.Remove(silinecek[i]);
                return silinecek.Count;
            }
        }
    }
}
