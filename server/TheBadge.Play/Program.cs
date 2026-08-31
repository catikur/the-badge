using System;
using System.Collections.Generic;
using TheBadge.CommandBus;
using TheBadge.Sim.Commands;
using TheBadge.Sim.Match;
using TheBadge.World;
using TheBadge.Play;

// ============================================================================================
// THE BADGE — OYNANABİLİR KONSOL (K11)
//
// NE OLDUĞU: FAZ 03/04'te kurulan GERÇEK motorun ilk oynanabilir yüzü. Sentetik hiçbir şey yok:
//   · senin maçın TAM MOTOR (LOD 0, 90 dakika, tick tick) — `MatchEngine`
//   · ligin kalan 9 maçı LOD 2 regresyonu — `Lod2Resolver` (ME 16.4'ün öngördüğü karışım)
//   · kadro → sahaya çıkan 11 `SquadBridge` (K11 dikişi)
//   · her yönetim eylemi Tek Kapı'dan — `CommandBus` 4 kapı + `WorldExecutor` atomik commit
//   · hafta sonu ekonomisi `EconomyTick` (ECONOMY_MAP source/sink)
//
// NE OLMADIĞI: bu bir UI değil, bir DOĞRULAMA YÜZEYİdir. FAZ 02 ekranları bunun yerine geçecek.
// Buradaki hiçbir sayı kendi kafasından gelmiyor; hepsi `balance/` altındaki [KALİBRE] dosyalardan.
//
// KULLANIM:
//   dotnet run --project server/TheBadge.Play                 → oyna
//   dotnet run --project server/TheBadge.Play -- --oto 38     → 38 haftayı otomatik oyna
//   dotnet run --project server/TheBadge.Play -- --seed 7     → başka bir lig/sezon çekirdeği
// ============================================================================================

static string RepoDosya(string rel)
{
    var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
    for (int up = 0; up < 10 && dir != null; up++, dir = dir.Parent)
    {
        string p = System.IO.Path.Combine(dir.FullName, rel);
        if (System.IO.File.Exists(p)) return p;
    }
    throw new System.IO.FileNotFoundException(rel);
}

int otoHafta = 0; ulong seed = 20260831UL; string kulupAdi = "Gölbaşı Şafak";   // lig adlarıyla ÇAKIŞMAZ (bkz. LigKurucu.Kur)
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--oto" && i + 1 < args.Length && int.TryParse(args[i + 1], out int n)) otoHafta = n;
    else if (args[i] == "--seed" && i + 1 < args.Length && ulong.TryParse(args[i + 1], out ulong s)) seed = s;
    else if (args[i] == "--kulup" && i + 1 < args.Length) kulupAdi = args[i + 1];
}
bool oto = otoHafta > 0;

// ------------------------------------------------------------------ BALANCE (tek kaynak: repo)
var jopt = new System.Text.Json.JsonSerializerOptions { IncludeFields = true, PropertyNameCaseInsensitive = true };
T Yukle<T>(string rel) => System.Text.Json.JsonSerializer.Deserialize<T>(System.IO.File.ReadAllText(RepoDosya(rel)), jopt);

var simBal = Yukle<TheBadge.Sim.Config.SimBalance>("balance/sim.balance.json");
var lod2Tbl = Yukle<TheBadge.Sim.Config.Lod2Table>("balance/sim.lod2.json");
var kural = Yukle<WorldRules>("balance/world.balance.json"); kural.Validate();
var eko = Yukle<EconomyBalance>("balance/economy.balance.json"); eko.Validate();
var sqBal = Yukle<SquadBalance>("balance/squad.balance.json"); sqBal.Validate();

var bantlar = new Bantlar();
var rlCfg = new Dictionary<RateClass, RateLimitCfg[]>();
using (var bd = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(RepoDosya("balance/command.bands.json"))))
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

