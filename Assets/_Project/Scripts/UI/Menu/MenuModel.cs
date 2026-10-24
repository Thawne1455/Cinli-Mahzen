namespace CinliMahzen.UI
{
    /// <summary>
    /// TODO 4.2: the navigation of a menu as plain C# (the IMGUI views only draw it). Open shows the root page, Settings is one level
    /// below; Escape closes the settings first, then the menu (unless it cannot close, like the main menu).
    /// </summary>
    public sealed class MenuModel
    {
        private readonly bool _canClose;

        /// <param name="canClose">False for the main menu: it is always open and Escape only leaves the settings.</param>
        public MenuModel(bool canClose)
        {
            _canClose = canClose;
            IsOpen = !canClose;
        }

        public bool IsOpen { get; private set; }

        public MenuScreen Screen { get; private set; } = MenuScreen.Root;

        public void Open()
        {
            IsOpen = true;
            Screen = MenuScreen.Root;
        }

        /// <summary>Closes the menu (a menu that cannot close goes back to its root page).</summary>
        public void Close()
        {
            Screen = MenuScreen.Root;
            if (_canClose)
                IsOpen = false;
        }

        public void ShowSettings()
        {
            if (IsOpen)
                Screen = MenuScreen.Settings;
        }

        public void Back()
        {
            Screen = MenuScreen.Root;
        }

        /// <summary>The Esc key: closed → open; settings → root; root → closed.</summary>
        public void Escape()
        {
            if (!IsOpen)
                Open();
            else if (Screen == MenuScreen.Settings)
                Back();
            else
                Close();
        }
    }
}
