using System.Collections.Generic;

namespace Neo.Abilities
{
    /// <summary>
    ///     Built-in "apply_modifier" op: attaches the catalog modifier <see cref="EffectNodeData.ModifierId" />
    ///     to every resolved target.
    /// </summary>
    public sealed class ApplyModifierEffectOperation : IEffectOperation
    {
        public string Id => AbilityEffectOps.ApplyModifier;

        public void Execute(EffectContext context, EffectNodeData node, List<UnitId> targets)
        {
            if (string.IsNullOrEmpty(node.ModifierId) ||
                !context.System.TryGetModifier(node.ModifierId, out ModifierBlueprint blueprint))
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

                context.System.Modifiers.Apply(blueprint, context.Caster, target.Id, context.AbilityId,
                    context.AbilityLevel);
            }
        }
    }
}
