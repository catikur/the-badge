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

1. **Maç öncesi:** 3 taktik kartı (Savunma/Denge/Hücum) + kadro gücü + rakip gücü + son-5 form.
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

**Değişmeyen anayasa uyumu:** `shared/TheBadge.Sim`'e dokunulmadı (yalnız `Rng` tüketildi);
tüm rastgelelik sayaç-RNG (`Rng.Hash64`, Domain gerekçeli); durum değiştiren her eylem
`CommandEnvelope`'la bus'tan; docs/ spec'leri değişmedi (öneriler DECISIONS'a yazıldı).

## 4. Doğrulama hattı (her değişiklikte koşulan)

| Kapı | Komut | Son durum (2026-08-07) |
|---|---|---|
| Çekirdek | `dotnet run --project shared/TheBadge.Sim.Checks -c Release` | ✅ YEŞİL |
| Harness (scratchpad, repo dışı) | 300 maç FlowSim pacing + sahne sözleşmesi + ekonomi + bus + 400 maç model (gol 2.71, kalibrasyon %38 vs %40, olay bantları sarı 2.08/kırmızı 0.23/sakatlık 0.64, yorgunluk/taze bacak/karar kilidi/olay determinizmi, değişiklik bus 4 negatif, vinyet iki takım) | ✅ TÜM KONTROLLER YEŞİL |
| Stub derleme (2 yol: varsayılan + `ENABLE_INPUT_SYSTEM;UNITY_IOS`) | Unity katmanı sözdizimi/tip kontrolü | ✅ 0 hata / 0 hata |
| EditMode testleri (Editor'de) | pacing, determinizm-lite, sahne sözleşmesi aynası, `ModelMatchTests` (7) | ⏳ Atilla — Editor'de koşulur |

Harness/stub scratchpad'te yaşar (repo'ya girmez); yeni oturumda gerekirse rapordaki tarife göre yeniden kurulur.

## 5. YAPILACAKLAR

**Atilla (kapıya giden yol):**
1. `git pull` (Unity KAPALIYKEN; makine ProjectSettings'i değiştirdiyse önce `git checkout -- .`).
2. Editor'de oyna — iterasyon 11 kontrol listesi: (a) maç öncesi isimli kadro; (b) enerji maç boyu
   düşüyor, tempo yükseltince daha hızlı mı; (c) sarı/kırmızı/sakatlık feed'e düşüyor mu; (d) bizim
   sakatlıkta karar paneli açılıyor, değiştir/eksik-devam şeridi oynatıyor mu; (e) OYUNCU DEĞİŞ ile
   manuel değişiklik + taze bacak etkisi; (f) DETAY "koç masası" (kadro durumu, kaydırma); (g) rakip
   gol vinyeti oynuyor ve feed vinyet sonrası SİLİNMİYOR mu; (h) konsol 0 error/0 warning.
3. His onayı → 30-60 sn kayıt (DoD-G) → iPhone build (runbook) → 3-5 kişi playtest →
   `docs/PLAYTEST_3G.md` doldur → **GO/NO-GO kararı → DECISIONS.md**.
4. Geri bildirim varsa iterasyon 12 açılır (süreç: davranış değişikliği önce yazıyla).
   Not: İt.11 SON içerik iterasyonuydu (timebox); bundan sonrası yeni özellik değil ayar/pürüz turu.

**Claude (bekleyen/koşullu):**
- Geri bildirim iterasyonları (his Atilla onayına bağlı — sayısal kanıt his kanıtı DEĞİL).
- GO çıkarsa: greybox emekliliği + FAZ 03'e taşıma haritası (`GREYBOX_MODEL.md` §FAZ 03 hizası:
  model→ME Spec LOD, [KALİBRE-G]→[KALİBRE] taşıma listesi). NO-GO çıkarsa: pivot dersleriyle tez revizyonu.
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
| `docs/PLAYTEST_3G.md` | Kapı formu (doldurulacak) |
| `docs/DECISIONS.md` | Pivot kaydı + bekleyen kararlar |
| `unity/UNITY_SETUP.md` | Atilla runbook (açılış → test → build) |
