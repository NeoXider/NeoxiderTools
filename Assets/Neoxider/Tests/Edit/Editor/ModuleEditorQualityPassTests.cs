using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Neo.Abilities;
using Neo.Cards;
using Neo.StateMachine;
using Neo.StateMachine.NoCode;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Guards for the module editors touched by the editor-tools quality pass (Abilities, Cards, State Machine,
    ///     Audio).
    ///     The shared test asmdef does not reference those editor assemblies, so the editors are reached by name
    ///     through reflection; the assertions observe authored data and the state the windows keep.
    /// </summary>
    public sealed class ModuleEditorQualityPassTests
    {
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

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

        [Test]
        public void DeckConfigValidation_IsCachedUntilTheAssetChanges()
        {
            DeckConfig config = Own(ScriptableObject.CreateInstance<DeckConfig>());
            UnityEditor.Editor editor = Own(UnityEditor.Editor.CreateEditor(config));
            Type editorType = editor.GetType();
            Assert.That(editorType.FullName, Is.EqualTo("Neo.Cards.Editor.DeckConfigEditor"));
            MethodInfo refresh = editorType.GetMethod("RefreshValidationIfNeeded", Instance, null,
                new[] { typeof(DeckConfig), typeof(bool) }, null);
            FieldInfo errorsField = editorType.GetField("_validationErrors", Instance);
            FieldInfo staleField = editorType.GetField("_validationStale", Instance);
            Assert.That(refresh, Is.Not.Null);
            Assert.That(errorsField, Is.Not.Null);
            Assert.That(staleField, Is.Not.Null);
            List<string> errors = (List<string>)errorsField.GetValue(editor);
            object[] layoutPass = { config, true };
            object[] repaintPass = { config, false };

            refresh.Invoke(editor, layoutPass);
            Assert.That(errors, Has.Some.Contains("No sprites assigned"),
                "An empty standard deck reports its empty suits.");

            const string sentinel = "ModuleEditorQualityPass cached result";
            errors.Add(sentinel);
            refresh.Invoke(editor, layoutPass);
            Assert.That(errors, Has.Member(sentinel),
                "An unchanged deck must reuse the last result instead of validating again.");

            staleField.SetValue(editor, true);
            refresh.Invoke(editor, repaintPass);
            Assert.That(errors, Has.Member(sentinel),
                "Outside Layout a stale result is kept, so Layout and Repaint draw the same messages.");
            refresh.Invoke(editor, layoutPass);
            Assert.That(errors, Has.No.Member(sentinel), "The next Layout pass replaces the stale result.");

            int dirtyBefore = EditorUtility.GetDirtyCount(config);
            using (SerializedObject serialized = new(config))
            {
                serialized.FindProperty("_useCustomDeck").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(config);
            Assume.That(EditorUtility.GetDirtyCount(config), Is.Not.EqualTo(dirtyBefore),
                "This editor build does not count dirty marks on in-memory assets.");
            refresh.Invoke(editor, layoutPass);
            Assert.That(errors, Has.None.Contains("No sprites assigned"),
                "After the asset changed the cached messages must follow the custom-deck rules.");
        }

        [Test]
        public void TransitionWindow_ReResolvesItsTransitionAfterADomainReload()
        {
            StateMachineData owner = Own(ScriptableObject.CreateInstance<StateMachineData>());
            StateTransition first = new() { TransitionName = "First" };
            StateTransition selected = new() { TransitionName = "Selected" };
            owner.Transitions.Add(first);
            owner.Transitions.Add(selected);

            Type windowType = Type.GetType(
                "Neo.StateMachine.NoCode.Editor.TransitionEditorWindow, Neo.StateMachine.Editor", true);
            EditorWindow window = (EditorWindow)Own(ScriptableObject.CreateInstance(windowType));
            windowType.GetMethod("SetTransition", Instance).Invoke(window, new object[] { selected, owner });

            FieldInfo transitionField = windowType.GetField("transition", Instance);
            Assert.That(transitionField.IsDefined(typeof(NonSerializedAttribute), false), Is.True,
                "An inline transition must not be serialized with the window: it would come back as a detached copy.");
            Assert.That(windowType.GetField("data", Instance).IsDefined(typeof(SerializeField), false), Is.True);
            Assert.That(windowType.GetField("transitionIndex", Instance).IsDefined(typeof(SerializeField), false),
                Is.True);

            transitionField.SetValue(window, null);
            windowType.GetMethod("OnEnable", Instance).Invoke(window, null);

            Assert.That(transitionField.GetValue(window), Is.SameAs(selected),
                "The window must pick its row up again from the owner and the stored index.");

            owner.Transitions.Remove(selected);
            owner.Transitions.Insert(0, selected);
            transitionField.SetValue(window, null);
            windowType.GetMethod("OnEnable", Instance).Invoke(window, null);

            Assert.That(transitionField.GetValue(window), Is.Null,
                "When the stored index now holds another row, the window must not edit it in place.");
        }

        [Test]
        public void StateMachineRegistrar_ChecksTheRegistrationAfterEveryReload()
        {
            Type registrar = Type.GetType(
                "Neo.StateMachine.NoCode.Editor.StateMachineEditorRegistrar, Neo.StateMachine.Editor", true);
            PropertyInfo initialized = registrar.GetProperty("IsInitialized", Static);
            MethodInfo onReady = registrar.GetMethod("OnEditorReady", Static);
            Assert.That(initialized, Is.Not.Null);
            Assert.That(onReady, Is.Not.Null);
            Assert.That(registrar.GetField("ValidatedSessionKey", Static), Is.Null,
                "A recompile is what can break the registration, so no session flag may skip the check.");

            MethodInfo setInitialized = initialized.GetSetMethod(true);
            bool previous = (bool)initialized.GetValue(null);
            try
            {
                setInitialized.Invoke(null, new object[] { false });
                onReady.Invoke(null, null);

                Assert.That(initialized.GetValue(null), Is.EqualTo(true));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                setInitialized.Invoke(null, new object[] { previous || (bool)initialized.GetValue(null) });
            }
        }

        [Test]
        public void AbilityDesigner_RebuildsItsBoardAfterAnEditMadeElsewhere()
        {
            AbilityDefinition ability = Own(ScriptableObject.CreateInstance<AbilityDefinition>());
            ability.name = "ModuleEditorQualityPass_Ability";
            Type windowType = Type.GetType("Neo.Abilities.Editor.AbilityDesignerWindow, Neo.Abilities.Editor", true);
            MethodInfo onFocus = windowType.GetMethod("OnFocus", Instance);
            Assert.That(onFocus, Is.Not.Null);
            EditorWindow window = (EditorWindow)ScriptableObject.CreateInstance(windowType);
            try
            {
                window.Show();
                if (window.rootVisualElement.childCount == 0)
                {
                    windowType.GetMethod("CreateGUI", Instance).Invoke(window, null);
                }

                windowType.GetMethod("SelectAsset", Instance).Invoke(window, new object[] { ability });
                int before = CountEffectCards(window);

                using (SerializedObject elsewhere = new(ability))
                {
                    SerializedProperty impact = elsewhere.FindProperty("_blueprint.ImpactEffects");
                    impact.InsertArrayElementAtIndex(impact.arraySize);
                    impact.GetArrayElementAtIndex(impact.arraySize - 1).boxedValue =
                        new EffectNodeData { OpId = AbilityEffectOps.Damage };
                    elsewhere.ApplyModifiedPropertiesWithoutUndo();
                }

                Assert.That(CountEffectCards(window), Is.EqualTo(before),
                    "Precondition: the board is a snapshot until the window refreshes it.");
                onFocus.Invoke(window, null);
                Assert.That(CountEffectCards(window), Is.EqualTo(before + 1),
                    "Focusing the window after an outside edit must rebuild the cards from the asset.");

                VisualElement firstCard = window.rootVisualElement.Q<VisualElement>(className: "nad-card");
                onFocus.Invoke(window, null);
                Assert.That(window.rootVisualElement.Q<VisualElement>(className: "nad-card"), Is.SameAs(firstCard),
                    "A focus without outside edits keeps the board, and with it any open card.");
            }
            finally
            {
                window.Close();
                Undo.ClearUndo(ability);
            }
        }

        [Test]
        public void AudioIdOptions_ReuseTheArrayForTheSameIdList()
        {
            Type drawerType = Type.GetType("Neo.Audio.Editor.AudioIdDrawer, Neo.Audio.Editor", true);
            object soundCache = drawerType.GetField("SoundIds", Static).GetValue(null);
            Type cacheType = soundCache.GetType();
            List<string> ids = (List<string>)cacheType.GetField("Ids", Instance).GetValue(soundCache);
            IDictionary options = (IDictionary)cacheType.GetField("Options", Instance).GetValue(soundCache);
            MethodInfo getOptions = drawerType.GetMethod("GetCachedOptions", Static);
            Assert.That(getOptions, Is.Not.Null);

            List<string> previousIds = new(ids);
            try
            {
                ids.Clear();
                ids.Add("coin");
                ids.Add("boom");
                options.Clear();

                object[] firstArgs = { ids, "boom", 0 };
                string[] first = (string[])getOptions.Invoke(null, firstArgs);
                object[] secondArgs = { ids, "boom", 0 };
                string[] second = (string[])getOptions.Invoke(null, secondArgs);
                object[] missingArgs = { ids, "retired", 0 };
                string[] missing = (string[])getOptions.Invoke(null, missingArgs);

                Assert.That(first, Is.SameAs(second), "The same id list and value must reuse its labels.");
                Assert.That(firstArgs[2], Is.EqualTo(2));
                Assert.That(secondArgs[2], Is.EqualTo(2));
                Assert.That(missing, Is.Not.SameAs(first));
                Assert.That(missing[(int)missingArgs[2]], Does.Contain("retired"));
            }
            finally
            {
                ids.Clear();
                ids.AddRange(previousIds);
                options.Clear();
            }
        }

        private static int CountEffectCards(EditorWindow window)
        {
            return window.rootVisualElement.Query<VisualElement>(className: "nad-card").ToList().Count;
        }

        private T Own<T>(T obj) where T : Object
        {
            _owned.Add(obj);
            return obj;
        }
    }
}
