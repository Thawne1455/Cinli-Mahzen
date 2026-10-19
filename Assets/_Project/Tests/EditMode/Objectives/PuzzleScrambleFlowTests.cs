using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using CinliMahzen.Objectives;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.Objectives
{
    /// <summary>
    /// Evil jinn mass sabotage through PuzzleSystemCore (Teknik §7.3, Oyun §4.2, TODO 2.4): ReqPuzzleScramble → role + JinnsAwake,
    /// dark room, no lantern protection, cooldown, energy → pay, TryScrambleAll, cooldown = now + 15 → PuzzleScrambled broadcast.
    /// </summary>
    public class PuzzleScrambleFlowTests : PuzzleSystemFixture
    {
        private const float StartEnergy = 100f;
        private const float Cost = 25f;
        private const double Cooldown = 15d;

        private static readonly int[] Solved = { 1, 3, 0 };

        [SetUp]
        public void GiveTheEvilJinnEnergy()
        {
            EnergyService.SetEnergy(EvilId, StartEnergy);
            EnergyService.SetEnergy(OtherEvilId, StartEnergy);
        }

        /// <summary>The refused request left no trace: arrangement, cooldown, energy, messages and events.</summary>
        private void AssertNothingHappened(string because, double expectedCooldownUntil = 0d, float expectedEnergy = StartEnergy)
        {
            CollectionAssert.AreEqual(Solved, Values(), because + ": values changed");
            Assert.AreEqual(expectedCooldownUntil, Runtime.State.ScrambleCooldownUntil, because + ": cooldown changed");
            Assert.AreEqual(expectedEnergy, EnergyService.Get(EvilId), because + ": energy was spent");
            Assert.AreEqual(0, Broadcasts.Count, because + ": something was broadcast");
            Assert.AreEqual(0, ScrambledEvents.Count, because + ": an event was published");
        }

        // ---------- the accepted case ----------

        [Test]
        public void EvilJinnScramble_ThroughTheBridge_ScramblesPaysEnergyStartsTheCooldown_AndBroadcasts()
        {
            SendAs(EvilId, ReqPuzzleScrambleMsg.Encode(Slot));

            Assert.IsFalse(Runtime.State.IsSolved, "a mass scramble never leaves the puzzle solved");
            Assert.IsFalse(Runtime.State.IsLocked);
            Assert.AreEqual(StartEnergy - Cost, EnergyService.Get(EvilId), "ScrambleEnergyCost (25) was paid");
            Assert.AreEqual(100d + Cooldown, Runtime.State.ScrambleCooldownUntil, "cooldown = Net.Time + ScramblePuzzleCooldown (15 s)");

            Assert.AreEqual(1, Broadcasts.Count, "exactly one broadcast");
            Assert.IsTrue(PuzzleScrambledMsg.TryDecode(Sent(MsgCode.PuzzleScrambled)[0], out int slot, out int[] values, out PlayerId by, out double cooldownUntil));
            Assert.AreEqual(Slot, slot);
            CollectionAssert.AreEqual(Values(), values, "the broadcast carries the new arrangement");
            CollectionAssert.AreNotEqual(Solved, values);
            Assert.AreEqual(EvilId, by);
            Assert.AreEqual(115d, cooldownUntil);

            Assert.AreEqual(1, ScrambledEvents.Count);
            Assert.AreEqual(Slot, ScrambledEvents[0].PuzzleSlot);
            Assert.AreEqual(EvilId, ScrambledEvents[0].ByJinn);
            Assert.AreEqual(115d, ScrambledEvents[0].CooldownUntil);
            Assert.AreEqual(0, StateChangedEvents.Count, "a scramble is not an op");
        }

        [Test]
        public void EvilJinnScramble_ReturnsNone_AndEveryValueStaysInRange()
        {
            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessScrambleAuthority(EvilId, Slot));

            for (int part = 0; part < Runtime.State.PartCount; part++)
            {
                Assert.That(Runtime.State.Get(part), Is.InRange(0, Runtime.State.ValueCount(part) - 1));
            }
        }

        [Test]
        public void EitherEvilJinnMayScramble()
        {
            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessScrambleAuthority(OtherEvilId, Slot));
            Assert.AreEqual(StartEnergy - Cost, EnergyService.Get(OtherEvilId));
            Assert.AreEqual(StartEnergy, EnergyService.Get(EvilId), "only the sender pays");
            Assert.AreEqual(OtherEvilId, ScrambledEvents[0].ByJinn);
        }

        [Test]
        public void Scramble_WithTheDefaultEnergyStub_AlwaysSucceeds_AndCountsTheSpend()
        {
            EnergyService.ClearEnergy(EvilId);

            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessScrambleAuthority(EvilId, Slot));

            Assert.AreEqual(Cost, EnergyService.TotalSpent);
            Assert.AreEqual(Config.EnergyMax, EnergyService.Get(EvilId), "the stub tank is unaffected");
        }

        // ---------- 15 s cooldown ----------

        [Test]
        public void SecondScramble_InsideTheCooldown_IsRefused_AndChangesNothing()
        {
            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessScrambleAuthority(EvilId, Slot));
            int[] afterFirst = Values();
            ClearRecordings();

            Now = 100d + Cooldown - 0.1d;
            Assert.AreEqual(PuzzleDenyReason.ScrambleCooldown, Puzzles.ProcessScrambleAuthority(EvilId, Slot));
            Assert.AreEqual(PuzzleDenyReason.ScrambleCooldown, Puzzles.ProcessScrambleAuthority(OtherEvilId, Slot), "the cooldown belongs to the puzzle, not the jinn");

            CollectionAssert.AreEqual(afterFirst, Values(), "arrangement untouched");
            Assert.AreEqual(115d, Runtime.State.ScrambleCooldownUntil, "cooldown not extended");
            Assert.AreEqual(StartEnergy - Cost, EnergyService.Get(EvilId), "no second payment");
            Assert.AreEqual(StartEnergy, EnergyService.Get(OtherEvilId));
            Assert.AreEqual(0, Broadcasts.Count);
            Assert.AreEqual(0, ScrambledEvents.Count);
        }

        [Test]
        public void SecondScramble_AfterTheCooldown_Works_AndStartsAFreshCooldown()
        {
            Puzzles.ProcessScrambleAuthority(EvilId, Slot);

            Now = 100d + Cooldown;
            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessScrambleAuthority(EvilId, Slot), "exactly at the end of the cooldown");

            Assert.AreEqual(StartEnergy - 2 * Cost, EnergyService.Get(EvilId));
            Assert.AreEqual(Now + Cooldown, Runtime.State.ScrambleCooldownUntil);
            Assert.AreEqual(2, ScrambledEvents.Count);
        }

        [Test]
        public void TheCooldown_IsPerPuzzle()
        {
            Puzzles.RegisterSlot(1, NewPaintings(Vector3.zero, SlotStage));
            Puzzles.ProcessScrambleAuthority(EvilId, Slot);

            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessScrambleAuthority(EvilId, 1), "another puzzle is not on cooldown");
            Assert.IsFalse(Puzzles.IsPuzzleScrambleOnCooldown(2, out _));
            Assert.IsTrue(Puzzles.IsPuzzleScrambleOnCooldown(1, out _));
        }

        // ---------- lantern protection ----------

        [Test]
        public void Scramble_InsideTheStrongLanternSafeZone_IsRefused_AndSpendsNothing()
        {
            HumanStatus.Position = new Vector3(1f, 0f, 0f); // 1 m from the puzzle, safe zone radius 2.5 m
            HumanStatus.IsBlessedLanternActive = true;

            Assert.AreEqual(PuzzleDenyReason.LanternProtected, Puzzles.ProcessScrambleAuthority(EvilId, Slot));

            AssertNothingHappened("lantern protected");
        }

        [Test]
        public void Scramble_ThroughTheBridge_InsideTheStrongLanternSafeZone_ChangesNothing()
        {
            HumanStatus.Position = new Vector3(0f, 0f, 2f);
            HumanStatus.IsBlessedLanternActive = true;

            SendAs(EvilId, ReqPuzzleScrambleMsg.Encode(Slot));

            AssertNothingHappened("lantern protected via bridge");
        }

        [Test]
        public void Scramble_WithTheHumanOutsideTheSafeZone_OrWithAWeakLantern_IsAllowed()
        {
            HumanStatus.IsBlessedLanternActive = true;
            HumanStatus.Position = new Vector3(2.6f, 0f, 0f); // just outside 2.5 m
            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessScrambleAuthority(EvilId, Slot), "human too far away");

            Now += Cooldown;
            HumanStatus.IsBlessedLanternActive = false;
            HumanStatus.Position = Vector3.zero;
            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessScrambleAuthority(EvilId, Slot), "right next to it, but the lantern is weak");
        }

        // ---------- light, wake delay, locks ----------

        [Test]
        public void Scramble_InALitRoom_IsRefused()
        {
            Rooms.SetLight(0, RoomLightState.On);

            Assert.AreEqual(PuzzleDenyReason.LitRoom, Puzzles.ProcessScrambleAuthority(EvilId, Slot));
            AssertNothingHappened("lit room");

            Rooms.SetLight(0, RoomLightState.Off);
            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessScrambleAuthority(EvilId, Slot), "dark again");
        }

        [Test]
        public void Scramble_ReadsTheLightOfThePuzzlesOwnRoom()
        {
            Puzzles.RegisterSlot(1, new PuzzleSlotRuntime(NewPaintingsState(), string.Empty, Vector3.zero, 7, SlotStage));
            Rooms.SetLight(0, RoomLightState.On);

            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessScrambleAuthority(EvilId, 1), "room 7 is dark, room 0 is lit");
            Assert.AreEqual(PuzzleDenyReason.LitRoom, Puzzles.ProcessScrambleAuthority(EvilId, Slot));
        }

        [Test]
        public void Scramble_BeforeTheJinnsWakeUp_IsRefused()
        {
            MatchInfo.JinnsAwake = false;

            Assert.AreEqual(PuzzleDenyReason.JinnsAsleep, Puzzles.ProcessScrambleAuthority(EvilId, Slot));
            AssertNothingHappened("jinns asleep");
        }

        [Test]
        public void Scramble_OnALockedPuzzle_IsRefused_AndSpendsNothing()
        {
            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessConfirmAuthority(HumanId, Slot));
            ClearRecordings();

            Assert.AreEqual(PuzzleDenyReason.Locked, Puzzles.ProcessScrambleAuthority(EvilId, Slot));
            AssertNothingHappened("locked");
        }

        // ---------- role, range, energy ----------

        [Test]
        public void Scramble_FromTheHumanOrTheGoodJinn_IsRefused()
        {
            Assert.AreEqual(PuzzleDenyReason.WrongRole, Puzzles.ProcessScrambleAuthority(HumanId, Slot));
            Assert.AreEqual(PuzzleDenyReason.WrongRole, Puzzles.ProcessScrambleAuthority(GoodJinnId, Slot));
            Assert.AreEqual(PuzzleDenyReason.WrongRole, Puzzles.ProcessScrambleAuthority(PlayerId.None, Slot));
            AssertNothingHappened("wrong role");
        }

        [Test]
        public void Scramble_ThroughTheBridge_FromTheHuman_ChangesNothing()
        {
            SendAs(HumanId, ReqPuzzleScrambleMsg.Encode(Slot));

            AssertNothingHappened("human sender");
        }

        [Test]
        public void Scramble_OutOfRange_IsRefused_AtExactlyScrambleRangePlusTolerance()
        {
            // 3 m scramble range + 0.5 m network tolerance = 3.5 m.
            PawnPositions[EvilId] = new Vector3(3.6f, 0f, 0f);
            Assert.AreEqual(PuzzleDenyReason.OutOfRange, Puzzles.ProcessScrambleAuthority(EvilId, Slot));
            AssertNothingHappened("out of range");

            PawnPositions[EvilId] = new Vector3(3.5f, 0f, 0f);
            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessScrambleAuthority(EvilId, Slot), "on the limit");
        }

        [Test]
        public void Scramble_WithoutAPawn_IsRefusedAsPawnMissing()
        {
            PawnPositions.Remove(EvilId);

            Assert.AreEqual(PuzzleDenyReason.PawnMissing, Puzzles.ProcessScrambleAuthority(EvilId, Slot));
            AssertNothingHappened("pawn missing");
        }

        [Test]
        public void Scramble_WithTooLittleEnergy_IsRefused_AndTheEnergyIsKept()
        {
            EnergyService.SetEnergy(EvilId, Cost - 0.1f);

            Assert.AreEqual(PuzzleDenyReason.InsufficientEnergy, Puzzles.ProcessScrambleAuthority(EvilId, Slot));

            AssertNothingHappened("too little energy", 0d, Cost - 0.1f);
        }

        [Test]
        public void Scramble_WithExactlyTheCostInEnergy_Works_AndLeavesZero()
        {
            EnergyService.SetEnergy(EvilId, Cost);

            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessScrambleAuthority(EvilId, Slot));
            Assert.AreEqual(0f, EnergyService.Get(EvilId));
        }

        [Test]
        public void Scramble_ForAnUnknownSlot_IsRefused()
        {
            Assert.AreEqual(PuzzleDenyReason.UnknownSlot, Puzzles.ProcessScrambleAuthority(EvilId, 9));
            AssertNothingHappened("unknown slot");
        }

        [Test]
        public void Scramble_RequestsAreIgnoredOnAClientThatIsNotTheAuthority()
        {
            Bridge.IsAuthority = false;

            SendAs(EvilId, ReqPuzzleScrambleMsg.Encode(Slot));

            AssertNothingHappened("non-authority");
        }

        [Test]
        public void Scramble_MalformedRequest_IsIgnored()
        {
            SendAs(EvilId, new NetMsg { Code = MsgCode.ReqPuzzleScramble, Payload = new object[] { 1.5f } });

            AssertNothingHappened("malformed");
        }

        // ---------- the scrambled puzzle can be worked on ----------

        [Test]
        public void AfterAScramble_TheHumanCanStillOperateIt_AndSolveItAgain()
        {
            Puzzles.ProcessScrambleAuthority(EvilId, Slot);
            Assert.IsFalse(Runtime.State.IsSolved);

            // Walk every part back to its answer with single steps (at most 3 per part with 4 values).
            for (int part = 0; part < Runtime.State.PartCount; part++)
            {
                while (Runtime.State.Get(part) != PaintingsSolution[part])
                {
                    Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessOpAuthority(HumanId, Slot, PuzzleOp.Cycle(part, 1)));
                }
            }

            Assert.IsTrue(Runtime.State.IsSolved);
            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessConfirmAuthority(HumanId, Slot));
            Assert.IsTrue(Puzzles.IsPuzzleDone(Slot));
        }
    }
}
