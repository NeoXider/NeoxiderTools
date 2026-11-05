using System.Collections.Generic;
using Neo.Abilities;
using Neo.Tests.Performance;
using NUnit.Framework;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     Explicit per-tick cost benchmark of the domain at N units × M modifiers. Every unit carries one ticking
    ///     damage-over-time modifier (each tick runs the full damage pipeline and its receipts) and M - 1 passive
    ///     modifiers: half contribute properties read by the pipeline, half react to take_damage. The rows compare
    ///     library passes by case id; the workload only uses the public float API.
    /// </summary>
    [TestFixture]
    public sealed class AbilityTickCostBenchmarks
    {
        private const int Samples = 60;
        private const int TicksPerSample = 2;

        [OneTimeSetUp]
        public void EmitCalibration()
        {
            NeoPerf.EmitCalibration("abilities.tick_cost", 16);
        }

        [Category(NeoPerf.Category)]
        [Explicit(NeoPerf.ExplicitReason)]
        [TestCase(256, 1)]
        [TestCase(256, 4)]
        [TestCase(1024, 4)]
        [TestCase(1024, 8)]
        public void Benchmark_SystemTick_UnitsTimesModifiers(int unitCount, int modifiersPerUnit)
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, team: 1);
            ModifierBlueprint burn = AbilityTestSupport.Modifier("bench_nm_burn");
            burn.TickInterval = 0.1f;
            burn.TickEffects.Add(AbilityTestSupport.DamageNode(1f, AbilityDamageTypes.Physical));

            List<ModifierBlueprint> passives = new List<ModifierBlueprint>(modifiersPerUnit);
            for (int k = 1; k < modifiersPerUnit; k++)
            {
                ModifierBlueprint passive = AbilityTestSupport.Modifier("bench_nm_passive_" + k);
                if (k % 2 == 1)
                {
                    passive.WithProperty(AbilityProperties.Armor, PropertyOp.Add, 1f)
                        .WithProperty(AbilityProperties.IncomingDamageMul, PropertyOp.Mul, 1f);
                }
                else
                {
                    ModifierEventReaction reaction = new ModifierEventReaction { EventId = AbilityEvents.TakeDamage };
                    reaction.Effects.Add(new EffectNodeData
                    {
                        OpId = AbilityEffectOps.ResourceChange,
                        Target = EffectTargetSelector.Target,
                        ResourceId = AbilityResourceIds.Mana,
                        Amount = 1f
                    });
                    passive.EventReactions.Add(reaction);
                }

                passives.Add(passive);
            }

            List<AbilityUnit> victims = new List<AbilityUnit>(unitCount);
            for (int i = 0; i < unitCount; i++)
            {
                AbilityUnit victim = AbilityTestSupport.CreateUnit(system, team: 2, health: 1000000f, mana: 1000000f);
                system.Modifiers.Apply(burn, caster.Id, victim.Id);
                for (int k = 0; k < passives.Count; k++)
                {
                    system.Modifiers.Apply(passives[k], caster.Id, victim.Id);
                }

                victims.Add(victim);
            }

            NeoPerf.MeasureSynchronous("abilities.tick_cost.n" + unitCount + ".m" + modifiersPerUnit,
                    unitCount * modifiersPerUnit, TicksPerSample, NeoPerf.DefaultWarmupSamples, Samples,
                    () => system.Tick(0.1f))
                .Add("role", "tick")
                .Add("units", unitCount)
                .Add("modifiers_per_unit", modifiersPerUnit)
                .Emit();

            Assert.That(victims[0].Health, Is.LessThan(1000000f), "every tick damaged the first unit");
            Assert.That(victims[unitCount - 1].Health, Is.LessThan(1000000f), "every tick damaged the last unit");
        }
    }
}
