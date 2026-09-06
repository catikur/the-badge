using System;
using System.Collections.Generic;
using TheBadge.CommandBus;
using TheBadge.Sim.Commands;
using TheBadge.Sim.Match;
using TheBadge.World;

namespace TheBadge.Match
{
    /// <summary>TEK KAPI KÖPRÜSÜ — TASK-002 Kural 1'in karşılığı. Ekrandaki taktik kadranları
    /// motorun `CommandQueue`suna DOĞRUDAN yazmaz; zincir şu:
    ///
    ///   UI → CommandBus.Submit(zarf, payload, WorldExecutor, saat, userId)
    ///        → 4 kapı → SquadActions.TaktikHandler → IMatchCommandSink → CommandQueue
    ///
    /// Kablolama `shared/TheBadge.Sim.Checks/WorldHarness.cs`ın (~290) desenidir; sink de
    /// oradaki `SpyMatchSink`in aynı sözleşmesi — tek fark, listeye yazmak yerine motorun
    /// kuyruğuna iletmesi.
    ///
    /// `Submit` `ICommandExecutor`ü ZORUNLU ister (null verilirse `ArgumentNullException`):
    /// yürütücüsüz çağrı durumu değiştirmeden "başarılı" derdi. `squad.set_team_tactic`i
    /// işleyen tek uygulama `WorldExecutor` + `SquadActions.Baglan`dır.
    ///
    /// DÜNYA DURUMU BU EKRANIN KONUSU DEĞİL (brif Scope/Out): `GameState` yalnız köprünün
    /// gerektirdiği en küçük hâlde kurulur — Kapı 3 kulüp sahipliğini okuyabilsin diye.
    /// Kadro, ekonomi, takvim yok; onlar S3'ün işi.</summary>
    public sealed class MacKomutKoprusu
    {
        /// <summary>Ev sahibi = oyuncunun takımı. Zarftaki `TeamIdx` yalnız ev/deplasmandır.</summary>
        public const byte OyuncuTakimIdx = 0;

        readonly WorldStore depo;
        readonly WorldContext ctx;
        readonly WorldExecutor exec;
        readonly TheBadge.CommandBus.CommandBus bus;
        readonly MotorKuyrukSink sink;
        readonly long userId;
        readonly int saveSlotId;

        /// <summary>Bus'ın KABUL edip motora ilettiği komut sayısı. `MatchEngine.TacticChanges`
        /// ile arasındaki fark, motorun GEÇ reddettiklerini verir (Kural 2, ikinci red yolu).</summary>
        public int MotoraIletilen => sink.Iletilen;

        MacKomutKoprusu(WorldStore depo, WorldContext ctx, WorldExecutor exec,
                        TheBadge.CommandBus.CommandBus bus, MotorKuyrukSink sink,
                        long userId, int saveSlotId)
        {
            this.depo = depo; this.ctx = ctx; this.exec = exec; this.bus = bus;
            this.sink = sink; this.userId = userId; this.saveSlotId = saveSlotId;
        }

        /// <summary>Köprüyü kurar. `kuyruk` motorun KENDİ kuyruğudur — komut oraya ancak dört
        /// kapıdan geçtikten sonra düşer.</summary>
        public static MacKomutKoprusu Kur(BalansKaynagi balans, CommandQueue kuyruk,
                                          long userId, long clubId = 1, int saveSlotId = 1)
        {
            if (balans == null) throw new ArgumentNullException(nameof(balans));
            if (kuyruk == null) throw new ArgumentNullException(nameof(kuyruk));

            var st = GameState.Olustur(balans.DunyaKurallari, clubId, userId);
            var depo = new WorldStore(st);
            // MAÇ bağlamı açık; Hub da açık (aynı aksiyon iki bağlamda geçerli, CB 4.2) ama
            // bu ekran yalnız maç tick'i > 0 ile gönderir, yani hub yoluna hiç girmez.
            var ctx = new WorldContext(depo, balans.DunyaKurallari) { Active = Context.Hub | Context.Match };
            var exec = new WorldExecutor(depo, ctx);
            var sink = new MotorKuyrukSink(kuyruk);
            SquadActions.Baglan(ctx, exec, balans.DunyaKurallari, sink);
            var bus = new TheBadge.CommandBus.CommandBus(
                balans.Bantlar, ctx,
                new SlidingWindowRateLimiter(balans.HizSiniri), new IdempotencyStore());
            return new MacKomutKoprusu(depo, ctx, exec, bus, sink, userId, saveSlotId);
        }

