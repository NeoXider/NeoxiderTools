using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     Resolves the units an effect node applies to inside an effect context. Called once per executed node;
    ///     <paramref name="results" /> is an empty scratch list owned by the system.
    /// </summary>
    public interface IEffectTargetResolver
    {
        void Resolve(EffectNodeData node, EffectContext context, List<UnitId> results);
    }

    /// <summary>
    ///     Default selection: <c>Target</c> ⇒ the context's primary targets; <c>Caster</c> ⇒ the caster; area selectors
    ///     ⇒ alive units within the (leveled) radius around the first primary target / target point or the caster;
    ///     <c>ConeFromCaster</c> ⇒ the radius around the caster narrowed to <see cref="EffectNodeData.Angle" /> around the
    ///     aim (target point, else first primary target). Areas and cones use the node team filter. With
    ///     <see cref="EffectNodeData.MaxTargets" /> &gt; 0 the nearest N are kept (distance, then unit id); unlimited
    ///     areas keep the world adapter's query order.
    /// </summary>
    public class StandardEffectTargetResolver : IEffectTargetResolver
    {
        private static readonly Comparison<AreaHit> AreaHitComparison = CompareAreaHits;

        private readonly List<UnitId> _unitScratch = new List<UnitId>(16);
        private readonly List<AreaHit> _areaScratch = new List<AreaHit>(16);

        public virtual void Resolve(EffectNodeData node, EffectContext context, List<UnitId> results)
        {
            IAbilityWorldAdapter world = context.System.World;
            switch (node.Target)
            {
                case EffectTargetSelector.Caster:
                    results.Add(context.Caster);
                    break;

                case EffectTargetSelector.Target:
                    results.AddRange(context.PrimaryTargets);
                    break;

                case EffectTargetSelector.AreaAroundTarget:
                {
                    Vector3 center;
                    if (context.PrimaryTargets.Count > 0 &&
                        world.TryGetPosition(context.PrimaryTargets[0], out Vector3 targetPos))
                    {
                        center = targetPos;
                    }
                    else if (context.HasTargetPoint)
                    {
                        center = context.TargetPoint;
                    }
                    else
                    {
                        return;
                    }

                    CollectArea(center, node, context, results, false, default, 0f);
                    break;
                }

                case EffectTargetSelector.AreaAroundCaster:
                {
                    if (world.TryGetPosition(context.Caster, out Vector3 casterPos))
                    {
                        CollectArea(casterPos, node, context, results, false, default, 0f);
                    }

                    break;
                }

                case EffectTargetSelector.ConeFromCaster:
                {
                    if (!world.TryGetPosition(context.Caster, out Vector3 origin) ||
                        !TryGetAim(context, world, origin, out Vector3 aim))
                    {
                        return;
                    }

                    float angle = node.Angle;
                    if (!(angle > 0f))
                    {
                        return;
                    }

                    if (angle >= 360f)
                    {
                        CollectArea(origin, node, context, results, false, default, 0f);
                        return;
                    }

                    float cosHalf = MathF.Cos(angle * 0.5f * (MathF.PI / 180f));
                    CollectArea(origin, node, context, results, true, aim, cosHalf);
                    break;
                }
            }
        }

        private static bool TryGetAim(EffectContext context, IAbilityWorldAdapter world, Vector3 origin, out Vector3 aim)
        {
            Vector3 toward;
            if (context.HasTargetPoint)
            {
                toward = context.TargetPoint - origin;
            }
            else if (context.PrimaryTargets.Count > 0 && context.PrimaryTargets[0] != context.Caster &&
                     world.TryGetPosition(context.PrimaryTargets[0], out Vector3 targetPos))
            {
                toward = targetPos - origin;
            }
            else
            {
                aim = default;
                return false;
            }

            float length = toward.magnitude;
            if (!(length > 1e-5f) || float.IsInfinity(length))
            {
                aim = default;
                return false;
            }

            aim = toward / length;
            return true;
        }

        private void CollectArea(Vector3 center, EffectNodeData node, EffectContext context, List<UnitId> results,
            bool cone, Vector3 aim, float cosHalf)
        {
            AbilitySystem system = context.System;
            float radius = LeveledValueResolver.SampleByLevel(node.RadiusByLevel, context.AbilityLevel, node.Radius);
            if (radius <= 0f)
            {
                return;
            }

            _unitScratch.Clear();
            system.World.QueryUnitsInRadius(center, radius, _unitScratch);
            AbilityUnit caster = system.GetUnit(context.Caster);
            int maxTargets = node.ResolveMaxTargets(context.AbilityLevel);

            // WHY: unlimited case preserves the historical query order and stays zero-allocation.
            if (maxTargets <= 0)
            {
                for (int i = 0; i < _unitScratch.Count; i++)
                {
                    AbilityUnit unit = system.GetUnit(_unitScratch[i]);
                    if (Admits(system, unit, caster, node, cone, center, aim, cosHalf))
                    {
                        results.Add(unit.Id);
                    }
                }

                return;
            }

            // WHY: capped case keeps the nearest N, sorted deterministically by distance then unit id.
            _areaScratch.Clear();
            for (int i = 0; i < _unitScratch.Count; i++)
            {
                UnitId id = _unitScratch[i];
                AbilityUnit unit = system.GetUnit(id);
                if (!Admits(system, unit, caster, node, cone, center, aim, cosHalf))
                {
                    continue;
                }

                // WHY: double ranking; a float square overflows to infinity for far units the exact radius
                // query admitted, and they would then tie and fall back to unit-id order.
                double distSq = system.World.TryGetPosition(id, out Vector3 pos)
                    ? AbilitySpatialGrid.SqrDistance(pos, center)
                    : double.PositiveInfinity;
                _areaScratch.Add(new AreaHit(id, distSq));
            }

            _areaScratch.Sort(AreaHitComparison);
            int keep = maxTargets < _areaScratch.Count ? maxTargets : _areaScratch.Count;
            for (int i = 0; i < keep; i++)
            {
                results.Add(_areaScratch[i].Unit);
            }
        }

        private static bool Admits(AbilitySystem system, AbilityUnit unit, AbilityUnit caster, EffectNodeData node,
            bool cone, Vector3 origin, Vector3 aim, float cosHalf)
        {
            if (unit == null || !unit.IsAlive)
            {
                return false;
            }

            if (caster != null && !AbilitySystem.MatchesTeamFilter(caster, unit, node.TeamFilter))
            {
                return false;
            }

            if (!cone)
            {
                return true;
            }

            if (!system.World.TryGetPosition(unit.Id, out Vector3 position))
            {
                return false;
            }

            Vector3 offset = position - origin;
            float distance = offset.magnitude;
            if (distance <= 1e-5f)
            {
                // WHY: a unit standing on the cone's apex is inside every direction of it.
                return true;
            }

            return Vector3.Dot(offset / distance, aim) >= cosHalf - 1e-6f;
        }

        private static int CompareAreaHits(AreaHit a, AreaHit b)
        {
            int byDistance = a.DistanceSq.CompareTo(b.DistanceSq);
            return byDistance != 0 ? byDistance : a.Unit.Value.CompareTo(b.Unit.Value);
        }

        private readonly struct AreaHit
        {
            public readonly UnitId Unit;
            public readonly double DistanceSq;

            public AreaHit(UnitId unit, double distanceSq)
            {
                Unit = unit;
                DistanceSq = distanceSq;
            }
        }
    }
}
