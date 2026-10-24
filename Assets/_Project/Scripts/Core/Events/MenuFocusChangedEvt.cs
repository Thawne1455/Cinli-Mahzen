namespace CinliMahzen.Core.Events
{
    /// <summary>Published by <see cref="MenuFocus"/>: a menu took (or gave back) the mouse and the keyboard. Gameplay input stops while open.</summary>
    public readonly struct MenuFocusChangedEvt
    {
        public readonly bool IsOpen;

        public MenuFocusChangedEvt(bool isOpen)
        {
            IsOpen = isOpen;
        }
    }
}
