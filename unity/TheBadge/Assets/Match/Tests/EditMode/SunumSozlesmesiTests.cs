using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TheBadge.Match;
using TheBadge.Sim.Commands;
using TheBadge.Sim.Match;

namespace TheBadge.Match.Tests
{
    /// <summary>SUNUM KATMANININ OKUMA SÖZLEŞMESİ — TASK-002 DoD-G maddesi 2.
    ///
    /// Bu testler ekranın GÖRÜNTÜSÜNÜ ölçmez (o mülakatlı gözlem turunun işi); ekranın
    /// motorla arasındaki SÖZLEŞMEYİ ölçer: durumu okur mu yoksa yazar mı, komut Tek Kapı'dan
    /// mı geçiyor, iki red yolu birbirine karışıyor mu, aynı tohum aynı maçı mı veriyor.
    ///
    /// Çekirdeği ölçen 180 kapı `shared/TheBadge.Sim.Checks`tedir ve buraya kopyalanmaz —
    /// burada ölçülen şey UNITY TARAFININ dikişi.</summary>
    public sealed class SunumSozlesmesiTests
    {
        const long KullaniciId = 42;
        const ulong Tohum = 20260906UL;
        const int OrneklemeTick = 30;

        static BalansKaynagi Balans() => BalansKaynagi.Yukle();

        static MacKosucu YeniKosucu(BalansKaynagi b, ulong tohum = Tohum)
            => new MacKosucu(b.Sim, tohum, OrneklemeTick);

        /// <summary>Maçı sonuna kadar TEK TICK adımlarıyla koşturur — kare hızı taklidi yok,
        /// yani sonuç tamamen motorun ve komutların işi.</summary>
        static void SonaKadar(MacKosucu k, int tavan = 200_000)
        {
            int n = 0;
            while (!k.Bitti && n++ < tavan) k.Ilerlet(1);
            Assert.IsTrue(k.Bitti, "maç emniyet tavanında bitmedi");
        }

        // ---------------------------------------------------------------- KURAL 3: OKUR, YAZMAZ

        [Test]
        public void Kural3_MatchStateSunumaHicSizmaz()
        {
            var t = typeof(MacKosucu);
            var sizinti = t.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(m =>
                {
                    Type tip = m is PropertyInfo p ? p.PropertyType
                             : m is FieldInfo f ? f.FieldType
                             : m is MethodInfo mi ? mi.ReturnType : null;
                    if (tip != null && (tip == typeof(MatchState) || tip == typeof(MatchState).MakeByRefType()))
                        return true;
                    // Parametre olarak da sızmamalı: `ref MatchState` alan public bir metot,
                    // sunuma yazma yolu açardı.
                    if (m is MethodInfo m2)
                        return m2.GetParameters().Any(pp => pp.ParameterType == typeof(MatchState)
                                                         || pp.ParameterType == typeof(MatchState).MakeByRefType());
                    return false;
                })
                .Select(m => m.Name).ToArray();

            Assert.IsEmpty(sizinti,
                "MacKosucu MatchState'i dışarı veriyor — sunum katmanı duruma YAZABİLİR hale gelir (Kural 3). Üyeler: "
                + string.Join(", ", sizinti));
        }

        [Test]
        public void Kural1_KomutKuyruguSunumaHicSizmaz()
        {
            // Tek Kapı'nın YAPISAL güvencesi: açık bir CommandQueue olsaydı ekran
            // `Kuyruk.Enqueue(...)` diyerek dört kapıyı atlayabilirdi.
            var t = typeof(MacKosucu);
            var sizinti = t.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(m =>
                {
                    Type tip = m is PropertyInfo p ? p.PropertyType
                             : m is FieldInfo f ? f.FieldType
                             : m is MethodInfo mi ? mi.ReturnType : null;
                    return tip == typeof(CommandQueue);
                })
                .Select(m => m.Name).ToArray();

            Assert.IsEmpty(sizinti,
                "MacKosucu CommandQueue'yu dışarı veriyor — Tek Kapı atlanabilir hale gelir (Kural 1). Üyeler: "
                + string.Join(", ", sizinti));
        }

        // ---------------------------------------------------------------- KURAL 1: TEK KAPI ZİNCİRİ

        [Test]
        public void TekKapi_GecerliTaktikBusUzerindenMotoraUlasir()
        {
            var b = Balans();
            var k = YeniKosucu(b);
            var kopru = k.KopruKur(b, KullaniciId);

            k.Ilerlet(50);
            int oncekiDegisiklik = k.TaktikDegisiklik;

            var o = kopru.TaktikGonder(2, 0, -1, 1, k.Tick + 1, Saat());

            Assert.IsTrue(o.Ok, $"geçerli taktik reddedildi: {o.Reason} / {o.Detail}");
            Assert.AreEqual(1, kopru.MotoraIletilen, "komut motorun kuyruğuna iletilmedi");

            // Motor komutu ME 14.2'nin güvenli anında uygular; birkaç bin tick yeter.
            for (int i = 0; i < 6000 && k.TaktikDegisiklik == oncekiDegisiklik && !k.Bitti; i++) k.Ilerlet(1);
            Assert.Greater(k.TaktikDegisiklik, oncekiDegisiklik, "motor taktik değişikliğini hiç uygulamadı");
        }

