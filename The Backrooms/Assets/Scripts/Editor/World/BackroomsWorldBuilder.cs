using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Backrooms.EditorTools.World
{
    public static class BackroomsWorldBuilder
    {
        public const string MainScenePath = "Assets/Scenes/Backrooms.unity";
        public const string ZoneSceneFolder = "Assets/Scenes/Zones";
        const string LightingPath = "Assets/Settings/BackroomsLightingSettings.lighting";

        class ZoneDef
        {
            public string Name;
            public Func<Level0Look, ZoneKit> Build;
            public Color Fog, Background;
            public float FogStart, FogEnd, Far;
        }

        static List<ZoneDef> Zones() => new List<ZoneDef>
        {
            new ZoneDef { Name = "Zone00ResearchBase", Build = l => ZoneResearchBase.Build("Zone00ResearchBase", new Vector3(0, 0, 0), false, l), Fog = new Color(0.1f, 0.11f, 0.12f), Background = new Color(0.05f, 0.055f, 0.06f), FogStart = 10f, FogEnd = 70f, Far = 90f },
            new ZoneDef { Name = "Zone01Level0", Build = l => ZoneLevel0.Build("Zone01Level0", new Vector3(100, 0, 0), l), Fog = new Color(0.4f, 0.4f, 0.29f), Background = new Color(0.4f, 0.4f, 0.29f), FogStart = 5f, FogEnd = 36f, Far = 45f },
            new ZoneDef { Name = "Zone02AmusementPark", Build = l => ZoneAmusementPark.Build("Zone02AmusementPark", new Vector3(200, 0, 0)), Fog = new Color(0.08f, 0.06f, 0.14f), Background = new Color(0.03f, 0.02f, 0.07f), FogStart = 18f, FogEnd = 90f, Far = 110f },
            new ZoneDef { Name = "Zone03PoolRooms", Build = l => ZonePoolRooms.Build("Zone03PoolRooms", new Vector3(300, 0, 0)), Fog = new Color(0.1f, 0.13f, 0.11f), Background = new Color(0.06f, 0.08f, 0.07f), FogStart = 6f, FogEnd = 48f, Far = 60f },
            new ZoneDef { Name = "Zone04ResearchBaseEmpty", Build = l => ZoneResearchBase.Build("Zone04ResearchBaseEmpty", new Vector3(400, 0, 0), true, l), Fog = new Color(0.18f, 0.19f, 0.21f), Background = new Color(0.18f, 0.19f, 0.21f), FogStart = 8f, FogEnd = 55f, Far = 70f },
        };

        public static string ZoneScenePath(string name) => $"{ZoneSceneFolder}/{name}.unity";

        public static string[] ExistingZoneScenes() =>
            Directory.Exists(ZoneSceneFolder) ? Directory.GetFiles(ZoneSceneFolder, "*.unity").Select(p => p.Replace('\\', '/')).OrderBy(p => p).ToArray() : new string[0];

        [MenuItem("Backrooms/2. Build World", priority = 2)]
        static void BuildFromMenu()
        {
            if (!EditorUtility.DisplayDialog("Backrooms", "메인 씬과 구역 씬을 새로 생성해서 덮어씁니다. 계속할까요?", "생성", "취소"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            Build(EditorUtility.DisplayDialog("Backrooms", "구역별로 라이트를 바로 베이크할까요?", "베이크", "나중에"));
            OpenWorld();
        }

        [MenuItem("Backrooms/3. Bake Lighting (All Zones)", priority = 3)]
        public static void BakeAllZones()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            foreach (var path in ExistingZoneScenes())
                BakeZone(path);
            OpenWorld();
        }

        [MenuItem("Backrooms/Open World", priority = 4)]
        public static void OpenWorld()
        {
            if (!File.Exists(MainScenePath)) return;
            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            foreach (var path in ExistingZoneScenes())
                EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByPath(MainScenePath));
        }

        [MenuItem("Backrooms/7. Capture Zone Previews", priority = 30)]
        static void CaptureFromMenu() => Capture(Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? "", "Temp/ZonePreviews"));

        public static void BuildAndBake() => Build(true);

        public static void BuildBakeAndCapture()
        {
            Build(Environment.GetEnvironmentVariable("BACKROOMS_SKIP_BAKE") != "1");
            Capture(Environment.GetEnvironmentVariable("BACKROOMS_CAPTURE_DIR") ?? "Temp/ZonePreviews");
        }

        public static void Build(bool bake)
        {
            AssetLibraryProcessor.Run();
            AssetLibraryProcessor.FixOversizedColliders();
            DreamcorePropGenerator.Run();
            ZoneKit.ResetIndex();
            QuestProjectSetup.EnsureFolder(ZoneSceneFolder);

            var look = Level0Look.Create();
            var defs = Zones();
            Vector3 spawnPos = new Vector3(24f, 0f, -12.5f);
            Quaternion spawnRot = Quaternion.identity;
            var scenePaths = new List<string>();
            var only = (Environment.GetEnvironmentVariable("BACKROOMS_ONLY_ZONES") ?? "").Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            Debug.Log($"[Backrooms] Zone filter: {(only.Count == 0 ? "all" : string.Join(",", only))}");

            for (int i = 0; i < defs.Count; i++)
            {
                var def = defs[i];
                if (only.Count > 0 && !only.Contains(def.Name) && File.Exists(ZoneScenePath(def.Name)))
                {
                    scenePaths.Add(ZoneScenePath(def.Name));
                    continue;
                }
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                ApplyLightingSettings();
                var kit = def.Build(look);
                var atmo = kit.Root.gameObject.AddComponent<ZoneAtmosphere>();
                atmo.applyOnStart = i == 0;
                atmo.fogColor = def.Fog; atmo.backgroundColor = def.Background;
                atmo.fogStart = def.FogStart; atmo.fogEnd = def.FogEnd; atmo.farClip = def.Far;
                ApplyFog(atmo.fogColor, atmo.fogStart, atmo.fogEnd);
                if (i == 0)
                {
                    var entry = kit.Markers.Find("EntryPoint");
                    if (entry != null) { spawnPos = entry.position; spawnRot = entry.rotation; }
                }
                if (i < defs.Count - 1) AddPortal(kit, defs[i + 1].Name);
                AddZoneGrading(kit, def.Name);
                var path = ZoneScenePath(def.Name);
                EditorSceneManager.SaveScene(scene, path);
                scenePaths.Add(path);
                if (bake) BakeZone(path);
                EditorUtility.UnloadUnusedAssetsImmediate();
                GC.Collect();
            }

            var main = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ApplyLightingSettings();
            ApplyFog(defs[0].Fog, defs[0].FogStart, defs[0].FogEnd);
            PlacePlayer(spawnPos, spawnRot, defs[0].Background, defs[0].Far);
            CreatePostProcessing();
            var loader = new GameObject("ZoneSceneLoader").AddComponent<ZoneSceneLoader>();
            loader.zoneScenes = defs.Select(d => d.Name).ToArray();
            EditorSceneManager.SaveScene(main, MainScenePath);

            var buildScenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(MainScenePath, true) };
            buildScenes.AddRange(scenePaths.Select(p => new EditorBuildSettingsScene(p, true)));
            EditorBuildSettings.scenes = buildScenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[Backrooms] World built");
        }

        static void AddPortal(ZoneKit kit, string targetZone)
        {
            var exitDoor = kit.Root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "ExitDoor");
            var exitPoint = kit.Markers.Find("ExitPoint");
            if (exitDoor == null || exitPoint == null) { Debug.LogWarning($"[Backrooms] {kit.Name}: ExitDoor/ExitPoint 없음, 포털 생략"); return; }
            var outward = exitDoor.position - exitPoint.position;
            outward.y = 0f;
            outward = outward.sqrMagnitude > 0.001f ? outward.normalized : exitDoor.forward;
            var go = new GameObject("ZonePortal");
            go.transform.SetParent(kit.Markers, true);
            go.transform.SetPositionAndRotation(exitDoor.position + outward * 0.5f + Vector3.up * 1.1f, Quaternion.LookRotation(outward));
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(1.4f, 2.2f, 0.9f);
            var portal = go.AddComponent<ZonePortal>();
            portal.targetZone = targetZone;
            portal.door = exitDoor.GetComponent<DoorController>();
        }

        static void BakeZone(string path)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Lightmapping.lightingSettings = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingPath);
            Lightmapping.Bake();
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Backrooms] Baked {Path.GetFileNameWithoutExtension(path)}");
            EditorUtility.UnloadUnusedAssetsImmediate();
            GC.Collect();
        }

        static void ApplyFog(Color color, float start, float end)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = color;
            RenderSettings.fogStartDistance = start;
            RenderSettings.fogEndDistance = end;
        }

        static void ApplyLightingSettings()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.1f, 0.095f, 0.09f);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;

            var s = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingPath);
            if (s == null)
            {
                QuestProjectSetup.EnsureFolder("Assets/Settings");
                s = new LightingSettings { name = "BackroomsLightingSettings" };
                AssetDatabase.CreateAsset(s, LightingPath);
            }
            s.bakedGI = true;
            s.realtimeGI = false;
            s.lightmapper = LightingSettings.Lightmapper.ProgressiveCPU;
            s.lightmapResolution = 4f;
            s.lightmapPadding = 2;
            s.lightmapMaxSize = 1024;
            s.directionalityMode = LightmapsMode.NonDirectional;
            s.lightmapCompression = LightmapCompression.NormalQuality;
            s.ao = true;
            s.aoMaxDistance = 1f;
            s.aoExponentDirect = 1f;
            s.aoExponentIndirect = 1f;
            s.directSampleCount = 24;
            s.indirectSampleCount = 128;
            s.environmentSampleCount = 64;
            s.maxBounces = 3;
            s.filteringMode = LightingSettings.FilterMode.Auto;
            EditorUtility.SetDirty(s);
            Lightmapping.lightingSettings = s;
        }

        const string PostProfilePath = "Assets/Settings/BackroomsPostProcessProfile.asset";

        static void CreatePostProcessing()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/QuestRenderer.asset");
            if (renderer != null && renderer.postProcessData == null)
            {
                renderer.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
                EditorUtility.SetDirty(renderer);
            }

            if (File.Exists(PostProfilePath)) AssetDatabase.DeleteAsset(PostProfilePath);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, PostProfilePath);

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.88f);
            bloom.intensity.Override(0.35f);
            bloom.scatter.Override(0.6f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.2f);
            vignette.smoothness.Override(0.5f);
            var grain = profile.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Medium3);
            grain.intensity.Override(0.16f);
            foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            var go = new GameObject("PostProcessVolume");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0;
            volume.sharedProfile = profile;
        }

        static readonly Dictionary<string, (float sat, float temp, float tint, float contrast, float exposure)> Grades = new Dictionary<string, (float, float, float, float, float)>
        {
            { "Zone00ResearchBase", (-25f, -6f, 2f, 10f, 0.15f) },
            { "Zone01Level0", (-12f, -4f, 7f, 8f, 0.05f) },
            { "Zone02AmusementPark", (10f, -4f, -12f, 8f, 0f) },
            { "Zone03PoolRooms", (-6f, -6f, 14f, 12f, 0f) },
            { "Zone04ResearchBaseEmpty", (-30f, -14f, 2f, 16f, -0.1f) },
        };

        static VolumeProfile GradingProfile(string name)
        {
            var key = name == "ShellInterior" ? "Zone01Level0" : name;
            if (!Grades.TryGetValue(key, out var g)) return null;
            QuestProjectSetup.EnsureFolder("Assets/Settings/ZoneGrading");
            var path = $"Assets/Settings/ZoneGrading/{name}Grading.asset";
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            var color = profile.Add<ColorAdjustments>(true);
            color.saturation.Override(g.sat);
            color.contrast.Override(g.contrast);
            color.postExposure.Override(g.exposure);
            var white = profile.Add<WhiteBalance>(true);
            white.temperature.Override(g.temp);
            white.tint.Override(g.tint);
            foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        static void AddLocalVolume(Transform parent, string name, Bounds worldBounds, VolumeProfile profile, float priority)
        {
            if (profile == null) return;
            var go = new GameObject($"{name}Volume");
            go.transform.SetParent(parent, true);
            go.transform.position = worldBounds.center;
            go.layer = 0;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = worldBounds.size;
            var v = go.AddComponent<Volume>();
            v.isGlobal = false;
            v.blendDistance = 1.5f;
            v.priority = priority;
            v.sharedProfile = profile;
        }

        static void AddZoneGrading(ZoneKit kit, string zoneName)
        {
            var b = ZoneBounds(kit.Root);
            b.Expand(new Vector3(4f, 6f, 4f));
            AddLocalVolume(kit.Root, zoneName, b, GradingProfile(zoneName), 1f);
            var shell = kit.Root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "BackroomsShell");
            if (shell != null)
            {
                var sb = ZoneBounds(shell);
                sb.Expand(new Vector3(-0.2f, 0f, -0.2f));
                AddLocalVolume(shell, "ShellInterior", sb, GradingProfile("ShellInterior"), 2f);
            }
        }

        static void PlacePlayer(Vector3 pos, Quaternion rot, Color background, float far)
        {
            var path = QuestProjectSetup.FindSamplePrefab("Starter Assets", "Prefabs/XR Origin (XR Rig).prefab");
            var rig = path != null ? (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path)) : new GameObject();
            rig.name = "PlayerOrigin";
            rig.transform.SetPositionAndRotation(pos, rot);
            if (rig.GetComponent<SprintController>() == null) rig.AddComponent<SprintController>();
            if (rig.GetComponent<HeadCollisionGuard>() == null) rig.AddComponent<HeadCollisionGuard>();
            if (rig.GetComponent<DesktopPlayerControls>() == null) rig.AddComponent<DesktopPlayerControls>();
            QuestProjectSetup.SetupInteractionSimulator(false);
            foreach (var cam in rig.GetComponentsInChildren<Camera>(true))
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = background;
                cam.farClipPlane = far;
                cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            }
        }

        public static void Capture(string outDir)
        {
            OpenWorld();
            Directory.CreateDirectory(outDir);

            var camGo = new GameObject("PreviewCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 78f;
            cam.nearClipPlane = 0.05f;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var rt = new RenderTexture(1280, 720, 24) { antiAliasing = 4 };
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);

            foreach (var atmo in UnityEngine.Object.FindObjectsOfType<ZoneAtmosphere>())
            {
                var root = atmo.transform;
                var markers = root.Find("Markers");
                var entry = markers != null ? markers.Find("EntryPoint") : null;
                if (entry == null) continue;
                ApplyFog(atmo.fogColor, atmo.fogStart, atmo.fogEnd);
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = atmo.backgroundColor; cam.farClipPlane = atmo.farClip;

                var b = ZoneBounds(root);
                var views = new List<(string, Vector3, Quaternion)>
                {
                    ("Entry", entry.position + Vector3.up * 1.6f, entry.rotation * Quaternion.Euler(4, 0, 0)),
                    ("EntryLeft", entry.position + Vector3.up * 1.6f + entry.forward * 3f, entry.rotation * Quaternion.Euler(4, -55, 0)),
                    ("EntryRight", entry.position + Vector3.up * 1.6f + entry.forward * 3f, entry.rotation * Quaternion.Euler(4, 55, 0)),
                    ("Center", new Vector3(b.center.x, 1.6f, b.center.z) + new Vector3(0, 0, -4f), Quaternion.Euler(4, 20, 0)),
                    ("Back", new Vector3(b.center.x, 1.6f, b.max.z - 3f), Quaternion.Euler(4, 200, 0)),
                };
                foreach (Transform m in markers)
                    if (m.name.StartsWith("Preview"))
                        views.Add((m.name, m.position + Vector3.up * 1.6f, m.rotation * Quaternion.Euler(4, 0, 0)));
                foreach (var (label, p, r) in views)
                {
                    cam.transform.SetPositionAndRotation(p, r);
                    Shoot(cam, rt, tex, Path.Combine(outDir, $"{root.name}_{label}.png"));
                }

                var hidden = new List<Renderer>();
                foreach (var rend in root.GetComponentsInChildren<Renderer>())
                    if (rend.name == "Ceiling" || rend.bounds.min.y > root.position.y + 2.5f) { if (rend.enabled) { rend.enabled = false; hidden.Add(rend); } }
                RenderSettings.fog = false;
                cam.farClipPlane = 300f;
                cam.transform.SetPositionAndRotation(new Vector3(b.center.x, 55f, b.center.z - 30f), Quaternion.Euler(58, 0, 0));
                Shoot(cam, rt, tex, Path.Combine(outDir, $"{root.name}_Overview.png"));
                foreach (var rend in hidden) rend.enabled = true;
            }
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(camGo);
            Debug.Log($"[Backrooms] Previews saved: {outDir}");
        }

        static Bounds ZoneBounds(Transform root)
        {
            var arch = root.Find("Architecture");
            var rs = (arch != null ? arch : root).GetComponentsInChildren<Renderer>();
            var b = rs.Length > 0 ? rs[0].bounds : new Bounds(root.position, Vector3.one);
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        static void Shoot(Camera cam, RenderTexture rt, Texture2D tex, string file)
        {
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(file, tex.EncodeToPNG());
            cam.targetTexture = null;
        }
    }
}
