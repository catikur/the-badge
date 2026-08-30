using System;
using System.Collections.Generic;
using TheBadge.CommandBus;
using TheBadge.Sim.Commands;

namespace TheBadge.World
{
    /// <summary>Aksiyon yürütücüsü — K3-K5 doldurur. Handler durumu DOĞRUDAN DEĞİŞTİRMEZ:
    /// yazmalarını `WorldJournal`a kuyruklar ve `None` döndürürse `WorldExecutor` uygular.
    /// Hata döndürdüğünde journal atılır; geri alınacak bir şey yoktur.</summary>
    public interface IActionHandler
    {
        RejectionReason Apply(GameState st, WorldJournal journal, CommandEnvelope env,
                              ActionDef action, IPayloadView payload, out string detail);
    }

    /// <summary>CB 9.1 denetim kaydı — durum hash'leriyle. K1'in `AuditRecord`'u zarf verisini
    /// taşır; durum hash'lerini yalnız yürütücü bilir, bu yüzden burada sarmalanır.</summary>
    public readonly struct WorldAuditEntry
    {
        public readonly AuditRecord Base;
        public readonly RejectionReason Result;
        public readonly ulong PreStateHash, PostStateHash;
        public readonly ulong StateVersion;

        public WorldAuditEntry(AuditRecord b, RejectionReason result, ulong pre, ulong post, ulong version)
        { Base = b; Result = result; PreStateHash = pre; PostStateHash = post; StateVersion = version; }
    }

    /// <summary>Denetim + olay kalıcılığı. CB 5.2 "durum geçişi + event üretimi + audit kaydı ya
    /// birlikte kalıcı olur ya hiç olmaz" sözleşmesinin host ayağı: K6'da bu çağrı veritabanı
    /// transaction'ının İÇİNDE koşar. Fırlatırsa istisna bus'a kadar çıkar, bus rezervasyonu
    /// bırakır ve host transaction'ı geri alır — bellek içi durum da o geri almanın parçasıdır.</summary>
    public interface IWorldAuditSink
    {
        void Persist(WorldAuditEntry entry, IReadOnlyList<WorldEvent> events);
    }

    /// <summary>DÜNYA YÜRÜTÜCÜSÜ — Tek Kapı'nın yazma ucu (CLAUDE.md değişmez #1).
    /// `GameState`i değiştiren TEK meşru yol buradan geçer.
    ///
    /// ATOMİKLİK (CB 5.2): handler → journal → ÖN DENETİM → uygula. Journal'ın tek bir yazması
    /// bile geçersizse hiçbiri uygulanmaz; yani "yarım yazılmış durum" bir hata değil, yapısal
    /// olarak ulaşılamaz bir hâldir.
    ///
    /// EŞZAMANLILIK (K1 inceleme dersi): bus eşzamanlı RPC'lerden çağrılır. Durum mutasyonu ve
    /// hash hesabı tek kilit altında serileştirilir — iki komut aynı anda journal uygularsa
    /// `StateVersion` ve hash tutarsız kalırdı.</summary>
    public sealed class WorldExecutor : ICommandExecutor
    {
        readonly WorldStore depo;
        readonly IValidationContext kapi3;
        readonly IActionHandler[] handlers;      // katalog indeksine göre
        readonly IWorldAuditSink audit;
        readonly WorldJournal journal = new WorldJournal();
        IMatchCommandSink macKuyrugu;
        IOnlineSink onlineKanal;

        GameState st => depo.State;

        /// <summary>`gate3` ZORUNLUDUR: yürütme anında Kapı 3 YENİDEN denetlenir (aşağıya bak).
        /// İsteğe bağlı bırakılsaydı, unutulduğu her yerde yarış sessizce geri gelirdi — K1'in
        /// "unutulabilir varsayılan bırakma" dersinin aynısı.</summary>
        public WorldExecutor(WorldStore store, IValidationContext gate3, IWorldAuditSink auditSink = null)
        {
            depo = store ?? throw new ArgumentNullException(nameof(store));
            kapi3 = gate3 ?? throw new ArgumentNullException(nameof(gate3));
            handlers = new IActionHandler[Catalog.Count];
            audit = auditSink;
        }

