using System.Collections.Generic;
using Neo.Abilities;
using Neo.Abilities.Fixed;
using NUnit.Framework;

namespace Neo.Editor.Tests.Abilities.Fixed
{
    /// <summary>Conversion of float content to fixed blueprints and its report.</summary>
    public sealed class FixedContentConverterTests
    {
        [TestCase(0.1d, 3)]
        [TestCase(1d, 30)]
        [TestCase(0.25d, 8)]
        [TestCase(1.2d, 36)]
        [TestCase(0d, 0)]
        [TestCase(-1d, 0)]
        [TestCase(0.001d, 1)]
        public void Seconds_BecomeTicksEndingOnTheFloatStep(double seconds, int ticks)
        {
            Assert.That(FixedContentConverter.ToTicks((float)seconds, 30), Is.EqualTo(ticks));
        }

        [TestCase(0.5d, 128)]
        [TestCase(0.3d, 77)]
        [TestCase(-0.3d, -77)]
        [TestCase(1.5d / 256d, 2)]
        [TestCase(-1.5d / 256d, -2)]
        [TestCase(1e12d, int.MaxValue)]
        public void Numbers_RoundHalfAwayFromZeroAndSaturate(double value, int raw)
        {
            Assert.That(FixedContentConverter.ToRaw(value, FixedFormat.Q8), Is.EqualTo(raw));
        }

        [Test]
        public void Modifier_ConvertsEveryFieldAndReportsTicksAndSaturation()
        {
            ModifierBlueprint source = new ModifierBlueprint
            {
                Id = "burn", Duration = 0.25f, TickInterval = 0.1f, StackPolicy = ModifierStackPolicy.Stack, MaxStacks = 4,
                TickOnApply = true, IsDebuff = true, Dispellable = false, DurationByLevel = new[] { 1f, 2f },
                RestartTicksOnRefresh = true
            };
            source.Properties.Add(new PropertyContribution(AbilityProperties.MoveSpeed, PropertyOp.Mul, 1.1f, 0.05f));
            source.Properties.Add(new PropertyContribution(AbilityProperties.Armor, PropertyOp.Add, 2.5f));
            source.Properties.Add(new PropertyContribution(AbilityProperties.Armor, PropertyOp.Max, 1e12f));
            source.States.Add(AbilityStates.Rooted);
            source.TickEffects.Add(new EffectNodeData { OpId = AbilityEffectOps.Damage, Amount = 4f, AmountPerStack = 2f, Chance = 0.5f });
            ModifierEventReaction reaction = new ModifierEventReaction
            {
                EventId = AbilityEvents.TakeDamage, Trigger = ModifierTrigger.HealthBelow, Threshold = 0.25f, Cooldown = 2f,
                MaxTriggers = 3, RemoveWhenSpent = true, Priority = 2, HitsOnly = true
            };
            reaction.Effects.Add(new EffectNodeData { OpId = AbilityEffectOps.Knockback, Amount = 3f });
            source.EventReactions.Add(reaction);
            List<FixedConversionIssue> issues = new List<FixedConversionIssue>();

            FixedModifierBlueprint result = FixedContentConverter.Convert(source, new FixedConversionSettings(), issues);

            Assert.That(result.DurationTicks, Is.EqualTo(8));
            Assert.That(result.TickIntervalTicks, Is.EqualTo(3));
            Assert.That(result.DurationByLevelTicks, Is.EqualTo(new[] { 30, 60 }));
            Assert.That(result.StackPolicy, Is.EqualTo(FixedStackPolicy.Stack));
            Assert.That(result.MaxStacks, Is.EqualTo(4));
            Assert.That(result.TickOnApply && result.IsDebuff && !result.Dispellable && result.RestartTicksOnRefresh, Is.True);
            Assert.That(result.Properties[0].ValueRaw, Is.EqualTo(110), "an off-grid whole percent stays exact");
            Assert.That(result.Properties[0].PerStackRaw, Is.EqualTo(5));
            Assert.That(result.Properties[0].Denominator, Is.EqualTo(100));
            Assert.That(result.Properties[1].ValueRaw, Is.EqualTo(640));
            Assert.That(result.Properties[1].Denominator, Is.Zero);
            Assert.That(result.Properties[2].ValueRaw, Is.EqualTo(int.MaxValue));
            Assert.That(result.States, Is.EqualTo(new[] { AbilityStates.Rooted }));
            Assert.That(result.TickEffects[0].AmountRaw, Is.EqualTo(1024));
            Assert.That(result.TickEffects[0].AmountPerStackRaw, Is.EqualTo(512));
            Assert.That(result.TickEffects[0].ChanceRaw, Is.EqualTo(128));
            FixedModifierReaction converted = result.EventReactions[0];
            Assert.That(converted.Trigger, Is.EqualTo(FixedModifierTrigger.HealthBelow));
            Assert.That(converted.ThresholdRaw, Is.EqualTo(64));
            Assert.That(converted.CooldownTicks, Is.EqualTo(60));
            Assert.That(converted.MaxTriggers == 3 && converted.RemoveWhenSpent && converted.Priority == 2 && converted.HitsOnly,
                Is.True);

            Assert.That(issues.Exists(i => i.Field == "Duration" && i.Severity == FixedConversionSeverity.Warning), Is.True,
                "0.25 s is 7.5 ticks");
            Assert.That(issues.Exists(i => i.Field == "TickInterval"), Is.False, "0.1 s is exactly 3 ticks");
            Assert.That(issues.Exists(i => i.Field == "Properties[2].Value" && i.Severity == FixedConversionSeverity.Warning),
                Is.True, "saturated");
            Assert.That(issues.Exists(i => i.Field.EndsWith(".OpId") && i.Message.Contains("knockback")), Is.True,
                "motion ops are not in the fixed core");
            Assert.That(issues.Exists(i => i.Severity == FixedConversionSeverity.Info), Is.False);
        }

