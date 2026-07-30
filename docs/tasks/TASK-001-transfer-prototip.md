# Task Brief 001: Transfer Loop Dikey Prototipi (FAZ 00)

## Objective
Konsept doğrulama: transfer pazarlığı core-loop'unun "his" testi. 3 dakikada bir
transfer tamamlanabiliyor mu, pazarlık gerilimi eğlenceli mi? (GDD §17 FAZ 00)

## Önkoşul
Unity 6 projesi kurulu (unity/UNITY_SETUP.md tamam), sim paketi görünüyor.

## Scope
- In: tek sahne; 10 kurgusal oyunculuk sabit liste; teklif ver → kural-tabanlı
  karşı teklif → kabul/ret/pazarlık döngüsü; UI Toolkit placeholder arayüz.
- Out (dokunma): gerçek DB, LLM, ağ, kalıcılık, sanat, ME Spec sistemleri.

## Context to read first
CLAUDE.md → docs/DECISIONS.md → docs/GDD_v4_1.md (§1.2, §17 FAZ 00) → docs/CommandBus_Spec_v1_0.md §2 (prototipte bile durum değişikliği tek fonksiyondan geçsin — alışkanlık).

## States
idle / teklif hazırlama / karşı-teklif / kabul / ret / iptal.

## Kurallar
- Karşı teklif DETERMİNİSTİK: TheBadge.Sim.Determinism.Rng kullan (Domain.Decision, seed sabit) — prototipte bile System.Random YASAK.
- Yeni bağımlılık yok; [KALİBRE] adayı sayılar tek bir PrototypeBalance.cs'te toplansın (sonra JSON'a taşınır).

## Acceptance criteria
- 60 sn içinde ilk teklif verilebiliyor; tam döngü ≤3 dk.
- Aynı seed + aynı teklifler = aynı karşı teklifler (elle doğrula, raporla).
- Checks yeşil kalıyor.

## Verification required
Derleme kanıtı + 30-60 sn ekran kaydı + varsayım/risk raporu + "his" notların (eğlenceli mi, nerede sürtünme var).
