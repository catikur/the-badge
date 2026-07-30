<!-- THE BADGE GDD v4.1 — bu dosya bağlayıcıdır; değişiklik önerileri docs/DECISIONS.md üzerinden -->

# 1. OYUN VİZYONU VE TEMEL KİMLİK

## 1.1. Projenin Tanımı

Bu proje, 1998 yapımı efsanevi Ultimate Soccer Manager 98 (USM 98) oyununun modern bir yeniden yapımıdır (Remake). Amaç, USM 98'in sunduğu derinlemesine "Tycoon" (İşletme Simülasyonu) ve "Serbest Taktiksel Diziliş" özelliklerini koruyarak; bu yapıyı modern Unity 6 motoru, Yapay Zeka (LLM) diyalogları ve OSM (Online Soccer Manager) tarzı sosyal lig sistemiyle birleştirmektir.

**v4.0 notu:** Bu sürüm; içerik/lisans stratejisini (kurgusal evren + topluluk editörü), sezonluk anlatı katmanını (Hikaye Motoru + Asistan Hafızası), viral büyüme altyapısını (Deterministik Replay Paylaşımı + Haftanın Panoraması), kozmetik ekonomiyi, FTUE tasarımını ve LLM güvenlik katmanını dokümana tam entegre eder.

## 1.2. Temel Oynanış Döngüsü (Core Loop)

Oyun, oyuncunun hem saha içini (Menajerlik) hem de saha dışını (Başkanlık/İşletme) yönettiği ikili bir yapıya sahiptir:

- **Hafta İçi (The Hub):** Antrenman planlama, stadyum inşa etme, büfe fiyatlarını ayarlama, medya ve oyuncularla LLM üzerinden konuşma, aktif hikaye arklarını yönetme.
- **Maç Günü (Match Day):** 2D maç motorunda taktiksel müdahalelerle maçı yönetme.
- **Sonuç (Outcome):** Maç sonu röportajı, lig puan durumu güncellemesi, kazanılan parayla kulübü geliştirme ve öne çıkan anların replay klibi olarak paylaşılması.
- **Hafta Kapanışı (Weekly Wrap):** Haftanın Panoraması ile lig gündeminin izlenmesi, hikaye arklarının ilerlemesi.

## 1.3. Stratejik Konumlandırma ve Farklılaştırıcılar

- **Serbest Pozisyonlama Sistemi:** Oyuncular futbolcuları sahanın herhangi bir pikseline yerleştirebilir — katı pozisyon kutucukları yoktur.
- **LLM-Tabanlı Doğal Dil İletişimi:** Oyuncular asistan, medya ve futbolcularla doğal metin ile iletişim kurar.
- **Sezon Hikaye Motoru [YENİ v4.0]:** LLM tek maçlık röportajın ötesinde, haftalara yayılan rekabet ve dram arkları üretir; personalar sezon geçmişini hatırlar.
- **Tycoon + Menajerlik Hibrit Modeli:** Stadyum inşaatı, büfe yönetimi, sponsor anlaşmaları ile saha içi başarı birleşiyor.
- **Server-Side Maç Simülasyonu:** Online liglerde hile yapmak matematiksel olarak imkansız.
- **Deterministik Replay Paylaşımı [YENİ v4.0]:** Seed + girdi kaydıyla her maç yeniden oynatılabilir; gol klipleri organik büyüme motorudur.
- **Ayarlanabilir Chaos Engine:** Oyuncular/lig kurucuları şans faktörünü seçebilir (Düşük/Orta/Yüksek).
- **Kurgusal Evren + Topluluk Editörü [YENİ v4.0]:** Lisans riski sıfırlanmış özgün futbol evreni; topluluk kendi isim paketlerini yerel olarak uygulayabilir.
- **AI-First Geliştirme:** Oyun yüzde 70-80 maliyet tasarrufu ile inşa ediliyor (bölüm 13).

## 1.4. Hedef Kitle

- **Birincil:** 25-45 yaş arası futbol management oyunu severler (FM, Championship Manager, USM nostaljisi).
- **İkincil:** 16-24 yaş arası mobil/casual yönetim oyunu oyuncuları (OSM, Top Eleven tarzı).
- **Coğrafi:** Türkiye + Avrupa + Güney Amerika + Orta Doğu ağırlıklı global pazar.
- **Psikografik:** Derinlik ve strateji isteyenler; skill-based ilerleme ve uzun vadeli kariyer tercih edenler.
- **Platformlar [KARAR v4.1]:** iOS + Android — salt mobil. PC sürümü kapsam dışıdır (Karar Günlüğü v4.1).

## 1.5. Lansman Sıralaması (Tam Vizyon, Kademeli Devreye Alma)

Tam özellik seti hedefi korunur; hiçbir sistem kapsam dışına alınmaz. Aşağıdaki sıralama yalnızca store lansmanını bloke etmeden hangi sistemlerin lansman sonrası ilk haftalarda devreye alınabileceğini tanımlar. Tüm sistemler feature-flag arkasında geliştirilir ve 17. bölümdeki roadmap'e dahildir.

| Sistem | Devreye Alma | Not |
| --- | --- | --- |
| Match Engine, Kadro, Tycoon, Online, LLM (güvenlik katmanlı), FTUE, Çekirdek Monetizasyon, Replay kaydı + klip paylaşımı, Kozmetik temel set | Lansman günü | Store lansmanının çekirdeği |
| Haftanın Panoraması | Lansman + 4 hafta | İçerik hattı stabilize olduktan sonra |
| Hikaye Motoru (tam ark seti) | Lansman + 6 hafta | Lansmanda Lite mod (2 ark tipi) aktif |
| Sezon Pası — Sezon 1 | Lansman + 2 hafta | Altyapı lansmanda hazır, S1 kısa gecikmeyle başlar |
| Editör içe/dışa aktarma arayüzü | Lansman + 2 hafta | Kanonik ID mimarisi lansmanda hazır |

# 2. MODÜL 1 — THE HUB (MENAJER OFİSİ VE ARAYÜZ)

Bu modül, oyunun ana menüsüdür. Sıkıcı listeler yerine, oyuncuyu oyunun dünyasına çeken görsel bir ortam sunar.

## 2.1. Görsel Atmosfer ve Progression (İlerleme)

**Sahne Tasarımı:** Ofis statik bir 2D resim değildir. Unity içinde katmanlı 2.5D prerender sahnelerle (Midjourney/Scenario üretimi) kurulur; derinlik hissi katman parallax'ı ile verilir [KARAR v4.1]. Mobilde/iPad'de cihazı hafifçe sağa-sola eğince ofis kamerası da o yöne hafifçe kayar (Parallax Effect).

**İlerleme (Dinamik Ofis):** Kulüp başarılı oldukça ofis de gelişir. Başlangıç: Amatör küme ofisi (eski masa, tek ampul). Şampiyonluk: Cam kule, modern mobilya, kupalar rafı. Şampiyonlar Ligi: Özel manzara, lüks detaylar.

**Kozmetik Bağlantısı [YENİ v4.0]:** Ofis dekorasyon nesneleri (tablo, maket, kupa vitrini, aydınlatma temaları) kozmetik ekonominin (bölüm 12.9) parçasıdır. Tier ilerlemesi ana görünümü, kozmetikler kişiselleştirmeyi belirler.

## 2.2. Etkileşimli Nesneler

- **Taktik Tahtası:** Takım/Taktik ekranına kısayol.
- **Televizyon:** Maç özetleri, analizler ve Haftanın Panoraması yayını (bölüm 8.4).
- **Telefon / Akıllı Telefon:** Transfer görüşmeleri, personel alımı.
- **Dosya Dolabı:** Oyuncu veritabanı, scouting raporları.
- **Pencere:** Stadyum İnşaat Moduna geçiş.
- **Gazete / Dergi:** Haftalık özetler, LLM manşetleri ve aktif hikaye arklarının basın yansımaları.

## 2.3. Asistan ve LLM Entegrasyonu (Yapay Zeka)

Oyunun iletişim katmanı, ChatGPT/Claude benzeri bir LLM üzerine kuruludur.

**Hibrit İletişim Yapısı:**

- **Mod A (Hızlı/Klasik):** Oyuncu zaman kaybetmek istemiyorsa, LLM'in önerdiği hazır butonları kullanır.
- **Mod B (Derin/LLM):** Oyuncu "Konuş" butonuna basarak serbest metin girebilir. Tüm Mod B trafiği 11.7'deki güvenlik katmanından geçer.
- **Fonksiyon Çağırma (Function Calling):** LLM, oyuncunun yazdığı metni analiz eder ve whitelist'li aksiyon önerilerine çevirir; son kararı her zaman sunucu doğrulayıcısı verir.

> **🎨 AI-FIRST ASSET ÜRETİM STRATEJİSİ — THE HUB**
> Midjourney/Scenario: İzometrik ofis sahneleri, mobilya, dekor eşyaları (5 tier × 15 öğe = ~75 asset)
> Unity AI (6.2+): Texture, material, decorative pattern üretimi
> ElevenLabs: Asistan karakter sesleri (Türkçe/İngilizce), telefon zil sesleri
> Suno AI: Ofis ambient müziği, haftalık menü müzikleri (5 parça)
> Figma: Tüm menü akışları, modal pencereler, bildirim tasarımları (design system)
> Rive: Parallax efektleri, nesne hover animasyonları, bildirim micro-interactions

# 3. MODÜL 2 — KADRO YÖNETİMİ VE TAKTİK

USM 98'in en belirgin özelliği olan "Slotlara bağlı olmayan taktik" anlayışı burada modernize edilmiştir.

## 3.1. Serbest Pozisyonlama (Free-Form Positioning)

- **Saha Yapısı:** Taktik ekranı, maç motorundaki sahanın birebir aynısıdır.
- **Mekanik:** Oyuncu, futbolcu ikonunu sahanın herhangi bir pikseline (X, Y koordinatı) bırakabilir. Katı pozisyon kutucukları yoktur.
- **AI Geometrik Analiz:** Sistem oyuncuların konumuna göre diziliş şeklini otomatik tanımlar (4-4-2, 3-5-2, asimetrik bomba vs.).
- **Yardımcı Görselleştirme:** Oyuncular arası mesafe çizgileri, dengesizlik uyarıları.

## 3.2. Takım Oyuncu Talimatları

- **Bireysel Roller:** Her oyuncu için ayrı rol ataması (Ofansif WB, Libero, Regista, False 9 vs.).
- **Hareket Zonları:** Oyuncuya saha üstünde hareket alanı çizilebilir.
- **Eşleştirme (Marking):** Rakip oyuncu üzerine birebir adam markajı atanabilir.

## 3.3. Taktik Şablonları

- **Ön-Tanımlı:** Klasik 4-4-2, 4-3-3, 3-5-2 gibi şablonlar başlangıç için mevcut.
- **Özel Kaydetme:** Oyuncu kendi taktiklerini kaydedebilir ve diğer oyuncularla paylaşabilir (online).
- **Taktik Kütüphanesi (LLM):** Asistana "gegenpress taktiği kur" yazdığında, LLM oyuncuları uygun konumlara dizer.
- **Replay Entegrasyonu [YENİ v4.0]:** Paylaşılan taktikler, o taktikle oynanmış örnek maç replay linkiyle birlikte sunulabilir (bölüm 8.3).

