using System;
using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using CinliMahzen.Match;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.Match
{
    /// <summary>
    /// Round trips and rejection cases for the four match codecs (Teknik §4.3, §9): MatchStateChanged, RoundSetup,
    /// RoundEnded, RoundTimeAdded. TryDecode must never throw; a malformed message just returns false, so an exception
    /// in these tests is a failure.
    /// </summary>
    public class MatchNetMsgTests
    {
        /// <summary>Values of a wrong type for every element of every match payload (no element is null, string, long or bool).</summary>
        private static readonly object[] WrongTypeJunk = { null, "junk", 1L, true };

        // ---------------------------------------------------------------- helpers

        private static Role[] FourPlayerRoles()
        {
            return new[] { Role.Human, Role.GoodJinn, Role.EvilJinn, Role.EvilJinn };
        }

        private static NetMsg WithCode(NetMsg m, MsgCode code)
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
            return WithPayload(m, copy);
        }

        private static NetMsg WithAppended(NetMsg m, object extra)
        {
            var copy = new object[m.Payload.Length + 1];
            Array.Copy(m.Payload, copy, m.Payload.Length);
            copy[copy.Length - 1] = extra;
            return WithPayload(m, copy);
        }

        private static object[] Prefix(object[] payload, int count)
        {
            var copy = new object[count];
            Array.Copy(payload, copy, count);
            return copy;
        }

        /// <summary>Teknik §4.3: payload may only hold PUN-serializable types.</summary>
        private static void AssertPunSerializable(object[] payload)
        {
            Assert.IsNotNull(payload);
            for (int i = 0; i < payload.Length; i++)
            {
                object o = payload[i];
                bool ok = o is byte || o is bool || o is short || o is int || o is long || o is float || o is double
                          || o is string || o is byte[] || o is int[] || o is float[] || o is object[];
                Assert.IsTrue(ok, "payload[" + i + "] has non-serializable type " + (o == null ? "null" : o.GetType().ToString()));
            }
        }

        /// <summary>Every element replaced by a wrong-typed value must make the decode fail.</summary>
        private static void AssertEveryElementRejectsWrongType(NetMsg valid, Func<NetMsg, bool> decode)
        {
            for (int i = 0; i < valid.Payload.Length; i++)
            {
                foreach (object junk in WrongTypeJunk)
                {
                    Assert.IsFalse(decode(WithElement(valid, i, junk)), "element " + i + " = " + (junk ?? "null"));
                }
            }
        }

        private static bool DecodeMatchState(NetMsg m)
        {
            return MatchStateChangedMsg.TryDecode(m, out _, out _, out _);
        }

        private static bool DecodeRoundEnded(NetMsg m)
        {
            return RoundEndedMsg.TryDecode(m, out _, out _, out _);
        }

        private static bool DecodeRoundSetup(NetMsg m)
        {
            return RoundSetupMsg.TryDecode(m, out _, out _);
        }

        private static bool DecodeRoundTimeAdded(NetMsg m)
        {
            return RoundTimeAddedMsg.TryDecode(m, out _, out _);
        }

        private static void AssertMatchStateRoundTrip(MatchState state, int round, double end)
        {
            NetMsg msg = MatchStateChangedMsg.Encode(state, round, end);

            Assert.IsTrue(MatchStateChangedMsg.TryDecode(msg, out MatchState s, out int r, out double e), state + " " + round + " " + end);
            Assert.AreEqual(state, s);
            Assert.AreEqual(round, r);
            Assert.AreEqual(end, e);
        }

        private static void AssertRoundEndedRoundTrip(RoundResult result, RoundEndReason reason, int round)
        {
            NetMsg msg = RoundEndedMsg.Encode(result, reason, round);

            Assert.IsTrue(RoundEndedMsg.TryDecode(msg, out RoundResult r, out RoundEndReason why, out int index), result + " / " + reason);
            Assert.AreEqual(result, r);
            Assert.AreEqual(reason, why);
            Assert.AreEqual(round, index);
        }

        private static void AssertRoundTimeAddedRoundTrip(float bonus, double end)
        {
            NetMsg msg = RoundTimeAddedMsg.Encode(bonus, end);

            Assert.IsTrue(RoundTimeAddedMsg.TryDecode(msg, out float decodedBonus, out double decodedEnd), bonus + " / " + end);
            Assert.AreEqual(bonus, decodedBonus);
            Assert.AreEqual(end, decodedEnd);
        }

        // ---------------------------------------------------------------- MatchStateChanged

        [Test]
        public void MatchStateChanged_Encode_UsesItsCode_AndPunSerializableTypes()
        {
            NetMsg msg = MatchStateChangedMsg.Encode(MatchState.Playing, 2, 368d);

            Assert.AreEqual(MsgCode.MatchStateChanged, msg.Code);
            Assert.AreEqual(3, msg.Payload.Length);
            Assert.IsInstanceOf<byte>(msg.Payload[0]);
            Assert.IsInstanceOf<int>(msg.Payload[1]);
            Assert.IsInstanceOf<double>(msg.Payload[2]);
            AssertPunSerializable(msg.Payload);
        }

        [Test]
        public void MatchStateChanged_RoundTrip_EveryState()
        {
            foreach (MatchState state in Enum.GetValues(typeof(MatchState)))
            {
                AssertMatchStateRoundTrip(state, 2, 368d);
            }
        }

        [Test]
        public void MatchStateChanged_RoundTrip_BoundaryValues()
        {
            AssertMatchStateRoundTrip(MatchState.Lobby, 0, 0d);
            AssertMatchStateRoundTrip(MatchState.RoleReveal, 0, double.MaxValue);      // no-timer state, "no end" value
            AssertMatchStateRoundTrip(MatchState.MatchEnd, int.MaxValue, double.MaxValue);
            AssertMatchStateRoundTrip(MatchState.Playing, 1, 1234.5d);
        }

        [Test]
        public void MatchStateChanged_EncodeFromTransition_CarriesTheTargetStateRoundAndEnd()
        {
            var transition = new MatchTransition(MatchState.Intro, MatchState.Playing, 2, 368d);

            NetMsg msg = MatchStateChangedMsg.Encode(transition);

            Assert.IsTrue(MatchStateChangedMsg.TryDecode(msg, out MatchState state, out int round, out double end));
            Assert.AreEqual(MatchState.Playing, state, "the To state, not From");
            Assert.AreEqual(2, round);
            Assert.AreEqual(368d, end);
        }

        [Test]
        public void MatchStateChanged_TryDecode_RejectsAnotherCode()
        {
            NetMsg valid = MatchStateChangedMsg.Encode(MatchState.Playing, 2, 368d);

            Assert.IsFalse(DecodeMatchState(WithCode(valid, MsgCode.RoundSetup)));
            Assert.IsFalse(DecodeMatchState(WithCode(valid, MsgCode.RoundEnded)));
        }

        [Test]
        public void MatchStateChanged_TryDecode_RejectsNullEmptyAndShortPayload()
        {
            NetMsg valid = MatchStateChangedMsg.Encode(MatchState.Playing, 2, 368d);

            Assert.IsFalse(DecodeMatchState(WithPayload(valid, null)));
            Assert.IsFalse(DecodeMatchState(WithPayload(valid, new object[0])));
            for (int n = 1; n < valid.Payload.Length; n++)
            {
                Assert.IsFalse(DecodeMatchState(WithPayload(valid, Prefix(valid.Payload, n))), "first " + n + " elements");
            }
        }

        [Test]
        public void MatchStateChanged_TryDecode_IsLenientAboutExtraTrailingElements()
        {
            NetMsg longer = WithAppended(MatchStateChangedMsg.Encode(MatchState.Playing, 2, 368d), 99);

            Assert.IsTrue(MatchStateChangedMsg.TryDecode(longer, out MatchState state, out int round, out double end));
            Assert.AreEqual(MatchState.Playing, state);
            Assert.AreEqual(2, round);
            Assert.AreEqual(368d, end);
        }

        [Test]
        public void MatchStateChanged_TryDecode_RejectsWrongElementTypes()
        {
            NetMsg valid = MatchStateChangedMsg.Encode(MatchState.Playing, 2, 368d);

            Assert.IsFalse(DecodeMatchState(WithElement(valid, 0, (int)MatchState.Playing)), "state as int");
            Assert.IsFalse(DecodeMatchState(WithElement(valid, 1, 2L)), "round as long");
            Assert.IsFalse(DecodeMatchState(WithElement(valid, 2, 368f)), "end as float");
            AssertEveryElementRejectsWrongType(valid, DecodeMatchState);
        }

        [Test]
        public void MatchStateChanged_TryDecode_RejectsUndeclaredStateNegativeRoundAndNonFiniteTime()
        {
            NetMsg valid = MatchStateChangedMsg.Encode(MatchState.Playing, 2, 368d);

            Assert.IsFalse(DecodeMatchState(WithElement(valid, 0, (byte)7)), "state 7 is not declared");
            Assert.IsFalse(DecodeMatchState(WithElement(valid, 0, (byte)255)));
            Assert.IsFalse(DecodeMatchState(WithElement(valid, 1, -1)), "negative round");
            Assert.IsFalse(DecodeMatchState(WithElement(valid, 2, double.NaN)));
            Assert.IsFalse(DecodeMatchState(WithElement(valid, 2, double.PositiveInfinity)));
            Assert.IsFalse(DecodeMatchState(WithElement(valid, 2, double.NegativeInfinity)));
        }

        // ---------------------------------------------------------------- RoundSetup

        [Test]
        public void RoundSetup_Encode_UsesItsCode_AndPayloadShape()
        {
            NetMsg msg = RoundSetupMsg.Encode(1234, FourPlayerRoles());

            Assert.AreEqual(MsgCode.RoundSetup, msg.Code);
            Assert.AreEqual(2, msg.Payload.Length);
            Assert.IsInstanceOf<int>(msg.Payload[0]);
            Assert.IsInstanceOf<byte[]>(msg.Payload[1]);
            Assert.AreEqual(4, ((byte[])msg.Payload[1]).Length);
            AssertPunSerializable(msg.Payload);
        }

        [Test]
        public void RoundSetup_RoundTrip_SeedAndFourRoles()
        {
            Role[] roles = FourPlayerRoles();
            NetMsg msg = RoundSetupMsg.Encode(1234, roles);

            Assert.IsTrue(RoundSetupMsg.TryDecode(msg, out int seed, out Role[] decoded));
            Assert.AreEqual(1234, seed);
            CollectionAssert.AreEqual(roles, decoded);
        }

        [Test]
        public void RoundSetup_RoundTrip_SeedBoundaries()
        {
            int[] seeds = { 0, -1, 1, 1234567890, int.MaxValue, int.MinValue };

            foreach (int seed in seeds)
            {
                NetMsg msg = RoundSetupMsg.Encode(seed, FourPlayerRoles());

                Assert.IsTrue(RoundSetupMsg.TryDecode(msg, out int decodedSeed, out _), "seed " + seed);
                Assert.AreEqual(seed, decodedSeed);
            }
        }

        [Test]
        public void RoundSetup_RoundTrip_RotationRolesOfEveryRound()
        {
            for (int round = 0; round < RoleRotation.DefaultPlayerCount; round++)
            {
                Role[] roles = RoleRotation.CreateRoles(round);
                NetMsg msg = RoundSetupMsg.Encode(round, roles);

                Assert.IsTrue(RoundSetupMsg.TryDecode(msg, out _, out Role[] decoded), "round " + round);
                CollectionAssert.AreEqual(roles, decoded, "round " + round);
            }
        }

        [Test]
        public void RoundSetup_EmptyRoleList_IsEncodedButRejectedOnDecode()
        {
            // A round without any player is malformed, so the empty list does not round trip (RoundSetupMsg.TryDecode).
            NetMsg msg = RoundSetupMsg.Encode(7, new Role[0]);

            Assert.AreEqual(0, ((byte[])msg.Payload[1]).Length);
            Assert.IsFalse(DecodeRoundSetup(msg));
        }

        [Test]
        public void RoundSetup_Encode_CopiesTheRolesAtEncodeTime()
        {
            Role[] roles = FourPlayerRoles();
            NetMsg msg = RoundSetupMsg.Encode(7, roles);

            roles[0] = Role.EvilJinn;

            Assert.IsTrue(RoundSetupMsg.TryDecode(msg, out _, out Role[] decoded));
            Assert.AreEqual(Role.Human, decoded[0]);
        }

        [Test]
        public void RoundSetup_TryDecode_ReturnsAFreshArrayEachTime()
        {
            NetMsg msg = RoundSetupMsg.Encode(7, FourPlayerRoles());

            Assert.IsTrue(RoundSetupMsg.TryDecode(msg, out _, out Role[] first));
            first[0] = Role.EvilJinn;

            Assert.IsTrue(RoundSetupMsg.TryDecode(msg, out _, out Role[] second));
            Assert.AreEqual(Role.Human, second[0]);
        }

        [Test]
        public void RoundSetup_Encode_NullRoles_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => RoundSetupMsg.Encode(7, null));
        }

        [Test]
        public void RoundSetup_TryDecode_RejectsAnotherCode()
        {
            NetMsg valid = RoundSetupMsg.Encode(7, FourPlayerRoles());

            Assert.IsFalse(DecodeRoundSetup(WithCode(valid, MsgCode.MatchStateChanged)));
            Assert.IsFalse(DecodeRoundSetup(WithCode(valid, MsgCode.RoundTimeAdded)));
        }

        [Test]
        public void RoundSetup_TryDecode_RejectsNullEmptyAndShortPayload()
        {
            NetMsg valid = RoundSetupMsg.Encode(7, FourPlayerRoles());

            Assert.IsFalse(DecodeRoundSetup(WithPayload(valid, null)));
            Assert.IsFalse(DecodeRoundSetup(WithPayload(valid, new object[0])));
            Assert.IsFalse(DecodeRoundSetup(WithPayload(valid, Prefix(valid.Payload, 1))), "seed without roles");
        }

        [Test]
        public void RoundSetup_TryDecode_IsLenientAboutExtraTrailingElements()
        {
            NetMsg longer = WithAppended(RoundSetupMsg.Encode(7, FourPlayerRoles()), 99);

            Assert.IsTrue(RoundSetupMsg.TryDecode(longer, out int seed, out Role[] roles));
            Assert.AreEqual(7, seed);
            CollectionAssert.AreEqual(FourPlayerRoles(), roles);
        }

        [Test]
        public void RoundSetup_TryDecode_RejectsWrongElementTypes()
        {
            NetMsg valid = RoundSetupMsg.Encode(7, FourPlayerRoles());

            Assert.IsFalse(DecodeRoundSetup(WithElement(valid, 0, 7L)), "seed as long");
            Assert.IsFalse(DecodeRoundSetup(WithElement(valid, 1, new int[] { 1, 2, 3, 3 })), "roles as int[]");
            Assert.IsFalse(DecodeRoundSetup(WithElement(valid, 1, new object[] { (byte)1 })), "roles as object[]");
            Assert.IsFalse(DecodeRoundSetup(WithElement(valid, 1, "P1")), "roles as string");
            AssertEveryElementRejectsWrongType(valid, DecodeRoundSetup);
        }

        [Test]
        public void RoundSetup_TryDecode_RejectsUndeclaredRoleValues()
        {
            NetMsg valid = RoundSetupMsg.Encode(7, FourPlayerRoles());

            Assert.IsFalse(DecodeRoundSetup(WithElement(valid, 1, new byte[] { 1, 2, 4, 3 })), "role 4 is not declared");
            Assert.IsFalse(DecodeRoundSetup(WithElement(valid, 1, new byte[] { 1, 2, 255, 3 })));
        }

        // ---------------------------------------------------------------- RoundEnded

        [Test]
        public void RoundEnded_Encode_UsesItsCode_AndPayloadShape()
        {
            NetMsg msg = RoundEndedMsg.Encode(RoundResult.JinnsWin, RoundEndReason.Faint, 1);

            Assert.AreEqual(MsgCode.RoundEnded, msg.Code);
            Assert.AreEqual(3, msg.Payload.Length);
            Assert.IsInstanceOf<byte>(msg.Payload[0]);
            Assert.IsInstanceOf<byte>(msg.Payload[1]);
            Assert.IsInstanceOf<int>(msg.Payload[2]);
            AssertPunSerializable(msg.Payload);
        }

        [Test]
        public void RoundEnded_RoundTrip_EveryResultAndReasonCombination()
        {
            foreach (RoundResult result in Enum.GetValues(typeof(RoundResult)))
            {
                foreach (RoundEndReason reason in Enum.GetValues(typeof(RoundEndReason)))
                {
                    AssertRoundEndedRoundTrip(result, reason, 3);
                }
            }
        }

        [Test]
        public void RoundEnded_RoundTrip_RoundIndexBoundaries()
        {
            AssertRoundEndedRoundTrip(RoundResult.SeekersWin, RoundEndReason.TreasureDug, 0);
            AssertRoundEndedRoundTrip(RoundResult.JinnsWin, RoundEndReason.Timeout, int.MaxValue);
        }

        [Test]
        public void RoundEnded_TryDecode_RejectsAnotherCode()
        {
            NetMsg valid = RoundEndedMsg.Encode(RoundResult.JinnsWin, RoundEndReason.Timeout, 0);

            Assert.IsFalse(DecodeRoundEnded(WithCode(valid, MsgCode.MatchStateChanged)));
            Assert.IsFalse(DecodeRoundEnded(WithCode(valid, MsgCode.RoundTimeAdded)));
        }

        [Test]
        public void RoundEnded_TryDecode_RejectsNullEmptyAndShortPayload()
        {
            NetMsg valid = RoundEndedMsg.Encode(RoundResult.JinnsWin, RoundEndReason.Timeout, 0);

            Assert.IsFalse(DecodeRoundEnded(WithPayload(valid, null)));
            Assert.IsFalse(DecodeRoundEnded(WithPayload(valid, new object[0])));
            for (int n = 1; n < valid.Payload.Length; n++)
            {
                Assert.IsFalse(DecodeRoundEnded(WithPayload(valid, Prefix(valid.Payload, n))), "first " + n + " elements");
            }
        }

        [Test]
        public void RoundEnded_TryDecode_IsLenientAboutExtraTrailingElements()
        {
            NetMsg longer = WithAppended(RoundEndedMsg.Encode(RoundResult.SeekersWin, RoundEndReason.TreasureDug, 2), 99);

            Assert.IsTrue(RoundEndedMsg.TryDecode(longer, out RoundResult result, out RoundEndReason reason, out int round));
            Assert.AreEqual(RoundResult.SeekersWin, result);
            Assert.AreEqual(RoundEndReason.TreasureDug, reason);
            Assert.AreEqual(2, round);
        }

        [Test]
        public void RoundEnded_TryDecode_RejectsWrongElementTypes()
        {
            NetMsg valid = RoundEndedMsg.Encode(RoundResult.JinnsWin, RoundEndReason.Timeout, 0);

            Assert.IsFalse(DecodeRoundEnded(WithElement(valid, 0, (int)RoundResult.JinnsWin)), "result as int");
            Assert.IsFalse(DecodeRoundEnded(WithElement(valid, 1, (int)RoundEndReason.Timeout)), "reason as int");
            Assert.IsFalse(DecodeRoundEnded(WithElement(valid, 2, 0L)), "round as long");
            AssertEveryElementRejectsWrongType(valid, DecodeRoundEnded);
        }

        [Test]
        public void RoundEnded_TryDecode_RejectsUndeclaredResultReasonAndNegativeRound()
        {
            NetMsg valid = RoundEndedMsg.Encode(RoundResult.JinnsWin, RoundEndReason.Timeout, 0);

            Assert.IsFalse(DecodeRoundEnded(WithElement(valid, 0, (byte)3)), "result 3 is not declared");
            Assert.IsFalse(DecodeRoundEnded(WithElement(valid, 0, (byte)255)));
            Assert.IsFalse(DecodeRoundEnded(WithElement(valid, 1, (byte)4)), "reason 4 is not declared");
            Assert.IsFalse(DecodeRoundEnded(WithElement(valid, 1, (byte)255)));
            Assert.IsFalse(DecodeRoundEnded(WithElement(valid, 2, -1)), "negative round");
        }

        // ---------------------------------------------------------------- RoundTimeAdded

        [Test]
        public void RoundTimeAdded_Encode_UsesItsCode_AndPayloadShape()
        {
            NetMsg msg = RoundTimeAddedMsg.Encode(90f, 458d);

            Assert.AreEqual(MsgCode.RoundTimeAdded, msg.Code);
            Assert.AreEqual(2, msg.Payload.Length);
            Assert.IsInstanceOf<float>(msg.Payload[0]);
            Assert.IsInstanceOf<double>(msg.Payload[1]);
            AssertPunSerializable(msg.Payload);
        }

        [Test]
        public void RoundTimeAdded_RoundTrip_Values()
        {
            AssertRoundTimeAddedRoundTrip(90f, 458d);
            AssertRoundTimeAddedRoundTrip(0f, 0d);
            AssertRoundTimeAddedRoundTrip(90f, double.MaxValue);     // finite, so accepted
        }

        [Test]
        public void RoundTimeAdded_TryDecode_RejectsAnotherCode()
        {
            NetMsg valid = RoundTimeAddedMsg.Encode(90f, 458d);

            Assert.IsFalse(DecodeRoundTimeAdded(WithCode(valid, MsgCode.MatchStateChanged)));
            Assert.IsFalse(DecodeRoundTimeAdded(WithCode(valid, MsgCode.RoundEnded)));
        }

        [Test]
        public void RoundTimeAdded_TryDecode_RejectsNullEmptyAndShortPayload()
        {
            NetMsg valid = RoundTimeAddedMsg.Encode(90f, 458d);

            Assert.IsFalse(DecodeRoundTimeAdded(WithPayload(valid, null)));
            Assert.IsFalse(DecodeRoundTimeAdded(WithPayload(valid, new object[0])));
            Assert.IsFalse(DecodeRoundTimeAdded(WithPayload(valid, Prefix(valid.Payload, 1))), "bonus without end time");
        }

        [Test]
        public void RoundTimeAdded_TryDecode_IsLenientAboutExtraTrailingElements()
        {
            NetMsg longer = WithAppended(RoundTimeAddedMsg.Encode(90f, 458d), 99);

            Assert.IsTrue(RoundTimeAddedMsg.TryDecode(longer, out float bonus, out double end));
            Assert.AreEqual(90f, bonus);
            Assert.AreEqual(458d, end);
        }

        [Test]
        public void RoundTimeAdded_TryDecode_RejectsWrongElementTypes()
        {
            NetMsg valid = RoundTimeAddedMsg.Encode(90f, 458d);

            Assert.IsFalse(DecodeRoundTimeAdded(WithElement(valid, 0, 90d)), "bonus as double");
            Assert.IsFalse(DecodeRoundTimeAdded(WithElement(valid, 0, 90)), "bonus as int");
            Assert.IsFalse(DecodeRoundTimeAdded(WithElement(valid, 1, 458f)), "end as float");
            AssertEveryElementRejectsWrongType(valid, DecodeRoundTimeAdded);
        }

        [Test]
        public void RoundTimeAdded_TryDecode_RejectsNonFiniteNumbers()
        {
            NetMsg valid = RoundTimeAddedMsg.Encode(90f, 458d);

            Assert.IsFalse(DecodeRoundTimeAdded(WithElement(valid, 0, float.NaN)));
            Assert.IsFalse(DecodeRoundTimeAdded(WithElement(valid, 0, float.PositiveInfinity)));
            Assert.IsFalse(DecodeRoundTimeAdded(WithElement(valid, 1, double.NaN)));
            Assert.IsFalse(DecodeRoundTimeAdded(WithElement(valid, 1, double.NegativeInfinity)));
        }
    }
}
