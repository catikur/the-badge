# Persona Denetim Paneli — 1G Go/No-Go (Anayasa 9.7)
Girdi: GAME_THESIS.md + MARKET_CHECK.md + GDD v4.1 özeti. Personalar kör ve paralel koşuldu; sentez ana oturumda.

## P1 — Tür Veteranı Oyuncu
- [BLOCKER] Maç izlenebilirliği kanıtlanmadan tüm mimari buna yaslanıyor — feel testi planda motor inşasından (4-6 hafta) SONRA. Fix: placeholder greybox fun testi motor yatırımından ÖNCE.
- [MAJOR] Serbest pozisyonlama telefonda parmak hassasiyeti sorunu yaşar; snap/yakınlaştırma yardımları tasarlanmalı (tablet değil telefon birincil).
- [MAJOR] LLM konuşma veteran için yavaşlatıcı olabilir; Mod A tam hız paritesi ve FTUE'da Mod B dayatmaması şart.
- [MINOR] "Neden kaybettim" şeffaflığı (replay+log) rakip 1★ öfkesine karşı ana pazarlama kozu yapılmalı.

## P2 — F2P Ekonomi Uzmanı
- [BLOCKER] Source/sink haritasında sezonluk net arz bandı ve sink oranları tanımsızdı. Fix: 1,05-1,15 arz bandı + maaş sink payı tanımlandı (ECONOMY_MAP güncellendi).
- [MAJOR] P2PF geçici avantajlar public ligde "gizli güç" algısı riski — premium etkiler için maç önü şeffaf rozet değerlendirilmeli (bilgi ifşası trade-off'u ile birlikte). Karar FAZ 02 öncesi.
- [MAJOR] Fonksiyonel+kozmetik+pass üçlü SKU seti ilk 30 günde boğar; mağaza aşamalı açılım FTUE progressive disclosure ile hizalanmalı.
- [MINOR] Rewarded ads bilinçli non-goal — DECISIONS'a yazıldı; TR/LATAM'da v2 için veri toplansın.

## P3 — Şüpheci Pazar Analisti
- [BLOCKER] D1 %40 / D7 %20 kapı hedefi sektör P99 bandı (medyan D7 ~%4; top çeyrek %7-8). Yanlış "başarısızlık" sinyali üretir. Fix: kapı D1 ≥%30 / D7 ≥%8'e revize; %40/%20 aspirasyona taşındı.
- [MAJOR] Differentiation gerçek (FM Netflix kilidi + tycoon boşluğu) ama store'da GÖSTERİMİ zor: LLM metni preview satmaz → App Preview klip paylaşımı + stadyum büyüme timelapse'ine yaslanmalı.
- [MAJOR] OSM/Top Eleven ağ etkisi güçlü; arkadaş ligi daveti FTUE hafta-4'te geç — soft launch'ta erken davet deneyi koş.
- [MINOR] ASO ikili dil: "menajerlik" + "football tycoon" (düşük rekabetli açı).

## P4 — Hedef Kullanıcı (thesis'ten: 32y, FM'i zamansızlıktan bırakmış, akşam 20-30 dk)
- [MAJOR] 15 dk FTUE uzun; ilk interaktif değer anı ≤3 dk olmalı (bilet fiyatı etkileşimi öne alınabilir; ilk maç ~5. dk hedeflenmeli).
- [MAJOR] "Maç saatini kaçırma" kaygısı sürtünme; sabah replay/Panorama akışının FTUE'da açıkça vaat edilmesi gerekir (mekanik zaten var, İLETİŞİMİ eksik).
- [MINOR] Konuşmayı denemem ama butonlar hızlıysa kalırım — Mod A paritesi thesis'te net; koru.

## P5 — AI Güvenilirlik Denetçisi
- [BLOCKER] Golden set/eval ve eşik yoktu. Fix: docs/evals v0 + "skor <%85 merge yok" eşiği kondu; ilk set röportaj senaryolarıyla başlıyor.
- [MAJOR] Maliyet tavanı yoktu → 15K token/gün/kullanıcı + degrade zinciri balance JSON + DECISIONS'a işlendi; tüketim telemetri event'i ANALYTICS sözlüğü kurulunca eklenecek (backlog).
- [MAJOR] Hikaye Motoru halüsinasyon riski: beat üretimi yalnız verilen memory_facts'i kullanabilir kuralı + eval'de olgu-tutarlılık kontrolü şart.
- [MINOR] Prompt sürüm disiplini: docs/prompts değişikliği = eval koşusu (CLAUDE.md kuralına bağlandı).

## SENTEZ
| Bulgu | Şiddet | Karar |
|---|---|---|
| Fun kanıtı motor yatırımından önce yok | BLOCKER | ÇÖZÜLDÜ: FAZ 00.5 Greybox Fun Gate (DECISIONS) |
| Ekonomi arz bandı tanımsız | BLOCKER | ÇÖZÜLDÜ: ECONOMY_MAP v0 bantları |
| Retention kapısı gerçekdışı | BLOCKER | ÇÖZÜLDÜ: kapı %30/%8'e revize (GDD 18.3 bekleyen) |
| Eval/golden set + maliyet tavanı yok | BLOCKER | ÇÖZÜLDÜ: docs/evals + tavan |
| 6 MAJOR (rozet, SKU açılımı, preview dili, erken davet, FTUE ≤3dk değer anı, halüsinasyon guardrail) | MAJOR | Backlog: FAZ 02 tasarım girdileri + FTUE revizyonu |

**Kapı sonucu (Claude sentezi): GO önerisi.** Çözülmemiş blocker yok; nihai geçiş onayı Atilla'da (Anayasa Bölüm 1). Koşul: D4 kararının onayı.

**GÜNCELLEME:** Kapı geçişi ONAYLANDI (Atilla, 2026-07-30). D4 = ADR-001 Accepted.
