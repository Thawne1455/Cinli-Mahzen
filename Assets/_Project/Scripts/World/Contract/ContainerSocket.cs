using UnityEngine;

namespace CinliMahzen.World
{
    public class ContainerSocket : LevelMarker
    {
        public override MarkerKind Kind => MarkerKind.ContainerSocket;
        protected override Color GizmoColor => new Color(0.8f, 0.5f, 0.2f);
    }
}
