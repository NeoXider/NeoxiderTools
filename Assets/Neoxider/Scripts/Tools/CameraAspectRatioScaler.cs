using UnityEngine;

namespace Neo
{
    /// <summary>
    ///     Provides automatic camera scaling functionality to maintain consistent view proportions across different screen
    ///     resolutions.
    /// </summary>
    /// <remarks>
    ///     This component supports both orthographic and perspective cameras, offering multiple scaling modes to handle
    ///     various aspect ratios.
    ///     It automatically adjusts the camera's view to match the target resolution while maintaining the desired aspect
    ///     ratio.
    /// </remarks>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Neo/" + "Tools/" + nameof(CameraAspectRatioScaler))]
    public class CameraAspectRatioScaler : MonoBehaviour
    {
        /// <summary>
        ///     Specifies the strategy used to scale the camera view when the screen resolution changes.
        /// </summary>
        public enum ScaleMode
        {
            /// <summary>Maintains the target width, potentially cropping the top and bottom of the view.</summary>
            FitWidth,

            /// <summary>Maintains the target height, potentially cropping the left and right sides of the view.</summary>
            FitHeight,

            /// <summary>Maintains both dimensions, potentially adding letterboxing to preserve the aspect ratio.</summary>
            FitBoth,

            /// <summary>Disables automatic scaling, allowing manual control of the camera view.</summary>
            Manual
        }

        [Header("Core Settings")]
        [Tooltip("Specifies how the camera view should scale when the screen resolution changes.")]
        [SerializeField]
        private ScaleMode scaleMode = ScaleMode.FitBoth;

        [Tooltip("When enabled, uses the specified target resolution for scaling calculations.")] [SerializeField]
        private bool useTargetResolution = true;

        [Tooltip("The resolution (in pixels) that the camera should be optimized for.")] [SerializeField]
        private Vector2 targetResolution = new(1920, 1080);

        [Tooltip("Additional scaling factor applied to the calculated camera size.")]
        [Range(0.1f, 10f)]
        [SerializeField]
        private float scaleMultiplier = 1f;

        [Tooltip("Use viewport letterbox/pillarbox in FitBoth mode to preserve target aspect exactly.")]
        [SerializeField]
        private bool useViewportRectInFitBoth = true;

        [Header("Scaling Limits")]
        [Tooltip("The minimum allowed size for orthographic cameras or field of view for perspective cameras.")]
        [SerializeField]
        private float minSize = 5f;

        [Tooltip("The maximum allowed size for orthographic cameras or field of view for perspective cameras.")]
        [SerializeField]
        private float maxSize = 20f;

        [Header("Update Settings")]
        [Tooltip("If enabled, updates the camera scale during gameplay when the screen resolution changes.")]
        [SerializeField]
        private bool updateInRuntime = true;

        [Tooltip("If enabled, updates the camera scale in the Unity Editor when values are modified.")] [SerializeField]
        private bool updateInEditor = true;

        [Tooltip("Restores full viewport rect when component is disabled.")] [SerializeField]
        private bool resetViewportRectOnDisable = true;

        private Camera _camera;
        private float _referenceFov;
        private float _referenceSize;
        private int _lastScreenWidth = -1;
        private int _lastScreenHeight = -1;
        private ScaleMode _lastScaleMode;
        private bool _lastUseTargetResolution;
        private Vector2 _lastTargetResolution;
        private float _lastScaleMultiplier;
        private bool _lastUseViewportRectInFitBoth;
        private bool _referenceCaptured;

        /// <summary>
        ///     Initializes the component and stores the camera's default values.
        /// </summary>
        private void Awake()
        {
            EnsureInitialized();
        }

        /// <summary>
        ///     Applies the initial camera scaling when the game starts.
        /// </summary>
        private void Start()
        {
            ApplyCameraScale(true);
        }

        private void OnEnable()
        {
            EnsureInitialized();
            ApplyCameraScale(true);
        }

