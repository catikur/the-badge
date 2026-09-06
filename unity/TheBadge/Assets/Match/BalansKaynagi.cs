using System;
using System.Collections.Generic;
using System.IO;
using TheBadge.CommandBus;
using TheBadge.Sim.Config;
using TheBadge.World;
using UnityEngine;

namespace TheBadge.Match
{
    /// <summary>BALANCE YÜKLEYİCİ — üç dosya, tek yer.
    ///
    /// `balance/` repo kökündedir ve editörde `Application.dataPath`ten üç yukarısıdır
    /// (Assets → unity/TheBadge → unity → kök). Bu, EngineDev sahnesinin zaten kullandığı
    /// yolun aynısıdır; kopyalanmadı, aynı türetme tekrar edildi (dosya taşındığı için
    /// paylaşılacak ortak bir yer yok — ikisi de kendi asmdef'inde).
    ///
    /// KAPSAM: bu ekran dünyayı OYNAMAZ, yalnız komut köprüsünü kurar (TASK-002 Scope/Out).
    /// O yüzden `economy.balance.json` / `transfer.balance.json` YÜKLENMEZ — yüklenseydi
    /// ekran, kapsam dışı bırakılan tycoon/transfer yollarını sessizce canlandırırdı.</summary>
    public sealed class BalansKaynagi
    {
        public SimBalance Sim { get; private set; }
        public WorldRules DunyaKurallari { get; private set; }
        public BantSaglayici Bantlar { get; private set; }
        public Dictionary<RateClass, RateLimitCfg[]> HizSiniri { get; private set; }

        /// <summary>Repo kökündeki `balance/` klasörü — editörde.</summary>
        public static string BalansKlasoru()
            => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", "balance"));

        public static BalansKaynagi Yukle()
        {
            string dir = BalansKlasoru();
            var k = new BalansKaynagi();

            k.Sim = JsonUtility.FromJson<SimBalance>(Oku(dir, "sim.balance.json"));
            if (k.Sim == null) throw new FormatException("sim.balance.json okunamadı.");

            k.DunyaKurallari = JsonUtility.FromJson<WorldRules>(Oku(dir, "world.balance.json"));
            if (k.DunyaKurallari == null) throw new FormatException("world.balance.json okunamadı.");
            k.DunyaKurallari.Validate();       // bozuk balance kurulumda patlar, maç ortasında değil

            var kok = BasitJson.Ayristir(Oku(dir, "command.bands.json"));

            var bant = new BantSaglayici();
            foreach (var kv in BasitJson.Nesne(BasitJson.Yol(kok, "bantlar")))
            {
                var ikili = BasitJson.Dizi(kv.Value);
                if (ikili.Count != 2) throw new FormatException("command.bands: bant [min, max] olmalı: " + kv.Key);
                bant.Ekle(kv.Key, BasitJson.Sayi(ikili[0]), BasitJson.Sayi(ikili[1]));
            }
            k.Bantlar = bant;

            k.HizSiniri = new Dictionary<RateClass, RateLimitCfg[]>();
            foreach (var kv in BasitJson.Nesne(BasitJson.Yol(kok, "rateLimit")))
            {
                if (!Enum.TryParse(kv.Key, out RateClass sinif))
                    throw new FormatException("command.bands: bilinmeyen RateClass: " + kv.Key);
                var pencereler = BasitJson.Dizi(kv.Value);
                var cfg = new RateLimitCfg[pencereler.Count];
                for (int i = 0; i < pencereler.Count; i++)
                {
                    var p = BasitJson.Dizi(pencereler[i]);
                    if (p.Count != 2) throw new FormatException("command.bands: rateLimit [adet, saniye] olmalı: " + kv.Key);
                    // saniye → ms (Checks harness'ıyla aynı dönüşüm; birim dosyada saniyedir)
                    cfg[i] = new RateLimitCfg((int)BasitJson.Sayi(p[0]), (long)BasitJson.Sayi(p[1]) * 1000L);
                }
                k.HizSiniri[sinif] = cfg;
            }
            return k;
        }

        static string Oku(string dir, string ad)
        {
            string yol = Path.Combine(dir, ad);
            if (!File.Exists(yol)) throw new FileNotFoundException("balance dosyası bulunamadı: " + yol, yol);
            return File.ReadAllText(yol);
        }
    }

    /// <summary>`IBandProvider` — Kapı 2'nin bant tablosu. Anahtar YOKSA `false` döner ve komut
    /// `ParamOutOfBand` ile reddedilir; sessiz geçiş yoktur (CB Spec 5).</summary>
    public sealed class BantSaglayici : IBandProvider
    {
        readonly Dictionary<string, (double min, double max)> b =
            new Dictionary<string, (double, double)>(StringComparer.Ordinal);

        public void Ekle(string anahtar, double min, double max) => b[anahtar] = (min, max);
        public int Sayi => b.Count;

        public bool TryGetBand(string bandKey, out double min, out double max)
        {
            min = max = 0;
            if (bandKey == null || !b.TryGetValue(bandKey, out var v)) return false;
            min = v.min; max = v.max; return true;
        }
    }
}
