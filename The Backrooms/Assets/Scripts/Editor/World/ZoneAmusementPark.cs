using UnityEngine;

namespace Backrooms.EditorTools.World
{
    public static class ZoneAmusementPark
    {
        const string S = "Assets/Structure/AmusementPark";
        const float W = 56f, D = 56f, H = 7f;

        static readonly Color Pink = new Color(1f, 0.72f, 0.82f), Blue = new Color(0.66f, 0.84f, 1f), Mint = new Color(0.68f, 0.96f, 0.82f),
            Lavender = new Color(0.8f, 0.72f, 1f), Butter = new Color(1f, 0.94f, 0.62f), Peach = new Color(1f, 0.79f, 0.64f);
        static readonly Color[] Pastels = { Pink, Blue, Mint, Lavender, Butter, Peach };
        static readonly Color Warm = new Color(1f, 0.82f, 0.6f);

        public static ZoneKit Build(string name, Vector3 origin)
        {
            var k = new ZoneKit(name, S, origin, W, D, H, 777) { WallThickness = 0.4f };
            k.Room(0, 0, W, D);
            k.Opening(W / 2, 0f, 1.08f);
            k.Opening(W / 2, D, 1.08f);

            var tex = $"{S}/Textures";
            var ground = WorldAssets.SetMaterial(S, "ParkGroundMaterial", tex, "Asphalt031", 4f, new Color(0.55f, 0.53f, 0.6f), 0.2f);
            var bricks = WorldAssets.SetMaterial(S, "ParkWallMaterial", tex, "PaintedBricks002", 2.5f, new Color(1f, 0.86f, 0.9f));
            var trim = WorldAssets.Lit(S, "ParkTrimMaterial", new Color(0.95f, 0.93f, 0.98f), smoothness: 0.4f);
            k.Build(new ZoneKit.Surfaces { Floor = ground, Wall = bricks, HasCeiling = false, Baseboard = trim, BaseboardHeight = 0.25f });
            k.Reserve(W / 2 - 2f, 0f, 4f, 5f);
            k.Reserve(W / 2 - 3f, D - 6f, 6f, 6f);

            var plaza = WorldAssets.SetMaterial(S, "ParkPlazaMaterial", "Assets/Structure/Common/Textures", "Terrazzo004", 2f, new Color(1f, 0.93f, 0.96f), 0.35f);
            WorldAssets.MeshGo("Plaza", WorldAssets.SaveMesh(WorldAssets.Cylinder(13f, 0.03f, 64), S, $"{name}PlazaMesh"), plaza, k.Architecture, new Vector3(W / 2, 0f, 26f), Vector3.zero, Vector3.one);
            var path = WorldAssets.SetMaterial(S, "ParkPathMaterial", "Assets/Structure/Common/Textures", "Terrazzo004", 2f, new Color(0.93f, 0.9f, 1f), 0.35f);
            k.Box("PathSouth", new Vector3(W / 2, 0.012f, 7f), new Vector3(4f, 0.024f, 14f), path);
            k.Box("PathNorth", new Vector3(W / 2, 0.012f, 45.5f), new Vector3(4f, 0.024f, 21f), path);

            BuildCarousel(k, name, new Vector3(W / 2, 0f, 26f));
            BuildFerrisWheel(k, name, new Vector3(44f, 0f, 46f));
            BuildStalls(k, name);
            BuildLamps(k, name);
            BuildDecor(k, name);
            BuildFunhouse(k, name);

            k.Place("DoorPink", W / 2, -0.2f, 0f, isStatic: false).name = "EntryDoor";
            k.Marker("EntryPoint", W / 2, 1.5f, 0f);
            k.Marker("ExitPoint", W / 2, D - 1.5f, 180f);

            k.LightProbes(5f, 0.5f, 2f);
            k.ReflectionProbe(W / 2, 3f, 26f, new Vector3(W, 10f, D));
            return k;
        }

        static Material Paint(string name, Color c, float smooth = 0.45f, float metal = 0f) => WorldAssets.Lit(S, name, c, smoothness: smooth, metallic: metal);

