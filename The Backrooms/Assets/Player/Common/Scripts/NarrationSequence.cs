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
            NarrationSubtitles.Play(lines, () => Finished = true);
        }
    }
}