        /// <summary>Taktik dört kadranı — maç bağlamında. `macTick` SIFIRDAN BÜYÜK olmalı:
        /// CB 3.1'e göre 0 "hub komutu" demektir ve komut kalıcı taktiği düzenlerdi, canlı
        /// müdahale olmazdı.
        ///
        /// Değerler MUTLAK kadran konumudur (delta değil): motorda `TacticChangeCmd.Delta`
        /// oyuncunun o anki kolu, bandı [-2, +2] (`squad.mentalite` vb.).</summary>
        public CommandOutcome TaktikGonder(int mentalite, int tempo, int pres, int hat,
                                           uint macTick, long hostSaatMs)
        {
            if (macTick == 0) throw new ArgumentOutOfRangeException(nameof(macTick),
                "maç komutu için MatchTick > 0 olmalı (CB 3.1: 0 = hub komutu).");

            var zarf = new CommandEnvelope
            {
                // İdempotency anahtarı — istemci üretir (CB 3.1). Rastgelelik SİMÜLASYONA
                // girmez: zarf kimliği motorun durumuna dokunmaz, yalnız bus'ın tekilleştirme
                // deposunda yaşar.
                CommandId = Guid.NewGuid(),
                CatalogVersion = Catalog.Version,
                Source = CommandSource.UI,
                ActionType = "squad.set_team_tactic",
                IssuedAtUnixMs = hostSaatMs,
                MatchTick = macTick,
                UserId = userId,
                SaveSlotId = saveSlotId,
                TeamIdx = OyuncuTakimIdx,
                PayloadJson = Array.Empty<byte>(),
            };
            var yuk = new SozlukYuk()
                .Koy("mentalite", mentalite).Koy("tempo", tempo)
                .Koy("pres", pres).Koy("hat", hat);

            return bus.Submit(zarf, yuk, exec, hostSaatMs, userId);
        }

        /// <summary>Motorun kuyruğuna ileten sink — `SpyMatchSink`in üretim karşılığı.
        /// Yayınlama commit'in parçasıdır: `WorldExecutor` sink'i ancak journal doğrulanıp
        /// uygulandıktan sonra çağırır, yani reddedilen komut kuyruğa DÜŞMEZ.</summary>
        sealed class MotorKuyrukSink : IMatchCommandSink
        {
            readonly CommandQueue kuyruk;
            public int Iletilen { get; private set; }
            public MotorKuyrukSink(CommandQueue q) { kuyruk = q; }
            public void Enqueue(MatchCommand cmd) { kuyruk.Enqueue(cmd); Iletilen++; }
        }

        /// <summary>`IPayloadView` — Kapı 2'nin okuduğu yük. Alan SIRASI korunur; sıkı mod
        /// fazladan alanı reddettiği için (CB 3.2) yalnız kataloğun istediği dördü konur.</summary>
        public sealed class SozlukYuk : IPayloadView
        {
            readonly List<string> adlar = new List<string>();
            readonly Dictionary<string, long> degerler = new Dictionary<string, long>(StringComparer.Ordinal);

            public SozlukYuk Koy(string ad, long v)
            {
                if (!degerler.ContainsKey(ad)) adlar.Add(ad);
                degerler[ad] = v; return this;
            }

            public IReadOnlyList<string> FieldNames => adlar;
            public bool TryGetInt(string name, out long value) => degerler.TryGetValue(name, out value);
            public bool TryGetNumber(string name, out double value)
            {
                value = 0;
                if (!degerler.TryGetValue(name, out long l)) return false;
                value = l; return true;
            }
            // Bu ekranın gönderdiği tek aksiyonun dört alanı da tamsayı; metin/bool yolu
            // BİLEREK boş döner — sahte bir değer üretmek şema kapısını yanıltırdı.
            public bool TryGetText(string name, out string value) { value = null; return false; }
            public bool TryGetBool(string name, out bool value) { value = false; return false; }
        }
    }
}
