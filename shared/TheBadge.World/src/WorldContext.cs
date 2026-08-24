using System;
using TheBadge.CommandBus;
using TheBadge.Sim.Commands;

namespace TheBadge.World
{
    /// <summary>Aksiyona ÖZGÜ Kapı 3 kuralı — K3-K5 doldurur (inşaat maliyeti, sponsor uygunluğu,
    /// personel envanteri...). K2 yalnız DURUMA dayalı yapısal denetimleri yapar ve bilmediği
    /// semantiği buraya devreder; kayıtlı kural yoksa yapısal denetim sonucu geçerlidir.</summary>
    public interface IActionRule
    {
        RejectionReason Check(GameState st, CommandEnvelope env, ActionDef action, IPayloadView payload, out string detail);
    }

    /// <summary>Oyuncu sahiplik gereksinimi — aksiyona göre DEĞİŞİR: kendi oyuncuna rol verirsin,
    /// BAŞKASININ oyuncusuna teklif yaparsın, SERBEST oyuncuyla sözleşme imzalarsın. Tek bir
    /// "oyuncu bizim mi" kuralı bu üçünü birden yanlış cevaplardı.</summary>
    enum OwnerNeed : byte { Yok = 0, Sahip = 1, Yabanci = 2, Serbest = 3 }

    /// <summary>KAPI 3 — "bağlam, sahiplik, kaynak, hak" (CB Spec 5). Dünya durumuna dayalı
    /// yapısal denetimler burada; ekonomik/semantik olanlar `IActionRule` ile K3-K5'ten gelir.
    ///
    /// Bu sınıf durumu DEĞİŞTİRMEZ — doğrulama saf okumadır. Yazma yolu yalnız
    /// `WorldExecutor`dır (Tek Kapı, CLAUDE.md değişmez #1).</summary>
    public sealed class WorldContext : IValidationContext
    {
        readonly GameState st;
        readonly WorldRules kural;
        readonly IActionRule[] ekKural;   // katalog indeksine göre; K3-K5 doldurur

        /// <summary>Host tarafından ayarlanır: şu an hangi bağlamlar açık (hub ekranı, maç,
        /// online oturum). Kapı 3'ün ilk ayağı bunu okur.</summary>
        public Context Active = Context.Hub;

        public WorldContext(GameState state, WorldRules rules)
        {
            st = state ?? throw new ArgumentNullException(nameof(state));
            kural = rules ?? throw new ArgumentNullException(nameof(rules));
            ekKural = new IActionRule[Catalog.Count];
        }

