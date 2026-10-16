using CinliMahzen.Core;
using CinliMahzen.Core.Net;
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
        public static void SwitchToPlayer(int index)
        {
            Todo("F" + (index + 1) + " Switch To Player " + (index + 1), "1.3 Hotseat");
        }

        public static void ToggleImmortalHuman() => ToggleImmortal();
        public static void ToggleInfiniteEnergy() => Todo("F6 Infinite Energy", "1.7 Possession");
        public static void ToggleRevealAll() => Todo("F7 Reveal All", "3.9 Visibility");
        public static void RestartRound() => Todo("F8 Restart Round (New Seed)", "1.10 Match");
        public static void CompleteCurrentStage() => Todo("F11 Complete Current Stage (+StageTimeBonus)", "2.7 StageGate");
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
