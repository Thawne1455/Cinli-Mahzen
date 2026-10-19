using System.Text;
using CinliMahzen.Core;

namespace CinliMahzen.UI
{
    /// <summary>
    /// Text building for the placeholder HUD (TODO 3.11), plain C#. The view calls these only when the underlying number changes,
    /// never every frame. Glyphs (hearts, bar blocks, arrows) are symbols, not words, so they are not Loc keys.
    /// </summary>
    public static class HudFormat
    {
        private const char HeartFull = '♥';   // ♥
        private const char HeartEmpty = '♡';  // ♡
        private const char BarFull = '■';     // ■
        private const char BarEmpty = '□';    // □
        private static readonly char[] Arrows = { '↑', '↗', '→', '↘', '↓', '↙', '←', '↖' }; // ↑ ↗ → ↘ ↓ ↙ ← ↖

        /// <summary>"m:ss"; negative clamps to 0:00.</summary>
        public static string Clock(int seconds)
        {
            if (seconds < 0)
                seconds = 0;
            int m = seconds / 60;
            int s = seconds % 60;
            return m + ":" + (s < 10 ? "0" : string.Empty) + s;
        }

        public static string Hearts(int hp, int max)
        {
            if (max < 0)
                max = 0;
            var sb = new StringBuilder(max);
            for (int i = 0; i < max; i++)
                sb.Append(i < hp ? HeartFull : HeartEmpty);
            return sb.ToString();
        }

        /// <summary>A <paramref name="cells"/>-wide block bar for <paramref name="value01"/> (rounded down, clamped).</summary>
        public static string Bar(float value01, int cells)
        {
            if (cells <= 0)
                return string.Empty;
            int full = BarCells(value01, cells);
            var sb = new StringBuilder(cells);
            for (int i = 0; i < cells; i++)
                sb.Append(i < full ? BarFull : BarEmpty);
            return sb.ToString();
        }

        /// <summary>Filled cells of a <see cref="Bar"/>: the key the view caches on.</summary>
        public static int BarCells(float value01, int cells)
        {
            if (value01 <= 0f || float.IsNaN(value01))
                return 0;
            if (value01 >= 1f)
                return cells;
            return (int)(value01 * cells);
        }

        /// <summary>
        /// One of eight arrows for a target at <paramref name="signedAngleDeg"/> from the view direction (0 = ahead, +90 = right),
        /// i.e. <c>Vector3.SignedAngle(forward, toTarget, up)</c>.
        /// </summary>
        public static char Arrow(float signedAngleDeg)
        {
            float a = signedAngleDeg % 360f;
            if (a < 0f)
                a += 360f;
            int index = (int)((a + 22.5f) / 45f) % 8;
            return Arrows[index];
        }

        public static string RoleKey(Role role)
        {
            switch (role)
            {
                case Role.Human: return "role.human";
                case Role.GoodJinn: return "role.good_jinn";
                case Role.EvilJinn: return "role.evil_jinn";
                default: return "role.none";
            }
        }

        public static string ResultKey(RoundResult result)
        {
            switch (result)
            {
                case RoundResult.SeekersWin: return "hud.result_seekers";
                case RoundResult.JinnsWin: return "hud.result_jinns";
                default: return "hud.result_none";
            }
        }

        public static string ReasonKey(RoundEndReason reason)
        {
            switch (reason)
            {
                case RoundEndReason.Faint: return "hud.reason_faint";
                case RoundEndReason.TreasureDug: return "hud.reason_treasure";
                case RoundEndReason.Timeout: return "hud.reason_timeout";
                default: return "hud.result_none";
            }
        }
    }
}
