using System;

namespace TheBadge.World
{
    /// <summary>KARŞI TARAF SÜRÜCÜSÜ — K5'te bilerek ertelenen boşluk (DECISIONS, PR #19 açık
    /// thread'i). `propose_offer` topu karşı kulübe bırakıyordu ama hiçbir şey o kulübün sırasını
    /// İLERLETMİYORDU: kullanıcının açtığı teklif kendi başına kabul/ret/karşı teklif alamıyordu.
    ///
    /// NEDEN BURADA: karşı taraf BAŞKA BİR KULÜP. Online ligde onu başka bir oyuncu sürer ve sıra
    /// o cevap verince ilerler; offline'da sürücü bir AI kulüp tick'idir. Bu tahkim K6'nın
    /// konusu olduğu için sürücü de burada. `EconomyTick`/`MacTick` ile AYNI sözleşme: durumu
    /// doğrudan değiştirmez, journal'a yazar, host `Validate` + `Apply` yapar.
    ///
    /// DETERMİNİZM: yuvalar dizi sırasıyla gezilir (sözlük yok); karar `Valuation.Karar`ın
    /// sayaç-RNG'sinden gelir. Aynı durum + aynı seed = aynı sonuç.</summary>
    public static class TransferTick
    {
        /// <summary>Bekleyen tekliflerde AI kulübün sırasını ilerletir.
        ///
        /// Yalnız topu KARŞI TARAFTA olan teklifler işlenir; kullanıcının cevaplaması gereken
        /// teklife AI dokunmaz (yoksa kullanıcının kararını gasp ederdi).
        ///
        /// Süresi dolmuş teklife dokunulmaz: onun temizliği `propose_offer`ın yuva geri
        /// kazanımına ve kullanıcının `ret`ine ait (K5 inceleme kararı). İki yerden temizlemek
        /// aynı yuvayı iki farklı gerekçeyle kapatırdı.</summary>
        public static int Ilerlet(GameState st, TransferBalance tb, WorldRules kural,
                                  ulong saveSeed, WorldJournal j)
        {
            if (st == null) throw new ArgumentNullException(nameof(st));
            if (tb == null) throw new ArgumentNullException(nameof(tb));
            if (kural == null) throw new ArgumentNullException(nameof(kural));
            if (j == null) throw new ArgumentNullException(nameof(j));

            int islenen = 0;
            for (int i = 0; i < st.Club.TransferTeklifleri.Length; i++)
            {
                var t = st.Club.TransferTeklifleri[i];
                if (t.TeklifId == 0) continue;
                if (TransferActions.SureDoldu(st, t)) continue;

                // TOP KİMDE: `SiraTeklifEdende` true ise sıra teklifi AÇANdadır. AI, teklifi
                // açan BİZ olmadığımız durumda değil, BİZ olduğumuzda karşı taraf adına oynar.
                bool bizTeklifEttik = t.TeklifEdenClubId == st.Club.ClubId;
                if (bizTeklifEttik == t.SiraTeklifEdende) continue;   // sıra bizde: dokunma

                int oi = st.IndexOfPlayer(t.OyuncuId);
                if (oi < 0) continue;

                var karar = Valuation.Karar(st.Oyuncular[oi], t.BedelTl, t.TurSayisi, tb, saveSeed, out long karsi);
                islenen++;
                switch (karar)
                {
                    case PazarlikKarari.Kabul:
                        // AI KABUL ETTİ → sıra bize geçer. Transferi AI YAPMAZ: kabul komutu
                        // kullanıcının `respond_offer`ıyla Tek Kapı'dan geçmeli, yoksa kadro
                        // sınırları ve bütçe denetimi atlanırdı.
                        j.Set(MutTarget.TransferTeklif, i, OfferField.SiraTeklifEdende, bizTeklifEttik ? 1 : 0);
                        j.Emit(new WorldEvent(WorldEventType.SozlesmeGuncellendi, t.TeklifId, t.BedelTl,
                                              st.Takvim.Sezon, st.Takvim.Hafta));
                        break;
                    case PazarlikKarari.Ret:
                        TransferActions.YuvaTemizle(j, i);
                        j.Emit(new WorldEvent(WorldEventType.SozlesmeGuncellendi, t.TeklifId, 0,
                                              st.Takvim.Sezon, st.Takvim.Hafta));
                        break;
                    default:
                        if (t.TurSayisi >= tb.pazarlik.maxTur) { TransferActions.YuvaTemizle(j, i); break; }
                        j.Set(MutTarget.TransferTeklif, i, OfferField.Bedel, karsi);
                        j.Set(MutTarget.TransferTeklif, i, OfferField.SiraTeklifEdende, bizTeklifEttik ? 1 : 0);
                        j.Set(MutTarget.TransferTeklif, i, OfferField.TurSayisi, t.TurSayisi + 1);
                        j.Emit(new WorldEvent(WorldEventType.SozlesmeGuncellendi, t.TeklifId, karsi,
                                              st.Takvim.Sezon, st.Takvim.Hafta));
                        break;
                }
            }
            return islenen;
        }
    }
}
