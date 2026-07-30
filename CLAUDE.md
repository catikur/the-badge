# CLAUDE.md — The Badge Proje Anayasası

The Badge: Ultimate Soccer Manager 98'in modern mobil remake'i. Unity 6 (istemci) + .NET 8 C# simülasyon servisi + Nakama (platform hizmetleri). Metodoloji: **Vibe & Verify** — sen üretirsin, testler karar verir. Test geçmeyen kod reddedilir; bu pazarlık konusu değildir.

## Bağlayıcı Dokümanlar (önce oku, sonra kodla)

| Doküman | Ne zaman okunmalı |
| --- | --- |
| `docs/GDD_v4_1.md` | Kapsam/tasarım sorusu olan HER görevde ilgili bölüm |
| `docs/MatchEngine_Spec_v1_0.md` | `shared/TheBadge.Sim` içine dokunan her görevde ilgili bölüm |
| `docs/CommandBus_Spec_v1_0.md` | Komut, doğrulama, LLM entegrasyonu içeren her görevde |
| `docs/DECISIONS.md` | Her oturum başında (30 saniyelik bağlam) |

Spesifikasyonlarla çelişen bir istek görürsen: **durdur, çelişkiyi raporla, öneri sun.** Spesifikasyon dosyalarını doğrudan DEĞİŞTİRME; değişiklik önerisini `docs/DECISIONS.md`'ye "Bekleyen kararlar" satırı olarak ekle.

## Mimari Değişmezler (asla ihlal etme)

1. **Tek Kapı:** Oyun durumunu değiştiren her eylem `CommandEnvelope` ile Command Bus'tan geçer. UI, LLM, otomasyon — istisnasız. Doğrudan state mutasyonu yazan kod REDDEDİLİR.
2. **Determinizm:** Aynı girdi + aynı seed = bit düzeyinde aynı sonuç, her platformda. Sim koduna dokunan her PR determinizm kapısından geçer.
3. **Tek Kaynak Sim:** `shared/TheBadge.Sim` hem Unity'de hem sunucuda AYNI kaynaktan derlenir. Bu pakete UnityEngine referansı SIZAMAZ (`noEngineReferences: true` bunu zorlar — kaldırma).
4. **[KALİBRE] disiplini:** Ayarlanabilir her sayı `balance/sim.balance.json`'a gider. Koda gömülü magic number = review reddi. Balance dosyası config_hash kapsamındadır; şema değişikliği ME Spec 3.3'e tabidir.
5. **Öneri ≠ Yürütme:** LLM çıktısı en fazla katalog içi bir öneridir; yürütme yalnız onay + 4 kapılı doğrulama sonrası (CB Spec 5-7).

## Determinizm Kuralları (sim kodu yazarken)

**YASAK — `TheBadge.Sim` içinde asla:**
- `System.Random`, `Guid.NewGuid()` (rastgelelik için), `DateTime.Now/UtcNow`, `Environment.TickCount`
- `Dictionary`/`HashSet` iterasyon sırasına bağımlı mantık; sırasız LINQ (`OrderBy`siz `First`, `GroupBy` iterasyonu)
- Sıcak yolda LINQ/closure/heap tahsisi; `string` tabanlı kimlikler (ID kullan)
- Tick'ler arası taşınan `float/double` durum — kalıcı durum **int** (mm, mm/sn); ara hesap double, sonuç `Units.QuantizeMm`
- Platform intrinsics, `Math.Sin/Cos` sim mantığında (LUT gelecek — ME Spec 3.2)

**ZORUNLU desenler:**
- Rastgelelik yalnız `Rng.Hash64/Rand01/Gauss01` + doğru `Domain` akışı (ME Spec 3.1). Domain seçimini yorum satırıyla gerekçele.
- Ajan güncelleme sırası sabit (takım, formasyon indeksi). Paralellik yalnız MAÇLAR arası.
- Her yeni sim alt sistemi `Checks`'e en az 1 kontrol ekler.

## Klasör Haritası

```
shared/TheBadge.Sim/        deterministik çekirdek (saf C#, netstandard2.1, C# 9)
shared/TheBadge.Sim.Checks/ bağımlılıksız test kapısı — dotnet run ile koşar
server/TheBadge.SimWorker/  .NET sim servisi (FAZ 04'te Nakama RPC köprüsü)
unity/TheBadge/             Unity 6 projesi (UNITY_SETUP.md ile kurulur)
balance/sim.balance.json     tüm [KALİBRE] katsayılar (config_hash İÇİ — sezon içinde donuk)
balance/llm.budget.json      LLM maliyet tavanı + degrade merdiveni (config_hash DIŞI)
docs/                        bağlayıcı spesifikasyonlar + DECISIONS.md
docs/prompts/                TÜM LLM prompt'ları — versiyonlu, koda gömülmez
docs/tasks/                  görev brifleri (Claude Code'a verilecek işler)
evals/                       LLM kalite golden set'leri + değerlendirme rubriği
```

## Test ve Commit Kuralları

