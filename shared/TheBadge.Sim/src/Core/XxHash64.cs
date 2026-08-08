using System;

namespace TheBadge.Sim.Core
{
    /// <summary>
    /// xxHash64 — kanonik durum checksum'u ve config_hash için (ME Spec 3.2/3.3).
    /// Tek atımlık, tahsissiz; yalnız tamsayı aritmetiği ve AÇIK little-endian bayt okuma
    /// kullanır — platformlar arası bit eşitliği endianness'e bırakılmaz.
    /// </summary>
    public static class XxHash64
    {
        const ulong P1 = 11400714785074694791UL;
        const ulong P2 = 14029467366897019727UL;
        const ulong P3 = 1609587929392839161UL;
        const ulong P4 = 9650029242287828579UL;
        const ulong P5 = 2870177450012600261UL;

        public static ulong Hash(ReadOnlySpan<byte> data, ulong seed = 0UL)
        {
            int len = data.Length, i = 0;
            ulong h;
            if (len >= 32)
            {
                ulong v1 = seed + P1 + P2, v2 = seed + P2, v3 = seed, v4 = seed - P1;
                while (i <= len - 32)
                {
                    v1 = Round(v1, ReadU64(data, i));
                    v2 = Round(v2, ReadU64(data, i + 8));
                    v3 = Round(v3, ReadU64(data, i + 16));
                    v4 = Round(v4, ReadU64(data, i + 24));
                    i += 32;
                }
                h = Rotl(v1, 1) + Rotl(v2, 7) + Rotl(v3, 12) + Rotl(v4, 18);
                h = MergeRound(h, v1);
                h = MergeRound(h, v2);
                h = MergeRound(h, v3);
                h = MergeRound(h, v4);
            }
            else h = seed + P5;

            h += (ulong)len;
            while (i <= len - 8) { h ^= Round(0UL, ReadU64(data, i)); h = Rotl(h, 27) * P1 + P4; i += 8; }
            if (i <= len - 4) { h ^= ReadU32(data, i) * P1; h = Rotl(h, 23) * P2 + P3; i += 4; }
            while (i < len) { h ^= data[i] * P5; h = Rotl(h, 11) * P1; i++; }

            h ^= h >> 33; h *= P2; h ^= h >> 29; h *= P3; h ^= h >> 32;
            return h;
        }

        static ulong Round(ulong acc, ulong input) => Rotl(acc + input * P2, 31) * P1;
        static ulong MergeRound(ulong acc, ulong val) => (acc ^ Round(0UL, val)) * P1 + P4;
        static ulong Rotl(ulong x, int r) => (x << r) | (x >> (64 - r));

        static ulong ReadU64(ReadOnlySpan<byte> d, int i) =>
            d[i]
            | ((ulong)d[i + 1] << 8) | ((ulong)d[i + 2] << 16) | ((ulong)d[i + 3] << 24)
            | ((ulong)d[i + 4] << 32) | ((ulong)d[i + 5] << 40) | ((ulong)d[i + 6] << 48)
            | ((ulong)d[i + 7] << 56);

        static uint ReadU32(ReadOnlySpan<byte> d, int i) =>
            d[i] | ((uint)d[i + 1] << 8) | ((uint)d[i + 2] << 16) | ((uint)d[i + 3] << 24);
    }
}
