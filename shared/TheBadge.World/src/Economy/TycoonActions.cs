using System;
using TheBadge.CommandBus;
using TheBadge.Sim.Commands;

namespace TheBadge.World
{
    /// <summary>CB 4.1 TYCOON AKSİYONLARI — 9 aksiyonun Kapı 3 kuralları ve yürütücüleri.
    ///
    /// K2 yalnız DURUMA dayalı yapısal denetimleri yapmıştı ve "bilmediği bedeli tahmin etmez"
    /// demişti. K3 o boşluğu doldurur: hesaplanan maliyetler (inşaat, sponsor) artık
    /// `economy.balance.json`tan gelir ve Kapı 3'e bağlanır.
    ///
    /// TEK KAPI: handler'lar durumu doğrudan değiştirmez, `WorldJournal`a yazar.</summary>
    public static class TycoonActions
    {
        public static readonly string[] TribunEnum = { "kuzey", "guney", "dogu", "bati", "vip" };
        public static readonly string[] BufeEnum = { "yiyecek", "icecek", "atistirmalik" };
        public static readonly string[] MagazaEnum = { "forma", "atki", "hatira" };

        /// <summary>Katalog enum değerini dizi indeksine çevirir; bulunamazsa -1. Katalog kapı 1'de
        /// enum'u zaten doğruladı — bu yalnız eşlemedir, ikinci bir doğrulama değil.</summary>
        public static int EnumIndex(string[] tablo, string deger)
        {
            for (int i = 0; i < tablo.Length; i++)
                if (string.Equals(tablo[i], deger, StringComparison.Ordinal)) return i;
            return -1;
        }

        /// <summary>Tüm tycoon aksiyonlarını bağlar. Host kurulumda BİR KEZ çağırır; sonrasında
        /// `WorldExecutor.UnboundActions()` bu 9 aksiyonu artık listelemez.</summary>
        public static void Baglan(WorldContext ctx, WorldExecutor exec, EconomyBalance eco)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            if (exec == null) throw new ArgumentNullException(nameof(exec));
            if (eco == null) throw new ArgumentNullException(nameof(eco));

            ctx.RegisterRule("tycoon.start_construction", new InsaatKurali(eco));
            ctx.RegisterRule("tycoon.take_loan", new KrediKurali());
            ctx.RegisterRule("tycoon.sign_sponsor", new SponsorKurali());
            ctx.RegisterRule("tycoon.repay_loan", new KrediOdemeKurali());

            exec.RegisterHandler("tycoon.set_ticket_price", new BiletFiyatHandler());
            exec.RegisterHandler("tycoon.set_season_ticket_price", new KombineFiyatHandler());
            exec.RegisterHandler("tycoon.set_concession_price", new BufeFiyatHandler());
            exec.RegisterHandler("tycoon.set_merch_price", new MagazaFiyatHandler());
            exec.RegisterHandler("tycoon.start_construction", new InsaatBaslatHandler(eco));
            exec.RegisterHandler("tycoon.cancel_construction", new InsaatIptalHandler(eco));
            exec.RegisterHandler("tycoon.take_loan", new KrediAlHandler(eco));
            exec.RegisterHandler("tycoon.repay_loan", new KrediOdeHandler());
            exec.RegisterHandler("tycoon.sign_sponsor", new SponsorImzalaHandler());
        }

        // ---------------------------------------------------------------- KAPI 3 KURALLARI

