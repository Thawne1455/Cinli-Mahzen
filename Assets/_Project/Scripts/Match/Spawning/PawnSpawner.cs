using System.Collections.Generic;
using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.Match
{
    /// <summary>
    /// Creates the round's pawns (Teknik §3, TODO 1.3): one per player with a role, from <see cref="PawnSpawnPlanner"/>'s plan —
    /// P_Human for the human, P_Spirit for either jinn, NetId 1 + player slot, placed at the house's spawn points. It talks to the
    /// pawns only through <see cref="PawnBase"/>, so CM.Match needs neither CM.Player nor CM.Jinn; the prefabs are assigned in the scene
    /// (CinliMahzen/Setup/Add Pawn Spawner To Open Scene).
    /// <para>
    /// When it spawns: on every <see cref="IPlayerRegistry.RolesChanged"/> (the MatchController assigns each round's roles; in Sandbox the hotseat
    /// bootstrap assigns round 0 once the scene has loaded), and in <c>Start</c> if the registry holds roles that no RolesChanged has spawned yet.
    /// <see cref="SpawnAll"/> always destroys the previous pawns first, so it is idempotent. Pawns are configured while inactive
    /// (instantiated under a disabled holder), then activated, so they register in the EntityRegistry once with their final NetId and a
    /// CharacterController never starts at the prefab's pose.
    /// </para>
    /// <para>
    /// Hand-placed pawns (the old Sandbox P_Human): the spawner owns the NetIds it plans. A scene pawn it did not create that holds one of
    /// those NetIds is removed with a warning, because two pawns of one player would fight over camera and input. Pawns with other NetIds
    /// are left alone. The setup menu also deletes hand-placed P_Human / P_Spirit instances from the scene it adds the spawner to.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PawnSpawner : MonoBehaviour
    {
        private const string PawnRootName = "Pawns";
        private const string HolderName = "_InactiveHolder";

        [Tooltip("Prefab of the human pawn (P_Human).")]
        [SerializeField] private PawnBase humanPrefab;
        [Tooltip("Prefab of both jinn kinds (P_Spirit).")]
        [SerializeField] private PawnBase spiritPrefab;

        private readonly List<PawnBase> _spawned = new List<PawnBase>(RoleRotation.DefaultPlayerCount);
        private Transform _pawnRoot;
        private Transform _holder;
        private IPlayerRegistry _registry;

        /// <summary>The pawns this spawner created, in player order.</summary>
        public IReadOnlyList<PawnBase> Spawned => _spawned;

        private void Awake()
        {
            _pawnRoot = CreateChild(PawnRootName, true);
            _holder = CreateChild(HolderName, false);
        }

        private void OnEnable()
        {
            _registry = GameServices.Players;
            if (_registry == null)
            {
                CMLog.Warn("Spawn", name + ": no IPlayerRegistry registered (is the bootstrap running?) — nothing will be spawned");
                return;
            }
            _registry.RolesChanged += OnRolesChanged;
        }

        private void Start()
        {
            // Spawning waits for Start so every scene Awake (CameraRig, services) has run before a pawn binds to them.
            // Roles that arrived before Start (RolesChanged right after the scene loaded) have been spawned already: do not redo it.
            if (_registry != null && _spawned.Count == 0 && PawnSpawnPlanner.HasAnyRole(_registry.Players))
                SpawnAll(_registry);
        }

        private void OnDisable()
        {
            if (_registry != null)
            {
                _registry.RolesChanged -= OnRolesChanged;
                _registry = null;
            }
        }

        /// <summary>Destroys the previous pawns, then creates one per player that has a role. Safe to call repeatedly.</summary>
        public void SpawnAll(IPlayerRegistry registry)
        {
            if (registry == null)
            {
                CMLog.Warn("Spawn", "SpawnAll: no registry");
                return;
            }
            if (humanPrefab == null || spiritPrefab == null)
            {
                CMLog.Error("Spawn", name + ": humanPrefab / spiritPrefab not assigned — run CinliMahzen/Setup/Add Pawn Spawner To Open Scene");
                return;
            }
            if (_pawnRoot == null || _holder == null)
            {
                CMLog.Error("Spawn", name + ": SpawnAll before Awake");
                return;
            }

            Despawn();

            IHouseInfo house = GameServices.House;
            Vector3 humanSpawn = house != null ? house.HumanSpawn : Vector3.zero;
            List<PawnSpawnEntry> plan = PawnSpawnPlanner.Plan(registry.Players, humanSpawn, house?.JinnSpawns);
            RemoveCollidingScenePawns(plan);

            for (int i = 0; i < plan.Count; i++)
                _spawned.Add(SpawnOne(plan[i]));

            CMLog.Info("Spawn", "Spawned " + _spawned.Count + " pawn(s)" + Describe(plan));
        }

        /// <summary>
        /// Destroys every pawn this spawner created. They are deactivated first so their NetIds leave the EntityRegistry at once
        /// (Destroy only takes effect at the end of the frame, and the next spawn reuses the same ids).
        /// </summary>
        public void Despawn()
        {
            for (int i = _spawned.Count - 1; i >= 0; i--)
                Remove(_spawned[i]);
            _spawned.Clear();
        }

        private PawnBase SpawnOne(in PawnSpawnEntry entry)
        {
            PawnBase prefab = entry.Kind == PawnKind.Human ? humanPrefab : spiritPrefab;
            PawnBase pawn = Instantiate(prefab, entry.Position, Quaternion.identity, _holder);
            pawn.name = prefab.name + "_" + entry.Owner;
            pawn.SetNetId(entry.NetId);
            pawn.SetOwner(entry.Owner, entry.Role);
            // Leaving the disabled holder activates the pawn: Awake/OnEnable run now, Start (first locality bind) before its first Update.
            pawn.transform.SetParent(_pawnRoot, true);
            return pawn;
        }

        private void RemoveCollidingScenePawns(List<PawnSpawnEntry> plan)
        {
            PawnBase[] scenePawns = FindObjectsByType<PawnBase>(FindObjectsSortMode.None);
            for (int i = 0; i < scenePawns.Length; i++)
            {
                PawnBase pawn = scenePawns[i];
                if (!UsesPlannedId(plan, pawn.NetId))
                    continue;
                CMLog.Warn("Spawn", "Removed hand-placed pawn '" + pawn.name + "' (" + pawn.NetId + ", " + pawn.Owner
                    + "): the spawner owns that NetId. Run CinliMahzen/Setup/Add Pawn Spawner To Open Scene to delete it from the scene.");
                Remove(pawn);
            }
        }

        private static bool UsesPlannedId(List<PawnSpawnEntry> plan, NetId id)
        {
            for (int i = 0; i < plan.Count; i++)
            {
                if (plan[i].NetId == id)
                    return true;
            }
            return false;
        }

        private static void Remove(PawnBase pawn)
        {
            if (pawn == null)
                return;
            pawn.gameObject.SetActive(false);
            Destroy(pawn.gameObject);
        }

        private void OnRolesChanged()
        {
            if (_registry != null)
                SpawnAll(_registry);
        }

        private Transform CreateChild(string childName, bool active)
        {
            var go = new GameObject(childName);
            go.SetActive(active);
            go.transform.SetParent(transform, false);
            return go.transform;
        }

        private static string Describe(List<PawnSpawnEntry> plan)
        {
            if (plan.Count == 0)
                return string.Empty;
            var sb = new System.Text.StringBuilder(": ");
            for (int i = 0; i < plan.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append(plan[i].Owner).Append('=').Append(plan[i].Role).Append('/').Append(plan[i].NetId);
            }
            return sb.ToString();
        }
    }
}
