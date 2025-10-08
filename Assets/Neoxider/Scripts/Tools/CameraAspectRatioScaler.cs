
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
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
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
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

        private Camera _camera;
        private float _defaultFOV;
        private float _defaultSize;

        /// <summary>
        ///     Initializes the component and stores the camera's default values.
        /// </summary>
        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera.orthographic)
                _defaultSize = _camera.orthographicSize;
            else
                _defaultFOV = _camera.fieldOfView;
        }

        /// <summary>
        ///     Applies the initial camera scaling when the game starts.
        /// </summary>
        private void Start()
        {
            UpdateCameraScale();
        }

        /// <summary>
        ///     Updates the camera scale based on the current screen resolution and settings.
        /// </summary>
        private void Update()
        {
            if ((Application.isPlaying && updateInRuntime) || (!Application.isPlaying && updateInEditor))
                UpdateCameraScale();
        }

        /// <summary>
        ///     Validates and updates camera settings when values are modified in the Unity Inspector.
        /// </summary>
        private void OnValidate()
        {
            if (_camera == null) _camera = GetComponent<Camera>();

            UpdateCameraScale();
        }

        /// <summary>
        ///     Calculates and applies the appropriate camera scale based on current settings.
        /// </summary>
        [Button]
        private void UpdateCameraScale()
        {
            if (_camera == null) return;

            var targetAspect = useTargetResolution ? targetResolution.x / targetResolution.y : 16f / 9f;
            var currentAspect = (float)Screen.width / Screen.height;

            if (_camera.orthographic)
                UpdateOrthographicCamera(targetAspect, currentAspect);
            else
                UpdatePerspectiveCamera(targetAspect, currentAspect);
        }

        /// <summary>
        ///     Updates the orthographic camera size based on the current aspect ratio and settings.
        /// </summary>
        /// <param name="targetAspect">The target aspect ratio to maintain.</param>
        /// <param name="currentAspect">The current screen aspect ratio.</param>
        private void UpdateOrthographicCamera(float targetAspect, float currentAspect)
        {
            var newSize = _defaultSize;

            switch (scaleMode)
            {
                case ScaleMode.FitWidth:
                    newSize = _defaultSize * (targetAspect / currentAspect);
                    break;
                case ScaleMode.FitHeight:
                    newSize = _defaultSize;
                    break;
                case ScaleMode.FitBoth:
                    newSize = _defaultSize * Mathf.Max(1f, targetAspect / currentAspect);
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
            var newFOV = _defaultFOV;

            switch (scaleMode)
            {
                case ScaleMode.FitWidth:
                    newFOV = _defaultFOV * (currentAspect / targetAspect);
                    break;
                case ScaleMode.FitHeight:
                    newFOV = _defaultFOV;
                    break;
                case ScaleMode.FitBoth:
                    newFOV = _defaultFOV * Mathf.Min(1f, currentAspect / targetAspect);
                    break;
                case ScaleMode.Manual:
                    return;
            }

            newFOV *= scaleMultiplier;
            newFOV = Mathf.Clamp(newFOV, minSize, maxSize);
            _camera.fieldOfView = newFOV;
        }
    }
}