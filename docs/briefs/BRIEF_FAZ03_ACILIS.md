# BRIEF — FAZ 03 AÇILIŞI: Match Engine (ME Spec v1.0 gerçek motoru)

Tarih: 2026-08-08 · Önkoşul: Fun Gate KAPANDI (NO-GO %40 — DECISIONS 2026-08-08)
Çalışma dalı: **`faz03/match-engine`** (yeni dal; greybox dalı PR #1 ile kapanır — Atilla kararı)
Süreç: her görevde plan → Atilla onayı → uygulama → kanıt (CLAUDE.md akışı; yazı→onay→kod sürer)

## 1. Amaç

`shared/TheBadge.Sim` içinde ME Spec v1.0'ın DETERMİNİSTİK maç motorunu kurmak. Greybox'ın
cevaplayamadığı "gerçek menajerlik derinliği" (tam nitelikli oyuncular, vasıflı kaleciler,
gerçek kurtarış/duello/xG modeli) bu fazın işidir — artık vekil/kopya yok.

## 2. Bağlayıcı dayanaklar (kod bu bölümlere uyar)

| Konu | ME Spec |
| --- | --- |
| Determinizm: sayaç-RNG domain akışları, int durum (mm), çapraz platform bit eşitliği | 3.1-3.3 |
| Tick pipeline (karar→fizik→duello→event/durum→checksum) | 4.x |
| **Nitelik tablosu** — fiziksel/teknik/mental + **kaleci: Reflexes, Handling, OneOnOne, AerialCommand, Kicking, Throwing**; A_eff = taban × kondisyon × moral × hava × zemin | 6.1-6.2 |
| Karar mimarisi (utility skor: threat/risk/tactic/role/fatigue/chaos) | 7.x |
| Kaleci modeli: pozisyon açıortayı, t_react=f(Reflexes), tutuş=f(Handling), dağıtım=f(Kicking/Throwing), penaltı karma strateji | 8.4/10.x/11.x ilgili bölümler |
| Hakem/kart (bant 3.5-5.5) + VAR 4 sınıf + avantaj | 11.x |
| Durum modeli: stamina (Energy 0-1000), sakatlık (INJURY domain, bant 0.35-0.60), moral, hava/zemin | 12.x |
| Müdahale katmanı — Tek Kapı, uygulama anları (taktik ≤250ms, değişiklik DEAD_BALL) | 14.x |
| Event log + xG + highlight tespiti (istatistik ekranlarının TEK kaynağı) | 15.x |
| Performans/bellek (zero-alloc sıcak yol) + kalibrasyon bantları | 16-17 |

CB Spec: `match.*` komutları (substitution Tier 1, motivation_talk, squad.set_team_tactic
{mentalite, tempo, pres, hat}) — greybox'taki hafif bus yerini 4 kapılı doğrulamaya bırakmaya başlar
(tam doğrulama FAZ 04'te tamamlanır).

## 3. Greybox'tan TAŞINANLAR (kod değil, ÖĞRENİM ve değerler)

1. **Görünür olasılık + müdahale→tepki döngüsü** (kazanma şeridi, "G %38→%45"): sunum katmanı
   kavramı olarak taşınır — **NO-GO nedeniyle olduğu gibi DEĞİL, motor üstünde yeniden tasarlanarak**
   (Dikey Dilim öncesi mülakatlı doğrulama turu borcu — DECISIONS).
2. **Zorunlu karar anı deseni** (sakatlıkta akış durur, skip atlayamaz) — motor event'lerine bağlanır.
3. **Kalibrasyon deneyimi:** `greybox.balance.json` değerleri [KALİBRE] aday listesi (blok gol bandı,
   tempo çarpanları, drenaj, olay bantları, taktik matchup hisleri) — sim.balance.json şeması ME 3.3.
4. **Sahiplik değişmezi + sahne sözleşmesi denetim yöntemi** (yazılı sözleşme → otomatik audit) —
   motorun test disiplinine taşınır (Checks + kalibrasyon koşuları).
5. **Telemetri olay tasarımı** (intervention/substitution/incident/block_result deseni).
6. Playtest dersi (süreç): mülakat + telemetri OLMADAN tur koşulmaz — kopuş nedeni verisiz kalıyor.

Greybox prototipi EMEKLİ: `Game.Greybox` asmdef'i atılacak kod olarak kalır (referans), yeni
geliştirme almaz; `greybox.balance.json` config_hash dışıydı, taşıma listesi çıkınca silinebilir.

## 4. İlk dilim önerisi (onaya sunulacak plan taslağı — ME Spec hafta planına hizalı)

1. **M0 — Motor iskeleti:** MatchState (int/mm), tick pipeline boş geçişleri, checksum, GoldenHash
   genişletmesi; Checks'e motor kapıları. (ME 3-4)
2. **M1 — Nitelik + TeamSheet:** 6.1 tablosu, EffectiveAttribute (salt-okunur taban + runtime
   çarpanlar), kadro/formasyon yükleme; kaleci nitelik seti dahil. (ME 6)
3. **M2 — Karar + hareket çekirdeği:** utility karar, koşu/pas/şut ilkelleri, sahiplik (greybox
   değişmezinin motor hali). (ME 7-9)
4. **M3 — Kaleci + şut çözümü:** t_react/Reflexes, Handling tutuş dalları, xG kaydı. (ME 10-11, 15.2)
5. Sonrası ME Spec hafta planını izler (duran top, hakem/VAR, durum modeli, müdahale, LOD).

Her dilim: plan → onay → kod → `dotnet run --project shared/TheBadge.Sim.Checks -c Release` yeşil
+ kalibrasyon bantları (17.2) + determinizm kapısı. Sim koduna dokunan her PR determinizm kapısından geçer.

## 5. Kapsam DIŞI (bu açılışta)

- FAZ 02 UI seti ve sanat (stil rehberi kapısı ayrı); FAZ 04 tam 4 kapılı doğrulama + Nakama köprüsü;
  FAZ 05 prerender sunum. LLM özellikleri (golden set kuralları geçerli, bu fazda yok).
- Maç SUNUMUNUN yeniden tasarımı ayrı bir tasarım turudur (Dikey Dilim öncesi) — motorla karışmaz.

## 6. DoD-G (faz boyu geçerli)

Sim.Checks yeşil + EditMode/PlayMode testleri + determinizm kanıtı (aynı seed çapraz koşu) +
kalibrasyon bant raporu + temiz konsol; performansa dokunan işte profiler ÖNCE/SONRA.
