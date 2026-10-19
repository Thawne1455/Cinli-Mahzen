using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Objectives
{
    /// <summary>
    /// The puzzle service as a scene object (Teknik §7.4, TODO 2.4): the thin MonoBehaviour around <see cref="PuzzleSystemCore"/>.
    /// One per Game scene. It is also the scene's <see cref="IObjectiveInfo"/> (registered in OnEnable; the stub is back after
    /// OnDisable) and adds the "puzzles" section to Dump State.
    ///
    /// <para>All behaviour lives in the core: slots are registered with <see cref="RegisterSlot"/> / <see cref="ClearSlots"/> (the round
    /// setup applier, TODO 2.7), the three requests (<c>ReqPuzzleOp</c>, <c>ReqPuzzleScramble</c>, <c>ReqPuzzleConfirm</c>) are
    /// validated on the authority and answered with broadcasts every machine applies. This component only supplies what needs the scene:
    /// the position and the damage target of a player's pawn, found among the <see cref="PawnBase"/> objects by owner when a request arrives
    /// (never per frame).</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PuzzleSystem : MonoBehaviour, IObjectiveInfo
    {
        private const string DebugStateKey = "puzzles";

        private static PuzzleSystem s_debugOwner;

        private PuzzleSystemCore _core;

        /// <summary>The rules and state. Null when the bootstrap services were missing in Awake (the component is then inert).</summary>
        public PuzzleSystemCore Core => _core;

        // ---- IObjectiveInfo (forwarded to the core) ----

        public int StageIndex => _core != null ? _core.StageIndex : 0;

        public int StageCount => _core != null ? _core.StageCount : 0;

        public ObjectivePhase Phase => _core != null ? _core.Phase : ObjectivePhase.Garden;

        public float DigProgress => _core != null ? _core.DigProgress : 0f;

        public int DigCheckpoint => _core != null ? _core.DigCheckpoint : 0;

        public bool IsPuzzleDone(int slot)
        {
            return _core != null && _core.IsPuzzleDone(slot);
        }

        public bool IsPuzzleScrambleOnCooldown(int slot, out float remaining)
        {
            if (_core == null)
            {
                remaining = 0f;
                return false;
            }
            return _core.IsPuzzleScrambleOnCooldown(slot, out remaining);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_debugOwner = null;
        }

        // ---- Slots (forwarded) ----

        /// <summary>See <see cref="PuzzleSystemCore.RegisterSlot"/>. Ignored with an error when the component is inert.</summary>
        public void RegisterSlot(int slot, PuzzleSlotRuntime runtime)
        {
            if (_core == null)
            {
                CMLog.Error("Puzzle", name + ": inert (no GameServices.Net / Config / Players) - slot " + slot + " not registered");
                return;
            }
            _core.RegisterSlot(slot, runtime);
        }

        public void ClearSlots()
        {
            _core?.ClearSlots();
        }

        private void Awake()
        {
            INetBridge net = GameServices.Net;
            GameBalanceConfig config = GameServices.Config;
            IPlayerRegistry players = GameServices.Players;
            if (net == null || config == null || players == null)
            {
                CMLog.Error("Puzzle", name + ": GameServices.Net / Config / Players missing (is the bootstrap running?) - the puzzle system is inert");
                enabled = false;
                return;
            }

            _core = new PuzzleSystemCore(net, config, players, FindPawnPosition, FindDamageable);
        }

        private void OnEnable()
        {
            if (_core == null)
            {
                return;
            }

            _core.Attach();
            GameServices.Register<IObjectiveInfo>(this);
            s_debugOwner = this;
            DebugState.Register(DebugStateKey, _core.BuildDebugJson);
        }

        private void OnDisable()
        {
            if (_core == null)
            {
                return;
            }

            _core.Detach();
            GameServices.Unregister<IObjectiveInfo>(this);
            if (s_debugOwner == this)
            {
                DebugState.Unregister(DebugStateKey);
                s_debugOwner = null;
            }
        }

        /// <summary>Scene search over PawnBase by owner. Runs on a request only, never per frame.</summary>
        private static Vector3? FindPawnPosition(PlayerId who)
        {
            PawnBase pawn = FindPawn(who);
            return pawn != null ? pawn.transform.position : (Vector3?)null;
        }

        private static IDamageable FindDamageable(PlayerId who)
        {
            PawnBase pawn = FindPawn(who);
            return pawn != null ? pawn.GetComponent<IDamageable>() : null;
        }

        private static PawnBase FindPawn(PlayerId who)
        {
            PawnBase[] pawns = FindObjectsByType<PawnBase>(FindObjectsSortMode.None);
            for (int i = 0; i < pawns.Length; i++)
            {
                if (pawns[i].Owner == who)
                {
                    return pawns[i];
                }
            }
            return null;
        }
    }
}
