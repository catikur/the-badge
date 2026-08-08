using System;
using TheBadge.Sim.Determinism;

namespace TheBadge.Greybox.Sim
{
    public enum PlayerPos { GK = 0, DF = 1, MF = 2, FW = 3 }

    /// <summary>Maç içi olay türleri — ME Spec 11.2/12.2'nin blok ölçekli vekilleri (Öneri İt.11 A2).</summary>
    public enum IncidentType { Yellow = 0, SecondYellowRed = 1, RedDirect = 2, Injury = 3 }

    public struct Incident
    {
        public IncidentType Type;
        public int Team;          // 0 biz / 1 rakip
        public int PlayerId;
        public int Block;
        public bool AutoResolved; // rakip sakatlığı otomatik çözülür (değişiklik/eksik)
        public int AutoSubInId;   // rakip otomatik değişiklikte giren oyuncu (yoksa -1)
    }

    public sealed class SquadPlayer
    {
        public int Id;
        public string Name;
        public PlayerPos Pos;
        public float Guc;         // 0-100 bireysel güç — ME Spec 6.1 nitelik tablosunun greybox VEKİLİ (İt.12)
        public float Energy;      // 0..enerjiBaslangic — ME Spec 12.1 Energy'nin vekili
        public int Yellow;
        public bool SentOff;
        public bool Injured;
        public bool OnPitch;
        public int Goals;
    }

    /// <summary>
    /// Hafif isimli kadro — FAZ 03 bireysel oyuncu modelinin greybox VEKİLİ (Öneri İt.11 A3).
    /// Model olasılıkları TAKIM seviyesinde hesaplar; kadro yalnız enerji/kart/sakatlık/gol
    /// durumunu taşır ve karar anlarına isim verir. İsimler kurgusal evren kuralına uygun
    /// hece kombinasyonudur (GDD 9.x — gerçek oyuncu adı üretilmez, üretim deterministiktir).
    /// Dizilim: 0 GK · 1-4 DF · 5-8 MF · 9-10 FW · 11-15 yedek (DF, MF, MF, FW, FW).
    /// Greybox sadeleştirmesi: kaleci olaylara ve değişikliğe girmez (FAZ 03'te tam model).
    /// </summary>
    public sealed class Squad
    {
        public SquadPlayer[] Players; // 16

        // İçerik listesi (ayar değil): kurgusal soyad heceleri — [KALİBRE-G] kapsamı dışıdır.
        static readonly string[] Hece1 = { "Ak", "Kar", "Dem", "Öz", "Yıl", "Taş", "Boz", "Gün", "Er", "Kal", "San", "Dur", "Sar", "Tek", "Ay", "Kor" };
        static readonly string[] Hece2 = { "soy", "han", "dağ", "kan", "türk", "sel", "maz", "gül", "er", "al", "az", "tay", "kut", "ün", "baş", "ca" };

        static readonly PlayerPos[] Dizilim =
        {
            PlayerPos.GK,
            PlayerPos.DF, PlayerPos.DF, PlayerPos.DF, PlayerPos.DF,
            PlayerPos.MF, PlayerPos.MF, PlayerPos.MF, PlayerPos.MF,
            PlayerPos.FW, PlayerPos.FW,
            PlayerPos.DF, PlayerPos.MF, PlayerPos.MF, PlayerPos.FW, PlayerPos.FW
        };

