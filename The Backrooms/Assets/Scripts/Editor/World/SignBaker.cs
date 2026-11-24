using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Backrooms.EditorTools.World
{
    public static class SignBaker
    {
        const int Layer = 31;

        public static Texture2D Bake(string path, string text, Color background, Color foreground, Vector2 size)
        {
            if (File.Exists(path))
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            QuestProjectSetup.EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            int w = 1024, h = Mathf.Clamp(Mathf.RoundToInt(1024 * size.y / size.x), 64, 1024);
            var root = new GameObject("SignBakeRoot") { hideFlags = HideFlags.HideAndDontSave };
            root.transform.position = new Vector3(0, -5000, 0);

            var camGo = new GameObject("SignBakeCamera");
            camGo.transform.SetParent(root.transform, false);
            camGo.transform.localPosition = new Vector3(0, 0, -10);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 0.5f;
            cam.aspect = (float)w / h;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.cullingMask = 1 << Layer;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 20f;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = false;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var textGo = new GameObject("SignBakeText") { layer = Layer };
            textGo.transform.SetParent(root.transform, false);
            var tm = textGo.AddComponent<TextMesh>();
            tm.font = font;
            tm.fontSize = 128;
            tm.characterSize = 0.05f;
            tm.text = text;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = foreground;
            var mr = textGo.GetComponent<MeshRenderer>();
            mr.sharedMaterial = font.material;

            var b = mr.bounds;
            float fit = Mathf.Min(cam.aspect * 0.88f / Mathf.Max(0.001f, b.size.x), 0.8f / Mathf.Max(0.001f, b.size.y));
            textGo.transform.localScale = Vector3.one * fit;

            var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            cam.targetTexture = rt;
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            cam.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(root);

            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
