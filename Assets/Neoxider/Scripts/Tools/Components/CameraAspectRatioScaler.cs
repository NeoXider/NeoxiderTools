using UnityEngine;

namespace Neo.Tools
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
    [NeoDoc("Tools/CameraAspectRatioScaler.md")]
    [CreateFromMenu("Neoxider/Tools/Camera/CameraAspectRatioScaler")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(CameraAspectRatioScaler))]
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

        [Header("Reference")]
        [Tooltip(
            "Orthographic size of the reference composition (at Target Resolution). Follows the Camera's Size whenever " +
            "it holds a value this scaler did not write; scaling always starts from this value, never from the " +
            "scaled camera.")]
        [SerializeField]
        private float _referenceSize;

        [Tooltip(
            "Vertical field of view of the reference composition. Follows the Camera's Field of View whenever it holds " +
            "a value this scaler did not write.")]
        [SerializeField]
        private float _referenceFov;

        // WHY: serialized so an Edit Mode preview that rewrites the camera is not captured again as the reference
        // after a domain reload or scene reopen (each capture of a scaled value compounded the scaling).
        [SerializeField] [HideInInspector] private bool _referenceCaptured;

        // WHY: the last camera size/FOV the reference already accounts for (captured or written by this scaler).
        // A camera value that differs was authored by someone else (Camera Inspector, code) and becomes the new
        // reference, so editing the Camera keeps working without re-capturing the scaler's own scaled output.
        [SerializeField] [HideInInspector] private float _observedCameraSize;
        [SerializeField] [HideInInspector] private float _observedCameraFov;

        private Camera _camera;
        private bool _runtimeReferenceSynced;
        private ScaleMode _lastScaleMode;
        private float _lastScaleMultiplier;
        private int _lastScreenHeight = -1;
        private int _lastScreenWidth = -1;
        private int _lastTargetWidth;
        private int _lastTargetHeight;
        private bool _lastOrthographic;
        private Vector2 _lastTargetResolution;
        private bool _lastUseTargetResolution;
        private bool _lastUseViewportRectInFitBoth;
        private float _lastReferenceSize;
        private float _lastReferenceFov;
        private float _lastMinSize;
        private float _lastMaxSize;

        /// <summary>Orthographic size of the reference composition; scaling starts from this value.</summary>
        public float ReferenceOrthographicSize
        {
            get => _referenceSize;
            set
            {
                EnsureInitialized();
                _referenceSize = Mathf.Max(0.01f, value);
                _referenceCaptured = true;
                if (_camera != null)
                {
                    // WHY: the explicit value wins over a camera edit made before it.
                    _observedCameraSize = _camera.orthographicSize;
                }
            }
        }

        /// <summary>Vertical field of view (degrees) of the reference composition; scaling starts from this value.</summary>
        public float ReferenceFieldOfView
        {
            get => _referenceFov;
            set
            {
                EnsureInitialized();
                _referenceFov = Mathf.Clamp(value, 1f, 179f);
                _referenceCaptured = true;
                if (_camera != null)
                {
                    _observedCameraFov = _camera.fieldOfView;
                }
            }
        }

        /// <summary>Scale mode applied on the next refresh.</summary>
        public ScaleMode Mode
        {
            get => scaleMode;
            set => scaleMode = value;
        }

        /// <summary>Reference resolution whose aspect the camera preserves.</summary>
        public Vector2 TargetResolution
        {
            get => targetResolution;
            set => targetResolution = new Vector2(Mathf.Max(1f, value.x), Mathf.Max(1f, value.y));
        }

        /// <summary>
        ///     When true the reference aspect comes from <see cref="TargetResolution" />; when false a 16:9 reference
        ///     is used.
        /// </summary>
        public bool UseTargetResolution
        {
            get => useTargetResolution;
            set => useTargetResolution = value;
        }

        /// <summary>Extra factor applied to the computed orthographic size or field of view (at least 0.01).</summary>
        public float ScaleMultiplier
        {
            get => scaleMultiplier;
            set => scaleMultiplier = Mathf.Max(0.01f, value);
        }

        /// <summary>
        ///     In <see cref="ScaleMode.FitBoth" />, letterbox or pillarbox through <c>Camera.rect</c> so the target
        ///     aspect is kept exactly. Cameras that render into a texture never get bars.
        /// </summary>
        public bool UseViewportRectInFitBoth
        {
            get => useViewportRectInFitBoth;
            set => useViewportRectInFitBoth = value;
        }

        /// <summary>
        ///     Lower clamp for the computed orthographic size, or the field of view of a perspective camera
        ///     (at least 0.01). Raising it above <see cref="MaxSize" /> raises <see cref="MaxSize" /> too.
        /// </summary>
        public float MinSize
        {
            get => minSize;
            set
            {
                minSize = Mathf.Max(0.01f, value);
                maxSize = Mathf.Max(minSize, maxSize);
            }
        }

        /// <summary>
        ///     Upper clamp for the computed orthographic size, or the field of view of a perspective camera; never
        ///     below <see cref="MinSize" />.
        /// </summary>
        public float MaxSize
        {
            get => maxSize;
            set => maxSize = Mathf.Max(minSize, value);
        }

        /// <summary>
        ///     Re-applies the scale during Play whenever the screen, the target texture or a setting changes. When
        ///     false, call <see cref="Refresh" /> after changing settings.
        /// </summary>
        public bool UpdateInRuntime
        {
            get => updateInRuntime;
            set => updateInRuntime = value;
        }

        /// <summary>Applies the scale as an Edit Mode preview on enable, validation and the Inspector buttons.</summary>
        public bool UpdateInEditor
        {
            get => updateInEditor;
            set => updateInEditor = value;
        }

        /// <summary>Restores the full-screen <c>Camera.rect</c> when the scaler is disabled.</summary>
        public bool ResetViewportRectOnDisable
        {
            get => resetViewportRectOnDisable;
            set => resetViewportRectOnDisable = value;
        }

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
            if (Application.isPlaying || updateInEditor)
            {
                ApplyCameraScale(true);
            }
        }

        /// <summary>
        ///     Updates the camera scale based on the current screen resolution and settings.
        /// </summary>
        private void Update()
        {
            // WHY: Only drive the camera continuously at runtime. In edit mode the camera is a
            // serialized sibling object, so writing its properties every frame would dirty the
            // scene whenever the Game/Scene-view aspect differs from the reference aspect and would
            // fight manual edits. Edit-mode preview is applied on demand instead (OnValidate / the
            // "Apply Camera Scale" button), gated by updateInEditor.
            if (Application.isPlaying && updateInRuntime)
            {
                ApplyCameraScale();
            }
        }

        private void OnEnable()
        {
            EnsureInitialized();

            // WHY: Avoid driving the camera on every edit-mode enable (scene load / domain reload),
            // which would otherwise dirty the scene. Runtime always applies; edit-mode preview is
            // opt-in via updateInEditor.
            if (!Application.isPlaying && !updateInEditor)
            {
                return;
            }

            ApplyCameraScale(true);
        }

        private void OnDisable()
        {
            if (_camera != null && resetViewportRectOnDisable && (Application.isPlaying || updateInEditor))
            {
                _camera.rect = new Rect(0f, 0f, 1f, 1f);
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
            if (_referenceCaptured)
            {
                _referenceSize = Mathf.Max(0.01f, _referenceSize);
                _referenceFov = Mathf.Clamp(_referenceFov, 1f, 179f);
            }

            // WHY: Apply on-demand only when explicitly opted in. This still writes the camera (and may
            // dirty the scene if the editor aspect differs from the reference), but it is a discrete
            // user-driven change rather than a continuous per-frame mutation.
            if (!Application.isPlaying && !updateInEditor)
            {
                return;
            }

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

        /// <summary>
        ///     Recalculates the camera projection and viewport now, even if nothing changed since the last update.
        /// </summary>
        public void Refresh()
        {
            EnsureInitialized();
            ApplyCameraScale(true);
        }

        /// <summary>
        ///     Takes the Camera's current orthographic size and field of view as the new reference composition.
        ///     Call it after authoring the camera for <see cref="TargetResolution" />, not while it is scaled.
        /// </summary>
        /// <remarks>
        ///     Editing the Camera's Size / Field of View in Edit Mode is picked up automatically on the next scaler
        ///     refresh; this call is only needed to adopt a value equal to the one the scaler wrote last.
        /// </remarks>
        [Button("Capture Reference From Camera")]
        public void CaptureReferenceFromCamera()
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }

            if (_camera == null)
            {
                return;
            }

            _referenceSize = Mathf.Max(0.01f, _camera.orthographicSize);
            _referenceFov = Mathf.Clamp(_camera.fieldOfView, 1f, 179f);
            _observedCameraSize = _camera.orthographicSize;
            _observedCameraFov = _camera.fieldOfView;
            _referenceCaptured = true;
        }

        private void Reset()
        {
            _referenceCaptured = false;
            EnsureInitialized();
        }

        private void ApplyCameraScale(bool force = false)
        {
            if (_camera == null)
            {
                return;
            }

            if (force && !Application.isPlaying)
            {
                AdoptCameraEdits();
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
                    newSize = useViewportRectInFitBoth && _camera.targetTexture == null
                        ? baseSize
                        : Mathf.Max(baseSize, fitWidthSize);
                    break;
                case ScaleMode.Manual:
                    return;
            }

            newSize *= scaleMultiplier;
            newSize = Mathf.Clamp(newSize, minSize, maxSize);
            _camera.orthographicSize = newSize;
            _observedCameraSize = _camera.orthographicSize;
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
                    newFOV = useViewportRectInFitBoth && _camera.targetTexture == null
                        ? baseFov
                        : Mathf.Max(baseFov, fitWidthFov);
                    break;
                case ScaleMode.Manual:
                    return;
            }

            newFOV *= scaleMultiplier;
            newFOV = Mathf.Clamp(newFOV, minSize, maxSize);
            _camera.fieldOfView = newFOV;
            // WHY: read back, a physical camera may store a slightly different FOV than the one assigned.
            _observedCameraFov = _camera.fieldOfView;
        }

        private void EnsureInitialized()
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }

            if (_camera == null)
            {
                return;
            }

            if (!_referenceCaptured)
            {
                CaptureReferenceFromCamera();
                return;
            }

            // WHY: at runtime the composition is read once per instance (like the old Awake capture), so gameplay
            // zoom written to the camera is not adopted; in Edit Mode every refresh re-reads Camera edits.
            if (!Application.isPlaying || !_runtimeReferenceSynced)
            {
                AdoptCameraEdits();
                _runtimeReferenceSynced = Application.isPlaying;
            }
        }

        /// <summary>
        ///     Adopts the camera's size/FOV as the reference when it differs from the last value the reference
        ///     accounts for (an authored edit, not the scaler's own output).
        /// </summary>
        private void AdoptCameraEdits()
        {
            if (!_referenceCaptured)
            {
                CaptureReferenceFromCamera();
                return;
            }

            if (_camera.orthographic)
            {
                float size = _camera.orthographicSize;
                if (!Mathf.Approximately(size, _observedCameraSize))
                {
                    _referenceSize = Mathf.Max(0.01f, size);
                    _observedCameraSize = size;
                }
            }
            else
            {
                float fov = _camera.fieldOfView;
                if (!Mathf.Approximately(fov, _observedCameraFov))
                {
                    _referenceFov = Mathf.Clamp(fov, 1f, 179f);
                    _observedCameraFov = fov;
                }
            }
        }

        private bool HasRuntimeDataChanged()
        {
            // WHY: runs every frame; read the target texture through one native call.
            RenderTexture target = _camera.targetTexture;
            bool hasTarget = target != null;
            return _lastScreenWidth != Screen.width
                   || _lastScreenHeight != Screen.height
                   || _lastTargetWidth != (hasTarget ? target.width : 0)
                   || _lastTargetHeight != (hasTarget ? target.height : 0)
                   || _lastOrthographic != _camera.orthographic
                   || _lastScaleMode != scaleMode
                   || _lastUseTargetResolution != useTargetResolution
                   || _lastTargetResolution != targetResolution
                   || !Mathf.Approximately(_lastScaleMultiplier, scaleMultiplier)
                   || _lastUseViewportRectInFitBoth != useViewportRectInFitBoth
                   || !Mathf.Approximately(_lastReferenceSize, _referenceSize)
                   || !Mathf.Approximately(_lastReferenceFov, _referenceFov)
                   || _lastMinSize != minSize
                   || _lastMaxSize != maxSize;
        }

        private void CacheCurrentState()
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            _lastTargetWidth = _camera.targetTexture != null ? _camera.targetTexture.width : 0;
            _lastTargetHeight = _camera.targetTexture != null ? _camera.targetTexture.height : 0;
            _lastOrthographic = _camera.orthographic;
            _lastScaleMode = scaleMode;
            _lastUseTargetResolution = useTargetResolution;
            _lastTargetResolution = targetResolution;
            _lastScaleMultiplier = scaleMultiplier;
            _lastUseViewportRectInFitBoth = useViewportRectInFitBoth;
            _lastReferenceSize = _referenceSize;
            _lastReferenceFov = _referenceFov;
            _lastMinSize = minSize;
            _lastMaxSize = maxSize;
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

        private float GetCurrentAspect()
        {
            RenderTexture target = _camera.targetTexture;
            float width = target != null ? target.width : Screen.width;
            float height = target != null ? target.height : Screen.height;
            return width / Mathf.Max(1f, height);
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
            if (scaleMode != ScaleMode.FitBoth || !useViewportRectInFitBoth || _camera.targetTexture != null)
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
