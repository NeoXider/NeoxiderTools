using System.Collections.Generic;
using Neo.Abilities.Fixed;
using NUnit.Framework;
using static Neo.Editor.Tests.Abilities.Fixed.FixedTestSupport;

namespace Neo.Editor.Tests.Abilities.Fixed
{
    /// <summary>
    ///     The integer core's leveled and scalable parameters: cast parameters by level and the level-change rules,
    ///     leveled costs with the UpTo / All modes, the spent term (instant, projectile, DoT capture and its state
    ///     hash), multiplicative and resource-stat scaling, modifier strength by level and node extras by level.
    /// </summary>
    public sealed class FixedLeveledParameterTests
    {
        private FixedAbilitySystem _system;
        private FixedRecordingWorld _world;
        private FixedUnit _caster;
        private FixedUnit _enemy;

        [SetUp]
        public void SetUp()
        {
            _system = NewSystem();
            _world = new FixedRecordingWorld(_system);
            _system.World = _world;
            _caster = Unit(_system, 1, mana: 200d);
            _enemy = Unit(_system, 2, x: 3d);
        }

        private FixedAbilitySlot Grant(FixedAbilityBlueprint ability)
        {
            _system.RegisterAbility(ability);
            return _system.GrantAbility(_caster.Id, ability.Id);
        }

        private bool CastAtEnemy(string abilityId)
        {
            FixedCastRequest request = FixedCastRequest.AtUnit(_caster.Id, abilityId, _enemy.Id);
            return _system.Cast(in request).Success;
        }

        private bool CastNoTarget(string abilityId)
        {
            FixedCastRequest request = FixedCastRequest.NoTarget(_caster.Id, abilityId);
            return _system.Cast(in request).Success;
        }

        private static FixedEffectNode SpentDamage(double coefficient)
        {
            FixedEffectNode node = Node(FixedOps.Damage);
            node.AmountSpentResource = FixedResources.Mana;
            node.AmountSpentCoefficientRaw = Q(coefficient);
            return node;
        }

        [Test]
        public void CooldownByLevel_IsSampledAtTheSlotLevel_AndARunningCooldownKeepsItsTicks()
        {
            FixedAbilityBlueprint ability = Ability("fx_cd", FixedTargeting.NoTarget);
            ability.CooldownTicks = 300;
            ability.CooldownByLevelTicks = new[] { 300, 240, 180 };
            FixedAbilitySlot slot = Grant(ability);

            Assert.That(CastNoTarget("fx_cd"), Is.True);
            Assert.That(slot.CooldownRemainingTicks, Is.EqualTo(300));
            _system.Step(100);
            _system.SetAbilityLevel(_caster.Id, "fx_cd", 3);
            Assert.That(slot.CooldownRemainingTicks, Is.EqualTo(200), "the running cooldown is not rescaled");
            _system.Step(200);
            Assert.That(CastNoTarget("fx_cd"), Is.True);
            Assert.That(slot.CooldownRemainingTicks, Is.EqualTo(180));
        }

        [Test]
        public void RangeAndProjectileSpeedByLevel_GateAndReachTheSpawn()
        {
            FixedAbilityBlueprint ability = Ability("fx_range", FixedTargeting.Unit, Node(FixedOps.Damage, 1d));
            ability.RangeRaw = Q(2d);
            ability.RangeByLevelRaw = new[] { Q(2d), Q(4d) };
            ability.Delivery = FixedDelivery.Projectile;
            ability.ProjectileArchetypeId = "bolt";
            ability.ProjectileSpeedRaw = Q(10d);
            ability.ProjectileSpeedByLevelRaw = new[] { Q(10d), Q(25d) };
            FixedAbilitySlot slot = Grant(ability);

            Assert.That(CastAtEnemy("fx_range"), Is.False, "3 units away, range 2 at level 1");
            _system.SetAbilityLevel(_caster.Id, "fx_range", 2);
            Assert.That(slot.RangeRaw, Is.EqualTo(Q(4d)));
            Assert.That(CastAtEnemy("fx_range"), Is.True);
            Assert.That(_world.Spawns[0].MagnitudeRaw, Is.EqualTo(Q(25d)));
        }

