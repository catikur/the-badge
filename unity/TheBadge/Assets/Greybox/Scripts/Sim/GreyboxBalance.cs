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
            public float perspektifYSkala;    // sunum: TV kamerası perspektif ezmesi (1 = düz üstten)
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

        [Serializable]
        public sealed class ModelCfg
        {
            public int blokSayisi;            // maç kaç aksiyon bloğu (Sahneleme §0)
            public float pGolTabani;          // blok başına taban gol olasılığı (taraf başına)
            public float gucEtkiCarpan;       // güç farkı puanı başına olasılık kayması
            public float momentumEtki;        // momentumun blok olasılığına etkisi
            public float taktikTempoEtki;     // tempo çarpanının etki ağırlığı
            public float taktikSutEtki;       // şut iştahının etki ağırlığı
            public float taktikPresSavunmaEtki; // rakip presinin savunma etkisi
            public float pGolMin;
            public float pGolMax;
            public float tehlikeCarpan;       // sessiz blokta "tehlike" olayı üretme çarpanı
            public float momentumGolDelta;    // gol sonrası momentum itmesi
            public float momentumSonum;       // blok başına ortalamaya dönüş
            public float momentumBlokGurultu; // blok başına rastgele salınım
            public int hamleHakki;            // maç başına müdahale hakkı
            public float tempoYukseltBiz;     // müdahale çarpanları (risk iki yönlü)
            public float tempoYukseltRakip;
            public float kilitlenBiz;
            public float kilitlenRakip;
            public float blokOynatmaSn;       // 1x hızda blok sunum süresi (gerçek sn)
            public float gerilimBeklemeSn;    // olasılık gösterimi → zar arası bekleme
            public float vinyetMaxSimSn;      // vinyet üretiminde sim arama tavanı
            public float vinyetKayitSn;       // vinyetin gol ÖNCESİ uzunluğu
            public float vinyetKutlamaSn;     // gol SONRASI kutlama kaydı — sevinç tam izlenir
            public float evAvantaj;           // ev sahibi avantajı (greybox'ta oyuncu hep ev)
            public float gucEtkiMax;          // güç farkının doyuma ulaşan tavan etkisi (tanh)
            public float gucOlcek;            // tanh ölçeği (puan)
            public float[] fazCarpanlar;      // blok başına maç fazı çarpanı (son dakika golleri)
            public float gerideRiskCarpan;    // geride olan taraf riske girer
            public float ondeKontrolCarpan;   // önde olan taraf maçı soğutur
            public float formEtkiCarpan;      // son 5 maç formunun etkisi (net galibiyet başına)
            public float[] taktikMatchup;     // 3x3 etkileşim matrisi (satır: hücum, sütun: savunma)
        }

        [Serializable]
        public sealed class SquadCfg
        {
            public float enerjiBaslangic;       // ME Spec 12.1 Energy tavanının vekili (1000)
            public float yorgunlukBlokDrenaj;   // blok başına oyuncu enerji drenajı
            public float drenajTempoYukselt;    // tempo müdahalesi drenaj çarpanları (risk/bedel)
            public float drenajKilitlen;
            public float drenajRakipEtki;       // bizim temponun rakip drenajına yansıma oranı
            public float drenajTaktikEtki;      // taktik tempo çarpanının drenaja katkısı
            public float gkDrenajCarpan;        // kaleci yavaş yorulur
            public float yorgunlukGucTaban;     // ME 12.1 M_kondisyon vekili: E=0'da kalan güç oranı
            public int degisiklikHakki;         // GDD 12.4 standart 3 (hamle hakkından AYRI)
            public float tazeBacakEnerji;       // giren oyuncunun enerjisi
            public float eksikHucumCarpan;      // eksik oyuncu başına kendi gol olasılığı çarpanı
            public float rakipEksikSavunmaCarpan; // rakip eksikken bizim gol olasılığımız artar
        }

        [Serializable]
        public sealed class EventCfg
        {
            public float sariMacBasi;           // maç TOPLAM sarı bandı (iki takım) — ME 11.2 ölçekli
            public float kirmiziMacBasi;        // maç toplam direkt kırmızı
            public float sakatlikMacBasi;       // maç toplam sakatlık — ME 12.2 bandı ölçekli
            public float kartTempoYukseltCarpan; // agresif tempoda kart riski artar
            public float ikinciSariAgirlik;     // agresif tempoda kartlı oyuncunun seçilme ağırlığı
            public float sakatlikYorgunlukEtki; // ME 12.2 M_yorgunluk vekili (takım enerjisiyle)
            public float[] kartMevkiAgirlik;    // DF, MF, FW seçilme ağırlıkları
            public float[] golMevkiAgirlik;     // gol atfı ağırlıkları: DF, MF, FW (kozmetik)
        }

        public MetaInfo _meta;
        public ModelCfg model;
        public SquadCfg squad;
        public EventCfg olay;
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
