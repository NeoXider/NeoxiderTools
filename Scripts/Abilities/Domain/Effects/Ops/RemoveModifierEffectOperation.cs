using System.Collections.Generic;

namespace Neo.Abilities
{
    /// <summary>
    ///     Built-in "remove_modifier" op: removes every instance of <see cref="EffectNodeData.ModifierId" />
    ///     from the resolved targets.
    /// </summary>
    public sealed class RemoveModifierEffectOperation : IEffectOperation
    {
        public string Id => AbilityEffectOps.RemoveModifier;

        public void Execute(EffectContext context, EffectNodeData node, List<UnitId> targets)
        {
            if (string.IsNullOrEmpty(node.ModifierId))
            {
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                context.System.Modifiers.RemoveById(targets[i], node.ModifierId);
            }
        }
    }
}
