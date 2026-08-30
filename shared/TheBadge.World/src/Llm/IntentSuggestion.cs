using System;
using TheBadge.CommandBus;
using TheBadge.Sim.Commands;

namespace TheBadge.World
{
    /// <summary>LLM çıktısının ALABİLECEĞİ TEK ŞEKİL — CB 7.1. Serbest metin ya sohbettir ya da
    /// katalog içi bir ÖNERİdir; üçüncü bir olasılık yoktur.</summary>
    public enum OneriSonucu : byte
    {
        Sohbet = 0,      // aksiyon yok — düz yanıt
        Oneri = 1,       // katalog içi, bant içi öneri kartı
        Dusuruldu = 2    // öneri üretilmeye çalışıldı ama doğrulamayı geçemedi → sohbete çevrildi
    }

    /// <summary>Öneri kartı — CB 7.1 "IntentSuggestion(actionType, payload, gerekçe)".
    /// ÖNERİ YÜRÜTME DEĞİLDİR (CB 7.2 K4): kullanıcı onayı olmadan hiçbir şey çalışmaz.</summary>
    public sealed class IntentSuggestion
    {
        public Guid SuggestionId;
        public OneriSonucu Sonuc;
        public string ActionType;      // Sonuc == Oneri ise dolu
        public Tier Tier;              // KATALOGDAN gelir, LLM'den DEĞİL
        public string Gerekce;
        public string DusurmeSebebi;   // Dusuruldu ise dolu
        /// <summary>Girdi metninin ÖZETİ — CB 7.4 izlenebilirlik zinciri. Ham metin denetim
        /// loguna girmez (PII), özeti girer.</summary>
        public ulong GirdiOzeti;
    }

    /// <summary>Girdi temizliği sonucu — CB 7.1.</summary>
    public enum GirdiRedSebebi : byte { Yok = 0, Bos = 1, CokUzun = 2, KontrolKarakteri = 3, TekrarSpam = 4 }

    /// <summary>MOD B HATTI — CB 7. LLM'in ÜRETEBİLECEĞİ EN İYİ SONUÇ bir öneri kartıdır.
    ///
    /// KAPSAM SINIRI (K6 ile aynı ilke): LLM ÇAĞRISININ KENDİSİ burada yok — API erişimi bu
    /// ortamda kanıtlanamaz ve prompt'lar `docs/prompts/` altında versiyonlu dosyalarda yaşar
    /// (CLAUDE.md). Burada olan şey SAVUNMA: modelin ne söylediğinden BAĞIMSIZ olarak, çıktının
    /// katalog dışına, bant dışına ya da onaysız yürütmeye dönüşemeyeceğini yapısal kılan katman.
    /// CB 7.2'nin güvencesi zaten buna dayanır: "en başarılı injection bile yalnızca bir öneri
    /// kartı üretebilir".
    ///
    /// Bu sınıf LLM'e GÜVENMEZ: gelen `actionType` katalogda yoksa düşer, `tier` çıktıdan DEĞİL
    /// katalogdan okunur, payload bant doğrulamasına girer.</summary>
    public static class SuggestionPipeline
    {
        /// <summary>Girdi temizliği — CB 7.1. Moderasyon (nefret/taciz/PII) SUNUCU tarafındadır
        /// ve burada yok; burada olan MEKANİK temizlik: uzunluk, kontrol karakteri, tekrar spam.</summary>
        public static GirdiRedSebebi GirdiTemizle(string metin, LlmRules kural)
        {
            if (kural == null) throw new ArgumentNullException(nameof(kural));
            if (string.IsNullOrWhiteSpace(metin)) return GirdiRedSebebi.Bos;
            if (metin.Length > kural.girdi.maxKarakter) return GirdiRedSebebi.CokUzun;
            for (int i = 0; i < metin.Length; i++)
            {
                char c = metin[i];
                // Kontrol karakterleri: sınırlayıcı taklidi ve log zehirlemesi için kullanılır.
                // Sekme ve satır sonu SERBEST; geri kalan C0/C1 bloğu reddedilir.
                if (c == '\t' || c == '\n' || c == '\r') continue;
                if (c < 0x20 || (c >= 0x7F && c <= 0x9F)) return GirdiRedSebebi.KontrolKarakteri;
            }
            if (TekrarOrani(metin) >= kural.girdi.tekrarSpamOrani) return GirdiRedSebebi.TekrarSpam;
            return GirdiRedSebebi.Yok;
        }

