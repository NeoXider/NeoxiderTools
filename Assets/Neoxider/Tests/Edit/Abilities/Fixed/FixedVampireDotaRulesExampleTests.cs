using System.Collections.Generic;
using Neo.Abilities.Fixed;
using NUnit.Framework;
using static Neo.Editor.Tests.Abilities.Fixed.FixedTestSupport;

namespace Neo.Editor.Tests.Abilities.Fixed
{
    /// <summary>
    ///     How a game with its own combat rules (VampireDota's design) plugs them into the fixed core through the
    ///     extension points, without copying game code: armor as amount × 100 / (100 + armor), StatBlock-style rational
    ///     multipliers as an aggregator, and DenyNew / RefreshDuration / StackUpTo3 stacking as a stacking rule.
    /// </summary>
    public sealed class FixedVampireDotaRulesExampleTests
    {
        private const int DenyNew = 0;
        private const int RefreshDuration = 1;
        private const int StackUpTo3 = 2;

        [Test]
        public void ArmorRule_IsAmountTimesHundredOverHundredPlusArmor()
        {
            FixedAbilitySystem system = CreateSystem();
            FixedUnit attacker = Unit(system, 1);
            FixedUnit defender = Unit(system, 2, 1000d);
            defender.SetBaseProperty(FixedProperties.Armor, Q(25d));
            int damage = system.Damage.Apply(attacker.Id, defender.Id, Q(50d), FixedDamageTypes.Physical).HealthDamageRaw;
            Assert.That(damage, Is.EqualTo(Q(40d)), "50 x 100 / 125");

            defender.SetBaseProperty(FixedProperties.Armor, 7 * One / 2);
            int raw = FixedMath.MulDivToInt(Q(50d), 100 * One, 100 * One + 7 * One / 2);
            Assert.That(system.Damage.Apply(attacker.Id, defender.Id, Q(50d), FixedDamageTypes.Physical).HealthDamageRaw,
                Is.EqualTo(raw), "fractional armor stays in Q8.8 through the division");

            defender.SetBaseProperty(FixedProperties.MagicResistPercent, One / 4);
            Assert.That(system.Damage.Apply(attacker.Id, defender.Id, Q(40d), FixedDamageTypes.Magical).HealthDamageRaw,
                Is.EqualTo(Q(30d)), "resistance is a raw fraction (0.25 = 64)");
        }

        [Test]
        public void RationalAggregator_ResolvesStatBlockMultipliersOnce()
        {
            FixedAbilitySystem system = CreateSystem();
            FixedUnit hero = Unit(system, 1);
            hero.SetBaseProperty(FixedProperties.MoveSpeed, Q(6d));
            FixedModifierBlueprint boots = Modifier("boots").WithProperty(FixedProperties.MoveSpeed, FixedPropertyOp.Add, Q(1d));
            FixedModifierBlueprint haste = Modifier("haste")
                .WithProperty(FixedProperties.MoveSpeed, FixedPropertyOp.Mul, 110, denominator: 100);
            FixedModifierBlueprint slow = Modifier("slow")
                .WithProperty(FixedProperties.MoveSpeed, FixedPropertyOp.Mul, 2, denominator: 3);
            system.Modifiers.Apply(boots, hero.Id, hero.Id);
            system.Modifiers.Apply(haste, hero.Id, hero.Id);
            system.Modifiers.Apply(slow, hero.Id, hero.Id);

            int expected = FixedMath.MulDivToInt(Q(7d), 110 * 2, 100 * 3);
            Assert.That(hero.GetProperty(FixedProperties.MoveSpeed), Is.EqualTo(expected),
                "(6 + 1) x 110/100 x 2/3, rounded once");
        }

