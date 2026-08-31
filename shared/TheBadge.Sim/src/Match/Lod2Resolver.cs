using System;
using TheBadge.Sim.Config;
using TheBadge.Sim.Determinism;

namespace TheBadge.Sim.Match
{
    /// <summary>Simülasyon detay seviyesi — ME Spec 16.1.
    /// <para><b>Lod1 kasıtlı olarak Lod0'a EŞDEĞERDİR.</b> 16.1'in LOD 1 satırı (5 Hz hareket /
    /// 2 Hz karar) tek gerekçeyle vardı: CPU. Ölçüm (M15) LOD 0'ı maç başına 131 ms buldu — LOD 1
    /// bütçesinin (800 ms) 6 katı altında. İkinci bir tick oranı, ikinci bir fizik entegrasyonu ve
    /// ikinci bir kalibrasyon demek olurdu; kazanç sıfırken bedeli "tek sim, tek gerçek" ilkesi.
    /// Karar DECISIONS.md'ye gerekçesiyle yazıldı; geri alınırsa burası ayrışır.</para></summary>
    public enum LodLevel : byte
    {
        Lod0 = 0,   // tam simülasyon — online maçlar ZORUNLU olarak budur (16.3)
        Lod1 = 1,   // şu an Lod0'ın eşleniği (yukarıdaki nota bakın)
        Lod2 = 2    // tablo tabanlı hızlı çözüm — yalnız arka plan dünya simülasyonu
    }

    /// <summary>LOD 2 hızlı çözücü — ME Spec 16.1/16.4. Maç OYNANMAZ; sonuç, LOD 0 koşularından
    /// regresyonla türetilmiş tablodan ÖRNEKLENİR (16.1: "LOD 2 tabloları LOD 0 ile koşulan
    /// kalibrasyon maçlarından regresyonla türetilir").
    /// <para>Neden var: LOD 0 sunucuda fazlasıyla ucuz (M15 ölçümü 131 ms → 24 çekirdekte 183
    /// maç/sn, 16.3 hedefi 16,7). Bütçenin sıkıştığı tek yer İSTEMCİDİR: 16.4'ün sezon turu
    /// ~200 arka plan maçı ister ve 12-18 sn'de bitmelidir; 200 × LOD 0 orta cihazda dakikalara
    /// çıkar. LOD 2 bunu milisaniyelere indirir.</para>
    /// <para>Determinizm: aynı (seed, kadro, tablo) → aynı sonuç. Zarlar CHAOS akışından çekilir
    /// (ME 3.1) — LOD 2'nin belirsizliği belirli bir alt sisteme değil MAÇIN BÜTÜNÜNE aittir;
    /// Decision/Duel/Physics akışlarını kirletmek LOD 0 replay'lerini tehdit ederdi.</para></summary>
    public sealed class Lod2Resolver
    {
        readonly SimBalance bal;
        readonly Lod2Table table;

        /// <summary>Özet log kapasitesi — ME 16.1: LOD 2'de "özet log (şut zinciri + kartlar)".
        /// Tam log YOKTUR ve highlight/replay üretilmez (aynı satır).</summary>
        public const int SummaryCapacity = 32;
        readonly MatchEvent[] summary = new MatchEvent[SummaryCapacity];
        int summaryCount;
        public int SummaryCount => summaryCount;
        public MatchEvent GetSummaryEvent(int i) => summary[i < 0 ? 0 : i >= summaryCount ? summaryCount - 1 : i];

        public Lod2Resolver(SimBalance balance, Lod2Table lod2Table)
        {
            bal = balance;
            table = lod2Table;
        }

        /// <summary>Takım gücü (0-100) — kadrodan TÜRETİLİR, simülasyon gerektirmez.
        /// Ağırlıklar `sim.balance.json` → `lod.guc` altındadır [KALİBRE]; kaleci ayrı bileşen
        /// çünkü nitelik seti ortak değildir (Reflexes/Handling saha oyuncusunda anlamsızdır).</summary>
        public double TeamStrength(TeamSheet sheet)
        {
            var g = bal.lod.guc;
            var k = sheet.Starters[0].Attributes;
            double gk = g.kaleci.reflexes * k.Reflexes + g.kaleci.handling * k.Handling
                      + g.kaleci.oneOnOne * k.OneOnOne + g.kaleci.aerialCommand * k.AerialCommand;
            double saha = 0;
            for (int i = 1; i < 11; i++)
            {
                var a = sheet.Starters[i].Attributes;
                var o = g.sahaOyuncusu;
                saha += o.passing * a.Passing + o.finishing * a.Finishing + o.tackling * a.Tackling
                      + o.pace * a.Pace + o.positioning * a.Positioning + o.decisions * a.Decisions
                      + o.firstTouch * a.FirstTouch + o.strength * a.Strength;
            }
            saha /= 10.0;
            return g.kaleciPayi * gk + (1.0 - g.kaleciPayi) * saha;
        }