        [Test]
        public void MaxChargesByLevel_RaisedCapGrantsCharges_LoweredCapClamps()
        {
            FixedAbilityBlueprint ability = Ability("fx_charges", FixedTargeting.NoTarget);
            ability.CooldownTicks = 150;
            ability.ChargeRestoreTicks = 120;
            ability.MaxChargesByLevel = new[] { 1, 3, 2 };
            FixedAbilitySlot slot = Grant(ability);
            Assert.That(slot.Charges, Is.EqualTo(1));

            _system.SetAbilityLevel(_caster.Id, "fx_charges", 2);
            Assert.That(slot.Charges, Is.EqualTo(3));
            Assert.That(CastNoTarget("fx_charges"), Is.True);
            Assert.That(CastNoTarget("fx_charges"), Is.True);
            Assert.That(slot.CooldownRemainingTicks, Is.EqualTo(120));

            _system.SetAbilityLevel(_caster.Id, "fx_charges", 3);
            Assert.That(slot.Charges, Is.EqualTo(1));
            _system.Step(120);
            Assert.That(slot.Charges, Is.EqualTo(2));
            Assert.That(slot.CooldownRemainingTicks, Is.EqualTo(0));

            Assert.That(CastNoTarget("fx_charges"), Is.True);
            _system.SetAbilityLevel(_caster.Id, "fx_charges", 1);
            Assert.That(slot.Charges, Is.EqualTo(1));
            Assert.That(slot.CooldownRemainingTicks, Is.EqualTo(0));
            Assert.That(slot.IsReady, Is.True);
        }

        [Test]
        public void ChargeRestoreByLevel_ZeroUsesTheLevelCooldown()
        {
            FixedAbilityBlueprint ability = new FixedAbilityBlueprint
            {
                MaxCharges = 2, ChargeRestoreTicks = 60, ChargeRestoreByLevelTicks = new[] { 60, 0 },
                CooldownByLevelTicks = new[] { 90, 45 }
            };

            Assert.That(ability.ResolveChargeRestoreTicks(1), Is.EqualTo(60));
            Assert.That(ability.ResolveChargeRestoreTicks(2), Is.EqualTo(45));
            Assert.That(ability.ResolveCooldownTicks(7), Is.EqualTo(45));
        }

        [Test]
        public void CostByLevelAndKey_PayTheLevelAmountAndAQuarterOfMaxMana()
        {
            FixedAbilityBlueprint leveled = Ability("fx_cost_lv", FixedTargeting.Unit, Node(FixedOps.Damage, 1d));
            leveled.Costs.Add(new FixedAbilityCost(FixedResources.Mana, Q(10d)) { AmountByLevelRaw = new[] { Q(10d), Q(30d) } });
            Grant(leveled);
            _system.SetAbilityLevel(_caster.Id, "fx_cost_lv", 2);
            Assert.That(CastAtEnemy("fx_cost_lv"), Is.True);
            Assert.That(_caster.ManaRaw, Is.EqualTo(Q(170d)));

            FixedAbilityBlueprint keyed = Ability("fx_cost_key", FixedTargeting.Unit, Node(FixedOps.Damage, 1d));
            keyed.Costs.Add(new FixedAbilityCost(FixedResources.Mana, 0) { AmountKey = "cost" });
            keyed.Specials.Add(new FixedAbilitySpecial
            {
                Name = "cost",
                Value = new FixedLeveledValue
                {
                    ScaleProperty = FixedResources.Mana, ScaleStat = FixedScaleStat.ResourceMax, ScalePerPointRaw = Q(0.25d)
                }
            });
            Grant(keyed);
            Assert.That(CastAtEnemy("fx_cost_key"), Is.True);
            Assert.That(_caster.ManaRaw, Is.EqualTo(Q(120d)), "25% of 200 max mana");
        }

