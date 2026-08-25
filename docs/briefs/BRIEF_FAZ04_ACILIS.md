# BRIEF — FAZ 04 AÇILIŞI: Core Modüller (Kadro · Transfer · Tycoon · Online+LLM)

Tarih: 2026-08-23 · Önkoşul: **FAZ 03 kapandı** (PR #13/#14/#15 merge; ME 17.2 13/13,
50 golden replay bit-eşit, arayüz `docs/INTERFACE_FREEZE_FAZ04.md` ile dondurulmuş)
Çalışma dalı: **`faz04/tycoon`** · Süreç: CLAUDE.md akışı (plan → uygula → kanıt → kayıt → PR)

## 1. Amaç

GDD FAZ 04'ün beş core modülünü kurmak: Squad Management, Transfer Market AI, Tycoon Economy,
Online (Nakama) ve LLM katmanı. FAZ 03 maçı üretiyor; bu faz **maçın etrafındaki dünyayı** kurar.

## 2. Sıra neden böyle (keyfi değil)

Anayasa değişmezi #1 **Tek Kapı**: oyun durumunu değiştiren HER eylem `CommandEnvelope` ile
Command Bus'tan geçer; doğrudan state mutasyonu yazan kod reddedilir. Bugün hub tarafında
Command Bus **yok** — yalnız maç içi ucu (`CommandQueue`, ME 14.1) var. Squad/Transfer/Tycoon
modülleri bus'tan önce yazılırsa ya değişmezi ihlal eder ya yeniden yazılır. Bu yüzden:

| Sıra | Dilim | Neden burada |
| --- | --- | --- |
| **K1** ✅ | **Command Bus çekirdeği** (CB 3-6, 8) | Diğer her modülün geçmek zorunda olduğu kapı |
| **K2** ✅ | Dünya durumu (`GameState`): kulüp, kadro, finans, takvim | Kapı 3'ün (bağlam/sahiplik/kaynak) denetleyeceği durum |
| K3 | Tycoon Economy (CB 4.1, 9 aksiyon) | En kapalı devre modül; ekonomi bantları ECONOMY_MAP sözleşmesi |
| K4 | Squad Management (CB 4.2) | Maç motoruna en yakın; anchor/rol/talimat zaten ME'de karşılığı var |
| K5 | Transfer Market AI (CB 4.3) | Değerleme + pazarlık; K2 finans ve K4 kadro üstüne oturur |
| K6 | Online (Nakama RPC) + SimWorker | Sunucu otoritesi; K1-K5 tamamlanmadan sözleşme donmaz |
| K7 | LLM hattı (Mod B) + injection savunması (CB 7) | Öneri ≠ yürütme; bus ve katalog hazır olmadan bağlanamaz |

## 3. K1 kapsamı — Command Bus çekirdeği

**Yeni paket:** `shared/TheBadge.CommandBus` (netstandard2.1, **bağımlılıksız** — UnityEngine ve
JSON kütüphanesi SIZMAZ; `TheBadge.Sim` ile aynı disiplin). Hem sunucu hem Unity aynı doğrulamayı
çalıştırır: istemci ön-doğrular, sunucu **yeniden** doğrular (otorite sunucudadır).

- **Katalog v1 — 32 aksiyon** (CB 4.1-4.4): her aksiyon için tier (0-2), bağlam (Hub/Maç/Online),
  rate-limit sınıfı ve parametre tanımları. Tier katalogda SABİTTİR ve kaynaktan bağımsızdır —
  LLM kaynaklı komut tier'ını asla düşüremez (CB 6).
