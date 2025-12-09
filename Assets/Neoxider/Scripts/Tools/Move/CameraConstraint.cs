using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Constrains camera movement within specified bounds.
    ///     Supports both 2D and 3D cameras with multiple boundary types.
    /// </summary>
    [AddComponentMenu("Neo/" + "Tools/" + nameof(CameraConstraint))]
    public class CameraConstraint : MonoBehaviour
    {
        public enum BoundsType
        {
            SpriteRenderer,
            Collider2D,
            Collider,
            Manual
        }

        [Header("Bounds Source")] [Tooltip("Type of boundary to use for constraining camera.")] [SerializeField]
        private BoundsType boundsType = BoundsType.SpriteRenderer;

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Collider2D collider2D;
        [SerializeField] private Collider collider;

        [Header("Manual Bounds (when BoundsType is Manual)")] [SerializeField]
        private Bounds manualBounds = new(Vector3.zero, Vector3.one * 100f);

        [Header("Camera")] [Tooltip("Camera to constrain. Uses component's camera if not set.")] [SerializeField]
        private Camera cam;

        [Header("Settings")] [Tooltip("Additional padding from the edge of bounds.")] [SerializeField]
        private float edgePadding;

        [SerializeField] private bool constraintX = true;
        [SerializeField] private bool constraintY = true;
        [SerializeField] private bool constraintZ = true;

        [Header("Debug")] [SerializeField] private bool showDebugGizmos = true;

        private bool _boundsValid;
        private Vector3 _maxBounds;

        private Vector3 _minBounds;

        private void Start()
        {
            if (cam == null)
            {
                cam = GetComponent<Camera>();
            }

            if (cam == null)
            {
                Debug.LogError("CameraConstraint: No camera found!");
                enabled = false;
                return;
            }

            if (!ValidateBoundsSource())
            {
                Debug.LogError($"CameraConstraint: Bounds source not set for type {boundsType}!");
                enabled = false;
                return;
            }

            CalculateBounds();
        }

        private void LateUpdate()
        {
            if (_boundsValid)
            {
                ConstrainCamera();
            }
        }

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos)
            {
                return;
            }

            if (cam == null)
            {
                cam = GetComponent<Camera>();
            }

            if (cam == null)
            {
                return;
            }

            if (Application.isPlaying && _boundsValid)
            {
                DrawConstraintBounds(_minBounds, _maxBounds);
            }
            else if (!Application.isPlaying)
            {
                if (ValidateBoundsSource())
                {
                    CalculateBoundsPreview();
                }
            }
        }

        private bool ValidateBoundsSource()
        {
            return boundsType switch
            {
                BoundsType.SpriteRenderer => spriteRenderer != null,
                BoundsType.Collider2D => collider2D != null,
                BoundsType.Collider => collider != null,
                BoundsType.Manual => true,
                _ => false
            };
        }

        private Bounds GetSourceBounds()
        {
            return boundsType switch
            {
                BoundsType.SpriteRenderer => spriteRenderer.bounds,
                BoundsType.Collider2D => collider2D.bounds,
                BoundsType.Collider => collider.bounds,
                BoundsType.Manual => manualBounds,
                _ => new Bounds()
            };
        }

        private void CalculateBounds()
        {
            Bounds bounds = GetSourceBounds();
            Vector3 cameraSize = GetCameraSize();

            _minBounds = bounds.min + cameraSize + Vector3.one * edgePadding;
            _maxBounds = bounds.max - cameraSize - Vector3.one * edgePadding;

            if (_minBounds.x > _maxBounds.x)
            {
                float centerX = bounds.center.x;
                _minBounds.x = _maxBounds.x = centerX;
            }

            if (_minBounds.y > _maxBounds.y)
            {
                float centerY = bounds.center.y;
                _minBounds.y = _maxBounds.y = centerY;
            }

            if (_minBounds.z > _maxBounds.z)
            {
                float centerZ = bounds.center.z;
                _minBounds.z = _maxBounds.z = centerZ;
            }

            _boundsValid = true;
        }

        private void CalculateBoundsPreview()
        {
            Bounds bounds = GetSourceBounds();
            Vector3 cameraSize = GetCameraSize();

            Vector3 minBounds = bounds.min + cameraSize + Vector3.one * edgePadding;
            Vector3 maxBounds = bounds.max - cameraSize - Vector3.one * edgePadding;

            if (minBounds.x > maxBounds.x)
            {
                float centerX = bounds.center.x;
                minBounds.x = maxBounds.x = centerX;
            }

            if (minBounds.y > maxBounds.y)
            {
                float centerY = bounds.center.y;
                minBounds.y = maxBounds.y = centerY;
            }

            if (minBounds.z > maxBounds.z)
            {
                float centerZ = bounds.center.z;
                minBounds.z = maxBounds.z = centerZ;
            }

            DrawConstraintBounds(minBounds, maxBounds);
        }

        private Vector3 GetCameraSize()
        {
            if (cam.orthographic)
            {
                float height = cam.orthographicSize;
                float width = height * cam.aspect;
                return new Vector3(width, height, 0f);
            }

            float distance = Mathf.Abs(transform.position.z);
            float height3D = distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float width3D = height3D * cam.aspect;
            return new Vector3(width3D, height3D, 0f);
        }

        private void ConstrainCamera()
        {
            Vector3 position = transform.position;

            if (constraintX)
            {
                position.x = Mathf.Clamp(position.x, _minBounds.x, _maxBounds.x);
            }

            if (constraintY)
            {
                position.y = Mathf.Clamp(position.y, _minBounds.y, _maxBounds.y);
            }

            if (constraintZ)
            {
                position.z = Mathf.Clamp(position.z, _minBounds.z, _maxBounds.z);
            }

            transform.position = position;
        }

        private void DrawConstraintBounds(Vector3 min, Vector3 max)
        {
            Gizmos.color = Color.green;

            Vector3 topLeftFront = new(min.x, max.y, min.z);
            Vector3 topRightFront = new(max.x, max.y, min.z);
            Vector3 bottomLeftFront = new(min.x, min.y, min.z);
            Vector3 bottomRightFront = new(max.x, min.y, min.z);

            Gizmos.DrawLine(topLeftFront, topRightFront);
            Gizmos.DrawLine(topRightFront, bottomRightFront);
            Gizmos.DrawLine(bottomRightFront, bottomLeftFront);
            Gizmos.DrawLine(bottomLeftFront, topLeftFront);

            if (cam != null && !cam.orthographic)
            {
                Vector3 topLeftBack = new(min.x, max.y, max.z);
                Vector3 topRightBack = new(max.x, max.y, max.z);
                Vector3 bottomLeftBack = new(min.x, min.y, max.z);
                Vector3 bottomRightBack = new(max.x, min.y, max.z);

                Gizmos.DrawLine(topLeftBack, topRightBack);
                Gizmos.DrawLine(topRightBack, bottomRightBack);
                Gizmos.DrawLine(bottomRightBack, bottomLeftBack);
                Gizmos.DrawLine(bottomLeftBack, topLeftBack);

                Gizmos.DrawLine(topLeftFront, topLeftBack);
                Gizmos.DrawLine(topRightFront, topRightBack);
                Gizmos.DrawLine(bottomLeftFront, bottomLeftBack);
                Gizmos.DrawLine(bottomRightFront, bottomRightBack);
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(new Vector3(min.x, max.y, min.z), 0.2f);
            Gizmos.DrawWireSphere(new Vector3(max.x, max.y, min.z), 0.2f);
            Gizmos.DrawWireSphere(new Vector3(min.x, min.y, min.z), 0.2f);
            Gizmos.DrawWireSphere(new Vector3(max.x, min.y, min.z), 0.2f);
        }

        #region === Public API ===

        /// <summary>
        ///     Recalculates bounds. Call if bounds or camera parameters change at runtime.
        /// </summary>
        public void UpdateBounds()
        {
            CalculateBounds();
        }

        /// <summary>
        ///     Set bounds type and source at runtime.
        /// </summary>
        public void SetBoundsSource(BoundsType type, Object source = null)
        {
            boundsType = type;

            switch (type)
            {
                case BoundsType.SpriteRenderer:
                    if (source is SpriteRenderer sr)
                    {
                        spriteRenderer = sr;
                    }

                    break;
                case BoundsType.Collider2D:
                    if (source is Collider2D col2D)
                    {
                        collider2D = col2D;
                    }

                    break;
                case BoundsType.Collider:
                    if (source is Collider col)
                    {
                        collider = col;
                    }

                    break;
            }

            UpdateBounds();
        }

        /// <summary>
        ///     Set manual bounds at runtime.
        /// </summary>
        public void SetManualBounds(Bounds bounds)
        {
            boundsType = BoundsType.Manual;
            manualBounds = bounds;
            UpdateBounds();
        }

        /// <summary>
        ///     Set edge padding at runtime.
        /// </summary>
        public void SetEdgePadding(float padding)
        {
            edgePadding = Mathf.Max(0f, padding);
            UpdateBounds();
        }

        /// <summary>
        ///     Enable or disable constraint on specific axis.
        /// </summary>
        public void SetAxisConstraint(bool x, bool y, bool z)
        {
            constraintX = x;
            constraintY = y;
            constraintZ = z;
        }

        /// <summary>
        ///     Get current calculated bounds.
        /// </summary>
        public void GetConstraintBounds(out Vector3 min, out Vector3 max)
        {
            min = _minBounds;
            max = _maxBounds;
        }

        /// <summary>
        ///     Check if camera is at edge of bounds.
        /// </summary>
        public bool IsAtEdge(out bool atMinX, out bool atMaxX, out bool atMinY, out bool atMaxY)
        {
            Vector3 pos = transform.position;
            float threshold = 0.01f;

            atMinX = Mathf.Abs(pos.x - _minBounds.x) < threshold;
            atMaxX = Mathf.Abs(pos.x - _maxBounds.x) < threshold;
            atMinY = Mathf.Abs(pos.y - _minBounds.y) < threshold;
            atMaxY = Mathf.Abs(pos.y - _maxBounds.y) < threshold;

            return atMinX || atMaxX || atMinY || atMaxY;
        }

        #endregion
    }
}