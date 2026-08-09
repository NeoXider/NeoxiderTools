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

        public event Action<string, RpgResourceRuntime> ResourceChanged;

        public IReadOnlyDictionary<string, RpgResourceRuntime> Resources => _resources;

        public void Build(IReadOnlyList<RpgResourceDefinition> definitions)
        {
            _resources.Clear();
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
            _resources.Clear();
        }

        public bool Contains(string resourceId)
        {
            return !string.IsNullOrWhiteSpace(resourceId) && _resources.ContainsKey(resourceId);
        }

        public bool TryGet(string resourceId, out RpgResourceRuntime resource)
        {
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

        public bool Spend(string resourceId, float amount)
        {
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

            resource.SetCurrent(resource.Current - amount);
            NotifyChanged(resourceId, resource);
            PauseAfterSpend(resource);
            return true;
        }

        public float Increase(string resourceId, float amount)
        {
            if (amount <= 0f || !TryGet(resourceId, out RpgResourceRuntime resource))
            {
                return 0f;
            }

            float before = resource.Current;
            resource.SetCurrent(before + amount);
            float applied = resource.Current - before;
            if (applied > 0f)
            {
                NotifyChanged(resourceId, resource);
            }

            return applied;
        }

        public float Decrease(string resourceId, float amount)
        {
            if (amount <= 0f || !TryGet(resourceId, out RpgResourceRuntime resource))
            {
                return 0f;
            }

            float before = resource.Current;
            resource.SetCurrent(before - amount);
            float applied = before - resource.Current;
            if (applied > 0f)
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

            resource.SetCurrent(resource.Max);
            NotifyChanged(resourceId, resource);
            return true;
        }

        public void RestoreAll()
        {
            foreach (KeyValuePair<string, RpgResourceRuntime> pair in _resources)
            {
                pair.Value.SetCurrent(pair.Value.Max);
                NotifyChanged(pair.Key, pair.Value);
            }
        }

        public bool SetMax(string resourceId, float newMax)
        {
            if (!TryGet(resourceId, out RpgResourceRuntime resource))
            {
                return false;
            }

            resource.SetMax(newMax, true);
            NotifyChanged(resourceId, resource);
            return true;
        }

        public bool AddMax(string resourceId, float delta)
        {
            if (!TryGet(resourceId, out RpgResourceRuntime resource))
            {
                return false;
            }

            resource.SetMax(resource.Max + delta, true);
            NotifyChanged(resourceId, resource);
            return true;
        }

        public void TickRegen(float deltaTime, bool isDead)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

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

                switch (definition.mode)
                {
                    case RpgRegenMode.FlatPerSecond:
                    case RpgRegenMode.PercentMaxPerSecond:
                    case RpgRegenMode.FromStat:
                        if (resource.ResolvedRegenPerSecond > 0f)
                        {
                            Increase(pair.Key, resource.ResolvedRegenPerSecond * deltaTime);
                        }

                        break;
                    case RpgRegenMode.FlatPerTick:
                    case RpgRegenMode.PercentMaxPerTick:
                        float tickInterval = Mathf.Max(0.01f, definition.tickInterval);
                        resource.TickAccumulator += deltaTime;
                        while (resource.TickAccumulator >= tickInterval)
                        {
                            float amount = definition.mode == RpgRegenMode.FlatPerTick
                                ? definition.value
                                : resource.Max * (definition.value / 100f);
                            if (amount > 0f)
                            {
                                Increase(pair.Key, amount);
                            }

                            resource.TickAccumulator -= tickInterval;
                        }

                        break;
                }
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
            if (maxResolver == null)
            {
                throw new ArgumentNullException(nameof(maxResolver));
            }

            if (regenResolver == null)
            {
                throw new ArgumentNullException(nameof(regenResolver));
            }

            foreach (KeyValuePair<string, RpgResourceRuntime> pair in _resources)
            {
                RpgResourceRuntime resource = pair.Value;
                resource.SetMax(maxResolver(resource), !resource.Definition.canOverfill);
                resource.ResolvedRegenPerSecond = regenResolver(resource);

                if (initial && resource.Definition.restoreOnAwake && resource.Definition.restoreToFull)
                {
                    resource.SetCurrent(resource.Max);
                }

                NotifyChanged(pair.Key, resource);
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
