using Neo.NPC.Combat;
using NUnit.Framework;

namespace Neo.NPC.Tests
{
    public class NpcCombatDecisionCoreTests
    {
        [Test]
        public void Decide_WithoutTarget_ReturnsAcquireTarget()
        {
            NpcCombatDecisionCore.Decision decision = NpcCombatDecisionCore.Decide(
                false,
                true,
                true,
                float.PositiveInfinity,
                2f,
                10f);

            Assert.That(decision, Is.EqualTo(NpcCombatDecisionCore.Decision.AcquireTarget));
        }

        [Test]
        public void Decide_WhenTargetTooFar_ReturnsChaseTarget()
        {
            NpcCombatDecisionCore.Decision decision = NpcCombatDecisionCore.Decide(
                true,
                true,
                true,
                6f,
                2f,
                10f);

            Assert.That(decision, Is.EqualTo(NpcCombatDecisionCore.Decision.ChaseTarget));
        }

        [Test]
        public void Decide_WhenInRangeAndAttackReady_ReturnsAttack()
        {
            NpcCombatDecisionCore.Decision decision = NpcCombatDecisionCore.Decide(
                true,
                true,
                true,
                1.5f,
                2f,
                10f);

            Assert.That(decision, Is.EqualTo(NpcCombatDecisionCore.Decision.Attack));
        }

        [Test]
        public void Decide_WhenInRangeButCannotAttack_ReturnsHoldPosition()
        {
            NpcCombatDecisionCore.Decision decision = NpcCombatDecisionCore.Decide(
                true,
                true,
                false,
                1.5f,
                2f,
                10f);

            Assert.That(decision, Is.EqualTo(NpcCombatDecisionCore.Decision.HoldPosition));
        }

        [Test]
        public void Decide_WhenTargetLost_ReturnsClearTarget()
        {
            NpcCombatDecisionCore.Decision decision = NpcCombatDecisionCore.Decide(
                true,
                true,
                true,
                12f,
                2f,
                10f);

            Assert.That(decision, Is.EqualTo(NpcCombatDecisionCore.Decision.ClearTarget));
        }
    }
}
