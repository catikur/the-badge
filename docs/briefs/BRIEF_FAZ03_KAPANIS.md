# FAZ 03 Kapanış Planı — açık işlerin tamamı

Durum tarihi: 2026-08-10 · Taban: PR #10 sonrası `main` (62ca2e1) · Checks 62 PASS yeşil

Bu doküman FAZ 03'te AÇIK kalan her işi tek listede toplar, sıraya koyar ve her birine
**kabul kapısı** yazar. Kaynak: `docs/DECISIONS.md` borç kayıtları + `ME Spec 18.3` sprint
eşlemesi. Sıra keyfi değildir: önce ölçümü bozan model eksikleri, sonra kalibrasyon, sonra
FAZ 04 arayüz dondurması.

## Bugünkü taban (ME 17.2 karşılaştırması)

| Metrik | Hedef bant | Bugün | Durum |
| --- | --- | --- | --- |
| Pas isabeti | 78-86 | %80 | ✓ |
| Şut | 20-28 | 23,2 | ✓ |
| Faul | 18-28 | 27,7 | ✓ |
| Kart | 3,0-5,0 | 2,9-3,6 | ✓ |
| Ofsayt | 2-5 | 4,0 | ✓ |
| Sakatlık | 0,35-0,60 | 0,42 | ✓ |
| Bitiş enerjisi | 350-550 | 424 | ✓ |
| **Gol** | 2,4-3,0 | 3,3-3,7 | ✗ hafif üstünde |
| **Korner** | 8-12 | 7,0-8,3 | ✗ hafif altında |
| **Penaltı** | 0,20-0,35 | ~0 | ✗ |
| **Taç** | — | ~0-4 | ✗ düşük |

## Sıra

### M9 — Kontra atak ve geçiş modeli  *(en yüksek değer)*
İki borç muhafızı da aynı eksik mekanizmadan doğuyor: hat arkasına hızlı kontra yok.
- Geçiş anında (top kazanıldığı an) **doğrudan/dikey oyun penceresi**: kazanan takımın
  fayda ağırlıkları kısa süre ileri kayar; rakip henüz şekline dönmemiştir (geçiş penceresi
  M8-B'de eklendi, kullanılmıyor).
- Hat arkası boşluğun kontra sırasında **gerçekten değerli** olması: ara pas ulaşım yarışı
  geçiş penceresinde savunanın toparlanma gecikmesini görmeli.
- **Kapı:** `M7AttackRiskRegresyon` ×0,80 → **>1,00** (hücumun bedeli doğar) ve
  `M7DefendRegresyon` ×1,51 → **<1,00** (savunmak ödül verir). İkisi de sert kapıya döner.

### ~~M10 — Duran top ve ceza sahası üretimi~~ *(kısmen kapandı 2026-08-13)*
Orta aksiyonu eklendi; korner ve taç bantta. **Penaltı borcu M11'e taşındı.**

<details><summary>özgün madde</summary>

#### M10 — Duran top ve ceza sahası üretimi
Korner 7-8 (hedef 8-12), penaltı ~0 (hedef 0,20-0,35), taç düşük.
- Kanat/orta akışı: ceza sahasına ORTA (ME 10.2 zinciri var, besleme yok).
- Ceza sahası içi ihlal → penaltı üretimi (ME 11.2 + `cezaSahasiIhtiyatCarpan` dengesi).
- **Kapı:** korner 8-12 ✓ (7,9-8,8) · taç > 8/maç ✓ (43-48) · penaltı 0,15-0,40 ✗ (~0 → M11)
</details>

### M11 — Gol bandı ve şut kalitesi ince ayarı
Gol 3,3-3,7 → 2,4-3,0; xG/şut gerçek ~0,10-0,13'e yaklaşmalı.
- **Kapı:** gol 2,4-3,2 · xG/şut ≤ 0,20 · isabetli şut 7-11.

### M12 — VAR dram sistemi (ME 11.4)
Saha kararı kalır oranı chaos seviyesine bağlı, 20-90 sn bekleme, karar iptali.
- **Kapı:** VAR olayı üretimi + geri alınan karar oranı bandı + determinizm.

### M13 — Hava ve zemin (ME 12.4) — ✅ TAMAM (2026-08-13)
Yağış/zemin → sürtünme, sekme, pas hatası, sakatlık çarpanları.
- ~~**Kapı:** kuru/ıslak koşuda ölçülebilir ve BANTTA kalan fark.~~
- **Uygulanan kapı (plandan SAPMA — gerekçe aşağıda):** (1) referans koşul (kuru + Tier 3 +
  rüzgarsız) BİT DÜZEYİNDE değişmedi — `M13NotrAynilik` + M0-M12 golden'ları; (2) her koşul
  kendi içinde tekrarlanabilir ve kurudan farklı (`M13Determinizm`, `M13KosulEtkisi`);
  (3) rüzgar sapması doğrudan geometriyle ölçülüyor ve hıza doğrusal (`M13Ruzgar`);
  (4) a_roll'ün imzası ölçülebilir (`M13IslakMenzil`, `M13KarMenzil`); (5) sıcakta kondisyon
  düşüyor (`M13SicakKondisyon`); (6) her koşul FUTBOL ZARFI içinde (`M13FutbolZarfi`).
- **Sapmanın gerekçesi:** "her koşul ME 17.2 bandında kalsın" şartı 12.4'ün kendisini silerdi —
  17.2 bandı REFERANS koşul içindir, havanın maçı kaydırması özelliğin ta kendisidir. Ölçülen
  bant dışı sapmalar (yağmurda korner 14,7 · karda 3,9) silinmedi: mekanizmasıyla birlikte
  DECISIONS.md'ye M16 borcu olarak yazıldı (kök: `pass.groundSpeedMin` × 1/a_roll aşım etkisi,
  kuru koşulda da var).

