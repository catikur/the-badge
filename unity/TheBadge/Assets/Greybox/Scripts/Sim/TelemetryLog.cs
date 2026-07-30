using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace TheBadge.Greybox.Sim
{
    /// <summary>
    /// Fun telemetrisi — Brif K4: yerel JSONL dosya logu.
    /// Kapı metrikleri bu logdan hesaplanır: oturum başına maç sayısı (match_end sayısı),
    /// "Sonraki Maç" tıklama oranı (next_match_click / match_end), maç başına skip (skip olayları),
    /// maç başına izleme süresi (match_end.watch_real_sec).
    /// Saf C#: harness örnek log üretir, Unity tarafı persistentDataPath'e yazar.
    /// Her satır anında flush edilir — playtest sırasında uygulama kapansa da veri kalır.
    /// </summary>
    public sealed class TelemetryLog : IDisposable
    {
        readonly StreamWriter writer;
        readonly string sessionId;
        public string FilePath { get; }

        public TelemetryLog(string directory, string sessionId, string appVersion, string device)
        {
            this.sessionId = sessionId;
            Directory.CreateDirectory(directory);
            FilePath = Path.Combine(directory, "telemetry_" + sessionId + ".jsonl");
            // BOM'suz UTF-8: JSONL ilk satırı her ayrıştırıcıda temiz kalsın
            writer = new StreamWriter(new FileStream(FilePath, FileMode.Append, FileAccess.Write, FileShare.Read), new UTF8Encoding(false));
            Event("session_start").Str("app_ver", appVersion).Str("device", device).Send();
        }

        public EventBuilder Event(string type) => new EventBuilder(this, type);

        void WriteLine(string json)
        {
            writer.WriteLine(json);
            writer.Flush();
        }

        public void Dispose() => writer.Dispose();

        /// <summary>Bağımlılıksız mini JSON satır kurucusu (kültür-bağımsız sayı formatı).</summary>
        public sealed class EventBuilder
        {
            readonly TelemetryLog log;
            readonly StringBuilder sb = new StringBuilder(160);

            internal EventBuilder(TelemetryLog log, string type)
            {
                this.log = log;
                sb.Append("{\"t\":\"").Append(type).Append('"');
                Str("sid", log.sessionId);
                Str("ts", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
            }

            public EventBuilder Str(string key, string value)
            {
                sb.Append(",\"").Append(key).Append("\":\"").Append(Escape(value)).Append('"');
                return this;
            }

            public EventBuilder Num(string key, long value)
            {
                sb.Append(",\"").Append(key).Append("\":").Append(value.ToString(CultureInfo.InvariantCulture));
                return this;
            }

            public EventBuilder Num(string key, double value)
            {
                sb.Append(",\"").Append(key).Append("\":")
                  .Append(value.ToString("0.0##", CultureInfo.InvariantCulture));
                return this;
            }

            public void Send()
            {
                sb.Append('}');
                log.WriteLine(sb.ToString());
            }

            static string Escape(string s)
            {
                if (string.IsNullOrEmpty(s)) return "";
                return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");
            }
        }
    }
}
