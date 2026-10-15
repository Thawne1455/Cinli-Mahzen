using System.Collections.Generic;
using System.Reflection;
using CinliMahzen.Core;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.Objectives
{
    /// <summary>
    /// PuzzleDefinition data checks and the serialized enum values (Teknik §7.2).
    /// Definitions are created with ScriptableObject.CreateInstance (no asset files); private serialized fields
    /// are filled through reflection and every instance is destroyed again.
    /// </summary>
    public class PuzzleDefinitionTests
    {
        private readonly List<ScriptableObject> _created = new List<ScriptableObject>();

        [TearDown]
        public void DestroyCreated()
        {
            foreach (ScriptableObject so in _created)
            {
                UnityEngine.Object.DestroyImmediate(so);
            }
            _created.Clear();
        }

        private PuzzleDefinition NewDefinition(string nameKey, int[] valueCounts, PuzzleOpKind opKind)
        {
            var def = ScriptableObject.CreateInstance<PuzzleDefinition>();
            def.name = "PZ_Test";
            _created.Add(def);
            SetField(def, "nameKey", nameKey);
            SetField(def, "valueCounts", valueCounts);
            SetField(def, "opKind", opKind);
            return def;
        }

        private static void SetField(PuzzleDefinition def, string field, object value)
        {
            FieldInfo info = typeof(PuzzleDefinition).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(info, "serialized field '" + field + "' missing on PuzzleDefinition");
            info.SetValue(def, value);
        }

        private static List<string> Problems(PuzzleDefinition def)
        {
            var problems = new List<string>();
            def.CollectProblems(problems);
            return problems;
        }

        // ---------- empty definition ----------

        [Test]
        public void EmptyDefinition_ReportsProblems()
        {
            var def = ScriptableObject.CreateInstance<PuzzleDefinition>();
            _created.Add(def);

            List<string> problems = Problems(def);

            Assert.IsNotEmpty(problems);
            Assert.IsTrue(problems.Exists(p => p.Contains("nameKey")), "empty nameKey not reported");
            Assert.IsTrue(problems.Exists(p => p.Contains("valueCounts")), "empty valueCounts not reported");
            Assert.IsTrue(problems.Exists(p => p.Contains("opKind")), "opKind None not reported");
        }

        [Test]
        public void EmptyDefinition_HasSafeDefaults()
        {
            var def = ScriptableObject.CreateInstance<PuzzleDefinition>();
            _created.Add(def);

            Assert.AreEqual(0, def.PartCount);
            Assert.AreEqual(0, def.ValueCounts.Count);
            Assert.AreEqual(0, def.CopyValueCounts().Length);
            Assert.AreEqual(string.Empty, def.RequiredItemId);
            Assert.IsFalse(def.HasRequiredItem);
            Assert.IsFalse(def.IsPermutation);
            Assert.IsNull(def.InteractivePrefab);
            Assert.IsNull(def.ReferencePrefab);
        }

        [Test]
        public void CollectProblems_NullList_Throws()
        {
            var def = ScriptableObject.CreateInstance<PuzzleDefinition>();
            _created.Add(def);
            Assert.Throws<System.ArgumentNullException>(() => def.CollectProblems(null));
        }

        // ---------- filled definitions ----------

        [Test]
        public void ValidDefinitions_ReportNoProblems()
        {
            Assert.IsEmpty(Problems(NewDefinition("puzzle.paintings", new[] { 4, 4, 4 }, PuzzleOpKind.Cycle)));
            Assert.IsEmpty(Problems(NewDefinition("puzzle.statue", new[] { 6, 3 }, PuzzleOpKind.Cycle)));
            Assert.IsEmpty(Problems(NewDefinition("puzzle.sigil", new[] { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 }, PuzzleOpKind.Set)));
            Assert.IsEmpty(Problems(NewDefinition("puzzle.books", new[] { 5, 5, 5, 5, 5, 4 }, PuzzleOpKind.Swap | PuzzleOpKind.Cycle)));
            Assert.IsEmpty(Problems(NewDefinition("puzzle.jigsaw", new[] { 6, 6, 6, 6, 6, 6 }, PuzzleOpKind.Swap)));
        }

        [Test]
        public void PartCount_And_ValueCounts_FollowTheArray()
        {
            PuzzleDefinition def = NewDefinition("puzzle.statue", new[] { 6, 3 }, PuzzleOpKind.Cycle);
            Assert.AreEqual(2, def.PartCount);
            Assert.AreEqual(6, def.ValueCounts[0]);
            Assert.AreEqual(3, def.ValueCounts[1]);
            CollectionAssert.AreEqual(new[] { 6, 3 }, def.CopyValueCounts());
        }

        [Test]
        public void CopyValueCounts_ReturnsAnIndependentArray()
        {
            PuzzleDefinition def = NewDefinition("puzzle.statue", new[] { 6, 3 }, PuzzleOpKind.Cycle);
            int[] copy = def.CopyValueCounts();
            copy[0] = 99;
            Assert.AreEqual(6, def.ValueCounts[0]);
        }

        [Test]
        public void IsPermutation_OnlyForPureSwap()
        {
            Assert.IsTrue(NewDefinition("k", new[] { 6, 6, 6, 6, 6, 6 }, PuzzleOpKind.Swap).IsPermutation);
            Assert.IsFalse(NewDefinition("k", new[] { 5, 5, 5, 5, 5, 4 }, PuzzleOpKind.Swap | PuzzleOpKind.Cycle).IsPermutation);
            Assert.IsFalse(NewDefinition("k", new[] { 4, 4, 4 }, PuzzleOpKind.Cycle).IsPermutation);
            Assert.IsFalse(NewDefinition("k", new[] { 2, 2 }, PuzzleOpKind.Set).IsPermutation);
        }

        [Test]
        public void RequiredItemId_EmptyMeansNone()
        {
            PuzzleDefinition def = NewDefinition("puzzle.sigil", new[] { 2, 2 }, PuzzleOpKind.Set);
            Assert.IsFalse(def.HasRequiredItem);

            SetField(def, "requiredItemId", "paint");
            Assert.IsTrue(def.HasRequiredItem);
            Assert.AreEqual("paint", def.RequiredItemId);
        }

        [Test]
        public void CollectProblems_ReportsEmptyNameKey()
        {
            List<string> problems = Problems(NewDefinition("", new[] { 4, 4 }, PuzzleOpKind.Cycle));
            Assert.AreEqual(1, problems.Count);
            StringAssert.Contains("nameKey", problems[0]);
        }

        [Test]
        public void CollectProblems_ReportsOpKindNone()
        {
            List<string> problems = Problems(NewDefinition("k", new[] { 4, 4 }, PuzzleOpKind.None));
            Assert.AreEqual(1, problems.Count);
            StringAssert.Contains("opKind", problems[0]);
        }

        [Test]
        public void CollectProblems_ReportsUnknownOpFlags()
        {
            List<string> problems = Problems(NewDefinition("k", new[] { 4, 4 }, PuzzleOpKind.Cycle | (PuzzleOpKind)16));
            Assert.AreEqual(1, problems.Count);
            StringAssert.Contains("unknown", problems[0]);
        }

        [Test]
        public void CollectProblems_ReportsEveryValueCountBelowTwo()
        {
            List<string> problems = Problems(NewDefinition("k", new[] { 4, 1, 0, 6 }, PuzzleOpKind.Cycle));
            Assert.AreEqual(2, problems.Count);
            StringAssert.Contains("valueCounts[1]", problems[0]);
            StringAssert.Contains("valueCounts[2]", problems[1]);
        }

        [Test]
        public void CollectProblems_ReportsEmptyValueCounts()
        {
            List<string> problems = Problems(NewDefinition("k", new int[0], PuzzleOpKind.Cycle));
            Assert.AreEqual(1, problems.Count);
            StringAssert.Contains("valueCounts", problems[0]);
        }

        [Test]
        public void CollectProblems_PermutationNeedsValueCountEqualToPartCount()
        {
            List<string> problems = Problems(NewDefinition("k", new[] { 6, 6, 6, 5, 6, 6 }, PuzzleOpKind.Swap));
            Assert.AreEqual(1, problems.Count);
            StringAssert.Contains("permutation", problems[0]);
        }

        [Test]
        public void CollectProblems_AppendsToTheGivenList()
        {
            var problems = new List<string> { "existing" };
            NewDefinition("", new int[0], PuzzleOpKind.None).CollectProblems(problems);
            Assert.AreEqual("existing", problems[0]);
            Assert.AreEqual(4, problems.Count);
        }

        [Test]
        public void CollectProblems_MessagesStartWithTheAssetName()
        {
            foreach (string problem in Problems(NewDefinition("", new int[0], PuzzleOpKind.None)))
            {
                StringAssert.StartsWith("PZ_Test", problem);
            }
        }

        // ---------- enums are serialized as ints ----------

        [Test]
        public void EnumValues_AreStable()
        {
            Assert.AreEqual(0, (int)PuzzleType.Paintings);
            Assert.AreEqual(1, (int)PuzzleType.Statue);
            Assert.AreEqual(2, (int)PuzzleType.Wires);
            Assert.AreEqual(3, (int)PuzzleType.Sigil);
            Assert.AreEqual(4, (int)PuzzleType.Books);
            Assert.AreEqual(5, (int)PuzzleType.Jigsaw);

            Assert.AreEqual(0, (int)PuzzleOpKind.None);
            Assert.AreEqual(1, (int)PuzzleOpKind.Cycle);
            Assert.AreEqual(2, (int)PuzzleOpKind.Set);
            Assert.AreEqual(4, (int)PuzzleOpKind.Swap);

            Assert.AreEqual(0, (int)PuzzleSlotSize.Wall);
            Assert.AreEqual(1, (int)PuzzleSlotSize.Floor);
            Assert.AreEqual(2, (int)PuzzleSlotSize.Table);
        }

        [Test]
        public void IsValidOpKinds_AcceptsKnownCombinationsOnly()
        {
            Assert.IsFalse(PuzzleDefinition.IsValidOpKinds(PuzzleOpKind.None));
            Assert.IsTrue(PuzzleDefinition.IsValidOpKinds(PuzzleOpKind.Cycle));
            Assert.IsTrue(PuzzleDefinition.IsValidOpKinds(PuzzleOpKind.Set));
            Assert.IsTrue(PuzzleDefinition.IsValidOpKinds(PuzzleOpKind.Swap));
            Assert.IsTrue(PuzzleDefinition.IsValidOpKinds(PuzzleOpKind.Swap | PuzzleOpKind.Cycle));
            Assert.IsTrue(PuzzleDefinition.IsValidOpKinds(PuzzleOpKind.Cycle | PuzzleOpKind.Set | PuzzleOpKind.Swap));
            Assert.IsFalse(PuzzleDefinition.IsValidOpKinds((PuzzleOpKind)8));
            Assert.IsFalse(PuzzleDefinition.IsValidOpKinds(PuzzleOpKind.Cycle | (PuzzleOpKind)8));
        }
    }
}
