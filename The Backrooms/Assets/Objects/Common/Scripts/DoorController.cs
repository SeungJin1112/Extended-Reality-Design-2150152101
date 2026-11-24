using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Backrooms
{
    public class DoorController : MonoBehaviour
    {
        public Transform hinge;
        public float maxAngle = 105f;
        public float speed = 180f;
        public float openThreshold = 35f;

        XRSimpleInteractable interactable;
        IXRSelectInteractor holder;
        float angle;
        float target;
        float grabOffset;

        public bool IsOpen => Mathf.Abs(angle) > openThreshold;

        void Awake()
        {
            if (hinge == null) hinge = transform.Find("Hinge");
            if (hinge == null) { enabled = false; return; }
            interactable = hinge.GetComponent<XRSimpleInteractable>();
            if (interactable == null) interactable = hinge.gameObject.AddComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(OnGrab);
            interactable.selectExited.AddListener(OnRelease);
            interactable.activated.AddListener(OnActivate);
        }

        void OnDestroy()
        {
            if (interactable == null) return;
            interactable.selectEntered.RemoveListener(OnGrab);
            interactable.selectExited.RemoveListener(OnRelease);
            interactable.activated.RemoveListener(OnActivate);
        }

        void OnGrab(SelectEnterEventArgs args)
        {
            holder = args.interactorObject;
            grabOffset = AngleTo(holder.GetAttachTransform(interactable).position) - angle;
        }

        void OnRelease(SelectExitEventArgs args)
        {
            if (args.interactorObject == holder) holder = null;
        }

        void OnActivate(ActivateEventArgs args) => Toggle();

        public void Toggle()
        {
            if (Mathf.Abs(target) > maxAngle * 0.5f) Close();
            else Open();
        }

        public void Open()
        {
            var cam = Camera.main;
            float side = 1f;
            if (cam != null) side = transform.InverseTransformPoint(cam.transform.position).z >= 0f ? 1f : -1f;
            target = maxAngle * side;
        }

        public void Close() => target = 0f;

        float AngleTo(Vector3 world)
        {
            var local = transform.InverseTransformPoint(world) - hinge.localPosition;
            return Mathf.Atan2(-local.z, local.x) * Mathf.Rad2Deg;
        }

        Transform panel;
        readonly System.Collections.Generic.List<Transform> hands = new System.Collections.Generic.List<Transform>();

        void Start()
        {
            if (hinge == null) return;
            panel = hinge.Find("Panel");
            foreach (var interactor in FindObjectsOfType<XRBaseInputInteractor>(true))
                hands.Add(interactor.transform);
        }

        void PushByHands()
        {
            if (panel == null) return;
            foreach (var hand in hands)
            {
                if (hand == null || !hand.gameObject.activeInHierarchy) continue;
                var p = panel.InverseTransformPoint(hand.position);
                if (Mathf.Abs(p.x) > 0.5f || Mathf.Abs(p.y) > 0.5f || Mathf.Abs(p.z) > 2.4f) continue;
                float handAngle = AngleTo(hand.position);
                target = p.z > 0f
                    ? Mathf.Clamp(Mathf.Max(target, handAngle + 7f), -maxAngle, maxAngle)
                    : Mathf.Clamp(Mathf.Min(target, handAngle - 7f), -maxAngle, maxAngle);
            }
        }

        void Update()
        {
            if (holder != null)
                target = Mathf.Clamp(Mathf.DeltaAngle(0f, AngleTo(holder.GetAttachTransform(interactable).position) - grabOffset), -maxAngle, maxAngle);
            else
                PushByHands();
            angle = Mathf.MoveTowards(angle, target, speed * Time.deltaTime);
            hinge.localRotation = Quaternion.Euler(0f, angle, 0f);
        }
    }
}
