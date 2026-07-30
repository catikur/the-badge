using System;

namespace TheBadge.Sim.Commands
{
    /// <summary>Komut kaynağı — CB Spec 2.2. AUTO v1'de kapalı.</summary>
    public enum CommandSource : byte { UI = 0, LLM = 1, Auto = 2 }

    /// <summary>Red sebepleri — CB Spec 11.1.</summary>
    public enum RejectionReason : byte
    {
        None = 0,
        UnknownAction, UnsupportedCatalogVersion, SchemaViolation,
        ParamOutOfBand, InsufficientFunds, NotOwned, WindowClosed,
        NoChargesLeft, RateLimited, DuplicateCommand, StateConflict, Banned
    }

    /// <summary>Tek Kapı komut zarfı — CB Spec 3.1. UI, LLM ve otomasyon aynı zarfı kullanır.</summary>
    public sealed record CommandEnvelope
    {
        public Guid CommandId { get; init; }          // istemci üretir; idempotency anahtarı
        public ushort CatalogVersion { get; init; }
        public CommandSource Source { get; init; }
        public string ActionType { get; init; }       // ör. "tycoon.set_ticket_price"
        public long IssuedAtUnixMs { get; init; }
        public uint MatchTick { get; init; }          // 0 = hub komutu
        public long UserId { get; init; }
        public int SaveSlotId { get; init; }
        public byte TeamIdx { get; init; }
        public byte[] PayloadJson { get; init; }
        public Guid? SuggestionId { get; init; }      // yalnız Source=LLM
    }
}
