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

            int index = level - 1;
            if (index < 0)
            {
                index = 0;
            }
            else if (index >= byLevel.Length)
            {
                index = byLevel.Length - 1;
            }

            return byLevel[index];
        }

        /// <summary>
        ///     Resolves a <see cref="LeveledValue" /> in a context for a specific target.
        ///     1) picks the driving level (ability / caster-unit / target-unit / none ⇒ 1),
        ///     2) picks the base (per-level array, else Base + linear term),
        ///     3) adds the property-scaling term (ScalePerPoint * property of caster or target).
        /// </summary>
        public static float Resolve(in LeveledValue value, EffectContext context, UnitId target)
        {
            int level = LevelFor(value.LevelFrom, context, target);

            float result;
            if (value.Levels != null && value.Levels.Length > 0)
            {
                result = SampleByLevel(value.Levels, level, value.Base);
            }
            else
            {
                result = value.Base + value.PerLevel * (level - 1);
            }

            if (!string.IsNullOrEmpty(value.ScaleProperty) && value.ScalePerPoint != 0f)
            {
                UnitId scaleUnit = value.ScaleFrom == ScaleAmountSource.Target ? target : context.Caster;
                AbilityUnit unit = context.System.GetUnit(scaleUnit);
                if (unit != null)
                {
                    result += value.ScalePerPoint * unit.GetProperty(value.ScaleProperty);
                }
            }

            return result;
        }

        /// <summary>
        ///     Resolves the amount an effect node deals to <paramref name="target" />. Uses the node's named
        ///     special (<see cref="EffectNodeData.AmountKey" />) when set and found on the context, otherwise
        ///     the node's inline leveled fields. All-default ⇒ <see cref="EffectNodeData.Amount" />.
        /// </summary>
        public static float ResolveAmount(EffectNodeData node, EffectContext context, UnitId target)
        {
            if (!string.IsNullOrEmpty(node.AmountKey) &&
                context.TryGetSpecial(node.AmountKey, out LeveledValue special))
            {
                return Resolve(in special, context, target);
            }

            LeveledValue inline = node.ToLeveledValue();
            return Resolve(in inline, context, target);
        }

        private static int LevelFor(LevelSource source, EffectContext context, UnitId target)
        {
            switch (source)
            {
                case LevelSource.AbilityLevel:
                    return context.AbilityLevel;
                case LevelSource.CasterUnitLevel:
                    return LevelOf(context, context.Caster);
                case LevelSource.TargetUnitLevel:
                    return LevelOf(context, target);
                default:
                    return 1;
            }
        }

        private static int LevelOf(EffectContext context, UnitId unitId)
        {
            AbilityUnit unit = context.System.GetUnit(unitId);
            return unit != null ? unit.Level : 1;
        }
    }
}
