using System;
using System.Collections.Generic;

namespace Neo.Abilities.Fixed
{
    /// <summary>One live application of a <see cref="FixedModifierBlueprint" /> on a unit; times are ticks.</summary>
    public sealed class FixedModifierInstance
    {
        internal int TickAccumulator;
        internal bool IsActiveInternal = true;
        internal bool IsShieldBreaking;
        internal uint TickRollIndex;
        internal uint ReactionRollIndex;
        internal FixedReactionState[] ReactionStates;
        internal byte TriggerFlags;

        internal const byte ReactiveFlag = 1;
        internal const byte StateWatcherFlag = 2;
        internal const byte BeforeDealFlag = 4;
        internal const byte BeforeTakeFlag = 8;
        internal const byte AttackLandedFlag = 16;
        internal const byte AttackReceivedFlag = 32;
        internal const byte PrioritizedFlag = 64;

        internal FixedModifierInstance(int instanceId, FixedModifierBlueprint blueprint, int caster, int owner,
            string sourceAbilityId, int abilityLevel)
        {
            InstanceId = instanceId;
            Blueprint = blueprint ?? throw new ArgumentNullException(nameof(blueprint));
            Caster = caster;
            Owner = owner;
            SourceAbilityId = sourceAbilityId;
            AbilityLevel = abilityLevel < 1 ? 1 : abilityLevel;
            Stacks = 1;
            InitialDurationTicks = blueprint.ResolveDurationTicks(AbilityLevel);
            RemainingTicks = InitialDurationTicks;
        }

        public int InstanceId { get; }
        public FixedModifierBlueprint Blueprint { get; }
        public int Caster { get; }
        public int Owner { get; }
        public string SourceAbilityId { get; }
        public int AbilityLevel { get; internal set; }
        public int Stacks { get; internal set; }
        public int InitialDurationTicks { get; private set; }
        public int RemainingTicks { get; internal set; }
        public int ShieldConsumedRaw { get; internal set; }
        public bool IsActive => IsActiveInternal;
        public bool IsPermanent => InitialDurationTicks <= 0;

        internal bool WatchesStates => (TriggerFlags & StateWatcherFlag) != 0;

        public int GetReactionFireCount(int reactionIndex)
        {
            return ReactionStates != null && reactionIndex >= 0 && reactionIndex < ReactionStates.Length
                ? ReactionStates[reactionIndex].Fired
                : 0;
        }

        public int GetReactionCounter(int reactionIndex)
        {
            return ReactionStates != null && reactionIndex >= 0 && reactionIndex < ReactionStates.Length
                ? ReactionStates[reactionIndex].Counter
                : 0;
        }

        /// <summary>Charges left of a reaction with MaxTriggers; -1 when it has no charge limit.</summary>
        public int GetRemainingCharges(int reactionIndex)
        {
            List<FixedModifierReaction> reactions = Blueprint.EventReactions;
            if (reactions == null || reactionIndex < 0 || reactionIndex >= reactions.Count ||
                reactions[reactionIndex] == null || reactions[reactionIndex].MaxTriggers <= 0)
            {
                return -1;
            }

            int left = reactions[reactionIndex].MaxTriggers - GetReactionFireCount(reactionIndex);
            return left > 0 ? left : 0;
        }

        internal ref FixedReactionState StateFor(int reactionIndex)
        {
            if (ReactionStates == null || reactionIndex >= ReactionStates.Length)
            {
                int count = Blueprint.EventReactions != null ? Blueprint.EventReactions.Count : 0;
                FixedReactionState[] grown = new FixedReactionState[count > reactionIndex ? count : reactionIndex + 1];
                if (ReactionStates != null)
                {
                    Array.Copy(ReactionStates, grown, ReactionStates.Length);
                }

                ReactionStates = grown;
            }

            return ref ReactionStates[reactionIndex];
        }

