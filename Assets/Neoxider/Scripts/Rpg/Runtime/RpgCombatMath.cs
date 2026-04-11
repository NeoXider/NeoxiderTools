using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Rpg
{
    public static class RpgCombatMath
    {
        public static float GetOutgoingDamageMultiplier(IEnumerable<ActiveBuffEntry> activeBuffs,
            Func<string, BuffDefinition> resolveBuff)
        {
            return 1f + GetPercentSum(activeBuffs, resolveBuff, BuffStatType.DamagePercent) / 100f;
        }

        public static float GetIncomingDamageMultiplier(IEnumerable<ActiveBuffEntry> activeBuffs,
            Func<string, BuffDefinition> resolveBuff, string damageType)
        {
            float defensePercent = GetPercentSum(activeBuffs, resolveBuff, BuffStatType.DefensePercent);
            
            if (!string.IsNullOrEmpty(damageType))
            {
                defensePercent += GetSpecificFlatSum(activeBuffs, resolveBuff, BuffStatType.SpecificDefensePercent, damageType);
            }

            return Mathf.Clamp01(1f - defensePercent / 100f);
        }

        public static float GetRegenPerSecond(float baseRegen, IEnumerable<ActiveBuffEntry> activeBuffs,
            Func<string, BuffDefinition> resolveBuff)
        {
            return Mathf.Max(0f, baseRegen + GetFlatSum(activeBuffs, resolveBuff, BuffStatType.HpRegenPerSecond));
        }

        public static float GetMovementSpeedMultiplier(IEnumerable<ActiveBuffEntry> activeBuffs,
            IEnumerable<ActiveStatusEntry> activeStatuses,
            Func<string, BuffDefinition> resolveBuff,
            Func<string, StatusEffectDefinition> resolveStatus)
        {
            float buffMultiplier =
                1f + GetPercentSum(activeBuffs, resolveBuff, BuffStatType.MovementSpeedPercent) / 100f;
            float statusMultiplier = 1f;
            if (activeStatuses != null)
            {
                foreach (ActiveStatusEntry entry in activeStatuses)
                {
                    StatusEffectDefinition definition = resolveStatus(entry.StatusId);
                    if (definition == null)
                    {
                        continue;
                    }

                    statusMultiplier *= Mathf.Max(0f, definition.MovementSpeedMultiplier);
                }
            }

            return Mathf.Max(0f, buffMultiplier * statusMultiplier);
        }

        public static bool HasBlockingStatus(IEnumerable<ActiveStatusEntry> activeStatuses,
            Func<string, StatusEffectDefinition> resolveStatus)
        {
            if (activeStatuses == null)
            {
                return false;
            }

            foreach (ActiveStatusEntry entry in activeStatuses)
            {
                StatusEffectDefinition definition = resolveStatus(entry.StatusId);
                if (definition != null && definition.BlocksActions)
                {
                    return true;
                }
            }

            return false;
        }

        private static float GetPercentSum(IEnumerable<ActiveBuffEntry> activeBuffs,
            Func<string, BuffDefinition> resolveBuff, BuffStatType statType)
        {
            return GetFlatSum(activeBuffs, resolveBuff, statType);
        }

        private static float GetFlatSum(IEnumerable<ActiveBuffEntry> activeBuffs,
            Func<string, BuffDefinition> resolveBuff, BuffStatType statType)
        {
            float total = 0f;
            if (activeBuffs == null)
            {
                return total;
            }

            foreach (ActiveBuffEntry entry in activeBuffs)
            {
                BuffDefinition definition = resolveBuff(entry.BuffId);
                if (definition == null)
                {
                    continue;
                }

                BuffStatModifier[] modifiers = definition.Modifiers;
                for (int i = 0; i < modifiers.Length; i++)
                {
                    BuffStatModifier modifier = modifiers[i];
                    if (modifier != null && modifier.StatType == statType)
                    {
                        total += modifier.Value;
                    }
                }
            }

            return total;
        }

        private static float GetSpecificFlatSum(IEnumerable<ActiveBuffEntry> activeBuffs,
            Func<string, BuffDefinition> resolveBuff, BuffStatType statType, string specificType)
        {
            float total = 0f;
            if (activeBuffs == null)
            {
                return total;
            }

            foreach (ActiveBuffEntry entry in activeBuffs)
            {
                BuffDefinition definition = resolveBuff(entry.BuffId);
                if (definition == null)
                {
                    continue;
                }

                BuffStatModifier[] modifiers = definition.Modifiers;
                for (int i = 0; i < modifiers.Length; i++)
                {
                    BuffStatModifier modifier = modifiers[i];
                    if (modifier != null && modifier.StatType == statType && string.Equals(modifier.SpecificDamageType, specificType, StringComparison.OrdinalIgnoreCase))
                    {
                        total += modifier.Value;
                    }
                }
            }

            return total;
        }
    }
}
