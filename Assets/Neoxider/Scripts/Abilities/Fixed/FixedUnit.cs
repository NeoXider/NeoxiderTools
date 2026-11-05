using System.Collections.Generic;

namespace Neo.Abilities.Fixed
{
    /// <summary>A raw resource pool of a fixed unit (health, mana, energy...).</summary>
    public sealed class FixedResourcePool
    {
        internal FixedResourcePool(string id)
        {
            Id = id;
        }

        public string Id { get; }
        public int CurrentRaw { get; internal set; }
        public int MaxRaw { get; internal set; }

        /// <summary>When false (default), an empty pool cannot be increased (a dead unit is not healed back).</summary>
        public bool AllowIncreaseFromEmpty { get; set; }
    }

    /// <summary>
    ///     A unit of the fixed core: id, team, life, level, position, raw resource pools, raw base properties,
    ///     permanent states and a property cache aggregated through <see cref="FixedRules.Aggregator" />.
    /// </summary>
    public sealed class FixedUnit
    {
        private readonly List<FixedResourcePool> _pools = new List<FixedResourcePool>(2);

        private readonly Dictionary<string, int> _baseProperties =
            new Dictionary<string, int>(FixedIdComparer.Instance);

        private readonly List<string> _permanentStates = new List<string>(2);

        private readonly Dictionary<string, CachedProperty> _propertyCache =
            new Dictionary<string, CachedProperty>(FixedIdComparer.Instance);

        private readonly List<FixedResolvedContribution> _contributionScratch =
            new List<FixedResolvedContribution>(8);

        private FixedResourcePool _health;
        private FixedResourcePool _mana;
        private int _baseVersion;
        private int _rulesVersionSeen;
        private int _appliedMaxHealthBonus;
        private int _appliedMaxManaBonus;
        internal long HealthRegenCarry;
        internal long ManaRegenCarry;

        internal FixedUnit(int id, int team, FixedAbilitySystem system)
        {
            Id = id;
            Team = team;
            System = system;
        }

        public int Id { get; }
        public FixedAbilitySystem System { get; }

        /// <summary>Team id; 0 is neutral (an enemy of everyone, an ally of nobody but itself).</summary>
        public int Team { get; set; }

        public bool IsAlive { get; internal set; } = true;
        public int Level { get; private set; } = 1;

        /// <summary>Raw position read by the default world adapter.</summary>
        public FixedVec3 Position { get; set; }

        public int HealthRaw => _health != null ? _health.CurrentRaw : 0;
        public int MaxHealthRaw => _health != null ? _health.MaxRaw : 0;
        public int ManaRaw => _mana != null ? _mana.CurrentRaw : 0;
        public int MaxManaRaw => _mana != null ? _mana.MaxRaw : 0;

        public bool IsAllyOf(FixedUnit other)
        {
            return other != null && (other.Id == Id || Team != 0 && Team == other.Team);
        }

        public bool IsEnemyOf(FixedUnit other)
        {
            return other != null && other.Id != Id && Team != other.Team;
        }

        public void SetLevel(int level)
        {
            int clamped = level < 1 ? 1 : level;
            if (clamped != Level)
            {
                Level = clamped;
                _baseVersion++;
            }
        }

        /// <summary>Adds or replaces a pool; <paramref name="currentRaw" /> below 0 starts it full.</summary>
        public FixedResourcePool AddPool(string resourceId, int maxRaw, int currentRaw = -1)
        {
            if (string.IsNullOrEmpty(resourceId))
            {
                return null;
            }

            FixedResourcePool pool = FindPool(resourceId);
            if (pool == null)
            {
                pool = new FixedResourcePool(resourceId);
                _pools.Add(pool);
                if (FixedIdComparer.Same(resourceId, FixedResources.Health))
                {
                    _health = pool;
                }
                else if (FixedIdComparer.Same(resourceId, FixedResources.Mana))
                {
                    _mana = pool;
                }
            }

            pool.MaxRaw = maxRaw < 0 ? 0 : maxRaw;
            int current = currentRaw < 0 ? pool.MaxRaw : currentRaw;
            pool.CurrentRaw = pool.MaxRaw > 0 && current > pool.MaxRaw ? pool.MaxRaw : current;
            return pool;
        }

