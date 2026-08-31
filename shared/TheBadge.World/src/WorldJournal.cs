using System;
using System.Collections.Generic;

namespace TheBadge.World
{
    /// <summary>Mutasyon hedefi — journal girdisinin hangi durum parçasına yazdığı.</summary>
    public enum MutTarget : byte { Kulup = 0, Oyuncu = 1, Takvim = 2, Insaat = 3, Kredi = 4, Tesis = 5, Mac = 6, Fiyat = 7, Sponsor = 8, Taktik = 9, Preset = 10, Talimat = 11, TransferTeklif = 12, Lig = 13, Personel = 14 }

    public static class ClubField
    {
        public const byte Kasa = 1, StadyumKapasite = 2, HaftalikMaasGider = 3,
                          SponsorHaftalik = 4, Form = 5, SponsorKalanHafta = 6, DonemInsaatGideri = 7,
                          Kaptan = 8, AntrenmanPlan = 9, AntrenmanYogunluk = 10, AktifPremium = 11,
                          DonemTransferGideri = 12;
    }

    /// <summary>Fiyat alanları — `Index` slot (tribün 0-4 / ürün 0-2), değer KURUŞ.
    /// `Kombine` slot kullanmaz.</summary>
    public static class PriceField { public const byte Bilet = 1, Kombine = 2, Bufe = 3, Magaza = 4; }
    public static class PlayerField
    {
        public const byte ClubId = 1, HaftalikMaas = 2, SozlesmeKalanHafta = 3, Moral = 4,
                          Kondisyon = 5, SakatlikHafta = 6, RolId = 7, AnchorX = 8, AnchorY = 9, Listede = 10,
                          Guc = 11, Potansiyel = 12, Yas = 13, IstenenBedel = 14;
    }

    /// <summary>Personel alanları — CB 4.3.</summary>
    public static class StaffField { public const byte Tip = 1, Tier = 2, KalanHafta = 3; }

    /// <summary>Lig alanları — CB 4.4.</summary>
    public static class LeagueField
    {
        public const byte LigId = 1, Kurucu = 2, Chaos = 3, Hiz = 4, Butce = 5,
                          SaatDilimi = 6;
    }

    /// <summary>Transfer teklifi alanları — CB 4.3.</summary>
    public static class OfferField
    {
        public const byte TeklifId = 1, OyuncuId = 2, TeklifEden = 3, Bedel = 4, Maas = 5,
                          SonGecerlilikSezon = 6, SonGecerlilikHafta = 7, SiraTeklifEdende = 8, TurSayisi = 9;
    }
    public static class CalendarField { public const byte Sezon = 1, Hafta = 2, Pencere = 3; }
    public static class ConstructionField { public const byte InsaatId = 1, TesisId = 2, HedefTier = 3, KalanHafta = 4, ToplamMaliyet = 5; }
    public static class LoanField { public const byte KrediId = 1, Anapara = 2, KalanAy = 3, FaizBp = 4; }
    public static class FacilityField { public const byte Tier = 1; }
    public static class TacticField { public const byte Mentalite = 1, Tempo = 2, Pres = 3, Hat = 4; }
    /// <summary>Preset alanları — `Index` slot indeksi. `Ad` metin olduğu için journal'da DEĞİL:
    /// metin yazması `AdYaz` ile ayrı taşınır (journal tamsayı taşıyıcısıdır).</summary>
    public static class PresetField { public const byte Slot = 1, Mentalite = 2, Tempo = 3, Pres = 4, Hat = 5; }
    /// <summary>Talimat alanları — `Index` = oyuncuIndex * yuvaSayisi + yuvaIndex (düz adres).</summary>
    public static class InstructionField { public const byte TalimatId = 1, Deger = 2; }

    public static class SponsorField { public const byte TeklifId = 1, Haftalik = 2, Sure = 3, SonGecerlilik = 4, SonGecerlilikSezon = 5; }
    public static class MatchField { public const byte KalanDegisiklikHakki = 1; }

    /// <summary>Tek bir durum yazması. Journal girdileri TİPLİdir (kapanış/closure değil) ki
    /// hem uygulanabilsin hem de delta sync için serileştirilebilsin (CB 8.2).</summary>
    public readonly struct Mutation
    {
        public readonly MutTarget Target;
        public readonly int Index;      // oyuncu indeksi / slot indeksi / tesisId — hedefe göre
        public readonly byte Field;
        public readonly long Value;     // IsDelta ise ekleme, değilse mutlak değer
        public readonly bool IsDelta;

        public Mutation(MutTarget target, int index, byte field, long value, bool isDelta)
        { Target = target; Index = index; Field = field; Value = value; IsDelta = isDelta; }
    }

