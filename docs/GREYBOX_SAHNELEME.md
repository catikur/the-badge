# Greybox Maç Sahneleme Senaryosu (v1)

**Amaç:** 2D greybox maçının her sahnesini KODDAN ÖNCE yazıyla sabitlemek (Atilla süreç kararı, 2026-07-31).
Kod bu senaryoya uymak zorundadır; senaryoya girmeyen davranış koda giremez.

**Kök ilke — bu güne kadarki hatanın düzeltmesi:**
> Bir sahne, DİZİLİŞ KOŞULU SAĞLANMADAN başlamaz. Süre değil, yerleşim tetikler.
> (Her koşulun kilitlenme emniyeti vardır: diziliş ~8 sn'de kurulamazsa oyun yine akar.)

**Bilinçli basitleştirmeler (greybox kapsamı):** taç atışı yok (top taç çizgisinden çıkmaz),
faul/serbest vuruş/penaltı yok, ofsayt yok, oyuncu değişikliği yok, taraflar devre arasında değişmez.
Bunlar his prototipinin dışındadır; itiraz varsa bu doküman üstünden konuşulur.

---

## 1. SANTRA (maç başı · devre başı · gol sonrası)

**Diziliş koşulu (düdük şartı):**
- 22 oyuncunun TAMAMI kendi yarı sahasında (Kural 8).
- Santrayı kullanmayan takımın tüm oyuncuları orta yuvarlağın (9.15 m) DIŞINDA.
- Santra takımından forvet topun başında; ikinci forvet hemen çember kenarında.
- Kaleciler kale önünde.

**Akış:** Koşul sağlanınca düdük → topun başındaki forvet geriye/yana kısa pas → AÇIK OYUN.
**Gol sonrası:** iki takım da sahnenin dönüş koşusunu hızlı tempoda yapar (~4-6 sn izlenir;
skip/2x çalışır; spiker "Oyun yeniden başlamak üzere..." der). Santrayı gol YİYEN takım kullanır.
İkinci yarı santrası deplasmanındır.

## 2. AÇIK OYUN

- Top her an ya bir oyuncunun ayağındadır ya da havada bir ALICIYA gitmektedir; boş alana pas yok.
- Pas anonsu → alıcı buluşma noktasına koşar → top ayağına gelir → kısa kontrol → yeni karar
  (kısa ileri/yan/geri pas, riskli uzun top, kısa taşıma).
- Savunma: topa en yakın oyuncu prese çıkar; blok topa göre daralır/kayar; hücum bloğu öne kayar.
- Top kaybı: ara pas kesilir; kapan oyuncunun ayağıyla oyun kesintisiz devam eder.

**Top fiziği (v1.1):**
- Pas, alıcıya yaklaşırken YAVAŞLAR (sürtünme hissi — top ayağa gelir, düşmez).
- Topun sahibi topu AYAĞINDA taşır: top taşıyıcıya yapışıktır, taşıyıcı hücum yönüne kısa
  driplingle ilerler; taşıyıcı toptan uzaksa (kapma anı) önce topa gider.
- Şutlar serttir: sabit yüksek hız, yavaşlama yok.
- Sunum her oyun hızında (1x/2x/slow-mo) SABİT SİM ADIMI + kareler arası interpolasyonla
  akıcı kalır; hız değişimi top-oyuncu ilişkisini bozamaz.

**Top yüksekliği ve perspektif (v1.2):**
- Kısa pas YERDEN gider; uzun top, korner ortası ve kaleci degajı HAVADAN parabolik yayla gider.
- Havadaki top kameraya yaklaştığı için ekranda BÜYÜR, gölgesinden ayrılır (gölge yerde kalır),
  düşerken küçülüp ayağa iner. Işınlanan top YASAKTIR — her top hareketi görünür bir uçuştur.
- Tam izometrik/eğik kamera greybox kapsamı dışıdır (2.5D prerender FAZ 05+); greybox üstten
  bakışta yükseklik hissini ölçek + gölge + kaldırma ile verir.

**Oyuncu yönü (v1.2, v1.3 revizyonu):**
- Her daire, baktığı yönün ÖNÜNDE YAN YANA iki küçük "ayak ucu" çıkıntısı taşır: hareket
  halindeyken gittiği yöne, dururken topa döner.

**SAHİPLİK DEĞİŞMEZİ (v1.3 — motor kararı, Buckland/Simple Soccer modeli):**
Top hiçbir an ÖZERK değildir. Her an şu dört durumdan tam birindedir:
1. **KONTROLDE:** bir oyuncunun ayağında (yapışık); kararlar YALNIZ bu durumdan üretilir.
2. **HAVADA:** vuran X'ten alan Y'ye giden isimli bir uçuş (pas/orta/şut/degaj/uzaklaştırma).
3. **DURAN TOP:** santra/korner/kale vuruşu noktasında, kullanıcısını bekliyor.
4. **SERBEST:** kimsenin değil — iki takımın da en yakın oyuncusu topa koşar, İLK ULAŞAN
   kontrol eder; serbest topta hiçbir pas/şut kararı üretilemez (4 sn emniyet).
- **HİÇBİR VURUŞ, vuran oyuncu topun yanında değilken gerçekleşemez (kontrol ~1.6 m; kafa/uzaklaştırma uzanması ≤ ~2.2 m)** — sahne
  sözleşmesi bunu her vuruşta otomatik denetler.
- Top kapma: pres yapan oyuncu yakınsa TEMİZ ÇALAR (top ayağına geçer); uzaksa top açığa
  çıkar (küçük sekme) ve kapışılır.
- Korner ortasında karşılayan uzaksa top kutuda SERBEST kalır (kapışma/karambol); uzaklaştıran
  savunmacı da topun yanında olmak zorundadır.

## 3. HÜCUM POZİSYONU (ceza sahasına giriş)

- Final üçlüde tempo yükselir: 1-2 hızlı pas kutu çevresindeki hücumculara; forvetler kutuya dalar.
- Pozisyon ya ŞUTLA ya savunmanın uzaklaştırmasıyla (bazen kornerle) biter.

## 4. ŞUT SONUÇLARI

| Sonuç | Sahne |
|---|---|
| **GOL** | Top AĞLARIN İÇİNDE durur ve sevinç boyunca orada kalır → Sahne 6. |
| **KURTARIŞ** | Top kalecinin önünde/ellerinde ölür; kaleci ~1.5 sn tutar, kısa pasla oyunu başlatır (alıcıya). Bazen kornere çeler → Sahne 5. |
| **AUT** | Top direk dışından çizgiyi geçer → **KALE VURUŞU:** top kale sahasına konur, kaleci başına gelir, savunma açılır, hücum orta sahaya çekilir; kaleci KISA PASLA ya da (sık sık) YÜKSEK DEGAJLA devam eder. |
| **KORNERE SEKME** | → Sahne 5. |

## 5. KORNER

**Kurulum:** Top köşe noktasına; korner kullanıcısı topun başına gider.

**Diziliş koşulu (orta şartı) — İKİSİ DE sağlanmadan orta GELMEZ:**
- Hücum: ≥5 oyuncu ceza sahası İÇİNDE, 2 oyuncu kutu önünde (rebound), 2 oyuncu geride kontra sigortası.
- Savunma: kutu oyuncuları hücumcuların GOL TARAFINDA adam tutuyor; kaleci çizgide; 1-2 forvet kontra için orta sahada.

**Akış:** Orta kutudaki bir hücumcuya → kafa vuruşu (gol/kurtarış/aut) YA DA savunma uzaklaştırır:
uzaklaştırma HAVADAN uçan bir toptur (ışınlanma yok) — top kutu dışına süzülür, kapan takımın
oyuncusu buluşma noktasına koşup karşılar → açık oyun.

## 6. GOL SEVİNCİ + YENİDEN BAŞLAMA

1. Gol anı: yavaşlatma + titreşim + flaş + spiker; top ağlarda.
2. Sevinç (~3 sn): atan takım (kaleci hariç) SKORERİN etrafında kümelenir; yiyen takım santraya yürümeye başlar.
3. Top santra noktasına taşınır; **Sahne 1'in diziliş koşulu beklenir** — herkes kendi yarısına
   dönmeden düdük çalmaz. Santra gol yiyenin.

## 7. DEVRE ARASI / MAÇ SONU

- 45' dolunca ilk uygun anda (pozisyon ortası kesilmez) düdük → kısa "DEVRE ARASI" bandosu →
  ikinci yarı SANTRA sahnesiyle (deplasman) başlar.
- 90' → maç sonu düdüğü → maç sonu ekranı.

---

## Sahne sözleşmesi testleri (kod bu dokümana karşı denetlenir)

Harness/EditMode assert'leri:
1. **Her santra düdüğü anında:** 22 oyuncu kendi yarısında; santra kullanmayan takım çember dışında.
2. **Her korner ortası anında:** hücumdan ≥5 oyuncu kutu içinde; savunmadan ≥5 kutu içinde.
3. **Her golde:** topun son konumu kale ağzı sınırları içinde ve çizginin arkasında.
4. Kilitlenme emniyeti: hiçbir diziliş beklemesi ~8 sn'yi aşamaz (aşarsa sahne zorla başlar, sayaç loglanır).

## Sürüm
- v1 (2026-07-31): İlk metin — Atilla onayladı; kod hizalandı (iterasyon 3).
- v1.1 (2026-08-01): §2 Top fiziği eklendi (pas yavaşlaması, ayakta taşıma, sabit sim adımı +
  interpolasyon) — Atilla'nın "2x'te top-oyuncu dinamikleri karışıyor" geri bildirimi üzerine.
- v1.2 (2026-08-01): Top yüksekliği/perspektif + oyuncu yön çıkıntısı + korner uzaklaştırma
  uçuşu (ışınlama yasağı) + kaleci degajı — Atilla'nın perspektif/yön geri bildirimi üzerine.
- v1.3 (2026-08-02): **Sahiplik Değişmezi** (motor kararı: Yol A — Simple Soccer sahiplik modeli
  retrofit'i; "kimse yokken pas" sınıfı hatanın yapısal yasağı) + ayak ucu çifti revizyonu.
