using System.Collections.Generic;

namespace Neo.Abilities
{
    /// <summary>
    ///     Built-in "dispel" op: removes dispellable modifiers. By default purges debuffs (a cleanse);
    ///     with CustomParam = "buffs" it strips buffs from the target instead (an enemy purge).
    /// </summary>
    public sealed class DispelEffectOperation : IEffectOperation
    {
        // WHY: cached non-capturing predicates keep a dispel per target allocation-free.
        private static readonly System.Predicate<ModifierInstance> DispellableDebuff =
            m => m.Blueprint.Dispellable && m.Blueprint.IsDebuff;

        private static readonly System.Predicate<ModifierInstance> DispellableBuff =
            m => m.Blueprint.Dispellable && !m.Blueprint.IsDebuff;

        public string Id => AbilityEffectOps.Dispel;

        public void Execute(EffectContext context, EffectNodeData node, List<UnitId> targets)
        {
            bool stripBuffs = string.Equals(node.CustomParam, "buffs", System.StringComparison.OrdinalIgnoreCase);
            System.Predicate<ModifierInstance> predicate = stripBuffs ? DispellableBuff : DispellableDebuff;

            for (int i = 0; i < targets.Count; i++)
            {
                context.System.Modifiers.RemoveWhere(targets[i], predicate);
            }
        }
    }
}
