using System;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
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
        [SerializeField]
        private int mouseButton;

        [Tooltip("When set, rotation only active while this key is held (e.g. Alt). None = no modifier.")]
        [SerializeField]
        private KeyCode modifierKey = KeyCode.None;

        [Tooltip("Multiplier for mouse delta (resolution-independent sensitivity).")] [SerializeField]
        private float mouseSensitivity = 0.1f;

        [Header("Settings")] public AxisRotationSettings xAxisSettings = new(true, 10);

        public AxisRotationSettings yAxisSettings = new(true, 10);
        public AxisRotationSettings zAxisSettings = new(false);

        [Header("Events")] [SerializeField] private UnityEvent onRotateStart = new();

        [SerializeField] private UnityEvent onRotateEnd = new();
        private bool _wasRotating;

        private Vector3 currentRotation;
        private Vector3 lastMousePosition;

        private void Start()
        {
            CaptureCurrentRotation();
        }

        private void Update()
        {
            if (!MouseInputCompat.TryGetPosition(out Vector3 mousePosition))
            {
                EndDrag();
                return;
            }

            bool modifierOk = modifierKey == KeyCode.None || KeyInputCompat.GetKey(modifierKey);
            bool buttonHeld = MouseInputCompat.TryGetButton(mouseButton, out bool held) && held;
            ProcessDragSample(new Vector2(mousePosition.x, mousePosition.y), buttonHeld, modifierOk, Time.deltaTime);
        }

        /// <summary>Processes the same pointer sample as Update without reading a physical device.</summary>
        internal void ProcessDragSample(Vector2? position, bool held, bool modifier, float deltaTime)
        {
            if (!isActiveAndEnabled || !position.HasValue || !held || !modifier)
            {
                EndDrag();
                return;
            }

            Vector3 mousePosition = position.Value;
            if (!_wasRotating)
            {
                CaptureCurrentRotation();
                lastMousePosition = mousePosition;
                _wasRotating = true;
                onRotateStart?.Invoke();
                return;
            }

            Vector3 mouseDelta = (mousePosition - lastMousePosition) * mouseSensitivity;
            lastMousePosition = mousePosition;
            if (mouseDelta.sqrMagnitude == 0f || deltaTime <= 0f)
            {
                return;
            }

            float mouseX = mouseDelta.x;
            float mouseY = mouseDelta.y;

            // Preserve the existing pixels * sensitivity * rotationSpeed * deltaTime contract.
            if (xAxisSettings != null && xAxisSettings.isEnabled)
            {
                currentRotation.x -= mouseY * xAxisSettings.rotationSpeed * deltaTime;
                currentRotation.x = xAxisSettings.ClampAngle(currentRotation.x);
            }

            if (yAxisSettings != null && yAxisSettings.isEnabled)
            {
                currentRotation.y += mouseX * yAxisSettings.rotationSpeed * deltaTime;
                currentRotation.y = yAxisSettings.ClampAngle(currentRotation.y);
            }

            if (zAxisSettings != null && zAxisSettings.isEnabled)
            {
                float mouseZ = (mouseX + mouseY) * 0.5f;
                currentRotation.z += mouseZ * zAxisSettings.rotationSpeed * deltaTime;
                currentRotation.z = zAxisSettings.ClampAngle(currentRotation.z);
            }

            transform.localRotation = Quaternion.Euler(currentRotation);
        }

        private void CaptureCurrentRotation()
        {
            Vector3 euler = transform.localEulerAngles;
            currentRotation = new Vector3(
                EquivalentAngle(euler.x, xAxisSettings),
                EquivalentAngle(euler.y, yAxisSettings),
                EquivalentAngle(euler.z, zAxisSettings));
        }

        private static float EquivalentAngle(float angle, AxisRotationSettings settings)
        {
            if (settings == null || !settings.isEnabled)
            {
                return angle;
            }

            double midpoint = ((double)settings.minAngle + settings.maxAngle) * 0.5d;
            double turns = Math.Round((midpoint - angle) / 360d);
            return (float)(angle + turns * 360d);
        }

        private void EndDrag()
        {
            if (_wasRotating)
            {
                _wasRotating = false;
                onRotateEnd?.Invoke();
            }
        }

        private void OnDisable()
        {
            EndDrag();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                EndDrag();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                EndDrag();
            }
        }
    }
}
