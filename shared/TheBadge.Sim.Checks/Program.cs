using System;
using TheBadge.Sim.Commands;
using TheBadge.Sim.Core;
using TheBadge.Sim.Determinism;
using TheBadge.Sim.Match;

// Bağımlılıksız determinizm kapısı — CI ve yerel geliştirme her commit öncesi koşar.
// Kural (CLAUDE.md): Bu program yeşil değilse commit YOK.
static int Fail(string name, string msg) { Console.WriteLine($"[FAIL] {name}: {msg}"); return 1; }
static void Pass(string name) => Console.WriteLine($"[PASS] {name}");

int failures = 0;
const ulong SEED = 0xC0FFEE2026UL;

// 1) Golden değer sabitliği: Hash64 platformlar/sürümler arası bit-eşit kalmalı.
const ulong GOLDEN_HASH = 0x4F653F320D0CA523UL; // sabitlendi — platform sapmasi = FAIL
ulong h = Rng.Hash64(SEED, (uint)Domain.Duel, 42, 1000, 7);
Console.WriteLine($"[info] Hash64 ornek deger: 0x{h:X}");
if (GOLDEN_HASH != 0 && h != GOLDEN_HASH)
    failures += Fail("GoldenHash", $"0x{h:X} != 0x{GOLDEN_HASH:X}");
else Pass("GoldenHash");

// 2) Tekrarlanabilirlik: aynı adres = aynı değer (1000 deneme).
bool repOk = true;
for (uint t = 0; t < 1000; t++)
{
    if (Rng.Hash64(SEED, 2, t, t * 3, 5) != Rng.Hash64(SEED, 2, t, t * 3, 5))
    { failures += Fail("Repeatability", $"tick {t}"); repOk = false; break; }
}
if (repOk) Pass("Repeatability(1000)");

// 3) Sıra bağımsızlığı: çağrı sırası değerleri etkilememeli (ME Spec 3.1).
double a1 = Rng.Rand01(SEED, Domain.Decision, 1, 50, 1);
double b1 = Rng.Rand01(SEED, Domain.Decision, 2, 50, 1);
double b2 = Rng.Rand01(SEED, Domain.Decision, 2, 50, 1);
double a2 = Rng.Rand01(SEED, Domain.Decision, 1, 50, 1);
if (a1 != a2 || b1 != b2) failures += Fail("OrderIndependence", "çağrı sırası sonucu değiştirdi");
else Pass("OrderIndependence");

// 4) Dağılım sağlığı: Rand01 ortalama ~0.5; Gauss ortalama ~0, sigma ~1.
double sum = 0, gsum = 0, g2 = 0; const int N = 100000;
bool rangeOk = true;
for (uint i = 0; i < N; i++)
{
    double r = Rng.Rand01(SEED, Domain.Chaos, i, i / 7, 3);
    if (r < 0 || r >= 1) { failures += Fail("Rand01Range", r.ToString()); rangeOk = false; break; }
    sum += r;
    double g = Rng.Gauss01(SEED, Domain.Chaos, i, i / 7, 9);
    gsum += g; g2 += g * g;
}
if (rangeOk)
{
    double mean = sum / N, gmean = gsum / N;
    double gstd = Math.Sqrt(g2 / N - gmean * gmean);
    if (Math.Abs(mean - 0.5) > 0.01) failures += Fail("Rand01Mean", mean.ToString("F4"));
    else Pass($"Rand01Mean={mean:F4}");
    if (Math.Abs(gmean) > 0.02 || Math.Abs(gstd - 1.0) > 0.05)
        failures += Fail("GaussShape", $"mean={gmean:F4} std={gstd:F4}");
    else Pass($"GaussShape mean={gmean:F4} std={gstd:F4}");
}

// 5) Units kuantalama.
if (Units.QuantizeMm(12.345678) != 12346) failures += Fail("QuantizeMm", "yuvarlama");
else Pass("QuantizeMm");

