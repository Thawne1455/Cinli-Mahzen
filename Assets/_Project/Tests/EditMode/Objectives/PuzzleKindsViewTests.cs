using System.Collections.Generic;
using System.Reflection;
using CinliMahzen.Core;
using CinliMahzen.Objectives;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.Objectives
{
    /// <summary>
    /// TODO 3.1–3.3: the scene pieces the Statue, Wires and Sigil puzzles add — yaw / pitch displays, the Toggle part (paint / wipe a seal line,
    /// gated by the paint item), the trap pictures and the composed reference rotation.
    /// </summary>
    public class PuzzleKindsViewTests : PuzzleSystemFixture
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

        private PuzzlePartDisplay Display(Transform parent, PuzzlePartDisplay.Mode mode, float step = 90f, float baseAngle = 0f, int children = 0)
        {
            GameObject go = New("Display", parent);
            for (int i = 0; i < children; i++)
                New("Child" + i, go.transform);
            var display = go.AddComponent<PuzzlePartDisplay>();
            Set(display, "mode", mode);
            Set(display, "degreesPerValue", step);
            Set(display, "baseAngle", baseAngle);
            return display;
        }

        [Test]
        public void RotateYaw_TurnsTheStatueHead60PerValue()
        {
            PuzzlePartDisplay head = Display(New("Root").transform, PuzzlePartDisplay.Mode.RotateYaw, 60f);
            head.Show(2);
            Assert.AreEqual(120f, Mathf.Round(head.transform.localEulerAngles.y));
        }

        [Test]
        public void RotatePitch_TiltsTheStaffFromItsBaseAngle()
        {
            PuzzlePartDisplay staff = Display(New("Root").transform, PuzzlePartDisplay.Mode.RotatePitch, 30f, -30f);
            staff.Show(0);
            Assert.AreEqual(-30f, Mathf.Round(Mathf.DeltaAngle(0f, staff.transform.localEulerAngles.x)), "Up");
            staff.Show(2);
            Assert.AreEqual(30f, Mathf.Round(Mathf.DeltaAngle(0f, staff.transform.localEulerAngles.x)), "Down");
        }

        [Test]
        public void TogglePart_PaintsAndWipesALine_OnlyWithPaint()
        {
            Runtime = NewPaintedPuzzle(Vector3.zero);
            Puzzles.RegisterSlot(5, Runtime);

            GameObject root = New("Sigil");
            var parts = new PuzzlePart[2];
            for (int i = 0; i < 2; i++)
            {
                GameObject line = New("Line" + i, root.transform);
                PuzzlePartDisplay display = Display(line.transform, PuzzlePartDisplay.Mode.ActivateChild, children: 2);
                parts[i] = line.AddComponent<PuzzlePart>();
                Set(parts[i], "display", display);
                Set(parts[i], "op", PuzzlePart.PartOp.Toggle);
            }
            var view = root.AddComponent<PuzzleView>();
            Set(view, "parts", parts);
            Assert.IsTrue(view.Bind(Puzzles, 5, 1));

            Assert.AreEqual(PuzzleOpKind.Set, parts[0].BuildOp().Kind);
            Assert.AreEqual(0, parts[0].BuildOp().Value, "solution { 1, 0 }: part 0 painted → next is empty");
            Assert.AreEqual(1, parts[1].BuildOp().Value);

            parts[0].ExecuteAuthority(HumanId);
            Assert.AreEqual(1, Runtime.State.Get(0), "no paint: refused by the item rule");

            Inventory.Give(HumanId, "paint");
            parts[0].ExecuteAuthority(HumanId);
            Assert.AreEqual(0, Runtime.State.Get(0));
            Assert.AreEqual(0, parts[0].Display.Shown);
            parts[0].ExecuteAuthority(HumanId);
            Assert.AreEqual(1, Runtime.State.Get(0), "pressed again: painted back");
        }

        [Test]
        public void Reference_ShowsTrapPictures_AndKeepsItsRootsOwnTilt()
        {
            LocalPlayer = GoodJinnId;
            GameServices.Register<CinliMahzen.Core.Net.INetBridge>(Bridge);
            GameServices.Register<IPlayerRegistry>(Players);

            GameObject root = New("Reference");
            GameObject content = New("Content", root.transform);
            GameObject tilted = New("RotationRoot", content.transform);
            tilted.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var answers = new[] { Display(tilted.transform, PuzzlePartDisplay.Mode.ActivateChild, children: 6) };
            var traps = new[]
            {
                Display(content.transform, PuzzlePartDisplay.Mode.ActivateChild, children: 6),
                Display(content.transform, PuzzlePartDisplay.Mode.ActivateChild, children: 6),
            };
            var reference = root.AddComponent<PuzzleReference>();
            Set(reference, "parts", answers);
            Set(reference, "content", content);
            Set(reference, "rotationRoot", tilted.transform);
            Set(reference, "trapDisplays", traps);

            Assert.IsTrue(reference.Bind(new[] { 3 }, 90, new[] { 1, 4 }));

            Assert.AreEqual(1, traps[0].Shown);
            Assert.AreEqual(4, traps[1].Shown);
            Quaternion expected = Quaternion.Euler(-90f, 0f, 0f) * Quaternion.Euler(0f, 0f, -90f);
            Assert.Less(Quaternion.Angle(expected, tilted.transform.localRotation), 0.01f, "rotation composed with the tilt");
        }

        [Test]
        public void Reference_HidesTrapPictures_WhenThereAreNoTraps()
        {
            GameObject root = New("Reference");
            GameObject content = New("Content", root.transform);
            var answers = new[] { Display(content.transform, PuzzlePartDisplay.Mode.ActivateChild, children: 6) };
            var traps = new[] { Display(content.transform, PuzzlePartDisplay.Mode.ActivateChild, children: 6) };
            var reference = root.AddComponent<PuzzleReference>();
            Set(reference, "parts", answers);
            Set(reference, "content", content);
            Set(reference, "trapDisplays", traps);

            Assert.IsTrue(reference.Bind(new[] { 2 }, 0, null));
            Assert.IsFalse(traps[0].gameObject.activeSelf);
        }
    }
}
