using UnityEngine;

namespace CinliMahzen.World
{
    public class HumanSpawnMarker : LevelMarker
    {
        public override MarkerKind Kind => MarkerKind.HumanSpawn;
        protected override Color GizmoColor => Color.green;
    }
}
