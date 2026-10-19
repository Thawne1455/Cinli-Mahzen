using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>
    /// Base of every house marker component (Teknik 8). House systems recognise the hand-built house ONLY through these
    /// markers (CLAUDE.md rule 11): never by hierarchy names. A marker is a plain data holder; <see cref="HouseService"/>
    /// reads all of them once in <see cref="HouseService.Index"/>. Gizmos are editor-only.
    /// </summary>
    public abstract class HouseMarker : MonoBehaviour
    {
        private const float GizmoRadius = 0.15f;
        private const float GizmoArrowLength = 0.6f;

        /// <summary>Scene view colour of this marker kind.</summary>
        protected virtual Color GizmoColor
        {
            get { return Color.white; }
        }

#if UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {
            Gizmos.color = GizmoColor;
            Vector3 position = transform.position;
            Gizmos.DrawWireSphere(position, GizmoRadius);
            Gizmos.DrawRay(position, transform.forward * GizmoArrowLength);
        }
#endif
    }
}
