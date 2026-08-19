namespace TheBadge.Sim.Config
{
    /// <summary>
    /// LOD 2 TABLOSU — ME Spec 16.1: "LOD 2 tabloları, LOD 0 ile koşulan kalibrasyon maçlarından
    /// regresyonla türetilir; her balance güncellemesinde yeniden üretim CI adımıdır."
    /// <para><b>Neden parametrik formül değil ızgara:</b> önce λ = exp(b0 + b1·d) denendi, sonra
    /// |d| terimi (dengesiz maçta İKİ takım da çok şut çekiyor), sonra seviye terimi eklendi.
    /// Üçü de LOD 0'ın tepki yüzeyini ±%25 içinde tutamadı. Sebep: yüzey hem dik hem eğri
    /// (M15 ölçümü: ~20 puanlık kadro üstünlüğü 10-0'a gidiyor) ve global bir fonksiyon biçimi
    /// ona oturmuyor. Izgara + iki doğrusal ara değerleme YAPISI GEREĞİ oturur, ek varsayım
    /// taşımaz — ve spec'in kelimesi de zaten "tablo"dur.</para>
    /// <para>ÜRETİLMİŞ VERİDİR, elle ayarlanmaz — bu yüzden `sim.balance.json`'dan ayrı dosyada
    /// yaşar. Elle ayarlanan tek şey güç bileşiminin ağırlıklarıdır ve o `sim.balance.json` →
    /// `lod.guc` altındadır. İkisini karıştırmak "hangi sayı kararla, hangisi ölçümle geldi"
    /// ayrımını yok ederdi.</para>
    /// Üretici: <c>dotnet run --project shared/TheBadge.Sim.Checks -c Release -- fit-lod2</c>
    /// </summary>
    [System.Serializable]
    public sealed class Lod2Table
    {
        public int kaynakMacSayisi;     // ızgarayı besleyen LOD 0 maç sayısı (denetim izi)
        public int hucreBasinaMac;      // hücre başına örneklem (gürültü payı için)

        /// <summary>Takım gücü ekseni (0-100 ölçeği), ARTAN. Izgara İKİ kez bu ekseni kullanır:
        /// bir kez kendi gücü, bir kez rakibin gücü.
        /// <para><b>Neden (kendi, rakip) ve (fark, seviye) değil:</b> önce (d = fark, m = seviye)
        /// eksenleri denendi ve ±%25 kapısını geçemedi. Sebep, ölçümle bulundu: aynı FARK farklı
        /// SEVİYEDE aynı maçı vermiyor ve m'ye duyarlılık o kadar yüksek ki (0,07'lik m değişimi
        /// gol sayısını %80 oynattı) kaba bir m ekseni yetmiyordu. (kendi, rakip) fiziksel
        /// sürücülerin KENDİSİdir — döndürme yok, kuplaj yok, ara değerleme doğal koordinatta.</para></summary>
        public double[] gucEkseni = new double[0];

        // Izgara değerleri — TAKIM BAŞINA ortalama; düzleştirme: indeks = kendiIdx * n + rakipIdx.
        // Eksen dışında KIRPILIR (dışdeğerleme yok): eğrinin uçlarda ne yaptığı ölçülmemiştir.
        public double[] gol = new double[0];
        public double[] sut = new double[0];
        public double[] isabetliSut = new double[0];
        public double[] korner = new double[0];
        public double[] faul = new double[0];
        public double[] sari = new double[0];
        public double[] kirmizi = new double[0];
        public double[] xg = new double[0];
    }
}
