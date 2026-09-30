using System.Collections.Generic;
using System.IO;
using CinliMahzen.Core;
using UnityEditor;
using UnityEngine;

namespace CinliMahzen.Editor
{
    /// <summary>Owner: C. C0.1: one URP/Lit material for the whole KayKit pack + remap of every model onto it.</summary>
    public static class KayKitSetup
    {
        public const string ModelsDir = "Assets/_Project/Art/KayKit/Models";
        public const string TexturePath = "Assets/_Project/Art/KayKit/Textures/dungeon_texture.png";
        public const string MaterialPath = "Assets/_Project/Art/Materials/M_KayKit_Dungeon.mat";

        [MenuItem("CinliMahzen/Setup C/1 KayKit Material + Remap")]
        public static void Run()
        {
            var mat = EnsureMaterial();
            var guids = AssetDatabase.FindAssets("t:Model", new[] { ModelsDir });
            int changed = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var imp = AssetImporter.GetAtPath(path) as ModelImporter;
                    if (imp == null) continue;
                    var names = new HashSet<string>();
                    foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                        if (o is Material m) names.Add(m.name);
                    bool dirty = false;
                    if (imp.materialImportMode != ModelImporterMaterialImportMode.ImportStandard)
                    {
                        imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                        imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
                        dirty = true;
                    }
                    foreach (var n in names)
                    {
                        var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), n);
                        var map = imp.GetExternalObjectMap();
                        if (!map.TryGetValue(id, out var cur) || cur != mat) { imp.AddRemap(id, mat); dirty = true; }
                    }
                    if (imp.importCameras || imp.importLights) { imp.importCameras = false; imp.importLights = false; dirty = true; }
                    if (dirty) { imp.SaveAndReimport(); changed++; }
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            AssetDatabase.SaveAssets();
            CMLog.Info("KayKit", "Remapped " + changed + "/" + guids.Length + " models onto M_KayKit_Dungeon");
        }

        private static Material EnsureMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat != null) return mat;
            Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            mat = new Material(shader) { name = "M_KayKit_Dungeon" };
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", 0.1f);
            mat.SetFloat("_Metallic", 0f);
            AssetDatabase.CreateAsset(mat, MaterialPath);
            return mat;
        }
    }
}
