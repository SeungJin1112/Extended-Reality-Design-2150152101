using Unity.XR.CoreUtils;
using UnityEngine;

namespace Backrooms
{
    [RequireComponent(typeof(BoxCollider))]
    public class ZonePortal : MonoBehaviour
    {
        public string targetZone;
        public string targetMarker = "EntryPoint";
        public float cooldown = 1.5f;
        public DoorController door;

        static float lastTeleport = -10f;

        void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (Time.time - lastTeleport < cooldown) return;
            if (door != null && !door.IsOpen) return;
            var origin = other.GetComponentInParent<XROrigin>();
            if (origin == null) return;
            var zone = GameObject.Find("/" + targetZone);
            if (zone == null) return;
            var marker = zone.transform.Find("Markers/" + targetMarker);
            if (marker == null) return;

            lastTeleport = Time.time;
            var controller = origin.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            origin.MatchOriginUpCameraForward(Vector3.up, marker.forward);
            origin.MoveCameraToWorldLocation(marker.position + Vector3.up * origin.CameraInOriginSpaceHeight);
            if (controller != null) controller.enabled = true;

            var atmosphere = zone.GetComponent<ZoneAtmosphere>();
            if (atmosphere != null) atmosphere.Apply();
        }
    }
}
