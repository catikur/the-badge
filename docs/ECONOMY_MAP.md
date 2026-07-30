# Ekonomi Haritası v0 — Source/Sink (Anayasa 4G.2 zorunluluğu)

## Yumuşak para (₺K — kulüp kasası)
| SOURCES (üretim) | SINKS (tüketim) |
|---|---|
| Bilet + kombine | Oyuncu maaşları (haftalık, en büyük sink) |
| Büfe + yan ürün | Transfer bedelleri |
| Sponsorluk + yayın geliri | İnşaat + tesis bakımı |
| Maç ödülleri + lig ikramiyesi | Personel ücretleri (ücretsiz katman) |
| Başarımlar + pass ücretsiz şerit | Kredi faizi |

## Gerçek para hattı (IAP) — ₺K'ya DÖNÜŞMEZ
Premium personel süreleri · maç günü paketleri · hızlandırıcılar · kozmetik · sezon pası. Kural: **gerçek para doğrudan kasa parası satın alamaz** (Finans Direktörü gibi etkiler yalnız gelir ÇARPANI, geçici). Kozmetik tamamen ayrı şerit.

## Denge kuralları [KALİBRE]
- Sezon başına net arz bandı: source toplamı / sink toplamı = **1,05-1,15** (hafif pozitif; enflasyon kontrolü) — 10K sezon simülasyonuyla doğrulanır (ME Spec 17).
- Maaş sink'i toplam sink'in %45-60'ı; iflas eğrisi: bilinçli kötü yönetimde 2-3 sezonda tetiklenir.
- Online lig ekonomisi sezonluk yeniden dengelenir ama **oyuncu kazanımları silinmez** (Top Eleven anti-pattern'i).
- Premium çarpan tavanları: gelir etkisi ≤ %25, süre etkisi ≤ %50 — bantlar balance JSON'da.

## Açık işler
Tam sayısal simülasyon FAZ 04 balance sprintinde; bu harita o sprintin sözleşmesidir.
