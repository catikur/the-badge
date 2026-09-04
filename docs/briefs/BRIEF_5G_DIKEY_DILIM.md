# BRIEF — 5G DİKEY DİLİM AÇILIŞI: Tek Maç Günü, Final Kalitede

Tarih: 2026-09-04 · Önkoşul: **FAZ 04 kapandı** (PR #29/#30 merge; `docs/briefs/BRIEF_FAZ04_KAPANIS.md`;
176 kapı yeşil) · Dal: **`5g/dikey-dilim`** · Süreç: CLAUDE.md akışı (plan → uygula → kanıt → kayıt → PR)
· Anayasa: v2.1 **Aşama 5G / 4G.7**, kapı kanıtları 4G.7 + DoD-G (4G.9) + Persona Paneli (9.7)

> **İSİM UYARISI — iki ayrı şey aynı rakamı taşıyor.** Anayasa'nın **Aşama 5G**'si (Vertical Slice)
> ile GDD'nin **FAZ 05**'i (Asset Üretimi) AYNI ŞEY DEĞİLDİR. Bu brif Aşama 5G'yi açıyor; GDD FAZ 05
> seri asset üretimi bu kapının ARKASINDADIR (DECISIONS, v2.1 uyum turu: "FAZ 02 ve FAZ 05 seri asset
> üretimi fun kapısının arkasına alındı"). Dal adı bu yüzden `5g/` öneki taşıyor, `faz05/` değil.

---

## 1. Amaç ve kapının tanımı

Anayasa 4G.7: dikey dilim, **tek bölümü final kaliteye çekmektir** — gerçek art + ses + haptic,
FTUE'nun ilk 5 dakikası, gerçek monetizasyon anı (sandbox IAP), analytics event'leri, hedef cihazda
tutan performans.

Bu projede "tek bölüm"ün karşılığı DECISIONS'ta zaten yazılı (v2.1 uyum turu):

> "FAZ 03 sonrası **5G Dikey Dilim kapısı** eklendi (**tek maç günü final kalitede** + sandbox IAP +
> cihazda fps)."

Ve "maç günü" GAME_THESIS'in Session Shape'idir — 8-15 dk:

```
hafta hazırlığı 3-5 dk (taktik + tycoon + 1 konuşma)
  → maç 5-8 dk (canlı VEYA sabah replay/özet)
    → kapanış 1-2 dk (röportaj + yarına plan)
```

**Dilimin tanımı bu yüzden şudur:** *bir oyuncunun telefonunda, tek bir maç gününü baştan sona,
final kalitede, gerçek motorla ve gerçek dünyayla oynayabilmesi.* Bu tanım keyfi değil — tezin
vaat ettiği oturumun ta kendisi, ve dünya (FAZ 04) ile motorun (FAZ 03) dikişini UI üzerinden
geçmeye zorluyor.

---

## 2. Bugünkü gerçek taban — ne VAR, ne YOK

Bu tablo bugün ölçüldü (kapanış brifinin kuralı: hafızadan yazılan sayı ölçülmüş sayı değildir).

### VAR (kanıtlı)

| Ne | Nerede | Kanıt |
| --- | --- | --- |
| Deterministik maç motoru | `shared/TheBadge.Sim` | 85 M\* kapısı · 50 golden replay bit-eşit |
| Dünya + Tek Kapı + 32 aksiyon | `shared/TheBadge.World`, `shared/TheBadge.CommandBus` | 81 K\* kapısı |
| Ekonomi ECONOMY_MAP bandında | `World/Economy` | `K10CapexSozlesmesi`, `K10MerdivenSonrasiSink` |
| Oynanabilir sezon (konsol) | `server/TheBadge.Play` | `--oto 38` tam sezon, determinist |
| LLM hattı + injection savunması + eval koşucusu | `World/Llm`, `docs/prompts`, `docs/evals` | `K7InjectionKorpusu`, `K9EvalKosuSozlesmesi` |
| Unity'de motor testi | `unity/.../Scenes/EngineDev.unity` | `EngineDevBootstrap` |

**Toplam: 176 kapı yeşil** (`dotnet run --project shared/TheBadge.Sim.Checks -c Release`).

### YOK (ve bu dilimin işi büyük ölçüde bu listedir)

