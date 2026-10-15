namespace CinliMahzen.Core
{
    /// <summary>Objective progress (Teknik §4.8). Implemented by Objectives; consumed by HUD, Match, Jinn (Rage), Possession (Spirit Block).</summary>
    public interface IObjectiveInfo
    {
        /// <summary>Current stage: 0 = garden, 1.. = house floors.</summary>
        int StageIndex { get; }
        /// <summary>Total stages including the garden (StagesActive + 1).</summary>
        int StageCount { get; }
        bool IsPuzzleDone(int slot);
        /// <summary>True while the puzzle in <paramref name="slot"/> cannot be scrambled again; <paramref name="remaining"/> in seconds.</summary>
        bool IsPuzzleScrambleOnCooldown(int slot, out float remaining);
        /// <summary>0..1 over DigTotalTime.</summary>
        float DigProgress { get; }
        /// <summary>Completed dig layers, 0..DigCheckpoints. Progress never falls below the last checkpoint.</summary>
        int DigCheckpoint { get; }
        ObjectivePhase Phase { get; }
    }
}
