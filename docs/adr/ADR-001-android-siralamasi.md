# ADR-001: Android'in lansman sıralaması
**Durum:** Accepted (Atilla onayı, 2026-07-30) · **Tarih:** 2026-07-30
**Bağlam:** Anayasa v2.1/D4: oyunlarda iOS-first; Android ancak Aşama 9 büyüme kararı + ADR. TR pazarı Android ~%76; hedef kitle TR-ağır. GDD v4.1 ise Android+iOS gün-1 diyordu.
**Karar (öneri):** Soft launch yalnız iOS (tek platform QA + daha temiz D1/ödeme sinyali). **Android, Aşama 9'u beklemeden GLOBAL lansmanda (FAZ 08) eklenir** — anayasadan bilinçli sapma budur ve gerekçesi TR erişimidir. Unity portu marjinal maliyet.
**Sonuçlar:** FAZ 07 cihaz matrisi başlangıçta iOS'a daralır; Android cihaz testi FAZ 08 öncesi ayrı sprint; GDD 11.3/17/18 v4.2'de güncellenir.
**Alternatifler:** (a) Salt anayasa: Android Aşama 9 → TR erişimi gecikir. (b) Gün-1 çift platform: QA yükü + kirli soft launch sinyali.
