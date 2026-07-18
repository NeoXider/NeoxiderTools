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

            ReactivePropertyFloat state = _character.GetStatState(_statId.Value);
            if (state == null)
            {
                return;
            }

            state.AddListener(HandleValue);
            _onValue?.Invoke(state.CurrentValue);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _character == null)
            {
                _subscribed = false;
                return;
            }

            _character.GetStatState(_statId.Value)?.RemoveListener(HandleValue);
            _subscribed = false;
        }

        private void HandleValue(float v)
        {
            _onValue?.Invoke(v);
        }
    }
}
