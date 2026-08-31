using System;
using TheBadge.CommandBus;
using TheBadge.Sim.Commands;

namespace TheBadge.World
{
    /// <summary>CB 4.3 TRANSFER AKSİYONLARI — 5 aksiyon (`list_player`, `propose_offer`,
    /// `respond_offer`, `sign_free_agent`, `release_player`).
    ///
    /// SAHİPLİK YÖNÜ üç ilişkiyi ayırır (K2'nin `Yabanci` dersi): kendi oyuncun (listeleme,
    /// fesih), BAŞKA kulübün oyuncusu (teklif), serbest oyuncu (imza). Bu üçünü tek "sahip mi"
    /// sorusuna indirgemek, serbest oyuncuyu "yabancı" sayıp teklif kabul etmeye yol açardı.
    ///
    /// KADRO SINIRLARI burada GERÇEK oluyor: K2 `kadroMax`ı doğrulayıp KULLANMIYORDU (inceleme
    /// bulgusu). Kadroya oyuncu KATAN yollar (imza, teklif kabulü) `kadroMax`a, kadrodan
    /// ÇIKARAN yollar (fesih) `kadroMin`e takılır.</summary>
    public static class TransferActions
    {
        public static readonly string[] CevapEnum = { "kabul", "ret", "karsiTeklif" };

        /// <summary>`saveSeed` durumda DEĞİL host'ta yaşar (`EconomyTick` ile aynı sözleşme):
        /// kayıt tohumu oyun durumunun parçası değil, oyunun KİMLİĞİdir.</summary>
        public static void Baglan(WorldContext ctx, WorldExecutor exec, WorldRules kural,
                                  TransferBalance tb, ulong saveSeed)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            if (exec == null) throw new ArgumentNullException(nameof(exec));
            if (kural == null) throw new ArgumentNullException(nameof(kural));
            if (tb == null) throw new ArgumentNullException(nameof(tb));

            // `transfer.list_player` için AYRI kural YOK: sahiplik denetimini K2 katmanı
            // (`WorldContext` OwnerNeed.Sahip) zaten yapıyor ve KAPI 3'te bizden ÖNCE koşuyor.
            // Buraya bir kural daha koymak ölü koddu — K3-B'de öğrenilen "derin katmanın
            // maskelediği kural" tuzağının aynısı.
            ctx.RegisterRule("transfer.propose_offer", new TeklifKurali());
            ctx.RegisterRule("transfer.respond_offer", new CevapKurali());
            // `sign_free_agent` için AYRI kural YOK: kadro TAVANI K2 katmanında (WorldContext 5a),
            // serbestlik denetimi de orada (OwnerNeed.Serbest). Buraya kural koymak ölü koddu.
            ctx.RegisterRule("transfer.release_player", new FesihKurali(tb));
            ctx.RegisterRule("transfer.sign_free_agent", new SerbestMaasKurali(tb));

            exec.RegisterHandler("transfer.list_player", new ListeHandler());
            exec.RegisterHandler("transfer.propose_offer", new TeklifHandler(tb, kural));
            exec.RegisterHandler("transfer.respond_offer", new CevapHandler(tb, kural, saveSeed));
            exec.RegisterHandler("transfer.sign_free_agent", new SerbestHandler(tb));
            exec.RegisterHandler("transfer.release_player", new FesihHandler(tb));
        }

        // ---------------------------------------------------------------- KAPI 3 KURALLARI

