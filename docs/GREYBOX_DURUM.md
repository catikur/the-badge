# GREYBOX DURUM — FAZ 00.5 derleme dokümanı (son hal)

Tarih: 2026-08-07 (İt.11 sonrası) · Branch: `claude/3g-greybox-task-plan-76qg49` (PR #1, taslak) · Sahip: Atilla · Uygulayıcı: Claude Code
Amaç: 10 iterasyonluk konuşmanın kalıcı özeti — **ne yaptık, neden yaptık, sırada ne var.** Yeni bir oturum
bu dokümanla + `docs/DECISIONS.md` ile bağlamı tam kurar. İterasyon iterasyon ayrıntı: `docs/GREYBOX_3G_RAPOR.md`.

## 1. Ne yapıyoruz ve neden (30 saniyede)

FAZ 00.5 **Greybox Fun Gate**: FAZ 02 (60+ ekran UI) ve FAZ 05 (seri asset) kilidini açmadan önce
oyunun eğlenceli olduğunu 3-5 gerçek oyuncuyla kanıtlamak. Kapı metriği: **"bir maç daha" isteği ≥ %60**
(`docs/PLAYTEST_3G.md`). Sanat yok, gri kutular var; his sayılarının tamamı `greybox.balance.json`'da
[KALİBRE-G] (config_hash DIŞI, Fun Gate sonrası prototiple emekli edilir).

**En kritik karar (2026-08-02, DECISIONS'a işli):** 7 iterasyon 2D maç izlenebilirliği cilaladıktan sonra
Atilla tezi düzeltti — USM 98 DNA'sı "maçı seyretmek" değil, **"karar ver → kazanma ihtimalin görünür
şekilde değişsin → sonucu yaşa"**. RA#1 revize edildi: ana deneyim **Model Maçı**, 2D motor gol anlarında
**highlight vinyeti**ne indirildi. Kapı metriği değişmedi.

## 2. Şu anki deneyim (oyuncunun gördüğü)

1. **Maç öncesi:** 3 taktik kartı (Savunma/Denge/Hücum) + isimli kadro (11+5, bireysel GÜÇ
   puanlarıyla — İt.12) + rakip gücü + son-5 form.
2. **Model Maçı ekranı:** maç 10 aksiyon bloğu. Her blokta ÖNCE olasılık kartı ("Gol ihtimali BİZ %18 ·
   etkenler: güç ×1.12 · yorgunluk ×0.96 …"), SONRA zar. Üstte canlı **G/B/M kazanma şeridi** (kesin DP,
   faz+enerji ileri projeksiyonlu), momentum çubukları, **kalıcı kaydırılabilir spiker feed'i**,
   **canlı istatistik satırı** (xG · Tehlike · Enerji · Hamle · Değişiklik) + "DETAY ▾" kaydırılabilir
   **koç masası** paneli (kart/sakatlık/enerji karşılaştırması, günlükler, 16 kişilik kadro durumu).
3. **Müdahale (Tek Kapı):** taktik/tempo (3 hamle) + **oyuncu değişikliği (3 hak, ayrı havuz)**; her
   komut `CommandEnvelope` ile bus'tan geçer, şerit anında yeniden hesaplanır ("G %38→%45" feed'e düşer).
   **Kart/sakatlık olayları** maça doku katar; bizim sakatlıkta akış DURUR: "değiştir / eksik devam"
   zorunlu karar paneli (İt.11 — tezin vitrini: kararın şeridi görünür oynatması).
4. **Gol vinyeti:** golde 2D sahne kaydı oynar (FlowSim, sahiplik değişmezli) — gol ağlara, kutlama
   kümelenmesi SONUNA KADAR izlenir (acele yok, Atilla kuralı); rakip golünde sahne aynalanır.
5. **Maç sonu:** skor + bilet geliri + prim → bilet fiyat slider'ı (canlı doluluk/gelir önizleme) →
   Sonraki Maç. Mini-save + JSONL telemetri (kapı metrikleri logdan hesaplanır).

## 3. Yapılanlar ve NEDENLERİ (karar günlüğü, sıkıştırılmış)

| # | Ne yapıldı | Neden (tetikleyen) |
|---|---|---|
| Kuruluş | Unity 6 LTS (6000.3.21f1) projesi metinle üretildi: sabit GUID'li .meta'lar, tek Bootstrap sahnesi, kodla uGUI, local package `com.thebadge.sim` | Editor'süz ortamda üretim; Atilla yalnız açıp oynar (`unity/UNITY_SETUP.md` runbook) |
| Doğrulama hattı | Scratchpad harness (300 maç FlowSim + 400 maç model taraması + sahne denetimleri) ve UnityEngine stub derlemesi (2 define yolu) + `Sim.Checks` | Unity Editor yokken "çalışması lazım" yasak — kanıt zorunlu (CLAUDE.md) |
| Input System geçişi | `activeInputHandler=1` + `InputSystemUIInputModule` (`#if ENABLE_INPUT_SYSTEM` korumalı) | 6000.3 LTS eski Input Manager'ı deprecation'a aldı (it.1 sonrası uyarı) |
| İt.1-2: sahneleme cilası | Gerçek alıcıya pas, korner dizilişi, gol ağlarda biter, kutlama kümelenmesi, spiker akışı, titreşim | "Top oyunculardan uzak", "izlemek acı verici" |
| **İt.3: yazı→onay→kod süreci** | `docs/GREYBOX_SAHNELEME.md` yazıldı, Atilla onayladı, kod senaryoya hizalandı; sahneler süreyle değil DİZİLİŞ KOŞULUYLA başlar; sahne sözleşmesi denetimleri harness'a girdi | Atilla süreç kuralı koydu: davranış önce yazıyla sabitlenir — **hâlâ yürürlükte** |
| İt.4: sabit adım + interpolasyon | Sim 0.05 sn sabit adım; sunum kareler arası interpolasyon; pas yavaşlaması; top-ayak yapışması | "2x'te top-oyuncu dinamikleri karışıyor" — kök neden kare başına örneklemeydi |
| İt.5: yükseklik + ışınlama yasağı | Top yükseklik parabolü (büyür/gölgeden ayrılır), korner uzaklaştırması gerçek uçuş, çift ayak ucu yön işareti | "Korner sonrası sahne atlıyor" — top 20 m ışınlanıyordu |
| **İt.6: motor kararı → Sahiplik Değişmezi** | Literatür tarandı (Buckland Simple Soccer vd.), 3 yol sunuldu, Atilla **Yol A**'yı seçti: top asla özerk değil (taşıyıcıda / isimli uçuşta / duran topta / serbest); karar kapısı `CarrierHasBall`; her vuruş mesafesi denetlenir (≤2.4 m) | "Top ortada kalıp kendi kendine pas oluyor; motoru komple gözden geçir, bana yol öner" |
| İt.7: canlı top saati | Maç dakikası yalnız top oyundayken işler; kaleci-taşıyıcı topa gider; 4 sn takılma bekçisi | "Top kalecide takıldı" + "2x'te az pozisyon" — aynı kök neden: akış donarken saat yanıyordu |
| **İt.8: FUN GATE PİVOTU → Model Maçı** | `MatchModel` (10 blok, açık olasılık formülü, kesin DP kazanma şeridi), müdahaleler Tek Kapı'dan, vinyet boru hattı (`VignetteRecorder` headless FlowSim kaydı) | Atilla: "olasılıkları görünür yapıp modelde oynatmalıyız; fun gate'e yanlış yaklaşmışız" — DECISIONS kaydı |
| İt.9: kutlamalı vinyet + 8 etkenli kriter modeli | Vinyet kutlama bitene dek kayıt+oynatma; `docs/GREYBOX_MODEL.md`: tanh güç, 3×3 taktik matrisi, faz eğrisi, momentum (OU), skor durumu, tempo, ev avantajı, form; `Factors()` dökümü blok kartında | "Hiçbir şey için acele etme" + "olasılık kriterlerini iyi modelleyelim" |
| İt.10: rakip vinyeti + kalıcı feed + istatistik | Vinyet güç eğimi hep GOL ATAN tarafa (rakip golü artık bulunuyor); feed kalıcı+kaydırılabilir (hiçbir satır silinmez); xG/Tehlike/Hamle satırı + DETAY paneli (`XgUs/XgThem/DangerUs/DangerThem` — ayrı zar akışı, skor zarına dokunmaz) | "Rakip golleri gösterilmeden dönüyor; anlatım kaybolmasın; istatistik göster, detay expand" |
| **İt.11: "Koçun Eli"** (öneri → onay → kod) | 10 etkenli model (+Yorgunluk, +Eksik); isimli 11+5 kadro; kart/sakatlık olayları (Referee/Injury domain — skor zarı değişmedi); sakatlıkta ZORUNLU karar paneli (akış kilitlenir, skip atlayamaz); `model.substitution`/`model.continue_short` (değişiklik 3 hak, hamleden ayrı); koç masası istatistik paneli (kaydırılabilir); DP'ye faz+enerji projeksiyonu; vinyet dönüşünde feed silinme kalıntısı düzeltildi. Paket B (ön-emirler, Oto-Koç, online/offline ilkesi) GDD v4.2 bekleyen kararı | Atilla: "kart/sakatlık/yorulma yok; değişiklik/mantalite isterim; istatistik her koçun ihtiyacını taşısın; oto-koç fikri" → `GREYBOX_ONERI_IT11.md`, karar: Paket A tam |
| **İt.12: "Kadro Kimliği"** (öneri → onay → kod) | Bireysel oyuncu gücü (ME 6.1 tek-puan vekili; taban normalize → kalibrasyon korundu) + mevki ağırlıklı HÜCUM/SAVUNMA reytingleri; Güç etkeni reyting farkına bağlandı, Yorgunluk/Eksik reytingin içine taşındı (GREYBOX_MODEL v3); **kaleci savunmada en ağır mevki** ("etkisiz kaleci" bitti) + Danger'da isimle kurtarış; yıldız kaybı > vasat kaybı; panellerde güç görünür; gol atfı güç ağırlıklı; DP reyting eğimi projeksiyonu; şerit altı açıklama satırı; kompakt bilet slider'ı | Atilla: "kaleciler aşırı etkisiz; oyuncuların özellikleri olmalı; kazanma olasılığı neye göre; slider çok büyük" → `GREYBOX_ONERI_IT12.md`, karar: S1 |

**Değişmeyen anayasa uyumu:** `shared/TheBadge.Sim`'e dokunulmadı (yalnız `Rng` tüketildi);
tüm rastgelelik sayaç-RNG (`Rng.Hash64`, Domain gerekçeli); durum değiştiren her eylem
`CommandEnvelope`'la bus'tan; docs/ spec'leri değişmedi (öneriler DECISIONS'a yazıldı).

