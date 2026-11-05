using System;
using System.Collections.Generic;

namespace Neo.Abilities
{
    /// <summary>
    ///     Owns every live <see cref="ModifierInstance" /> of one <see cref="AbilitySystem" />:
    ///     application with stack policies, interval ticks, guaranteed expiry, dispel queries,
    ///     and property/state aggregation feeds. Deterministic: driven only by <see cref="Tick" />.
    ///     Re-entrancy safe: tick/removal callbacks may apply or remove modifiers.
    /// </summary>
    public sealed class ModifierEngine
    {
        private readonly Dictionary<UnitId, List<ModifierInstance>> _byOwner =
            new Dictionary<UnitId, List<ModifierInstance>>();

        private readonly Dictionary<UnitId, int> _versions = new Dictionary<UnitId, int>();

        // WHY: per-owner counts of reacting / state-watching instances let every publication skip owners whose
        // modifiers cannot react, without copying their modifier list.
        private readonly Dictionary<UnitId, TriggerCounts> _triggerCounts = new Dictionary<UnitId, TriggerCounts>();
        private int _beforeDealTotal;
        private int _beforeTakeTotal;
        private int _attackLandedTotal;
        private int _attackReceivedTotal;
        private int _prioritizedTotal;
        private const byte ReactiveFlag = ModifierInstance.ReactiveTriggerFlag;
        private const byte StateWatcherFlag = ModifierInstance.StateWatcherTriggerFlag;
        private readonly List<ModifierInstance> _tickScratch = new List<ModifierInstance>(32);
        private readonly List<List<ModifierInstance>> _removalScratchPool = new List<List<ModifierInstance>>(2);

        // WHY: DoT and slow churn empties and refills owner lists constantly; recycling them leaves the
        // ModifierInstance as the only allocation of a fresh application.
        private readonly List<List<ModifierInstance>> _ownerListPool = new List<List<ModifierInstance>>(16);
        private const int MaxPooledOwnerLists = 256;
        private int _removalDepth;
        private int _nextInstanceId = 1;

        // WHY: versions come from one engine-wide counter, so a value is never handed out twice and
        // a forgotten owner can restart at 0 without matching a stale property cache entry.
        private int _versionCounter;

        /// <summary>
        ///     Rules this engine reads its stacking decision from; set by the owning <see cref="AbilitySystem" />.
        ///     Null (a standalone engine) uses <see cref="StandardModifierStackingRule" />.
        /// </summary>
        public AbilityRules Rules { get; set; }

        /// <summary>Fired after an instance is applied (new) or re-applied (refresh/stack).</summary>
        public event Action<ModifierInstance, bool> Applied;

        /// <summary>Fired after an instance is removed. Second argument is true when it expired naturally.</summary>
        public event Action<ModifierInstance, bool> Removed;

        /// <summary>Fired when an instance's tick interval elapses. The system executes its tick effects.</summary>
        public event Action<ModifierInstance> TickDue;

        /// <summary>
        ///     Version of an owner's modifier set, used for property caches. Compare values for equality only:
        ///     they come from one engine-wide counter, so they change (and jump) on every change of this
        ///     owner's set, are never handed out twice, and drop back to 0 after <see cref="ForgetOwner" />.
        ///     0 means the owner has no recorded change (never modified, or forgotten after destruction).
        /// </summary>
        public int VersionOf(UnitId owner)
        {
            return _versions.TryGetValue(owner, out int v) ? v : 0;
        }

        /// <summary>
        ///     Drops the version entry of an owner that no longer exists. Ignored while the owner still has
        ///     live modifiers. <see cref="AbilitySystem.DestroyUnit" /> calls it for you.
        /// </summary>
        public void ForgetOwner(UnitId owner)
        {
            if (!_byOwner.ContainsKey(owner))
            {
                _versions.Remove(owner);
            }
        }

        /// <summary>Number of owners whose version is currently tracked (diagnostics and leak tests).</summary>
        public int TrackedOwnerCount => _versions.Count;

