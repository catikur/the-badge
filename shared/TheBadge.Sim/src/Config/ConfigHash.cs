using System;
using TheBadge.Sim.Core;
using TheBadge.Sim.Match;

namespace TheBadge.Sim.Config
{
    /// <summary>
    /// config_hash — ME Spec 3.3. Replay dörtlüsünün ({engineVersion, config_hash, seed,
    /// komut zaman çizelgesi}) kimlik üyesi: aynı hash = aynı kurulum = bit-eşit oynatılabilir.
    ///
    /// SPEC'TEN BİLİNÇLİ SAPMA (mimari değişmez gereği): 3.3 "balanceJson_kanonik_bytes" der,
    /// ama `TheBadge.Sim` JSON PARSE ETMEZ (CLAUDE.md bağımlılıksızlık kuralı). Bu yüzden ham
    /// bayt özeti HOST tarafından hesaplanır (dosyayı zaten o okur) ve `MatchConfig.BalanceHash`
    /// ile içeri verilir. Sonuç spec'in amacını BİREBİR karşılar: balance dosyasındaki tek bayt
    /// değişikliği bile config_hash'i değiştirir; çekirdek ise bağımlılıksız kalır.
    ///
    /// Kapsam 3.3'ün listesinden GENİŞTİR (M17): hava/zemin/rüzgar (12.4) ve chaos seviyesi
    /// (13.2) sonucu doğrudan değiştirir — kimliğe girmezlerse iki farklı kurulum aynı hash'i
    /// paylaşır ve "eski replay yeni parametrelerle sessizce oynamaz" güvencesi delinirdi.
    /// FAZ 04'te bir üye daha eklendi: KOMUT BANTLARI (`balance/command.bands.json`) — bantlar
    /// hangi komutun kabul edildiğini belirler, o da replay dörtlüsünün dördüncü üyesi olan
    /// komut zaman çizelgesini belirler (Atilla kararı, 2026-08-25).
    /// </summary>
    public static class ConfigHash
    {
        /// <summary>Kadro anlık görüntüsü özeti (3.3 rosterSnapshotHash): PlayerId + tüm
        /// nitelikler, SABİT sırayla. Nitelik tek bayt bile değişirse hash değişir.</summary>
        public static ulong RosterHash(TeamSheet home, TeamSheet away)
        {
            if (home == null) throw new ArgumentNullException(nameof(home));
            if (away == null) throw new ArgumentNullException(nameof(away));
            Span<byte> buf = stackalloc byte[4096];
            int o = 0;
            WriteSheet(buf, ref o, home);
            WriteSheet(buf, ref o, away);
            return XxHash64.Hash(buf.Slice(0, o));
        }

        static void WriteSheet(Span<byte> buf, ref int o, TeamSheet s)
        {
            WriteRoster(buf, ref o, s.Starters);
            WriteRoster(buf, ref o, s.Bench);
        }

        static void WriteRoster(Span<byte> buf, ref int o, PlayerEntry[] list)
        {
            if (list == null) { W32(buf, ref o, 0xFFFFFFFF); return; }
            W32(buf, ref o, (uint)list.Length);
            for (int i = 0; i < list.Length; i++)
            {
                var e = list[i];
                W32(buf, ref o, (uint)(ushort)e.PlayerId);
                buf[o++] = e.RoleId;
                W32(buf, ref o, (uint)e.AnchorXmm);
                W32(buf, ref o, (uint)e.AnchorYmm);
                var p = e.Attributes;
                buf[o++] = p.Passing; buf[o++] = p.Finishing; buf[o++] = p.Dribbling; buf[o++] = p.Tackling;
                buf[o++] = p.Heading; buf[o++] = p.FirstTouch; buf[o++] = p.Crossing; buf[o++] = p.SetPieces;
                buf[o++] = p.Positioning; buf[o++] = p.Decisions; buf[o++] = p.Composure; buf[o++] = p.Aggression;
                buf[o++] = p.Workrate; buf[o++] = p.Vision; buf[o++] = p.Pace; buf[o++] = p.Acceleration;
                buf[o++] = p.Stamina; buf[o++] = p.Strength; buf[o++] = p.Agility; buf[o++] = p.JumpReach;
                buf[o++] = p.Reflexes; buf[o++] = p.Handling; buf[o++] = p.OneOnOne; buf[o++] = p.AerialCommand;
                buf[o++] = p.Kicking; buf[o++] = p.Throwing;
            }
        }

