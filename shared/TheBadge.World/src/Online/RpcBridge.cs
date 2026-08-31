using System;
using System.Collections.Generic;
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
        /// <summary>CB Spec 3: `resultingEvents`. İstemci komutun sonucunu BUNUNLA uygular;
        /// olmadığında tanımsız bir tam/delta çekimi yapmak zorunda kalırdı (inceleme bulgusu, P1).
        /// Reddedilen komutta BOŞ — reddin olayı olmaz.</summary>
        public readonly IReadOnlyList<WorldEvent> Olaylar;
        public bool Ok => Sebep == RejectionReason.None;

        public KomutYaniti(RejectionReason sebep, string detay, bool tekrar, ulong yeniStateVersion,
                           IReadOnlyList<WorldEvent> olaylar)
        { Sebep = sebep; Detay = detay; Tekrar = tekrar; YeniStateVersion = yeniStateVersion; Olaylar = olaylar; }
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
    /// KRİTİK 1: pompanın BAŞARISIZLIĞI komutu BAŞARISIZ YAPMAZ. Durum commit edilmiştir ve yayın
    /// outbox'ta DURUR; pompa bir sonraki turda yeniden dener. Pompa hatasında komutu reddetmek,
    /// outbox'ın çözdüğü bağımlılığı geri kurardı.
    ///
    /// KRİTİK 2 — TESLİM İSTEK YOLUNDA DEĞİLDİR (inceleme bulgusu, P1). İlk yazımda `Gonder`
    /// pompayı SENKRON çağırıyordu: ağ yavaşsa ya da asılıysa, ÇOKTAN COMMIT EDİLMİŞ bir komutun
    /// yanıtı yayın kanalını bekliyordu. Yani outbox'ın kaldırdığı geri-alma bağımlılığı yerine
    /// GECİKME bağımlılığı duruyordu ve CB Spec'in "Hub RTT ≤ 300 ms (p95)" hedefi yayın kanalının
    /// sağlığına bağlanıyordu. Hatanın ironisi, ayrımı anlatan yorumun hemen altında olmasıydı.
    ///
    /// Teslimi HOST sürer: `PompayiSur` arka plan döngüsünden çağrılır. `Gonder` yalnız outbox'a
    /// yazılmış olanı bırakır ve döner. `BekleyenTeslimVar` host'a "sürecek iş var" der; bu bir
    /// ZORUNLULUK değil bir İPUCUdur — host pompayı periyodik sürmek zorundadır, çünkü süreç
    /// ölümünden sonra kalan kayıtları yalnız o boşaltır.</summary>
    public sealed class RpcKopru : IKomutTasima
    {
        readonly CommandBus.CommandBus bus;
        readonly WorldExecutor exec;
        readonly OutboxPompasi pompa;
        readonly int turBasinaTeslim;
        readonly OlayOnbellegi onbellek;

        public string SonPompaDetayi { get; private set; }
        public int ToplamTeslim { get; private set; }

        /// <summary>Host'a ipucu: outbox'ta sürülmeyi bekleyen kayıt var mı. Host bunu beklemeden
        /// de pompayı periyodik sürmelidir — süreç ölümünden sonra kalanlar buradan görünmez.</summary>
        public bool BekleyenTeslimVar => bekleyenSayaci != null && bekleyenSayaci() > 0;
        readonly Func<int> bekleyenSayaci;

        public RpcKopru(CommandBus.CommandBus bus, WorldExecutor exec, OutboxPompasi pompa = null,
                        int turBasinaTeslim = 32, Func<int> bekleyenSayaci = null,
                        long olayPenceresiMs = 24L * 60 * 60 * 1000)
        {
            this.bus = bus ?? throw new ArgumentNullException(nameof(bus));
            this.exec = exec ?? throw new ArgumentNullException(nameof(exec));
            this.pompa = pompa;
            this.turBasinaTeslim = turBasinaTeslim;
            this.bekleyenSayaci = bekleyenSayaci;
            onbellek = new OlayOnbellegi(olayPenceresiMs);
            exec.OlayKanaliBagla(onbellek);
        }

        /// <summary>Komut olaylarını (kullanıcı, CommandId) anahtarıyla dedup penceresi boyunca
        /// tutar. Anahtarda KULLANICI da vardır — `IdempotencyStore` ile aynı gerekçe: yalnız
        /// `CommandId` anahtarı, başka bir oturumun olaylarını sızdırırdı.</summary>
        sealed class OlayOnbellegi : IKomutOlaySinki
        {
            static readonly WorldEvent[] Bos = new WorldEvent[0];
            readonly object kilit = new object();
            // ANAHTAR (KULLANICI, CommandId) — yalnız `CommandId` DEĞİL. İlk yazımda bu gerekçeyi
            // yorumda yazıp sözlüğü tek anahtarla kurmuşum (inceleme bulgusu, Bugbot): yorum bir
            // güvenlik özelliğini anlatıyor, kod onu uygulamıyordu. Aynı Id'yi kullanan başka bir
            // oturum ötekinin `resultingEvents`ini alabilir ya da üzerine yazabilirdi.
            readonly Dictionary<(long user, Guid id), (WorldEvent[] olaylar, long at)> kayit
                = new Dictionary<(long, Guid), (WorldEvent[], long)>();
            readonly long pencereMs;
            long sonBudama = long.MinValue;
            public OlayOnbellegi(long pencereMs) { this.pencereMs = pencereMs; }

            public void Yaz(Guid commandId, long userId, long anUnixMs, IReadOnlyList<WorldEvent> olaylar)
            {
                var kopya = new WorldEvent[olaylar?.Count ?? 0];
                for (int i = 0; i < kopya.Length; i++) kopya[i] = olaylar[i];
                lock (kilit) kayit[(userId, commandId)] = (kopya, anUnixMs);
            }

            /// <summary>Süresi DOLMUŞ kayıt döndürülmez. Budama amorti edilmiştir (her çağrıda
            /// koşmaz), dolayısıyla okuma anında budanmamış eski bir kayıt bulunabilir; sürenin
            /// burada da denetlenmesi, "budama henüz koşmadı" halinde bayat olay dönmesini
            /// engeller (inceleme bulgusu, Bugbot).</summary>
            public IReadOnlyList<WorldEvent> AlVeyaBos(long userId, Guid commandId, long now)
            {
                lock (kilit)
                {
                    if (!kayit.TryGetValue((userId, commandId), out var k)) return Bos;
                    if (now - k.at >= pencereMs) { kayit.Remove((userId, commandId)); return Bos; }
                    return k.olaylar;
                }
            }

            public void Buda(long now)
            {
                lock (kilit)
                {
                    if (now - sonBudama < pencereMs / 8) return;   // amorti edilmiş
                    sonBudama = now;
                    var sil = new List<(long, Guid)>();
                    foreach (var kv in kayit) if (now - kv.Value.at >= pencereMs) sil.Add(kv.Key);
                    for (int i = 0; i < sil.Count; i++) kayit.Remove(sil[i]);
                }
            }
        }

        public KomutYaniti Gonder(CommandEnvelope zarf, IPayloadView yuk, long userId, long nowUnixMs)
        {
            var sonuc = bus.Submit(zarf, yuk, exec, nowUnixMs, userId);

            // StateVersion yürütücüden OKUNUR, bus'tan değil: tekrar yanıtında da güncel sürüm
            // döner, çünkü istemcinin ihtiyacı "benim komutum ne yaptı" değil "durum şu an nerede".
            ulong sv = exec.StateVersion;

            // OLAYLAR: yürütmede taze üretilenler, TEKRARDA önbellekten (CB 8.1 "önceki yanıt
            // AYNEN döner" — durumu yalnız statüden ibaret saymak, tekrar eden istemciyi olaysız
            // bırakırdı). Anahtar (KULLANICI, CommandId); süresi dolmuş kayıt DÖNMEZ.
            var olaylar = onbellek.AlVeyaBos(zarf.UserId, zarf.CommandId, nowUnixMs);
            onbellek.Buda(nowUnixMs);

            // TESLİM BURADA YAPILMAZ — bkz. sınıf yorumundaki KRİTİK 2.
            return new KomutYaniti(sonuc.Reason, sonuc.Detail, sonuc.Replayed, sv, olaylar);
        }

        /// <summary>TESLİMİN TEK YOLU. Host bunu arka plan döngüsünden sürer; komut akışıyla
        /// arasında bağ yoktur. Süreç öldükten sonra outbox'ta kalan kayıtlar da yalnız buradan
        /// boşalır — yeni bir komut gelmesini beklemek, kayıtları rehin bırakırdı.</summary>
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