        /// <summary>En sık karakterin oranı — ucuz ve DETERMİNİSTİK spam ölçüsü.
        /// "aaaaaaaa…" gibi girdiler bağlam penceresini şişirmek için kullanılır.
        ///
        /// TÜM KARAKTERLER SAYILIR, yalnız ASCII değil. İlk yazımda 128'lik bir dizi kullanıp
        /// `c >= 128` olanları ATLIYORDUM: TÜRKÇE bir oyunda "ğğğğğ…" ya da emoji spam'i oranı
        /// **0** veriyordu, yani filtreden serbestçe geçiyordu. Türkçe metin bu oyunda kural,
        /// istisna değil — ASCII varsayımı burada özellikle yanlıştı.
        ///
        /// `Dictionary` kullanımı burada GÜVENLİ: sonuç en yüksek SAYIdır, iterasyon SIRASINA
        /// bağlı değildir (ME 3.2 yasağı sıraya bağımlı mantık içindir). Ayrıca bu sıcak yol
        /// değil — kullanıcı mesajı başına bir kez koşar.</summary>
        internal static double TekrarOrani(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            // KOD NOKTASI (rune) sayılır, `char` DEĞİL. BMP dışı karakterler (emoji) C#'ta
            // VEKİL ÇİFTİdir: "😀"×150 iki farklı char'dan 150'şer tane demektir ve char
            // sayarken oran 0,5'te kalıp eşiğin ALTINA düşer. Emoji spam'i tam bu yüzden
            // filtreden geçiyordu — vekil çift, tekrarı görünürde YARIYA indiriyor.
            var sayac = new System.Collections.Generic.Dictionary<int, int>();
            int enCok = 0, toplam = 0;
            for (int i = 0; i < s.Length; i++)
            {
                int kod;
                if (char.IsHighSurrogate(s[i]) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))
                { kod = char.ConvertToUtf32(s[i], s[i + 1]); i++; }
                else kod = s[i];
                toplam++;
                sayac.TryGetValue(kod, out int n);
                sayac[kod] = ++n;
                if (n > enCok) enCok = n;
            }
            return toplam == 0 ? 0 : enCok / (double)toplam;
        }

