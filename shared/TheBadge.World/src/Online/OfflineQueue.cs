using System;
using System.Collections.Generic;
using TheBadge.CommandBus;
using TheBadge.Sim.Commands;

namespace TheBadge.World
{
    /// <summary>Bağlantı durumu — CB 8.3. `Cevrimici` sunucuya erişim VAR demektir; `Cevrimdisi`
    /// online ligde bağlantının koptuğu hâldir (offline KARİYER modu ayrıdır ve zaten yereldir).</summary>
    public enum BaglantiDurumu : byte { Cevrimici = 0, Cevrimdisi = 1 }

    /// <summary>Kuyrukta bekleyen komutun uzlaştırma sonucu — K6 kararı (2026-08-25):
    /// sunucu otoriterdir ama düşen komut SESSİZCE YUTULMAZ, sebebiyle raporlanır.</summary>
    public enum UzlastirmaSonucu : byte { Uygulandi = 0, Dustu = 1 }

    /// <summary>Kuyruktaki bir komutun akıbeti. `Sebep` düşen komutun NEDEN düştüğünü taşır —
    /// kullanıcıya gösterilecek olan budur (CB 8.2: "kullanıcı her zaman net sonuç görür").</summary>
    public struct UzlastirmaKaydi
    {
        public Guid CommandId;
        public string ActionType;
        public UzlastirmaSonucu Sonuc;
        public RejectionReason Sebep;      // Uygulandi ise None
        public string Detay;
    }

    /// <summary>OFFLINE KUYRUK VE UZLAŞTIRMA — CB 8.3 + FAZ 04 K6 kararı.
    ///
    /// İKİ KURAL, İKİSİ DE YAPISAL:
    /// 1. **Tier 1-2 bağlantısız VERİLEMEZ** (CB 8.3). Ekonomik durum çatallanması "kullanıcı
    ///    dikkatli olur" diye değil, komut hiç kuyruğa GİRMEDİĞİ için engellenir. Kuyruğa alıp
    ///    sonra reddetmek, kullanıcıya yaptığı işin tutulduğunu düşündürürdü.
    /// 2. **Düşen komut RAPOR EDİLİR.** Yeniden bağlanınca kuyruk SIRAYLA sunucuya gider; sunucu
    ///    otoriterdir (D3/G3) ama reddettiği her komut `UzlastirmaKaydi` olarak geri döner.
    ///    Elenen seçenek (a) "sunucu kazanır + sessiz düş": kullanıcının emeğini görünmez
    ///    şekilde siler (DECISIONS 2026-08-25).
    ///
    /// DETERMİNİZM: kuyruk `List` ile SIRALIdır (sözlük değil); tekrar oynatma ekleme sırasındadır.
    /// Aynı kuyruk + aynı sunucu durumu = aynı uzlaştırma raporu.</summary>
    public sealed class OfflineQueue
    {
        readonly List<(CommandEnvelope env, IPayloadView payload)> kuyruk
            = new List<(CommandEnvelope, IPayloadView)>();
        readonly int tavan;

        /// <summary>`kuyrukTavani` [KALİBRE]: sınırsız kuyruk, uzun bir kopukluktan sonra
        /// yeniden bağlanmayı dakikalarca sürecek bir tekrar oynatmaya çevirirdi.</summary>
        public OfflineQueue(int kuyrukTavani)
        {
            if (kuyrukTavani <= 0) throw new ArgumentOutOfRangeException(nameof(kuyrukTavani));
            tavan = kuyrukTavani;
        }

        public int Sayi => kuyruk.Count;
        public bool Dolu => kuyruk.Count >= tavan;

        /// <summary>Bağlantı yokken komut kabulü. Tier 1-2 REDDEDİLİR — kuyruğa alınmaz.
        /// Dönen sebep `None` ise komut kuyruğa girdi.</summary>
        public RejectionReason Kuyrukla(CommandEnvelope env, ActionDef action, IPayloadView payload, out string detay)
        {
            detay = null;
            if (action == null) return RejectionReason.UnknownAction;
            // CB 8.3: Tier 1-2 bağlantı yokken VERİLEMEZ. Tier katalogda sabittir ve kaynaktan
            // bağımsızdır (CB 6) — yani LLM ya da otomasyon bu kapıyı düşüremez.
            if (action.Tier != Tier.T0)
            {
                detay = $"bağlantı yokken Tier {(int)action.Tier} komut verilemez (CB 8.3)";
                return RejectionReason.StateConflict;
            }
            if (Dolu)
            {
                detay = $"çevrimdışı kuyruk dolu ({tavan})";
                return RejectionReason.StateConflict;
            }
            kuyruk.Add((env, payload));
            return RejectionReason.None;
        }

        /// <summary>Yeniden bağlanma: kuyruk SIRAYLA sunucuya gönderilir. Sunucu otoriter —
        /// reddettiği komut düşer ama SEBEBİYLE raporlanır.
        ///
        /// HER TAMAMLANAN GİRDİ ANINDA KUYRUKTAN DÜŞER. Önce sonunda `Clear()` çağırıyordum;
        /// gönderim ortada PATLARSA (ağ kopması) o satıra hiç ulaşılmıyor ve ZATEN UYGULANMIŞ
        /// önek kuyrukta kalıyordu — sonraki bağlanmada tekrar oynardı ve o turun raporu da
        /// kaybolurdu (inceleme bulgusu, P2). Artık istisna yukarı çıksa bile kuyrukta yalnız
        /// GÖNDERİLMEMİŞ sonek kalır; tamamlananların raporu `tamamlanan` üzerinden dışarı verilir.</summary>
        public UzlastirmaKaydi[] YenidenBaglan(Func<CommandEnvelope, IPayloadView, (RejectionReason, string)> sunucuyaGonder)
        {
            var rapor = new List<UzlastirmaKaydi>();
            YenidenBaglan(sunucuyaGonder, rapor);
            return rapor.ToArray();
        }

        /// <summary>Rapor DIŞARIDAN verilen listeye yazılır: gönderim patlarsa çağıran taraf
        /// o ana kadar tamamlananların raporunu yine de elinde tutar.</summary>
        public void YenidenBaglan(Func<CommandEnvelope, IPayloadView, (RejectionReason, string)> sunucuyaGonder,
                                  List<UzlastirmaKaydi> rapor)
        {
            if (sunucuyaGonder == null) throw new ArgumentNullException(nameof(sunucuyaGonder));
            if (rapor == null) throw new ArgumentNullException(nameof(rapor));
            while (kuyruk.Count > 0)
            {
                var (env, payload) = kuyruk[0];
                var (sebep, detay) = sunucuyaGonder(env, payload);   // patlarsa: bu girdi KUYRUKTA kalır
                kuyruk.RemoveAt(0);                                   // tamamlandı → hemen düş
                rapor.Add(new UzlastirmaKaydi
                {
                    CommandId = env.CommandId,
                    ActionType = env.ActionType,
                    Sonuc = sebep == RejectionReason.None ? UzlastirmaSonucu.Uygulandi : UzlastirmaSonucu.Dustu,
                    Sebep = sebep,
                    Detay = detay
                });
            }
        }

        /// <summary>Kuyruğu boşaltır — kullanıcı "bekleyenleri iptal et" derse. Rapor üretmez.</summary>
        public void Bosalt() => kuyruk.Clear();
    }
}
