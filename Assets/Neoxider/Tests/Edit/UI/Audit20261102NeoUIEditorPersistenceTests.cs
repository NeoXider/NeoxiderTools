using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Neo.Editor.Tests;
using Neo.Tests.Performance;
using Neo.UI;
using Neo.UI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Tests.Edit.UI
{
    /// <summary>
    /// Undo/Redo and scene save/reopen persistence of UI Mesh Rig authoring commands, run in an owned scene.
    /// </summary>
    /// <remarks>Never resets global Undo history or borrowed previews; every assertion uses owned objects.</remarks>
    public sealed class Audit20261102NeoUIEditorPersistenceTests
    {
        private const string FolderPrefix = "Assets/Audit20261023_UIEditorPersistence_";
        private readonly List<BorrowedScene> _borrowed = new List<BorrowedScene>();
        private Scene _activeBefore;
        private Scene _scene;
        private Object[] _selectionBefore;
        private string _folder;
        private string _scenePath;
        private int _previewCountBefore;
        private int _motionPreviewsBefore;
        private bool _ownsFolder;

        [SetUp]
        public void SetUp()
        {
            _scene = default;
            _folder = null;
            _scenePath = null;
            _ownsFolder = false;
            _activeBefore = SceneManager.GetActiveScene();
            _selectionBefore = Selection.objects;
            _previewCountBefore = EditorSceneManager.previewSceneCount;
            _motionPreviewsBefore = UIMeshRigMotionPreviewDriver.ActivePreviewCount;
            _borrowed.Clear();
            for (int index = 0; index < SceneManager.sceneCount; index++)
                _borrowed.Add(new BorrowedScene(SceneManager.GetSceneAt(index)));
            Assert.That(Application.isPlaying, Is.False, "This fixture saves only its own ordinary Edit Mode scene.");
            Assert.That(_activeBefore.IsValid() && _activeBefore.isLoaded, Is.True,
                "Keep an existing borrowed scene active; the fixture will restore it without global scene setup replacement.");
            Assert.That(PrefabStageUtility.GetCurrentPrefabStage(), Is.Null,
                "Do not leave or save a borrowed Prefab Stage to run this fixture.");
            Assert.That(_motionPreviewsBefore, Is.Zero,
                "Do not run persistence Undo while a borrowed preview is active; this fixture never clears it.");
            // WHY: separates every owned command from borrowed Undo history.
            Undo.IncrementCurrentGroup();
            _folder = FolderPrefix + Guid.NewGuid().ToString("N");
            Assert.That(AssetDatabase.IsValidFolder(_folder), Is.False);
            Assert.That(AssetDatabase.CreateFolder("Assets", _folder.Substring("Assets/".Length)), Is.Not.Empty);
            _ownsFolder = true;
            _scenePath = _folder + "/Owned.unity";
            Audit20261023NeoEditorTestsSceneAssets.CreateEmptySceneAsset(_scenePath);
            _scene = EditorSceneManager.OpenScene(_scenePath, OpenSceneMode.Additive);
            Assert.That(SceneManager.SetActiveScene(_scene), Is.True);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                CloseOwnedScene();
            }
            finally
            {
                try
                {
                    if (_activeBefore.IsValid() && _activeBefore.isLoaded)
                        SceneManager.SetActiveScene(_activeBefore);
                    Selection.objects = _selectionBefore;
                    if (_ownsFolder && !string.IsNullOrEmpty(_folder) && AssetDatabase.IsValidFolder(_folder))
                    {
                        Assert.That(_folder.StartsWith(FolderPrefix, StringComparison.Ordinal), Is.True);
                        Assert.That(AssetDatabase.DeleteAsset(_folder), Is.True);
                    }
                }
                finally
                {
                    AssertBorrowedContext();
                }
            }
        }

        [TestCase(typeof(UIMeshRigGraphic))]
        [TestCase(typeof(UIMeshRigWorldRenderer))]
        [TestCase(typeof(UIMeshRigSpriteRenderer))]
        public void DuplicateNestedPoint_UndoRedoAndSceneReload_PreservePoseRestKeysAndReferences(Type adapter)
        {
            IUIMeshRigOwner rig = CreateRig(adapter);
            rig.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
            GameObject group = new GameObject("Transformed group", typeof(RectTransform));
            group.transform.SetParent(rig.RigTransform, false);
            group.transform.localPosition = new Vector3(40f, -30f, 0f);
            group.transform.localRotation = Quaternion.Euler(0f, 0f, 17f);
            group.transform.localScale = new Vector3(1.5f, 0.8f, 1f);
            UIMeshRigPoint source = CreatePoint(group.transform, "Control", "Source key");
            Vector3 restPosition = new Vector3(7f, 11f, 0f);
            Quaternion restRotation = Quaternion.Euler(0f, 0f, 12f);
            Vector3 restScale = new Vector3(0.9f, 1.1f, 1f);
            SetPose(source.transform, restPosition, restRotation, restScale);
            source.CaptureRestPose(rig);
            Vector2 restCenter = source.RestCenterNormalized;
            float restAngle = source.RestRotationDegrees;
            Vector3 restRigScale = source.RestScale;
            SetPose(source.transform, new Vector3(10f, 9f, 0f), Quaternion.Euler(0f, 0f, 29f),
                new Vector3(1.2f, 0.7f, 1f));
            UIMeshRigPoint inactive = CreatePoint(source.transform, "Inactive child", "Inactive key");
            inactive.CaptureRestPose(rig);
            inactive.gameObject.SetActive(false);
            ConfigureMotion(source.gameObject.AddComponent<UIMeshRigPointMotion>());
            GameObject imageObject = new GameObject("Internal graphic", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(source.transform, false);
            Image image = imageObject.GetComponent<Image>();
            Button button = source.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            GameObject externalObject = new GameObject("External reference", typeof(RectTransform), typeof(Button));
            externalObject.transform.SetParent(rig.RigTransform, false);
            Button external = externalObject.GetComponent<Button>();
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnRight = external;
            button.navigation = navigation;
            rig.NotifyPointChanged();
            Assert.That(rig.RigPoints.Count, Is.EqualTo(2));
            Undo.IncrementCurrentGroup();
            int groupId = Undo.GetCurrentGroup();

            UIMeshRigPoint duplicate = UIMeshRigEditorUtility.DuplicatePoint(rig, source);
            Assert.That(duplicate, Is.Not.Null);
            string duplicateKey = duplicate.BindingKey;
            string childKey = duplicate.transform.Find("Inactive child").GetComponent<UIMeshRigPoint>().BindingKey;
            Assert.That(duplicateKey, Is.Not.EqualTo("Source key"));
            Assert.That(childKey, Is.Not.EqualTo("Inactive key"));
            AssertDuplicate(rig, duplicateKey, childKey, restCenter, restAngle, restRigScale);
            Undo.FlushUndoRecordObjects();
            Undo.CollapseUndoOperations(groupId);
            Undo.IncrementCurrentGroup();
            Undo.PerformUndo();
            rig.NotifyPointChanged();
            Assert.That(duplicate == null, Is.True);
            Assert.That(rig.RigPoints.Count, Is.EqualTo(2));
            Assert.That(source.BindingKey, Is.EqualTo("Source key"));
            AssertPose(source.transform, new Vector3(10f, 9f, 0f), Quaternion.Euler(0f, 0f, 29f),
                new Vector3(1.2f, 0.7f, 1f));

            Undo.PerformRedo();
            rig.NotifyPointChanged();
            AssertDuplicate(rig, duplicateKey, childKey, restCenter, restAngle, restRigScale);
            SaveAndReopenOwnedScene();
            rig = GetRig(adapter);
            AssertDuplicate(rig, duplicateKey, childKey, restCenter, restAngle, restRigScale);
            UIMeshRigPoint restored = RequirePoint(rig, duplicateKey);
            restored.ResetPose(rig);
            AssertPose(restored.transform, restPosition, restRotation, restScale);
            AssertPose(RequirePoint(rig, "Source key").transform, new Vector3(10f, 9f, 0f),
                Quaternion.Euler(0f, 0f, 29f), new Vector3(1.2f, 0.7f, 1f));
        }

        [TestCase(typeof(UIMeshRigGraphic))]
        [TestCase(typeof(UIMeshRigWorldRenderer))]
        [TestCase(typeof(UIMeshRigSpriteRenderer))]
        public void InspectorApplyLayout_UndoRedo_DoesNotAbsorbUnrelatedUndoGroup(Type adapter)
        {
            IUIMeshRigOwner rig = CreateRig(adapter);
            UIMeshRigPoint original = CreatePoint(rig.RigTransform, "Original control", "Authored key");
            original.CaptureRestPose(rig);
            rig.NotifyPointChanged();
            Assert.That(rig.AuthoringMode, Is.EqualTo(UIMeshRigAuthoringMode.Setup));
            GameObject unrelated = new GameObject("Unrelated owned edit");
            unrelated.transform.localPosition = Vector3.right;
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Unrelated owned transform edit");
            Undo.RecordObject(unrelated.transform, "Unrelated owned transform edit");
            unrelated.transform.localPosition = Vector3.right * 9f;
            Undo.FlushUndoRecordObjects();

            // WHY: no increment here; the inspector's Apply Layout command must open its own Undo group.
            UIMeshRigPoint created = UIMeshRigEditorUtility.ApplyLayoutAsUndoStep(
                rig, UIMeshRigLayoutPreset.Character, false);
            Assert.That(created, Is.Not.Null);
            AssertLayout(rig);
            Undo.FlushUndoRecordObjects();
            Undo.IncrementCurrentGroup();
            Undo.PerformUndo();
            rig.NotifyPointChanged();
            Assert.That(unrelated.transform.localPosition, Is.EqualTo(Vector3.right * 9f),
                "Undoing ApplyLayout must not undo the preceding unrelated edit.");
            Assert.That(rig.AuthoringMode, Is.EqualTo(UIMeshRigAuthoringMode.Setup));
            Assert.That(rig.RigPoints.Count, Is.EqualTo(1));
            Assert.That(RequirePoint(rig, "Authored key"), Is.Not.Null);

            Undo.PerformUndo();
            Assert.That(unrelated.transform.localPosition, Is.EqualTo(Vector3.right));
            Undo.PerformRedo();
            Assert.That(unrelated.transform.localPosition, Is.EqualTo(Vector3.right * 9f));
            Assert.That(rig.RigPoints.Count, Is.EqualTo(1), "Redo of the unrelated group must not redo layout.");
            Undo.PerformRedo();
            rig.NotifyPointChanged();
            AssertLayout(rig);
            Assert.That(unrelated.transform.localPosition, Is.EqualTo(Vector3.right * 9f));
        }

        [TestCase(typeof(UIMeshRigGraphic))]
        [TestCase(typeof(UIMeshRigWorldRenderer))]
        [TestCase(typeof(UIMeshRigSpriteRenderer))]
        public void AddPoint_UndoRedoAndSceneReload_PreservesUniqueKeysPlacementAndRest(Type adapter)
        {
            IUIMeshRigOwner rig = CreateRig(adapter);
            int uiLayer = OffsetAddPointPlacement(rig);
            Undo.IncrementCurrentGroup();
            UIMeshRigPoint first = UIMeshRigEditorUtility.CreatePoint(rig);
            Undo.FlushUndoRecordObjects();
            Undo.IncrementCurrentGroup();
            UIMeshRigPoint second = UIMeshRigEditorUtility.CreatePoint(rig);
            Undo.FlushUndoRecordObjects();
            Undo.IncrementCurrentGroup();
            rig.NotifyPointChanged();
            AddedPointSnapshot firstExpected = new AddedPointSnapshot(first);
            AddedPointSnapshot secondExpected = new AddedPointSnapshot(second);
            Assert.That(firstExpected.Key, Is.EqualTo("Rig Point"));
            Assert.That(secondExpected.Key, Is.Not.Empty.And.Not.EqualTo(firstExpected.Key).And.Not.EqualTo("Point"),
                "The serialized default key must be replaced by a unique project-scheme key.");
            firstExpected.AssertDiffersFromCreationDefaults(uiLayer);
            secondExpected.AssertDiffersFromCreationDefaults(uiLayer);
            AssertAddedPoints(rig, firstExpected, secondExpected);

            Undo.PerformUndo();
            rig.NotifyPointChanged();
            Assert.That(second == null, Is.True, "The second Add Point is its own Undo step.");
            Assert.That(rig.RigPoints.Count, Is.EqualTo(1));
            Undo.PerformUndo();
            rig.NotifyPointChanged();
            Assert.That(first == null, Is.True);
            Assert.That(rig.RigPoints.Count, Is.Zero);

            Undo.PerformRedo();
            Undo.PerformRedo();
            rig.NotifyPointChanged();
            AssertAddedPoints(rig, firstExpected, secondExpected);
            SaveAndReopenOwnedScene();
            rig = GetRig(adapter);
            AssertAddedPoints(rig, firstExpected, secondExpected);
        }

        [TestCase("create")]
        [TestCase("sprite")]
        [TestCase("world")]
        [TestCase("convert")]
        [TestCase("child")]
        public void MenuCommand_SingleUndo_RevertsWholeCommandIncludingStarterLayout(string command)
        {
            GameObject host = command == "create"
                ? new GameObject("Owned menu canvas", typeof(RectTransform), typeof(Canvas))
                : new GameObject("Owned menu host", typeof(RectTransform));
            Assert.That(host.scene, Is.EqualTo(_scene));
            GameObject source = null;
            Image image = null;
            Button button = null;
            Color sourceColor = new Color(0.21f, 0.43f, 0.65f, 0.87f);
            if (command == "convert" || command == "child")
            {
                source = new GameObject("Owned interactive image",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                source.transform.SetParent(host.transform, false);
                image = source.GetComponent<Image>();
                image.color = sourceColor;
                button = source.GetComponent<Button>();
                button.targetGraphic = image;
            }

            string methodName;
            MenuCommand menuCommand;
            switch (command)
            {
                case "create":
                    methodName = "Create";
                    menuCommand = new MenuCommand(host);
                    break;
                case "sprite":
                    methodName = "CreateSpriteRenderer";
                    menuCommand = new MenuCommand(host);
                    break;
                case "world":
                    methodName = "CreateWorld";
                    menuCommand = new MenuCommand(host);
                    break;
                case "convert":
                    methodName = "ConvertImage";
                    menuCommand = new MenuCommand(image);
                    LogAssert.Expect(LogType.Warning, new Regex(@"^\[UI Mesh Rig\] The Image was replaced "));
                    break;
                default:
                    methodName = "CreateNonDestructiveChild";
                    menuCommand = new MenuCommand(image);
                    break;
            }

            MethodInfo menu = typeof(UIMeshRigMenu).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(menu, Is.Not.Null, methodName);
            GameObject[] rootsBefore = _scene.GetRootGameObjects();
            // WHY: Unity opens one Undo group per menu event; the command itself must not split that group.
            Undo.IncrementCurrentGroup();

            menu.Invoke(null, new object[] { menuCommand });

            IUIMeshRigOwner rig = FindOwnedRig(host);
            Assert.That(rig, Is.Not.Null, "The menu command must create or convert a rig.");
            rig.NotifyPointChanged();
            Assert.That(rig.RigPoints.Count, Is.GreaterThan(0), "The command applies its starter layout.");
            Assert.That(rig.AuthoringMode, Is.EqualTo(UIMeshRigAuthoringMode.Pose));
            if (button != null)
            {
                Assert.That(button.targetGraphic == (rig as Graphic), Is.True, "The Button must target the new rig.");
            }

            Undo.FlushUndoRecordObjects();
            Undo.IncrementCurrentGroup();
            Undo.PerformUndo();
            UIMeshRigMotionPreviewDriver.StopPreview(null);

            CollectionAssert.AreEqual(rootsBefore, _scene.GetRootGameObjects());
            Assert.That(FindOwnedRig(host), Is.Null, "One Undo must remove the rig together with its starter layout.");
            Assert.That(host.GetComponentsInChildren<UIMeshRigPoint>(true), Is.Empty);
            Assert.That(UIMeshRigMotionPreviewDriver.ActivePreviewCount, Is.EqualTo(_motionPreviewsBefore));
            switch (command)
            {
                case "convert":
                    Image restored = source.GetComponent<Image>();
                    Assert.That(restored != null, Is.True, "One Undo must restore the converted Image.");
                    Assert.That(restored.color, Is.EqualTo(sourceColor));
                    Assert.That(source.GetComponent<UIMeshRigGraphic>() == null, Is.True);
                    Assert.That(button.targetGraphic == restored, Is.True);
                    Assert.That(source.transform.childCount, Is.Zero);
                    break;
                case "child":
                    Assert.That(image.enabled, Is.True, "One Undo must re-enable the source Image.");
                    Assert.That(button.targetGraphic == image, Is.True);
                    Assert.That(source.transform.childCount, Is.Zero);
                    break;
                default:
                    Assert.That(host.transform.childCount, Is.Zero);
                    break;
            }
        }

        [TestCase(1)]
        [TestCase(16)]
        [TestCase(128)]
        [Category(NeoPerf.Category)]
        [Explicit(NeoPerf.ExplicitReason)]
        public void Perf_MotionPreviewDriverTick_PlayingPausedStopped(int previews)
        {
            const int ticksPerSample = 1;
            IUIMeshRigOwner rig = CreateRig(typeof(UIMeshRigGraphic));
            List<UIMeshRigPointMotion> motions = new List<UIMeshRigPointMotion>(previews);
            for (int index = 0; index < previews; index++)
            {
                UIMeshRigPoint point = CreatePoint(rig.RigTransform, "Preview point " + index, "Preview key " + index);
                point.CaptureRestPose(rig);
                UIMeshRigPointMotion motion = point.gameObject.AddComponent<UIMeshRigPointMotion>();
                motion.ApplyPreset(UIMeshRigMotionPreset.Breathe);
                motion.PlayOnEnable = false;
                motions.Add(motion);
            }

            rig.NotifyPointChanged();
            Action tick = () => UIMeshRigMotionPreviewDriver.TickForTests(PreviewTickSeconds);
            try
            {
                for (int index = 0; index < motions.Count; index++)
                    UIMeshRigMotionPreviewDriver.StartPreview(motions[index]);
                Assert.That(UIMeshRigMotionPreviewDriver.ActivePreviewCount, Is.EqualTo(previews));
                Assert.That(UIMeshRigMotionPreviewDriver.IsUpdateSubscribed, Is.True);

                int playingTicks = MeasureEditorPerf("ui.meshrig.preview_tick.playing.n" + previews, previews,
                    ticksPerSample, tick, "evaluations_per_tick=" + previews + " update_subscribed=1");
                for (int index = 0; index < motions.Count; index++)
                    Assert.That(motions[index].CurrentTime,
                        Is.EqualTo(playingTicks * PreviewTickSeconds).Within(0.001f),
                        "Every playing preview must be evaluated once per measured tick.");

                for (int index = 0; index < motions.Count; index++)
                    UIMeshRigMotionPreviewDriver.PausePreview(motions[index]);
                Assert.That(UIMeshRigMotionPreviewDriver.IsUpdateSubscribed, Is.False,
                    "Paused previews must not keep the editor update subscription.");
                float pausedTime = motions[0].CurrentTime;
                MeasureEditorPerf("ui.meshrig.preview_tick.paused.n" + previews, previews, ticksPerSample, tick,
                    "evaluations_per_tick=0 update_subscribed=0");
                for (int index = 0; index < motions.Count; index++)
                    Assert.That(motions[index].CurrentTime, Is.EqualTo(pausedTime), "Paused previews must not evaluate.");
                Assert.That(UIMeshRigMotionPreviewDriver.ActivePreviewCount, Is.EqualTo(previews));

                for (int index = 0; index < motions.Count; index++)
                    UIMeshRigMotionPreviewDriver.StopPreview(motions[index]);
                Assert.That(UIMeshRigMotionPreviewDriver.ActivePreviewCount, Is.EqualTo(_motionPreviewsBefore));
                Assert.That(UIMeshRigMotionPreviewDriver.IsUpdateSubscribed, Is.False);
                MeasureEditorPerf("ui.meshrig.preview_tick.stopped.n" + previews, previews, ticksPerSample, tick,
                    "evaluations_per_tick=0 update_subscribed=0");
                Assert.That(UIMeshRigMotionPreviewDriver.IsUpdateSubscribed, Is.False);
            }
            finally
            {
                for (int index = 0; index < motions.Count; index++)
                {
                    if (motions[index] != null && motions[index].PreviewInEditMode)
                        UIMeshRigMotionPreviewDriver.StopPreview(motions[index]);
                }
            }
        }

        [Test]
        public void ConvertImage_PrefabInstance_SaveReopen_PreservesSelectableOverride()
        {
            GameObject template = new GameObject("Owned button prefab", typeof(RectTransform), typeof(Image), typeof(Button));
            Image source = template.GetComponent<Image>();
            source.color = new Color(0.13f, 0.27f, 0.61f, 0.74f);
            template.GetComponent<Button>().targetGraphic = source;
            string prefabPath = _folder + "/Button.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(template, prefabPath);
            Assert.That(prefab, Is.Not.Null);
            string guid = AssetDatabase.AssetPathToGUID(prefabPath);
            string prefabDiskPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", prefabPath));
            byte[] prefabBefore = File.ReadAllBytes(prefabDiskPath);
            Object.DestroyImmediate(template);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _scene);
            Assert.That(instance.scene, Is.EqualTo(_scene));
            instance.name = "Converted prefab instance";
            MethodInfo convert = typeof(UIMeshRigMenu).GetMethod("ConvertImage", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(convert, Is.Not.Null);
            LogAssert.Expect(LogType.Warning, new Regex(@"^\[UI Mesh Rig\] The Image was replaced "));
            Undo.IncrementCurrentGroup();

            convert.Invoke(null, new object[] { new MenuCommand(instance.GetComponent<Image>()) });

            AssertConverted(instance, prefab);
            SaveAndReopenOwnedScene(prefabPath);
            GameObject reloadedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(reloadedPrefab, Is.Not.Null);
            GameObject reopened = null;
            foreach (GameObject root in _scene.GetRootGameObjects())
                if (root.name == "Converted prefab instance") reopened = root;
            Assert.That(reopened, Is.Not.Null);
            Assert.That(PrefabUtility.GetPrefabInstanceStatus(reopened), Is.EqualTo(PrefabInstanceStatus.Connected));
            AssertConverted(reopened, reloadedPrefab);
            Assert.That(reloadedPrefab.GetComponent<Image>(), Is.Not.Null);
            Assert.That(reloadedPrefab.GetComponent<UIMeshRigGraphic>(), Is.Null, "Conversion must remain an instance override.");
            Assert.That(reloadedPrefab.GetComponent<Button>().targetGraphic, Is.SameAs(reloadedPrefab.GetComponent<Image>()));
            CollectionAssert.AreEqual(prefabBefore, File.ReadAllBytes(prefabDiskPath));
            Assert.That(AssetDatabase.AssetPathToGUID(prefabPath), Is.EqualTo(guid));
        }

        private static void AssertLayout(IUIMeshRigOwner rig)
        {
            rig.NotifyPointChanged();
            Assert.That(rig.AuthoringMode, Is.EqualTo(UIMeshRigAuthoringMode.Pose));
            int count = UIMeshRigLayoutPresets.GetPointCount(UIMeshRigLayoutPreset.Character);
            Assert.That(rig.RigPoints.Count, Is.EqualTo(count));
            for (int index = 0; index < count; index++)
            {
                UIMeshRigPointLayout expected = UIMeshRigLayoutPresets.GetPoint(UIMeshRigLayoutPreset.Character, index);
                UIMeshRigPoint point = RequirePoint(rig, expected.Name);
                Assert.That(Vector2.Distance(point.RestCenterNormalized, expected.CenterNormalized), Is.LessThan(0.0001f));
                if (expected.MotionPreset == UIMeshRigMotionPreset.Custom) continue;
                UIMeshRigPointMotion motion = point.GetComponent<UIMeshRigPointMotion>();
                Assert.That(motion, Is.Not.Null);
                Assert.That(motion.Preset, Is.EqualTo(expected.MotionPreset));
                Assert.That(motion.Phase, Is.EqualTo(expected.Phase).Within(0.0001f));
                Assert.That(motion.Seed, Is.EqualTo(expected.Seed));
                Assert.That(motion.PlayOnEnable, Is.True);
            }
        }

        private static void AssertConverted(GameObject instance, GameObject prefab)
        {
            UIMeshRigGraphic rig = instance.GetComponent<UIMeshRigGraphic>();
            Button button = instance.GetComponent<Button>();
            Assert.That(rig, Is.Not.Null);
            Assert.That(button, Is.Not.Null);
            Assert.That(instance.GetComponent<Image>(), Is.Null);
            Assert.That(button.targetGraphic, Is.SameAs(rig));
            Assert.That(rig.color, Is.EqualTo(new Color(0.13f, 0.27f, 0.61f, 0.74f)));
            PropertyModification[] modifications = PrefabUtility.GetPropertyModifications(instance);
            Assert.That(modifications, Is.Not.Null);
            bool retarget = false;
            foreach (PropertyModification modification in modifications)
                if (modification.target == prefab.GetComponent<Button>() && modification.propertyPath == "m_TargetGraphic"
                    && modification.objectReference == rig) retarget = true;
            Assert.That(retarget, Is.True, "The actual saved prefab instance must retain its selectable graphic override.");
        }

        private IUIMeshRigOwner CreateRig(Type adapter)
        {
            GameObject root = new GameObject("Owned rig", typeof(RectTransform), adapter);
            Assert.That(root.scene, Is.EqualTo(_scene));
            ((RectTransform)root.transform).sizeDelta = new Vector2(400f, 300f);
            return (IUIMeshRigOwner)root.GetComponent(adapter);
        }

        /// <summary>
        /// Moves the rig to the UI layer and off-centres its coordinate space, so the layer and the captured rest
        /// pose of an added point differ from the values a freshly constructed point would get after a lost Redo.
        /// </summary>
        private static int OffsetAddPointPlacement(IUIMeshRigOwner rig)
        {
            int uiLayer = LayerMask.NameToLayer("UI");
            Assert.That(uiLayer, Is.GreaterThan(0), "The built-in UI layer is required.");
            rig.RigTransform.gameObject.layer = uiLayer;
            if (rig is UIMeshRigGraphic)
            {
                ((RectTransform)rig.RigTransform).pivot = Vector2.zero;
            }
            else if (rig is UIMeshRigWorldRenderer)
            {
                using SerializedObject serialized = new SerializedObject((Object)rig);
                SerializedProperty pivot = serialized.FindProperty("_pivot");
                Assert.That(pivot, Is.Not.Null);
                pivot.vector2Value = Vector2.zero;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            return uiLayer;
        }

        private static IUIMeshRigOwner FindOwnedRig(GameObject host)
        {
            foreach (MonoBehaviour behaviour in host.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour is IUIMeshRigOwner owner) return owner;
            return null;
        }

        private const float PreviewTickSeconds = 1f / 60f;

        /// <summary>
        /// Prints one NEO_PERF line through the shared <see cref="NeoPerf" /> writer (Stopwatch percentiles, calibrated
        /// GC.Alloc counts) and returns how many times <paramref name="operation" /> ran.
        /// </summary>
        private static int MeasureEditorPerf(string caseId, int scale, int operationsPerSample, Action operation,
            string extraKeys)
        {
            const int samples = 120;
            NeoPerf.MeasureSynchronous(caseId, scale, operationsPerSample, NeoPerf.DefaultWarmupSamples, samples,
                    operation)
                .AddPairs(extraKeys)
                .Emit();
            return NeoPerf.CallsPerMeasure(NeoPerf.DefaultWarmupSamples, samples) * operationsPerSample;
        }

        private IUIMeshRigOwner GetRig(Type adapter)
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
                if (root.name == "Owned rig") return (IUIMeshRigOwner)root.GetComponent(adapter);
            Assert.Fail("The owned rig must survive native scene reload.");
            return null;
        }

        private static UIMeshRigPoint CreatePoint(Transform parent, string name, string key)
        {
            GameObject target = new GameObject(name, typeof(RectTransform), typeof(UIMeshRigPoint));
            target.transform.SetParent(parent, false);
            UIMeshRigPoint point = target.GetComponent<UIMeshRigPoint>();
            point.SetBindingKey(key);
            return point;
        }

        private static void ConfigureMotion(UIMeshRigPointMotion motion)
        {
            motion.ApplyPreset(UIMeshRigMotionPreset.Custom);
            motion.Profile.Duration = 3.25f;
            motion.Profile.PositionAmplitudePixels = new Vector2(4f, 9f);
            motion.Profile.RotationAmplitudeDegrees = 13f;
            motion.Profile.ScaleAmplitude = new Vector2(0.07f, 0.12f);
            motion.Profile.PositionX = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            motion.Phase = 0.37f;
            motion.Seed = 47;
            motion.Speed = 1.4f;
            motion.PlayOnEnable = false;
            motion.PreviewInEditMode = false;
        }

        private static UIMeshRigPoint RequirePoint(IUIMeshRigOwner rig, string key)
        {
            UIMeshRigPoint result = null;
            rig.NotifyPointChanged();
            foreach (UIMeshRigPoint point in rig.RigPoints)
                if (point.BindingKey == key)
                {
                    Assert.That(result, Is.Null, "Binding keys must be unique: " + key);
                    result = point;
                }
            Assert.That(result, Is.Not.Null, "Missing persisted binding key: " + key);
            return result;
        }

        private static void AssertDuplicate(IUIMeshRigOwner rig, string key, string childKey,
            Vector2 restCenter, float restAngle, Vector3 restScale)
        {
            Assert.That(rig.AuthoringMode, Is.EqualTo(UIMeshRigAuthoringMode.Pose));
            UIMeshRigPoint source = RequirePoint(rig, "Source key");
            UIMeshRigPoint duplicate = RequirePoint(rig, key);
            UIMeshRigPoint inactive = RequirePoint(rig, childKey);
            Assert.That(rig.RigPoints.Count, Is.EqualTo(4));
            HashSet<string> keys = new HashSet<string>();
            foreach (UIMeshRigPoint point in rig.RigPoints)
                Assert.That(!string.IsNullOrEmpty(point.BindingKey) && keys.Add(point.BindingKey), Is.True);
            Assert.That(duplicate.transform.parent, Is.SameAs(source.transform.parent));
            Assert.That(duplicate.transform.parent.name, Is.EqualTo("Transformed group"));
            AssertPose(duplicate.transform.parent, new Vector3(40f, -30f, 0f), Quaternion.Euler(0f, 0f, 17f),
                new Vector3(1.5f, 0.8f, 1f));
            AssertPose(source.transform, new Vector3(10f, 9f, 0f), Quaternion.Euler(0f, 0f, 29f),
                new Vector3(1.2f, 0.7f, 1f));
            AssertPose(duplicate.transform, new Vector3(10f, 9f, 0f), Quaternion.Euler(0f, 0f, 29f),
                new Vector3(1.2f, 0.7f, 1f));
            Assert.That(Vector3.Distance(duplicate.transform.position, source.transform.position), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(duplicate.transform.rotation, source.transform.rotation), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(duplicate.transform.lossyScale, source.transform.lossyScale), Is.LessThan(0.0001f));
            Assert.That(duplicate.RestCenterNormalized, Is.EqualTo(restCenter));
            Assert.That(duplicate.RestRotationDegrees, Is.EqualTo(restAngle).Within(0.0001f));
            Assert.That(duplicate.RestScale, Is.EqualTo(restScale));
            Assert.That(inactive.transform.parent, Is.SameAs(duplicate.transform));
            Assert.That(inactive.gameObject.activeSelf, Is.False);
            Button copiedButton = duplicate.GetComponent<Button>();
            Image copiedImage = duplicate.transform.Find("Internal graphic").GetComponent<Image>();
            Button external = rig.RigTransform.Find("External reference").GetComponent<Button>();
            Assert.That(copiedButton.targetGraphic, Is.SameAs(copiedImage));
            Assert.That(copiedImage, Is.Not.SameAs(source.GetComponent<Button>().targetGraphic));
            Assert.That(copiedButton.navigation.selectOnRight, Is.SameAs(external));
            Assert.That(source.GetComponent<Button>().navigation.selectOnRight, Is.SameAs(external));
            UIMeshRigPointMotion motion = duplicate.GetComponent<UIMeshRigPointMotion>();
            Assert.That(motion, Is.Not.Null);
            Assert.That(motion.Preset, Is.EqualTo(UIMeshRigMotionPreset.Custom));
            Assert.That(motion.Profile.Duration, Is.EqualTo(3.25f).Within(0.0001f));
            Assert.That(motion.Profile.PositionAmplitudePixels, Is.EqualTo(new Vector2(4f, 9f)));
            Assert.That(motion.Profile.RotationAmplitudeDegrees, Is.EqualTo(13f));
            Assert.That(motion.Profile.ScaleAmplitude, Is.EqualTo(new Vector2(0.07f, 0.12f)));
            Assert.That(motion.Profile.PositionX.length, Is.EqualTo(2));
            Assert.That(motion.Profile.PositionX.Evaluate(0.35f), Is.EqualTo(0.35f).Within(0.0001f));
            Assert.That(motion.Phase, Is.EqualTo(0.37f).Within(0.0001f));
            Assert.That(motion.Seed, Is.EqualTo(47));
            Assert.That(motion.Speed, Is.EqualTo(1.4f).Within(0.0001f));
            Assert.That(motion.PlayOnEnable, Is.False);
            Assert.That(motion.PreviewInEditMode, Is.False);
        }

        private static void AssertAddedPoints(IUIMeshRigOwner rig, AddedPointSnapshot first, AddedPointSnapshot second)
        {
            rig.NotifyPointChanged();
            Assert.That(rig.RigPoints.Count, Is.EqualTo(2));
            first.AssertMatches(RequirePoint(rig, first.Key));
            second.AssertMatches(RequirePoint(rig, second.Key));
        }

        private readonly struct AddedPointSnapshot
        {
            internal readonly string Key;
            private readonly string _name;
            private readonly int _layer;
            private readonly Vector3 _localPosition;
            private readonly Vector2 _anchorMin;
            private readonly Vector2 _anchorMax;
            private readonly Vector2 _sizeDelta;
            private readonly Vector2 _restCenter;
            private readonly float _restAngle;
            private readonly Vector3 _restScale;
            private readonly Vector3 _restLocalPosition;

            internal AddedPointSnapshot(UIMeshRigPoint point)
            {
                RectTransform rect = (RectTransform)point.transform;
                Key = point.BindingKey;
                _name = point.gameObject.name;
                _layer = point.gameObject.layer;
                _localPosition = rect.localPosition;
                _anchorMin = rect.anchorMin;
                _anchorMax = rect.anchorMax;
                _sizeDelta = rect.sizeDelta;
                _restCenter = point.RestCenterNormalized;
                _restAngle = point.RestRotationDegrees;
                _restScale = point.RestScale;
                _restLocalPosition = ReadRestLocalPosition(point);
            }

            /// <summary>Proves the snapshot can detect a Redo that restores an unconfigured point.</summary>
            internal void AssertDiffersFromCreationDefaults(int expectedLayer)
            {
                Assert.That(_layer, Is.EqualTo(expectedLayer), "Add Point must copy the rig layer.");
                Assert.That(_restLocalPosition.sqrMagnitude, Is.GreaterThan(0.0001f),
                    "Precondition: the captured rest position must differ from the serialized default.");
            }

            internal void AssertMatches(UIMeshRigPoint point)
            {
                RectTransform rect = (RectTransform)point.transform;
                Assert.That(point.BindingKey, Is.EqualTo(Key));
                Assert.That(point.gameObject.name, Is.EqualTo(_name));
                Assert.That(point.gameObject.layer, Is.EqualTo(_layer));
                Assert.That(Vector3.Distance(rect.localPosition, _localPosition), Is.LessThan(0.0001f));
                Assert.That(Vector2.Distance(rect.anchorMin, _anchorMin), Is.LessThan(0.0001f));
                Assert.That(Vector2.Distance(rect.anchorMax, _anchorMax), Is.LessThan(0.0001f));
                Assert.That(Vector2.Distance(rect.sizeDelta, _sizeDelta), Is.LessThan(0.0001f),
                    "Add Point placement must survive Redo instead of returning to the default RectTransform size.");
                Assert.That(Vector2.Distance(point.RestCenterNormalized, _restCenter), Is.LessThan(0.0001f));
                Assert.That(point.RestRotationDegrees, Is.EqualTo(_restAngle).Within(0.0001f));
                Assert.That(Vector3.Distance(point.RestScale, _restScale), Is.LessThan(0.0001f));
                Assert.That(Vector3.Distance(ReadRestLocalPosition(point), _restLocalPosition), Is.LessThan(0.0001f),
                    "The captured rest pose must survive Redo and scene reload.");
            }

            private static Vector3 ReadRestLocalPosition(UIMeshRigPoint point)
            {
                using SerializedObject serialized = new SerializedObject(point);
                SerializedProperty property = serialized.FindProperty("_restLocalPosition");
                Assert.That(property, Is.Not.Null);
                return property.vector3Value;
            }
        }

        private static void SetPose(Transform target, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            target.localPosition = position;
            target.localRotation = rotation;
            target.localScale = scale;
        }

        private static void AssertPose(Transform target, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Assert.That(Vector3.Distance(target.localPosition, position), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(target.localRotation, rotation), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(target.localScale, scale), Is.LessThan(0.0001f));
        }

        private void SaveAndReopenOwnedScene(string reimportPrefab = null)
        {
            Assert.That(_scene.path, Is.EqualTo(_scenePath));
            Assert.That(EditorSceneManager.IsPreviewScene(_scene), Is.False);
            EditorSceneManager.MarkSceneDirty(_scene);
            Assert.That(EditorSceneManager.SaveScene(_scene, _scenePath), Is.True);
            CloseOwnedScene();
            if (!string.IsNullOrEmpty(reimportPrefab))
                AssetDatabase.ImportAsset(reimportPrefab, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            _scene = EditorSceneManager.OpenScene(_scenePath, OpenSceneMode.Additive);
            Assert.That(SceneManager.SetActiveScene(_scene), Is.True);
        }

        private void CloseOwnedScene()
        {
            if (!_scene.IsValid() || !_scene.isLoaded) return;
            try
            {
                Assert.That(_scene.path, Is.EqualTo(_scenePath));
                foreach (GameObject root in _scene.GetRootGameObjects())
                    foreach (Transform target in root.GetComponentsInChildren<Transform>(true))
                    {
                        UIMeshRigPointMotion motion = target.GetComponent<UIMeshRigPointMotion>();
                        if (motion != null) motion.PreviewInEditMode = false;
                        foreach (Component component in target.GetComponents<Component>())
                            if (component != null) Undo.ClearUndo(component);
                        Undo.ClearUndo(target.gameObject);
                    }
            }
            finally
            {
                if (_activeBefore.IsValid() && _activeBefore.isLoaded) SceneManager.SetActiveScene(_activeBefore);
                bool closed = EditorSceneManager.CloseScene(_scene, true);
                if (closed) _scene = default;
                Assert.That(closed, Is.True, "The exact owned regular scene must close even if point cleanup failed.");
            }
        }

        private void AssertBorrowedContext()
        {
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_activeBefore));
            Assert.That(EditorSceneManager.previewSceneCount, Is.EqualTo(_previewCountBefore));
            Assert.That(UIMeshRigMotionPreviewDriver.ActivePreviewCount, Is.EqualTo(_motionPreviewsBefore),
                "Owned previews must not leak and borrowed previews must not be cleared.");
            Assert.That(SceneManager.sceneCount, Is.EqualTo(_borrowed.Count));
            for (int index = 0; index < _borrowed.Count; index++)
            {
                BorrowedScene prior = _borrowed[index];
                Scene current = SceneManager.GetSceneAt(index);
                Assert.That(current, Is.EqualTo(prior.Scene));
                Assert.That(current.path, Is.EqualTo(prior.Path));
                Assert.That(current.isLoaded, Is.EqualTo(prior.Loaded));
                Assert.That(current.isDirty, Is.EqualTo(prior.Dirty));
                if (prior.Loaded) CollectionAssert.AreEqual(prior.Roots, current.GetRootGameObjects());
            }
            CollectionAssert.AreEqual(_selectionBefore, Selection.objects);
        }

        private readonly struct BorrowedScene
        {
            internal readonly Scene Scene;
            internal readonly string Path;
            internal readonly bool Loaded;
            internal readonly bool Dirty;
            internal readonly GameObject[] Roots;
            internal BorrowedScene(Scene scene)
            {
                Scene = scene;
                Path = scene.path;
                Loaded = scene.isLoaded;
                Dirty = scene.isDirty;
                Roots = Loaded ? scene.GetRootGameObjects() : Array.Empty<GameObject>();
            }
        }
    }
}
