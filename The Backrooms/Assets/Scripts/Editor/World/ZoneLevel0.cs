using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Backrooms.EditorTools.World
{
    public class Level0Look
    {
        public Material Wallpaper, Carpet, Ceiling, Baseboard, Troffer, TrofferDead, Stain, Outlet, Vent;
        public Color LightColor = new Color(1f, 0.98f, 0.9f);
        public float LightIntensity = 9f;

        public static Level0Look Create()
        {
            const string S = "Assets/Structure/Level0";
            var t = $"{S}/Textures";
            var look = new Level0Look();
            look.Wallpaper = WorldAssets.Lit(S, "Level0WallpaperMaterial", new Color(0.86f, 0.86f, 0.79f),
                null, WorldAssets.Tex($"{t}/WallNormal.jpg"), null, 1f, 0.1f, 0.3f);
            look.Carpet = WorldAssets.Lit(S, "Level0CarpetMaterial", new Color(0.56f, 0.52f, 0.42f),
                WorldAssets.Tex($"{t}/FloorTexture.jpg"), WorldAssets.Tex($"{t}/FloorNormal.jpg"), null, 1.6f, 0.05f, 0.9f);
            look.Ceiling = WorldAssets.Lit(S, "Level0CeilingMaterial", new Color(0.86f, 0.87f, 0.81f),
                WorldAssets.Tex($"{t}/CeilingTexture.jpg"), WorldAssets.Tex($"{t}/CeilingNormal.jpg"), null, 3.6f, 0.15f, 0.6f);
            look.Baseboard = WorldAssets.Lit(S, "Level0BaseboardMaterial", new Color(0.42f, 0.36f, 0.27f), smoothness: 0.35f);
            look.Troffer = WorldAssets.Unlit(S, "Level0TrofferMaterial", new Color(1f, 0.98f, 0.9f), ProceduralTextures.TrofferPanel($"{t}/Generated/TrofferBaseColor.png"));
            look.TrofferDead = WorldAssets.Lit(S, "Level0TrofferDeadMaterial", new Color(0.55f, 0.55f, 0.52f), WorldAssets.Tex($"{t}/Generated/TrofferBaseColor.png"), smoothness: 0.5f);
            look.Stain = WorldAssets.Transparent(S, "Level0StainMaterial", Color.white, ProceduralTextures.WaterStain($"{t}/Generated/WaterStainBaseColor.png", 3));
            look.Outlet = WorldAssets.Lit(S, "Level0OutletMaterial", new Color(0.92f, 0.9f, 0.84f), smoothness: 0.55f);
            look.Vent = WorldAssets.Lit(S, "Level0VentMaterial", new Color(0.78f, 0.76f, 0.7f), smoothness: 0.45f, metallic: 0.4f);
            return look;
        }
    }

    public static class ZoneLevel0
    {
        const string S = "Assets/Structure/Level0";
        const int Cells = 16;
        const float CellSize = 4f, H = 2.75f;

        public static ZoneKit Build(string name, Vector3 origin, Level0Look look)
        {
            float size = Cells * CellSize;
            var k = new ZoneKit(name, S, origin, size, size, H, 2150) { WallThickness = 0.15f };
            k.Room(0, 0, size, size);

            var layout = new MazeLayout(Cells, Cells, 2150, 0.34, 4);
            float t = k.WallThickness;

            for (int i = 1; i < Cells; i++)
            {
                int z = 0;
                while (z < Cells)
                {
                    if (!layout.WallZ[i, z]) { z++; continue; }
                    int start = z;
                    var doors = new List<(float, float)>();
                    while (z < Cells && layout.WallZ[i, z])
                    {
                        if (k.Rng.NextDouble() < 0.22) doors.Add(((z - start) * CellSize + CellSize / 2 + t / 2, 1.1f));
                        z++;
                    }
                    k.Partition(i * CellSize, start * CellSize - t / 2, i * CellSize, z * CellSize + t / 2, doors.ToArray());
                }
            }
            for (int j = 1; j < Cells; j++)
            {
                int x = 0;
                while (x < Cells)
                {
                    if (!layout.WallX[x, j]) { x++; continue; }
                    int start = x;
                    var doors = new List<(float, float)>();
                    while (x < Cells && layout.WallX[x, j])
                    {
                        if (k.Rng.NextDouble() < 0.22) doors.Add(((x - start) * CellSize + CellSize / 2 + t / 2, 1.1f));
                        x++;
                    }
                    k.Partition(start * CellSize - t / 2, j * CellSize, x * CellSize + t / 2, j * CellSize, doors.ToArray());
                }
            }

            float entryX = 2f, exitX = size - 2f;
            k.Opening(entryX, 0f, 1.08f);
            k.Opening(exitX, size, 1.08f);

            k.Build(new ZoneKit.Surfaces { Floor = look.Carpet, Wall = look.Wallpaper, Ceiling = look.Ceiling, Baseboard = look.Baseboard, BaseboardHeight = 0.09f });

            k.Place("DoorOffice", entryX, -0.075f, 0f, isStatic: false).name = "EntryDoor";
            k.Place("DoorOffice", exitX, size + 0.075f, 0f, isStatic: false).name = "ExitDoor";
            k.Marker("EntryPoint", entryX, 1.2f, 0f);
            k.Marker("ExitPoint", exitX, size - 1.2f, 0f);

            PlaceHalfWalls(k, look, size);
            PlaceTroffers(k, look, size);
            PlaceWallDetails(k, look, layout, size);
            PlaceFramesAndRails(k, look, layout);
            PlaceStains(k, look, size);
            PlaceProps(k, size);

            var hum = AssetDatabase.LoadAssetAtPath<AudioClip>($"{S}/Audio/FluorescentHum.wav");
            if (hum != null)
            {
                var a = new GameObject("AmbientHum").AddComponent<AudioSource>();
                a.transform.SetParent(k.Root, false);
                a.clip = hum; a.loop = true; a.playOnAwake = true; a.spatialBlend = 0f; a.volume = 0.3f;
            }

            k.LightProbes(4f, 0.5f, 1.7f);
            return k;
        }

        static void PlaceHalfWalls(ZoneKit k, Level0Look look, float size)
        {
            var body = new BackroomsMeshFactory();
            var cap = new BackroomsMeshFactory();
            const float h = 1.1f, t = 0.15f;
            int placed = 0;
            for (int tries = 0; tries < 200 && placed < 10; tries++)
            {
                bool alongX = k.Rng.NextDouble() < 0.5;
                float len = 1.8f + (float)k.Rng.NextDouble() * 2.2f;
                float cx = 2f + (float)k.Rng.NextDouble() * (size - 4f), cz = 2f + (float)k.Rng.NextDouble() * (size - 4f);
                var r = alongX ? new Rect(cx - len / 2, cz - 0.6f, len, 1.2f) : new Rect(cx - 0.6f, cz - len / 2, 1.2f, len);
                if (k.HitsWall(r, 0.6f) || k.NearDoor(cx, cz, 2.5f)) continue;
                var c = new Vector3(cx, h / 2, cz);
                var s = alongX ? new Vector3(len, h, t) : new Vector3(t, h, len);
                body.AddBox(c, s, BackroomsMeshFactory.Faces.Sides);
                cap.AddBox(new Vector3(cx, h + 0.02f, cz), s + new Vector3(alongX ? 0.04f : 0.06f, -h + 0.04f, alongX ? 0.06f : 0.04f), BackroomsMeshFactory.Faces.All & ~BackroomsMeshFactory.Faces.NegY);
                k.Collider(c, s);
                k.Reserve(r.x, r.y, r.width, r.height);
                placed++;
            }
            if (placed == 0) return;
            k.MeshObject("HalfWalls", body, look.Wallpaper, 1.5f);
            k.MeshObject("HalfWallCaps", cap, look.Baseboard, 4f);
        }

        static void PlaceTroffers(ZoneKit k, Level0Look look, float size)
        {
            var parent = k.Child("Troffers", k.Details);
            for (float x = 1.5f; x < size; x += 3.6f)
            for (float z = 1.2f; z < size; z += 4.8f)
            {
                var r = new Rect(x - 0.35f, z - 0.65f, 0.7f, 1.3f);
                if (k.HitsWall(r)) continue;
                bool dead = k.Rng.NextDouble() < 0.12;
                k.Quad("Troffer", new Vector3(x, H - 0.004f, z), new Vector2(0.6f, 1.2f), new Vector3(-90, 0, 0), dead ? look.TrofferDead : look.Troffer, parent);
                if (!dead) k.AreaLight(x, H - 0.02f, z, 0.6f, 1.2f, look.LightColor, look.LightIntensity * (0.85f + (float)k.Rng.NextDouble() * 0.3f));
            }
        }

        static void PlaceWallDetails(ZoneKit k, Level0Look look, MazeLayout layout, float size)
        {
            var parent = k.Child("WallFixtures", k.Details);
            float t = k.WallThickness;
            for (int i = 0; i <= Cells; i++)
            for (int z = 0; z < Cells; z++)
            {
                bool wall = i == 0 || i == Cells || layout.WallZ[i, z];
                if (!wall) continue;
                foreach (int side in new[] { -1, 1 })
                {
                    if ((i == 0 && side < 0) || (i == Cells && side > 0)) continue;
                    double roll = k.Rng.NextDouble();
                    float x = i * CellSize + side * (i == 0 || i == Cells ? 0.005f : t / 2 + 0.005f);
                    float zz = z * CellSize + 0.6f + (float)k.Rng.NextDouble() * 2.8f;
                    float rot = side > 0 ? 90f : 270f;
                    if (k.NearDoor(x, zz, 0.9f)) continue;
                    if (roll < 0.25) Outlet(k, look, parent, new Vector3(x, 0.32f, zz), rot);
                    else if (roll < 0.33) Vent(k, look, parent, new Vector3(x, H - 0.35f, zz), rot);
                }
            }
            for (int j = 0; j <= Cells; j++)
            for (int x = 0; x < Cells; x++)
            {
                bool wall = j == 0 || j == Cells || layout.WallX[x, j];
                if (!wall) continue;
                foreach (int side in new[] { -1, 1 })
                {
                    if ((j == 0 && side < 0) || (j == Cells && side > 0)) continue;
                    double roll = k.Rng.NextDouble();
                    float z = j * CellSize + side * (j == 0 || j == Cells ? 0.005f : t / 2 + 0.005f);
                    float xx = x * CellSize + 0.6f + (float)k.Rng.NextDouble() * 2.8f;
                    float rot = side > 0 ? 0f : 180f;
                    if (k.NearDoor(xx, z, 0.9f)) continue;
                    if (roll < 0.25) Outlet(k, look, parent, new Vector3(xx, 0.32f, z), rot);
                    else if (roll < 0.33) Vent(k, look, parent, new Vector3(xx, H - 0.35f, z), rot);
                }
            }
        }

        static void Outlet(ZoneKit k, Level0Look look, Transform parent, Vector3 p, float rot)
        {
            var go = new GameObject("WallOutlet");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = p;
            go.transform.localRotation = Quaternion.Euler(0, rot, 0);
            k.Box("Plate", new Vector3(0, 0, 0.004f), new Vector3(0.075f, 0.115f, 0.008f), look.Outlet, go.transform);
            k.Box("Socket", new Vector3(0, 0.025f, 0.009f), new Vector3(0.035f, 0.035f, 0.004f), look.Vent, go.transform);
            k.Box("Socket", new Vector3(0, -0.025f, 0.009f), new Vector3(0.035f, 0.035f, 0.004f), look.Vent, go.transform);
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) r.receiveGI = ReceiveGI.LightProbes;
        }

        static void Vent(ZoneKit k, Level0Look look, Transform parent, Vector3 p, float rot)
        {
            var go = new GameObject("WallVent");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = p;
            go.transform.localRotation = Quaternion.Euler(0, rot, 0);
            k.Box("Frame", new Vector3(0, 0, 0.008f), new Vector3(0.42f, 0.22f, 0.016f), look.Vent, go.transform);
            for (int i = 0; i < 6; i++)
                k.Box("Slat", new Vector3(0, -0.08f + i * 0.032f, 0.02f), new Vector3(0.38f, 0.012f, 0.012f), look.Vent, go.transform);
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) r.receiveGI = ReceiveGI.LightProbes;
        }

        static void PlaceFramesAndRails(ZoneKit k, Level0Look look, MazeLayout layout)
        {
            var spots = new System.Collections.Generic.List<(float x, float z, float rot, bool alongX)>();
            float t = k.WallThickness;
            for (int i = 0; i <= Cells; i++)
            for (int z = 0; z < Cells; z++)
            {
                if (!(i == 0 || i == Cells || layout.WallZ[i, z])) continue;
                foreach (int side in new[] { -1, 1 })
                {
                    if ((i == 0 && side < 0) || (i == Cells && side > 0)) continue;
                    float off = i == 0 || i == Cells ? 0.01f : t / 2 + 0.01f;
                    spots.Add((i * CellSize + side * off, z * CellSize + CellSize / 2, side > 0 ? 90f : 270f, false));
                }
            }
            for (int j = 0; j <= Cells; j++)
            for (int x = 0; x < Cells; x++)
            {
                if (!(j == 0 || j == Cells || layout.WallX[x, j])) continue;
                foreach (int side in new[] { -1, 1 })
                {
                    if ((j == 0 && side < 0) || (j == Cells && side > 0)) continue;
                    float off = j == 0 || j == Cells ? 0.01f : t / 2 + 0.01f;
                    spots.Add((x * CellSize + CellSize / 2, j * CellSize + side * off, side > 0 ? 0f : 180f, true));
                }
            }

            var frames = new[] { "HangingPictureFrame01", "HangingPictureFrame02", "HangingPictureFrame03", "FancyPictureFrame01", "FancyPictureFrame02" };
            var rail = WorldAssets.Lit(S, "Level0RailMaterial", new Color(0.62f, 0.6f, 0.52f), smoothness: 0.55f, metallic: 0.4f);
            var railParent = k.Child("Handrails", k.Details);
            int frameCount = 0, railCount = 0;
            for (int n = 0; n < spots.Count && (frameCount < 18 || railCount < 10); n++)
            {
                var s = spots[k.Rng.Next(spots.Count)];
                if (k.NearDoor(s.x, s.z, 1.6f)) continue;
                double roll = k.Rng.NextDouble();
                if (roll < 0.5 && frameCount < 18)
                {
                    var f = k.Place(frames[k.Rng.Next(frames.Length)], s.x, s.z, s.rot, y: 1.35f, snap: false, isStatic: true);
                    if (f == null) continue;
                    var b = ZoneKit.Bounds(f);
                    var inward = Quaternion.Euler(0, s.rot, 0) * Vector3.forward;
                    var wallPoint = k.Root.position + new Vector3(s.x, 0, s.z);
                    float centerProj = Vector3.Dot(b.center - wallPoint, inward);
                    float half = Mathf.Abs(inward.x) * b.extents.x + Mathf.Abs(inward.z) * b.extents.z;
                    f.transform.position += inward * (half - centerProj);
                    frameCount++;
                }
                else if (railCount < 10)
                {
                    var dir = s.alongX ? Vector3.right : Vector3.forward;
                    var inward = Quaternion.Euler(0, s.rot, 0) * Vector3.forward;
                    var c = new Vector3(s.x, 0.92f, s.z) + inward * 0.07f;
                    var bar = k.Primitive("Rail", "Cylinder.fbx", c, s.alongX ? new Vector3(0, 0, 90) : new Vector3(90, 0, 0), new Vector3(0.045f, 1.6f, 0.045f), rail, railParent);
                    for (int q = -1; q <= 1; q++)
                        k.Box("Bracket", c + dir * (q * 1.4f) - inward * 0.035f + Vector3.down * 0.03f, s.alongX ? new Vector3(0.03f, 0.08f, 0.07f) : new Vector3(0.07f, 0.08f, 0.03f), rail, railParent);
                    railCount++;
                }
            }
        }

        static void PlaceStains(ZoneKit k, Level0Look look, float size)
        {
            var parent = k.Child("Stains", k.Details);
            for (int n = 0; n < 34; n++)
            {
                float x = 1f + (float)k.Rng.NextDouble() * (size - 2f), z = 1f + (float)k.Rng.NextDouble() * (size - 2f);
                float s = 0.8f + (float)k.Rng.NextDouble() * 2.2f;
                if (k.HitsWall(new Rect(x - s / 2, z - s / 2, s, s), 0f)) continue;
                bool ceiling = n % 3 == 0;
                k.Quad(ceiling ? "CeilingStain" : "CarpetStain", new Vector3(x, ceiling ? H - 0.008f : 0.006f, z), new Vector2(s, s),
                    new Vector3(ceiling ? -90 : 90, (float)k.Rng.NextDouble() * 360f, 0), look.Stain, parent);
            }
        }

        static void PlaceProps(ZoneKit k, float size)
        {
            k.Reserve(0f, 0f, 4f, 4f);
            k.Reserve(size - 4f, size - 4f, 4f, 4f);
            var spots = new (string prefab, float scaleMin, float scaleMax, int count)[]
            {
                ("PlasticBottleGallon", 1f, 1f, 5), ("SchoolChair01", 1f, 1f, 2), ("Television01", 1f, 1f, 1),
                ("Trashbag", 1f, 1f, 2), ("CanRusted", 1f, 1f, 2), ("WoodenChair01", 1f, 1f, 1),
                ("PortableCassettePlayer", 1f, 1f, 1), ("AlarmClock01", 1f, 1f, 1), ("PlasticBroom", 1f, 1f, 1), ("Mousetrap", 1f, 1f, 2),
            };
            var all = new Rect(0.6f, 0.6f, size - 1.2f, size - 1.2f);
            foreach (var (prefab, a, b, count) in spots)
                for (int i = 0; i < count; i++)
                {
                    for (int tries = 0; tries < 20; tries++)
                    {
                        var go = k.Scatter(prefab, all, 0.5f, a, b, 1);
                        if (go == null) continue;
                        var bnd = ZoneKit.Bounds(go);
                        var local = new Rect(bnd.min.x - k.Root.position.x, bnd.min.z - k.Root.position.z, bnd.size.x, bnd.size.z);
                        if (k.HitsWall(local, 0.05f)) { Object.DestroyImmediate(go); continue; }
                        break;
                    }
                }
            k.Place("KoreanPublicPayphone01", 4f * 5 + 2f, 0.25f, 0f, y: 1.1f, snap: false, isStatic: true);
            k.Place("WallClock", 4f * 7 + 2f, size - 0.03f, 180f, y: 2.1f, snap: false, isStatic: true);
        }
    }

    public class MazeLayout
    {
        public readonly bool[,] WallZ, WallX;
        readonly System.Random rng;
        readonly int w, h;

        public MazeLayout(int width, int height, int seed, double removeChance, int openRooms)
        {
            w = width; h = height; rng = new System.Random(seed);
            WallZ = new bool[w + 1, h];
            WallX = new bool[w, h + 1];
            for (int i = 0; i <= w; i++) for (int z = 0; z < h; z++) WallZ[i, z] = true;
            for (int x = 0; x < w; x++) for (int j = 0; j <= h; j++) WallX[x, j] = true;
            Carve();
            for (int i = 1; i < w; i++) for (int z = 0; z < h; z++) if (WallZ[i, z] && rng.NextDouble() < removeChance) WallZ[i, z] = false;
            for (int x = 0; x < w; x++) for (int j = 1; j < h; j++) if (WallX[x, j] && rng.NextDouble() < removeChance) WallX[x, j] = false;
            for (int r = 0; r < openRooms; r++)
            {
                int rw = rng.Next(2, 4), rh = rng.Next(2, 4), x0 = rng.Next(0, w - rw + 1), z0 = rng.Next(0, h - rh + 1);
                for (int i = x0 + 1; i < x0 + rw; i++) for (int z = z0; z < z0 + rh; z++) WallZ[i, z] = false;
                for (int x = x0; x < x0 + rw; x++) for (int j = z0 + 1; j < z0 + rh; j++) WallX[x, j] = false;
            }
        }

        void Carve()
        {
            var visited = new bool[w, h];
            var stack = new Stack<(int, int)>();
            visited[0, 0] = true; stack.Push((0, 0));
            var dirs = new List<(int, int)>(4);
            while (stack.Count > 0)
            {
                var (x, z) = stack.Peek();
                dirs.Clear();
                if (x > 0 && !visited[x - 1, z]) dirs.Add((-1, 0));
                if (x < w - 1 && !visited[x + 1, z]) dirs.Add((1, 0));
                if (z > 0 && !visited[x, z - 1]) dirs.Add((0, -1));
                if (z < h - 1 && !visited[x, z + 1]) dirs.Add((0, 1));
                if (dirs.Count == 0) { stack.Pop(); continue; }
                var (dx, dz) = dirs[rng.Next(dirs.Count)];
                int nx = x + dx, nz = z + dz;
                if (dx != 0) WallZ[Mathf.Max(x, nx), z] = false; else WallX[x, Mathf.Max(z, nz)] = false;
                visited[nx, nz] = true; stack.Push((nx, nz));
            }
        }
    }
}
