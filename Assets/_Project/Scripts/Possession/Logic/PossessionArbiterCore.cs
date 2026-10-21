using System.Collections.Generic;
using CinliMahzen.Core;

namespace CinliMahzen.Possession
{
    /// <summary>
    /// Authority-side possession rules (Tech §6.2), plain C#. Owns one
    /// <see cref="PossessableStateMachine"/> per registered object, the player → object map and
    /// the per (player, object) re-enter cooldowns.
    ///
    /// <c>TryPossess</c> checks, in order (v2.1):
    /// 1. player is EvilJinn or GoodJinn (the good jinn only into <see cref="PossessableTraits.AllowGoodJinn"/>
    ///    objects) and not stunned / possess-locked
    /// 2. match is Playing and jinns are awake (JinnWakeDelay)
    /// 3. object is registered, Free and not Spent
    /// 4. light rule: an evil jinn cannot enter an object in a lit room, unless it is a lamp
    ///    (<see cref="PossessableTraits.IsLamp"/>); the good jinn ignores light
    /// 5. distance ≤ PossessRange + tolerance
    /// 6. this player's re-enter cooldown for this object elapsed, and the player is not already
    ///    in (or entering) another object
    ///
    /// Race: requests are processed in arrival order; the first one flips the object to Entering,
    /// so a second request in the same frame is denied with <see cref="PossessDenyReason.Occupied"/>.
    /// </summary>
    public sealed class PossessionArbiterCore
    {
        private readonly IPossessionWorld _world;
        private readonly Dictionary<NetId, int> _indexById = new Dictionary<NetId, int>();
        private readonly List<NetId> _ids = new List<NetId>();
        private readonly List<PossessableStateMachine> _machines = new List<PossessableStateMachine>();
        private readonly List<PossessableTraits> _traits = new List<PossessableTraits>();
        private readonly Dictionary<PlayerId, NetId> _objectByPlayer = new Dictionary<PlayerId, NetId>();
        private readonly CooldownTracker _reenter = new CooldownTracker();

        public PossessionArbiterCore(IPossessionWorld world, PossessionRules rules)
        {
            _world = world;
            Rules = rules;
        }

        public PossessionRules Rules { get; set; }

        public int ObjectCount => _ids.Count;

        /// <summary>Re-enter cooldown multiplier (Öfke: <c>RageCooldownMult</c>).</summary>
        public float CooldownMultiplier => _reenter.Multiplier;

        // ------------------------------------------------------------------ registry

        /// <summary>
        /// Registers a possessable (populator / Possessable.OnEnable). Idempotent: registering again returns
        /// the existing state machine and replaces the traits. Without <paramref name="traits"/> the object
        /// is a plain evil-jinn-only, non-lamp object.
        /// </summary>
        public PossessableStateMachine Register(NetId obj, PossessableTraits traits = default)
        {
            if (_indexById.TryGetValue(obj, out int i))
            {
                _traits[i] = traits;
                return _machines[i];
            }
            var sm = new PossessableStateMachine();
            _indexById.Add(obj, _ids.Count);
            _ids.Add(obj);
            _machines.Add(sm);
            _traits.Add(traits);
            return sm;
        }

        /// <summary>
        /// Removes an object. If a jinn was inside, it is released without a cooldown and returned
        /// so the caller can broadcast <c>PossessEnded(Forced)</c>.
        /// </summary>
        public PlayerId Unregister(NetId obj)
        {
            if (!_indexById.TryGetValue(obj, out int i))
            {
                return PlayerId.None;
            }
            PlayerId occupant = _machines[i].Occupant;
            if (_machines[i].IsOccupied)
            {
                _objectByPlayer.Remove(occupant);
            }
            int last = _ids.Count - 1;
            if (i != last)
            {
                NetId movedId = _ids[last];
                _ids[i] = movedId;
                _machines[i] = _machines[last];
                _traits[i] = _traits[last];
                _indexById[movedId] = i;
            }
            _ids.RemoveAt(last);
            _machines.RemoveAt(last);
            _traits.RemoveAt(last);
            _indexById.Remove(obj);
            return occupant;
        }

