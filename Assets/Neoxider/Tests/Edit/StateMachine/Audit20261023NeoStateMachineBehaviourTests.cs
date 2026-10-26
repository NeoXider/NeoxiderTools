using System;
using System.Collections;
using System.Collections.Generic;
using Neo.Condition;
using Neo.StateMachine;
using Neo.StateMachine.NoCode;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using RuntimeGenericHost = Neo.Editor.Tests.Edit.Audit20261023NeoStateMachineGenericHost;

namespace Neo.Editor.Tests.Edit
{
    public class Audit20261023NeoStateMachineBehaviourTests
    {
        private static readonly bool[] HostModes = { false, true };
        private static readonly string[] ExitModes = { "OnDisable", "OnDestroy", "Reload" };
        private readonly List<GameObject> nativeObjects = new();
        private readonly List<ScriptableObject> nativeAssets = new();

        [UnityTest]
        public IEnumerator EnterDisablesOwner_EmitsEnteredStateThenOneStopWithSceneContext(
            [ValueSource(nameof(HostModes))] bool generic)
        {
            yield return new EnterPlayMode();
            Assert.That(Application.isPlaying, Is.True);
            GameObject owner = CreateNativeObject("AuditStateMachineDisableDuringEntry");
            StateMachineData data = CreateNativeAsset<StateMachineData>();
            StateData source = CreateNativeAsset<StateData>();
            StateData entered = CreateNativeAsset<StateData>();
            Component host = AddNativeHost(owner, generic);
            source.StateName = "Source";
            entered.StateName = "Entered";
            data.States = new[] { source, entered };
            data.InitialState = source;
            ConfigureHost(host, data, Array.Empty<GameObject>());
            RecordContextAction onEnter = new();
            RecordContextAction onExit = new();
            entered.OnEnterActions.Add(onEnter);
            entered.OnEnterActions.Add(new SetContextGameObjectActiveAction
            {
                ContextSlot = ConditionContextSlot.Owner,
                SetActive = false
            });
            entered.OnExitActions.Add(onExit);

            int frame = Time.frameCount;
            double deadline = Time.realtimeSinceStartupAsDouble + 5d;
            owner.SetActive(true);
            while (AwaitingNativeFrame(frame, deadline)) yield return null;
            Assert.That(((Behaviour)host).isActiveAndEnabled, Is.True);
            StateMachine<IState> machine = GetMachine(host);
            Assert.That(machine.CurrentState, Is.SameAs(source), "Native Start must load the configured initial state.");
            List<string> events = new();
            RecordEvents(machine, events);

            ChangeHostState(host, "Entered");

            string[] stoppedEntry =
            {
                "exited Source", "entered Entered", "changed Source Entered",
                "exited Entered", "changed Entered -"
            };
            Assert.That(events, Is.EqualTo(stoppedEntry));
            Assert.That(onEnter.ContextObject, Is.SameAs(owner));
            Assert.That(onExit.ContextObject, Is.SameAs(owner));
            Assert.That(onEnter.ExecutionCount, Is.EqualTo(1));
            Assert.That(onExit.ExecutionCount, Is.EqualTo(1));
            Assert.That(owner.activeSelf, Is.False);
            Assert.That(machine.CurrentState, Is.Null);
            Assert.That(machine.PreviousState, Is.SameAs(entered));
            machine.Stop();
            machine.Stop();
            Assert.That(events, Is.EqualTo(stoppedEntry), "Repeated public Stop must not publish another departure.");
            Assert.That(machine.PreviousState, Is.SameAs(entered));

            events.Clear();
            frame = Time.frameCount;
            deadline = Time.realtimeSinceStartupAsDouble + 5d;
            owner.SetActive(true);
            while (AwaitingNativeFrame(frame, deadline)) yield return null;
            Assert.That(GetMachine(host), Is.SameAs(machine));
            Assert.That(machine.CurrentState, Is.SameAs(source));
            Assert.That(events, Is.EqualTo(new[] { "exited -", "entered Source", "changed - Source" }));
            Assert.That(onExit.ExecutionCount, Is.EqualTo(1));
            events.Clear();
            ChangeHostState(host, "Entered");
            Assert.That(events, Is.EqualTo(stoppedEntry));
            Assert.That(onEnter.ExecutionCount, Is.EqualTo(2));
            Assert.That(onExit.ExecutionCount, Is.EqualTo(2));
            Assert.That(onEnter.ContextObject, Is.SameAs(owner));
            Assert.That(onExit.ContextObject, Is.SameAs(owner));
            Assert.That(owner.activeSelf, Is.False);
            Assert.That(machine.CurrentState, Is.Null);
            Assert.That(machine.PreviousState, Is.SameAs(entered));
        }