// 6) CommandEnvelope derleme/kullanım (CB Spec 3.1 sözleşmesi ayakta mı).
var env = new CommandEnvelope
{
    CommandId = Guid.Empty, CatalogVersion = 1, Source = CommandSource.UI,
    ActionType = "tycoon.set_ticket_price", IssuedAtUnixMs = 0,
    MatchTick = 0, UserId = 1, SaveSlotId = 1, TeamIdx = 0,
    PayloadJson = Array.Empty<byte>(), SuggestionId = null
};
if (env.ActionType.Length == 0 || env.Source != CommandSource.UI)
    failures += Fail("Envelope", "beklenmeyen durum");
else Pass("Envelope");

// 7) FAZ 03 M0 — Motor iskeleti determinizm kapıları (ME Spec 3.2/4.2; BRIEF_FAZ03_ACILIS M0)

// xxHash64 RESMİ test vektörleri: ""=0xEF46DB3751D8E999, "a"=0xD24EC4F1A98C6E5B, "abc"=0x44BC2CF5AD770999
ulong xh0 = XxHash64.Hash(ReadOnlySpan<byte>.Empty);
ulong xhA = XxHash64.Hash(System.Text.Encoding.ASCII.GetBytes("a"));
ulong xhAbc = XxHash64.Hash(System.Text.Encoding.ASCII.GetBytes("abc"));
if (xh0 != 0xEF46DB3751D8E999UL || xhA != 0xD24EC4F1A98C6E5BUL || xhAbc != 0x44BC2CF5AD770999UL)
    failures += Fail("XxHash64Vectors", $"bos=0x{xh0:X} a=0x{xhA:X} abc=0x{xhAbc:X}");
else Pass("XxHash64Vectors");

// Motor koşucu: sabit başlangıç + komut zaman çizelgesi, N tick
static (ulong finalHash, ulong traceHash, uint applied, ulong at600) RunSkeleton(bool reorderEnqueue)
{
    var q = new CommandQueue();
    var early = new TacticChangeCmd(200, 0, new TacticDelta(1, 0, -1, 0));
    var late = new MotivationCmd(500, 1, ToneType.Atesle);
    // Aynı zaman çizelgesi, farklı KUYRUĞA GİRİŞ sırası — tick'ler arası uygulama sırası değişmemeli
    if (reorderEnqueue) { q.Enqueue(late); q.Enqueue(early); }
    else { q.Enqueue(early); q.Enqueue(late); }

    var eng = new MatchEngine(0xFA20300UL, q);
    var st = MatchEngine.CreateInitialState();
    st.Ball.Vx = 4200; st.Ball.Vy = -1300; // mm/sn — fizik iskeleti hash'i hareket ettirsin
    ulong at600 = 0;
    for (int t = 0; t < 1200; t++)
    {
        eng.Tick(ref st);
        if (st.Tick == 600) at600 = st.LastChecksum;
    }
    return (MatchEngine.StateHash(in st), q.AppliedTraceHash, q.AppliedCount, at600);
}

var runA = RunSkeleton(reorderEnqueue: false);
var runB = RunSkeleton(reorderEnqueue: false);
var runC = RunSkeleton(reorderEnqueue: true);

Console.WriteLine($"[info] M0 durum hash (1200 tick): 0x{runA.finalHash:X}");

// 7a) Determinizm: aynı girdi = bit düzeyinde aynı hash + aynı checksum kadans değeri
if (runA.finalHash != runB.finalHash || runA.at600 != runB.at600)
    failures += Fail("MatchSkeletonDeterminism", $"0x{runA.finalHash:X} != 0x{runB.finalHash:X}");
else Pass("MatchSkeletonDeterminism");

// 7b) Golden: durum hash'i sabitlendi — alan/sıra değişikliği bilinçli golden güncellemesi ister
const ulong MATCH_GOLDEN = 0x8954F2FA14EC7BFAUL; // sabitlendi — alan/sıra değişikliği bilinçli güncelleme ister
if (MATCH_GOLDEN != 0 && runA.finalHash != MATCH_GOLDEN)
    failures += Fail("MatchSkeletonGolden", $"0x{runA.finalHash:X} != 0x{MATCH_GOLDEN:X}");