> **🎨 AI-FIRST ASSET ÜRETİM STRATEJİSİ — KADRO & TAKTİK**
> Scenario (Custom Model): 500+ tutarlı stilde kurgusal oyuncu portresi (yaş/ten/saç varyasyonu, gerçek kişi benzerliği yasağı — bölüm 10.2)
> Midjourney: Takım kitleri (özgün tasarımlar), kurgusal kulüp armaları
> Figma + FigmaToUnity: Taktik tahtası UI, oyuncu kartları, heat map overlayları
> Rive: Oyuncu drag-drop animasyonları, formasyon morph geçişleri, stat bar doldurmaları
> Rive: Oyuncu portresi expression katman animasyonları (mutlu, morali bozuk, sakat)

# 4. MODÜL 3 — TYCOON EKONOMİSİ VE TESİSLER

USM 98'in en büyük farkı "saha dışı yönetim" boyutudur. Bu modül tam anlamıyla modernize edilmiş bir işletme simülasyonudur.

## 4.1. Stadyum İnşaat Modu

**Görsel Yapı:** Pencereden bakınca stadyum yakından görünür. İnşaat süreci katmanlı 2.5D sahnelerde aşama aşama canlı izlenir.

- **Tribün Yönetimi:** Güney, Kuzey, Doğu, Batı tribünleri ayrı ayrı genişletilebilir.
- **VIP Localar:** Yüksek gelir sağlar ama pahalıdır.
- **Işıklandırma:** Gece maçları için şart; belirli lig seviyelerinde zorunlu.
- **Soyunma Odaları:** Oyuncu moraline etki eder.
- **Kapasite Progression:** 5.000 → 15.000 → 30.000 → 60.000+ (Tier ilerledikçe).

## 4.2. Gelir Kaynakları

- **Bilet Satışı:** Kapasite × doluluk oranı × bilet fiyatı. Doluluk, takım başarısına ve bilet fiyatına duyarlıdır.
- **Sezonluk Kombine Satışı:** Peşin gelir, ama uzun vadeli taraftar sadakati gerektirir.
- **Büfe ve Yan Ürünler:** Sosisli, patates kızartması, içecekler, forma, atkı. Fiyat-doluluk optimizasyonu.
- **Sponsorluk Anlaşmaları:** Forma sponsoru, stat isim sponsoru, bölge sponsorları (tümü kurgusal markalar — bölüm 10.3).
- **Yayın Gelirleri:** TV hakları lig seviyesine göre dağıtılır.
- **Maç Günü Hizmetleri:** Park, bilet alma kolaylığı gibi mikro-optimizasyonlar.

## 4.3. Antrenman Tesisleri ve Altyapı

- **Antrenman Sahası:** Tier 1 (basit çimlik) → Tier 5 (bilimsel veri toplayan komple kompleks).
- **Medikal Merkez:** Sakatlık iyileşme süresini etkiler.
- **Altyapı/Genç Takım Tesisi:** Genç oyuncu yetiştirme kalitesini belirler.
- **Stadyum Müzesi:** Efsane oyuncu satın alıp sergileyebilirsiniz (kozmetik, taraftar morali).

## 4.4. Finansal Yönetim

- **Bütçe Planlaması:** Yıllık gelir/gider dengesi, transfer bütçesi, maaş bütçesi.
- **Banka Kredisi:** Yatırım için borç alınabilir; faiz ödenir.
- **Başkan Beklentileri:** Her sezon başında başkanla gerçekleşmesi beklenen hedefler (Yönetim Baskısı hikaye arkının besleyicisi — bölüm 7.3).
- **İflas Riski:** Üst üste kayıp yıllar iflas ve işten çıkarılma ile sonuçlanır.

> **🎨 AI-FIRST ASSET ÜRETİM STRATEJİSİ — TYCOON**
> Midjourney: 5 tier stadyum görselleri (5 × 4 açı = 20 asset), tesis renderleri
> Scenario: Kurgusal sponsor logoları, yan ürün görselleri (forma, atkı, büfe ürünleri)
> Unity Sentis: Dinamik ekonomi simülasyonu (talep tahmini, fiyat optimizasyonu)
> Figma: İnşaat modu UI, finansal dashboard, sponsor sözleşme ekranları

# 5. MODÜL 4 — MAÇ MOTORU (MATCH ENGINE)

## 5.1. Görsel Yaklaşım

**2D Top-Down Görünüm:** USM 98 estetiğinde yukarıdan bakış, ama modern grafiklerle (gölge, partikül, smooth animasyon).

**Kamera Seçenekleri:** Tam saha, yakın takip, stadyum manzarası.

**Performans:** Düşük-orta mobil cihazda 30fps sabit (minimum spec Android hedefi).

## 5.2. Simülasyon Felsefesi

- **Deterministik Motor:** Aynı input + aynı seed = her zaman aynı çıktı. Online adalet ve replay sistemi için kritik.
- **Event-Based Sistem:** Her olay (pas, şut, faul) loglanır. Maç sonu LLM bu logu okur, röportaj üretir; Hikaye Motoru aynı logdan ark tetikleyicileri çıkarır.
- **Fizik Tabanlı Top:** Gerçekçi top sekmesi, yerçekimi, arka döndürme.
- **AI Karar Verme:** Behaviour Tree + Utility AI karması ile futbolcular karar verir.

## 5.3. Ayarlanabilir Chaos Engine

**Chaos Engine Nedir?:** Şans faktörü. Yetenekli takım her zaman kazanmaz; futbolun gerçekçi sürprizlerini simüle eder.

| Seviye | Şans Etkisi | Kullanım | Hedef Oyuncu |
| --- | --- | --- | --- |
| Düşük | %5-10 | Rekabetçi ligler, turnuvalar | Strateji odaklı, eSports |
| Orta (Default) | %15-25 | Public ligler, varsayılan | Ortalama oyuncu |
| Yüksek | %30-40 | Eğlence modu, casual | Dramatik an peşinde |

- **Uygulama Kuralları:** Özel liglerde kurucu seçer; public liglerde Orta default; offline kariyerde oyuncu seçer.

## 5.4. Maç İçi Etkileşim

- **Canlı Müdahale:** Oyuncu değişikliği, taktik değişimi, motivasyon konuşması.
- **Koçluk Talimatları:** Pressing yoğunluğu, defansif hat yüksekliği, tempoya müdahale.
- **Duraklatma:** Maç istenildiği an durdurulabilir, detaylar analiz edilebilir.

## 5.5. Maç Sonu (LLM Röportajı)

Maç bitiminde LLM, maçın "Event Log" verisini okur ve buna göre dinamik sorular sorar. Verilen cevaplar taraftar moralini, yönetim güvenini ve aktif hikaye arklarını doğrudan etkiler.

## 5.6. İzlenebilirlik ve Highlight Sistemi [YENİ v4.0]

Maçın teknik doğruluğu kadar izleme keyfi de ürünün kalbidir. FAZ 03'teki "Match Feel İterasyonu" alt-fazının tasarım temeli:

- **Dramatik An Tespiti:** Motor; beklenen gol (xG) sapması yüksek pozisyonları, son dakika gollerini, geri dönüşleri ve seri kurtarışları otomatik etiketler.
- **Highlight İşaretleri:** Maç zaman çizelgesinde tıklanabilir önemli an işaretleri; replay klip seçimi (bölüm 8.3) bu işaretlerden beslenir.
- **Tempo Eğrisi:** Sıkıcı orta saha pas trafiği hızlandırılmış akışta, kritik aksiyonlar normal hızda sunulur (izleyici modunda otomatik).
- **Kamera Vurguları:** Gol ve kritik pozisyonlarda kısa yakın takip + yavaşlatma; mobilde haptik geri bildirim.
- **İzlenebilirlik Testi:** Feel iterasyonunda 5 kişilik panel maçları sadece izler; "sıkıldım" anları işaretlenir, tempo parametreleri buna göre ayarlanır.

> **⚠️ KRİTİK UYGULAMA NOTU — MATCH ENGINE DETERMİNİZM**
> Match Engine DETERMİNİSTİK olmak ZORUNDA. AI ile yazılsa bile:
> • Her build öncesi 1.000 maç simülasyon + sonuç karşılaştırma testi
> • Unit testler: Aynı takım + aynı taktik + aynı seed = IDENTIK sonuç
> • Server-side simülasyon: Online maçlar asla client cihazında çalıştırılmıyor
> • Chaos faktörü bile deterministik: seed+time hash'ten türeyen pseudo-random
> • Balance parametreleri (JSON) de deterministik girdinin parçasıdır; replay kaydına config hash'i dahildir (bölüm 11.9)
> AI kod üretebilir ama bu testleri geçemezse REDDEDİLİR. Vibe & Verify kuralı.

# 6. MODÜL 5 — ONLINE ALTYAPI VE LİGLER

## 6.1. Lig Yapısı

- **Özel Ligler (Private):** Oyuncular kendi arkadaş grupları için şifreli lig kurabilir. Kurucu ligin kurallarını (Maç hızı, Chaos seviyesi, Transfer bütçesi) belirler.
- **Genel Ligler (Public):** Oyunun otomatik oluşturduğu, benzer Elo puanına sahip oyuncuların eşleştiği rekabetçi ligler.

## 6.2. Asenkron Oynanış (Sıra Tabanlı)

- **Zamanlayıcı:** Her ligin bir "Maç Saati" vardır (Örn: Her akşam 22:00).
- **Hayalet Menajer:** Oyuncular gün içinde taktiklerini ayarlar. Maç saati geldiğinde sunucu tüm maçları simüle eder.
- **Canlı İzleme:** Oyuncu maç saatinde online ise canlı izleyebilir ve müdahale yapabilir.

## 6.3. Teknik Güvenlik

**Server-Side Simulation:** Maç motoru online liglerde asla oyuncunun cihazında çalışmaz. Tüm hesaplamalar sunucuda yapılır, telefona sadece "Sonuç Verisi" ve replay kaydı gönderilir. Bu sayede client tarafı hile yapılması imkansızdır.

## 6.4. Sosyal Özellikler

- **Lig Sohbeti:** Metin tabanlı lig içi sohbet (moderasyon: AI içerik filtresi).
- **Oyuncu Profili:** Kariyer istatistikleri, kupa koleksiyonu, karşılıklı maç geçmişi.
- **Takım Paylaşımı:** Taktikleri arkadaşlarla paylaşma, top oyuncu izleme listesi.
- **Leaderboard:** Lig içi/global sıralamalar, haftalık şampiyon, ayın menajeri.
- **Klip Akışı [YENİ v4.0]:** Lig içi "Haftanın Golleri" akışı — üyelerin paylaştığı replay klipleri (bölüm 8.3).

## 6.5. Rekabet Bütünlüğü [YENİ v4.0]

Server-side simülasyon client hilesini keser; ancak danışıklı oynanışı kesmez. Bu katman onun için vardır:

- **Collusion (Danışıklı Maç) Tespiti:** Bağlantılı hesaplar arasında tekrarlayan tek taraflı sonuç örüntüleri, kasıtlı zayıf kadro çıkarma (sabotaj dizilişi) ve anormal transfer akışları sunucu tarafında örüntü analiziyle işaretlenir.
- **Elo Besleme / Sandbagging:** Public liglerde kasıtlı puan düşürüp alt segmentte ezici üstünlük kurma davranışı; ilerleme hızı anomalisi ve sonuç varyansı ile tespit edilir.
- **Smurf / Çoklu Hesap:** Cihaz parmak izi + davranışsal benzerlik skoru; yeni hesapta anormal ustalık işaretlenir.
- **Ceza Merdiveni:** 1) Uyarı → 2) Sezon lig puanı sıfırlama → 3) Public lig yasağı (30 gün) → 4) Kalıcı public yasak. Özel ligler (arkadaş grupları) bu denetimden muaftır.
- **İtiraz Süreci:** Tüm cezalar log kanıtıyla desteklenir; uygulama içi itiraz formu.

## 6.6. Maç Saati Yük Dengeleme [YENİ v4.0]

