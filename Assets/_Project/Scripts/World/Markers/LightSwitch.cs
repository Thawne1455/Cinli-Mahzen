using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>
    /// The wall switch of a room (Teknik 8): the human walks to it to toggle the room's lamps. Every indoor room has at least one, next to a door.
    /// Since TODO 3.4 it is also the interactable: E → InteractionAuthority → <see cref="ExecuteAuthority"/> →
    /// <see cref="LightService.ToggleRoomAuthority"/> (HouseService binds the service and gives the marker an Interactable collider at runtime).
    /// </summary>
    public sealed class LightSwitch : NetHouseMarker, IInteractable
    {
        private const string PromptKeyValue = "interact.light_switch";

        private LightService _lights;
        [Tooltip("RoomMarker.RoomId of the room this switch controls.")]
        [SerializeField] private int roomId;

        public int RoomId
        {
            get { return roomId; }
        }

        public string PromptKey => PromptKeyValue;

        public float HoldTime => 0f;

        public float NoiseOnInteract => 0.1f;

        /// <summary>Runtime: the light service this switch drives (HouseService after Index).</summary>
        public void BindLights(LightService lights)
        {
            _lights = lights;
        }

        public bool CanInteract(PlayerId who, Role role)
        {
            return role == Role.Human && _lights != null;
        }

        public bool ValidateAuthority(PlayerId who)
        {
            return _lights != null;
        }

        public void ExecuteAuthority(PlayerId who)
        {
            Debug.Assert(GameServices.Net.IsAuthority);
            _lights.ToggleRoomAuthority(roomId);
        }

        /// <summary>Used by the graybox builder and tests.</summary>
        public void Configure(int newRoomId)
        {
            roomId = newRoomId;
        }

        protected override Color GizmoColor
        {
            get { return new Color(0.3f, 1f, 0.3f); }
        }
    }
}
