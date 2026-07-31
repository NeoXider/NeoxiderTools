using System.Collections.Generic;
using System.Reflection;
using Neo.Abilities;
using NUnit.Framework;
using UnityEditor;
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
        public void NotifyProjectileHit_RollsIndependentRngPerHit()
        {
            _caster.SetBaseProperty(AbilityProperties.CritChance, 0.5f);
            _caster.SetBaseProperty(AbilityProperties.CritMultiplier, 2f);
            Projectile();

            CastResult result = _system.Cast(CastRequest.AtUnit(_caster.Id, "bolt", _target.Id, 777u));
            Assert.That(result.Success, Is.True);

            // WHY: with the raw cast seed replayed per hit, every pierced target would crit (or
            // none would) — 8 hits must produce a mix of 40 and 80 damage outcomes.
            var outcomes = new HashSet<float>();
            for (int i = 0; i < 8; i++)
            {
                AbilityUnit enemy = AbilityTestSupport.CreateUnit(_system, team: 2, health: 1000f);
                _system.NotifyProjectileHit(result.CastId, enemy.Id, Vector3.zero);
                outcomes.Add(1000f - enemy.Health);
            }

            Assert.That(outcomes.Count, Is.GreaterThan(1),
                "per-hit rolls must not replay the identical sequence");
        }

        [Test]
        public void PiercingProjectile_DoesNotRehitSameUnit_AcrossFrames()
        {
            var hubGo = new GameObject("AbilityHubUnderTest");
            var ownerGo = new GameObject("owner");
            var enemyAGo = new GameObject("enemyA");
            var enemyBGo = new GameObject("enemyB");
            var projectileGo = new GameObject("projectile");
            try
            {
                AbilitySystemBehaviour hub = hubGo.AddComponent<AbilitySystemBehaviour>();
                AbilitySystem system = hub.System;

                AbilityUnitBehaviour owner = AddUnit(ownerGo, team: 1, new Vector3(10f, 0f, 0f));
                AbilityUnitBehaviour enemyA = AddUnit(enemyAGo, team: 2, new Vector3(0.1f, 0f, 0f));
                AbilityUnitBehaviour enemyB = AddUnit(enemyBGo, team: 2, new Vector3(-0.1f, 0f, 0f));
                AbilityTestSupport.AddPool(enemyA.Unit, AbilityResourceIds.Health, 100f);
                AbilityTestSupport.AddPool(enemyB.Unit, AbilityResourceIds.Health, 100f);

                var ability = new AbilityBlueprint
                {
                    Id = "pierce_bolt",
                    Targeting = TargetingMode.Point,
                    Delivery = AbilityDeliveryType.Projectile,
                    ProjectileArchetypeId = "unbound_archetype"
                };
                ability.ImpactEffects.Add(AbilityTestSupport.DamageNode(10f));
                system.RegisterAbility(ability);
                system.GrantAbility(owner.UnitId, "pierce_bolt");

                CastResult cast = system.Cast(
                    CastRequest.AtPoint(owner.UnitId, "pierce_bolt", new Vector3(-10f, 0f, 0f)));
                Assert.That(cast.Success, Is.True);

                var projectile = projectileGo.AddComponent<AbilityProjectileBehaviour>();
                var serialized = new SerializedObject(projectile);
                serialized.FindProperty("_maxHits").intValue = 3;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                projectileGo.transform.position = Vector3.zero;
                projectile.OnSpawned(new SpawnRequest("unbound_archetype", owner.UnitId, Vector3.zero,
                    Vector3.zero, UnitId.None, "pierce_bolt", 0f, cast.CastId), hub);

                // WHY: both enemies stay inside the hit radius on consecutive frames; a piercing
                // projectile must not burn its remaining hits on units it already hit.
                CallPrivate(projectile, "TryHitUnits");
                CallPrivate(projectile, "TryHitUnits");

                Assert.That(enemyA.Unit.Health, Is.EqualTo(90f).Within(0.001f), "enemy A hit exactly once");
                Assert.That(enemyB.Unit.Health, Is.EqualTo(90f).Within(0.001f), "enemy B hit exactly once");
            }
            finally
            {
                foreach (GameObject go in new[] { projectileGo, enemyBGo, enemyAGo, ownerGo, hubGo })
                {
                    var unitBehaviour = go.GetComponent<AbilityUnitBehaviour>();
                    if (unitBehaviour != null && unitBehaviour.Unit != null)
                    {
                        CallPrivate(unitBehaviour, "OnDisable");
                    }

                    Object.DestroyImmediate(go);
                }
            }
        }

        [Test]
        public void SpawnOp_TargetWithoutPosition_FallsBackToCastPoint()
        {
            var node = new EffectNodeData
            {
                OpId = AbilityEffectOps.Spawn,
                Target = EffectTargetSelector.Target,
                ArchetypeId = "zone"
            };
            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id, targets: _target.Id);
            ctx.TargetPoint = new Vector3(3f, 0f, 4f);
            ctx.HasTargetPoint = true;
            _world.Positions.Remove(_target.Id);

            _system.ExecuteEffects(AbilityTestSupport.Nodes(node), ctx);

            Assert.That(_world.Spawns.Count, Is.EqualTo(1));
            Assert.That(_world.Spawns[0].Position, Is.EqualTo(new Vector3(3f, 0f, 4f)),
                "cast-point fallback used when the target has no world position");
        }

        /// <summary>Adds a unit behaviour and drives its registration manually (EditMode has no lifecycle).</summary>
        private static AbilityUnitBehaviour AddUnit(GameObject go, int team, Vector3 position)
        {
            go.transform.position = position;
            var behaviour = go.AddComponent<AbilityUnitBehaviour>();
            behaviour.SetTeamOverride(team);
            CallPrivate(behaviour, "OnEnable");
            Assert.That(behaviour.Unit, Is.Not.Null, "manual OnEnable must register the unit");
            return behaviour;
        }

        private static void CallPrivate(Component target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, $"method {methodName} not found on {target.GetType().Name}");
            method.Invoke(target, null);
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
