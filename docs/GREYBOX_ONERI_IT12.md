# GREYBOX ÖNERİ — İterasyon 12: "Kadro Kimliği" (KARAR BEKLİYOR)

Tarih: 2026-08-07 · Süreç: yazı → onay → kod (onaysız kod yok)
Tetikleyen: Atilla — "Kaleciler aşırı etkisiz, orada özellik olmalı. Oyuncuların özellikleri
olması gerekmiyor mu her takımda? Kazanma olasılığı neye göre?"

## 0. Tespitler (dürüst durum)

1. **Kaleci modelde yalnız kozmetik.** Vinyetteki kurtarışlar FlowSim'den gelir (2D sahne);
   MODEL tarafında — yani skoru ve şeridi belirleyen yerde — kalecinin HİÇBİR etkisi yok:
   olasılığa girmez, olaylardan muaf, değiştirilemez. Atilla'nın sezgisi doğru.
2. **Oyuncuların bireysel özelliği yok.** Takım gücü TEK sayı; kadrodaki isimler yalnız
   enerji/kart/sakatlık taşır. Sonuç: değişiklik kararı sadece "taze bacak" kararı — "yorgun
   yıldız mı, taze vasat mı?" ikilemi YOK; yıldızını kırmızıdan kaybetmenin bedeli, sıradan
   oyuncuyu kaybetmekle AYNI. Karar derinliği tezimiz için gerçek bir eksik.
3. Kazanma olasılığının hesabı belgeli ve ekranda (10 etken + kesin DP — `GREYBOX_MODEL.md`,
   blok kartı etken satırı, DETAY paneli) ama sahibe yeterince görünür ULAŞMIYOR — görünürlük
   iyileştirmesi bu pakete dahil edildi (aşağıda S1.6).

## 1. S1 — Önerilen kapsam: bireysel güç + Hücum/Savunma kanalları + kaleci etkisi

FAZ 03'ün tam nitelik tablosunu (ME Spec 6.1: Pace, Stamina, JumpReach…) greybox'a TAŞIMAYIZ —
onun vekilini kurarız: **oyuncu başına tek GÜÇ puanı** ve mevkiye dayalı iki takım reytingi.

1. **Bireysel güç (0-100):** takım tabanı ± yayılım [KALİBRE-G `squad.gucYayilim` ~8];
   deterministik üretim (Domain.Decision — dünya üretimi; isimler kozmetik kalır Domain.Crowd).
   Üretim sonrası ağırlıklı ortalama takım tabanına NORMALİZE edilir → mevcut gol bandı ve
   kalibrasyon bozulmaz (kanıt bandı korunur).
2. **İki kanal:** sahadaki 11'den mevki ağırlıklı, enerji çarpanlı:
   - `Hücum = Σ güç×enerji×wA(mevki) / Σ wA` — wA: FW 3, MF 2, DF 1, GK 0
   - `Savunma = Σ güç×enerji×wD(mevki) / Σ wD` — wD: **GK 3**, DF 3, MF 2, FW 1
   Güç etkeni artık `tanh((HücumBiz − SavunmaRakip)/ölçek)` (rakip için simetrik).
   Yorgunluk/Eksik etkenleri bu reytinglerin İÇİNE taşınır (enerji çarpanı + sahada olmayan
   katkı vermez) — etken satırı sadeleşir, çifte sayım olmaz.
3. **Kaleci artık ETKİLİ:** savunma reytingine en yüksek ağırlıkla girer — kötü kaleci rakip
   golünü belirgin artırır. Lezzet: Danger bloklarında feed "Kurtarış: {kaleci}!" der (kozmetik).
   (Kaleci değişikliği/olayları greybox'ta hâlâ yok — FAZ 03; panelde görünür, dokunulmaz.)
4. **Kararlar kalite kararı olur:** sub picker ve sakatlık panelinde güç gösterilir
   ("FV Bozkan 82 · %41" vs "FV Karsel 68 · %100"); kırmızı/sakatlıkta yıldız kaybı şeridi
   sıradan oyuncudan DAHA ÇOK oynatır (reytingden doğal düşer). Gol atfı güç ağırlıklı.
5. **UI:** maç öncesi kadroda güç puanları; koç masası kadro satırlarına güç sütunu.
6. **Şerit görünürlüğü:** kazanma şeridinin altına kalıcı tek satır: "şerit = kalan blokların
   kesin dağılımı · dokün: DETAY" — hesabın NE olduğu oyun içinde tek cümleyle söylenir.

**Kanıt bantları (kod onaydan sonra):** kalibrasyon <0.10 KORUNUR; gol bandı korunur; işaret
testleri: iyi kaleci → rakip p düşer; yıldız çıkınca reyting düşer > vasat çıkınca; normalize
determinizmi; tüm mevcut testler yeşil. + stub 2 yol 0/0 + Sim.Checks.

**Maliyet:** ~1 üretim günü. Timebox: FAZ 00.5'in 2 haftası 2026-08-13'te doluyor — S1 sığar
ama playtest kenara itilir; gerekirse bilinçli +3 gün uzatma DECISIONS'a yazılır (kapı atlanmaz,
kaydırma kayda geçer).

## 2. Alternatifler

| Seçenek | İçerik | Artı | Eksi |
|---|---|---|---|
| **S1** (önerim) | Tek güç puanı + 2 kanal + GK etkisi + görünürlük | Kararlar kaliteleşir; kaleci anlamlanır; kalibrasyon korunur; FAZ 03'ü taklit etmez | ~1 gün; playtest timebox kenarına |
| S2 | Mini nitelik seti (oyuncu başına Hücum/Savunma/Kondisyon) | Daha zengin profil | FAZ 03 6.1'i taklide başlar; greybox için fazla; süre 2-3 gün; kalibrasyon riski |
| S3 | Hiçbiri — playtest'e böyle çık | Timebox garanti | Sahibin güvenilirlik itirazı playtesterlarda da çıkar; değişiklik kararı sığ kalır; kaleci kozmetik kalır |

## 3. Karar sorusu (Atilla)
İterasyon 12 kapsamı: **S1** / S2 / S3?
