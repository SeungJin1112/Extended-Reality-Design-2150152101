using Unity.XR.CoreUtils;
using UnityEngine;

namespace Backrooms
{
    [RequireComponent(typeof(XROrigin))]
    public class HeadCollisionGuard : MonoBehaviour
    {
        public float radius = 0.16f;

        XROrigin origin;
        SphereCollider probe;
        readonly Collider[] hits = new Collider[16];

        void Start()
        {
            origin = GetComponent<XROrigin>();
            var body = GetComponent<CharacterController>();
            if (body != null) body.stepOffset = Mathf.Min(body.stepOffset, 0.25f);
            if (origin.Camera == null) { enabled = false; return; }
            var go = new GameObject("HeadProbe");
            go.transform.SetParent(transform, false);
            go.layer = 2;
            probe = go.AddComponent<SphereCollider>();
            probe.isTrigger = true;
            probe.radius = radius;
        }

        void LateUpdate()
        {
            var head = origin.Camera.transform.position;
            probe.transform.position = head;
            int count = Physics.OverlapSphereNonAlloc(head, radius, hits, ~(1 << 2), QueryTriggerInteraction.Ignore);
            var push = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                var other = hits[i];
                if (other.transform.IsChildOf(transform)) continue;
                if (Physics.ComputePenetration(probe, head, Quaternion.identity, other, other.transform.position, other.transform.rotation, out var dir, out var dist))
                    push += dir * dist;
            }
            push.y = 0f;
            if (push.sqrMagnitude > 1e-6f)
                transform.position += push;
        }
    }
}
