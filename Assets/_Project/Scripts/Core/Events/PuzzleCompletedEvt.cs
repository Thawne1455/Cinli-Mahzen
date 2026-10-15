namespace CinliMahzen.Core.Events
{
    /// <summary>Published by Objectives when a puzzle slot is solved.</summary>
    public readonly struct PuzzleCompletedEvt
    {
        public readonly int PuzzleSlot;
        public readonly int Stage;

        public PuzzleCompletedEvt(int puzzleSlot, int stage)
        {
            PuzzleSlot = puzzleSlot;
            Stage = stage;
        }
    }
}
