using System;
using Neo.StateMachine;
using Neo.StateMachine.NoCode;
using NUnit.Framework;
using UnityEngine;
using IdleState = Neo.Editor.Tests.Edit.Audit20261023NeoStateMachineRuntimeTests.IdleState;
using RunState = Neo.Editor.Tests.Edit.Audit20261023NeoStateMachineRuntimeTests.RunState;

namespace Neo.Editor.Tests.Edit
{
    public class Audit20261023NeoStateMachineDurationContextTests
    {
        [TestCase("direct", false)]
        [TestCase("and", false)]
        [TestCase("or", false)]
        [TestCase("not", false)]
        [TestCase("direct", true)]
        [TestCase("and", true)]
        [TestCase("or", true)]
        [TestCase("not", true)]
        public void SharedData_DefaultTimingIsIndependentAcrossMachines(string composition, bool inverted)
        {
            StateMachineData data = ScriptableObject.CreateInstance<StateMachineData>();
            StateData source = ScriptableObject.CreateInstance<StateData>();
            StateData target = ScriptableObject.CreateInstance<StateData>();
            try
            {
                float now = 0f;
                StateMachine<IState> first = new(true, true, () => now);
                StateMachine<IState> second = new(true, true, () => now);
                StateDurationPredicate duration = new()
                {
                    RequiredDuration = 5f,
                    Comparison = inverted ? ComparisonType.LessThan : ComparisonType.GreaterThanOrEqual,
                    IsInverted = inverted
                };
                StateTransition transition = new() { FromStateData = source, ToStateData = target };
                transition.AddPredicate(Compose(duration, composition));
                data.States = new[] { source, target };
                data.InitialState = source;
                data.Transitions.Add(transition);
                data.LoadIntoStateMachine(first);
                data.LoadIntoStateMachine(second);
                first.ChangeState(source);
                now = 4f;
                second.ChangeState(source);
                now = 5f;

                first.EvaluateTransitions();
                second.EvaluateTransitions();

                Assert.That(first.CurrentState, Is.SameAs(target), "Second entry must not reset the first machine.");
                Assert.That(second.CurrentState, Is.SameAs(source));
                now = 9f;
                second.EvaluateTransitions();
                Assert.That(second.CurrentState, Is.SameAs(target));
                Assert.That(data.Transitions[0], Is.SameAs(transition), "No graph cloning is required.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(data);
                UnityEngine.Object.DestroyImmediate(source);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void Reentry_RestartsOnlyTheReenteredMachineClock()
        {
            float now = 0f;
            StateMachine<IState> first = new(true, true, () => now);
            StateMachine<IState> second = new(true, true, () => now);
            StateDurationPredicate duration = new() { RequiredDuration = 5f };
            StateTransition transition = CreateTransition(duration);
            first.RegisterTransition(transition);
            second.RegisterTransition(transition);
            first.ChangeState<IdleState>();
            now = 4f;
            second.ChangeState<IdleState>();
            now = 5f;
            Assert.That(first.CanTransitionTo<RunState>(), Is.True);
            Assert.That(second.CanTransitionTo<RunState>(), Is.False);

            first.ChangeState<RunState>();
            first.ChangeState<IdleState>();
            Assert.That(first.CanTransitionTo<RunState>(), Is.False);
            now = 9f;
            Assert.That(second.CanTransitionTo<RunState>(), Is.True);
            Assert.That(first.CanTransitionTo<RunState>(), Is.False);
            now = 10f;
            Assert.That(first.TryChangeState<RunState>(), Is.True);
            Assert.That(first.CurrentState, Is.TypeOf<RunState>());
        }

        [Test]
        public void StandaloneSetterAndAddressedMachineAnchor_HaveSeparateContracts()
        {
            float now = 100f;
            StateMachine<IState> first = new(true, true, () => now);
            StateMachine<IState> second = new(true, true, () => now);
            StateDurationPredicate duration = new() { RequiredDuration = 5f };
            duration.SetEnterTime(Time.time - 100f);
            StateTransition transition = CreateTransition(duration);
            first.RegisterTransition(transition);
            second.RegisterTransition(transition);
            Assert.That(first.SetCurrentStateEnterTime(0f), Is.False);
            first.ChangeState<IdleState>();
            second.ChangeState<IdleState>();

            Assert.That(duration.Evaluate(), Is.True, "Machine entry must preserve the standalone anchor.");
            Assert.That(duration.Evaluate(first.CurrentState), Is.True);
            Assert.That(first.CanTransitionTo<RunState>(), Is.False);
            duration.SetEnterTime(Time.time - 200f);
            Assert.That(first.CanTransitionTo<RunState>(), Is.False, "Standalone edits cannot reset machine timing.");
            Assert.That(first.SetCurrentStateEnterTime(95f), Is.True);
            Assert.That(first.CanTransitionTo<RunState>(), Is.True);
            Assert.That(second.CanTransitionTo<RunState>(), Is.False);
            duration.OnOwnerStateEntered(Time.time + 1000f);
            Assert.That(duration.Evaluate(), Is.False, "Standalone entry hooks still anchor standalone evaluation.");
            Assert.That(first.CanTransitionTo<RunState>(), Is.True);
            first.Stop();
            Assert.That(first.SetCurrentStateEnterTime(0f), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NestedMachineEvaluation_RestoresOuterTimingEvenAfterInnerException(bool innerThrows)
        {
            float outerNow = 0f;
            float innerNow = 100f;
            StateMachine<IState> outer = new(true, true, () => outerNow);
            StateMachine<IState> inner = new(true, true, () => innerNow);
            StateDurationPredicate duration = new() { RequiredDuration = 5f };
            duration.SetEnterTime(Time.time + 1000f);
            CustomPredicate innerCallback = new();
            innerCallback.SetEvaluator(() =>
            {
                if (innerThrows) throw new InvalidOperationException("Expected inner fault");
                return true;
            });
            StateTransition innerTransition = CreateTransition(innerCallback);
            innerTransition.AddPredicate(duration);
            inner.RegisterTransition(innerTransition);
            CustomPredicate outerCallback = new();
            outerCallback.SetEvaluator(() =>
            {
                if (innerThrows)
                {
                    Assert.Throws<InvalidOperationException>(() => inner.CanTransitionTo<RunState>());
                }
                else
                {
                    Assert.That(inner.CanTransitionTo<RunState>(), Is.False);
                }

                Assert.That(duration.Evaluate(outer.CurrentState), Is.True, "Outer context must be restored.");
                return true;
            });
            StateTransition outerTransition = CreateTransition(outerCallback);
            outerTransition.AddPredicate(duration);
            outer.RegisterTransition(outerTransition);
            outer.ChangeState<IdleState>();
            inner.ChangeState<IdleState>();
            outerNow = 5f;
            innerNow = 101f;

            Assert.That(outer.CanTransitionTo<RunState>(), Is.True);
            Assert.That(duration.Evaluate(), Is.False, "No machine timing may leak to standalone calls.");
        }

        [Test]
        public void EvaluationException_RestoresStandaloneTimingAndAllowsNextEvaluation()
        {
            float now = 0f;
            StateMachine<IState> machine = new(true, true, () => now);
            StateDurationPredicate duration = new() { RequiredDuration = 5f };
            duration.SetEnterTime(Time.time + 1000f);
            CustomPredicate callback = new();
            callback.SetEvaluator(() => throw new InvalidOperationException("Expected evaluation fault"));
            StateTransition transition = CreateTransition(callback);
            transition.AddPredicate(duration);
            machine.RegisterTransition(transition);
            machine.ChangeState<IdleState>();
            now = 5f;

            Assert.Throws<InvalidOperationException>(() => machine.EvaluateTransitions());

            Assert.That(duration.Evaluate(), Is.False);
            callback.SetEvaluator(() => true);
            Assert.That(machine.CanTransitionTo<RunState>(), Is.True);
        }

        [Test]
        public void EntryHookException_RestoresTimingAndPreservesCustomHookDispatch()
        {
            float now = 0f;
            StateMachine<IState> machine = new(true, true, () => now);
            StateDurationPredicate duration = new() { RequiredDuration = 5f };
            duration.SetEnterTime(Time.time - 100f);
            EntryHookPredicate callback = new() { ThrowOnEntry = true };
            StateTransition transition = CreateTransition(callback);
            transition.AddPredicate(duration);
            machine.RegisterTransition(transition);

            Assert.Throws<InvalidOperationException>(() => machine.ChangeState<IdleState>());

            Assert.That(duration.Evaluate(), Is.True);
            callback.ThrowOnEntry = false;
            machine.ChangeState<RunState>();
            machine.ChangeState<IdleState>();
            Assert.That(callback.EntryCount, Is.EqualTo(2));
            Assert.That(callback.LastEntryTime, Is.EqualTo(0f));
            Assert.That(duration.Evaluate(), Is.True);
            Assert.That(machine.SetCurrentStateEnterTime(-5f), Is.True);
            Assert.That(callback.EntryCount, Is.EqualTo(2), "Addressed timing edit must not replay custom hooks.");
            Assert.That(machine.CanTransitionTo<RunState>(), Is.True);
        }

        [Test]
        public void CustomVirtualEvaluate_PreservesStateDispatchAndSingleInversion()
        {
            float now = 0f;
            StateMachine<IState> machine = new(true, true, () => now);
            VirtualDispatchPredicate custom = new() { IsInverted = true };
            AndPredicate composite = new();
            composite.AddPredicate(custom);
            composite.AddPredicate(new StateDurationPredicate { RequiredDuration = 5f });
            machine.RegisterTransition(CreateTransition(composite));
            machine.ChangeState<IdleState>();
            now = 5f;

            Assert.That(machine.CanTransitionTo<RunState>(), Is.True);
            Assert.That(custom.StateCalls, Is.EqualTo(1));
            Assert.That(custom.RawCalls, Is.EqualTo(1));
            Assert.That(custom.StateArgument, Is.SameAs(machine.CurrentState));
        }

        private static StatePredicate Compose(StatePredicate child, string composition)
        {
            if (composition == "and")
            {
                AndPredicate conjunction = new();
                conjunction.AddPredicate(new BoolPredicate { Value = true });
                conjunction.AddPredicate(child);
                return conjunction;
            }

            if (composition == "or")
            {
                OrPredicate disjunction = new();
                disjunction.AddPredicate(new BoolPredicate { Value = false });
                disjunction.AddPredicate(child);
                return disjunction;
            }

            return composition == "not" ? new NotPredicate { Predicate = new NotPredicate { Predicate = child } } : child;
        }

        private static StateTransition CreateTransition(StatePredicate predicate)
        {
            StateTransition transition = new() { FromStateType = typeof(IdleState), ToStateType = typeof(RunState) };
            transition.AddPredicate(predicate);
            return transition;
        }

        private sealed class EntryHookPredicate : StatePredicate
        {
            public bool ThrowOnEntry { get; set; }
            public int EntryCount { get; private set; }
            public float LastEntryTime { get; private set; }

            public override void OnOwnerStateEntered(float enterTime)
            {
                EntryCount++;
                LastEntryTime = enterTime;
                if (ThrowOnEntry) throw new InvalidOperationException("Expected entry hook fault");
            }

            protected override bool EvaluateInternal(IState currentState) => true;
        }

        private sealed class VirtualDispatchPredicate : StatePredicate
        {
            public int StateCalls { get; private set; }
            public int RawCalls { get; private set; }
            public IState StateArgument { get; private set; }

            public override bool Evaluate(IState currentState)
            {
                StateCalls++;
                StateArgument = currentState;
                return base.Evaluate(currentState);
            }

            public override bool Evaluate() => throw new InvalidOperationException("Wrong predicate overload");

            protected override bool EvaluateInternal(IState currentState) { RawCalls++; return false; }
        }
    }
}
