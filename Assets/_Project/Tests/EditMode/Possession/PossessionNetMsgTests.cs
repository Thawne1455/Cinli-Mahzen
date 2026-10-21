using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using CinliMahzen.Possession;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.Possession
{
    /// <summary>
    /// Round trip and rejection cases for the possession codecs (Teknik §4.3, §6.2, TODO 1.7):
    /// ReqPossess, ReqUnpossess, PossessBegan, PossessCompleted, PossessEnded and PossessDenied.
    /// </summary>
    public class PossessionNetMsgTests
    {
        private static readonly PlayerId Evil = new PlayerId(2);
        private static readonly NetId Chair = new NetId(40001);

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

        private static NetMsg With(MsgCode code, params object[] payload)
        {
            return new NetMsg { Code = code, Payload = payload };
        }

        // ---- ReqPossess ----

        [Test]
        public void ReqPossess_RoundTrip()
        {
            NetMsg msg = ReqPossessMsg.Encode(Chair);
            Assert.AreEqual(MsgCode.ReqPossess, msg.Code);
            AssertPunSerializable(msg.Payload);

            Assert.IsTrue(ReqPossessMsg.TryDecode(msg, out NetId obj));
            Assert.AreEqual(Chair, obj);
        }

        [Test]
        public void ReqPossess_RejectsMalformed()
        {
            Assert.IsFalse(ReqPossessMsg.TryDecode(With(MsgCode.ReqUnpossess, Chair.Value), out _), "wrong code");
            Assert.IsFalse(ReqPossessMsg.TryDecode(With(MsgCode.ReqPossess), out _), "empty");
            Assert.IsFalse(ReqPossessMsg.TryDecode(new NetMsg { Code = MsgCode.ReqPossess }, out _), "null payload");
            Assert.IsFalse(ReqPossessMsg.TryDecode(With(MsgCode.ReqPossess, "x"), out _), "wrong type");
            Assert.IsFalse(ReqPossessMsg.TryDecode(With(MsgCode.ReqPossess, 0), out _), "invalid id");
            Assert.IsFalse(ReqPossessMsg.TryDecode(With(MsgCode.ReqPossess, 1, 2), out _), "too long");
        }

        // ---- ReqUnpossess ----

        [Test]
        public void ReqUnpossess_RoundTrip_AndRejectsMalformed()
        {
            NetMsg msg = ReqUnpossessMsg.Encode();
            Assert.AreEqual(MsgCode.ReqUnpossess, msg.Code);
            AssertPunSerializable(msg.Payload);
            Assert.IsTrue(ReqUnpossessMsg.TryDecode(msg));
            Assert.IsTrue(ReqUnpossessMsg.TryDecode(new NetMsg { Code = MsgCode.ReqUnpossess }), "null payload is empty");

            Assert.IsFalse(ReqUnpossessMsg.TryDecode(With(MsgCode.ReqPossess)), "wrong code");
            Assert.IsFalse(ReqUnpossessMsg.TryDecode(With(MsgCode.ReqUnpossess, 1)), "not empty");
        }

        // ---- PossessBegan ----

        [Test]
        public void PossessBegan_RoundTrip()
        {
            NetMsg msg = PossessBeganMsg.Encode(Evil, Chair, 12.5d);
            Assert.AreEqual(MsgCode.PossessBegan, msg.Code);
            AssertPunSerializable(msg.Payload);

            Assert.IsTrue(PossessBeganMsg.TryDecode(msg, out PlayerId player, out NetId obj, out double end));
            Assert.AreEqual(Evil, player);
            Assert.AreEqual(Chair, obj);
            Assert.AreEqual(12.5d, end);
        }

        [Test]
        public void PossessBegan_RejectsMalformed()
        {
            Assert.IsFalse(PossessBeganMsg.TryDecode(With(MsgCode.PossessCompleted, (byte)2, 5, 1d), out _, out _, out _), "wrong code");
            Assert.IsFalse(PossessBeganMsg.TryDecode(With(MsgCode.PossessBegan, (byte)2, 5), out _, out _, out _), "short");
            Assert.IsFalse(PossessBeganMsg.TryDecode(With(MsgCode.PossessBegan, 2, 5, 1d), out _, out _, out _), "player not a byte");
            Assert.IsFalse(PossessBeganMsg.TryDecode(With(MsgCode.PossessBegan, (byte)2, 5, 1f), out _, out _, out _), "time not a double");
            Assert.IsFalse(PossessBeganMsg.TryDecode(With(MsgCode.PossessBegan, PlayerId.None.Value, 5, 1d), out _, out _, out _), "no player");
            Assert.IsFalse(PossessBeganMsg.TryDecode(With(MsgCode.PossessBegan, (byte)2, 0, 1d), out _, out _, out _), "no object");
        }

        // ---- PossessCompleted ----

        [Test]
        public void PossessCompleted_RoundTrip_AndRejectsMalformed()
        {
            NetMsg msg = PossessCompletedMsg.Encode(Evil, Chair);
            Assert.AreEqual(MsgCode.PossessCompleted, msg.Code);
            AssertPunSerializable(msg.Payload);
            Assert.IsTrue(PossessCompletedMsg.TryDecode(msg, out PlayerId player, out NetId obj));
            Assert.AreEqual(Evil, player);
            Assert.AreEqual(Chair, obj);

            Assert.IsFalse(PossessCompletedMsg.TryDecode(With(MsgCode.PossessBegan, (byte)2, 5), out _, out _), "wrong code");
            Assert.IsFalse(PossessCompletedMsg.TryDecode(With(MsgCode.PossessCompleted, (byte)2), out _, out _), "short");
            Assert.IsFalse(PossessCompletedMsg.TryDecode(With(MsgCode.PossessCompleted, (byte)2, 0), out _, out _), "no object");
        }

        // ---- PossessEnded ----

        [Test]
        public void PossessEnded_RoundTrip_EveryReason()
        {
            foreach (PossessEndReason reason in System.Enum.GetValues(typeof(PossessEndReason)))
            {
                NetMsg msg = PossessEndedMsg.Encode(Evil, Chair, reason, reason == PossessEndReason.Spent);
                Assert.AreEqual(MsgCode.PossessEnded, msg.Code);
                AssertPunSerializable(msg.Payload);
                Assert.IsTrue(PossessEndedMsg.TryDecode(msg, out PlayerId player, out NetId obj, out PossessEndReason decoded, out bool spent), reason.ToString());
                Assert.AreEqual(Evil, player);
                Assert.AreEqual(Chair, obj);
                Assert.AreEqual(reason, decoded);
                Assert.AreEqual(reason == PossessEndReason.Spent, spent);
            }
        }

        [Test]
        public void PossessEnded_RejectsMalformed()
        {
            Assert.IsFalse(PossessEndedMsg.TryDecode(With(MsgCode.PossessEnded, (byte)2, 5, (byte)0), out _, out _, out _, out _), "short");
            Assert.IsFalse(PossessEndedMsg.TryDecode(With(MsgCode.PossessEnded, (byte)2, 5, (byte)200, false), out _, out _, out _, out _), "unknown reason");
            Assert.IsFalse(PossessEndedMsg.TryDecode(With(MsgCode.PossessEnded, (byte)2, 5, (byte)0, 1), out _, out _, out _, out _), "spent not a bool");
            Assert.IsFalse(PossessEndedMsg.TryDecode(With(MsgCode.PossessEnded, (byte)2, 0, (byte)0, false), out _, out _, out _, out _), "no object");
        }

        [Test]
        public void PossessEndReason_ValuesAreStable()
        {
            Assert.AreEqual(0, (int)PossessEndReason.Voluntary);
            Assert.AreEqual(1, (int)PossessEndReason.Exorcised);
            Assert.AreEqual(2, (int)PossessEndReason.RoomLit);
            Assert.AreEqual(3, (int)PossessEndReason.Spent);
            Assert.AreEqual(4, (int)PossessEndReason.Forced);
            Assert.AreEqual(5, (int)PossessEndReason.Kicked);
        }

        // ---- PossessDenied ----

        [Test]
        public void PossessDenied_RoundTrip_EveryReason()
        {
            foreach (PossessDenyReason reason in System.Enum.GetValues(typeof(PossessDenyReason)))
            {
                if (reason == PossessDenyReason.None)
                    continue;
                NetMsg msg = PossessDeniedMsg.Encode(Evil, Chair, reason);
                AssertPunSerializable(msg.Payload);
                Assert.IsTrue(PossessDeniedMsg.TryDecode(msg, out PlayerId player, out NetId obj, out PossessDenyReason decoded), reason.ToString());
                Assert.AreEqual(Evil, player);
                Assert.AreEqual(Chair, obj);
                Assert.AreEqual(reason, decoded);
            }
        }

        [Test]
        public void PossessDenied_RejectsNoneUnknownAndMalformed()
        {
            Assert.IsFalse(PossessDeniedMsg.TryDecode(PossessDeniedMsg.Encode(Evil, Chair, PossessDenyReason.None), out _, out _, out _), "None is not a denial");
            Assert.IsFalse(PossessDeniedMsg.TryDecode(With(MsgCode.PossessDenied, (byte)2, 5, (byte)8), out _, out _, out _), "retired value 8");
            Assert.IsFalse(PossessDeniedMsg.TryDecode(With(MsgCode.PossessDenied, (byte)2, 5), out _, out _, out _), "short");
            Assert.IsTrue(PossessDeniedMsg.TryDecode(With(MsgCode.PossessDenied, (byte)2, 0, (byte)PossessDenyReason.InvalidTarget), out _, out NetId obj, out _),
                "a request without a valid object is still answered");
            Assert.AreEqual(NetId.Invalid, obj);
        }
    }
}
