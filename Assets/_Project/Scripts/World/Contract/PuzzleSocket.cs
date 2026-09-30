using UnityEngine;

namespace CinliMahzen.World
{
    public class PuzzleSocket : LevelMarker
    {
        public override MarkerKind Kind => MarkerKind.PuzzleSocket;
        protected override Color GizmoColor => Color.blue;

        [SerializeField] private PuzzleType puzzleType;

        public PuzzleType PuzzleType => puzzleType;

        public void Setup(PuzzleType type) { puzzleType = type; }
    }
}
