using System.Collections.Generic;
using Neo.Abilities;
using NUnit.Framework;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>Findings of the runtime content validator for the trigger, hook, payload, zone and cone settings.</summary>
    public sealed class AbilityContentValidatorTests
    {
        [Test]
        public void TriggerMisconfigurations_AreReported()
        {
            ModifierBlueprint modifier = AbilityTestSupport.Modifier("bad_triggers");
            modifier.EventReactions.Add(new ModifierEventReaction
            {
                EventId = AbilityEvents.DealDamage,
                Trigger = ModifierTrigger.HealthBelow,
                Threshold = 1.5f
            });
            modifier.EventReactions.Add(new ModifierEventReaction { Trigger = ModifierTrigger.StateEnter });
            modifier.EventReactions.Add(new ModifierEventReaction
            {
                EventId = AbilityEvents.TakeDamage,
                RemoveWhenSpent = true,
                HitsOnly = true,
                Cooldown = -1f
            });
            modifier.EventReactions.Add(new ModifierEventReaction { EventId = AbilityEvents.ModifierApplied });

            List<AbilityContentIssue> issues = Validate(null, modifier);

            AssertHas(issues, AbilityIssueSeverity.Error, "HealthBelow is evaluated on take_damage");
            AssertHas(issues, AbilityIssueSeverity.Error, "must be in (0, 1]");
            AssertHas(issues, AbilityIssueSeverity.Error, "StateEnter needs a StateId");
            AssertHas(issues, AbilityIssueSeverity.Warning, "RemoveWhenSpent without MaxTriggers");
            AssertHas(issues, AbilityIssueSeverity.Warning, "HitsOnly only filters");
            AssertHas(issues, AbilityIssueSeverity.Error, "Cooldown is negative");
            AssertHas(issues, AbilityIssueSeverity.Warning, "use the Applied");
        }

        [Test]
        public void HookOpsOutsideHooks_UnknownModes_AndSelfDamageLoops_AreReported()
        {
            AbilityBlueprint ability = new AbilityBlueprint { Id = "misuse" };
            ability.ImpactEffects.Add(new EffectNodeData { OpId = AbilityEffectOps.Reflect, Amount = 0.2f });
            ModifierBlueprint modifier = AbilityTestSupport.Modifier("hooks");
            ModifierEventReaction hook = new ModifierEventReaction { EventId = AbilityEvents.BeforeTakeDamage };
            hook.Effects.Add(new EffectNodeData { OpId = AbilityEffectOps.ModifyDamage, CustomParam = "double" });
            hook.Effects.Add(new EffectNodeData { OpId = AbilityEffectOps.ModifyDamage });
            modifier.EventReactions.Add(hook);
            ModifierEventReaction loop = new ModifierEventReaction { EventId = AbilityEvents.TakeDamage };
            loop.Effects.Add(AbilityTestSupport.DamageNode(1f));
            modifier.EventReactions.Add(loop);

            List<AbilityContentIssue> issues = Validate(ability, modifier);

            AssertHas(issues, AbilityIssueSeverity.Warning, "reflect only works in a before_deal_damage");
            AssertHas(issues, AbilityIssueSeverity.Error, "mode 'double' is unknown");
            AssertHas(issues, AbilityIssueSeverity.Warning, "multiply with Amount 0");
            AssertHas(issues, AbilityIssueSeverity.Warning, "re-triggers itself");
        }

        [Test]
        public void PayloadZoneAndConeProblems_AreReported()
        {
            AbilityBlueprint ability = new AbilityBlueprint { Id = "payloads" };
            ability.ImpactEffects.Add(new EffectNodeData { OpId = AbilityEffectOps.Projectile });
            ability.ImpactEffects.Add(new EffectNodeData { OpId = AbilityEffectOps.CastAbility, AbilityId = "missing" });
            ability.ImpactEffects.Add(new EffectNodeData { OpId = AbilityEffectOps.Zone, Radius = 0f });
            ability.ImpactEffects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.Damage,
                Target = EffectTargetSelector.ConeFromCaster,
                Radius = 3f
            });
            ability.ImpactEffects.Add(new EffectNodeData { OpId = "not_an_op" });

            List<AbilityContentIssue> issues = Validate(ability, null);

            AssertHas(issues, AbilityIssueSeverity.Error, "projectile needs an AbilityId");
            AssertHas(issues, AbilityIssueSeverity.Error, "'missing' does not match any ability");
            AssertHas(issues, AbilityIssueSeverity.Error, "zone needs a ModifierId, an AbilityId payload or both");
            AssertHas(issues, AbilityIssueSeverity.Error, "zone needs a positive Radius");
            AssertHas(issues, AbilityIssueSeverity.Error, "ConeFromCaster needs an Angle");
            AssertHas(issues, AbilityIssueSeverity.Error, "op 'not_an_op' is not registered");
        }

        [Test]
        public void CastAbilityCycles_AreReported()
        {
            AbilityBlueprint a = new AbilityBlueprint { Id = "ping" };
            a.ImpactEffects.Add(new EffectNodeData { OpId = AbilityEffectOps.CastAbility, AbilityId = "pong" });
            AbilityBlueprint b = new AbilityBlueprint { Id = "pong" };
            b.ImpactEffects.Add(new EffectNodeData { OpId = AbilityEffectOps.CastAbility, AbilityId = "ping" });

            List<AbilityContentIssue> issues = AbilityContentValidator.Validate(new List<AbilityBlueprint> { a, b },
                new List<ModifierBlueprint>(), null);

            AssertHas(issues, AbilityIssueSeverity.Warning, "cast_ability cycle ping -> pong -> ping");
        }

        private static List<AbilityContentIssue> Validate(AbilityBlueprint ability, ModifierBlueprint modifier)
        {
            EffectOpRegistry ops = new EffectOpRegistry();
            DefaultEffectOps.RegisterAll(ops);
            List<AbilityBlueprint> abilities = new List<AbilityBlueprint>();
            if (ability != null)
            {
                abilities.Add(ability);
            }

            List<ModifierBlueprint> modifiers = new List<ModifierBlueprint>();
            if (modifier != null)
            {
                modifiers.Add(modifier);
            }

            return AbilityContentValidator.Validate(abilities, modifiers, ops);
        }

        private static void AssertHas(List<AbilityContentIssue> issues, AbilityIssueSeverity severity, string fragment)
        {
            foreach (AbilityContentIssue issue in issues)
            {
                if (issue.Severity == severity && issue.Message.Contains(fragment))
                {
                    return;
                }
            }

            Assert.Fail("Expected a " + severity + " containing \"" + fragment + "\". Got:\n" +
                        string.Join("\n", issues));
        }
    }
}
