using Neo.Abilities;
using NUnit.Framework;

namespace Neo.Editor.Tests.Abilities
{
    public sealed class Audit20261023TestGameplayProbabilityTests
    {
        [TestCase(0f, 20f, 2)]
        [TestCase(0.499f, 20f, 2)]
        [TestCase(0.5f, 50f, 1)]
        [TestCase(0.999f, 50f, 1)]
        public void ExecuteEffects_ChanceThresholdControlsBonusDamageAndReceipts(
            float roll, float expectedHealth, int expectedDamageReceipts)
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, team: 1, health: 100f);
            AbilityUnit target = AbilityTestSupport.CreateUnit(system, team: 2, health: 100f);
            EventLog receipts = new EventLog(system);
            EffectNodeData bonus = AbilityTestSupport.DamageNode(30f);
            bonus.Chance = 0.5f;
            EffectContext context = new EffectContext(system, caster.Id, "audit_probability",
                new FixedRandomSource(roll));
            context.PrimaryTargets.Add(target.Id);

            system.ExecuteEffects(AbilityTestSupport.Nodes(AbilityTestSupport.DamageNode(50f), bonus), context);

            Assert.That(target.Health, Is.EqualTo(expectedHealth).Within(0.001f));
            Assert.That(caster.Health, Is.EqualTo(100f));
            Assert.That(receipts.Count(AbilityEvents.TakeDamage), Is.EqualTo(expectedDamageReceipts));
            Assert.That(receipts.TryGetLast(AbilityEvents.TakeDamage, out AbilityEventArgs lastDamage), Is.True);
            Assert.That(lastDamage.Target, Is.EqualTo(target.Id));
            Assert.That(lastDamage.Source, Is.EqualTo(caster.Id));
            Assert.That(lastDamage.Amount, Is.EqualTo(expectedDamageReceipts == 2 ? 30f : 50f).Within(0.001f));

            float reportedDamage = 0f;
            foreach (AbilityEventArgs receipt in receipts.Events)
            {
                if (receipt.EventId == AbilityEvents.TakeDamage)
                {
                    Assert.That(receipt.Target, Is.EqualTo(target.Id));
                    Assert.That(receipt.Source, Is.EqualTo(caster.Id));
                    reportedDamage += receipt.Amount;
                }
            }

            Assert.That(reportedDamage, Is.EqualTo(100f - expectedHealth).Within(0.001f),
                "Without shields, healing or reactions, damage receipts must account for the exact health removed.");
        }

        // A deterministic adapter for the existing injection seam; no global random state is changed.
        private sealed class FixedRandomSource : IRandomSource
        {
            private readonly float _roll;

            public FixedRandomSource(float roll)
            {
                _roll = roll;
            }

            public float NextFloat()
            {
                return _roll;
            }

            public int NextInt(int maxExclusive)
            {
                return 0;
            }
        }
    }
}
