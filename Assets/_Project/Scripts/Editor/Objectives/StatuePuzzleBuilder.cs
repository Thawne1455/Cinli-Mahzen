using CinliMahzen.Objectives;
using UnityEditor;
using UnityEngine;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// TODO 3.1: <c>CinliMahzen/Setup/Build Statue Puzzle Prefabs</c> — the graybox P2 "Heykelin Nazarlığı" (floor slot, +Z into the room).
    /// <list type="bullet">
    /// <item><c>P_Puzzle_Statue</c>: a statue on a pedestal surrounded by the 6 objects (Candle, Skull, Cat, Ewer, Sandal, Mirror) every 60°.
    /// Part 0 = the head (<see cref="PuzzlePartDisplay.Mode.RotateYaw"/>, 60° per value: it looks at object <c>value</c>); part 1 = the staff
    /// (<see cref="PuzzlePartDisplay.Mode.RotatePitch"/>: Up / Middle / Down, 30° apart). The human presses E on the head or on the staff
    /// (two colliders, so no Shift is needed). Lever on a post at the side.</item>
    /// <item><c>P_PuzzleRef_Statue</c> (SpiritOnly): the statue's dark "shadow" with the same head / staff displays and the 6 objects, behind a veil.</item>
    /// </list>
    /// </summary>
    public static class StatuePuzzleBuilder
    {
        public const string PuzzlePrefabPath = PuzzlePrefabKit.PrefabFolder + "/P_Puzzle_Statue.prefab";
        public const string ReferencePrefabPath = PuzzlePrefabKit.PrefabFolder + "/P_PuzzleRef_Statue.prefab";

        private const float ObjectRadius = 1.0f;
        private const float HeadStep = 60f;
        private const float StaffBase = -30f;
        private const float StaffStep = 30f;

        private static readonly string[] ObjectNames = { "Candle", "Skull", "Cat", "Ewer", "Sandal", "Mirror" };
        private static readonly Color[] ObjectColors =
        {
            new Color(0.95f, 0.95f, 0.85f), new Color(0.85f, 0.8f, 0.7f), new Color(0.1f, 0.1f, 0.1f),
            new Color(0.75f, 0.45f, 0.2f), new Color(0.45f, 0.3f, 0.15f), new Color(0.6f, 0.8f, 0.95f),
        };

        [MenuItem("CinliMahzen/Setup/Build Statue Puzzle Prefabs")]
        public static void Build()
        {
            if (!PuzzlePrefabKit.CheckNotPlaying("Build Statue Puzzle Prefabs"))
                return;

            Material stone = PuzzlePrefabKit.Material("M_Puzzle_Stone", new Color(0.55f, 0.55f, 0.5f));
            Material shadow = PuzzlePrefabKit.Material("M_Puzzle_Shadow", new Color(0.15f, 0.1f, 0.25f));
            Material marker = PuzzlePrefabKit.Material("M_Puzzle_Marker", new Color(0.05f, 0.05f, 0.05f));
            Material wood = PuzzlePrefabKit.Material("M_Puzzle_Frame", new Color(0.25f, 0.15f, 0.08f));
            var objects = new Material[ObjectNames.Length];
            for (int i = 0; i < ObjectNames.Length; i++)
                objects[i] = PuzzlePrefabKit.Material("M_Puzzle_Statue_" + ObjectNames[i], ObjectColors[i]);

            // ---- interactive ----
            var root = new GameObject("P_Puzzle_Statue");
            GameObject body = PuzzlePrefabKit.Empty("Body", root.transform, Vector3.zero);
            PuzzlePrefabKit.Cube("Pedestal", body.transform, new Vector3(0f, 0.4f, 0f), new Vector3(0.6f, 0.8f, 0.6f), stone);
            PuzzlePrefabKit.Cube("Torso", body.transform, new Vector3(0f, 1.2f, 0f), new Vector3(0.4f, 0.8f, 0.3f), stone);
            AddObjects(body.transform, objects);

            GameObject head = PuzzlePrefabKit.Interactable("Head", body.transform, new Vector3(0f, 1.8f, 0f), new Vector3(0.4f, 0.4f, 0.4f));
            Transform headPivot = HeadVisual(head.transform, stone, marker);
            PuzzlePart headPart = PuzzlePrefabKit.Part(head, PuzzlePrefabKit.Display(head, headPivot, PuzzlePartDisplay.Mode.RotateYaw, HeadStep));

            GameObject staff = PuzzlePrefabKit.Interactable("Staff", body.transform, new Vector3(0.35f, 1.2f, 0f), new Vector3(0.25f, 1.1f, 0.25f));
            Transform staffPivot = StaffVisual(staff.transform, wood);
            PuzzlePart staffPart = PuzzlePrefabKit.Part(staff, PuzzlePrefabKit.Display(staff, staffPivot, PuzzlePartDisplay.Mode.RotatePitch, StaffStep, StaffBase));

            GameObject post = PuzzlePrefabKit.Empty("LeverPost", root.transform, new Vector3(1.4f, 0f, 0.4f));
            PuzzlePrefabKit.Cube("Post", post.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.1f, 0.9f, 0.1f), wood);
            ConfirmLever lever = PuzzlePrefabKit.Lever(post.transform, new Vector3(0f, 0.95f, 0.05f), wood, marker);
            PuzzlePrefabKit.View(root, new[] { headPart, staffPart }, lever, body.transform);

            // ---- reference (the shadow) ----
            var reference = new GameObject("P_PuzzleRef_Statue");
            GameObject content = PuzzlePrefabKit.Empty("Content", reference.transform, Vector3.zero);
            PuzzlePrefabKit.Cube("Torso", content.transform, new Vector3(0f, 1.2f, 0f), new Vector3(0.4f, 0.8f, 0.3f), shadow);
            AddObjects(content.transform, objects);
            GameObject refHead = PuzzlePrefabKit.Empty("Head", content.transform, new Vector3(0f, 1.8f, 0f));
            PuzzlePartDisplay refHeadDisplay = PuzzlePrefabKit.Display(refHead, HeadVisual(refHead.transform, shadow, marker), PuzzlePartDisplay.Mode.RotateYaw, HeadStep);
            GameObject refStaff = PuzzlePrefabKit.Empty("Staff", content.transform, new Vector3(0.35f, 1.2f, 0f));
            PuzzlePartDisplay refStaffDisplay = PuzzlePrefabKit.Display(refStaff, StaffVisual(refStaff.transform, shadow), PuzzlePartDisplay.Mode.RotatePitch, StaffStep, StaffBase);
            GameObject veil = PuzzlePrefabKit.Cube("Veil", reference.transform, new Vector3(0f, 1.1f, 0f), new Vector3(2.4f, 2.2f, 2.4f), PuzzlePrefabKit.Veil());
            PuzzlePrefabKit.Reference(reference, new[] { refHeadDisplay, refStaffDisplay }, content, veil);
            PuzzlePrefabKit.MakeSpiritOnly(reference);

            PuzzlePrefabKit.SaveAndLink(root, PuzzlePrefabPath, reference, ReferencePrefabPath, "PZ_Statue");
            Debug.Log("[Setup] Build Statue Puzzle Prefabs: done");
        }

        /// <summary>The 6 objects on the floor around the statue; object i sits at yaw i × 60° (where the head looks at value i).</summary>
        private static void AddObjects(Transform parent, Material[] mats)
        {
            for (int i = 0; i < ObjectNames.Length; i++)
            {
                Vector3 at = Quaternion.Euler(0f, i * HeadStep, 0f) * Vector3.forward * ObjectRadius;
                PuzzlePrefabKit.Cube("Obj_" + ObjectNames[i], parent, at + Vector3.up * 0.15f, new Vector3(0.18f, 0.3f, 0.18f), mats[i]);
            }
        }

        /// <summary>A head with a nose pointing along local +Z (the direction it looks).</summary>
        private static Transform HeadVisual(Transform parent, Material skin, Material nose)
        {
            GameObject pivot = PuzzlePrefabKit.Empty("HeadPivot", parent, Vector3.zero);
            PuzzlePrefabKit.Cube("Skull", pivot.transform, Vector3.zero, new Vector3(0.3f, 0.3f, 0.3f), skin);
            PuzzlePrefabKit.Cube("Nose", pivot.transform, new Vector3(0f, 0f, 0.2f), new Vector3(0.08f, 0.08f, 0.15f), nose);
            return pivot.transform;
        }

        /// <summary>A staff pivoting at the statue's hand; pitch tilts its top back (Up) or forward (Down).</summary>
        private static Transform StaffVisual(Transform parent, Material mat)
        {
            GameObject pivot = PuzzlePrefabKit.Empty("StaffPivot", parent, Vector3.zero);
            PuzzlePrefabKit.Cube("Stick", pivot.transform, new Vector3(0f, 0.3f, 0f), new Vector3(0.05f, 1.0f, 0.05f), mat);
            return pivot.transform;
        }
    }
}
