using Neo.Abilities.Fixed;
using Neo.Tests.Performance;
using NUnit.Framework;
using static Neo.Editor.Tests.Abilities.Fixed.FixedTestSupport;

namespace Neo.Editor.Tests.Abilities.Fixed
{
    /// <summary>
    ///     Zero-GC guards of the fixed core: ticks with damage over time and reactions, hooked casts (multiply, mark,
    ///     reflect, every Nth with a cooldown), zone pulses, receipts with handlers and the state hash. Each test warms
    ///     its buffers, then counts the GC allocations of the repeated calls on this thread.
    /// </summary>
    public sealed class FixedAllocationTests
    {
        private const int Iterations = 200;

        [Test]
        public void TicksWithDotsAndReactions_AllocateNothingWhenWarm()
        {
            FixedAbilitySystem system = NewSystem();
            FixedUnit hero = Unit(system, 1);
            FixedModifierBlueprint burn = Modifier("burn", 0, FixedStackPolicy.Stack);
            burn.MaxStacks = 3;
            burn.TickIntervalTicks = 1;
            burn.TickEffects.Add(Node(FixedOps.Damage, 1d, damageType: FixedDamageTypes.Physical));
            FixedModifierBlueprint passive = Modifier("passive");
            FixedModifierReaction onHit = new FixedModifierReaction { EventId = FixedEvents.TakeDamage };
            FixedEffectNode mana = Node(FixedOps.ResourceChange, 1d);
            mana.ResourceId = FixedResources.Mana;
            onHit.Effects.Add(mana);
            passive.EventReactions.Add(onHit);
            passive.WithProperty(FixedProperties.Armor, FixedPropertyOp.Add, Q(1d));
            for (int i = 0; i < 32; i++)
            {
                FixedUnit victim = Unit(system, 2, 1000000d, 1000000d);
                system.Modifiers.Apply(burn, hero.Id, victim.Id);
                system.Modifiers.Apply(burn, hero.Id, victim.Id);
                system.Modifiers.Apply(passive, hero.Id, victim.Id);
            }

            system.Step(4);
            using GcAllocCounter counter = GcAllocCounter.CreateOrIgnore();
            counter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                system.Step();
            }

            Assert.That(counter.End(), Is.EqualTo(0L));
        }

        [Test]
        public void HookedCasts_AllocateNothingWhenWarm()
        {
            FixedAbilitySystem system = NewSystem();
            FixedUnit hero = Unit(system, 1, 1000000d, 1000000d);
            FixedUnit golem = Unit(system, 2, 1000000d, x: 1d);
            FixedAbilityBlueprint strike = Ability("strike", FixedTargeting.Unit, Node(FixedOps.Damage, 10d));
            system.RegisterAbility(strike);
            system.GrantAbility(hero.Id, "strike");

            FixedModifierBlueprint power = Modifier("power");
            FixedModifierReaction multiply = new FixedModifierReaction { EventId = FixedEvents.BeforeDealDamage, HitsOnly = true };
            multiply.Effects.Add(Node(FixedOps.ModifyDamage, 1.5d));
            power.EventReactions.Add(multiply);
            FixedModifierBlueprint counter = Modifier("counter");
            FixedModifierReaction every = new FixedModifierReaction
                { EventId = FixedEvents.AttackLanded, Trigger = FixedModifierTrigger.EveryNth, EveryN = 3, CooldownTicks = 1 };
            FixedEffectNode mana = Node(FixedOps.ResourceChange, -1d);
            mana.ResourceId = FixedResources.Mana;
            mana.Target = FixedTargetSelector.Caster;
            every.Effects.Add(mana);
            counter.EventReactions.Add(every);
            FixedModifierBlueprint thorns = Modifier("thorns");
            FixedModifierReaction reflect = new FixedModifierReaction { EventId = FixedEvents.BeforeTakeDamage };
            reflect.Effects.Add(Node(FixedOps.Reflect, 0.25d));
            FixedModifierReaction mark = new FixedModifierReaction { EventId = FixedEvents.BeforeTakeDamage, HitsOnly = true };
            FixedEffectNode bonus = Node(FixedOps.ModifyDamage, 2d);
            bonus.CustomParam = FixedOps.ModeAdd;
            mark.Effects.Add(bonus);
            thorns.EventReactions.Add(reflect);
            thorns.EventReactions.Add(mark);
            system.Modifiers.Apply(power, hero.Id, hero.Id);
            system.Modifiers.Apply(counter, hero.Id, hero.Id);
            system.Modifiers.Apply(thorns, golem.Id, golem.Id);
            system.Events.Subscribe(FixedEvents.TakeDamage, (in FixedEventArgs args) => { });
            FixedCastRequest request = FixedCastRequest.AtUnit(hero.Id, "strike", golem.Id);
            for (int i = 0; i < 4; i++)
            {
                system.Cast(in request);
                system.Step();
            }

            int before = golem.HealthRaw;
            using GcAllocCounter allocations = GcAllocCounter.CreateOrIgnore();
            allocations.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                system.Cast(in request);
                system.Step();
            }

            Assert.That(allocations.End(), Is.EqualTo(0L));
            Assert.That(before - golem.HealthRaw, Is.EqualTo(Iterations * Q(18d)), "every hit: (10 + 2) x 1.5");
            Assert.That(hero.HealthRaw, Is.LessThan(Q(1000000d)), "thorns reflected");
        }

        [Test]
        public void ZonePulsesAndTheStateHash_AllocateNothingWhenWarm()
        {
            FixedAbilitySystem system = NewSystem();
            FixedUnit hero = Unit(system, 1);
            for (int i = 0; i < 16; i++)
            {
                Unit(system, 2, 1000000d, x: i % 4, z: i / 4);
            }

            FixedModifierBlueprint slow = Modifier("slow", 5).WithProperty(FixedProperties.MoveSpeed, FixedPropertyOp.Mul, Q(0.5d));
            system.RegisterModifier(slow);
            system.RegisterAbility(Ability("pulse", FixedTargeting.NoTarget, Node(FixedOps.Damage, 1d)));
            system.SpawnZone(new FixedZoneSpec
            {
                Caster = hero.Id, Center = FixedVec3.Zero, RadiusRaw = Q(10d), IntervalTicks = 1, PayloadAbilityId = "pulse",
                ModifierId = "slow", TeamFilter = FixedTeamFilter.Enemies
            });
            system.Step(4);
            system.ComputeStateHash();
            using GcAllocCounter counter = GcAllocCounter.CreateOrIgnore();
            counter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                system.Step();
                system.ComputeStateHash();
            }

            Assert.That(counter.End(), Is.EqualTo(0L));
        }
    }
}
