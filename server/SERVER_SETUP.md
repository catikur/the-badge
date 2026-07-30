# Sunucu Topolojisi (GDD v4.1 §11.1)
- **Nakama:** auth, lig, sosyal, eşleştirme (Docker; ölçekte managed değerlendirilir §14.3)
- **TheBadge.SimWorker (.NET 8):** Match Engine + doğrulama zinciri — `shared/TheBadge.Sim` ProjectReference ile AYNI C# kod
- FAZ 04 işleri: Nakama RPC köprüsü, komut kuyruğu, PostgreSQL persist, keyframe yayını (ME Spec 14.4)

Çalıştırma: `dotnet run --project server/TheBadge.SimWorker`