        /// <summary>config_hash hesabı. `balanceBytesHash` ve `commandBandsBytesHash`: ilgili
        /// balance dosyalarının HAM bayt özetleri (host hesaplar — yukarıdaki sapma notu).
        /// Kadro özeti kurulumdan türetilir.
        ///
        /// `commandBandsBytesHash` ZORUNLU parametredir (varsayılan yok): unutulabilir bir
        /// varsayılan, bant değişikliğinin kimliğe sessizce girmemesi demek olurdu.</summary>
        public static ulong Compute(MatchConfig cfg, ulong balanceBytesHash, ulong commandBandsBytesHash)
        {
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));
            Span<byte> buf = stackalloc byte[256];
            int o = 0;
            // Motor sürümü — TAM dize, kırpma ve daraltma YOK. İnceleme bulgusu (Codex): ilk
            // sürüm baytları sabit tampona yazıyordu; 196 karakterden uzun sürümlerde kuyruk
            // sessizce düşüyor, ayrıca (byte) daraltması ASCII dışı karakterleri örtüşüyordu —
            // ikisi de replay uyumluluğunu belirleyen bir kimlik için kabul edilemez.
            // Dize AYRI hash'lenir (UTF-16 kod birimleri, little-endian) ve 8 bayt olarak girer.
            W64(buf, ref o, StringHash(cfg.EngineVersion));
            // LOD + tick oranı (3.3 "lodLevel, tickRates")
            buf[o++] = (byte)cfg.Lod;
            W32(buf, ref o, (uint)MatchEngine.TicksPerSecond);
            // Balance ham bayt özeti (3.3 "balanceJson_kanonik_bytes")
            W64(buf, ref o, balanceBytesHash);
            // Komut bantları ham bayt özeti — config_hash İÇİ (Atilla kararı, 2026-08-25):
            // bantlar hangi komutun kabul edildiğini, o da komut zaman çizelgesini belirler.
            W64(buf, ref o, commandBandsBytesHash);
            // Chaos (3.3) + M17 genişletmesi: hava/zemin/rüzgar (12.4) ve hakem profili
            buf[o++] = (byte)cfg.Chaos;
            buf[o++] = (byte)cfg.Weather;
            buf[o++] = cfg.PitchTier;
            // Rüzgar KUANTALANIR: kalıcı kimlik float taşımaz (ME 3.2 ilkesi)
            W32(buf, ref o, (uint)Units.QuantizeMm(cfg.WindMS));
            W32(buf, ref o, (uint)Units.QuantizeMm(cfg.WindDirX));
            W32(buf, ref o, (uint)Units.QuantizeMm(cfg.WindDirY));
            buf[o++] = cfg.Referee.Strictness;
            buf[o++] = cfg.Referee.AdvantageTendency;
            buf[o++] = cfg.Referee.Consistency;
            // Kadro anlık görüntüsü (3.3 rosterSnapshotHash)
            W64(buf, ref o, RosterHash(cfg.Home, cfg.Away));
            return XxHash64.Hash(buf.Slice(0, o));
        }

        /// <summary>Dize özeti — UTF-16 kod birimleri, little-endian, uzunluk önekli.
        /// Kırpma yok; kod birimi daraltması yok. Soğuk yol (kurulumda bir kez) olduğu için
        /// tahsis serbesttir — zero-alloc kuralı tick geçişleri içindir (ME 16.2).</summary>
        static ulong StringHash(string s)
        {
            if (string.IsNullOrEmpty(s)) return XxHash64.Hash(ReadOnlySpan<byte>.Empty);
            var b = new byte[4 + s.Length * 2];
            uint n = (uint)s.Length;
            b[0] = (byte)n; b[1] = (byte)(n >> 8); b[2] = (byte)(n >> 16); b[3] = (byte)(n >> 24);
            for (int i = 0; i < s.Length; i++)
            {
                ushort c = s[i];
                b[4 + i * 2] = (byte)c;
                b[5 + i * 2] = (byte)(c >> 8);
            }
            return XxHash64.Hash(b);
        }

        static void W32(Span<byte> b, ref int o, uint v)
        {
            b[o++] = (byte)v; b[o++] = (byte)(v >> 8); b[o++] = (byte)(v >> 16); b[o++] = (byte)(v >> 24);
        }

        static void W64(Span<byte> b, ref int o, ulong v)
        {
            W32(b, ref o, (uint)v); W32(b, ref o, (uint)(v >> 32));
        }
    }
}
