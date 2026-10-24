using CinliMahzen.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CinliMahzen.UI
{
    /// <summary>
    /// TODO 4.2: the pause menu (placeholder IMGUI art). Created by <see cref="HudBootstrap"/>, persistent; Esc opens it only while a match
    /// runs. Open = <see cref="MenuFocus"/> takes the input (gameplay maps off, cursor free; offline the game time stops). Buttons: resume,
    /// settings, main menu, quit. Leaving the scene closes it.
    /// </summary>
    public sealed class PauseMenuView : MonoBehaviour
    {
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private float buttonWidth = 280f;
        [SerializeField] private float buttonHeight = 44f;
        [SerializeField] private float settingsWidth = 420f;
        [SerializeField] private float settingsHeight = 460f;
        [SerializeField] private int titleFontSize = 32;
        [Tooltip("IMGUI depth: lower draws on top of the HUD.")]
        [SerializeField] private int drawDepth = -10;

        private readonly MenuModel _menu = new MenuModel(true);
        private GUIStyle _title;

        public bool IsOpen => _menu.IsOpen;

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
        }

        private void OnSceneChanged(Scene from, Scene to)
        {
            _menu.Close();
            SyncFocus();
        }

        private void Update()
        {
            ILocalInputProvider input = GameServices.Get<ILocalInputProvider>();
            if (input == null || !input.PausePressed)
                return;
            if (!_menu.IsOpen && !MatchRunning())
                return;
            _menu.Escape();
            SyncFocus();
        }

        private static bool MatchRunning()
        {
            IMatchInfo match = GameServices.Match;
            return match != null && !(match is StubMatchInfo) && match.State != MatchState.Lobby;
        }

        /// <summary>Opens or closes the menu (debug / MCP; the same as Esc).</summary>
        public void Toggle()
        {
            if (!_menu.IsOpen && !MatchRunning())
                return;
            _menu.Escape();
            SyncFocus();
        }

        private void SyncFocus()
        {
            MenuFocus focus = GameServices.Get<MenuFocus>();
            focus?.SetOpen(_menu.IsOpen, true);
        }

        private void OnGUI()
        {
            if (!_menu.IsOpen)
                return;
            GUI.depth = drawDepth;
            if (_title == null)
                _title = new GUIStyle(GUI.skin.label) { fontSize = titleFontSize, alignment = TextAnchor.MiddleCenter };

            GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none);
            float y = Screen.height * 0.3f;
            GUI.Label(new Rect(0f, y - 70f, Screen.width, 50f), Loc.T("pause.title"), _title);

            if (_menu.Screen == MenuScreen.Settings)
            {
                var area = new Rect((Screen.width - settingsWidth) * 0.5f, y, settingsWidth, settingsHeight);
                if (SettingsPanel.Draw(area, GameServices.Get<GameSettings>(), buttonHeight))
                    _menu.Back();
                return;
            }

            float x = (Screen.width - buttonWidth) * 0.5f;
            float step = buttonHeight + 10f;
            if (GUI.Button(new Rect(x, y, buttonWidth, buttonHeight), Loc.T("pause.resume")))
            {
                _menu.Close();
                SyncFocus();
            }
            if (GUI.Button(new Rect(x, y + step, buttonWidth, buttonHeight), Loc.T("menu.settings")))
                _menu.ShowSettings();
            if (GUI.Button(new Rect(x, y + step * 2f, buttonWidth, buttonHeight), Loc.T("pause.main_menu")))
            {
                _menu.Close();
                SyncFocus();
                SceneManager.LoadScene(mainMenuScene);
            }
            if (GUI.Button(new Rect(x, y + step * 3f, buttonWidth, buttonHeight), Loc.T("menu.quit")))
                Application.Quit();
        }
    }
}
