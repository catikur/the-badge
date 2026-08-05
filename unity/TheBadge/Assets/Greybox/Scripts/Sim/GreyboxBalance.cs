using System;

namespace TheBadge.Greybox.Sim
{
    /// <summary>
    /// Greybox [KALİBRE-G] ayarları — Resources/greybox.balance.json'dan yüklenir.
    /// DİKKAT: Bu dosya balance/sim.balance.json'dan AYRIDIR ve config_hash kapsamı DIŞIDIR;
    /// FAZ 00.5 his prototipiyle birlikte emekli edilir (plan kararı, Atilla onayı 2026-07-30).
    /// Unity tarafı JsonUtility ile, headless harness System.Text.Json ile yükler:
    /// bu yüzden yalnız public alanlar + [Serializable] iç sınıflar + diziler kullanılır.
    /// </summary>
    [Serializable]
    public sealed class GreyboxBalance
    {
        [Serializable]
        public sealed class MetaInfo
        {
            public string schema;
            public string comment;
        }

        [Serializable]
        public sealed class ClockCfg
        {
            public float macSuresiSaniye;     // 90 dakikanın 1x hızda kaç GERÇEK saniyeye sığdığı
            public float devreArasiSaniye;    // devre arası bandosu süresi (gerçek sn)
            public float skipDilimSaniye;     // skip sırasında sessiz adım dilimi
        }

        [Serializable]
        public sealed class PaceCfg
        {
            public float aksiyonAralikMinSn;  // top hedefe varınca yeni karar arası bekleme bandı
            public float aksiyonAralikMaxSn;
            public float santraBeklemeSn;     // diziliş SAĞLANDIKTAN sonra düdük öncesi nefes (Sahneleme 1)
            public float kutlamaSuresiSn;
            public float spikerAralikSn;      // boşta akan spiker satırı sıklığı (sunum)
            public float dizilisEmniyetSn;    // diziliş kilitlenme emniyeti — Sahneleme kök ilkesi
            public float gkTutmaSn;           // kurtarış sonrası kalecinin topu tutma süresi (Sahne 4)
        }

        [Serializable]
        public sealed class BallCfg
        {
            public float pasHiziMinMS;
            public float pasHiziMaxMS;
            public float sutHiziMinMS;
            public float sutHiziMaxMS;
            public float ortaHiziMS;
            public float tasimaHiziMS;        // oyuncu topla ilerlerken (kısa sürüklemeler)
            public float pasYukMaxM;          // kısa pas tepe yüksekliği (yerden gider)
            public float uzunTopYukM;         // uzun top / degaj / uzaklaştırma tepe yüksekliği
            public float ortaYukM;            // korner ortası tepe yüksekliği
            public float sutYukM;             // şut tepe yüksekliği (sert ve alçak)
            public float yukOlcekCarpan;      // sunum: metre başına top ölçeği artışı
            public float yukKaldirmaCarpan;   // sunum: metre başına ekranda kaldırma (gölgeden ayrılma)
        }

        [Serializable]
        public sealed class PlayersCfg
        {
            public float vMaxMS;              // saha oyuncusu tavan hızı
            public float presHizCarpan;       // topa en yakın savunmacının hız çarpanı
            public float wanderGenlikM;       // amaçsız salınım genliği (canlılık)
            public float wanderPeriyotSn;
            public float hucumKaymaM;         // hücumda blokça öne kayma (metre)
            public float savunmaKaymaM;       // savunmada geriye kompaktlaşma
            public float topCekimYaricapM;    // topa yakın oyuncuların çekim alanı
            public float oyuncuYaricapM;      // GÖRSEL yarıçap (okunabilirlik için abartılı)
            public float topYaricapM;
        }

        [Serializable]
        public sealed class FlowCfg
        {
            public float pIleriPas;           // karar dağılımı: ileri pas
            public float pYanPas;
            public float pGeriPas;
            public float pUzunTop;
            public float pTopKaybiTaban;      // karar başına temel top kaybı olasılığı
            public float pTopKaybiPresEtki;   // rakip pres çarpanının katkısı
            public float pCezaSahasinaGiris;  // final üçlüde ChanceBuild'e geçiş
            public float ilerlemeMinM;        // ileri pas başına ilerleme bandı (metre)
            public float ilerlemeMaxM;
            public float genislikMaxM;        // yan pas / sürüklenme genişliği
            public float gucTutmaCarpan;      // kadro gücü farkının top tutmaya etkisi (puan başına)
            public float pDegaj;              // kalecinin uzun degaj tercihi (Sahne 4)
        }

