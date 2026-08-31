using System;
using System.Collections.Generic;

namespace TheBadge.World
{
    /// <summary>Golden set satırının KALİTE RUBRİĞİ — `evals/golden/*.jsonl` içindeki `expect`.</summary>
    public sealed class EvalRubrik
    {
        public string Id;
        public string Boyut;              // olgu · ton · yasak · uzunluk (kapsam kapısı bunu sayar)
        public string GirdiMetni;         // rubriğin dayandığı girdi paketi, düz metin
        public string Skor;               // "3-0 G" gibi — "yanlis skor" dedektörünün referansı
        public bool ArkVar;               // girdide aktif ark referansı var mı
        public string[] MustInclude = new string[0];
        public string[] Yasak = new string[0];
        public string Ton;
        public int MaxCumle = 2;
    }

    /// <summary>Tek bir çıktının puanı. `MakineKarari` KAPIYA girer; `InsanBakisi` girmez.</summary>
    public readonly struct EvalPuan
    {
        public readonly bool MakineKarari;             // makineyle doğrulanabilen TÜM boyutlar geçti mi
        public readonly string Detay;                  // düşen boyutlar
        public readonly IReadOnlyList<string> InsanBakisi;  // makinenin YARGILAMADIĞI boyutlar
        public EvalPuan(bool ok, string detay, IReadOnlyList<string> insan)
        { MakineKarari = ok; Detay = detay; InsanBakisi = insan; }
    }

    /// <summary>Röportaj çıktısı için deterministik rubrik puanlayıcı — docs/evals/README boyutları.
    ///
    /// NE YAPMAZ: prose kalitesini, üslubun inceliğini ya da "gerçekten kışkırtıcı mı"yı YARGILAMAZ.
    /// Bunları puanlamak, ölçmediği bir şeye sayı vermek olurdu. Bu boyutlar `InsanBakisi` listesine
    /// düşer ve koşu raporunda AYRI gösterilir (evals/golden/README: "script + insan bakışı karışımı").
    ///
    /// NE YAPAR — makineyle kesin karar verilebilen boyutlar:
    ///   · şema: çıktı bir SORU mu (şablon "1 soru" diyor), cümle sayısı bandı
    ///   · olgu: girdide OLMAYAN sayı = uydurma istatistik · yazılan skor girdiyle aynı mı
    ///   · must_include / yasak sözlükleri
    ///
    /// BİLİNMEYEN ANAHTAR SESSİZCE GEÇMEZ: `yasak` ya da `ton` içinde tanımadığı bir anahtar görürse
    /// puanlayıcı bunu HATA olarak döndürür. Tanımadığı kuralı "geçti" saymak, rubriği sessizce
    /// zayıflatırdı — K9-A'daki "çözemediğini atlama" disipliniyle aynı.</summary>
    public static class EvalScorer
    {
        // --- sözlükler (küçük ve açık; genişletmek rubrik değişikliğidir) ----------------------
        static readonly string[] AlayLex = { "komik", "gulunc", "acinasi", "rezalet", "utanc verici", "dalga gec" };
        static readonly string[] HakaretLex = { "beceriksiz", "aptal", "salak", "ise yaramaz", "rezil" };
        static readonly string[] TibbiLex = { "kirik", "yirtik", "menisk", "capraz bag", "ameliyat", "teshis", "roentgen", "mr sonucu" };
        static readonly string[] JargonLex = { "xg", "ppda", "blok yuksekligi", "half space", "gegenpressing", "progressive pass", "field tilt" };
        static readonly string[] RakipMenajerLex = { "rakip menajer", "meslektas", "karsi tarafin hocasi" };

        /// <summary>Türkçe duyarsız normalleştirme — karşılaştırmalar bunun üstünde yapılır.
        /// `ToLowerInvariant` TEK BAŞINA yetmez: "İ" → "i̇" (birleşik) döner ve "kirmizi" ile
        /// "KIRMIZI" eşleşmez. Harf eşlemesi elle yapılır.</summary>
        public static string Normalle(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (char c in s)
            {
                char x = c;
                switch (c)
                {
                    case 'İ': case 'I': case 'ı': x = 'i'; break;
                    case 'Ğ': case 'ğ': x = 'g'; break;
                    case 'Ü': case 'ü': x = 'u'; break;
                    case 'Ş': case 'ş': x = 's'; break;
                    case 'Ö': case 'ö': x = 'o'; break;
                    case 'Ç': case 'ç': x = 'c'; break;
                    default:
                        if (c >= 'A' && c <= 'Z') x = (char)(c + 32);
                        break;
                }
                sb.Append(x);
            }
            return sb.ToString();
        }

