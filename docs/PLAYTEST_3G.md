# PLAYTEST 3G — Greybox Fun Gate Sonuç Kaydı (FAZ 00.5)

> Doldurma sorumlusu: Atilla. Kaynak: `BRIEF_3G_GREYBOX.md` Fun Ölçüm Protokolü.
> Telemetri dosyaları cihazda `Application.persistentDataPath/telemetry/telemetry_*.jsonl`
> (iOS: Files app → The Badge Greybox; Editor: `~/Library/Application Support/TheBadge/The Badge Greybox/telemetry/`).
> Örnek satır formatı: `docs/samples/telemetry_ornek_oturum.jsonl`.

## Oturum kaydı (oyuncu başına bir satır)

| # | Oyuncu (takma ad) | Tarih | Süre (dk) | Maç sayısı | "Bir maç daha" dedi mi? | Erken skip/bırakma işareti | Not |
|---|---|---|---|---|---|---|---|
| 1 |  |  |  |  | evet / hayır |  |  |
| 2 |  |  |  |  | evet / hayır |  |  |
| 3 |  |  |  |  | evet / hayır |  |  |
| 4 |  |  |  |  | evet / hayır |  |  |
| 5 |  |  |  |  | evet / hayır |  |  |

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

| Oyuncu | Oturum | Maç | Ort. izleme sn/maç | Skip/maç | 2x kullanımı | Bilet fiyatı değiştirdi mi? |
|---|---|---|---|---|---|---|
|  |  |  |  |  |  |  |

## Serbest gözlem notları
- (Ekran karşısında ne yaptı? Nerede güldü, nerede telefonu bıraktı? Skip'i keşfetti mi?)

## KAPI KARARI

- [ ] **GO** — FAZ 02/03 kilidi açılır (DECISIONS.md'ye işle)
- [ ] **NO-GO** — pivot oturumu: mekanik değişir ya da proje düşer (Anayasa 4G.4; para/zaman gömülmez)

Karar gerekçesi (3-5 cümle):

