using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Backrooms.EditorTools
{
    public class BackroomsMeshFactory
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Vector3> normals = new List<Vector3>();
        readonly List<Vector4> tangents = new List<Vector4>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<int> triangles = new List<int>();
        float surfaceArea;

        public const float LightmapTexelsPerMeter = 4f;

        [System.Flags]
        public enum Faces
        {
            None = 0, PosX = 1, NegX = 2, PosY = 4, NegY = 8, PosZ = 16, NegZ = 32,
            Sides = PosX | NegX | PosZ | NegZ,
            All = Sides | PosY | NegY,
        }

        public int VertexCount => vertices.Count;

        public BackroomsMeshFactory AddBox(Vector3 center, Vector3 size, bool skipBottom = false, bool skipTop = false)
        {
            var faces = Faces.All;
            if (skipBottom) faces &= ~Faces.NegY;
            if (skipTop) faces &= ~Faces.PosY;
            return AddBox(center, size, faces);
        }

        public BackroomsMeshFactory AddBox(Vector3 center, Vector3 size, Faces faces)
        {
            var h = size * 0.5f;
            var min = center - h;
            var max = center + h;

            if ((faces & Faces.PosX) != 0)
                AddQuad(new Vector3(max.x, min.y, min.z), Vector3.forward, Vector3.up, size.z, size.y, Vector3.right, new Vector2(min.z, min.y));
            if ((faces & Faces.NegX) != 0)
                AddQuad(new Vector3(min.x, min.y, max.z), Vector3.back, Vector3.up, size.z, size.y, Vector3.left, new Vector2(-max.z, min.y));
            if ((faces & Faces.PosZ) != 0)
                AddQuad(new Vector3(max.x, min.y, max.z), Vector3.left, Vector3.up, size.x, size.y, Vector3.forward, new Vector2(-max.x, min.y));
            if ((faces & Faces.NegZ) != 0)
                AddQuad(new Vector3(min.x, min.y, min.z), Vector3.right, Vector3.up, size.x, size.y, Vector3.back, new Vector2(min.x, min.y));
            if ((faces & Faces.PosY) != 0)
                AddQuad(new Vector3(min.x, max.y, min.z), Vector3.right, Vector3.forward, size.x, size.z, Vector3.up, new Vector2(min.x, min.z));
            if ((faces & Faces.NegY) != 0)
                AddQuad(new Vector3(min.x, min.y, max.z), Vector3.right, Vector3.back, size.x, size.z, Vector3.down, new Vector2(min.x, -max.z));
            return this;
        }

        public BackroomsMeshFactory AddHorizontalRect(float minX, float minZ, float maxX, float maxZ, float y, bool faceDown)
        {
            float w = maxX - minX, d = maxZ - minZ;
            if (faceDown)
                AddQuad(new Vector3(minX, y, maxZ), Vector3.right, Vector3.back, w, d, Vector3.down, new Vector2(minX, -maxZ));
            else
                AddQuad(new Vector3(minX, y, minZ), Vector3.right, Vector3.forward, w, d, Vector3.up, new Vector2(minX, minZ));
            return this;
        }

        public BackroomsMeshFactory AddHorizontalQuad(float width, float depth, float y, bool faceDown)
        {
            if (faceDown)
                AddQuad(new Vector3(-width * 0.5f, y, depth * 0.5f), Vector3.right, Vector3.back, width, depth, Vector3.down, new Vector2(-width * 0.5f, -depth * 0.5f));
            else
                AddQuad(new Vector3(-width * 0.5f, y, -depth * 0.5f), Vector3.right, Vector3.forward, width, depth, Vector3.up, new Vector2(-width * 0.5f, -depth * 0.5f));
            return this;
        }

        void AddQuad(Vector3 origin, Vector3 uDir, Vector3 vDir, float uLen, float vLen, Vector3 normal, Vector2 uvOrigin)
        {
            int start = vertices.Count;
            surfaceArea += uLen * vLen;
            var u = uDir * uLen;
            var v = vDir * vLen;

            vertices.Add(origin);
            vertices.Add(origin + v);
            vertices.Add(origin + v + u);
            vertices.Add(origin + u);

            uvs.Add(uvOrigin);
            uvs.Add(uvOrigin + new Vector2(0f, vLen));
            uvs.Add(uvOrigin + new Vector2(uLen, vLen));
            uvs.Add(uvOrigin + new Vector2(uLen, 0f));

            var tangent = new Vector4(uDir.x, uDir.y, uDir.z, -1f);
            for (int i = 0; i < 4; i++)
            {
                normals.Add(normal);
                tangents.Add(tangent);
            }

            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        public BackroomsMeshFactory AddQuadPoints(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
        {
            int start = vertices.Count;
            surfaceArea += (Vector3.Cross(b - a, d - a).magnitude + Vector3.Cross(b - c, d - c).magnitude) * 0.5f;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            uvs.Add(ua); uvs.Add(ub); uvs.Add(uc); uvs.Add(ud);
            var t = (d - a).sqrMagnitude > 1e-8f ? (d - a).normalized : (c - b).normalized;
            float w = Vector3.Dot(Vector3.Cross(normal, t), b - a) >= 0f ? 1f : -1f;
            for (int i = 0; i < 4; i++) { normals.Add(normal); tangents.Add(new Vector4(t.x, t.y, t.z, w)); }
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) >= 0f)
                triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            else
                triangles.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
            return this;
        }

        public BackroomsMeshFactory AddArchWallX(float x0, float x1, float zc, float height, float thick, float pier, float opening, float spring, int segments = 14)
        {
            float zf = zc - thick / 2, zb = zc + thick / 2, r = opening / 2;
            int bays = Mathf.Max(1, Mathf.FloorToInt((x1 - x0 - pier) / (pier + opening)));
            float used = bays * (pier + opening) + pier;
            float start = x0 + (x1 - x0 - used) / 2;

            void Wall(float a, float b, float y0, float y1)
            {
                if (b - a < 1e-4f || y1 - y0 < 1e-4f) return;
                AddQuadPoints(new Vector3(a, y0, zf), new Vector3(a, y1, zf), new Vector3(b, y1, zf), new Vector3(b, y0, zf), Vector3.back,
                    new Vector2(a, y0), new Vector2(a, y1), new Vector2(b, y1), new Vector2(b, y0));
                AddQuadPoints(new Vector3(b, y0, zb), new Vector3(b, y1, zb), new Vector3(a, y1, zb), new Vector3(a, y0, zb), Vector3.forward,
                    new Vector2(-b, y0), new Vector2(-b, y1), new Vector2(-a, y1), new Vector2(-a, y0));
            }
            void Jamb(float x, float y0, float y1, float dir)
            {
                var n = new Vector3(dir, 0, 0);
                AddQuadPoints(new Vector3(x, y0, zf), new Vector3(x, y1, zf), new Vector3(x, y1, zb), new Vector3(x, y0, zb), n,
                    new Vector2(zf, y0), new Vector2(zf, y1), new Vector2(zb, y1), new Vector2(zb, y0));
            }

            Wall(x0, start, 0, height);
            Wall(start + used, x1, 0, height);
            float x = start;
            for (int i = 0; i < bays; i++)
            {
                Wall(x, x + pier, 0, height);
                if (i == 0 && start - x0 < 1e-3f) Jamb(x, 0, height, -1f);
                Jamb(x + pier, 0, spring, 1f);
                float ox = x + pier, cx = ox + r;
                for (int s = 0; s < segments; s++)
                {
                    float t0 = Mathf.PI * s / segments, t1 = Mathf.PI * (s + 1) / segments;
                    float ax = cx - r * Mathf.Cos(t0), ay = spring + r * Mathf.Sin(t0);
                    float bx = cx - r * Mathf.Cos(t1), by = spring + r * Mathf.Sin(t1);
                    AddQuadPoints(new Vector3(ax, ay, zf), new Vector3(ax, height, zf), new Vector3(bx, height, zf), new Vector3(bx, by, zf), Vector3.back,
                        new Vector2(ax, ay), new Vector2(ax, height), new Vector2(bx, height), new Vector2(bx, by));
                    AddQuadPoints(new Vector3(bx, by, zb), new Vector3(bx, height, zb), new Vector3(ax, height, zb), new Vector3(ax, ay, zb), Vector3.forward,
                        new Vector2(-bx, by), new Vector2(-bx, height), new Vector2(-ax, height), new Vector2(-ax, ay));
                    var mid = new Vector3((ax + bx) / 2, (ay + by) / 2, zc);
                    var inward = new Vector3(cx - mid.x, spring - mid.y, 0).normalized;
                    float arc0 = r * t0, arc1 = r * t1;
                    AddQuadPoints(new Vector3(ax, ay, zf), new Vector3(ax, ay, zb), new Vector3(bx, by, zb), new Vector3(bx, by, zf), inward,
                        new Vector2(arc0, zf), new Vector2(arc0, zb), new Vector2(arc1, zb), new Vector2(arc1, zf));
                }
                Jamb(ox + opening, 0, spring, -1f);
                x += pier + opening;
            }
            Wall(x, x + pier, 0, height);
            if (x1 - (start + used) < 1e-3f) Jamb(x + pier, 0, height, 1f);
            return this;
        }

        public Mesh SaveAsset(string name, string folder, float lightmapScale = 1f)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTangents(tangents);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();

            UnwrapParam.SetDefaults(out var unwrap);
            float texelWidth = Mathf.Sqrt(surfaceArea) * LightmapTexelsPerMeter * lightmapScale;
            unwrap.packMargin = Mathf.Clamp(2.5f / Mathf.Max(texelWidth, 1f), 0.004f, 0.25f);
            Unwrapping.GenerateSecondaryUVSet(mesh, unwrap);

            var path = $"{folder}/{name}.asset";
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
    }
}
