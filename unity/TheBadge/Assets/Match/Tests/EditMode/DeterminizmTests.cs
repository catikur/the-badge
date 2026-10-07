using System.IO;
using System.Runtime.InteropServices;
using NUnit.Framework;
using TheBadge.Sim.Replay;
using UnityEngine;

namespace TheBadge.Match.Tests
{
    /// <summary>DETERMİNİZM — P0 ikinci aşaması, Editor ayağı (Mono). DECISIONS P0 (b), ADR-004.
    ///
    /// Sunucunun koştuğu sondanın AYNISI (paket `com.thebadge.sim.replay`): balance baytları,
    /// balance değerleri, P0 pinleri ve 50 golden replay. Balance OYUNUN kendi yolundan okunur
    /// (`BalansKaynagi.Yukle`, yani JsonUtility) — sunucu System.Text.Json kullanır; bir katsayı son
    /// bitte farklı okunursa "BalansDegerleri" katmanı hangi alanda olduğunu söyler.
    ///
    /// 50 tam maç koşar: .NET Release'te ~8 sn sürdü (linux-x64); Mono Editor'de daha uzun bekle
    /// (RitimTests 24 maçta ~30 sn). Rapor Console'a ve test çıktısına basılır — kırmızıysa o metni
    /// olduğu gibi gönder.</summary>
    public sealed class DeterminizmTests
    {
        [Test]
        public void EditorSunucuylaBitEsit()
        {
            var balans = BalansKaynagi.Yukle();
            string balansKlasoru = BalansKaynagi.BalansKlasoru();
            string goldenKlasoru = Path.GetFullPath(Path.Combine(balansKlasoru, "..", "shared",
                                                                 "TheBadge.Sim.Checks", "goldens"));

            var rapor = DeterminizmSondasi.Kos(
                balans.Sim,
                File.ReadAllBytes(Path.Combine(balansKlasoru, "sim.balance.json")),
                File.ReadAllBytes(Path.Combine(balansKlasoru, "command.bands.json")),
                File.ReadAllText(Path.Combine(goldenKlasoru, "replay_set_v1.json")),
                File.ReadAllText(Path.Combine(goldenKlasoru, "balance_degerleri_v1.txt")),
                "editor-mono-" + RuntimeInformation.ProcessArchitecture + "-" + Application.platform);

            string metin = rapor.Metin();
            Debug.Log(metin);
            TestContext.WriteLine(metin);
            Assert.IsTrue(rapor.Gecti, metin);
        }
    }
}
