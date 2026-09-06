# Unity 6 Proje Kurulumu

> **Durum (5G-a / S2, 2026-09-06):** `unity/TheBadge` proje iskeleti repo'da HAZIR.
> Unity Hub'dan yeni proje AÇMAYA gerek yok; aşağıdaki adımları izle.
>
> **GREYBOX EMEKLİ.** Fun Gate kapandı (`docs/GREYBOX_3G_RAPOR.md`) ve K3 kararıyla arşivlendi:
> klasör `Assets/Greybox~/` oldu ve Unity `~` ile biten klasörleri İÇE AKTARMAZ. Bu runbook
> FAZ 00.5'te greybox'ı tarif ediyordu; artık koşulan ekran **maç sunumu**dur. Motor test
> sahnesi (`EngineDev`) arşivden ÇIKARILDI ve `Assets/EngineDev/` altında yaşamaya devam ediyor.

## Maç Sunumunu Çalıştırma (Atilla runbook'u — 5G-a)

1. **Unity sürümü:** Unity Hub → Installs → **Unity 6 LTS (6000.3.x, Apple Silicon)** kurulu olsun (iOS Build Support modülüyle). 6000.5 gibi LTS-dışı akımlarla AÇMA — proje tek yönlü yükselir, LTS'e dönüş desteklenmez.
2. **Projeyi aç:** Hub → Add → `unity/TheBadge` klasörünü seç → aç.
   - Pinli sürüm `6000.3.21f1`; sendeki 6000.3.x patch'i farklıysa Unity sürüm onay diyaloğu gösterir → **onayla**. İlk açılış paket çözümleme + ProjectSettings migrasyonu nedeniyle birkaç dakika sürebilir.
   - İlk açılışta oluşan `Packages/packages-lock.json` ve ProjectSettings'te Unity'nin tamamladığı alanları **commit et** (tek seferlik).
3. **Doğrulama:** Project panelinde `Packages → The Badge Sim Core` görünmeli (`com.thebadge.sim` local package). Console'da 0 error / 0 warning hedef.
4. **Oyna:** `Assets/Match/Scenes/MacSunum.unity` sahnesini aç → Play. (Build Settings'te de bu sahne kayıtlı.) Game görünümü PORTRE olmalı:
   Game penceresi üst barındaki çözünürlük menüsü → **+** → Type: *Fixed Resolution*, W:1080 H:1920, ad "Portre 1080x1920" → onu seç. (16:9 yatayda UI bilerek taşar — oyun portre kilitli.)
5. **EditMode testleri:** Window → General → Test Runner → EditMode → Run All. Hepsi yeşil olmalı
   (`Game.Match.EditModeTests`: sunum okuma sözleşmesi, Tek Kapı zinciri, iki red yolu,
   determinizm, duraklama ritmi). Ritim testleri ~30 sn sürer — 24 maç koşuyorlar.
   Greybox'ın dört EditMode dosyası artık KOŞMUYOR; kabul edilmiş bedeldir (K3), ölçtükleri
   kod emekli.
6. **Cihaz build'i (iPhone):** File → Build Settings → iOS → Build; Xcode projesini imzala, cihaza yükle. Orientation Portrait olarak ayarlı.
7. **Ekran kaydı (DoD-G):** 30-60 sn — bir maçın başı, kazanma şeridi, bir kritik an duraklaması,
   bir taktik müdahalesi (şerit AYNI TICK oynamalı), maç sonu + "BİR MAÇ DAHA".
   Örnek kareler: `docs/gorseller/TASK-002/`.
8. **⚠️ Telemetri: HENÜZ YOK.** Greybox'ın `TelemetryLog`u arşivle birlikte gitti ve maç sunum
   ekranı telemetri YAZMIYOR. `docs/PLAYTEST_3G.md`'nin "Telemetri özeti" tablosu (izleme sn/maç,
   skip/maç, 2x, müdahale/maç) bugün DOLDURULAMAZ — geçen tur da tam burada eksik kalmıştı.
   Gözlem turundan ÖNCE kapatılması gereken iş budur; format `docs/samples/telemetry_ornek_oturum.jsonl`.