        /// <summary>
        ///     Applies a modifier to <paramref name="owner" />. What a re-application does is decided by
        ///     <see cref="AbilityRules.Stacking" /> (default: the blueprint's <see cref="ModifierStackPolicy" />).
        ///     An ignored re-application returns the existing instance with <c>CreatedNew = false</c> and fires nothing.
        /// </summary>
        public ModifierApplyResult Apply(ModifierBlueprint blueprint, UnitId caster, UnitId owner,
            string sourceAbilityId = null, int abilityLevel = 1)
        {
            if (blueprint == null || string.IsNullOrEmpty(blueprint.Id) || !owner.IsValid)
            {
                return new ModifierApplyResult(null, false);
            }

            List<ModifierInstance> list = GetOrCreateList(owner);
            ModifierInstance existing = null;
            int activeInstances = 0;
            for (int i = 0; i < list.Count; i++)
            {
                ModifierInstance m = list[i];
                if (m.IsActiveInternal && !m.IsShieldBreaking && AbilityIdComparer.Same(m.Blueprint.Id, blueprint.Id))
                {
                    activeInstances++;
                    existing ??= m;
                }
            }

            IModifierStackingRule rule = Rules != null ? Rules.Stacking : StandardModifierStackingRule.Instance;
            ModifierStackAction action = rule.Decide(blueprint, existing, activeInstances);
            if (existing == null && action != ModifierStackAction.Ignore)
            {
                action = ModifierStackAction.CreateNew;
            }

            switch (action)
            {
                case ModifierStackAction.Ignore:
                    if (list.Count == 0)
                    {
                        ReleaseEmptyList(owner, list);
                    }

                    return new ModifierApplyResult(existing, false);

                case ModifierStackAction.Refresh:
                case ModifierStackAction.AddStack:
                case ModifierStackAction.Prolong:
                    if (action == ModifierStackAction.AddStack)
                    {
                        int max = blueprint.MaxStacks > 0 ? blueprint.MaxStacks : int.MaxValue;
                        if (existing.Stacks < max)
                        {
                            existing.Stacks++;
                        }
                    }

                    if (action == ModifierStackAction.Prolong)
                    {
                        float remaining = existing.RemainingDuration;
                        existing.RecaptureLevel(abilityLevel);
                        existing.RemainingDuration = remaining;
                        existing.Prolong(existing.InitialDuration);
                    }
                    else
                    {
                        // WHY: a re-application re-captures the applying ability level (and its leveled duration).
                        existing.RecaptureLevel(abilityLevel);
                        existing.RefreshDuration();
                    }

                    BumpVersion(owner);
                    Applied?.Invoke(existing, false);
                    return new ModifierApplyResult(existing, false);

                case ModifierStackAction.ReplaceOldest:
                    RemoveInternal(existing, false);
                    list = GetOrCreateList(owner);
                    break;
            }

            ModifierInstance instance = new ModifierInstance(_nextInstanceId++, blueprint, caster, owner, sourceAbilityId,
                abilityLevel);
            if (blueprint.TickOnApply && blueprint.HasTicks)
            {
                // WHY: primes the accumulator so the first Tick fires immediately.
                instance.TickAccumulator = blueprint.TickInterval;
            }

            instance.TriggerFlags = ComputeTriggerFlags(blueprint, out bool needsState);
            if (needsState)
            {
                instance.ReactionStates = new ReactionState[blueprint.EventReactions.Count];
            }

            list.Add(instance);
            CountTriggers(owner, instance.TriggerFlags, 1);
            BumpVersion(owner);
            Applied?.Invoke(instance, true);
            return new ModifierApplyResult(instance, true);
        }

        /// <summary>True when an active modifier of <paramref name="owner" /> has event reactions or triggers.</summary>
        public bool HasReactions(UnitId owner)
        {
            return _triggerCounts.TryGetValue(owner, out TriggerCounts counts) && counts.Reactive > 0;
        }

