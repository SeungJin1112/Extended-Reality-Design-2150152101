using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Backrooms.EditorTools
{
    /// <summary>
    /// 프로젝트를 처음 열었을 때(빈 Untitled 씬 / SampleScene) 자동으로 Backrooms 씬을 연다.
    /// XR Interaction Simulator는 Play 시 모든 씬에 자동 생성되는데,
    /// XR Origin이 없는 씬에서는 NullReferenceException이 나므로 공통 씬으로 바로 안내한다.
    /// 에디터 세션당 한 번만 동작하며, 작업 중인 씬은 건드리지 않는다.
    /// </summary>
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
            if (SceneManager.sceneCount != 1 || active.isDirty || !(isUntitled || isSample))
                return;

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
    }
}
