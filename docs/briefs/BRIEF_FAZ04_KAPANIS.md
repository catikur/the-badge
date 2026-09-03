# BRIEF — FAZ 04 KAPANIŞI: Core Modüller

Tarih: 2026-09-02 · Önkoşul: FAZ 04 açılış brifi (`BRIEF_FAZ04_ACILIS.md`, 2026-08-23)
Durum: **K1-K7 planı tamamlandı, üstüne K8-K13 ek dilimleri geldi** · Kapılar: 176/176 yeşil

> Bu bir SPEC DEĞİL. FAZ 03'ün kapanış brifi bir İŞ LİSTESİydi (yapılacaklar, tek tek işaretlendi);
> bu brif bir DEVİR TESLİMdir: ne bitti, neyi hangi kapı koruyor, hangi borç hangi tetikleyiciye
> bağlı, ve bir sonraki fazın neye güvenebileceği. Bağlayıcı belgeler ME/CB Spec ve DECISIONS'tır.

---

## 1. Plan neydi, ne oldu

Açılış brifi yedi dilim planlamıştı. Altısı tam kapandı, K6 dikişe kadar:

| Dilim | Plan | Durum | DECISIONS |
| --- | --- | --- | --- |
| K1 | Command Bus çekirdeği (CB 3-6, 8) | ✅ | K1 + inceleme turu |
| K2 | Dünya durumu (`GameState`) | ✅ | K2 + inceleme turu |
| K3 | Tycoon Economy (9 aksiyon) | ✅ | K3-A/B + inceleme turu |
| K4 | Squad Management (CB 4.2) | ✅ | K4 + inceleme turu |
| K5 | Transfer Market AI (CB 4.3) | ✅ | K5 + inceleme turu |
| K6 | Online dikişi (`IKomutTasima`) + SimWorker | ⚠️ **dikişe kadar** — Nakama bağlaması yok (bölüm 4) | K6 + inceleme turu |
| K7 | LLM hattı + injection savunması | ✅ | K7 — **katalog 32/32 kapandı** |

**K6'nın sınırı (inceleme bulgusu, Codex — 2026-09-02):** bu satır bir ✅ değil, ilk yazımda
öyleydi ve yanlıştı. Yapılan şey **taşımadan bağımsız dikiş**: `IKomutTasima` arayüzü, offline
kuyruk, uzlaştırma, outbox ve SimWorker konağı — hepsi kapılı. **Nakama RPC kaydının kendisi
yazılmadı**; bu ortamda koşturulamıyor ve CLAUDE.md kanıtlanamayan kodu yasaklıyor.
`server/SERVER_SETUP.md` eksikleri açıkça listeler, bu brif de bölüm 4'te borç olarak taşır.
Bir sonraki faz bu satıra bakıp "online üretimde hazır" diye plan YAPMAMALIDIR.

Plana **girmeyen** ama ölçüm gerektirdiği için açılan altı dilim daha:

| Dilim | Neden açıldı |
| --- | --- |
| K8 | `Rng.Gauss01` çarpışma borcu — K3'te ölçülmüş, Atilla "şimdi yap" dedi |
| K9 | FAZ 04 kapanış borçları: RNG adres kapısı, xG sapması, RPC outbox, LLM kalite kapısı |
| K10 | İnceleme bulguları + ECONOMY_MAP capex kapısı |
| K11 | **Oyun oynanabilir hâle geldi** — dünya ile motor arasındaki dikiş yoktu |
| K12 | Motor kalibrasyonu (rol ayrımlı kadro), kondisyon/moral motora, transfer piyasası |
| K13 | Sınırsız gelir büyümesi kapatıldı; kırmızı kart bandı sorgulandı |

**K11 bu fazın en önemli tek bulgusudur** ve plana girmemişti: FAZ 03 maçı üretiyordu, FAZ 04
dünyayı kurmuştu, ama ikisi **birbirine hiç bağlanmamıştı**. Her iki taraf da kendi kapılarında
yeşildi. Ders DECISIONS'a kural olarak yazıldı: *iki alt sistem ayrı ayrı yeşilse, aralarındaki
dikiş ölçülmemiş demektir.*

---

## 2. Bugünkü taban (kanıt)

`dotnet run --project shared/TheBadge.Sim.Checks -c Release` → **176 kapı, hepsi yeşil**
(81'i FAZ 03'ten M\*, 81'i FAZ 04'ten K\*, kalanı ortak).

**Çalışan yüzeyler:**

