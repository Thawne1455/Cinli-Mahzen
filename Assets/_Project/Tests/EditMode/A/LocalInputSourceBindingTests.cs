using CinliMahzen.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CinliMahzen.Tests.EditMode.A
{
    /// <summary>
    /// Binding table tests for CMInput.inputactions against Oyun §11. Deterministic: no virtual devices, no input
    /// events — only the action/binding definitions of a fresh <see cref="CMInput"/> are inspected.
    /// </summary>
    public class LocalInputSourceBindingTests
    {
        private CMInput _actions;

        [SetUp]
        public void SetUp()
        {
            _actions = new CMInput();
        }

        [TearDown]
        public void TearDown()
        {
            // Generated Dispose() uses Object.Destroy, which is illegal outside play mode (same as LocalInputSource.Dispose).
            if (Application.isPlaying)
                _actions.Dispose();
            else
                UnityEngine.Object.DestroyImmediate(_actions.asset);
            _actions = null;
        }

        private static void AssertBoundTo(InputAction action, string path)
        {
            Assert.IsNotNull(action, "action missing");
            var bindings = action.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                if (!bindings[i].isComposite && bindings[i].path == path)
                    return;
            }
            Assert.Fail("Action '" + action.name + "' has no binding '" + path + "'");
        }

        private static void AssertMissing(CMInput actions, string actionPath)
        {
            Assert.IsNull(actions.asset.FindAction(actionPath, false), "action '" + actionPath + "' should not exist");
        }

        // --- Human (Oyun §11 "İnsan (FPS)") ---

        [Test]
        public void Human_Bindings_MatchControlTable()
        {
            CMInput.HumanActions h = _actions.Human;
            AssertBoundTo(h.Sprint, "<Keyboard>/leftShift");
            AssertBoundTo(h.Interact, "<Keyboard>/e");
            AssertBoundTo(h.Kick, "<Keyboard>/f");
            AssertBoundTo(h.LanternPulse, "<Mouse>/rightButton");
            AssertBoundTo(h.Slot1, "<Keyboard>/1");
            AssertBoundTo(h.Slot2, "<Keyboard>/2");
            AssertBoundTo(h.Slot3, "<Keyboard>/3");
            AssertBoundTo(h.Drop, "<Keyboard>/g");
        }

        [Test]
        public void Human_MoveAndLook_Bindings()
        {
            CMInput.HumanActions h = _actions.Human;
            AssertBoundTo(h.Move, "<Keyboard>/w");
            AssertBoundTo(h.Move, "<Keyboard>/a");
            AssertBoundTo(h.Move, "<Keyboard>/s");
            AssertBoundTo(h.Move, "<Keyboard>/d");
            AssertBoundTo(h.Look, "<Mouse>/delta");
        }

        [Test]
        public void Human_RemovedActions_AreGone()
        {
            AssertMissing(_actions, "Human/Lantern");
            AssertMissing(_actions, "Human/UseItem");
            AssertMissing(_actions, "Human/DropGold");
        }

        // --- Spirit (Oyun §11 "Cin — Ruh Formu") ---

        [Test]
        public void Spirit_Bindings_MatchControlTable()
        {
            CMInput.SpiritActions s = _actions.Spirit;
            AssertBoundTo(s.Primary, "<Keyboard>/e");
            AssertBoundTo(s.Focus, "<Mouse>/rightButton");
            AssertBoundTo(s.Ping, "<Keyboard>/q");
            AssertBoundTo(s.Exorcise, "<Keyboard>/r");
        }

        [Test]
        public void Spirit_MoveLookVertical_Bindings()
        {
            CMInput.SpiritActions s = _actions.Spirit;
            AssertBoundTo(s.Move, "<Keyboard>/w");
            AssertBoundTo(s.Look, "<Mouse>/delta");
            AssertBoundTo(s.Vertical, "<Keyboard>/space");
            AssertBoundTo(s.Vertical, "<Keyboard>/leftCtrl");
        }

        [Test]
        public void Spirit_Boost_IsBothShifts()
        {
            // Both shifts: the Shift+E modifier is <Keyboard>/shift (left or right), so Boost must cover both too,
            // otherwise RightShift+E would count as Primary as well.
            AssertBoundTo(_actions.Spirit.Boost, "<Keyboard>/leftShift");
            AssertBoundTo(_actions.Spirit.Boost, "<Keyboard>/rightShift");
        }

        [Test]
        public void Spirit_PossessAsGood_IsShiftPlusE_OneModifierComposite()
        {
            var bindings = _actions.Spirit.PossessAsGood.bindings;
            Assert.AreEqual(3, bindings.Count, "composite + modifier + binding");

            Assert.IsTrue(bindings[0].isComposite);
            Assert.IsFalse(bindings[0].isPartOfComposite);
            Assert.AreEqual("OneModifier", bindings[0].path);

            Assert.IsTrue(bindings[1].isPartOfComposite);
            Assert.AreEqual("modifier", bindings[1].name);
            Assert.AreEqual("<Keyboard>/shift", bindings[1].path);

            Assert.IsTrue(bindings[2].isPartOfComposite);
            Assert.AreEqual("binding", bindings[2].name);
            Assert.AreEqual("<Keyboard>/e", bindings[2].path);
        }

        [Test]
        public void Spirit_RemovedActions_AreGone()
        {
            AssertMissing(_actions, "Spirit/Bless");
        }

        [Test]
        public void IsPrimaryAllowed_FalseWhileBoostHeld()
        {
            Assert.IsTrue(LocalInputSource.IsPrimaryAllowed(false), "plain E is Primary");
            Assert.IsFalse(LocalInputSource.IsPrimaryAllowed(true), "Shift+E is PossessAsGood, never Primary");
        }

        // --- Debug ---

        [Test]
        public void Debug_F11_IsCompleteStage()
        {
            AssertBoundTo(_actions.Debug.CompleteStage, "<Keyboard>/f11");
            AssertMissing(_actions, "Debug/AllFragments");
        }
    }
}
