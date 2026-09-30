namespace CinliMahzen.Core
{
    /// <summary>Match flow (Teknik §9.1). Wire/serialized value — do not reorder.</summary>
    public enum MatchState : byte
    {
        Lobby = 0,
        RoundSetup = 1,
        RoleReveal = 2,
        Intro = 3,
        Playing = 4,
        RoundEnd = 5,
        MatchEnd = 6,
    }
}
