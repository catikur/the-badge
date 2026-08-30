using System;
using TheBadge.CommandBus;
using TheBadge.Sim.Commands;

namespace TheBadge.World
{
    /// <summary>Kulüp DIŞI online etkileri taşıyan yayın kanalı — klip paylaşımı ve oyuncu
    /// raporu `GameState`i değiştirmez, sunucuya/moderasyona gider. `IMatchCommandSink`'in
    /// kardeşi: dünya paketi Nakama'ya doğrudan bağımlı OLMAZ, host bağlar.
    ///
    /// TEK KAPI: bu kanala yazma, journal üzerinden BEKLETİLİR ve yürütücü commit'ten sonra
    /// boşaltır (K4 inceleme dersi — doğrudan yazan handler geri alınamıyordu).</summary>
    public interface IOnlineSink
    {
        /// <summary>`commandId` UZAK TARAFIN dedup anahtarıdır. Yerel geri alma, uzak tarafın
        /// zaten ALDIĞI bir yayını geri çağıramaz (ağ tek yönlüdür); tekrar denemede ikinci
        /// kopyayı engelleyecek olan köprünün bu anahtarla yaptığı dedup'tır (inceleme
        /// bulgusu, P1). Anahtarsız arayüz bu güvenceyi YAPISAL OLARAK imkânsız kılardı.</summary>
        void KlipPaylas(Guid commandId, int macId, int pencereSn, byte hedef, long userId);
        void OyuncuRaporla(Guid commandId, long hedefUserId, byte sebep, string notlar, long raporlayanUserId);
    }

    /// <summary>CB 4.4 ONLINE AKSİYONLARI — 5 aksiyon (`league.create`, `join`, `set_rules`,
    /// `replay.share_clip`, `social.report_player`).
    ///
    /// KAPSAM NOTU: bu dilim Nakama'ya BAĞLANMAZ; deterministik katmanı ve host'un bağlayacağı
    /// arayüzü verir (karar: 2026-08-30). Gerçek RPC köprüsü ayrı dilim.</summary>
    public static class OnlineActions
    {
        public static readonly string[] PaylasimHedefi = { "lig", "arkadas", "genel" };
        public static readonly string[] RaporSebebi = { "hakaret", "hile", "spam", "diger" };

        public static void Baglan(WorldContext ctx, WorldExecutor exec, WorldRules kural, IOnlineSink kanal)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            if (exec == null) throw new ArgumentNullException(nameof(exec));
            if (kural == null) throw new ArgumentNullException(nameof(kural));
            // `kanal` null OLABİLİR: lig aksiyonları yereldir, yalnız klip/rapor kanal ister.

            exec.OnlineKanalBagla(kanal);

            ctx.RegisterRule("league.create", new LigYokKurali());
            ctx.RegisterRule("league.join", new KatilimKurali());
            ctx.RegisterRule("league.set_rules", new KurucuKurali());

            exec.RegisterHandler("league.create", new LigKurHandler());
            exec.RegisterHandler("league.join", new LigKatilHandler());
            exec.RegisterHandler("league.set_rules", new LigKuralHandler());
            exec.RegisterHandler("replay.share_clip", new KlipHandler());
            exec.RegisterHandler("social.report_player", new RaporHandler());
        }

        // ---------------------------------------------------------------- KAPI 3 KURALLARI

        /// <summary>Zaten bir ligdeyken yeni lig KURULAMAZ — iki ligde birden olmak, kulüp
        /// durumunun hangi kurallarla (chaos/hız) oynadığını belirsiz bırakırdı.</summary>
        sealed class LigYokKurali : IActionRule
        {
            public RejectionReason Check(GameState st, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (st.Lig.LigId != 0) { detail = "zaten bir ligdesin"; return RejectionReason.StateConflict; }
                return RejectionReason.None;
            }
        }

