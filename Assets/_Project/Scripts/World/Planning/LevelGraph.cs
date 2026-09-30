using System;
using System.Collections.Generic;

namespace CinliMahzen.World
{
    /// <summary>Room-graph queries shared by the planner and the validator. Path length = sum of room-centre distances.</summary>
    public static class LevelGraph
    {
        public static float[] PathDistances(IReadOnlyList<PlannedRoom> rooms, int from)
        {
            int n = rooms.Count;
            var dist = new float[n];
            var done = new bool[n];
            for (int i = 0; i < n; i++) dist[i] = float.PositiveInfinity;
            dist[from] = 0f;
            for (int iter = 0; iter < n; iter++)
            {
                int best = -1;
                for (int i = 0; i < n; i++)
                    if (!done[i] && (best < 0 || dist[i] < dist[best])) best = i;
                if (best < 0 || float.IsPositiveInfinity(dist[best])) break;
                done[best] = true;
                var a = rooms[best];
                for (int k = 0; k < a.Neighbors.Count; k++)
                {
                    int j = a.Neighbors[k];
                    var b = rooms[j];
                    float dx = a.CenterX - b.CenterX, dz = a.CenterZ - b.CenterZ;
                    float d = dist[best] + (float)Math.Sqrt(dx * dx + dz * dz);
                    if (d < dist[j]) dist[j] = d;
                }
            }
            return dist;
        }

        public static int[] Hops(IReadOnlyList<PlannedRoom> rooms, int from)
        {
            int n = rooms.Count;
            var hops = new int[n];
            for (int i = 0; i < n; i++) hops[i] = -1;
            var queue = new Queue<int>();
            hops[from] = 0;
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                int c = queue.Dequeue();
                var nb = rooms[c].Neighbors;
                for (int k = 0; k < nb.Count; k++)
                {
                    if (hops[nb[k]] >= 0) continue;
                    hops[nb[k]] = hops[c] + 1;
                    queue.Enqueue(nb[k]);
                }
            }
            return hops;
        }

        public static int EdgeCount(IReadOnlyList<PlannedRoom> rooms)
        {
            int sum = 0;
            for (int i = 0; i < rooms.Count; i++) sum += rooms[i].Neighbors.Count;
            return sum / 2;
        }
    }
}
