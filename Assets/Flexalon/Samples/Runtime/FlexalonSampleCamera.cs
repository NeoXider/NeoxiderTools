using UnityEngine;

#if UNITY_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Flexalon.Samples
{
    // Simple camera controller.
    // Use WASD or arrows to move. Rotate with right mouse button.
    // Pan with mouse wheel button.
    public class FlexalonSampleCamera : MonoBehaviour
    {
        public float Speed = 0.2f;
        public float RotateSpeed = 0.2f;
        public float InterpolationSpeed = 20.0f;

        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private float _alpha;
        private float _beta;
        private Vector3 _mousePos;
        private bool _wasRightDown;
        private bool _wasMiddleDown;

        void Start()
        {
            _targetPosition = transform.position;
            _targetRotation = transform.rotation;
            var euler = _targetRotation.eulerAngles;
            _alpha = euler.y;
            _beta = euler.x;
        }

        void Update()
        {
#if UNITY_GUI
            if (UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject)
            {
                return;
            }
#endif

#if UNITY_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            var up = Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed;
            var left = Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed;
            var right = Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed;
            var down = Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed;
            var mouseRight = Mouse.current.rightButton.isPressed;
            var mouseMiddle = Mouse.current.middleButton.isPressed;
            Vector3 mousePosition = Mouse.current.position.value;
#else
            var up = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
            var left = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
            var right = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
            var down = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
            var mouseRight = Input.GetMouseButton(1);
            var mouseMiddle = Input.GetMouseButton(2);
            Vector3 mousePosition = Input.mousePosition;
#endif

            var mouseRightDown = !_wasRightDown && mouseRight;
            var mouseMiddleDown = !_wasMiddleDown && mouseMiddle;

            _wasRightDown = mouseRight;
            _wasMiddleDown = mouseMiddle;

            if (up)
            {
                _targetPosition += transform.forward * Speed;
            }

            if (left)
            {
                _targetPosition += -transform.right * Speed;
            }

            if (right)
            {
                _targetPosition += transform.right * Speed;
            }

            if (down)
            {
                _targetPosition += -transform.forward * Speed;
            }

            if (mouseRightDown || mouseMiddleDown)
            {
                _mousePos = mousePosition;
            }

            if (mouseRight)
            {
                var delta = mousePosition - _mousePos;
                _alpha += delta.x * RotateSpeed;
                _beta -= delta.y * RotateSpeed;
                _targetRotation = Quaternion.Euler(_beta, _alpha, 0);
                _mousePos = mousePosition;
            }

            if (mouseMiddleDown)
            {
                _mousePos = mousePosition;
            }

            if (mouseMiddle)
            {
                var delta = mousePosition - _mousePos;
                _targetPosition -= delta.y * transform.up * Speed;
                _targetPosition -= delta.x * transform.right * Speed;
                _mousePos = mousePosition;
            }

            transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * InterpolationSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, Time.deltaTime * InterpolationSpeed);
        }
    }
}