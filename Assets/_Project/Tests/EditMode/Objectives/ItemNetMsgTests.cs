using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.Objectives
{
    /// <summary>TODO 2.7 codecs: GateOpened, ItemSpawned / ItemDropped, ItemPickedUp, InventoryChanged, ReqDropItem (round trip + rejection).</summary>
    public class ItemNetMsgTests
    {
        private static readonly PlayerId Human = new PlayerId(0);
        private static readonly NetId Item = new NetId(60005);

        private static NetMsg With(MsgCode code, params object[] payload)
        {
            return new NetMsg { Code = code, Payload = payload };
        }

        [Test]
        public void GateOpened_RoundTrip_AndRejects()
        {
            NetMsg msg = GateOpenedMsg.Encode(2);
            Assert.AreEqual(MsgCode.GateOpened, msg.Code);
            Assert.IsTrue(GateOpenedMsg.TryDecode(msg, out int stage));
            Assert.AreEqual(2, stage);

            Assert.IsFalse(GateOpenedMsg.TryDecode(With(MsgCode.GateOpened, -1), out _), "negative stage");
            Assert.IsFalse(GateOpenedMsg.TryDecode(With(MsgCode.GateOpened, "1"), out _), "wrong type");
            Assert.IsFalse(GateOpenedMsg.TryDecode(With(MsgCode.StageCompleted, 1), out _), "wrong code");
            Assert.IsFalse(GateOpenedMsg.TryDecode(new NetMsg { Code = MsgCode.GateOpened }, out _), "null payload");
        }

        [Test]
        public void ItemSpawned_AndItemDropped_RoundTrip()
        {
            var pos = new Vector3(1f, 2f, 3f);
            foreach (bool dropped in new[] { false, true })
            {
                NetMsg msg = ItemSpawnedMsg.Encode(Item, "paint", pos, dropped);
                Assert.AreEqual(dropped ? MsgCode.ItemDropped : MsgCode.ItemSpawned, msg.Code);
                Assert.IsTrue(ItemSpawnedMsg.TryDecode(msg, out NetId item, out string itemId, out Vector3 position, out bool wasDropped));
                Assert.AreEqual(Item, item);
                Assert.AreEqual("paint", itemId);
                Assert.AreEqual(pos, position);
                Assert.AreEqual(dropped, wasDropped);
            }
        }

        [Test]
        public void ItemSpawned_Rejects()
        {
            Assert.IsFalse(ItemSpawnedMsg.TryDecode(With(MsgCode.ItemSpawned, 0, "paint", Vector3.zero), out _, out _, out _, out _), "no id");
            Assert.IsFalse(ItemSpawnedMsg.TryDecode(With(MsgCode.ItemSpawned, 5, "", Vector3.zero), out _, out _, out _, out _), "empty item");
            Assert.IsFalse(ItemSpawnedMsg.TryDecode(With(MsgCode.ItemSpawned, 5, "paint"), out _, out _, out _, out _), "short");
            Assert.IsFalse(ItemSpawnedMsg.TryDecode(With(MsgCode.ItemPickedUp, 5, "paint", Vector3.zero), out _, out _, out _, out _), "wrong code");
        }

        [Test]
        public void ItemPickedUp_RoundTrip_AndRejects()
        {
            Assert.IsTrue(ItemPickedUpMsg.TryDecode(ItemPickedUpMsg.Encode(Item, Human), out NetId item, out PlayerId who));
            Assert.AreEqual(Item, item);
            Assert.AreEqual(Human, who);

            Assert.IsFalse(ItemPickedUpMsg.TryDecode(With(MsgCode.ItemPickedUp, 5, PlayerId.None.Value), out _, out _), "no player");
            Assert.IsFalse(ItemPickedUpMsg.TryDecode(With(MsgCode.ItemPickedUp, 0, (byte)0), out _, out _), "no item");
            Assert.IsFalse(ItemPickedUpMsg.TryDecode(With(MsgCode.ItemPickedUp, 5, 0), out _, out _), "player not a byte");
        }

        [Test]
        public void InventoryChanged_RoundTrip_CopiesAndNormalisesNulls()
        {
            var slots = new[] { "paint", null, "shovel" };
            NetMsg msg = InventoryChangedMsg.Encode(Human, slots);
            slots[0] = "changed";

            Assert.IsTrue(InventoryChangedMsg.TryDecode(msg, out PlayerId who, out string[] decoded));
            Assert.AreEqual(Human, who);
            CollectionAssert.AreEqual(new[] { "paint", "", "shovel" }, decoded);
        }

        [Test]
        public void InventoryChanged_Rejects()
        {
            Assert.IsFalse(InventoryChangedMsg.TryDecode(With(MsgCode.InventoryChanged, (byte)0, new string[0]), out _, out _), "empty");
            Assert.IsFalse(InventoryChangedMsg.TryDecode(With(MsgCode.InventoryChanged, (byte)0, new string[] { null }), out _, out _), "null slot");
            Assert.IsFalse(InventoryChangedMsg.TryDecode(With(MsgCode.InventoryChanged, (byte)0, "paint"), out _, out _), "not an array");
            Assert.IsFalse(InventoryChangedMsg.TryDecode(With(MsgCode.InventoryChanged, PlayerId.None.Value, new[] { "" }), out _, out _), "no player");
        }

        [Test]
        public void ReqDropItem_RoundTrip_AndRejects()
        {
            Assert.IsTrue(ReqDropItemMsg.TryDecode(ReqDropItemMsg.Encode(2), out int slot));
            Assert.AreEqual(2, slot);
            Assert.IsFalse(ReqDropItemMsg.TryDecode(With(MsgCode.ReqDropItem, -1), out _));
            Assert.IsFalse(ReqDropItemMsg.TryDecode(With(MsgCode.ReqDropItem), out _));
        }
    }
}