        /// <summary>Deterministik kadro üretimi. İsimler Domain.Crowd (kozmetik); bireysel GÜÇLER
        /// Domain.Decision (dünya üretimi — OYNANIŞA girer, İt.12). İlk 11'in düz güç ortalaması
        /// takım tabanına NORMALİZE edilir: mevcut gol bandı/kalibrasyon bozulmaz (Öneri İt.12 S1.1).</summary>
        public static Squad Generate(ulong seed, int teamIdx, GreyboxBalance.SquadCfg cfg, float baseStrength)
        {
            var s = new Squad { Players = new SquadPlayer[16] };
            var used = new bool[Hece1.Length * Hece2.Length];
            for (int i = 0; i < 16; i++)
            {
                // İsim kozmetiktir — Domain.Crowd akışı, takım başına ayrı entity
                int pick = (int)(Rng.Rand01(seed, Domain.Crowd, (uint)(700 + teamIdx), (uint)i, 1)
                                 * (Hece1.Length * Hece2.Length));
                pick = Math.Min(pick, Hece1.Length * Hece2.Length - 1);
                while (used[pick]) pick = (pick + 1) % (Hece1.Length * Hece2.Length); // deterministik çakışma taraması
                used[pick] = true;
                float g = baseStrength
                          + (float)Rng.Gauss01(seed, Domain.Decision, (uint)(710 + teamIdx), (uint)i, 1) * cfg.gucYayilim
                          + (i >= 11 ? cfg.yedekGucFarki : 0f); // kulübe ortalamada zayıf — gerçek kadro dokusu
                s.Players[i] = new SquadPlayer
                {
                    Id = i,
                    Name = Hece1[pick / Hece2.Length] + Hece2[pick % Hece2.Length],
                    Pos = Dizilim[i],
                    Guc = Clamp(g, cfg.gucMin, cfg.gucMax),
                    Energy = cfg.enerjiBaslangic,
                    OnPitch = i < 11
                };
            }
            // Normalizasyon: ilk 11 ortalaması tabana çekilir (yedek farkı görece korunur)
            float mean11 = 0f;
            for (int i = 0; i < 11; i++) mean11 += s.Players[i].Guc;
            mean11 /= 11f;
            float d = baseStrength - mean11;
            for (int i = 0; i < 16; i++)
                s.Players[i].Guc = Clamp(s.Players[i].Guc + d, cfg.gucMin, cfg.gucMax);
            return s;
        }

        static float Clamp(float v, float lo, float hi) => v < lo ? lo : (v > hi ? hi : v);

        public SquadPlayer Find(int id) =>
            id >= 0 && id < Players.Length ? Players[id] : null;

        /// <summary>Sahadaki oyuncuların ortalama enerjisi (yorgunluk etkeninin girdisi).</summary>
        public float TeamEnergyMean()
        {
            float sum = 0f; int n = 0;
            for (int i = 0; i < Players.Length; i++)
                if (Players[i].OnPitch) { sum += Players[i].Energy; n++; }
            return n > 0 ? sum / n : 0f;
        }

        /// <summary>Eksik oyuncu sayısı (kırmızı / değiştirilmemiş sakatlık) — gösterim için.</summary>
        public int MissingCount()
        {
            int n = 0;
            for (int i = 0; i < Players.Length; i++)
                if (Players[i].OnPitch) n++;
            return Math.Max(0, 11 - n);
        }

        /// <summary>Mevki ağırlıklı takım reytingi (İt.12 — Öneri S1.2): sahadakilerin
        /// güç × bireysel yorgunluk çarpanı katkısı. Payda TAM 11 slotun ağırlık toplamıdır —
        /// eksik oyuncu 0 katkı verir ama paydada kalır: kayıp, oyuncunun kalitesiyle orantılı
        /// acıtır (eski Yorgunluk/Eksik etkenleri bu yapının İÇİNE taşındı, çifte sayım yok).
        /// slopePerBlock: mevcut drenajla blok başına reyting düşüşü — DP projeksiyonu girdisi.</summary>
        public void RatingAndSlope(bool attack, GreyboxBalance.SquadCfg cfg, float drainRate,
                                   out float rating, out float slopePerBlock)
        {
            float wFull = 0f;
            for (int i = 0; i < 11; i++)
                wFull += Weight(attack, Dizilim[i], cfg);
            float sum = 0f, slopeSum = 0f;
            float k = (1f - cfg.yorgunlukGucTaban) / cfg.enerjiBaslangic;
            for (int i = 0; i < Players.Length; i++)
            {
                var p = Players[i];
                if (!p.OnPitch) continue;
                float w = Weight(attack, p.Pos, cfg);
                float fmul = cfg.yorgunlukGucTaban + (1f - cfg.yorgunlukGucTaban) * (p.Energy / cfg.enerjiBaslangic);
                sum += p.Guc * fmul * w;
                slopeSum += p.Guc * w * k * drainRate * (p.Pos == PlayerPos.GK ? cfg.gkDrenajCarpan : 1f);
            }
            rating = wFull > 0f ? sum / wFull : 0f;
            slopePerBlock = wFull > 0f ? slopeSum / wFull : 0f;
        }

        static float Weight(bool attack, PlayerPos pos, GreyboxBalance.SquadCfg cfg) =>
            (attack ? cfg.hucumAgirlik : cfg.savunmaAgirlik)[(int)pos];
    }
}
