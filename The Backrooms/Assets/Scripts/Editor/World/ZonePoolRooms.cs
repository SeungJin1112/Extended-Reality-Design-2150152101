using UnityEngine;
using Faces = Backrooms.EditorTools.BackroomsMeshFactory.Faces;

namespace Backrooms.EditorTools.World
{
    public static class ZonePoolRooms
    {
        const string S = "Assets/Structure/PoolRooms";
        const float HallW = 36f, HallD = 56f, HallH = 3.4f;
        const float GalX = 36f, GalZ = 6f, GalW = 30f, GalD = 44f, GalH = 8.5f;
        const float WaterLevel = -0.28f;

        static Material water, hallTile, hallWall, hallCeiling, basinTile, coping, trim, galTile, galWall, galCeiling, lightPanel, lightFrame, glow;

        public static ZoneKit Build(string name, Vector3 origin)
        {
            CreateMaterials();
            var k = new ZoneKit(name, S, origin, HallW, HallD, HallH, 3030) { WallThickness = 0.3f };
            BuildHall(k, name);
            BuildGallery(k, name);
            PlaceDucks(k);
            k.Marker("EntryPoint", 18f, 1.4f, 0f);
            k.LightProbes(4f, 0.5f, 2f);
            return k;
        }

        static void CreateMaterials()
        {
            var tex = $"{S}/Textures";
            var ripple = ProceduralTextures.WaterRippleNormal($"{tex}/Generated/WaterRippleNormal.png");
            water = WorldAssets.Transparent(S, "PoolWaterMaterial", new Color(0.1f, 0.24f, 0.2f, 0.78f), null, 0.97f);
            water.SetTexture("_BumpMap", ripple);
            water.SetFloat("_BumpScale", 0.35f);
            water.EnableKeyword("_NORMALMAP");
            hallTile = WorldAssets.SetMaterial(S, "HallFloorMaterial", tex, "Tiles133A", 1.0f, new Color(0.86f, 0.87f, 0.72f), 0.55f);
            hallWall = WorldAssets.SetMaterial(S, "HallWallMaterial", tex, "Tiles133A", 1.0f, new Color(0.84f, 0.86f, 0.72f), 0.6f);
            hallCeiling = WorldAssets.SetMaterial(S, "HallCeilingMaterial", "Assets/Structure/Common/Textures", "Plaster001", 2.5f, new Color(0.5f, 0.54f, 0.46f), 0.1f);
            basinTile = WorldAssets.SetMaterial(S, "BasinTileMaterial", tex, "Tiles133B", 0.8f, new Color(0.7f, 0.78f, 0.66f), 0.75f);
            coping = WorldAssets.SetMaterial(S, "PoolCopingMaterial", tex, "Tiles105", 0.6f, new Color(0.9f, 0.9f, 0.82f), 0.6f);
            trim = WorldAssets.Lit(S, "PoolTrimMaterial", new Color(0.55f, 0.6f, 0.5f), smoothness: 0.7f);
            galTile = WorldAssets.SetMaterial(S, "GalleryTileMaterial", tex, "Tiles107", 0.8f, new Color(0.96f, 0.98f, 0.98f), 0.8f);
            galWall = WorldAssets.SetMaterial(S, "GalleryWallMaterial", tex, "Tiles107", 0.8f, new Color(0.95f, 0.97f, 0.97f), 0.8f);
            galCeiling = WorldAssets.SetMaterial(S, "GalleryCeilingMaterial", "Assets/Structure/Common/Textures", "Plaster001", 2.5f, new Color(0.82f, 0.86f, 0.86f), 0.1f);
            lightPanel = WorldAssets.Unlit(S, "RecessedLightMaterial", new Color(0.97f, 1f, 0.88f));
            lightFrame = WorldAssets.Lit(S, "RecessedFrameMaterial", new Color(0.3f, 0.32f, 0.28f), smoothness: 0.4f);
            glow = WorldAssets.Unlit(S, "WarmGlowMaterial", new Color(1f, 0.86f, 0.6f));
        }

