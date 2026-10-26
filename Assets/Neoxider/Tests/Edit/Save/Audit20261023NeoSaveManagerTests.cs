using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Neo.Save;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using TestSaveable = global::Neo.Editor.Tests.Edit.Audit20261023FixtureNeoSaveManagerTestSaveable;
using BaseSaveable = global::Neo.Editor.Tests.Edit.Audit20261023FixtureNeoSaveManagerBaseSaveable;
using DerivedSaveable = global::Neo.Editor.Tests.Edit.Audit20261023FixtureNeoSaveManagerDerivedSaveable;
using SceneCallbackHost = global::Neo.Editor.Tests.Edit.Audit20261023FixtureNeoSaveManagerSceneCallbackHost;

namespace Neo.Editor.Tests.Edit
{
    public sealed class Audit20261023NeoSaveManagerTests
    {
        private readonly List<GameObject> _objects = new();
        private readonly List<string> _ownedScenePaths = new();
        private readonly List<(Scene Scene, bool Loaded, bool Dirty, GameObject[] Roots)> _borrowedScenes = new();
        private Dictionary<string, (MonoBehaviour instance, List<FieldInfo> fields)> _previousRegistrations;
        private SaveManager _previousManager;
        private bool _previousIsLoad;
        private Scene _previousActiveScene;
        private int _previousPreviewCount;
        private Scene _fixtureScene;
        private string _assetFolder;
        private bool _stateCaptured;
        private ISaveProvider _previousProvider;
        private bool _previousLogging;
        private FileSaveProvider _provider;
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _stateCaptured = false;
            _objects.Clear();
            _ownedScenePaths.Clear();
            _borrowedScenes.Clear();
            _assetFolder = null;
            _root = null;
            _previousProvider = SaveProvider.CurrentProvider;
            _previousLogging = SaveProvider.DebugLoggingEnabled;
            _previousActiveScene = SceneManager.GetActiveScene();
            _previousPreviewCount = EditorSceneManager.previewSceneCount;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                _borrowedScenes.Add((scene, scene.isLoaded, scene.isDirty,
                    scene.isLoaded ? scene.GetRootGameObjects() : Array.Empty<GameObject>()));
            }

            _previousRegistrations = new Dictionary<string, (MonoBehaviour instance, List<FieldInfo> fields)>(
                GetRegistrations());
            _previousIsLoad = SaveManager.IsLoad;
            SaveManager.TryGetInstance(out _previousManager);
            _stateCaptured = true;

            try
            {
                SaveProvider.DebugLoggingEnabled = false;
                SaveManager.ClearSubsystemCaches();
                string identity = Guid.NewGuid().ToString("N");
                _root = Path.Combine(Path.GetTempPath(), "Audit20261023NeoSaveManager_" + identity);
                Directory.CreateDirectory(_root);
                _provider = new FileSaveProvider("components.json", new FileSaveProviderOptions { PersistenceRoot = _root });
                SaveProvider.SetProvider(_provider);
                _assetFolder = "Assets/Audit20261023NeoSaveManager_" + identity;
                Assert.That(AssetDatabase.CreateFolder("Assets", "Audit20261023NeoSaveManager_" + identity),
                    Is.Not.Null.And.Not.Empty);
                _fixtureScene = OpenOwnedScene("Resident");
                ActivateScene(_fixtureScene);
            }
            catch (Exception setupFailure)
            {
                try
                {
                    TearDown();
                }
                catch (Exception cleanupFailure)
                {
                    throw new AggregateException("Save fixture setup and cleanup failed.", setupFailure, cleanupFailure);
                }

                throw;
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (!_stateCaptured)
            {
                return;
            }

            List<Exception> failures = new();
            foreach (GameObject gameObject in _objects)
            {
                if (gameObject != null)
                {
                    Cleanup(() => Object.DestroyImmediate(gameObject), failures);
                }
            }

            _objects.Clear();
            // Paths are recorded before OpenScene, so cleanup also owns a scene opened by a partially failed call.
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (_ownedScenePaths.Contains(scene.path))
                {
                    Cleanup(() => Assert.That(EditorSceneManager.CloseScene(scene, true), Is.True), failures);
                }
            }