// ------------------------------------------------------------------ OYUNCUNUN KULÜBÜ (dünya)
const long KULUP = 1L, KULLANICI = 42L;
var st = KulupKur(kural, eko, KULUP, KULLANICI);
var depo = new WorldStore(st);
var ctx = new WorldContext(depo, kural) { Active = Context.Hub };
var exec = new WorldExecutor(depo, ctx);
TycoonActions.Baglan(ctx, exec, eko);
var bus = new CommandBus(bantlar, ctx, new SlidingWindowRateLimiter(rlCfg, 8, 300_000), new IdempotencyStore());
long host = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

// ------------------------------------------------------------------ LİG
byte oyuncuGucu = 0;
{
    int t = 0, c = 0;
    for (int i = 0; i < st.Oyuncular.Length; i++) if (st.Oyuncular[i].ClubId == KULUP) { t += st.Oyuncular[i].Guc; c++; }
    oyuncuGucu = (byte)(c == 0 ? 60 : t / c);
}
var kulupler = LigKurucu.Kur(kulupAdi, sqBal, oyuncuGucu);
var fikstur = LigKurucu.Fikstur(LigKurucu.KulupSayisi);
var lod2 = new Lod2Resolver(simBal, lod2Tbl);

Yaz();
Yaz("  ╔═══════════════════════════════════════════════════════════════╗");
Yaz("  ║   T H E   B A D G E   —   Football Club Tycoon                ║");
Yaz("  ║   oynanabilir çekirdek · gerçek motor · gerçek Tek Kapı       ║");
Yaz("  ╚═══════════════════════════════════════════════════════════════╝");
Yaz($"  Kulüp: {kulupAdi}   ·   lig {LigKurucu.KulupSayisi} takım · {kural.yapi.sezonHaftaSayisi} hafta");
Yaz($"  Katalog: {Catalog.Count} aksiyon · bağlanmamış {exec.UnboundActions().Length}   ·   seed 0x{seed:X}");
Yaz();

// ------------------------------------------------------------------ SEZON DÖNGÜSÜ
int hafta = 1, oynanan = 0;
bool cik = false;
while (!cik && hafta <= kural.yapi.sezonHaftaSayisi)
{
    if (oto && oynanan >= otoHafta) break;
    var benim = BenimMac(hafta);
    if (benim.Ev < 0) { hafta++; continue; }

    if (!oto)
    {
        while (true)
        {
            Hub(hafta, benim);
            string sec = (Console.ReadLine() ?? "q").Trim().ToLowerInvariant();
            if (sec == "q") { cik = true; break; }
            if (sec == "m" || sec == "") break;
            Menu(sec);
        }
        if (cik) break;
    }

    HaftayiOyna(hafta, benim);
    hafta++; oynanan++;
}

Yaz();
Yaz("  ── SEZON DURUMU ────────────────────────────────────────────────");
PuanTablosu(8);
Yaz();
Yaz($"  Kasa: {Para(st.Club.KasaTl)}  ·  stadyum {st.Club.StadyumKapasite:N0}  ·  " +
    $"hafta {st.Takvim.Hafta}/{kural.yapi.sezonHaftaSayisi} · sezon {st.Takvim.Sezon}");
Yaz($"  Durum özeti (WorldHash): 0x{WorldHash.Compute(st):X16}   ← aynı girdi = aynı dünya");
Yaz();

// ============================================================ YARDIMCILAR

void Yaz(string s = "") => Console.WriteLine(s);
string Para(long tl) => tl >= 1_000_000 || tl <= -1_000_000 ? $"{tl / 1e6:F1}M₺" : $"{tl / 1e3:F0}K₺";

Mac BenimMac(int h)
{
    foreach (var m in fikstur) if (m.Hafta == h && (m.Ev == 0 || m.Dep == 0)) return m;
    return new Mac { Hafta = h, Ev = -1, Dep = -1 };
}

