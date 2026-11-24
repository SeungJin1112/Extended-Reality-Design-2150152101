using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

namespace Backrooms
{
    public class SprintController : MonoBehaviour
    {
        public float multiplier = 2.1f;
        public InputActionProperty sprintAction = new InputActionProperty(new InputAction("Sprint", InputActionType.Button));

        ContinuousMoveProvider move;
        float baseSpeed;

        void Awake()
        {
            move = GetComponentInChildren<ContinuousMoveProvider>(true);
            if (move != null) baseSpeed = move.moveSpeed;
            var action = sprintAction.action;
            if (action != null && action.bindings.Count == 0)
            {
                action.AddBinding("<XRController>{LeftHand}/{Primary2DAxisClick}");
                action.AddBinding("<XRController>{LeftHand}/thumbstickClicked");
                action.AddBinding("<Keyboard>/leftShift");
            }
        }

        void OnEnable() => sprintAction.action?.Enable();
        void OnDisable() => sprintAction.action?.Disable();

        void Update()
        {
            if (move == null) return;
            bool sprinting = sprintAction.action != null && sprintAction.action.IsPressed();
            move.moveSpeed = sprinting ? baseSpeed * multiplier : baseSpeed;
        }
    }
}