        public bool HasPool(string resourceId)
        {
            return FindPool(resourceId) != null;
        }

        public int GetCurrent(string resourceId)
        {
            FixedResourcePool pool = FindPool(resourceId);
            return pool != null ? pool.CurrentRaw : 0;
        }

        public int GetMax(string resourceId)
        {
            FixedResourcePool pool = FindPool(resourceId);
            return pool != null ? pool.MaxRaw : 0;
        }

        /// <summary>Lowers a pool by a positive raw amount (not below 0); returns the amount actually removed.</summary>
        public int Decrease(string resourceId, int amountRaw)
        {
            FixedResourcePool pool = FindPool(resourceId);
            if (pool == null || amountRaw <= 0)
            {
                return 0;
            }

            int previous = pool.CurrentRaw;
            int next = FixedMath.SaturatingSubtract(previous, amountRaw);
            pool.CurrentRaw = next < 0 ? 0 : next;
            return previous - pool.CurrentRaw;
        }

        /// <summary>
        ///     Raises a pool by a positive raw amount up to its max; an empty pool stays empty unless
        ///     <see cref="FixedResourcePool.AllowIncreaseFromEmpty" />. Returns the amount actually added.
        /// </summary>
        public int Increase(string resourceId, int amountRaw)
        {
            FixedResourcePool pool = FindPool(resourceId);
            if (pool == null || amountRaw <= 0 || pool.CurrentRaw <= 0 && !pool.AllowIncreaseFromEmpty)
            {
                return 0;
            }

            int previous = pool.CurrentRaw;
            int next = FixedMath.SaturatingAdd(previous, amountRaw);
            pool.CurrentRaw = pool.MaxRaw > 0 && next > pool.MaxRaw ? pool.MaxRaw : next;
            return pool.CurrentRaw - previous;
        }

        public void SetCurrent(string resourceId, int valueRaw)
        {
            FixedResourcePool pool = FindPool(resourceId);
            if (pool == null)
            {
                return;
            }

            int clamped = valueRaw < 0 ? 0 : valueRaw;
            pool.CurrentRaw = pool.MaxRaw > 0 && clamped > pool.MaxRaw ? pool.MaxRaw : clamped;
        }

        public void SetMax(string resourceId, int maxRaw)
        {
            FixedResourcePool pool = FindPool(resourceId);
            if (pool == null)
            {
                return;
            }

            pool.MaxRaw = maxRaw < 0 ? 0 : maxRaw;
            if (pool.MaxRaw > 0 && pool.CurrentRaw > pool.MaxRaw)
            {
                pool.CurrentRaw = pool.MaxRaw;
            }
        }

        public bool CanSpend(string resourceId, int amountRaw)
        {
            FixedResourcePool pool = FindPool(resourceId);
            return pool != null && amountRaw >= 0 && pool.CurrentRaw >= amountRaw;
        }

        public bool TrySpend(string resourceId, int amountRaw)
        {
            if (!CanSpend(resourceId, amountRaw))
            {
                return false;
            }

            Decrease(resourceId, amountRaw);
            return true;
        }

        /// <summary>Clears <paramref name="results" /> and fills it with this unit's pools.</summary>
        public void GetPools(List<FixedResourcePool> results)
        {
            results.Clear();
            for (int i = 0; i < _pools.Count; i++)
            {
                results.Add(_pools[i]);
            }
        }

        public void SetBaseProperty(string propertyId, int valueRaw)
        {
            if (string.IsNullOrEmpty(propertyId))
            {
                return;
            }

            _baseProperties[propertyId] = valueRaw;
            _baseVersion++;
            if (FixedIdComparer.Same(propertyId, FixedProperties.MaxHealthBonus) ||
                FixedIdComparer.Same(propertyId, FixedProperties.MaxManaBonus))
            {
                SyncResourceMaxBonuses();
            }
        }

        public int GetBaseProperty(string propertyId, int defaultRaw = 0)
        {
            return propertyId != null && _baseProperties.TryGetValue(propertyId, out int value) ? value : defaultRaw;
        }