        [UnityTest]
        public IEnumerator DefaultWrappers_SharedDurationDataUsesAddressedMachineAnchors(
            [ValueSource(nameof(HostModes))] bool generic)
        {
            yield return new EnterPlayMode();
            Assert.That(Application.isPlaying, Is.True);
            GameObject firstOwner = CreateNativeObject("AuditStateMachineFirstDurationOwner");
            GameObject secondOwner = CreateNativeObject("AuditStateMachineSecondDurationOwner");
            StateMachineData data = CreateNativeAsset<StateMachineData>();
            StateData source = CreateNativeAsset<StateData>();
            StateData target = CreateNativeAsset<StateData>();
            Component firstHost = AddNativeHost(firstOwner, generic);
            Component secondHost = AddNativeHost(secondOwner, generic);
            source.StateName = "Idle";
            target.StateName = "Run";
            data.States = new[] { source, target };
            data.InitialState = source;
            RecordContextAction exit = new();
            source.OnExitActions.Add(exit);
            StateTransition transition = new() { FromStateData = source, ToStateData = target };
            transition.AddPredicate(new StateDurationPredicate { RequiredDuration = 5f });
            data.Transitions.Add(transition);
            ConfigureHost(firstHost, data, Array.Empty<GameObject>());
            ConfigureHost(secondHost, data, Array.Empty<GameObject>());

            int frame = Time.frameCount;
            double deadline = Time.realtimeSinceStartupAsDouble + 5d;
            firstOwner.SetActive(true);
            secondOwner.SetActive(true);
            while (AwaitingNativeFrame(frame, deadline)) yield return null;
            StateMachine<IState> first = GetMachine(firstHost);
            StateMachine<IState> second = GetMachine(secondHost);
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.CurrentState, Is.SameAs(source));
            Assert.That(second.CurrentState, Is.SameAs(source));
            List<string> firstEvents = new();
            List<string> secondEvents = new();
            RecordEvents(first, firstEvents);
            RecordEvents(second, secondEvents);
            Assert.That(first.SetCurrentStateEnterTime(Time.time - 10f), Is.True);
            // A future anchor keeps the negative control pending while real PlayerLoop frames advance.
            Assert.That(second.SetCurrentStateEnterTime(Time.time + 60f), Is.True);
            frame = Time.frameCount;
            deadline = Time.realtimeSinceStartupAsDouble + 5d;
            while (AwaitingNativeFrame(frame, deadline)) yield return null;

            Assert.That(first.CurrentState, Is.SameAs(target));
            Assert.That(second.CurrentState, Is.SameAs(source));
            Assert.That(firstEvents, Is.EqualTo(new[] { "exited Idle", "entered Run", "changed Idle Run" }));
            Assert.That(secondEvents, Is.Empty);
            Assert.That(exit.ExecutionCount, Is.EqualTo(1));
            Assert.That(exit.ContextObject, Is.SameAs(firstOwner));

