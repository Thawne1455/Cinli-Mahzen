namespace CinliMahzen.World.Graybox
{
    /// <summary>An opening in the wall between two neighbouring cells of one floor.</summary>
    public sealed class GrayboxDoor
    {
        public int Floor { get; }
        public GrayboxCell CellA { get; }
        public GrayboxCell CellB { get; }
        public GrayboxDoorKind Kind { get; }
        public GrayboxEdge Edge { get; }

        /// <summary>Throws <see cref="System.ArgumentException"/> when the two cells are not neighbours.</summary>
        public GrayboxDoor(int floor, GrayboxCell cellA, GrayboxCell cellB, GrayboxDoorKind kind)
        {
            Floor = floor;
            CellA = cellA;
            CellB = cellB;
            Kind = kind;
            Edge = GrayboxEdge.Between(floor, cellA, cellB);
        }

        public override string ToString()
        {
            return Kind + " door " + Edge;
        }
    }
}
