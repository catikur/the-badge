using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using TheBadge.Sim.Core;

namespace TheBadge.Sim.Replay
{
    /// <summary>BALANCE DEĞER DÖKÜMÜ — okunmuş balance nesnesinin her alanı, bit düzeyinde ve
    /// kanonik sırayla (ADR-004). Neden: sunucu balance'ı bir JSON aracıyla, istemci başka bir
    /// araçla okur; sayı ayrıştırması son bitte farklıysa istemci simi kod aynıyken bile ilk
    /// tick'ten ayrışır. Döküm bu sapmayı yürütme sapmasından AYIRIR ve hangi alanda olduğunu gösterir.
    ///
    /// Biçim: satır başına "yol = değer", alanlar her düzeyde ADA göre sıralı (ordinal) — yansımanın
    /// alan sırası platforma bağlı olabilir. Ondalıklar YALNIZ bitleriyle yazılır ("d:0x…"); ondalık
    /// metin biçimlemesi çalışma zamanına göre değiştiği için karşılaştırmaya girmez.</summary>
    public static class BalansDokumu
    {
        public static string Metin(object kok)
        {
            var satirlar = new List<string>();
            Yaz(kok, "", satirlar, 0);
            return string.Join("\n", satirlar) + "\n";
        }

        public static ulong Ozet(string metin) => XxHash64.Hash(Encoding.UTF8.GetBytes(metin));

        /// <summary>İki dökümün farkı: farklı ya da tek tarafta olan satırlar (en çok <paramref name="enCok"/>).</summary>
        public static List<string> Fark(string beklenen, string olculen, int enCok)
        {
            var b = Tablo(beklenen); var o = Tablo(olculen);
            var anahtarlar = new List<string>(b.Keys);
            foreach (var k in o.Keys) if (!b.ContainsKey(k)) anahtarlar.Add(k);
            anahtarlar.Sort(StringComparer.Ordinal);
            var fark = new List<string>();
            foreach (var k in anahtarlar)
            {
                b.TryGetValue(k, out string vb); o.TryGetValue(k, out string vo);
                if (vb == vo) continue;
                fark.Add(k + ": sunucu " + Okunur(vb) + " · burada " + Okunur(vo));
                if (fark.Count >= enCok) break;
            }
            return fark;
        }

        static Dictionary<string, string> Tablo(string metin)
        {
            var t = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var satir in (metin ?? "").Split('\n'))
            {
                int esit = satir.IndexOf(" = ", StringComparison.Ordinal);
                if (esit > 0) t[satir.Substring(0, esit)] = satir.Substring(esit + 3);
            }
            return t;
        }

        /// <summary>Bir döküm değerini insan için açar: "d:0x…" → bitler + ondalık; diğerleri olduğu gibi.</summary>
        static string Okunur(string v)
        {
            if (v == null) return "(yok)";
            if (v.StartsWith("d:0x", StringComparison.Ordinal) &&
                long.TryParse(v.Substring(4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out long bit))
                return v + " (" + BitConverter.Int64BitsToDouble(bit).ToString("R", CultureInfo.InvariantCulture) + ")";
            return v;
        }

        static void Yaz(object o, string yol, List<string> satirlar, int derinlik)
        {
            if (derinlik > 16) throw new InvalidOperationException("balance dökümü: iç içelik çok derin: " + yol);
            var c = CultureInfo.InvariantCulture;
            if (o == null) { satirlar.Add(yol + " = null"); return; }
            switch (o)
            {
                case double d: satirlar.Add(yol + " = d:0x" + BitConverter.DoubleToInt64Bits(d).ToString("X16", c)); return;
                case float f: satirlar.Add(yol + " = f:0x" + BitConverter.SingleToInt32Bits(f).ToString("X8", c)); return;
                case int n: satirlar.Add(yol + " = i:" + n.ToString(c)); return;
                case long l: satirlar.Add(yol + " = l:" + l.ToString(c)); return;
                case bool b: satirlar.Add(yol + " = b:" + (b ? "1" : "0")); return;
                case string s: satirlar.Add(yol + " = s:" + s.Length.ToString(c) + ":" + s); return;
            }
            var t = o.GetType();
            if (t.IsEnum) { satirlar.Add(yol + " = e:" + Convert.ToInt64(o, c).ToString(c)); return; }
            if (t.IsPrimitive) { satirlar.Add(yol + " = p:" + Convert.ToString(o, c)); return; }
            if (o is Array a)
            {
                satirlar.Add(yol + ".Length = i:" + a.Length.ToString(c));
                for (int i = 0; i < a.Length; i++) Yaz(a.GetValue(i), yol + "[" + i.ToString(c) + "]", satirlar, derinlik + 1);
                return;
            }
            var alanlar = t.GetFields(BindingFlags.Public | BindingFlags.Instance);
            Array.Sort(alanlar, (x, y) => string.CompareOrdinal(x.Name, y.Name));
            foreach (var alan in alanlar)
                Yaz(alan.GetValue(o), yol.Length == 0 ? alan.Name : yol + "." + alan.Name, satirlar, derinlik + 1);
        }
    }
}
