<!-- Match Engine Teknik Spesifikasyonu v1.0 — bu dosya bağlayıcıdır; değişiklik önerileri docs/DECISIONS.md üzerinden -->

# 1. AMAÇ, KAPSAM VE GDD BAĞLANTISI

## 1.1. Bu Dokümanın Rolü

Bu spesifikasyon, GDD v4.0'ın (Anayasa) Bölüm 5 (Maç Motoru), Bölüm 11 (Teknik Mimari) ve Bölüm 16 (Vibe & Verify) hükümlerini uygulanabilir mühendislik detayına indirir. FAZ 03'ün birincil girdisidir: Cursor'a verilecek implementasyon görevleri ve CI'daki kabul testleri bu dokümandan türetilir.

- **Kapsam:** Simülasyon çekirdeği, karar AI'ı, fizik, kaleci modeli, duran toplar, hakem/VAR, maç içi durum modeli, hava/zemin, Chaos Engine matematiği, müdahale katmanı, olay günlüğü/xG/highlight, LOD ve performans bütçeleri, test-validasyon planı.
- **Kapsam dışı:** UI/sunum katmanı (GDD 5.1, FAZ 02), lig/turnuva takvimi, transfer AI, Nakama protokol detayları.
- **Detay seviyesi:** Tam blueprint — matematiksel formüller + pseudocode + C# arayüzleri + veri şemaları birlikte.

## 1.2. Tasarım İlkeleri (Değişmezler)

- **Determinizm mutlaktır:** Aynı MatchConfig + aynı komut zaman çizelgesi = bit düzeyinde aynı sonuç, her platformda (bölüm 3).
- **Tek Kapı:** Maç durumunu değiştiren her şey Command Bus'tan tick-damgalı geçer (GDD 11.4); UI, LLM ve otomasyon aynı kuyruğu kullanır.
- **Simülasyon = Gerçek:** Ekranda görünen her şey simülasyonun kendisidir; kozmetik yeniden kurgulama yoktur (kalabalık/kamera hariç).
- **Ayarlanabilirlik dışarıda:** Tüm katsayılar balance JSON'dadır; koddaki sabit yalnızca yapısal limitlerdir. Kalibre edilecek katsayılar bu dokümanda [KALİBRE] etiketiyle işaretlidir.
- **Ölçülebilirlik:** Her alt sistem, bölüm 17'deki istatistiksel bantlara karşı 10.000 sezon simülasyonuyla doğrulanır.

## 1.3. GDD Çapraz Referans Haritası

| Bu Doküman | GDD v4.0 Karşılığı |
| --- | --- |
| Bölüm 2 (Mimari Karar) | 5.2 Simülasyon Felsefesi, 11.3 Performans Hedefleri |
| Bölüm 3 (Determinizm) | 5.5 Determinizm Kutusu, 11.9 Replay Mimarisi |
| Bölüm 13 (Chaos) | 5.3 Ayarlanabilir Chaos Engine |
| Bölüm 14 (Müdahale) | 5.4 Maç İçi Etkileşim, 11.4 Command Bus |
| Bölüm 15 (Olay/Highlight) | 5.6 İzlenebilirlik, Modül 7 Replay/Panorama |
| Bölüm 16 (LOD/Performans) | 6.6 Yük Dengeleme, 14.3 Operasyonel Maliyet |

# 2. MİMARİ KARAR — SİMÜLASYON ÇEKİRDEĞİ TRADE-OFF ANALİZİ

## 2.1. Aday Mimariler

**Seçenek A — Saf Sürekli 2D Tick Simülasyonu:** 22 ajan + top, sabit zaman adımıyla her tick konum/karar günceller. Maçın her saniyesi gerçekten simüle edilir.

**Seçenek B — Makro-Olay Motoru:** Maç, olasılık tablolarından örneklenen olay dizisidir (hücum hakkı → şans kalitesi → sonuç). 2D görüntü, olayların kozmetik canlandırmasıdır.

**Seçenek C — Sürekli Çekirdek + LOD Katmanları (ÖNERİLEN):** Tek sürekli simülasyon çekirdeği; maçın önemine göre üç çözünürlük seviyesinde koşar. Arka plan dünya maçları için istatistiksel hızlı mod, ama oyuncunun gördüğü/online oynanan her maç gerçek simülasyondur.

## 2.2. Karşılaştırma Matrisi

| Kriter | A: Saf Sürekli | B: Makro-Olay | C: Sürekli + LOD |
| --- | --- | --- | --- |
| Serbest pozisyonlama sadakati (ana farklılaştırıcı) | Tam | Zayıf (girdi tabloya iner) | Tam |
| İzlenebilirlik / highlight kalitesi | Yüksek | Kozmetik | Yüksek |
| Replay uyumu (Modül 7: seed+girdi ile yeniden sim) | Doğal | Yapay | Doğal |
| Sunucu maliyeti (10K maç / 10 dk penceresi) | Yüksek | Çok düşük | Orta (LOD ile kontrollü) |
| Offline kariyer dünya simülasyonu (yüzlerce maç/hafta) | Pahalı | Ucuz | Ucuz (LOD 2) |
| Balance çabası | Yüksek (emergent) | Düşük | Orta (hedef bantlarla) |
| Determinizm riski | Orta (float disiplini gerek) | Düşük | Orta (bölüm 3 ile çözülür) |
| Geliştirme süresi | 6-8 hafta | 3-4 hafta | 6-8 hafta (FAZ 03 bütçesine uyar) |

## 2.3. Karar ve Gerekçe

> **✅ MİMARİ KARAR — SEÇENEK C: SÜREKLİ ÇEKİRDEK + LOD KATMANLARI**
> Gerekçe 1: Serbest pozisyonlama The Badge'un 1 numaralı farklılaştırıcısıdır (GDD 1.3). Makro-olay modeli bu farkı olasılık tablosuna indirger; oyuncunun piksel piksel dizilişi anlamsızlaşır. Kabul edilemez.
> Gerekçe 2: Modül 7 (Replay + Panorama) seed + girdi ile YENİDEN SİMÜLASYON varsayar. Yalnızca gerçek simülasyon bu mimariyi doğal kılar; makro-olayda replay bir kurgudur.
> Gerekçe 3: Sunucu maliyeti sorunu, motoru sahtelemekle değil çözünürlük katmanlamayla (LOD) ve kaydırılmış maç saatleriyle (GDD 6.6) çözülür. Bölüm 16'daki bütçe hesabı hedeflerin tutturulabilir olduğunu gösterir.
> Gerekçe 4: Feel iterasyonu (GDD FAZ 03) gerçek simülasyon üzerinde anlamlıdır; olay kurgusunda "tempo eğrisi" ayarlanamaz, ancak maskelenir.

## 2.4. LOD Tanımları (Ayrıntı: Bölüm 16)

| LOD | Hareket / Karar Tick | Kullanım | Maç Başına CPU Hedefi |
| --- | --- | --- | --- |
| LOD 0 — Tam | 10 Hz / 4 Hz | Oyuncunun izlediği maçlar, TÜM online lig maçları | ≤ 2,5 sn (tek çekirdek) [KALİBRE] |
| LOD 1 — Standart | 5 Hz / 2 Hz | Oyuncunun ligindeki diğer AI-AI maçları | ≤ 0,8 sn |
| LOD 2 — Hızlı | Olay örneklemeli istatistik modu | Offline kariyerde arka plan ligleri | ≤ 10 ms |

- **Tutarlılık kuralı:** Bir maçın LOD'u MatchConfig'te sabitlenir ve config_hash'e girer; aynı maç asla iki farklı LOD ile "aynı sonuç" iddiasında bulunmaz.
- **LOD 2 türetme kuralı:** LOD 2'nin olasılık tabloları elle yazılmaz; LOD 0 ile koşulan 10.000 kalibrasyon maçından regresyonla türetilir. Böylece hızlı mod, gerçek motorun istatistiksel gölgesi olur ve iki katman arasında güç tutarlılığı korunur.
- **GDD 11.3 revizyon notu:** "1 sezon server-side < 10 sn" hedefi şöyle netleşir: oyuncunun ligi (1 × LOD 0 + 9 × LOD 1) ≤ 12 sn, tam dünya güncellemesi (≈200 LOD 2 maçı dahil) ≤ 20 sn. Karar günlüğüne işlenecek.

