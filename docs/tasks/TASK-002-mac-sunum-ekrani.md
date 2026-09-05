# Task Brief 002: Maç Sunum Ekranı (5G-a / S2)

> Bu brif Unity tarafında koşacak Claude Code oturumu içindir (Unity MCP köprüsüyle).
> Bağlayıcı üst belge: `docs/briefs/BRIEF_5G_DIKEY_DILIM.md`.

## Objective

5G-a'nın çıkış kapısını koşulabilir hâle getirmek: **gerçek motorun üstünde**, placeholder
art'la, canlı maç sunumu. Kapının kendisi bu ekran DEĞİL — 3-5 kişilik **mülakatlı gözlem
turu**; ekran o turun aracıdır.

Fun Gate %40 ile NO-GO kapandı ve **kopuş nedeni ölçülmedi**. Bu turun iki çıktısı var:
"bir maç daha" sinyali **ve kopuş nedeninin yazılı olması**. Kayıtlı kural: *mülakatsız
playtest koşulmaz.*

## Önkoşul — ÖNCE BUNU DOĞRULA (Adım 0)

`main` üç paylaşılan paketi Unity paketi olarak bağladı (ADR-002): `com.thebadge.sim`,
`com.thebadge.commandbus`, `com.thebadge.world`. **Bunların Unity'de gerçekten derlendiği
HENÜZ KANITLANMADI** — S1'i yazan ortamda Unity yok, kapı yalnız yapısal koşulları ölçüyor
(`S1UnityPaketSiniri`).

Projeyi aç ve konsolu kontrol et. Beklenen: hata/uyarı yok, üç paket Packages altında görünüyor.
Patlarsa muhtemel sebepler ve ilk bakılacak yerler:
- **CS0579 yinelenen öznitelik** → paket klasöründe `obj/`/`bin/` kalmış. Üç pakette
  `Directory.Build.props` çıktıyı repo kökündeki `artifacts/`e yönlendiriyor; o dosyalar
  silinmiş olabilir.
- **C# sürüm hatası** → paketler netstandard2.1 / C# 9. Yeni kod da bu sınırda kalmalı.
- **asmdef referansı bulunamadı** → `TheBadge.World` → `TheBadge.Sim` + `TheBadge.CommandBus`
  zincirini kontrol et.

**Bu adım DoD-G'nin ilk maddesidir ve raporlanmadan ilerlenmez.** Sorun çıkarsa düzeltmeyi
`shared/` tarafında yap ve `dotnet run --project shared/TheBadge.Sim.Checks -c Release` ile
doğrula — 179 kapı yeşil kalmalı.

## Scope

**In:**
- `Game.Match` asmdef'i + tek sahne: canlı maç sunumu (portre, dikey saha — FAZ 00.5 kararı),
  **UI Toolkit** ile (K2 kararı).
- Canlı **üç sonuçlu kazanma şeridi** (G/B/M): `MatchEngine.AnlikOlasilik(in MatchState)`.
- **Kritik an duraklaması** (K1 kararı): `KritikAnDedektoru` ateşlediğinde sunum durur/vurgular.
- Skor + saat + faz; spiker akışı (`EventCount` / `GetEvent(i)`).
- **Müdahale:** taktik dört kadran (mentalite/tempo/pres/hat), −2..+2 — **gerçek Command Bus
  zinciriyle** (`World` köprüsü dahil, Kural 1).
- Hız kontrolü: 1x / 2x / atla.
- Placeholder art (renkli şekiller yeter).

**Out (dokunma):**
- Gerçek art, ses, haptic, FTUE, IAP, analytics → **5G-b**.
- Maç günü döngüsü (hafta hazırlığı, tycoon, röportaj) → **S3**. Bu ekran yalnız MAÇ.
- Replay/özet yolu → D-B kararıyla dilim DIŞINDA (yalnız canlı yol).
- Maç günü döngüsünün DÜNYASI (ekonomi, transfer, takvim, kadro ekranları) → **S3**. `World`
  paketi bu ekranda YALNIZ komut köprüsü için kullanılır (aşağıya bak), oyun durumu ekranı için değil.
- Greybox'ın `MatchModel`/`ModelMatchDirector` kodu → **taşınmaz**. Greybox emekli
  (`docs/GREYBOX_3G_RAPOR.md`).

### Greybox arşivi (K3) — ÖNCE EngineDev'i ÇIKAR

