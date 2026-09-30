using CinliMahzen.Core;
using CinliMahzen.Player;
using UnityEngine;

namespace CinliMahzen.DebugTools
{
    /// <summary>F1-F12 (Teknik §12.1) → DebugCommands. Reads the CMInput Debug map.</summary>
    public sealed class DebugHotkeys : MonoBehaviour
    {
        private LocalInputSource _input;

        private void Start()
        {
            _input = GameServices.Get<LocalInputSource>();
            if (_input == null)
                CMLog.Warn("Debug", "DebugHotkeys: no LocalInputSource service — hotkeys disabled");
        }

        private void Update()
        {
            if (_input == null)
                return;
            CMInput.DebugActions d = _input.Actions.Debug;
            if (d.Player1.WasPressedThisFrame()) DebugCommands.SwitchToPlayer(0);
            if (d.Player2.WasPressedThisFrame()) DebugCommands.SwitchToPlayer(1);
            if (d.Player3.WasPressedThisFrame()) DebugCommands.SwitchToPlayer(2);
            if (d.Player4.WasPressedThisFrame()) DebugCommands.SwitchToPlayer(3);
            if (d.ImmortalHuman.WasPressedThisFrame()) DebugCommands.ToggleImmortalHuman();
            if (d.InfiniteEnergy.WasPressedThisFrame()) DebugCommands.ToggleInfiniteEnergy();
            if (d.RevealAll.WasPressedThisFrame()) DebugCommands.ToggleRevealAll();
            if (d.NewRound.WasPressedThisFrame()) DebugCommands.RestartRound();
            if (d.Overlay.WasPressedThisFrame()) DebugCommands.ToggleOverlay();
            if (d.TimeScale.WasPressedThisFrame()) DebugCommands.ToggleTimeScale();
            if (d.AllFragments.WasPressedThisFrame()) DebugCommands.GiveAllFragments();
            if (d.DummyBot.WasPressedThisFrame()) DebugCommands.ToggleDummyHuman();
        }
    }
}
