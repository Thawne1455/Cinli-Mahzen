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
    /// </summary>
    public sealed class LocalInputSource : IHumanInput, ISpiritInput, IPossessedInput, IDisposable
    {
        private readonly CMInput _actions;
        private readonly Func<PlayerId> _localPlayer;
        private Role _role;

        /// <param name="localPlayer">Defaults to GameServices.Net/Players local player.</param>
        public LocalInputSource(Func<PlayerId> localPlayer = null)
        {
            _actions = new CMInput();
            _localPlayer = localPlayer ?? DefaultLocalPlayer;
            _actions.UI.Enable();
            _actions.Debug.Enable();
            EventBus.Subscribe<LocalRoleChangedEvt>(OnLocalRoleChanged);
            EventBus.Subscribe<PossessionChangedEvt>(OnPossessionChanged);
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
            SetMode(ModeForRole(e.Role));
        }

        private void OnPossessionChanged(PossessionChangedEvt e)
        {
            if (_role != Role.EvilJinn || e.Player != _localPlayer())
                return;
            bool inside = e.State == PossessionPhase.Lurking || e.State == PossessionPhase.Charging || e.State == PossessionPhase.Recovering;
            SetMode(inside ? InputMode.Possessed : InputMode.Spirit);
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

        private static PlayerId DefaultLocalPlayer()
        {
            IPlayerRegistry players = GameServices.Players;
            if (players != null)
                return players.LocalPlayer;
            Core.Net.INetBridge net = GameServices.Net;
            return net != null ? net.LocalPlayer : new PlayerId(0);
        }

        // --- IHumanInput ---
        Vector2 IHumanInput.Move => _actions.Human.Move.ReadValue<Vector2>();
        Vector2 IHumanInput.Look => _actions.Human.Look.ReadValue<Vector2>();
        public bool Sprint => _actions.Human.Sprint.IsPressed();
        public bool InteractHeld => _actions.Human.Interact.IsPressed();
        public bool KickPressed => _actions.Human.Kick.WasPressedThisFrame();
        public bool LanternPressed => _actions.Human.Lantern.WasPressedThisFrame();
        public bool UseItemPressed => _actions.Human.UseItem.WasPressedThisFrame();
        public int SelectSlot => _actions.Human.Slot1.WasPressedThisFrame() ? 0 : _actions.Human.Slot2.WasPressedThisFrame() ? 1 : -1;
        public bool DropGoldPressed => _actions.Human.DropGold.WasPressedThisFrame();

        // --- ISpiritInput ---
        Vector2 ISpiritInput.Move => _actions.Spirit.Move.ReadValue<Vector2>();
        Vector2 ISpiritInput.Look => _actions.Spirit.Look.ReadValue<Vector2>();
        public float Vertical => _actions.Spirit.Vertical.ReadValue<float>();
        public bool Boost => _actions.Spirit.Boost.IsPressed();
        public bool PrimaryPressed => _actions.Spirit.Primary.WasPressedThisFrame();
        public bool PrimaryHeld => _actions.Spirit.Primary.IsPressed();
        public bool PingPressed => _actions.Spirit.Ping.WasPressedThisFrame();
        public bool BlessPressed => _actions.Spirit.Bless.WasPressedThisFrame();

        // --- IPossessedInput ---
        Vector2 IPossessedInput.Look => _actions.Possessed.Look.ReadValue<Vector2>();
        Vector2 IPossessedInput.Move => _actions.Possessed.Move.ReadValue<Vector2>();
        public float Turn => _actions.Possessed.Turn.ReadValue<float>();
        public bool Action1Pressed => _actions.Possessed.Action1.WasPressedThisFrame();
        public bool Action1Held => _actions.Possessed.Action1.IsPressed();
        public bool Action2Pressed => _actions.Possessed.Action2.WasPressedThisFrame();
        public bool Action2Held => _actions.Possessed.Action2.IsPressed();
        public bool ExitPressed => _actions.Possessed.Exit.WasPressedThisFrame();
    }
}
