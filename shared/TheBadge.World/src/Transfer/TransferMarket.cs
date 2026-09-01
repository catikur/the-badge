using System;
using System.Collections.Generic;
using TheBadge.Sim.Determinism;

namespace TheBadge.World
{
    /// <summary>`balance/market.balance.json` POCO'su — K12-C piyasa modeli [KALİBRE].</summary>
    [Serializable]
    public sealed class MarketBalance
    {
        public int surum;
        public string aciklama;
        public int sezonBasiGiris;        // her sezon havuza katılan oyuncu sayısı
        public int serbestPayiYuzde;      // bunların yüzde kaçı SERBEST (ClubId 0)
        public int gucAlt, gucUst;        // yeni oyuncu güç bandı
        public int yasAlt, yasUst;
        public int potansiyelEkAlt, potansiyelEkUst;
        public int sozlesmeHaftaAlt, sozlesmeHaftaUst;
        public int rakipKulupSayisi;      // havuzdaki oyuncuların dağıtıldığı rakip kulüp sayısı
        public int havuzTavani;           // dünyadaki toplam oyuncu tavanı (bellek + determinizm sınırı)

        public void Validate()
        {
            if (sezonBasiGiris < 1) throw new ArgumentException("market.balance: sezonBasiGiris ≥ 1 olmalı.");
            if (serbestPayiYuzde < 0 || serbestPayiYuzde > 100)
                throw new ArgumentException("market.balance: serbestPayiYuzde 0-100 olmalı.");
            if (gucAlt < 1 || gucUst > 99 || gucAlt >= gucUst)
                throw new ArgumentException("market.balance: güç bandı 1 ≤ alt < üst ≤ 99 olmalı.");
            if (yasAlt < 15 || yasUst > 40 || yasAlt >= yasUst)
                throw new ArgumentException("market.balance: yaş bandı 15 ≤ alt < üst ≤ 40 olmalı.");
            if (rakipKulupSayisi < 1) throw new ArgumentException("market.balance: rakipKulupSayisi ≥ 1 olmalı.");
            if (havuzTavani < 64) throw new ArgumentException("market.balance: havuzTavani ≥ 64 olmalı.");
        }
    }

    /// <summary>TRANSFER PİYASASI — K12-C.
    ///
    /// NEDEN VAR: `K10MerdivenSonrasiSink` borcu "merdiven tükenince geriye sink kalmıyor" diyordu
    /// ve K11-E'de kalem (`WeekLedger.TransferTl`) bağlanmıştı, ama BORÇ ÖLÇÜLEMİYORDU: oyuncu
    /// havuzu fikstürle sınırlıydı, yenilenmiyordu ve rakip kulüplerin bütçesi yoktu. Kalemi
    /// bağlamak yetmiyordu; sürekli bir piyasa gerekiyordu.
    ///
    /// NE MODELLENİYOR: her sezon başı havuza yeni oyuncular katılır (gençlik + serbestler),
    /// bir kısmı serbest, bir kısmı rakip kulüplerde. Bu, ECONOMY_MAP'in "Transfer bedelleri"
    /// sink'ini SÜREKLİ hâle getirir — kulüp fazlasını sonsuza dek stadyuma değil kadroya da
    /// harcayabilir.
    ///
    /// NE MODELLENMİYOR (bilerek, ve gizlenmiyor): rakip kulüplerin KENDİ ekonomileri. Bir rakip
    /// bugün ne kasa tutuyor ne bütçe harcıyor; yalnız oyuncu SAHİBİdir ve `Valuation` kurallarıyla
    /// pazarlık eder. Tam çok-kulüp ekonomisi ayrı bir dilim (DECISIONS). Buradaki model, sink'in
    /// ÖLÇÜLEBİLİR olması için gereken asgari gerçekliktir — daha fazlasını uydurmak, ölçümü
    /// varsayımın kendisine çevirirdi.
    ///
    /// DETERMİNİZM: tüm çekilişler sayaç-RNG (`Rng.Rand01`) + save seed. Aynı seed = aynı piyasa.</summary>
    public static class TransferMarket
    {
        /// <summary>Sezon başı havuz girişi. `st.Oyuncular` KANONİK sırada (PlayerId artan) kalır.
        /// Havuz tavanına ulaşıldıysa giriş YAPILMAZ — sessizce büyüyen bir dizi, determinizm
        /// bütçesini ve `WorldHash` maliyetini fark ettirmeden şişirirdi.</summary>
        public static int SezonBasiGiris(GameState st, WorldRules kural, MarketBalance mb,
                                         TransferBalance tb, ulong saveSeed, int sezon)
        {
            if (st == null) throw new ArgumentNullException(nameof(st));
            if (mb == null) throw new ArgumentNullException(nameof(mb));
            if (st.Oyuncular.Length >= mb.havuzTavani) return 0;

            int adet = mb.sezonBasiGiris;
            if (st.Oyuncular.Length + adet > mb.havuzTavani) adet = mb.havuzTavani - st.Oyuncular.Length;

            int enBuyukId = 0;
            for (int i = 0; i < st.Oyuncular.Length; i++)
                if (st.Oyuncular[i].PlayerId > enBuyukId) enBuyukId = st.Oyuncular[i].PlayerId;

            var yeni = new List<PlayerState>(st.Oyuncular);
            int eklenen = 0;
            for (int k = 0; k < adet; k++)
            {
                int pid = enBuyukId + 1 + k;
                // KİMLİK GENİŞLİĞİ: motor tarafı `short` (SquadBridge denetliyor). Havuz burada
                // durur — sessizce kırpılan bir kimlik iki oyuncuyu birleştirirdi.
                if (pid > short.MaxValue) break;
                // DOMAIN SEÇİMİ: `Chaos` — piyasa girişi bir DÜNYA çekilişidir, maç içi bir karar
                // ya da kalabalık davranışı değil. `Crowd` seyirci modelinin akışı (EconomyTick
                // onu kullanıyor); aynı akışı paylaşmak iki alt sistemi birbirine bağlardı.
                uint e = (uint)pid;
                byte guc = (byte)(mb.gucAlt + (int)(Rng.Rand01(saveSeed, Domain.Chaos, e, (uint)sezon, 71)
                                                     * (mb.gucUst - mb.gucAlt + 1)));
                byte yas = (byte)(mb.yasAlt + (int)(Rng.Rand01(saveSeed, Domain.Chaos, e, (uint)sezon, 72)
                                                     * (mb.yasUst - mb.yasAlt + 1)));
                int potEk = mb.potansiyelEkAlt + (int)(Rng.Rand01(saveSeed, Domain.Chaos, e, (uint)sezon, 73)
                                                        * (mb.potansiyelEkUst - mb.potansiyelEkAlt + 1));
                bool serbest = Rng.Rand01(saveSeed, Domain.Chaos, e, (uint)sezon, 74) * 100.0 < mb.serbestPayiYuzde;
                long clubId = serbest ? 0L
                    : 900L + (long)(Rng.Rand01(saveSeed, Domain.Chaos, e, (uint)sezon, 75) * mb.rakipKulupSayisi);
                // ROL: köprünün `rolHat` uzayı (1 KL · 2-8 DF · 9-20 OS · 21-32 FV) — dağılım
                // 4-4-2 ihtiyacına yakın tutulur ki havuz tek mevkiye yığılmasın.
                double rr = Rng.Rand01(saveSeed, Domain.Chaos, e, (uint)sezon, 76);
                byte rol = rr < 0.10 ? (byte)1
                         : rr < 0.42 ? (byte)(2 + (int)(rr * 100) % 7)
                         : rr < 0.78 ? (byte)(9 + (int)(rr * 100) % 12)
                                     : (byte)(21 + (int)(rr * 100) % 12);
                var p = new PlayerState
                {
                    PlayerId = pid,
                    ClubId = clubId,
                    Guc = guc,
                    Potansiyel = (byte)Math.Min(99, guc + potEk),
                    Yas = yas,
                    RolId = rol,
                    Moral = 60,
                    Kondisyon = 90,
                    SozlesmeKalanHafta = serbest ? (ushort)0
                        : (ushort)(mb.sozlesmeHaftaAlt + (int)(Rng.Rand01(saveSeed, Domain.Chaos, e, (uint)sezon, 77)
                                                                * (mb.sozlesmeHaftaUst - mb.sozlesmeHaftaAlt + 1))),
                    Talimatlar = new Instruction[kural.yapi.talimatYuvaSayisi]
                };
                p.HaftalikMaasTl = serbest ? 0 : Valuation.MaasTalebi(p, tb);
                yeni.Add(p);
                eklenen++;
            }
            // PlayerId artan üretildi ve sona eklendi → kanonik sıra korunuyor.
            st.Oyuncular = yeni.ToArray();
            // GERÇEKTEN eklenen sayı döner: döngü `short` kimlik sınırında erken kesilebilir ve
            // "istenen kadar eklendi" demek, havuzun dolduğunu çağırandan gizlerdi.
            return eklenen;
        }

