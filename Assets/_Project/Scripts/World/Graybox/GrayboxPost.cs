namespace CinliMahzen.World.Graybox
{
    /// <summary>A pillar or fence post at a grid vertex (corner point). Vertex (vx,vz) is at world (vx*4, vz*4).</summary>
    public readonly struct GrayboxPost
    {
        public readonly int Floor;
        public readonly int VertexX;
        public readonly int VertexZ;
        public readonly GrayboxPostKind Kind;

        public GrayboxPost(int floor, int vertexX, int vertexZ, GrayboxPostKind kind)
        {
            Floor = floor;
            VertexX = vertexX;
            VertexZ = vertexZ;
            Kind = kind;
        }

        public override string ToString()
        {
            return Kind + " f" + Floor + " v(" + VertexX + "," + VertexZ + ")";
        }
    }
}
