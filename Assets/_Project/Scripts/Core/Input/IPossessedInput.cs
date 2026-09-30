using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>Evil jinn inside an object (TPS orbit) controls (Oyun §7). Consumed by B.</summary>
    public interface IPossessedInput : IPawnInput
    {
        /// <summary>Orbit camera, not aim. Mouse delta this frame (pixels).</summary>
        Vector2 Look { get; }
        /// <summary>WASD — only chair/stool move.</summary>
        Vector2 Move { get; }
        /// <summary>A/D limited rotation, -1..+1.</summary>
        float Turn { get; }
        bool Action1Pressed { get; }
        bool Action1Held { get; }
        bool Action2Pressed { get; }
        bool Action2Held { get; }
        /// <summary>Space / E.</summary>
        bool ExitPressed { get; }
    }
}