        [Test]
        public void Ability_ConvertsNodesSpecialsCostsAndReportsFractionalAngles()
        {
            AbilityBlueprint source = new AbilityBlueprint
            {
                Id = "breath", Targeting = TargetingMode.Point, TeamFilter = AbilityTeamFilter.Any, Range = 7.5f,
                Cooldown = 3f, MaxCharges = 2, ChargeRestoreTime = 1.5f, Delivery = AbilityDeliveryType.Projectile,
                ProjectileArchetypeId = "orb", ProjectileSpeed = 12f
            };
            source.Costs.Add(new AbilityCost(AbilityResourceIds.Mana, 15f));
            source.ImpactEffects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.Damage, Target = EffectTargetSelector.ConeFromCaster, Angle = 60.5f, Radius = 4f,
                AmountKey = "dmg", MaxTargets = 3, DamageType = AbilityDamageTypes.Physical
            });
            source.CastEffects.Add(new EffectNodeData
                { OpId = AbilityEffectOps.Zone, Duration = 6f, Interval = 0.5f, Radius = 2f, AbilityId = "pulse" });
            source.Specials.Add(new AbilitySpecialValue
            {
                Name = "dmg",
                Value = new LeveledValue { Base = 10f, Levels = new[] { 10f, 15f }, ScaleProperty = "spell_power", ScalePerPoint = 0.5f }
            });
            List<FixedConversionIssue> issues = new List<FixedConversionIssue>();

            FixedAbilityBlueprint result = FixedContentConverter.Convert(source, new FixedConversionSettings(), issues);