        /// <summary>Forgets every object, possession and cooldown (new round).</summary>
        public void Clear()
        {
            _indexById.Clear();
            _ids.Clear();
            _machines.Clear();
            _traits.Clear();
            _objectByPlayer.Clear();
            _reenter.Reset();
        }

        /// <summary>
        /// New round: every object back to Free (Spent ones included), nobody inside anything, every re-enter cooldown cleared.
        /// The registered objects and their traits stay. Nothing is broadcast; every machine resets its mirror on the same round event.
        /// </summary>
        public void ResetRound()
        {
            for (int i = 0; i < _machines.Count; i++)
            {
                _machines[i].Reset();
            }
            _objectByPlayer.Clear();
            _reenter.Reset();
        }

        public bool TryGetMachine(NetId obj, out PossessableStateMachine machine)
        {
            if (_indexById.TryGetValue(obj, out int i))
            {
                machine = _machines[i];
                return true;
            }
            machine = null;
            return false;
        }

        public bool TryGetTraits(NetId obj, out PossessableTraits traits)
        {
            if (_indexById.TryGetValue(obj, out int i))
            {
                traits = _traits[i];
                return true;
            }
            traits = default;
            return false;
        }

        public NetId GetObjectAt(int index)
        {
            return _ids[index];
        }

        public PossessableStateMachine GetMachineAt(int index)
        {
            return _machines[index];
        }

        // ------------------------------------------------------------------ queries

        /// <summary>True while a jinn is entering or inside the object.</summary>
        public bool IsPossessed(NetId obj)
        {
            return TryGetMachine(obj, out PossessableStateMachine sm) && sm.IsOccupied;
        }

        public PlayerId PossessorOf(NetId obj)
        {
            return TryGetMachine(obj, out PossessableStateMachine sm) ? sm.Occupant : PlayerId.None;
        }

        /// <summary>Object the player is entering or inside.</summary>
        public bool TryGetPossessed(PlayerId player, out NetId obj)
        {
            return _objectByPlayer.TryGetValue(player, out obj);
        }

        public double ReenterReadyAt(PlayerId player, NetId obj)
        {
            return _reenter.ReadyAt(ReenterKey(player, obj));
        }

        // ------------------------------------------------------------------ rules

        /// <summary>Evaluates the six rules without changing anything.</summary>
        public PossessDenyReason Check(PlayerId player, NetId obj, double now)
        {
            // 1. Player (the good jinn is only checked against the object's flag when the object is known;
            //    an unknown object falls through to InvalidTarget in rule 3)
            Role role = _world.GetRole(player);
            if (role != Role.EvilJinn && role != Role.GoodJinn)
            {
                return PossessDenyReason.RoleNotAllowed;
            }
            bool known = _indexById.TryGetValue(obj, out int index);
            if (role == Role.GoodJinn && known && !_traits[index].AllowGoodJinn)
            {
                return PossessDenyReason.GoodJinnNotAllowed;
            }
            if (_world.IsPossessionLocked(player, now))
            {
                return PossessDenyReason.Stunned;
            }

            // 2. Match phase
            if (!_world.IsPlaying)
            {
                return PossessDenyReason.NotPlaying;
            }
            if (!_world.JinnsAwake)
            {
                return PossessDenyReason.JinnsAsleep;
            }

            // 3. Object
            if (!known)
            {
                return PossessDenyReason.InvalidTarget;
            }
            PossessableStateMachine sm = _machines[index];
            if (sm.IsSpent)
            {
                return PossessDenyReason.Spent;
            }
            if (sm.State != PossessableState.Free)
            {
                return PossessDenyReason.Occupied;
            }

            // 4. Light: an evil jinn cannot enter a lit room's object; lamps are the one exception.
            //    The good jinn is not affected by light.
            if (role == Role.EvilJinn && !_traits[index].IsLamp && _world.LightOfObject(obj) == RoomLightState.On)
            {
                return PossessDenyReason.Lit;
            }

            // 5. Distance
            if (!_world.TryGetDistance(player, obj, out float distance) || distance > Rules.MaxDistance)
            {
                return PossessDenyReason.OutOfRange;
            }

            // 6. Re-enter cooldown, then already inside something else
            if (!_reenter.IsReady(ReenterKey(player, obj), now))
            {
                return PossessDenyReason.Cooldown;
            }
            if (_objectByPlayer.ContainsKey(player))
            {
                return PossessDenyReason.AlreadyPossessing;
            }

            return PossessDenyReason.None;
        }

