using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     Pure-data definition of a modifier (buff/debuff/DoT/aura payload/shield/stun...).
    ///     Serializable both as part of a <see cref="ScriptableObject" /> wrapper and over the network.
    ///     A modifier is the single durable-effect concept: it contributes properties, declares states,
    ///     ticks effects on an interval, and reacts to events declaratively.
    /// </summary>
    [Serializable]
    public class ModifierBlueprint
    {
        [Tooltip("Unique id of this modifier, e.g. 'burn', 'frost_slow'. Used for stacking and dispel.")]
        public string Id;

        [Tooltip("Display name for UI.")]
        public string DisplayName;

        [Tooltip("Seconds until the modifier expires. 0 or negative = permanent until removed.")]
        public float Duration;

        [Tooltip("Per-ability-level durations, resolved at apply time by the captured ability level. Empty ⇒ use Duration.")]
        public float[] DurationByLevel;

        [Tooltip("Stacking behavior when the same modifier id is applied again to the same unit.")]
        public ModifierStackPolicy StackPolicy = ModifierStackPolicy.Refresh;

        [Tooltip("Maximum stacks for the Stack policy. Ignored otherwise.")]
        public int MaxStacks = 1;

        [Tooltip("Property contributions while active (per stack scaling supported).")]
        public List<PropertyContribution> Properties = new List<PropertyContribution>();

        [Tooltip("Boolean states granted while active (any-true-wins), e.g. stunned, rooted.")]
        public List<string> States = new List<string>();

        [Tooltip("Seconds between effect ticks. 0 or negative = no ticking.")]
        public float TickInterval;

        [Tooltip("If true, the first tick fires immediately on application.")]
        public bool TickOnApply;

        [Tooltip("Effect nodes executed on every tick (e.g. periodic damage for a DoT).")]
        public List<EffectNodeData> TickEffects = new List<EffectNodeData>();

        [Tooltip("Declarative reactions to gameplay events on the owning unit (e.g. absorb on take_damage).")]
        public List<ModifierEventReaction> EventReactions = new List<ModifierEventReaction>();

        [Tooltip("Marks the modifier as negative for UI and dispel logic.")]
        public bool IsDebuff;

        [Tooltip("Can be removed by dispel effects.")]
        public bool Dispellable = true;

        [Tooltip("Optional presentation cue id consumed by presentation listeners (VFX/SFX hook).")]
        public string PresentationCue;

        public bool IsPermanent => Duration <= 0f;

        public bool HasTicks => TickInterval > 0f && TickEffects != null && TickEffects.Count > 0;

        /// <summary>
        ///     Resolves the effective duration for a captured ability level: <see cref="DurationByLevel" />
        ///     sampled by level when present, otherwise <see cref="Duration" />. Empty array ⇒ identical to today.
        /// </summary>
        public float ResolveDuration(int abilityLevel)
        {
            return LeveledValueResolver.SampleByLevel(DurationByLevel, abilityLevel, Duration);
        }
    }
}
