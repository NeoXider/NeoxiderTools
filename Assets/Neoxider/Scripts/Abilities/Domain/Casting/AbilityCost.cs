using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>How much of a resource a cost takes.</summary>
    public enum AbilityCostMode
    {
        /// <summary>Exactly the resolved amount; the cast fails when it cannot be paid (the default).</summary>
        Fixed = 0,

        /// <summary>
        ///     As much as is available up to the resolved amount: min(available, amount). The cast needs at least
        ///     <see cref="AbilityCost.MinAmount" /> available.
        /// </summary>
        UpTo = 1,

        /// <summary>
        ///     Everything available (the resolved amount is ignored). The cast needs at least
        ///     <see cref="AbilityCost.MinAmount" /> available.
        /// </summary>
        All = 2
    }

    /// <summary>
    ///     One resource cost of a cast (e.g. 50 mana). The amount may depend on the ability level
    ///     (<see cref="AmountByLevel" />) or come from a named special of the ability (<see cref="AmountKey" />: per level,
    ///     linear or scaled from a property or resource, e.g. 10% of max mana). Mana costs honor the mana_cost_mul
    ///     property. <see cref="Mode" /> chooses between a fixed price, "up to" and "everything available"; what a cast
    ///     actually paid scales effects through the spent term of <see cref="LeveledValue" />.
    /// </summary>
    [Serializable]
    public struct AbilityCost
    {
        [Tooltip("Resource pool id (health, mana, or custom).")]
        public string ResourceId;

        [Tooltip("Amount spent on cast.")]
        public float Amount;

        [Tooltip("Per-ability-level amounts (clamped to the last entry). Empty ⇒ use Amount.")]
        public float[] AmountByLevel;

        [Tooltip("Named special value of the ability (Specials) giving the amount, e.g. 10% of max mana via Scale Stat " +
                 "Resource Max. '' ⇒ Amount / Amount By Level.")]
        public string AmountKey;

        [Tooltip("Fixed: exactly the amount. Up To: min(available, amount). All: everything available. Up To and All " +
                 "need at least Min Amount available.")]
        public AbilityCostMode Mode;

        [Tooltip("Up To / All: the cast fails when less than this is available (0 = may cast with nothing to spend).")]
        public float MinAmount;

        public AbilityCost(string resourceId, float amount)
        {
            ResourceId = resourceId;
            Amount = amount;
            AmountByLevel = null;
            AmountKey = null;
            Mode = AbilityCostMode.Fixed;
            MinAmount = 0f;
        }

        public AbilityCost(string resourceId, float amount, AbilityCostMode mode, float minAmount = 0f)
            : this(resourceId, amount)
        {
            Mode = mode;
            MinAmount = minAmount;
        }

        /// <summary>
        ///     Authored amount at an ability level, before the cost rule (mana_cost_mul) and the spend mode: the named
        ///     special <see cref="AmountKey" /> of <paramref name="specials" /> when found (resolved for
        ///     <paramref name="caster" /> and <paramref name="target" />), else <see cref="AmountByLevel" /> sampled by
        ///     <paramref name="abilityLevel" />, else <see cref="Amount" />.
        /// </summary>
        public float ResolveAmount(List<AbilitySpecialValue> specials, AbilitySystem system, UnitId caster,
            int abilityLevel, UnitId target)
        {
            if (!string.IsNullOrEmpty(AmountKey) && specials != null)
            {
                for (int i = 0; i < specials.Count; i++)
                {
                    if (AbilityIdComparer.Same(specials[i].Name, AmountKey))
                    {
                        LeveledValue special = specials[i].Value;
                        return LeveledValueResolver.Resolve(in special, system, caster, abilityLevel, target);
                    }
                }
            }

            return LeveledValueResolver.SampleByLevel(AmountByLevel, abilityLevel, Amount);
        }
    }

    /// <summary>How much of one resource a cast actually paid (after the cost rule and the spend mode).</summary>
    public readonly struct AbilitySpentResource
    {
        public readonly string ResourceId;
        public readonly float Amount;

        public AbilitySpentResource(string resourceId, float amount)
        {
            ResourceId = resourceId;
            Amount = amount;
        }
    }
}
