using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace Backrooms
{
    [RequireComponent(typeof(XROrigin))]
    public class DesktopPlayerControls : MonoBehaviour
    {
        public float walkSpeed = 1.6f;
        public float sprintSpeed = 3.6f;
        public float mouseSensitivity = 0.12f;
        public float eyeHeight = 1.65f;
        public float interactDistance = 2.4f;

        XROrigin origin;
        CharacterController controller;
        Transform cam;
        float pitch;
        float verticalVelocity;
        bool active;
        Transform leftHand, rightHand;
        public Vector3 leftHandOffset = new Vector3(-0.2f, -0.28f, 0.42f);
        public Vector3 rightHandOffset = new Vector3(0.2f, -0.28f, 0.42f);

        void SetupHands()
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Left Controller") leftHand = t;
                else if (t.name == "Right Controller") rightHand = t;
            }
            foreach (var hand in new[] { leftHand, rightHand })
            {
                if (hand == null) continue;
                hand.gameObject.SetActive(true);
                foreach (var col in hand.GetComponentsInChildren<Collider>(true))
                    if (controller != null && col != controller) Physics.IgnoreCollision(controller, col, true);
                foreach (var behaviour in hand.GetComponents<MonoBehaviour>())
                    if ((behaviour.GetType().FullName ?? "").Contains("TrackedPoseDriver")) behaviour.enabled = false;
            }
        }

        void PlaceHands()
        {
            if (leftHand != null) leftHand.SetPositionAndRotation(cam.TransformPoint(leftHandOffset), cam.rotation * Quaternion.Euler(10f, 6f, 0f));
            if (rightHand != null) rightHand.SetPositionAndRotation(cam.TransformPoint(rightHandOffset), cam.rotation * Quaternion.Euler(10f, -6f, 0f));
        }

        void LateUpdate()
        {
            if (active) PlaceHands();
        }

        void Start()
        {
            origin = GetComponent<XROrigin>();
            controller = GetComponent<CharacterController>();
            cam = origin.Camera != null ? origin.Camera.transform : null;
            active = !XRDeviceActive() && cam != null && controller != null;
            if (!active) { enabled = false; return; }

            foreach (var behaviour in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == this) continue;
                var type = behaviour.GetType().FullName ?? "";
                if (type.Contains("TrackedPoseDriver") && behaviour.transform == cam) behaviour.enabled = false;
                if (type.Contains("Locomotion") || type.Contains("MoveProvider") || type.Contains("TurnProvider") || type.Contains("InputModalityManager")) behaviour.enabled = false;
            }
            if (origin.CameraFloorOffsetObject != null)
                origin.CameraFloorOffsetObject.transform.localPosition = Vector3.zero;
            cam.localPosition = new Vector3(0, eyeHeight, 0);
            cam.localRotation = Quaternion.identity;
            SetupHands();
            foreach (var col in GetComponentsInChildren<Collider>(true))
                if (col != controller) Physics.IgnoreCollision(controller, col, true);
            controller.height = eyeHeight + 0.1f;
            controller.radius = 0.25f;
            controller.stepOffset = 0.25f;
            controller.center = new Vector3(0, controller.height / 2 + controller.skinWidth, 0);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        static bool XRDeviceActive()
        {
            var displays = new List<XRDisplaySubsystem>();
            SubsystemManager.GetInstances(displays);
            foreach (var d in displays)
                if (d.running) return true;
            return false;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            if (mouse.leftButton.wasPressedThisFrame) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                var delta = mouse.delta.ReadValue() * mouseSensitivity;
                transform.Rotate(0f, delta.x, 0f, Space.World);
                pitch = Mathf.Clamp(pitch - delta.y, -85f, 85f);
                cam.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            var input = Vector2.zero;
            if (keyboard.wKey.isPressed) input.y += 1;
            if (keyboard.sKey.isPressed) input.y -= 1;
            if (keyboard.dKey.isPressed) input.x += 1;
            if (keyboard.aKey.isPressed) input.x -= 1;
            float speed = keyboard.leftShiftKey.isPressed ? sprintSpeed : walkSpeed;
            var move = (transform.forward * input.y + transform.right * input.x).normalized * speed;
            verticalVelocity = controller.isGrounded ? -1f : verticalVelocity + Physics.gravity.y * Time.deltaTime;
            move.y = verticalVelocity;
            controller.Move(move * Time.deltaTime);

            if (keyboard.eKey.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)
            {
                if (Physics.Raycast(cam.position, cam.forward, out var hit, interactDistance, ~0, QueryTriggerInteraction.Ignore))
                {
                    var door = hit.collider.GetComponentInParent<DoorController>();
                    if (door != null) door.Toggle();
                }
            }
        }
    }
}
