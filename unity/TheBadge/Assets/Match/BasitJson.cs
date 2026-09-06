using System;
using System.Collections.Generic;
using System.Globalization;

namespace TheBadge.Match
{
    /// <summary>KÜÇÜK JSON OKUYUCU — yalnız `balance/command.bands.json` için, yalnız Unity
    /// sunum katmanında.
    ///
    /// NEDEN VAR: `sim.balance.json` ve `world.balance.json` Unity'nin `JsonUtility`siyle
    /// okunabiliyor (alan adları C# alan adlarıyla birebir). `command.bands.json` okunamıyor:
    /// `bantlar` bir HARİTA ve anahtarları `squad.mentalite` gibi geçerli C# tanımlayıcısı
    /// olmayan adlar; `JsonUtility` haritayı desteklemez. Kalan üç yol da elenmişti:
    ///   - Bantları koda gömmek → Kural 4 ihlali (magic number), doğrudan review reddi.
    ///   - Newtonsoft → `com.unity.nuget.newtonsoft-json` projeye YALNIZ `com.unity.ai.assistant`
    ///     üzerinden geçici olarak geliyor; onun kalacağı kayıtlı bir karar değil.
    ///   - `TheBadge.CommandBus`e parse eklemek → çekirdek bağımlılıksız kalır (CLAUDE.md).
    ///
    /// SIMÜLASYON KODU DEĞİLDİR: `Dictionary` kullanır ve yalnız ANAHTARLA sorgulanır; hiçbir
    /// yerde iterasyon SIRASINA bağlı mantık yok (ME 3.2 yasağının koruduğu şey budur).
    /// Determinizme dokunmaz — okuduğu değerler zaten config_hash içindeki dosyadan gelir.
    ///
    /// SESSİZ BAŞARI YOK: bozuk/eksik JSON istisna atar. Yarım okunmuş bir bant tablosu
    /// komutları sessizce `ParamOutOfBand`a düşürürdü ve sebebi ekranda "bant yok" diye değil
    /// "değer bant dışı" diye görünürdü — yanlış yere bakılırdı.</summary>
    public static class BasitJson
    {
        /// <summary>Ayrıştırılmış değer: `Dictionary&lt;string, object&gt;`, `List&lt;object&gt;`,
        /// `double`, `string`, `bool` ya da `null`.</summary>
        public static object Ayristir(string metin)
        {
            if (metin == null) throw new ArgumentNullException(nameof(metin));
            int i = 0;
            object v = Deger(metin, ref i);
            Bosluk(metin, ref i);
            if (i != metin.Length) throw new FormatException($"JSON: {i}. karakterden sonra artık veri var.");
            return v;
        }

        /// <summary>Nokta ayraçlı yol ile nesne içinden okuma: `Yol(kok, "bantlar")`.
        /// Anahtarların KENDİSİ nokta içerebildiği için (ör. `squad.mentalite`) yol
        /// segmentleri ayrı ayrı verilir, tek bir string olarak değil.</summary>
        public static object Yol(object kok, params string[] anahtarlar)
        {
            object o = kok;
            for (int k = 0; k < anahtarlar.Length; k++)
            {
                if (!(o is Dictionary<string, object> d) || !d.TryGetValue(anahtarlar[k], out o))
                    throw new FormatException("JSON: anahtar bulunamadı: " + string.Join("/", anahtarlar, 0, k + 1));
            }
            return o;
        }

        public static Dictionary<string, object> Nesne(object o)
            => o as Dictionary<string, object> ?? throw new FormatException("JSON: nesne bekleniyordu.");

        public static List<object> Dizi(object o)
            => o as List<object> ?? throw new FormatException("JSON: dizi bekleniyordu.");

        public static double Sayi(object o)
            => o is double d ? d : throw new FormatException("JSON: sayı bekleniyordu.");

        // ------------------------------------------------------------------ ayrıştırıcı

        static void Bosluk(string s, ref int i)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\r' || s[i] == '\n')) i++;
        }

