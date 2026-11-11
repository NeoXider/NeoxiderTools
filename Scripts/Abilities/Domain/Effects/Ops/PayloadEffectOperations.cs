using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     Built-in "projectile" op: launches one projectile per resolved target (or one at the target point when there
    ///     is none) from the caster. It travels through the world adapter (<see cref="IAbilityWorldAdapter.RequestSpawn" />
    ///     with a cast id); when the host reports the hit (<see cref="AbilitySystem.NotifyProjectileHit" />) the impact
    ///     effects of the payload ability <see cref="EffectNodeData.AbilityId" /> run. Archetype:
    ///     <see cref="EffectNodeData.ArchetypeId" />, else the payload's projectile archetype; speed: a positive resolved
    ///     amount, else the payload's projectile speed. The payload needs no grant.
    /// </summary>
    public sealed class ProjectileEffectOperation : IEffectOperation
    {
        public string Id => AbilityEffectOps.Projectile;

        public void Execute(EffectContext context, EffectNodeData node, List<UnitId> targets)
        {
            AbilitySystem system = context.System;
            if (string.IsNullOrEmpty(node.AbilityId) || !system.TryGetAbility(node.AbilityId, out AbilityBlueprint payload))
            {
                return;
            }

            if (!system.World.TryGetPosition(context.Caster, out Vector3 origin))
            {
                return;
            }

            string archetype = !string.IsNullOrEmpty(node.ArchetypeId) ? node.ArchetypeId : payload.ProjectileArchetypeId;
            float resolved = LeveledValueResolver.ResolveAmount(node, context, UnitId.None);
            float speed = resolved > 0f && !float.IsInfinity(resolved) ? resolved : payload.ProjectileSpeed;
            if (targets.Count == 0)
            {
                if (context.HasTargetPoint)
                {
                    system.LaunchProjectile(payload, context.Caster, origin, context.TargetPoint - origin, UnitId.None,
                        archetype, speed, context.AbilityLevel, NextSeed(context));
                }

                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                UnitId target = targets[i];
                if (target == context.Caster)
                {
                    continue;
                }

                Vector3 direction = system.World.TryGetPosition(target, out Vector3 targetPosition)
                    ? targetPosition - origin
                    : context.HasTargetPoint
                        ? context.TargetPoint - origin
                        : Vector3.forward;
                system.LaunchProjectile(payload, context.Caster, origin, direction, target, archetype, speed,
                    context.AbilityLevel, NextSeed(context));
            }
        }

        private static uint NextSeed(EffectContext context)
        {
            return (uint)context.Random.NextInt(int.MaxValue) | 1u;
        }
    }

    /// <summary>
    ///     Built-in "area_apply" op: applies a bundle to every resolved target of the node's selector (use an area
    ///     selector, or ConeFromCaster for a cone): the catalog modifier <see cref="EffectNodeData.ModifierId" /> to
    ///     each target and, once, the impact effects of the payload ability <see cref="EffectNodeData.AbilityId" />
    ///     with all of them as primary targets. Either part may be empty.
    /// </summary>
    public sealed class AreaApplyEffectOperation : IEffectOperation
    {
        public string Id => AbilityEffectOps.AreaApply;

        public void Execute(EffectContext context, EffectNodeData node, List<UnitId> targets)
        {
            if (targets.Count == 0)
            {
                return;
            }

            AbilitySystem system = context.System;
            if (!string.IsNullOrEmpty(node.ModifierId) && system.TryGetModifier(node.ModifierId, out ModifierBlueprint modifier))
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    AbilityUnit unit = system.GetUnit(targets[i]);
                    if (unit != null && unit.IsAlive)
                    {
                        system.Modifiers.Apply(modifier, context.Caster, unit.Id, context.AbilityId, context.AbilityLevel);
                    }
                }
            }

            if (!string.IsNullOrEmpty(node.AbilityId) && system.TryGetAbility(node.AbilityId, out AbilityBlueprint payload))
            {
                system.ExecutePayload(payload, context, targets);
            }
        }
    }

    /// <summary>
    ///     Built-in "zone" op: spawns one zone (aura or formation) through <see cref="AbilitySystem.SpawnZone" />.
    ///     Selector <c>Caster</c> ⇒ an aura that follows the caster (<c>CustomParam = "fixed"</c> leaves it where the
    ///     caster stands); any other selector ⇒ a formation fixed at the target point, else at the first resolved
    ///     target (<c>CustomParam = "follow"</c> makes it follow that target). Radius (leveled), Duration, Interval,
    ///     TeamFilter, ModifierId (applied every pulse), AbilityId (payload impact effects every pulse) and ArchetypeId
    ///     (visual) come from the node.
    /// </summary>
    public sealed class ZoneEffectOperation : IEffectOperation
    {
        public string Id => AbilityEffectOps.Zone;

        public void Execute(EffectContext context, EffectNodeData node, List<UnitId> targets)
        {
            AbilitySystem system = context.System;
            float radius = LeveledValueResolver.SampleByLevel(node.RadiusByLevel, context.AbilityLevel, node.Radius);
            if (!(radius > 0f))
            {
                return;
            }

            AbilityZoneSpec spec = new AbilityZoneSpec
            {
                Caster = context.Caster,
                Radius = radius,
                Duration = node.Duration,
                Interval = node.Interval,
                PayloadAbilityId = node.AbilityId,
                ModifierId = node.ModifierId,
                TeamFilter = node.TeamFilter,
                AbilityLevel = context.AbilityLevel,
                SourceAbilityId = context.AbilityId,
                ArchetypeId = node.ArchetypeId,
                Seed = (uint)context.Random.NextInt(int.MaxValue) | 1u
            };

            if (node.Target == EffectTargetSelector.Caster)
            {
                if (!system.World.TryGetPosition(context.Caster, out Vector3 casterPosition))
                {
                    return;
                }

                spec.Center = casterPosition;
                spec.Anchor = context.Caster;
                spec.FollowAnchor = !string.Equals(node.CustomParam, "fixed", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                UnitId first = targets.Count > 0 ? targets[0] : UnitId.None;
                bool follow = first.IsValid &&
                              string.Equals(node.CustomParam, "follow", StringComparison.OrdinalIgnoreCase);
                if (follow && system.World.TryGetPosition(first, out Vector3 anchorPosition))
                {
                    spec.Center = anchorPosition;
                    spec.Anchor = first;
                    spec.FollowAnchor = true;
                }
                else if (context.HasTargetPoint)
                {
                    spec.Center = context.TargetPoint;
                }
                else if (first.IsValid && system.World.TryGetPosition(first, out Vector3 targetPosition))
                {
                    spec.Center = targetPosition;
                }
                else
                {
                    return;
                }
            }

            system.SpawnZone(in spec);
        }
    }

    /// <summary>
    ///     Built-in "cast_ability" op: the context caster casts the granted ability <see cref="EffectNodeData.AbilityId" />
    ///     through the normal cast pipeline (cooldown, charges, costs and range apply). Unit-targeted abilities are cast
    ///     at each resolved target, point and direction abilities towards each target's position (or the target point),
    ///     no-target and self abilities once. Use it for auto-cast techniques and "on hit, cast X". Nesting is capped by
    ///     <see cref="AbilitySystem.MaxCastDepth" />.
    /// </summary>
    public sealed class CastAbilityEffectOperation : IEffectOperation
    {
        public string Id => AbilityEffectOps.CastAbility;

        public void Execute(EffectContext context, EffectNodeData node, List<UnitId> targets)
        {
            AbilitySystem system = context.System;
            if (string.IsNullOrEmpty(node.AbilityId) ||
                !system.TryGetSlot(context.Caster, node.AbilityId, out AbilitySlot slot))
            {
                return;
            }

            AbilityBlueprint ability = slot.Blueprint;
            switch (ability.Targeting)
            {
                case TargetingMode.Unit:
                    for (int i = 0; i < targets.Count; i++)
                    {
                        if (system.Cast(CastRequest.AtUnit(context.Caster, ability.Id, targets[i], NextSeed(context)))
                            .Success && !slot.IsReady)
                        {
                            return;
                        }
                    }

                    return;

                case TargetingMode.Point:
                case TargetingMode.Direction:
                {
                    system.World.TryGetPosition(context.Caster, out Vector3 origin);
                    if (targets.Count == 0 && context.HasTargetPoint)
                    {
                        CastAt(system, context, ability, origin, context.TargetPoint);
                        return;
                    }

                    for (int i = 0; i < targets.Count; i++)
                    {
                        if (targets[i] != context.Caster &&
                            system.World.TryGetPosition(targets[i], out Vector3 point) &&
                            CastAt(system, context, ability, origin, point) && !slot.IsReady)
                        {
                            return;
                        }
                    }

                    return;
                }

                default:
                    system.Cast(CastRequest.NoTarget(context.Caster, ability.Id, NextSeed(context)));
                    return;
            }
        }

        private static bool CastAt(AbilitySystem system, EffectContext context, AbilityBlueprint ability, Vector3 origin,
            Vector3 point)
        {
            CastRequest request = ability.Targeting == TargetingMode.Point
                ? CastRequest.AtPoint(context.Caster, ability.Id, point, NextSeed(context))
                : CastRequest.Towards(context.Caster, ability.Id, point - origin, NextSeed(context));
            return system.Cast(request).Success;
        }

        private static uint NextSeed(EffectContext context)
        {
            return (uint)context.Random.NextInt(int.MaxValue) | 1u;
        }
    }
}
