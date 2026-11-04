using System;
using Neo.Reactive;
using Neo.Rpg.Components;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Rpg
{
    /// <summary>
    ///     Reusable runtime target selector for AI, spells, and skill controllers.
    /// </summary>
    [NeoDoc("Rpg/RpgTargetSelector.md")]
    [CreateFromMenu("Neoxider/RPG/RpgTargetSelector")]
    [AddComponentMenu("Neoxider/RPG/" + nameof(RpgTargetSelector))]
    public sealed class RpgTargetSelector : MonoBehaviour
    {
        [Header("Setup")] [SerializeField] private Transform _origin;

        [SerializeField] private RpgCharacter _characterSource;

        [Header("Targeting")] [SerializeField] private RpgTargetQuery _query = new();

        [Header("Reactive State")] public ReactivePropertyBool HasTargetState = new(false);

        [Header("Events")] [SerializeField] private RpgGameObjectEvent _onTargetSelected = new();

        [SerializeField] private UnityEvent _onTargetCleared = new();

        // WHY: cached; passing the method group would allocate a new delegate on every selection.
        private static readonly Func<GameObject, IRpgCombatReceiver> ResolveReceiver = ResolveReceiverFromGameObject;

        /// <summary>
        ///     Gets or sets the targeting query (range, layers, selection mode, filters) used by <see cref="SelectTarget" />.
        /// </summary>
        public RpgTargetQuery Query
        {
            get => _query;
            set => _query = value ?? new RpgTargetQuery();
        }

        /// <summary>
        ///     Gets the current selected target.
        /// </summary>
        public GameObject CurrentTarget { get; private set; }

        /// <summary>
        ///     Gets whether a target is currently selected.
        /// </summary>
        public bool HasTarget => CurrentTarget != null;

        /// <summary>
        ///     Selects a target using the configured query.
        /// </summary>
        [Button]
        public GameObject SelectTarget()
        {
            Transform source = _origin != null ? _origin : transform;
            CurrentTarget = RpgTargetingUtility.SelectTarget(source, _query, ResolveReceiver, _characterSource);
            HasTargetState.Value = CurrentTarget != null;
            if (CurrentTarget != null)
            {
                _onTargetSelected?.Invoke(CurrentTarget);
            }

            return CurrentTarget;
        }

        /// <summary>
        ///     Tries to select a target and returns whether one was found.
        /// </summary>
        public bool TrySelectTarget(out GameObject target)
        {
            target = SelectTarget();
            return target != null;
        }

        /// <summary>
        ///     Clears the current target reference.
        /// </summary>
        [Button]
        public void ClearTarget()
        {
            CurrentTarget = null;
            HasTargetState.Value = false;
            _onTargetCleared?.Invoke();
        }

        private static IRpgCombatReceiver ResolveReceiverFromGameObject(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            return RpgAttackController.TryResolveReceiver(target, out IRpgCombatReceiver receiver) ? receiver : null;
        }
    }
}
