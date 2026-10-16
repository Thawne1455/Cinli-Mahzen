namespace CinliMahzen.World.Graybox
{
    /// <summary>One wall piece to build, produced by <see cref="GrayboxWallPlanner"/>.</summary>
    public sealed class GrayboxWallSegment
    {
        public GrayboxEdge Edge { get; }
        public GrayboxWallKind Kind { get; }

        /// <summary>The door description when <see cref="Kind"/> is <see cref="GrayboxWallKind.Door"/>; otherwise null.</summary>
        public GrayboxDoor Door { get; }

        /// <summary>Id of the room whose "Walls" group holds this piece.</summary>
        public string OwnerRoomId { get; }

        public GrayboxWallSegment(GrayboxEdge edge, GrayboxWallKind kind, GrayboxDoor door, string ownerRoomId)
        {
            Edge = edge;
            Kind = kind;
            Door = door;
            OwnerRoomId = ownerRoomId;
        }

        public override string ToString()
        {
            return Kind + " " + Edge + " owner=" + OwnerRoomId;
        }
    }
}
