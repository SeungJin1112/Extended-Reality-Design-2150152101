using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Faces = Backrooms.EditorTools.BackroomsMeshFactory.Faces;

namespace Backrooms.EditorTools.World
{
    public class ZoneKit
    {
        public const float Cell = 0.5f;

        public readonly string Name;
        public readonly string AssetFolder;
        public readonly Transform Root, Architecture, Lights, Props, Details, Markers;
        public float Height;
        public float WallThickness = 0.15f;
        public float DoorHeight = 2.15f;
        public bool Thresholds = true;

        readonly bool[,] floor;
        readonly bool[,] hole;
        readonly int nx, nz;
        readonly List<Wall> partitions = new List<Wall>();
        readonly List<(float x, float z, float width, float top)> openings = new List<(float, float, float, float)>();
        readonly List<Rect> occupied = new List<Rect>();
        public readonly List<Vector2> DoorSpots = new List<Vector2>();

        public bool NearDoor(float x, float z, float distance) => DoorSpots.Exists(d => Vector2.Distance(d, new Vector2(x, z)) < distance);
        readonly GameObject colliders;
        public readonly System.Random Rng;

        struct Wall { public Vector2 A, B; public List<(float from, float to, float top)> Gaps; }
        struct Piece { public bool AlongX; public float Line, From, To, Y0, Y1, T; public int FloorSide; public bool CapFrom, CapTo, Bottom; }

        public ZoneKit(string name, string assetFolder, Vector3 origin, float width, float depth, float height, int seed)
        {
            Name = name;
            AssetFolder = assetFolder;
            Height = height;
            Rng = new System.Random(seed);
            nx = Mathf.CeilToInt(width / Cell);
            nz = Mathf.CeilToInt(depth / Cell);
            floor = new bool[nx, nz];
            hole = new bool[nx, nz];

            Root = new GameObject(name).transform;
            Root.position = origin;
            Architecture = Child("Architecture");
            Lights = Child("Lights");
            Props = Child("Props");
            Details = Child("Details");
            Markers = Child("Markers");
            colliders = new GameObject("Colliders");
            colliders.transform.SetParent(Architecture, false);
        }

