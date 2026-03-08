using Neo;
using Neo.Reactive;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Rpg
{
    /// <summary>
    /// Reusable runtime target selector for AI, spells, and skill controllers.
    /// </summary>
    [NeoDoc("Rpg/RpgTargetSelector.md")]
    [CreateFromMenu("Neoxider/RPG/RpgTargetSelector")]
    [AddComponentMenu("Neoxider/RPG/" + nameof(RpgTargetSelector))]
    public sealed class RpgTargetSelector : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private Transform _origin;
        [SerializeField] private RpgCombatant _combatantSource;
        [SerializeField] private RpgStatsManager _profileSource;

        [Header("Targeting")]
        [SerializeField] private RpgTargetQuery _query = new();

        [Header("Reactive State")]
        public ReactivePropertyBool HasTargetState = new(false);

        [Header("Events")]
        [SerializeField] private RpgGameObjectEvent _onTargetSelected = new();
        [SerializeField] private UnityEvent _onTargetCleared = new();

        private GameObject _currentTarget;

        /// <summary>
        /// Gets the current selected target.
        /// </summary>
        public GameObject CurrentTarget => _currentTarget;

        /// <summary>
        /// Gets whether a target is currently selected.
        /// </summary>
        public bool HasTarget => _currentTarget != null;

        /// <summary>
        /// Selects a target using the configured query.
        /// </summary>
        [Button]
        public GameObject SelectTarget()
        {
            Transform source = _origin != null ? _origin : transform;
            _currentTarget = RpgTargetingUtility.SelectTarget(source, _query, ResolveReceiverFromGameObject);
            HasTargetState.Value = _currentTarget != null;
            if (_currentTarget != null)
            {
                _onTargetSelected?.Invoke(_currentTarget);
            }

            return _currentTarget;
        }

        /// <summary>
        /// Tries to select a target and returns whether one was found.
        /// </summary>
        public bool TrySelectTarget(out GameObject target)
        {
            target = SelectTarget();
            return target != null;
        }

        /// <summary>
        /// Clears the current target reference.
        /// </summary>
        [Button]
        public void ClearTarget()
        {
            _currentTarget = null;
            HasTargetState.Value = false;
            _onTargetCleared?.Invoke();
        }

        private IRpgCombatReceiver ResolveReceiverFromGameObject(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            if (target.TryGetComponent(out RpgCombatant combatant))
            {
                return combatant;
            }

            if (target.TryGetComponent(out RpgStatsManager manager))
            {
                return manager;
            }

            return null;
        }
    }
}
