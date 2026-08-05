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

## Atilla'nın sıradaki adımları

`unity/UNITY_SETUP.md` runbook'u: projeyi aç → konsol/testler → Editor'de oyna → iPhone build → 3-5 oyuncu playtest → `docs/PLAYTEST_3G.md` doldur → kapı kararı (GO/NO-GO) → DECISIONS.md.
