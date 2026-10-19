using System;
using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using CinliMahzen.Objectives;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.Objectives
{
    /// <summary>
    /// Round trips and rejection cases for the seven puzzle codecs (Teknik §7.3, TODO 2.4): ReqPuzzleOp, PuzzleStateChanged,
    /// ReqPuzzleScramble, PuzzleScrambled, ReqPuzzleConfirm, PuzzleConfirmResult, PuzzleCompleted. TryDecode must never throw;
    /// a malformed message just returns false, so an exception in these tests is a failure.
    /// </summary>
    public class PuzzleNetMsgTests
    {
        private static readonly int[] Values = { 2, 0, 3 };

        // ---------------------------------------------------------------- helpers

        /// <summary>One value of every kind that is not the type of <paramref name="original"/> (null included).</summary>
        private static IEnumerable<object> WrongTypesFor(object original)
        {
            object[] candidates = { null, "junk", 1L, 1.5f, 1.5d, true, (byte)1, 1, new[] { 1 } };
            foreach (object candidate in candidates)
            {
                if (candidate == null || candidate.GetType() != original.GetType())
                {
                    yield return candidate;
                }
            }
        }

        /// <summary>
        /// The malformed-message matrix every decoder must reject: wrong code, null / empty / short / long payload and a wrong
        /// type at every element. <paramref name="good"/> must decode.
        /// </summary>
        private static void AssertRejectsMalformed(NetMsg good, Func<NetMsg, bool> decode)
        {
            Assert.IsTrue(decode(good), "the untouched message must decode");

            NetMsg otherCode = good;
            otherCode.Code = MsgCode.DebugCommand;
            Assert.IsFalse(decode(otherCode), "wrong code");

            NetMsg nullPayload = good;
            nullPayload.Payload = null;
            Assert.IsFalse(decode(nullPayload), "null payload");

            NetMsg empty = good;
            empty.Payload = new object[0];
            Assert.IsFalse(decode(empty), "empty payload");

            int length = good.Payload.Length;
            NetMsg shortPayload = good;
            shortPayload.Payload = new object[length - 1];
            Array.Copy(good.Payload, shortPayload.Payload, length - 1);
            Assert.IsFalse(decode(shortPayload), "one element missing");

            NetMsg longPayload = good;
            longPayload.Payload = new object[length + 1];
            Array.Copy(good.Payload, longPayload.Payload, length);
            longPayload.Payload[length] = 1;
            Assert.IsFalse(decode(longPayload), "one element too many");

            for (int i = 0; i < length; i++)
            {
                foreach (object junk in WrongTypesFor(good.Payload[i]))
                {
                    NetMsg wrong = good;
                    wrong.Payload = (object[])good.Payload.Clone();
                    wrong.Payload[i] = junk;
                    Assert.IsFalse(decode(wrong), "element " + i + " = " + (junk ?? "null") + " (" + (junk == null ? "null" : junk.GetType().Name) + ")");
                }
            }
        }

        private static NetMsg WithElement(NetMsg msg, int index, object value)
        {
            msg.Payload = (object[])msg.Payload.Clone();
            msg.Payload[index] = value;
            return msg;
        }

        // ---------------------------------------------------------------- wire codes

        [Test]
        public void PuzzleCodes_StayOnTheWire()
        {
            Assert.AreEqual(101, (byte)MsgCode.ReqPuzzleOp);
            Assert.AreEqual(102, (byte)MsgCode.PuzzleStateChanged);
            Assert.AreEqual(103, (byte)MsgCode.ReqPuzzleScramble);
            Assert.AreEqual(104, (byte)MsgCode.PuzzleScrambled);
            Assert.AreEqual(105, (byte)MsgCode.ReqPuzzleConfirm);
            Assert.AreEqual(106, (byte)MsgCode.PuzzleConfirmResult);
            Assert.AreEqual(107, (byte)MsgCode.PuzzleCompleted);
        }

        // ---------------------------------------------------------------- ReqPuzzleOp

        [Test]
        public void ReqPuzzleOp_Encode_UsesItsCode_AndPunSerializableTypes()
        {
            NetMsg msg = ReqPuzzleOpMsg.Encode(4, PuzzleOp.Cycle(1, -1));

            Assert.AreEqual(MsgCode.ReqPuzzleOp, msg.Code);
            Assert.AreEqual(5, msg.Payload.Length);
            Assert.IsInstanceOf<int>(msg.Payload[0]);
            Assert.IsInstanceOf<byte>(msg.Payload[1]);
            Assert.IsInstanceOf<int>(msg.Payload[2]);
            Assert.IsInstanceOf<int>(msg.Payload[3]);
            Assert.IsInstanceOf<int>(msg.Payload[4]);
        }

        [Test]
        public void ReqPuzzleOp_RoundTrip_PreservesEveryOpKind()
        {
            AssertOpRoundTrip(0, PuzzleOp.Cycle(2, 1));
            AssertOpRoundTrip(3, PuzzleOp.Cycle(0, -1));
            AssertOpRoundTrip(1, PuzzleOp.Set(5, 1));
            AssertOpRoundTrip(7, PuzzleOp.Swap(1, 4));
        }

        private static void AssertOpRoundTrip(int slot, PuzzleOp op)
        {
            NetMsg msg = ReqPuzzleOpMsg.Encode(slot, op);

            Assert.IsTrue(ReqPuzzleOpMsg.TryDecode(msg, out int decodedSlot, out PuzzleOp decoded), op.ToString());
            Assert.AreEqual(slot, decodedSlot);
            Assert.AreEqual(op.Kind, decoded.Kind);
            Assert.AreEqual(op.Part, decoded.Part);
            Assert.AreEqual(op.Value, decoded.Value);
            Assert.AreEqual(op.PartB, decoded.PartB);
        }

        [Test]
        public void ReqPuzzleOp_TryDecode_RejectsMalformedMessages()
        {
            AssertRejectsMalformed(ReqPuzzleOpMsg.Encode(2, PuzzleOp.Set(1, 1)), m => ReqPuzzleOpMsg.TryDecode(m, out _, out _));
        }

        [Test]
        public void ReqPuzzleOp_TryDecode_RejectsEveryKindThatIsNotExactlyOneKnownFlag()
        {
            NetMsg good = ReqPuzzleOpMsg.Encode(2, PuzzleOp.Cycle(1, 1));

            foreach (byte kind in new byte[] { 0, 3, 5, 6, 7, 8, 16, 255 })
            {
                NetMsg bad = WithElement(good, 1, kind);
                Assert.IsFalse(ReqPuzzleOpMsg.TryDecode(bad, out _, out _), "kind " + kind);
            }
        }

        [Test]
        public void ReqPuzzleOp_TryDecode_RejectsNegativeSlot_AndLeavesOutputsEmpty()
        {
            NetMsg bad = WithElement(ReqPuzzleOpMsg.Encode(2, PuzzleOp.Cycle(1, 1)), 0, -1);

            Assert.IsFalse(ReqPuzzleOpMsg.TryDecode(bad, out int slot, out PuzzleOp op));
            Assert.AreEqual(0, slot);
            Assert.AreEqual(PuzzleOpKind.None, op.Kind);
        }

        [Test]
        public void ReqPuzzleOp_UnusedFieldsComeBackAsZero()
        {
            // A sender that stuffs junk into the fields a kind does not use must not make the op carry it.
            NetMsg cycle = WithElement(ReqPuzzleOpMsg.Encode(0, PuzzleOp.Cycle(1, 1)), 4, 9);
            NetMsg swap = WithElement(ReqPuzzleOpMsg.Encode(0, PuzzleOp.Swap(1, 2)), 3, 9);

            Assert.IsTrue(ReqPuzzleOpMsg.TryDecode(cycle, out _, out PuzzleOp decodedCycle));
            Assert.AreEqual(0, decodedCycle.PartB);
            Assert.IsTrue(ReqPuzzleOpMsg.TryDecode(swap, out _, out PuzzleOp decodedSwap));
            Assert.AreEqual(0, decodedSwap.Value);
        }

        // ---------------------------------------------------------------- PuzzleStateChanged

        [Test]
        public void PuzzleStateChanged_Encode_UsesItsCode_AndPunSerializableTypes()
        {
            NetMsg msg = PuzzleStateChangedMsg.Encode(1, Values, new PlayerId(0));

            Assert.AreEqual(MsgCode.PuzzleStateChanged, msg.Code);
            Assert.AreEqual(3, msg.Payload.Length);
            Assert.IsInstanceOf<int>(msg.Payload[0]);
            Assert.IsInstanceOf<int[]>(msg.Payload[1]);
            Assert.IsInstanceOf<byte>(msg.Payload[2]);
        }

        [Test]
        public void PuzzleStateChanged_RoundTrip_PreservesEveryField()
        {
            NetMsg msg = PuzzleStateChangedMsg.Encode(6, Values, new PlayerId(3));

            Assert.IsTrue(PuzzleStateChangedMsg.TryDecode(msg, out int slot, out int[] values, out PlayerId by));
            Assert.AreEqual(6, slot);
            CollectionAssert.AreEqual(Values, values);
            Assert.AreEqual(new PlayerId(3), by);
        }

        [Test]
        public void PuzzleStateChanged_CopiesTheValueArray_OnEncodeAndOnDecode()
        {
            var source = new[] { 1, 2, 3 };
            NetMsg msg = PuzzleStateChangedMsg.Encode(0, source, new PlayerId(0));

            source[0] = 99;
            PuzzleStateChangedMsg.TryDecode(msg, out _, out int[] first, out _);
            Assert.AreEqual(1, first[0], "encode must not alias the caller's array");

            first[1] = 99;
            PuzzleStateChangedMsg.TryDecode(msg, out _, out int[] second, out _);
            Assert.AreEqual(2, second[1], "decode must not alias the message's array");
            Assert.AreNotSame(first, second);
        }

        [Test]
        public void PuzzleStateChanged_TryDecode_RejectsMalformedMessages()
        {
            AssertRejectsMalformed(PuzzleStateChangedMsg.Encode(2, Values, new PlayerId(0)), m => PuzzleStateChangedMsg.TryDecode(m, out _, out _, out _));
        }

        [Test]
        public void PuzzleStateChanged_TryDecode_RejectsEmptyValuesAndNegativeSlot()
        {
            NetMsg good = PuzzleStateChangedMsg.Encode(2, Values, new PlayerId(0));

            Assert.IsFalse(PuzzleStateChangedMsg.TryDecode(WithElement(good, 1, new int[0]), out _, out int[] values, out _));
            Assert.IsNull(values);
            Assert.IsFalse(PuzzleStateChangedMsg.TryDecode(WithElement(good, 0, -1), out _, out _, out _));
        }

        [Test]
        public void PuzzleStateChanged_Encode_RejectsNullValues()
        {
            Assert.Throws<ArgumentNullException>(() => PuzzleStateChangedMsg.Encode(0, null, new PlayerId(0)));
        }

        // ---------------------------------------------------------------- ReqPuzzleScramble / ReqPuzzleConfirm

        [Test]
        public void ReqPuzzleScramble_RoundTrip_AndPunTypes()
        {
            NetMsg msg = ReqPuzzleScrambleMsg.Encode(5);

            Assert.AreEqual(MsgCode.ReqPuzzleScramble, msg.Code);
            Assert.AreEqual(1, msg.Payload.Length);
            Assert.IsInstanceOf<int>(msg.Payload[0]);
            Assert.IsTrue(ReqPuzzleScrambleMsg.TryDecode(msg, out int slot));
            Assert.AreEqual(5, slot);
        }

        [Test]
        public void ReqPuzzleScramble_TryDecode_RejectsMalformedMessages_AndNegativeSlot()
        {
            AssertRejectsMalformed(ReqPuzzleScrambleMsg.Encode(2), m => ReqPuzzleScrambleMsg.TryDecode(m, out _));
            Assert.IsFalse(ReqPuzzleScrambleMsg.TryDecode(ReqPuzzleScrambleMsg.Encode(-1), out _));
        }

        [Test]
        public void ReqPuzzleConfirm_RoundTrip_AndPunTypes()
        {
            NetMsg msg = ReqPuzzleConfirmMsg.Encode(5);

            Assert.AreEqual(MsgCode.ReqPuzzleConfirm, msg.Code);
            Assert.AreEqual(1, msg.Payload.Length);
            Assert.IsInstanceOf<int>(msg.Payload[0]);
            Assert.IsTrue(ReqPuzzleConfirmMsg.TryDecode(msg, out int slot));
            Assert.AreEqual(5, slot);
        }

        [Test]
        public void ReqPuzzleConfirm_TryDecode_RejectsMalformedMessages_AndNegativeSlot()
        {
            AssertRejectsMalformed(ReqPuzzleConfirmMsg.Encode(2), m => ReqPuzzleConfirmMsg.TryDecode(m, out _));
            Assert.IsFalse(ReqPuzzleConfirmMsg.TryDecode(ReqPuzzleConfirmMsg.Encode(-1), out _));
        }

        [Test]
        public void RequestCodecs_DoNotDecodeEachOthersMessages()
        {
            Assert.IsFalse(ReqPuzzleConfirmMsg.TryDecode(ReqPuzzleScrambleMsg.Encode(1), out _));
            Assert.IsFalse(ReqPuzzleScrambleMsg.TryDecode(ReqPuzzleConfirmMsg.Encode(1), out _));
        }

        // ---------------------------------------------------------------- PuzzleScrambled

        [Test]
        public void PuzzleScrambled_Encode_UsesItsCode_AndPunSerializableTypes()
        {
            NetMsg msg = PuzzleScrambledMsg.Encode(1, Values, new PlayerId(2), 123.5d);

            Assert.AreEqual(MsgCode.PuzzleScrambled, msg.Code);
            Assert.AreEqual(4, msg.Payload.Length);
            Assert.IsInstanceOf<int>(msg.Payload[0]);
            Assert.IsInstanceOf<int[]>(msg.Payload[1]);
            Assert.IsInstanceOf<byte>(msg.Payload[2]);
            Assert.IsInstanceOf<double>(msg.Payload[3]);
        }

        [Test]
        public void PuzzleScrambled_RoundTrip_PreservesEveryField()
        {
            NetMsg msg = PuzzleScrambledMsg.Encode(3, Values, new PlayerId(2), 123.456d);

            Assert.IsTrue(PuzzleScrambledMsg.TryDecode(msg, out int slot, out int[] values, out PlayerId by, out double cooldownUntil));
            Assert.AreEqual(3, slot);
            CollectionAssert.AreEqual(Values, values);
            Assert.AreEqual(new PlayerId(2), by);
            Assert.AreEqual(123.456d, cooldownUntil);
        }

        [Test]
        public void PuzzleScrambled_CopiesTheValueArray_OnEncodeAndOnDecode()
        {
            var source = new[] { 1, 2, 3 };
            NetMsg msg = PuzzleScrambledMsg.Encode(0, source, new PlayerId(2), 10d);

            source[0] = 99;
            PuzzleScrambledMsg.TryDecode(msg, out _, out int[] first, out _, out _);
            Assert.AreEqual(1, first[0]);

            first[1] = 99;
            PuzzleScrambledMsg.TryDecode(msg, out _, out int[] second, out _, out _);
            Assert.AreEqual(2, second[1]);
        }

        [Test]
        public void PuzzleScrambled_TryDecode_RejectsMalformedMessages()
        {
            AssertRejectsMalformed(PuzzleScrambledMsg.Encode(2, Values, new PlayerId(2), 10d), m => PuzzleScrambledMsg.TryDecode(m, out _, out _, out _, out _));
        }

        [Test]
        public void PuzzleScrambled_TryDecode_RejectsBadValuesSlotAndCooldown()
        {
            NetMsg good = PuzzleScrambledMsg.Encode(2, Values, new PlayerId(2), 10d);

            Assert.IsFalse(PuzzleScrambledMsg.TryDecode(WithElement(good, 1, new int[0]), out _, out _, out _, out _));
            Assert.IsFalse(PuzzleScrambledMsg.TryDecode(WithElement(good, 0, -1), out _, out _, out _, out _));
            Assert.IsFalse(PuzzleScrambledMsg.TryDecode(WithElement(good, 3, double.NaN), out _, out _, out _, out _));
            Assert.IsFalse(PuzzleScrambledMsg.TryDecode(WithElement(good, 3, double.PositiveInfinity), out _, out _, out _, out _));
            Assert.IsFalse(PuzzleScrambledMsg.TryDecode(WithElement(good, 3, double.NegativeInfinity), out _, out _, out _, out _));
        }

        [Test]
        public void PuzzleScrambled_Encode_RejectsNullValues()
        {
            Assert.Throws<ArgumentNullException>(() => PuzzleScrambledMsg.Encode(0, null, new PlayerId(0), 1d));
        }

        // ---------------------------------------------------------------- PuzzleConfirmResult

        [Test]
        public void PuzzleConfirmResult_Encode_UsesItsCode_AndPunSerializableTypes()
        {
            NetMsg msg = PuzzleConfirmResultMsg.Encode(1, true, new PlayerId(0));

            Assert.AreEqual(MsgCode.PuzzleConfirmResult, msg.Code);
            Assert.AreEqual(3, msg.Payload.Length);
            Assert.IsInstanceOf<int>(msg.Payload[0]);
            Assert.IsInstanceOf<bool>(msg.Payload[1]);
            Assert.IsInstanceOf<byte>(msg.Payload[2]);
        }

        [Test]
        public void PuzzleConfirmResult_RoundTrip_PreservesEveryField_ForOkAndFail()
        {
            NetMsg ok = PuzzleConfirmResultMsg.Encode(4, true, new PlayerId(0));
            NetMsg fail = PuzzleConfirmResultMsg.Encode(5, false, new PlayerId(1));

            Assert.IsTrue(PuzzleConfirmResultMsg.TryDecode(ok, out int okSlot, out bool okFlag, out PlayerId okBy));
            Assert.AreEqual(4, okSlot);
            Assert.IsTrue(okFlag);
            Assert.AreEqual(new PlayerId(0), okBy);

            Assert.IsTrue(PuzzleConfirmResultMsg.TryDecode(fail, out int failSlot, out bool failFlag, out PlayerId failBy));
            Assert.AreEqual(5, failSlot);
            Assert.IsFalse(failFlag);
            Assert.AreEqual(new PlayerId(1), failBy);
        }

        [Test]
        public void PuzzleConfirmResult_TryDecode_RejectsMalformedMessages_AndNegativeSlot()
        {
            NetMsg good = PuzzleConfirmResultMsg.Encode(2, true, new PlayerId(0));

            AssertRejectsMalformed(good, m => PuzzleConfirmResultMsg.TryDecode(m, out _, out _, out _));
            Assert.IsFalse(PuzzleConfirmResultMsg.TryDecode(WithElement(good, 0, -1), out _, out _, out _));
        }

        // ---------------------------------------------------------------- PuzzleCompleted

        [Test]
        public void PuzzleCompleted_RoundTrip_AndPunTypes()
        {
            NetMsg msg = PuzzleCompletedMsg.Encode(3, 2);

            Assert.AreEqual(MsgCode.PuzzleCompleted, msg.Code);
            Assert.AreEqual(2, msg.Payload.Length);
            Assert.IsInstanceOf<int>(msg.Payload[0]);
            Assert.IsInstanceOf<int>(msg.Payload[1]);
            Assert.IsTrue(PuzzleCompletedMsg.TryDecode(msg, out int slot, out int stage));
            Assert.AreEqual(3, slot);
            Assert.AreEqual(2, stage);
        }

        [Test]
        public void PuzzleCompleted_TryDecode_RejectsMalformedMessages_AndNegativeSlotOrStage()
        {
            NetMsg good = PuzzleCompletedMsg.Encode(3, 2);

            AssertRejectsMalformed(good, m => PuzzleCompletedMsg.TryDecode(m, out _, out _));
            Assert.IsFalse(PuzzleCompletedMsg.TryDecode(WithElement(good, 0, -1), out _, out _));
            Assert.IsFalse(PuzzleCompletedMsg.TryDecode(WithElement(good, 1, -1), out _, out _));
            Assert.IsTrue(PuzzleCompletedMsg.TryDecode(PuzzleCompletedMsg.Encode(0, 0), out _, out _), "slot 0 / stage 0 (garden) is valid");
        }
    }
}
