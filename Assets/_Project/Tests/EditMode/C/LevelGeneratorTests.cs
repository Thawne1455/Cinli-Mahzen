using CinliMahzen.World;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode
{
    public class LevelGeneratorTests
    {
        [Test]
        public void SameSeed_SamePlanHash()
        {
            var s = new LevelGenSettings();
            Assert.AreEqual(LevelPlanner.Plan(4242, s).ComputeHash(), LevelPlanner.Plan(4242, s).ComputeHash());
        }

        [Test]
        public void DifferentSeeds_DifferentPlans()
        {
            var s = new LevelGenSettings();
            Assert.AreNotEqual(LevelPlanner.Plan(1, s).ComputeHash(), LevelPlanner.Plan(2, s).ComputeHash());
        }

        [Test]
        public void FiftySeeds_AllPassRules()
        {
            var s = new LevelGenSettings();
            for (int seed = 1; seed <= 50; seed++)
            {
                var plan = LevelPlanner.Plan(seed, s);
                Assert.IsNotNull(plan, "seed " + seed);
                var errors = LevelRules.Validate(plan.Rooms, plan.Markers, s);
                Assert.IsEmpty(errors, "seed " + seed + ": " + string.Join("; ", errors));
            }
        }

        [Test]
        public void FallbackSeed_IsValid()
        {
            var s = new LevelGenSettings();
            Assert.IsNotNull(LevelPlanner.Plan(s.FallbackSeed, s));
        }

        [Test]
        public void GeneratedHierarchy_SameSeed_SameLevelHash_AndValid()
        {
            var s = new LevelGenSettings();
            var a = new GameObject("A").transform;
            var b = new GameObject("B").transform;
            try
            {
                var la = new ProceduralLevelGenerator().Generate(99, s, a);
                new ProceduralLevelGenerator().Generate(99, s, b);
                Assert.AreEqual(LevelHash.Compute(a), LevelHash.Compute(b));
                Assert.IsEmpty(LevelValidator.Validate(la, s));
            }
            finally
            {
                Object.DestroyImmediate(a.gameObject);
                Object.DestroyImmediate(b.gameObject);
            }
        }
    }
}
