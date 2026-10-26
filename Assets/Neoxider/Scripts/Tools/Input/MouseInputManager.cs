using System;
using Neo;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Neo.Tools
{
    /// <summary>
    ///     Mouse input manager with value-type event data and allocation-free legacy input polling.
    ///     Provides Press, Hold, Release, Click events with MouseEventData struct.
    /// </summary>
    [CreateFromMenu("Neoxider/Tools/Input/MouseInputManager")]
    [AddComponentMenu("Neoxider/Tools/" + nameof(MouseInputManager))]
    [NeoDoc("Tools/Input/MouseInputManager.md")]
    public class MouseInputManager : Singleton<MouseInputManager>
    {
        public delegate void MouseEventHandler(in MouseEventData data);

        private static readonly RaycastHit2D[] _hits2D = new RaycastHit2D[1];

        // WHY: last event data for polling
        public static MouseEventData LastEventData;
        public static bool HasEventData;

        [Header("Gizmos")] public bool drawGizmos = true;
        public Color gizmoColor = Color.yellow;
        public Color gizmoTextColor = Color.white;
        public float gizmoRadius = 0.1f;
        public bool gizmoDrawText = true;
        public float gizmoTextScale = 1f;
        public int gizmoBaseFontSize = 18;
        public Vector3 gizmoTextOffset = new(0f, 1f, 0f);

        [Header("Enabled Modes")] public bool enablePress = true;
        public bool enableHold;
        public bool enableRelease = true;
        public bool enableClick = true;

        [Header("Raycast Settings")] [SerializeField]
        private Camera targetCamera;

        [Tooltip(
            "Resolve Camera.main only when Target Camera is empty. Disable when the camera is injected by scene setup.")]
        [SerializeField]
        private bool useMainCameraFallback = true;

        [Tooltip("Seconds between Camera.main fallback attempts while Target Camera is still empty.")]
        [SerializeField]
        [Min(0f)]
        private float cameraFallbackRetryInterval = 1f;

        [Tooltip(
            "Log missing camera through NeoDiagnostics. Warnings are still controlled by the global diagnostics gate.")]
        [SerializeField]
        private bool logMissingCamera;

        [SerializeField] private LayerMask interactableLayers = ~0;
        [SerializeField] private float fallbackDepth = 10f;
        private bool _hasData;
        private GameObject _lastHitObj;
        private Vector3 _lastWorldPos;
        private float _nextCameraFallbackTime;
        private bool _missingCameraLogged;

        private bool _pressed;
        private GameObject _pressedObj;
        // Release clears press state before callbacks, but its visual cleanup can still be interrupted.
        private bool _releasePending;
        private ulong _dispatchVersion;
        private DispatchPhase _dispatchPhase;

        private readonly SubscriberSnapshot<Action<MouseEventData>> _pressSubscribers = new();
        private readonly SubscriberSnapshot<Action<MouseEventData>> _holdSubscribers = new();
        private readonly SubscriberSnapshot<Action<MouseEventData>> _releaseSubscribers = new();
        private readonly SubscriberSnapshot<Action<MouseEventData>> _clickSubscribers = new();
        private readonly SubscriberSnapshot<Action> _canceledSubscribers = new();
        private readonly SubscriberSnapshot<MouseEventHandler> _pressInSubscribers = new();
        private readonly SubscriberSnapshot<MouseEventHandler> _holdInSubscribers = new();
        private readonly SubscriberSnapshot<MouseEventHandler> _releaseInSubscribers = new();
        private readonly SubscriberSnapshot<MouseEventHandler> _clickInSubscribers = new();
        public static ref readonly MouseEventData LastEventDataRef => ref LastEventData;

        protected override bool SetInstanceOnAwakeEnabled => true;
        protected override bool DontDestroyOnLoadEnabled => true;

        private void Update()
        {
            if (!MouseInputCompat.TryGetPosition(out Vector3 screenPosition) ||
                !MouseInputCompat.TryGetButtonDown(0, out bool wasPressed) ||
                !MouseInputCompat.TryGetButton(0, out bool isHeld) ||
                !MouseInputCompat.TryGetButtonUp(0, out bool wasReleased))
            {
                CancelPressedState();
                return;
            }

            ProcessMouseState(screenPosition, wasPressed, isHeld, wasReleased);
        }

        internal void ProcessMouseState(Vector2 screenPosition, bool wasPressed, bool isHeld, bool wasReleased)
        {
            if (this == null || !isActiveAndEnabled)
            {
                return;
            }

            if (ResolveTargetCamera() == null)
            {
                CancelPressedState();
                return;
            }

            if (!wasPressed && !wasReleased && !(enableHold && _pressed && isHeld))
            {
                return;
            }

            ulong version = unchecked(++_dispatchVersion);
            if (wasPressed)
            {
                BuildEventData(screenPosition, out MouseEventData data);
                CacheGizmoData(in data);
                _pressed = true;
                _pressedObj = data.HitObject;
                _releasePending = false;
                if (enablePress)
                {
                    if (!Dispatch(_pressSubscribers, in data, version, DispatchPhase.Press) ||
                        !DispatchIn(_pressInSubscribers, in data, version, DispatchPhase.Press))
                    {
                        return;
                    }
                }
            }

            if (!CanDispatch(version, DispatchPhase.Hold))
            {
                return;
            }

            if (enableHold && _pressed && isHeld)
            {
                BuildEventData(screenPosition, out MouseEventData data);
                CacheGizmoData(in data);
                if (!Dispatch(_holdSubscribers, in data, version, DispatchPhase.Hold) ||
                    !DispatchIn(_holdInSubscribers, in data, version, DispatchPhase.Hold))
                {
                    return;
                }
            }

            if (!CanDispatch(version, DispatchPhase.Release))
            {
                return;
            }

            if (wasReleased)
            {
                BuildEventData(screenPosition, out MouseEventData data);
                CacheGizmoData(in data);
                bool clicked = _pressed && _pressedObj && _pressedObj == data.HitObject;
                _releasePending = _pressed;
                ClearPressedState();
                bool releaseCompleted = false;
                try
                {
                    if (enableRelease &&
                        (!Dispatch(_releaseSubscribers, in data, version, DispatchPhase.Release) ||
                         !DispatchIn(_releaseInSubscribers, in data, version, DispatchPhase.Release)))
                    {
                        return;
                    }

                    if (!CanDispatch(version, DispatchPhase.Click))
                    {
                        return;
                    }

                    releaseCompleted = true;
                    _releasePending = false;
                    if (enableClick && clicked)
                    {
                        if (!Dispatch(_clickSubscribers, in data, version, DispatchPhase.Click))
                        {
                            return;
                        }

                        DispatchIn(_clickInSubscribers, in data, version, DispatchPhase.Click);
                    }
                }
                finally
                {
                    // A failed Release observer leaves ownership available for later cancellation.
                    if (releaseCompleted && _dispatchVersion == version)
                    {
                        _releasePending = false;
                    }
                }
            }
        }

        private void ClearPressedState()
        {
            _pressed = false;
            _pressedObj = null;
        }

        private void CancelPressedState()
        {
            bool wasPressed = _pressed || _releasePending;
            ClearPressedState();
            _releasePending = false;
            if (!wasPressed && _dispatchPhase == DispatchPhase.Canceled)
            {
                // A repeated loss signal must not fence the remaining cleanup subscribers.
                return;
            }

            ulong version = unchecked(++_dispatchVersion);
            if (wasPressed)
            {
                DispatchCanceled(version);
            }
        }

        private bool CanDispatch(ulong version, DispatchPhase phase)
        {
            return _dispatchVersion == version &&
                   (phase == DispatchPhase.Canceled || (this != null && isActiveAndEnabled));
        }

        private bool Dispatch(SubscriberSnapshot<Action<MouseEventData>> subscribers, in MouseEventData data,
            ulong version, DispatchPhase phase)
        {
            if (!CanDispatch(version, phase))
            {
                return false;
            }

            Delegate[] snapshot = subscribers.Capture();
            DispatchPhase previousPhase = _dispatchPhase;
            _dispatchPhase = phase;
            try
            {
                for (int i = 0; i < snapshot.Length; i++)
                {
                    if (!CanDispatch(version, phase))
                    {
                        return false;
                    }

                    ((Action<MouseEventData>)snapshot[i])(data);
                }

                return CanDispatch(version, phase);
            }
            finally
            {
                _dispatchPhase = previousPhase;
            }
        }

        private bool DispatchIn(SubscriberSnapshot<MouseEventHandler> subscribers, in MouseEventData data,
            ulong version, DispatchPhase phase)
        {
            if (!CanDispatch(version, phase))
            {
                return false;
            }

            Delegate[] snapshot = subscribers.Capture();
            DispatchPhase previousPhase = _dispatchPhase;
            _dispatchPhase = phase;
            try
            {
                for (int i = 0; i < snapshot.Length; i++)
                {
                    if (!CanDispatch(version, phase))
                    {
                        return false;
                    }

                    ((MouseEventHandler)snapshot[i])(in data);
                }

                return CanDispatch(version, phase);
            }
            finally
            {
                _dispatchPhase = previousPhase;
            }
        }

        private void DispatchCanceled(ulong version)
        {
            Delegate[] snapshot = _canceledSubscribers.Capture();
            DispatchPhase previousPhase = _dispatchPhase;
            _dispatchPhase = DispatchPhase.Canceled;
            try
            {
                for (int i = 0; i < snapshot.Length; i++)
                {
                    if (!CanDispatch(version, DispatchPhase.Canceled))
                    {
                        return;
                    }

                    ((Action)snapshot[i])();
                }
            }
            finally
            {
                _dispatchPhase = previousPhase;
            }
        }

        private void OnDisable()
        {
            CancelPressedState();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                CancelPressedState();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                CancelPressedState();
            }
        }

        protected override void Init()
        {
            base.Init();
            ResolveTargetCamera();
        }

        public Camera TargetCamera => targetCamera;

        public void SetTargetCamera(Camera camera)
        {
            targetCamera = camera;
            _nextCameraFallbackTime = 0f;
            _missingCameraLogged = false;
        }

        /// <seealso cref="MouseInputManagerSubsystemRegistration"/>
        public static void ResetSubsystemPollingState()
        {
            LastEventData = default;
            HasEventData = false;
        }

        /// <seealso cref="MouseInputManagerSubsystemRegistration"/>
        public static void EnableAutoCreateForRuntime()
        {
            CreateInstance = true;
        }

        private void CacheGizmoData(in MouseEventData data)
        {
            ref readonly MouseEventData d = ref LastEventDataRef;
            _lastWorldPos = d.WorldPosition;
            _lastHitObj = d.HitObject;
            _hasData = true;
        }

        private Camera ResolveTargetCamera()
        {
            if (targetCamera != null)
            {
                return targetCamera;
            }

            if (useMainCameraFallback)
            {
                float now = Time.realtimeSinceStartup;
                if (cameraFallbackRetryInterval <= 0f || now >= _nextCameraFallbackTime)
                {
                    targetCamera = Camera.main;
                    _nextCameraFallbackTime = now + Mathf.Max(0f, cameraFallbackRetryInterval);
                }
            }

                if (targetCamera == null && logMissingCamera && !_missingCameraLogged)
                {
                    _missingCameraLogged = true;
                    NeoDiagnostics.LogWarningThrottled(
                        $"{nameof(MouseInputManager)}.{NeoDiagnostics.StableId(this)}.MissingCamera",
                        $"[{nameof(MouseInputManager)}] Target Camera is not assigned and MainCamera fallback is unavailable.",
                        this,
                        5f);
                }

            return targetCamera;
        }

        private void BuildEventData(Vector2 screenPos, out MouseEventData data)
        {
            Vector3 worldPos;
            GameObject hitObj = null;
            RaycastHit hit3D = default;
            RaycastHit2D hit2D = default;

            if (targetCamera == null)
            {
                data = new MouseEventData(screenPos, Vector3.zero, null, hit3D, hit2D);
                return;
            }

            Ray ray = targetCamera.ScreenPointToRay(screenPos);

            // WHY: RaycastNonAlloc returns hits in arbitrary order, so a 1-element buffer could
            // report an occluded far collider; Physics.Raycast returns the closest hit (no alloc).
            if (Physics.Raycast(ray, out RaycastHit closestHit, float.MaxValue, interactableLayers))
            {
                hit3D = closestHit;
                worldPos = hit3D.point;
                hitObj = hit3D.collider.gameObject;
            }
            else if ((_hits2D[0] = Physics2D.GetRayIntersection(ray, float.MaxValue, interactableLayers)).collider)
            {
                hit2D = _hits2D[0];
                worldPos = hit2D.point;
                hitObj = hit2D.collider.gameObject;
            }
            else
            {
                worldPos = targetCamera.ScreenToWorldPoint(
                    new Vector3(screenPos.x, screenPos.y, fallbackDepth));
            }

            data = new MouseEventData(screenPos, worldPos, hitObj, hit3D, hit2D);
            // WHY: update static polling data as well
            LastEventData = data;
            HasEventData = true;
        }

        public event Action<MouseEventData> OnPress
        {
            add => _pressSubscribers.Add(value);
            remove => _pressSubscribers.Remove(value);
        }

        public event Action<MouseEventData> OnHold
        {
            add => _holdSubscribers.Add(value);
            remove => _holdSubscribers.Remove(value);
        }

        public event Action<MouseEventData> OnRelease
        {
            add => _releaseSubscribers.Add(value);
            remove => _releaseSubscribers.Remove(value);
        }

        public event Action<MouseEventData> OnClick
        {
            add => _clickSubscribers.Add(value);
            remove => _clickSubscribers.Remove(value);
        }

        /// <summary>
        ///     An active press was interrupted by lifecycle, camera or input loss.
        ///     State is cleared before notification; this does not emit Release or Click.
        /// </summary>
        public event Action OnCanceled
        {
            add => _canceledSubscribers.Add(value);
            remove => _canceledSubscribers.Remove(value);
        }

        public event MouseEventHandler OnPressIn
        {
            add => _pressInSubscribers.Add(value);
            remove => _pressInSubscribers.Remove(value);
        }

        public event MouseEventHandler OnHoldIn
        {
            add => _holdInSubscribers.Add(value);
            remove => _holdInSubscribers.Remove(value);
        }

        public event MouseEventHandler OnReleaseIn
        {
            add => _releaseInSubscribers.Add(value);
            remove => _releaseInSubscribers.Remove(value);
        }

        public event MouseEventHandler OnClickIn
        {
            add => _clickInSubscribers.Add(value);
            remove => _clickInSubscribers.Remove(value);
        }

        private enum DispatchPhase
        {
            None,
            Press,
            Hold,
            Release,
            Click,
            Canceled
        }

        // Private event storage: subscription changes invalidate ownership immediately. An active
        // dispatch owns its immutable local array; accessors never clear or mutate that array.
        private sealed class SubscriberSnapshot<T> where T : Delegate
        {
            private readonly object _gate = new();
            private T _handlers;
            private Delegate[] _snapshot;

            public void Add(T handler)
            {
                lock (_gate)
                {
                    T combined = (T)Delegate.Combine(_handlers, handler);
                    if (!ReferenceEquals(combined, _handlers))
                    {
                        _handlers = combined;
                        _snapshot = null;
                    }
                }
            }

            public void Remove(T handler)
            {
                lock (_gate)
                {
                    T remaining = (T)Delegate.Remove(_handlers, handler);
                    if (!ReferenceEquals(remaining, _handlers))
                    {
                        _handlers = remaining;
                        _snapshot = null;
                    }
                }
            }

            public Delegate[] Capture()
            {
                lock (_gate)
                {
                    if (_snapshot == null)
                    {
                        _snapshot = _handlers == null ? Array.Empty<Delegate>() : _handlers.GetInvocationList();
                    }

                    return _snapshot;
                }
            }
        }

        public readonly struct MouseEventData
        {
            public readonly Vector2 ScreenPosition;
            public readonly Vector3 WorldPosition;
            public readonly GameObject HitObject;
            public readonly RaycastHit Hit3D;
            public readonly RaycastHit2D Hit2D;

            public MouseEventData(Vector2 sp, Vector3 wp,
                GameObject hitObj,
                in RaycastHit hit3D,
                in RaycastHit2D hit2D)
            {
                ScreenPosition = sp;
                WorldPosition = wp;
                HitObject = hitObj;
                Hit3D = hit3D;
                Hit2D = hit2D;
            }
        }

#if UNITY_EDITOR
        private static GUIStyle _labelStyle;
        private void OnDrawGizmos()
        {
            if (!drawGizmos || !_hasData)
            {
                return;
            }

            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(_lastWorldPos, gizmoRadius);

            if (!gizmoDrawText)
            {
                return;
            }

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(EditorStyles.boldLabel);
            }

            _labelStyle.normal.textColor = gizmoTextColor;

            float handleSize = HandleUtility.GetHandleSize(_lastWorldPos);
            int fontSize = Mathf.Clamp((int)(gizmoBaseFontSize / handleSize * gizmoTextScale), 8, 64);
            _labelStyle.fontSize = fontSize;

            string label = _lastHitObj ? _lastHitObj.name : "No Hit";

            Camera cam = SceneView.currentDrawingSceneView != null
                ? SceneView.currentDrawingSceneView.camera
                : Camera.current;
            Vector3 up = cam != null ? cam.transform.up : Vector3.up;
            Vector3 right = cam != null ? cam.transform.right : Vector3.right;
            Vector3 forward = cam != null ? cam.transform.forward : Vector3.forward;
            Vector3 pos = _lastWorldPos + right * gizmoTextOffset.x + up * (gizmoRadius + gizmoTextOffset.y) +
                          forward * gizmoTextOffset.z;

            Handles.Label(pos, label, _labelStyle);
        }
#endif
    }
}
