using Neo.Reactive;
using UnityEngine;

namespace Neo.NoCode
{
    /// <summary>
    ///     Shared wiring: <see cref="ComponentFloatBinding"/>, optional reactive subscription, optional poll.
    /// </summary>
    public abstract class NoCodeFloatBindingBehaviour : MonoBehaviour
    {
        private const float DefaultPollIntervalSeconds = 0.16f;
        private const float MinPollIntervalSeconds = 0.016f;

        [SerializeField] private ComponentFloatBinding _binding = new();

        [SerializeField] private NoCodeFloatUpdateMode _updateMode = NoCodeFloatUpdateMode.Reactive;

        [Tooltip("When Update Mode is Poll, refreshes in LateUpdate using Poll Interval.")]
        [SerializeField]
        private bool _pollInLateUpdate = true;

        [Tooltip("Seconds between refreshes in Poll mode and Reactive fallback for ordinary fields. Default 0.16; minimum 0.016.")]
        [Min(MinPollIntervalSeconds)]
        [SerializeField]
        private float _pollIntervalSeconds = DefaultPollIntervalSeconds;

        private ReactivePropertyFloat _subscribedFloat;
        private ReactivePropertyInt _subscribedInt;
        private ReactivePropertyBool _subscribedBool;
        private float _nextPollTime;
        private bool _useReactivePollFallback;

        public ComponentFloatBinding Binding => _binding;

        private float PollIntervalSeconds => Mathf.Max(MinPollIntervalSeconds, _pollIntervalSeconds);

        private bool HasReactiveSubscription =>
            _subscribedFloat != null || _subscribedInt != null || _subscribedBool != null;

        protected virtual void OnValidate()
        {
            _binding.Invalidate();
            _pollIntervalSeconds = PollIntervalSeconds;
            _useReactivePollFallback = false;
        }

        protected virtual void OnEnable()
        {
            _binding.Invalidate();
            _useReactivePollFallback = false;
            _nextPollTime = Time.unscaledTime + PollIntervalSeconds;
            TrySubscribeReactive();
            RefreshFromSource();
            TrySubscribeReactive();
        }

        protected virtual void OnDisable()
        {
            if (_subscribedFloat != null)
            {
                _subscribedFloat.RemoveListener(OnReactiveFloatChanged);
                _subscribedFloat = null;
            }

            if (_subscribedInt != null)
            {
                _subscribedInt.RemoveListener(OnReactiveIntChanged);
                _subscribedInt = null;
            }

            if (_subscribedBool != null)
            {
                _subscribedBool.RemoveListener(OnReactiveBoolChanged);
                _subscribedBool = null;
            }
        }

        protected virtual void LateUpdate()
        {
            bool shouldPoll = _updateMode == NoCodeFloatUpdateMode.Poll ||
                              (_updateMode == NoCodeFloatUpdateMode.Reactive && _useReactivePollFallback);
            if (!shouldPoll || !_pollInLateUpdate)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now < _nextPollTime)
            {
                return;
            }

            RefreshFromSource();
            _nextPollTime = now + PollIntervalSeconds;
        }

        private void OnReactiveFloatChanged(float _)
        {
            RefreshFromSource();
        }

        private void OnReactiveIntChanged(int _)
        {
            RefreshFromSource();
        }

        private void OnReactiveBoolChanged(bool _)
        {
            RefreshFromSource();
        }

        private bool TrySubscribeReactive()
        {
            if (_updateMode != NoCodeFloatUpdateMode.Reactive ||
                HasReactiveSubscription)
            {
                return HasReactiveSubscription;
            }

            if (!_binding.TryGetReactiveProperty(this, out ReactivePropertyFloat reactiveFloat,
                    out ReactivePropertyInt reactiveInt, out ReactivePropertyBool reactiveBool))
            {
                return false;
            }

            if (reactiveFloat != null)
            {
                reactiveFloat.AddListener(OnReactiveFloatChanged);
                _subscribedFloat = reactiveFloat;
                return true;
            }

            if (reactiveInt != null)
            {
                reactiveInt.AddListener(OnReactiveIntChanged);
                _subscribedInt = reactiveInt;
                return true;
            }

            reactiveBool.AddListener(OnReactiveBoolChanged);
            _subscribedBool = reactiveBool;
            return true;
        }

        private void EnableReactivePollFallback()
        {
            if (_updateMode != NoCodeFloatUpdateMode.Reactive || HasReactiveSubscription)
            {
                return;
            }

            _useReactivePollFallback = true;
        }

        protected void RefreshFromSource()
        {
            bool reactive = TrySubscribeReactive();
            if (!_binding.TryReadFloat(this, out float value))
            {
                return;
            }

            ApplyFloat(value);
            reactive |= TrySubscribeReactive();
            if (!reactive)
            {
                EnableReactivePollFallback();
            }
        }

        protected abstract void ApplyFloat(float value);

#if UNITY_EDITOR
        /// <summary>
        ///     Edit Mode tests / editor utilities: apply binding when <see cref="OnEnable"/> did not run or after wiring via serialized object.
        /// </summary>
        internal void EditorInvokeRefreshFromSource() => RefreshFromSource();
#endif
    }
}
