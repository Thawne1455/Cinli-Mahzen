using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Jinn
{
    /// <summary>
    /// Drives <see cref="JinnEnergyService"/> every frame (TODO 1.8) and publishes it as <see cref="IJinnEnergyService"/> (replacing the stub) plus the
    /// "energy" Dump State section. Created once, hidden and persistent, by <c>SpiritBootstrap</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class JinnEnergyRunner : MonoBehaviour
    {
        private const string DebugStateKey = "energy";

        [Tooltip("Minimum change before a regen EnergyChanged broadcast (Teknik §6.5: 1).")]
        [SerializeField] private float syncMinDelta = 1f;
        [Tooltip("Minimum seconds between regen broadcasts (Teknik §6.5: 4 Hz).")]
        [SerializeField] private float syncMinInterval = 0.25f;

        private JinnEnergyService _service;

        public JinnEnergyService Service => _service;

        private void Awake()
        {
            INetBridge net = GameServices.Net;
            IPlayerRegistry players = GameServices.Players;
            GameBalanceConfig config = GameServices.Config;
            if (net == null || players == null || config == null)
            {
                CMLog.Warn("Energy", "GameServices.Net / Players / Config missing - jinn energy stays on the stub");
                enabled = false;
                return;
            }
            _service = JinnEnergyService.FromConfig(net, players, config, syncMinDelta, syncMinInterval);
        }

        private void OnEnable()
        {
            if (_service == null)
                return;
            _service.Attach();
            GameServices.Register<IJinnEnergyService>(_service);
            DebugState.Register(DebugStateKey, _service.BuildDebugJson);
        }

        private void OnDisable()
        {
            if (_service == null)
                return;
            _service.Detach();
            GameServices.Unregister<IJinnEnergyService>(_service);
            DebugState.Unregister(DebugStateKey);
        }

        private void Update()
        {
            _service.TickAuthority(Time.deltaTime);
        }
    }
}
