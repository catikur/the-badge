# FAZ 03 Kapanış Planı — açık işlerin tamamı

Durum tarihi: 2026-08-10 · Taban: PR #10 sonrası `main` (62ca2e1) · Checks 62 PASS yeşil

Bu doküman FAZ 03'te AÇIK kalan her işi tek listede toplar, sıraya koyar ve her birine
**kabul kapısı** yazar. Kaynak: `docs/DECISIONS.md` borç kayıtları + `ME Spec 18.3` sprint
eşlemesi. Sıra keyfi değildir: önce ölçümü bozan model eksikleri, sonra kalibrasyon, sonra
FAZ 04 arayüz dondurması.

## Bugünkü taban (ME 17.2 karşılaştırması)

| Metrik | Hedef bant | Bugün | Durum |
| --- | --- | --- | --- |
| Pas isabeti | 78-86 | %80 | ✓ |
| Şut | 20-28 | 23,2 | ✓ |
| Faul | 18-28 | 27,7 | ✓ |
| Kart | 3,0-5,0 | 2,9-3,6 | ✓ |
| Ofsayt | 2-5 | 4,0 | ✓ |
| Sakatlık | 0,35-0,60 | 0,42 | ✓ |
| Bitiş enerjisi | 350-550 | 424 | ✓ |
| **Gol** | 2,4-3,0 | 3,3-3,7 | ✗ hafif üstünde |
| **Korner** | 8-12 | 7,0-8,3 | ✗ hafif altında |
| **Penaltı** | 0,20-0,35 | ~0 | ✗ |
| **Taç** | — | ~0-4 | ✗ düşük |

## Sıra

### M9 — Kontra atak ve geçiş modeli  *(en yüksek değer)*
İki borç muhafızı da aynı eksik mekanizmadan doğuyor: hat arkasına hızlı kontra yok.
- Geçiş anında (top kazanıldığı an) **doğrudan/dikey oyun penceresi**: kazanan takımın
  fayda ağırlıkları kısa süre ileri kayar; rakip henüz şekline dönmemiştir (geçiş penceresi
  M8-B'de eklendi, kullanılmıyor).
- Hat arkası boşluğun kontra sırasında **gerçekten değerli** olması: ara pas ulaşım yarışı
  geçiş penceresinde savunanın toparlanma gecikmesini görmeli.
- **Kapı:** `M7AttackRiskRegresyon` ×0,80 → **>1,00** (hücumun bedeli doğar) ve
  `M7DefendRegresyon` ×1,51 → **<1,00** (savunmak ödül verir). İkisi de sert kapıya döner.

### M10 — Duran top ve ceza sahası üretimi
Korner 7-8 (hedef 8-12), penaltı ~0 (hedef 0,20-0,35), taç düşük.
- Kanat/orta akışı: ceza sahasına ORTA (ME 10.2 zinciri var, besleme yok).
- Ceza sahası içi ihlal → penaltı üretimi (ME 11.2 + `cezaSahasiIhtiyatCarpan` dengesi).
- **Kapı:** korner 8-12, penaltı 0,15-0,40, taç > 8/maç.

### M11 — Gol bandı ve şut kalitesi ince ayarı
Gol 3,3-3,7 → 2,4-3,0; xG/şut gerçek ~0,10-0,13'e yaklaşmalı.
- **Kapı:** gol 2,4-3,2 · xG/şut ≤ 0,20 · isabetli şut 7-11.

### M12 — VAR dram sistemi (ME 11.4)
Saha kararı kalır oranı chaos seviyesine bağlı, 20-90 sn bekleme, karar iptali.
- **Kapı:** VAR olayı üretimi + geri alınan karar oranı bandı + determinizm.

### M13 — Hava ve zemin (ME 12.4)
Yağış/zemin → sürtünme, sekme, pas hatası, sakatlık çarpanları.
- **Kapı:** kuru/ıslak koşuda ölçülebilir ve BANTTA kalan fark.

### M14 — Maç sonu veri paketi + event log + highlight (ME 15.1/15.3/15.4)
LLM ve Panorama'nın girdisi; FAZ 04'ün beslendiği yer.
- **Kapı:** paket şeması + highlight puanı > 0,50 olan an sayısı bandı.

### M15 — LOD 1-2 türetme (ME 16.1) + sunucu throughput (16.3)
Ligdeki tüm maçların aynı anda koşabilmesi buna bağlı.
- **Kapı:** LOD 1/2 sonuç dağılımı LOD 0 ile istatistiksel uyum + CPU bütçesi (16.4).

### M16 — 10.000 maç kalibrasyon + chaos upset (ME 17.2/17.3)
Bugüne kadar 8-24 maçlık örneklerle çalıştık; spec 10k istiyor. 75v55 upset ve 65v65
beraberlik bandı (%22-30) doğrulaması burada.
- **Kapı:** 17.2 tablosunun TAMAMI + 17.3 toleransları.

### M17 — Golden replay seti (ME 17.4) + FAZ 04 arayüz dondurması (18.3)
- **Kapı:** replay dörtlüsü (seed + config_hash + komut zaman çizelgesi + sürüm) ile
  bit-eşit yeniden üretim; sim ↔ sunucu/Unity sözleşmesi dondurulur.

## FAZ 03 dışı ama açık (karar bekleyen — kod değil, senin kararın)

1. **Model Maçı sunumu**: greybox emekli; motor üstünde YENİDEN tasarlanacak ve küçük,
   mülakatlı bir turla doğrulanacak (5G Dikey Dilim öncesi borç).
2. **GDD v4.2 adayları**: (a) koşullu ön-emirler, (b) Oto-Koç (kural tabanlı, Tek Kapı,
   şeffaf rozet), (c) online/offline asimetri ilkesinin yazımı.
3. **BRIEF_3G_GREYBOX RA#1** metninin pivot sonrası revizyonu.
4. **Premium etkilerin public ligde şeffaf rozeti** (FAZ 02 öncesi tasarım kararı).

## Çalışma kuralı

Her dilim: plan → uygula → **Checks yeşil** → kanıt (sayılarla) → DECISIONS kaydı → PR.
Kapı geçmeyen dilim gönderilmez; geçmeyen kapı sessizce gevşetilmez, borç muhafızına
çevrilirse gerekçesi ve HEDEFİ ekrana basılır.
