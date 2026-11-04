using System;
using System.Collections.Generic;
using Neo.Abilities;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     Zero-GC guards for the survivor-scale hot paths: event publication, instant and projectile casts
    ///     with costs, capped area casts, projectile hits, modifier ticks and event reactions, chain bounces,
    ///     dispel/remove, grid and hub radius queries (with in-place grid moves and removals), auto-caster
    ///     passes and blocked caster casts. Each test
    ///     warms its buffers once, then counts native GC.Alloc calls of the repeated calls on this thread.
    /// </summary>
    public sealed class AbilityHotPathAllocationTests
    {
        private const int Iterations = 200;

        [Test]
        public void EventBusPublish_WithHandlersObserversAndNesting_AllocatesNothingWhenWarm()
        {
            AbilityEventBus bus = new AbilityEventBus();
            UnitId target = new UnitId(7);
            int calls = 0;
            Action<AbilityEventArgs> counter = args => calls++;
            bus.Subscribe("hit", counter);
            bus.Subscribe("hit", counter);
            bus.SubscribeAny(counter);
            bus.SubscribeTarget(target, counter);
            AbilityEventArgs hit = new AbilityEventArgs("hit", target, UnitId.None, 5f);
            AbilityEventArgs outer = new AbilityEventArgs("outer", target, UnitId.None);
            bus.Subscribe("outer", args => bus.Publish(hit));
            bus.Publish(hit);
            bus.Publish(outer);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                bus.Publish(hit);
                bus.Publish(outer);
            }

            long allocated = allocationCounter.End();

            Assert.That(allocated, Is.EqualTo(0L));
            // WHY: per hit 4 calls (2 specific + global + target); per outer 2 observers + one nested hit.
            Assert.That(calls, Is.EqualTo((Iterations + 1) * 10));
        }

        [Test]
        public void InstantCastWithCost_AllocatesNothingWhenWarm()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, team: 1, mana: 1000000f);
            AbilityUnit target = AbilityTestSupport.CreateUnit(system, team: 2, health: 1000000f);
            AbilityBlueprint strike = new AbilityBlueprint
            {
                Id = "gc_strike",
                Targeting = TargetingMode.Unit,
                Delivery = AbilityDeliveryType.Instant
            };
            strike.Costs.Add(new AbilityCost(AbilityResourceIds.Mana, 1f));
            strike.ImpactEffects.Add(AbilityTestSupport.DamageNode(1f));
            system.RegisterAbility(strike);
            system.GrantAbility(caster.Id, "gc_strike");
            EventLog log = new EventLog(system);
            CastRequest request = CastRequest.AtUnit(caster.Id, "gc_strike", target.Id);
            Assert.That(system.Cast(request).Success, Is.True);
            log.Events.Capacity = 4096;

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                system.Cast(request);
            }

            long allocated = allocationCounter.End();

            Assert.That(allocated, Is.EqualTo(0L));
            Assert.That(target.Health, Is.EqualTo(1000000f - (Iterations + 1)).Within(0.01f));
        }

        [Test]
        public void ProjectileCastHitAndRelease_AllocateNothingWhenWarm()
        {
            FakeWorldAdapter world = new FakeWorldAdapter();
            AbilitySystem system = new AbilitySystem { World = world };
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, team: 1, mana: 1000000f);
            AbilityUnit target = AbilityTestSupport.CreateUnit(system, team: 2, health: 1000000f);
            world.SetPosition(caster.Id, Vector3.zero);
            world.SetPosition(target.Id, new Vector3(5f, 0f, 0f));
            AbilityBlueprint volley = new AbilityBlueprint
            {
                Id = "gc_volley",
                Targeting = TargetingMode.Unit,
                Delivery = AbilityDeliveryType.Projectile,
                ProjectileArchetypeId = "gc_volley_projectile"
            };
            volley.Costs.Add(new AbilityCost(AbilityResourceIds.Mana, 1f));
            volley.ImpactEffects.Add(AbilityTestSupport.DamageNode(1f));
            system.RegisterAbility(volley);
            system.GrantAbility(caster.Id, "gc_volley");
            CastRequest request = CastRequest.AtUnit(caster.Id, "gc_volley", target.Id);
            CastResult warm = system.Cast(request);
            Assert.That(warm.Success, Is.True);
            Assert.That(system.NotifyProjectileHit(warm.CastId, target.Id, Vector3.zero), Is.True);
            system.ReleaseProjectileCast(warm.CastId);
            world.Spawns.Capacity = 4096;

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                CastResult cast = system.Cast(request);
                system.NotifyProjectileHit(cast.CastId, target.Id, Vector3.zero);
                system.ReleaseProjectileCast(cast.CastId);
            }

            long allocated = allocationCounter.End();

            Assert.That(allocated, Is.EqualTo(0L));
            Assert.That(world.Spawns.Count, Is.EqualTo(Iterations + 1));
            Assert.That(target.Health, Is.EqualTo(1000000f - (Iterations + 1)).Within(0.01f));
        }

        [Test]
        public void ProjectileHits_AllocateNothingWhenWarm()
        {
            AbilitySystem system = new AbilitySystem { World = new FakeWorldAdapter() };
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, team: 1);
            AbilityUnit target = AbilityTestSupport.CreateUnit(system, team: 2, health: 1000000f);
            AbilityBlueprint bolt = new AbilityBlueprint
            {
                Id = "gc_bolt",
                Targeting = TargetingMode.Unit,
                Delivery = AbilityDeliveryType.Projectile,
                ProjectileArchetypeId = "gc_bolt_projectile"
            };
            bolt.ImpactEffects.Add(AbilityTestSupport.DamageNode(1f, AbilityDamageTypes.Physical));
            system.RegisterAbility(bolt);
            system.GrantAbility(caster.Id, "gc_bolt");
            caster.SetBaseProperty(AbilityProperties.CritChance, 0.5f);
            int observed = 0;
            system.Events.SubscribeTarget(target.Id, args => observed++);
            CastResult cast = system.Cast(CastRequest.AtUnit(caster.Id, "gc_bolt", target.Id, 99u));
            Assert.That(cast.Success, Is.True);
            Assert.That(system.NotifyProjectileHit(cast.CastId, target.Id, Vector3.zero), Is.True);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                system.NotifyProjectileHit(cast.CastId, target.Id, Vector3.zero);
            }

            long allocated = allocationCounter.End();

            Assert.That(allocated, Is.EqualTo(0L));
            Assert.That(observed, Is.GreaterThanOrEqualTo(Iterations + 1));
        }

        [Test]
        public void ModifierTicks_DamageOverTimeOnManyUnits_AllocateNothingWhenWarm()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, team: 1);
            ModifierBlueprint burn = AbilityTestSupport.Modifier("gc_burn");
            burn.TickInterval = 0.5f;
            burn.TickEffects.Add(AbilityTestSupport.DamageNode(1f));
            List<AbilityUnit> victims = new List<AbilityUnit>();
            for (int i = 0; i < 64; i++)
            {
                AbilityUnit victim = AbilityTestSupport.CreateUnit(system, team: 2, health: 1000000f);
                system.Modifiers.Apply(burn, caster.Id, victim.Id);
                victims.Add(victim);
            }

            system.Tick(0.5f);
            system.Tick(0.5f);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                system.Tick(0.5f);
            }

            long allocated = allocationCounter.End();

            Assert.That(allocated, Is.EqualTo(0L));
            Assert.That(victims[0].Health, Is.EqualTo(1000000f - (Iterations + 2)).Within(0.01f));
        }

        [Test]
        public void ChainBounces_AllocateNothingWhenWarm()
        {
            AbilitySystem system = new AbilitySystem();
            FakeWorldAdapter world = new FakeWorldAdapter();
            system.World = world;
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, team: 1);
            world.SetPosition(caster.Id, Vector3.zero);
            AbilityUnit first = null;
            for (int i = 0; i < 6; i++)
            {
                AbilityUnit enemy = AbilityTestSupport.CreateUnit(system, team: 2, health: 1000000f);
                world.SetPosition(enemy.Id, new Vector3(i * 2f, 0f, 0f));
                first ??= enemy;
            }

            EffectNodeData chain = new EffectNodeData
            {
                OpId = AbilityEffectOps.Chain,
                Amount = 1f,
                Radius = 3f,
                MaxTargets = 5,
                DamageType = AbilityDamageTypes.Pure
            };
            List<EffectNodeData> nodes = AbilityTestSupport.Nodes(chain);
            EffectContext context = AbilityTestSupport.Context(system, caster.Id, targets: first.Id);
            system.ExecuteEffects(nodes, context);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                system.ExecuteEffects(nodes, context);
            }

            long allocated = allocationCounter.End();

            Assert.That(allocated, Is.EqualTo(0L));
            Assert.That(first.Health, Is.EqualTo(1000000f - (Iterations + 1)).Within(0.01f));
        }

        [Test]
        public void RemoveModifierAndDispel_WithoutMatches_AllocateNothing()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, team: 1);
            AbilityUnit target = AbilityTestSupport.CreateUnit(system, team: 2);
            ModifierBlueprint aura = AbilityTestSupport.Modifier("gc_aura");
            aura.Dispellable = false;
            system.Modifiers.Apply(aura, caster.Id, target.Id);
            List<EffectNodeData> nodes = AbilityTestSupport.Nodes(
                new EffectNodeData { OpId = AbilityEffectOps.RemoveModifier, ModifierId = "gc_missing" },
                new EffectNodeData { OpId = AbilityEffectOps.Dispel },
                new EffectNodeData { OpId = AbilityEffectOps.Dispel, CustomParam = "buffs" });
            EffectContext context = AbilityTestSupport.Context(system, caster.Id, targets: target.Id);
            system.ExecuteEffects(nodes, context);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                system.ExecuteEffects(nodes, context);
            }

            long allocated = allocationCounter.End();

            Assert.That(allocated, Is.EqualTo(0L));
            Assert.That(AbilityTestSupport.ActiveModifiers(system, target.Id).Count, Is.EqualTo(1));
        }

        [Test]
        public void SpatialGridQueries_AllocateNothingWhenWarm()
        {
            AbilitySpatialGrid grid = new AbilitySpatialGrid(2f);
            XorShiftRandom random = new XorShiftRandom(5u);
            for (uint i = 1; i <= 500; i++)
            {
                grid.Add(new UnitId(i), new Vector3(random.NextFloat() * 80f - 40f, 0f, random.NextFloat() * 80f - 40f));
            }

            List<UnitId> results = new List<UnitId>(512);
            grid.Query(Vector3.zero, 3f, results);
            grid.Query(Vector3.zero, 500f, results);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                results.Clear();
                grid.Query(new Vector3(i % 40 - 20f, 0f, 0f), 2.5f, results);
            }

            long allocated = allocationCounter.End();

            Assert.That(allocated, Is.EqualTo(0L));
        }

        [Test]
        public void SpatialGridMultiHitQueries_AllocateNothingWhenWarm()
        {
            // WHY: multi-hit queries sort their hit indices; List.Sort() allocates a delegate per call in
            // Unity's Mono, so the grid must keep using a cached Comparison.
            AbilitySpatialGrid grid = new AbilitySpatialGrid(1f);
            for (uint i = 1; i <= 256; i++)
            {
                grid.Add(new UnitId(i), new Vector3(i % 16 * 0.5f, 0f, i / 16 * 0.5f));
            }

            List<UnitId> results = new List<UnitId>(256);
            grid.Query(new Vector3(4f, 0f, 4f), 2f, results);
            Assert.That(results.Count, Is.GreaterThan(8), "the warm query must hit several cells");

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                results.Clear();
                grid.Query(new Vector3(i % 8, 0f, 4f), 2f, results);
            }

            long allocated = allocationCounter.End();

            Assert.That(allocated, Is.EqualTo(0L));
            Assert.That(results.Count, Is.GreaterThan(8));
        }

        [Test]
        public void SpatialGridMovesAndRemovals_AllocateNothingWhenWarm()
        {
            AbilitySpatialGrid grid = new AbilitySpatialGrid(2f);
            List<UnitId> results = new List<UnitId>(256);
            FillGrid(grid, 256);
            grid.Move(new UnitId(1u), new Vector3(3f, 0f, 3f));
            grid.Remove(new UnitId(2u));
            grid.Query(Vector3.zero, 4f, results);
            // WHY: the id index and cell lists are warm now; a re-fill reuses their capacity.
            FillGrid(grid, 256);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                grid.Move(new UnitId((uint)(i % 256) + 1u), new Vector3(i % 40 - 20f, 0f, i % 13 - 6f));
                results.Clear();
                grid.Query(new Vector3(i % 20 - 10f, 0f, 0f), 3f, results);
            }

            for (uint id = 3u; id <= 130u; id++)
            {
                grid.Remove(new UnitId(id));
            }

            long allocated = allocationCounter.End();

            Assert.That(allocated, Is.EqualTo(0L));
            Assert.That(grid.Remove(new UnitId(3u)), Is.False, "removed ids stay removed until the next fill");
        }

        [Test]
        public void CappedAreaCast_SortingNearestTargets_AllocatesNothingWhenWarm()
        {
            FakeWorldAdapter world = new FakeWorldAdapter();
            AbilitySystem system = new AbilitySystem { World = world };
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, team: 1);
            world.SetPosition(caster.Id, Vector3.zero);
            List<AbilityUnit> enemies = new List<AbilityUnit>();
            for (int i = 0; i < 32; i++)
            {
                AbilityUnit enemy = AbilityTestSupport.CreateUnit(system, team: 2, health: 1000000f);
                world.SetPosition(enemy.Id, new Vector3(32 - i, 0f, i % 3));
                enemies.Add(enemy);
            }

            AbilityBlueprint nova = new AbilityBlueprint
            {
                Id = "gc_capped_nova",
                Targeting = TargetingMode.NoTarget,
                Delivery = AbilityDeliveryType.Instant
            };
            EffectNodeData splash = AbilityTestSupport.DamageNode(1f, AbilityDamageTypes.Pure,
                EffectTargetSelector.AreaAroundCaster);
            splash.Radius = 100f;
            splash.MaxTargets = 3;
            nova.ImpactEffects.Add(splash);
            system.RegisterAbility(nova);
            system.GrantAbility(caster.Id, "gc_capped_nova");
            CastRequest request = CastRequest.NoTarget(caster.Id, "gc_capped_nova");
            Assert.That(system.Cast(request).Success, Is.True);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                system.Cast(request);
            }

            long allocated = allocationCounter.End();

            Assert.That(allocated, Is.EqualTo(0L));
            // WHY: the three nearest enemies are the last three created (x = 1, 2, 3).
            Assert.That(enemies[31].Health, Is.EqualTo(1000000f - (Iterations + 1)).Within(0.01f));
            Assert.That(enemies[29].Health, Is.EqualTo(1000000f - (Iterations + 1)).Within(0.01f));
            Assert.That(enemies[28].Health, Is.EqualTo(1000000f).Within(0.01f));
        }

        [Test]
        public void HubRadiusQueries_LinearGridAndRebuild_AllocateNothingWhenWarm()
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                for (int i = 0; i < 64; i++)
                {
                    rig.AddUnit("QueryUnit" + i, 2, new Vector3(i % 8 * 1.5f, 0f, i / 8 * 1.5f));
                }

                Vector3 center = new Vector3(5f, 0f, 5f);
                List<UnitId> results = new List<UnitId>(128);
                rig.Hub.QueryUnitsInRadius(center, 4f, results);
                int linearHits = results.Count;
                Assert.That(linearHits, Is.GreaterThan(1));

                using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
                allocationCounter.Begin();
                for (int i = 0; i < Iterations; i++)
                {
                    results.Clear();
                    rig.Hub.QueryUnitsInRadius(center, 4f, results);
                }

                long linear = allocationCounter.End();

                rig.Hub.SpatialCellSize = 2f;
                results.Clear();
                rig.Hub.QueryUnitsInRadius(center, 4f, results);
                rig.Hub.InvalidateSpatialCache();
                results.Clear();
                rig.Hub.QueryUnitsInRadius(center, 4f, results);

                allocationCounter.Begin();
                for (int i = 0; i < Iterations; i++)
                {
                    results.Clear();
                    rig.Hub.QueryUnitsInRadius(center, 4f, results);
                }

                long grid = allocationCounter.End();

                allocationCounter.Begin();
                for (int i = 0; i < Iterations; i++)
                {
                    rig.Hub.InvalidateSpatialCache();
                    results.Clear();
                    rig.Hub.QueryUnitsInRadius(center, 4f, results);
                }

                long rebuild = allocationCounter.End();

                UnitId mover = results[0];
                Assert.That(rig.Hub.TryGetPosition(mover, out Vector3 home), Is.True);
                Vector3 away = new Vector3(50f, 0f, 50f);
                rig.Hub.TryMoveUnit(mover, away);
                rig.Hub.TryMoveUnit(mover, home);
                allocationCounter.Begin();
                for (int i = 0; i < Iterations; i++)
                {
                    rig.Hub.TryMoveUnit(mover, (i & 1) == 0 ? away : home);
                    results.Clear();
                    rig.Hub.QueryUnitsInRadius(center, 4f, results);
                }

                long moves = allocationCounter.End();

                Assert.That(linear, Is.EqualTo(0L), "linear scan");
                Assert.That(grid, Is.EqualTo(0L), "grid query");
                Assert.That(rebuild, Is.EqualTo(0L), "grid rebuild after invalidation");
                Assert.That(moves, Is.EqualTo(0L), "TryMoveUnit patching the grid between queries");
                Assert.That(results.Count, Is.EqualTo(linearHits), "the last move put the unit back home");
            }
        }

        [Test]
        public void AutoCasterPass_CastingEveryPass_AllocatesNothingWhenWarm()
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                AbilityUnitBehaviour player = rig.AddUnit("AutoCastPlayer", 1, Vector3.zero);
                AbilityCasterBehaviour caster = player.gameObject.AddComponent<AbilityCasterBehaviour>();
                AbilityAutoCaster autoCaster = player.gameObject.AddComponent<AbilityAutoCaster>();
                AbilityUnitBehaviour enemy = rig.AddUnit("AutoCastEnemy", 2, new Vector3(3f, 0f, 0f), 1000000f);
                rig.AddUnit("AutoCastFarEnemy", 2, new Vector3(6f, 0f, 0f));
                AbilityBlueprint zap = new AbilityBlueprint
                {
                    Id = "gc_auto_zap",
                    Targeting = TargetingMode.Unit,
                    TeamFilter = AbilityTeamFilter.Enemies,
                    Cooldown = 0f,
                    Range = 10f
                };
                zap.ImpactEffects.Add(AbilityTestSupport.DamageNode(1f));
                rig.System.RegisterAbility(zap);
                rig.System.GrantAbility(player.UnitId, "gc_auto_zap");
                autoCaster.CastReadyAbilities();
                autoCaster.CastReadyAbilities();
                Assert.That(caster.LastCastResult.Success, Is.True);

                using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
                allocationCounter.Begin();
                for (int i = 0; i < Iterations; i++)
                {
                    autoCaster.CastReadyAbilities();
                }

                long allocated = allocationCounter.End();

                Assert.That(allocated, Is.EqualTo(0L));
                Assert.That(enemy.CurrentHealth, Is.EqualTo(1000000f - (Iterations + 2)).Within(0.01f));
            }
        }

        [Test]
        public void EventReactions_ThornsOnEveryHit_AllocateNothingWhenWarm()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit attacker = AbilityTestSupport.CreateUnit(system, team: 1, health: 1000000f);
            AbilityUnit defender = AbilityTestSupport.CreateUnit(system, team: 2, health: 1000000f);
            ModifierBlueprint thorns = AbilityTestSupport.Modifier("gc_thorns");
            thorns.EventReactions.Add(new ModifierEventReaction
            {
                EventId = AbilityEvents.TakeDamage,
                TargetEventSource = true,
                Effects = AbilityTestSupport.Nodes(AbilityTestSupport.DamageNode(1f))
            });
            system.Modifiers.Apply(thorns, defender.Id, defender.Id);
            DamageService.ApplyDamage(system, attacker.Id, defender.Id, 2f, AbilityDamageTypes.Pure);
            DamageService.ApplyDamage(system, attacker.Id, defender.Id, 2f, AbilityDamageTypes.Pure);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                DamageService.ApplyDamage(system, attacker.Id, defender.Id, 2f, AbilityDamageTypes.Pure);
            }

            long allocated = allocationCounter.End();

            Assert.That(allocated, Is.EqualTo(0L));
            Assert.That(attacker.Health, Is.EqualTo(1000000f - (Iterations + 2)).Within(0.01f),
                "every hit reflected one thorns damage");
        }

        [Test]
        public void CasterBehaviourBlockedCasts_AllocateNothingWhenWarm()
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                AbilityUnitBehaviour player = rig.AddUnit("BlockedCaster", 1, Vector3.zero);
                AbilityCasterBehaviour caster = player.gameObject.AddComponent<AbilityCasterBehaviour>();
                AbilityBlueprint nova = new AbilityBlueprint
                {
                    Id = "gc_blocked_nova",
                    Targeting = TargetingMode.NoTarget,
                    Cooldown = 100f
                };
                rig.System.RegisterAbility(nova);
                rig.System.GrantAbility(player.UnitId, "gc_blocked_nova");
                Assert.That(caster.TryCast("gc_blocked_nova"), Is.True);
                Assert.That(caster.TryCast("gc_blocked_nova"), Is.False);
                Assert.That(caster.TryCastAtUnit("gc_blocked_nova", null), Is.False);

                using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
                allocationCounter.Begin();
                for (int i = 0; i < Iterations; i++)
                {
                    caster.TryCast("gc_blocked_nova");
                    caster.TryCastAtUnit("gc_blocked_nova", null);
                }

                long allocated = allocationCounter.End();

                // WHY: AI and auto-casters retry blocked casts every frame; failure names are cached strings.
                Assert.That(allocated, Is.EqualTo(0L));
                Assert.That(caster.LastCastResult.Failure, Is.EqualTo(CastFailureReason.InvalidTarget));
            }
        }

        [Test]
        public void ModifierReapplicationAfterExpiry_AllocatesOnlyTheNewInstance()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, team: 1);
            AbilityUnit victim = AbilityTestSupport.CreateUnit(system, team: 2);
            ModifierBlueprint slow = AbilityTestSupport.Modifier("gc_slow", duration: 0.5f)
                .WithProperty(AbilityProperties.MoveSpeed, PropertyOp.Add, -1f);
            for (int i = 0; i < 2; i++)
            {
                system.Modifiers.Apply(slow, caster.Id, victim.Id);
                system.Tick(1f);
            }

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                system.Modifiers.Apply(slow, caster.Id, victim.Id);
                system.Tick(1f);
            }

            long allocated = allocationCounter.End();

            // WHY: the owner list is recycled when the last modifier expires, so a fresh application
            // allocates its ModifierInstance and nothing else.
            Assert.That(allocated, Is.LessThanOrEqualTo((long)Iterations));
            Assert.That(AbilityTestSupport.ActiveModifiers(system, victim.Id), Is.Empty);
        }

        private static void FillGrid(AbilitySpatialGrid grid, int count)
        {
            grid.Clear();
            for (int i = 0; i < count; i++)
            {
                grid.Add(new UnitId((uint)i + 1u), new Vector3(i % 16 * 1.5f - 12f, 0f, i / 16 * 1.5f - 12f));
            }
        }
    }
}