        /// <summary>Maçı tablodan çözer — ME 16.1. Dönen `MatchResult` LOD 0'ınkiyle AYNI tiptedir:
        /// çağıran taraf (lig tablosu, haber katmanı) LOD'u bilmek zorunda kalmaz.
        /// `FinalChecksum` 0'dır: LOD 2'nin durum uzayı yoktur, replay sözleşmesi LOD 0'a aittir (16.3).</summary>
        public MatchResult Run(ulong seed, MatchConfig cfg)
        {
            summaryCount = 0;
            double sEv = TeamStrength(cfg.Home), sDep = TeamStrength(cfg.Away);

            int golEv = PoissonDraw(Ara(table.gol, sEv, sDep), seed, 1);
            int golDep = PoissonDraw(Ara(table.gol, sDep, sEv), seed, 2);

            var res = new MatchResult
            {
                HomeGoals = golEv,
                AwayGoals = golDep,
                // Süre alanları LOD 0'la aynı ANLAMI taşır ama örneklenmez: LOD 2'de saat yoktur.
                TotalTicks = 0,
                StoppageTicks = 0,
                Shots = Yuvarla(table.sut, sEv, sDep, seed, 3) + Yuvarla(table.sut, sDep, sEv, seed, 4),
                Saves = 0,
                Fouls = Yuvarla(table.faul, sEv, sDep, seed, 5) + Yuvarla(table.faul, sDep, sEv, seed, 6),
                Yellows = Yuvarla(table.sari, sEv, sDep, seed, 7) + Yuvarla(table.sari, sDep, sEv, seed, 8),
                Reds = Yuvarla(table.kirmizi, sEv, sDep, seed, 9) + Yuvarla(table.kirmizi, sDep, sEv, seed, 10),
                Corners = Yuvarla(table.korner, sEv, sDep, seed, 11) + Yuvarla(table.korner, sDep, sEv, seed, 12),
                Penalties = 0,
                XgHome = Ara(table.xg, sEv, sDep),
                XgAway = Ara(table.xg, sDep, sEv),
                FinalChecksum = 0
            };

            // ÖZET LOG (16.1): yalnız goller ve kartlar. Dakikalar CHAOS akışından çekilir —
            // haber/hikaye katmanı "78'de kazandı" diyebilsin diye; sunum değeri buradadır.
            for (int i = 0; i < golEv && summaryCount < SummaryCapacity; i++) OzetGol(seed, 0, i, i + 1, golDep);
            for (int i = 0; i < golDep && summaryCount < SummaryCapacity; i++) OzetGol(seed, 1, i, golEv, i + 1);
            int sariEv = Yuvarla(table.sari, sEv, sDep, seed, 7), sariDep = Yuvarla(table.sari, sDep, sEv, seed, 8);
            for (int i = 0; i < sariEv && summaryCount < SummaryCapacity; i++) OzetKart(seed, 0, i, EventType.YellowCard);
            for (int i = 0; i < sariDep && summaryCount < SummaryCapacity; i++) OzetKart(seed, 1, i, EventType.YellowCard);
            SirralaOzet();
            return res;
        }

        /// <summary>Izgarada iki doğrusal ara değerleme: (kendi güç, rakip güç) → takım başına
        /// ortalama. Eksen dışında KIRPAR (dışdeğerleme yok — tepki eğrisinin uçlarda ne yaptığı
        /// ölçülmemiştir, uydurmak sessiz hata olurdu).</summary>
        double Ara(double[] izgara, double kendi, double rakip)
        {
            var eks = table.gucEkseni;
            if (izgara == null || eks == null || eks.Length == 0 || izgara.Length != eks.Length * eks.Length)
                return 0.0;
            Konum(eks, kendi, out int i0, out int i1, out double tk);
            Konum(eks, rakip, out int j0, out int j1, out double tr);
            int n = eks.Length;
            double a = izgara[i0 * n + j0], b = izgara[i0 * n + j1];
            double c = izgara[i1 * n + j0], e = izgara[i1 * n + j1];
            double alt = a + (b - a) * tr, ust = c + (e - c) * tr;
            double v = alt + (ust - alt) * tk;
            return v < 0 ? 0 : v;
        }

