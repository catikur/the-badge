# FAZ 04 Entegrasyon Arayüz Dondurması — v1.0

Tarih: 2026-08-21 · Kaynak: ME Spec 18.3 (Hafta 7-8) · Durum: **DONDURULDU**
Motor sürümü: golden replay seti `m17-golden-v1` · Checks: tüm kapılar yeşil

Bu doküman `shared/TheBadge.Sim`'in FAZ 04 tüketicilerine (Nakama RPC köprüsü, `TheBadge.SimWorker`,
Unity istemcisi) verdiği **sözleşmeyi** dondurur. Buradaki imzalar FAZ 04 boyunca kırılmaz;
kırılması gerekirse `docs/DECISIONS.md`'ye gerekçeli kayıt + golden replay setinin yeniden
üretimi zorunludur (ME 17.4).

> Bu bir SPEC DEĞİL, spec'in uygulanmış hâlinin kaydıdır. ME Spec v1.0 bağlayıcı belge olarak kalır.

## 1. Giriş noktası

```csharp
var cfg = new MatchConfig { Seed, EngineVersion, BalanceHash, Home, Away, Referee,
                            Weather, PitchTier, WindMS, WindDirX, WindDirY, Chaos, Lod };
cfg.ConfigHash = ConfigHash.Compute(cfg, balanceBytesHash);   // ME 3.3
var queue = new CommandQueue();                                // Tek Kapı'nın maç içi ucu
var engine = new MatchEngine(cfg.Seed, queue, cfg, balance) { AutoManage = true };
var state  = MatchEngine.CreateInitialState(cfg);
MatchResult result = engine.Run(ref state);                    // veya engine.Tick(ref state)
MatchSummaryPacket packet = engine.BuildSummary(in state);     // ME 15.4
ulong checksum = MatchEngine.StateHash(in state);              // ME 3.2
```

- `AutoManage`: canlı kullanıcı YOKKEN motor zorunlu kararları (değişiklik vb.) kendi verir.
- `Run` headless tam maç; `Tick` adım adım (canlı izleme / sunum katmanı).
- **Motor `ConfigHash`'i OKUMAZ** — kimlik alanıdır, sonuç üretimine girmez.

## 2. Determinizm sözleşmesi (ME 3.2/3.3 — pazarlıksız)

| Güvence | Nasıl doğrulanır |
| --- | --- |
| Aynı `(seed, config_hash, komut zaman çizelgesi)` = bit-eşit sonuç | `M17GoldenReplay` (50 replay) |
| Balance dosyasında tek bayt değişikliği → farklı `config_hash` | `M17ReplaySetiGuncel` |
| Kurulumun her alanı kimliğe bağlı (9 alan) | `M17ConfigHashAyirtEdici` |
| Kanonik durum özeti alan sırası SABİT | `MatchEngine.StateHash` + golden'lar |

**Replay dörtlüsü:** `{ EngineVersion, ConfigHash, Seed, komut zaman çizelgesi }`. Dördü saklanırsa
maç yeniden üretilebilir; biri eksikse ÜRETİLEMEZ. Arşiv formatı:
`shared/TheBadge.Sim.Checks/goldens/replay_set_v1.json`, üretici `-- gen-replays`.

`config_hash` girdileri: motor sürümü · LOD · tick oranı · **balance ham bayt özeti** · chaos ·
hava · zemin tier'ı · rüzgar (kuantalanmış) · hakem profili · kadro anlık görüntüsü.

> **Bilinçli sapma (kayıtlı):** ME 3.3 "balanceJson_kanonik_bytes" der; çekirdek JSON parse
> etmediği için (CLAUDE.md bağımlılıksızlık kuralı) ham bayt özetini HOST hesaplar ve
> `MatchConfig.BalanceHash` ile verir. Spec'in amacı birebir korunur.

## 3. Veri sözleşmeleri (FAZ 04'ün okuduğu yüzeyler)

- **Girdi:** `MatchConfig` · `TeamSheet { Starters[11], Bench }` · `PlayerEntry { PlayerId, Name,
  RoleId, AnchorXmm, AnchorYmm, Attributes }` · `PlayerAttributes` (26 nitelik) · `RefereeProfile`.
- **Komut:** `MatchCommand` → `SubstitutionCmd` · `TacticChangeCmd(TacticDelta)` · `MotivationCmd(ToneType)`.
  Uygulama sırası deterministiktir: `(IssueTick artan, kuyruğa giriş sırası artan)`; kanonik iz
  `CommandQueue.AppliedTraceHash`.
- **Çıktı:** `MatchResult` (skor, süre, sayaçlar, xG, checksum) · `MatchSummaryPacket`
  (`MatchStatLine` ev/deplasman + eğriler + en yüksek anlar) · olay log'u
  (`EventCount`/`GetEvent(i)` → `MatchEvent`, 6 kategori / 30 tip, ME 15.1).
- **Olay log'u TEK YÖNLÜDÜR:** sim okumaz, `StateHash`'e girmez. Sunum ve LLM katmanının
  tek gerçek kaynağıdır (istatistik log'dan türer — çift muhasebe yasak).

## 4. Balance sözleşmesi

`SimBalance` alan adları `balance/sim.balance.json` anahtarlarıyla BİREBİRDİR (System.Text.Json
`IncludeFields` / Unity `JsonUtility` uyumu). Çekirdek JSON okumaz; host doldurur.
`balance/sim.lod2.json` üretilmiş tablodur (`-- fit-lod2`), elle düzenlenmez.

## 5. LOD sözleşmesi (ME 16.1/16.3)

| LOD | Kullanım | Bütçe (ölçülen) |
| --- | --- | --- |
| Lod0 | Online maçlar — ZORUNLU (replay + highlight sözleşmesi) | ~140 ms/maç |
| Lod1 | Lod0'ın bit-eşleniği (`M15Lod1Esdeger`) | = Lod0 |
| Lod2 | Arka plan dünya simülasyonu — ızgara tablosu | ~2,5 µs/maç |

## 6. Dondurma kapsamı DIŞINDA kalanlar (bilerek)

- `[KALİBRE]` katsayı DEĞERLERİ — balance dosyası sezon içi donuk, sürümler arası ayarlanabilir
  (değişiklik golden replay setini yeniden ürettirir).
- Motor-yerel teşhis sayaçları (`Woodwork`, `LongBallsByTeam`, `Pres01`, `LooseGoalKind` …) —
  hash dışıdır, sözleşme değildir, haber vermeden değişebilir.
- `docs/DECISIONS.md`'de "Bekleyen kararlar" altındaki açık maddeler.

## 7. Bilinen açık borçlar (dondurma bunları KAPATMAZ)

| Borç | Durum |
| --- | --- |
| ME 13.4 upset — 75v55 %80/%13/%7, revize hedef %78/%12/%10 | sürpriz payı 3 puan eksik; kök: atak zinciri (M16-H ölçümü) |
| M12'nin 2 VAR sınıfı (askıda gol durumu) | faz makinesi eklemesi, ayrı dilim |
| LOD 2 kompozisyon hatası %34 (hedef <%25) | hücum/savunma ekseni ayrımı |
| Yüksek chaos upset hedefi (~%68) | chaos borcu |

Bu borçların hiçbiri arayüzü değiştirmez — motor İÇİ davranış işidir. Kapatıldıklarında
golden replay seti yeniden üretilir, sözleşme aynı kalır.
