using System;
using System.Collections.Generic;
using Neo.Abilities;
using Neo.Tests.Performance;
using NUnit.Framework;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     Explicit benchmarks for the domain hot paths at survivor scale, without scene objects: a system tick with one
    ///     ticking damage-over-time modifier per unit, and event publication to one matching Target observer while 1,
    ///     100 or 1000 other units hold Target subscriptions (the publish cost must not follow the unrelated count).
    /// </summary>
    [TestFixture]
    public sealed class AbilityDomainBenchmarks
    {
        private const int Samples = 60;
        private const int TicksPerSample = 4;
        private const int PublishesPerSample = 256;

        [OneTimeSetUp]
        public void EmitCalibration()
        {
            NeoPerf.EmitCalibration("abilities.domain", 16);
        }

        [Category(NeoPerf.Category)]
        [Explicit(NeoPerf.ExplicitReason)]
        [TestCase(64)]
        [TestCase(256)]
        [TestCase(1024)]
        public void Benchmark_SystemTick_DamageOverTimeOnEveryUnit(int unitCount)
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, team: 1);
            ModifierBlueprint burn = AbilityTestSupport.Modifier("bench_burn");
            burn.TickInterval = 0.1f;
            burn.TickEffects.Add(AbilityTestSupport.DamageNode(1f));
            List<AbilityUnit> victims = new List<AbilityUnit>(unitCount);
            for (int i = 0; i < unitCount; i++)
            {
                AbilityUnit victim = AbilityTestSupport.CreateUnit(system, team: 2, health: 1000000f);
                system.Modifiers.Apply(burn, caster.Id, victim.Id);
                victims.Add(victim);
            }

            NeoPerf.MeasureSynchronous("abilities.domain.tick_dot.n" + unitCount, unitCount, TicksPerSample,
                    NeoPerf.DefaultWarmupSamples, Samples, () => system.Tick(0.1f))
                .Add("role", "tick")
                .Add("modifiers_per_unit", 1L)
                .Emit();

            int ticks = NeoPerf.CallsPerMeasure(NeoPerf.DefaultWarmupSamples, Samples) * TicksPerSample;
            Assert.That(victims[0].Health, Is.EqualTo(1000000f - ticks).Within(0.5f), "every tick damaged once");
            Assert.That(victims[unitCount - 1].Health, Is.EqualTo(1000000f - ticks).Within(0.5f));
        }

        [Category(NeoPerf.Category)]
        [Explicit(NeoPerf.ExplicitReason)]
        [TestCase(1)]
        [TestCase(100)]
        [TestCase(1000)]
        public void Benchmark_EventPublish_OneMatchingTargetAmongUnrelatedObservers(int unrelatedObservers)
        {
            AbilityEventBus bus = new AbilityEventBus();
            UnitId target = new UnitId(1u);
            int matching = 0;
            int unrelated = 0;
            Action<AbilityEventArgs> onTarget = args => matching++;
            Action<AbilityEventArgs> onOther = args => unrelated++;
            bus.SubscribeTarget(target, onTarget);
            for (int i = 0; i < unrelatedObservers; i++)
            {
                bus.SubscribeTarget(new UnitId((uint)i + 2u), onOther);
            }

            AbilityEventArgs hit = new AbilityEventArgs(AbilityEvents.TakeDamage, target, UnitId.None, 1f);
            NeoPerf.MeasureSynchronous("abilities.domain.publish_target.unrelated" + unrelatedObservers,
                    unrelatedObservers, PublishesPerSample, NeoPerf.DefaultWarmupSamples, Samples, () => bus.Publish(hit))
                .Add("role", "publish")
                .Add("matching_observers", 1L)
                .Emit();

            int publishes = NeoPerf.CallsPerMeasure(NeoPerf.DefaultWarmupSamples, Samples) * PublishesPerSample;
            Assert.That(matching, Is.EqualTo(publishes), "the matching Target observer sees every publication");
            Assert.That(unrelated, Is.Zero, "observers of other units are never invoked");
        }
    }
}
