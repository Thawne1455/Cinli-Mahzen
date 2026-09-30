using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>Jinn spirit form (FPS flight) controls (Oyun §7). Consumed by B's SpiritController.</summary>
    public interface ISpiritInput : IPawnInput
    {
        Vector2 Move { get; }
        /// <summary>Mouse delta this frame (pixels).</summary>
        Vector2 Look { get; }
        /// <summary>-1 (Ctrl, down) .. +1 (Space, up).</summary>
        float Vertical { get; }
        bool Boost { get; }
        /// <summary>E — evil: possess the looked-at object; good: exorcise (hold).</summary>
        bool PrimaryPressed { get; }
        bool PrimaryHeld { get; }
        /// <summary>Q — good jinn ping.</summary>
        bool PingPressed { get; }
        /// <summary>R — good jinn bless.</summary>
        bool BlessPressed { get; }
    }
}
