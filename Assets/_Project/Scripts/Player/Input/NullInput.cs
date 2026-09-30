using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.Player
{
    /// <summary>Input for pawns that are not locally controlled (hotseat): everything idle.</summary>
    public sealed class NullInput : IHumanInput, ISpiritInput, IPossessedInput
    {
        public static readonly NullInput Instance = new NullInput();

        public Vector2 Move => Vector2.zero;
        public Vector2 Look => Vector2.zero;
        public bool Sprint => false;
        public bool InteractHeld => false;
        public bool KickPressed => false;
        public bool LanternPressed => false;
        public bool UseItemPressed => false;
        public int SelectSlot => -1;
        public bool DropGoldPressed => false;
        public float Vertical => 0f;
        public bool Boost => false;
        public bool PrimaryPressed => false;
        public bool PrimaryHeld => false;
        public bool PingPressed => false;
        public bool BlessPressed => false;
        public float Turn => 0f;
        public bool Action1Pressed => false;
        public bool Action1Held => false;
        public bool Action2Pressed => false;
        public bool Action2Held => false;
        public bool ExitPressed => false;
    }
}
