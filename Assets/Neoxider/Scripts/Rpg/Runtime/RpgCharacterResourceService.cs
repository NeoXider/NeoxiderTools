using System;
using System.Collections.Generic;
using Neo.Reactive;
using UnityEngine;

namespace Neo.Rpg.Runtime
{
    /// <summary>
    ///     Plain-C# owner of a character's resource pools, mutations, reactive query surface, and
    ///     regeneration clocks. Scene components supply definitions and derived-value resolvers, then
    ///     react to <see cref="ResourceChanged"/> for presentation, death, persistence, or replication.
    /// </summary>
    public sealed class RpgCharacterResourceService
    {
        private readonly Dictionary<string, RpgResourceRuntime> _resources = new();
        private Func<RpgResourceRuntime, float> _regenResolver;
        private int _generation;
        private bool _isTicking;

        public event Action<string, RpgResourceRuntime> ResourceChanged;

        public IReadOnlyDictionary<string, RpgResourceRuntime> Resources => _resources;

        public void Build(IReadOnlyList<RpgResourceDefinition> definitions)
        {
            _generation++;
            _resources.Clear();
            _regenResolver = null;
            if (definitions == null)
            {
                return;
            }

            for (int i = 0; i < definitions.Count; i++)
            {
                RpgResourceDefinition definition = definitions[i];
                if (definition == null || !definition.id.IsValid)
                {
                    continue;
                }

                _resources[definition.id.Value] = new RpgResourceRuntime(definition);
            }
        }

        public void Clear()
        {
            _generation++;
            _resources.Clear();
            _regenResolver = null;
        }

        internal void InvalidateOperations()
        {
            _generation++;
        }

        private bool IsCurrent(int generation, string id, RpgResourceRuntime resource)
        {
            return generation == _generation && _resources.TryGetValue(id, out RpgResourceRuntime current) &&
                   ReferenceEquals(current, resource);
        }

        public bool Contains(string resourceId)
        {
            return !string.IsNullOrWhiteSpace(resourceId) && _resources.ContainsKey(resourceId);
        }

        public bool TryGet(string resourceId, out RpgResourceRuntime resource)
        {
            resource = null;
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                return false;
            }

            return _resources.TryGetValue(resourceId, out resource);
        }

        public ReactivePropertyFloat GetCurrentState(string resourceId)
        {
            return TryGet(resourceId, out RpgResourceRuntime resource) ? resource.CurrentState : null;
        }

        public ReactivePropertyFloat GetMaxState(string resourceId)
        {
            return TryGet(resourceId, out RpgResourceRuntime resource) ? resource.MaxState : null;
        }

        public ReactivePropertyFloat GetPercentState(string resourceId)
        {
            return TryGet(resourceId, out RpgResourceRuntime resource) ? resource.PercentState : null;
        }

        public float GetCurrent(string resourceId)
        {
            return TryGet(resourceId, out RpgResourceRuntime resource) ? resource.Current : 0f;
        }

        public float GetMax(string resourceId)
        {
            return TryGet(resourceId, out RpgResourceRuntime resource) ? resource.Max : 0f;
        }