`Assets/Greybox/` bugün hem emekli greybox'ı hem de **hâlâ gereken motor test sahnesini**
(`Scenes/EngineDev.unity` + `Scripts/EngineDev/EngineDevBootstrap.cs`) barındırıyor. Klasörü
olduğu gibi arşivlemek motor test sahnesini de götürür.

**TAŞINACAK TAM LİSTE** (zincir sonuna kadar sürüldü — inceleme bulgusu, Codex P1: ilk yazımda
yalnız sahne + bootstrap diyordum ve bu, önlemeye çalıştığım şeyi yapardı):

| Dosya | Neden |
| --- | --- |
| `Scenes/EngineDev.unity` (+ `.meta`) | motor test sahnesi |
| `Scripts/EngineDev/EngineDevBootstrap.cs` (+ `.meta`) | sahnenin kurucusu |
| `Scripts/View/SpriteFactory.cs` (+ `.meta`) | **bootstrap bunu ÇAĞIRIYOR** (`using TheBadge.Greybox.View`; `NewSprite`/`Circle`/`Solid`). Geride kalırsa yeni asmdef onu çözemez, konsol derleme hatası verir ve sahne koşmaz. |

**Zincir burada BİTİYOR (doğrulandı):** `SpriteFactory` yalnız `UnityEngine` kullanıyor.
`EngineDevBootstrap`ın diğer `using`leri `System.IO` + `TheBadge.Sim.*` + `UnityEngine`.
Balance dosyasını **repo kökünden** okuyor (`Application.dataPath/../../../balance/sim.balance.json`),
`Greybox/Resources/greybox.balance.json`dan DEĞİL — o dosya arşivle kalabilir.