        /// <summary>LLM çıktısını ÖNERİye çevirir — ya da düşürüp sohbete indirir.
        ///
        /// `onerilenAction` ve `payload` MODELDEN gelir, yani DÜŞMANCA kabul edilir.
        /// `Tier` modelden ALINMAZ: katalogdan okunur (CB 6 — "LLM tier'ını asla düşüremez").</summary>
        public static IntentSuggestion Degerlendir(string kullaniciMetni, string onerilenAction,
                                                   IPayloadView payload, IBandProvider bantlar,
                                                   LlmRules kural, ulong saveSeed)
        {
            if (bantlar == null) throw new ArgumentNullException(nameof(bantlar));
            if (kural == null) throw new ArgumentNullException(nameof(kural));
            // Payload YOKSA aksiyon önerisi değerlendirilemez: model aksiyon adı verip alanları
            // vermemiş olabilir. Bu bir düşürme sebebidir, çökme sebebi değil.
            if (payload == null && !string.IsNullOrEmpty(onerilenAction))
                return new IntentSuggestion
                {
                    SuggestionId = OneriKimligi(WorldHash.DizeOzeti(kullaniciMetni ?? string.Empty), saveSeed),
                    GirdiOzeti = WorldHash.DizeOzeti(kullaniciMetni ?? string.Empty),
                    Sonuc = OneriSonucu.Dusuruldu,
                    DusurmeSebebi = "payload yok"
                };

            ulong ozet = WorldHash.DizeOzeti(kullaniciMetni ?? string.Empty);
            var s = new IntentSuggestion
            {
                SuggestionId = OneriKimligi(ozet, saveSeed),
                GirdiOzeti = ozet
            };

            // Model aksiyon önermediyse: düz sohbet.
            if (string.IsNullOrEmpty(onerilenAction)) { s.Sonuc = OneriSonucu.Sohbet; return s; }

            // K3 — KATALOG KISITI: uydurulan fonksiyon burada ölür.
            var def = Catalog.Find(onerilenAction);
            if (def == null)
            {
                s.Sonuc = OneriSonucu.Dusuruldu;
                s.DusurmeSebebi = "katalog dışı aksiyon";
                return s;
            }

            // KAPI 1 + KAPI 2 ön denetimi: eksik zorunlu alan ya da bant dışı değer öneriyi
            // düşürür. Onay sonrası tam zincir yine koşar — bu, kullanıcıya ONAYLAYAMAYACAĞI
            // bir kart göstermemek içindir.
            for (int i = 0; i < def.Params.Length; i++)
            {
                var p = def.Params[i];
                if (p.Type == ParamType.Enum)
                {
                    if (!payload.TryGetText(p.Name, out string ev))
                    { if (p.Required) return Dusur(s, "eksik alan: " + p.Name); continue; }
                    if (TransferActions.EnumIndex(p.EnumValues, ev) < 0) return Dusur(s, "enum dışı: " + p.Name);
                    continue;
                }
                if (p.Type == ParamType.Text)
                {
                    if (!payload.TryGetText(p.Name, out string tv))
                    { if (p.Required) return Dusur(s, "eksik alan: " + p.Name); continue; }
                    if (p.MaxLength > 0 && tv != null && tv.Length > p.MaxLength)
                        return Dusur(s, "metin uzunluğu: " + p.Name);
                    continue;
                }
                if (!payload.TryGetNumber(p.Name, out double v))
                { if (p.Required) return Dusur(s, "eksik alan: " + p.Name); continue; }
                if (p.BandKey != null)
                {
                    if (!bantlar.TryGetBand(p.BandKey, out double min, out double max))
                        return Dusur(s, "bant tanımsız: " + p.BandKey);
                    if (v < min || v > max) return Dusur(s, "bant dışı: " + p.Name);
                }
            }

            s.Sonuc = OneriSonucu.Oneri;
            s.ActionType = def.ActionType;
            s.Tier = def.Tier;                 // KATALOGDAN — modelden değil
            return s;
        }

        static IntentSuggestion Dusur(IntentSuggestion s, string sebep)
        {
            s.Sonuc = OneriSonucu.Dusuruldu;
            s.DusurmeSebebi = sebep;
            s.ActionType = null;
            return s;
        }

        /// <summary>Öneri kimliği — CB 7.4 zincirini bağlar. `Guid.NewGuid()` YASAK (determinizm):
        /// aynı girdi + aynı kayıt aynı kimliği verir, yani replay'de zincir yeniden kurulur.</summary>
        internal static Guid OneriKimligi(ulong girdiOzeti, ulong saveSeed)
        {
            ulong a = TheBadge.Sim.Determinism.Rng.Hash64(saveSeed, 1u, (uint)(girdiOzeti & 0xFFFFFFFF),
                                                          (uint)(girdiOzeti >> 32), 0x5C0Fu);
            ulong b = TheBadge.Sim.Determinism.Rng.Hash64(saveSeed, 1u, (uint)(girdiOzeti >> 32),
                                                          (uint)(girdiOzeti & 0xFFFFFFFF), 0x5C0Fu + 1u);
            var bytes = new byte[16];
            for (int i = 0; i < 8; i++) { bytes[i] = (byte)(a >> (i * 8)); bytes[8 + i] = (byte)(b >> (i * 8)); }
            return new Guid(bytes);
        }
    }
}
