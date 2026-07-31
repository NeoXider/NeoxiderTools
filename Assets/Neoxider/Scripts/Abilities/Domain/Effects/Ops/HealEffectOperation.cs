using System.Collections.Generic;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     Built-in "heal" op: restores health honoring the target's healing_received_mul.
    ///     Never revives — dead targets are skipped.
    /// </summary>
    public sealed class HealEffectOperation : IEffectOperation
    {
        public string Id => AbilityEffectOps.Heal;

        public void Execute(EffectContext context, EffectNodeData node, List<UnitId> targets)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                AbilityUnit target = context.System.GetUnit(targets[i]);
                if (target == null || !target.IsAlive)
                {
                    continue;
                }

                float resolved = LeveledValueResolver.ResolveAmount(node, context, target.Id);
                if (resolved <= 0f)
                {
                    continue;
                }

                float mul = Mathf.Max(0f, target.GetProperty(AbilityProperties.HealingReceivedMul, 1f));
                float amount = resolved * mul;
                if (amount <= 0f)
                {
                    continue;
                }

                float before = target.Health;
                target.Resources.Increase(AbilityResourceIds.Health, amount);
                float effective = target.Health - before;
                if (effective > 0f)
                {
                    context.System.Events.Publish(new AbilityEventArgs(AbilityEvents.HealReceived,
                        target.Id, context.Caster, effective, context.AbilityId, castId: context.CastId));
                }
            }
        }
    }
}
