using System;

namespace CinliMahzen.World
{
    [Flags]
    public enum RoomTag
    {
        None = 0,
        Start = 1,
        Vault = 2,
        Exit = 4,
        Corridor = 8,
        PuzzleCandidate = 16,
        Large = 32,
    }
}
