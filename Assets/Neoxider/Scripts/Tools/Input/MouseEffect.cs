using UnityEngine;

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

        [Tooltip("Interval in seconds for Hold spawning and position updates")]
        public float holdInterval = 0.016f;

        [Header("Follow Settings")] [Tooltip("Depth (world Z) used to place the trail / follower")]
        public float followDepth = 10f;

        private Camera _cam;
        private bool _holdSingleSpawned;
        private bool _isFollowing;
        private float _lastFollowUpdate;

        private float _lastHoldSpawn;
        private MouseInputManager _mim;

        private void Awake()
        {
            _cam = Camera.main;
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

            if (trail) trail.enabled = false;
            if (followObject) followObject.SetActive(false);
        }

        private void Update()
        {
            if (!_isFollowing) return;
            if (Time.time - _lastFollowUpdate < holdInterval) return;
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
            if (trail)
            {
#if UNITY_2020_2_OR_NEWER
                trail.Clear();
#endif
                trail.transform.position = data.WorldPosition;
                trail.enabled = true;
            }

            if (followObject)
            {
                followObject.SetActive(false);
                followObject.transform.position = data.WorldPosition;
                followObject.SetActive(true);
            }

            if (spawnPrefab && spawnTrigger == SpawnTrigger.Press)
                Instantiate(spawnPrefab, data.WorldPosition, Quaternion.identity);

            _lastHoldSpawn = Time.time;
            _lastFollowUpdate = Time.time;
            _holdSingleSpawned = false;
            _isFollowing = true;
        }

        private void OnRelease(MouseInputManager.MouseEventData data)
        {
            if (spawnPrefab && spawnTrigger == SpawnTrigger.Release)
                Instantiate(spawnPrefab, data.WorldPosition, Quaternion.identity);

            if (trail) trail.enabled = false;
            if (followObject) followObject.SetActive(false);

            _isFollowing = false;
            _holdSingleSpawned = false;
        }

        private void OnClick(MouseInputManager.MouseEventData data)
        {
            if (spawnPrefab && spawnTrigger == SpawnTrigger.Click)
                Instantiate(spawnPrefab, data.WorldPosition, Quaternion.identity);
        }

        private void OnHold(MouseInputManager.MouseEventData data)
        {
            if (!spawnPrefab) return;

            /* single-shot spawn on first Hold frame */
            if (spawnTrigger == SpawnTrigger.Hold && !_holdSingleSpawned)
            {
                Instantiate(spawnPrefab, data.WorldPosition, Quaternion.identity);
                _holdSingleSpawned = true;
            }

            /* periodic spawn while holding */
            if (!spawnDuringHold) return;
            if (Time.time - _lastHoldSpawn < holdInterval) return;

            Instantiate(spawnPrefab, data.WorldPosition, Quaternion.identity);
            _lastHoldSpawn = Time.time;
        }
    }
}