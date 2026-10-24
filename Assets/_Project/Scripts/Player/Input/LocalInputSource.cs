using System;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using UnityEngine;

namespace CinliMahzen.Player
{
    /// <summary>
    /// Local keyboard/mouse input (TODO A0.8). Enables exactly one gameplay map for the local player's situation:
    /// Human role → Human, jinn roles → Spirit, jinn inside an object → Possessed (driven by PossessionChangedEvt
    /// for the local player). UI and Debug maps stay enabled. Pawns read it through IHumanInput/ISpiritInput/IPossessedInput.
    /// Also the machine's <see cref="ILocalInputProvider"/> (TODO 1.3): the pawn that is currently played acquires it, the others get NullInput.
    /// </summary>
    public sealed class LocalInputSource : IHumanInput, ISpiritInput, IPossessedInput, ILocalInputProvider, IDisposable
    {
        private readonly CMInput _actions;
        private readonly Func<PlayerId> _localPlayer;
        private Role _role;
        // Which players are inside an object (any jinn, not only the local one): hotseat switches to a possessing jinn must land in Possessed.
        private readonly bool[] _insideByPlayer = new bool[256];
        // TODO 4.2: a menu (pause, settings) holds the input: every gameplay map stays off until it closes.
        private bool _menuOpen;

        /// <param name="localPlayer">Defaults to GameServices.Net/Players local player.</param>
        public LocalInputSource(Func<PlayerId> localPlayer = null)
        {
            _actions = new CMInput();
            _localPlayer = localPlayer ?? DefaultLocalPlayer;
            // Roles may have been assigned before this source existed (bootstrap hook order is not fixed), so start from the registry.
            _role = InitialLocalRole();
            _actions.UI.Enable();
            _actions.Debug.Enable();
            EventBus.Subscribe<LocalRoleChangedEvt>(OnLocalRoleChanged);
            EventBus.Subscribe<PossessionChangedEvt>(OnPossessionChanged);
            EventBus.Subscribe<MenuFocusChangedEvt>(OnMenuFocusChanged);
        }

        public InputMode Mode { get; private set; } = InputMode.None;

        /// <summary>Raw generated actions (DebugHotkeys, UI).</summary>
        public CMInput Actions => _actions;

        public bool PausePressed => _actions.UI.Pause.WasPressedThisFrame();
        public bool ScoreboardHeld => _actions.UI.Scoreboard.IsPressed();

        public void SetMode(InputMode mode)
        {
            if (mode == Mode)
                return;
            _actions.Human.Disable();
            _actions.Spirit.Disable();
            _actions.Possessed.Disable();
            switch (mode)
            {
                case InputMode.Human: _actions.Human.Enable(); break;
                case InputMode.Spirit: _actions.Spirit.Enable(); break;
                case InputMode.Possessed: _actions.Possessed.Enable(); break;
            }
            Mode = mode;
            CMLog.Info("Input", "Map -> " + mode);
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<LocalRoleChangedEvt>(OnLocalRoleChanged);
            EventBus.Unsubscribe<PossessionChangedEvt>(OnPossessionChanged);
            EventBus.Unsubscribe<MenuFocusChangedEvt>(OnMenuFocusChanged);
            _actions.Disable();
            // Generated Dispose() uses Object.Destroy, which is illegal outside play mode (EditMode tests, editor tools).
            if (Application.isPlaying)
                _actions.Dispose();
            else
                UnityEngine.Object.DestroyImmediate(_actions.asset);
        }

        private void OnLocalRoleChanged(LocalRoleChangedEvt e)
        {
            _role = e.Role;
            SetMode(CurrentMode());
        }

        private void OnPossessionChanged(PossessionChangedEvt e)
        {
            if (!e.Player.IsValid)
                return;
            _insideByPlayer[e.Player.Value] = e.State == PossessionPhase.Lurking || e.State == PossessionPhase.Charging || e.State == PossessionPhase.Recovering;
            if (e.Player == _localPlayer())
                SetMode(CurrentMode());
        }

        private void OnMenuFocusChanged(MenuFocusChangedEvt e)
        {
            _menuOpen = e.IsOpen;
            SetMode(CurrentMode());
        }

        /// <summary>
        /// The map for the local player now: none while a menu is open, else its role's map, or Possessed while a local jinn (evil or good)
        /// is inside an object.
        /// </summary>
        private InputMode CurrentMode()
        {
            if (_menuOpen)
                return InputMode.None;
            InputMode mode = ModeForRole(_role);
            PlayerId local = _localPlayer();
            if (mode == InputMode.Spirit && local.IsValid && _insideByPlayer[local.Value])
                return InputMode.Possessed;
            return mode;
        }

