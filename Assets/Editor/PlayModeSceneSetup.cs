using UnityEditor;
using UnityEditor.SceneManagement;

namespace Arcade.Editor
{
    /// <summary>
    /// Configures Unity Editor to always launch into LV_BlockBreaker_MainMenu when pressing Play,
    /// regardless of which scene is currently open or being edited.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeSceneSetup
    {
        private const string MainMenuScenePath = "Assets/Scenes/LV_BlockBreaker_MainMenu.unity";

        static PlayModeSceneSetup()
        {
            ConfigurePlayModeStartScene();
        }

        [MenuItem("Tools/Arcade/Set Play Mode Start Scene to Main Menu")]
        public static void ConfigurePlayModeStartScene()
        {
            var menuScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);
            if (menuScene != null)
            {
                EditorSceneManager.playModeStartScene = menuScene;
            }
        }
    }
}
