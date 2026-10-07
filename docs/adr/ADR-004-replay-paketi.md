# ADR-004: Golden replay paketi — determinizm kanıtı Unity'de de aynı koddan
**Durum:** Accepted (Atilla onayı, 2026-10-07 — "önerdiğin gibi (b)'ye başla") · **Tarih:** 2026-10-07

**Bağlam:** P0 (a) .NET ölçeğinde kapandı: Linux x64 ile macOS arm64, 50 golden replay'de bit-eşit.
Gerçek istemci ise Unity'dir: Editor'de Mono, cihazda IL2CPP. ME 17.1 çok platformlu determinizm
kapısı ister ve bugün Unity tarafında hiçbir şey ölçülmüyor (DECISIONS, *P0 ikinci aşaması*).
Ölçümün önündeki engel bir paket sınırı:
- **Replay kurulumu Checks'in içinde:** `BuildReplay`, `BuildSheetSide`, `RunReplay`, `ReplayKaydi`
  ve P0 pin hesapları `shared/TheBadge.Sim.Checks/Program.cs`'in üst düzey kodunda.
- **Checks bir .NET 8 konsol programı:** Unity onu derlemez, göremez.

Aynı 50 maçı Unity'de koşmak için bu kodun bir kopyası yazılsaydı, "üretici ile kapı farklı
evreni ölçer" hatası (M17'nin tek kaynak ilkesi) platformlar arasına taşınırdı. Anayasa 4G.9 yeni
Unity paketini ADR'ye bağlar; bu kayıt o gereğin karşılığıdır.

**Karar:** Yeni yerel paket `com.thebadge.sim.replay` (`shared/TheBadge.Sim.Replay`), ADR-002'nin
deseniyle: `package.json` + kök `.asmdef` (`noEngineReferences: true`) + Unity manifest'inde `file:`
girdisi + `Directory.Build.props` (çıktı repo kökündeki `artifacts/`e). netstandard2.1 / C# 9, dış
paket YOK. Tek referansı `TheBadge.Sim`. İçeriği:
1. **Golden replay kurulumu ve oynatıcı:** test kadrosu, 50 replay'in kurulumu, oynatma, sekiz
   alanın kanonik kaydı. Checks ve Unity AYNI fonksiyonu çağırır.
2. **Golden set okuyucu:** küçük, bağımlılıksız bir JSON okuyucu. System.Text.Json Unity'de yok;
   Unity'nin kendi okuyucusu pakete giremez.
3. **P0 pinleri:** DetMath'in yedi fonksiyonunun ölçüm ızgarası ve çıktı özetleri, TrigLut özeti,
   dönüşüm fikstürleri. Pin değerleri tek yerde, pakette. Izgaranın GİRDİLERİ de ayrıca pinlenir:
   `taban + ölçek·U` bir çarpma-toplamadır ve bir derleyici onu birleştirirse (FMA) önce girdiler
   kayar — ayrı pin, sapmanın girdide mi DetMath'te mi olduğunu ayırır.
4. **Balance değer dökümü:** okunmuş `SimBalance`'ın her alanını (yansımayla, ada göre sıralı) bit
   düzeyinde döken kanonik metin. Sunucunun dökümü `goldens/` altında üretilir; Unity kendi
   okuduğu balance'ın dökümünü onunla karşılaştırır.
5. **Determinizm sondası:** üçünü birleştiren tek çağrı. Girdisi balance, balance ve bant
   dosyalarının baytları, golden set ve döküm; çıktısı tek bir rapor. Unity tarafı yalnız
   girdileri yükleyip bunu çağırır.

**Neden balance dökümü (bu kararın en önemli eki):** Unity balance'ı `JsonUtility` ile okuyor
(`BalansKaynagi`), sunucu `System.Text.Json` ile. .NET'in sayı ayrıştırması doğru yuvarlanır; Unity'nin
yerel ayrıştırıcısınınki belgelenmemiş. Bir katsayı son bitte farklı okunursa istemci simi ilk
tick'ten ayrışır — kod bit bit aynı olsa bile. Döküm bu sapmayı yürütme sapmasından AYIRIR ve
hangi alanda olduğunu gösterir.

**Unity tarafı (ince yapıştırıcı, mantık pakette):**
- `Game.Match.EditModeTests`'e bir test: balance'ı oyunun kendi yolundan (`BalansKaynagi.Yukle`)
  okur ve sondayı Mono'da koşar.
- `Assets/Determinizm/` altında bir IL2CPP sondası. Yalnız sonda build'ine paketlenen veri klasörü
  (`StreamingAssets/DeterminizmSondasi`) varken çalışır; normal build'de ve Editor'de hiçbir şey
  yapmaz, ama Editor'de her açılışta derlenir (derleme sembolü kullanılmadı: sembolle dışlanan kod
  ancak sonda build'i alınınca derleyiciden geçerdi).
- Bir Editor menüsü: macOS standalone IL2CPP build'i alır, oyuncuyu arm64 diliminde (`arch -arm64`)
  çalıştırır, raporu konsola basar. Proje ayarlarını, açık sahneleri ve geçici dosyaları build'den
  sonra geri yükler.
- `link.xml`: IL2CPP'nin kod budaması balance alanlarını silemesin diye.

**Gerekçe (elenen seçeneklerle birlikte):**
- **Replay kodunu `TheBadge.Sim`'in içine koymak** elendi: çekirdeğe test fikstürü (test
  kadrosu, sabit tohumlu 50 kurulum) taşır. Çekirdeğin sözleşmesi oyunun kendisidir.
- **Unity'de ayrı bir kopya** elendi: tek kaynak ilkesini bozar. İki kopyanın ayrıştığını ancak
  biri sapınca görürdük, yani ölçmek istediğimiz şeyi ölçemezdik.
- **Golden okuyucu olarak Unity'nin `JsonUtility`'si** elendi: paket motordan bağımsız kalmalı. Ayrıca
  okuyucu ile ölçülen şeyin (balance ayrıştırması) aynı araç olması sapmayı gizlerdi.

**Sonuçlar:**
- `S1UnityPaketSiniri` dördüncü paketi de ölçer (kimlik, bağımsızlık, grafik, profil, klasör).
- Checks'in replay ve P0 kapıları paketi çağırır. Taşımanın kanıtı: Checks aynı kalır, 50 golden
  kaydı değişmez.
- Yeni Checks kapısı sondayı .NET'te uçtan uca koşar: Unity'nin çağıracağı fonksiyon her koşuda
  sunucuda da doğrulanır. Ayrıca iki golden okuyucunun (Checks'inki ve paketinki) aynı kayıtları
  verdiğini ölçer.
- `gen-replays` balance değer dökümünü de üretir; M17 kapısı dökümün bayat olmadığını denetler.
- **Sınır:** bu kayıt paketin ve Unity yapıştırıcısının Unity'de GERÇEKTEN derlendiğini kanıtlamaz
  — bu ortamda Unity yok (ADR-002 ile aynı sınır). Kanıt Atilla'nın Mac'indeki iki koşudur:
  EditMode testi (Mono) ve IL2CPP sondası (`unity/UNITY_SETUP.md`).
- **Not — satır sonları:** balance hash'i dosyanın ham baytlarından alınır (ME 3.3). Windows'ta
  `core.autocrlf` dosyayı CRLF açarsa hash değişir ve sonda "balance hash farklı" der. Bugün Editor
  macOS'ta olduğu için sorun değil; ME 17.1'in "Windows editör" ayağı geldiğinde `.gitattributes`
  ile `balance/*.json` LF'e sabitlenmeli.
