using System;
using TheBadge.Sim.Config;
using TheBadge.Sim.Determinism;
using TheBadge.Sim.Match;

namespace TheBadge.Match
{
    /// <summary>MOTOR KOŞUCUSU — maçı ilerletir ve sunuma OKUMA yüzeyi verir.
    ///
    /// TASK-002 Kural 3'ün taşıyıcısı: `MatchState` bu sınıfın İÇİNDE kalır, dışarı `ref`
    /// verilmez. Sunum katmanı buradaki salt-okunur özelliklerden okur; `MatchState`e
    /// atama yapabilen tek yer motorun kendi `Tick`idir.
    ///
    /// Kural 6 (determinizm): duraklama, hız ve kare hızı YALNIZ `Ilerlet` çağrılarının
    /// zamanlamasını değiştirir. Motor tick sayısı ve komutların `IssueTick`i aynıysa maç
    /// bit düzeyinde aynıdır — duraklamak tick işlememektir, motoru yavaşlatmak değil.</summary>
    public sealed class MacKosucu
    {
        readonly MatchEngine motor;
        readonly CommandQueue kuyruk;
        readonly double kritikAnEsigi;
        readonly int orneklemeTick;
        MatchState durum;                       // DIŞARI SIZMAZ (Kural 3)
        KritikAnDedektoru dedektor;
        LiveWinProb.Sonuc olasilik;

        /// <summary>Motorun komut kuyruğunu KÖPRÜYE bağlar ve köprüyü döndürür.
        ///
        /// KUYRUK DIŞARI HİÇ VERİLMEZ (Kural 1): açık bir `Kuyruk` özelliği olsaydı sunum
        /// katmanı `Kuyruk.Enqueue(...)` diyerek dört kapıyı atlayabilirdi — ve bu, kuralı
        /// "dikkat edilecek bir şey" yapardı; burada YAPISAL OLARAK imkânsız. Motorun
        /// kuyruğuna ulaşan tek yol `CommandBus.Submit`tir.</summary>
        public MacKomutKoprusu KopruKur(BalansKaynagi balans, long userId)
            => MacKomutKoprusu.Kur(balans, kuyruk, userId);

        /// <summary>Bu maçta kaç kritik an duraklaması ateşlendi. Kabul ölçütü bunu ekranda
        /// GÖRÜNÜR istiyor (beklenen 8-12; motor tarafı `S2KritikAnRitmi` ile korunuyor).</summary>
        public int KritikAnSayisi { get; private set; }

        /// <summary>Son ateşlenen kritik anın büyüklüğü (olasılık sıçraması, 0-1).</summary>
        public double SonSicrama { get; private set; }

        /// <summary>SON TAKTİK MÜDAHALESİNİN AYNI TICK İÇİNDEKİ ETKİSİ — kabul ölçütü
        /// "taktik değişikliği şeridi ANINDA oynatıyor (aynı tick)" diyor ve bunun ekranda
        /// GÖRÜNÜR olmasını istiyor. Kare hızından ölçmek iddiayı 1-3 tick bulanıklaştırırdı;
        /// burada tek bir `motor.Tick` çağrısının İKİ YANI okunuyor, yani gösterilen sayı
        /// iddianın tam karşılığı. `TaktikDegisiklik` artmadıysa güncellenmez.</summary>
        public bool TaktikEtkisiVar { get; private set; }
        public LiveWinProb.Sonuc TaktikOncesi { get; private set; }
        public LiveWinProb.Sonuc TaktikSonrasi { get; private set; }
        public uint TaktikTick { get; private set; }

