using System;
using System.Globalization;
using System.Text;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Match
{
    /// <summary>
    /// The match as a scene object (Teknik §3, §9; TODO 1.10): the thin MonoBehaviour around <see cref="MatchStateMachine"/>.
    /// One per Game scene (CinliMahzen/Setup/Add Match Controller To Open Scene). It is also the scene's <see cref="IMatchInfo"/>
    /// (registered in <c>Awake</c>; the stub is back after <c>OnDestroy</c>).
    ///
    /// <para><b>Flow</b> (Request → authority → broadcast → everyone applies):
    /// the authority feeds <c>Net.Time</c> to the machine every frame and turns each <see cref="MatchTransition"/> into a
    /// <c>MatchStateChanged</c> broadcast; entering RoundSetup also broadcasts <c>RoundSetup{seed, roles}</c> (roles from
    /// <see cref="RoleRotation"/>) and — offline — completes the setup in the same frame; entering RoundEnd also broadcasts
    /// <c>RoundEnded</c>. The round's inputs reach the authority as local events: <see cref="HumanFaintedEvt"/> (3 faints end it),
    /// <see cref="TreasureDugEvt"/>, and <see cref="StageCompletedEvt"/> (a stage gate opened, TODO 2.7 → <see cref="AddStageTimeBonusAuthority"/> → <c>RoundTimeAdded</c>).</para>
    ///
    /// <para><b>Every machine</b> (the authority included) applies the broadcasts: a <see cref="MatchMirror"/> backs
    /// <see cref="IMatchInfo"/>; <c>RoundSetup</c> hands the roles to the registry (whose <c>RolesChanged</c> makes the scene's
    /// <c>PawnSpawner</c> destroy and re-create the pawns) and then publishes <see cref="RoundStartedEvt"/>;
    /// <c>RoundTimeAdded</c> and <c>RoundEnded</c> become <see cref="RoundTimeAddedEvt"/> / <see cref="RoundEndedEvt"/>.</para>
    ///
    /// <para><b>Seeds.</b> The authority picks a match seed when the match starts (<c>fixedSeed</c>, or time-based when 0);
    /// each round's seed is <see cref="RoundSeeds.ForRound"/> of it and travels in <c>RoundSetup</c>, so every placement
    /// derived from it is deterministic. The first Update starts the match (not Start), so a PawnSpawner's own Start has already run.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchController : MonoBehaviour, IMatchInfo
    {
        private const string Category = "Match";

        /// <summary>Safety cap for the tick loop: a normal frame advances 0-2 states (Playing → RoundEnd → RoundSetup → RoleReveal).</summary>
        private const int MaxTransitionsPerPump = 8;

        [Tooltip("0 = pick a time-based match seed when the match starts. Otherwise this exact match seed: the same rounds, layouts and role order every run.")]
        [SerializeField] private int fixedSeed;

        [Tooltip("Start the match on the first frame (authority only). Off = the match stays in Lobby until StartMatchAuthority is called.")]
        [SerializeField] private bool autoStart = true;

        private INetBridge _net;
        private MatchSettings _settings;
        private MatchMirror _mirror;
        private MatchStateMachine _machine;
        private int _matchSeed;
        private bool _startPending;
        private bool _registered;

        // Cached once so Register/Unregister/Subscribe and the machine/mirror never allocate a new delegate.
        private Func<double> _clock;
        private Func<int, int> _seedProvider;
        private Action<NetMsg> _onStateChanged;
        private Action<NetMsg> _onRoundSetup;
        private Action<NetMsg> _onTimeAdded;
        private Action<NetMsg> _onRoundEnded;
        private Action<HumanFaintedEvt> _onHumanFainted;
        private Action<TreasureDugEvt> _onTreasureDug;
        private Action<StageCompletedEvt> _onStageCompleted;

        // ---- IMatchInfo (the mirror: what every machine has been told) ----

        public MatchState State => _mirror != null ? _mirror.State : MatchState.Lobby;

        public int RoundIndex => _mirror != null ? _mirror.RoundIndex : 0;

        public double StateEndTime => _mirror != null ? _mirror.StateEndTime : double.MaxValue;

        public bool JinnsAwake => _mirror != null && _mirror.JinnsAwake;

        public int Faints => _mirror != null ? _mirror.Faints : 0;

        // ---- Extra read access (debug overlay / dump, UI) ----

        /// <summary>Setup seed of the current round (0 before the first RoundSetup).</summary>
        public int Seed => _mirror != null ? _mirror.Seed : 0;

        /// <summary>Winner of the most recently ended round.</summary>
        public RoundResult LastResult => _mirror != null ? _mirror.LastResult : RoundResult.None;

        /// <summary>Why the most recent round ended.</summary>
        public RoundEndReason LastReason => _mirror != null ? _mirror.LastReason : RoundEndReason.None;

        private void Awake()
        {
            _net = GameServices.Net;
            GameBalanceConfig config = GameServices.Config;
            if (_net == null || config == null)
            {
                CMLog.Error(Category, name + ": GameServices.Net / Config missing (is the bootstrap running?) - the match is inert");
                enabled = false;
                return;
            }

            _settings = MatchSettings.FromConfig(config);
            _clock = ReadNetTime;
            _seedProvider = SeedForRound;
            _mirror = new MatchMirror(_settings, _clock);
            _machine = new MatchStateMachine(_settings, _seedProvider);

            _onStateChanged = OnStateChangedMsg;
            _onRoundSetup = OnRoundSetupMsg;
            _onTimeAdded = OnTimeAddedMsg;
            _onRoundEnded = OnRoundEndedMsg;
            _onHumanFainted = OnHumanFaintedEvt;
            _onTreasureDug = OnTreasureDugEvt;
            _onStageCompleted = OnStageCompletedEvt;

            GameServices.Register<IMatchInfo>(this);
            _registered = true;
        }

        private void OnEnable()
        {
            if (_mirror == null)
            {
                return;
            }

            _net.Register(MsgCode.MatchStateChanged, _onStateChanged);
            _net.Register(MsgCode.RoundSetup, _onRoundSetup);
            _net.Register(MsgCode.RoundTimeAdded, _onTimeAdded);
            _net.Register(MsgCode.RoundEnded, _onRoundEnded);
            EventBus.Subscribe(_onHumanFainted);
            EventBus.Subscribe(_onTreasureDug);
            EventBus.Subscribe(_onStageCompleted);

            _startPending = autoStart && _machine.State == MatchState.Lobby;
        }

        private void OnDisable()
        {
            if (_mirror == null)
            {
                return;
            }

            _net.Unregister(MsgCode.MatchStateChanged, _onStateChanged);
            _net.Unregister(MsgCode.RoundSetup, _onRoundSetup);
            _net.Unregister(MsgCode.RoundTimeAdded, _onTimeAdded);
            _net.Unregister(MsgCode.RoundEnded, _onRoundEnded);
            EventBus.Unsubscribe(_onHumanFainted);
            EventBus.Unsubscribe(_onTreasureDug);
            EventBus.Unsubscribe(_onStageCompleted);
        }

        private void OnDestroy()
        {
            if (_registered)
            {
                GameServices.Unregister<IMatchInfo>(this);
                _registered = false;
            }
        }

        private void Update()
        {
            if (!_net.IsAuthority)
            {
                return;
            }

            if (_startPending)
            {
                _startPending = false;
                StartMatchAuthority();
            }

            Pump(_net.Time);
        }

        // ---- Authority: drive the machine ----

        /// <summary>
        /// Authority only. Lobby → RoundSetup (round 0) with a fresh match seed (<c>fixedSeed</c>, or time-based when it is 0).
        /// Returns false when the match is already running.
        /// </summary>
        public bool StartMatchAuthority()
        {
            Debug.Assert(_net != null && _net.IsAuthority, "StartMatchAuthority must only run on the authority");
            if (_machine == null || !_net.IsAuthority)
            {
                return false;
            }

            int seed = fixedSeed != 0 ? fixedSeed : RoundSeeds.CreateMatchSeed(Environment.TickCount);
            return BeginMatch(seed);
        }

        /// <summary>
        /// Authority only. A gate/stage opened: <c>StateEndTime += StageTimeBonus</c> and <c>RoundTimeAdded</c> is broadcast
        /// (TODO 2.7 stage gates call this; F11 does for now). False — nothing sent — outside Playing or after the counter ran out.
        /// </summary>
        public bool AddStageTimeBonusAuthority()
        {
            Debug.Assert(_net != null && _net.IsAuthority, "AddStageTimeBonusAuthority must only run on the authority");
            if (_machine == null || !_net.IsAuthority)
            {
                return false;
            }

            double now = _net.Time;
            Pump(now);
            if (!_machine.TryAddStageTimeBonus(now, out double newEndTime))
            {
                CMLog.Info(Category, "Stage time bonus ignored: state " + _machine.State + " does not accept round events");
                return false;
            }

            _net.Broadcast(RoundTimeAddedMsg.Encode(_settings.StageTimeBonus, newEndTime));
            return true;
        }

        /// <summary>
        /// Debug (F8 / "Restart Round (New Seed)"). Drops the running match back to Lobby and starts a new one with a new match seed,
        /// i.e. from round 0 (the role rotation restarts too). False — nothing changes — when this machine is not the authority.
        /// </summary>
        public bool RestartWithNewSeed()
        {
            if (_machine == null || !_net.IsAuthority)
            {
                CMLog.Warn(Category, "Restart: this machine is not the authority");
                return false;
            }

            _startPending = false;
            int seed = RoundSeeds.CreateMatchSeed(Environment.TickCount, _matchSeed);
            if (_machine.ReturnToLobby(out MatchTransition lobby))
            {
                Publish(lobby);
            }

            return BeginMatch(seed);
        }

        /// <summary>
        /// Debug ("Skip Match State"). Pulls the end of the current timed state to now (RoleReveal, Intro, Playing → Timeout, RoundEnd)
        /// and re-broadcasts the new end time, so every machine's mirror follows. False when the state has no timer to skip.
        /// </summary>
        public bool SkipStateTimer()
        {
            if (_machine == null || !_net.IsAuthority)
            {
                CMLog.Warn(Category, "Skip: this machine is not the authority");
                return false;
            }

            double now = _net.Time;
            Pump(now);
            MatchState state = _machine.State;
            if (!_machine.TrySkipTimer(now))
            {
                CMLog.Info(Category, "Skip: " + state + " has no pending timer");
                return false;
            }

            _net.Broadcast(MatchStateChangedMsg.Encode(state, _machine.RoundIndex, _machine.StateEndTime));
            CMLog.Info(Category, "Skipped the timer of " + state);
            Pump(now);
            return true;
        }

        private bool BeginMatch(int matchSeed)
        {
            _matchSeed = matchSeed;
            if (!_machine.StartMatch(out MatchTransition transition))
            {
                return false;
            }

            CMLog.Info(Category, "Match start: match seed " + matchSeed);
            Publish(transition);
            return true;
        }

        /// <summary>Advances the machine to <paramref name="now"/> and broadcasts every state it passed through.</summary>
        private void Pump(double now)
        {
            int passed = 0;
            while (passed < MaxTransitionsPerPump && _machine.Tick(now, out MatchTransition transition))
            {
                passed++;
                Publish(transition);
            }
        }

        /// <summary>Broadcasts one transition and what belongs to entering its state.</summary>
        private void Publish(in MatchTransition transition)
        {
            _net.Broadcast(MatchStateChangedMsg.Encode(transition));

            switch (transition.To)
            {
                case MatchState.RoundSetup:
                    EnterRoundSetup(transition);
                    break;

                case MatchState.RoundEnd:
                    _net.Broadcast(RoundEndedMsg.Encode(_machine.LastResult, _machine.LastReason, transition.RoundIndex));
                    break;
            }
        }

        private void EnterRoundSetup(in MatchTransition transition)
        {
            IPlayerRegistry players = GameServices.Players;
            int playerCount = players != null ? players.Players.Count : RoleRotation.DefaultPlayerCount;
            Role[] roles = RoleRotation.CreateRoles(transition.RoundIndex, playerCount);
            _net.Broadcast(RoundSetupMsg.Encode(_machine.Seed, roles));

            // Offline the house needs no build step, so the setup gate opens in the same frame.
            // TODO(M5, Teknik §9): online, wait for HouseReady from every client before CompleteRoundSetup.
            if (_machine.CompleteRoundSetup(_net.Time, out MatchTransition revealing))
            {
                Publish(revealing);
            }
        }

        // ---- Authority: round inputs (local events) ----

        private void OnHumanFaintedEvt(HumanFaintedEvt evt)
        {
            _mirror.ApplyFaints(evt.FaintCount);
            if (!_net.IsAuthority)
            {
                return;
            }

            // Tick first, then deliver the event: if the counter already ran out, the sun wins the tie.
            double now = _net.Time;
            Pump(now);
            if (_machine.OnHumanFainted(evt.FaintCount, now, out MatchTransition transition))
            {
                Publish(transition);
            }
        }

        /// <summary>TODO 2.7: a stage gate opened (Objectives' GateOpened → StageCompletedEvt on every machine); the authority adds StageTimeBonus.</summary>
        private void OnStageCompletedEvt(StageCompletedEvt evt)
        {
            if (_net.IsAuthority)
                AddStageTimeBonusAuthority();
        }

        private void OnTreasureDugEvt(TreasureDugEvt evt)
        {
            if (!_net.IsAuthority)
            {
                return;
            }

            double now = _net.Time;
            Pump(now);
            if (_machine.OnTreasureDug(now, out MatchTransition transition))
            {
                Publish(transition);
            }
        }

        // ---- Handlers (every machine, the authority included) ----

        private void OnStateChangedMsg(NetMsg msg)
        {
            if (!MatchStateChangedMsg.TryDecode(msg, out MatchState state, out int roundIndex, out double endTime))
            {
                CMLog.Warn(Category, "Malformed MatchStateChanged message ignored");
                return;
            }

            MatchState previous = _mirror.State;
            _mirror.ApplyStateChanged(state, roundIndex, endTime);
            if (state != previous)
            {
                CMLog.Info(Category, previous + " -> " + state + " (round " + roundIndex + ", ends " + FormatTime(endTime) + ")");
            }
        }

        private void OnRoundSetupMsg(NetMsg msg)
        {
            if (!RoundSetupMsg.TryDecode(msg, out int seed, out Role[] roles))
            {
                CMLog.Warn(Category, "Malformed RoundSetup message ignored");
                return;
            }

            _mirror.ApplyRoundSetup(seed);
            CMLog.Info(Category, "Round " + _mirror.RoundIndex + " setup: seed " + seed + ", roles " + Describe(roles));

            // The registry's RolesChanged re-creates the pawns (PawnSpawner); publish the round start only afterwards,
            // so the fresh pawns (HumanHealth.ResetForRound, ...) are subscribed when it arrives.
            ApplyRoles(roles);
            EventBus.Publish(new RoundStartedEvt(_mirror.RoundIndex, seed));
        }

        private void OnTimeAddedMsg(NetMsg msg)
        {
            if (!RoundTimeAddedMsg.TryDecode(msg, out float bonusSec, out double newEndTime))
            {
                CMLog.Warn(Category, "Malformed RoundTimeAdded message ignored");
                return;
            }

            if (!_mirror.ApplyTimeAdded(newEndTime))
            {
                CMLog.Warn(Category, "RoundTimeAdded arrived in " + _mirror.State + " - ignored");
                return;
            }

            CMLog.Info(Category, "Round time +" + bonusSec.ToString("0.#", CultureInfo.InvariantCulture) + " s, ends " + FormatTime(newEndTime));
            EventBus.Publish(new RoundTimeAddedEvt(bonusSec, newEndTime));
        }

        private void OnRoundEndedMsg(NetMsg msg)
        {
            if (!RoundEndedMsg.TryDecode(msg, out RoundResult result, out RoundEndReason reason, out int roundIndex))
            {
                CMLog.Warn(Category, "Malformed RoundEnded message ignored");
                return;
            }

            _mirror.ApplyRoundEnded(result, reason);
            CMLog.Info(Category, "RoundEnd " + result + "(" + reason + ")");
            EventBus.Publish(new RoundEndedEvt(result, reason));
        }

        private void ApplyRoles(Role[] roles)
        {
            var registry = GameServices.Players as OfflinePlayerRegistry;
            if (registry == null)
            {
                CMLog.Warn(Category, "RoundSetup: the player registry is not an OfflinePlayerRegistry - roles not applied (online registry: TODO M5)");
                return;
            }

            if (!registry.SetRoles(roles))
            {
                CMLog.Warn(Category, "RoundSetup: roles rejected by the registry (" + roles.Length + " roles, " + registry.Players.Count + " players)");
            }
        }

        // ---- Helpers ----

        private double ReadNetTime()
        {
            return _net.Time;
        }

        private int SeedForRound(int roundIndex)
        {
            return RoundSeeds.ForRound(_matchSeed, roundIndex);
        }

        private static string FormatTime(double time)
        {
            return time < double.MaxValue ? time.ToString("0.###", CultureInfo.InvariantCulture) : "never";
        }

        private static string Describe(Role[] roles)
        {
            var sb = new StringBuilder(roles.Length * 12);
            for (int i = 0; i < roles.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.Append('P').Append(i).Append('=').Append(roles[i]);
            }

            return sb.ToString();
        }
    }
}
