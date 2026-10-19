using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using UnityEngine;

namespace CinliMahzen.Visibility
{
    /// <summary>
    /// Local, cosmetic side of Oyun §12 (TODO 3.9). Camera culling already hides whole layers per role (CullingMasks, CM.Player); this service
    /// adds the distance rules of <see cref="VisibilityRules"/> for the locally played player, every frame:
    /// <list type="bullet">
    /// <item>evil jinn spirit bodies: a local good jinn sees them only within GoodSightSpiritRange; nobody sees a body while its jinn is inside an object
    /// (renderers are toggled with <c>Renderer.enabled</c>, so SpiritPawn's own SetActive of the visual is left alone);</item>
    /// <item>lurk / charge markers over possessed objects (layer XRayOutline, which the human's camera never renders): pale while lurking,
    /// red while charging, at the good jinn's ranges; always for the evil team;</item>
    /// <item>the good jinn's focus glow while it reads a reference (layer SpiritOnly: both jinn kinds see it, the human does not);</item>
    /// <item>the human's cold breath: <see cref="ColdBreathChangedEvt"/> when an evil spirit outside any object comes within ColdBreathRange.</item>
    /// </list>
    /// Nothing here changes game state, so it needs no authority. Pawns are found with FindObjectsByType only when the roster can have changed
    /// (roles, local player, round start, a cached pawn destroyed), never every frame. Created hidden and persistent by <see cref="VisibilityBootstrap"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VisibilityService : MonoBehaviour
    {
        private const int SpiritOnlyLayer = 13;   // Teknik §2 layer table (CullingMasks in CM.Player holds the same numbers)
        private const int XRayOutlineLayer = 18;

        [SerializeField] private float markerHeight = 0.6f;
        [SerializeField] private float markerSize = 0.25f;
        [SerializeField] private Color lurkColor = new Color(0.8f, 0.8f, 1f, 0.6f);
        [SerializeField] private Color chargeColor = new Color(1f, 0.15f, 0.1f, 1f);
        [SerializeField] private float focusGlowSize = 1.6f;
        [SerializeField] private Color focusGlowColor = new Color(0.4f, 1f, 0.95f, 0.5f);

        private sealed class PawnSlot
        {
            public PawnBase Pawn;
            public Renderer[] Renderers;
            public bool Shown = true;
            public GameObject FocusGlow;
        }

        private struct Inside
        {
            public NetId Obj;
            public PossessionPhase Phase;
            public Transform Target;
        }

        private readonly List<PawnSlot> _pawns = new List<PawnSlot>(4);
        private readonly Dictionary<byte, Inside> _inside = new Dictionary<byte, Inside>(4);
        private readonly Dictionary<byte, Renderer> _markers = new Dictionary<byte, Renderer>(4);
        private readonly Dictionary<byte, bool> _focusing = new Dictionary<byte, bool>(2);

        private IPlayerRegistry _players;
        private VisibilityRules _rules;
        private bool _dirty = true;
        private bool _coldBreath;

        private System.Action<PossessionChangedEvt> _onPossession;
        private System.Action<ReferenceFocusChangedEvt> _onFocus;
        private System.Action<RoundStartedEvt> _onRound;

        public VisibilityRules Rules => _rules;

        public bool IsColdBreathActive => _coldBreath;

        private void Awake()
        {
            _players = GameServices.Players;
            GameBalanceConfig config = GameServices.Config;
            if (_players == null || config == null)
            {
                CMLog.Warn("Visibility", "GameServices.Players / Config missing — visibility rules off");
                enabled = false;
                return;
            }
            _rules = VisibilityRules.FromConfig(config);
            _onPossession = OnPossessionChanged;
            _onFocus = OnFocusChanged;
            _onRound = OnRoundStarted;
        }

        private void OnEnable()
        {
            if (_players == null)
                return;
            _players.RolesChanged += MarkDirty;
            _players.LocalPlayerChanged += OnLocalPlayerChanged;
            EventBus.Subscribe(_onPossession);
            EventBus.Subscribe(_onFocus);
            EventBus.Subscribe(_onRound);
            _dirty = true;
        }

        private void OnDisable()
        {
            if (_players == null)
                return;
            _players.RolesChanged -= MarkDirty;
            _players.LocalPlayerChanged -= OnLocalPlayerChanged;
            EventBus.Unsubscribe(_onPossession);
            EventBus.Unsubscribe(_onFocus);
            EventBus.Unsubscribe(_onRound);
            SetColdBreath(false);
        }

        private void MarkDirty()
        {
            _dirty = true;
        }

        private void OnLocalPlayerChanged(PlayerId player)
        {
            _dirty = true;
        }

        private void OnRoundStarted(RoundStartedEvt evt)
        {
            _inside.Clear();
            _focusing.Clear();
            _dirty = true;
        }

        private void OnPossessionChanged(PossessionChangedEvt evt)
        {
            byte key = evt.Player.Value;
            if (!VisibilityRules.IsInside(evt.State))
            {
                _inside.Remove(key);
                return;
            }
            Transform target = null;
            IEntityRegistry entities = GameServices.Entities;
            if (entities != null && entities.TryGet(evt.Obj, out Component c) && c != null)
                target = c.transform;
            _inside[key] = new Inside { Obj = evt.Obj, Phase = evt.State, Target = target };
        }

        private void OnFocusChanged(ReferenceFocusChangedEvt evt)
        {
            _focusing[evt.Jinn.Value] = evt.IsFocusing;
        }

        private void Update()
        {
            if (_dirty)
                RefreshPawns();

            PlayerId local = _players.LocalPlayer;
            Role viewer = _players.GetRole(local);
            PawnSlot localSlot = null;
            for (int i = 0; i < _pawns.Count; i++)
            {
                if (_pawns[i].Pawn == null)
                {
                    _dirty = true; // destroyed: re-find next frame
                    continue;
                }
                if (_pawns[i].Pawn.Owner == local)
                    localSlot = _pawns[i];
            }
            Vector3 eye = localSlot != null ? localSlot.Pawn.transform.position : Vector3.zero;
            bool hasEye = localSlot != null;

            float nearestFreeEvil = float.PositiveInfinity;
            for (int i = 0; i < _pawns.Count; i++)
            {
                PawnSlot slot = _pawns[i];
                PawnBase pawn = slot.Pawn;
                if (pawn == null)
                    continue;
                if (pawn.Role == Role.EvilJinn)
                {
                    bool inside = _inside.ContainsKey(pawn.Owner.Value);
                    float distance = hasEye ? Vector3.Distance(eye, pawn.transform.position) : float.PositiveInfinity;
                    if (!inside && slot != localSlot && distance < nearestFreeEvil)
                        nearestFreeEvil = distance;
                    bool show = !inside && (slot == localSlot || viewer != Role.GoodJinn || _rules.SeesEvilSpirit(viewer, distance));
                    SetShown(slot, show);
                }
                else if (pawn.Role == Role.GoodJinn)
                {
                    bool focusing = _focusing.TryGetValue(pawn.Owner.Value, out bool f) && f;
                    SetFocusGlow(slot, focusing && slot != localSlot);
                }
            }

            UpdateMarkers(viewer, eye, hasEye);
            SetColdBreath(viewer == Role.Human && hasEye && _rules.ColdBreath(nearestFreeEvil));
        }

        private void UpdateMarkers(Role viewer, Vector3 eye, bool hasEye)
        {
            foreach (KeyValuePair<byte, Renderer> pair in _markers)
            {
                if (pair.Value != null && !_inside.ContainsKey(pair.Key))
                    pair.Value.gameObject.SetActive(false);
            }
            foreach (KeyValuePair<byte, Inside> pair in _inside)
            {
                Inside inside = pair.Value;
                LurkMarker marker = LurkMarker.None;
                if (inside.Target != null)
                {
                    float distance = hasEye ? Vector3.Distance(eye, inside.Target.position) : float.PositiveInfinity;
                    marker = _rules.MarkerFor(viewer, inside.Phase, distance);
                }
                Renderer r = MarkerOf(pair.Key, marker != LurkMarker.None);
                if (r == null)
                    continue;
                r.gameObject.SetActive(marker != LurkMarker.None);
                if (marker == LurkMarker.None)
                    continue;
                r.transform.position = inside.Target.position + Vector3.up * markerHeight;
                r.sharedMaterial.color = marker == LurkMarker.Charging ? chargeColor : lurkColor;
            }
        }

        private Renderer MarkerOf(byte player, bool create)
        {
            if (_markers.TryGetValue(player, out Renderer r) && r != null)
                return r;
            if (!create)
                return null;
            GameObject go = MakeSphere("LurkMarker_" + player, XRayOutlineLayer, markerSize, lurkColor);
            go.transform.SetParent(transform, false);
            r = go.GetComponent<Renderer>(); // once per evil jinn, then cached
            _markers[player] = r;
            return r;
        }

        private void SetShown(PawnSlot slot, bool shown)
        {
            if (slot.Shown == shown)
                return;
            slot.Shown = shown;
            for (int i = 0; i < slot.Renderers.Length; i++)
            {
                if (slot.Renderers[i] != null && slot.Renderers[i].gameObject != slot.FocusGlow)
                    slot.Renderers[i].enabled = shown;
            }
        }

        private void SetFocusGlow(PawnSlot slot, bool on)
        {
            if (slot.FocusGlow == null)
            {
                if (!on)
                    return;
                slot.FocusGlow = MakeSphere("FocusGlow", SpiritOnlyLayer, focusGlowSize, focusGlowColor);
                slot.FocusGlow.transform.SetParent(slot.Pawn.transform, false);
            }
            if (slot.FocusGlow.activeSelf != on)
                slot.FocusGlow.SetActive(on);
        }

        private void SetColdBreath(bool active)
        {
            if (_coldBreath == active)
                return;
            _coldBreath = active;
            EventBus.Publish(new ColdBreathChangedEvt(active));
        }

        private void RefreshPawns()
        {
            _dirty = false;
            for (int i = 0; i < _pawns.Count; i++)
            {
                PawnSlot old = _pawns[i];
                if (old.Pawn != null)
                    SetShown(old, true);
                if (old.FocusGlow != null)
                    Destroy(old.FocusGlow);
            }
            _pawns.Clear();
            PawnBase[] found = FindObjectsByType<PawnBase>(FindObjectsSortMode.None);
            for (int i = 0; i < found.Length; i++)
            {
                _pawns.Add(new PawnSlot
                {
                    Pawn = found[i],
                    Renderers = found[i].GetComponentsInChildren<Renderer>(true),
                });
            }
        }

        private static GameObject MakeSphere(string name, int layer, float size, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.layer = layer;
            Destroy(go.GetComponent<Collider>());
            go.transform.localScale = Vector3.one * size;
            Renderer r = go.GetComponent<Renderer>();
            r.sharedMaterial = new Material(r.sharedMaterial) { color = color };
            if (r.sharedMaterial.HasProperty("_EmissionColor"))
            {
                r.sharedMaterial.EnableKeyword("_EMISSION");
                r.sharedMaterial.SetColor("_EmissionColor", color * 2f);
            }
            return go;
        }

        /// <summary>Dump State section "visibility".</summary>
        internal string BuildDebugJson()
        {
            var sb = new System.Text.StringBuilder(128);
            sb.Append("{\"pawns\":").Append(_pawns.Count)
              .Append(",\"inside\":").Append(_inside.Count)
              .Append(",\"coldBreath\":").Append(_coldBreath ? "true" : "false")
              .Append('}');
            return sb.ToString();
        }
    }
}
