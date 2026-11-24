using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Backrooms.EditorTools
{
    public static class AssetLibraryProcessor
    {
        public static readonly string[] Categories = { "Player", "Structure", "Objects" };
        public static readonly string[] Zones = { "Common", "ResearchBase", "Level0", "AmusementPark", "PoolRooms" };
        const string ReportPath = "Assets/AssetLibraryReport.txt";

        [MenuItem("Backrooms/4. Process Asset Library", priority = 20)]
        public static void Run()
        {
            AssetDatabase.Refresh();
            int models = 0, materials = 0;
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var leaf in Leaves())
                    PrepareModelImporters(leaf);
            }
            finally { AssetDatabase.StopAssetEditing(); }
            AssetDatabase.Refresh();

            foreach (var leaf in Leaves())
            {
                materials += ProcessTextureSets(leaf);
                models += ProcessModels(leaf);
            }

            AssetDatabase.SaveAssets();
            WriteReport();
            Debug.Log($"[Backrooms] Asset library processed: {models} model prefabs, {materials} texture materials (new). Report: {ReportPath}");
        }

        [MenuItem("Backrooms/Fix Oversized Colliders", priority = 23)]
        public static void FixOversizedColliders()
        {
            int fixedCount = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Objects", "Assets/Structure", "Assets/Player" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                bool changed = false;
                var meshBounds = new Bounds();
                bool hasMesh = false;
                foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mf.sharedMesh == null) continue;
                    var b = TransformBounds(mf.sharedMesh.bounds, root.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix);
                    if (!hasMesh) { meshBounds = b; hasMesh = true; } else meshBounds.Encapsulate(b);
                }
                foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (smr.sharedMesh == null) continue;
                    var b = TransformBounds(smr.sharedMesh.bounds, root.transform.worldToLocalMatrix * smr.transform.localToWorldMatrix);
                    if (!hasMesh) { meshBounds = b; hasMesh = true; } else meshBounds.Encapsulate(b);
                }
                foreach (var box in root.GetComponents<BoxCollider>())
                {
                    if (!hasMesh) continue;
                    var s = box.size;
                    var m = meshBounds.size;
                    if (s.x > m.x * 1.5f + 0.1f || s.y > m.y * 1.5f + 0.1f || s.z > m.z * 1.5f + 0.1f)
                    {
                        box.center = meshBounds.center;
                        box.size = meshBounds.size;
                        changed = true;
                    }
                }
                if (changed) { PrefabUtility.SaveAsPrefabAsset(root, path); fixedCount++; }
                PrefabUtility.UnloadPrefabContents(root);
            }
            Debug.Log($"[Backrooms] Oversized colliders fixed: {fixedCount}");
        }

        static Bounds TransformBounds(Bounds b, Matrix4x4 m)
        {
            var result = new Bounds(m.MultiplyPoint3x4(b.center), Vector3.zero);
            for (int i = 0; i < 8; i++)
            {
                var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                result.Encapsulate(m.MultiplyPoint3x4(corner));
            }
            return result;
        }

        public static void RunAllBatch()
        {
            Run();
            DreamcorePropGenerator.Run();
            WriteReport();
        }

        public static IEnumerable<string> Leaves()
        {
            foreach (var c in Categories)
            foreach (var z in Zones)
            {
                var leaf = $"Assets/{c}/{z}";
                if (AssetDatabase.IsValidFolder(leaf))
                    yield return leaf;
            }
        }

        static bool IsStructure(string leaf) => leaf.StartsWith("Assets/Structure/");

        static void PrepareModelImporters(string leaf)
        {
            var modelsDir = $"{leaf}/Models";
            if (!AssetDatabase.IsValidFolder(modelsDir))
                return;

            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { modelsDir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is ModelImporter importer) || importer.userData == "BackroomsPrepared")
                    continue;

                importer.importCameras = false;
                importer.importLights = false;
                importer.importAnimation = false;
                importer.animationType = ModelImporterAnimationType.None;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                importer.generateSecondaryUV = IsStructure(leaf);
                importer.isReadable = false;
                importer.userData = "BackroomsPrepared";
                importer.SaveAndReimport();
            }
        }

        static int ProcessModels(string leaf)
        {
            var modelsDir = $"{leaf}/Models";
            if (!AssetDatabase.IsValidFolder(modelsDir))
                return 0;

            int created = 0;
            var prefabDir = $"{leaf}/Prefabs";
            foreach (var dir in AssetDatabase.GetSubFolders(modelsDir))
            {
                var name = Path.GetFileName(dir);
                var fbxPath = $"{dir}/{name}.fbx";
                var prefabPath = $"{prefabDir}/{name}.prefab";
                if (!File.Exists(fbxPath) || File.Exists(prefabPath))
                    continue;

                var sets = BuildModelMaterials(dir);
                RemapMaterials(fbxPath, sets);

                var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                if (model == null)
                    continue;

                QuestProjectSetup.EnsureFolder(prefabDir);
                var root = new GameObject(name);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                instance.name = "Model";

                var renderers = root.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    var bounds = renderers[0].bounds;
                    foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                    var box = root.AddComponent<BoxCollider>();
                    box.center = bounds.center;
                    box.size = bounds.size;

                    if (bounds.size.magnitude > 60f)
                        Debug.LogWarning($"[Backrooms] {name}: 크기가 비정상적으로 큽니다 ({bounds.size}). 스케일 확인 필요");
                }

                if (IsStructure(leaf))
                {
                    foreach (var t in root.GetComponentsInChildren<Transform>())
                        GameObjectUtility.SetStaticEditorFlags(t.gameObject,
                            StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic |
                            StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic);
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Object.DestroyImmediate(root);
                created++;
            }
            return created;
        }

        static Dictionary<string, Material> BuildModelMaterials(string modelDir)
        {
            var result = new Dictionary<string, Material>();
            var texDir = $"{modelDir}/Textures";
            if (!AssetDatabase.IsValidFolder(texDir))
                return result;

            var matDir = $"{modelDir}/Materials";
            foreach (var baseColorPath in Directory.GetFiles(texDir, "*BaseColor.jpg").Select(p => p.Replace('\\', '/')))
            {
                var set = Path.GetFileNameWithoutExtension(baseColorPath);
                set = set.Substring(0, set.Length - "BaseColor".Length);
                var mat = CreateLitMaterial(matDir, set, texDir, armPath: $"{texDir}/{set}Arm.jpg", roughnessPath: null);
                result[set] = mat;
            }
            return result;
        }

        static void RemapMaterials(string fbxPath, Dictionary<string, Material> sets)
        {
            if (sets.Count == 0 || !(AssetImporter.GetAtPath(fbxPath) is ModelImporter importer))
                return;

            var sourceNames = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Material>().Select(m => m.name).Distinct().ToList();
            bool changed = false;
            foreach (var src in sourceNames)
            {
                var match = BestMatch(src, sets.Keys);
                if (match == null) continue;
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), src), sets[match]);
                changed = true;
            }
            if (changed)
                importer.SaveAndReimport();
        }

        static string Normalize(string s) => new string(s.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

        static string BestMatch(string source, IEnumerable<string> sets)
        {
            var list = sets.ToList();
            if (list.Count == 1) return list[0];
            var n = Normalize(source);
            var exact = list.FirstOrDefault(s => Normalize(s) == n);
            if (exact != null) return exact;
            var contains = list.Where(s => n.Contains(Normalize(s)) || Normalize(s).Contains(n)).OrderByDescending(s => s.Length).FirstOrDefault();
            if (contains != null) return contains;
            return list.OrderByDescending(s => CommonPrefix(Normalize(s), n)).First();
        }

        static int CommonPrefix(string a, string b)
        {
            int i = 0;
            while (i < a.Length && i < b.Length && a[i] == b[i]) i++;
            return i;
        }

        static int ProcessTextureSets(string leaf)
        {
            var texRoot = $"{leaf}/Textures";
            if (!AssetDatabase.IsValidFolder(texRoot))
                return 0;

            int created = 0;
            var matDir = $"{leaf}/Materials";
            foreach (var dir in AssetDatabase.GetSubFolders(texRoot))
            {
                var set = Path.GetFileName(dir);
                if (!File.Exists($"{dir}/{set}BaseColor.jpg") || File.Exists($"{matDir}/{set}Material.mat"))
                    continue;
                CreateLitMaterial(matDir, set, dir, armPath: null, roughnessPath: $"{dir}/{set}Roughness.jpg");
                created++;
            }
            return created;
        }

        static Material CreateLitMaterial(string matDir, string set, string texDir, string armPath, string roughnessPath)
        {
            QuestProjectSetup.EnsureFolder(matDir);
            var matPath = $"{matDir}/{set}Material.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat != null)
                return mat;

            mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = $"{set}Material" };
            var baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>($"{texDir}/{set}BaseColor.jpg");
            mat.SetTexture("_BaseMap", baseMap);
            mat.SetColor("_BaseColor", Color.white);

            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{texDir}/{set}Normal.jpg");
            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.SetFloat("_BumpScale", 1f);
                mat.EnableKeyword("_NORMALMAP");
            }

            var msPath = $"{texDir}/{set}MetallicSmoothness.png";
            if (!File.Exists(msPath))
            {
                var src = !string.IsNullOrEmpty(armPath) && File.Exists(armPath) ? armPath
                        : !string.IsNullOrEmpty(roughnessPath) && File.Exists(roughnessPath) ? roughnessPath : null;
                if (src != null)
                    BuildMetallicSmoothness(src, msPath, isArm: src == armPath);
            }
            if (File.Exists(msPath))
            {
                AssetDatabase.ImportAsset(msPath);
                mat.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(msPath));
                mat.SetFloat("_Smoothness", 1f);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            else
            {
                mat.SetFloat("_Smoothness", 0.3f);
            }

            mat.enableInstancing = true;
            AssetDatabase.CreateAsset(mat, matPath);
            return mat;
        }

        static void BuildMetallicSmoothness(string srcPath, string dstPath, bool isArm)
        {
            var src = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            if (!src.LoadImage(File.ReadAllBytes(srcPath)))
                return;

            const int size = 512;
            var dst = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var c = src.GetPixelBilinear((x + 0.5f) / size, (y + 0.5f) / size);
                float rough = isArm ? c.g : c.r;
                float metal = isArm ? c.b : 0f;
                pixels[y * size + x] = new Color(metal, 0f, 0f, 1f - rough);
            }
            dst.SetPixels32(pixels);
            File.WriteAllBytes(dstPath, dst.EncodeToPNG());
            Object.DestroyImmediate(src);
            Object.DestroyImmediate(dst);
        }

        public static void WriteReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Asset Library Report (자동 생성: Backrooms > 4. Process Asset Library)");
            sb.AppendLine("집계 기준: 프리팹 + 표면 머티리얼(Structure: Materials/*.mat, 그 외: Textures 세트) + 오디오 클립");
            sb.AppendLine();
            sb.AppendLine($"{"Folder",-28}{"Prefabs",9}{"Materials",11}{"Audio",7}{"Total",7}");
            int grand = 0;
            foreach (var c in Categories)
            foreach (var z in Zones)
            {
                var leaf = $"Assets/{c}/{z}";
                if (!AssetDatabase.IsValidFolder(leaf)) { sb.AppendLine($"{leaf.Substring(7),-28}{"-",9}"); continue; }
                int prefabs = Count(leaf, "*.prefab", "Prefabs");
                int mats = c == "Structure"
                    ? (Directory.Exists($"{leaf}/Materials") ? Directory.GetFiles($"{leaf}/Materials", "*.mat", SearchOption.TopDirectoryOnly).Length : 0)
                    : (AssetDatabase.IsValidFolder($"{leaf}/Textures") ? AssetDatabase.GetSubFolders($"{leaf}/Textures").Length : 0);
                int audio = Count(leaf, "*.wav", null) + Count(leaf, "*.ogg", null) + Count(leaf, "*.mp3", null);
                int total = prefabs + mats + audio;
                grand += total;
                sb.AppendLine($"{leaf.Substring(7),-28}{prefabs,9}{mats,11}{audio,7}{total,7}");
            }
            sb.AppendLine();
            sb.AppendLine($"Total: {grand}");
            File.WriteAllText(ReportPath, sb.ToString(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(ReportPath);
            Debug.Log(sb.ToString());
        }

        static int Count(string leaf, string pattern, string sub)
        {
            var dir = sub == null ? leaf : $"{leaf}/{sub}";
            return Directory.Exists(dir) ? Directory.GetFiles(dir, pattern, SearchOption.AllDirectories).Length : 0;
        }
    }
}