| Eksik | Bugünkü durum | Neden 5G'nin işi |
| --- | --- | --- |
| **Dünya Unity'ye ULAŞMIYOR** | `unity/TheBadge/Packages/manifest.json`ın proje-dışı tek yerel paketi `com.thebadge.sim`; `TheBadge.World` ve `TheBadge.CommandBus`ta ne `package.json` ne `.asmdef` var | İstemci bugün SADECE maç motorunu görebiliyor. Kadro, ekonomi, transfer, Tek Kapı — hiçbiri Unity'den erişilebilir değil. Maç günü döngüsü bu köprü olmadan kurulamaz. |
| **Maç sunumu** | Model Maçı ekranı emekli greybox'ta (`MatchModel` + `ModelMatchDirector`), gerçek motorun üstünde DEĞİL | DECISIONS 2026-08-08: "motora olduğu gibi taşınmaz". Yeniden tasarım bu dilimin ilk işi. |
| **Unity mimarisi** | Tek `Game.Greybox` asmdef (+ test asmdef'i), 28 C# dosyası, 4 EditMode test dosyası. Greybox **emekli** ilan edildi. | `unity/UNITY_SETUP.md`'nin 5 modüllü asmdef haritası "FAZ 01'de kurulacak" diyor ve kurulmadı. Borcu bu dilim ödüyor. |
| **FTUE** | Yok. GDD 9.2 senaryosu yazılı, kod yok. | 4G.7 ilk 5 dakikayı zorunlu tutuyor. |
| **Monetizasyon** | Yok — StoreKit/IAP hiçbir yerde yok | 4G.7: "monetizasyon anı gerçek (sandbox IAP)". |
| **Analytics/telemetri** | Yalnız greybox-yerel `TelemetryLog.cs`; GDD 9.5'in FTUE hunisi yok | 4G.7 analytics event'lerini zorunlu tutuyor; huni D1'in öncü göstergesi. |
| **Art direction** | Yok. D1'de araç seçili (Scenario + Midjourney), stil rehberi yok. | 4G.5: **stil rehberi olmadan seri AI asset üretimi YASAK.** "Final kalite" bir stil kararı ister. |
| **Nakama bağlaması** | Dikiş var (`IKomutTasima`), taşıma yok — kapanış brifi bölüm 4'te borç | 5G'yi bloklamaz (aşağıda gerekçesi), ama "online" iddiası edilemez. |

### Bu ortamın koşturamadıkları (dürüst sınır)

Unity Editor, iPhone, Xcode, Nakama ve PostgreSQL bu konteynerde YOK. FAZ 03 ve 04 saf C#'tı ve
uçtan uca kapıyla kanıtlanabiliyordu; **5G'nin ağırlığı Unity + cihaz + insan tarafında.** Bu, iş
bölümünü değiştirir — bölüm 7 bunu açıkça ayırıyor. Kanıtlanamayan kod yazılmaz (CLAUDE.md),
dolayısıyla bu brif "yapıldı" diyebileceğim işlerle Atilla'nın koşması gereken işleri karıştırmaz.

---

## 3. Kapı sırası: ödenmemiş bir önkoşul var

Anayasa'nın sırası **Fun Gate (4G.4) → Vertical Slice (4G.7)**'dir ve 4G.10'un 19. hatası nettir:
*"Fun kanıtlanmadan art ve içerik üretimine para/zaman gömmek."*

**Bizim Fun Gate'imiz NO-GO ile kapandı:** "bir maç daha" 2/5 = **%40**, eşik %60
(`docs/PLAYTEST_3G.md`). Daha kötüsü: **kopuş NEDENİ ölçülmedi** — mini mülakat tablosu boş,
telemetri JSONL'leri repoya kopyalanmadı. Kayıt bunu dürüstçe yazıyor ("uydurma veri yok").

DECISIONS iki cümleyle bu borcun nasıl kapanacağını zaten söylüyor ve **ikisi çelişmiyor, iki
AŞAMA tarif ediyor**:

1. *"Model Maçı SUNUMU motora olduğu gibi taşınmaz; Dikey Dilim (5G) **öncesi** motor üstünde
   yeniden tasarlanır + küçük, **MÜLAKATLI** gözlem turuyla doğrulanır (borç kaydı)."* (2026-08-08)
2. *"Fun doğrulamasının **nihai yükü 5G Dikey Dilim'e** taşındı (persona paneli + gerçek kalite
   orada zorunlu)."* (2026-08-07)

