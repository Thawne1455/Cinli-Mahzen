using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using CinliMahzen.Objectives;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.Objectives
{
    /// <summary>
    /// Human part changes through PuzzleSystemCore (Teknik §7.3, TODO 2.4): ReqPuzzleOp → authority rules → TryApply →
    /// PuzzleStateChanged broadcast → every machine applies and publishes PuzzleStateChangedEvt.
    /// </summary>
    public class PuzzleOpFlowTests : PuzzleSystemFixture
    {
        private static readonly int[] Solved = { 1, 3, 0 };

        private void AssertNothingHappened(string because)
        {
            CollectionAssert.AreEqual(Solved, Values(), because + ": values changed");
            Assert.AreEqual(0, Broadcasts.Count, because + ": something was broadcast");
            Assert.AreEqual(0, StateChangedEvents.Count, because + ": an event was published");
        }

        [Test]
        public void HumanOp_ThroughTheBridge_ChangesTheState_Broadcasts_AndPublishesTheEvent()
        {
            SendAs(HumanId, ReqPuzzleOpMsg.Encode(Slot, PuzzleOp.Cycle(0, 1)));

            CollectionAssert.AreEqual(new[] { 2, 3, 0 }, Values());
            Assert.IsFalse(Runtime.State.IsSolved);

            Assert.AreEqual(1, Sent(MsgCode.PuzzleStateChanged).Count);
            Assert.AreEqual(1, Broadcasts.Count, "nothing but PuzzleStateChanged");
            Assert.IsTrue(PuzzleStateChangedMsg.TryDecode(Sent(MsgCode.PuzzleStateChanged)[0], out int slot, out int[] values, out PlayerId by));
            Assert.AreEqual(Slot, slot);
            CollectionAssert.AreEqual(new[] { 2, 3, 0 }, values);
            Assert.AreEqual(HumanId, by);

            Assert.AreEqual(1, StateChangedEvents.Count);
            Assert.AreEqual(Slot, StateChangedEvents[0].PuzzleSlot);
            Assert.AreEqual(HumanId, StateChangedEvents[0].ByPlayer);
        }

        [Test]
        public void HumanOp_ReturnsNone_AndEveryOpKindThePuzzleAllowsWorks()
        {
            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessOpAuthority(HumanId, Slot, PuzzleOp.Cycle(1, -1)));
            CollectionAssert.AreEqual(new[] { 1, 2, 0 }, Values());

            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessOpAuthority(HumanId, Slot, PuzzleOp.Cycle(1, 1)));
            CollectionAssert.AreEqual(Solved, Values());
            Assert.IsTrue(Runtime.State.IsSolved, "the human can undo a change");
        }

        [Test]
        public void HumanOp_SolvingByHand_NeedsNoSpecialHandling()
        {
            BreakThePuzzle();
            ClearRecordings();

            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessOpAuthority(HumanId, Slot, PuzzleOp.Cycle(0, -1)));

            Assert.IsTrue(Runtime.State.IsSolved);
            Assert.AreEqual(1, StateChangedEvents.Count);
            Assert.IsFalse(Runtime.State.IsLocked, "only the confirm lever locks");
        }

        [Test]
        public void Op_FromEveryNonHumanRole_IsRefused()
        {
            foreach (PlayerId who in new[] { GoodJinnId, EvilId, OtherEvilId })
            {
                Assert.AreEqual(PuzzleDenyReason.WrongRole, Puzzles.ProcessOpAuthority(who, Slot, PuzzleOp.Cycle(0, 1)), who.ToString());
            }
            Assert.AreEqual(PuzzleDenyReason.WrongRole, Puzzles.ProcessOpAuthority(PlayerId.None, Slot, PuzzleOp.Cycle(0, 1)), "unknown player");
            AssertNothingHappened("wrong role");
        }

        [Test]
        public void Op_ThroughTheBridge_FromAnEvilJinn_ChangesNothing()
        {
            SendAs(EvilId, ReqPuzzleOpMsg.Encode(Slot, PuzzleOp.Cycle(0, 1)));

            AssertNothingHappened("evil jinn op");
        }

        [Test]
        public void Op_OutOfRange_IsRefused_AtExactlyInteractRangePlusTolerance()
        {
            // 2.2 m interact range + 0.5 m network tolerance = 2.7 m.
            PawnPositions[HumanId] = new Vector3(2.7f, 0f, 0f);
            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessOpAuthority(HumanId, Slot, PuzzleOp.Cycle(0, 1)), "on the limit");

            ClearRecordings();
            Runtime.State.ResetToSolution();
            PawnPositions[HumanId] = new Vector3(2.8f, 0f, 0f);
            Assert.AreEqual(PuzzleDenyReason.OutOfRange, Puzzles.ProcessOpAuthority(HumanId, Slot, PuzzleOp.Cycle(0, 1)), "past the limit");
            AssertNothingHappened("out of range");
        }

        [Test]
        public void Op_RangeIsMeasuredToTheSlot_NotToTheOrigin()
        {
            Puzzles.RegisterSlot(1, NewPaintings(new Vector3(10f, 0f, 0f), SlotStage));

            Assert.AreEqual(PuzzleDenyReason.OutOfRange, Puzzles.ProcessOpAuthority(HumanId, 1, PuzzleOp.Cycle(0, 1)));

            PawnPositions[HumanId] = new Vector3(9f, 0f, 0f);
            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessOpAuthority(HumanId, 1, PuzzleOp.Cycle(0, 1)));
        }

        [Test]
        public void Op_WithoutAPawn_IsRefusedAsPawnMissing()
        {
            PawnPositions.Remove(HumanId);

            Assert.AreEqual(PuzzleDenyReason.PawnMissing, Puzzles.ProcessOpAuthority(HumanId, Slot, PuzzleOp.Cycle(0, 1)));
            AssertNothingHappened("pawn missing");
        }

        [Test]
        public void Op_NeedsTheRequiredItem_OnlyWhenThePuzzleHasOne()
        {
            Puzzles.RegisterSlot(1, NewPaintedPuzzle(Vector3.zero));

            Assert.AreEqual(PuzzleDenyReason.MissingItem, Puzzles.ProcessOpAuthority(HumanId, 1, PuzzleOp.Set(0, 0)), "no paint");
            Assert.AreEqual(1, Puzzles.Slots[1].State.Get(0), "state untouched");
            Assert.AreEqual(0, Broadcasts.Count);

            Inventory.Give(GoodJinnId, "paint");
            Assert.AreEqual(PuzzleDenyReason.MissingItem, Puzzles.ProcessOpAuthority(HumanId, 1, PuzzleOp.Set(0, 0)), "paint belongs to another player");

            Inventory.Give(HumanId, "fuse_key");
            Assert.AreEqual(PuzzleDenyReason.MissingItem, Puzzles.ProcessOpAuthority(HumanId, 1, PuzzleOp.Set(0, 0)), "wrong item");

            Inventory.Give(HumanId, "paint");
            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessOpAuthority(HumanId, 1, PuzzleOp.Set(0, 0)), "with paint");
            Assert.AreEqual(0, Puzzles.Slots[1].State.Get(0));

            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessOpAuthority(HumanId, Slot, PuzzleOp.Cycle(0, 1)), "slot 0 needs no item");
        }

        [Test]
        public void Op_OnALockedPuzzle_IsRefused()
        {
            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessConfirmAuthority(HumanId, Slot), "solved puzzle gets locked");
            ClearRecordings();

            Assert.AreEqual(PuzzleDenyReason.Locked, Puzzles.ProcessOpAuthority(HumanId, Slot, PuzzleOp.Cycle(0, 1)));
            AssertNothingHappened("locked");
        }

        [Test]
        public void Op_WhileTheHumanIsFainted_IsRefused()
        {
            HumanStatus.IsFainted = true;

            Assert.AreEqual(PuzzleDenyReason.HumanFainted, Puzzles.ProcessOpAuthority(HumanId, Slot, PuzzleOp.Cycle(0, 1)));
            AssertNothingHappened("fainted");
        }

        [Test]
        public void Op_TheStateRejects_IsRefusedAsInvalidOp_AndSendsNothing()
        {
            Assert.AreEqual(PuzzleDenyReason.InvalidOp, Puzzles.ProcessOpAuthority(HumanId, Slot, PuzzleOp.Set(0, 2)), "Set is not allowed on a Cycle puzzle");
            Assert.AreEqual(PuzzleDenyReason.InvalidOp, Puzzles.ProcessOpAuthority(HumanId, Slot, PuzzleOp.Swap(0, 1)), "Swap is not allowed either");
            Assert.AreEqual(PuzzleDenyReason.InvalidOp, Puzzles.ProcessOpAuthority(HumanId, Slot, PuzzleOp.Cycle(7, 1)), "no such part");
            Assert.AreEqual(PuzzleDenyReason.InvalidOp, Puzzles.ProcessOpAuthority(HumanId, Slot, PuzzleOp.Cycle(0, 2)), "cycle steps by one");
            Assert.AreEqual(PuzzleDenyReason.InvalidOp, Puzzles.ProcessOpAuthority(HumanId, Slot, default(PuzzleOp)), "default op is None");
            AssertNothingHappened("invalid op");
        }

        [Test]
        public void Op_ForAnUnknownSlot_IsRefused()
        {
            Assert.AreEqual(PuzzleDenyReason.UnknownSlot, Puzzles.ProcessOpAuthority(HumanId, 5, PuzzleOp.Cycle(0, 1)));
            Assert.AreEqual(PuzzleDenyReason.UnknownSlot, Puzzles.ProcessOpAuthority(HumanId, -1, PuzzleOp.Cycle(0, 1)));
            AssertNothingHappened("unknown slot");
        }

        [Test]
        public void Requests_AreIgnoredOnAClientThatIsNotTheAuthority()
        {
            Bridge.IsAuthority = false;

            SendAs(HumanId, ReqPuzzleOpMsg.Encode(Slot, PuzzleOp.Cycle(0, 1)));

            AssertNothingHappened("non-authority");
        }

        [Test]
        public void MalformedRequest_IsIgnored_WithoutThrowing()
        {
            SendAs(HumanId, new NetMsg { Code = MsgCode.ReqPuzzleOp, Payload = new object[] { "junk" } });
            SendAs(HumanId, new NetMsg { Code = MsgCode.ReqPuzzleOp });
            SendAs(HumanId, ReqPuzzleOpMsg.Encode(-3, PuzzleOp.Cycle(0, 1)));

            AssertNothingHappened("malformed");
        }

        [Test]
        public void AfterDetach_RequestsAreNotHandledAnymore()
        {
            Puzzles.Detach();

            SendAs(HumanId, ReqPuzzleOpMsg.Encode(Slot, PuzzleOp.Cycle(0, 1)));

            AssertNothingHappened("detached");
        }

        [Test]
        public void DebugIgnoreRange_SkipsOnlyTheRangeRule()
        {
            PawnPositions[HumanId] = new Vector3(50f, 0f, 0f);
            Puzzles.DebugIgnoreRange = true;

            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessOpAuthority(HumanId, Slot, PuzzleOp.Cycle(0, 1)), "far away but ignored");
            Assert.AreEqual(PuzzleDenyReason.WrongRole, Puzzles.ProcessOpAuthority(EvilId, Slot, PuzzleOp.Cycle(0, 1)), "role still enforced");

            PawnPositions.Remove(HumanId);
            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessOpAuthority(HumanId, Slot, PuzzleOp.Cycle(0, 1)), "no pawn needed either");

            Puzzles.DebugIgnoreRange = false;
            Assert.AreEqual(PuzzleDenyReason.PawnMissing, Puzzles.ProcessOpAuthority(HumanId, Slot, PuzzleOp.Cycle(0, 1)));
        }
    }
}
