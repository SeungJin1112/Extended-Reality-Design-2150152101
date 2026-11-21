using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Backrooms.EditorTools
{
    /// <summary>
    /// 백룸 구조물용 메시를 만든다.
    /// UV는 미터 단위로 계산되므로, 머티리얼 Tiling = 1 / (텍스처 한 장이 덮는 미터)로 두면
    /// 크기가 달라도 텍스처 밀도가 일정하게 유지된다.
    /// </summary>
    public class BackroomsMeshFactory
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Vector3> normals = new List<Vector3>();
        readonly List<Vector4> tangents = new List<Vector4>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<int> triangles = new List<int>();
        float surfaceArea;

        /// <summary>라이트맵 UV 여백 계산에 쓰는 기준 해상도(라이트맵 텍셀/미터). 라이팅 설정과 맞출 것.</summary>
        public const float LightmapTexelsPerMeter = 6f;

        /// <summary>축 정렬 박스를 추가한다. skipBottom/skipTop은 안 보이는 면을 생략할 때 사용.</summary>
        public BackroomsMeshFactory AddBox(Vector3 center, Vector3 size, bool skipBottom = false, bool skipTop = false)
        {
            var h = size * 0.5f;
            var min = center - h;
            var max = center + h;

            // 각 면: origin = 바깥에서 봤을 때 왼쪽 아래, uDir = 오른쪽, vDir = 위 (시계방향 = 앞면)
            // +X / -X
            AddQuad(new Vector3(max.x, min.y, min.z), Vector3.forward, Vector3.up, size.z, size.y, Vector3.right, new Vector2(min.z, min.y));
            AddQuad(new Vector3(min.x, min.y, max.z), Vector3.back, Vector3.up, size.z, size.y, Vector3.left, new Vector2(-max.z, min.y));
            // +Z / -Z
            AddQuad(new Vector3(max.x, min.y, max.z), Vector3.left, Vector3.up, size.x, size.y, Vector3.forward, new Vector2(-max.x, min.y));
            AddQuad(new Vector3(min.x, min.y, min.z), Vector3.right, Vector3.up, size.x, size.y, Vector3.back, new Vector2(min.x, min.y));
            // +Y / -Y
            if (!skipTop)
                AddQuad(new Vector3(min.x, max.y, min.z), Vector3.right, Vector3.forward, size.x, size.z, Vector3.up, new Vector2(min.x, min.z));
            if (!skipBottom)
                AddQuad(new Vector3(min.x, min.y, max.z), Vector3.right, Vector3.back, size.x, size.z, Vector3.down, new Vector2(min.x, -max.z));
            return this;
        }

        /// <summary>수평 사각형 한 장 (바닥은 위, 천장은 아래를 향함).</summary>
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

            // Unity: bitangent = cross(normal, tangent) * w. 이 배치에서는 cross(n, u) = -v 이므로 w = -1.
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

        /// <summary>메시를 만들고 라이트맵용 UV2를 생성한 뒤 에셋으로 저장한다.</summary>
        /// <param name="lightmapScale">렌더러의 Scale In Lightmap 값. UV 여백 계산에 반영된다.</param>
        public Mesh SaveAsset(string name, string folder, float lightmapScale = 1f)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTangents(tangents);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();

            // 기본 여백(packMargin)은 1024텍셀 기준이라, 라이트맵에서 작게 차지하는 메시는 UV가 겹친다.
            // 메시가 라이트맵에서 차지할 대략적인 텍셀 폭을 구해 약 2.5텍셀 여백이 되도록 맞춘다.
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
