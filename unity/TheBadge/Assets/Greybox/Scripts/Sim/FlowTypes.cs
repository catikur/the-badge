using System;

namespace TheBadge.Greybox.Sim
{
    /// <summary>
    /// Motor bağımsız 2D vektör — FlowSim UnityEngine'e SIZMAZ (greybox headless doğrulanabilirlik).
    /// Kalıcı float durum serbest: FAZ 00.5 his prototipi, determinizm borcu FAZ 03'ün (Brif K5).
    /// </summary>
    [Serializable]
    public struct Vec2
    {
        public float X;
        public float Y;

        public Vec2(float x, float y) { X = x; Y = y; }

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);
        public static Vec2 operator *(Vec2 a, float k) => new Vec2(a.X * k, a.Y * k);

        public float Magnitude => (float)Math.Sqrt(X * X + Y * Y);

        public Vec2 Normalized
        {
            get
            {
                float m = Magnitude;
                return m < 1e-6f ? new Vec2(0f, 0f) : new Vec2(X / m, Y / m);
            }
        }

        public static Vec2 MoveTowards(Vec2 from, Vec2 to, float maxStep)
        {
            Vec2 d = to - from;
            float m = d.Magnitude;
            if (m <= maxStep || m < 1e-6f) return to;
            return from + d * (maxStep / m);
        }

        public static float Distance(Vec2 a, Vec2 b) => (a - b).Magnitude;
    }

    /// <summary>Maç akış fazları — basitleştirilmiş akış modeli, ME Spec'in FSM'i DEĞİL (Brif K2).</summary>
    public enum FlowPhase
    {
        KickOff,          // santra dizilişi + kısa bekleme
        OpenPlay,         // top pas/taşıma dalgalanması
        ChanceBuild,      // ceza sahası çevresi tehlike anı ("önemli an" penceresi başlangıcı)
        ShotTravel,       // top kaleye gidiyor
        CornerSetup,      // top korner noktasına taşınıyor
        CornerCross,      // orta havada
        GoalCelebration,  // gol sonrası kutlama duraklaması
        HalfTimeBreak,    // devre arası bandosu
        FullTime          // maç bitti
    }

    /// <summary>Sunum katmanına akan tekil olaylar (telemetri + vurgu tetikleri).</summary>
    public enum FlowEventType
    {
        KickOff,
        HalfTime,
        SecondHalfKickOff,
        Shot,           // şut çekildi (sonucu henüz belli değil)
        Goal,
        Save,
        ShotWide,
        Corner,
        CornerHeader,   // korner sonrası kafa vuruşu (şut sayılır)
        FullTime,
        ChanceStart     // ceza sahasına giriş — spiker/gerilim satırı için (İterasyon 1)
    }

    /// <summary>Sıralı olay kaydı; View kuyruktan çeker, Telemetry loglar.</summary>
    public struct FlowEvent
    {
        public FlowEventType Type;
        public int Team;        // 0 = ev sahibi, 1 = deplasman; -1 = takımsız (devre vb.)
        public float Minute;    // maç dakikası (gösterim)
        public int HomeScore;
        public int AwayScore;

        public FlowEvent(FlowEventType type, int team, float minute, int hs, int aw)
        { Type = type; Team = team; Minute = minute; HomeScore = hs; AwayScore = aw; }
    }

    /// <summary>Maç kurulumu: seed + iki tarafın taktik/güç girdileri.</summary>
    public struct MatchSetup
    {
        public ulong Seed;
        public int HomeTacticId;
        public int AwayTacticId;
        public float HomeStrength;   // 0-100 bandı; momentum ve şut kalitesini eğer
        public float AwayStrength;
    }

    /// <summary>Maç istatistikleri — pacing kanıtı ve maç sonu ekranı için.</summary>
    public sealed class MatchStats
    {
        public int HomeShots;
        public int AwayShots;
        public int HomeOnTarget;
        public int AwayOnTarget;
        public int HomeCorners;
        public int AwayCorners;

        public int TotalShots => HomeShots + AwayShots;
        public int TotalCorners => HomeCorners + AwayCorners;
    }

    /// <summary>Sahadaki bir nokta (oyuncu). View daireyi bu pozisyondan çizer.</summary>
    public struct PlayerDot
    {
        public Vec2 Pos;
        public Vec2 Target;
        public int Team;        // 0 ev, 1 deplasman
        public bool IsKeeper;
    }
}
