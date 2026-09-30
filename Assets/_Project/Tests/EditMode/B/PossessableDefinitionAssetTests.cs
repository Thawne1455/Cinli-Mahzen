using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Possession;
using NUnit.Framework;
using UnityEditor;

namespace CinliMahzen.Tests.EditMode.B
{
    /// <summary>The 10 PD_*.asset files exist, are consistent and match the Oyun §4 table.</summary>
    public class PossessableDefinitionAssetTests
    {
        private const string Folder = "Assets/_Project/ScriptableObjects/Possessables";
        private const float Eps = 1e-4f;

        private static readonly string[] ExpectedAssets =
        {
            "PD_Shelf", "PD_Barrel", "PD_Chair", "PD_Stool", "PD_Chest",
            "PD_SwordShield", "PD_Torch", "PD_Candle", "PD_Bottle", "PD_Keg",
        };

        private Dictionary<string, PossessableDefinition> _defs;

        [OneTimeSetUp]
        public void LoadAll()
        {
            _defs = new Dictionary<string, PossessableDefinition>();
            foreach (string guid in AssetDatabase.FindAssets("t:PossessableDefinition", new[] { Folder }))
            {
                var def = AssetDatabase.LoadAssetAtPath<PossessableDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (def != null)
                {
                    _defs[def.name] = def;
                }
            }
        }

        private PossessableDefinition Get(string asset)
        {
            Assert.IsTrue(_defs.TryGetValue(asset, out PossessableDefinition def), asset + " missing");
            return def;
        }

        [Test]
        public void AllTenDefinitionsExist()
        {
            foreach (string asset in ExpectedAssets)
            {
                Get(asset);
            }
            Assert.AreEqual(ExpectedAssets.Length, _defs.Count);
        }

