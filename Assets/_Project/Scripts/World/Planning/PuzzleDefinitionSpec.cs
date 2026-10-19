using System;
using System.Collections.Generic;
using CinliMahzen.Core;

namespace CinliMahzen.World
{
    /// <summary>
    /// The data fields of one <see cref="PuzzleDefinition"/> asset as plain data (no prefabs): what the editor menu
    /// CinliMahzen/Setup/Create Puzzle Definitions writes into PZ_*.asset and what the EditMode tests instantiate in memory, so
    /// both read one source (<see cref="MvpPuzzleCatalog"/>). Immutable.
    /// </summary>
    public sealed class PuzzleDefinitionSpec
    {
        private readonly int[] _valueCounts;

        /// <summary>Asset file name without extension, e.g. "PZ_Paintings".</summary>
        public string AssetName { get; }

        public PuzzleType Type { get; }
        public string NameKey { get; }

        /// <summary>K per part; the length is the part count.</summary>
        public IReadOnlyList<int> ValueCounts { get; }

        public PuzzleOpKind OpKind { get; }

        /// <summary>ItemDefinition.Id; empty = none.</summary>
        public string RequiredItemId { get; }

        public PuzzleSlotSize Size { get; }

        public PuzzleDefinitionSpec(string assetName, PuzzleType type, string nameKey, int[] valueCounts, PuzzleOpKind opKind,
            string requiredItemId, PuzzleSlotSize size)
        {
            AssetName = assetName ?? string.Empty;
            Type = type;
            NameKey = nameKey ?? string.Empty;
            _valueCounts = valueCounts != null ? (int[])valueCounts.Clone() : Array.Empty<int>();
            ValueCounts = Array.AsReadOnly(_valueCounts);
            OpKind = opKind;
            RequiredItemId = requiredItemId ?? string.Empty;
            Size = size;
        }

        public int PartCount => _valueCounts.Length;

        /// <summary>A new array with K per part.</summary>
        public int[] CopyValueCounts()
        {
            return (int[])_valueCounts.Clone();
        }

        /// <summary>True when every data field of <paramref name="definition"/> equals this spec (prefab references are not compared).</summary>
        public bool Matches(PuzzleDefinition definition)
        {
            if (definition == null || definition.Type != Type || definition.OpKind != OpKind || definition.Size != Size
                || !string.Equals(definition.NameKey, NameKey, StringComparison.Ordinal)
                || !string.Equals(definition.RequiredItemId, RequiredItemId, StringComparison.Ordinal)
                || definition.PartCount != _valueCounts.Length)
            {
                return false;
            }
            for (int i = 0; i < _valueCounts.Length; i++)
            {
                if (definition.ValueCounts[i] != _valueCounts[i])
                {
                    return false;
                }
            }
            return true;
        }

        public override string ToString()
        {
            return AssetName + " (" + Type + ", " + string.Join("x", _valueCounts) + ", " + OpKind + ", " + Size
                + (RequiredItemId.Length > 0 ? ", needs " + RequiredItemId : string.Empty) + ")";
        }
    }
}
