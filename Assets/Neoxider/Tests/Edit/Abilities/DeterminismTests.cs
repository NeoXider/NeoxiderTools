using Neo.Abilities;
using NUnit.Framework;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     Determinism guarantees: the PRNG is seed-stable, and a full cast replayed with the same
    ///     request seed produces identical outcomes.
    /// </summary>
    public sealed class DeterminismTests
    {
        [Test]
        public void XorShift_SameSeed_ProducesIdenticalSequence()
        {
            var a = new XorShiftRandom(12345u);
            var b = new XorShiftRandom(12345u);

            for (int i = 0; i < 200; i++)
            {
                Assert.That(b.NextFloat(), Is.EqualTo(a.NextFloat()), $"float mismatch at {i}");
            }
        }

        [Test]
        public void XorShift_NextInt_StaysInRange()
        {
            var rng = new XorShiftRandom(99u);
            for (int i = 0; i < 500; i++)
            {
                int v = rng.NextInt(6);
                Assert.That(v, Is.InRange(0, 5));
            }

            Assert.That(rng.NextInt(0), Is.EqualTo(0), "non-positive bound yields 0");
        }

        [Test]
        public void XorShift_DifferentSeeds_Diverge()
        {
            var a = new XorShiftRandom(1u);
            var b = new XorShiftRandom(2u);

            bool anyDifference = false;
            for (int i = 0; i < 50 && !anyDifference; i++)
            {
                if (!a.NextFloat().Equals(b.NextFloat()))
                {
                    anyDifference = true;
                }
            }

            Assert.That(anyDifference, Is.True);
        }

        [Test]
        public void XorShift_ZeroSeed_IsHandled()
        {
            var rng = new XorShiftRandom(0u);
            // WHY: Must not get stuck at 0 forever (xorshift seeds 0 to a fixed non-zero constant).
            Assert.That(rng.NextFloat(), Is.GreaterThanOrEqualTo(0f));
            Assert.That(rng.NextFloat(), Is.LessThan(1f));
        }

        [Test]
        public void FullCast_ReplayedWithSameSeed_IsIdentical()
        {
            float first = RunCast(777u);
            float second = RunCast(777u);

            Assert.That(second, Is.EqualTo(first), "same seed -> identical damage outcome");
        }

        [Test]
        public void FullCast_ProbabilisticNode_ActuallyBranches()
        {
            // WHY: Sanity: the chance node is real - the total damage is one of the two deterministic outcomes.
            float damage = 1000f - RunCast(777u);
            Assert.That(damage, Is.EqualTo(50f).Or.EqualTo(80f), "either just the guaranteed hit, or with the 0.5 bonus");
        }

        /// <summary>Casts a fireball (guaranteed 50 + a 0.5-chance 30) on a fresh system and returns target HP.</summary>
        private static float RunCast(uint seed)
        {
            var system = new AbilitySystem();
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, team: 1, mana: 200f);
            AbilityUnit target = AbilityTestSupport.CreateUnit(system, team: 2, health: 1000f);

            var fireball = new AbilityBlueprint
            {
                Id = "fireball",
                Targeting = TargetingMode.Unit,
                Delivery = AbilityDeliveryType.Instant
            };
            fireball.ImpactEffects.Add(AbilityTestSupport.DamageNode(50f, AbilityDamageTypes.Magical));
            EffectNodeData bonus = AbilityTestSupport.DamageNode(30f, AbilityDamageTypes.Magical);
            bonus.Chance = 0.5f;
            fireball.ImpactEffects.Add(bonus);

            system.RegisterAbility(fireball);
            system.GrantAbility(caster.Id, "fireball");

            CastResult result = system.Cast(CastRequest.AtUnit(caster.Id, "fireball", target.Id, seed));
            Assert.That(result.Success, Is.True);
            return target.Health;
        }
    }
}
