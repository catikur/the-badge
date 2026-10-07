using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TheBadge.Sim.Replay
{
    /// <summary>Okunmuş golden set: sürüm, pinli bayt özetleri ve indeks sırasıyla kanonik kayıtlar.</summary>
    public sealed class GoldenSet
    {
        public string Surum;
        public ulong BalanceHash, BandsHash;
        /// <summary>İndeks = replay indeksi; her biri <see cref="GoldenReplay.Kayit"/> yazımında.</summary>
        public string[] Kayitlar;
    }

    /// <summary>GOLDEN SET OKUYUCU — `replay_set_v1.json`. Bağımlılıksızdır: System.Text.Json istemcide
    /// yok, istemcinin kendi okuyucusu pakete giremez (ADR-004). Checks'in okuyucusuyla AYNI
    /// katılıkta: "0x" + tam 16 hex, eksik alan, tekrarlı/bant dışı indeks ve tamsayı olmayan sayı
    /// REDDEDİLİR. İki okuyucunun aynı kayıtları verdiği Checks'te ayrıca ölçülür.</summary>
    public static class GoldenSetOkuyucu
    {
        public static GoldenSet Oku(string json)
        {
            var kok = Nesne(new JsonOkuyucu(json).Belge(), "kök");
            var set = new GoldenSet
            {
                Surum = Dizgi(Alan(kok, "surum"), "surum"),
                BalanceHash = Hex64(Dizgi(Alan(kok, "balanceHash"), "balanceHash"), "balanceHash"),
                BandsHash = Hex64(Dizgi(Alan(kok, "bandsHash"), "bandsHash"), "bandsHash"),
                Kayitlar = new string[GoldenReplay.Sayi]
            };
            if (!(Alan(kok, "replayler") is List<object> dizi))
                throw new FormatException("golden: 'replayler' dizi olmalı");
            if (dizi.Count != GoldenReplay.Sayi)
                throw new FormatException("golden: " + dizi.Count + " kayıt var, " + GoldenReplay.Sayi + " olmalı");
            foreach (var oge in dizi)
            {
                var k = Nesne(oge, "kayıt");
                long idx = Tamsayi(Alan(k, "idx"), "idx");
                if (idx < 0 || idx >= GoldenReplay.Sayi) throw new FormatException("golden: indeks bant dışı: " + idx);
                if (set.Kayitlar[idx] != null) throw new FormatException("golden: tekrarlı indeks: " + idx);
                set.Kayitlar[idx] = GoldenReplay.Kayit(
                    Hex64(Dizgi(Alan(k, "configHash"), "configHash"), "configHash"),
                    Hex64(Dizgi(Alan(k, "stateHash"), "stateHash"), "stateHash"),
                    Dizgi(Alan(k, "skor"), "skor"),
                    UInt(Alan(k, "tick"), "tick"),
                    Hex64(Dizgi(Alan(k, "komutIz"), "komutIz"), "komutIz"),
                    UInt(Alan(k, "uygulanan"), "uygulanan"),
                    UInt(Alan(k, "reddedilen"), "reddedilen"),
                    UInt(Alan(k, "degisiklik"), "degisiklik"));
            }
            return set;
        }

        /// <summary>"0x" + tam 16 hex → ulong. Öneksiz bir değerde ilk iki haneyi SESSİZCE yutmaz.</summary>
        public static ulong Hex64(string s, string ad)
        {
            if (s == null || s.Length != 18 || !s.StartsWith("0x", StringComparison.Ordinal))
                throw new FormatException("golden: " + ad + " '0x' + 16 hex olmalı: '" + s + "'");
            if (!ulong.TryParse(s.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ulong v))
                throw new FormatException("golden: " + ad + " hex değil: '" + s + "'");
            return v;
        }

        static Dictionary<string, object> Nesne(object o, string ne)
            => o as Dictionary<string, object> ?? throw new FormatException("golden: " + ne + " nesne olmalı");

        static object Alan(Dictionary<string, object> n, string ad)
            => n.TryGetValue(ad, out var v) ? v : throw new FormatException("golden: eksik alan '" + ad + "'");

        static string Dizgi(object o, string ad)
            => o as string ?? throw new FormatException("golden: '" + ad + "' dizgi olmalı");

        static long Tamsayi(object o, string ad)
            => o is long l ? l : throw new FormatException("golden: '" + ad + "' tamsayı olmalı");

        static uint UInt(object o, string ad)
        {
            long l = Tamsayi(o, ad);
            if (l < 0 || l > uint.MaxValue) throw new FormatException("golden: '" + ad + "' uint aralığında değil: " + l);
            return (uint)l;
        }

        /// <summary>Küçük JSON okuyucu: nesne, dizi, dizgi, TAMSAYI, true/false/null. Ondalık sayı
        /// golden sette yoktur ve kabul edilmez — kültüre ve ayrıştırıcıya bağlı tek alan böylece
        /// kapı dışında kalır.</summary>
        sealed class JsonOkuyucu
        {
            readonly string s;
            int i;
            public JsonOkuyucu(string metin) { s = metin ?? throw new FormatException("golden: boş metin"); }

            public object Belge()
            {
                if (s.Length > 0 && s[0] == '\uFEFF') i = 1;   // UTF-8 BOM (görünmez karakter kaynağa yazılmaz)
                var v = Deger();
                Bosluk();
                if (i != s.Length) Hata("belge sonunda fazladan içerik");
                return v;
            }

            object Deger()
            {
                Bosluk();
                if (i >= s.Length) Hata("beklenmeyen son");
                char c = s[i];
                if (c == '{') return NesneOku();
                if (c == '[') return DiziOku();
                if (c == '"') return DizgiOku();
                if (c == 't') { Sozcuk("true"); return true; }
                if (c == 'f') { Sozcuk("false"); return false; }
                if (c == 'n') { Sozcuk("null"); return null; }
                if (c == '-' || (c >= '0' && c <= '9')) return SayiOku();
                Hata("beklenmeyen karakter '" + c + "'");
                return null;
            }

            Dictionary<string, object> NesneOku()
            {
                var n = new Dictionary<string, object>(StringComparer.Ordinal);
                i++;
                Bosluk();
                if (Bak('}')) { i++; return n; }
                while (true)
                {
                    Bosluk();
                    if (!Bak('"')) Hata("anahtar dizgi olmalı");
                    string ad = DizgiOku();
                    Bosluk();
                    Bekle(':');
                    var v = Deger();
                    if (n.ContainsKey(ad)) Hata("tekrarlı anahtar '" + ad + "'");
                    n[ad] = v;
                    Bosluk();
                    if (Bak(',')) { i++; continue; }
                    Bekle('}');
                    return n;
                }
            }

            List<object> DiziOku()
            {
                var d = new List<object>();
                i++;
                Bosluk();
                if (Bak(']')) { i++; return d; }
                while (true)
                {
                    d.Add(Deger());
                    Bosluk();
                    if (Bak(',')) { i++; continue; }
                    Bekle(']');
                    return d;
                }
            }

            string DizgiOku()
            {
                i++;   // açılış tırnağı
                var b = new StringBuilder();
                while (true)
                {
                    if (i >= s.Length) Hata("kapanmamış dizgi");
                    char c = s[i++];
                    if (c == '"') return b.ToString();
                    if (c != '\\') { b.Append(c); continue; }
                    if (i >= s.Length) Hata("yarım kaçış");
                    char k = s[i++];
                    switch (k)
                    {
                        case '"': b.Append('"'); break;
                        case '\\': b.Append('\\'); break;
                        case '/': b.Append('/'); break;
                        case 'b': b.Append('\b'); break;
                        case 'f': b.Append('\f'); break;
                        case 'n': b.Append('\n'); break;
                        case 'r': b.Append('\r'); break;
                        case 't': b.Append('\t'); break;
                        case 'u':
                            if (i + 4 > s.Length) Hata("yarım \\u kaçışı");
                            if (!ushort.TryParse(s.Substring(i, 4), NumberStyles.AllowHexSpecifier,
                                                 CultureInfo.InvariantCulture, out ushort u)) Hata("geçersiz \\u kaçışı");
                            b.Append((char)u); i += 4; break;
                        default: Hata("bilinmeyen kaçış '\\" + k + "'"); break;
                    }
                }
            }

            long SayiOku()
            {
                int bas = i;
                if (s[i] == '-') i++;
                while (i < s.Length && s[i] >= '0' && s[i] <= '9') i++;
                if (i < s.Length && (s[i] == '.' || s[i] == 'e' || s[i] == 'E'))
                    Hata("golden sette ondalık sayı yok — tamsayı bekleniyordu");
                if (!long.TryParse(s.Substring(bas, i - bas), NumberStyles.AllowLeadingSign,
                                   CultureInfo.InvariantCulture, out long v)) Hata("geçersiz sayı");
                return v;
            }

            void Sozcuk(string w)
            {
                if (string.CompareOrdinal(s, i, w, 0, w.Length) != 0) Hata("'" + w + "' bekleniyordu");
                i += w.Length;
            }

            void Bosluk() { while (i < s.Length && (s[i] == ' ' || s[i] == '\n' || s[i] == '\r' || s[i] == '\t')) i++; }
            bool Bak(char c) => i < s.Length && s[i] == c;
            void Bekle(char c) { if (!Bak(c)) Hata("'" + c + "' bekleniyordu"); i++; }
            void Hata(string m) => throw new FormatException("golden JSON, konum " + i + ": " + m);
        }
    }
}
