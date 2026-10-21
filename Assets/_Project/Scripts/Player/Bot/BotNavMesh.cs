using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CinliMahzen.Player
{
    /// <summary>
    /// Runtime NavMesh for the dummy bot (TODO 3.12). The house is hand-built and has no baked NavMesh (Unity 6 bakes through the AI Navigation
    /// package, which this project does not use), so the first bot builds one from the colliders inside the house bounds with the built-in
    /// <see cref="NavMeshBuilder"/>. Built once per play session; a scene that already has NavMesh data under the human is used as is.
    /// </summary>
    public static class BotNavMesh
    {
        private static NavMeshDataInstance s_instance;
        private static bool s_built;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_instance = default;
            s_built = false;
        }

        /// <summary>True when a NavMesh exists near <paramref name="probe"/>, building one over <paramref name="bounds"/> first if needed.</summary>
        public static bool Ensure(Bounds bounds, int layerMask, Vector3 probe)
        {
            if (NavMesh.SamplePosition(probe, out _, 2f, NavMesh.AllAreas))
                return true;
            if (s_built)
                return false;
            s_built = true;

            var sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(bounds, layerMask, NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), sources);
            if (sources.Count == 0)
            {
                Core.CMLog.Warn("Bot", "No colliders inside the house bounds - no NavMesh for the dummy bot");
                return false;
            }
            NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);
            NavMeshData data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            if (data == null)
                return false;
            s_instance = NavMesh.AddNavMeshData(data);
            Core.CMLog.Info("Bot", "Runtime NavMesh built from " + sources.Count + " colliders");
            return NavMesh.SamplePosition(probe, out _, 2f, NavMesh.AllAreas);
        }
    }
}
