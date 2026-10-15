using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>
    /// Jinn spirit form (FPS flight) controls (Teknik §5.1/§6.5, Oyun §11 "Cin — Ruh Formu"). Consumed by B's SpiritController.
    /// "Pressed" members are true only on the frame the button went down; "Held" members stay true while it is down.
    /// </summary>
    public interface ISpiritInput : IPawnInput
    {
        /// <summary>WASD.</summary>
        Vector2 Move { get; }
        /// <summary>Mouse delta this frame (pixels).</summary>
        Vector2 Look { get; }
        /// <summary>-1 (Ctrl, down) .. +1 (Space, up).</summary>
        float Vertical { get; }
        /// <summary>Shift — fast flight (an evil jinn passing through a focusing good jinn causes a Spirit Bump).</summary>
        bool Boost { get; }
        /// <summary>
        /// E (down this frame) — evil: possess the looked-at object / start sabotaging a puzzle; good: light a lamp.
        /// False while Shift is held (Shift+E is <see cref="PossessAsGoodPressed"/>).
        /// </summary>
        bool PrimaryPressed { get; }
        /// <summary>
        /// E (held) — evil: sabotage a puzzle (1.5 s); good: repair a lamp (2 s).
        /// False while Shift is held (Shift+E is <see cref="PossessAsGoodPressed"/>).
        /// </summary>
        bool PrimaryHeld { get; }
        /// <summary>Right mouse button (held) — good jinn: Spirit Sight focus (opens the puzzle reference).</summary>
        bool FocusHeld { get; }
        /// <summary>Shift+E (down this frame) — good jinn: possess an object that allows good jinn.</summary>
        bool PossessAsGoodPressed { get; }
        /// <summary>Q — good jinn: ping.</summary>
        bool PingPressed { get; }
        /// <summary>R (held) — good jinn: exorcise (1.5 s).</summary>
        bool ExorciseHeld { get; }
    }
}
