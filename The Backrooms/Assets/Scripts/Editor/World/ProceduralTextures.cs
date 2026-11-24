using System.IO;
using UnityEditor;
using UnityEngine;

namespace Backrooms.EditorTools.World
{
    public static class ProceduralTextures
    {
        public static Texture2D BackroomsWallpaper(string path)
        {
            return Create(path, 1024, 1024, false, (x, y, w, h) =>
            {
                float u = (float)x / w, v = (float)y / h;
                var baseCol = new Color(0.84f, 0.81f, 0.56f);

                float blot = TileNoise(u, v, 3f) * 0.6f + TileNoise(u, v, 9f) * 0.3f + TileNoise(u, v, 27f) * 0.1f;
                var col = baseCol * Mathf.Lerp(0.965f, 1.02f, blot);

                float stripe = (u * 8f) % 1f;
                float px = stripe * 128f;
                bool line = (px > 2 && px < 6) || (px > 12 && px < 16);
                if (line) col = Color.Lerp(col, new Color(0.74f, 0.64f, 0.34f), 0.55f);

                float cx = (px - 72f) / 18f;
                float cy = ((v * 16f) % 1f) * 64f / 18f;
                if (Mathf.Abs(cx) < 1f)
                {
                    float vShape = Mathf.Abs(cy - 1.2f - Mathf.Abs(cx) * 0.8f);
                    if (vShape < 0.12f) col = Color.Lerp(col, new Color(0.93f, 0.85f, 0.56f), 0.6f);
                }
                return col;
            });
        }

        public static Texture2D TrofferPanel(string path)
        {
            return Create(path, 256, 512, false, (x, y, w, h) =>
            {
                float u = (float)x / w, v = (float)y / h;
                float edge = Mathf.Min(Mathf.Min(u, 1 - u) * 0.6f, Mathf.Min(v, 1 - v) * 1.2f);
                if (edge < 0.025f) return new Color(0.82f, 0.82f, 0.8f);
                bool louver = Mathf.Abs(u - 0.5f) < 0.012f || Mathf.Abs((v * 4f) % 1f - 0.5f) < 0.02f;
                float glow = 1f - 0.12f * Mathf.Pow(Mathf.Abs(u - 0.5f) * 2f, 2f);
                return louver ? new Color(0.88f, 0.88f, 0.86f) : new Color(1f, 0.99f, 0.93f) * glow;
            });
        }

        public static Texture2D WaterStain(string path, int seed)
        {
            return Create(path, 512, 512, true, (x, y, w, h) =>
            {
                float u = (float)x / w - 0.5f, v = (float)y / h - 0.5f;
                float r = Mathf.Sqrt(u * u + v * v) * 2f;
                float n = Mathf.PerlinNoise(u * 4f + seed * 3.1f, v * 4f + seed * 1.7f) * 0.6f + Mathf.PerlinNoise(u * 13f + seed, v * 13f - seed) * 0.4f;
                float shape = Mathf.Clamp01((1f - r) * 1.6f + (n - 0.5f) * 1.4f);
                float ring = Mathf.Clamp01(1f - Mathf.Abs(shape - 0.08f) * 14f) * 0.5f;
                float a = Mathf.Clamp01(shape * 0.55f + ring) * (shape > 0 ? 1f : 0f);
                return new Color(0.36f, 0.27f, 0.12f, a * 0.75f);
            });
        }

        public static Texture2D SafetyStripe(string path)
        {
            return Create(path, 512, 64, false, (x, y, w, h) =>
            {
                float d = (x + y) / 48f;
                bool yellow = (d % 2f) < 1f;
                float wear = Mathf.PerlinNoise(x * 0.05f, y * 0.2f);
                var c = yellow ? new Color(0.95f, 0.78f, 0.1f) : new Color(0.08f, 0.08f, 0.08f);
                return Color.Lerp(c, new Color(0.5f, 0.5f, 0.48f), wear > 0.72f ? 0.6f : 0f);
            });
        }