- Her görevin sonunda koş: `dotnet run --project shared/TheBadge.Sim.Checks -c Release` — yeşil değilse commit YOK, sonucu raporla.
- Unity tarafı kurulduktan sonra EditMode testleri de aynı statüdedir.
- Yeni katalog aksiyonu = şema + bant + 4 negatif test senaryosu birlikte gelir (CB Spec 10.1); testsiz katalog genişletmesi reddedilir.
- Unit test hedefi: core sistemlerde %80+ satır kapsamı (GDD 16.3).
- Branch modeli: `faz03/match-engine`, `faz04/tycoon` gibi faz-modül dalları; `main` her zaman yeşil.
- Commit mesajı: Conventional Commits (`feat:`, `fix:`, `test:`, `balance:`); gövde Türkçe olabilir.

## Çalışma Akışı (her görevde)

1. **Plan:** 3-6 maddelik kısa plan yaz; hangi spec bölümlerine dayandığını belirt.
2. **Uygula:** Küçük, derlenebilir adımlar; her adımda hangi dosyalara dokunduğunu söyle.
3. **Doğrula:** Checks + ilgili testleri KOŞ, çıktıyı göster. "Çalışması lazım" kabul edilmez — kanıt göster.
4. **Raporla:** Ne değişti, hangi [KALİBRE] değerleri eklendi/kullanıldı, açık uçlar neler.

Belirsizlikte varsayım üretme: seçenekleri artı/eksileriyle sun, karar iste. (Bu proje varsayım borcunu bir kez ödedi; tekrarı yok.)

## Dil ve Stil

- Kod kimlikleri İngilizce; yorumlar ve raporlar Türkçe. XML doc yorumlarında ilgili spec bölümünü referansla (ör. `— ME Spec 6.3`).
- C# 9 ile sınırlı kal (`TheBadge.Sim` Unity uyumluluğu); `Nullable` şimdilik kapalı.
- Public API'lerde `record`/`readonly struct` tercih et; sıcak yolda struct + önceden ayrılmış diziler (ME Spec 16.2).

## LLM Kalite ve Maliyet Kuralları (Apple Anayasası v1.0 ithali — 2026-07-30)

- **Golden set + kalite eval:** Her LLM özelliğinin (persona diyaloğu, röportaj, hikaye beat'i, Panorama senaryosu) `evals/golden/` altında 20-50 örneklik golden set'i olur. Prompt veya model değişikliği = PR + golden set üzerinde eval koşusu; sonuç PR'a eklenir. Eşik altı kalite merge EDİLMEZ. (Injection korpusu güvenliği test eder; bu kural KALİTEYİ test eder — ikisi ayrı kapıdır.)
- **Prompt'lar repo'da yaşar:** Prompt koda gömülmez; `docs/prompts/` altında versiyonlu dosyalardadır (front-matter: id, surum, model, son_eval). Prompt değişikliği kod değişikliğiyle aynı disipline tabidir; sohbette kaybolan prompt yok hükmündedir.
- **Kullanıcı başına maliyet tavanı + degrade:** Günlük/aylık token bütçesi `balance/ai.balance.json`'dadır [KALİBRE]; tüketim telemetriye event düşer. Degrade zinciri: %80 → küçük model (Haiku) + kısaltılmış bağlam (hafıza 12→6 olgu); %100 → yalnız cache + nazik sınır ekranı (sıfırlanma zamanı gösterilir). Sınır ekranı oyunu ASLA kilitlemez — Mod A (buton akışı) her zaman açıktır.

## Yapma Listesi

- `docs/` altındaki spec'leri değiştirme (DECISIONS.md'ye öneri yaz)
- `unity/**/Library`, `bin/`, `obj/` commit etme
- Balance değerini kodda "geçici" sabitleme — geçicisi de JSON'a yazılır
- Checks'i zayıflatma/atlama; toleransları gevşetme (gerekçeli öneri → karar → sonra değişiklik)
- `TheBadge.Sim`'e UnityEngine, Newtonsoft veya herhangi bir dış paket ekleme (çekirdek bağımlılıksız kalır)

## Anayasa v2.1 Oyun Hattı Ekleri (uyum turu)
- **Bağlayıcı süreç dokümanları:** docs/DECISIONS.md (D0-D6) her oturum başında okunur; GAME_THESIS kapsam filtresidir; ECONOMY_MAP balance sprintinin sözleşmesidir.
- **Kapılar:** Greybox Fun Gate (FAZ 00.5) ve 5G Dikey Dilim, FAZ 02 UI ve FAZ 05 seri asset üretiminden ÖNCEDİR. Persona Paneli Go/No-Go, Dikey Dilim ve Store Readiness'ta zorunlu (9.7).
- **DoD-G ekleri:** Sahne/oynanış görüntüsü veya kısa kayıt; performansa dokunan işte profiler ÖNCE/SONRA; Unity konsolu temiz.
- **Eval kuralı:** docs/prompts değişikliği = docs/evals golden set koşusu; skor < %85 merge yok. Hikaye/diyalog üretimi verilen memory_facts DIŞINA olgu uyduramaz.
- **AI maliyet tavanı:** balance sim.ai.gunlukTokenTavaniKullanici; aşımda degrade: cache → Haiku → nazik sınır ekranı.
- **Asset kuralı:** Stil rehberi onaylanmadan seri AI asset üretimi YASAK; LFS aktif (.gitattributes).
