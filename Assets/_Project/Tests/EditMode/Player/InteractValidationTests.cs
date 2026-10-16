using CinliMahzen.Player;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.Player
{
    /// <summary>Range rule for hold-to-interact: InteractRange (2.2) + NetRangeTolerance (0.5), Teknik §4.5.</summary>
    public class InteractValidationTests
    {
        private const float InteractRange = 2.2f;
        private const float Tolerance = 0.5f;

        private static readonly Vector3 Pawn = Vector3.zero;

        [Test]
        public void ExactlyAtTheLimit_IsInside()
        {
            Assert.IsTrue(InteractValidation.IsInRange(Pawn, new Vector3(0f, 0f, 2.7f), InteractRange, Tolerance));
        }

        [Test]
        public void JustBeyondTheLimit_IsOutside()
        {
            Assert.IsFalse(InteractValidation.IsInRange(Pawn, new Vector3(0f, 0f, 2.71f), InteractRange, Tolerance));
        }

        [Test]
        public void SamePosition_IsInside()
        {
            Assert.IsTrue(InteractValidation.IsInRange(Pawn, Pawn, InteractRange, Tolerance));
        }

        [Test]
        public void DiagonalWithinLimit_IsInside()
        {
            Assert.IsTrue(InteractValidation.IsInRange(Pawn, new Vector3(1.5f, 0f, 1.5f), InteractRange, Tolerance));
        }

        [Test]
        public void VerticalOffsetCountsTowardsTheDistance()
        {
            Assert.IsFalse(InteractValidation.IsInRange(Pawn, new Vector3(0f, 2.8f, 0f), InteractRange, Tolerance));
        }

        [Test]
        public void ZeroTolerance_LimitIsTheInteractRange()
        {
            Assert.IsTrue(InteractValidation.IsInRange(Pawn, new Vector3(0f, 0f, InteractRange), InteractRange, 0f));
            Assert.IsFalse(InteractValidation.IsInRange(Pawn, new Vector3(0f, 0f, 2.21f), InteractRange, 0f));
        }

        [Test]
        public void NegativeLimit_IsOutside()
        {
            Assert.IsFalse(InteractValidation.IsInRange(Pawn, Pawn, -1f, 0.5f));
        }
    }
}
