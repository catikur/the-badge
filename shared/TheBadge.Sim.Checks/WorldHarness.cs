using System;
using System.Collections.Generic;
using TheBadge.CommandBus;
using TheBadge.Sim.Commands;
using TheBadge.World;

namespace TheBadge.Checks
{
    /// <summary>Denetim + olay toplayıcısı — audit'in YÜRÜTME içinde geldiğini ve hash'lerin
    /// dolduğunu kanıtlar (CB 5.2 / 9.1).</summary>
    public sealed class CollectingAuditSink : IWorldAuditSink
    {
        public readonly List<WorldAuditEntry> Kayitlar = new List<WorldAuditEntry>();
        public readonly List<WorldEvent> Olaylar = new List<WorldEvent>();
        public void Persist(WorldAuditEntry entry, IReadOnlyList<WorldEvent> events)
        {
            Kayitlar.Add(entry);
            for (int i = 0; i < events.Count; i++) Olaylar.Add(events[i]);
        }
        public void Clear() { Kayitlar.Clear(); Olaylar.Clear(); }
    }

    /// <summary>Test handler'ı — K3-K5 gelene kadar journal mekanizmasını sınar. Davranışı
    /// alanlarla ayarlanır: kasa deltası, oyuncu yazması, kasıtlı red, kasıtlı GEÇERSİZ yazma.</summary>
    public sealed class TestHandler : IActionHandler
    {
        public long KasaDelta;
        public RejectionReason Result = RejectionReason.None;
        public bool GecersizYazma;          // aralık dışı yazma üretir (handler hatası taklidi)
        public int OyuncuIndex = -1;
        public byte OyuncuAlan = PlayerField.Moral;
        public long OyuncuDeger;
        public WorldEventType Olay = WorldEventType.None;
        public Action<WorldJournal> Ozel;   // serbest journal kurgusu (zincirleme yazma sınamaları)
        int cagri;
        public int Cagrilar => System.Threading.Volatile.Read(ref cagri);

        public RejectionReason Apply(GameState st, WorldJournal journal, CommandEnvelope env,
                                     ActionDef action, IPayloadView payload, out string detail)
        {
            System.Threading.Interlocked.Increment(ref cagri);
            detail = null;
            if (Result != RejectionReason.None) { detail = "test reddi"; return Result; }
            if (KasaDelta != 0) journal.KasaDelta(KasaDelta);
            if (OyuncuIndex >= 0) journal.OyuncuSet(OyuncuIndex, OyuncuAlan, OyuncuDeger);
            if (GecersizYazma) journal.Set(MutTarget.Oyuncu, 0, PlayerField.Moral, 300);  // 0-100 dışı
            if (Olay != WorldEventType.None)
                journal.Emit(new WorldEvent(Olay, 0, KasaDelta, st.Takvim.Sezon, st.Takvim.Hafta));
            Ozel?.Invoke(journal);
            return RejectionReason.None;
        }
    }

    /// <summary>Aksiyona özgü Kapı 3 kuralı sahtesi — K3-K5 seaminin sınandığı yer.</summary>
    public sealed class TestRule : IActionRule
    {
        public RejectionReason Sonuc = RejectionReason.None;
        public int Cagrilar;
        public RejectionReason Check(GameState st, CommandEnvelope env, ActionDef action, IPayloadView payload, out string detail)
        { Cagrilar++; detail = Sonuc == RejectionReason.None ? null : "test kuralı"; return Sonuc; }
    }

