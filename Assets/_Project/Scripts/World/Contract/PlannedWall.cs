namespace CinliMahzen.World
{
    /// <summary>
    /// One cell-edge. Horizontal=false: line x = X*cell spanning z in [Z, Z+1]*cell.
    /// Horizontal=true: line z = Z*cell spanning x in [X, X+1]*cell.
    /// </summary>
    public struct PlannedWall
    {
        public int X, Z;
        public bool Horizontal;
        public WallKind Kind;
    }
}
