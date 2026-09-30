using UnityEngine;

namespace CinliMahzen.World
{
    public class VaultMarker : LevelMarker
    {
        public override MarkerKind Kind => MarkerKind.Vault;
        protected override Color GizmoColor => Color.red;

        [SerializeField] private Transform doorTransform;

        /// <summary>Where the sealed gate sits.</summary>
        public Transform DoorTransform => doorTransform;

        public void SetDoor(Transform door) { doorTransform = door; }
    }
}
