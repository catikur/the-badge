using System;
using System.Collections.Generic;
using TheBadge.Sim.Commands;

namespace TheBadge.CommandBus
{
    /// <summary>Rate limit — CB Spec 5.1. Kayan pencere, (userId + aksiyon sınıfı) kapsamında.</summary>
    public interface IRateLimiter
    {
        bool Allow(long userId, RateClass cls, CommandSource source, long nowUnixMs);
        /// <summary>İstismar sinyali — CB 5.1: 5 dk içinde 3 kez RateLimited alan kullanıcı
        /// için denetim loguna AbuseFlag düşer (GDD 6.5 örüntü analizine girdi).</summary>
        bool ConsumeAbuseFlag(long userId, long nowUnixMs);
    }

    /// <summary>Sınıf başına limit tanımı — DEĞERLER balance'tan gelir (CB 5.1 tablosu).</summary>
    public sealed class RateLimitCfg
    {
        public readonly int Limit;        // izin verilen komut sayısı
        public readonly long WindowMs;    // pencere uzunluğu
        public RateLimitCfg(int limit, long windowMs) { Limit = limit; WindowMs = windowMs; }
    }

    /// <summary>Bellek içi kayan pencere sayacı. Sunucuda kalıcı depoya (Redis) taşınabilir;
    /// mantık burada tek yerde durur ki istemci ön-doğrulaması ile sunucu doğrulaması AYNI olsun.
    /// Zaman DIŞARIDAN verilir (`nowUnixMs`) — `DateTime.Now` yok: test edilebilirlik ve
    /// determinizm (aynı girdi = aynı karar) TheBadge.Sim disipliniyle aynı.</summary>
    public sealed class SlidingWindowRateLimiter : IRateLimiter
    {
        readonly Dictionary<RateClass, RateLimitCfg[]> cfg;   // bir sınıfın BİRDEN ÇOK penceresi olabilir
        readonly Dictionary<long, List<long>> hits = new Dictionary<long, List<long>>();
        readonly Dictionary<long, List<long>> redler = new Dictionary<long, List<long>>();
        readonly int abuseEsik;
        readonly long abusePencereMs;

        public SlidingWindowRateLimiter(Dictionary<RateClass, RateLimitCfg[]> config,
                                        int abuseEsik = 3, long abusePencereMs = 300_000)
        {
            cfg = config ?? throw new ArgumentNullException(nameof(config));
            this.abuseEsik = abuseEsik; this.abusePencereMs = abusePencereMs;
        }

        static long Key(long userId, RateClass cls) => userId * 16 + (long)cls;

        public bool Allow(long userId, RateClass cls, CommandSource source, long nowUnixMs)
        {
            // LLM kaynağı ModB penceresine DE tabidir (CB 5.1 "ModB çağrısı"): kaynak sınıfı
            // düşürmez, EKLER — LLM'den gelen komut hem kendi sınıfının hem ModB'nin limitindedir.
            if (source == CommandSource.LLM && !Izin(userId, RateClass.ModB, nowUnixMs)) { Redle(userId, nowUnixMs); return false; }
            if (!Izin(userId, cls, nowUnixMs)) { Redle(userId, nowUnixMs); return false; }
            Kaydet(userId, cls, nowUnixMs);
            if (source == CommandSource.LLM) Kaydet(userId, RateClass.ModB, nowUnixMs);
            return true;
        }

        bool Izin(long userId, RateClass cls, long now)
        {
            if (!cfg.TryGetValue(cls, out var pencereler) || pencereler == null) return true;
            long k = Key(userId, cls);
            if (!hits.TryGetValue(k, out var list)) return true;
            for (int i = 0; i < pencereler.Length; i++)
            {
                var w = pencereler[i];
                int sayim = 0;
                for (int j = list.Count - 1; j >= 0; j--)
                {
                    if (now - list[j] >= w.WindowMs) break;      // liste artan sırada
                    sayim++;
                }
                if (sayim >= w.Limit) return false;
            }
            return true;
        }

        void Kaydet(long userId, RateClass cls, long now)
        {
            long k = Key(userId, cls);
            if (!hits.TryGetValue(k, out var list)) { list = new List<long>(); hits[k] = list; }
            list.Add(now);
            // Budama: en uzun pencerenin dışında kalanlar atılır (bellek sızıntısı olmasın)
            long enUzun = 0;
            if (cfg.TryGetValue(cls, out var ws) && ws != null)
                for (int i = 0; i < ws.Length; i++) if (ws[i].WindowMs > enUzun) enUzun = ws[i].WindowMs;
            int kes = 0;
            while (kes < list.Count && now - list[kes] >= enUzun) kes++;
            if (kes > 0) list.RemoveRange(0, kes);
        }

        void Redle(long userId, long now)
        {
            if (!redler.TryGetValue(userId, out var list)) { list = new List<long>(); redler[userId] = list; }
            list.Add(now);
            int kes = 0;
            while (kes < list.Count && now - list[kes] >= abusePencereMs) kes++;
            if (kes > 0) list.RemoveRange(0, kes);
        }

        public bool ConsumeAbuseFlag(long userId, long nowUnixMs)
        {
            if (!redler.TryGetValue(userId, out var list)) return false;
            int sayim = 0;
            for (int j = list.Count - 1; j >= 0; j--)
            {
                if (nowUnixMs - list[j] >= abusePencereMs) break;
                sayim++;
            }
            if (sayim < abuseEsik) return false;
            list.Clear();       // bayrak tüketildi (aynı seri iki kez raporlanmaz)
            return true;
        }
    }
}
