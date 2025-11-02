using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Visual mouse effects driven by MouseInputManager.
    ///     • TrailRenderer or follower GameObject tracks the cursor; position is updated every <see cref="holdInterval" />
    ///     seconds.
    ///     • Prefab spawns on chosen trigger (Press / Hold / Release / Click) and, optionally, periodically while holding.
    /// </summary>
    public class MouseEffect : MonoBehaviour
    {
        public enum SpawnTrigger
        {
            Press,
            Hold,
            Release,
            Click
        }

        [Header("Interactivity")]
        [Tooltip("If disabled, component ignores mouse events and does nothing")] public bool interactable = true;
        [Tooltip("Disable Trail/Follow object on mouse release")] public bool disableOnRelease = true;

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

        [Tooltip("Interval in seconds for Hold spawning")] public float holdInterval = 0.05f;
        [Tooltip("Lifetime in seconds for spawned prefabs; 0 = don't destroy")] public float spawnLifetime = 0f;

        [Tooltip("Interval in seconds for following updates")] public float followInterval = 0.016f;

        [Header("Follow Settings")] [Tooltip("Depth (world Z) used to place the trail / follower")]
        public float followDepth = 10f;

        [Header("Spawn Parent (optional)")]
        [Tooltip("Parent for spawned prefabs; if null, spawns under this GameObject")] public Transform spawnParent;

        [Header("Events")] public UnityEvent onStartFollow; public UnityEvent onStopFollow; public UnityEvent onSpawn;

        private Camera _cam;
        private bool _holdSingleSpawned;
        private bool _isFollowing;
        private float _lastFollowUpdate;

        private float _lastHoldSpawn;
        private MouseInputManager _mim;

        private void Awake()
        {
            _cam = Camera.main;
            if (trail)
            {
                trail.enabled = true; // keep component enabled, control emission instead
#if UNITY_2020_2_OR_NEWER
                trail.emitting = false;
#else
                trail.enabled = false;
#endif
            }
            if (followObject) followObject.SetActive(false);
        }

        private void OnEnable()
        {
            _cam = _cam != null ? _cam : Camera.main;
            _mim = MouseInputManager.I;

            if (_mim != null)
            {
                _mim.OnPress += OnPress;
                _mim.OnRelease += OnRelease;
                _mim.OnClick += OnClick;
                _mim.OnHold += OnHold;
            }
            else
            {
                Debug.LogWarning("MouseInputManager is null");
            }
        }

        private void OnDisable()
        {
            if (_mim == null) return;
            _mim.OnPress -= OnPress;
            _mim.OnRelease -= OnRelease;
            _mim.OnClick -= OnClick;
            _mim.OnHold -= OnHold;
        }

        private void Update()
        {
            if (!interactable) return;
            if (!_isFollowing) return;
            if (Time.time - _lastFollowUpdate < followInterval) return;
            if (!TryGetCursorWorld(out var wp)) return;

            if (trail) trail.transform.position = wp;
            if (followObject && followObject.activeSelf) followObject.transform.position = wp;

            _lastFollowUpdate = Time.time;
        }

        private void OnDestroy()
        {
            if (_mim == null) return;
            _mim.OnPress -= OnPress;
            _mim.OnRelease -= OnRelease;
            _mim.OnClick -= OnClick;
            _mim.OnHold -= OnHold;
        }

        private bool TryGetCursorWorld(out Vector3 worldPos)
        {
            worldPos = Vector3.zero;
            if (_cam == null) return false;

            var mp = Input.mousePosition;
            mp.x = Mathf.Clamp(mp.x, 0f, Screen.width);
            mp.y = Mathf.Clamp(mp.y, 0f, Screen.height);

            worldPos = _cam.ScreenToWorldPoint(new Vector3(mp.x, mp.y, followDepth));
            return true;
        }

        /* ────────────── event handlers ────────────── */

        private void OnPress(MouseInputManager.MouseEventData data)
        {
            if (!interactable) return;
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
                SpawnAt(data.WorldPosition);

            _lastHoldSpawn = Time.time;
            _lastFollowUpdate = Time.time;
            _holdSingleSpawned = false;
            _isFollowing = true;
            onStartFollow?.Invoke();
        }

        private void OnRelease(MouseInputManager.MouseEventData data)
        {
            if (!interactable) return;
            if (spawnPrefab && spawnTrigger == SpawnTrigger.Release)
                SpawnAt(data.WorldPosition);

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
                if (followObject) followObject.SetActive(false);
            }

            _isFollowing = false;
            _holdSingleSpawned = false;
            onStopFollow?.Invoke();
        }

        private void OnClick(MouseInputManager.MouseEventData data)
        {
            if (!interactable) return;
            if (spawnPrefab && spawnTrigger == SpawnTrigger.Click)
                SpawnAt(data.WorldPosition);
        }

        private void OnHold(MouseInputManager.MouseEventData data)
        {
            if (!interactable) return;
            if (!spawnPrefab) return;

            /* single-shot spawn on first Hold frame */
            if (spawnTrigger == SpawnTrigger.Hold && !_holdSingleSpawned)
            {
                SpawnAt(data.WorldPosition);
                _holdSingleSpawned = true;
            }

            /* periodic spawn while holding */
            if (!spawnDuringHold) return;
            if (Time.time - _lastHoldSpawn < holdInterval) return;

            SpawnAt(data.WorldPosition);
            _lastHoldSpawn = Time.time;
        }

        private void SpawnAt(Vector3 position)
        {
            if (!spawnPrefab) return;
            var parent = spawnParent != null ? spawnParent : transform;
            var instance = Instantiate(spawnPrefab, position, Quaternion.identity, parent);
            if (spawnLifetime > 0f)
                Destroy(instance, spawnLifetime);
            onSpawn?.Invoke();
        }
    }
}