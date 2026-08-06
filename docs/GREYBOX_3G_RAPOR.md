# 3G Greybox Teslim Raporu — Claude Code tarafı (FAZ 00.5)

Tarih: 2026-07-30 · Branch: `claude/3g-greybox-task-plan-76qg49` · Brif: `docs/briefs/BRIEF_3G_GREYBOX.md`
Plan onayı: Atilla (aynı gün) — portre/dikey saha; greybox ayarları ayrı dosyada (config_hash dışı); kodla uGUI.

## Ne kuruldu

| Katman | İçerik | Konum |
|---|---|---|
| Unity iskeleti | Unity 6 LTS (pin: 6000.3.21f1) proje: manifest (`com.thebadge.sim` local package), ProjectSettings (portre, legacy input), tek sahneli Bootstrap deseni, tüm .meta'lar sabit GUID'li | `unity/TheBadge/` |
| FlowSim | Motor bağımsız akış simülasyonu: bölge/pas dalgalanması, momentum (OU), şut/gol/korner/kurtarış, 22 nokta formasyon hareketi, devre yapısı. **ME Spec motoru DEĞİL** (Brif K2) | `Assets/Greybox/Scripts/Sim/` |
| Sunum | Daire/dikdörtgen saha, 1x/2x/önemli-ana-atla, gol vurgusu (slow-mo + kamera titremesi + flaş), banner/ticker | `Scripts/View/`, `Scripts/Loop/MatchDirector.cs` |
| Core loop | Maç öncesi (3 taktik + kadro) → Maç → Maç sonu (skor + bilet geliri + prim) → bilet slider'ı (canlı doluluk/gelir önizleme, GDD 4.2) → Sonraki Maç; mini-save | `Scripts/UI/`, `Scripts/GreyboxBootstrap.cs` |
| Tek Kapı (hafif) | Gerçek `CommandEnvelope` + `GreyboxCommandBus`: `greybox.select_tactic`, `tycoon.set_ticket_price` (bant kontrolü), `greybox.next_match`. 4 kapılı tam doğrulama FAZ 04 borcu | `Scripts/Loop/GreyboxCommandBus.cs` |
| Telemetri | JSONL yerel log: session/match start-end, goal, speed, skip, ticket_price_set, next_match_click; satır başına flush | `Scripts/Sim/TelemetryLog.cs` |
| Testler | 9 EditMode testi (pacing bantları, determinizm-lite, skip erişimi, ekonomi monotonluğu, bus red/uygulama, form penceresi) | `Assets/Greybox/Tests/EditMode/` |
| Playtest kiti | Doldurulacak kapı formu + örnek telemetri | `docs/PLAYTEST_3G.md`, `docs/samples/telemetry_ornek_oturum.jsonl` |

Tüm his/ekonomi sayıları **[KALİBRE-G]** olarak `Assets/Greybox/Resources/greybox.balance.json`'da (koda gömülü sayı yok). Bu dosya `balance/sim.balance.json`'dan ayrıdır ve **config_hash dışıdır**; Fun Gate kapanınca prototiple birlikte emekli edilir.

## Kanıtlar (bu ortamda koşuldu)

