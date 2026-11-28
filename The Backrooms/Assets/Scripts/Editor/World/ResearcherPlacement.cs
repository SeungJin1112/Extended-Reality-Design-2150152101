using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Backrooms.EditorTools.World
{
    // 연구 기지(Zone00)의 Level 0 균열 앞에 방호복 연구원을 배치한다.
    // "2. Build World"로 다시 지어도 유지되고, 메뉴로 현재 씬에만 다시 배치할 수도 있다.
    public static class ResearcherPlacement
    {
        const string PrefabName = "ProtectiveSuit"; // Assets/Objects/ResearchBase/Prefabs/ProtectiveSuit.prefab
        const string ZoneName = "Zone00ResearchBase";
        const string GroupName = "Researchers";
        const float TargetHeight = 1.8f;

        enum Stance { Stand, Record, Crouch }

        // Mixamo 애니메이션 (Models/ProtectiveSuit/Animations/Researcher{Key}.fbx). 없으면 Stance 코드 자세로 대신한다.
        const string AnimDir = "Assets/Objects/ResearchBase/Models/ProtectiveSuit/Animations";
        const string ControllerDir = AnimDir + "/Controllers";

        // 구역 로컬 좌표. 메인 복도 x 12~15, 균열 곁복도 z 26~29, 균열 x=25 z=27.5.
        // 플레이어가 지나가는 길(복도 가운데)은 비워 둔다.
        static readonly (string name, float x, float z, float rotY, string anim, float offset, Stance fallback)[] Spots =
        {
            // 메인 복도: 혼자 서 있거나 둘러보는 연구원
            ("Researcher_Guard",     12.6f, 24.8f, 150f, "Idle",        0.10f, Stance.Stand),  // 곁복도 입구에서 들어오는 플레이어 쪽을 봄
            ("Researcher_Observer",  12.55f, 14.8f, 100f, "LookAround", 0.35f, Stance.Stand),  // 보안실 문 옆에서 두리번
            ("Researcher_Wanderer",  14.35f, 9.2f, 250f, "LookAround2",  0.60f, Stance.Stand),  // 창고 문 앞에서 두리번
            // 균열 곁복도: 둘이 마주 보고 이야기 (한 명은 말하고, 한 명은 고개를 저음)
            ("Researcher_Talker",    17.3f, 28.35f, 90f, "Arguing",     0.00f, Stance.Record),
            ("Researcher_Listener",  18.45f, 28.35f, 270f, "ShakeHead", 0.25f, Stance.Stand),
            // 균열 바로 앞: 혼자 떨어져 바닥 잔해를 붙잡고 있다가, 일어나 주변을 둘러보고 다시 앉기를 반복
            ("Researcher_Inspector", 23.95f, 28.55f, 185f, "Inspect", 0.00f, Stance.Crouch),
        };

        static readonly string[] AnimKeys = { "Idle", "LookAround", "LookAround2", "Arguing", "ShakeHead", "HoldingDown" };
        static readonly string[] LowAnims = { "HoldingDown", "Inspect" };

        // 두 동작을 번갈아 재생하는 조합. (첫 동작, 반복 횟수, 다음 동작, 반복 횟수)
        static readonly System.Collections.Generic.Dictionary<string, (string a, float loopsA, string b, float loopsB)> Combos =
            new System.Collections.Generic.Dictionary<string, (string, float, string, float)>
            {
                { "Inspect", ("HoldingDown", 1f, "LookAround", 1f) }, // 약 13초 앉아 있다가 6초 일어나 둘러봄
            };

        public static void Place(Transform zoneRoot, Transform parent)
        {
            ZoneKit.ResetIndex();
            var path = ZoneKit.FindPrefab(PrefabName);
            var prefab = path != null ? AssetDatabase.LoadAssetAtPath<GameObject>(path) : null;
            if (prefab == null) { Debug.LogWarning($"[Backrooms] 연구원 프리팹 없음: {PrefabName}"); return; }
            var controllers = SetupAnimations();

            var old = parent.Find(GroupName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var group = new GameObject(GroupName).transform;
            group.SetParent(parent, false);

            int animated = 0;
            foreach (var s in Spots)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
                go.name = s.name;
                go.transform.localPosition = new Vector3(s.x, 0f, s.z);
                go.transform.localRotation = Quaternion.Euler(0f, s.rotY, 0f);

                // 키는 기본 자세 기준으로 맞춘다. (스킨 메시 경계는 뼈를 움직여도 갱신되지 않음)
                var b = ZoneKit.Bounds(go);
                if (b.size.y > 0.01f) go.transform.localScale *= TargetHeight / b.size.y;
                b = ZoneKit.Bounds(go);
                float soleOffset = LowestBone(go) - b.min.y;
                float floor = zoneRoot.position.y;

                bool low;
                var anim = go.GetComponentInChildren<Animator>();
                if (anim != null && controllers.TryGetValue(s.anim, out var entry))
                {
                    // 애니메이션: 실행 중에는 Animator가 재생하고, 에디터에서는 해당 프레임 자세를 미리 찍어 둔다.
                    anim.enabled = true;
                    anim.runtimeAnimatorController = entry.controller;
                    anim.applyRootMotion = false;
                    anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                    entry.clip.SampleAnimation(anim.gameObject, entry.clip.length * s.offset);
                    var ra = go.AddComponent<ResearcherAnimation>();
                    ra.stateName = entry.state;
                    ra.startOffset = s.offset;
                    low = LowAnims.Contains(s.anim);
                    animated++;
                }
                else
                {
                    Pose(go, s.fallback);
                    low = s.fallback == Stance.Crouch;
                }

                // 자세를 잡은 뒤 가장 낮은 뼈(발끝) 기준으로 바닥에 붙인다.
                go.transform.position += Vector3.up * (floor - (LowestBone(go) - soleOffset));

                var col = go.AddComponent<CapsuleCollider>();
                float sc = go.transform.lossyScale.y;
                float h = low ? 1.15f : TargetHeight;
                col.height = h / sc;
                col.radius = (low ? 0.4f : 0.28f) / sc;
                col.center = new Vector3(0f, h / 2f / sc, 0f);
            }
            Debug.Log($"[Backrooms] 연구원 {Spots.Length}명 배치 완료 (애니메이션 {animated}명)");
        }

        // Mixamo FBX를 Humanoid로 가져오고, 동작마다 한 상태짜리 Animator Controller를 만든다.
        // 이미 설정된 파일은 건너뛰므로 여러 번 불러도 된다.
        static System.Collections.Generic.Dictionary<string, (RuntimeAnimatorController controller, AnimationClip clip, string state)> SetupAnimations()
        {
            var result = new System.Collections.Generic.Dictionary<string, (RuntimeAnimatorController controller, AnimationClip clip, string state)>();
            foreach (var key in AnimKeys)
            {
                var fbx = $"{AnimDir}/Researcher{key}.fbx";
                if (!(AssetImporter.GetAtPath(fbx) is ModelImporter imp)) continue;

                string clipName = $"Researcher{key}";
                bool ready = imp.animationType == ModelImporterAnimationType.Human && imp.userData == "BackroomsPrepared"
                             && imp.clipAnimations.Length > 0 && imp.clipAnimations[0].name == clipName;
                if (!ready)
                {
                    imp.animationType = ModelImporterAnimationType.Human;
                    imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    imp.importAnimation = true;
                    imp.importCameras = false;
                    imp.importLights = false;
                    imp.materialImportMode = ModelImporterMaterialImportMode.None;
                    imp.userData = "BackroomsPrepared"; // AssetLibraryProcessor가 리그를 지우지 않도록
                    imp.SaveAndReimport();

                    var clips = imp.defaultClipAnimations;
                    if (clips.Length == 0) { Debug.LogWarning($"[Backrooms] {fbx}: 애니메이션 클립 없음"); continue; }
                    var c = clips[0];
                    c.name = clipName;
                    c.loopTime = true;
                    c.lockRootRotation = true; c.keepOriginalOrientation = true;
                    c.lockRootHeightY = true; c.heightFromFeet = true; c.keepOriginalPositionY = false;
                    c.lockRootPositionXZ = true; c.keepOriginalPositionXZ = true;
                    imp.clipAnimations = new[] { c };
                    imp.SaveAndReimport();
                }

                var clip = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().FirstOrDefault(a => a.name == clipName);
                if (clip == null) continue;

                QuestProjectSetup.EnsureFolder(ControllerDir);
                var ctrlPath = $"{ControllerDir}/{clipName}.controller";
                var ctrl = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(ctrlPath);
                if (ctrl == null) ctrl = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPathWithClip(ctrlPath, clip);
                var state = ctrl.layers[0].stateMachine.defaultState;
                if (state.motion != clip) { state.motion = clip; EditorUtility.SetDirty(ctrl); }
                result[key] = (ctrl, clip, state.name);
            }

            // 조합 컨트롤러: A를 몇 번 반복한 뒤 B로 부드럽게 넘어가고, B가 끝나면 다시 A로.
            foreach (var combo in Combos)
            {
                if (!result.TryGetValue(combo.Value.a, out var A) || !result.TryGetValue(combo.Value.b, out var B)) continue;
                var ctrlPath = $"{ControllerDir}/Researcher{combo.Key}.controller";
                AssetDatabase.DeleteAsset(ctrlPath);
                var ctrl = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                var sm = ctrl.layers[0].stateMachine;
                var sa = sm.AddState(combo.Value.a); sa.motion = A.clip;
                var sb = sm.AddState(combo.Value.b); sb.motion = B.clip;
                sm.defaultState = sa;
                var toB = sa.AddTransition(sb);
                toB.hasExitTime = true; toB.exitTime = combo.Value.loopsA; toB.hasFixedDuration = true; toB.duration = 0.8f;
                var toA = sb.AddTransition(sa);
                toA.hasExitTime = true; toA.exitTime = combo.Value.loopsB; toA.hasFixedDuration = true; toA.duration = 0.8f;
                EditorUtility.SetDirty(ctrl);
                result[combo.Key] = (ctrl, A.clip, sa.name);
            }
            AssetDatabase.SaveAssets();
            return result;
        }

        static float LowestBone(GameObject go)
        {
            var smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
            var bones = smr != null && smr.bones.Length > 0 ? smr.bones.Where(t => t != null) : go.GetComponentsInChildren<Transform>();
            return bones.Min(t => t.position.y);
        }

        // 애니메이션이 없는 리그라 T포즈로 서 있으므로 뼈 방향을 직접 잡아 자세를 만든다.
        static void Pose(GameObject go, Stance stance)
        {
            var anim = go.GetComponentInChildren<Animator>();
            if (anim == null || !anim.isHuman) return;

            void Aim(HumanBodyBones bone, HumanBodyBones child, Vector3 worldDir)
            {
                var a = anim.GetBoneTransform(bone);
                var c = anim.GetBoneTransform(child);
                if (a == null || c == null) return;
                var cur = c.position - a.position;
                if (cur.sqrMagnitude < 1e-6f) return;
                a.rotation = Quaternion.FromToRotation(cur, worldDir.normalized) * a.rotation;
            }

            var t = go.transform;
            Vector3 up = t.up, down = -t.up, fwd = t.forward, right = t.right;

            switch (stance)
            {
                case Stance.Stand:
                    Aim(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, down - right * 0.18f + fwd * 0.05f);
                    Aim(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, down + fwd * 0.25f - right * 0.05f);
                    Aim(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, down + right * 0.18f + fwd * 0.05f);
                    Aim(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, down + fwd * 0.25f + right * 0.05f);
                    break;

                case Stance.Record:
                    Aim(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, down - right * 0.18f + fwd * 0.05f);
                    Aim(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, down + fwd * 0.25f - right * 0.05f);
                    Aim(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, down + right * 0.12f + fwd * 0.35f);
                    Aim(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, fwd + down * 0.15f - right * 0.35f);
                    break;

                case Stance.Crouch:
                    // 다리: 허벅지는 앞으로, 정강이는 아래로 접고, 발은 바닥과 평행하게
                    foreach (var side in new[] { -1f, 1f })
                    {
                        bool left = side < 0;
                        // 왼쪽 무릎은 세우고 오른쪽 무릎은 더 낮춰 짝다리로 깊게 앉힌다
                        float drop = left ? 0.12f : 0.3f;
                        Aim(left ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg, left ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg, fwd + down * drop + right * side * 0.3f);
                        Aim(left ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg, left ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot, down - fwd * 0.55f + right * side * 0.05f);
                        Aim(left ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot, left ? HumanBodyBones.LeftToes : HumanBodyBones.RightToes, fwd + down * 0.45f + right * side * 0.1f);
                    }
                    // 상체를 크게 숙이고 고개는 발 앞 바닥을 내려다본다
                    Aim(HumanBodyBones.Spine, HumanBodyBones.Neck, up + fwd * 1.1f);
                    Aim(HumanBodyBones.Neck, HumanBodyBones.Head, up * 0.6f + fwd);
                    // 왼팔은 팔뚝을 왼무릎 위에 걸치고, 오른손은 무릎 사이 바닥으로 내린다
                    Aim(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, down + fwd * 0.25f - right * 0.1f);
                    Aim(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, fwd + right * 0.35f + down * 0.15f);
                    Aim(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, down + fwd * 0.35f - right * 0.05f);
                    Aim(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, down + fwd * 0.15f - right * 0.15f);
                    break;
            }
            // 컨트롤러가 없는 Animator가 실행 중 자세를 되돌리지 않도록 끈다.
            anim.enabled = false;
        }

        // 연구원 주변을 여러 각도에서 찍어 Temp/ResearcherPreviews에 저장한다. (EditorJobBridge로 호출)
        static void CaptureResearchers()
        {
            var root = SceneManager.GetSceneByPath(BackroomsWorldBuilder.ZoneScenePath(ZoneName)).GetRootGameObjects().FirstOrDefault(g => g.name == ZoneName);
            if (root == null) { Debug.LogWarning("[Backrooms] Zone00 씬이 열려 있지 않아요."); return; }
            var o = root.transform.position;
            var atmo = root.GetComponent<ZoneAtmosphere>();
            if (atmo != null)
            {
                RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = atmo.fogColor; RenderSettings.fogStartDistance = atmo.fogStart; RenderSettings.fogEndDistance = atmo.fogEnd;
            }
            var dir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath) ?? "", "Temp", "ResearcherPreviews");
            System.IO.Directory.CreateDirectory(dir);
            var camGo = new GameObject("ResearcherPreviewCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 70f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 80f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = atmo != null ? atmo.backgroundColor : Color.black;
            UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(cam).renderPostProcessing = true;
            var rt = new RenderTexture(1280, 720, 24) { antiAliasing = 4 };
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var views = new (string name, Vector3 pos, Vector3 look)[]
            {
                ("MainCorridor", new Vector3(13.5f, 1.6f, 4.5f), new Vector3(13.4f, 1.1f, 16f)),
                ("Guard", new Vector3(14.2f, 1.6f, 21.5f), new Vector3(12.6f, 1.0f, 24.8f)),
                ("Pair", new Vector3(17.85f, 1.5f, 26.3f), new Vector3(17.85f, 1.1f, 28.35f)),
                ("SideCorridor", new Vector3(15.6f, 1.6f, 27.3f), new Vector3(24.5f, 0.9f, 27.6f)),
                ("Inspector", new Vector3(22.3f, 1.3f, 27.2f), new Vector3(23.95f, 0.5f, 28.4f)),
                ("InspectorFront", new Vector3(23.6f, 1.0f, 26.9f), new Vector3(23.95f, 0.45f, 28.5f)),
            };
            foreach (var v in views)
            {
                cam.transform.position = o + v.pos;
                cam.transform.rotation = Quaternion.LookRotation(v.look - v.pos);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                tex.Apply();
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, v.name + ".png"), tex.EncodeToPNG());
                cam.targetTexture = null;
            }
            RenderTexture.active = null;
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(rt);
            Debug.Log($"[Backrooms] 연구원 미리보기 저장: {dir}");
        }

        [MenuItem("Backrooms/6. Place Researchers", priority = 22)]
        static void PlaceFromMenu()
        {
            if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Backrooms", "Play 모드를 끈 다음 다시 실행해 주세요.", "확인"); return; }
            var path = BackroomsWorldBuilder.ZoneScenePath(ZoneName);
            var scene = SceneManager.GetSceneByPath(path);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                if (!System.IO.File.Exists(path)) { EditorUtility.DisplayDialog("Backrooms", $"{path} 씬이 없어요. 먼저 2. Build World를 실행해 주세요.", "확인"); return; }
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }
            var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == ZoneName);
            if (root == null) { Debug.LogWarning($"[Backrooms] {ZoneName} 루트를 찾지 못했어요."); return; }
            var props = root.transform.Find("Props");
            if (props == null) props = root.transform;

            Place(root.transform, props);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            var group = props.Find(GroupName);
            if (group != null) { Selection.activeGameObject = group.gameObject; SceneView.FrameLastActiveSceneView(); }
        }
    }
}
