using System.Collections.Generic;
using Neo.Abilities;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     Contracts of the deterministic profile and the state hash: identical inputs give identical hashes tick by
    ///     tick, different seeds and states give different ones, presentation-side state is not hashed, values stay on
    ///     the Q8.8 grid and hashing allocates nothing once warm.
    /// </summary>
    public sealed class DeterministicProfileTests
    {
        [Test]
        public void SameInputs_ProduceTheSameHash_EveryTick()
        {
            Simulation a = new Simulation(seed: 7);
            Simulation b = new Simulation(seed: 7);

            for (int tick = 0; tick < 90; tick++)
            {
                a.Step(tick);
                b.Step(tick);
                Assert.That(AbilityStateHasher.Compute(a.System), Is.EqualTo(AbilityStateHasher.Compute(b.System)),
                    "tick " + tick);
            }

            Assert.That(a.Enemy.Health, Is.LessThan(1000f), "the scenario dealt damage");
        }

        [Test]
        public void DifferentSeeds_Diverge()
        {
            Simulation a = new Simulation(seed: 7);
            Simulation b = new Simulation(seed: 8);

            for (int tick = 0; tick < 90; tick++)
            {
                a.Step(tick);
                b.Step(tick);
            }

            Assert.That(AbilityStateHasher.Compute(a.System), Is.Not.EqualTo(AbilityStateHasher.Compute(b.System)));
        }

        [Test]
        public void Hash_FollowsSimulationState_NotSubscriptionsOrTrace()
        {
            Simulation sim = new Simulation(seed: 3);
            ulong start = AbilityStateHasher.Compute(sim.System);

            sim.System.Events.SubscribeAny(args => { });
            sim.System.Trace.Enabled = true;
            Assert.That(AbilityStateHasher.Compute(sim.System), Is.EqualTo(start));

            sim.System.ApplyModifier("poison", sim.Hero.Id, sim.Enemy.Id);
            ulong withModifier = AbilityStateHasher.Compute(sim.System);
            Assert.That(withModifier, Is.Not.EqualTo(start));

            DamageService.ApplyDamage(sim.System, UnitId.None, sim.Enemy.Id, 1f, AbilityDamageTypes.Pure);
            Assert.That(AbilityStateHasher.Compute(sim.System), Is.Not.EqualTo(withModifier));
        }

        [Test]
        public void Profile_UsesFixedTicksAndTheQ8Grid()
        {
            Simulation sim = new Simulation(seed: 5);

            Assert.That(DeterministicAbilityProfile.IsApplied(sim.System.Rules), Is.True);
            Assert.That(sim.System.Advance(0.1f), Is.EqualTo(3), "30 ticks per second");
            for (int tick = 0; tick < 60; tick++)
            {
                sim.Step(tick);
            }

            float raw = sim.Enemy.Health * 256f;
            Assert.That(raw, Is.EqualTo(Mathf.Round(raw)), "health stays on the 1/256 grid");
            Assert.That(sim.System.TickCount, Is.EqualTo(63));
        }

        [Test]
        public void ComputeStateHash_AllocatesNothingWhenWarm()
        {
            Simulation sim = new Simulation(seed: 11);
            for (int tick = 0; tick < 30; tick++)
            {
                sim.Step(tick);
            }

            AbilityStateHasher.Compute(sim.System);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            ulong hash = 0;
            for (int i = 0; i < 200; i++)
            {
                hash ^= AbilityStateHasher.Compute(sim.System);
            }

            long allocated = allocationCounter.End();

            Assert.That(allocated, Is.EqualTo(0L));
            Assert.That(hash, Is.Not.EqualTo(1UL));
        }

        /// <summary>A small fixed scenario: a hero with a chance-based strike, poison, a mark and a formation.</summary>
        private sealed class Simulation
        {
            private readonly uint _seed;

            public Simulation(uint seed)
            {
                _seed = seed;
                FakeWorldAdapter world = new FakeWorldAdapter();
                System = new AbilitySystem { World = world };
                DeterministicAbilityProfile.Apply(System.Rules, ticksPerSecond: 30);
                Hero = AbilityTestSupport.CreateUnit(System, team: 1, health: 1000f, name: "Hero");
                Enemy = AbilityTestSupport.CreateUnit(System, team: 2, health: 1000f, name: "Enemy");
                world.SetPosition(Hero.Id, Vector3.zero);
                world.SetPosition(Enemy.Id, new Vector3(2f, 0f, 0f));
                Enemy.SetBaseProperty(AbilityProperties.Armor, 3f);
                Hero.SetBaseProperty(AbilityProperties.CritChance, 0.3f);
                Hero.SetBaseProperty(AbilityProperties.CritMultiplier, 1.7f);

                AbilityBlueprint strike = new AbilityBlueprint { Id = "strike", Targeting = TargetingMode.Unit };
                EffectNodeData hit = AbilityTestSupport.DamageNode(7.3f, AbilityDamageTypes.Physical);
                hit.Chance = 0.6f;
                strike.ImpactEffects.Add(hit);
                System.RegisterAbility(strike);
                System.GrantAbility(Hero.Id, "strike");
                AbilityTemplates.StackingPoison("poison", tickDamage: 1.1f, maxStacks: 4).SetUp(System, Hero.Id);
                AbilityTemplates.Mark("mark", bonus: 2.5f, hits: 2).SetUp(System, Hero.Id);
                AbilityTemplates.FormationZone("trap", radius: 3f, duration: 1f, interval: 0.2f, damage: 0.9f)
                    .SetUp(System, Hero.Id);
            }

            public AbilitySystem System { get; }
            public AbilityUnit Hero { get; }
            public AbilityUnit Enemy { get; }

            public void Step(int tick)
            {
                if (tick % 5 == 0)
                {
                    System.Cast(CastRequest.AtUnit(Hero.Id, "strike", Enemy.Id, _seed * 1000u + (uint)tick));
                }

                if (tick == 10)
                {
                    System.Cast(CastRequest.AtUnit(Hero.Id, "mark", Enemy.Id, _seed));
                }

                if (tick == 20)
                {
                    System.Cast(CastRequest.AtPoint(Hero.Id, "trap", new Vector3(2f, 0f, 0f), _seed));
                }

                System.StepTicks(1);
            }
        }
    }
}
