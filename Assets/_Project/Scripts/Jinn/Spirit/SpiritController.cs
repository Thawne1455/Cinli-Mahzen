using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Jinn
{
    /// <summary>
    /// Spirit form behaviour (Teknik §6.1, TODO 1.6): FPS flight, the good jinn's reference focus and the evil jinn's Spirit Bump.
    /// A sibling of <see cref="SpiritPawn"/> on P_Spirit — the pawn owns ownership, role, input binding, the camera and the placeholder
    /// look; this component reads <see cref="PawnBase.Input"/> as <see cref="ISpiritInput"/> while the pawn is the locally played one
    /// (<see cref="SpiritPawn.IsLocalView"/>) and does nothing otherwise. Good and evil jinn share the controller, only the speed differs.
    /// <para>
    /// Flight: mouse look turns the body (yaw) and the <see cref="SpiritPawn.Eyes"/> (pitch); WASD flies along the view, Space/Ctrl up/down,
    /// Shift boosts (<see cref="SpiritFlightModel"/>). The transform is moved directly — no collider, no Rigidbody, so walls do not
    /// stop it — and clamped into <c>IHouseInfo.Bounds</c> every frame.
    /// </para>
    /// <para>
    /// Network (Request → authority → broadcast → everyone applies; the authority side is <see cref="SpiritAuthority"/>):
    /// the local good jinn sends <c>ReqFocusReference</c> when <c>FocusHeld</c> changes; the local evil jinn sends <c>ReqSpiritBump</c>
    /// when it boosts through a focusing good jinn within <c>SpiritBumpRange</c> (<see cref="SpiritBumpRules.CanRequest"/>), then waits
    /// <c>SpiritBumpCooldown</c> before asking again. Every controller listens to <c>ReferenceFocusChanged</c> / <c>SpiritBumped</c>; the one
    /// whose pawn is the named player updates its mirror (<see cref="IsFocusingReference"/>, last bump) and publishes
    /// <see cref="ReferenceFocusChangedEvt"/> / <see cref="SpiritBumpedEvt"/> — exactly once per message. The focus is a toggle that outlives
    /// the pawn's local view (hotseat F1-F4 does not cancel it), so an evil jinn can be tested against a good jinn that is not being played.
    /// </para>
    /// Dump State section: <c>spirits</c>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpiritPawn))]
    public sealed class SpiritController : MonoBehaviour
    {
        private const string LogCategory = "Spirit";

        private static readonly List<SpiritController> s_active = new List<SpiritController>(4);

        [Header("Look (feel)")]
        [Tooltip("Degrees of rotation per pixel of mouse movement.")]
        [Min(0f)] [SerializeField] private float lookSensitivity = 0.1f;
        [Tooltip("Largest look-up / look-down angle (degrees).")]
        [Range(1f, 89f)] [SerializeField] private float pitchLimit = 85f;

        [Header("Debug")]
        [Tooltip("Distance (m) CinliMahzen/Debug/Teleport Local Spirit To Good Jinn leaves between the two jinns.")]
        [Min(0f)] [SerializeField] private float debugTeleportDistance = 1f;

        private SpiritPawn _pawn;
        private INetBridge _net;
        private GameBalanceConfig _config;
        private SpiritFlightModel _flight;

        // Cached once so Register/Unregister never allocate a new delegate.
        private Action<NetMsg> _onFocusChanged;
        private Action<NetMsg> _onBumped;

        private bool _wasLocal;
        private bool _boosting;
        private bool _focusHeldPrev;
        private bool _focusingReference;
        private bool _hasBumped;
        private double _lastBumpTime;
        private double _bumpCooldownUntil;
        private float _pitch;
        private bool _warnedBounds;

        /// <summary>Every enabled spirit controller (hotseat: the two evil jinns and the good jinn). Do not modify.</summary>
        public static IReadOnlyList<SpiritController> Active => s_active;

        /// <summary>The pawn this controller flies.</summary>
        public SpiritPawn Pawn => _pawn;

        public Role Role => _pawn.Role;

        public PlayerId Owner => _pawn.Owner;

        /// <summary>
        /// Mirror of the authority's flag: this (good) jinn is focusing a puzzle reference. Updated by <c>ReferenceFocusChanged</c> on every
        /// machine; false for any other role.
        /// </summary>
        public bool IsFocusingReference => _focusingReference;

        /// <summary>The local Shift (Boost) state, only while this pawn is played; false otherwise.</summary>
        public bool IsBoosting => _boosting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_active.Clear();
        }

        private void Awake()
        {
            _pawn = GetComponent<SpiritPawn>();
            _onFocusChanged = OnReferenceFocusChanged;
            _onBumped = OnSpiritBumped;
        }

        private void OnEnable()
        {
            s_active.Add(this);

            _net = GameServices.Net;
            _config = GameServices.Config;
            if (_net == null || _config == null)
            {
                CMLog.Error(LogCategory, name + ": GameServices.Net / Config missing (is the bootstrap running?) — controller is inert");
                _net = null;
                return;
            }

            _flight = SpiritFlightModel.FromConfig(_config);
            _wasLocal = false;
            _boosting = false;
            _focusHeldPrev = false;
            _focusingReference = false;
            _hasBumped = false;
            _bumpCooldownUntil = 0d;

            _net.Register(MsgCode.ReferenceFocusChanged, _onFocusChanged);
            _net.Register(MsgCode.SpiritBumped, _onBumped);
        }

        private void OnDisable()
        {
            if (_net != null)
            {
                _net.Unregister(MsgCode.ReferenceFocusChanged, _onFocusChanged);
                _net.Unregister(MsgCode.SpiritBumped, _onBumped);
                _net = null;
            }
            _boosting = false;
            s_active.Remove(this);
        }

        private void Update()
        {
            if (_net == null)
                return;

            // The pawn's locality flow has bound the input by the time IsLocalView changes, so both are read together.
            bool local = _pawn.IsLocalView;
            ISpiritInput input = local ? _pawn.Input as ISpiritInput : null;
            if (local != _wasLocal)
            {
                _wasLocal = local;
                if (local)
                    BeginLocalControl(input);
            }
            if (input == null)
            {
                _boosting = false;
                return;
            }

            _boosting = input.Boost;
            UpdateLook(input);
            UpdateFlight(input, Time.deltaTime);
            UpdateFocusRequest(input);
            UpdateSpiritBump();
        }

        // ---- Local control ----

        private void BeginLocalControl(ISpiritInput input)
        {
            Transform eyes = _pawn.Eyes;
            _pitch = eyes != transform ? Mathf.Clamp(NormalizePitch(eyes.localEulerAngles.x), -pitchLimit, pitchLimit) : 0f;
            // A button that is already down when control starts is not a new press.
            _focusHeldPrev = input != null && input.FocusHeld;
        }

        private void UpdateLook(ISpiritInput input)
        {
            Vector2 look = input.Look;
            float yawDelta = look.x * lookSensitivity;
            if (yawDelta != 0f)
                transform.Rotate(0f, yawDelta, 0f, Space.World);

            // Without a separate Eyes child the camera sits on the root: pitching would tip the whole pawn, so only yaw applies.
            Transform eyes = _pawn.Eyes;
            if (eyes == transform)
                return;
            _pitch = Mathf.Clamp(_pitch - look.y * lookSensitivity, -pitchLimit, pitchLimit);
            eyes.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void UpdateFlight(ISpiritInput input, float dt)
        {
            Transform eyes = _pawn.Eyes;
            Vector3 velocity = _flight.Velocity(_pawn.Role, input.Move, input.Vertical, _boosting, eyes.right, eyes.forward);

            Vector3 position = transform.position;
            Vector3 next = ClampToHouse(position + velocity * dt);
            if (next != position)
                transform.position = next;
        }

        /// <summary>
        /// The house bounds are looked up here, not cached: the real HouseService registers over the stub when the house is built, which can be
        /// after the pawns exist. The lookup is one dictionary read per frame on the local pawn only.
        /// </summary>
        private Vector3 ClampToHouse(Vector3 position)
        {
            IHouseInfo house = GameServices.House;
            if (house == null)
                return position;

            Bounds bounds = house.Bounds;
            if (bounds.size == Vector3.zero)
            {
                if (!_warnedBounds)
                {
                    _warnedBounds = true;
                    CMLog.Warn(LogCategory, name + ": IHouseInfo.Bounds is empty — the spirit is not clamped");
                }
                return position;
            }
            return SpiritFlightModel.ClampToBounds(position, bounds);
        }

        // ---- Reference focus (good jinn) ----

        private void UpdateFocusRequest(ISpiritInput input)
        {
            if (_pawn.Role != Role.GoodJinn)
                return;

            bool held = input.FocusHeld;
            if (held == _focusHeldPrev)
                return;
            _focusHeldPrev = held;
            // TODO 3.5: refuse to start without energy, drain FocusReferenceCost per second while focusing and stop at 0.
            RequestFocusReference(held);
        }

        /// <summary>
        /// Asks the authority to start / stop this (good) jinn's reference focus. The mirror (<see cref="IsFocusingReference"/>) changes
        /// when the authority's <c>ReferenceFocusChanged</c> arrives. Used by the focus button and by the debug menu.
        /// </summary>
        public void RequestFocusReference(bool focusing)
        {
            if (_net == null)
                return;
            _net.SendToAuthority(ReqFocusReferenceMsg.Encode(focusing));
        }

        // ---- Spirit Bump (evil jinn) ----

        private void UpdateSpiritBump()
        {
            if (_pawn.Role != Role.EvilJinn || !_boosting)
                return;

            double now = _net.Time;
            if (now < _bumpCooldownUntil)
                return;
            if (!TryFindGoodJinn(this, out SpiritController good))
                return;

            float distance = Vector3.Distance(transform.position, good.transform.position);
            if (!SpiritBumpRules.CanRequest(true, _boosting, good.IsFocusingReference, distance, _config.SpiritBumpRange, now, _bumpCooldownUntil))
                return;

            // Local throttle: the request is not repeated every frame while the jinn is still inside the range. The authority's own
            // cooldown (same length) is the real rule; an accepted bump re-syncs this one in OnSpiritBumped.
            _bumpCooldownUntil = now + _config.SpiritBumpCooldown;
            _net.SendToAuthority(ReqSpiritBumpMsg.Encode(good.Owner));
        }

        /// <summary>
        /// Debug (<c>CinliMahzen/Debug/Request Spirit Bump</c>): sends <c>ReqSpiritBump</c> for the scene's good jinn without the Boost,
        /// focus, range and cooldown pre-checks, so the authority's own rules can be tried. Returns false when there is no good jinn pawn.
        /// </summary>
        public bool DebugRequestSpiritBump()
        {
            if (_net == null || !TryFindGoodJinn(this, out SpiritController good))
                return false;
            _net.SendToAuthority(ReqSpiritBumpMsg.Encode(good.Owner));
            return true;
        }

        /// <summary>
        /// Debug (<c>CinliMahzen/Debug/Teleport Local Spirit To Good Jinn</c>): puts this spirit <see cref="debugTeleportDistance"/> metres
        /// beside <paramref name="other"/> (inside the house bounds), facing it, with a level view.
        /// </summary>
        public bool DebugTeleportBeside(SpiritController other)
        {
            if (other == null || other == this)
                return false;

            transform.position = ClampToHouse(other.transform.position + Vector3.right * debugTeleportDistance);

            Vector3 toOther = other.transform.position - transform.position;
            toOther.y = 0f;
            if (toOther.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(toOther);
            _pitch = 0f;
            Transform eyes = _pawn.Eyes;
            if (eyes != transform)
                eyes.localRotation = Quaternion.identity;
            return true;
        }

        // ---- Lookups (debug, authority) ----

        /// <summary>The first enabled controller of a good jinn other than <paramref name="exclude"/>. Loops a few entries, no allocation.</summary>
        public static bool TryFindGoodJinn(SpiritController exclude, out SpiritController good)
        {
            for (int i = 0; i < s_active.Count; i++)
            {
                SpiritController c = s_active[i];
                if (c != exclude && c._pawn.Role == Role.GoodJinn)
                {
                    good = c;
                    return true;
                }
            }
            good = null;
            return false;
        }

        /// <summary>The spirit controller of the locally played player, if that player has one.</summary>
        public static bool TryFindLocal(out SpiritController local)
        {
            for (int i = 0; i < s_active.Count; i++)
            {
                if (s_active[i]._pawn.IsLocallyControlled)
                {
                    local = s_active[i];
                    return true;
                }
            }
            local = null;
            return false;
        }

        /// <summary>World position of <paramref name="owner"/>'s spirit pawn, or null. The position source of <see cref="SpiritAuthority"/>.</summary>
        internal static Vector3? FindPosition(PlayerId owner)
        {
            for (int i = 0; i < s_active.Count; i++)
            {
                if (s_active[i]._pawn.Owner == owner)
                    return s_active[i].transform.position;
            }
            return null;
        }

        // ---- Broadcasts (every machine, authority included) ----

        private void OnReferenceFocusChanged(NetMsg msg)
        {
            if (!ReferenceFocusChangedMsg.TryDecode(msg, out PlayerId player, out bool focusing))
            {
                if (IsLogLeader)
                    CMLog.Warn(LogCategory, "Malformed ReferenceFocusChanged ignored");
                return;
            }
            if (player != _pawn.Owner)
                return;

            _focusingReference = focusing;
            EventBus.Publish(new ReferenceFocusChangedEvt(player, focusing));
        }

        private void OnSpiritBumped(NetMsg msg)
        {
            if (!SpiritBumpedMsg.TryDecode(msg, out PlayerId evilJinn, out PlayerId goodJinn))
            {
                if (IsLogLeader)
                    CMLog.Warn(LogCategory, "Malformed SpiritBumped ignored");
                return;
            }

            bool isEvil = evilJinn == _pawn.Owner;
            bool isGood = goodJinn == _pawn.Owner;
            if (!isEvil && !isGood)
                return;

            double now = _net.Time;
            _hasBumped = true;
            _lastBumpTime = now;
            if (isEvil)
                _bumpCooldownUntil = now + _config.SpiritBumpCooldown;
            if (isGood)
                EventBus.Publish(new SpiritBumpedEvt(evilJinn, goodJinn));
        }

        // Several controllers receive the same broadcast; only the first one reports a malformed message.
        private bool IsLogLeader => s_active.Count > 0 && s_active[0] == this;

        // ---- Debug state ----

        /// <summary>The <c>spirits</c> Dump State section: one entry per enabled spirit. Built at dump time only.</summary>
        internal static string BuildDebugJson()
        {
            CultureInfo ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder(256);
            sb.Append('[');
            for (int i = 0; i < s_active.Count; i++)
            {
                SpiritController c = s_active[i];
                Vector3 p = c.transform.position;
                if (i > 0)
                    sb.Append(',');
                sb.Append("{\"id\":").Append(DebugState.Quote(c._pawn.Owner.ToString()))
                  .Append(",\"net\":").Append(DebugState.Quote(c._pawn.NetId.ToString()))
                  .Append(",\"role\":").Append(DebugState.Quote(c._pawn.Role.ToString()))
                  .Append(",\"local\":").Append(c._pawn.IsLocallyControlled ? "true" : "false")
                  .Append(",\"pos\":[").Append(p.x.ToString("0.00", ci)).Append(',').Append(p.y.ToString("0.00", ci))
                  .Append(',').Append(p.z.ToString("0.00", ci)).Append(']')
                  .Append(",\"focusing\":").Append(c._focusingReference ? "true" : "false")
                  .Append(",\"boostOn\":").Append(c._boosting ? "true" : "false")
                  .Append(",\"lastBump\":").Append(c._hasBumped ? c._lastBumpTime.ToString("0.00", ci) : "null")
                  .Append('}');
            }
            sb.Append(']');
            return sb.ToString();
        }

        private static float NormalizePitch(float eulerX)
        {
            return eulerX > 180f ? eulerX - 360f : eulerX;
        }
    }
}