        private void OnDisable()
        {
            if (_camera != null && resetViewportRectOnDisable)
            {
                _camera.rect = new Rect(0f, 0f, 1f, 1f);
            }
        }

        /// <summary>
        ///     Updates the camera scale based on the current screen resolution and settings.
        /// </summary>
        private void Update()
        {
            if ((Application.isPlaying && updateInRuntime) || (!Application.isPlaying && updateInEditor))
            {
                ApplyCameraScale();
            }
        }

        /// <summary>
        ///     Validates and updates camera settings when values are modified in the Unity Inspector.
        /// </summary>
        private void OnValidate()
        {
            EnsureInitialized();
            targetResolution.x = Mathf.Max(1f, targetResolution.x);
            targetResolution.y = Mathf.Max(1f, targetResolution.y);
            minSize = Mathf.Max(0.01f, minSize);
            maxSize = Mathf.Max(minSize, maxSize);
            scaleMultiplier = Mathf.Max(0.01f, scaleMultiplier);
            ApplyCameraScale(true);
        }

        /// <summary>
        ///     Calculates and applies the appropriate camera scale based on current settings.
        /// </summary>
        [Button("Apply Camera Scale")]
        private void ApplyCameraScaleButton()
        {
            ApplyCameraScale(true);
        }

        private void ApplyCameraScale(bool force = false)
        {
            if (_camera == null)
            {
                return;
            }

            if (!force && !HasRuntimeDataChanged())
            {
                return;
            }

            float targetAspect = GetTargetAspect();
            float currentAspect = GetCurrentAspect();
            if (currentAspect <= 0f)
            {
                return;
            }

            if (scaleMode == ScaleMode.Manual)
            {
                if (_camera.rect != new Rect(0f, 0f, 1f, 1f))
                {
                    _camera.rect = new Rect(0f, 0f, 1f, 1f);
                }

                CacheCurrentState();
                return;
            }

            if (_camera.orthographic)
            {
                UpdateOrthographicCamera(targetAspect, currentAspect);
            }
            else
            {
                UpdatePerspectiveCamera(targetAspect, currentAspect);
            }

            UpdateViewportRect(targetAspect, currentAspect);
            CacheCurrentState();
        }

        /// <summary>
        ///     Updates the orthographic camera size based on the current aspect ratio and settings.
        /// </summary>
        /// <param name="targetAspect">The target aspect ratio to maintain.</param>
        /// <param name="currentAspect">The current screen aspect ratio.</param>
        private void UpdateOrthographicCamera(float targetAspect, float currentAspect)
        {
            float baseSize = GetReferenceSize();
            float fitWidthSize = baseSize * (targetAspect / currentAspect);
            float newSize = baseSize;

            switch (scaleMode)
            {
                case ScaleMode.FitWidth:
                    newSize = fitWidthSize;
                    break;
                case ScaleMode.FitHeight:
                    newSize = baseSize;
                    break;
                case ScaleMode.FitBoth:
                    newSize = useViewportRectInFitBoth ? baseSize : Mathf.Max(baseSize, fitWidthSize);
                    break;
                case ScaleMode.Manual:
                    return;
            }

            newSize *= scaleMultiplier;
            newSize = Mathf.Clamp(newSize, minSize, maxSize);
            _camera.orthographicSize = newSize;
        }

        /// <summary>
        ///     Updates the perspective camera field of view based on the current aspect ratio and settings.
        /// </summary>
        /// <param name="targetAspect">The target aspect ratio to maintain.</param>
        /// <param name="currentAspect">The current screen aspect ratio.</param>
        private void UpdatePerspectiveCamera(float targetAspect, float currentAspect)
        {
            float baseFov = GetReferenceFov();
            float fitWidthFov = ConvertVerticalFovForConstantWidth(baseFov, targetAspect, currentAspect);
            float newFOV = baseFov;

            switch (scaleMode)
            {
                case ScaleMode.FitWidth:
                    newFOV = fitWidthFov;
                    break;
                case ScaleMode.FitHeight:
                    newFOV = baseFov;
                    break;
                case ScaleMode.FitBoth:
                    newFOV = useViewportRectInFitBoth ? baseFov : Mathf.Max(baseFov, fitWidthFov);
                    break;
                case ScaleMode.Manual:
                    return;
            }

            newFOV *= scaleMultiplier;
            newFOV = Mathf.Clamp(newFOV, minSize, maxSize);
            _camera.fieldOfView = newFOV;
        }

