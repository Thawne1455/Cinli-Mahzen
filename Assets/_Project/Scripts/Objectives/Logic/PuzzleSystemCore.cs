using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Objectives
{
    /// <summary>
    /// The puzzle rules as a plain C# service (Teknik §7.3, §7.4): the logic of the <c>PuzzleSystem</c> scene component, testable
    /// without a scene. Slot → <see cref="PuzzleSlotRuntime"/>; three authority entry points and the broadcast mirror.
    /// <code>
    /// Request (any client) ──► authority: gather facts ► PuzzleRules ► mutate PuzzleState ► Broadcast ──► EVERY machine: ApplySnapshot + event
    ///   ReqPuzzleOp       human     → PuzzleStateChanged{slot, values, by}                    → PuzzleStateChangedEvt
    ///   ReqPuzzleScramble evil jinn → pay energy, TryScrambleAll, cooldown → PuzzleScrambled  → PuzzleScrambledEvt
    ///   ReqPuzzleConfirm  human     → solved: lock → PuzzleConfirmResult{ok} + PuzzleCompleted → PuzzleCompletedEvt
    ///                                 wrong : ConfirmFailDamage + ConfirmFailCooldown → PuzzleConfirmResult{fail}
    /// </code>
    /// The broadcast handlers run on every machine, the authority included; the authority mutates the state first and the handler's
    /// <see cref="PuzzleState.ApplySnapshot"/> then repeats the same values (harmless). Everything the rules need is read at request time
    /// (<see cref="GameServices"/> bridges, pawn lookups); nothing is polled per frame.
    /// <para>Sabotage randomness is one <see cref="GameRandom"/> seeded from <see cref="RoundStartedEvt.Seed"/> through
    /// <see cref="PuzzleRules.ScrambleSeed"/>, so the same round and the same request order give the same arrangements.</para>
    /// <para>The class is also the <see cref="IObjectiveInfo"/> implementation. Stage/phase are plain settable values for now
    /// (TODO 2.7's StageGate fills them); the dig values are 0 until the cellar exists.</para>
    /// </summary>
    public sealed class PuzzleSystemCore : IObjectiveInfo
    {
        /// <summary>DamageInfo.CauseId of the shock a wrong confirmation deals (autopsy Loc key source).</summary>
        public const string ConfirmFailCauseId = "puzzle.confirm_fail";

        private const string Category = "Puzzle";
        private const string TimeFormat = "0.0";

        private readonly INetBridge _net;
        private readonly GameBalanceConfig _config;
        private readonly IPlayerRegistry _players;
        private readonly Func<PlayerId, Vector3?> _pawnPosition;
        private readonly Func<PlayerId, IDamageable> _damageableOf;
        private readonly Dictionary<int, PuzzleSlotRuntime> _slots = new Dictionary<int, PuzzleSlotRuntime>();

        // Cached once so Register/Unregister/Subscribe never allocate a new delegate.
        private readonly Action<NetMsg> _onReqOp;
        private readonly Action<NetMsg> _onReqScramble;
        private readonly Action<NetMsg> _onReqConfirm;
        private readonly Action<NetMsg> _onStateChanged;
        private readonly Action<NetMsg> _onScrambled;
        private readonly Action<NetMsg> _onCompleted;
        private readonly Action<RoundStartedEvt> _onRoundStarted;

        private GameRandom _rng;
        private bool _attached;

        /// <param name="net">Required.</param>
        /// <param name="config">Required (ranges, costs, cooldowns, damage).</param>
        /// <param name="players">Required (role of a requester).</param>
        /// <param name="pawnPosition">World position of a player's pawn, or null when it has none. Called on a request only.</param>
        /// <param name="damageableOf">The damage target for a player's pawn (the human), or null. Called on a wrong confirmation only.</param>
        /// <exception cref="ArgumentNullException">Any argument is null.</exception>
        public PuzzleSystemCore(INetBridge net, GameBalanceConfig config, IPlayerRegistry players,
            Func<PlayerId, Vector3?> pawnPosition, Func<PlayerId, IDamageable> damageableOf)
        {
            _net = net ?? throw new ArgumentNullException(nameof(net));
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _players = players ?? throw new ArgumentNullException(nameof(players));
            _pawnPosition = pawnPosition ?? throw new ArgumentNullException(nameof(pawnPosition));
            _damageableOf = damageableOf ?? throw new ArgumentNullException(nameof(damageableOf));

            _onReqOp = OnReqPuzzleOp;
            _onReqScramble = OnReqPuzzleScramble;
            _onReqConfirm = OnReqPuzzleConfirm;
            _onStateChanged = OnPuzzleStateChanged;
            _onScrambled = OnPuzzleScrambled;
            _onCompleted = OnPuzzleCompleted;
            _onRoundStarted = OnRoundStarted;

            _rng = new GameRandom(PuzzleRules.ScrambleSeed(0));
            StageCount = config.StagesActive + 1;
        }

        // ---- IObjectiveInfo ----

        /// <summary>Current stage: 0 = garden, 1.. = house floors. Set by the stage gate logic (TODO 2.7).</summary>
        public int StageIndex { get; set; }

        /// <summary>Total stages including the garden. Starts at <c>StagesActive + 1</c>.</summary>
        public int StageCount { get; set; }

        public ObjectivePhase Phase { get; set; } = ObjectivePhase.Garden;

        /// <summary>Always 0 until the cellar dig exists (TODO 3.x).</summary>
        public float DigProgress { get; set; }

        /// <summary>Always 0 until the cellar dig exists (TODO 3.x).</summary>
        public int DigCheckpoint { get; set; }

        /// <summary>True when the puzzle in <paramref name="slot"/> is confirmed (locked). False for an unknown slot.</summary>
        public bool IsPuzzleDone(int slot)
        {
            return _slots.TryGetValue(slot, out PuzzleSlotRuntime runtime) && runtime.State.IsLocked;
        }

        public bool IsPuzzleScrambleOnCooldown(int slot, out float remaining)
        {
            remaining = 0f;
            if (!_slots.TryGetValue(slot, out PuzzleSlotRuntime runtime))
            {
                return false;
            }

            double now = _net.Time;
            if (!runtime.State.IsScrambleOnCooldown(now))
            {
                return false;
            }
            remaining = (float)(runtime.State.ScrambleCooldownUntil - now);
            return true;
        }

        // ---- Slots ----

        public int SlotCount => _slots.Count;

        /// <summary>All registered slots (read-only view; do not enumerate while registering).</summary>
        public IReadOnlyDictionary<int, PuzzleSlotRuntime> Slots => _slots;

        /// <summary>
        /// Debug cheat: every range check passes (the pawn lookup is skipped too). All other rules stay on. The debug menus use it to act
        /// from anywhere; never set it outside debug tools.
        /// </summary>
        public bool DebugIgnoreRange { get; set; }

        /// <summary>
        /// Registers (or replaces, with a warning) the puzzle of <paramref name="slot"/>. Call it on every machine with the same plan:
        /// the round setup applier does (TODO 2.7).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="slot"/> is negative.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="runtime"/> is null.</exception>
        public void RegisterSlot(int slot, PuzzleSlotRuntime runtime)
        {
            if (slot < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slot), slot, "slot must be >= 0");
            }
            if (runtime == null)
            {
                throw new ArgumentNullException(nameof(runtime));
            }
            if (_slots.ContainsKey(slot))
            {
                CMLog.Warn(Category, "Slot " + slot + " was already registered - replaced");
            }
            _slots[slot] = runtime;
        }

        /// <summary>Forgets every slot (a new round plan is about to be applied).</summary>
        public void ClearSlots()
        {
            _slots.Clear();
        }

        public bool TryGetSlot(int slot, out PuzzleSlotRuntime runtime)
        {
            return _slots.TryGetValue(slot, out runtime);
        }

        // ---- Wiring ----

        /// <summary>Starts listening: the three requests (acted on only by the authority), the three broadcasts, and RoundStartedEvt (re-seeds the sabotage RNG).</summary>
        public void Attach()
        {
            if (_attached)
            {
                return;
            }
            _attached = true;

            _net.Register(MsgCode.ReqPuzzleOp, _onReqOp);
            _net.Register(MsgCode.ReqPuzzleScramble, _onReqScramble);
            _net.Register(MsgCode.ReqPuzzleConfirm, _onReqConfirm);
            _net.Register(MsgCode.PuzzleStateChanged, _onStateChanged);
            _net.Register(MsgCode.PuzzleScrambled, _onScrambled);
            _net.Register(MsgCode.PuzzleCompleted, _onCompleted);
            EventBus.Subscribe(_onRoundStarted);
        }

        public void Detach()
        {
            if (!_attached)
            {
                return;
            }
            _attached = false;

            _net.Unregister(MsgCode.ReqPuzzleOp, _onReqOp);
            _net.Unregister(MsgCode.ReqPuzzleScramble, _onReqScramble);
            _net.Unregister(MsgCode.ReqPuzzleConfirm, _onReqConfirm);
            _net.Unregister(MsgCode.PuzzleStateChanged, _onStateChanged);
            _net.Unregister(MsgCode.PuzzleScrambled, _onScrambled);
            _net.Unregister(MsgCode.PuzzleCompleted, _onCompleted);
            EventBus.Unsubscribe(_onRoundStarted);
        }

        // ---- Authority entry points ----

        /// <summary>
        /// Authority only. Human part change: rules (<see cref="PuzzleRules.CheckOp"/>), then <see cref="PuzzleState.TryApply"/>, then a
        /// PuzzleStateChanged broadcast. Returns <see cref="PuzzleDenyReason.None"/> when the change was applied; otherwise the reason
        /// (nothing is sent, the state is unchanged).
        /// </summary>
        public PuzzleDenyReason ProcessOpAuthority(PlayerId who, int slot, in PuzzleOp op)
        {
            Debug.Assert(_net.IsAuthority, "ProcessOpAuthority must only run on the authority");
            if (!_slots.TryGetValue(slot, out PuzzleSlotRuntime runtime))
            {
                return Denied("op", slot, who, PuzzleDenyReason.UnknownSlot);
            }

            bool havePawn = TryMeasureRange(who, runtime.Position, _config.InteractRange, out bool inRange);
            PuzzleCheckFacts facts = default;
            facts.Role = _players.GetRole(who);
            facts.HumanFainted = GameServices.Get<IHumanStatusQuery>().IsFainted;
            facts.IsLocked = runtime.State.IsLocked;
            facts.InRange = inRange;
            facts.HasRequiredItem = !runtime.HasRequiredItem || GameServices.Get<IInventoryQuery>().HasItem(who, runtime.RequiredItemId);

            PuzzleDenyReason reason = Refine(PuzzleRules.CheckOp(facts), havePawn);
            if (reason != PuzzleDenyReason.None)
            {
                return Denied("op", slot, who, reason);
            }
            if (!runtime.State.TryApply(op))
            {
                return Denied("op", slot, who, PuzzleDenyReason.InvalidOp);
            }

            _net.Broadcast(PuzzleStateChangedMsg.Encode(slot, runtime.SnapshotValues(), who));
            return PuzzleDenyReason.None;
        }

        /// <summary>
        /// Authority only. Evil jinn mass sabotage (Teknik §7.3): rules (<see cref="PuzzleRules.CheckScramble"/>), pay
        /// <c>ScrambleEnergyCost</c>, <see cref="PuzzleState.TryScrambleAll"/> with the round RNG, cooldown = Net.Time +
        /// <c>ScramblePuzzleCooldown</c>, PuzzleScrambled broadcast. Returns None when it happened; otherwise the reason
        /// (no energy is spent, nothing changes, nothing is sent).
        /// </summary>
        public PuzzleDenyReason ProcessScrambleAuthority(PlayerId who, int slot)
        {
            Debug.Assert(_net.IsAuthority, "ProcessScrambleAuthority must only run on the authority");
            if (!_slots.TryGetValue(slot, out PuzzleSlotRuntime runtime))
            {
                return Denied("scramble", slot, who, PuzzleDenyReason.UnknownSlot);
            }

            IJinnEnergyService energy = GameServices.Get<IJinnEnergyService>();
            float cost = _config.ScrambleEnergyCost;
            double now = _net.Time;

            bool havePawn = TryMeasureRange(who, runtime.Position, _config.ScrambleRange, out bool inRange);
            PuzzleCheckFacts facts = default;
            facts.Role = _players.GetRole(who);
            facts.JinnsAwake = GameServices.Get<IMatchInfo>().JinnsAwake;
            facts.IsLocked = runtime.State.IsLocked;
            facts.InRange = inRange;
            facts.RoomLight = GameServices.Get<IRoomService>().LightOf(runtime.Room);
            facts.InLanternSafeZone = GameServices.Get<IHumanStatusQuery>().IsInLanternSafeZone(runtime.Position);
            facts.Now = now;
            facts.ScrambleCooldownUntil = runtime.State.ScrambleCooldownUntil;
            facts.Energy = energy.Get(who);
            facts.EnergyCost = cost;

            PuzzleDenyReason reason = Refine(PuzzleRules.CheckScramble(facts), havePawn);
            if (reason != PuzzleDenyReason.None)
            {
                return Denied("scramble", slot, who, reason);
            }
            if (!energy.TrySpendAuthority(who, cost))
            {
                return Denied("scramble", slot, who, PuzzleDenyReason.InsufficientEnergy);
            }
            if (!runtime.State.TryScrambleAll(_rng))
            {
                // Unreachable: the rules refused a locked puzzle just above, and TryScrambleAll only fails when locked.
                CMLog.Error(Category, "TryScrambleAll refused slot " + slot + " after the rules passed it");
                return PuzzleDenyReason.Locked;
            }

            double cooldownUntil = now + _config.ScramblePuzzleCooldown;
            runtime.State.ScrambleCooldownUntil = cooldownUntil;
            _net.Broadcast(PuzzleScrambledMsg.Encode(slot, runtime.SnapshotValues(), who, cooldownUntil));
            return PuzzleDenyReason.None;
        }

        /// <summary>
        /// Authority only. Confirm lever (Teknik §7.3): rules (<see cref="PuzzleRules.CheckConfirm"/>), then
        /// <see cref="PuzzleState.TryLock"/>. Solved: PuzzleConfirmResult{ok} + PuzzleCompleted, returns None. Not solved: lever locked for
        /// <c>ConfirmFailCooldown</c>, PuzzleConfirmResult{fail}, then <c>ConfirmFailDamage</c> to the human (cause
        /// <see cref="ConfirmFailCauseId"/>), returns <see cref="PuzzleDenyReason.WrongAnswer"/>. Any other reason means the request was
        /// refused and nothing happened (no damage, no cooldown, nothing sent).
        /// </summary>
        public PuzzleDenyReason ProcessConfirmAuthority(PlayerId who, int slot)
        {
            Debug.Assert(_net.IsAuthority, "ProcessConfirmAuthority must only run on the authority");
            if (!_slots.TryGetValue(slot, out PuzzleSlotRuntime runtime))
            {
                return Denied("confirm", slot, who, PuzzleDenyReason.UnknownSlot);
            }

            double now = _net.Time;
            bool havePawn = TryMeasureRange(who, runtime.ConfirmPosition, _config.InteractRange, out bool inRange);
            PuzzleCheckFacts facts = default;
            facts.Role = _players.GetRole(who);
            facts.HumanFainted = GameServices.Get<IHumanStatusQuery>().IsFainted;
            facts.IsLocked = runtime.State.IsLocked;
            facts.InRange = inRange;
            facts.Now = now;
            facts.ConfirmCooldownUntil = runtime.ConfirmCooldownUntil;

            PuzzleDenyReason reason = Refine(PuzzleRules.CheckConfirm(facts), havePawn);
            if (reason != PuzzleDenyReason.None)
            {
                return Denied("confirm", slot, who, reason);
            }

            if (runtime.State.TryLock())
            {
                _net.Broadcast(PuzzleConfirmResultMsg.Encode(slot, true, who));
                _net.Broadcast(PuzzleCompletedMsg.Encode(slot, runtime.Stage));
                return PuzzleDenyReason.None;
            }

            runtime.ConfirmCooldownUntil = now + _config.ConfirmFailCooldown;
            _net.Broadcast(PuzzleConfirmResultMsg.Encode(slot, false, who));
            ApplyConfirmFailDamage(who, runtime);
            return PuzzleDenyReason.WrongAnswer;
        }

        // ---- Debug ----

        /// <summary>JSON array of every slot for DebugState ("puzzles"): slot, stage, room, values, solved, locked, cooldowns left (s).</summary>
        public string BuildDebugJson()
        {
            var keys = new List<int>(_slots.Keys);
            keys.Sort();
            double now = _net.Time;

            var sb = new StringBuilder(256);
            sb.Append('[');
            for (int i = 0; i < keys.Count; i++)
            {
                PuzzleSlotRuntime runtime = _slots[keys[i]];
                PuzzleState state = runtime.State;
                if (i > 0)
                {
                    sb.Append(',');
                }
                sb.Append("{\"slot\":").Append(keys[i].ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"stage\":").Append(runtime.Stage.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"room\":").Append(runtime.Room.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"values\":[");
                for (int part = 0; part < state.PartCount; part++)
                {
                    if (part > 0)
                    {
                        sb.Append(',');
                    }
                    sb.Append(state.Get(part).ToString(CultureInfo.InvariantCulture));
                }
                sb.Append("],\"solved\":").Append(state.IsSolved ? "true" : "false");
                sb.Append(",\"locked\":").Append(state.IsLocked ? "true" : "false");
                sb.Append(",\"scrambleCooldownLeft\":").Append(Left(state.ScrambleCooldownUntil, now));
                sb.Append(",\"confirmCooldownLeft\":").Append(Left(runtime.ConfirmCooldownUntil, now));
                sb.Append('}');
            }
            sb.Append(']');
            return sb.ToString();
        }

        private static string Left(double until, double now)
        {
            double left = until - now;
            return (left > 0d ? left : 0d).ToString(TimeFormat, CultureInfo.InvariantCulture);
        }

        // ---- Request handlers (acted on by the authority only) ----

        private void OnReqPuzzleOp(NetMsg msg)
        {
            if (!_net.IsAuthority)
            {
                return;
            }
            if (!ReqPuzzleOpMsg.TryDecode(msg, out int slot, out PuzzleOp op))
            {
                CMLog.Warn(Category, "Malformed ReqPuzzleOp ignored");
                return;
            }
            ProcessOpAuthority(msg.Sender, slot, op);
        }

        private void OnReqPuzzleScramble(NetMsg msg)
        {
            if (!_net.IsAuthority)
            {
                return;
            }
            if (!ReqPuzzleScrambleMsg.TryDecode(msg, out int slot))
            {
                CMLog.Warn(Category, "Malformed ReqPuzzleScramble ignored");
                return;
            }
            ProcessScrambleAuthority(msg.Sender, slot);
        }

        private void OnReqPuzzleConfirm(NetMsg msg)
        {
            if (!_net.IsAuthority)
            {
                return;
            }
            if (!ReqPuzzleConfirmMsg.TryDecode(msg, out int slot))
            {
                CMLog.Warn(Category, "Malformed ReqPuzzleConfirm ignored");
                return;
            }
            ProcessConfirmAuthority(msg.Sender, slot);
        }

        // ---- Broadcast handlers (every machine, the authority included) ----

        private void OnPuzzleStateChanged(NetMsg msg)
        {
            if (!PuzzleStateChangedMsg.TryDecode(msg, out int slot, out int[] values, out PlayerId by))
            {
                CMLog.Warn(Category, "Malformed PuzzleStateChanged ignored");
                return;
            }
            if (!TryMirror(slot, values, "PuzzleStateChanged", out PuzzleSlotRuntime _))
            {
                return;
            }
            EventBus.Publish(new PuzzleStateChangedEvt(slot, by));
        }

        private void OnPuzzleScrambled(NetMsg msg)
        {
            if (!PuzzleScrambledMsg.TryDecode(msg, out int slot, out int[] values, out PlayerId by, out double cooldownUntil))
            {
                CMLog.Warn(Category, "Malformed PuzzleScrambled ignored");
                return;
            }
            if (!TryMirror(slot, values, "PuzzleScrambled", out PuzzleSlotRuntime runtime))
            {
                return;
            }
            runtime.State.ScrambleCooldownUntil = cooldownUntil;
            EventBus.Publish(new PuzzleScrambledEvt(slot, by, cooldownUntil));
        }

        private void OnPuzzleCompleted(NetMsg msg)
        {
            if (!PuzzleCompletedMsg.TryDecode(msg, out int slot, out int stage))
            {
                CMLog.Warn(Category, "Malformed PuzzleCompleted ignored");
                return;
            }
            if (!_slots.TryGetValue(slot, out PuzzleSlotRuntime runtime))
            {
                CMLog.Warn(Category, "PuzzleCompleted for unregistered slot " + slot + " ignored");
                return;
            }

            PuzzleState state = runtime.State;
            if (!state.IsLocked && !state.TryLock())
            {
                // The authority only locks a solved puzzle, so this machine's copy drifted: its word is the truth.
                CMLog.Warn(Category, "Slot " + slot + " was not solved on this machine when PuzzleCompleted arrived - forced to the solution");
                var solution = new int[state.PartCount];
                for (int i = 0; i < solution.Length; i++)
                {
                    solution[i] = state.Solution[i];
                }
                state.ApplySnapshot(solution, true);
            }
            EventBus.Publish(new PuzzleCompletedEvt(slot, stage));
        }

        private void OnRoundStarted(RoundStartedEvt evt)
        {
            _rng = new GameRandom(PuzzleRules.ScrambleSeed(evt.Seed));
        }

        // ---- Helpers ----

        /// <summary>Applies an unlocked snapshot from the authority to the local copy of <paramref name="slot"/>. False (and a warning) when it cannot.</summary>
        private bool TryMirror(int slot, int[] values, string what, out PuzzleSlotRuntime runtime)
        {
            if (!_slots.TryGetValue(slot, out runtime))
            {
                CMLog.Warn(Category, what + " for unregistered slot " + slot + " ignored");
                return false;
            }
            if (!runtime.State.ApplySnapshot(values, false))
            {
                CMLog.Warn(Category, what + " for slot " + slot + " does not fit the local puzzle (wrong length or value) - ignored");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Range of the requester's pawn to <paramref name="target"/>. Returns false when the pawn cannot be found (inRange is then false);
        /// <see cref="DebugIgnoreRange"/> returns true / in range without looking.
        /// </summary>
        private bool TryMeasureRange(PlayerId who, Vector3 target, float range, out bool inRange)
        {
            if (DebugIgnoreRange)
            {
                inRange = true;
                return true;
            }

            Vector3? pawn = _pawnPosition(who);
            if (!pawn.HasValue)
            {
                inRange = false;
                return false;
            }
            inRange = PuzzleRules.IsInRange(pawn.Value, target, range, _config.NetRangeTolerance);
            return true;
        }

        /// <summary>A rules "out of range" caused by a missing pawn is reported as PawnMissing.</summary>
        private static PuzzleDenyReason Refine(PuzzleDenyReason reason, bool havePawn)
        {
            return reason == PuzzleDenyReason.OutOfRange && !havePawn ? PuzzleDenyReason.PawnMissing : reason;
        }

        private static PuzzleDenyReason Denied(string what, int slot, PlayerId who, PuzzleDenyReason reason)
        {
            CMLog.Info(Category, "Puzzle " + what + " denied (" + reason + "): slot " + slot + ", " + who);
            return reason;
        }

        private void ApplyConfirmFailDamage(PlayerId who, PuzzleSlotRuntime runtime)
        {
            int amount = _config.ConfirmFailDamage;
            if (amount <= 0)
            {
                return;
            }

            IDamageable target = _damageableOf(who);
            if (target == null)
            {
                CMLog.Warn(Category, "Wrong confirmation by " + who + " but no damageable pawn was found - no damage dealt");
                return;
            }

            var info = new DamageInfo
            {
                Amount = amount,
                Attacker = PlayerId.None,
                SourceObject = NetId.Invalid,
                CauseId = ConfirmFailCauseId,
                Flags = DamageFlags.None,
                KnockdownTime = 0f,
                Point = runtime.ConfirmPosition,
                Direction = Vector3.zero,
            };
            target.ApplyDamageAuthority(info);
        }
    }
}
