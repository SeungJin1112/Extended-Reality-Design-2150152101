using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Backrooms.EditorTools
{
    [InitializeOnLoad]
    public static class BackroomsStartupScene
    {
        const string ScenePath = "Assets/Scenes/Backrooms.unity";
        const string SessionKey = "Backrooms.StartupSceneChecked";

        static BackroomsStartupScene()
        {
            if (SessionState.GetBool(SessionKey, false))
                return;
            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += OpenIfEmpty;
        }

        static void OpenIfEmpty()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(ScenePath))
                return;

            var active = SceneManager.GetActiveScene();
            bool isUntitled = string.IsNullOrEmpty(active.path);
            bool isSample = active.path == "Assets/Scenes/SampleScene.unity";
            bool isMainOnly = active.path == ScenePath;
            if (SceneManager.sceneCount != 1 || active.isDirty || !(isUntitled || isSample || isMainOnly))
                return;

            World.BackroomsWorldBuilder.OpenWorld();
        }
    }
}
