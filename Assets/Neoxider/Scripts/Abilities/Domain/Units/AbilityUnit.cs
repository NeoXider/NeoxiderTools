using System;
using System.Collections.Generic;
using Neo.Core.Resources;

namespace Neo.Abilities
{
    /// <summary>
    ///     Domain entity of the ability system: identity, team, resource pools (reuses
    ///     <see cref="ResourcePoolModel" /> from Neo.Core), base property values, permanent states,
    ///     and cached property/state aggregation over the system's <see cref="ModifierEngine" />.
    ///     Pure C#; the scene wrapper is <c>AbilityUnitBehaviour</c>.
    /// </summary>
    public sealed class AbilityUnit
    {
        private readonly Dictionary<string, float> _baseProperties =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<string> _permanentStates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, CachedProperty> _propertyCache =
            new Dictionary<string, CachedProperty>(StringComparer.OrdinalIgnoreCase);

        private readonly List<ResolvedContribution> _contributionScratch = new List<ResolvedContribution>(8);

        private int _baseVersion;

        internal AbilityUnit(UnitId id, AbilitySystem system)
        {
            Id = id;
            System = system;
            Resources = new ResourcePoolModel();
        }

        public UnitId Id { get; }
        public AbilitySystem System { get; }
        public TeamId Team { get; set; }
        public string DisplayName { get; set; }
        public ResourcePoolModel Resources { get; }
        public bool IsAlive { get; internal set; } = true;

        public float Health => Resources.GetCurrent(AbilityResourceIds.Health);
        public float MaxHealth => Resources.GetMax(AbilityResourceIds.Health);

        /// <summary>
        ///     Unit level for data-driven per-unit-level scaling (see <see cref="LeveledValue" /> with
        ///     <see cref="LevelSource.CasterUnitLevel" /> / <see cref="LevelSource.TargetUnitLevel" />).
        ///     Clamped to at least 1. Bridge it from a <c>Neo.Core.Level.LevelComponent</c> in the scene layer.
        /// </summary>
        public int Level { get; private set; } = 1;

        /// <summary>Sets the unit level (clamped to at least 1) and invalidates level-dependent property caches.</summary>
        public void SetLevel(int level)
        {
            int clamped = level < 1 ? 1 : level;
            if (clamped == Level)
            {
                return;
            }

            Level = clamped;
            _baseVersion++;
        }

        /// <summary>Sets the base value of a property (before modifiers).</summary>
        public void SetBaseProperty(string propertyId, float value)
        {
            if (string.IsNullOrEmpty(propertyId))
            {
                return;
            }

            _baseProperties[propertyId] = value;
            _baseVersion++;
        }

        public float GetBaseProperty(string propertyId, float defaultValue = 0f)
        {
            return _baseProperties.TryGetValue(propertyId, out float v) ? v : defaultValue;
        }

        /// <summary>
        ///     Final property value: (base + sum Add) * product Mul, raised to Max floors.
        ///     Cached until modifiers or base values change.
        /// </summary>
        public float GetProperty(string propertyId, float defaultBase = 0f)
        {
            if (string.IsNullOrEmpty(propertyId))
            {
                return defaultBase;
            }

            int modVersion = System.Modifiers.VersionOf(Id);
            if (_propertyCache.TryGetValue(propertyId, out CachedProperty cached) &&
                cached.ModifierVersion == modVersion && cached.BaseVersion == _baseVersion &&
                cached.DefaultBase.Equals(defaultBase))
            {
                return cached.Value;
            }

            float baseValue = GetBaseProperty(propertyId, defaultBase);
            _contributionScratch.Clear();
            System.Modifiers.CollectContributions(Id, propertyId, _contributionScratch);
            float value = PropertyAggregator.Compute(baseValue, _contributionScratch);
            _propertyCache[propertyId] = new CachedProperty(modVersion, _baseVersion, defaultBase, value);
            return value;
        }

        /// <summary>Grants a state permanently (unit-intrinsic, independent of modifiers).</summary>
        public void SetPermanentState(string stateId, bool enabled)
        {
            if (string.IsNullOrEmpty(stateId))
            {
                return;
            }

            if (enabled)
            {
                _permanentStates.Add(stateId);
            }
            else
            {
                _permanentStates.Remove(stateId);
            }
        }

        /// <summary>True when any active modifier or a permanent flag declares the state (any-true-wins).</summary>
        public bool HasState(string stateId)
        {
            if (string.IsNullOrEmpty(stateId))
            {
                return false;
            }

            return _permanentStates.Contains(stateId) || System.Modifiers.HasState(Id, stateId);
        }

        private readonly struct CachedProperty
        {
            public readonly int ModifierVersion;
            public readonly int BaseVersion;
            public readonly float DefaultBase;
            public readonly float Value;

            public CachedProperty(int modifierVersion, int baseVersion, float defaultBase, float value)
            {
                ModifierVersion = modifierVersion;
                BaseVersion = baseVersion;
                DefaultBase = defaultBase;
                Value = value;
            }
        }
    }
}
