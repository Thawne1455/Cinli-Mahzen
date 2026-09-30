using UnityEngine;

namespace CinliMahzen.World
{
    public class DecorSocket : LevelMarker
    {
        public override MarkerKind Kind => MarkerKind.DecorSocket;
        protected override Color GizmoColor => Color.gray;
    }
}
