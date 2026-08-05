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

## 3. HÜCUM POZİSYONU (ceza sahasına giriş)

- Final üçlüde tempo yükselir: 1-2 hızlı pas kutu çevresindeki hücumculara; forvetler kutuya dalar.
- Pozisyon ya ŞUTLA ya savunmanın uzaklaştırmasıyla (bazen kornerle) biter.

## 4. ŞUT SONUÇLARI

| Sonuç | Sahne |
|---|---|
| **GOL** | Top AĞLARIN İÇİNDE durur ve sevinç boyunca orada kalır → Sahne 6. |
| **KURTARIŞ** | Top kalecinin önünde/ellerinde ölür; kaleci ~1.5 sn tutar, kısa pasla oyunu başlatır (alıcıya). Bazen kornere çeler → Sahne 5. |
| **AUT** | Top direk dışından çizgiyi geçer → **KALE VURUŞU:** top kale sahasına konur, kaleci başına gelir, savunma açılır, hücum orta sahaya çekilir; kaleci pasıyla devam. |
| **KORNERE SEKME** | → Sahne 5. |

## 5. KORNER

**Kurulum:** Top köşe noktasına; korner kullanıcısı topun başına gider.

**Diziliş koşulu (orta şartı) — İKİSİ DE sağlanmadan orta GELMEZ:**
- Hücum: ≥5 oyuncu ceza sahası İÇİNDE, 2 oyuncu kutu önünde (rebound), 2 oyuncu geride kontra sigortası.
- Savunma: kutu oyuncuları hücumcuların GOL TARAFINDA adam tutuyor; kaleci çizgide; 1-2 forvet kontra için orta sahada.

**Akış:** Orta kutudaki bir hücumcuya → kafa vuruşu (gol/kurtarış/aut) YA DA savunma uzaklaştırır:
top kutu dışına, kapan takımın oyuncusunun ayağına → açık oyun.

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
- v1 (2026-07-31): İlk metin — Atilla onayı bekliyor. Onaydan sonra kod bu senaryoya hizalanır;
  sapmalar önce BU DOSYADA değiştirilir, sonra koda iner.
