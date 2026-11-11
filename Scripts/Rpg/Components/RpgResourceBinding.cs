using System;
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
    [CreateFromMenu("Neoxider/RPG/RpgResourceBinding")]
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
        private ReactivePropertyFloat _currentState;
        private ReactivePropertyFloat _maxState;
        private ReactivePropertyFloat _percentState;
        private int _subscriptionVersion;
        private RpgCharacter _boundCharacter;
        private UnityAction<float> _currentListener;
        private UnityAction<float> _maxListener;
        private UnityAction<float> _percentListener;
        private Action _rebuiltListener;

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
            RpgCharacter character = _character;
            ReactivePropertyFloat current = character.GetResourceCurrentState(id);
            ReactivePropertyFloat max = character.GetResourceMaxState(id);
            ReactivePropertyFloat percent = character.GetResourcePercentState(id);
            int version = ++_subscriptionVersion;
            _currentState = current;
            _maxState = max;
            _percentState = percent;
            _boundCharacter = character;
            _subscribed = true;
            Func<bool> sessionIsCurrent = () => character != null && _subscribed && isActiveAndEnabled &&
                version == _subscriptionVersion && _character == character && _resourceId.Value == id;
            Func<bool> statesAreCurrent = () => sessionIsCurrent() &&
                ReferenceEquals(character.GetResourceCurrentState(id), current) &&
                ReferenceEquals(character.GetResourceMaxState(id), max) &&
                ReferenceEquals(character.GetResourcePercentState(id), percent);
            _currentListener = value => { if (statesAreCurrent()) { _onCurrent?.Invoke(value); } };
            _maxListener = value => { if (statesAreCurrent()) { _onMax?.Invoke(value); } };
            _percentListener = value => { if (statesAreCurrent()) { _onPercent?.Invoke(value); } };
            _rebuiltListener = () =>
            {
                if (sessionIsCurrent())
                {
                    Unsubscribe();
                    SubscribeIfReady();
                }
            };
            character.RuntimeRebuilt += _rebuiltListener;

            if (current != null)
            {
                current.AddListener(_currentListener);
            }

            if (max != null)
            {
                max.AddListener(_maxListener);
            }

            if (percent != null)
            {
                percent.AddListener(_percentListener);
            }

            // WHY: push initial values immediately so UI is correct on the first frame.
            if (current != null)
            {
                _onCurrent?.Invoke(current.CurrentValue);
            }
            if (!statesAreCurrent())
            {
                return;
            }
            if (max != null)
            {
                _onMax?.Invoke(max.CurrentValue);
            }
            if (!statesAreCurrent())
            {
                return;
            }
            if (percent != null)
            {
                _onPercent?.Invoke(percent.CurrentValue);
            }

        }

        private void Unsubscribe()
        {
            _subscriptionVersion++;
            if (!_subscribed)
            {
                return;
            }

            _currentState?.RemoveListener(_currentListener);
            _maxState?.RemoveListener(_maxListener);
            _percentState?.RemoveListener(_percentListener);
            _currentState = null;
            _maxState = null;
            _percentState = null;
            if (_boundCharacter != null)
            {
                _boundCharacter.RuntimeRebuilt -= _rebuiltListener;
            }
            _boundCharacter = null;
            _currentListener = null;
            _maxListener = null;
            _percentListener = null;
            _rebuiltListener = null;
            _subscribed = false;
        }
    }
}
