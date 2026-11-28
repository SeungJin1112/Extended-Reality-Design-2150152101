using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Backrooms
{
    // VR용 나레이션 자막. 시선 앞 1.6m에 떠 있는 자막 패널을 만들고 머리를 천천히 따라온다.
    // 대사는 NarrationSequence(게임 시작 / 트리거)가 Play()로 넘긴다.
    public class NarrationSubtitles : MonoBehaviour
    {
        [Serializable]
        public class Line
        {
            public string speaker = "연구원";
            [TextArea(2, 4)] public string text;
            [Tooltip("0이면 글자 수로 자동 계산")] public float duration;
        }

        static NarrationSubtitles instance;

        public float distance = 1.6f;
        public float heightOffset = -0.42f;
        public float followSpeed = 2.5f;
        public float fade = 0.35f;

        Transform head;
        CanvasGroup group;
        Text speakerText, bodyText;
        readonly Queue<(Line[] lines, Action done)> pending = new Queue<(Line[], Action)>();
        bool playing;

        public static NarrationSubtitles Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<NarrationSubtitles>();
                    if (instance == null) instance = new GameObject("NarrationSubtitles").AddComponent<NarrationSubtitles>();
                }
                return instance;
            }
        }

        public static void Play(Line[] lines, Action done = null) => Instance.Enqueue(lines, done);

        void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            Build();
            group.alpha = 0f;
        }

        void Enqueue(Line[] lines, Action done)
        {
            if (lines == null || lines.Length == 0) return;
            pending.Enqueue((lines, done));
            if (!playing) StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            playing = true;
            while (pending.Count > 0)
            {
                var (lines, done) = pending.Dequeue();
                foreach (var line in lines)
                {
                    speakerText.text = string.IsNullOrEmpty(line.speaker) ? "" : line.speaker;
                    bodyText.text = line.text;
                    SnapToView();
                    yield return Fade(1f);
                    float hold = line.duration > 0f ? line.duration : Mathf.Clamp(1.6f + line.text.Length * 0.085f, 2.5f, 8f);
                    yield return new WaitForSeconds(hold);
                    yield return Fade(0f);
                    yield return new WaitForSeconds(0.25f);
                }
                done?.Invoke();
            }
            playing = false;
        }

        IEnumerator Fade(float to)
        {
            float from = group.alpha;
            for (float t = 0f; t < fade; t += Time.deltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, t / fade);
                yield return null;
            }
            group.alpha = to;
        }

        void LateUpdate()
        {
            if (!FindHead() || group.alpha <= 0f) return;
            var target = TargetPose();
            float k = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, target.position, k);
            transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, k);
        }

        void SnapToView()
        {
            if (!FindHead()) return;
            var p = TargetPose();
            transform.SetPositionAndRotation(p.position, p.rotation);
        }

        Pose TargetPose()
        {
            var fwd = head.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f) fwd = head.up;
            fwd.Normalize();
            // 벽에 가까이 서 있으면 자막이 벽 뒤로 묻히지 않게 당겨 온다.
            float d = distance;
            if (Physics.Raycast(head.position, fwd, out var hit, distance + 0.1f, ~0, QueryTriggerInteraction.Ignore))
                d = Mathf.Max(0.45f, hit.distance - 0.12f);
            var pos = head.position + fwd * d + Vector3.up * heightOffset * (d / distance);
            return new Pose(pos, Quaternion.LookRotation(fwd));
        }

        bool FindHead()
        {
            if (head != null && head.gameObject.activeInHierarchy) return true;
            var cam = Camera.main;
            head = cam != null ? cam.transform : null;
            return head != null;
        }

        void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;
            gameObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 1f;
            group = gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            // 큰 글자 크기 × 작은 스케일로 그려야 VR에서 글자가 선명하다. (패널 1.5m × 0.24m, 대사 두 줄까지)
            var rt = (RectTransform)transform;
            rt.sizeDelta = new Vector2(3000f, 480f);
            rt.localScale = Vector3.one * 0.0005f;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(transform, false);
            var bgRt = (RectTransform)bg.transform;
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0.03f, 0.035f, 0.04f, 0.72f);

            var bar = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(transform, false);
            var barRt = (RectTransform)bar.transform;
            barRt.anchorMin = new Vector2(0f, 0f); barRt.anchorMax = new Vector2(0f, 1f);
            barRt.sizeDelta = new Vector2(20f, 0f); barRt.anchoredPosition = new Vector2(10f, 0f);
            bar.GetComponent<Image>().color = new Color(0.84f, 0.78f, 0.42f, 1f);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            speakerText = MakeText("Speaker", font, 76, FontStyle.Bold, new Color(0.84f, 0.78f, 0.42f), new Vector2(100f, -45f), new Vector2(-100f, 100f), TextAnchor.UpperLeft);
            bodyText = MakeText("Body", font, 112, FontStyle.Normal, new Color(0.95f, 0.95f, 0.93f), new Vector2(100f, -165f), new Vector2(-100f, 280f), TextAnchor.UpperLeft);
        }

        Text MakeText(string name, Font font, int size, FontStyle style, Color color, Vector2 topLeft, Vector2 rightAndHeight, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0f, 1f);
            rt.offsetMin = new Vector2(topLeft.x, topLeft.y - rightAndHeight.y);
            rt.offsetMax = new Vector2(rightAndHeight.x, topLeft.y);
            var t = go.GetComponent<Text>();
            t.font = font; t.fontSize = size; t.fontStyle = style; t.color = color; t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.lineSpacing = 1.1f;
            t.raycastTarget = false;
            return t;
        }
    }
}