        internal void RecaptureLevel(int abilityLevel)
        {
            AbilityLevel = abilityLevel < 1 ? 1 : abilityLevel;
            InitialDurationTicks = Blueprint.ResolveDurationTicks(AbilityLevel);
        }

        internal void RefreshDuration()
        {
            RemainingTicks = InitialDurationTicks;
        }

        internal void Prolong(int ticks)
        {
            if (IsPermanent || ticks <= 0)
            {
                return;
            }

            RemainingTicks = FixedMath.SaturatingAdd(RemainingTicks, ticks);
            if (RemainingTicks > InitialDurationTicks)
            {
                InitialDurationTicks = RemainingTicks;
            }
        }
    }

    internal struct FixedReactionState
    {
        public int Counter;
        public int Fired;
        public long ReadyAtTick;
        public bool StateValue;
    }

    /// <summary>Result of applying a modifier: the instance (new or re-applied) and whether it was created.</summary>
    public readonly struct FixedModifierApplyResult
    {
        public readonly FixedModifierInstance Instance;
        public readonly bool CreatedNew;

        public FixedModifierApplyResult(FixedModifierInstance instance, bool createdNew)
        {
            Instance = instance;
            CreatedNew = createdNew;
        }
    }

    /// <summary>
    ///     Live modifiers of a fixed system: stack policies through <see cref="FixedRules.Stacking" />, interval ticks,
    ///     guaranteed expiry, dispel queries, property and state feeds. Ticks walk owners in the order they first
    ///     received a modifier; re-entrancy safe.
    /// </summary>
    public sealed class FixedModifierEngine
    {
        private const int MaxTicksPerStep = 8;

        private readonly Dictionary<int, OwnerState> _owners = new Dictionary<int, OwnerState>();
        private readonly List<OwnerState> _ownerOrder = new List<OwnerState>(16);
        private readonly List<FixedModifierInstance> _tickScratch = new List<FixedModifierInstance>(32);
        private readonly List<List<FixedModifierInstance>> _removalScratch = new List<List<FixedModifierInstance>>(2);
        private int _removalDepth;
        private int _nextInstanceId = 1;
        private int _versionCounter;
        private int _beforeDealTotal;
        private int _beforeTakeTotal;
        private int _attackLandedTotal;
        private int _attackReceivedTotal;
        private int _prioritizedTotal;

        internal FixedModifierEngine(FixedAbilitySystem system)
        {
            System = system;
        }

        public FixedAbilitySystem System { get; }

        internal Action<FixedModifierInstance, bool> Applied;
        internal Action<FixedModifierInstance, bool> Removed;
        internal Action<FixedModifierInstance> TickDue;

        /// <summary>Changes on every change of the owner's modifier set (property caches compare it).</summary>
        public int VersionOf(int owner)
        {
            return _owners.TryGetValue(owner, out OwnerState state) ? state.Version : 0;
        }

        /// <summary>Drops the bookkeeping of an owner without live modifiers (called when a unit is destroyed).</summary>
        public void ForgetOwner(int owner)
        {
            if (_owners.TryGetValue(owner, out OwnerState state) && state.List.Count == 0)
            {
                _owners.Remove(owner);
                _ownerOrder.Remove(state);
            }
        }

