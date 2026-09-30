using UnityEngine;

namespace CinliMahzen.World
{
    public class PossessableSocket : LevelMarker
    {
        public override MarkerKind Kind => MarkerKind.PossessableSocket;
        protected override Color GizmoColor => new Color(1f, 0.4f, 0.7f);

        [SerializeField] private SocketCategory category;
        [SerializeField] private bool wallFacing;

        public SocketCategory Category => category;
        public bool WallFacing => wallFacing;

        public void Setup(SocketCategory cat, bool wall) { category = cat; wallFacing = wall; }
    }
}
