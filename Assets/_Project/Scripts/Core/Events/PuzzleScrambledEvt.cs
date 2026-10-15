namespace CinliMahzen.Core.Events
{
    /// <summary>Published by Objectives when an evil jinn mass-scrambled a puzzle (all parts at once); it cannot be scrambled again until CooldownUntil.</summary>
    public readonly struct PuzzleScrambledEvt
    {
        public readonly int PuzzleSlot;
        public readonly PlayerId ByJinn;
        public readonly double CooldownUntil;

        public PuzzleScrambledEvt(int puzzleSlot, PlayerId byJinn, double cooldownUntil)
        {
            PuzzleSlot = puzzleSlot;
            ByJinn = byJinn;
            CooldownUntil = cooldownUntil;
        }
    }
}
