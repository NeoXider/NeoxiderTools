using Neo.Abilities;
using NUnit.Framework;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     Boolean states aggregate any-true-wins across modifiers and permanent flags; stun/silence
    ///     gate casting.
    /// </summary>
    public sealed class AbilityStateTests
    {
        private AbilitySystem _system;
        private AbilityUnit _unit;

        [SetUp]
        public void SetUp()
        {
            _system = new AbilitySystem();
            _unit = AbilityTestSupport.CreateUnit(_system, team: 1);
        }

        [Test]
        public void PermanentState_SetsAndClears()
        {
            Assert.That(_unit.HasState(AbilityStates.Rooted), Is.False);
            _unit.SetPermanentState(AbilityStates.Rooted, true);
            Assert.That(_unit.HasState(AbilityStates.Rooted), Is.True);
            _unit.SetPermanentState(AbilityStates.Rooted, false);
            Assert.That(_unit.HasState(AbilityStates.Rooted), Is.False);
        }

        [Test]
        public void ModifierState_GrantsWhileActive()
        {
            ModifierInstance stun = _system.Modifiers.Apply(
                AbilityTestSupport.Modifier("stun", 2f).WithState(AbilityStates.Stunned),
                UnitId.None, _unit.Id).Instance;

            Assert.That(_unit.HasState(AbilityStates.Stunned), Is.True);

            _system.Modifiers.Remove(stun);
            Assert.That(_unit.HasState(AbilityStates.Stunned), Is.False);
        }

        [Test]
        public void State_AnyTrueWins_AcrossModifiersAndPermanent()
        {
            // Two modifiers grant stunned; permanent also grants it.
            _system.Modifiers.Apply(
                AbilityTestSupport.Modifier("stunA", 5f, ModifierStackPolicy.Independent).WithState(AbilityStates.Stunned),
                UnitId.None, _unit.Id);
            ModifierInstance b = _system.Modifiers.Apply(
                AbilityTestSupport.Modifier("stunB", 5f, ModifierStackPolicy.Independent).WithState(AbilityStates.Stunned),
                UnitId.None, _unit.Id).Instance;
            _unit.SetPermanentState(AbilityStates.Stunned, true);

            Assert.That(_unit.HasState(AbilityStates.Stunned), Is.True);

            // Removing one modifier still leaves it true (other modifier + permanent).
            _system.Modifiers.Remove(b);
            Assert.That(_unit.HasState(AbilityStates.Stunned), Is.True);

            // Remove all modifiers; permanent still holds it true.
            _system.Modifiers.RemoveAllFrom(_unit.Id);
            Assert.That(_unit.HasState(AbilityStates.Stunned), Is.True);

            // Clear permanent too => finally false.
            _unit.SetPermanentState(AbilityStates.Stunned, false);
            Assert.That(_unit.HasState(AbilityStates.Stunned), Is.False);
        }

        [Test]
        public void StateExpiry_ClearsState()
        {
            _system.Modifiers.Apply(
                AbilityTestSupport.Modifier("stun", 1f).WithState(AbilityStates.Stunned),
                UnitId.None, _unit.Id);
            Assert.That(_unit.HasState(AbilityStates.Stunned), Is.True);

            _system.Tick(1f);
            Assert.That(_unit.HasState(AbilityStates.Stunned), Is.False);
        }

        [Test]
        public void Stunned_BlocksCasting()
        {
            GrantSimpleAbility("blast");
            _unit.SetPermanentState(AbilityStates.Stunned, true);

            CastResult result = _system.Cast(CastRequest.NoTarget(_unit.Id, "blast"));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Failure, Is.EqualTo(CastFailureReason.Stunned));
        }

        [Test]
        public void Silenced_BlocksCasting()
        {
            GrantSimpleAbility("blast");
            _unit.SetPermanentState(AbilityStates.Silenced, true);

            CastResult result = _system.Cast(CastRequest.NoTarget(_unit.Id, "blast"));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Failure, Is.EqualTo(CastFailureReason.Silenced));
        }

        private void GrantSimpleAbility(string id)
        {
            var ability = new AbilityBlueprint { Id = id, Targeting = TargetingMode.NoTarget };
            _system.RegisterAbility(ability);
            _system.GrantAbility(_unit.Id, id);
        }
    }
}
