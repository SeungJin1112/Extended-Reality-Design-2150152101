using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Backrooms.EditorTools
{
    /// <summary>
    /// 백룸(Level 0) 기본 공간을 생성한다. 이벤트가 없는 정적 구조물만 배치한다.
    /// 메뉴: Backrooms > 2. Build Backrooms Scene
    ///
    /// 생성 결과
    ///  - Assets/Meshes, Assets/Materials, Assets/Prefabs, Assets/Audio : 구조물 에셋
    ///  - Assets/Scenes/Backrooms.unity : 구조물이 배치된 공통 씬 (+ PlayerOrigin)
    ///
    /// 주의: 다시 실행하면 Backrooms 씬을 새로 만들어 덮어쓴다.
    ///       씬에서 직접 수정한 배치를 유지하려면 다시 실행하지 말 것.
    /// </summary>
    public static class BackroomsSceneBuilder
    {
        // ----- 공간 치수 (미터)
        public const int GridX = 12;              // 가로 셀 수
        public const int GridZ = 12;              // 세로 셀 수
        public const float CellSize = 4f;         // 셀 한 변
        public const float WallHeight = 2.8f;     // 천장 높이
        public const float WallThickness = 0.2f;
        const float WallLength = CellSize - WallThickness; // 기둥(WallPost) 사이 벽 길이
        const float DoorWidth = 1.2f;
        const float DoorHeight = 2.1f;
        const float BaseboardHeight = 0.1f;
        const float BaseboardDepth = 0.02f;
        const float PillarSize = 0.6f;
        const float DetailLightmapScale = 3f;    // 얇은 디테일(벽기둥, 형광등 틀)의 라이트맵 해상도 배율
        const float BaseboardLightmapScale = 6f; // 걸레받이(높이 10cm)는 더 키워야 UV가 겹치지 않음

        // ----- 생성 확률 (벽 제거/큰 홀 개수는 BackroomsLayout 참고)
        const float DoorwayChance = 0.2f;
        const float MoldWallChance = 0.08f;
        const float PillarChance = 0.15f;
        const float DeadLightChance = 0.15f;       // 불이 꺼진 셀 비율

        public const int DefaultSeed = 2150;

        const string ScenePath = "Assets/Scenes/Backrooms.unity";
        const string MeshFolder = "Assets/Meshes";
        const string MaterialFolder = "Assets/Materials";
        const string PrefabFolder = "Assets/Prefabs";
        const string AudioFolder = "Assets/Audio";
        const string TextureFolder = "Assets/Textures";
        const string LightingSettingsPath = "Assets/Settings/BackroomsLightingSettings.lighting";

        static readonly Color LightColor = new Color(1f, 0.95f, 0.8f);

        [MenuItem("Backrooms/2. Build Backrooms Scene", priority = 2)]
        static void BuildFromMenu()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Backrooms",
                    "Backrooms 씬을 새로 생성해서 덮어씁니다.\n씬에서 직접 수정한 내용은 사라집니다. 계속할까요?",
                    "생성", "취소"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            bool bake = EditorUtility.DisplayDialog("Backrooms",
                "조명을 바로 베이크할까요? (몇 분 걸릴 수 있습니다)\n나중에 Backrooms > 3. Bake Lighting 으로도 할 수 있습니다.",
                "베이크", "나중에");
            Build(DefaultSeed, bake);
        }

        [MenuItem("Backrooms/3. Bake Lighting", priority = 3)]
        public static void BakeLighting()
        {
            Lightmapping.Bake();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("[Backrooms] 라이트 베이크 완료");
        }

        /// <summary>커맨드라인(-executeMethod)용: 기본 시드로 생성 + 베이크.</summary>
        public static void BuildAndBake() => Build(DefaultSeed, true);

        public static void Build(int seed, bool bake)
        {
            foreach (var folder in new[] { MeshFolder, MaterialFolder, PrefabFolder, AudioFolder, "Assets/Scenes", "Assets/Settings" })
                QuestProjectSetup.EnsureFolder(folder);

            var materials = CreateMaterials();
            var prefabs = CreatePrefabs(materials);
            var humClip = FluorescentHumGenerator.CreateClip(AudioFolder + "/FluorescentHum.wav");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ApplyEnvironmentSettings();

            var layout = new BackroomsLayout(seed);
            var environment = new GameObject("Environment").transform;
            PlaceStructure(layout, prefabs, materials, environment);
            PlaceLightProbes(environment);
            PlaceAmbientAudio(humClip, environment);
            PlacePlayerOrigin(layout);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            if (bake)
                BakeLighting();

            AssetDatabase.SaveAssets();
            Debug.Log($"[Backrooms] 씬 생성 완료: {ScenePath} (seed {seed})");
        }

        // ================================================================ Materials

        class MaterialSet
        {
            public Material Wall, WallMold, Floor, Ceiling, Baseboard, LightFrame, LightPanel;
        }

        static MaterialSet CreateMaterials()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");

            return new MaterialSet
            {
                Wall = LitMaterial(lit, "WallMaterial", "WallTexture", "WallNormal", new Color(0.93f, 0.85f, 0.5f), 1.5f, 0.15f, 0.6f),
                WallMold = LitMaterial(lit, "WallMoldMaterial", "WallMoldTexture", "WallMoldNormal", new Color(0.9f, 0.82f, 0.5f), 1.5f, 0.1f, 0.6f),
                Floor = LitMaterial(lit, "FloorMaterial", "FloorTexture", "FloorNormal", new Color(0.95f, 0.85f, 0.6f), 2f, 0.05f, 0.8f),
                Ceiling = LitMaterial(lit, "CeilingMaterial", "CeilingTexture", "CeilingNormal", new Color(0.95f, 0.93f, 0.84f), CellSize, 0.2f, 0.5f),
                Baseboard = LitMaterial(lit, "BaseboardMaterial", null, null, new Color(0.55f, 0.47f, 0.3f), 1f, 0.3f, 0f),
                LightFrame = LitMaterial(lit, "LightFrameMaterial", null, null, new Color(0.85f, 0.85f, 0.82f), 1f, 0.4f, 0f),
                LightPanel = UnlitMaterial(unlit, "LightPanelMaterial", new Color(1f, 0.98f, 0.9f)),
            };
        }

        static Material LoadOrCreateMaterial(string name, Shader shader)
        {
            var path = $"{MaterialFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            return mat;
        }

        static Material LitMaterial(Shader shader, string name, string baseMap, string normalMap, Color tint,
            float metersPerTile, float smoothness, float normalStrength)
        {
            var mat = LoadOrCreateMaterial(name, shader);
            mat.SetColor("_BaseColor", tint);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", 0f);

            var scale = Vector2.one / metersPerTile; // 메시 UV가 미터 단위이므로
            if (baseMap != null)
            {
                mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/{baseMap}.jpg"));
                mat.SetTextureScale("_BaseMap", scale);
            }
            if (normalMap != null)
            {
                mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/{normalMap}.jpg"));
                mat.SetFloat("_BumpScale", normalStrength);
                mat.EnableKeyword("_NORMALMAP");
            }
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material UnlitMaterial(Shader shader, string name, Color color)
        {
            var mat = LoadOrCreateMaterial(name, shader);
            mat.SetColor("_BaseColor", color);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ================================================================ Prefabs

        class PrefabSet
        {
            public GameObject WallSegment, DoorwayWall, WallPost, Pillar, Floor, Ceiling, CeilingLight;
        }

        static PrefabSet CreatePrefabs(MaterialSet m)
        {
            float halfH = WallHeight * 0.5f;
            float sideLen = (WallLength - DoorWidth) * 0.5f;
            float sideX = (DoorWidth + sideLen) * 0.5f;
            float lintelH = WallHeight - DoorHeight;
            float boardZ = (WallThickness + BaseboardDepth) * 0.5f;
            float boardY = BaseboardHeight * 0.5f;

            // ----- Meshes
            var wallMesh = new BackroomsMeshFactory()
                .AddBox(new Vector3(0, halfH, 0), new Vector3(WallLength, WallHeight, WallThickness), skipBottom: true, skipTop: true)
                .SaveAsset("WallSegmentMesh", MeshFolder);
            var wallBoardMesh = new BackroomsMeshFactory()
                .AddBox(new Vector3(0, boardY, boardZ), new Vector3(WallLength, BaseboardHeight, BaseboardDepth), skipBottom: true)
                .AddBox(new Vector3(0, boardY, -boardZ), new Vector3(WallLength, BaseboardHeight, BaseboardDepth), skipBottom: true)
                .SaveAsset("WallBaseboardMesh", MeshFolder, BaseboardLightmapScale);

            var doorMesh = new BackroomsMeshFactory()
                .AddBox(new Vector3(-sideX, halfH, 0), new Vector3(sideLen, WallHeight, WallThickness), skipBottom: true, skipTop: true)
                .AddBox(new Vector3(sideX, halfH, 0), new Vector3(sideLen, WallHeight, WallThickness), skipBottom: true, skipTop: true)
                .AddBox(new Vector3(0, DoorHeight + lintelH * 0.5f, 0), new Vector3(DoorWidth, lintelH, WallThickness), skipTop: true)
                .SaveAsset("DoorwayWallMesh", MeshFolder);
            var doorBoardMesh = new BackroomsMeshFactory()
                .AddBox(new Vector3(-sideX, boardY, boardZ), new Vector3(sideLen, BaseboardHeight, BaseboardDepth), skipBottom: true)
                .AddBox(new Vector3(-sideX, boardY, -boardZ), new Vector3(sideLen, BaseboardHeight, BaseboardDepth), skipBottom: true)
                .AddBox(new Vector3(sideX, boardY, boardZ), new Vector3(sideLen, BaseboardHeight, BaseboardDepth), skipBottom: true)
                .AddBox(new Vector3(sideX, boardY, -boardZ), new Vector3(sideLen, BaseboardHeight, BaseboardDepth), skipBottom: true)
                .SaveAsset("DoorwayBaseboardMesh", MeshFolder, BaseboardLightmapScale);

            var postMesh = new BackroomsMeshFactory()
                .AddBox(new Vector3(0, halfH, 0), new Vector3(WallThickness, WallHeight, WallThickness), skipBottom: true, skipTop: true)
                .SaveAsset("WallPostMesh", MeshFolder, DetailLightmapScale);
            var postBoardMesh = new BackroomsMeshFactory()
                .AddBox(new Vector3(0, boardY, 0), new Vector3(WallThickness + 0.03f, BaseboardHeight, WallThickness + 0.03f), skipBottom: true)
                .SaveAsset("WallPostBaseboardMesh", MeshFolder, BaseboardLightmapScale);

            var pillarMesh = new BackroomsMeshFactory()
                .AddBox(new Vector3(0, halfH, 0), new Vector3(PillarSize, WallHeight, PillarSize), skipBottom: true, skipTop: true)
                .SaveAsset("PillarMesh", MeshFolder);
            var pillarBoardMesh = new BackroomsMeshFactory()
                .AddBox(new Vector3(0, boardY, 0), new Vector3(PillarSize + 0.04f, BaseboardHeight, PillarSize + 0.04f), skipBottom: true)
                .SaveAsset("PillarBaseboardMesh", MeshFolder, BaseboardLightmapScale);

            // 바닥/천장은 전체 영역을 한 장으로 만든다. (셀마다 나누면 라이트맵 이음새가 줄처럼 보임)
            float areaX = GridX * CellSize, areaZ = GridZ * CellSize;
            var floorMesh = new BackroomsMeshFactory().AddHorizontalQuad(areaX, areaZ, 0f, faceDown: false).SaveAsset("FloorMesh", MeshFolder);
            var ceilingMesh = new BackroomsMeshFactory().AddHorizontalQuad(areaX, areaZ, 0f, faceDown: true).SaveAsset("CeilingMesh", MeshFolder);

            // 천장 타일 2x1칸 크기(텍스처 기준)에 맞춘 형광등
            float panelW = CellSize / 3f, panelD = CellSize / 6f;
            var frameMesh = new BackroomsMeshFactory()
                .AddBox(new Vector3(0, -0.02f, 0), new Vector3(panelW + 0.04f, 0.04f, panelD + 0.04f), skipTop: true)
                .SaveAsset("LightFrameMesh", MeshFolder, DetailLightmapScale);
            var panelMesh = new BackroomsMeshFactory().AddHorizontalQuad(panelW, panelD, -0.041f, faceDown: true).SaveAsset("LightPanelMesh", MeshFolder);

            var set = new PrefabSet();

            // ----- WallSegment
            {
                var root = MeshObject("WallSegment", wallMesh, m.Wall, null);
                MeshObject("Baseboard", wallBoardMesh, m.Baseboard, root.transform, BaseboardLightmapScale);
                AddBoxCollider(root, new Vector3(0, halfH, 0), new Vector3(WallLength, WallHeight, WallThickness));
                set.WallSegment = SavePrefab(root);
            }
            // ----- DoorwayWall (문틀 없이 뚫린 통로)
            {
                var root = MeshObject("DoorwayWall", doorMesh, m.Wall, null);
                MeshObject("Baseboard", doorBoardMesh, m.Baseboard, root.transform, BaseboardLightmapScale);
                AddBoxCollider(root, new Vector3(-sideX, halfH, 0), new Vector3(sideLen, WallHeight, WallThickness));
                AddBoxCollider(root, new Vector3(sideX, halfH, 0), new Vector3(sideLen, WallHeight, WallThickness));
                AddBoxCollider(root, new Vector3(0, DoorHeight + lintelH * 0.5f, 0), new Vector3(DoorWidth, lintelH, WallThickness));
                set.DoorwayWall = SavePrefab(root);
            }
            // ----- WallPost (벽 이음새 기둥)
            {
                var root = MeshObject("WallPost", postMesh, m.Wall, null, DetailLightmapScale);
                MeshObject("Baseboard", postBoardMesh, m.Baseboard, root.transform, BaseboardLightmapScale);
                AddBoxCollider(root, new Vector3(0, halfH, 0), new Vector3(WallThickness, WallHeight, WallThickness));
                set.WallPost = SavePrefab(root);
            }
            // ----- Pillar (열린 공간의 독립 기둥)
            {
                var root = MeshObject("Pillar", pillarMesh, m.Wall, null);
                MeshObject("Baseboard", pillarBoardMesh, m.Baseboard, root.transform, BaseboardLightmapScale);
                AddBoxCollider(root, new Vector3(0, halfH, 0), new Vector3(PillarSize, WallHeight, PillarSize));
                set.Pillar = SavePrefab(root);
            }
            // ----- Floor
            {
                var root = MeshObject("Floor", floorMesh, m.Floor, null);
                AddBoxCollider(root, new Vector3(0, -0.05f, 0), new Vector3(areaX, 0.1f, areaZ));
                set.Floor = SavePrefab(root);
            }
            // ----- Ceiling
            {
                var root = MeshObject("Ceiling", ceilingMesh, m.Ceiling, null);
                AddBoxCollider(root, new Vector3(0, 0.05f, 0), new Vector3(areaX, 0.1f, areaZ));
                set.Ceiling = SavePrefab(root);
            }
            // ----- CeilingLight (형광등 + 베이크용 포인트 라이트)
            {
                var root = MeshObject("CeilingLight", frameMesh, m.LightFrame, null, DetailLightmapScale);
                var panel = MeshObject("LightPanel", panelMesh, m.LightPanel, root.transform, useProbes: true);
                panel.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

                var lightGo = new GameObject("PointLight");
                lightGo.transform.SetParent(root.transform, false);
                lightGo.transform.localPosition = new Vector3(0, -0.15f, 0);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = LightColor;
                light.intensity = 1.6f;
                light.range = 7f;
                light.lightmapBakeType = LightmapBakeType.Baked;
                light.shadows = LightShadows.Soft;
                light.shadowRadius = 0.4f;
                set.CeilingLight = SavePrefab(root);
            }

            return set;
        }

        /// <param name="lightmapScale">
        /// Scale In Lightmap. 걸레받이·벽기둥처럼 얇은 디테일은 텍셀이 부족하므로 키운다.
        /// (라이트 프로브는 프로브 범위 밖/아래에 있는 물체가 검게 나오므로 정적 구조물에는 쓰지 않는다.)
        /// </param>
        /// <param name="useProbes">라이트맵이 의미 없는 물체(Unlit 형광등 면 등)는 프로브 사용.</param>
        static GameObject MeshObject(string name, Mesh mesh, Material material, Transform parent,
            float lightmapScale = 1f, bool useProbes = false)
        {
            var go = new GameObject(name);
            if (parent != null)
                go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveGI = useProbes ? ReceiveGI.LightProbes : ReceiveGI.Lightmaps;
            if (!Mathf.Approximately(lightmapScale, 1f))
            {
                var so = new SerializedObject(renderer);
                so.FindProperty("m_ScaleInLightmap").floatValue = lightmapScale;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            GameObjectUtility.SetStaticEditorFlags(go,
                StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic);
            return go;
        }

        static void AddBoxCollider(GameObject go, Vector3 center, Vector3 size)
        {
            var c = go.AddComponent<BoxCollider>();
            c.center = center;
            c.size = size;
        }

        static GameObject SavePrefab(GameObject root)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}/{root.name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ================================================================ Scene

        static void ApplyEnvironmentSettings()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.24f, 0.22f, 0.15f);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.45f, 0.41f, 0.26f);
            RenderSettings.fogStartDistance = 8f;
            RenderSettings.fogEndDistance = 42f;

            var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingSettingsPath);
            if (settings == null)
            {
                settings = new LightingSettings { name = "BackroomsLightingSettings" };
                AssetDatabase.CreateAsset(settings, LightingSettingsPath);
            }
            settings.bakedGI = true;
            settings.realtimeGI = false;
            settings.lightmapper = LightingSettings.Lightmapper.ProgressiveCPU;
            settings.lightmapResolution = BackroomsMeshFactory.LightmapTexelsPerMeter;
            settings.lightmapPadding = 2;
            settings.lightmapMaxSize = 1024;
            settings.directionalityMode = LightmapsMode.NonDirectional; // Quest: 메모리/성능 절약
            settings.lightmapCompression = LightmapCompression.NormalQuality;
            settings.ao = true;
            settings.aoMaxDistance = 0.8f;
            settings.directSampleCount = 32;
            settings.indirectSampleCount = 128;
            settings.environmentSampleCount = 64;
            settings.maxBounces = 2;
            settings.filteringMode = LightingSettings.FilterMode.Auto;
            EditorUtility.SetDirty(settings);
            Lightmapping.lightingSettings = settings;
        }

        static Vector3 CellCenter(int x, int z) =>
            new Vector3((x + 0.5f) * CellSize - GridX * CellSize * 0.5f, 0f, (z + 0.5f) * CellSize - GridZ * CellSize * 0.5f);

        static Vector3 Vertex(int i, int j) =>
            new Vector3(i * CellSize - GridX * CellSize * 0.5f, 0f, j * CellSize - GridZ * CellSize * 0.5f);

        static void PlaceStructure(BackroomsLayout layout, PrefabSet p, MaterialSet m, Transform environment)
        {
            Spawn(p.Floor, environment, Vector3.zero, 0f);
            Spawn(p.Ceiling, environment, Vector3.up * WallHeight, 0f);
            var lights = Group("CeilingLights", environment);
            var walls = Group("Walls", environment);
            var posts = Group("WallPosts", environment);
            var pillars = Group("Pillars", environment);
            var rng = layout.Random;

            float panelOffsetZ = CellSize / 12f; // 형광등을 천장 타일 줄눈에 맞춤
            for (int x = 0; x < GridX; x++)
            for (int z = 0; z < GridZ; z++)
            {
                var c = CellCenter(x, z);
                if (rng.NextDouble() >= DeadLightChance)
                    Spawn(p.CeilingLight, lights, c + new Vector3(0, WallHeight, panelOffsetZ), 0f);
            }

            // Z 방향 벽 (vertex 라인 i, 셀 z 구간)
            for (int i = 0; i <= GridX; i++)
            for (int z = 0; z < GridZ; z++)
            {
                if (!layout.WallZ[i, z]) continue;
                bool boundary = i == 0 || i == GridX;
                var pos = Vertex(i, z) + new Vector3(0, 0, CellSize * 0.5f);
                PlaceWall(p, m, walls, pos, 90f, boundary, rng);
            }
            // X 방향 벽
            for (int x = 0; x < GridX; x++)
            for (int j = 0; j <= GridZ; j++)
            {
                if (!layout.WallX[x, j]) continue;
                bool boundary = j == 0 || j == GridZ;
                var pos = Vertex(x, j) + new Vector3(CellSize * 0.5f, 0, 0);
                PlaceWall(p, m, walls, pos, 0f, boundary, rng);
            }

            // 벽 이음새 기둥 / 열린 공간의 독립 기둥
            for (int i = 0; i <= GridX; i++)
            for (int j = 0; j <= GridZ; j++)
            {
                if (layout.HasWallAtVertex(i, j))
                    Spawn(p.WallPost, posts, Vertex(i, j), 0f);
                else if (rng.NextDouble() < PillarChance)
                    Spawn(p.Pillar, pillars, Vertex(i, j), 0f);
            }
        }

        static void PlaceWall(PrefabSet p, MaterialSet m, Transform parent, Vector3 pos, float yaw, bool boundary, System.Random rng)
        {
            bool doorway = !boundary && rng.NextDouble() < DoorwayChance;
            var go = Spawn(doorway ? p.DoorwayWall : p.WallSegment, parent, pos, yaw);
            if (rng.NextDouble() < MoldWallChance)
                go.GetComponent<MeshRenderer>().sharedMaterial = m.WallMold;
        }

        static Transform Group(string name, Transform parent)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
            return t;
        }

        static GameObject Spawn(GameObject prefab, Transform parent, Vector3 position, float yaw)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            return go;
        }

        /// <summary>나중에 추가될 움직이는 오브젝트(아이템 등)가 베이크된 조명을 받도록 라이트 프로브 배치.</summary>
        static void PlaceLightProbes(Transform environment)
        {
            var go = new GameObject("LightProbes");
            go.transform.SetParent(environment, false);
            var positions = new List<Vector3>();
            for (int x = 0; x < GridX; x++)
            for (int z = 0; z < GridZ; z++)
            {
                var c = CellCenter(x, z);
                positions.Add(c + Vector3.up * 0.4f);
                positions.Add(c + Vector3.up * 2.2f);
            }
            go.AddComponent<LightProbeGroup>().probePositions = positions.ToArray();
        }

        static void PlaceAmbientAudio(AudioClip clip, Transform environment)
        {
            var go = new GameObject("AmbientHum");
            go.transform.SetParent(environment, false);
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = true;
            source.spatialBlend = 0f;
            source.volume = 0.35f;
        }

        static void PlacePlayerOrigin(BackroomsLayout layout)
        {
            var spawn = CellCenter(layout.SpawnX, layout.SpawnZ);
            var prefabPath = QuestProjectSetup.FindSamplePrefab("Starter Assets", "Prefabs/XR Origin (XR Rig).prefab");
            if (prefabPath == null)
            {
                Debug.LogWarning("[Backrooms] XR Origin 프리팹이 없습니다. 먼저 Backrooms > 1. Setup Quest Project 를 실행하세요.");
                var placeholder = new GameObject("PlayerOrigin");
                placeholder.transform.position = spawn;
                return;
            }

            var rig = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
            rig.name = "PlayerOrigin";
            rig.transform.SetPositionAndRotation(spawn, Quaternion.identity);
            foreach (var cam in rig.GetComponentsInChildren<Camera>(true))
            {
                cam.farClipPlane = 60f; // 안개 너머는 그리지 않음
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = RenderSettings.fogColor;
            }
        }
    }

    /// <summary>
    /// 셀 격자 위의 벽 배치 데이터.
    /// 1) Recursive Backtracker 미로로 모든 셀이 연결되도록 보장
    /// 2) 벽 일부를 무작위로 제거해 불규칙한 방/복도 형태로 만듦
    /// 3) 몇 개의 직사각형 영역은 내부 벽을 모두 제거해 큰 홀로 만듦
    /// </summary>
    public class BackroomsLayout
    {
        public readonly bool[,] WallZ; // [i(0..GridX), z(0..GridZ-1)] : x = i 선 위, Z 방향으로 뻗은 벽
        public readonly bool[,] WallX; // [x(0..GridX-1), j(0..GridZ)] : z = j 선 위, X 방향으로 뻗은 벽
        public readonly System.Random Random;
        public readonly int SpawnX, SpawnZ;

        const int W = BackroomsSceneBuilder.GridX;
        const int H = BackroomsSceneBuilder.GridZ;
        const double ExtraWallRemoveChance = 0.3;  // 미로 생성 후 추가로 벽을 걷어낼 확률 (열린 공간)
        const int OpenRoomCount = 2;               // 내부 벽을 모두 없앤 큰 홀 개수

        public BackroomsLayout(int seed)
        {
            Random = new System.Random(seed);
            WallZ = new bool[W + 1, H];
            WallX = new bool[W, H + 1];
            for (int i = 0; i <= W; i++) for (int z = 0; z < H; z++) WallZ[i, z] = true;
            for (int x = 0; x < W; x++) for (int j = 0; j <= H; j++) WallX[x, j] = true;

            CarveMaze();
            RemoveRandomWalls(ExtraWallRemoveChance);
            for (int r = 0; r < OpenRoomCount; r++)
                CarveOpenRoom();

            SpawnX = W / 2;
            SpawnZ = H / 2;
        }

        void CarveMaze()
        {
            var visited = new bool[W, H];
            var stack = new Stack<(int x, int z)>();
            var start = (Random.Next(W), Random.Next(H));
            visited[start.Item1, start.Item2] = true;
            stack.Push(start);
            var dirs = new List<(int dx, int dz)>(4);

            while (stack.Count > 0)
            {
                var (x, z) = stack.Peek();
                dirs.Clear();
                if (x > 0 && !visited[x - 1, z]) dirs.Add((-1, 0));
                if (x < W - 1 && !visited[x + 1, z]) dirs.Add((1, 0));
                if (z > 0 && !visited[x, z - 1]) dirs.Add((0, -1));
                if (z < H - 1 && !visited[x, z + 1]) dirs.Add((0, 1));

                if (dirs.Count == 0)
                {
                    stack.Pop();
                    continue;
                }

                var (dx, dz) = dirs[Random.Next(dirs.Count)];
                int nx = x + dx, nz = z + dz;
                if (dx != 0) WallZ[Mathf.Max(x, nx), z] = false;
                else WallX[x, Mathf.Max(z, nz)] = false;
                visited[nx, nz] = true;
                stack.Push((nx, nz));
            }
        }

        void RemoveRandomWalls(double chance)
        {
            for (int i = 1; i < W; i++) for (int z = 0; z < H; z++)
                if (WallZ[i, z] && Random.NextDouble() < chance) WallZ[i, z] = false;
            for (int x = 0; x < W; x++) for (int j = 1; j < H; j++)
                if (WallX[x, j] && Random.NextDouble() < chance) WallX[x, j] = false;
        }

        void CarveOpenRoom()
        {
            int w = Random.Next(2, 4), h = Random.Next(2, 4);
            int x0 = Random.Next(0, W - w + 1), z0 = Random.Next(0, H - h + 1);
            for (int i = x0 + 1; i < x0 + w; i++) for (int z = z0; z < z0 + h; z++) WallZ[i, z] = false;
            for (int x = x0; x < x0 + w; x++) for (int j = z0 + 1; j < z0 + h; j++) WallX[x, j] = false;
        }

        public bool HasWallAtVertex(int i, int j)
        {
            return (j > 0 && WallZ[i, j - 1]) || (j < H && WallZ[i, j]) ||
                   (i > 0 && WallX[i - 1, j]) || (i < W && WallX[i, j]);
        }
    }
}
