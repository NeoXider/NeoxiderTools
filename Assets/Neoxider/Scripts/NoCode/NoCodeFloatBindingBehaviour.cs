using Neo.Reactive;
using UnityEngine;

namespace Neo.NoCode
{
    /// <summary>
    ///     Shared wiring: <see cref="ComponentFloatBinding"/>, optional reactive subscription, optional poll.
    /// </summary>
    public abstract class NoCodeFloatBindingBehaviour : MonoBehaviour
    {
        [SerializeField] private ComponentFloatBinding _binding = new();

        [SerializeField] private NoCodeFloatUpdateMode _updateMode = NoCodeFloatUpdateMode.Reactive;

        [Tooltip("When Update Mode is Poll, refreshes every LateUpdate.")]
        [SerializeField]
        private bool _pollInLateUpdate = true;

        private ReactivePropertyFloat _subscribedReactive;

        public ComponentFloatBinding Binding => _binding;

        protected virtual void OnValidate()
        {
            _binding.Invalidate();
        }

        protected virtual void OnEnable()
        {
            _binding.Invalidate();
            TrySubscribeReactive();
            RefreshFromSource();
            TrySubscribeReactive();
        }

        protected virtual void OnDisable()
        {
            if (_subscribedReactive != null)
            {
                _subscribedReactive.RemoveListener(OnReactiveValueChanged);
                _subscribedReactive = null;
            }
        }

        protected virtual void LateUpdate()
        {
            if (_updateMode == NoCodeFloatUpdateMode.Poll && _pollInLateUpdate)
            {
                RefreshFromSource();
            }
        }

        private void OnReactiveValueChanged(float _)
        {
            RefreshFromSource();
        }

        private void TrySubscribeReactive()
        {
            if (_updateMode != NoCodeFloatUpdateMode.Reactive || _subscribedReactive != null)
            {
                return;
            }

            if (!_binding.TryGetReactivePropertyFloat(this, out ReactivePropertyFloat reactive))
            {
                return;
            }

            reactive.AddListener(OnReactiveValueChanged);
            _subscribedReactive = reactive;
        }

        protected void RefreshFromSource()
        {
            TrySubscribeReactive();
            if (!_binding.TryReadFloat(this, out float value))
            {
                return;
            }

            ApplyFloat(value);
            TrySubscribeReactive();
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
