using UnityEngine;
using UnityEngine.SceneManagement;

namespace Backrooms
{
    public class ZoneSceneLoader : MonoBehaviour
    {
        public string[] zoneScenes;

        void Awake()
        {
            // 에디터에서 구역 씬을 모두 열어 둔 채 Play하면, 이 Awake 시점엔 다른 씬이 아직 로드 중(isLoaded=false)이다.
            // 그때 또 불러오면 같은 구역이 두 번 생기므로, 이미 존재하는(로드 중 포함) 씬은 건너뛴다.
            foreach (var scene in zoneScenes)
                if (!SceneManager.GetSceneByName(scene).IsValid())
                    SceneManager.LoadScene(scene, LoadSceneMode.Additive);
        }
    }
}
