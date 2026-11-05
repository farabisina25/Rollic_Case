using System.IO;
using BlockJam.Core.SceneManagement;
using BlockJam.Gameplay;
using BlockJam.Gameplay.Data;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BlockJam.Editor.LevelEditor
{
    // Opens the Gameplay scene in Play mode with one chosen level. The choice is kept in SessionState
    // because entering Play mode reloads the script domain and clears static fields.
    [InitializeOnLoad]
    public static class LevelPlaytest
    {
        private const string LevelGuidKey = "BlockJam.PlaytestLevelGuid";

        static LevelPlaytest()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                ApplyOverride();
            }
        }

        public static void Play(LevelData level)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            string scenePath = FindScenePath(SceneNames.Gameplay);
            if (scenePath == null)
            {
                EditorUtility.DisplayDialog("Play", $"The {SceneNames.Gameplay} scene is not in the Build Settings.", "OK");
                return;
            }

            EditorSceneManager.OpenScene(scenePath);
            SessionState.SetString(LevelGuidKey, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(level)));
            PlaytestLevel.Override = level;
            EditorApplication.EnterPlaymode();
        }

        private static void ApplyOverride()
        {
            string guid = SessionState.GetString(LevelGuidKey, string.Empty);
            if (guid.Length > 0)
            {
                PlaytestLevel.Override = AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid));
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.EraseString(LevelGuidKey);
                PlaytestLevel.Override = null;
            }
        }

        private static string FindScenePath(string sceneName)
        {
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (Path.GetFileNameWithoutExtension(scene.path) == sceneName)
                {
                    return scene.path;
                }
            }

            return null;
        }
    }
}
