using System.Collections.Generic;
using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>
    /// One per room (Teknik 8). The room volume is the union of the trigger <see cref="BoxCollider"/>s on this GameObject
    /// (an L-shaped room has one box per rectangle). The colliders exist only for <see cref="IRoomService.RoomOf"/>: they are
    /// triggers on a non-physical layer and are read as axis-aligned boxes (do not rotate a room marker).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class RoomMarker : HouseMarker
    {
        private static readonly List<BoxCollider> ColliderScratch = new List<BoxCollider>();

        [Tooltip("Unique room id; the value IRoomService.RoomOf returns. -1 is reserved for 'outside'.")]
        [SerializeField] private int roomId;
        [Tooltip("Stable lower-case key (\"hall\"); the Loc key of the room name is \"room.\" + key.")]
        [SerializeField] private string roomKey = string.Empty;
        [Tooltip("-1 cellar, 0 garden/ground floor, 1 first floor, 2 attic.")]
        [SerializeField] private int floor;
        [Tooltip("Outdoor rooms (the garden) need no lamp or light switch.")]
        [SerializeField] private bool isOutdoor;

        public int RoomId
        {
            get { return roomId; }
        }

        public string RoomKey
        {
            get { return roomKey; }
        }

        public int Floor
        {
            get { return floor; }
        }

        public bool IsOutdoor
        {
            get { return isOutdoor; }
        }

        /// <summary>Loc key of the room name, e.g. "room.hall".</summary>
        public string NameKey
        {
            get { return HouseContract.RoomNameKeyPrefix + roomKey; }
        }

        /// <summary>Used by the graybox builder and tests.</summary>
        public void Configure(int newRoomId, string newRoomKey, int newFloor, bool newIsOutdoor)
        {
            roomId = newRoomId;
            roomKey = newRoomKey ?? string.Empty;
            floor = newFloor;
            isOutdoor = newIsOutdoor;
        }

        /// <summary>
        /// Appends the world-space axis-aligned bounds of every enabled <see cref="BoxCollider"/> on this GameObject.
        /// Pure transform math: works on prefab assets and in edit mode (no physics scene needed).
        /// </summary>
        public void CollectWorldBoxes(List<Bounds> boxes)
        {
            GetComponents(ColliderScratch);
            for (int i = 0; i < ColliderScratch.Count; i++)
            {
                BoxCollider box = ColliderScratch[i];
                if (box != null && box.enabled)
                {
                    boxes.Add(WorldBounds(box));
                }
            }
            ColliderScratch.Clear();
        }

        private static Bounds WorldBounds(BoxCollider box)
        {
            Transform t = box.transform;
            Vector3 center = box.center;
            Vector3 half = box.size * 0.5f;
            Bounds result = new Bounds();
            bool first = true;
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    for (int sz = -1; sz <= 1; sz += 2)
                    {
                        Vector3 corner = t.TransformPoint(center + new Vector3(sx * half.x, sy * half.y, sz * half.z));
                        if (first)
                        {
                            result = new Bounds(corner, Vector3.zero);
                            first = false;
                        }
                        else
                        {
                            result.Encapsulate(corner);
                        }
                    }
                }
            }
            return result;
        }

        private void Reset()
        {
            BoxCollider box = GetComponent<BoxCollider>();
            if (box != null)
            {
                box.isTrigger = true;
            }
        }

        protected override Color GizmoColor
        {
            get { return new Color(0.2f, 0.9f, 0.9f, 0.5f); }
        }

#if UNITY_EDITOR
        protected override void OnDrawGizmos()
        {
            Gizmos.color = GizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            BoxCollider[] boxes = GetComponents<BoxCollider>();
            for (int i = 0; i < boxes.Length; i++)
            {
                Gizmos.DrawWireCube(boxes[i].center, boxes[i].size);
            }
            Gizmos.matrix = Matrix4x4.identity;
        }
#endif
    }
}
