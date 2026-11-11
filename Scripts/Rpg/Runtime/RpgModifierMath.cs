using System.Collections.Generic;
using UnityEngine;

namespace Neo.Rpg.Runtime
{
    /// <summary>
    ///     Allocation-free aggregation of projected buff modifiers (see
    ///     <see cref="RpgEffectShelf.BuildModifierApplications" />) into the combat multipliers used by
    ///     <c>RpgCharacter</c>. Legacy enum aliases are folded in exactly as the character facade always did.
    /// </summary>
    public static class RpgModifierMath
    {
        /// <summary>
        ///     Outgoing damage multiplier: <c>max(0, 1 + (OutgoingDamagePercent + DamagePercent) / 100)</c>,
        ///     each value scaled by its stack count.
        /// </summary>
        public static float GetOutgoingDamageMultiplier(IReadOnlyList<BuffStatModifierApplication> modifiers)
        {
            float percent = 0f;
            int count = modifiers?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                BuffStatModifierApplication m = modifiers[i];
                if (m.Type == BuffStatType.OutgoingDamagePercent || m.Type == BuffStatType.DamagePercent)
                {
                    // WHY: DamagePercent is a legacy alias kept for backward compatibility with older buff data.
                    percent += m.Value * Mathf.Max(1, m.Stacks);
                }
            }

            return Mathf.Max(0f, 1f + percent / 100f);
        }

        /// <summary>
        ///     Incoming damage multiplier for <paramref name="damageType" />:
        ///     <c>max(0, 1 + (IncomingDamagePercent - DefensePercent) / 100 - resist / 100)</c>, where resist sums
        ///     <see cref="BuffStatType.DamageTypeResistPercent" /> (typed damage only) and
        ///     <see cref="BuffStatType.SpecificDefensePercent" /> rows matching the damage type.
        /// </summary>
        public static float GetIncomingDamageMultiplier(IReadOnlyList<BuffStatModifierApplication> modifiers,
            string damageType)
        {
            float percent = 0f;
            float resist = 0f;
            int count = modifiers?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                BuffStatModifierApplication m = modifiers[i];
                int stacks = Mathf.Max(1, m.Stacks);
                if (m.Type == BuffStatType.IncomingDamagePercent)
                {
                    percent += m.Value * stacks;
                }
                else if (m.Type == BuffStatType.DefensePercent)
                {
                    // WHY: DefensePercent is a legacy alias kept for backward compatibility with older buff data.
                    percent -= m.Value * stacks;
                }
                else if (m.Type == BuffStatType.DamageTypeResistPercent &&
                         !string.IsNullOrEmpty(damageType) && m.DamageType == damageType)
                {
                    resist += m.Value * stacks;
                }
                else if (m.Type == BuffStatType.SpecificDefensePercent && m.DamageType == damageType)
                {
                    resist += m.Value * stacks;
                }
            }

            return Mathf.Max(0f, 1f + percent / 100f - resist / 100f);
        }

        /// <summary>
        ///     Buff part of the movement multiplier: <c>1 + (MoveSpeedPercent + MovementSpeedPercent) / 100</c>,
        ///     not clamped so callers can combine it with status multipliers before clamping.
        /// </summary>
        public static float GetMovementSpeedBuffMultiplier(IReadOnlyList<BuffStatModifierApplication> modifiers)
        {
            float percent = 0f;
            int count = modifiers?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                BuffStatModifierApplication m = modifiers[i];
                if (m.Type == BuffStatType.MoveSpeedPercent || m.Type == BuffStatType.MovementSpeedPercent)
                {
                    // WHY: MovementSpeedPercent is a legacy alias kept for backward compatibility with older buff data.
                    percent += m.Value * Mathf.Max(1, m.Stacks);
                }
            }

            return 1f + percent / 100f;
        }

        /// <summary>
        ///     Attack speed multiplier from <see cref="BuffStatType.AttackSpeedPercent" />:
        ///     <c>max(0, 1 + percent / 100)</c>. A value of 2 means attacks recover twice as fast.
        /// </summary>
        public static float GetAttackSpeedMultiplier(IReadOnlyList<BuffStatModifierApplication> modifiers)
        {
            float percent = 0f;
            int count = modifiers?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                BuffStatModifierApplication m = modifiers[i];
                if (m.Type == BuffStatType.AttackSpeedPercent)
                {
                    percent += m.Value * Mathf.Max(1, m.Stacks);
                }
            }

            return Mathf.Max(0f, 1f + percent / 100f);
        }
    }
}
