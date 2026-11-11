using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>When a <see cref="ModifierEventReaction" /> fires.</summary>
    public enum ModifierTrigger
    {
        /// <summary>Every time <see cref="ModifierEventReaction.EventId" /> fires on the owner (classic reaction).</summary>
        Event = 0,

        /// <summary>Every Nth matching event (<see cref="ModifierEventReaction.EveryN" />); the counter lives on the instance.</summary>
        EveryNth = 1,

        /// <summary>
        ///     Edge-triggered: a take_damage that drops the owner's health from at least
        ///     <see cref="ModifierEventReaction.Threshold" /> (fraction of max) to below it. Healing back above re-arms it.
        /// </summary>
        HealthBelow = 2,

        /// <summary>The owner gains <see cref="ModifierEventReaction.StateId" /> (after this instance was applied).</summary>
        StateEnter = 3,

        /// <summary>The owner loses <see cref="ModifierEventReaction.StateId" />.</summary>
        StateExit = 4,

        /// <summary>This modifier instance was created (not on refresh or stack).</summary>
        Applied = 5,

        /// <summary>This modifier instance was removed (expired, dispelled, spent or its owner died).</summary>
        Removed = 6
    }

    /// <summary>
    ///     Declarative reaction of a modifier: when its trigger fires on the owning unit, the <see cref="Effects" />
    ///     run through the normal effect pipeline (depth-capped). Event triggers listen to <see cref="EventId" />,
    ///     narrowed by the tag and ability filters; every trigger honours the charge limit, the internal cooldown and
    ///     the priority. All new fields default to the classic behaviour (fire on every matching event).
    /// </summary>
    [Serializable]
    public class ModifierEventReaction
    {
        [Tooltip("Event id from the open registry (see AbilityEvents for built-ins), e.g. take_damage. " +
                 "Used by the Event, EveryNth and HealthBelow triggers.")]
        public string EventId;

        [Tooltip("Effect nodes executed when the event fires. Caster = modifier caster, Target = modifier owner (or event source for FromSource).")]
        public List<EffectNodeData> Effects = new List<EffectNodeData>();

        [Tooltip("If true, effect targets resolve to the event source (e.g. the attacker) instead of the modifier owner.")]
        public bool TargetEventSource;

        [Header("Trigger (optional)")]
        [Tooltip("When the reaction fires: every event, every Nth event, a health threshold crossing, a state " +
                 "change, or this modifier being applied / removed.")]
        public ModifierTrigger Trigger = ModifierTrigger.Event;

        [Tooltip("EveryNth: fire on every Nth matching event (1 = every event).")]
        public int EveryN = 1;

        [Tooltip("HealthBelow: health fraction 0..1 of max (0.3 = 30%).")]
        [Range(0f, 1f)]
        public float Threshold = 0.3f;

        [Tooltip("StateEnter / StateExit: the boolean state watched on the owner (e.g. stunned).")]
        public string StateId;

        [Tooltip("Only events whose Tag matches (animation moment name...). Empty = any.")]
        public string TagFilter;

        [Tooltip("Only events of this ability id (e.g. a basic attack). Empty = any.")]
        public string AbilityFilter;

        [Tooltip("Damage hooks (before_deal_damage / before_take_damage) only for direct hits of casts and " +
                 "projectiles, not damage over time, reactions or reflection.")]
        public bool HitsOnly;

        [Tooltip("Charges: the reaction fires at most this many times per modifier instance. 0 = unlimited.")]
        [Min(0)]
        public int MaxTriggers;

        [Tooltip("Remove the modifier instance once its charges are spent (a mark consumed by the next N hits).")]
        public bool RemoveWhenSpent;

        [Tooltip("Internal cooldown in seconds between two fires of this reaction on one instance. 0 = none.")]
        [Min(0f)]
        public float Cooldown;

        [Tooltip("Ordering among the reactions on the same unit for the same event: higher runs first; ties keep " +
                 "modifier application order, then list order.")]
        public int Priority;

        /// <summary>True for triggers bound to <see cref="EventId" /> (Event, EveryNth, HealthBelow).</summary>
        public bool IsEventBound =>
            Trigger == ModifierTrigger.Event || Trigger == ModifierTrigger.EveryNth ||
            Trigger == ModifierTrigger.HealthBelow;

        /// <summary>True for triggers that need per-instance state (counters, charges, cooldowns, state edges).</summary>
        public bool NeedsState =>
            Trigger == ModifierTrigger.EveryNth || Trigger == ModifierTrigger.StateEnter ||
            Trigger == ModifierTrigger.StateExit || MaxTriggers > 0 || Cooldown > 0f;
    }
}
