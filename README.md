# The Badge — Starter Repo

Ultimate Soccer Manager 98 modern remake. Unity 6 (mobil) + .NET 8 C# simülasyon servisi + Nakama.

**Anayasa:** `CLAUDE.md` her Claude Code oturumunda otomatik okunur. Bağlayıcı spesifikasyonlar `docs/` altındadır.

## Hızlı Başlangıç
```bash
dotnet run --project shared/TheBadge.Sim.Checks -c Release   # determinizm kapısı (yeşil olmalı)
```

## Yapı
| Yol | Ne |
| --- | --- |
| `CLAUDE.md` | Claude Code proje anayasası (kurallar, yasaklar, iş akışı) |
| `docs/` | GDD v4.1 + Match Engine Spec + Command Bus Spec (markdown) |
| `shared/TheBadge.Sim` | Deterministik çekirdek — saf C#, UnityEngine YOK; Unity'ye local package, sunucuya ProjectReference |
| `shared/TheBadge.Sim.Checks` | Bağımlılıksız determinizm test kapısı (CI bunu koşar) |
| `server/` | .NET SimWorker iskeleti (FAZ 04'te büyür) |
| `unity/` | Unity 6 projesi buraya açılır → `unity/UNITY_SETUP.md` |
| `balance/sim.balance.json` | Tüm [KALİBRE] katsayılar — koda sabit yazılmaz |
| `.github/workflows/` | ci-sim (gün-1 aktif) + ci-unity (GameCI, lisansla açılır) |

## Tek-Kaynak İlkesi
`TheBadge.Sim` hem Unity istemcisinde hem .NET sunucusunda AYNI kaynak koddan derlenir (GDD v4.1 §11.1). `noEngineReferences: true` bunu asmdef düzeyinde zorlar.
