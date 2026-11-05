using System.Collections.Generic;
using Neo.Abilities;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     Contracts of the payload ops (projectile, area_apply, zone, cast_ability), the cone selector and domain
    ///     zones: lifetimes, pulses, anchors, receipts and visual spawn requests.
    /// </summary>
    public sealed class PayloadOpAndZoneTests
    {
        private FakeWorldAdapter _world;
        private AbilitySystem _system;
        private AbilityUnit _hero;
        private AbilityUnit _enemyNear;
        private AbilityUnit _enemyBehind;
        private AbilityUnit _ally;

        [SetUp]
        public void SetUp()
        {
            _world = new FakeWorldAdapter();
            _system = new AbilitySystem { World = _world };
            _hero = AbilityTestSupport.CreateUnit(_system, team: 1, health: 500f, mana: 500f, name: "Hero");
            _enemyNear = AbilityTestSupport.CreateUnit(_system, team: 2, health: 500f, name: "Front");
            _enemyBehind = AbilityTestSupport.CreateUnit(_system, team: 2, health: 500f, name: "Behind");
            _ally = AbilityTestSupport.CreateUnit(_system, team: 1, health: 500f, name: "Ally");
            _world.SetPosition(_hero.Id, Vector3.zero);
            _world.SetPosition(_enemyNear.Id, new Vector3(3f, 0f, 0f));
            _world.SetPosition(_enemyBehind.Id, new Vector3(-3f, 0f, 0f));
            _world.SetPosition(_ally.Id, new Vector3(0f, 0f, 2f));

            AbilityBlueprint bolt = new AbilityBlueprint
            {
                Id = "bolt_payload",
                ProjectileArchetypeId = "bolt_fx",
                ProjectileSpeed = 15f
            };
            bolt.ImpactEffects.Add(AbilityTestSupport.DamageNode(25f));
            _system.RegisterAbility(bolt);
        }

        [Test]
        public void Projectile_LaunchesPerTarget_AndTheReportedHitRunsThePayload()
        {
            AbilityBlueprint volley = Ability("volley", TargetingMode.NoTarget, new EffectNodeData
            {
                OpId = AbilityEffectOps.Projectile,
                AbilityId = "bolt_payload",
                Target = EffectTargetSelector.AreaAroundCaster,
                TeamFilter = AbilityTeamFilter.Enemies,
                Radius = 10f
            });
            _system.GrantAbility(_hero.Id, volley.Id);
            List<AbilityEventArgs> landed = Record(AbilityEvents.AttackLanded);

            Assert.That(_system.Cast(CastRequest.NoTarget(_hero.Id, "volley")).Success, Is.True);

            Assert.That(_world.Spawns.Count, Is.EqualTo(2));
            SpawnRequest first = _world.Spawns[0];
            Assert.That(first.ArchetypeId, Is.EqualTo("bolt_fx"));
            Assert.That(first.Magnitude, Is.EqualTo(15f));
            Assert.That(first.CastId, Is.Not.Zero);
            Assert.That(_system.NotifyProjectileHit(first.CastId, first.TargetUnit, Vector3.zero), Is.True);
            AbilityUnit hit = _system.GetUnit(first.TargetUnit);
            Assert.That(hit.Health, Is.EqualTo(475f).Within(0.001f));
            Assert.That(landed.Count, Is.EqualTo(1), "a projectile hit is a direct hit");
            _system.ReleaseProjectileCast(first.CastId);
            Assert.That(_system.NotifyProjectileHit(first.CastId, first.TargetUnit, Vector3.zero), Is.False);
        }

        [Test]
        public void ConeSelector_KeepsUnitsInFront_AndAreaApplyAppliesModifierAndPayload()
        {
            _system.RegisterModifier(AbilityTestSupport.Modifier("scorched", 5f));
            AbilityBlueprint breath = Ability("breath", TargetingMode.Point, new EffectNodeData
            {
                OpId = AbilityEffectOps.AreaApply,
                Target = EffectTargetSelector.ConeFromCaster,
                TeamFilter = AbilityTeamFilter.Enemies,
                Radius = 6f,
                Angle = 90f,
                ModifierId = "scorched",
                AbilityId = "bolt_payload"
            });
            _system.GrantAbility(_hero.Id, breath.Id);

            _system.Cast(CastRequest.AtPoint(_hero.Id, "breath", new Vector3(5f, 0f, 0f)));

            Assert.That(_enemyNear.Health, Is.EqualTo(475f).Within(0.001f));
            Assert.That(_system.Modifiers.CountActive(_enemyNear.Id, "scorched"), Is.EqualTo(1));
            Assert.That(_enemyBehind.Health, Is.EqualTo(500f), "behind the caster is outside a 90 degree cone");
            Assert.That(_system.Modifiers.CountActive(_enemyBehind.Id, "scorched"), Is.Zero);
            Assert.That(_ally.Health, Is.EqualTo(500f));
        }

        [Test]
        public void FormationZone_PulsesOnSpawnAndEveryInterval_ThenExpires()
        {
            AbilityTemplateBundle formation = AbilityTemplates.FormationZone("trap", radius: 4f, duration: 3f,
                interval: 1f, damage: 10f);
            formation.SetUp(_system, _hero.Id);
            List<AbilityEventArgs> spawned = Record(AbilityEvents.ZoneSpawned);
            List<AbilityEventArgs> expired = Record(AbilityEvents.ZoneExpired);

            Assert.That(_system.Cast(CastRequest.AtPoint(_hero.Id, "trap", new Vector3(3f, 0f, 0f))).Success, Is.True);
            Assert.That(_enemyNear.Health, Is.EqualTo(490f).Within(0.001f), "the spawn pulse");
            Assert.That(_enemyNear.GetProperty(AbilityProperties.MoveSpeed, 1f), Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(_enemyBehind.Health, Is.EqualTo(500f), "6 m away is outside the 4 m formation");
            Assert.That(_hero.Health, Is.EqualTo(500f), "the caster is not an enemy of its own formation");
            Assert.That(spawned.Count, Is.EqualTo(1));
            Assert.That(_world.Spawns.Count, Is.EqualTo(1));
            Assert.That(_world.Spawns[0].ZoneId, Is.EqualTo((int)spawned[0].CastId));
            Assert.That(_world.Spawns[0].Duration, Is.EqualTo(3f));
            Assert.That(_world.Spawns[0].Magnitude, Is.EqualTo(4f));

            for (int i = 0; i < 4; i++)
            {
                _system.Tick(1f);
            }

            Assert.That(_enemyNear.Health, Is.EqualTo(460f).Within(0.001f), "spawn pulse plus three interval pulses");
            Assert.That(expired.Count, Is.EqualTo(1));
            Assert.That(expired[0].CastId, Is.EqualTo(spawned[0].CastId));
            Assert.That(_system.ZoneCount, Is.Zero);
        }

        [Test]
        public void Aura_FollowsTheCaster_AndEndsWhenTheCasterDies()
        {
            AbilityTemplateBundle aura = AbilityTemplates.Aura("banner", radius: 3f, armor: 4f, duration: 0f);
            aura.SetUp(_system, _hero.Id);
            _system.Cast(CastRequest.NoTarget(_hero.Id, "banner"));
            Assert.That(_ally.GetProperty(AbilityProperties.Armor), Is.EqualTo(4f).Within(0.001f));
            Assert.That(_enemyNear.GetProperty(AbilityProperties.Armor), Is.Zero, "enemies get no ally buff");

            _world.SetPosition(_hero.Id, new Vector3(20f, 0f, 0f));
            _system.Tick(1.5f);
            Assert.That(_ally.GetProperty(AbilityProperties.Armor), Is.Zero, "the aura moved away and the buff faded");
            Assert.That(_system.TryGetZone(1, out AbilityZone zone), Is.True);
            Assert.That(zone.Center.x, Is.EqualTo(20f).Within(0.001f));

            DamageService.ApplyDamage(_system, UnitId.None, _hero.Id, 10000f, AbilityDamageTypes.Pure);
            _system.Tick(0.1f);
            Assert.That(_system.ZoneCount, Is.Zero, "an anchored zone ends with its anchor");
        }

        [Test]
        public void SpawnZone_CodePath_RemoveZoneAndRemoveZonesOf()
        {
            _system.RegisterModifier(AbilityTestSupport.Modifier("calm", 1f));
            int first = _system.SpawnZone(new AbilityZoneSpec
            {
                Caster = _hero.Id,
                Center = Vector3.zero,
                Radius = 10f,
                Interval = 0.5f,
                ModifierId = "calm",
                TeamFilter = AbilityTeamFilter.Any
            });
            int second = _system.SpawnZone(new AbilityZoneSpec { Caster = _hero.Id, Radius = 1f });

            Assert.That(first, Is.Not.Zero);
            Assert.That(_system.SpawnZone(new AbilityZoneSpec { Caster = _hero.Id, Radius = 0f }), Is.Zero);
            Assert.That(_system.Modifiers.CountActive(_enemyBehind.Id, "calm"), Is.EqualTo(1));
            Assert.That(_system.RemoveZone(first), Is.True);
            Assert.That(_system.RemoveZone(first), Is.False);
            Assert.That(_system.RemoveZonesOf(_hero.Id), Is.EqualTo(1));
            Assert.That(_system.TryGetZone(second, out _), Is.False);
        }

        [Test]
        public void CastAbility_FromAPassiveTick_CastsAtTheNearestEnemy_AndRespectsCooldowns()
        {
            AbilityTemplateBundle technique = AbilityTemplates.AutoCastTechnique("spark", interval: 1f, damage: 7f,
                range: 5f);
            technique.SetUp(_system, _hero.Id);
            technique.FindAbility("spark").Cooldown = 1.5f;

            _system.Tick(1f);
            Assert.That(_enemyNear.Health + _enemyBehind.Health, Is.EqualTo(993f).Within(0.001f));
            _system.Tick(1f);
            Assert.That(_enemyNear.Health + _enemyBehind.Health, Is.EqualTo(993f).Within(0.001f),
                "the slot is still on cooldown at the second pulse");
            _system.Tick(1f);
            Assert.That(_enemyNear.Health + _enemyBehind.Health, Is.EqualTo(986f).Within(0.001f));
        }

        [Test]
        public void CastAbility_SelfRecursion_StopsAtTheCastDepthCap()
        {
            AbilityBlueprint echo = Ability("echo", TargetingMode.NoTarget, new EffectNodeData
            {
                OpId = AbilityEffectOps.CastAbility,
                AbilityId = "echo",
                Target = EffectTargetSelector.Caster
            });
            echo.CastEffects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.ResourceChange,
                Target = EffectTargetSelector.Caster,
                ResourceId = AbilityResourceIds.Mana,
                Amount = -1f
            });
            _system.GrantAbility(_hero.Id, echo.Id);
            _system.Trace.Enabled = true;

            Assert.That(_system.Cast(CastRequest.NoTarget(_hero.Id, "echo")).Success, Is.True);

            Assert.That(_hero.Resources.GetCurrent(AbilityResourceIds.Mana),
                Is.EqualTo(500f - AbilitySystem.MaxCastDepth).Within(0.001f));
            Assert.That(_system.Trace.TryFindLatest(AbilityTraceKind.CastFailed, "echo", out AbilityTraceEntry failed),
                Is.True);
            Assert.That(failed.Index, Is.EqualTo((int)CastFailureReason.NestingLimit));
        }

        private AbilityBlueprint Ability(string id, TargetingMode targeting, EffectNodeData impact)
        {
            AbilityBlueprint ability = new AbilityBlueprint { Id = id, Targeting = targeting };
            ability.ImpactEffects.Add(impact);
            _system.RegisterAbility(ability);
            return ability;
        }

        private List<AbilityEventArgs> Record(string eventId)
        {
            List<AbilityEventArgs> events = new List<AbilityEventArgs>();
            _system.Events.Subscribe(eventId, events.Add);
            return events;
        }
    }
}
