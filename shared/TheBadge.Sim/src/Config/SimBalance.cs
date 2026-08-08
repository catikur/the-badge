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

        /// <summary>Kaleci kurtarış modeli — ME Spec 9.1-9.2 [KALİBRE].</summary>
        [System.Serializable]
        public sealed class GkCfg
        {
            public double tReactBase, tReactPerReflexEksik;   // t_react = taban + (100−Reflexes)×bu
            public double reachBase, reachAgilityFactor;      // erişim = taban + Agility/100×faktör (m)
            public double logisticSlope;                      // P_save = lojistik(slope × marj)
            public double saveClampMin, saveClampMax;
            public double dalisSureCarpan;                    // t_traverse = mesafe/(erişim/bu)
            public double cildirmaAcisiDeg;                   // çeldide sapma açısı
            public double derinlikTaban, derinlikPerM, derinlikMax; // 9.1 pozisyon derinliği
        }

        /// <summary>Şut yürütme — ME 6.4 kompoziti + 8.3 (M3 ekleme) [KALİBRE].</summary>
        [System.Serializable]
        public sealed class ShotExecCfg
        {
            public double sutMaxMesafeM;       // karar: bu mesafenin dışında şut aday olmaz
            public double sutHiziMS;
            public double sutSigmaTabanM;      // nişan sapması (kale düzleminde, m)
            public double sutSigmaMesafePerM;
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
        }

        /// <summary>Top fiziği — ME Spec 8.2-8.3 [KALİBRE].</summary>
        [System.Serializable]
        public sealed class PhysicsCfg
        {
            public double aRollKuru;          // yerde sürtünme yavaşlaması (m/sn²)
            public double sekmeEKuru;         // dikey sekme katsayısı
            public double sekmeYatayCarpan;   // sekmede yatay hız çarpanı (ME 8.3)
            public double dragK;              // hava direnci k_d
            public double magnusK;            // falso k_m (M3+ orta/frikik kullanır)
        }

        /// <summary>Genel düello çekirdeği — ME Spec 6.3 [KALİBRE].</summary>
        [System.Serializable]
        public sealed class DuelCfg
        {
            public double pTabanDefault, kDuel, clampMin, clampMax;
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
            public double sutTehditCarpan;        // şut adayının tehdit ağırlığı (M3)
        }

        /// <summary>Topsuz vektör karması ağırlıkları — ME Spec 7.4 [KALİBRE] (M2 alt kümesi).</summary>
        [System.Serializable]
        public sealed class OffballCfg
        {
            public double wTop;              // top çekimi ağırlığı
            public double fazIleriM;         // hücumda ileri itme (m)
            public double savunmaCekilmeM;   // savunmada topla kendi kalesi arasına çekilme (m)
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