        /// <summary>
        ///     True when an active modifier of <paramref name="owner" /> has an event-bound reaction (Event, EveryNth,
        ///     HealthBelow) to <paramref name="eventId" />. Scans the owner's modifiers without copying them.
        /// </summary>
        public bool ReactsTo(UnitId owner, string eventId)
        {
            // WHY: the damage pipeline asks for these on every hit; engine-wide totals answer without a lookup
            // while no modifier anywhere listens, per-owner counts without a scan otherwise.
            byte hook = HookFlag(eventId);
            if (hook != 0 && TotalFor(hook) == 0)
            {
                return false;
            }

            if (!_triggerCounts.TryGetValue(owner, out TriggerCounts counts) || counts.Reactive <= 0)
            {
                return false;
            }

            switch (hook)
            {
                case ModifierInstance.BeforeDealTriggerFlag:
                    return counts.BeforeDeal > 0;
                case ModifierInstance.BeforeTakeTriggerFlag:
                    return counts.BeforeTake > 0;
                case ModifierInstance.AttackLandedTriggerFlag:
                    return counts.AttackLanded > 0;
                case ModifierInstance.AttackReceivedTriggerFlag:
                    return counts.AttackReceived > 0;
            }

            if (!_byOwner.TryGetValue(owner, out List<ModifierInstance> list))
            {
                return false;
            }

            for (int i = 0; i < list.Count; i++)
            {
                ModifierInstance m = list[i];
                List<ModifierEventReaction> reactions = m.Blueprint.EventReactions;
                if (!m.IsActiveInternal || reactions == null)
                {
                    continue;
                }

                for (int r = 0; r < reactions.Count; r++)
                {
                    ModifierEventReaction reaction = reactions[r];
                    if (reaction != null && reaction.IsEventBound && AbilityIdComparer.Same(reaction.EventId, eventId))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>True when an active modifier of <paramref name="owner" /> has a reaction with a non-zero priority.</summary>
        internal bool HasPrioritizedReactions(UnitId owner)
        {
            return _prioritizedTotal > 0 && _triggerCounts.TryGetValue(owner, out TriggerCounts counts) &&
                   counts.Prioritized > 0;
        }

        private int TotalFor(byte hook)
        {
            switch (hook)
            {
                case ModifierInstance.BeforeDealTriggerFlag:
                    return _beforeDealTotal;
                case ModifierInstance.BeforeTakeTriggerFlag:
                    return _beforeTakeTotal;
                case ModifierInstance.AttackLandedTriggerFlag:
                    return _attackLandedTotal;
                default:
                    return _attackReceivedTotal;
            }
        }

        /// <summary>True when an active modifier of <paramref name="owner" /> watches a state (StateEnter / StateExit).</summary>
        public bool HasStateWatchers(UnitId owner)
        {
            return _triggerCounts.TryGetValue(owner, out TriggerCounts counts) && counts.StateWatchers > 0;
        }

        /// <summary>The oldest active instance of <paramref name="modifierId" /> on the owner (case-insensitive).</summary>
        public bool TryGetActive(UnitId owner, string modifierId, out ModifierInstance instance)
        {
            instance = null;
            if (string.IsNullOrEmpty(modifierId) || !_byOwner.TryGetValue(owner, out List<ModifierInstance> list))
            {
                return false;
            }

            for (int i = 0; i < list.Count; i++)
            {
                ModifierInstance m = list[i];
                if (m.IsActiveInternal && AbilityIdComparer.Same(m.Blueprint.Id, modifierId))
                {
                    instance = m;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Active instances of <paramref name="modifierId" /> on the owner.</summary>
        public int CountActive(UnitId owner, string modifierId)
        {
            int count = 0;
            if (!string.IsNullOrEmpty(modifierId) && _byOwner.TryGetValue(owner, out List<ModifierInstance> list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].IsActiveInternal && AbilityIdComparer.Same(list[i].Blueprint.Id, modifierId))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static byte ComputeTriggerFlags(ModifierBlueprint blueprint, out bool needsState)
        {
            needsState = false;
            List<ModifierEventReaction> reactions = blueprint.EventReactions;
            if (reactions == null || reactions.Count == 0)
            {
                return 0;
            }

            byte flags = 0;
            for (int i = 0; i < reactions.Count; i++)
            {
                ModifierEventReaction reaction = reactions[i];
                if (reaction == null)
                {
                    continue;
                }

                flags |= ReactiveFlag;
                if (reaction.Trigger == ModifierTrigger.StateEnter || reaction.Trigger == ModifierTrigger.StateExit)
                {
                    flags |= StateWatcherFlag;
                }

                if (reaction.IsEventBound)
                {
                    flags |= HookFlag(reaction.EventId);
                }

                if (reaction.Priority != 0)
                {
                    flags |= ModifierInstance.PrioritizedTriggerFlag;
                }

                needsState |= reaction.NeedsState;
            }

            return flags;
        }

        private static byte HookFlag(string eventId)
        {
            // WHY: producers pass the constants; reference checks avoid comparing equal-length ids char by char.
            if (ReferenceEquals(eventId, AbilityEvents.BeforeDealDamage))
            {
                return ModifierInstance.BeforeDealTriggerFlag;
            }

            if (ReferenceEquals(eventId, AbilityEvents.BeforeTakeDamage))
            {
                return ModifierInstance.BeforeTakeTriggerFlag;
            }

            if (ReferenceEquals(eventId, AbilityEvents.AttackLanded))
            {
                return ModifierInstance.AttackLandedTriggerFlag;
            }

            if (ReferenceEquals(eventId, AbilityEvents.AttackReceived))
            {
                return ModifierInstance.AttackReceivedTriggerFlag;
            }

            if (AbilityIdComparer.Same(eventId, AbilityEvents.BeforeDealDamage))
            {
                return ModifierInstance.BeforeDealTriggerFlag;
            }

            if (AbilityIdComparer.Same(eventId, AbilityEvents.BeforeTakeDamage))
            {
                return ModifierInstance.BeforeTakeTriggerFlag;
            }

            if (AbilityIdComparer.Same(eventId, AbilityEvents.AttackLanded))
            {
                return ModifierInstance.AttackLandedTriggerFlag;
            }

            return AbilityIdComparer.Same(eventId, AbilityEvents.AttackReceived)
                ? ModifierInstance.AttackReceivedTriggerFlag
                : (byte)0;
        }

        private void CountTriggers(UnitId owner, byte flags, int delta)
        {
            if (flags == 0)
            {
                return;
            }

            _triggerCounts.TryGetValue(owner, out TriggerCounts counts);
            if ((flags & ReactiveFlag) != 0)
            {
                counts.Reactive += delta;
            }

            if ((flags & StateWatcherFlag) != 0)
            {
                counts.StateWatchers += delta;
            }

            if ((flags & ModifierInstance.BeforeDealTriggerFlag) != 0)
            {
                counts.BeforeDeal += delta;
                _beforeDealTotal += delta;
            }

            if ((flags & ModifierInstance.BeforeTakeTriggerFlag) != 0)
            {
                counts.BeforeTake += delta;
                _beforeTakeTotal += delta;
            }

            if ((flags & ModifierInstance.AttackLandedTriggerFlag) != 0)
            {
                counts.AttackLanded += delta;
                _attackLandedTotal += delta;
            }

            if ((flags & ModifierInstance.AttackReceivedTriggerFlag) != 0)
            {
                counts.AttackReceived += delta;
                _attackReceivedTotal += delta;
            }

            if ((flags & ModifierInstance.PrioritizedTriggerFlag) != 0)
            {
                counts.Prioritized += delta;
                _prioritizedTotal += delta;
            }

            if (counts.Reactive <= 0 && counts.StateWatchers <= 0)
            {
                _triggerCounts.Remove(owner);
            }
            else
            {
                _triggerCounts[owner] = counts;
            }
        }

        private void ReleaseEmptyList(UnitId owner, List<ModifierInstance> list)
        {
            _byOwner.Remove(owner);
            if (_ownerListPool.Count < MaxPooledOwnerLists)
            {
                _ownerListPool.Add(list);
            }
        }

        public bool Remove(ModifierInstance instance)
        {
            return RemoveInternal(instance, false);
        }

        /// <summary>Removes the first (or every) active instance with the given blueprint id (case-insensitive).</summary>
        public int RemoveById(UnitId owner, string modifierId, bool allInstances = true)
        {
            // WHY: matched by id directly instead of a capturing lambda — remove_modifier runs per target.
            return string.IsNullOrEmpty(modifierId) ? 0 : RemoveMatching(owner, null, modifierId, allInstances);
        }

        /// <summary>Removes every active instance matching the predicate (dispel support).</summary>
        public int RemoveWhere(UnitId owner, Predicate<ModifierInstance> predicate)
        {
            return predicate == null ? 0 : RemoveMatching(owner, predicate, null, true);
        }

        private int RemoveMatching(UnitId owner, Predicate<ModifierInstance> predicate, string modifierId,
            bool allInstances)
        {
            if (!_byOwner.ContainsKey(owner))
            {
                return 0;
            }

            while (_removalScratchPool.Count <= _removalDepth)
            {
                _removalScratchPool.Add(new List<ModifierInstance>(8));
            }

            // WHY: Removed callbacks and predicates can mutate the live list or run another bulk
            // removal. Each call snapshots only its starting instances into its own scratch list.
            List<ModifierInstance> snapshot = _removalScratchPool[_removalDepth++];
            try
            {
                GetModifiers(owner, snapshot);
                int removed = 0;
                for (int i = snapshot.Count - 1; i >= 0; i--)
                {
                    ModifierInstance m = snapshot[i];
                    bool matches = predicate != null
                        ? m.IsActiveInternal && predicate(m)
                        : m.IsActiveInternal &&
                          AbilityIdComparer.Same(m.Blueprint.Id, modifierId);
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

        public int RemoveAllFrom(UnitId owner)
        {
            return RemoveWhere(owner, _ => true);
        }

        /// <summary>Advances durations and interval ticks. Nonpositive/nonfinite steps are ignored.</summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            {
                return;
            }

            _tickScratch.Clear();
            foreach (KeyValuePair<UnitId, List<ModifierInstance>> pair in _byOwner)
            {
                List<ModifierInstance> list = pair.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    _tickScratch.Add(list[i]);
                }
            }

            for (int i = 0; i < _tickScratch.Count; i++)
            {
                ModifierInstance m = _tickScratch[i];
                if (!m.IsActiveInternal)
                {
                    continue;
                }

                if (m.Blueprint.HasTicks)
                {
                    // WHY: only time while the modifier is alive can generate ticks. A frame hitch
                    // past expiry must not turn a one-second DoT into several seconds of damage.
                    float activeTime = m.IsPermanent
                        ? deltaTime
                        : MathF.Min(deltaTime, MathF.Max(0f, m.RemainingDuration));
                    m.TickAccumulator += activeTime;
                    // WHY: bounds missed-tick catch-up so frame spikes stay deterministic and finite.
                    int safety = 8;
                    while (m.IsActiveInternal && m.TickAccumulator >= m.Blueprint.TickInterval && safety-- > 0)
                    {
                        m.TickAccumulator -= m.Blueprint.TickInterval;
                        TickDue?.Invoke(m);
                    }
                }

                if (m.IsActiveInternal && !m.IsPermanent)
                {
                    m.RemainingDuration -= deltaTime;
                    if (m.RemainingDuration <= 0f)
                    {
                        RemoveInternal(m, true);
                    }
                }
            }
        }

        public void GetModifiers(UnitId owner, List<ModifierInstance> results)
        {
            results.Clear();
            if (_byOwner.TryGetValue(owner, out List<ModifierInstance> list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].IsActiveInternal)
                    {
                        results.Add(list[i]);
                    }
                }
            }
        }

        public void CollectContributions(UnitId owner, string propertyId, List<ResolvedContribution> results)
        {
            if (!_byOwner.TryGetValue(owner, out List<ModifierInstance> list))
            {
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                ModifierInstance m = list[i];
                if (!m.IsActiveInternal)
                {
                    continue;
                }

                List<PropertyContribution> props = m.Blueprint.Properties;
                if (props == null)
                {
                    continue;
                }

                for (int p = 0; p < props.Count; p++)
                {
                    PropertyContribution c = props[p];
                    if (AbilityIdComparer.Same(c.PropertyId, propertyId))
                    {
                        results.Add(new ResolvedContribution(c.Op, c.ValueForStacks(m.Stacks)));
                    }
                }
            }
        }

        public bool HasState(UnitId owner, string stateId)
        {
            if (!_byOwner.TryGetValue(owner, out List<ModifierInstance> list))
            {
                return false;
            }

            for (int i = 0; i < list.Count; i++)
            {
                ModifierInstance m = list[i];
                if (!m.IsActiveInternal)
                {
                    continue;
                }

                List<string> states = m.Blueprint.States;
                if (states == null)
                {
                    continue;
                }

                for (int s = 0; s < states.Count; s++)
                {
                    if (AbilityIdComparer.Same(states[s], stateId))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool RemoveInternal(ModifierInstance instance, bool expired)
        {
            if (instance == null || !instance.IsActiveInternal ||
                !_byOwner.TryGetValue(instance.Owner, out List<ModifierInstance> list) || !list.Remove(instance))
            {
                return false;
            }

            instance.IsActiveInternal = false;
            if (list.Count == 0)
            {
                ReleaseEmptyList(instance.Owner, list);
            }

            CountTriggers(instance.Owner, instance.TriggerFlags, -1);
            BumpVersion(instance.Owner);
            Removed?.Invoke(instance, expired);
            return true;
        }

        private List<ModifierInstance> GetOrCreateList(UnitId owner)
        {
            if (!_byOwner.TryGetValue(owner, out List<ModifierInstance> list))
            {
                int last = _ownerListPool.Count - 1;
                if (last >= 0)
                {
                    list = _ownerListPool[last];
                    _ownerListPool.RemoveAt(last);
                }
                else
                {
                    list = new List<ModifierInstance>(4);
                }

                _byOwner[owner] = list;
            }

            return list;
        }

        private void BumpVersion(UnitId owner)
        {
            _versions[owner] = ++_versionCounter;
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