Sıra:
1. Yukarıdaki üç dosya (+ `.meta`ları) **kendi klasörüne taşınır** (ör. `Assets/EngineDev/`,
   kendi asmdef'iyle, referans `TheBadge.Sim`). Namespace'ler `TheBadge.Greybox.*` kalabilir —
   derlemeyi etkilemez; istenirse ayrı bir adımda düzeltilir.
2. Kalan greybox `Assets/Greybox~/` olarak yeniden adlandırılır. Unity `~` ile biten klasörleri
   içe aktarmaz: **dosyalar git'te kalır, derlenmez, bakım yükü olmaz.**
3. **Bilinen bedel (kabul edilmiş):** dört EditMode test dosyası (`FlowSimTests`,
   `ModelMatchTests`, `EconomyAndBusTests`, `SahneSozlesmesiTests`) koşmayı bırakır. Hepsi
   greybox'ın KENDİ koduna bakıyor (emekli `MatchModel`, `GreyboxCommandBus`, `TycoonEconomy`) —
   paylaşılan çekirdeği ölçen tek satır yok. Bu bir kapı gevşetmesi değil, ölçtüğü şey emekli.
4. Neden bakım yükü: `Game.Greybox` asmdef'i `TheBadge.Sim`e referans veriyor. Arşivlenmezse
   çekirdek API'si her değiştiğinde emekli kod kırılır ve birinin onu düzeltmesi gerekir.

## Context to read first

`CLAUDE.md` → `docs/DECISIONS.md` (5G bölümleri: D-A/D-B/D-C kararları, S2 bulguları) →
`docs/briefs/BRIEF_5G_DIKEY_DILIM.md` → `unity/UNITY_SETUP.md` (paket tablosu + asmdef haritası)
→ `docs/MatchEngine_Spec_v1_0.md` §14 (maç içi komutlar) → `docs/CommandBus_Spec_v1_0.md` §5 (4 kapı).

## Kullanılacak yüzey (hepsi `main`'de, kapılı)

| Ne | Nasıl |
| --- | --- |
| Maçı adımla | `eng.Tick(ref st)` — kare başına N tick (hız kontrolü buradan) |
| **Canlı şerit** | `eng.AnlikOlasilik(in st)` → `.Ev` / `.Beraberlik` / `.Deplasman`, toplamı 1 |
| Skor / saat / faz | `st.HomeGoals`, `st.AwayGoals`, `st.Tick` (600 tick = 1 dk), `st.Phase` |
| Olay akışı | `eng.EventCount`, `eng.GetEvent(i)` |
| Maç sonu paketi | `eng.BuildSummary(in st)` — istatistik ekranlarının TEK kaynağı |
| **Komut gönder** | `bus.Submit(env, payload, executor, receivedAtUnixMs, userId)` → `CommandOutcome` |
| **Red sebebi (bus)** | `outcome.Reason` (`RejectionReason`) + `outcome.Detail` — kullanıcıya BU gösterilir |
| Red sayacı (motor) | `eng.RejectedCommands` — yalnız SAYI, sebep taşımaz; aşağıdaki nota bak |
| Uygulanan taktik | `eng.TacticChanges` |
| **Kritik an** | `KritikAnDedektoru.Kontrol(in olasilik, bal.canliOlasilik.kritikAnEsigi, out sicrama)` |

### Kritik an duraklaması — mekanizma hazır, YENİDEN YAZMA

K1 kararının karşılığı `main`'de: `TheBadge.Sim/src/Match/KritikAn.cs`. Kullanımı:
maç başında `det.Sifirla(eng.AnlikOlasilik(in st))`, sonra her örneklemede `det.Kontrol(...)`
— `true` dönerse duraklama anı; `sicrama` o anın büyüklüğüdür (vurgunun şiddeti buna bağlanabilir).

**ME 15.3'ün `highlight.esik`ini bu iş için KULLANMA.** O ölçüt maç başına 0,5-0,8 işaret verir
ve **maçların yarısını boş bırakır**; sıfır duraklamalı bir maç ritim değildir. Kullanılan ölçüt
kazanma olasılığının SIÇRAMASI — duraklama tam da sonucun maddi olarak kaydığı anda olur.

Ölçülmüş davranış (`S2KritikAnRitmi` her koşuda doğruluyor):
- eşik 0,04 → **maç başına 9,9 duraklama**, greybox'ın 8-12 blok ritmine oturuyor
- **hiçbir maç boş değil** (%0)
- **kadanstan bağımsız**: 1 sn ↔ 30 sn arası 30 kat aralıkta sayı 10,0 ↔ 9,5. Yani kare hızını
  serbestçe seç; taban yalnız ateşlendiğinde sıfırlandığı için sık örnekleme aynı sıçramayı daha
  ERKEN yakalar, daha ÇOK değil.

**`MatchSummaryPacket.WinProb3*` dizilerini CANLI OKUMA.** Onlar maç sonu inceleme eğrisidir
(dakika başı, o tick'in müdahaleleri uygulanmadan önce) ve maç bitmeden zaten dolmaz.
Canlı yol `AnlikOlasilik`tır — inceleme turunda bir P1 bulgusu tam olarak buydu.

## Kurallar (ihlali review reddi)

1. **TEK KAPI — ve kablolaması şöyle** (inceleme bulgusu, Codex P1: ilk yazımda "gerçek bus
   kullan" derken `World`ü kapsam dışına atmıştım; ikisi aynı anda mümkün değildi).
   Taktik değişikliği doğrudan `CommandQueue.Enqueue` ile YAZILMAZ. Zincir:

   ```
   UI → CommandBus.Submit(zarf, payload, WorldExecutor, saat, userId)
        → 4 kapı → SquadActions.TaktikHandler → IMatchCommandSink → motorun CommandQueue'su
   ```

   `Submit` **`ICommandExecutor` ZORUNLU ister** (null verirsen `ArgumentNullException` — bu
   bilinçli: yürütücüsüz çağrı durumu değiştirmeden "başarılı" der). `squad.set_team_tactic`i
   işleyen tek uygulama `TheBadge.World`teki `WorldExecutor` + `SquadActions.Baglan(...)`.
   **Bu köprüyü YENİDEN YAZMA** — üç paket de Unity'de mevcut.

   Referans kablolama: `shared/TheBadge.Sim.Checks/WorldHarness.cs` (satır ~290: `WorldStore` →
   `WorldContext` → `WorldExecutor` → `*Actions.Baglan` → `new CommandBus(...)`) ve
   `SpyMatchSink` (aynı dosyada, `IMatchCommandSink`in en küçük uygulaması). Ekranın sink'i
   `MatchCommand`ı motorun `CommandQueue`suna iletir.

2. **İKİ AYRI RED YOLU VAR, İKİSİ DE GÖSTERİLİR** (Codex P1).
   - **Bus reddi:** komut motora HİÇ ULAŞMAZ, `eng.RejectedCommands` DEĞİŞMEZ. Sebep
     `CommandOutcome.Reason` + `.Detail`tedir. Kullanıcıya gösterilecek olan budur.
   - **Motor geç reddi:** bus kabul etti ama uygulama anında düştü (bant dışı delta, değişiklik
     hakkı bitti, motivasyon 10 dk beklemesi). Bu yalnız `eng.RejectedCommands` sayacını artırır
     ve **sebep taşımaz** — sayacın arttığını görmek yeter, sebebi bugün yok.

   Hiçbir red sessizce yutulmaz (CB 11.1).
3. **Sunum katmanı durumu OKUR, yazmaz.** `MatchState`e doğrudan atama yapan UI kodu reddedilir.
4. **Magic number yok.** Ekran ayarları (kare başı tick, şerit animasyon süresi, feed uzunluğu)
   tek bir yerde toplanır ve `[KALİBRE]` adayı olarak işaretlenir.
5. **C# 9 / netstandard2.1** sınırı paylaşılan paketler için geçerli; Unity tarafı kodu bu
   sınırda olmak zorunda değil ama paketlere sızma olmamalı.
6. **Determinizm:** ekran kodu maç sonucunu ETKİLEMEZ. Aynı seed + aynı komutlar = aynı maç.

## Karar maddeleri — ✅ ÜÇÜ DE KAPANDI (2026-09-05)

**Bu bölümde sorulacak bir şey KALMADI; kayıt olarak duruyor.** Başlığı "Atilla'ya sorulacak"
diye okuyup durma — üç maddenin üçü de karara bağlandı ve karşılıkları brifin gövdesine işlendi.
Yeni bir belirsizlik çıkarsa CLAUDE.md'nin kuralı zaten geçerli (varsayım üretme, seçenekleri
artı/eksileriyle sun, karar iste) — ama o, bu listeye ait değil.

- ~~**K1. Sunum ritmi.**~~ → **KAPANDI (2026-09-05, Atilla): (b)** — motor sürekli koşar,
  **sunum kritik anlarda durur/vurgular.** Mekanizması ölçüldü ve `main`'e girdi, aşağıya bak.
- ~~**K2. UI teknolojisi.**~~ → **KAPANDI (2026-09-05, Atilla): UI Toolkit.**
  `com.unity.modules.uielements` zaten manifest'te (1.0.0) — **yeni bağımlılık değil, ADR
  gerekmiyor.** Placeholder için UXML/USS dosyası şart değil, arayüz C#'ta kurulabilir.
  Bu, FAZ 00.5'in "UI Toolkit seti FAZ 02'de" satırını ÖNE ÇEKİYOR; greybox'ın uGUI kodu
  (`UiShell.cs`, `UiWidgets.cs`) zaten taşınmıyordu, çelişki yok.
- ~~**K3. Greybox sahnesinin akıbeti.**~~ → **KAPANDI (2026-09-05, Atilla): arşiv olarak kalsın.**
  Uygulaması aşağıda — **sırası önemli, yoksa motor test sahnesi sessizce ölür.**

## Acceptance criteria

- Unity konsolu temiz; proje derleniyor (Adım 0 raporlandı).
- Tek maç baştan sona izlenebiliyor; şerit maç boyunca oynuyor.
- **Duraklama ritmi hissediliyor:** bir maçta duraklama sayısı raporlanır ve 8-12 civarındadır
  (motor tarafı `S2KritikAnRitmi` ile zaten korunuyor; ekranda GÖRÜNÜR olmalı).
- **Taktik değişikliği şeridi ANINDA oynatıyor** (aynı tick) — motor tarafında
  `S2AnlikOlasilikCanli` bunu zaten ölçüyor; ekranda GÖRÜNÜR olmalı.
- **Bus reddi sebebiyle gösteriliyor** (`CommandOutcome.Detail`); motor geç reddi en az
  sayaç olarak görünüyor. Bant dışı bir delta ile ikisi de elle denenip raporlanır.
- Aynı seed + aynı müdahaleler = aynı skor (elle doğrula, raporla).
- `dotnet run --project shared/TheBadge.Sim.Checks -c Release` yeşil (179 kapı).

## Verification required (DoD-G)

1. Unity konsolu temiz.
2. EditMode/PlayMode testleri yeşil; sunum katmanının okuma sözleşmesi testli.
3. **Kısa oynanış kaydı** — şeridin taktik müdahalesinde oynadığı an görünmeli.
4. Hedef cihazda değil, editörde yeter (cihaz ölçümü 5G-b/S6).
5. Varsayımlar ve kalan riskler raporu.

## Sonraki adım (bu brifin DIŞINDA)

Ekran koşar hâle gelince **mülakatlı gözlem turu**: 3-5 kişi, kişi başı ≥15 dk serbest oynama,
yönlendirme yok. `docs/PLAYTEST_3G.md` biçimi kullanılır ama **mini mülakat tablosu ve telemetri
BU SEFER DOLDURULUR** — geçen turda doldurulmadığı için kopuş nedeni bilinmiyor ve bütün 5G-a
o eksiği kapatmak için var.
