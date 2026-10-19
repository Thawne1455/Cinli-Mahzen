using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using UnityEngine;

namespace CinliMahzen.Objectives
{
    /// <summary>
    /// The interactive puzzle in the world (Teknik §7.4, TODO 2.5), root of <c>P_Puzzle_*</c>. Thin view over a slot of
    /// <see cref="PuzzleSystemCore"/>: <see cref="Bind"/> (round setup applier, debug menu) names the slot and the puzzle's ordinal, gives the
    /// parts and the lever their <see cref="PuzzleNetIds"/> and registers them as entities; afterwards every
    /// <see cref="PuzzleStateChangedEvt"/> / <see cref="PuzzleScrambledEvt"/> / <see cref="PuzzleCompletedEvt"/> of that slot redraws the
    /// parts from the state (every machine applies the same broadcast, so every machine draws the same thing). A mass sabotage also shakes
    /// the puzzle for a moment (cosmetic; the creak sound arrives with TODO 4.4).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PuzzleView : MonoBehaviour
    {
        [Tooltip("In part index order (PuzzleState part 0 first).")]
        [SerializeField] private PuzzlePart[] parts;
        [SerializeField] private ConfirmLever lever;
        [Tooltip("Shaken after a mass sabotage. Empty = no shake.")]
        [SerializeField] private Transform shakeRoot;
        [Tooltip("Cosmetic shake after a sabotage (s).")]
        [SerializeField] private float scrambleShakeTime = 0.6f;
        [Tooltip("Cosmetic shake amplitude (m).")]
        [SerializeField] private float scrambleShakeAmplitude = 0.02f;

        private PuzzleSystemCore _core;
        private int _slot;
        private int _ordinal = -1;
        private bool _bound;
        private float _shakeLeft;
        private Vector3 _shakeRest;

        public bool IsBound => _bound && _core != null;

        public int Slot => _slot;

        public int Ordinal => _ordinal;

        public PuzzleSystemCore Core => _core;

        public int PartCount => parts != null ? parts.Length : 0;

        public PuzzlePart GetPart(int index)
        {
            return parts[index];
        }

        /// <summary>True once the puzzle is confirmed (nobody can touch it any more).</summary>
        public bool IsLocked
        {
            get
            {
                PuzzleSystemCore core = Core;
                return core != null && core.TryGetSlot(_slot, out PuzzleSlotRuntime runtime) && runtime.State.IsLocked;
            }
        }

        /// <summary>
        /// Connects the view to <paramref name="slot"/> of <paramref name="core"/> (the scene PuzzleSystem's Core; register the slot first). <paramref name="ordinal"/> is the
        /// puzzle's index in the round plan (0..<see cref="PuzzleNetIds.MaxPuzzles"/>-1); it decides the NetIds. False (and an error) when the
        /// slot is unknown, the ordinal is out of range or the part count does not match the state.
        /// </summary>
        public bool Bind(PuzzleSystemCore core, int slot, int ordinal)
        {
            Unbind();
            if (core == null || !core.TryGetSlot(slot, out PuzzleSlotRuntime runtime))
            {
                CMLog.Error("Puzzle", name + ": cannot bind - slot " + slot + " is not registered");
                return false;
            }
            if (!PuzzleNetIds.IsValidOrdinal(ordinal))
            {
                CMLog.Error("Puzzle", name + ": ordinal " + ordinal + " is out of range");
                return false;
            }
            if (PartCount != runtime.State.PartCount || PartCount > PuzzleNetIds.MaxParts)
            {
                CMLog.Error("Puzzle", name + ": " + PartCount + " PuzzleParts but the state has " + runtime.State.PartCount + " parts");
                return false;
            }

            _core = core;
            _slot = slot;
            _ordinal = ordinal;
            _bound = true;

            IEntityRegistry entities = GameServices.Entities;
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i].Bind(this, i, PuzzleNetIds.Part(ordinal, i));
                entities?.Register(parts[i]);
            }
            if (lever != null)
            {
                lever.Bind(this, PuzzleNetIds.Lever(ordinal));
                entities?.Register(lever);
            }

            EventBus.Subscribe<PuzzleStateChangedEvt>(OnStateChanged);
            EventBus.Subscribe<PuzzleScrambledEvt>(OnScrambled);
            EventBus.Subscribe<PuzzleCompletedEvt>(OnCompleted);
            Refresh();
            return true;
        }

        public void Unbind()
        {
            if (!_bound)
                return;
            _bound = false;
            EventBus.Unsubscribe<PuzzleStateChangedEvt>(OnStateChanged);
            EventBus.Unsubscribe<PuzzleScrambledEvt>(OnScrambled);
            EventBus.Unsubscribe<PuzzleCompletedEvt>(OnCompleted);

            IEntityRegistry entities = GameServices.Entities;
            if (entities != null)
            {
                for (int i = 0; i < parts.Length; i++)
                    entities.Unregister(parts[i]);
                if (lever != null)
                    entities.Unregister(lever);
            }
            StopShake();
        }

        /// <summary>Draws every part from the current state (and the lever from the lock).</summary>
        public void Refresh()
        {
            PuzzleSystemCore core = Core;
            if (core == null || !core.TryGetSlot(_slot, out PuzzleSlotRuntime runtime))
                return;
            PuzzleState state = runtime.State;
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Display != null)
                    parts[i].Display.Show(state.Get(i));
            }
            if (lever != null)
                lever.ShowDone(state.IsLocked);
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void OnStateChanged(PuzzleStateChangedEvt e)
        {
            if (e.PuzzleSlot == _slot)
                Refresh();
        }

        private void OnScrambled(PuzzleScrambledEvt e)
        {
            if (e.PuzzleSlot != _slot)
                return;
            Refresh();
            if (shakeRoot != null && scrambleShakeTime > 0f)
            {
                if (_shakeLeft <= 0f)
                    _shakeRest = shakeRoot.localPosition;
                _shakeLeft = scrambleShakeTime;
            }
        }

        private void OnCompleted(PuzzleCompletedEvt e)
        {
            if (e.PuzzleSlot == _slot)
                Refresh();
        }

        private void Update()
        {
            if (_shakeLeft <= 0f)
                return;
            _shakeLeft -= Time.deltaTime;
            if (_shakeLeft <= 0f)
            {
                StopShake();
                return;
            }
            // Cosmetic only (CLAUDE.md rule 4 allows UnityEngine.Random here).
            shakeRoot.localPosition = _shakeRest + Random.insideUnitSphere * scrambleShakeAmplitude;
        }

        private void StopShake()
        {
            if (shakeRoot != null && _shakeLeft != 0f)
                shakeRoot.localPosition = _shakeRest;
            _shakeLeft = 0f;
        }
    }
}
