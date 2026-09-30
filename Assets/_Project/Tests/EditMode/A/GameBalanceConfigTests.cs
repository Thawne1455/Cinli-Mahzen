using CinliMahzen.Core;
using NUnit.Framework;
using UnityEditor;

namespace CinliMahzen.Tests.EditMode.A
{
    public class GameBalanceConfigTests
    {
        private const string Path = "Assets/_Project/ScriptableObjects/Config/GameBalanceConfig.asset";

        [Test]
        public void Asset_Exists_WithGddValues()
        {
            var c = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(Path);
            Assert.IsNotNull(c, Path);
            Assert.AreEqual(600f, c.RoundDuration);
            Assert.AreEqual(3, c.HumanMaxHp);
            Assert.AreEqual(3f, c.PossessRange);
            Assert.AreEqual(0.5f, c.NetRangeTolerance);
            Assert.AreEqual(2, c.ItemSlotCount);
        }

        [Test]
        public void ItemAssets_Exist()
        {
            var salt = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/ScriptableObjects/Items/ID_Salt.asset");
            var nazar = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/ScriptableObjects/Items/ID_Nazar.asset");
            Assert.IsNotNull(salt);
            Assert.IsNotNull(nazar);
            Assert.AreEqual("salt", salt.Id);
            Assert.AreEqual(ItemUseType.Deploy, salt.UseType);
            Assert.AreEqual("nazar", nazar.Id);
            Assert.AreEqual(ItemUseType.Passive, nazar.UseType);
        }
    }
}
