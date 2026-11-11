using System;
using System.Collections.Generic;

namespace Neo.Abilities.Fixed
{
    /// <summary>damage: resolved amount × (1 + spell_power%) through the damage pipeline.</summary>
    public sealed class FixedDamageOperation : IFixedEffectOperation
    {
        public string Id => FixedOps.Damage;

        public void Execute(FixedEffectContext context, FixedEffectNode node, List<int> targets)
        {
            if (targets.Count == 0)
            {
                return;
            }

            FixedAbilitySystem system = context.System;
            long hundred = 100L * system.Rules.Format.One;
            FixedUnit caster = system.GetUnit(context.Caster);
            int spellPower = caster != null ? caster.GetProperty(FixedProperties.SpellPower) : 0;
            for (int i = 0; i < targets.Count; i++)
            {
                int amount = FixedValueResolver.ResolveAmount(node, context, targets[i]);
                if (spellPower != 0)
                {
                    amount = FixedMath.MulDivToInt(amount, hundred + spellPower, hundred);
                }

                if (amount > 0)
                {
                    system.Damage.Apply(context.Caster, targets[i], amount, node.DamageType, context.AbilityId,
                        context.CastId, context.Random, context.HitFlags);
                }
            }
        }
    }

    /// <summary>heal: resolved amount × healing_received_mul of the target.</summary>
    public sealed class FixedHealOperation : IFixedEffectOperation
    {
        public string Id => FixedOps.Heal;

        public void Execute(FixedEffectContext context, FixedEffectNode node, List<int> targets)
        {
            FixedAbilitySystem system = context.System;
            int one = system.Rules.Format.One;
            for (int i = 0; i < targets.Count; i++)
            {
                FixedUnit target = system.GetUnit(targets[i]);
                if (target == null || !target.IsAlive)
                {
                    continue;
                }

                int resolved = FixedValueResolver.ResolveAmount(node, context, target.Id);
                if (resolved <= 0)
                {
                    continue;
                }

                int multiplier = FixedMath.Max(0, target.GetProperty(FixedProperties.HealingReceivedMul, one));
                int amount = FixedMath.MulDivToInt(resolved, multiplier, one);
                int healed = amount > 0 ? target.Increase(FixedResources.Health, amount) : 0;
                if (healed > 0)
                {
                    system.Events.Publish(new FixedEventArgs(FixedEvents.HealReceived, target.Id, context.Caster, healed,
                        context.AbilityId, castId: context.CastId));
                }
            }
        }
    }

    /// <summary>apply_modifier: applies the catalog modifier to every living target.</summary>
    public sealed class FixedApplyModifierOperation : IFixedEffectOperation
    {
        public string Id => FixedOps.ApplyModifier;

        public void Execute(FixedEffectContext context, FixedEffectNode node, List<int> targets)
        {
            FixedAbilitySystem system = context.System;
            if (!system.TryGetModifier(node.ModifierId, out FixedModifierBlueprint blueprint))
            {
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                FixedUnit target = system.GetUnit(targets[i]);
                if (target != null && target.IsAlive)
                {
                    system.Modifiers.Apply(blueprint, context.Caster, target.Id, context.AbilityId, context.AbilityLevel,
                        context);
                }
            }
        }
    }

    /// <summary>remove_modifier: removes every instance of ModifierId from the targets.</summary>
    public sealed class FixedRemoveModifierOperation : IFixedEffectOperation
    {
        public string Id => FixedOps.RemoveModifier;

        public void Execute(FixedEffectContext context, FixedEffectNode node, List<int> targets)
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

    /// <summary>dispel: removes dispellable debuffs (CustomParam "buffs" removes buffs instead).</summary>
    public sealed class FixedDispelOperation : IFixedEffectOperation
    {
        private static readonly Predicate<FixedModifierInstance> DispellableDebuff =
            m => m.Blueprint.Dispellable && m.Blueprint.IsDebuff;

        private static readonly Predicate<FixedModifierInstance> DispellableBuff =
            m => m.Blueprint.Dispellable && !m.Blueprint.IsDebuff;

        public string Id => FixedOps.Dispel;

        public void Execute(FixedEffectContext context, FixedEffectNode node, List<int> targets)
        {
            Predicate<FixedModifierInstance> predicate = FixedIdComparer.Same(node.CustomParam, "buffs")
                ? DispellableBuff
                : DispellableDebuff;
            for (int i = 0; i < targets.Count; i++)
            {
                context.System.Modifiers.RemoveWhere(targets[i], predicate);
            }
        }
    }

