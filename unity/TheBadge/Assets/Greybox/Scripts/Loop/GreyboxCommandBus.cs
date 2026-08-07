using System;
using TheBadge.Greybox.Sim;
using TheBadge.Sim.Commands;

namespace TheBadge.Greybox.Loop
{
    /// <summary>
    /// Tek Kapı'nın greybox uygulaması — CB Spec 3.1 zarfı gerçek, doğrulama HAFİF.
    /// CB Spec 5-7'deki 4 kapılı tam doğrulama (şema/bant/durum/oran) FAZ 04 borcudur;
    /// burada bant + bilinen-aksiyon kontrolü var. Amaç: UI'ın durumu asla doğrudan
    /// yazmadığı desenin ilk günden kurulması (CLAUDE.md Mimari Değişmez 1).
    /// </summary>
    public sealed class GreyboxCommandBus
    {
        public const ushort CatalogVersion = 1;

        public const string ActSelectTactic = "greybox.select_tactic";
        public const string ActSetTicketPrice = "tycoon.set_ticket_price"; // CB Spec 3.1 örnek aksiyonuyla aynı ad
        public const string ActNextMatch = "greybox.next_match";
        public const string ActModelTactic = "model.set_tactic";   // maç içi müdahale (Model Maçı)
        public const string ActModelTempo = "model.set_tempo";     // 0 normal / 1 yükselt / 2 kilitlen
        public const string ActModelSub = "model.substitution";    // CB Spec katalog adıyla aynı (İt.11)
        public const string ActModelContinueShort = "model.continue_short"; // sakatlıkta eksik devam kararı

        readonly GreyboxBalance bal;
        readonly GreyboxState state;

        /// <summary>Aktif Model Maçı — maç sürerken Bootstrap atar; müdahaleler buna yönlenir.</summary>
        public MatchModel ActiveModel;

        /// <summary>Başarıyla uygulanan her komut sonrası çağrılır (save + telemetri kancası).</summary>
        public event Action<CommandEnvelope> Applied;

        public GreyboxCommandBus(GreyboxBalance bal, GreyboxState state)
        {
            this.bal = bal;
            this.state = state;
        }

        /// <summary>UI kolaylığı: zarfı kur ve uygula.</summary>
        public RejectionReason Send(string actionType, byte[] payloadJson)
        {
            var env = new CommandEnvelope
            {
                CommandId = Guid.NewGuid(),   // istemci idempotency anahtarı (sim dışı — determinizm kuralı ihlali değil)
                CatalogVersion = CatalogVersion,
                Source = CommandSource.UI,
                ActionType = actionType,
                IssuedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                MatchTick = 0,                // greybox komutları hub komutudur
                UserId = 1,
                SaveSlotId = 1,
                TeamIdx = 0,
                PayloadJson = payloadJson ?? Array.Empty<byte>(),
                SuggestionId = null
            };
            return Apply(env);
        }

        public RejectionReason Apply(CommandEnvelope env)
        {
            if (env.CatalogVersion != CatalogVersion) return RejectionReason.UnsupportedCatalogVersion;

            RejectionReason result;
            switch (env.ActionType)
            {
                case ActSelectTactic:
                {
                    if (!GreyboxJson.TryGetNumber(env.PayloadJson, "tacticId", out double id))
                        return RejectionReason.SchemaViolation;
                    int t = (int)id;
                    bool known = false;
                    for (int i = 0; i < bal.taktikler.Length; i++)
                        if (bal.taktikler[i].id == t) known = true;
                    if (!known) return RejectionReason.ParamOutOfBand;
                    state.tacticId = t;
                    result = RejectionReason.None;
                    break;
                }
                case ActSetTicketPrice:
                {
                    if (!GreyboxJson.TryGetNumber(env.PayloadJson, "price", out double price))
                        return RejectionReason.SchemaViolation;
                    if (price < bal.ekonomi.fiyatMin || price > bal.ekonomi.fiyatMax)
                        return RejectionReason.ParamOutOfBand; // bant dışı fiyat reddedilir (CB Spec bant kapısı)
                    state.ticketPrice = (float)price;
                    result = RejectionReason.None;
                    break;
                }
                case ActNextMatch:
                {
                    state.matchIndex++;
                    result = RejectionReason.None;
                    break;
                }
                case ActModelTactic:
                {
                    // Maç içi taktik müdahalesi — hamle hakkı biterse NoChargesLeft (CB Spec 11.1)
                    if (ActiveModel == null) return RejectionReason.StateConflict;
                    if (!GreyboxJson.TryGetNumber(env.PayloadJson, "tacticId", out double mid))
                        return RejectionReason.SchemaViolation;
                    if (ActiveModel.MovesLeft <= 0) return RejectionReason.NoChargesLeft;
                    if (!ActiveModel.TrySetTactic((int)mid)) return RejectionReason.StateConflict;
                    state.tacticId = (int)mid; // sonraki maçın ön seçimi de güncellensin
                    result = RejectionReason.None;
                    break;
                }
                case ActModelTempo:
                {
                    if (ActiveModel == null) return RejectionReason.StateConflict;
                    if (!GreyboxJson.TryGetNumber(env.PayloadJson, "mode", out double mode))
                        return RejectionReason.SchemaViolation;
                    if (mode < 0 || mode > 2) return RejectionReason.ParamOutOfBand;
                    if (ActiveModel.MovesLeft <= 0) return RejectionReason.NoChargesLeft;
                    if (!ActiveModel.TrySetTempo((TempoMode)(int)mode)) return RejectionReason.StateConflict;
                    result = RejectionReason.None;
                    break;
                }
                case ActModelSub:
                {
                    // Oyuncu değişikliği — hak sayısı AYRI havuz (değişiklik ≠ hamle; GDD 12.4)
                    if (ActiveModel == null) return RejectionReason.StateConflict;
                    if (!GreyboxJson.TryGetNumber(env.PayloadJson, "out", out double outId) ||
                        !GreyboxJson.TryGetNumber(env.PayloadJson, "in", out double inId))
                        return RejectionReason.SchemaViolation;
                    if (outId < 0 || outId > 15 || inId < 0 || inId > 15)
                        return RejectionReason.ParamOutOfBand;
                    if (ActiveModel.SubsLeft <= 0) return RejectionReason.NoChargesLeft;
                    if (!ActiveModel.TrySubstitute((int)outId, (int)inId)) return RejectionReason.StateConflict;
                    result = RejectionReason.None;
                    break;
                }
                case ActModelContinueShort:
                {
                    // Sakatlıkta "eksik devam" — yalnız bekleyen karar varken anlamlı
                    if (ActiveModel == null) return RejectionReason.StateConflict;
                    if (!ActiveModel.TryContinueShort()) return RejectionReason.StateConflict;
                    result = RejectionReason.None;
                    break;
                }
                default:
                    return RejectionReason.UnknownAction;
            }

            if (result == RejectionReason.None) Applied?.Invoke(env);
            return result;
        }
    }
}
