using System;
using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.Objectives
{
    /// <summary>
    /// Runtime record of one puzzle slot (Teknik §7.3), plain C#: the <see cref="PuzzleState"/>, its definition data, where it is,
    /// which room and stage it belongs to, and the confirm-lever cooldown. Built by the round setup applier (TODO 2.7) and handed to
    /// <c>PuzzleSystem.RegisterSlot</c>; PuzzleSystem owns the state from then on.
    /// </summary>
    public sealed class PuzzleSlotRuntime
    {
        private readonly int[] _scratch;

        /// <summary>The slot of a real <see cref="PuzzleDefinition"/>. The state must have the definition's part count.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="state"/> or <paramref name="definition"/> is null.</exception>
        /// <exception cref="ArgumentException">Part counts of state and definition differ.</exception>
        public PuzzleSlotRuntime(PuzzleState state, PuzzleDefinition definition, Vector3 position, int room, int stage)
            : this(state, RequireDefinition(state, definition).RequiredItemId, position, room, stage)
        {
            Definition = definition;
        }

        /// <summary>A slot without a definition asset (tests, debug puzzles). <paramref name="requiredItemId"/>: null or "" = no item needed.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="state"/> is null.</exception>
        public PuzzleSlotRuntime(PuzzleState state, string requiredItemId, Vector3 position, int room, int stage)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            RequiredItemId = requiredItemId ?? string.Empty;
            Position = position;
            ConfirmPosition = position;
            Room = room;
            Stage = stage;
            _scratch = new int[state.PartCount];
        }

        public PuzzleState State { get; }

        /// <summary>The definition asset, or null when the slot was built without one.</summary>
        public PuzzleDefinition Definition { get; }

        /// <summary>ItemDefinition.Id the human must carry to change a part, "" = none.</summary>
        public string RequiredItemId { get; }

        public bool HasRequiredItem => RequiredItemId.Length > 0;

        /// <summary>World position of the puzzle body: range checks of ops and sabotage, and the lantern safe-zone test, measure to it.</summary>
        public Vector3 Position { get; }

        /// <summary>World position of the confirm lever; the confirm range check measures to it. Defaults to <see cref="Position"/>.</summary>
        public Vector3 ConfirmPosition { get; set; }

        /// <summary>RoomMarker id of the puzzle's room (-1 = outside every room); the light rule reads its light.</summary>
        public int Room { get; }

        /// <summary>Stage the puzzle belongs to (PuzzleCompleted carries it).</summary>
        public int Stage { get; }

        /// <summary>Absolute Net.Time before which the lever is locked after a wrong confirmation. Authority-owned.</summary>
        public double ConfirmCooldownUntil { get; set; }

        /// <summary>
        /// Current part values copied into a buffer owned by this slot (no allocation). Valid until the next call; do not store it.
        /// Message encoders copy it, so it can be passed straight to them.
        /// </summary>
        public int[] SnapshotValues()
        {
            State.CopyValuesTo(_scratch);
            return _scratch;
        }

        private static PuzzleDefinition RequireDefinition(PuzzleState state, PuzzleDefinition definition)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }
            if (definition.PartCount != state.PartCount)
            {
                throw new ArgumentException("state has " + state.PartCount + " parts but definition '" + definition.name + "' has " + definition.PartCount, nameof(state));
            }
            return definition;
        }
    }
}