    /// <summary>resource_change: adds (positive) or removes (negative) the amount on ResourceId.</summary>
    public sealed class FixedResourceChangeOperation : IFixedEffectOperation
    {
        public string Id => FixedOps.ResourceChange;

        public void Execute(FixedEffectContext context, FixedEffectNode node, List<int> targets)
        {
            if (string.IsNullOrEmpty(node.ResourceId))
            {
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                FixedUnit target = context.System.GetUnit(targets[i]);
                if (target == null || !target.IsAlive)
                {
                    continue;
                }

                int amount = FixedValueResolver.ResolveAmount(node, context, target.Id);
                if (amount > 0)
                {
                    target.Increase(node.ResourceId, amount);
                }
                else if (amount < 0)
                {
                    target.Decrease(node.ResourceId, FixedMath.SaturatingNegate(amount));
                }
            }
        }
    }

    /// <summary>spawn: asks the world adapter for ArchetypeId at each target (or the target point).</summary>
    public sealed class FixedSpawnOperation : IFixedEffectOperation
    {
        public string Id => FixedOps.Spawn;

        public void Execute(FixedEffectContext context, FixedEffectNode node, List<int> targets)
        {
            if (string.IsNullOrEmpty(node.ArchetypeId))
            {
                return;
            }

            IFixedWorldAdapter world = context.System.World;
            if (targets.Count == 0)
            {
                if (context.HasTargetPoint)
                {
                    int magnitude = FixedValueResolver.ResolveAmount(node, context, 0);
                    world.RequestSpawn(new FixedSpawnRequest(node.ArchetypeId, context.Caster, context.TargetPoint,
                        FixedVec3.Zero, 0, context.AbilityId, magnitude));
                }

                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                if (!world.TryGetPosition(targets[i], out FixedVec3 position) && context.HasTargetPoint)
                {
                    position = context.TargetPoint;
                }

                int magnitude = FixedValueResolver.ResolveAmount(node, context, targets[i]);
                world.RequestSpawn(new FixedSpawnRequest(node.ArchetypeId, context.Caster, position, FixedVec3.Zero,
                    targets[i], context.AbilityId, magnitude));
            }
        }
    }

    /// <summary>
    ///     execute: damage from a raw fraction of the target's missing / max / current health (CustomParam), else the
    ///     flat amount.
    /// </summary>
    public sealed class FixedExecuteOperation : IFixedEffectOperation
    {
        public string Id => FixedOps.Execute;

        public void Execute(FixedEffectContext context, FixedEffectNode node, List<int> targets)
        {
            FixedAbilitySystem system = context.System;
            int one = system.Rules.Format.One;
            for (int i = 0; i < targets.Count; i++)
            {
                FixedUnit target = system.GetUnit(targets[i]);
                if (target == null || !target.IsAlive)
                {
                    continue;
                }

                int value = FixedValueResolver.ResolveAmount(node, context, target.Id);
                int damage;
                if (FixedIdComparer.Same(node.CustomParam, "missing"))
                {
                    damage = FixedMath.MulDivToInt(value, (long)target.MaxHealthRaw - target.HealthRaw, one);
                }
                else if (FixedIdComparer.Same(node.CustomParam, "max"))
                {
                    damage = FixedMath.MulDivToInt(value, target.MaxHealthRaw, one);
                }
                else if (FixedIdComparer.Same(node.CustomParam, "current"))
                {
                    damage = FixedMath.MulDivToInt(value, target.HealthRaw, one);
                }
                else
                {
                    damage = value;
                }

                if (damage > 0)
                {
                    system.Damage.Apply(context.Caster, target.Id, damage, node.DamageType, context.AbilityId,
                        context.CastId, context.Random, context.HitFlags);
                }
            }
        }
    }

    /// <summary>
    ///     chain: damages the first target, then hops to the nearest unvisited admitted unit within Radius, up to
    ///     MaxTargets hits (default 4); CustomParam is the per-hop falloff ("0.5").
    /// </summary>
    public sealed class FixedChainOperation : IFixedEffectOperation
    {
        private const int DefaultMaxHits = 4;
        private readonly List<int> _queryScratch = new List<int>(16);
        private readonly List<List<int>> _visitedPool = new List<List<int>>(2);
        private int _depth;

        public string Id => FixedOps.Chain;

