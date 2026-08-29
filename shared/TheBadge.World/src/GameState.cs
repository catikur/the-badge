using System;

namespace TheBadge.World
{
    /// <summary>Transfer penceresi durumu — CB 5 Kapı 3 "pencere açık mı" denetiminin kaynağı.</summary>
    public enum TransferWindow : byte { Kapali = 0, Yaz = 1, Kis = 2 }

    /// <summary>Tesis inşaat kaydı — CB 4.1 `tycoon.start_construction` / `cancel_construction`
    /// için Kapı 3'ün "inşaat slotu boş mu" denetimini besler (CB 8.2: aynı tesise iki inşaat
    /// StateConflict'tir, "sessiz üzerine yazma" yoktur).</summary>
    public struct Construction
    {
        public int InsaatId;        // 0 = boş slot
        public int TesisId;
        public byte HedefTier;
        public ushort KalanHafta;
        public long ToplamMaliyetTl;
    }

    /// <summary>Kredi kaydı — GDD 4.4 "Banka Kredisi". Tutarlar TAMSAYI ₺ (kuruş disiplini:
    /// kalıcı durumda float YOK; ME 3.2'nin mm kuralının ekonomi karşılığı).</summary>
    public struct Loan
    {
        public int KrediId;         // 0 = boş slot
        public long AnaparaTl;      // kalan borç
        public ushort KalanAy;
        public ushort FaizBp;       // yıllık faiz, baz puan (100 bp = %1) — tamsayı
    }

    /// <summary>Sponsor teklifi — GDD 4.2 "Sponsorluk Anlaşmaları". `tycoon.sign_sponsor`
    /// bunlardan birini seçer. Teklifler K5/LiveOps tarafından doldurulur; K3 imzalamayı yürütür.</summary>
    public struct SponsorOffer
    {
        public int TeklifId;          // 0 = boş slot
        public long HaftalikTl;
        public ushort SureHafta;      // sözleşme süresi
        /// <summary>Son geçerlilik (sezon, hafta). SEZON de tutulur: yalnız hafta karşılaştırmak
        /// sezon dönüşünde takvim 1'e sarınca süresi geçmiş teklifi YENİDEN geçerli kılıyordu
        /// (inceleme bulgusu, 2026-08-29). 0 = süresiz.</summary>
        public ushort SonGecerlilikSezon;
        public ushort SonGecerlilikHafta;
    }

    /// <summary>Kulüp durumu — GDD 4 (Tycoon) + 4.4 (finans). TÜM kalıcı alanlar tamsayıdır.</summary>
    public sealed class ClubState
    {
        public long ClubId;
        public long OwnerUserId;            // sahiplik: Kapı 3 "bu kulüp bu kullanıcının mı"
        public long KasaTl;                 // ₺ (tam sayı). ECONOMY_MAP'in ₺K'sı SUNUM birimidir.
        public int StadyumKapasite;
        public byte[] TesisTier;            // tesisId → tier (1-5); index 0 kullanılmaz
        public Construction[] InsaatSlot;   // eşzamanlı inşaat slotları [KALİBRE: world.balance]
        public Loan[] Krediler;             // eşzamanlı kredi slotları [KALİBRE]
        public long HaftalikMaasGiderTl;    // türetilmiş değil, YAZILAN alan (hash içi)
        public long SponsorHaftalikTl;      // aktif sponsor sözleşmesi (K3-B `sign_sponsor` yazar)
        /// <summary>Aktif sponsor sözleşmesinin KALAN haftası. Olmadan sözleşme süresi imzada
        /// kayboluyor ve 1 haftalık anlaşma sonsuza dek ödeme yapıyordu (inceleme bulgusu).</summary>
        public ushort SponsorKalanHafta;
        /// <summary>Bu haftaya ait, KOMUTLA yapılmış inşaat harcaması (iptal iadesi negatif).
        /// Haftalık tick bunu `WeekLedger.InsaatTl`e boşaltır ve sıfırlar. Olmadan inşaat
        /// harcaması hiçbir sink kalemine girmiyordu (inceleme bulgusu, P1) — oysa ECONOMY_MAP
        /// "inşaat + tesis bakımı"nı açıkça sink sayıyor.</summary>
        public long DonemInsaatGideriTl;
        public SponsorOffer[] SponsorTeklifleri;
        public byte Form;                   // 0-100 — seyirci modelinin form ayağı (maç sonuçları besler)
    }

