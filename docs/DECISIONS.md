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
- **İnceleme turu 1 sonrası 10k (duran top senkronundan ÖNCEKİ konfigürasyon) — 13/13 ✓:** gol 2,41 ·
  şut 26,6 · isabetli 7,5 · korner 8,3 · faul 20,7 · sarı 3,18 · kırmızı 0,20 · penaltı 0,29 ·
  ofsayt 4,9 · sakatlık 0,51 · pas %81,2 · gol-xG sapması %0,0 · possession %59,2. 75v55
  (1.380 fark maçı): G/B/M **%84/%12/%4** — revize hedef %78/%12/%10; beraberlik tam hedefte,
  kalan 6 puan isabet dilimi + Yüksek chaos borcunda.

## Bekleyen kararlar
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
- **LOD 1'in geleceği (M15 kararı, 2026-08-16):** şu an LOD 0'ın eşleniği. Geri almak için gerekçe
  CPU olamaz (19 kat marj var); yalnız İSTEMCİ tarafında orta cihaz ölçümü LOD 0'ı 800 ms'nin
  üstüne çıkarırsa yeniden değerlendirilir. O ölçüm FAZ 05 cihaz testlerine ait.
- **Highlight eşiği / zaman çizelgesi işareti (M14 bulgusu, 2026-08-14):** ME 15.3'ün H > 0,50 eşiği
  ölçümde 0,5-0,8 işaret/maç veriyor. Seçenekler: (a) eşiği spec'te 0,35-0,40'a çekmek → ~3-5
  işaret/maç, formül aynı kalır; (b) eşiği korumak ve zaman çizelgesini "en yüksek 6 an"la beslemek →
  spec'e dokunulmaz, işaret sayısı sabit 6 olur; (c) `xG_salınımı` terimini 3 sonuçlu (galibiyet/
  beraberlik/mağlubiyet) WinProb'a taşımak → gol sıçramaları büyür, eşik anlamlı kalır, en fazla iş.
  Öneri: **(b)** — sunum kararı, motor mantığına dokunmaz (17.5 "ayar sahası" ilkesiyle uyumlu).
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