        sealed class KatilimKurali : IActionRule
        {
            public RejectionReason Check(GameState st, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("ligId", out long lid)) return RejectionReason.SchemaViolation;
                if (st.Lig.LigId != 0)
                {
                    // Aynı lige tekrar katılmak da hata: sessizce "zaten üyesin" demek yerine
                    // net cevap verilir (CB 8.2).
                    detail = st.Lig.LigId == (int)lid ? "bu ligin zaten üyesisin" : "önce mevcut ligden çıkmalısın";
                    return RejectionReason.StateConflict;
                }
                return RejectionReason.None;
            }
        }

        /// <summary>Kural değiştirmek YALNIZ kurucunun hakkı — GDD 6.2. Kurucu olmayan üye
        /// `NotOwned` alır: bu bir sahiplik reddidir, durum çelişkisi değil.</summary>
        sealed class KurucuKurali : IActionRule
        {
            public RejectionReason Check(GameState st, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("ligId", out long lid)) return RejectionReason.SchemaViolation;
                if (st.Lig.LigId == 0 || st.Lig.LigId != (int)lid)
                { detail = "bu ligin üyesi değilsin"; return RejectionReason.StateConflict; }
                if (st.Lig.KurucuUserId != env.UserId)
                { detail = "lig kurallarını yalnız kurucu değiştirir"; return RejectionReason.NotOwned; }
                return RejectionReason.None;
            }
        }

        // ---------------------------------------------------------------- YÜRÜTÜCÜLER

        sealed class LigKurHandler : IActionHandler
        {
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("chaos", out long chaos) || !p.TryGetInt("hiz", out long hiz)
                    || !p.TryGetNumber("butce", out double butce) || !p.TryGetInt("saatDilimi", out long tz))
                    return RejectionReason.SchemaViolation;
                // LİG KİMLİĞİ: `Guid` ya da zaman tabanlı kimlik YASAK (determinizm). Kayıt
                // tohumundan ve kurucudan TÜRETİLİR — aynı girdi aynı ligi verir.
                int ligId = LigKimligi(env.UserId, st.Takvim.Sezon, st.Takvim.Hafta);
                j.Set(MutTarget.Lig, 0, LeagueField.LigId, ligId);
                j.Set(MutTarget.Lig, 0, LeagueField.Kurucu, env.UserId);
                j.Set(MutTarget.Lig, 0, LeagueField.Chaos, chaos);
                j.Set(MutTarget.Lig, 0, LeagueField.Hiz, hiz);
                j.Set(MutTarget.Lig, 0, LeagueField.Butce, WorldMoney.ToTl(butce));
                j.Set(MutTarget.Lig, 0, LeagueField.SaatDilimi, tz);
                j.Emit(new WorldEvent(WorldEventType.LigKuruldu, ligId, chaos, st.Takvim.Sezon, st.Takvim.Hafta));
                return RejectionReason.None;
            }
        }

        sealed class LigKatilHandler : IActionHandler
        {
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("ligId", out long lid)) return RejectionReason.SchemaViolation;
                // ŞİFRE HİÇBİR BİÇİMDE KALICI DURUMA YAZILMAZ. Önce "ham değil, özeti" yazıyordum;
                // `DizeOzeti` TUZSUZ ve HIZLI bir xxHash64 — düşük entropili bir lig şifresi için
                // sözlük saldırısı ucuzdur, yani şifreyi saklamaktan anlamlı ölçüde iyi DEĞİLDİ
                // (inceleme bulgusu, P1). Doğrulama SUNUCUnundur; şifre yalnız komut payload'ında
                // sunucuya gider ve durumda iz bırakmaz.
                j.Set(MutTarget.Lig, 0, LeagueField.LigId, lid);
                // Kurucu DEĞİLİZ: katılan üyenin kurucu alanı 0 kalır ve `set_rules` kapalıdır.
                j.Emit(new WorldEvent(WorldEventType.LigeKatilindi, (int)lid, 0, st.Takvim.Sezon, st.Takvim.Hafta));
                return RejectionReason.None;
            }
        }

        sealed class LigKuralHandler : IActionHandler
        {
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                // chaos ve hiz İSTEĞE BAĞLI (katalog): verilmeyeni DEĞİŞTİRMEYİZ. Eksik alanı
                // varsayılana çekmek, kurucunun dokunmadığı ayarı sessizce sıfırlardı.
                bool dokunuldu = false;
                if (p.TryGetInt("chaos", out long chaos)) { j.Set(MutTarget.Lig, 0, LeagueField.Chaos, chaos); dokunuldu = true; }
                if (p.TryGetInt("hiz", out long hiz)) { j.Set(MutTarget.Lig, 0, LeagueField.Hiz, hiz); dokunuldu = true; }
                if (!dokunuldu) { detail = "değiştirilecek kural verilmedi"; return RejectionReason.SchemaViolation; }
                j.Emit(new WorldEvent(WorldEventType.LigKurallariDegisti, st.Lig.LigId, 0, st.Takvim.Sezon, st.Takvim.Hafta));
                return RejectionReason.None;
            }
        }

        /// <summary>Klip paylaşımı — kalıcı durumu DEĞİŞTİRMEZ, yayın kanalına gider.
        /// Kuyruk bağlı değilse sessiz başarı YOK (K4 dersi).</summary>
        sealed class KlipHandler : IActionHandler
        {
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("macId", out long macId) || !p.TryGetInt("pencereSn", out long pencere)
                    || !p.TryGetText("hedef", out string hedef))
                    return RejectionReason.SchemaViolation;
                int hi = TransferActions.EnumIndex(PaylasimHedefi, hedef);
                if (hi < 0) { detail = "hedef: " + hedef; return RejectionReason.SchemaViolation; }
                j.OnlineKlip(env.CommandId, (int)macId, (int)pencere, (byte)hi, env.UserId);
                return RejectionReason.None;
            }
        }

        sealed class RaporHandler : IActionHandler
        {
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("hedefUserId", out long hedef) || !p.TryGetText("sebep", out string sebep))
                    return RejectionReason.SchemaViolation;
                int si = TransferActions.EnumIndex(RaporSebebi, sebep);
                if (si < 0) { detail = "sebep: " + sebep; return RejectionReason.SchemaViolation; }
                if (hedef == env.UserId) { detail = "kendini raporlayamazsın"; return RejectionReason.StateConflict; }
                p.TryGetText("notlar", out string notlar);
                j.OnlineRapor(env.CommandId, hedef, (byte)si, notlar, env.UserId);
                return RejectionReason.None;
            }
        }

        /// <summary>Lig kimliği — DETERMİNİSTİK türetim. Katalog bandı `league.ligId` 1-100000000
        /// olduğu için sonuç o aralığa kırpılır; kimliğin TEKİLLİĞİ sunucunun işidir (çakışma
        /// hâlinde sunucu reddeder ve istemci yeniden dener). Buradaki iş kimliği ÜRETMEK değil,
        /// üretimin ZAMANA ya da Guid'e bağlı OLMAMASINI garanti etmektir.</summary>
        internal static int LigKimligi(long userId, ushort sezon, ushort hafta)
        {
            ulong h = TheBadge.Sim.Determinism.Rng.Hash64(
                unchecked((ulong)userId), (uint)TheBadge.Sim.Determinism.Domain.Decision, sezon, hafta, 0x11C0u);
            return (int)(h % 100_000_000UL) + 1;
        }
    }
}
