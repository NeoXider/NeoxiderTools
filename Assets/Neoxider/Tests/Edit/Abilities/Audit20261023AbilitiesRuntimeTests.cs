using System.Collections.Generic;
using Neo.Abilities;
using Neo.Core.Resources;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities
{
    public sealed class Audit20261023AbilitiesRuntimeTests
    {
        [Test]
        public void DuplicateResourceCosts_RejectUnaffordableTotalWithoutPaying()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, mana: 100f);
            AbilityBlueprint ability = new AbilityBlueprint
            {
                Id = "duplicate_cost",
                Cooldown = 5f,
                Costs =
                {
                    new AbilityCost(AbilityResourceIds.Mana, 60f),
                    new AbilityCost("MANA", 60f)
                }
            };
            system.RegisterAbility(ability);
            AbilitySlot slot = system.GrantAbility(caster.Id, ability.Id);
            EventLog log = new EventLog(system);

            CastResult result = system.Cast(CastRequest.NoTarget(caster.Id, ability.Id));

            Assert.That(result.Failure, Is.EqualTo(CastFailureReason.NotEnoughResources));
            Assert.That(caster.Resources.GetCurrent(AbilityResourceIds.Mana), Is.EqualTo(100f));
            Assert.That(slot.IsReady, Is.True);
            Assert.That(log.Count(AbilityEvents.AbilityCast), Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CappedResourceCost_RejectsBeforeAnyPaymentOrCastState(bool earlierOtherResourceCost)
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, health: 100f);
            caster.Resources.AddPool(AbilityResourceIds.Mana, new ResourcePoolEntry
            {
                Current = 100f,
                Max = 100f,
                MaxDecreaseAmount = 1f,
                MaxIncreaseAmount = -1f
            });
            AbilityTestSupport.AddPool(caster, "energy", 10f);
            AbilityBlueprint ability = new AbilityBlueprint
            {
                Id = "capped_cost",
                MaxCharges = 2,
                ChargeRestoreTime = 5f
            };
            if (earlierOtherResourceCost)
            {
                ability.Costs.Add(new AbilityCost("energy", 5f));
            }

            ability.Costs.Add(new AbilityCost(AbilityResourceIds.Mana, 10f));
            ability.ImpactEffects.Add(AbilityTestSupport.DamageNode(5f));
            system.RegisterAbility(ability);
            AbilitySlot slot = system.GrantAbility(caster.Id, ability.Id);
            EventLog log = new EventLog(system);
            int resourceChanges = 0;
            caster.Resources.OnResourceChanged += (id, current, max) => resourceChanges++;

            CastResult result = system.Cast(CastRequest.NoTarget(caster.Id, ability.Id));

            Assert.That(result.Failure, Is.EqualTo(CastFailureReason.NotEnoughResources));
            Assert.That(caster.Resources.GetCurrent(AbilityResourceIds.Mana), Is.EqualTo(100f));
            Assert.That(caster.Resources.GetCurrent("energy"), Is.EqualTo(10f));
            Assert.That(caster.Health, Is.EqualTo(100f));
            Assert.That(resourceChanges, Is.Zero);
            Assert.That(slot.Charges, Is.EqualTo(2));
            Assert.That(slot.CooldownRemaining, Is.Zero);
            Assert.That(log.Count(AbilityEvents.AbilityCast), Is.Zero);
        }

        [Test]
        public void RepeatedResourceCosts_ApplyTheDecreaseCapToEachAuthoredEntry()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system);
            caster.Resources.AddPool(AbilityResourceIds.Mana, new ResourcePoolEntry
            {
                Current = 100f,
                Max = 100f,
                MaxDecreaseAmount = 10f,
                MaxIncreaseAmount = -1f
            });
            AbilityBlueprint ability = new AbilityBlueprint
            {
                Id = "per_entry_cap",
                Costs = { new AbilityCost(AbilityResourceIds.Mana, 6f), new AbilityCost("MANA", 6f) }
            };
            system.RegisterAbility(ability);
            system.GrantAbility(caster.Id, ability.Id);

            CastResult result = system.Cast(CastRequest.NoTarget(caster.Id, ability.Id));

            Assert.That(result.Success, Is.True, "Each 6-mana operation fits the cap of 10; the total fits the balance.");
            Assert.That(caster.Resources.GetCurrent(AbilityResourceIds.Mana), Is.EqualTo(88f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PaymentCallbacks_UseCapturedResourceIdsAndEffectiveAmounts(bool mutateCostDefinition)
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, mana: 100f);
            AbilityTestSupport.AddPool(caster, "energy", 10f);
            AbilityBlueprint ability = new AbilityBlueprint
            {
                Id = "captured_costs",
                MaxCharges = 2,
                ChargeRestoreTime = 5f,
                Costs = { new AbilityCost("energy", 5f), new AbilityCost(AbilityResourceIds.Mana, 10f) }
            };
            system.RegisterAbility(ability);
            AbilitySlot slot = system.GrantAbility(caster.Id, ability.Id);
            caster.Resources.OnResourceChanged += (id, current, max) =>
            {
                if (id == "energy")
                {
                    caster.SetBaseProperty(AbilityProperties.ManaCostMul, 4f);
                    if (mutateCostDefinition)
                    {
                        ability.Costs[1] = new AbilityCost("energy", 1000f);
                    }
                }
            };

            CastResult result = system.Cast(CastRequest.NoTarget(caster.Id, ability.Id));

            Assert.That(result.Success, Is.True);
            Assert.That(caster.Resources.GetCurrent("energy"), Is.EqualTo(5f));
            Assert.That(caster.Resources.GetCurrent(AbilityResourceIds.Mana), Is.EqualTo(90f));
            Assert.That(slot.Charges, Is.EqualTo(1));
            Assert.That(slot.CooldownRemaining, Is.EqualTo(5f));
        }

        [Test]
        public void PaymentCallback_LateCapChangeRejectsCastWithoutClippingTheNextPayment()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, health: 100f);
            ResourcePoolEntry mana = new ResourcePoolEntry
            {
                Current = 100f,
                Max = 100f,
                MaxDecreaseAmount = -1f,
                MaxIncreaseAmount = -1f
            };
            caster.Resources.AddPool(AbilityResourceIds.Mana, mana);
            AbilityTestSupport.AddPool(caster, "energy", 10f);
            AbilityBlueprint ability = new AbilityBlueprint
            {
                Id = "late_capped_cost",
                MaxCharges = 2,
                ChargeRestoreTime = 5f,
                Costs = { new AbilityCost("energy", 5f), new AbilityCost(AbilityResourceIds.Mana, 10f) }
            };
            ability.ImpactEffects.Add(AbilityTestSupport.DamageNode(5f));
            system.RegisterAbility(ability);
            AbilitySlot slot = system.GrantAbility(caster.Id, ability.Id);
            EventLog log = new EventLog(system);
            caster.Resources.OnResourceChanged += (id, current, max) =>
            {
                if (id == "energy")
                {
                    mana.MaxDecreaseAmount = 1f;
                }
            };

            CastResult result = system.Cast(CastRequest.NoTarget(caster.Id, ability.Id));

            Assert.That(result.Failure, Is.EqualTo(CastFailureReason.NotEnoughResources));
            Assert.That(caster.Resources.GetCurrent("energy"), Is.EqualTo(5f), "Earlier completed payments are not rolled back.");
            Assert.That(caster.Resources.GetCurrent(AbilityResourceIds.Mana), Is.EqualTo(100f));
            Assert.That(caster.Health, Is.EqualTo(100f));
            Assert.That(slot.Charges, Is.EqualTo(2));
            Assert.That(slot.CooldownRemaining, Is.Zero);
            Assert.That(log.Count(AbilityEvents.AbilityCast), Is.Zero);
        }

        [Test]
        public void NaNResourceCost_CannotBypassLaterUnaffordableCost()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, mana: 100f);
            AbilityBlueprint ability = new AbilityBlueprint
            {
                Id = "nan_duplicate_cost",
                Cooldown = 5f,
                Costs =
                {
                    new AbilityCost(AbilityResourceIds.Mana, float.NaN),
                    new AbilityCost("MANA", 120f)
                }
            };
            system.RegisterAbility(ability);
            AbilitySlot slot = system.GrantAbility(caster.Id, ability.Id);
            EventLog log = new EventLog(system);

            CastResult result = system.Cast(CastRequest.NoTarget(caster.Id, ability.Id));

            Assert.That(result.Failure, Is.EqualTo(CastFailureReason.NotEnoughResources));
            Assert.That(caster.Resources.GetCurrent(AbilityResourceIds.Mana), Is.EqualTo(100f));
            Assert.That(slot.Charges, Is.EqualTo(1));
            Assert.That(slot.CooldownRemaining, Is.Zero);
            Assert.That(log.Count(AbilityEvents.AbilityCast), Is.Zero);
        }

        [TestCase(float.NaN, 1f)]
        [TestCase(float.PositiveInfinity, 1f)]
        [TestCase(float.NegativeInfinity, 1f)]
        [TestCase(1f, float.NaN)]
        [TestCase(1f, float.PositiveInfinity)]
        [TestCase(1f, float.NegativeInfinity)]
        [TestCase(float.MaxValue, 2f)]
        [TestCase(float.MinValue, 2f)]
        public void NonFiniteEffectiveCost_RejectsBeforeAnyResourcePayment(float amount, float manaMultiplier)
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, mana: 100f);
            AbilityTestSupport.AddPool(caster, "energy", 10f);
            caster.SetBaseProperty(AbilityProperties.ManaCostMul, manaMultiplier);
            AbilityBlueprint ability = new AbilityBlueprint
            {
                Id = "invalid_effective_cost",
                MaxCharges = 2,
                ChargeRestoreTime = 5f,
                Costs = { new AbilityCost("energy", 5f), new AbilityCost(AbilityResourceIds.Mana, amount) }
            };
            system.RegisterAbility(ability);
            AbilitySlot slot = system.GrantAbility(caster.Id, ability.Id);
            EventLog log = new EventLog(system);
            int resourceChanges = 0;
            caster.Resources.OnResourceChanged += (id, current, max) => resourceChanges++;

            CastResult result = system.Cast(CastRequest.NoTarget(caster.Id, ability.Id));

            Assert.That(result.Failure, Is.EqualTo(CastFailureReason.NotEnoughResources));
            Assert.That(caster.Resources.GetCurrent("energy"), Is.EqualTo(10f));
            Assert.That(caster.Resources.GetCurrent(AbilityResourceIds.Mana), Is.EqualTo(100f));
            Assert.That(resourceChanges, Is.Zero);
            Assert.That(slot.Charges, Is.EqualTo(2));
            Assert.That(slot.CooldownRemaining, Is.Zero);
            Assert.That(log.Count(AbilityEvents.AbilityCast), Is.Zero);
        }

        [Test]
        public void DuplicateManaCosts_UseEffectiveAmountsAndAllowExactPayment()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, mana: 60f);
            caster.SetBaseProperty(AbilityProperties.ManaCostMul, 0.5f);
            AbilityBlueprint ability = new AbilityBlueprint
            {
                Id = "discounted_cost",
                Costs = { new AbilityCost("mana", 60f), new AbilityCost("MANA", 60f) }
            };
            system.RegisterAbility(ability);
            system.GrantAbility(caster.Id, ability.Id);

            CastResult result = system.Cast(CastRequest.NoTarget(caster.Id, ability.Id));

            Assert.That(result.Success, Is.True);
            Assert.That(caster.Resources.GetCurrent(AbilityResourceIds.Mana), Is.Zero);
        }

        [Test]
        public void EmptyCostResourceId_IsRejectedAndEmptyRevokeIsANoOp()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system);
            AbilityBlueprint ability = new AbilityBlueprint
            {
                Id = "invalid_cost",
                Costs = { new AbilityCost(null, 5f) }
            };
            system.RegisterAbility(ability);
            AbilitySlot slot = system.GrantAbility(caster.Id, ability.Id);

            CastResult result = system.Cast(CastRequest.NoTarget(caster.Id, ability.Id));

            Assert.That(result.Failure, Is.EqualTo(CastFailureReason.NotEnoughResources));
            Assert.That(system.RevokeAbility(caster.Id, null), Is.False);
            Assert.That(slot.IsReady, Is.True);
        }

        [TestCase(0f, 6f)]
        [TestCase(50f, 3f)]
        public void ChargeRestoration_CatchesUpAllElapsedEffectiveIntervals(float reduction, float step)
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system);
            caster.SetBaseProperty(AbilityProperties.CooldownReductionPercent, reduction);
            AbilityBlueprint ability = new AbilityBlueprint
            {
                Id = "charges",
                MaxCharges = 3,
                ChargeRestoreTime = 2f
            };
            system.RegisterAbility(ability);
            AbilitySlot slot = system.GrantAbility(caster.Id, ability.Id);
            for (int i = 0; i < 3; i++)
            {
                Assert.That(system.Cast(CastRequest.NoTarget(caster.Id, ability.Id)).Success, Is.True);
            }

            system.Tick(step);

            Assert.That(slot.Charges, Is.EqualTo(3));
            Assert.That(slot.CooldownRemaining, Is.Zero);
            Assert.That(slot.NormalizedCooldown, Is.Zero);
            Assert.That(slot.IsReady, Is.True);
        }

        [Test]
        public void ChargeRestoration_PreservesPartialIntervalAfterLargeStep()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system);
            AbilityBlueprint ability = new AbilityBlueprint
            {
                Id = "partial_charges",
                MaxCharges = 3,
                ChargeRestoreTime = 2f
            };
            system.RegisterAbility(ability);
            AbilitySlot slot = system.GrantAbility(caster.Id, ability.Id);
            for (int i = 0; i < 3; i++)
            {
                system.Cast(CastRequest.NoTarget(caster.Id, ability.Id));
            }

            system.Tick(5f);

            Assert.That(slot.Charges, Is.EqualTo(2));
            Assert.That(slot.CooldownRemaining, Is.EqualTo(1f).Within(0.001f));
            system.Tick(1f);
            Assert.That(slot.Charges, Is.EqualTo(3));
            Assert.That(slot.CooldownRemaining, Is.Zero);
        }

        [Test]
        public void ZeroChargeRestoreTime_DoesNotPermanentlyConsumeCharges()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system);
            AbilityBlueprint ability = new AbilityBlueprint { Id = "instant_charges", MaxCharges = 2 };
            system.RegisterAbility(ability);
            AbilitySlot slot = system.GrantAbility(caster.Id, ability.Id);

            for (int i = 0; i < 4; i++)
            {
                Assert.That(system.Cast(CastRequest.NoTarget(caster.Id, ability.Id)).Success, Is.True);
            }

            Assert.That(slot.Charges, Is.EqualTo(2));
            Assert.That(slot.CooldownRemaining, Is.Zero);
        }

        [Test]
        public void NormalizedCooldown_UsesTheReducedDurationThatActuallyStarted()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system);
            caster.SetBaseProperty(AbilityProperties.CooldownReductionPercent, 50f);
            AbilityBlueprint ability = new AbilityBlueprint { Id = "reduced", Cooldown = 10f };
            system.RegisterAbility(ability);
            AbilitySlot slot = system.GrantAbility(caster.Id, ability.Id);

            system.Cast(CastRequest.NoTarget(caster.Id, ability.Id));
            Assert.That(slot.NormalizedCooldown, Is.EqualTo(1f).Within(0.001f));
            system.Tick(2.5f);
            Assert.That(slot.NormalizedCooldown, Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void FiniteDot_LargeStepDoesNotTickPastItsLifetime()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit unit = AbilityTestSupport.CreateUnit(system, health: 100f);
            ModifierBlueprint dot = new ModifierBlueprint { Id = "short_dot", Duration = 1f, TickInterval = 1f };
            dot.TickEffects.Add(AbilityTestSupport.DamageNode(10f));
            ModifierInstance instance = system.Modifiers.Apply(dot, unit.Id, unit.Id).Instance;

            system.Tick(5f);

            Assert.That(unit.Health, Is.EqualTo(90f));
            Assert.That(instance.IsActive, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BulkRemoval_CallbackCanRemoveRemainingInstances(bool byId)
        {
            ModifierEngine engine = new ModifierEngine();
            UnitId owner = new UnitId(1);
            ModifierBlueprint blueprint = new ModifierBlueprint
            {
                Id = "independent",
                StackPolicy = ModifierStackPolicy.Independent
            };
            List<ModifierInstance> instances = new List<ModifierInstance>();
            for (int i = 0; i < 3; i++)
            {
                instances.Add(engine.Apply(blueprint, owner, owner).Instance);
            }

            int receipts = 0;
            engine.Removed += (instance, expired) =>
            {
                receipts++;
                if (receipts == 1)
                {
                    engine.RemoveAllFrom(owner);
                }
            };

            int removed = byId ? engine.RemoveById(owner, blueprint.Id) : engine.RemoveWhere(owner, _ => true);

            Assert.That(removed, Is.EqualTo(1), "Only removals performed by the outer call count here.");
            Assert.That(receipts, Is.EqualTo(3));
            for (int i = 0; i < instances.Count; i++)
            {
                Assert.That(instances[i].IsActive, Is.False);
            }
        }

        [Test]
        public void BulkRemoval_DoesNotConsumeReplacementCreatedByCallback()
        {
            ModifierEngine engine = new ModifierEngine();
            UnitId owner = new UnitId(1);
            ModifierBlueprint blueprint = new ModifierBlueprint { Id = "replacement" };
            ModifierInstance original = engine.Apply(blueprint, owner, owner).Instance;
            ModifierInstance replacement = null;
            engine.Removed += (instance, expired) =>
                replacement = engine.Apply(blueprint, owner, owner).Instance;

            int removed = engine.RemoveAllFrom(owner);

            Assert.That(removed, Is.EqualTo(1));
            Assert.That(original.IsActive, Is.False);
            Assert.That(replacement, Is.Not.Null);
            Assert.That(replacement.IsActive, Is.True);
        }

        [Test]
        public void Remove_ForeignEngineCannotDeactivateAnInstance()
        {
            ModifierEngine ownerEngine = new ModifierEngine();
            ModifierEngine otherEngine = new ModifierEngine();
            UnitId owner = new UnitId(1);
            ModifierInstance instance = ownerEngine.Apply(new ModifierBlueprint { Id = "owned" }, owner, owner).Instance;

            bool removed = otherEngine.Remove(instance);

            Assert.That(removed, Is.False);
            Assert.That(instance.IsActive, Is.True);
            Assert.That(otherEngine.VersionOf(owner), Is.Zero);
            Assert.That(ownerEngine.Remove(instance), Is.True);
        }

        [Test]
        public void ResourceCallbackAreaEffect_DoesNotClobberRemainingUnitTicks()
        {
            AbilitySystem system = new AbilitySystem();
            FakeWorldAdapter world = new FakeWorldAdapter();
            system.World = world;
            AbilityUnit first = AbilityTestSupport.CreateUnit(system, health: 100f, healthCurrent: 50f);
            AbilityUnit second = AbilityTestSupport.CreateUnit(system, health: 100f, healthCurrent: 50f);
            first.SetBaseProperty(AbilityProperties.HealthRegen, 1f);
            second.SetBaseProperty(AbilityProperties.HealthRegen, 1f);
            world.SetPosition(first.Id, Vector3.zero);
            world.SetPosition(second.Id, new Vector3(10f, 0f, 0f));
            EffectNodeData area = AbilityTestSupport.DamageNode(0f, target: EffectTargetSelector.AreaAroundCaster);
            area.Radius = 0.5f;
            area.TeamFilter = AbilityTeamFilter.Any;
            first.Resources.OnResourceChanged += (id, current, max) =>
                system.ExecuteEffects(AbilityTestSupport.Nodes(area), AbilityTestSupport.Context(system, first.Id));
            second.Resources.OnResourceChanged += (id, current, max) =>
                system.ExecuteEffects(AbilityTestSupport.Nodes(area), AbilityTestSupport.Context(system, second.Id));

            system.Tick(1f);

            Assert.That(first.Health, Is.EqualTo(51f));
            Assert.That(second.Health, Is.EqualTo(51f), "The area query must not replace the unit tick snapshot.");
        }

        [Test]
        public void BaseMaxResourceBonuses_ImmediatelyUpdatePoolsWithoutCompounding()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit unit = AbilityTestSupport.CreateUnit(system, health: 100f, mana: 50f);

            unit.SetBaseProperty(AbilityProperties.MaxHealthBonus, 50f);
            unit.SetBaseProperty(AbilityProperties.MaxManaBonus, 25f);
            Assert.That(unit.MaxHealth, Is.EqualTo(150f));
            Assert.That(unit.Resources.GetMax(AbilityResourceIds.Mana), Is.EqualTo(75f));
            unit.SetBaseProperty(AbilityProperties.MaxHealthBonus, 20f);
            unit.SetBaseProperty(AbilityProperties.MaxManaBonus, 0f);

            Assert.That(unit.MaxHealth, Is.EqualTo(120f));
            Assert.That(unit.Resources.GetMax(AbilityResourceIds.Mana), Is.EqualTo(50f));
            Assert.That(unit.Health, Is.EqualTo(100f), "Increasing max health does not heal.");
        }

        [Test]
        public void MaxBonusResourceCallback_CanUpdateBonusWithoutLosingTheBaseMaximum()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit unit = AbilityTestSupport.CreateUnit(system, health: 100f);
            unit.Resources.OnResourceChanged += (id, current, max) =>
            {
                if (id == AbilityResourceIds.Health && max == 150f)
                {
                    unit.SetBaseProperty(AbilityProperties.MaxHealthBonus, 100f);
                }
            };

            unit.SetBaseProperty(AbilityProperties.MaxHealthBonus, 50f);
            Assert.That(unit.MaxHealth, Is.EqualTo(200f));
            unit.SetBaseProperty(AbilityProperties.MaxHealthBonus, 0f);

            Assert.That(unit.MaxHealth, Is.EqualTo(100f));
        }

        [Test]
        public void Damage_UnitWithoutHealthPoolDoesNotDieOrProduceDamageReceipts()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit unit = system.CreateUnit(new TeamId(1));
            EventLog log = new EventLog(system);

            DamageResult result = DamageService.ApplyDamage(system, UnitId.None, unit.Id, 10f,
                AbilityDamageTypes.Pure);

            Assert.That(result.Negated, Is.True);
            Assert.That(result.Killed, Is.False);
            Assert.That(unit.IsAlive, Is.True);
            Assert.That(log.Count(AbilityEvents.Death), Is.Zero);
            Assert.That(log.Count(AbilityEvents.TakeDamage), Is.Zero);
        }

        [Test]
        public void ShieldReceiptCallback_RemovedShieldCannotAbsorbTheRemainingHit()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit unit = AbilityTestSupport.CreateUnit(system, health: 100f);
            ModifierBlueprint first = AbilityTestSupport.Modifier("first")
                .WithProperty(AbilityProperties.ShieldHp, PropertyOp.Add, 10f);
            ModifierBlueprint second = AbilityTestSupport.Modifier("second")
                .WithProperty(AbilityProperties.ShieldHp, PropertyOp.Add, 100f);
            system.Modifiers.Apply(first, unit.Id, unit.Id);
            ModifierInstance removedShield = system.Modifiers.Apply(second, unit.Id, unit.Id).Instance;
            system.Events.Subscribe(AbilityEvents.ShieldAbsorbed, _ => system.Modifiers.Remove(removedShield));

            DamageResult result = DamageService.ApplyDamage(system, UnitId.None, unit.Id, 40f,
                AbilityDamageTypes.Physical);

            Assert.That(result.Absorbed, Is.EqualTo(10f));
            Assert.That(result.HealthDamage, Is.EqualTo(30f));
            Assert.That(unit.Health, Is.EqualTo(70f));
        }

        [Test]
        public void ShieldAbsorbedCallback_StackingCurrentShieldPreservesExpandedCapacity()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit unit = AbilityTestSupport.CreateUnit(system, health: 100f);
            ModifierBlueprint shield = AbilityTestSupport.Modifier("stacking_shield", policy: ModifierStackPolicy.Stack)
                .WithProperty(AbilityProperties.ShieldHp, PropertyOp.Add, 10f, 10f);
            shield.MaxStacks = 2;
            ModifierInstance instance = system.Modifiers.Apply(shield, unit.Id, unit.Id).Instance;
            EventLog log = new EventLog(system);
            bool stacked = false;
            system.Events.Subscribe(AbilityEvents.ShieldAbsorbed, _ =>
            {
                if (!stacked)
                {
                    stacked = true;
                    system.Modifiers.Apply(shield, unit.Id, unit.Id);
                }
            });

            DamageResult first = DamageService.ApplyDamage(system, UnitId.None, unit.Id, 10f,
                AbilityDamageTypes.Physical);

            Assert.That(first.Absorbed, Is.EqualTo(10f));
            Assert.That(instance.IsActive, Is.True);
            Assert.That(instance.Stacks, Is.EqualTo(2));
            Assert.That(DamageService.ShieldCapacity(instance), Is.EqualTo(20f));
            Assert.That(log.Count(AbilityEvents.ShieldBroken), Is.Zero);
            Assert.That(log.Count(AbilityEvents.ModifierRemoved), Is.Zero);

            DamageResult second = DamageService.ApplyDamage(system, UnitId.None, unit.Id, 5f,
                AbilityDamageTypes.Physical);

            Assert.That(second.Absorbed, Is.EqualTo(5f));
            Assert.That(instance.IsActive, Is.True);
            Assert.That(unit.Health, Is.EqualTo(100f));
            Assert.That(log.Count(AbilityEvents.ShieldBroken), Is.Zero);
        }

        [Test]
        public void ShieldAbsorbedCallback_DispellingCurrentShieldDoesNotPublishBreak()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit unit = AbilityTestSupport.CreateUnit(system, health: 100f);
            ModifierBlueprint shield = AbilityTestSupport.Modifier("dispelled_shield")
                .WithProperty(AbilityProperties.ShieldHp, PropertyOp.Add, 10f);
            ModifierInstance instance = system.Modifiers.Apply(shield, unit.Id, unit.Id).Instance;
            EventLog log = new EventLog(system);
            system.Events.Subscribe(AbilityEvents.ShieldAbsorbed, _ => system.Modifiers.Remove(instance));

            DamageResult result = DamageService.ApplyDamage(system, UnitId.None, unit.Id, 10f,
                AbilityDamageTypes.Physical);

            Assert.That(result.Absorbed, Is.EqualTo(10f));
            Assert.That(instance.IsActive, Is.False);
            Assert.That(unit.Health, Is.EqualTo(100f));
            Assert.That(log.Count(AbilityEvents.ShieldBroken), Is.Zero);
            Assert.That(log.Count(AbilityEvents.ModifierRemoved), Is.EqualTo(1));
        }

        [Test]
        public void ShieldAbsorbedCallback_NestedDamagePublishesOnlyOneBreakForTheInstance()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit unit = AbilityTestSupport.CreateUnit(system, health: 100f);
            ModifierBlueprint shield = AbilityTestSupport.Modifier("nested_shield")
                .WithProperty(AbilityProperties.ShieldHp, PropertyOp.Add, 100f);
            ModifierInstance instance = system.Modifiers.Apply(shield, unit.Id, unit.Id).Instance;
            EventLog log = new EventLog(system);
            bool nestedHit = false;
            system.Events.Subscribe(AbilityEvents.ShieldAbsorbed, _ =>
            {
                if (!nestedHit)
                {
                    nestedHit = true;
                    DamageService.ApplyDamage(system, UnitId.None, unit.Id, 90f, AbilityDamageTypes.Physical);
                }
            });

            DamageResult result = DamageService.ApplyDamage(system, UnitId.None, unit.Id, 10f,
                AbilityDamageTypes.Physical);

            Assert.That(result.Absorbed, Is.EqualTo(10f));
            Assert.That(instance.IsActive, Is.False);
            Assert.That(unit.Health, Is.EqualTo(100f));
            Assert.That(log.Count(AbilityEvents.ShieldAbsorbed), Is.EqualTo(2));
            Assert.That(log.Count(AbilityEvents.ShieldBroken), Is.EqualTo(1));
            Assert.That(log.Count(AbilityEvents.ModifierRemoved), Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ShieldBrokenReapplication_ReplacesExhaustedInstanceAndPreservesOwnReaction(bool ownReaction)
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit unit = AbilityTestSupport.CreateUnit(system, health: 100f);
            ModifierBlueprint shield = AbilityTestSupport.Modifier("renewing_shield")
                .WithProperty(AbilityProperties.ShieldHp, PropertyOp.Add, 10f);
            if (ownReaction)
            {
                shield.EventReactions.Add(new ModifierEventReaction
                {
                    EventId = AbilityEvents.ShieldBroken,
                    Effects = AbilityTestSupport.Nodes(new EffectNodeData
                    {
                        OpId = AbilityEffectOps.ApplyModifier,
                        ModifierId = shield.Id
                    })
                });
            }
            else
            {
                system.Events.Subscribe(AbilityEvents.ShieldBroken, _ =>
                    system.ApplyModifier(shield.Id, unit.Id, unit.Id));
            }

            system.RegisterModifier(shield);
            ModifierInstance original = system.ApplyModifier(shield.Id, unit.Id, unit.Id).Instance;
            EventLog log = new EventLog(system);

            DamageResult first = DamageService.ApplyDamage(system, UnitId.None, unit.Id, 10f,
                AbilityDamageTypes.Physical);

            List<ModifierInstance> active = AbilityTestSupport.ActiveModifiers(system, unit.Id);
            Assert.That(first.Absorbed, Is.EqualTo(10f));
            Assert.That(original.IsActive, Is.False);
            Assert.That(active.Count, Is.EqualTo(1));
            Assert.That(active[0], Is.Not.SameAs(original));
            Assert.That(active[0].IsActive, Is.True);
            Assert.That(log.Count(AbilityEvents.ShieldBroken), Is.EqualTo(1));
            Assert.That(log.Count(AbilityEvents.ModifierRemoved), Is.EqualTo(1));

            DamageResult second = DamageService.ApplyDamage(system, UnitId.None, unit.Id, 5f,
                AbilityDamageTypes.Physical);

            Assert.That(second.Absorbed, Is.EqualTo(5f));
            Assert.That(unit.Health, Is.EqualTo(100f));
            Assert.That(active[0].IsActive, Is.True);
            Assert.That(log.Count(AbilityEvents.ShieldBroken), Is.EqualTo(1));
            Assert.That(log.Count(AbilityEvents.ModifierRemoved), Is.EqualTo(1));
        }

        [TestCase(1, 3)]
        [TestCase(2, 1)]
        public void CatalogReplacement_ExistingGrantKeepsItsCostsEffectsCooldownAndCharges(int originalCharges,
            int replacementCharges)
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, team: 1, mana: 100f);
            AbilityUnit target = AbilityTestSupport.CreateUnit(system, team: 2, health: 1000f);
            AbilityBlueprint original = new AbilityBlueprint
            {
                Id = "retained_grant",
                Targeting = TargetingMode.Unit,
                Cooldown = 6f,
                ChargeRestoreTime = 6f,
                MaxCharges = originalCharges,
                Costs = { new AbilityCost(AbilityResourceIds.Mana, 10f) }
            };
            original.ImpactEffects.Add(AbilityTestSupport.DamageNode(10f));
            system.RegisterAbility(original);
            AbilitySlot slot = system.GrantAbility(caster.Id, original.Id);
            slot.Level = 3;
            Assert.That(system.Cast(CastRequest.AtUnit(caster.Id, original.Id, target.Id)).Success, Is.True);

            AbilityBlueprint replacement = new AbilityBlueprint
            {
                Id = "RETAINED_GRANT",
                Targeting = TargetingMode.Unit,
                Cooldown = 1f,
                ChargeRestoreTime = 1f,
                MaxCharges = replacementCharges,
                Costs = { new AbilityCost(AbilityResourceIds.Mana, 40f) }
            };
            replacement.ImpactEffects.Add(AbilityTestSupport.DamageNode(40f));
            system.RegisterAbility(replacement);

            Assert.That(system.TryGetAbility(original.Id, out AbilityBlueprint catalogEntry), Is.True);
            Assert.That(catalogEntry, Is.SameAs(replacement));
            Assert.That(system.GrantAbility(caster.Id, replacement.Id), Is.SameAs(slot));
            Assert.That(slot.Blueprint, Is.SameAs(original));
            Assert.That(slot.Level, Is.EqualTo(3));
            Assert.That(slot.CooldownRemaining, Is.EqualTo(6f));
            system.Tick(6f);

            CastResult result = system.Cast(CastRequest.AtUnit(caster.Id, replacement.Id, target.Id));

            Assert.That(result.Success, Is.True);
            Assert.That(caster.Resources.GetCurrent(AbilityResourceIds.Mana), Is.EqualTo(80f));
            Assert.That(target.Health, Is.EqualTo(980f));
            Assert.That(slot.CooldownRemaining, Is.EqualTo(6f));
            Assert.That(slot.NormalizedCooldown, Is.EqualTo(1f));
            Assert.That(slot.Charges, Is.EqualTo(originalCharges - 1));
            if (originalCharges > 1)
            {
                Assert.That(system.Cast(CastRequest.AtUnit(caster.Id, original.Id, target.Id)).Success, Is.True);
                Assert.That(caster.Resources.GetCurrent(AbilityResourceIds.Mana), Is.EqualTo(70f));
                Assert.That(target.Health, Is.EqualTo(970f));
            }

            CastResult unavailable = system.Cast(CastRequest.AtUnit(caster.Id, original.Id, target.Id));
            Assert.That(unavailable.Failure, Is.EqualTo(originalCharges > 1
                ? CastFailureReason.NoCharges
                : CastFailureReason.OnCooldown));
        }

        [Test]
        public void CatalogReplacement_FutureGrantAndExplicitRegrantUseTheReplacement()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, team: 1, mana: 100f);
            AbilityUnit target = AbilityTestSupport.CreateUnit(system, team: 2, health: 1000f);
            AbilityBlueprint original = new AbilityBlueprint
            {
                Id = "updated_grant",
                Targeting = TargetingMode.Unit,
                Cooldown = 7f,
                Costs = { new AbilityCost(AbilityResourceIds.Mana, 5f) }
            };
            original.ImpactEffects.Add(AbilityTestSupport.DamageNode(5f));
            system.RegisterAbility(original);
            AbilitySlot oldSlot = system.GrantAbility(caster.Id, original.Id);
            oldSlot.Level = 4;
            Assert.That(system.Cast(CastRequest.AtUnit(caster.Id, original.Id, target.Id)).Success, Is.True);

            AbilityBlueprint replacement = new AbilityBlueprint
            {
                Id = original.Id,
                Targeting = TargetingMode.Unit,
                Cooldown = 2f,
                ChargeRestoreTime = 2f,
                MaxCharges = 3,
                Costs = { new AbilityCost(AbilityResourceIds.Mana, 20f) }
            };
            replacement.ImpactEffects.Add(AbilityTestSupport.DamageNode(25f));
            system.RegisterAbility(replacement);
            AbilityUnit newCaster = AbilityTestSupport.CreateUnit(system, team: 1, mana: 100f);
            AbilitySlot futureGrant = system.GrantAbility(newCaster.Id, original.Id);

            Assert.That(system.GrantAbility(caster.Id, original.Id), Is.SameAs(oldSlot));
            Assert.That(futureGrant.Blueprint, Is.SameAs(replacement));
            Assert.That(system.RevokeAbility(caster.Id, original.Id), Is.True);
            AbilitySlot regranted = system.GrantAbility(caster.Id, original.Id);
            Assert.That(regranted, Is.Not.SameAs(oldSlot));
            Assert.That(regranted.Blueprint, Is.SameAs(replacement));
            Assert.That(regranted.Level, Is.EqualTo(1));
            Assert.That(regranted.CooldownRemaining, Is.Zero);
            Assert.That(regranted.Charges, Is.EqualTo(3));

            Assert.That(system.Cast(CastRequest.AtUnit(caster.Id, original.Id, target.Id)).Success, Is.True);
            Assert.That(system.Cast(CastRequest.AtUnit(newCaster.Id, original.Id, target.Id)).Success, Is.True);

            Assert.That(caster.Resources.GetCurrent(AbilityResourceIds.Mana), Is.EqualTo(75f));
            Assert.That(newCaster.Resources.GetCurrent(AbilityResourceIds.Mana), Is.EqualTo(80f));
            Assert.That(target.Health, Is.EqualTo(945f));
            Assert.That(regranted.CooldownRemaining, Is.EqualTo(2f));
            Assert.That(futureGrant.CooldownRemaining, Is.EqualTo(2f));
            Assert.That(regranted.Charges, Is.EqualTo(2));
            Assert.That(futureGrant.Charges, Is.EqualTo(2));
            system.Tick(2f);
            Assert.That(regranted.Charges, Is.EqualTo(3));
            Assert.That(futureGrant.Charges, Is.EqualTo(3));
        }

        [Test]
        public void CatalogReplacement_ProjectileUsesTheGrantedDeliveryAndImpact()
        {
            AbilitySystem system = new AbilitySystem();
            FakeWorldAdapter world = new FakeWorldAdapter();
            system.World = world;
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, team: 1, mana: 100f);
            AbilityUnit target = AbilityTestSupport.CreateUnit(system, team: 2, health: 100f);
            world.SetPosition(caster.Id, Vector3.zero);
            world.SetPosition(target.Id, Vector3.right);
            AbilityBlueprint original = new AbilityBlueprint
            {
                Id = "retained_projectile",
                Targeting = TargetingMode.Unit,
                Delivery = AbilityDeliveryType.Projectile,
                ProjectileArchetypeId = "original_projectile",
                Cooldown = 5f,
                Costs = { new AbilityCost(AbilityResourceIds.Mana, 10f) }
            };
            original.ImpactEffects.Add(AbilityTestSupport.DamageNode(10f));
            system.RegisterAbility(original);
            AbilitySlot slot = system.GrantAbility(caster.Id, original.Id);
            AbilityBlueprint replacement = new AbilityBlueprint
            {
                Id = original.Id,
                Targeting = TargetingMode.Unit,
                Delivery = AbilityDeliveryType.Instant,
                Costs = { new AbilityCost(AbilityResourceIds.Mana, 30f) }
            };
            replacement.ImpactEffects.Add(AbilityTestSupport.DamageNode(40f));
            system.RegisterAbility(replacement);

            CastResult result = system.Cast(CastRequest.AtUnit(caster.Id, original.Id, target.Id));

            Assert.That(result.Success, Is.True);
            Assert.That(world.Spawns.Count, Is.EqualTo(1));
            Assert.That(world.Spawns[0].ArchetypeId, Is.EqualTo("original_projectile"));
            Assert.That(target.Health, Is.EqualTo(100f));
            Assert.That(caster.Resources.GetCurrent(AbilityResourceIds.Mana), Is.EqualTo(90f));
            Assert.That(slot.CooldownRemaining, Is.EqualTo(5f));
            Assert.That(system.NotifyProjectileHit(result.CastId, target.Id, Vector3.right), Is.True);
            Assert.That(target.Health, Is.EqualTo(90f));
        }

        [Test]
        public void EventsClear_RemovesUserSubscriptionsAndKeepsModifierReactions()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit attacker = AbilityTestSupport.CreateUnit(system, team: 1, health: 100f);
            AbilityUnit defender = AbilityTestSupport.CreateUnit(system, team: 2, health: 100f);
            ModifierBlueprint thorns = AbilityTestSupport.Modifier("clear_thorns");
            thorns.EventReactions.Add(new ModifierEventReaction
            {
                EventId = AbilityEvents.TakeDamage,
                TargetEventSource = true,
                Effects = AbilityTestSupport.Nodes(AbilityTestSupport.DamageNode(5f))
            });
            system.Modifiers.Apply(thorns, defender.Id, defender.Id);
            int specificCalls = 0;
            int anyCalls = 0;
            system.Events.Subscribe(AbilityEvents.TakeDamage, _ => specificCalls++);
            system.Events.SubscribeAny(_ => anyCalls++);
            system.Events.Clear();
            EventLog log = new EventLog(system);

            DamageService.ApplyDamage(system, attacker.Id, defender.Id, 10f, AbilityDamageTypes.Pure);

            Assert.That(defender.Health, Is.EqualTo(90f));
            Assert.That(attacker.Health, Is.EqualTo(95f));
            Assert.That(specificCalls, Is.Zero);
            Assert.That(anyCalls, Is.Zero);
            Assert.That(log.Count(AbilityEvents.TakeDamage), Is.EqualTo(2));
        }

        [Test]
        public void EventsClear_PreservesSpecificReactionAnyHandlerOrder()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit attacker = AbilityTestSupport.CreateUnit(system, team: 1, health: 100f);
            AbilityUnit defender = AbilityTestSupport.CreateUnit(system, team: 2, health: 100f);
            ModifierBlueprint thorns = AbilityTestSupport.Modifier("ordered_thorns");
            thorns.EventReactions.Add(new ModifierEventReaction
            {
                EventId = AbilityEvents.TakeDamage,
                TargetEventSource = true,
                Effects = AbilityTestSupport.Nodes(AbilityTestSupport.DamageNode(5f))
            });
            system.Modifiers.Apply(thorns, defender.Id, defender.Id);
            system.Events.Clear();
            List<string> order = new List<string>();
            system.Events.Subscribe(AbilityEvents.TakeDamage, receipt =>
            {
                if (receipt.Target == defender.Id)
                {
                    Assert.That(attacker.Health, Is.EqualTo(100f));
                    order.Add("specific");
                }
                else if (receipt.Target == attacker.Id)
                {
                    order.Add("reaction");
                }
            });
            system.Events.SubscribeAny(receipt =>
            {
                if (receipt.EventId == AbilityEvents.TakeDamage && receipt.Target == defender.Id)
                {
                    Assert.That(attacker.Health, Is.EqualTo(95f));
                    order.Add("any");
                }
            });

            DamageService.ApplyDamage(system, attacker.Id, defender.Id, 10f, AbilityDamageTypes.Pure);

            Assert.That(order, Is.EqualTo(new string[] { "specific", "reaction", "any" }));
        }

        [Test]
        public void NestedReactionClearingUsers_KeepsCurrentAnySnapshotAndFutureReactions()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit attacker = AbilityTestSupport.CreateUnit(system, team: 1, health: 100f);
            AbilityUnit defender = AbilityTestSupport.CreateUnit(system, team: 2, health: 100f);
            ModifierBlueprint thorns = AbilityTestSupport.Modifier("nested_clear_thorns");
            thorns.EventReactions.Add(new ModifierEventReaction
            {
                EventId = AbilityEvents.TakeDamage,
                TargetEventSource = true,
                Effects = AbilityTestSupport.Nodes(AbilityTestSupport.DamageNode(5f))
            });
            system.Modifiers.Apply(thorns, defender.Id, defender.Id);
            int nestedClears = 0;
            int outerAnyCalls = 0;
            system.Events.Subscribe(AbilityEvents.TakeDamage, receipt =>
            {
                if (receipt.Target == attacker.Id)
                {
                    nestedClears++;
                    system.Events.Clear();
                }
            });
            system.Events.SubscribeAny(receipt =>
            {
                if (receipt.EventId == AbilityEvents.TakeDamage && receipt.Target == defender.Id)
                {
                    outerAnyCalls++;
                }
            });

            DamageService.ApplyDamage(system, attacker.Id, defender.Id, 10f, AbilityDamageTypes.Pure);
            Assert.That(outerAnyCalls, Is.EqualTo(1), "The outer publish already captured its any-handler snapshot.");
            DamageService.ApplyDamage(system, attacker.Id, defender.Id, 10f, AbilityDamageTypes.Pure);

            Assert.That(defender.Health, Is.EqualTo(80f));
            Assert.That(attacker.Health, Is.EqualTo(90f));
            Assert.That(nestedClears, Is.EqualTo(1));
            Assert.That(outerAnyCalls, Is.EqualTo(1), "Later publications see the cleared user lists.");
        }

        [Test]
        public void StandaloneEventBus_ClearRemovesUsersAndKeepsCurrentAnySnapshot()
        {
            AbilityEventBus events = new AbilityEventBus();
            AbilityEventArgs receipt = new AbilityEventArgs("standalone", UnitId.None, UnitId.None);
            int specificCalls = 0;
            int anyCalls = 0;
            events.Subscribe(receipt.EventId, _ => specificCalls++);
            events.SubscribeAny(_ => anyCalls++);
            events.Clear();
            events.Publish(receipt);
            Assert.That(specificCalls, Is.Zero);
            Assert.That(anyCalls, Is.Zero);
            List<string> delivered = new List<string>();
            events.SubscribeAny(_ =>
            {
                delivered.Add("first");
                events.Clear();
            });
            events.SubscribeAny(_ => delivered.Add("second"));

            events.Publish(receipt);
            events.Publish(receipt);

            Assert.That(delivered, Is.EqualTo(new string[] { "first", "second" }));
        }

        [Test]
        public void NestedEffectContext_KeepsTheCastReceiptId()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit unit = AbilityTestSupport.CreateUnit(system, health: 100f);
            EffectContext parent = AbilityTestSupport.Context(system, unit.Id);
            parent.CastId = 83;
            EffectContext nested = parent.CreateNested(unit.Id, "nested", new List<UnitId> { unit.Id });
            EventLog log = new EventLog(system);

            system.ExecuteEffects(AbilityTestSupport.Nodes(AbilityTestSupport.DamageNode(5f)), nested);

            Assert.That(log.TryGetLast(AbilityEvents.TakeDamage, out AbilityEventArgs receipt), Is.True);
            Assert.That(receipt.CastId, Is.EqualTo(83u));
        }

        [Test]
        public void ModifierReaction_KeepsTheTriggeringCastReceiptId()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit attacker = AbilityTestSupport.CreateUnit(system, team: 1);
            AbilityUnit defender = AbilityTestSupport.CreateUnit(system, team: 2);
            ModifierBlueprint thorns = AbilityTestSupport.Modifier("thorns");
            thorns.EventReactions.Add(new ModifierEventReaction
            {
                EventId = AbilityEvents.TakeDamage,
                TargetEventSource = true,
                Effects = AbilityTestSupport.Nodes(AbilityTestSupport.DamageNode(5f))
            });
            system.Modifiers.Apply(thorns, defender.Id, defender.Id);
            AbilityBlueprint ability = new AbilityBlueprint { Id = "hit", Targeting = TargetingMode.Unit };
            ability.ImpactEffects.Add(AbilityTestSupport.DamageNode(10f));
            system.RegisterAbility(ability);
            system.GrantAbility(attacker.Id, ability.Id);
            EventLog log = new EventLog(system);

            CastResult result = system.Cast(CastRequest.AtUnit(attacker.Id, ability.Id, defender.Id));

            Assert.That(result.Success, Is.True);
            AbilityEventArgs reflection = log.Events.Find(receipt =>
                receipt.EventId == AbilityEvents.TakeDamage && receipt.Target == attacker.Id);
            Assert.That(reflection.Amount, Is.EqualTo(5f));
            Assert.That(reflection.CastId, Is.EqualTo(result.CastId));
        }
    }
}
