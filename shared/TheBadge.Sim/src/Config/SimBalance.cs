namespace TheBadge.Sim.Config
{
    /// <summary>
    /// balance/sim.balance.json'ın çekirdek POCO'su. ÇEKİRDEK JSON PARSE ETMEZ — bağımlılıksızlık
    /// kuralı (CLAUDE.md): host (sunucu/Unity/Checks) kendi JSON aracıyla doldurur; alan adları
    /// JSON anahtarlarıyla BİREBİRDİR (System.Text.Json IncludeFields / Unity JsonUtility uyumu —
    /// [Serializable] bu yüzden var, Unity bağımlılığı değildir). config_hash İÇİ (ME Spec 3.3).
    /// Varsayılanlar 0: balance yüklenmemişse hesap GÖRÜNÜR bozulur (sessiz sapma yerine).
    /// </summary>
    [System.Serializable]
    public sealed class SimBalance
    {
        public AttributeCfg attribute = new AttributeCfg();
        public MoveCfg move = new MoveCfg();
        public PhysicsCfg physics = new PhysicsCfg();
        public DuelCfg duel = new DuelCfg();
        public PassCfg pass = new PassCfg();
        public AnchorCfg anchor = new AnchorCfg();
        public PossessionCfg possession = new PossessionCfg();
        public UtilityCfg utility = new UtilityCfg();
        public OffballCfg offball = new OffballCfg();
        public XtCfg xt = new XtCfg();
        public ChaosCfg chaos = new ChaosCfg();
        public GkCfg gk = new GkCfg();
        public ShotExecCfg shotExec = new ShotExecCfg();
        public ShotCfg shot = new ShotCfg();
        public RefereeCfg referee = new RefereeCfg();
        public StaminaCfg stamina = new StaminaCfg();
        public InjuryCfg injury = new InjuryCfg();
        public MomentumCfg momentum = new MomentumCfg();
        /// <summary>VAR — ME Spec 11.4 [KALİBRE]. JSON anahtarı "var"; C# anahtar sözcüğü
        /// olduğu için @ ile yazılır (ad JSON'da birebir "var" kalır).</summary>
        public VarCfg @var = new VarCfg();

        /// <summary>Hava ve zemin — ME Spec 12.4 [KALİBRE]. Tüm çarpanlar burada; kodda sabit yok.</summary>
        public HavaCfg hava = new HavaCfg();

        /// <summary>Highlight puanlama — ME Spec 15.3 [KALİBRE].</summary>
        public HighlightCfg highlight = new HighlightCfg();

        /// <summary>Uzun top / temizleme / kaleci dağıtımı — ME 7.2 (LongSwitch, ClearBall) +
        /// ME 9.4 (KısaAçıl/UzunDegaj/ElleAt) [KALİBRE] (M16-D).</summary>
        public LongballCfg longball = new LongballCfg();

        [System.Serializable]
        public sealed class LongballCfg
        {
            public double minMesafeM;        // bu mesafenin altı kısa pastır, uzun top aday olmaz
            public double hizMS;             // uçuş hızı (orta gibi balistik — ME 8.3)
            public double sigmaTabanDeg;     // nişan sapması tabanı (Passing daraltır)
            public double tehditCarpan;      // uzun topun tehdit ağırlığı (kesin kontrol yok → iskonto)
            public double riskTaban;         // iniş rekabeti taban kaybı
            public double havaRiskPerRakip;  // iniş noktasındaki rakip başına ek kayıp
            public double clearBolgeM;       // kendi kaleye bu mesafeden yakınken TEMİZLE aday olur
            public double clearIleriM;       // temizlemenin ileri menzili
            public double clearYanM;         // taca doğru yanal itme
            public double clearSigmaDeg;     // temizleme sapması (kontrollü vuruş değildir)
            public double clearKayipTaban;   // temizlemenin kabul edilmiş kayıp oranı
            public double clearPresBonus;    // baskı başına temizleme iştahı
            public double gkElleAtMaxM;      // elle atışın menzili (ME 9.4 ElleAt)
            public double gkElleSigmaCarpan; // elle atış isabet çarpanı (<1: elle atış isabetlidir)
            public double gkKisaBias;        // Kicking düşük kalecinin kısa oynama eğilimi (9.4)
            public double gkDegajSigmaDeg;   // degaj sapması (Kicking daraltır — 9.4)
            public double gkPresDegajBias;   // pres altında degaj bias'ı (9.4: +0,25)
            public double gkDegajRisk;       // degajın kabul edilmiş kayıp oranı
            public int gkKisaN;              // kısa açılış aday sayısı
        }

        /// <summary>LOD bütçeleri + LOD 2 güç bileşimi — ME Spec 16.1 [KALİBRE].
        /// Regresyon KATSAYILARI burada DEĞİL, üretilmiş `balance/sim.lod2.json` dosyasındadır.</summary>
        public LodCfg lod = new LodCfg();
        public CanliOlasilikCfg canliOlasilik = new CanliOlasilikCfg();

        /// <summary>CANLI kazanma olasılığı [KALİBRE] — `LiveWinProb`. Katsayılar motorun KENDİ
        /// davranışından oturtuldu (49 eşleşme × 300 maç, log-lineer en küçük kareler, R² = 0,977);
        /// uydurma değil ÖLÇÜLMÜŞ değerlerdir. Yeniden oturtmak için: `-- fit-winprob`.</summary>
        [System.Serializable]
        public sealed class CanliOlasilikCfg
        {
            /// <summary>Güç farkı sıfırken takım başına 90 dakikalık beklenen gol.</summary>
            public double lambdaTaban;
            /// <summary>Güç farkının 1 biriminin gol oranına üstel etkisi.</summary>
            public double gucKatsayisi;
            /// <summary>Poisson toplamının kesme noktası (kalan sürede takım başına en çok kaç gol).</summary>
            public int maxEkGol;
            /// <summary>Taktik kadranlarının gol oranına ÜSTEL etkisi (5G S2-B ölçümü).</summary>
            public TaktikCfg taktik = new TaktikCfg();

            /// <summary>Her kadran için İKİ katsayı: `*Kendi` kadranı ÇEVİREN takımın gol oranını,
            /// `*Rakip` KARŞI takımın gol oranını nasıl kaydırır. İkisi ayrı olmak zorunda —
            /// ölçüm `pres`in kendi golünü hiç artırmadığını ama rakibinkini %49 artırdığını
            /// gösterdi; tek katsayılı bir model bunu taşıyamazdı.
            ///
            /// ANA ETKİ MODELİ (bilinçli sınır): kadranlar TOPLANABİLİR varsayılıyor, oysa ölçüm
            /// etkileşimin gerçek olduğunu gösterdi (dördü birden hücuma alınca sonuç tek tek
            /// toplamından KÖTÜ). Kapı bileşik ayarları da içeren bir popülasyonda koşuyor;
            /// ana etkiler yetmezse ORADA düşer, sessizce geçmez.</summary>
            [System.Serializable]
            public sealed class TaktikCfg
            {
                public double mentaliteKendi, mentaliteRakip;
                public double tempoKendi, tempoRakip;
                public double presKendi, presRakip;
                public double hatKendi, hatRakip;
                /// <summary>AŞIRI UÇ CEZASI — kadran karelerinin toplamıyla çarpılır.
                /// Ana etkiler TEK BAŞINA yetmedi: dördü birden uca çekilince model gerçeği
                /// 18-22 puan ıskalıyordu (`tamHücum` %46,9 derken gerçek %33,0). Bu terimle
                /// en büyük sapma 0,225 → 0,080. Anlamı futbolca okunur: her kadranı uca çeken
                /// dengesiz bir kurulum kendine az fayda, rakibe çok alan verir.</summary>
                public double asiriUcKendi, asiriUcRakip;
            }
        }

        [System.Serializable]
        public sealed class LodCfg
        {
            public BudgetCfg cpuBudgetSn = new BudgetCfg();
            public GucCfg guc = new GucCfg();

            [System.Serializable]
            public sealed class BudgetCfg { public double lod0, lod1, lod2; }

            /// <summary>Takım gücü bileşimi (LOD 2 girdisi). Kaleci ayrı bileşen: nitelik seti
            /// saha oyuncusuyla ortak değildir.</summary>
            [System.Serializable]
            public sealed class GucCfg
            {
                public double kaleciPayi;
                public KaleciCfg kaleci = new KaleciCfg();
                public SahaCfg sahaOyuncusu = new SahaCfg();

                [System.Serializable]
                public sealed class KaleciCfg { public double reflexes, handling, oneOnOne, aerialCommand; }
                [System.Serializable]
                public sealed class SahaCfg
                { public double passing, finishing, tackling, pace, positioning, decisions, firstTouch, strength; }
            }
        }

        [System.Serializable]
        public sealed class HighlightCfg
        {
            public WCfg w = new WCfg();
            public double esik;              // H > bu → zaman çizelgesi işareti (GDD 5.6)
            public WinProbCfg winprob = new WinProbCfg();
            public double buyukSansXg;       // bu xG üstü şut "büyük şans" sayılır (Flags.BigChance)
            public double uzakGolMesafeM;    // bu mesafeden fazlası "uzak gol" nadirliğine girer
            public int zamanCizelgesiIsaret;   // [KALİBRE] zaman çizelgesine basılan işaret sayısı (ME 17.5 ayar sahası)
            public NadirlikCfg nadirlik = new NadirlikCfg();

            /// <summary>H = 0,35×xG salınımı + 0,20×geç dakika + 0,20×skor etkisi
            /// + 0,15×nadirlik + 0,10×hikaye ilgisi (ME 15.3).</summary>
            [System.Serializable]
            public sealed class WCfg
            { public double xgSalinim, gecDakika, skorEtkisi, nadirlik, hikayeIlgisi; }

            /// <summary>Kayan WinProb modeli — spec formül vermez, ME 15.3 yalnız "kayan model" der.
            /// p = lojistik(k × gol_farkı / √(kalan_dk / 90)).</summary>
            [System.Serializable]
            public sealed class WinProbCfg { public double k, minKalanDk; }

            /// <summary>Nadirlik taban tablosu — ME 15.3 ("event tipi taban tablosu:
            /// röveşata sınıfı vole, 30 m gol, penaltı kurtarışı yüksek"). 0-1 bandı.</summary>
            [System.Serializable]
            public sealed class NadirlikCfg
            {
                public double gol, uzakGol, kafaGol, penaltiGol, kacanBuyukSans;
                public double penaltiKurtaris, kurtaris, celme, isabetliSut, blokluSut;
                public double penaltiVerildi, kirmiziKart, sariKart, varInceleme, varKarari;
                public double sakatlik, asist, diger;
            }
        }

        [System.Serializable]
        public sealed class HavaCfg
        {
            public KosulCfg yagmur = new KosulCfg();
            public KosulCfg kar = new KosulCfg();
            public KosulCfg sicak = new KosulCfg();
            /// <summary>Rüzgar sapma katsayısı — ME 12.4: sapma = rüzgar_hızı × k_w × uçuş_süresi.
            /// Spec sayı vermez [KALİBRE]. 0,15 aerodinamikten türetildi: top (0,43 kg, A≈0,038 m²,
            /// Cd≈0,25) 16 m/sn yan rüzgarda ≈3,4 m/sn² yanal ivme görür → 1,6 sn'lik bir kornerde
            /// ≈4 m sapma; doğrusal modelde bunun karşılığı k≈0,15'tir (M13 ölçümü: 16 m/sn → 3,9 m).</summary>
            public double ruzgarK;
            public ZeminKotuCfg zeminKotu = new ZeminKotuCfg();
            public ZeminIyiCfg zeminIyi = new ZeminIyiCfg();

            [System.Serializable]
            public sealed class KosulCfg
            {
                public double passingDelta, firstTouchDelta, visionDelta;
                public double aRoll, sekmeE, topHiziCarpan, vMaxCarpan, sakatlikCarpan, staminaCarpan;
            }
            [System.Serializable]
            public sealed class ZeminKotuCfg { public double firstTouchDelta, sekmePertDeg, sakatlikCarpan; }
            [System.Serializable]
            public sealed class ZeminIyiCfg { public double passingDelta; }
        }

        [System.Serializable]
        public sealed class VarCfg
        {
            public KalirCfg sahaKarariKalirOran = new KalirCfg();
            public double beklemeSnMin, beklemeSnMax;

            [System.Serializable]
            public sealed class KalirCfg { public double dusuk, orta, yuksek; }
        }

        /// <summary>Stamina — ME Spec 12.1 [KALİBRE].</summary>
        [System.Serializable]
        public sealed class StaminaCfg
        {
            public double kE;                    // drenaj katsayısı
            public double deadBallRecoveryPerSn; // ölü topta toparlanma (+2/sn)
            public double devreArasi;            // devre arası toparlanma (+150)
            public int yorgunlukEsik;            // altında DECISION sigma +%20 (12.1)
            public double presEkMaliyet;         // pres yapan ajanın ek drenajı
        }

        /// <summary>Sakatlık — ME Spec 12.2 [KALİBRE].</summary>
        [System.Serializable]
        public sealed class InjuryCfg
        {
            public double[] siddetDagilimi = new double[0]; // Hafif/Küçük/Orta/Ağır
            public double[] macBasiBandi = new double[0];   // kalibrasyon bandı (doğrulama)
            public double sertMudahaleEsik;                 // s > bu → sakatlık çekilişi (11.3)
            public double pTabanMudahale;                   // müdahale kaynaklı p_taban
            public double pTabanSprint;                     // yorgunken sprint kaynaklı p_taban
        }

        /// <summary>Momentum — ME Spec 12.3 [KALİBRE].</summary>
        [System.Serializable]
        public sealed class MomentumCfg
        {
            public int golDelta;                  // gol ±4
            public double sonumPerDk;             // dakikada 0'a sönüm
            public double decisionSigmaEtkiYuzde; // karar gürültüsüne ±% etki
            public int moralPuanTavan;            // M_moral tavanı (±5 puan)
            public double baskiNisanCarpan;       // kritik dakika baskısının nişana etkisi
        }
        public SetPieceCfg setpiece = new SetPieceCfg();
        public ExtraTimeCfg extraTime = new ExtraTimeCfg();

        /// <summary>Hakem eşikleri — ME Spec 11.2 [KALİBRE].</summary>
        [System.Serializable]
        public sealed class RefereeCfg
        {
            public double foulEsikTaban;      // s eşiği tabanı (0,30)
            public double strictnessCarpan;   // eşik kayması: (Strictness−50) × bu
            public double griBantOrta;        // ± gri bant genişliği
            public double sariEsik, kirmiziEsik;
            public double cezaSahasiIhtiyatCarpan; // kendi ceza sahasında şiddet skoru çarpanı
            public double avantajXtEsik;      // avantaj için mağdurun hücresinin asgari xT'si (ME 11.2 tehdit koşulu)
            public double sariSonrasiIhtiyat; // sarı görmüş oyuncunun şiddet skoru iskontosu (M16-E)

            // --- ŞİDDET FORMÜLÜ (ME 11.2) — K12'ye kadar KODA GÖMÜLÜYDÜ.
            // s = marginAgirlik×margin_açığı + hizAgirlik×hız + arkadanAgirlik×arkadan_mı
            // margin_açığı = clamp((taşıyıcının kaçış bileşiği − müdahale edenin bileşiği)/marginBolen, 0, 1)
            //
            // `marginBolen` bu modelin ROL DUYARLILIĞINI belirler ve tam olarak bu yüzden
            // [KALİBRE] olması gerekiyordu: bileşikler bir FARK olarak giriyor, dolayısıyla
            // kadro profili keskinleştikçe (gerçek defans / gerçek forvet) fark sistematik
            // büyüyor. M4/M5 kalibrasyonu rol ayrımı OLMAYAN sentetik kadrolarla yapıldığı için
            // 50 bölen orada ~0 fark üretiyordu ve sorun görünmüyordu (K11 bulgusu).
            public double marginBolen;
            public double marginAgirlik, hizAgirlik, arkadanAgirlik;
            public double agresifEsik, agresifEk;   // Eff(Aggression) > eşik → +ek
            public double motivasyonEk;             // ME 14.3 "Ateşle"/"Sakinleştir" şiddet deltası
        }

        /// <summary>Duran toplar — ME Spec 10 [KALİBRE].</summary>
        [System.Serializable]
        public sealed class SetPieceCfg
        {
            public PenaltyCfg penalty = new PenaltyCfg();
            public double[] cornerGolBandi = new double[0]; // kalibrasyon bandı (doğrulama, ME 17.2)
            public double frikikDirektEsikM;
            public double ortaKosuYaricapM;      // ortayı KARŞILAYABİLECEK arkadaşın hedefe uzaklığı (m)
            public double ortaHiziMS;            // açık oyun ortasının ilk hızı
            public double ortaHedefDerinlikM;    // ortanın kale çizgisine uzaklığı (hedef bölge)
            public double kornerOrtaHiziMS;      // korner ortası ilk hızı (M4 ekleme)
            public double kornerHedefDerinlikM;  // ortanın kaleye uzaklığı (hedef bölge)
            public double havaTopuYaricapM;      // hava topu düellosu yarıçapı
            public double havaTopuYukseklikM;    // bu yüksekliğin altına inince düello çözülür (ME 8.3)
            public double korneriGozeAlmaOran;   // baskı altında kendi kutusunda topu dışarı atma oranı
            public double uzaklastirmaHizMS;     // savunmanın uzaklaştırma hızı
            public int hazirlikTicks;            // duran top hazırlığı (sıkıştırılmış çözüm, ME 3.4)
            public double santraDaireM;          // santrada rakibin çekilmek zorunda olduğu daire yarıçapı (ME 4.1 DEAD_BALL)
            public int santraHazirlikTicks;      // santra beklemesi: diziliş toparlanana dek alım kilidi (ME 4.1)

            [System.Serializable]
            public sealed class PenaltyCfg
            {
                public double dogruTahmin, yanlisTahmin, ortaOrta, direk, hedefOrtalama;
            }
        }

        /// <summary>Uzatma süresi — ME Spec 3.4 formülü [KALİBRE].</summary>
        [System.Serializable]
        public sealed class ExtraTimeCfg
        {
            public double durakCarpan, kartCarpan, golCarpan;
            public int minDk, maxDk;
        }

        /// <summary>Kaleci kurtarış modeli — ME Spec 9.1-9.2 [KALİBRE].</summary>
        [System.Serializable]
        public sealed class GkCfg
        {
            public double tReactBase, tReactPerReflexEksik;   // t_react = taban + (100−Reflexes)×bu
            public double reachBase, reachAgilityFactor;      // erişim = taban + Agility/100×faktör (m)
            public double logisticSlope;                      // P_save = lojistik(slope × marj)
            public double saveClampMin, saveClampMax;
            public double dalisSureCarpan;                    // t_traverse = mesafe/(erişim/bu)
            public double celmeDirekPayiMm;     // çelmenin direk dışına taşma payı (mm, ME 9.2)
            public double cildirmaAcisiDeg;                   // çeldide sapma açısı
            public double derinlikTaban, derinlikPerM, derinlikMax; // 9.1 pozisyon derinliği
            public double altiPasYaricapM;      // kalecinin gol sahasında topa kapanma yarıçapı (ME 9.4)
            public double altiPasDerinlikM;     // gol sahası derinliği (m)
            public double altiPasYariGenislikM; // gol sahası yarı genişliği (m)
            public double havaHakimiyetCarpani; // kalecinin hava topu yarıçapı çarpanı (ME 9.3)
            public double cikisMesafeM;         // kalecinin serbest topa çıktığı derinlik (m, ME 9.3)
            public double cikisGenislikM;       // çıkış bölgesinin yarı genişliği (m)
            public double yakinMesafeM;         // 1v1 kapatma etkisinin başladığı mesafe (ME 9.3)
            public double yakinKapatmaKatsayi;  // OneOnOne'ın marja katkısı (sn)
            public double tutmaBoleni;       // tutuş kontrolü: Handling/bu (çelme payının tersi, M16-E)
            // ME 9.1 açıortay hatası + 9.2 direk bandı (M16-G)
            public double posHataTabanM;     // sigma_pos tabanı (spec: 0,9 m)
            public double posHataBolen;      // Positioning böleni (spec: 120)
            public int posHataYenilemeTicks; // hata çekilişinin yenilenme aralığı (titreme önlenir)
            public double direkBandiMm;      // kesişim direğe bu kadar yakınsa direk (spec: 120 mm)
        }

        /// <summary>Şut yürütme — ME 6.4 kompoziti + 8.3 (M3 ekleme) [KALİBRE].</summary>
        [System.Serializable]
        public sealed class ShotExecCfg
        {
            public double sutMaxMesafeM;       // karar: bu mesafenin dışında şut aday olmaz
            public double sutYariMesafeM;      // mesafe tehdidinin yarıya düştüğü mesafe (m, rasyonel çekirdek)
            public double sutHiziMS;
            public double sutSigmaTabanDeg;    // nişan sapması AÇISAL (derece) — mesafeyle büyür
            public double kafaSigmaCarpani;    // kafa vuruşunda nişan sapması çarpanı (ME 6.4)
            public double nisanDirekOrani;     // nişan noktası: direk yarı genişliğinin bu oranı
            public double blokOlasilik;        // koridorda savunucu varsa blok olasılığı (ME 15.1)
            // Yoğunluk kanalları — M16-F derin blok ödülü (ME 6.4/9.2 ruhu): sıkışık kutu
            // şutu hem bloklar hem bozar; bu iki alan yokken savunucu SAYISI çözüme girmiyordu
            public double blokEkSavunucu;      // koridordaki ek savunucu başına blok olasılığı artışı
            public double blokOlasilikMax;     // blok olasılığı tavanı
            public double presSigmaKisiBasi;   // şutçuya yakın (pass.presYaricapM) rakip başına sigma çarpım artışı
            // Nişan tarafı seçimi — kalecinin boş bıraktığı taraf (M16-G, ME 6.4 yerleşim tarafı)
            public double nisanDogruTaban;     // sıfır yetenekte doğru tarafı seçme olasılığı
            public double nisanDogruSpan;      // şut kompoziti başına ek doğru-seçim olasılığı
        }

        /// <summary>xG kayıt modeli — ME Spec 15.2 [KALİBRE]. Yalnız KAYIT/analiz — sonuç
        /// üretimine girmez (17.2 tutarlılık kapısı ayrıca denetler).</summary>
        [System.Serializable]
        public sealed class ShotCfg
        {
            public XgCfg xg = new XgCfg();
            [System.Serializable]
            public sealed class XgCfg
            { public double b0, bLnDist, bAngle, bPres, bHeader, bBigChance, bOneOnOne; }
        }

        /// <summary>Efektif nitelik çarpanları — ME Spec 6.2 [KALİBRE].</summary>
        [System.Serializable]
        public sealed class AttributeCfg
        {
            public double kondisyonTaban;
            public double kondisyonKuvvet;
            public double kondisyonUs;
            public double moralCarpanPerMomentum;
        }

        /// <summary>Ajan kinematiği — ME Spec 8.1 [KALİBRE].</summary>
        [System.Serializable]
        public sealed class MoveCfg
        {
            public double vMaxBase, vMaxPaceSpan;        // v_max = base + span × Pace/100 (m/sn)
            public double aMaxBase, aMaxAccelSpan;       // a_max = base + span × Accel/100 (m/sn²)
            public double dribbleCarpanBase, dribbleCarpanPerPuan; // topla v_max çarpanı (ME 8.1)
            public double seyirYogunlugu;   // topa uzakken v_max çarpanı (jog) — M5
            public double sprintYaricapM;   // bu yarıçap içinde tam gaz (topa/göreve yakınlık)
        }

        /// <summary>Top fiziği — ME Spec 8.2-8.3 [KALİBRE].</summary>
        [System.Serializable]
        public sealed class PhysicsCfg
        {
            public double aRollKuru;          // yerde sürtünme yavaşlaması (m/sn²)
            public double sekmeEKuru;         // dikey sekme katsayısı
            public double sekmeYatayCarpan;   // sekmede yatay hız çarpanı (ME 8.3)
            public double blokIleriOran;      // bloğun topu ileri sıyırma oranı (kalanı geri döner)
            public double blokIleriAcisiDeg;  // ileri sıyırmada yana açılma açısı
            public double blokSacilmaDeg;     // bloklanan şutun geri sekme açısı sigması (ME 8.3)
            public double dragK;              // hava direnci k_d
            public double magnusK;            // falso k_m (M3+ orta/frikik kullanır)
        }

        /// <summary>Genel düello çekirdeği — ME Spec 6.3 [KALİBRE].</summary>
        [System.Serializable]
        public sealed class DuelCfg
        {
            public double pTabanDefault, kDuel, clampMin, clampMax;
            public double pTabanTackle;   // top kapma düellosu tabanı (ME 6.3 "tipe göre")
            public double pTabanDriblin;  // dribling düellosu: taşıyıcının adam geçme tabanı
        }

        /// <summary>Pas modeli — ME Spec 6.5 [KALİBRE].</summary>
        [System.Serializable]
        public sealed class PassCfg
        {
            public double sigma0Deg;           // nişan hatası taban sigma (derece)
            public double distFactorPerM;      // f_mesafe = 1 + d × bu (1 + d/35)
            public double presFactorPerRakip;  // M_pres = 1 + bu × yakın rakip
            public double groundSpeedMin, groundSpeedMax; // yerden pas hızı bandı (m/sn)
            public double presYaricapM;        // "yakın rakip" sayım yarıçapı (M2 ekleme)
            public double kaleOnuTamponM;      // pas hedefi kendi kale çizgisine bundan fazla yaklaşamaz (M16-E)
        }

        /// <summary>Topsuz konumlanmada anchor ağırlığı — ME Spec 7.4 [KALİBRE].</summary>
        [System.Serializable]
        public sealed class AnchorCfg
        {
            public double wAnchorMin, wAnchorMax;
        }

        /// <summary>Sahiplik/kontrol — ME Spec 4.3 vekil bantları (M2 ekleme) [KALİBRE].</summary>
        [System.Serializable]
        public sealed class PossessionCfg
        {
            public double kontrolYaricapM;     // serbest topu kontrol alma yarıçapı
            public double tackleYaricapM;      // taşıyıcıya müdahale yarıçapı
            public double driblinYaricapM;     // dribling düellosunu tetikleyen ön mesafe
            public double kontrolHizEsigiMS;   // temiz kontrol için bağıl hız eşiği (m/sn, ME 6.4)
            public double kaleciElCarpani;     // kalecinin Handling ile açtığı eşik çarpanı (ME 9.4)   // temiz kontrol için bağıl hız eşiği (m/sn, ME 6.4)
            public int yenidenAlmaTicks;       // topu oynayanın geri alma kilidi (tick, ME 4.3)
            public int tackleCooldownTicks;    // müdahale sonrası aksiyon kilidi
            /// <summary>Aynı savunucunun iki tackle DENEMESİ arasındaki asgari süre (tick) —
            /// M16-C enstrümanı. Karar kilidinden (tackleCooldownTicks → ActionUntilTick) ayrı
            /// tutulur ki deneme SIKLIĞI tek başına ayarlanabilsin; iki değer eşitken davranış
            /// eski modelle birebir aynıdır (M0-M15 golden'ları kanıt).</summary>
            public int tackleDenemeAralikTicks;
            public double tackleLooseHizMS;    // kazanılan top bu hızla açığa çıkar
        }

        /// <summary>Utility karar ağırlıkları — ME Spec 7.2 [KALİBRE] (M2 çekirdek alt kümesi).</summary>
        [System.Serializable]
        public sealed class UtilityCfg
        {
            public double wThreat, wRisk, wVar;   // Score = wT×ΔxT + wR×(1−P_kayıp) + wV×ChaosNoise
            public int kararKilidiTicks;          // seçilen aksiyon bu kadar tick sürer (ME 7.2)
            public int adayTabani, adayVisionBolen; // aday_sayısı = taban + Vision/bolen (ME 7.2)
            public double dribbleIleriM;          // dribling hedef ilerlemesi
            public double kayipTaban, kayipKoridorRakip, kayipMesafePerM; // P_kayıp hızlı tahmini
            public double pasKesmeBandiSn;        // pas kesme yarışında %50→%100 taşıyan zaman marjı (sn)
            public double sutTehditCarpan;        // şut adayının tehdit ağırlığı (M3)
            public double sutBaskiCezasi;         // yakın rakip başına şut iştahı cezası (M4)
            public double sutKoridorCeza;         // kurulmuş blokta koridor gövdesi başına şut iştahı cezası (M16-F, pres01 ölçekli)
            public double araPasIleriM;           // ara pasın alıcının önüne bıraktığı mesafe (M5)
            public double araPasRisk;             // ara pasın ek kayıp riski
            public double araPasUlasimBandiM;     // ulaşım yarışında %50→%100'e taşıyan mesafe farkı (m)
            public double araPasKotuZamanlama;// koşu zamanlama hatası → ofsayt (ME 10.5)
            public double ortaTehditCarpan;       // orta aksiyonunun tehdit ağırlığı (ME 6.4)
            public double ortaMinMesafeM;         // bu mesafeden yakınsa orta yerine şut/pas
            public double ortaMaxMesafeM;         // bu mesafeden uzaksa orta aday olmaz
            public double kontraTehditCarpan;     // kontra penceresinde wThreat kayması (M9)
            public double kontraRiskTolerans;     // kontra penceresinde wRisk kayması (M9)
            public double kontraBlokBonus;        // derin bloktan çıkan kontranın ek tehdit çarpanı (M16-F, pres01 ölçekli)
            public double mentaliteTehditCarpan;  // mentalite ucunda wThreat kayması (±oran, ME 7.2/14.2)
            public double mentaliteRiskTolerans;  // mentalite ucunda wRisk kayması (±oran — kayıp korkusu)
            public double tempoTutCezasi;         // tempo başına "topu tut" cezası
        }

        /// <summary>Topsuz vektör karması ağırlıkları — ME Spec 7.4 [KALİBRE] (M2 alt kümesi).</summary>
        [System.Serializable]
        public sealed class OffballCfg
        {
            public double wTop;              // top çekimi ağırlığı
            public double fazIleriM;         // hücumda ileri itme (m)
            public double savunmaCekilmeM;   // (eski) — savunma bloku vektörüne devredildi
            // ME 7.6 hat formülü: hat_x = taban(talimat) + hatTopKatsayi × (top_x − saha_ortası)
            public double hatTabanM;         // savunma hattının kendi kalesine mesafesi (m)
            public double hatTopKatsayi;     // topun orta sahaya göre kaymasının hatta etkisi (spec: 0,35)
            public double hatMinM;           // hat taban kırpması — en derin blok (m)
            public double hatMaxM;           // hat taban kırpması — en yüksek blok (m)
            public double hatIleriMfM;       // orta sahanın hattın önündeki mesafesi (m)
            public double hatIleriFwM;       // forvetin hattın önündeki mesafesi (m)
            public double hatYanAnchor;      // yanal konum: anchor payı (kalanı topa hizalanır)
            public double kanatGenislikM;    // kanat rollerinin touchline açılımı (ME 7.4-A)
            public double kanatAnchorEsikMm; // bu |anchorY| üstü kanat sayılır
            public double markajGolTarafi;   // markajcının hedefin gol tarafına iniş oranı (ME 7.5)
            public int markajSayisi;         // takım başına markaj görevi sayısı
            public double presKesmeSn;       // pres öngörü süresi — kesme noktası (sn, ME 7.4-B)
            public double kanalKapamaM;      // ikinci savunanın topun gol tarafına iniş mesafesi (m)
            public double hatTalimatM;       // hat talimatı başına hat kayması (m, ME 7.6/14.2)
            public double kutuDerinlikM;     // kutuya girişte kale çizgisine mesafe (m, ME 7.4-A)
            public double kutuYanM;          // kutuya girişte direk hizası yanal mesafe (m)
            public double ortaGenislikEsikMm;// bu |Y| üstü "kanatta" sayılır (orta koşulu)
            public double kontraIleriM;      // kontra penceresinde hücumcunun ek derinliği (m)
            public double gecisSnTaban;      // topu kaptıran takımın toparlanma süresi (sn)
            public double gecisSnPerMentalite; // ofansif mentalite başına ek toparlanma süresi (sn)
            public double mentaliteIleriItmeM; // mentalite başına hücumda ileri itme (m, ME 7.4/14.2)
            public double mentaliteHatM;     // ofansif mentalite başına hat yükselmesi (m)
            // Derin blok — ME 7.6 genişlemesi (M16-F, DECISIONS 2026-08-19 hibrit kararı):
            // baskı yaşayan takım hattını kademeli indirir ve bloku kale eksenine daraltır
            public double blokBaskiBolgesiM; // kendi kalesinden bu mesafe içi "baskı bölgesi" (m)
            public int blokPresKBolen;       // baskı EMA'sı DOLUM böleni (Q16) — blok hızlı kurulur
            public int blokPresKBolenDusus;  // baskı EMA'sı BOŞALIM böleni — blok yavaş çözülür
            public double blokCokmeMaxM;     // tam baskıda hattın ek çökme miktarı (m)
            public double blokDaralmaOran;   // tam baskıda yanal daralma oranı (0-1)
            public double kontraPresEkSn;    // tam baskıdan çıkan kontranın ek geçiş penceresi (sn)
        }

        /// <summary>xT (beklenen tehdit) tablosu — ME Spec 7.2 [KALİBRE]. M2: ayrıştırılabilir
        /// (kolon × satır) temsil; tam 96 hücreli tabloya geçiş kalibrasyon sprintinde.</summary>
        [System.Serializable]
        public sealed class XtCfg
        {
            public double[] kolon = new double[0];  // [12] kendi kaleden rakip kaleye
            public double[] satir = new double[0];  // [8] alt taçtan üst taca (orta şerit yüksek)
        }

        /// <summary>Chaos seviye tablosu — ME Spec 13.2 [KALİBRE]. 5 enjeksiyon noktasının tamamı
        /// (M16-D2): düello marjı · karar skoru · nişan çarpanı · hakem gri bandı · sekme
        /// pertürbasyonu. Seviye maç kurulumundan gelir (MatchConfig.Chaos); varsayılan Orta.</summary>
        [System.Serializable]
        public sealed class ChaosCfg
        {
            public SigmaCfg duelSigma = new SigmaCfg();      // düello marjı gürültüsü (100 ölçeği)
            public SigmaCfg decisionSigma = new SigmaCfg();  // karar skoru gürültüsü
            public SigmaCfg aimCarpan = new SigmaCfg();      // nişan sapması çarpanı (pas/şut/orta/uzun)
            public SigmaCfg griBantEk = new SigmaCfg();      // hakem gri bandı (seviyenin TAM bandı)
            public SigmaCfg sekmePertDerece = new SigmaCfg();// sekme yönü pertürbasyonu (yalnız Yüksek)
            [System.Serializable]
            public sealed class SigmaCfg { public double dusuk, orta, yuksek; }
        }
    }
}
