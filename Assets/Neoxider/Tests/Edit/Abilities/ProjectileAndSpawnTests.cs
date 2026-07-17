using Neo.Abilities;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     Projectile delivery (deferred impact via the host callback) and the spawn effect op routed
    ///     through the world adapter.
    /// </summary>
    public sealed class ProjectileAndSpawnTests
    {
        private AbilitySystem _system;
        private FakeWorldAdapter _world;
        private AbilityUnit _caster;
        private AbilityUnit _target;

        [SetUp]
        public void SetUp()
        {
            _system = new AbilitySystem();
            _world = new FakeWorldAdapter();
            _system.World = _world;
            _caster = AbilityTestSupport.CreateUnit(_system, team: 1);
            _target = AbilityTestSupport.CreateUnit(_system, team: 2, health: 1000f);
            _world.SetPosition(_caster.Id, Vector3.zero);
            _world.SetPosition(_target.Id, new Vector3(5f, 0f, 0f));
        }

        private AbilityBlueprint Projectile()
        {
            var ability = new AbilityBlueprint
            {
                Id = "bolt",
                Targeting = TargetingMode.Unit,
                Delivery = AbilityDeliveryType.Projectile,
                ProjectileArchetypeId = "bolt_proj",
                ProjectileSpeed = 20f
            };
            ability.ImpactEffects.Add(AbilityTestSupport.DamageNode(40f));
            _system.RegisterAbility(ability);
            _system.GrantAbility(_caster.Id, "bolt");
            return ability;
        }

        [Test]
        public void ProjectileCast_RequestsSpawn_AndDefersImpact()
        {
            Projectile();

            CastResult result = _system.Cast(CastRequest.AtUnit(_caster.Id, "bolt", _target.Id));

            Assert.That(result.Success, Is.True);
            Assert.That(_world.Spawns.Count, Is.EqualTo(1));
            Assert.That(_world.Spawns[0].ArchetypeId, Is.EqualTo("bolt_proj"));
            Assert.That(_world.Spawns[0].CastId, Is.EqualTo(result.CastId));
            Assert.That(_target.Health, Is.EqualTo(1000f), "impact is deferred until the projectile reports a hit");
        }

        [Test]
        public void NotifyProjectileHit_RunsImpactOnHitUnit()
        {
            Projectile();
            CastResult result = _system.Cast(CastRequest.AtUnit(_caster.Id, "bolt", _target.Id));

            bool handled = _system.NotifyProjectileHit(result.CastId, _target.Id, new Vector3(5f, 0f, 0f));

            Assert.That(handled, Is.True);
            Assert.That(_target.Health, Is.EqualTo(960f).Within(0.001f), "40 impact damage applied on hit");
        }

        [Test]
        public void NotifyProjectileHit_UnknownCast_ReturnsFalse()
        {
            Assert.That(_system.NotifyProjectileHit(999u, _target.Id, Vector3.zero), Is.False);
        }

        [Test]
        public void ReleaseProjectileCast_PreventsLaterImpact()
        {
            Projectile();
            CastResult result = _system.Cast(CastRequest.AtUnit(_caster.Id, "bolt", _target.Id));

            _system.ReleaseProjectileCast(result.CastId);
            bool handled = _system.NotifyProjectileHit(result.CastId, _target.Id, Vector3.zero);

            Assert.That(handled, Is.False);
            Assert.That(_target.Health, Is.EqualTo(1000f));
        }

        [Test]
        public void SpawnOp_RequestsSpawnAtTargetPosition()
        {
            var node = new EffectNodeData
            {
                OpId = AbilityEffectOps.Spawn,
                Target = EffectTargetSelector.Target,
                ArchetypeId = "zone",
                Amount = 3f
            };
            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id, targets: _target.Id);

            _system.ExecuteEffects(AbilityTestSupport.Nodes(node), ctx);

            Assert.That(_world.Spawns.Count, Is.EqualTo(1));
            SpawnRequest spawn = _world.Spawns[0];
            Assert.That(spawn.ArchetypeId, Is.EqualTo("zone"));
            Assert.That(spawn.TargetUnit, Is.EqualTo(_target.Id));
            Assert.That(spawn.Position, Is.EqualTo(new Vector3(5f, 0f, 0f)));
            Assert.That(spawn.Magnitude, Is.EqualTo(3f));
        }
    }
}
