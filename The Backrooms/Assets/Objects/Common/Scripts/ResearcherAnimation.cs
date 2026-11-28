using UnityEngine;

namespace Backrooms
{
    // 연구원 NPC의 반복 동작을 시작 시점과 속도를 조금씩 다르게 재생한다.
    // 같은 동작을 쓰는 연구원끼리 박자가 똑같이 맞아 떨어지지 않게 하기 위함.
    public class ResearcherAnimation : MonoBehaviour
    {
        public string stateName;
        [Range(0f, 1f)] public float startOffset;
        public float speedVariance = 0.06f;

        void Start()
        {
            var anim = GetComponentInChildren<Animator>();
            if (anim == null || anim.runtimeAnimatorController == null || string.IsNullOrEmpty(stateName)) return;
            anim.applyRootMotion = false;
            anim.speed = 1f + Random.Range(-speedVariance, speedVariance);
            anim.Play(stateName, 0, startOffset);
        }
    }
}
