# GÖREV BRİFİ — 3G Greybox "Find the Fun" (FAZ 00.5)

**Kapı:** Anayasa v2.1 / 3G — en sert kapı. Bu kapı geçilmeden FAZ 02 (UI) ve FAZ 05 (asset üretimi) AÇILMAZ.
**Timebox:** 2 hafta, SERT. Uzatma yok; bitmeyen kapsam kesilir.
**Test edilen varsayım (Game Thesis RA#1):** "Deterministik 2D maç, mobilde İZLEMESİ keyifli hale getirilebilir."
**Branch:** `faz00-5/greybox` · Commit: Conventional Commits.

## Görev Bölüşümü
- **Claude Code:** Unity proje iskeleti + tüm kod + sahneler + telemetri + bu brifin DoD kanıtları.
- **Atilla:** Unity Hub'dan Unity 6 LTS kurulumu, Editor koşuları, cihaz build'i, 3-5 oyuncuyla playtest.

## KAPSAM (yapılacaklar)
1. **Unity projesi:** `unity/TheBadge` konumunda kur (bkz. `unity/UNITY_SETUP.md`); `com.thebadge.sim` local package bağlantısını doğrula.
2. **Greybox maç sahnesi (işin kalbi):** 2D top-down, SADECE placeholder (daireler = oyuncular, dikdörtgen = saha). Basitleştirilmiş akış simülasyonu — ME Spec'in tam motoru DEĞİL; amaç izlenebilirlik HİSSİ:
   - Top ve 22 nokta hareketi, hücum-savunma dalgalanması, şut/gol/korner anları
   - Hız kontrolleri: 1x / 2x / önemli ana atla (skip)
   - Gol anında basit "vurgu" (yavaşlatma + titreme) — highlight duygusunun testi
3. **Core loop kabuğu:** Maç öncesi (3 hazır taktik preseti + kadro listesi) → Maç → Maç sonu (skor + para ödülü) → **1 tycoon aksiyonu** (bilet fiyatı slider'ı → sonraki maç geliri değişsin) → "Sonraki Maç" butonu.
4. **Fun telemetrisi (yerel dosya logu yeter):** oturum başına maç sayısı, "Sonraki Maç" tıklama oranı, maç başına skip sayısı, maç başına izleme süresi.
5. **Determinizm:** Bu aşamada ŞART DEĞİL (his prototipi). `TheBadge.Sim.Rng` kullanmak serbest; tam determinizm borcu FAZ 03'ündür.

## KAPSAM DIŞI (dokunulmayacak)
- LLM/AI entegrasyonu, Nakama/online, gerçek Match Engine implementasyonu (ME Spec), UI Toolkit ekran seti, art/asset üretimi, monetizasyon kodu.
- `shared/TheBadge.Sim` içindeki mevcut sözleşmeler (Rng/Units/Commands imzaları): KULLAN ama DEĞİŞTİRME.
- `docs/` spesifikasyonları: değişiklik önerisi varsa DECISIONS.md'ye "Bekleyen" satırı.

## FUN ÖLÇÜM PROTOKOLÜ (Atilla koşar, kod hazırlar)
- 3-5 gerçek oyuncu, kişi başı ≥15 dk serbest oynama; yönlendirme yok.
- **Kapı metrikleri (Game Thesis):** "bir maç daha" oranı ≥ %60 · sıkılma işareti (erken skip / bırakma) < 3/maç.
- Mini mülakat (3 soru): En keyifli an neydi? Ne zaman sıkıldın? Yarın kendi isteğinle açar mıydın?
- Sonuçlar `docs/PLAYTEST_3G.md`'ye işlenir (telemetri özetiyle birlikte).

## DoD-G KANITLARI (görev bitti diyebilmek için)
- [ ] Unity konsolu temiz (0 error, 0 warning hedef)
- [ ] `dotnet run --project shared/TheBadge.Sim.Checks` YEŞİL (çekirdek bozulmadı kanıtı)
- [ ] 30-60 sn oynanış ekran kaydı (greybox maç + loop turu)
- [ ] Hedef cihazda akıcılık notu (greybox 60fps beklenir; sorun varsa profiler önce/sonra)
- [ ] Varsayım-risk raporu: neyi test ettik, neyi ETMEDİK, sıradaki riskler
- [ ] Telemetri log örneği + PLAYTEST_3G.md şablonu hazır

## KAPI KARARI
Playtest sonucu Atilla'ya sunulur → **GO** ise FAZ 02/03 kilidi açılır; **NO-GO** ise pivot oturumu (Anayasa 3G: fun yoksa para ve zaman gömülmez).

## Çalışma Akışı Hatırlatması
Önce `CLAUDE.md` + `docs/DECISIONS.md` oku → 3-6 maddelik plan sun → onay → küçük derlenebilir adımlar → her adımda kanıt göster. Belirsizlikte varsayım üretme; seçenek sun.
