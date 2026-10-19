namespace CinliMahzen.Core
{
    /// <summary>One awarded match-end title (TODO 4.3): who earned it and with how many.</summary>
    public readonly struct MatchTitle
    {
        public readonly TitleKind Kind;
        public readonly PlayerId Player;
        public readonly int Count;

        public MatchTitle(TitleKind kind, PlayerId player, int count)
        {
            Kind = kind;
            Player = player;
            Count = count;
        }
    }
}
