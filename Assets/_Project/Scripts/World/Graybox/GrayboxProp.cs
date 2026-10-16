namespace CinliMahzen.World.Graybox
{
    /// <summary>A garden prop. Position is in cell units: cell (cx,cz) has its centre at (cx + 0.5, cz + 0.5).</summary>
    public sealed class GrayboxProp
    {
        public GrayboxPropKind Kind { get; }
        public float CellX { get; }
        public float CellZ { get; }
        public float YawDegrees { get; }

        public GrayboxProp(GrayboxPropKind kind, float cellX, float cellZ, float yawDegrees)
        {
            Kind = kind;
            CellX = cellX;
            CellZ = cellZ;
            YawDegrees = yawDegrees;
        }

        /// <summary>The cell this prop stands in.</summary>
        public GrayboxCell Cell
        {
            get { return new GrayboxCell((int)System.Math.Floor(CellX), (int)System.Math.Floor(CellZ)); }
        }

        public override string ToString()
        {
            return Kind + " @(" + CellX + "," + CellZ + ")";
        }
    }
}
