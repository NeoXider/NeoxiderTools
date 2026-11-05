using Neo.Abilities;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     Zero-GC guards for the new hot paths: hooked damage (modify_damage, reflect, HitsOnly charges), EveryNth and
    ///     HealthBelow triggers, zone pulses, fixed-step Advance, the fixed-point numeric backend and an enabled trace.
    ///     Each test warms its buffers once, then counts native GC.Alloc calls of the repeated calls on this thread.
    /// </summary>
    public sealed class AbilityTriggerAllocationTests
    {
        private const int Iterations = 200;

        [Test]
        public void HookedHits_WithMultiplierReflectAndTriggers_AllocateNothingWhenWarm()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit hero = AbilityTestSupport.CreateUnit(system, team: 1, health: 1e6f, mana: 1e6f);
            AbilityUnit enemy = AbilityTestSupport.CreateUnit(system, team: 2, health: 1e6f, mana: 1e6f);
            AbilityBlueprint strike = new AbilityBlueprint { Id = "gc_hook_strike", Targeting = TargetingMode.Unit };
            strike.ImpactEffects.Add(AbilityTestSupport.DamageNode(10f));
            system.RegisterAbility(strike);
            system.GrantAbility(hero.Id, strike.Id);

            AbilityTemplateBundle enhancer = AbilityTemplates.AttackEnhancer("gc_power", 1.5f, hits: 0, duration: 0f);
            enhancer.Modifiers[0].EventReactions[0].RemoveWhenSpent = false;
            enhancer.SetUp(system, hero.Id);
            system.ApplyModifier("gc_power", hero.Id, hero.Id);
            AbilityTemplates.Thorns("gc_thorns", 0.1f).SetUp(system, enemy.Id);
            ModifierBlueprint counter = AbilityTestSupport.Modifier("gc_counter");
            ModifierEventReaction everyThird = new ModifierEventReaction
            {
                EventId = AbilityEvents.AttackLanded,
                Trigger = ModifierTrigger.EveryNth,
                EveryN = 3,
                Cooldown = 0.001f
            };
            everyThird.Effects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.ResourceChange,
                Target = EffectTargetSelector.Caster,
                ResourceId = AbilityResourceIds.Mana,
                Amount = 1f
            });
            counter.EventReactions.Add(everyThird);
            system.Modifiers.Apply(counter, hero.Id, hero.Id);
            CastRequest request = CastRequest.AtUnit(hero.Id, strike.Id, enemy.Id);
            for (int i = 0; i < 4; i++)
            {
                system.Cast(request);
                system.Tick(0.01f);
            }

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                system.Cast(request);
                system.Tick(0.01f);
            }

            long allocated = allocationCounter.End();

            Assert.That(allocated, Is.EqualTo(0L));
            Assert.That(1e6f - enemy.Health, Is.GreaterThan(Iterations * 14f), "every hit was empowered");
            Assert.That(hero.Health, Is.LessThan(1e6f), "thorns reflected");
        }

        [Test]
        public void ZonePulses_FixedStepAdvance_Q8AndTrace_AllocateNothingWhenWarm()
        {
            FakeWorldAdapter world = new FakeWorldAdapter();
            AbilitySystem system = new AbilitySystem { World = world };
            system.Rules.Time = new FixedStepTimeSource(30);
            system.Rules.Numeric = FixedPointNumeric.Q8;
            system.Trace.Enabled = true;
            AbilityUnit hero = AbilityTestSupport.CreateUnit(system, team: 1, health: 1e6f);
            world.SetPosition(hero.Id, Vector3.zero);
            for (int i = 0; i < 8; i++)
            {
                AbilityUnit enemy = AbilityTestSupport.CreateUnit(system, team: 2, health: 1e6f);
                world.SetPosition(enemy.Id, new Vector3(i * 0.5f, 0f, 0f));
            }

            AbilityTemplateBundle formation = AbilityTemplates.FormationZone("gc_formation", radius: 6f,
                duration: 0f, interval: 1f / 30f, damage: 1.37f);
            formation.SetUp(system, hero.Id);
            system.SpawnZone(new AbilityZoneSpec
            {
                Caster = hero.Id,
                Center = Vector3.zero,
                Radius = 6f,
                Interval = 1f / 30f,
                PayloadAbilityId = "gc_formation_pulse",
                TeamFilter = AbilityTeamFilter.Enemies
            });
            for (int i = 0; i < 4; i++)
            {
                system.Advance(1f / 30f);
            }

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                system.Advance(1f / 30f);
            }

            long allocated = allocationCounter.End();

            Assert.That(allocated, Is.EqualTo(0L));
            Assert.That(system.TickCount, Is.GreaterThanOrEqualTo(Iterations));
            Assert.That(system.Trace.Count, Is.EqualTo(system.Trace.Capacity), "the trace kept recording");
        }
    }
}