        public void Execute(FixedEffectContext context, FixedEffectNode node, List<int> targets)
        {
            int current = targets.Count > 0 ? targets[0] : context.PrimaryTargets.Count > 0 ? context.PrimaryTargets[0] : 0;
            if (current == 0)
            {
                return;
            }

            FixedAbilitySystem system = context.System;
            FixedFormat format = system.Rules.Format;
            int maxTargets = node.ResolveMaxTargets(context.AbilityLevel);
            int maxHits = maxTargets > 0 ? maxTargets : DefaultMaxHits;
            int falloff = format.TryParse(node.CustomParam, out int parsed) ? parsed : format.One;
            int radius = node.ResolveRadiusRaw(context.AbilityLevel);
            FixedUnit caster = system.GetUnit(context.Caster);
            while (_visitedPool.Count <= _depth)
            {
                _visitedPool.Add(new List<int>(8));
            }

            List<int> visited = _visitedPool[_depth];
            _depth++;
            try
            {
                visited.Clear();
                int multiplier = format.One;
                for (int hit = 0; hit < maxHits && current != 0; hit++)
                {
                    int amount = FixedMath.MulDivToInt(FixedValueResolver.ResolveAmount(node, context, current),
                        multiplier, format.One);
                    if (amount > 0)
                    {
                        system.Damage.Apply(context.Caster, current, amount, node.DamageType, context.AbilityId,
                            context.CastId, context.Random, context.HitFlags);
                    }

                    visited.Add(current);
                    if (hit + 1 >= maxHits || !system.World.TryGetPosition(current, out FixedVec3 from))
                    {
                        break;
                    }

                    current = FindNearest(system, from, radius, visited, caster, node.TeamFilter);
                    multiplier = format.Mul(multiplier, falloff);
                }
            }
            finally
            {
                visited.Clear();
                _depth--;
            }
        }

        private int FindNearest(FixedAbilitySystem system, FixedVec3 from, int radius, List<int> visited, FixedUnit caster,
            FixedTeamFilter filter)
        {
            if (radius <= 0)
            {
                return 0;
            }

            _queryScratch.Clear();
            system.World.QueryUnitsInRadius(from, radius, _queryScratch);
            int best = 0;
            ulong bestDistance = ulong.MaxValue;
            bool found = false;
            for (int i = 0; i < _queryScratch.Count; i++)
            {
                int id = _queryScratch[i];
                FixedUnit unit = system.GetUnit(id);
                if (visited.Contains(id) || unit == null || !unit.IsAlive ||
                    caster != null && !FixedAbilitySystem.MatchesTeamFilter(caster, unit, filter) ||
                    !system.World.TryGetPosition(id, out FixedVec3 position))
                {
                    continue;
                }

                ulong distance = FixedVec3.SquaredDistance(position, from);
                if (!found || distance < bestDistance || distance == bestDistance && id < best)
                {
                    found = true;
                    best = id;
                    bestDistance = distance;
                }
            }

            _queryScratch.Clear();
            return best;
        }
    }

    /// <summary>
    ///     modify_damage (hook reactions only): CustomParam multiply (default), add, absorb or cancel the damage in
    ///     flight by the resolved raw amount.
    /// </summary>
    public sealed class FixedModifyDamageOperation : IFixedEffectOperation
    {
        public string Id => FixedOps.ModifyDamage;

        public void Execute(FixedEffectContext context, FixedEffectNode node, List<int> targets)
        {
            FixedDamageContext damage = context.Damage;
            if (damage == null || !damage.CanModify)
            {
                return;
            }

            string mode = node.CustomParam;
            if (FixedIdComparer.Same(mode, FixedOps.ModeCancel))
            {
                damage.Cancel();
                return;
            }

            int amount = FixedValueResolver.ResolveAmount(node, context, damage.Target);
            if (FixedIdComparer.Same(mode, FixedOps.ModeAdd))
            {
                damage.FlatAddRaw = FixedMath.SaturatingAdd(damage.FlatAddRaw, amount);
            }
            else if (FixedIdComparer.Same(mode, FixedOps.ModeAbsorb))
            {
                damage.AbsorbRaw = FixedMath.SaturatingAdd(damage.AbsorbRaw, FixedMath.Max(0, amount));
            }
            else
            {
                damage.MultiplierRaw = context.System.Rules.Format.Mul(damage.MultiplierRaw, FixedMath.Max(0, amount));
            }
        }
    }

    /// <summary>reflect (hook reactions only): adds a raw fraction of the mitigated damage to send back.</summary>
    public sealed class FixedReflectOperation : IFixedEffectOperation
    {
        public string Id => FixedOps.Reflect;

