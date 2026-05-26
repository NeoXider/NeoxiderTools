using System.Collections.Generic;
using UnityEngine;

namespace Neo.Rpg.Runtime
{
    /// <summary>
    ///     Pure (no MonoBehaviour, no Unity API except <see cref="Mathf"/>) calculator that
    ///     turns a stat / resource definition + the current active modifiers into a final value.
    ///     <para>Used by `RpgCharacter` whenever level / buffs / upgrades / equipment change.
    ///     Keeping this isolated guarantees that UI binding, damage formulas, save/load and
    ///     network sync all see the same numbers.</para>
    /// </summary>
    public static class RpgStatResolver
    {
        /// <summary>
        ///     Computes the final value of a stat given its runtime base, optional level growth,
        ///     accumulated upgrade points, and the active buffs that may modify it.
        /// </summary>
        public static float ResolveStat(
            RpgStatRuntime stat,
            int level,
            IReadOnlyList<BuffStatModifierApplication> activeBuffMods,
            IReadOnlyDictionary<string, RpgStatUpgradeRule> upgradeRules)
        {
            float value = stat.BaseValue;

            // Level growth (Dota-style auto-growth).
            if (stat.Definition.affectedByLevel)
            {
                value += stat.Definition.growth.Evaluate(level);
            }

            // Upgrade points (Dark-Souls).
            if (stat.UpgradeCount > 0 && upgradeRules != null &&
                upgradeRules.TryGetValue(stat.Id, out RpgStatUpgradeRule rule) && rule != null)
            {
                value += rule.increasePerPoint * stat.UpgradeCount;
            }

            // Buff modifiers.
            if (activeBuffMods != null)
            {
                float flat = 0f;
                float percent = 0f;
                for (int i = 0; i < activeBuffMods.Count; i++)
                {
                    BuffStatModifierApplication mod = activeBuffMods[i];
                    if (mod.TargetId != stat.Id)
                    {
                        continue;
                    }

                    switch (mod.Type)
                    {
                        case BuffStatType.AddStatFlat:
                            flat += mod.Value * Mathf.Max(1, mod.Stacks);
                            break;
                        case BuffStatType.AddStatPercent:
                            percent += mod.Value * Mathf.Max(1, mod.Stacks);
                            break;
                    }
                }

                value = (value + flat) * (1f + percent / 100f);
            }

            // Min / Max clamps.
            if (stat.Definition.minValue >= 0f && value < stat.Definition.minValue)
            {
                value = stat.Definition.minValue;
            }

            if (stat.Definition.maxValue >= 0f && value > stat.Definition.maxValue)
            {
                value = stat.Definition.maxValue;
            }

            return value;
        }

        /// <summary>
        ///     Computes the final Max for a resource pool given its base, upgrade-derived modifiers
        ///     (e.g. Vitality +1 → Max HP +15) and active buff modifiers.
        /// </summary>
        public static float ResolveResourceMax(
            RpgResourceRuntime resource,
            IReadOnlyDictionary<string, RpgStatRuntime> stats,
            IReadOnlyDictionary<string, RpgStatUpgradeRule> upgradeRules,
            IReadOnlyList<BuffStatModifierApplication> activeBuffMods)
        {
            float value = resource.BaseMax;

            // Upgrade-derived resource modifiers.
            if (stats != null && upgradeRules != null)
            {
                foreach (KeyValuePair<string, RpgStatRuntime> kv in stats)
                {
                    if (kv.Value.UpgradeCount <= 0)
                    {
                        continue;
                    }

                    if (!upgradeRules.TryGetValue(kv.Key, out RpgStatUpgradeRule rule) || rule == null)
                    {
                        continue;
                    }

                    if (rule.derivedResourceModifiers == null)
                    {
                        continue;
                    }

                    for (int i = 0; i < rule.derivedResourceModifiers.Length; i++)
                    {
                        RpgResourceModifier r = rule.derivedResourceModifiers[i];
                        if (r == null || r.resourceId != resource.Id)
                        {
                            continue;
                        }

                        switch (r.kind)
                        {
                            case RpgResourceModifierKind.AddMaxFlat:
                                value += r.value * kv.Value.UpgradeCount;
                                break;
                            case RpgResourceModifierKind.AddMaxPercent:
                                value += resource.BaseMax * (r.value / 100f) * kv.Value.UpgradeCount;
                                break;
                        }
                    }
                }
            }

            // Buff modifiers (Max only).
            if (activeBuffMods != null)
            {
                float flat = 0f;
                float percent = 0f;
                for (int i = 0; i < activeBuffMods.Count; i++)
                {
                    BuffStatModifierApplication mod = activeBuffMods[i];
                    if (mod.TargetId != resource.Id)
                    {
                        continue;
                    }

                    int stacks = Mathf.Max(1, mod.Stacks);

                    switch (mod.Type)
                    {
                        case BuffStatType.AddResourceMaxFlat:
                            flat += mod.Value * stacks;
                            break;
                        case BuffStatType.AddResourceMaxPercent:
                            percent += mod.Value * stacks;
                            break;
                    }
                }

                value = (value + flat) * (1f + percent / 100f);
            }

            if (resource.Definition.maxCap > 0f && value > resource.Definition.maxCap)
            {
                value = resource.Definition.maxCap;
            }

            return Mathf.Max(0f, value);
        }

