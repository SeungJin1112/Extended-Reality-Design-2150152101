using UnityEngine;
using UnityEngine.SceneManagement;

namespace Backrooms
{
    public class ZoneSceneLoader : MonoBehaviour
    {
        public string[] zoneScenes;

        void Awake()
        {
            foreach (var scene in zoneScenes)
                if (!SceneManager.GetSceneByName(scene).isLoaded)
                    SceneManager.LoadScene(scene, LoadSceneMode.Additive);
        }
    }
}
