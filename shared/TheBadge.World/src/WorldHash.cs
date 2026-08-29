using System;
using TheBadge.Sim.Core;

namespace TheBadge.World
{
    /// <summary>Dünya durumu checksum'u — ME 3.2'nin `StateHash` deseninin maç dışı karşılığı.
    /// CB 9.1 `PreStateHash`/`PostStateHash` alanlarını besler ve CB 8.3'te save checksum'una
    /// girer ("komut logu save checksum'una girer").
    ///
    /// KAPSAM: yalnız KALICI durum. Olay logu (`WorldEvent`) hash'e GİRMEZ — tek yönlü çıktıdır.
    /// `StateVersion` de GİRMEZ: aynı durumu farklı komut yollarıyla üreten iki save'in hash'i
    /// eşit olmalıdır; versiyon sayacı durum değil MUHASEBEdir (CB 8.2 delta sync için).
    ///
    /// KANONİKLİK: tüm alanlar açık little-endian bayt olarak, SABİT sırada yazılır — platformlar
    /// arası bit eşitliği endianness'e veya struct yerleşimine bırakılmaz.</summary>
    public static class WorldHash
    {
        public static ulong Compute(GameState st)
        {
            if (st == null) return 0UL;
            int talimatYuva = st.Oyuncular.Length > 0 && st.Oyuncular[0].Talimatlar != null
                              ? st.Oyuncular[0].Talimatlar.Length : 0;
            var b = new Buf(160 + st.Presetler.Length * 16
                            + st.Oyuncular.Length * (52 + talimatYuva * 2) + st.Club.InsaatSlot.Length * 24
                            + st.Club.Krediler.Length * 20 + st.Club.TesisTier.Length
                            + st.Club.SponsorTeklifleri.Length * 20
                            + (st.Fiyat.BiletKurus.Length + st.Fiyat.BufeKurus.Length
                               + st.Fiyat.MagazaKurus.Length + 1) * 4);

            // --- Kulüp ---
            b.I64(st.Club.ClubId);
            b.I64(st.Club.OwnerUserId);
            b.I64(st.Club.KasaTl);
            b.I32(st.Club.StadyumKapasite);
            b.I64(st.Club.HaftalikMaasGiderTl);
            b.I64(st.Club.SponsorHaftalikTl);
            b.U16(st.Club.SponsorKalanHafta);
            b.I64(st.Club.DonemInsaatGideriTl);
            b.U8(st.Club.Form);
            // Kadro yönetimi kalıcı alanları — GDD 3.2. Kaptan ve antrenman planı durum
            // senkronunun parçasıdır: replay dördülünde ayrışırsa maç girdisi ayrışır.
            b.I32(st.Club.KaptanPlayerId);
            b.U8(st.Club.AntrenmanPlanId);
            b.U8(st.Club.AntrenmanYogunluk);

            b.I32(st.Club.TesisTier.Length);
            for (int i = 0; i < st.Club.TesisTier.Length; i++) b.U8(st.Club.TesisTier[i]);

            b.I32(st.Club.InsaatSlot.Length);
            for (int i = 0; i < st.Club.InsaatSlot.Length; i++)
            {
                var c = st.Club.InsaatSlot[i];
                b.I32(c.InsaatId); b.I32(c.TesisId); b.U8(c.HedefTier); b.U16(c.KalanHafta); b.I64(c.ToplamMaliyetTl);
            }

            b.I32(st.Club.Krediler.Length);
            for (int i = 0; i < st.Club.Krediler.Length; i++)
            {
                var k = st.Club.Krediler[i];
                b.I32(k.KrediId); b.I64(k.AnaparaTl); b.U16(k.KalanAy); b.U16(k.FaizBp);
            }

            // --- Kadro (kanonik sıra: PlayerId artan) ---
            b.I32(st.Oyuncular.Length);
            for (int i = 0; i < st.Oyuncular.Length; i++)
            {
                var p = st.Oyuncular[i];
                b.I32(p.PlayerId); b.I64(p.ClubId); b.I64(p.HaftalikMaasTl);
                b.U16(p.SozlesmeKalanHafta); b.U8(p.Moral); b.U8(p.Kondisyon); b.U8(p.SakatlikHafta);
                b.U8(p.RolId); b.I32(p.AnchorXmm); b.I32(p.AnchorYmm); b.U8(p.ListedeMi ? (byte)1 : (byte)0);
                b.I32(p.Talimatlar.Length);
                for (int k = 0; k < p.Talimatlar.Length; k++)
                { b.U8(p.Talimatlar[k].TalimatId); b.U8(p.Talimatlar[k].Deger); }
            }

            b.I32(st.Club.SponsorTeklifleri.Length);
            for (int i = 0; i < st.Club.SponsorTeklifleri.Length; i++)
            {
                var so = st.Club.SponsorTeklifleri[i];
                b.I32(so.TeklifId); b.I64(so.HaftalikTl); b.U16(so.SureHafta);
                b.U16(so.SonGecerlilikSezon); b.U16(so.SonGecerlilikHafta);
            }

            // --- Fiyatlar (kuruş) ---
            for (int i = 0; i < st.Fiyat.BiletKurus.Length; i++) b.I32(st.Fiyat.BiletKurus[i]);
            b.I32(st.Fiyat.KombineKurus);
            for (int i = 0; i < st.Fiyat.BufeKurus.Length; i++) b.I32(st.Fiyat.BufeKurus[i]);
            for (int i = 0; i < st.Fiyat.MagazaKurus.Length; i++) b.I32(st.Fiyat.MagazaKurus[i]);

            // --- Taktik + şablonlar ---
            b.U8(st.Taktik.Mentalite); b.U8(st.Taktik.Tempo); b.U8(st.Taktik.Pres); b.U8(st.Taktik.Hat);
            b.I32(st.Presetler.Length);
            for (int i = 0; i < st.Presetler.Length; i++)
            {
                var pr = st.Presetler[i];
                b.U8(pr.Slot); b.U8(pr.Mentalite); b.U8(pr.Tempo); b.U8(pr.Pres); b.U8(pr.Hat);
                // Ad SUNUM verisidir ama durum senkronunun parçası: ham metin yerine DİZE ÖZETİ
                // girer (ME 3.3 `StringHash` deseni — UTF-16 kod birimleri, uzunluk önekli).
                b.I64(unchecked((long)DizeOzeti(pr.Ad)));
            }

            // --- Takvim + maç hakları ---
            b.U16(st.Takvim.Sezon); b.U16(st.Takvim.Hafta); b.U8((byte)st.Takvim.Pencere);
            b.U8(st.KalanDegisiklikHakki);

            return XxHash64.Hash(b.Span);
        }