        /// <summary>
        ///     Computes the per-second regen rate for a resource, including FromStat scaling and buff bonuses.
        /// </summary>
        public static float ResolveRegen(
            RpgResourceRuntime resource,
            IReadOnlyDictionary<string, RpgStatRuntime> stats,
            IReadOnlyList<BuffStatModifierApplication> activeBuffMods)
        {
            RpgRegenDefinition r = resource.Definition.regen;
            if (r == null || !r.enabled)
            {
                return BuffOnlyRegen(resource.Id, activeBuffMods);
            }

            float baseRate = r.mode switch
            {
                RpgRegenMode.FlatPerSecond => r.value,
                RpgRegenMode.PercentMaxPerSecond => resource.Max * (r.value / 100f),
                RpgRegenMode.FlatPerTick => r.tickInterval > 0f ? r.value / r.tickInterval : 0f,
                RpgRegenMode.PercentMaxPerTick =>
                    r.tickInterval > 0f ? resource.Max * (r.value / 100f) / r.tickInterval : 0f,
                RpgRegenMode.FromStat => GetStatValue(stats, r.scalingStat.Value) * r.scalingMultiplier,
                _ => 0f
            };

            // Buff modifiers (regen flat / percent on same resource).
            float buffFlat = 0f;
            float buffPercent = 0f;
            if (activeBuffMods != null)
            {
                for (int i = 0; i < activeBuffMods.Count; i++)
                {
                    BuffStatModifierApplication mod = activeBuffMods[i];
                    if (mod.TargetId != resource.Id)
                    {
                        continue;
                    }

                    int stacks = Mathf.Max(1, mod.Stacks);

                    switch (mod.Type)
                    {
                        case BuffStatType.RegenFlat:
                            buffFlat += mod.Value * stacks;
                            break;
                        case BuffStatType.RegenPercent:
                            buffPercent += mod.Value * stacks;
                            break;
                    }
                }
            }

            return (baseRate + buffFlat) * (1f + buffPercent / 100f);
        }

        private static float BuffOnlyRegen(string resourceId, IReadOnlyList<BuffStatModifierApplication> mods)
        {
            if (mods == null)
            {
                return 0f;
            }

            float flat = 0f;
            for (int i = 0; i < mods.Count; i++)
            {
                BuffStatModifierApplication m = mods[i];
                if (m.TargetId != resourceId)
                {
                    continue;
                }

                if (m.Type == BuffStatType.RegenFlat)
                {
                    flat += m.Value * Mathf.Max(1, m.Stacks);
                }
            }

            return flat;
        }

        private static float GetStatValue(IReadOnlyDictionary<string, RpgStatRuntime> stats, string id)
        {
            if (stats == null || string.IsNullOrEmpty(id))
            {
                return 0f;
            }

            return stats.TryGetValue(id, out RpgStatRuntime s) ? s.CurrentValue : 0f;
        }
    }

    /// <summary>
    ///     Snapshot of one stack of an active buff's modifier — passed to <see cref="RpgStatResolver"/>.
    ///     Avoids reflection / per-frame allocations.
    /// </summary>
    public readonly struct BuffStatModifierApplication
    {
        public readonly BuffStatType Type;
        public readonly string TargetId;
        public readonly string DamageType;
        public readonly float Value;
        public readonly int Stacks;

        public BuffStatModifierApplication(BuffStatType type, string targetId, string damageType, float value,
            int stacks)
        {
            Type = type;
            TargetId = targetId ?? string.Empty;
            DamageType = damageType ?? string.Empty;
            Value = value;
            Stacks = stacks;
        }
    }
}
