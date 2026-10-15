using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Possession;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.B
{
    /// <summary>The 14 v2.1 PD_*.asset files exist, are consistent and match the Oyun §8 table (Tech §6.3).</summary>
    public class PossessableDefinitionAssetTests
    {
        private const string Folder = "Assets/_Project/ScriptableObjects/Possessables";
        private const string LocPath = "Assets/_Project/Resources/Loc/LocTable_A.asset";
        private const float Eps = 1e-4f;

        private static readonly string[] ExpectedAssets =
        {
            "PD_Bottle", "PD_Candle", "PD_Plate", "PD_Slipper", "PD_Stool", "PD_Chair", "PD_Barrel",
            "PD_Chest", "PD_Table", "PD_Shelf", "PD_Lamp", "PD_Radio", "PD_Cologne", "PD_QuestItem",
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

        // ------------------------------------------------------------ inventory + consistency

        [Test]
        public void AllFourteenDefinitionsExist_AndNoV1Leftovers()
        {
            foreach (string asset in ExpectedAssets)
            {
                Get(asset);
            }
            Assert.AreEqual(ExpectedAssets.Length, _defs.Count, "unexpected extra PD_* asset (PD_Keg / PD_SwordShield / PD_Torch are gone in v2.1)");
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
        public void IdsAreUnique_AndNameKeysFollowTheId()
        {
            var ids = new HashSet<string>();
            foreach (PossessableDefinition def in _defs.Values)
            {
                Assert.IsTrue(ids.Add(def.Id), "duplicate id " + def.Id);
                Assert.AreEqual("poss." + def.Id, def.NameKey, def.name);
            }
        }

        [Test]
        public void ActionIds_AreFromTheV21List_AndNameKeysFollowTheActionId()
        {
            var known = new HashSet<string>
            {
                "throw", "homing_slipper", "lunge", "slide", "topple", "burst", "play_tune", "fling", "revive_splash",
            };
            foreach (PossessableDefinition def in _defs.Values)
            {
                for (int i = 0; i < def.ActionCount; i++)
                {
                    PossessableActionDef a = def.GetAction(i);
                    Assert.IsTrue(known.Contains(a.Id), def.name + ": unknown action id " + a.Id);
                    Assert.AreEqual("action." + a.Id, a.NameKey, def.name);
                }
            }
        }

        [Test]
        public void LocKeys_ExistInTurkishAndEnglish()
        {
            var table = AssetDatabase.LoadAssetAtPath<LocTable>(LocPath);
            Assert.IsNotNull(table, LocPath);
            var tr = new Dictionary<string, string>();
            var en = new Dictionary<string, string>();
            for (int i = 0; i < table.Entries.Count; i++)
            {
                tr[table.Entries[i].Key] = table.Entries[i].Tr;
                en[table.Entries[i].Key] = table.Entries[i].En;
            }
            var keys = new HashSet<string> { "action.hop" };
            foreach (PossessableDefinition def in _defs.Values)
            {
                keys.Add(def.NameKey);
                for (int i = 0; i < def.ActionCount; i++)
                {
                    keys.Add(def.GetAction(i).NameKey);
                }
            }
            foreach (string key in keys)
            {
                Assert.IsTrue(tr.ContainsKey(key) && !string.IsNullOrEmpty(tr[key]), "missing TR text for " + key);
                Assert.IsTrue(en.ContainsKey(key) && !string.IsNullOrEmpty(en[key]), "missing EN text for " + key);
            }
        }

        // ------------------------------------------------------------ classification (size / lamp / good jinn)

        // asset, size, allowGoodJinn, isLamp
        [TestCase("PD_Bottle", PossessableSize.Small, false, false)]
        [TestCase("PD_Candle", PossessableSize.Small, false, false)]
        [TestCase("PD_Plate", PossessableSize.Small, false, false)]
        [TestCase("PD_Slipper", PossessableSize.Slipper, false, false)]
        [TestCase("PD_Stool", PossessableSize.Small, false, false)]
        [TestCase("PD_Chair", PossessableSize.Small, false, false)]
        [TestCase("PD_Barrel", PossessableSize.Large, true, false)]
        [TestCase("PD_Chest", PossessableSize.Large, true, false)]
        [TestCase("PD_Table", PossessableSize.Large, true, false)]
        [TestCase("PD_Shelf", PossessableSize.Shelf, false, false)]
        [TestCase("PD_Lamp", PossessableSize.Lamp, false, true)]
        [TestCase("PD_Radio", PossessableSize.Radio, false, false)]
        [TestCase("PD_Cologne", PossessableSize.Cologne, true, false)]
        [TestCase("PD_QuestItem", PossessableSize.QuestItem, true, false)]
        public void Classification_MatchesDesign(string asset, PossessableSize size, bool allowGoodJinn, bool isLamp)
        {
            PossessableDefinition d = Get(asset);
            Assert.AreEqual(size, d.Size);
            Assert.AreEqual(allowGoodJinn, d.AllowGoodJinn);
            Assert.AreEqual(isLamp, d.IsLamp);
            PossessableTraits t = PossessableTraits.From(d);
            Assert.AreEqual(isLamp, t.IsLamp);
            Assert.AreEqual(allowGoodJinn, t.AllowGoodJinn);
        }

        [Test]
        public void OnlyTheLamp_IsALamp()
        {
            foreach (PossessableDefinition d in _defs.Values)
            {
                Assert.AreEqual(d.name == "PD_Lamp", d.IsLamp, d.name);
            }
        }

        [Test]
        public void GoodJinnObjects_AreTheSlidersAndTheHarmlessCarryables()
        {
            var expected = new HashSet<string> { "PD_Barrel", "PD_Chest", "PD_Table", "PD_Cologne", "PD_QuestItem" };
            foreach (PossessableDefinition d in _defs.Values)
            {
                Assert.AreEqual(expected.Contains(d.name), d.AllowGoodJinn, d.name);
            }
        }

        [Test]
        public void GoodJinnActions_AreHarmless()
        {
            foreach (PossessableDefinition d in _defs.Values)
            {
                for (int i = 0; i < d.ActionCount; i++)
                {
                    PossessableActionDef a = d.GetAction(i);
                    if (!a.IsUsableBy(Role.GoodJinn))
                    {
                        continue;
                    }
                    Assert.AreEqual(0, a.Damage, d.name + "." + a.Id);
                    Assert.AreEqual(DamageFlags.None, a.Flags, d.name + "." + a.Id);
                    Assert.AreEqual(StatusType.None, a.ApplyStatus, d.name + "." + a.Id);
                }
            }
        }

        [Test]
        public void ActionUsers_FollowRoles()
        {
            PossessableActionDef slide = Get("PD_Table").Primary;
            Assert.AreEqual(ActionUser.Both, slide.UsableBy);
            Assert.IsTrue(slide.IsUsableBy(Role.EvilJinn));
            Assert.IsTrue(slide.IsUsableBy(Role.GoodJinn));
            Assert.IsFalse(slide.IsUsableBy(Role.Human));
            Assert.IsFalse(slide.IsUsableBy(Role.None));

            PossessableActionDef topple = Get("PD_Shelf").Primary;
            Assert.AreEqual(ActionUser.EvilOnly, topple.UsableBy);
            Assert.IsTrue(topple.IsUsableBy(Role.EvilJinn));
            Assert.IsFalse(topple.IsUsableBy(Role.GoodJinn));

            PossessableActionDef revive = Get("PD_Cologne").GetAction(PossessableDefinition.SecondaryIndex);
            Assert.AreEqual(ActionUser.GoodOnly, revive.UsableBy);
            Assert.IsFalse(revive.IsUsableBy(Role.EvilJinn));
            Assert.IsTrue(revive.IsUsableBy(Role.GoodJinn));
        }

        // ------------------------------------------------------------ Oyun §8 table

        // asset, action, telegraph, energy, cooldown, single use, damage
        [TestCase("PD_Bottle", "throw", 0.5f, 15f, 0f, true, 1)]
        [TestCase("PD_Candle", "throw", 0.5f, 15f, 0f, true, 1)]
        [TestCase("PD_Plate", "throw", 0.5f, 15f, 0f, true, 1)]
        [TestCase("PD_Slipper", "homing_slipper", 0.6f, 25f, 15f, false, 1)]
        [TestCase("PD_Stool", "lunge", 0.4f, 15f, 5f, false, 1)]
        [TestCase("PD_Chair", "lunge", 0.4f, 15f, 5f, false, 1)]
        [TestCase("PD_Barrel", "slide", 0.6f, 10f, 3f, false, 0)]
        [TestCase("PD_Chest", "slide", 0.6f, 10f, 3f, false, 0)]
        [TestCase("PD_Table", "slide", 0.6f, 10f, 3f, false, 0)]
        [TestCase("PD_Shelf", "topple", 1.0f, 35f, 0f, true, 1)]
        [TestCase("PD_Lamp", "burst", 1.2f, 30f, 20f, false, 0)]
        [TestCase("PD_Radio", "play_tune", 0.3f, 15f, 20f, false, 0)]
        [TestCase("PD_Cologne", "fling", 0.5f, 10f, 8f, false, 0)]
        [TestCase("PD_QuestItem", "fling", 0.5f, 10f, 8f, false, 0)]
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
            Assert.AreEqual(damage > 0, !string.IsNullOrEmpty(a.CauseId), "harmful actions carry a causeId, harmless ones do not");
        }

        [TestCase("PD_Bottle")]
        [TestCase("PD_Candle")]
        [TestCase("PD_Plate")]
        public void SmallThrowables_ArcThrowWithYawLimit(string asset)
        {
            PossessableDefinition d = Get(asset);
            Assert.AreEqual(ActionShape.Projectile, d.Primary.Shape);
            Assert.Greater(d.Primary.ProjectileArc, 0f, "arc throw");
            Assert.AreEqual(45f, d.YawLimitDeg, Eps, "A/D +-45");
            Assert.AreEqual(StatusType.None, d.Primary.ApplyStatus);
            Assert.AreEqual(ActionUser.EvilOnly, d.Primary.UsableBy);
            Assert.IsFalse(d.HasSecondary);
        }

        [Test]
        public void Slipper_FlatFastLine_OneDamageAndCameraSpin()
        {
            PossessableActionDef a = Get("PD_Slipper").Primary;
            Assert.AreEqual(ActionShape.Projectile, a.Shape);
            Assert.AreEqual(0f, a.ProjectileArc, Eps, "no arc: gravity-free straight line");
            Assert.Greater(a.ProjectileSpeed, Get("PD_Bottle").Primary.ProjectileSpeed, "faster than a thrown bottle");
            Assert.AreEqual(StatusType.CameraSpin180, a.ApplyStatus);
            Assert.AreEqual(0.15f, a.StatusDuration, Eps, "Tech §5.1: 180 degrees in 0.15 s");
            Assert.AreEqual(1, a.Damage);
            Assert.AreEqual(25f, a.EnergyCost, Eps);
            Assert.AreEqual(15f, a.Cooldown, Eps);
            Assert.IsFalse(a.SingleUse, "unbreakable");
            Assert.AreEqual(ActionUser.EvilOnly, a.UsableBy);
        }

        [TestCase("PD_Chair")]
        [TestCase("PD_Stool")]
        public void ChairAndStool_HopAndLunge(string asset)
        {
            PossessableDefinition d = Get(asset);
            Assert.IsTrue(d.CanMove);
            Assert.AreEqual(1f, d.HopDistance, Eps);
            Assert.AreEqual(0.5f, d.HopInterval, Eps);
            Assert.AreEqual(2f, d.HopEnergy, Eps);
            Assert.AreEqual(2f, d.Primary.ProjectileRange, Eps, "lunge distance 2 m");
            Assert.AreEqual(ActionUser.EvilOnly, d.Primary.UsableBy);
        }

        [TestCase("PD_Barrel")]
        [TestCase("PD_Chest")]
        [TestCase("PD_Table")]
        public void LargeFurniture_SlideOneCell_GoodJinnMayToo(string asset)
        {
            PossessableDefinition d = Get(asset);
            Assert.AreEqual(PossessableSize.Large, d.Size);
            Assert.IsFalse(d.CanMove);
            Assert.AreEqual(ActionUser.Both, d.Primary.UsableBy);
            Assert.AreEqual(1.5f, d.Primary.ProjectileRange, Eps, "one cell");
            Assert.Greater(d.Primary.ProjectileSpeed, 0f);
            Assert.AreEqual(0, d.Primary.Damage);
            Assert.AreEqual(DamageFlags.None, d.Primary.Flags);
        }

        [Test]
        public void Shelf_TopplesForwardWithKnockdown_SingleUse()
        {
            PossessableActionDef a = Get("PD_Shelf").Primary;
            Assert.AreEqual(ActionShape.Box, a.Shape);
            Assert.Greater(a.ShapeOffset.z, 0f, "hit box must be in front of the shelf");
            Assert.AreEqual(DamageFlags.Knockdown, a.Flags);
            Assert.AreEqual(2f, a.KnockdownTime, Eps, "KnockdownTime 2 s");
            Assert.IsTrue(a.SingleUse);
            Assert.AreEqual(ActionUser.EvilOnly, a.UsableBy);
        }

        [Test]
        public void Lamp_BurstsTheRoom_UntilRepaired()
        {
            PossessableDefinition d = Get("PD_Lamp");
            Assert.IsTrue(d.IsLamp);
            Assert.AreEqual(ActionUser.EvilOnly, d.Primary.UsableBy);
            Assert.AreEqual(0f, d.Primary.EffectDuration, Eps, "stays burst until the good jinn repairs it");
        }

        [Test]
        public void Radio_PlayTuneSuppressesNoiseFor12Seconds()
        {
            PossessableActionDef a = Get("PD_Radio").Primary;
            Assert.AreEqual(12f, a.EffectDuration, Eps);
            Assert.AreEqual(ActionUser.EvilOnly, a.UsableBy);
            Assert.AreEqual(0, a.Damage);
        }

        [Test]
        public void Cologne_FlingForBothJinns_ReviveSplashForTheGoodJinn()
        {
            PossessableDefinition d = Get("PD_Cologne");
            Assert.IsTrue(d.AllowGoodJinn);
            Assert.AreEqual(2, d.ActionCount);

            PossessableActionDef fling = d.Primary;
            Assert.AreEqual("fling", fling.Id);
            Assert.AreEqual(8f, fling.ProjectileRange, Eps, "flung 8 m");
            Assert.AreEqual(0, fling.Damage);
            Assert.AreEqual(ActionUser.Both, fling.UsableBy);

            PossessableActionDef splash = d.GetAction(PossessableDefinition.SecondaryIndex);
            Assert.AreEqual("revive_splash", splash.Id);
            Assert.AreEqual(ActionUser.GoodOnly, splash.UsableBy);
            Assert.IsTrue(splash.SingleUse, "Spent after the splash");
            Assert.AreEqual(0, splash.Damage);
        }

        [Test]
        public void QuestItem_FlingIsHarmlessAndGoodJinnFriendly()
        {
            PossessableDefinition d = Get("PD_QuestItem");
            Assert.AreEqual("fling", d.Primary.Id);
            Assert.AreEqual(ActionUser.Both, d.Primary.UsableBy);
            Assert.AreEqual(8f, d.Primary.ProjectileRange, Eps);
            Assert.IsFalse(d.HasSecondary);
        }

        [Test]
        public void OnlyCologne_HasASecondaryAction()
        {
            foreach (PossessableDefinition d in _defs.Values)
            {
                bool cologne = d.name == "PD_Cologne";
                Assert.AreEqual(cologne, d.HasSecondary, d.name);
                Assert.AreEqual(cologne, d.GetAction(PossessableDefinition.SecondaryIndex) != null, d.name);
            }
        }

        // ------------------------------------------------------------ CollectProblems catches v2.1 inconsistencies

        private static PossessableDefinition MakeDefinition(PossessableSize size, bool allowGoodJinn, bool isLamp,
            ActionUser user, int damage)
        {
            var def = ScriptableObject.CreateInstance<PossessableDefinition>();
            def.name = "PD_Test";
            var so = new SerializedObject(def);
            so.FindProperty("id").stringValue = "test";
            so.FindProperty("nameKey").stringValue = "poss.test";
            so.FindProperty("size").enumValueIndex = (int)size;
            so.FindProperty("allowGoodJinn").boolValue = allowGoodJinn;
            so.FindProperty("isLamp").boolValue = isLamp;
            so.FindProperty("primary.id").stringValue = "slide";
            so.FindProperty("primary.nameKey").stringValue = "action.slide";
            so.FindProperty("primary.causeId").stringValue = "test.slide";
            so.FindProperty("primary.usableBy").enumValueIndex = (int)user;
            so.FindProperty("primary.damage").intValue = damage;
            so.ApplyModifiedPropertiesWithoutUndo();
            return def;
        }

        private static List<string> ProblemsOf(PossessableDefinition def)
        {
            var problems = new List<string>();
            def.CollectProblems(problems);
            Object.DestroyImmediate(def);
            return problems;
        }

        [Test]
        public void Validation_ConsistentDefinition_IsClean()
        {
            Assert.IsEmpty(ProblemsOf(MakeDefinition(PossessableSize.Large, true, false, ActionUser.Both, 0)));
            Assert.IsEmpty(ProblemsOf(MakeDefinition(PossessableSize.Large, false, false, ActionUser.EvilOnly, 1)));
        }

        [Test]
        public void Validation_GoodJinnFlagWithoutGoodAction_IsReported()
        {
            Assert.IsNotEmpty(ProblemsOf(MakeDefinition(PossessableSize.Large, true, false, ActionUser.EvilOnly, 0)));
        }

        [Test]
        public void Validation_GoodActionWithoutGoodJinnFlag_IsReported()
        {
            Assert.IsNotEmpty(ProblemsOf(MakeDefinition(PossessableSize.Large, false, false, ActionUser.Both, 0)));
        }

        [Test]
        public void Validation_HarmfulGoodJinnAction_IsReported()
        {
            Assert.IsNotEmpty(ProblemsOf(MakeDefinition(PossessableSize.Large, true, false, ActionUser.Both, 1)));
        }

        [Test]
        public void Validation_LampFlagMustMatchLampSize()
        {
            Assert.IsNotEmpty(ProblemsOf(MakeDefinition(PossessableSize.Large, false, true, ActionUser.EvilOnly, 0)));
            Assert.IsNotEmpty(ProblemsOf(MakeDefinition(PossessableSize.Lamp, false, false, ActionUser.EvilOnly, 0)));
            Assert.IsEmpty(ProblemsOf(MakeDefinition(PossessableSize.Lamp, false, true, ActionUser.EvilOnly, 0)));
        }
    }
}
