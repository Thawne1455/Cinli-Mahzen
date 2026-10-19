using System.Collections.Generic;
using System.Reflection;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using CinliMahzen.Objectives;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.Objectives
{
    /// <summary>
    /// TODO 2.5: the scene side of the Portraits puzzle — <see cref="PuzzleNetIds"/>, <see cref="PuzzlePartDisplay"/>, <see cref="PuzzleView"/>
    /// (bind, redraw on the broadcast events, parts and lever as interactables on the authority) and the Spirit Veil of
    /// <see cref="PuzzleReference"/>. Builds small GameObject hierarchies (no prefab) and wires the private fields by reflection.
    /// The view uses the shared <see cref="PuzzleSystemFixture"/> world (slot 0: 3 × 4, solution { 1, 3, 0 }).
    /// </summary>
    public class PaintingsPuzzleViewTests : PuzzleSystemFixture
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void DestroyObjects()
        {
            foreach (GameObject go in _objects)
                Object.DestroyImmediate(go);
            _objects.Clear();
        }

        private static void Set(object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(info, target.GetType().Name + "." + field + " missing");
            info.SetValue(target, value);
        }

        private GameObject New(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null)
                go.transform.SetParent(parent, false);
            else
                _objects.Add(go);
            return go;
        }

        private PuzzlePartDisplay NewDisplay(Transform parent, PuzzlePartDisplay.Mode mode, int children = 0)
        {
            GameObject go = New("Display", parent);
            for (int i = 0; i < children; i++)
                New("Child" + i, go.transform);
            var display = go.AddComponent<PuzzlePartDisplay>();
            Set(display, "mode", mode);
            return display;
        }

        private PuzzleView NewView(int partCount, out PuzzlePart[] parts, out ConfirmLever lever)
        {
            GameObject root = New("View");
            parts = new PuzzlePart[partCount];
            for (int i = 0; i < partCount; i++)
            {
                GameObject frame = New("Frame" + i, root.transform);
                PuzzlePartDisplay display = NewDisplay(frame.transform, PuzzlePartDisplay.Mode.RotateRoll);
                parts[i] = frame.AddComponent<PuzzlePart>();
                Set(parts[i], "display", display);
            }
            lever = New("Lever", root.transform).AddComponent<ConfirmLever>();
            var view = root.AddComponent<PuzzleView>();
            Set(view, "parts", parts);
            Set(view, "lever", lever);
            return view;
        }

        private static float Roll(PuzzlePart part)
        {
            return Mathf.Round(Mathf.DeltaAngle(0f, part.Display.transform.localEulerAngles.z));
        }

        // ---------- NetIds ----------

        [Test]
        public void NetIds_AreUniquePerPuzzle_AndStayBetweenHouseMarkersAndPossessables()
        {
            var seen = new HashSet<int>();
            for (int ordinal = 0; ordinal < PuzzleNetIds.MaxPuzzles; ordinal++)
            {
                for (int part = 0; part < PuzzleNetIds.MaxParts; part++)
                    Assert.IsTrue(seen.Add(PuzzleNetIds.Part(ordinal, part).Value));
                Assert.IsTrue(seen.Add(PuzzleNetIds.Lever(ordinal).Value));
                Assert.IsTrue(seen.Add(PuzzleNetIds.ScrambleTarget(ordinal).Value));
            }
            foreach (int id in seen)
            {
                Assert.GreaterOrEqual(id, PuzzleNetIds.Base);
                Assert.Less(id, CinliMahzen.Possession.PossessionSystem.LevelNetIdBase, "must not reach the possessable ids");
            }
            Assert.IsFalse(PuzzleNetIds.IsValidOrdinal(-1));
            Assert.IsFalse(PuzzleNetIds.IsValidOrdinal(PuzzleNetIds.MaxPuzzles));
        }

        // ---------- display ----------

        [Test]
        public void RotateRoll_TurnsClockwise90PerValue()
        {
            PuzzlePartDisplay display = NewDisplay(New("Root").transform, PuzzlePartDisplay.Mode.RotateRoll);

            display.Show(1);
            Assert.AreEqual(-90f, Mathf.Round(Mathf.DeltaAngle(0f, display.transform.localEulerAngles.z)));
            display.Show(2);
            Assert.AreEqual(180f, Mathf.Abs(Mathf.Round(Mathf.DeltaAngle(0f, display.transform.localEulerAngles.z))));
            display.Show(0);
            Assert.AreEqual(0f, Mathf.Round(Mathf.DeltaAngle(0f, display.transform.localEulerAngles.z)), "back to the rest pose");
            Assert.AreEqual(0, display.Shown);
        }

        [Test]
        public void ActivateChild_ShowsOnlyTheValuesChild()
        {
            PuzzlePartDisplay display = NewDisplay(New("Root").transform, PuzzlePartDisplay.Mode.ActivateChild, 3);

            display.Show(2);

            Assert.IsFalse(display.transform.GetChild(0).gameObject.activeSelf);
            Assert.IsFalse(display.transform.GetChild(1).gameObject.activeSelf);
            Assert.IsTrue(display.transform.GetChild(2).gameObject.activeSelf);
        }

        // ---------- view ----------

        [Test]
        public void Bind_DrawsTheSolvedStart_AndGivesDeterministicIds()
        {
            PuzzleView view = NewView(3, out PuzzlePart[] parts, out ConfirmLever lever);

            Assert.IsTrue(view.Bind(Puzzles, Slot, 2));

            Assert.AreEqual(-90f, Roll(parts[0]), "solution part 0 = 1");
            Assert.AreEqual(90f, Roll(parts[1]), "solution part 1 = 3 (Left)");
            Assert.AreEqual(0f, Roll(parts[2]));
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(PuzzleNetIds.Part(2, i), parts[i].NetId);
                Assert.AreEqual(i, parts[i].Index);
            }
            Assert.AreEqual(PuzzleNetIds.Lever(2), lever.NetId);
        }

        [Test]
        public void Bind_RefusesAWrongPartCount_OrAnUnknownSlot()
        {
            PuzzleView two = NewView(2, out _, out _);
            Assert.IsFalse(two.Bind(Puzzles, Slot, 0));
            Assert.IsFalse(two.IsBound);

            PuzzleView three = NewView(3, out _, out _);
            Assert.IsFalse(three.Bind(Puzzles, 99, 0));
            Assert.IsFalse(three.Bind(Puzzles, Slot, PuzzleNetIds.MaxPuzzles));
        }

        [Test]
        public void HumanTurningAPart_ThroughItsInteractable_RedrawsTheView()
        {
            PuzzleView view = NewView(3, out PuzzlePart[] parts, out _);
            Assert.IsTrue(view.Bind(Puzzles, Slot, 0));

            Assert.IsTrue(parts[0].CanInteract(HumanId, Role.Human));
            Assert.IsFalse(parts[0].CanInteract(EvilId, Role.EvilJinn), "only the human turns portraits");
            Assert.IsTrue(parts[0].ValidateAuthority(HumanId));
            parts[0].ExecuteAuthority(HumanId);

            CollectionAssert.AreEqual(new[] { 2, 3, 0 }, Values());
            Assert.AreEqual(180f, Mathf.Abs(Roll(parts[0])), "redrawn from the PuzzleStateChanged event");
        }

        [Test]
        public void EvilJinnRunningAPartsExecute_ChangesNothing()
        {
            PuzzleView view = NewView(3, out PuzzlePart[] parts, out _);
            Assert.IsTrue(view.Bind(Puzzles, Slot, 0));

            parts[1].ExecuteAuthority(EvilId);

            CollectionAssert.AreEqual(new[] { 1, 3, 0 }, Values(), "the core's role rule refuses it");
        }

        [Test]
        public void Scramble_RedrawsEveryPart()
        {
            PuzzleView view = NewView(3, out PuzzlePart[] parts, out _);
            Assert.IsTrue(view.Bind(Puzzles, Slot, 0));

            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessScrambleAuthority(EvilId, Slot));

            int[] values = Values();
            for (int i = 0; i < 3; i++)
                Assert.AreEqual(values[i], parts[i].Display.Shown, "part " + i);
        }

        [Test]
        public void Lever_ConfirmsASolvedPuzzle_AndThePartsLock()
        {
            PuzzleView view = NewView(3, out PuzzlePart[] parts, out ConfirmLever lever);
            Assert.IsTrue(view.Bind(Puzzles, Slot, 0));

            Assert.IsTrue(lever.CanInteract(HumanId, Role.Human));
            lever.ExecuteAuthority(HumanId);

            Assert.IsTrue(view.IsLocked);
            Assert.AreEqual(1, CompletedEvents.Count);
            Assert.IsFalse(parts[0].CanInteract(HumanId, Role.Human), "a completed puzzle cannot be turned");
            Assert.IsFalse(lever.CanInteract(HumanId, Role.Human));
        }

        [Test]
        public void Lever_OnAWrongAnswer_Shocks()
        {
            PuzzleView view = NewView(3, out PuzzlePart[] parts, out ConfirmLever lever);
            Assert.IsTrue(view.Bind(Puzzles, Slot, 0));
            parts[2].ExecuteAuthority(HumanId);
            ClearRecordings();

            lever.ExecuteAuthority(HumanId);

            Assert.IsFalse(view.IsLocked);
            Assert.AreEqual(1, HumanBody.Hits.Count);
        }

        [Test]
        public void Unbind_StopsRedrawing()
        {
            PuzzleView view = NewView(3, out PuzzlePart[] parts, out _);
            Assert.IsTrue(view.Bind(Puzzles, Slot, 0));
            view.Unbind();

            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessOpAuthority(HumanId, Slot, PuzzleOp.Cycle(0, 1)));

            Assert.AreEqual(1, parts[0].Display.Shown, "still the value drawn before Unbind");
            Assert.IsFalse(view.IsBound);
        }

        // ---------- reference / Spirit Veil ----------

        private PuzzleReference NewReference(out GameObject content, out GameObject veil, out PuzzlePartDisplay[] displays)
        {
            GameObject root = New("Reference");
            content = New("Content", root.transform);
            veil = New("Veil", root.transform);
            displays = new PuzzlePartDisplay[3];
            for (int i = 0; i < 3; i++)
                displays[i] = NewDisplay(content.transform, PuzzlePartDisplay.Mode.RotateRoll);
            var reference = root.AddComponent<PuzzleReference>();
            Set(reference, "parts", displays);
            Set(reference, "content", content);
            Set(reference, "veil", veil);
            return reference;
        }

        private void UseLocal(PlayerId who)
        {
            LocalPlayer = who;
            GameServices.Register<INetBridge>(Bridge);
            GameServices.Register<IPlayerRegistry>(Players);
        }

        [Test]
        public void Reference_DrawsTheAnswer_ButStaysVeiled_UntilTheLocalGoodJinnFocuses()
        {
            UseLocal(GoodJinnId);
            PuzzleReference reference = NewReference(out GameObject content, out GameObject veil, out PuzzlePartDisplay[] displays);

            Assert.IsTrue(reference.Bind(PaintingsSolution, 0, null));
            Assert.AreEqual(1, displays[0].Shown);
            Assert.AreEqual(3, displays[1].Shown);
            Assert.AreEqual(0, displays[2].Shown);
            Assert.IsFalse(reference.IsRevealed);
            Assert.IsFalse(content.activeSelf);
            Assert.IsTrue(veil.activeSelf);

            EventBus.Publish(new ReferenceFocusChangedEvt(GoodJinnId, true));
            Assert.IsTrue(reference.IsRevealed);
            Assert.IsTrue(content.activeSelf);
            Assert.IsFalse(veil.activeSelf);

            EventBus.Publish(new ReferenceFocusChangedEvt(GoodJinnId, false));
            Assert.IsFalse(reference.IsRevealed, "letting go of the right mouse button veils it again");
        }

        [Test]
        public void Reference_StaysVeiled_ForAnEvilJinn_EvenWhileTheGoodJinnFocuses()
        {
            UseLocal(EvilId);
            PuzzleReference reference = NewReference(out _, out GameObject veil, out _);
            Assert.IsTrue(reference.Bind(PaintingsSolution, 0, null));

            EventBus.Publish(new ReferenceFocusChangedEvt(GoodJinnId, true));

            Assert.IsFalse(reference.IsRevealed);
            Assert.IsTrue(veil.activeSelf);
        }

        [Test]
        public void Reference_RefusesAnAnswerOfTheWrongLength()
        {
            UseLocal(GoodJinnId);
            PuzzleReference reference = NewReference(out _, out _, out _);
            Assert.IsFalse(reference.Bind(new[] { 1, 2 }, 0, null));
        }

        [Test]
        public void Reference_IsRotatedByTheReferenceRotation()
        {
            UseLocal(GoodJinnId);
            PuzzleReference reference = NewReference(out GameObject content, out _, out _);

            Assert.IsTrue(reference.Bind(PaintingsSolution, 90, null));

            Assert.AreEqual(-90f, Mathf.Round(Mathf.DeltaAngle(0f, content.transform.localEulerAngles.z)));
        }
    }
}
