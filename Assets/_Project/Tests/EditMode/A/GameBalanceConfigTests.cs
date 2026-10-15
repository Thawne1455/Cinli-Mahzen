using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using CinliMahzen.Core;
using NUnit.Framework;
using UnityEditor;

namespace CinliMahzen.Tests.EditMode.A
{
    public class GameBalanceConfigTests
    {
        private const string Path = "Assets/_Project/ScriptableObjects/Config/GameBalanceConfig.asset";
        private const string ItemsDir = "Assets/_Project/ScriptableObjects/Items/";
        private const double Tolerance = 1e-4;

        private static void AssertGddValues(GameBalanceConfig c)
        {
            // Match
            Assert.AreEqual(360f, c.BaseRoundDuration);
            Assert.AreEqual(90f, c.StageTimeBonus);
            Assert.AreEqual(10f, c.JinnWakeDelay);
            Assert.AreEqual(5f, c.IntroCountdown);
            Assert.AreEqual(4, c.RoundsPerMatch);
            Assert.AreEqual(2, c.StagesActive);
            Assert.AreEqual(2, c.PuzzlesPerStage);
            Assert.AreEqual(3f, c.RoleRevealDuration);
            Assert.AreEqual(8f, c.RoundEndDuration);

            // Human
            Assert.AreEqual(3, c.HumanMaxHp);
            Assert.AreEqual(12f, c.FaintDuration);
            Assert.AreEqual(3, c.FaintsToLose);
            Assert.AreEqual(1.5f, c.PushHoldTime);
            Assert.AreEqual(18f, c.DigTotalTime);
            Assert.AreEqual(3, c.DigCheckpoints);
            Assert.AreEqual(1, c.ConfirmFailDamage);
            Assert.AreEqual(5f, c.ConfirmFailCooldown);

            // Lantern & Items
            Assert.AreEqual(2.5f, c.LanternSafeZoneRadius);
            Assert.AreEqual(3, c.ItemSlotCount);

            // Jinn
            Assert.AreEqual(3f, c.PossessRange);
            Assert.AreEqual(2f, c.LitRoomEjectDelay);

            // Good Jinn
            Assert.AreEqual(100f, c.GoodEnergyMax);
            Assert.AreEqual(8f, c.GoodEnergyRegen);
            Assert.AreEqual(2f, c.FocusReferenceCost);
            Assert.AreEqual(2f, c.SpiritBumpBlurTime);
            Assert.AreEqual(1.5f, c.SpiritBumpRange);
            Assert.AreEqual(6f, c.SpiritBumpCooldown);
            Assert.AreEqual(30f, c.SpiritBlockEnergyCost);
            Assert.AreEqual(1f, c.SpiritBlockRadius);
            Assert.AreEqual(3f, c.CologneReviveRange);
            Assert.AreEqual(10f, c.KeySenseRange);

            // Sabotage & Lights
            Assert.AreEqual(25f, c.ScrambleEnergyCost);
            Assert.AreEqual(1.5f, c.ScrambleHoldTime);
            Assert.AreEqual(3f, c.ScrambleRange);
            Assert.AreEqual(15f, c.ScramblePuzzleCooldown);
            Assert.AreEqual(8f, c.LampLightRange);
            Assert.AreEqual(2f, c.RepairHoldTime);

            // Score
            Assert.AreEqual(50, c.ScorePerPuzzle);
            Assert.AreEqual(50, c.ScoreBasementOpen);
            Assert.AreEqual(150, c.ScoreTreasureWin);
            Assert.AreEqual(50, c.ScorePerFaint);
            Assert.AreEqual(200, c.JinnWinPoints);
            Assert.AreEqual(40, c.ScorePerUnfinishedPuzzle);

            // Network
            Assert.AreEqual(0.5f, c.NetRangeTolerance);
        }

        [Test]
        public void Defaults_MatchGdd()
        {
            var c = UnityEngine.ScriptableObject.CreateInstance<GameBalanceConfig>();
            try
            {
                AssertGddValues(c);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(c);
            }
        }

        [Test]
        public void Asset_Exists_WithGddValues()
        {
            var c = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(Path);
            Assert.IsNotNull(c, Path);
            AssertGddValues(c);
        }

