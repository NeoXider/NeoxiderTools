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
using RuntimeGenericHost = Neo.Editor.Tests.Edit.Audit20261023NeoStateMachineGenericHost;

namespace Neo.Editor.Tests.Edit
{
    /// <summary>
    ///     Covers the 2026-11 StateMachine closure pass: C# source types edited after registration route from their
    ///     new state, diagnostics are scoped per machine (one behaviour's Enable Debug Log no longer changes another
    ///     machine's logs), an explicit StateMachineData.Validate(false) always reports, and a saved (cloned) guard
    ///     asset whose predicates leave Source Object empty still reads its context slots.
    ///     EditMode public-API contracts; lifecycle methods are invoked reflectively, not by the native PlayerLoop.
    /// </summary>
    [TestFixture]
    public sealed class StateMachineClosureTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly List<Object> _owned = new();
        private PropertyInfo _sharedSwitch;
        private bool _sharedSwitchBefore;

        [SetUp]
        public void SetUp()
        {
            _sharedSwitch = Type.GetType("Neo.StateMachine.StateMachineLog, Neo.StateMachine")
                ?.GetProperty("Enabled", BindingFlags.Public | BindingFlags.Static);
            Assert.That(_sharedSwitch, Is.Not.Null, "StateMachineLog.Enabled not found.");
            _sharedSwitchBefore = (bool)_sharedSwitch.GetValue(null);
        }