        public FixedModifierApplyResult Apply(FixedModifierBlueprint blueprint, int caster, int owner,
            string sourceAbilityId = null, int abilityLevel = 1)
        {
            if (blueprint == null || string.IsNullOrEmpty(blueprint.Id) || owner == 0)
            {
                return new FixedModifierApplyResult(null, false);
            }

            OwnerState state = GetOrCreate(owner);
            List<FixedModifierInstance> list = state.List;
            FixedModifierInstance existing = null;
            int activeInstances = 0;
            for (int i = 0; i < list.Count; i++)
            {
                FixedModifierInstance m = list[i];
                if (m.IsActiveInternal && !m.IsShieldBreaking && FixedIdComparer.Same(m.Blueprint.Id, blueprint.Id))
                {
                    activeInstances++;
                    existing ??= m;
                }
            }

            FixedStackAction action = System.Rules.Stacking.Decide(blueprint, existing, activeInstances);
            if (existing == null && action != FixedStackAction.Ignore)
            {
                action = FixedStackAction.CreateNew;
            }

            switch (action)
            {
                case FixedStackAction.Ignore:
                    return new FixedModifierApplyResult(existing, false);
                case FixedStackAction.Refresh:
                case FixedStackAction.AddStack:
                case FixedStackAction.Prolong:
                    if (action == FixedStackAction.AddStack)
                    {
                        int max = blueprint.MaxStacks > 0 ? blueprint.MaxStacks : int.MaxValue;
                        if (existing.Stacks < max)
                        {
                            existing.Stacks++;
                        }
                    }

                    if (action == FixedStackAction.Prolong)
                    {
                        int remaining = existing.RemainingTicks;
                        existing.RecaptureLevel(abilityLevel);
                        existing.RemainingTicks = remaining;
                        existing.Prolong(existing.InitialDurationTicks);
                    }
                    else
                    {
                        existing.RecaptureLevel(abilityLevel);
                        existing.RefreshDuration();
                    }

                    if (blueprint.RestartTicksOnRefresh)
                    {
                        existing.TickAccumulator = 0;
                    }

                    Bump(state);
                    Applied?.Invoke(existing, false);
                    return new FixedModifierApplyResult(existing, false);
                case FixedStackAction.ReplaceOldest:
                    RemoveInternal(existing, false);
                    break;
            }

            FixedModifierInstance instance =
                new FixedModifierInstance(_nextInstanceId++, blueprint, caster, owner, sourceAbilityId, abilityLevel);
            if (blueprint.TickOnApply && blueprint.HasTicks)
            {
                instance.TickAccumulator = blueprint.TickIntervalTicks;
            }

            instance.TriggerFlags = ComputeTriggerFlags(blueprint, out bool needsState);
            if (needsState)
            {
                instance.ReactionStates = new FixedReactionState[blueprint.EventReactions.Count];
            }

            state.List.Add(instance);
            CountTriggers(state, instance.TriggerFlags, 1);
            Bump(state);
            Applied?.Invoke(instance, true);
            return new FixedModifierApplyResult(instance, true);
        }

        public bool Remove(FixedModifierInstance instance)
        {
            return RemoveInternal(instance, false);
        }

        /// <summary>Removes the first (or every) active instance with this id.</summary>
        public int RemoveById(int owner, string modifierId, bool allInstances = true)
        {
            return string.IsNullOrEmpty(modifierId) ? 0 : RemoveMatching(owner, null, modifierId, allInstances);
        }

        /// <summary>Removes every active instance matching the predicate (cache the predicate to stay allocation-free).</summary>
        public int RemoveWhere(int owner, Predicate<FixedModifierInstance> predicate)
        {
            return predicate == null ? 0 : RemoveMatching(owner, predicate, null, true);
        }

        public int RemoveAllFrom(int owner)
        {
            return RemoveMatching(owner, null, null, true);
        }