        static bool Icerir(string norm, string parca) => norm.IndexOf(Normalle(parca), StringComparison.Ordinal) >= 0;

        static bool IcerirHerhangi(string norm, string[] lex)
        {
            for (int i = 0; i < lex.Length; i++) if (Icerir(norm, lex[i])) return true;
            return false;
        }

        /// <summary>Metindeki tam sayıları toplar. Regex YOK — kültür/derleyici farkı riski sıfır.</summary>
        public static List<string> Sayilar(string s)
        {
            var liste = new List<string>();
            if (string.IsNullOrEmpty(s)) return liste;
            int i = 0;
            while (i < s.Length)
            {
                if (s[i] >= '0' && s[i] <= '9')
                {
                    int bas = i;
                    while (i < s.Length && s[i] >= '0' && s[i] <= '9') i++;
                    liste.Add(s.Substring(bas, i - bas));
                }
                else i++;
            }
            return liste;
        }

        /// <summary>Cümle sayısı — `.`, `?`, `!` sonlandırıcı sayılır; ardışık sonlandırıcılar tek sayılır.</summary>
        public static int CumleSayisi(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            int n = 0; bool oncekiSonlandirici = false; bool icerikVar = false;
            foreach (char c in s)
            {
                bool son = c == '.' || c == '?' || c == '!';
                if (son) { if (!oncekiSonlandirici && icerikVar) n++; oncekiSonlandirici = true; }
                else { oncekiSonlandirici = false; if (!char.IsWhiteSpace(c)) icerikVar = true; }
            }
            if (icerikVar && !oncekiSonlandirici) n++;   // sonlandırıcısız son cümle
            return n;
        }

        public static EvalPuan Puanla(string cikti, EvalRubrik r)
        {
            if (r == null) throw new ArgumentNullException(nameof(r));
            var insan = new List<string>();
            string hata = "";
            string cn = Normalle(cikti ?? "");
            string gn = Normalle(r.GirdiMetni ?? "");

            // --- ŞEMA: şablon "gazeteci personasıyla 1 SORU" diyor ---------------------------
            if (string.IsNullOrWhiteSpace(cikti)) return new EvalPuan(false, "cikti bos", insan);
            if (cikti.IndexOf('?') < 0) hata += "soru degil(? yok) ";
            int cs = CumleSayisi(cikti);
            if (cs > r.MaxCumle) hata += $"cumle {cs} > {r.MaxCumle} ";
            if (cs == 0) hata += "cumle yok ";

            // --- MUST_INCLUDE ----------------------------------------------------------------
            for (int i = 0; i < r.MustInclude.Length; i++)
                if (!Icerir(cn, r.MustInclude[i])) hata += $"eksik:{r.MustInclude[i]} ";

            // --- YASAK: her anahtar bir DEDEKTÖRE bağlıdır -----------------------------------
            for (int i = 0; i < r.Yasak.Length; i++)
            {
                string y = Normalle(r.Yasak[i]);
                switch (y)
                {
                    case "uydurma istatistik":
                        {
                            // Girdide OLMAYAN her sayı uydurmadır (memory_facts dışına çıkma YOK).
                            var gs = new HashSet<string>(Sayilar(r.GirdiMetni ?? ""));
                            foreach (var n in Sayilar(cikti))
                                if (!gs.Contains(n)) { hata += $"uydurma sayi:{n} "; break; }
                            break;
                        }
                    case "yanlis skor":
                        {
                            // Çıktıdaki her "a-b" kalıbı GİRDİNİN HERHANGİ BİR YERİNDE geçmeli —
                            // yalnız `skor` alanında değil. Fikstür bunu yakaladı (g019): maçın
                            // skoru 3-3 ama girdi "3-0 onde iken" diyor; 3-0'a atıf UYDURMA DEĞİL,
                            // pakette olan bir olgudur. Referansı `skor` alanına daraltmak, doğru
                            // cümleyi hatalı sayıyordu — kural zaten "memory_facts dışına çıkma".
                            var girdiSkorlari = new HashSet<string>(SkorKaliplari(r.GirdiMetni ?? ""));
                            foreach (var k in SkorKaliplari(cikti))
                                if (!girdiSkorlari.Contains(k)) { hata += $"yanlis skor:{k} "; break; }
                            break;
                        }
                    case "alay": if (IcerirHerhangi(cn, AlayLex)) hata += "alay "; break;
                    case "hakaret": if (IcerirHerhangi(cn, HakaretLex)) hata += "hakaret "; break;
                    case "hakem hakareti":
                        if (Icerir(cn, "hakem") && IcerirHerhangi(cn, HakaretLex)) hata += "hakem hakareti ";
                        break;
                    case "tibbi teshis": if (IcerirHerhangi(cn, TibbiLex)) hata += "tibbi teshis "; break;
                    case "jargon agirligi":
                        {
                            int j = 0;
                            for (int k = 0; k < JargonLex.Length; k++) if (Icerir(cn, JargonLex[k])) j++;
                            if (j >= 2) hata += $"jargon agirligi({j}) ";
                            break;
                        }
                    case "ark disi polemik":
                        if (!r.ArkVar && IcerirHerhangi(cn, RakipMenajerLex)) hata += "ark disi polemik ";
                        break;
                    default:
                        // SESSİZ GEÇİŞ YOK: tanımadığı kuralı "geçti" saymak rubriği zayıflatırdı.
                        hata += $"BILINMEYEN yasak anahtari:{r.Yasak[i]} ";
                        break;
                }
            }

            // --- TON: yalnız makineyle kesin olan bileşen kontrol edilir ----------------------
            switch (Normalle(r.Ton ?? ""))
            {
                case "sorgulayici":
                case "endise+soru":
                case "kiskirtici":
                    // soru işareti zaten şemada denetlendi; üslubun kendisi insan bakışı
                    insan.Add($"{r.Id}: ton uslubu '{r.Ton}'");
                    break;
                case "tebrik+manset":
                case "tanisma+umut":
                    insan.Add($"{r.Id}: ton uslubu '{r.Ton}'");
                    break;
                default:
                    hata += $"BILINMEYEN ton:{r.Ton} ";
                    break;
            }
            insan.Add($"{r.Id}: TR dil kalitesi");

            return new EvalPuan(hata.Length == 0, hata.TrimEnd(), insan);
        }

