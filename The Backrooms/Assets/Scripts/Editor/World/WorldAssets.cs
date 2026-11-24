using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Backrooms.EditorTools.World
{
    public static class WorldAssets
    {
        public static Material SetMaterial(string zoneFolder, string name, string textureSetFolder, string set, float metersPerTile, Color tint, float smoothness = -1f, float normalScale = 1f)
        {
            var dir = $"{textureSetFolder}/{set}";
            return Lit(zoneFolder, name, tint,
                AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{set}BaseColor.jpg"),
                AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{set}Normal.jpg"),
                AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{set}MetallicSmoothness.png"),
                metersPerTile, smoothness, normalScale);
        }

        public static Material Lit(string zoneFolder, string name, Color tint, Texture2D baseMap = null, Texture2D normal = null, Texture2D metallicSmoothness = null,
            float metersPerTile = 1f, float smoothness = 0.3f, float normalScale = 1f, float metallic = 0f)
        {
            var mat = LoadOrCreate(zoneFolder, name, "Universal Render Pipeline/Lit");
            mat.SetColor("_BaseColor", tint);
            mat.SetTexture("_BaseMap", baseMap);
            mat.SetTextureScale("_BaseMap", Vector2.one / Mathf.Max(0.01f, metersPerTile));
            mat.SetFloat("_Metallic", metallic);
            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.SetFloat("_BumpScale", normalScale);
                mat.EnableKeyword("_NORMALMAP");
            }
            else mat.DisableKeyword("_NORMALMAP");
            if (metallicSmoothness != null && smoothness < 0f)
            {
                mat.SetTexture("_MetallicGlossMap", metallicSmoothness);
                mat.SetFloat("_Smoothness", 1f);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            else
            {
                mat.SetTexture("_MetallicGlossMap", null);
                mat.DisableKeyword("_METALLICSPECGLOSSMAP");
                mat.SetFloat("_Smoothness", smoothness < 0f ? 0.3f : smoothness);
            }
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static Material Unlit(string zoneFolder, string name, Color color, Texture2D tex = null)
        {
            var mat = LoadOrCreate(zoneFolder, name, "Universal Render Pipeline/Unlit");
            mat.SetColor("_BaseColor", color);
            mat.SetTexture("_BaseMap", tex);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static Material Transparent(string zoneFolder, string name, Color color, Texture2D tex = null, float smoothness = 0.2f, bool unlit = false)
        {
            var mat = LoadOrCreate(zoneFolder, name, unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            mat.SetColor("_BaseColor", color);
            mat.SetTexture("_BaseMap", tex);
            if (!unlit) mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetOverrideTag("RenderType", "Transparent");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material LoadOrCreate(string zoneFolder, string name, string shader)
        {
            var dir = $"{zoneFolder}/Materials/World";
            QuestProjectSetup.EnsureFolder(dir);
            var path = $"{dir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find(shader)) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            else mat.shader = Shader.Find(shader);
            return mat;
        }

        public static Material Cutout(string zoneFolder, string name, Color tint, Texture2D tex, float metersPerTile, float smoothness = 0.5f, float metallic = 0.8f)
        {
            var mat = LoadOrCreate(zoneFolder, name, "Universal Render Pipeline/Lit");
            mat.SetColor("_BaseColor", tint);
            mat.SetTexture("_BaseMap", tex);
            mat.SetTextureScale("_BaseMap", Vector2.one / metersPerTile);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_AlphaClip", 1f);
            mat.SetFloat("_Cutoff", 0.5f);
            mat.SetFloat("_Cull", 0f);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.renderQueue = (int)RenderQueue.AlphaTest;
            mat.SetOverrideTag("RenderType", "TransparentCutout");
            mat.doubleSidedGI = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static Material Additive(string zoneFolder, string name, Color color, Texture2D tex)
        {
            var mat = LoadOrCreate(zoneFolder, name, "Universal Render Pipeline/Unlit");
            mat.SetColor("_BaseColor", color);
            mat.SetTexture("_BaseMap", tex);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 2f);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_Cull", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.One);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_BLENDMODE_ADD");
            mat.renderQueue = (int)RenderQueue.Transparent + 10;
            mat.SetOverrideTag("RenderType", "Transparent");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static Texture2D Tex(string path) => AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        public static Mesh SaveMesh(Mesh mesh, string zoneFolder, string name)
        {
            var dir = $"{zoneFolder}/Meshes/Generated";
            QuestProjectSetup.EnsureFolder(dir);
            var path = $"{dir}/{name}.asset";
            mesh.name = name;
            Unwrapping.GenerateSecondaryUVSet(mesh);
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(mesh, existing);
                Object.DestroyImmediate(mesh);
                return existing;
            }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        public static Mesh Cone(float radius, float height, int segments, bool open = false)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            float slope = radius / height;
            for (int i = 0; i <= segments; i++)
            {
                float a = (float)i / segments * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var normal = (dir + Vector3.up * slope).normalized;
                v.Add(dir * radius); n.Add(normal); uv.Add(new Vector2((float)i / segments, 0));
                v.Add(Vector3.up * height); n.Add(normal); uv.Add(new Vector2((float)i / segments, 1));
            }
            for (int i = 0; i < segments; i++)
            {
                int b = i * 2;
                tri.AddRange(new[] { b, b + 1, b + 2, b + 2, b + 1, b + 3 });
            }
            if (!open)
            {
                int c = v.Count; v.Add(Vector3.zero); n.Add(Vector3.down); uv.Add(new Vector2(0.5f, 0.5f));
                for (int i = 0; i <= segments; i++)
                {
                    float a = (float)i / segments * Mathf.PI * 2f;
                    v.Add(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius); n.Add(Vector3.down); uv.Add(new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f));
                }
                for (int i = 0; i < segments; i++) tri.AddRange(new[] { c, c + 1 + i, c + 2 + i });
            }
            return Build(v, n, uv, tri);
        }

        public static Mesh Cylinder(float radius, float height, int segments, bool caps = true)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            float circ = Mathf.PI * 2f * radius;
            for (int i = 0; i <= segments; i++)
            {
                float a = (float)i / segments * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                v.Add(dir * radius); n.Add(dir); uv.Add(new Vector2((float)i / segments * circ, 0));
                v.Add(dir * radius + Vector3.up * height); n.Add(dir); uv.Add(new Vector2((float)i / segments * circ, height));
            }
            for (int i = 0; i < segments; i++)
            {
                int b = i * 2;
                tri.AddRange(new[] { b, b + 1, b + 2, b + 2, b + 1, b + 3 });
            }
            if (caps)
            {
                foreach (var top in new[] { false, true })
                {
                    int c = v.Count; float y = top ? height : 0f;
                    v.Add(Vector3.up * y); n.Add(top ? Vector3.up : Vector3.down); uv.Add(Vector2.zero);
                    for (int i = 0; i <= segments; i++)
                    {
                        float a = (float)i / segments * Mathf.PI * 2f;
                        var p = new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius);
                        v.Add(p); n.Add(top ? Vector3.up : Vector3.down); uv.Add(new Vector2(p.x, p.z));
                    }
                    for (int i = 0; i < segments; i++)
                        tri.AddRange(top ? new[] { c, c + 2 + i, c + 1 + i } : new[] { c, c + 1 + i, c + 2 + i });
                }
            }
            return Build(v, n, uv, tri);
        }

        public static Mesh Torus(float radius, float tube, int segments, int sides)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            for (int i = 0; i <= segments; i++)
            {
                float u = (float)i / segments * Mathf.PI * 2f;
                var center = new Vector3(Mathf.Cos(u), 0, Mathf.Sin(u)) * radius;
                for (int j = 0; j <= sides; j++)
                {
                    float a = (float)j / sides * Mathf.PI * 2f;
                    var nn = new Vector3(Mathf.Cos(u) * Mathf.Cos(a), Mathf.Sin(a), Mathf.Sin(u) * Mathf.Cos(a));
                    v.Add(center + nn * tube); n.Add(nn); uv.Add(new Vector2((float)i / segments, (float)j / sides));
                }
            }
            for (int i = 0; i < segments; i++)
            for (int j = 0; j < sides; j++)
            {
                int a = i * (sides + 1) + j, b = a + sides + 1;
                tri.AddRange(new[] { a, a + 1, b, b, a + 1, b + 1 });
            }
            return Build(v, n, uv, tri);
        }

        static Mesh Build(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> tri)
        {
            var m = new Mesh();
            m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
            m.RecalculateTangents(); m.RecalculateBounds();
            return m;
        }

        public static GameObject MeshGo(string name, Mesh mesh, Material mat, Transform parent, Vector3 pos, Vector3 euler, Vector3 scale, bool isStatic = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (isStatic) ZoneKit.MakeStatic(go);
            return go;
        }

        public static GameObject Sign(Transform parent, string folder, string name, Vector3 pos, float rotY, Vector2 size, Material board, string text, Color textColor, float charSize)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;
            root.transform.localRotation = Quaternion.Euler(0, rotY, 0);
            var plate = new GameObject("Plate");
            plate.transform.SetParent(root.transform, false);
            plate.transform.localScale = new Vector3(size.x, size.y, 0.02f);
            plate.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            plate.AddComponent<MeshRenderer>().sharedMaterial = board;
            ZoneKit.MakeStatic(plate);
            var bg = board.HasProperty("_BaseColor") ? board.GetColor("_BaseColor") : Color.white;
            var tex = SignBaker.Bake($"{folder}/Textures/Generated/Signs/{name}Sign.png", text, bg, textColor, size);
            var face = Lit(folder, $"{name}SignMaterial", Color.white, tex, smoothness: 0.35f);
            face.SetTextureScale("_BaseMap", Vector2.one);
            var quad = new GameObject("Face");
            quad.transform.SetParent(root.transform, false);
            quad.transform.localPosition = new Vector3(0, 0, -0.0105f);
            quad.transform.localScale = new Vector3(size.x * 0.99f, size.y * 0.99f, 1f);
            quad.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var r = quad.AddComponent<MeshRenderer>();
            r.sharedMaterial = face;
            r.receiveGI = ReceiveGI.LightProbes;
            GameObjectUtility.SetStaticEditorFlags(quad, StaticEditorFlags.BatchingStatic);
            return root;
        }

        public static void SetLightmapScale(GameObject go, float scale)
        {
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
            {
                var so = new SerializedObject(r);
                so.FindProperty("m_ScaleInLightmap").floatValue = scale;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        public static void UseProbes(GameObject go)
        {
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) r.receiveGI = ReceiveGI.LightProbes;
        }
    }
}