## 4. Doğrulama hattı (her değişiklikte koşulan)

| Kapı | Komut | Son durum (2026-08-07) |
|---|---|---|
| Çekirdek | `dotnet run --project shared/TheBadge.Sim.Checks -c Release` | ✅ YEŞİL |
| Harness (scratchpad, repo dışı) | 300 maç FlowSim pacing + sahne sözleşmesi + ekonomi + bus + 400 maç model (gol 2.77, kalibrasyon %38 vs %41, olay bantları sarı 2.08/kırmızı 0.23/sakatlık 0.64, yorgunluk/taze bacak/karar kilidi/olay determinizmi, kaleci etkisi/yıldız kaybı/kadro üretimi, değişiklik bus 4 negatif, vinyet iki takım) | ✅ TÜM KONTROLLER YEŞİL |
| Stub derleme (2 yol: varsayılan + `ENABLE_INPUT_SYSTEM;UNITY_IOS`) | Unity katmanı sözdizimi/tip kontrolü | ✅ 0 hata / 0 hata |
| EditMode testleri (Editor'de) | pacing, determinizm-lite, sahne sözleşmesi aynası, `ModelMatchTests` (7) | ⏳ Atilla — Editor'de koşulur |

Harness/stub scratchpad'te yaşar (repo'ya girmez); yeni oturumda gerekirse rapordaki tarife göre yeniden kurulur.

