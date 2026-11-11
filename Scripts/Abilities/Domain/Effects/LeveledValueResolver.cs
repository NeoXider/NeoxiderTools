using System;
using System.Collections.Generic;

namespace Neo.Abilities
{
    /// <summary>
    ///     Pure, deterministic resolution of <see cref="LeveledValue" /> (and per-level float arrays).
    ///     Never touches <c>UnityEngine.Random</c> or <c>Time</c>, allocates nothing on the hot path.
    ///     Contract: an all-default <see cref="LeveledValue" /> (or an all-default effect node) resolves
    ///     to its flat <c>Base</c>/<c>Amount</c>, so existing data is byte-for-byte unchanged.
    /// </summary>
    public static class LeveledValueResolver
    {
        /// <summary>
        ///     Samples a per-level float array by ability/unit level (1-based), clamped to the array bounds.
        ///     Falls back to <paramref name="fallback" /> when the array is null/empty.
        /// </summary>
        public static float SampleByLevel(float[] byLevel, int level, float fallback)
        {
            if (byLevel == null || byLevel.Length == 0)
            {
                return fallback;
            }

            return byLevel[ClampIndex(level, byLevel.Length)];
        }

        /// <summary>The integer twin of <see cref="SampleByLevel(float[], int, float)" /> (charges, target caps).</summary>
        public static int SampleByLevel(int[] byLevel, int level, int fallback)
        {
            if (byLevel == null || byLevel.Length == 0)
            {
                return fallback;
            }

            return byLevel[ClampIndex(level, byLevel.Length)];
        }

        /// <summary>
        ///     Resolves a <see cref="LeveledValue" /> in a context for a specific target.
        ///     1) picks the driving level (ability / caster-unit / target-unit / none ⇒ 1),
        ///     2) picks the base (per-level array, else Base + linear term),
        ///     3) adds the spent term (SpentCoefficient * what the context's cast spent of SpentResource),
        ///     4) applies the scaling term (ScalePerPoint * stat of caster or target), added or multiplied.
        /// </summary>
        public static float Resolve(in LeveledValue value, EffectContext context, UnitId target)
        {
            return ResolveCore(in value, context.System, context.Caster, context.AbilityLevel, target, context);
        }

        /// <summary>
        ///     Resolves a <see cref="LeveledValue" /> outside an effect execution (cast costs, UI previews): the same
        ///     steps as <see cref="Resolve(in LeveledValue, EffectContext, UnitId)" /> without the spent term, which
        ///     only exists once a cast has paid.
        /// </summary>
        public static float Resolve(in LeveledValue value, AbilitySystem system, UnitId caster, int abilityLevel,
            UnitId target)
        {
            return ResolveCore(in value, system, caster, abilityLevel, target, null);
        }

        /// <summary>
        ///     Resolves the amount an effect node deals to <paramref name="target" />. Uses the node's named
        ///     special (<see cref="EffectNodeData.AmountKey" />) when set and found on the context, otherwise
        ///     the node's inline leveled fields, plus <see cref="EffectNodeData.AmountPerStack" /> per extra stack of the
        ///     modifier running the node. All-default ⇒ <see cref="EffectNodeData.Amount" />.
        /// </summary>
        public static float ResolveAmount(EffectNodeData node, EffectContext context, UnitId target)
        {
            float amount;
            if (!string.IsNullOrEmpty(node.AmountKey) &&
                context.TryGetSpecial(node.AmountKey, out LeveledValue special))
            {
                amount = Resolve(in special, context, target);
            }
            else
            {
                LeveledValue inline = node.ToLeveledValue();
                amount = Resolve(in inline, context, target);
            }

            // WHY: per-stack scaling of tick / reaction amounts (stacking poison); 0 keeps every existing node unchanged.
            if (node.AmountPerStack != 0f && context.SourceStacks > 1)
            {
                amount += node.AmountPerStack * (context.SourceStacks - 1);
            }

            return amount;
        }