        public float GetPercent(string resourceId)
        {
            if (!TryGet(resourceId, out RpgResourceRuntime resource) || resource.Max <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(resource.Current / resource.Max);
        }

        /// <summary>Accepts a positive cost only when its clamped stored debit is positive, before observers.
        /// Float rounding need not match the nominal cost exactly. Finite nonpositive costs retain no-op success.</summary>
        public bool Spend(string resourceId, float amount)
        {
            if (!RpgTickUtility.IsFinite(amount))
            {
                return false;
            }
            if (amount <= 0f)
            {
                return true;
            }

            if (!TryGet(resourceId, out RpgResourceRuntime resource))
            {
                return false;
            }

            if (resource.Current < amount && !resource.Definition.canGoBelowZero)
            {
                return false;
            }

            float candidate = resource.ClampCurrent(resource.Current - amount);
            if (!(candidate < resource.Current))
            {
                return false;
            }
            int generation = _generation;
            Func<bool> isCurrent = () => IsCurrent(generation, resourceId, resource);
            if (resource.SetCurrentGuarded(candidate, isCurrent, out _))
            {
                NotifyChanged(resourceId, resource);
                if (isCurrent())
                {
                    PauseAfterSpend(resource);
                }
            }
            return true;
        }

        public float Increase(string resourceId, float amount)
        {
            if (!RpgTickUtility.IsFinite(amount) || amount <= 0f || !TryGet(resourceId, out RpgResourceRuntime resource))
            {
                return 0f;
            }

            int generation = _generation;
            bool current = resource.SetCurrentGuarded(resource.Current + amount,
                () => IsCurrent(generation, resourceId, resource), out float applied);
            if (current && applied > 0f)
            {
                NotifyChanged(resourceId, resource);
            }

            return applied;
        }

        public float Decrease(string resourceId, float amount)
        {
            if (!RpgTickUtility.IsFinite(amount) || amount <= 0f || !TryGet(resourceId, out RpgResourceRuntime resource))
            {
                return 0f;
            }

            int generation = _generation;
            bool current = resource.SetCurrentGuarded(resource.Current - amount,
                () => IsCurrent(generation, resourceId, resource), out float change);
            float applied = -change;
            if (current && applied > 0f)
            {
                NotifyChanged(resourceId, resource);
            }

            return applied;
        }

        public bool Restore(string resourceId)
        {
            if (!TryGet(resourceId, out RpgResourceRuntime resource))
            {
                return false;
            }

            int generation = _generation;
            if (resource.SetCurrentGuarded(resource.Max, () => IsCurrent(generation, resourceId, resource), out _))
            {
                NotifyChanged(resourceId, resource);
            }
            return true;
        }

        public void RestoreAll()
        {
            int generation = _generation;
            foreach (KeyValuePair<string, RpgResourceRuntime> pair in _resources)
            {
                if (!pair.Value.SetCurrentGuarded(pair.Value.Max,
                        () => IsCurrent(generation, pair.Key, pair.Value), out _))
                {
                    return;
                }
                NotifyChanged(pair.Key, pair.Value);
                if (generation != _generation)
                {
                    return;
                }
            }
        }

        public bool SetMax(string resourceId, float newMax)
        {
            if (!RpgTickUtility.IsFinite(newMax) || !TryGet(resourceId, out RpgResourceRuntime resource))
            {
                return false;
            }

            int generation = _generation;
            Func<bool> isCurrent = () => IsCurrent(generation, resourceId, resource);
            if (!resource.SetMaxGuarded(newMax, true, isCurrent))
            {
                return true;
            }
            float regen = _regenResolver != null
                ? _regenResolver(resource) : RpgStatResolver.ResolveRegen(resource, null, null);
            if (!isCurrent())
            {
                return true;
            }
            resource.ResolvedRegenPerSecond = regen;
            NotifyChanged(resourceId, resource);
            return true;
        }

        public bool AddMax(string resourceId, float delta)
        {
            if (!RpgTickUtility.IsFinite(delta) || !TryGet(resourceId, out RpgResourceRuntime resource))
            {
                return false;
            }

            return SetMax(resourceId, resource.Max + delta);
        }

        public void TickRegen(float deltaTime, bool isDead)
        {
            if (_isTicking || !RpgTickUtility.IsFinite(deltaTime) || deltaTime <= 0f)
            {
                return;
            }

            _isTicking = true;
            int generation = _generation;
            try
            {
                foreach (KeyValuePair<string, RpgResourceRuntime> pair in _resources)
                {
                    RpgResourceRuntime resource = pair.Value;
                    RpgRegenDefinition definition = resource.Definition?.regen;
                    if (definition == null || !definition.enabled || definition.onlyWhenAlive && isDead ||
                        definition.onlyWhenNotFull && resource.Current >= resource.Max)
                    {
                        continue;
                    }

                    if (resource.RegenPauseRemaining > 0f)
                    {
                        resource.RegenPauseRemaining -= deltaTime;
                        continue;
                    }

                    float rate = resource.ResolvedRegenPerSecond;
                    switch (definition.mode)
                    {
                        case RpgRegenMode.FlatPerSecond:
                        case RpgRegenMode.PercentMaxPerSecond:
                        case RpgRegenMode.FromStat:
                            if (rate > 0f && RpgTickUtility.TryAmount((double)rate * deltaTime, out float continuous))
                            {
                                Increase(pair.Key, continuous);
                                if (generation != _generation)
                                {
                                    return;
                                }
                            }

                            break;
                        case RpgRegenMode.FlatPerTick:
                        case RpgRegenMode.PercentMaxPerTick:
                            float interval = RpgTickUtility.IsFinite(definition.tickInterval)
                                ? Mathf.Max(0.01f, definition.tickInterval) : definition.tickInterval;
                            int pending = RpgTickUtility.Consume(resource.TickAccumulator, deltaTime, interval,
                                out float phase);
                            resource.TickAccumulator = phase;
                            if (!RpgTickUtility.TryAmount((double)rate * interval, out float amount) || amount <= 0f)
                            {
                                break;
                            }

                            while (pending > 0)
                            {
                                pending--;
                                Increase(pair.Key, amount);
                                if (generation != _generation)
                                {
                                    return;
                                }
                            }

                            break;
                    }
                }
            }
            finally
            {
                _isTicking = false;
            }
        }

        public void PauseAfterDamage()
        {
            foreach (KeyValuePair<string, RpgResourceRuntime> pair in _resources)
            {
                RpgResourceRuntime resource = pair.Value;
                RpgRegenDefinition definition = resource.Definition?.regen;
                if (definition == null || !definition.pauseAfterDamage)
                {
                    continue;
                }

                resource.RegenPauseRemaining = Mathf.Max(resource.RegenPauseRemaining,
                    definition.pauseAfterDamageSeconds);
            }
        }

        public void RefreshDerived(Func<RpgResourceRuntime, float> maxResolver,
            Func<RpgResourceRuntime, float> regenResolver, bool initial)
        {
            RefreshDerived(maxResolver, regenResolver, initial, null, null);
        }

        internal void RefreshDerived(Func<RpgResourceRuntime, float> maxResolver,
            Func<RpgResourceRuntime, float> regenResolver, bool initial,
            IReadOnlyDictionary<string, RpgResourceSaveEntry> restoreRows, Func<bool> ownerIsCurrent)
        {
            if (maxResolver == null)
            {
                throw new ArgumentNullException(nameof(maxResolver));
            }

            if (regenResolver == null)
            {
                throw new ArgumentNullException(nameof(regenResolver));
            }

            _regenResolver = regenResolver;
            int generation = _generation;
            foreach (KeyValuePair<string, RpgResourceRuntime> pair in _resources)
            {
                RpgResourceRuntime resource = pair.Value;
                Func<bool> isCurrent = () => IsCurrent(generation, pair.Key, resource) &&
                    (ownerIsCurrent == null || ownerIsCurrent());
                float max = maxResolver(resource);
                if (!isCurrent())
                {
                    return;
                }
                RpgResourceSaveEntry saved = null;
                bool restoring = restoreRows != null && restoreRows.TryGetValue(pair.Key, out saved);
                if (!resource.SetMaxGuarded(max, !restoring && !resource.Definition.canOverfill, isCurrent))
                {
                    return;
                }
                float regen = regenResolver(resource);
                if (!isCurrent())
                {
                    return;
                }
                resource.ResolvedRegenPerSecond = regen;

                if (restoring || initial && resource.Definition.restoreOnAwake && resource.Definition.restoreToFull)
                {
                    if (!resource.SetCurrentGuarded(restoring ? saved.Current : resource.Max, isCurrent, out _))
                    {
                        return;
                    }
                }

                NotifyChanged(pair.Key, resource);
                if (!isCurrent())
                {
                    return;
                }
            }
        }

        private static void PauseAfterSpend(RpgResourceRuntime resource)
        {
            RpgRegenDefinition definition = resource.Definition?.regen;
            if (definition == null || !definition.pauseAfterSpend)
            {
                return;
            }

            resource.RegenPauseRemaining = Mathf.Max(resource.RegenPauseRemaining,
                definition.pauseAfterSpendSeconds);
        }

        private void NotifyChanged(string resourceId, RpgResourceRuntime resource)
        {
            ResourceChanged?.Invoke(resourceId, resource);
        }
    }
}
