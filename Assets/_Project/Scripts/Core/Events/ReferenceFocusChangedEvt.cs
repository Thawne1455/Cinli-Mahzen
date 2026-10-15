namespace CinliMahzen.Core.Events
{
    /// <summary>Published by Jinn when the good jinn's Spirit Veil focus starts or stops.</summary>
    public readonly struct ReferenceFocusChangedEvt
    {
        public readonly PlayerId Jinn;
        public readonly bool IsFocusing;

        public ReferenceFocusChangedEvt(PlayerId jinn, bool isFocusing)
        {
            Jinn = jinn;
            IsFocusing = isFocusing;
        }
    }
}
