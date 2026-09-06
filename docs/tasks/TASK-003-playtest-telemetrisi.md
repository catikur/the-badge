# Task Brief 003: Playtest Telemetrisi (5G-a / S2 turu)

> TASK-002'nin turunu koşulabilir kılar. Bağlayıcı üst belge:
> `docs/briefs/BRIEF_5G_DIKEY_DILIM.md`; tur tanımı `docs/PLAYTEST_3G.md`.

## Objective

Mülakatlı gözlem turunun ölçebilmesi için **yerel bir JSONL olay logu**. Tur kapısının iki
metriğinden biri buna bağlı ve geçen sefer TAM DA BU eksikti.

## Bu gerçekten gerekli mi — evet, iki bağımsız sebeple

1. **Kapı metriği ölçülemiyor.** `PLAYTEST_3G.md` iki eşikli metrik tanımlıyor. İkincisi —
   **"sıkılma işareti < 3/maç"** — geçen turda şöyle döndü: *"gözlemde 2/5 oyuncuda işaret;
   telemetri paylaşılmadığından maç başı sayı yok"* → **veri kısmi.** Telemetrisiz ikinci kez
   ölçülemez.
2. **TASK-002 zaten şart koşuyor:** *"mini mülakat tablosu ve telemetri BU SEFER DOLDURULUR"*.

## Ama sanıldığından KÜÇÜK — yazıcı zaten var

`unity/TheBadge/Assets/Greybox/Scripts/Sim/TelemetryLog.cs` — **88 satır, sıfır UnityEngine
referansı**, jenerik `Event(tip).Str(...).Send()` kurucusu, her satırda flush (uygulama playtest
ortasında kapansa da veri kalır). Ürettiği biçim repoda örnekli:
`docs/samples/telemetry_ornek_oturum.jsonl`.

**YENİDEN YAZMA.** Yapılacak iş yazıcı değil, **olay kümesi**.

## ⚠️ SIRA TUZAĞI — arşivden ÖNCE çıkar

`TelemetryLog.cs` `Assets/Greybox/Scripts/Sim/` altında. TASK-002'nin K3 adımı kalan greybox'ı
`Assets/Greybox~/` yapıyor ve **Unity `~` ile biten klasörü içe aktarmaz.** Yani arşivi önce
yaparsan, TASK-003'ün ihtiyaç duyduğu aracı arşivlemiş olursun.

Bu, aynı şeklin **üçüncü örneği** (EngineDev/`SpriteFactory` ikincisiydi) — `docs/DECISIONS.md`'de
kural olarak yazılı: *tehlikeyi adlandıran çare, zincirin sonuna kadar sürülmeden bitmez.*

**Çözüm:** `TelemetryLog.cs`'i (+ `.meta`) EngineDev taşımasıyla AYNI turda çıkar. Namespace'i
`TheBadge.Greybox.Sim`; derlemeyi etkilemediği için kalabilir, istenirse ayrı adımda düzeltilir.

## Scope

**In:** olay kümesi + ekranın onu beslemesi + oturum kimliğinin mülakat formuyla eşleşmesi.

**Out (dokunma):**
- **Analytics sağlayıcı entegrasyonu YOK.** GDD 9.5 Firebase diyor, anayasa TelemetryDeck +
  MetricKit diyor ve **D-E kararı AÇIK** (`BRIEF_5G_DIKEY_DILIM.md`). Sağlayıcı seçmek bu brifin
  işi değil; şimdi bir SDK bağlamak ya açık kararı gasp eder ya da atılacak kod üretir.