        public static Texture2D Stripes(string path, Color a, Color b, int count)
        {
            return Create(path, 512, 64, false, (x, y, w, h) => ((x * count / w) % 2 == 0) ? a : b);
        }

        public static Texture2D Speckle(string path, Color c, float amount)
        {
            return Create(path, 256, 256, false, (x, y, w, h) => c * (1f + (TileNoise((float)x / w, (float)y / h, 31f) - 0.5f) * amount));
        }

        public static Texture2D ExitSign(string path)
        {
            return Create(path, 256, 128, false, (x, y, w, h) =>
            {
                float u = (float)x / w, v = (float)y / h;
                var green = new Color(0.05f, 0.62f, 0.3f);
                if (u < 0.03f || u > 0.97f || v < 0.06f || v > 0.94f) return Color.white;
                float ax = (u - 0.62f) / 0.25f, ay = (v - 0.5f) / 0.35f;
                bool arrow = (ax > -1f && ax < 0.2f && Mathf.Abs(ay) < 0.18f) || (ax >= 0.2f && ax < 1f && Mathf.Abs(ay) < (1f - ax) * 0.8f);
                float hx = (u - 0.25f) / 0.06f, hy = (v - 0.72f) / 0.12f;
                bool head = hx * hx + hy * hy < 1f;
                bool body = Mathf.Abs(u - 0.25f) < 0.035f && v > 0.25f && v < 0.62f;
                return arrow || head || body ? Color.white : green;
            });
        }

        public static Texture2D DrywallBack(string path)
        {
            return Create(path, 512, 1024, false, (x, y, w, h) =>
            {
                float u = (float)x / w, v = (float)y / h;
                var c = new Color(0.66f, 0.63f, 0.57f);
                float n = Mathf.PerlinNoise(u * 18f, v * 36f) * 0.06f + Mathf.PerlinNoise(u * 90f, v * 180f) * 0.04f;
                c *= 0.95f + n;
                if (u < 0.008f || u > 0.992f) c *= 0.72f;
                if (Mathf.Abs(u - 0.5f) < 0.004f) c *= 0.9f;
                float row = (v * 8f) % 1f;
                if (row > 0.48f && row < 0.5f && ((int)(u * 40f) % 3 != 0) && u > 0.2f && u < 0.8f) c = Color.Lerp(c, new Color(0.25f, 0.35f, 0.55f), 0.35f);
                if (Mathf.PerlinNoise(u * 4f + 7f, v * 3f) > 0.72f) c *= 0.88f;
                return c;
            });
        }

        public static Texture2D BarrierTape(string path)
        {
            return Create(path, 512, 32, false, (x, y, w, h) =>
            {
                var yellow = new Color(0.98f, 0.82f, 0.08f);
                if (y < 2 || y > h - 3) return yellow * 0.9f;
                int block = (x / 16) % 8;
                bool ink = block < 5 && (x % 16) > 3 && y > 8 && y < h - 8 && ((x / 4 + y / 6) % 3 != 0);
                return ink ? new Color(0.06f, 0.06f, 0.06f) : yellow;
            });
        }

        public static Texture2D PlasticSheet(string path)
        {
            return Create(path, 128, 512, true, (x, y, w, h) =>
            {
                float u = (float)x / w, v = (float)y / h;
                float streak = Mathf.PerlinNoise(u * 30f, v * 2f) * 0.25f + Mathf.PerlinNoise(u * 6f, v * 0.5f) * 0.2f;
                float dirt = v < 0.12f ? (0.12f - v) * 2.5f : 0f;
                return new Color(0.9f - dirt, 0.93f - dirt, 0.92f - dirt * 1.2f, 0.18f + streak + dirt);
            });
        }

        public static Texture2D ChainLink(string path)
        {
            return Create(path, 256, 256, true, (x, y, w, h) =>
            {
                float u = (float)x / w * 4f, v = (float)y / h * 4f;
                float a = Mathf.Abs(((u + v) % 1f) - 0.5f), b = Mathf.Abs(((u - v + 8f) % 1f) - 0.5f);
                bool wire = a > 0.455f || b > 0.455f;
                return wire ? new Color(0.62f, 0.64f, 0.66f, 1f) : new Color(0.5f, 0.5f, 0.5f, 0f);
            });
        }