    /// <summary>Oyuncu durumu — GDD 3 (kadro) + ME 5.2 (TeamSheet'e beslenen taban).
    /// `Anchor` mm cinsindendir (ME 5.3) — maç motoruyla AYNI birim, dönüşüm yok.</summary>
    public struct PlayerState
    {
        public int PlayerId;                // kanonik kimlik; dizi PlayerId'ye göre SIRALI tutulur
        public long ClubId;                 // 0 = serbest oyuncu
        public long HaftalikMaasTl;
        public ushort SozlesmeKalanHafta;
        public byte Moral;                  // 0-100
        public byte Kondisyon;              // 0-100
        public byte SakatlikHafta;          // 0 = sağlam
        public byte RolId;                  // GDD 3.2 bireysel rol
        public int AnchorXmm, AnchorYmm;    // GDD 3.1 serbest pozisyonlama (ME 5.3 birimi)
        public bool ListedeMi;              // transfer listesi
    }

    /// <summary>Fiyat durumu — GDD 4.2 gelir kaynakları. TÜM fiyatlar KURUŞ cinsindendir
    /// (1 ₺ = 100 kuruş): `command.bands.json` büfe fiyatını [0,5 - 50] ₺ bandıyla tanımlıyor,
    /// yani kesirli fiyat meşru; kalıcı durum ise tamsayı olmak zorunda (ME 3.2 disiplini).
    /// Tek birim seçildi — bilet tam ₺, büfe kuruş olsaydı dönüşüm hatası kaçınılmazdı.</summary>
    public sealed class PricingState
    {
        public int[] BiletKurus;    // [5] kuzey, guney, dogu, bati, vip
        public int KombineKurus;
        public int[] BufeKurus;     // [3] yiyecek, icecek, atistirmalik
        public int[] MagazaKurus;   // [3] forma, atki, hatira
    }

    /// <summary>Takvim — sezon/hafta ve transfer penceresi. Maç fikstürü K6'da (online) bağlanır;
    /// K2 yalnız Kapı 3'ün ihtiyaç duyduğu zaman eksenini tutar.</summary>
    public sealed class CalendarState
    {
        public ushort Sezon;
        public ushort Hafta;                // 1..sezonHaftaSayisi
        public TransferWindow Pencere;
    }

    /// <summary>DÜNYA DURUMU — FAZ 04 K2. Maç dışı her şeyin tek kaynağı.
    ///
    /// TEK KAPI (CLAUDE.md değişmez #1): bu nesnenin alanları dışarıdan DOĞRUDAN değiştirilmez;
    /// tek meşru yol `WorldExecutor` üzerinden gelen `WorldJournal`dır. Alanların public olması
    /// host'un durumu OKUYUP serileştirebilmesi içindir (kalıcılık katmanı K6); yazma yolu
    /// yalnız `ApplyJournal`dır ve `StateVersion`ı o artırır (CB 8.2).
    ///
    /// DETERMİNİZM (CB 5.2 "aynı durum + aynı komut = aynı sonuç"): kalıcı alanlar TAMSAYIdır,
    /// diziler KANONİK sıradadır (oyuncular PlayerId'ye göre artan), sırasız yapı (Dictionary/
    /// HashSet) kullanılmaz — arama ikili aramadır.</summary>
    public sealed class GameState
    {
        public ClubState Club;
        public PlayerState[] Oyuncular;     // PlayerId'ye göre ARTAN sıralı (kanonik)
        public CalendarState Takvim;
        public PricingState Fiyat;

        /// <summary>CB 8.2: her yanıt `newStateVersion` döndürür; istemci eski versiyonla ekran
        /// gösteriyorsa delta sync tetiklenir. Yalnız `ApplyJournal` artırır.</summary>
        public ulong StateVersion;

