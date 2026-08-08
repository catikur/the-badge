# Greybox Model Maçı — Olasılık Kriter Modeli (v3)

**Amaç:** "Blok gol ihtimali %18" sayısının NEYE GÖRE hesaplandığını kalıcı olarak sabitlemek
(Atilla talebi, 2026-08-02). Kod bu dokümana uyar; kriter değişikliği önce buraya yazılır.
Tüm katsayılar `greybox.balance.json → model.*` altında **[KALİBRE-G]**'dir.

## Toplam formül

```
p(taraf, blok) = Taban × Güç × Taktik × Faz × Momentum × Skor × TempoModu × Ev × Form
                 → clamp[pGolMin, pGolMax]

Güç (v3) = 1 + gucEtkiMax × tanh((HÜCUM_kendi − SAVUNMA_rakip) / gucOlcek)
  HÜCUM/SAVUNMA = mevki ağırlıklı kadro reytingi: Σ oyuncuGücü × enerjiÇarpanı × w(mevki) / Σw(11 slot)
  — bireysel güçler, ENERJİ ve EKSİKLER bu reytinglerin içindedir (eski Yorgunluk/Eksik etkenleri
  v3'te buraya taşındı, çifte sayım yok); payda TAM 11 slottur: eksik oyuncu 0 katkı verir ama
  paydada kalır → kayıp, oyuncunun KALİTESİYLE orantılı acıtır.
```

Her etken oyun içinde de görünür: blok kartının altında "etkenler: güç ×1.12 · faz ×1.05 ..."
satırı, 1'den sapan çarpanları gösterir (`MatchModel.Factors`).

## Etkenler

