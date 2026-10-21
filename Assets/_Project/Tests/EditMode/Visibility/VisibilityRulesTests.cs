using CinliMahzen.Core;
using CinliMahzen.Visibility;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.Visibility
{
    /// <summary>TODO 3.9: the Oyun §12 table as <see cref="VisibilityRules"/> decides it (ranges 20 / 4 / 25 / 3).</summary>
    public class VisibilityRulesTests
    {
        private static readonly VisibilityRules Rules = new VisibilityRules(20f, 4f, 25f, 3f);

        [Test]
        public void EvilSpirit_GoodJinnSeesWithin20m_TeamAlways_HumanNever()
        {
            Assert.IsTrue(Rules.SeesEvilSpirit(Role.GoodJinn, 19.9f));
            Assert.IsFalse(Rules.SeesEvilSpirit(Role.GoodJinn, 20.1f));
            Assert.IsTrue(Rules.SeesEvilSpirit(Role.EvilJinn, 500f));
            Assert.IsFalse(Rules.SeesEvilSpirit(Role.Human, 1f));
        }

        [Test]
        public void Lurking_GoodJinnSeesWithin4m()
        {
            Assert.AreEqual(LurkMarker.Lurking, Rules.MarkerFor(Role.GoodJinn, PossessionPhase.Lurking, 3.5f));
            Assert.AreEqual(LurkMarker.None, Rules.MarkerFor(Role.GoodJinn, PossessionPhase.Lurking, 4.5f));
            Assert.AreEqual(LurkMarker.Lurking, Rules.MarkerFor(Role.GoodJinn, PossessionPhase.Recovering, 1f));
        }

        [Test]
        public void Charging_GoodJinnSeesRedWithin25m()
        {
            Assert.AreEqual(LurkMarker.Charging, Rules.MarkerFor(Role.GoodJinn, PossessionPhase.Charging, 24f));
            Assert.AreEqual(LurkMarker.None, Rules.MarkerFor(Role.GoodJinn, PossessionPhase.Charging, 26f));
        }

        [Test]
        public void EvilTeam_AlwaysSeesMarkers_HumanNever()
        {
            Assert.AreEqual(LurkMarker.Lurking, Rules.MarkerFor(Role.EvilJinn, PossessionPhase.Lurking, 100f));
            Assert.AreEqual(LurkMarker.Charging, Rules.MarkerFor(Role.EvilJinn, PossessionPhase.Charging, 100f));
            Assert.AreEqual(LurkMarker.None, Rules.MarkerFor(Role.Human, PossessionPhase.Charging, 1f));
        }

        [Test]
        public void NoMarker_WhenNobodyIsInside()
        {
            Assert.AreEqual(LurkMarker.None, Rules.MarkerFor(Role.EvilJinn, PossessionPhase.Free, 1f));
            Assert.AreEqual(LurkMarker.None, Rules.MarkerFor(Role.EvilJinn, PossessionPhase.Entering, 1f));
            Assert.AreEqual(LurkMarker.None, Rules.MarkerFor(Role.EvilJinn, PossessionPhase.Spent, 1f));
            Assert.IsFalse(VisibilityRules.IsInside(PossessionPhase.Entering));
            Assert.IsTrue(VisibilityRules.IsInside(PossessionPhase.Charging));
        }

        [Test]
        public void ColdBreath_Within3m()
        {
            Assert.IsTrue(Rules.ColdBreath(2.9f));
            Assert.IsFalse(Rules.ColdBreath(3.1f));
            Assert.IsFalse(Rules.ColdBreath(float.PositiveInfinity));
        }

        [Test]
        public void FromConfig_UsesTheBalanceValues()
        {
            var config = UnityEngine.ScriptableObject.CreateInstance<GameBalanceConfig>();
            VisibilityRules rules = VisibilityRules.FromConfig(config);
            Assert.AreEqual(config.GoodSightSpiritRange, rules.SpiritRange);
            Assert.AreEqual(config.GoodSightLurkRange, rules.LurkRange);
            Assert.AreEqual(config.GoodSightChargeRange, rules.ChargeRange);
            Assert.AreEqual(config.ColdBreathRange, rules.ColdBreathRange);
            UnityEngine.Object.DestroyImmediate(config);
        }
    }
}
