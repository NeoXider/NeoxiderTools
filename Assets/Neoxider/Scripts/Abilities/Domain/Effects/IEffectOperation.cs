using System.Collections.Generic;

namespace Neo.Abilities
{
    /// <summary>
    ///     A versioned strategy executing one effect node kind (damage, heal, apply_modifier...).
    ///     Ops are registered once in <see cref="EffectOpRegistry" /> and composed in data forever —
    ///     that is what makes new abilities authorable without code.
    /// </summary>
    public interface IEffectOperation
    {
        /// <summary>Registry id, e.g. "damage". Case-insensitive.</summary>
        string Id { get; }

        /// <summary>Executes the node against the already-resolved target list.</summary>
        void Execute(EffectContext context, EffectNodeData node, List<UnitId> targets);
    }
}
