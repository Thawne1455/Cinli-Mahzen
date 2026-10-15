namespace CinliMahzen.Possession
{
    /// <summary>
    /// Which jinn may use a possessable action (Oyun §5.2 / §8): the good jinn only slides, carries / flings
    /// harmlessly and revives with cologne. Serialized as int — append only.
    /// </summary>
    public enum ActionUser : byte
    {
        EvilOnly = 0,
        GoodOnly = 1,
        Both = 2,
    }
}
