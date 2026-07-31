using System.Collections.Generic;
using Neo.Abilities;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     Interval ticking (DoT), immediate first tick, bounded catch-up, and declarative event
    ///     reactions (heal-on-hit, thorns routed to the attacker, and depth-bounded reaction loops).
    /// </summary>
    public sealed class ModifierTickAndReactionTests
    {
        private AbilitySystem _system;
        private AbilityUnit _attacker;
        private AbilityUnit _victim;

        [SetUp]
        public void SetUp()
        {
            _system = new AbilitySystem();
            _attacker = AbilityTestSupport.CreateUnit(_system, team: 1, health: 1000f);
            _victim = AbilityTestSupport.CreateUnit(_system, team: 2, health: 1000f);
        }

        private static ModifierBlueprint Dot(string id, float duration, float interval, float damage,
            bool tickOnApply = false)
        {
            var bp = new ModifierBlueprint
            {
                Id = id,
                Duration = duration,
                TickInterval = interval,
                TickOnApply = tickOnApply,
                StackPolicy = ModifierStackPolicy.Refresh
            };
            bp.TickEffects.Add(AbilityTestSupport.DamageNode(damage));
            return bp;
        }

        [Test]
        public void Dot_DealsPeriodicDamage_ThenExpires()
        {
            var log = new EventLog(_system);
            _system.Modifiers.Apply(Dot("burn", duration: 3f, interval: 1f, damage: 10f), _attacker.Id, _victim.Id);

            _system.Tick(1f);
            Assert.That(_victim.Health, Is.EqualTo(990f).Within(0.001f), "one tick");

            _system.Tick(1f);
            _system.Tick(1f);

            Assert.That(_victim.Health, Is.EqualTo(970f).Within(0.001f), "three ticks total");
            Assert.That(AbilityTestSupport.ActiveModifiers(_system, _victim.Id).Count, Is.EqualTo(0),
                "DoT expires after its duration");

            Assert.That(log.TryGetLast(AbilityEvents.ModifierRemoved, out AbilityEventArgs removed), Is.True);
            Assert.That(removed.Amount, Is.EqualTo(1f), "expiry reports expired=true (amount 1)");
        }

        [Test]
        public void TickOnApply_FiresImmediatelyOnFirstTick()
        {
            AbilityUnit withImmediate = _victim;
            AbilityUnit withoutImmediate = AbilityTestSupport.CreateUnit(_system, team: 2, health: 1000f);

            _system.Modifiers.Apply(Dot("burnA", duration: 0f, interval: 1f, damage: 5f, tickOnApply: true),
                _attacker.Id, withImmediate.Id);
            _system.Modifiers.Apply(Dot("burnB", duration: 0f, interval: 1f, damage: 5f, tickOnApply: false),
                _attacker.Id, withoutImmediate.Id);

            // WHY: A tiny step, far below the interval.
            _system.Tick(0.1f);

            Assert.That(withImmediate.Health, Is.EqualTo(995f).Within(0.001f), "TickOnApply fires on first step");
            Assert.That(withoutImmediate.Health, Is.EqualTo(1000f), "without TickOnApply, nothing yet");
        }

        [Test]
        public void MissedTicks_CatchUpIsBounded()
        {
            AbilityUnit tank = AbilityTestSupport.CreateUnit(_system, team: 2, health: 100000f);
            // WHY: Permanent DoT so duration never interferes with the catch-up count.
            _system.Modifiers.Apply(Dot("bleed", duration: 0f, interval: 1f, damage: 10f), _attacker.Id, tank.Id);

            // WHY: A huge single step: the engine caps catch-up at 8 ticks per Tick call.
            _system.Tick(100f);

            Assert.That(tank.Health, Is.EqualTo(100000f - 80f).Within(0.001f),
                "at most 8 ticks (8 * 10 = 80) fire in one big step");
        }

        [Test]
        public void Reaction_OnTakeDamage_HealsOwner()
        {
            _victim = AbilityTestSupport.CreateUnit(_system, team: 2, health: 100f, healthCurrent: 50f);

            var bp = new ModifierBlueprint { Id = "regrowth", Duration = 0f };
            bp.EventReactions.Add(new ModifierEventReaction
            {
                EventId = AbilityEvents.TakeDamage,
                TargetEventSource = false, // WHY: heal the owner
                Effects = AbilityTestSupport.Nodes(AbilityTestSupport.HealNode(5f))
            });
            _system.Modifiers.Apply(bp, _victim.Id, _victim.Id);

            DamageService.ApplyDamage(_system, _attacker.Id, _victim.Id, 10f, AbilityDamageTypes.Pure);

            // WHY: -10 from the hit, +5 from the reaction.
            Assert.That(_victim.Health, Is.EqualTo(45f).Within(0.001f));
        }

        [Test]
        public void Reaction_TargetEventSource_RoutesToAttacker()
        {
            var thorns = new ModifierBlueprint { Id = "thorns", Duration = 0f };
            thorns.EventReactions.Add(new ModifierEventReaction
            {
                EventId = AbilityEvents.TakeDamage,
                TargetEventSource = true, // WHY: reflect to the attacker
                Effects = AbilityTestSupport.Nodes(AbilityTestSupport.DamageNode(15f))
            });
            _system.Modifiers.Apply(thorns, _victim.Id, _victim.Id);

            DamageService.ApplyDamage(_system, _attacker.Id, _victim.Id, 10f, AbilityDamageTypes.Pure);

            Assert.That(_victim.Health, Is.EqualTo(990f).Within(0.001f), "victim took the direct hit");
            Assert.That(_attacker.Health, Is.EqualTo(985f).Within(0.001f), "attacker took reflected 15");
        }

        [Test]
        public void MutualReactions_TerminateWithinDepthCap()
        {
            var tankA = AbilityTestSupport.CreateUnit(_system, team: 1, health: 100000f);
            var tankB = AbilityTestSupport.CreateUnit(_system, team: 2, health: 100000f);
            var log = new EventLog(_system);

            _system.Modifiers.Apply(Thorns("thornsA", 5f), tankA.Id, tankA.Id);
            _system.Modifiers.Apply(Thorns("thornsB", 5f), tankB.Id, tankB.Id);

            // WHY: This would ping-pong forever without the reaction depth cap.
            DamageService.ApplyDamage(_system, tankA.Id, tankB.Id, 5f, AbilityDamageTypes.Pure);

            int takeDamage = log.Count(AbilityEvents.TakeDamage);
            Assert.That(takeDamage, Is.GreaterThan(1), "the reflection did chain");
            Assert.That(takeDamage, Is.LessThanOrEqualTo(8), "but the depth cap bounds the loop");
        }

        [Test]
        public void AreaDamage_WithNestedThornsReaction_StillHitsEveryAreaTarget()
        {
            var world = new FakeWorldAdapter();
            _system.World = world;

            AbilityUnit a = AbilityTestSupport.CreateUnit(_system, team: 2, health: 1000f);
            AbilityUnit b = AbilityTestSupport.CreateUnit(_system, team: 2, health: 1000f);
            AbilityUnit c = AbilityTestSupport.CreateUnit(_system, team: 2, health: 1000f);
            world.SetPosition(_attacker.Id, Vector3.zero);
            world.SetPosition(a.Id, new Vector3(1f, 0f, 0f));
            world.SetPosition(b.Id, new Vector3(2f, 0f, 0f));
            world.SetPosition(c.Id, new Vector3(3f, 0f, 0f));

            // WHY: thorns on every victim — whichever the area hits first re-enters effect
            // execution mid-iteration and must not clobber the outer target list.
            _system.Modifiers.Apply(Thorns("thornsA", 5f), a.Id, a.Id);
            _system.Modifiers.Apply(Thorns("thornsB", 5f), b.Id, b.Id);
            _system.Modifiers.Apply(Thorns("thornsC", 5f), c.Id, c.Id);

            EffectNodeData node = AbilityTestSupport.DamageNode(10f, target: EffectTargetSelector.AreaAroundCaster);
            node.Radius = 5f;
            node.TeamFilter = AbilityTeamFilter.Enemies;
            EffectContext ctx = AbilityTestSupport.Context(_system, _attacker.Id);

            _system.ExecuteEffects(AbilityTestSupport.Nodes(node), ctx);

            Assert.That(a.Health, Is.EqualTo(990f).Within(0.001f), "first area target damaged");
            Assert.That(b.Health, Is.EqualTo(990f).Within(0.001f),
                "second area target still damaged after the nested reaction");
            Assert.That(c.Health, Is.EqualTo(990f).Within(0.001f),
                "third area target still damaged after the nested reaction");
            Assert.That(_attacker.Health, Is.EqualTo(985f).Within(0.001f), "one thorns reflection per victim");
        }

        [Test]
        public void TwoReactiveModifiers_BothFire_WhenFirstReactionDamagesAttacker()
        {
            _victim = AbilityTestSupport.CreateUnit(_system, team: 2, health: 100f, healthCurrent: 50f);
            _system.Modifiers.Apply(Thorns("thorns", 15f), _victim.Id, _victim.Id);

            var regrowth = new ModifierBlueprint { Id = "regrowth", Duration = 0f };
            regrowth.EventReactions.Add(new ModifierEventReaction
            {
                EventId = AbilityEvents.TakeDamage,
                TargetEventSource = false,
                Effects = AbilityTestSupport.Nodes(AbilityTestSupport.HealNode(5f))
            });
            _system.Modifiers.Apply(regrowth, _victim.Id, _victim.Id);

            DamageService.ApplyDamage(_system, _attacker.Id, _victim.Id, 10f, AbilityDamageTypes.Pure);

            Assert.That(_attacker.Health, Is.EqualTo(985f).Within(0.001f), "thorns fired");
            // WHY: -10 hit, +5 regrowth — the second reaction must survive the nested thorns execution.
            Assert.That(_victim.Health, Is.EqualTo(45f).Within(0.001f),
                "the victim's second reactive modifier still fires");
        }

        private static ModifierBlueprint Thorns(string id, float reflect)
        {
            var bp = new ModifierBlueprint { Id = id, Duration = 0f };
            bp.EventReactions.Add(new ModifierEventReaction
            {
                EventId = AbilityEvents.TakeDamage,
                TargetEventSource = true,
                Effects = AbilityTestSupport.Nodes(AbilityTestSupport.DamageNode(reflect))
            });
            return bp;
        }
    }
}
