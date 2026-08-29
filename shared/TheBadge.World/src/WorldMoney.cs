using System;

namespace TheBadge.World
{
    /// <summary>Para birimi köprüsü. Kalıcı durum TAMSAYI ₺'dir (ME 3.2 mm disiplininin ekonomi
    /// karşılığı: kalıcı alanda float yok). Payload'daki tutarlar `ParamType.Number`dır — bantlar
    /// da tam ₺ cinsindendir (`transfer.bedel` [0, 500000000], `tycoon.krediMiktar` [10000,
    /// 5000000]), yani dönüşüm yalnız YUVARLAMAdır, ölçek değişimi değildir.</summary>
    public static class WorldMoney
    {
        /// <summary>Payload tutarını tamsayı ₺'ye çevirir. Yuvarlama SIFIRDAN UZAĞAdır: bankacı
        /// yuvarlaması (varsayılan) 0,5'i çift sayıya çeker ve para bağlamında kullanıcıya
        /// açıklanamaz; ayrıca tek bir kural her platformda aynı sonucu verir.</summary>
        public static long ToTl(double v) => (long)Math.Round(v, MidpointRounding.AwayFromZero);

        /// <summary>Payload fiyatını KURUŞ'a çevirir (1 ₺ = 100 kuruş). Fiyatlar kesirli olabilir
        /// (`command.bands.json` büfe bandı [0,5 - 50] ₺); kalıcı durum tamsayı olmak zorunda.</summary>
        public static int ToKurus(double v) => (int)Math.Round(v * 100.0, MidpointRounding.AwayFromZero);
    }
}