# 3. DETERMİNİZM SÖZLEŞMESİ

## 3.1. Rastgelelik: Sayaç-Tabanlı, Durumsuz RNG

Klasik "sıralı PRNG" (tek Random nesnesi) müdahale determinizmini kırar: oyuncunun 37. dakikada yaptığı bir değişiklik, sonraki tüm çekilişleri kaydırır ve alakasız olayları değiştirir. Çözüm: her rastgele değer, çağrı SIRASINDAN bağımsız olarak adresinden türetilir.

```csharp
// Durumsuz, sayaç-tabanlı gürültü (SplitMix64 çekirdeği)
ulong Hash64(ulong seed, uint domain, uint entity, uint tick, uint salt)
{
    ulong z = seed ^ (domain * 0x9E3779B97F4A7C15UL)
              ^ ((ulong)entity << 32) ^ ((ulong)tick << 1) ^ salt;
    z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
    z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
    return z ^ (z >> 31);
}

double Rand01(Domain d, uint entity, uint tick, uint salt)
    => (Hash64(matchSeed, (uint)d, entity, tick, salt) >> 11)
       * (1.0 / 9007199254740992.0);

// Box-Muller yerine deterministik yaklaşık Gauss (12-toplam yöntemi)
double Gauss01(Domain d, uint e, uint t, uint s) {
    double sum = 0;
    for (uint i = 0; i < 12; i++) sum += Rand01(d, e, t, s * 16 + i);
    return sum - 6.0;  // ortalama 0, sigma yaklaşık 1
}
```

- **Domain akışları:** DECISION, PHYSICS, DUEL, CHAOS, REFEREE, INJURY, SETPIECE, CROWD. Her alt sistem yalnızca kendi domain'inden çeker; müdahaleler diğer domainlerin çekilişlerini etkileyemez.
- **CROWD domain'i** yalnızca istemci kozmetiğidir (tezahürat zamanlaması); sonuç durumuna asla girmez.

## 3.2. Sayısal Disiplin (Cross-Platform Bit Eşitliği)

- **Konum/hız tamsayıdır:** Tüm kalıcı durum int32 milimetre (konum) ve int32 mm/sn (hız) tutulur. Ara hesaplar double, sonuç her tick'te kuantalanır (QuantizeMm). Float durumu asla tick'ler arasında taşınmaz.
- **Trigonometri LUT:** sin/cos 4096 girişli tamsayı tablodan (Q16 sabit nokta); Math.Sin platform farkı riski sıfırlanır.
- **Sıralı toplama:** Ajan güncelleme sırası her tick sabittir (takım, formasyon indeksi); paralellik yalnız MAÇLAR ARASI kullanılır, maç içi asla.
- **Yasaklar:** DateTime.Now, Environment.TickCount, Dictionary iterasyon sırasına bağımlılık, unordered LINQ, platform intrinsics.
- **Checksum:** Her 600 tick'te (60 sn) kanonik durum üzerinden xxHash64 alınır ve event log'a yazılır. Replay oynatımı checksum uyuşmazlığında durur ve telemetriye MatchDesyncEvent gönderir.

## 3.3. Config Hash ve Uyumluluk

```
config_hash = xxHash64(
    engineVersion, lodLevel, tickRates,
    balanceJson_kanonik_bytes, chaosLevel, rosterSnapshotHash )
```

- Replay kaydı { engineVersion, config_hash, seed, komut zaman çizelgesi } dörtlüsüyle oynar (GDD 11.9). Sezon içi motor dondurma kuralı (GDD 8.2) geçerlidir.
- Balance JSON'daki TEK bayt değişikliği config_hash'i değiştirir → eski replay'ler yeni parametrelerle asla "sessizce" oynatılmaz.

## 3.4. Zaman Modeli

- **Sabit zaman adımı:** LOD 0'da Δt = 100 ms (10 Hz). Maç saati simülasyon tick'inden türetilir; gerçek zaman hızlandırma (2x/4x izleme) yalnız SUNUM hızını değiştirir, tick içeriğini değiştirmez.
- **Ölü top sıkıştırma:** Taç/aut/kale vuruşu hazırlıkları tek çözüm adımında işlenir ve maç saatinden deterministik süre düşer (ör. taç: 12-18 sn, SETPIECE domain çekilişi). CPU tasarrufu + gerçekçi maç saati akışı birlikte sağlanır.
- **Uzatma süresi:** Duraklamaların birikimiyle deterministik hesaplanır: ek süre = clamp(round(0.55 × durak_dakikası + kart × 0.3 + gol × 0.35), 1, 9) dk [KALİBRE].

# 4. SİMÜLASYON DÖNGÜSÜ VE FAZ MAKİNESİ

## 4.1. Maç Faz Makinesi

```csharp
enum MatchPhase { KICKOFF, OPEN_PLAY, DEAD_BALL, SET_PIECE,
                  PENALTY, HALF_TIME, FULL_TIME, VAR_REVIEW }
```

- **OPEN_PLAY:** Ana döngü; tüm ajanlar aktif.
- **DEAD_BALL(tip):** Taç, kale vuruşu, santra hazırlığı. Sıkıştırılmış çözüm (3.4) + oyuncular hedef dizilişe ışınlanmaz, hızlandırılmış yürüyüşle taşınır (izlenebilirlik).
- **SET_PIECE(tip):** Korner, frikik, penaltı — bölüm 10'daki özel çözücüler.
- **VAR_REVIEW:** Bölüm 11.4; oynanış donar, dramatik bekleme sunulur.
- Faz geçişleri yalnızca event üretir; UI fazı event log'dan okur (Interrupt Abstraction, GDD 15.1).

## 4.2. Tick Pipeline (LOD 0)

Her 100 ms'lik tick, sabit sırayla 6 aşama işler:

```csharp
void Tick(uint t) {
    1. CommandQueue.ApplyDue(t);        // Bölüm 14: müdahaleler
    2. PerceptionPass(t);               // uzamsal grid güncelle (top, rakip, boşluk)
    3. DecisionPass(t);                 // yalnız sırası gelen ajanlar (4 Hz, kademeli)
    4. ActionResolutionPass(t);         // düello/pas/şut çözümleri (Bölüm 6-10)
    5. PhysicsPass(t);                  // hareket entegrasyonu + top fiziği (Bölüm 8)
    6. EventAndStatePass(t);            // event log, stamina/moral, checksum, faz geçişi
}
```

- **Kademeli karar (staggering):** 22 ajan 5 gruba bölünür (agentId mod 5); her tick yalnız bir grup karar verir → karar yükü tick başına ~4-5 ajana düşer, tepki gecikmesi en fazla 250 ms kalır (insan algısı altı).
- **Algı bütçesi:** PerceptionPass, sahayı 12×8 hücreli uzamsal grid'de tutar; "en yakın N rakip", "pas koridoru boş mu" sorguları O(1)-O(k) maliyetlidir. Ajan başına algı sorgusu üst sınırı: 24/karar [KALİBRE].

## 4.3. Karar Önceliği ve Top Sahipliği

- Top sahibi ajan her karar tick'inde tam Utility değerlendirmesi yapar (Bölüm 7.2).
- Topsuz ajanlar rol-pozisyon karması hedef nokta hesaplar (Bölüm 7.4); yalnızca tetikleyici olaylarda (pres tetiği, markaj değişimi) tam yeniden değerlendirme yapılır.
- Aynı tick'te topa iki ajan ulaşırsa: kazanan, DUEL domain'inden çözülen kontrol düellosudur (6.4); "aynı anda iki sahip" durumu yapısal olarak imkansızdır.

# 5. VERİ MODELLERİ (C# ŞEMALARI)

## 5.1. Çekirdek Arayüzler

