# Unity 6 Proje Kurulumu
1. Unity Hub → New Project → **Unity 6 LTS** → 2D (Mobile) şablonu → konum: `unity/TheBadge`
2. `unity/TheBadge/Packages/manifest.json` dependencies içine ekle:

   "com.thebadge.sim": "file:../../../shared/TheBadge.Sim"

3. Doğrulama: Project panelinde Packages → The Badge Sim Core görünmeli. `noEngineReferences: true` sayesinde çekirdeğe UnityEngine sızamaz.

## Assets asmdef Haritası (FAZ 01'de kurulacak)
| asmdef | İçerik | Referanslar |
| --- | --- | --- |
| Game.Commands | Command Bus istemci ucu, katalog önbelleği | TheBadge.Sim |
| Game.Services | Nakama istemcisi, save/load, telemetri | Game.Commands |
| Game.UI | UI Toolkit ekranları, Rive köprüleri | Game.Services |
| Game.Match | Maç sunum katmanı (izleme/replay oynatıcı) | TheBadge.Sim, Game.Services |
| Tests.EditMode / Tests.PlayMode | Unity testleri | ilgili modüller |

Kural: sunum katmanı sim durumunu OKUR, asla doğrudan yazmaz — durum değişikliği yalnız Command Bus (Tek Kapı).
