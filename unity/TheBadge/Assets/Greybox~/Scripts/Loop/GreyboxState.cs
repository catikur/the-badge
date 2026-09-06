using System;
using TheBadge.Greybox.Sim;

namespace TheBadge.Greybox.Loop
{
    /// <summary>
    /// Greybox oyun durumu. DEĞİŞMEZ KURAL (CLAUDE.md Tek Kapı): durumu değiştiren
    /// KULLANICI eylemleri yalnız GreyboxCommandBus.Apply üzerinden gelir; UI doğrudan yazamaz.
    /// Maç sonucunun kasaya işlenmesi (Settle) kullanıcı eylemi değil sim çıktısıdır —
    /// CB Spec kapsamı dışında sistem akışıdır, FAZ 04'te sunucu-otoriter akışa taşınır.
    /// </summary>
    [Serializable]
    public sealed class GreyboxState
    {
        public long money;
        public float ticketPrice;
        public int matchIndex = 1;        // 1 tabanlı: sıradaki maç günü
        public int tacticId;
        public int[] lastResults = new int[0];  // en yenisi sonda; 1/0/-1
        public int sessionCount;
        public long worldSeed;            // maç günü üretim tohumu; 0 = henüz atanmadı (Bootstrap atar)

        public static GreyboxState NewGame(GreyboxBalance bal)
        {
            return new GreyboxState
            {
                money = bal.ekonomi.baslangicPara,
                ticketPrice = bal.ekonomi.refFiyat,
                matchIndex = 1,
                tacticId = 0,
                lastResults = new int[0],
                sessionCount = 0
            };
        }

        /// <summary>Maç sonucunu işler: gelir kasaya, sonuç form penceresine (son 5).</summary>
        public IncomeProjection Settle(GreyboxBalance bal, int result)
        {
            var proj = TycoonEconomy.Project(bal.ekonomi, ticketPrice, lastResults, result);
            money += proj.Total;

            int n = lastResults.Length;
            int keep = Math.Min(n, 4);
            var next = new int[keep + 1];
            Array.Copy(lastResults, n - keep, next, 0, keep);
            next[keep] = result;
            lastResults = next;
            return proj;
        }
    }
}