        /// <summary>İnşaat: hedef tier MEVCUT+1 olmalı (CB 4.1 tablosu) ve maliyet karşılanmalı.
        /// Slot/çakışma denetimleri K2'de yapısal olarak yapılıyor; bu kural ekonomiyi ekler.</summary>
        sealed class InsaatKurali : IActionRule
        {
            readonly EconomyBalance eco;
            public InsaatKurali(EconomyBalance e) { eco = e; }
            public RejectionReason Check(GameState st, CommandEnvelope env, ActionDef action, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("tesisId", out long tesis) || !p.TryGetInt("hedefTier", out long hedef))
                    return RejectionReason.SchemaViolation;
                if (tesis < 0 || tesis >= st.Club.TesisTier.Length) { detail = "tesisId kapsam dışı"; return RejectionReason.StateConflict; }
                int mevcut = st.Club.TesisTier[(int)tesis];
                if (hedef != mevcut + 1) { detail = $"hedefTier {hedef} ≠ mevcut+1 ({mevcut + 1})"; return RejectionReason.StateConflict; }
                if (hedef >= eco.insaat.tierSureHafta.Length) { detail = "tier tanımlı değil"; return RejectionReason.StateConflict; }
                long maliyet = eco.TierMaliyet((int)hedef);
                if (!st.CanAfford(maliyet)) { detail = $"maliyet {maliyet} ₺"; return RejectionReason.InsufficientFunds; }
                return RejectionReason.None;
            }
        }

        /// <summary>Kredi: boş slot olmalı (aksi hâlde yeni kredi kaydedilemez).</summary>
        sealed class KrediKurali : IActionRule
        {
            public RejectionReason Check(GameState st, CommandEnvelope env, ActionDef action, IPayloadView p, out string detail)
            {
                detail = null;
                if (st.FreeLoanSlot() < 0) { detail = "boş kredi slotu yok"; return RejectionReason.StateConflict; }
                return RejectionReason.None;
            }
        }

        /// <summary>Kredi ödemesi: tutar kalan anaparayı AŞAMAZ. (Bakiye denetimi K2'de.)</summary>
        sealed class KrediOdemeKurali : IActionRule
        {
            public RejectionReason Check(GameState st, CommandEnvelope env, ActionDef action, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("krediId", out long kid) || !p.TryGetNumber("miktar", out double m))
                    return RejectionReason.SchemaViolation;
                int i = st.IndexOfLoan((int)kid);
                if (i < 0) { detail = "kredi yok"; return RejectionReason.StateConflict; }
                long tutar = WorldMoney.ToTl(m);
                if (tutar > st.Club.Krediler[i].AnaparaTl)
                { detail = $"ödeme {tutar} > kalan anapara {st.Club.Krediler[i].AnaparaTl}"; return RejectionReason.StateConflict; }
                return RejectionReason.None;
            }
        }

        /// <summary>Sponsor: teklif var olmalı ve süresi geçmemiş olmalı.</summary>
        sealed class SponsorKurali : IActionRule
        {
            public RejectionReason Check(GameState st, CommandEnvelope env, ActionDef action, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("teklifId", out long tid)) return RejectionReason.SchemaViolation;
                int i = st.IndexOfSponsorOffer((int)tid);
                if (i < 0) { detail = "teklif yok"; return RejectionReason.StateConflict; }
                var o = st.Club.SponsorTeklifleri[i];
                // Geçerlilik (SEZON, HAFTA) çiftiyle karşılaştırılır. Yalnız haftaya bakmak,
                // sezon dönüşünde takvim 1'e sarınca süresi geçmiş teklifi YENİDEN geçerli
                // kılıyordu (inceleme bulgusu): S1H10'da biten teklif S2H1'de kabul ediliyordu.
                if (o.SonGecerlilikHafta != 0 || o.SonGecerlilikSezon != 0)
                {
                    long sonAnahtar = (long)o.SonGecerlilikSezon * 100000 + o.SonGecerlilikHafta;
                    long simdi = (long)st.Takvim.Sezon * 100000 + st.Takvim.Hafta;
                    if (simdi > sonAnahtar)
                    { detail = $"teklif süresi doldu (S{o.SonGecerlilikSezon}H{o.SonGecerlilikHafta})"; return RejectionReason.WindowClosed; }
                }
                return RejectionReason.None;
            }
        }

        // ---------------------------------------------------------------- YÜRÜTÜCÜLER

        sealed class BiletFiyatHandler : IActionHandler
        {
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetText("tribun", out string t) || !p.TryGetNumber("fiyat", out double f))
                    return RejectionReason.SchemaViolation;
                int idx = EnumIndex(TribunEnum, t);
                if (idx < 0) { detail = "tribün: " + t; return RejectionReason.SchemaViolation; }
                j.Set(MutTarget.Fiyat, idx, PriceField.Bilet, WorldMoney.ToKurus(f));
                j.Emit(new WorldEvent(WorldEventType.FiyatGuncellendi, idx, WorldMoney.ToKurus(f), st.Takvim.Sezon, st.Takvim.Hafta));
                return RejectionReason.None;
            }
        }

        sealed class KombineFiyatHandler : IActionHandler
        {
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetNumber("fiyat", out double f)) return RejectionReason.SchemaViolation;
                j.Set(MutTarget.Fiyat, 0, PriceField.Kombine, WorldMoney.ToKurus(f));
                j.Emit(new WorldEvent(WorldEventType.FiyatGuncellendi, 100, WorldMoney.ToKurus(f), st.Takvim.Sezon, st.Takvim.Hafta));
                return RejectionReason.None;
            }
        }

        sealed class BufeFiyatHandler : IActionHandler
        {
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetText("urun", out string u) || !p.TryGetNumber("fiyat", out double f))
                    return RejectionReason.SchemaViolation;
                int idx = EnumIndex(BufeEnum, u);
                if (idx < 0) { detail = "ürün: " + u; return RejectionReason.SchemaViolation; }
                j.Set(MutTarget.Fiyat, idx, PriceField.Bufe, WorldMoney.ToKurus(f));
                j.Emit(new WorldEvent(WorldEventType.FiyatGuncellendi, 200 + idx, WorldMoney.ToKurus(f), st.Takvim.Sezon, st.Takvim.Hafta));
                return RejectionReason.None;
            }
        }

        sealed class MagazaFiyatHandler : IActionHandler
        {
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetText("urun", out string u) || !p.TryGetNumber("fiyat", out double f))
                    return RejectionReason.SchemaViolation;
                int idx = EnumIndex(MagazaEnum, u);
                if (idx < 0) { detail = "ürün: " + u; return RejectionReason.SchemaViolation; }
                j.Set(MutTarget.Fiyat, idx, PriceField.Magaza, WorldMoney.ToKurus(f));
                j.Emit(new WorldEvent(WorldEventType.FiyatGuncellendi, 300 + idx, WorldMoney.ToKurus(f), st.Takvim.Sezon, st.Takvim.Hafta));
                return RejectionReason.None;
            }
        }

        /// <summary>İnşaat başlat — maliyet PEŞİN tahsil edilir, slot işgal edilir, süre balance'tan.</summary>
        sealed class InsaatBaslatHandler : IActionHandler
        {
            readonly EconomyBalance eco;
            public InsaatBaslatHandler(EconomyBalance e) { eco = e; }
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("tesisId", out long tesis) || !p.TryGetInt("hedefTier", out long hedef))
                    return RejectionReason.SchemaViolation;
                int slot = st.FreeConstructionSlot();
                if (slot < 0) { detail = "boş slot yok"; return RejectionReason.StateConflict; }
                long maliyet = eco.TierMaliyet((int)hedef);
                int sure = eco.insaat.tierSureHafta[(int)hedef];
                int id = st.NextConstructionId();
                j.Set(MutTarget.Insaat, slot, ConstructionField.InsaatId, id);
                j.Set(MutTarget.Insaat, slot, ConstructionField.TesisId, tesis);
                j.Set(MutTarget.Insaat, slot, ConstructionField.HedefTier, hedef);
                j.Set(MutTarget.Insaat, slot, ConstructionField.KalanHafta, sure);
                j.Set(MutTarget.Insaat, slot, ConstructionField.ToplamMaliyet, maliyet);
                j.KasaDelta(-maliyet);
                j.Add(MutTarget.Kulup, 0, ClubField.DonemInsaatGideri, maliyet);   // sink raporuna girsin
                j.Emit(new WorldEvent(WorldEventType.InsaatBasladi, (int)tesis, maliyet, st.Takvim.Sezon, st.Takvim.Hafta));
                return RejectionReason.None;
            }
        }

        /// <summary>İnşaat iptal — kısmi iade (balance'taki oran). Slot boşalır, tier DEĞİŞMEZ.</summary>
        sealed class InsaatIptalHandler : IActionHandler
        {
            readonly EconomyBalance eco;
            public InsaatIptalHandler(EconomyBalance e) { eco = e; }
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("insaatId", out long iid)) return RejectionReason.SchemaViolation;
                int slot = st.IndexOfConstruction((int)iid);
                if (slot < 0) { detail = "inşaat yok"; return RejectionReason.StateConflict; }
                var c = st.Club.InsaatSlot[slot];
                long iade = (long)Math.Round(c.ToplamMaliyetTl * eco.insaat.iptalIadeOrani, MidpointRounding.AwayFromZero);
                j.Set(MutTarget.Insaat, slot, ConstructionField.InsaatId, 0);
                j.Set(MutTarget.Insaat, slot, ConstructionField.TesisId, 0);
                j.Set(MutTarget.Insaat, slot, ConstructionField.HedefTier, 0);
                j.Set(MutTarget.Insaat, slot, ConstructionField.KalanHafta, 0);
                j.Set(MutTarget.Insaat, slot, ConstructionField.ToplamMaliyet, 0);
                j.KasaDelta(iade);
                j.Add(MutTarget.Kulup, 0, ClubField.DonemInsaatGideri, -iade);     // iade sink'i geri çeker
                j.Emit(new WorldEvent(WorldEventType.InsaatIptal, c.TesisId, iade, st.Takvim.Sezon, st.Takvim.Hafta));
                return RejectionReason.None;
            }
        }

        /// <summary>Kredi al — anapara kasaya girer, borç kaydedilir. Faiz balance'tan.</summary>
        sealed class KrediAlHandler : IActionHandler
        {
            readonly EconomyBalance eco;
            public KrediAlHandler(EconomyBalance e) { eco = e; }
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetNumber("miktar", out double m) || !p.TryGetInt("vadeAy", out long vade))
                    return RejectionReason.SchemaViolation;
                int slot = st.FreeLoanSlot();
                if (slot < 0) { detail = "boş kredi slotu yok"; return RejectionReason.StateConflict; }
                long tutar = WorldMoney.ToTl(m);
                int id = st.NextLoanId();
                j.Set(MutTarget.Kredi, slot, LoanField.KrediId, id);
                j.Set(MutTarget.Kredi, slot, LoanField.Anapara, tutar);
                j.Set(MutTarget.Kredi, slot, LoanField.KalanAy, vade);
                j.Set(MutTarget.Kredi, slot, LoanField.FaizBp, eco.kredi.yillikFaizBp);
                j.KasaDelta(tutar);
                j.Emit(new WorldEvent(WorldEventType.KrediAlindi, id, tutar, st.Takvim.Sezon, st.Takvim.Hafta));
                return RejectionReason.None;
            }
        }

        /// <summary>Kredi öde — kasa azalır, anapara düşer; sıfırlanırsa slot boşalır.</summary>
        sealed class KrediOdeHandler : IActionHandler
        {
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("krediId", out long kid) || !p.TryGetNumber("miktar", out double m))
                    return RejectionReason.SchemaViolation;
                int slot = st.IndexOfLoan((int)kid);
                if (slot < 0) { detail = "kredi yok"; return RejectionReason.StateConflict; }
                long tutar = WorldMoney.ToTl(m);
                long kalan = st.Club.Krediler[slot].AnaparaTl - tutar;
                j.Set(MutTarget.Kredi, slot, LoanField.Anapara, kalan);
                if (kalan <= 0)
                {
                    j.Set(MutTarget.Kredi, slot, LoanField.KrediId, 0);
                    j.Set(MutTarget.Kredi, slot, LoanField.KalanAy, 0);
                    j.Set(MutTarget.Kredi, slot, LoanField.FaizBp, 0);
                }
                j.KasaDelta(-tutar);
                j.Emit(new WorldEvent(WorldEventType.KrediOdendi, (int)kid, tutar, st.Takvim.Sezon, st.Takvim.Hafta));
                return RejectionReason.None;
            }
        }

        /// <summary>Sponsor imzala — haftalık gelir aktifleşir, teklif slotu tüketilir.</summary>
        sealed class SponsorImzalaHandler : IActionHandler
        {
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("teklifId", out long tid)) return RejectionReason.SchemaViolation;
                int slot = st.IndexOfSponsorOffer((int)tid);
                if (slot < 0) { detail = "teklif yok"; return RejectionReason.StateConflict; }
                var o = st.Club.SponsorTeklifleri[slot];
                j.Set(MutTarget.Kulup, 0, ClubField.SponsorHaftalik, o.HaftalikTl);
                // SÜRE KORUNUR: teklifin `SureHafta`sı aktif sözleşmeye taşınır, yoksa sözleşme
                // süresiz olurdu (inceleme bulgusu). 0 süreli teklif imzalanmış sayılmaz.
                j.Set(MutTarget.Kulup, 0, ClubField.SponsorKalanHafta, o.SureHafta);
                j.Set(MutTarget.Sponsor, slot, SponsorField.TeklifId, 0);
                j.Set(MutTarget.Sponsor, slot, SponsorField.Haftalik, 0);
                j.Set(MutTarget.Sponsor, slot, SponsorField.Sure, 0);
                j.Set(MutTarget.Sponsor, slot, SponsorField.SonGecerlilik, 0);
                j.Set(MutTarget.Sponsor, slot, SponsorField.SonGecerlilikSezon, 0);
                // Sponsor imzası bir FİYAT olayı değildir: tip 8'i fiyat bildirimine yönlendiren
                // tüketiciler bunu yanlış raporlardı (inceleme bulgusu).
                j.Emit(new WorldEvent(WorldEventType.SponsorImzalandi, (int)tid, o.HaftalikTl, st.Takvim.Sezon, st.Takvim.Hafta));
                return RejectionReason.None;
            }
        }
    }
}
