using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Editor.Quest;
using Neo.Editor.Windows;
using Neo.Quest;
using Neo.StateMachine.NoCode;
using Neo.UI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;
using UIImage = UnityEngine.UI.Image;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Repaint caches of the module inspectors brought onto the shared editor patterns: summary badge arrays and
    ///     their texts are rebuilt only when a value changes, the StateData action-type scan runs once per domain
    ///     reload, the Scene Saver's per-save console line is opt-in; plus the mascot click that opens the component's
    ///     page, the English UI Mesh Rig menu messages and the lobby scene's input module.
    /// </summary>
    public sealed class EditorRepaintCacheTests
    {
        private const string StateMachineEditorNamespace = "Neo.StateMachine.NoCode.Editor.";

        [NeoDoc("Editor/CustomEditorBase.md")]
        private sealed class MascotLinkedProbe : ScriptableObject
        {
        }

        private sealed class MascotUnlinkedProbe : ScriptableObject
        {
        }

        private sealed class MascotProbeEditor : CustomEditorBase
        {
            protected override void ProcessAttributeAssignments()
            {
            }
        }

        [Test]
        public void QuestFlowConfigEditor_CollectsStructureInOnePassAndReusesItsBadges()
        {
            QuestFlowConfig config = ScriptableObject.CreateInstance<QuestFlowConfig>();
            QuestConfig quest = ScriptableObject.CreateInstance<QuestConfig>();
            QuestFlowConfigEditor editor = null;
            try
            {
                using SerializedObject serialized = new(config);
                SerializedProperty chains = serialized.FindProperty("_chains");
                SerializedProperty standalone = serialized.FindProperty("_standaloneQuests");
                chains.arraySize = 1;
                chains.GetArrayElementAtIndex(0).FindPropertyRelative("_strictOrder").boolValue = true;
                chains.GetArrayElementAtIndex(0).FindPropertyRelative("_quests").arraySize = 0;
                standalone.arraySize = 2;
                standalone.GetArrayElementAtIndex(0).objectReferenceValue = quest;
                standalone.GetArrayElementAtIndex(1).objectReferenceValue = quest;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                serialized.Update();

                QuestFlowConfigEditor.FlowStats stats = QuestFlowConfigEditor.CollectStats(chains, standalone);

                Assert.That(stats.Chains, Is.EqualTo(1));
                Assert.That(stats.StrictChains, Is.EqualTo(1));
                Assert.That(stats.Standalone, Is.EqualTo(2));
                Assert.That(stats.QuestRefs, Is.EqualTo(2));
                Assert.That(stats.DuplicateRefs, Is.EqualTo(1));
                Assert.That(stats.HasEmptyChain, Is.True);

                editor = (QuestFlowConfigEditor)UnityEditor.Editor.CreateEditor(config, typeof(QuestFlowConfigEditor));
                NeoxiderEditorGUI.Badge[] first = editor.GetSummaryBadges(stats);
                NeoxiderEditorGUI.Badge[] second = editor.GetSummaryBadges(stats);
                Assert.That(second, Is.SameAs(first), "An unchanged config must not rebuild its badges.");
                Assert.That(first.Length, Is.EqualTo(5));
                Assert.That(first[4].Text, Is.EqualTo("Duplicates 1"));

                QuestFlowConfigEditor.FlowStats clean = stats;
                clean.DuplicateRefs = 0;
                NeoxiderEditorGUI.Badge[] rebuilt = editor.GetSummaryBadges(clean);
                Assert.That(rebuilt, Is.Not.SameAs(first));
                Assert.That(rebuilt.Length, Is.EqualTo(4), "Without duplicates there is no Duplicates badge.");
            }
            finally
            {
                if (editor != null)
                {
                    Object.DestroyImmediate(editor);
                }

                Object.DestroyImmediate(quest);
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void StateMachineDataEditor_ReusesItsBadgesAndRowTexts()
        {
            StateMachineData data = ScriptableObject.CreateInstance<StateMachineData>();
            UnityEditor.Editor editor = UnityEditor.Editor.CreateEditor(data,
                GetStateMachineEditorType("StateMachineDataEditor"));
            try
            {
                MethodInfo badges = editor.GetType().GetMethod("GetSummaryBadges",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(badges, Is.Not.Null);
                NeoxiderEditorGUI.Badge[] first =
                    (NeoxiderEditorGUI.Badge[])badges.Invoke(editor, new object[] { 2, 1, true, false });
                NeoxiderEditorGUI.Badge[] second =
                    (NeoxiderEditorGUI.Badge[])badges.Invoke(editor, new object[] { 2, 1, true, false });
                Assert.That(second, Is.SameAs(first));
                Assert.That(first.Length, Is.EqualTo(3));
                Assert.That(first[0].Text, Is.EqualTo("States 2"));

                NeoxiderEditorGUI.Badge[] legacy =
                    (NeoxiderEditorGUI.Badge[])badges.Invoke(editor, new object[] { 2, 1, false, true });
                Assert.That(legacy, Is.Not.SameAs(first));
                Assert.That(legacy.Length, Is.EqualTo(4));
                Assert.That(legacy[2].Text, Is.EqualTo("Initial Missing"));

                MethodInfo title = editor.GetType().GetMethod("GetTransitionTitle",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(title, Is.Not.Null);
                string titleText = (string)title.Invoke(null, new object[] { 3 });
                Assert.That(titleText, Is.EqualTo("Transition 4"));
                Assert.That(title.Invoke(null, new object[] { 3 }), Is.SameAs(titleText));
            }
            finally
            {
                Object.DestroyImmediate(editor);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void StateDataEditor_SharesOneActionTypeScanAcrossInspectors()
        {
            StateData first = ScriptableObject.CreateInstance<StateData>();
            StateData second = ScriptableObject.CreateInstance<StateData>();
            Type editorType = GetStateMachineEditorType("StateDataEditor");
            UnityEditor.Editor firstEditor = UnityEditor.Editor.CreateEditor(first, editorType);
            UnityEditor.Editor secondEditor = UnityEditor.Editor.CreateEditor(second, editorType);
            try
            {
                FieldInfo actionTypes = editorType.GetField("actionTypes",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(actionTypes, Is.Not.Null);
                Type[] firstTypes = (Type[])actionTypes.GetValue(firstEditor);
                Assert.That(firstTypes, Is.Not.Empty);
                Assert.That(actionTypes.GetValue(secondEditor), Is.SameAs(firstTypes),
                    "A second inspector must reuse the cached scan instead of running TypeCache + LINQ again.");

                MethodInfo shortName = editorType.GetMethod("ToShortTypeName",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(shortName, Is.Not.Null);
                Assert.That(shortName.Invoke(null, new object[] { "Neo.StateMachine Neo.StateMachine.NoCode.Foo" }),
                    Is.EqualTo("Foo"));
                Assert.That(shortName.Invoke(null, new object[] { "Malformed" }), Is.EqualTo("Action"));
            }
            finally
            {
                Object.DestroyImmediate(firstEditor);
                Object.DestroyImmediate(secondEditor);
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void SceneSaver_LogEachSave_IsOffByDefaultAndPersisted()
        {
            bool hadKey = EditorPrefs.HasKey(SceneSaverGUI.LogEachSaveKey);
            bool previous = EditorPrefs.GetBool(SceneSaverGUI.LogEachSaveKey, false);
            try
            {
                SceneSaverGUI.ResetLogEachSave();
                Assert.That(SceneSaverGUI.LogEachSave, Is.False, "The per-save console line must be opt-in.");
                Assert.That(EditorPrefs.HasKey(SceneSaverGUI.LogEachSaveKey), Is.False);

                SceneSaverGUI.LogEachSave = true;

                Assert.That(SceneSaverGUI.LogEachSave, Is.True);
                Assert.That(EditorPrefs.GetBool(SceneSaverGUI.LogEachSaveKey, false), Is.True);
            }
            finally
            {
                if (hadKey)
                {
                    SceneSaverGUI.LogEachSave = previous;
                    EditorPrefs.SetBool(SceneSaverGUI.LogEachSaveKey, previous);
                }
                else
                {
                    SceneSaverGUI.ResetLogEachSave();
                }
            }
        }

        [Test]
        public void SceneSaver_SuccessfulBackupIsLoggedOnlyWithLogEachSaveButAFailureAlways()
        {
            const string backupPath = "Assets/Scenes/AutoSaves/Probe/Probe_AutoSave.unity";
            bool hadKey = EditorPrefs.HasKey(SceneSaverGUI.LogEachSaveKey);
            bool previous = EditorPrefs.GetBool(SceneSaverGUI.LogEachSaveKey, false);
            List<string> lines = new();
            Application.LogCallback capture = (condition, stackTrace, type) =>
            {
                if (condition.StartsWith("[SceneSaver]", StringComparison.Ordinal))
                {
                    lines.Add(type + ": " + condition);
                }
            };
            Application.logMessageReceived += capture;
            try
            {
                SceneSaverGUI gui = new(new SceneSaverSettings());
                SceneSaverGUI.LogEachSave = false;

                gui.ReportBackupResult(true, backupPath);

                Assert.That(lines, Is.Empty, "With Log Each Auto-Save off a successful backup writes no console line.");
                Assert.That(gui.LastSaveStatus, Is.EqualTo("Auto-saved: " + backupPath));

                gui.ReportBackupResult(false, backupPath);

                Assert.That(lines, Is.EqualTo(new[] { "Warning: [SceneSaver] Failed to save scene to " + backupPath }),
                    "A failed backup is reported whatever the toggle says.");
                Assert.That(gui.LastSaveStatus, Is.EqualTo("Auto-save failed."));

                lines.Clear();
                SceneSaverGUI.LogEachSave = true;

                gui.ReportBackupResult(true, backupPath);

                Assert.That(lines, Is.EqualTo(new[] { "Log: [SceneSaver] Auto-saved: " + backupPath }));
            }
            finally
            {
                Application.logMessageReceived -= capture;
                if (hadKey)
                {
                    SceneSaverGUI.LogEachSave = previous;
                    EditorPrefs.SetBool(SceneSaverGUI.LogEachSaveKey, previous);
                }
                else
                {
                    SceneSaverGUI.ResetLogEachSave();
                }
            }
        }

        [Test]
        public void SceneSaver_SummaryBadges_AreRebuiltOnlyWhenAValueChanges()
        {
            SceneSaverGUI gui = new(new SceneSaverSettings());

            NeoxiderEditorGUI.Badge[] first = gui.GetSummaryBadges(true, 3f, false);
            NeoxiderEditorGUI.Badge[] second = gui.GetSummaryBadges(true, 3f, false);
            NeoxiderEditorGUI.Badge[] changed = gui.GetSummaryBadges(true, 5f, false);

            Assert.That(second, Is.SameAs(first));
            Assert.That(changed, Is.Not.SameAs(first));
            Assert.That(changed[1].Text, Is.EqualTo("Interval 5 min"));
        }

        [Test]
        public void UIMeshRigSpriteRendererEditor_CachesItsNativeSizeAndDrawModeTexts()
        {
            UIMeshRigSpriteRendererEditor editor = ScriptableObject.CreateInstance<UIMeshRigSpriteRendererEditor>();
            try
            {
                string size = editor.GetNativeSizeText(new Vector2(1f, 2f));
                Assert.That(editor.GetNativeSizeText(new Vector2(1f, 2f)), Is.SameAs(size));
                Assert.That(size, Does.EndWith(" world units"));
                Assert.That(editor.GetNativeSizeText(new Vector2(3f, 2f)), Is.Not.EqualTo(size));

                string warning = editor.GetDrawModeWarning(SpriteDrawMode.Sliced);
                Assert.That(editor.GetDrawModeWarning(SpriteDrawMode.Sliced), Is.SameAs(warning));
                Assert.That(warning, Does.Contain("Sliced"));
                Assert.That(editor.GetDrawModeWarning(SpriteDrawMode.Tiled), Does.Contain("Tiled"));
            }
            finally
            {
                Object.DestroyImmediate(editor);
            }
        }

        [Test]
        public void UIMeshRigMenu_ConsoleMessagesAreEnglish()
        {
            string[] messages =
            {
                UIMeshRigMenu.ConvertedNotice,
                UIMeshRigMenu.GetConvertRefusedMessage(UIImage.Type.Sliced),
                UIMeshRigMenu.GetChildRefusedMessage(UIImage.Type.Tiled)
            };

            foreach (string message in messages)
            {
                Assert.That(message, Does.StartWith("[UI Mesh Rig] "));
                foreach (char character in message)
                {
                    Assert.That(character < 0x0400 || character > 0x04FF, Is.True,
                        "Library console messages are English: " + message);
                }
            }

            Assert.That(UIMeshRigMenu.GetConvertRefusedMessage(UIImage.Type.Sliced), Does.Contain("Sliced"));
        }

        [Test]
        public void MascotClick_TargetsTheDocumentationSectionPageOrNothing()
        {
            MascotLinkedProbe linked = ScriptableObject.CreateInstance<MascotLinkedProbe>();
            MascotUnlinkedProbe unlinked = ScriptableObject.CreateInstance<MascotUnlinkedProbe>();
            MascotProbeEditor linkedEditor =
                (MascotProbeEditor)UnityEditor.Editor.CreateEditor(linked, typeof(MascotProbeEditor));
            MascotProbeEditor unlinkedEditor =
                (MascotProbeEditor)UnityEditor.Editor.CreateEditor(unlinked, typeof(MascotProbeEditor));
            try
            {
                string linkedPath = linkedEditor.GetMascotDocPath();
                Assert.That(linkedPath, Is.Not.Null.And.EndWith("Docs/Editor/CustomEditorBase.md"),
                    "The mascot must open the page the Documentation section shows.");
                Assert.That(unlinkedEditor.GetMascotDocPath(), Is.Null,
                    "Without a page the mascot click is only a poke.");
            }
            finally
            {
                Object.DestroyImmediate(linkedEditor);
                Object.DestroyImmediate(unlinkedEditor);
                Object.DestroyImmediate(linked);
                Object.DestroyImmediate(unlinked);
            }
        }

        [Test]
        public void MascotClick_OnTheChipQueuesThePageOnce_ElsewhereOrDoubleClickOnlyPokes()
        {
            MascotLinkedProbe linked = ScriptableObject.CreateInstance<MascotLinkedProbe>();
            MascotUnlinkedProbe unlinked = ScriptableObject.CreateInstance<MascotUnlinkedProbe>();
            MascotProbeEditor linkedEditor =
                (MascotProbeEditor)UnityEditor.Editor.CreateEditor(linked, typeof(MascotProbeEditor));
            MascotProbeEditor unlinkedEditor =
                (MascotProbeEditor)UnityEditor.Editor.CreateEditor(unlinked, typeof(MascotProbeEditor));
            Rect chip = new(0f, 0f, 20f, 20f);
            Vector2 onChip = new(10f, 10f);
            Vector2 onTitle = new(60f, 10f);
            try
            {
                string page = linkedEditor.GetMascotDocPath();
                Assert.That(page, Is.Not.Null);

                Assert.That(linkedEditor.TryQueueMascotDocOpen(onTitle, 1, chip), Is.False,
                    "A click on the title or tagline only pokes the slime.");
                Assert.That(linkedEditor.TryQueueMascotDocOpen(onChip, 2, chip), Is.False,
                    "The second click of a double-click must not open the page again.");
                Assert.That(unlinkedEditor.TryQueueMascotDocOpen(onChip, 1, chip), Is.False,
                    "Without a page the mascot click is only a poke.");
                Assert.That(linkedEditor.PendingMascotDocPath, Is.Null);
                Assert.That(unlinkedEditor.PendingMascotDocPath, Is.Null);

                Assert.That(linkedEditor.TryQueueMascotDocOpen(onChip, 1, chip), Is.True);
                Assert.That(linkedEditor.TryQueueMascotDocOpen(onChip, 1, chip), Is.True);

                Assert.That(linkedEditor.PendingMascotDocPath, Is.EqualTo(page));
                Assert.That(CountDelayCallsOf(linkedEditor), Is.EqualTo(1),
                    "Repeated clicks before the next editor tick must open the page once.");

                linkedEditor.CancelPendingMascotDoc();

                Assert.That(linkedEditor.PendingMascotDocPath, Is.Null);
                Assert.That(CountDelayCallsOf(linkedEditor), Is.Zero);
            }
            finally
            {
                linkedEditor.CancelPendingMascotDoc();
                Object.DestroyImmediate(linkedEditor);
                Object.DestroyImmediate(unlinkedEditor);
                Object.DestroyImmediate(linked);
                Object.DestroyImmediate(unlinked);
            }
        }

        [Test]
        public void MascotClick_HeaderMouseDownOnTheChipQueuesThePageAndUsesTheEvent()
        {
            MascotLinkedProbe linked = ScriptableObject.CreateInstance<MascotLinkedProbe>();
            MascotProbeEditor editor =
                (MascotProbeEditor)UnityEditor.Editor.CreateEditor(linked, typeof(MascotProbeEditor));
            Rect header = new(0f, 0f, 200f, 44f);
            Rect chip = new(5f, 5f, 34f, 34f);
            try
            {
                // WHY: Event.current cannot be replaced outside an IMGUI pass in Unity 6.3, so the events go in directly.
                Event titleClick = new()
                {
                    type = EventType.MouseDown, button = 0, clickCount = 1, mousePosition = new Vector2(120f, 20f)
                };
                editor.HandleMascotPoke(titleClick, header, chip);

                Assert.That(titleClick.type, Is.EqualTo(EventType.Used), "A header click is consumed by the poke.");
                Assert.That(editor.PendingMascotDocPath, Is.Null, "Only the chip opens the page.");

                Event chipClick = new()
                {
                    type = EventType.MouseDown, button = 0, clickCount = 1, mousePosition = new Vector2(20f, 20f)
                };
                editor.HandleMascotPoke(chipClick, header, chip);

                Assert.That(chipClick.type, Is.EqualTo(EventType.Used));
                Assert.That(editor.PendingMascotDocPath, Is.EqualTo(editor.GetMascotDocPath()));
            }
            finally
            {
                editor.CancelPendingMascotDoc();
                Object.DestroyImmediate(editor);
                Object.DestroyImmediate(linked);
            }
        }

#if MIRROR
        [Test]
        public void LobbyDemoScene_EventSystemUsesTheInputSystemUIModuleWhenThePackageIsInstalled()
        {
            bool inputSystemInstalled = false;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "Unity.InputSystem")
                {
                    inputSystemInstalled = true;
                    break;
                }
            }

            string expected = inputSystemInstalled
                ? "UnityEngine.InputSystem.UI.InputSystemUIInputModule"
                : typeof(StandaloneInputModule).FullName;

            GameObject eventSystem = LobbyDemoSceneCreator.CreateEventSystem();
            try
            {
                Assert.That(eventSystem.GetComponent<EventSystem>(), Is.Not.Null);
                BaseInputModule[] modules = eventSystem.GetComponents<BaseInputModule>();
                Assert.That(modules, Has.Length.EqualTo(1), "The lobby EventSystem carries exactly one UI module.");
                Assert.That(modules[0].GetType().FullName, Is.EqualTo(expected));
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                Assert.That(modules[0], Is.Not.InstanceOf<StandaloneInputModule>(),
                    "StandaloneInputModule throws every frame in an Input System-only project.");
#endif
            }
            finally
            {
                Object.DestroyImmediate(eventSystem);
            }
        }
#endif

        private static int CountDelayCallsOf(object target)
        {
            int count = 0;
            if (EditorApplication.delayCall == null)
            {
                return count;
            }

            foreach (Delegate call in EditorApplication.delayCall.GetInvocationList())
            {
                if (ReferenceEquals(call.Target, target))
                {
                    count++;
                }
            }

            return count;
        }

        private static Type GetStateMachineEditorType(string typeName)
        {
            // WHY: the shared test asmdef has no Neo.StateMachine.Editor reference (see
            // Audit20261023NeoStateMachineEditorTests); the editors are reached by name.
            return Type.GetType(StateMachineEditorNamespace + typeName + ", Neo.StateMachine.Editor", true);
        }
    }
}
