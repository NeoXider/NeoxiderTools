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

        public ModifierApplyResult Apply(ModifierBlueprint blueprint, UnitId caster, UnitId owner,
            string sourceAbilityId = null, int abilityLevel = 1)
        {
            if (blueprint == null || string.IsNullOrEmpty(blueprint.Id) || !owner.IsValid)
            {
                return new ModifierApplyResult(null, false);
            }

            List<ModifierInstance> list = GetOrCreateList(owner);

            if (blueprint.StackPolicy != ModifierStackPolicy.Independent)
            {
                ModifierInstance existing = FindActive(list, blueprint.Id);
                if (existing != null)
                {
                    if (blueprint.StackPolicy == ModifierStackPolicy.Stack)
                    {
                        int max = blueprint.MaxStacks > 0 ? blueprint.MaxStacks : int.MaxValue;
                        if (existing.Stacks < max)
                        {
                            existing.Stacks++;
                        }
                    }

                    // WHY: a re-application re-captures the applying ability level (and its leveled duration).
                    existing.RecaptureLevel(abilityLevel);
                    existing.RefreshDuration();
                    BumpVersion(owner);
                    Applied?.Invoke(existing, false);
                    return new ModifierApplyResult(existing, false);
                }
            }

            ModifierInstance instance = new ModifierInstance(_nextInstanceId++, blueprint, caster, owner, sourceAbilityId,
                abilityLevel);
            if (blueprint.TickOnApply && blueprint.HasTicks)
            {
                // WHY: primes the accumulator so the first Tick fires immediately.
                instance.TickAccumulator = blueprint.TickInterval;
            }

            list.Add(instance);
            BumpVersion(owner);
            Applied?.Invoke(instance, true);
            return new ModifierApplyResult(instance, true);
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

        private static ModifierInstance FindActive(List<ModifierInstance> list, string blueprintId)
        {
            for (int i = 0; i < list.Count; i++)
            {
                ModifierInstance m = list[i];
                if (m.IsActiveInternal && !m.IsShieldBreaking &&
                    AbilityIdComparer.Same(m.Blueprint.Id, blueprintId))
                {
                    return m;
                }
            }

            return null;
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
                _byOwner.Remove(instance.Owner);
                if (_ownerListPool.Count < MaxPooledOwnerLists)
                {
                    _ownerListPool.Add(list);
                }
            }

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
    }
}
