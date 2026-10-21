using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>
    /// Where a lamp hangs (Teknik 8). Every indoor room has at least one; the lamp itself (PD_Lamp, light, break/repair)
    /// is built by the light system (TODO 3.4). The room is lit while any of its lamps is on.
    /// </summary>
    public sealed class LampMarker : NetHouseMarker, ISpiritHoldTarget
    {
        private LightService _lights;

        /// <summary>Runtime: the light service (HouseService after Index). Since TODO 3.5 the good jinn's E target (light / repair).</summary>
        public void BindLights(LightService lights)
        {
            _lights = lights;
        }

        private LampState State => _lights != null ? _lights.State.StateOf(NetId) : LampState.Off;

        public string SpiritPromptKey => State == LampState.Broken ? "interact.lamp_repair" : "interact.lamp_light";

        public float SpiritHoldTime
        {
            get
            {
                GameBalanceConfig config = GameServices.Config;
                return State == LampState.Broken && config != null ? config.RepairHoldTime : 0f;
            }
        }

        public float SpiritRange
        {
            get
            {
                GameBalanceConfig config = GameServices.Config;
                return config != null ? config.LampLightRange : 8f;
            }
        }

        public bool CanSpiritInteract(PlayerId who, Role role)
        {
            return role == Role.GoodJinn && _lights != null && State != LampState.On;
        }

        public void SendRequest(PlayerId who)
        {
            GameServices.Net?.SendToAuthority(ReqLampMsg.Encode(NetId, State == LampState.Broken));
        }

        [Tooltip("RoomMarker.RoomId of the room this lamp lights.")]
        [SerializeField] private int roomId;

        public int RoomId
        {
            get { return roomId; }
        }

        /// <summary>Used by the graybox builder and tests.</summary>
        public void Configure(int newRoomId)
        {
            roomId = newRoomId;
        }

        protected override Color GizmoColor
        {
            get { return new Color(1f, 1f, 0.6f); }
        }
    }
}