            firstEvents.Clear();
            firstOwner.SetActive(false);
            Assert.That(first.CurrentState, Is.Null);
            Assert.That(first.PreviousState, Is.SameAs(target));
            Assert.That(firstEvents, Is.EqualTo(new[] { "exited Run", "changed Run -" }));
            firstEvents.Clear();
            frame = Time.frameCount;
            deadline = Time.realtimeSinceStartupAsDouble + 5d;
            firstOwner.SetActive(true);
            while (AwaitingNativeFrame(frame, deadline)) yield return null;
            Assert.That(GetMachine(firstHost), Is.SameAs(first));
            Assert.That(first.CurrentState, Is.SameAs(source));
            Assert.That(second.CurrentState, Is.SameAs(source));
            Assert.That(firstEvents, Is.EqualTo(new[] { "exited -", "entered Idle", "changed - Idle" }));
            firstEvents.Clear();
            Assert.That(first.SetCurrentStateEnterTime(Time.time + 60f), Is.True);
            Assert.That(second.SetCurrentStateEnterTime(Time.time - 10f), Is.True);
            frame = Time.frameCount;
            deadline = Time.realtimeSinceStartupAsDouble + 5d;
            while (AwaitingNativeFrame(frame, deadline)) yield return null;
            Assert.That(first.CurrentState, Is.SameAs(source));
            Assert.That(firstEvents, Is.Empty);
            Assert.That(second.CurrentState, Is.SameAs(target));
            Assert.That(secondEvents, Is.EqualTo(new[] { "exited Idle", "entered Run", "changed Idle Run" }));
            Assert.That(exit.ExecutionCount, Is.EqualTo(2));
            Assert.That(exit.ContextObject, Is.SameAs(secondOwner));
        }