        /// <summary>Maç içi değişiklik hakkı — CB 4.2 `match.substitute` Kapı 3'ünün
        /// (`NoChargesLeft`) kaynağı. Maç başında `Reset` ile doldurulur (K4/K6).</summary>
        public byte KalanDegisiklikHakki;

        public static GameState Bos() => new GameState
        {
            Club = new ClubState { TesisTier = new byte[0], InsaatSlot = new Construction[0], Krediler = new Loan[0],
                                   SponsorTeklifleri = new SponsorOffer[0] },
            Oyuncular = new PlayerState[0],
            Takvim = new CalendarState(),
            Fiyat = BosFiyat(),
        };

        static PricingState BosFiyat() => new PricingState
        { BiletKurus = new int[5], KombineKurus = 0, BufeKurus = new int[3], MagazaKurus = new int[3] };

        /// <summary>Boyutları YAPILANDIRMADAN alan kurulum — slot sayıları kodda sabit değildir
        /// (`world.balance.json` → yapi.*). Kadro ve kasa çağıran tarafından doldurulur.</summary>
        public static GameState Olustur(WorldRules rules, long clubId, long ownerUserId)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            rules.Validate();
            return new GameState
            {
                Club = new ClubState
                {
                    ClubId = clubId,
                    OwnerUserId = ownerUserId,
                    TesisTier = new byte[rules.yapi.tesisSayisi + 1],   // index 0 kullanılmaz (tesisId 1'den başlar)
                    InsaatSlot = new Construction[rules.yapi.insaatSlotSayisi],
                    Krediler = new Loan[rules.yapi.krediSlotSayisi],
                    SponsorTeklifleri = new SponsorOffer[rules.yapi.sponsorTeklifSlotSayisi],
                },
                Oyuncular = new PlayerState[0],
                Takvim = new CalendarState { Sezon = 1, Hafta = 1, Pencere = TransferWindow.Kapali },
                Fiyat = BosFiyat(),
                KalanDegisiklikHakki = (byte)rules.yapi.macBasinaDegisiklik,
            };
        }

        /// <summary>Kanonik sıra + kimlik tekilliği denetimi. Kurulumdan SONRA bir kez çağrılır;
        /// bozuk sıra sessizce kabul edilirse hash platformlar arası ayrışır.</summary>
        public void Validate()
        {
            if (Club == null) throw new ArgumentException("GameState: Club boş.");
            if (Oyuncular == null) throw new ArgumentException("GameState: Oyuncular boş.");
            if (Takvim == null) throw new ArgumentException("GameState: Takvim boş.");
            if (Fiyat == null || Fiyat.BiletKurus == null || Fiyat.BiletKurus.Length != 5
                || Fiyat.BufeKurus == null || Fiyat.BufeKurus.Length != 3
                || Fiyat.MagazaKurus == null || Fiyat.MagazaKurus.Length != 3)
                throw new ArgumentException("GameState: Fiyat dizileri eksik (5 tribün / 3 büfe / 3 mağaza).");
            if (Club.TesisTier == null || Club.InsaatSlot == null || Club.Krediler == null
                || Club.SponsorTeklifleri == null)
                throw new ArgumentException("GameState: kulüp dizileri boş.");
            for (int i = 1; i < Oyuncular.Length; i++)
            {
                if (Oyuncular[i].PlayerId == Oyuncular[i - 1].PlayerId)
                    throw new ArgumentException($"GameState: PlayerId {Oyuncular[i].PlayerId} tekrarlı.");
                if (Oyuncular[i].PlayerId < Oyuncular[i - 1].PlayerId)
                    throw new ArgumentException("GameState: Oyuncular PlayerId'ye göre artan sırada olmalı (kanonik sıra).");
            }
        }

        /// <summary>Oyuncu indeksi — ikili arama (Dictionary YOK, ME 3.2 sırasız yapı yasağı).
        /// Bulunamazsa -1.</summary>
        public int IndexOfPlayer(int playerId)
        {
            int lo = 0, hi = Oyuncular.Length - 1;
            while (lo <= hi)
            {
                int mid = lo + ((hi - lo) >> 1);
                int id = Oyuncular[mid].PlayerId;
                if (id == playerId) return mid;
                if (id < playerId) lo = mid + 1; else hi = mid - 1;
            }
            return -1;
        }

