using System;
using System.Collections.Generic;
using TheBadge.CommandBus;
using TheBadge.Sim.Commands;
using TheBadge.Sim.Determinism;

// SimWorker — `command.submit` akışının SUNUCU tarafını AYAĞA KALDIRIR (K9-C).
//
// Buradaki amaç bir kanıttır: RPC köprüsü + outbox bileşimi test koşumuna hapis DEĞİL, gerçek
// bir uygulama sürecinde de kuruluyor ve çalışıyor. Ağ katmanı YOK — `IKomutTasima`nın Nakama
// adaptörü bu ortamda koşturulamayacağı için yazılmadı (CLAUDE.md: kanıtlanamayan kod eklenmez).
//
// Nakama tarafı bağlandığında değişecek TEK yer: aşağıdaki `kopru.Gonder(...)` çağrısını bir
// RPC kaydının içine almak. Doğrulama zinciri, dedup, atomiklik ve teslim dayanıklılığı
// taşımadan bağımsızdır ve `TheBadge.Sim.Checks` içindeki K9 kapılarıyla ölçülür.

static string RepoDosya(string rel)
{
    var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
    for (int up = 0; up < 8 && dir != null; up++, dir = dir.Parent)
    {
        string p = System.IO.Path.Combine(dir.FullName, rel);
        if (System.IO.File.Exists(p)) return p;
    }
    throw new System.IO.FileNotFoundException(rel);
}

Console.WriteLine("The Badge SimWorker — paylasilan cekirdek + command.submit koprusu");
Console.WriteLine($"  Hash64 ornegi: 0x{Rng.Hash64(1, 1, 1, 1, 1):X}");

var opts = new System.Text.Json.JsonSerializerOptions { IncludeFields = true, PropertyNameCaseInsensitive = true };
string balDir = System.IO.Path.GetDirectoryName(RepoDosya("balance/sim.balance.json"));
var kural = System.Text.Json.JsonSerializer.Deserialize<TheBadge.World.WorldRules>(
    System.IO.File.ReadAllText(System.IO.Path.Combine(balDir, "world.balance.json")), opts);
kural.Validate();
var eko = System.Text.Json.JsonSerializer.Deserialize<TheBadge.World.EconomyBalance>(
    System.IO.File.ReadAllText(System.IO.Path.Combine(balDir, "economy.balance.json")), opts);

// Bantlar + rate limit — komut otobüsünün Kapı 2 ve Kapı 4 girdileri
var bantlar = new SunucuBantlari();
var rlCfg = new Dictionary<RateClass, RateLimitCfg[]>();
using (var bd = System.Text.Json.JsonDocument.Parse(
           System.IO.File.ReadAllText(System.IO.Path.Combine(balDir, "command.bands.json"))))
{
    foreach (var b in bd.RootElement.GetProperty("bantlar").EnumerateObject())
        bantlar.Ekle(b.Name, b.Value[0].GetDouble(), b.Value[1].GetDouble());
    foreach (var r in bd.RootElement.GetProperty("rateLimit").EnumerateObject())
    {
        var l = new List<RateLimitCfg>();
        foreach (var w in r.Value.EnumerateArray()) l.Add(new RateLimitCfg(w[0].GetInt32(), w[1].GetInt64() * 1000));
        rlCfg[(RateClass)Enum.Parse(typeof(RateClass), r.Name)] = l.ToArray();
    }
}

long simdi = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();   // SUNUCU saati — sim çekirdeğinde DEĞİL
const long kullanici = 42L;

var durum = TheBadge.World.GameState.Olustur(kural, clubId: 1L, ownerUserId: kullanici);
var depo = new TheBadge.World.WorldStore(durum);
var ctx = new TheBadge.World.WorldContext(depo, kural) { Active = Context.Hub | Context.Online };
var exec = new TheBadge.World.WorldExecutor(depo, ctx);

var outbox = new TheBadge.World.BellekOutboxStore();   // gerçek sunucuda: durum yazmasıyla AYNI işlemde commit edilen tablo
TheBadge.World.OnlineActions.Baglan(ctx, exec, kural, new TheBadge.World.OutboxSink(outbox, () => simdi));
TheBadge.World.TycoonActions.Baglan(ctx, exec, eko);

var bus = new TheBadge.CommandBus.CommandBus(bantlar, ctx,
    new SlidingWindowRateLimiter(rlCfg, 8, 300_000), new IdempotencyStore());
