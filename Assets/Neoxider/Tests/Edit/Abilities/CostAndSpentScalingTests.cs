using System.Collections.Generic;
using Neo.Abilities;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     Leveled costs (AmountByLevel, AmountKey specials such as 10% of max mana), the UpTo / All spend modes, the spent
    ///     term that scales effects by what the cast actually paid (instant, projectile, DoT ticks), and the multiplicative
    ///     and resource-stat scaling terms.
    /// </summary>
    public sealed class CostAndSpentScalingTests
    {
        private AbilitySystem _system;
        private FakeWorldAdapter _world;
        private AbilityUnit _caster;
        private AbilityUnit _enemy;

        [SetUp]
        public void SetUp()
        {
            _system = new AbilitySystem();
            _world = new FakeWorldAdapter();
            _system.World = _world;
            _caster = AbilityTestSupport.CreateUnit(_system, team: 1, mana: 200f);
            _enemy = AbilityTestSupport.CreateUnit(_system, team: 2);
            _world.SetPosition(_caster.Id, Vector3.zero);
            _world.SetPosition(_enemy.Id, new Vector3(3f, 0f, 0f));
        }

        private float Mana => _caster.Resources.GetCurrent(AbilityResourceIds.Mana);

        private void Grant(AbilityBlueprint ability)
        {
            _system.RegisterAbility(ability);
            _system.GrantAbility(_caster.Id, ability.Id);
        }

        private bool CastAtEnemy(string abilityId)
        {
            return _system.Cast(CastRequest.AtUnit(_caster.Id, abilityId, _enemy.Id)).Success;
        }

        private static AbilityBlueprint Strike(string id, params AbilityCost[] costs)
        {
            AbilityBlueprint ability = new AbilityBlueprint { Id = id, Targeting = TargetingMode.Unit };
            ability.Costs.AddRange(costs);
            return ability;
        }

        private static EffectNodeData SpentDamage(float coefficient)
        {
            return new EffectNodeData
            {
                OpId = AbilityEffectOps.Damage,
                DamageType = AbilityDamageTypes.Pure,
                AmountSpentResource = AbilityResourceIds.Mana,
                AmountSpentCoefficient = coefficient
            };
        }

        [Test]
        public void CostAmountByLevel_PaysTheSlotLevelAmount()
        {
            AbilityCost cost = new AbilityCost(AbilityResourceIds.Mana, 50f) { AmountByLevel = new[] { 50f, 60f, 70f } };
            Grant(Strike("cost_lv", cost));
            _system.SetAbilityLevel(_caster.Id, "cost_lv", 3);

            Assert.That(CastAtEnemy("cost_lv"), Is.True);
            Assert.That(Mana, Is.EqualTo(130f).Within(1e-3f), "200 - 70 at level 3");
        }

        [Test]
        public void CostAmountKey_TenPercentOfMaxMana()
        {
            AbilityBlueprint ability = Strike("cost_pct",
                new AbilityCost(AbilityResourceIds.Mana, 0f) { AmountKey = "mana_cost" });
            ability.Specials.Add(new AbilitySpecialValue
            {
                Name = "mana_cost",
                Value = new LeveledValue
                {
                    ScaleProperty = AbilityResourceIds.Mana,
                    ScaleStat = ScaleAmountStat.ResourceMax,
                    ScalePerPoint = 0.1f
                }
            });
            Grant(ability);

            Assert.That(CastAtEnemy("cost_pct"), Is.True);
            Assert.That(Mana, Is.EqualTo(180f).Within(1e-3f), "10% of 200 max mana");
        }

        [Test]
        public void ManaCostMultiplier_StillAppliesToLeveledCosts()
        {
            AbilityCost cost = new AbilityCost(AbilityResourceIds.Mana, 40f) { AmountByLevel = new[] { 40f, 80f } };
            Grant(Strike("cost_mul", cost));
            _system.SetAbilityLevel(_caster.Id, "cost_mul", 2);
            _caster.SetBaseProperty(AbilityProperties.ManaCostMul, 0.5f);

            Assert.That(CastAtEnemy("cost_mul"), Is.True);
            Assert.That(Mana, Is.EqualTo(160f).Within(1e-3f), "80 at level 2, halved by mana_cost_mul");
        }

        [Test]
        public void UpTo_SpendsWhatIsAvailable_AndNeedsTheMinimum()
        {
            AbilityBlueprint ability = Strike("cost_upto",
                new AbilityCost(AbilityResourceIds.Mana, 150f, AbilityCostMode.UpTo, 30f));
            ability.ImpactEffects.Add(SpentDamage(1f));
            Grant(ability);

            Assert.That(CastAtEnemy("cost_upto"), Is.True);
            Assert.That(Mana, Is.EqualTo(50f).Within(1e-3f), "150 of 200");
            Assert.That(_enemy.Health, Is.EqualTo(850f).Within(1e-3f), "damage = 1 x spent 150");

            Assert.That(CastAtEnemy("cost_upto"), Is.True, "50 left is above the minimum of 30");
            Assert.That(Mana, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(_enemy.Health, Is.EqualTo(800f).Within(1e-3f), "only the 50 available were spent");

            _caster.Resources.SetCurrent(AbilityResourceIds.Mana, 20f);
            Assert.That(_system.Cast(CastRequest.AtUnit(_caster.Id, "cost_upto", _enemy.Id)).Failure,
                Is.EqualTo(CastFailureReason.NotEnoughResources), "20 is below the minimum");
            Assert.That(Mana, Is.EqualTo(20f).Within(1e-3f), "nothing was paid");
        }

        [Test]
        public void SpendAllMana_DealsTwiceTheSpentMana()
        {
            AbilityBlueprint ability = Strike("nuke_all",
                new AbilityCost(AbilityResourceIds.Mana, 0f, AbilityCostMode.All, 1f));
            ability.ImpactEffects.Add(SpentDamage(2f));
            Grant(ability);
            _caster.Resources.SetCurrent(AbilityResourceIds.Mana, 120f);

            Assert.That(CastAtEnemy("nuke_all"), Is.True);
            Assert.That(Mana, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(_enemy.Health, Is.EqualTo(760f).Within(1e-3f), "2 x 120 spent");
            Assert.That(CastAtEnemy("nuke_all"), Is.False, "the All cost needs at least 1 mana");
        }

        [Test]
        public void TwoCostsOfOneResource_AllTakesWhatTheFirstLeft()
        {
            AbilityBlueprint ability = Strike("cost_pair",
                new AbilityCost(AbilityResourceIds.Mana, 50f),
                new AbilityCost(AbilityResourceIds.Mana, 0f, AbilityCostMode.All));
            ability.ImpactEffects.Add(SpentDamage(1f));
            Grant(ability);

            Assert.That(CastAtEnemy("cost_pair"), Is.True);
            Assert.That(Mana, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(_enemy.Health, Is.EqualTo(800f).Within(1e-3f), "the spent term sums both entries: 50 + 150");
        }

        [Test]
        public void SpentAmounts_TravelWithTheProjectileToEveryHit()
        {
            AbilityBlueprint ability = Strike("bolt_all", new AbilityCost(AbilityResourceIds.Mana, 0f, AbilityCostMode.All));
            ability.Delivery = AbilityDeliveryType.Projectile;
            ability.ProjectileArchetypeId = "bolt";
            ability.ImpactEffects.Add(SpentDamage(2f));
            Grant(ability);
            _caster.Resources.SetCurrent(AbilityResourceIds.Mana, 40f);

            Assert.That(CastAtEnemy("bolt_all"), Is.True);
            _system.Tick(0.5f);
            Assert.That(_enemy.Health, Is.EqualTo(1000f), "no damage before the hit");

            uint castId = _world.Spawns[0].CastId;
            Assert.That(_system.NotifyProjectileHit(castId, _enemy.Id, new Vector3(3f, 0f, 0f)), Is.True);
            Assert.That(_enemy.Health, Is.EqualTo(920f).Within(1e-3f), "2 x 40 spent, applied on the delayed hit");
            Assert.That(_system.NotifyProjectileHit(castId, _enemy.Id, new Vector3(3f, 0f, 0f)), Is.True);
            Assert.That(_enemy.Health, Is.EqualTo(840f).Within(1e-3f), "a piercing second hit sees it too");
        }

        [Test]
        public void SpentAmounts_AreCapturedByAModifierWhoseTicksScaleWithThem()
        {
            ModifierBlueprint burn = AbilityTestSupport.Modifier("spent_burn", duration: 3f);
            burn.TickInterval = 1f;
            burn.TickEffects.Add(SpentDamage(0.5f));
            ModifierBlueprint slow = AbilityTestSupport.Modifier("plain_slow", duration: 3f)
                .WithProperty(AbilityProperties.MoveSpeed, PropertyOp.Mul, 0.5f);
            _system.RegisterModifier(burn);
            _system.RegisterModifier(slow);
            AbilityBlueprint ability = Strike("burn_all", new AbilityCost(AbilityResourceIds.Mana, 0f, AbilityCostMode.All));
            ability.ImpactEffects.Add(new EffectNodeData { OpId = AbilityEffectOps.ApplyModifier, ModifierId = "spent_burn" });
            ability.ImpactEffects.Add(new EffectNodeData { OpId = AbilityEffectOps.ApplyModifier, ModifierId = "plain_slow" });
            Grant(ability);
            _caster.Resources.SetCurrent(AbilityResourceIds.Mana, 60f);

            Assert.That(CastAtEnemy("burn_all"), Is.True);
            Assert.That(_system.Modifiers.TryGetActive(_enemy.Id, "spent_burn", out ModifierInstance burning), Is.True);
            Assert.That(burning.SpentCount, Is.EqualTo(1));
            Assert.That(burning.GetSpent(AbilityResourceIds.Mana), Is.EqualTo(60f).Within(1e-3f));
            Assert.That(_system.Modifiers.TryGetActive(_enemy.Id, "plain_slow", out ModifierInstance slowed), Is.True);
            Assert.That(slowed.SpentCount, Is.EqualTo(0), "a modifier that does not scale with it captures nothing");

            _system.Tick(1f);
            _system.Tick(1f);
            Assert.That(_enemy.Health, Is.EqualTo(940f).Within(1e-3f), "two ticks of 0.5 x 60 spent");
        }

        [Test]
        public void ReapplicationWithoutPaidAmounts_ClearsTheCapture()
        {
            ModifierBlueprint burn = AbilityTestSupport.Modifier("spent_burn_re", duration: 5f);
            burn.TickInterval = 1f;
            burn.TickEffects.Add(SpentDamage(1f));
            _system.RegisterModifier(burn);
            AbilityBlueprint ability = Strike("burn_re", new AbilityCost(AbilityResourceIds.Mana, 30f));
            ability.ImpactEffects.Add(new EffectNodeData { OpId = AbilityEffectOps.ApplyModifier, ModifierId = "spent_burn_re" });
            Grant(ability);

            Assert.That(CastAtEnemy("burn_re"), Is.True);
            _system.Modifiers.TryGetActive(_enemy.Id, "spent_burn_re", out ModifierInstance instance);
            Assert.That(instance.GetSpent(AbilityResourceIds.Mana), Is.EqualTo(30f).Within(1e-3f));

            _system.ApplyModifier("spent_burn_re", _caster.Id, _enemy.Id, "burn_re");
            Assert.That(instance.SpentCount, Is.EqualTo(0), "the latest application wins, like the ability level");
        }

        [Test]
        public void SpecialWithASpentTerm_IsResolvedThroughAmountKey()
        {
            AbilityBlueprint ability = Strike("key_spent", new AbilityCost(AbilityResourceIds.Mana, 25f));
            ability.Specials.Add(new AbilitySpecialValue
            {
                Name = "damage",
                Value = new LeveledValue
                    { Base = 10f, SpentResource = AbilityResourceIds.Mana, SpentCoefficient = 4f }
            });
            ability.ImpactEffects.Add(new EffectNodeData
                { OpId = AbilityEffectOps.Damage, DamageType = AbilityDamageTypes.Pure, AmountKey = "damage" });
            Grant(ability);

            Assert.That(CastAtEnemy("key_spent"), Is.True);
            Assert.That(_enemy.Health, Is.EqualTo(890f).Within(1e-3f), "10 + 4 x 25");
        }

        [Test]
        public void MultiplyMode_ScalesTheWholeValue_AfterTheSpentTerm()
        {
            _caster.SetBaseProperty(AbilityProperties.SpellPower, 50f);
            EffectNodeData node = new EffectNodeData
            {
                Amount = 10f,
                AmountScaleProperty = AbilityProperties.SpellPower,
                AmountScalePerPoint = 0.01f,
                AmountScaleMode = ScaleAmountMode.Multiply,
                AmountSpentResource = AbilityResourceIds.Mana,
                AmountSpentCoefficient = 1f
            };
            EffectContext context = AbilityTestSupport.Context(_system, _caster.Id, targets: _enemy.Id);
            context.AddSpent(AbilityResourceIds.Mana, 30f);

            Assert.That(LeveledValueResolver.ResolveAmount(node, context, _enemy.Id), Is.EqualTo(60f).Within(1e-3f),
                "(10 + 30) x (1 + 0.01 x 50)");

            node.AmountScaleMode = ScaleAmountMode.Add;
            Assert.That(LeveledValueResolver.ResolveAmount(node, context, _enemy.Id), Is.EqualTo(40.5f).Within(1e-3f),
                "10 + 30 + 0.01 x 50");
        }

        [Test]
        public void ResourceStats_ReadCurrentMaxAndMissing()
        {
            _enemy.Resources.SetCurrent(AbilityResourceIds.Health, 600f);
            EffectContext context = AbilityTestSupport.Context(_system, _caster.Id, targets: _enemy.Id);
            LeveledValue value = new LeveledValue
            {
                ScaleProperty = AbilityResourceIds.Health,
                ScalePerPoint = 0.1f,
                ScaleFrom = ScaleAmountSource.Target
            };

            value.ScaleStat = ScaleAmountStat.ResourceCurrent;
            Assert.That(LeveledValueResolver.Resolve(in value, context, _enemy.Id), Is.EqualTo(60f).Within(1e-3f));
            value.ScaleStat = ScaleAmountStat.ResourceMax;
            Assert.That(LeveledValueResolver.Resolve(in value, context, _enemy.Id), Is.EqualTo(100f).Within(1e-3f));
            value.ScaleStat = ScaleAmountStat.ResourceMissing;
            Assert.That(LeveledValueResolver.Resolve(in value, context, _enemy.Id), Is.EqualTo(40f).Within(1e-3f));
        }

        [Test]
        public void ContextSpent_IgnoresInvalidAmounts_AndNestedContextsShareIt()
        {
            EffectContext context = AbilityTestSupport.Context(_system, _caster.Id);
            context.AddSpent(AbilityResourceIds.Mana, 10f);
            context.AddSpent("MANA", 5f);
            context.AddSpent(AbilityResourceIds.Mana, -3f);
            context.AddSpent(AbilityResourceIds.Mana, float.NaN);
            context.AddSpent(string.Empty, 9f);

            Assert.That(context.SpentCount, Is.EqualTo(2));
            Assert.That(context.GetSpent(AbilityResourceIds.Mana), Is.EqualTo(15f), "ids are case-insensitive");

            EffectContext nested = context.CreateNested(_caster.Id, "child", new List<UnitId>());
            Assert.That(nested.GetSpent(AbilityResourceIds.Mana), Is.EqualTo(15f));
            nested.AddSpent("rage", 4f);
            Assert.That(nested.GetSpent("rage"), Is.EqualTo(4f));
            Assert.That(context.GetSpent("rage"), Is.EqualTo(0f), "a nested record never leaks into its parent");
        }
    }
}
