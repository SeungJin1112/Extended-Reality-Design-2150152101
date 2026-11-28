using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace Backrooms
{
    // 대사 묶음. 게임 시작 시 재생하거나, 플레이어가 트리거 영역에 들어오면 한 번 재생한다.
    // 대사는 Inspector에서 바로 고칠 수 있다.
    public class NarrationSequence : MonoBehaviour
    {
        public enum StartMode { OnGameStart, OnPlayerEnter }

        public StartMode startMode = StartMode.OnGameStart;
        [Tooltip("게임 시작 후 첫 대사까지 기다리는 시간(초)")] public float startDelay = 2f;
        [Tooltip("이 대사가 먼저 끝나야 재생 (비우면 조건 없음)")] public NarrationSequence after;
        public NarrationSubtitles.Line[] lines;

        public bool Finished { get; private set; }
        bool started;

        // 한 번 재생한 대사 묶음은 같은 실행 중에 다시 나오지 않는다.
        // (같은 구역 씬이 두 번 로드되거나 트리거를 다시 밟아도 반복되지 않게 이름으로 기록)
        static readonly System.Collections.Generic.HashSet<string> played = new System.Collections.Generic.HashSet<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetPlayed() => played.Clear();

        IEnumerator Start()
        {
            if (startMode != StartMode.OnGameStart) yield break;
            yield return new WaitForSeconds(startDelay);
            Begin();
        }

        void OnTriggerEnter(Collider other)
        {
            if (startMode != StartMode.OnPlayerEnter || started) return;
            if (other.GetComponentInParent<XROrigin>() == null) return;
            StartCoroutine(WaitThenBegin());
        }

        IEnumerator WaitThenBegin()
        {
            started = true;
            while (after != null && !after.Finished) yield return null;
            Begin();
        }

        void Begin()
        {
            started = true;
            if (!played.Add(gameObject.name)) { Finished = true; return; }
            NarrationSubtitles.Play(lines, () => Finished = true);
        }
    }
}