        /// <summary>
        ///     True when one of <paramref name="nodes" /> scales with a spent resource, inline
        ///     (<see cref="EffectNodeData.AmountSpentResource" />) or through a named special of
        ///     <paramref name="specials" />. Allocation-free.
        /// </summary>
        public static bool UsesSpent(List<EffectNodeData> nodes, List<AbilitySpecialValue> specials)
        {
            if (nodes == null)
            {
                return false;
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                EffectNodeData node = nodes[i];
                if (node == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(node.AmountSpentResource))
                {
                    return true;
                }

                if (!string.IsNullOrEmpty(node.AmountKey) && specials != null)
                {
                    for (int s = 0; s < specials.Count; s++)
                    {
                        if (AbilityIdComparer.Same(specials[s].Name, node.AmountKey))
                        {
                            if (!string.IsNullOrEmpty(specials[s].Value.SpentResource))
                            {
                                return true;
                            }

                            break;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>True when the tick effects or a reaction of <paramref name="modifier" /> scale with a spent resource.</summary>
        public static bool UsesSpent(ModifierBlueprint modifier, List<AbilitySpecialValue> specials)
        {
            if (modifier == null)
            {
                return false;
            }

            if (UsesSpent(modifier.TickEffects, specials))
            {
                return true;
            }

            List<ModifierEventReaction> reactions = modifier.EventReactions;
            if (reactions == null)
            {
                return false;
            }

            for (int i = 0; i < reactions.Count; i++)
            {
                if (reactions[i] != null && UsesSpent(reactions[i].Effects, specials))
                {
                    return true;
                }
            }

            return false;
        }

        private static float ResolveCore(in LeveledValue value, AbilitySystem system, UnitId caster, int abilityLevel,
            UnitId target, EffectContext spentSource)
        {
            int level = LevelFor(value.LevelFrom, system, caster, abilityLevel, target);

            float result;
            if (value.Levels != null && value.Levels.Length > 0)
            {
                result = SampleByLevel(value.Levels, level, value.Base);
            }
            else
            {
                result = value.Base + value.PerLevel * (level - 1);
            }

            if (spentSource != null && !string.IsNullOrEmpty(value.SpentResource) && value.SpentCoefficient != 0f)
            {
                result += value.SpentCoefficient * spentSource.GetSpent(value.SpentResource);
            }

            if (!string.IsNullOrEmpty(value.ScaleProperty) && value.ScalePerPoint != 0f && system != null)
            {
                UnitId scaleUnit = value.ScaleFrom == ScaleAmountSource.Target ? target : caster;
                AbilityUnit unit = system.GetUnit(scaleUnit);
                if (unit != null)
                {
                    float term = value.ScalePerPoint * ReadStat(unit, value.ScaleStat, value.ScaleProperty);
                    result = value.ScaleMode == ScaleAmountMode.Multiply ? result * (1f + term) : result + term;
                }
            }

            return result;
        }

        private static float ReadStat(AbilityUnit unit, ScaleAmountStat stat, string id)
        {
            switch (stat)
            {
                case ScaleAmountStat.ResourceCurrent:
                    return unit.Resources.GetCurrent(id);
                case ScaleAmountStat.ResourceMax:
                    return unit.Resources.GetMax(id);
                case ScaleAmountStat.ResourceMissing:
                    return MathF.Max(0f, unit.Resources.GetMax(id) - unit.Resources.GetCurrent(id));
                default:
                    return unit.GetProperty(id);
            }
        }

        private static int ClampIndex(int level, int length)
        {
            int index = level - 1;
            if (index < 0)
            {
                return 0;
            }

            return index >= length ? length - 1 : index;
        }

        private static int LevelFor(LevelSource source, AbilitySystem system, UnitId caster, int abilityLevel,
            UnitId target)
        {
            switch (source)
            {
                case LevelSource.AbilityLevel:
                    return abilityLevel;
                case LevelSource.CasterUnitLevel:
                    return LevelOf(system, caster);
                case LevelSource.TargetUnitLevel:
                    return LevelOf(system, target);
                default:
                    return 1;
            }
        }

        private static int LevelOf(AbilitySystem system, UnitId unitId)
        {
            AbilityUnit unit = system?.GetUnit(unitId);
            return unit != null ? unit.Level : 1;
        }
    }
}
