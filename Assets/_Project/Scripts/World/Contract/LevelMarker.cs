using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>Base of all level markers (Spec 8.2). Draws a coloured cube + label in the Scene view.</summary>
    public abstract class LevelMarker : MonoBehaviour
    {
        [SerializeField] private int roomId;

        public int RoomId => roomId;
        public abstract MarkerKind Kind { get; }
        protected virtual Color GizmoColor => Color.white;
        protected virtual Vector3 GizmoSize => new Vector3(0.4f, 0.4f, 0.4f);

        public void Init(int room) { roomId = room; }

        private void OnDrawGizmos()
        {
            Gizmos.color = GizmoColor;
            Gizmos.DrawCube(transform.position, GizmoSize);
            Gizmos.color = Color.black;
            Gizmos.DrawRay(transform.position, transform.forward * 0.6f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, Kind + " r" + roomId);
#endif
        }
    }
}