        public static Texture2D WaterRippleNormal(string path)
        {
            const int n = 256;
            var height = new float[n, n];
            var waves = new (int fx, int fy, float amp, float phase)[] { (3, 1, 1f, 0.3f), (1, 4, 0.8f, 1.7f), (5, 3, 0.45f, 2.9f), (7, -2, 0.3f, 0.6f), (2, 7, 0.25f, 4.1f), (11, 5, 0.12f, 1.1f) };
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (float)x / n, v = (float)y / n, h = 0;
                foreach (var w in waves) h += w.amp * Mathf.Sin((u * w.fx + v * w.fy) * Mathf.PI * 2f + w.phase);
                height[x, y] = h;
            }
            return Create(path, n, n, false, (x, y, w, h) =>
            {
                float dx = height[(x + 1) % n, y] - height[(x - 1 + n) % n, y];
                float dy = height[x, (y + 1) % n] - height[x, (y - 1 + n) % n];
                var normal = new Vector3(-dx * 0.6f, -dy * 0.6f, 1f).normalized;
                return new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f);
            });
        }

        public static Texture2D LightConeFade(string path)
        {
            return Create(path, 32, 256, true, (x, y, w, h) =>
            {
                float v = (float)y / (h - 1);
                float a = Mathf.Pow(v, 2.2f) * 0.9f + 0.02f;
                return new Color(1f, 1f, 1f, Mathf.Clamp01(a));
            });
        }

        public static Texture2D CameraFeed(string path, int seed)
        {
            return Create(path, 256, 160, false, (x, y, w, h) =>
            {
                float u = (float)x / w, v = (float)y / h;
                var wall = new Color(0.52f, 0.5f, 0.36f);
                float horizon = 0.38f + 0.04f * Mathf.Sin(seed);
                Color c = v < horizon ? new Color(0.24f, 0.22f, 0.16f) * (0.7f + v) : wall * (1.1f - Mathf.Abs(u - 0.5f) * 0.6f);
                float door = Mathf.Abs(u - (0.35f + 0.3f * ((seed * 37) % 10) / 10f));
                if (door < 0.06f && v > horizon && v < horizon + 0.36f) c = new Color(0.1f, 0.1f, 0.08f);
                if (v > 0.9f && Mathf.Abs((u * 5f) % 1f - 0.5f) < 0.12f) c = new Color(0.85f, 0.85f, 0.75f);
                float n = Mathf.PerlinNoise(x * 0.9f + seed * 13f, y * 0.9f) * 0.12f;
                float scan = (y % 3 == 0) ? 0.82f : 1f;
                return (c + new Color(n, n, n)) * scan * 0.9f;
            });
        }

        public delegate Color Painter(int x, int y, int w, int h);

        public static Texture2D Create(string path, int w, int h, bool alpha, Painter paint)
        {
            if (!File.Exists(path))
            {
                QuestProjectSetup.EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
                var tex = new Texture2D(w, h, alpha ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
                var px = new Color[w * h];
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = paint(x, y, w, h);
                tex.SetPixels(px);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.alphaIsTransparency = alpha;
                importer.wrapMode = alpha ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static float TileNoise(float u, float v, float freq)
        {
            float s = 0;
            float f = Mathf.Round(freq);
            s += Mathf.Sin((u * f + Mathf.Sin(v * 6.2831f * 2f) * 0.3f) * 6.2831f) * 0.5f;
            s += Mathf.Sin((v * (f + 1f) + Mathf.Sin(u * 6.2831f * 3f) * 0.2f) * 6.2831f) * 0.35f;
            s += Mathf.Sin(((u * 2f + v) * Mathf.Max(1f, Mathf.Round(f * 0.5f))) * 6.2831f) * 0.15f;
            return s * 0.5f + 0.5f;
        }
    }
}
