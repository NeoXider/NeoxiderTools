using CMF;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Hands camera work over to an external camera system — Cinemachine in particular — while keeping the
    ///     character's movement camera-relative.
    /// </summary>
    /// <remarks>
    ///     CMF expects <c>AdvancedWalkerController.cameraTransform</c> to be the transform the player actually looks
    ///     through: it projects that transform's forward/right onto the ground plane to build the movement basis. With
    ///     Cinemachine the rendering camera is the Brain's camera, not the virtual camera and not CMF's own rig — so
    ///     pointing the controller at anything else makes "forward" drift away from what the player sees.
    ///     <para>
    ///         Typical Cinemachine setup: keep CMF's <c>CameraController</c> + <see cref="NeoCameraInput" /> on a pivot
    ///         object as the aim source, assign that pivot as the Cinemachine camera's Follow/Look At target, and put
    ///         this component on the character with <see cref="LiveCamera" /> left empty so it binds to the Brain's
    ///         camera automatically.
    ///     </para>
    ///     <para>
    ///         Deliberately free of any Cinemachine API reference, so it compiles whether Cinemachine is installed or
    ///         not, works with both Cinemachine 2 and 3, and serves any other external camera driver just as well.
    ///     </para>
    /// </remarks>
    [NeoDoc("Tools/Move/CharacterController/NeoCharacterCameraBridge.md")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(NeoCharacterCameraBridge))]
    [DefaultExecutionOrder(-50)]
    public class NeoCharacterCameraBridge : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Controller whose movement basis follows the live camera. Auto-found on this GameObject.")]
        [SerializeField]
        private AdvancedWalkerController _controller;

        [Tooltip("The camera the player actually renders through (the Cinemachine Brain's camera). Empty = Camera.main.")]
        [SerializeField]
        private Camera _liveCamera;

        [Header("Behaviour")]
        [Tooltip("Re-resolve the live camera every frame. Needed when cameras are swapped at runtime (split screen, cutscenes); costs a Camera.main lookup per frame when no camera is assigned.")]
        [SerializeField]
        private bool _trackCameraChanges;

        [Tooltip("Log a warning when no live camera could be resolved.")] [SerializeField]
        private bool _logSetupWarnings = true;

        private bool _missingCameraWarningShown;

        /// <summary>
        ///     Gets the camera the movement basis follows. Null until one is resolved.
        /// </summary>
        public Camera LiveCamera => _liveCamera;

        private void Awake()
        {
            if (_controller == null)
            {
                _controller = GetComponent<AdvancedWalkerController>();
            }
        }

        private void OnEnable()
        {
            Bind();
        }

        private void LateUpdate()
        {
            if (_trackCameraChanges)
            {
                Bind();
            }
        }

        /// <summary>
        ///     Re-resolves the live camera and re-points the controller's movement basis at it. Call after swapping
        ///     cameras at runtime when <c>Track Camera Changes</c> is off.
        /// </summary>
        public void Bind()
        {
            if (_controller == null)
            {
                return;
            }

            Camera resolved = _liveCamera != null ? _liveCamera : Camera.main;
            if (resolved == null)
            {
                WarnMissingCameraOnce();
                return;
            }

            _controller.cameraTransform = resolved.transform;
        }

        /// <summary>
        ///     Assigns the live camera explicitly and re-binds immediately.
        /// </summary>
        public void SetLiveCamera(Camera camera)
        {
            _liveCamera = camera;
            _missingCameraWarningShown = false;
            Bind();
        }

        private void WarnMissingCameraOnce()
        {
            if (_missingCameraWarningShown || !_logSetupWarnings)
            {
                return;
            }

            _missingCameraWarningShown = true;
            NeoDiagnostics.LogWarning(
                "[NeoCharacterCameraBridge] No live camera found. Assign one, or tag your Cinemachine Brain camera as MainCamera.",
                this);
        }
    }
}
