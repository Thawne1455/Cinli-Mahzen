using UnityEngine;

namespace CinliMahzen.World
{
    public class GoldSpawnMarker : LevelMarker
    {
        public override MarkerKind Kind => MarkerKind.GoldSpawn;
        protected override Color GizmoColor => Color.yellow;
    }
}
