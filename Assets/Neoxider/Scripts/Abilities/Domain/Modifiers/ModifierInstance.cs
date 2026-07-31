using System;

namespace Neo.Abilities
{
    /// <summary>
    ///     A live modifier attached to a unit. Owned and ticked by <see cref="ModifierEngine" />;
    ///     guaranteed to expire when its duration elapses. Never resurrected — re-application either
    ///     refreshes/stacks this instance or creates a new one, per the blueprint's stack policy.
    /// </summary>
    public sealed class ModifierInstance
    {
        internal float TickAccumulator;
        internal bool IsActiveInternal = true;

        /// <summary>Shield HP already consumed from this instance's shield_hp contribution (damage pipeline).</summary>
        internal float ShieldConsumed;

        internal ModifierInstance(int instanceId, ModifierBlueprint blueprint, UnitId caster, UnitId owner,
            string sourceAbilityId, int abilityLevel = 1)
        {
            InstanceId = instanceId;
            Blueprint = blueprint ?? throw new ArgumentNullException(nameof(blueprint));
            Caster = caster;
            Owner = owner;
            SourceAbilityId = sourceAbilityId;
            AbilityLevel = abilityLevel < 1 ? 1 : abilityLevel;
            Stacks = 1;
            InitialDuration = blueprint.ResolveDuration(AbilityLevel);
            RemainingDuration = InitialDuration;
        }

        public int InstanceId { get; }
        public ModifierBlueprint Blueprint { get; }
        public UnitId Caster { get; }
        public UnitId Owner { get; }
        public string SourceAbilityId { get; }

        /// <summary>Ability level captured at apply time; drives leveled tick/reaction values and duration.</summary>
        public int AbilityLevel { get; internal set; }

        public int Stacks { get; internal set; }

        /// <summary>Effective duration this instance was created with (resolves DurationByLevel by AbilityLevel).</summary>
        public float InitialDuration { get; private set; }

        public float RemainingDuration { get; internal set; }

        /// <summary>False once the instance is removed or expired; stale references must check this.</summary>
        public bool IsActive => IsActiveInternal;

        public bool IsPermanent => InitialDuration <= 0f;

        public float NormalizedRemaining =>
            IsPermanent ? 1f : RemainingDuration / InitialDuration;

        internal void RefreshDuration()
        {
            RemainingDuration = InitialDuration;
        }

        /// <summary>Re-captures the ability level (and re-resolves the leveled duration) on re-application.</summary>
        internal void RecaptureLevel(int abilityLevel)
        {
            AbilityLevel = abilityLevel < 1 ? 1 : abilityLevel;
            InitialDuration = Blueprint.ResolveDuration(AbilityLevel);
        }
    }
}