        static void BuildCarousel(ZoneKit k, string zone, Vector3 c)
        {
            var root = k.Child("Carousel", k.Props);
            root.localPosition = c;
            var deck = Paint("CarouselDeckMaterial", Lavender, 0.5f);
            var gold = Paint("CarouselGoldMaterial", new Color(1f, 0.84f, 0.45f), 0.55f, 0.3f);
            var canopyTex = ProceduralTextures.Stripes($"{S}/Textures/Generated/CarouselStripesBaseColor.png", Pink, new Color(1f, 0.97f, 0.97f), 24);
            var canopy = WorldAssets.Lit(S, "CarouselCanopyMaterial", Color.white, canopyTex, smoothness: 0.3f);
            var under = Paint("CarouselUnderMaterial", new Color(1f, 0.92f, 0.95f), 0.3f);
            var bulb = WorldAssets.Unlit(S, "WarmBulbMaterial", new Color(1f, 0.9f, 0.65f));

            WorldAssets.MeshGo("Deck", WorldAssets.SaveMesh(WorldAssets.Cylinder(6f, 0.35f, 48), S, $"{zone}CarouselDeckMesh"), deck, root, Vector3.zero, Vector3.zero, Vector3.one);
            WorldAssets.MeshGo("Column", WorldAssets.SaveMesh(WorldAssets.Cylinder(0.7f, 4.8f, 24), S, $"{zone}CarouselColumnMesh"), gold, root, new Vector3(0, 0.35f, 0), Vector3.zero, Vector3.one);
            WorldAssets.MeshGo("Under", WorldAssets.SaveMesh(WorldAssets.Cylinder(7f, 0.12f, 48), S, $"{zone}CarouselUnderMesh"), under, root, new Vector3(0, 5.1f, 0), Vector3.zero, Vector3.one);
            WorldAssets.MeshGo("Canopy", WorldAssets.SaveMesh(WorldAssets.Cone(7.2f, 2.6f, 48), S, $"{zone}CarouselCanopyMesh"), canopy, root, new Vector3(0, 5.22f, 0), Vector3.zero, Vector3.one);
            WorldAssets.MeshGo("Rim", WorldAssets.SaveMesh(WorldAssets.Torus(7.1f, 0.09f, 64, 8), S, $"{zone}CarouselRimMesh"), gold, root, new Vector3(0, 5.12f, 0), Vector3.zero, Vector3.one);
            WorldAssets.MeshGo("Finial", WorldAssets.SaveMesh(WorldAssets.Cone(0.35f, 1.1f, 16), S, $"{zone}CarouselFinialMesh"), gold, root, new Vector3(0, 7.8f, 0), Vector3.zero, Vector3.one);
            k.Collider(c + new Vector3(0, 0.175f, 0), new Vector3(10f, 0.35f, 10f));

            for (int i = 0; i < 40; i++)
            {
                float a = i / 40f * Mathf.PI * 2f;
                k.Primitive("Bulb", "Sphere.fbx", new Vector3(Mathf.Cos(a) * 7.1f, 4.98f, Mathf.Sin(a) * 7.1f), Vector3.zero, Vector3.one * 0.12f, bulb, root);
            }

            var pole = WorldAssets.SaveMesh(WorldAssets.Cylinder(0.035f, 4.75f, 10), S, $"{zone}CarouselPoleMesh");
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f, r = i % 2 == 0 ? 4.6f : 3.2f;
                var p = new Vector3(Mathf.Cos(a) * r, 0.35f, Mathf.Sin(a) * r);
                WorldAssets.MeshGo("Pole", pole, gold, root, p, Vector3.zero, Vector3.one);
                var horse = k.Place("HorseStatue01", 0f, 0f, -a * Mathf.Rad2Deg, parent: root, snap: false, isStatic: true);
                if (horse == null) continue;
                var b = ZoneKit.Bounds(horse);
                float scale = b.size.y > 0.01f ? 1.3f / b.size.y : 1f;
                horse.transform.localScale = Vector3.one * scale;
                horse.transform.localPosition = p + Vector3.up * (0.65f + (i % 3) * 0.25f);
                var b2 = ZoneKit.Bounds(horse);
                horse.transform.position += new Vector3(root.position.x + p.x - b2.center.x, 0, root.position.z + p.z - b2.center.z);
            }

