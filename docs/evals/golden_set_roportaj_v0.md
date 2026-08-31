# Golden Set v0 — Maç Sonu Röportajı

**Durum (K9-D, 2026-08-30): makine-okunur set 24 örneğe çıkarıldı** →
`evals/golden/mac_sonu_roportaj.golden.jsonl` (docs/evals bandı 20-50 ✓). Her satırda `boyut`
alanı var (olgu · ton · yasak · uzunluk) ve `K9GoldenSetKapsami` dört boyutun da temsil
edilmesini zorunlu kılıyor.

Puanlayıcı: `TheBadge.World.EvalScorer` — `yasak` anahtarlarının HER BİRİ deterministik bir
dedektöre bağlıdır; tanınmayan anahtar kapıyı kırmızıya döndürür. Prose kalitesi ve üslup
İNCELİĞİ makineyle puanlanmaz, `InsanBakisi` listesine düşer (bkz. evals/golden/README:
"script + insan bakışı karışımı").

Kalite koşusu: `dotnet run --project shared/TheBadge.Sim.Checks -c Release -- eval-run <cevaplar.jsonl>`
— eşik %85 (`balance/llm.balance.json` → `eval.gecmeEsigiYuzde`, [KALİBRE]). Cevabı olmayan
golden satırı BAŞARISIZ sayılır; atlanırsa eksik üretim yüzdeyi yükseltirdi.

Aşağıdaki nitel liste, makine-okunur setin kaynağı ve insan bakışının referansıdır.
Format: GİRDİ (özet paket) → BEKLENEN NİTELİK
1. 3-0 galibiyet, hat-trick genç oyuncu → coşkulu ama ölçülü; genç övgüsü; kibir tetiklememe.
2. 90+5 penaltıyla 1-2 mağlubiyet, VAR kararı → hakem sorusu gelir; cevap şablonları öfke/sükunet/politik; kart riski imasız.
3. Derbi 0-0, düşük tempo → taraftar sıkıntısı sorusu; taktik savunusu seçeneği.
4. Başkana "ilk 4" sözü varken 7. sıra (memory_fact) → söz HATIRLATILIR; uydurma başka söz YOK.
5. Yıldızın transfer söylentisi arkı aktif → ark durumuna uygun iğneleyici soru; oyuncuyu satma vaadi ÜRETİLEMEZ.
6. 5 maçlık galibiyet serisi kırıldı → seri olgusu doğru anılır; moral yönetimi tonları.
7. Kırmızı kart + sakatlık aynı maçta → iki olay da anılır; sakatlık süresi UYDURULMAZ ("doktor raporu bekleniyor").
8. Kaydırılmış saat: kullanıcı maçı izlemedi, sabah özeti → "izlemedin" suçlaması YOK; özet-dostu ton.
