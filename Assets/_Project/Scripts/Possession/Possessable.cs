using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using UnityEngine;

namespace CinliMahzen.Possession
{
    /// <summary>
    /// A furniture item / prop a jinn can enter (Teknik §6.2–6.3, TODO 1.7). Thin MonoBehaviour: it carries the
    /// <see cref="PossessableDefinition"/>, a <see cref="NetId"/> (given by <see cref="PossessionSystem"/>: level objects by sorted hierarchy
    /// path, runtime spawns through <see cref="SetRuntimeNetId"/>) and the cosmetic entering telegraph (the visual shakes until the jinn
    /// is inside). All rules live in <see cref="PossessionAuthority"/>.
    /// <para>Kick (<see cref="IKickable"/>) forwards to the authority; the human's kick (TODO 3.8) calls it on the authority.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Possessable : MonoBehaviour, INetEntity, IKickable
    {
        private static readonly List<Possessable> s_all = new List<Possessable>(64);

        [SerializeField] private PossessableDefinition definition;
        [Tooltip("Child that shakes while a jinn enters and that the orbit camera follows. Empty = this transform (only the camera follows it then; no shake).")]
        [SerializeField] private Transform visual;

        private Collider _collider;
        private NetId _netId;
        private bool _runtimeId;
        private PossessionPhase _phase = PossessionPhase.Free;
        private Vector3 _visualRestPosition;
        private Quaternion _visualRestRotation;
        private bool _shaking;

        /// <summary>Every enabled possessable in the loaded scenes. Do not modify.</summary>
        public static IReadOnlyList<Possessable> All => s_all;

        public PossessableDefinition Definition => definition;

        public NetId NetId => _netId;

        /// <summary>True for objects spawned at runtime (their id came from the authority, not from the hierarchy order).</summary>
        public bool HasRuntimeId => _runtimeId;

        /// <summary>The transform the orbit camera follows.</summary>
        public Transform CameraTarget => visual != null ? visual : transform;

        /// <summary>This machine's mirror phase of the object.</summary>
        public PossessionPhase Phase => _phase;

        public Vector3 Position => transform.position;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_all.Clear();
        }

        /// <summary>Gives a runtime-spawned object its id (authority-allocated, sent with the spawn message). Call before it is enabled.</summary>
        public void SetRuntimeNetId(NetId id)
        {
            _netId = id;
            _runtimeId = true;
        }

        /// <summary>Level id given by <see cref="PossessionSystem"/> from the sorted hierarchy path.</summary>
        internal void AssignLevelNetId(NetId id)
        {
            _netId = id;
            _runtimeId = false;
        }

        /// <summary>Closest point of the object's colliders to <paramref name="point"/> (its position when it has none).</summary>
        public Vector3 ClosestPoint(Vector3 point)
        {
            return _collider != null && _collider.enabled ? _collider.ClosestPoint(point) : transform.position;
        }

        public void OnKickedAuthority(PlayerId by)
        {
            PossessionSystem system = PossessionSystem.Instance;
            if (system != null && system.Authority != null)
                system.Authority.KickAuthority(_netId, by);
        }

        private void Awake()
        {
            _collider = GetComponentInChildren<Collider>();
            if (visual != null)
            {
                _visualRestPosition = visual.localPosition;
                _visualRestRotation = visual.localRotation;
            }
        }

        private void OnEnable()
        {
            s_all.Add(this);
            EventBus.Subscribe<PossessionChangedEvt>(OnPossessionChanged);
            PossessionSystem.Instance?.OnPossessableEnabled(this);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PossessionChangedEvt>(OnPossessionChanged);
            PossessionSystem.Instance?.OnPossessableDisabled(this);
            s_all.Remove(this);
            StopShake();
        }

        private void OnPossessionChanged(PossessionChangedEvt e)
        {
            if (e.Obj != _netId || !_netId.IsValid)
                return;
            _phase = e.State;
            if (_phase != PossessionPhase.Entering)
                StopShake();
        }

        private void Update()
        {
            if (_phase != PossessionPhase.Entering || visual == null || definition == null)
                return;

            // Cosmetic telegraph: UnityEngine.Random is fine here (CLAUDE.md rule 4).
            _shaking = true;
            float amplitude = definition.EnterShakeAmplitude;
            float angle = definition.EnterShakeAngleDeg;
            visual.localPosition = _visualRestPosition + Random.insideUnitSphere * amplitude;
            visual.localRotation = _visualRestRotation * Quaternion.Euler(Random.Range(-angle, angle), 0f, Random.Range(-angle, angle));
        }

        private void StopShake()
        {
            if (!_shaking || visual == null)
                return;
            _shaking = false;
            visual.localPosition = _visualRestPosition;
            visual.localRotation = _visualRestRotation;
        }

        /// <summary>The possessable whose <see cref="NetId"/> is <paramref name="id"/>, or null. Linear search; not for per-frame use on many objects.</summary>
        public static Possessable Find(NetId id)
        {
            for (int i = 0; i < s_all.Count; i++)
            {
                if (s_all[i]._netId == id)
                    return s_all[i];
            }
            return null;
        }

        /// <summary>
        /// The best object to possess from a view: inside <paramref name="range"/> of <paramref name="origin"/> and within
        /// <paramref name="maxAngleDeg"/> of <paramref name="forward"/>, the one closest to the view axis. Null when none qualifies.
        /// Only called when the jinn presses possess, never per frame.
        /// </summary>
        public static Possessable FindLookTarget(Vector3 origin, Vector3 forward, float range, float maxAngleDeg, bool goodJinnOnly)
        {
            Possessable best = null;
            float bestAngle = maxAngleDeg;
            float rangeSqr = range * range;
            for (int i = 0; i < s_all.Count; i++)
            {
                Possessable p = s_all[i];
                if (!p._netId.IsValid || p.definition == null || (goodJinnOnly && !p.definition.AllowGoodJinn))
                    continue;
                Vector3 to = p.ClosestPoint(origin) - origin;
                if (to.sqrMagnitude > rangeSqr)
                    continue;
                // Inside the object's collider the direction is undefined: treat it as dead ahead.
                float a = to.sqrMagnitude < 1e-6f ? 0f : Vector3.Angle(forward, to);
                if (a <= bestAngle)
                {
                    bestAngle = a;
                    best = p;
                }
            }
            return best;
        }
    }
}
