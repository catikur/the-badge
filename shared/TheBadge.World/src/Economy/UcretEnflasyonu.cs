using System;

namespace TheBadge.World
{
    /// <summary>SEZON BAŞI ÜCRET GÖZDEN GEÇİRMESİ — K13-A'nın ikinci kolu (Atilla kararı,
    /// 2026-09-01: "(a) maaş enflasyonu + (c) gelir doygunluğu").
    ///
    /// NEDEN VAR: ECONOMY_MAP source/sink bandı (1,05-1,15) merdiven SONRASI tutmuyordu ve sebebi
    /// ölçüldü — merdiven bitince kapasite üçe katlanıyor, gelir 1,73 → 3,14 milyar ₺'ye çıkıyor,
    /// **gider ise 1,67 → 1,58 milyar ₺'ye DÜŞÜYOR.** Hiçbir sink gelirle ölçeklenmiyordu.
    /// ECONOMY_MAP'in kendi tablosunda maaş "en büyük sink" diye yazılı; büyüyen bir kulübün
    /// ücret yükünün sabit kalması o tablonun kendi mantığıyla çelişiyordu.
    ///
    /// NE MODELLENİYOR: her sezon başı kadro ücretleri, kulübün BUGÜNKÜ ölçeğindeki talebe doğru
    /// çekilir. Ölçek `EconomyTick.KulupOlcegi` ile ETKİN kapasiteden gelir — yani doygunluk kolu
    /// geliri kısarken ücret baskısı da kısılır; iki kol aynı büyüklüğe bağlıdır.
    ///
    /// NE MODELLENMİYOR (bilerek): oyuncunun pazarlık etme, reddetme ya da ayrılma hakkı. Bugünkü
    /// dünya tek kulüplü; ücret talebini bir MÜZAKEREYE çevirmek karşı taraf ekonomisi gerektirir
    /// (DECISIONS, K12-C). Buradaki model, sink'in gelirle ölçeklenmesi için gereken asgari
    /// gerçekliktir — daha fazlasını uydurmak ölçümü varsayıma çevirirdi.
    ///
    /// KADEMELİ: tek sezonda en çok `sezonlukEnUstDegisim` kadar oynar. Ölçek bir sıçramayla
    /// büyüdüğünde (yeni tribün açıldığında) ücretlerin bir gecede iki katına çıkması hem
    /// gerçekçi değil hem de kulübü bir sezonda iflasa sürüklerdi.
    ///
    /// TEK KAPI: durumu doğrudan değiştirmez — `WorldJournal`a yazar, `EconomyTick` ile aynı
    /// sözleşme (bu bir oyuncu KOMUTU değil, haftalık/sezonluk dünya işleyişi).</summary>
    public static class UcretEnflasyonu
    {
        /// <summary>Sezon başı ücret gözden geçirmesi. Kulübün kadrosundaki her oyuncunun maaşını
        /// bugünkü ölçekteki talebe doğru (kademeli olarak) çeker ve kulübün haftalık maaş
        /// giderini yeniden toplar.
        ///
        /// DÖNEN DEĞER: gözden geçirme SONRASI haftalık toplam maaş gideri. Çağıran bunu haftanın
        /// kalemine yazmalıdır — journal yazmaları henüz uygulanmadığı için `st.Club`'a bakmak
        /// ESKİ toplamı verirdi ve enflasyon bir hafta geç görünürdü.</summary>
        public static long SezonBasi(GameState st, EconomyBalance eco, TransferBalance tb,
                                     WorldJournal j)
        {
            if (st == null) throw new ArgumentNullException(nameof(st));
            if (eco == null) throw new ArgumentNullException(nameof(eco));
            if (tb == null) throw new ArgumentNullException(nameof(tb));
            if (j == null) throw new ArgumentNullException(nameof(j));

            double olcek = EconomyTick.KulupOlcegi(st, eco);
            double enUst = eco.doygunluk.sezonlukEnUstDegisim;
            long yeniToplam = 0;

            for (int i = 0; i < st.Oyuncular.Length; i++)
            {
                var p = st.Oyuncular[i];
                if (p.ClubId != st.Club.ClubId) continue;

                long mevcut = p.HaftalikMaasTl;
                long talep = Valuation.MaasTalebi(p, tb, olcek);

                // YALNIZ YUKARI. İlk yazımda maaşı doğrudan talebe EŞİTLİYORDUM ve bu bir
                // enflasyon değil bir YENİDEN TÜRETMEydi: fikstürün başlangıç ücretleri modelin
                // kendi `MaasTalebi` formülünden yüksekti, dolayısıyla ilk sezon başında bütün
                // kadro ucuzluyordu. `K3EkonomiSozlesmesi` bunu anında yakaladı — maaş payı
                // %50 → %36,6, oran 1,47 (bant [1,05-1,15] dışı) ve `K3IflasEgrisi` de kırmızı:
                // kötü yönetimin iflası artık gelmiyordu. Mekanizmanın işi ücretleri kulüp
                // büyüdükçe YUKARI çekmek; taban ücretleri yeniden pazarlık etmek DEĞİL.
                // (Gerçek hayatta da sözleşme ortasında ücret kesilmez.)
                long yeni = talep > mevcut ? talep : mevcut;

                // KADEMELİ: tek sezonda en çok `sezonlukEnUstDegisim` kadar. Yeni bir tribün
                // açıldığında ölçek sıçrar; ücretlerin bir gecede sıçraması hem gerçekçi değil
                // hem de kulübü tek sezonda iflasa sürüklerdi.
                long tavan = mevcut + (long)(mevcut * enUst);
                if (yeni > tavan) yeni = tavan;
                if (yeni < 0) yeni = 0;

                if (yeni != mevcut) j.OyuncuSet(i, PlayerField.HaftalikMaas, yeni);
                yeniToplam += yeni;
            }

            if (yeniToplam != st.Club.HaftalikMaasGiderTl)
                j.Set(MutTarget.Kulup, 0, ClubField.HaftalikMaasGider, yeniToplam);
            return yeniToplam;
        }
    }
}