        /// <summary>Maç komut kuyruğu YÜRÜTÜCÜye bağlanır, handler'a değil: yayınlama commit'in
        /// parçasıdır (aşağıda, denetimden SONRA boşaltılır). Handler kuyruğa doğrudan yazsaydı
        /// geri alma onu toplayamazdı (inceleme bulgusu, P1).</summary>
        public void MacKuyruguBagla(IMatchCommandSink sink)
        {
            if (macKuyrugu != null) throw new InvalidOperationException("maç kuyruğu zaten bağlı");
            macKuyrugu = sink;
        }

        /// <summary>Online yayın kanalı — maç kuyruğuyla aynı gerekçe: yayınlama commit'in
        /// parçasıdır, handler doğrudan yazamaz.</summary>
        public void OnlineKanalBagla(IOnlineSink sink)
        {
            if (onlineKanal != null) throw new InvalidOperationException("online kanal zaten bağlı");
            onlineKanal = sink;
        }

        /// <summary>K3-K5 aksiyonlarını buraya bağlar.</summary>
        public void RegisterHandler(string actionType, IActionHandler handler)
        {
            int i = CatalogIndex(actionType);
            if (i < 0) throw new ArgumentException("katalogda yok: " + actionType, nameof(actionType));
            // ÇİFTE KAYIT REDDEDİLİR — iki modülün aynı aksiyonu yürütmesi bir kablolama
            // hatasıdır ve sessizce sonuncunun kazanması hatayı gizler.
            if (handlers[i] != null)
                throw new InvalidOperationException("aksiyona zaten yürütücü bağlı: " + actionType);
            handlers[i] = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        /// <summary>Handler'ı OLMAYAN aksiyonlar. Host bunu AÇILIŞTA okur ve kablolama boşluğunu
        /// istek anında değil kurulum anında görür (K1'in "sahte başarı" dersinin devamı).</summary>
        public string[] UnboundActions()
        {
            var all = Catalog.Actions;
            var eksik = new List<string>();
            for (int i = 0; i < all.Count; i++) if (handlers[i] == null) eksik.Add(all[i].ActionType);
            return eksik.ToArray();
        }

        public ulong StateHash() => depo.Hash();
        public ulong StateVersion => depo.Version;

        public RejectionReason Execute(CommandEnvelope env, ActionDef action, IPayloadView payload,
                                       AuditRecord auditRecord, out string detail)
        {
            detail = null;
            if (action == null) return RejectionReason.UnknownAction;

            lock (depo.Kilit)
            {
                // KAPI 3 YENİDEN — bus doğrulaması kilidin DIŞINDA koştu; arada başka bir komut
                // bakiyeyi harcamış, slotu doldurmuş ya da değişiklik hakkını bitirmiş olabilir
                // (inceleme bulgusu, HIGH: doğrula-sonra-yürüt penceresi = TOCTOU). Otoriter
                // karar kilidin İÇİNDE verilir; dışarıdaki doğrulama hızlı geri bildirim içindir.
                // Bu, projenin "istemci ön-doğrular, sunucu yeniden doğrular" ilkesinin bir
                // katman aşağıya uygulanmasıdır.
                var tekrar = kapi3.CheckOwnershipAndState(env, action, payload, out string tekrarDetay);
                if (tekrar != RejectionReason.None)
                { detail = "yürütme anında: " + (tekrarDetay ?? tekrar.ToString()); return tekrar; }

                int ci = CatalogIndex(action.ActionType);
                var h = ci >= 0 ? handlers[ci] : null;
                if (h == null)
                {
                    // SESSİZ BAŞARI YOK: doğrulamayı geçmiş ama yürütücüsü olmayan aksiyon
                    // "oldu" diye raporlanamaz — idempotency deposu o sahte başarıyı tekrar
                    // oynatırdı (K1 P1 bulgusunun aynısı). Kullanıcı açısından aksiyon bu
                    // sürümde mevcut değildir.
                    detail = "yürütücü bağlı değil: " + action.ActionType;
                    return RejectionReason.UnknownAction;
                }

                journal.Clear();
                var r = h.Apply(st, journal, env, action, payload, out detail);
                if (r != RejectionReason.None) return r;              // hiçbir yazma uygulanmadı

                if (!journal.Validate(st, out string hata))
                {
                    // Handler geçersiz yazma üretti: bu bir KOD hatasıdır ama durumu bozmasına
                    // izin verilmez. Red olarak döner ve detayı denetim loguna girer.
                    detail = hata;
                    return RejectionReason.StateConflict;
                }

                ulong pre = WorldHash.Compute(st);
                journal.Apply(st);
                ulong post = WorldHash.Compute(st);

                // Denetim + olaylar YÜRÜTME TRANSACTION'ININ İÇİNDE (CB 5.2). Sink fırlatırsa
                // BELLEKTEKİ durum da geri alınır: önceki sürüm bunu host'un veritabanı
                // rollback'ine havale ediyordu, ama bellek o rollback'in parçası değildi —
                // "hep ya da hiç" bir varsayıma dayanıyordu (inceleme bulgusu). Artık mekanizma
                // burada: geri al, sonra istisnayı yukarı bırak.
                if (audit != null)
                {
                    try
                    {
                        audit.Persist(new WorldAuditEntry(auditRecord, RejectionReason.None, pre, post, st.StateVersion),
                                      journal.Events);
                    }
                    catch { journal.Geri(st); throw; }
                }

                // MAÇ KOMUTLARI EN SONDA YAYINLANIR — journal doğrulaması, uygulama ve denetim
                // hepsi geçtikten sonra. Buraya kadar gelen her yol "işlem tamamlandı" demektir;
                // yukarıdaki her erken dönüş ve `Geri` yolu komutları YAYINLANMAMIŞ bırakır.
                if (journal.MacKomutlari.Count > 0)
                {
                    if (macKuyrugu == null)
                    {
                        // Buraya düşmek kablolama hatasıdır: handler maç komutu üretti ama kuyruk yok.
                        // Sessiz başarı YOK — durum zaten uygulandı, o yüzden geri al ve reddet.
                        journal.Geri(st);
                        detail = "maç kuyruğu bağlı değil";
                        return RejectionReason.StateConflict;
                    }
                    try
                    {
                        for (int i = 0; i < journal.MacKomutlari.Count; i++) macKuyrugu.Enqueue(journal.MacKomutlari[i]);
                    }
                    catch { journal.Geri(st); throw; }   // aynı gerekçe: yayınlama commit'in parçası
                }

                if (journal.OnlineYayinlar.Count > 0)
                {
                    if (onlineKanal == null)
                    {
                        journal.Geri(st);
                        detail = "online kanal bağlı değil";
                        return RejectionReason.StateConflict;
                    }
                    // YAYIN PATLARSA DURUM GERİ ALINIR. Önce korumasızdı: `KlipPaylas` ağ
                    // zaman aşımıyla fırlarsa istisna `Apply` ve `Persist`ten SONRA kaçıyor,
                    // `Geri` çağrılmıyor ve bus rezervasyonu serbest bırakıyordu — durum ilerlemiş
                    // kalıyor, tekrar denemede aynı klip yeniden yayınlanabiliyordu (inceleme
                    // bulgusu, P1). Yerel taraf artık tutarlı; UZAK tarafın ikinci kopyayı elemesi
                    // `commandId` dedup'ıyla köprünün işi (DECISIONS: outbox borcu).
                    try
                    {
                        for (int i = 0; i < journal.OnlineYayinlar.Count; i++)
                        {
                            var y = journal.OnlineYayinlar[i];
                            if (y.Klip) onlineKanal.KlipPaylas(y.CommandId, y.MacId, y.PencereSn, y.Kod, y.UserId);
                            else onlineKanal.OyuncuRaporla(y.CommandId, y.HedefUserId, y.Kod, y.Notlar, y.UserId);
                        }
                    }
                    catch { journal.Geri(st); throw; }
                }
                return RejectionReason.None;
            }
        }

        static int CatalogIndex(string actionType)
        {
            var all = Catalog.Actions;
            for (int i = 0; i < all.Count; i++)
                if (string.Equals(all[i].ActionType, actionType, StringComparison.Ordinal)) return i;
            return -1;
        }
    }
}
