# ADR-002: Unity paket sınırı — `World` ve `CommandBus` istemciye açılır
**Durum:** Accepted (Atilla onayı, 2026-09-04 — "a ve c için önerdiğin şekilde devam et") · **Tarih:** 2026-09-04

**Bağlam:** 5G Dikey Dilim açılış brifi (`docs/briefs/BRIEF_5G_DIKEY_DILIM.md`, D-C) ölçümle şunu
buldu: `unity/TheBadge/Packages/manifest.json`ın proje-dışı tek yerel paketi `com.thebadge.sim`;
`TheBadge.World` ve `TheBadge.CommandBus`ta ne `package.json` ne `.asmdef` vardı. Yani FAZ 04'ün
tamamı — Tek Kapı, 32 aksiyon, kadro, ekonomi, transfer — **istemciden erişilemez** durumdaydı.
Bu, K11'in dersinin bir üst seviyedeki hâliydi: orada iki alt sistem arasındaki dikiş
ölçülmemişti; burada dikiş için gereken paket sınırı hiç yoktu.

Anayasa 4G.9 yeni Unity paketini ADR'ye bağlıyor; bu kayıt o gereğin karşılığıdır.

**Karar:** `TheBadge.World` ve `TheBadge.CommandBus`, `TheBadge.Sim` ile **aynı desende** Unity
yerel paketi olur: `package.json` + kök `.asmdef` (`noEngineReferences: true`) + Unity
manifest'inde `file:` girdisi. Üç paket de netstandard2.1 / C# 9 / dış paket referansı YOK
kalır (CLAUDE.md değişmez #3 ve #5).

**Gerekçe (elenen seçeneklerle birlikte):**
- **(b) İnce cephe paketi (yalnız DTO + katalog önbelleği)** elendi: doğrulamayı sunucuya kaydırır.
  CB Spec'in kendi mimarisi "istemci ön-doğrular, **sunucu yeniden doğrular**" der; cephe deseni
  ön-doğrulamayı imkânsız kılar, offline kuyruğu ve anlık UI geri bildirimini zayıflatır.
- **(c) Sunucu-only + RPC DTO'ları** elendi: G3 otoritesi zaten sunucuda ama **Nakama bağlaması
  YOK** (FAZ 04 kapanış brifi bölüm 4 borcu). Bugün çalışacak bir yol değil.
- (a) seçildi çünkü tek kaynak değişmezini (CLAUDE.md #3) istemciye kadar taşıyan tek seçenek o.

**Sonuçlar:**
- Üç paketin Unity uyumluluğu artık **sürekli korunmalı**: `S1UnityPaketSiniri` kapısı kimlik
  (package.json ↔ manifest ↔ asmdef), bağımsızlık (`noEngineReferences`, kaynakta UnityEngine izi
  yok, csproj'da `PackageReference` yok), **asmdef ↔ csproj grafiğinin birebirliği**, netstandard2.1
  / C# 9 ve klasör disiplinini her koşuda ölçüyor.
- **Klasör tuzağı kapatıldı:** Unity paket klasöründeki TÜM `.cs`'i derler; `dotnet build`in
  ürettiği `obj/**/*.AssemblyInfo.cs` orada kalırsa Unity CS0579 ile düşerdi — üstelik yalnız
  biri `dotnet build` koştuktan SONRA. Üç pakete `Directory.Build.props` konup çıktı repo
  kökündeki `artifacts/`e yönlendirildi; kapı `src/` dışında `.cs` KALMADIĞINI doğruluyor.
  (Bu tuzak `TheBadge.Sim` için bugün de vardı ve henüz patlamamıştı.)
- `unity/UNITY_SETUP.md`'nin asmdef haritası güncellendi: `Game.Commands` artık `TheBadge.Sim`e
  değil `TheBadge.CommandBus`a bağlı (harita FAZ 04'ten eskiydi).
- **Sınır:** bu kayıt paketlerin Unity'de GERÇEKTEN derlendiğini kanıtlamaz — bu ortamda Unity
  yok (CLAUDE.md: kanıtlanamayan kod eklenmez). Kapı, derlemenin sessizce bozulabileceği yapısal
  koşulları ölçer; Editor'de ilk derleme Atilla'nın DoD-G kanıtıdır (brif bölüm 7).
