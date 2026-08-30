namespace TheBadge.Sim.Determinism
{
    /// <summary>Rastgelelik domain akışları — ME Spec 3.1. CROWD yalnız istemci kozmetiğidir.</summary>
    public enum Domain : uint
    {
        Decision = 1, Physics = 2, Duel = 3, Chaos = 4,
        Referee = 5, Injury = 6, SetPiece = 7, Crowd = 8
    }

    /// <summary>
    /// Sayaç-tabanlı, DURUMSUZ RNG (SplitMix64 çekirdeği) — ME Spec 3.1.
    /// Her değer adresinden (seed, domain, entity, tick, salt) türetilir;
    /// çağrı SIRASINDAN bağımsızdır. Müdahaleler diğer çekilişleri kaydıramaz.
    /// YASAK: Simülasyon içinde System.Random veya Guid tabanlı rastgelelik.
    /// </summary>
    public static class Rng
    {
        public static ulong Hash64(ulong seed, uint domain, uint entity, uint tick, uint salt)
        {
            ulong z = seed
                      ^ (domain * 0x9E3779B97F4A7C15UL)
                      ^ ((ulong)entity << 32)
                      ^ ((ulong)tick << 1)
                      ^ salt;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        /// <summary>[0,1) aralığında deterministik değer.</summary>
        public static double Rand01(ulong seed, Domain d, uint entity, uint tick, uint salt)
            => (Hash64(seed, (uint)d, entity, tick, salt) >> 11) * (1.0 / 9007199254740992.0);

        /// <summary>
        /// Deterministik yaklaşık Gauss (12-toplam) — ortalama 0, sigma ~1. — ME Spec 3.1
        ///
        /// ALT-SALT YAYILIMI SPEC'TEN AYRILIR (KARAR: Atilla, 2026-08-30; DECISIONS bağlayıcı kayıt).
        /// ME 3.1'in kod bloğu `s * 16 + i` yazar; bu, 12 çekilişi `[16·salt, 16·salt+12)` aralığına
        /// koyar ve bu küme bit-0/bit-1 çevirmeleri altında KAPALIdır. Adres `z = seed ^ … ^
        /// (tick<<1) ^ salt` biçiminde XOR'landığı için, seed'in bit-0'ını veya tick'in ilgili
        /// bitini çevirmek salt'ları yalnız kendi aralarında yer değiştiriyordu: çokluk kümesi —
        /// dolayısıyla toplam — DEĞİŞMİYORDU. Ölçüm: komşu tick'lerin %50,0'ı, bit-0 farklı
        /// tohumların %100,0'ı aynı Gauss değerini alıyordu.
        ///
        /// Tek sayı adımlı yayılım bunu kırar: 12 adres `base + i·k` (k tek) biçimindedir; bir
        /// elemanın bit-0'ını çevirmek başka bir elemana düşemez, çünkü bu `(j-i)·k = ±1 (mod 2^32)`
        /// gerektirir ve k tersinir olduğundan çözüm |j-i| ≤ 11 aralığının çok dışındadır.
        /// Ölçüm sonrası: komşu tick %0,0 · bit0-seed %0,0. `K3RngGauss01Borcu` bunu izler.
        /// </summary>
        public static double Gauss01(ulong seed, Domain d, uint entity, uint tick, uint salt)
        {
            double sum = 0;
            for (uint i = 0; i < 12; i++)
                sum += Rand01(seed, d, entity, tick, unchecked(salt * 0x9E3779B1u + i * 0x85EBCA6Bu));
            return sum - 6.0;
        }
    }
}