        /// <summary>
        ///     Base value combined with the active modifier contributions by the rules' aggregator; cached until the
        ///     unit's modifiers, base values or the rules change.
        /// </summary>
        public int GetProperty(string propertyId, int defaultBaseRaw = 0)
        {
            if (string.IsNullOrEmpty(propertyId))
            {
                return defaultBaseRaw;
            }

            FixedRules rules = System.Rules;
            if (rules.Version != _rulesVersionSeen)
            {
                _rulesVersionSeen = rules.Version;
                _baseVersion++;
            }

            int modifierVersion = System.Modifiers.VersionOf(Id);
            if (_propertyCache.TryGetValue(propertyId, out CachedProperty cached) &&
                cached.ModifierVersion == modifierVersion && cached.BaseVersion == _baseVersion &&
                cached.DefaultBase == defaultBaseRaw)
            {
                return cached.Value;
            }

            int baseValue = GetBaseProperty(propertyId, defaultBaseRaw);
            _contributionScratch.Clear();
            System.Modifiers.CollectContributions(Id, propertyId, _contributionScratch);
            int value = rules.Aggregator.Compute(propertyId, baseValue, _contributionScratch, rules.Format);
            _contributionScratch.Clear();
            _propertyCache[propertyId] = new CachedProperty(modifierVersion, _baseVersion, defaultBaseRaw, value);
            return value;
        }

        public void SetPermanentState(string stateId, bool enabled)
        {
            if (string.IsNullOrEmpty(stateId))
            {
                return;
            }

            int index = IndexOfState(stateId);
            if (enabled == index >= 0)
            {
                return;
            }

            if (enabled)
            {
                _permanentStates.Add(stateId);
            }
            else
            {
                _permanentStates.RemoveAt(index);
            }

            System.OnPermanentStateChanged(Id);
        }

        public bool HasState(string stateId)
        {
            if (string.IsNullOrEmpty(stateId))
            {
                return false;
            }

            return IndexOfState(stateId) >= 0 || System.Modifiers.HasState(Id, stateId);
        }

        internal void SyncResourceMaxBonuses()
        {
            ApplyMaxBonus(_health, FixedProperties.MaxHealthBonus, ref _appliedMaxHealthBonus);
            ApplyMaxBonus(_mana, FixedProperties.MaxManaBonus, ref _appliedMaxManaBonus);
        }

        internal FixedResourcePool FindPool(string resourceId)
        {
            if (resourceId == null)
            {
                return null;
            }

            if (_health != null && ReferenceEquals(resourceId, FixedResources.Health))
            {
                return _health;
            }

            if (_mana != null && ReferenceEquals(resourceId, FixedResources.Mana))
            {
                return _mana;
            }

            for (int i = 0; i < _pools.Count; i++)
            {
                if (FixedIdComparer.Same(_pools[i].Id, resourceId))
                {
                    return _pools[i];
                }
            }

            return null;
        }

        private void ApplyMaxBonus(FixedResourcePool pool, string propertyId, ref int applied)
        {
            if (pool == null || pool.MaxRaw <= 0 && applied == 0)
            {
                return;
            }

            // WHY: the pool stores base + the bonus applied last time; removing that delta recovers the base so
            // re-syncs never compound.
            int baseMax = pool.MaxRaw - applied;
            int target = FixedMath.SaturatingAdd(baseMax, GetProperty(propertyId));
            if (target < 0)
            {
                target = 0;
            }

            applied = target - baseMax;
            if (target != pool.MaxRaw)
            {
                SetMax(pool.Id, target);
            }
        }

        private int IndexOfState(string stateId)
        {
            for (int i = 0; i < _permanentStates.Count; i++)
            {
                if (FixedIdComparer.Same(_permanentStates[i], stateId))
                {
                    return i;
                }
            }

            return -1;
        }

        public override string ToString()
        {
            return "FixedUnit(" + Id + ")";
        }

        private readonly struct CachedProperty
        {
            public readonly int ModifierVersion;
            public readonly int BaseVersion;
            public readonly int DefaultBase;
            public readonly int Value;

            public CachedProperty(int modifierVersion, int baseVersion, int defaultBase, int value)
            {
                ModifierVersion = modifierVersion;
                BaseVersion = baseVersion;
                DefaultBase = defaultBase;
                Value = value;
            }
        }
    }
}
