namespace CinliMahzen.Core.Events
{
    /// <summary>
    /// Published by Visibility on the local machine (TODO 3.9): the locally played human starts / stops feeling an evil jinn in spirit form within
    /// <c>ColdBreathRange</c> (Oyun §12: the human never sees it, but its breath fogs). UI / audio react; cosmetic only.
    /// </summary>
    public readonly struct ColdBreathChangedEvt
    {
        public readonly bool Active;

        public ColdBreathChangedEvt(bool active)
        {
            Active = active;
        }
    }
}
