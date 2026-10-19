using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using CinliMahzen.Match;
using CinliMahzen.Player;
using UnityEngine;

namespace CinliMahzen.DebugTools
{
    /// <summary>
    /// Single entry point for debug actions — used by F-key hotkeys and the CinliMahzen/Debug menu (MCP).
    /// Features whose systems do not exist yet log a TODO.
    /// </summary>
    public static class DebugCommands
    {
        /// <summary>
        /// F1-F4 / "Switch To Player N" (TODO 1.3): makes player <paramref name="index"/> the played one. The registry raises
        /// LocalPlayerChanged (+ LocalRoleChangedEvt when the role differs); every pawn then re-evaluates whether it is the local one
        /// (input map, camera, cursor), and the camera culling follows the new role.
        /// </summary>
        public static void SwitchToPlayer(int index)
        {
            string what = "Switch To Player " + (index + 1);
            var registry = GameServices.Players as OfflinePlayerRegistry;
            if (registry == null)
            {
                CMLog.Warn("Debug", what + ": no offline player registry (not booted, or online)");
                return;
            }
            if (index < 0 || index >= registry.Players.Count || !registry.SetLocalPlayer(new PlayerId((byte)index)))
            {
                CMLog.Warn("Debug", what + ": there is no such player (0.." + (registry.Players.Count - 1) + ")");
                return;
            }
            CMLog.Info("Debug", "Local player -> " + registry.LocalPlayer + " (" + registry.GetRole(registry.LocalPlayer) + ")");
        }

        public static void ToggleImmortalHuman() => ToggleImmortal();
        public static void ToggleInfiniteEnergy() => Todo("F6 Infinite Energy", "1.7 Possession");
        public static void ToggleRevealAll() => Todo("F7 Reveal All", "3.9 Visibility");
        public static void RestartRound() => RestartMatch();
        public static void CompleteCurrentStage() => AddStageTimeBonus();
        public static void ToggleDummyHuman() => Todo("F12 Toggle Dummy Human", "3.12 Dummy bot");

        public static void ToggleOverlay()
        {
            DebugOverlay.Visible = !DebugOverlay.Visible;
            CMLog.Info("Debug", "Overlay " + (DebugOverlay.Visible ? "on" : "off"));
        }

        public static void ToggleTimeScale()
        {
            Time.timeScale = Time.timeScale > 1.5f ? 1f : 2f;
            CMLog.Info("Debug", "Time scale x" + Time.timeScale);
        }

        public static void DumpState()
        {
            CMLog.Info("Dump", DebugState.BuildJson());
        }

        /// <summary>
        /// Menu "Spin Human 180": the authority applies CameraSpin180 to the scene's human. The duration is the human's
        /// serialized debugSpinDuration (PD_Slipper's status duration), so no number lives here.
        /// </summary>
        public static void SpinHuman180()
        {
            INetBridge net = GameServices.Net;
            if (net == null || !net.IsAuthority)
            {
                CMLog.Warn("Debug", "Spin Human 180: this machine is not the authority");
                return;
            }
            HumanController human = Object.FindFirstObjectByType<HumanController>();
            if (human == null)
            {
                CMLog.Warn("Debug", "Spin Human 180: no HumanController in the scene");
                return;
            }
            human.ApplyStatusAuthority(StatusType.CameraSpin180, human.DebugSpinDuration, PlayerId.None);
        }

        /// <summary>
        /// Menu "Trigger Vision Blur": blurs the local camera for SpiritBumpBlurTime, as a Spirit Bump would (TODO 1.2).
        /// Goes straight to the camera, so it works for any local role.
        /// </summary>
        public static void TriggerVisionBlur()
        {
            ICameraRig rig = GameServices.Get<ICameraRig>();
            GameBalanceConfig config = GameServices.Config;
            if (rig == null || config == null)
            {
                CMLog.Warn("Debug", "Trigger Vision Blur: no ICameraRig / GameBalanceConfig (add the CameraRig to the scene first)");
                return;
            }
            rig.TriggerBlur(config.SpiritBumpBlurTime);
            CMLog.Info("Debug", "Vision blur for " + config.SpiritBumpBlurTime + " s");
        }