```csharp
public interface IMatchEngine {
    MatchResult Run(MatchConfig cfg, ICommandTimeline cmds);   // headless
    void RunInteractive(MatchConfig cfg, ISimClock clock,
                        ICommandSource live, IEventSink sink); // izlenen maç
}
public interface IRngService {
    double Rand01(Domain d, uint entity, uint tick, uint salt);
    double Gauss(Domain d, uint entity, uint tick, uint salt, double sigma);
}
public interface IActionResolver {   // her düello/aksiyon tipi için tek giriş
    ResolutionOutcome Resolve(in ActionContext ctx);
}
public interface IEventSink { void Emit(in MatchEvent e); }
```

## 5.2. MatchConfig ve MatchState

```csharp
public sealed class MatchConfig {
    public ulong Seed; public ulong ConfigHash; public string EngineVersion;
    public LodLevel Lod; public ChaosLevel Chaos;
    public TeamSheet Home, Away;          // kadro + serbest pozisyon anchor'ları
    public PitchCondition Pitch;          // Bölüm 12.4
    public WeatherState Weather;          // Bölüm 12.4
    public RefereeProfile Referee;        // Bölüm 11.1
}
public struct MatchState {               // kalıcı durum: yalnız tamsayı
    public uint Tick; public MatchPhase Phase; public int HomeGoals, AwayGoals;
    public BallState Ball; public PlayerAgentState[] Agents; // [22]
    public TeamRuntime HomeRt, AwayRt;    // hat yüksekliği, pres modu, momentum
    public ulong LastChecksum;
}
public struct BallState {
    public int X, Y, Z;                  // mm
    public int Vx, Vy, Vz;               // mm/sn
    public int SpinY;                    // falso bileşeni (mm/sn2 eşleniği)
    public short OwnerId;                // -1 = serbest top
}
```

## 5.3. PlayerAgentState

```csharp
public struct PlayerAgentState {
    public short Id; public byte TeamIdx; public byte RoleId;
    public int X, Y, Vx, Vy;             // mm, mm/sn
    public int AnchorX, AnchorY;         // kullanıcının piksel dizilişi (taktik girdisi)
    public ushort Energy;                // 0-1000 (0,1 hassasiyet)
    public sbyte Momentum;               // -10..+10 (Bölüm 12.3)
    public byte YellowCards; public bool SentOff; public InjuryState Injury;
    public byte CurrentAction; public uint ActionUntilTick;
}
```

- TeamSheet, nitelikleri (Bölüm 6.1) salt-okunur EffectiveAttribute tablosu olarak taşır; maç içi çarpanlar (kondisyon, moral, hava) runtime'da uygulanır, taban değer asla mutasyona uğramaz.

## 5.4. Komut ve Olay Sözleşmeleri (Özet)

```csharp
public abstract record MatchCommand(uint IssueTick, byte TeamIdx);
public sealed record SubstitutionCmd(uint T, byte Team, short Out, short In) : ...
public sealed record TacticChangeCmd(uint T, byte Team, TacticDelta Delta) : ...
public sealed record InstructionCmd(uint T, byte Team, short PlayerId, Instr I) : ...
public sealed record MotivationCmd(uint T, byte Team, ToneType Tone) : ...
```

MatchEvent şeması bölüm 15.1'de alan alan tanımlıdır. Komutların uygulanma anları bölüm 14.2'dedir.

# 6. NİTELİK SİSTEMİ VE AKSİYON EŞLEMELERİ

## 6.1. Nitelik Seti (1-100 Ölçeği)

| Grup | Nitelikler |
| --- | --- |
| Teknik | Passing, Finishing, Dribbling, Tackling, Heading, FirstTouch, Crossing, SetPieces |
| Zihinsel | Positioning, Decisions, Composure, Aggression, Workrate, Vision |
| Fiziksel | Pace, Acceleration, Stamina, Strength, Agility, JumpReach |
| Kaleci | Reflexes, Handling, OneOnOne, AerialCommand, Kicking, Throwing |

## 6.2. Efektif Nitelik Formülü

Her kullanımda taban değer bağlamsal çarpanlarla ölçeklenir:

A_eff = A_base × M_kondisyon × M_moral × M_hava × M_zemin, sonuç [1, 100] bandına kırpılır.

- M_kondisyon = 0,70 + 0,30 × (Energy/1000)^0,7 [KALİBRE] — yorgun oyuncu tabanın en az yüzde 70'ini korur, çöküş kademelidir.
- M_moral = 1 + Momentum × 0,005 (±5 puan tavan; GDD 7.4 bant sınırı ile uyumlu).
- M_hava ve M_zemin tabloları bölüm 12.4'tedir; yalnız ilgili nitelikleri etkiler (ör. yağmur Passing/FirstTouch, sıcak Stamina drenajını).

## 6.3. Genel Düello Çözüm Modeli

Tüm ikili mücadeleler (top kapma, omuz omuza, hava topu, kontrol düellosu) tek çekirdek formülü kullanır:

- margin = (A_eff − D_eff) + Chaos_noise, burada Chaos_noise = sigma_chaos × Gauss (DUEL domain; sigma tablosu bölüm 13.2).
- P(atak kazanır) = clamp( P_taban + k_duel × margin / 100, P_min, P_max ).
- Varsayılanlar [KALİBRE]: P_taban düello tipine göre 0,42-0,55; k_duel = 0,9; kırpma bandı [0,08, 0,92] — hiçbir düello asla "garanti" değildir.

## 6.4. Aksiyon Eşleme Tablosu

| Aksiyon | Saldıran Kompoziti | Savunan Kompoziti | Ek Bağlam |
| --- | --- | --- | --- |
| Top kapma (tackle) | 0,6 Tackling + 0,25 Positioning + 0,15 Strength | 0,5 Dribbling + 0,3 Agility + 0,2 Strength | Arkadan girişte foul riski (11.2) |
| Hava topu | 0,4 Heading + 0,35 JumpReach + 0,25 Strength | aynı kompozit | Ortanın kalitesi hedef sapmasını belirler |
| İlk dokunuş | 0,7 FirstTouch + 0,3 Composure | pres şiddeti (yakın rakip sayısı × mesafe) | Kötü dokunuş = serbest top eventi + pres tetiği |
| Kontrol düellosu (aynı anda topa varış) | 0,5 Acceleration + 0,3 Agility + 0,2 Strength | aynı kompozit | Varış süresi farkı margin'e eklenir |
| Şut isabeti | 0,55 Finishing + 0,25 Composure + 0,2 Technique bağlamı | kaleci modeli (Bölüm 9) | xG hattı bölüm 15.2 |

## 6.5. Pas Modeli — Geometrik Çözüm

Pas bir zar değil, geometri problemi olarak çözülür; başarı kesişim analizinden doğar:

- **1) Nişan hatası:** Hedef noktaya açı sapması sigma_theta = sigma_0 × (1 − Passing/125) × f_mesafe(d) × M_pres × M_hava; sigma_0 = 6,5 derece [KALİBRE], f_mesafe(d) = 1 + d/35m, M_pres = 1 + 0,15 × yakın_rakip_sayısı.
- **2) Top uçuşu:** Sapmalı hedefe, seçilen ilk hızla (yerde: 12-19 m/sn; havadan: bölüm 8.3 balistik) yörünge kurulur.
- **3) Kesişim taraması:** Pas koridorundaki her rakip için t_intercept (ajanın kesişim noktasına varış süresi, Bölüm 8.1 kinematiği) ile t_ball (topun o noktaya varışı) karşılaştırılır; t_intercept + tepki_payı < t_ball ise kesme girişimi doğar; kesme başarısı FirstTouch/Positioning düellosuyla (6.3) çözülür.
- **4) Alıcı kontrolü:** Kesilmezse alıcı İlk Dokunuş çözümüne girer.
- Sonuç: pas isabeti oranları emergent'tır; kalibrasyon bandı (bölüm 17.2) sadece doğrulama içindir, formüle geri yazılmaz.

# 7. KARAR AI'I — UTILITY SKORLAMA + BEHAVIOUR TREE

## 7.1. İki Katmanlı Yapı

- **Behaviour Tree (yapı):** "Hangi durumdayım, hangi karar ailesine bakmalıyım" sorusunu cevaplar. Faz/rol bazlı dallanma; okunabilir, debug edilebilir.
- **Utility AI (seçim):** Dalın içindeki somut aksiyon adaylarını skorlar ve seçer. Ağırlıklar balance JSON'da; feel iterasyonunun ana ayar sahası burasıdır.

