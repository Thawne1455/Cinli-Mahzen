using System;
using System.Globalization;
using System.Text;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Jinn
{
    /// <summary>
    /// Jinn energy as a service (Teknik §6.5, TODO 1.8), plain C#: one <see cref="JinnEnergyCore"/> per jinn player (evil jinns with
    /// EnergyMax / EnergyStart / EnergyRegen, the good jinn with GoodEnergyMax / GoodEnergyRegen). The authority regenerates
    /// (<see cref="TickAuthority"/>) and spends (<see cref="TrySpendAuthority"/>: possession actions, hops, mass sabotage) and broadcasts
    /// <c>EnergyChanged</c> (a spend at once, regen throttled); clients mirror it. New round / new roles: every tank back to its start.
    /// Players that are not jinns have no tank: Get = 0 and every positive spend fails.
    /// </summary>
    public sealed class JinnEnergyService : IJinnEnergyService
    {
        private const int Slots = 256;

        private readonly INetBridge _net;
        private readonly IPlayerRegistry _players;
        private readonly JinnEnergySettings _evil;
        private readonly JinnEnergySettings _good;
        private readonly JinnEnergyCore[] _cores = new JinnEnergyCore[Slots];
        private readonly Role[] _roleOfCore = new Role[Slots];
        private readonly Action<NetMsg> _onChanged;
        private readonly Action _onRolesChanged;
        private readonly Action<RoundStartedEvt> _onRoundStarted;
        private readonly float[] _drainPerSecond = new float[Slots];
        private bool _attached;
        private bool _unlimited;

        public JinnEnergyService(INetBridge net, IPlayerRegistry players, in JinnEnergySettings evil, in JinnEnergySettings good)
        {
            _net = net ?? throw new ArgumentNullException(nameof(net));
            _players = players ?? throw new ArgumentNullException(nameof(players));
            _evil = evil;
            _good = good;
            _onChanged = OnEnergyChanged;
            _onRolesChanged = ResetAll;
            _onRoundStarted = e => ResetAll();
        }

        /// <summary>Evil and good settings from the config; <paramref name="syncMinDelta"/> / <paramref name="syncMinInterval"/> throttle regen broadcasts.</summary>
        public static JinnEnergyService FromConfig(INetBridge net, IPlayerRegistry players, GameBalanceConfig config, float syncMinDelta, float syncMinInterval)
        {
            var evil = new JinnEnergySettings(config.EnergyMax, config.EnergyStart, config.EnergyRegen, config.RageRegenMult, syncMinDelta, syncMinInterval);
            var good = new JinnEnergySettings(config.GoodEnergyMax, Mathf.Min(config.EnergyStart, config.GoodEnergyMax), config.GoodEnergyRegen, 1f,
                syncMinDelta, syncMinInterval);
            return new JinnEnergyService(net, players, evil, good);
        }

        /// <summary>Authority: a continuous drain ran a player dry (the drain is cleared first). TODO 3.5: the good jinn's focus stops.</summary>
        public event Action<PlayerId> DrainExhausted;

        /// <summary>Authority: <paramref name="player"/> pays <paramref name="perSecond"/> every second until set back to 0 (focus: FocusReferenceCost).</summary>
        public void SetDrainAuthority(PlayerId player, float perSecond)
        {
            if (player.IsValid)
                _drainPerSecond[player.Value] = perSecond > 0f ? perSecond : 0f;
        }

        public float DrainOf(PlayerId player)
        {
            return player.IsValid ? _drainPerSecond[player.Value] : 0f;
        }

        /// <summary>Debug (F6): every spend succeeds and nothing is deducted.</summary>
        public bool Unlimited
        {
            get => _unlimited;
            set
            {
                _unlimited = value;
                for (int i = 0; i < Slots; i++)
                {
                    if (_cores[i] != null)
                        _cores[i].Unlimited = value;
                }
            }
        }

        public void Attach()
        {
            if (_attached)
                return;
            _attached = true;
            _net.Register(MsgCode.EnergyChanged, _onChanged);
            _players.RolesChanged += _onRolesChanged;
            EventBus.Subscribe(_onRoundStarted);
            ResetAll();
        }

        public void Detach()
        {
            if (!_attached)
                return;
            _attached = false;
            _net.Unregister(MsgCode.EnergyChanged, _onChanged);
            _players.RolesChanged -= _onRolesChanged;
            EventBus.Unsubscribe(_onRoundStarted);
        }

        // ---- IJinnEnergyService ----

        public float Get(PlayerId player)
        {
            JinnEnergyCore core = CoreOf(player);
            return core != null ? core.Current : 0f;
        }

        public bool TrySpendAuthority(PlayerId player, float amount)
        {
            Debug.Assert(_net.IsAuthority, "TrySpendAuthority must only run on the authority");
            if (amount <= 0f)
                return true;
            JinnEnergyCore core = CoreOf(player);
            if (core == null || !core.TrySpend(amount))
                return false;
            Sync(player, core, _net.Time);
            return true;
        }

        /// <summary>Authority, every frame: regeneration and throttled sync.</summary>
        public void TickAuthority(float deltaTime)
        {
            if (!_net.IsAuthority)
                return;
            double now = _net.Time;
            for (int i = 0; i < Slots; i++)
            {
                JinnEnergyCore core = _cores[i];
                if (core == null)
                    continue;
                core.Tick(deltaTime);
                float drain = _drainPerSecond[i];
                if (drain > 0f && !core.Drain(drain * deltaTime))
                {
                    _drainPerSecond[i] = 0f;
                    DrainExhausted?.Invoke(new PlayerId((byte)i));
                }
                if (core.NeedsSync(now))
                    Sync(new PlayerId((byte)i), core, now);
            }
        }

        /// <summary>Authority: Öfke (cellar dig, TODO 3.7) for every evil jinn.</summary>
        public void SetRageAuthority(bool active)
        {
            for (int i = 0; i < Slots; i++)
            {
                if (_cores[i] != null && _roleOfCore[i] == Role.EvilJinn)
                    _cores[i].SetRage(active);
            }
        }

        // ---- internals ----

        private JinnEnergyCore CoreOf(PlayerId player)
        {
            return player.IsValid ? _cores[player.Value] : null;
        }

        private void Sync(PlayerId player, JinnEnergyCore core, double now)
        {
            if (!_net.IsAuthority)
                return;
            _net.Broadcast(EnergyChangedMsg.Encode(player, core.Current));
            core.MarkSynced(now);
        }

        /// <summary>One tank per jinn player, filled to its start value; everybody else none.</summary>
        private void ResetAll()
        {
            Array.Clear(_cores, 0, Slots);
            Array.Clear(_drainPerSecond, 0, Slots);
            var infos = _players.Players;
            for (int i = 0; i < infos.Count; i++)
            {
                PlayerInfo info = infos[i];
                if (!info.Id.IsValid || (info.Role != Role.EvilJinn && info.Role != Role.GoodJinn))
                    continue;
                var core = new JinnEnergyCore(info.Role == Role.EvilJinn ? _evil : _good) { Unlimited = _unlimited };
                _cores[info.Id.Value] = core;
                _roleOfCore[info.Id.Value] = info.Role;
            }
        }

        private void OnEnergyChanged(NetMsg msg)
        {
            if (_net.IsAuthority || !EnergyChangedMsg.TryDecode(msg, out PlayerId player, out float value))
                return;
            CoreOf(player)?.ApplyRemote(value);
        }

        /// <summary>JSON for Dump State ("energy"): player → value.</summary>
        public string BuildDebugJson()
        {
            var sb = new StringBuilder("{");
            bool first = true;
            for (int i = 0; i < Slots; i++)
            {
                if (_cores[i] == null)
                    continue;
                if (!first)
                    sb.Append(',');
                first = false;
                sb.Append("\"P").Append(i.ToString(CultureInfo.InvariantCulture)).Append("\":")
                    .Append(_cores[i].Current.ToString("0.0", CultureInfo.InvariantCulture));
            }
            return sb.Append('}').ToString();
        }
    }
}