        [Test]
        public void NoDataProblems()
        {
            var problems = new List<string>();
            foreach (PossessableDefinition def in _defs.Values)
            {
                def.CollectProblems(problems);
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void IdsAreUnique()
        {
            var ids = new HashSet<string>();
            foreach (PossessableDefinition def in _defs.Values)
            {
                Assert.IsTrue(ids.Add(def.Id), "duplicate id " + def.Id);
            }
        }

        // asset, action, telegraph, energy, cooldown, single use, damage — Oyun §4
        [TestCase("PD_Shelf", "topple", 1.0f, 40f, 0f, true, 3)]
        [TestCase("PD_Barrel", "roll", 0.8f, 20f, 6f, false, 1)]
        [TestCase("PD_Chair", "lunge", 0.4f, 15f, 5f, false, 1)]
        [TestCase("PD_Stool", "lunge", 0.4f, 15f, 5f, false, 1)]
        [TestCase("PD_Chest", "mimic", 0f, 25f, 15f, false, 1)]
        [TestCase("PD_SwordShield", "launch", 0.7f, 25f, 0f, true, 1)]
        [TestCase("PD_Torch", "extinguish", 0.3f, 15f, 30f, false, 0)]
        [TestCase("PD_Candle", "extinguish", 0.3f, 15f, 30f, false, 0)]
        [TestCase("PD_Bottle", "throw", 0.5f, 10f, 0f, true, 0)]
        [TestCase("PD_Keg", "explode", 3.5f, 60f, 0f, true, 3)]
        public void Primary_MatchesDesignTable(string asset, string action, float telegraph, float energy,
            float cooldown, bool singleUse, int damage)
        {
            PossessableActionDef a = Get(asset).Primary;
            Assert.AreEqual(action, a.Id);
            Assert.AreEqual(telegraph, a.TelegraphTime, Eps);
            Assert.AreEqual(energy, a.EnergyCost, Eps);
            Assert.AreEqual(cooldown, a.Cooldown, Eps);
            Assert.AreEqual(singleUse, a.SingleUse);
            Assert.AreEqual(damage, a.Damage);
            Assert.IsFalse(string.IsNullOrEmpty(a.CauseId));
            Assert.IsFalse(string.IsNullOrEmpty(a.NameKey));
        }

        [Test]
        public void Shelf_ToppleIsLethalBoxInFront()
        {
            PossessableActionDef a = Get("PD_Shelf").Primary;
            Assert.AreEqual(ActionShape.Box, a.Shape);
            Assert.AreEqual(2f, a.ShapeSize.x, Eps);
            Assert.AreEqual(1.5f, a.ShapeSize.z, Eps);
            Assert.Greater(a.ShapeOffset.z, 0f, "hit box must be in front of the shelf");
            Assert.IsTrue((a.Flags & DamageFlags.Lethal) != 0);
            Assert.IsTrue(Get("PD_Shelf").IsSearchableContainer);
        }

        [Test]
        public void Barrel_RollIsChargedKnockdown()
        {
            PossessableDefinition d = Get("PD_Barrel");
            Assert.AreEqual(ActionTrigger.HoldToCharge, d.Primary.Trigger);
            Assert.AreEqual(8f, d.Primary.ProjectileSpeed, Eps);
            Assert.AreEqual(12f, d.Primary.ProjectileRange, Eps);
            Assert.IsTrue((d.Primary.Flags & DamageFlags.Knockdown) != 0);
            Assert.AreEqual(1.5f, d.Primary.KnockdownTime, Eps);
            Assert.Greater(d.YawLimitDeg, 0f, "A/D aiming");
        }

        [TestCase("PD_Chair")]
        [TestCase("PD_Stool")]
        public void ChairAndStool_Hop(string asset)
        {
            PossessableDefinition d = Get(asset);
            Assert.IsTrue(d.CanMove);
            Assert.AreEqual(1f, d.HopDistance, Eps);
            Assert.AreEqual(0.5f, d.HopInterval, Eps);
            Assert.AreEqual(2f, d.HopEnergy, Eps);
            Assert.AreEqual(2f, d.Primary.ProjectileRange, Eps, "lunge distance");
        }

        [Test]
        public void Chest_MimicGrabsOnTrigger()
        {
            PossessableDefinition d = Get("PD_Chest");
            Assert.AreEqual(ActionTrigger.ToggleArm, d.Primary.Trigger);
            Assert.IsTrue((d.Primary.Flags & DamageFlags.Grab) != 0);
            Assert.AreEqual(2f, d.Primary.GrabTime, Eps);
            Assert.IsTrue(d.IsSearchableContainer);
        }

        [Test]
        public void SwordShield_StraightProjectileWithYawLimit()
        {
            PossessableDefinition d = Get("PD_SwordShield");
            Assert.AreEqual(ActionShape.Projectile, d.Primary.Shape);
            Assert.AreEqual(14f, d.Primary.ProjectileSpeed, Eps);
            Assert.AreEqual(15f, d.Primary.ProjectileRange, Eps);
            Assert.AreEqual(0f, d.Primary.ProjectileArc, Eps);
            Assert.AreEqual(35f, d.YawLimitDeg, Eps);
        }

        [TestCase("PD_Torch")]
        [TestCase("PD_Candle")]
        public void TorchAndCandle_ExtinguishAndFlame(string asset)
        {
            PossessableDefinition d = Get(asset);
            Assert.AreEqual(20f, d.Primary.EffectDuration, Eps, "lights off for 20 s");
            Assert.IsTrue(d.HasSecondary);
            Assert.AreEqual(2, d.ActionCount);
            PossessableActionDef flame = d.GetAction(PossessableDefinition.SecondaryIndex);
            Assert.AreEqual("flame", flame.Id);
            Assert.AreEqual(0.6f, flame.TelegraphTime, Eps);
            Assert.AreEqual(30f, flame.EnergyCost, Eps);
            Assert.AreEqual(12f, flame.Cooldown, Eps);
            Assert.AreEqual(1, flame.Damage);
            Assert.AreEqual(ActionShape.Cone, flame.Shape);
            Assert.AreEqual(2f, flame.ShapeSize.x, Eps, "2 m cone");
        }

        [Test]
        public void Bottle_ArcThrowMakesDrunk()
        {
            PossessableDefinition d = Get("PD_Bottle");
            Assert.AreEqual(StatusType.Drunk, d.Primary.ApplyStatus);
            Assert.AreEqual(4f, d.Primary.StatusDuration, Eps);
            Assert.Greater(d.Primary.ProjectileArc, 0f);
            Assert.AreEqual(45f, d.YawLimitDeg, Eps);
        }

        [Test]
        public void Keg_TwoDamageRings()
        {
            PossessableActionDef a = Get("PD_Keg").Primary;
            Assert.AreEqual(ActionTrigger.HoldToCharge, a.Trigger);
            Assert.AreEqual(ActionShape.Sphere, a.Shape);
            Assert.AreEqual(3f, a.ShapeSize.x, Eps);
            Assert.AreEqual(5f, a.OuterRadius, Eps);
            Assert.AreEqual(1, a.OuterDamage);
            Assert.IsTrue((a.OuterFlags & DamageFlags.Knockdown) != 0);
        }

        [Test]
        public void SingleActionObjects_HaveNoSecondary()
        {
            foreach (PossessableDefinition d in _defs.Values)
            {
                if (d.Id == "torch" || d.Id == "candle")
                {
                    continue;
                }
                Assert.IsFalse(d.HasSecondary, d.name);
                Assert.IsNull(d.GetAction(PossessableDefinition.SecondaryIndex), d.name);
            }
        }
    }
}