            for (int i = 0; i < 4; i++) { float a = i / 4f * Mathf.PI * 2f; k.PointLight(c.x + Mathf.Cos(a) * 3.2f, 4.3f, c.z + Mathf.Sin(a) * 3.2f, Warm, 2.4f, 11f); }
            for (int i = 0; i < 4; i++)
            {
                float a = i / 4f * Mathf.PI * 2f + 0.4f;
                k.PointLight(c.x + Mathf.Cos(a) * 7.1f, 4.9f, c.z + Mathf.Sin(a) * 7.1f, new Color(1f, 0.88f, 0.7f), 2.2f, 9f);
            }
        }

        static void BuildFerrisWheel(ZoneKit k, string zone, Vector3 c)
        {
            var root = k.Child("FerrisWheel", k.Props);
            root.localPosition = c;
            root.localRotation = Quaternion.Euler(0, 90, 0);
            float hub = 10.5f, r = 8.5f;
            var frame = Paint("FerrisFrameMaterial", new Color(0.97f, 0.95f, 1f), 0.5f, 0.2f);
            var ring = WorldAssets.SaveMesh(WorldAssets.Torus(r, 0.14f, 72, 8), S, $"{zone}FerrisRingMesh");
            foreach (var x in new[] { -0.7f, 0.7f })
                WorldAssets.MeshGo("Ring", ring, frame, root, new Vector3(x, hub, 0), new Vector3(0, 0, 90), Vector3.one);
            WorldAssets.MeshGo("Hub", WorldAssets.SaveMesh(WorldAssets.Cylinder(0.5f, 1.8f, 20), S, $"{zone}FerrisHubMesh"), frame, root, new Vector3(-0.9f, hub, 0), new Vector3(0, 0, -90), Vector3.one);
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f;
                foreach (var x in new[] { -0.7f, 0.7f })
                {
                    var spoke = k.Box("Spoke", Vector3.zero, new Vector3(0.08f, r, 0.08f), frame, root);
                    spoke.transform.localPosition = new Vector3(x, hub, 0) + Quaternion.Euler(a, 0, 0) * (Vector3.up * r / 2);
                    spoke.transform.localRotation = Quaternion.Euler(a, 0, 0);
                }
            }
            foreach (var s in new[] { -1f, 1f })
            foreach (var x in new[] { -1.4f, 1.4f })
            {
                var leg = k.Box("Leg", Vector3.zero, new Vector3(0.25f, 11.55f, 0.25f), frame, root, collider: true);
                leg.transform.localPosition = new Vector3(x, hub / 2, s * 2.4f);
                leg.transform.localRotation = Quaternion.Euler(-s * 24.6f, 0, 0);
            }
            var bulb = WorldAssets.Unlit(S, "PastelBulbMaterial", new Color(1f, 0.85f, 0.95f));
            for (int i = 0; i < 48; i++)
            {
                float a = i / 48f * 360f;
                var p = new Vector3(0.8f, hub, 0) + Quaternion.Euler(a, 0, 0) * (Vector3.up * r);
                k.Primitive("Bulb", "Sphere.fbx", p, Vector3.zero, Vector3.one * 0.14f, bulb, root);
            }
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f + 15f;
                var p = new Vector3(0, hub, 0) + Quaternion.Euler(a, 0, 0) * (Vector3.up * r);
                var col = Pastels[i % Pastels.Length];
                var cabin = Paint($"FerrisCabin{i % Pastels.Length}Material", col, 0.5f);
                k.Box("Cabin", p + Vector3.down * 1.05f, new Vector3(1.1f, 1.0f, 1.0f), cabin, root);
                k.Box("CabinRoof", p + Vector3.down * 0.45f, new Vector3(1.3f, 0.12f, 1.2f), frame, root);
                k.Box("Hanger", p + Vector3.down * 0.2f, new Vector3(0.05f, 0.45f, 0.05f), frame, root);
            }
            k.PointLight(c.x + 2f, 6f, c.z - 3f, new Color(1f, 0.8f, 0.95f), 3.5f, 16f);
            k.PointLight(c.x + 2f, 15f, c.z, new Color(0.85f, 0.85f, 1f), 3f, 14f);
            k.Reserve(c.x - 5f, c.z - 5f, 10f, 10f);
        }

        static void BuildStalls(ZoneKit k, string zone)
        {
            var stalls = new (string title, float x, float z, float rot, Color a, string[] goods)[]
            {
                ("FoodStall", 6f, 14f, 90f, Pink, new[] { "StrawberryChocolateCake", "CarrotCake", "Croissant", "HamburgerBuns", "Bananas", "FoodApple01", "FoodKiwi01", "FoodPomegranate01", "Lemon", "FoodLychee01" }),
                ("SweetsStall", 6f, 28f, 90f, Mint, new[] { "FoodPearsAsian01", "FoodAvocado01", "FoodLime01", "WineBottles01", "TeaSet01", "BrassGoblets", "CarrotCake", "Croissant" }),
                ("GameStall", 50f, 14f, 270f, Blue, new[] { "Football", "AmericanFootball", "Baseball01", "BaseballBat", "ChessSet", "SungkaBoard", "DirtyFootball", "SungkaBoard02" }),
                ("GiftStall", 50f, 28f, 270f, Lavender, new[] { "CeramicVase01", "CeramicVase02", "CeramicVase03", "CeramicVase04", "AntiqueCeramicVase01", "BrassCandleholders", "Lantern01", "CarvedWoodenElephant" }),
            };
            var wood = WorldAssets.SetMaterial(S, "StallWoodMaterial", $"{S}/Textures", "Planks010", 2f, new Color(1f, 0.95f, 0.9f));
            var white = Paint("StallWhiteMaterial", new Color(0.98f, 0.97f, 0.95f), 0.4f);
            for (int s = 0; s < stalls.Length; s++)
            {
                var st = stalls[s];
                var root = k.Child(st.title, k.Props);
                root.localPosition = new Vector3(st.x, 0, st.z);
                root.localRotation = Quaternion.Euler(0, st.rot, 0);
                var stripes = WorldAssets.Lit(S, $"{st.title}RoofMaterial", Color.white, ProceduralTextures.Stripes($"{S}/Textures/Generated/{st.title}StripesBaseColor.png", st.a, Color.white, 12), smoothness: 0.3f);
                var accent = Paint($"{st.title}AccentMaterial", st.a, 0.45f);

                k.Box("Counter", new Vector3(0, 0.5f, 0.6f), new Vector3(3.6f, 1.0f, 0.6f), wood, root, collider: true);
                k.Box("CounterTop", new Vector3(0, 1.03f, 0.6f), new Vector3(3.8f, 0.06f, 0.75f), accent, root);
                k.Box("Back", new Vector3(0, 1.4f, -1.2f), new Vector3(3.6f, 2.8f, 0.1f), wood, root, collider: true);
                foreach (var x in new[] { -1.75f, 1.75f })
                    k.Box("Post", new Vector3(x, 1.4f, 0.85f), new Vector3(0.12f, 2.8f, 0.12f), white, root);
                var roof = WorldAssets.MeshGo("Roof", WorldAssets.SaveMesh(WorldAssets.Cone(2.9f, 1.4f, 4), S, $"{zone}{st.title}RoofMesh"), stripes, root, new Vector3(0, 2.8f, -0.2f), new Vector3(0, 45, 0), new Vector3(1f, 1f, 0.78f));
                k.Box("Valance", new Vector3(0, 2.7f, 0.95f), new Vector3(3.9f, 0.25f, 0.04f), accent, root);
                k.Box("Sign", new Vector3(0, 3.15f, 1.0f), new Vector3(2.0f, 0.45f, 0.06f), white, root);

                for (int g = 0; g < st.goods.Length; g++)
                {
                    float gx = -1.5f + (g % 5) * 0.75f, gz = g < 5 ? 0.6f : -1.0f;
                    var item = k.Place(st.goods[g], 0f, 0f, 0f, parent: root, snap: false);
                    if (item == null) continue;
                    item.transform.localPosition = new Vector3(gx, 0, gz);
                    item.transform.localRotation = Quaternion.Euler(0, 180 + (g * 37 % 40) - 20, 0);
                    float top = g < 5 ? 1.06f : 0f;
                    var b = ZoneKit.Bounds(item);
                    item.transform.position += Vector3.up * (k.Root.position.y + top - b.min.y);
                    if (g >= 5) item.transform.localPosition += new Vector3(0, 0, 0.6f);
                }
                if (st.title == "GameStall") k.Place("Dartboard", 0f, -1.12f, 0f, y: 1.7f, parent: root, snap: false);
                var lp = root.TransformPoint(new Vector3(0, 2.4f, 0.3f)) - k.Root.position;
                k.PointLight(lp.x, lp.y, lp.z, Warm, 2.2f, 7f);
                k.Reserve(st.x - 2.5f, st.z - 2.5f, 5f, 5f);
            }

            var booth = k.Child("TicketBooth", k.Props);
            booth.localPosition = new Vector3(W / 2 - 5f, 0, 6f);
            var boothMat = Paint("TicketBoothMaterial", Butter, 0.45f);
            k.Box("Booth", new Vector3(0, 1.2f, 0), new Vector3(2f, 2.4f, 1.6f), boothMat, booth, collider: true);
            k.Box("Window", new Vector3(0, 1.3f, 0.81f), new Vector3(1.2f, 0.7f, 0.02f), WorldAssets.Unlit(S, "BoothWindowMaterial", new Color(1f, 0.93f, 0.75f)), booth);
            WorldAssets.MeshGo("BoothRoof", WorldAssets.SaveMesh(WorldAssets.Cone(1.7f, 1.1f, 4), S, $"{zone}BoothRoofMesh"), Paint("BoothRoofMaterial", Pink, 0.4f), booth, new Vector3(0, 2.4f, 0), new Vector3(0, 45, 0), Vector3.one);
            k.Place("CashRegister01", 0f, 0.75f, 180f, y: 1.0f, parent: booth, snap: false);
            k.Place("Megaphone01", 0.6f, 0.75f, 160f, y: 1.0f, parent: booth, snap: false);
            k.Place("StandingChalkboard01", W / 2 - 3.2f, 7.5f, 200f);
            k.PointLight(W / 2 - 5f, 2.1f, 7.2f, Warm, 1.8f, 6f);
            k.Reserve(W / 2 - 6.5f, 4.5f, 3.5f, 4f);
        }

        static void BuildLamps(ZoneKit k, string zone)
        {
            var bulbMats = new Material[Pastels.Length];
            for (int i = 0; i < Pastels.Length; i++) bulbMats[i] = WorldAssets.Unlit(S, $"StringBulb{i}Material", Color.Lerp(Pastels[i], Color.white, 0.35f));
            var wire = Paint("StringWireMaterial", new Color(0.15f, 0.15f, 0.17f), 0.3f);
            var posts = new Vector3[10];
            for (int i = 0; i < posts.Length; i++)
            {
                float a = i / (float)posts.Length * Mathf.PI * 2f + 0.3f;
                posts[i] = new Vector3(W / 2 + Mathf.Cos(a) * 15.5f, 0, 26f + Mathf.Sin(a) * 15.5f);
            }
            var lamps = k.Child("Lamps", k.Props);
            float headY = 4f;
            for (int i = 0; i < posts.Length; i++)
            {
                var lamp = k.Place(i % 2 == 0 ? "StreetLamp01" : "StreetLamp02", posts[i].x, posts[i].z, i * 36f, parent: lamps, isStatic: true);
                if (lamp != null) headY = ZoneKit.Bounds(lamp).max.y - k.Root.position.y;
                k.PointLight(posts[i].x, headY - 0.4f, posts[i].z, new Color(1f, 0.86f, 0.66f), 3.2f, 12f, lamps);
                k.Reserve(posts[i].x - 0.6f, posts[i].z - 0.6f, 1.2f, 1.2f);
            }
            var strings = k.Child("StringLights", k.Props);
            for (int i = 0; i < posts.Length; i++)
            {
                var a = posts[i] + Vector3.up * (headY - 0.6f);
                var b = posts[(i + 1) % posts.Length] + Vector3.up * (headY - 0.6f);
                int n = 16;
                for (int j = 0; j <= n; j++)
                {
                    float t = j / (float)n;
                    var p = Vector3.Lerp(a, b, t) + Vector3.down * Mathf.Sin(t * Mathf.PI) * 0.9f;
                    k.Primitive("Bulb", "Sphere.fbx", p, Vector3.zero, Vector3.one * 0.11f, bulbMats[(i + j) % bulbMats.Length], strings);
                    if (j < n)
                    {
                        float t2 = (j + 1) / (float)n;
                        var p2 = Vector3.Lerp(a, b, t2) + Vector3.down * Mathf.Sin(t2 * Mathf.PI) * 0.9f;
                        var seg = k.Box("Wire", (p + p2) / 2, new Vector3(0.015f, 0.015f, Vector3.Distance(p, p2)), wire, strings);
                        seg.transform.localRotation = Quaternion.LookRotation(p2 - p);
                    }
                }
            }
        }

        static void BuildDecor(ZoneKit k, string zone)
        {
            var benchRing = new[] { 0.9f, 2.2f, 3.5f, 4.8f };
            for (int i = 0; i < benchRing.Length; i++)
            {
                float a = benchRing[i];
                var p = new Vector3(W / 2 + Mathf.Cos(a) * 11f, 0, 26f + Mathf.Sin(a) * 11f);
                float rot = -a * Mathf.Rad2Deg - 90f;
                k.Place(i % 2 == 0 ? "ModularStreetSeating" : "PaintedWoodenBench", p.x, p.z, rot, isStatic: true);
                k.Reserve(p.x - 1.2f, p.z - 1.2f, 2.4f, 2.4f);
            }
            k.Place("ThrowPillows01", W / 2 + Mathf.Cos(2.2f) * 11f, 26f + Mathf.Sin(2.2f) * 11f, 0f, y: 0.45f);
            k.Place("Ukulele01", W / 2 + Mathf.Cos(4.8f) * 11f, 26f + Mathf.Sin(4.8f) * 11f, 40f, y: 0.45f);

            foreach (var (x, z) in new[] { (10f, 40f), (13f, 44f), (8f, 46f) })
            {
                k.Place("WoodenPicnicTable", x, z, (float)(k.Rng.NextDouble() * 60 - 30), isStatic: true);
                k.Reserve(x - 1.5f, z - 1.5f, 3f, 3f);
            }

            var pedestal = Paint("PedestalMaterial", new Color(0.97f, 0.96f, 0.99f), 0.5f);
            var statues = new[] { ("GothicStatue", 4f, 4f), ("MarbleBust01", 52f, 4f), ("LionHead", 4f, 52f), ("HorseHead", 52f, 52f), ("BullHead", 20f, 3f), ("GardenGnome", 36f, 3f) };
            foreach (var (prefab, x, z) in statues)
            {
                k.Box("Pedestal", new Vector3(x, 0.55f, z), new Vector3(0.9f, 1.1f, 0.9f), pedestal, k.Props, collider: true);
                var s = k.Place(prefab, x, z, 180f + (float)(k.Rng.NextDouble() * 40 - 20), y: 1.1f);
                if (s != null)
                {
                    var b = ZoneKit.Bounds(s);
                    if (b.size.y < 0.5f) { s.transform.localScale *= 0.8f / Mathf.Max(0.05f, b.size.y); var b2 = ZoneKit.Bounds(s); s.transform.position += Vector3.up * (k.Root.position.y + 1.1f - b2.min.y); }
                }
                k.Reserve(x - 0.8f, z - 0.8f, 1.6f, 1.6f);
            }

            var balloonMats = new Material[Pastels.Length];
            for (int i = 0; i < Pastels.Length; i++) balloonMats[i] = WorldAssets.Lit(S, $"Balloon{i}Material", Pastels[i], smoothness: 0.85f);
            var stringMat = Paint("BalloonStringMaterial", new Color(0.95f, 0.95f, 0.95f), 0.2f);
            foreach (var (x, z) in new[] { (9f, 18f), (47f, 18f), (9f, 33f), (47f, 33f), (24.5f, 9f), (33f, 40f) })
            {
                var cluster = k.Child("Balloons", k.Props);
                cluster.localPosition = new Vector3(x, 0, z);
                k.Box("Weight", new Vector3(0, 0.08f, 0), new Vector3(0.18f, 0.16f, 0.18f), Paint("BalloonWeightMaterial", new Color(0.9f, 0.88f, 0.95f), 0.5f), cluster);
                for (int i = 0; i < 6; i++)
                {
                    var top = new Vector3((float)(k.Rng.NextDouble() - 0.5) * 0.9f, 2.4f + (float)k.Rng.NextDouble() * 0.9f, (float)(k.Rng.NextDouble() - 0.5) * 0.9f);
                    var ball = k.Primitive("Balloon", "Sphere.fbx", top, Vector3.zero, new Vector3(0.42f, 0.5f, 0.42f), balloonMats[(i + (int)x) % balloonMats.Length], cluster, isStatic: false);
                    WorldAssets.UseProbes(ball);
                    var from = new Vector3(0, 0.16f, 0);
                    var line = k.Box("String", (from + top) / 2, new Vector3(0.008f, 0.008f, Vector3.Distance(from, top)), stringMat, cluster, isStatic: false);
                    line.transform.localRotation = Quaternion.LookRotation(top - from);
                }
            }

            var toys = new Rect(18f, 36f, 10f, 6f);
            foreach (var p in new[] { "Baseball01", "Football", "PaintedWoodenChair01", "PaintedWoodenChair02", "PaintedWoodenStool", "MetalStool01", "BarChairRound01" })
                k.Scatter(p, toys, 0.5f);
            k.Place("PaintedWoodenTable", 22f, 40f, 15f);
            k.Place("StreetRat", 15f, 21f, 120f);
            k.Place("SpinningWheel01", 4f, 30f, 90f);
            k.Place("PaintedWoodenSofa", 3f, 38f, 90f);
        }

        static void BuildFunhouse(ZoneKit k, string zone)
        {
            var root = k.Child("Funhouse", k.Props);
            root.localPosition = new Vector3(W / 2, 0, D);
            var facade = Paint("FunhouseFacadeMaterial", new Color(0.78f, 0.7f, 1f), 0.4f);
            var trim = Paint("FunhouseTrimMaterial", new Color(1f, 0.97f, 0.9f), 0.5f);
            var awning = WorldAssets.Lit(S, "FunhouseAwningMaterial", Color.white, ProceduralTextures.Stripes($"{S}/Textures/Generated/FunhouseStripesBaseColor.png", Mint, Color.white, 16), smoothness: 0.3f);
            k.Box("FacadeLeft", new Vector3(-3.55f, 3f, -0.45f), new Vector3(6f, 6f, 0.5f), facade, root, collider: true);
            k.Box("FacadeRight", new Vector3(3.55f, 3f, -0.45f), new Vector3(6f, 6f, 0.5f), facade, root, collider: true);
            k.Box("FacadeTop", new Vector3(0, 4.3f, -0.45f), new Vector3(1.1f, 3.4f, 0.5f), facade, root, collider: true);
            k.Box("ArchTrim", new Vector3(0, 2.4f, -0.72f), new Vector3(2.2f, 0.18f, 0.06f), trim, root);
            k.Box("Awning", new Vector3(0, 2.75f, -1.3f), new Vector3(3.4f, 0.06f, 1.4f), awning, root).transform.localRotation = Quaternion.Euler(-15f, 0, 0);
            var face = WorldAssets.MeshGo("Moon", WorldAssets.SaveMesh(WorldAssets.Cylinder(1.3f, 0.2f, 40), S, $"{zone}FunhouseMoonMesh"), WorldAssets.Unlit(S, "FunhouseMoonMaterial", new Color(1f, 0.96f, 0.82f)), root, new Vector3(0, 5.2f, -0.75f), new Vector3(90, 0, 0), Vector3.one);
            k.Place("DoorLavender", 0f, -0.25f, 0f, parent: root, isStatic: false).name = "ExitDoor";
            var lp = root.TransformPoint(new Vector3(0, 3.4f, -2.2f)) - k.Root.position;
            k.PointLight(lp.x, lp.y, lp.z, new Color(0.95f, 0.85f, 1f), 3f, 9f);
        }
    }
}
