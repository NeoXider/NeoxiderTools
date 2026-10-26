using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.StateMachine;
using Neo.StateMachine.NoCode;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    public class Audit20261023NeoStateMachineEditorTests
    {
        private const string EditorNamespace = "Neo.StateMachine.NoCode.Editor.";
        private readonly List<Object> createdObjects = new();

        [Serializable]
        public sealed class ThrowingAction : StateAction
        {
            public ThrowingAction()
            {
                throw new InvalidOperationException("Audit action constructor failed");
            }

            public override void Execute()
            {
            }
        }

        public sealed class NonSerializableAction : StateAction
        {
            public override void Execute()
            {
            }
        }

        [Serializable]
        public sealed class ConfigurablePredicate : StatePredicate
        {
            [SerializeField] private bool accepts;

            protected override bool EvaluateInternal(IState currentState)
            {
                return accepts;
            }
        }

        [SetUp]
        public void BeginUndoGroup()
        {
            Undo.IncrementCurrentGroup();
        }

        [TearDown]
        public void DestroyCreatedObjects()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                Object createdObject = createdObjects[i];
                if (createdObject != null)
                {
                    Undo.ClearUndo(createdObject);
                    Object.DestroyImmediate(createdObject);
                }
            }

            createdObjects.Clear();
        }

        [TestCase("onEnterActions")]
        [TestCase("onUpdateActions")]
        [TestCase("onExitActions")]
        public void AddAction_AppendsIndependentSerializedActionsAndSupportsUndo(string propertyPath)
        {
            StateData state = CreateAsset<StateData>();
            UnityEditor.Editor editor = CreateEditor(state, "StateDataEditor");
            Invoke(editor, "AddAction", propertyPath, typeof(LogStateAction));
            List<StateAction> actions = GetActions(state, propertyPath);
            Assert.That(actions.Count, Is.EqualTo(1));
            ((LogStateAction)actions[0]).Message = "Keep the first action";

            Undo.IncrementCurrentGroup();
            Invoke(editor, "AddAction", propertyPath, typeof(LogStateAction));

            actions = GetActions(state, propertyPath);
            Assert.That(actions.Count, Is.EqualTo(2));
            Assert.That(actions[1], Is.Not.SameAs(actions[0]));
            Assert.That(((LogStateAction)actions[0]).Message, Is.EqualTo("Keep the first action"));
            Assert.That(((LogStateAction)actions[1]).Message, Is.EqualTo("State Action Executed"));

            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.That(GetActions(state, propertyPath).Count, Is.EqualTo(1));
            Assert.That(((LogStateAction)GetActions(state, propertyPath)[0]).Message,
                Is.EqualTo("Keep the first action"));
        }

        [Test]
        public void AddAction_ConstructorFailureLeavesExistingArrayUnchanged()
        {
            StateData state = CreateAsset<StateData>();
            state.OnEnterActions.Add(new LogStateAction { Message = "Existing" });
            UnityEditor.Editor editor = CreateEditor(state, "StateDataEditor");
            LogAssert.Expect(LogType.Error,
                "[StateDataEditor] Cannot create ThrowingAction: Audit action constructor failed");

            Invoke(editor, "AddAction", "onEnterActions", typeof(ThrowingAction));
            editor.serializedObject.ApplyModifiedProperties();

            Assert.That(state.OnEnterActions.Count, Is.EqualTo(1));
            Assert.That(((LogStateAction)state.OnEnterActions[0]).Message, Is.EqualTo("Existing"));

            Invoke(editor, "AddAction", "onEnterActions", typeof(LogStateAction));
            Assert.That(state.OnEnterActions.Count, Is.EqualTo(2));
            Assert.That(((LogStateAction)state.OnEnterActions[0]).Message, Is.EqualTo("Existing"));
            Assert.That(((LogStateAction)state.OnEnterActions[1]).Message, Is.EqualTo("State Action Executed"));
        }

        [Test]
        public void AddAction_NonSerializableTypeIsNotAddedOrOfferedInTheMenu()
        {
            StateData state = CreateAsset<StateData>();
            UnityEditor.Editor editor = CreateEditor(state, "StateDataEditor");
            Invoke(editor, "AddAction", "onEnterActions", typeof(NonSerializableAction));

            Assert.That(state.OnEnterActions, Is.Empty);
            FieldInfo actionTypesField = editor.GetType().GetField("actionTypes",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(actionTypesField, Is.Not.Null);
            Type[] actionTypes = (Type[])actionTypesField.GetValue(editor);
            CollectionAssert.DoesNotContain(actionTypes, typeof(NonSerializableAction));
            Assert.That(actionTypes, Does.Contain(typeof(LogStateAction)));
        }

        [Test]
        public void AddPredicate_IsSerializedAndCanBeUndoneAndRedone()
        {
            StateMachineData owner = CreateAsset<StateMachineData>();
            owner.Transitions.Add(new StateTransition());
            EditorWindow window = CreateTransitionWindow(owner.Transitions[0], owner);

            Invoke(window, "AddPredicate", typeof(BoolPredicate));

            Assert.That(owner.Transitions[0].Predicates.Count, Is.EqualTo(1));
            Assert.That(owner.Transitions[0].Predicates[0], Is.TypeOf<BoolPredicate>());
            Assert.That(owner.Transitions[0].Predicates[0].PredicateName, Is.EqualTo(nameof(BoolPredicate)));
            Assert.That(EditorUtility.IsDirty(owner), Is.True);
            StateMachineData restored = CreateAsset<StateMachineData>();
            EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(owner), restored);
            Assert.That(restored.Transitions[0].Predicates[0], Is.TypeOf<BoolPredicate>());

            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.That(owner.Transitions[0].Predicates, Is.Empty);
            Undo.PerformRedo();
            Assert.That(owner.Transitions[0].Predicates.Count, Is.EqualTo(1));
            Assert.That(owner.Transitions[0].Predicates[0].PredicateName, Is.EqualTo(nameof(BoolPredicate)));
        }

        [Test]
        public void RemovePredicate_RemovesTheSelectedNullEntryAndUndoRestoresIt()
        {
            StateMachineData owner = CreateAsset<StateMachineData>();
            StateTransition selected = new();
            selected.Predicates.Add(null);
            selected.AddPredicate(new BoolPredicate { Value = true });
            owner.Transitions.Add(selected);
            EditorWindow window = CreateTransitionWindow(selected, owner);

            Invoke(window, "RemovePredicateAt", 0);

            Assert.That(owner.Transitions[0].Predicates.Count, Is.EqualTo(1));
            Assert.That(owner.Transitions[0].Evaluate(), Is.True);
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.That(owner.Transitions[0].Predicates.Count, Is.EqualTo(2));
            Assert.That(owner.Transitions[0].Predicates[0], Is.Null);
            Assert.That(owner.Transitions[0].Evaluate(), Is.False);
        }

        [Test]
        public void TransitionDetails_UndoRestoresAllEditedValuesAndKeepsEndpoints()
        {
            StateMachineData owner = CreateAsset<StateMachineData>();
            StateData from = CreateAsset<StateData>();
            StateData to = CreateAsset<StateData>();
            StateTransition selected = new()
            {
                TransitionName = "Original", Priority = 2, FromStateData = from, ToStateData = to
            };
            owner.Transitions.Add(selected);
            EditorWindow window = CreateTransitionWindow(selected, owner);

            Invoke(window, "SetTransitionDetails", "Edited", false, 7);

            Assert.That(owner.Transitions[0].TransitionName, Is.EqualTo("Edited"));
            Assert.That(owner.Transitions[0].IsEnabled, Is.False);
            Assert.That(owner.Transitions[0].Priority, Is.EqualTo(7));
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.That(owner.Transitions[0].TransitionName, Is.EqualTo("Original"));
            Assert.That(owner.Transitions[0].IsEnabled, Is.True);
            Assert.That(owner.Transitions[0].Priority, Is.EqualTo(2));
            Assert.That(owner.Transitions[0].FromStateData, Is.SameAs(from));
            Assert.That(owner.Transitions[0].ToStateData, Is.SameAs(to));
        }

        [TestCase(typeof(BoolPredicate), "value")]
        [TestCase(typeof(ConfigurablePredicate), "accepts")]
        public void PredicateSettings_EditConcreteFieldsAndPersistTheirRuntimeBehavior(Type predicateType,
            string booleanField)
        {
            StateMachineData owner = CreateAsset<StateMachineData>();
            StateTransition selected = new();
            selected.AddPredicate((StatePredicate)Activator.CreateInstance(predicateType));
            owner.Transitions.Add(selected);
            EditorWindow window = CreateTransitionWindow(selected, owner);
            Assert.That(selected.Evaluate(), Is.False);

            using (SerializedObject serializedOwner = new(owner))
            {
                serializedOwner.Update();
                SerializedProperty settings = (SerializedProperty)Invoke(window, "GetPredicateProperty",
                    serializedOwner, 0);
                Assert.That(settings, Is.Not.Null);
                settings.FindPropertyRelative(booleanField).boolValue = true;
                serializedOwner.ApplyModifiedProperties();
            }

            Assert.That(owner.Transitions[0].Evaluate(), Is.True);
            StateMachineData restored = CreateAsset<StateMachineData>();
            EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(owner), restored);
            Assert.That(restored.Transitions[0].Evaluate(), Is.True);
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.That(owner.Transitions[0].Evaluate(), Is.False);
        }

        [Test]
        public void PredicateSettings_FollowTheSelectedTransitionWhenRowsAreReordered()
        {
            StateMachineData owner = CreateAsset<StateMachineData>();
            StateTransition first = new();
            first.AddPredicate(new BoolPredicate());
            StateTransition selected = new();
            selected.AddPredicate(new IntComparisonPredicate());
            owner.Transitions.Add(first);
            owner.Transitions.Add(selected);
            EditorWindow window = CreateTransitionWindow(selected, owner);
            owner.Transitions.Reverse();

            using (SerializedObject serializedOwner = new(owner))
            {
                serializedOwner.Update();
                SerializedProperty settings = (SerializedProperty)Invoke(window, "GetPredicateProperty",
                    serializedOwner, 0);
                Assert.That(settings, Is.Not.Null);
                settings.FindPropertyRelative("value").intValue = 4;
                settings.FindPropertyRelative("threshold").intValue = 3;
                serializedOwner.ApplyModifiedProperties();
            }

            Assert.That(owner.Transitions[0].Evaluate(), Is.True);
            Assert.That(owner.Transitions[1].Evaluate(), Is.False);
        }

        [Test]
        public void ReplacedTransition_DoesNotReceiveEditsThroughAStaleWindow()
        {
            StateMachineData owner = CreateAsset<StateMachineData>();
            StateTransition detached = new();
            owner.Transitions.Add(detached);
            EditorWindow window = CreateTransitionWindow(detached, owner);
            owner.Transitions[0] = new StateTransition { TransitionName = "Replacement" };
            Invoke(window, "OnUndoRedo");

            Invoke(window, "AddPredicate", typeof(BoolPredicate));
            Invoke(window, "SetTransitionDetails", "Wrong target", false, 99);

            Assert.That(owner.Transitions[0].TransitionName, Is.EqualTo("Replacement"));
            Assert.That(owner.Transitions[0].Predicates, Is.Empty);
            Assert.That(detached.Predicates, Is.Empty);
        }

        [Test]
        public void DestroyedOwner_DoesNotTurnAnAssetWindowIntoAnOwnerlessEditor()
        {
            StateMachineData owner = CreateAsset<StateMachineData>();
            StateTransition selected = new() { TransitionName = "Original" };
            owner.Transitions.Add(selected);
            EditorWindow window = CreateTransitionWindow(selected, owner);
            Object.DestroyImmediate(owner);

            Invoke(window, "AddPredicate", typeof(BoolPredicate));
            Invoke(window, "SetTransitionDetails", "Late edit", false, 9);

            Assert.That(selected.TransitionName, Is.EqualTo("Original"));
            Assert.That(selected.Predicates, Is.Empty);
        }

        [Test]
        public void ClosedWindow_DoesNotApplyALateMenuCommand()
        {
            StateMachineData owner = CreateAsset<StateMachineData>();
            owner.Transitions.Add(new StateTransition());
            EditorWindow window = CreateTransitionWindow(owner.Transitions[0], owner);
            Object.DestroyImmediate(window);

            Invoke(window, "AddPredicate", typeof(BoolPredicate));

            Assert.That(owner.Transitions[0].Predicates, Is.Empty);
        }

        [Test]
        public void Validation_ReportsFailureWhenAnotherSelectedConfigurationIsInvalid()
        {
            StateMachineData valid = CreateAsset<StateMachineData>();
            StateData state = CreateAsset<StateData>();
            valid.States = new[] { state };
            valid.InitialState = state;
            StateMachineData invalid = CreateAsset<StateMachineData>();
            UnityEditor.Editor editor = CreateEditor(new Object[] { valid, invalid }, "StateMachineDataEditor");

            Assert.That(Invoke(editor, "ValidateSelectedConfigurations", true), Is.False);
            invalid.States = new[] { state };
            invalid.InitialState = state;
            Assert.That(Invoke(editor, "ValidateSelectedConfigurations", true), Is.True);
        }

        private T CreateAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            createdObjects.Add(asset);
            return asset;
        }

        private UnityEditor.Editor CreateEditor(Object target, string typeName)
        {
            return CreateEditor(new[] { target }, typeName);
        }

        private UnityEditor.Editor CreateEditor(Object[] targets, string typeName)
        {
            UnityEditor.Editor editor = UnityEditor.Editor.CreateEditor(targets, GetEditorType(typeName));
            createdObjects.Add(editor);
            return editor;
        }

        private EditorWindow CreateTransitionWindow(StateTransition selected, StateMachineData owner)
        {
            EditorWindow window = (EditorWindow)ScriptableObject.CreateInstance(GetEditorType("TransitionEditorWindow"));
            createdObjects.Add(window);
            Invoke(window, "SetTransition", selected, owner);
            return window;
        }

        private static Type GetEditorType(string typeName)
        {
            // The shared test asmdef has no Neo.StateMachine.Editor reference. Keep it unchanged.
            return Type.GetType(EditorNamespace + typeName + ", Neo.StateMachine.Editor", true);
        }

        private static object Invoke(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            return method.Invoke(target, arguments);
        }

        private static List<StateAction> GetActions(StateData state, string propertyPath)
        {
            return propertyPath switch
            {
                "onEnterActions" => state.OnEnterActions,
                "onUpdateActions" => state.OnUpdateActions,
                "onExitActions" => state.OnExitActions,
                _ => throw new ArgumentOutOfRangeException(nameof(propertyPath))
            };
        }
    }
}