        public MacKosucu(SimBalance balans, ulong tohum, int orneklemeTick)
        {
            if (balans == null) throw new ArgumentNullException(nameof(balans));
            if (orneklemeTick <= 0) throw new ArgumentOutOfRangeException(nameof(orneklemeTick));
            this.orneklemeTick = orneklemeTick;
            // EŞİK BALANCE'TAN OKUNUR, kopyalanmaz: `canliOlasilik.kritikAnEsigi` [KALİBRE]
            // (K1 kararı, 0,04 → maç başına 9,9 an, hiçbir maç boş değil).
            // ME 15.3'ün `highlight.esik`i BU İŞ İÇİN KULLANILMAZ — o ölçüt maç başına 0,5-0,8
            // işaret verir ve maçların yarısını boş bırakır (DECISIONS 2026-09-05).
            kritikAnEsigi = balans.canliOlasilik.kritikAnEsigi;

            var cfg = new MatchConfig
            {
                Seed = tohum,
                EngineVersion = "5G-a",
                Home = KadroKur(1, ev: true),
                Away = KadroKur(2, ev: false)
            };
            kuyruk = new CommandQueue();
            motor = new MatchEngine(cfg.Seed, kuyruk, cfg, balans);
            durum = MatchEngine.CreateInitialState(cfg);

            olasilik = motor.AnlikOlasilik(in durum);
            dedektor.Sifirla(in olasilik);       // taban maç başında kurulur
        }

        // ------------------------------------------------------------------ OKUMA YÜZEYİ

        public bool Bitti => MatchEngine.IsFinished(in durum);
        public int EvGol => durum.HomeGoals;
        public int DeplasmanGol => durum.AwayGoals;
        public uint Tick => durum.Tick;
        public byte Devre => durum.Half;
        public MatchPhase Faz => durum.Phase;

        /// <summary>Canlı üç sonuçlu olasılık — toplamı 1. CANLI YOL BUDUR.
        /// `MatchSummaryPacket.WinProb3*` dizileri OKUNMAZ: onlar maç sonu inceleme eğrisidir
        /// (dakika başı, o tick'in müdahaleleri uygulanmadan ÖNCE) ve maç bitmeden dolmaz —
        /// inceleme turunun P1 bulgusu tam olarak buydu.</summary>
        public LiveWinProb.Sonuc Olasilik => olasilik;

        /// <summary>Motorun UYGULADIĞI taktik değişikliği sayısı.</summary>
        public int TaktikDegisiklik => motor.TacticChanges;

        /// <summary>MOTOR GEÇ REDDİ sayacı (Kural 2, ikinci yol): bus kabul etti ama uygulama
        /// anında düştü. YALNIZ SAYIdır, sebep taşımaz — bugün sebebi yok, sayacın arttığını
        /// görmek yeter. Bus reddi buraya HİÇ ULAŞMAZ ve bu sayacı DEĞİŞTİRMEZ.</summary>
        public int MotorRedSayaci => motor.RejectedCommands;

        public int OlaySayisi => motor.EventCount;
        public MatchEvent Olay(int i) => motor.GetEvent(i);

        /// <summary>Ev takımının o anki taktik kolları (-2..+2) — ekranın kadranları bunu
        /// gösterir, kendi yerel kopyasını değil.</summary>
        public sbyte EvMentalite => durum.HomeRt.Mentalite;
        public sbyte EvTempo => durum.HomeRt.Tempo;
        public sbyte EvPres => durum.HomeRt.Pres;
        public sbyte EvHat => durum.HomeRt.Hat;

        /// <summary>Saha noktalarını KOPYALAR (22 oyuncu). Dizi çağıran tarafından ayrılır ve
        /// yeniden kullanılır — kare başına tahsis yok. `MatchState` dışarı verilmediği için
        /// sunumun yazabileceği bir yüzey oluşmaz.</summary>
        public void AjanlariKopyala(SahaNoktasi[] hedef)
        {
            if (hedef == null || hedef.Length < 22) throw new ArgumentException("hedef en az 22 uzunlukta olmalı.");
            for (int i = 0; i < 22; i++)
            {
                var a = durum.Agents[i];
                hedef[i] = new SahaNoktasi(a.X, a.Y, a.Active, a.Energy);
            }
        }

        public SahaNoktasi Top => new SahaNoktasi(durum.Ball.X, durum.Ball.Y, true, durum.Ball.Z);

        // ------------------------------------------------------------------ İLERLETME

