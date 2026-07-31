using System;
using System.Collections.Generic;

namespace Neo.Abilities
{
    /// <summary>
    ///     Built-in "execute" op: deals damage derived from the target's health rather than a flat number.
    ///     The resolved <see cref="EffectNodeData.Amount" /> is treated as a fraction (0.1 = 10%) of the
    ///     target's health selected by <see cref="EffectNodeData.CustomParam" />:
    ///     <c>"missing"</c> (max − current), <c>"max"</c>, <c>"current"</c>, or a flat amount otherwise.
    ///     Routes through the full <see cref="DamageService" /> pipeline with the node's damage type.
    /// </summary>
    public sealed class ExecuteEffectOperation : IEffectOperation
    {
        public string Id => AbilityEffectOps.Execute;

        public void Execute(EffectContext context, EffectNodeData node, List<UnitId> targets)
        {
            if (targets.Count == 0)
            {
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                AbilityUnit target = context.System.GetUnit(targets[i]);
                if (target == null || !target.IsAlive)
                {
                    continue;
                }

                float fraction = LeveledValueResolver.ResolveAmount(node, context, target.Id);
                float damage = DamageFor(node.CustomParam, fraction, target);
                if (damage <= 0f)
                {
                    continue;
                }

                DamageService.ApplyDamage(context.System, context.Caster, target.Id, damage,
                    node.DamageType, context.AbilityId, context.CastId, context.Random);
            }
        }

        private static float DamageFor(string mode, float value, AbilityUnit target)
        {
            if (string.Equals(mode, "missing", StringComparison.OrdinalIgnoreCase))
            {
                return value * (target.MaxHealth - target.Health);
            }

            if (string.Equals(mode, "max", StringComparison.OrdinalIgnoreCase))
            {
                return value * target.MaxHealth;
            }

            if (string.Equals(mode, "current", StringComparison.OrdinalIgnoreCase))
            {
                return value * target.Health;
            }

            return value;
        }
    }
}
