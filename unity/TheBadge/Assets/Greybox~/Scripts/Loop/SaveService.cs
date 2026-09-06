using System;
using System.IO;
using TheBadge.Greybox.Sim;
using UnityEngine;

namespace TheBadge.Greybox.Loop
{
    /// <summary>
    /// Mini kalıcılık — para/bilet fiyatı/form oturumlar arası korunur ki
    /// "yarın kendi isteğinle açar mıydın?" sorusunun zemini olsun (Fun Ölçüm Protokolü).
    /// Greybox kolaylığı: JsonUtility + tek dosya; FAZ 04'te sunucu-otoriter save'e taşınır.
    /// </summary>
    public static class SaveService
    {
        static string PathFor() => Path.Combine(Application.persistentDataPath, "greybox_save.json");

        public static GreyboxState LoadOrNew(GreyboxBalance bal)
        {
            try
            {
                string p = PathFor();
                if (File.Exists(p))
                {
                    var loaded = JsonUtility.FromJson<GreyboxState>(File.ReadAllText(p));
                    if (loaded != null && loaded.matchIndex >= 1) return loaded;
                }
            }
            catch (Exception)
            {
                // bozuk save greybox'ta veri kaybı sayılmaz; sıfırdan başlat
            }
            return GreyboxState.NewGame(bal);
        }

        public static void Save(GreyboxState state)
        {
            try
            {
                File.WriteAllText(PathFor(), JsonUtility.ToJson(state, prettyPrint: true));
            }
            catch (Exception)
            {
                // yazılamazsa sessiz geç (greybox); telemetri dosyası ayrı yoldadır
            }
        }

        public static void Delete()
        {
            try { if (File.Exists(PathFor())) File.Delete(PathFor()); }
            catch (Exception) { }
        }
    }
}