    /// <summary>Kapı 3 yarışını DETERMİNİSTİK yapan sarmalayıcı. Yarışı şansa bırakan bir test,
    /// hata varken de yeşil kalabilir — nitekim ilk sürümü öyle oldu: tekrar denetimi kaldırıldığı
    /// hâlde kapı yeşil kaldı. Burada bariyer, TÜM iş parçacıkları Kapı 3'ü geçene kadar hiçbirinin
    /// yürütmeye geçmemesini garantiler; yani "doğrula-sonra-yürüt" penceresi her koşuda açılır.
    ///
    /// Bariyer YALNIZ iş parçacığının İLK çağrısında (bus doğrulaması) beklenir; yürütücünün
    /// kilit altındaki TEKRAR denetimi (ikinci çağrı) geçer — yoksa kilitlenirdi.</summary>
    public sealed class BarrierContext : IValidationContext
    {
        readonly IValidationContext ic;
        readonly System.Threading.Barrier bariyer;
        [ThreadStatic] static int derinlik;
        public BarrierContext(IValidationContext inner, int katilimci)
        { ic = inner; bariyer = new System.Threading.Barrier(katilimci); }
        public bool IsContextActive(Context context) => ic.IsContextActive(context);
        public long ResolveTeamKey(CommandEnvelope env) => ic.ResolveTeamKey(env);
        public RejectionReason CheckOwnershipAndState(CommandEnvelope env, ActionDef action, IPayloadView payload, out string detail)
        {
            var r = ic.CheckOwnershipAndState(env, action, payload, out detail);
            if (derinlik++ == 0) bariyer.SignalAndWait();   // durum kilidi BURADA tutulmuyor
            return r;
        }
    }

    /// <summary>Fırlatan denetim sinki — CB 5.2'nin bellek ayağını sınar: audit yazımı
    /// başarısızsa durum İLERLEMEMİŞ olmalı.</summary>
    public sealed class ThrowingAuditSink : IWorldAuditSink
    {
        public int Cagrilar;
        public void Persist(WorldAuditEntry entry, IReadOnlyList<WorldEvent> events)
        { Cagrilar++; throw new InvalidOperationException("denetim deposu erişilemez"); }
    }

    /// <summary>K3 REFERANS KULÜP — ekonomi sözleşmesinin (ECONOMY_MAP) ölçüldüğü senaryo.
    /// "İyi yönetilen orta ölçekli kulüp": tier 3 stadyum (30.000), 22 kişilik kadro, tesisler
    /// makul seviyede, fiyatlar referans bandında. Katsayılar bu kulübü 1,05-1,15 bandında
    /// TUTMALIDIR — fixture sabit, kalibre edilen `economy.balance.json`tır.</summary>
    public static class EkonomiFixture
    {
        public const int Kapasite = 30000;
        public const int KadroSayisi = 22;
        public const long OyuncuHaftalikMaas = 70_700;

        public static GameState Kur(WorldRules rules, EconomyBalance eco, long clubId, long ownerUserId)
        {
            var st = WorldFixture.Kur(rules, clubId, ownerUserId, KadroSayisi, 2, 2, 20_000_000);
            st.Club.StadyumKapasite = Kapasite;
            st.Club.Form = 50;
            // Tesisler: stadyum tier 3 + dört tesis tier 2 (bakım gideri tier toplamına bağlı)
            st.Club.TesisTier[EconomyTick.StadyumTesisId] = 3;
            for (int i = 2; i <= 5; i++) st.Club.TesisTier[i] = 2;
            // Maaş gideri kadroyla tutarlı
            for (int i = 0; i < st.Oyuncular.Length; i++)
                if (st.Oyuncular[i].ClubId == clubId) st.Oyuncular[i].HaftalikMaasTl = OyuncuHaftalikMaas;
            st.Club.HaftalikMaasGiderTl = KadroSayisi * OyuncuHaftalikMaas;
            // K5 değerleme girdileri: güç/potansiyel/yaş DETERMİNİSTİK dağıtılır (indeksten
            // türetilir, RNG yok) — böylece transfer kapıları sabit bir kadro üzerinde ölçer.
            for (int i = 0; i < st.Oyuncular.Length; i++)
            {
                st.Oyuncular[i].Guc = (byte)(45 + (i * 7) % 45);          // 45-89
                st.Oyuncular[i].Potansiyel = (byte)System.Math.Min(99, st.Oyuncular[i].Guc + (i * 3) % 12);
                st.Oyuncular[i].Yas = (byte)(19 + (i * 5) % 17);          // 19-35
                st.Oyuncular[i].SozlesmeKalanHafta = (ushort)(26 + (i * 11) % 130);
            }
            // Fiyatlar referans seviyesinde (kuruş)
            for (int t = 0; t < 5; t++) st.Fiyat.BiletKurus[t] = eco.tribun.referansFiyat[t] * 100;
            st.Fiyat.KombineKurus = (int)(eco.kombine.referansFiyat * 100);
            for (int i = 0; i < 3; i++) st.Fiyat.BufeKurus[i] = (int)(eco.macGunu.bufeReferansFiyat * 100);
            for (int i = 0; i < 3; i++) st.Fiyat.MagazaKurus[i] = (int)(eco.macGunu.magazaReferansFiyat * 100);
            return st;
        }
    }

