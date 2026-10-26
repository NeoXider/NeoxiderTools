using System;
using System.Collections.Generic;
using Neo.StateMachine;
using Neo.StateMachine.NoCode;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Edit
{
    public class Audit20261023NeoStateMachineRuntimeTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void PriorityChangedAfterCacheWarmup_SelectsNewHighestPriority(bool cacheTransitions)
        {
            StateMachine<IState> machine = new(true, cacheTransitions);
            StateTransition first = CreateTransition(typeof(IdleState), typeof(RunState), 10);
            StateTransition promoted = CreateTransition(typeof(IdleState), typeof(AttackState), 0);
            machine.RegisterTransition(first);
            machine.RegisterTransition(promoted);
            machine.ChangeState<IdleState>();
            Assert.That(machine.GetAvailableTransitions(typeof(IdleState))[0], Is.SameAs(first));

            promoted.Priority = 20;
            machine.EvaluateTransitions();

            Assert.That(machine.CurrentState, Is.TypeOf<AttackState>());
        }

        [TestCase(true)]
        [TestCase(false)]
        public void UnregisterAfterSourceEdit_RemovesOriginalRegistration(bool cacheTransitions)
        {
            StateMachine<IState> machine = new(true, cacheTransitions);
            StateTransition transition = CreateTransition(typeof(IdleState), typeof(RunState));
            machine.RegisterTransition(transition);
            machine.ChangeState<IdleState>();

            transition.FromStateType = typeof(AttackState);
            machine.UnregisterTransition(transition);
            transition.FromStateType = typeof(IdleState);
            machine.EvaluateTransitions();

            Assert.That(machine.CurrentState, Is.TypeOf<IdleState>());
            Assert.That(machine.CanTransitionTo<RunState>(), Is.False);
        }

        [TestCase(typeof(object))]
        [TestCase(typeof(AbstractState))]
        [TestCase(typeof(NeedsArgumentState))]
        public void UnusableTarget_DoesNotBlockLowerPriorityTransition(Type unusableTarget)
        {
            StateMachine<IState> machine = new();
            machine.RegisterTransition(CreateTransition(typeof(IdleState), unusableTarget, 10));
            machine.RegisterTransition(CreateTransition(typeof(IdleState), typeof(RunState), 0));
            machine.ChangeState<IdleState>();

            machine.EvaluateTransitions();

            Assert.That(machine.CurrentState, Is.TypeOf<RunState>());
        }

        [TestCase(true)]
        [TestCase(false)]
        public void FailedTargetConstructor_PreservesExplicitChoiceOrAllowsOrdinaryFallback(bool chooseState)
        {
            StateMachine<IState> machine = new();
            List<string> transcript = new();
            ConstructorLifecycleState source = new() { Name = "A", Transcript = transcript };
            ConstructorLifecycleState chosen = new() { Name = "C", Transcript = transcript };
            ConstructorFallbackState fallback = machine.GetOrCreateState<ConstructorFallbackState>();
            fallback.Name = "D";
            fallback.Transcript = transcript;
            StateTransition failed = CreateTransition(typeof(ConstructorLifecycleState),
                typeof(FailedConstructorState), 10);
            StateTransition lowerPriority = CreateTransition(typeof(ConstructorLifecycleState),
                typeof(ConstructorFallbackState), 0);
            List<StateTransition> evaluated = new();
            Func<IState, string> label = state => state == null ? "-" :
                state is ConstructorLifecycleState observed ? observed.Name : state.GetType().Name;
            machine.OnStateExited.AddListener(state => transcript.Add("exited " + label(state)));
            machine.OnStateEntered.AddListener(state => transcript.Add("entered " + label(state)));
            machine.OnStateChanged.AddListener((previous, current) =>
                transcript.Add("changed " + label(previous) + " " + label(current)));
            machine.OnTransitionEvaluated.AddListener((transition, passed) => evaluated.Add(transition));
            machine.RegisterTransition(failed);
            machine.RegisterTransition(lowerPriority);

            try
            {
                FailedConstructorState.OneShotConstructorHook = chooseState ? () => machine.ChangeState(chosen) : null;
                machine.ChangeState(source);
                Assert.That(transcript, Is.EqualTo(new[] { "exited -", "enter A", "entered A", "changed - A" }));
                transcript.Clear();

                // Constructor failure is deliberately logged only when the existing global debug gate is enabled.
                ILogger logger = Debug.unityLogger;
                bool previousLogEnabled = logger.logEnabled;
                try
                {
                    logger.logEnabled = false;
                    machine.EvaluateTransitions();
                }
                finally
                {
                    logger.logEnabled = previousLogEnabled;
                }

                ConstructorLifecycleState expected = chooseState ? chosen : fallback;
                Assert.That(machine.CurrentState, Is.SameAs(expected));
                Assert.That(machine.PreviousState, Is.SameAs(source));
                Assert.That(evaluated, Is.EqualTo(chooseState ? new[] { failed } : new[] { failed, lowerPriority }));
                Assert.That(transcript, Is.EqualTo(new[]
                {
                    "exit A", "exited A", "enter " + expected.Name, "entered " + expected.Name,
                    "changed A " + expected.Name
                }));
                Assert.That(source.EnterCount, Is.EqualTo(1));
                Assert.That(source.ExitCount, Is.EqualTo(1));
                Assert.That(chosen.EnterCount, Is.EqualTo(chooseState ? 1 : 0));
                Assert.That(fallback.EnterCount, Is.EqualTo(chooseState ? 0 : 1));
                Assert.That(expected.ExitCount, Is.Zero);

                transcript.Clear();
                machine.ChangeState(source);
                Assert.That(machine.CurrentState, Is.SameAs(source));
                Assert.That(machine.PreviousState, Is.SameAs(expected));
                Assert.That(transcript, Is.EqualTo(new[]
                {
                    "exit " + expected.Name, "exited " + expected.Name, "enter A", "entered A",
                    "changed " + expected.Name + " A"
                }));
                Assert.That(expected.ExitCount, Is.EqualTo(1));
                Assert.That(source.EnterCount, Is.EqualTo(2));
                transcript.Clear();
                machine.Stop();
                Assert.That(machine.CurrentState, Is.Null);
                Assert.That(machine.PreviousState, Is.SameAs(source));
                Assert.That(transcript, Is.EqualTo(new[] { "exit A", "exited A", "changed A -" }));
                Assert.That(source.ExitCount, Is.EqualTo(2));
            }
            finally
            {
                FailedConstructorState.OneShotConstructorHook = null;
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DerivedStateData_CanUseItsRegisteredTransition(bool cacheTransitions)
        {
            Audit20261023NeoStateMachineDerivedState source =
                ScriptableObject.CreateInstance<Audit20261023NeoStateMachineDerivedState>();
            StateData destination = ScriptableObject.CreateInstance<StateData>();
            try
            {
                StateMachine<IState> machine = new(true, cacheTransitions);
                machine.RegisterTransition(new StateTransition
                {
                    FromStateData = source,
                    ToStateData = destination
                });
                machine.ChangeState(source);

                machine.EvaluateTransitions();

                Assert.That(machine.CurrentState, Is.SameAs(destination));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
                UnityEngine.Object.DestroyImmediate(destination);
            }
        }

        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void EvaluationCallback_RespectsExplicitStateChoice(bool fromPredicate, bool stop)
        {
            StateMachine<IState> machine = new();
            IdleState initial = new();
            AttackState chosen = new();
            StateTransition transition = CreateTransition(typeof(IdleState), typeof(RunState));
            Action chooseState = () =>
            {
                if (stop)
                {
                    machine.Stop();
                }
                else
                {
                    machine.ChangeState(chosen);
                }
            };

            if (fromPredicate)
            {
                CustomPredicate predicate = new();
                predicate.SetEvaluator(() =>
                {
                    chooseState();
                    return true;
                });
                transition.AddPredicate(predicate);
            }
            else
            {
                machine.OnTransitionEvaluated.AddListener((evaluated, result) => chooseState());
            }

            machine.RegisterTransition(transition);
            machine.ChangeState(initial);
            machine.EvaluateTransitions();

            Assert.That(machine.CurrentState, Is.SameAs(stop ? null : chosen));
            Assert.That(machine.PreviousState, Is.SameAs(initial));
        }

        [TestCase("missing-initial")]
        [TestCase("foreign-source")]
        [TestCase("foreign-target")]
        [TestCase("missing-source")]
        [TestCase("missing-target")]
        public void Validate_RejectsInvalidNoCodeConfiguration(string defect)
        {
            StateMachineData data = ScriptableObject.CreateInstance<StateMachineData>();
            StateData initial = ScriptableObject.CreateInstance<StateData>();
            StateData destination = ScriptableObject.CreateInstance<StateData>();
            StateData foreign = ScriptableObject.CreateInstance<StateData>();
            try
            {
                data.States = new[] { initial, destination };
                data.InitialState = initial;
                StateTransition transition = new() { FromStateData = initial, ToStateData = destination };
                data.Transitions.Add(transition);
                Assert.That(data.Validate(true), Is.True, "The initial configuration must be valid.");

                switch (defect)
                {
                    case "missing-initial":
                        data.InitialState = null;
                        break;
                    case "foreign-source":
                        transition.FromStateData = foreign;
                        break;
                    case "foreign-target":
                        transition.ToStateData = foreign;
                        break;
                    case "missing-source":
                        transition.FromStateData = null;
                        break;
                    case "missing-target":
                        transition.ToStateData = null;
                        break;
                }

                Assert.That(data.Validate(true), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(data);
                UnityEngine.Object.DestroyImmediate(initial);
                UnityEngine.Object.DestroyImmediate(destination);
                UnityEngine.Object.DestroyImmediate(foreign);
            }
        }

        [Test]
        public void NullStates_HasEmptyLookupAndRemainsInvalid()
        {
            StateMachineData data = ScriptableObject.CreateInstance<StateMachineData>();
            try
            {
                data.States = null;
                data.InitialStateName = "Missing";

                Assert.That(data.States, Is.Empty);
                Assert.That(data.GetStateByName("Missing"), Is.Null);
                Assert.That(data.InitialState, Is.Null);
                Assert.That(data.Validate(true), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(data);
            }
        }

        private static StateTransition CreateTransition(Type source, Type target, int priority = 0)
        {
            return new StateTransition { FromStateType = source, ToStateType = target, Priority = priority };
        }

        public class IdleState : IState
        {
            public void OnEnter() { }
            public void OnExit() { }
            public void OnUpdate() { }
            public void OnFixedUpdate() { }
            public void OnLateUpdate() { }
        }

        public sealed class RunState : IdleState { }
        public sealed class AttackState : IdleState { }
        public abstract class AbstractState : IdleState { }

        public sealed class NeedsArgumentState : IdleState
        {
            public NeedsArgumentState(int argument) { }
        }

        public class ConstructorLifecycleState : IState
        {
            public string Name { get; set; }
            public List<string> Transcript { get; set; }
            public int EnterCount { get; private set; }
            public int ExitCount { get; private set; }

            public void OnEnter() { EnterCount++; Transcript?.Add("enter " + Name); }
            public void OnExit() { ExitCount++; Transcript?.Add("exit " + Name); }
            public void OnUpdate() { }
            public void OnFixedUpdate() { }
            public void OnLateUpdate() { }
        }

        public sealed class ConstructorFallbackState : ConstructorLifecycleState { }

        public sealed class FailedConstructorState : IdleState
        {
            public static Action OneShotConstructorHook { get; set; }

            public FailedConstructorState()
            {
                Action callback = OneShotConstructorHook;
                OneShotConstructorHook = null;
                callback?.Invoke();
                throw new InvalidOperationException("Expected target constructor fault");
            }
        }
    }
}
