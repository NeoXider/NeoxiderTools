using System.Collections.Generic;

namespace Neo.Abilities.Fixed
{
    /// <summary>Spawn parameters of a fixed zone (aura or formation); times in ticks, radius raw.</summary>
    public struct FixedZoneSpec
    {
        public int Caster;
        public int Anchor;
        public bool FollowAnchor;
        public FixedVec3 Center;
        public int RadiusRaw;

        /// <summary>Lifetime in ticks; 0 = until removed or its anchor dies.</summary>
        public int DurationTicks;

        /// <summary>Ticks between pulses after the spawn pulse; 0 = only the spawn pulse.</summary>
        public int IntervalTicks;

        public string PayloadAbilityId;
        public string ModifierId;
        public FixedTeamFilter TeamFilter;
        public int AbilityLevel;
        public string SourceAbilityId;
        public string ArchetypeId;
        public uint Seed;
    }

    /// <summary>A live zone of a fixed system (pooled: check <see cref="IsActive" /> and <see cref="Id" />).</summary>
    public sealed class FixedZone
    {
        internal int Accumulator;
        internal uint Seed;
        internal int CasterTeam;

        internal FixedZone()
        {
        }

        public int Id { get; internal set; }
        public bool IsActive { get; internal set; }
        public int Caster { get; internal set; }
        public int Anchor { get; internal set; }
        public bool FollowAnchor { get; internal set; }
        public FixedVec3 Center { get; internal set; }
        public int RadiusRaw { get; internal set; }
        public int DurationTicks { get; internal set; }
        public int RemainingTicks { get; internal set; }
        public int IntervalTicks { get; internal set; }
        public string PayloadAbilityId { get; internal set; }
        public string ModifierId { get; internal set; }
        public FixedTeamFilter TeamFilter { get; internal set; }
        public int AbilityLevel { get; internal set; }
        public string SourceAbilityId { get; internal set; }
        public string ArchetypeId { get; internal set; }
        public int PulseCount { get; internal set; }
        public bool IsPermanent => DurationTicks <= 0;

        internal void Clear()
        {
            IsActive = false;
            PayloadAbilityId = null;
            ModifierId = null;
            SourceAbilityId = null;
            ArchetypeId = null;
            Accumulator = 0;
            PulseCount = 0;
        }
    }

    public sealed partial class FixedAbilitySystem
    {
        private const int MaxZonePulsesPerStep = 8;
        private readonly List<FixedZone> _zones = new List<FixedZone>(8);
        private readonly List<FixedZone> _zonePool = new List<FixedZone>(8);
        private readonly List<List<int>> _zoneQueryPool = new List<List<int>>(2);
        private int _zoneQueryDepth;
        private int _zoneStepDepth;
        private int _nextZoneId = 1;

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

        public void GetZones(List<FixedZone> results)
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

        public bool TryGetZone(int zoneId, out FixedZone zone)
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
        ///     Spawns a zone that pulses immediately and then every interval: each pulse applies ModifierId to every
        ///     admitted unit inside and runs the payload ability's impact on them. Returns the zone id (0 = rejected).
        /// </summary>
        public int SpawnZone(in FixedZoneSpec spec)
        {
            if (spec.RadiusRaw <= 0)
            {
                return 0;
            }

            int last = _zonePool.Count - 1;
            FixedZone zone;
            if (last >= 0)
            {
                zone = _zonePool[last];
                _zonePool.RemoveAt(last);
            }
            else
            {
                zone = new FixedZone();
            }

            int id = _nextZoneId++;
            FixedUnit caster = GetUnit(spec.Caster);
            zone.Id = id;
            zone.IsActive = true;
            zone.Caster = spec.Caster;
            zone.CasterTeam = caster != null ? caster.Team : 0;
            zone.Anchor = spec.Anchor;
            zone.FollowAnchor = spec.FollowAnchor && spec.Anchor != 0;
            zone.Center = spec.Center;
            if (zone.FollowAnchor && _world.TryGetPosition(spec.Anchor, out FixedVec3 anchorPosition))
            {
                zone.Center = anchorPosition;
            }

            zone.RadiusRaw = spec.RadiusRaw;
            zone.DurationTicks = spec.DurationTicks > 0 ? spec.DurationTicks : 0;
            zone.RemainingTicks = zone.DurationTicks;
            zone.IntervalTicks = spec.IntervalTicks > 0 ? spec.IntervalTicks : 0;
            zone.PayloadAbilityId = spec.PayloadAbilityId;
            zone.ModifierId = spec.ModifierId;
            zone.TeamFilter = spec.TeamFilter;
            zone.AbilityLevel = spec.AbilityLevel < 1 ? 1 : spec.AbilityLevel;
            zone.SourceAbilityId = spec.SourceAbilityId ?? spec.PayloadAbilityId;
            zone.ArchetypeId = spec.ArchetypeId;
            zone.Accumulator = 0;
            zone.PulseCount = 0;
            zone.Seed = spec.Seed != 0u ? spec.Seed : FixedSeeds.Mix((uint)id, 0x5A0E5u);
            _zones.Add(zone);
            Events.Publish(new FixedEventArgs(FixedEvents.ZoneSpawned, zone.Caster, zone.Anchor, zone.RadiusRaw,
                zone.SourceAbilityId, zone.ModifierId, castId: (uint)id, tag: zone.ArchetypeId));
            if (!string.IsNullOrEmpty(zone.ArchetypeId))
            {
                _world.RequestSpawn(new FixedSpawnRequest(zone.ArchetypeId, zone.Caster, zone.Center, FixedVec3.Zero,
                    zone.FollowAnchor ? zone.Anchor : 0, zone.SourceAbilityId, zone.RadiusRaw, 0u, id, zone.DurationTicks));
            }

            if (zone.IsActive && zone.Id == id)
            {
                PulseZone(zone);
            }

            return id;
        }

