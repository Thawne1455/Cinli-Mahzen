using System.Collections.Generic;
using CinliMahzen.World;
using CinliMahzen.World.Graybox;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// TODO 1.8 / 4.1: <c>CinliMahzen/Setup/Add Test Possessables To House In Open Scene</c> — a first furnishing pass until the hand-placed
    /// furniture (🖐 4.1). This puts <c>P_Poss_*</c> instances (Build Possessable Prefabs first) on the free corners of every indoor RoomMarker box
    /// (never in front of a door or stairs mouth, never on a flight or over a stair hole): one Mother's Slipper per floor, one Lemon Cologne (the
    /// round plan moves it to its CologneSpawnSpot every round) and up to three items from a rotating list per room. Everything goes under a
    /// "TestPossessables" child of the HouseService; running it again rebuilds that group. Marks the scene dirty; save it afterwards.
    /// </summary>
    public static class TestPossessablesSetup
    {
        private const string GroupName = "TestPossessables";
        private const float CornerInset = 1.2f;
        private const float ClearOfOpenings = 2.2f;

        private static readonly string[] Rotation = { "Chair", "Bottle", "Shelf", "Radio", "Barrel", "Stool", "Plate", "Table", "Candle", "Chest" };

        [MenuItem("CinliMahzen/Setup/Add Test Possessables To House In Open Scene")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[SetupTestPossessables] Stop play mode first");
                return;
            }
            HouseService house = Object.FindFirstObjectByType<HouseService>(FindObjectsInactive.Include);
            if (house == null)
            {
                Debug.LogError("[SetupTestPossessables] No HouseService in the open scene");
                return;
            }

            Transform old = house.transform.Find(GroupName);
            if (old != null)
                Undo.DestroyObjectImmediate(old.gameObject);
            var group = new GameObject(GroupName);
            Undo.RegisterCreatedObjectUndo(group, "Add test possessables");
            group.transform.SetParent(house.transform, false);

            var rooms = new List<RoomMarker>(house.GetComponentsInChildren<RoomMarker>(true));
            rooms.Sort((a, b) => a.RoomId.CompareTo(b.RoomId));
            var boxes = new List<Bounds>(4);
            var floorsWithSlipper = new HashSet<int>();
            bool cologne = false;
            int next = 0;
            int placed = 0;
            foreach (RoomMarker room in rooms)
            {
                if (room.IsOutdoor)
                    continue;
                boxes.Clear();
                room.CollectWorldBoxes(boxes);
                // Corners of every box of the room, minus the ones that would block a doorway or stand on / beside stairs.
                var corners = new List<Vector3>();
                for (int bi = 0; bi < boxes.Count; bi++)
                {
                    Bounds b = boxes[bi];
                    float floorY = room.Floor * GrayboxLayout.FloorHeight;
                    float dx = Mathf.Max(0f, b.extents.x - CornerInset);
                    float dz = Mathf.Max(0f, b.extents.z - CornerInset);
                    foreach (Vector3 corner in new[]
                             {
                                 new Vector3(b.center.x - dx, floorY, b.center.z - dz), new Vector3(b.center.x + dx, floorY, b.center.z - dz),
                                 new Vector3(b.center.x - dx, floorY, b.center.z + dz), new Vector3(b.center.x + dx, floorY, b.center.z + dz),
                             })
                    {
                        if (IsClear(corner, room.Floor))
                            corners.Add(corner);
                    }
                }
                if (corners.Count == 0)
                    continue;

                // The slipper of the floor (or the one cologne) first, then up to three items from the rotation.
                int c = 0;
                if (!floorsWithSlipper.Contains(room.Floor))
                {
                    floorsWithSlipper.Add(room.Floor);
                    placed += Place(group.transform, "Slipper", corners[c++], room.RoomId) ? 1 : 0;
                }
                else if (!cologne && room.Floor != GrayboxLayout.CellarFloor)
                {
                    cologne = Place(group.transform, "Cologne", corners[c++], room.RoomId);
                }
                for (int i = 0; i < 3 && c < corners.Count; i++)
                {
                    placed += Place(group.transform, Rotation[next % Rotation.Length], corners[c++], room.RoomId) ? 1 : 0;
                    next++;
                }
            }
            if (cologne)
                placed++;

            EditorSceneManager.MarkSceneDirty(house.gameObject.scene);
            Debug.Log("[SetupTestPossessables] " + placed + " test possessables under '" + house.name + "/" + GroupName + "'" + (cologne ? "" : " (no cologne - no second room on a floor)")
                + "; scene is dirty - save it");
        }

        /// <summary>
        /// False when an object at <paramref name="p"/> (floor <paramref name="floor"/>) would block a door or a stairs mouth
        /// (<see cref="ClearOfOpenings"/> metres) or stand on a flight or over a stair hole (from <see cref="GrayboxLayout.Default"/>,
        /// the layout P_House is built from).
        /// </summary>
        private static bool IsClear(Vector3 p, int floor)
        {
            GrayboxLayout layout = GrayboxLayout.Default;
            var cell = new GrayboxCell(Mathf.FloorToInt(p.x / GrayboxLayout.CellSize), Mathf.FloorToInt(p.z / GrayboxLayout.CellSize));
            for (int i = 0; i < layout.Doors.Count; i++)
            {
                if (layout.Doors[i].Floor == floor && Near(p, layout.Doors[i].Edge))
                    return false;
            }
            for (int i = 0; i < layout.Stairs.Count; i++)
            {
                GrayboxStairs stairs = layout.Stairs[i];
                if ((stairs.BaseFloor == floor || stairs.TopFloor == floor) && stairs.Contains(cell))
                    return false;
                if ((stairs.BaseFloor == floor && Near(p, stairs.EntranceEdge)) || (stairs.TopFloor == floor && Near(p, stairs.ArrivalEdge)))
                    return false;
            }
            return true;
        }

        private static bool Near(Vector3 p, GrayboxEdge edge)
        {
            float x = edge.CenterCellX * GrayboxLayout.CellSize;
            float z = edge.CenterCellZ * GrayboxLayout.CellSize;
            return new Vector2(p.x - x, p.z - z).magnitude < ClearOfOpenings;
        }

        private static bool Place(Transform parent, string name, Vector3 position, int roomId)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PossessablePrefabsBuilder.PathOf(name));
            if (prefab == null)
            {
                Debug.LogError("[SetupTestPossessables] " + PossessablePrefabsBuilder.PathOf(name) + " missing - run CinliMahzen/Setup/Build Possessable Prefabs");
                return false;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = prefab.name + "_R" + roomId;
            go.transform.position = position;
            return true;
        }
    }
}
