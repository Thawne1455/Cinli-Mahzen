namespace CinliMahzen.Core
{
    /// <summary>Light state of a room (Oyun §7, Teknik §4.8). Off = dark (moonlight only). Wire value — do not reorder.</summary>
    public enum RoomLightState : byte
    {
        Off = 0,
        On = 1,
    }
}