        static void BuildHall(ZoneKit k, string zone)
        {
            k.Room(0, 0, HallW, HallD);
            k.Opening(18f, 0f, 1.08f);
            k.Opening(HallW, 28f, 3f, 3f);

            var pools = new (string n, float x, float z, float w, float d, float depth)[]
            {
                ("CanalWest", 5f, 6f, 5f, 44f, 1.1f),
                ("CanalCross", 10.6f, 24f, 19.4f, 4f, 1.0f),
                ("BasinNorth", 18f, 36f, 14f, 14f, 1.5f),
                ("BasinSouth", 14f, 7f, 16f, 8f, 0.9f),
            };
            foreach (var p in pools)
            {
                k.PoolBasin(p.n, p.x, p.z, p.w, p.d, p.depth, WaterLevel, basinTile, coping, water);
                var w = k.Architecture.Find($"{p.n}Water");
                if (w != null) w.gameObject.AddComponent<WaterSurface>();
            }

            k.Build(new ZoneKit.Surfaces { Floor = hallTile, Wall = hallWall, Ceiling = hallCeiling, Baseboard = trim, BaseboardHeight = 0.12f });

            var pillars = new BackroomsMeshFactory();
            foreach (var x in new[] { 3f, 13f, 23f, 33f })
            foreach (var z in new[] { 4f, 17f, 31f, 45f, 53f })
            {
                bool wet = !k.IsFloor(x, z);
                float y0 = wet ? -1.5f : 0f;
                pillars.AddBox(new Vector3(x, (y0 + HallH) / 2, z), new Vector3(1.4f, HallH - y0, 1.4f), Faces.Sides);
                k.Collider(new Vector3(x, (y0 + HallH) / 2, z), new Vector3(1.4f, HallH - y0, 1.4f));
                k.Reserve(x - 1f, z - 1f, 2f, 2f);
            }
            k.MeshObject("Pillars", pillars, hallWall, 1f);

            var lights = k.Child("RecessedLights", k.Details);
            for (float x = 6f; x < HallW; x += 6f)
            for (float z = 4f; z < HallD; z += 6.5f)
            {
                if (Mathf.Abs(x % 10f - 3f) < 0.9f && Mathf.Abs(z % 13.5f - 4f) < 0.9f) continue;
                k.Box("LightFrame", new Vector3(x, HallH - 0.03f, z), new Vector3(1.25f, 0.06f, 1.25f), lightFrame, lights);
                k.Quad("LightPanel", new Vector3(x, HallH - 0.065f, z), new Vector2(1.1f, 1.1f), new Vector3(-90, 0, 0), lightPanel, lights);
                k.AreaLight(x, HallH - 0.09f, z, 1.1f, 1.1f, new Color(0.92f, 1f, 0.82f), 6.5f);
            }

            foreach (var (x, z, r) in new[] { (10.4f, 12f, 90f), (4.6f, 30f, 270f), (17.6f, 40f, 270f), (30f, 7.4f, 180f) })
                k.Place("PoolLadder", x, z, r, y: -1.2f, snap: false);
            k.Place("WetFloorSign01", 13f, 3f, 200f);
            k.Place("WetFloorSign01", 32f, 20f, 30f);
            k.Place("PlasticMonoblocChair01", 34f, 14f, 250f);
            k.Place("PlasticMonoblocChair01", 34.2f, 15.2f, 230f);
            k.Place("Lifebuoy", 0.18f, 20f, 90f, y: 1.5f, snap: false);
            k.Place("GardenHoseWallMounted01", HallW - 0.18f, 10f, 270f, y: 0.9f, snap: false);
            k.Place("WaterManholeCover", 26f, 18.5f, 0f);
            k.Place("PottedPlant02", 1f, 54.8f, 0f);
            k.Place("AnthuriumBotany01", 35f, 54.8f, 0f);

            var door = k.Place("DoorMint", 18f, -0.15f, 0f, isStatic: false);
            if (door != null) door.name = "EntryDoor";
            k.Reserve(16f, 0f, 4f, 4f);
        }

