using System.Collections.Generic;

namespace Neo.Abilities
{
    /// <summary>
    ///     Built-in "dispel" op: removes dispellable modifiers. By default purges debuffs (a cleanse);
    ///     with CustomParam = "buffs" it strips buffs from the target instead (an enemy purge).
    /// </summary>
    public sealed class DispelEffectOperation : IEffectOperation
    {
        public string Id => AbilityEffectOps.Dispel;

        public void Execute(EffectContext context, EffectNodeData node, List<UnitId> targets)
        {
            bool stripBuffs = string.Equals(node.CustomParam, "buffs", System.StringComparison.OrdinalIgnoreCase);

            for (int i = 0; i < targets.Count; i++)
            {
                context.System.Modifiers.RemoveWhere(targets[i], m =>
                    m.Blueprint.Dispellable && m.Blueprint.IsDebuff != stripBuffs);
            }
        }
    }
}