        /// <summary>Metindeki "a-b" skor kalıpları (normalize edilmiş).</summary>
        static List<string> SkorKaliplari(string s)
        {
            var liste = new List<string>();
            if (string.IsNullOrEmpty(s)) return liste;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] < '0' || s[i] > '9') continue;
                int bas = i;
                while (i < s.Length && s[i] >= '0' && s[i] <= '9') i++;
                if (i < s.Length && (s[i] == '-' || s[i] == '–'))
                {
                    int tire = i; i++;
                    if (i < s.Length && s[i] >= '0' && s[i] <= '9')
                    {
                        while (i < s.Length && s[i] >= '0' && s[i] <= '9') i++;
                        liste.Add(s.Substring(bas, i - bas).Replace('–', '-'));
                        continue;
                    }
                    i = tire;
                }
            }
            return liste;
        }

        /// <summary>Bir koşunun toplu sonucu.</summary>
        public readonly struct KosuSonucu
        {
            public readonly int Toplam, Gecen;
            public readonly double YuzdeGecme;
            public readonly IReadOnlyList<string> Dusenler;
            public readonly IReadOnlyList<string> InsanBakisi;
            public KosuSonucu(int toplam, int gecen, IReadOnlyList<string> dusenler, IReadOnlyList<string> insan)
            { Toplam = toplam; Gecen = gecen; YuzdeGecme = toplam == 0 ? 0 : 100.0 * gecen / toplam; Dusenler = dusenler; InsanBakisi = insan; }
        }

        /// <summary>Rubrik listesi + aynı sıradaki çıktılar → koşu sonucu. Eşik ÇAĞIRANIN işidir
        /// (balance'tan okunur); puanlayıcı eşiği bilmez, yalnız ölçer.</summary>
        public static KosuSonucu Kos(IReadOnlyList<EvalRubrik> rubrikler, IReadOnlyList<string> ciktilar)
        {
            if (rubrikler == null) throw new ArgumentNullException(nameof(rubrikler));
            if (ciktilar == null) throw new ArgumentNullException(nameof(ciktilar));
            if (rubrikler.Count != ciktilar.Count)
                throw new ArgumentException($"rubrik {rubrikler.Count} ile cikti {ciktilar.Count} sayisi uyusmuyor — " +
                                            "eksik cikti SESSIZCE gecmis sayilamaz");
            var dusen = new List<string>(); var insan = new List<string>();
            int gecen = 0;
            for (int i = 0; i < rubrikler.Count; i++)
            {
                var p = Puanla(ciktilar[i], rubrikler[i]);
                if (p.MakineKarari) gecen++;
                else dusen.Add($"{rubrikler[i].Id}: {p.Detay}");
                for (int j = 0; j < p.InsanBakisi.Count; j++) insan.Add(p.InsanBakisi[j]);
            }
            return new KosuSonucu(rubrikler.Count, gecen, dusen, insan);
        }
    }
}
