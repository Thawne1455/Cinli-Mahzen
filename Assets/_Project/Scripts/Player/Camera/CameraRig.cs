using System.Globalization;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using UnityEngine;
// Aliases instead of whole namespaces: UnityEngine.Rendering also has a public DebugState, which would clash with CinliMahzen.Core.DebugState.
using DepthOfField = UnityEngine.Rendering.Universal.DepthOfField;
using UniversalAdditionalCameraData = UnityEngine.Rendering.Universal.UniversalAdditionalCameraData;
using Volume = UnityEngine.Rendering.Volume;
using VolumeProfile = UnityEngine.Rendering.VolumeProfile;

namespace CinliMahzen.Player
{
    /// <summary>
    /// The local player's camera (Teknik §2, TODO 1.2), a thin wrapper around plain C# logic: <see cref="CameraBlendState"/> (0.3 s mode
    /// changes), <see cref="OrbitSolver"/> (orbit that stops at walls), <see cref="BlurPulse"/> (blur envelope) and
    /// <see cref="CullingMasks"/> (what each role sees). Lives on the scene's Main Camera and registers as <see cref="ICameraRig"/>;
    /// pawns drive it through that interface (the human calls SetFps when it becomes the local pawn).
    /// The pose is written in LateUpdate in world space, after pawns have moved in Update. Cosmetic only: nothing here is game state.
    /// Blur = a global URP Volume whose Gaussian DepthOfField override is faded in by weight + a small positional shake.
    /// Triggers: SpiritBumpedEvt (when the bumped good jinn is the local player) and a StatusApplied(VisionBlurred) whose target
    /// is the local pawn. Role changes (LocalRoleChangedEvt) swap the culling mask.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class CameraRig : MonoBehaviour, ICameraRig
    {
        private const string DebugStateKey = "camera";

        // Arbitrary offsets so the two shake axes read different slices of the Perlin noise (cosmetic).
        private const float NoiseOffsetX = 0.17f;
        private const float NoiseOffsetY = 0.53f;

        private static CameraRig s_debugOwner;

        [Header("References")]
        [Tooltip("Global URP Volume (weight 0, Gaussian DepthOfField override) driven by the blur envelope. Wired by CinliMahzen/Setup/Add Camera Rig To Open Scene.")]
        [SerializeField] private Volume blurVolume;

        [Header("Blend (feel)")]
        [Tooltip("Seconds a mode change (FPS / Orbit / Faint) takes. TODO 1.2: 0.3. 0 = cut.")]
        [Min(0f)] [SerializeField] private float blendDuration = 0.3f;

        [Header("Orbit (feel)")]
        [Tooltip("Camera distance (m) when SetOrbit is called with distance <= 0.")]
        [Min(0.1f)] [SerializeField] private float defaultOrbitDistance = 3f;
        [Tooltip("Height above the orbit target's origin that the camera looks at (m).")]
        [Min(0f)] [SerializeField] private float orbitPivotHeight = 0.8f;
        [Tooltip("Degrees of rotation per pixel of mouse movement.")]
        [Min(0f)] [SerializeField] private float orbitLookSensitivity = 0.1f;
        [Tooltip("Pitch right after SetOrbit (degrees, positive looks down so the camera sits above the pivot).")]
        [Range(-89f, 89f)] [SerializeField] private float orbitStartPitch = 20f;
        [Range(-89f, 89f)] [SerializeField] private float orbitMinPitch = -10f;
        [Range(-89f, 89f)] [SerializeField] private float orbitMaxPitch = 70f;
        [Tooltip("Radius of the sphere cast from the pivot toward the camera (m); about the near-plane half-size so walls never clip the view.")]
        [Min(0.01f)] [SerializeField] private float orbitCollisionRadius = 0.2f;
        [Tooltip("Gap kept between the camera and the wall (m).")]
        [Min(0f)] [SerializeField] private float orbitCollisionSkin = 0.05f;
        [Tooltip("The wall check never pulls the camera closer than this to the pivot (m).")]
        [Min(0f)] [SerializeField] private float orbitMinDistance = 0.4f;

        [Header("Faint (feel)")]
        [Tooltip("Pitch of the view from above (degrees, 90 = straight down).")]
        [Range(0f, 89f)] [SerializeField] private float faintPitch = 75f;
        [Tooltip("Distance above the fainted body (m); the ceiling shortens it like a wall.")]
        [Min(0.1f)] [SerializeField] private float faintDistance = 2.5f;
        [Tooltip("Height above the body's origin that the camera looks at (m).")]
        [Min(0f)] [SerializeField] private float faintPivotHeight = 0.3f;

        [Header("Blur (feel)")]
        [Tooltip("Seconds for the blur to reach full strength after the trigger.")]
        [Min(0f)] [SerializeField] private float blurFadeIn = 0.1f;
        [Tooltip("Seconds before the end over which the blur fades out.")]
        [Min(0f)] [SerializeField] private float blurFadeOut = 0.4f;
        [Tooltip("Peak positional shake (m) at full blur strength.")]
        [Min(0f)] [SerializeField] private float shakeAmplitude = 0.04f;
        [Tooltip("Shake speed (noise cycles per second).")]
        [Min(0f)] [SerializeField] private float shakeFrequency = 18f;

        private readonly CameraBlendState _blend = new CameraBlendState();

        private BlurPulse _blur;
        private Camera _camera;
        private Transform _cameraTransform;
        private Transform _anchor;
        private CameraMode _mode = CameraMode.None;
        private Role _role = Role.None;
        private IPossessedInput _orbitInput;
        private INetBridge _net;
        private int _obstacleMask;
        private float _viewDistance;
        private float _resolvedDistance;
        private float _yaw;
        private float _pitch;
        private float _shakeClock;
        private float _appliedBlurWeight = -1f;

        public CameraMode Mode => _mode;

        public Camera Camera => _camera;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_debugOwner = null;
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _cameraTransform = _camera.transform;
            _blur = new BlurPulse(blurFadeIn, blurFadeOut);
            _obstacleMask = 1 << CullingMasks.EnvironmentLayer;
            ValidateBlurSetup();
            GameServices.Register<ICameraRig>(this);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<LocalRoleChangedEvt>(OnLocalRoleChanged);
            EventBus.Subscribe<SpiritBumpedEvt>(OnSpiritBumped);

            _net = GameServices.Net;
            if (_net != null)
                _net.Register(MsgCode.StatusApplied, OnStatusApplied);
            else
                CMLog.Warn("Camera", name + ": GameServices.Net missing — StatusApplied(VisionBlurred) is not listened to");

            ApplyRole(ResolveInitialRole());

            s_debugOwner = this;
            DebugState.Register(DebugStateKey, BuildDebugJson);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<LocalRoleChangedEvt>(OnLocalRoleChanged);
            EventBus.Unsubscribe<SpiritBumpedEvt>(OnSpiritBumped);
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
            _blur.Cancel();
            ApplyBlurWeight(0f);
        }

