using UnityEngine;
using UnityEngine.SceneManagement;

namespace CinliMahzen.Core
{
    /// <summary>Lives in Boot.unity: services are already up (GameBootstrap), so just move on.</summary>
    public sealed class BootSceneLoader : MonoBehaviour
    {
        [SerializeField] private string nextScene = "MainMenu";

        private void Start()
        {
            SceneManager.LoadScene(nextScene);
        }
    }
}