        public static InputMode ModeForRole(Role role)
        {
            switch (role)
            {
                case Role.Human: return InputMode.Human;
                case Role.GoodJinn:
                case Role.EvilJinn: return InputMode.Spirit;
                default: return InputMode.None;
            }
        }

        // --- ILocalInputProvider ---
        IPawnInput ILocalInputProvider.Idle => NullInput.Instance;

        IPawnInput ILocalInputProvider.AcquireLocal(Role role)
        {
            _role = role;
            SetMode(CurrentMode());
            return this;
        }

        private Role InitialLocalRole()
        {
            IPlayerRegistry players = GameServices.Players;
            return players != null ? players.GetRole(_localPlayer()) : Role.None;
        }

        private static PlayerId DefaultLocalPlayer()
        {
            IPlayerRegistry players = GameServices.Players;
            if (players != null)
                return players.LocalPlayer;
            Core.Net.INetBridge net = GameServices.Net;
            return net != null ? net.LocalPlayer : new PlayerId(0);
        }

        /// <summary>TODO 4.2: the player's look sensitivity and inverted look (GameSettings), one place for every controller.</summary>
        private static Vector2 ScaleLook(Vector2 raw)
        {
            GameSettings settings = GameServices.Get<GameSettings>();
            return settings != null ? settings.ScaleLook(raw) : raw;
        }

        // --- IHumanInput (Oyun §11 "İnsan (FPS)") ---
        Vector2 IHumanInput.Move => _actions.Human.Move.ReadValue<Vector2>();
        Vector2 IHumanInput.Look => ScaleLook(_actions.Human.Look.ReadValue<Vector2>());
        public bool Sprint => _actions.Human.Sprint.IsPressed();
        public bool InteractHeld => _actions.Human.Interact.IsPressed();
        public bool InteractPressed => _actions.Human.Interact.WasPressedThisFrame();
        public bool KickPressed => _actions.Human.Kick.WasPressedThisFrame();
        public bool LanternPulsePressed => _actions.Human.LanternPulse.WasPressedThisFrame();
        public int SelectSlot
        {
            get
            {
                if (_actions.Human.Slot1.WasPressedThisFrame()) return 0;
                if (_actions.Human.Slot2.WasPressedThisFrame()) return 1;
                if (_actions.Human.Slot3.WasPressedThisFrame()) return 2;
                return -1;
            }
        }
        public bool DropPressed => _actions.Human.Drop.WasPressedThisFrame();

        /// <summary>
        /// Shift+E (PossessAsGood) overlaps plain E (Primary): both bindings fire when E goes down with Shift held.
        /// Shift is Boost, so while Boost is held E is never treated as Primary — only PossessAsGood sees it.
        /// Public (not internal) so the EditMode test assembly can call it without InternalsVisibleTo.
        /// </summary>
        public static bool IsPrimaryAllowed(bool boostHeld) => !boostHeld;

        // --- ISpiritInput (Oyun §11 "Cin — Ruh Formu") ---
        Vector2 ISpiritInput.Move => _actions.Spirit.Move.ReadValue<Vector2>();
        Vector2 ISpiritInput.Look => ScaleLook(_actions.Spirit.Look.ReadValue<Vector2>());
        public float Vertical => _actions.Spirit.Vertical.ReadValue<float>();
        public bool Boost => _actions.Spirit.Boost.IsPressed();
        public bool PrimaryPressed => IsPrimaryAllowed(_actions.Spirit.Boost.IsPressed()) && _actions.Spirit.Primary.WasPressedThisFrame();
        public bool PrimaryHeld => IsPrimaryAllowed(_actions.Spirit.Boost.IsPressed()) && _actions.Spirit.Primary.IsPressed();
        public bool FocusHeld => _actions.Spirit.Focus.IsPressed();
        public bool PossessAsGoodPressed => _actions.Spirit.PossessAsGood.WasPressedThisFrame();
        public bool PingPressed => _actions.Spirit.Ping.WasPressedThisFrame();
        public bool ExorciseHeld => _actions.Spirit.Exorcise.IsPressed();

        // --- IPossessedInput ---
        Vector2 IPossessedInput.Look => ScaleLook(_actions.Possessed.Look.ReadValue<Vector2>());
        Vector2 IPossessedInput.Move => _actions.Possessed.Move.ReadValue<Vector2>();
        public float Turn => _actions.Possessed.Turn.ReadValue<float>();
        public bool Action1Pressed => _actions.Possessed.Action1.WasPressedThisFrame();
        public bool Action1Held => _actions.Possessed.Action1.IsPressed();
        public bool Action2Pressed => _actions.Possessed.Action2.WasPressedThisFrame();
        public bool Action2Held => _actions.Possessed.Action2.IsPressed();
        public bool ExitPressed => _actions.Possessed.Exit.WasPressedThisFrame();
    }
}
