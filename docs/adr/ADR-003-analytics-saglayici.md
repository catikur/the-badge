# ADR-003: Analytics sağlayıcı — TelemetryDeck, SDK'sız HTTP gönderici (GDD'deki Firebase'den sapma)
**Durum:** Accepted (Atilla onayı, 2026-10-05 — "D-D ve D-E önerdiğin gibi") · **Tarih:** 2026-10-05

**Bağlam:** 5G Dikey Dilim brifi (`docs/briefs/BRIEF_5G_DIKEY_DILIM.md`, D-E) iki kaynağın
çeliştiğini kaydetti: GDD 9.5/17 **Firebase Analytics** der; Anayasa'nın privacy tabanı
**TelemetryDeck + MetricKit** der ve yeni bağımlılık ADR ister (4G.9). Karar 5G-b'den önce
gerekiyor, çünkü Vertical Slice kapısının bir maddesi *"Analytics düşüyor (FTUE hunisi dahil,
event sözlüğüne uygun)"* (brif bölüm 6).

Bugünkü durum: olay sözlüğü tek yerde, `unity/TheBadge/Assets/Match/MacTelemetri.cs`; yazıcı
`unity/TheBadge/Assets/Services/TelemetryLog.cs` (Game.Services asmdef, saf C#). Olaylar
yalnız cihazdaki JSONL dosyasına yazılır, sunucuya gönderim yok (TASK-003, playtest için).

**Karar:**
1. Sağlayıcı **TelemetryDeck**.
2. Entegrasyon **SDK ile değil, doğrudan HTTP ile**: Game.Services'e küçük bir gönderici eklenir,
   `MacTelemetri`nin AYNI olay sözlüğünü TelemetryDeck'in ingest API'sine yollar. Yerel JSONL
   playtest'ler için yerinde kalır — iki hedef, tek sözlük.
3. **MetricKit lansman sonrasına** ertelenir.
4. GDD'deki Firebase satırı bu ADR ile **sapma** olarak kayda geçer; GDD değiştirilmez
   (CLAUDE.md: spec'ler doğrudan değiştirilmez).

**Gerekçe (elenen seçeneklerle birlikte):**
- **Firebase Analytics** elendi: anayasa varsayılanı değil; ek bir Google SDK'sı getirir (native
  bağımlılık, build boyutu, App Store gizlilik etiketleri). Dilimin "privacy varsayılan" tabanına
  ters düşer.
- **TelemetryDeck Unity SDK'sı** elendi: 2026-10-04 araştırmasında deposu çok az geliştirilmiş
  görünüyordu (11 commit) ve `jillejr.newtonsoft.json-for-unity` bağımlılığı istiyordu, yani
  Game.Services'e dış bir JSON paketi girerdi. HTTP gönderici tek dosya ve dış bağımlılıksız; olay
  sözlüğü zaten bizde.
- **MetricKit şimdi** elendi: dilimde gerçek kullanıcı yok. Aynı araştırmaya göre metrik yükü
  günde en fazla bir kez gelir (tanı yükü iOS 15+'ta anında); değeri canlı oyunda ortaya çıkar ve
  native bir eklenti ister.

**Kurallar (gönderici yazılırken bağlayıcı):**
- **PII yok.** Kullanıcı kimliği rastgele bir kurulum kimliğidir (TASK-003 kural 1'in devamı).
- **Telemetri oynanışı etkilemez:** gönderim hatası oyunu düşürmez, ama sessizce de yutulmaz —
  bant dışı bir işaret bırakır (TASK-003 kural 3'ün devamı).
- **Olay adları tek yerde** (`MacTelemetri`): gönderici sözlük tanımlamaz, aktarır.
- **Uygulama kimliği koda gömülmez;** yapılandırmada durur.
- Gönderim **çevrim dışı kuyruklu ve toplu**; ağ çağrısı maç döngüsünü bloklamaz.

**Sonuçlar:**
- Gönderici işi başladığında Atilla bir TelemetryDeck hesabı ve uygulaması açıp uygulama kimliğini
  verir; fiyatlandırma o gün birlikte kontrol edilir.
- 5G-b'nin "analytics düşüyor" kanıtı bu göndericiyle ölçülür; FTUE hunisi olayları sözlüğe
  eklenir.
- CLAUDE.md'nin LLM maliyet kuralı ("tüketim telemetriye event düşer") aynı hattı kullanır.
- **Sınır:** bu kayıt bir karar ve kurallar kaydıdır, entegrasyonu yapmaz. Kod 5G-b'de gelir;
  gerçek bir olayın TelemetryDeck'e düştüğünün kanıtı cihazda/Editor'de alınır (bu ortamda Unity
  yok).
