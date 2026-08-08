namespace TheBadge.Sim.Config
{
    /// <summary>
    /// balance/sim.balance.json'ın çekirdek POCO'su. ÇEKİRDEK JSON PARSE ETMEZ — bağımlılıksızlık
    /// kuralı (CLAUDE.md): host (sunucu/Unity/Checks) kendi JSON aracıyla doldurur; alan adları
    /// JSON anahtarlarıyla BİREBİRDİR (System.Text.Json IncludeFields / Unity JsonUtility uyumu).
    /// Bu dosya config_hash İÇİDİR (ME Spec 3.3) — sezon içinde donuk.
    /// M1: yalnız attribute bölümü; sonraki dilimler kendi bölümlerini ekler.
    /// </summary>
    public sealed class SimBalance
    {
        public AttributeCfg attribute = new AttributeCfg();

        /// <summary>Efektif nitelik çarpanları — ME Spec 6.2 [KALİBRE].
        /// Varsayılanlar 0'dır: balance yüklenmemişse hesap GÖRÜNÜR bozulur (sessiz sapma yerine).</summary>
        public sealed class AttributeCfg
        {
            public double kondisyonTaban;          // M_kondisyon sabiti (0.70)
            public double kondisyonKuvvet;         // enerji bileşeni ağırlığı (0.30)
            public double kondisyonUs;             // (Energy/1000)^üs (0.7)
            public double moralCarpanPerMomentum;  // M_moral = 1 + Momentum × bu (0.005; ±5 puan tavan)
        }
    }
}