        [Test]
        public void UpToAndAll_SpendWhatIsAvailable_AndTheSpentTermDoublesIt()
        {
            FixedAbilityBlueprint upTo = Ability("fx_upto", FixedTargeting.Unit, SpentDamage(1d));
            upTo.Costs.Add(new FixedAbilityCost(FixedResources.Mana, Q(150d), FixedCostMode.UpTo, Q(30d)));
            Grant(upTo);
            Assert.That(CastAtEnemy("fx_upto"), Is.True);
            Assert.That(_caster.ManaRaw, Is.EqualTo(Q(50d)));
            Assert.That(_enemy.HealthRaw, Is.EqualTo(Q(850d)));

            _caster.SetCurrent(FixedResources.Mana, Q(20d));
            Assert.That(CastAtEnemy("fx_upto"), Is.False, "below the minimum");
            Assert.That(_caster.ManaRaw, Is.EqualTo(Q(20d)));

            FixedAbilityBlueprint all = Ability("fx_all", FixedTargeting.Unit, SpentDamage(2d));
            all.Costs.Add(new FixedAbilityCost(FixedResources.Mana, 0, FixedCostMode.All, Q(1d)));
            Grant(all);
            Assert.That(CastAtEnemy("fx_all"), Is.True);
            Assert.That(_caster.ManaRaw, Is.EqualTo(0));
            Assert.That(_enemy.HealthRaw, Is.EqualTo(Q(810d)), "2 x 20 spent");
            Assert.That(CastAtEnemy("fx_all"), Is.False, "nothing left for the minimum of 1");
        }

        [Test]
        public void SpentAmounts_TravelWithTheProjectile()
        {
            FixedAbilityBlueprint bolt = Ability("fx_bolt_all", FixedTargeting.Unit, SpentDamage(2d));
            bolt.Delivery = FixedDelivery.Projectile;
            bolt.ProjectileArchetypeId = "bolt";
            bolt.Costs.Add(new FixedAbilityCost(FixedResources.Mana, 0, FixedCostMode.All));
            Grant(bolt);
            _caster.SetCurrent(FixedResources.Mana, Q(40d));

            Assert.That(CastAtEnemy("fx_bolt_all"), Is.True);
            _system.Step(5);
            Assert.That(_enemy.HealthRaw, Is.EqualTo(Q(1000d)));
            Assert.That(_system.NotifyProjectileHit(_world.Spawns[0].CastId, _enemy.Id, _enemy.Position), Is.True);
            Assert.That(_enemy.HealthRaw, Is.EqualTo(Q(920d)));
        }

        [Test]
        public void SpentAmounts_AreCapturedByScalingModifiers_AndTicksScaleWithThem()
        {
            ulong first = PlayBurn(60d, 0.5d, out int firstSpentCount, out int slowSpentCount);
            ulong second = PlayBurn(60d, 0.5d, out _, out _);

            Assert.That(firstSpentCount, Is.EqualTo(1));
            Assert.That(slowSpentCount, Is.EqualTo(0), "a modifier that does not scale with it captures nothing");
            Assert.That(second, Is.EqualTo(first), "deterministic");
        }

        [Test]
        public void StateHash_FoldsTheCapturedSpentAmount()
        {
            // WHY: a zero coefficient and a drained pool leave the captured amount as the only difference.
            ulong sixty = PlayBurn(60d, 0d, out _, out _);
            ulong eighty = PlayBurn(80d, 0d, out _, out _);

            Assert.That(eighty, Is.Not.EqualTo(sixty));
        }

