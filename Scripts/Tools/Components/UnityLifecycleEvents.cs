using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Forwards Unity lifecycle callbacks to UnityEvents: OnEnable, OnDisable, Awake, Start, Destroy,
    ///     and optionally per-frame Update, FixedUpdate, LateUpdate with delta time arguments.
    /// </summary>
    [NeoDoc("Tools/Components/UnityLifecycleEvents.md")]
    [CreateFromMenu("Neoxider/Tools/Components/UnityLifecycleEvents")]
    [AddComponentMenu("Neoxider/Tools/" + nameof(UnityLifecycleEvents))]
    public class UnityLifecycleEvents : MonoBehaviour
    {
        [Header("Lifecycle")] [SerializeField] private UnityEvent _onAwake = new();

        [SerializeField] private UnityEvent _onEnable = new();
        [SerializeField] private UnityEvent _onStart = new();
        [SerializeField] private UnityEvent _onDisable = new();
        [SerializeField] private UnityEvent _onDestroy = new();

        [Header("Per frame (enable only when needed)")]
        [Tooltip("Invoke On Update every frame; argument is deltaTime.")]
        [SerializeField]
        private bool _emitUpdate;

        [SerializeField] private UnityEvent<float> _onUpdate = new();

        [Tooltip("Invoke On Fixed Update every physics tick; argument is fixedDeltaTime.")] [SerializeField]
        private bool _emitFixedUpdate;

        [SerializeField] private UnityEvent<float> _onFixedUpdate = new();

        [Tooltip("Invoke On Late Update every frame after Update; argument is deltaTime.")] [SerializeField]
        private bool _emitLateUpdate;

        [SerializeField] private UnityEvent<float> _onLateUpdate = new();

        [Header("Application")] [SerializeField]
        private UnityEvent<bool> _onApplicationPause = new();

        [SerializeField] private UnityEvent<bool> _onApplicationFocus = new();

        public UnityEvent OnAwake => _onAwake;
        public UnityEvent OnEnableEvent => _onEnable;
        public UnityEvent OnStart => _onStart;
        public UnityEvent OnDisableEvent => _onDisable;
        public UnityEvent OnDestroyEvent => _onDestroy;
        public UnityEvent<float> OnUpdateEvent => _onUpdate;
        public UnityEvent<float> OnFixedUpdateEvent => _onFixedUpdate;
        public UnityEvent<float> OnLateUpdateEvent => _onLateUpdate;
        public UnityEvent<bool> OnApplicationPauseEvent => _onApplicationPause;
        public UnityEvent<bool> OnApplicationFocusEvent => _onApplicationFocus;

        private void Awake()
        {
            _onAwake?.Invoke();
        }

        private void Start()
        {
            _onStart?.Invoke();
        }

        private void Update()
        {
            if (_emitUpdate)
            {
                _onUpdate?.Invoke(Time.deltaTime);
            }
        }

        private void FixedUpdate()
        {
            if (_emitFixedUpdate)
            {
                _onFixedUpdate?.Invoke(Time.fixedDeltaTime);
            }
        }

        private void LateUpdate()
        {
            if (_emitLateUpdate)
            {
                _onLateUpdate?.Invoke(Time.deltaTime);
            }
        }

        private void OnEnable()
        {
            _onEnable?.Invoke();
        }

        private void OnDisable()
        {
            _onDisable?.Invoke();
        }

        private void OnDestroy()
        {
            _onDestroy?.Invoke();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            _onApplicationFocus?.Invoke(hasFocus);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            _onApplicationPause?.Invoke(pauseStatus);
        }
    }
}
