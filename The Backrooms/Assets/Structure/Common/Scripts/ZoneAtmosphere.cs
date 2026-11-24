using UnityEngine;

namespace Backrooms
{
    public class ZoneAtmosphere : MonoBehaviour
    {
        public bool applyOnStart;
        public Color fogColor = new Color(0.45f, 0.41f, 0.26f);
        public float fogStart = 8f;
        public float fogEnd = 45f;
        public Color backgroundColor = new Color(0.45f, 0.41f, 0.26f);
        public float farClip = 80f;

        void Start()
        {
            if (applyOnStart)
                Apply();
        }

        public void Apply()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;

            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = backgroundColor;
                cam.farClipPlane = farClip;
            }
        }
    }
}
