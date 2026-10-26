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
        private int _removalDepth;
        private int _nextInstanceId = 1;

        /// <summary>Fired after an instance is applied (new) or re-applied (refresh/stack).</summary>
        public event Action<ModifierInstance, bool> Applied;

        /// <summary>Fired after an instance is removed. Second argument is true when it expired naturally.</summary>
        public event Action<ModifierInstance, bool> Removed;

        /// <summary>Fired when an instance's tick interval elapses. The system executes its tick effects.</summary>
        public event Action<ModifierInstance> TickDue;

        /// <summary>Monotonic version per owner — bumps on any modifier change; used for property caches.</summary>
        public int VersionOf(UnitId owner)
        {
            return _versions.TryGetValue(owner, out int v) ? v : 0;
        }

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

        /// <summary>Removes the first (or every) active instance with the given blueprint id.</summary>
        public int RemoveById(UnitId owner, string modifierId, bool allInstances = true)
        {
            return RemoveMatching(owner,
                m => string.Equals(m.Blueprint.Id, modifierId, StringComparison.OrdinalIgnoreCase), allInstances);
        }

        /// <summary>Removes every active instance matching the predicate (dispel support).</summary>
        public int RemoveWhere(UnitId owner, Predicate<ModifierInstance> predicate)
        {
            return RemoveMatching(owner, predicate, true);
        }

        private int RemoveMatching(UnitId owner, Predicate<ModifierInstance> predicate, bool allInstances)
        {
            if (predicate == null || !_byOwner.ContainsKey(owner))
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
                    if (m.IsActiveInternal && predicate(m) && RemoveInternal(m, false))
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
                    if (string.Equals(c.PropertyId, propertyId, StringComparison.OrdinalIgnoreCase))
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
                    if (string.Equals(states[s], stateId, StringComparison.OrdinalIgnoreCase))
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
                    string.Equals(m.Blueprint.Id, blueprintId, StringComparison.OrdinalIgnoreCase))
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
            }

            BumpVersion(instance.Owner);
            Removed?.Invoke(instance, expired);
            return true;
        }

        private List<ModifierInstance> GetOrCreateList(UnitId owner)
        {
            if (!_byOwner.TryGetValue(owner, out List<ModifierInstance> list))
            {
                list = new List<ModifierInstance>(4);
                _byOwner[owner] = list;
            }

            return list;
        }

        private void BumpVersion(UnitId owner)
        {
            _versions.TryGetValue(owner, out int v);
            _versions[owner] = v + 1;
        }
    }
}
