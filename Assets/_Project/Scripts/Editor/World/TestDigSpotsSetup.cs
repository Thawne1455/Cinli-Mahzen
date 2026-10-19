using CinliMahzen.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// TODO 3.7: <c>CinliMahzen/Setup/Add Test Dig Spots To House In Open Scene</c> — the graybox house has no cellar yet (TODO 4.1), so this puts
    /// three <see cref="DigSpot"/> markers ("TestDigSpot_0..2") under the open scene's HouseService, 2 m apart, at the selected object's position
    /// (or 4 m in front of the house origin), so the round plan picks a real dig site and the dig can be played. Idempotent; marks the scene dirty.
    /// Remove them when the real cellar arrives.
    /// </summary>
    public static class TestDigSpotsSetup
    {
        private const string NamePrefix = "TestDigSpot_";
        private const int Count = 3;
        private const float Spacing = 2f;

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

            Vector3 origin = Selection.activeTransform != null ? Selection.activeTransform.position : house.transform.position + house.transform.forward * -4f;
            int added = 0;
            for (int i = 0; i < Count; i++)
            {
                string name = NamePrefix + i;
                if (house.transform.Find(name) != null)
                    continue;
                var go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, "Add test dig spot");
                go.transform.SetParent(house.transform, true);
                go.transform.position = origin + Vector3.right * (i * Spacing);
                go.AddComponent<DigSpot>();
                added++;
            }

            Scene scene = house.gameObject.scene;
            if (added > 0 && scene.IsValid())
                EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[SetupDig] " + added + " test dig spots added under '" + house.name + "'" + (added > 0 ? "; scene is dirty - save it" : string.Empty));
        }
    }
}