    /// <summary>Sezon simülatörü — haftalık tick'i TEK KAPI'dan geçirerek koşturur (doğrudan
    /// durum mutasyonu yok). Sonuç dizisi ECONOMY_MAP sözleşmesinin ölçüldüğü veridir.</summary>
    public static class EkonomiKosu
    {
        /// <summary>`sezon` sezon boyunca haftalık tick. Maç sonuçları DETERMİNİSTİK bir
        /// örüntüden gelir (rastgelelik ekonomiyi değil, ölçümü bulanıklaştırırdı): sırayla
        /// G-B-M-G-B-M... → %33 galibiyet, %33 beraberlik, %33 mağlubiyet.</summary>
        public static WeekLedger Kos(GameState st, EconomyBalance eco, WorldRules kural,
                                     ulong saveSeed, int sezon, out int iflasSezonu)
        {
            var toplam = new WeekLedger();
            var j = new WorldJournal();
            iflasSezonu = -1;
            int hafta = 0;
            for (int s = 0; s < sezon; s++)
            {
                for (int h = 0; h < kural.yapi.sezonHaftaSayisi; h++, hafta++)
                {
                    var sonuc = (WeekResult)(byte)(1 + (hafta % 3));      // G, B, M döngüsü
                    bool evMaci = (hafta % 2) == 0;
                    j.Clear();
                    var L = EconomyTick.Hafta(st, eco, kural, saveSeed, sonuc, evMaci, j);
                    if (!j.Validate(st, out string hata))
                        throw new InvalidOperationException("ekonomi journal geçersiz: " + hata);
                    j.Apply(st);
                    toplam.Topla(L);
                    if (iflasSezonu < 0 && st.Club.KasaTl <= eco.iflas.esikTl) iflasSezonu = s + 1;
                }
            }
            return toplam;
        }
    }

    /// <summary>Maç kuyruğu casusu — köprünün ME komutunu GERÇEKTEN ürettiğini ölçer.</summary>
    public sealed class SpyMatchSink : IMatchCommandSink
    {
        public readonly List<TheBadge.Sim.Match.MatchCommand> Komutlar = new List<TheBadge.Sim.Match.MatchCommand>();
        public void Enqueue(TheBadge.Sim.Match.MatchCommand cmd) => Komutlar.Add(cmd);
    }

    /// <summary>Online yayın casusu — K6. Klip ve rapor ayrı listelerde tutulur ki
    /// "yayınlandı mı" ve "hangisi yayınlandı" ayrı ayrı ölçülebilsin.</summary>
    /// <summary>YALNIZ belirli bir kaydı patlatan kanal. `SpyOnlineSink.Patlat` hepsini birden
    /// patlatır ve o yüzden SIRA iddiasını ÖLÇEMEZ: hepsi patlayınca "başta takıldı" ile "hepsini
    /// denedi, hepsi patladı" aynı sonucu verir. Sıra korunuyor mu sorusunu ancak ilki patlarken
    /// arkadakiler BAŞARILI OLABİLİYORKEN sorabilirsin.</summary>
    public sealed class SecmeliPatlayanSink : TheBadge.World.IOnlineSink
    {
        public readonly List<(System.Guid cid, int macId)> Klipler = new List<(System.Guid, int)>();
        public int PatlayanMacId = -1;
        public void KlipPaylas(System.Guid commandId, int macId, int pencereSn, byte hedef, long userId)
        {
            if (macId == PatlayanMacId) throw new InvalidOperationException($"mac {macId} icin ag hatasi (test)");
            Klipler.Add((commandId, macId));
        }
        public void OyuncuRaporla(System.Guid commandId, long hedefUserId, byte sebep, string notlar, long userId) { }
    }