### M14 — Maç sonu veri paketi + event log + highlight (ME 15.1/15.3/15.4) — ✅ TAMAM (2026-08-14)
LLM ve Panorama'nın girdisi; FAZ 04'ün beslendiği yer.
- ~~**Kapı:** paket şeması + highlight puanı > 0,50 olan an sayısı bandı.~~
- **Uygulanan kapı:** `M14TekKaynak` (paket istatistiği = motor sayaçları, birebir) ·
  `M14TamponTasmasi` (4096 halka yetiyor: tepe 1.651, düşen 0) · `M14LogDeterminizmi` (aynı tohum =
  alan alan aynı olay dizisi) · `M14PaketSemasi` (eğriler 90 nokta, en yüksek anlar H'ye göre
  azalan, H ∈ [0,1]) · `M14HighlightSiralamasi` (golü olan her maçta gol ilk 10'da) ·
  `M14EventHacmi` + `M14SariBandi` + `M14KirmiziBandi`.
- **"H > 0,50 an sayısı bandı" maddesi kapı OLMADI:** ölçüm 0,5-0,8/maç ve bu bir kalibrasyon
  değil formül özelliği (nadirlik tablosu maksimumda bile 20. dakika golü eşiği geçmiyor).
  Aritmetiğiyle DECISIONS.md'ye yazıldı, "Bekleyen kararlar"a 3 seçenekli öneri eklendi.
- **Event log'un ilk kazancı:** M4'ten beri `kart = sarı + kırmızı` toplamının içinde saklanan
  **kırmızı kart 1,0/maç** (bant 0,15-0,30) görünür oldu — tamamı ikinci sarı. M16 borcu.
- **M12'nin 2 VAR sınıfı hâlâ açık:** engel event log değilmiş — ikisi de "gol verilir, sonra
  incelenir, geri alınır" akışını istiyor; motorda askıda gol durumu yok. Ayrı dilim önerilir.

### M15 — LOD 1-2 türetme (ME 16.1) + sunucu throughput (16.3) — ✅ TAMAM (2026-08-16)
Ligdeki tüm maçların aynı anda koşabilmesi buna bağlı.
- **Kapı:** ~~LOD 1/2~~ LOD 2 sonuç dağılımı LOD 0 ile istatistiksel uyum + CPU bütçesi (16.4). ✓
- **Ölçüm önce:** LOD 0 **131 ms/maç** — 16.1 bütçesinin (2.500 ms) **19 katı altında**;
  24 çekirdekli düğüm ~185 maç/sn (16.3 hedefi 16,7). 16.3'ün "2 düğüm zirvede" hesabı
  2,5 sn/maç varsayımına dayanıyordu.
- **LOD 1 → LOD 0'ın eşleniği yapıldı** (karar, DECISIONS.md): tek gerekçesi CPU'ydu ve o gerekçe
  ölçümle düştü. `M15Lod1Esdeger` kapısı bit-aynılığı doğruluyor.
- **LOD 2 ızgara tablosu** (kendi güç × rakip güç, iki doğrusal ara değerleme) — 3 µs/maç,
  5 güç kademesinde LOD 0 ile ±%25 uyum. Üretici: `-- fit-lod2` (ME 16.1 CI adımı).
- **Bu dilimin en ağır çıktısı bir BULGU:** motorun güç tepkisi aşırı dik — 75,6'lık takım
  39,6'lık takımı 28-0,1 yeniyor (gerçek ~3-0). Köşegen (eşit güç) bantta. ME 17.3 chaos upset
  doğrulaması bu eğrinin üstünde durur → **M16'nın asıl işi**, artık sayısıyla birlikte.

### M16 — 10.000 maç kalibrasyon + chaos upset (ME 17.2/17.3)
Bugüne kadar 8-24 maçlık örneklerle çalıştık; spec 10k istiyor. 75v55 upset ve 65v65
beraberlik bandı (%22-30) doğrulaması burada.
- **Kapı:** 17.2 tablosunun TAMAMI + 17.3 toleransları.

#### M16-A — Sonuç dağılımı teşhisi — ✅ TAMAM (2026-08-16)
Kalibrasyondan ÖNCE "neden kalibre edilemiyor" ölçüldü.
- **Eşit güçte beraberlik %27** — ME 17.3 hedefi %22-30 → ✓ zaten bantta. Denk maç sağlam.
- **75v55: %100 / %0 / %0** (hedef %66/%18/%16). Kök ölçüldü: sahiplik gerçekçi (60/40) ve
  atak sayısı neredeyse eşit; kırılma tek yerde — **şut/atak 0,566 vs 0,004 (×100)**.
- Bir atağın şuta dönmesi ~8 ardışık başarı istiyor (futbolda 3-4); zincir uzun çünkü sahiplik
  maç başına 374 kez el değiştiriyor (gerçek ~120). Uzun zincirde halka başına küçük üstünlük
  ÜSTEL katlanıyor.
- **Tek katsayı çözmüyor:** `kDuel` 0,90 → 0,20 (×4,5 azaltma) 75v55'i yalnız %99,5 → %87
  yapıyor; güç farkı ~8 ayrı kanaldan akıyor.
- **Bu kök, M13/M14/M15'te ayrı ayrı yazılan dört borcun da kökü** — hepsi pas/sahiplik
  modelinin topu çok sık ve çok kısa oynatmasına çıkıyor.
- Kapılar: `M16BeraberlikBandi` (geçiyor) · `M16UpsetBandi` (borç muhafızı).

#### M16-B — Pas aşımı düzeltmesi — ⛔ DENENDİ, ÖLÇÜLDÜ, GERİ ALINDI (2026-08-16)
`groundSpeedMin` taban kırpması yerine fizikten türetilen varış hızı modeli denendi
(v0² = v_varış² + 2·a·d → aşım mesafesi sabit).
- **Hipotez yanlış çıktı:** aşımı tamamen kaldırmak sahiplik değişimini 360 → 331'in altına
  indiremedi (hedef ~120) ve **75v55 hiç kıpırdamadı** (%97-100). Hiçbir v_varış değeri de
  ME 17.2 bandını korumuyor. Ölçülebilir kazanç olmadan golden'lar yeniden pinlenmez → geri alındı.
- **Düzeltilmiş kök:** her v_varış değerinde **tackle ≈ sahiplik değişimi** (333≈342, 298≈331).
  Sahiplik neredeyse her seferinde bir tackle ile el değiştiriyor; gerçekte maç başına ~35 tackle
  var, bizde 260-415 — model **~10 kat fazla tetikleniyor.** Sıradaki deneme burasıdır.
- Yama saklandı: `scratchpad/M16B_pas_varis_hizi.patch`. Ölçüm tablosu DECISIONS.md'de.

#### M16-C — Tackle tetikleme ölçümü — ✅ TAMAM (2026-08-16)
Enstrüman (davranış-nötr: deneme aralığı karar kilidinden ayrıldı, ayrışım sayaçları, golden'lar
korundu) + süpürme + karar.
- **Ayrışım:** sahiplik değişimi 341 = tackle 165 + **pas kesme 167** + serbest 10. Zincir iki
  eşit motorla dönüyor; M16-A'nın "sürücü tackle" okuması yarım doğruymuş.
- **Süpürme:** deneme 873→431, tackle kaynaklı değişim 193→74 — AMA kesme kanalı sabit (~185
  taban), toplam yalnız 379→278 ve **75v55 yerinden oynamadı (%99,2→%96,7)**. Varsayılanlar
  değişmedi; katsayı ancak M16-E tam kalibrasyonuyla anlamlı.
- **Asıl sonuç:** eksik mekanizmanın spec'te adı var — ME 7.2 **LongSwitch/ClearBall** + ME 9.4
  kaleci **UzunDegaj/ElleAt** motorda YOK (M10'daki "orta" durumunun aynısı). Zayıf takımın
  zinciri kısaltma yolu bunlar → M16-D uygular, upset ondan sonra yeniden ölçülür.

#### M16-D — Uzun top + kaleci dağıtımı + CHAOS motoru — ✅ TAMAM (2026-08-16)
Üç spec borcu birden: ME 7.2 aday kümesi (LongSwitch/ClearBall), ME 9.4 kaleci dağıtımı
(KısaAçıl/UzunDegaj/ElleAt — kale vuruşları dahil), ME 13.1-13.3 chaos motoru (5 enjeksiyon
noktasının TAMAMI, 3 seviye, MatchConfig.Chaos).
- **Kullanım (Orta):** uzun top 27/maç (%54 kazanma) · temizleme 26 · GK 11/32/6,5.
  Uzun top kullanımı chaos seviyesiyle kendiliğinden artıyor (18→29→66).
- **ME 13.4'e karşı:** Düşük %99,3 · Orta %98,7 · Yüksek %94,7 (hedef %76/%66/%54) — YÖN ilk
  kez doğru, büyüklük M16-E tam kalibrasyonunun işi.
- Golden'lar yeniden pinlendi; LOD 2 tablosu yeni motorla yeniden üretildi (7.840 maç);
  ME 17.2 bantları korundu. Kapılar: `M16DKullanim` · `M16DChaosDeterminizm` ·
  `M16DChaosSeviyeEtkisi` · `M16DUpsetYuksek` (borç muhafızı).

#### M16-E — açık (sıradaki)
10.000 maçlık 17.2 tablosu + 13.4/17.3 upset kalibrasyonu (çok katsayılı arama) + kart/faul
hacmi (kırmızı 1,0/maç, avantaj 28/maç; kart üretiminin kadro-farkı bağımlılığı → kalibrasyon
setinin kadro dağılımı tanımlanmalı).

### M17 — Golden replay seti (ME 17.4) + FAZ 04 arayüz dondurması (18.3)
- **Kapı:** replay dörtlüsü (seed + config_hash + komut zaman çizelgesi + sürüm) ile
  bit-eşit yeniden üretim; sim ↔ sunucu/Unity sözleşmesi dondurulur.

## FAZ 03 dışı ama açık (karar bekleyen — kod değil, senin kararın)

1. **Model Maçı sunumu**: greybox emekli; motor üstünde YENİDEN tasarlanacak ve küçük,
   mülakatlı bir turla doğrulanacak (5G Dikey Dilim öncesi borç).
2. **GDD v4.2 adayları**: (a) koşullu ön-emirler, (b) Oto-Koç (kural tabanlı, Tek Kapı,
   şeffaf rozet), (c) online/offline asimetri ilkesinin yazımı.
3. **BRIEF_3G_GREYBOX RA#1** metninin pivot sonrası revizyonu.
4. **Premium etkilerin public ligde şeffaf rozeti** (FAZ 02 öncesi tasarım kararı).

## Çalışma kuralı

Her dilim: plan → uygula → **Checks yeşil** → kanıt (sayılarla) → DECISIONS kaydı → PR.
Kapı geçmeyen dilim gönderilmez; geçmeyen kapı sessizce gevşetilmez, borç muhafızına
çevrilirse gerekçesi ve HEDEFİ ekrana basılır.
