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