    /// <summary>Durumu DEĞİŞTİREN ve AYNI komutta yayın YAPAN teste özel handler. Katalogda böyle
    /// bir aksiyon yok (mevcut yayıncı aksiyonlar durumu değiştirmiyor), ama `WorldExecutor`in
    /// commit SIRASI bu birleşimde anlam kazanıyor: olaylar yayınlardan ÖNCE önbelleğe yazılırsa,
    /// yayın patlayıp durum geri alındığında önbellekte hayalet olaylar kalır.</summary>
    public sealed class HemDegistirHemYayinla : TheBadge.World.IActionHandler
    {
        public RejectionReason Apply(TheBadge.World.GameState st, TheBadge.World.WorldJournal j,
                                     CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
        {
            detail = null;
            j.Set(TheBadge.World.MutTarget.Kulup, 0, TheBadge.World.ClubField.Form, 55);
            j.Emit(new TheBadge.World.WorldEvent(TheBadge.World.WorldEventType.TaktikGuncellendi, 0, 55,
                                                 st.Takvim.Sezon, st.Takvim.Hafta));
            j.PersonaKonusma(env.CommandId, 1, 0, env.UserId);
            return RejectionReason.None;
        }
    }

    /// <summary>Olay kanalı patlatan sink — sözleşme "FIRLATMAMALI, fırlatırsa yutulur" diyor.
    /// Bu tip o sözleşmeyi sınar: patlayan bir kanal komutu DÜŞÜRMEMELİ ve durumu geri ALMAMALI.</summary>
    public sealed class PatlayanOlayKanali : TheBadge.World.IKomutOlaySinki
    {
        public void Yaz(System.Guid commandId, long userId, long anUnixMs,
                        IReadOnlyList<TheBadge.World.WorldEvent> olaylar)
            => throw new InvalidOperationException("olay kanali patladi (test)");
    }

    public sealed class PatlayanPersona : TheBadge.World.IPersonaSink
    {
        public void KonusmaAyarlandi(System.Guid commandId, int personaId, byte tonIndeksi, long userId)
            => throw new InvalidOperationException("persona kanali patladi (test)");
        public void BasinYaniti(System.Guid commandId, int soruId, byte cevapSinifi, long userId)
            => throw new InvalidOperationException("persona kanali patladi (test)");
    }

    public sealed class SessizPersona : TheBadge.World.IPersonaSink
    {
        public void KonusmaAyarlandi(System.Guid commandId, int personaId, byte tonIndeksi, long userId) { }
        public void BasinYaniti(System.Guid commandId, int soruId, byte cevapSinifi, long userId) { }
    }

    public sealed class SpyOnlineSink : TheBadge.World.IOnlineSink
    {
        public readonly List<(System.Guid cid, int macId, int pencereSn, byte hedef, long userId)> Klipler
            = new List<(System.Guid, int, int, byte, long)>();
        public readonly List<(System.Guid cid, long hedefUserId, byte sebep, string notlar, long userId)> Raporlar
            = new List<(System.Guid, long, byte, string, long)>();
        /// <summary>true ise yayın PATLAR — işlem güvenliği ölçümü için.</summary>
        public bool Patlat;
        public void KlipPaylas(System.Guid commandId, int macId, int pencereSn, byte hedef, long userId)
        {
            if (Patlat) throw new InvalidOperationException("ağ zaman aşımı (test)");
            Klipler.Add((commandId, macId, pencereSn, hedef, userId));
        }
        public void OyuncuRaporla(System.Guid commandId, long hedefUserId, byte sebep, string notlar, long userId)
        {
            if (Patlat) throw new InvalidOperationException("ağ zaman aşımı (test)");
            Raporlar.Add((commandId, hedefUserId, sebep, notlar, userId));
        }
    }