        /// <summary>
        /// Authority: validates and, on success, moves the object to Entering.
        /// <paramref name="enterEndTime"/> = now + PossessTime (payload of <c>PossessBegan</c>).
        /// </summary>
        public PossessDenyReason TryPossess(PlayerId player, NetId obj, double now, out double enterEndTime)
        {
            enterEndTime = 0d;
            PossessDenyReason reason = Check(player, obj, now);
            if (reason != PossessDenyReason.None)
            {
                return reason;
            }
            TryGetMachine(obj, out PossessableStateMachine sm);
            enterEndTime = now + Rules.PossessTime;
            sm.BeginEnter(player, enterEndTime);
            _objectByPlayer[player] = obj;
            return PossessDenyReason.None;
        }

        /// <summary>
        /// Player leaves whatever it is in (voluntary exit, exorcism, room lit, debug).
        /// Starts the re-enter cooldown for (player, object). Returns false if the player was not inside anything.
        /// </summary>
        public bool Release(PlayerId player, double now, out NetId obj)
        {
            if (!_objectByPlayer.TryGetValue(player, out obj))
            {
                return false;
            }
            _objectByPlayer.Remove(player);
            if (TryGetMachine(obj, out PossessableStateMachine sm))
            {
                sm.Exit();
            }
            _reenter.Start(ReenterKey(player, obj), now, Rules.ReenterCooldown);
            return true;
        }

        /// <summary>Ejects whoever is in <paramref name="obj"/> (room lit, exorcism). Returns the ejected player or None.</summary>
        public PlayerId Eject(NetId obj, double now)
        {
            PlayerId occupant = PossessorOf(obj);
            if (!IsPossessed(obj))
            {
                return PlayerId.None;
            }
            Release(occupant, now, out _);
            return occupant;
        }

        /// <summary>Öfke: scales re-enter cooldowns (running ones included).</summary>
        public void SetCooldownMultiplier(float multiplier, double now)
        {
            _reenter.SetMultiplier(multiplier, now);
        }

        /// <summary>
        /// Advances every object's timers and appends the transitions to <paramref name="results"/>
        /// (not cleared). Players ejected by <see cref="PossessableTransition.SpentEjected"/> are released
        /// from the player map. Allocation-free once lists are warm.
        /// </summary>
        public int Tick(double now, List<PossessionTransitionInfo> results)
        {
            int count = 0;
            for (int i = 0; i < _machines.Count; i++)
            {
                PossessableStateMachine sm = _machines[i];
                PlayerId occupant = sm.Occupant;
                PossessableTransition t = sm.Tick(now);
                if (t == PossessableTransition.None)
                {
                    continue;
                }
                if (t == PossessableTransition.SpentEjected)
                {
                    _objectByPlayer.Remove(occupant);
                    _reenter.Start(ReenterKey(occupant, _ids[i]), now, Rules.ReenterCooldown);
                }
                results.Add(new PossessionTransitionInfo(_ids[i], occupant, t));
                count++;
            }
            return count;
        }

        private static long ReenterKey(PlayerId player, NetId obj)
        {
            return CooldownTracker.Key(player.Value, obj.Value);
        }
    }
}