        private static ulong PlayBurn(double mana, double coefficient, out int burnSpentCount, out int slowSpentCount)
        {
            FixedAbilitySystem system = NewSystem();
            FixedUnit caster = Unit(system, 1, mana: 100d);
            FixedUnit enemy = Unit(system, 2, x: 1d);
            FixedModifierBlueprint burn = Modifier("fx_burn", 90);
            burn.TickIntervalTicks = 30;
            burn.TickEffects.Add(SpentDamage(coefficient));
            FixedModifierBlueprint slow = Modifier("fx_slow", 90).WithProperty(FixedProperties.MoveSpeed,
                FixedPropertyOp.Mul, 128);
            system.RegisterModifier(burn);
            system.RegisterModifier(slow);
            FixedAbilityBlueprint ability = Ability("fx_burn_all", FixedTargeting.Unit,
                new FixedEffectNode { OpId = FixedOps.ApplyModifier, ModifierId = "fx_burn" },
                new FixedEffectNode { OpId = FixedOps.ApplyModifier, ModifierId = "fx_slow" });
            ability.Costs.Add(new FixedAbilityCost(FixedResources.Mana, 0, FixedCostMode.All));
            system.RegisterAbility(ability);
            system.GrantAbility(caster.Id, "fx_burn_all");
            caster.SetCurrent(FixedResources.Mana, Q(mana));
            FixedCastRequest request = FixedCastRequest.AtUnit(caster.Id, "fx_burn_all", enemy.Id);
            system.Cast(in request);

            system.Modifiers.TryGetActive(enemy.Id, "fx_burn", out FixedModifierInstance burning);
            system.Modifiers.TryGetActive(enemy.Id, "fx_slow", out FixedModifierInstance slowed);
            burnSpentCount = burning.SpentCount;
            slowSpentCount = slowed.SpentCount;
            caster.SetCurrent(FixedResources.Mana, 0);
            system.Step(60);
            Assert.That(enemy.HealthRaw, Is.EqualTo(Q(1000d - 2d * coefficient * mana)), "two ticks of k x spent");
            return FixedStateHasher.Compute(system);
        }

        [Test]
        public void MultiplyAndResourceStats_ScaleTheValue()
        {
            // WHY: a custom stat, because spell_power also amplifies damage in the damage formula.
            _caster.SetBaseProperty("focus", Q(2d));
            FixedAbilityBlueprint ability = Ability("fx_mul", FixedTargeting.Unit);
            FixedEffectNode node = Node(FixedOps.Damage, 10d);
            node.AmountScaleProperty = "focus";
            node.AmountScalePerPointRaw = Q(0.25d);
            node.AmountScaleMode = FixedScaleMode.Multiply;
            node.AmountSpentResource = FixedResources.Mana;
            node.AmountSpentCoefficientRaw = Q(1d);
            ability.ImpactEffects.Add(node);
            ability.Costs.Add(new FixedAbilityCost(FixedResources.Mana, Q(30d)));
            Grant(ability);

            Assert.That(CastAtEnemy("fx_mul"), Is.True);
            Assert.That(_enemy.HealthRaw, Is.EqualTo(Q(940d)), "(10 + 30) x (1 + 0.25 x 2)");

            FixedAbilityBlueprint missing = Ability("fx_missing", FixedTargeting.Unit);
            FixedEffectNode execute = Node(FixedOps.Damage);
            execute.AmountScaleProperty = FixedResources.Health;
            execute.AmountScaleStat = FixedScaleStat.ResourceMissing;
            execute.AmountScaleSource = FixedScaleSource.Target;
            execute.AmountScalePerPointRaw = Q(0.5d);
            missing.ImpactEffects.Add(execute);
            Grant(missing);
            Assert.That(CastAtEnemy("fx_missing"), Is.True);
            Assert.That(_enemy.HealthRaw, Is.EqualTo(Q(910d)), "half of the 60 missing health");
        }

