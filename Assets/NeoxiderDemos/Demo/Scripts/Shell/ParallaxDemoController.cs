using System.Globalization;
using Neo.Parallax;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Samples
{
    /// <summary>
    ///     Controller of the authored <b>Neo.Parallax</b> demo scene. The landscape bands - each a real
    ///     <see cref="ParallaxLayer" /> with its own multiplier, tiling and sorting - the UI and the info card are
    ///     saved in ParallaxDemo.unity. This component only moves the camera (auto pan,
    ///     reverse, jumps) so the layers have something to react to, and mirrors the camera position into the
    ///     authored label. It builds nothing at runtime.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Parallax Demo")]
    public sealed class ParallaxDemoController : MonoBehaviour
    {
        [Header("Scene")]
        [Tooltip("Camera the ParallaxLayers are bound to. Falls back to Camera.main when empty.")]
        [SerializeField] private Camera _camera;

        [Tooltip("Authored layers (for the log and the C# reference); the controller never changes their settings.")]
        [SerializeField] private ParallaxLayer[] _layers = new ParallaxLayer[0];

        [Header("Camera pan")]
        [Tooltip("Pan speed in world units per second.")]
        [SerializeField] [Min(0f)] private float _panSpeed = 1.5f;

        [SerializeField] private bool _autoPan = true;

        [Header("UI")]
        [SerializeField] private TMP_Text _cameraX;
        [SerializeField] private Slider _speedSlider;
        [SerializeField] private Toggle _autoPanToggle;
        [SerializeField] private DemoLogFeed _log;

        private Transform _cameraTransform;
        private int _direction = 1;
        private float _startX;
        private int _shownTenths = int.MinValue;
        private bool _warnedUnbuilt;

        /// <summary>True when the authored scene references are assigned.</summary>
        public bool IsSceneBuilt => _camera != null && _layers != null && _layers.Length > 0 && _cameraX != null &&
                                    _log != null;

        /// <summary>Authored parallax layers.</summary>
        public ParallaxLayer[] Layers => _layers;

        /// <summary>Current pan speed (world units per second).</summary>
        public float PanSpeed => _panSpeed;

        /// <summary>+1 when the camera pans right, -1 when it pans left.</summary>
        public int Direction => _direction;

        private void Start()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }

            _cameraTransform = _camera != null ? _camera.transform : null;
            _startX = _cameraTransform != null ? _cameraTransform.position.x : 0f;
            if (_speedSlider != null)
            {
                _speedSlider.SetValueWithoutNotify(_panSpeed);
            }

            if (_autoPanToggle != null)
            {
                _autoPanToggle.SetIsOnWithoutNotify(_autoPan);
            }

            RefreshCameraLabel();
            Log(_layers != null ? _layers.Length + " ParallaxLayers follow '" + (_camera != null ? _camera.name : "no camera") +
                                  "' - select a band under [Demo] World" : "No layers assigned");
        }

        private void Update()
        {
            if (_cameraTransform == null || !_autoPan || _panSpeed <= 0f)
            {
                return;
            }

            // WHY: endless pan; every ParallaxLayer reads the camera delta in LateUpdate and recycles its tiles.
            Vector3 position = _cameraTransform.position;
            position.x += _direction * _panSpeed * Time.deltaTime;
            _cameraTransform.position = position;
            RefreshCameraLabel();
        }

        /// <summary>Slider target: pan speed in world units per second.</summary>
        public void SetPanSpeed(float unitsPerSecond)
        {
            _panSpeed = Mathf.Max(0f, unitsPerSecond);
        }

        /// <summary>Toggle target: starts or stops the automatic pan.</summary>
        public void SetAutoPan(bool enabled)
        {
            _autoPan = enabled;
            Log(enabled ? "Auto pan on" : "Auto pan off - the clouds keep drifting (scrollSpeed)");
        }

        /// <summary>Button target: flips the pan direction.</summary>
        public void Reverse()
        {
            _direction = -_direction;
            Log(_direction > 0 ? "Pan -> right" : "Pan <- left");
        }

        /// <summary>Button target: teleports the camera; every layer re-centres its tile pool in one step.</summary>
        public void Jump(float distance)
        {
            if (_cameraTransform == null)
            {
                return;
            }

            Vector3 position = _cameraTransform.position;
            position.x += distance;
            _cameraTransform.position = position;
            RefreshCameraLabel();
            Log("Camera jumped " + distance.ToString("+0;-0", CultureInfo.InvariantCulture) +
                " units - tiles recycle seamlessly");
        }

        /// <summary>Button target: returns the camera to where it started.</summary>
        public void ResetCamera()
        {
            if (_cameraTransform == null)
            {
                return;
            }

            Vector3 position = _cameraTransform.position;
            position.x = _startX;
            _cameraTransform.position = position;
            RefreshCameraLabel();
            Log("Camera back at the start");
        }

        private void RefreshCameraLabel()
        {
            if (_cameraX == null || _cameraTransform == null)
            {
                return;
            }

            float x = _cameraTransform.position.x;
            int tenths = Mathf.RoundToInt(x * 10f);
            if (tenths == _shownTenths)
            {
                return;
            }

            _shownTenths = tenths;
            // WHY SetText with an argument: formats into TMP's own buffer without allocating a string per frame.
            _cameraX.SetText("{0:1}", x);
        }

        private void Log(string message)
        {
            if (_log != null)
            {
                _log.Log(message);
            }
        }

        private void WarnUnbuilt()
        {
            if (_warnedUnbuilt)
            {
                return;
            }

            _warnedUnbuilt = true;
            global::Neo.Demo.SampleDiagnostics.Warn(
                "[ParallaxDemoController] The scene is not authored. Re-import the scene from the Demo download.",
                this);
        }
    }
}