Yani: **küçük mülakatlı tur = 5G'nin önkoşulu; nihai fun kanıtı = 5G kapısının kendisi.**
Bugün birincisi ÖDENMEDİ. Ve ödenmemiş olması tek başına bir gecikme değil — sunum yeniden
tasarımının **girdisi yok**: 3 oyuncunun neden koptuğu (blok yapısı mı, tempo mu, müdahale sığlığı
mı) hâlâ bilinmiyor. Girdisiz bir yeniden tasarım tahmindir; üstüne art + FTUE + IAP koymak
19. hatanın tarifidir.

**Önerim: 5G iki kapılı açılsın.**

| | Kapsam | Çıkış kapısı |
| --- | --- | --- |
| **5G-a** | Paket köprüsü + maç sunumunun GERÇEK motor üstünde yeniden tasarımı, **placeholder art ile** + mülakatlı gözlem turu (3-5 kişi) | "bir maç daha" sinyali + **kopuş nedeninin YAZILI olması** |
| **5G-b** | Final kalite: art, ses/haptic, FTUE ilk 5 dk, sandbox IAP, analytics hunisi, cihaz performansı | Anayasa 4G.7 Vertical Slice Gate + persona paneli |

Bu, kapıyı gevşetmek değil **sırasını düzeltmek**: pahalı yarı, ucuz yarının sinyalini bekler.
5G-a'nın maliyeti günler; 5G-b'nin maliyeti haftalar ve geri alınamaz (art üretimi).

---

## 4. Kapsam önerisi — altı dilim

| # | Dilim | Neden burada | Faz |
| --- | --- | --- | --- |
| **S1** ✅ | **Paket köprüsü:** `TheBadge.World` + `TheBadge.CommandBus` Unity paketi olur; `UNITY_SETUP.md`'nin asmdef haritası güncellenir (FAZ 01 borcu) | Bunsuz Unity dünyayı GÖREMEZ. Her şeyin önkoşulu. | 5G-a |
| **S2** | **Maç sunumu yeniden tasarımı** gerçek motor üstünde + **mülakatlı gözlem turu** | Fun borcunun ödenmesi; 5G-b'nin girdisi | 5G-a |
| **S3** | **Maç günü döngüsü uçtan uca:** hafta hazırlığı (taktik + 1 tycoon aksiyonu + 1 konuşma) → maç → kapanış (röportaj + plan) | Dilimin TANIMI bu. Dünya-motor dikişi burada UI'dan geçer. | 5G-b |
| **S4** | **FTUE ilk 5 dakika** (GDD 9.2 "Enkazı Devral" akışının başı) + progressive disclosure hafta 1 | 4G.7 zorunlu; persona P4'ün "≤3 dk ilk değer anı" MAJOR'ı burada karşılanır | 5G-b |
| **S5** | **Monetizasyon anı** (sandbox IAP) + **telemetri/analytics** + FTUE hunisi | 4G.7 ikisini de zorunlu tutuyor; huni D1'in öncü göstergesi (GDD 9.5) | 5G-b |
| **S6** | **Kapı kanıtları:** cihazda fps + profiler + build boyutu + termal/batarya notu + persona paneli | Vertical Slice Gate'in kendisi | 5G-b |

### Sıra neden böyle (keyfi değil)

- **S1 en başta**, çünkü Tek Kapı değişmezi istemcide de geçerli: `unity/UNITY_SETUP.md`'nin kendi
  kuralı *"sunum katmanı sim durumunu OKUR, asla doğrudan yazmaz — durum değişikliği yalnız Command
  Bus"*. Bugün Unity'de Command Bus YOK; S1 olmadan yazılan her ekran ya değişmezi ihlal eder ya
  yeniden yazılır. Bu, FAZ 04'te K1'in en başta olmasının aynı gerekçesi.
- **S2, S3'ten önce**, çünkü maç günü döngüsünün ortasındaki en büyük belirsizlik maçın kendisi.
- **S4/S5, S3'ten sonra**, çünkü FTUE var olmayan bir döngüyü öğretemez ve monetizasyon anı
  oturumun içinde bir yere oturmak zorundadır.
