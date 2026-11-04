using System.Collections.Generic;
using Neo.Reactive;

namespace Neo.Rpg.Runtime
{
    /// <summary>
    ///     Plain-C# owner of a character's stats: live <see cref="RpgStatRuntime" /> values, the upgrade-rule
    ///     catalogue and the upgrade points invested per stat. Resolves final stat values and stat-derived
    ///     resource limits/regeneration without any scene dependency, so <c>RpgCharacter</c>, tests and custom
    ///     actors share one implementation.
    /// </summary>
    /// <remarks>
    ///     The service only mutates data; publication order, network routing and re-entrancy guards stay with the
    ///     owning facade, which calls <see cref="ResolveStat" /> and pushes the result through
    ///     <see cref="RpgStatRuntime.SetCurrent" />.
    /// </remarks>
    public sealed class RpgCharacterStatService
    {
        private readonly Dictionary<string, RpgStatRuntime> _stats = new();
        private readonly Dictionary<string, RpgStatUpgradeRule> _upgradeRules = new();
        private readonly Dictionary<string, int> _investments = new();

        /// <summary>Live stats by id.</summary>
        public IReadOnlyDictionary<string, RpgStatRuntime> Stats => _stats;

        /// <summary>Upgrade rules by stat id, built from the active progression definition.</summary>
        public IReadOnlyDictionary<string, RpgStatUpgradeRule> UpgradeRules => _upgradeRules;

        /// <summary>Upgrade points invested per stat id (Dark-Souls style flow).</summary>
        public IReadOnlyDictionary<string, int> Investments => _investments;

        /// <summary>Number of live stats.</summary>
        public int Count => _stats.Count;

        // WHY: concrete table for allocation-free enumeration by the facade; the public view boxes its enumerator.
        internal Dictionary<string, RpgStatRuntime> StatTable => _stats;

        /// <summary>Replaces all stat runtimes with fresh ones built from <paramref name="definitions" />.</summary>
        /// <remarks>Null entries and invalid ids are skipped; a later duplicate id replaces an earlier one.</remarks>
        public void Build(IReadOnlyList<RpgStatDefinition> definitions)
        {
            _stats.Clear();
            if (definitions == null)
            {
                return;
            }

            for (int i = 0; i < definitions.Count; i++)
            {
                RpgStatDefinition definition = definitions[i];
                if (definition == null || !definition.id.IsValid)
                {
                    continue;
                }

                _stats[definition.id.Value] = new RpgStatRuntime(definition);
            }
        }

        /// <summary>Rebuilds the upgrade-rule lookup from <paramref name="progression" /> (null clears it).</summary>
        public void BuildUpgradeRules(RpgProgressionDefinition progression)
        {
            _upgradeRules.Clear();
            if (progression == null || progression.upgradeRules == null)
            {
                return;
            }

            for (int i = 0; i < progression.upgradeRules.Length; i++)
            {
                RpgStatUpgradeRule rule = progression.upgradeRules[i];
                if (rule == null || !rule.statId.IsValid)
                {
                    continue;
                }

                _upgradeRules[rule.statId.Value] = rule;
            }
        }

        /// <summary>Removes all stat runtimes and upgrade rules. Invested points are kept; see <see cref="ClearInvestments" />.</summary>
        public void Clear()
        {
            _stats.Clear();
            _upgradeRules.Clear();
        }

        /// <summary>Forgets every invested upgrade point and resets <see cref="RpgStatRuntime.UpgradeCount" />.</summary>
        public void ClearInvestments()
        {
            _investments.Clear();
            foreach (KeyValuePair<string, RpgStatRuntime> pair in _stats)
            {
                pair.Value.UpgradeCount = 0;
            }
        }

        /// <summary>Returns whether a stat with <paramref name="statId" /> exists.</summary>
        public bool Contains(string statId)
        {
            return !string.IsNullOrWhiteSpace(statId) && _stats.ContainsKey(statId);
        }

        /// <summary>Looks up a live stat; a null id is treated as empty.</summary>
        public bool TryGet(string statId, out RpgStatRuntime stat)
        {
            return _stats.TryGetValue(statId ?? string.Empty, out stat);
        }

        /// <summary>Final resolved value of a stat, or 0 when it does not exist.</summary>
        public float GetValue(string statId)
        {
            return TryGet(statId, out RpgStatRuntime stat) ? stat.CurrentValue : 0f;
        }

        /// <summary>Reactive final value of a stat, or null when it does not exist.</summary>
        public ReactivePropertyFloat GetState(string statId)
        {
            return TryGet(statId, out RpgStatRuntime stat) ? stat.ValueState : null;
        }

        /// <summary>
        ///     Adds <paramref name="delta" /> to a stat's base value. Returns false for unknown stats, a NaN or
        ///     infinite delta, or a sum that overflows the float range; the base is unchanged then.
        /// </summary>
        public bool AddBase(string statId, float delta)
        {
            if (!RpgTickUtility.IsFinite(delta) || !TryGet(statId, out RpgStatRuntime stat))
            {
                return false;
            }

            float next = stat.BaseValue + delta;
            if (!RpgTickUtility.IsFinite(next))
            {
                return false;
            }

            stat.BaseValue = next;
            return true;
        }

