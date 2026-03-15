using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Rpg
{
    internal static class RpgCombatMath
    {
        internal static float GetOutgoingDamageMultiplier(IEnumerable<ActiveBuffEntry> activeBuffs, Func<string, BuffDefinition> resolveBuff)
        {
            return 1f + GetPercentSum(activeBuffs, resolveBuff, BuffStatType.DamagePercent) / 100f;
        }

        internal static float GetIncomingDamageMultiplier(IEnumerable<ActiveBuffEntry> activeBuffs, Func<string, BuffDefinition> resolveBuff)
        {
            float defensePercent = GetPercentSum(activeBuffs, resolveBuff, BuffStatType.DefensePercent);
            return Mathf.Clamp01(1f - defensePercent / 100f);
        }

        internal static float GetRegenPerSecond(float baseRegen, IEnumerable<ActiveBuffEntry> activeBuffs, Func<string, BuffDefinition> resolveBuff)
        {
            return Mathf.Max(0f, baseRegen + GetFlatSum(activeBuffs, resolveBuff, BuffStatType.HpRegenPerSecond));
        }

        internal static float GetMovementSpeedMultiplier(IEnumerable<ActiveBuffEntry> activeBuffs,
            IEnumerable<ActiveStatusEntry> activeStatuses,
            Func<string, BuffDefinition> resolveBuff,
            Func<string, StatusEffectDefinition> resolveStatus)
        {
            float buffMultiplier = 1f + GetPercentSum(activeBuffs, resolveBuff, BuffStatType.MovementSpeedPercent) / 100f;
            float statusMultiplier = 1f;
            if (activeStatuses != null)
            {
                foreach (ActiveStatusEntry entry in activeStatuses)
                {
                    StatusEffectDefinition definition = resolveStatus(entry.StatusId);
                    if (definition == null)
                        continue;
                    statusMultiplier *= Mathf.Max(0f, definition.MovementSpeedMultiplier);
                }
            }
            return Mathf.Max(0f, buffMultiplier * statusMultiplier);
        }

        internal static bool HasBlockingStatus(IEnumerable<ActiveStatusEntry> activeStatuses, Func<string, StatusEffectDefinition> resolveStatus)
        {
            if (activeStatuses == null)
                return false;
            foreach (ActiveStatusEntry entry in activeStatuses)
            {
                StatusEffectDefinition definition = resolveStatus(entry.StatusId);
                if (definition != null && definition.BlocksActions)
                    return true;
            }
            return false;
        }

        private static float GetPercentSum(IEnumerable<ActiveBuffEntry> activeBuffs, Func<string, BuffDefinition> resolveBuff, BuffStatType statType)
        {
            return GetFlatSum(activeBuffs, resolveBuff, statType);
        }

        private static float GetFlatSum(IEnumerable<ActiveBuffEntry> activeBuffs, Func<string, BuffDefinition> resolveBuff, BuffStatType statType)
        {
            float total = 0f;
            if (activeBuffs == null)
                return total;
            foreach (ActiveBuffEntry entry in activeBuffs)
            {
                BuffDefinition definition = resolveBuff(entry.BuffId);
                if (definition == null)
                    continue;
                BuffStatModifier[] modifiers = definition.Modifiers;
                for (int i = 0; i < modifiers.Length; i++)
                {
                    BuffStatModifier modifier = modifiers[i];
                    if (modifier != null && modifier.StatType == statType)
                        total += modifier.Value;
                }
            }
            return total;
        }
    }
}