9. **Save sıfırlama:** gerekmiyor — bu ekran kalıcı durum yazmıyor (maç dışı dünya S3'ün işi).
   Yeni maç için ekrandaki "BİR MAÇ DAHA" yeter; her maç yeni tohum alır.

Sorun giderme:
- "balance dosyası bulunamadı" hatası → ekran `balance/`i REPO KÖKÜNDEN okur
  (`Application.dataPath/../../../balance/`). Projeyi repo dışına kopyaladıysan bu yol kırılır.
- Paket çözümleme hatası → Hub'daki Unity sürümünde iOS modülü ve internet olduğundan emin ol; `com.unity.ugui`/`com.unity.test-framework` editor önbelleğinden gelir.

## Assets mimari notu (5G-a)

- `Assets/Match/` → maç sunumu. `MacKosucu` motoru koşturur ve sunuma YALNIZ OKUMA yüzeyi verir
  (`MatchState` ve `CommandQueue` dışarı hiç çıkmaz — Tek Kapı yapısal olarak korunur).
  `MacKomutKoprusu` `CommandBus → WorldExecutor → SquadActions → motorun kuyruğu` zincirini kurar.
  Ekranın tüm ayarlanabilir sayıları `MacSunumAyarlari`de, `[KALİBRE]` ADAYI olarak.
- `Assets/EngineDev/` → motor test sahnesi (`EngineDev.unity`). Geliştirici aracıdır, BUILD'E
  GİRMEZ: balance'ı repo kökünden okur, bir build'de zaten çalışmaz.
- `Assets/Greybox~/` → **arşiv.** Unity içe aktarmaz, derlenmez, bakım yükü yoktur. Silinmedi;
  git'te duruyor (K3 kararı).
- Sahne neredeyse boş: `MacSunum` objesi arayüzü runtime'da UI Toolkit ile kurar (K2 kararı;
  UXML/USS dosyası yok). Elle sahne düzenlemesi gerekmez.

## Paylaşılan paketler (5G S1 — ADR-002)

Unity üç yerel paketi `manifest.json` üzerinden `shared/` altından alır; hepsi
`noEngineReferences: true` (CLAUDE.md değişmez #3) ve dış paket referansı yok:

| Paket | Klasör | asmdef referansları |
| --- | --- | --- |
| `com.thebadge.sim` | `shared/TheBadge.Sim` | — |
| `com.thebadge.commandbus` | `shared/TheBadge.CommandBus` | `TheBadge.Sim` |
| `com.thebadge.world` | `shared/TheBadge.World` | `TheBadge.Sim`, `TheBadge.CommandBus` |

> **Paket klasörüne `.cs` bırakma.** Unity paket klasöründeki TÜM `.cs`'i derler; MSBuild'in
> ürettiği `obj/**/*.AssemblyInfo.cs` orada kalırsa Unity CS0579 ile düşer. Üç pakette
> `Directory.Build.props` çıktıyı repo kökündeki `artifacts/`e yönlendirir — o dosyaları silme.
>
> Bu maddelerin hepsini `S1UnityPaketSiniri` kapısı her koşuda ölçüyor
> (`dotnet run --project shared/TheBadge.Sim.Checks -c Release`).

## Assets asmdef Haritası (5G S1'de kuruluyor)

| asmdef | İçerik | Referanslar |
| --- | --- | --- |
| Game.Commands | Command Bus istemci ucu, katalog önbelleği | **TheBadge.CommandBus**, TheBadge.Sim |
| Game.Services | Nakama istemcisi, save/load, telemetri | Game.Commands, **TheBadge.World** |
| Game.UI | UI Toolkit ekranları, Rive köprüleri | Game.Services |
| Game.Match | Maç sunum katmanı — **5G-a'da KURULDU** | TheBadge.Sim, **TheBadge.CommandBus**, **TheBadge.World** |
| Game.EngineDev | Motor test sahnesi (build dışı) | TheBadge.Sim |
| Tests.EditMode / Tests.PlayMode | Unity testleri | ilgili modüller |

> Bu harita FAZ 01'de yazılmış ve FAZ 04'ten ESKİYDİ: `Game.Commands`ı `TheBadge.Sim`e bağlıyordu,
> oysa Command Bus ayrı bir pakette. ADR-002 ile düzeltildi.
>
> FAZ 00.5'te bilinçli sapma tek `Game.Greybox` asmdef'iydi; greybox **emekli** (Fun Gate kapandı,
> `docs/GREYBOX_3G_RAPOR.md`), beş modüllü harita 5G Dikey Dilim'de kuruluyor.
>
> **`Game.Match` 5G-a'da kuruldu ve haritadan SAPTI:** harita onu `Game.Services` üzerinden
> bağlıyordu, ama `Game.Services` (Nakama/save/telemetri) henüz yok ve TASK-002 kapsamı dışında.
> Ekran bugün paketlere DOĞRUDAN bağlanıyor. `Game.Services` geldiğinde köprü oraya taşınır.

Kural: sunum katmanı sim durumunu OKUR, asla doğrudan yazmaz — durum değişikliği yalnız Command Bus (Tek Kapı).
