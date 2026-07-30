using System;
using TheBadge.Sim.Commands;
using TheBadge.Sim.Core;
using TheBadge.Sim.Determinism;

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

Console.WriteLine(failures == 0 ? "== TUM KONTROLLER YESIL ==" : $"== {failures} HATA ==");
return failures == 0 ? 0 : 1;
