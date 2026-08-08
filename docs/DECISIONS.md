# Project Decisions: The Badge
Anayasa v2.1 uyumu — geriye dönük yazıldı (retrofit Bölüm 13.2): fiili durum + hedef + tetikleyiciler.

| Decision | Choice | Rationale | Date |
|---|---|---|---|
| D0 Product type | **game** | Menajerlik + tycoon mobil oyunu | 2026-07-30 |
| D1 Engine + Art | **Unity 6** + Scenario custom model & Midjourney (stil rehberi zorunlu, 4G.5) | Anayasa varsayılanı; deterministik C# çekirdek gereksinimi | 2026-07-30 |
| D2 Monetization | **F2P + IAP** (P2PF geçici avantaj + kozmetik + sezon pası) | Fiyat hipotezi GDD 12.7; source/sink: docs/ECONOMY_MAP.md; LiveOps yükü kabul; rewarded ads = bilinçli non-goal v1 | 2026-07-30 |
| D3 Servis kademesi | **G3** (sunucu-otoriter) | Tetikleyici birebir: rekabetçi bütünlük + çok oyunculu ligler; Nakama + .NET SimWorker | 2026-07-30 |
| D4 Platforms | **iOS-first soft launch; Android global lansmanda (FAZ 08)** — ADR-001 (Accepted) | Anayasa varsayılanı iOS-first; iOS D1 sinyali daha temiz (top çeyrek D1 %31-33 vs Android %25-27); TR Android %76 gerçeği için Android Aşama 9 yerine global lansmana çekildi = sapma → ADR-001 | 2026-07-30 |
| D5 AI features | **yes — cloud (Claude), sunucu proxy** | CB Spec Tek Kapı = proxy; golden set docs/evals/; maliyet tavanı 15K token/gün/kullanıcı + degrade (cache → Haiku → nazik sınır) | 2026-07-30 |
| D6 Mode | **solo + AI ajanları** | Claude Code tek ajan; CLAUDE.md | 2026-07-30 |

