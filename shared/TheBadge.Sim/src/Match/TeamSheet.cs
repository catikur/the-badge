using System;

namespace TheBadge.Sim.Match
{
    /// <summary>Kadro girdisi — ME Spec 5.2 TeamSheet üyesi. Name yalnız sunum verisidir ve
    /// MatchState'e GİRMEZ (kalıcı durum yalnız tamsayı, ME 3.2); kanonik PlayerId kimliktir
    /// (GDD kanonik ID mimarisi). Anchor: kullanıcının serbest diziliş çapası, mm (ME 5.3).</summary>
    public sealed class PlayerEntry
    {
        public short PlayerId;
        public string Name;
        public byte RoleId;                  // rol tablosu M2 karar diliminde anlamlanır (ME 7.4)
        public int AnchorXmm, AnchorYmm;
        public PlayerAttributes Attributes;  // 1-100 taban değerler — salt-okunur taşınır
    }

    /// <summary>Takım kadrosu — 11 ilk on bir (index 0 KALECİ konvansiyonu) + kulübe.
    /// Ajan slot eşlemesi: ev Starters[i] → Agents[i], deplasman Starters[i] → Agents[11+i].</summary>
    public sealed class TeamSheet
    {
        public PlayerEntry[] Starters;   // [11]
        public PlayerEntry[] Bench;      // 0..9 — değişiklik hakları M-müdahale diliminde (ME 14.2)

        /// <summary>Yapısal doğrulama — motor kurulumundan ÖNCE çağrılır; hata = kurulum reddi.</summary>
        public void Validate(string label)
        {
            if (Starters == null || Starters.Length != 11)
                throw new ArgumentException($"{label}: ilk 11 tam olarak 11 oyuncu olmalı.");
            var bench = Bench ?? Array.Empty<PlayerEntry>();
            for (int i = 0; i < Starters.Length; i++)
                if (Starters[i] == null) throw new ArgumentException($"{label}: Starters[{i}] boş.");
            for (int i = 0; i < bench.Length; i++)
                if (bench[i] == null) throw new ArgumentException($"{label}: Bench[{i}] boş.");
            // PlayerId benzersizliği — sırasız yapı KULLANILMAZ (ME 3.2): O(n²) küçük n için yeterli
            int total = Starters.Length + bench.Length;
            for (int i = 0; i < total; i++)
            {
                var pi = i < 11 ? Starters[i] : bench[i - 11];
                for (int j = i + 1; j < total; j++)
                {
                    var pj = j < 11 ? Starters[j] : bench[j - 11];
                    if (pi.PlayerId == pj.PlayerId)
                        throw new ArgumentException($"{label}: PlayerId {pi.PlayerId} tekrarlı.");
                }
            }
        }
    }
}
