using CinliMahzen.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CinliMahzen.UI
{
    /// <summary>
    /// The main menu (A0.9, TODO 4.2; placeholder IMGUI art): offline hotseat test game, online (M5, disabled), settings, quit. Esc leaves the
    /// settings page. The class keeps its placeholder name so the MainMenu scene keeps its reference.
    /// </summary>
    public sealed class MainMenuPlaceholder : MonoBehaviour
    {
        [SerializeField] private string gameScene = "Game";
        [SerializeField] private float buttonWidth = 280f;
        [SerializeField] private float buttonHeight = 44f;
        [SerializeField] private float settingsWidth = 420f;
        [SerializeField] private float settingsHeight = 460f;

        private readonly MenuModel _menu = new MenuModel(false);
        private GUIStyle _title;
        private ILocalInputProvider _input;

        private void OnEnable()
        {
            // Coming back from a game: the cursor must be free and time running.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f;
            _input = GameServices.Get<ILocalInputProvider>();
        }

        private void Update()
        {
            // A closing pause menu (leaving the Game scene) may restore the game's locked cursor after OnEnable: the menu needs it free.
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            if (_input != null && _input.PausePressed)
                _menu.Escape();
        }

        private void OnGUI()
        {
            if (_title == null)
                _title = new GUIStyle(GUI.skin.label) { fontSize = 36, alignment = TextAnchor.MiddleCenter };

            float y = Screen.height * 0.3f;
            GUI.Label(new Rect(0f, y - 80f, Screen.width, 60f), Loc.T("menu.title"), _title);

            if (_menu.Screen == MenuScreen.Settings)
            {
                var area = new Rect((Screen.width - settingsWidth) * 0.5f, y, settingsWidth, settingsHeight);
                if (SettingsPanel.Draw(area, GameServices.Get<GameSettings>(), buttonHeight))
                    _menu.Back();
                return;
            }

            float x = (Screen.width - buttonWidth) * 0.5f;
            float step = buttonHeight + 10f;
            if (GUI.Button(new Rect(x, y, buttonWidth, buttonHeight), Loc.T("menu.play_hotseat")))
                SceneManager.LoadScene(gameScene);

            GUI.enabled = false;
            GUI.Button(new Rect(x, y + step, buttonWidth, buttonHeight), Loc.T("menu.online"));
            GUI.enabled = true;

            if (GUI.Button(new Rect(x, y + step * 2f, buttonWidth, buttonHeight), Loc.T("menu.settings")))
                _menu.ShowSettings();

            if (GUI.Button(new Rect(x, y + step * 3f, buttonWidth, buttonHeight), Loc.T("menu.quit")))
                Application.Quit();
        }
    }
}