- **S6 en sonda**, çünkü kapı kanıtı ölçülecek bir şey ister.

### Persona panelinin park edilmiş borcu burada ödeniyor

1G panelinin 6 MAJOR'ı "FAZ 02 tasarım girdileri + FTUE revizyonu" diye ertelenmişti
(`docs/PERSONA_PANEL_1G.md`). Bu dilime düşenler:

| Bulgu | Kim | Nerede karşılanır |
| --- | --- | --- |
| FTUE ≤3 dk ilk interaktif değer anı (15 dk uzun) | P4 | S4 |
| Serbest pozisyonlamada parmak hassasiyeti — snap/yakınlaştırma yardımı, **telefon birincil** | P1 | S3 (taktik tahtası) |
| "Maç saatini kaçırma" kaygısı — replay/Panorama vaadinin FTUE'da İLETİLMESİ | P4 | S4 |
| Premium etki şeffaflığı (maç önü rozet) | P2 | S5 (karar; uygulama kapsam dışı olabilir) |
| Mağaza aşamalı açılımı FTUE progressive disclosure ile hizalı | P2 | S4/S5 |

Kalan ikisi (App Preview dili, erken arkadaş daveti) store/soft-launch işidir — bu dilimde değil.

---

## 5. Karar maddeleri — Atilla'nın

Hiçbiri varsayımla kapatılmadı (CLAUDE.md: "Belirsizlikte varsayım üretme").

### D-A. Kapı sırası — ✅ **KAPANDI (2026-09-04, Atilla): (a) iki kapılı**

| Seçenek | Artı | Eksi |
| --- | --- | --- |
| **(a) İki kapılı: 5G-a → 5G-b** ← **önerilen** | Fun borcu ödenir; art/FTUE/IAP harcaması sinyalden SONRA; 19. hataya düşmez | Takvimde bir gözlem turu (günler) |
| (b) Tek kapı: tam dilim şimdi | Daha hızlı görünür | Sunum yeniden tasarımının girdisi yok; art üretimi geri alınamaz; kapıda "fun yok" çıkarsa harcanan iş çöp |
| (c) Fun borcunu tamamen 5G kapısına bırak | Kayıttaki 2. cümlenin harfi harfine okunuşu | 1. cümledeki "**öncesi**" kaydını yok sayar; borç kaydını sessizce siler |

### D-B. "Tek maç günü" kapsamı tam olarak ne?

