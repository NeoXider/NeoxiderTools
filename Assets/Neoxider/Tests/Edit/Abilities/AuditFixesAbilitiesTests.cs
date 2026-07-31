using Neo.Abilities;
using NUnit.Framework;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     Revive must leave a receipt on the event bus. Everything observable in the domain flows through
    ///     <c>AbilitySystem.Events</c>, so a Death receipt without a matching Revive receipt leaves VFX,
    ///     ragdoll handlers and the replication layer showing a unit the simulation already considers alive.
    /// </summary>
    public sealed class AuditFixesAbilitiesTests
    {
        private AbilitySystem _system;
        private AbilityUnit _unit;
        private EventLog _log;

        [SetUp]
        public void SetUp()
        {
            _system = new AbilitySystem();
            _unit = AbilityTestSupport.CreateUnit(_system, team: 1, health: 200f);
            _log = new EventLog(_system);
        }

        [Test]
        public void Revive_AfterMarkDead_PublishesReviveReceiptForTheUnit()
        {
            _system.MarkDead(_unit, UnitId.None);
            Assert.That(_log.Count(AbilityEvents.Death), Is.EqualTo(1));

            _system.Revive(_unit, 0.5f);

            Assert.That(_unit.IsAlive, Is.True);
            Assert.That(_log.Count(AbilityEvents.Revive), Is.EqualTo(1),
                "A Death receipt must be balanced by a Revive receipt; polling IsAlive is not the contract.");
            Assert.That(_log.TryGetLast(AbilityEvents.Revive, out AbilityEventArgs args), Is.True);
            Assert.That(args.Target, Is.EqualTo(_unit.Id));
        }

        [Test]
        public void Revive_PublishesRestoredHealthAsAmount_AfterTheHealthIsWritten()
        {
            _system.MarkDead(_unit, UnitId.None);

            _system.Revive(_unit, 0.25f);

            Assert.That(_log.TryGetLast(AbilityEvents.Revive, out AbilityEventArgs args), Is.True);
            Assert.That(args.Amount, Is.EqualTo(50f).Within(0.01f));
            Assert.That(_unit.Health, Is.EqualTo(50f).Within(0.01f));
        }

        [Test]
        public void Revive_OnLivingUnit_IsANoOpAndPublishesNothing()
        {
            _system.Revive(_unit);

            Assert.That(_log.Any(AbilityEvents.Revive), Is.False,
                "Reviving a unit that never died must not emit a receipt.");
        }

        [Test]
        public void Revive_ReceiptIsRoutedToSubscribersOfTheReviveEventId()
        {
            int received = 0;
            _system.Events.Subscribe(AbilityEvents.Revive, _ => received++);

            _system.MarkDead(_unit, UnitId.None);
            _system.Revive(_unit);

            Assert.That(received, Is.EqualTo(1));
        }
    }
}
