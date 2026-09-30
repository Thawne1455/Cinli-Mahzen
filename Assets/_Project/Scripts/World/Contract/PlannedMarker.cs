namespace CinliMahzen.World
{
    /// <summary>Unity-free marker description. Positions are level-local metres, Y up.</summary>
    public sealed class PlannedMarker
    {
        public MarkerKind Kind;
        public int RoomId;
        public float X, Y, Z;
        public float YawDeg;
        public SocketCategory Category;
        public PuzzleType Puzzle;
        public bool WallFacing;
    }
}
