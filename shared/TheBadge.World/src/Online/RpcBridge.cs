using System;
using TheBadge.CommandBus;
using TheBadge.Sim.Commands;

namespace TheBadge.World
{
    /// <summary>`command.submit(zarf)` yanıtı — CB 3. bölümdeki Nakama RPC'sinin dönüşü.
    ///
    /// `YeniStateVersion` CB 8.2'nin şartıdır: "her yanıt newStateVersion döndürür; istemci eski
    /// versiyonla ekran gösteriyorsa delta sync tetiklenir". `CommandOutcome` bu alanı TAŞIMIYOR
    /// (bus durum katmanını tanımaz, tanımamalı da) — köprü katmanı yürütücüden okuyup ekler.</summary>
    public readonly struct KomutYaniti
    {
        public readonly RejectionReason Sebep;
        public readonly string Detay;
        public readonly bool Tekrar;             // CB 8.1: yanıt dedup deposundan mı geldi
        public readonly ulong YeniStateVersion;  // CB 8.2
        public bool Ok => Sebep == RejectionReason.None;

        public KomutYaniti(RejectionReason sebep, string detay, bool tekrar, ulong yeniStateVersion)
        { Sebep = sebep; Detay = detay; Tekrar = tekrar; YeniStateVersion = yeniStateVersion; }
    }

    /// <summary>TAŞIMA DİKİŞİ. Nakama'ya bağımlılık BURADA biter: sunucu tarafı bu arayüzü bir
    /// Nakama RPC kaydına, testler bellek içi bir çağrıya bağlar. `TheBadge.World` hiçbir ağ
    /// paketine referans vermez (CLAUDE.md: çekirdek bağımlılıksız kalır).</summary>
    public interface IKomutTasima
    {
        KomutYaniti Gonder(CommandEnvelope zarf, IPayloadView yuk, long userId, long nowUnixMs);
    }

    /// <summary>RPC köprüsü — `command.submit` akışının SUNUCU tarafı.
    ///
    /// Zincir: bus (4 kapı + CB 8.1 dedup) → `WorldExecutor` (Tek Kapı, atomik commit) →
    /// outbox pompası (teslim) → `KomutYaniti` (+ newStateVersion).
    ///
    /// KRİTİK: pompanın BAŞARISIZLIĞI komutu BAŞARISIZ YAPMAZ. Durum commit edilmiştir ve yayın
    /// outbox'ta DURUR; pompa bir sonraki turda yeniden dener. Pompa hatasında komutu reddetmek,
    /// outbox'ın çözdüğü bağımlılığı geri kurardı — yayın kanalının sağlığı komutun sonucunu
    /// belirlemeye devam ederdi. Teslim edilememiş kayıt sayısı yanıtta değil TELEMETRİDE
    /// izlenir; kullanıcıya "komutun başarısız" demek yanlış olurdu, çünkü değildi.</summary>
    public sealed class RpcKopru : IKomutTasima
    {
        readonly CommandBus.CommandBus bus;
        readonly WorldExecutor exec;
        readonly OutboxPompasi pompa;
        readonly int turBasinaTeslim;

        public string SonPompaDetayi { get; private set; }
        public int ToplamTeslim { get; private set; }

        public RpcKopru(CommandBus.CommandBus bus, WorldExecutor exec, OutboxPompasi pompa = null,
                        int turBasinaTeslim = 32)
        {
            this.bus = bus ?? throw new ArgumentNullException(nameof(bus));
            this.exec = exec ?? throw new ArgumentNullException(nameof(exec));
            this.pompa = pompa;
            this.turBasinaTeslim = turBasinaTeslim;
        }

        public KomutYaniti Gonder(CommandEnvelope zarf, IPayloadView yuk, long userId, long nowUnixMs)
        {
            var sonuc = bus.Submit(zarf, yuk, exec, nowUnixMs, userId);

            // StateVersion yürütücüden OKUNUR, bus'tan değil: tekrar yanıtında da güncel sürüm
            // döner, çünkü istemcinin ihtiyacı "benim komutum ne yaptı" değil "durum şu an nerede".
            ulong sv = exec.StateVersion;

            if (pompa != null)
            {
                ToplamTeslim += pompa.Sur(turBasinaTeslim, out string takili);
                SonPompaDetayi = takili;   // null = pompa temiz
            }
            return new KomutYaniti(sonuc.Reason, sonuc.Detail, sonuc.Replayed, sv);
        }

        /// <summary>Pompayı komut akışından BAĞIMSIZ sürmek için (arka plan görevi / yeniden
        /// başlatma sonrası kurtarma). Süreç öldükten sonra outbox'ta kalan kayıtlar yalnız
        /// buradan boşalır — yeni bir komut gelmesini beklemek, kayıtları rehin bırakırdı.</summary>
        public int PompayiSur(out string takiliDetay)
        {
            if (pompa == null) { takiliDetay = "pompa bagli degil"; return 0; }
            int n = pompa.Sur(turBasinaTeslim, out takiliDetay);
            ToplamTeslim += n;
            SonPompaDetayi = takiliDetay;
            return n;
        }
    }
}