        public void Execute(FixedEffectContext context, FixedEffectNode node, List<int> targets)
        {
            FixedDamageContext damage = context.Damage;
            if (damage == null || !damage.CanModify || damage.HasFlag(FixedDamageFlags.Reflected))
            {
                return;
            }

            int fraction = FixedValueResolver.ResolveAmount(node, context, damage.Target);
            if (fraction <= 0)
            {
                return;
            }

            damage.ReflectFractionRaw = FixedMath.SaturatingAdd(damage.ReflectFractionRaw, fraction);
            if (!string.IsNullOrEmpty(node.DamageType))
            {
                damage.ReflectDamageType = node.DamageType;
            }
        }
    }

    /// <summary>
    ///     projectile: launches the payload ability (AbilityId) from the caster at each target (or the target
    ///     point); its impact effects run when the host reports a hit. The resolved amount overrides the speed.
    /// </summary>
    public sealed class FixedProjectileOperation : IFixedEffectOperation
    {
        public string Id => FixedOps.Projectile;

        public void Execute(FixedEffectContext context, FixedEffectNode node, List<int> targets)
        {
            FixedAbilitySystem system = context.System;
            if (!system.TryGetAbility(node.AbilityId, out FixedAbilityBlueprint payload) ||
                !system.World.TryGetPosition(context.Caster, out FixedVec3 origin))
            {
                return;
            }

            string archetype = !string.IsNullOrEmpty(node.ArchetypeId) ? node.ArchetypeId : payload.ProjectileArchetypeId;
            int resolved = FixedValueResolver.ResolveAmount(node, context, 0);
            int speed = resolved > 0 ? resolved : payload.ResolveProjectileSpeedRaw(context.AbilityLevel);
            if (targets.Count == 0)
            {
                if (context.HasTargetPoint)
                {
                    system.LaunchProjectile(payload, context.Caster, origin, context.TargetPoint - origin, 0, archetype,
                        speed, context.AbilityLevel, NextSeed(context), context);
                }

                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                int target = targets[i];
                if (target == context.Caster)
                {
                    continue;
                }

                FixedVec3 direction = system.World.TryGetPosition(target, out FixedVec3 targetPosition)
                    ? targetPosition - origin
                    : context.HasTargetPoint
                        ? context.TargetPoint - origin
                        : new FixedVec3(0, 0, system.Rules.Format.One);
                system.LaunchProjectile(payload, context.Caster, origin, direction, target, archetype, speed,
                    context.AbilityLevel, NextSeed(context), context);
            }
        }

        internal static uint NextSeed(FixedEffectContext context)
        {
            return (uint)context.Random.NextInt(int.MaxValue) | 1u;
        }
    }

    /// <summary>area_apply: applies ModifierId to every target and runs the payload ability's impact on them.</summary>
    public sealed class FixedAreaApplyOperation : IFixedEffectOperation
    {
        public string Id => FixedOps.AreaApply;

        public void Execute(FixedEffectContext context, FixedEffectNode node, List<int> targets)
        {
            if (targets.Count == 0)
            {
                return;
            }

            FixedAbilitySystem system = context.System;
            if (system.TryGetModifier(node.ModifierId, out FixedModifierBlueprint modifier))
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    FixedUnit unit = system.GetUnit(targets[i]);
                    if (unit != null && unit.IsAlive)
                    {
                        system.Modifiers.Apply(modifier, context.Caster, unit.Id, context.AbilityId, context.AbilityLevel,
                            context);
                    }
                }
            }

