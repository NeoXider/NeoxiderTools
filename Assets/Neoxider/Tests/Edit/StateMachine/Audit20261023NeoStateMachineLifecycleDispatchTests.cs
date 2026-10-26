using System;
using System.Collections.Generic;
using Neo.StateMachine;
using NUnit.Framework;

namespace Neo.Editor.Tests.Edit
{
    public class Audit20261023NeoStateMachineLifecycleDispatchTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void ExitRequest_DrainsAfterCurrentChangeWithoutDuplicateExit(bool returnToSource)
        {
            StateMachine<IState> machine = new();
            List<string> events = new();
            ProbeState source = new("A", events);
            ProbeState intermediate = new("B", events);
            ProbeState chosen = returnToSource ? source : new ProbeState("C", events);
            RecordEvents(machine, events);
            machine.ChangeState(source);
            events.Clear();
            bool requested = false;
            source.ExitAction = () =>
            {
                Assert.That(machine.CurrentState, Is.SameAs(source));
                if (!requested)
                {
                    requested = true;
                    machine.ChangeState(chosen);
                }
            };

            machine.ChangeState(intermediate);

            string destination = chosen.Name;
            Assert.That(events, Is.EqualTo(new[]
            {
                "exit A", "exited A", "enter B", "entered B", "changed A B",
                "exit B", "exited B", "enter " + destination, "entered " + destination,
                "changed B " + destination
            }));
            Assert.That(machine.CurrentState, Is.SameAs(chosen));
            Assert.That(machine.PreviousState, Is.SameAs(intermediate));
            Assert.That(source.ExitCount, Is.EqualTo(1));
            Assert.That(chosen.EnterCount, Is.EqualTo(returnToSource ? 2 : 1));
        }

        [Test]
        public void EnterRequestsRepeatedStop_PublishesEnteredThenOneTerminalStop()
        {
            StateMachine<IState> machine = new();
            List<string> events = new();
            ProbeState source = new("A", events);
            ProbeState entered = new("B", events);
            RecordEvents(machine, events);
            machine.ChangeState(source);
            events.Clear();
            entered.EnterAction = () => { machine.Stop(); machine.Stop(); };
            entered.ExitAction = () => { machine.Stop(); machine.Stop(); };

            machine.ChangeState(entered);
            machine.Stop();

            Assert.That(events, Is.EqualTo(new[]
            {
                "exit A", "exited A", "enter B", "entered B", "changed A B",
                "exit B", "exited B", "changed B -"
            }));
            Assert.That(machine.CurrentState, Is.Null);
            Assert.That(machine.PreviousState, Is.SameAs(entered));
            Assert.That(entered.ExitCount, Is.EqualTo(1));
        }

        [Test]
        public void StopExitRequestsChange_PreservesStoppedPayloadBeforeNewEntry()
        {
            StateMachine<IState> machine = new();
            List<string> events = new();
            ProbeState source = new("A", events);
            ProbeState chosen = new("B", events);
            RecordEvents(machine, events);
            machine.ChangeState(source);
            events.Clear();
            source.ExitAction = () =>
            {
                Assert.That(machine.CurrentState, Is.Null);
                machine.ChangeState(chosen);
            };

            machine.Stop();

            Assert.That(events, Is.EqualTo(new[]
            {
                "exit A", "exited A", "changed A -", "exited -", "enter B", "entered B", "changed - B"
            }));
            Assert.That(machine.CurrentState, Is.SameAs(chosen));
            Assert.That(source.ExitCount, Is.EqualTo(1));
        }

