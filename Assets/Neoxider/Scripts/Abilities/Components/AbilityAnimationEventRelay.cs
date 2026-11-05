using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     Forwards animation moments from an Animator on a child object to the unit's domain: Animation Events and
    ///     Timeline signals call <see cref="Raise" /> with a moment name (e.g. "hit_frame"), which becomes an
    ///     animation_event on the unit. Reactions with EventId animation_event and that TagFilter fire.
    /// </summary>
    [NeoDoc("Abilities/AbilityUnitBehaviour.md")]
    [AddComponentMenu("Neoxider/Abilities/Ability Animation Event Relay")]
    public sealed class AbilityAnimationEventRelay : MonoBehaviour
    {
        [Tooltip("Unit receiving the moments. Empty = the first AbilityUnitBehaviour in the parents.")]
        [SerializeField] private AbilityUnitBehaviour _unit;

        public AbilityUnitBehaviour Unit => _unit != null ? _unit : _unit = GetComponentInParent<AbilityUnitBehaviour>();

        /// <summary>Raises the moment on the unit (no-op without a registered unit).</summary>
        public void Raise(string moment)
        {
            AbilityUnitBehaviour unit = Unit;
            if (unit != null)
            {
                unit.RaiseAnimationEvent(moment);
            }
        }
    }
}