Tek bir global "22:00" maç saati, sunucuda dakikalık dev yük sivrilmesi yaratır. Çözüm:

- **Kaydırılmış Maç Saatleri:** Public ligler 20:00-23:30 arası 15 dakikalık dilimlere dağıtılır; özel ligler kurucunun seçtiği saatte kalır.
- **Simülasyon Kuyruğu:** Maçlar lig bazında kuyruklanır; hedef, 10.000 eşzamanlı lig maçının 10 dakikalık pencerede tamamlanması.
- **Maliyet Etkisi:** Kaydırma, zirve sunucu kapasite ihtiyacını yüzde 40-60 azaltır (bölüm 14.3).

# 7. MODÜL 6 — SEZON HİKAYE MOTORU VE ASİSTAN HAFIZASI [YENİ — v4.0]

## 7.1. Amaç

LLM entegrasyonunu tek maçlık röportajdan sezonluk anlatıya taşımak. Oyuncunun kararları haftalar boyunca yankı bulur; kulüp "yaşayan" bir dünyaya dönüşür. Bu modül, The Badge'un en güçlü farklılaştırıcısı olan LLM katmanını rakiplerin kopyalayamayacağı derinliğe çıkarır.

## 7.2. Storyline Mimarisi

- **Storyline Nesnesi:** { tip, durum, katılımcılar, tetikleyiciler, son_beat, süre_sayacı } yapısında veri nesnesi.
- **Yaşam Döngüsü:** Tohum → Tırmanma → Karar Anı → Çözülme. Her aşama "beat" adı verilen olay parçalarıyla ilerler.
- **Tetikleyiciler:** Maç Event Log'u, transfer olayları, moral eşikleri ve başkan hedef sapmaları arkları tohumlar veya ilerletir.
- **Eşzamanlılık Limiti:** Aynı anda en fazla 3 aktif ark (bilişsel yük + LLM token bütçesi kontrolü). Yeni tohumlar kuyruğa alınır.

## 7.3. Ark Tipleri

- **Rekabet Arkı:** Rakip menajerle basın üzerinden polemik; derbi haftalarında doruk yapar.
- **Oyuncu Sagası:** Yıldızın transfer isteği, genç yeteneğin çıkışı, kaptanla moral krizi.
- **Yönetim Baskısı:** Başkan hedeflerinden sapma → ültimatom → güven oylaması dizisi.
- **Medya Anlatısı:** Basının kulüp hakkında ördüğü gündem (taktik eleştirisi, taraftar tepkisi).
- **Sezon Anlatısı:** Şampiyonluk yarışı / düşme hattı dramaturjisi; Panorama'ya içerik sağlar.

## 7.4. Oyuncu Etkisi ve Sınırlar

- Röportaj ve birebir konuşma seçimleri ark durumunu değiştirir (yatıştırma, körükleme, görmezden gelme).
- **Etki Sınırı:** Arklar yalnızca sınırlı modifiye ediciler üretir (moral ±, yönetim güveni ±, taraftar desteği ±). Maç determinizmine asla doğrudan dokunmaz; tüm etkiler sunucu doğrulamalıdır ve bant sınırları JSON config'te tanımlıdır.
- **Adalet Kuralı:** Online liglerde ark modifiye edicileri tüm oyuncular için aynı bant içindedir; hikaye avantajı satın alınamaz.

## 7.5. Asistan Hafızası (Persistent Memory)

- **Veri Katmanı:** SQLite tablo memory_facts { varlık, olgu, önem_puanı, zaman, decay_katsayısı }.
- **Haftalık Özetleyici:** LLM'siz, şablon tabanlı yerel özetleyici her hafta "rolling sezon özeti" üretir (maliyet: sıfır API çağrısı).
- **Bağlam Enjeksiyonu:** Her LLM çağrısında persona başına en önemli 12 olgu (~600 token) sistem bağlamına eklenir.
- **Örnek Çıktı:** "Başkana 3 hafta önce ilk 4'ü söz vermiştin; şu an 7. sıradasın." / "Kaptan, derbi öncesi yaptığın konuşmayı hâlâ takdirle anıyor."

## 7.6. Hafıza Kategorileri

- **Sözler:** Oyuncunun başkana, basına, futbolculara verdiği taahhütler.
- **Olaylar:** Kritik maç sonuçları, transferler, krizler.
- **İlişki Skorları:** Persona başına -100/+100 güven-yakınlık ekseni.
- **İstatistik Anları:** Rekorlar, seriler, kişisel kilometre taşları.

## 7.7. Maliyet ve Cache Uyumu

- Ark beat'leri senaryo şablonu + dinamik slot mimarisiyle üretilir; Redis cache kategorilerine (bölüm 11.6) yeni "Storyline Beats" kategorisi eklenir (TTL: 14 gün).
- **Hedef:** Hikaye Motoru'nun LLM maliyeti, toplam LLM harcamasının yüzde 20'sini aşmaz.

## 7.8. Lansman Kapsamı

Lansmanda "Lite" mod: Yönetim Baskısı + Oyuncu Sagası aktif. Tam ark seti lansman + 6 haftada devrede (bölüm 1.5).

> **🎨 AI-FIRST ASSET ÜRETİM STRATEJİSİ — HİKAYE MOTORU**
> Claude API: Ark beat üretimi, persona diyalog varyasyonları (şablon+slot, cache dostu)
> Figma: Hikaye akış paneli (Hub gazete/bildirim entegrasyonu), ark zaman çizelgesi UI
> Rive: Ark bildirimi micro-animasyonları, "gerilim göstergesi" animasyonu
> Scenario: Ark görselleri (gazete manşet fotoğrafları, kurgusal basın kartları)

# 8. MODÜL 7 — REPLAY PAYLAŞIMI VE HAFTANIN PANORAMASI [YENİ — v4.0]

## 8.1. Deterministik Replay

Motorun determinizmi, replay'i neredeyse bedavaya getirir:

- **Kayıt Formatı:** { motor_versiyonu, config_hash, seed, girdi zaman çizelgesi, kadro snapshot referansı } → maç başına ~5-20 KB.
- **Oynatma:** İstemci aynı sim çekirdeğiyle maçı yeniden simüle eder; video dosyası saklamaya gerek yoktur.
- **Kontroller:** İleri/geri sarma, hız (0.5x-4x), highlight işaretlerine atlama (bölüm 5.6), kamera değiştirme.

## 8.2. Motor Versiyonlama Kuralı

- Replay yalnızca kaydedildiği sim çekirdeği versiyonu + config hash ile birebir oynar.
- **Sezon İçi Dondurma:** Bir online sezon boyunca sim çekirdeği değiştirilmez; balance güncellemeleri sezon geçişlerinde yapılır.
- **Arşivleme:** Eski sezon replay'leri, öne çıkanlar video olarak render edilip CDN arşivine alınır; ham kayıtlar 2 sezon saklanır.

## 8.3. Klip Paylaşımı (Organik Büyüme Motoru)

- **Klip Seçimi:** Oyuncu highlight işaretinden pencere seçer (örn. gol ±15 sn).
- **Dışa Aktarım:** İstemci klip penceresini video olarak render eder; native paylaşım sayfası + oyun içi replay derin bağlantısı (deep link).
- **Watermark ve Atıf:** Her klipte "The Badge" damgası + install atıflama linki; paylaşım → indirme dönüşümü ölçülür (K-faktör, bölüm 18.3).
- **Lig İçi Akış:** Klipler lig sohbet akışına ve "Haftanın Golleri" paneline düşer; beğeni ile haftalık gol ödülü seçilir.

## 8.4. Haftanın Panoraması

Her lig için haftada bir üretilen, 60-120 saniyelik özet programı:

- **İçerik Seçimi:** Sunucu; sürpriz sonuçları, haftanın gollerini, sıralama değişimlerini ve Hikaye Motoru dramlarını puanlayıp seçer.
- **Senaryo:** LLM, şablon + dinamik slot ile spiker metni yazar (Redis cache kategorisi, TTL: 7 gün).
- **Seslendirme:** ElevenLabs TTS — haftalık toplu üretim (kullanıcı başına değil, lig başına bir kez).
- **Montaj:** Seçilen replay klipleri + skor bantları + jenerik; Hub'daki Televizyon nesnesinden izlenir.
- **Maliyet Modeli:** Üretim lig sayısıyla ölçeklenir, kullanıcı sayısıyla değil. Tahmin: lig başına haftada ~0,03-0,08 dolar (bölüm 14.3).

## 8.5. Lansman Kapsamı

Replay kaydı + klip paylaşımı lansman günü aktiftir. Panorama, lansman + 4 haftada devreye girer (bölüm 1.5).

> **🎨 AI-FIRST ASSET ÜRETİM STRATEJİSİ — REPLAY & PANORAMA**
> Suno AI: Panorama jenerik müziği + geçiş jingle'ları
> ElevenLabs: Spiker sesi (Türkçe/İngilizce; haftalık toplu TTS)
> Rive: Skor bandı, alt yazı şeridi, sıralama tablosu animasyonları
> Midjourney: Panorama stüdyo arka planı, program logosu varyasyonları

# 9. FTUE — İLK OYUNCU DENEYİMİ VE ONBOARDING [YENİ — v4.0]

## 9.1. Felsefe: Diegetik Tutorial

Ayrı bir "tutorial ekranı" yoktur. Öğretim, oyunun dünyasının içinden akar: asistan anlatır, oyuncu yaparak öğrenir. Tycoon + menajerlik + LLM hibrit yapısının karmaşıklığı, D1 tutundurma hedefinin (yüzde 40+) en büyük tehdididir; bu bölüm o tehdidin panzehiridir.

## 9.2. Açılış Senaryosu — "Enkazı Devral"

Oyuncu, iflasın eşiğindeki bir amatör kulübü devralır. İlk 15 dakika akışı:

- **Dakika 0-2:** Yıkık dökük ofise varış; asistan kendini tanıtır (senaryolu + önbellekli diyalog — canlı LLM API çağrısı YOK).
- **Dakika 2-4:** İlk tycoon dokunuşu: bilet fiyatını ayarla, tahmini doluluk etkisini canlı gör.
- **Dakika 4-7:** İlk taktik dokunuşu: 2 futbolcuyu taktik tahtasında serbestçe sürükle; sistemin diziliş tanımasını gör.
- **Dakika 7-12:** İlk maç: hızlandırılmış akışta, 2 yönlendirilmiş müdahale (oyuncu değişikliği + tempo talimatı).
- **Dakika 12-14:** Maç sonu mini röportaj (önbellekli varyasyonlar); cevabın taraftar moraline etkisi gösterilir.
- **Dakika 14-15:** İlk hikaye tohumu ekilir: başkandan kısa bir "beklentilerim var" mesajı.

## 9.3. Aşamalı Açılım (Progressive Disclosure)

- **Oyun içi Hafta 1:** Çekirdek döngü (taktik + maç + temel gelir).
- **Hafta 2:** Stadyum İnşaat Modu açılır.
- **Hafta 3:** Sponsorluk + transfer penceresi açılır.
- **Hafta 4:** Online lige davet + Mod B (serbest metin) tanıtımı.

## 9.4. İlk Oturum Tamamen Çevrimdışı

FTUE'nun tamamı senaryolu/önbellekli içerikle çalışır; LLM API bağımlılığı yoktur. Gerekçe: maliyet kontrolü, ilk oturum güvenilirliği (zayıf bağlantıda bile akıcı ilk izlenim) ve store inceleme süreci uyumu.

## 9.5. Ölçümleme