        private void OnDestroy()
        {
            GameServices.Unregister<ICameraRig>(this);
        }

        private void OnValidate()
        {
            if (orbitMaxPitch < orbitMinPitch)
                orbitMaxPitch = orbitMinPitch;
        }

        // ---- ICameraRig ----

        public void SetFps(Transform head)
        {
            if (head == null)
            {
                CMLog.Warn("Camera", "SetFps(null) ignored");
                return;
            }
            if (_mode == CameraMode.Fps && _anchor == head)
                return;
            BeginTransition(head, CameraMode.Fps);
        }

        public void SetOrbit(Transform target, float distance)
        {
            if (target == null)
            {
                CMLog.Warn("Camera", "SetOrbit(null) ignored");
                return;
            }
            _viewDistance = distance > 0f ? distance : defaultOrbitDistance;
            if (_mode == CameraMode.Orbit && _anchor == target)
                return;

            _orbitInput = GameServices.Get<LocalInputSource>();
            if (_orbitInput == null)
                CMLog.Warn("Camera", "SetOrbit: no LocalInputSource registered — the orbit cannot be turned with the mouse");
            _yaw = target.eulerAngles.y;
            _pitch = OrbitSolver.ClampPitch(orbitStartPitch, orbitMinPitch, orbitMaxPitch);
            BeginTransition(target, CameraMode.Orbit);
        }

