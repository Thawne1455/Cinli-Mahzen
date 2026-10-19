using CinliMahzen.Core;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.A
{
    /// <summary>TODO 1.3: the change detector behind PawnBase.OnLocalityChanged (plain C#).</summary>
    public class LocalityTrackerTests
    {
        [Test]
        public void New_IsUnknown_AndNotLocal()
        {
            var t = new LocalityTracker();

            Assert.IsTrue(t.IsUnknown);
            Assert.IsFalse(t.IsLocal);
        }

        [Test]
        public void FirstObservation_AlwaysCounts_EvenWhenNotLocal()
        {
            var notLocal = new LocalityTracker();
            var local = new LocalityTracker();

            Assert.IsTrue(notLocal.TryUpdate(false), "a pawn that starts non-local must still bind its idle input once");
            Assert.IsTrue(local.TryUpdate(true));
            Assert.IsFalse(notLocal.IsUnknown);
            Assert.IsTrue(local.IsLocal);
        }

        [Test]
        public void SameValue_IsNotAChange()
        {
            var t = new LocalityTracker();
            t.TryUpdate(true);

            Assert.IsFalse(t.TryUpdate(true));
            Assert.IsFalse(t.TryUpdate(true));
        }

        [Test]
        public void HotseatSwitches_ReportEveryFlip()
        {
            var t = new LocalityTracker();
            t.TryUpdate(false);

            Assert.IsTrue(t.TryUpdate(true));
            Assert.IsTrue(t.IsLocal);
            Assert.IsTrue(t.TryUpdate(false));
            Assert.IsFalse(t.IsLocal);
            Assert.IsFalse(t.TryUpdate(false));
        }

        [Test]
        public void Reset_MakesTheNextObservationAChange()
        {
            var t = new LocalityTracker();
            t.TryUpdate(true);

            t.Reset();

            Assert.IsTrue(t.IsUnknown);
            Assert.IsFalse(t.IsLocal);
            Assert.IsTrue(t.TryUpdate(true), "an owner change re-binds even though the answer is the same");
        }
    }
}
