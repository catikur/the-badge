using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace TheBadge.Determinizm.Editor
{
    /// <summary>DETERMİNİZM SONDASI — macOS IL2CPP build'i (P0 ikinci aşaması, DECISIONS P0 (b), ADR-004).
    ///
    /// Menü: The Badge → Determinizm Sondası (macOS IL2CPP). Tek tıkla:
    ///  1. sunucunun golden seti, balance değer dökümü ve balance dosyalarını StreamingAssets'e kopyalar
    ///     (oyuncu repo'yu göremez);
    ///  2. boş bir geçici sahne kurar — oyunun sahnesi oyuncuda balance'ı bulamaz ve hata basardı;
    ///  3. IL2CPP + Release (sevkiyat yapılandırması) ile macOS standalone build alır;
    ///  4. oyuncuyu `arch -arm64` ile `-batchmode -nographics` koşar, raporu Console'a basar ve
    ///     sonucu bir diyalogla söyler (rapor + günlük build'in yanında, `artifacts/` altında).
    /// Proje ayarlarını (scripting backend, IL2CPP yapılandırması), açık sahneleri ve kopyalanan
    /// dosyaları HER DURUMDA geri yükler. Build çıktısı repo kökünde `artifacts/` altındadır (git dışı).
    ///
    /// Ön koşul: aktif platform macOS (Build Profiles), Unity'de "Mac Build Support (IL2CPP)" modülü,
    /// Xcode (ya da Command Line Tools). macOS ve iOS aynı derleyici ailesini (clang/arm64) kullanır:
    /// bu build iOS'un güçlü vekilidir, kesin kanıtı iOS cihaz koşusu verir (5G-b).</summary>
    public static class DeterminizmSondasiDerleyici
    {
        const string Baslik = "Determinizm Sondası";
        const string GeciciSahne = "Assets/Determinizm/DeterminizmSondasi_gecici.unity";
        const string StreamingAsset = "Assets/StreamingAssets";
        const string VeriAsset = StreamingAsset + "/" + DeterminizmSondasiOyuncu.VeriKlasoru;

        /// <summary>Oyuncunun en uzun koşma süresi. Sondanın tamamı .NET Release'te ~8 sn sürdü (linux-x64,
        /// 2026-10-07); bu sınır yalnız Quit'e ulaşamayan bir oyuncunun Editor'ü dondurmasını önler.</summary>
        const int ZamanAsimiMs = 10 * 60 * 1000;

        [MenuItem("The Badge/Determinizm Sondası (macOS IL2CPP)")]
        public static void DerleVeKos()
        {
            if (Application.platform != RuntimePlatform.OSXEditor)
            { Mesaj("Bu sonda macOS'ta koşar (clang/arm64 — iOS'un derleyici ailesi)."); return; }
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneOSX)
            { Mesaj("Önce aktif platformu macOS yap: File → Build Profiles → macOS → Switch Platform. Sonra menüyü tekrar çalıştır."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string repo = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));
            string balans = Path.Combine(repo, "balance");
            string golden = Path.Combine(repo, "shared", "TheBadge.Sim.Checks", "goldens");
            string uygulama = Path.Combine(repo, "artifacts", "DeterminizmSondasi", "DeterminizmSondasi.app");
            string streamingKok = Path.Combine(Application.dataPath, "StreamingAssets");
            bool streamingVardi = Directory.Exists(streamingKok);

            var hedef = NamedBuildTarget.Standalone;
            var oncekiArka = PlayerSettings.GetScriptingBackend(hedef);
            var oncekiYapi = PlayerSettings.GetIl2CppCompilerConfiguration(hedef);
            var sahneler = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                // 1) Veri — sunucunun ürettiği dosyalar, olduğu gibi (baytları balance özetine girer)
                string veri = Path.Combine(streamingKok, DeterminizmSondasiOyuncu.VeriKlasoru);
                Directory.CreateDirectory(veri);
                Kopyala(balans, veri, "sim.balance.json");
                Kopyala(balans, veri, "command.bands.json");
                Kopyala(golden, veri, "replay_set_v1.json");
                Kopyala(golden, veri, "balance_degerleri_v1.txt");
                AssetDatabase.Refresh();

                // 2) Boş geçici sahne — sonda RuntimeInitializeOnLoadMethod ile sahneden önce koşar
                var sahne = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                if (!EditorSceneManager.SaveScene(sahne, GeciciSahne))
                { Mesaj("Geçici sahne kaydedilemedi: " + GeciciSahne); return; }

                // 3) IL2CPP + Release, macOS standalone
                PlayerSettings.SetScriptingBackend(hedef, ScriptingImplementation.IL2CPP);
                PlayerSettings.SetIl2CppCompilerConfiguration(hedef, Il2CppCompilerConfiguration.Release);
                var rapor = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { GeciciSahne },
                    locationPathName = uygulama,
                    target = BuildTarget.StandaloneOSX,
                    targetGroup = BuildTargetGroup.Standalone,
                    options = BuildOptions.None
                });
                if (rapor.summary.result != BuildResult.Succeeded)
                {
                    Mesaj("Build başarısız (" + rapor.summary.result + "). Console'daki hatalara bak. " +
                          "'Mac Build Support (IL2CPP)' modülü ve Xcode kurulu olmalı.");
                    return;
                }
            }
            finally
            {
                PlayerSettings.SetScriptingBackend(hedef, oncekiArka);
                PlayerSettings.SetIl2CppCompilerConfiguration(hedef, oncekiYapi);
                try { if (sahneler != null && sahneler.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(sahneler); }
                catch (Exception ex) { Debug.LogWarning("Açık sahneler geri yüklenemedi: " + ex.Message); }
                AssetDatabase.DeleteAsset(GeciciSahne);
                AssetDatabase.DeleteAsset(VeriAsset);
                if (!streamingVardi) AssetDatabase.DeleteAsset(StreamingAsset);
                AssetDatabase.Refresh();
            }

            Kos(uygulama);
        }

        static void Kos(string uygulama)
        {
            // Çalıştırılabilir önce ürün adıyla aranır; yoksa klasördeki TEK dosya alınır (birden çoksa
            // hangisinin bu build olduğu bilinemez → dur)
            string macos = Path.Combine(uygulama, "Contents", "MacOS");
            string calistirilabilir = Path.Combine(macos, PlayerSettings.productName);
            if (!File.Exists(calistirilabilir))
            {
                string[] dosyalar = Directory.Exists(macos) ? Directory.GetFiles(macos) : new string[0];
                if (dosyalar.Length != 1)
                {
                    Mesaj("Oyuncunun çalıştırılabilir dosyası bulunamadı: " + macos +
                          " (Build Profiles'ta 'Create Xcode Project' kapalı olmalı).");
                    return;
                }
                calistirilabilir = dosyalar[0];
            }

            // Rapor ve günlük build'in yanına yazılır (git dışı `artifacts/`); eski koşunun dosyaları silinir
            string klasor = Path.GetDirectoryName(uygulama);
            string sonucYolu = Path.Combine(klasor, DeterminizmSondasiOyuncu.SonucDosyasi);
            string logYolu = Path.Combine(klasor, "oyuncu.log");
            if (File.Exists(sonucYolu)) File.Delete(sonucYolu);
            if (File.Exists(logYolu)) File.Delete(logYolu);

            // `arch -arm64`: evrensel (Intel + Apple silicon) build'in arm64 dilimini koşturur, arm64
            // dilimi yoksa HİÇ koşturmaz. Bu olmadan yalnız-Intel bir build Rosetta'da x64 olarak koşar
            // ve sonda iOS'un mimarisini (arm64) değil x64'ü ölçer — yeşil ama anlamsız bir sonuç.
            var bilgi = new ProcessStartInfo("/usr/bin/arch",
                "-arm64 " + Tirnak(calistirilabilir) + " -batchmode -nographics -logFile " + Tirnak(logYolu) +
                " " + DeterminizmSondasiOyuncu.SonucArgumani + " " + Tirnak(sonucYolu))
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = klasor
            };
            var hata = new StringBuilder();
            int kod;
            EditorUtility.DisplayProgressBar(Baslik, "Oyuncu 50 maçı ve P0 pinlerini koşuyor…", 0.5f);
            try
            {
                using (var p = new Process { StartInfo = bilgi })
                {
                    p.ErrorDataReceived += (s, e) => { if (e.Data != null) lock (hata) hata.AppendLine(e.Data); };
                    p.Start();
                    p.BeginErrorReadLine();
                    if (!p.WaitForExit(ZamanAsimiMs))
                    {
                        try { p.Kill(); } catch (Exception) { }
                        EditorUtility.ClearProgressBar();
                        Mesaj("Oyuncu " + ZamanAsimiMs / 60000 + " dakikada bitmedi ve durduruldu. Günlük: " + logYolu);
                        return;
                    }
                    p.WaitForExit();   // asenkron stderr okuması boşalsın
                    kod = p.ExitCode;
                }
            }
            finally { EditorUtility.ClearProgressBar(); }

            bool raporVar = File.Exists(sonucYolu);
            string sonuc = raporVar ? File.ReadAllText(sonucYolu) : "(rapor dosyası yok: " + sonucYolu + ")\n";
            string stderr;
            lock (hata) stderr = hata.ToString();
            string gunluk = File.Exists(logYolu) ? File.ReadAllText(logYolu) : "(günlük yok: " + logYolu + ")";
            Debug.Log(sonuc + "--- oyuncu çıkış kodu: " + kod + " · build: " + uygulama +
                      (stderr.Length > 0 ? "\n--- stderr ---\n" + stderr : "") +
                      (raporVar ? "" : "\n--- oyuncu günlüğünün sonu ---\n" + Son(gunluk, 4000)));

            // Etiket oyuncunun DERLEME bayrağından gelir (ENABLE_IL2CPP): build bir Build Profile
            // ayarı yüzünden Mono çıktıysa sonuç yeşil olsa da IL2CPP'yi ölçmemiştir
            bool il2cpp = raporVar && sonuc.IndexOf("[il2cpp-", StringComparison.Ordinal) >= 0;
            if (!raporVar)
                Mesaj("Rapor oluşmadı (çıkış kodu " + kod + "). Oyuncu hiç başlamamış olabilir: build'de arm64 " +
                      "dilimi yoksa koşturulmaz (Build Profiles → macOS → Architecture: 'Apple silicon' ya da " +
                      "'Intel 64-bit + Apple silicon'). Console'daki metni olduğu gibi gönder.");
            else if (!il2cpp)
                Mesaj("Build IL2CPP DEĞİL — ölçüm geçersiz. Aktif Build Profile'ın Player Settings " +
                      "override'ı scripting backend'i değiştiriyor olabilir. Console'daki metni gönder.");
            else if (kod == 0)
                Mesaj("GEÇTİ — macOS IL2CPP (arm64) sunucuyla bit-eşit. Raporu Console'dan gönder.");
            else
                Mesaj("KALDI (çıkış kodu " + kod + "). Raporu Console'dan olduğu gibi gönder.");
        }

        static string Tirnak(string s) => "\"" + s + "\"";

        static void Kopyala(string kaynakKlasor, string hedefKlasor, string ad)
            => File.Copy(Path.Combine(kaynakKlasor, ad), Path.Combine(hedefKlasor, ad), true);

        static string Son(string s, int n) => s == null ? "" : s.Length <= n ? s : s.Substring(s.Length - n);

        static void Mesaj(string m)
        {
            Debug.Log(Baslik + ": " + m);
            EditorUtility.DisplayDialog(Baslik, m, "Tamam");
        }
    }
}
