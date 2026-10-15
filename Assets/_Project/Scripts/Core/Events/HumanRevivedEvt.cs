namespace CinliMahzen.Core.Events
{
    /// <summary>Published by Player on every client when the human revived (after FaintDuration, or instantly via Lemon Cologne).</summary>
    public readonly struct HumanRevivedEvt
    {
        public readonly bool ByCologne;

        public HumanRevivedEvt(bool byCologne)
        {
            ByCologne = byCologne;
        }
    }
}
