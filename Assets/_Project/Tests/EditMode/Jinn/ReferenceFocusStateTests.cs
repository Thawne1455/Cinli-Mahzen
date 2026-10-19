using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Jinn;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.Jinn
{
    public class ReferenceFocusStateTests
    {
        private static readonly PlayerId P1 = new PlayerId(1);
        private static readonly PlayerId P2 = new PlayerId(2);

        private ReferenceFocusState _state;

        [SetUp]
        public void SetUp()
        {
            _state = new ReferenceFocusState();
        }

        [Test]
        public void StartsWithNobodyFocusing()
        {
            Assert.AreEqual(0, _state.Count);
            Assert.IsFalse(_state.IsFocusing(P1));
        }

        [Test]
        public void TrySet_On_ReportsTheChange()
        {
            Assert.IsTrue(_state.TrySet(P1, true));

            Assert.IsTrue(_state.IsFocusing(P1));
            Assert.AreEqual(1, _state.Count);
        }

        [Test]
        public void TrySet_SameValueAgain_ReportsNoChange()
        {
            _state.TrySet(P1, true);

            Assert.IsFalse(_state.TrySet(P1, true));
            Assert.IsTrue(_state.IsFocusing(P1));
            Assert.AreEqual(1, _state.Count);
        }

        [Test]
        public void TrySet_Off_ReportsTheChange_AndClearsTheFlag()
        {
            _state.TrySet(P1, true);

            Assert.IsTrue(_state.TrySet(P1, false));

            Assert.IsFalse(_state.IsFocusing(P1));
            Assert.AreEqual(0, _state.Count);
        }

        [Test]
        public void TrySet_OffWhenAlreadyOff_ReportsNoChange()
        {
            Assert.IsFalse(_state.TrySet(P1, false));
            Assert.AreEqual(0, _state.Count);
        }

        [Test]
        public void PlayersAreIndependent()
        {
            _state.TrySet(P1, true);

            Assert.IsTrue(_state.IsFocusing(P1));
            Assert.IsFalse(_state.IsFocusing(P2));

            _state.TrySet(P2, true);
            _state.TrySet(P1, false);

            Assert.IsFalse(_state.IsFocusing(P1));
            Assert.IsTrue(_state.IsFocusing(P2));
            Assert.AreEqual(1, _state.Count);
        }

        [Test]
        public void InvalidPlayer_IsNeverFocusing_AndCannotBeSet()
        {
            Assert.IsFalse(_state.TrySet(PlayerId.None, true));

            Assert.IsFalse(_state.IsFocusing(PlayerId.None));
            Assert.AreEqual(0, _state.Count);
        }

        [Test]
        public void HighestValidPlayerId_Works()
        {
            var last = new PlayerId(254);

            Assert.IsTrue(_state.TrySet(last, true));
            Assert.IsTrue(_state.IsFocusing(last));
        }

        [Test]
        public void ClearAll_ReturnsAndListsTheClearedPlayers()
        {
            _state.TrySet(P2, true);
            _state.TrySet(P1, true);
            var cleared = new List<PlayerId>();

            int n = _state.ClearAll(cleared);

            Assert.AreEqual(2, n);
            Assert.AreEqual(2, cleared.Count);
            Assert.Contains(P1, cleared);
            Assert.Contains(P2, cleared);
            Assert.AreEqual(0, _state.Count);
            Assert.IsFalse(_state.IsFocusing(P1));
            Assert.IsFalse(_state.IsFocusing(P2));
        }

        [Test]
        public void ClearAll_WhenNobodyFocuses_ListsNothing()
        {
            var cleared = new List<PlayerId>();

            Assert.AreEqual(0, _state.ClearAll(cleared));
            Assert.AreEqual(0, cleared.Count);
        }

        [Test]
        public void ClearAll_WithoutAList_StillClears()
        {
            _state.TrySet(P1, true);

            Assert.AreEqual(1, _state.ClearAll(null));
            Assert.IsFalse(_state.IsFocusing(P1));
        }

        [Test]
        public void SetAgainAfterClearAll_ReportsAChange()
        {
            _state.TrySet(P1, true);
            _state.ClearAll(null);

            Assert.IsTrue(_state.TrySet(P1, true));
            Assert.AreEqual(1, _state.Count);
        }
    }
}
