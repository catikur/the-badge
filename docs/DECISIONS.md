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
- **M6 borcu (M5 sonunda yazıldı):** (a) müdahale katmanı motora bağlanmadı (taktik/değişiklik/
  motivasyon komutları — ME 14.2 uygulama anları; oyuncu değişikliği olmadığı için sakatlanan
  oyuncunun yeri boş kalıyor); (b) hareket modelinde jog/yürüyüş kademesi kaba — mutlak sprint
  sayısı gerçek futbolun ~5 katı; (c) taç üretimi düşük; (d) VAR (11.4), hava/zemin (12.4),
  LOD (16.1) hiç başlamadı.

- M6 (müdahale katmanı) uygulandı (2026-08-09, plan onayı Atilla):
  **ME 14.1-14.3 — menajerin maç içi elleri.** `CommandQueue.ISink` ile motor kuyruğa kendini
  tanıtır; komutun SIRASI kuyruğun, DAVRANIŞI motorun işidir. Uygulanan anlar: taktik deltası
  anında (bant doğrulaması -2..+2, dışı REDDEDİLİR), oyuncu değişikliği yalnız **ölü topta**
  (giren oyuncu tam enerjiyle, kart sicili sıfır), motivasyon konuşması anında + **10 dk bekleme**
  (Ateşle +momentum, Sakinleştir negatifi söndürür, Uyar 10 dk karar sigmasını %10 düşürür;
  etki çarpanı kaptan Leadership vekili × skor bağlamı yerindeliği). Taktik kolları GERÇEKTEN
  işler: mentalite fayda ağırlıklarını (wThreat/wRisk) kaydırır ve topsuz şekli yukarı taşır,
  tempo "topu tut"u cezalandırır, pres presçi sayısını (1-4) belirler, hat savunma derinliğini
  kaydırır. **AutoManage** (offline adaleti): canlı kullanıcı yokken sakatlanan oyuncunun yeri
  motor tarafından doldurulur — kural seti bilinçli olarak DAR (Oto-Koç ürün kararı hâlâ bekliyor).
- **M6'da yakalanan hata — sessiz komut kaybı:** bekleyen değişiklik takım başına TEK slottaydı;
  ölü top gelmeden ikinci komut geldiğinde birincisi sessizce eziliyordu (kapıdan geçmiş komut
  yok oluyordu). Düzeltme: takım başına MaxSubs kadar bekleme kuyruğu (gerçek maçtaki çoklu
  değişiklik tek durakta) + hak hesabına bekleyenlerin dahil edilmesi. Artık her komut ya uygulanır
  ya REDDEDİLİR; sayaç ekranda. Durum şeması değiştiği için M0/M2/M4 golden'ları yeniden sabitlendi.
- **M6 kalibrasyon krizi ve çözümü (ME 17.2):** taktik ilk sürümde ileri aksiyonlara SABİT bonus
  veriyordu (`mentaliteIleriBias`). Sonuç: mentalite +2 → maç başına **15 gol / 73 şut**. Sabit
  bonus, fayda skorunun baskın terimiyle aynı büyüklükte olduğu için tüm kararları tek yöne
  kilitliyordu. Model çarpımsala çevrildi (`mentaliteTehditCarpan`, `mentaliteRiskTolerans`):
  kötü aksiyon ofansif kurulumda da kötü kalır. Aynı turda ikinci kök neden bulundu: **ara pas
  bedavaydı** — hat arkası boşluk hedefe kimin önce varacağı hesaba katılmadan tehdit sayılıyordu
  (maç başına 24-88 ara pas). ME 7.6 alan kontrolü eklendi: hedef noktaya koşucu mu, en yakın
  savunan mı (KALECİ dahil) önce varır (`araPasUlasimBandiM`); ulaşılamayan top tehdit değildir.
  Ara pas 32,8 → **9,3/maç** (gerçekçi bant), ofsayt 2,5 (bant 2-5 ✓). Kapı da sertleştirildi:
  M6TacticEffect artık 6 tohumda ORTALAMA alır ve **üst sınır** denetler (etki var ama patlama yok).
- **M6 kalibrasyon sonuçları (12 maç ort.):** gol 3,00 · şut 13,3 · korner 2,1 · faul 11,1 ·
  kart 3,00 · ara pas 9,3 · ofsayt 2,5 · sakatlık 0,92 · bitiş enerji 442 · süre 91,3 dk.
  Taktik yanıtı (6 tohum ort., tüm kollar ofansif): ileri üretim 36,3 → 70,3.
- **M7 borcu (M6'da ölçülerek yazıldı):** (a) **defansif mentalite ödüllendirmiyor** — aynı kadro
  ayna maçta (24 maç) mentalite −2 kendi xG'sini 5,52→2,34 düşürürken yediği xG'yi 3,76→5,14
  ARTIRIYOR; kök neden derin blokta alan yoğunluğu yok (ME 7.6 tam alan kontrolü + kontra
  geçişleri). Bu haliyle "hep hücum" baskın strateji riski taşır — 5G Dikey Dilim öncesi
  kapatılmalı; (b) korner 2,1/maç (gerçek ~10) ve taç üretimi hâlâ düşük — kanat/orta akışı zayıf;
  (c) sprint sayacı ~7000/maç (gerçeğin ~5 katı) — jog/yürüyüş kademesi hâlâ kaba; (d) ΣxG şut
  başına yüksek — xG modeli ile şut kalitesi dağılımı birlikte kalibre edilmeli; (e) VAR (11.4),
  hava/zemin (12.4), LOD (16.1), maç sonu veri paketi (15.4) hiç başlamadı.

- M7 (savunma hattı + taktik dengesi) uygulandı (2026-08-09, plan onayı Atilla):
  **ME 7.6 hat formülü BİREBİR uygulandı** — `hat_x = taban(talimat) + 0,35 × (top_x − saha_ortası)`.
  Eski "kaleye oran" biçimi (hat = kale + oran×(top−kale)) top geri gelince savunma hattını kale
  çizgisine YAPIŞTIRIYOR, ofsayt çizgisi de onunla çöküyor, rakip forvet altı pasa kadar kamp
  kurabiliyordu — M6'da yazılan "defansif mentalite ödüllendirmiyor" borcunun kök nedeni buydu.
  Ayrıca: **kesme ve kanal kapama** (ME 7.4-B) — birinci presçi taşıyıcının/topun öngörü noktasına,
  ikincisi topun KENDİ KALESİ tarafına iner; **serbest topta buluşma noktası** (ME 6.5/8.2:
  yuvarlanma yavaşlaması altında topun t saniye sonraki yeri); **buluşma noktalı pas** — top
  uçarken alıcı yerinde durmadığı için pas alıcının GELECEK konumuna atılır; **fiziksel pas kesme
  riski** — koridordaki rakip SAYMAK yerine "top mu çizgiye önce varır" zaman yarışı;
  **rasyonel şut mesafe çekirdeği** 1/(1+(d/d0)²) — doğrusal biçim 14 m ötesini tümüyle eliyordu.
  Teşhis sayaçları eklendi (hash dışı, ME 15.4 paketinin çekirdeği): şut mesafesi, şut anındaki
  baskı, xG'ye giren şut sayısı.
- **M7 kalibrasyon (12 maç ort.):** gol 3,75 · şut 29,6 · **korner 9,7 (ME 17.2 bandı 8-12 ✓, M6'da
  2,1'di)** · faul 12,7 · kart 4,17 · ofsayt 2,4 ✓ · sakatlık 0,25 · ara pas 6,7 · bitiş enerji 422.
  Hat formülü korner ve faul üretimini kendiliğinden banda taşıdı — kanat/duran top borcunun bir
  kısmı savunma geometrisi hatasıymış.
- **Yeni kapı — M7 taktik dengesi (ayna kadro, 10 maç):** nitelikleri BİREBİR aynı iki takım,
  tek değişken taktik. Sonuç: nötr 6,83/5,11 · ofansif 9,33/5,99 · defansif 3,80/10,44.
  `M7MirrorSymmetry` (taraf yanlılığı yok, %29) ve `M7AttackTradeoff` (+%37 üretim / +%17 risk)
  YEŞİL: hücum artık bedelsiz değil.
- **M8 BAŞLIĞI — alım/sahiplik modeli (ölçülerek yazıldı, Dikey Dilim öncesi kapatılmalı):**
  `M7DefendRegresyon` kapısı bugünkü gerçeği kilitliyor ama HEDEFİ tutmuyor: defansif kurulum
  yediği xG'yi ×2,04 ARTIRIYOR (hedef <1,00). Kök neden zinciri ölçüldü: **pas isabeti %55**
  (ME 17.2 bandı %78-86) → kendi yarı sahanda güvenli oynamak "top kaybı = net şans" demek →
  savunmak cezalandırılıyor. Denenen ve GERİ ALINAN müdahaleler (kanıtlarıyla): (a) nişan sapması
  sıfırlandı → isabet yine %55 (yani sorun nişan DEĞİL); (b) süpürme (swept) sahiplik testi →
  maç başına 28 gol / 143 şut, çekirdek dengesizleşti; (c) geri alma kilidi + bağıl hız kontrol
  yarıçapı → gerçek isabet %21-25 olarak ortaya çıktı (mevcut %55'in önemli kısmı pasçının kendi
  pasını geri alması). Sonuç: alım/sahiplik modeli (süpürme çarpışması + ilk dokunuş + lane seçimi)
  KENDİ dilimini ve kendi kalibrasyon bütçesini hak ediyor; parça parça yamayla düzelmiyor.
- **M7 sonrası kalan borçlar:** şut başına xG 0,47 (gerçek ~0,10; ortalama şut mesafesi 9,7 m,
  gerçek ~17 m — aynı kök: ceza sahasında yoğunluk yok); sprint sayacı ~6200/maç; taç üretimi 0;
  VAR (11.4), hava/zemin (12.4), LOD (16.1), maç sonu veri paketi (15.4) başlamadı.

- M8 (alım/sahiplik modeli) DENENDİ ve **GÖNDERİLMEDİ** (2026-08-09, "M8'i tam yap" onayı Atilla):
  Model yazıldı, ölçüldü, motoru M7'den belirgin KÖTÜ yaptığı için anayasa gereği reddedildi
  ("Test geçmeyen kod reddedilir"). Yama saklandı; bu kayıt işin ÜRÜNÜDÜR — bir sonraki denemenin
  aynı duvarlara tekrar çarpmaması için ölçümler tam yazılıyor.
  **Yazılan model:** (a) SÜPÜRME çarpışması — sahiplik testi topun tick içinde süpürdüğü YOLA bakar
  (top tick başına 1,2-1,9 m gider; nokta testi çizgideki oyuncunun üzerinden tünelliyordu);
  (b) ÜÇ SONUÇLU alım (ME 6.4) — temiz kontrol / ÇELME / temas yok; çelme sahiplik vermez, topu
  serbest bırakır (kesilen pas ve rebound'un doğal kaynağı); (c) temas menzili topun bağıl hızıyla
  daralır (hızlı topa tepki süresi yok); (d) temas için NİYET şartı (topa koşan ilk iki oyuncu,
  pasın alıcısı, kaleci ya da yavaş top); (e) geri alma kilidi; (f) **dürüst pas metriği** — pasçının
  kendi pasını geri alması "tamamlanmış" sayılmaz; (g) ME 7.4-A **boşluk vektörü** (8 yön × 2 yarıçap
  sabit arama: xT × açıklık).
  **Sonuç:** pas isabeti dürüst ölçümde **%26-43** aralığında TAVAN yaptı ve maç kalitesi çöktü
  (gol 10-22, faul 33-61, şut 26-48). M7 tabanı: gol 3,75 · şut 29,6 · korner 9,7 · faul 12,7.
- **M8'de ELENEN hipotezler (her biri ölçüldü — bir daha denenmesin):**
  1. *Nişan hatası* — pas sapması SIFIRA çekildi, isabet değişmedi (%55→%55). Sorun nişan değil.
  2. *Koridor risk ağırlığı* — 0,22 → 1,6 denendi; isabet artmadı, yalnız pas SAYISI düştü
     (900→460): oyuncu pas yerine dribling seçiyor, gol daha da artıyor.
  3. *Pas hızı* — groundSpeedMin 12 → 4-9; çelme azaldı ama uçuş uzadığı için kesme arttı, isabet düştü.
  4. *Kontrol eşiği / temas menzili / niyet kapısı* — çelme 752 → 210'a indi, isabet %10 → %43 ile sınırlı.
  5. *Takım yayılımı (wTop 0,25 → 0)* — isabet %33-34, hiç oynamadı.
  6. *Boşluk vektörü (ME 7.4-A)* — isabet +3 puan (%33 → %36). Eksikti, eklendi, yetmedi.
  7. *Top kapma oranı* — pTabanTackle/pTabanDriblin ile başarılı top kapma 550 → 199'a indirildi
     (gerçek ~55); sahiplik değişimi 680 → 364; **isabet yine %26-35**. Ana değişken bu da değil.
- **M8 teşhisi (bir sonraki dilimin girdisi):** dürüst pas isabeti motorun HİÇBİR tekil katsayısına
  duyarlı değil. Bu, sorunun bir katsayıda değil MİMARİDE olduğunu söylüyor: 22 ajan sürekli topun
  çevresinde toplanıyor, taşıyıcı 1,2 sn karar kilidiyle baskı altında oynuyor, top taşıyıcıya
  YAPIŞIK ilerliyor ve sahiplik ortalama 1,3 pasta el değiştiriyor (gerçek ~4). Öneri: bir sonraki
  denemenin adı "alım modeli" değil **"sahiplik süresi ve oyun ritmi"** olmalı — sırasıyla
  (i) taşıyıcı-top ayrımı (ilk dokunuş mesafesi, top gövdenin önünde), (ii) karar kadansının
  duruma bağlanması (baskı altında hızlı, boşta yavaş), (iii) pas ALTERNATİFLERİNİN topsuz
  koşularla üretilmesi, ancak sonra (iv) alım modeli. Sıra bu değilse alım modeli tek başına
  motoru bozuyor — bu tur bunun kanıtı.
- **M8'den ÇIKAN ve BEKLEYEN karar:** `PassCompletions` bugün pasçının kendi pasını geri almasını
  "tamamlanmış pas" sayıyor; dürüst ölçüm %21-25. Metriği düzeltmek `M2PassBand` kapısını kırar ve
  kapı gevşetmek karar gerektirir (CLAUDE.md). Öneri: metrik düzeltilsin, bant gerçeğe göre
  yeniden yazılsın ve ME 17.2 hedefi (%78-86) AÇIK BORÇ olarak kapıda görünsün.

- M8-B (pas/alım kök hataları) uygulandı (2026-08-10, "önerini uygula, gerekirse daha temel sorunu
  çöz" onayı Atilla): **Dürüst metrik üç kök hatayı ortaya çıkardı.** Önce `PassCompletions`
  pasçının kendi pasını saymayacak şekilde düzeltildi ve pas SONUCU sınıflandırıldı. İlk ölçüm:
  maç başına 1214 pasın **hedefe ulaşanı 0**, takım arkadaşına ulaşanı 0; 669'unu pasçı GERİ ALIYOR,
  543'ünü rakip topluyor. Yıllardır raporlanan "%55 isabet" tümüyle bu geri almaydı.
  1. **Pas atıldığı tick'te iptal oluyordu.** Tick sırası Karar → Aksiyon → Fizik; `ExecutePass`
     topa hız verir ama top hâlâ pasçının ayağındadır, aynı tick'teki serbest top taraması onu
     0 metrede bulup geri verir. Düzeltme: topu oynayanın GERİ ALMA KİLİDİ (ME 4.3).
     Sonuç: paslar gerçekten yol almaya başladı, gol 8,3 → 2,5, isabet %17 (gerçek taban).
  2. **Herkes 15 m/sn giden topu "kontrol ediyordu".** Vuruş noktasının 1-2 m ötesindeki presçi,
     topun ilk tick'te girdiği 1 m yarıçapta onu sahipleniyordu. Düzeltme: ME 6.4 İLK DOKUNUŞ —
     bağıl hız eşiği (FirstTouch açar); hızlı top kontrol edilemez, geçer. Pasın hedefinde top
     zaten yavaşladığı için alıcı alır. **Pas isabeti %17 → %76-82**, sahiplik değişimi
     1070 → ~350 (gerçek bant). Eşik EĞRİSİ tek tepeli: 6 → %62, 12 → %76, 20 → %25.
  3. **Golün %94'ü şut değildi.** Gol kaynağı sınıflandırıldı: 6,6 golün 6,2'si serbest yuvarlanan
     topun çizgiyi geçmesiydi — kaleci hiç şans bulmuyordu (nokta testi topun üzerinden atlıyor,
     kaleci serbest topa çıkmıyor). Düzeltme: kaleci için SÜPÜRME testi (son savunma hattı
     tünellenemez) + ME 9.3 ÇIKIŞ KARARI (kendi bölgesindeki serbest topu karşılar) + kalecinin
     Handling ile açılan kontrol eşiği (ME 9.4: eller). **Gol 6,6 → 1,3; serbest top golü 6,2 → 1,1.**
  Ayrıca **GEÇİŞ penceresi** eklendi: topu kaptıran takım savunma şekline ışınlanamaz, ileride
  kalanlar geri dönmek için zamana ihtiyaç duyar (mentalite ileri gittikçe uzar) — kontra atağın
  doğduğu yer ve hücuma çıkmanın bedeli.
- **M8-B kalibrasyon (12 maç ort.) — ME 17.2 bantları İLK KEZ toplu tutuyor:**
  **pas isabeti %80 (bant 78-86 ✓, ilk kez)** · şut 23,2 (20-28 ✓) · faul 27,7 (18-28 ✓) ·
  kart 2,9-3,6 (3,0-5,0 ✓) · ofsayt 4,0 (2-5 ✓) · sakatlık 0,42 (0,35-0,60 ✓) ·
  bitiş enerji 424 (350-550 ✓) · gol 3,3-3,7 (hedef 2,4-3,0 — hafif üstünde) ·
  korner 7,0-8,3 (hedef 8-12 — hafif altında) · sahiplik değişimi ~350 (gerçekçi).
- **M9 borçları (kapıda görünür halde):** (a) `M7AttackRiskRegresyon` — ofansif kurulum atak
  başına DAHA AZ yiyor (×0,80; hedef >1,00): hat arkasına hızlı KONTRA modeli yok, geçiş penceresi
  tek başına yetmiyor; (b) `M7DefendRegresyon` — defansif kurulum hâlâ daha çok yiyor (×1,51);
  (c) gol hedef bandın hafif üstünde, korner hafif altında; (d) sprint sayacı ~7300/maç;
  (e) VAR (11.4), hava/zemin (12.4), LOD (16.1), maç sonu veri paketi (15.4) başlamadı.
- **Kapı değişikliği (bilinçli, rapor edilmiştir):** `M7AttackTradeoff` ikiye ayrıldı —
  `M7AttackEffect` (taktik kolu işliyor mu; KAPI) + `M7AttackRiskRegresyon` (hücumun bedeli;
  bugünkü gerçeği kilitleyen BORÇ MUHAFIZI, hedefi ekrana yazar). Gerekçe: kapı, motor
  düzelmeden önceki bir varsayıma dayanıyordu; hücumun bedeli kontra modeli gelmeden doğmuyor.
  Sessizce gevşetilmedi; hedef her koşuda basılıyor.

- M9 (kontra atak / geçiş modeli) uygulandı (2026-08-10, "açık işleri planlayıp kapatalım" onayı):
  M8-B'de eklenen GEÇİŞ penceresi artık KULLANILIYOR. (a) Karar tarafı: rakip topu yeni
  kaptırdıysa (henüz şekline dönmedi) kazanan takımın fayda ağırlıkları kısa süre ileri kayar —
  doğrudan/dikey oyun ödüllenir; (b) topsuz taraf: kontra penceresinde forvet/orta saha derinliğe
  koşar. Hücuma ne kadar yığıldıysan pencere o kadar uzun (mentalite ile ölçekli) — hücumun
  bedelinin doğduğu yer burası.
- **M9 sonucu (ayna kadro, 20 maç):** `M7DefendRegresyon` **×1,51 → ×0,97** — savunmak ARTIK ÖDÜL
  VERİYOR (hedef <1,00 tutuldu). Hücumun bedeli (`M7AttackRiskRegresyon`) 10 maçlık örneklemde
  ×1,11-1,15 ölçüldü, 20 maçta ×0,83-0,90'a döndü: **etki gürültü sınırında, KANITLANMADI.**
  Kapı bu yüzden sertleştirilmedi (CLAUDE.md: kanıtlanmamış etkiyle kapı sertleştirilmez);
  örneklem 10 → 20 maça çıkarıldı ve hedef her koşuda basılıyor.
- **M9 kalibrasyon (12 maç ort.):** gol 3,2-3,7 · şut 19,8-21,2 · korner 8,0-8,6 (bant 8-12 ✓) ·
  faul 25,8 (18-28 ✓) · kart 3,6-4,6 (3,0-5,0 ✓) · süre 92,4 dk.
- **FAZ 03 kapanış planı yazıldı:** `docs/briefs/BRIEF_FAZ03_KAPANIS.md` — M9'dan M17'ye kadar
  kalan her iş, sırası ve KABUL KAPISI ile birlikte; ayrıca FAZ 03 dışı karar bekleyen 4 madde.

- M10 (duran top + ceza sahası üretimi) uygulandı (2026-08-13): **ORTA aksiyonu** eklendi —
  ME 6.4 aksiyon tablosunda vardı, motorda YOKTU. Kanattan ceza sahasına havadan besleme
  (8.3 balistiği + 10.2 hava topu zinciri). İki ek model gerekti: (a) ortanın hedefi SABİT
  derinlik değil, **ofsayt çizgisiyle kale arasına** nişan alınır — sabit hedef, ofsayt kısıtı
  yüzünden hep boş kalıyordu (aksiyon ölü doğuyordu); (b) top havadayken ileri roller **iniş
  noktasına koşar** — bu koşu olmadan hava topu yarıçapında kimse bulunmuyordu.
  Ölçüm: orta 0,0 → 0,6 → 1,9 → **5,1/maç** (gerçek 15-25 — mekanizma çalışıyor, sıklık düşük).
- **M10 durumu (hedeflere karşı):** korner 7,9-8,8 (bant 8-12 ✓) · **taç 43-48/maç** (gerçek ~40 ✓
  — M5'ten beri açık duran "taç üretimi ~0" borcu KAPANDI) · **penaltı hâlâ ~0** (bant 0,20-0,35 ✗).
  Penaltı borcu M11'e taşındı: kök neden ceza sahası içinde ihlal doğuracak kadar top girmemesi
  (orta sıklığı düşük + hücumcu kutuda kalamıyor).
- **M9 ölçümünün DÜZELTMESİ (dürüstlük kaydı):** M9 PR'ında `M7DefendRegresyon` ×0,97 ölçümüne
  dayanarak "savunmak artık ödül veriyor" dedim. M10 sonrası aynı kapı ×1,57 okuyor. Aradaki fark
  yalnız orta aksiyonu değil; metrik 20 maçta bile ×0,97-1,57 arasında SALINIYOR. Doğru ifade:
  **taktik denge metrikleri henüz kararlı değil**; iki hedef de (hücumun bedeli >1,00, savunmanın
  ödülü <1,00) AÇIK. Bu metrikler ancak M16'daki 10.000 maçlık koşuyla güvenilir hale gelir —
  kapı sertleştirme kararı oraya ertelendi.

- M11 (gol bandı + şut kalitesi) **KISMİ** (2026-08-13): kapı TUTULMADI, çalışma teşhisle kapandı.
  Yapılan: **ME 9.2 çelme geometrisi düzeltildi** — kaleci çeldiği topu "direk dışına" göndermeli;
  eski kod bunu sabit açıyla UMUT EDİYORDU, artık kale çizgisinde direk dışı bir noktaya
  geometriyle nişan alınıyor. Ayrıca gol kaynağı teşhis sayaçları eklendi (hash dışı).
- **M11 teşhisi (asıl bulgu):** maç başına ~4,4 golün yalnız **0,4'ü şut**; **4,0'ı serbest top**.
  Serbest top gollerinin **2,4'ünde topa en son SAVUNAN takım dokunmuş**, giriş hızı **13,8 m/s**,
  neredeyse hiçbiri havada değil. Bu, gerçek futbolun kendi kalesine gol oranının (~0,05/maç)
  ~50 katı. Elenen kaynak: kaleci çelmesi (geometri düzeltildi, sayı DEĞİŞMEDİ). Kalan şüpheliler,
  hız imzasına göre sıralı: (1) **kendi yarı sahasındaki pas** — pas yer hızı bandı 12-19 m/s,
  ölçülen 13,8 m/s ile birebir örtüşüyor; pas aday kümesi KALECİYİ de içeriyor (geri pas);
  (2) uzaklaştırma (12,6 m/s — yön mantığı kodda doğrulandı); (3) isabetsiz şutun fizik sonrası
  direkler arasına dönmesi (Flight=0 olduğu için kaleci analitik çözüme hiç girmiyor).
  **Sıradaki prob:** serbest top golünde topa en son dokunan AJANI (rol + mevki) kaydet; kaleciye
  geri pas mı, savunmacı pası mı, sapmış şut mu — üçünü ayırır.
- **M11 KAPANDI (aynı gün, prob koşuldu):** "son dokunan ajan" probu üç adımda kaynağı buldu.
  Serbest top gollerinin dağılımı: **kaleci 1,40 · defans 1,70 · orta saha-forvet 1,00**.
  Kaleci payı kaynağı gösterdi: **saha oyuncuları kaleciye GERİ PAS atıyordu** ve 12-19 m/s ile
  gelen top kalecinin toplayamadığı bir hızda kendi ağına gidiyordu. Kaleci pas ADAY KÜMESİNDEN
  çıkarıldı (kalecinin topu dağıtması ME 9.4'te kendi aksiyonudur; saha oyuncusunun kendi
  kalesine top yollaması bu modelde yok). **Gol 4,7 → 2,4** — ME 17.2 bandına (2,4-3,0) İLK KEZ girdi.
  Yolda iki model hatası daha düzeltildi (ikisi de tek başına yetmedi ama ikisi de yanlıştı):
  ME 9.2 çelme geometrisi ve **bloklanan şutun 360° DÜZGÜN dağılımdan sekmesi** — top kaleye
  momentumla gelirken gövdeye çarpıp rastgele yöne, sık sık ağa gidiyordu; artık geri/yana
  saçılma (`physics.blokSacilmaDeg`).
- **M11 kalibrasyon (12 maç ort.):** **gol 2,42 ✓ (bant 2,4-3,0)** · şut 17,7-21,1 · korner 5,5-6,8 ·
  faul 28,3 ✓ · kart 3,4-4,0 ✓ · ofsayt 3,8 ✓ · sakatlık 0,83 ✓ · bitiş enerji 413 ✓ · 91,3 dk.
- **M11 sonrası açık kalan:** korner 5,5-6,8 (bant 8-12 — M10'da 8,8'di, gol düşünce şut/korner de
  düştü) · şut 17,7 (bant 20-28) · penaltı ~0 · xG/şut 0,30 (hedef ≤0,20).
- **M11 borcu (model sınırı, bilinçli):** kaleciye geri pas + kalecinin topu TOPLAMASI (ME 9.4
  dağıtım zinciri) modellenmedi; şu an saha oyuncusu kaleciye pas atamıyor. Gerçek futbolda bu
  akış var — M14/M15 sırasında kaleci dağıtımıyla birlikte geri getirilmeli.
- **Kapı iyileştirmesi:** `M3GkMatters` tek maç yerine 6 tohum toplamına bakıyor (gol sayısı banda
  inince tek maçlık 1→2 farkı gürültüydü — aynı özellik, güvenilir ölçüm).

- M11-B (ceza sahası akışı) uygulandı (2026-08-13): **penaltı üretimi açıldı.**
  Kök neden ölçüldü: faul YALNIZ topu taşıyana yapılan müdahaleden doğuyordu; hava topu
  mücadelesi hakem makinesine hiç sunulmuyordu — oysa gerçek futbolda ceza sahası penaltılarının
  en yaygın kaynağı odur. Üç değişiklik birlikte gerekti:
  (a) **hava topu ihlali** ME 11.2'ye sunuldu (kaybeden tutunur/iter; şiddet skorunu hakem hesaplar,
  düdük çalarsa mücadelenin sonucu uygulanmaz); (b) **savunan da ortayı karşılıyor** — hücumcuyu
  M10'da koşturmuştuk, savunanı değil: hava toplarının yalnız %15'i ikili mücadeleliydi
  (10,7 olayın 1,6'sı), şimdi 2,6; (c) `cezaSahasiIhtiyatCarpan` 0,6 → 0,95 — bu çarpan M4'te
  penaltı 1,2/maç iken konmuştu, yeni modelde kutuda ihlali imkânsız kılıyordu.
  **Penaltı 0,00 → 0,30 (bant 0,20-0,35 ✓).**
  Ayrıca: bloklanan şut artık iki modlu (geri dönüş / yandan sıyırma) ve baskı altındaki savunan
  kendi kutusunda **korneri göze alıp** topu dışarı atabiliyor.
- **M11-B kalibrasyon (12 maç ort.):** gol 2,42-3,42 · şut 15,1-22,2 · **penaltı 0,30 ✓** ·
  faul 27,2 ✓ · kart 3,6-3,8 ✓ · korner 4,9-6,2 ✗ (bant 8-12).
- **Korner borcu (ölçülerek):** korner 4,3 → 6,2'ye çıktı ama banda girmedi. Kök neden korner
  üretimi DEĞİL, ceza sahasına giren top sayısı: orta 5,9/maç (gerçek 15-25) ve şut 15-22
  (bant 20-28). Korner bunların türevi — orta sıklığı M10 borcu olarak açık, önce o kapanmalı.

- M11-C (kutuya giriş + kaleci hakimiyeti) uygulandı (2026-08-13, "orta sıklığını aç, korner ve
  şutu banda sok" isteği): **orta 5,9 → 21,2/maç (gerçek 15-25).** Ölçüm önce yanlış kaldıracı
  eledi: maç başına **175 orta FIRSATI** oluşuyordu ama yalnız **11'inde** kutuda karşılayacak
  arkadaş vardı — yani sorun ortanın fayda ağırlığı değil, **kutuya kimsenin girmemesiydi.**
  Eklenen davranışlar: (a) **kutuya giriş** (ME 7.4-A) — topu taşıyan kanatta ve ileri konumdaysa
  ileri roller ceza sahasına koşar, yakın/uzak direği paylaşır; (b) **kaleci hava hakimiyeti**
  (ME 9.3) — kendi kutusuna inen havadaki topta AerialCommand ile öne çıkar ve topu TOPLAR;
  (c) **kaleci altı pasında topun üstüne kapanır** (ME 9.4) — gol sahasındaki serbest topta
  kontrol yarıçapı büyür; (d) **kafa vuruşu nişanı** ayakla aynı isabette değil (ME 6.4);
  (e) yer müdahalesinde de savunan kendi kutusunda korneri göze alabiliyor.
- **M11-C kalibrasyon (12 maç, iki tohum kümesi):** **gol 2,50-2,75 ✓ (bant 2,4-3,0)** ·
  **şut 20,3-22,8 ✓ (bant 20-28)** · korner 5,3-8,8 (bant 8-12 — bir küme içinde, biri değil) ·
  faul 29,4 ✓ · kart 4,3-5,1 ✓ · penaltı 0,30 ✓ · pas isabeti %80 ✓.
- **Korner borcu (kalan):** iki tohum kümesi arasında 5,3 ↔ 8,8 salınıyor; ortalama ~7, bant 8-12.
  Kaleci hakimiyeti ile korner üretimi doğrudan ters çalışıyor (kaleci topladıkça korner düşüyor) —
  ikisinin dengesi 10.000 maçlık kümede (M16) sabitlenmeli; tek tohum kümesinde ince ayar,
  aşırı uydurma riski taşır.
- **Kapı düzeltmesi:** `M6AutoManage` artık TÜM sakatlıkları değil, oyuncuyu SAHADAN ÇIKARAN
  sakatlıkları sayıyor (`InjuriesOffPitch`) — hafif sakatlık zaten değişiklik gerektirmiyordu,
  kapı yanlış şeyi ölçüyordu.

- M12 (VAR dram sistemi) uygulandı (2026-08-13): **ME 11.4 birebir.** `MatchPhase.VarReview`
  fazı devreye girdi: inceleme sırasında oyun DURUR, saat işlemez, duraklama birikir (→ uzatma,
  ME 3.4). Bekleme süresi 20 + 70×zorluk sn (REFEREE çekilişi — sunumda gerilim kancası).
  Karar doğruluğu spec'teki gibi: **VAR gerçeği bilir** (motor kesin veriye sahiptir); yanılma payı
  yalnız chaos seviyesine bağlı ("saha kararı kalır" oranı, `var.sahaKarariKalirOran`).
  Uygulanan inceleme sınıfları: **(3) ceza sahası içi foul gri bandı** — karar VERİLMEDEN incelemeye
  gider; **(4) kırmızı kart gri bandı** — kart gösterilir, sonra incelenir, geri alınırsa sarıya iner.
- **M12'de UYGULANMAYAN 2 sınıf (yapısal gerekçe, kayıt):** (1) *gol öncesi ofsayt marjı < 0,30 m* —
  motorda ofsayt pas ANINDA düdükle biter (ME 10.5 uygulaması), yani "ofsayt golü" durumu hiç
  oluşmuyor; bu sınıf ancak ofsayt modeli "gol sonrası inceleme" akışına geçerse anlamlı olur.
  (2) *gol öncesi atak fazında foul gri bandı* — atak fazı geçmişi (olay tamponu) yok; ME 15.1
  event log dilimiyle (M14) birlikte gelmeli. İkisi de M14 borcuna yazıldı.
- **M12 ölçüm:** inceleme 0,08/maç (Checks kadrosu) — 40 maçlık örneklemde geri alma oranı %33;
  duraklama 3,1 dk/maç. Kapılar: `M12VarProduced`, `M12VarOverturn`, `M12VarDeterminism`,
  `M12VarStoppage`.
- **M12 kalibrasyon (12 maç):** gol 2,25-3,00 · şut 19,6-20,9 · **korner 8,1-8,2 ✓ (bant 8-12)** ·
  faul 29,7 ✓ · kart 4,3-5,3 ✓. Korner borcu KAPANDI.
- **Kapı düzeltmesi:** `M5MomentumSwing` tek tohuma bağlıydı ve golsüz maçta ölçülemiyordu;
  artık gol görülene kadar 8 tohum deniyor (aynı özellik, tohum şansına bağlı değil).

- M13 (hava ve zemin) uygulandı (2026-08-13): **ME 12.4 tablosu birebir.** `MatchConfig`'e
  `Weather` (Kuru/Yağmur/Kar/Sıcak), `PitchTier` (1-5), `WindMS` + `WindDir` girdi; tüm çarpanlar
  `balance/sim.balance.json` → `hava.*` altında. Motor bunları maç başında BİR KEZ türetir
  (koşul maç boyunca sabit, sıcak yolda dallanma yok): nitelik deltaları (Passing/FirstTouch/
  Vision) motorun nitelik kopyasına işlenir — kulübeden gelen oyuncuya değişiklik anında da; a_roll,
  sekme e, top hızı, v_max, sakatlık ve stamina çarpanları kendi kullanım yerlerine bağlanır.
  Kötü zeminde (Tier 1-2) sekme yönüne ±2° pertürbasyon eklenir (PHYSICS domain, LUT ile).
- **Nötrlük kanıtı:** Kuru + Tier 3 + rüzgarsız kurulumda TÜM çarpanlar tam 1,0 ve deltalar 0'dır →
  M0-M12 golden hash'lerinin hepsi BİT DÜZEYİNDE korundu. `M13NotrAynilik` kapısı bunu ayrıca
  niyet olarak yazıyor: hava alanına hiç dokunulmamış kurulum ile kuru kurulum aynı hash'i verir.
- **KÖK NEDEN (M13'te bulundu ve düzeltildi) — pas gücünde çift sayım:** `PassSpeed` pasın hızını
  "hedefte DURACAK şekilde" çözer, yani zeminin etkisi zaten `a_roll`'dedir. ME 12.4'ün "top hızı
  −%10" satırı bunun ÜSTÜNE uygulanınca her pas sistematik olarak %19 kısa kalıyordu — pasçı
  oynadığı zemini bilmiyormuş gibi. Ölçüm (kar, 12 maç): gol 2,25→**1,17**, taç 15,4→**1,3**,
  sahiplik değişimi 360→431. Düzeltmeden sonra kar: gol 1,92 · taç 5,0. Çarpan artık yalnız hızını
  MESAFEDEN değil VURUŞTAN alan toplara uygulanıyor: şut, orta, korner, uzaklaştırma.
- **Rüzgar doğrulaması (istatistik değil doğrudan geometri):** elle kurulmuş kornerde topun düşüş
  noktası — 8 m/sn → 1,88 m · 16 m/sn → 3,78 m · ters yön → −1,86 m. ME 12.4 formülüne
  (rüzgar × k_w × uçuş_süresi) tam doğrusal ve işaretli.
- **`ruzgarK` [KALİBRE] 0,045 → 0,15:** ilk değer tahmindi. Yeni değer aerodinamikten türetildi —
  top (0,43 kg, A≈0,038 m², Cd≈0,25) 16 m/sn yan rüzgarda ≈3,4 m/sn² yanal ivme görür; 1,6 sn'lik
  bir korner uçuşunda ≈4 m sapma eder. 0,045 aynı kornerde yalnız 1,2 m veriyordu (fırtınada bile
  ölçülemez etki). Rüzgar varsayılanı 0'dır; koşula atama FAZ 04 lig takvimi katmanında.
- **M13 ölçüm (12 maç, koşul başına — gol/şut/faul · pas isabeti · taç · bitiş enerjisi · sakatlık):**
  kuru 2,25/19,6/27,5 · %82,9 · 15,4 · 413 · 0,67 — yağmur 2,75/23,0/26,6 · %80,5 · **36,1** · 474 ·
  0,83 — kar 1,92/20,1/21,0 · %83,4 · **5,0** · 398 · 1,08 — sıcak 3,50/22,8/27,3 · %81,2 · 17,5 ·
  **351** · 1,25 — zeminKötü 2,25/22,1/33,4 · %82,5 · 20,4 · 427 · 0,67.
- **Kapının duruşu (bilinçli):** ME 17.2 kalibrasyon bandı REFERANS koşul (kuru + Tier 3) içindir.
  "Kar da 2,4-3,0 gol atsın" demek 12.4'ü silmek olurdu. Kapı bu yüzden iki şey denetler: (1) referans
  koşul bit düzeyinde değişmedi, (2) her koşul spec'in söylediği YÖNDE ölçülebilir fark üretiyor ve
  hâlâ FUTBOL kalıyor (`M13FutbolZarfi`: gol 1,0-4,5 · şut 12-32 · faul 15-40 · pas isabeti %70-90 —
  bu zarf 17.2 bandı DEĞİLDİR ve gate mesajında böyle yazılıdır).
- **M16 BORCU (ölçüldü, kapatılmadı):** yağmurda korner 8,2→**14,7** (bant 8-12 üstü), karda
  8,2→**3,9** (bant altı); yağmurda taç ×2,2. Mekanizma anlaşıldı ve tek: `pass.groundSpeedMin`
  (12 m/sn) kısa pasları zorunlu olarak sert vurdurur, topun aşım mesafesi 1/a_roll ile ölçeklenir —
  KURU koşulda da 10 m'lik bir pas 11,8 m aşıyor. Yani bu bir hava hatası değil, pas modelinin
  zaten var olan zayıflığının hava tarafından BÜYÜTÜLMESİ. Düzeltmesi pas gücü modeline dokunur
  (tüm M4-M12 kalibrasyonunu ve golden'ları hareket ettirir) → 10.000 maçlık M16 sprintine.
- **M17 BORCU:** `Weather`/`PitchTier`/`Wind*` replay dörtlüsünün config_hash'ine GİRMELİDİR
  (ME 3.3). ConfigHash şu an host tarafından set ediliyor; arayüz dondurmada (M17) hava alanları
  kanonik özete dahil edilecek — aksi halde aynı seed farklı havada farklı maç verir ve replay kırılır.
- **Bilinçli sınır:** rüzgar yalnız HAVA topuna (orta + korner) uygulanır; ME 12.4 satırı da
  "uzun top/orta sapması, frikik-korner nişanına eklenir" der. Şut ve yerden pas kapsam dışıdır.

- M14 (event log + highlight + maç sonu paketi) uygulandı (2026-08-14): **ME 15.1/15.3/15.4.**
  `MatchEvent` şeması spec'teki alanlarla birebir; 6 kategori, 30 tip; 4096'lık halka tampon maç
  başında tek tahsis (16.2). Log **TEK YÖNLÜ**: simülasyon ondan asla okumaz, `StateHash`'e girmez —
  bu yüzden tampon taşması davranışı değiştiremez. M0-M13 golden'larının hepsi bit düzeyinde korundu.
- **Tek kaynak ilkesi uygulandı (15.1):** `MatchSummaryPacket`'in istatistik satırı ayrı sayaçlardan
  değil EVENT LOG'dan türetiliyor; `M14TekKaynak` kapısı bunu motorun kendi sayaçlarıyla birebir
  karşılaştırıyor. İlk koşuda iki sapma yakalandı ve düzeltildi: (a) gol hem `ShotOnTarget` hem
  `Goal` olayıyla iki kez şut sayılıyordu — gol artık şut SAYILMIYOR (girişim olayı zaten yazılı;
  serbest top golü futbolda da şut değildir); (b) VAR'ın ONAYLADIĞI faul sayacı artırıyor ama olay
  yazmıyordu.
- **WinProb modeli [KALİBRE] — spec formül vermez:** ME 15.3 yalnız "kayan WinProb modeli" der.
  p = lojistik(k × gol_farkı / √(kalan_dk/90)), k = 0,85. Gerekçe: aynı gol farkı kalan süre azaldıkça
  daha kesin sonuç demektir. Gerçek futbolla ölçek denetimi: 1-0 / kalan 45 dk → %77 (gerçek ~%77) ·
  1-0 / kalan 10 dk → %93 (~%92) · 2-0 / kalan 45 dk → %92 (~%93).
- **hikaye_ilgisi terimi 0'dır (bilinçli):** aktif hikaye arkı Modül 6 (FAZ 04) ile gelir. Ağırlık
  (0,10) tabloda DURUYOR — kanca bağlanınca puanlar kendiliğinden zenginleşir, formül değişmez.
- **BULGU — highlight işaret yoğunluğu ince:** ME 15.3'ün H > 0,50 eşiği ölçümde **0,5-0,8 an/maç**
  üretiyor; maçların yarısında zaman çizelgesinde HİÇ işaret yok. Aritmetik: 67. dakikada beraberliği
  bozan gol H = 0,58 (geçer), 20. dakikada beraberliği bozan gol H = 0,45 (geçmez), 2-0'ı yapan gol
  H = 0,32. Formülün ağırlıkları ve 0,50 eşiği SPEC SABİTİDİR; nadirlik tablosunu maksimuma çeksek
  bile 20. dakika golü eşiği geçmiyor (0,15 × 1,0 = 0,15 tavan katkı). Yani bu bir kalibrasyon değil
  FORMÜL özelliği. Spec değiştirilmedi; öneri "Bekleyen kararlar"a yazıldı. Not: "en yüksek 6 an klip
  önerisi" akışı bundan BAĞIMSIZ çalışıyor (top-10 listesi her maçta dolu, `M14PaketSemasi` denetliyor).
- **BULGU — kırmızı kart 1,0-1,2/maç (ME 17.2 bandı 0,15-0,30), tamamı İKİNCİ SARI.** Doğrudan
  kırmızı 0,00: `kirmiziEsik 0,80` şiddet skorunun erişilebilir tavanının (≈0,90, ceza sahasında
  ×0,72) üstünde kalıyor. Bu metrik M4'ten beri `kart = sarı + kırmızı` toplamının içinde SAKLIYDI —
  event log'un ilk kazancı bunu görünür kılmak oldu. Ayrı ölçülmeyen metrik, ölçülmemiş metriktir:
  `M14KirmiziBandi` ve `M14SariBandi` artık AYRI kapılar.
- **BULGU — hakem makinesine sunulan olay 649/maç:** 27,7'si düdükle, 28,0'ı avantajla sonuçlanıyor,
  593'ü sessiz geçiyor. Avantaj 28/maç gerçek futbolun (~2-5) çok üstünde; faul sayısı bantta
  görünüyor çünkü avantaj kuralı fazlalığın yarısını yutuyor. Üçü de (kırmızı yığılması, avantaj
  sıklığı, olay hacmi) aynı yere bakıyor: **`ResolveFoul` her temas denemesinde çağrılıyor.** Kök
  düzeltme karar/temas modeline dokunur ve tüm golden'ları hareket ettirir → M16.
- **M12'nin 2 VAR sınıfı hâlâ açık — gerekçe SADELEŞTİ:** engel event log'un yokluğu değilmiş.
  İkisi de "gol VERİLİR, sonra incelenir, gerekirse GERİ ALINIR" akışını gerektiriyor; motorda
  geçici/askıda gol durumu yok (gol anında momentum uygulanıyor ve santra kuruluyor). Bu bir faz
  makinesi eklemesidir → M17 arayüz dondurmasından önce, ayrı dilim olarak önerilir.
- **M14 ölçüm (12 maç):** event 1.530/maç (tepe 1.651 · kapasite 4.096 · düşen 0) · H>eşik 0,50/maç ·
  sarı 4,25 ✓ · kırmızı 1,00 ✗. Tip dağılımı (/maç): PassCompleted 976 · PassIntercepted 169 ·
  PhaseChange 141 · TackleWon 49 · FoulCommitted 27 · AdvantagePlayed 27 · CrossDelivered 17 ·
  ThrowIn 15 · ShotOffTarget 9 · StaminaAlert 9 · CornerAwarded 8 · Goal 2,25.
- **M16 BORCU (yeni):** event hacmi 1.530/maç, ME 15.1 hedefi 900-1.400. Sapmanın tamamı pas
  olaylarından (1.145/maç) geliyor — M13'te yazılan `groundSpeedMin` aşımıyla AYNI kök.

- M15 (LOD türetme + performans bütçeleri) uygulandı (2026-08-16): **ME 16.1/16.3/16.4.**
- **ÖLÇÜM ÖNCE — LOD 0 maç başına 131 ms** (50 maç, tek çekirdek). ME 16.1'in bütçesi 2.500 ms:
  **19 kat altında.** ME 16.3'ün throughput hesabı 2,5 sn/maç varsayımına dayanıyordu; gerçek
  sayıyla 24 çekirdekli düğüm **~185 maç/sn** yapıyor, hedef 16,7 → 2 düğüm değil **tek düğümün
  onda biri** yetiyor. Bellek: maç içi geçici tahsis ~9 KB (hedef <50 KB ✓); ölçülen 137 KB'ın
  128'i event log halka tamponunun kendisi (motor NESNESİ başına, tick içinde değil).
- **KARAR — LOD 1, LOD 0'ın eşleniği yapıldı.** 16.1'in LOD 1 satırı (5 Hz hareket / 2 Hz karar)
  tek gerekçeyle vardı: CPU. LOD 0 zaten LOD 1 bütçesinin (800 ms) 6 katı altında. İkinci bir tick
  oranı = ikinci bir fizik entegrasyonu = ikinci bir kalibrasyon; kazanç sıfırken bedeli "tek sim,
  tek gerçek" ilkesi. `M15Lod1Esdeger` kapısı bunu yürütülebilir olgu yapıyor (bit-aynı hash).
  Spec DEĞİŞTİRİLMEDİ; karar geri alınırsa `LodLevel.Lod1` ayrışır ve kapı bunu yakalar.
- **LOD 2 gerçekten gerekli:** bütçenin sıkıştığı tek yer istemci. ME 16.4'ün sezon turu ~200 arka
  plan maçı istiyor ve 12-18 sn'de bitmeli; 200 × LOD 0 orta cihazda dakikalara çıkar. LOD 2 ölçümü:
  **3 µs/maç** (bütçe 10 ms) → sezon turu bu makinede ~1,3 sn.
- **LOD 2 modeli — üç yanlış denemeden sonra ızgara.** (1) λ = exp(b0 + b1·d): dengeli maçta 40 şut
  üretti, LOD 0 22,5. Sebep: şut sayısı güç farkının YÖNÜNE değil BÜYÜKLÜĞÜNE bağlı. (2) |d| terimi
  eklendi: ±12 kademesi hâlâ %26-29 saptı. Sebep: aynı fark farklı SEVİYEDE aynı maçı vermiyor.
  (3) seviye terimi eklendi: yine saptı — 0,07'lik seviye değişimi gol sayısını %80 oynatıyor,
  global fonksiyon biçimi bu yüzeye oturmuyor. **Çözüm: (kendi güç × rakip güç) ızgarası + iki
  doğrusal ara değerleme.** Doğal koordinat, döndürme yok, ek varsayım yok — spec'in kelimesi de
  zaten "tablo". Sonuç: 5 güç kademesinde ortalama sapma ±%25 içinde (uçta 12,78 vs 13,05).
- **BULGU (M15'in en ağır çıktısı) — güç tepkisi aşırı dik.** 3.920 LOD 0 maçından üretilen
  7×7 gol ızgarası (satır = kendi gücü, sütun = rakip gücü, takım başına gol):
  ```
        39,6   45,6   51,6   57,6   63,6   69,6   75,6
  39,6   1,95   0,76   0,60   0,21   0,23   0,01   0,09
  51,6   4,11   2,13   1,45   0,86   0,46   0,29   0,14
  63,6  12,56   6,24   3,03   1,74   1,28   0,74   0,48
  75,6  27,98  21,89  11,71   5,59   2,81   1,90   1,06
  ```
  Köşegen (eşit güç) SAĞLAM: 57,6 vs 57,6 → 1,30 + 1,30 = 2,60 gol/maç, ME 17.2 bandında.
  Bozuk olan MİSMATCH tepkisi: 75,6'lık takım 39,6'lık takımı **28-0,1** yeniyor; gerçek futbolda
  aynı fark ~3-0'dır. ME 17.2'nin "güçlü takım possession bandı (75v55)" satırı ve ME 17.3'ün chaos
  upset doğrulaması bu eğrinin üstünde durur — 75v55 bizde ~11,7-0,5, yani upset olasılığı ~0.
  **M16'nın asıl işi budur** ve artık sayısı var.
- **BULGU — kompozisyon tek skalerle temsil edilemiyor:** aynı toplam güce sahip FARKLI çekilişli
  kadrolar aynı sonucu vermiyor (69,6 ev takımı, 60,1'lik AYNA rakibe 2,5 gol atarken aynı güçteki
  FARKLI çekilişli rakibe 5,2 atıyor). Ölçülen LOD 2 kompozisyon hatası **%42**. Doğru çözüm
  futbolun standart modeli: hücum ve savunma güçlerini AYRI eksene almak ("A'nın hücumu ×
  B'nin savunması"); tablo 2 boyutlu kalır, üretici hücum/savunma niteliklerini bağımsız tarar.
  M16 borcu; `M15KompozisyonHatasi` bugünkü hatayı kilitliyor.
- **Üretici CI adımı (16.1):** `dotnet run --project shared/TheBadge.Sim.Checks -c Release -- fit-lod2 [hücreBaşınaMaç]`.
  Kapı programının İÇİNDE çünkü ikisi de aynı test kadrosu üreticisini ve aynı balance yükleyicisini
  kullanmak zorunda; ayrı proje ikinci bir "test kadrosu" tanımı doğururdu. Paralellik yalnız
  MAÇLAR arası (CLAUDE.md). 3.920 maç ~2 dk 14 sn (8 çekirdek).
- **Dosya ayrımı:** `balance/sim.lod2.json` ÜRETİLMİŞ veridir (elle düzenlenmez); elle ayarlanan tek
  şey güç bileşiminin ağırlıkları ve o `sim.balance.json` → `lod.guc` altındadır. Karıştırmak
  "hangi sayı kararla, hangisi ölçümle geldi" ayrımını yok ederdi.

- M16-A (sonuç dağılımı teşhisi) tamamlandı (2026-08-16): **ME 13.4 / 17.3.** Bu dilim kod
  kalibrasyonu DEĞİL, kalibrasyonun neden yapılamadığının ÖLÇÜMÜDÜR — ve tek bir köke çıkıyor.
- **İyi haber:** eşit güçte beraberlik oranı **%27**, ME 17.3'ün hedefi %22-30 → ✓ zaten bantta.
  Yani motorun "denk maç" davranışı sağlam; bozuk olan yalnız GÜÇ FARKININ sonuca yansıması.
- **ÖLÇÜM — üstünlük zincirin neresinde katlanıyor** (40 maç/kademe, ayna kadro):
  | fark | sahiplik | atak sayısı | ŞUT/ATAK | şut oranı | gol oranı |
  | --- | --- | --- | --- | --- | --- |
  | 0 | ×0,99 | 187 / 187 | 0,054 / 0,065 | ×0,82 | ×1,08 |
  | +6 | ×1,15 | 180 / 193 | 0,087 / 0,036 | ×2,26 | ×1,90 |
  | +12 | ×1,29 | 170 / 198 | 0,158 / 0,019 | ×7,13 | ×5,44 |
  | +24 | ×1,51 | 153 / 202 | **0,566 / 0,004** | ×102 | ×259 |
  **Sahiplik GERÇEKÇİ** (+24'te 60/40) ve **atak sayısı neredeyse eşit** — zayıf takım daha çok
  atak bile yapıyor. Kırılma tek yerde: **bir atağın şuta dönüşme olasılığı.** Ribaund döngüsü
  değil (şutların yalnız %10,8'i 6 sn içinde tekrar şut).
- **KÖK NEDEN — atak zinciri çok uzun.** Bir atağın şuta dönmesi ~8 ardışık başarı istiyor
  (p^k uyumu: k≈8); futbolda bu 3-4'tür. Zincir uzun çünkü **sahiplik maç başına 374 kez el
  değiştiriyor** (gerçek ~120): pas hacmi 1.177/maç (gerçek ~800) ve tackle 318/maç (gerçek ~35).
  Uzun zincirde halka başına küçük bir üstünlük ÜSTEL katlanır — 0,50 → 0,67'lik bir kenar
  k=8'de ×10 olur. Ölçülen tam olarak budur.
- **KANIT — tek katsayı çözmüyor.** `kDuel` süpürmesi (200 maç/nokta, 75v55):
  0,90 → %99,5 · 0,60 → %98 · 0,45 → %97 · 0,30 → %94 · **0,20 → %87** (hedef %66).
  Çarpanı 4,5 kat düşürmek bile yetmiyor, çünkü güç farkı **~8 ayrı kanaldan** akıyor: düello
  marjı, v_max, pas sigması, şut sigması, kaleci kurtarışı, kontrol eşiği, aday sayısı, karar
  gürültüsü. Eşit güçte kDuel'in etkisi ihmal edilebilir (gol 1,30-1,16 → 1,28-1,33), yani
  katsayıyı kısmak "denk maçı bozmadan" da çözmüyor — sorun katsayıda değil YAPIDA.
- **BU KÖK, ÖNCEKİ DÖRT BORCUN DA KÖKÜ.** M13 (yağmur/kar korner sapması: `groundSpeedMin` ×
  1/a_roll aşımı), M14 (event hacmi 1.530/maç — sapmanın tamamı pas olaylarından), M14 (hakeme
  sunulan 649 olay + avantaj 28/maç), M15 (güç tepkisi ×14,8 ve LOD 2 kompozisyon hatası) —
  dördü de aynı yere bakıyor: **pas/sahiplik modeli topu çok sık ve çok kısa oynatıyor.**
- **KARAR ÖNERİSİ (Atilla'nın onayı bekleniyor):** M16'nın ilk gerçek işi katsayı taraması değil,
  **pas/sahiplik modelinin yeniden kurulmasıdır** — hedef: maç başına ~800 pas, ~120 sahiplik
  değişimi, ~35 tackle. Bu değişiklik TÜM golden'ları ve TÜM kalibrasyon sayılarını hareket
  ettirir, bu yüzden ayrı ve bilinçli bir dilim olmalı. Kapılar (`M16UpsetBandi`,
  `M15GucTepkisi`, `M14EventHacmi`) bugünkü gerçeği kilitliyor; ilerleme oradan okunacak.
- **M16'nın kalan alt dilimleri (henüz açık):** chaos motoru (ME 13.1-13.3 — 5 enjeksiyon
  noktasının yalnız 1'i uygulanmış, üstelik tek seviye sabit kodlu; 17.3 doğrulaması buna bağlı),
  kart/faul hacmi, 10.000 maçlık 17.2 tablosu.

- M16-B (pas aşımı düzeltmesi) **DENENDİ ve GERİ ALINDI** (2026-08-16, Atilla'nın "önce küçük
  adım" kararıyla). Yama saklandı: `scratchpad/M16B_pas_varis_hizi.patch`.
- **Ne denendi:** `PassSpeed`'in "hedefte DUR" + `groundSpeedMin` (12 m/sn) taban kırpması yerine
  fizikten türetilen varış hızı modeli: v0² = v_varış² + 2·a·d. Bu biçimde aşım mesafesi SABİT
  olur (v_varış²/2a) ve 21,8 m'den kısa her pası 21,8 m'ye fırlatan taban ortadan kalkar.
- **Ölçüm — `varisHiziMS` süpürmesi (120 maç/nokta, ayna kadro):**
  | v_varış | gol (eşit) | şut | korner | pas isabeti | SAHİPLİK DEĞİŞİMİ | tackle | 75v55 G/B/M |
  | --- | --- | --- | --- | --- | --- | --- | --- |
  | 5 | 1,06 | 20,3 | 3,4 | %78,6 | 482 | 415 | %97,5 / %2,5 / %0 |
  | 7 | 0,93 | 18,6 | 3,6 | %83,0 | 396 | 376 | %96,7 / %3,3 / %0 |
  | 9 | 1,65 | 21,1 | 5,1 | %83,7 | 342 | 333 | %97,5 / %2,5 / %0 |
  | 11 | 3,09 | 22,1 | 9,0 | %81,3 | 331 | 298 | %99,2 / %0,8 / %0 |
  | 13 | 6,21 | 26,3 | 20,7 | %73,1 | 345 | 260 | %100 / %0 / %0 |
  (eski model: gol 2,25 · şut 19,6 · korner 8,2 · isabet %82,9 · sahiplik değişimi 360 · tackle 318)
- **SONUÇ: hipotez YANLIŞ çıktı.** Aşımı tamamen kaldırmak sahiplik değişimini 360 → **331'in
  altına indiremedi** (hedef ~120) ve **75v55 hiç kıpırdamadı** (%97-100, hedef %66). Yani pas
  aşımı, zincir uzunluğunun ana sürücüsü DEĞİLMİŞ. Ayrıca hiçbir v_varış değeri ME 17.2 bandını
  korumuyor: 11'de gol/korner tutuyor ama sahiplik kazancı ihmal edilebilir, 5-9'da gol çöküyor.
  Ölçülebilir kazanç olmadan tüm golden'ları yeniden pinlemek bedelsiz değil → geri alındı.
- **DÜZELTİLMİŞ KÖK NEDEN — tackle sıklığı.** Süpürme asıl sürücüyü gösterdi: her v_varış
  değerinde **tackle sayısı ≈ sahiplik değişimi sayısı** (333 ≈ 342, 298 ≈ 331, 260 ≈ 345).
  Yani sahiplik neredeyse HER SEFERİNDE bir tackle ile el değiştiriyor. Gerçek futbolda maç
  başına ~35 tackle ve ~120 sahiplik değişimi vardır; bizde **tackle 260-415**, yani model ~10 kat
  fazla tetikleniyor. Zincir uzunluğunu belirleyen budur — ve M14'ün "hakeme sunulan 649 olay"
  bulgusu da aynı yere bakıyordu. **M16'nın bir sonraki denemesi tackle tetikleme modelidir**
  (yarıçap, soğuma, "yalnız en yakın savunucu dalar" kuralının gerçekten ne sıklıkla tetiklendiği),
  pas modeli değil.
- **Yöntem notu:** bu, M8'den sonra ikinci "denendi, ölçüldü, reddedildi" dilimidir. İkisinde de
  yama saklandı ve bulgu kaydedildi; ikisinde de kod REDDEDİLDİ çünkü ölçüm hipotezi doğrulamadı.
  Negatif sonuç da sonuçtur — bir sonraki denemeyi doğru yere yönlendirir.

- M16-C (tackle tetikleme ölçümü) tamamlandı (2026-08-16): **enstrüman + süpürme + karar.**
- **Enstrüman (davranış-nötr, golden'lar korundu):** tackle DENEME aralığı karar kilidinden ayrıldı
  (`possession.tackleDenemeAralikTicks` [KALİBRE], varsayılan 40 = eski davranışla birebir);
  `TackleAttempts` sayacı; sahiplik değişimi AYRIŞIMI (`PossChangeTackle/Intercept/Loose` —
  topu açığa çıkaran son olaya göre); `TackleWon` olayı artık iki müdahale yolunda da yazılıyor
  (M14 kapsama boşluğuydu). `M16AyrisimMuhasebesi` kapısı iç tutarlılığı denetliyor.
- **Ölçüm — ayrışım (varsayılan ayarlarla, Checks kadrosu):** sahiplik değişimi 341 =
  **tackle 165 + pas kesme 167 + serbest 10**; deneme 904 → başarı 305. Yani zincir iki eşit
  motorla dönüyor: müdahale VE kesme. M16-A'daki "374'ün sürücüsü tackle" okuması YARIM doğruymuş.
- **Süpürme (aralık × taban, 120 maç/nokta, ayna kadro):** aralık 40→480 + p 0,30→0,18 denemeleri
  873→431'e, tackle kaynaklı değişimi 193→74'e indirdi. AMA: (a) kesme kanalı sabit kaldı
  (~173-190 — taban budur, tackle ayarına cevap vermez); (b) toplam değişim yalnız 379→278;
  (c) **75v55 yerinden oynamadı: %99,2 → %96,7.** Faul 12,9→8,0'a düştü (bant dışına), sarı zaten
  0,5-0,7 (ayna kadroda kart, marj farkı olmadığı için üretilmiyor — kart bandının kadro
  dağılımına bağımlılığı ayrıca not edildi, M16-E kalibrasyon setinin tanımına girecek).
- **KARAR: varsayılanlar DEĞİŞMEDİ.** Tackle ayarları kendi hedefini (hacim) taşıyor ama upset'i
  taşımıyor ve bantları tek başına bozuyor; katsayı değişikliği ancak M16-E tam kalibrasyonuyla
  birlikte anlam kazanır. Enstrüman repo'da kaldı — M16-D/E onunla yön bulacak.
- **ASIL SONUÇ — eksik mekanizmanın spec'te ADI VAR.** İki teşhis üst üste bindi: zayıf takımın
  atağı şuta dönemiyor (M16-A: şut/atak ×100) ve zincirin tabanı kesme kanalı (M16-C). Gerçek
  futbolda zayıf takım bu zinciri KISALTARAK yaşar: uzun top, degaj, kontra — 1-2 halkada hücum
  sahasına. ME 7.2'nin aday kümesinde **LongSwitch(r)** ve **ClearBall** var; ME 9.4'te kaleci
  dağıtımı **KısaAçıl / UzunDegaj / ElleAt** var. ÜÇÜ DE MOTORDA YOK — M10'daki "orta spec'te
  vardı, motorda yoktu" durumunun birebir aynısı. **M16-D bu adayları uygular**; upset yeniden
  ondan sonra ölçülür. (Kaleci M11'de pas adaylarından çıkarılmıştı; 9.4 dağıtım seti gelince
  kaleci "pas alan" değil "dağıtan" olarak geri döner — geri paslar sorunu geri gelmez.)

- M16-D (uzun top + kaleci dağıtımı + chaos motoru) uygulandı (2026-08-16, Atilla'nın "hepsini
  kapatacak şekilde devam" onayıyla): **üç spec borcu birden kapandı.**
- **ME 7.2 aday kümesi tamamlandı:** `LongSwitch` (25-50 m havadan, koridor kesilemez, bedeli
  inişteki hava rekabeti — Flight=4 → 10.2 zinciri) ve `ClearBall` (kendi savunma bölgesinde,
  baskı arttıkça kısa pastan öne geçer; tehdit terimi bilinçli olarak YOK — iddiası üretim değil
  tehlikeyi kutudan uzaklaştırmak). Tüm katsayılar `balance/sim.balance.json` → `longball.*`.
- **ME 9.4 kaleci dağıtımı:** kaleci top sahibiyken saha oyuncusu aday seti yerine KALECİ seti
  çalışır — KısaAçıl (Kicking düşükse bias) / UzunDegaj (Kicking, pres altında +0,25 bias — 9.4
  sabiti) / ElleAt (hızlı-isabetli, `sigmaCarpan 0,3`). Kale vuruşları da buradan geçer (taker
  zaten kaleci). Kaleci M11'de pas HEDEFLERİNDEN çıkarılmıştı; şimdi "dağıtan" olarak döndü —
  geri pas sorunu geri gelmedi (gol bandı korundu).
- **Hava topu bölge ayrımı (ResolveAerial):** kazanmak her yerde kafa ŞUTU demek değil artık —
  kale menzili dışında kafayla İNDİRİP kontrol. 40 m'den kafa şutu hem saçmaydı hem uzun topu
  kullanılmaz kılardı.
- **ME 13.1-13.3 CHAOS MOTORU — 5 enjeksiyon noktasının TAMAMI, 3 seviyede:** (1) düello marjı
  gürültüsü (DuelWin, DUEL domain — spec'in "yeteneği düşürmez, çözüme bant içi gürültü" ilkesi
  birebir); (2) karar skoru (vardı, seviye tablosuna bağlandı); (3) nişan çarpanı (pas + şut +
  orta + korner + uzun top, PHYSICS); (4) hakem gri bandı (seviyenin TAM bandı; Orta = eski
  `griBantOrta` 0,06 — Orta'da hakem davranışı değişmedi) + VAR "saha kararı kalır" oranı;
  (5) sekme pertürbasyonu (yalnız Yüksek; zemin pertürbasyonuyla sigma toplamı). Seviye
  `MatchConfig.Chaos`'tan gelir (varsayılan Orta — 13.2 "Default"), xG kaydına asla dokunmaz (13.3).
- **Ölçüm — mekanizmalar canlı (Checks kadrosu, Orta):** uzun top 27/maç (kazanma %54) ·
  temizleme 26 · GK kısa 11 / elle 32 / degaj 6,5. Chaos seviyeleri ayrık ve determinist
  (`M16DChaosSeviyeEtkisi`); uzun top kullanımı seviyeyle KENDİLİĞİNDEN artıyor (18→29→66/maç —
  kısa pas riskleştikçe uzun top öne geçiyor, kodlanmış bir kural değil).
- **ME 13.4 upset tablosuna karşı (150 maç/seviye, ayna 75v55):** Düşük %99,3 · Orta %98,7 ·
  **Yüksek %94,7** (hedefler %76/%66/%54). YÖN ilk kez doğru — seviye arttıkça güçlünün oranı
  düşüyor ve Yüksek'te sürpriz+beraberlik ilk kez %5'i geçti — ama büyüklük uzak. Dört ölçüm
  (kDuel, varış hızı, tackle, şimdi yapı+chaos) aynı sonuca işaret ediyor: kalan fark tek
  mekanizmada değil; **M16-E tam kalibrasyonunun işi** (10.000 maç, çok-katsayılı arama).
- **Golden'lar yeniden pinlendi** (davranış değişikliği bilinçli): skeleton `0x300F0587...`,
  M2 `0xD495175A...`, M4 `0xAF634A7C...`, M6 `0x4DFB0413...`. LOD 2 tablosu yeni motorla yeniden
  üretildi (7.840 maç, hücre başına 160 — 80'lik örneklem 0/0 hücresinde gürültü sınırını aşıyordu).
  ME 17.2 bantları korundu: M4 kalibrasyon gol 2,33 · şut 23,5 · korner 7,8 · faul 29,3 · kart 3,67.
- **Yeni kapılar:** `M16DKullanim` (üç mekanizma da kullanılıyor; uzun top kazanma %30-90 bandında)
  · `M16DChaosDeterminizm` · `M16DChaosSeviyeEtkisi` · `M16DUpsetYuksek` (bilinçli sert-eşiksiz:
  40 maçta %5'lik oranın sıfır çıkması %11 tohum şansı — sert eşik M16-E'nin 10k örneklemine ait).

### M16-E: 17.2 tam kalibrasyonu + iki yapısal taraf-asimetrisi düzeltmesi (2026-08-18)
- M16-E uygulandı (Atilla'nın "hepsini kapatacak şekilde devam" talimatı kapsamında): ME 17.2
  tablosunun 13 bandı lig dağılımlı sette (ofset ±12, Chaos domain çekilişi) yeşile çekildi;
  spec borçları kapandı: **ME 12.4 avantaj tehdit koşulu** (`XtAt ≥ referee.avantajXtEsik` —
  eksikken avantaj 28/maç, şimdi ~4,6), **sarı sonrası ihtiyat** (`sariSonrasiIhtiyat` — kırmızıların
  tamamının ikinci sarı olması davranışsal kaynaktandı; oyuncu sarı görünce sertliği kısar),
  **kale önü pas tamponu** (`pass.kaleOnuTamponM` — serbest-gol hastalığının dönüşü %77'den
  bant içine), kaleci `tutmaBoleni` magic-number'ı [KALİBRE]'ye, xG `b0` yeniden kalibrasyonu
  (gol-xG sapması %63→%1-3), `LooseGoalKind` teşhis ayrışımı (9 sınıf — hash dışı).
- **Kalibrasyon paketi (hepsi [KALİBRE]):** `sutTehditCarpan 0,46` · `sutSigmaTabanDeg 24,5` ·
  `tutmaBoleni 260` · `korneriGozeAlmaOran 0,95` · `blokIleriOran 0,65` · `b0 −2,6` ·
  `pTabanMudahale 0,0085` · `logisticSlope 6,0 (geri alındı — gol'e etkisiz, M3 kaleci
  işaretini bozuyordu)`.
- **Yapısal asimetri 1 — santra kuralı eksikti:** rakip forvetler (anchor ±3 m) santra pasını
  0. saniyede basıyordu; kilitlenme çarkıyla (korner→şut→blok→korner) santrayı kullanan takım
  maç boyu eziliyordu. Ayna ölçümü: devre 1 şutları ev/dep 60/145, 20 tohumun 15'i deplasman
  lehine, sd5'te ev 90 dakikada 0 şut. Düzeltme (ME 4.1 DEAD_BALL "santra hazırlığı"):
  santrada herkes anchor dizilişine döner, restart yapmayan takım merkez dairesi
  (`santraDaireM 9,15`) dışına radyal itilir, top `santraHazirlikTicks 50` bekleme dolmadan
  alınamaz; devre arasında pozisyonlar anchor'a reset (15 dk'lık ara simüle edilmez — oyun-içi
  ışınlama yasağının kapsamı dışında). SONUÇ: devre 1 şutları 92/108'e dengelendi ve **M4/M5
  eşit-güç gol kapıları kendiliğinden bant içine döndü** (gol 2,08/2,00 — santra baskını eşit
  maçların golünü de yiyormuş).
- **Yapısal asimetri 2 — karar taramasının yönü:** tek yönlü sabit tarama (0→21), top sahibinin
  aksiyonuna grup içi AYNI-TİCK savunma tepkisini hep deplasmana veriyordu (ev hücumu anında
  cevaplanıyor, deplasman hücumu 1 tick bedava alıyordu). Kanıt: ayna kadroda xG sapması %27
  (dep lehine), tarama TERSİNE çevrilince %9 (ev lehine) — yön mekanizmayı birebir izliyor.
  Düzeltme: tarama yönü tick paritesiyle değişir (5'li kademe tek sayı olduğundan her ajan
  ardışık kararlarında erken/geç konumu sırayla alır). N=120 ayna: sapma %9 (≈0,7σ — gürültü).
  ME 3.2 "sabit sıra" yorumu: sıra yalnız Tick'e bağlı ve yeniden üretilebilir; sırasız yapı
  yasağı ihlal edilmez.
- **Eşit-güç maç ekonomisi bulgusu (ME 13.4 köküyle aynı):** şuta dönüşüm (ŞUT/ATAK) eşit güçte
  %5, +12 farkta %13, +24 farkta %33 — üstünlük zincirde üssel katlanıyor; lig bantları eşit
  maç (~gol 1,8-2,0) ile fark maçlarının (75v55 şut ~45) ortalamasıyla geçiyor. Düz çarpanlarla
  iki uç birden hedeflenemiyor (ölçüldü: şut bandı ile eşit-maç golü aynı kaldıraca ters asılı).
  Konvekslik kararı "Bekleyen kararlar"a yazıldı — M17 dondurmasından önce.
- **10k doğrulaması (`-- calib10k 10000`) — 13/13 bant ✓:** gol 2,48 (2,4-3) · şut 27,4 (20-28) ·
  isabetli 7,4 (7-11) · korner 8,4 (8-12) · faul 18,7 (18-28) · sarı 3,11 (3-5) · kırmızı 0,19
  (0,15-0,3) · penaltı 0,25 (0,2-0,35) · ofsayt 4,9 (2-5) · sakatlık 0,55 (0,35-0,6) · pas isabet
  %81,4 (78-86) · gol-xG sapması %1,4 (±8) · güçlü possession %64,0 (55-65). Gol kaynağı: şut 0,78 +
  serbest top 1,52; kurtarış 5,9/maç; xG/şut 0,089. 75v55 profili (1.380 maç): G/B/M %93/%6/%1 —
  ME 13.4 hedefinden uzak; karar maddesi aşağıda.
- **Golden'lar yeniden pinlendi** (bilinçli — santra kuralı + parite taraması davranışı değiştirir):
  skeleton `0xC668C601...`, M2 `0xFE765787...`, M4 `0x6D67DA53...`, M6 `0xF2301D12...`.
  LOD 2 tablosu yeniden üretildi (7.840 maç); `M15KompozisyonHatasi` %91→%60 (eşik 0,60 altı).
- **Yeni kapı:** `M16ECalibGenis` — ME 17.4'ün iki katmanı: CI'da 500 maç GENİŞ toleransla
  (12 metrik; kadro dağılımı üretici komutla birebir aynı tanım), dar bantlar `-- calib10k 10000`
  üretici komutuyla (sonuç bu kayda işlenir).

### M16-F: derin blok + bloktan kontra — ME 13.4 hibrit kararının uygulaması (2026-08-19)
- **Mekanizma seti (tümü pres01-ölçekli — blok kurulmadıkça eski davranış birebir korunur):**
  (1) **Baskı EMA'sı** `presQ16[2]` — Q16 int (float durum yasağına uygun), top takımın kendi
  `blokBaskiBolgesiM` (22 m) içinde kaldıkça dolar; ASİMETRİK (dolum böleni 64, boşalım 320 —
  blok hızlı kurulur, yavaş çözülür); yalnız açık oyunda birikir; motor-yerel, hash dışı.
  (2) **Hat çökmesi + daralma** (ME 7.6 genişlemesi): `blokCokmeMaxM 14` × pres01 hat kaleye iner,
  `blokDaralmaOran 0,45` × pres01 blok kale eksenine daralır. (3) **Yoğunluk kanalları**: şut
  koridorundaki gövde başına blok olasılığı artışı (`blokEkSavunucu 0,30`, tavan 0,85), şutçu
  üstündeki pres → sigma bozulması (`presSigmaKisiBasi 0,18`), şut KARARINA koridor-kalabalık
  cezası (`sutKoridorCeza 0,34`, taban 0,25 — şut asla tümüyle ölmez). (4) **Bloktan kontra**:
  geçiş penceresi kazananın pres01'iyle uzar (`kontraPresEkSn 6`), kontra tehdit bonusu YALNIZ
  bloktaki tarafa (`kontraBlokBonus 1,0` × pres01 — simetrik büyütme denendi ve geri alındı:
  güçlünün hücumunu da coşturdu, 75v55 şut 47→52). (5) **kDuel 0,9→0,35**: M16-A'da tek başına
  işlemeyen katsayı (eşit maç bozuluyordu), denge mekanizmaları kurulunca ANA kaldıraç oldu —
  eşit maç metrikleri kDuel'e artık duyarsız (ölçüldü: 1,0/0,7/0,5 süpürmesi).
- **ÜSSEL KATLANMA KIRILDI (zincir ölçümü, 60 maç/koşul):** şuta dönüşüm oranı (güçlü/zayıf)
  +12 farkta ×6,1 → ×3,1; +24 farkta ×99 → ×8,8. +24'te güçlünün şutu 57→32, zayıfın golü
  0,20→0,60; eşit maç tam simetrik (×1,01). 75v55 gol profili 4,4-0,18 → 2,8-0,45.
- **ME 13.4 REVİZE hedefe karşı (lig dağılımlı ara-10k, 1.380 fark maçı):** G/B/M **%82/%12/%6**
  (hedef %78/%12/%10) — beraberlik TAM hedefte; başlangıç %93/%6/%1'di. Kaos fixture (200 maç,
  Orta): %88/%8/%4. Düşük chaos %85 (hedef ~%85 ✓), Yüksek %81 (hedef ~%68 — chaos borcu sürer).
- **FİNAL 10k (`-- calib10k 10000`, push edilen konfigürasyon):** 75v55 G/B/M **%84/%11/%6**
  (sürpriz+beraberlik %17; hedef %22). 17.2 tablosu **11/13 ✓**: şut 24,9 · korner 8,4 ·
  faul 20,9 · sarı 3,24 · kırmızı 0,20 · penaltı 0,30 · ofsayt 4,9 · sakatlık 0,56 · pas %80,8 ·
  xG sapması %4,8 · possession %59,3. Bilinçli sapma İKİ bantta: **gol 2,38** (bant 2,4-3;
  −0,02) ve **isabetli 6,9** (bant 7-11; −0,1) — isabet dilimi borcunun kapanışında düzelir.
- **Bilinçli sapma (17.2 dar bantları):** gol 2,3-2,4 sınırında (bant 2,4-3) ve isabetli ~6,8
  (bant 7-11) — derin bloğun DOĞRU sonucu: fark maçlarında şut kalitesi kırpılıyor (isabetli
  14→9,4). Sigma/blok kaldıraçlarının iki yüzeyi (bant ↔ upset) ters oynattığı ping-pong
  ölçümleriyle kanıtlı; kalan mesafe İSABET-ÖZGÜ mekanizma ister (nişan modelinin kaleci
  pozisyonuna bağlanması — ayrı dilim borcu). CI kapısı `M16ECalibGenis` (geniş bant) YEŞİL.
- **Kapı değişimi:** `M16DUpsetYuksek` (eşiksiz muhafız) → `M16FUpsetOrta` (SERT eşik, 200 maç
  Orta: güçlü ≤ %91, sürpriz+beraberlik ≥ %9 — bugünkü gerçek + SE; hedef %78/%22 metinde).
- Golden'lar yeniden pinlendi (bilinçli); LOD 2 tablosu M16-F motoruyla yeniden üretildi.
- **PR incelemesinden iki gerçek bulgu (2026-08-19, Codex + Bugbot; ikisi de doğrulanıp kapatıldı):**
  (1) **GK koridor çifte sayımı** — `CorridorOpponents` şut koridorunda KALECİYİ de sayıyordu;
  kaleci kurtarış zarında (9.2) zaten ayrıca ele alınırken merkezi şutlarda blok yoğunluğuna
  +1 gövde olarak giriyor, isabetli şutu ve kurtarışı sistematik bastırıyordu (M16-F'nin
  "bilinçli sapma" dediği isabetli açığının gerçek kökü BUYMUŞ). Düzeltme: `gkHaric` parametresi
  — ŞUT sayımlarında kaleci atlanır, PAS kesme koridorunda sayılmaya devam eder. Sonuç: gol ve
  isabetli bantları KENDİLİĞİNDEN bant içine döndü; b0 −2,48 ve sutTehditCarpan 0,53 ile hizalandı.
  (2) **Tackle-kazanımlı geçişlerde pencere atlanması** — kazanılan tackle `LastTouchTeam`'i
  tackle yapana çeviriyor, topu takımı toplayınca "sahiplik değişimi" algılanmıyordu: geçiş
  penceresi, MARKAJ ataması (M9'dan beri!) ve M16-F kontra bonusu derin bloğun ANA kazanım
  yolunda hiç işlemiyordu. Düzeltme: değişim algısı dokunuş-takımından SAHİPLİK-takımına
  (`lastOwnerTeam`, motor-yerel) taşındı; `LastTouchTeam` taç/korner hakemliğinin sahibi olarak
  aynen kaldı; santra alımı değişim sayılmaz. İki düzeltme davranış değiştirir → golden'lar
  yeniden pinlendi, kalibrasyon yeniden doğrulandı (bir alt satır).
- **İnceleme turu 2 — üçüncü bulgu (Bugbot, HIGH; doğrulanıp kapatıldı):** sahiplik değişimi
  artık `lastOwnerTeam`'e bakıyordu ama `AwardSetPiece` yalnız `LastTouchTeam`'i sıfırlıyordu —
  santra senkronluyken taç/kale vuruşu/frikik senkronlamıyordu, dolayısıyla RUTİN RESTART'lar
  "açık oyunda top çalma" gibi işlem görüyordu (geçiş penceresi + markaj ataması + M16-F kontra
  bonusu, özellikle kale vuruşlarında). Düzeltme: `AwardSetPiece` de `lastOwnerTeam = forTeam`
  senkronunu yapar. Ölçüm: sahte değişimler sayaçtan temizlendi (sahiplik değişimi 396→347/maç);
  bantlar korundu. **İki kapı bu değişimle bant kenarına düştü ve ÖRNEKLEM BÜYÜTÜLEREK çözüldü
  (bant/tolerans DEĞİŞMEDİ — kapılar güçlendi):** `M3GoalsBand` tek maçta "en az 1 gol" şartı
  0-0'ı hata sayıyordu → 8 tohum ortalaması (1,5-5,0 bandı, tek maç 1-12'den daha sıkı);
  `M5NoRegression` 12→32 maç; `M14SariBandi` 12→32 maç (12'de 5,83 ölçen fikstürün gerçek
  ortalaması 5,25 — küçük örneklem zarı). Kalibrasyon yeniden hizalandı: `sutTehditCarpan 0,57`
  (eşit maç golü), `sutKoridorCeza 0,42` + `blokEkSavunucu 0,36` (pres01-ölçekli — yalnız fark
  maçlarını kırpar), `kDuel 0,35→0,28` (upset kaldıracı; eşit maça duyarsız olduğu ölçülüydü).
- **NİHAİ 10k (push edilen konfigürasyon) — 17.2 tablosu 13/13 ✓:** gol 2,46 · şut 27,4 ·
  isabetli 7,7 · korner 8,5 · faul 20,5 · sarı 3,14 · kırmızı 0,20 · penaltı 0,28 · ofsayt 4,8 ·
  sakatlık 0,50 · pas %81,4 · gol-xG sapması %2,8 · possession %58,2. 75v55 (1.380 fark maçı):
  G/B/M **%83/%12/%6** — revize hedef %78/%12/%10; beraberlik tam hedefte, kalan 5 puan
  isabet/nişan dilimi + Yüksek chaos borcunda. LOD 2 tablosu bu motorla yeniden üretildi.
- **İnceleme turu 1 sonrası 10k (duran top senkronundan ÖNCEKİ konfigürasyon) — 13/13 ✓:** gol 2,41 ·
  şut 26,6 · isabetli 7,5 · korner 8,3 · faul 20,7 · sarı 3,18 · kırmızı 0,20 · penaltı 0,29 ·
  ofsayt 4,9 · sakatlık 0,51 · pas %81,2 · gol-xG sapması %0,0 · possession %59,2. 75v55
  (1.380 fark maçı): G/B/M **%84/%12/%4** — revize hedef %78/%12/%10; beraberlik tam hedefte,
  kalan 6 puan isabet dilimi + Yüksek chaos borcunda.

### M16-G: ME 9.1/9.2 isabet borçları — ve "isabet dilimi upset'i kapatır" hipotezinin ÇÜRÜTÜLMESİ (2026-08-19)
- **Önce teşhis (zincir ölçümü, 60 maç/koşul) — kendi hipotezimi çürüttü.** M16-F kapanışında
  "kalan upset açığı isabet/nişan modelinden gelir" diye yazmıştım. Ölçüm bunun YANLIŞ olduğunu
  gösterdi: +24 farkta **xG/şut ×0,99** (0,101 vs 0,103) — şut KALİTESİ iki taraf için zaten
  eşit; **atak sayısı ×1,14** (182 vs 159) — pozisyon kazanma da neredeyse eşit. Fark tümüyle
  **ŞUT/ATAK ×8,33**'te (0,202 vs 0,024): zayıf takım aynı sayıda atağı şuta çeviremiyor.
  Kök, M16-A'dan beri kayıtlı "atak zincirinin uzunluğu" borcudur — isabet modeli değil.
  Kayıt amacı: bu satır, hipotezin ölçümle reddedildiğini ve upset açığının gerçek adresini
  sabitler (varsayım borcu ikinci kez ödenmesin).
- **Spec borçları yine de kapatıldı (fidelity işi, upset işi değil):**
  (1) **ME 9.1 açıortay hatası** — `sigma_pos = 0,9 × (1 − Positioning/120) m` motorda YOKTU;
  kaleci kusursuz açıortayda duruyordu ve Positioning niteliği kalecinin KENDİ pozisyonlamasında
  hiç kullanılmıyordu. Hata `posHataYenilemeTicks` kovasıyla yavaş değişir (tick başına çekiliş
  kaleciyi titretirdi); DECISION domain (pozisyon alma bir yargı hatasıdır).
  (2) **ME 9.2 direk bandı** — "kesişim direğe < 12 cm" kuralı yoktu (kodda "direk bandı
  M-duran-top/ince ayar" notuyla ertelenmişti). Uygulandı: `direkBandiMm 120`. Sıralama spec'te
  kurtarıştan SONRAdır; burada GEOMETRİ ÖNCE çözülür — gerekçe: event log TEK YÖNLÜDÜR (ME 15.1),
  ShotOnTarget yayımlandıktan sonra "aslında direkti" demek log'u geri almayı gerektirirdi.
  Direği bulan şut isabetli sayılmaz (Opta konvansiyonu); top direkten sahaya seker.
  **Ölçüm: 0,47 direk/maç, şutların %1,7 — gerçek futbolla birebir.**
  (3) **Nişan noktasının kaleciye bağlanması** — şutçu kalecinin BOŞ bıraktığı tarafa nişan alır;
  doğru tarafı seçme olasılığı şut kompozitiyle ölçeklenir. Bu bağ olmadan (1) SONUÇSUZ kalırdı:
  kimse boşluğu kullanmadığı için kalecinin nerede durduğu şutçu için bilgi taşımıyordu.
- **Beklenmeyen sonuç — upset YİNE DE iyileşti:** üçlü mekanizma 75v55 profilini (Kaos fixture,
  240 maç, Orta) **%88/%8/%4 → %82,9/%11,7/%5,4** taşıdı; beraberlik revize hedefe (%12) oturdu.
  Nedeni isabet DEĞİL, eklenen mekanizmaların çok şut atan tarafı orantısal olarak daha çok
  cezalandırması (direk bandı + kaleci hatası varyansı).
- **Kalibrasyon — kaldıraç seçimi ölçümle yapıldı:** gol bandı için önce `sutSigmaTabanDeg`
  denendi (19,0→18,4): gol geldi ama upset %79→%92 fırladı (isabet artışı ÇOK ŞUT ATANA yarar) →
  GERİ ALINDI. Sonra `nisanDogruTaban` yükseltildi (düz kaldıraç, düşük yeteneğe orantısal fayda):
  0,62→0,76 golü getirdi ama upset %82→%85 → orta noktada bırakıldı (0,62). Nihai kaldıraç
  **`gk.saveClampMax 0,96→0,92`**: dominant kaleciye KARŞI şut atan tarafa (yani zayıfa)
  orantısal olarak daha çok yarayan tek düz kaldıraç — gol 2,35→2,44 ✓ ve upset %82,9 KORUNDU.
  0,89 denendi: ek upset faydası YOK, yalnız gol şişiyor → 0,92'de yakınsandı.
- **Kapı:** `M16FUpsetOrta` bugünkü gerçeğe sıkıldı (güçlü ≤ %91→%90, sürpriz+beraberlik ≥ %9→%10).
  `M5NoRegression` 32→96 maç: 1,84 ± 0,15 ölçümü bandın 2,0 tabanını gürültüyle tetikliyordu;
  96 maçta 2,26 — BANT DEĞİŞMEDİ, örneklem büyüdü (kapı güçlendi).
- **NİHAİ 10k (push edilen konfigürasyon) — 17.2 tablosu 13/13 ✓:** gol 2,46 · şut 27,4 ·
  isabetli 7,4 · korner 8,3 · faul 20,4 · sarı 3,14 · kırmızı 0,19 · penaltı 0,29 · ofsayt 4,8 ·
  sakatlık 0,50 · pas %81,3 · gol-xG sapması %2,4 · possession %58,0 · **direk 0,46/maç**.
  75v55 (1.380 fark maçı): G/B/M **%80/%13/%7** — revize hedef %78/%12/%10: galibiyet ve
  beraberlik oranı HEDEFTE, sürpriz payı 3 puan eksik (zincir borcunda). Günün başlangıcı
  %93/%6/%1 idi; M16-F sonrası %83/%12/%6.
- **DÜZELTME (inceleme, Bugbot):** ilk push'ta direği bulan şut `ShotOffTarget` olarak
  yayımlanıyordu ve gerekçe olarak "ME 15.1 tablosu 30 tiple kapalı, uygun tip yok" yazmıştım.
  **Bu gerekçe YANLIŞTI:** `EventType.Post` zaten 15.1 şut zincirinde tanımlı ve penaltı kolu
  onu kullanıyor — ben aramamışım. Açık oyun direği artık `Post` yayımlıyor (tek kaynak).
  StatLine iki tipi zaten aynı dalda sayar (şut evet, isabetli hayır) ve event log hash dışıdır,
  yani düzeltme kalibrasyonu ve golden'ları DEĞİŞTİRMEZ — yalnız log'u dürüstleştirir: açık
  oyun direği artık sıradan bir ıskadan ayırt edilebiliyor (highlight/sunum değeri).
  Spec'e önerilecek bir şey YOK; borç yanlış teşhis edilmişti.

### M16-H: atak zinciri denemesi — ⛔ DENENDİ, ÖLÇÜLDÜ, GERİ ALINDI (2026-08-21)
M16-G'nin adresini kesinleştirdiği zincir açığına üç mekanizmayla girildi; **üçü de zinciri
kıpırdatmadı**, M16-B precedent'i uygulandı (ölçülebilir kazanç olmadan golden yeniden pinlenmez).
- **Yeni teşhis aleti — sahiplik dizisi çıkarımı (olay log'undan):** dizi başına pas · dizinin
  ŞUTLA bitme oranı · dizinin ulaştığı EN İLERİ nokta. **Bulgu (+24 fark, 60 maç):** güçlü
  %23,8 şut / 62,3 m · zayıf **%2,8 şut / 42,4 m** — zayıf takımın ortalama atağı **orta sahayı
  bile geçmiyor**. Kayıp NEDENLERİ iki tarafta neredeyse aynı (kesme ~%36-40, tackle ~%54-61):
  sorun topu kaptırma biçimi değil, İLERLEYEMEMEK. Eşit maçta iki taraf da 51-52 m'ye ulaşıyor
  ve dizilerin %8-10'u şutla bitiyor — bu oran gerçek futbolla uyumlu (~%11), yani model
  şekli eşit maçta DOĞRU; kusur yalnız asimetride.
- **Denenen 1 — derin blokta çıkış noktası:** forvet hatla birlikte çökmesin, yukarıda kalsın
  (otobüsü park eden takım santrforu bırakır). Etki: en ileri nokta 42,7 → 42,4 m. YOK.
- **Denenen 2 — uzun top aday kümesinin ayrılması:** LongSwitch alıcıları kısa pas listesinden
  geliyordu; o liste mesafeye göre EN YAKIN N ile kırpılıyor, uzun topun hedefi ise tanımı gereği
  UZAK. Ayrı ileri-sıralı aday kümesi yazıldı (Vision kapısı korunarak). Etki: uzun top 26 → 41/maç
  (mekanizma ÇALIŞTI) ama en ileri nokta değişmedi. **Kusur gerçekti, sonucu değiştirmedi.**
- **Denenen 3 — LongSwitch pres bonusu** (ClearBall'daki `clearPresBonus` simetriği). Ölçüm
  gösterdi ki iki takım zaten SİMETRİK sayıda uzun top atıyor (17/maç) — "yeterince uzun
  oynamıyorlar" hipotezi de yanlıştı; uzun top zayıfa toprak KAZANDIRMIYOR.
- **Sonuç ve karar:** kalan upset açığı bir ayar veya tek mekanizma işi değil; **pas/sahiplik
  modelinin kendi şekli** (maç başına ~145 dizi × ~3 pas) territoryi TEKRARLA kazandırıyor ve
  tekrar, daha iyi takımı çarpımsal olarak ödüllendiriyor. Bu, DECISIONS'ta M13/M14/M15/M16-A
  borçlarının ortak kökü olarak zaten kayıtlı. Düzeltmek faz ölçeğinde bir yeniden yapılandırmadır
  (tüm golden'lar + tüm kalibrasyon yeniden). **M17 dondurmasının hemen öncesinde, ölçülmüş
  kazancı olmayan bir davranış değişikliğini push etmek projenin kendi kuralına aykırıdır** →
  kod geri alındı, yama `scratchpad/M16H_zincir_denemesi.patch` olarak saklandı.
- **Kalıcı kazanç:** zincir teşhisi `-- calib10k` üretici komutuna KALICI olarak eklendi
  (hash dışı, yalnız üretici modda) — sonraki dilim körlemesine başlamaz.
- **Bugünkü gerçek (M16-G, dondurulacak taban):** 75v55 %80/%13/%7 · revize hedef %78/%12/%10 —
  galibiyet ve beraberlik HEDEFTE, sürpriz payı 3 puan eksik ve adresi yukarıda yazılıdır.

### M17: golden replay seti + config_hash + FAZ 04 arayüz dondurması — ✅ TAMAM (2026-08-21)
FAZ 03'ün son dilimi. Üç parça birlikte kapandı; **FAZ 03 motor tarafı dondu.**
- **ME 3.3 config_hash UYGULANDI** (alan vardı, hiç hesaplanmıyordu): motor sürümü · LOD · tick
  oranı · balance ham bayt özeti · chaos · **hava · zemin · rüzgar (kuantalanmış)** · hakem
  profili · kadro anlık görüntüsü. Hava/zemin/rüzgar 3.3'ün listesinde YOK ama sonucu doğrudan
  değiştiriyor — kimliğe girmezlerse iki farklı kurulum aynı hash'i paylaşır ve spec'in
  "eski replay yeni parametrelerle sessizce oynamaz" güvencesi delinirdi (kapsam genişletmesi
  gerekçesiyle birlikte kodda ve dondurma dokümanında yazılı).
- **Bilinçli sapma (mimari değişmez gereği):** 3.3 "balanceJson_kanonik_bytes" der; `TheBadge.Sim`
  JSON PARSE ETMEZ (CLAUDE.md bağımlılıksızlık kuralı). Ham bayt özeti HOST'ta hesaplanır
  (dosyayı zaten o okur) ve `MatchConfig.BalanceHash` ile verilir. Spec'in amacı birebir korunur:
  balance'taki tek bayt değişikliği config_hash'i değiştirir; çekirdek bağımlılıksız kalır.
- **Golden replay seti (ME 17.4) — 50 arşiv replay:** `shared/TheBadge.Sim.Checks/goldens/
  replay_set_v1.json`; üretici `-- gen-replays`, doğrulayıcı `M17GoldenReplay`. Her kayıt
  replay dörtlüsünün TAMAMINI pinler: config_hash · durum hash'i · skor · süre · **komut izi**
  (`AppliedTraceHash`) · uygulanan/reddedilen komut sayısı. Kurulum çeşitliliği tohumdan türer:
  50 replay hava/zemin/rüzgar/chaos/hakem/kadro-gücü kombinasyonlarını tarar — dondurulan
  sözleşme yalnız "kuru + Orta" değildir. Komut zaman çizelgesi üç aileyi de içerir
  (taktik/motivasyon/değişiklik) ki replay yalnız fiziği değil MÜDAHALE yolunu da pinlesin.
- **Üretici ve kapı TEK KAYNAKTAN kurulur** (`BuildReplay`): "üretici ile kapı farklı evreni
  ölçer" hatası yapısal olarak imkansız (M16-E'de bu ders bir kez ödendi).
- **Bayat set sessizce GEÇMEZ:** `M17ReplaySetiGuncel` balance ham bayt özetini karşılaştırır;
  tutmuyorsa kapı düşer ve yeniden üretim ister (spec: "balance değişikliği yeni golden set").
- **`M17ConfigHashAyirtEdici`:** 9 alanın (sürüm·lod·balance·chaos·hava·zemin·rüzgar·hakem·kadro)
  her birini tek tek değiştirip hash'in gerçekten kaydığını doğrular — "eklendi ama bağlanmadı"
  sessiz hatasına karşı.
- **İnceleme düzeltmeleri (Codex, 3 bulgu — üçü de haklı):**
  (1) **Değişiklik komutu hiç uygulanmıyormuş.** `SubstitutionCmd` sözleşmesi `OutId` = SAHA
  SLOTU (0-10 ev / 11-21 deplasman), `InId` = KULÜBE İNDEKSİ (0-4) ister; golden set PlayerId
  (705/711) geçiyordu → her replay'de 1 reddedilen komut ve **"komut zaman çizelgesi üç aileyi
  de içerir" iddiası GERÇEKLEŞMİYORDU.** Düzeltildi ve ölçülebilir kılındı: `SubsMade` de
  pinlenir. Ölçüm: reddedilen 50 → **0**, değişiklik 0 → **66** (50 replay'in 50'sinde ≥1).
  (2) **Kapı indeks kapsamını denetlemiyormuş:** döngü yalnız DOSYADAKİ kayıtları doğruluyordu;
  kırpılmış ya da yinelenen indeksli bir set "50 replay geçti" diye raporlanabilirdi. Yeni kapı
  `M17ReplaySetiKapsami` 0..49'un tam ve tekil olduğunu ayrıca denetler.
  (3) **`config_hash`'te motor sürümü kırpılıyormuş:** sabit tampona yazım 196 karakterden
  uzun sürümlerde kuyruğu sessizce düşürüyor, `(byte)` daraltması ASCII dışı karakterleri
  örtüşüyordu. Sürüm artık AYRI hash'lenir (UTF-16 kod birimleri, uzunluk önekli, kırpma yok).
  Golden set üç düzeltmeden sonra yeniden üretildi — kapının kendisi bunu zaten zorunlu kılıyor.
- **FAZ 04 arayüz dondurması:** `docs/INTERFACE_FREEZE_FAZ04.md` — giriş noktası, determinizm
  sözleşmesi, veri sözleşmeleri, balance ve LOD sözleşmeleri, dondurma kapsamı DIŞINDA kalanlar
  (teşhis sayaçları, [KALİBRE] değerler) ve **bilinen açık borçlar** (upset 3 puanı, VAR 2 sınıfı,
  LOD 2 kompozisyon hatası, Yüksek chaos). Borçların hiçbiri arayüzü değiştirmez — kapatıldıklarında
  golden set yeniden üretilir, sözleşme aynı kalır.

## FAZ 04 — Core Modüller

### K1: Command Bus çekirdeği — ✅ TAMAM (2026-08-23)
FAZ 04 açıldı (`docs/briefs/BRIEF_FAZ04_ACILIS.md`). **Sıra keyfi değil:** anayasa değişmezi #1
(Tek Kapı) gereği durumu değiştiren her eylem `CommandEnvelope` ile bus'tan geçmek zorunda;
bugün hub tarafında bus YOKTU (yalnız maç içi ucu, ME 14.1). Squad/Transfer/Tycoon bus'tan önce
yazılsaydı ya değişmezi ihlal ederdi ya yeniden yazılırdı → K1 ilk dilim.
- **Yeni paket `shared/TheBadge.CommandBus`** (netstandard2.1, **bağımlılıksız**): UnityEngine
  ve JSON kütüphanesi SIZMAZ — `TheBadge.Sim` ile aynı disiplin. Hem sunucu hem Unity AYNI
  doğrulama kodunu çalıştırır (istemci ön-doğrular, sunucu yeniden doğrular; otorite sunucuda).
- **Katalog v1 — 32 aksiyon** (CB 4.1-4.4, 70 parametre): her aksiyonda tier (0-2), bağlam
  (Hub/Maç/Online bayrağı), rate-limit sınıfı ve parametre tanımları. **Tier katalogda sabittir
  ve kaynaktan bağımsızdır** (CB 6): LLM kaynaklı komut tier'ını asla düşüremez.
- **4 kapılı zincir** (CB 5), deterministik sırayla, ilk hatada durur: (1) katalog+şema — sıkı
  mod: eksik alan, tip hatası, **fazladan alan**, enum dışı, metin >40, kontrol karakteri hepsi
  `SchemaViolation`; (2) parametre bandı — 46 bant `balance/command.bands.json`'dan, **bant
  anahtarı tanımsızsa sessizce GEÇMEZ**, yapılandırma hatası da reddir; (3) bağlam/sahiplik/
  kaynak/hak — `IValidationContext` arayüzü (uygulamaları K2-K5 ile gelir; bus modüllere
  bağımlı olmaz, modüller bus'a bağlanır); (4) rate limit — CB 5.1 sınıf tablosu, kayan pencere.
- **JSON sınırı — ME 3.3 `BalanceHash` deseninin aynısı:** çekirdek JSON parse etmez; host ham
  payload'ı ayrıştırıp `IPayloadView` (alan adları + tipli okuyucular) olarak verir, şema
  sıkılığı çekirdekte denetlenir. Böylece "spec JSON Schema diyor ama çekirdek bağımlılıksız
  kalmalı" gerilimi mimari değişmezi bozmadan çözülür.
- **Rate limit:** sınıf başına ÇOKLU pencere (Economic 20/dk **ve** 200/saat), kullanıcı yalıtımı,
  AbuseFlag (5 dk içinde 3 red → denetim loguna sinyal, CB 5.1). **LLM kaynağı sınıfı düşürmez,
  EKLER:** LLM'den gelen komut hem kendi sınıfının hem ModB penceresinin limitindedir.
  Zaman DIŞARIDAN verilir (`DateTime.Now` yok) — test edilebilirlik + determinizm.
- **Idempotency (CB 8.1):** `CommandId` 24 saatlik pencerede; aynı Id ikinci kez YÜRÜTÜLMEZ,
  önceki yanıt aynen döner. **Tasarım notu:** idempotency doğrulamadan ÖNCEdir — yeniden
  doğrulamak, aradaki durum değişimi yüzünden aynı komuta farklı yanıt üretebilir ve retry'yi
  güvensiz kılardı. RED de idempotenttir (düzeltilmiş payload'la aynı Id gelirse eski red döner).
- **İnceleme düzeltmeleri (Codex, 8 bulgu — SEKİZİ DE haklı, üçü P1):**
  (1) **İstemci saati güveniliyordu (P1).** Rate limit penceresi ve idempotency süresi zarfın
  `IssuedAtUnixMs` alanıyla çalışıyordu; her partiyi ileri tarihli göndererek sayaç sıfırlanabilirdi.
  Artık HOST'un alış saati (`receivedAtUnixMs`) ayrı parametredir; `IssuedAtUnixMs` yalnız metadata.
  (2) **Idempotency atomik değildi (P1).** "Önce bak, sonra sakla" deseninde iki eşzamanlı RPC
  aynı `CommandId`'yi birlikte yürütebiliyordu ve `Dictionary` senkronsuzdu — iddia edilen
  exactly-once sağlanmıyordu. Artık `TryReserve → yürüt → Complete` (kilitli); ikinci eşzamanlı
  çağrı `DuplicateCommand` alır, çöken rezervasyon uçuş süresi sonunda devralınır.
  (3) **Rate limit kabulü atomik değildi (P1).** Paralel patlama hepsi birden boş kapasite görüp
  limiti aşabiliyordu. Denetim+kayıt tek kilit altında.
  (4) **Yürütücüsüz `Submit` SAHTE BAŞARI üretiyordu (P1).** Durum değişmediği hâlde "başarılı"
  sonuç idempotency deposuna yazılıyor, gerçek yürütücüyle yapılan retry bu sahte başarıyı tekrar
  oynatıyordu. Yürütücü artık ZORUNLU (kablolama hatası görünür patlar); yalnız doğrulama için
  ayrı `Validate` API'si var ve o rate limit hakkını TÜKETMEZ.
  (5) **Bağlam denetimi birleşik bayrakla yapılıyordu.** Hem hub hem maç geçerli bir aksiyon
  (ör. `squad.set_player_role`) maç damgasıyla gelse bile "hub açık" olduğu için geçiyordu.
  Artık zarfın seçtiği bağlamla aksiyon bayraklarının KESİŞİMİ denetlenir.
  (6) **`CommandSource.Auto` hiç doğrulanmıyordu.** CB 2.2 "AUTO v1'de kapalı" derken `Auto`
  zarfı UI komutu gibi yürüyebiliyordu. Artık `Auto` ve tanımsız enum değerleri reddedilir.
  (7) **Denetim kaydı yürütmeden SONRA ayrı çağrılıyordu** — audit yazımı başarısız olursa durum
  audit'siz kalır ve CB 5.2'nin "hep ya da hiç" sözleşmesi delinirdi. `AuditRecord` artık
  yürütücüye geçer, yani durum/event ile AYNI transaction'da kalıcı olur; `IAuditSink` yalnız
  durum değiştirmeyen sonuçlar (redler, tekrar oynatmalar) içindir.
  (8) **Maç içi limit takım kapsamlı değildi.** CB 5.1 "10/dk/TAKIM" derken anahtar yalnız
  `UserId`'ydi: takımı paylaşan kullanıcılar limiti birlikte aşabiliyor, farklı takımları yöneten
  kullanıcı gereksiz kısılıyordu. `MatchCmd` sınıfında takım kimliği anahtara girdi.
  Sekizi de ayrı kapıyla sınanır (`K1IncelemeDuzeltmeleri`) — eşzamanlılık bulguları gerçek
  `Parallel.For` altında ölçülür.
- **İKİNCİ inceleme turu (Bugbot, düzeltme commit'i üzerinde — 2 bulgu, ikisi de haklı):**
  (9) **Maç içi limit HÂLÂ takım kapsamlı değildi.** İlk düzeltmede takım kimliğini anahtara
  EKLEDİM ama `userId`'yi ÇIKARMADIM — yani aynı takımı yöneten iki kullanıcı yine ikişer pencere
  alıyordu; spec "10/dk/TAKIM" diyor, "kullanıcı+takım" değil. Ayrıca zarftaki `TeamIdx` kararlı
  bir takım kimliği DEĞİL (yalnız ev/deplasman). Çözüm: kimliği HOST üretir
  (`IValidationContext.ResolveTeamKey`) ve MatchCmd anahtarında kullanıcı kimliği YER ALMAZ.
  (10) **Rezervasyon devralması çift yürütmeye açıktı.** Uçuş süresi dolunca rezervasyonu
  "çökmüş sayıp" devralıyordum; ama ilk çağrı hâlâ `Execute` içindeyse İKİ yürütme birden durum
  değiştirebilirdi — exactly-once iddiası, tam da onu korumak için yazdığım kolda deliniyordu.
  Ayrıca `Complete`/`Release` sahiplik denetimsizdi: gecikmiş bir çağrı başkasının sonucunu
  ezebiliyor ya da rezervasyonunu silebiliyordu. Çözüm: **otomatik devralma KALDIRILDI**
  (canlılık uğruna güvenlik feda edilmez; asılı rezervasyon yalnız `Prune` ile, operatör
  denetiminde açılır) + **sahiplik jetonu** (jeton eşleşmeyen `Complete`/`Release` no-op).
- **Ders (kayıt için):** bus'ı tek iş parçacıklı bir zihinle yazmışım; hedefi paralel RPC işleyen
  bir sunucu. İki inceleme turunun 10 bulgusunun 6'sı doğrudan eşzamanlılık/güven sınırı
  konusuydu. K6 (Nakama köprüsü) bu dersle başlar.
- **Kapılar (8):** `K1KatalogTamligi` (32 aksiyon, her bantlı parametrenin bandı balance'ta VAR) ·
  `K1SemaSikiligi` (7 senaryo) · `K1BantZorlamasi` (**59 bantlı parametrenin TAMAMI** alt sınırda
  reddediliyor — tek tek değil, katalog taranarak) · `K1BaglamKapisi` (maç↔hub ayrımı) ·
  `K1RateLimit` · `K1Idempotency` · `K1IncelemeDuzeltmeleri` (8 bulgu) · `K1RedDeterminizmi` (aynı girdi=aynı
  sebep, kapı sırası, tier kaynaktan bağımsız).

### K2: Dünya durumu çekirdeği — ✅ TAMAM (2026-08-24)
`shared/TheBadge.World` (netstandard2.1, **bağımlılıksız** — Sim ve CommandBus dışında referans
yok). K1 kapıyı kurdu; K2 kapının ARDINDAKİ durumu kurar: kulüp, kadro, finans, takvim.

**Neden Atilla'nın kararını BEKLEMEDİ (ve nesi hâlâ bekliyor):** brief §4.1 "dünya durumu nerede
yaşar" diye sordu. Kayıtları okuyunca sorunun büyük kısmının ZATEN kapalı olduğu görüldü —
**D3 (2026-07-30): G3, sunucu-otoriter**, gerekçesi rekabetçi bütünlük + çok oyunculu ligler;
GDD 6.3 "maç motoru online liglerde asla oyuncunun cihazında çalışmaz"; GDD 11.2 "komut
doğrulama .NET C# servis katmanında koşar". Üstelik CB 8.3 offline modu için "aynı doğrulama
zinciri YEREL `IValidationContext` ile çalışır; **kod tek, davranış özdeş**" diyor. Yani
durum çekirdeği HER İKİ seçenekte de aynıdır ve `shared/`te yaşamak zorundadır; kararın
etkilediği şey yalnız **otorite bağlaması ve kalıcılık** (Nakama/Postgres vs yerel save) —
o da K6'nın kapsamı. K2 mimari-nötr olanı yazdı, K6'ya ait olanı yazmadı.
→ Atilla'ya kalan gerçek soru daralttı: **offline kuyruğun uzlaştırma politikası** (bağlantı
dönünce yerel Tier 0 kuyruğu sunucu durumuyla çeliştiğinde ne olur). Bekleyen kararlara işlendi.

- **Determinizm disiplini sim'den ithal:** kalıcı alanların TAMAMI tamsayı (para tam ₺ — ME 3.2'nin
  mm kuralının ekonomi karşılığı; ECONOMY_MAP'in ₺K'sı SUNUM birimidir), diziler kanonik sırada
  (oyuncular PlayerId artan), sırasız yapı (Dictionary/HashSet) yok — arama ikili arama.
  `WorldHash` = xxHash64, açık little-endian, ME 3.2 `StateHash` deseninin dünya karşılığı.
- **Olay logu TEK YÖNLÜ:** `WorldEvent` listesi dünya mantığı tarafından ASLA okunmaz ve hash'e
  GİRMEZ (ME 15.1'de maç logu için kurulan kuralın aynısı). `StateVersion` de hash'e girmez:
  aynı durumu farklı komut yollarıyla üreten iki save eşit hash'li olmalıdır — versiyon durum
  değil MUHASEBEdir (CB 8.2 delta sync).
- **Atomiklik journal ile (CB 5.2):** handler durumu doğrudan değiştirmez, yazmalarını
  `WorldJournal`a kuyruklar; yürütücü önce ÖN DENETİMDEN geçirir (hedef var mı, değer aralıkta mı),
  sonra uygular. Tek geçersiz yazma varsa HİÇBİRİ uygulanmaz. Sonuç: "yarım yazılmış durum" bir
  hata değil, **yapısal olarak ulaşılamaz bir hâl** — geri alma koduna gerek kalmıyor.
- **Kapı 3 sahiplik üç ayrı ilişkidir:** kendi oyuncuna rol verirsin (`Sahip`), BAŞKASININ
  oyuncusuna teklif yaparsın (`Yabanci`), SERBEST oyuncuyla imzalarsın (`Serbest`). Tek bir
  "oyuncu bizim mi" kuralı bu üçünü birden yanlış cevaplardı; tablo aksiyon bazında yazıldı.
- **K3-K5 seami:** K2 yalnız DURUMA dayalı yapısal denetimleri yapar; hesaplanan bedeller
  (inşaat maliyeti, sponsor, personel) `IActionRule`/`IActionHandler` ile modüllerden gelir.
  **K2 bilmediği bir bedeli tahmin etmez** — kaynak denetimi yalnız payload'ın tutarı AÇIKÇA
  bildirdiği aksiyonlarda yapılır (`tycoon.repay_loan.miktar`, `transfer.propose_offer.bedel`).
- **K1 derslerinin taşınması:** (a) *sahte başarı yok* — doğrulamayı geçmiş ama handler'ı
  bağlanmamış aksiyon "oldu" diye raporlanmaz (idempotency deposu o yalanı tekrar oynatırdı);
  `UnboundActions()` kablolama boşluğunu istek anında değil AÇILIŞTA gösterir. (b) *eşzamanlılık* —
  yürütme tek kilit altında serileştirildi; kilit kaldırılınca kapı gerçekten kırmızıya döndü
  (`StateVersion 396≠400`), yani kapının dişi ölçülerek doğrulandı.
- **Balance:** `balance/world.balance.json` — yapısal sınırlar (inşaat/kredi slotu, tesis sayısı,
  kadro min/max, sezon haftası, maç başına değişiklik). **Ekonomik katsayı YOK**: maliyet/gelir/faiz
  ECONOMY_MAP sözleşmesine göre K3 balance sprintinde gelir. Transfer penceresine tabi aksiyon
  LİSTESİ de kodda değil bu dosyada — hangi işlemin pencereye tabi olduğu tasarım kararıdır ve
  K5'te kesinleşir; kod değişmeden ayarlanır.
- **Kapılar (10):** `K2DurumKanonik` · `K2HashKapsami` (30 kalıcı alanın HER BİRİ hash'i oynatıyor,
  StateVersion oynatmıyor) · `K2Kapi3Sebepleri` (NotOwned×4 · WindowClosed · InsufficientFunds×2 ·
  NoChargesLeft · StateConflict×5 · bağlam) · `K2KuralSeami` · `K2Atomiklik` · `K2SahteBasariYok` ·
  `K2YurutmeDeterminizmi` (25 komut × 2 koşu bit-eşit) · `K2Eszamanlilik` (8 iş parçacığı × 50) ·
  `K2BalanceZorlamasi` (7 bozuk yapılandırma reddi) · `K2TekKapiUctanUca` (bus→kapı 3→yürütme;
  idempotency durumu ikinci kez DEĞİŞTİRMİYOR).
- **Açık uç:** 32 aksiyonun hiçbirinin handler'ı yok (tasarım gereği — K3-K5'in işi). Bu bir
  eksiklik değil sözleşme; `UnboundActions()` sayısı K3-K5 ilerledikçe düşer ve ilerlemenin
  ölçüsüdür.

### K1 güvenlik turu — ✅ TAMAM (2026-08-24)
Cursor Security Agent PR #16'da iki MEDIUM buldu; ikisi de haklı çıktı.

**(A) Kota kimliği istemci zarfından okunuyordu.** Kapı 4 rate limit'i (ve AbuseFlag'i)
`env.UserId` ile anahtarlıyordu. Bu, *kendi verdiğim kararla tutarsızdı*: `IssuedAtUnixMs`'i
"istemci verisi, güvenilmez" diye host saatiyle değiştirmiş, `TeamIdx`'i "kararlı kimlik değil"
diye `ResolveTeamKey`e taşımıştım — ama aynı zarfın ASIL kota kimliğini olduğu gibi bırakmıştım.
Zarfı deserialize edip `Submit` çağıran bir host, `UserId`'yi oturumdan ezmezse çağıran her parti
için yeni kimlik uydurup Tactic/Economic/OnlineSocial/ModB pencerelerini döndürebilir, AbuseFlag'i
başkasının kimliğine yapıştırabilirdi.
- **Çözüm:** `authenticatedUserId` artık `Submit`/`Validate`/`Validator.Validate`'in **ZORUNLU**
  parametresi (`receivedAtUnixMs` deseninin aynısı — host'un unutabileceği varsayılan bırakılmaz).
  Zarfın iddiası oturumla ayrışıyorsa **kapı 1'de** `NotOwned` ("kimlik uyuşmazlığı") ile düşer:
  bu bir zarf BÜTÜNLÜĞÜ önkoşuludur, ondan sonrası değerlendirilmez. `env.UserId` denetim
  metadata'sı olarak kalır — değeri, artık uyuşmazlığın YAKALANABİLİR olmasıdır.

**(B) Doğrulamada düşen komutlar 24 saatlik kayıt açıyordu, bus depoyu hiç budamıyordu.**
Rezervasyon doğrulamadan ÖNCE alınır (bilinçli: retry'yi yeniden doğrulamak aradaki durum
değişimi yüzünden aynı komuta farklı yanıt üretirdi). Yan etkisi: şema/bant/bağlam redleri
Kapı 4'e hiç ulaşmadığından rate limit tüketmez, ama yine de kayıt açardı; benzersiz
`CommandId`'li bozuk payload seli paylaşılan belleği sınırsız büyütebilirdi.
- **Çözüm iki parçalı:** (1) dedup penceresi artık kayıt başına — **YÜRÜTÜLEN** komutlar için
  24 saat, yürütmeye ulaşmamış redler için 10 dk [KALİBRE `idempotencyRedDk`]. "Red de
  idempotenttir" sözleşmesi gerçek retry ufkunda korunur, sel maliyeti o ufka iner. (2) bus
  `Submit`te amorti edilmiş budama yapar [KALİBRE `idempotencyBudamaDk` = 5 dk]. Budama ASILI
  rezervasyonlara DOKUNMAZ — devralma yasağı operatör denetiminde kalır (ikinci tur kararı).
- **Triyaj notu:** botun aynı bulguda geçen "her kullanıcı için o Id'leri blokler" kısmı pratikte
  önemsiz (CommandId Guid; çakışma olmaz). Gerçek risk BELLEK BÜYÜMESİdir; kapı da onu ölçüyor.

- **Kapı:** `K1GuvenlikTuru`. Dişi ölçüldü — kimlik denetimi kaldırılınca 6 ayrı iddia birden
  düştü (uyuşmazlık geçiyor · döndürme pencereyi aşıyor · uyuşmayan zarf limiter'a ulaşıyor);
  budama + kısa pencere kaldırılınca sel 201 kayda çıktı. Kota kimliğinin OTURUMDAN geldiği
  `SpyRateLimiter` ile doğrudan ölçülüyor (bus AbuseFlag'i kendisi tükettiği için sonradan
  sorgulamak yanıltıcı olurdu — ilk yazdığım kapı bu yüzden yanlış kırmızı verdi, düzeltildi).
- **Ders:** bir güven sınırı kararını verirken (istemci verisi → host verisi) o sınırdan geçen
  TÜM alanları birlikte taramalıydım. İki alanı taşıyıp üçüncüsünü bırakmak, kararın kendisini
  değil yalnız iki örneğini uygulamak demek.

**(C) Kimlik denetimi KISA DEVRE yollarında geçerli değildi** (üçüncü MEDIUM, aynı gün, benim
(A) düzeltmemin içinde). Denetimi kapı 1'e koymuştum; oysa `Submit` idempotency kısa devresinde
(`Completed`/`InFlight`) `Validator.Validate`e HİÇ ulaşmadan dönüyor. Yani sözleşme tam da onu
atlayan yollarda yoktu. Kayıt yalnız `CommandId` ile anahtarlı olduğundan sonuç bilgi sızıntısından
ibaret de değildi: aynı Guid'i kullanan İKİNCİ kullanıcının komutu hiç çalışmadan ötekinin
önbellekli yanıtını alıyor, yani sessizce hiçbir şey yapmadan "başarılı" görünüyordu.
- **Çözüm iki katmanlı:** (1) depo anahtarı artık **(KULLANICI, CommandId)** — başka bir oturumun
  kaydına erişmek yapısal olarak imkânsız, uçuş durumu da yoklanamaz; (2) kimlik denetimi
  `Submit`in EN BAŞINA, rezervasyondan önce alındı (denetim `Validator`da DA duruyor: ön-doğrulama
  yolu için gerekli — savunma iki katmanlı). Kendi retry'si idempotent kalıyor.
- **Ders (öncekinin devamı):** bir denetimi "kapı 1'e koydum" demek, o kapıya ULAŞMAYAN yolların
  denetimsiz olduğunu ölçmediğim sürece yeterli değil. Erken dönüş yolları da sözleşmenin
  parçasıdır.

### K2 inceleme turu — ✅ TAMAM (2026-08-24)
Bugbot K2'de 5 bulgu çıkardı (3 HIGH, 2 MEDIUM); beşi de haklı. İkisi tam da raporumda en
kendinden emin anlattığım yerlerdeydi — atomiklik garantisi ve "üç ayrı sahiplik ilişkisi" tablosu.

1. **(HIGH) Journal ön denetimi zincirlemeyi atlıyordu.** `Validate` her yazmayı DEĞİŞMEMİŞ duruma
   karşı bakıyordu, `Apply` ise sırayla zincirliyordu. Aynı alana iki delta (moral +30, +30; taban
   60) tek tek bantta görünüp zincirde 120 yazıyordu — yani "aralık taşması sessizce kırpılmaz"
   garantisi, tam da onu veren metnin altında deliniyordu. Artık her yazma, kendinden önceki aynı
   hedefli yazmalar katlandıktan sonraki değere karşı denetleniyor; her ARA sonuç da bantta.
   Tarama O(n²) — journal birkaç yazmalık (TeamSheet.Validate precedent'i).
2. **(HIGH) Kapı 3 yürütmeyle yarışıyordu.** Bağlam durumu KİLİTSİZ okuyor, yürütücü kendi kilidi
   altında yazıyordu: iki paralel komut aynı bakiyeyi "yeterli" görüp ikisi de yürütülebiliyordu.
   Yürütme kilidi yazmaları serileştiriyor ama KARARI korumuyordu (klasik TOCTOU). İki parçalı
   çözüm: (a) `WorldStore` — durum ve onu koruyan kilit tek sahipte, okuyanla yazanın aynı kilidi
   paylaşması konvansiyon değil YAPISAL zorunluluk; (b) yürütücü kilit içinde Kapı 3'ü YENİDEN
   denetliyor. Bu, projenin "istemci ön-doğrular, sunucu yeniden doğrular" ilkesinin bir katman
   aşağıya uygulanmasıdır. `gate3` zorunlu parametre — unutulabilir varsayılan bırakılmıyor.
3. **(HIGH) `OwnerNeed.Yabanci` serbest oyuncuyu geçiriyordu.** Kural "bizim değilse geçer" diye
   yazılmıştı; serbest oyuncunun (ClubId 0) bedel teklif edilecek bir kulübü yok, yolu
   `sign_free_agent`. Üç ilişkiyi ayırdığımı yazmışım ama üçüncüsünü ikincinin içinden
   çıkarmamışım. Artık `Yabanci` = BAŞKA BİR KULÜBÜN oyuncusu.
4. **(MEDIUM) `kadroMax` hiç zorlanmıyordu.** Yükleme anında doğrulanıp kullanılmıyordu; hiçbir şey
   yapmayan bir yapılandırma anahtarı, olmayan anahtardan daha kötüdür — var sanılır.
   `transfer.sign_free_agent` artık tavanı denetliyor.
5. **(MEDIUM) Audit fırlarsa bellek ilerlemiş kalıyordu.** Yorumum bunu host'un veritabanı
   rollback'ine havale ediyordu ama bellek o rollback'in parçası değildi: "hep ya da hiç" bir
   MEKANİZMAYA değil bir VARSAYIMA dayanıyordu. `WorldJournal.Geri` eklendi (Apply öncesi değerler
   saklanır, ters sırayla geri yazılır, `StateVersion` geri alınır); sink fırlarsa geri alınıp
   istisna yukarı bırakılıyor.

- **Kapılar:** `K2ZincirlemeYazma` · `K2AuditGeriAlma` · `K2Kapi3Yarisi` + `K2Kapi3Sebepleri`
  genişletildi (NotOwned×5, StateConflict×7).
- **Kendi testimin dişsiz çıkması (kayda değer):** ilk yazdığım `K2Kapi3Yarisi` yarışı ŞANSA
  bırakıyordu — tekrar denetimini kaldırdığımda kapı YEŞİL kaldı, yani hatayı yakalamıyordu.
  `BarrierContext` ile 8 komutun Kapı 3'ü BİRLİKTE geçmesi garanti edildi; şimdi tekrar denetimi
  kaldırılınca her koşuda kırmızı (8 komut geçiyor, kasa **-700**). **Ders: eşzamanlılık kapısının
  dişi ölçülmeden yazılmış sayılmaz** — zamanlamaya bağlı bir test, hata varken de yeşil kalar.

### FAZ 04 açık kararları — ✅ KAPANDI (2026-08-25, Atilla: "önerilerini kabul ediyorum")
Üç madde de önerilen biçimde karara bağlandı. İkisinin KOD karşılığı vardı; ikisi de uygulandı.

**1. Offline kuyruk uzlaştırma → sunucu kazanır, düşen komutlar RAPOR EDİLİR.**
Bağlantı dönünce yerel Tier 0 kuyruğu sunucu durumuyla çelişirse sunucu otoritedir (D3/G3), ama
düşen komutlar sessizce yutulmaz — kullanıcıya hangi komutun neden düştüğü gösterilir. Gerekçe
CB 8.2'nin kendi ilkesi: "sessiz üzerine yazma YOKTUR; kullanıcı her zaman net sonuç görür."
Elenen (a) sunucu kazanır + sessiz düşer → kullanıcının emeğini görünmez şekilde siliyordu;
elenen (c) komut bazlı birleştirme → Tier 0 zaten geri alınabilir olduğu için maliyetini hak
etmiyor. **Bu bir K6 politikasıdır** (Nakama köprüsü); K2-K5'te kod karşılığı yok, K3-K5'i
bloklamıyor. Uygulaması K6 diliminde.

**2. Komut bantları config_hash kapsamına girer → UYGULANDI.**
`balance/command.bands.json` ham bayt özeti artık `ConfigHash.Compute`'un ZORUNLU üçüncü
parametresi (`MatchConfig.CommandBandsHash`). Gerekçe: bantlar hangi komutun KABUL edildiğini
belirler → replay dörtlüsünün dördüncü üyesi olan komut zaman çizelgesini belirler. Bant değişip
hash sabit kalsaydı aynı çizelge farklı oynar ve 3.3'ün "eski replay yeni parametrelerle sessizce
oynamaz" güvencesi delinirdi. M17'nin `BalanceHash` deseni (özeti host hesaplar, çekirdek JSON
parse etmez) ikinci dosyaya birebir genişletildi. **Varsayılan parametre BIRAKILMADI** — unutulabilir
bir varsayılan, bant değişikliğinin kimliğe sessizce girmemesi demekti (bu oturumun tekrar eden dersi).
- **Sonucu:** kapsam genişleyince golden replay seti geçersizleşti ve YENİDEN ÜRETİLDİ (50 replay;
  `bandsHash 0x03BEA30B618B4B08` sete pinlendi). Bu bir yan hasar değil, config_hash'in var olma sebebi.
- **Kapılar:** `M17ReplaySetiGuncel` iki özeti birden denetliyor; `M17ConfigHashAyirtEdici` 10 alana çıktı.

**3. Katalog sürüm politikası → aksiyon ekleme MINOR, parametre/bant değişikliği MAJOR.**
`Catalog.Version` notu kesinleşti. Politika TEMENNİ değil, iki mekanizmayla ZORLANIYOR:
- **KOD ayağı:** `Catalog.ShapeHash()` — aksiyon sayısı, her aksiyonun adı/tier/bağlam/sınıfı, her
  parametrenin adı/tipi/zorunluluğu/bant anahtarı/uzunluğu/enum değerleri. `K1KatalogSurumKilidi`
  pinli sabitle karşılaştırır; değişince kapı düşer ve "MINOR mu MAJOR mu" kararını yüzünüze çıkarır.
- **VERİ ayağı:** bant DEĞERLERİ katalogda değil `command.bands.json`'da; o dosya da 2. karar
  sayesinde config_hash kapsamında → değer değişikliği golden seti bayatlatıyor.
  **İki karar birbirini tamamladı:** 2. maddenin uygulaması, 3. maddeye veri tarafında diş verdi.

**Dişler ölçüldü:** gerçek bir bant DEĞERİ değişikliği (`tycoon.biletFiyat` 500→600) ve gerçek bir
katalog PARAMETRE değişikliği (`fiyat` required true→false) ayrı ayrı denendi; her biri kendi
kapısını kırmızıya döndürdü (parametre değişikliğini ayrıca `K1SemaSikiligi` ve `K1BantZorlamasi`
da bağımsız yakaladı — savunma derinliği çalışıyor).

### K3-A: Tycoon ekonomi çekirdeği — ✅ TAMAM (2026-08-25)
`shared/TheBadge.World/src/Economy/` + `balance/economy.balance.json`. GDD 4.2/4.4'ün ekonomisi,
sözleşmesi `docs/ECONOMY_MAP.md`.

- **Haftalık tick:** seyirci → bilet/büfe/mağaza; sezon başı kombine (peşin); yayın + sponsor +
  maç primi; maaş/bakım/personel/işletme; kredi (4 haftada bir); inşaat ilerlemesi. TEK KAPI'ya
  uyar — tick durumu doğrudan değiştirmez, `WorldJournal`a yazar.
- **Kredi muhasebesi:** FAİZ sink'tir, ANAPARA değildir — anapara bilanço aktarımıdır, para
  yaratmaz. ECONOMY_MAP yalnız "kredi faizi"ni sink sayıyor; anapara sink'e girseydi kredi çekmek
  source/sink oranını yapay olarak bozardı. `WeekLedger` bunu ayrı alanda tutuyor.
- **Kalibrasyon (tek tur):** ilk ölçüm source/sink **1,548** ve maaş payı **%70,2** ile bant
  dışıydı. Maaş sabit tutulup maaş-dışı sink'ler yükseltildi (bakım 26k→45k, personel 165k→420k,
  işletme 210k→570k ₺/hafta). Sonuç: **source/sink 1,133 ∈ [1,05-1,15]** · **maaş payı %51,2 ∈
  [%45-60]**. Referans kulüp (30k kapasite, 22 kişilik kadro, tier 3 stadyum) fixture'dır; kalibre
  edilen balance dosyasıdır — fixture'ı oynatarak bant tutturmak ölçümü anlamsız kılardı.
- **Kapılar (7):** `K3EkonomiSozlesmesi` (10 sezon, iki bant) · `K3EkonomiDeterminizmi` ·
  `K3SeyirciModeli` (fiyat/form yönü + kapasite ve min doluluk sınırları) · `K3InsaatIlerlemesi` ·
  `K3KrediAmortismani` · `K3IflasEgrisi` (kötü yönetim sezon 2 ∈ [2,3]; iyi yönetim 6 sezon ayakta) ·
  `K3RngGauss01Borcu` (aşağıdaki bulgunun gözcüsü).

### ~~🔴 BULGU: `Rng.Gauss01` komşu tick'lerde ve bit-0 farklı seed'lerde AYNI değeri üretiyor~~ → ✅ KAPANDI (2026-08-30)
K3'ün seyirci varyansı seed'e duyarlı çıkmayınca ortaya çıktı; kök sebep FAZ 03 kodunda.

- **Mekanizma:** `Gauss01` 12 çekilişi `[16·salt, 16·salt+12)` salt aralığında topluyor. Bu küme
  bit-0 ve bit-1 çevirmeleri altında **kapalı**: `z = seed ^ … ^ (tick<<1) ^ salt` olduğu için
  seed'in bit-0'ını veya tick'in ilgili bitini çevirmek, salt'ları yalnız kendi aralarında yer
  değiştiriyor. Çokluk kümesi aynı kalıyor → toplam aynı (yalnız toplama sırası değişiyor).
- **Ölçüm (2000 örnek):** aynı çekiliş kümesi — **komşu tick %50,0**, **bit-0 farklı seed %100,0**.
  (Tam kayan nokta eşitliği %26,9 / %55,3; toplama sırası son bitleri ayırdığı için tam eşitlik
  gerçek bağımsızlık kaybını OLDUĞUNDAN KÜÇÜK gösteriyor.) `Rand01`'de çarpışma **sıfır** —
  kusur toplama deseninde, çekirdek hash'te değil.
- **Etki alanı:** maç motorunda **13 çağrı yeri** (fizik, karar, düello, şut nişanı), hepsi
  `st.Tick` anahtarlı. Determinizm bozulmuyor (sonuç hâlâ tekrarlanabilir) ama gürültü
  tasarlandığından çok daha bağımlı: ardışık tick'lerin yarısı aynı Gauss değerini alıyor.
- **DÜZELTİLMEDİ (bilinçli):** düzeltme 50 golden replay'i ve M16-E'nin 12 metriğini kaydırır →
  ayrı dilim + yeniden kalibrasyon. K3 kendi gürültüsünü `Rand01` tabanlı simetrik üniformdan
  alıyor (genlik `sigma·√3`, sd korunur) ve `K3RngGauss01Borcu` kapısı borcu ekranda tutup
  KÖTÜLEŞMESİNİ engelliyor (M15 kompozisyon borcunda kurulan desen).
- **Öneri:** salt aralığını çarpışmayan bir dizilime taşımak (ör. `salt*16 + i` yerine
  `salt*0x9E3779B1 + i*0x85EBCA6B` gibi tek sayı adımlı bir yayılım). Tek satırlık değişiklik,
  maliyeti düzeltmenin kendisi değil ARDINDAN gelen yeniden kalibrasyon. Kararı Atilla verir.

### K3-B: 9 tycoon aksiyonu — ✅ TAMAM (2026-08-25)
CB 4.1'in dokuz aksiyonu bağlandı: fiyatlar (bilet/kombine/büfe/mağaza), inşaat başlat/iptal,
kredi al/öde, sponsor imzala. `WorldExecutor.UnboundActions()` 32'den **23**'e düştü — kalan 23
K4-K7'nin işi ve sayı ilerlemenin ölçüsü olmayı sürdürüyor.

- **K2'nin bıraktığı boşluk kapandı:** K2 "bilmediği bedeli tahmin etmez" deyip hesaplanan
  maliyetleri seame bırakmıştı. K3 onları `economy.balance.json`tan getirip Kapı 3'e bağladı:
  inşaat maliyeti + `hedefTier = mevcut+1` (CB 4.1 tablosu), kredi slot doluluğu, sponsor teklif
  geçerliliği, fazla ödeme.
- **Fiyat birimi KURUŞ:** `command.bands.json` büfe fiyatını [0,5 - 50] ₺ ile tanımlıyor, yani
  kesirli fiyat meşru; kalıcı durum ise tamsayı olmak zorunda (ME 3.2). Tek birim seçildi —
  bilet tam ₺, büfe kuruş olsaydı dönüşüm hatası kaçınılmazdı.
- **Kimlik üretimi deterministik:** yeni inşaat/kredi kimliği "mevcut en büyük + 1"dir; sayaç ya
  da `Guid` kullanılmaz — aynı durumdan aynı komut aynı kimliği üretmeli (CB 5.2).

**CB 10.1 negatif matrisi — 9 aksiyon × 4 senaryo = 36.** Senaryolar KATALOGDAN mekanik türetiliyor
(bant dışı değer, ilk bantlı parametreden hesaplanıyor); elle yazılmış 36 vaka bir aksiyonu sessizce
atlayabilirdi, tarama atlayamaz.

**Kapı yazarken iki ders çıktı, ikisi de ölçümle:**
1. **Rate limit senaryosu durumlu aksiyonlarda Kapı 4'e hiç ulaşmıyordu.** Aynı komutu 21 kez
   YÜRÜTÜNCE inşaat/kredi/sponsor 2. denemede meşru bir `StateConflict` veriyor ve kapı 4 hiç
   çalışmıyor. Rate limit'i sınamak durumu sabit tutmayı gerektiriyor → senaryo yürütmeden
   doğrulamaya çevrildi.
2. **Kapı 3 senaryosu yalnız sebep koduna bakıyordu ve bu YETMİYORDU.** Dişini ölçerken görüldü:
   kredi slot ve fazla ödeme kuralları KAPATILDIĞI HÂLDE kapı yeşil kalıyordu — çünkü daha derin
   katmanlar (handler'ın kendi denetimi, journal'ın aralık koruması) aynı `StateConflict`i
   üretiyordu. Savunma derinliği çalışıyordu ama sınanmak istenen kapı sınanmıyordu. Çözüm:
   her Kapı 3 senaryosu ayrıca `Validate` (yürütmesiz) ile doğrulanıyor — aynı sebebi vermesi,
   reddin doğrulama zincirinden çıktığının kanıtı. Dört kural da kapatılıp yeniden ölçüldü;
   şimdi dördü de yakalanıyor.
- **Kapılar (3):** `K3TycoonBaglanti` · `K3TycoonMutluYol` (9 aksiyonun durum etkisi) ·
  `K3NegatifMatris` (36 senaryo + kapı 3 köken denetimi).

### K3 inceleme turu — ✅ TAMAM (2026-08-29)
Codex, PR #17 incelemeye açılınca dört bulgu çıkardı (2 P1, 2 P2); dördü de haklı.

1. **(P1) İnşaat harcaması hiçbir sink kalemine girmiyordu.** Handler kasadan düşüyordu ama
   `WeekLedger.ToplamGider`'in inşaat bileşeni yoktu — oysa ECONOMY_MAP "inşaat + tesis bakımı"nı
   açıkça sink sayıyor. Sonuç: inşaat İÇEREN bir sezonun source/sink oranı olduğundan İYİ
   görünürdü ve kalibrasyon kapısı sözleşmeyi ihlal eden bir balance'ı onaylayabilirdi.
   Çözüm: `ClubState.DonemInsaatGideriTl` biriktiricisi — komut harcamayı biriktirir, haftalık tick
   `WeekLedger.InsaatTl`e boşaltıp sıfırlar; iptal iadesi biriktiriciyi geri çeker.
   **Çift muhasebe tuzağı:** bedel komut anında kasadan zaten düşüyor, bu yüzden `InsaatTl`
   `NetTl`e GİRMEZ — yalnız SINK RAPORUdur. (Kredi anaparasında verilen kararın kardeşi.)
2. **(P1) Sponsor sözleşme süresi imzada siliniyordu.** `SureHafta` teklifle birlikte temizleniyor,
   tick ise `SponsorHaftalikTl`i her hafta süresiz ödüyordu: 1 haftalık anlaşma sonsuza dek gelir
   yazıyordu. Çözüm: `ClubState.SponsorKalanHafta` — imzada taşınır, tick'te azalır, sıfırlanınca
   gelir temizlenir ve `SponsorSonaErdi` olayı basılır (taban sponsora dönülür).
3. **(P2) Teklif geçerliliği sezon dönüşünü aşıyordu.** Karşılaştırma yalnız HAFTAydı; tick sezon
   sonunda haftayı 1'e sardığı için S1H10'da biten teklif S2H1-H10 arasında yeniden geçerli
   oluyordu. Çözüm: `SonGecerlilikSezon` eklendi, karşılaştırma (sezon, hafta) çiftiyle.
4. **(P2) Sponsor imzası `FiyatGuncellendi` olayı basıyordu.** Hiçbir fiyat değişmiyor; tip 8'i
   fiyat bildirimine yönlendiren tüketiciler aksiyonu yanlış raporlar ve sözleşme güncellemesini
   hiç almazdı. Çözüm: `SponsorImzalandi` (11) ve `SponsorSonaErdi` (12) eklendi — kendi kuralıma
   uyarak SONA, mevcut değerler yeniden kullanılmadan.

- **Kapı:** `K3IncelemeBulgulari` + `K3TycoonMutluYol`a sponsor olay tipi denetimi. Dördü de
  ters çevrilip ölçüldü; her biri kendi iddiasını kırmızıya döndürüyor.

**5. (Bugbot) `K2HashKapsami` kendi iddiasını yanlışlamıştı.** Kapı "30 kalıcı alanın HER BİRİ
hash'i oynatıyor" diyordu; K3 yedi yeni kalıcı alan ekledi (Form, sponsor haftalık/kalan hafta,
dönem inşaat gideri, fiyat dizileri, teklif alanları) ve mutasyon listesi elle bakımlı olduğu için
GERİDE KALDI — kapı yeşil raporlamayı sürdürürken kapsam iddiası artık doğru değildi. Alanlar
hash'te vardı, ama kapı onları ÖLÇMÜYORDU: yarın biri hash'ten çıkarsa kimse fark etmezdi.
- **Çözüm listeyi uzatmak DEĞİL:** beklenen alan kümesi artık YANSIMAYLA türetiliyor (kalıcı durum
  tiplerinin tüm public alanları; `StateVersion` bilerek dışarıda). Mutasyonu olmayan bir alan
  kapıyı DÜŞÜRÜYOR, fazladan bir mutasyon da. Kapsamı hatırlamak insana bırakılmadı. 30 → **46 alan**.
- **Diş ölçümü:** mutasyonsuz yeni bir alan eklendi → kapı düştü ("MUTASYONU YOK"); mevcut bir alan
  hash'ten çıkarıldı → kapı düştü ("hash oynamadı"). İkisi de doğru mesajla.
- **Ders:** "her X" diyen bir kapı, X'in listesini elle tutuyorsa iddiası zamanla yanlışlanır.
  Liste türetilebiliyorsa türetilmeli; bu, bu turda öğrenilen en genellenebilir şey.

**6. (Bugbot) Çift muhasebe koruması ATEŞLENEMİYORDU.** İnşaat bulgusunu düzeltirken yazdığım
"kasa çift düşmesin" iddiası `kasa > taban + gelir` biçimindeydi. Oysa inşaat `NetTl`e girseydi
kasa DÜŞERDİ — yani iddia, korumak için yazıldığı hatanın yönüne bakmıyordu ve hiçbir koşulda
ateşlenemezdi. Bulguyu düzeltirken ölü bir koruma yazmışım.
- **Çözüm:** beklenen kasa hareketi ledger kalemlerinden `NetTl` KULLANILMADAN kuruluyor
  (`ToplamGelir - opex - anapara`; `InsaatTl` kasıtlı dışarıda) ve TAM EŞİTLİKLE karşılaştırılıyor.
  Bağımsız hesap olduğu için `NetTl`in tanımı bozulursa sapma görünür.
- **Diş ölçümü:** `NetTl` kasten çift muhasebeye çevrildi → kapı doğru teşhisle düştü:
  `kasa hareketi ledger'la tutmuyor (6129600 ≠ 10329600; inşaat 4200000 çift sayılmış olabilir)`.
- **Ders:** bir koruma yazarken "hangi YÖNDE sapar?" sorusunu sormadan eşitsizlik kurmak, ölü
  iddia üretiyor. Belirsizlikte eşitsizlik değil TAM EŞİTLİK kur; tam eşitlik yanlış yöne de
  duyarlıdır. (Bu turda kapıların kendi zayıflığı ÜÇÜNCÜ kez bulundu: zamanlamaya bağlı yarış
  kapısı, derin katmanlarca maskelenen Kapı 3 kapısı, ve şimdi ölü eşitsizlik.)
- **Kapsam notu:** referans kulüp senaryosu inşaatsız kaldı (bilinçli). ECONOMY_MAP'in 1,05-1,15
  bandı SÜREKLİ işletme dengesi hakkında; inşaat yığınsal sermaye harcamasıdır ve 10 sezonluk
  ortalamaya karıştırmak bandın anlamını değiştirir. Ledger artık her senaryoyu doğru ölçüyor;
  bandın capex'i kapsayıp kapsamayacağı balance sprintinin sorusu (bekleyen kararlara işlendi).

### K4: Kadro yönetimi — 9 aksiyon + hub/maç ayrımı — ✅ TAMAM (2026-08-29)
Kadro katmanı Tek Kapı'ya bağlandı: **9 aksiyon** (`squad.set_player_anchor`, `set_player_role`,
`set_instruction`, `set_team_tactic`, `save_tactic_preset`, `set_captain`, `set_training_plan`,
`match.substitution`, `match.motivation_talk`). Bağlanmamış aksiyon 32'de 23 — kalanı K5-K7'nin işi.

- **Kalıcı durum eklendi:** `TacticState` (mentalite/tempo/pres/hat, 0-100 MUTLAK), `TacticPreset[]`
  (20 yuva, GDD 3.3 "Özel Kaydetme"), `PlayerState.Talimatlar[]` (4 yuva), `ClubState.KaptanPlayerId`,
  `AntrenmanPlanId`, `AntrenmanYogunluk`. Sözlük değil DİZİ: sırasız yapı yasağı (ME 3.2) ve hash
  kanonikliği. Preset adı sunum verisidir ama senkronun parçası — hash'e ham metin değil DİZE ÖZETİ
  girer (`WorldHash.DizeOzeti`, UTF-16 kod birimi + uzunluk öneki, kırpma yok).
- **Delta → mutlak dönüşümü:** katalogda `set_team_tactic` DELTA verir ([-2,+2], CB 4.2); kalıcı durum
  MUTLAKtır. Dönüşüm `taktik.adim` [KALİBRE] ile yapılır ve 0-100'e kırpılır — kod içinde adım
  sabiti YOK.
- **Hub/maç ayrımı:** 7 hub aksiyonu kalıcı durumu düzenler ve maç kuyruğuna SIZMAZ; taktik/değişiklik/
  motivasyon maç bağlamında ME kuyruğuna gider ve kalıcı durumu KORUR. Kuyruksuz host maç aksiyonunu
  reddeder (sessiz yutma yok).

**ME arayüz boşluğu (yeni borç, ME 14.2).** CB 4.2 tablosu `set_player_anchor`/`set_player_role`/
`set_instruction`'ı "Hub + Maç" diye listeliyor, ama `MatchCommands.cs`'te anchor ve rol komutu YOK ve
`PlayerInstr` bir taslak (`None = 0`, katalog boş). Üç seçenek vardı: (a) maçta sessizce yut — Tek
Kapı'nın "her komutun cevabı var" ilkesini kırar, istemci UI'ı yanlış gösterir; (b) ME komut kümesini
bu dilimde genişlet — kapsam kayması, determinizm kapısı + golden replay etkisi; (c) maç bağlamında
KAPI 3'ten açık sebeple reddet, borcu görünür tut. Seçilen: **(c)**. Reddin kapı 3'ten gelmesi bilinçli:
komut yürütücüye hiç ulaşmadan düşer, ön-doğrulama da aynı cevabı verir. Kapatma dilimi: ME komut
kümesi genişletmesi (K5-K7 sonrası, `PlayerInstr` kataloğuyla birlikte).

- **Kapılar:** `K4Baglanti`, `K4HubYolu`, `K4MacYolu`, `K4MeArayuzBoslugu`, `K4CifteKayit`.
- **Diş ölçümü (üçü de ters çevrilip ölçüldü):** maç reddi kaldırılınca 3 aksiyon maçta sessizce
  geçiyor ve İKİSİ maç sırasında kalıcı durumu oynatıyor (`durum oynadı`); çifte kayıt koruması
  kaldırılınca ikinci kural ve ikinci yürütücü sessizce kabul ediliyor; kapı 3 detayı düşürülünce
  üç aksiyon da "sebep açıklanmıyor"a düşüyor.

**`K2HashKapsami` yine kendi işini yaptı.** Yansımaya çevrilmiş kapsam kapısı — K3 turunda tam da bu
sebeple yansımaya çevrilmişti — K4'ün altı yeni kalıcı alanını mutasyonsuz yakaladı, sonra üçünün
(`KaptanPlayerId`, `AntrenmanPlanId`, `AntrenmanYogunluk`) `WorldHash`'te HİÇ olmadığını gösterdi:
alanlar duruma girmişti, hash'e girmemişti. Elle bakımlı liste bunu bir daha kaçıracaktı.

**Konteyner geri dönüşümü (süreç notu).** Bu dilim bir kez kaybedildi: oturum konteyneri geri
dönüştürülünce `faz04/squad` dalı (henüz itilmemişti) ve tüm K4 kaynağı diskten silindi, .NET SDK'sı
da kurulu değildi. Kaynak oturum transkriptinden geri kazanıldı, SDK `packages.microsoft.com`
deposundan kuruldu. **Ders: yeşil kapıya ulaşan her dilim BEKLETMEDEN itilir** — yerel dal yedek değildir.

### K4 inceleme turu — ✅ TAMAM (2026-08-29)
PR #18 incelemeye açılınca **iki bağımsız inceleyici** (Codex + Bugbot) dört bulgu çıkardı; dördü de
haklı, ikisini ikisi birden gördü. Ayrıca kendi ters-okumamda çıkan iki şeyden biri (talimat şeridi)
Codex'inkiyle aynı çıktı — bağımsız iki yoldan aynı yere varmak bulgunun gerçekliğini pekiştirdi.

1. **(P1 ×2) Değişiklik hakkı maç başına DOLMUYORDU.** `KalanDegisiklikHakki` yalnız dünya
   kurulumunda doluyor, sonra YALNIZ azalıyordu. Alanın kaynağı `macBasinaDegisiklik`, yani adı
   "her maç" diyor — ama hiçbir yol hakkı geri doldurmuyordu: bir save'de toplam N değişiklikten
   sonra HER maç `NoChargesLeft` ile reddedilirdi. Çözüm: `MacTick.Basla(st, kural, j)` — maç
   yaşam döngüsü kancası, `EconomyTick` ile AYNI sözleşme (durumu doğrudan değiştirmez, journal'a
   yazar, host `Validate` + `Apply` yapar). Bunu önce gözcü kapı olarak yazmıştım; inceleme
   "belgele" değil "düzelt" dedi ve haklıydı — borç değil, hatanın kendisiydi.
2. **(P1 ×2) Maç komutu yürütme işleminin DIŞINDAydı.** Handler `Apply` içinde doğrudan
   `kuyruk.Enqueue` yapıyordu; sonraki journal doğrulaması ya da denetim sink'i patladığında
   `Geri` yalnız `GameState`i geri alıyor, komut kuyrukta KALIYORDU — ve tekrar denemede İKİNCİ
   kopya giriyordu. Çözüm: komutlar `WorldJournal`da BEKLETİLİR (`MacKomutu`), yürütücü denetim de
   geçtikten SONRA boşaltır (`MacKuyruguBagla`). Yayınlama artık commit'in parçası; her erken dönüş
   ve her `Geri` yolu komutları yayınlanmamış bırakır. Bu, K2'nin "yarım yazılmış durum yapısal
   olarak erişilemez" ilkesinin maç kuyruğuna uzatılmasıdır.
3. **(High) Bus, motorun reddedeceği değişikliği kabul ediyordu.** Balance 5 hak veriyordu ama
   `MatchEngine.MaxSubs` **3**'tür (derleme zamanı sabiti — `PendingSubs` dizisini boyutlandırıyor,
   yani balance'tan okunması determinizm etkili bir motor değişikliğidir). Dünya "oldu" der, motor
   sessizce `RejectedCommands++` yapardı. Çözüm: `macBasinaDegisiklik` 5 → **3** ve
   `MacTick.HakTavaniTutarli` + kapı ikisinin eşitliğini zorunlu tutuyor. Bu bir balance KALİBRASYONU
   değil düzeltmedir: 5 bugün fiziksel olarak onurlandırılamıyordu.
4. **(P2) Ayrışık talimat şeridi sessizce başka oyuncuya yazardı.** `TalimatHandler` adresi
   oyuncunun KENDİ dizi uzunluğuyla düzleştiriyor, `WorldJournal` ise `Oyuncular[0]`ın uzunluğuyla
   çözüyordu. Diziler bir gün ayrışırsa yazma başka oyuncunun yuvasına düşerdi. Çözüm İKİ yönlü:
   handler artık çözücüyle AYNI şeridi kullanıyor **ve** `GameState.Validate` tekdüze yuva sayısını
   zorunlu kılıyor — çözücünün varsayımı artık doğrulanmış bir değişmez.

- **Kapı:** `K4IncelemeBulgulari` (4 bulgu tek kapıda). Dördü de ters çevrilip ölçüldü; her biri
  kendi iddiasını kırmızıya döndürüyor — reset kapatılınca "sonraki maçta değişiklik reddedildi
  (NoChargesLeft)", tavan 5'e çekilince "hak tavanı ayrışık(balance 5 vs motor 3)", `Enqueue`
  işleme geri alınınca "geri alınan komut kuyrukta KALDI(1)", şerit denetimi kapatılınca ayrışık
  dizi kabul ediliyor.

**Açık kalan (K6'ya):** motor, değişikliği kendi kurallarıyla da reddedebilir (kulübe indeksi,
kırmızı kart görmüş oyuncu, yanlış taraf, aynı oyuncu iki kez) ve bu redler yalnız
`RejectedCommands`'ı artırır — istemciye "oldu" denmiş olur. Hak tavanı artık tek kaynak olduğu için
en sık yol kapandı, ama motor sonucunun host üzerinden istemciye geri akması (komut sonucu geri
bildirimi) K6 maç yaşam döngüsünün işidir. Dünya katmanının motorun tüm kurallarını kopyalaması
yanlış çözüm olurdu: iki kaynak, iki ayrışma.

### K5: Transfer pazarı — 5 aksiyon + değerleme + pazarlık — ✅ TAMAM (2026-08-29)
GDD 17 FAZ 04'ün "Valuation algoritması, negotiation logic, kontrat sistemi" kalemi. **5 aksiyon**
bağlandı (`transfer.list_player`, `propose_offer`, `respond_offer`, `sign_free_agent`,
`release_player`). K5 kapısı yalnız transfer modülünü bağladığı için orada 27/32 bağlanmamış
görünür; K3+K4+K5'i birlikte bağlayan bir host'ta kalan **9**'dur (32 − 9 tycoon − 9 kadro/maç −
5 transfer). *(Düzeltme: bu satır önce "18" diyordu — K3'ün 9 tycoon aksiyonunu saymayı atlamıştım;
PR #19 açıklamasında da aynı yanlış sayı geçti.)* Kalan 9: `staff.hire`, `staff.activate_premium`,
`social.arrange_talk`, `social.press_response`, `social.report_player`, `league.create`,
`league.join`, `league.set_rules`, `replay.share_clip`.
Kalanı K6-K7'nin işi.

- **Durum:** `PlayerState`'e değerleme girdileri (`Guc`, `Potansiyel`, `Yas`, `IstenenBedelTl`) ve
  `ClubState`'e `TransferOffer[8]` [KALİBRE]. Maç motoru bu alanları HENÜZ okumuyor (ME ajan
  durumunda karşılıkları yok) — transfer değerlemesinin girdisidirler, ama kalıcı durumun parçası
  oldukları için hash kapsamındadırlar.
- **Değerleme:** dışbükey güç eğrisi × potansiyel primi × yaş çarpanı × sözleşme çarpanı, hepsi
  `balance/transfer.balance.json` [KALİBRE]. Sözleşmesi biten oyuncu ucuzlar (yakında bedelsiz);
  yaş çarpanının ALT sınırı var (yaşlı oyuncunun değeri sıfıra inmez — devredilebilir bir varlıktır);
  potansiyel < güç ise prim 0'dır, CEZA değil (tavanına ulaşmış oyuncu cezalandırılmaz).
- **Pazarlık:** `Rng.Rand01` + `Domain.Decision`. Domain gerekçesi: bu bir AJAN KARARIdır (satıcı
  kulübün tutumu), fiziksel olay değil; `Chaos` reddedildi çünkü kaos akışı maç içi sapma içindir ve
  transferi oraya bağlamak iki alanı aynı sayaç uzayında çakıştırırdı. `Gauss01` KULLANILMADI —
  çakışma borcu açık (bekleyen kararlar), K3 ekonomi tick'iyle aynı gerekçe.
- **Kontrat:** fesih bedeli = kalan hafta × maaş × çarpan, asgari tabanlı. Oyuncu göndermek bedava
  değildir; aksi hâlde maaş yükü sıfır maliyetle atılırdı (GDD 4.2).

**Kadro sınırları: otoritenin YERİNİ yanlış sandım, diş ölçümü düzeltti.** Önce "K2'nin
kullanılmayan `kadroMax`ı K5'te gerçek oluyor" diye yazmıştım — **yanlış**. K2 inceleme turunda o
bulgu ZATEN kapatılmış: `WorldContext` (5a) `sign_free_agent` için tavanı, (5b) `release_player`
için tabanı uyguluyor. Bunu, kadro tabanı kuralımı kaldırıp kapının kırmızıya dönmesini
beklerken buldum — **dönmedi**, çünkü fesih döngüsünü durduran benim kuralım değil K2'ydi.
Sonuç: `SerbestKurali` tamamen, `FesihKurali`'nin taban denetimi de kaldırıldı (ölü koddu);
`FesihKurali`'de yalnız K2'nin BİLEMEYECEĞİ şey kaldı — payload'da bildirilmeyen, HESAPLANAN
fesih bedelinin karşılanabilirliği (K3-K5 seami).

**Gerçek boşluk bu arayışta çıktı:** K2 sınırları `sign_free_agent` ve `release_player` için
koyuyor ama **`respond_offer` için KOYMUYOR** — oysa teklif kabulü de kadroyu büyütür (alış) ve
küçültür (satış). O sınır K5'in kendi işi ve artık `CevapHandler`'da; `K5PazarlikDongusu` iki yönü
de ölçüyor. Tavan senaryosu standart fikstüre sığmadığı için (26 oyuncu, kadroMax 32) kendi
fikstürünü kuruyor.

**Ölü kod bulgusu — kendi kuralımı K2 maskeliyordu.** `TeklifKurali`/`ListeKurali`/`SerbestKurali`
içine sahiplik denetimleri yazmıştım; `WorldContext`'in K2'den gelen üç ilişkili sahiplik katmanı
(`OwnerNeed.Sahip/Yabanci/Serbest`) bunları ZATEN eliyor ve KAPI 3'te benden ÖNCE koşuyor. Yani
yazdığım denetimler erişilemezdi. `ListeKurali` tamamen kaldırıldı, diğer ikisinden sahiplik
kısımları çıkarıldı; kapılar da artık otoritenin verdiği sebebi (`NotOwned`) ölçüyor, benim
kopyamı değil. Bu, K3-B'de öğrenilen "derin katmanın maskelediği kural" tuzağının aynısı.

**Kapı yazarken kendi hatamı buldum, ama kapı onu YAKALAMIYORDU.** `CevapEnum` sırası
{kabul, ret, karsiTeklif}, `PazarlikKarari` sırası {Ret, Kabul, KarsiTeklif} — AYNI DEĞİL; indeksi
doğrudan enum'a cast etmek **kabul ile reddi takas ediyordu** (ret oyuncuyu satıyor, kabul hiçbir şey
yapmıyordu). Hatayı okurken yakalayıp düzelttim, sonra diş ölçümünde geri koyduğumda **suite yeşil
kaldı**: `K5MutluYol` yalnız sıra denetimini ölçüyordu, transferin KENDİSİNİ değil. `K5PazarlikDongusu`
bu boşluğu kapatmak için yazıldı — kabul gerçekten oyuncuyu gönderiyor mu, ret gerçekten kapatıyor mu.
Geri koyunca artık kırmızı: "RET oyuncuyu GÖNDERDİ(kulüp 900)". **Ders: en kritik yolu ölçmeyen
kapı, o yolda yapılan hatayı da ölçmez — sıra denetimini test etmek transferi test etmek değildir.**

**Maaş muhasebesi hatası (kendi okumamda bulundu).** Alış yolunda kulübün maaş yüküne
`yeniMaas - eskiMaas` ekliyordum; oyuncu bizim DEĞİLDİ, eski maaşı başka kulübün gider kaleminde
duruyordu. Pahalı kulüpten ucuz maaşa alınan oyuncuda gider yükü EKSİK görünürdü. Tam maaş eklenir.
Diş ölçümü: geri koyunca `alışta maaş yükü TAM maaş kadar artmadı(80000≠120000)`.

- **Kapılar (7):** `K5Baglanti` · `K5DegerlemeMonotonlugu` · `K5PazarlikDeterminizmi` (100 tekrar
  bit-eşit, seed ayrıştırıyor) · `K5MutluYol` · `K5SahiplikVeKadro` · `K5PazarlikDongusu` ·
  `K5NegatifMatris` (CB 10.1: şema·bant·kapı3·rate).
- **Diş ölçümü (5):** maaş muhasebesi · kabul yolunun kadro sınırları · sıra denetimi · potansiyel
  primi · kabul/ret eşlemesi — beşi de ters çevrildiğinde kendi iddiasını kırmızıya döndürüyor.
- `K2HashKapsami` yine yeni alanları mutasyonsuz yakaladı (5 alan) — üçüncü dilim üst üste.

**Bu dilimin taşınacak dersi:** ÜÇ kez aynı tuzağa düştüm — kendi kuralımı yazdım, daha derin bir
katman (K2) onu zaten yapıyordu, ve kural ÖLÜ kaldı. Üçünde de fark ettiren şey aynı: **kapıyı ters
çevirip kırmızıya dönmesini beklemek.** Dönmediğinde kapı zayıf değil, kural gereksizdi. Bundan
sonra yeni bir `IActionRule` yazmadan önce `WorldContext`'in o aksiyon için ne yaptığına bakılır.

### K5 inceleme turu — ✅ TAMAM (2026-08-30)
PR #19'da iki bağımsız inceleyici (Codex + Bugbot) **altı** bulgu çıkardı; altısı da haklı, ikisini
ikisi birden gördü. Biri (serbest oyuncu maaş talebi) inceleme gelmeden önce kendi okumamda
bulunmuş ve düzeltilmişti — aynı yere iki yoldan varmak bulgunun gerçekliğini pekiştirdi.

1. **(P1 ×2) Süresi dolmuş teklif yuvayı SONSUZA DEK kilitliyordu.** Cevap yolu süresi dolmuş
   teklifi reddediyordu — **ret dahil**; iptal aksiyonu yok, haftalık tick temizlemiyor. Sekiz
   teklif süresi dolduktan sonra kulüp bir daha teklif VEREMEZDİ. Çözüm üç parçalı: (a) yeni teklif
   ararken süresi dolmuş yuva GERİ KAZANILIR, (b) süresi dolmuş teklife **ret** verilebilir (sıra
   beklemeden — kapatmanın kullanıcı yolu), kabul/karşı teklif hâlâ reddedilir, (c) süresi dolmuş
   teklif "açık teklif" SAYILMAZ; sayılsaydı bir kez süresi geçen teklif o oyuncuya bir daha
   teklif vermeyi engellerdi. (c)'yi kapının kendisi yakaladı: (a)'yı yazdıktan sonra kapı
   "yuva geri kazanılmadı(bu oyuncuya açık teklifin zaten var)" dedi.
2. **(P1 ×2) Kabul, sahipliği YENİDEN denetlemiyordu.** `respond_offer` KAPI 3'te `OwnerNeed.Yok`;
   yön yalnız `TeklifEdenClubId`den çıkarılıyordu. Teklif açıkken oyuncu satılmış ya da feshedilmişse
   BAYAT teklif artık bizde OLMAYAN birini taşır, bedeli kasaya yazar, maaş defterini bozardı.
   Teklif "kim teklif etti"yi söyler, "oyuncu ŞU AN kimde"yi söylemez. Çözüm: kabul anında satışta
   oyuncu HÂLÂ bizim, alışta HÂLÂ yabancı olmalı.
3. **(P1) Serbest oyuncu maaş talebi yoktu** — 90 güçlü bir oyuncu haftalık ₺0'a imzalanabiliyordu
   ve yeni eklenen `maasTalepOran` hiçbir işe yaramıyordu (`Valuation.MaasTalebi` ölü koddu).
   Kendi okumamda bulunup düzeltilmişti; `SerbestMaasKurali` talebi zorunlu kılıyor.
4. **(P2) Kardeş teklifler transfer sonrası CANLI kalıyordu.** Kabul yalnız seçilen yuvayı
   kapatıyordu; `release_player` teklif dizisine hiç dokunmuyordu. Aynı oyuncu ikinci kez
   satılabilir ya da artık gitmiş biri için bedel ödenebilirdi.
5. **(P2) Kadrodan çıkan oyuncu KAPTAN kalıyordu.** Satış ve fesih `ClubId`i değiştiriyor ama
   `KaptanPlayerId`e dokunmuyordu: hash'lenen kalıcı durum kadroda olmayan birini kaptan gösterirdi.
6. **(P2) Teklif kimliği katalog bandını aşıyordu.** `SonrakiTeklifId` "açık yuvaların en büyüğü + 1"
   idi. PR açıklamasında bunu "yuvalar boşalınca sıfırlanır, sorun değil" diye NOT olarak yazmıştım —
   **yanlış**: teklifler ÇAKIŞIRSA max hiç sıfırlanmaz, kimlik sınırsız büyür ve 4096'yı aşan teklif
   OLUŞTURULABİLİR ama `transfer.teklifId` bandı yüzünden asla CEVAPLANAMAZ. Çözüm: en küçük
   kullanılmayan kimlik, tavan `yapi.transferTeklifIdMax` [KALİBRE] ve bir kapı bu tavanın katalog
   bandıyla AYNI kalmasını zorluyor (K4'ün `MaxSubs` dersinin kardeşi).

- **Kapı:** `K5IncelemeBulgulari` (6 bulgu tek kapıda). Dördü ters çevrilip ölçüldü — yuva geri
  kazanımı kapatılınca "yuva geri kazanılmadı(teklif yuvası dolu)", sahiplik denetimi kalkınca
  "bayat satış kasaya para yazdı", kardeş/kaptan temizliği kalkınca "kardeş teklif 1 açık kaldı ·
  satışta kaptanlık düşmedi", eski kimlik algoritması geri konunca "teklif kimliği bandı aştı
  (4097 > 4096)".

**Açık kalan (K6'ya): karşı tarafın cevabını YÜRÜTEN yok.** `propose_offer` her zaman
`SiraTeklifEdende = false` yazıyor; World modülünde hedef kulübün sırasını ilerleten bir tick ya da
AI tüketicisi YOK, yani kullanıcının açtığı teklif kendi başına kabul/ret/karşı teklif ALAMAZ —
`Valuation.Karar` bugün yalnız zaten var olan bir karşı teklifi işlerken çağrılıyor. Alış akışı
uçtan uca değil. Bu bilinçli: karşı taraf BAŞKA BİR KULÜP ve onu kimin sürdüğü henüz verilmemiş bir
karara bağlı — online ligde başka bir oyuncu (K6, Nakama), offline'da bir AI kulüp tick'i
(`EconomyTick`/`MacTick` kardeşi). İkisinin tahkimini değerleme ve pazarlık MANTIĞInı kapsayan bir
dilim içinde seçmek kapsam kaymasıydı; pazarlık beyni burada ve test edilmiş durumda, sürücü K6'da.
(Codex bunu PR #19'da P1 olarak işaretledi; gerekçeyle K6'ya bırakıldı, thread açık.)

**Bu turun dersi:** PR açıklamasına "biliyorum ama önemsiz" diye yazdığım bir NOT (kimlik tekilliği)
gerçek bir hataydı — gerekçem "yuvalar periyodik olarak boşalır" varsayımına dayanıyordu ve o
varsayım hiçbir yerde ZORLANMIYORDU. **Açık uç olarak yazdığım şeyin gerekçesi bir varsayıma
dayanıyorsa, o varsayımı ya kapıya bağla ya da bulgu say.**

### K6: Online katmanı — offline kuyruk, uzlaştırma, 5 aksiyon, transfer sürücüsü — ✅ TAMAM (2026-08-30)
**Kapsam kararı (Atilla, 2026-08-30):** yalnız DETERMİNİSTİK katman. Bu konteynerde Nakama örneği
yok ve egress politikası dış servisleri engelliyor; gerçek RPC köprüsü yazılsa derlenirdi ama
ÇALIŞTIĞI KANITLANAMAZDI ve "test geçmeyen kod reddedilir" kuralı delinirdi. Köprü ayrı dilim;
bu dilim host'un bağlayacağı arayüzleri (`IOnlineSink`) ve tüm kararları kapıyla veriyor.

- **Offline kuyruk (CB 8.3):** Tier 1-2 bağlantı yokken **kuyruğa GİRMEZ** — reddedilir. Kuyruğa
  alıp sonra reddetmek kullanıcıya işinin tutulduğunu düşündürürdü; ekonomik durum çatallanması
  "kullanıcı dikkatli olur" diye değil, komut hiç kaydedilmediği için engellenir. Tier katalogda
  sabit ve kaynaktan bağımsız (CB 6) — LLM bu kapıyı düşüremez.
- **Uzlaştırma (K6 kararı 2026-08-25):** kuyruk SIRAYLA gider, sunucu otoriterdir, ama reddettiği
  her komut `UzlastirmaKaydi` olarak **sebebiyle** döner. Kuyruk her hâlükârda boşalır: yarım
  uygulanmış kuyruk ikinci bağlanmada komutları tekrar oynatırdı (idempotency deposu çoğunu
  yakalar ama ona güvenmek yapısal değil).
- **5 online aksiyon:** `league.create/join/set_rules`, `replay.share_clip`, `social.report_player`.
  Lig kimliği `Rng.Hash64` ile TÜRETİLİR (Guid/zaman YASAK); tekillik sunucunun işi, buradaki iş
  üretimin deterministik olması. Katılım şifresi kalıcı duruma **ham** yazılmaz, `DizeOzeti` girer.
  Kural değişikliği yalnız KURUCUya açık (GDD 6.2) ve kısmi güncelleme dokunulmayanı KORUR.
- **Yayın kanalı:** klip ve rapor `GameState`i değiştirmez, `IOnlineSink`e gider — ama K4'ün maç
  kuyruğu dersiyle AYNI şekilde journal'da BEKLETİLİR ve yürütücü denetimden SONRA boşaltır.
  Geri alınamayan dış etkiler işlemin dışında kalamaz.
- **Transfer karşı taraf sürücüsü:** K5'te bilerek ertelenen boşluk (PR #19 açık thread'i) kapandı.
  `TransferTick.Ilerlet` topu karşı tarafta olan teklifleri işler; sıra BİZDEYSE **dokunmaz**
  (kullanıcının kararını gasp etmez) ve süresi dolmuşa karışmaz (temizlik K5'in yolu — iki yerden
  temizlemek aynı yuvayı iki gerekçeyle kapatırdı). AI kabul ederse transferi KENDİSİ yapmaz,
  sırayı bize geçirir: kabul komutu Tek Kapı'dan geçmeli, yoksa kadro ve bütçe denetimi atlanırdı.

- **Kapılar (6):** `K6Baglanti` · `K6OfflineKuyruk` · `K6Uzlastirma` · `K6LigAksiyonlari` ·
  `K6YayinKanali` · `K6TransferSurucusu`. Beşi ters çevrilip ölçüldü: tier kapısı kalkınca 4 aksiyon
  kuyruğa giriyor, rapor sessizleşince "düşen sayısı 0≠2", kurucu yetkisi kalkınca katılan üye kural
  değiştiriyor, sıra denetimi kalkınca AI kullanıcının kararını gasp ediyor, kanal denetimi kalkınca
  kanalsız host sessizce başarı dönüyor.

**Kendi diffimde iki kusur buldum (inceleme gelmeden).**
1. **`ligUyeMax` ÖLÜ [KALİBRE] anahtarıydı** — yüklemede doğrulanıyor, hiçbir yerde kullanılmıyordu.
   K5 incelemesinde Bugbot'a "hiçbir şey yapmayan bir yapılandırma anahtarı, olmayandan kötüdür;
   var sanılır" diye yazmıştım ve bir dilim sonra aynısını yapmışım. Kaldırıldı.
2. **`LeagueState.UyeSayisi` hash'e giriyordu ama YERELDE TÜRETİLEMEZDİ.** Lige katılan istemci
   ligde kaç kulüp olduğunu BİLMEZ; `join` alanı 1 yapıyordu, yani hash'e giren bir sayı
   uyduruluyordu. Hash'e giren her alan replay dördülünden (engineVersion, config_hash, seed,
   komut zaman çizelgesi) yeniden üretilebilmelidir — türetilemeyen sayı iki istemciyi
   ayrıştırırdı. Alan kaldırıldı; mevcut ve tavan denetimi SUNUCUnundur. `K6LigAksiyonlari`
   artık sınıfı koruyor: katılım, payload'ın vermediği alanları (kurucu/chaos/hız/bütçe/saat
   dilimi) YAZMAMALI. Diş ölçümü: uydurma eklenince `katılım sunucunun alanlarını uydurdu(chaos 1…)`.

**Kendi kapımın ölçüm YERİ yanlıştı.** `K6TransferSurucusu`'nun "seed sürücüyü oynatıyor mu"
iddiası ilk yazımda sabit 1,5M teklifle ölçülüyordu; o teklif oyuncunun değerinin çok dışındaydı ve
karar seed'den BAĞIMSIZ olarak hep aynıydı — kapı kırmızı yandı. İddia yanlış değildi, **ölçüm
yeri** yanlıştı: pazarlık salınımı ancak PAZARLIK BANDINDA sonucu değiştirir. Teklif artık değerin
%75'i olarak hesaplanıyor. (K5'in "kapı en kritik yolu ölçmeli" dersinin kardeşi: doğru şeyi
yanlış noktada ölçen kapı da yanıltır.)

`K2HashKapsami` dördüncü dilim üst üste yeni alanı yakaladı (`GameState.Lig`).

### K6 inceleme turu — ✅ TAMAM (2026-08-30)
Codex dört bulgu çıkardı (3 P1, 1 P2); dördü de haklı. Biri **para basma yolu**ydu.

1. **(P1) Alıcı kararı SATICI eşikleriyle veriliyordu.** `TransferTick` tek rutin (`Valuation.Karar`)
   kullanıyordu; o rutin SATICI mantığıdır ve "yeterince YÜKSEK"i kabul eder. Teklifi karşı taraf
   açtığında AI **alıcı** rolündedir — ama aynı rutinle, istenen fiyat ne kadar yüksekse o kadar
   istekli oluyordu. Sömürü: gelen teklife FAHİŞ karşı teklif ver, AI kabul etsin, satışı tamamla.
   Çözüm: `Valuation.AliciKarari` — eşikler TERS (ucuzu kabul, fahişi ret, AŞAĞI pazarlık), ayrı
   [KALİBRE] katsayılar ve sıra denetimi (`aliciKabulEsigiOran < aliciRedEsigiOran`).
   **Ders: iki rol simetrik değildir; "aynı fonksiyon iki tarafa da çalışır" varsayımı sömürü üretti.**
2. **(P1) Yayın patlarsa durum geri alınmıyordu.** `KlipPaylas` ağ zaman aşımıyla fırlarsa istisna
   `Apply` ve `Persist`ten SONRA kaçıyor, `Geri` çağrılmıyor, bus rezervasyonu serbest bırakılıyordu:
   durum ilerlemiş kalıyor ve tekrar denemede aynı klip yeniden yayınlanabiliyordu. Çözüm: yayın
   `try/catch` içinde, patlarsa `journal.Geri`. Maç kuyruğu boşaltması da aynı korumayı aldı.
3. **(P1) Lig şifresinin TUZSUZ HIZLI özeti kalıcı duruma yazılıyordu.** "Ham değil, özeti" diye
   yazmıştım ama `DizeOzeti` tuzsuz bir xxHash64: düşük entropili bir lig şifresi için sözlük
   saldırısı ucuzdur, yani şifreyi saklamaktan anlamlı ölçüde iyi DEĞİLDİ. Çözüm: alan tamamen
   kaldırıldı — şifre kalıcı durumda **hiçbir biçimde** iz bırakmaz, doğrulama sunucunundur.
4. **(P2) Uzlaştırma ortada patlarsa uygulanmış önek tekrar oynuyordu.** `Clear()` döngü SONUNDAydı;
   gönderim ortada patlayınca o satıra ulaşılmıyor, zaten uygulanmış komutlar kuyrukta kalıyor ve
   sonraki bağlanmada tekrar oynuyordu — o turun raporu da kayboluyordu. Çözüm: her tamamlanan
   girdi ANINDA kuyruktan düşer; rapor dışarıdan verilen listeye yazılır, istisna yukarı çıksa bile
   tamamlananların raporu elde kalır ve kuyrukta yalnız gönderilmemiş sonek durur.

- **Kapı:** `K6IncelemeBulgulari`. Dördü de ters çevrilip ölçüldü — satıcı rutini alıcıya
  verilince "AI alıcı FAHİŞ fiyatı kabul etti (para basma yolu açık)", geri alma kalkınca "yayın
  patlayınca sürüm geri alınmadı", eski `Clear()` konumu geri gelince "ikinci bağlanma 5 komut
  gönderdi (uygulanmış önek tekrar oynadı)", şifre sızıntısı eklenince "farklı şifreler farklı
  hash veriyor".

**Şifre kapım İLK HÂLİYLE DİŞSİZDİ.** "Şifreli katılım ile şifresiz katılım aynı hash'i vermeli"
diye yazmıştım; sızıntıyı `ozet % 3` ile taklit ettiğimde sonuç tesadüfen 0 çıktı ve hash'ler
eşleşti — kapı yeşil kaldı. Üç yollu karşılaştırmaya çevrildi (şifresiz, şifre A, şifre B): şifreden
türeyen herhangi bir şey sızarsa iki FARKLI şifre mutlaka ayrışır, bu şansa bağlı değil.
**Ders: tek örnekle kurulan eşitlik iddiası, o örneğin şansına bağlıdır.**

**Kalan borç (Nakama köprüsü dilimine):** yerel geri alma, uzak tarafın ZATEN ALDIĞI bir yayını geri
çağıramaz. `IOnlineSink` artık `commandId` taşıyor — ikinci kopyayı elemek köprünün bu anahtarla
yapacağı dedup'ın (outbox) işi. Anahtarsız arayüz bu güvenceyi yapısal olarak imkânsız kılardı.

### K7: LLM hattı + injection savunması — katalog 32/32 kapandı — ✅ TAMAM (2026-08-30)
**Kapsam (K6 ile aynı ilke):** LLM ÇAĞRISININ KENDİSİ kapsam dışı — API erişimi bu ortamda
kanıtlanamaz ve prompt'lar `docs/prompts/` altında versiyonlu dosyalarda yaşar (CLAUDE.md).
Burada olan şey **savunma**: modelin ne söylediğinden BAĞIMSIZ olarak çıktının katalog dışına,
bant dışına ya da onaysız yürütmeye dönüşemeyeceğini yapısal kılan katman. CB 7.2'nin güvencesi
zaten buna dayanır — "en başarılı injection bile yalnızca bir öneri kartı üretebilir".

- **`SuggestionPipeline`:** LLM çıktısı ya SOHBET, ya katalog içi ÖNERİ, ya da DÜŞÜRÜLÜR; üçüncü
  olasılık yok. `actionType` katalogda yoksa düşer; `Tier` çıktıdan DEĞİL katalogdan okunur
  (CB 6 — "LLM tier'ını asla düşüremez"); payload bant ön denetiminden geçer.
- **Girdi temizliği (CB 7.1):** boş, uzunluk [KALİBRE 500], kontrol karakteri, tekrar spam.
  Sekme ve satır sonu SERBEST — meşru çok satırlı girdi reddedilmez.
- **`SuggestionId` (CB 7.4):** `Guid.NewGuid()` YASAK; girdi özeti + kayıt tohumundan TÜRETİLİR,
  yani replay'de zincir yeniden kurulur.
- **Son 4 aksiyon:** `social.arrange_talk`, `social.press_response`, `staff.hire`,
  `staff.activate_premium`. **Katalog 32/32 bağlı — FAZ 04 aksiyon hattı tamam.**
- **Injection korpusu:** `evals/injection/korpus_v1.jsonl` — **28 kalıp × 11 kategori**
  (CB 10.2'nin yedisi + tier düşürme, bant aşımı, enum dışı, eksik alan). Sonuç: 6 sohbet,
  15 düşürüldü, 7 katalog içi öneri. Hiçbiri katalog dışı çıktı, bant dışı parametre ya da
  onaysız yürütme üretemedi.

**Journal'da SESSİZ YANLIŞ YAZMA bulundu (kendi kapım yakaladı).** `ClubField` uygulama zinciri
`else st.Club.Form = v` ile bitiyordu. `AktifPremium` alanını ekleyince aralık denetimi onu tanıdı
ama uygulama zinciri tanımadı ve yazma **yakala-hepsini else'e düşüp FORM'a gitti**: aktivasyon izi
yazılmadı, kulüp formu bozuldu ve komut **BAŞARILI döndü**. Düşürülmüş bir yazmadan da kötü — yanlış
alana yazma. Çözüm: yakala-hepsini kaldırıldı, `Form` kendi dalına alındı, bilinmeyen alan artık
**patlıyor**. Aralık denetleyebildiği ama uygulayamadığı bir alan journal için KOD hatasıdır ve
sessiz kalamaz. Diş ölçümü: eski `else` geri konunca `premium izi yazilmadi`.

**ASCII varsayımı TÜRKÇE bir oyunda özellikle yanlıştı (öz-inceleme).** Tekrar spam ölçüsünü
128'lik bir diziyle yazmış ve `c >= 128` olanları ATLAMIŞTIM: `"ğ"×200` oranı **0** veriyordu,
yani Türkçe spam filtreden serbestçe geçiyordu. Türkçe metin bu oyunda kural, istisna değil.
Düzelttikten sonra emoji spam'i HÂLÂ geçiyordu — ikinci sebep: BMP dışı karakterler C#'ta VEKİL
ÇİFTtir, `"😀"×150` iki farklı `char`dan 150'şer tane demektir ve char sayarken oran 0,5'te kalıp
eşiğin altına düşer. Sayım kod noktasına (rune) çevrildi. `Dictionary` kullanımı burada güvenli:
sonuç en yüksek SAYIdır, iterasyon sırasına bağlı değildir (ME 3.2 yasağı sıraya bağımlı MANTIK
içindir) ve bu sıcak yol değil. Diş ölçümü: ASCII varsayımı geri konunca hem Türkçe hem emoji
spam'i geçiyor. **Ders: "yalnız ASCII" ve "bir karakter = bir char" iki ayrı sessiz varsayımdı;
ikisi de bu projenin ana dilinde yanlış.**

**Dördüncü kez aynı tuzak: ölü kural.** `KonusmaHandler`'a ton için enum denetimi yazmıştım;
katalog `ton`u `ParamType.Enum` + `TonEnum` ile tanımlıyor ve **KAPI 1 şema denetimi** enum dışını
benden ÖNCE eliyor. Denetimi kaldırıp ölçtüğümde suite YEŞİL kaldı — kural gereksizdi, kapı zayıf
değildi. Kapı artık otoriteyi (bus'ın `SchemaViolation`ı) ölçüyor. K5'te sahiplik ve kadro
sınırında, K6'da lig üye sayısında, burada enum'da: **yeni bir denetim yazmadan önce daha derin
katmanın o aksiyon için ne yaptığına bakılır.** Bu artık bir alışkanlık değil, kural.

- **Kapılar (6):** `K7KatalogKapandi` · `K7GirdiTemizligi` · `K7InjectionKorpusu` ·
  `K7OneriYurutmeDegil` · `K7SonDortAksiyon` · `K7Izlenebilirlik`. Üçü ters çevrilip ölçüldü:
  katalog kısıtı kalkınca `admin.grant_all` öneri olarak geçiyor, Tier katalogdan okunmayınca
  6 kalıpta `Tier dusuruldu(T0 != T2)`, yakala-hepsini else geri gelince premium izi kayboluyor.
- `K2HashKapsami` beşinci dilim üst üste yeni alanı yakaladı (`Personel`, `AktifPremiumId`).

**Açık uç:** LLM'in KALİTESİ (golden set eval, CLAUDE.md "skor < %85 merge yok") bu dilimde
ölçülmedi — model çağrısı gerektirir. `evals/golden/` iskeleti duruyor; kalite kapısı, model
erişimi olan ortamda koşacak ayrı iş. Bu dilim GÜVENLİĞİ test eder, KALİTEYİ değil — CLAUDE.md'nin
kendisi bunların ayrı kapılar olduğunu söylüyor.

### K7 inceleme turu — ✅ TAMAM (2026-08-30)
Codex beş bulgu çıkardı (1 P1, 4 P2); beşi de haklı.

1. **(P1) Personel sözleşmesi HİÇ SONA ERMİYORDU.** `staff.hire` `KalanHafta` yazıyordu ama
   hiçbir şey azaltmıyordu: süre dolduktan sonra personel aktif kalıyor, yuvayı KALICI işgal
   ediyor ve aynı-tip kuralı o tipin bir daha alınmasını SONSUZA DEK engelliyordu. **K4'te
   değişiklik hakkının maç başına dolmamasıyla aynı sınıf hata: yazılan ama hiç ilerletilmeyen
   sayaç.** Çözüm: `StaffTick.Hafta` — `EconomyTick`/`MacTick`/`TransferTick` ile aynı sözleşme.
   Süre bitince yuva TAMAMEN boşalır (`Tip`, `Tier`, `KalanHafta`): yalnız `Tip = 0` bırakmak
   hash'te artık bırakır ve iki farklı yoldan aynı kadroya varan iki kayıt ayrışırdı.
2. **(P2) Öneri, doğrulanmış PAYLOAD'ı taşımıyordu.** CB 7.1 sözleşmesi `IntentSuggestion(actionType,
   payload, gerekçe)` diyor; benimkinde payload YOKTU. Kart, doğrulamayı geçen argümanları
   gösteremez ve onay anında AYNISINI gönderemezdi — ayrı ve değiştirilebilir bir `IPayloadView`
   tutmak gerekirdi, yani **gösterilen öneri ile onaylanıp denetime giren şey birbirine bağlı
   olmazdı.** Çözüm: `OneriParam[]` ile doğrulanan değerler öneriyle taşınıyor.
3. **(P2) `SuggestionId` ÖNERİYİ kapsamıyordu.** Kimlik yalnız (girdi özeti, kayıt tohumu)'ndan
   türüyordu. Model DETERMİNİSTİK DEĞİL: aynı prompt aynı oturumda farklı aksiyon/payload
   önerebilir ve o kartlar onay ile denetim kaydında AYIRT EDİLEMEZ olurdu — CB 7.4'ün vaat ettiği
   bire bir girdi → öneri → sonuç izi koparadı. Çözüm: aksiyon adı + parametreler (katalog
   sırasında) kimliğe giriyor; determinizm korunuyor.
4. **(P2) Tam sayı alanı `TryGetNumber` ile okunuyordu.** `tip: 3.5` bant içi görünüp ÖNERİ
   oluyordu ama bus aynı payload'ı `SchemaViolation` ile reddediyordu — yani kullanıcıya
   **onaylayamayacağı kart** gösteriliyordu. Bu, bu hattın ön denetim yapma GEREKÇESİNİN tam
   tersiydi. Çözüm: `ParamType.Int` için `TryGetInt`.
5. **(P2) Kanal eksikken denetime BAŞARILI kayıt yazılıyordu.** Kablolama denetimi
   `audit.Persist(..., None)`den SONRAydı: komut reddediliyor ve durum geri alınıyordu ama
   denetim logunda kalıcı bir "başarılı" kaydı kalıyordu — **denetim logu olmayan bir başarıyı
   anlatıyordu.** Çözüm: üç kanalın (maç kuyruğu, online, persona) varlık denetimi `Apply`dan da
   önce. Kanal yokluğu durumdan bağımsız, deterministik bir kablolama hatasıdır; uygulamadan önce
   bilinebilir.

**ARANACAK ÖRÜNTÜ — "yazılan ama hiç ilerletilmeyen sayaç" (ikinci kez).** K4'te
`KalanDegisiklikHakki` dünya kurulumunda doluyor, sonra yalnız azalıyordu; K7'de personelin
`KalanHafta`sı alımda yazılıyor, hiç azalmıyordu. İkisi de tek başına masum görünür — hata,
sayacın YAŞAM DÖNGÜSÜ eksik olduğunda ortaya çıkar ve genellikle BAŞKA bir kuralla birleşip
büyür (K4: `NoChargesLeft` her maçı kilitledi; K7: aynı-tip kuralı o tipi sonsuza dek kilitledi).
**Kural: kalıcı duruma bir SAYAÇ yazan her dilim, o sayacı kimin ilerlettiğini/tazelediğini de
göstermek zorundadır.** Gösteremiyorsa ya tick eksiktir ya sayaç kalıcı durumda olmamalıdır.
Bu, K7'nin "yeni denetim yazmadan önce daha derin katmana bak" kuralının kardeşi: ikisi de
"eklediğim şeyin etrafındaki mekanizmayı da kontrol et" diyor.

- **Kapı:** `K7IncelemeBulgulari`. Beşi de ters çevrilip ölçüldü — süre ilerlemeyince
  "yuva tam bosalmadi(tip 4 tier 3 hafta 38)" ve aynı tip bir daha alınamıyor, kimlik öneriyi
  kapsamayınca "ayni prompt + FARKLI payload ayni SuggestionId", `TryGetNumber`a dönünce
  "ondalik tam sayi alani ONERI oldu", payload taşınmayınca kart boş, denetim sırası eskiye
  dönünce "reddedilen komut icin BASARILI denetim kaydi yazildi".

**4. bulgunun kendi kapısı da vardı:** iddianın dayanağı "bus GERÇEKTEN reddediyor" olduğu için
kapı bunu da ölçüyor — yoksa "bus reddedecek" varsayımı dayanaksız kalırdı. K5'in "açık uç olarak
yazdığın şeyin gerekçesi varsayıma dayanıyorsa kapıya bağla" dersinin uygulaması.

### 🔴 BULGU: `Rng.Gauss01` borç gözcüsü, gözetlediği fonksiyonu ÖLÇMÜYORDU (2026-08-30)
K7 merge edildikten sonra bekleyen kararın (b) şıkkının tetikleyici koşulu geldi ("FAZ 04 sonunda,
K7 bittikten sonra"). Kararı fiyatlamak için önerilen düzeltme GEÇİCİ olarak uygulanıp tam kapı
koşuldu. Düzeltme commit EDİLMEDİ — `Gauss01`'in gövdesi ME Spec 3.1'de normatif kod bloğu olarak
duruyor (`s * 16 + i` dahil), yani değişiklik bir spec çelişkisidir ve kararı Atilla verir.

- **Gözcü kapı sahteydi.** `K3RngGauss01Borcu`, `salt*16 + i` adres formülünü kapının İÇİNDE yeniden
  kuruyor ve 12 çekilişi `Rand01`den kendisi topluyordu. `Gauss01` yalnız yazdırılan ama İDDİA
  EDİLMEYEN tam-eşitlik satırında çağrılıyordu. Yani gözcü, gözetlediği fonksiyona hiç bakmıyordu.
- **Kanıt (tek satır):** düzeltme kaynağa uygulandığında kapının iddia ettiği sayılar **%50,0 /
  %100,0'da çakılı kaldı**; yalnız iddia edilmeyen tam eşitlik %26,9/%55,3 → **%0,0/%0,0**'a indi.
  Borç ödense kapı bunu göremezdi; `Gauss01` başka türlü bozulsa da göremezdi.
- **DÜZELTİLDİ (kapı, kaynak değil):** kapı artık gerçek `Gauss01`'i çağırıp YAKIN EŞİTLİK ölçüyor
  (|Δ| < 1e-12). Küme aynıysa toplam yalnız toplama sırasında ayrışır (hata sınırı ~12·eps·6 ≈
  1,6e-14), dolayısıyla yakın eşitlik aynı kümeyi public API üzerinden yakalar. Mevcut kodda AYNI
  sayıları veriyor (%50,0 / %100,0) — hiçbir eşik gevşemedi ya da sıkılmadı, yalnız ölçü dürüstleşti.
- **Diş ölçümü (üç yön):** borç dururken %50,0/%100,0 → PASS · düzeltme uygulanınca %0,0/%0,0 → PASS
  (borcun ödendiği artık GÖRÜNÜYOR) · `tick & ~3u` ile kötüleştirilince %75,0 → **FAIL**. İlk
  kötüleştirme denemem (`tick & ~1u`) mevcut kusurla aynı yere düştü ve %50'de kaldı: perturbasyon
  yanlıştı, ölçü değil — tam eşitlik %26,9→%50,0 hareket ederek değişikliği yine de gördü.
- **Kural:** bir borcu "görünür tutan" gözcü kapı, borcun **ÖDENDİĞİNİ de gösterebilmelidir**.
  Gösteremiyorsa gözetlediği şeyi değil kendi kopyasını ölçüyordur. Kapının içine, gözetlediği
  kodun formülünü ikinci kez yazmak bu hatanın taşıyıcısıdır.

### 📏 ÖLÇÜM: `Gauss01` düzeltmesinin gerçek maliyeti — tahminden ÇOK ucuz (2026-08-30)
Bekleyen karar "golden set yeniden üretilir, M16-E'nin 12 metriği yeniden ölçülür ve **muhtemelen
yeniden kalibre edilir (1-2 dilim)**" diyordu. Ölçüldü; bu tahmin fazla kötümsermiş.

- **Kırmızıya dönen 6 kapı:** `MatchSkeletonGolden` · `M2Golden` · `M4Golden` · `M6Golden` ·
  `M17GoldenReplay` (50/50) — beşi de golden HASH kapısı, yani mekanik yeniden üretim işi
  (üretici komut zaten var). Altıncısı `M4CalibrationBands`.
- **`M16ECalibGenis` GEÇTİ — 12 metriğin hepsi bantta kaldı.** gol 2,41→2,57 · şut 27,6→27,4 ·
  isabetli 7,5→7,6 · korner 8,5→8,2 · faul 21,2→21,0 · sarı 3,32→3,21 · kırmızı 0,21→0,23 ·
  penaltı 0,30→0,32 · ofsayt 4,9→4,9 · sakatlık 0,48→0,52 · pas %81,3→%81,4 · xG sapma %1,1→%5,7.
  **Yeniden kalibrasyon gerekmiyor.**
- **`M4CalibrationBands` gürültüden düştü, motordan değil.** N=12'de gol 1,58 (taban 2,0) verdi.
  Aynı fikstürde N=200 ölçümü: taban **2,43** → düzeltmeli **2,40**. Yani gerçek etki 0,03 gol;
  1,58 tamamen örneklem gürültüsü. **TUZAK:** düzeltmeyi yapan kişi bu kırmızıyı görüp motoru
  "yeniden kalibre" etmeye kalkarsa gürültüye kalibre etmiş olur.
- **Yan bulgu:** `M4CalibrationBands` 12 maçlık örneklemde gerçek değeri 2,40 olan bir metriği 1,58
  ölçebiliyor ve bandın tabanı 2,0. Yani bu kapı **tek başına gürültüden kırmızıya dönebilir**.
  Gürültüden düşebilen kapı, geçtiğinde de az şey söyler. Ayrı bir öneri olarak bekleyen kararlarda.
- **Açık kalan tek soru:** xG sapma %1,1 → %5,7 (tavan %10). Bantta ama oransal olarak en çok
  hareket eden metrik; düzeltme dilimi bunu kabul etmeden önce bakmalı.
- **Yeniden fiyatlama:** golden yeniden üretimi + xG sapmasına bir bakış = **tek küçük dilim**,
  "1-2 dilim + yeniden kalibrasyon" değil.

### K8: `Rng.Gauss01` çarpışma borcu ödendi — ✅ TAMAM (2026-08-30)
Atilla'nın kararı (yukarıda) üzerine uygulandı. FAZ 03'ten kalan son sim çekirdeği borcu.

- **Değişiklik tek satır:** `salt * 16 + i` → `salt * 0x9E3779B1u + i * 0x85EBCA6Bu`. 12 adres artık
  `base + i·k` (k tek) biçiminde; bir elemanın bit-0'ını çevirmek başka bir elemana düşemez, çünkü
  bu `(j-i)·k = ±1 (mod 2^32)` gerektirir ve k tersinir olduğundan çözüm |j-i| ≤ 11'in çok dışındadır.
- **Sonuç:** komşu tick %50,0 → **%0,0** · bit0-seed %100,0 → **%0,0**.
- **SPEC ÇELİŞKİSİ — bu kayıt bağlayıcıdır.** ME Spec 3.1'in kod bloğu `s * 16 + i` yazar; kod artık
  ondan ayrılıyor. Spec dosyasına DOKUNULMADI (yasak). ME 13.4 upset kararında (2026-08-19) kurulan
  precedent uygulandı: spec dosyası korunur, DECISIONS kaydı bağlayıcı olur. Gerekçe `Rng.Gauss01`
  XML doc yorumuna da yazıldı ki koda bakan kişi ayrılığı orada görsün.
- **Golden yeniden üretimi (5 kapı):** `MatchSkeletonGolden` 0x896E1495EFF5C34C · `M2Golden`
  0x2950BCCD69FEACA4 · `M4Golden` 0x66A1E641E68B66B2 · `M6Golden` 0x12F21C303ACF022E ·
  `M17GoldenReplay` 50/50 (`gen-replays` üretici komutuyla; balanceHash ve bandsHash DEĞİŞMEDİ —
  0xCE04A7006C62F2C2 / 0x03BEA30B618B4B08, yani bu bir motor değişikliğidir, balance değişikliği değil).
- **Kalibrasyon:** M16-E'nin 12 metriğinin hepsi bantta, yeniden kalibrasyon YAPILMADI (fiyatlama
  kaydındaki tahmin doğrulandı). `M4CalibrationBands` örneklemi 12 → 200 yapıldı: gol 2,43 → 2,40.
- **xG sapması %1,1 → %5,7 (tavan %10) — bakıldı, KABUL EDİLDİ.** Ham sayılar: xG 2,44 → 2,43
  (sabit), şut 27,6 → 27,4 (sabit), goller 2,41 → **2,57**. Yani sapma xG modelinin kaymasından
  DEĞİL, dönüşüm oranının artmasından geliyor. Gol 2,57 ME 17.2 bandının (2,2-3,2) içinde ve gerçek
  futbola (~2,7) 2,41'den daha yakın. **Mekanizma KANITLANMADI** — şut/kaleci düellosuna baktım,
  gördüğüm iki `salt 63` çağrısı orta nişanı ve savunma uzaklaştırmasıydı, dönüşümü açıklamıyor.
  İddia edilmiyor; ölçüm kaydediliyor. Açık uç: xG katsayıları artık %5,7 az tahmin ediyor →
  balance sprintinde nudge edilmeli (bekleyen kararlara yazıldı).
- **Gözcü kapının eşikleri SIKILDI.** Önceki eşikler %50/%100'dü çünkü ölçüt "bugünkü borçtan
  kötüleşme"ydi. Borç kapandıktan sonra o eşikleri bırakmak hatanın TAMAMEN geri gelmesine sessizce
  izin verirdi. Tavan %0,5 (2000 örnekte 10; tesadüf beklentisi ~1,6e-9). Diş: eski formül geri
  konunca kapı iki boyutta da **FAIL** veriyor.
- **Kural:** **bir borç kapandığında, o borcu bekleyen kapının eşiği de kapanmalıdır.** Eşik borcun
  seviyesinde unutulursa kapı hatanın tamamen geri gelmesine izin verir — gözcü kapının en sinsi
  çürüme biçimi budur.

### 🟡 BULGU (açık): iki alt sistem aynı RNG adresini paylaşıyor — `Physics · 700+entity · salt 63`
K8 sırasında xG mekanizması aranırken görüldü, K8'in kapsamı dışında.

- `MatchEngine.cs:1136` orta nişanı: `Gauss01(seed, Physics, 700 + i, st.Tick, 63)`
- `MatchEngine.cs:3151` savunma uzaklaştırması: `Gauss01(seed, Physics, 700 + def, st.Tick, 63)`
- Aynı domain, aynı entity aralığı, aynı salt, aynı tick anahtarı. `i == def` olan bir ajan aynı
  tick'te her ikisini de yaparsa **birebir aynı** değeri çeker. Domain/entity/salt şemasının varlık
  sebebi tam olarak bunu önlemek.
- Erişilebilirliği DOĞRULANMADI (aynı tick'te hem orta yapıp hem uzaklaştıran ajan gerekiyor).
  Bugün bir kapıyı düşürmüyor. Önerisi bekleyen kararlarda.

### K8 inceleme turu — ✅ TAMAM (2026-08-30)
Tek bulgu (Codex, P2) ve fazlasıyla yerinde: kapının BAŞLIK yorumu hâlâ borcu ödenmemiş
anlatıyordu — `DÜZELTİLMEDİ (bilinçli)`, "karar bekleyen kararlarda", `[16·salt, 16·salt+12)`
aralığında "topluyor" — oysa aynı commit borcu kapatıp eşiği sıkmıştı. Gövdeyi yeniden yazıp
üstündeki bloğu bir daha okumamışım.

- **Zararın doğru tarifi bulguda:** mesele "yorum eskimiş" değil, **birbiriyle çelişen iki
  talimat**. Başlık "dokunma, karar bekliyor" diyor; on satır aşağıda gövde borcun kapandığını
  söylüyor. Kırmızı bir kapıyı teşhis eden kişi önce başlığı okur ve yanlış sonuca varır.
- **Codex bir yer gördü; aynı bayatlık iki yerde daha vardı** ve ikisi de bir TASARIM TERCİHİNİ
  açık borca işaret ederek gerekçelendiriyordu: `EconomyTick` ("`Gauss01`'in kendisi FAZ 03
  borcudur") ve `Valuation` ("`Gauss01` KULLANILMAZ — çakışma borcu açıktır"). Üçü de güncellendi.
- **İki çağrı yerinin KODU değişmedi:** `Rand01` tabanlı üniform gürültüde kalıyorlar. `Gauss01`'e
  geçmek ekonomi ve transfer sonuçlarını kaydırır ve karşılığında bir şey kazandırmaz — üniform
  gürültü orada zaten doğru araçtı, borç yalnızca ikincil gerekçeydi. Yorumlar artık bunu söylüyor
  ki sonraki okuyucu, hâlâ geçerli bir tercihi "kaldırılmayı bekleyen geçici çözüm" sanmasın.
- **Kapı adı korundu:** `K3RngGauss01Borcu` artık borcu değil regresyonu bekliyor, ama ad
  DECISIONS'taki çapraz referanslar çözülsün diye değiştirilmedi; başlık yorumu bunu açıkça yazıyor.
- **Kural:** **bir borç kapatıldığında, o borcu GEREKÇE olarak gösteren her yorum da kapatılır.**
  Kodu düzeltip üstündeki gerekçeyi bırakmak, düzeltmeyi okunamaz hale getirir. Bu turda ironisi
  şuydu: konusu "bayat anlatan kapı yanıltır" olan bir PR, tam da bayat anlatan bir kapı içeriyordu.
- **Diş ölçümü YOK — bilinçli.** Değişiklik yalnız yorum; bir kapının yakalayacağı davranış yok.
  Diş ölçtüğünü iddia etmek gösteri olurdu.

## FAZ 04 kapanış borçları (K9)

### K9-A: RNG adres çakışma kapısı — ✅ TAMAM (2026-08-30)
Bekleyen karardaki öneri (b) uygulandı: tek örneği elle düzeltmek yerine TÜM çağrı yerlerini tarayan
bir kapı yazıldı. Doğru karar çıktı — elle düzeltme, aşağıdaki iki bulgunun ikisini de kaçırırdı.

- **Kapı ne yapıyor:** `MatchEngine.cs` kaynağını tarar, her `Rng.Gauss01/Rand01` çağrısının
  (domain, entity tabanı, tick, salt) adresini çıkarır ve **işgal ettiği ham salt kümesini** hesaplar.
  `Rand01(s)` yalnız `s`'yi tüketir; `Gauss01(s)` K8 sonrası `s·0x9E3779B1 + i·0x85EBCA6B` (i∈[0,12))
  alt-saltlarını tüketir. İki çağrı ancak bu kümeler KESİŞİRSE çakışır.
- **ÇÖZEMEDİĞİNİ ATLAMAZ:** sarmalayıcı üzerinden gelen değişken entity/salt'lar bildirilmiş bir
  tabloyla çözülür; tabloda olmayan bir sarmalayıcı ya da çözülemeyen bir argüman kapıyı KIRMIZIYA
  döndürür. Bugünkü tarama: **53 adres → 20 ayrık dörtlü, çakışan 0, çözülemeyen 0.**
- **Bulgu 1 (bilinen):** `ExecuteOpenCross` (orta nişanı) ve `ResolveAerial` (hava topu temizliği)
  ikisi de `Physics·(700+ajan)·st.Tick·63` kullanıyordu. **Erişilebilirlik ÖLÇÜLDÜ: 200 maçta maç
  İÇİ çakışma 0.** İlk ölçümüm yanlıştı — adres kümesini 200 maç boyunca biriktirmiştim, oysa adres
  `seed`'i de içerir ve farklı tohumlu iki maçtaki aynı (entity,tick) çakışma değildir; maç içi
  ölçünce 0 çıktı. Yani koruma adres şemasından değil **"tek top" değişmezinden** geliyordu (orta ve
  hava topu temizliği aynı tick'te aynı ajana düşemez). Salt 67'ye taşındı.
- **Bulgu 2 (kapının ortaya çıkardığı, DAHA ÖNEMLİ): bir salt aralığının GENİŞLİĞİNİ balance
  belirliyor.** `Gurultu(35+c)` ve `Gurultu(40+c)` döngüleri `taban..taban+kisaMax-1` aralığını işgal
  ediyor ve `kisaMax = min(longball.gkKisaN, n)`. Bugün `gkKisaN = 3` → 35-37 · 40-42 · sabit 45,
  ayrık. **`gkKisaN ≥ 6` olsaydı iki karar-gürültüsü akışı SESSİZCE çakışırdı.** Bir balance
  düzenlemesi ("kaleci daha çok kısa seçenek düşünsün") determinizm varsayımını bozardı ve JSON'da
  tek sayı değişikliği olarak incelemeden geçerdi. Kapı span'ı balance'tan OKUR: o düzenleme artık
  kırmızıya döner.
- **Diş ölçümü (üç yön):** salt 67→63 → **FAIL** (satır 1136+3157) · `gkKisaN` 3→6 → **FAIL**
  (satır 1244+1253 ve 1253+1270) · `gkKisaN` 3→**5** (tam sınır) → **PASS**. Sonuncusu kapının kaba
  değil TAM olduğunu gösteriyor: sınırda kurt masalı anlatmıyor.
- **Kapının kendi kör noktası diş ölçümüyle bulundu.** İlk yazımda sarmalayıcının bütün çağrıları
  `Rng` satırına atfediliyordu ve "aynı satır = aynı çağrı yeri" elemesi yüzünden **iki farklı
  ÇAĞIRANIN birbiriyle çakışması tamamen görünmezdi** — `gkKisaN=6` denemesi kapıyı yeşil bıraktı.
  Çağıranın satırı kaydedilerek düzeltildi. Kapıyı ölçmeseydim, kapı bulguyu bulamayacaktı.
- **Golden yeniden üretimi:** salt değişikliği 5 kapıyı kaydırdı; `MatchSkeletonGolden`
  0x1A872CD9CB06B721 · `M2Golden` 0x03F1FB2C645841A1 · `M4Golden` 0xD8C76BF965937DC2 · `M6Golden`
  0x675BCD0B9EF7AB84 · `M17GoldenReplay` 50/50. balanceHash/bandsHash değişmedi.
- **Kural:** **bir salt aralığının genişliğini ayarlanabilir bir sayı belirliyorsa, o sayı artık
  balance değil determinizm parametresidir.** Ya kapıya bağlanır ya da koddan sabitlenir.

### K9-B: xG sapması — K8'deki OKUMAM YANLIŞTI, gerçek bulgu daha eski ve sistematik (2026-08-30)
K8'de "gol 2,41→2,57 çıktı, xG sabit kaldı, yani düzeltme dönüşüm oranını artırdı" yazmıştım.
Tek 500 maçlık ölçüme dayanan bu okuma **yanlıştı**.

- **Ölçüm:** 8 BAĞIMSIZ tohum ailesi × 500 maç, işaretli sapma. Sonuç **ORT +%4,26 · SD 2,52 ·
  aralık [+1,44, +8,52]**. Yani tek bir 500 maçlık ölçümün doğal yayılımı ±2,5 puan; K8'deki %5,7
  ile K9-A'daki %2,5 bu yayılımın İÇİNDE. İkisi de motor değişikliği hakkında bir şey söylemiyordu.
- **Asıl bulgu 8/8 ailenin POZİTİF olması** (tesadüf olasılığı ≈ %0,8): xG sistematik olarak az
  tahmin ediyordu ve bu K8'DEN ÖNCE DE BÖYLEYDİ — o zaman sadece düşük bir örnek (%1,1) çekmişiz.
- **Kaynak ayrıştırıldı — penaltı DEĞİL.** Penaltı modelinin beklenen dönüşümü ≈0,753
  (pCenter/tahmin dağılımından, direk 0,04 düşülerek); kaydedilen `hedefOrtalama` 0,76. Fark <%1 ve
  penaltı hacmi 0,24/maç — toplam sapmanın kaynağı olamaz. Sapma **açık oyun xG'sinde**.
- **DÜZELTME BALANCE İŞİ, SPEC İŞİ DEĞİL:** ME 15.2 xG'nin FORMÜLÜNÜ (lojistik + terimler)
  belirtir; katsayılar `balance/sim.balance.json` → `shot.xg` altında [KALİBRE]'dir. Formüle
  dokunulmadı, yalnız `b0` ayarlandı: **-2,48 → -2,43**.
- **Nudge ölçülerek seçildi** (aynı 8 aile): b0 -2,48 → ORT +%4,26 · b0 **-2,43 → ORT +%0,33**
  (aralık [-2,37, +4,42]) · b0 -2,40 → ORT −%1,95. Ortayı tutturan -2,43.
- **Yan kazanç — kapı sağlamlaştı, bant GEVŞEMEDİ.** `M16ECalibGenis`'in xG sapma tavanı %10;
  eski merkezle en kötü gözlem 8,52 (pay 1,5 puan), yeni merkezle 4,42 (pay 5,6 puan). Yani
  "gürültüden kırmızıya dönme" riski bandı gevşetmeden yarıya indi. Gerçek yanlılığı düzeltmek,
  kapının kırılganlığını da düzeltti — bandı esnetmek bunu yapmazdı, yalnız gizlerdi.
- **`balanceHash` DEĞİŞTİ** (0xCE04A7006C62F2C2 → 0x4A949442E9BE564C): bu bir balance değişikliğidir,
  golden set yeniden üretildi. K9-A'nın motor değişikliğinden farkı burada görünür.
- **Kural:** **tek bir örneklemden "şu değişiklik şunu yaptı" sonucu çıkarma.** Önce o ölçünün
  kendi yayılımını ölç; fark yayılımın içindeyse ortada bulgu yoktur. K8'de bunu yapmadım ve
  olmayan bir nedensellik yazdım.

### K9-C: RPC köprüsü + transactional outbox — ✅ TAMAM (2026-08-30)
K6'da ertelenen "gerçek Nakama köprüsü" dilimi. Yazmaya başlamadan önce mevcut katmana bakıldı ve
**24 saatlik dedup'ın ZATEN var olduğu** görüldü (K1, `IdempotencyStore`: 24 saat pencere, önceki
yanıt aynen, (kullanıcı, CommandId) anahtarı, güvenlik turu düzeltmeleri dahil). Neredeyse ikinci
bir kopyası yazılacaktı — "yeni denetim yazmadan önce daha derin katmana bak" kuralı işe yaradı.

- **Gerçek boşluk neydi:** yayın bugün `WorldExecutor` içinde, durum commit'iyle aynı kilitte ve
  hata halinde geri alınarak yapılıyor. Bu SÜREÇ İÇİ hataya karşı doğru ama SÜREÇ ÖLÜMÜNE karşı
  değil: durum yazıldıktan sonra ağ çağrısı yarıda kalırsa yayın KAYBOLUR ve durum "yayınlandı" der.
- **Transactional outbox:** `IOutboxStore` (dayanıklılık dikişi) · `OutboxSink : IOnlineSink`
  (mevcut dikişe takılır, **executor'da değişiklik gerekmez** — böylece outbox yazması zaten atomik
  bölgenin içinde olur) · `OutboxPompasi` (teslim). Teslim en-az-bir-kez olduğu için uzak taraf
  `CommandId` ile dedup yapmak ZORUNDA; arayüz o anahtarı K6'dan beri taşıyordu.
- **Sıra:** takılan kayıt ARKASINDAKİLERİ de bekletir (CB 8.2 "varış sırası esastır"). Sırayı atlayıp
  devam etmek, bağımlı iki yayını uzak tarafa ters sırada ulaştırabilirdi.
- **`RpcKopru` + CB 8.2:** `CommandOutcome` `newStateVersion` taşımıyordu (bus durum katmanını
  tanımaz, tanımamalı da); köprü yürütücüden okuyup `KomutYaniti`ye ekliyor.
- **KRİTİK TASARIM: pompa hatası komutu DÜŞÜRMEZ.** Durum commit edilmiştir ve kayıt outbox'ta durur.
  Pompa hatasında komutu reddetmek, outbox'ın çözdüğü bağımlılığı geri kurardı — yayın kanalının
  sağlığı komutun sonucunu belirlemeye devam ederdi.
- **Diş ölçümü (dört yön):** outbox kaydı kalıcı değil → `K9OutboxDayanikliligi` FAIL · pompa sırayı
  atlıyor → `K9OutboxSirasi` FAIL (`SIRA bozuldu(302,303,301)`) · köprü pompa hatasında komutu
  düşürüyor → `K9PompaKomutuDusurmez` FAIL · yanıt stateVersion taşımıyor → `K9RpcYaniti` FAIL.
- **SIRA KAPISI İLK YAZIMDA DİŞSİZDİ.** `SpyOnlineSink.Patlat` hepsini birden patlatıyor; o durumda
  "başta takıldı" ile "hepsini denedi, hepsi patladı" AYNI sonucu veriyor ve pompayı sırayı atlayacak
  şekilde bozduğumda kapı YEŞİL kaldı. `SecmeliPatlayanSink` eklendi: yalnız ilki patlar, arkadakiler
  teslim EDİLEBİLİR durumdadır — iddia ancak böyle ölçülebiliyor.
- **SimWorker gerçekten ayağa kalkıyor:** köprü test koşumuna hapis değil. `dotnet run --project
  server/TheBadge.SimWorker` → `submit#1 → ok=True stateVersion=1 tekrar=False`,
  `submit#2 (ayni CommandId) → tekrar=True`.
- **YAPILMADI, açıkça:** Nakama RPC kaydının kendisi ve PostgreSQL outbox deposu. Bu ortamda
  koşturulamaz, dolayısıyla yazılmadı (kanıtlanamayan kod eklenmez). Dikişler hazır: `IKomutTasima`
  ve `IOutboxStore`. Gerçek deponun TEK şartı, outbox yazmasının durum yazmasıyla aynı işlemde
  commit edilmesidir — outbox'ın bütün değeri o özellikten gelir. `server/SERVER_SETUP.md` güncellendi.
- **Kural:** **"hepsi başarısız" senaryosu, sıra iddiasını ölçemez.** Bir sıralama garantisini test
  etmek için, atlanabilecek olanın atlanabilir DURUMDA olması gerekir.

### K9-D: LLM kalite kapısı — alet burada, ölçüm model erişimi olan ortamda (2026-08-30)
K7'nin açık ucu. **Kapsam sınırı en baştan:** bu ortamda canlı model YOK, dolayısıyla burada koşan
şey "modelin kalitesi" değil kalite kapısının ALETİdir. Bu ayrımı bulanıklaştırmak — elle yazılmış
cümleleri model çıktısı sayıp yüksek bir skor raporlamak — ölçmediği şeye puan vermek olurdu.

- **Golden set 5 → 24 örnek** (`docs/evals` bandı 20-50 ✓). Her satıra `boyut` alanı eklendi
  (olgu · ton · yasak · uzunluk); `K9GoldenSetKapsami` dört boyutun da temsil edilmesini zorunlu
  kılıyor, id tekrarını ve eksik `ton`/`max_cumle`yi reddediyor.
- **`EvalScorer` — `yasak` anahtarları KAVRAMDIR, düz metin değil.** "uydurma istatistik", "alay",
  "tibbi teshis" gibi anahtarlar altdizi araması olamaz; her biri deterministik bir DEDEKTÖRE
  bağlandı (girdide olmayan sayı · girdide geçmeyen skor kalıbı · sözlükler · jargon yoğunluğu ·
  ark bağlamı). **Tanınmayan anahtar kapıyı KIRMIZIYA döndürür** — rubrikte yazılı ama hiç
  denetlenmeyen bir kural, sessiz zayıflamadır (K9-A'daki "çözemediğini atlama" disiplini).
- **Makinenin YARGILAMADIĞI boyutlar ayrı raporlanır.** Prose kalitesi ve üslup inceliği
  `InsanBakisi` listesine düşer, puana GİRMEZ. `evals/golden/README` zaten "script + insan bakışı
  karışımı" diyordu; puanlayıcı o sözleşmeye uyuyor.
- **`scorer_fixtures.jsonl` MODEL ÇIKTISI DEĞİLDİR** ve dosyanın ilk satırı bunu söylüyor: elle
  yazılmış, her biri bir dedektörü hedefleyen 20 fikstür. `K9EvalRubrigi` her birinin beklenen
  makine kararını verdiğini denetliyor.
- **Fikstür gerçek bir hata yakaladı (g019):** "yanlis skor" dedektörü yalnız `skor` alanına
  bakıyordu; girdinin `one_cikan` alanı "3-0 onde iken" diyorken maçın skoru 3-3 olduğu için doğru
  cümle hatalı sayılıyordu. Referans girdinin TAMAMI yapıldı — kural zaten "memory_facts dışına
  çıkma"ydı, `skor` alanına daraltmak kuralın kendisini daraltıyordu.
- **Koşucu:** `-- eval-run <cevaplar.jsonl>`, eşik `balance/llm.balance.json` →
  `eval.gecmeEsigiYuzde` = **85** [KALİBRE]. **Cevabı olmayan golden satırı BAŞARISIZ sayılır**;
  atlansaydı eksik üretim yüzde payını yükseltirdi (az örnekle yüksek skor). Aletin gösterimi:
  24 satırın 9'una cevap verilince %37,5 → "MERGE YOK" döndü.
- **Diş ölçümü (üç yön):** bilinmeyen `yasak` anahtarı sessizce geçsin → `K9EvalRubrigi` FAIL ·
  golden set 24→18 → `K9GoldenSetKapsami` FAIL · `Kos`un sayı denetimi kaldırılsın →
  `K9EvalKosuSozlesmesi` FAIL.
- **ÜÇÜNCÜ KAPI İLK YAZIMDA DİŞSİZDİ.** `catch (ArgumentException)` yazmıştım; ama
  `ArgumentOutOfRangeException` ondan TÜREDİĞİ için, koruma kaldırılınca patlayan indeks çökmesini
  "koruma çalıştı" sanıyordu. Kapı artık açık korumanın MESAJINI arıyor. Fark önemli: koruma varken
  anlamlı bir hata, yokken anlamsız bir indeks çökmesi olur.
- **YAPILMADI, açıkça:** gerçek model çıktılarıyla kalite koşusu. Model erişimi gerektirir; CI
  adımı olarak `-- eval-run` ile bağlanır. `docs/prompts/templates/mac_sonu_roportaj.md`'nin
  `son_eval: bekliyor` alanı DOĞRU kalıyor — canlı eval koşulmadı, koşulmuş gibi yazılmadı.
- **Kural:** **bir istisna tipini yakalamak, o istisnayı yakaladığını kanıtlamaz** — türemiş tipler
  aynı `catch`e düşer. Bir korumayı ölçen kapı, korumanın KENDİ imzasını (mesaj/tip) aramalıdır.

### K9 inceleme turu — ✅ TAMAM (2026-08-31)
Üç bulgu (Codex): iki P1, bir P2. Üçü de geçerli çıktı.

- **P1 — teslim İSTEK YOLUNDAYDI.** `RpcKopru.Gonder` pompayı SENKRON çağırıyordu: yavaş ya da
  asılı bir yayın kanalı, ÇOKTAN COMMIT EDİLMİŞ bir komutun yanıtını bekletiyordu. Yani outbox'ın
  kaldırdığı geri-alma bağımlılığının yerine GECİKME bağımlılığı duruyordu ve CB Spec'in
  "Hub RTT ≤ 300 ms (p95)" hedefi yayın kanalının sağlığına bağlanıyordu. **Hatanın ironisi,
  ayrımı anlatan yorumun hemen altında olmasıydı** — bağımlılığın bir eksenini kesip diğerini
  görmemişim. Teslim artık yalnız `PompayiSur` ile, host'un arka plan döngüsünden sürülüyor.
  Kapı bunu doğrudan ölçüyor: `Gonder`den sonra ağ kanalı BOŞ olmalı.
- **P1 — `resultingEvents` yanıtta yoktu.** CB Spec 3'ün şeması `{ status, resultingEvents,
  newStateVersion }`. `newStateVersion` için 8.2'yi referans gösterip AYNI diyagramdaki üçüncü
  alanı atlamışım. Domain event'leri zaten üretiliyordu (`journal.Events`) ama yalnız denetim
  sink'ine gidiyordu; RPC köprüsünü kullanan istemci komutun sonucunu uygulayamıyor, tanımsız bir
  tam/delta çekimi yapmak zorunda kalıyordu. `IKomutOlaySinki` eklendi — denetimle AYNI transaction,
  aynı geri alma sözleşmesi. Tekrar yanıtı da AYNI olayları taşıyor (CB 8.1 "önceki yanıt aynen";
  durumu yalnız statüden ibaret saymak, tekrar eden istemciyi olaysız bırakırdı). Önbellek dedup
  penceresiyle aynı ömre budanıyor — aksi halde pencere içinde bir tekrar boş liste alır ve
  "aynen" iddiası delinirdi.
- **P2 — kapı yalnız `MatchEngine.cs`'i tarıyordu.** `Lod2Resolver.cs`'teki dört üretim çağrısı
  kapının DIŞINDAYDI: kapı, iddia ettiği kapsamın bir bölümünü hiç görmüyordu. Artık
  `shared/TheBadge.Sim` altındaki TÜM kaynaklar taranıyor (69 adres, 2 dosya) ve RNG'li yeni bir
  dosya kendiliğinden kapsama giriyor.
- **P2'nin zorunlu kıldığı ikinci değişiklik: ENTITY ARTIK ARALIK.** `7100 + team*20 + idx` gibi
  ifadelerde taban tek başına yanıltıcı; aralık modellemeden `7100` ile `7120`nin çakışıp
  çakışmadığı görülemez. Her entity ifadesi tabloda bildirilir, bildirilmeyen kapıyı kırmızıya
  döndürür.
- **KASITLI PAYLAŞIM BİLDİRİLİR (Codex'in kendi önerisi).** `Lod2Resolver` satır 92 ve 105 aynı
  çekilişi BİLEREK iki kez okuyor: 92 sarı kart toplamını, 105 aynı saltlarla taraf başına
  değerleri türetiyor. Farklı adres kullanmak ikisini ayrıştırırdı (toplam ≠ parçaların toplamı).
  Gerekçesiyle listede duruyor; bildirilmemiş her paylaşım hata sayılıyor.
- **Diş ölçümü (beş yön):** pompa istek yoluna geri kondu → `K9OutboxDayanikliligi` FAIL
  (`TESLIM ISTEK YOLUNDA YAPILDI`) · olaylar yanıta konmadı → `K9RpcYaniti` FAIL · tekrar
  önbelleği okumadı → `K9RpcYaniti` FAIL (`farkli olay sayisi(1/0)`) · `Lod2Resolver`a çakışma
  sokuldu → `K9AdresCakismasi` FAIL (yeni dosyanın gerçekten kapsandığının kanıtı) · kasıtlı
  paylaşım bildirimi kaldırıldı → FAIL.
- **Kural:** **bir bağımlılığı kesmek, onun TÜM eksenlerini kesmek demek değildir.** Outbox
  geri-alma eksenini kesiyordu; gecikme ekseni duruyordu ve tam da ayrımı anlatan yorumun altında
  görünmez kalmıştı. Bir ayrıştırma iddiası, ayrıştırdığı her ekseni ayrı ayrı ölçmelidir.

**İki bulgu daha (Cursor Bugbot, ikisi de Medium, ikisi de geçerli) — ikisi de "kapının MODELİ
gerçeklikten kopabilir" temasında:**

- **Bilinmeyen sarmalayıcı YANLIŞ ATFEDİLİYORDU.** Değişken argümanlı bir `Rng` çağrısını "en yakın
  helper ADI"na `LastIndexOf` ile bağlıyordum. Tabloda OLMAYAN bir sarmalayıcı, kendinden önce adı
  geçen BAŞKA bir helper'a atfediliyor ve onun çağrılarıyla genişletiliyordu — `cozulemeyen`e hiç
  düşmüyordu. Yani kapının en çok övündüğüm özelliği ("sessizce atlamaz") tam da bu yolda
  tutmuyordu. Artık KAPSAYAN BİLDİRİMİN adı çıkarılıyor; ad tabloda yoksa kapı kırmızıya dönüyor.
  Diş: `Lod2Resolver`a tablosuz bir sarmalayıcı eklendi → `kapsayan bildirim
  'BilinmeyenSarmalayici' TABLODA YOK`.
- **KAPI, `Gauss01`İN İŞGAL MODELİNİ YENİDEN KURUYORDU** — üretimde formül değişse model eskir ve
  kapı yeşil kalırdı. **Bu, K8'de kendi bulduğum hata sınıfının aynısı, üstelik onu düzeltmek için
  yazdığım kapının içinde.** Alt-saltlar public API'den gözlenemez, ama model DOĞRULANABİLİR:
  modellenen alt-saltlarda `Rand01` toplanıp 6 çıkarıldığında gerçek `Gauss01` çıkmalıdır. Beş
  farklı saltta denetleniyor. Diş: üretimdeki çarpanı değiştirdim → `ISGAL MODELI GERCEK Gauss01'i
  URETMIYOR (salt 0: model -1,087 != gercek -0,185)`.
- **Kural (ikisinin ortak dersi):** **bir kapının kaynak kod hakkındaki MODELİ de kodun kendisi
  kadar eskiyebilir.** Model gözlenemeyen bir şeyi anlatıyorsa, gözlenebilir bir sonucuna karşı
  DOĞRULANMALIDIR; "aynı formülü ben de yazdım" bir kanıt değil, ikinci bir kopyadır.

**İki bulgu daha (Bugbot, `5d8d66e` üzerinde) — ikisi de olay önbelleğinde, ikisi de benim:**

- **ÖNBELLEK KULLANICIYI ANAHTARA KOYMUYORDU.** Doc yorumuna, `IdempotencyStore`un 2026-08-24
  güvenlik bulgusunu referans göstererek "(kullanıcı, CommandId) anahtarı — yalnız `CommandId`
  DEĞİL" yazmışım; sözlüğü tek anahtarla kurmuşum ve `userId` parametresini hiç kullanmamışım.
  **Yorum bir güvenlik özelliğini anlatıyor, kod onu uygulamıyordu.** Aynı Id'yi kullanan başka bir
  oturum ötekinin `resultingEvents`ini alabilir ya da üzerine yazabilirdi.
- **OLAYLAR YAYINLARDAN ÖNCE ÖNBELLEĞE YAZILIYORDU.** `Yaz`, denetimden hemen sonra çağrılıyordu;
  ardından gelen üç yayın bloğundan biri patlayıp `Geri` çağırırsa durum geri alınıyor ama önbellek
  KALIYORDU — yanıt, hiç gerçekleşmemiş bir durum geçişinin olaylarını taşırdı. Yayınların en
  sonuna taşındı: oraya ulaşan her yol "işlem tamamlandı" demektir. Ayrıca `AlVeyaBos` artık okuma
  anında da süre denetliyor (budama amorti edilmiş olduğu için henüz budanmamış bayat kayıt
  okunabiliyordu).
- **Bugün erişilebilir DEĞİL, ama sözleşme gerçek:** katalogda durumu HEM değiştirip HEM yayın
  yapan bir aksiyon yok. Bu yüzden executor'ın commit SIRASI teste özel bir handler'la doğrudan
  sınandı — bulgu erişilebilirlikle değil sırayla ilgiliydi.
- **Diş ölçümü (üç yön):** anahtardan kullanıcı çıkarıldı → FAIL (`BASKA KULLANICI otekinin
  resultingEvents'ini aldi`) · okuma anındaki süre denetimi kaldırıldı → FAIL · `Yaz` yayınlardan
  öncesine alındı → FAIL (`GERI ALINAN komutun olaylari onbellekte kaldi`).
- **ÜÇÜNCÜ DİŞİ İKİ KEZ YANLIŞ KURDUM.** Önce geri almadan sonra YENİ bir `CommandId` ile denedim —
  hayalet kaydı hiç sorgulamıyordu. Sonra aynı payload'la denedim — komut yeniden yürütülüp TAZE
  olay üretiyordu ve hayaletle ayırt edilemiyordu. Doğrusu: aynı `CommandId`, ama BANT DIŞI payload
  — handler hiç koşmaz, dolayısıyla yanıttaki her olay hayalettir.
- **Kural:** **bir "artık olmamalı" iddiasını ölçerken, ölçüm yolunun o şeyi ÜRETMEDİĞİNDEN emin
  ol.** Testin kendisi taze veri üretiyorsa, bayat veriyi göremezsin.

**🔒 GÜVENLİK BULGUSU (Cursor Security Agent, MEDIUM) — çapraz kullanıcı ifşası:**

- **`Gonder` önbelleği İSTEMCİ KONTROLÜNDEKİ `zarf.UserId` ile okuyordu**, host oturumundan gelen
  `userId` ile değil. Bus, `env.UserId != authenticatedUserId` ise komutu `NotOwned` ile reddeder —
  **ama önbellek okuması red yolunda da çalışıyor.** Saldırgan kendi oturumuyla bağlanıp zarfa
  KURBANIN kimliğini ve `CommandId`sini yazarak, komut reddedilirken kurbanın olaylarını (kasa,
  transfer, taktik) alabilirdi.
- **Bu, bir önceki bulgunun düzeltmesinin ARDINDAN kalan yol.** `(kullanıcı, CommandId)` anahtarını
  kurdum ama anahtarın KULLANICI bileşenini güvenilmeyen kaynaktan besledim. Anahtarı doğru
  tasarlayıp yanlış değerle sorgulamak, anahtarı hiç koymamakla aynı sonucu verir.
- **`IdempotencyStore` bunu K1'den beri DOĞRU yapıyordu** (`TryReserve(authenticatedUserId, …)`).
  Onun yanına, ona bakarak kurduğum yapıda güvenilmeyen alanı kullanmışım.
- **Kapım bu yolu göremiyordu:** iki kullanıcılı yalıtım testimde her iki çağrıda da zarfın
  kullanıcısı OTURUMUN kullanıcısına eşitti. Yani "yalıtım" testi, yalıtımın kırıldığı asıl
  senaryoyu hiç kurmuyordu. Kapıya zarf/oturum UYUŞMAZLIĞI yolu eklendi.
- **Diş:** okuma `zarf.UserId`'ye geri bağlandı → `ZARF/OTURUM UYUSMAZLIGINDA kurbanin olaylari
  sizdi(1)`.
- **Kural:** **bir yetkilendirme anahtarının değeri, anahtarın kendisi kadar önemlidir.** Kimlik
  bileşenini istemciden almak, anahtarı hiç koymamakla aynı kapıyı açar. Ve bir yalıtım testi,
  yalıtımın kırılabileceği yolu KURMUYORSA yalıtımı ölçmüyordur.

**BULGU: bir önceki düzeltmenin kendisi yeni bir sözleşme ihlali yarattı (Bugbot, MEDIUM).**

- `Yaz`ı yayınların ardına taşırken **etrafındaki `try`/`Geri` sarmalayıcısını da düşürmüşüm.**
  Sonuç: `Yaz` fırlatırsa durum uygulanmış, yayınlar çıkmış, ama rezervasyon TAMAMLANMAMIŞ kalıyor
  → istemci tekrarı handler'ı İKİNCİ KEZ çalıştırıyor (çift uygulama). Üstelik `IKomutOlaySinki`
  doc yorumu hâlâ "denetimle aynı sözleşme: fırlatırsa durum geri alınır" diyordu — **yine yorum
  bir şey, kod başka şey.**
- **Doğru sözleşme, geri almak DEĞİL:** o noktada yayınlar ÇIKMIŞTIR ve geri çağrılamaz. Geri
  almak "yarısı yayınlanmış" bir işlem bırakırdı; istisnayı yukarı bırakmak çift uygulama yaratırdı.
  Kanal artık **FIRLATMAMALIDIR** ve fırlatırsa istisna YUTULUR — yanıt önbelleğini kaybetmek ikisinden
  de ucuzdur (istemci delta yerine tam çekim yapar). Yutulan hata `OlayKanaliHatasi` ile SAYILIR:
  sessizlik ölçülebilir kalmalı.
- **Kalıcı olay saklama isteniyorsa bu kanal doğru yer değildir** — o, denetim sink'i yoluna aittir
  (yayınlardan önce koşar ve fırlatırsa durum gerçekten geri alınır). Doc'a yazıldı.
- **Diş:** yutma kaldırıldı → istisna yukarı çıkıp koşuyu çökertiyor (temiz FAIL satırı değil, ama
  süreç sıfırdan farklı kodla düşüyor).
- **Kural:** **bir çağrıyı taşırken etrafındaki hata sözleşmesini de taşıdığından emin ol.** Konum
  değişince doğru sözleşme de değişebilir; eski sarmalayıcıyı körü körüne taşımak da onu düşürmek
  kadar yanlıştır — bu vakada doğru cevap üçüncü bir şeydi (fırlatmayan kanal).

### 🟡 BULGU (açık): `OzetKart` entity ayrımı yapısal olarak garantili değil
K9 inceleme turunda entity aralıkları modellenirken görüldü.

- `Lod2Resolver.OzetGol` / `OzetKart` entity'si `7100 + team*20 + idx` ve `7200 + team*20 + idx`.
  `team*20` ayrımı yalnız `idx < 20` iken doğrudur.
- **Gol tarafı GÜVENLİ:** `PoissonDraw` gol sayısını `k < 15` ile kapatıyor → idx ≤ 14 < 20, yapısal.
- **Kart tarafı DEĞİL:** `sariEv = Yuvarla(table.sari, …)` bir kalibrasyon ızgarasından geliyor ve
  yapısal bir üst sınırı yok; döngü yalnız `summaryCount < SummaryCapacity` (32) ile kapalı.
  `sariEv ≥ 20` olsaydı takım 0'ın 20. kartı, takım 1'in 0. kartıyla AYNI adresi çekerdi.
- Bugün erişilemez (ızgara değerleri ~2-3), ama koruma yapıdan değil DEĞERDEN geliyor — `gkKisaN`
  ile aynı sınıf. Seçenekler: (a) `idx`i 20'de kapat (tek satır, golden'ları kaydırır);
  (b) ayrımı 20'den `SummaryCapacity`ye çıkar; (c) kapıya `idx < 20` iddiasını bağla.
  **Öneri: (a)** — kapatma hem ucuz hem de garantiyi yapıya taşır.

### K10-A: LOD 2 özet log taraf ayrımı yapısal yapıldı — ✅ TAMAM (2026-08-31)
K9'un açık ucu. **Önerimden SAPTIM ve sebebi kayıtta.**

- **Kayıtlı öneri (a) `idx`i 20'de kapatmaktı.** Uygulamadım: bu garantiyi yapıya taşırdı ama
  20'den sonraki kart olaylarını LOG'DAN DÜŞÜRÜRDÜ. Seçenek (b) — ayrımı `SummaryCapacity`ye
  çekmek — aynı garantiyi VERİ KAYBETMEDEN veriyor. Öneriyi yazarken bu maliyeti hesaba katmamışım.
- **Değişiklik:** `7100/7200 + team * 20 + idx` → `+ team * SummaryCapacity + idx`. `idx`,
  `summaryCount` ile birlikte artıyor ve döngüler `summaryCount < SummaryCapacity` ile kapanıyor;
  dolayısıyla `idx ≤ SummaryCapacity-1` HER ZAMAN doğru → iki taraf yapısal olarak ayrık.
- **`K9AdresCakismasi` bu sınıfı GÖREMİYOR** ve bunu abartmamak için ayrı kapı yazıldı. O kapı her
  çağrı yerini TEK bir entity aralığı olarak modelleyip aralıklar ARASINDA kesişim arar; bir
  aralığın KENDİ İÇİNDE takla atması tek çağrı yerinin içinde olur ve karşılaştırma hiç kurulmaz.
- **`K10OzetAyrimi`** üç şeyi denetler: ayrım `SummaryCapacity` SEMBOLÜNÜN kendisi mi (sayıca eşit
  olması yetmez — biri değişince öteki de değişmeli) · `summaryCount <` karşılaştırmalarından
  HİÇBİRİ başka sınıra bağlı değil mi · gol tarafının ikinci kapağı (`k < 15`) duruyor mu.
- **KAPI İLK YAZIMDA GEVŞEKTİ:** döngü kapağı denetimini "en az 4 tane olsun" diye TAHMİN ettiğim
  bir sayıya bağlamışım; gerçek sayı 6 çıktı, dolayısıyla biri bozulup 5'e düşünce kapı yine
  geçiyordu. Sayı yerine ÖZELLİK ifade edildi: hiçbir `summaryCount <` başka sınıra bağlı olamaz.
- **Diş (iki yön):** ayrım sabit sayıya döndürüldü → FAIL · bir döngü kapağı başka sabite bağlandı
  → FAIL (`1 adet summaryCount < BASKA sinira bagli (64)`).
- **Golden KAYMADI** — beklenen: LOD 2 özet dakikaları hiçbir kapıda sabitlenmiyor (golden set LOD 0
  maçlarıdır). Değişiklik gerçek (deplasman tarafının adresi 7120+idx → 7132+idx) ama kapılara
  görünmez. Bu, LOD 2 özet logunun pinlenmemiş olduğunun kaydıdır — bir açık uç değil, bilgi.
- **Kural:** **bir kapıyı TAHMİN ettiğin bir sayıya bağlama.** Sayı yanlışsa kapı gevşer ve bunu
  yalnız diş ölçümü gösterir; ifade edilebilen bir ÖZELLİK varsa ona bağla.

### 🔴 K10-B: CB 4.2 açık ucunun ÖNERİSİ YANLIŞ VARSAYIMA DAYANIYORDU (2026-08-31)
Kapatmaya giderken kapatılacak şeyin var olmadığı ortaya çıktı.

- **Kayıtlı öneri (c):** "yalnız `set_instruction`ı maça taşı, anchor/rol hub'da kalsın." Gerekçesi
  GDD 3.1/3.2 ayrımıydı — makul, ama **hub tarafında talimatın bir ETKİSİ olduğunu VARSAYIYORDU.**
- **GERÇEK DURUM: bireysel talimat İKİ TARAFTA DA ATIL.** `Talimatlar` yazılıyor, `WorldHash`e
  giriyor ve `InstructionSlot` ile YUVA TAHSİSİ için okunuyor; **hiçbir oynanış mantığı `Deger`ini
  okumuyor.** Maç tarafında da `PlayerInstr` kataloğu boş (`None = 0`).
- **Dolayısıyla (c) uygulanmadı:** hiçbir şey yapmayan bir komutu yeni bir bağlama taşımak boşluğu
  kapatmaz, **GÖRÜNMEZ hale getirirdi** — maçta kabul edilen, kimsenin okumadığı bir bayt.
- **Şema da örtüşmüyor:** katalog `set_instruction(oyuncuId, talimatId 1-64, deger 0-10)` SKALER bir
  dial ifade eder. GDD 3.2'nin üç talimatı ise roller (ayrı aksiyon), hareket zonları (anchor) ve
  MARKAJ — markaj bir HEDEF oyuncu ister ve `deger` 0-10 bunu taşıyamaz. ME 14.2 "bireysel talimat /
  markaj değişimi"ni sonraki karar tick'ine bağlıyor, yani mekanizmayı öngörüyor; eksik olan
  talimat KATALOĞU ve şemanın onu taşıyabilmesi.
- **Motorda markaj ZATEN VAR** (ME 7.5, `markajSayisi`, MatchEngine:1583) ama motor-içi otomatik
  atamadır — oyuncu komutuyla verilmez.
- **`K10TalimatAtilligi` kapısı:** talimat DEĞERİNİ okuyan bir oynanış kodu belirirse ya da
  `PlayerInstr` kataloğu dolarsa kırmızıya döner ve CB 4.2 sorusunu masaya koyar. Ölçüt "Talimatlar"
  kelimesi DEĞİL `Talimatlar[...].Deger` okuması — kelimeyle aramak doğrulama mesajının metnine bile
  takılıyordu (ilk yazımda takıldı). Diş: sahte bir değer okuması → FAIL · `PlayerInstr`e ikinci
  değer → FAIL.
- **KARAR ATİLLA'NIN, seçenekler netleşti:** (a) talimat sistemini GERÇEKTEN uygula — katalog
  tasarımı + şema genişletmesi (markaj hedefi) + motor bağlama + kalibrasyon; birden fazla dilim ve
  CB 4.2 şema revizyonu gerektirir. (b) CB 4.2'de üçünü de "Hub" olarak revize et — spec revizyonu,
  motor işi yok, GDD 3.2'nin "maç içi bireysel talimat" vaadini daraltır. (c) bugünkü hâlde bırak;
  `K10TalimatAtilligi` atıllığı görünür tutar. **Öneri: (b)** — bugün maç içi bireysel talimat
  YOK ve olmayan bir vaadi spec'te taşımak, boşluğu kalıcı borç gibi gösteriyor; gerçekten
  istendiğinde (a) ayrı bir GDD v4.2 kalemi olarak açılır.
- **Kural:** **bir öneriyi yazarken dayandığı varsayımı da yaz** — "hub'da çalışıyor" varsayımını
  yazsaydım, uygulamaya geçmeden önce onu doğrulardım. Bu, bu projede aynı sınıftan ikinci vaka.

### K10-C: zaman çizelgesi işaretleri eşikten ayrıldı — ✅ TAMAM (2026-08-31)
M14 açık ucu, seçenek **(b)**: eşik korunur, çizelge en yüksek N'den beslenir.

- **Sorun ÖLÇÜLDÜ, iddia edilmedi:** 60 maçlık koşuda ME 15.3'ün `H > 0,5` ölçütü **41 maçta SIFIR**
  işaret verdi. Yani maçların üçte ikisinde zaman çizelgesi boş kalıyordu — M14'ün 0,5-0,8/maç
  ölçümüyle birebir.
- **İKİ BÜYÜKLÜK AYRILDI.** `HighlightCount` hâlâ ME 15.3'ün EŞİK tanımıdır ve DEĞİŞMEDİ;
  `TimelineMarks` sunum içindir ve `zamanCizelgesiIsaret` [KALİBRE] kadar en yüksek andan dolar.
  Bunları birleştirmek spec'i sessizce değiştirmek olurdu — kapı ikisinin ayrı davrandığını
  denetliyor (eşik hâlâ bazı maçlarda 0 vermeli; vermezse ölçüt çizelgeye bağlanmış demektir).
- **Motor mantığı DEĞİŞMEDİ** — `top` listesi zaten H'ye göre azalan sıralıydı, ilk N alınıyor.
  ME 17.5 "ayar sahası" ilkesine uygun: sunum kararı, sim kararı değil.
- **Diş (iki yön):** işaret sayısı 0 → `60/60 macta zaman cizelgesi BOS` · çizelge eşiğe bağlandı →
  `60/60 hedefi tutmadi` + `41/60 BOS`.
- **GÖZLEM — sunum ayarı `config_hash` içinde.** `zamanCizelgesiIsaret` `sim.balance.json`'a
  girdiği için `balanceHash` değişti ve golden set yeniden üretilmesi gerekti. Bunun DAVRANIŞ
  değişikliği OLMADIĞI kanıtlandı: yeniden üretim sonrası **50/50 `stateHash` AYNI**, yalnız
  `balanceHash` değişti (0x4A949442E9BE564C → 0xCD38F01FAA168AAD). Yani `config_hash` bütün balance
  dosyasını kapsadığı için, simülasyonu etkilemeyen bir ayar da replay setini çalkalıyor.
  Seçenekler: (a) kabul et — churn ucuz ve "tek balance" hikayesi sade kalır; (b) sunum ayarlarını
  `config_hash` DIŞI ayrı bir dosyaya taşı (`llm.balance.json`'ın zaten yaptığı gibi). **Öneri: (a)**
  bugün — ikinci bir dosya, motorun iki kaynaktan okuması demek ve tek sunum ayarı için bu maliyet
  yüksek; sunum ayarı sayısı artarsa (b) yeniden değerlendirilir. Bekleyen kararlara yazıldı.

### K10-D: ECONOMY_MAP capex kapısı — ✅ TAMAM, dört bulguyla (2026-08-31)
K3 inceleme turunun açık ucu, seçenek **(a)**: 1,05-1,15 bandı işletme dengesi olarak kalır, capex
AYRI bir kapıyla ölçülür. Uygulandı — ama ölçüm önerinin gerekçesini DÜZELTTİ (aşağıda).

- **Ölçülen senaryo:** `KademeliInsaatKosu` — parametresiz, kredisiz, EN HIZLI inşa politikası
  (slot boşsa ve para yetiyorsa yap), referans kulübün kendi tesis merdiveni (stadyum 3→5 + dört
  tesis 2→5 = 14 adım), komutlar **Command Bus'tan** (Tek Kapı). Politikaya bilerek "kasa rezervi"
  eşiği KONMADI: bir eşik olsaydı kapının verdiği cevabı eşiği oynatarak istediğim yere
  götürebilirdim — ölçtüğünü değil ayarını raporlayan bir kapı olurdu.
- **BULGU 1 — capex bandı BOZMUYOR, bandı AYAKTA TUTAN sink capex'in kendisi.** Öneri (a)'nın
  gerekçesi "yığınsal harcama sürekli dengeyi bulanıklaştırır" idi; ölçüm bunun tersini gösterdi.
  Merdiven penceresinde (11 sezon, 8 seed'de de aynı): source/sink **capex HARİÇ 1,48-1,49
  (BANT DIŞI)**, **capex DAHİL 1,123-1,132 (BANT İÇİ)**. İnşaat, kapasitesi 30K→90K'ya çıkan
  kulübün ürettiği fazlayı emen kalemdir. Yani `K3EkonomiSozlesmesi`'nin capex'i dışarıda
  bırakması bir eksiklik DEĞİL, hiç inşaat yapmayan bir koşunun tanımıdır — ve o koşu bantta
  kalıyorsa bunun nedeni kulübün büyümemesidir.
- **BULGU 2 — bant SEZON SEZON değil, PENCERE ORTALAMASI olarak tutuyor.** Capex yumruludur: tek
  bir tier adımı bir sezon gelirinin %11-38'i. Merdiven penceresinde sezon oranları **0,824 ile
  2,255 arasında savruluyor**; ortalama 1,131. ECONOMY_MAP "sezon başına net arz bandı" diyor —
  harfi harfine okunduğunda inşaat yapan hiçbir kulüp bandı tutturamaz. Kapı bilerek pencere
  ortalamasını ölçer ve savrulmayı ayrıca raporlar (ortalamayı "her sezon böyle" diye okumak,
  kapının iddiasını ölçtüğünden geniş yapardı).
- **BULGU 3 — merdiven süresi capex maliyetine olduğu kadar FAZLA ORANINA da bağlı.** Yayın geliri
  süpürüldüğünde: taban oran 1,041 → merdiven 40 sezonda BİTMİYOR · 1,074 → 18 sezon · 1,133 →
  11 · 1,159 → 10. Yani ECONOMY_MAP'in kendi bandının ALT ucunda referans merdiven fiilen
  ulaşılamaz. `merdivenSezonBandi` bu yüzden **bilerek geniş** ([6,24]): dar bir bant capex'i
  değil fazla oranını ölçerdi, fazla oranı ise zaten `K3EkonomiSozlesmesi`'nin işi. İki kuralın
  çakıştığı bu nokta "Bekleyen kararlar"a yazıldı.
- **BULGU 4 (BORÇ) — merdiven tükenince GERİYE SINK KALMIYOR.** 11. sezondan sonra oran **2,25'te
  kilitleniyor** ve orada kalıyor. Bu bir kapı hatası değil SENARYO kapsamının sonucudur:
  ECONOMY_MAP beş sink satırı sayıyor, referans koşu bunlardan **transfer bedellerini** hiç
  işletmiyor. `K10MerdivenSonrasiSink` bugünkü değeri `merdivenSonrasiOranTavani` [KALİBRE] ile
  DONDURUR (sessizce kötüleşmesin), hedefi (1,15) basar ve borç kapandığında KENDİSİ kırmızıya
  döner ("tavan kaldırılmalı ve bu kapı düşmeli").
- **[KALİBRE] eklenenler** (`balance/economy.balance.json` → `capex`): `merdivenSezonBandi [6,24]`,
  `merdivenSonrasiOranTavani 2,40`, `merdivenSonrasiHedefOran 1,15`.
- **ECONOMY_MAP bandı koda TEK YERDE indi** (`EkoOranAlt/EkoOranUst`); balance JSON'una
  TAŞINMADI, bilerek: bu bir ayar değil sözleşmedir, JSON'a taşımak bandı gevşetmeyi kod
  incelemesinden çıkarıp bir satır düzenlemesine indirirdi.
- **Diş (altı yön, hepsi ölçüldü):** tier maliyeti ×5 → `merdiven 24 sezonda TAMAMLANMADI` +
  `capex ÇIKARILINCA oran 1,113 hâlâ bant içinde` · ×0,5 → `merdiven 4 sezon, bant dışı` ·
  iflas eşiği −5M (kasa dibi −5,8M) → `en hızlı inşa eden kulüp sezon 1'de iflas etti` ·
  tavan 2,20 → `borç KÖTÜLEŞTİ` · hedef 2,30 → `BORÇ KAPANDI, tavan kaldırılmalı` ·
  bant üstü 30 > ufuk 24 → `kapı bandı ölçemez`.
- **Ölçülemeyen:** kapının "capex yük taşıyor" iddiası (3) yalnız BUGÜNKÜ senaryoda anlamlı;
  transfer sink'i modellendiğinde işletme oranı da düşeceği için o kapı yeniden kalibre edilmeli.

### K11: OYUN OYNANABİLİR HÂLE GELDİ — dikiş kuruldu, iki hata ölçümle yakalandı (2026-08-31)
Atilla "oyunu artık denemek istiyorum" dedi. Denenecek bir şey OLMADIĞI ortaya çıktı ve sebebi tekti.

- **KÖK BULGU — iki yarı hiç bağlanmamıştı.** `PlayerState.Guc`'un kendi XML yorumu bunu zaten
  söylüyordu: *"Maç motoru bunları HENÜZ kullanmıyor."* Maç motoru SENTETİK `TeamSheet`lerle, dünya
  katmanı SENTETİK G-B-M sonuç döngüsüyle test ediliyordu. FAZ 03 ve FAZ 04 ayrı ayrı yeşildi ve
  aralarında kod yoktu. Kapılar bunu göremezdi çünkü her kapı kendi yarısını ölçüyordu.
- **`SquadBridge` (K11-A):** kulüp kadrosu → diziliş kadrosu. `Guc` (0-100) motorun 26 niteliğine
  `balance/squad.balance.json` hat profilleriyle açılır. RNG YOK — köprü bir eşlemedir; rastgelelik
  aynı kadroya iki maçta farklı 11 verirdi. Eksik kadro SESSİZCE 11 uydurmuyor: `null` + hangi hat.
- **HATA 1 — ROL KİMLİK UZAYLARI FARKLI.** Dünya `rolId`si 1-32 bandında bir GDD 3.2 rol
  kataloğu; motorunki bir HAT kodu ve anlamı sabit (1 KL · 2 DF · 3 OS · 4 FV; `RoleId > 3`
  markaja inmez, `>= 3` ileri koşar). İlk yazımda dünya rolünü olduğu gibi geçirmişim: takım
  **14 forvetle** sahaya çıkıyordu. Ölçüm gizlenemezdi — maç başına 40-58 şut, 8-4 skorlar.
  Çeviri `rolHat` hattıdır: hat + 1 = motor rolü.
- **HATA 2 — KİMLİK GENİŞLİĞİ.** Dünyada `PlayerId` int, motorda `short`. Sessiz `(short)`
  dönüşümü iki oyuncuyu aynı kimliğe düşürebilirdi; köprü artık aralığı denetliyor. (Motorun
  `CreateInitialState`i bunu zaten "PlayerId 101 iki takımda birden" diye yakaladı — doğru kapı,
  ama sebebi kadro üretimine kadar geri izlemek gerekiyordu.)
- **BULGU — MOTORUN KART KALİBRASYONU ROL AYRIMINA DUYARLI.** Rol profili gerçekçileştikçe kart
  patladı: düz test kadrosuyla kart 4,97/maç ve kırmızı 0,00; köprü kadrosuyla **9,07 ve 1,88**
  (bantlar 2,5-7,0 ve 0,15-0,30). Sebep ME 11.2'de: faul şiddeti
  `marginGap = (taşıyıcının kaçış bileşiği − müdahale edenin bileşiği)/50` kullanıyor ve motor
  topa EN YAKIN oyuncuyu daldırıyor (rol bakmadan). Forvet müdahalede zayıf olduğu için
  forvet-forvet presi devasa bir fark üretiyor. Düz kadroda bu fark ~0 olduğu için M4/M5
  kalibrasyonu bunu hiç görmemişti. **İlk iki hipotezim (Aggression eşiği, defans müdahale
  katsayısı) SÜPÜRMEYLE ÇÜRÜTÜLDÜ** — kart oranı o kollara duyarsızdı; teşhis ancak köprü
  kadrosu ile test kadrosunun yan yana ölçülmesiyle çıktı.
- **KALİBRASYON ve BEDELİ (gizlenmiyor):** profil motorun kendi bantlarına göre ayarlandı
  (80 maç, 2 bağımsız kadro çifti): gol 3,35 · kart 4,50 · kırmızı 0,12 · korner 9,6 — hepsi bant
  içi. Bedeli: forvet çevikliği (0,85) defansınkinin (1,00) ALTINDA ve forvet çalımı 1,10→0,85.
  Futbol gerçekçiliğine ters. Bu bir tasarım tercihi DEĞİL, yukarıdaki motor bulgusunun semptomu.
- **BORÇ — şut/maç 33,5, hedef ≤32.** Her düzeltme kartı indirirken şutu çıkarıyor (faul azalınca
  oyun açılıyor). `sutTavani` 36 [KALİBRE] ile donduruldu, hedef basılıyor, borç kapandığında kapı
  KENDİSİ kırmızıya dönüyor. M4'ün kendi yorumu şut bandını "tohum kümesi varyansı geniş" diye
  niteliyor ve asıl bandı M5'e bırakıyor; %4'lük aşım o gevşekliğin içinde ama sessiz geçilmiyor.
- **`TheBadge.Play` (K11-B):** oynanabilir konsol. Senin maçın TAM MOTOR (LOD 0), ligin kalan 9
  maçı `Lod2Resolver` — ME 16.4'ün öngördüğü karışım. 20 kulüp × çift devre = 38 hafta, yani
  `sezonHaftaSayisi` ile birebir (sayı uydurulmadı; sezon uzunluğu zaten 20 takımlı bir ligi
  tarif ediyordu). Her yönetim eylemi Tek Kapı'dan, hafta sonu `EconomyTick`. Rakip kadroları
  OYUNCUNUNKİYLE AYNI köprüden geçer — ayrı bir üretici, iki takımın farklı kurallarla sahaya
  çıkması demekti. Tam sezon iki koşuda BİT-AYNI.
- **Ölçülen ilk sezon:** oyuncu 14. sıra, 42 puan, kasa 20M→26M. Şampiyon 94 puan. Yani lig
  yenilebilir ama bedava değil — GAME_THESIS'in "batık kulübü devral" başlangıcıyla uyumlu.
- **Kural:** *iki alt sistem ayrı ayrı yeşilse, aralarındaki dikiş ölçülmemiş demektir.* Bu
  projede kapılar hep bir modülün İÇİNİ ölçtü; K11 arayüzü ölçen ilk kapı ve ilk denemede iki
  hata çıkardı.

### K11-E: transfer sink'i ledger'a bağlandı — kalem KURULDU, borç KAPANMADI (2026-08-31)
"Merdiven sonrası uzun vade sink'i" kararı (a) olarak kapatılmıştı: *referans koşuya transfer
hattı eklensin, capex kapısı yeniden kalibre edilsin.* Uygulamanın YARISI yapıldı ve ikinci
yarısının neden yapılamadığı ölçümle netleşti.

- **YAPILDI — kalem var.** `WeekLedger.TransferTl` eklendi (ECONOMY_MAP "Transfer bedelleri"),
  `ClubState.DonemTransferGideriTl` biriktiricisiyle. Desen `InsaatTl`in BİREBİR aynısı: kasadan
  komut anında düşülür, haftalık tick sink RAPORUNA boşaltır, `NetTl`e GİRMEZ (çift muhasebe).
  Alış +bedel, satış −bedel (kalem NET harcamadır — satışı ayrı bir SOURCE saymak, ECONOMY_MAP'in
  source listesinde OLMAYAN bir gelir kalemi uydurmak olurdu), fesih +bedel.
- **BULGU: bu kalem K11'e kadar HİÇBİR YERE girmiyordu** — inşaatın K3 incelemesinde yaşadığı
  hatanın birebir aynısı. Transfer YAPAN bir sezonun source/sink oranı olduğundan İYİ
  görünüyordu, çünkü harcama sink'e hiç yazılmıyordu.
- **`K2HashKapsami` yeni alanı hemen yakaladı** ("MUTASYONU YOK — hash kapsamı ölçülmüyor").
  Yansımayla çalışan bir kapının değeri tam burada görünüyor: kalıcı duruma alan eklemek,
  hash kapsamını ve determinizmi sessizce delebilirdi.
- **`K11TransferSinki` kapısı** dört yolu ölçüyor + çift muhasebe koruması bağımsız hesapla
  kuruluyor (`NetTl` KULLANILMADAN). Diş: `TransferTl`i `NetTl`e sokunca kapı
  "transfer 4000000 çift sayılmış olabilir" diyor; alışta biriktirici beslenmezse üç iddia birden
  düşüyor.
- **YAPILAMADI — borç AÇIK kalıyor, ve sebebi kapsam.** `K10MerdivenSonrasiSink` (merdiven
  tükenince oran 2,25'te kilitleniyor) kapanmadı. Kalemi bağlamak yetmiyor; borcu kapatmak için
  referans koşunun SÜREKLİ bir transfer piyasası işletmesi gerekiyor ve **öyle bir piyasa yok**:
  dünya katmanı tek kulübü modelliyor, oyuncu havuzu fikstürle sınırlı ve yenilenmiyor, rakip
  kulüplerin ekonomisi yok. Mevcut havuzla ölçülecek cevap "hayır, emmiyor" olurdu — bu bir
  BULGU değil, senaryonun sınırı. **Uydurulmuş bir piyasayla ölçüp borcu kapatmış saymak,
  kapının ölçtüğü şeyi değiştirmek olurdu.**
- **Kalan iş (yeni bekleyen karar):** oyuncu piyasası modeli — havuz yenilenmesi + rakip kulüp
  bütçeleri. Kalem hazır olduğu için o dilim geldiğinde ölçüm doğrudan koşar.

### K12-A: motorun faul/kart modeli rol ayrımına göre kalibre edildi — ✅ TAMAM, bir borçla (2026-09-01)
K11 bulgusunun kararı (a): "motor tarafını yeniden kalibre et". Atilla "hepsini yap" dedi; yapıldı.

- **ÖNCE MAGIC NUMBER BORCU ÖDENDİ.** ME 11.2 şiddet formülünün ağırlıkları KODA GÖMÜLÜYDÜ
  (`/50` margin böleni, 0,4/0,25/0,2 terim ağırlıkları, 70 agresiflik eşiği, 0,05 ve 0,04 ekler).
  Hepsi `sim.balance.json → referee` altına [KALİBRE] olarak taşındı. Bölen özellikle önemliydi:
  modelin ROL DUYARLILIĞINI o belirliyor, çünkü bileşikler bir FARK olarak giriyor.
- **ÜÇ KADRO DAĞILIMI BİRDEN ÖLÇÜLDÜ.** İlk süpürmemde ikisine bakmıştım (düz + köprü) ve
  M16-E'nin "lig dağılımı" gözden kaçmıştı — kapı bunu hemen yakaladı. **Bu, K11'de yaptığım
  hatanın aynısıydı: eksik popülasyonla kalibre etmek.** Süpürme aracı üçünü birden ölçecek
  şekilde yeniden yazıldı.
- **DÖRT HİPOTEZ ÖLÇÜMLE ÇÜRÜDÜ, sırayla:** (1) agresiflik eşiği/defans müdahale katsayısı —
  kart oranı o kollara duyarsız; (2) `marginBolen`i büyütmek — düz kadroda uçurum (5,05 → 0,87);
  (3) doğrudan kırmızı eşiği (`kirmiziEsik`) — kol erişmiyor, lig kırmızısı 0,05'ten kıpırdamadı;
  (4) ihtiyatı "pervasız değilse" koşuluna bağlamak — TAM TERS etki, çünkü köprü kadrosunun
  faulleri zaten üst sınırın üstünde (kart 5,95 → 9,32). (5) iskontoyu yalnız `marginGap`e
  uygulamak da denendi: aralığı genişletti ama çatışmayı kapatmadı.
- **KAYNAK BULUNDU:** kaybedilen HER müdahale `ResolveFoul`a gidiyor. Kötü müdahaleci hem daha
  çok düello kaybediyor hem daha yüksek `marginGap` taşıyor — çifte ceza. Rol profili
  gerçekçileştikçe forvet presi bu iki etkiyi birden büyütüyor.
- **UYGULANAN KALİBRASYON:** `sariEsik` 0,555 → 0,560 · `sariSonrasiIhtiyat` 0,18 → 0,24 ·
  `utility.sutTehditCarpan` 0,57 → 0,47 · `utility.sutBaskiCezasi` 0,35 → 0,45.
- **SONUÇ — kadro profili GERİ DÜZELTİLDİ.** K11'de bedel yanlış yerde ödeniyordu (forvet
  çevikliği defansın altına indirilmişti). Profil futbol gerçekçiliğine döndü ve köprü kadrosu
  motorun KENDİ bantlarında: **gol 2,74 · şut 30,2 · kart 5,69 · kırmızı 0,28 · korner 9,6.**
- **ŞUT BORCU KAPANDI.** K11'de şut 33,5 idi ve 36 tavanıyla dondurulmuştu; artık 30,2 ve
  normal banda ([10-32]) döndü. `sutTavani`/`sutHedefi` kaldırıldı — kapının kendi "borç kapandı"
  dalı tam olarak bunu söylüyordu.
- **KALAN BORÇ — DÜZ DAĞILIMDA KIRMIZI 0,03 (hedef 0,10-0,36).** İki popülasyon aynı anda hem
  kart hem kırmızı bandını TUTTURAMIYOR: rol ayrımı olan kadro her ayarda ~1,8× daha fazla kart
  üretiyor ve onu bastıran iskonto seviyesi düz dağılımın ikinci sarılarını siliyor.
  **BANT GEVŞETİLMEDİ:** metrik `M16ECalibGenis`ten AYRILDI ve `M16EKirmiziBorcu` kapısına
  taşındı — bugünkü değer tavanla donduruldu, hedef basılıyor, hedefe ulaşılırsa kapı KENDİSİ
  düşüyor. Diğer 11 metrik tam güçte kaldı. Tavan örneklem gürültüsünün altına konuldu
  (500 maçta 0,03 ≈ 15 olay, Poisson ±0,008) — aksi hâlde kapı borcu değil zarı raporlardı.
- **Golden setler bilinçli yenilendi:** M2/M4/M6 sabitleri + 50 replay + LOD 2 tablosu.
- **Kural:** *kalibrasyon, ölçtüğün popülasyonların HEPSİNİ kapsamalı.* Bu oturumda aynı hatayı
  iki kez yaptım (K11'de rol ayrımı, K12'de lig dağılımı); ikisini de kapılar yakaladı.

### K12-B: maç öncesi kondisyon ve moral motora bağlandı — ✅ TAMAM (2026-09-01)
K11 köprü kararının (a) seçeneği: ME 12.1'e başlangıç enerjisi. Atilla "hepsini yap" dedi.

- **NEDEN GEREKLİYDİ:** motor her maça `Energy = 1000` ile başlıyordu; `Kondisyon` yalnız dünya
  tarafında anlam taşıyordu ve **rotasyon oynanışa HİÇ değmiyordu.** GDD 3'ün kadro yönetimi
  vaadi karşılıksızdı: yorgun oyuncuyla çıkmakla dinlenmiş oyuncuyla çıkmak aynı maçı veriyordu.
- **ME 12.1 EKİ:** `PlayerEntry.BaslangicEnerji` (0 = AYARLANMAMIŞ → tam enerji). Sıfırı
  "bitkin" saymak, alanı doldurmayan her eski kadro kurucusunu sessizce sakatlardı; kapı bu
  geriye uyumu ayrıca ölçüyor.
- **ME 12.3 EKİ:** `TeamSheet.BaslangicMomentum` (-10..+10), İLK 11'in moral ortalamasından —
  kulübede oturanın morali sahadaki havayı kurmaz. Motor momentumu maç içinde kendi işlemeye
  devam ediyor; bu yalnız başlangıç noktası.
- **[KALİBRE]** `squad.balance.json → macaGiris`: `enerji = enerjiTaban + enerjiAralik×(Kondisyon/100)`
  (550 + 450) · `momentum = clamp(round((ortMoral − 50)/10), −10, +10)`. Taban SIFIR DEĞİL:
  kondisyonu 0 olan oyuncunun sahada yok hükmünde olması bir KADRO KURALI, enerji eğrisinin işi değil.
- **ÖLÇÜM:** kondisyon 90 → enerji 955, kondisyon 20 → 640 · moral 0 → momentum −5, moral 100 → +5 ·
  24 maçlık averaj: taze −23, yorgun −39. Yorgun kadro sahada ölçülebilir şekilde daha kötü.
- **Diş:** motorun `BaslangicEnerji` okuması kaldırılınca kapı `[enerji] yorgun kadro sahada AYNI:
  taze averaj -10, yorgun -10` diyor — yani iddia gerçekten motoru ölçüyor.
- **Golden churn YOK:** düz kadro kurucuları alanı doldurmadığı için M2/M4/M6/M16-E davranışı
  değişmedi. Yalnız köprü kadrosu etkilendi ve bantlarda kaldı (gol 3,15 · şut 29,2 · kart 5,88).
- **SPEC NOTU:** bu iki ek ME 12.1/12.3'e yapılmış EKLERDİR; spec dosyasına dokunulmadı, bu kayıt
  bağlayıcıdır (ME 13.4 precedent'i) ve sonraki ME revizyonunda işlenir.

### K12-C: transfer piyasası kuruldu — borç İYİLEŞTİ ama kapanmadı, sebebi artık ÖLÇÜLÜ (2026-09-01)
K11-E'nin bıraktığı yer: kalem (`WeekLedger.TransferTl`) bağlıydı ama borç ölçülemiyordu, çünkü
piyasa yoktu. Atilla "hepsini yap" dedi; piyasa kuruldu.

- **`TransferMarket` (World):** her sezon başı havuza yeni oyuncular katılır (`market.balance.json`
  [KALİBRE]) — bir kısmı serbest, kalanı rakip kulüplerde. Tüm çekilişler sayaç-RNG + save seed.
  Rakip kulüplerin KENDİ ekonomileri modellenmiyor ve bu gizlenmiyor: onlar oyuncu SAHİBİdir,
  `Valuation` kurallarıyla pazarlık eder. Asgari gerçeklik bilinçli — daha fazlasını uydurmak
  ölçümü varsayımın kendisine çevirirdi.
- **Merdiven koşusuna transfer hattı eklendi:** teklif → karşı taraf sürücüsü (`TransferTick`) →
  kabul, hepsi Command Bus'tan. Artı kadro dönüşü: kadro tavandayken piyasada belirgin daha iyisi
  varsa en zayıf feshedilir (fesih bedeli de transfer sink'i).
- **POLİTİKA İKİ KEZ DÜZELTİLDİ, ikisi de ölçümle:**
  1. *Sınırsız politika iflas etti.* Kulüp hem inşa hem transfer yapmaya çalıştı; 6 alım kalıcı
     maaş yükü olarak işletme fazlasının TAMAMINI yedi (oran 1,13 → 1,01), merdiven hiç bitmedi,
     kasa −13M. Asıl sink BEDEL değil ÜCRET. → ECONOMY_MAP'in kendi maaş kuralı (%55 tavan) eklendi.
  2. *Öncelik yoktu.* İki sink tek fazla için yarışıyordu. Sıralama ekonomik gerçekten geldi:
     **capex SONLUdur ve geliri KALICI büyütür; transfer SONSUZ sink'tir ve geliri büyütmez.**
     Önce merdiven, sonra kadro. → merdiven 11 sezonda bitti, sonra transfer devreye girdi.
- **SONUÇ: merdiven sonrası oran 2,25 → 1,911.** Sink GERÇEKTEN çalışıyor (14 alım · 5 fesih ·
  64M₺ transfer sink'i). Borç ratchet'i 2,40 → 2,00 sıkıldı ve ölçüm PİYASALI koşuya taşındı.
- **BORÇ KAPANMADI ve sebebi artık ÖLÇÜLÜ: HAVUZUN KALİTE TAVANI.** Kadro havuzun tepesine
  ulaşınca alacak kimse kalmıyor; 32 sezonda kasa 3,3 milyar ₺'ye çıkıyor. **Sınırlı bir yetenek
  havuzu sınırsız geliri ememez.** Kalan asıl mesele gelirin kendisi: stadyum kapasitesi üçe
  katlanıp KALICI yüksek gelir üretiyor, hiçbir gider onunla ölçeklenmiyor.
- **Kapı bunu koruyor:** `K10MerdivenSonrasiSink` artık piyasalı koşuyu ölçüyor ve piyasanın
  GERÇEKTEN işlediğini ayrıca denetliyor (transfer sink'i sıfırsa ya da hiç alım yoksa düşer) —
  aksi hâlde "borç iyileşti" sahte bir iyileşme olurdu.

### 🔴 K12-D: "ECONOMY_MAP'in iki kuralı çakışıyor" BULGUSU YANLIŞTI (2026-09-01)
K10-D'de kaydettiğim bulgu — "bandın alt ucunda referans merdiven ulaşılamaz" — **doğru değildi**
ve düzeltiyorum.

- **HATA NEREDEYDİ:** ölçümü yayın gelirini süpürerek yapmıştım ve "merdiven 40 sezonda bitmiyor"
  gözlemi **taban oran 1,041**'de alınmıştı. 1,041 ECONOMY_MAP'in bandının (1,05-1,15) **DIŞINDA**.
  Bandın içinde ölçmek yerine, bandın dışındaki bir noktadan banda EKSTRAPOLE ettim.
- **GERÇEK ÖLÇÜM (ikili aramayla bandın tam uçlarına oturtularak):** alt uç 1,050 → merdiven
  **19 sezon**; üst uç 1,150 → merdiven **9 sezon**. İkisi de `merdivenSezonBandi` [6,24] içinde,
  ikisinde de **iflas yok**. Çakışma YOK.
- **KAPIYA BAĞLANDI:** `K12DBantUclari` yayın gelirini ikili aramayla bandın uçlarına oturtur ve
  merdiveni HER İKİ UÇTA koşar. Aramanın gerçekten uca oturduğunu da ayrıca denetler — tutturamazsa
  kapı bandın ucunu değil rastgele bir noktayı ölçmüş olurdu. Bir sonraki ekstrapolasyonu kapı engeller.
- **Bu, K10-D'de (c) diye kapatılan kararı da geçersiz kılıyor:** korunacak bir çakışma yoktu.
  `merdivenSezonBandi`'nin geniş tutulma gerekçesi (fazla oranına duyarlılık) hâlâ geçerli — ama
  artık "bandın ucunda ulaşılamaz" değil, "bandın ucunda 19 sezon" diye biliniyor.
- **Kural (bu oturumda ÜÇÜNCÜ kez):** *ölçtüğün şeyin geçerli olduğu bölgenin İÇİNDEN ölç.*
  K11'de eksik kadro popülasyonu, K12-A'da eksik lig dağılımı, burada bandın dışından
  ekstrapolasyon. Üçünü de ölçüm yakaladı; hiçbirini okuma yakalamadı.

### K12 inceleme turu — ✅ TAMAM, dördü de GERÇEK çıktı (2026-09-01)

PR #26'ya iki inceleyici toplam altı bulgu yazdı (Codex 4, Bugbot 2 — biri Codex'inkiyle aynı).
**Beşi ayrı ayrı koda karşı doğrulandı, beşi de gerçekti** ve düzeltildi. Aşağıda Codex'in dördü;
Bugbot'un yeni bulgusu (süresi dolmuş teklif donması) ayrı kayıtta. Sıralama şiddet değil,
ETKİ sırasına göre:

1. **(P2) Maç sonrası hiçbir şey kondisyon/moral YAZMIYORDU.** K12-B "kondisyonu motora bağladım"
   diyordu ve eşleme doğruydu — ama repo genelinde `PlayerState.Kondisyon`a oynanış tarafından
   yazan tek bir çağrı yoktu. Yani her oyuncu her maça aynı 90 ile giriyordu ve **rotasyon oyunda
   yine hiçbir şey değiştirmiyordu.** İddiam gerçekte olduğundan genişti. Düzeltme:
   `shared/TheBadge.World/src/Squad/MacSonrasi.cs` + `squad.balance.json → macSonrasi`
   ([KALİBRE] `oynayanDusus 14`, `dinlenenArtis 9`, `kondisyonTaban 25`,
   `moralGalibiyet 6 / moralBeraberlik 0 / moralMaglubiyet -7`); `TheBadge.Play` haftalık döngüsü
   journal üzerinden çağırıyor, kadro ekranı kond/moral gösteriyor.
2. **(P1) Rakip kadrolar sessiz kondisyon avantajı alıyordu.** `Lig.RakipKadro` köprüye kondisyon
   dizisi geçirmiyor, köprünün "ayarlanmamış = tam enerji" nöbetçisine düşüyordu: oyuncunun 11'i
   955, rakibin 11'i 1000 enerjiyle sahaya çıkıyordu. Düzeltme: `LigKurucu.VarsayilanKondisyon 90`
   / `VarsayilanMoral 60`, iki taraf da AYNI yoldan.
3. **(P2) Değişiklikte giren oyuncu TAM enerjiyle geliyordu.** `ApplyPendingSubs` koşulsuz
   `slot.Energy = 1000` yazıyordu — yorgun bir yedek sahaya girer girmez taze oluyor, kondisyon
   modeli değişiklik yoluyla baypas ediliyordu. Doküman da aynı şeyi söylüyordu; ikisi birlikte
   düzeltildi.
4. **(P1) Serbest oyuncu transfer sink'ini kilitleyebilirdi.** `EnIyiHedef` serbest oyuncu
   dönebiliyor ama koşucu her hedefe `propose_offer` gönderiyordu; bus bunu `NotOwned` ile
   reddeder (K2 sahiplik denetimi) ve aynı hedef her hafta yeniden seçilirdi. Düzeltme: yol ayrımı
   (`KademeliInsaatKosu.TransferAksiyonu`) + OK olmayan her sonuç `BeklenmeyenRed` sayılıyor.

**DİŞ ÖLÇÜMÜ — dördü de ayrı ayrı söküldü, dördü de kırmızıya döndü:**

| Sökülen düzeltme | Kapının verdiği cevap |
|---|---|
| `slot.Energy = 1000` geri | `[değişiklik] giren oyuncunun enerjisi kadro girdisini izlemiyor (taze 1000 → 1000, yorgun 300 → 1000)` |
| Rakip kadro kondisyonsuz | `[rakip] 456 rakip girdisi oyuncunun yolundan SAPIYOR (beklenen enerji 955, momentum 1)` |
| `MacSonrasi.Isle` boşa çıkarıldı | `[maç sonrası] OYNAYAN yorulmadı (90 → 90) · OYNAMAYAN dinlenmedi · galibiyette moral artmadı` |
| Yol ayrımı sökülü | `K2Kapi3Sebepleri: [yol] serbest hedef sign_free_agent'e yönlendirilmiyor` |

### 🔴 BULGU: yazdığım ilk kapının İKİSİ de kendi varsayımını ölçüyordu (2026-09-01)

Bu turun asıl dersi düzeltmelerde değil, onları koruyacak kapıyı yazarken çıktı. İlk hâlde iki
kapı da yeşil değil, YANLIŞ ölçüyordu:

- **Değişiklik enerjisi kapısı maç SONUNDA bakıyordu.** Ölü topta toparlanma (+2/sn, ME 12.1)
  beş dakikada 300'ü de 1000'i de tavana çekiyor; kapı "taze 1000, yorgun 1000" görüp
  düzeltmenin çalıştığını sanacaktı. Çözüm: motor `Tick` `Tick` sürülüyor ve enerji
  **değişikliğin uygulandığı ANDA** okunuyor. Ölçüm anı, ölçülen şeyin parçasıdır.
- **Rakip kadro kapısı beklenen enerjiyi 90×10=900 diye YAZMIŞTI.** Köprünün eğrisi 90 → 955
  veriyor. Kapı kodu değil kendi aritmetiğini ölçüyordu. Çözüm: beklenen değer artık aynı
  kondisyondaki bir OYUNCU kadrosundan TÜRETİLİYOR — iddia da zaten buydu: "rakip, oyuncunun
  yolundan geçer".

**Ve bir tanesinin dişi hiç yoktu:** serbest oyuncu yol ayrımını söktüğümde merdiven koşusu
YEŞİL kaldı. Ölçtüm: 13 sezonluk piyasalı koşuda en iyi hedef **hiçbir zaman** serbest çıkmıyor
(0 kez) — yani düzeltme gerçek bir hatayı kapatıyor ama o senaryo onu hiç çalıştırmıyor. Bunu
"kapı var" diye geçmek, olmayan bir korumayı varmış gibi göstermek olurdu. Ayrım ayrı bir metoda
çıkarıldı ve kuralın kendisiyle YAN YANA (K2 sahiplik kapısında) ölçülüyor.

**Kural (yeni):** *bir düzeltmeyi koruduğunu söylediğin kapıyı, düzeltmeyi SÖKEREK ölç; sökünce
kırmızıya dönmüyorsa kapı o düzeltmeyi korumuyordur — nerede durduğundan bağımsız.*

### 🔴 BULGU (oyunu OYNARKEN bulundu): yorgunluk modelim bir CIRCIRdı ve seçim onu görmüyordu (2026-09-01)

İnceleme turu bitip her kapı yeşil olduktan sonra oyunu 6 hafta oynadım. Takım **ligin sonuncusu**
oldu: 1 puan, 3-16. Kapılar bir şey demiyordu çünkü yorgunluk kapısı **yönü** (oynayan yorulur mu)
ve **tabanı** (sıfıra düşer mi) ölçüyordu — modelin ŞEKLİNİ değil.

**Birinci hata — model bir denge değil bir circirdı.** "Oynayan −14, oynamayan +9" demiştim; yani
oynayan oyuncu HİÇ toparlanmıyordu. Düzenli ilk 11 beş maçta tabana (25) çakılıyor ve sezonun
kalanını orada geçiriyordu. Düzeltme: toparlanma HERKESE ve 100'e olan AÇIĞIN yüzdesi olarak
(`toparlanmaYuzde 35`, `toparlanmaTavani 20`). Bu bir denge noktası kurar:
`100 − oynayanDusus×100/toparlanmaYuzde` = **60**. Her hafta oynayan 60'ta oturur, dinlenen 100'e
tırmanır. `SquadBalance.Validate` artık bu dengeyi hesaplayıp tabanın altındaysa **yüklemeyi
reddediyor** — circir bir daha balance dosyasından giremez.

**İkinci hata — seçim yorgunluğu görmüyordu.** `SquadBridge` ham `Guc`a göre sıralıyordu: aynı en
güçlü 11 her hafta çıkıyor, erirken bile çıkmaya devam ediyordu. Konsolda rotasyon komutu da yok.
Yani model, oyuncunun karşılık veremediği düz bir cezaydı. Düzeltme: seçim **etkin güce** göre —
`Guc × ((1−e) + e × kondisyon/100)`, `secim.kondisyonEtkisi = 0.5` [KALİBRE]. Yorgun yıldız yerini
taze yedeğe bırakır; rotasyon sistemin KENDİ cevabı olur.

**ÖLÇÜM — ve burada kendi ölçümümü bir kez düzeltmem gerekti.** İlk tabloyu **6 haftalık**
koşulardan kurmuştum ve "düzeltilmiş model kontrolün de ÜSTÜNDE" diye yazmıştım. **O okuma
gürültüydü:** 6 maç, bir ligde bir örneklem bile değil. Rakip yorgunluğu eklendikten sonra aynı
yapılandırma 6 haftada 20. sıraya düştü — model değil örneklem konuşuyordu. **TAM SEZON (38 hafta),
aynı seed:**

| Model | Sıra | Puan | A-Y |
|---|---|---|---|
| Yorgunluk ~kapalı (`oynayanDusus 1`, kontrol) | 10. | 54 | 51-51 |
| Circir + ham güç seçimi | 14. | 42 | 46-60 |
| **Denge + etkin güç + rakip yorgunluğu (bugünkü)** | **11.** | **48** | **49-54** |

Doğru okuma bu: circir modeli tam sezonda **12 puana ve −14 averaja** mal oluyor ve oyuncunun
buna verecek cevabı yok. Düzeltilmiş model kontrolün **6 puan altında** — yorgunluk gerçek bir
maliyet, ama rotasyonla yönetilebilir bir maliyet. "Kontrolün üstünde" değil; öyle olsaydı
zaten yorgunluk bir kısıt olmazdı.

**Kural (yeni):** *6 maçlık bir örneklemden model sonucu çıkarma.* Circir bulgusunu 6 haftalık
koşu YAKALADI çünkü etki devasaydı (1 puana karşı 6); ama düzeltmenin BÜYÜKLÜĞÜNÜ aynı koşudan
okumak, gürültüyü ölçüm sanmaktı.

**Kapı artık şekli ölçüyor:** (1) 30 hafta üst üste oynayan TABANIN ÜSTÜNDE bir noktaya OTURUR ve
o nokta türetilen denge ile ±3 içinde uyuşur, (2) 30. haftada değişim sıfırdır (gerçekten oturdu),
(3) hiç oynamayan 100'e DÖNER, (4) kondisyonu tabana inen yıldız 11'den DÜŞER, (5) kondisyonlar
EŞİTken 11 değişmez (etkin güç, K11'in "seçim güce göre" iddiasını sessizce ezmesin). Diş: circir
geri konduğunda `CIRCIR: her hafta oynayan tabana çakıldı (25) — denge 60 olmalıydı`; seçim ham
güce döndüğünde `kondisyonu 25 olan yıldız (güç 85) hâlâ ilk 11'de`.

Küçük bir yan bulgu, kapı yakaladı: oranlı toparlanmada tam sayı bölmesi tepeye yakın sıfıra
düşüyor ve dinlenen oyuncu **98'de kilitleniyordu**. Açık varken toparlanma en az 1.

**Kural (yeni):** *bir modelin YÖNÜNÜ ve SINIRINI ölçmek şeklini ölçmez; dengesi olan her modelde
kapı DENGEYİ ölçmeli.* Ve daha genel olanı: bu bulguyu hiçbir inceleyici bulmadı, hiçbir kapı
bulmadı — **oyunu oynamak buldu.**

### 🔴 BULGU (Bugbot, gerçek): rakipler hiç yorulmuyordu — düzelttiğim asimetrinin SÜRÜKLENEN hâli (2026-09-01)

Yorgunluk modelini düzeltip yayınladıktan sonra Bugbot yeni bir bulgu yazdı ve haklıydı:
`MacSonrasi.Isle` YALNIZ oyuncunun kulübü için koşuyordu. Rakip `TeamSheet`ler açılışta bir kez
kuruluyor ve sezon boyu aynen kullanılıyordu — yani oyuncunun 11'i dengesine (kondisyon 60,
enerji ~700) inerken rakipler bütün sezon 90 kondisyonda (enerji 955) kalıyordu. **Maç 1'den sonra
her rakibe sessiz bir form üstünlüğü.**

Bu, K12 turunda düzelttiğim BAŞLANGIÇ asimetrisinin (rakipler 1000, oyuncu 955) sürüklenen hâliydi:
başlangıcı eşitledim, ama zamanla açılan farkı görmedim. Aynı sınıftan hatanın üçüncü tekrarı.

Düzeltme: `Kulup` artık ham kadro dizilerini taşıyor, `LigKurucu.HaftaSonu` her hafta
`MacSonrasi.YeniKondisyon` ile — **oyuncuyla BİREBİR AYNI aritmetik** — kondisyonu yürütüyor ve
kadroyu yeniden kuruyor (yorgunluk bir sonraki haftanın SEÇİMİNE de yansısın diye). İki ayrı
aritmetik yazmak aynı hatayı dördüncü kez davet ederdi; bu yüzden adım tek bir public metoda
çıkarıldı.

Ölçüm: 30 hafta sonra rakip 11'inin ortalama enerjisi **955 → 887**, aralık [820,987].

**Ve kapı beni yine yakaladı:** ilk yazdığım iddia "hiçbir rakip 11 oyuncusu başlangıç enerjisinin
üstünde olamaz"dı. Kırmızıya döndü — çünkü rakipler artık ROTASYON yapıyor ve dinlenip 100
kondisyona çıkan bir yedek 11'e girdiğinde enerjisi 987 oluyor. Davranış doğru, iddia yanlıştı:
yorgunluk bir tek oyuncunun değil kadronun SEVİYESİdir. İddia ortalamaya taşındı.

### 🔴 BULGU (Bugbot, gerçek): circir kontrolüm circirin bir kolunu açık bırakıyordu (2026-09-01)

Circiri engellemek için `SquadBalance.Validate`e denge kontrolü koymuştum:
`100 − oynayanDusus×100/toparlanmaYuzde` tabanın üstünde mi? Bugbot bunun **kırpmayı görmediğini**
söyledi ve haklıydı: çalışma anında toparlanma `toparlanmaTavani` ile de kırpılıyor. Tavan
yorgunluğa eşit ya da ondan küçükse hiçbir kondisyonda net kazanç olamaz — oyuncu tabana kayar —
ama sürekli sabit nokta bunu "60" diye okur ve yapılandırma kabul edilir.

Yani engellemek için yazdığım kontrol, engellediğini sandığım şeyin bir kolunu açık bırakıyordu.
Bugünkü değerlerde (tavan 20 > düşüş 14) sorun yok; kontrol GELECEKTEKİ bir [KALİBRE] değişikliği
içindi ve tam orada delikti.

`toparlanmaTavani > oynayanDusus` şartı eklendi (tavan şartı sağlandığında denge noktasındaki
toparlanma zaten kırpılmaz, yani sürekli formül orada geçerlidir). Kapı hem NEGATİF hem POZİTİF
yönü ölçüyor: tavan = düşüş olan yapılandırma REDDEDİLMELİ, geçerli yapılandırma reddedilMEMELİ
(kontrol fazla geniş olmasın). Diş: kontrol söküldüğünde `tavan ≤ düşüş olan balance KABUL EDİLDİ`.

Küçük bir yan ders: kapının ilk hâli yapılandırmayı JSON turuyla kopyalıyordu ve `nitelikSirasi`
kayboluyordu — kapı ölçmek istediği şeyi değil serileştirmeyi ölçecekti. Gerçek nesne geçici
olarak bozulup hemen geri alınıyor.

### 🟢 Ölçüm boşluğu KAPANDI (kısmen): nöbetçi artefaktın kendisine kondu (2026-09-01)

Haftalık döngünün üst-düzey deyimler içinde yerel bir fonksiyon olması ve `Sim.Checks`in onu
çağıramaması, bu turda **iki kez** ısırdı: önce `MacSonrasi.Isle` hiç çağrılmıyordu, sonra
rakiplerin hafta sonu. Üçüncüsünü beklemedim.

Kaynak metnine bakan bir kapı yazmadım — ilk yeniden düzenlemede yanlış yerden kırmızıya döner ve
kapı gevşetme baskısı üretirdi. Bunun yerine nöbetçi **konsolun kendisine** kondu ve DAVRANIŞA
bakıyor: en az iki hafta oynandıysa hem oyuncunun kadro kondisyonu hem rakip 11 enerjisi DEĞİŞMİŞ
olmalı; değişmediyse `!! HAFTA SONU DÜNYASI İŞLEMEDİ` yazıp çıkış kodu 2 ile düşüyor.

Diş, iki yarısı için ayrı ayrı ölçüldü: `MacSonrasi.Isle` söküldüğünde "oyuncunun kadro kondisyonu
6 hafta sonra HİÇ değişmedi", `LigKurucu.HaftaSonu` söküldüğünde "rakip 11 enerjisi 6 hafta sonra
HİÇ değişmedi" — ikisinde de çıkış kodu 2. Normal koşuda 0.

Bu, birim kapısının yerini TUTMAZ ve tutar gibi de yazılmadı: FAZ 02'de döngü test edilebilir bir
servise taşındığında asıl kapı gelir. Ama artık sessizce kaybedilemez.

### 🔴 BULGU (Bugbot, gerçek): süresi dolmuş teklif transfer politikasını DONDURUYORDU (2026-09-01)

Codex'in dört bulgusunun yanına Cursor Bugbot iki bulgu daha yazdı; biri Codex'in serbest oyuncu
bulgusunun aynısıydı, **ikincisi yeni ve K12-C'nin ölçümünü geçersiz kılıyordu.**

`TransferTick` süresi dolmuş teklife BİLEREK dokunmuyor (K5 inceleme kararı: yuva temizliği
`propose_offer`ın geri kazanımına ve kullanıcının `ret`ine ait; iki yerden temizlemek aynı yuvayı
iki gerekçeyle kapatırdı). Koşucu ise "yuva dolu = açık teklif" okuyordu (`TeklifId != 0`). Sonuç:
**ilk teklif süresi dolduktan sonra kulüp bir daha ne teklif veriyor, ne fesih yapıyor, ne kabul
ediyordu.** Politika donuyordu ve donduğunu kimse görmüyordu.

**ÖLÇÜM (13 sezonluk piyasalı koşu):** açık teklifli 508 haftanın **494'ü** tam olarak bu donmuş
durumdaydı — yani transfer motoru koşunun neredeyse tamamında ölüydü.

| | Donmuş (bulgu öncesi) | Düzeltilmiş |
|---|---|---|
| Alım | 14 | **29** |
| Fesih | 5 | **19** |
| Transfer sink | 64M₺ | **149M₺** |
| En uzun engelli seri | 494 hafta | **1 hafta** |
| Merdiven sonrası source/sink | 1,911 | **1,912** |

Düzeltme koşucuda: açık teklif = **CANLI** teklif. Dünya tarafı doğruydu, okuma yanlıştı.

**VE BU, K12-C'NİN SONUCUNU ÇÜRÜTMÜYOR — GÜÇLENDİRİYOR.** Transfer sink'i 2,3 KATINA çıktı ve
merdiven sonrası oran 1,911'den 1,912'ye "yükseldi" (yani hiç kıpırdamadı). K12-C'de "borcun asıl
kaynağı transfer sink'inin küçüklüğü değil, GELİRİN sınırsız büyümesi" diye kaydetmiştim; bu artık
bir yorum değil, kontrollü bir deney: **sink'i ikiye katlamak oranı 0,001 oynatıyor.** Kalan borç
gelir tarafındadır ve Atilla'nın kararını bekleyen üç seçenek (ücret enflasyonu / gelir doygunluğu)
aynen geçerlidir.

**Kapı:** politikanın açık teklif yüzünden ÜST ÜSTE durduğu en uzun hafta serisi ölçülüyor ve
tavan TÜRETİLİYOR (`tb.pazarlik.teklifGecerlilikHafta + 1` = 3). "Kaç transfer oldu" diye sormak
senaryonun zenginliğini ölçerdi; engellenme süresi ise doğrudan donmayı ölçer. Diş: düzeltme
söküldüğünde kapı `494 hafta ÜST ÜSTE ... (tavan 3)` diyerek kırmızıya döndü.

### 📐 Dikiş kuralının ikinci uygulaması: kapı artık oyunun GERÇEK kurulum kodunu koşuyor (2026-09-01)

Rakip kondisyon bulgusu, K11'de kaydedilen kuralın birebir tekrarıydı: *iki alt sistem ayrı ayrı
yeşilse, aralarındaki dikiş ölçülmemiş demektir.* `SquadBridge` yeşildi, `Lig.RakipKadro` yeşildi;
aradaki çağrı yanlıştı ve hiçbir kapı oraya bakmıyordu. Bu yüzden `TheBadge.Sim.Checks` artık
`server/TheBadge.Play`e referans veriyor ve kapı **oyunun gerçek lig kurulumunu** (`LigKurucu.Kur`,
19 rakip × 2 kadro × 11 = 418 girdi) koşarak ölçüyor.

**Kalan ölçüm boşluğu (gizlenmiyor):** haftalık döngünün KENDİSİ (`HaftayiOyna`) hâlâ üst-düzey
deyimler içinde yerel bir fonksiyon; kapı onu çağıramıyor. Yani "döngü `MacSonrasi.Isle`'yi
çağırıyor mu" sorusu bugün ancak gözle doğrulanıyor. Kaynak metnine bakan bir kapı yazmadım:
ilk yeniden düzenlemede yanlış yerden kırmızıya döner ve kapı gevşetme baskısı üretirdi. Bu boşluk
FAZ 02'de konsol yerini ekranlara bıraktığında kapanır — döngü test edilebilir bir servise
taşındığında kapı doğrudan onu koşar.

### K13-A: sınırsız gelir büyümesi kapatıldı — BORÇ KAPANDI (2026-09-02)

**Atilla kararı:** "(a) maaş enflasyonu + (c) gelir doygunluğu ile devam et." İkisi de yapıldı.

**ÖLÇÜLEN SORUN (önce şeklini çıkardım):** 40 sezonluk koşuda merdiven 11. sezonda bitiyor, sonra

| | sezon 1-10 | 11-20 | 21-30 | 31-40 |
|---|---|---|---|---|
| gelir | 1,73mr | 3,15mr | 3,14mr | 3,14mr |
| gider | 1,67mr | 1,72mr | 1,58mr | 1,58mr |
| oran | 1,037 | 1,827 | 1,988 | 1,989 |

Yani kapasite üçe katlanınca **gelir üçe katlanıyor, gider ise DÜŞÜYOR.** Borç tek satırda buydu.

**(c) GELİR DOYGUNLUĞU:** `EconomyTick.EtkinKapasite` — `referansKapasite`ye (30.000) kadar her
koltuk normal dolar, ötesi `ekKapasiteVerimi` (0,50) oranında. Şehrin taraftarı sonsuz değildir.
Sert kesme DEĞİL: stadyum büyütmek hâlâ kazandırır, yoksa merdivenin son basamakları anlamsızlaşır
ve `K10CapexSozlesmesi` haklı olarak kırmızıya dönerdi. Maç günü geliri, büfe/mağaza (kişi başı)
ve KOMBİNE aynı doygunluktan geçer — kombineyi unutmak, maç günü doyarken sezonluğun sınırsız
büyümesi demekti.

**(a) ÜCRET ENFLASYONU:** `UcretEnflasyonu.SezonBasi` — her sezon başı kadro ücretleri kulübün
bugünkü ölçeğindeki talebe doğru çekilir. Ölçek ETKİN kapasiteden gelir, yani doygunluk kolu
geliri kısarken ücret baskısını da kısar; iki kol aynı büyüklüğe bağlı. `EconomyTick.Hafta`ın
İÇİNDEN çağrılır (ayrı bir çağrı yeri, unutulabilecek bir yer daha demekti — bu oturumda o desen
üç kez ısırdı) ve `tb` parametresi ZORUNLU yapıldı ki hiçbir çağıran enflasyonu sessizce atlamasın.

**KAPILAR ÜÇ KEZ BENİ DÜZELTTİ:**
1. İlk yazımda ücret doğrudan `MaasTalebi`ye EŞİTLENİYORDU. Bu enflasyon değil YENİDEN TÜRETMEydi:
   fikstürün taban ücretleri formülden yüksekti, dolayısıyla ilk sezon başında bütün kadro
   ucuzluyordu. `K3EkonomiSozlesmesi` anında yakaladı (maaş payı %50 → %36,6, oran 1,47) ve
   `K3IflasEgrisi` de kırmızıya döndü. Model artık YALNIZ YUKARI çeker.
2. `K3IflasEgrisi` ikinci kez konuştu: senaryo kulüp maaş TOPLAMINI ×1,5 yapıyor ama oyuncu
   maaşlarına dokunmuyordu; sezon başı gözden geçirmesi toplamı kadrodan yeniden topladığı için
   o ×1,5 siliniyordu. Senaryo tutarlı hâle getirildi (aşırı harcama artık oyuncu maaşından
   gelir) ve **kulüp toplamı = kadro toplamı** değişmezi ilk kez kapıya bağlandı.
3. Kalibrasyonda bandın ALTINA geçtim (1,025). Kapı iki taraflı olduğu için yakaladı.

**KALİBRASYON TAHMİNLE DEĞİL SÜPÜRMEYLE:** 22 nokta ölçüldü.
`referansKapasite × ekKapasiteVerimi × ucretOlcekAgirligi` ızgarası, sonra `tierMaliyetTaban`.

**VE SÜPÜRME YAPISAL BİR ŞEY BULDU:** inşaat penceresi oranı hiçbir kalibrasyonda 1,05'e
ulaşmıyordu (en iyi 1,044) — çünkü referans politika parası varken hep harcıyor, yani PARA
SINIRLI, ve para sınırlı kulüpte gelirin tamamı capex'e gider: oran 1,00'a çakılır. Bandın orada
tutması bakiyenin değil POLİTİKANIN işiymiş. Politikaya nakit rezervi eklendi (28 hafta işletme
gideri) — bu aynı zamanda ölçümün kendi raporladığı bir yarayı da kapattı: referans kulübün en
düşük kasası −5,8M₺ idi, yani düzenli olarak eksideydi. Şimdi +20,0M₺.

`tierMaliyetTaban` ×0,70 ile ayarlandı: eski değer geliri SINIRSIZ büyüyen bir kulübe göre
kalibreydi; doygunluk gelince aynı merdiven 11 → 18 sezona çıkıyor ve bandın alt ucunda hiç
bitmiyordu.

**SONUÇ (hepsi aynı koşuda):**

| ölçüm | önce | sonra | bant |
|---|---|---|---|
| merdiven sonrası durağan oran | **1,99** | **1,107** | [1,05-1,15] ✓ |
| inşaat penceresi (capex dahil) | 1,131 | 1,058 | [1,05-1,15] ✓ |
| işletme oranı (capex hariç) | 1,49 | 1,298 | bandın üstünde ✓ |
| merdiven süresi | 11 | 11 | [6,24] ✓ |
| bant uçlarında merdiven | 19 / 9 | 15 / 11 | [6,24] ✓ |
| maaş payı | %49 | %51,2 | [%45-60] ✓ |
| 40 sezon sonu kasa | 4,62 milyar ₺ | ~0,6 milyar ₺ | — |
| referans kulübün en düşük kasası | −5,8M₺ | +20,0M₺ | — |

**BORÇ TAVANI KALDIRILDI.** `merdivenSonrasiOranTavani` (2,00) ve `merdivenSonrasiHedefOran`
balance'tan SİLİNDİ; kapı artık ECONOMY_MAP'in kendi bandını **iki taraflı** uyguluyor. Yani
gevşemedi, DARALDI — borç gözcüsünün işi buydu: hedefe ulaşıldığında kendini kapattırmak.

**DİŞ ÖLÇÜMÜ (iki kol ayrı ayrı):** doygunluk kapatılınca durağan oran **1,382** (pencere de
1,164 ile bant dışı), ücret enflasyonu kapatılınca **1,284**. İkisi açıkken 1,107 — yani ne biri
tek başına yetiyor, ikisi de yük taşıyor. Mekanizma kapısı (`K13ADoygunlukVeUcret`) ayrıca
şunları ölçüyor: referans altında kırpma yok, üstünde koltuk başına verim AZALIYOR, ücret yukarı
çekiliyor ama aşağı çekilMİYOR, sezon tavanı aşılmıyor, kulüp toplamı = kadro toplamı.

### 🔴 BULGU (Codex, gerçek): nakit rezervi BAKIMI atlıyordu — ve hiçbir kapı görmüyordu (2026-09-02)

K13-A'da referans politikaya eklediğim nakit rezervi "28 haftalık işletme gideri" diyordu ama
personel + genel işletme + maaşı topluyor, **tesis BAKIMINI atlıyordu.** Bakım her hafta işlenen
ve merdivenle BÜYÜYEN bir kalem (tier toplamı × tier başı ücret); referans tier'larda tampon
~13,9M₺ eksik kalıyordu. Yani inşaat iddia edilenden ERKEN başlıyor ve capex oranları daha küçük
bir tamponla ölçülüyordu.

**VE ASIL DERS BURADA:** düzeltmeyi yapıp dişini ölçtüm — **bakımı geri çıkardığımda bütün
kapılar YEŞİL kaldı** (pencere 1,104 → 1,075, hâlâ bant içi; alt uç 23 → 21). Gerçek bir hata,
hiçbir korumanın olmadığı bir yerde duruyordu. Bu oturumda ikinci kez: serbest oyuncu yol
ayrımının da dişi yoktu.

Kapı eklendi ve iddiası SABİT LİSTEYE değil TİCK'İN KENDİSİNE karşı kuruldu:
`rezerv == RezervHafta × (L.ToplamGider − InsaatTl − TransferTl − FaizTl)`. Capex ve transfer
komut anında düşüldüğü için, faiz de politika kredisiz olduğu için hariç (kapı ayrıca faizin
gerçekten sıfır olduğunu denetliyor). Bir liste yazsaydım, ileride eklenecek yeni bir işletme
kalemi yine sessizce dışarıda kalırdı. Diş: bakım çıkarıldığında
`tampon 63.635.000₺ ... 76.010.000₺ — rezerv bir kalemi atlıyor`.

**YENİDEN KALİBRASYON:** rezerv büyüyünce merdiven yavaşladı ve bandın ALT ucunda 24 sezona
(sınıra) dayandı. 22/25/28 yeniden ölçüldü:

| rezerv | pencere | alt uçta merdiven | durağan |
|---|---|---|---|
| 22 | 1,058 | 22 | 1,108 |
| **25** | **1,104** | **23** | **1,124** |
| 28 | 1,081 | 24 (sınır) | 1,109 |

25 seçildi: pencere bandın ortasında, alt uçta 1 sezon pay var. Gizlenmesin: **pencere oranı
rezervde monoton değil** (28, 22'den iyi ama 25'ten kötü) — merdiven adımlarının sezon
sınırlarına düşme zamanlaması ayrık bir etki yaratıyor. 25 bu ızgaranın en iyisi, kanıtlanmış
bir optimum değil.

### 🔴 K13-B: "müdahale kararını role duyarlı yap" ÖNERİSİ ÖLÇÜMLE ÇÜRÜDÜ (2026-09-02)

**Atilla kararı:** "düz dağılımda kırmızı borcu için (a) ile devam et" — yani müdahale KARARINI
role duyarlı yap, kötü müdahaleci dalmak yerine jokey yapsın. Uygulandı, ölçüldü, **çürüdü** ve
geri alındı. Kayıt burada duruyor ki aynı yol ikinci kez denenmesin.

**NE YAPILDI:** `MatchEngine`'in tackle kararına jokey kolu eklendi —
`p_jokey = clamp(egim × (def_etkin − atk_etkin − esik) / bolen, 0, tavan)`, `Domain.Decision`
akışında ([KALİBRE] `sim.balance.json → possession`).

**BİRİNCİ SÜRÜM SEÇİCİ DEĞİLDİ:** eşiksiz hâli (taban + eğim × fark) maç başına **~93 kez**
tetikleniyor ve iki popülasyonu da aynı oranda bastırıyordu — rol ayrımı değil, küresel bir kart
indirimi. Eşik eklendi (12 puan): tetiklenme 93 → **19**/maç, yani artık gerçekten yalnız geride
kalan savunucu jokeyliyor.

**VE ASIL BULGU:** seçici hâliyle de İKİ POPÜLASYONU AYIRMIYOR. `sariSonrasiIhtiyat` ince
süpürüldü (jokey AÇIK, 300 lig + 80 köprü maçı):

| ihtiyat | LİG kart | LİG kırmızı | KÖPRÜ kart | KÖPRÜ kırmızı |
|---|---|---|---|---|
| 0,18 | 2,77 ✓ | **0,107 ✓** | 8,00 ✗ | 1,350 ✗ |
| 0,19 | 2,67 ✓ | 0,077 ✗ | 7,42 ✗ | 1,150 ✗ |
| 0,20 | 2,69 ✓ | 0,073 ✗ | 6,83 ✓ | 0,775 ✗ |
| 0,21 | 2,66 ✓ | 0,060 ✗ | 6,31 ✓ | 0,525 ✓ |
| 0,22 | 2,62 ✓ | 0,037 ✗ | 5,78 ✓ | 0,312 ✓ |

(Bantlar: LİG kart 2,6-5,5 · kırmızı hedefi 0,10-0,36 · KÖPRÜ kart 2,50-7,00 · kırmızı 0,05-0,60.)

**Hiçbir değerde ikisi birden banda girmiyor.** K12-A'nın bulgusu, jokey kolu EKLENDİKTEN SONRA da
aynen geçerli.

**SEBEBİ ARTIK BİLİNİYOR — ve öneriyi çürüten şey bu:** köprü kadrosunun kart fazlası müdahale
SAYISINDAN değil müdahale ŞİDDETİNDEN geliyor. Jokey kolu SAYIYI azaltıyor; ama köprüdeki fauller
zaten iyi savunucu ↔ iyi hücumcu düellolarından çıkıyor (yetenek farkı ≈ 0, yani jokey hiç
tetiklenmiyor) ve şiddeti `marginGap`in sistematik büyüklüğünden alıyor. Öneri (a), yanlış
büyüklüğü hedefliyordu.

**İKİNCİ HİPOTEZ DE ÖLÇÜLDÜ VE ELENDİ:** şiddet ağırlığını `marginAgirlik`ten duruma
(hız + arkadan) kaydırmak. İki popülasyonun ORANINI gerçekten yaklaştırıyor (LİG/KÖPRÜ 0,33 →
0,56) ama mutlak seviyeyi patlatıyor: `marginAgirlik` 0,40 → 0,25'te lig kartı 2,84 → 9,95,
0,12'de 24,62. Kullanılabilmesi için `sariEsik` + `foulEsikTaban` ile birlikte dört parametreli
bir yeniden kalibrasyon gerekir ve o da bugün bantta olan 11 metriği yeniden riske atar.

**KARAR: jokey kolu GERİ ALINDI.** Borcu kapatmıyor, buna karşılık golden hash'leri (M2/M4/M6)
ve M14 sarı bandını (2,94, bant 3,0-5,0) bozuyor — yani tam bir yeniden kalibrasyon faturası
çıkarıyor, karşılığında ölçülmüş bir kazanç yok. Borç bugünkü hâliyle korunuyor:
`M16EKirmiziBorcu` tavanı tutuyor ve hedefi basıyor.

**AÇIK KALAN — ATİLLA'NIN KARARI:**
- **(a) jokey kolunu KENDİ değeri için geri getir.** Umutsuz bir müdahalecinin her seferinde
  dalması gerçekçi değil (ME 7.6 ruhu) ve mekanizma çalışıyor. Bedeli: 11 metriğin yeniden
  kalibrasyonu + golden yenileme, borç yine açık kalır. Ayrı bir dilim olarak açılmalı.
- **(b) dört parametreli şiddet yeniden kalibrasyonu** (marginAgirlik + sariEsik + foulEsikTaban
  + ihtiyat). Borcu kapatabilecek TEK ölçülmüş yol; riski, bugün bantta olan 11 metrik.
- **(c) borcu kapatma, bandı sorgula.** Düz/lig dağılımı YAPAY bir popülasyon: 11 özdeş oyuncu
  gerçek futbolda yok, ve sarılar dağıldığı için ikinci sarı doğal olarak seyrek. Borcun
  ölçtüğü şey bir model hatası değil, yapay kadronun kendi özelliği olabilir.
  **Önerim (c) + sonra (a) ayrı dilim** — ama bu bir bant sorgusudur, kararı sende.

### K13-C: bant SORGULANDI ve TEMİZ çıktı — borç yanlış yere asılmış (2026-09-02)

**Atilla kararı:** "(c) ile devam et" — borcu kapatma, **bandı sorgula.** Sorgulandı. Cevap:
**bant yanlış değil, borcun ÇERÇEVESİ yanlış.**

**(c)'NİN KENDİ HİPOTEZİ ÇÜRÜDÜ.** Önerirken şöyle demiştim: "düz dağılım yapay bir popülasyon;
11 özdeş oyuncu gerçek futbolda yok ve sarılar dağıldığı için ikinci sarı doğal olarak seyrek."
Ölçüm (500 maç × 3 popülasyon) bunu tutmadı:

| popülasyon | faul | sarı | kırmızı | doğrudan | ikinci sarı | sarı yoğunlaşması | ort şiddet | **en yüksek şiddet** |
|---|---|---|---|---|---|---|---|---|
| DÜZ | 26,8 | 4,53 | 0,000 | **0,000** | 0,000 | 1,021 | 0,498 | 0,692 |
| LİG | 19,9 | 2,94 | 0,030 | **0,000** | 0,030 | 1,025 | 0,492 | 0,746 |
| KÖPRÜ | 39,4 | 5,47 | 0,086 | **0,000** | 0,086 | 1,056 | 0,496 | 0,754 |

- **Yoğunlaşma hipotezi ölü:** üç popülasyonda da ~1,02-1,06. Sarılar her yerde benzer dağılıyor.
- **"Yapay kadro az kart üretiyor" da yanlış:** DÜZ, LİG'den DAHA ÇOK faul ve sarı üretiyor
  (26,8/4,53 vs 19,9/2,94) — ve yine de sıfır kırmızı.
- **GERÇEK BULGU:** kırmızıların TAMAMI ikinci sarı. Üç popülasyonda da **doğrudan kırmızı
  SIFIR**, ve görülen en şiddetli faul 0,754 iken eşik 0,80. Yol nadir değil, **ERİŞİLEMEZ.**

Yani gerçek futbolda kırmızının iki kaynağı var (doğrudan + ikinci sarı); bu modelde **bir tanesi
hiç çalışmıyor**. Bant (0,10-0,36) iki kaynaklı bir gerçekliğe göre yazılmış; tek kaynakla
tutturulamaması bandın değil modelin eksiği. Borç "düz dağılımda kırmızı" diye tek bir yapay
popülasyona asılmıştı; asıl yeri model kolunun kendisi.

**TEK PARAMETRELİ DÜZELTME DE ÖLÇÜLDÜ VE YETMEDİ.** `kirmiziEsik`i erişilebilir aralığa çekmek
akla yakın geldi ve süpürüldü: 0,72 → LİG 0,065 · 0,70 → 0,095 · 0,68 → 0,115. Ama kapının
KENDİ koşusunda (N=500, kendi tohum ailesi) 0,70 yalnız **0,05** veriyor — hedefin (0,10) altında
— ve üstelik `K11KadroKoprusu`nun enerji iddiasını kırmızı kart gürültüsünde bozuyor. 0,68'de ise
köprü kadrosu kendi bandını aşıyor (0,635 > 0,60). Borç tek anahtarla kapanmıyor.

### 🔴 KENDİ ÖLÇÜMÜMDE İKİ METOT HATASI (ikisini de kapı/karşılaştırma yakaladı)

1. **"DÜZ" popülasyonunu AYNA MAÇ olarak kurmuşum.** `entity 7`'ye karşı yine `entity 7`
   (yalnız PlayerId farklı) — yani takım kendisiyle oynuyordu. Faul 15,5 / sarı 1,39 okuyup
   "düz dağılım az kart üretiyor" diye yorumlamıştım. Gerçek popülasyon (7'ye karşı 8):
   **26,8 / 4,53.** Yorumun yönü tersine döndü.
2. **LİG'i kapının tohum ailesiyle değil kendi ailemle, üstelik N=200 ile ölçmüşüm.** 0,095
   okumuştum; kapının kendi koşusu (0xCA11B0, N=500) aynı ayarda 0,05 veriyor. Az kalsın
   200 maçlık bir örneklemden kalibrasyon kararı çıkaracaktım.

**Kural (yeni):** *bir kapının ölçtüğü şeyi tartışacaksan, KAPININ KENDİ popülasyonu ve tohum
ailesiyle ölç.* Yan yana koyduğun iki sayı farklı evrenlerdense karşılaştırma değil, gürültü.

### Kapı yeniden çerçevelendi: `K13CDogrudanKirmiziOlu`

Borç artık teşhise bağlı ve **iki taraflı**:
- **Yol canlanırsa** (doğrudan kırmızı > 0) kapı düşer ve borcun kapandığını söyler.
- **Boşluk kötüleşirse** (`kirmiziEsik − enYuksekSiddet` > 0,08) kapı kırmızıya döner.
- Ölçüm bedava: `M16ECalibGenis`in zaten koştuğu 500 maçın olay log'undan okunuyor.

**İlk yazımda kötüleşme iddiası TEK YANLIYDI** (inceleme bulgusu, Codex P2): yalnız şiddet
TAVANINI koruyordum ("0,70'in altına inmesin"), oysa borcun büyüklüğü BOŞLUK. `kirmiziEsik`
0,80 → 0,90 yapılsaydı boşluk 0,054'ten 0,154'e çıkar — borç kötüleşir — ama tavan kıpırdamadığı
için kapı YEŞİL kalırdı. Ekrana bastığım sayıyı iddiaya bağlamamıştım; bu oturumda üçüncü kez
aynı sınıf hata (doğru şeyi ölçüp yanlış şeyi iddia etmek).

Tavan 0,08: bugünkü boşluk 0,054 ve `enYuksekSiddet` 500 maçın MAKSİMUMU, yani uç-değer
istatistiği (koşudan koşuya 0,746-0,754 arası oynuyor). 0,08 gürültüye yer bırakır, eşik oynatma
ölçeğindeki gerçek kötüleşmeyi (0,10+) yakalar.

Diş üç senaryoda da ölçüldü: `kirmiziEsik` 0,70'e çekilince `doğrudan kırmızı 0.026 > 0 — YOL
CANLANDI`; 0,90'a çıkarılınca `boşluk 0.154 > 0.08 — BORÇ KÖTÜLEŞTİ`; `marginAgirlik` 0,20'ye
indirilince `boşluk 0.212 > 0.08 — BORÇ KÖTÜLEŞTİ`.

`M16EKirmiziBorcu` (LİG kırmızı oranı) DURUYOR — semptom hâlâ ölçülüyor; yeni kapı SEBEBİ
ölçüyor. Kayıttaki "düz dağılım" ifadesi de düzeltildi: ölçülen popülasyon LİG'dir.

### KARAR (2026-09-02, Atilla): **(c) — teşhis kapısıyla BEKLE**

Üç seçenek ölçümleriyle sunuldu:
- **(a) Şiddet modeline gerçek bir "doğrudan kırmızı" kolu ekle** (DOGSO / ciddi faul / şiddetli
  hareket), eşik oyunuyla değil. Bugünkü formül `margin + hız + arkadan` ile en fazla 0,754
  üretiyor; doğrudan kırmızı ayrı bir OLAY sınıfı olmalı, aynı sürekli skorun kuyruğu değil.
- **(b) `kirmiziEsik`i düşür ve yan hasarı kabul et.** Ölçüldü: hedefi tutturmuyor (kapının kendi
  koşusunda 0,05 < hedef 0,10), `K11KadroKoprusu`nun enerji iddiasını bozuyor, golden/replay
  yenilemesi gerektiriyor. Önerilmedi.
- **(c) Teşhis kapısıyla bekle.** ← **SEÇİLEN**

**Kararın anlamı:** borç BUGÜN kapatılmıyor ve bu bir erteleme değil, ölçülmüş bir tercih.
Gerekçe: doğrudan kırmızı gerçek bir olay sınıfı istiyor; bugün eklemek bant içindeki 11 kalibre
metriği yeniden riske atardı ve (b) ölçüldüğü gibi hedefi zaten tutturmuyor. Borç şu an
**görünür, sayısal ve iki taraflı korunuyor** — `K13CDogrudanKirmiziOlu` yol canlandığında da
boşluk kötüleştiğinde de kırmızıya döner (dişi iki yönde ölçüldü).

**(a) kapatılmadı, TETİKLEYİCİYE bağlandı** — aşağıdaki "Bekleyen kararlar" listesine
`LOD 1` satırıyla aynı biçimde geçti: bir karar değil, koşulu net bir bekleyiş.

### 📐 KURAL: yapıldığını hatırladığın şey, ölçülmüş şey değildir (2026-09-02)

FAZ 04 kapanış brifi (PR #29) bu kuralı iki turda iki kez kanıtladı ve ikisi de **aynı kökten**
geldi: brifi oturum içi hafızadan yazdım, sonra kaynağa karşı doğruladım.

**Birinci tur — beş SAYI yanlıştı.** Taze koşuya karşı karşılaştırınca: kapı sayısı 177 → **176**,
inşaat penceresi 1,058 → **1,104**, işletme oranı 1,298 → **1,353**, merdiven sonrası durağan
1,107 → **1,124**, bant uçlarında merdiven 15/11 → **23/10**. Hepsi son kalibrasyondan (rezerv
haftası 25) ÖNCEKİ ölçümlerden kalmıştı — yani bir zamanlar doğruydular, bu da tam olarak onları
tehlikeli yapan şey.

**İkinci tur — iki KAPSAM iddiası yanlıştı** (Codex inceleme bulguları, ikisi de gerçek):
- K6 satırı "Online (Nakama RPC) + SimWorker ✅" diyordu. Oysa `server/SERVER_SETUP.md` RPC
  kaydını, PostgreSQL persist'i ve keyframe yayınını "YAPILMADI" başlığı altında SAYIYORDU. Brif
  kendi kaynak dosyasıyla çelişiyordu; ben o dosyayı yazan taraftım ve yine de yanlış hatırladım.
- "Balance dosyaları (hepsi config_hash içi)" diyordu. `ConfigHash.Compute` yalnız
  `sim.balance.json` + `command.bands.json` alır. Yani `world/economy/squad/transfer/market/
  sim.lod2` değişince golden set **bayatlamaz** — brife güvenen biri var olmayan bir güvenceye
  yaslanırdı. Aynı hata bir seviye daha derinde, `TransferBalance.cs` başlığında da vardı.

**Kural:** *Yapıldığını hatırladığın şey, ölçülmüş şey değildir.* Bir devir teslim belgesindeki
her sayı taze bir koşudan, her kapsam iddiası ise onu YAPAN ya da YAPMADIĞINI SÖYLEYEN kaynak
dosyadan doğrulanır. "Bunu ben yazdım, biliyorum" bir kanıt değildir — hafıza, koda dokunulduğu
anda bayatlar ve bayatladığını kendisi söylemez.

**Neden bu kural ayrı yazıldı:** projedeki diğer kurallar KODUN ölçülmesini düzenliyor. Bu kural
RAPORUN ölçülmesini düzenliyor ve boşluk oradaydı — 176 kapı yeşilken bile brif yanlış olabilir,
çünkü hiçbir kapı bir markdown cümlesini ölçmez. Kanıt yükü belgeyi yazanda; bir sonraki fazın
yaslandığı yüzey, o belgede yazan yüzeydir.

## 5G DİKEY DİLİM AÇILDI (2026-09-04)

Açılış brifi: `docs/briefs/BRIEF_5G_DIKEY_DILIM.md`. Anayasa v2.1 **Aşama 5G / 4G.7**.
Dilimin tanımı DECISIONS'ın kendi eski kaydından geliyor: **tek maç günü final kalitede**
(+ sandbox IAP + cihazda fps) — ve "maç günü" GAME_THESIS'in Session Shape'idir (8-15 dk).

> İSİM UYARISI: Anayasa'nın **Aşama 5G**'si ile GDD'nin **FAZ 05**'i (Asset Üretimi) AYNI ŞEY
> DEĞİLDİR. Seri asset üretimi bu kapının ARKASINDADIR.

### D-A: KARAR (2026-09-04, Atilla) — **(a) 5G iki kapılı açılır**

**Sorun:** Anayasa'nın sırası Fun Gate → Vertical Slice'tır ve 4G.10/19 nettir: *fun kanıtlanmadan
art ve içerik üretimine para/zaman gömülmez.* Bizim Fun Gate'imiz **%40 (2/5) ile NO-GO** kapandı
ve **kopuş nedeni ölçülmedi** (mülakat kaydedilmedi, telemetri repoya kopyalanmadı). DECISIONS
iki aşama tarif ediyor ve bunlar çelişmiyor: *"Dikey Dilim (5G) **öncesi** ... küçük, MÜLAKATLI
gözlem turuyla doğrulanır"* (önkoşul) ve *"fun doğrulamasının **nihai yükü** 5G'ye taşındı"*
(kapının kendisi). **Birincisi ödenmemişti** — ve ödenmemiş olması sadece bir gecikme değil:
sunum yeniden tasarımının GİRDİSİ yok.

| | Kapsam | Çıkış kapısı |
| --- | --- | --- |
| **5G-a** | Paket köprüsü + maç sunumunun GERÇEK motor üstünde yeniden tasarımı (**placeholder art**) + mülakatlı gözlem turu (3-5 kişi) | "bir maç daha" sinyali + **kopuş nedeninin YAZILI olması** |
| **5G-b** | Final kalite: art, ses/haptic, FTUE ilk 5 dk, sandbox IAP, analytics hunisi, cihaz performansı | 4G.7 Vertical Slice Gate + persona paneli (9.7) |

Kapıyı gevşetmek değil **sırasını düzeltmek**: 5G-a günler, 5G-b haftalar ve art üretimi geri
alınamaz. Elenen (b) tam dilimi şimdi başlatmak (girdisiz tasarım + geri alınamaz harcama),
elenen (c) fun borcunu tamamen kapıya bırakmak (kayıttaki "öncesi" satırını sessizce silerdi).

### D-C: KARAR (2026-09-04, Atilla) — **(a) `World` + `CommandBus` Unity paketi olur** → ADR-002

Gerekçe ve elenen seçenekler `docs/adr/ADR-002-unity-paket-siniri.md`de. Özet: (b) ince cephe
CB'nin kendi "istemci ön-doğrular" mimarisine aykırıydı, (c) sunucu-only bugün Nakama bağlaması
olmadığı için çalışmıyor.

### S1 UYGULANDI — Unity paket köprüsü (2026-09-04)

`TheBadge.World` ve `TheBadge.CommandBus` artık `TheBadge.Sim` ile aynı desende Unity paketi.
Yeni kapı **`S1UnityPaketSiniri`** altı şeyi birden ölçüyor; dişi dört koruma SÖKÜLEREK ölçüldü.

**Kapının ilk koşusunda YAKALADIĞI ilk şey benim kendi hatam oldu:** Unity manifest'indeki
`file:` yolları **Packages klasörüne** görelidir, proje köküne değil; ben proje kökünü taban
almıştım ve üç paketin de yolu "çözülmüyor" diye kırmızıya döndü. Kapı yazılırken ölçtüğü şeyi
gerçekten ölçtüğünün kanıtı budur.

**BULUNAN GİZLİ TUZAK (ölçüm sırasında çıktı):** Unity, paket klasöründeki **TÜM** `.cs`
dosyalarını derler. `dotnet build`in ürettiği `obj/Release/netstandard2.1/*.AssemblyInfo.cs`
paket klasöründe kalıyordu — Unity CS0579 (yinelenen öznitelik) ile düşerdi. Tuzak **bugün
`TheBadge.Sim` için de vardı** ve patlamamasının tek sebebi, Unity'yi açan kişinin o klasörde
henüz `dotnet build` koşmamış olmasıydı: yani sessizce bekleyen bir mayındı. Üç pakete
`Directory.Build.props` konup çıktı repo kökündeki `artifacts/`e yönlendirildi; kapı `src/`
dışında `.cs` kalmadığını doğruluyor.

**Ders (kural adayı değil, gözlem):** bir paket sınırını "dosya var mı" diye ölçmek yetmez —
sınırın DİĞER TARAFTAKİ derleyicisinin ne göreceğini ölçmek gerekir. Bu ortamda Unity yok, ama
Unity'nin derleyeceği DOSYA KÜMESİ ölçülebilir; kapı orayı tutuyor.

### D-B: KARAR (2026-09-04, Atilla) — **canlı yol**

Dikey dilim yalnız **canlı maç yolunu** final kaliteye çeker. Replay/özet yolu (tezin "canlıyı
kaçırmak ceza değildir" vaadi) dilim DIŞINDA — iki sunum yolunu birden final kaliteye çekmek
dilimi ikiye katlardı. Kabul edilen risk açıkça yazılı: dilim, tezin "kaçırsan da olur" kolunu
test ETMEZ; o kol sonraki dilime kalır. Playtest turu için insanları Atilla bulacak
(kayıtlı kural: mülakatsız playtest koşulmaz).

### 🔴 S2 BULGUSU: sunumun yaslanacağı KAZANMA OLASILIĞI motorda YOK (2026-09-04)

S2'nin ilk işi "sunumu gerçek motor üstünde yeniden tasarla"ydı. Tasarıma başlamadan önce motorun
maç ORTASINDA ne verdiğine baktım ve greybox'ın fun hipotezinin dayandığı büyüklüğün karşılığının
olmadığını **ölçtüm**.

**Greybox'ın hipotezi** (RA#1 revizyonu, 2026-08-02): *"model + görünür olasılıklar + müdahale
döngüsü — **karar ver → kazanma ihtimali DEĞİŞSİN → sonucu yaşa**"*. Greybox bunu KESİN DP ile
üretiyordu ve şeridi KALİBREydi (tahmin %37 vs gerçekleşen %40, <0,10 bant).

**Motorda bugün ne var:** `MatchEngine.WinProb(int golFarki, int dakika)` — imzası bu, başka
girdisi YOK. Takım gücü, momentum, xG, kırmızı kart, müdahale: hiçbiri girmiyor. Bu bir hata
değil, ME 15.3'ün `xG_salınımı` terimi için yazılmış ve o işi yapıyor (highlight sıralamasında
yalnız GÖRECELİ sıçrama gerekir). Sunum omurgası olmak için yazılmamıştı.

**ÖLÇÜM** (3 senaryo × 300 maç, ORTA chaos, `BuildSheetSide` ofsetleriyle 60v60 / 75v55 / 55v75;
tohum ailesi `0xA5E7000 + n×7919`, `AutoManage = true`, eğri `BuildSummary(...).WinProbHome`ten
okundu — yeniden üretmek için bu üçü birlikte gerekir, K13-C'nin dersi):

| senaryo | gerçek EV galibiyeti | şerit 0. dk | şerit 45. dk | şeridin tam %50'de kaldığı süre |
| --- | --- | --- | --- | --- |
| dengeli 60v60 | %35,7 (B %29 · M %36) | **%50,0** | %50,0 | **%51** |
| güçlü EV 75v55 | **%80,7** | **%50,0** | %74,7 | %34 |
| güçlü DEP 55v75 | **%5,3** | **%50,0** | %25,5 | %32 |

Kalibrasyon (dakika başına örnek, üç senaryo birlikte) — en kalabalık kova en kötüsü:

| kova | n | tahmin | gerçekleşen | sapma |
| --- | --- | --- | --- | --- |
| %20-30 | 8.469 | %25,6 | %7,0 | **−18,6** |
| **%50-60** | **31.547** | %50,0 | **%31,5** | **−18,5** |
| %70-80 | 8.428 | %74,6 | %80,3 | +5,6 |
| %90-100 | 11.716 | %97,2 | %98,6 | +1,5 |

**Üç ayrı sorun, üçü de ölçülü:**
1. **Güce kör.** Kaç vuruşta şerit ÜÇ senaryoda da tam %50 diyor — gerçek galibiyet oranı %80,7
   ile %5,3 arasında değişirken. 75v55 maçına başlayan oyuncu "50/50" görüyor.
2. **Maçın üçte biri-yarısı boyunca DONUK.** Şerit yalnız skor değişince oynuyor; dengeli maçta
   dakikaların %51'inde tam %50'de duruyor.
3. **Ortada kalibre değil.** 0-0 "yarı yarıya" demek değil: beraberlik ayrı bir sonuç. En kalabalık
   kova 18,5 puan sapıyor.

**Ve asıl sonuç:** `WinProb`un girdisi (gol farkı, dakika) olduğu için bir **taktik müdahalesi
şeridi TAM OLARAK SIFIR kadar oynatır**. Yani greybox'ın şeridi motora olduğu gibi taşınsaydı,
fun döngüsünün çekirdek vaadi — *"karar ver → ihtimal değişsin"* — sessizce ölürdü. Kapı bunu
görmezdi: `M14PaketSemasi` yalnız dizinin 90 elemanlı OLDUĞUNU denetliyor, DEĞERİNİ değil.

**Bugün canlı bir hata YOK:** `WinProbHome`u motor dışında tüketen kimse yok (röportaj promptu,
Hikaye Motoru, Panorama — hiçbiri okumuyor). Eğri atıl. Sorun "bozuk tüketici" değil, **sunumun
ihtiyaç duyduğu büyüklüğün henüz var olmaması**.

**ÖNEMLİ ÇERÇEVE (kararı etkiler):** greybox'ın şeridi ÇALIŞIYORDU ve %40 onunla ölçüldü. Yani
doğru bir şerit yapmak bir DÜZELTME değil, **PARİTE**dir — onsuz motor sunumu, zaten kalan
greybox'tan daha kötü başlar. Redesign'ın kendisi ayrıca bir şey değiştirmek zorunda.

Seçenekler ve önerim "Bekleyen kararlar"da (S2-A).

### S2-A KARARI (2026-09-04, Atilla): **(a) + (b), sırayla**

### S2-A/(a) UYGULANDI — canlı kazanma olasılığı (2026-09-04)

`TeamRating` (takım gücünün TEK tanımı, `Lod2Resolver` buraya devretti) + `LiveWinProb`
(üç sonuçlu Poisson konvolüsyonu). Katsayılar motorun kendi davranışından oturtuldu
(28 eşleşme × 300 maç, R² = 0,977); `-- fit-winprob` yeniden oturtur ama balance'ı EZMEZ.

| | en büyük kova sapması |
| --- | --- |
| Eski şerit (ME 15.3 `WinProb`) | 0,214 |
| Yeni `LiveWinProb` | **0,051** |

Doğrulama oturtma verisinden AYRI tohum ailesiyle. **Sunum-only olduğu kanıtlandı:** 50 golden
replay'in durum hash'leri ve skorları birebir aynı; yalnız config_hash kaydı.

**KAPININ İKİ KEZ DİŞİ YOKTU — ikincisi bir DERS:**

1. Kapıyı M16-E'nin döngüsüne "bedava" bindirmiştim. `gucKatsayisi = 0` ile modeli güce KÖR
   yaptım, kapı geçti (0,048): M16-E'nin ofset çekilişi farkı 0 civarında yoğunlaştırıyor.
   Doğru büyüklük, YANLIŞ popülasyon. Kapı kendi popülasyonuna taşındı (fark −24..+24).
2. **Geniş popülasyonda DA geçti (0,059).** Sebebi daha derin: simetrik bir popülasyonda
   TABAN ORANI basan model MARJİNAL olarak kalibredir ve tamamen işe yaramazdır.
   **Kalibrasyon, AYIRT EDİCİLİĞİ ölçmez.** Kapıya kaç vuruşu Brier BECERİ payı eklendi.

Diş (son hâl): güce kör model → beceri −0,008 → "AYIRT EDİCİLİK YOK" (kalibrasyonu 0,059 ile
hâlâ geçiyor, ölçünün gerekçesi bu) · bir eğri dizisi dolmazsa → 0,248 + toplam hatası ·
bozulmamış → sapma 0,051, beceri 0,177.

### 🔴 S2-B ÖLÇÜMÜ: TAKTİK sonucu ÇOK güçlü değiştiriyor — ve üç tuhaflık var (2026-09-04)

(b)'nin ilk sorusu şuydu: şerit bugün gol, kırmızı kart ve oyuncu değişikliğiyle oynuyor ama
taktikle OYNAMIYOR — bu modelin eksiği mi, yoksa motorda taktik zaten etkisiz mi?

**Ölçüm** (dengeli 60v60, AYNI tohumlar, 400 maç/kol, `TacticChangeCmd` kaç vuruşunda):

| kol | EV galibiyeti | kontrole göre |
| --- | --- | --- |
| hat +2 | %44 | +9 |
| mentalite +2 | %42 | +7 |
| **KONTROL** | %35 | — |
| tempo +2 | %34 | −1 |
| pres +2 | %26 | −9 |
| mentalite −2 | %23 | −12 |
| **tam hücum (2,2,2,2)** | **%28** | **−7** |
| tam kapanma (−2,−2,−2,−2) | **%7** | −28 |

**Ana sonuç: greybox'ın vaadi motorda DOĞRU.** Taktik kazanma olasılığını 37 puanlık bir
aralıkta oynatıyor — `LiveWinProb`un yakaladığı güç farkından bile geniş. Yani şerit bugün
oyuncunun elindeki EN BÜYÜK kolu hiç görmüyor.

**Kadran başına ölçüm** (250 maç/kol, gol oranının kontrole göre log oranı):

| kadran | kendi golü | rakip golü |
| --- | --- | --- |
| mentalite −2 → +2 | −0,145 → +0,341 (tekdüze artan) | +0,213 → +0,118 (**tekdüze DEĞİL**) |
| tempo −2 → +2 | −0,809 → +0,131 (−2 felaket) | −0,062 → +0,188 |
| **pres −2 → +2** | **+0,051 → +0,039 (DÜZ)** | **+0,152 → +0,486 (tekdüze artan)** |
| hat −2 → +2 | −0,181 → +0,154 | +0,195 → −0,148 |

**ÜÇ TUHAFLIK (gözlendi, sebebi ÖLÇÜLMEDİ):**
1. **Kadranlar toplanabilir değil:** mentalite +2 (+7) ve hat +2 (+9) tek tek iyi, ama dördü
   birden hücuma alınınca sonuç kontrolden KÖTÜ (−7). Etkileşim gerçek.
2. **`pres` saf ceza:** kendi golünü hiç artırmıyor (düz), rakibin golünü tekdüze artırıyor
   (+2'de %49 daha fazla). Motorda pres yapmak KESİN olarak kötü. Gerçek futbolda pres topu
   yukarıda kazanıp şans üretir; bu, kalibrasyon sorunu olabilir.
3. **`pres` −1 ile −2 BİREBİR AYNI** (0,96/1,08 her ikisinde de, aynı tohumlarla): kadran
   negatif tarafta DOYUYOR — yani oyuncuya sunulan bir seçenek hiçbir şey yapmıyor.
4. Savunmacı yön daha ÇOK gol yediriyor: mentalite −2'de rakip +0,213, tam kapanmada rakip
   1,38 (kontrolde 0,95).

**MODEL BU VERİYLE HENÜZ BESLENMEDİ ve bu bilinçli:** `pres` kalibrasyonu düzelirse oturtulan
katsayılar çöp olur; kararsız zemine model kurmak israf. Karar S2-B'de.

### S2-B KARARI (2026-09-04, Atilla): **(a)** — ayrı motor dilimi, (b) beklemeden devam

Taktiğin üç tuhaflığı (pres saf ceza · pres negatif tarafta doyuyor · savunmacı yön daha çok gol
yediriyor) AYRI bir motor dilimine gitti; (b) onu beklemeden taktiği şeride kattı.

### S2-A/(b) UYGULANDI — şerit artık taktiğe cevap veriyor (2026-09-04)

`LiveWinProb` taktik girdisi aldı: her kadran için `*Kendi`/`*Rakip` katsayısı + bir `asiriUc`
terimi. Şerit ölçülen davranışı izliyor: kontrol %34,5 · mentalite+2 %43,6 · tam kapanma %17,2.

**ÜÇ ŞEY YANLIŞTI, ÜÇÜ DE ÖLÇÜMLE BULUNDU:**

1. **Katsayıları GOL ORANINDAN oturtmuştum.** Doğal görünen yol; model yönü tutturuyor ama
   büyüklüğü ıskalıyordu (`tamHücum` %63,4 derken gerçek %38,8). Hedef sonuç olasılığı, gol oranı
   ise yalnız bir VEKİL. Doğrudan sonuca oturtmak hatayı 0,419 → 0,267'ye indirdi.
2. **Ana etkiler TEK BAŞINA yetmedi.** Kadranlar toplanabilir değil: dördü birden uca çekilince
   sapma 18-22 puan. Fiziksel olarak anlamlı TEK bir terim eklendi — kadran karelerinin toplamı
   (`asiriUc`): dengesiz, her kolu uca çeken kurulum kendine az fayda, rakibe çok alan verir.
   Hata 0,267 → **0,053**, en büyük sapma 0,225 → **0,080**. `tamHücum` artık birebir tutuyor.
3. **Beceri örneğini dakika 0'dan alıyordum.** Motorun tick döngüsünde `SampleCurves` komut
   uygulamasından ÖNCE koşuyor (MatchEngine 422 vs 424), yani dakika 0 örneği kaç vuruşunda
   verilen taktiği HENÜZ GÖRMEZ. Dakika 1'e alındı.
   **Bu aynı zamanda sunum tarafında gerçek bir kusurdur:** şeridin İLK okuması, oyuncunun maç
   öncesi taktik kurulumunu yok sayar. Bugün kapı dakika 1'den ölçerek etrafından dolaşıyor;
   ekran yazılırken bu görünür hale gelir (borç).

### 📐 KURAL: mutlak bir eşik değil, ULAŞILABİLİR TAVANIN PAYI ölçülür (2026-09-04)

Ayırt edicilik kapısına önce mutlak taban (0,10) koymuştum. Taktik alt popülasyonu 0,042 verdi ve
kapı düştü. **Modeli suçlamadan önce tavanı hesapladım:** her kolun KENDİ gerçek frekansını bilen
KÂHİN model bile ancak **0,045** alıyor. Yani 0,10'luk taban TEORİK MAKSİMUMUN ÜSTÜNDEYDİ —
kapı kötü modeli değil İMKÂNSIZI istiyordu. Modelin gerçek başarısı tavanın %93'ü.

Sebebi futbolun kendisi: sonuçlara maçtan maça rastgelelik hâkim ve hiçbir model onu açıklayamaz.
Açıklanabilir olan yalnız KOLLAR ARASI fark; büyüklüğü de alt popülasyona göre değişir (güç
kolları %5-92 yayılıyor, taktik kolları %6-44). **Mutlak eşik bu yüzden anlamsızdır; doğru soru
"bilinebilirin ne kadarını yakalıyor".** Kapı artık payı ölçüyor (taban %50).

Bugün: **güç tavanın %97'si · taktik %86'sı** · kalibrasyon sapması 0,057.
Diş: taktik katsayıları sıfırlanınca −%40, `gucKatsayisi` sıfırlanınca −%2 → ikisi de kırmızı.

### 🔴 BULGU (Codex, P1, gerçek): canlı şerit için motor HİÇBİR ŞEY sunmuyordu (2026-09-05)

Bulgunun kendisi: `wp3*` dizileri yalnız dakika başlarında ve `queue.ApplyDue`dan ÖNCE yazılıyor,
bu yüzden müdahalenin etkisi bir sonraki dakika sınırına kadar görünmüyor.

**Doğrularken bulgu daha da keskin çıktı:** diziler `private` ve dışarıya YALNIZ maç sonunda
`BuildSummary` ile veriliyor. Yani canlı şerit için motor bir dakika gecikmeli değil, **hiç**
veri sunmuyordu. Kendi kaydımda ("şeridin ilk okuması taktiği görmüyor") sorunu dakika 0'a özel
sanmıştım; Codex genelleştirdi ve haklıydı — sorun her müdahalede, her dakika içinde vardı.

**Düzeltme:** `MatchEngine.AnlikOlasilik(in MatchState)` — durumu okur, yazmaz; kalan süreyi tick
çözünürlüğünde hesaplar. Dizilerin anlamı da kesinleştirildi: onlar MAÇ SONU İNCELEME eğrisidir
("dakika m'nin başı, o tick'in müdahaleleri uygulanmadan önce"), canlı sunum onları OKUMAMALIDIR.

**Kapı `S2AnlikOlasilikCanli`** — dört taktik + kırmızı kart, hepsi AYNI TICK içinde şeridi en az
2 puan oynatmalı. Bugün: mentalite+2 %32,1→%37,4 · hat+2 →%39,0 · tam kapanma →%9,1 ·
pres+2 →%22,2 · kırmızı →%23,5. **Diş:** kusur geri konunca (dizinin son dakikası okunursa)
beş senaryonun BEŞİ de düşüyor.

Ölçüm çıktısını okurken kendi biçim hatamı da yakaladım: `{x:+0.0}` .NET'te LİTERAL artı basar,
işareti değil (negatif değerler "-+0.2" görünüyordu). Bölüm ayırıcısına çevrildi (`+0.0;-0.0`).

### K1 KARARI (2026-09-05, Atilla): **(b) — sunum kritik anlarda duraklar**

Motor sürekli koşar; sunum, sonucun maddi olarak kaydığı anlarda durur/vurgular. Bu, greybox'ın
8-12 bloklu ritmini gerçek motorun sürekli akışına taşımanın yolu.

### 🔴 TUZAK: ME 15.3'ün eşiği bu işi SÜREMEZ (2026-09-05)

Kararın doğal uygulaması "highlight eşiğini kullan" olurdu. Ölçüm bunu çürüttü — ve bu zaten
kayıtlıydı: `H > highlight.esik` maç başına **0,5-0,8** işaret veriyor ve **maçların yarısı BOŞ**
(`TimelineMarks`ın eşikten değil "en yüksek N"den beslenmesinin sebebi de buydu). Sıfır ya da bir
duraklamalı bir maç ritim değildir. "En yüksek N" ise CANLI kullanılamaz: maç bitmeden kimin ilk
altıda olduğu bilinemez.

**Kullanılan ölçüt: kazanma olasılığının SIÇRAMASI** (son duraklamadan bu yana toplam değişim
mesafesi). Döngünün kendi vaadine bağlı — duraklama tam da sonucun kaydığı anda olur.

**ÖLÇÜM** (200 maç, karışık güç dağılımı, örnekleme 3 sn maç zamanı):

| eşik | an/maç | boş maç |
| --- | --- | --- |
| 0,02 | 19,0 | %0 |
| 0,03 | 12,9 | %0 |
| **0,04** | **9,9** | **%0** |
| 0,05 | 8,1 | %0 |
| 0,10 | 4,5 | %0 |
| 0,15 | 3,2 | %2 |

0,04 seçildi: greybox'ın 8-12 blok ritmine oturuyor, hiçbir maç boş kalmıyor.

**KADANS BAĞIMSIZLIĞI ÖLÇÜLDÜ, VARSAYILMADI.** Taban yalnız ateşlendiğinde sıfırlandığı için
sık örnekleme aynı sıçramayı daha ERKEN yakalar, daha ÇOK değil — ama bu bir tasarım İDDİASIydı,
ölçmeden yazmadım: 1 sn → 30 sn arası 30 kat aralıkta sayı 10,0 / 9,9 / 9,9 / 9,7 / 9,5.
Sunum kare hızını serbestçe seçebilir.

**Uygulama:** `TheBadge.Sim/src/Match/KritikAn.cs` (`KritikAnDedektoru`, durumsuz sunum aracı;
`MatchState`e dokunmaz), eşik `canliOlasilik.kritikAnEsigi` [KALİBRE].

**Kapı `S2KritikAnRitmi`** üç şeyi birden ölçüyor: ritim 8-12 bandında · hiçbir maç boş değil ·
kadans bağımsızlığı korunuyor. **Diş:** eşik 0,15 → 3,2 an/maç (bant dışı); eşik 0,015 → 24,9
(bant dışı) **VE kadans bağımsızlığı da bozuluyor** (3 sn 24,9 vs 30 sn 22,1). Yani kapının iki
iddiası birbirinin yedeği değil — eşiği iki taraftan sıkıştırıyorlar.

**TASK-002 bu karara göre güncellendi;** brif "mekanizma hazır, yeniden yazma" diyor ve ME 15.3
eşiğini kullanmayı açıkça yasaklıyor.

### K2 KARARI (2026-09-05, Atilla): **UI Toolkit**

`com.unity.modules.uielements` (1.0.0) zaten Unity manifest'inde — **yeni bağımlılık değil,
ADR gerekmiyor** (CLAUDE.md "yeni Unity paketi = ADR" kuralı tetiklenmiyor). Placeholder için
UXML/USS dosyası şart değil; arayüz C#'ta kurulabilir.

**FAZ 00.5'in bir satırını ÖNE ÇEKİYOR:** o kayıt "kodla üretilen uGUI (UI Toolkit seti FAZ
02'de)" diyordu. Çelişki yok, çünkü o karar greybox içindi ve greybox emekli; ayrıca greybox'ın
uGUI kodu (`UiShell.cs`, `UiWidgets.cs`) zaten taşınmıyordu. Bu satır, FAZ 02'nin UI Toolkit
setini 5G-a'nın placeholder ihtiyacıyla erken başlatıyor.

### K3 KARARI (2026-09-05, Atilla): **greybox arşiv olarak kalsın**

Silinmez; dosyalar git'te durur. Ama "arşiv" pratikte bir şey daha demek: **derlenmemeli.**
`Game.Greybox` asmdef'i `TheBadge.Sim`e referans veriyor, yani arşivlenmezse çekirdek API'si her
değiştiğinde emekli kod kırılır ve birinin onu düzeltmesi gerekir.

**SIRA ÖNEMLİ — yoksa motor test sahnesi sessizce ölür.** `Assets/Greybox/` bugün hem emekli
greybox'ı hem de hâlâ gereken `EngineDev.unity` + `EngineDevBootstrap.cs`'i barındırıyor:
1. EngineDev kendi klasörüne taşınır (kendi asmdef'i, referans `TheBadge.Sim`). **Taşınacak
   ÜÇ dosya var, iki değil** (inceleme bulgusu, Codex P1): sahne, `EngineDevBootstrap.cs` ve
   **`Scripts/View/SpriteFactory.cs`** — bootstrap onu çağırıyor (`using TheBadge.Greybox.View`).
   Geride kalırsa yeni asmdef çözemez ve sahne derlenmez; yani ilk yazdığım sıra, tam olarak
   önlemeye çalıştığı şeyi yapardı. Zincir orada bitiyor (doğrulandı: `SpriteFactory` yalnız
   `UnityEngine` kullanıyor; bootstrap balance'ı REPO KÖKÜNDEN okuyor, `Greybox/Resources`tan değil).
2. Kalan greybox `Assets/Greybox~/` olur — Unity `~` ile biten klasörü içe aktarmaz.
3. **Kabul edilen bedel:** dört EditMode test dosyası koşmayı bırakır (`FlowSimTests`,
   `ModelMatchTests`, `EconomyAndBusTests`, `SahneSozlesmesiTests`). Hepsi greybox'ın KENDİ
   koduna bakıyor; paylaşılan çekirdeği ölçen tek satır yok. **Bu bir kapı gevşetmesi değil** —
   ölçtüğü şey emekli. (Çekirdeği ölçen 180 kapı `Sim.Checks`te ve dokunulmuyor.)

TASK-002 bu sırayı adım adım yazıyor.

## Bekleyen kararlar

- **İnceleme eğrisinin bir dakikalık gecikmesi (5G S2, 2026-09-05).** CANLI yol `AnlikOlasilik`
  ile kapandı; geriye maç sonu inceleme eğrisi kalıyor: `SampleCurves` tick döngüsünde
  `queue.ApplyDue`dan önce koştuğu için 37. dakikadaki bir müdahale dizide 38'de görünür.
  Seçenekler: (a) `SampleCurves`i `ApplyDue`dan SONRAYA al — ME 15.3'ün momentum örneklemesini
  de bir adım kaydırır, yani ilgisiz bir sebeple spec'li bir davranışı değiştirmek olur;
  (b) yalnız `wp3*` örneklemesini ayır — iki örnekleme noktası, atlanan dakika mantığı
  ikilenir; (c) olduğu gibi bırak, anlamı belgede kesin — bugünkü hâl budur ve inceleme
  eğrisi için bir dakikalık gecikme zararsız olabilir. **Önerim (c)**, ama karar ekranı yazan
  turda verilmeli: eğriyi kim, ne için okuyacak henüz belli değil.

- **MOTOR DİLİMİ (5G S2-B'den ayrıldı, 2026-09-04, Atilla (a) dedi) — taktiğin üç tuhaflığı.**
  Bir karar değil, bir TETİKLEYİCİ: motor kalibrasyon dilimi açıldığında ele alınır.
  (1) `pres` saf ceza — kendi golünü hiç artırmıyor, rakibinkini +2'de %49 artırıyor; gerçek
  futbolda pres topu yukarıda kazanıp şans üretir. (2) `pres` −1 ile −2 BİREBİR aynı sonucu
  veriyor: kadran negatif tarafta doyuyor, oyuncuya sunulan bir seçenek hiçbir şey yapmıyor
  (`K10TalimatAtilligi`nin kardeşi). (3) Savunmacı yön daha ÇOK gol yediriyor (tam kapanmada
  rakip 1,38, kontrolde 0,95). Düzeltildiğinde `-- fit-winprob` yeniden koşulur ve
  `S2WinProbKalibrasyon` katsayıları doğrular — bağ ucuz, bu yüzden (b) beklemedi.
- **Şeridin ilk okuması taktiği görmüyor (5G S2, 2026-09-04).** `SampleCurves` komut
  uygulamasından önce koştuğu için dakika 0 örneği maç öncesi taktik kurulumunu yok sayar.
  Kapı dakika 1'den ölçerek etrafından dolaşıyor; ekran yazılırken görünür hale gelir.
  Seçenekler o gün netleşir: örnekleme sırasını değiştirmek (motor semantiği) ya da sunumun
  ilk kareyi dakika 1'den okuması (yalnız sunum).

- ~~**S2-B: taktiğin üç tuhaflığı nereye ait?**~~ → **KAPANDI (2026-09-04, Atilla): (a)** —
  ayrı motor dilimi açıldı, (b) beklemeden taktiği modele kattı. Uygulaması yukarıdaki
  "S2-B KARARI" ve "S2-A/(b) UYGULANDI" kayıtlarında; motor dilimi bu listenin başındaki
  MOTOR DİLİMİ satırında tetikleyicisiyle duruyor. Aşağıdaki seçenek menüsü, kararın
  alındığı andaki bilgi durumu olarak kalıyor:
  Şeride taktiği sokmak (b)'nin işi ve ölçüm bunun MÜMKÜN olduğunu gösterdi. Ama ölçüm ayrıca
  motor tarafında üç şey buldu: `pres` saf ceza, `pres` negatif tarafta doyuyor, savunmacı yön
  daha çok gol yediriyor.
  - **(a) Ayrı motor dilimi aç, (b) onu beklemeden taktiği modele katsın** ← **önerilen**.
    (b)'nin işi taktiği ŞERİDE göstermek; taktiğin kendisinin doğru kalibre olup olmadığı ayrı
    bir soru. Eksi: `pres` düzelirse katsayılar yeniden oturtulur (ucuz — `-- fit-winprob`).
  - **(b) Önce motoru düzelt, sonra modele kat.** Artı: tek oturtma. Eksi: 5G-a'yı motor
    kalibrasyonuna bağımlı kılar; dikey dilimin sunum işi motoru beklemek zorunda kalır.
  - **(c) Taktiği modele hiç katma, sunum başka eksene otursun.** Artı: tuhaflıklardan bağımsız.
    Eksi: ölçüm taktiğin EN BÜYÜK kol olduğunu gösterdi; onu göstermeyen bir şerit oyuncunun
    kararını görmezden gelir — greybox'ın vaadini ikinci kez kırar.

- ~~**S2-A: sunumun omurgası ne olacak?**~~ → **KAPANDI (2026-09-04, Atilla): (a) + (b),
  sırayla.** İkisi de UYGULANDI ve sevk edildi: `LiveWinProb` (kalibrasyon sapması 0,057;
  ayırt edicilik güç tavanın %97'si, taktik %86'sı) + `AnlikOlasilik` canlı okuma.
  Kapılar: `S2WinProbKalibrasyon`, `S2AnlikOlasilikCanli`. **Bu satır yeniden açılamaz;**
  aşağıdaki seçenek menüsü kararın alındığı andaki bilgi durumu olarak kalıyor:
  - **(a) Gerçek bir canlı kazanma olasılığı kur:** 3 sonuçlu (G/B/M), güç farkına duyarlı, kırmızı
    kart/momentum/xG girdili, deterministik ve ucuz; N maçla KALİBRE edilir ve kalibrasyon bir kapı
    olur (greybox'ın <0,10 bandının motor karşılığı). Artı: fun hipotezini dürüstçe test edilebilir
    hâle getirir, müdahale şeridi gerçekten oynatır. Eksi: gerçek bir modelleme işi ve
    **tek başına bir DÜZELTME değil, PARİTE** — %40 zaten çalışan bir şeritle ölçülmüştü.
  - **(b) Şeridi omurga olmaktan çıkar:** sunum başka bir eksene otursun (motorun kendi xG'siyle
    blok kartı "gol ihtimali BİZ %18", ya da momentum/baskı ekseni). Artı: (a)'nın maliyeti yok ve
    redesign'ı gerçekten YENİ bir şeye zorlar. Eksi: greybox'la karşılaştırma zemini kaybolur —
    %40'ın neden geldiğini hâlâ bilmiyoruz, ekseni değiştirmek değişkeni değiştirmek olur.
  - **(c) Ertele:** `WinProb`u ME 15.3 işinde bırak, sunum eksenine S2'nin tasarım turunda karar ver.
    Artı: karar veri geldikten sonra. Eksi: S2'nin çıkış kapısı zaten o veri turu; ekseni
    seçmeden tur koşulamaz.
  - **Önerim: (a) + (b) birlikte, ama sırayla** — (a) pariteyi kurar (ölçülebilir, burada
    kanıtlanabilir, kapıyla korunur); redesign'ın YENİ olan kısmı (b)'nin ekseninden gelir ve
    mülakatlı tur ikisini birden test eder. Tek başına (a) eski hipotezi tekrar eder, tek başına
    (b) kıyas zeminini atar.

- ~~**`OzetKart` entity ayrımı.**~~ → **YAPILDI (2026-08-31, K10-A):** seçenek (a) yerine (b) —
  `idx`i kapatmak 20. karttan sonrasını log'dan düşürürdü; ayrımı `SummaryCapacity`ye çekmek aynı
  garantiyi veri kaybetmeden veriyor. `K10OzetAyrimi` kapısı eklendi.
- ~~**xG katsayıları az tahmin ediyor.**~~ → **YAPILDI (2026-08-30, K9-B):** seçenek (b)
  sonra (a) — önce neden arandı. "%5,7" ölçüm gürültüsüydü; gerçek yanlılık +%4,26 ve K8'den
  eskiydi. `shot.xg.b0` -2,48 → -2,43 ile merkeze oturtuldu (+%0,33).
- ~~**`Physics · 700+entity · salt 63` adres paylaşımı.**~~ → **YAPILDI (2026-08-30, K9-A):**
  seçenek (b) — tarayan kapı yazıldı, sonra düzeltildi. Kapı ikinci bir bulgu daha çıkardı
  (balance'ın salt aralığı genişliğini belirlemesi); elle düzeltme ikisini de kaçırırdı.
- ~~**CB 4.2 tablosu ile ME komut kümesi çelişiyor (K10-B).**~~ → **KARAR (2026-08-31, Atilla'nın
  "hepsini kapat" talimatıyla): (b) — CB 4.2'de üçü de "Hub".** GDD 3.2'nin "maç içi bireysel
  talimat" vaadi BUGÜN karşılıksız: talimat iki tarafta da atıl, `PlayerInstr` kataloğu boş, şema
  markaj hedefini taşıyamıyor. Olmayan bir vaadi spec'te taşımak boşluğu kalıcı borç gibi gösterir.
  Bu kayıt BAĞLAYICIDIR (ME 13.4 precedent'i); spec dosyasına dokunulmadı, GDD/CB sonraki
  revizyonda işlenir.
  **KOD HİZALAMASI DENENDİ ve ÖLÇÜMLE REDDEDİLDİ (aynı gün):** kataloğu `Hub | Match` → `Hub`
  yapmayı denedim; `K4MeArayuzBoslugu` kırmızıya döndü ve haklıydı. Bugünkü tasarım bu üç aksiyonu
  maç bağlamında BİLEREK kabul edip bir KURALLA açık sebeple reddediyor ("ME karşılığı yok").
  Bağlamı kataloğdan çıkarmak, o açıklayıcı reddi jenerik bir "yanlış bağlam" hatasına düşürüyordu
  — oyuncuya daha az bilgi veren bir uygulama. Yani (b)'nin doğru uygulaması, kayıtta zaten yazdığı
  gibi, **"motor işi yok"tur**: karar spec metnini bağlar, kod açıklayıcı reddi korur. Bu satır
  kalsın diye yazıldı: bir kararı koda çevirirken kararın kendi "iş yok" notunu okumadım.
  Gerçek talimat sistemi istendiğinde (a) ayrı bir GDD v4.2 kalemi olarak açılır;
  `K10TalimatAtilligi` o güne kadar atıllığı görünür tutar.
- ~~**ECONOMY_MAP source/sink bandı sermaye harcamasını (inşaat) kapsasın mı? (K3 inceleme turu,
  2026-08-29)**~~ → **YAPILDI (2026-08-31, K10-D):** seçenek (a) — bant işletme dengesi olarak
  kaldı, capex `K10CapexSozlesmesi` ile ayrı ölçülüyor. Not: önerinin GEREKÇESİ yanlıştı ("capex
  bandı bulanıklaştırır"); ölçüm capex'in bandı AYAKTA TUTAN sink olduğunu gösterdi. Kararın
  kendisi doğru çıktı, gerekçesi düzeltildi — yukarıdaki K10-D kaydı.
- ~~**ECONOMY_MAP'in iki kuralı çakışıyor (K10-D).**~~ → **BULGU YANLIŞMIŞ — KAPANDI
  (2026-09-01, K12-D).** 2026-08-31'de (c) ile kapatılmıştı; şimdi korunacak bir çakışma
  OLMADIĞI ölçüldü: bandın alt ucunda merdiven 19, üst ucunda 9 sezonda bitiyor, ikisi de bant
  içi, iflas yok. Eski gözlem bandın DIŞINDAKİ (1,041) bir ölçümden ekstrapole edilmişti.
  `K12DBantUclari` artık uçları ikili aramayla bulup ölçüyor — yukarıdaki K12-D kaydı.
- ~~**Merdiven tükendikten sonra uzun vade sink'i ne? (K10-D BULGU 4)**~~ → **KARAR (2026-08-31,
  "hepsini kapat"): (a) — referans koşuya TRANSFER hattı eklenir, capex kapısı yeniden kalibre
  edilir.** Dokümanın zaten saydığı sink'i modellemek, yeni mekanik icat etmekten ucuz ve doğru;
  (c) (tesis tavanını açmak) Top Eleven anti-pattern'ine yakın olduğu için elendi, (b) (maaş
  enflasyonu) kadro gücü sistemine bağlı olduğu için sonraki dilime bırakıldı. Uygulama: K11-E.
- ~~**Oyuncu piyasası modeli — merdiven sonrası sink borcunun ÖN KOŞULU (K11-E).**~~ →
  **YAPILDI (2026-09-01, K12-C):** seçenek (a)'nın asgari hâli — havuz yenilenmesi + pazarlık +
  kadro dönüşü kuruldu. Borç 2,25 → 1,911'e indi ama KAPANMADI.
- ~~**Sınırsız gelir büyümesi: merdiven sonrası borcun ASIL kaynağı (K12-C ölçümü, 2026-09-01).**~~
  → **YAPILDI (2026-09-02, K13-A):** Atilla (a)+(c) dedi, ikisi de kuruldu; oran 1,99 → 1,107,
  borç tavanı kaldırılıp yerine ECONOMY_MAP bandı iki taraflı kondu. Yukarıdaki K13-A kaydı.
  Aşağıdaki eski metin, kararın alındığı andaki bilgi durumu olarak duruyor.
  Piyasa kuruldu ve çalışıyor, ama havuzun kalite tavanı yüzünden sink doyuyor: kadro havuzun
  tepesine ulaşınca kasa şişiyor (32 sezonda 3,3 milyar ₺). Sorun sink tarafında değil KAYNAK
  tarafında: stadyum kapasitesi üçe katlanıp kalıcı yüksek gelir üretiyor ve hiçbir gider onunla
  ölçeklenmiyor. Seçenekler: (a) maaş enflasyonu — kulüp büyüdükçe oyuncu ücret talebi büyüsün
  (piyasa modeli hazır, `Valuation.MaasTalebi`'ye kulüp ölçeği girer); (b) havuz kalitesi kulüple
  birlikte büyüsün (üst lig oyuncuları görünür olsun) — sink doymaz ama "her sezon daha iyisi
  var" hissi Top Eleven'a yaklaşır, dikkat; (c) gelir tarafı doygunlaşsın (kapasite ötesi seyirci
  getirisi azalan verimli olsun). **Öneri: (a) + (c)** — ikisi de ECONOMY_MAP'in kendi mantığı
  içinde kalır ve rekabeti bozmaz. KARAR ATİLLA'NIN.
- ~~**Motorun faul/kart kalibrasyonu rol ayrımı olan kadrolarla yeniden yapılsın mı?**~~ →
  **YAPILDI (2026-09-01, K12-A):** seçenek (a). Kadro profili gerçekçiliğe geri döndü, köprü
  kadrosu motorun kendi bantlarında. Kalan tek kaçak (düz dağılımda kırmızı) bant gevşetilmeden
  ayrı bir borç kapısına taşındı — yukarıdaki K12-A kaydı.
- **Doğrudan kırmızı olay sınıfı (K13-C (a)) — KARARLA DEĞİL, FAZ 05 HAKEM DİLİMİYLE açılır.**
  2026-09-02'de Atilla (c) dedi: bugün kapatma, teşhis kapısıyla bekle. Bu satır bir karar değil
  bir TETİKLEYİCİdir ve kapatan şey nettir: **FAZ 05 hakem modeli dilimi açıldığında** şiddet
  skoruna ayrı bir "doğrudan kırmızı" olay sınıfı (DOGSO / ciddi faul / şiddetli hareket) eklenir;
  eşik oynatmak çözüm DEĞİLDİR (ölçüldü — K13-C kaydı). O güne kadar borç korumasız değil:
  `K13CDogrudanKirmiziOlu` yol canlandığında düşer, boşluk kötüleşirse kırmızıya döner;
  `M16EKirmiziBorcu` da semptomu (LİG kırmızı oranı) ölçmeye devam eder.
- **Kırmızı kart borcu — K13-C'de ÇERÇEVESİ DÜZELTİLDİ.** Bant sorgulandı ve TEMİZ çıktı; borç
  "düz dağılımda kırmızı" değil **"doğrudan kırmızı yolu erişilemez"** (üç popülasyonda da sıfır,
  en yüksek şiddet 0,754 vs eşik 0,80). Yeni kapı `K13CDogrudanKirmiziOlu` teşhisi iki taraflı
  koruyor. Bundan sonrası için üç seçenek ve önerim K13-C kaydının sonunda. Eski satırlar,
  kararların alındığı andaki bilgi durumu olarak duruyor:
- **[ESKİ] Düz dağılımda kırmızı 0,03 (hedef 0,10-0,36) — BORÇ (K12-A), ÖNERİ (a) K13-B'DE ÇÜRÜDÜ.**
  Jokey kolu uygulandı ve ölçüldü: iki popülasyonu ayırmıyor, çünkü köprünün kart fazlası
  müdahale sayısından değil ŞİDDETİNDEN geliyor (yukarıdaki K13-B kaydı). Geri alındı; borç
  bugünkü kapısıyla korunuyor. Yeni seçenekler ve önerim K13-B kaydının sonunda.
  Eski metin, kararın alındığı andaki bilgi durumu olarak duruyor:
- **[ESKİ] Düz dağılımda kırmızı 0,03 (hedef 0,10-0,36) — BORÇ (K12-A).** İki popülasyon aynı anda hem
  kart hem kırmızı bandını tutturamıyor. Seçenekler: (a) müdahale KARARINI role duyarlı yap —
  kötü müdahaleci dalmak yerine jokey yapsın (model işi, ME 7.6 pres tetiği); (b) ikinci sarı
  mekanizmasını şiddetten ayır; (c) borç açık kalsın, kapı korusun. **Öneri: (a)** — kaynak
  orada (kaybedilen her müdahale foul adayı üretiyor), ama ayrı bir dilim.
- ~~**Köprü kadrosuyla şut/maç 33,5 (hedef ≤32) — BORÇ.**~~ → **KAPANDI (2026-09-01, K12-A):**
  motor kalibrasyonu sonrası 30,2; tavan kaldırıldı, metrik normal banda döndü.
- ~~**Maç öncesi `Kondisyon` ve `Moral` motora taşınsın mı? (K11 köprü kararı)**~~ →
  **YAPILDI (2026-09-01, K12-B):** seçenek (a). ME 12.1'e başlangıç enerjisi, ME 12.3'e başlangıç
  momentumu eklendi; rotasyon artık oynanışa değiyor. Yukarıdaki K12-B kaydı.
- ~~**`Rng.Gauss01` çarpışması ne zaman düzeltilsin?**~~ → **KARAR (2026-08-30, Atilla): ŞİMDİ
  YAP.** Üç seçenek ölçülmüş maliyetle sunuldu (şimdi / FAZ 05 öncesi / hiç). Uygulandı, aşağıdaki
  K8 kaydına bakınız. Bekleyen karar kapandı.
- ~~**`M4CalibrationBands` örneklemi 12 maç — gürültüden kırmızıya dönebiliyor.**~~ → **YAPILDI
  (2026-08-30): N=12 → 200.** Seçenek (a) uygulandı; bant DEĞİŞTİRİLMEDİ, yalnız ölçümün gürültüsü
  küçültüldü. Kapı süresi kabul edilebilir kaldı (tüm takım 3 dk 14 sn).
- ~~ME 13.4 upset büyüklüğü~~ → **KARAR (2026-08-19, Atilla): (d) HİBRİT.** Dört seçenek
  sunuldu — (a) tam zincir normalizasyonu (2 dilim motor işi), (b) yalnız hedef revizyonu
  (%93 revize hedefin de üstünde kalır), (c) skor üstü yeniden örnekleme (tek-kaynak ilkesi +
  xG tutarlılığıyla çatışır — reddedildi), (d) hibrit. Seçilen: **spec hedef tablosunun
  gerçekçi banda revizyonu + derin blok mekanizması tek dilimde.** Gerekçe: Elo'da 200 puanlık
  fark ≈ %76 beklenen skor; büyük liglerde büyük favori galibiyeti ~%75-80 — 13.4'ün %66'sı
  gerçekçilik değil tasarım tercihiydi ve 5 ölçüm motorun oraya tek katsayıyla inmediğini
  kanıtladı. **Revize hedef tablo (75v55): Düşük ~%85/%8/%7 · Orta ~%78/%12/%10 ·
  Yüksek ~%68/%16/%16.** Spec dosyasına dokunulmaz (yasak); bu kayıt bağlayıcıdır, kapı
  metinleri bu tabloyu basar, GDD/ME v-sonraki revizyonda spec'e işlenir. Uygulama dilimi:
  M16-F (derin blok — aşağıda).
- ~~**Highlight eşiği / zaman çizelgesi işareti.**~~ → **YAPILDI (2026-08-31, K10-C):** seçenek (b).
  Eşik korundu (`HighlightCount` hâlâ ME 15.3 tanımı), çizelge `zamanCizelgesiIsaret` [KALİBRE]
  kadar en yüksek andan besleniyor. Ölçüm: eşikle 41/60 maçta sıfır işaret.
- ~~**Sunum ayarları `config_hash` içinde mi kalsın? (K10-C gözlemi)**~~ → **KARAR (2026-08-31,
  "hepsini kapat"): (a) — kabul.** Tek sunum ayarı için ikinci bir okuma kaynağı pahalı; "tek
  balance" hikayesi sade kalır. Golden churn'ün DAVRANIŞ değişikliği olmadığı zaten ölçüldü
  (50/50 `stateHash` aynı). Yeniden değerlendirme eşiği: sunum ayarı sayısı 5'i geçerse (b).
- ~~**Premium etkilerin public ligde şeffaf rozeti (panel M-bulgusu).**~~ → **KARAR (2026-08-31,
  "hepsini kapat"): ROZET GÖSTERİLİR.** Gerekçe GAME_THESIS'in kendi ilkesi: public ligde rekabetin
  adil OKUNMASI, premium'un gizlenmesinden değerlidir; gizli çarpan, kaybeden oyuncuya "para mı
  kazandı" şüphesi bırakır ve bu şüphe rozetin maliyetinden pahalıdır (Top Eleven anti-pattern'i).
  Kapsam: yalnız AKTİF ve oynanışa etki eden premium çarpanlar rozetlenir (kozmetik rozetlenmez —
  kozmetik zaten ayrı şerittir). Uygulama FAZ 02 UI dilimine ait; bu kayıt tasarımı bağlar.

- ~~3G Greybox Fun Gate GO/NO-GO~~ → **KAPANDI (2026-08-08): NO-GO %40** — uygulama yukarıdaki kapanış bölümünde; sunum revizyonu + mülakatlı doğrulama turu Dikey Dilim öncesi BORÇ.
- ~~**BRIEF_3G_GREYBOX RA#1 metninin pivot sonrası revizyonu.**~~ → **KARAR (2026-08-31,
  "hepsini kapat"): REVİZE EDİLMEZ, ARŞİV OLARAK KALIR.** FAZ 00.5 kapandı (NO-GO %40); brif o
  fazın girdisiydi ve revize edilmiş hâli hiçbir işi beslemeyecek. Pivot kararı zaten DECISIONS'ta
  (2026-08-02, "Model Maçı" tezi) ve GREYBOX_DURUM arşiv başlığında kayıtlı — bilgi kaybı yok.
  Yeni brif gerektiğinde FAZ 03/04'ün kendi brifi yazılır, eskisi yamanmaz.

- ~~Greybox iterasyon 11 kapsamı~~ → **KARAR (2026-08-07, Atilla): Paket A TAM** — yorgunluk + kart/sakatlık zorunlu karar anları + isimli kadro/değişiklik + koç masası greybox'a girdi (İt.11, GREYBOX_MODEL.md v2). Bu SON içerik iterasyonudur; sırada his onayı + playtest.
- ~~Greybox iterasyon 12 kapsamı~~ → **KARAR (2026-08-07, Atilla): S1 — Kadro Kimliği** uygulandı: bireysel oyuncu gücü + mevki ağırlıklı Hücum/Savunma kanalları (kaleci savunmada en ağır) + şerit görünürlük satırı (İt.12, GREYBOX_MODEL.md v3). Timebox: playtest 2 haftalık kutunun kenarında — kayarsa bilinçli uzatma bu satıra işlenir.
- ~~**GDD v4.2 adayları (Atilla fikri, 2026-08-07).**~~ → **KARAR (2026-08-31, "hepsini kapat"):
  (a) ve (c) GDD v4.2'ye GİRER, (b) Oto-Koç GİRMEZ — ERTELENİR.**
  - **(a) Koşullu ön-emirler → GİRER.** GAME_THESIS Session Shape'in kendi taahhüdünü besliyor:
    "canlıyı kaçırmak ceza değildir". Ücretsiz katmanın offline adaleti bugün yalnız Tier 0
    kuyruğuyla (CB 8.3) sağlanıyor ve o kuyruk KOŞULSUZ — "0-1 gerideysek hücuma geç" diyemiyorsun.
  - **(c) Online/offline asimetri ilkesinin net yazımı → GİRER.** Maliyeti doküman, faydası her
    sonraki kapsam tartışmasında ölçüt olması. Bugün bu ilke sözlü.
  - **(b) Oto-Koç → GİRMEZ (bugün).** Gerekçe TASARIM TERCİHİ DEĞİL, projenin kendi kapsam
    filtresi: **GAME_THESIS Non-goals (v1) listesinde "AUTO komut kaynağı" AÇIKÇA yazıyor**, ve
    `CommandSource.Auto = 2` kataloğa bu yüzden "tanımlı ama kullanılmaz" olarak duruyor. Oto-Koç
    tam olarak bir AUTO komut kaynağıdır; kabul etmek v1 non-goal'unu kaldırmak demektir — bu,
    tek satırlık bir GDD kalemi değil, tez seviyesinde bir karardır ve "adaylar" listesinden
    sessizce geçmemelidir. Ayrıca fikrin kendi içindeki "canlı insan > oto-koç optimal-altı bant"
    şartı, P2PF adalet algısını (Riskiest Assumption 3) doğrudan gerginleştirir.
    **Yeniden açma koşulu:** ücretsiz katmanın offline adaleti (a) ile ölçüldükten SONRA hâlâ
    yetersizse, Oto-Koç non-goal revizyonu olarak ayrı gündemle açılır.
- **LOD 1'in geleceği (M15, 2026-08-16) — KAPATILAMAZ, ölçüm bekliyor.** Şu an LOD 0'ın
  eşleniği; ayrıştırma gerekçesi CPU OLAMAZ (19 kat marj var). Bu satır bir karar değil bir
  TETİKLEYİCİdir; kararla kapatmak, olmayan bir ölçümü varmış gibi göstermek olurdu.
  Kapatan şey nettir: FAZ 05 cihaz testinde ORTA seviye cihazda LOD 0'ın bir maçı > 800 ms
  sürerse LOD 1 ayrışır, sürmezse satır silinir. O güne kadar `M15Lod1Esdeger` eşdeğerliği,
  ME 16.4 sezon turu da CPU marjını HER koşuda ölçüyor — yani satır bekliyor ama korumasız değil.

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
