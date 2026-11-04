using System;
using System.Collections.Generic;
using Neo.StateMachine;
using Neo.StateMachine.NoCode;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

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

                // WHY: constructor failure is deliberately logged only when the existing global debug gate is enabled.
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

        [TestCase("evaluate", false, true)]
        [TestCase("evaluate", true, true)]
        [TestCase("evaluate", false, false)]
        [TestCase("evaluate", true, false)]
        [TestCase("try", false, true)]
        [TestCase("try", true, true)]
        [TestCase("try", false, false)]
        [TestCase("try", true, false)]
        [TestCase("change", false, true)]
        [TestCase("change", true, true)]
        [TestCase("change", false, false)]
        [TestCase("change", true, false)]
        public void SuccessfulTargetConstructor_PreservesNewerChoiceWithoutCachingOrFallback(
            string activation, bool stop, bool cacheStates)
        {
            StateMachine<IState> machine = new(cacheStates, true);
            List<string> transcript = new();
            ConstructorLifecycleState source = new() { Name = "A", Transcript = transcript };
            ConstructorLifecycleState chosen = new() { Name = "C", Transcript = transcript };
            ConstructorFallbackState fallback = machine.GetOrCreateState<ConstructorFallbackState>();
            fallback.Name = "D";
            fallback.Transcript = transcript;
            StateTransition automatic = CreateTransition(typeof(ConstructorLifecycleState),
                typeof(SuccessfulConstructorState), 10);
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
            machine.RegisterTransition(automatic);
            machine.RegisterTransition(lowerPriority);
            bool accepted = false;
            Action activate = () =>
            {
                switch (activation)
                {
                    case "evaluate":
                        machine.EvaluateTransitions();
                        break;
                    case "try":
                        accepted = machine.TryChangeState<SuccessfulConstructorState>();
                        break;
                    case "change":
                        machine.ChangeState<SuccessfulConstructorState>();
                        break;
                    default:
                        Assert.Fail("Unknown activation path: " + activation);
                        break;
                }
            };

            try
            {
                machine.ChangeState(source);
                Assert.That(transcript, Is.EqualTo(new[] { "exited -", "enter A", "entered A", "changed - A" }));
                transcript.Clear();
                SuccessfulConstructorState canceled = null;
                int constructions = 0;
                SuccessfulConstructorState.OneShotConstructorHook = state =>
                {
                    constructions++;
                    canceled = state;
                    state.Name = "B";
                    state.Transcript = transcript;
                    if (stop)
                    {
                        machine.Stop();
                    }
                    else
                    {
                        machine.ChangeState(chosen);
                    }
                };

                activate();

                Assert.That(constructions, Is.EqualTo(1));
                Assert.That(canceled, Is.Not.Null);
                Assert.That(SuccessfulConstructorState.OneShotConstructorHook, Is.Null);
                Assert.That(machine.CurrentState, Is.SameAs(stop ? null : chosen));
                Assert.That(machine.PreviousState, Is.SameAs(source));
                if (activation == "try")
                {
                    Assert.That(accepted, Is.False, "The superseded conditional request did not enter B.");
                }

                Assert.That(transcript, Is.EqualTo(stop
                    ? new[] { "exit A", "exited A", "changed A -" }
                    : new[] { "exit A", "exited A", "enter C", "entered C", "changed A C" }));
                Assert.That(source.EnterCount, Is.EqualTo(1));
                Assert.That(source.ExitCount, Is.EqualTo(1));
                Assert.That(chosen.EnterCount, Is.EqualTo(stop ? 0 : 1));
                Assert.That(chosen.ExitCount, Is.Zero);
                Assert.That(canceled.EnterCount, Is.Zero);
                Assert.That(canceled.ExitCount, Is.Zero);
                Assert.That(fallback.EnterCount, Is.Zero);
                Assert.That(evaluated, Is.EqualTo(activation == "evaluate"
                    ? new[] { automatic } : Array.Empty<StateTransition>()));

                transcript.Clear();
                machine.ChangeState(source);
                Assert.That(transcript, Is.EqualTo(stop
                    ? new[] { "exited -", "enter A", "entered A", "changed - A" }
                    : new[] { "exit C", "exited C", "enter A", "entered A", "changed C A" }));
                Assert.That(source.EnterCount, Is.EqualTo(2));
                Assert.That(chosen.ExitCount, Is.EqualTo(stop ? 0 : 1));
                transcript.Clear();
                evaluated.Clear();
                SuccessfulConstructorState recovered = null;
                SuccessfulConstructorState.OneShotConstructorHook = state =>
                {
                    constructions++;
                    recovered = state;
                    state.Name = "B";
                    state.Transcript = transcript;
                };

                activate();

                Assert.That(recovered, Is.Not.Null, "The superseded instance must not have entered the cache.");
                Assert.That(recovered, Is.Not.SameAs(canceled));
                Assert.That(machine.CurrentState, Is.SameAs(recovered));
                Assert.That(machine.PreviousState, Is.SameAs(source));
                Assert.That(constructions, Is.EqualTo(2));
                Assert.That(canceled.EnterCount, Is.Zero);
                Assert.That(source.ExitCount, Is.EqualTo(2));
                Assert.That(recovered.EnterCount, Is.EqualTo(1));
                Assert.That(transcript, Is.EqualTo(new[]
                {
                    "exit A", "exited A", "enter B", "entered B", "changed A B"
                }));
                Assert.That(evaluated, Is.EqualTo(activation == "evaluate"
                    ? new[] { automatic } : Array.Empty<StateTransition>()));
                if (activation == "try")
                {
                    Assert.That(accepted, Is.True);
                }

                machine.ChangeState(source);
                transcript.Clear();
                evaluated.Clear();
                SuccessfulConstructorState lastConstructed = recovered;
                SuccessfulConstructorState.OneShotConstructorHook = state =>
                {
                    constructions++;
                    lastConstructed = state;
                    state.Name = "B";
                    state.Transcript = transcript;
                };

                activate();

                Assert.That(machine.CurrentState, Is.SameAs(lastConstructed));
                Assert.That(machine.PreviousState, Is.SameAs(source));
                Assert.That(constructions, Is.EqualTo(cacheStates ? 2 : 3));
                if (cacheStates)
                {
                    Assert.That(lastConstructed, Is.SameAs(recovered));
                }
                else
                {
                    Assert.That(lastConstructed, Is.Not.SameAs(recovered));
                }

                Assert.That(source.EnterCount, Is.EqualTo(3));
                Assert.That(source.ExitCount, Is.EqualTo(3));
                Assert.That(recovered.EnterCount, Is.EqualTo(cacheStates ? 2 : 1));
                Assert.That(recovered.ExitCount, Is.EqualTo(1));
                Assert.That(lastConstructed.EnterCount, Is.EqualTo(cacheStates ? 2 : 1));
                Assert.That(canceled.EnterCount, Is.Zero);
                Assert.That(fallback.EnterCount, Is.Zero);
                Assert.That(transcript, Is.EqualTo(new[]
                {
                    "exit A", "exited A", "enter B", "entered B", "changed A B"
                }));
                Assert.That(evaluated, Is.EqualTo(activation == "evaluate"
                    ? new[] { automatic } : Array.Empty<StateTransition>()));
                if (activation == "try")
                {
                    Assert.That(accepted, Is.True);
                }

                transcript.Clear();
                machine.Stop();
                Assert.That(machine.CurrentState, Is.Null);
                Assert.That(machine.PreviousState, Is.SameAs(lastConstructed));
                Assert.That(lastConstructed.ExitCount, Is.EqualTo(cacheStates ? 2 : 1));
                Assert.That(transcript, Is.EqualTo(new[] { "exit B", "exited B", "changed B -" }));
            }
            finally
            {
                SuccessfulConstructorState.OneShotConstructorHook = null;
                machine.Stop();
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

        [Test]
        public void CachedTypeTargets_RepeatedAutomaticTransitions_DoNotAllocate()
        {
            StateMachine<IState> machine = new(true, true, () => 0f);
            machine.RegisterTransition(CreateTransition(typeof(PingState), typeof(PongState)));
            machine.RegisterTransition(CreateTransition(typeof(PongState), typeof(PingState)));
            machine.ChangeState<PingState>();
            PingState ping = (PingState)machine.CurrentState;
            for (int i = 0; i < 8; i++)
            {
                machine.EvaluateTransitions();
            }

            PongState pong = machine.GetOrCreateState<PongState>();
            Assert.That(machine.CurrentState, Is.SameAs(ping));
            int pingEnters = ping.EnterCount;
            int pongEnters = pong.EnterCount;
            const int iterations = 1000;
            AssertAllocationCounterObservesPayload();

            Assert.That(() =>
                {
                    for (int i = 0; i < iterations; i++)
                    {
                        machine.EvaluateTransitions();
                    }
                }, Is.Not.AllocatingGCMemory(),
                "A cached CLR target must not repeat reflection admission on every automatic transition.");

            Assert.That(machine.CurrentState, Is.SameAs(ping));
            Assert.That(ping.EnterCount, Is.EqualTo(pingEnters + iterations / 2));
            Assert.That(pong.EnterCount, Is.EqualTo(pongEnters + iterations / 2));
            Assert.That(machine.GetOrCreateState<PingState>(), Is.SameAs(ping));
        }

        [Test]
        public void RejectedTypeTargets_AreNotReflectedAgain_AndKeepFallbackOrder()
        {
            StateMachine<IState> fresh = CreateAdmissionMachine(false);
            fresh.ChangeState<PingState>();
            IState previousPong = null;
            for (int round = 0; round < 3; round++)
            {
                fresh.EvaluateTransitions();
                Assert.That(fresh.CurrentState, Is.TypeOf<PongState>(),
                    "Remembered rejections must keep falling through to the next valid rule.");
                Assert.That(fresh.CurrentState, Is.Not.SameAs(previousPong),
                    "Without state caching every activation still constructs a fresh target.");
                previousPong = fresh.CurrentState;
                fresh.EvaluateTransitions();
                Assert.That(fresh.CurrentState, Is.TypeOf<PingState>());
            }

            StateMachine<IState> cached = CreateAdmissionMachine(true);
            cached.ChangeState<PingState>();
            PingState ping = (PingState)cached.CurrentState;
            for (int i = 0; i < 8; i++)
            {
                cached.EvaluateTransitions();
            }

            PongState pong = cached.GetOrCreateState<PongState>();
            int pongEnters = pong.EnterCount;
            const int rounds = 200;
            AssertAllocationCounterObservesPayload();

            // WHY: a rejected target without a parameterless constructor made the old admission call
            // GetConstructor, which allocates, on every round; a remembered rejection must not reflect again.
            Assert.That(() =>
                {
                    for (int round = 0; round < rounds; round++)
                    {
                        cached.EvaluateTransitions();
                        cached.EvaluateTransitions();
                    }
                }, Is.Not.AllocatingGCMemory(),
                "Rejected CLR targets must not repeat reflection admission on every evaluation.");

            Assert.That(cached.CurrentState, Is.SameAs(ping));
            Assert.That(pong.EnterCount, Is.EqualTo(pongEnters + rounds),
                "Every Ping evaluation must still fall through both rejected rules to Pong.");
        }

        [Test]
        [Category(NeoPerf.Category)]
        [Explicit(NeoPerf.ExplicitReason)]
        public void Performance_EvaluateTransitionsScanAndTypeActivation()
        {
            const int operationsPerSample = 1000;
            Func<bool> alwaysFalse = () => false;
            int[] transitionCounts = { 0, 16, 128 };
            for (int countIndex = 0; countIndex < transitionCounts.Length; countIndex++)
            {
                int count = transitionCounts[countIndex];
                StateMachine<IState> scanMachine = new(true, true, () => 0f);
                for (int i = 0; i < count; i++)
                {
                    StateTransition blocked = CreateTransition(typeof(PingState), typeof(PongState), i);
                    CustomPredicate predicate = new();
                    predicate.SetEvaluator(alwaysFalse);
                    blocked.AddPredicate(predicate);
                    scanMachine.RegisterTransition(blocked);
                }

                scanMachine.ChangeState<PingState>();
                MeasureEditorPerf("statemachine.evaluate_scan.t" + count, count, operationsPerSample,
                    scanMachine.EvaluateTransitions, null);
                Assert.That(scanMachine.CurrentState, Is.TypeOf<PingState>());
            }

            StateMachine<IState> cachedMachine = new(true, true, () => 0f);
            cachedMachine.RegisterTransition(CreateTransition(typeof(PingState), typeof(PongState)));
            cachedMachine.RegisterTransition(CreateTransition(typeof(PongState), typeof(PingState)));
            cachedMachine.ChangeState<PingState>();
            MeasureEditorPerf("statemachine.type_transition.cached", 2, operationsPerSample,
                cachedMachine.EvaluateTransitions, null);
            Assert.That(cachedMachine.CurrentState, Is.TypeOf<PingState>().Or.TypeOf<PongState>());

            StateMachine<IState> rejectingMachine = CreateAdmissionMachine(true);
            rejectingMachine.ChangeState<PingState>();
            MeasureEditorPerf("statemachine.type_transition.cached_with_rejections", 4, operationsPerSample,
                rejectingMachine.EvaluateTransitions, null);
            Assert.That(rejectingMachine.CurrentState, Is.TypeOf<PingState>().Or.TypeOf<PongState>());

            StateMachine<IState> uncachedMachine = new(false, true, () => 0f);
            uncachedMachine.RegisterTransition(CreateTransition(typeof(PingState), typeof(PongState)));
            uncachedMachine.RegisterTransition(CreateTransition(typeof(PongState), typeof(PingState)));
            uncachedMachine.ChangeState<PingState>();
            MeasureEditorPerf("statemachine.type_transition.uncached", 2, operationsPerSample,
                uncachedMachine.EvaluateTransitions, null);

            // WHY: before the per-machine admission memo every type-target transition re-ran this reflection.
            // This micro-benchmark times only that removed part, not the old transition path as a whole.
            Type target = typeof(PongState);
            bool admitted = false;
            MeasureEditorPerf("statemachine.type_admission.legacy_reflection", 1, operationsPerSample, () =>
            {
                admitted = target.IsClass && typeof(IState).IsAssignableFrom(target) && !target.IsAbstract &&
                           !target.ContainsGenericParameters &&
                           !typeof(UnityEngine.Object).IsAssignableFrom(target) &&
                           target.GetConstructor(Type.EmptyTypes) != null;
            }, "kind=removed_part_only");
            Assert.That(admitted, Is.True);
        }

        private static StateMachine<IState> CreateAdmissionMachine(bool cacheStates)
        {
            StateMachine<IState> machine = new(cacheStates, true, () => 0f);
            machine.RegisterTransition(CreateTransition(typeof(PingState), typeof(AbstractState), 20));
            machine.RegisterTransition(CreateTransition(typeof(PingState), typeof(NeedsArgumentState), 10));
            machine.RegisterTransition(CreateTransition(typeof(PingState), typeof(PongState), 0));
            machine.RegisterTransition(CreateTransition(typeof(PongState), typeof(PingState), 0));
            return machine;
        }

        /// <summary>
        ///     Positive control for the zero-allocation assertions: the GC.Alloc sampler must see a retained payload.
        /// </summary>
        /// <remarks>GC.GetAllocatedBytesForCurrentThread is not used: it read 0 for a retained payload in this Editor.</remarks>
        private static void AssertAllocationCounterObservesPayload()
        {
            byte[] payload = null;
            Assert.That(() => { payload = new byte[4096]; }, Is.AllocatingGCMemory(),
                "The GC.Alloc sampler must observe a known allocation before a zero result is trusted.");
            Assert.That(payload.Length, Is.EqualTo(4096));
        }

        /// <summary>
        ///     Prints one NEO_PERF line through the shared <see cref="NeoPerf" /> writer (Stopwatch percentiles,
        ///     calibrated GC.Alloc counts per operation) plus the optional preformatted <paramref name="extraKeys" />.
        /// </summary>
        private static void MeasureEditorPerf(string caseId, int scale, int operationsPerSample, Action operation,
            string extraKeys)
        {
            NeoPerf.MeasureSynchronous(caseId, scale, operationsPerSample, NeoPerf.DefaultWarmupSamples, 60, operation)
                .AddPairs(extraKeys)
                .Emit();
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

        public class CountingState : IState
        {
            public int EnterCount { get; private set; }
            public int ExitCount { get; private set; }

            public void OnEnter() { EnterCount++; }
            public void OnExit() { ExitCount++; }
            public void OnUpdate() { }
            public void OnFixedUpdate() { }
            public void OnLateUpdate() { }
        }

        public sealed class PingState : CountingState { }
        public sealed class PongState : CountingState { }
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

        public sealed class SuccessfulConstructorState : ConstructorLifecycleState
        {
            public static Action<SuccessfulConstructorState> OneShotConstructorHook { get; set; }

            public SuccessfulConstructorState()
            {
                Action<SuccessfulConstructorState> callback = OneShotConstructorHook;
                OneShotConstructorHook = null;
                callback?.Invoke(this);
            }
        }

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
