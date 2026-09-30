using System.Collections.Generic;
using CinliMahzen.Core;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.A
{
    public class GameRandomTests
    {
        [Test]
        public void SameSeed_Same1000Numbers()
        {
            var a = new GameRandom(12345);
            var b = new GameRandom(12345);
            for (int i = 0; i < 1000; i++)
            {
                Assert.AreEqual(a.Range(0, 1000000), b.Range(0, 1000000), "int #" + i);
                Assert.AreEqual(a.Range(-5f, 5f), b.Range(-5f, 5f), "float #" + i);
            }
        }

        [Test]
        public void DifferentSeed_DifferentSequence()
        {
            var a = new GameRandom(1);
            var b = new GameRandom(2);
            int same = 0;
            for (int i = 0; i < 100; i++)
            {
                if (a.Range(0, 1000000) == b.Range(0, 1000000))
                    same++;
            }
            Assert.Less(same, 5);
        }

        [Test]
        public void Range_StaysInBounds()
        {
            var r = new GameRandom(7);
            for (int i = 0; i < 1000; i++)
            {
                int n = r.Range(3, 6);
                Assert.GreaterOrEqual(n, 3);
                Assert.Less(n, 6);
                float f = r.Range(1f, 2f);
                Assert.GreaterOrEqual(f, 1f);
                Assert.LessOrEqual(f, 2f);
            }
            Assert.AreEqual(4, r.Range(4, 4), "empty range returns min");
        }

        [Test]
        public void Shuffle_IsDeterministicPermutation()
        {
            var x = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var y = new List<int>(x);
            new GameRandom(99).Shuffle(x);
            new GameRandom(99).Shuffle(y);
            CollectionAssert.AreEqual(x, y);
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, x);
        }

        [Test]
        public void WeightedPick_RespectsWeights()
        {
            var r = new GameRandom(5);
            var weights = new List<float> { 0f, 1f, 3f };
            var counts = new int[3];
            for (int i = 0; i < 4000; i++)
                counts[r.WeightedPick(weights)]++;
            Assert.AreEqual(0, counts[0], "zero weight never picked");
            Assert.That(counts[2], Is.InRange(2700, 3300));
            Assert.AreEqual(-1, r.WeightedPick(new List<float> { 0f, 0f }));
        }

        [Test]
        public void Pick_ReturnsElement()
        {
            var r = new GameRandom(3);
            var list = new[] { "a", "b", "c" };
            for (int i = 0; i < 50; i++)
                CollectionAssert.Contains(list, r.Pick(list));
        }
    }
}
