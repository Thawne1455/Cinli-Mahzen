using CinliMahzen.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// TODO 3.7: <c>CinliMahzen/Setup/Add Test Dig Spots To House In Open Scene</c> — the graybox house has no cellar yet (TODO 4.1), so this builds
    /// a stand-in under the garden: a "TestCellar" group under the open scene's HouseService with a cellar RoomMarker (floor
    /// <see cref="HouseContract.CellarFloor"/>, next free room id), a floor slab on the Environment layer and three <see cref="DigSpot"/> markers 2 m apart, so the round plan picks a real dig site and the dig can be played
    /// (reach it by teleport). Running it again rebuilds the group (and removes the old garden "TestDigSpot_*" markers). Marks the scene dirty.
    /// Remove it when the real cellar arrives.
    /// </summary>
    public static class TestDigSpotsSetup
    {
        private const string GroupName = "TestCellar";
        private const string OldNamePrefix = "TestDigSpot_";
        private const string RoomKey = "cellar";
        private const string MarkerLayerName = "Ignore Raycast";
        private const string FloorLayerName = "Environment";
        private const int Count = 3;
        private const float Spacing = 2f;
        private static readonly Vector3 RoomCenter = new Vector3(2f, -2f, -6f);
        private static readonly Vector3 RoomSize = new Vector3(8f, 4f, 4f);

        [MenuItem("CinliMahzen/Setup/Add Test Dig Spots To House In Open Scene")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[SetupDig] Stop play mode first");
                return;
            }
            HouseService house = Object.FindFirstObjectByType<HouseService>(FindObjectsInactive.Include);
            if (house == null)
            {
                Debug.LogError("[SetupDig] No HouseService in the open scene (CinliMahzen/House/Place House In Open Scene first)");
                return;
            }

            for (int i = house.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = house.transform.GetChild(i);
                if (child.name == GroupName || child.name.StartsWith(OldNamePrefix))
                    Undo.DestroyObjectImmediate(child.gameObject);
            }

            int roomId = 0;
            foreach (RoomMarker room in house.GetComponentsInChildren<RoomMarker>(true))
                roomId = Mathf.Max(roomId, room.RoomId + 1);

            var group = new GameObject(GroupName);
            Undo.RegisterCreatedObjectUndo(group, "Add test cellar");
            group.transform.SetParent(house.transform, false);
            group.transform.position = RoomCenter;
            int markerLayer = LayerMask.NameToLayer(MarkerLayerName);
            if (markerLayer >= 0)
                group.layer = markerLayer;
            var marker = group.AddComponent<RoomMarker>(); // RequireComponent adds the BoxCollider
            marker.Configure(roomId, RoomKey, HouseContract.CellarFloor, false);
            var box = group.GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = Vector3.zero;
            box.size = RoomSize;

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(group.transform, false);
            floor.transform.localPosition = new Vector3(0f, -RoomSize.y * 0.5f - 0.1f, 0f);
            floor.transform.localScale = new Vector3(RoomSize.x, 0.2f, RoomSize.z);
            int floorLayer = LayerMask.NameToLayer(FloorLayerName);
            if (floorLayer >= 0)
                floor.layer = floorLayer;

            for (int i = 0; i < Count; i++)
            {
                var go = new GameObject("TestDigSpot_" + i);
                go.transform.SetParent(group.transform, false);
                go.transform.localPosition = new Vector3((i - (Count - 1) * 0.5f) * Spacing, -RoomSize.y * 0.5f, 0f);
                if (markerLayer >= 0)
                    go.layer = markerLayer;
                go.AddComponent<DigSpot>();
            }

            EditorSceneManager.MarkSceneDirty(house.gameObject.scene);
            Debug.Log("[SetupDig] Test cellar (room " + roomId + ") with " + Count + " dig spots under '" + house.name
                + "/" + GroupName + "'; scene is dirty - save it");
        }
    }
}
