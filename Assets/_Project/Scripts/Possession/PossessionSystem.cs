using System;
using System.Collections.Generic;
using System.Text;
using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Possession
{
    /// <summary>
    /// Possession as a scene object (Teknik §6.2, TODO 1.7): the thin MonoBehaviour around <see cref="PossessionAuthority"/> (rules,
    /// authority only) and <see cref="PossessionMirror"/> (every machine). One per Game / Sandbox scene
    /// (<c>CinliMahzen/Setup/Add Possession System To Open Scene</c>). It is the scene's <see cref="IPossessionQuery"/> and adds the
    /// "possession" section to Dump State.
    /// <list type="bullet">
    /// <item>NetIds: in Start every <see cref="Possessable"/> of the loaded scenes without a runtime id gets
    /// <see cref="LevelNetIdBase"/> + its rank in the ordinal order of hierarchy paths (scene name, then names with sibling indices), so every
    /// machine with the same scene assigns the same ids. Runtime spawns bring their own id.</item>
    /// <item>Per frame: <see cref="PossessionAuthority.TickAuthority"/> on the authority (entering timer, lit-room ejection).</item>
    /// <item>World facts (<see cref="IPossessionWorld"/>): roles from <see cref="IPlayerRegistry"/>, match phase from <see cref="IMatchInfo"/>,
    /// light from <see cref="IRoomService"/>, distances from the requester's pawn (found by owner on request, never per frame).</item>
    /// </list>
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder)]
    public sealed class PossessionSystem : MonoBehaviour, IPossessionWorld, IPossessionQuery
    {
        /// <summary>After HouseService (-100), before ordinary scripts.</summary>
        public const int ExecutionOrder = -50;

        /// <summary>First level NetId of possessables (house markers use 1000 up; 40000..59999 is left to props).</summary>
        public const int LevelNetIdBase = 40000;

        private const string DebugStateKey = "possession";
        private const string LogCategory = "Possession";

        private readonly List<Possessable> _sorted = new List<Possessable>(64);

        private INetBridge _net;
        private IPlayerRegistry _players;
        private PossessionAuthority _authority;
        private PossessionMirror _mirror;
        private bool _started;

        /// <summary>The active system of the scene, or null.</summary>
        public static PossessionSystem Instance { get; private set; }

        /// <summary>Rules and authority entry points (call the ...Authority methods on the authority only). Null when inert.</summary>
        public PossessionAuthority Authority => _authority;

        /// <summary>This machine's view of every possession. Null when inert.</summary>
        public PossessionMirror Mirror => _mirror;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            _net = GameServices.Net;
            _players = GameServices.Players;
            GameBalanceConfig config = GameServices.Config;
            if (_net == null || _players == null || config == null)
            {
                CMLog.Error(LogCategory, name + ": GameServices.Net / Players / Config missing (is the bootstrap running?) - possession is inert");
                enabled = false;
                return;
            }

            _authority = PossessionAuthority.FromConfig(_net, this, config);
            _mirror = new PossessionMirror(_net);
        }

        private void OnEnable()
        {
            if (_authority == null)
                return;
            if (Instance != null && Instance != this)
                CMLog.Warn(LogCategory, "More than one PossessionSystem; " + name + " takes over");
            Instance = this;

            _authority.Attach();
            _mirror.Attach();
            GameServices.Register<IPossessionQuery>(this);
            DebugState.Register(DebugStateKey, _mirror.BuildDebugJson);

            if (_started)
                RegisterAll();
        }

        private void Start()
        {
            if (_authority == null)
                return;
            AssignLevelNetIds();
            RegisterAll();
            _started = true;
        }

        private void OnDisable()
        {
            if (_authority == null)
                return;
            _authority.Detach();
            _mirror.Detach();
            GameServices.Unregister<IPossessionQuery>(this);
            if (Instance == this)
            {
                DebugState.Unregister(DebugStateKey);
                Instance = null;
            }
        }

        private void Update()
        {
            if (_net.IsAuthority)
                _authority.TickAuthority(_net.Time);
        }

        // ---- possessable registry ----

        internal void OnPossessableEnabled(Possessable p)
        {
            if (!_started || _authority == null)
                return;
            if (!p.NetId.IsValid)
            {
                CMLog.Warn(LogCategory, p.name + " was enabled after the scene start without a runtime NetId - it cannot be possessed");
                return;
            }
            RegisterOne(p);
        }

        internal void OnPossessableDisabled(Possessable p)
        {
            if (!_started || _authority == null || !p.NetId.IsValid)
                return;
            _authority.Unregister(p.NetId);
            GameServices.Entities?.Unregister(p);
        }

        private void RegisterAll()
        {
            IReadOnlyList<Possessable> all = Possessable.All;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].NetId.IsValid)
                    RegisterOne(all[i]);
            }
        }

        private void RegisterOne(Possessable p)
        {
            _authority.Register(p.NetId, PossessableTraits.From(p.Definition));
            GameServices.Entities?.Register(p);
        }

        private void AssignLevelNetIds()
        {
            _sorted.Clear();
            IReadOnlyList<Possessable> all = Possessable.All;
            for (int i = 0; i < all.Count; i++)
            {
                if (!all[i].HasRuntimeId)
                    _sorted.Add(all[i]);
            }

            var keys = new string[_sorted.Count];
            var items = _sorted.ToArray();
            for (int i = 0; i < items.Length; i++)
                keys[i] = HierarchyKey(items[i].transform);
            Array.Sort(keys, items, StringComparer.Ordinal);

            int max = NetId.RuntimeMin - LevelNetIdBase;
            if (items.Length > max)
                CMLog.Error(LogCategory, items.Length + " possessables exceed the level id range (" + max + ")");

            for (int i = 0; i < items.Length && i < max; i++)
                items[i].AssignLevelNetId(new NetId(LevelNetIdBase + i));

            CMLog.Info(LogCategory, items.Length + " possessables got level NetIds from " + LevelNetIdBase);
        }

        /// <summary>"Scene/Root[0]/Child[3]/...": names plus sibling indices, so duplicate names still sort the same everywhere.</summary>
        public static string HierarchyKey(Transform t)
        {
            var sb = new StringBuilder();
            Transform current = t;
            var stack = new List<Transform>(8);
            while (current != null)
            {
                stack.Add(current);
                current = current.parent;
            }
            sb.Append(t.gameObject.scene.name);
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                sb.Append('/').Append(stack[i].name).Append('[').Append(stack[i].GetSiblingIndex()).Append(']');
            }
            return sb.ToString();
        }

        // ---- IPossessionWorld ----

        public bool IsPlaying
        {
            get
            {
                IMatchInfo match = GameServices.Match;
                return match != null && match.State == MatchState.Playing;
            }
        }

        public bool JinnsAwake
        {
            get
            {
                IMatchInfo match = GameServices.Match;
                return match != null && match.JinnsAwake;
            }
        }

        public Role GetRole(PlayerId player)
        {
            return _players.GetRole(player);
        }

        /// <summary>Exorcism stuns arrive with TODO 3.5; kick locks are kept by the authority itself.</summary>
        public bool IsPossessionLocked(PlayerId player, double now)
        {
            return false;
        }

        public RoomLightState LightOfObject(NetId obj)
        {
            Possessable p = Possessable.Find(obj);
            if (p == null)
                return RoomLightState.Off;
            IRoomService rooms = GameServices.Get<IRoomService>();
            return rooms != null ? rooms.LightOf(rooms.RoomOf(p.Position)) : RoomLightState.Off;
        }

        public bool TryGetDistance(PlayerId player, NetId obj, out float distance)
        {
            distance = 0f;
            Possessable p = Possessable.Find(obj);
            PawnBase pawn = FindPawn(player);
            if (p == null || pawn == null)
                return false;
            Vector3 from = pawn.transform.position;
            distance = Vector3.Distance(from, p.ClosestPoint(from));
            return true;
        }

        // ---- IPossessionQuery ----

        public bool IsPossessed(NetId obj)
        {
            return _mirror != null && _mirror.IsPossessed(obj);
        }

        public PlayerId PossessorOf(NetId obj)
        {
            return _mirror != null ? _mirror.PossessorOf(obj) : PlayerId.None;
        }

        public void GetPossessedInCone(Vector3 origin, Vector3 dir, float range, float angleDeg, List<NetId> results)
        {
            results.Clear();
            if (_mirror == null)
                return;
            float rangeSqr = range * range;
            IReadOnlyList<Possessable> all = Possessable.All;
            for (int i = 0; i < all.Count; i++)
            {
                Possessable p = all[i];
                if (!p.NetId.IsValid || !_mirror.IsPossessed(p.NetId))
                    continue;
                Vector3 to = p.Position - origin;
                if (to.sqrMagnitude > rangeSqr)
                    continue;
                if (to.sqrMagnitude > 1e-6f && Vector3.Angle(dir, to) > angleDeg)
                    continue;
                results.Add(p.NetId);
            }
        }

        /// <summary>Scene search over PawnBase by owner. Runs on a request only, never per frame.</summary>
        public static PawnBase FindPawn(PlayerId who)
        {
            PawnBase[] pawns = FindObjectsByType<PawnBase>(FindObjectsSortMode.None);
            for (int i = 0; i < pawns.Length; i++)
            {
                if (pawns[i].Owner == who)
                    return pawns[i];
            }
            return null;
        }
    }
}