- **FTUE hunisi YOK** (GDD 9.5) — 5G-b.
- Sunucuya gönderim YOK: dosya cihazda kalır, Atilla elle toplar (geçen turun runbook'u gibi).

## Olay kümesi — SORUDAN TÜRETİLİR, kopyalanmaz

Greybox'ın şeması eski soruları yanıtlıyordu (maç sayısı, skip, izleme süresi). **S2'nin sunumu
farklı** ve tezi şu: *"karar ver → kazanma ihtimali DEĞİŞSİN → sonucu yaşa."* Tur bunun tutup
tutmadığını ölçmeli.

| Turun cevaplaması gereken soru | Gereken olay |
| --- | --- |
| "Bir maç daha" dedi mi? (kapı metriği 1) | `session_start`, `match_start`, `match_end` + zaman damgası — 15 dk dolduktan SONRA yeni maç başladı mı |
| Sıkılma işareti/maç < 3 mü? (kapı metriği 2) | `skip`, hız değişimi (`speed_set` 1x/2x/atla), maçı yarıda bırakma |
| **Duraklama ANI işe yarıyor mu?** | `kritik_an` (dedektör ateşledi, `sicrama` değeriyle) |
| **Oyuncu o anda MÜDAHALE ETTİ Mİ?** | `mudahale` (taktik komutu + kabul/red) — duraklama ile aynı pencerede mi, yoksa geçip gitti mi |
| Şeridin hareketi kaydedildi mi? | müdahale öncesi/sonrası olasılık (`AnlikOlasilik`'ten, iki alan) |
| Nerede bıraktı? | **`session_end`e GÜVENME** — aşağıdaki nota bak |

### ⚠️ TERK SİNYALİ TERMİNAL OLAYA BAĞLANAMAZ (inceleme bulgusu, Codex P1)

İlk yazımda "nerede bıraktı" sorusunu `session_end`e bağlamıştım. **Çalışmaz ve yanlı sonuç
üretir:** oyuncu uygulamayı zorla kapatınca, süreç çökünce ya da işletim sistemi öldürünce
`session_end` HİÇ yazılmaz. Yani tam da terk senaryosunda terk kaydı kaybolur — ve terk,
"sıkılma işareti" kapı metriğini besleyen şey. Sonuç tamamlanmış oturumlara doğru yanlı olurdu;
üstelik kabul kriterinin kendisi (uygulamayı maç ortasında kapat) bu senaryoyu zorluyor.

**Doğru tasarım — terk ÇIKARSANIR, raporlanmaz:**
- Her satır zaten anında flush ediliyor, yani **dosyanın kendisi kayıttır**.
- Ekran/faz geçişleri **olduğu anda** yazılır (`screen`, `phase`) — böylece "nerede bıraktı"
  son yazılan satırdan okunur, terminal olaya gerek kalmaz.
- **Terk analitik olarak tanımlanır:** son satırı `session_end` OLMAYAN oturum, ya da eşleşen
  `match_end`i olmayan bir `match_start`.
- `session_end` yine yazılır ama artık **temiz çıkış işareti**dir, bağımlılık değil.

**En kritik çift `kritik_an` + `mudahale`dir.** Duraklamaların kaçında oyuncu bir şey yaptı,
kaçında geçti — bu tek oran, sunumun çekirdek vaadinin tutup tutmadığını doğrudan söyler.
Greybox'ta bu ölçülmemişti; %40'ın nedenini bilmememizin bir sebebi de bu.

## Kurallar

1. **PII YOK.** Takma ad bile yazılmaz; oturum kimliği rastgele bir dize olsun. Mülakat formuyla
   eşleşme o kimlik üzerinden yapılır (CLAUDE.md privacy tabanı).
2. **Yazıcı yeniden yazılmaz** (yukarı).
3. Telemetri **oynanışı etkilemez**: yazma hatası oyunu düşürmez. **Ama sessizce yutulmaz ve
   bayrak KIRIK YAZICIDAN geçmez** (inceleme bulgusu, Codex P2): dizin yazılamıyorsa ya da disk
   doluysa, hatayı `session_end`e yazmak aynı bozuk yazıcıyı kullanmak demekti — dosya yalnızca
   EKSİK görünür, ölçümün başarısız olduğuna dair hiçbir iz kalmazdı.
   Bildirim **bant dışı** olmalı: playtest yapısında ekranda görünür bir uyarı
   ("TELEMETRİ YAZILAMIYOR") — gözlemci odada olduğu için en hızlı sinyal budur ve oturum
   boşa gitmeden düzeltilir. Ek olarak ayrı bir işaret (`PlayerPrefs` bayrağı ya da yanına
   yazılan `telemetry_error.txt`) ki sonradan da anlaşılsın.
4. Olay adları ve alanları **tek yerde sabit** tutulur (ekranın içine dağılmaz) — sonraki turda
   şema değişirse tek dosya değişir.

## Acceptance criteria

- Bir maç oynandığında JSONL dosyası yazılıyor; her satır geçerli JSON.
- Yukarıdaki tablodaki her soru için en az bir olay düşüyor.
- **Uygulama maç ortasında kapatılınca dosyada o ana kadarki satırlar DURUYOR** (flush davranışı
  elle denenip raporlanır — playtest'te bu gerçekten olur) **ve o dosyadan terk ÇIKARILABİLİYOR**:
  son satır `session_end` değil, son `match_start`ın eşleşen `match_end`i yok, son `screen`/`phase`
  satırı nerede bırakıldığını söylüyor.
- **Telemetri yazamazsa ekranda görünüyor:** yazılamayan bir dizine yönlendirip denenir ve
  uyarının çıktığı raporlanır (bant dışı bildirim, kural 3).
- Oturum kimliği dosya adında ve her satırda; PII yok.
- `dotnet run --project shared/TheBadge.Sim.Checks -c Release` yeşil (180 kapı; bu iş çekirdeğe
  dokunmamalı — dokunduysa sebebini yaz).

## Verification required (DoD-G)

1. Unity konsolu temiz.
2. Örnek bir oturumun JSONL çıktısı `docs/samples/` altına konur (geçen turda bu YAPILMADI ve
   rapor gücü kayboldu).
3. Kısa oynanış kaydı ya da ekran görüntüsü.
4. Varsayımlar ve kalan riskler.

## Sıra

TASK-002 ekranı → **TASK-003 telemetrisi** → mülakatlı tur. Telemetri turu bloklar, ekranı
BLOKLAMAZ: ekran koşarken paralel yazılabilir. Ama **arşiv adımından ÖNCE** `TelemetryLog.cs`
çıkarılmış olmalı.