| # | Etken | Girdi | Formül | Anahtar | Gerekçe |
|---|---|---|---|---|---|
| 1 | **Kadro gücü (v3: reyting)** | bireysel güçler + mevkiler + enerji + eksikler | `1 + gucEtkiMax × tanh((Hücum_kendi − Savunma_rakip)/gucOlcek)`; ağırlıklar wA=[GK 0, DF 1, MF 2, FW 3], wD=[**GK 3**, DF 3, MF 2, FW 1] | `gucEtkiMax` 0.45, `gucOlcek` 18, `squad.hucumAgirlik/savunmaAgirlik/gucYayilim/yedekGucFarki` | KALECİ savunmada en ağır mevki ("etkisiz kaleci" bitti); yıldız kaybı vasat kaybından çok acıtır; tanh doygunluğu korunur |
| 2 | **Taktik etkileşimi** | iki tarafın preseti | `Matchup[atk][def] × tempo × şutİştahı × (1 − presEtkisi)` | `taktikMatchup` (3×3), `taktikTempoEtki`, `taktikSutEtki`, `taktikPresSavunmaEtki` | taş-kağıt-makas katmanı: Savunma Bloku kontrayla Hücum Baskısı'nı cezalandırır (aşağıdaki matris) |
| 3 | **Maç fazı** | blok indeksi | `fazCarpanlar[blok]` | `fazCarpanlar` (10 eleman) | gerçek futbol: goller son 15 dakikada yoğunlaşır — son blok ×1.25 |
| 4 | **Momentum** | blok süreci (gol ±, OU gürültü) | `1 + momentum × momentumEtki` | `momentumEtki`, `momentumGolDelta`, `momentumSonum`, `momentumBlokGurultu` | maçın psikolojik dalgası; ekrandaki momentum çubuklarının aynısı |
| 5 | **Skor durumu** | anlık fark | geride: ×`gerideRiskCarpan` · önde: ×`ondeKontrolCarpan` | 1.12 / 0.92 | geride kalan riske girer, önde olan maçı soğutur |
| 6 | **Tempo modu** | oyuncu müdahalesi | Yükselt: biz ×1.35 rakip ×1.22 · Kilitlen: biz ×0.75 rakip ×0.62 | `tempoYukselt*`, `kilitlen*` | müdahalenin İKİ YÖNLÜ riski — bilinçli tasarım |
| 7 | **Ev avantajı** | sabit (greybox'ta oyuncu hep ev) | biz ×(1+`evAvantaj`), rakip ×(1−`evAvantaj`/2) | `evAvantaj` 0.06 | seyirci etkisi; ileride bilet/doluluk ile bağlanabilir (FAZ 04 adayı) |
| 8 | **Form** | son 5 maç net galibiyet | `1 + formNet × formEtkiCarpan` | `formEtkiCarpan` 0.03 | tycoon döngüsü maça dokunur: seri kazanmak maçta da hissedilir |
| — | ~~Yorgunluk~~ / ~~Eksik~~ (İt.11) | — | **v3'te Güç reytinginin İÇİNE taşındı**: bireysel enerji çarpanı (ME 12.1 vekili, `yorgunlukGucTaban`) + eksik oyuncunun 0 katkısı (tam payda) | `squad.yorgunlukGucTaban` 0.85, `squad.yorgunlukBlokDrenaj` 62 | ayrı etken olarak çifte sayım yapmasın; tempo drenaj bedeli aynen sürer |

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
  programlamayla taşınır (Monte Carlo yok). İt.11/12: **faz eğrisi ve reyting drenajı deterministik
  olduğundan blok blok İLERİ projeksiyon edilir** (reyting eğimi lineerdir — enerji çarpanı enerjide
  lineer, drenaj sabit oranlı); momentum/skor stokastik olduğundan mevcut değerlerinde sabitlenir —
  ekrandaki sayı "bu gidişat sürerse" olasılığıdır. Ekranda tek satırla söylenir (şerit altı, İt.12).
- Kalibrasyon kanıtı (harness, 400 maç): maç başı tahmini G oranı ile gerçekleşen G oranı
  arasındaki fark < 0.10 (test: `ModelCalibration`).

## Kadro ve olay katmanı (İt.11 — Öneri "Koçun Eli", Atilla onayı 2026-08-07)

- **İsimli kadro:** 11 + 5 yedek; isimler kurgusal hece üretimi (Domain.Crowd, kozmetik).
  **v3: her oyuncunun BİREYSEL GÜCÜ var** (Domain.Decision — oynanışa girer): takım tabanı ±
  `gucYayilim` (Gauss), kulübe `yedekGucFarki` kadar zayıf; ilk 11'in düz ortalaması takım tabanına
  NORMALİZE edilir (gol bandı/kalibrasyon korunur — kanıt `ModelSquadGen`). Kaleci olaylara ve
  değişikliğe girmez (FAZ 03) ama SAVUNMA reytinginde en ağır mevkidir (`ModelGkMatters`);
  Danger bloklarında feed kaleciyi isimle anar. Gol atfı mevki × bireysel güç ağırlıklıdır.
- **Yorgunluk:** oyuncu başına enerji; blok drenajı = `yorgunlukBlokDrenaj × tempoÇarpanı ×
  taktikTempoEtkisi` (kaleci ×`gkDrenajCarpan`). Bizim tempomuz rakibe `drenajRakipEtki` oranında yansır.
- **Olay zarları (skor zarına DOKUNMAZ):** sarı/kırmızı Domain.Referee, sakatlık Domain.Injury —
  ME Spec 11.2/12.2'nin blok ölçekli vekilleri. Bantlar maç TOPLAMI [KALİBRE-G `olay.*`]:
  sarı 2.0, direkt kırmızı 0.10, sakatlık 0.45 (yorgun takımda `sakatlikYorgunlukEtki` ile artar;
  kurban seçimi mevki ağırlıklı, sakatlıkta bireysel yorgunluk ağırlıklı). Agresif tempoda kart
  riski ×`kartTempoYukseltCarpan`; kartlı oyuncunun ikinci sarı seçilme ağırlığı ×`ikinciSariAgirlik`.
  Ölçekleme gerekçesi: 10 bloklu soyut maçta her olay bir KARAR taşımalı (GREYBOX_ONERI_IT11 A2).
- **Zorunlu karar anı:** bizim sakatlıkta akış DURUR (`HasPendingDecision`); `ResolveNext` kilitlenir.
  Çözüm yalnız Tek Kapı'dan: `model.substitution` (çıkan=sakat, giren=yedek; hak yakar) ya da
  `model.continue_short` (hak yakmaz; takım eksik kalır, bedeli Eksik etkeni öder). Skip bu
  beklemeyi ATLAYAMAZ. Rakip sakatlığı deterministik politikayla otomatik çözülür (aynı mevki yedek).
- **Değişiklik hakkı:** `squad.degisiklikHakki` 3 — hamle hakkından AYRI havuz (GDD 12.4 standardı;
  premium "Yedek Kulübesi Genişletmesi" kancası ileride 4-5 yapar). Giren oyuncu `tazeBacakEnerji`
  ile girer → takım enerjisi ve şerit görünür toparlanır. Kırmızı kartlı oyuncunun yeri DOLDURULAMAZ.
- **Gol atfı:** kozmetik (Domain.Crowd, skor çoktan Duel akışında belirlendi); forvet ağırlıklı
  `olay.golMevkiAgirlik` — feed "34' GOOOL! Yılmaz" der, istatistikte oyuncu golü sayılır.
- Kanıt bantları (harness 400 maç): sarı 2.08 · kırmızı 0.23 · sakatlık 0.64 · karar anı 0.31/maç;
  `ModelFatigueDrain`, `ModelFreshLegs`, `ModelIncidentDeterminism`, `ModelDecisionGate`,
  `BusSubstitution` (4 negatif + hak sınırı) testleri.

## FAZ 03 eşlemesi (ileriye dönük)

Bu kriterler ME Spec'in gerçek motoruna şu kanallardan bağlanacak: Güç→duello/xG girdileri,
Momentum→ME Spec momentum modülü, Taktik matrisi→taktik parametre setleri, Yorgunluk→ME 12.1
Stamina (Energy/M_kondisyon), Olay bantları→ME 11.2 kart + 12.2 sakatlık üretimi, Değişiklik→
CB katalog `match.substitution` (Tier 1) + ME 14.2 DEAD_BALL uygulaması, Form→moral.
Greybox modeli atılırken bu tablo FAZ 03 kalibrasyonuna taşınır.

## Sürüm
- v1 (2026-08-02): 4 etkenli örtük modelden 8 etkenli açık kriter modeline geçiş
  (tanh güç, matchup matrisi, faz eğrisi, skor durumu, ev, form) + oyun içi etken satırı.
- v2 (2026-08-07, İt.11 "Koçun Eli"): +Yorgunluk ve +Eksik etkenleri (10 etken); isimli kadro;
  kart/sakatlık olayları (Referee/Injury domain, skor zarı değişmedi); sakatlıkta zorunlu karar
  kilidi; `model.substitution`/`model.continue_short` komutları; DP'ye faz+enerji projeksiyonu.
- v3 (2026-08-07, İt.12 "Kadro Kimliği"): bireysel oyuncu gücü (ME 6.1 vekili) + mevki ağırlıklı
  HÜCUM/SAVUNMA kanalları; Güç etkeni reyting farkına bağlandı, Yorgunluk/Eksik reytingin içine
  taşındı (9 çarpan); kaleci savunmada en ağır mevki; DP reyting eğimi projeksiyonu; gol atfı güç
  ağırlıklı; şerit açıklama satırı. Kanıt: `ModelGkMatters`, `ModelStarLoss`, `ModelSquadGen`.
