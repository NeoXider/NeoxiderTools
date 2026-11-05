using System.Collections.Generic;
using Neo.Abilities.Fixed;
using Neo.Tests.Performance;
using NUnit.Framework;
using static Neo.Editor.Tests.Abilities.Fixed.FixedTestSupport;

namespace Neo.Editor.Tests.Abilities.Fixed
{
    /// <summary>
    ///     Per-tick cost of the fixed core, measured exactly like the float benchmarks (AbilityDomainBenchmarks
    ///     tick_dot, AbilityTickCostBenchmarks tick_cost): the same unit counts, the same modifiers, one damage-over-time
    ///     tick per unit and step, passives that add properties or react to take_damage. Case ids carry ".fixed".
    /// </summary>
    [TestFixture]
    public sealed class FixedTickCostBenchmarks
    {
        private const int Samples = 60;

        [OneTimeSetUp]
        public void EmitCalibration()
        {
            NeoPerf.EmitCalibration("abilities.fixed", 16);
        }

        [Category(NeoPerf.Category)]
        [Explicit(NeoPerf.ExplicitReason)]
        [TestCase(64)]
        [TestCase(256)]
        [TestCase(1024)]
        public void Benchmark_FixedStep_DamageOverTimeOnEveryUnit(int unitCount)
        {
            FixedAbilitySystem system = NewSystem();
            FixedUnit caster = Unit(system, 1);
            FixedModifierBlueprint burn = Modifier("bench_burn");
            burn.TickIntervalTicks = 1;
            burn.TickEffects.Add(Node(FixedOps.Damage, 1d));
            List<FixedUnit> victims = new List<FixedUnit>(unitCount);
            for (int i = 0; i < unitCount; i++)
            {
                FixedUnit victim = Unit(system, 2, 1000000d);
                system.Modifiers.Apply(burn, caster.Id, victim.Id);
                victims.Add(victim);
            }

            NeoPerf.MeasureSynchronous("abilities.fixed.tick_dot.n" + unitCount, unitCount, 4, NeoPerf.DefaultWarmupSamples,
                    Samples, () => system.Step())
                .Add("role", "tick")
                .Add("modifiers_per_unit", 1L)
                .Emit();

            int ticks = NeoPerf.CallsPerMeasure(NeoPerf.DefaultWarmupSamples, Samples) * 4;
            Assert.That(victims[0].HealthRaw, Is.EqualTo(Q(1000000d) - ticks * One), "every tick damaged once");
            Assert.That(victims[unitCount - 1].HealthRaw, Is.EqualTo(Q(1000000d) - ticks * One));
        }

        [Category(NeoPerf.Category)]
        [Explicit(NeoPerf.ExplicitReason)]
        [TestCase(256, 1)]
        [TestCase(256, 4)]
        [TestCase(1024, 4)]
        [TestCase(1024, 8)]
        public void Benchmark_FixedStep_UnitsTimesModifiers(int unitCount, int modifiersPerUnit)
        {
            FixedAbilitySystem system = NewSystem();
            FixedUnit caster = Unit(system, 1);
            FixedModifierBlueprint burn = Modifier("bench_nm_burn");
            burn.TickIntervalTicks = 1;
            burn.TickEffects.Add(Node(FixedOps.Damage, 1d, damageType: FixedDamageTypes.Physical));
            List<FixedModifierBlueprint> passives = new List<FixedModifierBlueprint>(modifiersPerUnit);
            for (int k = 1; k < modifiersPerUnit; k++)
            {
                FixedModifierBlueprint passive = Modifier("bench_nm_passive_" + k);
                if (k % 2 == 1)
                {
                    passive.WithProperty(FixedProperties.Armor, FixedPropertyOp.Add, One)
                        .WithProperty(FixedProperties.IncomingDamageMul, FixedPropertyOp.Mul, One);
                }
                else
                {
                    FixedModifierReaction reaction = new FixedModifierReaction { EventId = FixedEvents.TakeDamage };
                    FixedEffectNode mana = Node(FixedOps.ResourceChange, 1d);
                    mana.ResourceId = FixedResources.Mana;
                    reaction.Effects.Add(mana);
                    passive.EventReactions.Add(reaction);
                }

                passives.Add(passive);
            }

            List<FixedUnit> victims = new List<FixedUnit>(unitCount);
            for (int i = 0; i < unitCount; i++)
            {
                FixedUnit victim = Unit(system, 2, 1000000d, 1000000d);
                system.Modifiers.Apply(burn, caster.Id, victim.Id);
                for (int k = 0; k < passives.Count; k++)
                {
                    system.Modifiers.Apply(passives[k], caster.Id, victim.Id);
                }

                victims.Add(victim);
            }

            NeoPerf.MeasureSynchronous("abilities.fixed.tick_cost.n" + unitCount + ".m" + modifiersPerUnit,
                    unitCount * modifiersPerUnit, 2, NeoPerf.DefaultWarmupSamples, Samples, () => system.Step())
                .Add("role", "tick")
                .Add("units", unitCount)
                .Add("modifiers_per_unit", modifiersPerUnit)
                .Emit();

            Assert.That(victims[0].HealthRaw, Is.LessThan(Q(1000000d)), "every tick damaged the first unit");
            Assert.That(victims[unitCount - 1].HealthRaw, Is.LessThan(Q(1000000d)), "every tick damaged the last unit");
        }
    }
}