else Pass("MatchSkeletonGolden");

// 7c) Checksum kadansı: 600. tick'te yazılmış ve son duruma eşit değil (durum ilerledi)
if (runA.at600 == 0 || runA.at600 == runA.finalHash)
    failures += Fail("MatchChecksumCadence", $"600. tick checksum'u beklenen gibi degil (0x{runA.at600:X})");
else Pass("MatchChecksumCadence");

// 7d) Komut sırası: kuyruğa giriş sırası tick'ler arası uygulama sırasını DEĞİŞTİREMEZ (ME 14.1)
if (runA.applied != 2 || runC.applied != 2 || runA.traceHash != runC.traceHash)
    failures += Fail("MatchCommandOrder", $"iz A=0x{runA.traceHash:X} C=0x{runC.traceHash:X}");
else Pass("MatchCommandOrder");

// 8) FAZ 03 M1 — Nitelik tablosu + TeamSheet kapıları (ME Spec 6.1-6.2; BRIEF_FAZ03_ACILIS M1)

// Balance yükleme: çekirdek parse etmez — host (burada Checks) System.Text.Json ile doldurur
static string FindRepoFile(string relative)
{
    var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
    for (int up = 0; up < 8 && dir != null; up++, dir = dir.Parent)
    {
        string p = System.IO.Path.Combine(dir.FullName, relative);
        if (System.IO.File.Exists(p)) return p;
    }
    throw new System.IO.FileNotFoundException(relative);
}

var balOpts = new System.Text.Json.JsonSerializerOptions { IncludeFields = true, PropertyNameCaseInsensitive = true };
var simBal = System.Text.Json.JsonSerializer.Deserialize<TheBadge.Sim.Config.SimBalance>(
    System.IO.File.ReadAllText(FindRepoFile("balance/sim.balance.json")), balOpts);
var at = simBal.attribute;
if (at.kondisyonTaban is < 0.5 or > 0.9 || at.kondisyonKuvvet is < 0.1 or > 0.5 ||
    at.kondisyonUs is < 0.4 or > 1.0 || at.moralCarpanPerMomentum is < 0.001 or > 0.02)
    failures += Fail("BalanceAttributeLoaded",
        $"taban={at.kondisyonTaban} kuvvet={at.kondisyonKuvvet} us={at.kondisyonUs} moral={at.moralCarpanPerMomentum}");
else Pass("BalanceAttributeLoaded");

var luts = AttributeLuts.Build(simBal);

// 8a) Tam enerji + nötr moral = taban değer (M_kondisyon=1.0 tam, M_moral=1.0 tam)
bool fullOk = true;
for (byte b = 1; b <= 100 && fullOk; b++)
    if (EffectiveAttributes.Compute(b, 1000, 0, luts) != b) fullOk = false;
if (!fullOk) failures += Fail("AeffFullEnergy", "tam enerjide A_eff != taban");
else Pass("AeffFullEnergy");

// 8b) Enerji monotonluğu + taban koruması (ME 6.2: yorgun oyuncu tabanın en az %70'ini korur)
{
    int prev = int.MaxValue; bool mono = true;
    for (int e = 1000; e >= 0; e -= 50)
    {
        int v = EffectiveAttributes.Compute(80, (ushort)e, 0, luts);
        if (v > prev) { mono = false; break; }
        prev = v;
    }
    int floor = EffectiveAttributes.Compute(80, 0, 0, luts);
    if (!mono || floor < 55 || floor > 57) // 80 × 0.70 = 56
        failures += Fail("AeffEnergyCurve", $"mono={mono} taban80@0enerji={floor}");
    else Pass("AeffEnergyCurve");
}

