using System;
using System.Collections.Generic;

namespace TheBadge.World
{
    /// <summary>Mutasyon hedefi — journal girdisinin hangi durum parçasına yazdığı.</summary>
    public enum MutTarget : byte { Kulup = 0, Oyuncu = 1, Takvim = 2, Insaat = 3, Kredi = 4, Tesis = 5, Mac = 6, Fiyat = 7, Sponsor = 8 }

    public static class ClubField
    {
        public const byte Kasa = 1, StadyumKapasite = 2, HaftalikMaasGider = 3,
                          SponsorHaftalik = 4, Form = 5, SponsorKalanHafta = 6, DonemInsaatGideri = 7;
    }

    /// <summary>Fiyat alanları — `Index` slot (tribün 0-4 / ürün 0-2), değer KURUŞ.
    /// `Kombine` slot kullanmaz.</summary>
    public static class PriceField { public const byte Bilet = 1, Kombine = 2, Bufe = 3, Magaza = 4; }
    public static class PlayerField
    {
        public const byte ClubId = 1, HaftalikMaas = 2, SozlesmeKalanHafta = 3, Moral = 4,
                          Kondisyon = 5, SakatlikHafta = 6, RolId = 7, AnchorX = 8, AnchorY = 9, Listede = 10;
    }
    public static class CalendarField { public const byte Sezon = 1, Hafta = 2, Pencere = 3; }
    public static class ConstructionField { public const byte InsaatId = 1, TesisId = 2, HedefTier = 3, KalanHafta = 4, ToplamMaliyet = 5; }
    public static class LoanField { public const byte KrediId = 1, Anapara = 2, KalanAy = 3, FaizBp = 4; }
    public static class FacilityField { public const byte Tier = 1; }
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

        public int Count => yazmalar.Count;
        public IReadOnlyList<Mutation> Mutations => yazmalar;
        public IReadOnlyList<WorldEvent> Events => olaylar;

        public void Clear() { yazmalar.Clear(); olaylar.Clear(); geriDegerler.Clear(); }

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
                    else st.Club.Form = (byte)v;
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
