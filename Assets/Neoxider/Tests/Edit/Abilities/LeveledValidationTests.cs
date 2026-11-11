using System.Collections.Generic;
using Neo.Abilities;
using NUnit.Framework;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     AbilityContentValidator findings for the leveled and scalable fields: per-level cast parameters, cost modes and
    ///     keys, node level arrays, spent and scale terms, modifier strength by level.
    /// </summary>
    public sealed class LeveledValidationTests
    {
        private static List<AbilityContentIssue> Validate(AbilityBlueprint ability, ModifierBlueprint modifier = null)
        {
            return AbilityContentValidator.Validate(new[] { ability },
                modifier != null ? new[] { modifier } : new ModifierBlueprint[0]);
        }

        private static AbilityBlueprint Ability()
        {
            AbilityBlueprint ability = new AbilityBlueprint { Id = "val_ability" };
            ability.ImpactEffects.Add(AbilityTestSupport.DamageNode(5f));
            return ability;
        }

        private static bool Has(List<AbilityContentIssue> issues, AbilityIssueSeverity severity, string fragment)
        {
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Severity == severity && issues[i].Message.Contains(fragment))
                {
                    return true;
                }
            }

            return false;
        }

        [Test]
        public void ValidLeveledContent_HasNoIssues()
        {
            AbilityBlueprint ability = Ability();
            ability.CooldownByLevel = new[] { 10f, 8f, 6f };
            ability.MaxChargesByLevel = new[] { 1, 2 };
            ability.Costs.Add(new AbilityCost(AbilityResourceIds.Mana, 0f, AbilityCostMode.All, 1f));
            ability.ImpactEffects[0].AmountSpentResource = AbilityResourceIds.Mana;
            ability.ImpactEffects[0].AmountSpentCoefficient = 2f;
            ability.ImpactEffects[0].ChanceByLevel = new[] { 0.5f, 1f };

            Assert.That(Validate(ability), Is.Empty);
        }

        [Test]
        public void NegativeOrNonFiniteCastLevels_AreErrors()
        {
            AbilityBlueprint ability = Ability();
            ability.CooldownByLevel = new[] { 5f, -1f };
            ability.RangeByLevel = new[] { float.NaN };
            ability.MaxChargesByLevel = new[] { -2 };

            List<AbilityContentIssue> issues = Validate(ability);
            Assert.That(Has(issues, AbilityIssueSeverity.Error, "CooldownByLevel"), Is.True);
            Assert.That(Has(issues, AbilityIssueSeverity.Error, "RangeByLevel"), Is.True);
            Assert.That(Has(issues, AbilityIssueSeverity.Error, "MaxChargesByLevel"), Is.True);
        }

        [Test]
        public void CostProblems_AreReported()
        {
            AbilityBlueprint ability = Ability();
            ability.Costs.Add(new AbilityCost(null, 0f, AbilityCostMode.UpTo));
            ability.Costs.Add(new AbilityCost(AbilityResourceIds.Mana, 0f) { AmountKey = "missing" });
            ability.Costs.Add(new AbilityCost(AbilityResourceIds.Mana, 5f) { MinAmount = 3f });
            ability.Costs.Add(new AbilityCost(AbilityResourceIds.Mana, 5f, AbilityCostMode.All, -1f));

            List<AbilityContentIssue> issues = Validate(ability);
            Assert.That(Has(issues, AbilityIssueSeverity.Error, "has no ResourceId"), Is.True);
            Assert.That(Has(issues, AbilityIssueSeverity.Error, "AmountKey 'missing'"), Is.True);
            Assert.That(Has(issues, AbilityIssueSeverity.Warning, "MinAmount is only read"), Is.True);
            Assert.That(Has(issues, AbilityIssueSeverity.Error, "MinAmount must be finite"), Is.True);
            Assert.That(Has(issues, AbilityIssueSeverity.Warning, "ignores the amount"), Is.True);
        }

        [Test]
        public void NodeLevelArrays_AreChecked()
        {
            AbilityBlueprint ability = Ability();
            ability.ImpactEffects[0].ChanceByLevel = new[] { 0.5f, 1.5f };
            ability.ImpactEffects[0].MaxTargetsByLevel = new[] { -1 };
            ability.ImpactEffects[0].DurationByLevel = new[] { 2f };
            ability.ImpactEffects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.Zone, Radius = 2f, AbilityId = "val_ability", Target = EffectTargetSelector.Caster,
                DurationByLevel = new[] { 1f, -1f }
            });

            List<AbilityContentIssue> issues = Validate(ability);
            Assert.That(Has(issues, AbilityIssueSeverity.Error, "ChanceByLevel[1]"), Is.True);
            Assert.That(Has(issues, AbilityIssueSeverity.Error, "MaxTargetsByLevel[0]"), Is.True);
            Assert.That(Has(issues, AbilityIssueSeverity.Warning, "only read by the zone op"), Is.True);
            Assert.That(Has(issues, AbilityIssueSeverity.Error, "zone DurationByLevel"), Is.True);
        }

        [Test]
        public void SpentAndScaleTerms_AreChecked()
        {
            AbilityBlueprint ability = Ability();
            ability.ImpactEffects[0].AmountSpentResource = "rage";
            ability.ImpactEffects[0].AmountSpentCoefficient = 1f;
            ability.ImpactEffects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.Heal, AmountSpentResource = AbilityResourceIds.Mana,
                AmountScaleMode = ScaleAmountMode.Multiply
            });
            ability.Costs.Add(new AbilityCost(AbilityResourceIds.Mana, 10f));

            List<AbilityContentIssue> issues = Validate(ability);
            Assert.That(Has(issues, AbilityIssueSeverity.Warning, "'rage' is not paid"), Is.True);
            Assert.That(Has(issues, AbilityIssueSeverity.Warning, "has a 0 coefficient"), Is.True);
            Assert.That(Has(issues, AbilityIssueSeverity.Warning, "no scale property"), Is.True);
        }

        [Test]
        public void ModifierValueByLevel_NonFiniteIsAnError()
        {
            ModifierBlueprint modifier = AbilityTestSupport.Modifier("val_mod", 1f);
            modifier.Properties.Add(new PropertyContribution(AbilityProperties.MoveSpeed, PropertyOp.Mul, 0.8f)
                { ValueByLevel = new[] { 0.8f, float.PositiveInfinity } });
            modifier.Properties.Add(new PropertyContribution(AbilityProperties.Armor, PropertyOp.Mul, 1f)
                { ValueByLevel = new[] { -0.5f } });

            List<AbilityContentIssue> issues = Validate(Ability(), modifier);
            Assert.That(Has(issues, AbilityIssueSeverity.Error, "[Property #1] ValueByLevel"), Is.True);
            Assert.That(Has(issues, AbilityIssueSeverity.Warning, "[Property #2]"), Is.True);
        }
    }
}
