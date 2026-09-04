# Unity 6 Proje Kurulumu

> **Durum (FAZ 00.5):** `unity/TheBadge` proje iskeleti repo'da HAZIR (Claude Code üretti).
> Unity Hub'dan yeni proje AÇMAYA gerek yok; aşağıdaki "Greybox Çalıştırma" adımlarını izle.

## Greybox Çalıştırma (Atilla runbook'u — FAZ 00.5)

1. **Unity sürümü:** Unity Hub → Installs → **Unity 6 LTS (6000.3.x, Apple Silicon)** kurulu olsun (iOS Build Support modülüyle). 6000.5 gibi LTS-dışı akımlarla AÇMA — proje tek yönlü yükselir, LTS'e dönüş desteklenmez.
2. **Projeyi aç:** Hub → Add → `unity/TheBadge` klasörünü seç → aç.
   - Pinli sürüm `6000.3.21f1`; sendeki 6000.3.x patch'i farklıysa Unity sürüm onay diyaloğu gösterir → **onayla**. İlk açılış paket çözümleme + ProjectSettings migrasyonu nedeniyle birkaç dakika sürebilir.
   - İlk açılışta oluşan `Packages/packages-lock.json` ve ProjectSettings'te Unity'nin tamamladığı alanları **commit et** (tek seferlik).
3. **Doğrulama:** Project panelinde `Packages → The Badge Sim Core` görünmeli (`com.thebadge.sim` local package). Console'da 0 error / 0 warning hedef.
4. **Oyna:** `Assets/Greybox/Scenes/Greybox.unity` sahnesini aç → Play. Game görünümü PORTRE olmalı:
   Game penceresi üst barındaki çözünürlük menüsü → **+** → Type: *Fixed Resolution*, W:1080 H:1920, ad "Portre 1080x1920" → onu seç. (16:9 yatayda UI bilerek taşar — oyun portre kilitli.)
5. **EditMode testleri:** Window → General → Test Runner → EditMode → Run All. Hepsi yeşil olmalı (FlowSim pacing + ekonomi + command bus).
6. **Cihaz build'i (iPhone):** File → Build Settings → iOS → Build; Xcode projesini imzala, cihaza yükle. Orientation Portrait olarak ayarlı.
7. **Ekran kaydı (DoD-G):** 30-60 sn — bir maçın başı, bir gol vurgusu, skip kullanımı, maç sonu + bilet slider'ı + "Sonraki Maç" turu.
8. **Telemetri:** loglar `Application.persistentDataPath/telemetry/telemetry_<oturum>.jsonl`.
   - macOS Editor: `~/Library/Application Support/TheBadge/The Badge Greybox/telemetry/`
   - iOS: Ayarlar gerekmez; Files → On My iPhone → The Badge Greybox (ya da Xcode → Devices → Download Container).
   - Playtest sonrası dosyaları `docs/samples/playtest_<oyuncu>.jsonl` adıyla repoya kopyala; `docs/PLAYTEST_3G.md`'yi doldur.
9. **Save sıfırlama (testçiler arası):** persistentDataPath içindeki `greybox_save.json` dosyasını sil.

Sorun giderme:
- "greybox.balance.json bulunamadı" hatası → `Assets/Greybox/Resources/greybox.balance.json` yerinde mi bak; Reimport All dene.
- Paket çözümleme hatası → Hub'daki Unity sürümünde iOS modülü ve internet olduğundan emin ol; `com.unity.ugui`/`com.unity.test-framework` editor önbelleğinden gelir.

## Greybox mimari notu (FAZ 00.5)

- `Assets/Greybox/Scripts/Sim/` → **motor bağımsız** akış simülasyonu (UnityEngine'siz; headless derlenip test edilir). ME Spec motoru DEĞİLDİR; his prototipidir.
- `Assets/Greybox/Scripts/Loop/` → durum + hafif Tek Kapı (`GreyboxCommandBus`, gerçek `CommandEnvelope` ile) + maç sürücüsü.
- `Assets/Greybox/Resources/greybox.balance.json` → tüm [KALİBRE-G] his/ekonomi ayarları (config_hash DIŞI; `balance/sim.balance.json`'a karışmaz).
- Sahne neredeyse boş: tek `Bootstrap` objesi her şeyi runtime'da kurar (kamera, saha, UI). Elle sahne düzenlemesi gerekmez.

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
| Game.Match | Maç sunum katmanı (izleme/replay oynatıcı) | TheBadge.Sim, Game.Services |
| Tests.EditMode / Tests.PlayMode | Unity testleri | ilgili modüller |

> Bu harita FAZ 01'de yazılmış ve FAZ 04'ten ESKİYDİ: `Game.Commands`ı `TheBadge.Sim`e bağlıyordu,
> oysa Command Bus ayrı bir pakette. ADR-002 ile düzeltildi.
>
> FAZ 00.5'te bilinçli sapma tek `Game.Greybox` asmdef'iydi; greybox **emekli** (Fun Gate kapandı,
> `docs/GREYBOX_3G_RAPOR.md`), beş modüllü harita 5G Dikey Dilim'de kuruluyor.

Kural: sunum katmanı sim durumunu OKUR, asla doğrudan yazmaz — durum değişikliği yalnız Command Bus (Tek Kapı).