        [UnityTest]
        public IEnumerator LifecycleExit_UsesOwnerAndOverrideContext(
            [ValueSource(nameof(HostModes))] bool generic, [ValueSource(nameof(ExitModes))] string lifecycle)
        {
            yield return new EnterPlayMode();
            Assert.That(Application.isPlaying, Is.True);
            GameObject owner = CreateNativeObject("AuditStateMachineOwner");
            GameObject target = CreateNativeObject("AuditStateMachineExitTarget", true);
            StateMachineData data = CreateNativeAsset<StateMachineData>();
            StateData exiting = CreateNativeAsset<StateData>();
            StateData initial = CreateNativeAsset<StateData>();
            Component host = AddNativeHost(owner, generic);
            exiting.StateName = "Exiting";
            initial.StateName = "Initial";
            data.States = new[] { exiting, initial };
            data.InitialState = initial;
            ConfigureHost(host, data, new[] { target });
            RecordContextAction record = new();
            exiting.OnExitActions.Add(record);
            exiting.OnExitActions.Add(new SetContextGameObjectActiveAction
            {
                ContextSlot = ConditionContextSlot.Override1,
                SetActive = false
            });
            int frame = Time.frameCount;
            double deadline = Time.realtimeSinceStartupAsDouble + 5d;
            owner.SetActive(true);
            while (AwaitingNativeFrame(frame, deadline)) yield return null;
            StateMachine<IState> machine = GetMachine(host);
            Assert.That(machine.CurrentState, Is.SameAs(initial));
            Assert.That(record.ExecutionCount, Is.Zero);
            ChangeHostState(host, "Exiting");
            Assert.That(machine.CurrentState, Is.SameAs(exiting));
            Assert.That(target.activeSelf, Is.True);
            List<string> events = new();
            RecordEvents(machine, events);

            if (lifecycle == "Reload")
            {
                if (host is StateMachineBehaviourBase untyped) untyped.ReloadFromStateMachineData();
                else ((RuntimeGenericHost)host).LoadFromStateMachineData();
            }
            else
            {
                frame = Time.frameCount;
                deadline = Time.realtimeSinceStartupAsDouble + 5d;
                if (lifecycle == "OnDisable") owner.SetActive(false);
                else UnityEngine.Object.Destroy(owner);
                while (AwaitingNativeFrame(frame, deadline)) yield return null;
            }

            Assert.That(record.ContextObject, Is.SameAs(owner));
            Assert.That(record.ExecutionCount, Is.EqualTo(1));
            Assert.That(target.activeSelf, Is.False);
            Assert.That(machine.CurrentState, Is.SameAs(lifecycle == "Reload" ? initial : null));
            Assert.That(events, Is.EqualTo(lifecycle == "Reload"
                ? new[] { "exited Exiting", "changed Exiting -", "exited -", "entered Initial", "changed - Initial" }
                : new[] { "exited Exiting", "changed Exiting -" }));
            if (lifecycle == "OnDestroy") Assert.That(owner == null, Is.True);
            if (lifecycle == "OnDisable")
            {
                events.Clear();
                frame = Time.frameCount;
                deadline = Time.realtimeSinceStartupAsDouble + 5d;
                owner.SetActive(true);
                while (AwaitingNativeFrame(frame, deadline)) yield return null;
                Assert.That(GetMachine(host), Is.SameAs(machine));
                Assert.That(machine.CurrentState, Is.SameAs(initial));
                Assert.That(events, Is.EqualTo(new[] { "exited -", "entered Initial", "changed - Initial" }));
                Assert.That(record.ExecutionCount, Is.EqualTo(1));
                ChangeHostState(host, "Exiting");
                events.Clear();
                owner.SetActive(false);
                Assert.That(machine.CurrentState, Is.Null);
                Assert.That(events, Is.EqualTo(new[] { "exited Exiting", "changed Exiting -" }));
                Assert.That(record.ExecutionCount, Is.EqualTo(2));
                Assert.That(record.ContextObject, Is.SameAs(owner));
                Assert.That(target.activeSelf, Is.False);
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void MissingOverride_DoesNotEvaluateAgainstOwner(int missingKind)
        {
            // EditMode public API contract; this does not claim native lifecycle execution.
            GameObject owner = new("AuditStateMachineConditionOwner");
            GameObject destroyed = missingKind == 2 ? new GameObject("AuditDestroyedOverride") : null;
            StateData initial = ScriptableObject.CreateInstance<StateData>();
            StateData destination = ScriptableObject.CreateInstance<StateData>();
            try
            {
                StateMachineBehaviourBase host = owner.AddComponent<StateMachineBehaviourBase>();
                ConfigureHost(host, null, missingKind == 0 ? Array.Empty<GameObject>() : new[] { destroyed });
                if (destroyed != null)
                {
                    UnityEngine.Object.DestroyImmediate(destroyed);
                }

                host.RegisterTransition(CreateActiveConditionTransition(initial, destination,
                    ConditionContextSlot.Override1));
                host.StateMachine.ChangeState(initial);
                Assert.That(owner.activeSelf, Is.True, "The owner would satisfy the incorrectly substituted condition.");

                host.EvaluateTransitionsNow();

                Assert.That(host.CurrentState, Is.SameAs(initial));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
                if (destroyed != null)
                {
                    UnityEngine.Object.DestroyImmediate(destroyed);
                }

                UnityEngine.Object.DestroyImmediate(initial);
                UnityEngine.Object.DestroyImmediate(destination);
            }
        }

        [TestCase(ConditionContextSlot.Owner, true)]
        [TestCase(ConditionContextSlot.Override1, true)]
        [TestCase(ConditionContextSlot.Override1, false)]
        [TestCase((ConditionContextSlot)(-1), true)]
        public void ConditionSlot_UsesOnlySelectedContext(ConditionContextSlot slot, bool targetActive)
        {
            // EditMode public API contract; this does not claim native lifecycle execution.
            GameObject owner = new("AuditStateMachineSlotOwner");
            GameObject target = new("AuditStateMachineSlotTarget");
            StateData initial = ScriptableObject.CreateInstance<StateData>();
            StateData destination = ScriptableObject.CreateInstance<StateData>();
            try
            {
                StateMachineBehaviourBase host = owner.AddComponent<StateMachineBehaviourBase>();
                target.SetActive(targetActive);
                ConfigureHost(host, null, new[] { target });
                host.RegisterTransition(CreateActiveConditionTransition(initial, destination, slot));
                host.StateMachine.ChangeState(initial);

                host.EvaluateTransitionsNow();

                bool shouldChange = slot == ConditionContextSlot.Owner ||
                    slot == ConditionContextSlot.Override1 && targetActive;
                Assert.That(host.CurrentState, Is.SameAs(shouldChange ? destination : initial));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(initial);
                UnityEngine.Object.DestroyImmediate(destination);
            }
        }

        private static StateTransition CreateActiveConditionTransition(StateData source, StateData target,
            ConditionContextSlot slot)
        {
            StateTransition transition = new() { FromStateData = source, ToStateData = target };
            transition.AddPredicate(new ConditionEntryPredicate
            {
                ContextSlot = slot,
                ConditionEntry = new ConditionEntry
                {
                    Source = SourceMode.GameObject,
                    PropertyName = nameof(GameObject.activeSelf),
                    CurrentValueType = Neo.Condition.ValueType.Bool,
                    Compare = CompareOp.Equal,
                    ThresholdBool = true
                }
            });
            return transition;
        }

        private static StateMachine<IState> GetMachine(Component host)
        {
            return host is StateMachineBehaviourBase untyped
                ? untyped.StateMachine
                : ((RuntimeGenericHost)host).StateMachine;
        }

        private GameObject CreateNativeObject(string name, bool active = false)
        {
            GameObject owner = new(name);
            nativeObjects.Add(owner);
            owner.SetActive(active);
            return owner;
        }

        private T CreateNativeAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            nativeAssets.Add(asset);
            return asset;
        }

        private static Component AddNativeHost(GameObject owner, bool generic)
        {
            Component host = generic
                ? owner.AddComponent<RuntimeGenericHost>()
                : owner.AddComponent<StateMachineBehaviourBase>();
            Assert.That(host, Is.Not.Null, "The relocated runtime host must be admitted before configuration.");
            return host;
        }

        private static void ChangeHostState(Component host, string stateName)
        {
            if (host is StateMachineBehaviourBase untyped) untyped.ChangeState(stateName);
            else ((RuntimeGenericHost)host).ChangeState(stateName);
        }

        private static bool AwaitingNativeFrame(int previousFrame, double deadline)
        {
            Assert.That(Application.isPlaying, Is.True);
            // Leave a complete PlayerLoop frame before asserting Update or deferred destruction effects.
            if ((long)Time.frameCount - previousFrame >= 2L) return false;
            Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThan(deadline),
                "The real PlayerLoop did not advance within the five-second fixture deadline.");
            return true;
        }

