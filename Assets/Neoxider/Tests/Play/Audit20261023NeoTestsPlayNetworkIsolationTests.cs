#if MIRROR
using System;
using System.Collections;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    /// <summary>Exclusive test-world checks, including native teardown after an interrupted legacy body.</summary>
    public sealed class Audit20261023NeoTestsPlayNetworkIsolationTests
    {
        private Func<IEnumerator> _nestedCleanup;
        private GameObject _sentinelObject;
        private Transport _previousTransport;
        private bool _previousBackground;
        private int _previousFrameRate;
        private int _previousVSync;
        private float _previousTimeScale;
        private UnityEngine.Random.State _previousRandomState;

        [SetUp]
        public void CaptureGlobalsAndPause()
        {
            _nestedCleanup = null;
            _sentinelObject = null;
            _previousTransport = Transport.active;
            _previousBackground = Application.runInBackground;
            _previousFrameRate = Application.targetFrameRate;
            _previousVSync = QualitySettings.vSyncCount;
            _previousTimeScale = Time.timeScale;
            _previousRandomState = UnityEngine.Random.state;
            Assert.That(NetworkServer.active || NetworkClient.active, Is.False);
            Assert.That(NetworkManager.singleton == null, Is.True);
            Time.timeScale = 0f;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            try
            {
                if (_nestedCleanup != null)
                {
                    yield return _nestedCleanup();
                    _nestedCleanup = null;
                }
            }
            finally
            {
                if (_sentinelObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(_sentinelObject);
                }

                Transport.active = _previousTransport;
                Application.runInBackground = _previousBackground;
                Application.targetFrameRate = _previousFrameRate;
                QualitySettings.vSyncCount = _previousVSync;
                Time.timeScale = _previousTimeScale;
                UnityEngine.Random.state = _previousRandomState;
            }
        }

        private IEnumerator RunPaused(Func<IEnumerator> setup, Func<IEnumerator> teardown,
            params Func<IEnumerator>[] scenarios)
        {
            foreach (Func<IEnumerator> scenario in scenarios)
            {
                _nestedCleanup = teardown;
                yield return setup();
                Assert.That(Time.timeScale, Is.Zero);
                yield return scenario();
                yield return teardown();
                _nestedCleanup = null;
                Assert.That(Time.timeScale, Is.Zero);
            }
        }

        [UnityTest]
        public IEnumerator PausedDispatcher_ReceivesAllEnvelopesAndKeepsOfflineFallback()
        {
            NetworkEventDispatcherTests fixture = new NetworkEventDispatcherTests();
            yield return RunPaused(fixture.SetUp, fixture.TearDown, fixture.DispatchGlobalEvent_Host_FiresOnce,
                fixture.DispatchGlobalEvent_DefaultAuthorityNone_AllowsSceneObject);
            yield return fixture.DispatchGlobalEvent_OfflineFallback_FiresLocally();
        }

        [UnityTest]
        public IEnumerator PausedRelay_PreservesPayloadsCountsAndNegativeControls()
        {
            NetworkActionRelayTests fixture = new NetworkActionRelayTests();
            yield return RunPaused(fixture.SetUp, fixture.TearDown, fixture.Trigger_Void_FiresOnAllClients,
                fixture.TriggerFloat_FiresWithPayload, fixture.TriggerString_FiresWithPayload,
                fixture.ServerOnly_Scope_FiresOnServer, fixture.OthersOnly_Scope_DoesNotEchoToHostSender,
                fixture.TriggerByName_FindsCorrectChannel, fixture.Trigger_InvalidIndex_DoesNotThrow);
        }

        [UnityTest]
        public IEnumerator PausedMoney_PreservesExactBalancesAndReceipts()
        {
            MoneyNetworkTests fixture = new MoneyNetworkTests();
            yield return RunPaused(fixture.SetUp, fixture.TearDown, fixture.Money_Shared_AddMoneyUpdatesGlobally,
                fixture.Money_Shared_SpendMoneyUpdatesGlobally, fixture.Money_Personal_ChangesRemainLocal);
        }

        [UnityTest]
        public IEnumerator PausedTools_ObserveStateAndEvents()
        {
            NetworkToolsTests fixture = new NetworkToolsTests();
            yield return RunPaused(fixture.SetUp, fixture.TearDown, fixture.Counter_CmdSetValue_UpdatesGlobally,
                fixture.Selector_CmdSyncState_UpdatesGlobally, fixture.Selector_NetworkedHostSet_FiresSelectionChangedOnce,
                fixture.Selector_NetworkedFillMode_ActivatesPrefix, fixture.Selector_NetworkedExcludeInclude_UpdatesPool,
                fixture.NeoCondition_CmdCheckResult_FiresGlobalEvents, fixture.RandomRange_CmdGenerate_UpdatesGlobally,
                fixture.Selector_CmdSetRandom_UpdatesGlobally);
        }

        [UnityTest]
        public IEnumerator PausedSingleton_ObservesAuthoritativeValue()
        {
            NetworkSingletonTests fixture = new NetworkSingletonTests();
            yield return RunPaused(fixture.SetUp, fixture.TearDown, fixture.IsInitialized_WhenSpawned_ReturnsTrue,
                fixture.ServerAuthority_InHostMode_ReturnsTrue, fixture.CmdSetTestValue_UpdatesSyncVar);
        }

        [UnityTest]
        public IEnumerator PausedInteraction_PreservesExactHostEventCounts()
        {
            InteractiveObjectNetworkTests fixture = new InteractiveObjectNetworkTests();
            yield return RunPaused(fixture.SetUp, fixture.TearDown,
                fixture.InteractiveObject_NetworkedHostPublicApi_FiresEachEventOnce,
                fixture.InteractiveObject_DefaultAuthorityNone_AllowsSceneObject);
            yield return fixture.InteractiveObject_OfflineFallback_FiresLocally();
        }

        [UnityTest]
        public IEnumerator PausedPhysics_ReceivesNativeEnterAndExitInItsOwnWorld()
        {
            PhysicsEventsNetworkTests fixture = new PhysicsEventsNetworkTests();
            yield return RunPaused(fixture.SetUp, fixture.TearDown, fixture.PhysicsEvents3D_Networked_FiresIfServer,
                fixture.PhysicsEvents3D_NotNetworked_FiresLocally);
        }

#if UNITY_EDITOR
        private enum BorrowedEventSystemState
        {
            Registered,
            Disabled,
            Destroyed
        }

        [UnityTest]
        public IEnumerator MultScene_CleanupRestoresRegisteredBorrowedEventSystem()
        {
            yield return VerifyBorrowedEventSystemCleanup(BorrowedEventSystemState.Registered);
        }

        [UnityTest]
        public IEnumerator MultScene_CleanupLeavesDisabledBorrowedEventSystemUnchanged()
        {
            yield return VerifyBorrowedEventSystemCleanup(BorrowedEventSystemState.Disabled);
        }

        [UnityTest]
        public IEnumerator MultScene_CleanupDoesNotSelectDestroyedBorrowedEventSystem()
        {
            yield return VerifyBorrowedEventSystemCleanup(BorrowedEventSystemState.Destroyed);
        }

        private IEnumerator VerifyBorrowedEventSystemCleanup(BorrowedEventSystemState state)
        {
            UnityEngine.EventSystems.EventSystem ambient = UnityEngine.EventSystems.EventSystem.current;
            Neo.Save.ISaveProvider provider = Neo.Save.SaveProvider.CurrentProvider;
            GameObject replacementObject = null;
            try
            {
                _sentinelObject = new GameObject("BorrowedEventSystem");
                UnityEngine.EventSystems.EventSystem borrowed =
                    _sentinelObject.AddComponent<UnityEngine.EventSystems.EventSystem>();
                Assert.That(borrowed.isActiveAndEnabled, Is.True);
                UnityEngine.EventSystems.EventSystem.current = borrowed;
                Assert.That(UnityEngine.EventSystems.EventSystem.current, Is.SameAs(borrowed),
                    "Native OnEnable must register the actual original before it is captured.");
                MultSceneNetworkTests fixture = new MultSceneNetworkTests();
                _nestedCleanup = fixture.TearDown;
                yield return fixture.SetUp();

                if (state == BorrowedEventSystemState.Disabled)
                {
                    borrowed.enabled = false; // This regression owns it; native OnDisable unregisters it.
                    Assert.That(borrowed.isActiveAndEnabled, Is.False);
                }
                else if (state == BorrowedEventSystemState.Destroyed)
                {
                    UnityEngine.Object.DestroyImmediate(borrowed);
                    Assert.That(borrowed == null, Is.True);
                }

                replacementObject = new GameObject("NewCurrentEventSystem");
                UnityEngine.EventSystems.EventSystem replacement =
                    replacementObject.AddComponent<UnityEngine.EventSystems.EventSystem>();
                Assert.That(replacement.isActiveAndEnabled, Is.True);
                UnityEngine.EventSystems.EventSystem.current = replacement;
                Assert.That(UnityEngine.EventSystems.EventSystem.current, Is.SameAs(replacement));
                yield return fixture.TearDown();
                _nestedCleanup = null;

                Assert.That(_sentinelObject != null && replacementObject != null, Is.True);
                Assert.That(replacement.isActiveAndEnabled, Is.True);
                Assert.That(Neo.Save.SaveProvider.CurrentProvider, Is.SameAs(provider));
                if (state == BorrowedEventSystemState.Registered)
                {
                    Assert.That(borrowed.isActiveAndEnabled, Is.True);
                    Assert.That(UnityEngine.EventSystems.EventSystem.current, Is.SameAs(borrowed));
                }
                else
                {
                    Assert.That(UnityEngine.EventSystems.EventSystem.current, Is.SameAs(replacement),
                        "Cleanup must leave native registration alone when the old original is unavailable.");
                    if (state == BorrowedEventSystemState.Disabled)
                    {
                        Assert.That(borrowed != null, Is.True);
                        Assert.That(borrowed.enabled, Is.False, "Cleanup must not re-enable a borrowed system.");
                    }
                    else
                    {
                        Assert.That(borrowed == null, Is.True);
                    }
                }
            }
            finally
            {
                if (replacementObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(replacementObject);
                }

                if (ambient != null && ambient.isActiveAndEnabled &&
                    UnityEngine.EventSystems.EventSystem.current != ambient)
                {
                    UnityEngine.EventSystems.EventSystem.current = ambient;
                }
            }
        }

        [UnityTest]
        public IEnumerator MultScene_InterruptedPendingLoad_ReleasesItsSceneAndManager()
        {
            _sentinelObject = new GameObject("BorrowedSceneSentinel");
            _sentinelObject.SetActive(false);
            UnityEngine.SceneManagement.Scene borrowedScene = _sentinelObject.scene;
            Neo.Save.ISaveProvider provider = Neo.Save.SaveProvider.CurrentProvider;
            MultSceneNetworkTests fixture = new MultSceneNetworkTests();
            _nestedCleanup = fixture.TearDown;
            yield return fixture.SetUp();
            fixture.BeginOwnedSceneLoad();
            Assert.Throws<AssertionException>(() => Assert.Fail("intentional failure while the owned load is pending"));
            yield return fixture.TearDown();
            _nestedCleanup = null;

            Assert.That(ReferenceEquals(fixture.OwnedManager, null), Is.False,
                "Native loading must have created and ledgered a manager before cleanup can prove its destruction.");
            Assert.That(fixture.OwnedManager == null, Is.True);
            Assert.That(!fixture.OwnedScene.IsValid() || !fixture.OwnedScene.isLoaded, Is.True);
            int released = 0;
            foreach (GameObject owned in fixture.OwnedSceneObjects)
            {
                Assert.That(owned == null, Is.True, "All explicitly ledgered scene/DDOL objects must be released.");
                released++;
            }

            Assert.That(released, Is.GreaterThan(0));
            Assert.That(_sentinelObject != null, Is.True);
            Assert.That(_sentinelObject.activeSelf, Is.False);
            Assert.That(_sentinelObject.scene, Is.EqualTo(borrowedScene));
            Assert.That(borrowedScene.isLoaded, Is.True, "Additive cleanup must preserve the borrowed scene.");
            Assert.That(Neo.Save.SaveProvider.CurrentProvider, Is.SameAs(provider));
            Assert.That(NetworkManager.singleton == null, Is.True);
            Assert.That(NetworkServer.active || NetworkClient.active, Is.False);
        }

        [UnityTest]
        public IEnumerator MultScene_InterruptedAfterAwake_ReleasesDdolAndPreservesBorrowedRoot()
        {
            _sentinelObject = new GameObject("BorrowedDdolSentinel");
            UnityEngine.Object.DontDestroyOnLoad(_sentinelObject);
            _sentinelObject.transform.position = new Vector3(7f, 11f, 13f);
            MultSceneNetworkTests fixture = new MultSceneNetworkTests();
            _nestedCleanup = fixture.TearDown;
            yield return fixture.SetUp();
            yield return fixture.LoadOwnedScene();
            Assert.That(fixture.OwnedScene.isLoaded, Is.True);
            Assert.That(fixture.OwnedManager != null, Is.True);
            GameObject manager = fixture.OwnedManager.gameObject;
            Assert.That(manager.scene.name, Is.EqualTo("DontDestroyOnLoad"),
                "The authored Mult manager has dontDestroyOnLoad enabled; establish the actual moved-root precondition.");
            Assert.That(fixture.OwnedManager.ScenePlayerTemplate.scene, Is.EqualTo(fixture.OwnedScene));
            Assert.Throws<AssertionException>(() => Assert.Fail("intentional failure after scene Awake and DDOL move"));
            yield return fixture.TearDown();
            _nestedCleanup = null;

            Assert.That(manager == null, Is.True);
            Assert.That(!fixture.OwnedScene.IsValid() || !fixture.OwnedScene.isLoaded, Is.True);
            foreach (GameObject owned in fixture.OwnedSceneObjects)
            {
                Assert.That(owned == null, Is.True);
            }

            Assert.That(_sentinelObject != null, Is.True);
            Assert.That(_sentinelObject.scene.name, Is.EqualTo("DontDestroyOnLoad"));
            Assert.That(_sentinelObject.transform.position, Is.EqualTo(new Vector3(7f, 11f, 13f)));
            Assert.That(NetworkManager.singleton == null, Is.True);
            Assert.That(NetworkServer.active || NetworkClient.active, Is.False);
        }

        [UnityTest]
        public IEnumerator MultScene_DuringLoadBorrowedDdolRoot_IsNeverOwnedAndSurvivesCleanup()
        {
            _sentinelObject = new GameObject("PreexistingBorrowedDdolRoot");
            UnityEngine.Object.DontDestroyOnLoad(_sentinelObject);
            _sentinelObject.transform.position = new Vector3(19f, 23f, 29f);
            UnityEngine.SceneManagement.Scene borrowedDdolScene = _sentinelObject.scene;
            Transform borrowedParent = _sentinelObject.transform.parent;
            Vector3 borrowedPosition = _sentinelObject.transform.position;
            UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            Neo.Save.ISaveProvider provider = Neo.Save.SaveProvider.CurrentProvider;
            UnityEngine.EventSystems.EventSystem eventSystem = UnityEngine.EventSystems.EventSystem.current;
            CursorLockMode cursorLock = Cursor.lockState;
            bool cursorVisible = Cursor.visible;
            Transport borrowedTransport = _sentinelObject.AddComponent<DummyTransport>();
            Transport.active = borrowedTransport;
            Application.runInBackground = false;
            Application.targetFrameRate = 37;
            QualitySettings.vSyncCount = 0;
            UnityEngine.Random.InitState(4901);
            UnityEngine.Random.State randomState = UnityEngine.Random.state;
            float timeScale = Time.timeScale;

            MultSceneNetworkTests fixture = new MultSceneNetworkTests();
            GameObject duringLoadRoot = null;
            GameObject duringLoadChild = null;
            UnityEngine.SceneManagement.Scene observedScene = default;
            UnityEngine.SceneManagement.Scene externalScene = default;
            Transform externalParent = null;
            Vector3 externalPosition = new Vector3(31f, 37f, 41f);
            Vector3 externalChildLocalPosition = new Vector3(2f, 3f, 5f);
            int callbackCount = 0;
            UnityEngine.Events.UnityAction<UnityEngine.SceneManagement.Scene,
                UnityEngine.SceneManagement.LoadSceneMode> earlierObserver = (scene, mode) =>
            {
                if (scene.path != "Assets/Scenes/Mult.unity" ||
                    mode != UnityEngine.SceneManagement.LoadSceneMode.Additive)
                {
                    return;
                }

                callbackCount++;
                if (callbackCount != 1)
                {
                    return;
                }

                observedScene = scene;
                duringLoadRoot = new GameObject("ExternalDuringMultLoadRoot");
                UnityEngine.Object.DontDestroyOnLoad(duringLoadRoot);
                duringLoadRoot.transform.position = externalPosition;
                duringLoadChild = new GameObject("ExternalDuringMultLoadChild");
                duringLoadChild.transform.SetParent(duringLoadRoot.transform, false);
                duringLoadChild.transform.localPosition = externalChildLocalPosition;
                externalScene = duringLoadRoot.scene;
                externalParent = duringLoadRoot.transform.parent;
            };

            // Registered before BeginOwnedSceneLoad subscribes the fixture's ownership callback.
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += earlierObserver;
            try
            {
                _nestedCleanup = fixture.TearDown;
                yield return fixture.SetUp();
                yield return fixture.LoadOwnedScene();
                Assert.That(callbackCount, Is.EqualTo(1));
                Assert.That(observedScene, Is.EqualTo(fixture.OwnedScene));
                Assert.That(duringLoadRoot != null && duringLoadChild != null, Is.True,
                    "The actual earlier native callback must create the external hierarchy during this exact load.");
                Assert.That(externalScene, Is.EqualTo(borrowedDdolScene));
                Assert.That(externalParent, Is.Null, "The reproduction requires a new DDOL root.");
                Assert.That(fixture.OwnedScene.isLoaded, Is.True);
                Assert.That(fixture.OwnedManager != null, Is.True);
                GameObject manager = fixture.OwnedManager.gameObject;
                Assert.That(manager.scene, Is.EqualTo(borrowedDdolScene),
                    "The real owned manager must have moved into DDOL, independently of the external root.");
                Assert.That(fixture.OwnedManager.ScenePlayerTemplate.scene, Is.EqualTo(fixture.OwnedScene));
                Assert.That(Neo.Save.SaveProvider.CurrentProvider, Is.Not.SameAs(provider));

                bool managerLedgered = false;
                foreach (GameObject owned in fixture.OwnedSceneObjects)
                {
                    Assert.That(ReferenceEquals(owned, duringLoadRoot), Is.False,
                        "Appearing during scene load does not transfer the external root's ownership.");
                    Assert.That(ReferenceEquals(owned, duringLoadChild), Is.False);
                    managerLedgered |= ReferenceEquals(owned, manager);
                }

                Assert.That(managerLedgered, Is.True, "The genuine moved manager must still be owned.");
                NetworkTestHelper.UseDummyTransport(fixture.OwnedManager);
                fixture.OwnedManager.StartHost();
                yield return NetworkTestHelper.WaitForHost(requireLocalPlayer: true);
                Assert.That(NetworkManager.singleton, Is.SameAs(fixture.OwnedManager));
                Assert.That(NetworkServer.active && NetworkClient.isConnected, Is.True);
                yield return fixture.TearDown();
                _nestedCleanup = null;

                Assert.That(manager == null && fixture.OwnedManager == null, Is.True);
                Assert.That(!fixture.OwnedScene.IsValid() || !fixture.OwnedScene.isLoaded, Is.True);
                int released = 0;
                foreach (GameObject owned in fixture.OwnedSceneObjects)
                {
                    Assert.That(ReferenceEquals(owned, duringLoadRoot), Is.False);
                    Assert.That(ReferenceEquals(owned, duringLoadChild), Is.False);
                    Assert.That(owned == null, Is.True, "Every actual owned scene/DDOL ledger entry must be released.");
                    released++;
                }

                Assert.That(released, Is.GreaterThan(0));
                Assert.That(duringLoadRoot != null && duringLoadChild != null, Is.True);
                Assert.That(duringLoadRoot.scene, Is.EqualTo(externalScene));
                Assert.That(ReferenceEquals(duringLoadRoot.transform.parent, externalParent), Is.True);
                Assert.That(duringLoadRoot.transform.position, Is.EqualTo(externalPosition));
                Assert.That(duringLoadChild.scene, Is.EqualTo(externalScene));
                Assert.That(duringLoadChild.transform.parent, Is.SameAs(duringLoadRoot.transform));
                Assert.That(duringLoadChild.transform.localPosition, Is.EqualTo(externalChildLocalPosition));
                Assert.That(_sentinelObject != null, Is.True);
                Assert.That(_sentinelObject.scene, Is.EqualTo(borrowedDdolScene));
                Assert.That(ReferenceEquals(_sentinelObject.transform.parent, borrowedParent), Is.True);
                Assert.That(_sentinelObject.transform.position, Is.EqualTo(borrowedPosition));
                Assert.That(activeScene.isLoaded, Is.True);
                Assert.That(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), Is.EqualTo(activeScene));
                Assert.That(Neo.Save.SaveProvider.CurrentProvider, Is.SameAs(provider));
                Assert.That(UnityEngine.EventSystems.EventSystem.current, Is.SameAs(eventSystem));
                Assert.That(Cursor.lockState, Is.EqualTo(cursorLock));
                Assert.That(Cursor.visible, Is.EqualTo(cursorVisible));
                Assert.That(Transport.active, Is.SameAs(borrowedTransport));
                Assert.That(Application.runInBackground, Is.False);
                Assert.That(Application.targetFrameRate, Is.EqualTo(37));
                Assert.That(QualitySettings.vSyncCount, Is.Zero);
                Assert.That(Time.timeScale, Is.EqualTo(timeScale));
                Assert.That(UnityEngine.Random.state, Is.EqualTo(randomState));
                Assert.That(NetworkManager.singleton == null, Is.True);
                Assert.That(NetworkServer.active || NetworkClient.active, Is.False);
            }
            finally
            {
                UnityEngine.SceneManagement.SceneManager.sceneLoaded -= earlierObserver;
                if (duringLoadRoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(duringLoadRoot); // Only this observer's own hierarchy.
                }
            }
        }
