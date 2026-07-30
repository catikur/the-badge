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

        readonly GreyboxBalance bal;
        readonly GreyboxState state;

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
                default:
                    return RejectionReason.UnknownAction;
            }

            if (result == RejectionReason.None) Applied?.Invoke(env);
            return result;
        }
    }
}
