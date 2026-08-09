using System;
using System.Collections.Generic;
using TheBadge.Sim.Core;

namespace TheBadge.Sim.Match
{
    /// <summary>
    /// Tick-damgalı komut kuyruğu — ME Spec 14.1'in iskeleti (Tek Kapı'nın maç içi ucu).
    /// Uygulama sırası DETERMİNİSTİKTİR: (IssueTick artan, kuyruğa giriş sırası artan).
    /// Aynı komut zaman çizelgesi = aynı uygulama dizisi; replay dörtlüsünün parçası (ME 14.5).
    /// M0: komutlar durumu DEĞİŞTİRMEZ (davranış M-müdahale diliminde, uygulanma anları 14.2);
    /// uygulanan dizi sayaç + kanonik izle (AppliedTraceHash) denetlenir. Liste tahsisleri
    /// soğuk yoldur (komut nadir); sıcak yol zero-alloc kuralı tick geçişleri içindir (ME 16.2).
    /// </summary>
    public sealed class CommandQueue
    {
        struct Entry
        {
            public uint Seq;            // kuyruğa giriş sırası — aynı tick içinde eşitlik bozucu
            public MatchCommand Cmd;
        }

        readonly List<Entry> pending = new List<Entry>();
        uint nextSeq;

        public uint AppliedCount { get; private set; }

        /// <summary>Uygulanan dizinin kanonik izi: (tip, IssueTick, TeamIdx) zinciri.
        /// Seq İZE GİRMEZ — tick'ler arası uygulama sırası kuyruğa giriş sırasından bağımsızdır
        /// (denetim: Checks/MatchCommandOrder); aynı tick içi sıra zaten zaman çizelgesinin tanımıdır.</summary>
        public ulong AppliedTraceHash { get; private set; }

        public void Enqueue(MatchCommand cmd)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));
            pending.Add(new Entry { Seq = nextSeq++, Cmd = cmd });
        }

        /// <summary>Komutun DAVRANIŞINI uygulayan taraf (motor). Kuyruk yalnız sıralamadan sorumludur;
        /// uygulama anları ve doğrulama ME 14.2'ye tabidir (M6).</summary>
        public interface ISink { void ApplyCommand(MatchCommand cmd, ref MatchState st); }

        ISink sink;

        /// <summary>Motor kendini kuyruğa tanıtır (kurulumda bir kez).</summary>
        public void Bind(ISink handler) => sink = handler;

        /// <summary>Vadesi gelen (IssueTick ≤ tick) komutları deterministik sırayla uygular — ME 4.2 aşama 1.</summary>
        public void ApplyDue(uint tick, ref MatchState state)
        {
            if (pending.Count == 0) return;

            // Vadesi gelenleri topla, (IssueTick, Seq) sırala — Dictionary/sırasız yapı YOK (ME 3.2)
            List<Entry> due = null;
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                if (pending[i].Cmd.IssueTick > tick) continue;
                (due ??= new List<Entry>()).Add(pending[i]);
                pending.RemoveAt(i);
            }
            if (due == null) return;
            due.Sort((a, b) =>
            {
                int c = a.Cmd.IssueTick.CompareTo(b.Cmd.IssueTick);
                return c != 0 ? c : a.Seq.CompareTo(b.Seq);
            });

            for (int i = 0; i < due.Count; i++)
                Apply(due[i].Cmd, ref state);
        }

        void Apply(MatchCommand cmd, ref MatchState state)
        {
            // Sıralama kuyruğun, DAVRANIŞ motorun işidir (M6): motor 14.2 uygulama anlarını
            // (taktik ≤250 ms, değişiklik DEAD_BALL) ve bant doğrulamasını uygular.
            sink?.ApplyCommand(cmd, ref state);
            AppliedCount++;
            Span<byte> buf = stackalloc byte[14];
            buf[0] = TypeTag(cmd);
            WriteU32(buf, 1, cmd.IssueTick);
            buf[5] = cmd.TeamIdx;
            WriteU64(buf, 6, AppliedTraceHash);
            AppliedTraceHash = XxHash64.Hash(buf);
        }

        static byte TypeTag(MatchCommand cmd) => cmd switch
        {
            SubstitutionCmd _ => 1,
            TacticChangeCmd _ => 2,
            InstructionCmd _ => 3,
            MotivationCmd _ => 4,
            _ => 255
        };

        static void WriteU32(Span<byte> b, int i, uint v)
        { b[i] = (byte)v; b[i + 1] = (byte)(v >> 8); b[i + 2] = (byte)(v >> 16); b[i + 3] = (byte)(v >> 24); }

        static void WriteU64(Span<byte> b, int i, ulong v)
        { for (int k = 0; k < 8; k++) b[i + k] = (byte)(v >> (k * 8)); }
    }
}
