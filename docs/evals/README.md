# Eval Disiplini (Anayasa 8.4)
- Her AI feature'ın golden set'i: 20-50 gerçekçi girdi + beklenen çıktı NİTELİĞİ.
- Prompt/model değişikliği = golden set koşusu; **skor < %85 → merge yok** (başlangıç eşiği [KALİBRE]).
- Kontrol boyutları: olgu tutarlılığı (memory_facts dışına çıkma YOK), ton uyumu, şema uyumu, TR dil kalitesi, uzunluk bandı.
- Üretimde sinyal: beğen/yeniden üret event'leri (PII'siz).
