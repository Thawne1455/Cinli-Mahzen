using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.Player
{
    /// <summary>Round trip and rejection cases for the ReqInteract / InteractResult codecs (Teknik §4.5).</summary>
    public class InteractMsgTests
    {
        [Test]
        public void ReqInteract_Encode_UsesItsCode_AndPunSerializableTypes()
        {
            NetMsg msg = ReqInteractMsg.Encode(new NetId(1001));

            Assert.AreEqual(MsgCode.ReqInteract, msg.Code);
            Assert.AreEqual(1, msg.Payload.Length);
            Assert.IsInstanceOf<int>(msg.Payload[0]);
        }

        [Test]
        public void ReqInteract_RoundTrip_PreservesTheTarget()
        {
            NetMsg msg = ReqInteractMsg.Encode(new NetId(1001));

            Assert.IsTrue(ReqInteractMsg.TryDecode(msg, out NetId target));
            Assert.AreEqual(new NetId(1001), target);
        }

        [Test]
        public void ReqInteract_TryDecode_RejectsWrongCodeNullWrongLengthAndWrongType()
        {
            var otherCode = new NetMsg { Code = MsgCode.InteractResult, Payload = new object[] { 1001 } };
            var nullPayload = new NetMsg { Code = MsgCode.ReqInteract };
            var emptyPayload = new NetMsg { Code = MsgCode.ReqInteract, Payload = new object[0] };
            var extraElement = new NetMsg { Code = MsgCode.ReqInteract, Payload = new object[] { 1001, 1 } };
            var longId = new NetMsg { Code = MsgCode.ReqInteract, Payload = new object[] { 1001L } };
            var stringId = new NetMsg { Code = MsgCode.ReqInteract, Payload = new object[] { "1001" } };

            Assert.IsFalse(ReqInteractMsg.TryDecode(otherCode, out _));
            Assert.IsFalse(ReqInteractMsg.TryDecode(nullPayload, out _));
            Assert.IsFalse(ReqInteractMsg.TryDecode(emptyPayload, out _));
            Assert.IsFalse(ReqInteractMsg.TryDecode(extraElement, out _));
            Assert.IsFalse(ReqInteractMsg.TryDecode(longId, out _));
            Assert.IsFalse(ReqInteractMsg.TryDecode(stringId, out _));
        }

        [Test]
        public void InteractResult_Encode_UsesItsCode_AndPunSerializableTypes()
        {
            NetMsg msg = InteractResultMsg.Encode(new NetId(1001), new PlayerId(2), true);

            Assert.AreEqual(MsgCode.InteractResult, msg.Code);
            Assert.AreEqual(3, msg.Payload.Length);
            Assert.IsInstanceOf<int>(msg.Payload[0]);
            Assert.IsInstanceOf<byte>(msg.Payload[1]);
            Assert.IsInstanceOf<bool>(msg.Payload[2]);
        }

        [Test]
        public void InteractResult_RoundTrip_PreservesEveryField_ForOkAndRejected()
        {
            NetMsg ok = InteractResultMsg.Encode(new NetId(1001), new PlayerId(2), true);
            NetMsg rejected = InteractResultMsg.Encode(new NetId(1002), new PlayerId(0), false);

            Assert.IsTrue(InteractResultMsg.TryDecode(ok, out NetId okTarget, out PlayerId okPlayer, out bool okFlag));
            Assert.AreEqual(new NetId(1001), okTarget);
            Assert.AreEqual(new PlayerId(2), okPlayer);
            Assert.IsTrue(okFlag);

            Assert.IsTrue(InteractResultMsg.TryDecode(rejected, out NetId rejectedTarget, out PlayerId rejectedPlayer, out bool rejectedFlag));
            Assert.AreEqual(new NetId(1002), rejectedTarget);
            Assert.AreEqual(new PlayerId(0), rejectedPlayer);
            Assert.IsFalse(rejectedFlag);
        }

        [Test]
        public void InteractResult_TryDecode_RejectsWrongCodeNullWrongLengthAndWrongTypes()
        {
            var otherCode = new NetMsg { Code = MsgCode.ReqInteract, Payload = new object[] { 1001, (byte)2, true } };
            var nullPayload = new NetMsg { Code = MsgCode.InteractResult };
            var shortPayload = new NetMsg { Code = MsgCode.InteractResult, Payload = new object[] { 1001, (byte)2 } };
            var intPlayer = new NetMsg { Code = MsgCode.InteractResult, Payload = new object[] { 1001, 2, true } };
            var intOk = new NetMsg { Code = MsgCode.InteractResult, Payload = new object[] { 1001, (byte)2, 1 } };
            var longTarget = new NetMsg { Code = MsgCode.InteractResult, Payload = new object[] { 1001L, (byte)2, true } };

            Assert.IsFalse(InteractResultMsg.TryDecode(otherCode, out _, out _, out _));
            Assert.IsFalse(InteractResultMsg.TryDecode(nullPayload, out _, out _, out _));
            Assert.IsFalse(InteractResultMsg.TryDecode(shortPayload, out _, out _, out _));
            Assert.IsFalse(InteractResultMsg.TryDecode(intPlayer, out _, out _, out _));
            Assert.IsFalse(InteractResultMsg.TryDecode(intOk, out _, out _, out _));
            Assert.IsFalse(InteractResultMsg.TryDecode(longTarget, out _, out _, out _));
        }

        [Test]
        public void InteractCodes_StayOnTheWire()
        {
            Assert.AreEqual(30, (byte)MsgCode.ReqInteract);
            Assert.AreEqual(31, (byte)MsgCode.InteractResult);
        }
    }
}