## 7.2. Top Sahibi — Aday Aksiyonlar ve Skorlama

Aday küme (en fazla 12 aday/karar): ShortPass(r1..r4), ThroughPass(r), LongSwitch(r), Cross(bölge), Dribble(yön), Shoot, HoldShield, ClearBall.

Her adayın skoru:

Score = w_threat × ΔxT + w_risk × (1 − P_kayıp) + w_tactic × TacticBias + w_role × RoleBias + w_fatigue × EnerjiMaliyeti + w_var × ChaosNoise

- **ΔxT (beklenen tehdit değişimi):** Saha 12×8 hücrelik xT (expected threat) tablosuna bölünür; aksiyonun topu taşıyacağı hücrenin xT'si ile mevcut hücre farkı. xT tablosu balance JSON'dadır ve kalibrasyon simülasyonlarıyla güncellenir [KALİBRE].
- **P_kayıp:** Pas için kesişim taramasının hızlı tahmini (koridor yoğunluğu); dribbling için en yakın savunucu düello ön-tahmini; şut için 1 − xG.
- **TacticBias:** Mentalite (Çok Defansif −0,3 ... Çok Ofansif +0,3 ileri aksiyonlara), tempo (yüksek tempo kısa tutmayı cezalandırır), genişlik (kanat/Cross bias), talimatlar (ör. "riskli pasları dene": ThroughPass +0,15).
- **RoleBias:** Rol tablosundan (ör. Regista: LongSwitch +0,2; Golcü: Shoot +0,15 ceza alanı içinde).
- **ChaosNoise:** DECISION domain'inden sigma_karar × Gauss; seviye tablosu 13.2. Chaos "yanlış tercih"leri buradan doğurur — yetenek düşürmeden.
- **Seçim:** argmax(Score). Vision niteliği aday küme genişliğini belirler: aday_sayısı = 4 + floor(Vision/15) — düşük vizyonlu oyuncu en iyi seçeneği hiç GÖREMEYEBİLİR. Zar yerine bilgi kısıtı: determinizmle uyumlu, futbol gerçekliğiyle örtüşür.
- **Karar kilidi:** Seçilen aksiyon ActionUntilTick'e kadar sürer; her tick fikir değiştirme yoktur (titreme önlenir). Acil iptal yalnız top kaybı/faul eventinde.

## 7.3. Behaviour Tree — Saha Oyuncusu Kök Ağacı

```
ROOT (her karar tick'i)
├── Sequence [Faz == SET_PIECE] → SetPieceRoleNode (Bölüm 10)
├── Selector [Takımda top var mı?]
│   ├── [Top bende] → UtilitySelect(OnBallCandidates)      // 7.2
│   ├── [Top takım arkadaşında] → OffBallAttackNode        // 7.4-A
│   └── [Top rakipte veya serbest]
│       ├── [PresTetiği aktif && ben en yakın 2'deysem] → PressNode
│       ├── [Markaj görevim var] → MarkNode                // 7.5
│       └── → DefensiveShapeNode                           // 7.4-B
└── Fallback → HoldPositionNode
```

## 7.4. Topsuz Konumlanma — Vektör Karması

Topsuz ajanın hedef noktası, ağırlıklı vektör toplamıdır (rol tablosundan w katsayıları):

P_hedef = w_anchor × Anchor + w_faz × FazOfseti + w_top × TopÇekimi + w_boşluk × BoşlukVektörü + w_görev × GörevVektörü, ardından OfsaytKısıtı ve SahaSınırı kırpması.

- **A) Hücum modu:** FazOfseti ileri itme vektörüdür (mentalite × rol derinliği); BoşlukVektörü, xT gridinde yakın yüksek-değerli boş hücreye yönelim; kanat rolleri genişlik talimatına göre touchline çekimi alır. Anchor (kullanıcının serbest dizilişi) HER modda en yüksek tekil ağırlıktır: w_anchor = 0,45-0,60 [KALİBRE] — oyuncunun piksel yerleşimi davranışın omurgası kalır.
- **B) Savunma modu:** GörevVektörü = hat hizalama (bölüm 7.6) + kanal kapama (top-kale çizgisine iniş); TopÇekimi savunma üçte birinde güçlenir.
- **Ofsayt kısıtı:** Hücumda P_hedef.x, rakip son savunucu çizgisinin en fazla 0,3 m gerisine kırpılır; ThroughPass anında koşu serbest bırakılır (koşu zamanlama hatası = Positioning düellosu, ofsayt üretimi 10.5).

## 7.5. Markaj Atama Çözücüsü

Her savunma geçişinde (top kaybı eventi) takım koordinatörü çalışır:

```
AssignMarking():
  tehditler = rakip hücumcular, skor = xT(hücre) + 0,2 × (Pace/100)
  savunucular = müsait savunma rolleri (görev tablosuna göre)
  Greedy eşleme: en yüksek tehditten başla, en yakın uygun savunucuyu ata
  AdamAdama talimatı varsa: sabit eşleme kilitlenir (kullanıcı ataması, GDD 3.2)
  Kalan savunucular → bölgesel görev (DefensiveShapeNode)
```

## 7.6. Takım Koordinasyonu

- **Savunma hattı yüksekliği:** hat_x = taban(talimat) + 0,35 × (top_x − saha_orta) kırpılmış; ofsayt tuzağı talimatı hat senkron toleransını 0,5 m'den 0,2 m'ye indirir (risk: arkaya top, bölüm 17 upset bandı).
- **Pres tetikleri:** geri pas, kötü ilk dokunuş eventi, top taç çizgisi tuzak bölgesine girince, kaleciye geri dönüş. Tetik aktifken en yakın 2 ajan PressNode'a geçer; PresŞiddeti talimatı tetik eşiklerini ve pres süresini (enerji maliyeti 12.1) ölçekler.
- **Momentum yayılımı:** Takım momentumu (12.3) DECISION noise sigma'sını ±%15 ölçekler — baskı altındaki takım daha çok hata yapar, izlenebilir "çöküş" anları doğar.

# 8. HAREKET VE TOP FİZİĞİ

## 8.1. Ajan Kinematiği

- v_max = 4,2 + 4,6 × Pace/100 m/sn; a_max = 3,0 + 4,0 × Acceleration/100 m/sn2 [KALİBRE]; topla sürerken v_max × (0,72 + 0,0022 × Dribbling).
- Yön değişimi: açısal hız tavanı = f(Agility); keskin dönüş hız kaybettirir (v = v × cos_kayıp) — bu, dribling aldatmalarının fiziksel temelidir.
- Varış süresi kestirimi t_arrive(d, v0): iki fazlı (ivmelenme + seyir) kapalı form; pas kesişim taraması (6.5) ve kaleci çıkış kararı (9.3) bunu kullanır.

## 8.2. Yerde Top

- Sürtünme yavaşlaması a_roll = −3,3 m/sn2 (kuru) [KALİBRE]; zemin çarpanları 12.4 tablosunda (ıslak: −2,6 → top daha uzun kayar; ağır çim: −4,4).
- Menzil formülü: d = v0² / (2 × a_roll) → pas gücü seçimi bu ters formülle deterministik hesaplanır.

## 8.3. Havada Top — Balistik + Falso

- Entegrasyon (tick başına): g = 9,81; hava direnci F_d = −k_d × v × |v| (k_d = 0,0045 [KALİBRE]); Magnus (falso) ivmesi a_m = k_m × Spin × v_yatay dik bileşeni (k_m = 0,0032 [KALİBRE]).
- Spin üretimi: Crossing/SetPieces niteliği maksimum spin'i belirler; falsolu orta ve frikişlerde nişan, hedefin 1-3 m yanına kurulur, Magnus topu içeri kıvırır (görsel olarak tatmin edici, tamamen deterministik).
- **Sekme:** v_z' = −e × v_z; e = 0,62 kuru, tablo 12.4 (ıslak 0,50, kar 0,38); yatay hız sekmede ×0,78 sürtünme çarpanı. Chaos, sekme yönüne yalnız Yüksek seviyede ±3 derece pertürbasyon ekler (PHYSICS domain, 13.3).
- Kafa/kontrol yüksekliği: Z < 0,4 m ayak, 0,4-1,6 m kontrol/vole düellosu, > 1,6 m hava topu düellosu (JumpReach erişim tavanı: 2,05 + JumpReach/100 × 0,9 m).

