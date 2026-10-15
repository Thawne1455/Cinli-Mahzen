namespace CinliMahzen.Core.Events
{
    /// <summary>Published by Jinn when the evil jinn breaks the good jinn's focus, which blurs the good jinn.</summary>
    public readonly struct SpiritBumpedEvt
    {
        public readonly PlayerId EvilJinn;
        public readonly PlayerId GoodJinn;

        public SpiritBumpedEvt(PlayerId evilJinn, PlayerId goodJinn)
        {
            EvilJinn = evilJinn;
            GoodJinn = goodJinn;
        }
    }
}
