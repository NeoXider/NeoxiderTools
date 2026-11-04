using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Condition;
using Neo.StateMachine;
using Neo.StateMachine.NoCode;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.Edit
{
    /// <summary>
    ///     Covers the 2026-11 transition routing pass: NoCode transitions are looked up per StateData asset (a frame
    ///     evaluates and an entry notifies only the transitions leaving the active asset), sorted lists are revalidated
    ///     through <see cref="StateTransition.RoutingVersion"/> instead of a per-call priority scan, equal priorities
    ///     keep registration order, and a warm NoCode evaluation does not allocate.
    /// </summary>
    [TestFixture]
    public sealed class StateMachineRoutingTests
    {
        private const int Iterations = 200;
        private readonly List<Object> _owned = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _owned.Count - 1; i >= 0; i--)
            {
                if (_owned[i] != null)
                {
                    Object.DestroyImmediate(_owned[i]);
                }
            }

            _owned.Clear();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void NoCodeEvaluation_WalksOnlyTransitionsLeavingTheActiveAsset(bool cacheTransitions)
        {
            StateData a = NewState("A");
            StateData b = NewState("B");
            StateData c = NewState("C");
            StateMachine<IState> machine = new(true, cacheTransitions, () => 0f);
            CountingPredicate aToBGate = new();
            CountingPredicate bToCGate = new();
            CountingPredicate cToAGate = new();
            CountingPredicate anyGate = new();
            StateTransition aToB = Transition(a, b, 1, aToBGate);
            StateTransition bToC = Transition(b, c, 1, bToCGate);
            StateTransition cToA = Transition(c, a, 1, cToAGate);
            StateTransition fromAny = Transition(null, c, 0, anyGate);
            machine.RegisterTransition(aToB);
            machine.RegisterTransition(bToC);
            machine.RegisterTransition(cToA);
            machine.RegisterTransition(fromAny);
            List<StateTransition> evaluated = new();
            machine.OnTransitionEvaluated.AddListener((transition, passed) => evaluated.Add(transition));
            machine.ChangeState(a);

            for (int i = 0; i < 3; i++)
            {
                machine.EvaluateTransitions();
            }

            Assert.That(machine.CurrentState, Is.SameAs(a));
            Assert.That(evaluated, Is.EqualTo(new[] { aToB, fromAny, aToB, fromAny, aToB, fromAny }),
                "Only A's own transition and the source-less one are evaluated and reported.");
            Assert.That(aToBGate.Evaluations, Is.EqualTo(3));
            Assert.That(anyGate.Evaluations, Is.EqualTo(3));
            Assert.That(bToCGate.Evaluations + cToAGate.Evaluations, Is.Zero,
                "Transitions leaving other StateData assets must not be evaluated while A is active.");
            Assert.That(machine.GetAvailableTransitionsFrom(a), Is.EqualTo(new[] { aToB, fromAny }));
            Assert.That(machine.GetAvailableTransitionsFrom(b), Is.EqualTo(new[] { bToC, fromAny }));
            Assert.That(machine.GetAvailableTransitionsFrom(null), Is.Empty);

            aToBGate.Result = true;
            machine.EvaluateTransitions();
            Assert.That(machine.CurrentState, Is.SameAs(b));
            Assert.That(bToCGate.Evaluations, Is.Zero, "Entering B does not evaluate B's transitions in the same pass.");
        }

        [Test]
        public void LegacyTypeLookup_StillListsEveryNoCodeTransition()
        {
            StateData a = NewState("A");
            StateData b = NewState("B");
            StateMachine<IState> machine = new(true, true, () => 0f);
            StateTransition aToB = Transition(a, b, 0, null);
            StateTransition bToA = Transition(b, a, 2, null);
            machine.RegisterTransition(aToB);
            machine.RegisterTransition(bToA);

            Assert.That(machine.GetAvailableTransitions(typeof(StateData)), Is.EqualTo(new[] { bToA, aToB }),
                "The public type lookup keeps its meaning: every transition registered for the StateData type.");
        }

        [Test]
        public void NoCodeEntry_NotifiesOnlyPredicatesOfTransitionsLeavingTheEnteredAsset()
        {
            StateData a = NewState("A");
            StateData b = NewState("B");
            StateMachine<IState> machine = new(true, true, () => 0f);
            CountingPredicate aGate = new();
            CountingPredicate bGate = new();
            CountingPredicate anyGate = new();
            machine.RegisterTransition(Transition(a, b, 0, aGate));
            machine.RegisterTransition(Transition(b, a, 0, bGate));
            machine.RegisterTransition(Transition(null, b, 0, anyGate));

            machine.ChangeState(a);
            Assert.That(aGate.Entries, Is.EqualTo(1));
            Assert.That(bGate.Entries, Is.Zero, "B's transition does not leave A and must not hear A's entry.");
            Assert.That(anyGate.Entries, Is.EqualTo(1));

            machine.ChangeState(b);
            Assert.That(aGate.Entries, Is.EqualTo(1));
            Assert.That(bGate.Entries, Is.EqualTo(1));
            Assert.That(anyGate.Entries, Is.EqualTo(2));
        }

        [Test]
        public void FromStateDataEditedAfterRegistration_RoutesFromItsNewSource()
        {
            StateData a = NewState("A");
            StateData b = NewState("B");
            StateData c = NewState("C");
            StateMachine<IState> machine = new(true, true, () => 0f);
            StateTransition moved = Transition(a, b, 0, null);
            machine.RegisterTransition(moved);
            machine.ChangeState(c);
            machine.EvaluateTransitions();
            Assert.That(machine.CurrentState, Is.SameAs(c));
            Assert.That(machine.GetAvailableTransitionsFrom(a), Is.EqualTo(new[] { moved }));

            moved.FromStateData = c;

            Assert.That(machine.GetAvailableTransitionsFrom(a), Is.Empty);
            machine.EvaluateTransitions();
            Assert.That(machine.CurrentState, Is.SameAs(b), "The warm list of C must pick up the edited source.");
        }

        [Test]
        public void SerializedPriorityEdit_ReordersTheWarmLookupThroughTheDeserializationCallback()
        {
            StateData a = NewState("A");
            StateData b = NewState("B");
            StateData c = NewState("C");
            StateMachine<IState> machine = new(true, true, () => 0f);
            StateTransition toB = Transition(a, b, 1, null);
            StateTransition toC = Transition(a, c, 0, null);
            machine.RegisterTransition(toB);
            machine.RegisterTransition(toC);
            Assert.That(machine.GetAvailableTransitionsFrom(a), Is.EqualTo(new[] { toB, toC }));

            FieldInfo priorityField = typeof(StateTransition).GetField("priority",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(priorityField, Is.Not.Null, "StateTransition.priority not found.");
            priorityField.SetValue(toC, 5);
            ((ISerializationCallbackReceiver)toC).OnAfterDeserialize();
            Assert.That(machine.GetAvailableTransitionsFrom(a), Is.EqualTo(new[] { toC, toB }),
                "Unity deserialization (Inspector edit, Undo) bumps the routing version.");

            priorityField.SetValue(toB, 9);
            StateTransition.MarkRoutingChanged();
            machine.ChangeState(a);
            machine.EvaluateTransitions();
            Assert.That(machine.CurrentState, Is.SameAs(b), "MarkRoutingChanged covers other raw writes.");
        }

        [Test]
        public void EqualPriorities_KeepRegistrationOrder()
        {
            StateMachine<IState> machine = new(true, true, () => 0f);
            StateTransition first = new() { FromStateType = typeof(SourceState), ToStateType = typeof(TargetState), Priority = 1 };
            StateTransition second = new() { FromStateType = typeof(SourceState), ToStateType = typeof(TargetState), Priority = 1 };
            StateTransition highest = new() { FromStateType = typeof(SourceState), ToStateType = typeof(TargetState), Priority = 2 };
            StateTransition global = new() { ToStateType = typeof(TargetState), Priority = 1 };
            machine.RegisterTransition(first);
            machine.RegisterTransition(second);
            machine.RegisterTransition(highest);
            machine.RegisterTransition(global);

            Assert.That(machine.GetAvailableTransitions(typeof(SourceState)),
                Is.EqualTo(new[] { highest, first, second, global }));

            first.Priority = 0;
            first.Priority = 1;
            Assert.That(machine.GetAvailableTransitions(typeof(SourceState)),
                Is.EqualTo(new[] { highest, first, second, global }),
                "A priority round trip must not reorder equal priorities.");
        }

        [Test]
        public void PriorityEditedByAListenerMidEvaluation_FinishesThePassOnItsSnapshot()
        {
            StateData a = NewState("A");
            StateData b = NewState("B");
            StateData c = NewState("C");
            StateMachine<IState> machine = new(true, true, () => 0f);
            StateTransition first = Transition(a, c, 2, new CountingPredicate());
            StateTransition second = Transition(a, c, 1, new CountingPredicate());
            StateTransition third = Transition(a, b, 0, new CountingPredicate { Result = true });
            machine.RegisterTransition(first);
            machine.RegisterTransition(second);
            machine.RegisterTransition(third);
            List<StateTransition> evaluated = new();
            machine.OnTransitionEvaluated.AddListener((transition, passed) =>
            {
                evaluated.Add(transition);
                if (ReferenceEquals(transition, first))
                {
                    third.Priority = 5;
                }
            });
            machine.ChangeState(a);

            machine.EvaluateTransitions();

            Assert.That(evaluated, Is.EqualTo(new[] { first, second, third }),
                "The running pass keeps the list it started with.");
            Assert.That(machine.CurrentState, Is.SameAs(b));
            Assert.That(machine.GetAvailableTransitionsFrom(a), Is.EqualTo(new[] { third, first, second }),
                "The next lookup sees the new order.");
        }

        [Test]
        public void UnregisteredNoCodeTransition_LeavesTheWarmAssetList()
        {
            StateData a = NewState("A");
            StateData b = NewState("B");
            StateMachine<IState> machine = new(true, true, () => 0f);
            StateTransition aToB = Transition(a, b, 0, new CountingPredicate());
            machine.RegisterTransition(aToB);
            machine.ChangeState(a);
            machine.EvaluateTransitions();
            Assert.That(machine.GetAvailableTransitionsFrom(a), Is.EqualTo(new[] { aToB }));

            machine.UnregisterTransition(aToB);
            ((CountingPredicate)aToB.Predicates[0]).Result = true;
            machine.EvaluateTransitions();

            Assert.That(machine.GetAvailableTransitionsFrom(a), Is.Empty);
            Assert.That(machine.CurrentState, Is.SameAs(a));
        }

        [Test]
        public void WarmNoCodeEvaluation_WithManyStates_DoesNotAllocate()
        {
            StateMachine<IState> machine = CreateRing(32, 2, out StateData first, out CountingPredicate[] gates);
            machine.ChangeState(first);
            machine.EvaluateTransitions();
            machine.EvaluateTransitions();
            int before = TotalEvaluations(gates);
            Action evaluate = () =>
            {
                for (int i = 0; i < Iterations; i++)
                {
                    machine.EvaluateTransitions();
                }
            };

            GcAlloc.AssertNone(evaluate, "A warm NoCode evaluation must not allocate.");

            Assert.That(machine.CurrentState, Is.SameAs(first));
            Assert.That(TotalEvaluations(gates) - before, Is.EqualTo(Iterations * 2),
                "Each pass evaluates the two transitions leaving the active asset, not all 64.");
        }

        [Test]
        public void RoutingEditOnAnUnrelatedTransition_DoesNotReallocateWarmLists()
        {
            StateMachine<IState> machine = CreateRing(8, 2, out StateData first, out _);
            machine.ChangeState(first);
            machine.EvaluateTransitions();
            StateTransition unrelated = new() { FromStateType = typeof(SourceState), ToStateType = typeof(TargetState) };
            IReadOnlyList<StateTransition> warm = machine.GetAvailableTransitionsFrom(first);
            int nextPriority = 1;
            Action evaluateAfterForeignEdit = () => machine.EvaluateTransitions();
            unrelated.Priority = nextPriority++;
            evaluateAfterForeignEdit();

            using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
            {
                for (int round = 0; round < 8; round++)
                {
                    unrelated.Priority = nextPriority++;
                    long calls = counter.Count(evaluateAfterForeignEdit);
                    Assert.That(calls, Is.Zero,
                        "A routing edit in another machine revalidates this list without replacing it.");
                }
            }

            Assert.That(machine.GetAvailableTransitionsFrom(first), Is.SameAs(warm));
        }

        [Test]
        public void GuardReplica_ContextTogglePredicateAndDurationRouteBothWays()
        {
            GameObject toggleObject = NewObject("Noise Box");
            Toggle noise = toggleObject.AddComponent<Toggle>();
            noise.isOn = false;
            StateData sleep = NewState("Sleep");
            StateData alert = NewState("Alert");
            StateMachineData data = NewAsset<StateMachineData>();
            data.States = new[] { sleep, alert };
            data.InitialState = sleep;
            StateTransition toAlert = Transition(sleep, alert, 0, NoisePredicate(true));
            StateTransition toSleep = Transition(alert, sleep, 0, NoisePredicate(false));
            toSleep.AddPredicate(new StateDurationPredicate
            {
                RequiredDuration = 1.5f,
                Comparison = ComparisonType.GreaterThanOrEqual
            });
            data.Transitions.Add(toAlert);
            data.Transitions.Add(toSleep);

            GameObject guardObject = NewObject("Guard");
            StateMachineBehaviourBase guard = guardObject.AddComponent<StateMachineBehaviourBase>();
            SerializedObject serializedGuard = new(guard);
            SerializedProperty overrides = serializedGuard.FindProperty("contextOverrides");
            overrides.arraySize = 1;
            overrides.GetArrayElementAtIndex(0).objectReferenceValue = toggleObject;
            serializedGuard.ApplyModifiedPropertiesWithoutUndo();
            guard.StateMachineData = data;
            guard.LoadFromStateMachineData();
            Assert.That(guard.CurrentStateName, Is.EqualTo("Sleep"));

            guard.EvaluateTransitionsNow();
            Assert.That(guard.CurrentStateName, Is.EqualTo("Sleep"), "Noise off keeps the guard asleep.");

            guard.ChangeState(alert);
            guard.EvaluateTransitionsNow();
            Assert.That(guard.CurrentStateName, Is.EqualTo("Alert"), "Less than 1.5 s in Alert.");

            Assert.That(guard.StateMachine.SetCurrentStateEnterTime(Time.time - 2f), Is.True);
            guard.EvaluateTransitionsNow();
            Assert.That(guard.CurrentStateName, Is.EqualTo("Sleep"),
                "Alert -> Sleep fires after 1.5 s in Alert while the toggle in slot Override1 is off.");

            noise.isOn = true;
            guard.EvaluateTransitionsNow();
            Assert.That(guard.CurrentStateName, Is.EqualTo("Alert"), "Sleep -> Alert reads Toggle.isOn == true.");
        }

        [Test]
        [Category(NeoPerf.Category)]
        [Explicit(NeoPerf.ExplicitReason)]
        public void Performance_NoCodeEvaluationScalesWithOutgoingTransitions()
        {
            const int operationsPerSample = 1000;
            int[] stateCounts = { 4, 32, 128 };
            for (int index = 0; index < stateCounts.Length; index++)
            {
                int states = stateCounts[index];
                StateMachine<IState> machine = CreateRing(states, 2, out StateData first, out _);
                machine.ChangeState(first);
                NeoPerf.MeasureSynchronous("statemachine.nocode_evaluate.s" + states, states, operationsPerSample,
                        NeoPerf.DefaultWarmupSamples, 60, machine.EvaluateTransitions)
                    .Add("transitions", states * 2)
                    .Add("role", "current")
                    .Emit();

                // WHY: the old shared typeof(StateData) bucket evaluated and reported every NoCode transition; this
                // replays only that removed walk (source check + report), not the duration scopes it also pushed.
                IReadOnlyList<StateTransition> all = machine.GetAvailableTransitions(typeof(StateData));
                IState current = machine.CurrentState;
                NeoPerf.MeasureSynchronous("statemachine.nocode_evaluate.s" + states + ".legacy_emulated", states,
                        operationsPerSample, NeoPerf.DefaultWarmupSamples, 60, () =>
                        {
                            for (int i = 0; i < all.Count; i++)
                            {
                                StateTransition transition = all[i];
                                machine.OnTransitionEvaluated.Invoke(transition, transition.CanTransition(current));
                            }
                        })
                    .Add("transitions", states * 2)
                    .Add("role", "legacy_emulated")
                    .Add("kind", "removed_part_only")
                    .Emit();
                Assert.That(machine.CurrentState, Is.SameAs(first));
            }
        }

        private StateMachine<IState> CreateRing(int stateCount, int blockedPerState, out StateData first,
            out CountingPredicate[] gates)
        {
            StateMachine<IState> machine = new(true, true, () => 0f);
            StateData[] states = new StateData[stateCount];
            for (int i = 0; i < stateCount; i++)
            {
                states[i] = NewState("S" + i);
            }

            gates = new CountingPredicate[stateCount * blockedPerState];
            for (int i = 0; i < stateCount; i++)
            {
                for (int k = 0; k < blockedPerState; k++)
                {
                    CountingPredicate gate = new();
                    gates[i * blockedPerState + k] = gate;
                    machine.RegisterTransition(Transition(states[i], states[(i + 1 + k) % stateCount], k, gate));
                }
            }

            first = states[0];
            return machine;
        }

        private static int TotalEvaluations(CountingPredicate[] gates)
        {
            int total = 0;
            for (int i = 0; i < gates.Length; i++)
            {
                total += gates[i].Evaluations;
            }

            return total;
        }

        private static StateTransition Transition(StateData from, StateData to, int priority, StatePredicate gate)
        {
            StateTransition transition = new()
            {
                FromStateData = from,
                ToStateData = to,
                Priority = priority,
                TransitionName = (from != null ? from.StateName : "Any") + " -> " + to.StateName
            };
            if (gate != null)
            {
                transition.AddPredicate(gate);
            }

            return transition;
        }

        private static ConditionEntryPredicate NoisePredicate(bool noiseOn)
        {
            return new ConditionEntryPredicate
            {
                ContextSlot = ConditionContextSlot.Override1,
                ConditionEntry = new ConditionEntry
                {
                    Source = SourceMode.Component,
                    ComponentTypeName = typeof(Toggle).FullName,
                    PropertyName = nameof(Toggle.isOn),
                    CurrentValueType = Neo.Condition.ValueType.Bool,
                    Compare = CompareOp.Equal,
                    ThresholdSource = ThresholdSource.Constant,
                    ThresholdBool = noiseOn
                }
            };
        }

        private GameObject NewObject(string name)
        {
            GameObject owner = new("StateMachineRouting " + name);
            _owned.Add(owner);
            return owner;
        }

        private StateData NewState(string stateName)
        {
            StateData state = NewAsset<StateData>();
            state.StateName = stateName;
            return state;
        }

        private T NewAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            _owned.Add(asset);
            return asset;
        }

        /// <summary>Predicate that records evaluations and owner-state entries and returns <see cref="Result"/>.</summary>
        private sealed class CountingPredicate : StatePredicate
        {
            public bool Result { get; set; }

            public int Evaluations { get; private set; }

            public int Entries { get; private set; }

            public override void OnOwnerStateEntered(float enterTime)
            {
                Entries++;
            }

            protected override bool EvaluateInternal(IState currentState)
            {
                Evaluations++;
                return Result;
            }
        }

        private sealed class SourceState : IState
        {
            public void OnEnter()
            {
            }

            public void OnUpdate()
            {
            }

            public void OnExit()
            {
            }

            public void OnFixedUpdate()
            {
            }

            public void OnLateUpdate()
            {
            }
        }

        private sealed class TargetState : IState
        {
            public void OnEnter()
            {
            }

            public void OnUpdate()
            {
            }

            public void OnExit()
            {
            }

            public void OnFixedUpdate()
            {
            }

            public void OnLateUpdate()
            {
            }
        }
    }
}
