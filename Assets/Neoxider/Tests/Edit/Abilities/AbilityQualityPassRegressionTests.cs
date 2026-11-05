using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Abilities;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     Regressions for the 2026-11 Abilities quality pass: event-bus publication edge cases, pooled
    ///     effect contexts, per-tick/per-reaction RNG, modifier version cleanup, projectile lifecycle,
    ///     typed caster results and the spatial query grid.
    /// </summary>
    public sealed class AbilityQualityPassRegressionTests
    {
        [Test]
        public void Publish_NullEventId_IsIgnoredWithoutThrowing()
        {
            AbilityEventBus bus = new AbilityEventBus();
            int calls = 0;
            bus.SubscribeAny(args => calls++);

            Assert.DoesNotThrow(() => bus.Publish(default));
            Assert.That(calls, Is.Zero);
        }

        [Test]
        public void Publish_ThrowingHandler_LeavesLaterPublicationsIntact()
        {
            AbilityEventBus bus = new AbilityEventBus();
            UnitId target = new UnitId(3);
            List<string> delivered = new List<string>();
            bus.Subscribe("boom", args => throw new InvalidOperationException("handler failure"));
            bus.SubscribeAny(args => delivered.Add("any:" + args.EventId));
            bus.SubscribeTarget(target, args => delivered.Add("target:" + args.EventId));

            Assert.Throws<InvalidOperationException>(() =>
                bus.Publish(new AbilityEventArgs("boom", target, UnitId.None)));
            bus.Publish(new AbilityEventArgs("ok", target, UnitId.None));

            Assert.That(delivered, Is.EqualTo(new[] { "any:ok", "target:ok" }));
        }

        [Test]
        public void Publish_ThrowingHandler_ResetsTheNestingDepthAndClearsSnapshots()
        {
            AbilityEventBus bus = new AbilityEventBus();
            UnitId target = new UnitId(4);
            int calls = 0;
            Action<AbilityEventArgs> counter = args => calls++;
            bus.Subscribe("boom", args => throw new InvalidOperationException("handler failure"));
            bus.Subscribe("ok", counter);
            bus.SubscribeAny(counter);
            bus.SubscribeTarget(target, counter);
            AbilityEventArgs ok = new AbilityEventArgs("ok", target, UnitId.None);
            AbilityEventArgs boom = new AbilityEventArgs("boom", target, UnitId.None);
            bus.Publish(ok);

            Assert.Throws<InvalidOperationException>(() => bus.Publish(boom));

            Assert.That(SnapshotEntries(bus, "_handlerSnapshots") + SnapshotEntries(bus, "_observerSnapshots"),
                Is.Zero, "a throwing publication must not leave handlers referenced by the snapshot buffers");

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < 50; i++)
            {
                bus.Publish(ok);
            }

            long allocated = allocationCounter.End();

            Assert.That(allocated, Is.EqualTo(0L),
                "a leaked nesting depth would rent (and allocate) a fresh snapshot level on the next publish");
            Assert.That(calls, Is.EqualTo(51 * 3));
        }

        [Test]
        public void NestedPublishInsideSpecificHandler_KeepsTheOuterSpecificBatch()
        {
            AbilityEventBus bus = new AbilityEventBus();
            List<string> delivered = new List<string>();
            bus.Subscribe("outer", args =>
            {
                delivered.Add("outer-1");
                bus.Publish(new AbilityEventArgs("inner", UnitId.None, UnitId.None));
            });
            bus.Subscribe("outer", args => delivered.Add("outer-2"));
            bus.Subscribe("inner", args => delivered.Add("inner-1"));
            bus.Subscribe("inner", args => delivered.Add("inner-2"));

            bus.Publish(new AbilityEventArgs("outer", UnitId.None, UnitId.None));

            Assert.That(delivered, Is.EqualTo(new[] { "outer-1", "inner-1", "inner-2", "outer-2" }));
        }

        [Test]
        public void PooledCastContext_SurvivesNestedReactionContexts()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit attacker = AbilityTestSupport.CreateUnit(system, team: 1, health: 1000f);
            AbilityUnit defender = AbilityTestSupport.CreateUnit(system, team: 2, health: 1000f);
            ModifierBlueprint thorns = AbilityTestSupport.Modifier("pool_thorns");
            thorns.EventReactions.Add(new ModifierEventReaction
            {
                EventId = AbilityEvents.TakeDamage,
                TargetEventSource = true,
                Effects = AbilityTestSupport.Nodes(AbilityTestSupport.DamageNode(1f))
            });
            system.Modifiers.Apply(thorns, defender.Id, defender.Id);
            AbilityBlueprint strike = new AbilityBlueprint
            {
                Id = "pool_strike",
                Targeting = TargetingMode.Unit,
                Delivery = AbilityDeliveryType.Instant
            };
            strike.ImpactEffects.Add(AbilityTestSupport.DamageNode(10f));
            strike.ImpactEffects.Add(AbilityTestSupport.DamageNode(20f));
            system.RegisterAbility(strike);
            system.GrantAbility(attacker.Id, "pool_strike");

            for (int cast = 0; cast < 3; cast++)
            {
                Assert.That(system.Cast(CastRequest.AtUnit(attacker.Id, "pool_strike", defender.Id)).Success,
                    Is.True);
            }

            Assert.That(defender.Health, Is.EqualTo(910f).Within(0.001f),
                "both impact nodes keep hitting the cast target while reactions borrow contexts");
            Assert.That(attacker.Health, Is.EqualTo(994f).Within(0.001f), "one thorns reflection per hit");
        }

        [Test]
        public void KeptPooledContext_IsReleasedAndIgnoredByExecuteEffects()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, team: 1, health: 1000f);
            AbilityUnit target = AbilityTestSupport.CreateUnit(system, team: 2, health: 1000f);
            ContextKeepingOp keeper = new ContextKeepingOp();
            system.Ops.Register(keeper);
            AbilityBlueprint mark = new AbilityBlueprint
            {
                Id = "keep_mark",
                Targeting = TargetingMode.Unit,
                Delivery = AbilityDeliveryType.Instant
            };
            mark.ImpactEffects.Add(new EffectNodeData { OpId = keeper.Id, Target = EffectTargetSelector.Target });
            system.RegisterAbility(mark);
            system.GrantAbility(caster.Id, "keep_mark");
            // WHY: the caster selector still resolves on a recycled context (Caster survives recycling),
            // and the nested child keeps its own target copy, so without the guard both nodes would hit.
            List<EffectNodeData> damage = AbilityTestSupport.Nodes(
                AbilityTestSupport.DamageNode(10f, target: EffectTargetSelector.Caster),
                AbilityTestSupport.DamageNode(10f));

            Assert.That(system.Cast(CastRequest.AtUnit(caster.Id, "keep_mark", target.Id)).Success, Is.True);
            EffectContext kept = keeper.Kept;
            EffectContext keptNested = keeper.KeptNested;

            Assert.That(keeper.ReleasedDuringExecute, Is.False, "a context is live while its effects run");
            Assert.That(kept.IsReleased, Is.True);
            Assert.That(keptNested.IsReleased, Is.True, "a nested child shares the pooled parent's lifetime");
            system.ExecuteEffects(damage, kept);
            system.ExecuteEffects(damage, keptNested);
            Assert.That(caster.Health, Is.EqualTo(1000f));
            Assert.That(target.Health, Is.EqualTo(1000f));

            Assert.That(system.Cast(CastRequest.AtUnit(caster.Id, "keep_mark", target.Id)).Success, Is.True);
            Assert.That(keptNested.IsReleased, Is.True, "re-renting the pooled parent does not revive old children");
            system.ExecuteEffects(damage, keptNested);
            Assert.That(target.Health, Is.EqualTo(1000f));

            EffectContext manual = AbilityTestSupport.Context(system, caster.Id, targets: target.Id);
            Assert.That(manual.IsReleased, Is.False);
            Assert.That(manual.CreateNested(caster.Id, "manual", null).IsReleased, Is.False);
            system.ExecuteEffects(AbilityTestSupport.Nodes(AbilityTestSupport.DamageNode(10f)), manual);
            Assert.That(target.Health, Is.EqualTo(990f).Within(0.001f), "user-built contexts are never released");
        }

        [Test]
        public void RecycledPendingProjectileRecords_RestartTheirHitRollsAndStayUnroutableAfterRelease()
        {
            List<float> fresh = RunProjectileHitRolls(warmPool: false);
            List<float> recycled = RunProjectileHitRolls(warmPool: true);

            Assert.That(fresh, Has.Some.GreaterThan(0f));
            Assert.That(recycled, Is.EqualTo(fresh), "a recycled pending record restarts its per-hit seed sequence");
        }

        [Test]
        public void ModifierTickChance_RollsIndependentlyPerTickAndStaysDeterministic()
        {
            float first = RunChanceDot();
            float second = RunChanceDot();

            Assert.That(first, Is.GreaterThan(0f).And.LessThan(40f),
                "a 50% tick node must neither fire on every tick nor on none");
            Assert.That(second, Is.EqualTo(first), "identical tick sequences roll identically");
        }

        [Test]
        public void ModifierReactionChance_RollsIndependentlyPerReactionAndStaysDeterministic()
        {
            float first = RunChanceThorns();
            float second = RunChanceThorns();

            Assert.That(first, Is.GreaterThan(0f).And.LessThan(40f),
                "a 50% reaction node must neither fire on every event nor on none");
            Assert.That(second, Is.EqualTo(first), "identical event sequences roll identically");
        }

        [Test]
        public void DestroyUnit_ForgetsTheModifierVersionOfChurnedUnits()
        {
            AbilitySystem system = new AbilitySystem();
            system.RegisterModifier(AbilityTestSupport.Modifier("churn_buff")
                .WithProperty(AbilityProperties.Armor, PropertyOp.Add, 1f));
            AbilityUnit survivor = AbilityTestSupport.CreateUnit(system);
            system.ApplyModifier("churn_buff", survivor.Id, survivor.Id);
            int baseline = system.Modifiers.TrackedOwnerCount;

            for (int i = 0; i < 200; i++)
            {
                AbilityUnit unit = AbilityTestSupport.CreateUnit(system, team: 2);
                system.ApplyModifier("churn_buff", unit.Id, unit.Id);
                system.DestroyUnit(unit.Id);
            }

            Assert.That(system.Modifiers.TrackedOwnerCount, Is.EqualTo(baseline));
            Assert.That(survivor.GetProperty(AbilityProperties.Armor), Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void ForgetOwner_KeepsLiveOwnersAndPropertyCachesStayCorrect()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit unit = AbilityTestSupport.CreateUnit(system);
            ModifierBlueprint armor = AbilityTestSupport.Modifier("cache_armor")
                .WithProperty(AbilityProperties.Armor, PropertyOp.Add, 5f);

            ModifierInstance applied = system.Modifiers.Apply(armor, unit.Id, unit.Id).Instance;
            system.Modifiers.ForgetOwner(unit.Id);
            Assert.That(system.Modifiers.VersionOf(unit.Id), Is.Not.Zero, "owners with live modifiers are kept");
            Assert.That(unit.GetProperty(AbilityProperties.Armor), Is.EqualTo(5f).Within(0.0001f));

            system.Modifiers.Remove(applied);
            Assert.That(unit.GetProperty(AbilityProperties.Armor), Is.Zero);
            system.Modifiers.ForgetOwner(unit.Id);
            Assert.That(system.Modifiers.VersionOf(unit.Id), Is.Zero);
            Assert.That(unit.GetProperty(AbilityProperties.Armor), Is.Zero);

            system.Modifiers.Apply(armor, unit.Id, unit.Id);
            Assert.That(unit.GetProperty(AbilityProperties.Armor), Is.EqualTo(5f).Within(0.0001f),
                "a re-applied modifier after ForgetOwner must invalidate the cached value");
        }

        [Test]
        public void RemoveById_IsCaseInsensitiveAndIgnoresEmptyIds()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit unit = AbilityTestSupport.CreateUnit(system);
            system.Modifiers.Apply(AbilityTestSupport.Modifier("Frost", policy: ModifierStackPolicy.Independent),
                unit.Id, unit.Id);
            system.Modifiers.Apply(AbilityTestSupport.Modifier("Frost", policy: ModifierStackPolicy.Independent),
                unit.Id, unit.Id);

            Assert.That(system.Modifiers.RemoveById(unit.Id, null), Is.Zero);
            Assert.That(system.Modifiers.RemoveById(unit.Id, string.Empty), Is.Zero);
            Assert.That(system.Modifiers.RemoveById(unit.Id, "frost", allInstances: false), Is.EqualTo(1));
            Assert.That(system.Modifiers.RemoveById(unit.Id, "FROST"), Is.EqualTo(1));
            Assert.That(AbilityTestSupport.ActiveModifiers(system, unit.Id), Is.Empty);
        }

        [Test]
        public void Projectile_ExternalDisable_ReleasesItsPendingCastOnce()
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                AbilityUnitBehaviour owner = rig.AddUnit("Owner", 1, Vector3.zero);
                AbilityUnitBehaviour enemy = rig.AddUnit("Enemy", 2, new Vector3(5f, 0f, 0f));
                AbilityBlueprint bolt = new AbilityBlueprint
                {
                    Id = "disable_bolt",
                    Targeting = TargetingMode.Unit,
                    Delivery = AbilityDeliveryType.Projectile,
                    ProjectileArchetypeId = "unbound_bolt"
                };
                bolt.ImpactEffects.Add(AbilityTestSupport.DamageNode(10f));
                rig.System.RegisterAbility(bolt);
                rig.System.GrantAbility(owner.UnitId, "disable_bolt");
                CastResult cast = rig.System.Cast(CastRequest.AtUnit(owner.UnitId, "disable_bolt", enemy.UnitId));
                Assert.That(cast.Success, Is.True);

                AbilityProjectileBehaviour projectile = rig.Own(new GameObject("Projectile"))
                    .AddComponent<AbilityProjectileBehaviour>();
                projectile.OnSpawned(new SpawnRequest("unbound_bolt", owner.UnitId, Vector3.zero, Vector3.right,
                    enemy.UnitId, "disable_bolt", 0f, cast.CastId), rig.Hub);
                Assert.That(projectile.IsFlying, Is.True);
                Assert.That(projectile.CastId, Is.EqualTo(cast.CastId));

                AbilitySceneRig.CallPrivate(projectile, "OnDisable");
                AbilitySceneRig.CallPrivate(projectile, "OnDisable");

                Assert.That(projectile.IsFlying, Is.False);
                Assert.That(rig.System.NotifyProjectileHit(cast.CastId, enemy.UnitId, Vector3.zero), Is.False,
                    "an externally disabled projectile must not keep its cast routable until the timeout");
                Assert.That(enemy.Unit.Health, Is.EqualTo(100f));
            }
        }

        [Test]
        public void Projectile_HubDestroyedMidFlight_StopsFlyingAndLeavesThroughThePool()
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                AbilitySystemBehaviour doomedHub = rig.Own(new GameObject("DoomedHub"))
                    .AddComponent<AbilitySystemBehaviour>();
                AbilityProjectileBehaviour projectile = rig.Own(new GameObject("OrphanProjectile"))
                    .AddComponent<AbilityProjectileBehaviour>();
                projectile.OnSpawned(new SpawnRequest("orphan_bolt", UnitId.None, Vector3.zero, Vector3.right,
                    UnitId.None, "orphan", 0f, 1u), doomedHub);
                Assert.That(projectile.IsFlying, Is.True);

                UnityEngine.Object.DestroyImmediate(doomedHub.gameObject);
                AbilitySceneRig.CallPrivate(projectile, "Update");

                Assert.That(projectile.IsFlying, Is.False);
                // WHY: this instance is not from a pool, so PoolManager.Release destroys it; a pooled one
                // would be deactivated and returned instead of staying active forever.
                Assert.That(projectile == null, Is.True, "the orphaned projectile went through PoolManager.Release");
            }
        }

        [Test]
        public void Projectile_DisableBeforeSpawn_IsNoOp()
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                AbilityProjectileBehaviour projectile = rig.Own(new GameObject("IdleProjectile"))
                    .AddComponent<AbilityProjectileBehaviour>();

                Assert.DoesNotThrow(() => AbilitySceneRig.CallPrivate(projectile, "OnDisable"));
                Assert.That(projectile.IsFlying, Is.False);
            }
        }

        [Test]
        public void CasterCast_ReturnsTheTypedResultAndRecordsItAsLastCastResult()
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                AbilityUnitBehaviour unit = rig.AddUnit("TypedCaster", 1, Vector3.zero);
                AbilityCasterBehaviour caster = unit.gameObject.AddComponent<AbilityCasterBehaviour>();
                List<string> failures = new List<string>();
                caster.OnCastFailed.AddListener(failures.Add);

                CastResult missing = caster.Cast(CastRequest.NoTarget(UnitId.None, "typed_missing"));

                Assert.That(missing.Success, Is.False);
                Assert.That(missing.Failure, Is.EqualTo(CastFailureReason.UnknownAbility));
                Assert.That(caster.LastCastResult.Failure, Is.EqualTo(CastFailureReason.UnknownAbility));
                Assert.That(failures, Is.EqualTo(new[] { nameof(CastFailureReason.UnknownAbility) }));

                rig.System.RegisterAbility(new AbilityBlueprint { Id = "typed_self", Targeting = TargetingMode.Self });
                rig.System.GrantAbility(unit.UnitId, "typed_self");
                CastResult success = caster.Cast(CastRequest.NoTarget(new UnitId(999), "typed_self"));

                Assert.That(success.Success, Is.True, "the request caster is replaced by this component's unit");
                Assert.That(success.CastId, Is.Not.Zero);
                Assert.That(caster.LastCastResult.CastId, Is.EqualTo(success.CastId));
                Assert.That(caster.TryCastAtUnit("typed_self", null), Is.False);
                Assert.That(caster.LastCastResult.Failure, Is.EqualTo(CastFailureReason.InvalidTarget));
            }
        }

        [Test]
        public void CastFailureReasonNames_MatchEnumToStringForEveryValue()
        {
            Type namesType = typeof(AbilitySystem).Assembly.GetType("Neo.Abilities.CastFailureReasonNames");
            Assert.That(namesType, Is.Not.Null);
            MethodInfo of = namesType.GetMethod("Of", BindingFlags.Public | BindingFlags.Static);
            Assert.That(of, Is.Not.Null);

            foreach (CastFailureReason reason in Enum.GetValues(typeof(CastFailureReason)))
            {
                Assert.That(of.Invoke(null, new object[] { reason }), Is.EqualTo(reason.ToString()));
            }

            Assert.That(of.Invoke(null, new object[] { (CastFailureReason)999 }), Is.EqualTo("999"));
        }

        [Test]
        public void AbilityIdComparer_MatchesOrdinalIgnoreCaseAndHashesEqualIdsAlike()
        {
            // WHY: the fast comparer replaced StringComparer.OrdinalIgnoreCase in the domain's id dictionaries; any
            // disagreement would make an authored id resolve differently than before.
            Type comparerType = typeof(AbilitySystem).Assembly.GetType("Neo.Abilities.AbilityIdComparer");
            Assert.That(comparerType, Is.Not.Null);
            FieldInfo instanceField = comparerType.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
            Assert.That(instanceField, Is.Not.Null);
            IEqualityComparer<string> comparer = (IEqualityComparer<string>)instanceField.GetValue(null);
            StringComparer reference = StringComparer.OrdinalIgnoreCase;

            string[] ids =
            {
                "health_regen", "HEALTH_REGEN", "Health_Regen", "health_regex", "mana", "MANA", "mana ", "", "a", "A",
                "z", "Z", "@", "`", "[", "{", "straße", "STRASSE", "Ärger", "ärger", "ı", "I", "i",
                "İ", "Привет", "ПРИВЕТ", "ǅ",
                "ǆ", "Ǆ", "µ", "Μ", "x́", "𐐀", "𐐨"
            };
            for (int a = 0; a < ids.Length; a++)
            {
                for (int b = 0; b < ids.Length; b++)
                {
                    bool expected = reference.Equals(ids[a], ids[b]);
                    Assert.That(comparer.Equals(ids[a], ids[b]), Is.EqualTo(expected), $"'{ids[a]}' vs '{ids[b]}'");
                    if (expected)
                    {
                        Assert.That(comparer.GetHashCode(ids[a]), Is.EqualTo(comparer.GetHashCode(ids[b])),
                            $"equal ids '{ids[a]}' and '{ids[b]}' must hash alike");
                    }
                }
            }

            int mismatches = 0;
            string firstMismatch = null;
            for (int code = 0; code <= char.MaxValue; code++)
            {
                string unit = ((char)code).ToString();
                string upper = char.ToUpperInvariant((char)code).ToString();
                string lower = char.ToLowerInvariant((char)code).ToString();
                bool upperEqual = comparer.Equals(unit, upper);
                bool lowerEqual = comparer.Equals(unit, lower);
                bool hashesAgree = (!upperEqual || comparer.GetHashCode(unit) == comparer.GetHashCode(upper)) &&
                                   (!lowerEqual || comparer.GetHashCode(unit) == comparer.GetHashCode(lower));
                if (upperEqual != reference.Equals(unit, upper) || lowerEqual != reference.Equals(unit, lower) ||
                    !hashesAgree)
                {
                    mismatches++;
                    firstMismatch ??= "U+" + code.ToString("X4");
                }
            }

            Assert.That(mismatches, Is.Zero, $"every UTF-16 unit against its invariant case forms; first: {firstMismatch}");
            Assert.That(comparer.Equals(null, null), Is.True);
            Assert.That(comparer.Equals(null, "a"), Is.False);
            Assert.That(comparer.Equals("a", null), Is.False);
        }

        [Test]
        public void SpatialGrid_MatchesALinearScanInInsertionOrder()
        {
            AbilitySpatialGrid grid = new AbilitySpatialGrid(2f);
            List<UnitId> ids = new List<UnitId>();
            List<Vector3> positions = new List<Vector3>();
            XorShiftRandom random = new XorShiftRandom(1234u);
            for (uint i = 1; i <= 300; i++)
            {
                Vector3 position = new Vector3(random.NextFloat() * 60f - 30f, random.NextFloat() * 6f - 3f,
                    random.NextFloat() * 60f - 30f);
                ids.Add(new UnitId(i));
                positions.Add(position);
                grid.Add(new UnitId(i), position);
            }

            float[] radii =
            {
                0f, 0.5f, 2f, 3.7f, -2.5f, 11f, 100f, float.PositiveInfinity, float.MaxValue, 1e10f,
                -float.MaxValue
            };
            List<UnitId> expected = new List<UnitId>();
            List<UnitId> actual = new List<UnitId>();
            for (int q = 0; q < 120; q++)
            {
                Vector3 point = new Vector3(random.NextFloat() * 70f - 35f, random.NextFloat() * 8f - 4f,
                    random.NextFloat() * 70f - 35f);
                float radius = radii[q % radii.Length];
                expected.Clear();
                for (int i = 0; i < ids.Count; i++)
                {
                    if (ExactlyWithin(positions[i], point, radius))
                    {
                        expected.Add(ids[i]);
                    }
                }

                actual.Clear();
                grid.Query(point, radius, actual);
                Assert.That(actual, Is.EqualTo(expected), $"query {q} at {point} radius {radius}");
            }

            Assert.That(grid.Count, Is.EqualTo(300));
            grid.Clear();
            actual.Clear();
            grid.Query(Vector3.zero, 100f, actual);
            Assert.That(actual, Is.Empty);
        }

        [Test]
        public void SpatialGrid_FarCoordinatesAndHugeRadii_MatchALinearScan()
        {
            // WHY: cell indices beyond the int range used to convert platform-dependently (x64 gave
            // int.MinValue and emptied the cell range), so float.MaxValue radii found nothing in grid mode.
            // The oracle is the exact (double) distance: a float `radius * radius` overflows to infinity for
            // 3e38, and whether it does depends on the JIT keeping the product in double, which made the old
            // float oracle disagree with the grid on Mono.
            AbilitySpatialGrid grid = new AbilitySpatialGrid(0.5f);
            Vector3[] positions =
            {
                Vector3.zero,
                new Vector3(1f, 0f, 1f),
                new Vector3(1e9f, 0f, 0f),
                new Vector3(1e12f, 0f, 0f),
                new Vector3(1.0000001e12f, 0f, 0f),
                new Vector3(-1e12f, 5f, -1e12f),
                new Vector3(float.MaxValue, 0f, 0f),
                new Vector3(0f, -float.MaxValue, 0f),
                new Vector3(float.NaN, 0f, 0f)
            };
            for (int i = 0; i < positions.Length; i++)
            {
                grid.Add(new UnitId((uint)i + 1u), positions[i]);
            }

            Vector3[] points =
            {
                Vector3.zero,
                new Vector3(1e12f, 0f, 0f),
                new Vector3(-1e12f, 5f, -1e12f),
                new Vector3(1e9f, 0f, 0f),
                new Vector3(float.MaxValue, 0f, 0f),
                new Vector3(-float.MaxValue, 0f, 0f),
                new Vector3(float.NaN, 0f, 0f)
            };
            float[] radii = { 0f, 1f, 1e5f, 1e10f, 1e13f, 3e38f, float.MaxValue, float.PositiveInfinity };
            List<UnitId> expected = new List<UnitId>();
            List<UnitId> actual = new List<UnitId>();
            for (int p = 0; p < points.Length; p++)
            {
                for (int r = 0; r < radii.Length; r++)
                {
                    float radius = radii[r];
                    expected.Clear();
                    for (int i = 0; i < positions.Length; i++)
                    {
                        bool within = ExactlyWithin(positions[i], points[p], radius);
                        if (within)
                        {
                            expected.Add(new UnitId((uint)i + 1u));
                        }

                        Assert.That(AbilitySpatialGrid.IsWithinRadius(positions[i], points[p], radius),
                            Is.EqualTo(within), $"IsWithinRadius unit {i + 1} point {points[p]} radius {radius}");
                    }

                    actual.Clear();
                    grid.Query(points[p], radius, actual);
                    Assert.That(actual, Is.EqualTo(expected), $"point {points[p]} radius {radius}");
                }
            }

            actual.Clear();
            grid.Query(Vector3.zero, float.MaxValue, actual);
            Assert.That(actual, Has.Member(new UnitId(1u)).And.Member(new UnitId(3u)),
                "an 'everything' radius must not come back empty");
            Assert.That(actual, Has.Member(new UnitId(7u)).And.Member(new UnitId(8u)),
                "units exactly float.MaxValue away are inside a float.MaxValue radius");

            actual.Clear();
            grid.Query(Vector3.zero, 3e38f, actual);
            Assert.That(actual, Has.No.Member(new UnitId(7u)).And.No.Member(new UnitId(8u)),
                "units float.MaxValue away are outside radius 3e38 even though float squares overflow");
        }

        [Test]
        public void SpatialGrid_MoveAndRemove_MatchARefilledGrid()
        {
            // WHY: in-place updates must be indistinguishable from refilling the grid with the live entries in
            // insertion order, including moves into far cells, adds after the id index exists and re-fills.
            const int unitCount = 40;
            const int lateUnit = unitCount;
            XorShiftRandom random = new XorShiftRandom(11u);
            AbilitySpatialGrid grid = new AbilitySpatialGrid(1.5f);
            AbilitySpatialGrid refilled = new AbilitySpatialGrid(1.5f);
            Vector3[] positions = new Vector3[unitCount + 1];
            bool[] live = new bool[unitCount + 1];
            for (int i = 0; i < unitCount; i++)
            {
                positions[i] = GridTestPoint(random);
                live[i] = true;
                Assert.That(grid.Add(new UnitId((uint)i + 1u), positions[i]), Is.EqualTo(i), "Add returns the index");
            }

            List<UnitId> actual = new List<UnitId>();
            List<UnitId> expected = new List<UnitId>();
            for (int step = 0; step < 300; step++)
            {
                if (step == 150)
                {
                    positions[lateUnit] = GridTestPoint(random);
                    live[lateUnit] = true;
                    Assert.That(grid.Add(new UnitId((uint)lateUnit + 1u), positions[lateUnit]), Is.EqualTo(unitCount));
                }

                int unit = random.NextInt(step >= 150 ? unitCount + 1 : unitCount);
                UnitId id = new UnitId((uint)unit + 1u);
                if (random.NextFloat() < 0.06f)
                {
                    Assert.That(grid.Remove(id), Is.EqualTo(live[unit]), $"step {step}: remove unit {unit + 1}");
                    live[unit] = false;
                }
                else
                {
                    Vector3 position = step % 37 == 0 ? new Vector3(1e12f, -3f, -1e12f) : GridTestPoint(random);
                    Assert.That(grid.Move(id, position), Is.EqualTo(live[unit]), $"step {step}: move unit {unit + 1}");
                    if (live[unit])
                    {
                        positions[unit] = position;
                    }
                }

                refilled.Clear();
                for (int i = 0; i <= unitCount; i++)
                {
                    if (live[i])
                    {
                        refilled.Add(new UnitId((uint)i + 1u), positions[i]);
                    }
                }

                Vector3 center = step % 37 == 1 ? new Vector3(1e12f, 0f, -1e12f) : GridTestPoint(random);
                float radius = step % 11 == 0 ? 1e13f : random.NextFloat() * 5f;
                actual.Clear();
                expected.Clear();
                grid.Query(center, radius, actual);
                refilled.Query(center, radius, expected);
                Assert.That(actual, Is.EqualTo(expected), $"step {step}: point {center} radius {radius}");
            }

            Assert.That(grid.Count, Is.EqualTo(unitCount + 1), "removed entries keep their slot until Clear");
            grid.Clear();
            Assert.That(grid.Move(new UnitId(1u), Vector3.zero), Is.False, "Clear forgets every entry");
            Assert.That(grid.Remove(new UnitId(1u)), Is.False, "Clear forgets every entry");
            grid.Add(new UnitId(1u), new Vector3(50f, 0f, 50f));
            Assert.That(grid.Move(new UnitId(1u), Vector3.zero), Is.True, "a re-filled id can move again");
            actual.Clear();
            grid.Query(Vector3.zero, 0.5f, actual);
            Assert.That(actual, Is.EqualTo(new[] { new UnitId(1u) }));
            grid.CellSize = 3f;
            Assert.That(grid.Move(new UnitId(1u), Vector3.zero), Is.False, "a new cell size clears the grid");
        }

        [Test]
        public void DirectionProjectile_SpawnAimReachesTheCastTargetPoint()
        {
            // WHY: the scene projectile detonates at Position + Direction; a unit-length aim vector used to
            // end skillshots one world unit after launch.
            FakeWorldAdapter world = new FakeWorldAdapter();
            AbilitySystem system = new AbilitySystem { World = world };
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, team: 1);
            Vector3 origin = new Vector3(1f, 0f, 2f);
            world.SetPosition(caster.Id, origin);
            system.RegisterAbility(Skillshot("ranged_shot", 12f));
            system.RegisterAbility(Skillshot("open_shot", 0f));
            system.GrantAbility(caster.Id, "ranged_shot");
            system.GrantAbility(caster.Id, "open_shot");

            Assert.That(system.Cast(CastRequest.Towards(caster.Id, "ranged_shot", Vector3.right)).Success, Is.True);
            Assert.That(system.Cast(CastRequest.Towards(caster.Id, "open_shot", new Vector3(0f, 0f, 0.25f))).Success,
                Is.True);
            Assert.That(system.Cast(CastRequest.Towards(caster.Id, "ranged_shot", Vector3.zero)).Success, Is.True);

            Vector3[] expectedAims =
            {
                new Vector3(12f, 0f, 0f),
                new Vector3(0f, 0f, AbilitySystem.DefaultDirectionReach),
                new Vector3(0f, 0f, 12f)
            };
            Assert.That(world.Spawns.Count, Is.EqualTo(expectedAims.Length * 2));
            for (int i = 0; i < expectedAims.Length; i++)
            {
                SpawnRequest marker = world.Spawns[i * 2];
                SpawnRequest projectile = world.Spawns[i * 2 + 1];
                Assert.That(marker.ArchetypeId, Is.EqualTo("aim_marker"));
                Assert.That(projectile.ArchetypeId, Is.EqualTo("shot_projectile"));
                Assert.That(Vector3.Distance(projectile.Direction, expectedAims[i]), Is.LessThan(1e-4f), $"cast {i}");
                Assert.That(Vector3.Distance(marker.Position, origin + projectile.Direction), Is.LessThan(1e-4f),
                    $"cast {i}: the projectile ends at the cast's own target point");
            }
        }

        [Test]
        public void HubLinearAndGridQueries_AgreeOnFarUnitsAndHugeRadii()
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                AbilityUnitBehaviour near = rig.AddUnit("Near", 2, Vector3.zero);
                AbilityUnitBehaviour far = rig.AddUnit("Far", 2, new Vector3(1e20f, 0f, 0f));
                List<UnitId> linear = new List<UnitId>();
                List<UnitId> gridded = new List<UnitId>();
                float[] radii = { 1f, 1e19f, 1e20f, 3e38f, float.MaxValue, float.PositiveInfinity };
                for (int r = 0; r < radii.Length; r++)
                {
                    rig.Hub.SpatialCellSize = 0f;
                    linear.Clear();
                    rig.Hub.QueryUnitsInRadius(Vector3.zero, radii[r], linear);
                    rig.Hub.SpatialCellSize = 2f;
                    gridded.Clear();
                    rig.Hub.QueryUnitsInRadius(Vector3.zero, radii[r], gridded);

                    Assert.That(gridded, Is.EqualTo(linear), $"radius {radii[r]}");
                    Assert.That(linear, Has.Member(near.UnitId), $"radius {radii[r]}");
                    // WHY: float squares of 1e20 overflow, so a float test would miss the far unit at 3e38.
                    Assert.That(linear.Contains(far.UnitId), Is.EqualTo(radii[r] >= 1e20f), $"radius {radii[r]}");
                }

                rig.Hub.SpatialCellSize = 0f;
                Assert.DoesNotThrow(() => rig.Hub.QueryUnitsInRadius(Vector3.zero, 1f, null), "linear mode");
                rig.Hub.SpatialCellSize = 2f;
                Assert.DoesNotThrow(() => rig.Hub.QueryUnitsInRadius(Vector3.zero, 1f, null), "grid mode");
            }
        }

        [Test]
        public void DirectionReach_FollowsLongAimsAndTheCastRangeBonus()
        {
            // WHY: Range 0 means unlimited, so an aim vector longer than DefaultDirectionReach (towards a target
            // or the cursor) must arrive there; a positive Range reaches Range + cast_range_bonus, the distance
            // the range check and AbilityAutoCaster use.
            FakeWorldAdapter world = new FakeWorldAdapter();
            AbilitySystem system = new AbilitySystem { World = world };
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, team: 1);
            Vector3 origin = new Vector3(-2f, 1f, 3f);
            world.SetPosition(caster.Id, origin);
            caster.SetBaseProperty(AbilityProperties.CastRangeBonus, 4f);
            system.RegisterAbility(Skillshot("open_shot", 0f));
            system.RegisterAbility(Skillshot("bonus_shot", 8f));
            system.GrantAbility(caster.Id, "open_shot");
            system.GrantAbility(caster.Id, "bonus_shot");

            Vector3[] aims =
            {
                new Vector3(0f, 0f, 15f),
                new Vector3(3f, 0f, 4f),
                new Vector3(1e20f, 0f, 0f),
                Vector3.forward,
                new Vector3(30f, 0f, 0f)
            };
            string[] abilities = { "open_shot", "open_shot", "open_shot", "bonus_shot", "bonus_shot" };
            Vector3[] expectedReach =
            {
                new Vector3(0f, 0f, 15f),
                new Vector3(6f, 0f, 8f),
                new Vector3(1e20f, 0f, 0f),
                new Vector3(0f, 0f, 12f),
                new Vector3(12f, 0f, 0f)
            };
            for (int i = 0; i < aims.Length; i++)
            {
                Assert.That(system.Cast(CastRequest.Towards(caster.Id, abilities[i], aims[i])).Success, Is.True,
                    $"cast {i}");
            }

            Assert.That(world.Spawns.Count, Is.EqualTo(aims.Length * 2));
            for (int i = 0; i < aims.Length; i++)
            {
                SpawnRequest marker = world.Spawns[i * 2];
                SpawnRequest projectile = world.Spawns[i * 2 + 1];
                float tolerance = expectedReach[i].magnitude * 1e-6f + 1e-4f;
                Assert.That(Vector3.Distance(projectile.Direction, expectedReach[i]), Is.LessThan(tolerance),
                    $"cast {i}: {projectile.Direction}");
                Assert.That(Vector3.Distance(marker.Position, origin + projectile.Direction), Is.LessThan(tolerance),
                    $"cast {i}: the projectile ends at the cast's own target point");
            }
        }

        [Test]
        public void AutoCaster_DirectionProjectile_ReachesItsUnlimitedRangeTarget()
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                AbilityUnitBehaviour player = rig.AddUnit("SkillshotPlayer", 1, new Vector3(1f, 0f, -2f));
                player.gameObject.AddComponent<AbilityCasterBehaviour>();
                AbilityAutoCaster autoCaster = player.gameObject.AddComponent<AbilityAutoCaster>();
                AbilityUnitBehaviour enemy = rig.AddUnit("FarEnemy", 2, new Vector3(1f, 0f, 13f));
                RecordingWorldAdapter world = new RecordingWorldAdapter(rig.Hub);
                rig.System.World = world;
                AbilityBlueprint skillshot = Skillshot("auto_open_shot", 0f);
                skillshot.CastEffects.Clear();
                skillshot.TeamFilter = AbilityTeamFilter.Enemies;
                rig.System.RegisterAbility(skillshot);
                rig.System.GrantAbility(player.UnitId, skillshot.Id);

                autoCaster.CastReadyAbilities();

                Assert.That(world.Spawns.Count, Is.EqualTo(1), "the auto-caster fired one skillshot");
                SpawnRequest spawn = world.Spawns[0];
                Assert.That(spawn.ArchetypeId, Is.EqualTo("shot_projectile"));
                Assert.That(Vector3.Distance(spawn.Position + spawn.Direction, enemy.transform.position),
                    Is.LessThan(1e-3f), "a Range 0 skillshot 15 units away must reach its target, not stop at 10");
            }
        }

        [Test]
        public void AutoCaster_DirectionProjectile_HonoursTheCastRangeBonus()
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                AbilityUnitBehaviour player = rig.AddUnit("BonusPlayer", 1, Vector3.zero);
                player.gameObject.AddComponent<AbilityCasterBehaviour>();
                AbilityAutoCaster autoCaster = player.gameObject.AddComponent<AbilityAutoCaster>();
                AbilityUnitBehaviour enemy = rig.AddUnit("BonusEnemy", 2, new Vector3(11f, 0f, 0f));
                player.Unit.SetBaseProperty(AbilityProperties.CastRangeBonus, 4f);
                RecordingWorldAdapter world = new RecordingWorldAdapter(rig.Hub);
                rig.System.World = world;
                AbilityBlueprint skillshot = Skillshot("auto_bonus_shot", 8f);
                skillshot.CastEffects.Clear();
                skillshot.TeamFilter = AbilityTeamFilter.Enemies;
                rig.System.RegisterAbility(skillshot);
                rig.System.GrantAbility(player.UnitId, skillshot.Id);

                autoCaster.CastReadyAbilities();

                Assert.That(world.Spawns.Count, Is.EqualTo(1), "the bonus brings the enemy at 11 into range");
                Vector3 aim = world.Spawns[0].Direction;
                Assert.That(aim.magnitude, Is.EqualTo(12f).Within(1e-4f), "Range 8 + cast_range_bonus 4");
                Assert.That(Vector3.Dot(aim.normalized, Vector3.right), Is.GreaterThan(0.9999f));
                Assert.That(aim.magnitude, Is.GreaterThanOrEqualTo(enemy.transform.position.magnitude),
                    "the skillshot flies at least as far as the target the auto-caster picked");
            }
        }

        [Test]
        public void CastRangeCheck_UsesTheExactRadiusTestOfTheQueries()
        {
            FakeWorldAdapter world = new FakeWorldAdapter();
            AbilitySystem system = new AbilitySystem { World = world };
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, team: 1);
            AbilityUnit far = AbilityTestSupport.CreateUnit(system, team: 2);
            world.SetPosition(caster.Id, Vector3.zero);
            world.SetPosition(far.Id, new Vector3(1e20f, 0f, 0f));
            AbilityBlueprint huge = new AbilityBlueprint
            {
                Id = "huge_reach",
                Targeting = TargetingMode.Unit,
                Range = 3e20f
            };
            AbilityBlueprint shrunk = new AbilityBlueprint
            {
                Id = "shrunk_reach",
                Targeting = TargetingMode.Point,
                Range = 2f
            };
            system.RegisterAbility(huge);
            system.RegisterAbility(shrunk);
            system.GrantAbility(caster.Id, huge.Id);
            system.GrantAbility(caster.Id, shrunk.Id);

            Assert.That(AbilitySpatialGrid.IsWithinRadius(new Vector3(1e20f, 0f, 0f), Vector3.zero, 3e20f), Is.True);
            Assert.That(system.Cast(CastRequest.AtUnit(caster.Id, huge.Id, far.Id)).Success, Is.True,
                "a unit the exact query admits is in range for the cast too");

            caster.SetBaseProperty(AbilityProperties.CastRangeBonus, -3f);
            Assert.That(system.Cast(CastRequest.AtPoint(caster.Id, shrunk.Id, Vector3.zero)).Failure,
                Is.EqualTo(CastFailureReason.OutOfRange), "a negative effective range reaches nothing");
            caster.SetBaseProperty(AbilityProperties.CastRangeBonus, 0f);
            Assert.That(system.Cast(CastRequest.AtPoint(caster.Id, shrunk.Id, new Vector3(2f, 0f, 0f))).Success,
                Is.True, "the range boundary is inclusive");
        }

        [Test]
        public void UnitDisable_AfterHubReplacement_LeavesTheReplacementHubsUnitAlone()
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                AbilityUnitBehaviour outliving = rig.AddUnit("OutlivingUnit", 1, Vector3.zero);
                AbilitySystemBehaviour replacement = rig.Own(new GameObject("ReplacementHub"))
                    .AddComponent<AbilitySystemBehaviour>();
                try
                {
                    AbilitySceneRig.SetCurrentHub(replacement);
                    AbilityUnitBehaviour resident = rig.AddUnit("ResidentUnit", 2, Vector3.right);
                    Assert.That(resident.Unit.System, Is.SameAs(replacement.System));
                    Assert.That(resident.UnitId, Is.EqualTo(outliving.UnitId),
                        "precondition: unit ids restart per system, so both units share an id value");
                    int damagedEvents = 0;
                    resident.OnDamaged.AddListener(amount => damagedEvents++);

                    AbilitySceneRig.CallPrivate(outliving, "OnDisable");

                    Assert.That(outliving.Unit, Is.Null);
                    Assert.That(replacement.GetBehaviour(resident.UnitId), Is.SameAs(resident),
                        "the outliving unit must not unregister the replacement hub's unit with the same id");
                    Assert.That(replacement.System.GetUnit(resident.UnitId), Is.SameAs(resident.Unit),
                        "the outliving unit must not destroy the replacement hub's unit with the same id");
                    resident.ApplyDamage(10f);
                    Assert.That(damagedEvents, Is.EqualTo(1), "the resident's target subscription still delivers");
                    Assert.That(resident.CurrentHealth, Is.EqualTo(90f).Within(0.001f));
                }
                finally
                {
                    AbilitySceneRig.SetCurrentHub(rig.Hub);
                }

                AbilityUnitBehaviour own = rig.AddUnit("OwnHubUnit", 1, Vector3.zero);
                UnitId ownId = own.UnitId;
                AbilitySceneRig.CallPrivate(own, "OnDisable");

                Assert.That(rig.Hub.GetBehaviour(ownId), Is.Null, "a unit of the current hub still unregisters");
                Assert.That(rig.System.GetUnit(ownId), Is.Null, "a unit of the current hub is still destroyed");
            }
        }

        [Test]
        public void ShieldedHits_LeaveThePipelineShieldScratchEmpty()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit first = AbilityTestSupport.CreateUnit(system, team: 2, health: 100f);
            AbilityUnit second = AbilityTestSupport.CreateUnit(system, team: 2, health: 100f);
            ModifierBlueprint shield = AbilityTestSupport.Modifier("scratch_shield")
                .WithProperty(AbilityProperties.ShieldHp, PropertyOp.Add, 50f);
            system.Modifiers.Apply(shield, first.Id, first.Id);
            system.Modifiers.Apply(shield, second.Id, second.Id);
            bool nested = false;
            system.Events.Subscribe(AbilityEvents.ShieldAbsorbed, args =>
            {
                if (!nested && args.Target == first.Id)
                {
                    nested = true;
                    DamageService.ApplyDamage(system, UnitId.None, second.Id, 5f, AbilityDamageTypes.Physical);
                }
            });

            DamageResult result = DamageService.ApplyDamage(system, UnitId.None, first.Id, 5f,
                AbilityDamageTypes.Physical);

            Assert.That(nested, Is.True, "a nested shielded hit ran inside the outer shield loop");
            Assert.That(result.Absorbed, Is.EqualTo(5f).Within(0.001f));
            FieldInfo poolField = typeof(DamagePipeline).GetField("_shieldScratch",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(poolField, Is.Not.Null);
            System.Collections.IList levels = (System.Collections.IList)poolField.GetValue(system.Damage);
            Assert.That(levels.Count, Is.GreaterThanOrEqualTo(2), "both nesting levels rented a scratch list");
            for (int i = 0; i < levels.Count; i++)
            {
                Assert.That(((System.Collections.ICollection)levels[i]).Count, Is.Zero,
                    $"scratch level {i} must not keep the last victim's modifiers alive");
            }
        }

        [Test]
        public void HubSpatialGrid_ReturnsTheLinearResultAndSeesSystemMovesImmediately()
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                List<AbilityUnitBehaviour> units = new List<AbilityUnitBehaviour>();
                for (int i = 0; i < 24; i++)
                {
                    units.Add(rig.AddUnit("GridUnit" + i, 2, new Vector3(i % 6 * 1.5f, 0f, i / 6 * 1.5f)));
                }

                Vector3 center = new Vector3(3f, 0f, 2f);
                List<UnitId> linear = new List<UnitId>();
                rig.Hub.QueryUnitsInRadius(center, 2.5f, linear);
                rig.Hub.SpatialCellSize = 2f;
                List<UnitId> gridded = new List<UnitId>();
                rig.Hub.QueryUnitsInRadius(center, 2.5f, gridded);

                Assert.That(linear, Is.Not.Empty);
                Assert.That(gridded, Is.EqualTo(linear));
                Assert.That(rig.Hub.RegisteredUnitCount, Is.EqualTo(24));

                UnitId moved = linear[0];
                Assert.That(rig.Hub.TryMoveUnit(moved, new Vector3(100f, 0f, 100f)), Is.True);
                gridded.Clear();
                rig.Hub.QueryUnitsInRadius(center, 2.5f, gridded);
                Assert.That(gridded, Has.No.Member(moved), "moves through the ability system invalidate the grid");

                units[23].transform.position = center;
                rig.Hub.InvalidateSpatialCache();
                gridded.Clear();
                rig.Hub.QueryUnitsInRadius(center, 0.1f, gridded);
                Assert.That(gridded, Is.EqualTo(new[] { units[23].UnitId }));

                rig.Hub.SpatialCellSize = -1f;
                Assert.That(rig.Hub.SpatialCellSize, Is.Zero, "invalid sizes fall back to the exact linear scan");
            }
        }

        [Test]
        public void HubSpatialGrid_MovesAndUnregistersPatchTheSnapshotInPlace()
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                List<AbilityUnitBehaviour> units = new List<AbilityUnitBehaviour>();
                for (int i = 0; i < 24; i++)
                {
                    units.Add(rig.AddUnit("PatchUnit" + i, 2, new Vector3(i % 6 * 1.5f, 0f, i / 6 * 1.5f)));
                }

                FieldInfo dirtyField = typeof(AbilitySystemBehaviour).GetField("_gridDirty",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(dirtyField, Is.Not.Null);
                Vector3 center = new Vector3(3f, 0f, 2f);
                const float radius = 2.5f;
                rig.Hub.SpatialCellSize = 2f;
                List<UnitId> gridded = new List<UnitId>();
                rig.Hub.QueryUnitsInRadius(center, radius, gridded);
                Assert.That((bool)dirtyField.GetValue(rig.Hub), Is.False, "the first query takes the snapshot");

                XorShiftRandom random = new XorShiftRandom(3u);
                List<UnitId> expected = new List<UnitId>();
                for (int step = 0; step < 48; step++)
                {
                    AbilityUnitBehaviour unit = units[step * 7 % units.Count];
                    if (unit.Unit == null)
                    {
                        continue;
                    }

                    if (step % 5 == 4)
                    {
                        AbilitySceneRig.CallPrivate(unit, "OnDisable");
                    }
                    else
                    {
                        Vector3 target = new Vector3(random.NextFloat() * 9f, 0f, random.NextFloat() * 6f);
                        Assert.That(rig.Hub.TryMoveUnit(unit.UnitId, target), Is.True);
                    }

                    // WHY: kills and knockback must not schedule a rebuild of every unit on the next query.
                    Assert.That((bool)dirtyField.GetValue(rig.Hub), Is.False, $"step {step}: patched in place");

                    gridded.Clear();
                    rig.Hub.QueryUnitsInRadius(center, radius, gridded);
                    expected.Clear();
                    for (int i = 0; i < units.Count; i++)
                    {
                        if (units[i].Unit != null &&
                            AbilitySpatialGrid.IsWithinRadius(units[i].transform.position, center, radius))
                        {
                            expected.Add(units[i].UnitId);
                        }
                    }

                    Assert.That(gridded, Is.EqualTo(expected), $"step {step}");
                }

                Assert.That(rig.Hub.RegisteredUnitCount, Is.LessThan(24), "the sequence unregistered units");
                List<UnitId> linear = new List<UnitId>();
                rig.Hub.SpatialCellSize = 0f;
                rig.Hub.QueryUnitsInRadius(center, radius, linear);
                Assert.That(gridded, Is.EqualTo(linear), "the patched snapshot equals the linear scan");
            }
        }

        private static int SnapshotEntries(AbilityEventBus bus, string fieldName)
        {
            FieldInfo field = typeof(AbilityEventBus).GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            System.Collections.IList levels = (System.Collections.IList)field.GetValue(bus);
            int entries = 0;
            for (int i = 0; i < levels.Count; i++)
            {
                entries += ((System.Collections.ICollection)levels[i]).Count;
            }

            return entries;
        }

        private static List<float> RunProjectileHitRolls(bool warmPool)
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, team: 1);
            AbilityUnit target = AbilityTestSupport.CreateUnit(system, team: 2, health: 100000f);
            AbilityBlueprint volley = new AbilityBlueprint
            {
                Id = "roll_volley",
                Targeting = TargetingMode.Unit,
                Delivery = AbilityDeliveryType.Projectile,
                ProjectileArchetypeId = "roll_volley_projectile"
            };
            // WHY: power-of-two amounts make each hit's damage encode exactly which chance nodes fired.
            for (int i = 0; i < 6; i++)
            {
                EffectNodeData node = AbilityTestSupport.DamageNode(1 << i);
                node.Chance = 0.5f;
                volley.ImpactEffects.Add(node);
            }

            system.RegisterAbility(volley);
            system.GrantAbility(caster.Id, "roll_volley");

            if (warmPool)
            {
                CastResult warm = system.Cast(CastRequest.AtUnit(caster.Id, "roll_volley", target.Id, 5u));
                for (int i = 0; i < 3; i++)
                {
                    Assert.That(system.NotifyProjectileHit(warm.CastId, target.Id, Vector3.zero), Is.True);
                }

                system.ReleaseProjectileCast(warm.CastId);
                Assert.That(system.NotifyProjectileHit(warm.CastId, target.Id, Vector3.zero), Is.False,
                    "a released cast is no longer routable, even though its record was recycled");
            }

            CastResult cast = system.Cast(CastRequest.AtUnit(caster.Id, "roll_volley", target.Id, 77u));
            Assert.That(cast.Success, Is.True);
            List<float> damagePerHit = new List<float>();
            for (int i = 0; i < 8; i++)
            {
                float before = target.Health;
                Assert.That(system.NotifyProjectileHit(cast.CastId, target.Id, Vector3.zero), Is.True);
                damagePerHit.Add(before - target.Health);
            }

            return damagePerHit;
        }

        private static float RunChanceDot()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, team: 1);
            AbilityUnit victim = AbilityTestSupport.CreateUnit(system, team: 2, health: 1000f);
            ModifierBlueprint dot = AbilityTestSupport.Modifier("chance_dot");
            dot.TickInterval = 1f;
            EffectNodeData node = AbilityTestSupport.DamageNode(1f);
            node.Chance = 0.5f;
            dot.TickEffects.Add(node);
            system.Modifiers.Apply(dot, caster.Id, victim.Id);

            for (int i = 0; i < 40; i++)
            {
                system.Tick(1f);
            }

            return 1000f - victim.Health;
        }

        private static float RunChanceThorns()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit attacker = AbilityTestSupport.CreateUnit(system, team: 1, health: 1000f);
            AbilityUnit defender = AbilityTestSupport.CreateUnit(system, team: 2, health: 1000f);
            ModifierBlueprint thorns = AbilityTestSupport.Modifier("chance_thorns");
            EffectNodeData node = AbilityTestSupport.DamageNode(1f);
            node.Chance = 0.5f;
            thorns.EventReactions.Add(new ModifierEventReaction
            {
                EventId = AbilityEvents.TakeDamage,
                TargetEventSource = true,
                Effects = AbilityTestSupport.Nodes(node)
            });
            system.Modifiers.Apply(thorns, defender.Id, defender.Id);

            for (int i = 0; i < 40; i++)
            {
                DamageService.ApplyDamage(system, attacker.Id, defender.Id, 1f, AbilityDamageTypes.Pure);
            }

            return 1000f - attacker.Health;
        }

        private static AbilityBlueprint Skillshot(string id, float range)
        {
            AbilityBlueprint skillshot = new AbilityBlueprint
            {
                Id = id,
                Targeting = TargetingMode.Direction,
                Delivery = AbilityDeliveryType.Projectile,
                ProjectileArchetypeId = "shot_projectile",
                Range = range
            };
            skillshot.CastEffects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.Spawn,
                Target = EffectTargetSelector.Target,
                ArchetypeId = "aim_marker"
            });
            return skillshot;
        }

        private static Vector3 GridTestPoint(XorShiftRandom random)
        {
            return new Vector3(random.NextFloat() * 12f - 6f, random.NextFloat() * 2f, random.NextFloat() * 12f - 6f);
        }

        private static bool ExactlyWithin(Vector3 position, Vector3 center, float radius)
        {
            double dx = (double)position.x - center.x;
            double dy = (double)position.y - center.y;
            double dz = (double)position.z - center.z;
            double r = radius;
            return dx * dx + dy * dy + dz * dz <= r * r;
        }

        private sealed class ContextKeepingOp : IEffectOperation
        {
            public EffectContext Kept;
            public EffectContext KeptNested;
            public bool ReleasedDuringExecute;

            public string Id => "keep_context_test";

            public void Execute(EffectContext context, EffectNodeData node, List<UnitId> targets)
            {
                ReleasedDuringExecute |= context.IsReleased;
                Kept = context;
                KeptNested = context.CreateNested(context.Caster, context.AbilityId, targets);
                ReleasedDuringExecute |= KeptNested.IsReleased;
            }
        }
    }
}
