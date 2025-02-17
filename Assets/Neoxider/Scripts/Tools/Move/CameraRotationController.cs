using UnityEngine;

namespace Neo
{
    namespace Tools
    {
        [System.Serializable]
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

        public class CameraRotationController : MonoBehaviour
        {
            public AxisRotationSettings xAxisSettings = new AxisRotationSettings(true, 10);
            public AxisRotationSettings yAxisSettings = new AxisRotationSettings(true, 10);
            public AxisRotationSettings zAxisSettings = new AxisRotationSettings(false);

            private Vector3 currentRotation;
            private Vector3 lastMousePosition;

            void Start()
            {
                currentRotation = transform.localEulerAngles;
            }

            void Update()
            {
                if (Input.GetMouseButtonDown(0))
                    lastMousePosition = Input.mousePosition;

                if (Input.GetMouseButton(0))
                {
                    Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
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