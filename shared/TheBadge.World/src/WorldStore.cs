using System;

namespace TheBadge.World
{
    /// <summary>Dünya durumunun SAHİBİ: durum + onu koruyan kilit tek yerde.
    ///
    /// Neden ayrı bir tip (inceleme bulgusu, 2026-08-24 — HIGH): Kapı 3 durumu kilitsiz OKUYOR,
    /// yürütücü ise kendi kilidi altında YAZIYORDU. İki paralel `Submit` aynı bakiyeyi görüp
    /// ikisi de "yeterli" kararı alabiliyor, sonra sırayla yürütülüp çift harcama yapabiliyordu —
    /// yürütme kilidi yazmaları serileştiriyor ama KARARI korumuyordu (klasik TOCTOU).
    /// Kilidin duruma ait olması, okuyanla yazanın aynı kilidi paylaşmasını bir konvansiyon
    /// değil YAPISAL bir zorunluluk hâline getirir.</summary>
    public sealed class WorldStore
    {
        public readonly GameState State;
        internal readonly object Kilit = new object();

        public WorldStore(GameState state)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            State.Validate();
        }

        public ulong Hash() { lock (Kilit) return WorldHash.Compute(State); }
        public ulong Version { get { lock (Kilit) return State.StateVersion; } }
    }
}