        /// <summary>
        /// Menu "Cycle Camera Mode": Fps -> Orbit -> Faint -> Fps around the scene's human, so the transitions, the orbit wall check
        /// and the faint view can be tried in Sandbox (TODO 1.2). The orbit reads the Possessed input map, so entering Orbit switches the
        /// local input to it (the human stops moving) and leaving it switches back to the Human map.
        /// </summary>
        public static void CycleCameraMode()
        {
            ICameraRig rig = GameServices.Get<ICameraRig>();
            HumanController human = Object.FindFirstObjectByType<HumanController>();
            if (rig == null || human == null || human.Head == null)
            {
                CMLog.Warn("Debug", "Cycle Camera Mode: needs an ICameraRig and a HumanController with a head in the scene");
                return;
            }

            LocalInputSource input = GameServices.Get<LocalInputSource>();
            switch (rig.Mode)
            {
                case CameraMode.Fps:
                    input?.SetMode(InputMode.Possessed);
                    rig.SetOrbit(human.transform, 0f);
                    break;
                case CameraMode.Orbit:
                    input?.SetMode(InputMode.Human);
                    rig.SetFaint(human.transform);
                    break;
                default:
                    rig.SetFps(human.Head);
                    break;
            }
            CMLog.Info("Debug", "Camera mode -> " + rig.Mode);
        }

        /// <summary>
        /// Menu "Faint Human": the authority hits the human for HumanMaxHp with IgnoreInvuln (CauseId "debug.faint"), so it faints
        /// regardless of the invulnerability window. A fainted human ignores further hits: revive it (Cologne menu or wait
        /// FaintDuration) before the next one. Three faints in a round end it with JinnsWin(Faint). F5 (Immortal) blocks it.
        /// </summary>
        public static void FaintHuman()
        {
            HumanHealth health = FindHumanHealth("Faint Human");
            GameBalanceConfig config = GameServices.Config;
            if (health == null || config == null)
                return;
            if (health.IsFainted)
            {
                CMLog.Warn("Debug", "Faint Human: the human is already fainted - revive it first (Revive Human With Cologne); after the last faint it stays down until a new round");
                return;
            }
            HitHuman(health, config.HumanMaxHp, DamageFlags.IgnoreInvuln, "debug.faint");
            CMLog.Info("Debug", "Faint Human: hp " + health.Hp + ", faints " + health.Faints);
        }

        /// <summary>Menu "Revive Human With Cologne": the authority wakes the fainted human at once with full HP (HumanRevived byCologne=true).</summary>
        public static void ReviveHumanWithCologne()
        {
            HumanHealth health = FindHumanHealth("Revive Human With Cologne");
            if (health == null)
                return;
            if (health.ReviveWithCologneAuthority())
                CMLog.Info("Debug", "Revive Human With Cologne: hp " + health.Hp + ", faints " + health.Faints);
            else
                CMLog.Warn("Debug", "Revive Human With Cologne: nothing to revive (the human is not fainted, or has already lost)");
        }

        /// <summary>Menu "Damage Human 1": a plain 1 HP hit (CauseId "debug.damage") that respects the invulnerability window.</summary>
        public static void DamageHuman1()
        {
            HumanHealth health = FindHumanHealth("Damage Human 1");
            if (health == null)
                return;
            HitHuman(health, 1, DamageFlags.None, "debug.damage");
            CMLog.Info("Debug", "Damage Human 1: hp " + health.Hp + ", faints " + health.Faints);
        }

        // ---- Match (TODO 1.10) ----

        /// <summary>
        /// Menu "Skip Match State": the authority pulls the end of the current timed match state to now (RoleReveal 3 s, Intro 5 s,
        /// RoundEnd 8 s - to shorten the waits in an MCP run; in Playing it ends the round by Timeout). RoundSetup and MatchEnd have no timer.
        /// </summary>
        public static void SkipMatchState()
        {
            MatchController match = FindMatchController("Skip Match State");
            if (match == null)
                return;
            MatchState before = match.State;
            if (match.SkipStateTimer())
                CMLog.Info("Debug", "Skip Match State: " + before + " -> " + match.State);
        }

