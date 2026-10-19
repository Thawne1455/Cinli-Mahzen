using System;
using CinliMahzen.Core;
using UnityEngine;
using UnityEngine.AI;

namespace CinliMahzen.Player
{
    /// <summary>
    /// Debug dummy human (TODO 3.12, F12): wanders the house on a NavMesh so a jinn tester has a moving target. It sits on the human pawn and
    /// drives it through <see cref="HumanController.InputOverride"/> — the same input path as a player, so movement, stamina, faints and every
    /// authority rule apply unchanged; nothing here touches game state directly. Destinations are random NavMesh points inside the house
    /// bounds (<see cref="IHouseInfo"/>), picked with a <see cref="GameRandom"/>; a new one is chosen on arrival, when no path exists, or when the
    /// bot has been stuck for <c>stuckTime</c>. The bot never interacts, kicks or drops.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HumanController))]
    public sealed class DummyHumanBot : MonoBehaviour
    {
        private const int MaxCorners = 32;
        private const int PickAttempts = 6;

        [Tooltip("0 = a time-based seed.")]
        [SerializeField] private int seed;
        [SerializeField] private float arriveDistance = 0.6f;
        [Tooltip("Degrees per second the bot turns toward its next corner.")]
        [SerializeField] private float turnSpeed = 240f;
        [Tooltip("Moved less than stuckDistance in stuckTime seconds = stuck: pick another destination.")]
        [SerializeField] private float stuckTime = 2.5f;
        [SerializeField] private float stuckDistance = 0.3f;
        [Tooltip("Pause at each destination (s).")]
        [SerializeField] private float idleAtDestination = 1.5f;
        [SerializeField] private float navSampleRadius = 2f;
        [Tooltip("Colliders the runtime NavMesh is built from (Environment).")]
        [SerializeField] private LayerMask navLayers = 1 << 6;

        private readonly BotHumanInput _input = new BotHumanInput();
        private HumanController _human;
        private BotSteering _steering;
        private NavMeshPath _path;
        private GameRandom _random;
        private bool _hasNavMesh;
        private float _stuckTimer;
        private float _idleTimer;
        private Vector3 _stuckFrom;

        private void Awake()
        {
            _human = GetComponent<HumanController>();
            _steering = new BotSteering(MaxCorners, arriveDistance);
            _path = new NavMeshPath();
            _random = new GameRandom(seed != 0 ? seed : Environment.TickCount);
        }

        private void OnEnable()
        {
            _human.InputOverride = _input;
            IHouseInfo house = GameServices.House;
            Bounds bounds = house != null ? house.Bounds : new Bounds(transform.position, Vector3.one * 60f);
            bounds.Expand(2f);
            _hasNavMesh = BotNavMesh.Ensure(bounds, navLayers, transform.position);
            if (!_hasNavMesh)
                CMLog.Warn("Bot", "Dummy human: no NavMesh near the human - it will stand still");
            _steering.Clear();
            _stuckFrom = transform.position;
            _stuckTimer = 0f;
            _idleTimer = 0f;
            CMLog.Info("Bot", "Dummy human on");
        }

        private void OnDisable()
        {
            if (_human != null && ReferenceEquals(_human.InputOverride, _input))
                _human.InputOverride = null;
            CMLog.Info("Bot", "Dummy human off");
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _input.Move = Vector2.zero;
            _input.Look = Vector2.zero;
            if (!_hasNavMesh)
                return;

            if (!_steering.HasPath)
            {
                _idleTimer -= dt;
                if (_idleTimer > 0f)
                    return;
                PickDestination();
                return;
            }

            Vector3 position = transform.position;
            _steering.Step(position, transform.eulerAngles.y, turnSpeed * dt, out Vector2 move, out float yawDelta);
            _input.Move = move;
            float sensitivity = _human.LookSensitivity;
            _input.Look = sensitivity > 0f ? new Vector2(yawDelta / sensitivity, 0f) : Vector2.zero;
            if (!_steering.HasPath)
            {
                _idleTimer = idleAtDestination;
                return;
            }

            _stuckTimer += dt;
            if (_stuckTimer >= stuckTime)
            {
                bool stuck = BotSteering.HorizontalDistance(position, _stuckFrom) < stuckDistance;
                _stuckTimer = 0f;
                _stuckFrom = position;
                if (stuck)
                    _steering.Clear();
            }
        }

        private void PickDestination()
        {
            IHouseInfo house = GameServices.House;
            Bounds bounds = house != null ? house.Bounds : new Bounds(transform.position, Vector3.one * 20f);
            Vector3 from = transform.position;
            if (NavMesh.SamplePosition(from, out NavMeshHit start, navSampleRadius, NavMesh.AllAreas))
                from = start.position;
            for (int i = 0; i < PickAttempts; i++)
            {
                var candidate = new Vector3(_random.Range(bounds.min.x, bounds.max.x), from.y, _random.Range(bounds.min.z, bounds.max.z));
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, navSampleRadius, NavMesh.AllAreas))
                    continue;
                if (!NavMesh.CalculatePath(from, hit.position, NavMesh.AllAreas, _path) || _path.status != NavMeshPathStatus.PathComplete)
                    continue;
                _steering.SetPath(_path.GetCornersNonAlloc(_steering.Buffer));
                _stuckFrom = from;
                _stuckTimer = 0f;
                return;
            }
            _idleTimer = idleAtDestination; // try again shortly
        }
    }
}
