namespace TheBadge.World
{
    /// <summary>Dünya olay tipi — sunum/bildirim/telemetri içindir. Numaralar KALICIDIR
    /// (kayıtlı save ve telemetri geriye uyumluluğu): yeni tip SONA eklenir, mevcut değer
    /// yeniden kullanılmaz.</summary>
    public enum WorldEventType : ushort
    {
        None = 0,
        KasaDegisti = 1,
        InsaatBasladi = 2,
        InsaatIptal = 3,
        KrediAlindi = 4,
        KrediOdendi = 5,
        OyuncuTransferi = 6,
        SozlesmeGuncellendi = 7,
        FiyatGuncellendi = 8,
        TaktikGuncellendi = 9,
        HaftaIlerledi = 10,
        SponsorImzalandi = 11,
        SponsorSonaErdi = 12,
        LigKuruldu = 13,
        LigeKatilindi = 14,
        LigKurallariDegisti = 15,
        PersonelAlindi = 16,
        PremiumAktif = 17,
        PersonelAyrildi = 18,
    }

    /// <summary>Dünya olayı — TEK YÖNLÜ: dünya mantığı bu kaydı ASLA OKUMAZ ve `WorldHash`e
    /// GİRMEZ. ME 15.1'de maç olay logu için kurulan kuralın dünya karşılığıdır: log bir ÇIKTIdır,
    /// girdi değil. Böylece "log değişti diye simülasyon değişti" sınıfı hatalar yapısal olarak
    /// imkânsızdır.</summary>
    public readonly struct WorldEvent
    {
        public readonly WorldEventType Type;
        public readonly int SubjectId;    // oyuncuId / tesisId / krediId — tipe göre
        public readonly long Value;       // tutar / yeni değer — tipe göre
        public readonly ushort Sezon, Hafta;

        public WorldEvent(WorldEventType type, int subjectId, long value, ushort sezon, ushort hafta)
        { Type = type; SubjectId = subjectId; Value = value; Sezon = sezon; Hafta = hafta; }
    }
}
