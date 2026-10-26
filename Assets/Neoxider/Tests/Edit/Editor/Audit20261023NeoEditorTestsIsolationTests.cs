using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Bonus;
using Neo.Save;
using Neo.Settings;
using Neo.Tools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoEditorTestsIsolationTests
    {
        [Test]
        public void PaylineFixtureCleanup_ReleasesItsGeneratedMultiplierAsset()
        {
            HashSet<SpriteMultiplayerData> before = new HashSet<SpriteMultiplayerData>(
                Resources.FindObjectsOfTypeAll<SpriteMultiplayerData>());
            List<SpriteMultiplayerData> created = new List<SpriteMultiplayerData>();
            SpinControllerPaylineTests fixture = new SpinControllerPaylineTests();
            try
            {
                try
                {
                    fixture.GetMultiplayers_ReturnsConfiguredMultiplier_AndPayoutMatchesHandComputed();
                    foreach (SpriteMultiplayerData data in Resources.FindObjectsOfTypeAll<SpriteMultiplayerData>())
                    {
                        if (!before.Contains(data) && !EditorUtility.IsPersistent(data))
                        {
                            created.Add(data);
                        }
                    }
                }
                finally
                {
                    fixture.TearDown();
                }

                Assert.That(created, Has.Count.EqualTo(1));
                Assert.That(created[0] == null, Is.True);
            }
            finally
            {
                foreach (SpriteMultiplayerData data in created)
                {
                    Object.DestroyImmediate(data);
                }
            }
        }

        [TestCase("Channels")]
        [TestCase("Entries")]
        [TestCase("Manager")]
        [TestCase("RandomMusic")]
        public void AudioFixtureCleanup_ReleasesTheClipsCreatedByItsTest(string fixtureName)
        {
            HashSet<AudioClip> before = new HashSet<AudioClip>(Resources.FindObjectsOfTypeAll<AudioClip>());
            List<AudioClip> created = new List<AudioClip>();
            GetAudioFixture(fixtureName, out Action setUp, out Action exercise, out Action tearDown);
            try
            {
                try
                {
                    setUp();
                    exercise();
                    foreach (AudioClip clip in Resources.FindObjectsOfTypeAll<AudioClip>())
                    {
                        if (!before.Contains(clip) && !EditorUtility.IsPersistent(clip))
                        {
                            created.Add(clip);
                        }
                    }
                }
                finally
                {
                    tearDown();
                }

                Assert.That(created, Is.Not.Empty, "The exercised fixture must create native clips.");
                foreach (AudioClip clip in created)
                {
                    Assert.That(clip == null, Is.True, "Fixture teardown must release each generated native clip.");
                }
            }
            finally
            {
                foreach (AudioClip clip in created)
                {
                    Object.DestroyImmediate(clip);
                }
            }
        }

        [TestCase("Bonus")]
        [TestCase("Cooldown")]
        [TestCase("Quest")]
        [TestCase("Slots")]
        [TestCase("Shop")]
        [TestCase("ShopRollback")]
        [TestCase("ShopAffordability")]
        [TestCase("ShopVariants")]
        [TestCase("MoneyAudit")]
        [TestCase("MoneyPersistence")]
        [TestCase("Progression")]
        [TestCase("Rpg")]
        [TestCase("Selector")]
        [TestCase("Inventory")]
        [TestCase("Settings")]
        [TestCase("SaveProvider")]
        [TestCase("SaveManager")]
        public void SaveFixtureLifecycle_PreservesPreviousProviderAndUnrelatedValues(string fixtureName)
        {
            using (Audit20261023NeoEditorTestsSaveScope owner = new())
            {
                ISaveProvider previous = SaveProvider.CurrentProvider;
                previous.SetString("Unrelated.OwnerData", "keep");
                GetSaveFixture(fixtureName, out Action setUp, out Action tearDown);

                try
                {
                    setUp();
                }
                finally
                {
                    tearDown();
                }

                Assert.That(SaveProvider.CurrentProvider, Is.SameAs(previous));
                Assert.That(previous.GetString("Unrelated.OwnerData"), Is.EqualTo("keep"));
                SaveProvider.SetInt("AfterFixture", 42);
                Assert.That(previous.GetInt("AfterFixture"), Is.EqualTo(42));
            }
        }

        [Test]
        public void SaveScope_RestoresProviderAfterFailure_WithoutLeakingTemporaryValues()
        {
            using (Audit20261023NeoEditorTestsSaveScope owner = new())
            {
                ISaveProvider previous = SaveProvider.CurrentProvider;
                Assert.Throws<InvalidOperationException>(() =>
                {
                    using (Audit20261023NeoEditorTestsSaveScope nested = new())
                    {
                        SaveProvider.SetString("Temporary", "discard");
                        SaveProvider.Save();
                        throw new InvalidOperationException("simulated assertion failure");
                    }
                });

                Assert.That(SaveProvider.CurrentProvider, Is.SameAs(previous));
                Assert.That(previous.HasKey("Temporary"), Is.False);
            }
        }

        [TestCase("ShopRollback", false)]
        [TestCase("ShopRollback", true)]
        [TestCase("ShopAffordability", false)]
        [TestCase("ShopAffordability", true)]
        [TestCase("ShopVariants", false)]
        [TestCase("ShopVariants", true)]
        public void ShopFixtureWritesAndFailureCleanup_PreserveAmbientProfile(string name, bool fail)
        {
            using Audit20261023NeoEditorTestsSaveScope owner = new Audit20261023NeoEditorTestsSaveScope();
            ISaveProvider previous = SaveProvider.CurrentProvider;
            previous.SetString("Shop", "foreign profile");
            previous.SetString("Test_ShopVariantsPanel", "foreign variants");
            GetSaveFixture(name, out Action setUp, out Action tearDown);
            Action run = () =>
            {
                try
                {
                    setUp();
                    SaveProvider.SetString("Shop", "owned profile write");
                    SaveProvider.SetString("Test_ShopVariantsPanel", "owned variants write");
                    SaveProvider.Save();
                    if (fail) throw new InvalidOperationException("simulated assertion failure");
                }
                finally { tearDown(); }
            };
            if (fail) Assert.Throws<InvalidOperationException>(() => run());
            else run();
            Assert.That(SaveProvider.CurrentProvider, Is.SameAs(previous));
            Assert.That(previous.GetString("Shop"), Is.EqualTo("foreign profile"));
            Assert.That(previous.GetString("Test_ShopVariantsPanel"), Is.EqualTo("foreign variants"));
        }

        [TestCase("Bonus", false)]
        [TestCase("Bonus", true)]
        [TestCase("Cooldown", false)]
        [TestCase("Cooldown", true)]
        public void RewardFixture_RestoresExactClockLazyStateAndProvider(string name, bool fail)
        {
            using Audit20261023NeoEditorTestsClockScope owner = new Audit20261023NeoEditorTestsClockScope();
            owner.Advance(86400d * 365d);
            TrustedClock previousClock = NeoTrustedTime.Clock;
            ISaveProvider previousProvider = SaveProvider.CurrentProvider;
            FieldInfo clock = TrustedTimeField("_clock");
            FieldInfo restored = TrustedTimeField("_restored");
            FieldInfo highWater = TrustedTimeField("_persistedHighWater");
            DateTime sentinel = owner.UtcNow.AddSeconds(-23d);
            restored.SetValue(null, false);
            highWater.SetValue(null, sentinel);
            previousProvider.SetString("Foreign.Reward", "keep");
            GetSaveFixture(name, out Action setUp, out Action tearDown);
            Action run = () =>
            {
                try
                {
                    setUp();
                    Assert.That(NeoTrustedTime.UtcNow.Year, Is.EqualTo(2030));
                    if (fail) throw new InvalidOperationException("simulated assertion failure");
                }
                finally { tearDown(); }
            };
            if (fail) Assert.Throws<InvalidOperationException>(() => run());
            else run();
            Assert.That(clock.GetValue(null), Is.SameAs(previousClock));
            Assert.That(restored.GetValue(null), Is.False);
            Assert.That(highWater.GetValue(null), Is.EqualTo(sentinel));
            Assert.That(SaveProvider.CurrentProvider, Is.SameAs(previousProvider));
            Assert.That(previousProvider.GetString("Foreign.Reward"), Is.EqualTo("keep"));
        }

        private static FieldInfo TrustedTimeField(string name)
        {
            FieldInfo field = typeof(NeoTrustedTime).GetField(name, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            return field;
        }

        [Test]
        public void DiagnosticsFixtures_RestoreNonDefaultFlags()
        {
            bool logs = NeoDiagnostics.RuntimeLogsEnabled;
            bool warnings = NeoDiagnostics.RuntimeWarningsEnabled;
            bool errors = NeoDiagnostics.RuntimeErrorsEnabled;
            try
            {
                NeoDiagnostics.Configure(true, true, false);
                AmChannelEditModeTests audio = new AmChannelEditModeTests();
                try
                {
                    audio.SetUp();
                    NeoDiagnostics.Configure(false, false, true);
                }
                finally { audio.TearDown(); }
                Assert.That(NeoDiagnostics.RuntimeLogsEnabled, Is.True);
                Assert.That(NeoDiagnostics.RuntimeWarningsEnabled, Is.True);
                Assert.That(NeoDiagnostics.RuntimeErrorsEnabled, Is.False);
                Neo.Tests.Edit.AuditFixesAnimationFlyTests fly = new Neo.Tests.Edit.AuditFixesAnimationFlyTests();
                try { fly.SetUp(); }
                finally { fly.RestoreDiagnostics(); }
                Assert.That(NeoDiagnostics.RuntimeErrorsEnabled, Is.False);
            }
            finally { NeoDiagnostics.Configure(logs, warnings, errors); }
        }

        [Test]
        public void CursorFixture_RestoresDisabledManagementSentinel()
        {
            bool management = CursorLockController.GlobalCursorManagement;
            CursorLockMode mode = Cursor.lockState;
            bool visible = Cursor.visible;
            try
            {
                CursorLockController.GlobalCursorManagement = false;
                Neo.Tests.Edit.CursorLockPlayerControlTests fixture = new Neo.Tests.Edit.CursorLockPlayerControlTests();
                try { fixture.SetUp(); }
                finally { fixture.TearDown(); }
                Assert.That(CursorLockController.GlobalCursorManagement, Is.False);
            }
            finally
            {
                CursorLockController.GlobalCursorManagement = management;
                Cursor.lockState = mode;
                Cursor.visible = visible;
            }
        }

        [Test]
        public void SceneFlowFixture_RestoresTheExactInjectedLoader()
        {
            Neo.Level.ISceneFlowSceneLoader previous = Neo.Level.SceneFlowController.SceneLoader;
            Neo.Level.ISceneFlowSceneLoader sentinel = new SentinelSceneLoader();
            try
            {
                Neo.Level.SceneFlowController.SceneLoader = sentinel;
                Neo.Level.Tests.SceneFlowControllerTests fixture = new Neo.Level.Tests.SceneFlowControllerTests();
                try { fixture.SetUp(); }
                finally { fixture.TearDown(); }
                Assert.That(Neo.Level.SceneFlowController.SceneLoader, Is.SameAs(sentinel));
            }
            finally { Neo.Level.SceneFlowController.SceneLoader = previous; }
        }

        private sealed class SentinelSceneLoader : Neo.Level.ISceneFlowSceneLoader
        {
            public int ActiveSceneBuildIndex => 42;
            public void LoadScene(int index, LoadSceneMode mode = LoadSceneMode.Single) { }
            public void LoadScene(string name, LoadSceneMode mode = LoadSceneMode.Single) { }
            public AsyncOperation LoadSceneAsync(int index, LoadSceneMode mode = LoadSceneMode.Single) => null;
            public AsyncOperation LoadSceneAsync(string name, LoadSceneMode mode = LoadSceneMode.Single) => null;
        }

        [Test]
        public void TransformAnimatorFixture_RestoresRandomStateAfterAssertionFailure()
        {
            UnityEngine.Random.State previous = UnityEngine.Random.state;
            try
            {
                UnityEngine.Random.InitState(173);
                UnityEngine.Random.State sentinel = UnityEngine.Random.state;
                Animations.TransformAnimatorTests fixture = new Animations.TransformAnimatorTests();
                Assert.Throws<InvalidOperationException>(() =>
                {
                    try
                    {
                        fixture.CaptureRandomState();
                        fixture.Component_Stop_RestoresBasePose();
                        throw new InvalidOperationException("simulated assertion failure");
                    }
                    finally { fixture.RestoreRandomState(); }
                });
                Assert.That(UnityEngine.Random.state, Is.EqualTo(sentinel));
            }
            finally { UnityEngine.Random.state = previous; }
        }

        [Test]
        public void TimerFixture_PreservesAmbientFixedKeySentinels()
        {
            using Audit20261023NeoEditorTestsSaveScope owner = new Audit20261023NeoEditorTestsSaveScope();
            ISaveProvider previous = SaveProvider.CurrentProvider;
            const string oldKey = "TimerObjectRealTimePausedTest";
            previous.SetFloat(oldKey + "_t", 42.5f);
            previous.SetBool(oldKey + "_a", true);
            previous.SetString(oldKey + "_rt", "foreign deadline");
            new TimerTests().TimerObject_RealTimeSave_PausedTimer_RestoresPausedFrozenTime();
            Assert.That(SaveProvider.CurrentProvider, Is.SameAs(previous));
            Assert.That(previous.GetFloat(oldKey + "_t"), Is.EqualTo(42.5f));
            Assert.That(previous.GetBool(oldKey + "_a"), Is.True);
            Assert.That(previous.GetString(oldKey + "_rt"), Is.EqualTo("foreign deadline"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PreviewScope_PreservesOpenSceneSetup_AndClosesItsScene(bool failInspection)
        {
            Scene activeBefore = SceneManager.GetActiveScene();
            SceneSetup[] setupBefore = EditorSceneManager.GetSceneManagerSetup();
            bool dirtyBefore = activeBefore.isDirty;
            int previewCountBefore = EditorSceneManager.previewSceneCount;
            Scene preview = default;

            Action inspect = () =>
            {
                using (Audit20261023NeoEditorTestsPreviewSceneScope scope = new())
                {
                    preview = scope.Scene;
                    Assert.That(EditorSceneManager.IsPreviewScene(preview), Is.True);
                    Assert.That(EditorSceneManager.previewSceneCount, Is.EqualTo(previewCountBefore + 1));
                    if (failInspection)
                    {
                        throw new InvalidOperationException("simulated inspection failure");
                    }
                }
            };

            if (failInspection)
            {
                Assert.Throws<InvalidOperationException>(() => inspect());
            }
            else
            {
                inspect();
            }

            Assert.That(preview.IsValid(), Is.False);
            Assert.That(EditorSceneManager.previewSceneCount, Is.EqualTo(previewCountBefore));
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(activeBefore));
            Assert.That(activeBefore.isDirty, Is.EqualTo(dirtyBefore));
            SceneSetup[] setupAfter = EditorSceneManager.GetSceneManagerSetup();
            Assert.That(setupAfter.Length, Is.EqualTo(setupBefore.Length));
            for (int index = 0; index < setupBefore.Length; index++)
            {
                Assert.That(setupAfter[index].path, Is.EqualTo(setupBefore[index].path));
                Assert.That(setupAfter[index].isLoaded, Is.EqualTo(setupBefore[index].isLoaded));
                Assert.That(setupAfter[index].isActive, Is.EqualTo(setupBefore[index].isActive));
            }
        }

        [Test]
        public void MeshRigCleanup_PreservesExistingInactiveObjectsAndSelection_AndRemovesNewObjects()
        {
            GameObject canvas = new GameObject("ExistingCanvas", typeof(RectTransform), typeof(Canvas));
            GameObject eventSystem = new GameObject("ExistingEventSystem");
            eventSystem.SetActive(false);
            eventSystem.AddComponent<EventSystem>();
            canvas.SetActive(false);
            Object[] selectionBefore = Selection.objects;
            Neo.Tests.UI.UIMeshRigEditorTests fixture = new Neo.Tests.UI.UIMeshRigEditorTests();
            GameObject created = null;

            try
            {
                Selection.activeGameObject = canvas;
                fixture.CaptureSceneObjects();
                created = new GameObject("CreatedByFixture", typeof(RectTransform), typeof(Canvas));
                fixture.TearDown();

                Assert.That(canvas != null, Is.True);
                Assert.That(eventSystem != null, Is.True);
                Assert.That(canvas.activeSelf, Is.False);
                Assert.That(created == null, Is.True);
                Assert.That(Selection.activeGameObject, Is.SameAs(canvas));
            }
            finally
            {
                fixture.TearDown();
                Object.DestroyImmediate(created);
                Object.DestroyImmediate(canvas);
                Object.DestroyImmediate(eventSystem);
                Selection.objects = selectionBefore;
            }
        }

        [Test]
        public void SettingsCleanup_PreservesAnExistingSettingsObject()
        {
            GameObject existing = new GameObject("ExistingSettings");
            existing.SetActive(false);
            GameSettingsComponent component = existing.AddComponent<GameSettingsComponent>();
            Neo.Tools.Tests.GameSettingsTests fixture = new Neo.Tools.Tests.GameSettingsTests();
            try
            {
                fixture.SetUp();
                fixture.TearDown();
                Assert.That(existing != null, Is.True);
                Assert.That(existing.GetComponent<GameSettingsComponent>(), Is.SameAs(component));
            }
            finally
            {
                fixture.TearDown();
                Object.DestroyImmediate(existing);
            }
        }

        [Test]
        public void InteractiveCleanup_UsesOwnedCameraReference_EvenWhenItsNameChanges()
        {
            GameObject existing = new GameObject("MainCamera", typeof(Camera));
            HashSet<Camera> camerasBefore = new HashSet<Camera>(Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Include, FindObjectsSortMode.None));
            InteractiveObjectTests fixture = new InteractiveObjectTests();
            GameObject ownedCamera = null;
            try
            {
                fixture.SetUp();
                foreach (Camera camera in Object.FindObjectsByType<Camera>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (!camerasBefore.Contains(camera))
                    {
                        ownedCamera = camera.gameObject;
                        ownedCamera.name = "RenamedOwnedCamera";
                        break;
                    }
                }

                Assert.That(ownedCamera, Is.Not.Null);
                fixture.TearDown();
                Assert.That(existing != null, Is.True);
                Assert.That(ownedCamera == null, Is.True);
            }
            finally
            {
                fixture.TearDown();
                Object.DestroyImmediate(ownedCamera);
                Object.DestroyImmediate(existing);
            }
        }

        [Test]
        public void SampleTagSearch_UsesOnlyTheInspectedHierarchy_IncludingInactiveChildren()
        {
            GameObject unrelated = new GameObject("UnrelatedPlayer") { tag = "Player" };
            GameObject root = new GameObject("InspectedRoot");
            GameObject player = new GameObject("InspectedPlayer") { tag = "Player" };
            player.transform.SetParent(root.transform);
            player.SetActive(false);
            try
            {
                Assert.That(SampleSceneCoverageTests.FindTaggedObject(new[] { root }, "Player"), Is.SameAs(player));
                player.tag = "Untagged";
                Assert.That(SampleSceneCoverageTests.FindTaggedObject(new[] { root }, "Player"), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(unrelated);
            }
        }

        [Test]
        public void SampleMissingScriptCheck_ReportsAMissingComponentOnAnInactiveDescendant()
        {
            string directoryName = "Audit20261023NeoEditorMissingNative_" + Guid.NewGuid().ToString("N");
            string folder = "Assets/" + directoryName;
            Assert.That(AssetDatabase.CreateFolder("Assets", directoryName), Is.Not.Empty);
            try
            {
                string path = folder + "/SyntheticScene.unity";
                Audit20261023NeoEditorTestsSceneAssets.CreateMissingScriptSceneAsset(path);
                using (Audit20261023NeoEditorTestsPreviewSceneScope preview = new Audit20261023NeoEditorTestsPreviewSceneScope(path))
                {
                    GameObject[] roots = preview.Scene.GetRootGameObjects();
                    Assert.That(roots, Has.Length.EqualTo(1));
                    GameObject root = roots[0];
                    Assert.That(root.name, Is.EqualTo("RootWithoutMissingScripts"));
                    Assert.That(root.transform.childCount, Is.EqualTo(1));
                    GameObject child = root.transform.GetChild(0).gameObject;
                    Assert.That(child.name, Is.EqualTo("ChildWithMissingScript"));
                    Assert.That(child.activeSelf, Is.False);
                    Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root), Is.Zero);
                    Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child), Is.EqualTo(1));

                    foreach (string reportedPath in new[] { "SyntheticScene.unity", path })
                    {
                        List<string> failures = new List<string>();
                        SampleSceneCoverageTests.AddMissingScriptFailures(reportedPath, roots, failures);
                        CollectionAssert.AreEqual(new[] { reportedPath + ": contains 1 missing MonoBehaviour script(s)." }, failures);
                    }
                }
            }
            finally
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }

        private static void GetAudioFixture(string name, out Action setUp, out Action exercise, out Action tearDown)
        {
            switch (name)
            {
                case "Channels":
                    AmChannelEditModeTests channels = new AmChannelEditModeTests();
                    setUp = channels.SetUp;
                    exercise = channels.LegacyMigration_PutsTheMigratedSoundsOnTheSfxChannel;
                    tearDown = channels.TearDown;
                    return;
                case "Entries":
                    AmEntryEditModeTests entries = new AmEntryEditModeTests();
                    setUp = entries.SetUp;
                    exercise = entries.GetSound_FindsEntryById_AndReturnsNullForUnknownId;
                    tearDown = entries.TearDown;
                    return;
                case "Manager":
                    AmEditModeTests manager = new AmEditModeTests();
                    setUp = manager.SetUp;
                    exercise = manager.SetRandomMusicTracks_ReplacesListUsedByEnableRandomMusic;
                    tearDown = manager.TearDown;
                    return;
                case "RandomMusic":
                    RandomMusicControllerEditModeTests randomMusic = new RandomMusicControllerEditModeTests();
                    setUp = randomMusic.SetUp;
                    exercise = randomMusic.Stop_BeforeStart_DoesNotFireOnStopped;
                    tearDown = randomMusic.TearDown;
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown fixture.");
            }
        }

        private static void GetSaveFixture(string name, out Action setUp, out Action tearDown)
        {
            switch (name)
            {
                case "Bonus":
                    BonusSystemTests bonus = new BonusSystemTests();
                    setUp = bonus.SetUp;
                    tearDown = bonus.TearDown;
                    return;
                case "Cooldown":
                    CooldownRewardAutoClaimTests cooldown = new CooldownRewardAutoClaimTests();
                    setUp = cooldown.SetUp;
                    tearDown = cooldown.TearDown;
                    return;
                case "Quest":
                    QuestManagerTests quest = new QuestManagerTests();
                    setUp = quest.SetUp;
                    tearDown = quest.TearDown;
                    return;
                case "Slots":
                    Edit.SaveSlotTests slots = new Edit.SaveSlotTests();
                    setUp = slots.Setup;
                    tearDown = slots.Teardown;
                    return;
                case "Shop":
                    Edit.ShopManagerTests shop = new Edit.ShopManagerTests();
                    setUp = shop.Setup;
                    tearDown = shop.Teardown;
                    return;
                case "MoneyAudit":
                    AuditFixesMoneyTests moneyAudit = new AuditFixesMoneyTests();
                    setUp = moneyAudit.SetUp;
                    tearDown = moneyAudit.TearDown;
                    return;
                case "MoneyPersistence":
                    Edit.MoneyPersistenceEditModeTests persistence = new Edit.MoneyPersistenceEditModeTests();
                    setUp = persistence.SetUp;
                    tearDown = persistence.TearDown;
                    return;
                case "ShopRollback":
                    ShopPurchaseRollbackTests rollback = new ShopPurchaseRollbackTests();
                    setUp = rollback.SetUp;
                    tearDown = rollback.TearDown;
                    return;
                case "ShopAffordability":
                    ShopAffordabilityTests affordability = new ShopAffordabilityTests();
                    setUp = affordability.SetUp;
                    tearDown = affordability.TearDown;
                    return;
                case "ShopVariants":
                    ShopVariantsPanelTests variants = new ShopVariantsPanelTests();
                    setUp = variants.SetUp;
                    tearDown = variants.TearDown;
                    return;
                case "Progression":
                    Neo.Progression.Tests.ProgressionManagerTests progression = new();
                    setUp = progression.BeginSaveIsolation;
                    tearDown = progression.EndSaveIsolation;
                    return;
                case "Rpg":
                    Neo.Tests.Edit.RPG.RpgCharacterTests rpg = new Neo.Tests.Edit.RPG.RpgCharacterTests();
                    setUp = rpg.BeginSaveIsolation;
                    tearDown = rpg.EndSaveIsolation;
                    return;
                case "Selector":
                    Neo.Tools.Tests.SelectorTests selector = new Neo.Tools.Tests.SelectorTests();
                    setUp = selector.BeginSaveIsolation;
                    tearDown = selector.EndSaveIsolation;
                    return;
                case "Inventory":
                    Neo.Tools.Tests.InventorySystemTests inventory = new Neo.Tools.Tests.InventorySystemTests();
                    setUp = inventory.BeginSaveIsolation;
                    tearDown = inventory.TearDown;
                    return;
                case "Settings":
                    Neo.Tools.Tests.GameSettingsTests settings = new Neo.Tools.Tests.GameSettingsTests();
                    setUp = settings.SetUp;
                    tearDown = settings.TearDown;
                    return;
                case "SaveProvider":
                    Neo.Save.Tests.SaveProviderTests save = new Neo.Save.Tests.SaveProviderTests();
                    setUp = save.SetUp;
                    tearDown = save.TearDown;
                    return;
                case "SaveManager":
                    Edit.SaveManagerReadModifyWriteTests manager = new Edit.SaveManagerReadModifyWriteTests();
                    setUp = manager.SetUp;
                    tearDown = manager.TearDown;
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown fixture.");
            }
        }
    }
}
