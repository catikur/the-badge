using System;
using System.Collections.Generic;

namespace TheBadge.World
{
    /// <summary>Outbox'a yazılmış tek yayın. `CommandId` UZAK TARAFIN dedup anahtarıdır
    /// (K6 inceleme bulgusu): teslimat en-az-bir-kez olduğu için ikinci kopyayı uzak taraf bu
    /// anahtarla eler. — CB 8.1/8.3</summary>
    public readonly struct OutboxKaydi
    {
        public readonly long Sira;            // FIFO — sunucuya varış sırası esastır (CB 8.2)
        public readonly Guid CommandId;
        public readonly byte Tur;             // 0 = klip paylaş · 1 = oyuncu raporla
        public readonly int A, B;             // klip: macId, pencereSn
        public readonly byte C;               // klip: hedef · rapor: sebep
        public readonly long D, E;            // klip: userId · rapor: hedefUserId, raporlayanUserId
        public readonly string Not;
        public readonly long EklendiSn;
        public readonly int Deneme;

        public OutboxKaydi(long sira, Guid cid, byte tur, int a, int b, byte c, long d, long e,
                           string not, long eklendiSn, int deneme)
        { Sira = sira; CommandId = cid; Tur = tur; A = a; B = b; C = c; D = d; E = e; Not = not; EklendiSn = eklendiSn; Deneme = deneme; }

        public OutboxKaydi DenemeArtmis() => new OutboxKaydi(Sira, CommandId, Tur, A, B, C, D, E, Not, EklendiSn, Deneme + 1);
    }

    /// <summary>Outbox'ın DAYANIKLILIK dikişi. Gerçek sunucuda bu, durum yazmasıyla AYNI veritabanı
    /// işleminde commit edilen bir tablodur; testte bellek içi eşleniği kullanılır. Arayüzün varlık
    /// sebebi budur — outbox'ın değeri "aynı işlemde commit" özelliğinden gelir, sınıfın kendisinden
    /// değil.</summary>
    public interface IOutboxStore
    {
        long Ekle(OutboxKaydi k);
        IReadOnlyList<OutboxKaydi> Bekleyenler(int enFazla);
        void TeslimIsaretle(long sira);
        void DenemeArtir(long sira);
        int BekleyenSayisi { get; }
        int TeslimSayisi { get; }
    }

    /// <summary>Bellek içi outbox — test eşleniği ve gerçek deponun ŞEKLİ.</summary>
    public sealed class BellekOutboxStore : IOutboxStore
    {
        readonly object kilit = new object();
        readonly List<OutboxKaydi> bekleyen = new List<OutboxKaydi>();
        long sonrakiSira = 1;
        int teslim;

        public long Ekle(OutboxKaydi k)
        {
            lock (kilit)
            {
                long s = sonrakiSira++;
                bekleyen.Add(new OutboxKaydi(s, k.CommandId, k.Tur, k.A, k.B, k.C, k.D, k.E, k.Not, k.EklendiSn, 0));
                return s;
            }
        }
        public IReadOnlyList<OutboxKaydi> Bekleyenler(int enFazla)
        {
            lock (kilit)
            {
                var liste = new List<OutboxKaydi>();
                for (int i = 0; i < bekleyen.Count && liste.Count < enFazla; i++) liste.Add(bekleyen[i]);
                return liste;
            }
        }
        public void TeslimIsaretle(long sira)
        {
            lock (kilit)
                for (int i = 0; i < bekleyen.Count; i++)
                    if (bekleyen[i].Sira == sira) { bekleyen.RemoveAt(i); teslim++; return; }
        }
        public void DenemeArtir(long sira)
        {
            lock (kilit)
                for (int i = 0; i < bekleyen.Count; i++)
                    if (bekleyen[i].Sira == sira) { bekleyen[i] = bekleyen[i].DenemeArtmis(); return; }
        }
        public int BekleyenSayisi { get { lock (kilit) return bekleyen.Count; } }
        public int TeslimSayisi { get { lock (kilit) return teslim; } }

        /// <summary>SÜREÇ ÖLÜMÜ benzetimi: bellek içi depo kalıcı deponun yerine geçtiği için,
        /// "yeniden başlatma" = aynı deponun yeni bir pompayla sürülmesi. Bekleyenler DURUR.</summary>
        public BellekOutboxStore() { }
    }

