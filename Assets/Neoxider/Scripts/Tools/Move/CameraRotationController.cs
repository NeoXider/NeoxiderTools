using System;
using UnityEngine;
using UnityEngine.Events;

namespace Neo
{
    namespace Tools
    {
        [Serializable]
        public class AxisRotationSettings
        {
            public bool isEnabled = true;
            public float rotationSpeed = 10;
            public float minAngle = -45f;
            public float maxAngle = 45f;

            public AxisRotationSettings(bool isEnabled = true,
                float rotationSpeed = 100f,
                float minAngle = -45f,
                float maxAngle = 45f)
            {
                this.isEnabled = isEnabled;
                this.rotationSpeed = rotationSpeed;
                this.minAngle = minAngle;
                this.maxAngle = maxAngle;
            }

            public float ClampAngle(float angle)
            {
                return Mathf.Clamp(angle, minAngle, maxAngle);
            }
        }

        [NeoDoc("Tools/Move/CameraRotationController.md")]
        [CreateFromMenu("Neoxider/Tools/Movement/CameraRotationController")]
        [AddComponentMenu("Neoxider/" + "Tools/" + nameof(CameraRotationController))]
        public class CameraRotationController : MonoBehaviour
        {
            [Header("Input")]
            [Tooltip("Mouse button for drag rotation (0 = left, 1 = right, 2 = middle).")]
            [SerializeField] private int mouseButton = 0;
            [Tooltip("When set, rotation only active while this key is held (e.g. Alt). None = no modifier.")]
            [SerializeField] private KeyCode modifierKey = KeyCode.None;
            [Tooltip("Multiplier for mouse delta (resolution-independent sensitivity).")]
            [SerializeField] private float mouseSensitivity = 0.1f;

            [Header("Settings")]
            public AxisRotationSettings xAxisSettings = new(true, 10);
            public AxisRotationSettings yAxisSettings = new(true, 10);
            public AxisRotationSettings zAxisSettings = new(false);

            [Header("Events")]
            [SerializeField] private UnityEvent onRotateStart = new UnityEvent();
            [SerializeField] private UnityEvent onRotateEnd = new UnityEvent();

            private Vector3 currentRotation;
            private Vector3 lastMousePosition;
            private bool _wasRotating;

            private void Start()
            {
                currentRotation = transform.localEulerAngles;
            }

            private void Update()
            {
                bool modifierOk = modifierKey == KeyCode.None || Input.GetKey(modifierKey);
                bool buttonDown = Input.GetMouseButtonDown(mouseButton);
                bool buttonHeld = Input.GetMouseButton(mouseButton);
                bool buttonUp = Input.GetMouseButtonUp(mouseButton);

                if (buttonDown && modifierOk)
                {
                    lastMousePosition = Input.mousePosition;
                    _wasRotating = true;
                    onRotateStart?.Invoke();
                }

                if (buttonUp && _wasRotating)
                {
                    _wasRotating = false;
                    onRotateEnd?.Invoke();
                }

                if (buttonHeld && modifierOk)
                {
                    Vector3 mouseDelta = (Input.mousePosition - lastMousePosition) * mouseSensitivity;
                    lastMousePosition = Input.mousePosition;

                    float mouseX = mouseDelta.x;
                    float mouseY = mouseDelta.y;

                    if (xAxisSettings.isEnabled)
                    {
                        currentRotation.x -= mouseY * xAxisSettings.rotationSpeed * Time.deltaTime;
                        currentRotation.x = xAxisSettings.ClampAngle(currentRotation.x);
                    }

                    if (yAxisSettings.isEnabled)
                    {
                        currentRotation.y += mouseX * yAxisSettings.rotationSpeed * Time.deltaTime;
                        currentRotation.y = yAxisSettings.ClampAngle(currentRotation.y);
                    }

                    if (zAxisSettings.isEnabled)
                    {
                        float mouseZ = (mouseX + mouseY) * 0.5f;
                        currentRotation.z += mouseZ * zAxisSettings.rotationSpeed * Time.deltaTime;
                        currentRotation.z = zAxisSettings.ClampAngle(currentRotation.z);
                    }

                    transform.localRotation = Quaternion.Euler(currentRotation);
                }
            }
        }
    }
}