## 5. YAPILACAKLAR — GÜNCEL PLAN (2026-08-07 kapanış kararı: playtest → kapı kapanır → FAZ 03)

**İçerik DONDU.** Yeni greybox özelliği yok (DECISIONS kapanış planı); yalnız playtest'i engelleyen
hata düzeltilir. "FM hissi" işi FAZ 03 motoru + Dikey Dilim'e taşındı — ME Spec 6.1 tam nitelik
tablosu (kaleci: Reflexes, Handling, OneOnOne, AerialCommand, Kicking, Throwing) orada devreye girer.


**Atilla (kapıya giden yol):**
1. `git pull` (Unity KAPALIYKEN; makine ProjectSettings'i değiştirdiyse önce `git checkout -- .`).
2. Editor'de oyna — iterasyon 11+12 kontrol listesi: (a) maç öncesi kadroda isim + GÜÇ puanları;
   (b) enerji maç boyu düşüyor, tempo yükseltince daha hızlı mı; (c) sarı/kırmızı/sakatlık feed'e
   düşüyor mu; (d) bizim sakatlıkta karar paneli, değiştir/eksik-devam şeridi oynatıyor mu; (e) OYUNCU
   DEĞİŞ panelinde güç+enerji birlikte — "yorgun yıldız mı taze vasat mı" hissi var mı; (f) Danger'da
   kaleci İSİMLE kurtarıyor mu; (g) DETAY koç masası: Hücum/Savunma reytingleri + kadro güç sütunu;
   (h) şerit altı açıklama satırı; (i) bilet slider'ı kompakt mı; (j) rakip gol vinyeti + feed kalıcı;
   (k) konsol 0 error/0 warning.
3. 30-60 sn oynanış kaydı (DoD-G) → iPhone build (runbook) → **3-5 kişiyle playtest (2-3 gün)**:
   kişi başı ≥15 dk yönlendirmesiz oynama → `docs/PLAYTEST_3G.md` doldur (özellikle: sakatlık karar
   paneline tepki, şerit oynayınca yüz ifadesi, değişikliği kendiliğinden keşif).
4. Formu doldurup push'la → Claude kapı raporunu yazar. Çökme/bloklayıcı hata dışında YENİ İSTEK YOK
   — içerik dondu; "şu da olsa" notları FAZ 03 backlog'una yazılır, greybox'a girmez.

**Claude (bekleyen/koşullu):**
- Playtest verisi gelince: kapı raporu + DECISIONS kapanış satırı → **FAZ 03 açılış brifi**
  (ME Spec motoru iskeleti + Model Maçı sunum katmanının taşıma planı + [KALİBRE-G]→[KALİBRE] listesi).
- Playtest'i engelleyen hata çıkarsa aynı gün düzeltme (tek istisna).
- Bekleyen kararlar (DECISIONS): BRIEF RA#1 metninin v4.2 revizyonu; kapı kararı satırı;
  **Paket B GDD v4.2 adayları** (koşullu ön-emirler, Oto-Koç — otomatik yürütme GDD 12.1'den
  sapmadır, deterministik + Tek Kapı + şeffaf rozet + "canlı insan > oto-koç" bandı ilkeleriyle;
  online/offline asimetri yazımı) — `GREYBOX_ONERI_IT11.md` §2.

**Açık borçlar (bilinçli):** 4 kapılı tam doğrulama FAZ 04'te; uzatma dakikaları yok; devre arası
saha değişimi yok; izometrik kamera FAZ 05+ (greybox ölçek+gölgeyle veriyor); A/B "canlı 2D maç modu"
kararı playtest sonrasına.

## 6. Süreç kuralları (Atilla'nın koyduğu, yürürlükte)

1. **Yazı → onay → kod:** maç/sahne davranışı önce `GREYBOX_SAHNELEME.md`/`GREYBOX_MODEL.md`'e yazılır,
   onaydan sonra kodlanır; kod-doküman uyumu denetimlerle bağlanır.
2. **Hiçbir şey için acele etme:** sunum anları (kutlama, vinyet, geçişler) tam yaşanır; feragat yok.
3. **Push yalnız `claude/3g-greybox-task-plan-76qg49`'a**; başka dala açık izinle.
4. Saatlik PR check-in'leri DURDURULDU (Atilla talebi) — yeniden kurma.
5. Belirsizlikte varsayım yok: seçenekler artı/eksiyle sunulur, Atilla seçer (motor kararı örneği).

## 7. Doküman haritası

| Doküman | İçerik |
|---|---|
| `docs/GREYBOX_DURUM.md` (bu) | Derleme: yapılanlar/nedenler/yapılacaklar — oturum başlangıç noktası |
| `docs/GREYBOX_3G_RAPOR.md` | İterasyon iterasyon teslim raporu + kanıtlar + DoD-G |
| `docs/GREYBOX_SAHNELEME.md` (v2.0) | §0 Model Maçı ana deneyim sözleşmesi + v1.x 2D sahne kuralları (vinyet sözleşmesi) |
| `docs/GREYBOX_MODEL.md` | 8 etkenli olasılık kriter modeli + DP açıklaması + kalibrasyon + FAZ 03 hizası |
| `docs/GREYBOX_ONERI_IT11.md` | İt.11 öneri paketi (A: greybox — UYGULANDI; B: GDD v4.2 adayları — bekliyor) |
| `docs/GREYBOX_ONERI_IT12.md` | İt.12 önerisi (S1 Kadro Kimliği — UYGULANDI) |
| `docs/PLAYTEST_3G.md` | Kapı formu (doldurulacak) |
| `docs/DECISIONS.md` | Pivot kaydı + bekleyen kararlar |
| `unity/UNITY_SETUP.md` | Atilla runbook (açılış → test → build) |
