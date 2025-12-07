#if UNITY_INPUT_SYSTEM

using UnityEngine;
using UnityEngine.InputSystem;

namespace Flexalon
{
    /// <summary>
    /// A simple input provider that uses the new input system
    /// That allows you to assign input actions for point and press.
    /// </summary>
    public class FlexalonInputSystemProvider : MonoBehaviour, InputProvider
    {
        // Implement InputProvider interface
        public bool Active => _active;
        public Vector3 UIPointer => _pointerInputAction.action.ReadValue<Vector2>();
        public Ray Ray => Camera.main.ScreenPointToRay(UIPointer);
        public InputMode InputMode => InputMode.Raycast;
        public GameObject ExternalFocusedObject => null;

        // Should be a button set to press and release (e.g. Left mouse click)
        [SerializeField]
        private InputActionReference _activeInputAction;

        // Should be a pointer position (e.g. mouse position)
        [SerializeField]
        private InputActionReference _pointerInputAction;

        private bool _active;

        private void OnEnable()
        {
            _activeInputAction.action.performed += OnPerformed;
        }

        private void OnDisable()
        {
            _activeInputAction.action.performed -= OnPerformed;
        }

        private void OnPerformed(InputAction.CallbackContext ctx)
        {
            _active = ctx.ReadValueAsButton();
        }
    }
}

#endif // UNITY_INPUT_SYSTEM