        static void BuildGallery(ZoneKit zone, string zoneName)
        {
            var g = new ZoneKit($"{zoneName}Gallery", S, zone.Root.position + new Vector3(GalX, 0, GalZ), GalW, GalD, GalH, 77) { WallThickness = 0.3f, Thresholds = false };
            g.Root.SetParent(zone.Architecture, true);
            g.Root.name = "Gallery";
            g.Room(0, 0, GalW, GalD);
            g.Opening(0f, 22f, 3f, 3f);
            g.Opening(15f, GalD, 1.08f, 3.35f);
            g.PoolBasin("GalleryChannel", 8f, 1.5f, 14f, 35f, 0.8f, WaterLevel, basinTile, coping, water);
            var ch = g.Architecture.Find("GalleryChannelWater");
            if (ch != null) ch.gameObject.AddComponent<WaterSurface>();
            g.Build(new ZoneKit.Surfaces { Floor = galTile, Wall = galWall, Ceiling = galCeiling, Baseboard = trim, BaseboardHeight = 0.1f });

            const float pier = 0.8f, opening = 1.7f, spring = 3.2f, thick = 0.6f;
            foreach (var cx in new[] { 6.2f, 23.8f })
            {
                var arches = new BackroomsMeshFactory().AddArchWallX(0f, 36.5f, 0f, GalH, thick, pier, opening, spring);
                var go = g.MeshObject(cx < 15f ? "ArcadeWest" : "ArcadeEast", arches, galWall, 0.5f);
                go.transform.localPosition = new Vector3(cx, 0, 0);
                go.transform.localRotation = Quaternion.Euler(0, -90, 0);
                int bays = Mathf.FloorToInt((36.5f - pier) / (pier + opening));
                float used = bays * (pier + opening) + pier, start = (36.5f - used) / 2;
                for (int i = 0; i <= bays; i++)
                {
                    float z = start + i * (pier + opening) + pier / 2;
                    g.Collider(new Vector3(cx, GalH / 2, z), new Vector3(thick, GalH, pier));
                }
                g.Collider(new Vector3(cx, (spring + GalH) / 2 + 0.6f, 18f), new Vector3(thick, GalH - spring - 1.2f, 36.5f));
            }

            var column = WorldAssets.SaveMesh(WorldAssets.Cylinder(0.9f, GalH + 0.8f, 40, caps: false), S, $"{zoneName}GalleryColumnMesh");
            foreach (var z in new[] { 8f, 18f, 28f })
            {
                WorldAssets.MeshGo("Column", column, galWall, g.Architecture, new Vector3(15f, -0.8f, z), Vector3.zero, Vector3.one);
                g.Collider(new Vector3(15f, GalH / 2, z), new Vector3(1.8f, GalH, 1.8f));
            }

            var steps = new BackroomsMeshFactory();
            for (int i = 0; i < 5; i++)
            {
                float y = 0.24f * (i + 1), z0 = 36.6f + i * 0.34f;
                steps.AddBox(new Vector3(15f, y / 2, (z0 + GalD) / 2), new Vector3(14f, y, GalD - z0), Faces.All & ~Faces.NegY);
                g.Collider(new Vector3(15f, y / 2, (z0 + GalD) / 2), new Vector3(14f, y, GalD - z0));
            }
            g.MeshObject("Steps", steps, galTile, 1f);

            var exitDoor = g.Place("DoorWhite", 15f, GalD + 0.15f, 0f, y: 1.2f, isStatic: false);
            if (exitDoor != null) exitDoor.name = "ExitDoor";
            g.Quad("DoorGlow", new Vector3(15f, 2.8f, GalD - 0.02f), new Vector2(2.6f, 0.5f), new Vector3(0, 180, 0), glow);
            g.SpotLight(15f, 4.6f, GalD - 1.5f, new Vector3(55f, 180f, 0f), new Color(1f, 0.82f, 0.55f), 9f, 16f, 80f);
            var exitPoint = zone.Marker("ExitPoint", GalX + 15f, GalZ + GalD - 1.4f, 0f);
            exitPoint.localPosition += Vector3.up * 1.2f;

            var windows = g.Child("HighWindows", g.Details);
            for (float z = 3f; z < 36f; z += 5f)
            {
                g.Quad("Window", new Vector3(0.02f, 6.4f, z), new Vector2(1.4f, 1.0f), new Vector3(0, -90, 0), lightPanel, windows);
                g.Quad("Window", new Vector3(GalW - 0.02f, 6.4f, z), new Vector2(1.4f, 1.0f), new Vector3(0, 90, 0), lightPanel, windows);
            }
            for (float z = 4f; z < 36f; z += 6f)
            {
                g.Quad("Skylight", new Vector3(15f, GalH - 0.01f, z), new Vector2(3f, 1.4f), new Vector3(-90, 0, 0), lightPanel, windows);
                g.AreaLight(15f, GalH - 0.03f, z, 3f, 1.4f, new Color(0.9f, 0.97f, 1f), 7f);
            }
            foreach (var x in new[] { 3f, 27f })
                for (float z = 6f; z < 36f; z += 10f)
                    g.PointLight(x, 3.4f, z, new Color(0.85f, 0.95f, 0.9f), 1.1f, 7f);

            g.Place("PoolLadder", 21.6f, 4f, 90f, y: -0.7f, snap: false);
            g.Place("PoolLadder", 8.4f, 30f, 270f, y: -0.7f, snap: false);
            g.Place("BronzeWhaleStatue", 3f, 40f, 140f);
            g.Place("ConcreteCatStatue", 27f, 40f, 220f);
            g.Place("PachiraAquatica01", 2f, 2f, 0f);
            g.Place("CalatheaOrbifolia01", 28f, 2f, 0f);
            g.ReflectionProbe(15f, 2.5f, 18f, new Vector3(GalW, GalH, 36f));
            g.LightProbes(4f, 0.5f, 2.2f, 5f);
        }