        /// <summary>Teklif YABANCI oyuncuya verilir: kendi oyuncuna teklif anlamsız, SERBEST
        /// oyuncuya teklif yanlış kapı (`sign_free_agent` var). İkisi ayrı sebeple reddedilir.</summary>
        sealed class TeklifKurali : IActionRule
        {
            public RejectionReason Check(GameState st, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("hedefOyuncuId", out long pid)) return RejectionReason.SchemaViolation;
                // SAHİPLİK BURADA DENETLENMEZ: K2 katmanı `OwnerNeed.Yabanci` ile hem kendi
                // oyuncunu hem serbest oyuncuyu hem de var olmayanı `NotOwned` ile eliyor ve
                // bizden ÖNCE koşuyor. Aynı denetimi burada tekrarlamak ölü kod olurdu.
                // Aynı oyuncuya AÇIK teklif varken ikincisi açılmaz: iki teklif tek cevapla
                // kapanamaz ve hangisinin bağlayıcı olduğu belirsiz kalırdı.
                for (int k = 0; k < st.Club.TransferTeklifleri.Length; k++)
                {
                    var t = st.Club.TransferTeklifleri[k];
                    // SÜRESİ DOLMUŞ teklif "açık" SAYILMAZ: sayılsaydı bir kez süresi geçen
                    // teklif, o oyuncuya bir daha teklif vermeyi SONSUZA DEK engellerdi
                    // (yuva geri kazanımını da kilitliyordu — kapı bunu yakaladı).
                    if (t.TeklifId != 0 && t.OyuncuId == (int)pid && t.TeklifEdenClubId == st.Club.ClubId
                        && !SureDoldu(st, t))
                    { detail = "bu oyuncuya açık teklifin zaten var"; return RejectionReason.StateConflict; }
                }
                if (BosYuvaVeyaSuresiDolmus(st) < 0) { detail = "teklif yuvası dolu"; return RejectionReason.StateConflict; }
                return RejectionReason.None;
            }
        }

        sealed class CevapKurali : IActionRule
        {
            public RejectionReason Check(GameState st, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("teklifId", out long tid)) return RejectionReason.SchemaViolation;
                int i = TeklifIndex(st, (int)tid);
                if (i < 0) { detail = "teklif yok"; return RejectionReason.StateConflict; }
                // SIRA denetimi: topun sende olmadığı teklifi cevaplayamazsın. Bu olmadan aynı
                // taraf kendi teklifini kendi kabul edebilirdi.
                var t = st.Club.TransferTeklifleri[i];
                bool benTeklifEden = t.TeklifEdenClubId == st.Club.ClubId;
                // Sıra denetimi YALNIZ yaşayan teklifte anlamlı: süresi dolmuşu kapatmak
                // sırayı beklemez, yoksa "sıra karşıda" diye kilitli kalırdı.
                if (!SureDoldu(st, t) && benTeklifEden != t.SiraTeklifEdende)
                { detail = "sıra sende değil"; return RejectionReason.StateConflict; }
                // SÜRESİ DOLMUŞ teklife yalnız RET verilebilir: kabul/karşı teklif anlamsızdır ama
                // reddi de yasaklamak yuvayı kilitliyordu (inceleme bulgusu). Ret, yuvayı
                // kapatmanın kullanıcı elindeki yoludur.
                if (SureDoldu(st, t))
                {
                    if (!p.TryGetText("cevap", out string cvp) || !string.Equals(cvp, "ret", StringComparison.Ordinal))
                    { detail = "teklif süresi doldu (yalnız ret verilebilir)"; return RejectionReason.StateConflict; }
                }
                return RejectionReason.None;
            }
        }

        /// <summary>Serbest oyuncu MAAŞ TALEBİ — bedelsiz oyuncu bedava DEĞİLDİR. Bu kural
        /// olmadan 90 güçlü bir serbest oyuncu haftalık ₺1'e imzalanabilirdi: transfer
        /// ekonomisinin tamamını atlayan bir kaçış yolu. Talep `Valuation.MaasTalebi` ile
        /// HESAPLANIR, yani K2'nin bilemeyeceği bir büyüklüktür (K3-K5 seami).
        /// Serbestlik ve kadro TAVANI denetimleri K2'dedir; burada tekrarlanmaz.</summary>
        sealed class SerbestMaasKurali : IActionRule
        {
            readonly TransferBalance tb;
            public SerbestMaasKurali(TransferBalance t) { tb = t; }
            public RejectionReason Check(GameState st, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("oyuncuId", out long pid) || !p.TryGetNumber("maas", out double maas))
                    return RejectionReason.SchemaViolation;
                int i = st.IndexOfPlayer((int)pid);
                if (i < 0) { detail = "oyuncu yok"; return RejectionReason.NotOwned; }   // K2 zaten eler
                long talep = Valuation.MaasTalebi(st.Oyuncular[i], tb);
                if (WorldMoney.ToTl(maas) < talep)
                { detail = $"oyuncunun maaş talebi {talep} ₺/hafta"; return RejectionReason.StateConflict; }
                return RejectionReason.None;
            }
        }

        /// <summary>Fesih: YALNIZ hesaplanan fesih bedelinin karşılanabilirliği.
        /// Sahiplik (OwnerNeed.Sahip) ve kadro TABANI (WorldContext 5b) K2 katmanındadır —
        /// ikisini burada tekrarlamak ölü koddu. Geriye K2'nin BİLEMEYECEĞİ tek şey kalıyor:
        /// bedel payload'da bildirilmiyor, HESAPLANIYOR (K3-K5 seami — K2 bilmediği bedeli
        /// tahmin etmez).</summary>
        sealed class FesihKurali : IActionRule
        {
            readonly TransferBalance tb;
            public FesihKurali(TransferBalance t) { tb = t; }
            public RejectionReason Check(GameState st, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("oyuncuId", out long pid)) return RejectionReason.SchemaViolation;
                int i = st.IndexOfPlayer((int)pid);
                if (i < 0) { detail = "oyuncu yok"; return RejectionReason.NotOwned; }   // K2 zaten eler
                // Fesih bedeli KAPI 3'te denetlenir: payload tutarı bildirmiyor, bedel HESAPLANIR
                // (K3-K5 seami — hesaplanan bedel modülden gelir, K2 tahmin etmez).
                if (!st.CanAfford(Valuation.FesihBedeli(st.Oyuncular[i], tb)))
                    return RejectionReason.InsufficientFunds;
                return RejectionReason.None;
            }
        }

        // ---------------------------------------------------------------- YÜRÜTÜCÜLER

        sealed class ListeHandler : IActionHandler
        {
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("oyuncuId", out long pid) || !p.TryGetNumber("istenenBedel", out double bedel))
                    return RejectionReason.SchemaViolation;
                int i = st.IndexOfPlayer((int)pid);
                if (i < 0) return RejectionReason.NotOwned;
                long tl = WorldMoney.ToTl(bedel);
                j.OyuncuSet(i, PlayerField.Listede, tl > 0 ? 1 : 0);
                j.OyuncuSet(i, PlayerField.IstenenBedel, tl);
                j.Emit(new WorldEvent(WorldEventType.SozlesmeGuncellendi, (int)pid, tl, st.Takvim.Sezon, st.Takvim.Hafta));
                return RejectionReason.None;
            }
        }

        sealed class TeklifHandler : IActionHandler
        {
            readonly TransferBalance tb; readonly WorldRules kural;
            public TeklifHandler(TransferBalance t, WorldRules k) { tb = t; kural = k; }
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("hedefOyuncuId", out long pid) || !p.TryGetNumber("bedel", out double bedel)
                    || !p.TryGetNumber("maas", out double maas))
                    return RejectionReason.SchemaViolation;
                int yuva = BosYuvaVeyaSuresiDolmus(st);
                if (yuva < 0) { detail = "teklif yuvası dolu"; return RejectionReason.StateConflict; }
                // Süresi dolmuş yuva geri kazanılıyorsa ÖNCE temizlenir: eski kimlik yeni
                // kimliğin seçiminde "kullanımda" sayılmasın.
                if (st.Club.TransferTeklifleri[yuva].TeklifId != 0) YuvaTemizle(j, yuva);

                int tid = SonrakiTeklifId(st, kural);
                if (tid == 0) { detail = "teklif kimliği kalmadı"; return RejectionReason.StateConflict; }
                Gecerlilik(st, tb, kural, out ushort sezon, out ushort hafta);
                j.Set(MutTarget.TransferTeklif, yuva, OfferField.TeklifId, tid);
                j.Set(MutTarget.TransferTeklif, yuva, OfferField.OyuncuId, pid);
                j.Set(MutTarget.TransferTeklif, yuva, OfferField.TeklifEden, st.Club.ClubId);
                j.Set(MutTarget.TransferTeklif, yuva, OfferField.Bedel, WorldMoney.ToTl(bedel));
                j.Set(MutTarget.TransferTeklif, yuva, OfferField.Maas, WorldMoney.ToTl(maas));
                j.Set(MutTarget.TransferTeklif, yuva, OfferField.SonGecerlilikSezon, sezon);
                j.Set(MutTarget.TransferTeklif, yuva, OfferField.SonGecerlilikHafta, hafta);
                // Top KARŞI tarafta: teklifi açan cevap bekler.
                j.Set(MutTarget.TransferTeklif, yuva, OfferField.SiraTeklifEdende, 0);
                j.Set(MutTarget.TransferTeklif, yuva, OfferField.TurSayisi, 1);
                j.Emit(new WorldEvent(WorldEventType.SozlesmeGuncellendi, tid, WorldMoney.ToTl(bedel), st.Takvim.Sezon, st.Takvim.Hafta));
                return RejectionReason.None;
            }
        }

        /// <summary>Teklife cevap. `kabul` transferi GERÇEKLEŞTİRİR (kasa, kulüp, maaş, yuva
        /// temizliği); `ret` yuvayı boşaltır; `karsiTeklif` bedeli günceller ve SIRAYI çevirir.
        /// AI karşı tarafın kararı `Valuation.Karar` ile deterministik üretilir.</summary>
        sealed class CevapHandler : IActionHandler
        {
            readonly TransferBalance tb; readonly WorldRules kural; readonly ulong seed;
            public CevapHandler(TransferBalance t, WorldRules k, ulong s) { tb = t; kural = k; seed = s; }
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("teklifId", out long tid) || !p.TryGetText("cevap", out string cevap))
                    return RejectionReason.SchemaViolation;
                int idx = TransferActions.EnumIndex(CevapEnum, cevap);
                if (idx < 0) { detail = "cevap: " + cevap; return RejectionReason.SchemaViolation; }
                int i = TeklifIndex(st, (int)tid);
                if (i < 0) { detail = "teklif yok"; return RejectionReason.StateConflict; }
                var t = st.Club.TransferTeklifleri[i];
                int oi = st.IndexOfPlayer(t.OyuncuId);
                if (oi < 0) { detail = "teklifin oyuncusu evrende yok"; return RejectionReason.StateConflict; }

                // CevapEnum sırası {kabul, ret, karsiTeklif}; `PazarlikKarari` sırası
                // {Ret, Kabul, KarsiTeklif}. İKİSİ AYNI DEĞİL — indeksi doğrudan enum'a
                // cast etmek kabul ile reddi TAKAS eder. Eşleme AÇIKÇA yazılır.
                PazarlikKarari secim = idx == 0 ? PazarlikKarari.Kabul
                                     : idx == 1 ? PazarlikKarari.Ret
                                                : PazarlikKarari.KarsiTeklif;
                switch (secim)
                {
                    case PazarlikKarari.Kabul:
                        {
                            // Kadro tavanı ALICI için geçerlidir; alıcı bu komutu veren taraf
                            // olmayabilir (satıcı kabul ediyorsa alıcı karşı taraftır), ama bu
                            // dilimde tek kulüp durumu tutuluyor: kabul eden BİZ isek kadroya
                            // katılım bizde olur.
                            bool bizAliyoruz = t.TeklifEdenClubId == st.Club.ClubId;
                            // SAHİPLİK YENİDEN DENETLENİR (inceleme bulgusu, iki inceleyici):
                            // `respond_offer` KAPI 3'te `OwnerNeed.Yok`tur — yön yalnız
                            // `TeklifEdenClubId`den çıkarılıyordu. Teklif açıkken oyuncu satılmış
                            // ya da feshedilmişse, BAYAT teklif artık bizde OLMAYAN birini taşır,
                            // bedeli kasaya yazar ve maaş defterini bozardı. Teklif "kim teklif
                            // etti"yi söyler, "oyuncu ŞU AN kimde"yi söylemez.
                            long simdikiSahip = st.Oyuncular[oi].ClubId;
                            if (bizAliyoruz)
                            {
                                // Alırken: oyuncu HÂLÂ yabancı bir kulüpte olmalı.
                                if (simdikiSahip == st.Club.ClubId)
                                { detail = "oyuncu zaten bu kulüpte"; return RejectionReason.StateConflict; }
                                if (simdikiSahip == 0)
                                { detail = "oyuncu serbest kaldı — sign_free_agent kullanılmalı"; return RejectionReason.StateConflict; }
                            }
                            else if (simdikiSahip != st.Club.ClubId)
                            {
                                // Satarken: oyuncu HÂLÂ bizim olmalı.
                                detail = "oyuncu artık bu kulüpte değil";
                                return RejectionReason.NotOwned;
                            }
                            if (bizAliyoruz)
                            {
                                if (KadroSayisi(st) >= kural.yapi.kadroMax)
                                { detail = $"kadro tavanı dolu ({kural.yapi.kadroMax})"; return RejectionReason.StateConflict; }
                                if (!st.CanAfford(t.BedelTl)) return RejectionReason.InsufficientFunds;
                                j.KasaDelta(-t.BedelTl);
                                j.Add(MutTarget.Kulup, 0, ClubField.DonemTransferGideri, t.BedelTl);   // sink raporuna girsin
                                j.OyuncuSet(oi, PlayerField.ClubId, st.Club.ClubId);
                                // TAM maaş eklenir, FARK değil: oyuncu bizim değildi, eski maaşı
                                // BAŞKA kulübün gider kaleminde duruyordu. Fark eklemek, pahalı
                                // kulüpten ucuz maaşa alınan oyuncuda gider yükünü EKSİK gösterirdi.
                                j.Add(MutTarget.Kulup, 0, ClubField.HaftalikMaasGider, t.HaftalikMaasTl);
                            }
                            else
                            {
                                if (KadroSayisi(st) <= kural.yapi.kadroMin)
                                { detail = $"kadro tabanı ({kural.yapi.kadroMin}) altına inilemez"; return RejectionReason.StateConflict; }
                                j.KasaDelta(t.BedelTl);
                                // SATIŞ NEGATİF: kalem NET transfer harcamasıdır. Satışı ayrı bir
                                // SOURCE saymak, ECONOMY_MAP'in source listesinde olmayan bir
                                // gelir kalemi uydurmak olurdu; doküman yalnız "Transfer bedelleri"ni
                                // sink olarak sayıyor (inşaat iptal iadesiyle aynı mantık).
                                j.Add(MutTarget.Kulup, 0, ClubField.DonemTransferGideri, -t.BedelTl);
                                j.OyuncuSet(oi, PlayerField.ClubId, t.TeklifEdenClubId);
                                j.Add(MutTarget.Kulup, 0, ClubField.HaftalikMaasGider, -st.Oyuncular[oi].HaftalikMaasTl);
                            }
                            j.OyuncuSet(oi, PlayerField.HaftalikMaas, t.HaftalikMaasTl);
                            // Transfer olan oyuncu listeden ve fiyattan düşer.
                            j.OyuncuSet(oi, PlayerField.Listede, 0);
                            j.OyuncuSet(oi, PlayerField.IstenenBedel, 0);
                            // Bu oyuncuya ait TÜM teklifler kapanır (yalnız kabul edilen değil):
                            // kardeş teklifler canlı kalsaydı aynı oyuncu ikinci kez satılabilirdi.
                            TeklifleriTemizle(st, j, t.OyuncuId);
                            // Kadrodan çıkıyorsa kaptanlık düşer.
                            if (!bizAliyoruz) KaptanliktanDusur(st, j, t.OyuncuId);
                            j.Emit(new WorldEvent(WorldEventType.OyuncuTransferi, t.OyuncuId, t.BedelTl, st.Takvim.Sezon, st.Takvim.Hafta));
                            return RejectionReason.None;
                        }
                    case PazarlikKarari.Ret:
                        YuvaTemizle(j, i);
                        j.Emit(new WorldEvent(WorldEventType.SozlesmeGuncellendi, (int)tid, 0, st.Takvim.Sezon, st.Takvim.Hafta));
                        return RejectionReason.None;
                    default: // KarsiTeklif
                        {
                            if (t.TurSayisi >= tb.pazarlik.maxTur)
                            { detail = $"pazarlık turu tavanı ({tb.pazarlik.maxTur})"; return RejectionReason.StateConflict; }
                            long yeni;
                            if (p.TryGetNumber("karsiBedel", out double kb)) yeni = WorldMoney.ToTl(kb);
                            else
                            {
                                // Karşı bedel verilmediyse AI hesaplar — deterministik.
                                Valuation.Karar(st.Oyuncular[oi], t.BedelTl, t.TurSayisi, tb, seed, out long oneri);
                                yeni = oneri > 0 ? oneri : t.BedelTl;
                            }
                            j.Set(MutTarget.TransferTeklif, i, OfferField.Bedel, yeni);
                            j.Set(MutTarget.TransferTeklif, i, OfferField.SiraTeklifEdende, t.SiraTeklifEdende ? 0 : 1);
                            j.Set(MutTarget.TransferTeklif, i, OfferField.TurSayisi, t.TurSayisi + 1);
                            j.Emit(new WorldEvent(WorldEventType.SozlesmeGuncellendi, (int)tid, yeni, st.Takvim.Sezon, st.Takvim.Hafta));
                            return RejectionReason.None;
                        }
                }
            }
        }

        sealed class SerbestHandler : IActionHandler
        {
            readonly TransferBalance tb;
            public SerbestHandler(TransferBalance t) { tb = t; }
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("oyuncuId", out long pid) || !p.TryGetNumber("maas", out double maas)
                    || !p.TryGetInt("sureYil", out long yil))
                    return RejectionReason.SchemaViolation;
                int i = st.IndexOfPlayer((int)pid);
                if (i < 0) return RejectionReason.StateConflict;
                long m = WorldMoney.ToTl(maas);
                j.OyuncuSet(i, PlayerField.ClubId, st.Club.ClubId);
                j.OyuncuSet(i, PlayerField.HaftalikMaas, m);
                // Yıl→hafta çevrimi [KALİBRE]: `sozlesmeTamYilHafta` değerlemede de kullanılan
                // AYNI sabittir. Burada 52 yazmak, balance'ı değiştirdiğimizde sözleşme süresiyle
                // değerlemenin sessizce AYRIŞMASI demekti.
                j.OyuncuSet(i, PlayerField.SozlesmeKalanHafta, yil * tb.degerleme.sozlesmeTamYilHafta);
                j.Add(MutTarget.Kulup, 0, ClubField.HaftalikMaasGider, m);
                j.Emit(new WorldEvent(WorldEventType.OyuncuTransferi, (int)pid, 0, st.Takvim.Sezon, st.Takvim.Hafta));
                return RejectionReason.None;
            }
        }

        sealed class FesihHandler : IActionHandler
        {
            readonly TransferBalance tb;
            public FesihHandler(TransferBalance t) { tb = t; }
            public RejectionReason Apply(GameState st, WorldJournal j, CommandEnvelope env, ActionDef a, IPayloadView p, out string detail)
            {
                detail = null;
                if (!p.TryGetInt("oyuncuId", out long pid)) return RejectionReason.SchemaViolation;
                int i = st.IndexOfPlayer((int)pid);
                if (i < 0) return RejectionReason.NotOwned;
                var oyuncu = st.Oyuncular[i];
                long bedel = Valuation.FesihBedeli(oyuncu, tb);
                if (!st.CanAfford(bedel)) return RejectionReason.InsufficientFunds;
                j.KasaDelta(-bedel);
                j.Add(MutTarget.Kulup, 0, ClubField.DonemTransferGideri, bedel);   // fesih bedeli de transfer sink'i
                j.OyuncuSet(i, PlayerField.ClubId, 0);
                j.OyuncuSet(i, PlayerField.SozlesmeKalanHafta, 0);
                j.OyuncuSet(i, PlayerField.Listede, 0);
                j.OyuncuSet(i, PlayerField.IstenenBedel, 0);
                j.Add(MutTarget.Kulup, 0, ClubField.HaftalikMaasGider, -oyuncu.HaftalikMaasTl);
                // Feshedilen oyuncunun açık teklifleri ve kaptanlığı da düşer.
                TeklifleriTemizle(st, j, (int)pid);
                KaptanliktanDusur(st, j, (int)pid);
                j.Emit(new WorldEvent(WorldEventType.OyuncuTransferi, (int)pid, -bedel, st.Takvim.Sezon, st.Takvim.Hafta));
                return RejectionReason.None;
            }
        }

        // ---------------------------------------------------------------- YARDIMCILAR

        public static int EnumIndex(string[] tablo, string deger)
        {
            for (int i = 0; i < tablo.Length; i++)
                if (string.Equals(tablo[i], deger, StringComparison.Ordinal)) return i;
            return -1;
        }

        /// <summary>Kadro mevcudu — kapılar da okur (kadro sınırı iddiası dışarıdan ölçülür).</summary>
        public static int KadroSayisiPublic(GameState st) => KadroSayisi(st);

        internal static int KadroSayisi(GameState st)
        {
            int n = 0;
            for (int i = 0; i < st.Oyuncular.Length; i++) if (st.Oyuncular[i].ClubId == st.Club.ClubId) n++;
            return n;
        }

        internal static int BosYuva(GameState st)
        {
            for (int i = 0; i < st.Club.TransferTeklifleri.Length; i++)
                if (st.Club.TransferTeklifleri[i].TeklifId == 0) return i;
            return -1;
        }

        internal static int TeklifIndex(GameState st, int teklifId)
        {
            if (teklifId <= 0) return -1;
            for (int i = 0; i < st.Club.TransferTeklifleri.Length; i++)
                if (st.Club.TransferTeklifleri[i].TeklifId == teklifId) return i;
            return -1;
        }

        /// <summary>Sıradaki teklif kimliği — EN KÜÇÜK kullanılmayan kimlik, `1..transferTeklifIdMax`.
        /// `Guid` ya da zaman tabanlı kimlik YASAK (determinizm).
        ///
        /// Önce "açık yuvaların en büyüğü + 1" yazmıştım ve "yuvalar boşalınca sıfırlanır" diye
        /// düşünmüştüm — inceleme bulgusu: teklifler ÇAKIŞIRSA max hiç sıfırlanmaz ve kimlik
        /// sınırsız büyür. Katalog bandı `transfer.teklifId` 1-4096 olduğu için 4096'yı aşan
        /// teklif OLUŞTURULABİLİR ama asla CEVAPLANAMAZDI. Artık tavan [KALİBRE] ve boşta kalan
        /// en küçük kimlik seçiliyor — yuva sayısı kadar kimlik her zaman bulunur.
        /// 0 dönerse çağıran taraf reddeder (yuva doluyken zaten oraya gelinmez).</summary>
        internal static int SonrakiTeklifId(GameState st, WorldRules kural)
        {
            for (int aday = 1; aday <= kural.yapi.transferTeklifIdMax; aday++)
            {
                bool kullanimda = false;
                for (int i = 0; i < st.Club.TransferTeklifleri.Length; i++)
                    if (st.Club.TransferTeklifleri[i].TeklifId == aday) { kullanimda = true; break; }
                if (!kullanimda) return aday;
            }
            return 0;
        }

        /// <summary>Oyuncuya ait TÜM açık teklifleri temizler. Oyuncunun kulübü değişince
        /// (satış, alış, fesih) kardeş teklifler CANLI kalırsa aynı oyuncu ikinci kez satılabilir
        /// ya da artık bizde olmayan biri için bedel ödenir (inceleme bulgusu).</summary>
        internal static void TeklifleriTemizle(GameState st, WorldJournal j, int oyuncuId)
        {
            for (int i = 0; i < st.Club.TransferTeklifleri.Length; i++)
                if (st.Club.TransferTeklifleri[i].TeklifId != 0 && st.Club.TransferTeklifleri[i].OyuncuId == oyuncuId)
                    YuvaTemizle(j, i);
        }

        /// <summary>Kadrodan ÇIKAN oyuncu kaptansa kaptanlık düşer. Aksi hâlde hash'lenen kalıcı
        /// durum kadroda olmayan birini kaptan gösterirdi (inceleme bulgusu).</summary>
        internal static void KaptanliktanDusur(GameState st, WorldJournal j, int oyuncuId)
        {
            if (st.Club.KaptanPlayerId == oyuncuId) j.Set(MutTarget.Kulup, 0, ClubField.Kaptan, 0);
        }

        /// <summary>Boş YA DA SÜRESİ DOLMUŞ yuva. Süresi dolmuş teklif yuvayı SONSUZA DEK işgal
        /// ediyordu: cevap yolu da (ret dahil) reddediliyordu, iptal aksiyonu yok, haftalık tick
        /// temizlemiyordu — sekiz teklif sonrası kulüp bir daha teklif VEREMİYORDU (inceleme
        /// bulgusu, iki inceleyici). Yuva geri kazanımı burada.</summary>
        internal static int BosYuvaVeyaSuresiDolmus(GameState st)
        {
            int bos = BosYuva(st);
            if (bos >= 0) return bos;
            for (int i = 0; i < st.Club.TransferTeklifleri.Length; i++)
                if (SureDoldu(st, st.Club.TransferTeklifleri[i])) return i;
            return -1;
        }

        /// <summary>Geçerlilik (sezon, hafta) — sezon dönüşünü AŞMAZ. Yalnız hafta saymak,
        /// sezon 1'e sarınca süresi geçmiş teklifi yeniden geçerli kılardı (K3 inceleme dersi).</summary>
        internal static void Gecerlilik(GameState st, TransferBalance tb, WorldRules kural, out ushort sezon, out ushort hafta)
        {
            int h = st.Takvim.Hafta + tb.pazarlik.teklifGecerlilikHafta;
            int sezonSonu = kural.yapi.sezonHaftaSayisi;
            sezon = st.Takvim.Sezon;
            hafta = (ushort)(h > sezonSonu ? sezonSonu : h);   // sezon sonunda EN GEÇ düşer
        }

        public static bool SureDoldu(GameState st, in TransferOffer t)
        {
            if (t.SonGecerlilikSezon == 0 && t.SonGecerlilikHafta == 0) return false;
            if (st.Takvim.Sezon != t.SonGecerlilikSezon) return st.Takvim.Sezon > t.SonGecerlilikSezon;
            return st.Takvim.Hafta > t.SonGecerlilikHafta;
        }

        public static void YuvaTemizle(WorldJournal j, int yuva)
        {
            j.Set(MutTarget.TransferTeklif, yuva, OfferField.TeklifId, 0);
            j.Set(MutTarget.TransferTeklif, yuva, OfferField.OyuncuId, 0);
            j.Set(MutTarget.TransferTeklif, yuva, OfferField.TeklifEden, 0);
            j.Set(MutTarget.TransferTeklif, yuva, OfferField.Bedel, 0);
            j.Set(MutTarget.TransferTeklif, yuva, OfferField.Maas, 0);
            j.Set(MutTarget.TransferTeklif, yuva, OfferField.SonGecerlilikSezon, 0);
            j.Set(MutTarget.TransferTeklif, yuva, OfferField.SonGecerlilikHafta, 0);
            j.Set(MutTarget.TransferTeklif, yuva, OfferField.SiraTeklifEdende, 0);
            j.Set(MutTarget.TransferTeklif, yuva, OfferField.TurSayisi, 0);
        }
    }
}