            Cleanup(() =>
            {
                if (_previousActiveScene.IsValid() && _previousActiveScene.isLoaded)
                {
                    ActivateScene(_previousActiveScene);
                }
            }, failures);
            Cleanup(() =>
            {
                if (_assetFolder != null && AssetDatabase.IsValidFolder(_assetFolder))
                {
                    Assert.That(AssetDatabase.DeleteAsset(_assetFolder), Is.True);
                }
            }, failures);
            Cleanup(() =>
            {
                SaveManager.ClearSubsystemCaches();
                Dictionary<string, (MonoBehaviour instance, List<FieldInfo> fields)> registrations = GetRegistrations();
                foreach (KeyValuePair<string, (MonoBehaviour instance, List<FieldInfo> fields)> entry in _previousRegistrations)
                {
                    registrations.Add(entry.Key, entry.Value);
                }

                PropertyInfo isLoad = typeof(SaveManager).GetProperty(nameof(SaveManager.IsLoad));
                Assert.That(isLoad, Is.Not.Null);
                isLoad.SetValue(null, _previousIsLoad);
            }, failures);
            Cleanup(() => SaveProvider.SetProvider(_previousProvider), failures);
            SaveProvider.DebugLoggingEnabled = _previousLogging;
            Cleanup(() =>
            {
                if (_root != null && Directory.Exists(_root))
                {
                    Directory.Delete(_root, true);
                }
            }, failures);
            Cleanup(AssertBorrowedState, failures);
            _stateCaptured = false;
            if (failures.Count > 0)
            {
                throw new AggregateException("Save fixture cleanup/state restoration failed.", failures);
            }
        }

        [Test]
        public void Unregister_RejectedDuplicate_DoesNotRemoveOwner()
        {
            TestSaveable owner = CreateSaveable("shared");
            TestSaveable duplicate = CreateSaveable("shared");
            owner.Value = 71;
            SaveManager.Register(owner);
            SaveManager.Register(duplicate);

            SaveManager.Unregister(duplicate);
            SaveManager.Save();
            owner.Value = 0;
            SaveManager.Load(owner);

            Assert.That(owner.Value, Is.EqualTo(71));
            Assert.That(owner.LoadedCount, Is.EqualTo(1));
        }

        [Test]
        public void ManualLoad_RejectedDuplicate_DoesNotReadOwnersData()
        {
            TestSaveable owner = CreateSaveable("shared");
            owner.Value = 71;
            SaveManager.Save(owner);
            TestSaveable duplicate = CreateSaveable("shared");
            duplicate.Value = 99;

            SaveManager.Load(duplicate);

            Assert.That(duplicate.Value, Is.EqualTo(99));
            Assert.That(duplicate.LoadedCount, Is.Zero);
            Assert.That(owner.LoadedCount, Is.Zero);
        }

        [Test]
        public void SubsetLoad_RejectedDuplicate_DoesNotReloadOwner()
        {
            TestSaveable owner = CreateSaveable("shared");
            owner.Value = 71;
            SaveManager.Save(owner);
            owner.Value = 80;
            TestSaveable duplicate = CreateSaveable("shared");
            duplicate.Value = 99;

            SaveManager.Load(new List<MonoBehaviour> { duplicate });

            Assert.That(owner.Value, Is.EqualTo(80));
            Assert.That(duplicate.Value, Is.EqualTo(99));
            Assert.That(owner.LoadedCount, Is.Zero);
            Assert.That(duplicate.LoadedCount, Is.Zero);
        }

        [Test]
        public void ManualSave_RejectedDuplicate_DoesNotWriteOwnersLiveState()
        {
            TestSaveable owner = CreateSaveable("shared");
            owner.Value = 71;
            SaveManager.Save(owner);
            string storedBefore = _provider.GetString("SaveData_All");
            owner.Value = 80;
            TestSaveable duplicate = CreateSaveable("shared");

            SaveManager.Save(duplicate);

            Assert.That(_provider.GetString("SaveData_All"), Is.EqualTo(storedBefore));
            owner.Value = 0;
            SaveManager.Load(owner);
            Assert.That(owner.Value, Is.EqualTo(71));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PrivateBaseFields_RoundTripWithDerivedFields(bool automaticSave)
        {
            GameObject gameObject = CreateObject("InheritedSaveable");
            DerivedSaveable saveable = gameObject.AddComponent<DerivedSaveable>();
            saveable.BaseValue = 37;
            saveable.DerivedValue = 12;
            if (automaticSave)
            {
                SaveManager.Register(saveable);
                SaveManager.Save();
            }
            else
            {
                SaveManager.Save(saveable);
            }

            saveable.BaseValue = 0;
            saveable.DerivedValue = 0;
            SaveManager.Load(saveable);

            Assert.That(saveable.BaseValue, Is.EqualTo(37));
            Assert.That(saveable.DerivedValue, Is.EqualTo(12));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MalformedRecords_DoNotBlockValidFields_AndLastComponentEntryWins(bool bulkLoad)
        {
            TestSaveable empty = CreateSaveable("empty");
            TestSaveable target = CreateSaveable("target");
            SaveManager.Register(empty);
            SaveManager.Register(target);
            target.OtherValue = 5;
            _provider.SetString("SaveData_All", MalformedContainer(empty, target));

            if (bulkLoad)
            {
                SaveManager.Load(new List<MonoBehaviour> { empty, target });
            }
            else
            {
                SaveManager.Load(target);
            }

            Assert.That(target.Value, Is.EqualTo(29));
            Assert.That(target.OtherValue, Is.EqualTo(5), "missing field type must keep the current value");
            Assert.That(target.LoadedCount, Is.EqualTo(1));
        }

        [Test]
        public void ManualSave_RepairsNullFieldLists_AndReplacesDuplicateEntries()
        {
            TestSaveable target = CreateSaveable("repair");
            string key = SaveIdentityUtility.GetComponentKey(target);
            _provider.SetString("SaveData_All", "{\"AllSavedComponents\":[null," +
                "{\"ComponentKey\":\"" + key + "\",\"Fields\":null}," +
                "{\"ComponentKey\":\"" + key + "\",\"Fields\":null}]}");
            target.Value = 44;

            SaveManager.Save(target);
            target.Value = 0;
            SaveManager.Load(new List<MonoBehaviour> { target });

            Assert.That(target.Value, Is.EqualTo(44));
            Assert.That(target.LoadedCount, Is.EqualTo(1));
        }

        [Test]
        [Description("EditMode dispatch of the production scene-filter callback; not native sceneLoaded/OnEnable ordering acceptance.")]
        public void SceneLoaded_RestoresAlreadyRegisteredSceneObjects_WithoutReloadingResidentObjects()
        {
            string identity = Guid.NewGuid().ToString("N");
            TestSaveable resident = CreateSaveable(identity + "_resident");
            resident.Value = 10;
            SaveManager.Save(resident);
            resident.Value = 20;
            GameObject hostObject = CreateObject("AuditSaveManagerHost");
            SceneCallbackHost host = hostObject.AddComponent<SceneCallbackHost>();
            Scene loadedScene = OpenOwnedScene("Incoming");
            TestSaveable incoming = CreateSaveable(identity + "_incoming");
            SceneManager.MoveGameObjectToScene(incoming.gameObject, loadedScene);
            incoming.Value = 88;
            SaveManager.Save(incoming);
            incoming.Value = 0;
            string residentKey = SaveIdentityUtility.GetComponentKey(resident);
            string incomingKey = SaveIdentityUtility.GetComponentKey(incoming);
            Assert.That(residentKey, Is.Not.EqualTo(incomingKey));
            Assert.That(CountOwnedRegistrations(resident, incoming), Is.EqualTo(2),
                "both owned instances must be registered before dispatch");
            Assert.That(GetRegistrations()[incomingKey].instance, Is.SameAs(incoming));
            string storedBefore = _provider.GetString("SaveData_All");
            int keyChanges = 0;
            int providerSaves = 0;
            int providerLoads = 0;
            Action<string> onChanged = key => keyChanges++;
            Action onSaved = () => providerSaves++;
            Action onLoaded = () => providerLoads++;
            _provider.OnKeyChanged += onChanged;
            _provider.OnDataSaved += onSaved;
            _provider.OnDataLoaded += onLoaded;

            try
            {
                // Deliberately test the callback's filtering after public registration. Reflection is dispatch only;
                // genuine OnEnable -> sceneLoaded ordering still requires separate top-level Play acceptance.
                MethodInfo callback = typeof(SaveManager).GetMethod("OnSceneLoaded",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(callback, Is.Not.Null);
                callback.Invoke(host, new object[] { loadedScene, LoadSceneMode.Additive });

                Assert.That(incoming.Value, Is.EqualTo(88));
                Assert.That(incoming.LoadedCount, Is.EqualTo(1));
                Assert.That(resident.Value, Is.EqualTo(20));
                Assert.That(resident.LoadedCount, Is.Zero);
                Assert.That(CountOwnedRegistrations(resident, incoming), Is.EqualTo(2));
                Assert.That(GetRegistrations()[residentKey].instance, Is.SameAs(resident));
                Assert.That(GetRegistrations()[incomingKey].instance, Is.SameAs(incoming));
                Assert.That(SaveIdentityUtility.GetComponentKey(resident), Is.EqualTo(residentKey));
                Assert.That(SaveIdentityUtility.GetComponentKey(incoming), Is.EqualTo(incomingKey));
                Assert.That(SaveProvider.CurrentProvider, Is.SameAs(_provider));
                Assert.That(_provider.GetString("SaveData_All"), Is.EqualTo(storedBefore));
                Assert.That(keyChanges, Is.Zero);
                Assert.That(providerSaves, Is.Zero);
                Assert.That(providerLoads, Is.Zero);
            }
            finally
            {
                _provider.OnKeyChanged -= onChanged;
                _provider.OnDataSaved -= onSaved;
                _provider.OnDataLoaded -= onLoaded;
            }
        }

        private Scene OpenOwnedScene(string name)
        {
            string path = _assetFolder + "/" + name + ".unity";
            Audit20261023NeoEditorTestsSceneAssets.CreateEmptySceneAsset(path);
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(path), Is.Not.Null);
            _ownedScenePaths.Add(path);
            return EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }

        private static void ActivateScene(Scene scene)
        {
            Assert.That(scene.IsValid(), Is.True);
            Assert.That(scene.isLoaded, Is.True);
            if (SceneManager.GetActiveScene() != scene)
            {
                Assert.That(SceneManager.SetActiveScene(scene), Is.True);
            }

            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(scene));
        }

        private static Dictionary<string, (MonoBehaviour instance, List<FieldInfo> fields)> GetRegistrations()
        {
            FieldInfo field = typeof(SaveManager).GetField("_saveableComponents", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (Dictionary<string, (MonoBehaviour instance, List<FieldInfo> fields)>)field.GetValue(null);
        }

        private static int CountOwnedRegistrations(MonoBehaviour first, MonoBehaviour second)
        {
            int count = 0;
            foreach (KeyValuePair<string, (MonoBehaviour instance, List<FieldInfo> fields)> entry in GetRegistrations())
            {
                if (ReferenceEquals(entry.Value.instance, first) || ReferenceEquals(entry.Value.instance, second))
                {
                    count++;
                }
            }

            return count;
        }

        private void AssertBorrowedState()
        {
            Assert.That(SceneManager.sceneCount, Is.EqualTo(_borrowedScenes.Count));
            Assert.That(EditorSceneManager.previewSceneCount, Is.EqualTo(_previousPreviewCount));
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_previousActiveScene));
            for (int i = 0; i < _borrowedScenes.Count; i++)
            {
                (Scene scene, bool loaded, bool dirty, GameObject[] roots) = _borrowedScenes[i];
                Assert.That(SceneManager.GetSceneAt(i), Is.EqualTo(scene));
                Assert.That(scene.isLoaded, Is.EqualTo(loaded));
                Assert.That(scene.isDirty, Is.EqualTo(dirty));
                if (loaded)
                {
                    Assert.That(scene.GetRootGameObjects(), Is.EqualTo(roots));
                }
            }

            SaveManager.TryGetInstance(out SaveManager currentManager);
            Assert.That(currentManager, Is.SameAs(_previousManager));
            Assert.That(SaveManager.IsLoad, Is.EqualTo(_previousIsLoad));
            Dictionary<string, (MonoBehaviour instance, List<FieldInfo> fields)> registrations = GetRegistrations();
            Assert.That(registrations.Count, Is.EqualTo(_previousRegistrations.Count));
            foreach (KeyValuePair<string, (MonoBehaviour instance, List<FieldInfo> fields)> entry in _previousRegistrations)
            {
                Assert.That(registrations.ContainsKey(entry.Key), Is.True);
                Assert.That(registrations[entry.Key].instance, Is.SameAs(entry.Value.instance));
                Assert.That(registrations[entry.Key].fields, Is.SameAs(entry.Value.fields));
            }

            Assert.That(SaveProvider.CurrentProvider, Is.SameAs(_previousProvider));
            Assert.That(SaveProvider.DebugLoggingEnabled, Is.EqualTo(_previousLogging));
        }

        private static void Cleanup(Action action, List<Exception> failures)
        {
            try
            {
                action();
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
        }

        private GameObject CreateObject(string name)
        {
            ActivateScene(_fixtureScene);
            GameObject gameObject = new(name);
            _objects.Add(gameObject);
            return gameObject;
        }

        private TestSaveable CreateSaveable(string identity)
        {
            TestSaveable saveable = CreateObject(identity).AddComponent<TestSaveable>();
            saveable.Identity = identity;
            return saveable;
        }

        private static string MalformedContainer(TestSaveable empty, TestSaveable target)
        {
            string emptyKey = SaveIdentityUtility.GetComponentKey(empty);
            string key = SaveIdentityUtility.GetComponentKey(target);
            return "{\"AllSavedComponents\":[null," +
                   "{\"ComponentKey\":null,\"Fields\":null}," +
                   "{\"ComponentKey\":\"" + emptyKey + "\",\"Fields\":null}," +
                   "{\"ComponentKey\":\"" + key + "\",\"Fields\":[" +
                   "{\"Key\":\"value\",\"TypeName\":\"System.Int32\",\"Value\":\"11\"}]}," +
                   "{\"ComponentKey\":\"" + key + "\",\"Fields\":[null," +
                   "{\"Key\":\"other\",\"Value\":\"999\"}," +
                   "{\"Key\":\"value\",\"TypeName\":\"System.Int32\",\"Value\":\"29\"}]}]}";
        }








    }
}