        [TearDown]
        public void TearDown()
        {
            _sharedSwitch?.SetValue(null, _sharedSwitchBefore);
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
        public void FromStateTypeEditedAfterRegistration_RoutesFromItsNewType(bool cacheTransitions)
        {
            StateMachine<IState> machine = new(true, cacheTransitions, () => 0f);
            StateTransition moved = new() { FromStateType = typeof(SourceState), ToStateType = typeof(TargetState) };
            machine.RegisterTransition(moved);
            machine.ChangeState<OtherState>();
            machine.EvaluateTransitions();
            Assert.That(machine.CurrentState, Is.TypeOf<OtherState>());

            moved.FromStateType = typeof(OtherState);
            machine.EvaluateTransitions();

            Assert.That(machine.CurrentState, Is.TypeOf<TargetState>(),
                "The warm list of the new source type must pick up a transition registered under another type.");
            if (cacheTransitions)
            {
                Assert.That(machine.GetAvailableTransitions(typeof(SourceState)), Is.Empty,
                    "The old source type no longer lists the moved transition.");
            }
        }

        [Test]
        public void FromStateTypeClearedAfterRegistration_BecomesSourceLess()
        {
            StateMachine<IState> machine = new(true, true, () => 0f);
            StateTransition cleared = new() { FromStateType = typeof(SourceState), ToStateType = typeof(TargetState) };
            machine.RegisterTransition(cleared);
            machine.ChangeState<OtherState>();
            machine.EvaluateTransitions();
            Assert.That(machine.CurrentState, Is.TypeOf<OtherState>());

            cleared.FromStateType = null;

            Assert.That(machine.GetAvailableTransitions(typeof(OtherState)), Is.EqualTo(new[] { cleared }));
            machine.EvaluateTransitions();
            Assert.That(machine.CurrentState, Is.TypeOf<TargetState>(), "A source-less transition leaves any state.");
        }

        [Test]
        public void SourceLessTransitionGivenASourceType_LeavesOtherTypeLists()
        {
            StateMachine<IState> machine = new(true, true, () => 0f);
            StateTransition scoped = new() { ToStateType = typeof(TargetState) };
            machine.RegisterTransition(scoped);
            Assert.That(machine.GetAvailableTransitions(typeof(OtherState)), Is.EqualTo(new[] { scoped }));

            scoped.FromStateType = typeof(SourceState);

            Assert.That(machine.GetAvailableTransitions(typeof(OtherState)), Is.Empty,
                "Other states must not evaluate (and report) a transition that now leaves only SourceState.");
            Assert.That(machine.GetAvailableTransitions(typeof(SourceState)), Is.EqualTo(new[] { scoped }));
        }

        [Test]
        public void RawMachine_DebugLogFollowsTheSharedSwitchUntilAssigned()
        {
            _sharedSwitch.SetValue(null, true);
            StateMachine<IState> machine = new(true, true, () => 0f);
            Assert.That(machine.DebugLogEnabled, Is.True, "Legacy: an unassigned machine follows the shared switch.");
            Assert.That(CaptureLogs(() => machine.ChangeState(null)), Has.Some.Contains("null state"));

            machine.DebugLogEnabled = false;
            Assert.That(CaptureLogs(() => machine.ChangeState(null)), Is.Empty,
                "An assigned machine ignores the shared switch.");

            _sharedSwitch.SetValue(null, false);
            machine.DebugLogEnabled = true;
            Assert.That(CaptureLogs(() => machine.RegisterTransition(null)), Has.Some.Contains("null transition"));
        }

        [Test]
        public void RawMachine_ActionLogsFollowTheMachineThatRunsThem()
        {
            _sharedSwitch.SetValue(null, false);
            StateData state = NewState("Ticking");
            state.OnUpdateActions.Add(new LogStateAction { Message = "tick from the loud machine" });
            StateMachine<IState> loud = new(true, true, () => 0f) { DebugLogEnabled = true };
            StateMachine<IState> quiet = new(true, true, () => 0f) { DebugLogEnabled = false };
            loud.ChangeState(state);
            quiet.ChangeState(state);

            Assert.That(CaptureLogs(loud.Update), Has.Some.Contains("tick from the loud machine"));
            Assert.That(CaptureLogs(quiet.Update), Is.Empty, "The same shared asset stays silent in a quiet machine.");
            Assert.That(CaptureLogs(() => state.OnUpdate()), Is.Empty,
                "Outside any machine the shared switch (off) applies.");
        }

        [Test]
        public void QuietMachineTicks_WithAnUpdateLogAction_DoNotAllocate()
        {
            _sharedSwitch.SetValue(null, true);
            StateData state = NewState("Quiet ticks");
            state.OnUpdateActions.Add(new LogStateAction { Message = "never formatted" });
            StateMachine<IState> machine = new(true, true, () => 0f) { DebugLogEnabled = false };
            machine.ChangeState(state);
            machine.Update();
            machine.FixedUpdate();
            machine.LateUpdate();
            Action tick = () =>
            {
                for (int i = 0; i < 200; i++)
                {
                    machine.Update();
                    machine.FixedUpdate();
                    machine.LateUpdate();
                }
            };

            GcAlloc.AssertNone(tick,
                "A quiet machine skips the action's message although the shared switch is on, and its log scope is free.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TwoBehaviours_OppositeDebugLog_KeepTheirOwnActionLogs(bool generic)
        {
            StateData first = NewState("First");
            first.OnEnterActions.Add(new LogStateAction { Message = "first entered" });
            StateData second = NewState("Second");
            second.OnEnterActions.Add(new LogStateAction { Message = "second entered" });
            Component loud = NewBehaviour("loud", generic, true);
            Component quiet = NewBehaviour("quiet", generic, false);
            InvokeLifecycle(loud, "OnEnable");
            InvokeLifecycle(quiet, "OnEnable");

            List<string> logs = CaptureLogs(() =>
            {
                ChangeState(loud, first);
                ChangeState(quiet, second);
            });
            Assert.That(logs, Has.Some.Contains("first entered"),
                "The loud behaviour keeps logging although a quiet behaviour was enabled after it.");
            Assert.That(logs, Has.None.Contains("second entered"));

            InvokeLifecycle(loud, "OnEnable");
            logs = CaptureLogs(() =>
            {
                ChangeState(quiet, first);
                ChangeState(loud, second);
            });
            Assert.That(logs, Has.None.Contains("first entered"),
                "The quiet behaviour stays quiet although a loud behaviour was enabled after it.");
            Assert.That(logs, Has.Some.Contains("second entered"), "The machine, not the asset, decides.");
        }

        [Test]
        public void EnableDebugLogSetter_ReachesTheRunningMachine()
        {
            StateMachineBehaviourBase owner = NewObject("setter").AddComponent<StateMachineBehaviourBase>();
            InvokeLifecycle(owner, "OnEnable");
            Assert.That(owner.StateMachine.DebugLogEnabled, Is.False);

            owner.EnableDebugLog = true;

            Assert.That(owner.EnableDebugLog, Is.True);
            Assert.That(owner.StateMachine.DebugLogEnabled, Is.True);
        }

        [Test]
        public void Validate_NonSilent_ReportsEvenWithTheSharedSwitchOff()
        {
            _sharedSwitch.SetValue(null, false);
            StateMachineData data = NewAsset<StateMachineData>();

            List<string> logs = CaptureLogs(() => Assert.That(data.Validate(false), Is.False));
            Assert.That(logs, Has.Some.Contains("No states defined"),
                "The Inspector's Validate Configuration button asks for console warnings.");
            Assert.That(CaptureLogs(() => Assert.That(data.Validate(true), Is.False)), Is.Empty);
        }

        [Test]
        public void ClonedGuardData_EmptySourcePredicatesStillReadContextSlots()
        {
            GameObject toggleObject = NewObject("Noise Box");
            Toggle noise = toggleObject.AddComponent<Toggle>();
            noise.isOn = false;
            StateData sleep = NewState("Sleep");
            StateData alert = NewState("Alert");
            StateMachineData authored = NewAsset<StateMachineData>();
            authored.States = new[] { sleep, alert };
            authored.InitialState = sleep;
            StateTransition toAlert = Transition(sleep, alert, NoisePredicate(true));
            StateTransition toSleep = Transition(alert, sleep, NoisePredicate(false));
            toSleep.AddPredicate(new StateDurationPredicate
            {
                RequiredDuration = 1.5f,
                Comparison = ComparisonType.GreaterThanOrEqual
            });
            authored.Transitions.Add(toAlert);
            authored.Transitions.Add(toSleep);

            // WHY: a serialization round trip, like a saved asset loaded in Play Mode; the clone's predicates and
            // condition entries are freshly deserialized while the StateData references stay shared.
            StateMachineData saved = Object.Instantiate(authored);
            _owned.Add(saved);
            ConditionEntryPredicate savedPredicate = (ConditionEntryPredicate)saved.Transitions[1].Predicates[0];
            Assert.That(savedPredicate, Is.Not.SameAs(toSleep.Predicates[0]), "Positive control: a real round trip.");
            Assert.That(saved.Transitions[1].FromStateData, Is.SameAs(alert));

            StateMachineBehaviourBase guard = NewObject("Guard").AddComponent<StateMachineBehaviourBase>();
            using (SerializedObject serializedGuard = new(guard))
            {
                SerializedProperty overrides = serializedGuard.FindProperty("contextOverrides");
                overrides.arraySize = 1;
                overrides.GetArrayElementAtIndex(0).objectReferenceValue = toggleObject;
                serializedGuard.ApplyModifiedPropertiesWithoutUndo();
            }

            guard.StateMachineData = saved;
            guard.LoadFromStateMachineData();
            Assert.That(guard.CurrentStateName, Is.EqualTo("Sleep"));

            guard.ChangeState(alert);
            Assert.That(guard.StateMachine.SetCurrentStateEnterTime(Time.time - 2f), Is.True);
            guard.EvaluateTransitionsNow();
            Assert.That(guard.CurrentStateName, Is.EqualTo("Sleep"),
                "Alert -> Sleep reads Toggle.isOn through slot Override1 after the round trip.");

            noise.isOn = true;
            guard.EvaluateTransitionsNow();
            Assert.That(guard.CurrentStateName, Is.EqualTo("Alert"));
        }

        private static StateTransition Transition(StateData from, StateData to, StatePredicate gate)
        {
            StateTransition transition = new()
            {
                FromStateData = from,
                ToStateData = to,
                TransitionName = from.StateName + " -> " + to.StateName
            };
            transition.AddPredicate(gate);
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

        private static List<string> CaptureLogs(Action body)
        {
            List<string> logs = new();
            Application.LogCallback handler = (condition, stackTrace, type) => logs.Add(condition);
            Application.logMessageReceived += handler;
            try
            {
                body();
            }
            finally
            {
                Application.logMessageReceived -= handler;
            }

            return logs;
        }

        private Component NewBehaviour(string name, bool generic, bool debugLog)
        {
            GameObject owner = NewObject(name);
            if (generic)
            {
                RuntimeGenericHost host = owner.AddComponent<RuntimeGenericHost>();
                host.EnableDebugLog = debugLog;
                return host;
            }

            StateMachineBehaviourBase behaviour = owner.AddComponent<StateMachineBehaviourBase>();
            behaviour.EnableDebugLog = debugLog;
            return behaviour;
        }

        private static void ChangeState(Component behaviour, StateData state)
        {
            if (behaviour is StateMachineBehaviourBase nonGeneric)
            {
                nonGeneric.ChangeState(state);
            }
            else
            {
                ((RuntimeGenericHost)behaviour).ChangeState(state);
            }
        }

        private static void InvokeLifecycle(Component host, string method)
        {
            Type declaring = host is StateMachineBehaviourBase
                ? typeof(StateMachineBehaviourBase)
                : typeof(StateMachineBehaviour<IState>);
            MethodInfo lifecycle = declaring.GetMethod(method, PrivateInstance);
            Assert.That(lifecycle, Is.Not.Null, declaring.Name + "." + method + " not found.");
            lifecycle.Invoke(host, null);
        }

        private GameObject NewObject(string name)
        {
            GameObject owner = new("StateMachineClosure " + name);
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

        public sealed class SourceState : IState
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

        public sealed class OtherState : IState
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

        public sealed class TargetState : IState
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
