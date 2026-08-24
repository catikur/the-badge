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
            for (int i = 0; i < kendi; i++)
                list.Add(new PlayerState { PlayerId = pid++, ClubId = clubId, HaftalikMaasTl = 10000,
                                           SozlesmeKalanHafta = 100, Moral = 60, Kondisyon = 90, RolId = 1 });
            for (int i = 0; i < yabanci; i++)
                list.Add(new PlayerState { PlayerId = pid++, ClubId = clubId + 1, HaftalikMaasTl = 12000,
                                           SozlesmeKalanHafta = 100, Moral = 60, Kondisyon = 90, RolId = 1 });
            for (int i = 0; i < serbest; i++)
                list.Add(new PlayerState { PlayerId = pid++, ClubId = 0, HaftalikMaasTl = 0,
                                           SozlesmeKalanHafta = 0, Moral = 50, Kondisyon = 80, RolId = 1 });
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
