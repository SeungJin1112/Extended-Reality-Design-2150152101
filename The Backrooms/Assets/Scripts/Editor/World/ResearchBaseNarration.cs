using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Line = Backrooms.NarrationSubtitles.Line;

namespace Backrooms.EditorTools.World
{
    // 연구 거점(Zone00)의 나레이션 자막을 배치한다.
    // 1) 게임 시작: 관측팀이 무전으로 상황을 설명하고 균열 쪽으로 부른다.
    // 2) 균열 앞 곁복도에 들어서면: 연구원이 Level 0 진입을 안내한다.
    // 실제 대사는 씬의 Narration 오브젝트 Inspector에서 고칠 수 있지만, 월드를 다시 지으면 아래 내용으로 돌아간다.
    public static class ResearchBaseNarration
    {
        const string ZoneName = "Zone00ResearchBase";
        const string GroupName = "Narration";

        static Line L(string speaker, string text) => new Line { speaker = speaker, text = text };

        static Line[] Intro() => new[]
        {
            L("관측팀 · 무전", "들립니까? 여기는 SITE-0 관측팀. 거점 진입 확인했습니다."),
            L("관측팀 · 무전", "지금 계신 곳은 B3 기계실, 저희 연구 거점입니다. 안전선 안쪽은 안전하니 천천히 오세요."),
            L("관측팀 · 무전", "사흘 전, 이 층 벽 너머에서 있을 수 없는 공간이 발견됐습니다. 저희는 그곳을 '백룸'이라고 부릅니다."),
            L("관측팀 · 무전", "복도 끝에서 오른쪽, 벽이 무너진 곳으로 와 주세요. 저희가 거기서 기다리고 있습니다."),
        };

        static Line[] Breach() => new[]
        {
            L("연구원", "오셨군요. 이 균열 너머가 Level 0입니다."),
            L("연구원", "노란 벽지, 젖은 카펫, 끝없이 울리는 형광등 소리. 안으로 들어가면 방향 감각부터 흐려질 겁니다."),
            L("연구원", "이번 임무는 안쪽 첫 번째 문까지 확인하고 돌아오는 것뿐입니다. 무전은 계속 열어 두겠습니다."),
            L("연구원", "…혹시 공간이 바뀌는 것 같으면, 멈추지 말고 계속 걸으세요."),
        };

        public static void Place(Transform zoneRoot)
        {
            var old = zoneRoot.Find(GroupName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var group = new GameObject(GroupName).transform;
            group.SetParent(zoneRoot, false);

            var intro = new GameObject("IntroRadio").AddComponent<NarrationSequence>();
            intro.transform.SetParent(group, false);
            intro.startMode = NarrationSequence.StartMode.OnGameStart;
            intro.startDelay = 2.5f;
            intro.lines = Intro();

            var breach = new GameObject("BreachBriefing").AddComponent<NarrationSequence>();
            breach.transform.SetParent(group, false);
            breach.transform.localPosition = new Vector3(19f, 1.4f, 27.5f);
            var box = breach.gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(4f, 2.8f, 3f);
            breach.startMode = NarrationSequence.StartMode.OnPlayerEnter;
            breach.after = intro;
            breach.lines = Breach();

            Debug.Log("[Backrooms] 연구 거점 나레이션 배치 완료");
        }

        // 자막이 실제로 어떻게 보이는지 Play 없이 찍어 Temp/NarrationPreviews에 저장한다. (EditorJobBridge로 호출)
        static void CapturePreview()
        {
            var scene = SceneManager.GetSceneByPath(BackroomsWorldBuilder.ZoneScenePath(ZoneName));
            var root = scene.IsValid() ? scene.GetRootGameObjects().FirstOrDefault(g => g.name == ZoneName) : null;
            if (root == null) { Debug.LogWarning("[Backrooms] Zone00 씬이 열려 있지 않아요."); return; }
            var o = root.transform.position;
            var atmo = root.GetComponent<ZoneAtmosphere>();
            if (atmo != null)
            {
                RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = atmo.fogColor; RenderSettings.fogStartDistance = atmo.fogStart; RenderSettings.fogEndDistance = atmo.fogEnd;
            }
            var dir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath) ?? "", "Temp", "NarrationPreviews");
            System.IO.Directory.CreateDirectory(dir);

            var camGo = new GameObject("NarrationPreviewCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 90f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 80f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = atmo != null ? atmo.backgroundColor : Color.black;
            UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(cam).renderPostProcessing = true;

            var subGo = new GameObject("PreviewSubtitles");
            var sub = subGo.AddComponent<NarrationSubtitles>();
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            typeof(NarrationSubtitles).GetMethod("Build", flags).Invoke(sub, null);
            var speaker = (UnityEngine.UI.Text)typeof(NarrationSubtitles).GetField("speakerText", flags).GetValue(sub);
            var body = (UnityEngine.UI.Text)typeof(NarrationSubtitles).GetField("bodyText", flags).GetValue(sub);
            Canvas.ForceUpdateCanvases();

            var rt = new RenderTexture(1280, 720, 24) { antiAliasing = 4 };
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var shots = new (string name, Vector3 eye, float yaw, Line line)[]
            {
                ("Intro", new Vector3(13.5f, 1.6f, 1.5f), 0f, Intro()[2]),
                ("Breach", new Vector3(18.5f, 1.6f, 27.4f), 90f, Breach()[1]),
            };
            foreach (var s in shots)
            {
                var fwd = Quaternion.Euler(0f, s.yaw, 0f) * Vector3.forward;
                cam.transform.SetPositionAndRotation(o + s.eye, Quaternion.LookRotation(fwd));
                subGo.transform.SetPositionAndRotation(o + s.eye + fwd * sub.distance + Vector3.up * sub.heightOffset, Quaternion.LookRotation(fwd));
                speaker.text = s.line.speaker;
                body.text = s.line.text;
                Canvas.ForceUpdateCanvases();
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                tex.Apply();
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, s.name + ".png"), tex.EncodeToPNG());
                cam.targetTexture = null;
            }
            RenderTexture.active = null;
            Object.DestroyImmediate(subGo);
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(rt);
            Debug.Log($"[Backrooms] 자막 미리보기 저장: {dir}");
        }

        // EditorJobBridge / 메뉴에서 현재 열린 Zone00 씬에 바로 적용
        [MenuItem("Backrooms/6b. Place Research Base Narration", priority = 22)]
        static void PlaceInScene()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[Backrooms] Play 모드를 끈 다음 실행해 주세요."); return; }
            var path = BackroomsWorldBuilder.ZoneScenePath(ZoneName);
            var scene = SceneManager.GetSceneByPath(path);
            if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == ZoneName);
            if (root == null) { Debug.LogWarning($"[Backrooms] {ZoneName} 루트를 찾지 못했어요."); return; }
            Place(root.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