            Assert.That(result.Targeting, Is.EqualTo(FixedTargeting.Point));
            Assert.That(result.TeamFilter, Is.EqualTo(FixedTeamFilter.Any));
            Assert.That(result.RangeRaw, Is.EqualTo(1920));
            Assert.That(result.CooldownTicks, Is.EqualTo(90));
            Assert.That(result.ChargeRestoreTicks, Is.EqualTo(45));
            Assert.That(result.MaxCharges, Is.EqualTo(2));
            Assert.That(result.Delivery, Is.EqualTo(FixedDelivery.Projectile));
            Assert.That(result.ProjectileSpeedRaw, Is.EqualTo(3072));
            Assert.That(result.Costs[0].AmountRaw, Is.EqualTo(3840));
            FixedEffectNode cone = result.ImpactEffects[0];
            Assert.That(cone.Target, Is.EqualTo(FixedTargetSelector.ConeFromCaster));
            Assert.That(cone.AngleDegrees, Is.EqualTo(61));
            Assert.That(cone.RadiusRaw, Is.EqualTo(1024));
            Assert.That(cone.ChanceRaw, Is.EqualTo(FixedEffectNode.Always));
            Assert.That(cone.AmountKey, Is.EqualTo("dmg"));
            Assert.That(result.CastEffects[0].DurationTicks, Is.EqualTo(180));
            Assert.That(result.CastEffects[0].IntervalTicks, Is.EqualTo(15));
            Assert.That(result.Specials[0].Value.LevelsRaw, Is.EqualTo(new[] { 2560, 3840 }));
            Assert.That(result.Specials[0].Value.ScalePerPointRaw, Is.EqualTo(128));
            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].Field, Is.EqualTo("ImpactEffects[0].Angle"));
        }

        [Test]
        public void OffGridReport_IsOptIn()
        {
            ModifierBlueprint source = new ModifierBlueprint { Id = "slow" };
            source.Properties.Add(new PropertyContribution(AbilityProperties.Armor, PropertyOp.Add, 0.3f));
            List<FixedConversionIssue> issues = new List<FixedConversionIssue>();
            FixedContentConverter.Convert(source, new FixedConversionSettings { ReportOffGridValues = true }, issues);
            Assert.That(issues.Exists(i => i.Severity == FixedConversionSeverity.Info && i.Field == "Properties[0].Value"),
                Is.True);
        }

        [Test]
        public void EveryTemplate_ConvertsWithoutErrorsAndRuns()
        {
            AbilityTemplateBundle[] bundles =
            {
                AbilityTemplates.AutoCastTechnique(), AbilityTemplates.AttackEnhancer(), AbilityTemplates.AutoDefenseShield(),
                AbilityTemplates.Mark(), AbilityTemplates.StackingPoison(), AbilityTemplates.Thorns(), AbilityTemplates.Aura(),
                AbilityTemplates.FormationZone()
            };
            FixedConversionSettings settings = new FixedConversionSettings();
            FixedAbilitySystem system = new FixedAbilitySystem(settings.CreateRules());
            FixedUnit hero = FixedTestSupport.Unit(system, 1, mana: 100d);
            FixedTestSupport.Unit(system, 2, x: 3d);
            for (int b = 0; b < bundles.Length; b++)
            {
                List<FixedConversionIssue> issues = new List<FixedConversionIssue>();
                FixedContentBundle bundle = FixedContentConverter.Convert(bundles[b].Abilities, bundles[b].Modifiers,
                    settings, issues);
                Assert.That(issues.Exists(i => i.Severity == FixedConversionSeverity.Error), Is.False, bundles[b].TemplateId);
                bundle.RegisterInto(system);
                for (int i = 0; i < bundles[b].GrantAbilityIds.Count; i++)
                {
                    Assert.That(system.GrantAbility(hero.Id, bundles[b].GrantAbilityIds[i]), Is.Not.Null);
                }

                for (int i = 0; i < bundles[b].InnateModifierIds.Count; i++)
                {
                    Assert.That(system.ApplyModifier(bundles[b].InnateModifierIds[i], hero.Id, hero.Id).Instance, Is.Not.Null);
                }
            }

            system.Step(300);
            Assert.That(hero.IsAlive, Is.True);
        }
    }
}