// 8c) Moral bandı (±10 momentum ≈ ±%5 → 100 tabanda ±5 puan; GDD 7.4 bant sınırı) + kırpma
{
    int hi = EffectiveAttributes.Compute(100, 1000, 10, luts);   // 105 → 100'e kırpılır
    int lo = EffectiveAttributes.Compute(100, 1000, -10, luts);  // 95
    int one = EffectiveAttributes.Compute(1, 0, -10, luts);      // taban 1 → en az 1
    if (hi != 100 || lo != 95 || one < 1)
        failures += Fail("AeffMoralClamp", $"hi={hi} lo={lo} one={one}");
    else Pass("AeffMoralClamp");
}

// 8d) A_eff golden vektörü — LUT kuantalaması platformlar arası sabit kalmalı
{
    int av1 = EffectiveAttributes.Compute(80, 500, -3, luts);
    int av2 = EffectiveAttributes.Compute(65, 250, 5, luts);
    Console.WriteLine($"[info] Aeff vektor: (80,500,-3)={av1} (65,250,5)={av2}");
    const int AV1 = 70, AV2 = 54; // sabitlendi — LUT/formül değişikliği bilinçli güncelleme ister
    if (AV1 != 0 && (av1 != AV1 || av2 != AV2))
        failures += Fail("AeffGoldenVector", $"{av1}/{av2} != {AV1}/{AV2}");
    else Pass("AeffGoldenVector");
}

// 8e) TeamSheet → kurulum determinizmi: aynı kadro = aynı durum hash'i; rol/anchor yansır
static TeamSheet BuildSheet(ulong seed, uint entity)
{
    var sheet = new TeamSheet { Starters = new PlayerEntry[11], Bench = new PlayerEntry[5] };
    for (int i = 0; i < 16; i++)
    {
        // Test kadrosu: nitelikler deterministik türetilir (üretim kadroları FAZ 04 veri katmanından gelir)
        byte V(uint salt) => (byte)(30 + (int)(Rng.Rand01(seed, Domain.Decision, entity, (uint)i, salt) * 60));
        var e = new PlayerEntry
        {
            PlayerId = (short)(entity * 100 + i),
            Name = $"Test-{entity}-{i}",
            RoleId = (byte)(i == 0 ? 1 : i < 5 ? 2 : i < 9 ? 3 : 4),
            AnchorXmm = (int)(entity * 1000) + i * 2500,
            AnchorYmm = 10000 + i * 3000,
            Attributes = new PlayerAttributes { Passing = V(1), Finishing = V(2), Pace = V(3), Stamina = V(4), Reflexes = V(5), Handling = V(6) }
        };
        if (i < 11) sheet.Starters[i] = e; else sheet.Bench[i - 11] = e;
    }
    return sheet;
}

{
    var cfgA = new MatchConfig { Seed = 9, EngineVersion = "m1", Home = BuildSheet(77, 1), Away = BuildSheet(77, 2) };
    var stA = MatchEngine.CreateInitialState(cfgA);
    var stB = MatchEngine.CreateInitialState(cfgA);
    bool roleOk = stA.Agents[0].RoleId == 1 && stA.Agents[11].RoleId == 1 &&
                  stA.Agents[3].AnchorX == cfgA.Home.Starters[3].AnchorXmm &&
                  stA.Agents[14].AnchorY == cfgA.Away.Starters[3].AnchorYmm;
    if (MatchEngine.StateHash(in stA) != MatchEngine.StateHash(in stB) || !roleOk)
        failures += Fail("TeamSheetInit", $"roleOk={roleOk}");
    else Pass("TeamSheetInit");

    // 8f) Doğrulama reddi: 11'den az ilk on bir / tekrarlı PlayerId kurulumu DURDURUR
    bool rejected = false;
    try
    {
        var bad = new MatchConfig { Home = BuildSheet(1, 3), Away = BuildSheet(1, 3) }; // aynı ID seti
        MatchEngine.CreateInitialState(bad);
    }
    catch (ArgumentException) { rejected = true; }
    if (!rejected) failures += Fail("TeamSheetValidate", "tekrarlı PlayerId kabul edildi");
    else Pass("TeamSheetValidate");
}

Console.WriteLine(failures == 0 ? "== TUM KONTROLLER YESIL ==" : $"== {failures} HATA ==");
return failures == 0 ? 0 : 1;
