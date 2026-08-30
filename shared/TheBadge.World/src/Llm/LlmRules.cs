using System;

namespace TheBadge.World
{
    /// <summary>`balance/llm.balance.json` karşılığı — CB 7.1 girdi temizliği eşikleri.
    /// config_hash DIŞIdır: bunlar oyun mekaniğini değil GİRDİ HİJYENİNİ ayarlar, replay'i
    /// etkilemez (`balance/llm.budget.json` ile aynı sınıf — CLAUDE.md klasör haritası).</summary>
    [Serializable]
    public sealed class LlmRules
    {
        public int surum;
        public GirdiCfg girdi = new GirdiCfg();

        [Serializable] public sealed class GirdiCfg
        {
            public int maxKarakter;        // [KALİBRE] CB 7.1 "≤ 500 karakter"
            public double tekrarSpamOrani; // [KALİBRE] en sık karakterin oranı bu değere ULAŞIRSA spam
        }

        public void Validate()
        {
            if (girdi.maxKarakter <= 0) throw new ArgumentException("llm.balance: girdi.maxKarakter > 0 olmalı.");
            // Oran (0,1] olmalı: 0 her girdiyi spam sayar, 1'in üstü hiçbirini yakalayamaz.
            if (girdi.tekrarSpamOrani <= 0 || girdi.tekrarSpamOrani > 1)
                throw new ArgumentException("llm.balance: girdi.tekrarSpamOrani (0,1] olmalı.");
        }
    }
}
