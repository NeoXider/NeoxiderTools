using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Neo.StateMachine;
using Neo.StateMachine.NoCode;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using RuntimeGenericHost = Neo.Editor.Tests.Edit.Audit20261023NeoStateMachineGenericHost;

namespace Neo.Editor.Tests.Edit
{
    /// <summary>
    ///     Covers the 2026-11 StateMachine wrapper quality pass: configuration made before Awake survives, initial C#
    ///     state names resolve outside Neo.StateMachine, the assigned initial StateData wins over a name collision,
    ///     per-frame evaluation does not allocate, unnamed transitions reuse their label, and NoCode actions are
    ///     allocation-free and tolerant of list edits.
    ///     EditMode public-API contracts; lifecycle methods are invoked reflectively, not by the native PlayerLoop.
    /// </summary>
    [TestFixture]
    public sealed class StateMachineWrapperQualityTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
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

        [TestCase(false)]
        [TestCase(true)]
        public void ConfigurationBeforeAwake_SurvivesAwake(bool generic)
        {
            GameObject owner = NewObject("pre-awake");
            StateData initial = NewState("Initial");
            StateData destination = NewState("Destination");
            Component host = AddHost(owner, generic);
            StateMachine<IState> machine = GetMachine(host);
            RegisterTransition(host, new StateTransition { FromStateData = initial, ToStateData = destination });
            machine.ChangeState(initial);
            int initializedEvents = 0;
            if (host is StateMachineBehaviourBase untyped)
            {
                untyped.OnInitialized.AddListener(() => initializedEvents++);
            }

            InvokeLifecycle(host, "Awake");

            Assert.That(GetMachine(host), Is.SameAs(machine), "Awake must keep the machine configured before it.");
            Assert.That(machine.CurrentState, Is.SameAs(initial));
            if (host is StateMachineBehaviourBase configured)
            {
                Assert.That(configured.CurrentStateName, Is.EqualTo("Initial"));
                Assert.That(initializedEvents, Is.EqualTo(1));
                configured.EvaluateTransitionsNow();
            }
            else
            {
                ((RuntimeGenericHost)host).EvaluateTransitionsNow();
            }

            Assert.That(machine.CurrentState, Is.SameAs(destination), "The transition registered before Awake runs.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InitialStateTypeName_ResolvesStatesOutsideTheStateMachineAssembly(bool assemblyQualified)
        {
            GameObject owner = NewObject("typed initial");
            RuntimeGenericHost host = owner.AddComponent<RuntimeGenericHost>();
            using (SerializedObject serialized = new(host))
            {
                serialized.FindProperty("initialStateTypeName").stringValue = assemblyQualified
                    ? typeof(TypedInitialState).AssemblyQualifiedName
                    : typeof(TypedInitialState).FullName;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            InvokeLifecycle(host, "Awake");
            InvokeLifecycle(host, "Start");

            Assert.That(host.CurrentState, Is.InstanceOf<TypedInitialState>(),
                "A namespace-qualified name of a state outside Neo.StateMachine must resolve.");
            Assert.That(((TypedInitialState)host.CurrentState).EnterCount, Is.EqualTo(1));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void InitialStateTypeName_Unresolved_WarnsInAwakeOnlyWithOwnDebugLog(bool debugLog)
        {
            GameObject owner = NewObject("missing initial");
            RuntimeGenericHost host = owner.AddComponent<RuntimeGenericHost>();
            using (SerializedObject serialized = new(host))
            {
                serialized.FindProperty("initialStateTypeName").stringValue = "Missing.Namespace.NoSuchState";
                serialized.FindProperty("enableDebugLog").boolValue = debugLog;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            if (debugLog)
            {
                LogAssert.Expect(LogType.Warning, new Regex("Missing\\.Namespace\\.NoSuchState"));
            }

            InvokeLifecycle(host, "Awake");

            LogAssert.NoUnexpectedReceived();
            Assert.That(host.CurrentState, Is.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LoadFromStateMachineData_EntersAssignedInitialAssetWhenNamesCollide(bool generic)
        {
            GameObject owner = NewObject("name collision");
            StateData decoy = NewState("New State");
            StateData initial = NewState("New State");
            StateMachineData data = NewAsset<StateMachineData>();
            data.States = new[] { decoy, initial };
            data.InitialState = initial;
            Component host = AddHost(owner, generic);

            if (host is StateMachineBehaviourBase untyped)
            {
                untyped.StateMachineData = data;
                untyped.LoadFromStateMachineData();
            }
            else
            {
                RuntimeGenericHost typed = (RuntimeGenericHost)host;
                typed.StateMachineData = data;
                typed.LoadFromStateMachineData();
            }

            Assert.That(GetMachine(host).CurrentState, Is.SameAs(initial),
                "The assigned initial asset must win over the first state with the same name.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EvaluateTransitionsNow_WarmCall_DoesNotAllocate(bool generic)
        {
            GameObject owner = NewObject("allocation");
            StateData idle = NewState("Idle");
            StateData run = NewState("Run");
            Component host = AddHost(owner, generic);
            StateTransition blocked = new() { FromStateData = idle, ToStateData = run, TransitionName = "Blocked" };
            blocked.AddPredicate(new BoolPredicate { Value = false });
            RegisterTransition(host, blocked);
            GetMachine(host).ChangeState(idle);
            Evaluate(host);
            Evaluate(host);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                Evaluate(host);
            }

            long allocated = allocationCounter.End();
            Assert.That(GetMachine(host).CurrentState, Is.SameAs(idle));
            Assert.That(allocated, Is.Zero, "Per-frame evaluation must not allocate a context delegate.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PlayerLoopCallbacks_WarmTicks_DoNotAllocate(bool generic)
        {
            GameObject owner = NewObject("ticks");
            StateData idle = NewState("Idle");
            StateData run = NewState("Run");
            StateData unrelated = NewState("Unrelated");
            Component host = AddHost(owner, generic);
            StateTransition blocked = new() { FromStateData = idle, ToStateData = run, TransitionName = "Blocked" };
            blocked.AddPredicate(new BoolPredicate { Value = false });
            RegisterTransition(host, blocked);
            RegisterTransition(host, new StateTransition
            {
                FromStateData = unrelated, ToStateData = run, TransitionName = "Unrelated"
            });
            GetMachine(host).ChangeState(idle);
            Action update = LifecycleDelegate(host, "Update");
            Action fixedUpdate = LifecycleDelegate(host, "FixedUpdate");
            Action lateUpdate = LifecycleDelegate(host, "LateUpdate");
            Action tick = () =>
            {
                for (int i = 0; i < Iterations; i++)
                {
                    update();
                    fixedUpdate();
                    lateUpdate();
                }
            };
            update();
            fixedUpdate();
            lateUpdate();

            GcAlloc.AssertNone(tick,
                "Update (with auto evaluation), FixedUpdate and LateUpdate must not allocate once warm.");

            Assert.That(GetMachine(host).CurrentState, Is.SameAs(idle),
                "The always-true transition leaving another asset must not fire from Idle.");
        }

        [Test]
        public void UnnamedTransition_ReportsCachedFallbackLabelUntilItsInputsChange()
        {
            GameObject owner = NewObject("unnamed");
            StateData idle = NewState("Idle");
            StateData run = NewState("Run");
            StateMachineBehaviourBase host = owner.AddComponent<StateMachineBehaviourBase>();
            StateTransition transition = new() { FromStateData = idle, ToStateData = run, TransitionName = "" };
            transition.AddPredicate(new BoolPredicate { Value = false });
            host.RegisterTransition(transition);
            host.StateMachine.ChangeState(idle);
            List<string> names = new();
            List<bool> results = new();
            host.OnTransitionEvaluated.AddListener((name, result) =>
            {
                names.Add(name);
                results.Add(result);
            });

            host.EvaluateTransitionsNow();
            host.EvaluateTransitionsNow();
            run.StateName = "Sprint";
            host.EvaluateTransitionsNow();

            Assert.That(names, Is.EqualTo(new[] { "Idle -> Run", "Idle -> Run", "Idle -> Sprint" }));
            Assert.That(ReferenceEquals(names[0], names[1]), Is.True, "The fallback label is built once.");
            Assert.That(results, Is.EqualTo(new[] { false, false, false }));
        }

        [Test]
        public void LogStateAction_WhileDebugLogIsOff_DoesNotAllocate()
        {
            PropertyInfo enabled = Type.GetType("Neo.StateMachine.StateMachineLog, Neo.StateMachine")
                ?.GetProperty("Enabled", BindingFlags.Public | BindingFlags.Static);
            Assert.That(enabled, Is.Not.Null, "StateMachineLog.Enabled not found.");
            bool previous = (bool)enabled.GetValue(null);
            enabled.SetValue(null, false);
            try
            {
                LogStateAction action = new() { Message = "tick" };
                action.Execute();

                using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
                allocationCounter.Begin();
                for (int i = 0; i < Iterations; i++)
                {
                    action.Execute();
                }

                long allocated = allocationCounter.End();
                Assert.That(allocated, Is.Zero, "On Update log actions must not format messages that are never logged.");
            }
            finally
            {
                enabled.SetValue(null, previous);
            }
        }

        [Test]
        public void StateData_ActionAppendingActionsDuringEnter_CompletesTheListAndRunsAppendedOnNextPass()
        {
            StateData state = NewState("Mutating");
            CountingAction appended = new();
            CountingAction last = new();
            AppendingAction appender = new(state.OnEnterActions, appended);
            state.OnEnterActions.Add(appender);
            state.OnEnterActions.Add(last);

            Assert.DoesNotThrow(() => state.OnEnter());

            Assert.That(last.Count, Is.EqualTo(1), "Actions after the editing action still run.");
            Assert.That(appended.Count, Is.Zero, "An action appended during a pass waits for the next pass.");

            state.OnEnterActions.Remove(appender);
            state.OnEnter();

            Assert.That(last.Count, Is.EqualTo(2));
            Assert.That(appended.Count, Is.EqualTo(1), "The appended action runs on the next pass.");
        }

        [Test]
        public void StateData_ActionThatKeepsAppendingAppenders_FinishesThePass()
        {
            StateData state = NewState("Self appending");
            SelfReplicatingAction seed = new(state.OnEnterActions);
            state.OnEnterActions.Add(seed);

            Assert.DoesNotThrow(() => state.OnEnter());

            Assert.That(seed.Count, Is.EqualTo(1), "A pass runs only the actions present when it started.");
            Assert.That(state.OnEnterActions.Count, Is.EqualTo(2));
        }

        private static Component AddHost(GameObject owner, bool generic)
        {
            return generic
                ? owner.AddComponent<RuntimeGenericHost>()
                : owner.AddComponent<StateMachineBehaviourBase>();
        }

        private static StateMachine<IState> GetMachine(Component host)
        {
            return host is StateMachineBehaviourBase untyped
                ? untyped.StateMachine
                : ((RuntimeGenericHost)host).StateMachine;
        }

        private static void RegisterTransition(Component host, StateTransition transition)
        {
            if (host is StateMachineBehaviourBase untyped)
            {
                untyped.RegisterTransition(transition);
            }
            else
            {
                ((RuntimeGenericHost)host).RegisterTransition(transition);
            }
        }

        private static void Evaluate(Component host)
        {
            if (host is StateMachineBehaviourBase untyped)
            {
                untyped.EvaluateTransitionsNow();
            }
            else
            {
                ((RuntimeGenericHost)host).EvaluateTransitionsNow();
            }
        }

        /// <summary>Binds a private PlayerLoop callback once, so the measured loop runs no reflection.</summary>
        private static Action LifecycleDelegate(Component host, string method)
        {
            Type declaring = host is StateMachineBehaviourBase
                ? typeof(StateMachineBehaviourBase)
                : typeof(StateMachineBehaviour<IState>);
            MethodInfo lifecycle = declaring.GetMethod(method, PrivateInstance);
            Assert.That(lifecycle, Is.Not.Null, declaring.Name + "." + method + " not found.");
            return (Action)Delegate.CreateDelegate(typeof(Action), host, lifecycle);
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
            GameObject owner = new("StateMachineWrapperQuality " + name);
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

        /// <summary>Plain C# state referenced by full name from the serialized initial-state field.</summary>
        public sealed class TypedInitialState : IState
        {
            public int EnterCount { get; private set; }

            public void OnEnter()
            {
                EnterCount++;
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

        [Serializable]
        private sealed class CountingAction : StateAction
        {
            public int Count { get; private set; }

            public override void Execute()
            {
                Count++;
            }
        }

        [Serializable]
        private sealed class AppendingAction : StateAction
        {
            private readonly List<StateAction> _list;
            private readonly StateAction _toAppend;

            public AppendingAction(List<StateAction> list, StateAction toAppend)
            {
                _list = list;
                _toAppend = toAppend;
            }

            public override void Execute()
            {
                _list.Add(_toAppend);
            }
        }

        [Serializable]
        private sealed class SelfReplicatingAction : StateAction
        {
            private readonly List<StateAction> _list;

            public SelfReplicatingAction(List<StateAction> list)
            {
                _list = list;
            }

            public int Count { get; private set; }

            public override void Execute()
            {
                Count++;
                _list.Add(new SelfReplicatingAction(_list));
            }
        }
    }
}
