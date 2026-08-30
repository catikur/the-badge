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

### 🔴 BULGU: `Rng.Gauss01` komşu tick'lerde ve bit-0 farklı seed'lerde AYNI değeri üretiyor
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

**Kendi kapımın ölçüm YERİ yanlıştı.** `K6TransferSurucusu`'nun "seed sürücüyü oynatıyor mu"
iddiası ilk yazımda sabit 1,5M teklifle ölçülüyordu; o teklif oyuncunun değerinin çok dışındaydı ve
karar seed'den BAĞIMSIZ olarak hep aynıydı — kapı kırmızı yandı. İddia yanlış değildi, **ölçüm
yeri** yanlıştı: pazarlık salınımı ancak PAZARLIK BANDINDA sonucu değiştirir. Teklif artık değerin
%75'i olarak hesaplanıyor. (K5'in "kapı en kritik yolu ölçmeli" dersinin kardeşi: doğru şeyi
yanlış noktada ölçen kapı da yanıltır.)

`K2HashKapsami` dördüncü dilim üst üste yeni alanı yakaladı (`GameState.Lig`).

## Bekleyen kararlar
- **CB 4.2 tablosu ile ME komut kümesi çelişiyor (K4 bulgusu, 2026-08-29):** spec `squad.set_player_anchor`/
  `set_player_role`/`set_instruction`'ı "Hub + Maç" sayıyor; motorda anchor/rol maç komutu yok,
  `PlayerInstr` kataloğu boş. Seçenekler: (a) ME komut kümesini üç komutla genişlet (determinizm kapısı +
  50 golden replay etkisi, 1 dilim); (b) CB 4.2'de bu üçünü "Hub" olarak revize et (spec revizyonu,
  motor işi yok, GDD 3.2 "maç içinde bireysel talimat" vaadini daraltır); (c) yalnız `set_instruction`'ı
  maça taşı, anchor/rol hub'da kalsın (orta yol: maç içi mikro-yönetim `PlayerInstr` ile gelir, serbest
  pozisyonlama maç arası kararı olur). Öneri: **(c)** — GDD 3.1 serbest pozisyonlama zaten formasyon
  kararı, GDD 3.2 talimatı ise maç içi tepki. Spec dosyasına dokunulmadı; şimdilik maç bağlamında
  açık sebeple reddediliyor ve `K4MeArayuzBoslugu` borcu görünür tutuyor.
- **ECONOMY_MAP source/sink bandı sermaye harcamasını (inşaat) kapsasın mı? (K3 inceleme turu,
  2026-08-29)** Ledger artık inşaatı sink sayıyor, ama referans kalibrasyon senaryosu inşaatsız:
  1,05-1,15 bandı SÜREKLİ işletme dengesini ölçüyor. Seçenekler: (a) bant işletme dengesi olarak
  kalsın, capex ayrı bir kapıyla ölçülsün (ör. "sezon başına capex ≤ gelirin %X'i"); (b) bant
  capex dahil yeniden tanımlansın ve yeniden kalibre edilsin. **Öneri: (a)** — yığınsal harcamayı
  sürekli dengeye karıştırmak bandı bulanıklaştırır ve kulübün yatırım yapmasını cezalandırır gibi
  okunur. Karar balance sprintine ait, K4-K5'i bloklamaz.
- **`Rng.Gauss01` çarpışması ne zaman düzeltilsin? (K3 bulgusu, 2026-08-25)** Ölçüm ve mekanizma
  yukarıdaki bulgu kaydında. Seçenekler: (a) ŞİMDİ düzelt → golden set yeniden üretilir, M16-E'nin
  12 metriği yeniden ölçülür ve muhtemelen yeniden kalibre edilir (1-2 dilim); (b) FAZ 04 sonunda,
  K7 bittikten sonra tek seferde; (c) FAZ 05 öncesi, cihaz testlerinden önce. **Öneri: (b)** —
  gürültü kalitesi bugün hiçbir kapıyı düşürmüyor, ama FAZ 05'e taşınırsa kalibrasyon borcu asset
  üretimiyle aynı sprinte biner. Gözcü kapı bu arada borcu görünür tutuyor.
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
