using System;
using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.World;
using UnityEditor;
using UnityEngine;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// TODO 2.3: <c>CinliMahzen/Setup/Create Puzzle Definitions</c> creates or updates PZ_Paintings / PZ_Statue / PZ_Wires / PZ_Sigil
    /// under <see cref="Folder"/> from <see cref="MvpPuzzleCatalog"/> (the same data the EditMode tests use). Idempotent: only the
    /// data fields are written (type, nameKey, valueCounts, opKind, requiredItemId, size) through SerializedObject; prefab
    /// references set by hand (TODO 2.5, 3.1-3.3) are kept. Last log line: "Create Puzzle Definitions: N created, M updated, K unchanged".
    /// </summary>
    public static class PuzzleDefinitionSetup
    {
        public const string Folder = "Assets/_Project/ScriptableObjects/Puzzles";

        private const string LogCategory = "Setup";

        [MenuItem("CinliMahzen/Setup/Create Puzzle Definitions")]
        public static void CreateAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                CMLog.Error(LogCategory, "Create Puzzle Definitions: stop play mode first");
                return;
            }

            EnsureFolder(Folder);
            int created = 0;
            int updated = 0;
            int unchanged = 0;
            IReadOnlyList<PuzzleDefinitionSpec> specs = MvpPuzzleCatalog.Specs;
            for (int i = 0; i < specs.Count; i++)
            {
                PuzzleDefinitionSpec spec = specs[i];
                string path = Folder + "/" + spec.AssetName + ".asset";
                var definition = AssetDatabase.LoadAssetAtPath<PuzzleDefinition>(path);
                bool isNew = definition == null;
                if (isNew)
                {
                    definition = ScriptableObject.CreateInstance<PuzzleDefinition>();
                    AssetDatabase.CreateAsset(definition, path);
                }

                bool changed = Apply(definition, spec);
                if (isNew || changed)
                {
                    EditorUtility.SetDirty(definition);
                }
                if (isNew)
                {
                    created++;
                    CMLog.Info(LogCategory, "Created " + path + " " + spec);
                }
                else if (changed)
                {
                    updated++;
                    CMLog.Info(LogCategory, "Updated " + path + " " + spec);
                }
                else
                {
                    unchanged++;
                }
            }

            AssetDatabase.SaveAssets();
            CMLog.Info(LogCategory, "Create Puzzle Definitions: " + created + " created, " + updated + " updated, " + unchanged
                + " unchanged (" + Folder + ")");
        }

        /// <summary>Writes the data fields of <paramref name="spec"/> into the definition (prefab references untouched). True when a value changed.</summary>
        public static bool Apply(PuzzleDefinition definition, PuzzleDefinitionSpec spec)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }
            if (spec == null)
            {
                throw new ArgumentNullException(nameof(spec));
            }

            var so = new SerializedObject(definition);
            Require(so, "type").intValue = (int)spec.Type;
            Require(so, "nameKey").stringValue = spec.NameKey;
            SerializedProperty counts = Require(so, "valueCounts");
            counts.arraySize = spec.PartCount;
            for (int i = 0; i < spec.PartCount; i++)
            {
                counts.GetArrayElementAtIndex(i).intValue = spec.ValueCounts[i];
            }
            Require(so, "opKind").intValue = (int)spec.OpKind;
            Require(so, "requiredItemId").stringValue = spec.RequiredItemId;
            Require(so, "size").intValue = (int)spec.Size;
            return so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// The PuzzleDefinition assets in <see cref="Folder"/>, sorted by type then name. When there are none, in-memory definitions built
        /// from <see cref="MvpPuzzleCatalog"/> (<paramref name="fromAssets"/> false; release them with <see cref="DestroyTransient"/>).
        /// </summary>
        public static List<PuzzleDefinition> LoadPool(out bool fromAssets)
        {
            var pool = new List<PuzzleDefinition>();
            if (AssetDatabase.IsValidFolder(Folder))
            {
                string[] guids = AssetDatabase.FindAssets("t:PuzzleDefinition", new[] { Folder });
                for (int i = 0; i < guids.Length; i++)
                {
                    var definition = AssetDatabase.LoadAssetAtPath<PuzzleDefinition>(AssetDatabase.GUIDToAssetPath(guids[i]));
                    if (definition != null)
                    {
                        pool.Add(definition);
                    }
                }
            }

            fromAssets = pool.Count > 0;
            if (fromAssets)
            {
                pool.Sort(CompareDefinitions);
                return pool;
            }

            CMLog.Warn(LogCategory, "No PuzzleDefinition assets in " + Folder
                + "; using in-memory definitions from MvpPuzzleCatalog (run CinliMahzen/Setup/Create Puzzle Definitions).");
            IReadOnlyList<PuzzleDefinitionSpec> specs = MvpPuzzleCatalog.Specs;
            for (int i = 0; i < specs.Count; i++)
            {
                var definition = ScriptableObject.CreateInstance<PuzzleDefinition>();
                definition.name = specs[i].AssetName;
                definition.hideFlags = HideFlags.DontSave;
                Apply(definition, specs[i]);
                pool.Add(definition);
            }
            return pool;
        }

        /// <summary>Destroys definitions that <see cref="LoadPool"/> created in memory (assets are left alone).</summary>
        public static void DestroyTransient(List<PuzzleDefinition> pool)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i] != null && !EditorUtility.IsPersistent(pool[i]))
                {
                    UnityEngine.Object.DestroyImmediate(pool[i]);
                }
            }
            pool.Clear();
        }

        private static int CompareDefinitions(PuzzleDefinition a, PuzzleDefinition b)
        {
            int byType = ((int)a.Type).CompareTo((int)b.Type);
            return byType != 0 ? byType : string.CompareOrdinal(a.name, b.name);
        }

        private static SerializedProperty Require(SerializedObject so, string field)
        {
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException("PuzzleDefinition has no serialized field '" + field + "'");
            }
            return property;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }
    }
}