        /// <summary>
        ///     Sets a stat's base value. Returns false for unknown stats or a NaN or infinite value; the base is
        ///     unchanged then.
        /// </summary>
        public bool SetBase(string statId, float value)
        {
            // WHY: one NaN base turns every derived stat, resource Max and percent into NaN, and with client state
            // commands enabled a remote peer could send one.
            if (!RpgTickUtility.IsFinite(value) || !TryGet(statId, out RpgStatRuntime stat))
            {
                return false;
            }

            stat.BaseValue = value;
            return true;
        }

        /// <summary>Looks up the upgrade rule configured for a stat.</summary>
        public bool TryGetUpgradeRule(string statId, out RpgStatUpgradeRule rule)
        {
            rule = null;
            return !string.IsNullOrWhiteSpace(statId) && _upgradeRules.TryGetValue(statId, out rule) && rule != null;
        }

        /// <summary>Number of upgrade points already invested into a stat.</summary>
        public int GetUpgradeLevel(string statId)
        {
            return _investments.TryGetValue(statId ?? string.Empty, out int count) ? count : 0;
        }

        /// <summary>
        ///     Returns whether the stat exists, has a valid rule, is below its upgrade cap and
        ///     <paramref name="availablePoints" /> cover the rule's cost.
        /// </summary>
        public bool CanUpgrade(string statId, int availablePoints)
        {
            if (!Contains(statId) || !TryGetUpgradeRule(statId, out RpgStatUpgradeRule rule) ||
                rule.costPerUpgrade <= 0)
            {
                return false;
            }

            if (availablePoints < rule.costPerUpgrade)
            {
                return false;
            }

            return rule.maxUpgradeCount < 0 || GetUpgradeLevel(statId) < rule.maxUpgradeCount;
        }

        /// <summary>Records one more invested upgrade point and mirrors it onto the stat runtime.</summary>
        /// <returns>The new invested count.</returns>
        public int AddInvestment(string statId)
        {
            int count = GetUpgradeLevel(statId) + 1;
            SetInvestment(statId, count);
            return count;
        }

        /// <summary>Sets the invested count for a stat id and mirrors it onto the stat runtime when present.</summary>
        public void SetInvestment(string statId, int count)
        {
            if (statId == null)
            {
                return;
            }

            _investments[statId] = count;
            if (_stats.TryGetValue(statId, out RpgStatRuntime stat))
            {
                stat.UpgradeCount = count;
            }
        }

        /// <summary>Resolves the final value of <paramref name="stat" /> at <paramref name="growthLevel" />.</summary>
        public float ResolveStat(RpgStatRuntime stat, int growthLevel,
            IReadOnlyList<BuffStatModifierApplication> modifiers)
        {
            return RpgStatResolver.ResolveStat(stat, growthLevel, modifiers, _upgradeRules);
        }

        /// <summary>Resolves a resource Max including stat upgrades and buff modifiers.</summary>
        public float ResolveResourceMax(RpgResourceRuntime resource,
            IReadOnlyList<BuffStatModifierApplication> modifiers)
        {
            return RpgStatResolver.ResolveResourceMax(resource, _stats, _upgradeRules, modifiers);
        }

        /// <summary>Resolves a resource regeneration rate including stat scaling, upgrades and buffs.</summary>
        public float ResolveRegen(RpgResourceRuntime resource, IReadOnlyList<BuffStatModifierApplication> modifiers)
        {
            return RpgStatResolver.ResolveRegen(resource, _stats, modifiers, _upgradeRules);
        }

        /// <summary>Appends stat bases and invested upgrades to save/snapshot rows (existing rows are kept).</summary>
        public void CaptureTo(ICollection<RpgStatSaveEntry> stats, ICollection<RpgUpgradeSaveEntry> upgrades)
        {
            if (stats != null)
            {
                foreach (KeyValuePair<string, RpgStatRuntime> pair in _stats)
                {
                    stats.Add(new RpgStatSaveEntry { Id = pair.Key, Base = pair.Value.BaseValue });
                }
            }

            if (upgrades != null)
            {
                foreach (KeyValuePair<string, int> pair in _investments)
                {
                    upgrades.Add(new RpgUpgradeSaveEntry { StatId = pair.Key, Count = pair.Value });
                }
            }
        }

        /// <summary>
        ///     Restores base values of known stats; rows for unknown stats and rows with a NaN or infinite base
        ///     (a corrupted save or snapshot) are ignored, so those stats keep their current base.
        /// </summary>
        public void RestoreBases(IReadOnlyList<RpgStatSaveEntry> rows)
        {
            if (rows == null)
            {
                return;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                RpgStatSaveEntry row = rows[i];
                if (row != null && RpgTickUtility.IsFinite(row.Base) &&
                    _stats.TryGetValue(row.Id ?? string.Empty, out RpgStatRuntime stat))
                {
                    stat.BaseValue = row.Base;
                }
            }
        }

        /// <summary>Replaces all investments with <paramref name="rows" /> (unknown stat ids are kept as data).</summary>
        public void RestoreInvestments(IReadOnlyList<RpgUpgradeSaveEntry> rows)
        {
            ClearInvestments();
            if (rows == null)
            {
                return;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                RpgUpgradeSaveEntry row = rows[i];
                if (row != null)
                {
                    SetInvestment(row.StatId, row.Count);
                }
            }
        }
    }
}
