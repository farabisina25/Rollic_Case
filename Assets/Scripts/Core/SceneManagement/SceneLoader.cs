using UnityEngine.SceneManagement;

namespace BlockJam.Core.SceneManagement
{
    public static class SceneLoader
    {
        public static void LoadMenu() => SceneManager.LoadScene(SceneNames.Menu);
        public static void LoadGameplay() => SceneManager.LoadScene(SceneNames.Gameplay);
    }
}
