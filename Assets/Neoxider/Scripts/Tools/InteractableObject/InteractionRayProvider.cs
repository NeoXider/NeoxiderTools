using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Attach to a Camera to control how <see cref="InteractiveObject" /> generates
    ///     hover / click raycasts.
    ///     <para>
    ///         <b>Mouse</b> — raycast from mouse / touch position (desktop default).<br />
    ///         <b>ScreenCenter</b> — raycast from the center of the screen (mobile crosshair).<br />
    ///         <b>Both</b> — screen-center ray is used for hover, mouse click still fires on tap
    ///         (default mode: works everywhere).
    ///     </para>
    /// </summary>
    [CreateFromMenu("Neoxider/Tools/InteractionRayProvider")]
    [AddComponentMenu("Neoxider/Tools/" + nameof(InteractionRayProvider))]
    [RequireComponent(typeof(Camera))]
    [NeoDoc("Tools/InteractableObject/InteractionRayProvider.md")]
    public class InteractionRayProvider : MonoBehaviour
    {
        /// <summary>
        ///     How the interaction ray is generated.
        /// </summary>
        public enum RayMode
        {
            /// <summary>Ray from mouse / touch position.</summary>
            Mouse,

            /// <summary>Ray from the center of the screen (fixed crosshair).</summary>
            ScreenCenter,

            /// <summary>Screen center for hover detection, mouse position for click confirmation (default).</summary>
            Both
        }

        [Tooltip("How the interaction ray is generated.\n" +
                 "Mouse — from cursor / touch position.\n" +
                 "ScreenCenter — from the center of the screen (crosshair).\n" +
                 "Both — screen center for hover, mouse for click.")]
        [SerializeField]
        private RayMode _rayMode = RayMode.Both;

        private Camera _camera;

        /// <summary>Current ray mode.</summary>
        public RayMode Mode
        {
            get => _rayMode;
            set
            {
                if (_rayMode != value)
                {
                    _rayMode = value;
                    InvalidateQueries();
                }
            }
        }

        /// <summary>Cached camera reference.</summary>
        public Camera Camera => _camera != null ? _camera : _camera = GetComponent<Camera>();

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void OnDisable()
        {
            InvalidateQueries();
        }

        /// <summary>Ends the shared raw-hit epoch after an application-controlled physics/world change.</summary>
        public static void InvalidateQueries()
        {
            InteractionPhysicsQueries.Invalidate();
        }

        /// <summary>
        ///     Returns the ray used for hover detection.
        /// </summary>
        public bool TryGetHoverRay(out Ray ray)
        {
            return TryCreateRay(Camera, UseScreenCenterForHover, out ray);
        }

        /// <summary>
        ///     Returns the ray used for click / interaction confirmation.
        /// </summary>
        public bool TryGetClickRay(out Ray ray)
        {
            return TryCreateRay(Camera, UseScreenCenterForClick, out ray);
        }

        internal static bool TryCreateRay(Camera camera, bool useScreenCenter, out Ray ray)
        {
            ray = default;
            if (camera == null || camera.pixelWidth <= 0 || camera.pixelHeight <= 0)
            {
                return false;
            }

            if (useScreenCenter)
            {
                // Viewport coordinates include the camera's pixelRect offset (split screen).
                ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                return true;
            }

            if (!MouseInputCompat.TryGetPosition(out Vector3 mousePosition) ||
                !float.IsFinite(mousePosition.x) || !float.IsFinite(mousePosition.y) ||
                !camera.pixelRect.Contains(new Vector2(mousePosition.x, mousePosition.y)))
            {
                return false;
            }

            ray = camera.ScreenPointToRay(mousePosition);
            return true;
        }

        /// <summary>
        ///     Whether hover should use screen center ray.
        /// </summary>
        public bool UseScreenCenterForHover =>
            _rayMode == RayMode.ScreenCenter || _rayMode == RayMode.Both;

        /// <summary>
        ///     Whether click should use screen center ray.
        /// </summary>
        public bool UseScreenCenterForClick =>
            _rayMode == RayMode.ScreenCenter;

        private static InteractionRayProvider _cachedInstance;

        /// <summary>
        ///     Finds or creates the <see cref="InteractionRayProvider" /> on <c>Camera.main</c>.
        ///     If Camera.main exists but has no InteractionRayProvider, one is automatically added
        ///     with the default <see cref="RayMode.Both" /> mode. Returns null for a disabled provider.
        ///     The cached provider is reused only while it belongs to the current main camera.
        /// </summary>
        public static InteractionRayProvider FindOnMainCamera()
        {
            Camera main = InteractionCameraResolver.Resolve(null, false);
            if (main == null)
            {
                if (_cachedInstance != null)
                {
                    InvalidateQueries();
                }

                _cachedInstance = null;
                return null;
            }

            if (_cachedInstance != null && _cachedInstance.gameObject == main.gameObject &&
                _cachedInstance.isActiveAndEnabled)
            {
                return _cachedInstance;
            }

            if (_cachedInstance != null && _cachedInstance.gameObject != main.gameObject)
            {
                InvalidateQueries();
            }

            _cachedInstance = main.GetComponent<InteractionRayProvider>();
            if (_cachedInstance == null)
            {
                _cachedInstance = main.gameObject.AddComponent<InteractionRayProvider>();
            }

            return _cachedInstance.isActiveAndEnabled ? _cachedInstance : null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _cachedInstance = null;
        }
    }
}
