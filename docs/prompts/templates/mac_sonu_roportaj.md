---
id: template.mac_sonu_roportaj
surum: 0.1
model: haiku
son_eval: bekliyor
golden_set: evals/golden/mac_sonu_roportaj.golden.jsonl
cache: redis / TTL 30g (GDD 11.6)
---
# Maç Sonu Röportaj Şablonu (taslak v0.1)
Girdi: MatchSummaryPacket (ME Spec 15.4). Çıktı: gazeteci personasıyla 1 soru.
Slotlar: {skor}, {one_cikan_olay}, {momentum_ozeti}, {aktif_ark_referansi?}
Kural: yalnız pakette olan olaylar; skoru doğru söyle; Türkçe.