        private void EnsureInitialized()
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }

            if (!_referenceCaptured && _camera != null)
            {
                _referenceSize = _camera.orthographicSize;
                _referenceFov = _camera.fieldOfView;
                _referenceCaptured = true;
            }
        }

        private bool HasRuntimeDataChanged()
        {
            return _lastScreenWidth != Screen.width
                   || _lastScreenHeight != Screen.height
                   || _lastScaleMode != scaleMode
                   || _lastUseTargetResolution != useTargetResolution
                   || _lastTargetResolution != targetResolution
                   || !Mathf.Approximately(_lastScaleMultiplier, scaleMultiplier)
                   || _lastUseViewportRectInFitBoth != useViewportRectInFitBoth;
        }

        private void CacheCurrentState()
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            _lastScaleMode = scaleMode;
            _lastUseTargetResolution = useTargetResolution;
            _lastTargetResolution = targetResolution;
            _lastScaleMultiplier = scaleMultiplier;
            _lastUseViewportRectInFitBoth = useViewportRectInFitBoth;
        }

        private float GetTargetAspect()
        {
            if (!useTargetResolution)
            {
                return 16f / 9f;
            }

            float width = Mathf.Max(1f, targetResolution.x);
            float height = Mathf.Max(1f, targetResolution.y);
            return width / height;
        }

        private static float GetCurrentAspect()
        {
            float h = Mathf.Max(1f, Screen.height);
            return Screen.width / h;
        }

        private float GetReferenceSize()
        {
            if (_camera == null)
            {
                return 5f;
            }

            if (!_referenceCaptured)
            {
                EnsureInitialized();
            }

            return Mathf.Max(0.01f, _referenceSize);
        }

        private float GetReferenceFov()
        {
            if (_camera == null)
            {
                return 60f;
            }

            if (!_referenceCaptured)
            {
                EnsureInitialized();
            }

            return Mathf.Clamp(_referenceFov, 1f, 179f);
        }

        private void UpdateViewportRect(float targetAspect, float currentAspect)
        {
            if (scaleMode != ScaleMode.FitBoth || !useViewportRectInFitBoth)
            {
                _camera.rect = new Rect(0f, 0f, 1f, 1f);
                return;
            }

            if (Mathf.Approximately(currentAspect, targetAspect))
            {
                _camera.rect = new Rect(0f, 0f, 1f, 1f);
                return;
            }

            if (currentAspect > targetAspect)
            {
                float width = targetAspect / currentAspect;
                float x = (1f - width) * 0.5f;
                _camera.rect = new Rect(x, 0f, width, 1f);
            }
            else
            {
                float height = currentAspect / targetAspect;
                float y = (1f - height) * 0.5f;
                _camera.rect = new Rect(0f, y, 1f, height);
            }
        }

        private static float ConvertVerticalFovForConstantWidth(float referenceVerticalFov, float referenceAspect,
            float currentAspect)
        {
            float safeReferenceAspect = Mathf.Max(0.01f, referenceAspect);
            float safeCurrentAspect = Mathf.Max(0.01f, currentAspect);

            float refHalfVerticalRad = referenceVerticalFov * Mathf.Deg2Rad * 0.5f;
            float refHalfHorizontalRad = Mathf.Atan(Mathf.Tan(refHalfVerticalRad) * safeReferenceAspect);
            float newHalfVerticalRad = Mathf.Atan(Mathf.Tan(refHalfHorizontalRad) / safeCurrentAspect);
            return Mathf.Clamp(newHalfVerticalRad * 2f * Mathf.Rad2Deg, 1f, 179f);
        }
    }
}