        /// <summary>Eksende konum: komşu iki indeks + aradaki oran. Eksen ARTAN varsayılır.</summary>
        static void Konum(double[] eksen, double v, out int i0, out int i1, out double t)
        {
            if (eksen.Length == 1) { i0 = i1 = 0; t = 0; return; }
            if (v <= eksen[0]) { i0 = i1 = 0; t = 0; return; }
            if (v >= eksen[eksen.Length - 1]) { i0 = i1 = eksen.Length - 1; t = 0; return; }
            int k = 0;
            while (k < eksen.Length - 2 && v > eksen[k + 1]) k++;
            i0 = k; i1 = k + 1;
            double genislik = eksen[i1] - eksen[i0];
            t = genislik <= 0 ? 0 : (v - eksen[i0]) / genislik;
        }

        /// <summary>Ortalamayı TAMSAYIYA çevirir; kesirli kısım olasılık olarak çekilir — aksi
        /// halde her maç aynı yuvarlanmış değeri verir ve dağılım tepe noktasında donardı.</summary>
        int Yuvarla(double[] izgara, double kendi, double rakip, ulong seed, uint salt)
        {
            double v = Ara(izgara, kendi, rakip);
            int taban = (int)v;
            double kesir = Kuanta(v - taban);
            return Rng.Rand01(seed, Domain.Chaos, 7000, 0, salt) < kesir ? taban + 1 : taban;
        }

        /// <summary>Poisson ters-CDF örneklemesi. `Math.Exp` platformlar arasında son bit'te
        /// oynayabilir → birikimli olasılık Q16'ya kuantalanır (motorun `pSave` çözümüyle aynı
        /// gerekçe, ME 3.2). Üst sınır 15: futbolda gol sayısı bu bandın dışına çıkmaz.</summary>
        int PoissonDraw(double lambda, ulong seed, uint salt)
        {
            if (lambda <= 0) return 0;
            double u = Rng.Rand01(seed, Domain.Chaos, 7000, 0, salt);
            double p = Math.Exp(-lambda), cum = Kuanta(p);
            int k = 0;
            while (u > cum && k < 15) { k++; p *= lambda / k; cum = Kuanta(cum + p); }
            return k;
        }

        static double Kuanta(double v) => (int)(v * 65536.0) / 65536.0;

        /// <summary>TARAF AYRIMI `SummaryCapacity` KADARDIR, 20 değil (K10 bulgusu). Eski ayrım
        /// `team * 20`, yalnız `idx < 20` iken doğruydu; goller `PoissonDraw`ın `k < 15` kapağıyla
        /// yapısal olarak güvendeydi ama KARTLAR değildi — `sariEv` bir kalibrasyon ızgarasından
        /// gelir ve yapısal üst sınırı yoktur; döngüyü yalnız `summaryCount < SummaryCapacity`
        /// kapatır. Yani koruma yapıdan değil ızgara DEĞERLERİNİN küçüklüğünden geliyordu.
        ///
        /// Ayrım `SummaryCapacity`ye çekilince garanti YAPISAL olur: `idx`, `summaryCount` ile
        /// birlikte arttığı ve döngü `summaryCount < SummaryCapacity` ile kapandığı için
        /// `idx ≤ SummaryCapacity-1` her zaman doğrudur. `idx`i 20'de kapatmak da garantiyi
        /// yapıya taşırdı ama 20. karttan sonrasını LOG'DAN DÜŞÜRÜRDÜ; bu çözüm veri kaybetmez.</summary>
        void OzetGol(ulong seed, byte team, int idx, int evSkor, int depSkor)
        {
            uint dk = (uint)(Rng.Rand01(seed, Domain.Chaos, (uint)(7100 + team * SummaryCapacity + idx), 0, 20) * 90);
            summary[summaryCount++] = new MatchEvent
            {
                Tick = dk * 600, Type = (ushort)EventType.Goal, ActorA = -1, ActorB = -1,
                TeamIdx = team, X = 0, Y = 0,
                AuxData = evSkor * 1000 + depSkor * 10, Xg = 0f, Flags = 0
            };
        }

        void OzetKart(ulong seed, byte team, int idx, EventType tip)
        {
            uint dk = (uint)(Rng.Rand01(seed, Domain.Chaos, (uint)(7200 + team * SummaryCapacity + idx), 0, 21) * 90);
            summary[summaryCount++] = new MatchEvent
            {
                Tick = dk * 600, Type = (ushort)tip, ActorA = -1, ActorB = -1,
                TeamIdx = team, X = 0, Y = 0, AuxData = 1, Xg = 0f, Flags = 0
            };
        }

        /// <summary>Özet log'u tick'e göre sıralar (küçük dizi — ekleme sıralaması, deterministik).</summary>
        void SirralaOzet()
        {
            for (int i = 1; i < summaryCount; i++)
            {
                var x = summary[i];
                int j = i - 1;
                while (j >= 0 && summary[j].Tick > x.Tick) { summary[j + 1] = summary[j]; j--; }
                summary[j + 1] = x;
            }
        }
    }
}