    /// <summary>`IOnlineSink`in outbox'a yazan hali — TEK DEĞİŞİKLİK bu.
    ///
    /// NEDEN GEREKLİ: yayın bugün `WorldExecutor` içinde, durum commit'iyle aynı kilitte ve
    /// hata halinde geri alınarak yapılıyor. Bu SÜREÇ İÇİ hataya karşı doğru, ama SÜREÇ ÖLÜMÜNE
    /// karşı değil: durum yazıldıktan sonra ağ çağrısı yarıda kalırsa yayın KAYBOLUR ve durum
    /// "yayınlandı" der. Outbox bunu kapatır — yayın, durumla AYNI atomik adımda kalıcı bir
    /// kayda yazılır; ağa teslimi ayrı bir pompa yapar ve teslim edene kadar kayıt DURUR.
    ///
    /// Executor'da değişiklik GEREKMEZ: bu tip mevcut `IOnlineSink` dikişine takılır, yani
    /// outbox yazması zaten atomik bölgenin içinde olur. — CB 8.1/8.3</summary>
    public sealed class OutboxSink : IOnlineSink
    {
        readonly IOutboxStore depo;
        readonly Func<long> saat;   // ENJEKTE: ortam saati yok, determinizm testlerde korunur

        public OutboxSink(IOutboxStore depo, Func<long> saat)
        {
            this.depo = depo ?? throw new ArgumentNullException(nameof(depo));
            this.saat = saat ?? throw new ArgumentNullException(nameof(saat));
        }

        public void KlipPaylas(Guid commandId, int macId, int pencereSn, byte hedef, long userId)
            => depo.Ekle(new OutboxKaydi(0, commandId, 0, macId, pencereSn, hedef, userId, 0, null, saat(), 0));

        public void OyuncuRaporla(Guid commandId, long hedefUserId, byte sebep, string notlar, long raporlayanUserId)
            => depo.Ekle(new OutboxKaydi(0, commandId, 1, 0, 0, sebep, hedefUserId, raporlayanUserId, notlar, saat(), 0));
    }

    /// <summary>Outbox pompası — bekleyenleri SIRAYLA gerçek kanala teslim eder.
    ///
    /// EN-AZ-BİR-KEZ: teslim başarılıysa kayıt düşer; patlarsa kayıt DURUR ve deneme sayısı artar.
    /// Bu yüzden uzak taraf `CommandId` ile dedup yapmak ZORUNDADIR — arayüz bu anahtarı taşır.
    /// Sıra korunur: bir kayıt teslim edilemezse ARKASINDAKİLER de bekler (CB 8.2 "varış sırası
    /// esastır"); sırayı atlayıp devam etmek, bağımlı iki yayını ters sırada uzak tarafa
    /// ulaştırabilirdi.</summary>
    public sealed class OutboxPompasi
    {
        readonly IOutboxStore depo;
        readonly IOnlineSink gercekKanal;
        readonly int denemeTavani;

        public OutboxPompasi(IOutboxStore depo, IOnlineSink gercekKanal, int denemeTavani = 8)
        {
            this.depo = depo ?? throw new ArgumentNullException(nameof(depo));
            this.gercekKanal = gercekKanal ?? throw new ArgumentNullException(nameof(gercekKanal));
            this.denemeTavani = denemeTavani;
        }

        /// <summary>Bir tur sürer. Dönen: teslim edilen kayıt sayısı.
        /// `takiliDetay` boş değilse pompa sıranın başında takılmıştır (çağıran geri çekilir).</summary>
        public int Sur(int enFazla, out string takiliDetay)
        {
            takiliDetay = null;
            int teslim = 0;
            var liste = depo.Bekleyenler(enFazla);
            for (int i = 0; i < liste.Count; i++)
            {
                var k = liste[i];
                try
                {
                    if (k.Tur == 0) gercekKanal.KlipPaylas(k.CommandId, k.A, k.B, k.C, k.D);
                    else gercekKanal.OyuncuRaporla(k.CommandId, k.D, k.C, k.Not, k.E);
                    depo.TeslimIsaretle(k.Sira);
                    teslim++;
                }
                catch (Exception ex)
                {
                    depo.DenemeArtir(k.Sira);
                    takiliDetay = k.Deneme + 1 >= denemeTavani
                        ? $"sira {k.Sira} deneme tavanina ulasti ({denemeTavani}): {ex.Message}"
                        : $"sira {k.Sira} teslim edilemedi (deneme {k.Deneme + 1}): {ex.Message}";
                    return teslim;   // SIRAYI ATLAMA — arkadakiler bekler
                }
            }
            return teslim;
        }
    }
}
