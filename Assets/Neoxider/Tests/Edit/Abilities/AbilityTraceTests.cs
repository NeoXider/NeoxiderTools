using System.Collections.Generic;
using Neo.Abilities;
using NUnit.Framework;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     Contracts of the runtime trace ring buffer: disabled by default, causal parents from a cast down to a
    ///     reaction ("why did this fire"), skipped triggers, wrap-around and explanation text.
    /// </summary>
    public sealed class AbilityTraceTests
    {
        [Test]
        public void Trace_IsDisabledByDefault_AndRecordsNothing()
        {
            AbilitySystem system = BuildMarkScenario(out AbilityUnit hero, out AbilityUnit enemy);

            system.Cast(CastRequest.AtUnit(hero.Id, "strike", enemy.Id));

            Assert.That(system.Trace.Enabled, Is.False);
            Assert.That(system.Trace.Count, Is.Zero);
        }

        [Test]
        public void Explain_WalksFromAReactionBackToTheCast()
        {
            AbilitySystem system = BuildMarkScenario(out AbilityUnit hero, out AbilityUnit enemy);
            system.Trace.Enabled = true;

            system.Cast(CastRequest.AtUnit(hero.Id, "strike", enemy.Id));

            Assert.That(system.Trace.TryFindLatest(AbilityTraceKind.Reaction, "marked", out AbilityTraceEntry reaction),
                Is.True);
            Assert.That(reaction.Detail, Is.EqualTo(AbilityEvents.BeforeTakeDamage));
            Assert.That(reaction.Unit, Is.EqualTo(enemy.Id));
            string why = system.Trace.Explain(reaction.Sequence);
            StringAssert.Contains("reaction 'marked'", why);
            StringAssert.Contains("because #", why);
            StringAssert.Contains("event before_take_damage", why);
            StringAssert.Contains("effect damage", why);
            StringAssert.Contains("cast 'strike'", why);

            AbilityUnit heroUnit = hero;
            string named = system.Trace.Explain(reaction.Sequence, 12,
                id => id == heroUnit.Id ? "Hero" : "Enemy");
            StringAssert.StartsWith("#" + reaction.Sequence + " reaction 'marked'[0] on Enemy: event", named,
                "a single-stack reaction prints no stack count");
            StringAssert.Contains("cast 'strike' by Hero", named);

            List<AbilityTraceEntry> entries = new List<AbilityTraceEntry>();
            system.Trace.CopyTo(entries);
            Assert.That(entries[0].Kind, Is.EqualTo(AbilityTraceKind.Cast), "the cast is the root");
            Assert.That(entries[0].Parent, Is.Zero);
        }

        [Test]
        public void SpentCharges_AreRecordedAsSkippedTriggers()
        {
            AbilitySystem system = BuildMarkScenario(out AbilityUnit hero, out AbilityUnit enemy);
            ModifierBlueprint marked = null;
            system.TryGetModifier("marked", out marked);
            marked.EventReactions[0].RemoveWhenSpent = false;
            system.Trace.Enabled = true;

            system.Cast(CastRequest.AtUnit(hero.Id, "strike", enemy.Id));
            system.Cast(CastRequest.AtUnit(hero.Id, "strike", enemy.Id));

            Assert.That(system.Trace.TryFindLatest(AbilityTraceKind.TriggerSkipped, "marked", out AbilityTraceEntry skip),
                Is.True);
            Assert.That(skip.Reason, Is.EqualTo("charges spent"));
        }

        [Test]
        public void RingBuffer_OverwritesTheOldestRecords()
        {
            AbilitySystem system = BuildMarkScenario(out AbilityUnit hero, out AbilityUnit enemy);
            system.Trace.SetCapacity(8);
            system.Trace.Enabled = true;

            for (int i = 0; i < 10; i++)
            {
                system.Cast(CastRequest.AtUnit(hero.Id, "strike", enemy.Id));
            }

            Assert.That(system.Trace.Count, Is.EqualTo(8));
            Assert.That(system.Trace.TryGet(1, out _), Is.False);
            Assert.That(system.Trace.TryGet(system.Trace.NextSequence - 1, out AbilityTraceEntry newest), Is.True);
            Assert.That(newest.IsValid, Is.True);
            StringAssert.Contains("no longer", system.Trace.Explain(1));
            Assert.That(system.Trace.Dump(4).Split('\n').Length, Is.EqualTo(4));
        }

        private static AbilitySystem BuildMarkScenario(out AbilityUnit hero, out AbilityUnit enemy)
        {
            AbilitySystem system = new AbilitySystem();
            hero = AbilityTestSupport.CreateUnit(system, team: 1, name: "Hero");
            enemy = AbilityTestSupport.CreateUnit(system, team: 2, name: "Enemy");
            AbilityBlueprint strike = new AbilityBlueprint { Id = "strike", Targeting = TargetingMode.Unit };
            strike.ImpactEffects.Add(AbilityTestSupport.DamageNode(10f));
            system.RegisterAbility(strike);
            system.GrantAbility(hero.Id, "strike");

            ModifierBlueprint marked = AbilityTestSupport.Modifier("marked", 30f);
            ModifierEventReaction bonus = new ModifierEventReaction
            {
                EventId = AbilityEvents.BeforeTakeDamage,
                MaxTriggers = 1,
                RemoveWhenSpent = true
            };
            bonus.Effects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.ModifyDamage,
                CustomParam = "add",
                Amount = 5f
            });
            marked.EventReactions.Add(bonus);
            system.RegisterModifier(marked);
            system.ApplyModifier("marked", hero.Id, enemy.Id);
            return system;
        }
    }
}
