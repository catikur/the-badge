using System;

namespace TheBadge.Sim.Match
{
    /// <summary>KRİTİK AN DEDEKTÖRÜ — sunumun "ne zaman duraklayacağı" (5G S2, Atilla K1 kararı
    /// 2026-09-05: sunum ritmi (b), motor sürekli koşar ama sunum kritik anlarda durur/vurgular).
    ///
    /// NEDEN ME 15.3'ÜN EŞİĞİ KULLANILMIYOR: `H > highlight.esik` ölçümde maç başına 0,5-0,8
    /// işaret veriyor ve **maçların yarısı BOŞ** (bu, `MatchSummaryPacket.TimelineMarks`ın
    /// eşikten değil "en yüksek N"den beslenmesinin de sebebiydi). Sıfır ya da bir duraklamalı
    /// bir maç, ritim değil. "En yüksek N" ise CANLI kullanılamaz: maç bitmeden kimin ilk altıda
    /// olduğu bilinemez.
    ///
    /// KULLANILAN ÖLÇÜT: kazanma olasılığının SIÇRAMASI — son duraklamadan bu yana toplam değişim
    /// mesafesi (L1/2). Bu, döngünün kendi vaadine bağlı: duraklama tam da sonucun maddi olarak
    /// kaydığı anda olur. ÖLÇÜLDÜ (200 maç, karışık güç dağılımı):
    ///
    ///   eşik 0,02 → 19,0 an/maç · 0,03 → 12,9 · **0,04 → 9,9** · 0,05 → 8,1 · 0,10 → 4,5
    ///
    /// 0,04 seçildi: greybox'ın 8-12 blokluk ritmine oturuyor ve **hiçbir maç boş kalmıyor (%0)**.
    ///
    /// KADANSTAN BAĞIMSIZ (ölçüldü, varsayılmadı): taban yalnız ATEŞLENDİĞİNDE sıfırlanır, o yüzden
    /// daha sık örnekleme aynı sıçramayı daha ERKEN yakalar, DAHA ÇOK değil. 1 sn'den 30 sn'ye
    /// 30 kat aralıkta sayı 10,0 / 9,9 / 9,9 / 9,7 / 9,5. Sunum kare hızını seçmekte serbesttir.
    ///
    /// DURUMU YOK: bu bir sunum aracıdır, `MatchState`e dokunmaz ve maç sonucunu etkilemez.</summary>
    public struct KritikAnDedektoru
    {
        double ev, be, de;
        bool kuruldu;

        /// <summary>Tabanı bugünkü olasılığa çeker. Maç başında ve her duraklamadan sonra
        /// otomatik çağrılır; dışarıdan yalnız yeniden başlatmak için gerekir.</summary>
        public void Sifirla(in LiveWinProb.Sonuc u)
        {
            ev = u.Ev; be = u.Beraberlik; de = u.Deplasman; kuruldu = true;
        }

        /// <summary>Kritik an mı? Öyleyse taban buraya çekilir (ardışık tetiklenme olmaz).
        /// `sicrama` son duraklamadan bu yana toplam değişim mesafesidir (0-1).</summary>
        public bool Kontrol(in LiveWinProb.Sonuc u, double esik, out double sicrama)
        {
            if (!kuruldu) { Sifirla(in u); sicrama = 0; return false; }
            sicrama = 0.5 * (Math.Abs(u.Ev - ev) + Math.Abs(u.Beraberlik - be) + Math.Abs(u.Deplasman - de));
            if (sicrama < esik) return false;
            Sifirla(in u);
            return true;
        }
    }
}
