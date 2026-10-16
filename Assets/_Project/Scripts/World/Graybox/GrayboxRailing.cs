namespace CinliMahzen.World.Graybox
{
    /// <summary>
    /// A low railing on the border between an upper-floor room cell and the stairwell opening.
    /// It replaces the full wall there; it is not walkable and not a door.
    /// </summary>
    public sealed class GrayboxRailing
    {
        public int Floor { get; }
        public GrayboxCell CellA { get; }
        public GrayboxCell CellB { get; }
        public GrayboxEdge Edge { get; }

        /// <summary>Throws <see cref="System.ArgumentException"/> when the two cells are not neighbours.</summary>
        public GrayboxRailing(int floor, GrayboxCell cellA, GrayboxCell cellB)
        {
            Floor = floor;
            CellA = cellA;
            CellB = cellB;
            Edge = GrayboxEdge.Between(floor, cellA, cellB);
        }

        public override string ToString()
        {
            return "Railing " + Edge;
        }
    }
}