        /// <summary>En çok `tickButcesi` tick ilerletir. Kritik an ateşlenirse ORADA DURUR ve
        /// `true` döner — duraklama tam o anın üstüne oturur.
        ///
        /// Örnekleme motor tick'ine bağlıdır (kare hızına değil): aynı tohum + aynı komutlar
        /// aynı duraklama dizisini verir. Dedektörün tabanı yalnız ATEŞLENDİĞİNDE sıfırlandığı
        /// için kadans sayıyı değiştirmez (K1 ölçümü: 1 sn ↔ 30 sn arasında 10,0 ↔ 9,5).</summary>
        public bool Ilerlet(int tickButcesi)
        {
            for (int n = 0; n < tickButcesi; n++)
            {
                if (Bitti) break;

                var oncesi = olasilik;                       // TICK'İN ÖNCESİ
                int taktikOncesi = motor.TacticChanges;
                motor.Tick(ref durum);
                // Şerit HER TICK tazelenir: kabul ölçütü "taktik değişikliği şeridi AYNI TICK
                // içinde oynatıyor" diyor; kare sonunda bir kez okumak bu iddiayı bir kareye
                // kadar geciktirirdi.
                olasilik = motor.AnlikOlasilik(in durum);    // TICK'İN SONRASI

                if (motor.TacticChanges > taktikOncesi)
                {
                    TaktikEtkisiVar = true;
                    TaktikOncesi = oncesi;
                    TaktikSonrasi = olasilik;
                    TaktikTick = durum.Tick;
                }

                if (durum.Tick % (uint)orneklemeTick != 0) continue;
                if (!dedektor.Kontrol(in olasilik, kritikAnEsigi, out double sicrama)) continue;
                KritikAnSayisi++;
                SonSicrama = sicrama;
                return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ PLACEHOLDER KADRO

        /// <summary>Prosedürel test kadrosu — `Checks`in `BuildSheetSide` deseni, EngineDev
        /// sahnesindekiyle aynı üretim. GERÇEK KADRO/VERİ KATMANI S3'ÜN İŞİ (brif Out):
        /// bu ekran maçı gösterir, dünyayı değil.
        ///
        /// Kopya olduğu kayıtlı bir borçtur: `Game.EngineDev` ile `Game.Match` birbirine
        /// referans veremez ve fixture'ı `TheBadge.Sim`e koymak çekirdeğe test verisi
        /// sokardı. S3 gerçek kadroyu getirince ikisi de düşer.</summary>
        static TeamSheet KadroKur(uint entity, bool ev)
        {
            var kadro = new TeamSheet { Starters = new PlayerEntry[11], Bench = new PlayerEntry[5] };
            int yon = ev ? -1 : 1;
            for (int i = 0; i < 16; i++)
            {
                byte D(uint tuz) => (byte)(35 + (int)(Rng.Rand01(998877UL, Domain.Decision, entity, (uint)i, tuz) * 50));
                int ax, ay;
                if (i == 0) { ax = 48000; ay = 0; }
                else if (i < 5) { ax = 33000; ay = (i - 1) * 16000 - 24000; }
                else if (i < 9) { ax = 12000; ay = (i - 5) * 16000 - 24000; }
                else { ax = 3000; ay = i == 9 ? -8000 : 8000; }
                var o = new PlayerEntry
                {
                    PlayerId = (short)(entity * 100 + i),
                    Name = (ev ? "EV-" : "DEP-") + i,
                    RoleId = (byte)(i == 0 ? 1 : i < 5 ? 2 : i < 9 ? 3 : 4),
                    AnchorXmm = yon * ax,
                    AnchorYmm = ay,
                    Attributes = new PlayerAttributes
                    {
                        Passing = D(1), Finishing = D(2), Dribbling = D(7), Tackling = D(8),
                        FirstTouch = D(9), Positioning = D(10), Vision = D(11), Composure = D(12),
                        Pace = D(3), Acceleration = D(13), Stamina = D(4), Strength = D(14), Agility = D(15),
                        Reflexes = D(5), Handling = D(6)
                    }
                };
                if (i < 11) kadro.Starters[i] = o; else kadro.Bench[i - 11] = o;
            }
            return kadro;
        }
    }

    /// <summary>Sahadaki bir noktanın SUNUMA verilen kopyası — mm cinsinden, salt okunur.
    /// `Ek` alanı oyuncuda enerji (0-1000), topta yükseklik (mm).</summary>
    public readonly struct SahaNoktasi
    {
        public readonly int Xmm, Ymm, Ek;
        public readonly bool Sahada;
        public SahaNoktasi(int xmm, int ymm, bool sahada, int ek)
        { Xmm = xmm; Ymm = ymm; Sahada = sahada; Ek = ek; }
    }
}
