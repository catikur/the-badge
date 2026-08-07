# GREYBOX ÖNERİ — İterasyon 11 aday paketi: "Koçun Eli" (KARAR BEKLİYOR)

Tarih: 2026-08-07 · Süreç: yazı → onay → kod (bu doküman YAZI adımıdır; Atilla onayı olmadan kod yok)
Tetikleyen: Atilla — "istatistik her koçun ihtiyacını barındırmalı; kart/sakatlık/yorgunluk yok;
online oyuncu değişiklik/mantalite ister; ön-ayarlı değişiklikler; oto-koç aylık satın alınabilir."

## 0. Durum tespiti: istenenlerin çoğu ZATEN tasarlı — icat etmiyoruz, ÖNE ÇEKİYORUZ

| Atilla'nın isteği | Bağlayıcı spec'te karşılığı |
|---|---|
| Yorulma | ME Spec 12.1 Stamina/Energy (M_kondisyon = 0.70 + 0.30×(E/1000)^0.7) |
| Hakem kart çıkarmıyor | ME Spec 11.2 foul/kart mantığı (bant 3.5-5.5 kart/maç, REFEREE domain) |
| Sakatlık yok | ME Spec 12.2 (bant 0.35-0.60/maç, INJURY domain, yorgunlukla çarpan bağı) |
| Oyuncu değişikliği | CB Spec katalog `match.substitution` (Tier 1) + ME Spec 14.2 (DEAD_BALL anı); GDD 12.4 standart 3 hak |
| Mantalite değişimi | CB Spec `squad.set_team_tactic {mentalite, tempo, pres, hat}` (Tier 0) + `match.motivation_talk` (3 ton, ME Spec 14.3) |
| Online canlı müdahale | GDD 6.x "maç saatinde online ise canlı izler ve müdahale eder" + ME Spec 14.4 canlı senkron |
| Koç istatistikleri | ME Spec 15.1 event log = "istatistik ekranlarının TEK kaynağı" (pas/şut/xG/kart/…); GDD oyuncu kartı expand (full stats, form eğrisi, sakatlık geçmişi) |

Yani nihai üründe bunların hepsi VAR. Karar sorusu şu: **hangileri fun ölçümünü güçlendirdiği için
greybox'a ŞİMDİ girer, hangileri GDD kararı olarak bekler?** Pivot tezimiz "karar ver → kazanma
ihtimali görünür değişsin → sonucu yaşa". Bugünkü müdahale alanı dar: 2 kol (taktik, tempo) × 3 hak.
Karar çeşitliliği tezin süsü değil KENDİSİ — ama timebox gerçeği: 2 haftalık FAZ 00.5'in 8 günü geçti.
Bu nedenle öneri: **tek ve SON içerik iterasyonu (Paket A), ardından his onayı + playtest.**

## 1. PAKET A — Greybox İterasyon 11: "Koçun Eli" (Model Maçı'na karar derinliği)

Hepsi [KALİBRE-G] (`greybox.balance.json` yeni `squad.*`/`event.*` anahtarları), hepsi Tek Kapı'dan,
tüm zarlar ayrı Rng domain akışlarından (skor zarı DEĞİŞMEZ — mevcut kalibrasyon korunur).

### A1 — Yorgunluk (modelin 9. etkeni)
- Takım enerjisi 1000'den başlar; her blokta drenaj [~70/blok], tempo moduna bağlı (yüksek tempo
  ×~1.35, kilitlen ×~0.8). Etki, ME Spec 12.1'in blok ölçekli vekili: `guc_eff = guc × (0.85 + 0.15×E/1000)`.
- Görünürlük: blok kartındaki etken satırına "yorgunluk ×0.96" eklenir; istatistik panelinde enerji çubuğu.
- Kazanım: tempo müdahalesi gerçek bir BEDEL kazanır (erken bastıran sonda çöker) — risk/ödül derinliği.

### A2 — Kart + sakatlık olayları (zorunlu karar anları)
- Blok sonucuna düşük olasılıklı olaylar eklenir: sarı kart, kırmızı kart, sakatlık. Bantlar greybox
  ölçeğinde [sarı ~1.5-2.5/maç toplam, kırmızı ~0.10/maç, sakatlık ~0.3-0.6/maç] — ME Spec bantları
  10 bloğa oranlanır; her olay bir karar taşısın diye kart sıklığı bilinçli seyreltilir (gerekçe: 10
  blokta 5 kart gürültü olur; [KALİBRE-G] zaten config_hash dışı).
- Kırmızı/sakatlık BLOĞU DURDURUR ve karar paneli açar: "Sakatlık: {oyuncu}. Değişiklik yap (hak 2/3)
  ya da eksik devam (güç −%8)". Karar sonrası G/B/M şeridi GÖZLE GÖRÜNÜR düşer/toparlanır — tezin
  vitrin anı. Sarı kart risk işaretidir: kartlı oyuncuyla agresif tempoda ikinci sarı riski artar.

