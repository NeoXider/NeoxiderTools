using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     Built-in "chain" op (chain lightning / bounce): damages the first primary target, then hops to
    ///     the nearest not-yet-hit enemy within <see cref="EffectNodeData.Radius" /> (leveled), up to
    ///     <see cref="EffectNodeData.MaxTargets" /> total hits (0 ⇒ 4). Each bounce multiplies the damage by
    ///     the per-bounce falloff parsed from <see cref="EffectNodeData.CustomParam" /> (e.g. "0.85", default 1).
    ///     Hop order is deterministic: nearest first, ties broken by unit id.
    /// </summary>
    public sealed class ChainEffectOperation : IEffectOperation
    {
        private const int DefaultMaxHits = 4;

        private readonly List<UnitId> _queryScratch = new List<UnitId>(16);

        public string Id => AbilityEffectOps.Chain;

        public void Execute(EffectContext context, EffectNodeData node, List<UnitId> targets)
        {
            UnitId current = targets.Count > 0
                ? targets[0]
                : context.PrimaryTargets.Count > 0 ? context.PrimaryTargets[0] : UnitId.None;
            if (!current.IsValid)
            {
                return;
            }

            int maxHits = node.MaxTargets > 0 ? node.MaxTargets : DefaultMaxHits;
            float falloff = ParseFalloff(node.CustomParam);
            float radius = LeveledValueResolver.SampleByLevel(node.RadiusByLevel, context.AbilityLevel, node.Radius);
            AbilityUnit caster = context.System.GetUnit(context.Caster);
            IAbilityWorldAdapter world = context.System.World;

            var visited = new HashSet<UnitId>();
            float multiplier = 1f;

            for (int hit = 0; hit < maxHits && current.IsValid; hit++)
            {
                float amount = LeveledValueResolver.ResolveAmount(node, context, current) * multiplier;
                if (amount > 0f)
                {
                    DamageService.ApplyDamage(context.System, context.Caster, current, amount,
                        node.DamageType, context.AbilityId, context.CastId, context.Random);
                }

                visited.Add(current);
                if (hit + 1 >= maxHits || !world.TryGetPosition(current, out Vector3 from))
                {
                    break;
                }

                UnitId next = FindNearest(world, from, radius, visited, caster, node.TeamFilter, context.System);
                if (!next.IsValid)
                {
                    break;
                }

                current = next;
                multiplier *= falloff;
            }
        }

        private UnitId FindNearest(IAbilityWorldAdapter world, Vector3 from, float radius,
            HashSet<UnitId> visited, AbilityUnit caster, AbilityTeamFilter filter, AbilitySystem system)
        {
            if (radius <= 0f)
            {
                return UnitId.None;
            }

            _queryScratch.Clear();
            world.QueryUnitsInRadius(from, radius, _queryScratch);

            UnitId best = UnitId.None;
            float bestDistSq = float.MaxValue;
            for (int i = 0; i < _queryScratch.Count; i++)
            {
                UnitId id = _queryScratch[i];
                if (visited.Contains(id))
                {
                    continue;
                }

                AbilityUnit unit = system.GetUnit(id);
                if (unit == null || !unit.IsAlive)
                {
                    continue;
                }

                if (caster != null && !AbilitySystem.MatchesTeamFilter(caster, unit, filter))
                {
                    continue;
                }

                if (!world.TryGetPosition(id, out Vector3 pos))
                {
                    continue;
                }

                float distSq = (pos - from).sqrMagnitude;
                if (distSq < bestDistSq || (distSq == bestDistSq && id.Value < best.Value))
                {
                    best = id;
                    bestDistSq = distSq;
                }
            }

            return best;
        }

        private static float ParseFalloff(string customParam)
        {
            if (!string.IsNullOrEmpty(customParam) &&
                float.TryParse(customParam, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                return parsed;
            }

            return 1f;
        }
    }
}
