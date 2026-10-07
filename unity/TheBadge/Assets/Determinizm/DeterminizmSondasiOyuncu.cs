using System;
using System.IO;
using System.Runtime.InteropServices;
using TheBadge.Sim.Config;
using TheBadge.Sim.Replay;
using UnityEngine;

namespace TheBadge.Determinizm
{
    /// <summary>DETERMİNİZM SONDASI — oyuncu (player) ayağı. P0 ikinci aşaması, DECISIONS P0 (b), ADR-004.
    ///
    /// IL2CPP build'inde sunucunun koştuğu sondanın AYNISINI koşar (paket `com.thebadge.sim.replay`),
    /// raporu Player.log'a ve bir dosyaya yazar, çıkış koduyla (0 = bit-eşit) kapanır. Dosya yolu
    /// `-sondaSonuc <yol>` argümanıyla verilir (derleyici öyle koşar); argüman yoksa
    /// Application.persistentDataPath altına yazılır (oyuncu elle açıldığında).
    ///
    /// Yalnız SONDA build'inde çalışır: veri klasörü (`StreamingAssets/DeterminizmSondasi`) yalnız o
    /// build'e paketlenir (`DeterminizmSondasiDerleyici` kopyalar ve build'den sonra siler). Normal
    /// build'lerde ve Editor'de hiçbir şey yapmaz — ama her zaman DERLENİR, yani kod Editor'ü
    /// açar açmaz derleyiciden geçer. Balance oyunla AYNI çağrıyla okunur (JsonUtility).</summary>
    public static class DeterminizmSondasiOyuncu
    {
        /// <summary>StreamingAssets altındaki veri klasörü — derleyici buraya kopyalar.</summary>
        public const string VeriKlasoru = "DeterminizmSondasi";

        /// <summary>Rapor dosyasının adı (argüman verilmezse persistentDataPath altında).</summary>
        public const string SonucDosyasi = "determinizm_sondasi.txt";

        /// <summary>Rapor dosyasının yolunu veren komut satırı argümanı.</summary>
        public const string SonucArgumani = "-sondaSonuc";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Kos()
        {
            if (Application.isEditor) return;
            string kok = Path.Combine(Application.streamingAssetsPath, VeriKlasoru);
            if (!Directory.Exists(kok)) return;   // sonda verisi yok → normal build, dokunma

            int cikis = 1;
            string metin;
            try
            {
                string balYolu = Path.Combine(kok, "sim.balance.json");
                // OYUNUN yolu: BalansKaynagi.Yukle aynı çağrıyı yapar (JsonUtility)
                var bal = JsonUtility.FromJson<SimBalance>(File.ReadAllText(balYolu));
                var rapor = DeterminizmSondasi.Kos(
                    bal,
                    File.ReadAllBytes(balYolu),
                    File.ReadAllBytes(Path.Combine(kok, "command.bands.json")),
                    File.ReadAllText(Path.Combine(kok, "replay_set_v1.json")),
                    File.ReadAllText(Path.Combine(kok, "balance_degerleri_v1.txt")),
                    Platform());
                metin = rapor.Metin();
                cikis = rapor.Gecti ? 0 : 1;
            }
            catch (Exception ex)
            {
                metin = "DETERMINIZM SONDASI [" + Platform() + "]: HATA — " + ex;
            }

            Debug.Log(metin);
            try { File.WriteAllText(SonucYolu(), metin); }
            catch (Exception ex) { Debug.LogError("sonda raporu yazılamadı: " + ex.Message); }
            Application.Quit(cikis);
        }

        static string SonucYolu()
        {
            string[] a = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < a.Length; i++)
                if (a[i] == SonucArgumani) return a[i + 1];
            return Path.Combine(Application.persistentDataPath, SonucDosyasi);
        }

        static string Platform()
        {
#if ENABLE_IL2CPP
            const string arka = "il2cpp";
#else
            const string arka = "mono";
#endif
            return arka + "-" + RuntimeInformation.ProcessArchitecture + "-" + Application.platform;
        }
    }
}
