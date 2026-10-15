using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>
    /// Human FPS controls (Teknik §5.1, Oyun §11 "İnsan (FPS)").
    /// "Pressed" members are true only on the frame the button went down; "Held" members stay true while it is down.
    /// </summary>
    public interface IHumanInput : IPawnInput
    {
        /// <summary>WASD.</summary>
        Vector2 Move { get; }
        /// <summary>Mouse delta this frame (pixels).</summary>
        Vector2 Look { get; }
        /// <summary>Shift — sprint.</summary>
        bool Sprint { get; }
        /// <summary>E (held) — interact / rotate / search / dig / push / radio.</summary>
        bool InteractHeld { get; }
        /// <summary>E (down this frame) — interact.</summary>
        bool InteractPressed { get; }
        /// <summary>F — kick.</summary>
        bool KickPressed { get; }
        /// <summary>Right mouse button — Lantern Pulse (only after the Blessed Oil is collected; the lantern itself is fixed equipment).</summary>
        bool LanternPulsePressed { get; }
        /// <summary>-1 = no change this frame, otherwise the quest-item slot index (0, 1, 2 for keys 1, 2, 3).</summary>
        int SelectSlot { get; }
        /// <summary>G — drop the selected quest item.</summary>
        bool DropPressed { get; }
    }
}
