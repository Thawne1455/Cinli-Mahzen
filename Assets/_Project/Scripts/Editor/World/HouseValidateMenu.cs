using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.World;
using CinliMahzen.World.Graybox;
using UnityEditor;
using UnityEngine;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// TODO 2.2: <c>CinliMahzen/House/Validate</c> indexes the house markers and runs <see cref="HouseValidator"/> on them.
    /// It uses the HouseService of the open scene; when no scene has one it reads the P_House prefab asset. Nothing is changed
    /// or registered (HouseService.TryBuildIndex has no side effects), so it also works in edit mode and with a dirty scene.
    /// Every error goes to the console as an error, every warning as a warning, and the last line is
    /// "House/Validate: N errors, M warnings". MCP: execute_menu_item("CinliMahzen/House/Validate") then read_console.
    /// </summary>
    public static class HouseValidateMenu
    {
        private const string LogCategory = "House";

        [MenuItem("CinliMahzen/House/Validate")]
        public static void Validate()
        {
            HouseService service = FindService(out string source);
            if (service == null)
            {
                Debug.LogError("[" + LogCategory + "] House/Validate: no HouseService in the open scene and no P_House prefab at "
                    + HouseGrayboxBuilder.HousePrefabPath + ". Run CinliMahzen/House/Build Graybox first.");
                Debug.LogError("[" + LogCategory + "] House/Validate: 1 errors, 0 warnings");
                return;
            }

            Debug.Log("[" + LogCategory + "] House/Validate: checking " + source);
            HouseIndex index;
            string error;
            if (!service.TryBuildIndex(out index, out error))
            {
                Debug.LogError("[" + LogCategory + "] Cannot index the markers: " + error);
                Debug.LogError("[" + LogCategory + "] House/Validate: 1 errors, 0 warnings");
                return;
            }

            List<HouseIssue> issues = HouseValidator.Validate(index, LoadKnownItemIds());
            CompareWithGrayboxData(index, issues);
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Severity == HouseIssueSeverity.Error)
                {
                    Debug.LogError("[" + LogCategory + "] " + issues[i].Message);
                }
                else
                {
                    Debug.LogWarning("[" + LogCategory + "] " + issues[i].Message);
                }
            }

            int errors = HouseValidator.CountErrors(issues);
            int warnings = HouseValidator.CountWarnings(issues);
            string summary = "[" + LogCategory + "] House/Validate: " + errors + " errors, " + warnings + " warnings";
            if (errors > 0)
            {
                Debug.LogError(summary);
            }
            else
            {
                Debug.Log(summary);
            }
        }

        /// <summary>
        /// The prefab is built from <see cref="GrayboxMarkers"/>, so every NetId-carrying marker must have the same key and NetId as
        /// in the pure-data index (the one the EditMode tests validate). A difference means the house was edited by hand or the
        /// prefab is stale: a warning, not an error.
        /// </summary>
        private static void CompareWithGrayboxData(HouseIndex real, List<HouseIssue> issues)
        {
            HouseIndex data = GrayboxHouseIndexFactory.BuildDefault();
            var realEntries = new List<HouseNetEntry>();
            real.CollectNetEntries(realEntries);
            var dataEntries = new List<HouseNetEntry>();
            data.CollectNetEntries(dataEntries);

            var expected = new Dictionary<string, int>();
            for (int i = 0; i < dataEntries.Count; i++)
            {
                expected[dataEntries[i].Path] = dataEntries[i].NetId.Value;
            }

            int differing = 0;
            var seen = new HashSet<string>();
            for (int i = 0; i < realEntries.Count; i++)
            {
                seen.Add(realEntries[i].Path);
                int id;
                if (!expected.TryGetValue(realEntries[i].Path, out id) || id != realEntries[i].NetId.Value)
                {
                    differing++;
                }
            }
            for (int i = 0; i < dataEntries.Count; i++)
            {
                if (!seen.Contains(dataEntries[i].Path))
                {
                    differing++;
                }
            }

            if (differing > 0)
            {
                issues.Add(new HouseIssue(HouseIssueSeverity.Warning, differing
                    + " marker(s) differ from the graybox marker data (hand-edited house or stale prefab; "
                    + "CinliMahzen/House/Build Graybox rebuilds it)"));
            }
        }

        private static HouseService FindService(out string source)
        {
            HouseService inScene = Object.FindFirstObjectByType<HouseService>(FindObjectsInactive.Include);
            if (inScene != null)
            {
                source = "scene object " + inScene.name;
                return inScene;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HouseGrayboxBuilder.HousePrefabPath);
            if (prefab != null)
            {
                HouseService onPrefab = prefab.GetComponent<HouseService>();
                if (onPrefab != null)
                {
                    source = "prefab " + HouseGrayboxBuilder.HousePrefabPath;
                    return onPrefab;
                }
                Debug.LogError("[" + LogCategory + "] " + HouseGrayboxBuilder.HousePrefabPath
                    + " has no HouseService; rebuild it with CinliMahzen/House/Build Graybox.");
            }

            source = string.Empty;
            return null;
        }

        /// <summary>Ids of every ItemDefinition asset (gate locks and tool shed items must use one); null when there are none, which skips the check.</summary>
        private static HashSet<string> LoadKnownItemIds()
        {
            string[] guids = AssetDatabase.FindAssets("t:ItemDefinition");
            if (guids.Length == 0)
            {
                return null;
            }

            var ids = new HashSet<string>();
            for (int i = 0; i < guids.Length; i++)
            {
                var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (item != null && !string.IsNullOrEmpty(item.Id))
                {
                    ids.Add(item.Id);
                }
            }
            return ids;
        }
    }
}