| Ne | Nerede | Kanıt kapısı |
| --- | --- | --- |
| Command Bus, 4 kapı, 32 aksiyon | `shared/TheBadge.CommandBus` | `K1KatalogTamligi`, `K1BantZorlamasi`, `K7KatalogKapandi` |
| Dünya durumu + atomik commit + hash | `shared/TheBadge.World` | `K2TekKapiUctanUca`, `K2Atomiklik`, `K2HashKapsami` |
| Tycoon ekonomi (ECONOMY_MAP sözleşmesi) | `World/Economy` | `K3EkonomiSozlesmesi`, `K3IflasEgrisi`, `K10CapexSozlesmesi` |
| Kadro yönetimi + maç köprüsü | `World/Squad` | `K4HubYolu`, `K11KadroKoprusu` |
| Transfer piyasası + değerleme + pazarlık | `World/Transfer` | `K5PazarlikDongusu`, `K10MerdivenSonrasiSink` |
| Online **dikişi**: offline kuyruk, uzlaştırma, outbox (bugün bellekte) | `World/Online`, `server/TheBadge.SimWorker` | `K6OfflineKuyruk`, `K9OutboxDayanikliligi` |
| LLM hattı + injection savunması + eval | `World/Llm`, `docs/prompts`, `docs/evals` | `K7InjectionKorpusu`, `K9EvalKosuSozlesmesi` |
| **Oynanabilir konsol** | `server/TheBadge.Play` | kendi nöbetçisi (aşağıda) |

**Balance dosyaları — hepsi [KALİBRE], ama hepsi aynı kimliğe girmiyor.** (İnceleme bulgusu,
Codex — 2026-09-02: ilk yazımda "hepsi config_hash içi" diyordu; `ConfigHash.Compute` yalnız İKİ
dosyanın ham bayt özetini alır. Fark önemli, çünkü config_hash golden replay setini geçersiz
kılan şeydir.)

| Sınıf | Dosyalar | Ne demek |
| --- | --- | --- |
| **config_hash İÇİ** (ME 3.3 replay dörtlüsü) | `sim.balance.json`, `command.bands.json` | Ham bayt özetleri `ConfigHash.Compute`a girer. Tek bayt değişirse golden set bayatlar; `M17ReplaySetiGuncel` yeniden üretim ister, `M17ConfigHashAyirtEdici` hash'in ayırt ediciliğini ölçer. |
| **config_hash DIŞI, ama sonucu değiştirir** | `world`, `economy`, `squad`, `transfer`, `market`, `sim.lod2` | Dünya/lig sonucunu değiştirir; determinizmi `K2YurutmeDeterminizmi` + `K3EkonomiDeterminizmi` korur. Golden replay setini geçersiz KILMAZ — bu dosyaları değiştiren, replay kimliğinin değişimi yakalamadığını bilerek değiştirmelidir. |
| **config_hash DIŞI, bilerek** | `llm.balance.json`, `ai.balance.json` | Oyun mekaniğini değil girdi hijyenini ve maliyet tavanını ayarlar; replay'i etkilemez (`LlmRules.cs` başlığı, CLAUDE.md LLM kuralı). |

**Ekonomi bugün nerede** (`K10CapexSozlesmesi` + `K10MerdivenSonrasiSink`):
merdiven 11 sezon ∈ [6,24] · inşaat penceresi 1,104 ∈ [1,05-1,15] · işletme 1,353 bandın üstünde ·
**merdiven sonrası durağan oran 1,124 ∈ [1,05-1,15]** · maaş payı %51,2 ∈ [%45-60] ·
bant uçlarında merdiven 23/10 ∈ [6,24] · referans kulübün en düşük kasası +20,0M₺.

**Motor bugün nerede** (`M16ECalibGenis`, 500 maç lig dağılımı): 11 metrik bant içinde.
Köprü kadrosuyla (`K11KadroKoprusu`): gol 3,23 · şut 29,8 · kart 5,91 · kırmızı 0,25.

---

## 3. Oyun oynanabilir — ve bu bir kapı değil, bir ALET

`dotnet run --project server/TheBadge.Play -- --oto 38` tam sezon koşuyor: senin maçın LOD 0 tam
motor, ligin kalanı LOD 2 (ME 16.4 karışımı), her yönetim eylemi Tek Kapı'dan, hafta sonu
ekonomisi + maç sonrası kadro durumu journal üzerinden.

**Bu konsol bir DOĞRULAMA YÜZEYİdir, UI değil.** FAZ 02 ekranları onun yerine geçecek. Ama bu
fazın en pahalı hatasını **oynamak** yakaladı: yorgunluk modeli bir cırcırdı (düzenli ilk 11 beş
maçta tabana çakılıyordu) ve hiçbir kapı bunu görmüyordu, çünkü kapılar modelin YÖNÜNÜ ve
SINIRINI ölçüyordu, ŞEKLİNİ değil. Tam sezon ölçümü: cırcır 14. sıra 42 puan · düzeltilmiş model
11. sıra 48 puan · yorgunluk kapalı kontrol 10. sıra 54 puan.

