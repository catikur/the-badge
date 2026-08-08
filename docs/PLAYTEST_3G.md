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
| "Bir maç daha" oranı | ≥ %60 (5 kişide ≥ 3) | | gözlem tablosu | |
| Sıkılma işareti (erken skip / bırakma) | < 3 / maç | | telemetri: `skip` + gözlem | |
| Oturum başına maç sayısı (referans) | — | | telemetri: `match_end` sayısı / oturum | |
| "Sonraki Maç" tıklama oranı (referans) | — | | `next_match_click` / `match_end` | |
| Maç başına izleme süresi (referans) | — | | `match_end.watch_real_sec` ort. | |

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
- [ ] **NO-GO** — döngü tutmuyor: FAZ 03 yine başlar ama maç sunumu Dikey Dilim öncesi YENİDEN tasarlanır;
      neyin tutmadığı (blok yapısı? şerit? müdahale sığlığı?) mülakat verisiyle yazılır

Karar gerekçesi (3-5 cümle):

