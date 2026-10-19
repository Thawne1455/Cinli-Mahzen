using System;
using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Possession
{
    /// <summary>
    /// Authority side of the possessed objects' actions (Teknik §6.3–6.4, TODO 1.8), plain C#. Drives the Charging / Recovering half of each
    /// object's <see cref="PossessableStateMachine"/> (the arbiter of <see cref="PossessionAuthority"/>) and turns every step into a broadcast:
    /// <code>
    /// ReqAction{index, aim} ─► inside + Lurking + defined + usable by the role + cooldown + energy ─► ActionTelegraph{obj, index, resolveAt}
    /// ChargeReady (timer)   ─► behaviour by action id ─► damage / status / effect ─► ActionResolved{obj, index, hit, spent, end pose, recoverUntil}
    ///                          projectile actions also ─► ProjectileSpawned{path} … TickAuthority steps it ─► ProjectileImpact{point, hitHuman}
    /// RecoverFinished       ─► ObjectStateChanged{obj, Lurking}
    /// PossessedMove{dir}    ─► hop (movable objects): ObjectMoved{obj, position, yaw}
    /// </code>
    /// Behaviours (action <c>id</c>): <c>throw</c> / <c>homing_slipper</c> / <c>fling</c> (projectile along the aim; arc from the asset,
    /// 0 = straight), <c>lunge</c> (dash + box hit at the end), <c>slide</c> (harmless push, good jinn too), <c>topple</c> (box in front:
    /// damage + knockdown), <c>burst</c> (lamp broken through <see cref="ILightService"/>), <c>play_tune</c> (RadioStateChanged: the room is
    /// muted for <c>effectDuration</c>), <c>revive_splash</c> (good jinn, fainted human within <c>CologneReviveRange</c>: CologneReviveTriggered).
    /// Moving actions keep their direction within ±<c>YawLimitDeg</c> of the object's facing; walls stop them (<see cref="IActionWorld.FreeDistance"/>).
    /// The Spirit Block of the cellar finale arrives with TODO 3.7.
    /// </summary>
    public sealed class PossessionActionRunner
    {
        private const string Category = "Action";
        private const float MoveRadius = 0.25f;
        private const float MinHop = 0.05f;
        private const float ThrowHeight = 0.3f;

        private readonly INetBridge _net;
        private readonly PossessionAuthority _authority;
        private readonly IActionWorld _world;
        private readonly float _cologneReviveRange;
        private readonly float _rangeTolerance;
        private readonly CooldownTracker _cooldowns = new CooldownTracker();
        private readonly Dictionary<NetId, Vector3> _aims = new Dictionary<NetId, Vector3>();
        private readonly Dictionary<NetId, double> _hopReadyAt = new Dictionary<NetId, double>();
        private readonly List<Flight> _flights = new List<Flight>(4);

        private readonly Action<NetMsg> _onReqAction;
        private readonly Action<NetMsg> _onReqMove;
        private readonly Action<NetMsg> _onRage;
        private readonly Action<PossessionTransitionInfo> _onTransition;
        private readonly Action<RoundStartedEvt> _onRoundStarted;
        private bool _attached;

        private struct Flight
        {
            public NetId Obj;
            public PlayerId By;
            public ProjectilePath Path;
            public Vector3 Last;
            public PossessableActionDef Action;
        }

        public PossessionActionRunner(INetBridge net, PossessionAuthority authority, IActionWorld world, float cologneReviveRange, float rangeTolerance)
        {
            _net = net ?? throw new ArgumentNullException(nameof(net));
            _authority = authority ?? throw new ArgumentNullException(nameof(authority));
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _cologneReviveRange = cologneReviveRange;
            _rangeTolerance = rangeTolerance;

            _onReqAction = OnReqAction;
            _onReqMove = OnReqMove;
            _onRage = OnRageStarted;
            _onTransition = OnTransition;
            _onRoundStarted = OnRoundStarted;
        }

        /// <summary>Spirit Block (cellar): radius (m) of the good jinn's body and the energy one block costs. 0 radius = off.</summary>
        public float SpiritBlockRadius { get; set; }

        public float SpiritBlockCost { get; set; }

        /// <summary>Öfke: action cooldowns and re-enter cooldowns are scaled by this while RageStarted is on (RageCooldownMult).</summary>
        public float RageCooldownMult { get; set; } = 1f;

        /// <summary>Projectiles in the air (authority).</summary>
        public int FlightCount => _flights.Count;

        public void Attach()
        {
            if (_attached)
                return;
            _attached = true;
            _net.Register(MsgCode.ReqAction, _onReqAction);
            _net.Register(MsgCode.PossessedMove, _onReqMove);
            _net.Register(MsgCode.RageStarted, _onRage);
            _authority.TransitionOccurred += _onTransition;
            EventBus.Subscribe(_onRoundStarted);
        }

        public void Detach()
        {
            if (!_attached)
                return;
            _attached = false;
            _net.Unregister(MsgCode.ReqAction, _onReqAction);
            _net.Unregister(MsgCode.PossessedMove, _onReqMove);
            _net.Unregister(MsgCode.RageStarted, _onRage);
            _authority.TransitionOccurred -= _onTransition;
            EventBus.Unsubscribe(_onRoundStarted);
        }

        /// <summary>Ready time of an object's action (Net.Time basis; negative infinity when it was never used this round).</summary>
        public double CooldownReadyAt(NetId obj, int index)
        {
            return _cooldowns.ReadyAt(CooldownTracker.Key(obj.Value, index));
        }

        // ---- authority entry points ----

        /// <summary>Authority only. Starts the charge of action <paramref name="index"/> of the object <paramref name="who"/> is in.</summary>
        public ActionDenyReason ProcessActionAuthority(PlayerId who, int index, Vector3 aim)
        {
            Debug.Assert(_net.IsAuthority, "ProcessActionAuthority must only run on the authority");
            ActionDenyReason reason = TryStart(who, index, aim);
            if (reason != ActionDenyReason.None)
            {
                CMLog.Info(Category, "Action " + index + " of " + who + " denied: " + reason);
                _net.Broadcast(ActionDeniedMsg.Encode(who, reason));
            }
            return reason;
        }

        /// <summary>Authority only. One hop of the movable object <paramref name="who"/> is in, along <paramref name="direction"/>.</summary>
        public ActionDenyReason ProcessHopAuthority(PlayerId who, Vector3 direction)
        {
            Debug.Assert(_net.IsAuthority, "ProcessHopAuthority must only run on the authority");
            ActionDenyReason reason = TryHop(who, direction);
            if (reason != ActionDenyReason.None && reason != ActionDenyReason.Cooldown)
                _net.Broadcast(ActionDeniedMsg.Encode(who, reason));
            return reason;
        }

        /// <summary>Authority only, every frame: steps the projectiles in the air.</summary>
        public void TickAuthority(double now)
        {
            if (!_net.IsAuthority)
                return;
            for (int i = _flights.Count - 1; i >= 0; i--)
            {
                Flight f = _flights[i];
                Vector3 next = f.Path.PositionAt(now);
                float radius = Mathf.Max(0.05f, f.Action.ShapeSize.x);
                if (TrySpiritBlock(f, next))
                {
                    _flights.RemoveAt(i);
                    continue;
                }
                if (_world.CastSegment(f.Last, next, radius, out Vector3 point, out HumanTarget target))
                {
                    _flights.RemoveAt(i);
                    Impact(f, point, target);
                    continue;
                }
                if (f.Path.IsFinished(now))
                {
                    _flights.RemoveAt(i);
                    // Out of range: the object drops where it is.
                    float down = _world.FreeDistance(next, Vector3.down, radius, 10f);
                    Impact(f, next + Vector3.down * down, default);
                    continue;
                }
                f.Last = next;
                _flights[i] = f;
            }
        }

        // ---- internals ----

        private ActionDenyReason TryStart(PlayerId who, int index, Vector3 aim)
        {
            PossessionArbiterCore arbiter = _authority.Arbiter;
            if (!arbiter.TryGetPossessed(who, out NetId obj) || !arbiter.TryGetMachine(obj, out PossessableStateMachine sm))
                return ActionDenyReason.NotInside;
            if (sm.State != PossessableState.Lurking)
                return ActionDenyReason.Busy;
            if (!_world.TryGetObject(obj, out PossessableDefinition def, out _, out _) || def == null)
                return ActionDenyReason.NoSuchAction;
            PossessableActionDef action = def.GetAction(index);
            if (action == null || !action.IsDefined)
                return ActionDenyReason.NoSuchAction;
            if (!action.IsUsableBy(_world.GetRole(who)))
                return ActionDenyReason.NotUsable;

            double now = _net.Time;
            if (!_cooldowns.IsReady(CooldownTracker.Key(obj.Value, index), now))
                return ActionDenyReason.Cooldown;
            IJinnEnergyService energy = GameServices.Get<IJinnEnergyService>();
            if (energy != null && !energy.TrySpendAuthority(who, action.EnergyCost))
                return ActionDenyReason.NotEnoughEnergy;

            double resolveAt = now + action.TelegraphTime;
            sm.BeginCharge(index, resolveAt);
            _aims[obj] = aim;
            _net.Broadcast(ActionTelegraphMsg.Encode(obj, index, resolveAt));
            return ActionDenyReason.None;
        }

        private ActionDenyReason TryHop(PlayerId who, Vector3 direction)
        {
            PossessionArbiterCore arbiter = _authority.Arbiter;
            if (!arbiter.TryGetPossessed(who, out NetId obj) || !arbiter.TryGetMachine(obj, out PossessableStateMachine sm))
                return ActionDenyReason.NotInside;
            if (sm.State != PossessableState.Lurking)
                return ActionDenyReason.Busy;
            if (!_world.TryGetObject(obj, out PossessableDefinition def, out Vector3 position, out _) || def == null || !def.CanMove)
                return ActionDenyReason.NoSuchAction;

            Vector3 flat = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (flat.sqrMagnitude < 1e-6f)
                return ActionDenyReason.Blocked;
            flat.Normalize();

            double now = _net.Time;
            if (_hopReadyAt.TryGetValue(obj, out double ready) && now < ready)
                return ActionDenyReason.Cooldown;

            float distance = _world.FreeDistance(position + Vector3.up * MoveRadius, flat, MoveRadius, def.HopDistance);
            if (distance < MinHop)
                return ActionDenyReason.Blocked;
            IJinnEnergyService energy = GameServices.Get<IJinnEnergyService>();
            if (energy != null && !energy.TrySpendAuthority(who, def.HopEnergy))
                return ActionDenyReason.NotEnoughEnergy;

            _hopReadyAt[obj] = now + def.HopInterval;
            _net.Broadcast(ObjectMovedMsg.Encode(obj, position + flat * distance, YawOf(flat)));
            return ActionDenyReason.None;
        }

        private void OnTransition(PossessionTransitionInfo t)
        {
            if (t.Kind == PossessableTransition.ChargeReady)
                Resolve(t.Object, t.Player);
            else if (t.Kind == PossessableTransition.RecoverFinished)
                _net.Broadcast(ObjectStateChangedMsg.Encode(t.Object, PossessionPhase.Lurking));
        }

        private void Resolve(NetId obj, PlayerId who)
        {
            PossessionArbiterCore arbiter = _authority.Arbiter;
            if (!arbiter.TryGetMachine(obj, out PossessableStateMachine sm) || sm.State != PossessableState.Charging)
                return;
            int index = sm.ActionIndex;
            if (!_world.TryGetObject(obj, out PossessableDefinition def, out Vector3 position, out float yaw) || def == null)
            {
                sm.CancelCharge();
                return;
            }
            PossessableActionDef action = def.GetAction(index);
            if (action == null || !action.IsDefined)
            {
                sm.CancelCharge();
                return;
            }

            double now = _net.Time;
            Vector3 aim = _aims.TryGetValue(obj, out Vector3 a) ? a : Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            _aims.Remove(obj);

            bool hit = false;
            Vector3 endPosition = position;
            float endYaw = yaw;
            float travel = 0f;
            HumanTarget target = default;

            switch (action.Id)
            {
                case "throw":
                case "homing_slipper":
                case "fling":
                {
                    Vector3 dir = aim.sqrMagnitude > 1e-6f ? aim.normalized : Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                    Vector3 origin = position + Vector3.up * ThrowHeight;
                    var path = new ProjectilePath(origin, dir, action.ProjectileSpeed, action.ProjectileRange, action.ProjectileArc, now);
                    _flights.Add(new Flight { Obj = obj, By = who, Path = path, Last = origin, Action = action });
                    _net.Broadcast(ProjectileSpawnedMsg.Encode(obj, path));
                    travel = path.Duration;
                    break;
                }

                case "lunge":
                case "slide":
                {
                    Vector3 dir = ClampToFacing(aim, yaw, def.YawLimitDeg);
                    float distance = _world.FreeDistance(position + Vector3.up * MoveRadius, dir, MoveRadius, action.ProjectileRange);
                    endPosition = position + dir * distance;
                    endYaw = YawOf(dir);
                    travel = action.ProjectileSpeed > 0f ? distance / action.ProjectileSpeed : 0f;
                    if (action.Damage > 0)
                        hit = FindInShape(action, endPosition, endYaw, out target);
                    break;
                }

                case "topple":
                    hit = FindInShape(action, position, yaw, out target);
                    break;

                case "burst":
                    GameServices.Get<ILightService>()?.SetLampAuthority(obj, LampState.Broken);
                    break;

                case "play_tune":
                    _net.Broadcast(RadioStateChangedMsg.Encode(Mathf.Max(0, _world.RoomOf(position)), now + action.EffectDuration));
                    break;

                case "revive_splash":
                    if (_world.IsHumanFainted && Vector3.Distance(_world.HumanPosition, position) <= _cologneReviveRange + _rangeTolerance)
                    {
                        hit = true;
                        _net.Broadcast(CologneReviveTriggeredMsg.Encode(who));
                    }
                    break;

                default:
                    CMLog.Warn(Category, "Unknown action id '" + action.Id + "' on " + def.name + " - resolved without effect");
                    break;
            }

            if (hit && target.IsValid)
                ApplyHit(action, obj, who, endPosition, target);

            double recoverUntil = now + travel + action.RecoverTime;
            sm.Resolve(action.SingleUse, recoverUntil);
            _cooldowns.Start(CooldownTracker.Key(obj.Value, index), now, action.Cooldown);
            _net.Broadcast(ActionResolvedMsg.Encode(obj, index, hit, action.SingleUse, endPosition, endYaw, recoverUntil));
        }

        private bool FindInShape(PossessableActionDef action, Vector3 position, float yaw, out HumanTarget target)
        {
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 size = action.ShapeSize;
            Vector3 offset = action.ShapeOffset;
            // No authored offset: the box stands right in front of the object, on the floor.
            if (offset == Vector3.zero)
                offset = new Vector3(0f, size.y * 0.5f, size.z * 0.5f);
            return _world.FindHumanInBox(position + rotation * offset, size * 0.5f, rotation, out target);
        }

        /// <summary>
        /// TODO 3.7: in the cellar a projectile that passes through the good jinn's spirit stops there, harmless, if the good jinn can pay
        /// SpiritBlockCost (SpiritBlockedProjectile, then the object drops: ProjectileImpact without a hit).
        /// </summary>
        private bool TrySpiritBlock(Flight f, Vector3 next)
        {
            if (SpiritBlockRadius <= 0f || !_world.IsCellarPhase)
                return false;
            if (!_world.CastGoodJinn(f.Last, next, SpiritBlockRadius, out PlayerId good, out Vector3 point))
                return false;
            IJinnEnergyService energy = GameServices.Get<IJinnEnergyService>();
            if (energy != null && !energy.TrySpendAuthority(good, SpiritBlockCost))
                return false;
            _net.Broadcast(SpiritBlockedMsg.Encode(good, f.Obj, point));
            float down = _world.FreeDistance(point, Vector3.down, 0.1f, 10f);
            _net.Broadcast(ProjectileImpactMsg.Encode(f.Obj, point + Vector3.down * down, false));
            CMLog.Info(Category, good + " spirit-blocked " + f.Obj);
            return true;
        }

        private void OnRageStarted(NetMsg msg)
        {
            if (!_net.IsAuthority || !CellarMsgs.TryDecodeRage(msg, out bool active))
                return;
            float mult = active ? RageCooldownMult : 1f;
            double now = _net.Time;
            _cooldowns.SetMultiplier(mult, now);
            _authority.Arbiter.SetCooldownMultiplier(mult, now);
        }

        private void Impact(Flight f, Vector3 point, HumanTarget target)
        {
            bool hitHuman = target.IsValid;
            if (hitHuman)
                ApplyHit(f.Action, f.Obj, f.By, point, target);
            _net.Broadcast(ProjectileImpactMsg.Encode(f.Obj, point, hitHuman));
        }

        private static void ApplyHit(PossessableActionDef action, NetId obj, PlayerId who, Vector3 point, HumanTarget target)
        {
            if (action.Damage > 0)
            {
                var info = new DamageInfo
                {
                    Amount = action.Damage,
                    Attacker = who,
                    SourceObject = obj,
                    CauseId = action.CauseId,
                    Flags = action.Flags,
                    KnockdownTime = action.KnockdownTime,
                    Point = point,
                    Direction = Vector3.zero,
                };
                target.Damageable.ApplyDamageAuthority(info);
            }
            if (action.ApplyStatus != StatusType.None && target.Status != null)
                target.Status.ApplyStatusAuthority(action.ApplyStatus, action.StatusDuration, who);
        }

        /// <summary>The aim flattened and turned at most ±<paramref name="limitDeg"/> away from the object's facing (A/D limited rotation).</summary>
        public static Vector3 ClampToFacing(Vector3 aim, float yaw, float limitDeg)
        {
            Vector3 facing = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            Vector3 flat = Vector3.ProjectOnPlane(aim, Vector3.up);
            if (flat.sqrMagnitude < 1e-6f)
                return facing;
            float angle = Vector3.SignedAngle(facing, flat, Vector3.up);
            angle = Mathf.Clamp(angle, -limitDeg, limitDeg);
            return Quaternion.Euler(0f, yaw + angle, 0f) * Vector3.forward;
        }

        public static float YawOf(Vector3 direction)
        {
            return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }

        private void OnReqAction(NetMsg msg)
        {
            if (!_net.IsAuthority)
                return;
            if (!ReqActionMsg.TryDecode(msg, out int index, out Vector3 aim))
            {
                CMLog.Warn(Category, "Malformed ReqAction from " + msg.Sender + " ignored");
                return;
            }
            ProcessActionAuthority(msg.Sender, index, aim);
        }

        private void OnReqMove(NetMsg msg)
        {
            if (!_net.IsAuthority)
                return;
            if (!ReqPossessedMoveMsg.TryDecode(msg, out Vector3 direction))
            {
                CMLog.Warn(Category, "Malformed PossessedMove from " + msg.Sender + " ignored");
                return;
            }
            ProcessHopAuthority(msg.Sender, direction);
        }

        private void OnRoundStarted(RoundStartedEvt e)
        {
            _cooldowns.Reset();
            _aims.Clear();
            _hopReadyAt.Clear();
            _flights.Clear();
        }
    }
}
