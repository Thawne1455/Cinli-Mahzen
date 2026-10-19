using CinliMahzen.Core;
using CinliMahzen.Objectives;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.Objectives
{
    /// <summary>TODO 2.7: <see cref="InventoryState"/> — 3 quest-item slots per player.</summary>
    public class InventoryStateTests
    {
        private static readonly PlayerId Human = new PlayerId(0);
        private static readonly PlayerId Other = new PlayerId(1);

        [Test]
        public void StartsEmpty_AndFillsSlotsInOrder()
        {
            var inv = new InventoryState(3);

            Assert.AreEqual(-1, inv.LastFilledSlot(Human));
            CollectionAssert.AreEqual(new[] { "", "", "" }, inv.Snapshot(Human));

            Assert.AreEqual(0, inv.TryAdd(Human, "house_key"));
            Assert.AreEqual(1, inv.TryAdd(Human, "paint"));
            Assert.IsTrue(inv.Has(Human, "paint"));
            Assert.IsFalse(inv.Has(Other, "paint"), "slots are per player");
            Assert.AreEqual(1, inv.LastFilledSlot(Human));
        }

        [Test]
        public void TryAdd_RefusesDuplicates_EmptyIds_AndAFullInventory()
        {
            var inv = new InventoryState(3);
            inv.TryAdd(Human, "a");

            Assert.AreEqual(-1, inv.TryAdd(Human, "a"), "the same quest item twice");
            Assert.AreEqual(-1, inv.TryAdd(Human, ""));
            Assert.AreEqual(-1, inv.TryAdd(Human, null));

            inv.TryAdd(Human, "b");
            inv.TryAdd(Human, "c");
            Assert.IsTrue(inv.IsFull(Human));
            Assert.AreEqual(-1, inv.TryAdd(Human, "d"));
        }

        [Test]
        public void TryRemoveAt_EmptiesTheSlot_AndTheNextItemFillsTheHole()
        {
            var inv = new InventoryState(3);
            inv.TryAdd(Human, "a");
            inv.TryAdd(Human, "b");

            Assert.IsTrue(inv.TryRemoveAt(Human, 0, out string removed));
            Assert.AreEqual("a", removed);
            Assert.IsFalse(inv.TryRemoveAt(Human, 0, out _), "already empty");
            Assert.IsFalse(inv.TryRemoveAt(Human, 5, out _), "out of range");

            Assert.AreEqual(0, inv.TryAdd(Human, "c"), "first empty slot");
        }

        [Test]
        public void Apply_ReplacesTheSlots_OnlyWithAValidList()
        {
            var inv = new InventoryState(3);

            Assert.IsTrue(inv.Apply(Human, new[] { "x", "", "y" }));
            Assert.AreEqual("y", inv.Get(Human, 2));
            Assert.IsFalse(inv.Apply(Human, new[] { "x" }), "wrong length");
            Assert.IsFalse(inv.Apply(Human, new[] { "x", null, "y" }), "null slot");
            Assert.AreEqual("x", inv.Get(Human, 0), "unchanged after a refused list");
        }

        [Test]
        public void Clear_EmptiesEveryone()
        {
            var inv = new InventoryState(3);
            inv.TryAdd(Human, "a");
            inv.TryAdd(Other, "b");

            inv.Clear();

            Assert.IsFalse(inv.Has(Human, "a"));
            Assert.IsFalse(inv.Has(Other, "b"));
        }

        [Test]
        public void Constructor_NeedsAtLeastOneSlot()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new InventoryState(0));
        }
    }
}