void Hub(int h, Mac benim)
{
    bool evde = benim.Ev == 0;
    var rakip = kulupler[evde ? benim.Dep : benim.Ev];
    Yaz();
    Yaz($"  ┌── SEZON {st.Takvim.Sezon} · HAFTA {h} ─────────────────────────────────┐");
    Yaz($"  │ Kasa {Para(st.Club.KasaTl),-10} Form {st.Club.Form,-4} Kadro {KadroSayisi(),-3} " +
        $"Stadyum {st.Club.StadyumKapasite,-7:N0}│");
    Yaz($"  │ Sıradaki: {(evde ? "EV" : "DEP")}  {rakip.Ad,-24} (güç ~{rakip.GucTaban})     │");
    Yaz($"  └──────────────────────────────────────────────────────────┘");
    Yaz("   1) Kadro      2) Bilet fiyatı   3) Kombine     4) Büfe/Mağaza");
    Yaz("   5) İnşaat     6) Kredi          7) Puan durumu 8) Fikstür");
    Yaz("   [M] maçı oyna      [Q] çık");
    Console.Write("  > ");
}

int KadroSayisi()
{
    int c = 0;
    for (int i = 0; i < st.Oyuncular.Length; i++) if (st.Oyuncular[i].ClubId == KULUP) c++;
    return c;
}

void Menu(string sec)
{
    switch (sec)
    {
        case "1": Kadro(); break;
        case "2": BiletFiyati(); break;
        case "3": Kombine(); break;
        case "4": BufeMagaza(); break;
        case "5": Insaat(); break;
        case "6": Kredi(); break;
        case "7": PuanTablosu(LigKurucu.KulupSayisi); break;
        case "8": Fikstur(); break;
        default: Yaz("  ? bilinmeyen seçim"); break;
    }
}

void Kadro()
{
    var sheet = SquadBridge.Kur(st, KULUP, sqBal, true, out string h);
    Yaz();
    if (sheet == null) { Yaz($"  Kadro sahaya çıkamaz: {h}"); return; }
    Yaz($"  ── İLK 11 ({sqBal.dizilis.ad}) ──────────────────────────────────");
    string[] hatAd = { "KL", "DF", "OS", "FV" };
    for (int i = 0; i < 11; i++)
    {
        var e = sheet.Starters[i];
        int hat = sqBal.rolHat[e.RoleId - 1];
        byte guc = 0;
        for (int k = 0; k < st.Oyuncular.Length; k++) if (st.Oyuncular[k].PlayerId == e.PlayerId) guc = st.Oyuncular[k].Guc;
        Yaz($"   {hatAd[hat]}  #{e.PlayerId,-4} güç {guc,3}   pas {e.Attributes.Passing,3} " +
            $"bitiricilik {e.Attributes.Finishing,3} müdahale {e.Attributes.Tackling,3} hız {e.Attributes.Pace,3}");
    }
    Yaz($"   Yedek: {sheet.Bench.Length} kişi   ·   motor okuması (takım gücü): {lod2.TeamStrength(sheet):F1}");
}

