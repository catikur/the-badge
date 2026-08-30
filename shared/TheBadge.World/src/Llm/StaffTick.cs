using System;

namespace TheBadge.World
{
    /// <summary>PERSONEL YAŞAM DÖNGÜSÜ — `EconomyTick`/`MacTick`/`TransferTick` ile AYNI sözleşme:
    /// durumu doğrudan değiştirmez, journal'a yazar, host `Validate` + `Apply` yapar.
    ///
    /// NEDEN VAR (inceleme bulgusu, P1): `staff.hire` `KalanHafta` yazıyordu ama hiçbir şey onu
    /// AZALTMIYORDU. Süre dolduktan sonra personel aktif kalıyor, yuvayı KALICI olarak işgal
    /// ediyor ve aynı-tip kuralı o tipin bir daha alınmasını SONSUZA DEK engelliyordu. Bu, K4'te
    /// değişiklik hakkının maç başına dolmamasıyla aynı sınıf hata: yazılan ama hiç ilerletilmeyen
    /// bir sayaç.</summary>
    public static class StaffTick
    {
        /// <summary>Bir haftayı ilerletir: süreleri azaltır, biteni yuvadan düşürür.
        /// Dönen sayı SONA EREN sözleşme adedidir.</summary>
        public static int Hafta(GameState st, WorldJournal j)
        {
            if (st == null) throw new ArgumentNullException(nameof(st));
            if (j == null) throw new ArgumentNullException(nameof(j));

            int biten = 0;
            for (int i = 0; i < st.Club.Personel.Length; i++)
            {
                var pr = st.Club.Personel[i];
                if (pr.Tip == 0) continue;              // boş yuva
                if (pr.KalanHafta > 1)
                {
                    j.Set(MutTarget.Personel, i, StaffField.KalanHafta, pr.KalanHafta - 1);
                    continue;
                }
                // Süre doldu: yuva TAMAMEN boşalır. `Tip = 0` bırakmak yetmez — `Tier` ve
                // `KalanHafta` artıkları hash'te kalır ve iki farklı yoldan aynı kadroya varan
                // iki kayıt AYRIŞIR.
                j.Set(MutTarget.Personel, i, StaffField.Tip, 0);
                j.Set(MutTarget.Personel, i, StaffField.Tier, 0);
                j.Set(MutTarget.Personel, i, StaffField.KalanHafta, 0);
                j.Emit(new WorldEvent(WorldEventType.PersonelAyrildi, pr.Tip, pr.Tier,
                                      st.Takvim.Sezon, st.Takvim.Hafta));
                biten++;
            }
            return biten;
        }
    }
}