        static void PlaceDucks(ZoneKit k)
        {
            void Float(string prefab, float x, float z, float scale = 1f, float sink = 0.04f)
            {
                var go = k.Place(prefab, x, z, (float)(k.Rng.NextDouble() * 360f), scale, WaterLevel - sink);
                if (go != null) WorldAssets.UseProbes(go);
            }
            for (int i = 0; i < 7; i++) Float("RubberDuckToy", 5.5f + (float)k.Rng.NextDouble() * 4f, 8f + (float)k.Rng.NextDouble() * 40f, 1.2f);
            for (int i = 0; i < 5; i++) Float(i % 2 == 0 ? "RubberDuckToy" : "RubberDuckPink", 19f + (float)k.Rng.NextDouble() * 12f, 37f + (float)k.Rng.NextDouble() * 12f, 1.3f);
            Float("RubberDuckFamily", 22f, 10f);
            Float("RubberDuckGiant", 25f, 43f, 1f, 0.25f);
            Float("SwimRingButter", 27f, 9.5f, 1f, 0.12f);
            Float("PoolNoodleMint", 20f, 26f, 1f, 0.03f);
            Float("BeachBallCloud", 29f, 39f, 1f, 0.18f);
            Float("RubberDuckFamily", GalX + 15f, GalZ + 14f);
            for (int i = 0; i < 6; i++) Float("RubberDuckToy", GalX + 9f + (float)k.Rng.NextDouble() * 12f, GalZ + 3f + (float)k.Rng.NextDouble() * 30f, 1.2f);
        }
    }
}
