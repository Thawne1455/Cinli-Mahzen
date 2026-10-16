using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.Player
{
    /// <summary>Round trip and rejection cases for the StatusApplied codec (lives in CM.Core, used by the pawns).</summary>
    public class StatusAppliedMsgTests
    {
        [Test]
        public void Encode_UsesTheStatusAppliedCode_AndPunSerializableTypes()
        {
            NetMsg msg = StatusAppliedMsg.Encode(new NetId(3), StatusType.CameraSpin180, 0.15f, new PlayerId(2));

            Assert.AreEqual(MsgCode.StatusApplied, msg.Code);
            Assert.AreEqual(4, msg.Payload.Length);
            Assert.IsInstanceOf<int>(msg.Payload[0]);
            Assert.IsInstanceOf<byte>(msg.Payload[1]);
            Assert.IsInstanceOf<float>(msg.Payload[2]);
            Assert.IsInstanceOf<byte>(msg.Payload[3]);
        }

        [Test]
        public void RoundTrip_PreservesEveryField()
        {
            NetMsg msg = StatusAppliedMsg.Encode(new NetId(3), StatusType.CameraSpin180, 0.15f, new PlayerId(2));

            Assert.IsTrue(StatusAppliedMsg.TryDecode(msg, out NetId target, out StatusType status, out float duration, out PlayerId source));
            Assert.AreEqual(new NetId(3), target);
            Assert.AreEqual(StatusType.CameraSpin180, status);
            Assert.AreEqual(0.15f, duration);
            Assert.AreEqual(new PlayerId(2), source);
        }

        [Test]
        public void RoundTrip_NoSource()
        {
            NetMsg msg = StatusAppliedMsg.Encode(new NetId(1), StatusType.Knockdown, 2f, PlayerId.None);

            Assert.IsTrue(StatusAppliedMsg.TryDecode(msg, out _, out StatusType status, out float duration, out PlayerId source));
            Assert.AreEqual(StatusType.Knockdown, status);
            Assert.AreEqual(2f, duration);
            Assert.IsFalse(source.IsValid);
        }

        [Test]
        public void TryDecode_RejectsAnotherCode()
        {
            NetMsg msg = StatusAppliedMsg.Encode(new NetId(3), StatusType.Stunned, 1f, new PlayerId(0));
            msg.Code = MsgCode.HumanDamaged;

            Assert.IsFalse(StatusAppliedMsg.TryDecode(msg, out _, out _, out _, out _));
        }

        [Test]
        public void TryDecode_RejectsNullOrShortPayload()
        {
            var empty = new NetMsg { Code = MsgCode.StatusApplied };
            var shortMsg = new NetMsg { Code = MsgCode.StatusApplied, Payload = new object[] { 3, (byte)7, 0.15f } };

            Assert.IsFalse(StatusAppliedMsg.TryDecode(empty, out _, out _, out _, out _));
            Assert.IsFalse(StatusAppliedMsg.TryDecode(shortMsg, out _, out _, out _, out _));
        }

        [Test]
        public void TryDecode_RejectsWrongElementTypes()
        {
            var wrongNetId = new NetMsg { Code = MsgCode.StatusApplied, Payload = new object[] { (byte)3, (byte)7, 0.15f, (byte)0 } };
            var wrongStatus = new NetMsg { Code = MsgCode.StatusApplied, Payload = new object[] { 3, 7, 0.15f, (byte)0 } };
            var wrongDuration = new NetMsg { Code = MsgCode.StatusApplied, Payload = new object[] { 3, (byte)7, 0.15d, (byte)0 } };
            var wrongSource = new NetMsg { Code = MsgCode.StatusApplied, Payload = new object[] { 3, (byte)7, 0.15f, "P0" } };

            Assert.IsFalse(StatusAppliedMsg.TryDecode(wrongNetId, out _, out _, out _, out _));
            Assert.IsFalse(StatusAppliedMsg.TryDecode(wrongStatus, out _, out _, out _, out _));
            Assert.IsFalse(StatusAppliedMsg.TryDecode(wrongDuration, out _, out _, out _, out _));
            Assert.IsFalse(StatusAppliedMsg.TryDecode(wrongSource, out _, out _, out _, out _));
        }

        [Test]
        public void TryDecode_RejectsNegativeOrNaNDuration()
        {
            NetMsg negative = StatusAppliedMsg.Encode(new NetId(3), StatusType.Stunned, -1f, new PlayerId(0));
            NetMsg nan = StatusAppliedMsg.Encode(new NetId(3), StatusType.Stunned, float.NaN, new PlayerId(0));

            Assert.IsFalse(StatusAppliedMsg.TryDecode(negative, out _, out _, out _, out _));
            Assert.IsFalse(StatusAppliedMsg.TryDecode(nan, out _, out _, out _, out _));
        }

        [Test]
        public void TryDecode_IgnoresExtraTrailingElements()
        {
            var msg = new NetMsg
            {
                Code = MsgCode.StatusApplied,
                Payload = new object[] { 3, (byte)7, 0.15f, (byte)1, "future field" },
            };

            Assert.IsTrue(StatusAppliedMsg.TryDecode(msg, out NetId target, out StatusType status, out _, out _));
            Assert.AreEqual(new NetId(3), target);
            Assert.AreEqual(StatusType.CameraSpin180, status);
        }

        [Test]
        public void StatusAppliedCode_StaysOnTheWire()
        {
            Assert.AreEqual(37, (byte)MsgCode.StatusApplied);
        }
    }
}