        [Test]
        public void SlowByLevel_UsesTheCapturedLevel()
        {
            FixedModifierBlueprint slow = Modifier("fx_frost", 60);
            slow.Properties.Add(new FixedPropertyContribution(FixedProperties.MoveSpeed, FixedPropertyOp.Mul, 80, 0, 100)
                { ValueByLevelRaw = new[] { 80, 70, 60 } });
            _system.RegisterModifier(slow);
            Grant(Ability("fx_frost_cast", FixedTargeting.Unit,
                new FixedEffectNode { OpId = FixedOps.ApplyModifier, ModifierId = "fx_frost" }));
            _enemy.SetBaseProperty(FixedProperties.MoveSpeed, Q(10d));

            int[] expected = { Q(8d), Q(7d), Q(6d) };
            for (int level = 1; level <= 3; level++)
            {
                _system.SetAbilityLevel(_caster.Id, "fx_frost_cast", level);
                Assert.That(CastAtEnemy("fx_frost_cast"), Is.True);
                Assert.That(_enemy.GetProperty(FixedProperties.MoveSpeed), Is.EqualTo(expected[level - 1]), "level " + level);
            }
        }

        [Test]
        public void ChanceTargetsAndZoneByLevel_AreSampledAtTheAbilityLevel()
        {
            List<FixedUnit> pack = new List<FixedUnit>();
            for (int i = 0; i < 3; i++)
            {
                pack.Add(Unit(_system, 2, z: 1d + i));
            }

            FixedAbilityBlueprint ability = Ability("fx_nodes", FixedTargeting.NoTarget);
            FixedEffectNode area = Node(FixedOps.Damage, 10d, FixedTargetSelector.AreaAroundCaster);
            area.RadiusRaw = Q(10d);
            area.MaxTargets = 1;
            area.MaxTargetsByLevel = new[] { 1, 2 };
            area.ChanceByLevelRaw = new[] { 0, FixedEffectNode.Always };
            ability.ImpactEffects.Add(area);
            ability.ImpactEffects.Add(new FixedEffectNode
            {
                OpId = FixedOps.Zone, Target = FixedTargetSelector.Caster, RadiusRaw = Q(2d), AbilityId = "fx_nodes_tick",
                DurationTicks = 60, DurationByLevelTicks = new[] { 60, 120 }, IntervalTicks = 30,
                IntervalByLevelTicks = new[] { 30, 15 }
            });
            _system.RegisterAbility(Ability("fx_nodes_tick", FixedTargeting.NoTarget, Node(FixedOps.Heal)));
            Grant(ability);

            Assert.That(CastNoTarget("fx_nodes"), Is.True);
            Assert.That(CountHurt(pack), Is.EqualTo(0), "0% at level 1");
            _system.RemoveZonesOf(_caster.Id);

            _system.SetAbilityLevel(_caster.Id, "fx_nodes", 2);
            Assert.That(CastNoTarget("fx_nodes"), Is.True);
            Assert.That(CountHurt(pack), Is.EqualTo(2), "100% at level 2 on the two nearest");
            List<FixedZone> zones = new List<FixedZone>();
            _system.GetZones(zones);
            Assert.That(zones.Count, Is.EqualTo(1));
            Assert.That(zones[0].DurationTicks, Is.EqualTo(120));
            Assert.That(zones[0].IntervalTicks, Is.EqualTo(15));
        }

        private static int CountHurt(List<FixedUnit> units)
        {
            int hurt = 0;
            for (int i = 0; i < units.Count; i++)
            {
                hurt += units[i].HealthRaw < Q(1000d) ? 1 : 0;
            }

            return hurt;
        }

        [Test]
        public void ContributionValueFor_FallsBackToValueRaw()
        {
            FixedPropertyContribution flat = new FixedPropertyContribution("armor", FixedPropertyOp.Add, Q(2d), Q(1d));
            FixedPropertyContribution leveled = flat;
            leveled.ValueByLevelRaw = new[] { Q(3d), Q(5d) };

            Assert.That(flat.ValueFor(3, 4), Is.EqualTo(flat.ValueForStacks(3)));
            Assert.That(leveled.ValueFor(2, 2), Is.EqualTo(Q(6d)), "5 at level 2 + 1 per extra stack");
        }
    }
}
