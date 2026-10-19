using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using CinliMahzen.Jinn;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.Jinn
{
    /// <summary>
    /// Round trip and rejection cases for the four spirit codecs (Teknik §4.3, §6.1, §6.5):
    /// ReqFocusReference, ReferenceFocusChanged, ReqSpiritBump and SpiritBumped.
    /// </summary>
    public class SpiritNetMsgTests
    {
        private static readonly PlayerId Evil = new PlayerId(2);
        private static readonly PlayerId Good = new PlayerId(1);

        /// <summary>Teknik §4.3: payload may only hold PUN-serializable types.</summary>
        private static void AssertPunSerializable(object[] payload)
        {
            Assert.IsNotNull(payload);
            for (int i = 0; i < payload.Length; i++)
            {
                object o = payload[i];
                Assert.IsNotNull(o, "payload[" + i + "] is null");
                bool ok = o is byte || o is bool || o is short || o is int || o is long || o is float || o is double
                          || o is string || o is Vector2 || o is Vector3 || o is Quaternion
                          || o is byte[] || o is int[] || o is float[] || o is object[];
                Assert.IsTrue(ok, "payload[" + i + "] has non-serializable type " + o.GetType());
            }
        }

        // ---- ReqFocusReference ----

        [Test]
        public void ReqFocusReference_Encode_UsesItsCode_AndPunSerializableTypes()
        {
            NetMsg msg = ReqFocusReferenceMsg.Encode(true);

            Assert.AreEqual(MsgCode.ReqFocusReference, msg.Code);
            Assert.AreEqual(1, msg.Payload.Length);
            Assert.IsInstanceOf<bool>(msg.Payload[0]);
            AssertPunSerializable(msg.Payload);
        }

        [Test]
        public void ReqFocusReference_RoundTrip_PreservesBothValues()
        {
            Assert.IsTrue(ReqFocusReferenceMsg.TryDecode(ReqFocusReferenceMsg.Encode(true), out bool on));
            Assert.IsTrue(on);

            Assert.IsTrue(ReqFocusReferenceMsg.TryDecode(ReqFocusReferenceMsg.Encode(false), out bool off));
            Assert.IsFalse(off);
        }

        [Test]
        public void ReqFocusReference_TryDecode_RejectsWrongCodeNullWrongLengthAndWrongType()
        {
            var otherCode = new NetMsg { Code = MsgCode.ReqSpiritBump, Payload = new object[] { true } };
            var nullPayload = new NetMsg { Code = MsgCode.ReqFocusReference };
            var emptyPayload = new NetMsg { Code = MsgCode.ReqFocusReference, Payload = new object[0] };
            var extraElement = new NetMsg { Code = MsgCode.ReqFocusReference, Payload = new object[] { true, true } };
            var intValue = new NetMsg { Code = MsgCode.ReqFocusReference, Payload = new object[] { 1 } };
            var byteValue = new NetMsg { Code = MsgCode.ReqFocusReference, Payload = new object[] { (byte)1 } };
            var nullElement = new NetMsg { Code = MsgCode.ReqFocusReference, Payload = new object[] { null } };

            Assert.IsFalse(ReqFocusReferenceMsg.TryDecode(otherCode, out _));
            Assert.IsFalse(ReqFocusReferenceMsg.TryDecode(nullPayload, out _));
            Assert.IsFalse(ReqFocusReferenceMsg.TryDecode(emptyPayload, out _));
            Assert.IsFalse(ReqFocusReferenceMsg.TryDecode(extraElement, out _));
            Assert.IsFalse(ReqFocusReferenceMsg.TryDecode(intValue, out _));
            Assert.IsFalse(ReqFocusReferenceMsg.TryDecode(byteValue, out _));
            Assert.IsFalse(ReqFocusReferenceMsg.TryDecode(nullElement, out _));
        }

        // ---- ReferenceFocusChanged ----

        [Test]
        public void ReferenceFocusChanged_Encode_UsesItsCode_AndPunSerializableTypes()
        {
            NetMsg msg = ReferenceFocusChangedMsg.Encode(Good, true);

            Assert.AreEqual(MsgCode.ReferenceFocusChanged, msg.Code);
            Assert.AreEqual(2, msg.Payload.Length);
            Assert.IsInstanceOf<byte>(msg.Payload[0]);
            Assert.IsInstanceOf<bool>(msg.Payload[1]);
            AssertPunSerializable(msg.Payload);
        }

        [Test]
        public void ReferenceFocusChanged_RoundTrip_PreservesPlayerAndFlag()
        {
            Assert.IsTrue(ReferenceFocusChangedMsg.TryDecode(ReferenceFocusChangedMsg.Encode(Good, true), out PlayerId player, out bool focusing));
            Assert.AreEqual(Good, player);
            Assert.IsTrue(focusing);

            Assert.IsTrue(ReferenceFocusChangedMsg.TryDecode(ReferenceFocusChangedMsg.Encode(new PlayerId(3), false), out player, out focusing));
            Assert.AreEqual(new PlayerId(3), player);
            Assert.IsFalse(focusing);
        }

        [Test]
        public void ReferenceFocusChanged_TryDecode_RejectsBadMessages()
        {
            var otherCode = new NetMsg { Code = MsgCode.SpiritBumped, Payload = new object[] { (byte)1, true } };
            var nullPayload = new NetMsg { Code = MsgCode.ReferenceFocusChanged };
            var shortPayload = new NetMsg { Code = MsgCode.ReferenceFocusChanged, Payload = new object[] { (byte)1 } };
            var longPayload = new NetMsg { Code = MsgCode.ReferenceFocusChanged, Payload = new object[] { (byte)1, true, 0 } };
            var intPlayer = new NetMsg { Code = MsgCode.ReferenceFocusChanged, Payload = new object[] { 1, true } };
            var intFlag = new NetMsg { Code = MsgCode.ReferenceFocusChanged, Payload = new object[] { (byte)1, 1 } };
            var noPlayer = new NetMsg { Code = MsgCode.ReferenceFocusChanged, Payload = new object[] { PlayerId.None.Value, true } };

            Assert.IsFalse(ReferenceFocusChangedMsg.TryDecode(otherCode, out _, out _));
            Assert.IsFalse(ReferenceFocusChangedMsg.TryDecode(nullPayload, out _, out _));
            Assert.IsFalse(ReferenceFocusChangedMsg.TryDecode(shortPayload, out _, out _));
            Assert.IsFalse(ReferenceFocusChangedMsg.TryDecode(longPayload, out _, out _));
            Assert.IsFalse(ReferenceFocusChangedMsg.TryDecode(intPlayer, out _, out _));
            Assert.IsFalse(ReferenceFocusChangedMsg.TryDecode(intFlag, out _, out _));
            Assert.IsFalse(ReferenceFocusChangedMsg.TryDecode(noPlayer, out _, out _));
        }

        [Test]
        public void ReferenceFocusChanged_FailedDecode_LeavesNoPlayer()
        {
            var bad = new NetMsg { Code = MsgCode.ReferenceFocusChanged, Payload = new object[] { (byte)1, 5 } };

            Assert.IsFalse(ReferenceFocusChangedMsg.TryDecode(bad, out PlayerId player, out bool focusing));
            Assert.IsFalse(player.IsValid);
            Assert.IsFalse(focusing);
        }

        // ---- ReqSpiritBump ----

        [Test]
        public void ReqSpiritBump_Encode_UsesItsCode_AndPunSerializableTypes()
        {
            NetMsg msg = ReqSpiritBumpMsg.Encode(Good);

            Assert.AreEqual(MsgCode.ReqSpiritBump, msg.Code);
            Assert.AreEqual(1, msg.Payload.Length);
            Assert.IsInstanceOf<byte>(msg.Payload[0]);
            AssertPunSerializable(msg.Payload);
        }

        [Test]
        public void ReqSpiritBump_RoundTrip_PreservesTheGoodJinn()
        {
            Assert.IsTrue(ReqSpiritBumpMsg.TryDecode(ReqSpiritBumpMsg.Encode(Good), out PlayerId good));
            Assert.AreEqual(Good, good);
        }

        [Test]
        public void ReqSpiritBump_TryDecode_RejectsBadMessages()
        {
            var otherCode = new NetMsg { Code = MsgCode.ReqFocusReference, Payload = new object[] { (byte)1 } };
            var nullPayload = new NetMsg { Code = MsgCode.ReqSpiritBump };
            var emptyPayload = new NetMsg { Code = MsgCode.ReqSpiritBump, Payload = new object[0] };
            var extraElement = new NetMsg { Code = MsgCode.ReqSpiritBump, Payload = new object[] { (byte)1, (byte)2 } };
            var intPlayer = new NetMsg { Code = MsgCode.ReqSpiritBump, Payload = new object[] { 1 } };
            var noPlayer = new NetMsg { Code = MsgCode.ReqSpiritBump, Payload = new object[] { PlayerId.None.Value } };

            Assert.IsFalse(ReqSpiritBumpMsg.TryDecode(otherCode, out _));
            Assert.IsFalse(ReqSpiritBumpMsg.TryDecode(nullPayload, out _));
            Assert.IsFalse(ReqSpiritBumpMsg.TryDecode(emptyPayload, out _));
            Assert.IsFalse(ReqSpiritBumpMsg.TryDecode(extraElement, out _));
            Assert.IsFalse(ReqSpiritBumpMsg.TryDecode(intPlayer, out _));
            Assert.IsFalse(ReqSpiritBumpMsg.TryDecode(noPlayer, out _));
        }

        // ---- SpiritBumped ----

        [Test]
        public void SpiritBumped_Encode_UsesItsCode_AndPunSerializableTypes()
        {
            NetMsg msg = SpiritBumpedMsg.Encode(Evil, Good);

            Assert.AreEqual(MsgCode.SpiritBumped, msg.Code);
            Assert.AreEqual(2, msg.Payload.Length);
            Assert.IsInstanceOf<byte>(msg.Payload[0]);
            Assert.IsInstanceOf<byte>(msg.Payload[1]);
            AssertPunSerializable(msg.Payload);
        }

        [Test]
        public void SpiritBumped_RoundTrip_KeepsEvilAndGoodApart()
        {
            Assert.IsTrue(SpiritBumpedMsg.TryDecode(SpiritBumpedMsg.Encode(Evil, Good), out PlayerId evil, out PlayerId good));
            Assert.AreEqual(Evil, evil);
            Assert.AreEqual(Good, good);
        }

        [Test]
        public void SpiritBumped_TryDecode_RejectsBadMessages()
        {
            var otherCode = new NetMsg { Code = MsgCode.ReqSpiritBump, Payload = new object[] { (byte)2, (byte)1 } };
            var nullPayload = new NetMsg { Code = MsgCode.SpiritBumped };
            var shortPayload = new NetMsg { Code = MsgCode.SpiritBumped, Payload = new object[] { (byte)2 } };
            var longPayload = new NetMsg { Code = MsgCode.SpiritBumped, Payload = new object[] { (byte)2, (byte)1, (byte)0 } };
            var intEvil = new NetMsg { Code = MsgCode.SpiritBumped, Payload = new object[] { 2, (byte)1 } };
            var stringGood = new NetMsg { Code = MsgCode.SpiritBumped, Payload = new object[] { (byte)2, "P1" } };
            var noEvil = new NetMsg { Code = MsgCode.SpiritBumped, Payload = new object[] { PlayerId.None.Value, (byte)1 } };
            var noGood = new NetMsg { Code = MsgCode.SpiritBumped, Payload = new object[] { (byte)2, PlayerId.None.Value } };

            Assert.IsFalse(SpiritBumpedMsg.TryDecode(otherCode, out _, out _));
            Assert.IsFalse(SpiritBumpedMsg.TryDecode(nullPayload, out _, out _));
            Assert.IsFalse(SpiritBumpedMsg.TryDecode(shortPayload, out _, out _));
            Assert.IsFalse(SpiritBumpedMsg.TryDecode(longPayload, out _, out _));
            Assert.IsFalse(SpiritBumpedMsg.TryDecode(intEvil, out _, out _));
            Assert.IsFalse(SpiritBumpedMsg.TryDecode(stringGood, out _, out _));
            Assert.IsFalse(SpiritBumpedMsg.TryDecode(noEvil, out _, out _));
            Assert.IsFalse(SpiritBumpedMsg.TryDecode(noGood, out _, out _));
        }

        // ---- Wire format ----

        [Test]
        public void Codes_StayOnTheWire()
        {
            Assert.AreEqual(84, (byte)MsgCode.ReqFocusReference);
            Assert.AreEqual(85, (byte)MsgCode.ReferenceFocusChanged);
            Assert.AreEqual(86, (byte)MsgCode.ReqSpiritBump);
            Assert.AreEqual(87, (byte)MsgCode.SpiritBumped);
        }
    }
}