        public Transform Child(string name, Transform parent = null)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent ?? Root, false);
            return t;
        }

        public void Room(float x, float z, float w, float d) => Fill(x, z, w, d, true);
        public void Carve(float x, float z, float w, float d) => Fill(x, z, w, d, false);

        public void Hole(float x, float z, float w, float d)
        {
            int i0 = Mathf.RoundToInt(x / Cell), j0 = Mathf.RoundToInt(z / Cell);
            int i1 = Mathf.RoundToInt((x + w) / Cell), j1 = Mathf.RoundToInt((z + d) / Cell);
            for (int i = Mathf.Max(0, i0); i < Mathf.Min(nx, i1); i++)
            for (int j = Mathf.Max(0, j0); j < Mathf.Min(nz, j1); j++)
                hole[i, j] = true;
        }

        public bool HitsWall(Rect r, float margin = 0.05f)
        {
            if (!IsInside(r.xMin, r.yMin) || !IsInside(r.xMax, r.yMax) || !IsInside(r.xMin, r.yMax) || !IsInside(r.xMax, r.yMin))
                return true;
            float half = WallThickness / 2 + margin;
            foreach (var w in partitions)
            {
                bool alongX = Mathf.Abs(w.B.y - w.A.y) < 0.001f;
                if (alongX)
                {
                    if (w.A.y > r.yMin - half && w.A.y < r.yMax + half && w.B.x > r.xMin && w.A.x < r.xMax) return true;
                }
                else if (w.A.x > r.xMin - half && w.A.x < r.xMax + half && w.B.y > r.yMin && w.A.y < r.yMax) return true;
            }
            return false;
        }

        public void PoolBasin(string name, float x, float z, float w, float d, float depth, float waterLevel, Material tile, Material coping, Material water)
        {
            Hole(x, z, w, d);
            const float t = 0.2f, rim = 0.3f, rimH = 0.06f;
            var basin = new BackroomsMeshFactory();
            basin.AddHorizontalRect(x, z, x + w, z + d, -depth, faceDown: false);
            basin.AddBox(new Vector3(x + w / 2, -depth / 2, z - t / 2), new Vector3(w + 2 * t, depth, t), Faces.PosZ);
            basin.AddBox(new Vector3(x + w / 2, -depth / 2, z + d + t / 2), new Vector3(w + 2 * t, depth, t), Faces.NegZ);
            basin.AddBox(new Vector3(x - t / 2, -depth / 2, z + d / 2), new Vector3(t, depth, d), Faces.PosX);
            basin.AddBox(new Vector3(x + w + t / 2, -depth / 2, z + d / 2), new Vector3(t, depth, d), Faces.NegX);
            MeshObject($"{name}Basin", basin, tile, 1f);

            var rimMesh = new BackroomsMeshFactory();
            rimMesh.AddBox(new Vector3(x + w / 2, rimH / 2, z - rim / 2), new Vector3(w + 2 * rim, rimH, rim), Faces.PosY | Faces.PosZ | Faces.NegZ);
            rimMesh.AddBox(new Vector3(x + w / 2, rimH / 2, z + d + rim / 2), new Vector3(w + 2 * rim, rimH, rim), Faces.PosY | Faces.NegZ | Faces.PosZ);
            rimMesh.AddBox(new Vector3(x - rim / 2, rimH / 2, z + d / 2), new Vector3(rim, rimH, d), Faces.PosY | Faces.PosX | Faces.NegX);
            rimMesh.AddBox(new Vector3(x + w + rim / 2, rimH / 2, z + d / 2), new Vector3(rim, rimH, d), Faces.PosY | Faces.NegX | Faces.PosX);
            MeshObject($"{name}Rim", rimMesh, coping, 3f);

            Collider(new Vector3(x + w / 2, -depth - 0.1f, z + d / 2), new Vector3(w, 0.2f, d));
            Collider(new Vector3(x + w / 2, -depth / 2, z - t / 2), new Vector3(w + 2 * t, depth, t));
            Collider(new Vector3(x + w / 2, -depth / 2, z + d + t / 2), new Vector3(w + 2 * t, depth, t));
            Collider(new Vector3(x - t / 2, -depth / 2, z + d / 2), new Vector3(t, depth, d));
            Collider(new Vector3(x + w + t / 2, -depth / 2, z + d / 2), new Vector3(t, depth, d));

            var waterMesh = new BackroomsMeshFactory();
            waterMesh.AddHorizontalRect(x, z, x + w, z + d, waterLevel, faceDown: false);
            var waterGo = MeshObject($"{name}Water", waterMesh, water, 1f, lightmapped: false);
            var wr = waterGo.GetComponent<MeshRenderer>();
            wr.shadowCastingMode = ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(waterGo, StaticEditorFlags.BatchingStatic);
        }

        bool IsInside(float x, float z)
        {
            int i = Mathf.FloorToInt(x / Cell), j = Mathf.FloorToInt(z / Cell);
            return i >= 0 && j >= 0 && i < nx && j < nz && floor[i, j];
        }

        void Fill(float x, float z, float w, float d, bool value)
        {
            int i0 = Mathf.RoundToInt(x / Cell), j0 = Mathf.RoundToInt(z / Cell);
            int i1 = Mathf.RoundToInt((x + w) / Cell), j1 = Mathf.RoundToInt((z + d) / Cell);
            for (int i = Mathf.Max(0, i0); i < Mathf.Min(nx, i1); i++)
            for (int j = Mathf.Max(0, j0); j < Mathf.Min(nz, j1); j++)
                floor[i, j] = value;
        }

        public bool IsFloor(float x, float z)
        {
            int i = Mathf.FloorToInt(x / Cell), j = Mathf.FloorToInt(z / Cell);
            return i >= 0 && j >= 0 && i < nx && j < nz && floor[i, j] && !hole[i, j];
        }

        public void Partition(float x1, float z1, float x2, float z2, params (float at, float width)[] doors)
        {
            var w = new Wall { A = new Vector2(Mathf.Min(x1, x2), Mathf.Min(z1, z2)), B = new Vector2(Mathf.Max(x1, x2), Mathf.Max(z1, z2)), Gaps = new List<(float, float, float)>() };
            foreach (var (at, width) in doors)
            {
                w.Gaps.Add((at - width / 2, at + width / 2, DoorHeight));
                bool alongX = Mathf.Abs(w.B.y - w.A.y) < 0.001f;
                DoorSpots.Add(alongX ? new Vector2(w.A.x + at, w.A.y) : new Vector2(w.A.x, w.A.y + at));
            }
            partitions.Add(w);
        }

        public void PartitionOpenGap(int partitionIndex, float at, float width)
        {
            var w = partitions[partitionIndex];
            w.Gaps.Add((at - width / 2, at + width / 2, 999f));
        }

        public int PartitionCount => partitions.Count;

        public void Opening(float x, float z, float width, float top = -1f)
        {
            openings.Add((x, z, width, top < 0 ? DoorHeight : top));
            DoorSpots.Add(new Vector2(x, z));
        }

        public class Surfaces
        {
            public Material Floor, Wall, Ceiling, Baseboard, WallLower;
            public float WainscotHeight = 0f;
            public bool HasCeiling = true;
            public float BaseboardHeight = 0.1f;
        }

        public void Build(Surfaces s)
        {
            var floorMesh = new BackroomsMeshFactory();
            var ceilMesh = new BackroomsMeshFactory();
            foreach (var r in GreedyRects((i, j) => floor[i, j] && !hole[i, j]))
            {
                floorMesh.AddHorizontalRect(r.xMin, r.yMin, r.xMax, r.yMax, 0f, faceDown: false);
                Collider(new Vector3(r.center.x, -0.1f, r.center.y), new Vector3(r.width, 0.2f, r.height));
            }
            if (s.HasCeiling)
                foreach (var r in GreedyRects((i, j) => floor[i, j]))
                {
                    ceilMesh.AddHorizontalRect(r.xMin, r.yMin, r.xMax, r.yMax, Height, faceDown: true);
                    Collider(new Vector3(r.center.x, Height + 0.1f, r.center.y), new Vector3(r.width, 0.2f, r.height));
                }
            foreach (var o in openings)
            {
                const float probe = 0.3f, extra = 0.2f;
                bool floorWest = IsInside(o.x - probe, o.z), floorEast = IsInside(o.x + probe, o.z);
                bool floorSouth = IsInside(o.x, o.z - probe), floorNorth = IsInside(o.x, o.z + probe);
                float x0, x1, z0, z1;
                if (floorWest != floorEast)
                {
                    x0 = floorWest ? o.x : o.x - WallThickness - extra;
                    x1 = floorWest ? o.x + WallThickness + extra : o.x;
                    z0 = o.z - o.width / 2; z1 = o.z + o.width / 2;
                }
                else if (floorSouth != floorNorth)
                {
                    z0 = floorSouth ? o.z : o.z - WallThickness - extra;
                    z1 = floorSouth ? o.z + WallThickness + extra : o.z;
                    x0 = o.x - o.width / 2; x1 = o.x + o.width / 2;
                }
                else continue;
                float mx0 = x0, mx1 = x1, mz0 = z0, mz1 = z1;
                if (floorWest != floorEast) { if (floorWest) mx1 = o.x + WallThickness; else mx0 = o.x - WallThickness; }
                else { if (floorSouth) mz1 = o.z + WallThickness; else mz0 = o.z - WallThickness; }
                if (Thresholds) floorMesh.AddHorizontalRect(mx0, mz0, mx1, mz1, 0f, faceDown: false);
                Collider(new Vector3((x0 + x1) / 2, -0.1f, (z0 + z1) / 2), new Vector3(x1 - x0, 0.2f, z1 - z0));
            }
            MeshObject("Floor", floorMesh, s.Floor, 1f);
            if (s.HasCeiling) MeshObject("Ceiling", ceilMesh, s.Ceiling, 1f);

            var pieces = new List<Piece>();
            PerimeterPieces(pieces);
            PartitionPieces(pieces);

            var wallMesh = new BackroomsMeshFactory();
            var lowerMesh = new BackroomsMeshFactory();
            var boardMesh = new BackroomsMeshFactory();
            foreach (var p in pieces)
            {
                float split = s.WallLower != null && s.WainscotHeight > 0 ? Mathf.Clamp(s.WainscotHeight, p.Y0, p.Y1) : p.Y0;
                if (split > p.Y0) AddPiece(lowerMesh, p, p.Y0, split, bottom: p.Bottom, top: false);
                AddPiece(wallMesh, p, split, p.Y1, bottom: p.Bottom && split <= p.Y0, top: false);
                Collider(PieceCenter(p, p.Y0, p.Y1), PieceSize(p, p.Y0, p.Y1));

                if (p.Y0 <= 0.001f && s.Baseboard != null)
                    AddBaseboard(boardMesh, p, s.BaseboardHeight);
            }
            MeshObject("Walls", wallMesh, s.Wall, 1f);
            if (lowerMesh.VertexCount > 0) MeshObject("WallsLower", lowerMesh, s.WallLower, 1f);
            if (boardMesh.VertexCount > 0) MeshObject("Baseboards", boardMesh, s.Baseboard, 6f);
        }

        void PerimeterPieces(List<Piece> pieces)
        {
            float t = WallThickness;
            for (int j = 0; j <= nz; j++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    int i = 0;
                    while (i < nx)
                    {
                        if (!EdgeX(i, j, side)) { i++; continue; }
                        int start = i;
                        while (i < nx && EdgeX(i, j, side)) i++;
                        AddRunWithOpenings(pieces, alongX: true, line: j * Cell, from: start * Cell, to: i * Cell, floorSide: side, t);
                    }
                }
            }
            for (int i = 0; i <= nx; i++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    int j = 0;
                    while (j < nz)
                    {
                        if (!EdgeZ(i, j, side)) { j++; continue; }
                        int start = j;
                        while (j < nz && EdgeZ(i, j, side)) j++;
                        AddRunWithOpenings(pieces, alongX: false, line: i * Cell, from: start * Cell, to: j * Cell, floorSide: side, t);
                    }
                }
            }
        }

        bool EdgeX(int i, int j, int side)
        {
            bool below = j - 1 >= 0 && floor[i, j - 1];
            bool above = j < nz && floor[i, j];
            return side < 0 ? (below && !above) : (above && !below);
        }

        bool EdgeZ(int i, int j, int side)
        {
            bool left = i - 1 >= 0 && floor[i - 1, j];
            bool right = i < nx && floor[i, j];
            return side < 0 ? (left && !right) : (right && !left);
        }

        void AddRunWithOpenings(List<Piece> pieces, bool alongX, float line, float from, float to, int floorSide, float t)
        {
            var gaps = new List<(float from, float to, float top)>();
            foreach (var o in openings)
            {
                float oLine = alongX ? o.z : o.x, oPos = alongX ? o.x : o.z;
                if (Mathf.Abs(oLine - line) < 0.01f && oPos > from && oPos < to)
                    gaps.Add((oPos - o.width / 2, oPos + o.width / 2, o.top));
            }
            var basePiece = new Piece { AlongX = alongX, Line = line + (floorSide < 0 ? t / 2 : -t / 2), T = t, FloorSide = floorSide };
            CutPieces(pieces, basePiece, from, to, gaps, capEnds: false);
        }

        void PartitionPieces(List<Piece> pieces)
        {
            foreach (var w in partitions)
            {
                bool alongX = Mathf.Abs(w.B.y - w.A.y) < 0.001f;
                float line = alongX ? w.A.y : w.A.x;
                float from = alongX ? w.A.x : w.A.y, to = alongX ? w.B.x : w.B.y;
                var gaps = w.Gaps.Select(g => (from + g.from, from + g.to, g.top)).ToList();
                var basePiece = new Piece { AlongX = alongX, Line = line, T = WallThickness, FloorSide = 0 };
                CutPieces(pieces, basePiece, from, to, gaps, capEnds: true);
            }
        }

        void CutPieces(List<Piece> pieces, Piece proto, float from, float to, List<(float from, float to, float top)> gaps, bool capEnds)
        {
            gaps = gaps.Where(g => g.to > from && g.from < to).OrderBy(g => g.from).ToList();
            float cursor = from;
            bool capPrev = capEnds;
            foreach (var g in gaps)
            {
                float gFrom = Mathf.Max(from, g.from), gTo = Mathf.Min(to, g.to);
                if (gFrom > cursor + 0.01f)
                {
                    var p = proto; p.From = cursor; p.To = gFrom; p.Y0 = 0; p.Y1 = Height; p.CapFrom = capPrev; p.CapTo = true;
                    pieces.Add(p);
                }
                if (g.top < Height)
                {
                    var l = proto; l.From = gFrom; l.To = gTo; l.Y0 = g.top; l.Y1 = Height; l.CapFrom = false; l.CapTo = false; l.Bottom = true;
                    pieces.Add(l);
                }
                cursor = gTo;
                capPrev = true;
            }
            if (to > cursor + 0.01f)
            {
                var p = proto; p.From = cursor; p.To = to; p.Y0 = 0; p.Y1 = Height; p.CapFrom = capPrev; p.CapTo = capEnds;
                pieces.Add(p);
            }
        }

        Vector3 PieceCenter(Piece p, float y0, float y1)
        {
            float mid = (p.From + p.To) / 2, y = (y0 + y1) / 2;
            return p.AlongX ? new Vector3(mid, y, p.Line) : new Vector3(p.Line, y, mid);
        }

        Vector3 PieceSize(Piece p, float y0, float y1)
        {
            float len = p.To - p.From, h = y1 - y0;
            return p.AlongX ? new Vector3(len, h, p.T) : new Vector3(p.T, h, len);
        }

        void AddPiece(BackroomsMeshFactory m, Piece p, float y0, float y1, bool bottom, bool top)
        {
            if (y1 - y0 < 0.001f) return;
            Faces f;
            if (p.AlongX)
            {
                f = p.FloorSide == 0 ? (Faces.PosZ | Faces.NegZ) : (p.FloorSide < 0 ? Faces.NegZ : Faces.PosZ);
                if (p.CapFrom) f |= Faces.NegX;
                if (p.CapTo) f |= Faces.PosX;
            }
            else
            {
                f = p.FloorSide == 0 ? (Faces.PosX | Faces.NegX) : (p.FloorSide < 0 ? Faces.NegX : Faces.PosX);
                if (p.CapFrom) f |= Faces.NegZ;
                if (p.CapTo) f |= Faces.PosZ;
            }
            if (bottom) f |= Faces.NegY;
            if (top) f |= Faces.PosY;
            m.AddBox(PieceCenter(p, y0, y1), PieceSize(p, y0, y1), f);
        }

        void AddBaseboard(BackroomsMeshFactory m, Piece p, float h)
        {
            const float depth = 0.02f;
            float off = p.T / 2 + depth / 2;
            foreach (int s in p.FloorSide == 0 ? new[] { -1, 1 } : new[] { p.FloorSide < 0 ? -1 : 1 })
            {
                float line = p.Line + s * off;
                float mid = (p.From + p.To) / 2, len = p.To - p.From + (p.FloorSide == 0 ? 0 : 0);
                var c = p.AlongX ? new Vector3(mid, h / 2, line) : new Vector3(line, h / 2, mid);
                var size = p.AlongX ? new Vector3(len, h, depth) : new Vector3(depth, h, len);
                var f = Faces.PosY | (p.AlongX ? (s < 0 ? Faces.NegZ : Faces.PosZ) : (s < 0 ? Faces.NegX : Faces.PosX));
                if (p.CapFrom) f |= p.AlongX ? Faces.NegX : Faces.NegZ;
                if (p.CapTo) f |= p.AlongX ? Faces.PosX : Faces.PosZ;
                m.AddBox(c, size, f);
            }
        }

        List<Rect> GreedyRects(System.Func<int, int, bool> cell)
        {
            var used = new bool[nx, nz];
            var rects = new List<Rect>();
            for (int j = 0; j < nz; j++)
            for (int i = 0; i < nx; i++)
            {
                if (!cell(i, j) || used[i, j]) continue;
                int w = 1; while (i + w < nx && cell(i + w, j) && !used[i + w, j]) w++;
                int h = 1;
                while (j + h < nz)
                {
                    bool ok = true;
                    for (int k = 0; k < w; k++) if (!cell(i + k, j + h) || used[i + k, j + h]) { ok = false; break; }
                    if (!ok) break; h++;
                }
                for (int a = 0; a < w; a++) for (int b = 0; b < h; b++) used[i + a, j + b] = true;
                rects.Add(new Rect(i * Cell, j * Cell, w * Cell, h * Cell));
            }
            return rects;
        }

        public GameObject MeshObject(string name, BackroomsMeshFactory factory, Material mat, float lightmapScale, Transform parent = null, bool lightmapped = true)
        {
            var dir = $"{AssetFolder}/Meshes/Generated";
            QuestProjectSetup.EnsureFolder(dir);
            var mesh = factory.SaveAsset($"{Name}{name}Mesh", dir, lightmapScale);
            var go = new GameObject(name);
            go.transform.SetParent(parent ?? Architecture, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.receiveGI = lightmapped ? ReceiveGI.Lightmaps : ReceiveGI.LightProbes;
            if (!Mathf.Approximately(lightmapScale, 1f))
            {
                var so = new SerializedObject(r);
                so.FindProperty("m_ScaleInLightmap").floatValue = lightmapScale;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            MakeStatic(go);
            return go;
        }

        public void Collider(Vector3 center, Vector3 size)
        {
            var c = colliders.AddComponent<BoxCollider>();
            c.center = center;
            c.size = size;
        }

        public static void MakeStatic(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject,
                    StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic |
                    StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic);
        }

        static Dictionary<string, string> prefabIndex;

        public static string FindPrefab(string name)
        {
            if (prefabIndex == null)
            {
                prefabIndex = new Dictionary<string, string>();
                foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Objects", "Assets/Structure", "Assets/Player" }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var key = Path.GetFileNameWithoutExtension(path);
                    if (!prefabIndex.ContainsKey(key)) prefabIndex[key] = path;
                }
            }
            return prefabIndex.TryGetValue(name, out var p) ? p : null;
        }

        public static void ResetIndex() => prefabIndex = null;

        public GameObject Place(string prefab, float x, float z, float rotY = 0f, float scale = 1f, float y = 0f, Transform parent = null, bool snap = true, bool isStatic = false)
        {
            var path = FindPrefab(prefab);
            if (path == null) { Debug.LogWarning($"[Backrooms] {Name}: 프리팹 없음 {prefab}"); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), parent ?? Props);
            go.transform.localPosition = new Vector3(x, y, z);
            go.transform.localRotation = Quaternion.Euler(0, rotY, 0);
            go.transform.localScale = Vector3.one * scale;
            if (snap)
            {
                var b = Bounds(go);
                if (b.size != Vector3.zero)
                    go.transform.position += Vector3.up * (Root.position.y + y - b.min.y);
            }
            if (isStatic) MakeStatic(go);
            var visual = Bounds(go);
            foreach (var c in go.GetComponentsInChildren<BoxCollider>(true))
            {
                var s = c.bounds.size;
                bool oversized = s.x > Mathf.Max(3f, visual.size.x * 1.5f) || s.z > Mathf.Max(3f, visual.size.z * 1.5f) || s.y > Mathf.Max(4f, visual.size.y * 1.5f);
                if (oversized || (s.x > 2.5f && s.z > 2.5f && s.y < 2.5f && !isStatic))
                    c.enabled = false;
            }
            return go;
        }

        public GameObject Scatter(string prefab, Rect area, float radius, float scaleMin = 1f, float scaleMax = 1f, int tries = 30)
        {
            for (int k = 0; k < tries; k++)
            {
                float x = Mathf.Lerp(area.xMin + radius, area.xMax - radius, (float)Rng.NextDouble());
                float z = Mathf.Lerp(area.yMin + radius, area.yMax - radius, (float)Rng.NextDouble());
                var r = new Rect(x - radius, z - radius, radius * 2, radius * 2);
                if (!IsFloor(r.xMin, r.yMin) || !IsFloor(r.xMax, r.yMax) || !IsFloor(r.xMin, r.yMax) || !IsFloor(r.xMax, r.yMin)) continue;
                if (occupied.Any(o => o.Overlaps(r))) continue;
                occupied.Add(r);
                float s = Mathf.Lerp(scaleMin, scaleMax, (float)Rng.NextDouble());
                return Place(prefab, x, z, (float)Rng.NextDouble() * 360f, s);
            }
            return null;
        }

        public void Reserve(float x, float z, float w, float d) => occupied.Add(new Rect(x, z, w, d));

        public static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        public Light AreaLight(float x, float y, float z, float w, float d, Color color, float intensity, Transform parent = null)
        {
            var go = new GameObject("AreaLight");
            go.transform.SetParent(parent ?? Lights, false);
            go.transform.localPosition = new Vector3(x, y, z);
            go.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var l = go.AddComponent<Light>();
            l.type = (LightType)3;
            l.areaSize = new Vector2(w, d);
            l.color = color;
            l.intensity = intensity;
            l.lightmapBakeType = LightmapBakeType.Baked;
            l.shadows = LightShadows.Soft;
            return l;
        }

        public Light PointLight(float x, float y, float z, Color color, float intensity, float range, Transform parent = null, float shadowRadius = 0.2f)
        {
            var go = new GameObject("PointLight");
            go.transform.SetParent(parent ?? Lights, false);
            go.transform.localPosition = new Vector3(x, y, z);
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.lightmapBakeType = LightmapBakeType.Baked;
            l.shadows = LightShadows.Soft;
            l.shadowRadius = shadowRadius;
            return l;
        }

        public Light SpotLight(float x, float y, float z, Vector3 euler, Color color, float intensity, float range, float angle, Transform parent = null)
        {
            var go = new GameObject("SpotLight");
            go.transform.SetParent(parent ?? Lights, false);
            go.transform.localPosition = new Vector3(x, y, z);
            go.transform.localRotation = Quaternion.Euler(euler);
            var l = go.AddComponent<Light>();
            l.type = LightType.Spot;
            l.color = color; l.intensity = intensity; l.range = range; l.spotAngle = angle;
            l.lightmapBakeType = LightmapBakeType.Baked;
            l.shadows = LightShadows.Soft;
            l.shadowRadius = 0.15f;
            return l;
        }

        public Transform Marker(string name, float x, float z, float rotY)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Markers, false);
            go.transform.localPosition = new Vector3(x, 0, z);
            go.transform.localRotation = Quaternion.Euler(0, rotY, 0);
            var icon = EditorGUIUtility.IconContent("sv_label_1").image as Texture2D;
            if (icon != null) EditorGUIUtility.SetIconForObject(go, icon);
            return go.transform;
        }

        public void LightProbes(float spacing, params float[] heights)
        {
            var positions = new List<Vector3>();
            for (float x = spacing / 2; x < nx * Cell; x += spacing)
            for (float z = spacing / 2; z < nz * Cell; z += spacing)
            {
                if (!IsInside(x, z)) continue;
                bool overHole = !IsFloor(x, z);
                positions.Add(new Vector3(x, overHole ? -0.3f : 0.06f, z));
                foreach (var h in heights) positions.Add(new Vector3(x, h, z));
            }
            for (int i = 0; i < nx; i += Mathf.Max(1, Mathf.RoundToInt(spacing / Cell)))
            for (int j = 0; j < nz; j += Mathf.Max(1, Mathf.RoundToInt(spacing / Cell)))
            {
                if (!floor[i, j] || !hole[i, j]) continue;
                positions.Add(new Vector3((i + 0.5f) * Cell, 0.25f, (j + 0.5f) * Cell));
            }
            if (positions.Count < 4) return;
            var go = new GameObject("LightProbes");
            go.transform.SetParent(Lights, false);
            go.AddComponent<LightProbeGroup>().probePositions = positions.ToArray();
        }

        public void ReflectionProbe(float x, float y, float z, Vector3 size)
        {
            var go = new GameObject("ReflectionProbe");
            go.transform.SetParent(Lights, false);
            go.transform.localPosition = new Vector3(x, y, z);
            var p = go.AddComponent<ReflectionProbe>();
            p.mode = ReflectionProbeMode.Baked;
            p.size = size;
            p.resolution = 128;
            p.boxProjection = true;
        }

        public GameObject Box(string name, Vector3 center, Vector3 size, Material mat, Transform parent = null, bool collider = false, bool isStatic = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent ?? Details, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            go.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (collider) go.AddComponent<BoxCollider>();
            if (isStatic) MakeStatic(go);
            return go;
        }

        public GameObject Quad(string name, Vector3 center, Vector2 size, Vector3 euler, Material mat, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent ?? Details, false);
            go.transform.localPosition = center;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = new Vector3(size.x, size.y, 1);
            go.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveGI = ReceiveGI.LightProbes;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            return go;
        }

        public GameObject Primitive(string name, string builtinMesh, Vector3 center, Vector3 euler, Vector3 scale, Material mat, Transform parent = null, bool isStatic = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent ?? Details, false);
            go.transform.localPosition = center;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>(builtinMesh);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (isStatic) MakeStatic(go);
            return go;
        }
    }
}