    /// <summary>YÜRÜTME JOURNAL'I — CB 5.2 atomikliğinin taşıyıcısı: "durum geçişi + event üretimi
    /// + audit kaydı ya birlikte kalıcı olur ya hiç olmaz".
    ///
    /// Handler durumu DOĞRUDAN değiştirmez; yazmalarını buraya kuyruklar. `WorldExecutor` yalnız
    /// handler `None` döndürdüğünde ve journal ÖN DENETİMDEN geçtiğinde uygular. Böylece yarım
    /// yazılmış durum yapısal olarak imkânsızdır — hata yolunda geri alınacak bir şey yoktur.</summary>
    public sealed class WorldJournal
    {
        readonly List<Mutation> yazmalar = new List<Mutation>();
        readonly List<WorldEvent> olaylar = new List<WorldEvent>();
        readonly List<long> geriDegerler = new List<long>();   // Apply öncesi değerler (geri alma)
        readonly List<(int slot, string ad)> adYazmalari = new List<(int, string)>();
        readonly List<string> adGeri = new List<string>();

        public int Count => yazmalar.Count + adYazmalari.Count;
        public IReadOnlyList<Mutation> Mutations => yazmalar;
        public IReadOnlyList<WorldEvent> Events => olaylar;

        /// <summary>Maç motoruna gidecek komutlar — journal'da BEKLETİLİR, doğrudan kuyruğa
        /// YAZILMAZ (inceleme bulgusu, P1). Handler `Apply` içinde kuyruğa yazsaydı, sonraki
        /// journal doğrulaması ya da denetim sink'i patladığında `Geri` yalnız `GameState`i
        /// geri alır; komut kuyrukta KALIR ve tekrar denemede İKİNCİ kopya girerdi. Artık
        /// yayınlama commit'in parçası: yürütücü, denetim de geçtikten SONRA boşaltır.</summary>
        readonly List<TheBadge.Sim.Match.MatchCommand> macKomutlari = new List<TheBadge.Sim.Match.MatchCommand>();
        public IReadOnlyList<TheBadge.Sim.Match.MatchCommand> MacKomutlari => macKomutlari;
        public void MacKomutu(TheBadge.Sim.Match.MatchCommand cmd) => macKomutlari.Add(cmd);

        /// <summary>Online yayınlar — maç komutlarıyla AYNI sözleşme: journal'da BEKLETİLİR,
        /// yürütücü denetim de geçtikten SONRA boşaltır. Klip paylaşımı ve oyuncu raporu geri
        /// alınamayan DIŞ etkilerdir; işlem yarıda kalırsa yayınlanmamaları gerekir.</summary>
        public struct OnlineYayin
        {
            public Guid CommandId;         // uzak tarafın dedup anahtarı
            public bool Klip;              // false = rapor
            public int MacId, PencereSn;
            public byte Kod;               // klip: hedef · rapor: sebep
            public long UserId, HedefUserId;
            public string Notlar;
        }
        readonly List<OnlineYayin> onlineYayinlar = new List<OnlineYayin>();
        public IReadOnlyList<OnlineYayin> OnlineYayinlar => onlineYayinlar;
        /// <summary>Persona kanalı — online yayınla AYNI sözleşme: journal'da bekletilir,
        /// yürütücü denetimden sonra boşaltır, patlarsa durum geri alınır.</summary>
        public struct PersonaYayin
        {
            public Guid CommandId;
            public bool Konusma;           // false = basın
            public int Id;                 // konuşma: personaId · basın: soruId
            public byte Kod;               // konuşma: ton · basın: cevap sınıfı
            public long UserId;
        }
        readonly List<PersonaYayin> personaYayinlar = new List<PersonaYayin>();
        public IReadOnlyList<PersonaYayin> PersonaYayinlar => personaYayinlar;
        public void PersonaKonusma(Guid cid, int personaId, byte ton, long userId)
            => personaYayinlar.Add(new PersonaYayin { CommandId = cid, Konusma = true, Id = personaId, Kod = ton, UserId = userId });
        public void PersonaBasin(Guid cid, int soruId, byte cevapSinifi, long userId)
            => personaYayinlar.Add(new PersonaYayin { CommandId = cid, Konusma = false, Id = soruId, Kod = cevapSinifi, UserId = userId });

        public void OnlineKlip(Guid commandId, int macId, int pencereSn, byte hedef, long userId)
            => onlineYayinlar.Add(new OnlineYayin { CommandId = commandId, Klip = true, MacId = macId, PencereSn = pencereSn, Kod = hedef, UserId = userId });
        public void OnlineRapor(Guid commandId, long hedefUserId, byte sebep, string notlar, long userId)
            => onlineYayinlar.Add(new OnlineYayin { CommandId = commandId, Klip = false, HedefUserId = hedefUserId, Kod = sebep, Notlar = notlar, UserId = userId });

        public void Clear() { yazmalar.Clear(); olaylar.Clear(); geriDegerler.Clear(); adYazmalari.Clear(); adGeri.Clear(); macKomutlari.Clear(); onlineYayinlar.Clear(); personaYayinlar.Clear(); }

        /// <summary>Preset ADI yazması — journal TAMSAYI taşıyıcısıdır, metin ayrı listede taşınır.
        /// Aralık denetimi yok (uzunluk kapı 1'de doğrulandı); geri alma için eski ad saklanır.</summary>
        public void PresetAd(int slotIndex, string ad) => adYazmalari.Add((slotIndex, ad));

        public void Set(MutTarget t, int index, byte field, long value) => yazmalar.Add(new Mutation(t, index, field, value, false));
        public void Add(MutTarget t, int index, byte field, long delta) => yazmalar.Add(new Mutation(t, index, field, delta, true));

