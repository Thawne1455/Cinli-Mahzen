using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Jinn
{
    /// <summary>
    /// Scene side of <see cref="PingSystem"/> (TODO 3.5): ticks it and draws each live ping as a glowing sphere everybody sees (Default layer).
    /// Created hidden and persistent by <c>SpiritBootstrap</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PingRunner : MonoBehaviour
    {
        [SerializeField] private float glowSize = 0.35f;
        [SerializeField] private Color glowColor = new Color(0.6f, 0.9f, 1f);

        private readonly Dictionary<int, GameObject> _glows = new Dictionary<int, GameObject>();
        private PingSystem _system;
        private INetBridge _net;

        public PingSystem System => _system;

        private void Awake()
        {
            _net = GameServices.Net;
            IPlayerRegistry players = GameServices.Players;
            GameBalanceConfig config = GameServices.Config;
            if (_net == null || players == null || config == null)
            {
                enabled = false;
                return;
            }
            _system = PingSystem.FromConfig(_net, players, config);
            _system.PingAdded += OnPingAdded;
            _system.PingRemoved += OnPingRemoved;
        }

        private void OnEnable()
        {
            _system?.Attach();
        }

        private void OnDisable()
        {
            _system?.Detach();
        }

        private void Update()
        {
            _system.Tick(_net.Time);
        }

        private void OnPingAdded(PingSystem.Ping ping)
        {
            GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glow.name = "Ping_" + ping.Id;
            Destroy(glow.GetComponent<Collider>());
            glow.transform.position = ping.Point;
            glow.transform.localScale = Vector3.one * glowSize;
            Renderer r = glow.GetComponent<Renderer>();
            r.material.color = glowColor;
            if (r.material.HasProperty("_EmissionColor"))
            {
                r.material.EnableKeyword("_EMISSION");
                r.material.SetColor("_EmissionColor", glowColor * 2f);
            }
            _glows[ping.Id] = glow;
        }

        private void OnPingRemoved(int id)
        {
            if (_glows.TryGetValue(id, out GameObject glow))
            {
                _glows.Remove(id);
                if (glow != null)
                    Destroy(glow);
            }
        }
    }
}
