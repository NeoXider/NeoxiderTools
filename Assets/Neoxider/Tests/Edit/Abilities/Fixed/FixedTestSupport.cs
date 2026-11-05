using System.Collections.Generic;
using Neo.Abilities;
using Neo.Abilities.Fixed;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities.Fixed
{
    /// <summary>Helpers of the fixed-core tests: Q8.8 conversions, unit factories, fixed content builders.</summary>
    internal static class FixedTestSupport
    {
        public const int One = 256;

        public static int Q(double value)
        {
            return FixedContentConverter.ToRaw(value, FixedFormat.Q8);
        }

        public static double D(int raw)
        {
            return raw / (double)One;
        }

        public static FixedAbilitySystem NewSystem(int ticksPerSecond = 30)
        {
            return new FixedAbilitySystem(new FixedRules { TicksPerSecond = ticksPerSecond });
        }

        public static FixedUnit Unit(FixedAbilitySystem system, int team, double health = 1000d, double mana = 0d,
            double x = 0d, double z = 0d)
        {
            FixedUnit unit = system.CreateUnit(team);
            if (health > 0d)
            {
                unit.AddPool(FixedResources.Health, Q(health));
            }

            if (mana > 0d)
            {
                unit.AddPool(FixedResources.Mana, Q(mana));
            }

            unit.Position = new FixedVec3(Q(x), 0, Q(z));
            return unit;
        }

        public static FixedEffectNode Node(string opId, double amount = 0d,
            FixedTargetSelector target = FixedTargetSelector.Target, string damageType = FixedDamageTypes.Pure)
        {
            return new FixedEffectNode { OpId = opId, AmountRaw = Q(amount), Target = target, DamageType = damageType };
        }

        public static FixedAbilityBlueprint Ability(string id, FixedTargeting targeting, params FixedEffectNode[] impact)
        {
            FixedAbilityBlueprint ability = new FixedAbilityBlueprint { Id = id, Targeting = targeting };
            ability.ImpactEffects.AddRange(impact);
            return ability;
        }

        public static FixedModifierBlueprint Modifier(string id, int durationTicks = 0,
            FixedStackPolicy policy = FixedStackPolicy.Refresh)
        {
            return new FixedModifierBlueprint { Id = id, DurationTicks = durationTicks, StackPolicy = policy };
        }
    }

    /// <summary>Records every receipt of a fixed system (after reactions).</summary>
    internal sealed class FixedEventLog
    {
        public readonly List<FixedEventArgs> Events = new List<FixedEventArgs>();

        public FixedEventLog(FixedAbilitySystem system)
        {
            system.Events.SubscribeAny(Record);
        }

        public int Count(string eventId)
        {
            int count = 0;
            for (int i = 0; i < Events.Count; i++)
            {
                if (Events[i].EventId == eventId)
                {
                    count++;
                }
            }

            return count;
        }

        private void Record(in FixedEventArgs args)
        {
            Events.Add(args);
        }
    }

    /// <summary>Fixed world adapter on unit positions that also records spawn requests.</summary>
    internal sealed class FixedRecordingWorld : IFixedWorldAdapter
    {
        private readonly FixedAbilitySystem _system;
        public readonly List<FixedSpawnRequest> Spawns = new List<FixedSpawnRequest>();

        public FixedRecordingWorld(FixedAbilitySystem system)
        {
            _system = system;
        }

        public bool TryGetPosition(int unit, out FixedVec3 position)
        {
            return _system.DefaultWorld.TryGetPosition(unit, out position);
        }

        public void QueryUnitsInRadius(FixedVec3 center, int radiusRaw, List<int> results)
        {
            _system.DefaultWorld.QueryUnitsInRadius(center, radiusRaw, results);
        }

        public void RequestSpawn(in FixedSpawnRequest request)
        {
            Spawns.Add(request);
        }
    }

    /// <summary>
    ///     One API over both cores, so a conformance scenario is written once and played on the float core (with
    ///     <see cref="DeterministicAbilityProfile" />) and on the fixed core (with converted content).
    /// </summary>
    internal interface IAbilityScenarioDriver
    {
        string Name { get; }
        int Unit(int team, double health, double mana = 0d, double x = 0d, double z = 0d);
        void Base(int unit, string propertyId, double value);
        void Register(IEnumerable<AbilityBlueprint> abilities, IEnumerable<ModifierBlueprint> modifiers);
        void Grant(int unit, string abilityId);
        void Apply(string modifierId, int caster, int target);
        bool Cast(int caster, string abilityId, int target = 0, uint seed = 0u);
        bool CastAt(int caster, string abilityId, double x, double z, uint seed = 0u);
        bool HitLastProjectile(int unit);
        void Step(int ticks);
        double Health(int unit);
        double Mana(int unit);
        double Property(int unit, string propertyId);
        bool Alive(int unit);
        int Count(int unit, string modifierId);
        int Stacks(int unit, string modifierId);
        int RemainingTicks(int unit, string modifierId);
        int Charges(int unit, string modifierId, int reaction);
        int EventCount(string eventId);
        double EventSum(string eventId);
        int ZoneCount { get; }
    }

    internal sealed class FloatScenarioDriver : IAbilityScenarioDriver
    {
        private readonly AbilitySystem _system = new AbilitySystem();
        private readonly FakeWorldAdapter _world = new FakeWorldAdapter();
        private readonly List<AbilityEventArgs> _events = new List<AbilityEventArgs>();
        private readonly int _ticksPerSecond;

        public FloatScenarioDriver(int ticksPerSecond = 30)
        {
            _ticksPerSecond = ticksPerSecond;
            DeterministicAbilityProfile.Apply(_system.Rules, ticksPerSecond);
            _system.World = _world;
            _system.Events.SubscribeAny(args => _events.Add(args));
        }

        public string Name => "float+profile";
        public int ZoneCount => _system.ZoneCount;

        public int Unit(int team, double health, double mana = 0d, double x = 0d, double z = 0d)
        {
            AbilityUnit unit = AbilityTestSupport.CreateUnit(_system, team, (float)health, mana: (float)mana);
            _world.SetPosition(unit.Id, new Vector3((float)x, 0f, (float)z));
            return (int)unit.Id.Value;
        }

        public void Base(int unit, string propertyId, double value)
        {
            _system.GetUnit(Id(unit)).SetBaseProperty(propertyId, (float)value);
        }

        public void Register(IEnumerable<AbilityBlueprint> abilities, IEnumerable<ModifierBlueprint> modifiers)
        {
            foreach (ModifierBlueprint modifier in modifiers)
            {
                _system.RegisterModifier(modifier);
            }

            foreach (AbilityBlueprint ability in abilities)
            {
                _system.RegisterAbility(ability);
            }
        }

        public void Grant(int unit, string abilityId)
        {
            _system.GrantAbility(Id(unit), abilityId);
        }

        public void Apply(string modifierId, int caster, int target)
        {
            _system.ApplyModifier(modifierId, Id(caster), Id(target));
        }

        public bool Cast(int caster, string abilityId, int target = 0, uint seed = 0u)
        {
            CastRequest request = target != 0
                ? CastRequest.AtUnit(Id(caster), abilityId, Id(target), seed)
                : CastRequest.NoTarget(Id(caster), abilityId, seed);
            return _system.Cast(request).Success;
        }

        public bool CastAt(int caster, string abilityId, double x, double z, uint seed = 0u)
        {
            return _system.Cast(CastRequest.AtPoint(Id(caster), abilityId, new Vector3((float)x, 0f, (float)z), seed))
                .Success;
        }

        public bool HitLastProjectile(int unit)
        {
            if (_world.Spawns.Count == 0)
            {
                return false;
            }

            SpawnRequest spawn = _world.Spawns[_world.Spawns.Count - 1];
            _world.TryGetPosition(Id(unit), out Vector3 point);
            return _system.NotifyProjectileHit(spawn.CastId, Id(unit), point);
        }

        public void Step(int ticks)
        {
            _system.StepTicks(ticks);
        }

        public double Health(int unit)
        {
            return _system.GetUnit(Id(unit)).Health;
        }

        public double Mana(int unit)
        {
            return _system.GetUnit(Id(unit)).Resources.GetCurrent(AbilityResourceIds.Mana);
        }

        public double Property(int unit, string propertyId)
        {
            return _system.GetUnit(Id(unit)).GetProperty(propertyId);
        }

        public bool Alive(int unit)
        {
            return _system.GetUnit(Id(unit)).IsAlive;
        }

        public int Count(int unit, string modifierId)
        {
            return _system.Modifiers.CountActive(Id(unit), modifierId);
        }

        public int Stacks(int unit, string modifierId)
        {
            return _system.Modifiers.TryGetActive(Id(unit), modifierId, out ModifierInstance m) ? m.Stacks : 0;
        }

        public int RemainingTicks(int unit, string modifierId)
        {
            return _system.Modifiers.TryGetActive(Id(unit), modifierId, out ModifierInstance m)
                ? Mathf.RoundToInt(m.RemainingDuration * _ticksPerSecond)
                : -1;
        }

        public int Charges(int unit, string modifierId, int reaction)
        {
            return _system.Modifiers.TryGetActive(Id(unit), modifierId, out ModifierInstance m)
                ? m.GetRemainingCharges(reaction)
                : -2;
        }

        public int EventCount(string eventId)
        {
            int count = 0;
            for (int i = 0; i < _events.Count; i++)
            {
                if (_events[i].EventId == eventId)
                {
                    count++;
                }
            }

            return count;
        }

        public double EventSum(string eventId)
        {
            double sum = 0d;
            for (int i = 0; i < _events.Count; i++)
            {
                if (_events[i].EventId == eventId)
                {
                    sum += _events[i].Amount;
                }
            }

            return sum;
        }

        private static UnitId Id(int unit)
        {
            return new UnitId((uint)unit);
        }
    }

    internal sealed class FixedScenarioDriver : IAbilityScenarioDriver
    {
        private readonly FixedAbilitySystem _system;
        private readonly FixedRecordingWorld _world;
        private readonly FixedEventLog _log;
        private readonly FixedConversionSettings _settings;

        public FixedScenarioDriver(int ticksPerSecond = 30)
        {
            _settings = new FixedConversionSettings { TicksPerSecond = ticksPerSecond };
            _system = new FixedAbilitySystem(_settings.CreateRules());
            _world = new FixedRecordingWorld(_system);
            _system.World = _world;
            _log = new FixedEventLog(_system);
        }

        public readonly List<FixedConversionIssue> Issues = new List<FixedConversionIssue>();

        public FixedAbilitySystem System => _system;
        public string Name => "fixed Q8.8";
        public int ZoneCount => _system.ZoneCount;

        public int Unit(int team, double health, double mana = 0d, double x = 0d, double z = 0d)
        {
            return FixedTestSupport.Unit(_system, team, health, mana, x, z).Id;
        }

        public void Base(int unit, string propertyId, double value)
        {
            _system.GetUnit(unit).SetBaseProperty(propertyId, FixedTestSupport.Q(value));
        }

        public void Register(IEnumerable<AbilityBlueprint> abilities, IEnumerable<ModifierBlueprint> modifiers)
        {
            FixedContentConverter.Convert(abilities, modifiers, _settings, Issues).RegisterInto(_system);
        }

        public void Grant(int unit, string abilityId)
        {
            _system.GrantAbility(unit, abilityId);
        }

        public void Apply(string modifierId, int caster, int target)
        {
            _system.ApplyModifier(modifierId, caster, target);
        }

        public bool Cast(int caster, string abilityId, int target = 0, uint seed = 0u)
        {
            FixedCastRequest request = target != 0
                ? FixedCastRequest.AtUnit(caster, abilityId, target, seed)
                : FixedCastRequest.NoTarget(caster, abilityId, seed);
            return _system.Cast(in request).Success;
        }

        public bool CastAt(int caster, string abilityId, double x, double z, uint seed = 0u)
        {
            FixedCastRequest request = FixedCastRequest.AtPoint(caster, abilityId,
                new FixedVec3(FixedTestSupport.Q(x), 0, FixedTestSupport.Q(z)), seed);
            return _system.Cast(in request).Success;
        }

        public bool HitLastProjectile(int unit)
        {
            if (_world.Spawns.Count == 0)
            {
                return false;
            }

            FixedSpawnRequest spawn = _world.Spawns[_world.Spawns.Count - 1];
            _world.TryGetPosition(unit, out FixedVec3 point);
            return _system.NotifyProjectileHit(spawn.CastId, unit, point);
        }

        public void Step(int ticks)
        {
            _system.Step(ticks);
        }

        public double Health(int unit)
        {
            return FixedTestSupport.D(_system.GetUnit(unit).HealthRaw);
        }

        public double Mana(int unit)
        {
            return FixedTestSupport.D(_system.GetUnit(unit).ManaRaw);
        }

        public double Property(int unit, string propertyId)
        {
            return FixedTestSupport.D(_system.GetUnit(unit).GetProperty(propertyId));
        }

        public bool Alive(int unit)
        {
            return _system.GetUnit(unit).IsAlive;
        }

        public int Count(int unit, string modifierId)
        {
            return _system.Modifiers.CountActive(unit, modifierId);
        }

        public int Stacks(int unit, string modifierId)
        {
            return _system.Modifiers.TryGetActive(unit, modifierId, out FixedModifierInstance m) ? m.Stacks : 0;
        }

        public int RemainingTicks(int unit, string modifierId)
        {
            return _system.Modifiers.TryGetActive(unit, modifierId, out FixedModifierInstance m) ? m.RemainingTicks : -1;
        }

        public int Charges(int unit, string modifierId, int reaction)
        {
            return _system.Modifiers.TryGetActive(unit, modifierId, out FixedModifierInstance m)
                ? m.GetRemainingCharges(reaction)
                : -2;
        }

        public int EventCount(string eventId)
        {
            return _log.Count(eventId);
        }

        public double EventSum(string eventId)
        {
            long sum = 0L;
            for (int i = 0; i < _log.Events.Count; i++)
            {
                if (_log.Events[i].EventId == eventId)
                {
                    sum += _log.Events[i].AmountRaw;
                }
            }

            return sum / (double)FixedTestSupport.One;
        }
    }
}
