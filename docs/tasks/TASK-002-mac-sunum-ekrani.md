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
- `Game.Match` asmdef'i + tek sahne: canlı maç sunumu (portre, dikey saha — FAZ 00.5 kararı).
- Canlı **üç sonuçlu kazanma şeridi** (G/B/M): `MatchEngine.AnlikOlasilik(in MatchState)`.
- Skor + saat + faz; spiker akışı (`EventCount` / `GetEvent(i)`).
- **Müdahale:** taktik dört kadran (mentalite/tempo/pres/hat), −2..+2.
- Hız kontrolü: 1x / 2x / atla.
- Placeholder art (renkli şekiller yeter).

**Out (dokunma):**
- Gerçek art, ses, haptic, FTUE, IAP, analytics → **5G-b**.
- Maç günü döngüsü (hafta hazırlığı, tycoon, röportaj) → **S3**. Bu ekran yalnız MAÇ.
- Replay/özet yolu → D-B kararıyla dilim DIŞINDA (yalnız canlı yol).
- `TheBadge.World` / `GameState` → bu ekran için GEREKMİYOR; maç motoru kendi durumunu tutar.
  World S3'te devreye girer.
- Greybox'ın `MatchModel`/`ModelMatchDirector` kodu → **taşınmaz**. Greybox emekli
  (`docs/GREYBOX_3G_RAPOR.md`); `EngineDev.unity` motor testi için kalabilir.

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
| Reddedilen komut | `eng.RejectedCommands`, `eng.TacticChanges` |

**`MatchSummaryPacket.WinProb3*` dizilerini CANLI OKUMA.** Onlar maç sonu inceleme eğrisidir
(dakika başı, o tick'in müdahaleleri uygulanmadan önce) ve maç bitmeden zaten dolmaz.
Canlı yol `AnlikOlasilik`tır — inceleme turunda bir P1 bulgusu tam olarak buydu.

## Kurallar (ihlali review reddi)

1. **TEK KAPI.** Taktik değişikliği doğrudan `CommandQueue.Enqueue` ile YAZILMAZ. Gerçek
   `CommandBus.Submit` kullanılır (`squad.set_team_tactic`, Tier 0, Context.Match) → kabul
   edilirse motorun kuyruğuna düşer. Bus artık Unity'de var; greybox'ın "hafif bus"u geçersiz.
   Reddedilen komut kullanıcıya SEBEBİYLE gösterilir (CB 11.1) — sessizce yutulmaz.
2. **Sunum katmanı durumu OKUR, yazmaz.** `MatchState`e doğrudan atama yapan UI kodu reddedilir.
3. **Magic number yok.** Ekran ayarları (kare başı tick, şerit animasyon süresi, feed uzunluğu)
   tek bir yerde toplanır ve `[KALİBRE]` adayı olarak işaretlenir.
4. **C# 9 / netstandard2.1** sınırı paylaşılan paketler için geçerli; Unity tarafı kodu bu
   sınırda olmak zorunda değil ama paketlere sızma olmamalı.
5. **Determinizm:** ekran kodu maç sonucunu ETKİLEMEZ. Aynı seed + aynı komutlar = aynı maç.

## Karar maddeleri — ATİLLA'YA SORULACAK (varsayım üretme)

- **K1. Sunum ritmi.** Greybox 8-12 blokluydu ve oyuncu bloklar arası müdahale ediyordu; gerçek
  motor 90 dakika SÜREKLİ. (a) sürekli izleme, oyuncu istediği an müdahale eder; (b) motor
  sürekli koşar ama sunum kritik anlarda durur/vurgular. **Bu, redesign'ın merkezi sorusudur** —
  %40'ın nedeni bilinmediği için ikisi arasında ölçümle seçim yapılamıyor.
- **K2. UI teknolojisi.** Greybox kodla üretilen uGUI kullandı ("UI Toolkit seti FAZ 02'de").
  5G-a placeholder olduğu için uGUI devam mı, yoksa UI Toolkit'e şimdi mi geçilsin?
- **K3. Greybox sahnesinin akıbeti.** `Greybox.unity` emekli; silinsin mi, arşiv olarak
  kalsın mı? (`EngineDev.unity` motor testi için kalmalı.)

## Acceptance criteria

- Unity konsolu temiz; proje derleniyor (Adım 0 raporlandı).
- Tek maç baştan sona izlenebiliyor; şerit maç boyunca oynuyor.
- **Taktik değişikliği şeridi ANINDA oynatıyor** (aynı tick) — motor tarafında
  `S2AnlikOlasilikCanli` bunu zaten ölçüyor; ekranda GÖRÜNÜR olmalı.
- Reddedilen komut sebebiyle gösteriliyor.
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
