using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     Declarative reaction of a modifier to a gameplay event: when <see cref="EventId" /> fires on the
    ///     owning unit, the <see cref="Effects" /> run through the normal effect pipeline (depth-capped).
    /// </summary>
    [Serializable]
    public class ModifierEventReaction
    {
        [Tooltip("Event id from the open registry (see AbilityEvents for built-ins), e.g. take_damage.")]
        public string EventId;

        [Tooltip("Effect nodes executed when the event fires. Caster = modifier caster, Target = modifier owner (or event source for FromSource).")]
        public List<EffectNodeData> Effects = new List<EffectNodeData>();

        [Tooltip("If true, effect targets resolve to the event source (e.g. the attacker) instead of the modifier owner.")]
        public bool TargetEventSource;
    }
}
