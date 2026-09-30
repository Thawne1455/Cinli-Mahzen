using UnityEngine;

namespace CinliMahzen.World
{
    public class ExitMarker : LevelMarker
    {
        public override MarkerKind Kind => MarkerKind.Exit;
        protected override Color GizmoColor => Color.cyan;
    }
}
