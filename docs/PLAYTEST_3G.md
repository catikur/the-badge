# PLAYTEST 3G — Greybox Fun Gate Sonuç Kaydı (FAZ 00.5)

> Doldurma sorumlusu: Atilla. Kaynak: `BRIEF_3G_GREYBOX.md` Fun Ölçüm Protokolü.
> Telemetri dosyaları cihazda `Application.persistentDataPath/telemetry/telemetry_*.jsonl`
> (iOS: Files app → The Badge Greybox; Editor: `~/Library/Application Support/TheBadge/The Badge Greybox/telemetry/`).
> Örnek satır formatı: `docs/samples/telemetry_ornek_oturum.jsonl`.

## Oturum kaydı (oyuncu başına bir satır)

| # | Oyuncu (takma ad) | Tarih | Süre (dk) | Maç sayısı | "Bir maç daha" dedi mi? | Erken skip/bırakma işareti | Not |
|---|---|---|---|---|---|---|---|
| 1 | 1 | 08.08.2026 | 15 | 3 | hayır | evet |  
| 2 | 2 | 08.08.2026 | 20 | 5 | evet | hayır | 
| 3 | 3 | 08.08.2026 | 16 | 4 | hayır | evet | 
| 4 | 4 | 08.08.2026 | 19 | 5 | evet | hayır | 
| 5 | 5 | 08.08.2026 | 17 | 4 | hayır | hayır | 

Kurallar: kişi başı ≥ 15 dk serbest oynama, YÖNLENDİRME YOK (soru sorarsa "nasıl istersen" de).
"Bir maç daha" sinyali = 15 dk dolduktan sonra kendi isteğiyle yeni maç başlatması VEYA bunu sözlü istemesi.

## Kapı metrikleri (Game Thesis RA#1)

| Metrik | Eşik | Ölçülen | Kaynak | Geçti? |
|---|---|---|---|---|
| "Bir maç daha" oranı | ≥ %60 (5 kişide ≥ 3) | **2/5 = %40** | gözlem tablosu | **HAYIR** |
| Sıkılma işareti (erken skip / bırakma) | < 3 / maç | gözlemde 2/5 oyuncuda işaret; telemetri paylaşılmadığından maç başı sayı yok | gözlem | veri kısmi |
| Oturum başına maç sayısı (referans) | — | ort. **4.2** (3-5 bandı) | oturum tablosu | — |
| "Sonraki Maç" tıklama oranı (referans) | — | telemetri paylaşılmadı | — | — |
| Maç başına izleme süresi (referans) | — | ~4.1 dk/maç (oturum süresinden kaba) | oturum tablosu | — |

## Mini mülakat (oyuncu başına, maks 5 dk)

| Oyuncu | 1) En keyifli an neydi? | 2) Ne zaman sıkıldın? | 3) Yarın kendi isteğinle açar mıydın? |
|---|---|---|---|
| 1 |  |  |  |
| 2 |  |  |  |
| 3 |  |  |  |
| 4 |  |  |  |
| 5 |  |  |  |

## Telemetri özeti

Log dosyalarını `docs/samples/` yanına kopyala (`playtest_<oyuncu>.jsonl`) ve buraya özetle:

| Oyuncu | Oturum | Maç | Ort. izleme sn/maç | Skip/maç | 2x | Müdahale/maç (`intervention`) | Değişiklik/maç (`substitution`) | Bilet değişti mi? |
|---|---|---|---|---|---|---|---|---|
|  |  |  |  |  |  |  |  |  |

## Serbest gözlem notları
- (Ekran karşısında ne yaptı? Nerede güldü, nerede telefonu bıraktı? Skip'i keşfetti mi?)
- Özellikle izle (İt.11-12): sakatlık KARAR PANELİ çıktığında tepkisi ne oldu? Oyuncu değişikliğini
  kendiliğinden keşfetti mi? Şerit oynadığında ("G %38→%45") yüzü değişti mi? Kadro güçlerine baktı mı?

## KAPI KARARI (revize semantik — DECISIONS 2026-08-07: playtest sonrası kapı HER DURUMDA kapanır, FAZ 03 başlar)

- [ ] **GO** — döngü tutuyor: Model Maçı sunumu (şerit + karar anları) FAZ 03 motorunun sunum katmanı olur
- [x] **NO-GO** — döngü tutmuyor: FAZ 03 yine başlar ama maç sunumu Dikey Dilim öncesi YENİDEN tasarlanır;
      neyin tutmadığı (blok yapısı? şerit? müdahale sığlığı?) mülakat verisiyle yazılır

Karar gerekçesi (3-5 cümle):

"Bir maç daha" oranı %40 (2/5) — %60 eşiğinin altında: kapı metriği GEÇMEDİ, kayıt NO-GO.
Sinyal yine de karışık: tutunan 2 oyuncu en uzun oturumları yaptı ve en çok maçı oynadı (5'er maç,
skip yok) — döngü bir oyuncu profilini tutuyor; 2 oyuncu aktif koptu (erken skip/bırakma), 1 nötr.
Mini mülakat ve telemetri kaydedilmediğinden KOPUŞ NEDENİ (blok yapısı mı, tempo mu, müdahale
sığlığı mı) bu turda yazılamıyor — sunum revizyonunun girdisi olarak Dikey Dilim öncesi küçük,
gözlemli bir doğrulama turu gerekir. Kapanış planı gereği (DECISIONS 2026-08-07) FAZ 03 motoru
başlar; Model Maçı sunumu olduğu gibi TAŞINMAZ, motor üstünde yeniden ele alınır.

> Mini mülakat tablosu boş bırakıldı — görüşmeler kaydedilmedi (dürüst kayıt; uydurma veri yok).
> Telemetri JSONL'leri repoya kopyalanmadı; ileride benzer turlarda rapor gücü için eklenmeli.

