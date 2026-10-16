using CinliMahzen.Core;
using CinliMahzen.Player;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.Player
{
    /// <summary>Teknik §2 culling table, per local role.</summary>
    public class CullingMasksTests
    {
        private const int LayerCount = 32;

        // Layers that only some roles see (Teknik §2 layers 11, 13, 14, 18; 12 is seen by everyone).
        private static readonly int[] JinnLayers =
        {
            CullingMasks.EvilJinnVisualLayer,
            CullingMasks.SpiritOnlyLayer,
            CullingMasks.JinnOnlyFxLayer,
            CullingMasks.XRayOutlineLayer,
        };

        private static bool Sees(int mask, int layer)
        {
            return (mask & (1 << layer)) != 0;
        }

        // ---- Human ----

        [Test]
        public void Human_SeesTheFaintGoodJinn_ButNothingJinnOnly()
        {
            int mask = CullingMasks.For(Role.Human);

            Assert.IsTrue(Sees(mask, CullingMasks.GoodJinnVisualLayer), "GoodJinnVisual");
            Assert.IsFalse(Sees(mask, CullingMasks.EvilJinnVisualLayer), "EvilJinnVisual");
            Assert.IsFalse(Sees(mask, CullingMasks.SpiritOnlyLayer), "SpiritOnly");
            Assert.IsFalse(Sees(mask, CullingMasks.JinnOnlyFxLayer), "JinnOnlyFX");
            Assert.IsFalse(Sees(mask, CullingMasks.XRayOutlineLayer), "XRayOutline");
        }

        [Test]
        public void Human_SeesEveryOtherLayer()
        {
            int mask = CullingMasks.For(Role.Human);

            for (int layer = 0; layer < LayerCount; layer++)
            {
                if (System.Array.IndexOf(JinnLayers, layer) >= 0)
                    continue;
                Assert.IsTrue(Sees(mask, layer), "layer " + layer);
            }
        }

        // ---- Good jinn ----

        [Test]
        public void GoodJinn_SeesBothJinnVisuals_SpiritOnly_AndXRay_ButNotJinnOnlyFx()
        {
            int mask = CullingMasks.For(Role.GoodJinn);

            Assert.IsTrue(Sees(mask, CullingMasks.GoodJinnVisualLayer), "GoodJinnVisual");
            Assert.IsTrue(Sees(mask, CullingMasks.EvilJinnVisualLayer), "EvilJinnVisual");
            Assert.IsTrue(Sees(mask, CullingMasks.SpiritOnlyLayer), "SpiritOnly");
            Assert.IsTrue(Sees(mask, CullingMasks.XRayOutlineLayer), "XRayOutline");
            Assert.IsFalse(Sees(mask, CullingMasks.JinnOnlyFxLayer), "JinnOnlyFX");
        }

        [Test]
        public void GoodJinn_SeesEveryLayerExceptJinnOnlyFx()
        {
            int mask = CullingMasks.For(Role.GoodJinn);

            for (int layer = 0; layer < LayerCount; layer++)
            {
                if (layer == CullingMasks.JinnOnlyFxLayer)
                    continue;
                Assert.IsTrue(Sees(mask, layer), "layer " + layer);
            }
        }

        // ---- Evil jinn ----

        [Test]
        public void EvilJinn_SeesEverything()
        {
            int mask = CullingMasks.For(Role.EvilJinn);

            Assert.AreEqual(~0, mask);
            foreach (int layer in JinnLayers)
                Assert.IsTrue(Sees(mask, layer), "layer " + layer);
            Assert.IsTrue(Sees(mask, CullingMasks.GoodJinnVisualLayer), "GoodJinnVisual");
        }

        // ---- Across roles ----

        [Test]
        public void MasksNestFromTheMostRestrictedToTheLeast()
        {
            int human = CullingMasks.For(Role.Human);
            int good = CullingMasks.For(Role.GoodJinn);
            int evil = CullingMasks.For(Role.EvilJinn);

            Assert.AreEqual(0, human & ~good, "everything the human sees, the good jinn sees");
            Assert.AreEqual(0, good & ~evil, "everything the good jinn sees, the evil jinn sees");
            Assert.AreNotEqual(human, good);
            Assert.AreNotEqual(good, evil);
        }

        [Test]
        public void NoRole_GetsTheHumanMask_TheOneThatRevealsTheLeast()
        {
            Assert.AreEqual(CullingMasks.For(Role.Human), CullingMasks.For(Role.None));
        }

        [Test]
        public void EnvironmentLayer_IsVisibleToEveryRole()
        {
            foreach (Role role in new[] { Role.None, Role.Human, Role.GoodJinn, Role.EvilJinn })
                Assert.IsTrue(Sees(CullingMasks.For(role), CullingMasks.EnvironmentLayer), role.ToString());
        }

        // ---- The layer table matches the project ----

        [TestCase(CullingMasks.EnvironmentLayer, "Environment")]
        [TestCase(CullingMasks.EvilJinnVisualLayer, "EvilJinnVisual")]
        [TestCase(CullingMasks.GoodJinnVisualLayer, "GoodJinnVisual")]
        [TestCase(CullingMasks.SpiritOnlyLayer, "SpiritOnly")]
        [TestCase(CullingMasks.JinnOnlyFxLayer, "JinnOnlyFX")]
        [TestCase(CullingMasks.XRayOutlineLayer, "XRayOutline")]
        public void LayerNumber_MatchesTheNameInTagManager(int layer, string expectedName)
        {
            Assert.AreEqual(expectedName, LayerMask.LayerToName(layer), "TagManager layer " + layer + " was renumbered (Teknik §2)");
        }
    }
}
