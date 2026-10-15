using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using CinliMahzen.Player;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.Player
{
    public class HumanNetMsgTests
    {
        private static DamageInfo FullInfo()
        {
            return new DamageInfo
            {
                Amount = 2,
                Attacker = new PlayerId(3),
                SourceObject = new NetId(1234),
                CauseId = "shelf.topple",
                Flags = DamageFlags.Knockdown | DamageFlags.IgnoreInvuln,
                KnockdownTime = 2.5f,
                Point = new Vector3(1.25f, -2f, 3.5f),
                Direction = new Vector3(0f, 0.5f, -1f),
            };
        }

        private static void AssertInfoEqual(DamageInfo expected, DamageInfo actual)
        {
            Assert.AreEqual(expected.Amount, actual.Amount, "Amount");
            Assert.AreEqual(expected.Attacker, actual.Attacker, "Attacker");
            Assert.AreEqual(expected.SourceObject, actual.SourceObject, "SourceObject");
            Assert.AreEqual(expected.CauseId, actual.CauseId, "CauseId");
            Assert.AreEqual(expected.Flags, actual.Flags, "Flags");
            Assert.AreEqual(expected.KnockdownTime, actual.KnockdownTime, "KnockdownTime");
            Assert.AreEqual(expected.Point, actual.Point, "Point");
            Assert.AreEqual(expected.Direction, actual.Direction, "Direction");
        }

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

        private static NetMsg With(NetMsg m, MsgCode code)
        {
            m.Code = code;
            return m;
        }

        private static NetMsg WithPayload(NetMsg m, object[] payload)
        {
            m.Payload = payload;
            return m;
        }

        private static NetMsg WithElement(NetMsg m, int index, object value)
        {
            var copy = (object[])m.Payload.Clone();
            copy[index] = value;
            m.Payload = copy;
            return m;
        }

        // ---------------------------------------------------------------- DamageInfoCodec

        [Test]
        public void DamageInfoCodec_RoundTrip_AllFields_AtOffset()
        {
            var payload = new object[DamageInfoCodec.FieldCount + 2];
            DamageInfoCodec.Write(FullInfo(), payload, 2);

            Assert.IsTrue(DamageInfoCodec.TryRead(payload, 2, out DamageInfo decoded));
            AssertInfoEqual(FullInfo(), decoded);
        }

        [Test]
        public void DamageInfoCodec_NullCauseId_IsSentAsEmptyString()
        {
            DamageInfo info = FullInfo();
            info.CauseId = null;
            var payload = new object[DamageInfoCodec.FieldCount];

            DamageInfoCodec.Write(info, payload, 0);

            Assert.IsNotNull(payload[3], "PUN must never see a null slot");
            Assert.IsTrue(DamageInfoCodec.TryRead(payload, 0, out DamageInfo decoded));
            Assert.AreEqual(string.Empty, decoded.CauseId);
        }

        [Test]
        public void DamageInfoCodec_SentinelIds_RoundTrip()
        {
            DamageInfo info = FullInfo();
            info.Attacker = PlayerId.None;
            info.SourceObject = NetId.Invalid;
            info.Flags = DamageFlags.None;
            var payload = new object[DamageInfoCodec.FieldCount];

            DamageInfoCodec.Write(info, payload, 0);

            Assert.IsTrue(DamageInfoCodec.TryRead(payload, 0, out DamageInfo decoded));
            Assert.AreEqual(PlayerId.None, decoded.Attacker);
            Assert.IsFalse(decoded.SourceObject.IsValid);
            Assert.AreEqual(DamageFlags.None, decoded.Flags);
        }

        [Test]
        public void DamageInfoCodec_EveryFlagCombination_RoundTrips()
        {
            for (int bits = 0; bits < 16; bits++)
            {
                DamageInfo info = FullInfo();
                info.Flags = (DamageFlags)bits;
                var payload = new object[DamageInfoCodec.FieldCount];

                DamageInfoCodec.Write(info, payload, 0);

                Assert.IsTrue(DamageInfoCodec.TryRead(payload, 0, out DamageInfo decoded));
                Assert.AreEqual((DamageFlags)bits, decoded.Flags);
            }
        }

        [Test]
        public void DamageInfoCodec_TryRead_RejectsBadInput_WithoutThrowing()
        {
            var good = new object[DamageInfoCodec.FieldCount];
            DamageInfoCodec.Write(FullInfo(), good, 0);

            Assert.IsFalse(DamageInfoCodec.TryRead(null, 0, out _));
            Assert.IsFalse(DamageInfoCodec.TryRead(good, -1, out _));
            Assert.IsFalse(DamageInfoCodec.TryRead(good, 1, out _), "range does not fit");
            Assert.IsFalse(DamageInfoCodec.TryRead(good, int.MaxValue, out _));
            Assert.IsFalse(DamageInfoCodec.TryRead(new object[0], 0, out _));

            for (int i = 0; i < DamageInfoCodec.FieldCount; i++)
            {
                var nulled = (object[])good.Clone();
                nulled[i] = null;
                Assert.IsFalse(DamageInfoCodec.TryRead(nulled, 0, out _), "null at " + i);

                var wrongType = (object[])good.Clone();
                wrongType[i] = new object();
                Assert.IsFalse(DamageInfoCodec.TryRead(wrongType, 0, out _), "wrong type at " + i);
            }
        }

        // ---------------------------------------------------------------- HumanDamagedMsg

        [Test]
        public void HumanDamaged_Encode_UsesCode_AndPunSerializablePayload()
        {
            NetMsg m = HumanDamagedMsg.Encode(FullInfo(), 1);

            Assert.AreEqual(MsgCode.HumanDamaged, m.Code);
            Assert.AreEqual(HumanDamagedMsg.PayloadLength, m.Payload.Length);
            AssertPunSerializable(m.Payload);
        }

        [Test]
        public void HumanDamaged_RoundTrip_AllFields()
        {
            NetMsg m = HumanDamagedMsg.Encode(FullInfo(), 1);

            Assert.IsTrue(HumanDamagedMsg.TryDecode(m, out DamageInfo info, out int hpAfter));
            AssertInfoEqual(FullInfo(), info);
            Assert.AreEqual(1, hpAfter);
        }

        [Test]
        public void HumanDamaged_RoundTrip_ZeroHp_AndDefaultInfo()
        {
            NetMsg m = HumanDamagedMsg.Encode(default(DamageInfo), 0);

            Assert.IsTrue(HumanDamagedMsg.TryDecode(m, out DamageInfo info, out int hpAfter));
            Assert.AreEqual(0, hpAfter);
            Assert.AreEqual(0, info.Amount);
            Assert.AreEqual(string.Empty, info.CauseId, "null cause is normalised to empty");
        }

        [Test]
        public void HumanDamaged_Decode_IgnoresBridgeFilledFields()
        {
            NetMsg m = HumanDamagedMsg.Encode(FullInfo(), 2);
            m.Sender = new PlayerId(1);
            m.SentTime = 123.5d;

            Assert.IsTrue(HumanDamagedMsg.TryDecode(m, out DamageInfo info, out int hpAfter));
            AssertInfoEqual(FullInfo(), info);
            Assert.AreEqual(2, hpAfter);
        }

        [Test]
        public void HumanDamaged_Decode_RejectsMalformed()
        {
            NetMsg good = HumanDamagedMsg.Encode(FullInfo(), 1);

            AssertDamagedRejected(With(good, MsgCode.HumanFainted), "wrong code");
            AssertDamagedRejected(With(good, MsgCode.HumanRevived), "wrong code");
            AssertDamagedRejected(default(NetMsg), "default message");
            AssertDamagedRejected(WithPayload(good, null), "null payload");
            AssertDamagedRejected(WithPayload(good, new object[0]), "empty payload");
            AssertDamagedRejected(WithPayload(good, new object[HumanDamagedMsg.PayloadLength - 1]), "short payload");
            AssertDamagedRejected(WithPayload(good, new object[HumanDamagedMsg.PayloadLength + 1]), "long payload");
            AssertDamagedRejected(WithElement(good, 0, "1"), "hp as string");
            AssertDamagedRejected(WithElement(good, 0, 1f), "hp as float");
            AssertDamagedRejected(WithElement(good, 0, null), "hp null");
            AssertDamagedRejected(WithElement(good, 4, 1), "cause id as int");
            AssertDamagedRejected(WithElement(good, 7, 0f), "point as float");
        }

        private static void AssertDamagedRejected(NetMsg m, string why)
        {
            Assert.IsFalse(HumanDamagedMsg.TryDecode(m, out DamageInfo info, out int hpAfter), why);
            Assert.AreEqual(0, hpAfter, why);
            Assert.AreEqual(0, info.Amount, why);
        }

        // ---------------------------------------------------------------- HumanFaintedMsg

        [Test]
        public void HumanFainted_Encode_UsesCode_AndPunSerializablePayload()
        {
            NetMsg m = HumanFaintedMsg.Encode(FullInfo(), 2);

            Assert.AreEqual(MsgCode.HumanFainted, m.Code);
            Assert.AreEqual(HumanFaintedMsg.PayloadLength, m.Payload.Length);
            AssertPunSerializable(m.Payload);
        }

        [Test]
        public void HumanFainted_RoundTrip_AllFields()
        {
            NetMsg m = HumanFaintedMsg.Encode(FullInfo(), 3);

            Assert.IsTrue(HumanFaintedMsg.TryDecode(m, out DamageInfo info, out int faintCount));
            AssertInfoEqual(FullInfo(), info);
            Assert.AreEqual(3, faintCount);
        }

        [Test]
        public void HumanFainted_Decode_RejectsMalformed()
        {
            NetMsg good = HumanFaintedMsg.Encode(FullInfo(), 1);

            AssertFaintedRejected(With(good, MsgCode.HumanDamaged), "wrong code");
            AssertFaintedRejected(default(NetMsg), "default message");
            AssertFaintedRejected(WithPayload(good, null), "null payload");
            AssertFaintedRejected(WithPayload(good, new object[0]), "empty payload");
            AssertFaintedRejected(WithPayload(good, new object[HumanFaintedMsg.PayloadLength - 1]), "short payload");
            AssertFaintedRejected(WithPayload(good, new object[HumanFaintedMsg.PayloadLength + 1]), "long payload");
            AssertFaintedRejected(WithElement(good, 0, (byte)1), "count as byte");
            AssertFaintedRejected(WithElement(good, 0, null), "count null");
            AssertFaintedRejected(WithElement(good, 2, (short)1), "attacker as short");
            AssertFaintedRejected(WithElement(good, 6, 1.5), "knockdown time as double");
        }

        private static void AssertFaintedRejected(NetMsg m, string why)
        {
            Assert.IsFalse(HumanFaintedMsg.TryDecode(m, out DamageInfo info, out int faintCount), why);
            Assert.AreEqual(0, faintCount, why);
            Assert.AreEqual(0, info.Amount, why);
        }

        [Test]
        public void HumanDamaged_And_HumanFainted_AreNotInterchangeable()
        {
            NetMsg damaged = HumanDamagedMsg.Encode(FullInfo(), 1);
            NetMsg fainted = HumanFaintedMsg.Encode(FullInfo(), 1);

            Assert.IsFalse(HumanFaintedMsg.TryDecode(damaged, out _, out _));
            Assert.IsFalse(HumanDamagedMsg.TryDecode(fainted, out _, out _));
        }

        // ---------------------------------------------------------------- HumanRevivedMsg

        [TestCase(false, 3)]
        [TestCase(true, 3)]
        [TestCase(true, 1)]
        public void HumanRevived_RoundTrip(bool byCologne, int hp)
        {
            NetMsg m = HumanRevivedMsg.Encode(byCologne, hp);

            Assert.AreEqual(MsgCode.HumanRevived, m.Code);
            Assert.AreEqual(HumanRevivedMsg.PayloadLength, m.Payload.Length);
            AssertPunSerializable(m.Payload);
            Assert.IsTrue(HumanRevivedMsg.TryDecode(m, out bool decodedCologne, out int decodedHp));
            Assert.AreEqual(byCologne, decodedCologne);
            Assert.AreEqual(hp, decodedHp);
        }

        [Test]
        public void HumanRevived_Decode_IgnoresBridgeFilledFields()
        {
            NetMsg m = HumanRevivedMsg.Encode(true, 3);
            m.Sender = new PlayerId(0);
            m.SentTime = 9.75d;

            Assert.IsTrue(HumanRevivedMsg.TryDecode(m, out bool byCologne, out int hp));
            Assert.IsTrue(byCologne);
            Assert.AreEqual(3, hp);
        }

        [Test]
        public void HumanRevived_Decode_RejectsMalformed()
        {
            NetMsg good = HumanRevivedMsg.Encode(true, 3);

            AssertRevivedRejected(With(good, MsgCode.HumanFainted), "wrong code");
            AssertRevivedRejected(default(NetMsg), "default message");
            AssertRevivedRejected(WithPayload(good, null), "null payload");
            AssertRevivedRejected(WithPayload(good, new object[0]), "empty payload");
            AssertRevivedRejected(WithPayload(good, new object[] { true }), "short payload");
            AssertRevivedRejected(WithPayload(good, new object[] { true, 3, 0 }), "long payload");
            AssertRevivedRejected(WithElement(good, 0, 1), "flag as int");
            AssertRevivedRejected(WithElement(good, 0, null), "flag null");
            AssertRevivedRejected(WithElement(good, 1, "3"), "hp as string");
            AssertRevivedRejected(WithElement(good, 1, (byte)3), "hp as byte");
        }

        private static void AssertRevivedRejected(NetMsg m, string why)
        {
            Assert.IsFalse(HumanRevivedMsg.TryDecode(m, out bool byCologne, out int hp), why);
            Assert.IsFalse(byCologne, why);
            Assert.AreEqual(0, hp, why);
        }
    }
}
