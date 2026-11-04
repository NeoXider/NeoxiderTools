using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Rotates the object every <c>LateUpdate</c> to face a camera, away from it, or along a fixed direction.
    /// </summary>
    [NeoDoc("Tools/View/BillboardUniversal.md")]
    [CreateFromMenu("Neoxider/Tools/View/BillboardUniversal")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(BillboardUniversal))]
    public class BillboardUniversal : MonoBehaviour
    {
        /// <summary>Which way the object is turned.</summary>
        public enum BillboardMode
        {
            /// <summary>Forward points at the camera.</summary>
            TowardsCamera,

            /// <summary>Forward points away from the camera (sprites and world-space UI read correctly).</summary>
            AwayFromCamera,

            /// <summary>Forward follows <see cref="BillboardUniversal.CustomDirection" />; no camera needed.</summary>
            TowardsDirection
        }

        [Header("References")]
        [Tooltip("Camera to face. Empty = Camera.main when the fallback below is on.")]
        [SerializeField]
        private Camera targetCamera;

        [Header("Settings")]
        [Tooltip("Face the camera, face away from it, or follow Custom Direction.")]
        [SerializeField]
        private BillboardMode billboardMode = BillboardMode.AwayFromCamera;

        [Tooltip("Flatten the direction onto the horizontal plane so the object only turns around Y.")]
        [SerializeField]
        private bool ignoreY = true;

        [Tooltip("World direction used by the TowardsDirection mode.")]
        [SerializeField]
        private Vector3 customDirection = Vector3.forward;

        [Header("Fallback")]
        [Tooltip(
            "Resolve Camera.main only when Target Camera is empty. Disable when the camera is injected by scene setup.")]
        [SerializeField]
        private bool useMainCameraFallback = true;

        [SerializeField] private bool logMissingCamera;
        private bool _missingCameraLogged;
        private Transform _transform;
        private Camera _cachedCamera;
        private Transform _cachedCameraTransform;

        /// <summary>Current mode; change it with <see cref="SetBillboardMode" />.</summary>
        public BillboardMode Mode => billboardMode;

        /// <summary>Whether rotation is limited to the Y axis; change it with <see cref="SetIgnoreY" />.</summary>
        public bool IgnoreY => ignoreY;

        /// <summary>Direction used by <see cref="BillboardMode.TowardsDirection" />.</summary>
        public Vector3 CustomDirection => customDirection;

        /// <summary>The assigned (or already resolved) camera; may be null before the first camera-facing frame.</summary>
        public Camera TargetCamera => targetCamera;

        private void Start()
        {
            if (billboardMode != BillboardMode.TowardsDirection)
            {
                ResolveCamera();
            }
        }

        private void LateUpdate()
        {
            ApplyRotation(billboardMode == BillboardMode.TowardsDirection ? null : ResolveCamera());
        }

#if UNITY_EDITOR
        /// <summary>
        /// Explicit edit-mode preview. There is deliberately no <c>OnValidate</c>: Unity calls it on
        /// prefab assets as they load, and rotating there baked a <c>Camera.main</c>-dependent rotation
        /// into every prefab that holds this component the next time the project saved.
        /// </summary>
        [ContextMenu("Face Camera Now")]
        private void FaceCameraNowInEditor()
        {
            Camera camera = targetCamera != null ? targetCamera : useMainCameraFallback ? Camera.main : null;
            UnityEditor.Undo.RecordObject(transform, "Billboard Face Camera");
            ApplyRotation(camera);
        }
#endif

        private void ApplyRotation(Camera camera)
        {
            if (camera == null && billboardMode != BillboardMode.TowardsDirection)
            {
                return;
            }

            Transform self = SelfTransform;
            Vector3 direction = GetDirection(camera, self);
            if (ignoreY)
            {
                direction.y = 0;
            }

            if (direction != Vector3.zero)
            {
                self.rotation = Quaternion.LookRotation(direction);
            }
        }

        // WHY cached: Component.transform is a native call, and the camera-facing modes read both transforms
        // every LateUpdate for every billboard in the scene.
        private Transform SelfTransform
        {
            get
            {
                if (ReferenceEquals(_transform, null))
                {
                    _transform = transform;
                }

                return _transform;
            }
        }

        private Transform CameraTransform(Camera camera)
        {
            if (!ReferenceEquals(_cachedCamera, camera))
            {
                _cachedCamera = camera;
                _cachedCameraTransform = camera.transform;
            }

            return _cachedCameraTransform;
        }

        private Vector3 GetDirection(Camera camera, Transform self)
        {
            return billboardMode switch
            {
                BillboardMode.TowardsCamera => CameraTransform(camera).position - self.position,
                BillboardMode.AwayFromCamera => self.position - CameraTransform(camera).position,
                BillboardMode.TowardsDirection => customDirection,
                _ => Vector3.zero
            };
        }

        /// <summary>Sets the direction used by <see cref="BillboardMode.TowardsDirection" />; zero leaves the rotation as is.</summary>
        public void SetCustomDirection(Vector3 direction)
        {
            customDirection = direction;
        }

        /// <summary>Switches the facing mode; takes effect on the next <c>LateUpdate</c>.</summary>
        public void SetBillboardMode(BillboardMode mode)
        {
            billboardMode = mode;
        }

        /// <summary>Limits (true) or frees (false) the rotation to the Y axis.</summary>
        public void SetIgnoreY(bool ignore)
        {
            ignoreY = ignore;
        }

        /// <summary>Assigns the camera to face explicitly; null re-enables the Camera.main fallback.</summary>
        public void SetTargetCamera(Camera camera)
        {
            targetCamera = camera;
            _missingCameraLogged = false;
        }

        private Camera ResolveCamera()
        {
            if (targetCamera != null)
            {
                return targetCamera;
            }

            if (useMainCameraFallback)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null && logMissingCamera && !_missingCameraLogged)
            {
                _missingCameraLogged = true;
                NeoDiagnostics.LogWarning($"[{nameof(BillboardUniversal)}] Target camera is not assigned.", this);
            }

            return targetCamera;
        }
    }
}
