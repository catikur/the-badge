# TASK-002 — maç sunum ekranı, DoD-G kayıt kareleri (2026-09-06)

Unity 6000.3.21f1, Play modu, `Assets/Match/Scenes/MacSunum.unity`. Placeholder art (renkli
şekiller) — 5G-a'nın kapsamı bu; gerçek art 5G-b.

Kareler durağan; video değil. **Şeridin müdahalede oynadığı an bir kareden okunabilsin diye
ekran o bilgiyi YAZIYLA da taşıyor** (`son müdahale (tick N, AYNI TICK): ...`) — DoD-G'nin
istediği "şeridin taktik müdahalesinde oynadığı an görünmeli" maddesi böyle karşılandı.

| Dosya | Ne kanıtlıyor |
| --- | --- |
| `01-bus-reddi-ve-ayni-tick-serit.png` | Kabul ölçütlerinin çoğu tek karede: altta kırmızı **`BUS REDDİ — ParamOutOfBand · mentalite`** (sebep + hangi parametre, CB 11.1); şeridin altında **`son müdahale (tick 5442, AYNI TICK): G %41,0 → %47,3`**; spikerde sarı `28' DEP GOL!`; MENTALİTE `+2`; sayaç satırı (`duraklama 3 · uygulanan taktik 2 · motora iletilen 2 · bus reddi 3 · motor geç reddi 0`). |
| `02-mac-basi-dikey-saha.png` | Portre yerleşim, dikey saha + ceza sahaları (ev takımı YUKARI hücum eder), iki takım ayırt edilebilir (beyaz / mavi), top sarı, üç sonuçlu şerit, hız kontrolü `1x / 2x / ATLA`. |
| `03-mac-sonu-ekrani.png` | Maç `91:00` `FullTime`a kadar koştu; bitiş ekranı duraklama sayısı, uygulanan taktik değişikliği, iki red sayacı ve tohumu gösteriyor. Altındaki "BİR MAÇ DAHA" düğmesi yeni tohumla temiz başlatıyor (denendi: 20260906 → 20260907). |

**Karelerdeki sayılar hangi tohumdan:** `MacSunumEkrani.tohum = 20260906`. Aynı tohum + aynı
müdahaleler aynı maçı verir (`Kural6_AyniTohumAyniKomut_AyniMac`), yani bu kareler yeniden
üretilebilir.

**Bir uyarı:** `03`teki bitiş ekranı metni bu karelerden SONRA düzeltildi. Ekran o an
"duraklama: 17 (beklenen 8-12)" diyordu; 8-12 bir ORTALAMA ve tek maçta 4-17 sağlıklı
(24 maç ölçüldü: ortalama 11,04, aralık 4-17, boş maç %0). Bugünkü metin
"(ortalama 8-12; tek maçta 4-17 normal)". Gerekçe DECISIONS'ta.