        public void KasaDelta(long tl) => Add(MutTarget.Kulup, 0, ClubField.Kasa, tl);
        public void OyuncuSet(int oyuncuIndex, byte field, long value) => Set(MutTarget.Oyuncu, oyuncuIndex, field, value);

        /// <summary>Olay kaydı — TEK YÖNLÜdür: dünya mantığı bu listeyi ASLA OKUMAZ ve liste
        /// `WorldHash`e GİRMEZ (ME 15.1'de maç olay logu için kurulan kuralın aynısı). Sunum,
        /// bildirim ve telemetri içindir.</summary>
        public void Emit(WorldEvent e) => olaylar.Add(e);

        /// <summary>ÖN DENETİM — uygulamadan önce her yazmanın hedefi ve aralığı doğrulanır.
        /// Tek bir geçersiz yazma varsa HİÇBİRİ uygulanmaz (atomiklik). Aralık taşması sessizce
        /// kırpılmaz: byte alana 300 yazan bir handler hatası burada GÖRÜNÜR olur.
        ///
        /// ZİNCİRLEME (inceleme bulgusu, 2026-08-24 — HIGH): ilk sürüm her yazmayı DEĞİŞMEMİŞ
        /// duruma karşı denetliyordu, oysa `Apply` yazmaları SIRAYLA zincirliyor. Aynı alana iki
        /// delta (ör. moral +30, +30; taban 60) tek tek bakıldığında bantta görünür ama zincirde
        /// 120 yazardı — yani tam da bu metnin verdiği garanti deliniyordu. Artık her yazma,
        /// KENDİNDEN ÖNCEKİ aynı hedefli yazmalar katlandıktan sonraki değere karşı denetlenir;
        /// böylece her ARA sonuç da banttadır. Tarama O(n²)'dir; journal birkaç yazmalıktır
        /// (TeamSheet.Validate'te kurulan "küçük n için yeterli" precedent'i).</summary>
        public bool Validate(GameState st, out string hata)
        {
            for (int i = 0; i < yazmalar.Count; i++)
            {
                var m = yazmalar[i];
                if (!Hedef(st, m, out long taban, out long min, out long max, out hata)) return false;
                long mevcut = Katla(taban, i, m);
                long yeni = m.IsDelta ? mevcut + m.Value : m.Value;
                if (yeni < min || yeni > max)
                { hata = $"{m.Target}[{m.Index}].{m.Field} = {yeni} aralık dışı [{min},{max}]"; return false; }
            }
            hata = null;
            return true;
        }

        /// <summary>`i`'den ÖNCEKİ aynı hedefli yazmaları taban değere katlar.</summary>
        long Katla(long taban, int i, in Mutation m)
        {
            long deger = taban;
            for (int j = 0; j < i; j++)
            {
                var e = yazmalar[j];
                if (e.Target != m.Target || e.Index != m.Index || e.Field != m.Field) continue;
                deger = e.IsDelta ? deger + e.Value : e.Value;
            }
            return deger;
        }

        /// <summary>Journal'ı sırayla uygular ve `StateVersion`ı BİR artırır (komut başına tek
        /// versiyon — CB 8.2). Çağrılmadan önce `Validate` geçmiş olmalıdır. Her yazmanın ÖNCEKİ
        /// değeri saklanır ki `Geri` ile tam geri alınabilsin.</summary>
        public void Apply(GameState st)
        {
            geriDegerler.Clear();
            for (int i = 0; i < yazmalar.Count; i++)
            {
                var m = yazmalar[i];
                Hedef(st, m, out long mevcut, out _, out _, out _);
                geriDegerler.Add(mevcut);
                Yaz(st, m, m.IsDelta ? mevcut + m.Value : m.Value);
            }
            adGeri.Clear();
            for (int i = 0; i < adYazmalari.Count; i++)
            {
                var (slot, ad) = adYazmalari[i];
                adGeri.Add(st.Presetler[slot].Ad);
                st.Presetler[slot].Ad = ad;
            }
            st.StateVersion++;
        }

        /// <summary>`Apply`ı GERİ ALIR — yalnız başarılı bir `Apply`ın hemen ardından çağrılır.
        /// Yazmalar TERS sırayla eski değerlerine döndürülür (zincirleme yazmalar için sıra
        /// önemlidir) ve `StateVersion` geri alınır.
        ///
        /// Neden var (inceleme bulgusu, 2026-08-24): denetim kaydı yazımı (`IWorldAuditSink`)
        /// fırlatırsa host'un veritabanı transaction'ı geri alınır ama BELLEKTEKİ durum ilerlemiş
        /// kalırdı — "hep ya da hiç" sözleşmesinin bellek ayağı yoktu. Önceki yorumum bunu
        /// host'un geri almasına havale ediyordu; havale mekanizma değildir, bu odur.</summary>
        public void Geri(GameState st)
        {
            if (geriDegerler.Count != yazmalar.Count)
                throw new InvalidOperationException("Geri: eşleşen bir Apply yok.");
            for (int i = adYazmalari.Count - 1; i >= 0; i--) st.Presetler[adYazmalari[i].slot].Ad = adGeri[i];
            adGeri.Clear();
            for (int i = yazmalar.Count - 1; i >= 0; i--) Yaz(st, yazmalar[i], geriDegerler[i]);
            geriDegerler.Clear();
            st.StateVersion--;
        }