        private static void RecordEvents(StateMachine<IState> machine, List<string> events)
        {
            machine.OnStateEntered.AddListener(state => events.Add("entered " + StateName(state)));
            machine.OnStateExited.AddListener(state => events.Add("exited " + StateName(state)));
            machine.OnStateChanged.AddListener((previous, current) =>
                events.Add("changed " + StateName(previous) + " " + StateName(current)));
        }

        private static string StateName(IState state)
        {
            return state is StateData data ? data.StateName : state == null ? "-" : state.GetType().Name;
        }

        [UnityTearDown]
        public IEnumerator CleanupNativeObjectsAndExitPlayMode()
        {
            // Cleanup only: main assertions observe deferred native destruction through a real frame above.
            // Owners leave their states while their StateData/actions and context overrides are still alive.
            for (int i = 0; i < nativeObjects.Count; i++)
            {
                if (nativeObjects[i] != null) UnityEngine.Object.DestroyImmediate(nativeObjects[i]);
            }

            nativeObjects.Clear();
            for (int i = nativeAssets.Count - 1; i >= 0; i--)
            {
                if (nativeAssets[i] != null) UnityEngine.Object.DestroyImmediate(nativeAssets[i]);
            }

            nativeAssets.Clear();
            if (Application.isPlaying) yield return new ExitPlayMode();
        }

        private static void ConfigureHost(Component host, StateMachineData data, GameObject[] overrides)
        {
            using (SerializedObject serialized = new(host))
            {
                serialized.FindProperty("stateMachineData").objectReferenceValue = data;
                SerializedProperty contexts = serialized.FindProperty("contextOverrides");
                contexts.arraySize = overrides.Length;
                for (int i = 0; i < overrides.Length; i++)
                {
                    contexts.GetArrayElementAtIndex(i).objectReferenceValue = overrides[i];
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private sealed class RecordContextAction : StateAction
        {
            public GameObject ContextObject { get; private set; }
            public int ExecutionCount { get; private set; }

            public override void Execute() { }

            public override void Execute(GameObject contextObject)
            {
                ContextObject = contextObject;
                ExecutionCount++;
            }
        }
    }
}