double SayiSor(string soru, double varsayilan)
{
    Console.Write($"  {soru} [{varsayilan:F0}]: ");
    string s = Console.ReadLine();
    return double.TryParse(s, System.Globalization.NumberStyles.Any,
                           System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : varsayilan;
}

void BiletFiyati()
{
    string[] tribun = { "kuzey", "guney", "dogu", "bati", "vip" };
    Yaz();
    for (int i = 0; i < 5; i++) Yaz($"   {i + 1}) {tribun[i],-7} şu an {st.Fiyat.BiletKurus[i] / 100.0,7:F0} ₺");
    int t = (int)SayiSor("hangi tribün (1-5)", 1) - 1;
    if (t < 0 || t > 4) { Yaz("  ? tribün yok"); return; }
    double f = SayiSor($"{tribun[t]} yeni fiyat", st.Fiyat.BiletKurus[t] / 100.0);
    Komut("tycoon.set_ticket_price", new Yuk().Koy("tribun", tribun[t]).Koy("fiyat", f));
}

void Kombine()
{
    double f = SayiSor("kombine fiyatı", st.Fiyat.KombineKurus / 100.0);
    Komut("tycoon.set_season_ticket_price", new Yuk().Koy("fiyat", f));
}

void BufeMagaza()
{
    Yaz("   1) büfe (yiyecek)   2) mağaza (forma)");
    if ((int)SayiSor("hangisi", 1) == 2)
        Komut("tycoon.set_merch_price", new Yuk().Koy("urun", "forma")
              .Koy("fiyat", SayiSor("forma fiyatı", st.Fiyat.MagazaKurus[0] / 100.0)));
    else
        Komut("tycoon.set_concession_price", new Yuk().Koy("urun", "yiyecek")
              .Koy("fiyat", SayiSor("yiyecek fiyatı", st.Fiyat.BufeKurus[0] / 100.0)));
}

void Insaat()
{
    Yaz();
    Yaz("   Tesisler (1 = stadyum):");
    for (int i = 1; i <= 5; i++)
    {
        int mevcut = st.Club.TesisTier[i];
        long m = mevcut < 5 ? eko.TierMaliyet(mevcut + 1) : 0;
        Yaz($"    {i}) tesis {i}  tier {mevcut}" + (mevcut < 5 ? $" → {mevcut + 1} : {Para(m)}" : "  (tavan)"));
    }
    int t = (int)SayiSor("hangi tesis (1-5)", 1);
    if (t < 1 || t > 5) { Yaz("  ? tesis yok"); return; }
    Komut("tycoon.start_construction", new Yuk().Koy("tesisId", (long)t).Koy("hedefTier", (long)(st.Club.TesisTier[t] + 1)));
}

void Kredi()
{
    double miktar = SayiSor("kredi miktarı ₺", 1_000_000);
    double vade = SayiSor("vade (ay)", 24);
    Komut("tycoon.take_loan", new Yuk().Koy("miktar", miktar).Koy("vadeAy", (long)vade));
}

// TEK KAPI: her yönetim eylemi buradan geçer. Red sebebi GİZLENMEZ — 4 kapının hangisinin
// kapattığını görmek, sistemin çalıştığını görmenin kendisidir.
void Komut(string aksiyon, Yuk yuk)
{
    host += 1000;
    var zarf = new CommandEnvelope
    {
        CommandId = Guid.NewGuid(), CatalogVersion = Catalog.Version, Source = CommandSource.UI,
        ActionType = aksiyon, IssuedAtUnixMs = host, MatchTick = 0, UserId = KULLANICI,
        SaveSlotId = 1, TeamIdx = 0, PayloadJson = new byte[0]
    };
    var o = bus.Submit(zarf, yuk, exec, host, KULLANICI);
    Yaz(o.Ok ? $"  ✓ {aksiyon} uygulandı  (kasa {Para(st.Club.KasaTl)})"
             : $"  ✗ {aksiyon} REDDEDİLDİ — {o.Reason}{(o.Detail == null ? "" : ": " + o.Detail)}");
}

void Fikstur()
{
    Yaz();
    Yaz("  ── SIRADAKİ 6 MAÇIN ────────────────────────────────────────────");
    int g = 0;
    foreach (var m in fikstur)
    {
        if (m.Hafta < hafta || (m.Ev != 0 && m.Dep != 0)) continue;
        bool evde = m.Ev == 0;
        Yaz($"   H{m.Hafta,2}  {(evde ? "EV " : "DEP")}  {kulupler[evde ? m.Dep : m.Ev].Ad}");
        if (++g >= 6) break;
    }
}

void PuanTablosu(int kac)
{
    var s = LigKurucu.PuanDurumu(kulupler);
    Yaz();
    Yaz("   #  Takım                     O   G  B  M    A   Y   Av   P");
    Yaz("   ─────────────────────────────────────────────────────────────");
    // OYUNCUNUN SATIRI HER ZAMAN GÖSTERİLİR. Kısaltılmış tabloda kendi sıranı görememek,
    // tablonun tek işlevini ("ben neredeyim") yok ediyordu — ilk tam sezon koşusunda oldu.
    int benimSira = -1;
    for (int i = 0; i < s.Length; i++) if (ReferenceEquals(s[i], kulupler[0])) benimSira = i;
    for (int i = 0; i < s.Length; i++)
    {
        bool benim = i == benimSira;
        if (i >= kac && !benim) continue;
        if (i >= kac && benim) Yaz("        ⋮");
        var c = s[i];
        Yaz($"  {(benim ? "►" : " ")}{i + 1,2}  {c.Ad,-24}{c.O,3} {c.G,3}{c.B,3}{c.M,3} {c.AG,4}{c.YG,4} {c.Averaj,4} {c.Puan,3}");
    }
}

// ---------------------------------------------------------------- HAFTA: MAÇ + LİG + EKONOMİ
void HaftayiOyna(int h, Mac benim)
{
    bool evde = benim.Ev == 0;
    var benimKulup = kulupler[0];
    var rakip = kulupler[evde ? benim.Dep : benim.Ev];

    var benimSheet = SquadBridge.Kur(st, KULUP, sqBal, evde, out string kh);
    if (benimSheet == null) { Yaz($"  Maça çıkılamadı: {kh}"); return; }
    var rakipSheet = evde ? rakip.Deplasman : rakip.Ev;

    // SENİN MAÇIN: TAM MOTOR (LOD 0) — 90 dakika, 100 ms'lik tick'ler.
    ulong macSeed = seed ^ ((ulong)h << 32) ^ (ulong)rakip.Id;
    var cfg = new MatchConfig
    {
        Seed = macSeed, EngineVersion = "k11-play",
        Home = evde ? benimSheet : rakipSheet,
        Away = evde ? rakipSheet : benimSheet,
        Referee = RefereeProfile.Default, Lod = LodLevel.Lod0
    };
    var eng = new MatchEngine(macSeed, new CommandQueue(), cfg, simBal) { AutoManage = true };
    var ms = MatchEngine.CreateInitialState(cfg);
    var sonuc = eng.Run(ref ms);
    var pkt = eng.BuildSummary(in ms);

    string evAd = evde ? benimKulup.Ad : rakip.Ad;
    string depAd = evde ? rakip.Ad : benimKulup.Ad;
    Yaz();
    Yaz($"  ╭─ HAFTA {h} ────────────────────────────────────────────────╮");
    Yaz($"  │  {evAd,26}  {sonuc.HomeGoals} - {sonuc.AwayGoals}  {depAd,-26}│");
    Yaz($"  ╰──────────────────────────────────────────────────────────╯");

    // ZAMAN ÇİZELGESİ — K10-C'nin işi: eşikten değil, EN YÜKSEK N andan.
    if (pkt.TimelineMarks.Length > 0)
    {
        Yaz("   Öne çıkan anlar:");
        var sirali = new List<MatchEvent>(pkt.TimelineMarks);
        sirali.Sort((a, b) => a.Tick.CompareTo(b.Tick));
        foreach (var e in sirali)
            Yaz($"    {e.Minute,3}'  {Olay(e.Kind),-18} {(e.TeamIdx == 0 ? evAd : e.TeamIdx == 1 ? depAd : "")}");
    }
    Yaz($"   Şut {pkt.Home.Shots}-{pkt.Away.Shots} · isabet {pkt.Home.ShotsOnTarget}-{pkt.Away.ShotsOnTarget} · " +
        $"xG {pkt.Home.Xg:F2}-{pkt.Away.Xg:F2} · korner {pkt.Home.Corners}-{pkt.Away.Corners} · " +
        $"kart {pkt.Home.Yellows}/{pkt.Home.Reds}-{pkt.Away.Yellows}/{pkt.Away.Reds} · " +
        $"topla oynama %{pkt.Home.PossessionPct:F0}-%{pkt.Away.PossessionPct:F0}");

    LigKurucu.SonucIsle(kulupler[benim.Ev], kulupler[benim.Dep], sonuc.HomeGoals, sonuc.AwayGoals);
    var benimSonuc = evde
        ? (sonuc.HomeGoals > sonuc.AwayGoals ? WeekResult.Galibiyet : sonuc.HomeGoals == sonuc.AwayGoals ? WeekResult.Beraberlik : WeekResult.Maglubiyet)
        : (sonuc.AwayGoals > sonuc.HomeGoals ? WeekResult.Galibiyet : sonuc.HomeGoals == sonuc.AwayGoals ? WeekResult.Beraberlik : WeekResult.Maglubiyet);

    // LİGİN KALANI: LOD 2 — ME 16.4'ün öngördüğü karışım (1 tam maç + kalanı regresyon).
    int lod2Sayisi = 0;
    foreach (var m in fikstur)
    {
        if (m.Hafta != h || m.Ev == 0 || m.Dep == 0) continue;
        var e = kulupler[m.Ev]; var d = kulupler[m.Dep];
        var c2 = new MatchConfig
        {
            Seed = macSeed ^ ((ulong)e.Id << 16) ^ (ulong)d.Id, EngineVersion = "k11-play",
            Home = e.Ev, Away = d.Deplasman, Referee = RefereeProfile.Default, Lod = LodLevel.Lod2
        };
        var r2 = lod2.Run(c2.Seed, c2);
        LigKurucu.SonucIsle(e, d, r2.HomeGoals, r2.AwayGoals);
        lod2Sayisi++;
    }

    // HAFTA SONU EKONOMİSİ — ECONOMY_MAP source/sink
    var j = new WorldJournal();
    var L = EconomyTick.Hafta(st, eko, kural, seed, benimSonuc, evde, j);
    if (!j.Validate(st, out string ej)) { Yaz($"  ! ekonomi journal geçersiz: {ej}"); return; }
    j.Apply(st);

    Yaz($"   Seyirci {L.Seyirci:N0} · bilet {Para(L.BiletTl)} büfe {Para(L.BufeTl)} mağaza {Para(L.MagazaTl)} " +
        $"prim {Para(L.PrimTl)} | maaş {Para(L.MaasTl)} bakım {Para(L.BakimTl)} → hafta neti {Para(L.NetTl)}");
    Yaz($"   Kasa: {Para(st.Club.KasaTl)}   ·   ligin kalanı LOD 2 ile çözüldü ({lod2Sayisi} maç)");
    if (st.Club.KasaTl <= eko.iflas.esikTl) Yaz("   ⚠ İFLAS EŞİĞİ AŞILDI");
}

static string Olay(EventType t)
{
    switch (t)
    {
        case EventType.Goal: return "GOL";
        case EventType.ShotOnTarget: return "isabetli şut";
        case EventType.ShotOffTarget: return "auta şut";
        case EventType.ShotBlocked: return "blok";
        case EventType.Save: return "kurtarış";
        case EventType.Parry: return "çeldi";
        case EventType.Post: return "direk";
        case EventType.BigChanceMissed: return "kaçan fırsat";
        case EventType.PenaltyAwarded: return "PENALTI";
        case EventType.RedCard: return "KIRMIZI KART";
        case EventType.YellowCard: return "sarı kart";
        case EventType.CornerAwarded: return "korner";
        case EventType.FreeKickAwarded: return "serbest vuruş";
        case EventType.VarDecision: return "VAR kararı";
        default: return t.ToString();
    }
}

// Oyuncunun kulübü: 18 kişilik rollü kadro + referans seviyesinde fiyatlar.
// TÜM sayılar balance'tan; buradaki tek "içerik" kararı kadro büyüklüğü ve rol dağılımıdır.
static GameState KulupKur(WorldRules kural, EconomyBalance eko, long clubId, long userId)
{
    var st = GameState.Olustur(kural, clubId, userId);
    st.Club.KasaTl = 20_000_000;
    st.Club.StadyumKapasite = eko.insaat.kapasiteTier[3];
    st.Club.Form = 50;
    st.Club.TesisTier[1] = 3;
    for (int i = 2; i <= 5; i++) st.Club.TesisTier[i] = 2;

    var list = new List<PlayerState>();
    byte[] roller = { 1, 1, 2, 3, 4, 5, 6, 7, 9, 10, 11, 12, 13, 14, 21, 22, 23, 24 };
    int pid = 100;
    for (int i = 0; i < roller.Length; i++, pid++)
    {
        byte guc = (byte)(55 + (pid * 11) % 22);          // 55-76: yenilebilir ama zayıf bir kadro
        list.Add(new PlayerState
        {
            PlayerId = pid, ClubId = clubId, HaftalikMaasTl = 70_700,
            SozlesmeKalanHafta = (ushort)(60 + (pid * 7) % 100), Moral = 60, Kondisyon = 90,
            RolId = roller[i], Guc = guc, Potansiyel = (byte)Math.Min(99, guc + 8),
            Yas = (byte)(19 + (pid * 5) % 17),
            Talimatlar = new Instruction[kural.yapi.talimatYuvaSayisi]
        });
    }
    st.Oyuncular = list.ToArray();
    st.Club.HaftalikMaasGiderTl = list.Count * 70_700L;
    for (int t = 0; t < 5; t++) st.Fiyat.BiletKurus[t] = eko.tribun.referansFiyat[t] * 100;
    st.Fiyat.KombineKurus = (int)(eko.kombine.referansFiyat * 100);
    for (int i = 0; i < 3; i++) st.Fiyat.BufeKurus[i] = (int)(eko.macGunu.bufeReferansFiyat * 100);
    for (int i = 0; i < 3; i++) st.Fiyat.MagazaKurus[i] = (int)(eko.macGunu.magazaReferansFiyat * 100);
    st.Validate();
    return st;
}

// --- host tarafı yardımcıları (çekirdeğe SIZMAZ) ---------------------------------------------
sealed class Bantlar : IBandProvider
{
    readonly Dictionary<string, (double min, double max)> b = new();
    public void Ekle(string ad, double min, double max) => b[ad] = (min, max);
    public bool TryGetBand(string k, out double min, out double max)
    { if (b.TryGetValue(k, out var v)) { min = v.min; max = v.max; return true; } min = max = 0; return false; }
}

sealed class Yuk : IPayloadView
{
    readonly Dictionary<string, object> d = new();
    public Yuk Koy(string k, object v) { d[k] = v; return this; }
    public bool TryGetNumber(string k, out double v)
    { v = 0; if (!d.TryGetValue(k, out var o)) return false; if (o is double dd) { v = dd; return true; }
      if (o is long l) { v = l; return true; } return false; }
    public bool TryGetInt(string k, out long v)
    { v = 0; if (!d.TryGetValue(k, out var o)) return false; if (o is long l) { v = l; return true; }
      if (o is double dd && dd == Math.Floor(dd)) { v = (long)dd; return true; } return false; }
    public bool TryGetText(string k, out string v)
    { v = null; if (!d.TryGetValue(k, out var o) || o is not string s) return false; v = s; return true; }
    public bool TryGetBool(string k, out bool v)
    { v = false; if (!d.TryGetValue(k, out var o) || o is not bool b) return false; v = b; return true; }
    public IReadOnlyList<string> FieldNames => new List<string>(d.Keys);
}
