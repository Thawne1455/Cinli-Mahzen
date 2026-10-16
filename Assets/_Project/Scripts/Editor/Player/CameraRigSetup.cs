using CinliMahzen.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
// Aliases instead of whole namespaces: UnityEngine.Rendering also has a public DebugState, which would clash with CinliMahzen.Core.DebugState.
using DepthOfField = UnityEngine.Rendering.Universal.DepthOfField;
using DepthOfFieldMode = UnityEngine.Rendering.Universal.DepthOfFieldMode;
using UniversalAdditionalCameraData = UnityEngine.Rendering.Universal.UniversalAdditionalCameraData;
using Volume = UnityEngine.Rendering.Volume;
using VolumeProfile = UnityEngine.Rendering.VolumeProfile;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// Sets up the CameraRig (TODO 1.2) in the open scene: puts <see cref="CameraRig"/> on the scene's Main Camera (creating a camera
    /// when there is none), turns post-processing on for it, and wires a global blur Volume. The Volume is a child of the camera named
    /// CM_BlurVolume, weight 0, using Assets/_Project/Settings/CM_BlurProfile.asset (a Gaussian DepthOfField override that blurs the whole
    /// view at weight 1; the rig drives the weight). Idempotent: existing camera, rig, volume and profile are reused and brought up to
    /// date. Marks the scene dirty but does not save it.
    /// MCP: open the scene (Sandbox / Game), execute_menu_item("CinliMahzen/Setup/Add Camera Rig To Open Scene"), save the scene.
    /// </summary>
    public static class CameraRigSetup
    {
        public const string ProfilePath = "Assets/_Project/Settings/CM_BlurProfile.asset";

        private const string ProfileParentFolder = "Assets/_Project";
        private const string ProfileFolderName = "Settings";
        private const string MainCameraTag = "MainCamera";
        private const string MainCameraName = "Main Camera";
        private const string VolumeName = "CM_BlurVolume";
        private const int DefaultLayer = 0;
        private const int VolumePriority = 100;

        // Gaussian depth of field tuned to blur the whole picture: blur starts at once (start 0) and is at full strength
        // 0.5 m out (end, the parameter's minimum), with the largest radius the parameter allows (1.5).
        private const float GaussianStart = 0f;
        private const float GaussianEnd = 0.5f;
        private const float GaussianMaxRadius = 1.5f;

        // Overview pose of a camera created here; the rig takes the view over as soon as a pawn attaches.
        private static readonly Vector3 CreatedCameraPosition = new Vector3(0f, 6f, -10f);
        private static readonly Quaternion CreatedCameraRotation = Quaternion.Euler(30f, 0f, 0f);

        [MenuItem("CinliMahzen/Setup/Add Camera Rig To Open Scene")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[SetupCamera] Stop play mode first");
                return;
            }
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[SetupCamera] No open scene");
                return;
            }

            VolumeProfile profile = EnsureProfile();
            if (profile == null)
                return;

            Camera camera = FindMainCamera(scene);
            bool createdCamera = camera == null;
            if (createdCamera)
                camera = CreateMainCamera();

            CameraRig rig = camera.GetComponent<CameraRig>();
            bool createdRig = rig == null;
            if (createdRig)
                rig = Undo.AddComponent<CameraRig>(camera.gameObject);
            WarnAboutOtherRigs(rig);

            Volume volume = EnsureVolume(camera, profile);
            EnsurePostProcessing(camera, volume.gameObject.layer);

            var so = new SerializedObject(rig);
            so.FindProperty("blurVolume").objectReferenceValue = volume;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rig);

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[SetupCamera] " + (createdCamera ? "Created camera, " : "Camera '" + camera.name + "': ")
                + (createdRig ? "added CameraRig, " : "CameraRig already there, ")
                + "blur volume '" + volume.name + "' -> " + ProfilePath + " (scene '" + scene.name + "' is dirty - save it)");
        }

        private static Camera FindMainCamera(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Camera candidate in root.GetComponentsInChildren<Camera>(true))
                {
                    if (candidate.CompareTag(MainCameraTag))
                        return candidate;
                }
            }
            return null;
        }

        private static Camera CreateMainCamera()
        {
            var go = new GameObject(MainCameraName);
            Undo.RegisterCreatedObjectUndo(go, "Create Main Camera");
            go.tag = MainCameraTag;
            go.transform.SetPositionAndRotation(CreatedCameraPosition, CreatedCameraRotation);
            Camera camera = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
            return camera;
        }

        private static void WarnAboutOtherRigs(CameraRig keep)
        {
            foreach (CameraRig other in Object.FindObjectsByType<CameraRig>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (other != keep && other.gameObject.scene == keep.gameObject.scene)
                    Debug.LogWarning("[SetupCamera] Another CameraRig on '" + other.name + "' - only one may be registered as ICameraRig", other);
            }
        }

        private static VolumeProfile EnsureProfile()
        {
            string folder = ProfileParentFolder + "/" + ProfileFolderName;
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(ProfileParentFolder, ProfileFolderName);

            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            if (!profile.TryGet(out DepthOfField dof))
            {
                dof = profile.Add<DepthOfField>(false);
                AssetDatabase.AddObjectToAsset(dof, profile);
            }
            dof.active = true;
            dof.mode.Override(DepthOfFieldMode.Gaussian);
            dof.gaussianStart.Override(GaussianStart);
            dof.gaussianEnd.Override(GaussianEnd);
            dof.gaussianMaxRadius.Override(GaussianMaxRadius);
            dof.highQualitySampling.Override(true);

            EditorUtility.SetDirty(dof);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static Volume EnsureVolume(Camera camera, VolumeProfile profile)
        {
            Transform child = camera.transform.Find(VolumeName);
            GameObject go;
            if (child != null)
            {
                go = child.gameObject;
            }
            else
            {
                go = new GameObject(VolumeName);
                Undo.RegisterCreatedObjectUndo(go, "Create blur volume");
                go.transform.SetParent(camera.transform, false);
            }
            go.layer = DefaultLayer;

            Volume volume = go.GetComponent<Volume>();
            if (volume == null)
                volume = Undo.AddComponent<Volume>(go);
            volume.isGlobal = true;
            volume.priority = VolumePriority;
            volume.weight = 0f;
            volume.sharedProfile = profile;
            EditorUtility.SetDirty(volume);
            return volume;
        }

        /// <summary>URP only runs volumes for a camera that has post-processing on and the volume's layer in its volume mask.</summary>
        private static void EnsurePostProcessing(Camera camera, int volumeLayer)
        {
            UniversalAdditionalCameraData data = camera.GetComponent<UniversalAdditionalCameraData>();
            if (data == null)
                data = Undo.AddComponent<UniversalAdditionalCameraData>(camera.gameObject);
            data.renderPostProcessing = true;

            LayerMask mask = data.volumeLayerMask;
            mask.value |= 1 << volumeLayer;
            data.volumeLayerMask = mask;
            EditorUtility.SetDirty(data);
        }
    }
}
