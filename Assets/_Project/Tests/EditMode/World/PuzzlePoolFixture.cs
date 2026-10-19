using System.Collections.Generic;
using System.Reflection;
using CinliMahzen.Core;
using CinliMahzen.World;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.World
{
    /// <summary>
    /// In-memory <see cref="PuzzleDefinition"/>s for the planner tests (no asset files): ScriptableObject.CreateInstance and the
    /// private serialized fields filled through reflection, from <see cref="MvpPuzzleCatalog"/> (the same data the
    /// Create Puzzle Definitions menu writes) or from custom values. Call <see cref="DestroyAll"/> in the tear-down.
    /// </summary>
    internal sealed class PuzzlePoolFixture
    {
        private readonly List<ScriptableObject> _created = new List<ScriptableObject>();

        /// <summary>The four MVP definitions in catalog order (Paintings, Statue, Wires, Sigil).</summary>
        public List<PuzzleDefinition> Mvp()
        {
            var pool = new List<PuzzleDefinition>();
            IReadOnlyList<PuzzleDefinitionSpec> specs = MvpPuzzleCatalog.Specs;
            for (int i = 0; i < specs.Count; i++)
            {
                pool.Add(FromSpec(specs[i]));
            }
            return pool;
        }

        public PuzzleDefinition FromSpec(PuzzleDefinitionSpec spec)
        {
            return Create(spec.AssetName, spec.Type, spec.NameKey, spec.CopyValueCounts(), spec.OpKind, spec.RequiredItemId, spec.Size);
        }

        /// <summary>A Cycle definition with the given shape (name key "puzzle.test").</summary>
        public PuzzleDefinition Custom(PuzzleType type, PuzzleSlotSize size, string requiredItemId, params int[] valueCounts)
        {
            return Create("PZ_Test_" + type, type, "puzzle.test", valueCounts, PuzzleOpKind.Cycle, requiredItemId, size);
        }

        public PuzzleDefinition Create(string name, PuzzleType type, string nameKey, int[] valueCounts, PuzzleOpKind opKind,
            string requiredItemId, PuzzleSlotSize size)
        {
            var definition = ScriptableObject.CreateInstance<PuzzleDefinition>();
            definition.name = name;
            _created.Add(definition);
            SetField(definition, "type", type);
            SetField(definition, "nameKey", nameKey);
            SetField(definition, "valueCounts", valueCounts);
            SetField(definition, "opKind", opKind);
            SetField(definition, "requiredItemId", requiredItemId ?? string.Empty);
            SetField(definition, "size", size);
            return definition;
        }

        /// <summary>A GameBalanceConfig with its default values (StagesActive 2, PuzzlesPerStage 2), destroyed with the rest.</summary>
        public GameBalanceConfig DefaultConfig()
        {
            var config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            _created.Add(config);
            return config;
        }

        public void DestroyAll()
        {
            for (int i = 0; i < _created.Count; i++)
            {
                if (_created[i] != null)
                {
                    Object.DestroyImmediate(_created[i]);
                }
            }
            _created.Clear();
        }

        private static void SetField(PuzzleDefinition definition, string field, object value)
        {
            FieldInfo info = typeof(PuzzleDefinition).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(info, "serialized field '" + field + "' missing on PuzzleDefinition");
            info.SetValue(definition, value);
        }
    }
}
