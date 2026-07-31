using System.Collections.Generic;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     Built-in "spawn" op: asks the world adapter to spawn an archetype (zone, summon, extra
    ///     projectile) at the target point or each target's position. The host owns what the
    ///     archetype id means (prefab, pool entry, summon preset).
    /// </summary>
    public sealed class SpawnEffectOperation : IEffectOperation
    {
        public string Id => AbilityEffectOps.Spawn;

        public void Execute(EffectContext context, EffectNodeData node, List<UnitId> targets)
        {
            if (string.IsNullOrEmpty(node.ArchetypeId))
            {
                return;
            }

            if (targets.Count == 0)
            {
                if (context.HasTargetPoint)
                {
                    float magnitude = LeveledValueResolver.ResolveAmount(node, context, UnitId.None);
                    context.System.World.RequestSpawn(new SpawnRequest(node.ArchetypeId, context.Caster,
                        context.TargetPoint, Vector3.zero, UnitId.None, context.AbilityId, magnitude));
                }

                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                // WHY: TryGetPosition zeroes its out-param on failure; only accept it on success so
                // the cast-point fallback survives (headless adapters, just-despawned targets).
                if (!context.System.World.TryGetPosition(targets[i], out Vector3 position) &&
                    context.HasTargetPoint)
                {
                    position = context.TargetPoint;
                }
                float magnitude = LeveledValueResolver.ResolveAmount(node, context, targets[i]);
                context.System.World.RequestSpawn(new SpawnRequest(node.ArchetypeId, context.Caster,
                    position, Vector3.zero, targets[i], context.AbilityId, magnitude));
            }
        }
    }
}