        /// <summary>F8 / "Restart Round (New Seed)": the authority restarts the match with a new match seed (round 0, roles and layout re-rolled).</summary>
        private static void RestartMatch()
        {
            MatchController match = FindMatchController("Restart Round (New Seed)");
            if (match == null)
                return;
            if (match.RestartWithNewSeed())
                CMLog.Info("Debug", "Restart Round (New Seed): match restarted - round " + match.RoundIndex + ", round seed " + match.Seed);
        }

        /// <summary>
        /// F11 / "Complete Current Stage" (TODO 2.7): the authority opens the gate of the current stage through
        /// <see cref="IObjectiveDebugCommands"/> (GateOpened → door, StageCompletedEvt → MatchController adds StageTimeBonus). Without the
        /// objectives in the scene only the time half happens: StageTimeBonus is added to the Sunrise counter directly.
        /// </summary>
        private static void AddStageTimeBonus()
        {
            MatchController match = FindMatchController("Complete Current Stage");
            if (match == null)
                return;
            if (GameServices.TryGet(out IObjectiveDebugCommands objectives))
            {
                if (objectives.CompleteCurrentStageAuthority())
                    CMLog.Info("Debug", "Complete Current Stage: gate opened (+StageTimeBonus)");
                else
                    CMLog.Warn("Debug", "Complete Current Stage: the current stage's gate is already open");
                return;
            }
            if (match.AddStageTimeBonusAuthority())
                CMLog.Info("Debug", "Complete Current Stage: +StageTimeBonus (stage gates 2.7)");
            else
                CMLog.Warn("Debug", "Complete Current Stage: the match is not Playing (state " + match.State + ") - no time added");
        }

        /// <summary>The scene's MatchController, only on the authority (match state is authority-only). Logs and returns null otherwise.</summary>
        private static MatchController FindMatchController(string command)
        {
            INetBridge net = GameServices.Net;
            if (net == null || !net.IsAuthority)
            {
                CMLog.Warn("Debug", command + ": this machine is not the authority");
                return null;
            }
            var match = GameServices.Match as MatchController;
            if (match == null)
            {
                CMLog.Warn("Debug", command + ": no MatchController in the scene (CinliMahzen/Setup/Add Match Controller To Open Scene)");
                return null;
            }
            return match;
        }

        private static void ToggleImmortal()
        {
            HumanHealth health = FindHumanHealth("Immortal Human");
            if (health == null)
                return;
            health.Immortal = !health.Immortal;
            CMLog.Info("Debug", "Immortal Human " + (health.Immortal ? "on" : "off"));
        }

        private static void HitHuman(HumanHealth health, int amount, DamageFlags flags, string causeId)
        {
            var info = new DamageInfo
            {
                Amount = amount,
                Attacker = PlayerId.None,
                SourceObject = NetId.Invalid,
                CauseId = causeId,
                Flags = flags,
                KnockdownTime = 0f,
                Point = health.transform.position,
                Direction = Vector3.zero,
            };
            health.ApplyDamageAuthority(info);
        }

        /// <summary>The scene's HumanHealth, only on the authority (health changes are authority-only). Logs and returns null otherwise.</summary>
        private static HumanHealth FindHumanHealth(string command)
        {
            INetBridge net = GameServices.Net;
            if (net == null || !net.IsAuthority)
            {
                CMLog.Warn("Debug", command + ": this machine is not the authority");
                return null;
            }
            HumanHealth health = Object.FindFirstObjectByType<HumanHealth>();
            if (health == null)
            {
                CMLog.Warn("Debug", command + ": no HumanHealth in the scene (rebuild P_Human: CinliMahzen/Setup/Build Human Prefab)");
                return null;
            }
            return health;
        }

        private static void Todo(string what, string waitingFor)
        {
            CMLog.Info("Debug", what + ": TODO (" + waitingFor + ")");
        }
    }
}
