using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Пробрасывает события жизненного цикла Unity в UnityEvent. Появление/исчезновение (OnEnable, OnDisable),
    ///     Awake, Start, Destroy, при необходимости — каждый кадр (Update, FixedUpdate, LateUpdate) с передачей времени.
    /// </summary>
    [NeoDoc("Tools/Components/UnityLifecycleEvents.md")]
    [CreateFromMenu("Neoxider/Tools/Components/UnityLifecycleEvents")]
    [AddComponentMenu("Neoxider/Tools/" + nameof(UnityLifecycleEvents))]
    public class UnityLifecycleEvents : MonoBehaviour
    {
        [Header("Lifecycle")]
        [SerializeField] private UnityEvent _onAwake = new UnityEvent();
        [SerializeField] private UnityEvent _onEnable = new UnityEvent();
        [SerializeField] private UnityEvent _onStart = new UnityEvent();
        [SerializeField] private UnityEvent _onDisable = new UnityEvent();
        [SerializeField] private UnityEvent _onDestroy = new UnityEvent();

        [Header("Per frame (включать только при необходимости)")]
        [Tooltip("Вызывать On Update каждый кадр; аргумент — deltaTime.")]
        [SerializeField] private bool _emitUpdate = false;
        [SerializeField] private UnityEvent<float> _onUpdate = new UnityEvent<float>();
        [Tooltip("Вызывать On Fixed Update каждый фиксированный кадр; аргумент — fixedDeltaTime.")]
        [SerializeField] private bool _emitFixedUpdate = false;
        [SerializeField] private UnityEvent<float> _onFixedUpdate = new UnityEvent<float>();
        [Tooltip("Вызывать On Late Update каждый кадр после Update; аргумент — deltaTime.")]
        [SerializeField] private bool _emitLateUpdate = false;
        [SerializeField] private UnityEvent<float> _onLateUpdate = new UnityEvent<float>();

        [Header("Application")]
        [SerializeField] private UnityEvent<bool> _onApplicationPause = new UnityEvent<bool>();
        [SerializeField] private UnityEvent<bool> _onApplicationFocus = new UnityEvent<bool>();

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

        private void Awake() => _onAwake?.Invoke();
        private void OnEnable() => _onEnable?.Invoke();
        private void Start() => _onStart?.Invoke();
        private void OnDisable() => _onDisable?.Invoke();
        private void OnDestroy() => _onDestroy?.Invoke();

        private void Update()
        {
            if (_emitUpdate)
                _onUpdate?.Invoke(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (_emitFixedUpdate)
                _onFixedUpdate?.Invoke(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            if (_emitLateUpdate)
                _onLateUpdate?.Invoke(Time.deltaTime);
        }

        private void OnApplicationPause(bool pauseStatus) => _onApplicationPause?.Invoke(pauseStatus);
        private void OnApplicationFocus(bool hasFocus) => _onApplicationFocus?.Invoke(hasFocus);
    }
}