# 9. KALECİ ÖZEL DAVRANIŞ MODELİ

## 9.1. Pozisyon Alma

Kaleci hedef noktası: top-kale açıortayı üzerinde, derinlik d_gk = clamp(0,9 + 0,08 × top_mesafesi, 0,9, 5,5) m; AerialCommand yüksek kaleci ortalarda 1,5 çarpanla daha önde durur. Positioning niteliği açıortay hatasını belirler: sigma_pos = 0,9 × (1 − Positioning/120) m.

## 9.2. Şut Kurtarma Çözümü

```
ResolveSave(shot):
  t_plane   = topun kale düzlemine varış süresi (8.3 entegrasyonu)
  t_react   = 0,16 + (100 − Reflexes) × 0,0016        // sn [KALİBRE]
  mesafe    = |şut_kesişim_noktası − kaleci_konumu|    // düzlemde
  erişim    = 0,9 + Agility/100 × 1,9                  // dalış zarfı, m
  t_traverse= mesafe / (erişim / 0,55)                 // dalış süresi modeli
  marj      = t_plane − (t_react + t_traverse) + ChaosNoise(DUEL)
  P_save    = lojistik(6,5 × marj) kırp [0,04, 0,96]
  Sonuç dalı: kurtarış → TutuşKontrolü(Handling): tut / çeldi(köşe-önü tehlike)
             kaçırdı → gol / direk (bant: kesişim direğe < 12 cm)
```

## 9.3. Çıkış Kararı (1v1 ve Ortalar)

- 1v1: Utility karşılaştırması — ÇıkGel skoru = OneOnOne/100 + (hücumcunun topa varışı − kalecinin varışı) × 1,4; KaldaKal skoru = Positioning tabanlı. Erken çık + kaybet = penaltı/kırmızı riski (11.2 bağlantısı).
- Orta yakalama: AerialCommand + varış marjı; başarısız yumruklama serbest top + ikinci top kargaşası (10.2) üretir.

## 9.4. Dağıtım

Kaleci top sahibi olduğunda 7.2 Utility'si kaleci aday setiyle çalışır: KısaAçıl (Kicking düşükse bias), UzunDegaj (Kicking), ElleAt (Throwing, hızlı hücum tetiği). Rakip pres yüksekse UzunDegaj bias'ı +0,25 (kalecinin ayağıyla oynama talimatı bunu tersler).

# 10. DURAN TOPLAR

## 10.1. Taç ve Kale Vuruşu

Sıkıştırılmış çözüm (3.4): kısa taç = pas modeline devir; uzun taç (talimat) = hava topu düellosu kurulumu. Kale vuruşu: kısa açılış veya orta saha hava düellosu — dağıtım Utility'sinin basitleştirilmiş hali.

## 10.2. Korner

- **Hedef bölge seçimi:** {ön direk, arka direk, penaltı noktası, kısa korner} — ağırlıklar: taktik talimat + SetPieces niteliği + rakip bölge zafiyeti (boy ortalaması karşılaştırması).
- **Hücum kurulumu:** En iyi 3 hava topçusu (JumpReach+Heading kompoziti) hedef bölgelere, savunma markaj çözücüsü (7.5) karşılık atar; kaleci AerialCommand'e göre çıkma kararı (9.3).
- **Çözüm zinciri:** Orta uçuşu (8.3, falso dahil) → bölgedeki en iyi marjlı hava düellosu (6.4) → kazanan: kafa şutu (xG hattı 15.2) / uzaklaştırma → uzaklaştırma kısa düşerse İKİNCİ TOP kargaşası: ceza yayı bölgesindeki ajanlara kontrol düellosu, kazanana bir Utility kararı (şut/yeniden orta). Kornerden gol kalibrasyon bandı: maç başına 0,10-0,16 [KALİBRE].

## 10.3. Frikik