    /// <summary>Persona kanal casusu — K7.</summary>
    public sealed class SpyPersonaSink : TheBadge.World.IPersonaSink
    {
        public readonly List<(System.Guid cid, int personaId, byte ton, long userId)> Konusmalar
            = new List<(System.Guid, int, byte, long)>();
        public readonly List<(System.Guid cid, int soruId, byte sinif, long userId)> Basinlar
            = new List<(System.Guid, int, byte, long)>();
        public void KonusmaAyarlandi(System.Guid cid, int personaId, byte ton, long userId)
            => Konusmalar.Add((cid, personaId, ton, userId));
        public void BasinYaniti(System.Guid cid, int soruId, byte sinif, long userId)
            => Basinlar.Add((cid, soruId, sinif, userId));
    }

    /// <summary>K2 dünya durumu kurulum yardımcıları.</summary>
    public static class WorldFixture
    {
        /// <summary>Kanonik kadro: PlayerId artan. `yabanciSayisi` kadar oyuncu BAŞKA kulüpte,
        /// `serbestSayisi` kadarı serbest (ClubId 0) — sahiplik denetimlerinin üç kolu için.</summary>
        public static GameState Kur(WorldRules rules, long clubId, long ownerUserId,
                                    int kendi, int yabanci, int serbest, long kasaTl)
        {
            var st = GameState.Olustur(rules, clubId, ownerUserId);
            st.Club.KasaTl = kasaTl;
            st.Club.StadyumKapasite = 20000;
            var list = new List<PlayerState>();
            int pid = 100;
            Instruction[] Yuva() => new Instruction[rules.yapi.talimatYuvaSayisi];
            for (int i = 0; i < kendi; i++)
                list.Add(new PlayerState { PlayerId = pid++, ClubId = clubId, HaftalikMaasTl = 10000,
                                           SozlesmeKalanHafta = 100, Moral = 60, Kondisyon = 90, RolId = 1,
                                           Talimatlar = Yuva() });
            for (int i = 0; i < yabanci; i++)
                list.Add(new PlayerState { PlayerId = pid++, ClubId = clubId + 1, HaftalikMaasTl = 12000,
                                           SozlesmeKalanHafta = 100, Moral = 60, Kondisyon = 90, RolId = 1,
                                           Talimatlar = Yuva() });
            for (int i = 0; i < serbest; i++)
                list.Add(new PlayerState { PlayerId = pid++, ClubId = 0, HaftalikMaasTl = 0,
                                           SozlesmeKalanHafta = 0, Moral = 50, Kondisyon = 80, RolId = 1,
                                           Talimatlar = Yuva() });
            st.Oyuncular = list.ToArray();
            st.Validate();
            return st;
        }

        /// <summary>Kadrodaki KENDİ oyuncumuzun ilk PlayerId'si.</summary>
        public static int IlkKendi(GameState st)
        {
            for (int i = 0; i < st.Oyuncular.Length; i++)
                if (st.Oyuncular[i].ClubId == st.Club.ClubId) return st.Oyuncular[i].PlayerId;
            return -1;
        }
        public static int IlkYabanci(GameState st)
        {
            for (int i = 0; i < st.Oyuncular.Length; i++)
                if (st.Oyuncular[i].ClubId != 0 && st.Oyuncular[i].ClubId != st.Club.ClubId) return st.Oyuncular[i].PlayerId;
            return -1;
        }
        public static int IlkSerbest(GameState st)
        {
            for (int i = 0; i < st.Oyuncular.Length; i++)
                if (st.Oyuncular[i].ClubId == 0) return st.Oyuncular[i].PlayerId;
            return -1;
        }
    }
}
