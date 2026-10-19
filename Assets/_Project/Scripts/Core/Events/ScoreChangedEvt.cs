namespace CinliMahzen.Core.Events
{
    /// <summary>Published by Match on every machine when a player's score changed (TODO 3.10). <c>PlayerInfo.Score</c> already holds <see cref="Total"/>.</summary>
    public readonly struct ScoreChangedEvt
    {
        public readonly PlayerId Player;
        public readonly int Total;
        public readonly int Delta;
        public readonly ScoreReason Reason;

        public ScoreChangedEvt(PlayerId player, int total, int delta, ScoreReason reason)
        {
            Player = player;
            Total = total;
            Delta = delta;
            Reason = reason;
        }
    }
}
