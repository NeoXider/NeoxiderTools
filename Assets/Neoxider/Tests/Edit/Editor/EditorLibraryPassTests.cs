using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Neo.Editor.Binding;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using CollectionProbe = global::Neo.Editor.Tests.Audit20261023FixtureNeoEditorCollectionCollectionProbe;
using InjectionProbe = global::Neo.Editor.Tests.EditorLibraryPassInjectionProbe;
using Object = UnityEngine.Object;
using PlainComponent = global::Neo.Editor.Tests.EditorLibraryPassPlainComponent;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Regression and cost guards for the Neo.Editor quality pass: attribute injection runs on every IMGUI event
    ///     of every MonoBehaviour inspector, so it must be free for types without injection attributes, report each
    ///     problem once and record its assignments with Undo; [Button] actions act on every selected object.
    /// </summary>
    public sealed class EditorLibraryPassTests
    {
        private const int Iterations = 200;

        private readonly List<Object> _owned = new();
        private NeoxiderSettings.NeoxiderData _previousSettings;

        [SetUp]
        public void SetUp()
        {
            _previousSettings = NeoxiderSettings.Current;
            NeoxiderSettings.Current = new NeoxiderSettings.NeoxiderData { enableAttributeSearch = true };
        }

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
            NeoxiderSettings.Current = _previousSettings;
        }

        [Test]
        public void InjectionProcessing_TypeWithoutInjectionAttributes_AllocatesNothingWhenWarm()
        {
            GameObject host = Own(new GameObject("EditorLibraryPass.NoAttributes"));
            PlainComponent plain = host.AddComponent<PlainComponent>();
            ComponentDrawer.ProcessComponentAttributes(plain);
            ResourceDrawer.ProcessResourceAttributes(plain);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                ComponentDrawer.ProcessComponentAttributes(plain);
                ResourceDrawer.ProcessResourceAttributes(plain);
            }

            long allocated = allocationCounter.End();

            Assert.That(allocated, Is.EqualTo(0L),
                "Every inspector event used to reflect all fields and box every value-type field.");
        }

        [Test]
        public void InjectionProcessing_AssignedAttributedFields_AllocatesNothingWhenWarm()
        {
            GameObject host = Own(new GameObject("EditorLibraryPass.Assigned"));
            BoxCollider collider = host.AddComponent<BoxCollider>();
            CollectionProbe probe = host.AddComponent<CollectionProbe>();
            Texture2D image = Own(new Texture2D(2, 2));
            probe.Local = new[] { collider };
            probe.Children = new List<BoxCollider> { collider };
            probe.SceneArray = new[] { collider };
            probe.SceneList = new List<BoxCollider> { collider };
            probe.Images = new List<Texture2D> { image };
            probe.ImageArray = new[] { image };
            ComponentDrawer.ProcessComponentAttributes(probe);
            ResourceDrawer.ProcessResourceAttributes(probe);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                ComponentDrawer.ProcessComponentAttributes(probe);
                ResourceDrawer.ProcessResourceAttributes(probe);
            }

            long allocated = allocationCounter.End();

            Assert.That(allocated, Is.EqualTo(0L));
            Assert.That(probe.Local, Is.EqualTo(new[] { collider }), "Assigned collections must stay untouched.");
        }

        [Test]
        public void InjectionProcessing_ReportsEachUnresolvableFieldOnce()
        {
            GameObject host = Own(new GameObject("EditorLibraryPass.Diagnostics"));
            InjectionProbe probe = host.AddComponent<InjectionProbe>();
            // WHY: LogAssert matches expectations in order; the component pass runs first in the loop below.
            LogAssert.Expect(LogType.Warning, new Regex("Field 'NotComponents': type .* is not a Component or GameObject"));
            LogAssert.Expect(LogType.Warning, new Regex("for field 'Missing' not found at Resources path"));

            for (int i = 0; i < 5; i++)
            {
                ComponentDrawer.ProcessComponentAttributes(probe);
                ResourceDrawer.ProcessResourceAttributes(probe);
            }

            LogAssert.NoUnexpectedReceived();
            Assert.That(probe.Missing, Is.Null);
        }

        [Test]
        public void GetComponentAssignment_IsRecordedWithUndo()
        {
            GameObject host = Own(new GameObject("EditorLibraryPass.Undo"));
            BoxCollider collider = host.AddComponent<BoxCollider>();
            InjectionProbe probe = host.AddComponent<InjectionProbe>();
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            try
            {
                ComponentDrawer.ProcessComponentAttributes(probe);
                Assert.That(probe.Local, Is.SameAs(collider));

                Undo.FlushUndoRecordObjects();
                Undo.CollapseUndoOperations(group);
                Undo.PerformUndo();

                Assert.That(probe.Local == null, Is.True,
                    "An unrecorded SetValue is lost on prefab instances and cannot be undone.");
            }
            finally
            {
                Undo.IncrementCurrentGroup();
            }
        }

        [Test]
        public void EmptyCollectionWithoutMatches_IsNotReassignedOrDirtiedOnEveryEvent()
        {
            GameObject host = Own(new GameObject("EditorLibraryPass.EmptyCollection"));
            InjectionProbe probe = host.AddComponent<InjectionProbe>();
            SphereCollider[] initial = probe.NoMatches;
            ComponentDrawer.ProcessComponentAttributes(probe);
            int dirtyCount = EditorUtility.GetDirtyCount(probe);

            for (int i = 0; i < 5; i++)
            {
                ComponentDrawer.ProcessComponentAttributes(probe);
            }

            Assert.That(probe.NoMatches, Is.SameAs(initial), "An empty result must not replace an empty collection.");
            Assert.That(EditorUtility.GetDirtyCount(probe), Is.EqualTo(dirtyCount),
                "Re-recording a no-op kept the scene dirty after merely viewing the inspector.");
        }

        [Test]
        public void InjectionDiagnostics_ReportOncePerPairUntilResolved()
        {
            InjectionDiagnostics diagnostics = new();
            ScriptableObject first = Own(ScriptableObject.CreateInstance<ScriptableObject>());
            ScriptableObject second = Own(ScriptableObject.CreateInstance<ScriptableObject>());
            FieldInfo field = typeof(InjectionProbe).GetField(nameof(InjectionProbe.Local));

            Assert.That(diagnostics.ShouldReport(first, field), Is.True);
            Assert.That(diagnostics.ShouldReport(first, field), Is.False);
            Assert.That(diagnostics.ShouldReport(second, field), Is.True, "Pairs are tracked per object.");

            diagnostics.Resolve(first, field);

            Assert.That(diagnostics.ShouldReport(first, field), Is.True, "A resolved problem that returns is reported.");
        }

        [Test]
        public void InjectedFieldCache_ListsOnlyAttributedFieldsOncePerType()
        {
            InjectedField[] component = InjectedFieldCache.GetComponentFields(typeof(InjectionProbe));
            InjectedField[] resource = InjectedFieldCache.GetResourceFields(typeof(InjectionProbe));

            Assert.That(component, Has.Length.EqualTo(3));
            Assert.That(resource, Has.Length.EqualTo(1));
            Assert.That(resource[0].Field.Name, Is.EqualTo(nameof(InjectionProbe.Missing)));
            Assert.That(InjectedFieldCache.GetComponentFields(typeof(InjectionProbe)), Is.SameAs(component));
            Assert.That(InjectedFieldCache.GetComponentFields(typeof(PlainComponent)), Is.Empty);
            Assert.That(InjectedFieldCache.GetResourceFields(typeof(PlainComponent)), Is.Empty);
        }

        private sealed class CountingProbe : ScriptableObject
        {
            public static int StaticCalls;
            public int Calls;

            [Button]
            public void Bump()
            {
                Calls++;
            }

            [Button]
            public static void StaticBump()
            {
                StaticCalls++;
            }
        }

        [CanEditMultipleObjects]
        private sealed class MultiProbeEditor : CustomEditorBase
        {
            protected override void ProcessAttributeAssignments()
            {
            }
        }

        [Test]
        public void ButtonAction_RunsOnEverySelectedObjectAndStaticMethodsOnce()
        {
            CountingProbe first = Own(ScriptableObject.CreateInstance<CountingProbe>());
            CountingProbe second = Own(ScriptableObject.CreateInstance<CountingProbe>());
            MultiProbeEditor editor =
                (MultiProbeEditor)Own(UnityEditor.Editor.CreateEditor(new Object[] { first, second },
                    typeof(MultiProbeEditor)));
            Assume.That(editor.targets, Has.Length.EqualTo(2));
            MethodInfo bump = typeof(CountingProbe).GetMethod(nameof(CountingProbe.Bump));
            MethodInfo staticBump = typeof(CountingProbe).GetMethod(nameof(CountingProbe.StaticBump));
            CountingProbe.StaticCalls = 0;

            editor.InvokeButtonMethod(bump, bump.GetParameters());
            editor.InvokeButtonMethod(staticBump, staticBump.GetParameters());

            Assert.That(first.Calls, Is.EqualTo(1));
            Assert.That(second.Calls, Is.EqualTo(1), "A button pressed on a multi-selection must act on each object.");
            Assert.That(CountingProbe.StaticCalls, Is.EqualTo(1), "Static actions run once per press.");
        }

        [Test]
        public void ButtonAction_InEditMode_IsUndoable()
        {
            Assume.That(EditorApplication.isPlaying, Is.False);
            CountingProbe probe = Own(ScriptableObject.CreateInstance<CountingProbe>());
            MultiProbeEditor editor = (MultiProbeEditor)Own(UnityEditor.Editor.CreateEditor(probe, typeof(MultiProbeEditor)));
            MethodInfo bump = typeof(CountingProbe).GetMethod(nameof(CountingProbe.Bump));
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            try
            {
                editor.InvokeButtonMethod(bump, bump.GetParameters());
                Assert.That(probe.Calls, Is.EqualTo(1));

                Undo.FlushUndoRecordObjects();
                Undo.CollapseUndoOperations(group);
                Undo.PerformUndo();

                Assert.That(probe.Calls, Is.Zero, "Serialized changes made by an action must be undoable.");
            }
            finally
            {
                Undo.IncrementCurrentGroup();
            }
        }

        [TestCase("ShortName", null, 16, "Short Name")]
        [TestCase("Method", "Explicit", 16, "Explicit")]
        [TestCase("VeryLongMethodNameHere", null, 16, "Very Long Method...")]
        [TestCase("Name", null, 1, "...")]
        [TestCase("Name", null, 0, "Name")]
        public void ActionTitle_ShortensWithoutThrowingForAnyLength(string method, string buttonName, int maxLength,
            string expected)
        {
            Assert.That(CustomEditorBase.BuildActionTitle(method, buttonName, maxLength), Is.EqualTo(expected));
        }

        [Test]
        public void ReplicationMarker_IsOnlyConsideredForReplicatingComponentTypes()
        {
            Assert.That(CustomEditorBase.CanShowReplicationMarker(typeof(Neo.Tools.Counter)), Is.True);
            Assert.That(CustomEditorBase.CanShowReplicationMarker(typeof(PlainComponent)), Is.False);
            Assert.That(CustomEditorBase.CanShowReplicationMarker(null), Is.False);
        }

        [Test]
        public void ReplicationMarker_GateAndMemberChecksShareOneTable()
        {
            Type counter = typeof(Neo.Tools.Counter);
            Type relay = typeof(Neo.Network.NetworkActionRelay);

            Assert.That(CustomEditorBase.IsReplicatedEventMember(counter, "OnValueChangedInt", "OnValueChangedInt"),
                Is.True);
            Assert.That(CustomEditorBase.IsReplicatedReactiveMember(counter, "Value"), Is.True);
            Assert.That(CustomEditorBase.IsReplicatedEventMember(counter, "onTriggered", "actions.Array.data[0].onTriggered"),
                Is.False, "Members belong to their own component type.");
            Assert.That(CustomEditorBase.CanShowReplicationMarker(relay), Is.True);
            Assert.That(CustomEditorBase.IsReplicatedEventMember(relay, "onTriggered", "onTriggered"), Is.True);
            Assert.That(CustomEditorBase.IsReplicatedEventMember(relay, "data", "actions.Array.data[0].onTriggered"),
                Is.True, "Relay events nested in its action list are replicated too.");
            Assert.That(CustomEditorBase.IsReplicatedReactiveMember(relay, "Value"), Is.False);
            Assert.That(CustomEditorBase.IsReplicatedEventMember(typeof(PlainComponent), "onClick", "onClick"), Is.False);
        }

        [Test]
        public void SectionFoldoutKey_IsStableAndBuiltOncePerTypeAndTitle()
        {
            string first = CustomEditorBase.GetSectionFoldoutKey(typeof(CollectionProbe), "Movement");
            string second = CustomEditorBase.GetSectionFoldoutKey(typeof(CollectionProbe), "Movement");

            Assert.That(first, Is.EqualTo(typeof(CollectionProbe).FullName + ".NeoFoldout.Header.Movement"),
                "The key format is persisted foldout state; it must not change.");
            Assert.That(second, Is.SameAs(first));
            Assert.That(CustomEditorBase.GetSectionFoldoutKey(null, "Movement"), Is.EqualTo("Neo.NeoFoldout.Header.Movement"));
        }

        [Test]
        public void FloatBindingMembers_AreReflectedOncePerType()
        {
            GameObject host = Own(new GameObject("EditorLibraryPass.Members"));
            BoxCollider source = host.AddComponent<BoxCollider>();
            List<string> keys = new();
            List<string> labels = new();
            List<string> keysAgain = new();
            List<string> labelsAgain = new();

            ComponentBindingInspectorShared.BuildFloatBindingMemberLists(source, keys, labels);
            ComponentBindingInspectorShared.BuildFloatBindingMemberLists(source, keysAgain, labelsAgain);

            Assume.That(labels, Is.Not.Empty);
            Assert.That(keysAgain, Is.EqualTo(keys));
            Assert.That(labelsAgain[0], Is.SameAs(labels[0]), "Labels are formatted once per type, not per draw.");
        }

        [Test]
        public void ComponentPickList_NumbersDuplicatesInAttachOrder()
        {
            GameObject host = Own(new GameObject("EditorLibraryPass.PickList"));
            host.AddComponent<BoxCollider>();
            host.AddComponent<SphereCollider>();
            host.AddComponent<BoxCollider>();
            host.AddComponent<BoxCollider>();
            List<string> names = new();
            List<string> types = new();

            ComponentBindingInspectorShared.BuildComponentPickLists(host, names, types);

            Assert.That(names, Is.EqualTo(new[] { "BoxCollider", "SphereCollider", "BoxCollider (2)", "BoxCollider (3)" }));
            Assert.That(types, Has.Count.EqualTo(4));
        }

        [Test]
        public void SceneLookup_ReusesTheAnswerAndRefindsADestroyedObject()
        {
            string name = "EditorLibraryPass.Lookup." + Guid.NewGuid().ToString("N");
            ComponentBindingInspectorShared.ClearSceneLookupCache();
            GameObject first = Own(new GameObject(name));

            Assert.That(ComponentBindingInspectorShared.FindSceneObject(name), Is.SameAs(first));
            Assert.That(ComponentBindingInspectorShared.FindSceneObject(name), Is.SameAs(first));

            Object.DestroyImmediate(first);
            GameObject second = Own(new GameObject(name));

            Assert.That(ComponentBindingInspectorShared.FindSceneObject(name), Is.SameAs(second),
                "A cached object that was destroyed must not hide its replacement.");
            Assert.That(ComponentBindingInspectorShared.FindSceneObject(null), Is.Null);
            ComponentBindingInspectorShared.ClearSceneLookupCache();
        }

        [Test]
        public void SceneSaverScheduler_IntervalPrecheckMatchesShouldSave()
        {
            SceneSaverAutoSaveScheduler scheduler = new();
            scheduler.ResetTimer(100.0);

            scheduler.ObserveDirty(true);
            scheduler.ObserveDirty(true);
            Assert.That(scheduler.DirtyEpoch, Is.EqualTo(1L), "Observing the same dirty flag counts once.");
            Assert.That(scheduler.IsIntervalElapsed(150.0, 60.0), Is.False);
            Assert.That(scheduler.ShouldSave(new SceneSaverCheckContext(150.0, 60.0, "Assets/A.unity", 1, true, false)),
                Is.False);

            Assert.That(scheduler.IsIntervalElapsed(161.0, 60.0), Is.True);
            Assert.That(scheduler.ShouldSave(new SceneSaverCheckContext(161.0, 60.0, "Assets/A.unity", 1, true, false)),
                Is.True);
            Assert.That(scheduler.DirtyEpoch, Is.EqualTo(1L), "The precheck must not double-count the transition.");

            scheduler.ObserveDirty(false);
            scheduler.ObserveDirty(true);
            Assert.That(scheduler.DirtyEpoch, Is.EqualTo(2L));
        }

        [Test]
        public void SettingsFolderCheck_ListsMissingRootAndFolders()
        {
            string root = "NeoEditorLibraryPass_" + Guid.NewGuid().ToString("N");
            NeoxiderSettings.Current = new NeoxiderSettings.NeoxiderData
            {
                rootFolder = root,
                folders = new[] { "Audio", string.Empty, "Prefabs" }
            };

            List<string> missing = NeoxiderSettings.FindMissingFolders();

            Assert.That(missing, Is.EqualTo(new[] { root, "Audio", "Prefabs" }));
        }

        [Test]
        public void SettingsSave_PersistsTheSceneHierarchyTemplate()
        {
            string path = Path.Combine(Application.temporaryCachePath,
                "NeoEditorLibraryPass_" + Guid.NewGuid().ToString("N") + ".json");
            CreateSceneHierarchy hierarchy = NeoxiderSettings.SceneHierarchy;
            string original = JsonUtility.ToJson(hierarchy);
            NeoxiderSettings.SettingsPathOverride = path;
            try
            {
                SetSeparator(hierarchy, "##");
                NeoxiderSettings.SaveSettings();
                SetSeparator(hierarchy, "--");

                NeoxiderSettings.LoadSettings();

                using (SerializedObject reloaded = new(NeoxiderSettings.SceneHierarchy))
                {
                    Assert.That(reloaded.FindProperty("separatorSymbols").stringValue, Is.EqualTo("##"),
                        "Template edits in the settings window must survive a reload.");
                }
            }
            finally
            {
                NeoxiderSettings.SettingsPathOverride = null;
                JsonUtility.FromJsonOverwrite(original, NeoxiderSettings.SceneHierarchy);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Test]
        public void SceneHierarchy_AsFirstSettingsAccessAfterReload_RestoresTheSavedTemplate()
        {
            string path = Path.Combine(Application.temporaryCachePath,
                "NeoEditorLibraryPass_" + Guid.NewGuid().ToString("N") + ".json");
            NeoxiderSettings.NeoxiderData liveData = NeoxiderSettings.Current;
            CreateSceneHierarchy liveHierarchy = NeoxiderSettings.SceneHierarchy;
            CreateSceneHierarchy edited = Own(ScriptableObject.CreateInstance<CreateSceneHierarchy>());
            NeoxiderSettings.SettingsPathOverride = path;
            try
            {
                NeoxiderSettings.SetCachedStateForTests(new NeoxiderSettings.NeoxiderData(), edited);
                SetSeparator(edited, "##");
                NeoxiderSettings.SaveSettings();

                // WHY: the menu items (Create/Sort Scene Hierarchy) read SceneHierarchy before anything loads Current.
                NeoxiderSettings.SetCachedStateForTests(null, null);
                CreateSceneHierarchy restored = Own(NeoxiderSettings.SceneHierarchy);

                Assert.That(restored, Is.Not.SameAs(edited));
                Assert.That(GetSeparator(restored), Is.EqualTo("##"),
                    "The first access after a reload must apply the template saved with the settings.");
            }
            finally
            {
                NeoxiderSettings.SettingsPathOverride = null;
                NeoxiderSettings.SetCachedStateForTests(liveData, liveHierarchy);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Test]
        public void SceneHierarchy_DestroyedInstance_IsRebuiltFromTheLoadedTemplate()
        {
            NeoxiderSettings.NeoxiderData liveData = NeoxiderSettings.Current;
            CreateSceneHierarchy liveHierarchy = NeoxiderSettings.SceneHierarchy;
            CreateSceneHierarchy template = Own(ScriptableObject.CreateInstance<CreateSceneHierarchy>());
            SetSeparator(template, "##");
            CreateSceneHierarchy unloaded = ScriptableObject.CreateInstance<CreateSceneHierarchy>();
            Object.DestroyImmediate(unloaded);
            try
            {
                NeoxiderSettings.SetCachedStateForTests(
                    new NeoxiderSettings.NeoxiderData { sceneHierarchyJson = JsonUtility.ToJson(template) }, unloaded);

                CreateSceneHierarchy rebuilt = Own(NeoxiderSettings.SceneHierarchy);

                Assert.That(rebuilt != null, Is.True);
                Assert.That(GetSeparator(rebuilt), Is.EqualTo("##"),
                    "An unloaded instance must not fall back to the default template.");
            }
            finally
            {
                NeoxiderSettings.SetCachedStateForTests(liveData, liveHierarchy);
            }
        }

        private static string GetSeparator(CreateSceneHierarchy hierarchy)
        {
            using (SerializedObject serialized = new(hierarchy))
            {
                return serialized.FindProperty("separatorSymbols").stringValue;
            }
        }

        private static void SetSeparator(CreateSceneHierarchy hierarchy, string separator)
        {
            using (SerializedObject serialized = new(hierarchy))
            {
                serialized.FindProperty("separatorSymbols").stringValue = separator;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        [Test]
        public void SettingsFolderCheck_HandlesMissingFolderList()
        {
            NeoxiderSettings.Current = new NeoxiderSettings.NeoxiderData { rootFolder = string.Empty, folders = null };

            Assert.That(NeoxiderSettings.FindMissingFolders(), Is.Empty,
                "The Assets folder itself exists and a null list has nothing to report.");
        }

        [Test]
        public void UpdateCheck_PersistedCheckingWithoutRequest_ReadsAsUnknown()
        {
            Assume.That(NeoUpdateChecker.IsChecking, Is.False, "A real update request is in flight.");
            int saved = SessionState.GetInt(NeoUpdateChecker.SessionStatus, 0);
            try
            {
                SessionState.SetInt(NeoUpdateChecker.SessionStatus, (int)NeoUpdateChecker.UpdateStatus.Checking);
                NeoUpdateChecker.InvalidateCache();

                Assert.That(NeoUpdateChecker.Peek().Status, Is.EqualTo(NeoUpdateChecker.UpdateStatus.Unknown),
                    "A domain reload mid-request must not leave the strip spinning until the next scheduled check.");
            }
            finally
            {
                SessionState.SetInt(NeoUpdateChecker.SessionStatus, saved);
                NeoUpdateChecker.InvalidateCache();
            }
        }

        private const int DrawIterations = 100;

        private sealed class AllocationProbeWindow : EditorWindow
        {
            public Action Draw;
            public long Allocated = -1;

            /// <summary>Unity's own cost of one text draw: a label with a content that keeps its text.</summary>
            public long TextFloor = -1;

            private static readonly GUIContent s_floorText = new("Section");

            private void OnGUI()
            {
                if (Draw == null || Event.current.type != EventType.Repaint)
                {
                    return;
                }

                Draw();
                Rect floorRect = new(0f, 80f, 320f, 18f);
                GUI.Label(floorRect, s_floorText, EditorStyles.label);

                // WHY: OnGUI cannot ignore a test, so a counter that is not admitted leaves Allocated at -1.
                if (!GcAllocCounter.TryCreate(out GcAllocCounter counter, out string _))
                {
                    return;
                }

                using (counter)
                {
                    counter.Begin();
                    for (int i = 0; i < DrawIterations; i++)
                    {
                        Draw();
                    }

                    Allocated = counter.End();
                    counter.Begin();
                    for (int i = 0; i < DrawIterations; i++)
                    {
                        GUI.Label(floorRect, s_floorText, EditorStyles.label);
                    }

                    TextFloor = counter.End();
                }
            }
        }

        [Test]
        public void SectionHeaderAndPillToggle_ReuseStylesInsteadOfAllocatingPerEvent()
        {
            AllocationProbeWindow window = ScriptableObject.CreateInstance<AllocationProbeWindow>();
            try
            {
                Rect header = new(0f, 0f, 320f, NeoxiderEditorGUI.SectionHeaderHeight);
                Rect pill = new(0f, 40f, NeoxiderEditorGUI.PillToggleWidth, NeoxiderEditorGUI.PillToggleHeight);
                Color accent = NeoxiderEditorGUI.SectionAccent(true);
                Color title = NeoxiderEditorGUI.SectionTitleColor(true);
                window.Draw = () =>
                {
                    NeoxiderEditorGUI.DrawSectionHeaderRect(header, true, "Section", 7, accent, "d_Folder Icon", title);
                    NeoxiderEditorGUI.DrawPillToggle(pill, true);
                };
                window.ShowUtility();
                RepaintNow(window);

                Assume.That(window.Allocated, Is.GreaterThanOrEqualTo(0L),
                    "Requires an IMGUI graphics context and an admitted GC.Alloc counter.");
                Assume.That(window.TextFloor, Is.GreaterThanOrEqualTo(0L), "The text floor was measured.");
                // WHY: in Unity 6.3 every IMGUI text draw costs one GC.Alloc inside the engine binding, which no
                // caller can avoid; the bar draws two texts (title and count) and the switch none, with the widths
                // measured once. Fresh strings, GUIStyle copies or a CalcSize per draw would exceed this floor.
                // WHY: a per-draw regression adds at least DrawIterations; a tenth of that absorbs one-off sampler noise.
                Assert.That(window.Allocated, Is.LessThanOrEqualTo(2 * window.TextFloor + DrawIterations / 10),
                    "The section bar and switch cost no more than Unity's own two text draws.");
            }
            finally
            {
                window.Close();
            }
        }

        private static void RepaintNow(EditorWindow window)
        {
            // WHY: RepaintImmediately is internal; it runs a full Layout + Repaint pass right now.
            MethodInfo repaint = typeof(EditorWindow).GetMethod("RepaintImmediately",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, Type.EmptyTypes, null);
            Assume.That(repaint, Is.Not.Null, "Required EditorWindow.RepaintImmediately seam is unavailable.");
            repaint.Invoke(window, null);
        }

#pragma warning disable CS0618
        [Test]
        public void ObsoleteAutoRegister_NoLongerScansAssembliesOnDomainReload()
        {
            Assert.That(typeof(NeoEditorAutoRegister).IsDefined(typeof(InitializeOnLoadAttribute), false), Is.False,
                "The hook enumerated every loaded type after each reload and discarded the result.");
        }
#pragma warning restore CS0618

        private T Own<T>(T obj) where T : Object
        {
            _owned.Add(obj);
            return obj;
        }
    }
}
