using System.Collections.Generic;
using UnityEngine;

namespace Neo.Abilities
{
    public sealed partial class AbilitySystem
    {
        private const string ZoneSpawnedReason = "spawned";
        private const string ZonePulseReason = "pulse";
        private const string ZoneExpiredReason = "expired";
        private const int MaxZonePulsesPerTick = 8;

        private readonly List<AbilityZone> _zones = new List<AbilityZone>(8);
        private readonly List<AbilityZone> _zonePool = new List<AbilityZone>(8);
        private readonly List<List<UnitId>> _zoneQueryPool = new List<List<UnitId>>(2);
        private int _zoneQueryDepth;
        private int _nextZoneId = 1;
        private int _zoneTickDepth;

        /// <summary>Number of active zones.</summary>
        public int ZoneCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _zones.Count; i++)
                {
                    if (_zones[i].IsActive)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>Clears <paramref name="results" /> and fills it with the active zones in spawn order.</summary>
        public void GetZones(List<AbilityZone> results)
        {
            results.Clear();
            for (int i = 0; i < _zones.Count; i++)
            {
                if (_zones[i].IsActive)
                {
                    results.Add(_zones[i]);
                }
            }
        }

        /// <summary>The active zone with this id.</summary>
        public bool TryGetZone(int zoneId, out AbilityZone zone)
        {
            for (int i = 0; i < _zones.Count; i++)
            {
                if (_zones[i].IsActive && _zones[i].Id == zoneId)
                {
                    zone = _zones[i];
                    return true;
                }
            }

            zone = null;
            return false;
        }

        /// <summary>
        ///     Spawns a zone and runs its first pulse immediately. Returns the zone id, or 0 when the radius is not
        ///     positive. Publishes zone_spawned and asks the world adapter for the visual archetype, when set.
        /// </summary>
        public int SpawnZone(in AbilityZoneSpec spec)
        {
            if (!(spec.Radius > 0f) || float.IsInfinity(spec.Radius))
            {
                return 0;
            }

            AbilityZone zone;
            int last = _zonePool.Count - 1;
            if (last >= 0)
            {
                zone = _zonePool[last];
                _zonePool.RemoveAt(last);
            }
            else
            {
                zone = new AbilityZone();
            }

            int id = _nextZoneId++;
            AbilityUnit caster = GetUnit(spec.Caster);
            zone.Id = id;
            zone.IsActive = true;
            zone.Caster = spec.Caster;
            zone.CasterTeam = caster != null ? caster.Team : TeamId.Neutral;
            zone.Anchor = spec.Anchor;
            zone.FollowAnchor = spec.FollowAnchor && spec.Anchor.IsValid;
            zone.Center = spec.Center;
            if (zone.FollowAnchor && _world.TryGetPosition(spec.Anchor, out Vector3 anchorPosition))
            {
                zone.Center = anchorPosition;
            }

            zone.Radius = spec.Radius;
            zone.Duration = spec.Duration > 0f ? spec.Duration : 0f;
            zone.Remaining = zone.Duration;
            zone.Interval = spec.Interval > 0f ? spec.Interval : 0f;
            zone.PayloadAbilityId = spec.PayloadAbilityId;
            zone.ModifierId = spec.ModifierId;
            zone.TeamFilter = spec.TeamFilter;
            zone.AbilityLevel = spec.AbilityLevel < 1 ? 1 : spec.AbilityLevel;
            zone.SourceAbilityId = spec.SourceAbilityId ?? spec.PayloadAbilityId;
            zone.ArchetypeId = spec.ArchetypeId;
            zone.Accumulator = 0f;
            zone.PulseCount = 0;
            zone.Seed = spec.Seed != 0 ? spec.Seed : MixSeed((uint)id, 0x5A0E5u);
            _zones.Add(zone);

            TraceNote(AbilityTraceKind.Zone, zone.Caster, zone.Anchor, zone.Radius, zone.ArchetypeId,
                zone.PayloadAbilityId, ZoneSpawnedReason, id);
            Events.Publish(new AbilityEventArgs(AbilityEvents.ZoneSpawned, zone.Caster, zone.Anchor, zone.Radius,
                zone.SourceAbilityId, zone.ModifierId, castId: (uint)id, tag: zone.ArchetypeId));
            if (!string.IsNullOrEmpty(zone.ArchetypeId))
            {
                _world.RequestSpawn(new SpawnRequest(zone.ArchetypeId, zone.Caster, zone.Center, Vector3.zero,
                    zone.FollowAnchor ? zone.Anchor : UnitId.None, zone.SourceAbilityId, zone.Radius, 0u, id,
                    zone.Duration));
            }

            if (zone.IsActive && zone.Id == id)
            {
                PulseZone(zone);
            }

            return id;
        }

        /// <summary>Ends a zone (publishes zone_expired). Returns false when no active zone has this id.</summary>
        public bool RemoveZone(int zoneId)
        {
            if (!TryGetZone(zoneId, out AbilityZone zone))
            {
                return false;
            }

            ExpireZone(zone);
            CompactZones();
            return true;
        }

        /// <summary>Ends every zone owned by <paramref name="caster" />. Returns how many ended.</summary>
        public int RemoveZonesOf(UnitId caster)
        {
            int removed = 0;
            for (int i = 0; i < _zones.Count; i++)
            {
                AbilityZone zone = _zones[i];
                if (zone.IsActive && zone.Caster == caster)
                {
                    ExpireZone(zone);
                    removed++;
                }
            }

            CompactZones();
            return removed;
        }

        private void TickZones(float deltaTime)
        {
            if (_zones.Count == 0)
            {
                return;
            }

            _zoneTickDepth++;
            try
            {
                // WHY: zones spawned by a pulse start next tick; the count is fixed before iterating.
                int count = _zones.Count;
                for (int i = 0; i < count; i++)
                {
                    AbilityZone zone = _zones[i];
                    if (!zone.IsActive)
                    {
                        continue;
                    }

                    if (zone.Anchor.IsValid)
                    {
                        AbilityUnit anchor = GetUnit(zone.Anchor);
                        if (anchor == null || !anchor.IsAlive)
                        {
                            ExpireZone(zone);
                            continue;
                        }

                        if (zone.FollowAnchor && _world.TryGetPosition(zone.Anchor, out Vector3 position))
                        {
                            zone.Center = position;
                        }
                    }

                    if (zone.Interval > 0f)
                    {
                        // WHY: only time while the zone is alive can produce pulses (same rule as modifier ticks).
                        float activeTime = zone.IsPermanent
                            ? deltaTime
                            : Mathf.Min(deltaTime, Mathf.Max(0f, zone.Remaining));
                        zone.Accumulator += activeTime;
                        int id = zone.Id;
                        int safety = MaxZonePulsesPerTick;
                        while (zone.IsActive && zone.Id == id && zone.Accumulator >= zone.Interval && safety-- > 0)
                        {
                            zone.Accumulator -= zone.Interval;
                            PulseZone(zone);
                        }
                    }

                    if (zone.IsActive && !zone.IsPermanent)
                    {
                        zone.Remaining -= deltaTime;
                        if (zone.Remaining <= 0f)
                        {
                            ExpireZone(zone);
                        }
                    }
                }
            }
            finally
            {
                _zoneTickDepth--;
            }

            CompactZones();
        }

        private void PulseZone(AbilityZone zone)
        {
            if (_executionDepth >= EffectContext.MaxDepth)
            {
                return;
            }

            while (_zoneQueryPool.Count <= _zoneQueryDepth)
            {
                _zoneQueryPool.Add(new List<UnitId>(16));
            }

            List<UnitId> inside = _zoneQueryPool[_zoneQueryDepth];
            _zoneQueryDepth++;
            int pulse = zone.PulseCount++;
            uint seed = MixSeed(zone.Seed, (uint)pulse);
            EffectContext context = RentContext(zone.Caster, zone.SourceAbilityId, seed);
            long previousCause = _traceCause;
            try
            {
                inside.Clear();
                _world.QueryUnitsInRadius(zone.Center, zone.Radius, inside);
                for (int i = 0; i < inside.Count; i++)
                {
                    AbilityUnit unit = GetUnit(inside[i]);
                    if (unit != null && unit.IsAlive && ZoneAdmits(zone, unit))
                    {
                        context.PrimaryTargets.Add(unit.Id);
                    }
                }

                if (Trace.Enabled)
                {
                    _traceCause = Trace.Record(previousCause, AbilityTraceKind.Zone, _time, TickCount, zone.Caster,
                        zone.Anchor, zone.ArchetypeId, zone.PayloadAbilityId, zone.Id, context.PrimaryTargets.Count,
                        ZonePulseReason);
                }

                context.TargetPoint = zone.Center;
                context.HasTargetPoint = true;
                context.AbilityLevel = zone.AbilityLevel;
                context.Origin = EffectOrigin.Zone;
                if (!string.IsNullOrEmpty(zone.ModifierId) && TryGetModifier(zone.ModifierId, out ModifierBlueprint modifier))
                {
                    for (int i = 0; i < context.PrimaryTargets.Count; i++)
                    {
                        AbilityUnit unit = GetUnit(context.PrimaryTargets[i]);
                        if (unit != null && unit.IsAlive)
                        {
                            Modifiers.Apply(modifier, zone.Caster, unit.Id, zone.SourceAbilityId, zone.AbilityLevel);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(zone.PayloadAbilityId) &&
                    TryGetAbility(zone.PayloadAbilityId, out AbilityBlueprint payload) && payload.ImpactEffects.Count > 0)
                {
                    context.Specials = payload.Specials;
                    ExecuteEffects(payload.ImpactEffects, context);
                }
            }
            finally
            {
                ReturnContext(context);
                inside.Clear();
                _zoneQueryDepth--;
                _traceCause = previousCause;
            }
        }

        private static bool ZoneAdmits(AbilityZone zone, AbilityUnit unit)
        {
            switch (zone.TeamFilter)
            {
                case AbilityTeamFilter.Allies:
                    return unit.Id == zone.Caster || zone.CasterTeam.IsAllyOf(unit.Team);
                case AbilityTeamFilter.Enemies:
                    return unit.Id != zone.Caster && zone.CasterTeam.IsEnemyOf(unit.Team);
                default:
                    return true;
            }
        }

        private void ExpireZone(AbilityZone zone)
        {
            if (!zone.IsActive)
            {
                return;
            }

            zone.IsActive = false;
            TraceNote(AbilityTraceKind.Zone, zone.Caster, zone.Anchor, 0f, zone.ArchetypeId, zone.PayloadAbilityId,
                ZoneExpiredReason, zone.Id);
            Events.Publish(new AbilityEventArgs(AbilityEvents.ZoneExpired, zone.Caster, zone.Anchor, 0f,
                zone.SourceAbilityId, zone.ModifierId, castId: (uint)zone.Id, tag: zone.ArchetypeId));
        }

        // WHY: removal keeps spawn order (pulse order is part of determinism); only run outside a zone tick.
        private void CompactZones()
        {
            if (_zoneTickDepth > 0)
            {
                return;
            }

            int write = 0;
            for (int read = 0; read < _zones.Count; read++)
            {
                AbilityZone zone = _zones[read];
                if (zone.IsActive)
                {
                    _zones[write++] = zone;
                }
                else
                {
                    zone.Clear();
                    _zonePool.Add(zone);
                }
            }

            if (write < _zones.Count)
            {
                _zones.RemoveRange(write, _zones.Count - write);
            }
        }

        /// <summary>
        ///     Launches a projectile carrying <paramref name="payload" />'s impact effects (projectile op). The host
        ///     reports hits with <see cref="NotifyProjectileHit" /> using the returned cast id.
        /// </summary>
        internal uint LaunchProjectile(AbilityBlueprint payload, UnitId caster, Vector3 origin, Vector3 direction,
            UnitId target, string archetypeId, float speed, int level, uint seed)
        {
            uint castId = _nextCastId++;
            _pendingProjectiles[castId] = RentPending(payload, caster, seed, level, _time + PendingProjectileTimeout);
            _world.RequestSpawn(new SpawnRequest(archetypeId, caster, origin, direction, target, payload.Id, speed,
                castId));
            return castId;
        }

        /// <summary>
        ///     Runs <paramref name="payload" />'s impact effects in a fresh pooled context one level below
        ///     <paramref name="parent" /> (area_apply): same caster, cast, point, level and origin; the payload's specials.
        /// </summary>
        internal void ExecutePayload(AbilityBlueprint payload, EffectContext parent, List<UnitId> targets)
        {
            if (payload == null || payload.ImpactEffects.Count == 0 || parent.Depth + 1 >= EffectContext.MaxDepth)
            {
                return;
            }

            uint seed = (uint)parent.Random.NextInt(int.MaxValue) | 1u;
            EffectContext context = RentContext(parent.Caster, payload.Id, seed);
            try
            {
                context.Depth = parent.Depth + 1;
                context.CastId = parent.CastId;
                context.AbilityLevel = parent.AbilityLevel;
                context.Specials = payload.Specials;
                context.Origin = parent.Origin;
                context.SourceStacks = parent.SourceStacks;
                context.TargetPoint = parent.TargetPoint;
                context.HasTargetPoint = parent.HasTargetPoint;
                for (int i = 0; i < targets.Count; i++)
                {
                    context.PrimaryTargets.Add(targets[i]);
                }

                ExecuteEffects(payload.ImpactEffects, context);
            }
            finally
            {
                ReturnContext(context);
            }
        }
    }
}