        /// <summary>Kapı 3 sorgusu: oyuncu bu kulübün mü (CB 5 "oyuncu bu kulübün mü").</summary>
        public bool OwnsPlayer(int playerId)
        {
            int i = IndexOfPlayer(playerId);
            return i >= 0 && Oyuncular[i].ClubId == Club.ClubId;
        }

        /// <summary>Kapı 3 sorgusu: kasa yeterli mi (`InsufficientFunds`).
        /// TAMSAYI karşılaştırma — eşik tam eşitlikte GEÇER (borç sıfırlayan ödeme meşrudur).</summary>
        public bool CanAfford(long tutarTl) => tutarTl <= 0 || Club.KasaTl >= tutarTl;

        /// <summary>Kapı 3 sorgusu: boş inşaat slotu var mı. Yoksa -1.</summary>
        public int FreeConstructionSlot()
        {
            for (int i = 0; i < Club.InsaatSlot.Length; i++)
                if (Club.InsaatSlot[i].InsaatId == 0) return i;
            return -1;
        }

        /// <summary>Kapı 3 sorgusu: bu tesiste zaten inşaat var mı (CB 8.2 StateConflict).</summary>
        public bool HasConstructionFor(int tesisId)
        {
            for (int i = 0; i < Club.InsaatSlot.Length; i++)
                if (Club.InsaatSlot[i].InsaatId != 0 && Club.InsaatSlot[i].TesisId == tesisId) return true;
            return false;
        }

        /// <summary>İnşaat kimliğinin slot indeksi; yoksa -1.</summary>
        public int IndexOfConstruction(int insaatId)
        {
            if (insaatId == 0) return -1;
            for (int i = 0; i < Club.InsaatSlot.Length; i++)
                if (Club.InsaatSlot[i].InsaatId == insaatId) return i;
            return -1;
        }

        /// <summary>Kredi kimliğinin slot indeksi; yoksa -1.</summary>
        public int IndexOfLoan(int krediId)
        {
            if (krediId == 0) return -1;
            for (int i = 0; i < Club.Krediler.Length; i++)
                if (Club.Krediler[i].KrediId == krediId) return i;
            return -1;
        }

        /// <summary>Sponsor teklifinin slot indeksi; yoksa -1.</summary>
        public int IndexOfSponsorOffer(int teklifId)
        {
            if (teklifId == 0) return -1;
            for (int i = 0; i < Club.SponsorTeklifleri.Length; i++)
                if (Club.SponsorTeklifleri[i].TeklifId == teklifId) return i;
            return -1;
        }

        /// <summary>Boş kredi slotu; yoksa -1.</summary>
        public int FreeLoanSlot()
        {
            for (int i = 0; i < Club.Krediler.Length; i++)
                if (Club.Krediler[i].KrediId == 0) return i;
            return -1;
        }

        /// <summary>Yeni kimlik üretimi — DETERMİNİSTİK: mevcut en büyük kimliğin bir fazlası.
        /// `Guid`/sayaç kullanılmaz; aynı durumdan aynı komut aynı kimliği üretmeli (CB 5.2).</summary>
        public int NextConstructionId()
        {
            int m = 0;
            for (int i = 0; i < Club.InsaatSlot.Length; i++) if (Club.InsaatSlot[i].InsaatId > m) m = Club.InsaatSlot[i].InsaatId;
            return m + 1;
        }
        public int NextLoanId()
        {
            int m = 0;
            for (int i = 0; i < Club.Krediler.Length; i++) if (Club.Krediler[i].KrediId > m) m = Club.Krediler[i].KrediId;
            return m + 1;
        }

        /// <summary>Kapı 3 sorgusu: transfer penceresi açık mı (`WindowClosed`).</summary>
        public bool IsTransferWindowOpen() => Takvim.Pencere != TransferWindow.Kapali;
    }
}
