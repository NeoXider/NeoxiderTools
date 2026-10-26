using Neo.StateMachine;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Covers machine-owned duration anchors, explicit retiming and reentry. Shared predicates retain
    ///     their standalone setter, while active machine evaluation uses that machine's current-state anchor.
    /// </summary>
    [TestFixture]
    public class StateDurationPredicateTests
    {
        private class IdleState : IState
        {
            public void OnEnter() { }
            public void OnExit() { }
            public void OnUpdate() { }
            public void OnFixedUpdate() { }
            public void OnLateUpdate() { }
        }

        private class RunState : IState
        {
            public void OnEnter() { }
            public void OnExit() { }
            public void OnUpdate() { }
            public void OnFixedUpdate() { }
            public void OnLateUpdate() { }
        }

        private static StateTransition CreateTransition(StateDurationPredicate predicate)
        {
            StateTransition transition = new()
            {
                FromStateType = typeof(IdleState),
                ToStateType = typeof(RunState)
            };
            transition.AddPredicate(predicate);
            return transition;
        }

        [Test]
        public void EnteringAState_AnchorsTheDurationPredicateToNow()
        {
            StateDurationPredicate predicate = new() { RequiredDuration = 5f };
            StateMachine<IState> machine = new();
            machine.RegisterTransition(CreateTransition(predicate));

            machine.ChangeState(new IdleState());

            Assert.IsFalse(machine.CanTransitionTo<RunState>(),
                "a 5 second wait must not be satisfied the instant the state is entered");
        }

        [Test]
        public void ZeroDuration_IsSatisfiedImmediately()
        {
            StateDurationPredicate predicate = new() { RequiredDuration = 0f };
            StateMachine<IState> machine = new();
            machine.RegisterTransition(CreateTransition(predicate));

            machine.ChangeState(new IdleState());

            Assert.IsTrue(machine.CanTransitionTo<RunState>(),
                "a zero second wait is satisfied as soon as the state is entered");
        }

        [Test]
        public void SetEnterTimeInThePast_SatisfiesTheThreshold()
        {
            StateDurationPredicate predicate = new() { RequiredDuration = 2f };
            StateMachine<IState> machine = new();
            machine.RegisterTransition(CreateTransition(predicate));
            machine.ChangeState(new IdleState());

            Assert.That(machine.SetCurrentStateEnterTime(Time.time - 10f), Is.True);

            Assert.IsTrue(machine.CanTransitionTo<RunState>(),
                "ten seconds of elapsed time must satisfy a two second threshold");
        }

        [Test]
        public void ReEnteringAState_RestartsTheClock()
        {
            StateDurationPredicate predicate = new() { RequiredDuration = 3f };
            StateMachine<IState> machine = new();
            machine.RegisterTransition(CreateTransition(predicate));

            machine.ChangeState(new IdleState());
            Assert.That(machine.SetCurrentStateEnterTime(Time.time - 60f), Is.True);
            Assert.IsTrue(machine.CanTransitionTo<RunState>(), "precondition: the wait is satisfied");

            machine.ChangeState(new RunState());
            machine.ChangeState(new IdleState());

            Assert.IsFalse(machine.CanTransitionTo<RunState>(),
                "coming back into the state must restart the wait");
        }

        [Test]
        public void CompositePredicate_ForwardsTheAnchorToItsChildren()
        {
            StateDurationPredicate duration = new() { RequiredDuration = 5f };
            AndPredicate composite = new();
            composite.AddPredicate(duration);
            composite.AddPredicate(new BoolPredicate { Value = true });

            StateTransition transition = new()
            {
                FromStateType = typeof(IdleState),
                ToStateType = typeof(RunState)
            };
            transition.AddPredicate(composite);

            StateMachine<IState> machine = new();
            machine.RegisterTransition(transition);
            machine.ChangeState(new IdleState());

            Assert.IsFalse(machine.CanTransitionTo<RunState>(),
                "a duration nested in an AndPredicate must be anchored too");
        }
    }
}
