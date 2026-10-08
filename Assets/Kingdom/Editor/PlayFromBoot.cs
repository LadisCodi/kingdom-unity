using Kingdom.Game.Startup;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Kingdom.Editor
{
    // Play in the editor always starts from the Boot scene, as the game does, whatever scene is open.
    [InitializeOnLoad]
    public static class PlayFromBoot
    {
        private const string BOOT_SCENE_PATH = "Assets/Kingdom/Scenes/" + SceneNames.BOOT + ".unity";

        static PlayFromBoot()
        {
            EditorApplication.delayCall += Apply;
        }

        private static void Apply()
        {
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BOOT_SCENE_PATH);
        }
    }
}