1. **Çekirdek kapısı:** `dotnet run --project shared/TheBadge.Sim.Checks -c Release` → `== TUM KONTROLLER YESIL ==` (çekirdek bozulmadı; imzalara dokunulmadı).
2. **Headless pacing taraması** (FlowSim saf C# olduğundan Unity'siz koşuldu; 300 maç, karışık taktik/güç):
   - ort. gol **2.20** (0-0: %9, 6+ gol: %2; dağılım 1-3 gol ağırlıklı)
   - ort. şut **11.1** (isabet 6.4) · ort. korner **3.3**
   - 1x hızda maç gerçek süresi ort. **165.6 sn** (duraklamalar dahil; hedef ~150 sn aktif + duraklamalar)
   - güç farkı anlamlı: 60'lık takım 48'lik rakibe **30/60**, 72'lik rakibe **15/60** galibiyet
   - determinizm-lite: aynı seed + aynı adım deseni = aynı skor/şut (kaçak `System.Random` yok kanıtı)
   - ekonomi: ref fiyatta doluluk = talep tabanı; fiyat↑ → doluluk↓; bant dışı fiyat komutu `ParamOutOfBand` ile RED
3. **Derleme temizliği:** Sim + Loop + View + UI + testler, C# 9 / netstandard2.1'de **0 hata, 0 uyarı** (UnityEngine API yüzeyi stub'lanarak; gerçek Editor derlemesi Atilla'nın ilk açılışında doğrulanacak).
4. **Örnek telemetri:** `docs/samples/telemetry_ornek_oturum.jsonl` — 3 maçlık sentetik oturum; kapı metriklerinin logdan hesaplanabilirliğini gösterir.

## DoD-G durumu

| Kanıt | Durum |
|---|---|
| `Sim.Checks` yeşil | ✅ (yukarıda) |
| Unity konsolu temiz (0 error/0 warning) | ⏳ Atilla — ilk açılışta doğrulanır (stub derlemesi temiz; risk düşük) |
| 30-60 sn oynanış kaydı | ⏳ Atilla (runbook adım 7) |
| Hedef cihazda akıcılık notu (60 fps) | ⏳ Atilla — sahnede ~40 sprite + hafif UI var; profiler gerekirse önce/sonra |
| Varsayım-risk raporu | ✅ (aşağıda) |
| Telemetri örneği + PLAYTEST_3G şablonu | ✅ |

## Neyi test ettik / ETMEDİK (varsayım-risk)

**Test edilen:** akış üretiminin pacing bantları; maçın her seed'de bitmesi (kilitlenme yok); skip'in her durumda önemli ana ya da maç sonuna ulaşması; ekonomi formüllerinin monotonluğu ve bantları; komut reddi yolları; form penceresi; telemetri format geçerliliği.

**Test EDİLMEYEN (bilinçli):**
1. **His** — kapının asıl sorusu. Sayısal pacing ≠ izleme keyfi; yalnız playtest cevaplar (RA#1).
2. Unity Editor/cihaz gerçek derleme-çalıştırma: paket çözümleme, ProjectSettings migrasyonu, sahne ilk import'u, dokunmatik hedef boyutları. Stub derlemesi tip/sözdizimi garantisi verir, davranış garantisi vermez.
3. Cihazda fps/ısınma — greybox yükü çok düşük ama iddia kanıtsız; Atilla notu bekliyor.
4. Uzun oturum dayanıklılığı (50+ maç üst üste), save migrasyonu, kilitli ekran/arka plan geçişleri.

**Bilinen sınırlar / sıradaki riskler:**
- Takımlar devre arasında saha DEĞİŞTİRMEZ (okunabilirlik tercihi); playtester yadırgarsa nota geçir.
- Uzatma dakikaları yok (90'da biter); korner ortalaması (3.3) gerçek futboldan düşük — his için yeterli varsayıldı.
- Ekonomide doluluk tabanı (%5) yüzünden çok yüksek fiyatta gelir eğrisi düzleşip hafif yükselir; optimal bölge ~ref fiyat civarı olduğundan sömürülemez ama slider ucunda görünür.
- ~~`activeInputHandler=0` (eski Input Manager): FAZ 02'de Input System'e geçiş bilinçli borç.~~ **Kapatıldı (2026-07-31):** 6000.3 LTS eski Input Manager'ı deprecation uyarısıyla işaretlediği için Input System'e geçildi (`com.unity.inputsystem` + `InputSystemUIInputModule`; paket yoksa `#if ENABLE_INPUT_SYSTEM` koruması eski yola düşer).
- Skip sırasında gelen goller vurgusuz geçer (ticker'a düşer) — tasarım gereği, playtest'te gözle.

## İterasyon 1 — Atilla'nın ilk oynayış geri bildirimi (2026-07-31)

| Geri bildirim | Yapılan |
|---|---|
| "Yazılar ekrana sığmıyor" | İki ayaklı: (1) CanvasScaler genişlik-eşleme (0) yapıldı — 19.5:9 uzun ekranlarda %2 taşma vardı; (2) runbook'a Game penceresi 1080×1920 portre preset adımı eklendi (16:9 yatay görünümde taşma normaldir, oyun portre kilitli). |
| "Top oyunculardan çok uzakta" | Paslar artık gerçek takım arkadaşına gidiyor (`PickReceiver`/`PickReceiverNearGoal`); topu alan oyuncu **taşıyıcı** oluyor ve topla oynuyor; korner kullanıcısı köşeye gidiyor, ortayı bir hücumcu karşılıyor; kurtarışta top kalecide. |
| "Sonuç güçten bağımsız" | Güç farkı artık üç kanala işliyor: top tutma (`flow.gucTutmaCarpan` 0.008), şut kalitesi (`gucEtkiCarpan` 0.004), momentum eğimi (0.010). Harness: zayıf rakibe (48) galibiyet 36/60, güçlüye (72) 6/60 — önceki 30/60–15/60'tan belirgin ayrışma. |
| "Akan metin olsa" | Spiker satırları eklendi: olay yorumları (şut/kurtarış/korner/gol/atak başlangıcı `ChanceStart` olayı) + boşta akış cümleleri (`pace.spikerAralikSn` 8 sn) — dakika damgalı ticker. |
| "Gol anında telefon titresin" | `Handheld.Vibrate()` cihazda gol vurgusuna bağlandı (`vurgu.titresimAktif` [KALİBRE-G], yalnız iOS/Android derlemesi). |
| "Maç süresi ideal, belki tık uzun" | 150 sn korundu; müdahale mekanikleri gelince `clock.macSuresiSaniye` tek satırla uzatılır. |

İterasyon sonrası kanıt: 300 maç harness yeşil (2.21 gol / 10.2 şut / 3.0 korner / 166 sn); determinizm-lite geçer; tüm derleme yolları 0 hata / 0 uyarı; Sim.Checks yeşil.

**Sahip his notu (2026-07-31, iterasyon 1 sonrası):** Atilla hissi henüz "tamam" bulmuyor; süreç playtest'e taşındı. Kapı kararı 4G.4 gereği yalnız playtest metriğinden çıkar ("his eksik ama ileride düzelir" gerekçesi FAZ 02/05 kilidini AÇMAZ); Atilla'nın kendi gözlemi playtest formuna tasarımcı notu olarak ayrıca işlenecek.

## İterasyon 2 — "İzlemek acı verici" sahneleme turu (2026-07-31)

Atilla'nın somutlaştırdığı üç kopma anı + kök neden düzeltmesi:

| Geri bildirim | Yapılan |
|---|---|
| "Kornerde herkes rakip sahada değil" | Korner sahnelemesi: orta öncesi diziliş beklemesi (`corner.dizilisSn` 2.2 sn [KALİBRE-G]); hücum kutu içi 6 + kutu önü 2 + kontra sigortası 2 noktaya doluşur, savunma kutuda gol tarafında adam tutar, 2 forvet kontrada bekler; korner kullanıcısı köşede, ortayı kutudaki bir hücumcu karşılar. |
| "Gollerde top ağlara gitmiyor" | Şut hedef derinliği sonuca bağlandı: gol ağların İÇİNDE biter (+1.7 m) ve kutlama boyunca ağlarda kalır; kurtarış kaleci önünde ölür (ışınlama kaldırıldı); aut çizgiyi geçer. Kale görseli: belirgin ağ kutusu + parlak kale ağzı barı. |
| "Gol sevinci için toplanmıyorlar" | Kutlama kümelenmesi: atan takım skorer noktasında dar halkada toplanır (sevinç sprinti ×1.25, süre 3.4 sn), kaleci katılmaz, yiyen takım santraya döner. |
| "Top hareketleri yapay/alakasız" (kök neden) | Pas havadayken ALICI buluşma noktasına koşar — top artık boş alana düşüp beklemez; pas isabeti sıkılaştırıldı (±1.5 m → ±0.6 m); amaçsız salınım 2.4→1.6 m. |

Kanıt: 300 maç harness yeşil (2.13 gol / 10.4 şut / 3.1 korner / 167 sn); güç ayrışması 45/60 vs 8/60; determinizm-lite geçer; tüm derleme yolları 0 hata/0 uyarı; Sim.Checks yeşil. **His onayı Atilla'nın yeniden oynayışına bağlı.**

## İterasyon 3 — Sahneleme senaryosuna hizalama (2026-07-31/08-01)

Süreç değişikliği (Atilla kararı): sahneler önce yazıyla sabitlendi (`docs/GREYBOX_SAHNELEME.md`, onaylı v1),
kod senaryoya hizalandı. Kök ilke: **sahneler süreyle değil DİZİLİŞ KOŞULUYLA başlar.**

| Senaryo maddesi | Uygulama |
|---|---|
| Santra: herkes kendi yarısında + rakip çember dışı + forvet topun başında olmadan düdük YOK | `KickoffReady()` koşulu + kendi-yarı-saha kilitli santra dizilişi (`KickoffTarget`); santra pası geriye/yana kısa pas; gol sonrası da aynı koşul beklenir |
| Korner: kutu dolmadan orta GELMEZ (hücum ≥5 + savunma ≥5 kutuda + taker köşede) | `CornerReady()` koşulu; emniyet ~8 sn [KALİBRE-G dizilisEmniyetSn] |
| Aut → kale vuruşu sahnesi | Yeni `GoalKick` fazı: top kale sahasına, kaleci başına, rakip kutuyu boşaltır, kaleci pasıyla devam |
| Kurtarış → kaleci topu tutar | `gkTutmaSn` 1.5 sn [KALİBRE-G], sonra kısa dağıtım |
| Diziliş beklemeleri maç saatini yemesin | Staging duraklamalarında maç saati durur — 90 dk saf akışa ait |
| Sahne sözleşmesi testleri | Harness: 300 maçta 1211 santra + 908 korner geçişi denetlendi, **0 ihlal, 0 emniyet devreye girişi**; gol topunun ağda bitişi de denetleniyor. EditMode aynası: `SahneSozlesmesiTests` |

Pacing: 2.04 gol / 10.0 şut / 3.0 korner; 1x maç ~197 sn (sahneleme gerçek süreye eklendi — 2x/skip telafi eder).
Güç ayrışması: 43/60 vs 7/60. Tüm derleme yolları 0 hata/0 uyarı; Sim.Checks yeşil.

## İterasyon 4 — 2x pürüzsüzlüğü + top fiziği (2026-08-01, Sahneleme v1.1)

Geri bildirim: "2x hızda top-oyuncu dinamikleri karışıyor; fizik kuralları iyileşmeli."
Kök neden: kare başına örnekleme — hız arttıkça pozisyonlar her karede iki kat zıplıyordu (sim değil sunum sorunu).

| Değişiklik | Etki |
|---|---|
| **Sabit sim adımı (0.05 sn) + kareler arası interpolasyon** (MatchDirector/PitchView) | Her hızda (1x/2x/slow-mo) pürüzsüz hareket; sim kare hızından tamamen bağımsızlaştı; skip sonrası interpolasyon sıfırlanır |
| **Pas yavaşlaması** (son 12 m'de %55'e iner; şutlar sert kalır) | Top "ayağa gelir", robotik sabit hız gitti |
| **Top-ayak yapışması + dripling** (GlueBallToCarrier; taşıyıcı uzaksa önce topa gider) | Top ile taşıyıcının ayrı gezmesi bitti; taşıma görüntüsü gerçek dripling |

Kanıt: 300 maç — 1.99 gol / 9.4 şut / 2.8 korner / 193 sn; sahne sözleşmesi 1197+830 geçişte 0 ihlal;
determinizm-lite geçer; iki derleme yolu 0 hata/0 uyarı; Sim.Checks yeşil.

## İterasyon 5 — Yükseklik fiziği + yön + ışınlama yasağı (2026-08-01, Sahneleme v1.2)

| Geri bildirim | Yapılan |
|---|---|
| "Korner sonrası sahne atlayıp top orta sahadan devam ediyor" | Kök neden: korner uzaklaştırması topu 20 m ışınlıyordu. Artık uzaklaştırma HAVADAN uçan gerçek bir top: kutu dışına süzülür, kapan takımın oyuncusu buluşma noktasına koşar. Işınlama senaryoda YASAK (v1.2) ve yükseklik bandı harness'ta denetleniyor. |
| "Oyuncuların yönü belli olsun (ayak çıkıntısı)" | Her dairede kenara oturan koyu "ayak/burun" işareti: hareketteyken gidilen yöne, dururken topa döner (yumuşatılmış dönüş). |
| "Top yükselince büyüsün, düşünce küçülsün (perspektif)" | Top yüksekliği simüle ediliyor (parabolik yay): kısa pas yerden; uzun top/korner ortası/degaj/uzaklaştırma havadan. Sunumda top yükseldikçe BÜYÜR, yerdeki gölgesinden ayrılır (kaldırma), düşünce ayağa iner. [KALİBRE-G]: yük tepeleri + ölçek/kaldırma çarpanları. Tam izometrik/eğik kamera bilinçli kapsam dışı (2.5D prerender FAZ 05+); greybox üstten bakışta yükseklik hissini ölçek+gölgeyle verir. |
| Proaktif fizik süpürmesi | Kaleci artık sık sık YÜKSEK degaj kullanır (`flow.pDegaj`, kapılma riskli); kafa vuruşları alçak/sert; şut sert kalır. |

Kanıt: 300 maç — 1.96 gol / 9.1 şut / 2.7 korner / 193 sn; sahne sözleşmesi (1187 santra + 797 korner + yükseklik bandı) 0 ihlal; determinizm-lite geçer; derleme yolları 0/0; Sim.Checks yeşil.

## İterasyon 6 — Motor kararı: Sahiplik Değişmezi (2026-08-02, Sahneleme v1.3)

Atilla'nın "motoru komple gözden geçir, literatüre bak, yol öner" talebi üzerine üç yol sunuldu
(A: sahiplik retrofit'i — Buckland/Simple Soccer modeli; B: temiz yeniden yazım; C: FM-tarzı
koreografi/replay). **Karar: Yol A (Atilla).**

Uygulanan değişmez: top hiçbir an ÖZERK değil — ya bir oyuncunun ayağında, ya İSİMLİ bir uçuşta
(vuran X → alan Y), ya duran top noktasında, ya SERBEST (ilk ulaşan alır; serbest topta karar üretilemez).

| Mekanizma | Uygulama |
|---|---|
| Karar kapısı | Pas/şut kararları yalnız `CarrierHasBall()` (≤1.6 m) sağlanınca üretilir — "kimse yokken pas" YAPISAL olarak imkânsız |
| Vuruş kaydı | Her `SendBall` vuran oyuncuyu kaydeder (`KickCount`/`LastKickDist`); duran top transportları hariç |
| Temiz çalma / açık top | Top kaybında pres yakınsa (≤2.5 m) temiz çalar; uzaksa top AÇIĞA çıkar, iki takımın en yakını kapışır (4 sn emniyet) |
| Korner karambolü | Ortayı karşılayan uzaksa kafa YOK — top kutuda serbest kalır (karambol); uzaklaştıran savunmacı da topun yanında olmak zorunda |
| Denetim | Sahne sözleşmesine yeni assert: hiçbir vuruş vuran topun yanında değilken olamaz (kontrol ~1.6 m; kafa/uzaklaştırma uzanması ≤ ~2.2 m; denetim toleransı 2.4 m). İlk koşuda 26 sınır ihlali yakalandı → uzanma payı sözleşmeye yazıldı, denetim hizalandı |
| Ayak uçları | Yön işareti tek burun yerine ÖNDE YAN YANA İKİ ayak ucu çıkıntısına çevrildi (Atilla) |

Kanıt: 300 maç — 1.95 gol / 9.3 şut / 2.7 korner / 192 sn; sahne sözleşmesi (santra + korner +
gol-ağda + vuruş-yakınlığı) 0 ihlal; determinizm-lite geçer; derleme yolları 0/0; Sim.Checks yeşil.

## İterasyon 7 — Canlı top saati + takılma emniyeti + perspektif (2026-08-02, Sahneleme v1.4)

Kök neden analizi: "2x'te az pozisyon" ile "top kalecide takıldı" AYNI hataydı — kaleci taşıyıcıyken
topa yürümüyordu (hedef formülü kale önüne kilitliyordu) → sahiplik kapısı karar üretemiyordu →
akış donarken MAÇ SAATİ İŞLEMEYE devam ediyordu → 90 dakika boş yanıyor, pozisyon azalıyordu.

| Değişiklik | Etki |
|---|---|
| **Canlı top saati** | Maç dakikası yalnız top oyundayken (uçuş/kontrol) işler — beklemeler ve olası donmalar 90'ı yiyemez; pozisyon sayısı hızdan bağımsız |
| **Kaleci topa gider** | Taşıyıcı kaleciyse hedefi top (kurtarış/kale vuruşu takılması bitti) |
| **Takılma bekçisi** | Taşıyıcı 4 sn'de topa ulaşamazsa top serbest kalır (genel donma emniyeti, sayaç loglanır) |
| **Arrive yavaşlaması** | Oyuncular hedefe son 1.5 m'de yavaşlayarak varır (Buckland steering sentezi) |
| **TV perspektifi + gölgeler** | Sahne dikey ezme [KALİBRE-G perspektifYSkala 0.88] + oyuncu temas gölgeleri; tam izometrik FAZ 05+ |
| **Ayak uçları** | Aralık 0.17 → 0.24 (belirgin çift çıkıntı) |

Kanıt: 300 maç — **2.24 gol / 10.7 şut / 3.2 korner** (canlı saat aksiyon yoğunluğunu geri getirdi;
0-0 %9); 1x maç ~210 sn (2x ~105 sn); sahne sözleşmesi 1273 santra + 968 korner + vuruş-yakınlığı
denetiminde 0 ihlal, 0 emniyet; determinizm-lite geçer; derleme yolları 0/0; Sim.Checks yeşil.

## İterasyon 8 — FUN GATE PİVOTU: Model Maçı (2026-08-02, Sahneleme v2.0, DECISIONS kaydı)

Atilla kararı: RA#1 revize edildi — test edilen his artık "model + görünür olasılıklar + müdahale
döngüsü". Ana ekran model; 2D motor gol bloklarında highlight VİNYETİ.

| Bileşen | Uygulama |
|---|---|
| `MatchModel` (saf C#) | 10 aksiyon bloğu; blok gol olasılıkları AÇIK formülle (güç + taktik etkileşimi + momentum + tempo modu, hepsi [KALİBRE-G model.*]); zar Rng ile; KESİN DP kazanma dağılımı (Monte Carlo değil) |
| Müdahale (Tek Kapı) | `model.set_tactic` / `model.set_tempo` CommandEnvelope ile; hamle hakkı [KALİBRE-G hamleHakki=3]; hak bitince `NoChargesLeft` (CB Spec 11.1); her müdahalede şerit anında yeniden hesaplanır, feed'e "G %38→%45" düşer |
| Model ekranı | Canlı G/B/M kazanma şeridi (animasyonlu) + momentum çubukları + spiker feed'i + blok kartı ("Gol ihtimali BİZ %18") + müdahale barı + 1x/2x/atla |
| Vinyet | `VignetteRecorder`: headless FlowSim golü arar (5 deneme + şut fallback), son 8 sn'yi kare kare kaydeder; rakip golünde sahne aynalanır + takım renkleri değişilir; oynatmada vurgu paketi (shake+flaş+titreşim) gol karesinde |
| Eski canlı 2D akış | `MatchDirector` + `FlowSim` korunuyor (vinyet motoru + olası A/B modu); tüm sahne sözleşmesi testleri yeşil kalmaya devam ediyor |

Kanıt: model 400 maç — ort. **2.88 gol**; kazanma şeridi KALİBRE (tahmin %38 vs gerçekleşen %44,
<0.10 bant); güç ayrışması ve tempo-risk iki yönlülüğü doğrulandı; vinyet iki takım için de üretiliyor.
FlowSim sahne sözleşmesi + pacing testleri değişmeden yeşil. Derleme yolları 0/0; Sim.Checks yeşil.
Yeni EditMode seti: `ModelMatchTests` (7 test — DP toplamı/simetri, güç, tempo-risk, hamle hakkı
bus reddi, determinizm, pacing+kalibrasyon, vinyet).

## Atilla'nın sıradaki adımları

`unity/UNITY_SETUP.md` runbook'u: projeyi aç → konsol/testler → Editor'de oyna → iPhone build → 3-5 oyuncu playtest → `docs/PLAYTEST_3G.md` doldur → kapı kararı (GO/NO-GO) → DECISIONS.md.
