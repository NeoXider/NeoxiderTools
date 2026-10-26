#if MIRROR
using System;
using System.Collections.Generic;
using System.IO;
using Mirror;
using Neo.Network;
using Neo.Tools;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoEditorLobbyTests
    {
        private Scene _originalActive;
        private Scene _sentinel;
        private GameObject _sentinelRoot;
        private UnityEngine.Object[] _originalSelection;
        private string _fixtureFolder;
        private string _scenePath;
        private string _contextFolder;
        private readonly List<(Scene Scene, string Path, bool Loaded, bool Dirty, GameObject[] Roots)> _borrowedScenes = new();
        private int _previousPreviewCount;

        [SetUp]
        public void SetUp()
        {
            _originalActive = SceneManager.GetActiveScene();
            _originalSelection = Selection.objects;
            _previousPreviewCount = EditorSceneManager.previewSceneCount;
            _sentinel = default;
            _sentinelRoot = null;
            _borrowedScenes.Clear();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                _borrowedScenes.Add((scene, scene.path, scene.isLoaded, scene.isDirty,
                    scene.isLoaded ? scene.GetRootGameObjects() : Array.Empty<GameObject>()));
            }

            try
            {
                string name = "Audit20261023NeoEditorLobby_" + Guid.NewGuid().ToString("N");
                AssetDatabase.CreateFolder("Assets", name);
                _fixtureFolder = "Assets/" + name;
                _scenePath = _fixtureFolder + "/Lobby.unity";
                AssetDatabase.CreateFolder("Assets", name + "_Context");
                _contextFolder = "Assets/" + name + "_Context";
                string path = _contextFolder + "/Sentinel.unity";
                Audit20261023NeoEditorTestsSceneAssets.CreateEmptySceneAsset(path);
                Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(path), Is.Not.Null);
                _sentinel = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                Assert.That(EditorSceneManager.IsPreviewScene(_sentinel), Is.False);
                Assert.That(_sentinel.path, Is.EqualTo(path));
                Assert.That(_sentinel.GetRootGameObjects(), Is.Empty);
                Assert.That(SceneManager.SetActiveScene(_sentinel), Is.True);
                _sentinelRoot = new GameObject("Unsaved_Owned_Sentinel");
                EditorSceneManager.MarkSceneDirty(_sentinel);
            }
            catch
            {
                TearDown();
                throw;
            }
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (!string.IsNullOrEmpty(_scenePath))
                {
                    Scene generated = SceneManager.GetSceneByPath(_scenePath);
                    if (generated.IsValid() && generated.isLoaded)
                    {
                        EditorSceneManager.CloseScene(generated, true);
                    }
                }

                if (_sentinel.IsValid() && _sentinel.isLoaded)
                {
                    EditorSceneManager.CloseScene(_sentinel, true);
                }

                if (!string.IsNullOrEmpty(_fixtureFolder))
                {
                    AssetDatabase.DeleteAsset(_fixtureFolder);
                    _fixtureFolder = null;
                }

                if (!string.IsNullOrEmpty(_contextFolder))
                {
                    AssetDatabase.DeleteAsset(_contextFolder);
                    _contextFolder = null;
                }
            }
            finally
            {
                if (_originalActive.IsValid() && _originalActive.isLoaded)
                {
                    SceneManager.SetActiveScene(_originalActive);
                }

                if (_originalSelection != null)
                {
                    Selection.objects = _originalSelection;
                }

                Assert.That(SceneManager.sceneCount, Is.EqualTo(_borrowedScenes.Count));
                Assert.That(EditorSceneManager.previewSceneCount, Is.EqualTo(_previousPreviewCount));
                Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_originalActive));
                for (int i = 0; i < _borrowedScenes.Count; i++)
                {
                    Scene expected = _borrowedScenes[i].Scene;
                    Assert.That(SceneManager.GetSceneAt(i), Is.EqualTo(expected));
                    Assert.That(expected.path, Is.EqualTo(_borrowedScenes[i].Path));
                    Assert.That(expected.isLoaded, Is.EqualTo(_borrowedScenes[i].Loaded));
                    Assert.That(expected.isDirty, Is.EqualTo(_borrowedScenes[i].Dirty));
                    if (_borrowedScenes[i].Loaded)
                    {
                        CollectionAssert.AreEqual(_borrowedScenes[i].Roots, expected.GetRootGameObjects());
                    }
                }
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Cancellation_PreservesUnsavedWorkBeforeAnySceneReplacement(bool cancelDestination)
        {
            int scenes = SceneManager.sceneCount;
            int previews = EditorSceneManager.previewSceneCount;
            Scene expectedScene = _sentinel;
            bool destinationRequested = false;

            bool created = LobbyDemoSceneCreator.TryCreateLobbyDemoScene(() => cancelDestination, () =>
            {
                destinationRequested = true;
                return "";
            });

            Assert.That(created, Is.False);
            Assert.That(destinationRequested, Is.EqualTo(cancelDestination));
            Assert.That(SceneManager.sceneCount, Is.EqualTo(scenes));
            Assert.That(EditorSceneManager.previewSceneCount, Is.EqualTo(previews));
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(expectedScene));
            Assert.That(_sentinel.isDirty, Is.True);
            Assert.That(_sentinelRoot, Is.Not.Null);
            Assert.That(_sentinelRoot.scene, Is.EqualTo(expectedScene));
            Assert.That(System.IO.Directory.GetFileSystemEntries(_fixtureFolder), Is.Empty);
        }

        [Test]
        public void ExistingDestination_IsRejectedWithoutOverwritingOrReplacingScenes()
        {
            byte[] source = File.ReadAllBytes(_sentinel.path);
            string guid = AssetDatabase.AssetPathToGUID(_sentinel.path);
            int scenes = SceneManager.sceneCount;
            UnityEngine.Object[] selection = Selection.objects;

            IOException failure = Assert.Throws<IOException>(() =>
                LobbyDemoSceneCreator.TryCreateLobbyDemoScene(() => true, () => _sentinel.path,
                    NewSceneMode.Additive));

            Assert.That(failure.Message, Does.StartWith("Refusing to overwrite an existing lobby destination:"));
            Assert.That(File.ReadAllBytes(_sentinel.path), Is.EqualTo(source));
            Assert.That(AssetDatabase.AssetPathToGUID(_sentinel.path), Is.EqualTo(guid));
            Assert.That(SceneManager.sceneCount, Is.EqualTo(scenes));
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_sentinel));
            Assert.That(_sentinel.isLoaded && _sentinel.isDirty, Is.True);
            Assert.That(_sentinelRoot.scene, Is.EqualTo(_sentinel));
            CollectionAssert.AreEqual(selection, Selection.objects);
            Assert.That(Directory.GetFileSystemEntries(_fixtureFolder), Is.Empty);
        }

        [Test]
        public void PopulationFailure_ReleasesOnlyCreatedAssetsAndRestoresActiveSceneAndSelection()
        {
            byte[] source = File.ReadAllBytes(_sentinel.path);
            int scenes = SceneManager.sceneCount;
            int previews = EditorSceneManager.previewSceneCount;
            UnityEngine.Object[] selection = Selection.objects;
            Scene partialScene = default;
            GameObject partialRoot = null;
            bool reachedPopulation = false;

            InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() =>
                LobbyDemoSceneCreator.TryCreateLobbyDemoScene(() => true, () => _scenePath,
                    NewSceneMode.Additive, (Scene scene) =>
                    {
                        reachedPopulation = true;
                        partialScene = scene;
                        Assert.That(scene.path, Is.EqualTo(_scenePath));
                        Assert.That(EditorSceneManager.IsPreviewScene(scene), Is.False);
                        Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(scene));
                        Assert.That(AssetDatabase.IsValidFolder(_fixtureFolder + "/Lobby_Assets"), Is.True);
                        partialRoot = new GameObject("OwnedPopulationFailureMarker");
                        Selection.activeGameObject = partialRoot;
                        throw new InvalidOperationException("OwnedPopulationFailure");
                    }));

            Assert.That(reachedPopulation, Is.True);
            Assert.That(failure.Message, Is.EqualTo("OwnedPopulationFailure"));
            Assert.That(partialScene.IsValid(), Is.False);
            Assert.That(partialRoot == null, Is.True);
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(_scenePath), Is.Null);
            Assert.That(Directory.GetFileSystemEntries(_fixtureFolder), Is.Empty);
            Assert.That(SceneManager.sceneCount, Is.EqualTo(scenes));
            Assert.That(EditorSceneManager.previewSceneCount, Is.EqualTo(previews));
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_sentinel));
            Assert.That(_sentinel.isLoaded && _sentinel.isDirty, Is.True);
            Assert.That(_sentinelRoot.scene, Is.EqualTo(_sentinel));
            Assert.That(File.ReadAllBytes(_sentinel.path), Is.EqualTo(source));
            CollectionAssert.AreEqual(selection, Selection.objects);
        }

        [Test]
        public void SavedAndReopenedLobby_RetainsTypedPrefabReferencesAndPresenterCallbacks()
        {
            byte[] sentinelSource = File.ReadAllBytes(_sentinel.path);
            GameObject[] sentinelRoots = _sentinel.GetRootGameObjects();
            if (Type.GetType("kcp2k.KcpTransport, Mirror.Transports") == null)
            {
                LogAssert.Expect(LogType.Warning, "[Neo] KcpTransport not found. Please add a Transport manually.");
            }

            Assert.That(LobbyDemoSceneCreator.TryCreateLobbyDemoScene(() => true, () => _scenePath,
                NewSceneMode.Additive), Is.True);
            Scene created = SceneManager.GetSceneByPath(_scenePath);
            EditorSceneManager.CloseScene(created, true);
            Scene reopened = EditorSceneManager.OpenScene(_scenePath, OpenSceneMode.Additive);
            NeoLobbyPresenter presenter = FindOwned<NeoLobbyPresenter>(reopened);
            NeoLobbyManager manager = FindOwned<NeoLobbyManager>(reopened);
            Assert.That(presenter.Manager, Is.SameAs(manager));
            Assert.That(manager.RoomScene, Is.EqualTo(_scenePath));
            Assert.That(manager.offlineScene, Is.EqualTo(_scenePath));
            Assert.That(manager.onlineScene, Is.EqualTo(_scenePath));
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(manager.GameplayScene), Is.Not.Null);
            Assert.That(manager.roomPlayerPrefab, Is.TypeOf<NeoLobbyPlayer>());
            Assert.That(EditorUtility.IsPersistent(manager.roomPlayerPrefab), Is.True);
            Assert.That(EditorUtility.IsPersistent(manager.playerPrefab), Is.True);
            Assert.That(manager.playerPrefab.GetComponent<NetworkIdentity>(), Is.Not.Null);
            Assert.That(manager.playerPrefab.GetComponent<NeoLobbyPlayer>(), Is.Null);
            Assert.That(manager.roomPlayerPrefab.gameObject, Is.Not.SameAs(manager.playerPrefab));

            Dictionary<string, string> callbacks = new()
            {
                { "HostButton", nameof(NeoLobbyPresenter.HostLobby) },
                { "JoinButton", nameof(NeoLobbyPresenter.JoinLobby) },
                { "ReadyButton", nameof(NeoLobbyPresenter.ToggleReady) },
                { "LeaveButton", nameof(NeoLobbyPresenter.LeaveLobby) }
            };
            foreach (GameObject root in reopened.GetRootGameObjects())
            {
                foreach (Button button in root.GetComponentsInChildren<Button>(true))
                {
                    Assert.That(callbacks.TryGetValue(button.name, out string method), Is.True);
                    Assert.That(button.onClick.GetPersistentEventCount(), Is.EqualTo(1));
                    Assert.That(button.onClick.GetPersistentTarget(0), Is.SameAs(presenter));
                    Assert.That(button.onClick.GetPersistentMethodName(0), Is.EqualTo(method));
                    callbacks.Remove(button.name);
                }
            }

            Assert.That(callbacks, Is.Empty, "All four controls must survive save/reopen.");
            TMP_InputField input = FindOwned<TMP_InputField>(reopened);
            Transform countTransform = presenter.transform.Find("ConnectionPanel/PlayerCountText");
            Transform statusTransform = presenter.transform.Find("ConnectionPanel/StatusText");
            Assert.That(countTransform, Is.Not.Null);
            Assert.That(statusTransform, Is.Not.Null);
            SetText count = countTransform.GetComponent<SetText>();
            SetText status = statusTransform.GetComponent<SetText>();
            Assert.That(count, Is.Not.Null);
            Assert.That(status, Is.Not.Null);
            UnityEvent<string>[] callbackEvents =
                { input.onValueChanged, presenter.OnPlayerCountTextChanged, presenter.OnStatusChanged };
            UnityEngine.Object[] callbackTargets = { presenter, count, status };
            string[] callbackMethods = { nameof(NeoLobbyPresenter.SetAddress), nameof(SetText.Set), nameof(SetText.Set) };
            UnityEventCallState[] originalStates = new UnityEventCallState[callbackEvents.Length];
            for (int i = 0; i < callbackEvents.Length; i++)
            {
                Assert.That(callbackEvents[i].GetPersistentEventCount(), Is.EqualTo(1));
                Assert.That(callbackEvents[i].GetPersistentTarget(0), Is.SameAs(callbackTargets[i]));
                Assert.That(callbackEvents[i].GetPersistentMethodName(0), Is.EqualTo(callbackMethods[i]));
                originalStates[i] = callbackEvents[i].GetPersistentListenerState(0);
                Assert.That(originalStates[i], Is.EqualTo(UnityEventCallState.RuntimeOnly));
            }

            // Edit-mode serialized wiring/adapter coverage; RuntimeOnly delivery still needs Play QA.
            // Enable only these three owned loaded callbacks, then restore their saved policy without saving.
            try
            {
                foreach (UnityEvent<string> callback in callbackEvents)
                {
                    callback.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
                }

                input.onValueChanged.Invoke("10.0.0.5");
                Assert.That(presenter.Address, Is.EqualTo("10.0.0.5"));
                presenter.OnPlayerCountTextChanged.Invoke("Players: 7");
                Assert.That(count.text.text, Is.EqualTo("Players: 7"));
                presenter.OnStatusChanged.Invoke("Restored status");
                Assert.That(status.text.text, Is.EqualTo("Restored status"));
            }
            finally
            {
                for (int i = 0; i < callbackEvents.Length; i++)
                {
                    callbackEvents[i].SetPersistentListenerState(0, originalStates[i]);
                }
            }

            for (int i = 0; i < callbackEvents.Length; i++)
            {
                Assert.That(callbackEvents[i].GetPersistentListenerState(0), Is.EqualTo(originalStates[i]));
                Assert.That(callbackEvents[i].GetPersistentEventCount(), Is.EqualTo(1));
                Assert.That(callbackEvents[i].GetPersistentTarget(0), Is.SameAs(callbackTargets[i]));
                Assert.That(callbackEvents[i].GetPersistentMethodName(0), Is.EqualTo(callbackMethods[i]));
            }

            Scene gameplay = EditorSceneManager.OpenScene(manager.GameplayScene, OpenSceneMode.Additive);
            try
            {
                NeoLobbyPresenter gamePresenter = FindOwned<NeoLobbyPresenter>(gameplay);
                Button leave = FindOwned<Button>(gameplay);
                Assert.That(leave.onClick.GetPersistentTarget(0), Is.SameAs(gamePresenter));
                Assert.That(leave.onClick.GetPersistentMethodName(0), Is.EqualTo(nameof(NeoLobbyPresenter.LeaveLobby)));
            }
            finally
            {
                EditorSceneManager.CloseScene(gameplay, true);
            }

            Assert.That(_sentinel.isLoaded && _sentinel.isDirty, Is.True);
            Assert.That(File.ReadAllBytes(_sentinel.path), Is.EqualTo(sentinelSource));
            CollectionAssert.AreEqual(sentinelRoots, _sentinel.GetRootGameObjects());
        }

        private static T FindOwned<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            Assert.Fail("Missing owned component: " + typeof(T).Name);
            return null;
        }
    }
}
#endif