- **Direkt eşik:** mesafe < 28 m ve açı uygunsa direkt şut adayı; SetPieces × açı faktörü skoru, orta/pas alternatifleriyle Utility'de yarışır.
- **Baraj modeli:** Baraj, kale açısının bir dilimini kapatır (kişi başı 0,35 rad yaklaşık); şutçu seçimi: BarajÜstü (falso ile aş: spin gereksinimi yüksek, Magnus 8.3), BarajYanı (açı dar), AlttanSert (baraj zıplarsa — REFEREE domain çekilişi, Yüksek chaos'ta olasılık artar).
- Direkt frikik gol bandı: 0,05-0,09/maç; frikik-orta üretimi korner zinciriyle aynı çözücüyü paylaşır.

## 10.4. Penaltı — Nişan/Tahmin Matris Oyunu

- Şutçu hedef dağılımı 3×2 ızgarada (köşeler yüksek EV); Composure düşükse "güvenli orta" bias'ı artar, kritik dakika baskısı (12.3) dağılımı daraltır.
- Kaleci karma strateji: yön tahmini {sol, orta, sağ} — şutçunun geçmiş penaltı event'leri seed'li ağırlık verir (asistan hafızasına da yazılır, GDD 7.5 çapraz besleme).
- P(gol) çekirdeği: doğru tahmin 0,55, yanlış tahmin 0,93, orta-orta 0,25; direk bandı 0,04; kalibrasyon hedefi ortalama 0,76 ± 0,03 [KALİBRE]. Tüm çekilişler SETPIECE domain.

## 10.5. Ofsayt Üretimi

Pas anında (ThroughPass event tick'i) alıcının x konumu son savunucuyla milimetre düzeyinde karşılaştırılır; ihlal marjı VAR incelemesine girdi olur (11.4). Ofsayt sayısı bandı: 2-5/maç.

# 11. HAKEM VE VAR SİSTEMİ

## 11.1. Hakem Profili

```csharp
RefereeProfile { Strictness 0-100, AdvantageTendency 0-100,
                 Consistency 0-100, HomeBias sabit 0 }   // adalet ilkesi
```

Profil maç başında REFEREE domain'inden lig hakem havuzundan seçilir; event log'a yazılır (Panorama "hakem gündemi" içeriği üretebilir, Modül 7).

## 11.2. Foul Tespiti ve Kart Mantığı

- Top kapma çözümü (6.4) kaybedildiğinde şiddet skoru üretilir: s = 0,4 × margin_açığı + 0,25 × hız + 0,2 × arkadan_mı + 0,15 × ayak_yüksekliği (0-1 normalize).
- Foul eşiği: s > 0,30 − (Strictness − 50) × 0,002; gri bant (eşik ± 0,06) REFEREE çekilişiyle çözülür — "tartışmalı pozisyon" doğal olarak buradan doğar.
- Kart: s > 0,55 sarı; s > 0,80 veya bariz gol şansı engelleme (son adam, x-koşulu) kırmızı; ikinci sarı otomatiği. Aggression yüksek oyuncu şiddet skoruna +0,05 taşır. Kart bandı: 3,5-5,5/maç [KALİBRE].
- **Avantaj:** Foul sonrası 1,2 sn içinde hücum eden takım tehdit hücresine ilerlerse (xT artışı) ve AdvantageTendency çekilişi tutarsa oyun devam, faul "geri dönüşlü" olarak loglanır.

## 11.3. Sakatlık Bağlantısı

s > 0,65 girişimler mağdur için sakatlık riski çekilişi tetikler (12.2); kart + sakatlık kombinasyonu Hikaye Motoru'na yüksek-önem event'i düşürür (GDD 7.2 tetikleyicileri).

## 11.4. VAR Dram Sistemi

- **İnceleme kapsamı (yalnız 4 sınıf):** gol öncesi ofsayt marjı < 0,30 m; gol öncesi atak fazında foul gri bandı; ceza sahası içi foul gri bandı; kırmızı kart gri bandı.
- **Akış:** Tetik → VAR_REVIEW fazı → dramatik bekleme süresi = 20 + 70 × zorluk sn (REFEREE çekilişi, sunumda gerilim müziği kancası) → karar.
- **Karar doğruluğu:** VAR gerçek-durumu bilir (motor zaten kesin veriye sahip); yanılma payı YALNIZ chaos seviyesine bağlıdır: Düşük chaos'ta VAR hatası 0; Orta'da gri bant kararlarının yüzde 4'ü; Yüksek'te yüzde 8'i "saha kararı kalır" ile sonuçlanır [KALİBRE]. Deterministiktir, REFEREE domain.
- **Event çıktısı:** VarReviewStarted / VarDecision event'leri; highlight puanına +0,25 dram katkısı (15.3); Panorama tartışma segmenti kancası.

# 12. MAÇ İÇİ DURUM MODELİ

## 12.1. Stamina (Enerji)

- **Durum:** Energy 0-1000 (0,1 hassasiyet). Karar tick'i başına drenaj: ΔE = k_e × (v / v_max)^2,2 × M_workrate × M_hava × M_zemin + pres_ek_maliyeti. k_e [KALİBRE] hedefi: ortalama oyuncu maçı Energy 350-550 bandında bitirir; yoğun pres talimatlı takım 80-120 puan daha fazla tüketir.
- **Toparlanma:** DEAD_BALL fazlarında +2/sn; devre arasında +150 (tavan 1000).
- **Etkiler:** M_kondisyon (6.2) üzerinden tüm nitelikler; v_max ve a_max doğrudan çarpan; Energy < 250 iken DECISION sigma +yüzde 20 (yorgun beyin hatası) ve sakatlık çarpanı devrede (12.2).
- **Sprint sayacı:** Yüksek yoğunluklu efor sayısı event log'a yazılır (Panorama "en çok koşan" segmenti + antrenman modülüne veri, GDD 4.3).

## 12.2. Sakatlık Üretimi

- **Tetik olayları:** sert müdahaleye maruz kalma (s > 0,65), Energy < 250 iken sprint, hava topu inişi, çarpışma.
- **Olasılık:** p = p_taban(olay) × M_yorgunluk × M_yatkınlık × M_zemin; M_yorgunluk = 1 + max(0, 300 − Energy)/300 × 1,5; M_yatkınlık oyuncu profilinden (0,7-1,6); INJURY domain.
- **Şiddet dağılımı** [KALİBRE]:

| Şiddet | Olasılık | Sonuç |
| --- | --- | --- |
| Hafif (devam eder) | %55 | Maç sonuna kadar nitelikler −5, iyileşme 0 gün |
| Küçük | %28 | Oyundan çıkmalı; 3-10 gün |
| Orta | %12 | 2-5 hafta; Medikal Merkez tier'ı süreyi kısaltır (GDD 4.3) |
| Ağır | %5 | 6-16 hafta; Hikaye Motoru'na yüksek-önem event |

- **Kalibrasyon bandı:** Toplam 0,35-0,60 sakatlık/maç (17.2).

## 12.3. Momentum ve Baskı

- **Takım momentumu M ∈ [−10, +10]:** Olay deltaları — gol +4 / yenilen −4, büyük kaçan −1, penaltı kazanma +2, rakibe kırmızı +2, kritik kurtarış +1, üst üste 3 başarılı pres +1. Dakikada 0,3 hızla 0'a söner.
- **Etkiler:** DECISION sigma ±yüzde 15 (7.6), M_moral nitelik çarpanı (6.2, ±5 puan tavan).
- **Kritik dakika baskısı:** dakika > 80 ve skor farkı ≤ 1 iken baskı katsayısı c = (dakika − 80)/10 × (2 − |fark|)/2; c, şut nişan sapmasına ve penaltı dağılım daralmasına (10.4) eklenir. Composure yüksek oyuncular c etkisinin yüzde 60'ını söndürür — "büyük maç oyuncusu" buradan doğar.
- **Adalet notu:** Maç DIŞI kaynaklı moral (hikaye arkları, GDD 7.4) yalnız başlangıç Momentum'una bantlı (−2..+2) girer; maç içi akış tamamen maçın kendisinden beslenir.

## 12.4. Hava ve Zemin Koşulları

| Koşul | Başlıca Etkiler |
| --- | --- |
| Kuru / Ilıman | Baz değerler (tüm çarpanlar 1,00) |
| Yağmur | Passing −8, FirstTouch −10, a_roll 2,6 (top kayar), sekme e 0,50, sakatlık ×1,10 |
| Kar | Top hızı −%10, Vision −10, a_roll 4,6, sekme e 0,38, v_max −%6, sakatlık ×1,15 |
| Sıcak (30°C+) | Stamina drenajı ×1,20; ikinci yarı kondisyon farkları belirginleşir |
| Rüzgar | Uzun top/orta sapması = rüzgar_hızı × k_w × uçuş_süresi vektörü; frikik-korner nişanına eklenir |
| Zemin Tier 1-2 (kötü) | FirstTouch −6, sekme pertürbasyonu ±2° (PHYSICS), sakatlık ×1,20 |
| Zemin Tier 4-5 (iyi) | Passing +2, sekme stabil; Tycoon bakım yatırımının sahaya yansıması (GDD 4.3) |

- Hava, MatchConfig'te deterministik atanır (lig takvim seed'i); tüm çarpanlar balance JSON'dadır.

# 13. CHAOS ENGINE MATEMATİĞİ

## 13.1. Enjeksiyon İlkesi

Chaos, YETENEĞİ düşürmez; ÇÖZÜMLERE bant içi gürültü ekler. Yalnız 5 noktada enjekte edilir, başka hiçbir yerde rastgelelik seviyeyle ölçeklenmez:

- Düello marjı gürültüsü (6.3) — DUEL domain
- Karar skoru gürültüsü (7.2) — DECISION domain
- Nişan sapması çarpanı (pas 6.5, şut 15.2, orta 8.3) — PHYSICS domain
- Hakem gri bandı genişliği + VAR hata payı (11.2, 11.4) — REFEREE domain
- Sekme pertürbasyonu (yalnız Yüksek seviye, 8.3) — PHYSICS domain

## 13.2. Seviye Tablosu [KALİBRE]

| Enjeksiyon Noktası | Düşük | Orta (Default) | Yüksek |
| --- | --- | --- | --- |
| Düello sigma (100 ölçeği) | 4 | 9 | 16 |
| Karar sigma (skor ölçeği) | 0,03 | 0,07 | 0,12 |
| Nişan sapması çarpanı | ×1,00 | ×1,15 | ×1,35 |
| Hakem gri bandı | ±0,03 | ±0,06 | ±0,10 |
| Sekme pertürbasyonu | 0 | 0 | ±3° |
| VAR "saha kararı kalır" oranı | %0 | %4 | %8 |

## 13.3. Dokunulmazlar

Chaos hiçbir seviyede şunlara dokunmaz: nitelik taban değerleri, xG kayıt modeli (istatistik gerçeği bozulmaz), stamina drenajı, sakatlık şiddet dağılımı, ekonomi/ödüller, event log doğruluğu. Böylece Yüksek chaos'ta bile analiz verisi güvenilir kalır.

## 13.4. Upset Kalibrasyon Hedefleri

10.000 maçlık setlerle doğrulanır (17.3): 75 ortalama güçteki takım, 55 ortalama güçteki takıma karşı Galibiyet/Beraberlik/Mağlubiyet hedefi:

| Seviye | Güçlü Kazanır | Beraberlik | Sürpriz |
| --- | --- | --- | --- |
| Düşük | %76 | %14 | %10 |
| Orta | %66 | %18 | %16 |
| Yüksek | %54 | %22 | %24 |

# 14. MÜDAHALE KATMANI (INTERRUPT ABSTRACTION)

## 14.1. Komut Akışı — Tek Kapı

UI butonu, LLM IntentAction'ı ve otomasyon aynı yoldan geçer: Kaynak → Command Bus → Doğrulayıcı (yetki, parametre bandı, hak sayısı, rate limit: takım başına dakikada 10 komut) → CommandQueue(tick damgalı) → Tick aşama 1'de uygulama (4.2). LLM için ayrı yürütme yolu yoktur (GDD 11.7).

## 14.2. Uygulama Anları

| Komut | Uygulama Anı | Gerekçe |
| --- | --- | --- |
| Taktik delta (mentalite, tempo, pres, hat) | Sonraki karar tick'i (≤250 ms) | Akıcı his; yapısal bozulma yok |
| Bireysel talimat / markaj değişimi | Sonraki karar tick'i | Aynı |
| Oyuncu değişikliği | Sonraki DEAD_BALL fazı | Gerçekçilik + deterministik sıralama |
| Formasyon / anchor değişikliği | Sonraki DEAD_BALL fazı | 22 hedefin tutarlı yeniden hesabı |
| Motivasyon konuşması | Anında; 10 dk bekleme süresi | Momentum aracı, spam korumalı |

## 14.3. Motivasyon Konuşması Mekaniği

- Tonlar: Sakinleştir (Momentum −'yi söndürür: +1..+2, kart riskini düşürür), Ateşle (+2 momentum, Aggression > 70 oyuncularda foul şiddet skoruna +0,04), Uyar (bireysel; hedef oyuncunun DECISION sigma'sını 10 dk yüzde 10 düşürür).
- Etki çarpanı: kaptanın Leadership'i × konuşma yerindeliği (skor durumu bağlamı). LLM Mod B'de yazılan serbest konuşma, güvenlik katmanında bu üç tondan birine sınıflanır — metin ne olursa olsun mekanik etki bantlıdır.

## 14.4. Canlı İzleme Senkronizasyonu

- Sunucu otoriterdir. Yayın: saniyede 1 keyframe (sıkıştırılmış durum ~2 KB) + aradaki event delta'ları. İstemci ara tick'leri AYNI motoru koşarak yeniden üretir; keyframe checksum'u tutmazsa sessiz yeniden senkron olur. Bant genişliği hedefi: < 6 KB/sn/izleyici.
- Geç katılım: son keyframe + hızlı event sarması (izleyici 5-10 sn içinde canlıya yetişir).
- İzleyici komutları yalnız kendi takımı için geçerlidir ve sunucu doğrulayıcısından geçer; izleme istemcisi asla otorite değildir.

## 14.5. Replay Bütünlüğü

Komut zaman çizelgesi (ICommandTimeline) replay dörtlüsünün parçasıdır (3.3): müdahaleli maçların replay'i, müdahaleleriyle birlikte bire bir yeniden oynar. Motivasyon konuşmalarının tonu event log'da görünür (Panorama "teknik direktör hamlesi" segmenti kancası).

# 15. OLAY GÜNLÜĞÜ, xG VE HIGHLIGHT TESPİTİ

## 15.1. MatchEvent Şeması

```csharp
public struct MatchEvent {
    public uint Tick;          // simülasyon zamanı
    public ushort Type;        // EventType enum
    public short ActorA;       // birincil oyuncu (yoksa -1)
    public short ActorB;       // ikincil oyuncu (pas alıcısı, faul mağduru...)
    public byte TeamIdx;       // 0 ev, 1 deplasman, 2 nötr
    public int X, Y;           // mm konum
    public int AuxData;        // tipe özel (şiddet skoru, VAR sınıfı, kart tipi)
    public float Xg;           // yalnız şut tiplerinde dolu
    public byte Flags;         // BigChance, FastBreak, SetPieceKaynaklı, VarİncelemeAdayı
}
```

| Kategori | Event Tipleri |
| --- | --- |
| Top akışı | PassCompleted, PassIntercepted, CrossDelivered, TouchError, DribblePast, TackleWon, BallOut |
| Şut zinciri | ShotOnTarget, ShotOffTarget, ShotBlocked, Goal, Save, Parry, Post, BigChanceMissed, AssistRecorded |
| Duran top | CornerAwarded, FreeKickAwarded, PenaltyAwarded, ThrowIn, Offside |
| Disiplin | FoulCommitted, YellowCard, RedCard, AdvantagePlayed |
| VAR | VarReviewStarted, VarDecision |
| Durum | PhaseChange, Substitution, TacticChange, MotivationTalk, InjuryOccurred, MomentumShift, StaminaAlert |

- Ring buffer 4096 event; maç sonunda kalıcı log (ortalama 900-1.400 event/maç). Bu log; LLM röportajı (GDD 5.5), Hikaye Motoru tetikleyicileri (GDD 7.2), Panorama seçici (GDD 8.4) ve istatistik ekranlarının TEK kaynağıdır.

## 15.2. xG Modeli

Her şut anında kayda geçen beklenen gol değeri (lojistik model):

z = −0,90 − 1,05 × ln(d / 10) + 1,35 × A − 0,45 × pres − 0,35 × kafa_mı + 0,25 × büyük_şans + 0,20 × tek_vs_tek [KALİBRE]

xG = 1 / (1 + e^(−z)), burada d = kaleye mesafe (m), A = iki direk arası görüş açısı (radyan), pres = 1,2 m içindeki savunucu sayısı (0-3 kırpılmış).

- xG modeli kayıt/analiz gerçeğidir; şutun FİİLİ sonucu 6.4 + 9.2 çözümlerinden gelir. İki modelin uzun vadeli tutarlılığı 17.2'de test edilir (sezon toplam gol ≈ toplam xG ±yüzde 8).
- Chaos, xG kaydına asla dokunmaz (13.3) — Yüksek chaos liginde bile analiz doğru kalır.

## 15.3. Highlight Puanlama

H = 0,35 × xG_salınımı + 0,20 × geç_dakika + 0,20 × skor_etkisi + 0,15 × nadirlik + 0,10 × hikaye_ilgisi

- xG_salınımı: olayın maç kazanma olasılığı eğrisinde yarattığı sıçrama (kayan WinProb modeli, 90 nokta örneklem).
- geç_dakika: dakika/90 doğrusal; skor_etkisi: beraberlik bozan/eşitleyen +1; nadirlik: event tipi taban tablosu (röveşata sınıfı vole, 30 m gol, penaltı kurtarışı yüksek); hikaye_ilgisi: aktif ark katılımcısı sahne alıyorsa +1 (Modül 6 çapraz beslemesi).
- H > 0,50 → zaman çizelgesi işareti (GDD 5.6); maç başına en yüksek 6 an klip önerisi olarak sunulur (GDD 8.3); Panorama seçici maçları toplam H ile sıralar.

## 15.4. Maç Sonu Veri Paketi (LLM/Panorama Girdisi)

```csharp
MatchSummaryPacket {
    skor, temel istatistik satırı (şut, isabet, korner, faul, possession),
    top10_event (H sıralı), momentum_eğrisi[90], winprob_eğrisi[90],
    ark_ilgili_eventler[], hakem_profil_özeti, hava_zemin
}
```

Röportaj promptu ve Hikaye Motoru beat üretimi bu paketi tüketir; ham event log LLM'e ASLA verilmez (token + determinizm disiplini).

# 16. LOD VE PERFORMANS BÜTÇELERİ

## 16.1. LOD Detay Matrisi

| Özellik | LOD 0 | LOD 1 | LOD 2 |
| --- | --- | --- | --- |
| Hareket / karar tick | 10 Hz / 4 Hz | 5 Hz / 2 Hz | Olay örneklemeli |
| Algı bütçesi (sorgu/karar) | 24 | 12 | — |
| Duran top çözünürlüğü | Tam (Bölüm 10) | Tam | Tablo tabanlı |
| Event granülerliği | Tam log | Tam log | Özet log (şut zinciri + kartlar) |
| Highlight/replay | Var | Var | Yok (yalnız skor + özet) |
| CPU hedefi / maç (tek çekirdek) | ≤ 2,5 sn | ≤ 0,8 sn | ≤ 10 ms |

- LOD 2 tabloları, LOD 0 ile koşulan 10.000 kalibrasyon maçından regresyonla türetilir (2.4); her balance güncellemesinde yeniden üretim CI adımıdır.

## 16.2. Bellek Disiplini (Zero-Alloc)

- Tüm sıcak yol struct + önceden ayrılmış diziler; maç içinde heap tahsisi hedefi 0 (event ring buffer dahil). String yok — tüm kimlikler ID.
- Hedefler: maç başına Gen-2 GC = 0; geçici tahsis < 50 KB; MatchState toplam < 64 KB (keyframe sıkıştırması 14.4 bunu ~2 KB'a indirir).

## 16.3. Sunucu Throughput Hesabı

- LOD 0 @ 2,5 sn/maç → 24 çekirdekli düğüm ≈ 9,6 maç/sn. Hedef pencere: 10.000 lig maçı / 10 dk = 16,7 maç/sn → 2 düğüm zirvede.
- Kaydırılmış maç saatleri (GDD 6.6) zirveyi yüzde 40-60 düşürür → 1 sürekli düğüm + otomatik ölçeklenen yedek. GDD 14.3 sunucu kalemleriyle tutarlıdır.
- Online maçlar LOD 0'dır (replay + highlight sözleşmesi); LOD 1-2 yalnız offline dünya simülasyonunda kullanılır.

## 16.4. İstemci Bütçeleri

- Canlı izleme (LOD 0 yeniden üretim + sunum): orta cihazda CPU < %20, 30 fps sabit (GDD 11.3).
- Offline sezon turu: 1 × LOD 0 + 9 × LOD 1 + ~200 × LOD 2 ≈ 12-18 sn (GDD 11.3 revizyon notu, 2.4).

# 17. TEST VE VALİDASYON PLANI

## 17.1. Determinizm Test Seti

- **Merge kapısı:** 100 maç × (Windows editör + Linux server build) checksum bit-eşitliği; tek fark = build FAIL.
- **Gece koşusu:** 1.000 maç × 4 platform (Win, Linux, Android arm64, iOS) + müdahaleli senaryolar (aynı komut zaman çizelgesi enjekte edilir).
- **Sıra bağımsızlık testi:** Aynı maç, komutlar farklı gerçek-zaman anlarında ama aynı tick damgalarıyla verilerek koşulur — sonuç bit-eşit olmalı (3.1 sayaç-RNG doğrulaması).

## 17.2. İstatistiksel Kalibrasyon Bantları (10.000 Maç Seti)

| Metrik (maç başına) | Hedef Bant |
| --- | --- |
| Gol | 2,4 - 3,0 |
| Şut / isabetli şut | 20-28 / 7-11 |
| Korner | 8-12 |
| Faul / sarı / kırmızı | 18-28 / 3,0-5,0 / 0,15-0,30 |
| Penaltı / ofsayt | 0,20-0,35 / 2-5 |
| Sakatlık | 0,35-0,60 |
| Pas isabet yüzdesi | 78-86 |
| Sezon toplam gol vs toplam xG sapması | ±%8 |
| Güçlü takım possession bandı (75v55) | %55-65 |

Bant dışı metrik = kalibrasyon FAIL → ilgili [KALİBRE] katsayıları güncellenir, kod değişmez (balance JSON disiplini).

## 17.3. Chaos Upset Doğrulaması

Her seviye için 10.000 maç (75v55 profili) → 13.4 tablosuna ±3 puan tolerans. Ek test: eşit güçte (65v65) beraberlik bandı %22-30 tüm seviyelerde.

## 17.4. Golden Replay ve CI Kapıları

- Her motor sürümünde 50 arşiv "golden replay" bit-eşit oynamalı (config_hash eşleşmesiyle); balance değişikliği yeni golden set üretir (GDD 16.3 Replay Uyumluluk Testi).
- CI kapıları sırayla: derleme → unit (%80+ coverage) → determinizm 100 → hızlı kalibrasyon 500 maç (geniş tolerans) → perf bütçesi (LOD 0 ≤ 2,75 sn tolerans) → zero-alloc denetimi. Gece: tam setler.

## 17.5. Feel İterasyonu Ölçümleri (FAZ 03 Alt-Fazı)

- 5 kişilik panel, 2 tur; yalnız izler. Ölçümler: "sıkıldım" işareti < 3/maç, highlight hatırlama > %70, tempo memnuniyeti anketi ≥ 4/5.
- Ayar sahası: sunum hız eğrisi, kamera vurgu eşikleri (H bazlı), ölü top sıkıştırma süreleri — hepsi balance JSON, motor mantığına dokunulmaz.

# 18. AÇIK SORULAR, FAZ 03 EŞLEMESİ VE KARAR GÜNLÜĞÜ

## 18.1. [KALİBRE] Katsayı Envanteri (Balance JSON Yolları)

sim.duel (P_taban, k_duel, kırpma bandı), sim.pass (sigma0, mesafe/pres çarpanları), sim.shot (xG katsayı seti b0-b6, nişan sapması), sim.gk (t_react, erişim, lojistik eğim), sim.setpiece (korner bölge ağırlıkları, penaltı matrisi, frikik eşikleri), sim.ref (foul eşiği, kart eşikleri, gri bant), sim.var (hata oranları, bekleme süresi), sim.stamina (k_e, toparlanma), sim.injury (p_taban seti, şiddet dağılımı), sim.momentum (delta tablosu, sönüm), sim.weather (tüm çarpan tabloları), sim.chaos (13.2 tablosu), sim.move (v_max, a_max, dribbling çarpanı), sim.xt (12×8 grid), sim.lod (tick oranları, bütçeler), sim.highlight (H ağırlıkları). Toplam ~140 ayarlanabilir değer; tamamı config_hash kapsamındadır.

## 18.2. Açık Sorular (FAZ 01-02'de Kararlaştırılacak)

- Çarpışma modeli: tam disk çözünürlüğü mü (yarıçap 0,4 m, önerilen) yoksa yumuşak itme alanı mı? Öneri: disk + itme karması.
- LOD 1'de highlight üretimi: tam mı, eşik yükseltilmiş mi? Öneri: eşik H > 0,65.
- Uzatma/penaltı serisi kupa formatları: motor destekli (bu spec kapsar) — lig modülü FAZ 04'te format bayraklarını bağlar.
- Golden replay arşiv boyutu ve saklama: 50 × ~20 KB önerisi yeterli mi? (Depolama politikası GDD 11.9 ile).

## 18.3. FAZ 03 Sprint Eşlemesi (6-8 Hafta)

- **Hafta 1-2:** Çekirdek döngü + determinizm altyapısı (RNG, tamsayı durum, checksum) + hareket/top fiziği + CI determinizm kapısı.
- **Hafta 3-4:** Utility/BT karar sistemi + pas/şut/düello çözümleri + kaleci modeli + event log.
- **Hafta 5:** Duran toplar + hakem/VAR + durum modeli (stamina/sakatlık/momentum) + hava/zemin.
- **Hafta 6:** 10.000 maç kalibrasyon koşuları → 17.2/17.3 bantlarına oturtma; LOD 1-2 türetme.
- **Hafta 7-8:** Feel İterasyonu (17.5) + golden replay seti + FAZ 04 entegrasyon arayüz dondurması.

## 18.4. Karar Günlüğü

| Versiyon | Tarih | Kararlar |
| --- | --- | --- |
| v1.0 | Temmuz 2026 | Mimari: Seçenek C (sürekli çekirdek + LOD). Sayaç-tabanlı durumsuz RNG + domain akışları. Tamsayı (mm) kalıcı durum + LUT trigonometri + checksum sözleşmesi. Anchor ağırlığı 0,45-0,60 (serbest diziliş omurga). LOD 2 tabloları LOD 0 regresyonundan türetilir. VAR kapsamı 4 olay sınıfı; chaos 5 enjeksiyon noktası. GDD 11.3 performans hedefi revizyon notu düşüldü. Ek sistemler (duran top, hava/zemin, hakem/VAR, kaleci) v1.0 kapsamına alındı. |

> **ONAY DURUMU — MATCH ENGINE TEKNİK SPESİFİKASYONU v1.0**
> Bu belge, GDD v4.0 Bölüm 5'in bağlayıcı mühendislik ekidir. FAZ 03 implementasyonu, CI kabul kapıları ve balance JSON şeması bu dokümandan türetilir. [KALİBRE] etiketli tüm katsayılar başlangıç değeridir; 17. bölüm testleri geçmeden kesinleşmez.
> — Full Blueprint Edition · v1.0 —
