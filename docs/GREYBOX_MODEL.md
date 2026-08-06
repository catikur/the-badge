# Greybox Model Maçı — Olasılık Kriter Modeli (v1)

**Amaç:** "Blok gol ihtimali %18" sayısının NEYE GÖRE hesaplandığını kalıcı olarak sabitlemek
(Atilla talebi, 2026-08-02). Kod bu dokümana uyar; kriter değişikliği önce buraya yazılır.
Tüm katsayılar `greybox.balance.json → model.*` altında **[KALİBRE-G]**'dir.

## Toplam formül

```
p(taraf, blok) = Taban × Güç × Taktik × Faz × Momentum × Skor × TempoModu × Ev × Form
                 → clamp[pGolMin, pGolMax]
```

Her etken oyun içinde de görünür: blok kartının altında "etkenler: güç ×1.12 · faz ×1.05 ..."
satırı, 1'den sapan çarpanları gösterir (`MatchModel.Factors`).

## Etkenler

| # | Etken | Girdi | Formül | Anahtar | Gerekçe |
|---|---|---|---|---|---|
| 1 | **Kadro gücü** | iki takımın güç puanı (0-100) | `1 + gucEtkiMax × tanh(fark / gucOlcek)` | `gucEtkiMax` 0.45, `gucOlcek` 18 | tanh ile DOYGUN: uç farklar orantısız patlamaz; ±18 puan ≈ etkinin ~%76'sı |
| 2 | **Taktik etkileşimi** | iki tarafın preseti | `Matchup[atk][def] × tempo × şutİştahı × (1 − presEtkisi)` | `taktikMatchup` (3×3), `taktikTempoEtki`, `taktikSutEtki`, `taktikPresSavunmaEtki` | taş-kağıt-makas katmanı: Savunma Bloku kontrayla Hücum Baskısı'nı cezalandırır (aşağıdaki matris) |
| 3 | **Maç fazı** | blok indeksi | `fazCarpanlar[blok]` | `fazCarpanlar` (10 eleman) | gerçek futbol: goller son 15 dakikada yoğunlaşır — son blok ×1.25 |
| 4 | **Momentum** | blok süreci (gol ±, OU gürültü) | `1 + momentum × momentumEtki` | `momentumEtki`, `momentumGolDelta`, `momentumSonum`, `momentumBlokGurultu` | maçın psikolojik dalgası; ekrandaki momentum çubuklarının aynısı |
| 5 | **Skor durumu** | anlık fark | geride: ×`gerideRiskCarpan` · önde: ×`ondeKontrolCarpan` | 1.12 / 0.92 | geride kalan riske girer, önde olan maçı soğutur |
| 6 | **Tempo modu** | oyuncu müdahalesi | Yükselt: biz ×1.35 rakip ×1.22 · Kilitlen: biz ×0.75 rakip ×0.62 | `tempoYukselt*`, `kilitlen*` | müdahalenin İKİ YÖNLÜ riski — bilinçli tasarım |
| 7 | **Ev avantajı** | sabit (greybox'ta oyuncu hep ev) | biz ×(1+`evAvantaj`), rakip ×(1−`evAvantaj`/2) | `evAvantaj` 0.06 | seyirci etkisi; ileride bilet/doluluk ile bağlanabilir (FAZ 04 adayı) |
| 8 | **Form** | son 5 maç net galibiyet | `1 + formNet × formEtkiCarpan` | `formEtkiCarpan` 0.03 | tycoon döngüsü maça dokunur: seri kazanmak maçta da hissedilir |

## Taktik etkileşim matrisi (satır: hücum eden, sütun: savunan)

| atk \ def | Dengeli | Hücum Baskısı | Savunma Bloku |
|---|---|---|---|
| **Dengeli** | 1.00 | 1.03 | 0.95 |
| **Hücum Baskısı** | 1.08 | 1.10 | 0.93 |
| **Savunma Bloku (kontra)** | 0.94 | **1.07** | 0.88 |

Okuma: Hücum Baskısı açık savunmalara iyi (1.08-1.10) ama bloğu kıramaz (0.93);
Savunma Bloku, Hücum Baskısı'na KONTRA vurur (1.07). Müdahale kararlarına derinlik verir.

## Blok sonucu ve kazanma şeridi

- Blok zarı: `[0,1)` → `pBiz` altı bizim gol; `pBiz+pRakip` altı rakip gol; kalanı sessiz
  (sessizin bir kısmı `tehlikeCarpan` ile "tehlikeli dakikalar" olayına döner — yalnız sunum).
- **Kazanma şeridi KESİN hesaptır:** kalan bloklar üzerinde skor farkı dağılımı dinamik
  programlamayla taşınır (Monte Carlo yok). Dürüst yaklaşıklık: momentum/tempo İLERİYE dönük
  mevcut değerleriyle sabitlenir — ekrandaki sayı "bu gidişat sürerse" olasılığıdır.
- Kalibrasyon kanıtı (harness, 400 maç): maç başı tahmini G oranı ile gerçekleşen G oranı
  arasındaki fark < 0.10 (test: `ModelCalibration`).

## FAZ 03 eşlemesi (ileriye dönük)

Bu kriterler ME Spec'in gerçek motoruna şu kanallardan bağlanacak: Güç→duello/xG girdileri,
Momentum→ME Spec momentum modülü, Taktik matrisi→taktik parametre setleri, Faz→yorgunluk
(stamina) modeli, Form→moral. Greybox modeli atılırken bu tablo FAZ 03 kalibrasyonuna taşınır.

## Sürüm
- v1 (2026-08-02): 4 etkenli örtük modelden 8 etkenli açık kriter modeline geçiş
  (tanh güç, matchup matrisi, faz eğrisi, skor durumu, ev, form) + oyun içi etken satırı.
