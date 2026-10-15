namespace CinliMahzen.Core.Events
{
    /// <summary>Published by Objectives when the state of a puzzle slot changes.</summary>
    public readonly struct PuzzleStateChangedEvt
    {
        public readonly int PuzzleSlot;
        public readonly PlayerId ByPlayer;

        public PuzzleStateChangedEvt(int puzzleSlot, PlayerId byPlayer)
        {
            PuzzleSlot = puzzleSlot;
            ByPlayer = byPlayer;
        }
    }
}
