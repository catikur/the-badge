using TheBadge.Greybox.Sim;
using UnityEngine;

namespace TheBadge.Greybox.UI
{
    /// <summary>
    /// Greybox spiker satırları — İterasyon 1 ("akan metin olsa maçı anlatmak için güzel olur").
    /// Sunum katmanı metni: içerik kurgusaldır, LLM/prompt hattıyla İLGİSİZDİR (o FAZ 06 işi).
    /// Rastgele seçim UnityEngine.Random iledir — sunum katmanı, determinizm kapsamı dışı.
    /// </summary>
    public static class Commentary
    {
        static readonly string[] Idle =
        {
            "{T} topu çeviriyor, acele etmiyor",
            "Orta sahada kısa pas trafiği",
            "{R} pres deniyor, boşluk arıyor",
            "Tribünler tempo tutuyor",
            "Oyun sakinledi — taktik disiplin ön planda",
            "Kanatlar üzerinden genişlik aranıyor"
        };

        static readonly string[] Chance =
        {
            "{T} ceza sahasına yükleniyor!",
            "Tehlike büyüyor — {T} fırsat kolluyor!",
            "{T} son bölgeye girdi, savunma alarmda!"
        };

        static readonly string[] Shot =
        {
            "{T} şutunu çekti!",
            "{T} kaleyi yokladı!",
            "Uzaklardan denedi {T}!"
        };

        static readonly string[] Header = { "{T} kafayı vurdu!", "Ortaya {T} yükseldi!" };

        static readonly string[] Save =
        {
            "Kaleci müthiş çıkardı!",
            "Eldiven öpen kurtarış!",
            "Kaleci köşeden tırmıkladı!"
        };

        static readonly string[] Wide =
        {
            "Az farkla auta!",
            "Direğin yanından dışarı!",
            "Isabet bulamadı, kale vuruşu"
        };

        static readonly string[] Corner = { "{T} korner kazandı", "Top çizgiyi son anda geçti — korner {T} lehine" };

        static readonly string[] Goal =
        {
            "GOOOL! {T} ağları havalandırdı!",
            "GOOOL! Müthiş bitiriş — {T}!",
            "GOOOL! {T} farkı yazdırdı!"
        };

        /// <summary>Olay satırı; {T} = olayın takımı, {R} = rakibi.</summary>
        public static string For(FlowEventType type, string team, string rival)
        {
            string[] pool;
            switch (type)
            {
                case FlowEventType.ChanceStart: pool = Chance; break;
                case FlowEventType.Shot: pool = Shot; break;
                case FlowEventType.CornerHeader: pool = Header; break;
                case FlowEventType.Save: pool = Save; break;
                case FlowEventType.ShotWide: pool = Wide; break;
                case FlowEventType.Corner: pool = Corner; break;
                case FlowEventType.Goal: pool = Goal; break;
                case FlowEventType.SecondHalfKickOff: return "İkinci yarı başladı";
                case FlowEventType.HalfTime: return "İlk yarı sona erdi";
                default: return "";
            }
            return Pick(pool, team, rival);
        }

        public static string IdleLine(string home, string away)
        {
            // {T}/{R} boşta rastgele taraf alır — iki takım da "yaşasın"
            bool homeSide = Random.Range(0, 2) == 0;
            return Pick(Idle, homeSide ? home : away, homeSide ? away : home);
        }

        static string Pick(string[] pool, string team, string rival) =>
            pool[Random.Range(0, pool.Length)].Replace("{T}", team).Replace("{R}", rival);
    }
}
