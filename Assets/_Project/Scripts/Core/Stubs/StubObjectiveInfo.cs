namespace CinliMahzen.Core
{
    /// <summary>Placeholder until Objectives' PuzzleSystem: garden stage, nothing solved, no cooldowns. Settable for tests/sandboxes.</summary>
    public sealed class StubObjectiveInfo : IObjectiveInfo
    {
        public int StageIndex { get; set; }
        public int StageCount { get; set; } = 3;
        public float DigProgress { get; set; }
        public int DigCheckpoint { get; set; }
        public ObjectivePhase Phase { get; set; } = ObjectivePhase.Garden;

        public bool IsPuzzleDone(int slot) => false;

        public bool IsPuzzleScrambleOnCooldown(int slot, out float remaining)
        {
            remaining = 0f;
            return false;
        }
    }
}