#endif

        [UnityTest]
        public IEnumerator FailedPartialSetup_ReleasesOwnedRootAndRestoresGlobals()
        {
            NetworkTestSession session = new NetworkTestSession();
            _nestedCleanup = session.Cleanup;
            GameObject partialRoot = session.Own(new GameObject("PartialNetworkSetup"));
            Transport previousTransport = Transport.active;
            bool previousBackground = Application.runInBackground;
            int previousFrameRate = Application.targetFrameRate;
            int previousVSync = QualitySettings.vSyncCount;
            AssertionException failure = Assert.Throws<AssertionException>(() =>
            {
                Transport.active = partialRoot.AddComponent<DummyTransport>();
                Application.runInBackground = !previousBackground;
                Application.targetFrameRate = 43;
                QualitySettings.vSyncCount = previousVSync == 0 ? 1 : 0;
                Assert.Fail("intentional partial setup failure before manager creation");
            });
            StringAssert.Contains("partial setup", failure.Message);
            yield return session.Cleanup();
            yield return session.Cleanup();
            _nestedCleanup = null;
            Assert.That(partialRoot == null, Is.True);
            Assert.That(Transport.active, Is.SameAs(previousTransport));
            Assert.That(Application.runInBackground, Is.EqualTo(previousBackground));
            Assert.That(Application.targetFrameRate, Is.EqualTo(previousFrameRate));
            Assert.That(QualitySettings.vSyncCount, Is.EqualTo(previousVSync));
            Assert.That(Time.timeScale, Is.Zero);
        }

        [Test]
        public void InactiveBorrowedManager_IsRejectedWithoutTakingOwnership()
        {
            _sentinelObject = new GameObject("InactiveBorrowedManager");
            _sentinelObject.SetActive(false);
            DummyTransport transport = _sentinelObject.AddComponent<DummyTransport>();
            TestNetworkManager manager = _sentinelObject.AddComponent<TestNetworkManager>();
            manager.transport = transport;
            Transport previousTransport = Transport.active;
            Assert.Throws<AssertionException>(() => new NetworkTestSession());
            Assert.That(manager != null, Is.True);
            Assert.That(_sentinelObject.activeSelf, Is.False);
            Assert.That(NetworkManager.singleton == null, Is.True);
            Assert.That(Transport.active, Is.SameAs(previousTransport));
        }

        [UnityTest]
        public IEnumerator TwoFixtures_RestoreExactBorrowedGlobalsAndTransport()
        {
            _sentinelObject = new GameObject("BorrowedTransportSentinel");
            Transport sentinelTransport = _sentinelObject.AddComponent<DummyTransport>();
            Transport.active = sentinelTransport;
            Application.runInBackground = false;
            Application.targetFrameRate = 37;
            QualitySettings.vSyncCount = 0;
            UnityEngine.Random.InitState(731);
            UnityEngine.Random.State expectedRandomState = UnityEngine.Random.state;

            for (int round = 0; round < 2; round++)
            {
                NetworkEventDispatcherTests fixture = new NetworkEventDispatcherTests();
                _nestedCleanup = fixture.TearDown;
                yield return fixture.SetUp();
                GameObject manager = NetworkManager.singleton.gameObject;
                GameObject originalPlayerTemplate = NetworkManager.singleton.playerPrefab;
                yield return fixture.DispatchGlobalEvent_Host_FiresOnce();
                yield return fixture.TearDown();
                _nestedCleanup = null;

                Assert.That(manager == null, Is.True);
                Assert.That(originalPlayerTemplate == null, Is.True);
                Assert.That(Transport.active, Is.SameAs(sentinelTransport));
                Assert.That(Application.runInBackground, Is.False);
                Assert.That(Application.targetFrameRate, Is.EqualTo(37));
                Assert.That(QualitySettings.vSyncCount, Is.Zero);
                Assert.That(Time.timeScale, Is.Zero);
                Assert.That(UnityEngine.Random.state, Is.EqualTo(expectedRandomState));
                Assert.That(_sentinelObject != null, Is.True);
            }
        }

        [UnityTest]
        public IEnumerator ForeignRunningSession_IsRejectedWithoutChangingItsManagerOrTransport()
        {
            NetworkEventDispatcherTests fixture = new NetworkEventDispatcherTests();
            _nestedCleanup = fixture.TearDown;
            yield return fixture.SetUp();
            NetworkManager manager = NetworkManager.singleton;
            Transport transport = Transport.active;
            bool background = Application.runInBackground;
            int frameRate = Application.targetFrameRate;
            int vSync = QualitySettings.vSyncCount;

            Assert.Throws<AssertionException>(() => new NetworkTestSession());
            Assert.That(NetworkManager.singleton, Is.SameAs(manager));
            Assert.That(Transport.active, Is.SameAs(transport));
            Assert.That(NetworkServer.active && NetworkClient.isConnected, Is.True);
            Assert.That(Application.runInBackground, Is.EqualTo(background));
            Assert.That(Application.targetFrameRate, Is.EqualTo(frameRate));
            Assert.That(QualitySettings.vSyncCount, Is.EqualTo(vSync));
            yield return fixture.DispatchGlobalEvent_Host_FiresOnce();
        }

        [UnityTest]
        public IEnumerator InterruptedScenePlayerBody_ReleasesUnspawnedCopyAndPickup()
        {
            _sentinelObject = new GameObject("UnrelatedPickupSentinel");
            NeoNetworkManagerScenePlayerTests fixture = new NeoNetworkManagerScenePlayerTests();
            _nestedCleanup = fixture.TearDown;
            yield return fixture.SetUp();
            IEnumerator body = fixture.ScenePlayerTemplate_TriggerPickup_EnablesWeaponOnlyForEnteredPlayerClone();
            GameObject pickup = null;
            try
            {
                for (int step = 0; step < 8 && pickup == null; step++)
                {
                    Assert.That(body.MoveNext(), Is.True, "The pickup allocation checkpoint must be reached.");
                    pickup = GameObject.Find("WeaponPickup");
                    if (pickup == null)
                    {
                        yield return body.Current;
                    }
                }

                Assert.That(pickup, Is.Not.Null);
                GameObject unspawnedCopy = null;
                foreach (NetworkIdentity identity in UnityEngine.Object.FindObjectsByType<NetworkIdentity>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (identity.netId == 0 && identity.sceneId == 0)
                    {
                        Assert.That(unspawnedCopy, Is.Null, "Only one additional unspawned player copy is expected.");
                        unspawnedCopy = identity.gameObject;
                    }
                }

                Assert.That(unspawnedCopy, Is.Not.Null);
                Assert.Throws<AssertionException>(() => Assert.Fail("intentional failure after pickup/copy allocation"));
                (body as IDisposable)?.Dispose();
                yield return fixture.TearDown();
                _nestedCleanup = null;
                Assert.That(pickup == null, Is.True);
                Assert.That(unspawnedCopy == null, Is.True);
                Assert.That(_sentinelObject != null, Is.True);
            }
            finally
            {
                (body as IDisposable)?.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator InterruptedBootstrapBody_ReleasesDuplicateAndPreservesForeignObject()
        {
            _sentinelObject = new GameObject("UnrelatedBootstrapSentinel");
            NeoNetworkBootstrapTests fixture = new NeoNetworkBootstrapTests();
            _nestedCleanup = fixture.TearDown;
            yield return fixture.SetUp();
            IEnumerator body = fixture.ASecondBootstrap_DisablesItselfAndTheFirstKeepsOwnership();
            try
            {
                Assert.That(body.MoveNext(), Is.True);
                GameObject duplicate = GameObject.Find("SecondBootstrap");
                Assert.That(duplicate, Is.Not.Null);
                Assert.Throws<AssertionException>(() => Assert.Fail("intentional failure after duplicate allocation"));
                (body as IDisposable)?.Dispose();
                yield return fixture.TearDown();
                _nestedCleanup = null;
                Assert.That(duplicate == null, Is.True);
                Assert.That(_sentinelObject != null, Is.True);
            }
            finally
            {
                (body as IDisposable)?.Dispose();
            }
        }
    }
}
#endif
