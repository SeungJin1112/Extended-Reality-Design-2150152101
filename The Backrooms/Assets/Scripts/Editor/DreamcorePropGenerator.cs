using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Backrooms.EditorTools
{
    public static class DreamcorePropGenerator
    {
        static readonly (string name, Color color)[] Pastels =
        {
            ("Pink", new Color(1f, 0.72f, 0.82f)),
            ("BabyBlue", new Color(0.66f, 0.84f, 1f)),
            ("Mint", new Color(0.68f, 0.96f, 0.82f)),
            ("Lavender", new Color(0.8f, 0.72f, 1f)),
            ("Butter", new Color(1f, 0.94f, 0.62f)),
            ("Peach", new Color(1f, 0.79f, 0.64f)),
        };

        [MenuItem("Backrooms/5. Generate Dreamcore Props", priority = 21)]
        public static void Run()
        {
            GeneratePoolProps("Assets/Objects/PoolRooms");
            GenerateDoors("Assets/Objects/Common");
            AssetDatabase.SaveAssets();
            Debug.Log("[Backrooms] Dreamcore props generated");
        }

        static void GeneratePoolProps(string leaf)
        {
            var mesh = $"{leaf}/Meshes";
            var mats = $"{leaf}/Materials";
            var prefabs = $"{leaf}/Prefabs";
            foreach (var f in new[] { mesh, mats, prefabs }) QuestProjectSetup.EnsureFolder(f);

            var torus = SaveMesh(BuildTorus(0.32f, 0.12f, 32, 16), $"{mesh}/SwimRingMesh.asset");
            var sphere = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            var cylinder = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
            var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");

            foreach (var (name, color) in Pastels)
            {
                var plastic = Lit(mats, $"Plastic{name}Material", color, 0.75f);
                Prefab(prefabs, $"SwimRing{name}", root =>
                {
                    Part(root, "Ring", torus, plastic, Vector3.up * 0.12f, Quaternion.identity, Vector3.one);
                    Collider(root, new Vector3(0, 0.12f, 0), new Vector3(0.9f, 0.24f, 0.9f));
                });
                Prefab(prefabs, $"PoolNoodle{name}", root =>
                {
                    Part(root, "Noodle", cylinder, plastic, Vector3.up * 0.04f, Quaternion.Euler(0, 0, 90), new Vector3(0.08f, 0.8f, 0.08f));
                    Collider(root, new Vector3(0, 0.04f, 0), new Vector3(1.6f, 0.08f, 0.08f));
                });
            }

            var stripeSets = new[]
            {
                ("BeachBallClassic", new[] { Pastels[0].color, Color.white, Pastels[1].color, Color.white, Pastels[4].color, Color.white }),
                ("BeachBallDream", new[] { Pastels[3].color, Pastels[2].color, Pastels[5].color, Pastels[0].color, Pastels[1].color, Pastels[4].color }),
                ("BeachBallCloud", new[] { Color.white, Pastels[1].color, Color.white, Pastels[1].color, Color.white, Pastels[1].color }),
            };
            foreach (var (name, colors) in stripeSets)
            {
                var tex = StripeTexture($"{leaf}/Textures/{name}BaseColor.png", colors);
                var mat = Lit(mats, $"{name}Material", Color.white, 0.8f, tex);
                Prefab(prefabs, name, root =>
                {
                    Part(root, "Ball", sphere, mat, Vector3.up * 0.25f, Quaternion.identity, Vector3.one * 0.5f);
                    root.AddComponent<SphereCollider>().center = Vector3.up * 0.25f;
                    root.GetComponent<SphereCollider>().radius = 0.25f;
                });
            }

            var chrome = Lit(mats, "PoolChromeMaterial", new Color(0.86f, 0.88f, 0.9f), 0.9f, metallic: 1f);
            var whitePlastic = Lit(mats, "PoolWhitePlasticMaterial", new Color(0.97f, 0.97f, 0.97f), 0.6f);
            var tileBlue = Lit(mats, "PoolTileBlueMaterial", new Color(0.62f, 0.85f, 0.95f), 0.85f);

            Prefab(prefabs, "PoolLadder", root =>
            {
                foreach (var x in new[] { -0.25f, 0.25f })
                    Part(root, "Rail", cylinder, chrome, new Vector3(x, 0.6f, 0), Quaternion.identity, new Vector3(0.05f, 0.6f, 0.05f));
                for (int i = 0; i < 4; i++)
                    Part(root, "Step", cube, chrome, new Vector3(0, 0.15f + i * 0.28f, 0.03f), Quaternion.identity, new Vector3(0.5f, 0.03f, 0.1f));
                Collider(root, new Vector3(0, 0.6f, 0), new Vector3(0.6f, 1.2f, 0.12f));
            });
            Prefab(prefabs, "DivingBoard", root =>
            {
                Part(root, "Stand", cube, whitePlastic, new Vector3(0, 0.25f, -0.6f), Quaternion.identity, new Vector3(0.5f, 0.5f, 0.6f));
                Part(root, "Board", cube, Lit(mats, "DivingBoardMaterial", Pastels[1].color, 0.5f), new Vector3(0, 0.53f, 0.4f), Quaternion.identity, new Vector3(0.5f, 0.06f, 2.4f));
                Collider(root, new Vector3(0, 0.3f, 0.3f), new Vector3(0.5f, 0.6f, 2.8f));
            });
            Prefab(prefabs, "LaneRope", root =>
            {
                var a = Lit(mats, "LaneFloatAMaterial", Pastels[0].color, 0.7f);
                var b = Lit(mats, "LaneFloatBMaterial", Color.white, 0.7f);
                for (int i = 0; i < 20; i++)
                    Part(root, "Float", sphere, i % 2 == 0 ? a : b, new Vector3(0, 0.06f, i * 0.2f - 1.9f), Quaternion.identity, new Vector3(0.12f, 0.12f, 0.18f));
                Collider(root, new Vector3(0, 0.06f, 0), new Vector3(0.12f, 0.12f, 4f));
            });
            foreach (var (name, color) in new[] { Pastels[0], Pastels[1] })
                Prefab(prefabs, $"PoolFloatMat{name}", root =>
                {
                    Part(root, "Mat", cube, Lit(mats, $"FloatMat{name}Material", color, 0.7f), Vector3.up * 0.05f, Quaternion.identity, new Vector3(0.7f, 0.1f, 1.8f));
                    Collider(root, Vector3.up * 0.05f, new Vector3(0.7f, 0.1f, 1.8f));
                });
            Prefab(prefabs, "PoolEdgeTile", root =>
            {
                Part(root, "Edge", cube, tileBlue, new Vector3(0, 0.05f, 0), Quaternion.identity, new Vector3(1f, 0.1f, 0.3f));
                Collider(root, new Vector3(0, 0.05f, 0), new Vector3(1f, 0.1f, 0.3f));
            });

            var duck = AssetDatabase.LoadAssetAtPath<GameObject>($"{leaf}/Prefabs/RubberDuckToy.prefab");
            if (duck != null)
            {
                Variant(prefabs, "RubberDuckGiant", duck, go => go.transform.localScale = Vector3.one * 8f);
                Variant(prefabs, "RubberDuckFloating", duck, go => go.transform.GetChild(0).localRotation = Quaternion.Euler(6f, 25f, -4f));
                Prefab(prefabs, "RubberDuckFamily", root =>
                {
                    var offsets = new[] { (0f, 0f, 1.4f), (-0.25f, -0.3f, 1f), (0.22f, -0.32f, 1f), (-0.1f, -0.6f, 0.9f), (0.15f, -0.62f, 0.9f) };
                    for (int i = 0; i < offsets.Length; i++)
                    {
                        var d = (GameObject)PrefabUtility.InstantiatePrefab(duck, root.transform);
                        d.name = $"Duck{i + 1}";
                        d.transform.localPosition = new Vector3(offsets[i].Item1, 0, offsets[i].Item2);
                        d.transform.localScale = Vector3.one * offsets[i].Item3;
                        d.transform.localRotation = Quaternion.Euler(0, 180f + i * 7f, 0);
                    }
                });
                foreach (var (name, color) in new[] { Pastels[0], Pastels[1], Pastels[3] })
                {
                    var tint = Color.Lerp(Color.white, color, 0.85f);
                    Variant(prefabs, $"RubberDuck{name}", duck, go =>
                    {
                        foreach (var r in go.GetComponentsInChildren<Renderer>())
                        {
                            var src = r.sharedMaterial;
                            var m = AssetDatabase.LoadAssetAtPath<Material>($"{mats}/RubberDuck{name}Material.mat");
                            if (m == null)
                            {
                                m = new Material(src) { name = $"RubberDuck{name}Material" };
                                m.SetColor("_BaseColor", tint);
                                AssetDatabase.CreateAsset(m, $"{mats}/RubberDuck{name}Material.mat");
                            }
                            r.sharedMaterial = m;
                        }
                    });
                }
            }
            else
            {
                Debug.LogWarning("[Backrooms] RubberDuckToy 프리팹이 없어 오리 변형을 건너뜀 (먼저 4. Process Asset Library)");
            }
        }

        static void GenerateDoors(string leaf)
        {
            var mats = $"{leaf}/Materials";
            var prefabs = $"{leaf}/Prefabs";
            foreach (var f in new[] { mats, prefabs }) QuestProjectSetup.EnsureFolder(f);

            var frameMat = Lit(mats, "DoorFrameMaterial", new Color(0.93f, 0.9f, 0.82f), 0.35f);
            var handleMat = Lit(mats, "DoorHandleMaterial", new Color(0.85f, 0.75f, 0.45f), 0.85f, metallic: 1f);

            var variants = new List<(string name, Color color, float smooth, float metal, Vector2 size)>
            {
                ("DoorWood", new Color(0.62f, 0.45f, 0.3f), 0.35f, 0f, new Vector2(0.9f, 2.05f)),
                ("DoorWhite", new Color(0.96f, 0.96f, 0.94f), 0.4f, 0f, new Vector2(0.9f, 2.05f)),
                ("DoorMetal", new Color(0.62f, 0.64f, 0.66f), 0.6f, 0.8f, new Vector2(0.95f, 2.1f)),
                ("DoorOffice", new Color(0.86f, 0.8f, 0.6f), 0.3f, 0f, new Vector2(0.9f, 2.05f)),
                ("DoorTiny", Pastels[0].color, 0.5f, 0f, new Vector2(0.45f, 0.8f)),
                ("DoorGiant", Pastels[3].color, 0.5f, 0f, new Vector2(1.8f, 4.2f)),
            };
            foreach (var (name, color) in Pastels)
                variants.Add(($"Door{name}", color, 0.5f, 0f, new Vector2(0.9f, 2.05f)));

            var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            var sphere = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");

            foreach (var v in variants)
            {
                var panelMat = Lit(mats, $"{v.name}Material", v.color, v.smooth, metallic: v.metal);
                Prefab(prefabs, v.name, root => BuildDoor(root, v.size, panelMat, frameMat, handleMat, cube, sphere, window: false));
            }
            var glass = Lit(mats, "DoorGlassMaterial", new Color(0.8f, 0.92f, 1f, 0.35f), 0.95f, transparent: true);
            Prefab(prefabs, "DoorWithWindow", root =>
            {
                BuildDoor(root, new Vector2(0.9f, 2.05f), Lit(mats, "DoorWithWindowMaterial", Pastels[2].color, 0.5f), frameMat, handleMat, cube, sphere, window: true);
                var panel = root.transform.Find("Hinge/Panel");
                Part(panel.gameObject, "Window", cube, glass, new Vector3(0, 0.25f, 0), Quaternion.identity, new Vector3(0.6f, 0.3f, 1.05f), worldScaleParent: true);
            });
            Prefab(prefabs, "DoubleDoor", root =>
            {
                var mat = Lit(mats, "DoubleDoorMaterial", new Color(0.94f, 0.92f, 0.86f), 0.4f);
                var left = new GameObject("LeftDoor"); left.transform.SetParent(root.transform, false); left.transform.localPosition = new Vector3(-0.45f, 0, 0);
                BuildDoor(left, new Vector2(0.9f, 2.05f), mat, frameMat, handleMat, cube, sphere, window: false);
                var right = new GameObject("RightDoor"); right.transform.SetParent(root.transform, false); right.transform.localPosition = new Vector3(0.45f, 0, 0); right.transform.localScale = new Vector3(-1, 1, 1);
                BuildDoor(right, new Vector2(0.9f, 2.05f), mat, frameMat, handleMat, cube, sphere, window: false);
            });
            Prefab(prefabs, "DoorFrameOnly", root =>
            {
                BuildDoor(root, new Vector2(0.9f, 2.05f), frameMat, frameMat, handleMat, cube, sphere, window: false);
                Object.DestroyImmediate(root.transform.Find("Hinge").gameObject);
            });

            foreach (var (name, color) in new[] { ("Brass", new Color(0.85f, 0.7f, 0.35f)), ("Silver", new Color(0.8f, 0.82f, 0.85f)), ("Pink", Pastels[0].color) })
            {
                var mat = Lit(mats, $"Key{name}Material", color, 0.85f, metallic: name == "Pink" ? 0f : 1f);
                Prefab(prefabs, $"Key{name}", root =>
                {
                    Part(root, "Bow", Resources.GetBuiltinResource<Mesh>("Cylinder.fbx"), mat, new Vector3(-0.045f, 0, 0), Quaternion.Euler(90, 0, 0), new Vector3(0.05f, 0.004f, 0.05f));
                    Part(root, "Shaft", cube, mat, new Vector3(0.02f, 0, 0), Quaternion.identity, new Vector3(0.08f, 0.008f, 0.008f));
                    Part(root, "Bit", cube, mat, new Vector3(0.05f, -0.008f, 0), Quaternion.identity, new Vector3(0.015f, 0.016f, 0.008f));
                    Collider(root, Vector3.zero, new Vector3(0.14f, 0.05f, 0.02f));
                });
            }
        }

        static void BuildDoor(GameObject root, Vector2 size, Material panelMat, Material frameMat, Material handleMat, Mesh cube, Mesh sphere, bool window)
        {
            float w = size.x, h = size.y, t = 0.05f, frame = 0.08f;
            Part(root, "FrameLeft", cube, frameMat, new Vector3(-w / 2 - frame / 2, h / 2, 0), Quaternion.identity, new Vector3(frame, h, 0.14f));
            Part(root, "FrameRight", cube, frameMat, new Vector3(w / 2 + frame / 2, h / 2, 0), Quaternion.identity, new Vector3(frame, h, 0.14f));
            Part(root, "FrameTop", cube, frameMat, new Vector3(0, h + frame / 2, 0), Quaternion.identity, new Vector3(w + frame * 2, frame, 0.14f));

            var hinge = new GameObject("Hinge");
            hinge.transform.SetParent(root.transform, false);
            hinge.transform.localPosition = new Vector3(-w / 2, 0, 0);
            var panel = Part(hinge, "Panel", cube, panelMat, new Vector3(w / 2, h / 2, 0), Quaternion.identity, new Vector3(w - 0.01f, h - 0.01f, t));
            var panelCollider = panel.AddComponent<BoxCollider>();
            panelCollider.size = Vector3.one;

            float hy = Mathf.Min(1.0f, h * 0.48f);
            foreach (var z in new[] { 0.045f, -0.045f })
            {
                var handle = Part(hinge, z > 0 ? "HandleFront" : "HandleBack", sphere, handleMat, new Vector3(w - 0.1f, hy, z), Quaternion.identity, Vector3.one * Mathf.Clamp(w * 0.07f, 0.035f, 0.09f));
                handle.AddComponent<SphereCollider>().radius = 0.9f;
            }
            if (root.GetComponent<Backrooms.DoorController>() == null)
                root.AddComponent<Backrooms.DoorController>().hinge = hinge.transform;
        }

        static Material Lit(string dir, string name, Color color, float smoothness, Texture2D tex = null, float metallic = 0f, bool transparent = false)
        {
            var path = $"{dir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            if (tex != null) mat.SetTexture("_BaseMap", tex);
            if (transparent)
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetFloat("_ZWrite", 0f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            mat.enableInstancing = true;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static Texture2D StripeTexture(string path, Color[] colors)
        {
            if (!File.Exists(path))
            {
                QuestProjectSetup.EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
                const int w = 256, h = 64;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                for (int x = 0; x < w; x++)
                {
                    var c = colors[x * colors.Length / w];
                    for (int y = 0; y < h; y++) tex.SetPixel(x, y, c);
                }
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Mesh SaveMesh(Mesh mesh, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static Mesh BuildTorus(float radius, float tube, int segments, int sides)
        {
            var verts = new List<Vector3>(); var normals = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
            for (int i = 0; i <= segments; i++)
            {
                float u = (float)i / segments * Mathf.PI * 2f;
                var center = new Vector3(Mathf.Cos(u), 0, Mathf.Sin(u)) * radius;
                for (int j = 0; j <= sides; j++)
                {
                    float v = (float)j / sides * Mathf.PI * 2f;
                    var n = new Vector3(Mathf.Cos(u) * Mathf.Cos(v), Mathf.Sin(v), Mathf.Sin(u) * Mathf.Cos(v));
                    verts.Add(center + n * tube); normals.Add(n); uvs.Add(new Vector2((float)i / segments, (float)j / sides));
                }
            }
            for (int i = 0; i < segments; i++)
            for (int j = 0; j < sides; j++)
            {
                int a = i * (sides + 1) + j, b = a + sides + 1;
                tris.AddRange(new[] { a, a + 1, b, b, a + 1, b + 1 });
            }
            var mesh = new Mesh { name = "SwimRingMesh" };
            mesh.SetVertices(verts); mesh.SetNormals(normals); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
            mesh.RecalculateTangents(); mesh.RecalculateBounds();
            return mesh;
        }

        static GameObject Part(GameObject parent, string name, Mesh mesh, Material mat, Vector3 pos, Quaternion rot, Vector3 scale, bool worldScaleParent = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            if (worldScaleParent)
            {
                go.transform.localPosition = new Vector3(0, pos.y / parent.transform.localScale.y, 0);
            }
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        static void Collider(GameObject root, Vector3 center, Vector3 size)
        {
            var c = root.AddComponent<BoxCollider>();
            c.center = center; c.size = size;
        }

        static void Prefab(string dir, string name, System.Action<GameObject> build)
        {
            var path = $"{dir}/{name}.prefab";
            if (File.Exists(path)) return;
            var root = new GameObject(name);
            build(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        static void Variant(string dir, string name, GameObject basePrefab, System.Action<GameObject> modify)
        {
            var path = $"{dir}/{name}.prefab";
            if (File.Exists(path)) return;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            instance.name = name;
            modify(instance);
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
        }
    }
}