**Konsolun kendi nöbetçisi var:** iki haftadan fazla oynandıysa kadro kondisyonu ve rakip enerjisi
DEĞİŞMİŞ olmalı; değişmediyse `!! HAFTA SONU DÜNYASI İŞLEMEDİ` yazıp çıkış kodu 2 ile düşer.
Sebebi: haftalık döngü üst-düzey deyimler içinde yerel bir fonksiyon ve `Sim.Checks` onu
çağıramıyor — bu boşluk tek turda iki kez ısırdı (`MacSonrasi.Isle` ve `LigKurucu.HaftaSonu`
çağrılmıyordu).

---

## 4. Açık borçlar — hepsi korumalı, hiçbiri gizli

| Borç | Bugünkü değer | Koruyan kapı | Kapanma koşulu |
| --- | --- | --- | --- |
| **Doğrudan kırmızı yolu ölü** | 0,000/maç · en yüksek şiddet 0,746 vs eşik 0,80 | `K13CDogrudanKirmiziOlu` (iki taraflı: yol canlanırsa düşer, boşluk > 0,08 olursa kırmızı) | FAZ 05 hakem dilimi — ayrı OLAY SINIFI (Atilla kararı 2026-09-02: (c) bekle) |
| **LİG dağılımında kırmızı oranı** | 0,030 (hedef 0,10-0,36) | `M16EKirmiziBorcu` (tavan dondurulmuş, hedef basılıyor) | yukarıdakiyle aynı kök |
| **LOD 1'in geleceği** | LOD 0'ın eşleniği | `M15Lod1Esdeger` + ME 16.4 CPU marjı her koşuda | FAZ 05 cihaz testi: ORTA cihazda LOD 0 bir maç > 800 ms ise LOD 1 ayrışır, değilse satır silinir |
| **Haftalık döngü birim kapısı** | konsol nöbetçisi tutuyor | `TheBadge.Play` çıkış kodu 2 | FAZ 02: döngü test edilebilir bir servise taşınınca |
| **LLM kalite eval'i model erişimi bekliyor** | alet hazır, ölçüm yok | `K9EvalKosuSozlesmesi` (koşucu sözleşmesi) | model erişimi olan ortamda golden set koşusu |
| **Nakama bağlaması + kalıcı outbox + keyframe yayını** | dikiş hazır (`IKomutTasima`, `IOutboxStore`), taşıma yok; outbox bugün `BellekOutboxStore` | `K6OfflineKuyruk`, `K9OutboxDayanikliligi` — **dikişi** ölçer, taşımayı değil; eksikler `server/SERVER_SETUP.md`de yazılı | Nakama + PostgreSQL koşturulabilen ortam: RPC kaydı, outbox yazmasının durum yazmasıyla AYNI işlemde commit'i, keyframe yayını (ME 14.4) |

**Kapanan borçlar (bu fazda):** `Rng.Gauss01` çarpışması (K8) · RNG adres çakışmaları (K9-A) ·
xG sapması (K9-B) · `OzetKart` entity ayrımı (K10-A) · zaman çizelgesi eşiği (K10-C) ·
şut/maç 33,5 (K12-A) · merdiven sonrası sink 2,25 → **1,124** (K11-E → K12-C → K13-A).

---

## 5. Bir sonraki faz neye güvenebilir (dondurma)

FAZ 03, FAZ 04'e `docs/INTERFACE_FREEZE_FAZ04.md` ile bir arayüz dondurmuştu. FAZ 04'ün
tüketicisi **Dikey Dilim / FAZ 02 UI**; ona dondurulan yüzey:

1. **Tek Kapı sözleşmesi.** Durum değiştiren her eylem `CommandEnvelope` → `CommandBus.Submit`
   → 4 kapı → `WorldExecutor` atomik commit. UI doğrudan state yazmaz. Katalog 32 aksiyon,
   sürüm kilidi `K1KatalogSurumKilidi` ile korunuyor.
2. **Okuma yüzeyi.** `GameState` (kulüp, kadro, finans, takvim, teklifler) + `WeekLedger` (haftalık
   kalem raporu) + `MatchSummaryPacket` (maç sonu istatistik/highlight — istatistik ekranlarının
   TEK kaynağı, `M14PaketSemasi`).
3. **Determinizm.** Aynı save seed + aynı komut dizisi = aynı `WorldHash`. `K2YurutmeDeterminizmi`
   ve `K3EkonomiDeterminizmi` bunu her koşuda ölçüyor.