### A3 — İsimli kadro + oyuncu değişikliği
- Hafif oyuncu katmanı: isimli 11 + 5 yedek (üretilen kurgusal isimler); mevki + güç katkısı +
  bireysel enerji/kart/sakatlık durumu. Model yine takım seviyesinde hesaplar; oyuncular katkı
  toplamı olarak girer (FAZ 03'ün tam bireysel modeli DEĞİL — vekil).
- Yeni komut `model.substitution` (çıkan, giren): **değişiklik hakkı 3, hamle hakkından AYRI**
  (GDD 12.4 standart 3'e hiza; premium "Yedek Kulübesi Genişletmesi" kancası ileride 4-5 yapar).
  Maç öncesi ekranda "değişebilirler" listesi işaretlenir (Atilla'nın ön-ayar fikrinin greybox hali).
- Taze bacak etkisi: giren oyuncu tam enerjiyle girer → takım enerjisi ve güç görünür toparlanır.
- Lezzet: gol atfı isimlere yapılır — feed "34' GOL! Yılmaz" (kozmetik atama, forvet ağırlıklı, ayrı domain).

### A4 — İstatistik paneli v2 ("koç masası")
- Takım karşılaştırma tablosu: skor, xG, tehlike, kart (S/K), sakatlık, ort. enerji, momentum zaman
  payı, kullanılan müdahale/değişiklik.
- Oyuncu satırları: isim · mevki · enerji çubuğu · kart/sakatlık ikonu · gol.
- Dürüst sınır: pas isabeti/şut haritası gibi TAM oyuncu istatistikleri greybox'ta üretilemez —
  bunlar FAZ 03 event log'undan (ME Spec 15.1) bedavaya gelir; greybox'ta taklit ÜRETMEYİZ.

### Kabul kanıtları (kod onaydan sonra; commit öncesi hepsi yeşil)
Harness bantları: kart/sakatlık maç başına bant içinde; yorgunluk monotonluğu (tempo↑ → enerji↓);
değişiklik etkisinin işareti doğru (taze bacak → P(gol) artışı); DP kalibrasyonu korunur (<0.10);
determinizm (aynı seed = aynı olay dizisi). + Sim.Checks + stub 2 yol 0/0 + EditMode aynaları.

### Kapsam alternatifleri
| Seçenek | İçerik | Artı | Eksi |
|---|---|---|---|
| **A tam** (önerim) | A1+A2+A3+A4 | Parçalar birbirine kilitli: yorgunluk değişikliğe amaç, kart/sakatlık zorunlu karar, istatistik görünürlük verir; playtest tezi tam güçle test eder | En büyük iterasyon (~1 gün üretim); timebox'ın kalanını yer |
| A minimum | A1+A3 (+A4'ün takım satırı) | Daha küçük; kart/sakatlık belirsizliği yok | Zorunlu karar anları (en güçlü tez vitrini) teste girmez |
| Önce playtest | İt.10 haliyle playtest, paket sonra | Baseline verisi; timebox garantisi | 3-5 gerçek oyuncu kıt kaynak — dar müdahale alanıyla harcanır; Atilla'nın his onayı zaten yok |

## 2. PAKET B — GDD v4.2 adayları (greybox DIŞI — bekleyen karar)

### B1 — Koşullu ön-emirler (ücretsiz katman; offline adaletinin temeli)
Maç öncesi 2-3 koşullu emir: "60' geride isek → tempo yükselt", "sakatlıkta {X} yerine {Y} girer",
"kırmızı görürsek → kilitlen". Artı: offline oyuncu cezasız, randevu baskısı düşer, USM ruhuna uygun
planlama derinliği. Eksi: fazla derinleşirse canlı olmanın değeri erir (sınır: emir sayısı ve koşul
sözlüğü dar tutulur [KALİBRE]).

### B2 — Oto-Koç (aylık kiralık; OTOMATİK YÜRÜTEN)
**GDD ile fark:** GDD 12.1 Taktik Analist premium'u yalnız ÖNERİR; Atilla'nın istediği oto-koç
YÜRÜTÜR — bu GDD 12'ye yeni madde ister (o yüzden karar). Tasarım çizgileri:
- Kural-tabanlı **deterministik** ajan, **LLM DEĞİL** (determinizm + maliyet + CB Spec K4 "öneri ≠
  yürütme" LLM sınırıyla çelişmez; oyuncu kiralarken açık yetki verir).
- Komutları herkes gibi Tek Kapı'dan geçer, replay/event log'da "oto-koç hamlesi" olarak görünür
  (ME Spec 14.5) — public ligde ŞEFFAF ROZET (mevcut bekleyen kararla birleşir).
- **Hiyerarşi ilkesi: canlı insan > oto-koç > hiç müdahale.** Oto-koç "iyi ama optimal altı" bantta
  oynar [KALİBRE] — aksi halde maça gelme motivasyonu ve randevu mekaniği (D1 sürücüsü) ölür.
- Fiyat GDD 12.7 hipotezine eklenir; D2 "P2PF geçici avantaj" sınıfına girer.

### B3 — Online/offline asimetri ilkesi (net yazım)
Canlı müdahale ÜCRETSİZ ve emek-bazlı avantajdır (oyunun randevu çekirdeği); offline default =
yalnız ön-emirler uygulanır, başka OTOMATİK müdahale yok; Oto-Koç bu boşluğun paralı köprüsüdür.
Not: istatistik ekranları için yeni karar GEREKMEZ — ME Spec 15.1 + GDD oyuncu kartı zaten kapsar.

## 3. Karar soruları (Atilla)
1. İterasyon 11 kapsamı: **A tam** / A minimum / önce playtest?
2. Paket B DECISIONS'a "bekleyen karar" olarak işlendi; GDD v4.2 turunda detaylandırılacak — itiraz var mı?
