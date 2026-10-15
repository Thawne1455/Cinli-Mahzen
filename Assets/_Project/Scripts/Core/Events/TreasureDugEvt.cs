namespace CinliMahzen.Core.Events
{
    /// <summary>Published by Objectives when the treasure is dug up in the cellar.</summary>
    public readonly struct TreasureDugEvt
    {
        public readonly PlayerId Digger;

        public TreasureDugEvt(PlayerId digger)
        {
            Digger = digger;
        }
    }
}