        public bool RemoveZone(int zoneId)
        {
            if (!TryGetZone(zoneId, out FixedZone zone))
            {
                return false;
            }

            ExpireZone(zone);
            CompactZones();
            return true;
        }

        public int RemoveZonesOf(int caster)
        {
            int removed = 0;
            for (int i = 0; i < _zones.Count; i++)
            {
                FixedZone zone = _zones[i];
                if (zone.IsActive && zone.Caster == caster)
                {
                    ExpireZone(zone);
                    removed++;
                }
            }

            CompactZones();
            return removed;
        }

        private void StepZones()
        {
            if (_zones.Count == 0)
            {
                return;
            }

            _zoneStepDepth++;
            try
            {
                int count = _zones.Count;
                for (int i = 0; i < count; i++)
                {
                    FixedZone zone = _zones[i];
                    if (!zone.IsActive)
                    {
                        continue;
                    }

                    if (zone.Anchor != 0)
                    {
                        FixedUnit anchor = GetUnit(zone.Anchor);
                        if (anchor == null || !anchor.IsAlive)
                        {
                            ExpireZone(zone);
                            continue;
                        }

                        if (zone.FollowAnchor && _world.TryGetPosition(zone.Anchor, out FixedVec3 position))
                        {
                            zone.Center = position;
                        }
                    }

                    if (zone.IntervalTicks > 0)
                    {
                        zone.Accumulator++;
                        int id = zone.Id;
                        int safety = MaxZonePulsesPerStep;
                        while (zone.IsActive && zone.Id == id && zone.Accumulator >= zone.IntervalTicks && safety-- > 0)
                        {
                            zone.Accumulator -= zone.IntervalTicks;
                            PulseZone(zone);
                        }
                    }

                    if (zone.IsActive && !zone.IsPermanent)
                    {
                        zone.RemainingTicks--;
                        if (zone.RemainingTicks <= 0)
                        {
                            ExpireZone(zone);
                        }
                    }
                }
            }
            finally
            {
                _zoneStepDepth--;
            }

            CompactZones();
        }

        private void PulseZone(FixedZone zone)
        {
            if (_executionDepth >= FixedEffectContext.MaxDepth)
            {
                return;
            }

            while (_zoneQueryPool.Count <= _zoneQueryDepth)
            {
                _zoneQueryPool.Add(new List<int>(16));
            }

            List<int> inside = _zoneQueryPool[_zoneQueryDepth];
            _zoneQueryDepth++;
            int pulse = zone.PulseCount++;
            FixedEffectContext context = RentContext(zone.Caster, zone.SourceAbilityId, FixedSeeds.Mix(zone.Seed, (uint)pulse));
            try
            {
                inside.Clear();
                _world.QueryUnitsInRadius(zone.Center, zone.RadiusRaw, inside);
                for (int i = 0; i < inside.Count; i++)
                {
                    FixedUnit unit = GetUnit(inside[i]);
                    if (unit != null && unit.IsAlive && ZoneAdmits(zone, unit))
                    {
                        context.PrimaryTargets.Add(unit.Id);
                    }
                }

                context.TargetPoint = zone.Center;
                context.HasTargetPoint = true;
                context.AbilityLevel = zone.AbilityLevel;
                context.Origin = FixedEffectOrigin.Zone;
                if (TryGetModifier(zone.ModifierId, out FixedModifierBlueprint modifier))
                {
                    for (int i = 0; i < context.PrimaryTargets.Count; i++)
                    {
                        FixedUnit unit = GetUnit(context.PrimaryTargets[i]);
                        if (unit != null && unit.IsAlive)
                        {
                            Modifiers.Apply(modifier, zone.Caster, unit.Id, zone.SourceAbilityId, zone.AbilityLevel);
                        }
                    }
                }

                if (TryGetAbility(zone.PayloadAbilityId, out FixedAbilityBlueprint payload) && payload.ImpactEffects.Count > 0)
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
            }
        }

        private static bool ZoneAdmits(FixedZone zone, FixedUnit unit)
        {
            switch (zone.TeamFilter)
            {
                case FixedTeamFilter.Allies:
                    return unit.Id == zone.Caster || zone.CasterTeam != 0 && zone.CasterTeam == unit.Team;
                case FixedTeamFilter.Enemies:
                    return unit.Id != zone.Caster && zone.CasterTeam != unit.Team;
                default:
                    return true;
            }
        }

        private void ExpireZone(FixedZone zone)
        {
            if (!zone.IsActive)
            {
                return;
            }

            zone.IsActive = false;
            Events.Publish(new FixedEventArgs(FixedEvents.ZoneExpired, zone.Caster, zone.Anchor, 0, zone.SourceAbilityId,
                zone.ModifierId, castId: (uint)zone.Id, tag: zone.ArchetypeId));
        }

        // WHY: removal keeps spawn order (pulse order is part of determinism); never during a zone step.
        private void CompactZones()
        {
            if (_zoneStepDepth > 0)
            {
                return;
            }

            int write = 0;
            for (int read = 0; read < _zones.Count; read++)
            {
                FixedZone zone = _zones[read];
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
    }
}
