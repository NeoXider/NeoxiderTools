using Neo.Reactive;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Rpg.Components
{
    /// <summary>
    ///     Thin component that exposes one resource of an <see cref="RpgCharacter"/> as
    ///     <see cref="ReactivePropertyFloat"/>s and NeoCondition-readable values, so UI
    ///     (Slider, TMP_Text) can bind to <c>"DarkMana"</c>, <c>"Stamina"</c>, etc. without code.
    ///     <para>Drop on the UI GameObject, drag the <c>RpgCharacter</c>, pick the resource — done.</para>
    /// </summary>
    [NeoDoc("Rpg/RpgResourceBinding.md")]
    [AddComponentMenu("Neoxider/RPG/" + nameof(RpgResourceBinding))]
    public sealed class RpgResourceBinding : MonoBehaviour
    {
        [Tooltip("Character whose resource is bound. When empty, searches up the hierarchy.")] [SerializeField]
        private RpgCharacter _character;

        [Tooltip("Resource id (preset or Custom string).")] [SerializeField]
        private RpgStatId _resourceId = new(RpgStatPreset.Hp);

        [Header("Events (per-frame, drives UI)")] [SerializeField]
        private UnityEventFloat _onCurrent = new();

        [SerializeField] private UnityEventFloat _onMax = new();
        [SerializeField] private UnityEventFloat _onPercent = new();

        private bool _subscribed;

        public RpgCharacter Character
        {
            get => _character;
            set
            {
                Unsubscribe();
                _character = value;
                SubscribeIfReady();
            }
        }

        public RpgStatId ResourceId
        {
            get => _resourceId;
            set
            {
                Unsubscribe();
                _resourceId = value;
                SubscribeIfReady();
            }
        }

        public UnityEventFloat OnCurrent => _onCurrent;
        public UnityEventFloat OnMax => _onMax;
        public UnityEventFloat OnPercent => _onPercent;

        public float CurrentValue =>
            _character != null ? _character.GetResource(_resourceId.Value) : 0f;

        public float MaxValue =>
            _character != null ? _character.GetResourceMax(_resourceId.Value) : 0f;

        public float PercentValue =>
            _character != null ? _character.GetResourcePercent(_resourceId.Value) : 0f;

        private void OnEnable()
        {
            if (_character == null)
            {
                _character = GetComponentInParent<RpgCharacter>();
            }

            SubscribeIfReady();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void SubscribeIfReady()
        {
            if (_subscribed || _character == null || !isActiveAndEnabled)
            {
                return;
            }

            string id = _resourceId.Value;
            ReactivePropertyFloat current = _character.GetResourceCurrentState(id);
            ReactivePropertyFloat max = _character.GetResourceMaxState(id);
            ReactivePropertyFloat percent = _character.GetResourcePercentState(id);

            if (current != null)
            {
                current.AddListener(HandleCurrent);
            }

            if (max != null)
            {
                max.AddListener(HandleMax);
            }

            if (percent != null)
            {
                percent.AddListener(HandlePercent);
            }

            // WHY: push initial values immediately so UI is correct on the first frame.
            if (current != null)
            {
                _onCurrent?.Invoke(current.CurrentValue);
            }

            if (max != null)
            {
                _onMax?.Invoke(max.CurrentValue);
            }

            if (percent != null)
            {
                _onPercent?.Invoke(percent.CurrentValue);
            }

            _subscribed = current != null || max != null || percent != null;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _character == null)
            {
                _subscribed = false;
                return;
            }

            string id = _resourceId.Value;
            _character.GetResourceCurrentState(id)?.RemoveListener(HandleCurrent);
            _character.GetResourceMaxState(id)?.RemoveListener(HandleMax);
            _character.GetResourcePercentState(id)?.RemoveListener(HandlePercent);
            _subscribed = false;
        }

        private void HandleCurrent(float v)
        {
            _onCurrent?.Invoke(v);
        }

        private void HandleMax(float v)
        {
            _onMax?.Invoke(v);
        }

        private void HandlePercent(float v)
        {
            _onPercent?.Invoke(v);
        }
    }
}