        public void GetModifiers(int owner, List<FixedModifierInstance> results)
        {
            results.Clear();
            if (_owners.TryGetValue(owner, out OwnerState state))
            {
                List<FixedModifierInstance> list = state.List;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].IsActiveInternal)
                    {
                        results.Add(list[i]);
                    }
                }
            }
        }

        /// <summary>The oldest active instance of this id on the owner.</summary>
        public bool TryGetActive(int owner, string modifierId, out FixedModifierInstance instance)
        {
            instance = null;
            if (string.IsNullOrEmpty(modifierId) || !_owners.TryGetValue(owner, out OwnerState state))
            {
                return false;
            }

            List<FixedModifierInstance> list = state.List;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].IsActiveInternal && FixedIdComparer.Same(list[i].Blueprint.Id, modifierId))
                {
                    instance = list[i];
                    return true;
                }
            }

            return false;
        }

        public int CountActive(int owner, string modifierId)
        {
            int count = 0;
            if (!string.IsNullOrEmpty(modifierId) && _owners.TryGetValue(owner, out OwnerState state))
            {
                List<FixedModifierInstance> list = state.List;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].IsActiveInternal && FixedIdComparer.Same(list[i].Blueprint.Id, modifierId))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        public bool HasState(int owner, string stateId)
        {
            if (!_owners.TryGetValue(owner, out OwnerState state))
            {
                return false;
            }

            List<FixedModifierInstance> list = state.List;
            for (int i = 0; i < list.Count; i++)
            {
                FixedModifierInstance m = list[i];
                List<string> states = m.Blueprint.States;
                if (!m.IsActiveInternal || states == null)
                {
                    continue;
                }

                for (int s = 0; s < states.Count; s++)
                {
                    if (FixedIdComparer.Same(states[s], stateId))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void CollectContributions(int owner, string propertyId, List<FixedResolvedContribution> results)
        {
            if (!_owners.TryGetValue(owner, out OwnerState state))
            {
                return;
            }

            List<FixedModifierInstance> list = state.List;
            for (int i = 0; i < list.Count; i++)
            {
                FixedModifierInstance m = list[i];
                List<FixedPropertyContribution> properties = m.Blueprint.Properties;
                if (!m.IsActiveInternal || properties == null)
                {
                    continue;
                }

                for (int p = 0; p < properties.Count; p++)
                {
                    FixedPropertyContribution c = properties[p];
                    if (FixedIdComparer.Same(c.PropertyId, propertyId))
                    {
                        results.Add(new FixedResolvedContribution(c.Op, c.ValueForStacks(m.Stacks), c.Denominator));
                    }
                }
            }
        }

        public bool HasReactions(int owner)
        {
            return _owners.TryGetValue(owner, out OwnerState state) && state.Counts.Reactive > 0;
        }

        public bool HasStateWatchers(int owner)
        {
            return _owners.TryGetValue(owner, out OwnerState state) && state.Counts.StateWatchers > 0;
        }

        internal bool HasPrioritizedReactions(int owner)
        {
            return _prioritizedTotal > 0 && _owners.TryGetValue(owner, out OwnerState state) &&
                   state.Counts.Prioritized > 0;
        }

        /// <summary>True when an active modifier of the owner reacts (Event, EveryNth, HealthBelow) to this event.</summary>
        public bool ReactsTo(int owner, string eventId)
        {
            byte hook = HookFlag(eventId);
            if (hook != 0 && TotalFor(hook) == 0)
            {
                return false;
            }

            if (!_owners.TryGetValue(owner, out OwnerState state) || state.Counts.Reactive <= 0)
            {
                return false;
            }

            switch (hook)
            {
                case FixedModifierInstance.BeforeDealFlag:
                    return state.Counts.BeforeDeal > 0;
                case FixedModifierInstance.BeforeTakeFlag:
                    return state.Counts.BeforeTake > 0;
                case FixedModifierInstance.AttackLandedFlag:
                    return state.Counts.AttackLanded > 0;
                case FixedModifierInstance.AttackReceivedFlag:
                    return state.Counts.AttackReceived > 0;
            }

            List<FixedModifierInstance> list = state.List;
            for (int i = 0; i < list.Count; i++)
            {
                FixedModifierInstance m = list[i];
                List<FixedModifierReaction> reactions = m.Blueprint.EventReactions;
                if (!m.IsActiveInternal || reactions == null)
                {
                    continue;
                }

                for (int r = 0; r < reactions.Count; r++)
                {
                    FixedModifierReaction reaction = reactions[r];
                    if (reaction != null && reaction.IsEventBound && FixedIdComparer.Same(reaction.EventId, eventId))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Advances every modifier by one tick: interval ticks first, then expiry.</summary>
        internal void Step()
        {
            _tickScratch.Clear();
            for (int o = 0; o < _ownerOrder.Count; o++)
            {
                List<FixedModifierInstance> list = _ownerOrder[o].List;
                for (int i = 0; i < list.Count; i++)
                {
                    _tickScratch.Add(list[i]);
                }
            }

            for (int i = 0; i < _tickScratch.Count; i++)
            {
                FixedModifierInstance m = _tickScratch[i];
                if (!m.IsActiveInternal)
                {
                    continue;
                }

                if (m.Blueprint.HasTicks)
                {
                    m.TickAccumulator++;
                    int interval = m.Blueprint.TickIntervalTicks;
                    int safety = MaxTicksPerStep;
                    while (m.IsActiveInternal && m.TickAccumulator >= interval && safety-- > 0)
                    {
                        m.TickAccumulator -= interval;
                        TickDue?.Invoke(m);
                    }
                }

                if (m.IsActiveInternal && !m.IsPermanent)
                {
                    m.RemainingTicks--;
                    if (m.RemainingTicks <= 0)
                    {
                        RemoveInternal(m, true);
                    }
                }
            }

            _tickScratch.Clear();
        }

        internal void GetOwnersInOrder(List<int> results)
        {
            results.Clear();
            for (int i = 0; i < _ownerOrder.Count; i++)
            {
                results.Add(_ownerOrder[i].Owner);
            }
        }

        private int RemoveMatching(int owner, Predicate<FixedModifierInstance> predicate, string modifierId,
            bool allInstances)
        {
            if (!_owners.ContainsKey(owner))
            {
                return 0;
            }

            while (_removalScratch.Count <= _removalDepth)
            {
                _removalScratch.Add(new List<FixedModifierInstance>(8));
            }

            List<FixedModifierInstance> snapshot = _removalScratch[_removalDepth++];
            try
            {
                GetModifiers(owner, snapshot);
                int removed = 0;
                for (int i = snapshot.Count - 1; i >= 0; i--)
                {
                    FixedModifierInstance m = snapshot[i];
                    if (!m.IsActiveInternal)
                    {
                        continue;
                    }

                    bool matches = predicate != null
                        ? predicate(m)
                        : modifierId == null || FixedIdComparer.Same(m.Blueprint.Id, modifierId);
                    if (matches && RemoveInternal(m, false))
                    {
                        removed++;
                        if (!allInstances)
                        {
                            break;
                        }
                    }
                }

                return removed;
            }
            finally
            {
                snapshot.Clear();
                _removalDepth--;
            }
        }

        private bool RemoveInternal(FixedModifierInstance instance, bool expired)
        {
            if (instance == null || !instance.IsActiveInternal || !_owners.TryGetValue(instance.Owner, out OwnerState state) ||
                !state.List.Remove(instance))
            {
                return false;
            }

            instance.IsActiveInternal = false;
            CountTriggers(state, instance.TriggerFlags, -1);
            Bump(state);
            Removed?.Invoke(instance, expired);
            return true;
        }

        private OwnerState GetOrCreate(int owner)
        {
            if (!_owners.TryGetValue(owner, out OwnerState state))
            {
                state = new OwnerState(owner);
                _owners[owner] = state;
                _ownerOrder.Add(state);
            }

            return state;
        }

        private void Bump(OwnerState state)
        {
            state.Version = ++_versionCounter;
        }

        private static byte ComputeTriggerFlags(FixedModifierBlueprint blueprint, out bool needsState)
        {
            needsState = false;
            List<FixedModifierReaction> reactions = blueprint.EventReactions;
            if (reactions == null || reactions.Count == 0)
            {
                return 0;
            }

            byte flags = 0;
            for (int i = 0; i < reactions.Count; i++)
            {
                FixedModifierReaction reaction = reactions[i];
                if (reaction == null)
                {
                    continue;
                }

                flags |= FixedModifierInstance.ReactiveFlag;
                if (reaction.Trigger == FixedModifierTrigger.StateEnter || reaction.Trigger == FixedModifierTrigger.StateExit)
                {
                    flags |= FixedModifierInstance.StateWatcherFlag;
                }

                if (reaction.IsEventBound)
                {
                    flags |= HookFlag(reaction.EventId);
                }

                if (reaction.Priority != 0)
                {
                    flags |= FixedModifierInstance.PrioritizedFlag;
                }

                needsState |= reaction.NeedsState;
            }

            return flags;
        }

        private static byte HookFlag(string eventId)
        {
            if (ReferenceEquals(eventId, FixedEvents.BeforeDealDamage) || FixedIdComparer.Same(eventId, FixedEvents.BeforeDealDamage))
            {
                return FixedModifierInstance.BeforeDealFlag;
            }

            if (ReferenceEquals(eventId, FixedEvents.BeforeTakeDamage) || FixedIdComparer.Same(eventId, FixedEvents.BeforeTakeDamage))
            {
                return FixedModifierInstance.BeforeTakeFlag;
            }

            if (ReferenceEquals(eventId, FixedEvents.AttackLanded) || FixedIdComparer.Same(eventId, FixedEvents.AttackLanded))
            {
                return FixedModifierInstance.AttackLandedFlag;
            }

            return ReferenceEquals(eventId, FixedEvents.AttackReceived) ||
                   FixedIdComparer.Same(eventId, FixedEvents.AttackReceived)
                ? FixedModifierInstance.AttackReceivedFlag
                : (byte)0;
        }

        private int TotalFor(byte hook)
        {
            switch (hook)
            {
                case FixedModifierInstance.BeforeDealFlag:
                    return _beforeDealTotal;
                case FixedModifierInstance.BeforeTakeFlag:
                    return _beforeTakeTotal;
                case FixedModifierInstance.AttackLandedFlag:
                    return _attackLandedTotal;
                default:
                    return _attackReceivedTotal;
            }
        }

        private void CountTriggers(OwnerState state, byte flags, int delta)
        {
            if (flags == 0)
            {
                return;
            }

            if ((flags & FixedModifierInstance.ReactiveFlag) != 0)
            {
                state.Counts.Reactive += delta;
            }

            if ((flags & FixedModifierInstance.StateWatcherFlag) != 0)
            {
                state.Counts.StateWatchers += delta;
            }

            if ((flags & FixedModifierInstance.BeforeDealFlag) != 0)
            {
                state.Counts.BeforeDeal += delta;
                _beforeDealTotal += delta;
            }

            if ((flags & FixedModifierInstance.BeforeTakeFlag) != 0)
            {
                state.Counts.BeforeTake += delta;
                _beforeTakeTotal += delta;
            }

            if ((flags & FixedModifierInstance.AttackLandedFlag) != 0)
            {
                state.Counts.AttackLanded += delta;
                _attackLandedTotal += delta;
            }

            if ((flags & FixedModifierInstance.AttackReceivedFlag) != 0)
            {
                state.Counts.AttackReceived += delta;
                _attackReceivedTotal += delta;
            }

            if ((flags & FixedModifierInstance.PrioritizedFlag) != 0)
            {
                state.Counts.Prioritized += delta;
                _prioritizedTotal += delta;
            }
        }

        private sealed class OwnerState
        {
            public readonly int Owner;
            public readonly List<FixedModifierInstance> List = new List<FixedModifierInstance>(4);
            public int Version;
            public TriggerCounts Counts;

            public OwnerState(int owner)
            {
                Owner = owner;
            }
        }

        private struct TriggerCounts
        {
            public int Reactive;
            public int StateWatchers;
            public int BeforeDeal;
            public int BeforeTake;
            public int AttackLanded;
            public int AttackReceived;
            public int Prioritized;
        }
    }
}
