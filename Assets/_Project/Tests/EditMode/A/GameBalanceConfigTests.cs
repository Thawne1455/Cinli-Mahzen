using CinliMahzen.Core;
using NUnit.Framework;
using UnityEditor;

namespace CinliMahzen.Tests.EditMode.A
{
    public class GameBalanceConfigTests
    {
        private const string Path = "Assets/_Project/ScriptableObjects/Config/GameBalanceConfig.asset";
        private const string ItemsDir = "Assets/_Project/ScriptableObjects/Items/";

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
            Assert.AreEqual(30f, c.LampBurstEnergy);
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
