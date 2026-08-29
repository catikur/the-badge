namespace TheBadge.World
{
    /// <summary>Bir haftanın gelir/gider dökümü — `docs/ECONOMY_MAP.md` source/sink tablosunun
    /// birebir karşılığı. Kapılar sezon boyunca bunu toplayıp sözleşmeyi ölçer
    /// (source/sink 1,05-1,15 · maaş payı %45-60).
    ///
    /// KREDİ NOTU: kredi ANAPARASI ne source ne sink'tir — bilanço aktarımıdır, para yaratmaz.
    /// ECONOMY_MAP yalnız "kredi faizi"ni sink sayar; bu yüzden `FaizTl` sink'e girer,
    /// anapara hareketi girmez. Aksi hâlde kredi çekmek oranı yapay olarak şişirirdi.</summary>
    public struct WeekLedger
    {
        // --- SOURCE ---
        public long BiletTl, KombineTl, BufeTl, MagazaTl, SponsorTl, YayinTl, PrimTl;
        // --- SINK ---
        public long MaasTl, BakimTl, PersonelTl, IsletmeTl, FaizTl;
        // --- bilgi ---
        public int Seyirci;
        public long AnaparaOdemeTl;   // bilanço aktarımı — source/sink'e GİRMEZ

        public long ToplamGelir => BiletTl + KombineTl + BufeTl + MagazaTl + SponsorTl + YayinTl + PrimTl;
        public long ToplamGider => MaasTl + BakimTl + PersonelTl + IsletmeTl + FaizTl;
        public long NetTl => ToplamGelir - ToplamGider - AnaparaOdemeTl;

        public void Topla(in WeekLedger o)
        {
            BiletTl += o.BiletTl; KombineTl += o.KombineTl; BufeTl += o.BufeTl; MagazaTl += o.MagazaTl;
            SponsorTl += o.SponsorTl; YayinTl += o.YayinTl; PrimTl += o.PrimTl;
            MaasTl += o.MaasTl; BakimTl += o.BakimTl; PersonelTl += o.PersonelTl;
            IsletmeTl += o.IsletmeTl; FaizTl += o.FaizTl;
            Seyirci += o.Seyirci; AnaparaOdemeTl += o.AnaparaOdemeTl;
        }
    }

    /// <summary>Haftalık maç sonucu — ekonomiye prim ve form olarak yansır.</summary>
    public enum WeekResult : byte { Yok = 0, Galibiyet = 1, Beraberlik = 2, Maglubiyet = 3 }
}
