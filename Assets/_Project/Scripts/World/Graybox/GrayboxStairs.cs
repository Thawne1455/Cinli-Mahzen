using System.Collections.Generic;

namespace CinliMahzen.World.Graybox
{
    /// <summary>
    /// One straight staircase from the ground floor to the upper floor, <see cref="RunCells"/> cells long and one cell wide.
    /// Below, the stair cells belong to a ground-floor room (the stairwell). Above, the same cells are an open shaft
    /// (no floor tiles); the stairs arrive in the cell just beyond the last stair cell.
    ///
    /// The model is KayKit <c>Env_Stairs</c>, used unscaled: one cell of run (4 m) climbs exactly one floor (4 m, 45 degrees,
    /// measured in Unity). Unrotated, the prefab climbs towards its local -Z with its pivot at the TOP end: put the
    /// pivot at the centre of <see cref="ArrivalEdge"/> and rotate by <see cref="ModelYawDegrees"/>.
    /// </summary>
    public sealed class GrayboxStairs
    {
        /// <summary>Length of the run in cells. One Env_Stairs: 4 m run for a 4 m rise (45 degrees), no scaling.</summary>
        public const int RunCells = 1;

        /// <summary>
        /// Env_Stairs climbs towards local -Z when unrotated (its top step is at the pivot, the entrance is at local +Z).
        /// Turning the instance by 180 degrees on top of <see cref="GrayboxDirectionUtil.YawDegrees"/> makes it climb
        /// towards the stairs direction.
        /// </summary>
        public const float ModelYawOffsetDegrees = 180f;

        /// <summary>First (lowest) stair cell.</summary>
        public GrayboxCell Start { get; }

        /// <summary>The direction the stairs climb towards.</summary>
        public GrayboxDirection Direction { get; }

        /// <summary>The stair cells, lowest first.</summary>
        public IReadOnlyList<GrayboxCell> Cells { get; }

        /// <summary>Highest stair cell.</summary>
        public GrayboxCell LastCell
        {
            get { return Cells[Cells.Count - 1]; }
        }

        /// <summary>Ground-floor cell you walk in from (just before <see cref="Start"/>).</summary>
        public GrayboxCell Entrance
        {
            get { return Start.Step(Direction.Opposite()); }
        }

        /// <summary>Upper-floor cell you arrive in (just after <see cref="LastCell"/>).</summary>
        public GrayboxCell Arrival
        {
            get { return LastCell.Step(Direction); }
        }

        /// <summary>Ground-floor border between <see cref="Entrance"/> and <see cref="Start"/>: the stage gate sits here.</summary>
        public GrayboxEdge EntranceEdge
        {
            get { return GrayboxEdge.Between(GrayboxLayout.GroundFloor, Entrance, Start); }
        }

        /// <summary>
        /// Yaw (degrees about +Y) to give the Env_Stairs instance so that it climbs towards <see cref="Direction"/>:
        /// North 180, East 270, South 0, West 90. Place the instance pivot at the centre of <see cref="ArrivalEdge"/>.
        /// </summary>
        public float ModelYawDegrees
        {
            get { return (Direction.YawDegrees() + ModelYawOffsetDegrees) % 360f; }
        }

        /// <summary>Upper-floor border between <see cref="LastCell"/> and <see cref="Arrival"/>: kept open, no wall.</summary>
        public GrayboxEdge ArrivalEdge
        {
            get { return GrayboxEdge.Between(GrayboxLayout.UpperFloor, LastCell, Arrival); }
        }

        public GrayboxStairs(GrayboxCell start, GrayboxDirection direction)
        {
            Start = start;
            Direction = direction;
            var cells = new List<GrayboxCell>(RunCells);
            for (int i = 0; i < RunCells; i++)
            {
                cells.Add(start.Step(direction, i));
            }
            Cells = cells;
        }

        public bool Contains(GrayboxCell cell)
        {
            for (int i = 0; i < Cells.Count; i++)
            {
                if (Cells[i].Equals(cell))
                {
                    return true;
                }
            }
            return false;
        }

        public override string ToString()
        {
            return "Stairs " + Start + " " + Direction;
        }
    }
}