- Her FTUE adımı funnel telemetrisiyle izlenir (Firebase Analytics özel event seti).
- **Hedefler:** Adım 1 tamamlama yüzde 85+, ilk maç tamamlama yüzde 60+, FTUE tam bitirme yüzde 50+.
- FTUE funnel'ı, D1 tutundurma hedefinin (yüzde 40+) öncü göstergesi olarak haftalık raporlanır.

## 9.6. Veteran Atlaması

"Menajerlik oyunlarında deneyimliyim" seçeneği: hızlandırılmış kurulum (3 dakikalık özet tur) + tüm özellikler ilk günden açık.

# 10. İÇERİK VE LİSANS STRATEJİSİ — KURGUSAL EVREN [YENİ — v4.0]

> **⚖️ STRATEJİK KARAR — KURGUSAL EVREN + TOPLULUK EDİTÖRÜ**
> Gerçek oyuncu, kulüp ve lig isimleri/görselleri KULLANILMAZ. Oyun tamamen özgün bir futbol evreninde geçer.
> Topluluk, isimleri yerel editör paketleriyle kendisi değiştirebilir; resmi sunucular bu paketleri BARINDIRMAZ ve DAĞITMAZ.
> Gerekçe: Gerçek isim/lisans kullanımı milyonlarca dolarlık lisans anlaşması gerektirir (FM modeli) ve solo geliştirici için varoluşsal hukuki risktir. FM topluluğunun "real name fix" kültürü, editör yaklaşımının oyuncular tarafından kabul gördüğünü kanıtlar.

## 10.1. Kurgusal Oyuncu Veritabanı