        [Serializable]
        public sealed class ShotCfg
        {
            public float pSutChanceBuild;     // ChanceBuild adımı başına şut olasılığı
            public float pGol;                // şut başına gol tabanı
            public float pKurtarma;           // gol değilse: kurtarış ağırlığı
            public float pDisari;             // gol değilse: auta gitme ağırlığı
            public float pKornerSekmesi;      // gol değilse: kornere sekme ağırlığı
            public float pKornerKurtarisSonrasi; // kurtarışın kornere çelinme olasılığı
            public float gucEtkiCarpan;       // takım gücü farkının pGol'e etkisi (puan başına)
            public float momentumEtki;        // momentumun pGol'e etkisi
        }

        [Serializable]
        public sealed class CornerCfg
        {
            public float pKafaSut;            // ortadan kafa şutu çıkma olasılığı
            public float pGolKafa;            // kafa şutundan gol (sim.balance cornerGolBandi aynası)
            public float dizilisSn;           // diziliş SAĞLANDIKTAN sonra orta öncesi nefes (Sahne 5)
        }

        [Serializable]
        public sealed class MomentumCfg
        {
            public float sigma;               // OU süreci gürültüsü
            public float sonum;               // ortalamaya dönüş hızı
            public float golBoost;            // gol atan tarafa anlık itme
            public float gucFarkiCarpan;      // kadro gücü farkının sürekli eğimi (puan başına)
        }

        [Serializable]
        public sealed class VurguCfg
        {
            public float slowmoCarpan;        // gol anında zaman çarpanı
            public float slowmoSureSn;        // gerçek saniye
            public float shakeGenlikM;
            public float shakeSureSn;
            public float golFlasSureSn;
            public bool titresimAktif;        // gol anında cihaz titreşimi (yalnız mobil)
        }

        [Serializable]
        public sealed class EkonomiCfg
        {
            public int kapasite;              // GDD 4.1: başlangıç stadı 5.000
            public float refFiyat;
            public float fiyatMin;
            public float fiyatMax;
            public float talepTaban;          // ref fiyatta nötr form doluluk oranı
            public float fiyatEsneklik;       // fiyat sapmasının doluluğa etkisi (GDD 4.2)
            public float formEtkiGalibiyet;   // son 5 maçtaki galibiyet başına talep artışı
            public float formEtkiMaglubiyet;
            public float dolulukMin;
            public int galibiyetPrimi;
            public int beraberlikPrimi;
            public int maglubiyetPrimi;
            public int baslangicPara;
        }

        [Serializable]
        public sealed class TakimlarCfg
        {
            public float oyuncuTakimGucu;     // oyuncunun kulübü (sabit başlangıç gücü)
            public float rakipGucMin;
            public float rakipGucMax;
        }

        [Serializable]
        public sealed class TacticCfg
        {
            public int id;
            public string ad;
            public string formasyon;          // "442" | "433" | "532"
            public float hatYuksekligi;       // 0-1: savunma hattının saha boyundaki konumu
            public float tempo;               // aksiyon sıklığı çarpanı
            public float sutIstahi;           // şut olasılığı çarpanı
            public float pres;                // top kazanma çarpanı
        }

        [Serializable]
        public sealed class RenklerCfg
        {
            public string saha;
            public string cizgi;
            public string evTakim;
            public string evKaleci;
            public string depTakim;
            public string depKaleci;
            public string top;
            public string arkaplan;
        }

        public MetaInfo _meta;
        public ClockCfg clock;
        public PaceCfg pace;
        public BallCfg ball;
        public PlayersCfg players;
        public FlowCfg flow;
        public ShotCfg shot;
        public CornerCfg corner;
        public MomentumCfg momentum;
        public VurguCfg vurgu;
        public EkonomiCfg ekonomi;
        public TakimlarCfg takimlar;
        public TacticCfg[] taktikler;
        public RenklerCfg renkler;
    }
}
