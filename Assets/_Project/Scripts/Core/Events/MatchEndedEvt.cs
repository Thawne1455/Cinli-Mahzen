namespace CinliMahzen.Core.Events
{
    /// <summary>
    /// Published by Match on every machine when the match reaches MatchEnd (TODO 3.10). Final scores are in <c>PlayerInfo.Score</c>;
    /// <see cref="Winner"/> is the top scorer, <see cref="PlayerId.None"/> on a tie for first.
    /// </summary>
    public readonly struct MatchEndedEvt
    {
        public readonly PlayerId Winner;
        public readonly int TopScore;

        public MatchEndedEvt(PlayerId winner, int topScore)
        {
            Winner = winner;
            TopScore = topScore;
        }
    }
}
