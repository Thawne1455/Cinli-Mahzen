using CinliMahzen.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CinliMahzen.UI
{
    /// <summary>Placeholder main menu (A0.9) until A3.1: offline hotseat test game, online (M4, disabled), quit.</summary>
    public sealed class MainMenuPlaceholder : MonoBehaviour
    {
        [SerializeField] private string gameScene = "Game";
        [SerializeField] private float buttonWidth = 280f;
        [SerializeField] private float buttonHeight = 44f;

        private GUIStyle _title;

        private void OnGUI()
        {
            if (_title == null)
                _title = new GUIStyle(GUI.skin.label) { fontSize = 36, alignment = TextAnchor.MiddleCenter };

            float x = (Screen.width - buttonWidth) * 0.5f;
            float y = Screen.height * 0.3f;
            GUI.Label(new Rect(0f, y - 80f, Screen.width, 60f), Loc.T("menu.title"), _title);

            if (GUI.Button(new Rect(x, y, buttonWidth, buttonHeight), Loc.T("menu.play_hotseat")))
                SceneManager.LoadScene(gameScene);

            GUI.enabled = false;
            GUI.Button(new Rect(x, y + buttonHeight + 10f, buttonWidth, buttonHeight), Loc.T("menu.online"));
            GUI.enabled = true;

            if (GUI.Button(new Rect(x, y + (buttonHeight + 10f) * 2f, buttonWidth, buttonHeight), Loc.T("menu.quit")))
                Application.Quit();
        }
    }
}