        /// <summary>Hedefin mevcut değerini ve geçerli aralığını verir. Aralıklar alanın
        /// DEPOLAMA tipinden gelir (yapısal sınır); ekonomik bantlar Kapı 2'nin işidir.</summary>
        static bool Hedef(GameState st, in Mutation m, out long mevcut, out long min, out long max, out string hata)
        {
            mevcut = 0; min = long.MinValue; max = long.MaxValue; hata = null;
            switch (m.Target)
            {
                case MutTarget.Kulup:
                    switch (m.Field)
                    {
                        case ClubField.Kasa: mevcut = st.Club.KasaTl; return true;
                        case ClubField.StadyumKapasite: mevcut = st.Club.StadyumKapasite; min = 0; max = int.MaxValue; return true;
                        case ClubField.HaftalikMaasGider: mevcut = st.Club.HaftalikMaasGiderTl; min = 0; return true;
                        case ClubField.SponsorHaftalik: mevcut = st.Club.SponsorHaftalikTl; min = 0; return true;
                        case ClubField.Form: mevcut = st.Club.Form; min = 0; max = 100; return true;
                        case ClubField.SponsorKalanHafta: mevcut = st.Club.SponsorKalanHafta; min = 0; max = ushort.MaxValue; return true;
                        case ClubField.DonemInsaatGideri: mevcut = st.Club.DonemInsaatGideriTl; return true;
                        case ClubField.DonemTransferGideri: mevcut = st.Club.DonemTransferGideriTl; return true;
                        case ClubField.Kaptan: mevcut = st.Club.KaptanPlayerId; min = 0; max = int.MaxValue; return true;
                        case ClubField.AntrenmanPlan: mevcut = st.Club.AntrenmanPlanId; min = 0; max = 255; return true;
                        case ClubField.AntrenmanYogunluk: mevcut = st.Club.AntrenmanYogunluk; min = 0; max = 255; return true;
                        case ClubField.AktifPremium: mevcut = st.Club.AktifPremiumId; min = 0; max = int.MaxValue; return true;
                    }
                    break;
                case MutTarget.Fiyat:
                    switch (m.Field)
                    {
                        case PriceField.Bilet:
                            if (m.Index < 0 || m.Index >= st.Fiyat.BiletKurus.Length) { hata = "tribün indeksi kapsam dışı"; return false; }
                            mevcut = st.Fiyat.BiletKurus[m.Index]; min = 0; max = int.MaxValue; return true;
                        case PriceField.Kombine: mevcut = st.Fiyat.KombineKurus; min = 0; max = int.MaxValue; return true;
                        case PriceField.Bufe:
                            if (m.Index < 0 || m.Index >= st.Fiyat.BufeKurus.Length) { hata = "büfe ürün indeksi kapsam dışı"; return false; }
                            mevcut = st.Fiyat.BufeKurus[m.Index]; min = 0; max = int.MaxValue; return true;
                        case PriceField.Magaza:
                            if (m.Index < 0 || m.Index >= st.Fiyat.MagazaKurus.Length) { hata = "mağaza ürün indeksi kapsam dışı"; return false; }
                            mevcut = st.Fiyat.MagazaKurus[m.Index]; min = 0; max = int.MaxValue; return true;
                    }
                    break;
                case MutTarget.Oyuncu:
                    if (m.Index < 0 || m.Index >= st.Oyuncular.Length) { hata = "oyuncu indeksi kapsam dışı"; return false; }
                    switch (m.Field)
                    {
                        case PlayerField.ClubId: mevcut = st.Oyuncular[m.Index].ClubId; min = 0; return true;
                        case PlayerField.HaftalikMaas: mevcut = st.Oyuncular[m.Index].HaftalikMaasTl; min = 0; return true;
                        case PlayerField.SozlesmeKalanHafta: mevcut = st.Oyuncular[m.Index].SozlesmeKalanHafta; min = 0; max = ushort.MaxValue; return true;
                        case PlayerField.Moral: mevcut = st.Oyuncular[m.Index].Moral; min = 0; max = 100; return true;
                        case PlayerField.Kondisyon: mevcut = st.Oyuncular[m.Index].Kondisyon; min = 0; max = 100; return true;
                        case PlayerField.SakatlikHafta: mevcut = st.Oyuncular[m.Index].SakatlikHafta; min = 0; max = byte.MaxValue; return true;
                        case PlayerField.RolId: mevcut = st.Oyuncular[m.Index].RolId; min = 0; max = byte.MaxValue; return true;
                        case PlayerField.AnchorX: mevcut = st.Oyuncular[m.Index].AnchorXmm; min = int.MinValue; max = int.MaxValue; return true;
                        case PlayerField.AnchorY: mevcut = st.Oyuncular[m.Index].AnchorYmm; min = int.MinValue; max = int.MaxValue; return true;
                        case PlayerField.Listede: mevcut = st.Oyuncular[m.Index].ListedeMi ? 1 : 0; min = 0; max = 1; return true;
                        case PlayerField.Guc: mevcut = st.Oyuncular[m.Index].Guc; min = 0; max = 100; return true;
                        case PlayerField.Potansiyel: mevcut = st.Oyuncular[m.Index].Potansiyel; min = 0; max = 100; return true;
                        case PlayerField.Yas: mevcut = st.Oyuncular[m.Index].Yas; min = 0; max = byte.MaxValue; return true;
                        case PlayerField.IstenenBedel: mevcut = st.Oyuncular[m.Index].IstenenBedelTl; min = 0; return true;
                    }
                    break;
                case MutTarget.Personel:
                    if (m.Index < 0 || m.Index >= st.Club.Personel.Length) { hata = "personel slotu kapsam dışı"; return false; }
                    switch (m.Field)
                    {
                        case StaffField.Tip: mevcut = st.Club.Personel[m.Index].Tip; min = 0; max = 255; return true;
                        case StaffField.Tier: mevcut = st.Club.Personel[m.Index].Tier; min = 0; max = 255; return true;
                        case StaffField.KalanHafta: mevcut = st.Club.Personel[m.Index].KalanHafta; min = 0; max = ushort.MaxValue; return true;
                    }
                    break;
                case MutTarget.Lig:
                    switch (m.Field)
                    {
                        case LeagueField.LigId: mevcut = st.Lig.LigId; min = 0; max = int.MaxValue; return true;
                        case LeagueField.Kurucu: mevcut = st.Lig.KurucuUserId; min = 0; return true;
                        case LeagueField.Chaos: mevcut = st.Lig.Chaos; min = 0; max = 255; return true;
                        case LeagueField.Hiz: mevcut = st.Lig.Hiz; min = 0; max = 255; return true;
                        case LeagueField.Butce: mevcut = st.Lig.ButceTl; min = 0; return true;
                        case LeagueField.SaatDilimi: mevcut = st.Lig.SaatDilimi; min = short.MinValue; max = short.MaxValue; return true;
                    }
                    break;
                case MutTarget.TransferTeklif:
                    if (m.Index < 0 || m.Index >= st.Club.TransferTeklifleri.Length) { hata = "transfer teklif slotu kapsam dışı"; return false; }
                    switch (m.Field)
                    {
                        case OfferField.TeklifId: mevcut = st.Club.TransferTeklifleri[m.Index].TeklifId; min = 0; max = int.MaxValue; return true;
                        case OfferField.OyuncuId: mevcut = st.Club.TransferTeklifleri[m.Index].OyuncuId; min = 0; max = int.MaxValue; return true;
                        case OfferField.TeklifEden: mevcut = st.Club.TransferTeklifleri[m.Index].TeklifEdenClubId; min = 0; return true;
                        case OfferField.Bedel: mevcut = st.Club.TransferTeklifleri[m.Index].BedelTl; min = 0; return true;
                        case OfferField.Maas: mevcut = st.Club.TransferTeklifleri[m.Index].HaftalikMaasTl; min = 0; return true;
                        case OfferField.SonGecerlilikSezon: mevcut = st.Club.TransferTeklifleri[m.Index].SonGecerlilikSezon; min = 0; max = ushort.MaxValue; return true;
                        case OfferField.SonGecerlilikHafta: mevcut = st.Club.TransferTeklifleri[m.Index].SonGecerlilikHafta; min = 0; max = ushort.MaxValue; return true;
                        case OfferField.SiraTeklifEdende: mevcut = st.Club.TransferTeklifleri[m.Index].SiraTeklifEdende ? 1 : 0; min = 0; max = 1; return true;
                        case OfferField.TurSayisi: mevcut = st.Club.TransferTeklifleri[m.Index].TurSayisi; min = 0; max = byte.MaxValue; return true;
                    }
                    break;
                case MutTarget.Takvim:
                    switch (m.Field)
                    {
                        case CalendarField.Sezon: mevcut = st.Takvim.Sezon; min = 0; max = ushort.MaxValue; return true;
                        case CalendarField.Hafta: mevcut = st.Takvim.Hafta; min = 0; max = ushort.MaxValue; return true;
                        case CalendarField.Pencere: mevcut = (byte)st.Takvim.Pencere; min = 0; max = 2; return true;
                    }
                    break;
                case MutTarget.Insaat:
                    if (m.Index < 0 || m.Index >= st.Club.InsaatSlot.Length) { hata = "inşaat slotu kapsam dışı"; return false; }
                    switch (m.Field)
                    {
                        case ConstructionField.InsaatId: mevcut = st.Club.InsaatSlot[m.Index].InsaatId; min = 0; max = int.MaxValue; return true;
                        case ConstructionField.TesisId: mevcut = st.Club.InsaatSlot[m.Index].TesisId; min = 0; max = int.MaxValue; return true;
                        case ConstructionField.HedefTier: mevcut = st.Club.InsaatSlot[m.Index].HedefTier; min = 0; max = byte.MaxValue; return true;
                        case ConstructionField.KalanHafta: mevcut = st.Club.InsaatSlot[m.Index].KalanHafta; min = 0; max = ushort.MaxValue; return true;
                        case ConstructionField.ToplamMaliyet: mevcut = st.Club.InsaatSlot[m.Index].ToplamMaliyetTl; min = 0; return true;
                    }
                    break;
                case MutTarget.Kredi:
                    if (m.Index < 0 || m.Index >= st.Club.Krediler.Length) { hata = "kredi slotu kapsam dışı"; return false; }
                    switch (m.Field)
                    {
                        case LoanField.KrediId: mevcut = st.Club.Krediler[m.Index].KrediId; min = 0; max = int.MaxValue; return true;
                        case LoanField.Anapara: mevcut = st.Club.Krediler[m.Index].AnaparaTl; min = 0; return true;
                        case LoanField.KalanAy: mevcut = st.Club.Krediler[m.Index].KalanAy; min = 0; max = ushort.MaxValue; return true;
                        case LoanField.FaizBp: mevcut = st.Club.Krediler[m.Index].FaizBp; min = 0; max = ushort.MaxValue; return true;
                    }
                    break;
                case MutTarget.Tesis:
                    if (m.Index < 0 || m.Index >= st.Club.TesisTier.Length) { hata = "tesis indeksi kapsam dışı"; return false; }
                    if (m.Field == FacilityField.Tier) { mevcut = st.Club.TesisTier[m.Index]; min = 0; max = byte.MaxValue; return true; }
                    break;
                case MutTarget.Taktik:
                    switch (m.Field)
                    {
                        case TacticField.Mentalite: mevcut = st.Taktik.Mentalite; min = 0; max = 255; return true;
                        case TacticField.Tempo: mevcut = st.Taktik.Tempo; min = 0; max = 255; return true;
                        case TacticField.Pres: mevcut = st.Taktik.Pres; min = 0; max = 255; return true;
                        case TacticField.Hat: mevcut = st.Taktik.Hat; min = 0; max = 255; return true;
                    }
                    break;
                case MutTarget.Preset:
                    if (m.Index < 0 || m.Index >= st.Presetler.Length) { hata = "preset slotu kapsam dışı"; return false; }
                    switch (m.Field)
                    {
                        case PresetField.Slot: mevcut = st.Presetler[m.Index].Slot; min = 0; max = 255; return true;
                        case PresetField.Mentalite: mevcut = st.Presetler[m.Index].Mentalite; min = 0; max = 255; return true;
                        case PresetField.Tempo: mevcut = st.Presetler[m.Index].Tempo; min = 0; max = 255; return true;
                        case PresetField.Pres: mevcut = st.Presetler[m.Index].Pres; min = 0; max = 255; return true;
                        case PresetField.Hat: mevcut = st.Presetler[m.Index].Hat; min = 0; max = 255; return true;
                    }
                    break;
                case MutTarget.Talimat:
                    {
                        int yuva = st.Oyuncular.Length > 0 && st.Oyuncular[0].Talimatlar != null ? st.Oyuncular[0].Talimatlar.Length : 0;
                        if (yuva <= 0) { hata = "talimat yuvası tanımsız"; return false; }
                        int oi = m.Index / yuva, yi = m.Index % yuva;
                        if (oi < 0 || oi >= st.Oyuncular.Length) { hata = "talimat oyuncu indeksi kapsam dışı"; return false; }
                        switch (m.Field)
                        {
                            case InstructionField.TalimatId: mevcut = st.Oyuncular[oi].Talimatlar[yi].TalimatId; min = 0; max = 255; return true;
                            case InstructionField.Deger: mevcut = st.Oyuncular[oi].Talimatlar[yi].Deger; min = 0; max = 255; return true;
                        }
                        break;
                    }
                case MutTarget.Sponsor:
                    if (m.Index < 0 || m.Index >= st.Club.SponsorTeklifleri.Length) { hata = "sponsor teklif slotu kapsam dışı"; return false; }
                    switch (m.Field)
                    {
                        case SponsorField.TeklifId: mevcut = st.Club.SponsorTeklifleri[m.Index].TeklifId; min = 0; max = int.MaxValue; return true;
                        case SponsorField.Haftalik: mevcut = st.Club.SponsorTeklifleri[m.Index].HaftalikTl; min = 0; return true;
                        case SponsorField.Sure: mevcut = st.Club.SponsorTeklifleri[m.Index].SureHafta; min = 0; max = ushort.MaxValue; return true;
                        case SponsorField.SonGecerlilik: mevcut = st.Club.SponsorTeklifleri[m.Index].SonGecerlilikHafta; min = 0; max = ushort.MaxValue; return true;
                        case SponsorField.SonGecerlilikSezon: mevcut = st.Club.SponsorTeklifleri[m.Index].SonGecerlilikSezon; min = 0; max = ushort.MaxValue; return true;
                    }
                    break;
                case MutTarget.Mac:
                    if (m.Field == MatchField.KalanDegisiklikHakki) { mevcut = st.KalanDegisiklikHakki; min = 0; max = byte.MaxValue; return true; }
                    break;
            }
            hata = $"tanımsız alan: {m.Target}.{m.Field}";
            return false;
        }

