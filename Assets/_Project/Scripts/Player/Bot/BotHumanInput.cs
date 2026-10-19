using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.Player
{
    /// <summary>The dummy human bot's input (TODO 3.12): the bot writes Move / Look each frame; every button stays up.</summary>
    public sealed class BotHumanInput : IHumanInput
    {
        public Vector2 Move { get; set; }
        public Vector2 Look { get; set; }
        public bool Sprint { get; set; }
        public bool InteractHeld => false;
        public bool InteractPressed => false;
        public bool KickPressed => false;
        public bool LanternPulsePressed => false;
        public int SelectSlot => -1;
        public bool DropPressed => false;
    }
}
