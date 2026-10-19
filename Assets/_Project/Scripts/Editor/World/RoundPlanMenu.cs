using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.World;
using CinliMahzen.World.Graybox;
using UnityEditor;
using UnityEngine;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// TODO 2.3 menus around <see cref="RoundSetupPlanner"/>:
    /// <list type="bullet">
    /// <item><c>CinliMahzen/House/Print Round Plan (Seed=12345)</c>: plans seed 12345, logs it readably and validates it; last line
    /// "Print Round Plan: seed 12345, hash 0x..., N validator errors".</item>
    /// <item><c>CinliMahzen/House/Plan Batch Report (500 seeds)</c>: <see cref="RoundPlanBatchReport"/> over seeds 1..500 (validity,
    /// type/room/reward distributions, timing, first errors); last line "Plan Batch Report: V/500 valid".</item>
    /// </list>
    /// House: the HouseService of the open scene, else the P_House prefab (like House/Validate), else the graybox marker data.
    /// Pool: the PZ_*.asset files, else in-memory MvpPuzzleCatalog definitions. Settings: GameBalanceConfig.asset (StagesActive,
    /// PuzzlesPerStage). Nothing is changed, so both work in edit mode.
    /// </summary>
    public static class RoundPlanMenu
    {
        private const string MenuRoot = "CinliMahzen/House/";
        private const string LogCategory = "House";
        private const string ConfigPath = "Assets/_Project/ScriptableObjects/Config/GameBalanceConfig.asset";
        private const int PrintSeed = 12345;
        private const int BatchFirstSeed = 1;
        private const int BatchSeedCount = 500;

        [MenuItem(MenuRoot + "Print Round Plan (Seed=12345)")]
        public static void PrintRoundPlan()
        {
            HouseIndex house;
            string source;
            if (!TryLoadHouse(out house, out source))
            {
                return;
            }

            bool fromAssets;
            List<PuzzleDefinition> pool = PuzzleDefinitionSetup.LoadPool(out fromAssets);
            try
            {
                PlannerSettings settings = LoadSettings();
                CMLog.Info(LogCategory, "Print Round Plan: house from " + source + ", " + DescribePool(pool, fromAssets) + ", " + settings);

                RoundPlan plan;
                string error;
                if (!new RoundSetupPlanner().TryPlan(house, pool, settings, PrintSeed, out plan, out error))
                {
                    CMLog.Error(LogCategory, "Print Round Plan: seed " + PrintSeed + " cannot be planned: " + error);
                    return;
                }

                CMLog.Info(LogCategory, RoundPlanFormatter.Format(plan, house, pool));
                List<string> errors = RoundPlanValidator.Validate(plan, house, pool, settings);
                for (int i = 0; i < errors.Count; i++)
                {
                    CMLog.Error(LogCategory, "Plan error: " + errors[i]);
                }

                string summary = "Print Round Plan: seed " + PrintSeed + ", hash 0x" + plan.ComputeHash().ToString("X16") + ", "
                    + errors.Count + " validator errors";
                if (errors.Count > 0)
                {
                    CMLog.Error(LogCategory, summary);
                }
                else
                {
                    CMLog.Info(LogCategory, summary);
                }
            }
            finally
            {
                if (!fromAssets)
                {
                    PuzzleDefinitionSetup.DestroyTransient(pool);
                }
            }
        }

        [MenuItem(MenuRoot + "Plan Batch Report (500 seeds)")]
        public static void PlanBatchReport()
        {
            HouseIndex house;
            string source;
            if (!TryLoadHouse(out house, out source))
            {
                CMLog.Error(LogCategory, "Plan Batch Report: 0/" + BatchSeedCount + " valid");
                return;
            }

            bool fromAssets;
            List<PuzzleDefinition> pool = PuzzleDefinitionSetup.LoadPool(out fromAssets);
            try
            {
                PlannerSettings settings = LoadSettings();
                CMLog.Info(LogCategory, "Plan Batch Report: house from " + source + ", " + DescribePool(pool, fromAssets) + ", " + settings);

                RoundPlanBatchReport report = RoundPlanBatchReport.Run(house, pool, settings, BatchFirstSeed, BatchSeedCount);
                List<string> lines = report.ToLines(house);
                for (int i = 0; i < lines.Count - 1; i++)
                {
                    if (lines[i].StartsWith("Invalid ", System.StringComparison.Ordinal))
                    {
                        CMLog.Error(LogCategory, lines[i]);
                    }
                    else
                    {
                        CMLog.Info(LogCategory, lines[i]);
                    }
                }

                if (report.InvalidCount > 0)
                {
                    CMLog.Error(LogCategory, report.SummaryLine);
                }
                else
                {
                    CMLog.Info(LogCategory, report.SummaryLine);
                }
            }
            finally
            {
                if (!fromAssets)
                {
                    PuzzleDefinitionSetup.DestroyTransient(pool);
                }
            }
        }

        // ------------------------------------------------------------------------------------------------

        private static bool TryLoadHouse(out HouseIndex house, out string source)
        {
            HouseService service = HouseValidateMenu.FindService(out source);
            if (service == null)
            {
                house = GrayboxHouseIndexFactory.BuildDefault();
                source = "the graybox marker data (no HouseService in the open scene or P_House prefab)";
                return true;
            }

            string error;
            if (!service.TryBuildIndex(out house, out error))
            {
                CMLog.Error(LogCategory, "Cannot index the markers of " + source + ": " + error);
                return false;
            }
            return true;
        }

        private static PlannerSettings LoadSettings()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(ConfigPath);
            if (config != null)
            {
                return PlannerSettings.FromConfig(config);
            }

            CMLog.Warn(LogCategory, "No GameBalanceConfig at " + ConfigPath + "; using its default values.");
            var defaults = ScriptableObject.CreateInstance<GameBalanceConfig>();
            try
            {
                return PlannerSettings.FromConfig(defaults);
            }
            finally
            {
                Object.DestroyImmediate(defaults);
            }
        }

        private static string DescribePool(List<PuzzleDefinition> pool, bool fromAssets)
        {
            var names = new List<string>(pool.Count);
            for (int i = 0; i < pool.Count; i++)
            {
                names.Add(pool[i].name);
            }
            return "pool " + (fromAssets ? "assets" : "in-memory catalog") + " [" + string.Join(", ", names) + "]";
        }
    }
}
