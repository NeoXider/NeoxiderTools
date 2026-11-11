using System;
using System.Collections.Generic;

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

        /// <summary>Exhausted shield currently delivering its break receipt; reapplication must create a replacement.</summary>
        internal bool IsShieldBreaking;

        /// <summary>Tick executions so far; salts each tick's RNG seed so chance/crit rolls differ per tick.</summary>
        internal uint TickRollIndex;

        /// <summary>Event reactions executed so far; salts each reaction's RNG seed.</summary>
        internal uint ReactionRollIndex;

        /// <summary>Per-reaction trigger state (counters, charges, cooldowns, state edges); null when none is needed.</summary>
        internal ReactionState[] ReactionStates;

        internal const byte ReactiveTriggerFlag = 1;
        internal const byte StateWatcherTriggerFlag = 2;
        internal const byte BeforeDealTriggerFlag = 4;
        internal const byte BeforeTakeTriggerFlag = 8;
        internal const byte AttackLandedTriggerFlag = 16;
        internal const byte AttackReceivedTriggerFlag = 32;
        internal const byte PrioritizedTriggerFlag = 64;

        /// <summary>Reaction kinds of the blueprint captured at creation (engine bookkeeping).</summary>
        internal byte TriggerFlags;

        // WHY: what the applying cast paid, kept only for blueprints whose tick or reaction amounts scale with a
        // spent resource, so every other modifier stays allocation- and hash-identical.
        private AbilitySpentResource[] _spent;
        private int _spentCount;

        internal bool WatchesStates => (TriggerFlags & StateWatcherTriggerFlag) != 0;

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

        /// <summary>Adds time to a finite instance; the initial duration grows with it so NormalizedRemaining stays &lt;= 1.</summary>
        internal void Prolong(float seconds)
        {
            if (IsPermanent || !(seconds > 0f))
            {
                return;
            }

            RemainingDuration += seconds;
            if (RemainingDuration > InitialDuration)
            {
                InitialDuration = RemainingDuration;
            }
        }

        /// <summary>
        ///     Times reaction <paramref name="reactionIndex" /> has fired on this instance (charges consumed). 0 when the
        ///     reaction keeps no state.
        /// </summary>
        public int GetReactionFireCount(int reactionIndex)
        {
            return ReactionStates != null && reactionIndex >= 0 && reactionIndex < ReactionStates.Length
                ? ReactionStates[reactionIndex].Fired
                : 0;
        }

        /// <summary>Matching events counted by reaction <paramref name="reactionIndex" /> (EveryNth counter).</summary>
        public int GetReactionCounter(int reactionIndex)
        {
            return ReactionStates != null && reactionIndex >= 0 && reactionIndex < ReactionStates.Length
                ? ReactionStates[reactionIndex].Counter
                : 0;
        }

        /// <summary>Charges left on reaction <paramref name="reactionIndex" />, or -1 when it is unlimited.</summary>
        public int GetRemainingCharges(int reactionIndex)
        {
            List<ModifierEventReaction> reactions = Blueprint.EventReactions;
            if (reactions == null || reactionIndex < 0 || reactionIndex >= reactions.Count ||
                reactions[reactionIndex] == null || reactions[reactionIndex].MaxTriggers <= 0)
            {
                return -1;
            }

            int left = reactions[reactionIndex].MaxTriggers - GetReactionFireCount(reactionIndex);
            return left > 0 ? left : 0;
        }

        /// <summary>The state slot of a reaction, growing the array when the blueprint gained reactions.</summary>
        internal ref ReactionState StateFor(int reactionIndex)
        {
            if (ReactionStates == null || reactionIndex >= ReactionStates.Length)
            {
                int count = Blueprint.EventReactions != null ? Blueprint.EventReactions.Count : 0;
                ReactionState[] grown = new ReactionState[count > reactionIndex ? count : reactionIndex + 1];
                if (ReactionStates != null)
                {
                    Array.Copy(ReactionStates, grown, ReactionStates.Length);
                }

                ReactionStates = grown;
            }

            return ref ReactionStates[reactionIndex];
        }

        /// <summary>Re-captures the ability level (and re-resolves the leveled duration) on re-application.</summary>
        internal void RecaptureLevel(int abilityLevel)
        {
            AbilityLevel = abilityLevel < 1 ? 1 : abilityLevel;
            InitialDuration = Blueprint.ResolveDuration(AbilityLevel);
        }

        /// <summary>
        ///     Resource amounts the applying cast paid, captured at apply time (and re-captured by each re-application)
        ///     when the blueprint's tick or reaction effects scale with a spent resource; 0 otherwise.
        /// </summary>
        public int SpentCount => _spentCount;

        /// <summary>The <paramref name="index" />-th captured paid amount.</summary>
        public AbilitySpentResource GetSpentAt(int index)
        {
            if ((uint)index >= (uint)_spentCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _spent[index];
        }

        /// <summary>Total captured amount of <paramref name="resourceId" /> (case-insensitive); 0 when none.</summary>
        public float GetSpent(string resourceId)
        {
            float total = 0f;
            for (int i = 0; i < _spentCount; i++)
            {
                if (AbilityIdComparer.Same(_spent[i].ResourceId, resourceId))
                {
                    total += _spent[i].Amount;
                }
            }

            return total;
        }

        /// <summary>
        ///     Captures what <paramref name="source" />'s cast paid when the blueprint scales with it; a re-application
        ///     without paid amounts clears an earlier capture (the latest application wins, like the ability level).
        /// </summary>
        internal void CaptureSpent(EffectContext source)
        {
            int count = source != null ? source.SpentCount : 0;
            if (count == 0 || !LeveledValueResolver.UsesSpent(Blueprint, source.Specials))
            {
                _spentCount = 0;
                return;
            }

            if (_spent == null || _spent.Length < count)
            {
                _spent = new AbilitySpentResource[count];
            }

            for (int i = 0; i < count; i++)
            {
                _spent[i] = source.GetSpentAt(i);
            }

            _spentCount = count;
        }

        /// <summary>Replays the captured paid amounts into a tick or reaction context.</summary>
        internal void CopySpentTo(EffectContext context)
        {
            for (int i = 0; i < _spentCount; i++)
            {
                context.AddSpent(_spent[i].ResourceId, _spent[i].Amount);
            }
        }
    }

    /// <summary>Mutable trigger bookkeeping of one reaction on one modifier instance.</summary>
    internal struct ReactionState
    {
        public int Counter;
        public int Fired;
        public float ReadyAt;
        public bool StateValue;
    }
}
