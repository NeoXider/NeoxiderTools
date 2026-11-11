using Neo.Reactive;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Component attached to each item managed by a Selector. Stores index, knows its Selector,
    ///     and reacts to activate/deactivate commands (e.g. when Selector runs in NotifySelectorItemsOnly mode).
    ///     Use for anomaly-style flows: subscribe to OnActivated/OnDeactivated to drive visuals or call ExcludeFromSelector
    ///     when "fixed".
    /// </summary>
    [NeoDoc("Tools/View/SelectorItem.md")]
    [CreateFromMenu("Neoxider/Tools/View/SelectorItem")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(SelectorItem))]
    public class SelectorItem : MonoBehaviour
    {
        [Header("State")]
        [Tooltip("Index of this item in the parent Selector (set by Selector or manually).")]
        [SerializeField]
        private int _index;

        [Tooltip("Current active state (true = selected/active).")] [SerializeField]
        private bool _isActive;

        [Tooltip("Reactive state; subscribe via Active.OnChanged.")]
        public ReactivePropertyBool Active = new();

        [Header("Events")] [Tooltip("Invoked when this item becomes active (selected).")]
        public UnityEvent OnActivated;

        [Tooltip("Invoked when this item becomes inactive (deselected).")]
        public UnityEvent OnDeactivated;

        [Tooltip(
            "Invoked when state changes; passes the inverse of the new value (true when deactivated, false when activated). Subscribe via Active.OnChanged for the direct value.")]
        public UnityEvent<bool> OnValueChangeInverse;

        private Selector _cachedSelector;

        /// <summary>
        ///     Index of this item in the parent Selector.
        /// </summary>
        public int Index
        {
            get => _index;
            set => _index = value;
        }

        /// <summary>
        ///     Current active state (for NeoCondition and reflection).
        /// </summary>
        public bool ValueBool => Active.CurrentValue;

        /// <summary>
        ///     Current active state (alias for ValueBool).
        /// </summary>
        public bool ActiveValue => Active.CurrentValue;

        private void Awake()
        {
            Active.Value = _isActive;
            CacheSelector();
        }

        private void OnTransformParentChanged()
        {
            _cachedSelector = null;
        }

        private void CacheSelector()
        {
            if (_cachedSelector == null)
            {
                _cachedSelector = GetComponentInParent<Selector>();
            }
        }

        /// <summary>
        ///     Sets the active state (called by Selector in NotifySelectorItemsOnly mode). Updates Active and invokes events.
        /// </summary>
        /// <param name="active">True to activate, false to deactivate.</param>
        [Button]
        public void SetActive(bool active)
        {
            SetActive(active, false);
        }

        /// <summary>
        ///     Same as <see cref="SetActive(bool)"/>, but when <paramref name="forceNotify"/> is true and the logical state
        ///     already matches, still invokes <see cref="OnActivated"/> / <see cref="OnDeactivated"/> so Inspector-wired
        ///     side effects (e.g. disabling another object) run on the first selector sync.
        /// </summary>
        public void SetActive(bool active, bool forceNotify)
        {
            if (_isActive == active && !forceNotify)
            {
                return;
            }

            if (_isActive != active)
            {
                _isActive = active;
                Active.Value = active;
            }

            if (active)
            {
                OnActivated?.Invoke();
                OnValueChangeInverse?.Invoke(false);
            }
            else
            {
                OnDeactivated?.Invoke();
                OnValueChangeInverse?.Invoke(true);
            }
        }

        [Button]
        public void Activate()
        {
            SetActive(true);
        }

        [Button]
        public void Deactivate()
        {
            SetActive(false);
        }

        /// <summary>
        ///     Excludes this item's index from the parent Selector's pool (e.g. when "fixed" or resolved).
        ///     Does nothing if no Selector is found.
        /// </summary>
        [Button]
        public void ExcludeFromSelector()
        {
            CacheSelector();
            _cachedSelector?.ExcludeIndex(_index);
        }

        /// <summary>
        ///     Includes this item's index back into the parent Selector's pool.
        /// </summary>
        [Button]
        public void IncludeInSelector()
        {
            CacheSelector();
            _cachedSelector?.IncludeIndex(_index);
        }

        /// <summary>
        ///     Returns the parent Selector, or null if not under a Selector. Result is cached after first lookup.
        /// </summary>
        public Selector GetSelector()
        {
            CacheSelector();
            return _cachedSelector;
        }
    }
}