- **Ölçek:** 50.000+ prosedürel üretilmiş futbolcu; tutarlı yetenek dağılımları (bölgesel güç eğrileri JSON config'te).
- **İsim Havuzları:** Bölge başına AI üretimli, fonetik olarak otantik ad-soyad havuzları (Türkçe, İspanyolca, Portekizce, İngilizce, Almanca, Fransızca, Arapça vb.).
- **Çakışma Karalisti:** Üretilen tam isimler, tanınmış gerçek futbolcular karalistesiyle otomatik karşılaştırılır; çakışma bulunursa yeniden üretilir.
- **Portreler:** Scenario custom model ile tamamen kurgusal yüzler; prompt setinde gerçek kişi adı/benzerliği kullanımı yasaktır (üretim pipeline kuralı).

## 10.2. Kurgusal Lig Piramidi ve Kulüpler

- **Piramit:** Türkiye esintili ulusal piramit + Avrupa, Güney Amerika ve Orta Doğu kurgusal ligleri; kıtasal kupa yapısı özgün isimlerle.
- **Benzerlik Yasağı:** Kulüp adı + renk + arma kombinasyonları, gerçek kulüplerin markalarıyla "karıştırılabilir benzerlik" oluşturamaz. Lansman öncesi iç denetim listesiyle tarama yapılır (ör. gerçek kulüp adının tek harf değişmiş türevleri yasak).
- **Sponsorlar ve Markalar:** Oyun içi tüm sponsor/ürün markaları kurgusaldır (bölüm 4.2).

## 10.3. Topluluk Editörü

- **Kapsam:** Lig, kulüp, oyuncu ve turnuva adlarının + kit renklerinin yerel olarak yeniden adlandırılması/düzenlenmesi.
- **Paket Formatı:** JSON tabanlı içe/dışa aktarma ("Editör Paketi"); dosya olarak elden ele paylaşılabilir.
- **Kanonik ID Mimarisi:** Sunucu yalnızca kanonik ID'leri bilir; editör paketleri SADECE istemcide görüntü katmanına uygulanır. Online ligde her kullanıcı kendi paketini görür; oyun bütünlüğü etkilenmez.
- **Barındırmama İlkesi:** Resmi sunucular ve oyun içi mağaza, editör paketi barındırmaz/dağıtmaz/öne çıkarmaz. Hukuki ayrım: paketler kullanıcı üretimi içeriktir ve dağıtımı kullanıcıya aittir.

## 10.4. Hukuki Çerçeve

- **ToS Maddesi:** Editör paketlerinin sorumluluğu üreten/paylaşan kullanıcıya aittir; marka ihlali içeren paketlerin resmi kanallarda tanıtımı yasaktır.
- **Bildirim-Kaldırma:** İleride opsiyonel resmi paylaşım altyapısı açılırsa DMCA benzeri bildirim-kaldırma prosedürü devreye girer (şu an kapsam dışı).
- **Lansman Öncesi Tarama:** Kulüp/lig adları ve armalar için marka benzerlik taraması; şüpheli öğeler yeniden tasarlanır.
- **Risk Kaydı:** Bu stratejiyle Lisans/IP riski "Kritik etki / Yüksek olasılık" seviyesinden "Kritik etki / Düşük olasılık" seviyesine çekilir (bölüm 18.2).

# 11. TEKNİK MİMARİ [GÜNCELLENDİ — v4.0]

## 11.1. Teknoloji Stack'i

- **Game Engine:** Unity 6.3 LTS (C#)
- **UI Framework:** Unity UI Toolkit (UXML + USS, data binding)
- **Backend:** Nakama — auth, lig, sosyal, eşleştirme (Docker self-hosted; ölçekte managed seçenek 14.3)
- **Simülasyon Servisi [YENİ v4.1]:** .NET 8 headless C# worker'lar — Match Engine ve doğrulama zinciri sunucuda AYNI C# kodla koşar, Nakama RPC ile konuşur (tek-kaynak ilkesi)
- **Veritabanı:** SQLite (client local) + PostgreSQL (server)
- **Caching:** Redis (LLM yanıt cache + oturum verileri)
- **LLM API:** Claude (ana görevlerde Sonnet, hızlı/ucuz görevlerde Haiku); model seçimi FAZ 04 maliyet benchmark'ıyla kesinleşir. Ton sınıflandırma gibi basit görevler kural tabanlıdır — LLM çağrısı gerektirmez [v4.1]
- **Sync:** WebSocket (canlı maç izleme) + REST (genel veri)
- **Analytics:** Firebase Analytics + custom telemetry (FTUE funnel event seti dahil)
- **Crash Reporting:** Firebase Crashlytics
- **CI/CD:** GitHub Actions (GameCI) + Fastlane

## 11.2. Veri Mimarisi

**Client-Side:** ScriptableObject (statik veri — ligler, yetenekler) + SQLite (dinamik veri — oyuncular, maçlar, memory_facts).

**Server-Side:** PostgreSQL (kullanıcı verileri, lig kayıtları, maç sonuçları, replay kayıtları, bütünlük logları). Simülasyon ve komut doğrulama .NET C# servis katmanında koşar; Nakama yalnız platform hizmetlerini üstlenir [v4.1].

**External Config:** JSON/YAML (ekonomik parametreler, stat kuralları, ark etki bantları — hotfix için). Config değişiklikleri versiyonlanır ve hash'lenir (replay uyumluluğu, bölüm 11.9).

## 11.3. Performans Hedefleri

- **Minimum Android:** Snapdragon 665 / 3GB RAM / Android 9.0
- **Minimum iOS:** iPhone XR / iOS 16
- **FPS Hedef:** 30fps düşük cihazda, 60fps orta-üst cihazda
- **Build Size:** 200MB altı (APK/IPA ilk indirme)
- **Maç Simülasyon [REVİZE v4.1]:** Oyuncunun ligi (1 × LOD 0 + 9 × LOD 1) 12 saniyenin altında; tam dünya turu 20 saniyenin altında (ME Spec 2.4 LOD modeli)
- **Replay Oynatma:** Kayıttan yeniden simülasyon başlatma gecikmesi 2 saniyenin altında

## 11.4. Modüler Mimari Prensipleri

- **FM26-Inspired Interrupt Abstraction Layer:** Match Engine ↔ UI arası Data Store pattern.
- **Decoupled Match Engine:** UI'dan bağımsız, tamamen unit-test edilebilir simülasyon çekirdeği.
- **Command Bus — "Tek Kapı" Prensibi [YENİ v4.0]:** Oyun durumunu değiştiren HER eylem (UI butonu, LLM önerisi, otomasyon) aynı Command Bus'tan geçer ve aynı doğrulayıcıya tabidir. LLM için ayrı/özel bir yürütme yolu YOKTUR.
- **Virtualized Lists:** 50.000+ oyuncu veritabanı için pooled cells + lazy loading.
- **Tile/Card UI Pattern:** Kompakt tile → detaylı card genişleme (FM26 UX deseni).
- **Externalized Game Parameters:** Tüm dengeleme parametreleri JSON/YAML'de; kod değiştirmeden balance update.

## 11.5. LLM Entegrasyonu — Persona Prompt'ları

- **Asistan:** Profesyonel, bilgili, saygılı. Oyuncunun tavsiyesinde teknik detaya giriyor.
- **Medya (Gazeteci):** Kışkırtıcı, manşet peşinde, polemik çıkarmaya meyilli.
- **Futbolcular:** Her oyuncunun kendine özgü karakteri (dışadönük/içedönük, lider/takipçi).
- **Başkan:** Sonuç odaklı, maddi konuşuyor, beklentileri net.
- **Rakip Menajerler:** Rekabet, respekt veya kişilik bazlı tepkiler; Rekabet Arkı'nın (7.3) taşıyıcısı.
- **Hafıza Bağlamı:** Tüm personalar, 11.8'deki hafıza katmanından persona-özel bağlam alır.

## 11.6. LLM Önbellek Sistemi (Redis)

**Amaç:** Tekrarlayan prompt kalıpları için API çağrısı yapmadan cached yanıt döndürerek yüzde 40-60 maliyet tasarrufu.

**Önbellek Kategorileri:**

- **Maç Sonu Röportajları:** Senaryo bazlı (3-0 galibiyet, 0-2 mağlubiyet, 1-1 beraberlik + olay tipi). TTL: 30 gün.
- **Asistan Tavsiyeleri:** Sık sorulan 200+ tavsiye kalıbı. TTL: 7 gün.
- **Gazete Manşetleri:** Maç sonucuna göre dinamik şablon. TTL: 14 gün.
- **Transfer Müzakeresi:** Oyuncu tipi + teklif aralığı kombinasyonları. TTL: 30 gün.
- **Storyline Beats [YENİ v4.0]:** Ark tipi + aşama + bağlam sınıfı kombinasyonları. TTL: 14 gün.
- **Panorama Senaryoları [YENİ v4.0]:** Hafta özeti şablon + slot kombinasyonları. TTL: 7 gün.

**Cache Miss Stratejisi:** Cache'de yoksa → LLM API çağrısı → yanıt cache'e kaydedilir → ileride benzer istekler cache'den döner.

## 11.7. LLM Güvenlik Katmanı — Mod B [YENİ — v4.0]

> **🔒 GÜVENLİK İLKESİ**
> LLM asla yürütmez; yalnızca ÖNERİR. Oyun durumunu değiştiren tek yol, sunucu tarafında doğrulanan Command Bus'tır. "Bana Messi'yi bedava transfer et" tarzı prompt injection girişimleri mimari olarak sonuçsuzdur.

- **Whitelist IntentAction Şeması:** LLM çıktısı, önceden tanımlı aksiyon listesine (ör. SetTicketPrice, ProposeTransferOffer, SetTrainingPlan, ArrangeTalk) ve JSON Schema doğrulamasına tabidir. Şema dışı çıktı reddedilir.
- **Sunucu Doğrulayıcı:** Her aksiyon için 4 kontrol: aksiyon tanımlı mı, parametreler bant içinde mi, oyuncu kaynağa/yetkiye sahip mi, rate limit aşıldı mı (kullanıcı başına dakikada 10 Mod B çağrısı).
- **Prompt Injection Savunması:** Kullanıcı metni her zaman "veri" olarak etiketlenir; sistem prompt'ları yalnızca sunucuda tutulur; çıktı şema doğrulaması + jailbreak/istismar filtresi uygulanır.
- **Ekonomik Güvence:** Hiçbir LLM yolu para, oyuncu, stat veya eşya ÜRETEMEZ. Tüm durum değişimleri, UI butonlarıyla aynı doğrulama hattından geçer (Tek Kapı, 11.4).
- **İçerik Moderasyonu:** Lig sohbeti ve Mod B girdileri AI içerik filtresinden geçer (nefret söylemi, taciz, kişisel veri paylaşımı engellenir).
- **Denetim Logu:** Tüm Mod B etkileşimleri (girdi, önerilen aksiyon, doğrulama sonucu) loglanır; istismar analizi ve itiraz süreçlerinde kullanılır.
- **Test Zorunluluğu:** Her whitelist aksiyonu için injection test seti CI'da koşar (bölüm 16.3).

## 11.8. Hafıza Veri Mimarisi [YENİ — v4.0]

- **Şema:** memory_facts { id, persona_id, varlık, olgu_metni, önem (0-100), olusturma_zamani, decay_katsayısı, kategori }.
- **Özetleme:** Haftalık yerel özetleyici (şablon tabanlı, LLM'siz) düşük önemli olguları birleştirir; tablo boyutu save başına 2.000 kayıtla sınırlandırılır.
- **Token Bütçesi:** Çağrı başına persona bağlamı en fazla ~600 token (12 olgu); sezon özeti ~200 token.
- **Gizlilik:** Hafıza verisi save dosyasına aittir; sunucuya yalnızca online etkileşimlerde gereken alt küme gider.

## 11.9. Replay Veri Mimarisi [YENİ — v4.0]

- **Kayıt:** { motor_versiyonu, config_hash, seed, girdi_zaman_cizelgesi, kadro_snapshot_ref } — maç başına ~5-20 KB, sunucuda saklanır.
- **Uyumluluk:** Oynatma, motor_versiyonu + config_hash eşleşmesini şart koşar; sezon içi motor dondurma kuralı (8.2) bu uyumu garanti eder.
- **Saklama Politikası:** Ham kayıtlar 2 sezon; öne çıkan maçlar video render + CDN arşivi.
- **Klip Render:** Video export istemcide yapılır (sunucu maliyeti sıfır); yalnızca paylaşım meta verisi sunucuya gider.

# 12. MONETİZASYON STRATEJİSİ [GÜNCELLENDİ — v4.0]

> **💡 TEMEL FELSEFE — PAY-TO-PROGRESS-FASTER**
> "Para veren oyuncu sonuca etki eden avantajlar alır, ancak bu avantajlar geçici süreli. Para vermeyen oyuncu aynı sonuçlara daha uzun sürede ulaşır, ancak kazandıkları kalıcı. Sürekli para harcayanlar sürekli hızlı kalır; durduklarında herkes eşitlenir."
> → Sonuç-odaklı monetizasyon ama "Pay-to-Win" algısından uzak
> → Ücretsiz oyuncu da aynı yere ulaşabilir, sadece daha uzun sürede
> → v4.0 eki: Kozmetik Ekonomi ve Sezon Pası, oynanışa SIFIR etkiyle ikinci ve üçüncü gelir hattını açar

## 12.1. Personel Sistemi (Sonucu Doğrudan Etkileyen)

### Scout

- **Ücretsiz (Kalıcı):** Temel yetenek aralığı gösterir (60-85). Başarılarla daha iyi scoutlar kalıcı kazanılır.
- **Premium (Geçici):** Tam değerler, gizli stats, potansiyel analizi, "gizli cevherler" listesi.

### Taktik Analist

- **Ücretsiz:** Maç sonrası genel özet.
- **Premium (Geçici):** Canlı maçta rakip zayıf noktalarını gösterir, taktik değişikliği önerir, oyuncu değişikliği önerir.

### Antrenör

- **Ücretsiz:** Standart antrenman verimliliği (1x).
- **Premium (Geçici):** Antrenman 1.5x-2x verimli, gelişim hızlı, sakatlık riski azalır. Süre dolunca statlar kalır, gelişim normale döner.

### Doktor/Fizyoterapist

- **Ücretsiz:** Standart iyileşme süresi.
- **Premium (Geçici):** Sakatlık süresi yüzde 30-50 kısalır, şiddet azalır.

### Spor Psikologu

- **Premium Tier 2:** Oyuncu morallerini önceden tahmin.
- **Premium Tier 3:** Soyunma odası krizi önleme, LLM motivasyon konuşmaları.

## 12.2. Ekonomik Avantajlar

### Sponsor Danışmanı (Geçici)

Daha yüksek değerli sponsor teklifleri getirir, görüşmelerde daha iyi şartlar sağlar. Süre dolunca mevcut sözleşmeler geçerliliğini korur, yeni görüşmelerde avantaj kaybolur.

### Finans Direktörü (Geçici)

Maç günü gelirlerini yüzde 15-25 artırır, banka kredilerinde düşük faiz sağlar, maaş görüşmelerinde avantaj verir. Süre dolunca kazanılan para kasada kalır.

## 12.3. Transfer Pazarı Avantajları

### Gizli Oyuncu Listeleri (Geçici)

Diğer kulüplerin satmaya hazır olduğu oyuncuları görür, serbest kalacak oyuncuların erken bildirimi, genç yetenek keşif önceliği.

### Hızlı Pazarlık (Tek Kullanımlık)

Transfer görüşmelerini hızlandırır, rakip kulüplerin tepki süresini kısaltır.

## 12.4. Maç Günü

### Maç Hazırlık Paketi (Tek Kullanımlık)

Rakip analizi önceden hazır, spesifik taktik önerileri, kilit rakip oyuncuların güçlü/zayıf yönleri.

### Moral Takviyesi (Tek Kullanımlık)

Maç öncesi takım moralini yüzde 15-20 artırır. Kritik maçlarda (derbi, şampiyonluk) çok değerli.

### Yedek Kulübesi Genişletmesi (Geçici)

Maç içinde 4-5 değişiklik hakkı (standart 3 yerine). Süre boyunca tüm maçlarda geçerli.

## 12.5. Altyapı Hızlandırma

### İnşaat Hızlandırıcı (Geçici)

Stadyum/tesis inşaatını yüzde 50-100 hızlandırır. Tamamlanan binalar kalıcı, devam edenler normal hıza döner.

### Altyapı Uzmanı (Geçici)

Genç oyuncu yetiştirme hızlı, başlangıç statları yüksek. Süre dolunca o dönem yetiştirilenler kalır.

## 12.6. Zaman Paketleri

### Sezon Hızlandırıcı (Geçici)

Hafta içi simülasyonu hızlandırır. Online liglerde geçersiz, offline kariyerde çalışır.

### Çoklu Kariyer (Geçici)

Aynı anda birden fazla kulüp yönetme. Süre dolunca bir kulüp seçilir, diğerleri askıya alınır.

## 12.7. Fiyatlandırma Yapısı

- **Personel Paketleri (7 gün):** 1,99-2,99 dolar
- **Personel Paketleri (14 gün):** 3,49-4,99 dolar
- **Personel Paketleri (30 gün):** 5,99-8,99 dolar
- **Maç Hazırlık Paketi:** 0,49-0,99 dolar
- **Moral Takviyesi:** 0,29-0,49 dolar
- **Hızlandırıcılar (tek):** 0,99-1,99 dolar
- **Komple Paket (7 gün):** 4,99 dolar
- **Komple Paket (30 gün):** 14,99 dolar
- **Bölgesel Fiyatlama [YENİ v4.1]:** USD değerler referanstır; TR/LATAM için yüzde 40-60 indirimli yerel fiyat katmanları lansman öncesi tabloya işlenir.

## 12.8. Ücretsiz Oyuncu Kazanım Yolları

**Kalıcı Personel Kazanma:**

- **İlk lig şampiyonluğu:** Tier 2 Scout (kalıcı)
- **Üst üste 2 şampiyonluk:** Tier 2 Antrenör (kalıcı)
- **Kupa kazanma:** Tier 2 Taktik Analist (kalıcı)
- **Avrupa kupası kazanma:** Tier 3 personel seçimi (kalıcı)

**Geçici Premium Deneyimi:**

- **Aylık 3 gün:** Ücretsiz premium personel
- **Arkadaş davet:** 7 gün premium
- **Etkinlikler:** Özel günlerde premium ödüller

## 12.9. Kozmetik Ekonomi [YENİ — v4.0]

Oynanışa etkisi SIFIR olan, kimlik ve ifade odaklı gelir hattı:

- **Forma Editörü:** Desen, renk paleti, yaka/kol varyasyonları; premium desen paketleri.
- **Arma Editörü:** Şekil, sembol, çerçeve kütüphanesi; premium sembol setleri.
- **Ofis Dekorasyonu:** Tablo, maket, kupa vitrini, aydınlatma temaları (Hub tier'ları ile uyumlu — bölüm 2.1).
- **Gol Kutlama Animasyonları:** Takıma özel kutlama paketleri (Rive animasyonları).
- **UI Temaları:** Renk temaları, skorboard stilleri.
- **Fiyatlandırma:** Tekil öğe 0,99-4,99 dolar; temalı paketler 3,99-9,99 dolar.
- **Kazanılabilirlik:** Her kozmetik kategorisinin bir alt kümesi başarımlarla ücretsiz kazanılır (P2W algısına karşı tampon).
- **Gelir Payı Hedefi:** Toplam gelirin yüzde 25-30'u.

## 12.10. Sezon Pası [YENİ — v4.0]

- **Ritim:** 8 haftalık "Sezonlar"; her sezon yeni kozmetik seti + tema.
- **Ücretsiz Şerit:** Kozmetikler + küçük hızlandırıcılar (P2PF uyumlu geçici ödüller).
- **Premium Şerit:** 5,99 dolar; ek kozmetikler, özel ofis teması, sezon çerçevesi.
- **Pass XP:** Maç oynama + haftalık objektiflerle kazanılır; tier atlama satın alınabilir (ilerleme hızı satışı — felsefeyle tutarlı, kalıcı avantaj içermez).
- **Kalıcılık:** Sezon sonunda kazanılan kozmetikler kalıcıdır; kaçırılan sezon içerikleri 1 yıl sonra "arşiv vitrini"nde döner.
- **Gelir Payı Hedefi:** Toplam gelirin yüzde 10-15'i.

## 12.11. Gelir Karması Hedefi [YENİ — v4.0]

| Gelir Hattı | Hedef Pay | Not |
| --- | --- | --- |
| Fonksiyonel-Geçici (Personel, Paketler, Hızlandırıcılar) | %55-65 | Mevcut P2PF çekirdeği |
| Kozmetik Ekonomi | %25-30 | Oynanış etkisi sıfır; algı açısından en güvenli hat |
| Sezon Pası | %10-15 | Tutundurma + öngörülebilir tekrarlayan gelir |

# 13. AI-FIRST GELİŞTİRME PIPELINE'I

> **🚀 STRATEJİK KARAR — AI-FIRST GELİŞTİRME**
> The Badge geleneksel yöntemle değil, AI-First yaklaşımla inşa edilecek.
> Neden AI-First?
> • Geleneksel tahmini süre: 18-24 ay, maliyet: 80.000-150.000 dolar
> • AI-First tahmini süre: 24-36 hafta (bölüm 17), aylık araç maliyeti: ~69 dolar
> • Karpathy'nin "vibe coding" metodolojisi + FM26 Unity/UI Toolkit kanıtı
> • Kıdemli dev'ler AI coding ile yüzde 81 verimlilik artışı rapor ediyor
> Temel Prensip: "Vibe & Verify" → AI kod üretir, insan doğrular. Test geçmeyen AI kodu reddedilir.

## 13.1. AI-First Nedir?

**Vibe Coding:** Andrej Karpathy tarafından Şubat 2025'te tanımlanan, AI asistanlarla doğal dilde talimat vererek kod üretmeye dayalı geliştirme metodolojisi. 2026'da Collins Dictionary'nin "Word of the Year"ı.

**Yaklaşım:** Developer kodun yüzde 80'ini AI'a yazdırır (boilerplate, UI, veri modelleri, testler). Yüzde 20'sini (core algoritmalar, match engine) elle yazar veya yoğun review'dan geçirir.

**İstatistik:** 2026 itibarıyla US developer'ların yüzde 92'si günlük AI kodlama araçları kullanıyor. Kıdemli dev'lerde yüzde 81 productivity gain.

## 13.2. Pipeline'ın 2 Katmanı (+ Opsiyonel)

### Katman 1: Stratejik (Claude — Projects + Sohbet)

- **Mimari tasarım:** Sistem şemaları, design patterns, FM26 mimarisini USM'e adaptasyon
- **Algoritma tasarımı:** Match engine matematik modeli, chaos engine olasılık dağılımları
- **GDD rafinasyonu:** Mevcut dokümanı sürekli güncel tutma, tutarsızlık tespiti
- **Code review:** Haftalık tüm codebase analizi, optimization önerileri
- **Prompt engineering:** LLM persona tanımları, function calling şemaları, güvenlik katmanı şablonları

### Katman 2: Operasyonel (Claude Code)

- **Multi-file reasoning:** Birden fazla dosyayı aynı anda anlayarak refactoring
- **Agentic görevler:** Görev delege edilir; Claude Code kodu yazar, testleri KOŞAR, sonucu raporlar — Vibe & Verify'ın Verify adımı iş akışına gömülür
- **CLAUDE.md dosyası:** Projeye özel C# conventions, determinizm disiplini ve Tek Kapı kuralları otomatik bağlam olarak yüklenir
- **Hızlı prototipleme:** Konsept test için 30 dakikada çalışan prototip

### Opsiyonel: Satır İçi Tamamlama

- İstenirse VS Code + ücretsiz Copilot tab-completion yan araç olarak kullanılabilir; pipeline'ın çekirdeği DEĞİLDİR. Unit test üretimi dahil tüm kod görevleri Claude Code'a delege edilir.

## 13.3. AI-Assisted Asset Üretimi

Geleneksel yöntemle asset üretim maliyeti 3.700-7.500 dolar. AI-first yaklaşımla 800-2.000 dolar (yüzde 70-80 tasarruf).

### Görsel Assetler

- **Scenario (15 dolar/ay):** Custom model train → tutarlı stilde kurgusal oyuncu portreleri, kit, badge
- **Midjourney (10 dolar/ay):** Stadyum, ofis, konsept art, Panorama stüdyosu
- **Unity AI (dahil):** Texture, material, sprite generation

### Ses Assetleri

- **Suno AI (10 dolar/ay):** 10+ müzik parçası (menü, maç, zafer, mağlubiyet, Panorama jeneriği)
- **ElevenLabs (5 dolar/ay):** Commentator, asistan, narrator, Panorama spikeri sesleri
- **Meta AudioCraft (ücretsiz):** Ambiyans sesleri, UI ses efektleri

### Analitik ve Pazar

- **Ludo.ai (ücretsiz):** 1M oyun veritabanında pazar doğrulama, Ludo Score
- **Claude (web search):** Rakip analizi, trend araştırması — ayrı araç gerekmez [v4.1]

# 14. AI TOOL STACK VE MALİYET ANALİZİ [GÜNCELLENDİ — v4.0]

## 14.1. Komple Tool Stack

| Kategori | Araç | Kullanım | Maliyet/ay |
| --- | --- | --- | --- |
| Motor | Unity 6 Personal | Oyun motoru (FM26 referansı) | Ücretsiz* |
| Ajan Kodlama | Claude Code | Agentic geliştirme, CLAUDE.md bağlamı | Claude Pro'ya dahil |
| Inline AI (ops.) | Copilot Free | İsteğe bağlı tab-completion | Ücretsiz |
| AI Asistan | Claude Pro | Mimari, algoritma, code review | 20 dolar |
| UI Tasarım | Figma | Ekran tasarımı, design system | Ücretsiz |
| UI Animasyon | Rive | State machine, interactive anim | 9 dolar |
| Art Üretimi | Scenario Starter | Custom model + tutarlı portreler | 15 dolar |
| Görsel AI | Midjourney Basic | Stadyum, ofis, konsept art | 10 dolar |
| Müzik AI | Suno AI Pro | 10+ müzik parçası | 10 dolar |
| Ses AI | ElevenLabs Starter | Voiceover, commentator | 5 dolar |
| Pazar Analiz | Ludo.ai | Market research, Ludo Score | Ücretsiz |
| TOPLAM | Aylık Stack | Tek seferlik kalem yok | ~69 dolar/ay |

*Unity 6 Personal: 200 bin dolar/yıl altında gelir → ücretsiz.

**Geliştirme dönemi ek kalemleri [YENİ v4.0]:** Claude API geliştirme/test kullanımı (~20-50 dolar/ay) + geliştirme sunucusu VPS (~20-40 dolar/ay). Geliştirme dönemi gerçekçi toplam: ~110-160 dolar/ay.

## 14.2. Maliyet Karşılaştırma Analizi

| Kalem | Geleneksel Yöntem | AI-First Yöntem (v4.0) |
| --- | --- | --- |
| Geliştirme Süresi | 18-24 ay | 24-36 hafta (6-9 ay) |
| Asset Üretim (toplam) | 3.700-7.500 dolar | 800-2.000 dolar |
| Kodlama Süresi | Aylar | Haftalar (vibe coding) |
| UI/UX Tasarım | Hafta hafta manuel | Figma AI pipeline otomatik |
| Playtesting (sezon sim) | Manuel, haftalarca | 10.000 sim, saatler |
| Aylık Sabit Maliyet (geliştirme) | 200-500 dolar | ~110-160 dolar |
| Toplam Proje Maliyeti (lansmana kadar) | 80.000-150.000 dolar | 8.000-18.000 dolar + emek |

Not: v3.0'daki 5.000-10.000 dolar tahmini; sunucu altyapısı, LLM API ölçek maliyeti ve minimum UA bütçesi eklenerek 8.000-18.000 dolar bandına revize edilmiştir. Tasarruf oranı geleneksel yönteme göre yüzde 85-90 olarak korunur.

## 14.3. Operasyonel Maliyet Modeli — Ölçek Tablosu [YENİ — v4.0]

Lansman sonrası aylık işletme maliyetleri (kaydırılmış maç saatleri aktif, Redis cache yüzde 40-60 verimde):

| MAU | Nakama Sunucu | LLM API (cache'li) | TTS + Panorama | CDN + Replay | Aylık Toplam |
| --- | --- | --- | --- | --- | --- |
| 1.000 | 40-80 dolar | 20-40 dolar | 5-10 dolar | 5-10 dolar | 70-140 dolar |
| 10.000 | 120-250 dolar | 80-150 dolar | 15-30 dolar | 20-40 dolar | 235-470 dolar |
| 100.000 | 500-1.200 dolar | 400-900 dolar | 60-120 dolar | 100-250 dolar | 1.060-2.470 dolar |

- **Kritik bağımlılık:** Redis cache hedefi tutmazsa LLM kalemi 2-2,5 katına çıkar; cache hit oranı haftalık izlenen operasyonel KPI'dır.
- **Sunucu kalemi kapsamı [v4.1]:** Nakama + .NET sim worker düğümleri birlikte.
- **Managed alternatif [YENİ v4.1]:** Yönetilen Nakama (Heroic Cloud) ops yükünü sıfıra indirir, maliyet ~1,5-2 katı; soft launch self-host, 10K MAU üzerinde yeniden değerlendirme.
- **Panorama maliyeti** lig sayısıyla ölçeklenir (lig başına haftada ~0,03-0,08 dolar), kullanıcı sayısıyla değil.
- **Break-even bağlamı:** 10.000 MAU + yüzde 3 dönüşüm + 25-45 dolar ARPPU senaryosunda aylık gelir, işletme maliyetinin belirgin üzerindedir; model ölçekle birlikte marj korur.

## 14.4. UA ve Pazarlama Bütçesi [YENİ — v4.0]

Araç stack'i UA harcamasını içermez; indirme hedefleri bütçe senaryosuna bağlanmıştır:

| Senaryo | Bütçe | 30 Günlük İndirme Beklentisi |
| --- | --- | --- |
| Organik (0 UA) | 0 dolar | 5.000-15.000 |
| Soft Launch + Hedefli UA | 500-1.500 dolar | 15.000-25.000 |
| Global Launch UA | 3.000-8.000 dolar | 25.000-50.000 |

- **CPI varsayımı:** 0,25-0,60 dolar (Türkiye/LATAM ağırlıklı hedefleme).
- **Organik çarpanlar:** Replay klip paylaşımı (K-faktör hedefi 18.3), ASO A/B testleri, Discord/Reddit topluluk inşası, içerik üretici mikro-sponsorlukları (500-1.000 dolar ayrı kalem).
- **İlke:** UA harcaması, soft launch tutundurma metrikleri (D1/D7) hedefleri geçtikten SONRA ölçeklenir; tutmayan üründe UA yakılmaz.

# 15. FM26 MİMARİ REFERANSLARI

> **🏆 NEDEN FM26 REFERANS?**
> Sports Interactive'in Football Manager 26'sı, futbol yönetim oyunu mimarisinin mevcut en iyi örneğidir.
> Unity + UI Toolkit ile tamamen yeniden yazıldı — bizim tech stack'imizin kanıtı.
> Endüstrinin en kompleks management UI'sını başarıyla yönettiği kanıtlandı.
> Onların öğrendiği dersler bizim için gold mine — tekrar etmeden öğrenebiliriz.

## 15.1. Mimari Desen — Interrupt Abstraction Layer

FM26'nın ana mimari deseni:

- **C++ Game World:** Tüm simülasyon mantığı (match engine, ekonomi, transfer AI) C++ çekirdekte
- **C# UI Layer:** Unity UI Toolkit ile tüm arayüz C# ile
- **Data Store Pattern:** UI ve simülasyon arası tek noktalı veri iletişimi
- **Interrupt Abstraction:** UI event'leri simülasyonu durdurup kaldığı yerden devam ettirebiliyor

The Badge için adaptasyon: C# tabanlı deterministic match engine + UI Toolkit arasında aynı pattern. C++ kullanmayacağız ama mimari prensip aynı.

## 15.2. UI Pattern — Virtualized Lists

FM26'nın en büyük performans sırrı: 50.000+ oyuncu veritabanını pürüzsüz tarama.

- **Pool Cells:** Sadece ekranda görünen itemlar render edilir, diğerleri data halinde tutulur
- **Lazy Loading:** Oyuncu detayı sadece card açıldığında yüklenir
- **Search Index:** Önceden hesaplanmış arama indeksi (name, position, team, nation)
- **Progressive Loading:** İlk 50 oyuncu hemen → scroll ile lazy batch loading

## 15.3. UX Pattern — Tile/Card System

- **Tile View:** Kompakt görünüm, minimum bilgi (ad, pozisyon, rating)
- **Card Expansion:** Tap → detaylı kart (full stats, form curve, injury history)
- **Context Menu:** Long press → hızlı aksiyonlar (offer transfer, add to shortlist)
- **Comparison Mode:** Multi-select → yan yana karşılaştırma

## 15.4. Data Pattern — Externalized Parameters

- **JSON/YAML Config:** Tüm balance parametreleri oyun dışı dosyalarda
- **Hot-Reload:** Build etmeden parametre güncelleme
- **A/B Testing:** Farklı oyuncu grupları için farklı config dosyaları
- **Live Balance:** Server-side config → client'a update push (sezon geçişlerinde — bölüm 8.2 dondurma kuralına tabi)

## 15.5. Performance Pattern — Mobile-First

- **Düşük-Cihaz Profiling:** Her feature minimum cihazda (Snapdragon 665) test
- **Texture Atlas:** Tüm UI ikonları tek texture'da (draw call azaltma)
- **Addressables:** Dinamik asset loading, unused memory free
- **Object Pooling:** Match engine'de sürekli oluşturulan objeler pool'dan gelir

## 15.6. EA Sports FC UX Framework — Immersion Matrix

EA Sports FC UX ekibi ekranları 3 kategoriye ayırır:

- **Light UI:** Hızlı navigasyon, minimum cognitive load (ana menü, bildirimler)
- **Medium UI:** Yönetim görevleri, orta karmaşıklık (antrenman, transfer liste)
- **Heavy UI:** Tam analitik odak, kompleks veri (taktik board, detaylı stats)

Her kategoriye özel interaction pattern. The Badge için uygulama: UI Toolkit'te farklı UXML şablonları her kategori için.

# 16. VIBE CODING METODOLOJİSİ VE KALİTE KONTROL

## 16.1. "Vibe & Verify" Altın Kuralı

AI hızlı kod üretir, ama kod kalitesini garanti etmez. Bizim yaklaşımımız:

- **AI Üretir:** Claude Code kodu yazar, scaffold eder, test yazar
- **İnsan Doğrular:** Her PR'da senior review, her commit test coverage kontrolü
- **Testler Karar Verir:** Test geçmeyen AI kodu reddedilir, revize edilir

## 16.2. Vibe Coding 6 Altın Kuralı

- **Match Engine Deterministik Olmalı:** AI yazabilir ama her maç sonucu unit test ile doğrulanmalı. Aynı input = aynı output her zaman.
- **Claude Code'a 50+ Saat Yatırım Yap:** Vibe coding öğrenme eğrisi ~50 saat. İlk hafta yavaş → sonrası 3-5x hızlanma (PATRON iş akışı deneyimi doğrudan aktarılır).
- **AI'ın Her Kodunu Oku ve Anla:** Kara kutu kod kabul etme. Dev'in anlamadığı kodu maintain edemez, debug edemez.
- **Modül Bazlı Branch Stratejisi:** Her modül ayrı branch'te. AI kaynaklı regression'ları izole et, hızlı rollback yap.
- **80/20 Kuralı:** Core mekanikler (yüzde 20) elle kontrol, boilerplate (yüzde 80) AI'a bırak. Match engine core: insan. UI boilerplate: AI.
- **Haftalık Code Review:** Cuma günleri Claude'a tüm codebase analizi yaptır. Tutarsızlık, code smell, architectural drift tespit et.

## 16.3. Test Stratejisi

- **Unit Tests:** Her core sistem için minimum yüzde 80 coverage. Claude Code test yazıyor, dev review ediyor.
- **Integration Tests:** Modül arası etkileşim (ör: Match Engine + Tycoon gelir hesaplama).
- **Simulation Tests:** 10.000 sezon otomatik simülasyon → balance anomalisi tespiti.
- **Injection Test Seti [YENİ v4.0]:** Her whitelist LLM aksiyonu için prompt injection ve parametre sınır ihlali test senaryoları; CI'da her merge'te koşar (bölüm 11.7).
- **Replay Uyumluluk Testi [YENİ v4.0]:** Her balance/motor değişikliğinde önceki kayıtların uyumluluk davranışı doğrulanır (bölüm 11.9).
- **Device Tests:** Firebase Test Lab ile 20+ Android cihazda otomatik test.
- **Manual Playtesting:** Her fazın sonunda 5-10 kişi closed beta; FAZ 03'te ayrıca izlenebilirlik paneli (bölüm 5.6).

## 16.4. AI-Assisted Code Review Süreci

- **Adım 1:** Cuma günü Claude'a tüm src/ klasörü yükleniyor (geniş context).
- **Adım 2:** "Bu codebase'i analiz et. Hangi modüller FM26 pattern'inden sapmış?"
- **Adım 3:** "Hangi fonksiyonlarda code smell var? Refactor önerileri?"
- **Adım 4:** "Performans darboğazları nerede? Optimization stratejileri?"
- **Adım 5:** Claude raporu → Claude Code'a görev → AI refactor → İnsan review → Merge.

## 16.5. Prompt Engineering Disiplini

- **CLAUDE.md dosyası:** Her projede C# coding standards, Unity best practices, naming conventions, determinizm kuralları.
- **Context yükleme:** Her major task'ta ilgili mimari doküman Claude'a yüklenir.
- **Spesifik talimatlar:** "Bir fonksiyon yaz" yerine "IMatchEventHandler interface'ini implement eden, deterministic olan, unit-test edilebilir bir fonksiyon yaz".
- **Iteration:** İlk AI çıktısı yeterli değilse, spesifik feedback ile tekrar istenir.

# 17. MASTER ROADMAP v4.0 (AI-ACCELERATED) [GÜNCELLENDİ — v4.0]

> **⚡ 9 FAZ AI-ACCELERATED PIPELINE — v4.0 REVİZYONU**
> Toplam Süre: 24-36 hafta (~6-9 ay)
> v3.0'daki 18-28 haftalık takvim; Match Engine gerçekçi süresi (feel iterasyonu dahil), v4.0 sistemleri (Hikaye Motoru, Replay/Panorama, Kozmetik/Pass, FTUE) ve güvenlik katmanı eklenerek revize edilmiştir.
> Asset üretimi (FAZ 05) core geliştirmeyle paraleldir ve takvime süre eklemez.
> Her faz sonunda: çalışan artifact + Claude code review + progress sync.

## FAZ 00: Konsept Doğrulama (1-2 Hafta) — Kısmen Tamamlandı

**Amaç:** GDD'yi AI ile rafine et, pazarı doğrula, oynanabilir prototip yap.

- **Claude:** GDD kritik ✓ (bu doküman v4.0 revizyonu o sürecin çıktısıdır), monetizasyon simülasyonu, rakip App Store yorumları analizi
- **Ludo.ai:** Pazar boşluğu tespiti, Ludo Score kalibrasyonu
- **Claude Code:** Unity içinde transfer loop dikey prototipi [v4.1]
- **Claude (web search):** FM26, Top Eleven, OSM rekabetçi analiz

**Çıktı:** GDD v4.1 onaylı ✓ + Pazar analiz raporu + Oynanabilir prototip + 10 beta test geri bildirimi

## FAZ 01: Mimari ve Kurulum (1-2 Hafta)

**Amaç:** Unity projesi, folder structure, CI/CD, Git stratejisi.

- **Claude:** FM26 Interrupt Abstraction + Data Store pattern USM adaptasyonu; Command Bus (Tek Kapı) tasarımı
- **Claude Code:** Unity project scaffold + CLAUDE.md kurulumu
- **Arayüz iskeleti:** Core interface tanımları (IMatchEngine, ITransferMarket, ISquadManager, IIntentAction)
- **CI/CD:** GitHub Actions (GameCI) + Fastlane; injection test seti iskeleti

**Çıktı:** Çalışan Unity projesi + Modüler mimari + Command Bus iskeleti + CI/CD pipeline

## FAZ 02: UI/UX Tasarım (3-4 Hafta)

**Amaç:** Tüm ekranları Figma'da tasarla, Unity'ye otomatik import et.

- **Figma + AI Plugins:** 60+ ekran (FM26 UI referans; editörler, pass, Panorama, FTUE akışı dahil)
- **Rive:** Interactive UI animations, state machines
- **Midjourney:** UI konsept, ikonlar, mood board
- **Figma → Claude Code:** UXML/USS üretimi (FigmaToUnity yedek seçenek) [v4.1]

**Çıktı:** 60+ ekran Figma dosyası + Design System + Rive animasyonları + Unity UI import

## FAZ 03: Match Engine ve Feel (6-8 Hafta)

**Amaç:** Oyunun kalbini gerçekçi süreyle, izlenebilirlik dahil inşa etmek.

- **Motor Çekirdeği (4-6 hafta):** ME Spec v1.0 esas alınır + Claude Code C# implementasyon + fizik + Behaviour Tree/Utility AI + 1.000 maç determinizm validasyonu
- **Match Feel İterasyonu (2 hafta):** Highlight sistemi, tempo eğrisi, kamera vurguları (bölüm 5.6); 5 kişilik izlenebilirlik paneli ile 2 tur ayar
- **Chaos Engine:** Seed türevli deterministik şans katmanı + seviye konfigürasyonu

**Çıktı:** Deterministik, izlemesi keyifli match engine + highlight altyapısı + validasyon raporu

## FAZ 04: Core Modüller (5-7 Hafta)

**Amaç:** Kadro, Transfer, Tycoon, Online ve LLM katmanları.

- **Squad Management (1-2 hafta):** Serbest pozisyonlama, training, injury system
- **Transfer Market AI (1-2 hafta):** Valuation algoritması, negotiation logic, kontrat sistemi
- **Tycoon Economy (1-2 hafta):** Stadium, facilities, sponsor AI, balance testing
- **Online + LLM + Güvenlik (2-3 hafta):** Nakama backend + Claude API + Redis cache + 11.7 güvenlik katmanı + rekabet bütünlüğü temel örüntüleri

**Çıktı:** 5 çalışan core modül + Unit tests + Injection test seti yeşil + Performance benchmarks

## FAZ 05: Asset Üretimi (3-4 Hafta — FAZ 03-04 ile Paralel)

**Amaç:** AI ile görsel, ses, veri asset'lerinin toplu üretimi. Takvime ek süre yazmaz.

- **Scenario Custom Model:** 500+ kurgusal oyuncu portresi (bölüm 10.1 kuralları)
- **Midjourney:** 5 tier stadyum × 4 açı, ofis sahneleri, Panorama stüdyosu
- **Suno AI:** 10+ müzik parçası + Panorama jeneriği
- **ElevenLabs:** Asistan/spiker sesleri
- **Kurgusal Veri:** Bölgesel isim havuzları üretimi + karaliste taraması + kulüp/arma benzerlik denetimi
- **Kozmetik Temel Set:** Lansman forma desenleri, arma sembolleri, ofis dekor öğeleri

**Çıktı:** Komple asset kütüphanesi + onaylı kurgusal veri tabanı + kozmetik lansman seti

## FAZ 06: v4.0 Sistemleri (4-6 Hafta)

**Amaç:** Yeni katmanların inşası ve feature-flag arkasında hazırlanması.

- **Hikaye Motoru Lite + Hafıza (2 hafta):** Storyline mimarisi, 2 ark tipi, memory_facts + özetleyici
- **Replay + Klip Paylaşımı (1-2 hafta):** Kayıt/oynatma, klip export, deep link + watermark
- **Kozmetik + Sezon Pası Altyapısı (1-2 hafta):** Editörler, envanter, pass ilerleme sistemi
- **FTUE İmplementasyonu (1 hafta):** "Enkazı Devral" akışı + funnel telemetrisi + veteran atlaması

**Çıktı:** 4 yeni sistem feature-flag arkasında çalışır durumda + FTUE funnel canlı

## FAZ 07: Entegrasyon, Test ve QA (3-4 Hafta)

**Amaç:** Sistem birleştirme, performans, kapsamlı test ve closed beta.

- **Claude Code:** Cross-module entegrasyon, bug fixing
- **Test Paketi:** 10.000 sezon simülasyonu, injection seti, replay uyumluluk, Firebase Test Lab (20+ cihaz)
- **Unity Profiler + Claude:** Darboğaz analizi, mobil optimizasyon
- **Closed Beta:** 20-50 kişi; FTUE funnel ve izlenebilirlik metrikleri ana kabul kriterleri
- **Save/Load + Lokalizasyon:** 5 dil

**Çıktı:** Stabil release candidate + Balance raporu + Beta sonuç raporu

## FAZ 08: Lansman ve Marketing (2-4 Hafta)

**Amaç:** Store listing, ASO, trailer, kademeli lansman.

- **Claude:** Store açıklamaları, ASO keyword analizi, press kit
- **Midjourney:** Store screenshots, feature graphic, icon
- **Suno/Runway:** 60 sn trailer + müzik; **ElevenLabs:** trailer voiceover
- **Soft Launch:** Türkiye + 2 pazar; D1/D7 kapıları geçilince UA ölçeklenir (bölüm 14.4)
- **Global Launch:** Kademeli devreye alma planı aktif (bölüm 1.5)

**Çıktı:** Store'da yayında olan oyun + Marketing materyalleri + Community kanalları

## POST-LAUNCH: Live Operations (Sürekli)

- **Lansman + 2 hafta:** Sezon Pası S1 başlangıcı; editör içe/dışa aktarma arayüzü
- **Lansman + 4 hafta:** Haftanın Panoraması devrede
- **Lansman + 6 hafta:** Hikaye Motoru tam ark seti
- **Haftalık:** Balance patch (sezon geçişi kuralına uygun), bug fix, cache hit ve bütünlük raporu
- **Aylık:** Yeni lig temaları, yeni personel, yeni kozmetik setleri
- **Sezonluk:** 8 haftalık pass sezonları + tematik etkinlikler (yılbaşı, kıta kupaları dönemi)
- **Community:** Discord, Reddit, içerik üretici programı; klip yarışmaları (K-faktör beslemesi)

# 18. RİSK YÖNETİMİ VE BAŞARI METRİKLERİ [GÜNCELLENDİ — v4.0]

## 18.1. Teknik Riskler

| Risk | Olasılık | Etki | Mitigation |
| --- | --- | --- | --- |
| AI kodu determinism bozması | Yüksek | Kritik | Her merge'te 1.000 maç sim + CI kapısı |
| LLM prompt injection / istismar | Orta | Yüksek | Tek Kapı + whitelist + injection test seti (11.7) |
| LLM API maliyet fırlaması | Orta | Yüksek | Redis cache + usage limit + haftalık hit-rate KPI |
| Mobil performans | Orta | Yüksek | Minimum cihaz profiling erken |
| Replay-motor versiyon uyumsuzluğu | Orta | Orta | Config hash + sezon içi motor dondurma (8.2, 11.9) |
| Backend scalability (maç saati zirvesi) | Orta | Yüksek | Kaydırılmış maç saatleri + sim kuyruğu (6.6) |
| UI toolkit öğrenme eğrisi | Yüksek | Orta | FM26 UI patterns referans |

## 18.2. Ticari ve Hukuki Riskler

| Risk | Olasılık | Etki | Mitigation |
| --- | --- | --- | --- |
| Lisans/IP ihlali | Düşük (mitigasyon sonrası) | Kritik | Kurgusal evren + benzerlik denetimi + paket barındırmama (bölüm 10) |
| Pay-to-Win algısı | Orta | Yüksek | Kozmetik gelir ağırlığı + kalıcı kazanım yolları + community izleme |
| Collusion / Elo manipülasyonu | Orta | Orta | Örüntü tespiti + ceza merdiveni (6.5) |
| Kapsam genişlemesi (v4.0 sistemleri) | Yüksek | Orta | Feature-flag mimarisi + kademeli devreye alma (1.5) |
| Rakip oyun lansmanı | Orta | Orta | USM nostaljisi + tycoon + hikaye farklılaştırıcılarını güçlendir |
| ASO etkisiz kalması | Orta | Orta | A/B store listing + 2 haftada bir keyword güncelleme |
| Soft launch başarısızlığı | Orta | Yüksek | D1/D7 kapıları geçilmeden UA ölçeklenmez (14.4) |

## 18.3. Başarı Metrikleri (KPI)

### Lansman Öncesi (6 ay)

- **Beta signup:** 5.000+ e-posta
- **Discord members:** 500+
- **Store pre-registration:** 10.000+ Google Play

### Lansman (İlk 30 gün)

- **İndirme:** Organik senaryo 5.000-15.000 / UA'lı senaryo 25.000-50.000 (bölüm 14.4)
- **D1 Retention:** yüzde 40 üzeri (öncü gösterge: FTUE funnel hedefleri, bölüm 9.5)
- **D7 Retention:** yüzde 20 üzeri
- **Crash-free rate:** yüzde 99 üzeri
- **Store rating:** 4.2/5 üzeri

### Büyüme ve Paylaşım [YENİ v4.0]

- **Klip paylaşım oranı:** MAU'nun yüzde 8'i ayda en az 1 klip paylaşır
- **K-faktör:** Paylaşım başına install 0,15 üzeri
- **Panorama izlenme:** Aktif lig üyelerinin yüzde 50'si haftalık bölümü izler

### İlk Yıl

- **MAU:** 100.000+
- **ARPU:** 1,50-3,00 dolar
- **Conversion rate (F2P → Paying):** yüzde 3 üzeri
- **ARPPU:** 25-45 dolar (mağaza kesintisi sonrası net) [v4.1]
- **Sezon Pası dönüşümü:** MAU'nun yüzde 5-8'i [YENİ v4.0]
- **Kozmetik attach rate:** Ödeme yapanların yüzde 35'i en az 1 kozmetik alır [YENİ v4.0]
- **D30 retention:** yüzde 10 üzeri
- **Redis cache hit oranı:** yüzde 50 üzeri (operasyonel KPI) [YENİ v4.0]
- **LLM öneri kabul oranı:** yüzde 60 üzeri — Mod B isabet göstergesi (CB Spec 9.2) [YENİ v4.1]

# 19. EKLER — HIZLI REFERANS VE KARAR GÜNLÜĞÜ

## 19.1. Karar Günlüğü (v1.0 → v4.0)

| Versiyon | Tarih | Stratejik Kararlar |
| --- | --- | --- |
| v1.0 | Kasım 2025 | Temel GDD: 4 modül, FAZ 1-4, Unity 6, Nakama, LLM entegrasyonu, AI Staff Market |
| v2.0 | Aralık 2025 | Chaos Engine ayarlanabilirliği + LLM Redis cache + Pay-to-Progress-Faster monetizasyon (6 kategori) + FAZ 5 |
| v3.0 | Nisan 2026 | AI-First Pipeline + 8 Faz roadmap + Cursor/Claude/Copilot stack + FM26 mimari referansları + Vibe & Verify + AI asset stratejisi |
| v4.0 | Temmuz 2026 | 5 kritik boşluk kapatıldı: Lisans stratejisi (kurgusal evren + topluluk editörü), gerçekçi takvim (24-36 hafta), LLM güvenlik katmanı (Tek Kapı + whitelist), tam maliyet modeli (operasyonel + UA), FTUE tasarımı. 3 yeni sistem: Sezon Hikaye Motoru + Asistan Hafızası, Kozmetik Ekonomi + Sezon Pası, Replay Paylaşımı + Haftanın Panoraması. Rekabet bütünlüğü + kademeli devreye alma planı eklendi. v2.0 orijinal roadmap'i Master Roadmap'e katlandı. |
| v4.1 | Temmuz 2026 | Denetim revizyonu: Cursor → Claude Code (CLAUDE.md, 2 katmanlı pipeline). Sunucu topolojisi netleşti: .NET C# sim servisi + Nakama (tek-kaynak doğrulama garantisi). PC iptal — salt mobil. Hub/stadyum 2.5D prerender; Meshy, Spine Pro, Perplexity çıkarıldı, Copilot opsiyonel, GameCI'ya geçiş → aylık stack 109 → ~69 dolar. Min spec: iOS 16 / Android 9. Bölgesel fiyatlama ilkesi + net-ARPPU tanımı. 11.3 performans hedefi ME Spec LOD modeliyle revize. Yeni KPI: LLM öneri kabul oranı. Ekler: Match Engine Spec v1.0 + Command Bus & Güvenlik Spec v1.0 yayımlandı. |

## 19.2. Hızlı Tool Stack Referansı

- **Aylık Araç Toplamı:** ~69 dolar/ay (tam stack; tek seferlik kalem yok)
- **Geliştirme Dönemi Gerçekçi Toplam:** ~110-160 dolar/ay (Claude API test + geliştirme VPS dahil)
- **Minimum Başlangıç:** Claude Pro 20 (Claude Code dahil) + Figma 0 = 20 dolar/ay
- **Operasyonel (lansman sonrası):** Ölçek tablosuna bakınız (bölüm 14.3)

## 19.3. Kritik Başarı Faktörleri (CSF)

- **Match Engine Kalitesi:** Oyunun kalbi. Deterministik + dengeli + İZLEMESİ keyifli (feel iterasyonu bunun için var).
- **FTUE Funnel'ı:** İlk 15 dakika D1'i belirler; funnel hedefleri tutmadan UA açılmaz.
- **UI/UX Polish:** FM26 kalitesinde UI; 50.000+ oyuncu listesi pürüzsüz taranmalı.
- **Monetizasyon Algısı:** Pay-to-Win algısı oluşmamalı; kozmetik hat bu algıya karşı en güçlü tampon.
- **Online Performans ve Bütünlük:** Maç saati throughput'u + collusion tespiti birlikte çalışmalı.
- **LLM Maliyet Kontrolü:** Redis cache hit yüzde 50+; Hikaye Motoru payı yüzde 20 tavanında.
- **Organik Büyüme Motoru:** Klip paylaşımı K-faktörü; her sezon klip yarışmalarıyla beslenir.
- **Community Building:** Discord + Reddit + içerik üreticiler; early adopters'ı evangelistlere çevir.

## 19.4. Sıradaki Adımlar (v4.0 Sonrası)

- Claude Pro + GitHub kurulumu; CLAUDE.md dosyası (Unity C# conventions + determinizm ve Tek Kapı kuralları)
- Unity 6 LTS kurulumu + GitHub repo + CI iskeleti
- FAZ 00 kalanları: Ludo.ai pazar raporu + Claude Code transfer loop prototipi + 10 beta geri bildirimi
- ElevenLabs Türkçe spiker kalite testi (Panorama taahhüdünden önce)
- Kurgusal evren üretim hattı: bölgesel isim havuzları + ünlü oyuncu karalistesi + kulüp/arma benzerlik denetim listesi
- Command Bus (Tek Kapı) + IntentAction whitelist şeması tasarım oturumu (Claude ile, FAZ 01 girdisi)
- Kozmetik lansman seti sanat yönergesi (Scenario custom model eğitim planı)

> **ONAY DURUMU — GDD v4.1**
> Bu belge, THE BADGE projesinin "Anayasası" olarak v4.0'ın yerini alır. Match Engine Spec v1.0 ve Command Bus & Güvenlik Spec v1.0 bağlayıcı ekleridir. Tüm geliştirme kararları, kod mimarisi, AI tool kullanımı, içerik/lisans politikası ve asset üretimi bu dokümanla uyumlu olacaktır.
> — AI-First Edition · v4.1 —
