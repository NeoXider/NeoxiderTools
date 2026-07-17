using System.Collections.Generic;

namespace Neo.Abilities
{
    /// <summary>
    ///     Built-in "resource_change" op: adds (positive Amount) or drains (negative Amount) a resource
    ///     pool — mana burn, mana restore, energy gain... Health changes should use damage/heal instead
    ///     so mitigation and death events stay consistent.
    /// </summary>
    public sealed class ResourceChangeEffectOperation : IEffectOperation
    {
        public string Id => AbilityEffectOps.ResourceChange;

        public void Execute(EffectContext context, EffectNodeData node, List<UnitId> targets)
        {
            if (string.IsNullOrEmpty(node.ResourceId))
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

                float amount = LeveledValueResolver.ResolveAmount(node, context, target.Id);
                if (amount > 0f)
                {
                    target.Resources.Increase(node.ResourceId, amount);
                }
                else if (amount < 0f)
                {
                    target.Resources.Decrease(node.ResourceId, -amount);
                }
            }
        }
    }
}