            if (system.TryGetAbility(node.AbilityId, out FixedAbilityBlueprint payload))
            {
                system.ExecutePayload(payload, context, targets);
            }
        }
    }

    /// <summary>
    ///     zone: spawns a domain zone. Selector Caster ⇒ an aura following the caster (CustomParam "fixed" leaves it
    ///     in place); other selectors ⇒ a formation at the target point or the first target ("follow" follows it).
    /// </summary>
    public sealed class FixedZoneOperation : IFixedEffectOperation
    {
        public string Id => FixedOps.Zone;

        public void Execute(FixedEffectContext context, FixedEffectNode node, List<int> targets)
        {
            FixedAbilitySystem system = context.System;
            int radius = node.ResolveRadiusRaw(context.AbilityLevel);
            if (radius <= 0)
            {
                return;
            }

            FixedZoneSpec spec = new FixedZoneSpec
            {
                Caster = context.Caster,
                RadiusRaw = radius,
                DurationTicks = node.ResolveDurationTicks(context.AbilityLevel),
                IntervalTicks = node.ResolveIntervalTicks(context.AbilityLevel),
                PayloadAbilityId = node.AbilityId,
                ModifierId = node.ModifierId,
                TeamFilter = node.TeamFilter,
                AbilityLevel = context.AbilityLevel,
                SourceAbilityId = context.AbilityId,
                ArchetypeId = node.ArchetypeId,
                Seed = FixedProjectileOperation.NextSeed(context)
            };

            if (node.Target == FixedTargetSelector.Caster)
            {
                if (!system.World.TryGetPosition(context.Caster, out FixedVec3 casterPosition))
                {
                    return;
                }

                spec.Center = casterPosition;
                spec.Anchor = context.Caster;
                spec.FollowAnchor = !FixedIdComparer.Same(node.CustomParam, "fixed");
            }
            else
            {
                int first = targets.Count > 0 ? targets[0] : 0;
                if (first != 0 && FixedIdComparer.Same(node.CustomParam, "follow") &&
                    system.World.TryGetPosition(first, out FixedVec3 anchorPosition))
                {
                    spec.Center = anchorPosition;
                    spec.Anchor = first;
                    spec.FollowAnchor = true;
                }
                else if (context.HasTargetPoint)
                {
                    spec.Center = context.TargetPoint;
                }
                else if (first != 0 && system.World.TryGetPosition(first, out FixedVec3 targetPosition))
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
    ///     cast_ability: casts a granted ability (AbilityId) of the caster through the normal pipeline: unit abilities
    ///     at each target, point / direction abilities toward each target, no-target abilities once.
    /// </summary>
    public sealed class FixedCastAbilityOperation : IFixedEffectOperation
    {
        public string Id => FixedOps.CastAbility;

        public void Execute(FixedEffectContext context, FixedEffectNode node, List<int> targets)
        {
            FixedAbilitySystem system = context.System;
            if (!system.TryGetSlot(context.Caster, node.AbilityId, out FixedAbilitySlot slot))
            {
                return;
            }

            FixedAbilityBlueprint ability = slot.Blueprint;
            switch (ability.Targeting)
            {
                case FixedTargeting.Unit:
                    for (int i = 0; i < targets.Count; i++)
                    {
                        FixedCastRequest request = FixedCastRequest.AtUnit(context.Caster, ability.Id, targets[i],
                            FixedProjectileOperation.NextSeed(context));
                        if (system.Cast(in request).Success && !slot.IsReady)
                        {
                            return;
                        }
                    }

                    return;
                case FixedTargeting.Point:
                case FixedTargeting.Direction:
                {
                    system.World.TryGetPosition(context.Caster, out FixedVec3 origin);
                    if (targets.Count == 0 && context.HasTargetPoint)
                    {
                        CastAt(system, context, ability, origin, context.TargetPoint);
                        return;
                    }

                    for (int i = 0; i < targets.Count; i++)
                    {
                        if (targets[i] != context.Caster && system.World.TryGetPosition(targets[i], out FixedVec3 point) &&
                            CastAt(system, context, ability, origin, point) && !slot.IsReady)
                        {
                            return;
                        }
                    }

                    return;
                }
                default:
                {
                    FixedCastRequest request =
                        FixedCastRequest.NoTarget(context.Caster, ability.Id, FixedProjectileOperation.NextSeed(context));
                    system.Cast(in request);
                    return;
                }
            }
        }

        private static bool CastAt(FixedAbilitySystem system, FixedEffectContext context, FixedAbilityBlueprint ability,
            FixedVec3 origin, FixedVec3 point)
        {
            uint seed = FixedProjectileOperation.NextSeed(context);
            FixedCastRequest request = ability.Targeting == FixedTargeting.Point
                ? FixedCastRequest.AtPoint(context.Caster, ability.Id, point, seed)
                : FixedCastRequest.Towards(context.Caster, ability.Id, point - origin, seed);
            return system.Cast(in request).Success;
        }
    }

    /// <summary>Installs the built-in fixed operations.</summary>
    public static class FixedDefaultOps
    {
        public static void RegisterAll(FixedEffectOpRegistry registry)
        {
            if (registry == null)
            {
                return;
            }

            registry.Register(new FixedDamageOperation());
            registry.Register(new FixedHealOperation());
            registry.Register(new FixedApplyModifierOperation());
            registry.Register(new FixedRemoveModifierOperation());
            registry.Register(new FixedDispelOperation());
            registry.Register(new FixedResourceChangeOperation());
            registry.Register(new FixedSpawnOperation());
            registry.Register(new FixedExecuteOperation());
            registry.Register(new FixedChainOperation());
            registry.Register(new FixedModifyDamageOperation());
            registry.Register(new FixedReflectOperation());
            registry.Register(new FixedProjectileOperation());
            registry.Register(new FixedAreaApplyOperation());
            registry.Register(new FixedZoneOperation());
            registry.Register(new FixedCastAbilityOperation());
        }
    }
}
