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
            public double cikisMesafeM;         // kalecinin serbest topa çıktığı derinlik (m, ME 9.3)
            public double cikisGenislikM;       // çıkış bölgesinin yarı genişliği (m)
            public double yakinMesafeM;         // 1v1 kapatma etkisinin başladığı mesafe (ME 9.3)
            public double yakinKapatmaKatsayi;  // OneOnOne'ın marja katkısı (sn)
        }

        /// <summary>Şut yürütme — ME 6.4 kompoziti + 8.3 (M3 ekleme) [KALİBRE].</summary>
        [System.Serializable]
        public sealed class ShotExecCfg
        {
            public double sutMaxMesafeM;       // karar: bu mesafenin dışında şut aday olmaz
            public double sutYariMesafeM;      // mesafe tehdidinin yarıya düştüğü mesafe (m, rasyonel çekirdek)
            public double sutHiziMS;
            public double sutSigmaTabanDeg;    // nişan sapması AÇISAL (derece) — mesafeyle büyür
            public double nisanDirekOrani;     // nişan noktası: direk yarı genişliğinin bu oranı
            public double blokOlasilik;        // koridorda savunucu varsa blok olasılığı (ME 15.1)
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
            public double araPasIleriM;           // ara pasın alıcının önüne bıraktığı mesafe (M5)
            public double araPasRisk;             // ara pasın ek kayıp riski
            public double araPasUlasimBandiM;     // ulaşım yarışında %50→%100'e taşıyan mesafe farkı (m)
            public double araPasKotuZamanlama;// koşu zamanlama hatası → ofsayt (ME 10.5)
            public double ortaTehditCarpan;       // orta aksiyonunun tehdit ağırlığı (ME 6.4)
            public double ortaMinMesafeM;         // bu mesafeden yakınsa orta yerine şut/pas
            public double ortaMaxMesafeM;         // bu mesafeden uzaksa orta aday olmaz
            public double kontraTehditCarpan;     // kontra penceresinde wThreat kayması (M9)
            public double kontraRiskTolerans;     // kontra penceresinde wRisk kayması (M9)
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
            public double ortaGenislikEsikMm;// bu |Y| üstü "kanatta" sayılır (orta koşulu)
            public double kontraIleriM;      // kontra penceresinde hücumcunun ek derinliği (m)
            public double gecisSnTaban;      // topu kaptıran takımın toparlanma süresi (sn)
            public double gecisSnPerMentalite; // ofansif mentalite başına ek toparlanma süresi (sn)
            public double mentaliteIleriItmeM; // mentalite başına hücumda ileri itme (m, ME 7.4/14.2)
            public double mentaliteHatM;     // ofansif mentalite başına hat yükselmesi (m)
        }

        /// <summary>xT (beklenen tehdit) tablosu — ME Spec 7.2 [KALİBRE]. M2: ayrıştırılabilir
        /// (kolon × satır) temsil; tam 96 hücreli tabloya geçiş kalibrasyon sprintinde.</summary>
        [System.Serializable]
        public sealed class XtCfg
        {
            public double[] kolon = new double[0];  // [12] kendi kaleden rakip kaleye
            public double[] satir = new double[0];  // [8] alt taçtan üst taca (orta şerit yüksek)
        }

        /// <summary>Chaos sigma seviyeleri — ME Spec 13.2 (M2: yalnız orta seviye tüketilir).</summary>
        [System.Serializable]
        public sealed class ChaosCfg
        {
            public SigmaCfg decisionSigma = new SigmaCfg();
            [System.Serializable]
            public sealed class SigmaCfg { public double dusuk, orta, yuksek; }
        }
    }
}