4. **Balance yüzeyi.** Tüm ayarlanabilir sayı `balance/*.json`da; UI hiçbir sayıyı kendi
   içinde tutmaz.
5. **Dondurma DIŞINDA (bilerek):** maç sunumu (kamera, animasyon, 2.5D) — PLAYTEST_3G kaydına
   göre Dikey Dilim öncesi YENİDEN TASARLANACAK; bireysel oyuncu talimatları (`K10TalimatAtilligi`
   atıllığı görünür tutuyor); çok-kulüp ekonomisi (rakip kulüplerin kendi kasası yok).

---

## 6. Sıradaki kapı: 5G Dikey Dilim

Anayasa (CLAUDE.md v2.1 ekleri) kapı sırasını yazıyor: **Greybox Fun Gate ve 5G Dikey Dilim,
FAZ 02 UI ve FAZ 05 seri asset üretiminden ÖNCEDİR.**

Fun Gate 2026-08-08'de **NO-GO %40** ile kapandı ve cevabı "gerçek motoru kur" oldu (FAZ 03/04).
Motor artık gerçek, dünya gerçek, ekonomi bantta, oyun oynanabiliyor. Dolayısıyla sıradaki iş
Fun Gate'i **gerçek motorla** yeniden koşabilmek — bunun aracı Dikey Dilim.

**Dikey Dilim'in önkoşulları (bu brifin devrettiği iş):**
- Maç sunumunun yeniden tasarımı (PLAYTEST_3G: "sunum revizyonu Dikey Dilim öncesi") — greybox'ın
  Model Maçı sunumu motora olduğu gibi taşınmaz.
- Unity tarafı: `unity/TheBadge` bugün greybox iskeleti (EditMode testleri + `EngineDev`
  bootstrap). `shared/TheBadge.Sim` paketinin Unity'de aynı kaynaktan derlendiği
  (`noEngineReferences: true`) doğrulanmalı.
- Persona Paneli Go/No-Go, Dikey Dilim'de **zorunlu** (Anayasa 9.7).

**Bu brif Dikey Dilim'in kapsamını ÇİZMEZ** — o ayrı bir açılış brifidir. Burada devredilen şey
tabandır: neye güvenilebileceği, neyin borç olduğu ve hangi kapının neyi koruduğu.

---

## 7. Bu fazın kuralları (DECISIONS'tan, tekrar etmemek için)

Bu fazda ölçümle öğrenilen ve kayda geçen kurallar:

- *İki alt sistem ayrı ayrı yeşilse, aralarındaki dikiş ölçülmemiş demektir.* (K11)
- *Ölçtüğün şeyin geçerli olduğu bölgenin İÇİNDEN ölç.* (K12-D — bant dışından ekstrapolasyon)
- *Bir düzeltmeyi koruduğunu söylediğin kapıyı, düzeltmeyi SÖKEREK ölç.* (K12 inceleme turu)
- *Bir modelin yönünü ve sınırını ölçmek şeklini ölçmez; dengesi olan her modelde kapı DENGEYİ
  ölçmeli.* (K13-A — yorgunluk cırcırı)
- *6 maçlık bir örneklemden model sonucu çıkarma.* (K13-A)
- *Bir kapının ölçtüğü şeyi tartışacaksan, KAPININ KENDİ popülasyonu ve tohum ailesiyle ölç.* (K13-C)
- *Yapıldığını hatırladığın şey, ölçülmüş şey değildir.* (BU brif — önce beş sayı, sonra iki
  kapsam iddiası yanlış çıktı; ikisi de oturum içi hafızadan yazılmıştı. Bir devir teslim
  belgesinde her sayı taze koşudan, her kapsam iddiası kaynak dosyadan doğrulanır: 176 kapı
  yeşilken bile brif yanlış olabilir, çünkü hiçbir kapı bir markdown cümlesini ölçmez.)

---

## 8. Kapanış koşulu

Bu brif, aşağıdakiler doğru olduğu için yazıldı:
- [x] K1-K7 planının tamamı kapandı — K6 **taşımadan bağımsız dikişe kadar** (bölüm 1 ve 4), katalog 32/32
- [x] 176 kapı yeşil (`dotnet run --project shared/TheBadge.Sim.Checks -c Release`)
- [x] Oyun uçtan uca oynanabiliyor (38 haftalık sezon, determinist)
- [x] ECONOMY_MAP sözleşmesi hem inşaat penceresinde hem durağan hâlde bandında
- [x] Açık borçların hepsi kapıyla korumalı ve tetikleyicisi yazılı
- [x] Bir sonraki fazın güvenebileceği yüzey donduruldu (bölüm 5)
