using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>Human FPS controls (Teknik §5.1, Oyun §7).</summary>
    public interface IHumanInput : IPawnInput
    {
        Vector2 Move { get; }
        /// <summary>Mouse delta this frame (pixels).</summary>
        Vector2 Look { get; }
        bool Sprint { get; }
        bool InteractHeld { get; }
        bool KickPressed { get; }
        bool LanternPressed { get; }
        bool UseItemPressed { get; }
        /// <summary>-1 = no change this frame, otherwise the slot index (0, 1).</summary>
        int SelectSlot { get; }
        bool DropGoldPressed { get; }
    }
}