        static void Yaz(GameState st, in Mutation m, long v)
        {
            switch (m.Target)
            {
                case MutTarget.Kulup:
                    if (m.Field == ClubField.Kasa) st.Club.KasaTl = v;
                    else if (m.Field == ClubField.StadyumKapasite) st.Club.StadyumKapasite = (int)v;
                    else if (m.Field == ClubField.HaftalikMaasGider) st.Club.HaftalikMaasGiderTl = v;
                    else if (m.Field == ClubField.SponsorHaftalik) st.Club.SponsorHaftalikTl = v;
                    else if (m.Field == ClubField.SponsorKalanHafta) st.Club.SponsorKalanHafta = (ushort)v;
                    else if (m.Field == ClubField.DonemInsaatGideri) st.Club.DonemInsaatGideriTl = v;
                    else if (m.Field == ClubField.DonemTransferGideri) st.Club.DonemTransferGideriTl = v;
                    else if (m.Field == ClubField.Kaptan) st.Club.KaptanPlayerId = (int)v;
                    else if (m.Field == ClubField.AntrenmanPlan) st.Club.AntrenmanPlanId = (byte)v;
                    else if (m.Field == ClubField.AntrenmanYogunluk) st.Club.AntrenmanYogunluk = (byte)v;
                    else if (m.Field == ClubField.AktifPremium) st.Club.AktifPremiumId = (int)v;
                    else if (m.Field == ClubField.Form) st.Club.Form = (byte)v;
                    // YAKALA-HEPSİNİ `else` KALDIRILDI: zinciri `else st.Club.Form = v` ile
                    // bitirmek, ARALIK DENETİMİNDEN GEÇEN ama uygulama zincirine eklenmemiş her
                    // yeni alanı SESSİZCE Form'a yazıyordu. `AktifPremium` eklenince tam bunu
                    // yaptı: aktivasyon izi yazılmadı, kulüp formu bozuldu ve komut BAŞARILI
                    // döndü. Bilinmeyen alan artık PATLAR — journal'ın aralık denetleyebildiği
                    // ama uygulayamadığı bir alan bir KOD hatasıdır, sessiz kalamaz.
                    else throw new InvalidOperationException($"WorldJournal: ClubField {m.Field} uygulanamıyor");
                    break;
                case MutTarget.Fiyat:
                    if (m.Field == PriceField.Bilet) st.Fiyat.BiletKurus[m.Index] = (int)v;
                    else if (m.Field == PriceField.Kombine) st.Fiyat.KombineKurus = (int)v;
                    else if (m.Field == PriceField.Bufe) st.Fiyat.BufeKurus[m.Index] = (int)v;
                    else st.Fiyat.MagazaKurus[m.Index] = (int)v;
                    break;
                case MutTarget.Oyuncu:
                    switch (m.Field)
                    {
                        case PlayerField.ClubId: st.Oyuncular[m.Index].ClubId = v; break;
                        case PlayerField.Guc: st.Oyuncular[m.Index].Guc = (byte)v; break;
                        case PlayerField.Potansiyel: st.Oyuncular[m.Index].Potansiyel = (byte)v; break;
                        case PlayerField.Yas: st.Oyuncular[m.Index].Yas = (byte)v; break;
                        case PlayerField.IstenenBedel: st.Oyuncular[m.Index].IstenenBedelTl = v; break;
                        case PlayerField.HaftalikMaas: st.Oyuncular[m.Index].HaftalikMaasTl = v; break;
                        case PlayerField.SozlesmeKalanHafta: st.Oyuncular[m.Index].SozlesmeKalanHafta = (ushort)v; break;
                        case PlayerField.Moral: st.Oyuncular[m.Index].Moral = (byte)v; break;
                        case PlayerField.Kondisyon: st.Oyuncular[m.Index].Kondisyon = (byte)v; break;
                        case PlayerField.SakatlikHafta: st.Oyuncular[m.Index].SakatlikHafta = (byte)v; break;
                        case PlayerField.RolId: st.Oyuncular[m.Index].RolId = (byte)v; break;
                        case PlayerField.AnchorX: st.Oyuncular[m.Index].AnchorXmm = (int)v; break;
                        case PlayerField.AnchorY: st.Oyuncular[m.Index].AnchorYmm = (int)v; break;
                        case PlayerField.Listede: st.Oyuncular[m.Index].ListedeMi = v != 0; break;
                    }
                    break;
                case MutTarget.Takvim:
                    if (m.Field == CalendarField.Sezon) st.Takvim.Sezon = (ushort)v;
                    else if (m.Field == CalendarField.Hafta) st.Takvim.Hafta = (ushort)v;
                    else st.Takvim.Pencere = (TransferWindow)(byte)v;
                    break;
                case MutTarget.Insaat:
                    switch (m.Field)
                    {
                        case ConstructionField.InsaatId: st.Club.InsaatSlot[m.Index].InsaatId = (int)v; break;
                        case ConstructionField.TesisId: st.Club.InsaatSlot[m.Index].TesisId = (int)v; break;
                        case ConstructionField.HedefTier: st.Club.InsaatSlot[m.Index].HedefTier = (byte)v; break;
                        case ConstructionField.KalanHafta: st.Club.InsaatSlot[m.Index].KalanHafta = (ushort)v; break;
                        case ConstructionField.ToplamMaliyet: st.Club.InsaatSlot[m.Index].ToplamMaliyetTl = v; break;
                    }
                    break;
                case MutTarget.Kredi:
                    switch (m.Field)
                    {
                        case LoanField.KrediId: st.Club.Krediler[m.Index].KrediId = (int)v; break;
                        case LoanField.Anapara: st.Club.Krediler[m.Index].AnaparaTl = v; break;
                        case LoanField.KalanAy: st.Club.Krediler[m.Index].KalanAy = (ushort)v; break;
                        case LoanField.FaizBp: st.Club.Krediler[m.Index].FaizBp = (ushort)v; break;
                    }
                    break;
                case MutTarget.Tesis: st.Club.TesisTier[m.Index] = (byte)v; break;
                case MutTarget.Taktik:
                    if (m.Field == TacticField.Mentalite) st.Taktik.Mentalite = (byte)v;
                    else if (m.Field == TacticField.Tempo) st.Taktik.Tempo = (byte)v;
                    else if (m.Field == TacticField.Pres) st.Taktik.Pres = (byte)v;
                    else st.Taktik.Hat = (byte)v;
                    break;
                case MutTarget.Preset:
                    switch (m.Field)
                    {
                        case PresetField.Slot: st.Presetler[m.Index].Slot = (byte)v; break;
                        case PresetField.Mentalite: st.Presetler[m.Index].Mentalite = (byte)v; break;
                        case PresetField.Tempo: st.Presetler[m.Index].Tempo = (byte)v; break;
                        case PresetField.Pres: st.Presetler[m.Index].Pres = (byte)v; break;
                        case PresetField.Hat: st.Presetler[m.Index].Hat = (byte)v; break;
                    }
                    break;
                case MutTarget.Talimat:
                    {
                        int yuva = st.Oyuncular[0].Talimatlar.Length;
                        int oi = m.Index / yuva, yi = m.Index % yuva;
                        if (m.Field == InstructionField.TalimatId) st.Oyuncular[oi].Talimatlar[yi].TalimatId = (byte)v;
                        else st.Oyuncular[oi].Talimatlar[yi].Deger = (byte)v;
                        break;
                    }
                case MutTarget.Personel:
                    switch (m.Field)
                    {
                        case StaffField.Tip: st.Club.Personel[m.Index].Tip = (byte)v; break;
                        case StaffField.Tier: st.Club.Personel[m.Index].Tier = (byte)v; break;
                        case StaffField.KalanHafta: st.Club.Personel[m.Index].KalanHafta = (ushort)v; break;
                    }
                    break;
                case MutTarget.Lig:
                    switch (m.Field)
                    {
                        case LeagueField.LigId: st.Lig.LigId = (int)v; break;
                        case LeagueField.Kurucu: st.Lig.KurucuUserId = v; break;
                        case LeagueField.Chaos: st.Lig.Chaos = (byte)v; break;
                        case LeagueField.Hiz: st.Lig.Hiz = (byte)v; break;
                        case LeagueField.Butce: st.Lig.ButceTl = v; break;
                        case LeagueField.SaatDilimi: st.Lig.SaatDilimi = (short)v; break;
                    }
                    break;
                case MutTarget.TransferTeklif:
                    switch (m.Field)
                    {
                        case OfferField.TeklifId: st.Club.TransferTeklifleri[m.Index].TeklifId = (int)v; break;
                        case OfferField.OyuncuId: st.Club.TransferTeklifleri[m.Index].OyuncuId = (int)v; break;
                        case OfferField.TeklifEden: st.Club.TransferTeklifleri[m.Index].TeklifEdenClubId = v; break;
                        case OfferField.Bedel: st.Club.TransferTeklifleri[m.Index].BedelTl = v; break;
                        case OfferField.Maas: st.Club.TransferTeklifleri[m.Index].HaftalikMaasTl = v; break;
                        case OfferField.SonGecerlilikSezon: st.Club.TransferTeklifleri[m.Index].SonGecerlilikSezon = (ushort)v; break;
                        case OfferField.SonGecerlilikHafta: st.Club.TransferTeklifleri[m.Index].SonGecerlilikHafta = (ushort)v; break;
                        case OfferField.SiraTeklifEdende: st.Club.TransferTeklifleri[m.Index].SiraTeklifEdende = v != 0; break;
                        case OfferField.TurSayisi: st.Club.TransferTeklifleri[m.Index].TurSayisi = (byte)v; break;
                    }
                    break;
                case MutTarget.Sponsor:
                    switch (m.Field)
                    {
                        case SponsorField.TeklifId: st.Club.SponsorTeklifleri[m.Index].TeklifId = (int)v; break;
                        case SponsorField.Haftalik: st.Club.SponsorTeklifleri[m.Index].HaftalikTl = v; break;
                        case SponsorField.Sure: st.Club.SponsorTeklifleri[m.Index].SureHafta = (ushort)v; break;
                        case SponsorField.SonGecerlilik: st.Club.SponsorTeklifleri[m.Index].SonGecerlilikHafta = (ushort)v; break;
                        case SponsorField.SonGecerlilikSezon: st.Club.SponsorTeklifleri[m.Index].SonGecerlilikSezon = (ushort)v; break;
                    }
                    break;
                case MutTarget.Mac: st.KalanDegisiklikHakki = (byte)v; break;
            }
        }
    }
}
