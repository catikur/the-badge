using System;
using System.Globalization;
using System.Text;

namespace TheBadge.Greybox.Loop
{
    /// <summary>
    /// CommandEnvelope.PayloadJson için bağımlılıksız mikro okuyucu.
    /// Yalnız düz {"anahtar":sayı} yapısını destekler — greybox komut payload'ları bu kadar.
    /// (TheBadge.Sim çekirdeğine paket eklenmez; Newtonsoft/STJ bilinçli olarak yok.)
    /// </summary>
    public static class GreyboxJson
    {
        public static byte[] Payload(string key, double value)
        {
            string s = "{\"" + key + "\":" + value.ToString("0.###", CultureInfo.InvariantCulture) + "}";
            return Encoding.UTF8.GetBytes(s);
        }

        public static bool TryGetNumber(byte[] payloadJson, string key, out double value)
        {
            value = 0;
            if (payloadJson == null || payloadJson.Length == 0) return false;
            string s = Encoding.UTF8.GetString(payloadJson);
            string needle = "\"" + key + "\"";
            int i = s.IndexOf(needle, StringComparison.Ordinal);
            if (i < 0) return false;
            i = s.IndexOf(':', i + needle.Length);
            if (i < 0) return false;
            int start = i + 1;
            int end = start;
            while (end < s.Length && (char.IsDigit(s[end]) || s[end] == '-' || s[end] == '+' ||
                                      s[end] == '.' || s[end] == 'e' || s[end] == 'E' || s[end] == ' '))
                end++;
            string num = s.Substring(start, end - start).Trim();
            return double.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