## Kapı ve süreç kararları (v2.1 uyum turu)
- **1G Go/No-Go:** Game Thesis + Market Check üretildi; Persona Paneli koşuldu (docs/PERSONA_PANEL_1G.md). Sentez: **GO — Atilla onayladı (2026-07-30).**
- **Roadmap yeniden sıralama (GDD v4.2'ye işlenecek):** FAZ 00.5 = **Greybox Fun Gate** (2 hafta sert timebox, 3-5 gerçek oyuncu). FAZ 02 (60+ ekran) ve FAZ 05 seri asset üretimi fun kapısının ARKASINA alındı (4G.10/19). FAZ 03 sonrası **5G Dikey Dilim kapısı** eklendi (tek maç günü final kalitede + sandbox IAP + cihazda fps).
- **Retention kapı hedefleri revize (panel B3):** Gate: D1 ≥ %30, D7 ≥ %8 (tür top-çeyrek bandı). Aspirasyon: D1 %40 / D7 %20 korunur ama kapı değildir. GDD 18.3 bekleyen v4.2.
- **Persona paneli:** Go/No-Go, Dikey Dilim ve Store Readiness kapılarında zorunlu (v2.1/9.7).

## FAZ 00.5 — 3G Greybox uygulama kararları (2026-07-30, Atilla plan onayıyla)
- **Ekran yönü:** portre + dikey saha (tek el mobil kullanım; tycoon ekranlarıyla aynı yön).
- **Greybox ayar dosyası:** `unity/.../Resources/greybox.balance.json` **[KALİBRE-G]**, config_hash DIŞI — `balance/sim.balance.json` korunur; Fun Gate sonrası prototiple emekli edilir.
- **UI:** kodla üretilen uGUI (UI Toolkit seti FAZ 02'de); sahne tek Bootstrap objesi, her şey runtime kurulur.
- **Tek Kapı (hafif):** gerçek `CommandEnvelope` + bant doğrulamalı `GreyboxCommandBus`; 4 kapılı tam doğrulama FAZ 04 borcu.
- **asmdef sapması:** FAZ 01'in 5 modüllü haritası yerine tek `Game.Greybox` (+EditModeTests) — greybox atılacak kod olduğundan.
- Teslim raporu: `docs/GREYBOX_3G_RAPOR.md` · Playtest formu: `docs/PLAYTEST_3G.md` · **Kapı kararı playtest sonrasına bekliyor.**

## FAZ 00.5 — Fun Gate PİVOTU: "Model Maçı" (2026-08-02, Atilla kararı)
- **Gerekçe:** 7 iterasyonluk 2D fiziksel izlenebilirlik cilası, tezin özüne hizmet etmiyordu.
  Oyunun DNA'sı (Game Thesis + USM 98 mirası) "kararın sonucu görünür şekilde değiştirmesi".
- **RA#1 revizyonu:** ~~"Deterministik 2D maç mobilde izlemesi keyifli hale getirilebilir"~~ →
  **"Model + görünür olasılıklar + müdahale döngüsü ('karar ver → kazanma ihtimali değişsin →
  sonucu yaşa') 'bir maç daha' isteği yaratır."** Kapı metriği değişmez (≥ %60 bir-maç-daha).
- **Deneyim:** maç 8-12 aksiyon bloğu; blok olasılıkları ÖNCE gösterilir, sonra zar döner;
  canlı G/B/M kazanma şeridi; blok aralarında müdahale (taktik/tempo) → olasılıklar yeniden
  hesaplanır. 2D motor gol/kritik anlarda highlight VİNYETİ olarak yaşar (Atilla seçimi).
- FAZ 03 hizası: ME Spec zaten model-önce mimaridir; pivot greybox'ı gerçek motora yaklaştırır.
- Brif RA#1 metni v4.2 güncellemesinde revize edilecek (bekleyen).

## FAZ 00.5 — Fun Gate KAPANIŞ PLANI (2026-08-07, Atilla kararı)
- **Tespit (sahip + uygulayıcı mutabakatı):** Son 3 iterasyon FAZ 03'te zaten tasarlanmış sistemlerin
  (istatistik, kart/sakatlık, yorgunluk, oyuncu gücü) greybox'a ilkel kopyalarını taşıdı. "FM gibi
  hissettiriyor mu" sorusu greybox'ın DEĞİL Dikey Dilim'in (5G) sorusudur; menajerlik fantezisinin
  asıl katmanı (kadro/antrenman/transfer) maçlar ARASINDA yaşar ve FAZ 04'ün işidir.
- **Karar:** Mevcut greybox'la (İt.12 hali) 3-5 kişilik playtest yapılır → `PLAYTEST_3G.md` doldurulur
  → **sonuç ne olursa olsun Fun Gate kapanır ve FAZ 03 (gerçek motor + ME Spec 6.1 tam nitelik
  sistemi, kaleci nitelikleri dahil) başlar.** GO = Model Maçı sunumu (şerit + karar anları) motorun
  sunum katmanına taşınır; NO-GO = motor yine başlar, maç SUNUMU Dikey Dilim öncesi yeniden tasarlanır
  (4G.4'ün "mekanik değişir" kolu sunum katmanına uygulanır — bilinçli yorum, bu satır kaydıdır).
- **Greybox içerik DONDU:** yeni özellik iterasyonu yok; yalnız playtest'i engelleyen hata düzeltilir.
- Fun doğrulamasının nihai yükü 5G Dikey Dilim'e taşındı (persona paneli + gerçek kalite orada zorunlu).

## FAZ 00.5 — FUN GATE KAPANDI (2026-08-08)
- **Playtest sonucu (5 kişi, `PLAYTEST_3G.md`):** "bir maç daha" 2/5 = **%40** < %60 eşik → **NO-GO**.
  Sinyal karışık: tutunan 2 oyuncu en uzun oturum + en çok maç (5'er, skip'siz); 2 aktif kopuş; 1 nötr.
  Mülakat/telemetri kaydedilmediği için kopuş NEDENİ verisiz — bilinen sınır olarak kayıtlı.
- **Uygulama (kapanış planı gereği):** FAZ 03 Match Engine BAŞLAR (`docs/briefs/BRIEF_FAZ03_ACILIS.md`).
  Model Maçı SUNUMU motora olduğu gibi taşınmaz; Dikey Dilim (5G) öncesi motor üstünde yeniden
  tasarlanır + küçük, MÜLAKATLI gözlem turuyla doğrulanır (borç kaydı). Greybox prototipi emekli;
  `greybox.balance.json` kalibrasyon değerleri [KALİBRE] aday listesi olarak brife taşındı.

## FAZ 03 BAŞLADI (2026-08-08, Atilla: "faz03'e başla")
- Dal: `faz03/match-engine` (CLAUDE.md faz-modül dal modeli). Brif: `docs/briefs/BRIEF_FAZ03_ACILIS.md`.
- M0 (motor iskeleti) uygulandı: tick pipeline (ME 4.2, 6 sabit aşama) + int-mm durum şemaları
  (ME 5.2-5.3) + tick-damgalı CommandQueue (ME 14.1) + xxHash64 checksum kadansı (ME 3.2) +
  Checks'e 5 yeni determinizm kapısı (golden 0x8954F2FA14EC7BFA sabit).
- M1 (nitelik + TeamSheet) uygulandı (2026-08-08, plan onayı Atilla): ME 6.1 tam nitelik seti
  (26 nitelik, kaleci 6'lısı dahil) + TeamSheet/MatchConfig + A_eff (ME 6.2) — Math.Pow determinizm
  riski Q16 LUT kuantalamasıyla kapatıldı; [KALİBRE] katsayılar `balance/sim.balance.json → attribute`
  bölümüne eklendi (config_hash İÇİ şema ekleme — sezon ÖNCESİ, ME 3.3 uyumlu); Checks +7 kapı
  (A_eff vektör golden 70/54).

- M2 (karar/hareket çekirdeği + Motor Test Ekranı) uygulandı (2026-08-08, onay Atilla — Unity
  görünürlük köprüsü dahil): kademeli utility karar (ME 7.2 indirgenmiş aday seti + Vision kısıtı),
  topsuz anchor-omurga + pres tetiği (7.4/7.6 alt kümesi), sahiplik/kontrol/tackle düelloları
  (4.3+6.3-6.4), pas nişan hatası (6.5 çekirdeği), kinematik + top fiziği (8.1-8.3), TrigLut (3.2);
  [KALİBRE] yeni bölümler: possession/utility/offball/xt (ayrıştırılabilir 12×8) + pass/physics
  ekleri. Bilinçli M2 sınırları: şut/gol/kaleci M3; taç/aut tek adım restart; foul üretimi M-hakem.
  Golden'lar yeniden sabitlendi (motor 0x2F1B33BD03085FD1, 10dk maç 0x39F2F1E717FED332); 10 dk
  koşuda 339 pas (%59), 321 tackle, 56 taç/aut. Unity: EngineDev.unity motor test sahnesi.

- M3 (kaleci + şut/xG) uygulandı (2026-08-08, onay Atilla): şut kararı (rasyonel tehdit vekili —
  ln/atan karar yolunda yasak) + 9.2 ANALİTİK kurtarış (t_react/Reflexes, erişim/Agility, lojistik
  Q16-kuantalı) sonucu topun gerçek uçuşuyla sahnelenir (BallState.Flight: gol yolu/tutuş);
  Handling tut-çeldi; 9.1 kaleci pozisyonlaması; gol+santra restart'ı; xG KAYIT modeli 15.2 birebir
  (yalnız kayıt). [KALİBRE] gk/shotExec ekleri + ayar turu (dalisSure 0.45, sigma 1.6, tehdit 2.2).
  Kanıt: 90dk 5-6 gol · 23 şut · 16 kurtarış · ΣxG 9.5; GkMatters 5→1; 32 kapı YEŞİL; golden'lar
  yeniden sabit. MOTOR ARTIK TAM MAÇ OYNUYOR (duran top/hakem/durum modeli sonraki dilimler).

- M4 (duran toplar + hakem/kart + maç saati) uygulandı (2026-08-08, plan onayı Atilla):
  ME 11.2 foul/şiddet skoru + kart + avantaj (RefereeProfile: Strictness/AdvantageTendency/
  Consistency, HomeBias YOK), ME 10 duran toplar (korner ortası + hava topu düellosu, taç,
  kale vuruşu, frikik, ME 10.4 penaltı matrisi), ME 10.5 ofsayt (7.4 kısıtı + pas anı ihlali),
  ME 3.4 maç saati: 45+45 devre + duraklama birikimli uzatma → maç FullTime'da KENDİ KENDİNE
  biter (MatchResult).
- **M4 kalibrasyon turu (ME 17.2 gereği "bant dışı metrik = [KALİBRE] güncellemesi"):** ilk koşu
  maç başına 22-26 gol / 2000 müdahale / 0 korner veriyordu. Kök nedenler kodda düzeltildi:
  (1) duran top bayrağı yalnız atanan kullanıcıda temizleniyordu → korner dizilişi sonsuza
  donuyordu; (2) çelinen/bloklanan top ŞUTÇUNUN ayağının dibinde kalıyordu → rebound çorbası
  (şut/gol enflasyonu, korner üretimi sıfır); (3) her presçi ayrı ayrı dalıyordu → müdahale
  sıklığı gerçek dışı; (4) savunma alan kapatmıyordu (ME 7.4-B blok vektörü eklendi);
  (5) dribling bedelsizdi (ME 6.4 dribling düellosu eklendi); (6) şut nişanı mutlak metre
  sapmalıydı → açısal yapıldı; (7) kalecinin OneOnOne niteliği hiç kullanılmıyordu (ME 9.3
  yakın mesafe kapatma eklendi); (8) Checks test kadrosunda niteliklerin yarısı 0'dı — eksik
  nitelik alt sistemi sessizce öldürüyor (tüm nitelikler dolduruldu).
  Sonuç bantları (12 maç ort.): **gol 3,17 · şut 18,5 · kurtarış 6,3 · korner 4,7 · faul 14,7 ·
  kart 4,75 · süre 91,4 dk** — gol ve kart ME 17.2 bandında; şut/faul bandın hafif altında.
- **M5 borcu (kayıt):** taç üretimi ~0 (kanat oyunu/genişlik vektörü yok), ofsayt düdüğü ~0
  (ara pas yok), penaltı ~0,04 (bant ~0,25); kök neden ortak: ME 7.5 markaj çözücüsü + 7.4
  boşluk/genişlik vektörleri henüz yok. M5 dilimi bunları ve durum modelini (12.x stamina/
  sakatlık/moral) kapsayacak.

- M5 (durum modeli + alan kontrolü) uygulandı (2026-08-08, plan onayı Atilla):
  **ME 12.1 Stamina** — ΔE = k_e×(v/v_max)^2,2×M_workrate + pres eki (üs LUT'la kuantalı, Math.Pow
  sıcak yolda yasak), ölü topta +2/sn, devre arası +150, Energy<250'de DECISION sigma +%20,
  sprint sayacı; **ME 12.2 Sakatlık** — sert müdahale (11.3 bağlantısı) ve yorgunken sprint
  tetikleri, M_yorgunluk formülü, 4 kademeli şiddet (Hafif sahada kalır ve nitelikleri −5 düşer,
  üstü sahayı terk eder → takım eksik oynar); **ME 12.3 Momentum** — gol ±4, dakikada 0,3 sönüm,
  karar sigmasına ±%15, A_eff'e M_moral, kritik dakika baskısı (dk>80 & fark≤1) şut nişanına
  (Composure %60'ını söndürür); **ME 7.5 Markaj çözücüsü** — sahiplik değişiminde xT+Pace tehdit
  skoruyla greedy atama; **ME 7.4-A genişlik** — kanat rolleri touchline'a açılır; **ME 7.2 ara pas
  + 10.5 ofsayt** — boşluğa pas adayı ve koşu zamanlama hatası düdüğü.
  Ayrıca **seyir yoğunluğu** eklendi (topa/göreve uzakken jog): 22 ajanın 90 dakika v_max'ta
  koşması hem stamina hem sprint sayacını gerçek dışı yapıyordu.
- **M5 kalibrasyon (12-24 maç ort.):** gol 2,7-3,7 · şut 21 · kart 4,9-5,5 · korner 4,6 ·
  **bitiş enerji 432-482 (ME 12.1 hedefi 350-550 ✓)** · **sakatlık 0,42 (bant 0,35-0,60 ✓)** ·
  **ofsayt 3,2-4,2 (bant 2-5 ✓)** · taç 1,5 (hâlâ düşük) · kurtarış %64.
- **M6 borcu:** (a) müdahale katmanı motora bağlanmadı (taktik/değişiklik/motivasyon komutları —
  ME 14.2 uygulama anları; oyuncu değişikliği olmadığı için sakatlanan oyuncunun yeri boş kalıyor);
  (b) hareket modelinde jog/yürüyüş kademesi kaba — mutlak sprint sayısı gerçek futbolun ~5 katı;
  (c) taç üretimi düşük; (d) VAR (11.4), hava/zemin (12.4), LOD (16.1) hiç başlamadı.

## Bekleyen kararlar
- Premium etkilerin public ligde şeffaf rozeti (panel M-bulgusu) → tasarım kararı, FAZ 02 öncesi.
- ~~3G Greybox Fun Gate GO/NO-GO~~ → **KAPANDI (2026-08-08): NO-GO %40** — uygulama yukarıdaki kapanış bölümünde; sunum revizyonu + mülakatlı doğrulama turu Dikey Dilim öncesi BORÇ.
- BRIEF_3G_GREYBOX RA#1 metninin pivot sonrası revizyonu (GDD v4.2 turu).
- ~~Greybox iterasyon 11 kapsamı~~ → **KARAR (2026-08-07, Atilla): Paket A TAM** — yorgunluk + kart/sakatlık zorunlu karar anları + isimli kadro/değişiklik + koç masası greybox'a girdi (İt.11, GREYBOX_MODEL.md v2). Bu SON içerik iterasyonudur; sırada his onayı + playtest.
- ~~Greybox iterasyon 12 kapsamı~~ → **KARAR (2026-08-07, Atilla): S1 — Kadro Kimliği** uygulandı: bireysel oyuncu gücü + mevki ağırlıklı Hücum/Savunma kanalları (kaleci savunmada en ağır) + şerit görünürlük satırı (İt.12, GREYBOX_MODEL.md v3). Timebox: playtest 2 haftalık kutunun kenarında — kayarsa bilinçli uzatma bu satıra işlenir.
- **GDD v4.2 adayları (Atilla fikri, 2026-08-07):** (a) koşullu ön-emirler (ücretsiz katman, offline adaleti); (b) **Oto-Koç** — aylık kiralık OTOMATİK yürüten ajan: GDD 12.1 Taktik Analist "önerir"den sapma, kural-tabanlı deterministik (LLM değil), Tek Kapı + replay izi + public ligde şeffaf rozet + "canlı insan > oto-koç" optimal-altı bant ilkesi; (c) online/offline asimetri ilkesinin net yazımı. Öneri: `docs/GREYBOX_ONERI_IT11.md` §2.

## Karar günlüğü (tarihsel özet)
| Sürüm | Özet |
|---|---|
| v1.0-v3.0 | 4 modül → Chaos+P2PF → AI-First pipeline |
| v4.0 | Kurgusal evren, Hikaye Motoru, Replay/Panorama, Kozmetik+Pass, FTUE, güvenlik |
| ME/CB Spec v1.0 | LOD mimarisi, sayaç-RNG; Tek Kapı, 32 aksiyon, Tier 0-2 |
| v4.1 | Claude Code; C# sim servisi + Nakama; 2.5D prerender; stack ~69$/ay |
| **v2.1 uyum turu (bu doküman)** | D0-D6 retro; greybox & dikey dilim kapıları; retention gate revizesi; golden set + maliyet tavanı; iOS-first önerisi |

Deviations from the constitution require an ADR in docs/adr/.

## İsim Kararı (2026-07-30)
Proje adı: **The Badge** — store: başlık "The Badge" + alt başlık "Football Club Tycoon". Eski çalışma adı "USM Reborn" son IP bağı nedeniyle emekli edildi. Elenen adaylar (tarama kanıtlarıyla): Goalmine (aynı adlı tahmin oyunu + casino), Touchline (2 koç uygulaması + fantasy + bahis), Gaffer (aynı türde aktif menajerlik oyunu), Goalvault (çekiliş bitişikliği), Goalmint (temizdi, tercih edilmedi). "The Badge" taraması: birebir isimde ürün yok; "badge" kelimesi yalnız logo-quiz alt türünde yoğun — alt başlık tür ayrımını yapar. Resmi marka taraması 8G'de.
