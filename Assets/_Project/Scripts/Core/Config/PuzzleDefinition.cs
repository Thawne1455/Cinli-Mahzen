using System.Collections.Generic;
using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>
    /// Static data of one puzzle kind (Teknik §7.2): shape (N parts x K values), allowed human operations,
    /// required item, prefabs and slot size. Assets: ScriptableObjects/Puzzles/PZ_*.asset.
    /// Lives in CM.Core (not CM.Objectives) because RoundSetupPlanner (CM.World) reads the pool and
    /// CM.World cannot reference CM.Objectives; the runtime model is CinliMahzen.Objectives.PuzzleState.
    /// </summary>
    [CreateAssetMenu(menuName = "CinliMahzen/Puzzles/Puzzle Definition", fileName = "PZ_New")]
    public sealed class PuzzleDefinition : ScriptableObject
    {
        /// <summary>Every part needs at least two values, otherwise it can never be sabotaged.</summary>
        public const int MinValueCount = 2;

        private const byte KnownOpBits = (byte)(PuzzleOpKind.Cycle | PuzzleOpKind.Set | PuzzleOpKind.Swap);

        [SerializeField] private PuzzleType type;
        [SerializeField] private string nameKey;
        [Tooltip("K per part; the array length is the part count N.")]
        [SerializeField] private int[] valueCounts;
        [SerializeField] private PuzzleOpKind opKind;
        [Tooltip("ItemDefinition.Id the human must carry to operate the puzzle; empty = none (e.g. \"paint\", \"fuse_key\").")]
        [SerializeField] private string requiredItemId = string.Empty;
        [SerializeField] private GameObject interactivePrefab;
        [SerializeField] private GameObject referencePrefab;
        [SerializeField] private PuzzleSlotSize size;

        public PuzzleType Type => type;

        /// <summary>Loc key, e.g. "puzzle.paintings".</summary>
        public string NameKey => nameKey;

        /// <summary>K (value count) per part. Read-only view; use <see cref="CopyValueCounts"/> to get an array of your own.</summary>
        public IReadOnlyList<int> ValueCounts => valueCounts ?? System.Array.Empty<int>();

        public PuzzleOpKind OpKind => opKind;

        /// <summary>ItemDefinition.Id, "" = none.</summary>
        public string RequiredItemId => requiredItemId ?? string.Empty;

        /// <summary>P_Puzzle_* — the playable puzzle (visible to everyone).</summary>
        public GameObject InteractivePrefab => interactivePrefab;

        /// <summary>P_PuzzleRef_* — the answer view (SpiritOnly, Spirit Veil aware).</summary>
        public GameObject ReferencePrefab => referencePrefab;

        /// <summary>Slot size class — the definition only fits PuzzleSlot / ReferenceSlot markers of the same size.</summary>
        public PuzzleSlotSize Size => size;

        /// <summary>Number of parts N.</summary>
        public int PartCount => valueCounts != null ? valueCounts.Length : 0;

        public bool HasRequiredItem => !string.IsNullOrEmpty(requiredItemId);

        /// <summary>
        /// True for pure permutation puzzles (OpKind is exactly Swap — Jigsaw): the N parts hold the values 0..N-1 each exactly once.
        /// Mixed kinds such as Books (Swap | Cycle) are NOT treated as permutations for now: sabotage assigns every part a
        /// random value, so the 5-book order may stop being a permutation after a sabotage.
        /// </summary>
        public bool IsPermutation => opKind == PuzzleOpKind.Swap;

        /// <summary>True when <paramref name="kinds"/> is not None and only contains known flags (Cycle, Set, Swap).</summary>
        public static bool IsValidOpKinds(PuzzleOpKind kinds)
        {
            return kinds != PuzzleOpKind.None && ((byte)kinds & ~KnownOpBits) == 0;
        }

        /// <summary>New array with K per part (the PuzzleState constructor copies its input again, so this is safe to pass as-is).</summary>
        public int[] CopyValueCounts()
        {
            int count = PartCount;
            var copy = new int[count];
            for (int i = 0; i < count; i++)
            {
                copy[i] = valueCounts[i];
            }
            return copy;
        }

        /// <summary>Appends data problems (empty nameKey, empty or too small valueCounts, no operation, inconsistent permutation).</summary>
        public void CollectProblems(List<string> problems)
        {
            if (problems == null)
            {
                throw new System.ArgumentNullException(nameof(problems));
            }

            if (string.IsNullOrEmpty(nameKey))
            {
                problems.Add(name + ": empty nameKey");
            }

            int count = PartCount;
            if (count == 0)
            {
                problems.Add(name + ": valueCounts is empty");
            }
            for (int i = 0; i < count; i++)
            {
                if (valueCounts[i] < MinValueCount)
                {
                    problems.Add(name + ": valueCounts[" + i + "] = " + valueCounts[i] + " (must be >= " + MinValueCount + ")");
                }
            }

            if (opKind == PuzzleOpKind.None)
            {
                problems.Add(name + ": opKind is None");
            }
            else if (!IsValidOpKinds(opKind))
            {
                problems.Add(name + ": opKind has unknown flags");
            }

            if (IsPermutation)
            {
                for (int i = 0; i < count; i++)
                {
                    if (valueCounts[i] != count)
                    {
                        problems.Add(name + ": permutation puzzle needs valueCounts[" + i + "] == PartCount (" + count + "), is " + valueCounts[i]);
                        break;
                    }
                }
            }
        }
    }
}
