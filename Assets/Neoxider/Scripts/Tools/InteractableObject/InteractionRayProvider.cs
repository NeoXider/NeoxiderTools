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
    [AddComponentMenu("Neoxider/Tools/" + nameof(InteractionRayProvider))]
    [RequireComponent(typeof(Camera))]
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
            set => _rayMode = value;
        }

        /// <summary>Cached camera reference.</summary>
        public Camera Camera => _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        /// <summary>
        ///     Returns the ray used for hover detection.
        /// </summary>
        public bool TryGetHoverRay(out Ray ray)
        {
            ray = default;
            if (_camera == null)
            {
                return false;
            }

            if (_rayMode == RayMode.Mouse)
            {
                if (!MouseInputCompat.TryGetPosition(out Vector3 mousePos))
                {
                    return false;
                }

                ray = _camera.ScreenPointToRay(mousePos);
                return true;
            }

            // ScreenCenter or Both → hover always from center
            ray = _camera.ScreenPointToRay(
                new Vector3(_camera.pixelWidth * 0.5f, _camera.pixelHeight * 0.5f, 0f));
            return true;
        }

        /// <summary>
        ///     Returns the ray used for click / interaction confirmation.
        /// </summary>
        public bool TryGetClickRay(out Ray ray)
        {
            ray = default;
            if (_camera == null)
            {
                return false;
            }

            if (_rayMode == RayMode.ScreenCenter)
            {
                // Pure crosshair mode — click also from center
                ray = _camera.ScreenPointToRay(
                    new Vector3(_camera.pixelWidth * 0.5f, _camera.pixelHeight * 0.5f, 0f));
                return true;
            }

            // Mouse or Both → click from mouse/touch position
            if (!MouseInputCompat.TryGetPosition(out Vector3 mousePos))
            {
                return false;
            }

            ray = _camera.ScreenPointToRay(mousePos);
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

        // ── Singleton-like fast access ──
        private static InteractionRayProvider _cachedInstance;

        /// <summary>
        ///     Finds or creates the <see cref="InteractionRayProvider" /> on <c>Camera.main</c>.
        ///     If Camera.main exists but has no InteractionRayProvider, one is automatically added
        ///     with the default <see cref="RayMode.Both" /> mode. Result is cached for performance.
        /// </summary>
        public static InteractionRayProvider FindOnMainCamera()
        {
            if (_cachedInstance != null && _cachedInstance.isActiveAndEnabled)
            {
                return _cachedInstance;
            }

            Camera main = Camera.main;
            if (main == null)
            {
                _cachedInstance = null;
                return null;
            }

            _cachedInstance = main.GetComponent<InteractionRayProvider>();
            if (_cachedInstance == null)
            {
                _cachedInstance = main.gameObject.AddComponent<InteractionRayProvider>();
            }

            return _cachedInstance;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _cachedInstance = null;
        }
    }
}
