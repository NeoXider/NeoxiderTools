using System.Collections.Generic;

namespace Neo.Abilities
{
    /// <summary>
    ///     Built-in "damage" op: routes through the full <see cref="DamageService" /> pipeline.
    ///     Amount is additionally scaled by the caster's spell_power percentage when present.
    /// </summary>
    public sealed class DamageEffectOperation : IEffectOperation
    {
        public string Id => AbilityEffectOps.Damage;

        public void Execute(EffectContext context, EffectNodeData node, List<UnitId> targets)
        {
            if (targets.Count == 0)
            {
                return;
            }

            float spellFactor = 1f;
            AbilityUnit caster = context.System.GetUnit(context.Caster);
            if (caster != null)
            {
                float spellPower = caster.GetProperty(AbilityProperties.SpellPower);
                if (spellPower != 0f)
                {
                    spellFactor = 1f + spellPower / 100f;
                }
            }

            for (int i = 0; i < targets.Count; i++)
            {
                float amount = LeveledValueResolver.ResolveAmount(node, context, targets[i]) * spellFactor;
                if (amount <= 0f)
                {
                    continue;
                }

                DamageService.ApplyDamage(context.System, context.Caster, targets[i], amount,
                    node.DamageType, context.AbilityId, context.CastId, context.Random);
            }
        }
    }
}
