using System.Collections.Generic;
using Neo.Abilities;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     Contracts of the replaceable rules (<see cref="AbilityRules" />): every stack policy, a custom stacking rule,
    ///     Add / Mul / Max layering and the additive-percent aggregator, custom damage formula, mitigation, crit,
    ///     evasion, cooldown, cost, targeting and random rules, fixed-step time and the fixed-point numeric backend.
    /// </summary>
    public sealed class AbilityRulesTests
    {
        private AbilitySystem _system;
        private AbilityUnit _caster;
        private AbilityUnit _target;

        [SetUp]
        public void SetUp()
        {
            _system = new AbilitySystem();
            _caster = AbilityTestSupport.CreateUnit(_system, team: 1, health: 1000f, mana: 100f);
            _target = AbilityTestSupport.CreateUnit(_system, team: 2, health: 1000f);
        }

        [Test]
        public void StackPolicy_IndependentRefreshAndStack_KeepTheirClassicBehaviour()
        {
            ModifierBlueprint independent = AbilityTestSupport.Modifier("ind", 5f, ModifierStackPolicy.Independent);
            ModifierBlueprint refresh = AbilityTestSupport.Modifier("ref", 5f, ModifierStackPolicy.Refresh);
            ModifierBlueprint stack = AbilityTestSupport.Modifier("stk", 5f, ModifierStackPolicy.Stack);
            stack.MaxStacks = 2;

            for (int i = 0; i < 3; i++)
            {
                _system.Modifiers.Apply(independent, _caster.Id, _target.Id);
                _system.Modifiers.Apply(refresh, _caster.Id, _target.Id);
                _system.Modifiers.Apply(stack, _caster.Id, _target.Id);
                _system.Tick(1f);
            }

            Assert.That(_system.Modifiers.CountActive(_target.Id, "ind"), Is.EqualTo(3));
            Assert.That(_system.Modifiers.CountActive(_target.Id, "ref"), Is.EqualTo(1));
            Assert.That(_system.Modifiers.TryGetActive(_target.Id, "ref", out ModifierInstance refreshed), Is.True);
            Assert.That(refreshed.RemainingDuration, Is.EqualTo(4f).Within(0.001f));
            Assert.That(_system.Modifiers.TryGetActive(_target.Id, "stk", out ModifierInstance stacked), Is.True);
            Assert.That(stacked.Stacks, Is.EqualTo(2), "capped at MaxStacks");
        }

        [Test]
        public void StackPolicy_Unique_IgnoresReapplication_WithoutAReceipt()
        {
            ModifierBlueprint unique = AbilityTestSupport.Modifier("uniq", 5f, ModifierStackPolicy.Unique);
            ModifierApplyResult first = _system.Modifiers.Apply(unique, _caster.Id, _target.Id);
            _system.Tick(2f);
            int applied = 0;
            _system.Events.Subscribe(AbilityEvents.ModifierApplied, args => applied++);

            ModifierApplyResult second = _system.Modifiers.Apply(unique, _caster.Id, _target.Id);

            Assert.That(second.Instance, Is.SameAs(first.Instance));
            Assert.That(second.CreatedNew, Is.False);
            Assert.That(first.Instance.RemainingDuration, Is.EqualTo(3f).Within(0.001f), "not refreshed");
            Assert.That(applied, Is.Zero);
        }

        [Test]
        public void StackPolicy_Prolong_AddsTheFullDurationToTheRemainingTime()
        {
            ModifierBlueprint prolong = AbilityTestSupport.Modifier("long", 4f, ModifierStackPolicy.Prolong);
            ModifierInstance instance = _system.Modifiers.Apply(prolong, _caster.Id, _target.Id).Instance;
            _system.Tick(1f);

            _system.Modifiers.Apply(prolong, _caster.Id, _target.Id);

            Assert.That(instance.RemainingDuration, Is.EqualTo(7f).Within(0.001f));
            Assert.That(instance.NormalizedRemaining, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void StackPolicy_IndependentCapped_ReplacesTheOldestAtTheCap()
        {
            ModifierBlueprint capped = AbilityTestSupport.Modifier("cap", 10f, ModifierStackPolicy.IndependentCapped);
            capped.MaxStacks = 2;
            ModifierInstance oldest = _system.Modifiers.Apply(capped, _caster.Id, _target.Id).Instance;
            ModifierInstance middle = _system.Modifiers.Apply(capped, _caster.Id, _target.Id).Instance;

            ModifierApplyResult third = _system.Modifiers.Apply(capped, _caster.Id, _target.Id);

            Assert.That(third.CreatedNew, Is.True);
            Assert.That(oldest.IsActive, Is.False);
            Assert.That(middle.IsActive, Is.True);
            Assert.That(_system.Modifiers.CountActive(_target.Id, "cap"), Is.EqualTo(2));
        }

        [Test]
        public void CustomStackingRule_ReplacesTheDecision()
        {
            _system.Rules.Stacking = new AlwaysStackRule();
            ModifierBlueprint refresh = AbilityTestSupport.Modifier("custom", 5f, ModifierStackPolicy.Refresh);
            refresh.MaxStacks = 10;

            _system.Modifiers.Apply(refresh, _caster.Id, _target.Id);
            ModifierApplyResult again = _system.Modifiers.Apply(refresh, _caster.Id, _target.Id);

            Assert.That(again.Instance.Stacks, Is.EqualTo(2));
            _system.Rules.Stacking = null;
            Assert.That(_system.Rules.Stacking, Is.SameAs(StandardModifierStackingRule.Instance));
        }

        [TestCase(10f, 5f, 2f, 0f, 30f)]
        [TestCase(10f, 0f, 2f, 50f, 50f)]
        [TestCase(10f, -20f, 1f, 0f, -10f)]
        [TestCase(0f, 4f, 0.5f, 3f, 3f)]
        public void Layering_AddThenMulThenMax(float baseValue, float add, float mul, float floor, float expected)
        {
            ModifierBlueprint layers = AbilityTestSupport.Modifier("layers")
                .WithProperty("power", PropertyOp.Add, add)
                .WithProperty("power", PropertyOp.Mul, mul);
            if (floor != 0f)
            {
                layers.WithProperty("power", PropertyOp.Max, floor);
            }

            _target.SetBaseProperty("power", baseValue);
            _system.Modifiers.Apply(layers, _caster.Id, _target.Id);

            Assert.That(_target.GetProperty("power"), Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void Aggregator_AdditivePercent_SumsMultipliers_AndSwappingInvalidatesTheCache()
        {
            ModifierBlueprint a = AbilityTestSupport.Modifier("haste_a").WithProperty("speed", PropertyOp.Mul, 1.5f);
            ModifierBlueprint b = AbilityTestSupport.Modifier("haste_b").WithProperty("speed", PropertyOp.Mul, 1.5f);
            _target.SetBaseProperty("speed", 10f);
            _system.Modifiers.Apply(a, _caster.Id, _target.Id);
            _system.Modifiers.Apply(b, _caster.Id, _target.Id);
            Assert.That(_target.GetProperty("speed"), Is.EqualTo(22.5f).Within(0.001f), "default: multipliers compound");

            _system.Rules.Aggregator = AdditivePercentPropertyAggregator.Instance;

            Assert.That(_target.GetProperty("speed"), Is.EqualTo(20f).Within(0.001f), "additive: +50% +50% = x2");
        }

        [Test]
        public void CustomDamageFormula_MitigationCritAndEvasion_AreUsedByThePipeline()
        {
            _system.Rules.Mitigation = new HalfMitigation();
            DamageResult halved = DamageService.ApplyDamage(_system, _caster.Id, _target.Id, 40f,
                AbilityDamageTypes.Physical);
            Assert.That(halved.HealthDamage, Is.EqualTo(20f).Within(0.001f));

            _system.Rules.CriticalHit = new AlwaysCrit();
            DamageResult crit = DamageService.ApplyDamage(_system, _caster.Id, _target.Id, 40f,
                AbilityDamageTypes.Physical, random: new XorShiftRandom(1));
            Assert.That(crit.Crit, Is.True);
            Assert.That(crit.HealthDamage, Is.EqualTo(60f).Within(0.001f), "40 x3 crit, then halved");

            _system.Rules.Evasion = new AlwaysEvade();
            DamageResult evaded = DamageService.ApplyDamage(_system, _caster.Id, _target.Id, 40f,
                AbilityDamageTypes.Physical, random: new XorShiftRandom(1));
            Assert.That(evaded.Evaded, Is.True);

            _system.Rules.DamageFormula = new FlatFormula();
            DamageResult flat = DamageService.ApplyDamage(_system, _caster.Id, _target.Id, 999f,
                AbilityDamageTypes.Physical);
            Assert.That(flat.HealthDamage, Is.EqualTo(7f).Within(0.001f));
        }

        [Test]
        public void CooldownAndCostRules_ReplaceTheDefaults()
        {
            AbilityBlueprint spell = new AbilityBlueprint { Id = "spell", Targeting = TargetingMode.NoTarget, Cooldown = 10f };
            spell.Costs.Add(new AbilityCost(AbilityResourceIds.Mana, 50f));
            _system.RegisterAbility(spell);
            AbilitySlot slot = _system.GrantAbility(_caster.Id, "spell");
            _system.Rules.Cooldowns = new FixedCooldown();
            _system.Rules.Costs = new FreeCosts();

            Assert.That(_system.Cast(CastRequest.NoTarget(_caster.Id, "spell")).Success, Is.True);

            Assert.That(slot.CooldownRemaining, Is.EqualTo(2f).Within(0.001f));
            Assert.That(_caster.Resources.GetCurrent(AbilityResourceIds.Mana), Is.EqualTo(100f));
        }

        [Test]
        public void TargetingRule_ReplacesSelection()
        {
            _system.Rules.Targeting = new EveryoneResolver();
            AbilityBlueprint nova = new AbilityBlueprint { Id = "nova", Targeting = TargetingMode.NoTarget };
            nova.ImpactEffects.Add(AbilityTestSupport.DamageNode(5f));
            _system.RegisterAbility(nova);
            _system.GrantAbility(_caster.Id, "nova");

            _system.Cast(CastRequest.NoTarget(_caster.Id, "nova"));

            Assert.That(_caster.Health, Is.EqualTo(995f).Within(0.001f));
            Assert.That(_target.Health, Is.EqualTo(995f).Within(0.001f));
        }

        [Test]
        public void RandomFactory_SuppliesTheGeneratorsOfCasts()
        {
            CountingFactory factory = new CountingFactory();
            _system.Rules.Random = factory;
            AbilityBlueprint gamble = new AbilityBlueprint { Id = "gamble", Targeting = TargetingMode.Unit };
            EffectNodeData node = AbilityTestSupport.DamageNode(5f);
            node.Chance = 0.5f;
            gamble.ImpactEffects.Add(node);
            _system.RegisterAbility(gamble);
            _system.GrantAbility(_caster.Id, "gamble");

            _system.Cast(CastRequest.AtUnit(_caster.Id, "gamble", _target.Id));
            _system.Cast(CastRequest.AtUnit(_caster.Id, "gamble", _target.Id));

            Assert.That(factory.Created, Is.EqualTo(1), "one generator per pooled context, reseeded after");
            Assert.That(factory.Last.Rolls, Is.EqualTo(2));
            Assert.That(_target.Health, Is.EqualTo(1000f), "the custom source always rolls 0.99: the node never runs");
        }

        [Test]
        public void FixedStepTime_AdvancesInWholeTicks_AndCapsCatchUp()
        {
            FixedStepTimeSource time = new FixedStepTimeSource(10, maxStepsPerCall: 4);
            _system.Rules.Time = time;

            Assert.That(_system.Advance(0.25f), Is.EqualTo(2));
            Assert.That(_system.TickCount, Is.EqualTo(2));
            Assert.That(_system.Advance(0.05f), Is.EqualTo(1), "0.05 carried + 0.05 = one tick");
            Assert.That(_system.Advance(10f), Is.EqualTo(4), "a hitch runs at most four ticks");
            Assert.That(time.DiscardedSeconds, Is.GreaterThan(9f));
            Assert.That(_system.StepTicks(3), Is.True);
            Assert.That(_system.TickCount, Is.EqualTo(10));
            Assert.That(_system.ElapsedTime, Is.EqualTo(1f).Within(0.0001f));

            _system.Rules.Time = null;
            Assert.That(_system.StepTicks(1), Is.False, "the variable source has no fixed step");
            Assert.That(_system.Advance(0.3f), Is.EqualTo(1));
        }

        [Test]
        public void FixedPointNumeric_RoundsDamageAndPropertiesToTheQ8Grid()
        {
            _system.Rules.Numeric = FixedPointNumeric.Q8;
            _target.SetBaseProperty("ratio", 1f / 3f);

            DamageResult result = DamageService.ApplyDamage(_system, _caster.Id, _target.Id, 10f / 3f,
                AbilityDamageTypes.Pure);

            Assert.That(result.HealthDamage * 256f, Is.EqualTo(Mathf.Round(result.HealthDamage * 256f)));
            Assert.That(result.HealthDamage, Is.EqualTo(853f / 256f).Within(1e-6f));
            Assert.That(_target.GetProperty("ratio"), Is.EqualTo(85f / 256f).Within(1e-6f));
            Assert.That(FixedPointNumeric.Q8.ToRaw(1f), Is.EqualTo(256));
            Assert.That(FixedPointNumeric.Q8.FromRaw(128), Is.EqualTo(0.5f));
        }

        [Test]
        public void ResetToDefaults_RestoresEveryRule()
        {
            int version = _system.Rules.Version;
            _system.Rules.Mitigation = new HalfMitigation();
            _system.Rules.Numeric = FixedPointNumeric.Q8;

            _system.Rules.ResetToDefaults();

            Assert.That(_system.Rules.Mitigation, Is.SameAs(ArmorResistMitigationRule.Instance));
            Assert.That(_system.Rules.Numeric, Is.SameAs(FloatNumeric.Instance));
            Assert.That(_system.Rules.Version, Is.GreaterThan(version));
        }

        private sealed class AlwaysStackRule : IModifierStackingRule
        {
            public ModifierStackAction Decide(ModifierBlueprint blueprint, ModifierInstance existing, int activeInstances)
            {
                return ModifierStackAction.AddStack;
            }
        }

        private sealed class HalfMitigation : IMitigationRule
        {
            public float Mitigate(DamageContext damage, AbilityUnit target, float amount)
            {
                return amount * 0.5f;
            }
        }

        private sealed class AlwaysCrit : ICriticalHitRule
        {
            public bool TryRollCritical(DamageContext damage, AbilityUnit source, out float multiplier)
            {
                multiplier = 3f;
                return true;
            }
        }

        private sealed class AlwaysEvade : IEvasionRule
        {
            public bool RollEvasion(DamageContext damage, AbilityUnit target)
            {
                return true;
            }
        }

        private sealed class FlatFormula : IDamageFormula
        {
            public float Compute(DamageContext damage, AbilityUnit source, AbilityUnit target, AbilityRules rules)
            {
                return 7f;
            }
        }

        private sealed class FixedCooldown : ICooldownRule
        {
            public float ResolveCooldown(AbilityUnit caster, AbilityBlueprint ability, float baseSeconds)
            {
                return 2f;
            }
        }

        private sealed class FreeCosts : ICastCostRule
        {
            public float ResolveCost(AbilityUnit caster, AbilityBlueprint ability, AbilityCost cost)
            {
                return 0f;
            }
        }

        private sealed class EveryoneResolver : IEffectTargetResolver
        {
            private readonly List<AbilityUnit> _units = new List<AbilityUnit>();

            public void Resolve(EffectNodeData node, EffectContext context, List<UnitId> results)
            {
                context.System.GetUnits(_units);
                for (int i = 0; i < _units.Count; i++)
                {
                    results.Add(_units[i].Id);
                }
            }
        }

        private sealed class CountingFactory : IRandomSourceFactory
        {
            public int Created;
            public HighRandom Last;

            public IReseedableRandomSource Create(uint seed)
            {
                Created++;
                Last = new HighRandom();
                return Last;
            }
        }

        private sealed class HighRandom : IReseedableRandomSource
        {
            public int Rolls;

            public float NextFloat()
            {
                Rolls++;
                return 0.99f;
            }

            public int NextInt(int maxExclusive)
            {
                return 0;
            }

            public void Reseed(uint seed)
            {
            }
        }
    }
}