        static object Deger(string s, ref int i)
        {
            Bosluk(s, ref i);
            if (i >= s.Length) throw new FormatException("JSON: beklenmedik son.");
            char c = s[i];
            switch (c)
            {
                case '{': return NesneOku(s, ref i);
                case '[': return DiziOku(s, ref i);
                case '"': return MetinOku(s, ref i);
                case 't': Bekle(s, ref i, "true"); return true;
                case 'f': Bekle(s, ref i, "false"); return false;
                case 'n': Bekle(s, ref i, "null"); return null;
                default: return SayiOku(s, ref i);
            }
        }

        static void Bekle(string s, ref int i, string kelime)
        {
            if (i + kelime.Length > s.Length || string.CompareOrdinal(s, i, kelime, 0, kelime.Length) != 0)
                throw new FormatException($"JSON: {i}. karakterde '{kelime}' bekleniyordu.");
            i += kelime.Length;
        }

        static Dictionary<string, object> NesneOku(string s, ref int i)
        {
            var d = new Dictionary<string, object>(StringComparer.Ordinal);
            i++;                                   // '{'
            Bosluk(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return d; }
            while (true)
            {
                Bosluk(s, ref i);
                if (i >= s.Length || s[i] != '"') throw new FormatException($"JSON: {i}. karakterde anahtar bekleniyordu.");
                string ad = MetinOku(s, ref i);
                Bosluk(s, ref i);
                if (i >= s.Length || s[i] != ':') throw new FormatException($"JSON: {i}. karakterde ':' bekleniyordu.");
                i++;
                // TEKRARLI ANAHTAR SESSİZ GEÇMEZ: üzerine yazmak, aynı bandın iki tanımından
                // hangisinin geçerli olduğunu dosyadaki SIRAYA bırakırdı.
                if (d.ContainsKey(ad)) throw new FormatException("JSON: anahtar tekrarlı: " + ad);
                d[ad] = Deger(s, ref i);
                Bosluk(s, ref i);
                if (i >= s.Length) throw new FormatException("JSON: nesne kapanmadı.");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == '}') { i++; return d; }
                throw new FormatException($"JSON: {i}. karakterde ',' ya da '}}' bekleniyordu.");
            }
        }

        static List<object> DiziOku(string s, ref int i)
        {
            var l = new List<object>();
            i++;                                   // '['
            Bosluk(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return l; }
            while (true)
            {
                l.Add(Deger(s, ref i));
                Bosluk(s, ref i);
                if (i >= s.Length) throw new FormatException("JSON: dizi kapanmadı.");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == ']') { i++; return l; }
                throw new FormatException($"JSON: {i}. karakterde ',' ya da ']' bekleniyordu.");
            }
        }

        static string MetinOku(string s, ref int i)
        {
            i++;                                   // '"'
            var sb = new System.Text.StringBuilder();
            while (true)
            {
                if (i >= s.Length) throw new FormatException("JSON: metin kapanmadı.");
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                if (i >= s.Length) throw new FormatException("JSON: kaçış karakteri yarım.");
                char e = s[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 > s.Length) throw new FormatException("JSON: \\u kaçışı yarım.");
                        sb.Append((char)ushort.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        i += 4; break;
                    default: throw new FormatException("JSON: bilinmeyen kaçış: \\" + e);
                }
            }
        }

        static object SayiOku(string s, ref int i)
        {
            int bas = i;
            if (i < s.Length && (s[i] == '-' || s[i] == '+')) i++;
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.' || s[i] == 'e' || s[i] == 'E'
                                    || ((s[i] == '-' || s[i] == '+') && (s[i - 1] == 'e' || s[i - 1] == 'E')))) i++;
            if (i == bas) throw new FormatException($"JSON: {bas}. karakterde sayı bekleniyordu.");
            // KÜLTÜRDEN BAĞIMSIZ: Türkçe yerelde ondalık ayracı virgüldür ve `double.Parse`
            // varsayılanı 0,04'ü 4 diye okurdu — kritik an eşiğini 100 katına çıkarırdı.
            return double.Parse(s.Substring(bas, i - bas), NumberStyles.Float, CultureInfo.InvariantCulture);
        }
    }
}
