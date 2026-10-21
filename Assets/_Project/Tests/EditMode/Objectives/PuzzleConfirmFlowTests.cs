using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using CinliMahzen.Objectives;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.Objectives
{
    /// <summary>
    /// The confirm lever through PuzzleSystemCore (Teknik §7.3, TODO 2.4): ReqPuzzleConfirm → authority rules → solved: lock,
    /// PuzzleConfirmResult{ok} + PuzzleCompleted (every machine locks and publishes PuzzleCompletedEvt); wrong: ConfirmFailCooldown,
    /// PuzzleConfirmResult{fail} and ConfirmFailDamage to the human.
    /// </summary>
    public class PuzzleConfirmFlowTests : PuzzleSystemFixture
    {
        private void AssertNothingHappened(string because)
        {
            Assert.AreEqual(0, Broadcasts.Count, because + ": something was broadcast");
            Assert.AreEqual(0, CompletedEvents.Count, because + ": PuzzleCompletedEvt was published");
            Assert.AreEqual(0, HumanBody.Hits.Count, because + ": the human was hurt");
            Assert.IsFalse(Runtime.State.IsLocked, because + ": the puzzle was locked");
        }

        // ---------- correct answer ----------

        [Test]
        public void SolvedConfirm_ThroughTheBridge_Locks_SendsOkAndCompleted_AndPublishesTheEvent()
        {
            SendAs(HumanId, ReqPuzzleConfirmMsg.Encode(Slot));

            Assert.IsTrue(Runtime.State.IsLocked);
            Assert.IsTrue(Puzzles.IsPuzzleDone(Slot));

            Assert.AreEqual(2, Broadcasts.Count, "PuzzleConfirmResult + PuzzleCompleted, nothing else");
            Assert.AreEqual(MsgCode.PuzzleConfirmResult, Broadcasts[0].Code, "the result goes out first");
            Assert.IsTrue(PuzzleConfirmResultMsg.TryDecode(Broadcasts[0], out int resultSlot, out bool ok, out PlayerId by));
            Assert.AreEqual(Slot, resultSlot);
            Assert.IsTrue(ok);
            Assert.AreEqual(HumanId, by);

            Assert.AreEqual(MsgCode.PuzzleCompleted, Broadcasts[1].Code);
            Assert.IsTrue(PuzzleCompletedMsg.TryDecode(Broadcasts[1], out int completedSlot, out int stage));
            Assert.AreEqual(Slot, completedSlot);
            Assert.AreEqual(SlotStage, stage);

            Assert.AreEqual(1, CompletedEvents.Count);
            Assert.AreEqual(Slot, CompletedEvents[0].PuzzleSlot);
            Assert.AreEqual(SlotStage, CompletedEvents[0].Stage);
            Assert.AreEqual(0, HumanBody.Hits.Count, "a correct answer does not hurt");
        }

        [Test]
        public void SolvedConfirm_ReturnsNone_AndStartsNoLeverCooldown()
        {
            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessConfirmAuthority(HumanId, Slot));
            Assert.AreEqual(0d, Runtime.ConfirmCooldownUntil);
        }

        [Test]
        public void LockedPuzzle_RefusesOpsScramblesAndASecondConfirm()
        {
            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessConfirmAuthority(HumanId, Slot));
            ClearRecordings();

            Assert.AreEqual(PuzzleDenyReason.Locked, Puzzles.ProcessConfirmAuthority(HumanId, Slot), "no double completion");
            Assert.AreEqual(PuzzleDenyReason.Locked, Puzzles.ProcessOpAuthority(HumanId, Slot, PuzzleOp.Cycle(0, 1)));
            Assert.AreEqual(PuzzleDenyReason.Locked, Puzzles.ProcessScrambleAuthority(EvilId, Slot));

            Assert.AreEqual(0, Broadcasts.Count);
            Assert.AreEqual(0, CompletedEvents.Count);
            Assert.IsTrue(Runtime.State.IsSolved);
        }

        [Test]
        public void SolvingByHand_AfterASabotage_ThenConfirming_Completes()
        {
            BreakThePuzzle();
            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessOpAuthority(HumanId, Slot, PuzzleOp.Cycle(0, -1)));
            ClearRecordings();

            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessConfirmAuthority(HumanId, Slot));
            Assert.IsTrue(Runtime.State.IsLocked);
            Assert.AreEqual(1, CompletedEvents.Count);
        }

        // ---------- wrong answer ----------

        [Test]
        public void WrongConfirm_ThroughTheBridge_DealsDamage_StartsTheCooldown_AndSendsFail()
        {
            BreakThePuzzle();
            int[] before = Values();
            ClearRecordings();

            SendAs(HumanId, ReqPuzzleConfirmMsg.Encode(Slot));

            Assert.IsFalse(Runtime.State.IsLocked);
            CollectionAssert.AreEqual(before, Values(), "a wrong confirm does not touch the parts");
            Assert.AreEqual(Now + Config.ConfirmFailCooldown, Runtime.ConfirmCooldownUntil, 1e-9);

            Assert.AreEqual(1, Broadcasts.Count, "only PuzzleConfirmResult");
            Assert.IsTrue(PuzzleConfirmResultMsg.TryDecode(Broadcasts[0], out int slot, out bool ok, out PlayerId by));
            Assert.AreEqual(Slot, slot);
            Assert.IsFalse(ok);
            Assert.AreEqual(HumanId, by);
            Assert.AreEqual(0, CompletedEvents.Count);

            Assert.AreEqual(1, HumanBody.Hits.Count);
            DamageInfo hit = HumanBody.Hits[0];
            Assert.AreEqual(Config.ConfirmFailDamage, hit.Amount);
            Assert.AreEqual(PuzzleSystemCore.ConfirmFailCauseId, hit.CauseId);
            Assert.AreEqual(PlayerId.None, hit.Attacker);
            Assert.AreEqual(Runtime.ConfirmPosition, hit.Point);
        }

        [Test]
        public void WrongConfirm_ReturnsWrongAnswer()
        {
            BreakThePuzzle();
            Assert.AreEqual(PuzzleDenyReason.WrongAnswer, Puzzles.ProcessConfirmAuthority(HumanId, Slot));
        }

        [Test]
        public void SecondConfirm_InsideTheLeverCooldown_IsRefused_WithoutMoreDamage()
        {
            BreakThePuzzle();
            Assert.AreEqual(PuzzleDenyReason.WrongAnswer, Puzzles.ProcessConfirmAuthority(HumanId, Slot));
            ClearRecordings();

            Now += Config.ConfirmFailCooldown * 0.5f;
            Assert.AreEqual(PuzzleDenyReason.ConfirmCooldown, Puzzles.ProcessConfirmAuthority(HumanId, Slot));
            AssertNothingHappened("lever on cooldown");

            // Even fixing the puzzle does not unlock the lever early.
            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessOpAuthority(HumanId, Slot, PuzzleOp.Cycle(0, -1)));
            ClearRecordings();
            Assert.AreEqual(PuzzleDenyReason.ConfirmCooldown, Puzzles.ProcessConfirmAuthority(HumanId, Slot));
            AssertNothingHappened("lever on cooldown, puzzle solved");
        }

        [Test]
        public void Confirm_AfterTheLeverCooldown_Works_Again()
        {
            BreakThePuzzle();
            Assert.AreEqual(PuzzleDenyReason.WrongAnswer, Puzzles.ProcessConfirmAuthority(HumanId, Slot));
            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessOpAuthority(HumanId, Slot, PuzzleOp.Cycle(0, -1)));
            ClearRecordings();

            Now = Runtime.ConfirmCooldownUntil;
            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessConfirmAuthority(HumanId, Slot), "the cooldown ends exactly at its end time");
            Assert.IsTrue(Runtime.State.IsLocked);
        }

        [Test]
        public void TheLeverCooldown_IsPerPuzzle()
        {
            PuzzleSlotRuntime other = NewPaintings(new Vector3(0f, 0f, 0.5f), SlotStage);
            Puzzles.RegisterSlot(1, other);

            BreakThePuzzle();
            Assert.AreEqual(PuzzleDenyReason.WrongAnswer, Puzzles.ProcessConfirmAuthority(HumanId, Slot));

            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessConfirmAuthority(HumanId, 1));
            Assert.IsTrue(other.State.IsLocked);
        }

        [Test]
        public void WrongConfirm_WithoutADamageablePawn_StillStartsTheCooldown()
        {
            HasHumanBody = false;
            BreakThePuzzle();

            Assert.AreEqual(PuzzleDenyReason.WrongAnswer, Puzzles.ProcessConfirmAuthority(HumanId, Slot));
            Assert.AreEqual(Now + Config.ConfirmFailCooldown, Runtime.ConfirmCooldownUntil, 1e-9);
            Assert.AreEqual(0, HumanBody.Hits.Count);
        }

        // ---------- refusals ----------

        [Test]
        public void Confirm_FromEveryNonHumanRole_IsRefused()
        {
            BreakThePuzzle();
            ClearRecordings();
            foreach (PlayerId who in new[] { GoodJinnId, EvilId, OtherEvilId })
            {
                Assert.AreEqual(PuzzleDenyReason.WrongRole, Puzzles.ProcessConfirmAuthority(who, Slot), who.ToString());
            }
            AssertNothingHappened("wrong role");
            Assert.AreEqual(0d, Runtime.ConfirmCooldownUntil, "a refused request starts no cooldown");
        }

        [Test]
        public void Confirm_WhileTheHumanIsFainted_IsRefused()
        {
            HumanStatus.IsFainted = true;
            Assert.AreEqual(PuzzleDenyReason.HumanFainted, Puzzles.ProcessConfirmAuthority(HumanId, Slot));
            AssertNothingHappened("fainted");
        }

        [Test]
        public void Confirm_OutOfRange_IsRefused_AndRangeIsMeasuredToTheLever()
        {
            float limit = Config.InteractRange + Config.NetRangeTolerance;
            PawnPositions[HumanId] = new Vector3(limit + 0.5f, 0f, 0f);
            Assert.AreEqual(PuzzleDenyReason.OutOfRange, Puzzles.ProcessConfirmAuthority(HumanId, Slot));
            AssertNothingHappened("out of range");

            Runtime.ConfirmPosition = new Vector3(limit + 0.5f, 0f, 1f);
            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessConfirmAuthority(HumanId, Slot), "the lever stands next to the human");
        }

        [Test]
        public void Confirm_WithoutAPawn_IsRefusedAsPawnMissing()
        {
            PawnPositions.Remove(HumanId);
            Assert.AreEqual(PuzzleDenyReason.PawnMissing, Puzzles.ProcessConfirmAuthority(HumanId, Slot));
            AssertNothingHappened("no pawn");
        }

        [Test]
        public void Confirm_ForAnUnknownSlot_IsRefused()
        {
            Assert.AreEqual(PuzzleDenyReason.UnknownSlot, Puzzles.ProcessConfirmAuthority(HumanId, 42));
            AssertNothingHappened("unknown slot");
        }

        [Test]
        public void Confirm_MalformedRequest_IsIgnored()
        {
            SendAs(HumanId, new NetMsg { Code = MsgCode.ReqPuzzleConfirm, Payload = new object[] { "not a slot" } });
            AssertNothingHappened("malformed");
        }
    }
}
