using System;
using System.Collections.Generic;

namespace Neo.Abilities
{
    /// <summary>
    ///     Built-in "modify_damage" op for before_deal_damage / before_take_damage reactions: adjusts the damage in
    ///     flight. <see cref="EffectNodeData.CustomParam" /> selects the mode: <c>"multiply"</c> (default; damage ×=
    ///     amount), <c>"add"</c> (flat bonus before mitigation), <c>"absorb"</c> (flat block after mitigation) or
    ///     <c>"cancel"</c>. The amount resolves through the leveled-value fields (per stack included). Outside a before
    ///     hook it does nothing.
    /// </summary>
    public sealed class ModifyDamageEffectOperation : IEffectOperation
    {
        public const string ModeMultiply = "multiply";
        public const string ModeAdd = "add";
        public const string ModeAbsorb = "absorb";
        public const string ModeCancel = "cancel";

        public string Id => AbilityEffectOps.ModifyDamage;

        public void Execute(EffectContext context, EffectNodeData node, List<UnitId> targets)
        {
            DamageContext damage = context.Damage;
            if (damage == null || !damage.CanModify)
            {
                return;
            }

            string mode = node.CustomParam;
            if (Is(mode, ModeCancel))
            {
                damage.Cancel();
                return;
            }

            float amount = LeveledValueResolver.ResolveAmount(node, context, damage.Target);
            if (float.IsNaN(amount) || float.IsInfinity(amount))
            {
                return;
            }

            if (Is(mode, ModeAdd))
            {
                damage.FlatAdd += amount;
            }
            else if (Is(mode, ModeAbsorb))
            {
                damage.Absorb += MathF.Max(0f, amount);
            }
            else
            {
                damage.Multiplier *= MathF.Max(0f, amount);
            }
        }

        /// <summary>True when <paramref name="mode" /> is empty (multiply) or one of the known modes.</summary>
        public static bool IsKnownMode(string mode)
        {
            return string.IsNullOrEmpty(mode) || Is(mode, ModeMultiply) || Is(mode, ModeAdd) || Is(mode, ModeAbsorb) ||
                   Is(mode, ModeCancel);
        }

        private static bool Is(string mode, string expected)
        {
            return string.Equals(mode, expected, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    ///     Built-in "reflect" op for before_take_damage reactions: after the hit, the resolved amount as a fraction
    ///     (0.3 = 30%) of the mitigated damage goes back to the attacker as <see cref="EffectNodeData.DamageType" />
    ///     damage. Reflected damage is never reflected again. Outside a before hook it does nothing.
    /// </summary>
    public sealed class ReflectEffectOperation : IEffectOperation
    {
        public string Id => AbilityEffectOps.Reflect;

        public void Execute(EffectContext context, EffectNodeData node, List<UnitId> targets)
        {
            DamageContext damage = context.Damage;
            if (damage == null || !damage.CanModify || damage.HasFlag(DamageFlags.Reflected))
            {
                return;
            }

            float fraction = LeveledValueResolver.ResolveAmount(node, context, damage.Target);
            if (!(fraction > 0f) || float.IsInfinity(fraction))
            {
                return;
            }

            damage.ReflectFraction += fraction;
            if (!string.IsNullOrEmpty(node.DamageType))
            {
                damage.ReflectDamageType = node.DamageType;
            }
        }
    }
}
