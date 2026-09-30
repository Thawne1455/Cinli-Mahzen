using System;

namespace CinliMahzen.World
{
    [Serializable]
    public class LevelGenSettings
    {
        public int MinRooms = 8;
        public int MaxRooms = 12;
        public float CellSize = 4f;
        public int GridSize = 7;
        public float WallHeight = 3f;
        public float ExtraEdgeChance = 0.2f;
        public float MinExitPathFromVault = 35f;
        public float MinJinnDistanceFromHuman = 25f;
        public int MinRuneRoomHops = 2;
        public int MinPossessableSockets = 30;
        public int MaxPossessableSockets = 50;
        public int MinContainerSockets = 10;
        public int MaxContainerSockets = 16;
        public int MaxRetries = 5;
        public int FallbackSeed = 12345;
    }
}