var agKanali = new LogOnlineSink();
// TESLİM İSTEK YOLUNDA DEĞİL: köprü pompayı çalıştırmaz, HOST sürer (aşağıda). Gerçek sunucuda
// bu, periyodik bir arka plan görevidir; burada tek turluk bir sürüş gösterimi.
var kopru = new TheBadge.World.RpcKopru(bus, exec, new TheBadge.World.OutboxPompasi(outbox, agKanali),
                                        32, () => outbox.BekleyenSayisi);

Console.WriteLine($"  Katalog: {Catalog.Count} aksiyon · baglanmamis {exec.UnboundActions().Length}");

var zarf = new CommandEnvelope
{
    CommandId = Guid.NewGuid(), CatalogVersion = Catalog.Version, Source = CommandSource.UI,
    ActionType = "tycoon.set_season_ticket_price", IssuedAtUnixMs = simdi, MatchTick = 0,
    UserId = kullanici, SaveSlotId = 1, TeamIdx = 0, PayloadJson = new byte[0]
};
var yuk = new SunucuYuku().Koy("fiyat", 1200.0);

var y1 = kopru.Gonder(zarf, yuk, kullanici, simdi);
Console.WriteLine($"  submit#1 → ok={y1.Ok} sebep={y1.Sebep} stateVersion={y1.YeniStateVersion} tekrar={y1.Tekrar}");
var y2 = kopru.Gonder(zarf, yuk, kullanici, simdi);   // AYNI CommandId → CB 8.1
Console.WriteLine($"  submit#2 (ayni CommandId) → ok={y2.Ok} stateVersion={y2.YeniStateVersion} tekrar={y2.Tekrar}");
Console.WriteLine($"  outbox (pompa surulmeden): bekleyen={outbox.BekleyenSayisi} teslim={outbox.TeslimSayisi}");
kopru.PompayiSur(out string pompaDetay);   // HOST'un isi
Console.WriteLine($"  outbox (pompa surulunce): bekleyen={outbox.BekleyenSayisi} teslim={outbox.TeslimSayisi}" +
                  (pompaDetay == null ? "" : $" · takili: {pompaDetay}"));

// --- host tarafı yardımcıları (çekirdeğe SIZMAZ) ---------------------------------------
sealed class SunucuBantlari : IBandProvider
{
    readonly Dictionary<string, (double min, double max)> b = new Dictionary<string, (double, double)>();
    public void Ekle(string ad, double min, double max) => b[ad] = (min, max);
    public bool TryGetBand(string bandKey, out double min, out double max)
    {
        if (b.TryGetValue(bandKey, out var v)) { min = v.min; max = v.max; return true; }
        min = max = 0; return false;
    }
}

sealed class SunucuYuku : IPayloadView
{
    readonly Dictionary<string, object> d = new Dictionary<string, object>();
    public SunucuYuku Koy(string k, object v) { d[k] = v; return this; }
    public bool Has(string key) => d.ContainsKey(key);
    public bool TryGetNumber(string key, out double v)
    { v = 0; if (!d.TryGetValue(key, out var o) || !(o is double dd)) return false; v = dd; return true; }
    public bool TryGetInt(string key, out long v)
    { v = 0; if (!d.TryGetValue(key, out var o)) return false; if (o is long l) { v = l; return true; }
      if (o is double dd && dd == Math.Floor(dd)) { v = (long)dd; return true; } return false; }
    public bool TryGetText(string key, out string v)
    { v = null; if (!d.TryGetValue(key, out var o) || !(o is string s)) return false; v = s; return true; }
    public bool TryGetBool(string key, out bool v)
    { v = false; if (!d.TryGetValue(key, out var o) || !(o is bool b)) return false; v = b; return true; }
    public IReadOnlyList<string> FieldNames { get { var l = new List<string>(d.Keys); return l; } }
}

sealed class LogOnlineSink : TheBadge.World.IOnlineSink
{
    public void KlipPaylas(Guid commandId, int macId, int pencereSn, byte hedef, long userId)
        => Console.WriteLine($"  [ag] klip cid={commandId} mac={macId}");
    public void OyuncuRaporla(Guid commandId, long hedefUserId, byte sebep, string notlar, long userId)
        => Console.WriteLine($"  [ag] rapor cid={commandId} hedef={hedefUserId}");
}