        /// <summary>Kulübün kadrosunu güçlendirecek EN İYİ hedef — deterministik. Ölçüt: gücü
        /// kadronun EN ZAYIF oyuncusundan yüksek, bedeli karşılanabilir. Eşitlikte PlayerId artan
        /// (kanonik). Bulunamazsa -1.
        ///
        /// SERBEST OYUNCU DA DÖNEBİLİR (`ClubId == 0`) ve ÇAĞIRAN YOLU AYIRMAK ZORUNDADIR:
        /// `transfer.propose_offer` serbest oyuncuyu `NotOwned` ile reddeder (sahiplik denetimi
        /// K2'de `OwnerNeed.Yabanci` ile yapılıyor), doğru yol `transfer.sign_free_agent`tir.
        /// İlk yazımda çağıran bu ayrımı yapmıyordu: aynı serbest oyuncu her hafta seçiliyor,
        /// teklif sessizce reddediliyor ve transfer sink'i KİLİTLENİYORDU (inceleme bulgusu, P1).
        /// Serbest oyuncuyu burada elemek de bir seçenekti; elenmedi çünkü serbest transfer
        /// gerçek bir kazanım yolu ve maaş yüküyle yine sink'e girer.</summary>
        public static int EnIyiHedef(GameState st, long clubId, TransferBalance tb, long butce,
                                     int kadroMax)
        {
            int kadro = 0; byte enZayif = 255;
            for (int i = 0; i < st.Oyuncular.Length; i++)
                if (st.Oyuncular[i].ClubId == clubId)
                {
                    kadro++;
                    if (st.Oyuncular[i].Guc < enZayif) enZayif = st.Oyuncular[i].Guc;
                }
            if (kadro >= kadroMax) return -1;

            int en = -1; byte enGuc = enZayif;
            for (int i = 0; i < st.Oyuncular.Length; i++)
            {
                var p = st.Oyuncular[i];
                if (p.ClubId == clubId) continue;
                if (p.Guc <= enGuc) continue;
                if (Valuation.PiyasaDegeri(p, tb) > butce) continue;
                if (en < 0 || p.Guc > st.Oyuncular[en].Guc
                    || (p.Guc == st.Oyuncular[en].Guc && p.PlayerId < st.Oyuncular[en].PlayerId)) en = i;
            }
            return en;
        }
    }
}
