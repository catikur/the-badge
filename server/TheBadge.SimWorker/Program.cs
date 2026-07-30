using System;
using TheBadge.Sim.Determinism;

// FAZ 04'te Nakama RPC dinleyen gerçek worker'a dönüşecek iskelet.
Console.WriteLine("The Badge SimWorker — iskelet. Paylaşılan çekirdek testi:");
Console.WriteLine($"Hash64 ornegi: 0x{Rng.Hash64(1, 1, 1, 1, 1):X}");