- **4 kapılı doğrulama zinciri** (CB 5), deterministik sırayla, ilk hatada durur:
  1. Katalog + şema (sıkı mod: eksik alan, tip hatası, **fazladan alan** = `SchemaViolation`)
  2. Parametre bandı (bantlar `balance/command.bands.json`'dan — kodda magic number yok)
  3. Bağlam/sahiplik/kaynak/hak — `IValidationContext` arayüzü (uygulamaları K2-K5 ile gelir)
  4. Rate limit (kayan pencere, userId + aksiyon sınıfı; CB 5.1 tablosu)
- **Idempotency** (CB 8.1): `CommandId` dedup penceresi; aynı Id ikinci kez YÜRÜTÜLMEZ, önceki
  yanıt aynen döner (at-least-once → exactly-once etkisi).
- **JSON sınırı:** çekirdek JSON parse etmez. Host ham payload'ı ayrıştırıp `IPayloadView`
  (alan adları + tipli okuyucular) olarak verir; şema sıkılığı çekirdekte denetlenir.
  Bu, ME 3.3'te `BalanceHash` için kurulan desenin aynısıdır.

**Kabul kapıları (K1):** katalog tamlığı (32 aksiyon, tier/bağlam/sınıf dolu) · şema sıkılığı
(fazladan alan reddedilir) · bant zorlaması (her bantlı parametre için sınır testi) · rate limit
(sınıf başına pencere) · idempotency (aynı Id iki kez = tek yürütme) · **red determinizmi**
(aynı zarf + aynı bağlam = aynı red sebebi) · tier bütünlüğü (LLM kaynağı tier düşüremez).

## 3b. K2 kapsamı — dünya durumu çekirdeği (✅ tamam)

**Yeni paket:** `shared/TheBadge.World` (netstandard2.1, bağımlılıksız). `GameState` (kulüp/kadro/
finans/takvim, tamsayı + kanonik sıra), `WorldHash` (xxHash64), `WorldJournal` (atomik yazma),
`WorldContext` (Kapı 3), `WorldExecutor` (Tek Kapı'nın yazma ucu), `balance/world.balance.json`.

**Sınır — K2 nerede durdu:** 32 aksiyonun hiçbirinin handler'ı YOK, bu tasarım gereği. K2 durumu
ve mekanizmayı verir; aksiyon semantiği K3-K5'in `IActionRule`/`IActionHandler` uygulamalarıyla
gelir. `WorldExecutor.UnboundActions()` bağlanmamış aksiyonları listeler — sayısı K3-K5 ilerledikçe
düşer ve **ilerlemenin ölçüsüdür**. Kalıcılık ve otorite bağlaması K6'dadır.

**Kabul kapıları (K2, 10 adet):** kanonik durum · hash kapsamı (30 kalıcı alan) · Kapı 3 sebep
tablosu · K3-K5 kural seami · atomiklik · sahte başarı yok · yürütme determinizmi · eşzamanlılık ·
balance zorlaması · Tek Kapı uçtan uca.

## 4. Atilla'ya karar maddeleri (K3-K6 öncesi)

1. ~~**Dünya durumu nerede yaşar?**~~ → **ZATEN KAPALI** (K2 sırasında kayıtlar okununca görüldü):
   **D3 (2026-07-30) = G3, sunucu-otoriter**; GDD 6.3 "maç motoru online liglerde asla oyuncunun
   cihazında çalışmaz"; GDD 11.2 "komut doğrulama .NET C# servis katmanında koşar". CB 8.3 de
   offline için "kod tek, davranış özdeş" diyor — yani durum çekirdeği her iki okumada da aynıdır
   ve `shared/`te yaşar. Bu soruyu brief'e yazmam gereksizdi.
   **Geriye kalan gerçek soru:** offline kuyruk bağlantı dönünce sunucu durumuyla çelişirse ne olur?
   Öneri: sunucu kazanır ama düşen komutlar kullanıcıya RAPOR edilir (CB 8.2 "sessiz üzerine yazma
   yoktur"). Bu karar **K6'yı bloklar, K3-K5'i bloklamaz** — ayrıntı `docs/DECISIONS.md` bekleyen
   kararlar.
2. **Komut bantları config_hash kapsamına girsin mi?** Bantlar hangi komutun kabul edildiğini
   belirler → komut zaman çizelgesini → replay'i etkiler. Öneri: **evet**, `command.bands.json`
   da config_hash'e girer (M17'nin `BalanceHash` deseni ikinci dosyaya genişletilir).
3. **Katalog sürümü nasıl ilerler?** Öneri: aksiyon ekleme minor, parametre/bant değişikliği
   major; istemci desteklenmeyen sürümde `UnsupportedCatalogVersion` alır (CB 3.2 zaten böyle).

## 5. Bu fazda taşınan FAZ 03 borçları (arayüzü DEĞİŞTİRMEZ)

`docs/INTERFACE_FREEZE_FAZ04.md` §7'deki dört madde: upset'in son 3 puanı (atak zinciri),
M12'nin 2 VAR sınıfı, LOD 2 kompozisyon hatası, Yüksek chaos hedefi. Hiçbiri FAZ 04'ü
bloklamaz; kapatıldıklarında golden replay seti yeniden üretilir.