Öneri: **canlı maç yolu** (replay/özet yolu 5G dışı), **1 tycoon aksiyonu** (bilet fiyatı — GDD 9.2
FTUE'nun da ilk dokunuşu), **1 konuşma** (maç sonu röportaj), **1 taktik dokunuşu** (diziliş/tempo).
Alternatif: replay yolunu da içeri almak (tezin "canlıyı kaçırmak ceza değildir" vaadi orada) —
ama iki sunum yolunu birden final kaliteye çekmek dilimi ikiye katlar. **Karar Atilla'nın.**

### D-C. Unity paket sınırı — ✅ **KAPANDI (2026-09-04, Atilla): (a)** → `docs/adr/ADR-002-unity-paket-siniri.md`

| Seçenek | Artı | Eksi |
| --- | --- | --- |
| **(a) `World` + `CommandBus` da Unity paketi olur** (`Sim` ile aynı desen: `package.json` + `noEngineReferences: true` asmdef) ← **önerilen** | Tek kaynak değişmezi korunur; istemci ön-doğrulama yapabilir (CB: "istemci ön-doğrular, sunucu yeniden doğrular"); offline kuyruk mümkün | Üç paketin Unity uyumluluğu (C# 9, bağımlılıksızlık) sürekli korunmalı |
| (b) İnce cephe paketi (`TheBadge.Client`) — yalnız DTO + katalog önbelleği | Yüzey küçük | Doğrulama sunucuya kayar; offline akış ve anlık UI geri bildirimi zayıflar; **CB'nin kendi mimarisine aykırı** |
| (c) Sunucu-only + REST/RPC DTO'lar | En az istemci kodu | G3 otoritesi zaten sunucuda; ama Nakama bağlaması YOK — bugün çalışacak bir yol değil |

Not: `UNITY_SETUP.md`'deki asmdef haritası FAZ 04'ten ESKİYDİ — `Game.Commands`ı `TheBadge.Sim`e
bağlıyordu, oysa Command Bus ayrı pakette. **Kararla birlikte güncellendi.**

**S1 UYGULANDI (2026-09-04).** Üç paket de Unity'den erişilebilir; `S1UnityPaketSiniri` kapısı
kimlik + manifest yolu + `noEngineReferences` + **asmdef↔csproj grafiğinin birebirliği** +
netstandard2.1/C#9 + klasör disiplinini ölçüyor. Uygularken **gizli bir tuzak** çıktı: Unity paket
klasöründeki TÜM `.cs`'i derler ve `dotnet build`in ürettiği `obj/**/*.AssemblyInfo.cs` orada
kalıyordu. Tuzak `TheBadge.Sim` için de vardı; patlamamasının tek sebebi Unity'yi açanın o klasörde
henüz `dotnet build` koşmamış olmasıydı. Çıktı `artifacts/`e yönlendirildi.

### D-D. Art direction: stil rehberi ne zaman?

4G.5 seri AI asset üretimini stil rehberi olmadan YASAKLIYOR. "Final kalite" bir stil kararı ister.

| Seçenek | Artı | Eksi |
| --- | --- | --- |
| **(a) Stil rehberi 5G-b'nin ilk işi, asset üretimi SINIRLI** (yalnız bu maç gününün ihtiyacı) ← **önerilen** | 4G.5'e uyar; seri üretim (GDD FAZ 05) kapının arkasında kalır | Rehber işi dilime süre ekler |
| (b) Placeholder ile devam, stil rehberi FAZ 05'te | Hızlı | "Final kalite" iddiası düşer → kapı kanıtı üretilemez |
| (c) Tam stil rehberi + geniş asset seti | Sonraki fazlar hazır girer | 19. hata: fun sinyali gelmeden büyük asset harcaması |

### D-E. Analytics sağlayıcı (ADR gerektirir)

GDD 9.5/17 **Firebase Analytics** diyor. Anayasa privacy tabanı **TelemetryDeck + MetricKit**
diyor ve yeni bağımlılık = ADR. İkisi çelişiyor; bu dilim ilk gerçek event'leri yazacağı için
karar burada gerekiyor. Öneri: **TelemetryDeck + MetricKit** (anayasa varsayılanı, privacy
varsayılan), Firebase'in GDD'de kalması bir sapma olarak ADR'ye yazılır. **Karar Atilla'nın.**

---

## 6. Kapı kanıtları — Vertical Slice Gate (4G.7 + DoD-G + 9.7)

Kapı bu listenin tamamı yeşil olmadan geçilmez:

- [ ] Desteklenen **en düşük cihaz dahil gerçek cihazda fps ölçümü** + profiler raporu
- [ ] **Build boyutu** kaydı
- [ ] **Batarya/termal gözlem notu**
- [ ] **Oyunu tanımayan birinin FTUE'yu yardımsız geçmesi**
- [ ] Monetizasyon anı **sandbox'ta gerçek** (test satın alma tamamlanıyor)
- [ ] **Analytics düşüyor** (FTUE hunisi dahil, event sözlüğüne uygun)
- [ ] **Persona paneli sentez raporu** (9.7 — Dikey Dilim'de ZORUNLU, 3-5 persona kör + paralel)
- [ ] DoD-G: Unity konsolu temiz · EditMode/PlayMode testleri yeşil · oynanış kaydı · profiler
      önce/sonra · varsayım ve kalan risk raporu
- [ ] `dotnet run --project shared/TheBadge.Sim.Checks -c Release` yeşil (C# tarafı regresyon yok)

---

## 7. Kim neyi kanıtlayabilir — iş bölümü

Bu dilimin FAZ 03/04'ten farkı budur ve baştan yazılması gerekiyor.

| Bu ortamda KANITLANABİLİR (Claude) | Yalnız Atilla koşabilir |
| --- | --- |
| Paket sınırları: `package.json` + asmdef + bağımlılıksızlık (derleme dışı statik denetim + `Sim.Checks` kapısı) | Unity Editor'de gerçek derleme; konsolun temiz olması |
| Saf C# kurallar/ekonomi/ilerleme mantığı ve testleri (4G.9: "motor-bağımsız saf C#") | EditMode/PlayMode testlerinin Unity koşucusunda yeşil olması |
| Katalog/komut sözleşmesi, bant doğrulama, determinizm kapıları | Cihazda fps, profiler, build boyutu, termal |
| Telemetri event **sözlüğü** ve şema kapısı | Gerçek analytics SDK entegrasyonu ve event'in düşmesi |
| IAP ürün kataloğu/şema ve sunucu doğrulaması | StoreKit sandbox satın alma |
| Persona paneli koşusu ve sentezi (9.7 mekanizması) | **İnsan playtest'i** — hiçbir otomasyonun yerine geçemez (4G.9) |

**Kural:** kanıtlanamayan kod yazılmaz. Nakama adaptörünün FAZ 04'te yazılmamasının gerekçesi
buydu; aynı disiplin burada da geçerli — cihaz/Unity/insan gerektiren hiçbir madde "yapıldı"
diye raporlanmaz.

---

## 8. Bu dilimde YAPILMAYACAKLAR

- **60+ ekranlık UI seti** (GDD FAZ 02) — dikey dilim tek maç günüdür, ekran kataloğu değil.
- **Seri AI asset üretimi** (GDD FAZ 05) — kapının arkasında (DECISIONS v2.1 uyum turu + 4G.5).
- **Nakama üretim bağlaması / PostgreSQL persist / keyframe yayını** — kapanış brifi bölüm 4'ün
  borcu; tetikleyicisi "Nakama + PostgreSQL koşturulabilen ortam". Dikey dilim tek cihazda tek
  oyuncunun maç günüdür; online lig bu dilimin iddiası DEĞİL.
- **Android** — D4/ADR-001: iOS-first soft launch, Android global lansmanda.
- GAME_THESIS v1 non-goal'ları aynen: 3D maç, senkron PvP, gerçek lisanslar, rewarded ads.
- **Doğrudan kırmızı olay sınıfı** — tetikleyicisi FAZ 05 hakem dilimi (DECISIONS, K13-C).

---

## 9. Riskler

| Risk | Neden gerçek | Bugünkü karşılığı |
| --- | --- | --- |
| **Fun sinyali yine gelmezse** | %40 bir kez ölçüldü; nedeni bilinmiyor | 5G-a'nın tek amacı bu: ucuz turda öğren, pahalı turdan önce |
| **Dünya-motor dikişi UI'da yeniden kopar** | K11'in dersi: iki taraf ayrı ayrı yeşilken dikiş ölçülmemişti | S1 + S3 dikişi UI'dan geçiriyor; kapı gerçek kurulum kodunu koşmalı (K11'in ikinci uygulaması gibi) |
| **Unity paketleri C# 9 / bağımlılıksızlık disiplininden sapar** | `World` bugün .NET-only; Unity'ye girerken kısıtları karşılamalı | S1'in kabul kapısı: `noEngineReferences: true` + dış paket yok |
| **"Final kalite" tanımsız kalır** | Stil rehberi yok | D-D kararı |
| **Cihaz performansı** | LOD 0 bir maç ORTA cihazda > 800 ms olursa LOD 1 ayrışır (kapanış brifi borcu) | S6 bu borcun da tetikleyicisi — ölçüm burada yapılır |
| **Ölçümsüz playtest tekrarı** | Bir kez oldu: mülakat/telemetri kaydedilmedi | Kayıtlı kural: **mülakatsız playtest koşulmaz** (GREYBOX_3G_RAPOR kapanışı) |

---

## 10. Açılış koşulu

Bu brif bir plan önerisidir, kapı değil. **D-A ve D-C 2026-09-04'te kapandı** (DECISIONS +
ADR-002) ve **S1 uygulandı**; sıradaki iş **S2** — maç sunumunun gerçek motor üstünde yeniden
tasarımı ve mülakatlı gözlem turu. **D-B, D-D ve D-E hâlâ açık:** D-B (dilim kapsamı) S2 biterken,
D-D (stil rehberi) ve D-E (analytics sağlayıcı — GDD↔anayasa çelişkisi) 5G-b başlamadan kapanmalı.
Kararlar `docs/DECISIONS.md`'ye işlenir; sohbette kalan karar yok hükmündedir (Anayasa 9 +
CLAUDE.md).
