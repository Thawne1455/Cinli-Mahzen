using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>Order-dependent FNV-1a over the whole hierarchy under a level root (name, position, yaw, room id).</summary>
    public static class LevelHash
    {
        public static ulong Compute(Transform root)
        {
            ulong h = 14695981039346656037UL;
            Walk(root, root, ref h);
            return h;
        }

        private static void Walk(Transform root, Transform t, ref ulong h)
        {
            for (int i = 0; i < t.childCount; i++)
            {
                var c = t.GetChild(i);
                foreach (char ch in c.name) Mix(ref h, ch);
                Vector3 p = root.InverseTransformPoint(c.position);
                Mix(ref h, Mathf.RoundToInt(p.x * 100f));
                Mix(ref h, Mathf.RoundToInt(p.y * 100f));
                Mix(ref h, Mathf.RoundToInt(p.z * 100f));
                Mix(ref h, Mathf.RoundToInt(c.eulerAngles.y));
                var marker = c.GetComponent<LevelMarker>();
                if (marker != null) Mix(ref h, marker.RoomId);
                Walk(root, c, ref h);
            }
        }

        private static void Mix(ref ulong h, long v)
        {
            unchecked { h ^= (ulong)v; h *= 1099511628211UL; }
        }
    }
}