        [Test]
        public void TekKapi_MacTickSifirReddedilir()
        {
            var b = Balans();
            var k = YeniKosucu(b);
            var kopru = k.KopruKur(b, KullaniciId);
            // CB 3.1: MatchTick 0 "hub komutu"dur. Sessizce hub yoluna düşmek, canlı müdahale
            // sanılan bir komutun KALICI taktiği değiştirmesi demekti.
            Assert.Throws<ArgumentOutOfRangeException>(() => kopru.TaktikGonder(1, 0, 0, 0, 0, Saat()));
        }

        // ---------------------------------------------------------------- KURAL 2: İKİ RED YOLU

        [Test]
        public void Kural2_BusReddiMotoraUlasmaz_SebebiVar()
        {
            var b = Balans();
            var k = YeniKosucu(b);
            var kopru = k.KopruKur(b, KullaniciId);
            k.Ilerlet(50);

            int motorRedOnce = k.MotorRedSayaci;
            // Bant `squad.mentalite` = [-2, +2]; 5 bandın DIŞINDA.
            var o = kopru.TaktikGonder(5, 0, 0, 0, k.Tick + 1, Saat());

            Assert.IsFalse(o.Ok, "bant dışı değer kabul edildi");
            Assert.AreEqual(RejectionReason.ParamOutOfBand, o.Reason, "bus reddi beklenen sebeple gelmedi");
            Assert.AreEqual(0, kopru.MotoraIletilen, "bus reddi komutu motora İLETTİ");
            Assert.AreEqual(motorRedOnce, k.MotorRedSayaci,
                "bus reddi motorun geç red sayacını değiştirdi — iki red yolu birbirine karışıyor");
        }

        [Test]
        public void Kural2_HizSiniriDaSebebiyleGorunur()
        {
            // İKİNCİ BUS RED YOLU. `squad.set_team_tactic` RateClass.Tactic'te ve
            // `command.bands.json` ona 60/60sn veriyor. Kadranlara hızlı basan bir oyuncu bu
            // sınıra GERÇEKTEN çarpar; çarptığında ekranın "hiçbir şey olmadı" demesi
            // kabul edilemez (CB 11.1) — sebebin taşındığını burada ölçüyoruz.
            var b = Balans();
            var k = YeniKosucu(b);
            var kopru = k.KopruKur(b, KullaniciId);
            k.Ilerlet(50);

            long saat = Saat();                     // AYNI host saati: pencere kaymasın
            RejectionReason son = RejectionReason.None;
            int kabul = 0;
            for (int i = 0; i < 200 && son != RejectionReason.RateLimited; i++)
            {
                var o = kopru.TaktikGonder(i % 3 - 1, 0, 0, 0, k.Tick + 1, saat);
                if (o.Ok) kabul++;
                son = o.Reason;
            }

            Assert.AreEqual(RejectionReason.RateLimited, son,
                $"hız sınırına çarpılmadı ({kabul} komut kabul edildi) — red yolu sessiz kalıyor olabilir");
            Assert.AreEqual(kabul, kopru.MotoraIletilen,
                "motora iletilen komut sayısı bus'ın kabul ettiğinden farklı");
        }

        // ---------------------------------------------------------------- KURAL 6: DETERMİNİZM

        [Test]
        public void Kural6_AyniTohumAyniKomut_AyniMac()
        {
            var b = Balans();
            (int ev, int dep, int olay, int duraklama, int taktik) Kos()
            {
                var k = YeniKosucu(b);
                var kopru = k.KopruKur(b, KullaniciId);
                // Komutlar SABİT TICK'lerde verilir — "aynı müdahaleler" bu demek.
                int[] tickler = { 600, 5_000, 20_000, 40_000 };
                int sonraki = 0;
                int n = 0;
                while (!k.Bitti && n++ < 200_000)
                {
                    if (sonraki < tickler.Length && k.Tick >= tickler[sonraki])
                    {
                        var o = kopru.TaktikGonder(sonraki % 3 - 1, 1, 0, sonraki % 2, k.Tick + 1, Saat());
                        Assert.IsTrue(o.Ok, $"komut reddedildi: {o.Reason} / {o.Detail}");
                        sonraki++;
                    }
                    k.Ilerlet(1);
                }
                return (k.EvGol, k.DeplasmanGol, k.OlaySayisi, k.KritikAnSayisi, k.TaktikDegisiklik);
            }

            var a = Kos();
            var c = Kos();
            Assert.AreEqual(a, c, "aynı tohum + aynı müdahaleler farklı maç verdi (determinizm kırık)");
        }

        [Test]
        public void Ilerlet_ToplulukKareHizindanBagimsiz()
        {
            // Kare hızı sunumun işi, maçın değil (Kural 6). Tek tick'lik ve 250'lik adımlarla
            // koşan iki maç AYNI olmalı — yoksa hızlı bir cihaz başka bir maç oynardı.
            var b = Balans();
            var tek = YeniKosucu(b);
            SonaKadar(tek);

            var toplu = YeniKosucu(b);
            int n = 0;
            while (!toplu.Bitti && n++ < 200_000) toplu.Ilerlet(250);

            Assert.AreEqual(tek.EvGol, toplu.EvGol, "skor kare hızına göre değişti (ev)");
            Assert.AreEqual(tek.DeplasmanGol, toplu.DeplasmanGol, "skor kare hızına göre değişti (deplasman)");
            Assert.AreEqual(tek.OlaySayisi, toplu.OlaySayisi, "olay sayısı kare hızına göre değişti");
            Assert.AreEqual(tek.KritikAnSayisi, toplu.KritikAnSayisi,
                "duraklama sayısı kare hızına göre değişti — örnekleme tick ızgarasına bağlı değil");
        }

        static long Saat() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
