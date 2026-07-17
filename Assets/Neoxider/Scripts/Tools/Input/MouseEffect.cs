using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Visual mouse effects driven by MouseInputManager.
    ///     - TrailRenderer or follower GameObject tracks the cursor; position is updated every <see cref="holdInterval" />
    ///     seconds.
    ///     - Prefab spawns on chosen trigger (Press / Hold / Release / Click) and, optionally, periodically while holding.
    /// </summary>
    [NeoDoc("Tools/Input/MouseEffect.md")]
    [CreateFromMenu("Neoxider/Tools/Input/MouseEffect")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(MouseEffect))]
    public class MouseEffect : MonoBehaviour
    {
        public enum SpawnTrigger
        {
            Press,
            Hold,
            Release,
            Click
        }

        [Header("Interactivity")] [Tooltip("If disabled, component ignores mouse events and does nothing")]
        public bool interactable = true;

        [Tooltip("Disable Trail/Follow object on mouse release")]
        public bool disableOnRelease = true;

        [Header("Effect Sources")] [Tooltip("TrailRenderer that follows the cursor (optional)")]
        public TrailRenderer trail;

        [Tooltip("GameObject that follows the cursor (optional)")]
        public GameObject followObject;

        [Tooltip("Prefab to spawn (optional)")]
        public GameObject spawnPrefab;

        [Header("Prefab Spawn Settings")] [Tooltip("When to spawn the prefab")]
        public SpawnTrigger spawnTrigger = SpawnTrigger.Press;

        [Tooltip("Spawn prefab repeatedly while holding")]
        public bool spawnDuringHold = true;

        [Tooltip("Interval in seconds for Hold spawning")]
        public float holdInterval = 0.05f;

        [Tooltip("Lifetime in seconds for spawned prefabs; 0 = don't destroy")]
        public float spawnLifetime;

        [Tooltip("Interval in seconds for following updates")]
        public float followInterval = 0.016f;

        [Header("Follow Settings")] [Tooltip("Depth (world Z) used to place the trail / follower")]
        public float followDepth = 10f;

        [Tooltip(
            "Camera used to convert screen cursor position to world position. If empty, MouseInputManager.TargetCamera is used before the optional MainCamera fallback.")]
        [SerializeField]
        private Camera targetCamera;

        [Tooltip("Resolve Camera.main only when Target Camera and MouseInputManager.TargetCamera are empty.")]
        [SerializeField]
        private bool useMainCameraFallback = true;

        [Tooltip("Seconds between Camera.main fallback attempts while no camera is available.")]
        [SerializeField]
        [Min(0f)]
        private float cameraFallbackRetryInterval = 1f;

        [Header("Spawn Parent (optional)")]
        [Tooltip("Parent for spawned prefabs; if null, spawns under this GameObject")]
        public Transform spawnParent;

        [Header("Diagnostics")] [SerializeField]
        private bool _logMissingManagerWarning;

        [SerializeField] private bool _logMissingCameraWarning;

        public UnityEvent onStartFollow;
        public UnityEvent onStopFollow;
        public UnityEvent onSpawn;

        private Camera _cam;
        private bool _holdSingleSpawned;
        private bool _isFollowing;
        private float _lastFollowUpdate;

        private float _lastHoldSpawn;
        private float _nextCameraFallbackTime;
        private bool _missingCameraWarningShown;
        private bool _missingManagerWarningShown;
        private MouseInputManager _mim;

        public Camera TargetCamera => targetCamera;

        public void SetTargetCamera(Camera camera)
        {
            targetCamera = camera;
            _cam = camera;
            _nextCameraFallbackTime = 0f;
            _missingCameraWarningShown = false;
        }

        private void Awake()
        {
            ResolveTargetCamera(true);
            if (trail)
            {
                trail.enabled = true; // WHY: keep component enabled, control emission instead
#if UNITY_2020_2_OR_NEWER
                trail.emitting = false;
#else
                trail.enabled = false;
#endif
            }

            if (followObject)
            {
                followObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (!interactable)
            {
                return;
            }

            if (!_isFollowing)
            {
                return;
            }

            if (Time.time - _lastFollowUpdate < followInterval)
            {
                return;
            }

            if (!TryGetCursorWorld(out Vector3 wp))
            {
                return;
            }

            if (trail)
            {
                trail.transform.position = wp;
            }

            if (followObject && followObject.activeSelf)
            {
                followObject.transform.position = wp;
            }

            _lastFollowUpdate = Time.time;
        }

        private void OnEnable()
        {
            _mim = MouseInputManager.I;
            ResolveTargetCamera(true);

            if (_mim != null)
            {
                _mim.OnPress += OnPress;
                _mim.OnRelease += OnRelease;
                _mim.OnClick += OnClick;
                _mim.OnHold += OnHold;
            }
            else
            {
                if (_logMissingManagerWarning && !_missingManagerWarningShown)
                {
                    _missingManagerWarningShown = true;
                    NeoDiagnostics.LogWarning("[MouseEffect] MouseInputManager is missing.", this);
                }
            }
        }

        private void OnDisable()
        {
            if (_mim == null)
            {
                return;
            }

            _mim.OnPress -= OnPress;
            _mim.OnRelease -= OnRelease;
            _mim.OnClick -= OnClick;
            _mim.OnHold -= OnHold;
        }

        private void OnDestroy()
        {
            if (_mim == null)
            {
                return;
            }

            _mim.OnPress -= OnPress;
            _mim.OnRelease -= OnRelease;
            _mim.OnClick -= OnClick;
            _mim.OnHold -= OnHold;
        }

        private bool TryGetCursorWorld(out Vector3 worldPos)
        {
            worldPos = Vector3.zero;
            Camera camera = ResolveTargetCamera(false);
            if (camera == null)
            {
                return false;
            }

            Vector3 mp = Input.mousePosition;
            mp.x = Mathf.Clamp(mp.x, 0f, Screen.width);
            mp.y = Mathf.Clamp(mp.y, 0f, Screen.height);

            worldPos = camera.ScreenToWorldPoint(new Vector3(mp.x, mp.y, followDepth));
            return true;
        }

        private Camera ResolveTargetCamera(bool forceFallback)
        {
            if (targetCamera != null)
            {
                _cam = targetCamera;
                return _cam;
            }

            if (_mim != null && _mim.TargetCamera != null)
            {
                _cam = _mim.TargetCamera;
                return _cam;
            }

            if (_cam != null)
            {
                return _cam;
            }

            if (useMainCameraFallback)
            {
                float now = Time.realtimeSinceStartup;
                if (forceFallback || cameraFallbackRetryInterval <= 0f || now >= _nextCameraFallbackTime)
                {
                    _cam = Camera.main;
                    _nextCameraFallbackTime = now + Mathf.Max(0f, cameraFallbackRetryInterval);
                }
            }

            if (_cam == null && _logMissingCameraWarning && !_missingCameraWarningShown)
            {
                _missingCameraWarningShown = true;
                NeoDiagnostics.LogWarningThrottled(
#if UNITY_6000_5_OR_NEWER
                    $"{nameof(MouseEffect)}.{GetEntityId()}.MissingCamera",
#else
                    $"{nameof(MouseEffect)}.{GetInstanceID()}.MissingCamera",
#endif
                    "[MouseEffect] Target Camera is not assigned and no fallback camera is available.",
                    this,
                    5f);
            }

            return _cam;
        }

        private void OnPress(MouseInputManager.MouseEventData data)
        {
            if (!interactable)
            {
                return;
            }

            if (trail)
            {
#if UNITY_2020_2_OR_NEWER
                trail.Clear();
                trail.transform.position = data.WorldPosition;
                trail.emitting = true;
#else
                trail.transform.position = data.WorldPosition;
                trail.enabled = true;
#endif
            }

            if (followObject)
            {
                followObject.SetActive(false);
                followObject.transform.position = data.WorldPosition;
                followObject.SetActive(true);
            }

            if (spawnPrefab && spawnTrigger == SpawnTrigger.Press)
            {
                SpawnAt(data.WorldPosition);
            }

            _lastHoldSpawn = Time.time;
            _lastFollowUpdate = Time.time;
            _holdSingleSpawned = false;
            _isFollowing = true;
            onStartFollow?.Invoke();
        }

        private void OnRelease(MouseInputManager.MouseEventData data)
        {
            if (!interactable)
            {
                return;
            }

            if (spawnPrefab && spawnTrigger == SpawnTrigger.Release)
            {
                SpawnAt(data.WorldPosition);
            }

            if (disableOnRelease)
            {
                if (trail)
                {
#if UNITY_2020_2_OR_NEWER
                    trail.emitting = false;
#else
                    trail.enabled = false;
#endif
                }

                if (followObject)
                {
                    followObject.SetActive(false);
                }
            }

            _isFollowing = false;
            _holdSingleSpawned = false;
            onStopFollow?.Invoke();
        }

        private void OnClick(MouseInputManager.MouseEventData data)
        {
            if (!interactable)
            {
                return;
            }

            if (spawnPrefab && spawnTrigger == SpawnTrigger.Click)
            {
                SpawnAt(data.WorldPosition);
            }
        }

        private void OnHold(MouseInputManager.MouseEventData data)
        {
            if (!interactable)
            {
                return;
            }

            if (!spawnPrefab)
            {
                return;
            }

            if (spawnTrigger == SpawnTrigger.Hold && !_holdSingleSpawned)
            {
                SpawnAt(data.WorldPosition);
                _holdSingleSpawned = true;
            }

            if (!spawnDuringHold)
            {
                return;
            }

            if (Time.time - _lastHoldSpawn < holdInterval)
            {
                return;
            }

            SpawnAt(data.WorldPosition);
            _lastHoldSpawn = Time.time;
        }

        private void SpawnAt(Vector3 position)
        {
            if (!spawnPrefab)
            {
                return;
            }

            Transform parent = spawnParent != null ? spawnParent : transform;
            GameObject instance = Instantiate(spawnPrefab, position, Quaternion.identity, parent);
            if (spawnLifetime > 0f)
            {
                Destroy(instance, spawnLifetime);
            }

            onSpawn?.Invoke();
        }
    }
}