        [Test]
        public void Config_MatchesGddTable()
        {
            // Oyun §14 table is the spec: "| `Name` [/ `Name`...] | value [/ value...] | note |" rows.
            string gddPath = System.IO.Path.Combine(UnityEngine.Application.dataPath, "..", "docs", "01_GDD_Oyun.md");
            string[] lines = File.ReadAllLines(gddPath, System.Text.Encoding.UTF8);

            int start = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("## 14.", StringComparison.Ordinal))
                {
                    start = i;
                    break;
                }
            }
            Assert.IsTrue(start >= 0, "GDD section '## 14.' not found in " + gddPath);

            var nameRegex = new Regex("`([A-Za-z0-9]+)`");
            var expected = new Dictionary<string, double>();
            for (int i = start + 1; i < lines.Length && !lines[i].StartsWith("## ", StringComparison.Ordinal); i++)
            {
                string line = lines[i];
                if (!line.StartsWith("| `", StringComparison.Ordinal))
                    continue;

                string[] cells = line.Split('|');
                var names = new List<string>();
                foreach (System.Text.RegularExpressions.Match m in nameRegex.Matches(cells[1]))
                    names.Add(m.Groups[1].Value);
                string[] values = cells[2].Split('/');

                Assert.AreEqual(names.Count, values.Length,
                    $"GDD row has {names.Count} names but {values.Length} values: {line.Trim()}");
                for (int k = 0; k < names.Count; k++)
                {
                    Assert.IsFalse(expected.ContainsKey(names[k]), $"GDD lists {names[k]} twice");
                    expected[names[k]] = double.Parse(values[k].Trim(), CultureInfo.InvariantCulture);
                }
            }
            Assert.IsNotEmpty(expected, "no rows parsed from GDD section 14");

            const BindingFlags instancePublic = BindingFlags.Instance | BindingFlags.Public;
            var defaults = UnityEngine.ScriptableObject.CreateInstance<GameBalanceConfig>();
            try
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(Path);
                Assert.IsNotNull(asset, Path);

                // Forward: every GDD name is a config property and matches the table, for defaults and for the asset.
                foreach (KeyValuePair<string, double> row in expected)
                {
                    PropertyInfo prop = typeof(GameBalanceConfig).GetProperty(row.Key, instancePublic);
                    Assert.IsNotNull(prop, $"GDD lists {row.Key} but GameBalanceConfig has no such property");
                    AssertConfigValue(defaults, prop, row.Key, row.Value, "defaults");
                    AssertConfigValue(asset, prop, row.Key, row.Value, "asset");
                }

                // Reverse: every numeric config property is listed in the GDD table.
                foreach (PropertyInfo prop in typeof(GameBalanceConfig).GetProperties(instancePublic))
                {
                    Type t = prop.PropertyType;
                    if (t != typeof(int) && t != typeof(float) && t != typeof(bool))
                        continue;
                    Assert.IsTrue(expected.ContainsKey(prop.Name),
                        $"GameBalanceConfig.{prop.Name} is not in the Oyun §14 table");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(defaults);
            }
        }

        private static void AssertConfigValue(GameBalanceConfig config, PropertyInfo prop, string name, double expected, string source)
        {
            double actual = Convert.ToDouble(prop.GetValue(config), CultureInfo.InvariantCulture);
            Assert.AreEqual(expected, actual, Tolerance, $"{name} ({source}): config has {actual}, GDD §14 says {expected}");
        }

        [TestCase("ID_HouseKey", "house_key", ItemKind.QuestItem)]
        [TestCase("ID_BlessedOil", "blessed_oil", ItemKind.LanternUpgrade)]
        [TestCase("ID_FuseKey", "fuse_key", ItemKind.QuestItem)]
        [TestCase("ID_Paint", "paint", ItemKind.QuestItem)]
        [TestCase("ID_Shovel", "shovel", ItemKind.QuestItem)]
        [TestCase("ID_CellarKey", "cellar_key", ItemKind.QuestItem)]
        public void ItemAssets_Exist(string assetName, string expectedId, ItemKind expectedKind)
        {
            var path = ItemsDir + assetName + ".asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            Assert.IsNotNull(item, path);
            Assert.AreEqual(expectedId, item.Id);
            Assert.AreEqual("item." + expectedId, item.NameKey);
            Assert.AreEqual(expectedKind, item.Kind);
        }
    }
}
