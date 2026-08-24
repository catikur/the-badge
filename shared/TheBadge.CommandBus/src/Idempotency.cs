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
        /// <summary>Anahtar (KULLANICI, CommandId) — yalnız CommandId DEĞİL. `CommandId` istemcinin
        /// ürettiği bir Guid'dir; tek başına anahtar olduğunda kayıt SÜREÇ GENELİNDE paylaşılır ve
        /// başka bir oturum aynı Id ile gelirse ötekinin ÖNBELLEKLİ YANITINI alır — kendi komutu
        /// sessizce hiç çalışmadan "başarılı" görünür (inceleme bulgusu, 2026-08-24). Kimlik
        /// anahtara girince bu çakışma yapısal olarak imkânsızdır; uçuşta olan başka bir oturumun
        /// durumu da sorgulanamaz.</summary>
        readonly Dictionary<(long user, Guid id), (long at, bool done, ulong tok, long pencere, CommandOutcome outcome)> kayit
            = new Dictionary<(long, Guid), (long, bool, ulong, long, CommandOutcome)>();
        readonly long pencereMs;      // YÜRÜTÜLEN komutlar için (CB 8.1: 24 saat)
        readonly long redPencereMs;   // yürütmeye HİÇ ulaşmamış redler için (aşağıdaki not)
        ulong sonrakiJeton = 1;

        /// <summary>`redPencereMs` — güvenlik incelemesi bulgusu (2026-08-24). Rezervasyon
        /// doğrulamadan ÖNCE alınır (bu bilinçli: retry'yi yeniden doğrulamak aradaki durum
        /// değişimi yüzünden aynı komuta farklı yanıt üretirdi). Yan etkisi: şema/bant/bağlam
        /// redleri Kapı 4'e HİÇ ulaşmadığı için rate limit penceresini tüketmez, ama yine de
        /// 24 saatlik kayıt açardı — benzersiz `CommandId`'lerle bozuk payload seli, paylaşılan
        /// depoyu sınırsız büyütebilirdi. Çözüm: dedup penceresi YÜRÜTÜLEN komutlar için uzun,
        /// yürütmeye ulaşmamış redler için kısadır. "Red de idempotenttir" sözleşmesi gerçek
        /// retry ufkunda (saniyeler-dakikalar) korunur; sel maliyeti o ufka iner.</summary>
        public IdempotencyStore(long pencereMs = 24L * 60 * 60 * 1000, long redPencereMs = 10L * 60 * 1000)
        { this.pencereMs = pencereMs; this.redPencereMs = redPencereMs > 0 ? redPencereMs : pencereMs; }

        public int Count { get { lock (kilit) return kayit.Count; } }

        /// <summary>ATOMİK rezervasyon: ya sahiplik alınır, ya önceki yanıt döner, ya "sürüyor".
        ///
        /// UÇUŞ SÜRESİ DEVRALMASI YOKTUR (inceleme düzeltmesi): önceki sürümde bir rezervasyon
        /// belli süre sonra "çökmüş sayılıp" devralınıyordu; ama ilk çağrı hâlâ `Execute` içindeyse
        /// İKİ yürütme birden durum değiştirebiliyordu — yani exactly-once iddiası, tam da onu
        /// korumak için yazılmış kolda deliniyordu. Canlılık uğruna GÜVENLİK feda edilmez:
        /// asılı kalan rezervasyon `Prune` ile (operatör denetiminde) temizlenir, o ana kadar
        /// retry'ler `DuplicateCommand` alır — istemci için güvenli, durum için bozulmasız.</summary>
        public ReserveResult TryReserve(long userId, Guid commandId, long nowUnixMs,
                                        out CommandOutcome onceki, out ReservationToken token)
        {
            onceki = default; token = default;
            var anahtar = (userId, commandId);
            lock (kilit)
            {
                if (kayit.TryGetValue(anahtar, out var k))
                {
                    if (k.done)
                    {
                        if (nowUnixMs - k.at < k.pencere) { onceki = k.outcome.AsReplay(); return ReserveResult.Completed; }
                        kayit.Remove(anahtar);              // pencere doldu, yeniden yürütülebilir
                    }
                    else
                    {
                        return ReserveResult.InFlight;      // başka çağrı yürütüyor — DEVRALMA YOK
                    }
                }
                ulong t = sonrakiJeton++;
                kayit[anahtar] = (nowUnixMs, false, t, pencereMs, default);
                token = new ReservationToken(t);
                return ReserveResult.Reserved;
            }
        }

        /// <summary>Rezervasyonu sonuçla kapatır. Jeton eşleşmezse HİÇBİR ŞEY yapmaz —
        /// gecikmiş bir çağrı başkasının sonucunu ezemez.
        ///
        /// `yurutuldu` = komut yürütücüye ULAŞTI mı (sonucu ne olursa olsun). Yalnız ulaşanlar
        /// uzun dedup penceresini hak eder; doğrulamada düşenler kısa pencereye yazılır
        /// (bkz. yapıcıdaki `redPencereMs` notu).</summary>
        public bool Complete(long userId, Guid commandId, ReservationToken token, long nowUnixMs,
                             CommandOutcome outcome, bool yurutuldu = true)
        {
            var anahtar = (userId, commandId);
            lock (kilit)
            {
                if (!token.IsValid) return false;
                if (!kayit.TryGetValue(anahtar, out var k) || k.done || k.tok != token.Value) return false;
                kayit[anahtar] = (nowUnixMs, true, k.tok, yurutuldu ? pencereMs : redPencereMs, outcome);
                return true;
            }
        }

        /// <summary>Rezervasyonu geri alır (yürütme sırasında istisna). Jeton eşleşmezse
        /// hiçbir şey yapmaz — gecikmiş bir çağrı başkasının rezervasyonunu silemez.</summary>
        public bool Release(long userId, Guid commandId, ReservationToken token)
        {
            var anahtar = (userId, commandId);
            lock (kilit)
            {
                if (!token.IsValid) return false;
                if (!kayit.TryGetValue(anahtar, out var k) || k.done || k.tok != token.Value) return false;
                kayit.Remove(anahtar);
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
                var silinecek = new List<(long, Guid)>();
                foreach (var kv in kayit)
                {
                    long yas = nowUnixMs - kv.Value.at;
                    if (kv.Value.done) { if (yas >= kv.Value.pencere) silinecek.Add(kv.Key); }
                    else if (asiliRezervasyonMs > 0 && yas >= asiliRezervasyonMs) silinecek.Add(kv.Key);
                }
                for (int i = 0; i < silinecek.Count; i++) kayit.Remove(silinecek[i]);
                return silinecek.Count;
            }
        }
    }
}
