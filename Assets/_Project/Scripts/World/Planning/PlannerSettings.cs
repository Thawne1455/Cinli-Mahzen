using System;
using System.Collections.Generic;
using CinliMahzen.Core;

namespace CinliMahzen.World
{
    /// <summary>
    /// What <see cref="RoundSetupPlanner"/> and <see cref="RoundPlanValidator"/> need from the balance config (Teknik 7.5, Oyun 14):
    /// how many stages are in play and how many puzzles each stage holds. Immutable. The game takes it from
    /// <see cref="FromConfig"/>; tests may build other shapes with the constructor.
    ///
    /// Active stages are <see cref="HouseContract.FirstPuzzleStage"/> .. <see cref="LastStage"/> (MVP: 1 = ground floor, 2 = first floor).
    /// </summary>
    public sealed class PlannerSettings
    {
        /// <summary>ItemDefinition.Id of the shovel (Oyun 6: the reward of a puzzle in the last stage, needed in the cellar).</summary>
        public const string DefaultShovelItemId = "shovel";

        /// <summary>
        /// Upper bound on the candidates the planner tries in one plan (slot combinations, definitions, reward givers, reference slots).
        /// A technical guard against pathological houses, not a balance value: the graybox house needs well under a hundred.
        /// </summary>
        public const int DefaultMaxSearchSteps = 200000;

        public int StagesActive { get; }
        public int PuzzlesPerStage { get; }

        /// <summary>ItemDefinition.Id that a puzzle of the last stage must reward; empty = no such item.</summary>
        public string ShovelItemId { get; }

        public int MaxSearchSteps { get; }

        public PlannerSettings(int stagesActive, int puzzlesPerStage, string shovelItemId = DefaultShovelItemId,
            int maxSearchSteps = DefaultMaxSearchSteps)
        {
            StagesActive = stagesActive;
            PuzzlesPerStage = puzzlesPerStage;
            ShovelItemId = shovelItemId ?? string.Empty;
            MaxSearchSteps = maxSearchSteps;
        }

        /// <summary>StagesActive and PuzzlesPerStage from the config, the default shovel id and search budget.</summary>
        public static PlannerSettings FromConfig(GameBalanceConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            return new PlannerSettings(config.StagesActive, config.PuzzlesPerStage);
        }

        /// <summary>Puzzles in one round (StagesActive x PuzzlesPerStage).</summary>
        public int PuzzleCount => StagesActive * PuzzlesPerStage;

        public bool HasShovel => ShovelItemId.Length > 0;

        public int FirstStage => HouseContract.FirstPuzzleStage;

        public int LastStage => HouseContract.FirstPuzzleStage + StagesActive - 1;

        /// <summary>The house stage index of the <paramref name="ordinal"/>-th active stage (0 = the first puzzle stage).</summary>
        public int StageIndex(int ordinal)
        {
            return HouseContract.FirstPuzzleStage + ordinal;
        }

        public bool IsActiveStage(int stageIndex)
        {
            return stageIndex >= FirstStage && stageIndex <= LastStage;
        }

        /// <summary>Appends a message per unusable value (no stage, no puzzle per stage, no search budget).</summary>
        public void CollectProblems(List<string> problems)
        {
            if (problems == null)
            {
                throw new ArgumentNullException(nameof(problems));
            }
            if (StagesActive < 1)
            {
                problems.Add("StagesActive is " + StagesActive + " (must be >= 1)");
            }
            if (PuzzlesPerStage < 1)
            {
                problems.Add("PuzzlesPerStage is " + PuzzlesPerStage + " (must be >= 1)");
            }
            if (MaxSearchSteps < 1)
            {
                problems.Add("MaxSearchSteps is " + MaxSearchSteps + " (must be >= 1)");
            }
        }

        public override string ToString()
        {
            return "StagesActive " + StagesActive + ", PuzzlesPerStage " + PuzzlesPerStage + ", shovel '" + ShovelItemId + "'";
        }
    }
}
