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
            BoxCollider2D,
            BoxCollider
        }

        [Header("Bounds Source")] [Tooltip("Type of boundary to use for constraining camera.")] [SerializeField]
        private BoundsType boundsType = BoundsType.SpriteRenderer;

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private BoxCollider2D boxCollider2D;
        [SerializeField] private BoxCollider boxCollider;

        [Header("Camera")] [Tooltip("Camera to constrain. Uses component's camera if not set.")] [SerializeField]
        private Camera cam;

        [Header("Settings")] [Tooltip("Additional padding from the edge of bounds.")] [SerializeField]
        private float edgePadding;

        [SerializeField] private bool constraintX = true;
        [SerializeField] private bool constraintY = true;
        [SerializeField] private bool constraintZ;

        [Tooltip("When true, bounds are recalculated each LateUpdate (useful for moving/animated bounds source).")]
        [SerializeField]
        private bool autoUpdateBounds = true;

        [Header("Debug")] [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private Color sourceBoundsColor = new(0.25f, 0.8f, 1f, 0.9f);
        [SerializeField] private Color constraintBoundsColor = new(0.2f, 1f, 0.35f, 0.9f);
        [SerializeField] private Color clampedPositionColor = new(1f, 0.92f, 0.2f, 0.95f);

        private bool _boundsValid;
        private Vector3 _maxBounds;

        private Vector3 _minBounds;
        private Bounds _sourceBounds;
        private Vector3 _lastConstrainedPosition;
        private int _lastScreenWidth = -1;
        private int _lastScreenHeight = -1;
        private float _lastAspect = -1f;
        private float _lastOrthoSize = -1f;
        private float _lastFov = -1f;

        private void Start()
        {
            if (!TryInitialize())
            {
                return;
            }

            UpdateBounds();
        }

        private void LateUpdate()
        {
            if (!TryInitialize())
            {
                return;
            }

            if (!ValidateBoundsSource())
            {
                _boundsValid = false;
                return;
            }

            if (autoUpdateBounds || ShouldRecalculateBounds())
            {
                CalculateBounds();
            }

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

            if (!TryResolveCamera())
            {
                return;
            }

            if (Application.isPlaying && _boundsValid)
            {
                DrawSourceBounds(_sourceBounds);
                DrawConstraintBounds(_minBounds, _maxBounds);
                DrawClampInfo();
            }
            else if (!Application.isPlaying)
            {
                if (ValidateBoundsSource())
                {
                    CalculateBoundsPreview();
                }
            }
        }

        private bool TryInitialize()
        {
            if (!TryResolveCamera())
            {
                Debug.LogError("CameraConstraint: No camera found!");
                enabled = false;
                return false;
            }

            if (!ValidateBoundsSource())
            {
                Debug.LogError($"CameraConstraint: Bounds source not set for type {boundsType}!");
                enabled = false;
                return false;
            }

            return true;
        }

        private bool TryResolveCamera()
        {
            if (cam != null)
            {
                return true;
            }

            cam = GetComponent<Camera>();
            return cam != null;
        }

        private bool ValidateBoundsSource()
        {
            return boundsType switch
            {
                BoundsType.SpriteRenderer => spriteRenderer != null,
                BoundsType.BoxCollider2D => boxCollider2D != null,
                BoundsType.BoxCollider => boxCollider != null,
                _ => false
            };
        }

        private Bounds GetSourceBounds()
        {
            return boundsType switch
            {
                BoundsType.SpriteRenderer => spriteRenderer.bounds,
                BoundsType.BoxCollider2D => boxCollider2D.bounds,
                BoundsType.BoxCollider => boxCollider.bounds,
                _ => new Bounds()
            };
        }

        private void CalculateBounds()
        {
            _sourceBounds = GetSourceBounds();
            Vector2 cameraExtents = GetCameraExtentsAtDepth(_sourceBounds.center.z);

            float padding = Mathf.Max(0f, edgePadding);
            _minBounds.x = _sourceBounds.min.x + cameraExtents.x + padding;
            _maxBounds.x = _sourceBounds.max.x - cameraExtents.x - padding;
            _minBounds.y = _sourceBounds.min.y + cameraExtents.y + padding;
            _maxBounds.y = _sourceBounds.max.y - cameraExtents.y - padding;
            _minBounds.z = _sourceBounds.min.z + padding;
            _maxBounds.z = _sourceBounds.max.z - padding;

            if (_minBounds.x > _maxBounds.x)
            {
                float centerX = _sourceBounds.center.x;
                _minBounds.x = _maxBounds.x = centerX;
            }

            if (_minBounds.y > _maxBounds.y)
            {
                float centerY = _sourceBounds.center.y;
                _minBounds.y = _maxBounds.y = centerY;
            }

            if (_minBounds.z > _maxBounds.z || !constraintZ)
            {
                float currentZ = transform.position.z;
                _minBounds.z = _maxBounds.z = currentZ;
            }

            CacheRuntimeState();
            _boundsValid = true;
        }

        private void CalculateBoundsPreview()
        {
            Bounds bounds = GetSourceBounds();
            Vector2 cameraExtents = GetCameraExtentsAtDepth(bounds.center.z);
            float padding = Mathf.Max(0f, edgePadding);

            Vector3 minBounds = new(
                bounds.min.x + cameraExtents.x + padding,
                bounds.min.y + cameraExtents.y + padding,
                bounds.min.z + padding);
            Vector3 maxBounds = new(
                bounds.max.x - cameraExtents.x - padding,
                bounds.max.y - cameraExtents.y - padding,
                bounds.max.z - padding);

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

            if (minBounds.z > maxBounds.z || !constraintZ)
            {
                float currentZ = transform.position.z;
                minBounds.z = maxBounds.z = currentZ;
            }

            DrawSourceBounds(bounds);
            DrawConstraintBounds(minBounds, maxBounds);
            DrawClampInfoPreview(minBounds, maxBounds);
        }

        private Vector2 GetCameraExtentsAtDepth(float targetDepthZ)
        {
            if (cam.orthographic)
            {
                float halfHeight = cam.orthographicSize;
                float halfWidth = halfHeight * cam.aspect;
                return new Vector2(halfWidth, halfHeight);
            }

            float distanceToPlane = Mathf.Abs(transform.position.z - targetDepthZ);
            float halfHeight3D = distanceToPlane * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float halfWidth3D = halfHeight3D * cam.aspect;
            return new Vector2(halfWidth3D, halfHeight3D);
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

            _lastConstrainedPosition = position;
            transform.position = position;
        }

        private bool ShouldRecalculateBounds()
        {
            if (_lastScreenWidth != Screen.width || _lastScreenHeight != Screen.height)
            {
                return true;
            }

            if (cam == null)
            {
                return false;
            }

            if (!Mathf.Approximately(_lastAspect, cam.aspect))
            {
                return true;
            }

            if (cam.orthographic)
            {
                return !Mathf.Approximately(_lastOrthoSize, cam.orthographicSize);
            }

            return !Mathf.Approximately(_lastFov, cam.fieldOfView);
        }

        private void CacheRuntimeState()
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            _lastAspect = cam != null ? cam.aspect : -1f;
            _lastOrthoSize = cam != null ? cam.orthographicSize : -1f;
            _lastFov = cam != null ? cam.fieldOfView : -1f;
        }

        private void DrawSourceBounds(Bounds bounds)
        {
            Gizmos.color = sourceBoundsColor;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        private void DrawConstraintBounds(Vector3 min, Vector3 max)
        {
            Gizmos.color = constraintBoundsColor;

            Vector3 topLeftFront = new(min.x, max.y, min.z);
            Vector3 topRightFront = new(max.x, max.y, min.z);
            Vector3 bottomLeftFront = new(min.x, min.y, min.z);
            Vector3 bottomRightFront = new(max.x, min.y, min.z);

            Gizmos.DrawLine(topLeftFront, topRightFront);
            Gizmos.DrawLine(topRightFront, bottomRightFront);
            Gizmos.DrawLine(bottomRightFront, bottomLeftFront);
            Gizmos.DrawLine(bottomLeftFront, topLeftFront);

            if (cam != null && !cam.orthographic && constraintZ)
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
        }

        private void DrawClampInfo()
        {
            Gizmos.color = clampedPositionColor;
            Gizmos.DrawWireSphere(_lastConstrainedPosition, 0.2f);
            Gizmos.DrawLine(transform.position, _lastConstrainedPosition);
        }

        private void DrawClampInfoPreview(Vector3 min, Vector3 max)
        {
            Vector3 pos = transform.position;
            Vector3 clamped = new(
                constraintX ? Mathf.Clamp(pos.x, min.x, max.x) : pos.x,
                constraintY ? Mathf.Clamp(pos.y, min.y, max.y) : pos.y,
                constraintZ ? Mathf.Clamp(pos.z, min.z, max.z) : pos.z);
            Gizmos.color = clampedPositionColor;
            Gizmos.DrawWireSphere(clamped, 0.2f);
            Gizmos.DrawLine(pos, clamped);
        }

        #region === Public API ===

        /// <summary>
        ///     Recalculates bounds. Call if bounds or camera parameters change at runtime.
        /// </summary>
        public void UpdateBounds()
        {
            if (!TryResolveCamera() || !ValidateBoundsSource())
            {
                _boundsValid = false;
                return;
            }

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
                case BoundsType.BoxCollider2D:
                    if (source is BoxCollider2D col2D)
                    {
                        boxCollider2D = col2D;
                    }

                    break;
                case BoundsType.BoxCollider:
                    if (source is BoxCollider col)
                    {
                        boxCollider = col;
                    }

                    break;
            }

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