#if MIRROR
using System.Collections;
using System.Reflection;
using Mirror;
using Neo.Network;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    /// <summary>A dummy transport with a settable port, like Telepathy / KCP / SimpleWeb.</summary>
    public class PortDummyTransport : DummyTransport, PortTransport
    {
        public ushort Port { get; set; } = 1;
    }

    public class NeoNetworkBootstrapTests
    {
        private GameObject _managerObject;
        private TestNetworkManager _manager;
        private PortDummyTransport _transport;
        private GameObject _bootstrapObject;
        private NeoNetworkBootstrap _bootstrap;
        private GameObject _playerPrefab;
        private int _savedFrameRate;
        private int _savedVSync;
        private bool _savedRunInBackground;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _savedFrameRate = Application.targetFrameRate;
            _savedVSync = QualitySettings.vSyncCount;
            _savedRunInBackground = Application.runInBackground;

            _managerObject = new GameObject("BootstrapManager");
            _managerObject.SetActive(false);
            _transport = _managerObject.AddComponent<PortDummyTransport>();
            _manager = _managerObject.AddComponent<TestNetworkManager>();
            _manager.transport = _transport;
            Transport.active = _transport;
            _playerPrefab = new GameObject("BootstrapPlayerPrefab");
            NetworkTestHelper.SetAssetId(_playerPrefab.AddComponent<NetworkIdentity>(), 86101);
            _manager.playerPrefab = _playerPrefab;
            _managerObject.SetActive(true);

            _bootstrapObject = new GameObject("Bootstrap");
            _bootstrap = _bootstrapObject.AddComponent<NeoNetworkBootstrap>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_bootstrap != null)
            {
                _bootstrap.StopNetwork();
            }

            if (_manager != null)
            {
                _manager.StopNetwork();
            }

            yield return null;

            if (_bootstrapObject != null)
            {
                Object.DestroyImmediate(_bootstrapObject);
            }

            if (_managerObject != null)
            {
                Object.DestroyImmediate(_managerObject);
            }

            if (_playerPrefab != null)
            {
                Object.DestroyImmediate(_playerPrefab);
            }

            NetworkClient.ClearSpawners();
            NetworkServer.Shutdown();
            NetworkClient.Shutdown();
            Application.targetFrameRate = _savedFrameRate;
            QualitySettings.vSyncCount = _savedVSync;
            Application.runInBackground = _savedRunInBackground;
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, float timeoutSeconds = 5f)
        {
            float end = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition() && Time.realtimeSinceStartup < end)
            {
                yield return null;
            }
        }

        private void PretendTheCommandLineAskedFor(NeoStartupMode mode)
        {
            FieldInfo field = typeof(NeoNetworkBootstrap).GetField("_commandLineMode",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, "the bootstrap remembers what the command line asked for");
            field.SetValue(_bootstrap, mode);
        }

        [UnityTest]
        public IEnumerator WithoutNetworkSwitches_TheBootstrapStaysSolo()
        {
            yield return null;
            Assert.That(_bootstrap.StartupMode, Is.EqualTo(NeoStartupMode.Solo));
            Assert.That(_bootstrap.HasStartedNetwork, Is.False);
            Assert.That(NetworkServer.active, Is.False);
            Assert.That(NetworkClient.active, Is.False);
            Assert.That(NeoNetworkBootstrap.Active, Is.SameAs(_bootstrap));
            Assert.That(NeoNetworkBootstrap.HasInstance, Is.True);
        }

        [UnityTest]
        public IEnumerator Solo_IsNotSomethingToStart()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Solo is the absence"));
            Assert.That(_bootstrap.StartAs(NeoStartupMode.Solo), Is.False);
            Assert.That(_bootstrap.StartNetwork(NeoStartupIntent.Solo(NeoStartupDefaults.Default)), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ADedicatedServer_CannotBeStartedFromTheGame()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("only be started from the command line"));
            NeoStartupIntent dedicated = NeoStartupCommandLine.Parse(new[] { "-server" });

            Assert.That(_bootstrap.StartNetwork(dedicated), Is.False);
            Assert.That(NetworkServer.active, Is.False);
            Assert.That(NeoNetworkBootstrap.LocalPresentationSuppressed, Is.False, "a refused request changes nothing");
            yield return null;
        }

        [UnityTest]
        public IEnumerator StartAsHost_StartsOnce_AppliesSettings_AndStops()
        {
            int startedEvents = 0;
            _bootstrap.OnNetworkStartedEvent.AddListener(() => startedEvents++);
            NeoStartupIntent intent = NeoStartupCommandLine.Parse(new[] { "-host", "-port", "7790", "-maxplayers", "7" });

            Assert.That(_bootstrap.StartNetwork(intent), Is.True);
            yield return WaitUntil(() => NetworkServer.active && NetworkClient.isConnected);

            Assert.That(NetworkServer.active, Is.True);
            Assert.That(NetworkClient.active, Is.True, "a host is server + client");
            Assert.That(_bootstrap.HasStartedNetwork, Is.True);
            Assert.That(_manager.maxConnections, Is.EqualTo(7));
            Assert.That(_transport.Port, Is.EqualTo(7790), "the port reached the transport");
            Assert.That(Application.runInBackground, Is.True, "a networked peer keeps running when unfocused");
            Assert.That(startedEvents, Is.EqualTo(1));

            Assert.That(_bootstrap.StartAs(NeoStartupMode.Host), Is.False, "a second start is ignored");
            Assert.That(startedEvents, Is.EqualTo(1));

            _bootstrap.StopNetwork();
            yield return WaitUntil(() => !NetworkServer.active && !NetworkClient.active);
            Assert.That(NetworkServer.active, Is.False);
            Assert.That(_bootstrap.HasStartedNetwork, Is.False);
        }

        [UnityTest]
        public IEnumerator ASecondRequest_WhileRunning_ChangesNothing()
        {
            _bootstrap.StartAs(NeoStartupMode.Host);
            yield return WaitUntil(() => NetworkServer.active);
            NeoStartupMode before = _bootstrap.Intent.Mode;

            Assert.That(_bootstrap.StartAs(NeoStartupMode.Client), Is.False);

            Assert.That(_bootstrap.Intent.Mode, Is.EqualTo(before), "the running session's intent is not overwritten");
            Assert.That(NetworkServer.active, Is.True);
        }

        [UnityTest]
        public IEnumerator StartAsClient_SetsTheAddressBeforeConnecting()
        {
            _bootstrap.StartAsClient("10.1.2.3");
            yield return null;

            Assert.That(_bootstrap.Intent.Mode, Is.EqualTo(NeoStartupMode.Client));
            Assert.That(_bootstrap.Intent.Address, Is.EqualTo("10.1.2.3"));
            Assert.That(_manager.networkAddress, Is.EqualTo("10.1.2.3"));
            Assert.That(NetworkClient.active, Is.True);
            Assert.That(NetworkServer.active, Is.False);
        }

        [UnityTest]
        public IEnumerator ADedicatedServerFromTheCommandLine_SuppressesPresentation_AndKeepsTheFrameCap()
        {
            GameObject cameraObject = new GameObject("Camera", typeof(Camera), typeof(AudioListener));
            GameObject canvasObject = new GameObject("Canvas", typeof(Canvas));
            try
            {
                PretendTheCommandLineAskedFor(NeoStartupMode.DedicatedServer);
                NeoStartupIntent dedicated = NeoStartupCommandLine.Parse(new[] { "-server" });

                Assert.That(_bootstrap.StartNetwork(dedicated), Is.True);
                yield return WaitUntil(() => NetworkServer.active);

                Assert.That(NetworkServer.active, Is.True);
                Assert.That(NetworkClient.active, Is.False, "a dedicated server has no local client");
                Assert.That(NeoNetworkBootstrap.LocalPresentationSuppressed, Is.True);
                Assert.That(cameraObject.GetComponent<Camera>().enabled, Is.False);
                Assert.That(cameraObject.GetComponent<AudioListener>().enabled, Is.False);
                Assert.That(canvasObject.GetComponent<Canvas>().enabled, Is.False);
                Assert.That(NeoNetworkBootstrap.OwnsFrameRate, Is.True);
                Assert.That(Application.targetFrameRate, Is.EqualTo(60), "Mirror's headless frame-rate reset must not undo the cap");
                Assert.That(QualitySettings.vSyncCount, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(canvasObject);
            }
        }

        [UnityTest]
        public IEnumerator StatusLine_DescribesTheRole_AndCarriesTheGameSuffix()
        {
            _bootstrap.StatusExtraProvider = () => "humans=1 bots=4";
            _bootstrap.StartAsHost();
            yield return WaitUntil(() => _manager.IsConnectionSpawned(NetworkServer.localConnection));

            string line = _bootstrap.BuildStatusLine();
            StringAssert.Contains("[Server] status:", line);
            StringAssert.Contains("mode=Host", line);
            StringAssert.Contains("connections=1", line);
            StringAssert.Contains("spawned=1", line);
            StringAssert.Contains("humans=1 bots=4", line);
            StringAssert.Contains("uptime=", line);
        }

        [UnityTest]
        public IEnumerator AThrowingStatusProvider_DoesNotBreakTheStatusLine()
        {
            _bootstrap.StatusExtraProvider = () => throw new System.InvalidOperationException("provider bug");
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("provider bug"));

            string line = _bootstrap.BuildStatusLine();

            StringAssert.Contains("[Server] status:", line);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ASecondBootstrap_DisablesItselfAndTheFirstKeepsOwnership()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("second bootstrap"));
            GameObject second = new GameObject("SecondBootstrap");
            NeoNetworkBootstrap duplicate = second.AddComponent<NeoNetworkBootstrap>();
            yield return null;

            Assert.That(duplicate.enabled, Is.False);
            Assert.That(NeoNetworkBootstrap.Active, Is.SameAs(_bootstrap));
            Object.DestroyImmediate(second);
            Assert.That(NeoNetworkBootstrap.Active, Is.SameAs(_bootstrap), "destroying the duplicate must not release the first one's ownership");
        }

        [UnityTest]
        public IEnumerator Destroying_TheBootstrap_ReleasesTheStaticFlags()
        {
            Object.DestroyImmediate(_bootstrapObject);
            yield return null;
            Assert.That(NeoNetworkBootstrap.Active, Is.Null);
            Assert.That(NeoNetworkBootstrap.HasInstance, Is.False);
            Assert.That(NeoNetworkBootstrap.LocalPresentationSuppressed, Is.False);
            Assert.That(NeoNetworkBootstrap.OwnsFrameRate, Is.False);
        }
    }
}
#endif
