# Unity 6 Proje Kurulumu

> **Durum (FAZ 00.5):** `unity/TheBadge` proje iskeleti repo'da HAZIR (Claude Code üretti).
> Unity Hub'dan yeni proje AÇMAYA gerek yok; aşağıdaki "Greybox Çalıştırma" adımlarını izle.

## Greybox Çalıştırma (Atilla runbook'u — FAZ 00.5)

1. **Unity sürümü:** Unity Hub → Installs → **Unity 6 LTS (6000.0.x)** kurulu olsun (iOS Build Support modülüyle).
2. **Projeyi aç:** Hub → Add → `unity/TheBadge` klasörünü seç → aç.
   - Pinli sürüm `6000.0.58f1`; sendeki 6000.0.x farklıysa Unity sürüm yükseltme/onay diyaloğu gösterir → **onayla**. İlk açılış paket çözümleme + ProjectSettings migrasyonu nedeniyle birkaç dakika sürebilir.
   - İlk açılışta oluşan `Packages/packages-lock.json` ve ProjectSettings'te Unity'nin tamamladığı alanları **commit et** (tek seferlik).
3. **Doğrulama:** Project panelinde `Packages → The Badge Sim Core` görünmeli (`com.thebadge.sim` local package). Console'da 0 error / 0 warning hedef.
4. **Oyna:** `Assets/Greybox/Scenes/Greybox.unity` sahnesini aç → Play. Game görünümünü **9:16 Portrait** yap (Game penceresi üstündeki aspect menüsü).
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

## Assets asmdef Haritası (FAZ 01'de kurulacak)

| asmdef | İçerik | Referanslar |
| --- | --- | --- |
| Game.Commands | Command Bus istemci ucu, katalog önbelleği | TheBadge.Sim |
| Game.Services | Nakama istemcisi, save/load, telemetri | Game.Commands |
| Game.UI | UI Toolkit ekranları, Rive köprüleri | Game.Services |
| Game.Match | Maç sunum katmanı (izleme/replay oynatıcı) | TheBadge.Sim, Game.Services |
| Tests.EditMode / Tests.PlayMode | Unity testleri | ilgili modüller |

> FAZ 00.5'te bilinçli sapma: tek `Game.Greybox` asmdef'i (+ `Game.Greybox.EditModeTests`) kullanılır;
> greybox Fun Gate sonrası atılacağı için beş modüllü harita FAZ 01'e ertelendi.

Kural: sunum katmanı sim durumunu OKUR, asla doğrudan yazmaz — durum değişikliği yalnız Command Bus (Tek Kapı).