        public void SetFaint(Transform body)
        {
            if (body == null)
            {
                CMLog.Warn("Camera", "SetFaint(null) ignored");
                return;
            }
            if (_mode == CameraMode.Faint && _anchor == body)
                return;

            _viewDistance = faintDistance;
            _yaw = body.eulerAngles.y;
            BeginTransition(body, CameraMode.Faint);
        }

        public void TriggerBlur(float duration)
        {
            _blur.Trigger(duration);
        }

        // ---- Per-frame ----

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            float blur = _blur.Tick(dt);
            ApplyBlurWeight(blur);

            if (_mode == CameraMode.None)
                return;
            if (_anchor == null)
            {
                CMLog.Warn("Camera", name + ": the " + _mode + " anchor was destroyed — camera released");
                _mode = CameraMode.None;
                _blend.Cancel();
                return;
            }

            ComputeTargetPose(out Vector3 targetPosition, out Quaternion targetRotation);
            _blend.Evaluate(targetPosition, targetRotation, dt, out Vector3 position, out Quaternion rotation);

            if (blur > 0f && shakeAmplitude > 0f)
                position += ShakeOffset(rotation, blur, dt);

            _cameraTransform.SetPositionAndRotation(position, rotation);
        }

        private void ComputeTargetPose(out Vector3 position, out Quaternion rotation)
        {
            switch (_mode)
            {
                case CameraMode.Orbit:
                    UpdateOrbitLook();
                    SolveWalled(_anchor.position + Vector3.up * orbitPivotHeight, _yaw, _pitch, out position, out rotation);
                    break;
                case CameraMode.Faint:
                    SolveWalled(_anchor.position + Vector3.up * faintPivotHeight, _yaw, faintPitch, out position, out rotation);
                    break;
                default:
                    position = _anchor.position;
                    rotation = _anchor.rotation;
                    break;
            }
        }

        private void UpdateOrbitLook()
        {
            IPossessedInput input = _orbitInput;
            if (input == null)
                return;
            Vector2 look = input.Look;
            _yaw += look.x * orbitLookSensitivity;
            _pitch = OrbitSolver.ClampPitch(_pitch - look.y * orbitLookSensitivity, orbitMinPitch, orbitMaxPitch);
        }

        /// <summary>Orbit pose with the wall check: one sphere cast from the pivot toward the camera against the Environment layer.</summary>
        private void SolveWalled(Vector3 pivot, float yaw, float pitch, out Vector3 position, out Quaternion rotation)
        {
            float hitDistance = OrbitSolver.NoHit;
            Vector3 back = OrbitSolver.BackDirection(yaw, pitch);
            if (Physics.SphereCast(pivot, orbitCollisionRadius, back, out RaycastHit hit, _viewDistance, _obstacleMask, QueryTriggerInteraction.Ignore))
                hitDistance = hit.distance;

            OrbitSolver.Solve(pivot, yaw, pitch, _viewDistance, hitDistance, orbitCollisionSkin, orbitMinDistance, out position, out rotation);
            _resolvedDistance = Vector3.Distance(pivot, position);
        }

        private Vector3 ShakeOffset(Quaternion rotation, float intensity, float dt)
        {
            _shakeClock += dt * shakeFrequency;
            float x = Mathf.PerlinNoise(_shakeClock, NoiseOffsetX) - 0.5f;
            float y = Mathf.PerlinNoise(NoiseOffsetY, _shakeClock) - 0.5f;
            // Noise is -0.5..0.5, so 2 * amplitude * intensity is the peak offset.
            return rotation * new Vector3(x, y, 0f) * (2f * shakeAmplitude * intensity);
        }

        private void BeginTransition(Transform anchor, CameraMode mode)
        {
            // The first attach has no previous view worth blending from (the scene's editor camera), so it snaps.
            bool snap = _mode == CameraMode.None;
            _anchor = anchor;
            _mode = mode;
            if (snap)
                _blend.Cancel();
            else
                _blend.Begin(_cameraTransform.position, _cameraTransform.rotation, blendDuration);
        }

        private void ApplyBlurWeight(float intensity)
        {
            // Only touch the native Volume when the value changed (it is 0 almost all the time).
            if (blurVolume == null || intensity == _appliedBlurWeight)
                return;
            _appliedBlurWeight = intensity;
            blurVolume.weight = intensity;
        }

        // ---- Events ----

        private void OnLocalRoleChanged(LocalRoleChangedEvt e)
        {
            ApplyRole(e.Role);
        }

        private void ApplyRole(Role role)
        {
            _role = role;
            _camera.cullingMask = CullingMasks.For(role);
        }

        private static Role ResolveInitialRole()
        {
            IPlayerRegistry players = GameServices.Players;
            return players != null ? players.GetRole(players.LocalPlayer) : Role.None;
        }

        private void OnSpiritBumped(SpiritBumpedEvt e)
        {
            INetBridge net = GameServices.Net;
            GameBalanceConfig config = GameServices.Config;
            if (net == null || config == null || e.GoodJinn != net.LocalPlayer)
                return;
            TriggerBlur(config.SpiritBumpBlurTime);
        }

        private void OnStatusApplied(NetMsg msg)
        {
            if (!StatusAppliedMsg.TryDecode(msg, out NetId target, out StatusType status, out float duration, out PlayerId _))
                return;
            if (status != StatusType.VisionBlurred)
                return;

            IEntityRegistry entities = GameServices.Entities;
            if (entities != null && entities.TryGet(target, out PawnBase pawn) && pawn.IsLocallyControlled)
                TriggerBlur(duration);
        }

        // ---- Validation / debug ----

        private void ValidateBlurSetup()
        {
            const string Hint = " — run CinliMahzen/Setup/Add Camera Rig To Open Scene";
            if (blurVolume == null)
            {
                CMLog.Warn("Camera", name + ": no blur Volume assigned, the blur will only shake" + Hint);
                return;
            }
            VolumeProfile profile = blurVolume.sharedProfile;
            if (profile == null || !profile.TryGet(out DepthOfField _))
                CMLog.Warn("Camera", name + ": the blur Volume has no DepthOfField override" + Hint);
            if (!_camera.TryGetComponent(out UniversalAdditionalCameraData cameraData) || !cameraData.renderPostProcessing)
                CMLog.Warn("Camera", name + ": post-processing is off on the camera, the blur will only shake" + Hint);
        }

        private string BuildDebugJson()
        {
            CultureInfo ci = CultureInfo.InvariantCulture;
            Vector3 p = _cameraTransform != null ? _cameraTransform.position : Vector3.zero;
            return "{\"mode\":\"" + _mode + "\""
                + ",\"role\":\"" + _role + "\""
                + ",\"cullingMask\":" + (_camera != null ? _camera.cullingMask : 0)
                + ",\"blending\":" + (_blend.IsBlending ? "true" : "false")
                + ",\"blur\":" + _blur.Intensity.ToString("0.00", ci)
                + ",\"blurRemaining\":" + _blur.Remaining.ToString("0.00", ci)
                + ",\"viewDistance\":" + _viewDistance.ToString("0.00", ci)
                + ",\"resolvedDistance\":" + _resolvedDistance.ToString("0.00", ci)
                + ",\"pos\":[" + p.x.ToString("0.00", ci) + "," + p.y.ToString("0.00", ci) + "," + p.z.ToString("0.00", ci) + "]"
                + "}";
        }
    }
}