        /// <summary>Dize özeti — ME 3.3 `ConfigHash.StringHash` ile AYNI kural: UTF-16 kod
        /// birimleri, little-endian, uzunluk önekli, kırpma ve daraltma YOK.</summary>
        static ulong DizeOzeti(string s)
        {
            if (string.IsNullOrEmpty(s)) return XxHash64.Hash(ReadOnlySpan<byte>.Empty);
            var t = new byte[4 + s.Length * 2];
            uint n = (uint)s.Length;
            t[0] = (byte)n; t[1] = (byte)(n >> 8); t[2] = (byte)(n >> 16); t[3] = (byte)(n >> 24);
            for (int i = 0; i < s.Length; i++)
            { ushort c = s[i]; t[4 + i * 2] = (byte)c; t[5 + i * 2] = (byte)(c >> 8); }
            return XxHash64.Hash(t);
        }

        /// <summary>Tahsissiz yazma tamponu — açık little-endian.</summary>
        struct Buf
        {
            readonly byte[] a; int n;
            public Buf(int cap) { a = new byte[cap]; n = 0; }
            public ReadOnlySpan<byte> Span => new ReadOnlySpan<byte>(a, 0, n);
            public void U8(byte v) { a[n++] = v; }
            public void U16(ushort v) { a[n++] = (byte)v; a[n++] = (byte)(v >> 8); }
            public void I32(int v) { U32(unchecked((uint)v)); }
            public void U32(uint v) { a[n++] = (byte)v; a[n++] = (byte)(v >> 8); a[n++] = (byte)(v >> 16); a[n++] = (byte)(v >> 24); }
            public void I64(long v) { U32(unchecked((uint)v)); U32(unchecked((uint)(v >> 32))); }
        }
    }
}
