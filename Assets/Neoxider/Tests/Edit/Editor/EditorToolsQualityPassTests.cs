using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Editor.Binding;
using Neo.Editor.Condition;
using Neo.Editor.Quest;
using Neo.Quest;
using Neo.Tools;
using Neo.Tools.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Guards for the editor-tools quality pass (Neo.Editor outside the inspector core): cached popup options,
    ///     cached member labels and reflection, validation caching, the package root lookup and window state that
    ///     must survive a domain reload.
    /// </summary>
    public sealed class EditorToolsQualityPassTests
    {
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
            ComponentBindingInspectorShared.ClearPopupOptionsCache();
        }

        [Test]
        public void PopupOptions_ReuseTheArrayWhileTheTextIsUnchanged()
        {
            object owner = new();
            List<string> items = new() { "Alpha", "Beta" };

            string[] first = ComponentBindingInspectorShared.GetPopupOptions(owner, items);
            string[] second = ComponentBindingInspectorShared.GetPopupOptions(owner, items);

            Assert.That(first, Is.EqualTo(new[] { "Alpha", "Beta" }));
            Assert.That(second, Is.SameAs(first), "Unchanged options must not allocate a new array.");

            items[1] = "Gamma";
            string[] changed = ComponentBindingInspectorShared.GetPopupOptions(owner, items);
            Assert.That(changed, Is.Not.SameAs(first));
            Assert.That(changed, Is.EqualTo(new[] { "Alpha", "Gamma" }));
            Assert.That(first, Is.EqualTo(new[] { "Alpha", "Beta" }), "A handed-out array must never be rewritten.");

            string[] otherOwner = ComponentBindingInspectorShared.GetPopupOptions(new object(), items);
            Assert.That(otherOwner, Is.Not.SameAs(changed));
            Assert.That(otherOwner, Is.EqualTo(changed));

            Assert.That(ComponentBindingInspectorShared.GetPopupOptions(owner, new List<string>()), Is.Empty);
        }

        [Test]
        public void ReuseOptions_KeepsTheCallersArrayUntilTheTextChanges()
        {
            List<string> items = new() { "<none>", "Fire ()" };
            string[] slot = null;

            string[] first = ComponentBindingInspectorShared.ReuseOptions(ref slot, items);
            Assert.That(first, Is.EqualTo(new[] { "<none>", "Fire ()" }));
            Assert.That(slot, Is.SameAs(first));
            Assert.That(ComponentBindingInspectorShared.ReuseOptions(ref slot, items), Is.SameAs(first));

            items.Add("Stop ()");
            string[] changed = ComponentBindingInspectorShared.ReuseOptions(ref slot, items);
            Assert.That(changed, Is.Not.SameAs(first));
            Assert.That(slot, Is.SameAs(changed));
            Assert.That(first, Is.EqualTo(new[] { "<none>", "Fire ()" }), "A handed-out array must never be rewritten.");

            Assert.That(ComponentBindingInspectorShared.ReuseOptions(ref slot, new List<string>()), Is.Empty);
        }

        [Test]
        public void PopupOptions_WarmLookupDoesNotAllocate()
        {
            object owner = new();
            List<string> items = new() { "Transform", "BoxCollider", "BoxCollider (2)" };
            ComponentBindingInspectorShared.GetPopupOptions(owner, items);

            TestDelegate warmLookup = () => ComponentBindingInspectorShared.GetPopupOptions(owner, items);
            warmLookup();

            Assert.That(warmLookup, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void ConditionMemberLabels_MatchTheMemberListsAndAreCachedPerType()
        {
            GameObject root = Own(new GameObject("EditorToolsQualityPass_Members"));
            BoxCollider collider = root.AddComponent<BoxCollider>();
            List<string> names = new();
            List<Neo.Condition.ValueType> types = new();
            List<string> labels = new();
            List<bool> isMethod = new();
            List<Neo.Condition.ArgumentKind> argumentKinds = new();

            foreach (bool includeMethods in new[] { false, true })
            {
                ComponentBindingInspectorShared.BuildConditionMemberLists(collider, includeMethods, names, types,
                    labels, isMethod, argumentKinds);
                string[] cached = ComponentBindingInspectorShared.GetConditionMemberLabels(typeof(BoxCollider),
                    includeMethods);

                Assert.That(cached, Is.EqualTo(labels), "Popup labels must line up with the member rows.");
                Assert.That(ComponentBindingInspectorShared.GetConditionMemberLabels(typeof(BoxCollider),
                    includeMethods), Is.SameAs(cached));
            }

            List<string> keys = new();
            List<string> floatLabels = new();
            ComponentBindingInspectorShared.BuildFloatBindingMemberLists(collider, keys, floatLabels);
            string[] cachedFloat = ComponentBindingInspectorShared.GetFloatBindingMemberLabels(typeof(BoxCollider));
            Assert.That(cachedFloat, Is.EqualTo(floatLabels));
            Assert.That(ComponentBindingInspectorShared.GetFloatBindingMemberLabels(typeof(BoxCollider)),
                Is.SameAs(cachedFloat));
            Assert.That(ComponentBindingInspectorShared.GetConditionMemberLabels(null, true), Is.Empty);
        }

        [Test]
        public void ConditionCurrentValue_CachesTheMemberButReadsTheLiveValue()
        {
            GameObject root = Own(new GameObject("EditorToolsQualityPass_CurrentValue"));
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.isTrigger = false;

            object before = NeoConditionEditor.GetCurrentValueFromComponent(collider, nameof(BoxCollider.isTrigger),
                false, null, null, null, null);
            collider.isTrigger = true;
            object after = NeoConditionEditor.GetCurrentValueFromComponent(collider, nameof(BoxCollider.isTrigger),
                false, null, null, null, null);

            Assert.That(before, Is.EqualTo(false));
            Assert.That(after, Is.EqualTo(true), "The member lookup is cached, the value must not be.");
            Assert.That(NeoConditionEditor.GetCurrentValueFromComponent(collider, "NoSuchMember_EditorTools",
                false, null, null, null, null), Is.Null);
        }

        [Test]
        public void ConditionInspector_DrawingAMultiSelectionKeepsEveryObjectsOwnConditions()
        {
            Neo.Condition.NeoCondition first = AddConditionHost("EditorToolsQualityPass_ConditionA", true, 1f, true);
            Neo.Condition.NeoCondition second = AddConditionHost("EditorToolsQualityPass_ConditionB", false, 2f, false);
            UnityEditor.Editor editor = Own(UnityEditor.Editor.CreateEditor(new Object[] { first, second },
                typeof(NeoConditionEditor)));
            InspectorProbeWindow window = ScriptableObject.CreateInstance<InspectorProbeWindow>();
            try
            {
                window.Inspected = editor;
                window.ShowUtility();
                RepaintNow(window);
                RepaintNow(window);

                Assume.That(window.Passes, Is.GreaterThan(0), "This run has no IMGUI (batchmode without graphics).");
                AssertFirstCondition(first, true, 1f, true);
                AssertFirstCondition(second, false, 2f, false);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void DefinitionValidationCache_RefreshesOnlyAfterTheAssetChanges()
        {
            QuestConfig quest = Own(ScriptableObject.CreateInstance<QuestConfig>());
            DefinitionValidationCache cache = new();

            Assert.That(cache.NeedsRefresh(null), Is.False);
            Assert.That(cache.NeedsRefresh(quest), Is.True, "An empty cache must ask for a first result.");

            cache.Store(quest, quest.ValidateDefinition());
            Assert.That(cache.Issues, Is.Not.Empty, "A fresh quest has no id and no objectives.");
            Assert.That(cache.NeedsRefresh(quest), Is.False);

            int dirtyBefore = EditorUtility.GetDirtyCount(quest);
            using (SerializedObject serialized = new(quest))
            {
                serialized.FindProperty("_id").stringValue = "editor_tools_quest";
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(quest);
            Assume.That(EditorUtility.GetDirtyCount(quest), Is.Not.EqualTo(dirtyBefore),
                "This editor build does not count dirty marks on in-memory assets.");
            Assert.That(cache.NeedsRefresh(quest), Is.True, "An edited asset must be validated again.");

            cache.Store(quest, quest.ValidateDefinition());
            Assert.That(cache.NeedsRefresh(quest), Is.False);
            cache.Invalidate();
            Assert.That(cache.NeedsRefresh(quest), Is.True);

            QuestConfig other = Own(ScriptableObject.CreateInstance<QuestConfig>());
            cache.Store(quest, quest.ValidateDefinition());
            Assert.That(cache.NeedsRefresh(other), Is.True, "A different asset never reuses another's result.");
        }

        [Test]
        public void PackageRoot_IsAForwardSlashPathToTheFolderHoldingThisPackage()
        {
            string root = CreateMenuObject.startPath;

            Assert.That(root, Does.Not.Contain("\\"), "Asset paths always use '/'.");
            Assert.That(root, Does.EndWith("/"));
            Assert.That(root, Does.Not.Contain("/Tests/"),
                "The name filter also matches the CreateMenuObject test scripts; they must not anchor the root.");
            Assert.That(AssetDatabase.LoadAssetAtPath<MonoScript>(root + "Editor/Create/CreateMenuObject.cs"),
                Is.Not.Null);
        }

        [Test]
        public void CreateWindow_RebuildsItsEntriesOnTheFirstDrawWithoutThem()
        {
            const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
            Type windowType = typeof(CreateMenuObject).Assembly.GetType("Neo.CreateNeoxiderObjectWindow", true);
            EditorWindow window = (EditorWindow)Own(ScriptableObject.CreateInstance(windowType));
            FieldInfo entriesField = windowType.GetField("_entries", instance);
            MethodInfo ensureEntries = windowType.GetMethod("EnsureEntries", instance);
            Assert.That(entriesField, Is.Not.Null);
            Assert.That(ensureEntries, Is.Not.Null);

            Assert.That(((Array)entriesField.GetValue(window)).Length, Is.Zero,
                "OnEnable must not scan: ShowWindow hands a new window its list right after OnEnable.");

            ensureEntries.Invoke(window, null);
            Array entries = (Array)entriesField.GetValue(window);
            Array expected = (Array)typeof(CreateMenuObject)
                .GetMethod("BuildCreateMenuEntriesForWindow", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, null);

            Assert.That(entries.Length, Is.EqualTo(expected.Length),
                "A window created after a domain reload must list the components without pressing Refresh.");

            ensureEntries.Invoke(window, null);
            Assert.That(entriesField.GetValue(window), Is.SameAs(entries), "A window with a tree keeps its list.");
        }

        [Test]
        public void QuestGenerateId_GivesEverySelectedQuestTheIdOfItsOwnTitle()
        {
            QuestConfig first = Own(ScriptableObject.CreateInstance<QuestConfig>());
            QuestConfig second = Own(ScriptableObject.CreateInstance<QuestConfig>());
            SetQuestTexts(first, "Find the Key", "old_first");
            SetQuestTexts(second, "Open the Gate", "old_second");
            UnityEditor.Editor editor = Own(UnityEditor.Editor.CreateEditor(new Object[] { first, second },
                typeof(QuestConfigEditor)));
            MethodInfo generate = typeof(QuestConfigEditor).GetMethod("GenerateIdsFromOwnTitles",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(generate, Is.Not.Null);

            try
            {
                generate.Invoke(editor, null);

                Assert.That(first.Id, Is.EqualTo("Find_the_Key"));
                Assert.That(second.Id, Is.EqualTo("Open_the_Gate"),
                    "Each quest takes its own title, not the first selected quest's.");

                Undo.FlushUndoRecordObjects();
                Undo.PerformUndo();
                Assert.That(first.Id, Is.EqualTo("old_first"), "One undo step must restore every quest.");
                Assert.That(second.Id, Is.EqualTo("old_second"), "One undo step must restore every quest.");
            }
            finally
            {
                Undo.ClearUndo(first);
                Undo.ClearUndo(second);
            }
        }

        [Test]
        public void DialogueWindow_ClosingDoesNotDirtyAnUnchangedController()
        {
            GameObject host = Own(new GameObject("EditorToolsQualityPass_Dialogue"));
            DialogueController controller = host.AddComponent<DialogueController>();
            DialogueEditorWindow window = Own(ScriptableObject.CreateInstance<DialogueEditorWindow>());
            const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(DialogueEditorWindow).GetMethod("BindTarget", instance, null,
                new[] { typeof(DialogueController) }, null).Invoke(window, new object[] { controller });

            int calibration = EditorUtility.GetDirtyCount(controller);
            EditorUtility.SetDirty(controller);
            Assume.That(EditorUtility.GetDirtyCount(controller), Is.Not.EqualTo(calibration),
                "This editor build does not count dirty marks on scene objects.");

            int before = EditorUtility.GetDirtyCount(controller);
            typeof(DialogueEditorWindow).GetMethod("OnDisable", instance).Invoke(window, null);

            Assert.That(EditorUtility.GetDirtyCount(controller), Is.EqualTo(before),
                "Closing the window without edits must not mark the controller, and with it the scene, dirty.");
        }

        [Test]
        public void SceneOverlayToggles_AreCachedAndWrittenThroughToEditorPrefs()
        {
            const string labelsKey = "Neoxider.UIMeshRig.ShowAllLabels";
            bool hadKey = EditorPrefs.HasKey(labelsKey);
            bool original = EditorPrefs.GetBool(labelsKey, false);
            try
            {
                Neo.UI.Editor.UIMeshRigSceneHandles.ReloadOverlayPreferences();
                Assert.That(Neo.UI.Editor.UIMeshRigSceneHandles.ShowAllLabels, Is.EqualTo(original));

                Neo.UI.Editor.UIMeshRigSceneHandles.ShowAllLabels = !original;
                Assert.That(EditorPrefs.GetBool(labelsKey, original), Is.EqualTo(!original),
                    "A toggle must persist immediately.");

                EditorPrefs.SetBool(labelsKey, original);
                Assert.That(Neo.UI.Editor.UIMeshRigSceneHandles.ShowAllLabels, Is.EqualTo(!original),
                    "Scene view events read the cached value, not the registry.");

                Neo.UI.Editor.UIMeshRigSceneHandles.ReloadOverlayPreferences();
                Assert.That(Neo.UI.Editor.UIMeshRigSceneHandles.ShowAllLabels, Is.EqualTo(original));
            }
            finally
            {
                if (hadKey)
                {
                    EditorPrefs.SetBool(labelsKey, original);
                }
                else
                {
                    EditorPrefs.DeleteKey(labelsKey);
                }

                Neo.UI.Editor.UIMeshRigSceneHandles.ReloadOverlayPreferences();
            }
        }

        [Test]
        public void ProjectZip_CancelLeavesTheExistingArchiveUntouched()
        {
            string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                "NeoEditorToolsZip_" + Guid.NewGuid().ToString("N"));
            string assets = System.IO.Path.Combine(root, "Assets");
            System.IO.Directory.CreateDirectory(assets);
            string destination = System.IO.Path.Combine(root, "Project.zip");
            try
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(assets, "a.txt"), "a");
                System.IO.File.WriteAllText(System.IO.Path.Combine(assets, "b.txt"), "b");
                System.IO.File.WriteAllText(destination, "previous archive");
                int reported = 0;
                int reportedTotal = -1;

                Assert.Throws<OperationCanceledException>(() => global::SaveProjectZip.CreateArchive(root,
                    destination, (done, total, entry) =>
                    {
                        reported = done;
                        reportedTotal = total;
                        return true;
                    }));

                Assert.That(reported, Is.EqualTo(1), "Cancelling must stop at the first reported entry.");
                Assert.That(reportedTotal, Is.GreaterThanOrEqualTo(2), "The total is counted before writing.");
                Assert.That(System.IO.File.ReadAllText(destination), Is.EqualTo("previous archive"));
                Assert.That(System.IO.Directory.GetFiles(root, "*.tmp"), Is.Empty, "No temporary file may remain.");
            }
            finally
            {
                if (System.IO.Directory.Exists(root))
                {
                    System.IO.Directory.Delete(root, true);
                }
            }
        }

        private Neo.Condition.NeoCondition AddConditionHost(string name, bool invert, float threshold,
            bool waitForObject)
        {
            GameObject host = Own(new GameObject(name));
            Neo.Condition.NeoCondition condition = host.AddComponent<Neo.Condition.NeoCondition>();
            condition.AddCondition(new Neo.Condition.ConditionEntry
            {
                UseSceneSearch = true,
                SearchObjectName = name + "_Target",
                WaitForObject = waitForObject,
                Invert = invert,
                ThresholdFloat = threshold
            });
            return condition;
        }

        private static void AssertFirstCondition(Component condition, bool invert, float threshold,
            bool waitForObject)
        {
            using (SerializedObject serialized = new(condition))
            {
                SerializedProperty entry = serialized.FindProperty("_conditions").GetArrayElementAtIndex(0);
                string owner = condition.gameObject.name;
                Assert.That(entry.FindPropertyRelative("_invert").boolValue, Is.EqualTo(invert),
                    owner + ": drawing a multi-selection must not copy the first object's NOT toggle.");
                Assert.That(entry.FindPropertyRelative("_thresholdFloat").floatValue, Is.EqualTo(threshold),
                    owner + ": drawing a multi-selection must not copy the first object's threshold.");
                Assert.That(entry.FindPropertyRelative("_waitForObject").boolValue, Is.EqualTo(waitForObject),
                    owner + ": drawing a multi-selection must not copy the first object's Wait For Object.");
            }
        }

        private static void RepaintNow(EditorWindow window)
        {
            // WHY: RepaintImmediately is internal; it runs a full Layout + Repaint pass right now.
            MethodInfo repaint = typeof(EditorWindow).GetMethod("RepaintImmediately",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, Type.EmptyTypes, null);
            Assert.That(repaint, Is.Not.Null, "Required EditorWindow.RepaintImmediately seam is unavailable.");
            repaint.Invoke(window, null);
        }

        private sealed class InspectorProbeWindow : EditorWindow
        {
            public UnityEditor.Editor Inspected;
            public int Passes;

            private void OnGUI()
            {
                if (Inspected == null)
                {
                    return;
                }

                EventType type = Event.current.type;
                if (type != EventType.Layout && type != EventType.Repaint)
                {
                    return;
                }

                Inspected.OnInspectorGUI();
                Passes++;
            }
        }

        private static void SetQuestTexts(QuestConfig quest, string title, string id)
        {
            using (SerializedObject serialized = new(quest))
            {
                serialized.FindProperty("_title").stringValue = title;
                serialized.FindProperty("_id").stringValue = id;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private T Own<T>(T obj) where T : Object
        {
            _owned.Add(obj);
            return obj;
        }
    }
}