        /// <summary>K3-K5 kendi aksiyon kurallarını buraya bağlar. Bilinmeyen aksiyon adı
        /// SESSİZ GEÇMEZ: kablolama hatası kurulum anında görünür olur.</summary>
        public void RegisterRule(string actionType, IActionRule rule)
        {
            int i = CatalogIndex(actionType);
            if (i < 0) throw new ArgumentException("katalogda yok: " + actionType, nameof(actionType));
            ekKural[i] = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        static int CatalogIndex(string actionType)
        {
            var all = Catalog.Actions;
            for (int i = 0; i < all.Count; i++)
                if (string.Equals(all[i].ActionType, actionType, StringComparison.Ordinal)) return i;
            return -1;
        }

        public bool IsContextActive(Context context) => (Active & context) != Context.None;

        /// <summary>KARARLI takım kimliği — CB 5.1 maç içi limiti "10/dk/TAKIM". Zarftaki
        /// `TeamIdx` yalnız ev/deplasman'dır; kararlı kimlik kulüptür.</summary>
        public long ResolveTeamKey(CommandEnvelope env) => st.Club.ClubId;

        public RejectionReason CheckOwnershipAndState(CommandEnvelope env, ActionDef action, IPayloadView payload)
        {
            if (action == null) return RejectionReason.UnknownAction;

            // (1) KULÜP SAHİPLİĞİ — komutu veren kullanıcı bu kulübü yönetiyor mu.
            // Diğer her denetim bunun üstüne kuruludur: başkasının kulübünde "yeterli bakiye"
            // sorusunun anlamı yoktur.
            if (st.Club.OwnerUserId != env.UserId) return RejectionReason.NotOwned;

            // (2) OYUNCU SAHİPLİĞİ — aksiyonun gerektirdiği ilişkiye göre.
            var need = OwnerRequirement(action.ActionType, out string alan);
            if (need != OwnerNeed.Yok)
            {
                if (!payload.TryGetInt(alan, out long ham)) return RejectionReason.SchemaViolation;
                int pid = (int)ham;
                int idx = st.IndexOfPlayer(pid);
                if (idx < 0) return RejectionReason.NotOwned;          // var olmayan oyuncu
                long sahip = st.Oyuncular[idx].ClubId;
                switch (need)
                {
                    case OwnerNeed.Sahip: if (sahip != st.Club.ClubId) return RejectionReason.NotOwned; break;
                    case OwnerNeed.Yabanci: if (sahip == st.Club.ClubId) return RejectionReason.NotOwned; break;
                    case OwnerNeed.Serbest: if (sahip != 0) return RejectionReason.NotOwned; break;
                }
            }

            // (3) TRANSFER PENCERESİ — hangi aksiyonların pencere istediği KOD DEĞİL yapılandırma
            // kararıdır (`world.balance.json` → kapi3.pencereGerektiren); K5 kesinleştirir.
            if (kural.RequiresTransferWindow(action.ActionType) && !st.IsTransferWindowOpen())
                return RejectionReason.WindowClosed;

            // (4) MAÇ İÇİ HAK — CB 4.2 `match.substitution` (ME 14.2 değişiklik hakkı).
            if (string.Equals(action.ActionType, "match.substitution", StringComparison.Ordinal)
                && st.KalanDegisiklikHakki == 0)
                return RejectionReason.NoChargesLeft;

            // (5) İNŞAAT ÇAKIŞMASI — CB 8.2: "aynı tesise iki inşaat" StateConflict'tir;
            // sessiz üzerine yazma YOKTUR.
            if (string.Equals(action.ActionType, "tycoon.start_construction", StringComparison.Ordinal))
            {
                if (!payload.TryGetInt("tesisId", out long tesis)) return RejectionReason.SchemaViolation;
                if (st.HasConstructionFor((int)tesis)) return RejectionReason.StateConflict;
                if (st.FreeConstructionSlot() < 0) return RejectionReason.StateConflict;
            }
            if (string.Equals(action.ActionType, "tycoon.cancel_construction", StringComparison.Ordinal))
            {
                if (!payload.TryGetInt("insaatId", out long iid)) return RejectionReason.SchemaViolation;
                if (st.IndexOfConstruction((int)iid) < 0) return RejectionReason.StateConflict;
            }

            // (5b) KADRO ALT SINIRI — kadroyu oynanamaz hâle getiren fesih reddedilir.
            // Sınır [KALİBRE] (`world.balance.json` → yapi.kadroMin), kodda sabit değil.
            if (string.Equals(action.ActionType, "transfer.release_player", StringComparison.Ordinal)
                && KadroSayisi() <= kural.yapi.kadroMin)
                return RejectionReason.StateConflict;

            // (6) KAYNAK — yalnız payload'ın TUTARI AÇIKÇA BİLDİRDİĞİ aksiyonlar için.
            // Hesaplanan maliyetler (inşaat, sponsor, personel) K3-K5'in `IActionRule`'una aittir:
            // K2 bilmediği bir bedeli tahmin etmez.
            switch (action.ActionType)
            {
                case "tycoon.repay_loan":
                    {
                        if (!payload.TryGetInt("krediId", out long kid)) return RejectionReason.SchemaViolation;
                        if (st.IndexOfLoan((int)kid) < 0) return RejectionReason.StateConflict;
                        if (!payload.TryGetNumber("miktar", out double m)) return RejectionReason.SchemaViolation;
                        if (!st.CanAfford(WorldMoney.ToTl(m))) return RejectionReason.InsufficientFunds;
                        break;
                    }
                case "transfer.propose_offer":
                    {
                        if (!payload.TryGetNumber("bedel", out double b)) return RejectionReason.SchemaViolation;
                        if (!st.CanAfford(WorldMoney.ToTl(b))) return RejectionReason.InsufficientFunds;
                        break;
                    }
            }

            // (7) AKSİYONA ÖZGÜ KURAL — K3-K5.
            int ci = CatalogIndex(action.ActionType);
            var ek = ci >= 0 ? ekKural[ci] : null;
            if (ek != null) return ek.Check(st, env, action, payload, out _);

            return RejectionReason.None;
        }

        /// <summary>Kulübün kadro mevcudu.</summary>
        int KadroSayisi()
        {
            int n = 0;
            for (int i = 0; i < st.Oyuncular.Length; i++) if (st.Oyuncular[i].ClubId == st.Club.ClubId) n++;
            return n;
        }

        /// <summary>Aksiyonun oyuncu sahiplik gereksinimi ve hangi payload alanını okuduğu.</summary>
        static OwnerNeed OwnerRequirement(string actionType, out string alan)
        {
            switch (actionType)
            {
                case "squad.set_player_anchor":
                case "squad.set_player_role":
                case "squad.set_instruction":
                case "squad.set_captain":
                case "transfer.list_player":
                case "transfer.release_player":
                    alan = "oyuncuId"; return OwnerNeed.Sahip;
                case "transfer.propose_offer":
                    alan = "hedefOyuncuId"; return OwnerNeed.Yabanci;
                case "transfer.sign_free_agent":
                    alan = "oyuncuId"; return OwnerNeed.Serbest;
                default:
                    alan = null; return OwnerNeed.Yok;
            }
        }
    }
}
