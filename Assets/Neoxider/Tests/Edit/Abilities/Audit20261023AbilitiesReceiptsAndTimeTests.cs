using Neo.Abilities;
using Neo.Core.Resources;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities
{
    public sealed class Audit20261023AbilitiesReceiptsAndTimeTests
    {
        [TestCase(-1f, 10f)]
        [TestCase(3f, 3f)]
        public void DamageReceipt_UsesAcceptedDecreaseWhenCallbackHeals(float cap, float expectedDamage)
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit target = AbilityTestSupport.CreateUnit(system, health: 100f, healthCurrent: 50f);
            target.Resources.AddPool(AbilityResourceIds.Health, new ResourcePoolEntry
            {
                Current = 50f, Max = 100f, MaxDecreaseAmount = cap, MaxIncreaseAmount = -1f
            });
            bool responded = false;
            target.Resources.OnResourceChanged += (id, current, max) =>
            {
                if (id == AbilityResourceIds.Health && !responded)
                {
                    responded = true;
                    target.Resources.Increase(id, 20f);
                }
            };
            EventLog log = new EventLog(system);

            DamageResult result = DamageService.ApplyDamage(system, UnitId.None, target.Id, 10f,
                AbilityDamageTypes.Pure);

            Assert.That(result.HealthDamage, Is.EqualTo(expectedDamage));
            Assert.That(target.Health, Is.EqualTo(70f - expectedDamage));
            Assert.That(result.Killed, Is.False);
            Assert.That(log.Count(AbilityEvents.TakeDamage), Is.EqualTo(1));
            Assert.That(log.TryGetLast(AbilityEvents.TakeDamage, out AbilityEventArgs damage), Is.True);
            Assert.That(damage.Amount, Is.EqualTo(expectedDamage));
        }

        [Test]
        public void DamageReceipt_StaysAcceptedWhileDeathUsesCallbackModifiedHealth()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit target = AbilityTestSupport.CreateUnit(system, health: 100f, healthCurrent: 5f);
            bool responded = false;
            target.Resources.OnResourceChanged += (id, current, max) =>
            {
                if (id == AbilityResourceIds.Health && !responded)
                {
                    responded = true;
                    target.Resources.SetCurrent(id, 20f);
                }
            };
            EventLog log = new EventLog(system);

            DamageResult result = DamageService.ApplyDamage(system, UnitId.None, target.Id, 10f,
                AbilityDamageTypes.Pure);

            Assert.That(result.HealthDamage, Is.EqualTo(5f));
            Assert.That(result.Killed, Is.False);
            Assert.That(target.IsAlive, Is.True);
            Assert.That(target.Health, Is.EqualTo(20f));
            Assert.That(log.Count(AbilityEvents.TakeDamage), Is.EqualTo(1));
            Assert.That(log.Count(AbilityEvents.Death), Is.Zero);
        }

        [TestCase(-1f, 5f)]
        [TestCase(3f, 3f)]
        public void LifestealReceipt_UsesAcceptedIncreaseWhenBothBalancesChange(float cap, float expectedHeal)
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit source = AbilityTestSupport.CreateUnit(system, health: 100f, healthCurrent: 50f);
            AbilityUnit target = AbilityTestSupport.CreateUnit(system, team: 2, health: 100f, healthCurrent: 50f);
            source.Resources.AddPool(AbilityResourceIds.Health, new ResourcePoolEntry
            {
                Current = 50f, Max = 100f, MaxDecreaseAmount = -1f, MaxIncreaseAmount = cap
            });
            source.SetBaseProperty(AbilityProperties.LifestealPercent, 50f);
            bool targetResponded = false;
            bool sourceResponded = false;
            target.Resources.OnResourceChanged += (id, current, max) =>
            {
                if (id == AbilityResourceIds.Health && !targetResponded)
                {
                    targetResponded = true;
                    target.Resources.Increase(id, 20f);
                }
            };
            source.Resources.OnResourceChanged += (id, current, max) =>
            {
                if (id == AbilityResourceIds.Health && !sourceResponded)
                {
                    sourceResponded = true;
                    source.Resources.Decrease(id, 20f);
                }
            };
            EventLog log = new EventLog(system);

            DamageResult result = DamageService.ApplyDamage(system, source.Id, target.Id, 10f,
                AbilityDamageTypes.Pure, "receipt_attack", 37u);

            Assert.That(result.HealthDamage, Is.EqualTo(10f));
            Assert.That(target.Health, Is.EqualTo(60f));
            Assert.That(source.Health, Is.EqualTo(30f + expectedHeal));
            Assert.That(log.Count(AbilityEvents.HealReceived), Is.EqualTo(1));
            Assert.That(log.TryGetLast(AbilityEvents.HealReceived, out AbilityEventArgs heal), Is.True);
            Assert.That(heal.Target, Is.EqualTo(source.Id));
            Assert.That(heal.Source, Is.EqualTo(target.Id));
            Assert.That(heal.Amount, Is.EqualTo(expectedHeal));
            Assert.That(heal.CastId, Is.EqualTo(37u));
        }

        [TestCase(-1f, 20f)]
        [TestCase(4f, 4f)]
        public void HealEffectReceipt_UsesAcceptedIncreaseWhenCallbackDamages(float cap, float expectedHeal)
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit source = AbilityTestSupport.CreateUnit(system);
            AbilityUnit target = AbilityTestSupport.CreateUnit(system, health: 100f, healthCurrent: 50f);
            target.Resources.AddPool(AbilityResourceIds.Health, new ResourcePoolEntry
            {
                Current = 50f, Max = 100f, MaxDecreaseAmount = -1f, MaxIncreaseAmount = cap
            });
            target.SetBaseProperty(AbilityProperties.HealingReceivedMul, 2f);
            bool responded = false;
            target.Resources.OnResourceChanged += (id, current, max) =>
            {
                if (id == AbilityResourceIds.Health && !responded)
                {
                    responded = true;
                    target.Resources.Decrease(id, 30f);
                }
            };
            EffectContext context = AbilityTestSupport.Context(system, source.Id, 1u, target.Id);
            context.CastId = 41u;
            EventLog log = new EventLog(system);

            system.ExecuteEffects(AbilityTestSupport.Nodes(AbilityTestSupport.HealNode(10f)), context);

            Assert.That(target.Health, Is.EqualTo(20f + expectedHeal));
            Assert.That(log.Count(AbilityEvents.HealReceived), Is.EqualTo(1));
            Assert.That(log.TryGetLast(AbilityEvents.HealReceived, out AbilityEventArgs heal), Is.True);
            Assert.That(heal.Amount, Is.EqualTo(expectedHeal));
            Assert.That(heal.CastId, Is.EqualTo(41u));
            Assert.That(heal.Source, Is.EqualTo(source.Id));
        }

        [TestCase(-1f, 20f)]
        [TestCase(4f, 4f)]
        public void UnitHealReceipt_UsesAcceptedIncreaseWhenCallbackDamages(float cap, float expectedHeal)
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                AbilityUnitBehaviour behaviour = rig.AddUnit("Heal receipt unit", 1, Vector3.zero);
                AbilityUnit unit = behaviour.Unit;
                unit.Resources.AddPool(AbilityResourceIds.Health, new ResourcePoolEntry
                {
                    Current = 50f, Max = 100f, MaxDecreaseAmount = -1f, MaxIncreaseAmount = cap
                });
                unit.SetBaseProperty(AbilityProperties.HealingReceivedMul, 2f);
                bool responded = false;
                unit.Resources.OnResourceChanged += (id, current, max) =>
                {
                    if (id == AbilityResourceIds.Health && !responded)
                    {
                        responded = true;
                        unit.Resources.Decrease(id, 30f);
                    }
                };
                float announced = 0f;
                int callbacks = 0;
                behaviour.OnHealed.AddListener(amount =>
                {
                    announced = amount;
                    callbacks++;
                });
                EventLog log = new EventLog(rig.System);

                behaviour.ApplyHeal(10f);

                Assert.That(unit.Health, Is.EqualTo(20f + expectedHeal));
                Assert.That(callbacks, Is.EqualTo(1));
                Assert.That(announced, Is.EqualTo(expectedHeal));
                Assert.That(log.TryGetLast(AbilityEvents.HealReceived, out AbilityEventArgs heal), Is.True);
                Assert.That(heal.Amount, Is.EqualTo(expectedHeal));
                Assert.That(heal.Target, Is.EqualTo(unit.Id));
                Assert.That(heal.Source, Is.EqualTo(UnitId.None));
            }
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void SystemTick_NonfiniteStepPreservesTimersResourcesAndProjectileClock(float invalid)
        {
            FakeWorldAdapter world = new FakeWorldAdapter();
            AbilitySystem system = new AbilitySystem { World = world, PendingProjectileTimeout = 1f };
            AbilityUnit unit = AbilityTestSupport.CreateUnit(system, health: 100f, healthCurrent: 50f);
            unit.SetBaseProperty(AbilityProperties.HealthRegen, 4f);
            AbilityBlueprint ability = new AbilityBlueprint
            {
                Id = "finite_clock", Cooldown = 4f, Delivery = AbilityDeliveryType.Projectile,
                ProjectileArchetypeId = "clock_probe"
            };
            system.RegisterAbility(ability);
            AbilitySlot slot = system.GrantAbility(unit.Id, ability.Id);
            CastResult cast = system.Cast(CastRequest.NoTarget(unit.Id, ability.Id));
            Assert.That(cast.Success, Is.True);
            ModifierBlueprint dot = new ModifierBlueprint { Id = "clock_dot", Duration = 2f, TickInterval = 1f };
            dot.TickEffects.Add(AbilityTestSupport.DamageNode(2f));
            ModifierInstance modifier = system.Modifiers.Apply(dot, unit.Id, unit.Id).Instance;
            system.Tick(0.25f);
            EventLog log = new EventLog(system);

            system.Tick(invalid);

            Assert.That(unit.Health, Is.EqualTo(51f));
            Assert.That(slot.CooldownRemaining, Is.EqualTo(3.75f));
            Assert.That(modifier.RemainingDuration, Is.EqualTo(1.75f));
            Assert.That(modifier.IsActive, Is.True);
            Assert.That(log.Events, Is.Empty);
            Assert.That(system.NotifyProjectileHit(cast.CastId, unit.Id, Vector3.zero), Is.True);
            system.Tick(1f);
            Assert.That(system.NotifyProjectileHit(cast.CastId, unit.Id, Vector3.zero), Is.False);
            Assert.That(unit.Health, Is.EqualTo(53f));
            Assert.That(slot.CooldownRemaining, Is.EqualTo(2.75f));
            Assert.That(modifier.RemainingDuration, Is.EqualTo(0.75f));
            Assert.That(log.Count(AbilityEvents.TakeDamage), Is.EqualTo(1));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void ModifierTick_NonfiniteStepDoesNotChangeDurationOrTickPhase(float invalid)
        {
            ModifierEngine engine = new ModifierEngine();
            ModifierBlueprint blueprint = new ModifierBlueprint { Id = "finite_modifier", Duration = 2f, TickInterval = 1f };
            blueprint.TickEffects.Add(AbilityTestSupport.DamageNode(1f));
            UnitId owner = new UnitId(1);
            ModifierInstance modifier = engine.Apply(blueprint, owner, owner).Instance;
            int ticks = 0;
            engine.TickDue += instance => ticks++;
            engine.Tick(0.25f);

            engine.Tick(invalid);

            Assert.That(modifier.RemainingDuration, Is.EqualTo(1.75f));
            Assert.That(modifier.IsActive, Is.True);
            Assert.That(ticks, Is.Zero);
            engine.Tick(0.75f);
            Assert.That(ticks, Is.EqualTo(1));
            Assert.That(modifier.RemainingDuration, Is.EqualTo(1f));
        }

        [Test]
        public void SystemTick_FiniteClockOverflowRejectsTheWholeStep()
        {
            AbilitySystem system = new AbilitySystem { World = new FakeWorldAdapter(), PendingProjectileTimeout = 1f };
            system.Tick(float.MaxValue);
            AbilityUnit unit = AbilityTestSupport.CreateUnit(system, health: 100f, healthCurrent: 50f);
            unit.SetBaseProperty(AbilityProperties.HealthRegen, 4f);
            ModifierBlueprint duration = new ModifierBlueprint
            {
                Id = "clock_overflow_duration", Duration = 2f, TickInterval = 0f
            };
            ModifierInstance modifier = system.Modifiers.Apply(duration, unit.Id, unit.Id).Instance;
            AbilityBlueprint ability = new AbilityBlueprint
            {
                Id = "clock_overflow", Delivery = AbilityDeliveryType.Projectile,
                ProjectileArchetypeId = "clock_probe"
            };
            system.RegisterAbility(ability);
            system.GrantAbility(unit.Id, ability.Id);
            CastResult cast = system.Cast(CastRequest.NoTarget(unit.Id, ability.Id));
            Assert.That(cast.Success, Is.True);
            Assert.That(unit.IsAlive, Is.True);
            Assert.That(unit.Health, Is.EqualTo(50f));
            Assert.That(modifier.IsActive, Is.True);
            Assert.That(modifier.RemainingDuration, Is.EqualTo(2f));

            system.Tick(float.MaxValue);

            Assert.That(unit.Health, Is.EqualTo(50f));
            Assert.That(modifier.IsActive, Is.True);
            Assert.That(modifier.RemainingDuration, Is.EqualTo(2f));
            Assert.That(system.NotifyProjectileHit(cast.CastId, unit.Id, Vector3.zero), Is.True);

            system.Tick(1f);

            Assert.That(unit.Health, Is.EqualTo(54f));
            Assert.That(modifier.IsActive, Is.True);
            Assert.That(modifier.RemainingDuration, Is.EqualTo(1f));
        }
    }
}
