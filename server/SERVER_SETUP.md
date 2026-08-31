# Sunucu Topolojisi (GDD v4.1 §11.1)
- **Nakama:** auth, lig, sosyal, eşleştirme (Docker; ölçekte managed değerlendirilir §14.3)
- **TheBadge.SimWorker (.NET 8):** Match Engine + doğrulama zinciri — `shared/TheBadge.Sim` ProjectReference ile AYNI C# kod
## Durum (K9-C, 2026-08-30)

**YAPILDI — taşımadan bağımsız katman, `TheBadge.Sim.Checks` K9 kapılarıyla ölçülüyor:**
- `command.submit` akışının sunucu tarafı: `TheBadge.World.RpcKopru` (bus 4 kapı + CB 8.1 dedup →
  `WorldExecutor` atomik commit → outbox pompası → `KomutYaniti`).
- CB 8.2 `newStateVersion` yanıtta dönüyor.
- CB 8.1 24 saatlik dedup: K1'den beri var (`IdempotencyStore`); köprü yanıtı `Tekrar` bayrağıyla
  taşıyor. SimWorker çıktısında `submit#2 → tekrar=True` ile görülebilir.
- Transactional outbox: yayın, durum commit'iyle aynı atomik adımda kalıcı kayda yazılır; ağa
  teslimi ayrı pompa yapar. Süreç ölümünde kayıt DURUR, yeniden başlatmada teslim edilir.
  Teslim en-az-bir-kez → uzak taraf `CommandId` ile dedup yapmak zorunda (arayüz taşıyor).

**YAPILMADI — bu ortamda koşturulamadığı için yazılmadı (CLAUDE.md: kanıtlanamayan kod eklenmez):**
- Nakama RPC kaydının kendisi. Bağlanacağı dikiş hazır: `TheBadge.World.IKomutTasima`.
  Değişecek tek yer, `kopru.Gonder(...)` çağrısını bir RPC handler'ının içine almak.
- PostgreSQL persist: `IOutboxStore` arayüzü bunun dikişidir; bugün `BellekOutboxStore` var.
  Gerçek deponun tek şartı, outbox yazmasının durum yazmasıyla AYNI işlemde commit edilmesidir —
  outbox'ın bütün değeri o özellikten gelir.
- Keyframe yayını (ME Spec 14.4).

Çalıştırma: `dotnet run --project server/TheBadge.SimWorker`
