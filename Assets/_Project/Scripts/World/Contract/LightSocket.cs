using UnityEngine;

namespace CinliMahzen.World
{
    public class LightSocket : LevelMarker
    {
        public override MarkerKind Kind => MarkerKind.LightSocket;
        protected override Color GizmoColor => new Color(1f, 0.6f, 0.1f);

        [SerializeField] private bool wallMounted;

        public bool WallMounted => wallMounted;

        public void Setup(bool wall) { wallMounted = wall; }
    }
}
