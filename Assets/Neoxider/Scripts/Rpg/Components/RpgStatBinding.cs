using System;
using Neo.Reactive;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Rpg.Components
{
    /// <summary>
    ///     Bind one <see cref="RpgCharacter"/> stat (Strength / Defense / FireResist / custom) to a UI
    ///     UnityEvent. Same idea as <see cref="RpgResourceBinding"/> but for single-value stats.
    /// </summary>
    [NeoDoc("Rpg/RpgStatBinding.md")]
    [CreateFromMenu("Neoxider/RPG/RpgStatBinding")]
    [AddComponentMenu("Neoxider/RPG/" + nameof(RpgStatBinding))]
    public sealed class RpgStatBinding : MonoBehaviour
    {
        [SerializeField] private RpgCharacter _character;
        [SerializeField] private RpgStatId _statId = new(RpgStatPreset.Strength);
        [SerializeField] private UnityEventFloat _onValue = new();

        private bool _subscribed;
        private ReactivePropertyFloat _state;
        private int _subscriptionVersion;
        private RpgCharacter _boundCharacter;
        private UnityAction<float> _valueListener;
        private Action _rebuiltListener;

        public UnityEventFloat OnValue => _onValue;

        public float Value =>
            _character != null ? _character.GetStat(_statId.Value) : 0f;

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

        public RpgStatId StatId
        {
            get => _statId;
            set
            {
                Unsubscribe();
                _statId = value;
                SubscribeIfReady();
            }
        }

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

            RpgCharacter character = _character;
            string id = _statId.Value;
            ReactivePropertyFloat state = character.GetStatState(id);
            int version = ++_subscriptionVersion;
            _state = state;
            _boundCharacter = character;
            _subscribed = true;
            Func<bool> sessionIsCurrent = () => character != null && _subscribed && isActiveAndEnabled &&
                version == _subscriptionVersion && _character == character && _statId.Value == id;
            _valueListener = value =>
            {
                if (sessionIsCurrent() && ReferenceEquals(character.GetStatState(id), state))
                {
                    _onValue?.Invoke(value);
                }
            };
            _rebuiltListener = () =>
            {
                if (sessionIsCurrent())
                {
                    Unsubscribe();
                    SubscribeIfReady();
                }
            };
            character.RuntimeRebuilt += _rebuiltListener;
            if (state == null)
            {
                return;
            }

            state.AddListener(_valueListener);
            _onValue?.Invoke(state.CurrentValue);
        }

        private void Unsubscribe()
        {
            _subscriptionVersion++;
            if (!_subscribed)
            {
                return;
            }

            _state?.RemoveListener(_valueListener);
            _state = null;
            if (_boundCharacter != null)
            {
                _boundCharacter.RuntimeRebuilt -= _rebuiltListener;
            }
            _boundCharacter = null;
            _valueListener = null;
            _rebuiltListener = null;
            _subscribed = false;
        }
    }
}
