using System.Collections.Generic;
using CinliMahzen.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// TODO 1.8 / 4.1: <c>CinliMahzen/Setup/Add Test Possessables To House In Open Scene</c> — until the furnished house (4.1) the graybox has no
    /// objects to possess besides its lamps. This puts <c>P_Poss_*</c> instances (Build Possessable Prefabs first) into every indoor RoomMarker:
    /// three items from a rotating list near the room's corners, one Mother's Slipper per floor, and one Lemon Cologne in the first room (the round
    /// plan moves it to its CologneSpawnSpot every round). Everything goes under a "TestPossessables" child of the HouseService; running it again
    /// rebuilds that group. Marks the scene dirty; save it afterwards.
    /// </summary>
    public static class TestPossessablesSetup
    {
        private const string GroupName = "TestPossessables";
        private const float CornerInset = 1.2f;

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
                if (boxes.Count == 0)
                    continue;
                Bounds b = boxes[0];
                float floorY = b.min.y;
                float dx = Mathf.Max(0f, b.extents.x - CornerInset);
                float dz = Mathf.Max(0f, b.extents.z - CornerInset);
                var corners = new[]
                {
                    new Vector3(b.center.x - dx, floorY, b.center.z - dz),
                    new Vector3(b.center.x + dx, floorY, b.center.z - dz),
                    new Vector3(b.center.x - dx, floorY, b.center.z + dz),
                    new Vector3(b.center.x + dx, floorY, b.center.z + dz),
                };

                for (int i = 0; i < 3; i++)
                {
                    placed += Place(group.transform, Rotation[next % Rotation.Length], corners[i], room.RoomId) ? 1 : 0;
                    next++;
                }
                if (floorsWithSlipper.Add(room.Floor))
                    placed += Place(group.transform, "Slipper", corners[3], room.RoomId) ? 1 : 0;
                else if (!cologne)
                    cologne = Place(group.transform, "Cologne", corners[3], room.RoomId);
            }
            if (cologne)
                placed++;

            EditorSceneManager.MarkSceneDirty(house.gameObject.scene);
            Debug.Log("[SetupTestPossessables] " + placed + " test possessables under '" + house.name + "/" + GroupName + "'" + (cologne ? "" : " (no cologne - no second room on a floor)")
                + "; scene is dirty - save it");
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
