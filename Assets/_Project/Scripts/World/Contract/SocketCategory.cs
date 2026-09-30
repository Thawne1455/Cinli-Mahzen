using System;

namespace CinliMahzen.World
{
    [Flags]
    public enum SocketCategory
    {
        None = 0,
        WallLarge = 1,
        Floor = 2,
        Table = 4,
        WallMount = 8,
        Corner = 16,
        FloorTile = 32,
    }
}
