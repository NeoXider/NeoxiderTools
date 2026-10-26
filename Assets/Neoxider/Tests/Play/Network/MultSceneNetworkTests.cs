#if UNITY_EDITOR && MIRROR
using System.Collections;
using System.Collections.Generic;
using Neo.Save;
using UnityEngine.EventSystems;
using Mirror;
using Neo.Network;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    public class MultSceneNetworkTests
    {
        private const string MultScenePath = "Assets/Scenes/Mult.unity";
        private NetworkTestSession _session;
        private Scene _ownedScene;
        private Scene _previousActiveScene;
        private NeoNetworkManager _ownedManager;
        private ISaveProvider _previousProvider;
        private EventSystem _previousEventSystem;
        private CursorLockMode _previousCursorLock;
        private bool _previousCursorVisible;
        private bool _loadRequested;
        private bool _loadCompleted;
        private bool _listening;
        private readonly HashSet<GameObject> _ownedSceneObjects = new HashSet<GameObject>();

        internal Scene OwnedScene => _ownedScene;
        internal NeoNetworkManager OwnedManager => _ownedManager;
        internal IEnumerable<GameObject> OwnedSceneObjects => _ownedSceneObjects;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _session = null;
            _ownedScene = default;
            _ownedManager = null;
            _previousProvider = null;
            _loadRequested = false;
            _loadCompleted = false;
            _listening = false;
            _ownedSceneObjects.Clear();
            SkipWithoutMultScene();
            _session = new NetworkTestSession();
            _previousActiveScene = SceneManager.GetActiveScene();
            _previousEventSystem = EventSystem.current;
            _previousCursorLock = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;
            _previousProvider = SaveProvider.CurrentProvider;
            SaveProvider.SetProvider(new ShopPurchasePlayModeTests.MemorySaveProvider());
            yield break;
        }

        // WHY: the scene lives in the development project, not in the package, so a consumer running the
        // package tests has no Mult.unity — skip instead of failing on a missing asset.
        private static void SkipWithoutMultScene()
        {
            if (!System.IO.File.Exists(MultScenePath))
            {
                Assert.Ignore($"'{MultScenePath}' is not present — this test needs the development project's multiplayer scene.");
            }
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_session == null)
            {
                yield break; // Failed acquisition must not release a borrowed session/provider.
            }

            AsyncOperation unloading = null;
            bool loadFinished = !_loadRequested;
            try
            {
                try
                {
                    // A body can fail immediately after BeginOwnedSceneLoad, before Awake/sceneLoaded.
                    if (_loadRequested && !_loadCompleted)
                    {
                        // Readiness assertions belong to the body. Cleanup must continue even if
                        // scene loading finishes without publishing a valid manager.
                        double deadline = Time.realtimeSinceStartupAsDouble + 10d;
                        while (!_loadCompleted && Time.realtimeSinceStartupAsDouble < deadline)
                        {
                            LedgerLoadedScene();
                            yield return null;
                        }
                    }

                    LedgerLoadedScene();
                    loadFinished = !_loadRequested || _loadCompleted;
                    yield return _session.Cleanup();
                }
                finally
                {
                    if (_listening)
                    {
                        SceneManager.sceneLoaded -= OnOwnedSceneLoaded;
                        _listening = false;
                    }

                    if (_ownedScene.IsValid() && _ownedScene.isLoaded)
                    {
                        unloading = SceneManager.UnloadSceneAsync(_ownedScene);
                    }
                }

                if (unloading != null)
                {
                    yield return NetworkTestHelper.WaitUntil(() => unloading.isDone,
                        10f, "owned Mult scene unload");
                }

                Assert.That(loadFinished, Is.True,
                    "Owned Mult load did not finish before its cleanup deadline; native load evidence is incomplete.");
            }
            finally
            {
                _session = null;
                if (_previousActiveScene.IsValid() && _previousActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(_previousActiveScene);
                }

                // The getter captured a registered system. Native OnDisable unregisters it;
                // the setter rejects null, destroyed and no-longer-enabled originals.
                if (_previousEventSystem != null && _previousEventSystem.isActiveAndEnabled &&
                    EventSystem.current != _previousEventSystem)
                {
                    EventSystem.current = _previousEventSystem;
                }
                Cursor.lockState = _previousCursorLock;
                Cursor.visible = _previousCursorVisible;
                if (_previousProvider != null)
                {
                    SaveProvider.SetProvider(_previousProvider);
                    _previousProvider = null;
                }
            }
        }

        internal void BeginOwnedSceneLoad()
        {
            Assert.That(_session, Is.Not.Null);
            Assert.That(_loadRequested, Is.False, "This fixture owns one scene load.");
            Assert.That(SceneManager.GetSceneByPath(MultScenePath).IsValid(), Is.False,
                "An already loaded Mult scene is borrowed and must not be claimed or unloaded.");
            _loadRequested = true;
            SceneManager.sceneLoaded += OnOwnedSceneLoaded;
            _listening = true;
            _ownedScene = EditorSceneManager.LoadSceneInPlayMode(MultScenePath,
                new LoadSceneParameters(LoadSceneMode.Additive));
        }

        internal IEnumerator LoadOwnedScene()
        {
            BeginOwnedSceneLoad();
            yield return WaitForOwnedScene();
        }

        private IEnumerator WaitForOwnedScene()
        {
            yield return NetworkTestHelper.WaitUntil(() =>
            {
                LedgerLoadedScene();
                return _loadCompleted && _ownedScene.IsValid() && _ownedScene.isLoaded && _ownedManager != null;
            }, 10f, "Mult scene load, Awake and owned manager publication");
        }

        private void OnOwnedSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!_loadRequested || scene.path != MultScenePath ||
                (_ownedScene.IsValid() && scene != _ownedScene))
            {
                return;
            }

            _ownedScene = scene;
            // Ledger before any readiness assertions; Awake may already have moved roots into DDOL.
            LedgerLoadedScene();
            // A root appearing during this load can belong to an earlier sceneLoaded observer.
            // Only the exact scene and explicitly anchored manager hierarchy confer ownership.
            _loadCompleted = true;
        }

        private void OwnHierarchy(GameObject root)
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                _ownedSceneObjects.Add(transform.gameObject);
                _session.Own(transform.gameObject); // Child roots remain owned if Start moves/detaches them.
            }
        }

        private void LedgerLoadedScene()
        {
            if (!_loadRequested || !_ownedScene.IsValid() || !_ownedScene.isLoaded)
            {
                return;
            }

            foreach (GameObject root in _ownedScene.GetRootGameObjects())
            {
                OwnHierarchy(root);
            }

            NeoNetworkManager candidate = NetworkManager.singleton as NeoNetworkManager;
            if (candidate != null && BelongsToOwnedScene(candidate) &&
                (ReferenceEquals(_ownedManager, null) || ReferenceEquals(_ownedManager, candidate)))
            {
                _ownedManager = candidate;
                // Descend from the exact manager; a parent outside this hierarchy is not owned.
                OwnHierarchy(candidate.gameObject);
                _session.OwnManager(candidate);
            }
        }

        private bool BelongsToOwnedScene(NeoNetworkManager manager)
        {
            GameObject template = manager.ScenePlayerTemplate != null
                ? manager.ScenePlayerTemplate : manager.playerPrefab;
            return manager.gameObject.scene == _ownedScene || _ownedSceneObjects.Contains(manager.gameObject) ||
                   (template != null && template.scene == _ownedScene);
        }

        [UnityTest]
        public IEnumerator MultScene_StartHost_UsesSceneTemplateAndLeavesOnlyRuntimePlayerActive()
        {
            SkipWithoutMultScene();

            yield return LoadOwnedScene();

            NeoNetworkManager manager = _ownedManager;
            Assert.IsNotNull(manager, "Mult scene should contain a NeoNetworkManager.");
            NetworkTestHelper.UseDummyTransport(manager);

            GameObject sceneTemplate = manager.ScenePlayerTemplate != null
                ? manager.ScenePlayerTemplate
                : GameObject.Find("First Person Controller");

            Assert.IsNotNull(sceneTemplate,
                "Mult scene should contain a scene-authored First Person Controller template.");
            Assert.IsTrue(manager.UseScenePlayerTemplate || manager.playerPrefab == sceneTemplate,
                "Mult scene should use Scene Player Template mode or auto-migrate a scene object assigned as Player Prefab.");

            manager.StartHost();

            yield return NetworkTestHelper.WaitForHost(requireLocalPlayer: true);

            yield return null;

            GameObject localPlayer = NetworkClient.localPlayer.gameObject;
            Assert.AreNotSame(sceneTemplate, localPlayer,
                "Host player should be a runtime copy, not the scene template.");
            Assert.IsFalse(sceneTemplate.activeSelf,
                "Scene-authored player template should be disabled after host start.");

            NeoNetworkPlayer[] players = Object.FindObjectsByType<NeoNetworkPlayer>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            int activePlayers = 0;
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].gameObject.activeInHierarchy)
                {
                    activePlayers++;
                }
            }

            Assert.AreEqual(1, activePlayers, "Host should leave only one active runtime player in Mult scene.");
            Assert.IsTrue(localPlayer.activeInHierarchy,
                "NetworkClient.localPlayer should be the active runtime player.");
            Assert.AreEqual(0UL, localPlayer.GetComponent<NetworkIdentity>().sceneId,
                "Runtime player copy must not keep the template sceneId.");
        }

        [UnityTest]
        public IEnumerator MultScene_NetworkContextActionRelay_PickupDisablesCubeAndEnablesPlayerSphere()
        {
            // WHY: Mult.unity wires Trigger Cube (1) with TWO NetworkContextActionRelays:
            //   1. Self → Root → SetActive(false) — the cube itself disappears (visible to all clients).
            //   2. EventArgument → NetworkIdentityInParents → ChildByName("Sphere") → SetActive(true) —
            //      the entering player's Sphere child lights up (visible to all clients).
            // Both fire from PhysicsEvents3D.onTriggerEnter.
            SkipWithoutMultScene();

            yield return LoadOwnedScene();

            NetworkContextActionRelay[] relays = FindAllNetworkContextActionRelaysInLoadedScenes();
            Assert.AreEqual(2, relays.Length,
                "Mult.unity should now contain two NetworkContextActionRelays on Trigger Cube (1): pickup-self + bonus-on-player.");

            NetworkContextActionRelay pickupRelay = null;
            NetworkContextActionRelay bonusRelay = null;
            for (int i = 0; i < relays.Length; i++)
            {
                if (relays[i].ContextSource == NetworkContextSourceMode.Self)
                {
                    pickupRelay = relays[i];
                }
                else if (relays[i].ContextSource == NetworkContextSourceMode.EventArgument)
                {
                    bonusRelay = relays[i];
                }
            }

            Assert.IsNotNull(pickupRelay, "One relay must be configured as Pickup (ContextSource=Self).");
            Assert.IsNotNull(bonusRelay, "One relay must be configured as Bonus (ContextSource=EventArgument).");

            NeoNetworkManager manager = _ownedManager;
            Assert.IsNotNull(manager);
            NetworkTestHelper.UseDummyTransport(manager);

            manager.StartHost();

            yield return NetworkTestHelper.WaitForHost(requireLocalPlayer: true);

            yield return null;

            Assert.IsTrue(pickupRelay.gameObject.activeSelf,
                "Pickup trigger cube must be active once the server has spawned scene objects.");

            GameObject localPlayer = NetworkClient.localPlayer.gameObject;
            Transform sphere = FindDeepChild(localPlayer.transform, "Sphere");
            Assert.IsNotNull(sphere, "Runtime player should contain a child named Sphere.");
            Assert.IsFalse(sphere.gameObject.activeSelf, "Sphere should start disabled on the runtime player.");

            Collider playerCollider = localPlayer.GetComponentInChildren<Collider>(true);
            Assert.IsNotNull(playerCollider, "Runtime player should have at least one Collider for trigger tests.");

            pickupRelay.Trigger(playerCollider);
            bonusRelay.Trigger(playerCollider);
            yield return null;

            Assert.IsFalse(pickupRelay.gameObject.activeSelf,
                "Pickup relay should disable its own GameObject when the local player triggers it.");
            Assert.IsTrue(sphere.gameObject.activeSelf,
                "Bonus relay should enable the Sphere child of the entering player.");
        }

        private NetworkContextActionRelay[] FindAllNetworkContextActionRelaysInLoadedScenes()
        {
            List<NetworkContextActionRelay> relays = new List<NetworkContextActionRelay>();
            foreach (GameObject root in _ownedScene.GetRootGameObjects())
            {
                relays.AddRange(root.GetComponentsInChildren<NetworkContextActionRelay>(true));
            }

            return relays.ToArray();
        }

        private static NetworkContextActionRelay FindNetworkContextActionRelayInLoadedScenes()
        {
            NetworkContextActionRelay[] relays = Object.FindObjectsByType<NetworkContextActionRelay>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < relays.Length; i++)
            {
                if (relays[i] != null)
                {
                    return relays[i];
                }
            }

            const string triggerName = "Trigger Cube (1)";
            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                Scene candidate = SceneManager.GetSceneAt(s);
                if (!candidate.isLoaded)
                {
                    continue;
                }

                GameObject[] roots = candidate.GetRootGameObjects();
                for (int r = 0; r < roots.Length; r++)
                {
                    Transform found = FindDeepChild(roots[r].transform, triggerName);
                    if (found != null)
                    {
                        NetworkContextActionRelay onFound = found.GetComponent<NetworkContextActionRelay>();
                        if (onFound != null)
                        {
                            return onFound;
                        }
                    }
                }
            }

            return null;
        }

        private static Transform FindDeepChild(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName))
            {
                return null;
            }

            if (root.name == childName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeepChild(root.GetChild(i), childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
#endif
