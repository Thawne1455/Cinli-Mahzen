using UnityEngine;

namespace CinliMahzen.World
{
    public class JinnSpawnMarker : LevelMarker
    {
        public override MarkerKind Kind => MarkerKind.JinnSpawn;
        protected override Color GizmoColor => new Color(0.6f, 0.2f, 1f);
    }
}
