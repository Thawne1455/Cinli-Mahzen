using System.Globalization;
using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Player
{
    /// <summary>
    /// First-person human (Teknik §5.2): CharacterController movement with gravity (no jump), walk/sprint speed and stamina from
    /// GameBalanceConfig, mouse look (body yaw + head pitch). Stairs are climbed over invisible ramp colliders (slopeLimit /
    /// stepOffset below). Game state follows Request → authority → broadcast: <see cref="ApplyStatusAuthority"/> broadcasts
    /// StatusApplied and every client reacts to it in <see cref="OnStatusApplied"/> (CameraSpin180 turns the local view 180 degrees).
    /// Logic lives in plain classes: <see cref="HumanMotor"/>, <see cref="StaminaModel"/>, <see cref="YawSpin"/>.
    /// While this pawn is the local one it hands its head to the <see cref="ICameraRig"/> (SetFps); the rig owns the camera.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class HumanController : PawnBase, IStatusReceiver
    {
        /// <summary>The CameraSpin180 status turns the view by this many degrees; its duration comes with the status.</summary>
        private const float CameraSpinDegrees = 180f;

        private const string DebugStateKey = "human";

        private static HumanController s_debugOwner;

        [Header("References")]
        [Tooltip("Child transform that carries the camera; pitched by mouse look.")]
        [SerializeField] private Transform head;

        [Header("Look (feel)")]
        [Tooltip("Degrees of rotation per pixel of mouse movement.")]
        [Min(0f)] [SerializeField] private float lookSensitivity = 0.1f;
        [Range(1f, 89f)] [SerializeField] private float pitchLimit = 85f;
        [Tooltip("Lock and hide the cursor while this pawn is controlled locally (Esc releases it in the editor).")]
        [SerializeField] private bool lockCursor = true;

        [Header("Movement (feel)")]
        [Tooltip("Downward acceleration in m/s^2.")]
        [Min(0f)] [SerializeField] private float gravity = 20f;
        [Tooltip("Downward speed held while grounded so the controller stays glued to ramps and steps.")]
        [Min(0f)] [SerializeField] private float groundStickSpeed = 2f;
        [Tooltip("Steepest climbable slope in degrees. Stair ramps are 45 degrees, so this must stay above that.")]
        [Range(0f, 89f)] [SerializeField] private float slopeLimit = 50f;
        [Tooltip("Highest step the controller climbs without a ramp (m).")]
        [Min(0f)] [SerializeField] private float stepOffset = 0.4f;

        [Header("Debug")]
        [Tooltip("Duration used by CinliMahzen/Debug/Spin Human 180. Matches PD_Slipper's CameraSpin180 status duration.")]
        [Min(0f)] [SerializeField] private float debugSpinDuration = 0.15f;

        private readonly HumanMotor _motor = new HumanMotor();
        private readonly YawSpin _spin = new YawSpin();

        private CharacterController _controller;
        private StaminaModel _stamina;
        private GameBalanceConfig _config;
        private INetBridge _net;
        private IHumanInput _humanInput;

        private bool _sprinting;
        private float _pitch;
        private float _verticalVelocity;

        public HumanMotor Motor => _motor;

        public StaminaModel Stamina => _stamina;

        public bool IsSprinting => _sprinting;

        public bool IsGrounded => _controller != null && _controller.isGrounded;

        /// <summary>Pitched transform that carries the camera.</summary>
        public Transform Head => head;

        public float DebugSpinDuration => debugSpinDuration;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_debugOwner = null;
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _controller.slopeLimit = slopeLimit;
            _controller.stepOffset = stepOffset;
            if (head != null)
                _pitch = NormalizePitch(head.localEulerAngles.x);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            _net = GameServices.Net;
            _config = GameServices.Config;
            if (_net == null || _config == null)
            {
                CMLog.Error("Human", name + ": GameServices.Net / Config missing (is the bootstrap running?) — controller is inert");
                return;
            }

            _stamina = new StaminaModel(_config.StaminaMax, _config.StaminaDrain, _config.StaminaRegen, _config.StaminaRegenDelay);
            _net.Register(MsgCode.StatusApplied, OnStatusApplied);

            s_debugOwner = this;
            DebugState.Register(DebugStateKey, BuildDebugJson);
        }

        protected override void OnDisable()
        {
            if (_net != null)
            {
                _net.Unregister(MsgCode.StatusApplied, OnStatusApplied);
                _net = null;
            }
            if (s_debugOwner == this)
            {
                DebugState.Unregister(DebugStateKey);
                s_debugOwner = null;
            }
            SetCursorLocked(false);
            _spin.Cancel();
            base.OnDisable();
        }

        /// <summary>
        /// The pawn became / stopped being the played one (first Start, hotseat F1-F4, owner change): see <see cref="PawnBase.OnLocalityChanged"/>.
        /// </summary>
        protected override void OnLocalityChanged(bool isLocal)
        {
            BindInput(isLocal);
        }

        protected override void OnInputChanged()
        {
            _humanInput = Input as IHumanInput;
        }

        /// <summary>
        /// Authority only (Teknik §4.5). Validates and broadcasts StatusApplied; the effect itself is applied by
        /// <see cref="OnStatusApplied"/> on every client, the authority included.
        /// </summary>
        public void ApplyStatusAuthority(StatusType t, float duration, PlayerId source)
        {
            INetBridge net = GameServices.Net;
            Debug.Assert(net != null && net.IsAuthority, "ApplyStatusAuthority must only run on the authority");
            if (net == null || !net.IsAuthority)
                return;
            if (t == StatusType.None || float.IsNaN(duration) || duration < 0f)
            {
                CMLog.Warn("Human", "ApplyStatusAuthority rejected: " + t + " duration " + duration.ToString("0.###", CultureInfo.InvariantCulture));
                return;
            }
            if (!NetId.IsValid)
            {
                CMLog.Warn("Human", name + ": cannot apply a status without a NetId");
                return;
            }
            net.Broadcast(StatusAppliedMsg.Encode(NetId, t, duration, source));
        }

        private void OnStatusApplied(NetMsg msg)
        {
            if (!StatusAppliedMsg.TryDecode(msg, out NetId target, out StatusType status, out float duration, out PlayerId _))
            {
                CMLog.Warn("Human", "Malformed StatusApplied message ignored");
                return;
            }
            if (target != NetId)
                return;

            switch (status)
            {
                case StatusType.CameraSpin180:
                    if (IsLocallyControlled)
                        _spin.Begin(CameraSpinDegrees, duration);
                    break;
                default:
                    // Knockdown comes with a damage hit (HumanHealth starts it from DamageFlags.Knockdown); Stunned / Slowed arrive with 1.8 (actions).
                    CMLog.Info("Human", "Status " + status + " (" + duration.ToString("0.###", CultureInfo.InvariantCulture) + " s) not handled yet");
                    break;
            }
        }

        /// <summary>
        /// Fainted body state (Teknik §5.3): <see cref="HumanHealth"/> calls it on every client when HumanFainted / HumanRevived arrives.
        /// The motor locks movement and looking while fainted; a locally controlled pawn also hands the camera over
        /// (faint view while down, first person again on revive). Does nothing when the state is unchanged.
        /// </summary>
        public void SetFainted(bool fainted)
        {
            bool wasFainted = _motor.State == HumanMotorState.Fainted;
            _motor.SetFainted(fainted);
            if (wasFainted != fainted && IsLocallyControlled)
                AttachCamera();
        }

        private void Update()
        {
            if (_stamina == null || _net == null || !_controller.enabled)
                return;

            float dt = Time.deltaTime;
            double now = _net.Time;
            _motor.Tick(now);

            IHumanInput input = _humanInput;
            UpdateLook(input, dt);

            Vector2 move = Vector2.zero;
            bool wantsSprint = false;
            if (input != null && _motor.CanMove)
            {
                move = Vector2.ClampMagnitude(input.Move, 1f);
                wantsSprint = _motor.CanSprint && input.Sprint;
            }

            _sprinting = _stamina.Tick(dt, wantsSprint, move.sqrMagnitude > 0f, now);
            float speed = _sprinting ? _config.HumanSprintSpeed : _config.HumanWalkSpeed;

            Vector3 velocity = (transform.right * move.x + transform.forward * move.y) * speed;
            if (_controller.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -groundStickSpeed;
            else
                _verticalVelocity -= gravity * dt;
            velocity.y = _verticalVelocity;

            _controller.Move(velocity * dt);
        }

        private void UpdateLook(IHumanInput input, float dt)
        {
            float yawDelta = _spin.Step(dt);
            if (input != null && _motor.CanLook)
            {
                Vector2 look = input.Look;
                yawDelta += look.x * lookSensitivity;
                _pitch = Mathf.Clamp(_pitch - look.y * lookSensitivity, -pitchLimit, pitchLimit);
            }

            if (yawDelta != 0f)
                transform.Rotate(0f, yawDelta, 0f, Space.World);
            if (head != null)
                head.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void BindInput(bool local)
        {
            // Live input of the Human map (whatever a hand-placed pawn's serialized role says) when played, idle input otherwise.
            ApplyInput(local, Role.Human);
            SetCursorLocked(local && lockCursor);
            if (local)
                AttachCamera();
        }

        /// <summary>
        /// Gives the local camera to this pawn: the head (first person), or the faint view while the motor is Fainted. Runs whenever the
        /// input is (re)bound as local — first frame, owner change, hotseat switch — and on a faint / revive of the local pawn; the rig
        /// ignores a repeat of the same anchor. Update runs after every Awake, so a scene's rig is registered by now.
        /// </summary>
        private void AttachCamera()
        {
            ICameraRig rig = GameServices.Get<ICameraRig>();
            if (rig == null)
            {
                CMLog.Warn("Human", name + ": no ICameraRig registered (add the CameraRig: CinliMahzen/Setup/Add Camera Rig To Open Scene) — camera not attached");
                return;
            }
            if (_motor.State == HumanMotorState.Fainted)
            {
                rig.SetFaint(transform);
                return;
            }
            if (head == null)
            {
                CMLog.Warn("Human", name + ": no head assigned — camera not attached");
                return;
            }
            rig.SetFps(head);
        }

        // The lock is shared with the other pawn kinds and owned by one pawn (PawnBase): hotseat notifies the old and the
        // new local pawn in no fixed order, and releasing only counts for the pawn that holds it.
        private void SetCursorLocked(bool locked)
        {
            if (locked)
                AcquireCursorLock();
            else
                ReleaseCursorLock();
        }

        private static float NormalizePitch(float eulerX)
        {
            return eulerX > 180f ? eulerX - 360f : eulerX;
        }

        private string BuildDebugJson()
        {
            CultureInfo ci = CultureInfo.InvariantCulture;
            Vector3 p = transform.position;
            return "{\"pos\":[" + p.x.ToString("0.00", ci) + "," + p.y.ToString("0.00", ci) + "," + p.z.ToString("0.00", ci) + "]"
                + ",\"motor\":\"" + _motor.State + "\""
                + ",\"stamina\":" + (_stamina != null ? _stamina.Value.ToString("0.0", ci) : "0.0")
                + ",\"sprinting\":" + (_sprinting ? "true" : "false")
                + ",\"grounded\":" + (IsGrounded ? "true" : "false")
                + "}";
        }
    }
}