        [Test]
        public void StackingRule_MapsDenyNewRefreshDurationAndStackUpTo3()
        {
            FixedAbilitySystem system = CreateSystem();
            FixedUnit hero = Unit(system, 1);
            FixedModifierBlueprint deny = Modifier("deny", 60);
            deny.StackingKind = DenyNew;
            FixedModifierBlueprint refresh = Modifier("refresh", 60);
            refresh.StackingKind = RefreshDuration;
            FixedModifierBlueprint stack = Modifier("stack", 60);
            stack.StackingKind = StackUpTo3;
            stack.MaxStacks = 3;
            for (int i = 0; i < 5; i++)
            {
                system.Modifiers.Apply(deny, hero.Id, hero.Id);
                system.Modifiers.Apply(refresh, hero.Id, hero.Id);
                system.Modifiers.Apply(stack, hero.Id, hero.Id);
                system.Step(10);
            }

            Assert.That(Instance(system, hero, "deny").RemainingTicks, Is.EqualTo(10), "never refreshed");
            Assert.That(Instance(system, hero, "refresh").RemainingTicks, Is.EqualTo(50));
            Assert.That(Instance(system, hero, "refresh").Stacks, Is.EqualTo(1));
            Assert.That(Instance(system, hero, "stack").Stacks, Is.EqualTo(3), "capped at three");
            Assert.That(Instance(system, hero, "stack").RemainingTicks, Is.EqualTo(50), "every application refreshes");
        }

        private static FixedAbilitySystem CreateSystem()
        {
            FixedRules rules = new FixedRules
            {
                TicksPerSecond = 30,
                Mitigation = new ArmorRatioMitigation(),
                Aggregator = new RationalStatAggregator(),
                Stacking = new VampireDotaStackingRule()
            };
            return new FixedAbilitySystem(rules);
        }

        private static FixedModifierInstance Instance(FixedAbilitySystem system, FixedUnit unit, string id)
        {
            Assert.That(system.Modifiers.TryGetActive(unit.Id, id, out FixedModifierInstance instance), Is.True, id);
            return instance;
        }

        /// <summary>Physical: amount × 100 / (100 + armor) with Q8.8 armor; magical: amount × (1 − resistance).</summary>
        private sealed class ArmorRatioMitigation : IFixedMitigationRule
        {
            public int Mitigate(FixedDamageContext damage, FixedUnit target, int amountRaw)
            {
                int one = damage.System.Rules.Format.One;
                if (damage.IsPhysical)
                {
                    int armor = FixedMath.Clamp(target.GetProperty(FixedProperties.Armor), 0, 1000 * one);
                    return FixedMath.MulDivToInt(amountRaw, 100L * one, 100L * one + armor);
                }

                if (damage.IsMagical)
                {
                    int resistance = FixedMath.Clamp(target.GetProperty(FixedProperties.MagicResistPercent), 0, one);
                    return FixedMath.MulDivToInt(amountRaw, one - resistance, one);
                }

                return amountRaw;
            }
        }

        /// <summary>(base + ΣAdd) × ΠN / ΠD as an exact ratio, rounded half away from zero once.</summary>
        private sealed class RationalStatAggregator : IFixedPropertyAggregator
        {
            public int Compute(string propertyId, int baseRaw, List<FixedResolvedContribution> contributions,
                FixedFormat format)
            {
                long sum = baseRaw;
                long numerator = 1L;
                long denominator = 1L;
                for (int i = 0; i < contributions.Count; i++)
                {
                    FixedResolvedContribution c = contributions[i];
                    if (c.Op == FixedPropertyOp.Add)
                    {
                        sum += c.ValueRaw;
                    }
                    else if (c.Op == FixedPropertyOp.Mul)
                    {
                        numerator *= c.ValueRaw;
                        denominator *= c.Denominator > 0 ? c.Denominator : format.One;
                        long gcd = FixedMath.Gcd(numerator, denominator);
                        if (gcd > 1L)
                        {
                            numerator /= gcd;
                            denominator /= gcd;
                        }
                    }
                }

                return FixedMath.ClampToInt(FixedMath.MulDiv(sum, numerator, denominator));
            }
        }

        /// <summary>Reads the game's stacking kind from <see cref="FixedModifierBlueprint.StackingKind" />.</summary>
        private sealed class VampireDotaStackingRule : IFixedStackingRule
        {
            public FixedStackAction Decide(FixedModifierBlueprint blueprint, FixedModifierInstance existing,
                int activeInstances)
            {
                if (existing == null)
                {
                    return FixedStackAction.CreateNew;
                }

                switch (blueprint.StackingKind)
                {
                    case RefreshDuration:
                        return FixedStackAction.Refresh;
                    case StackUpTo3:
                        return FixedStackAction.AddStack;
                    default:
                        return FixedStackAction.Ignore;
                }
            }
        }
    }
}
