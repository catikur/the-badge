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

        /// <summary>Deterministik kadro üretimi — Domain.Crowd (yalnız isim kozmetiği; skor akışına girmez).</summary>
        public static Squad Generate(ulong seed, int teamIdx, float initialEnergy)
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
                s.Players[i] = new SquadPlayer
                {
                    Id = i,
                    Name = Hece1[pick / Hece2.Length] + Hece2[pick % Hece2.Length],
                    Pos = Dizilim[i],
                    Energy = initialEnergy,
                    OnPitch = i < 11
                };
            }
            return s;
        }

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

        /// <summary>Eksik oyuncu sayısı (kırmızı / değiştirilmemiş sakatlık) — Eksik etkeninin girdisi.</summary>
        public int MissingCount()
        {
            int n = 0;
            for (int i = 0; i < Players.Length; i++)
                if (Players[i].OnPitch) n++;
            return Math.Max(0, 11 - n);
        }
    }
}
