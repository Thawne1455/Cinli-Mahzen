namespace CinliMahzen.Core.Events
{
    /// <summary>Published by World when the light state of a room changes.</summary>
    public readonly struct RoomLightChangedEvt
    {
        public readonly int Room;
        public readonly RoomLightState State;

        public RoomLightChangedEvt(int room, RoomLightState state)
        {
            Room = room;
            State = state;
        }
    }
}