        [TestCase("evaluate", true)]
        [TestCase("evaluate", false)]
        [TestCase("can", true)]
        [TestCase("try", true)]
        public void DepartureAndSameInstanceReentry_CancelsOldEvaluation(string path, bool fromPredicate)
        {
            StateMachine<IState> machine = new();
            ProbeState source = new("A");
            ProbeState intermediate = new("B");
            StateTransition transition = new()
            {
                FromStateType = typeof(ProbeState), ToStateType = typeof(TargetState)
            };
            Action reenter = () => { machine.ChangeState(intermediate); machine.ChangeState(source); };
            if (fromPredicate)
            {
                CustomPredicate predicate = new();
                predicate.SetEvaluator(() => { reenter(); return true; });
                transition.AddPredicate(predicate);
            }
            else
            {
                machine.OnTransitionEvaluated.AddListener((evaluated, passed) => reenter());
            }

            machine.RegisterTransition(transition);
            machine.ChangeState(source);
            if (path == "evaluate")
            {
                machine.EvaluateTransitions();
            }
            else if (path == "can")
            {
                Assert.That(machine.CanTransitionTo<TargetState>(), Is.False);
            }
            else
            {
                Assert.That(machine.TryChangeState<TargetState>(), Is.False);
            }

            Assert.That(machine.CurrentState, Is.SameAs(source));
            Assert.That(source.EnterCount, Is.EqualTo(2));
            Assert.That(source.ExitCount, Is.EqualTo(1));
            Assert.That(intermediate.ExitCount, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DeferredPredicateChoice_CancelsAddressedEvaluationBeforeItCanCommit(bool tryChange)
        {
            StateMachine<IState> machine = new();
            ProbeState source = new("A");
            ProbeState chosen = new("B");
            StateTransition transition = new()
            {
                FromStateType = typeof(ProbeState), ToStateType = typeof(TargetState)
            };
            CustomPredicate predicate = new();
            predicate.SetEvaluator(() => { machine.ChangeState(chosen); return true; });
            transition.AddPredicate(predicate);
            machine.RegisterTransition(transition);
            bool accepted = true;
            source.EnterAction = () => accepted = tryChange
                ? machine.TryChangeState<TargetState>()
                : machine.CanTransitionTo<TargetState>();

            machine.ChangeState(source);

            Assert.That(accepted, Is.False);
            Assert.That(machine.CurrentState, Is.SameAs(chosen));
            Assert.That(source.ExitCount, Is.EqualTo(1));
            Assert.That(chosen.EnterCount, Is.EqualTo(1));
        }

        [Test]
        public void CyclicRequests_StopAtDocumentedLimitAndDiscardPendingWork()
        {
            StateMachine<IState> machine = new();
            ProbeState first = new("A");
            ProbeState second = new("B");
            int callbacks = 0;
            // The fixture itself is bounded, even on the old implementation; it never requests an infinite cycle.
            first.EnterAction = () => { if (++callbacks < 140) machine.ChangeState(second); };
            second.EnterAction = () => { if (++callbacks < 140) machine.ChangeState(first); };

            machine.ChangeState(first);

            Assert.That(callbacks, Is.EqualTo(128));
            Assert.That(first.EnterCount + second.EnterCount, Is.EqualTo(128));
            Assert.That(first.ExitCount + second.ExitCount, Is.EqualTo(127));
            first.EnterAction = null;
            second.EnterAction = null;
            machine.Stop();
            machine.ChangeState(first);
            Assert.That(machine.CurrentState, Is.SameAs(first));
            Assert.That(first.EnterCount + second.EnterCount, Is.EqualTo(129));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LifecycleException_ReleasesDispatcherAndDiscardsDeferredRequests(bool throwOnExit)
        {
            StateMachine<IState> machine = new();
            ProbeState source = new("A");
            ProbeState destination = new("B");
            ProbeState discarded = new("C");
            machine.ChangeState(source);
            bool requested = false;
            Action fail = () =>
            {
                if (!requested)
                {
                    requested = true;
                    machine.ChangeState(discarded);
                }

                throw new InvalidOperationException("Expected lifecycle fault");
            };
            if (throwOnExit) source.ExitAction = fail;
            else destination.EnterAction = fail;

            Assert.Throws<InvalidOperationException>(() => machine.ChangeState(destination));

            source.ExitAction = null;
            destination.EnterAction = null;
            machine.Stop();
            machine.ChangeState(destination);
            Assert.That(machine.CurrentState, Is.SameAs(destination));
            Assert.That(discarded.EnterCount, Is.Zero);
        }

        private static void RecordEvents(StateMachine<IState> machine, List<string> events)
        {
            machine.OnStateExited.AddListener(state => events.Add("exited " + Label(state)));
            machine.OnStateEntered.AddListener(state => events.Add("entered " + Label(state)));
            machine.OnStateChanged.AddListener((previous, current) =>
                events.Add("changed " + Label(previous) + " " + Label(current)));
        }

        private static string Label(IState state) => state is ProbeState probe ? probe.Name : "-";

        private sealed class ProbeState : IState
        {
            private readonly List<string> events;
            public string Name { get; }
            public Action EnterAction { get; set; }
            public Action ExitAction { get; set; }
            public int EnterCount { get; private set; }
            public int ExitCount { get; private set; }

            public ProbeState(string name, List<string> events = null)
            {
                Name = name;
                this.events = events;
            }

            public void OnEnter() { EnterCount++; events?.Add("enter " + Name); EnterAction?.Invoke(); }
            public void OnExit() { ExitCount++; events?.Add("exit " + Name); ExitAction?.Invoke(); }
            public void OnUpdate() { }
            public void OnFixedUpdate() { }
            public void OnLateUpdate() { }
        }

        public sealed class TargetState : IState
        {
            public void OnEnter() { }
            public void OnExit() { }
            public void OnUpdate() { }
            public void OnFixedUpdate() { }
            public void OnLateUpdate() { }
        }
    }
}
