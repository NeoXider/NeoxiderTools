using UnityEngine;

#if UNITY_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Flexalon
{
    /// <summary> Simple input provider that uses the mouse for input. </summary>
    public class FlexalonMouseInputProvider : InputProvider
    {
#if UNITY_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
        public bool Active => Mouse.current.leftButton.isPressed;
        public Vector3 UIPointer => Mouse.current.position.value;
#else
        public bool Active => Input.GetMouseButton(0);
        public Vector3 UIPointer => Input.mousePosition;
#endif

        public Ray Ray => Camera.main.ScreenPointToRay(UIPointer);
        public InputMode InputMode => InputMode.Raycast;
        public GameObject ExternalFocusedObject => null;
    }
}