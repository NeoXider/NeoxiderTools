#if MIRROR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Mirror;
using Mirror.Discovery;
using Neo.Network;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    public sealed class Audit20261023NeoNetworkUriTransport : DummyTransport
    {
        public Uri ConnectedUri;

        public override void ClientConnect(Uri uri)
        {
            ConnectedUri = uri;
        }
    }

    public sealed class Audit20261023NeoNetworkLobbyTests
    {
        private GameObject _managerObject;

        [SetUp]
        public void SetUp()
        {
            NetworkClient.Shutdown();
            NetworkServer.Shutdown();
            NetworkManager.ResetStatics();
            _managerObject = new GameObject(nameof(Audit20261023NeoNetworkLobbyTests));
            _managerObject.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            if (NetworkManager.singleton != null)
            {
                NetworkManager.singleton.StopClient();
            }

            Object.DestroyImmediate(_managerObject);
            NetworkClient.Shutdown();
            NetworkServer.Shutdown();
            NetworkManager.ResetStatics();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Discovery_ManualAndQuickPlayConnectionsPreserveTheAdvertisedUri(bool quickPlay)
        {
            Audit20261023NeoNetworkUriTransport transport =
                _managerObject.AddComponent<Audit20261023NeoNetworkUriTransport>();
            TestNetworkManager manager = _managerObject.AddComponent<TestNetworkManager>();
            manager.transport = transport;
            manager.autoCreatePlayer = false;
            manager.dontDestroyOnLoad = false;
            Transport.active = transport;
            _managerObject.SetActive(true);
            GameObject discoveryObject = new GameObject("Audit LAN discovery");
            discoveryObject.transform.SetParent(_managerObject.transform);
            NeoNetworkDiscovery discovery = discoveryObject.AddComponent<NeoNetworkDiscovery>();
            discovery.enabled = false; // No UDP discovery sockets; deliver Mirror's discovered response directly.
            Uri advertised = new Uri("tcp4://127.0.0.1:17891");
            int resolutions = 0;
            discovery.OnQuickPlayResolved.AddListener(host => resolutions += host ? 100 : 1);
            if (quickPlay)
            {
                FieldInfo active = typeof(NeoNetworkDiscovery).GetField("_quickPlayActive",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(active, Is.Not.Null);
                active.SetValue(discovery, true);
            }

            discoveryObject.GetComponent<NetworkDiscovery>().OnServerFound.Invoke(new ServerResponse
            {
                serverId = 12345,
                uri = advertised
            });
            if (!quickPlay)
            {
                discovery.ConnectToFirstServer();
            }

            Assert.That(transport.ConnectedUri, Is.EqualTo(advertised));
            Assert.That(transport.ConnectedUri.Port, Is.EqualTo(17891));
            Assert.That(NetworkClient.isConnecting, Is.True);
            Assert.That(resolutions, Is.EqualTo(quickPlay ? 1 : 0));
        }

        [Test]
        public void Presenter_JoinLeaveAndPersistentOutputTargetsHaveTypedRuntimePaths()
        {
            NeoLobbyPresenter presenter = CreatePresenter();
            List<string> statuses = new List<string>();
            List<string> counts = new List<string>();
            presenter.OnStatusChanged.AddListener(statuses.Add);
            presenter.OnPlayerCountTextChanged.AddListener(counts.Add);
            presenter.SetAddress(" 127.0.0.1 ");
            _managerObject.SetActive(true);
            Assert.That(presenter.Address, Is.EqualTo("127.0.0.1"));
            Assert.That(counts, Is.EqualTo(new[] { "Players: 0" }));
            presenter.RefreshView();
            Assert.That(counts.Count, Is.EqualTo(1), "unchanged view refresh does not repeat outputs");
            presenter.JoinLobby();
            Assert.That(NetworkClient.isConnecting, Is.True);
            AssertLobbyPrefabsRegistered(presenter.Manager);
            Assert.That(statuses[statuses.Count - 1], Is.EqualTo("Connecting..."));
            presenter.LeaveLobby();
            Assert.That(NetworkClient.active, Is.False);
            Assert.That(statuses[statuses.Count - 1], Is.EqualTo("Disconnected"));
        }

        [TestCase("count")]
        [TestCase("countText")]
        [TestCase("ready")]
        public void Presenter_ReentrantJoin_SupersedesRemainingOldSnapshotStages(string stage)
        {
            NeoLobbyPresenter presenter = CreatePresenter();
            presenter.SetAddress("127.0.0.1");
            List<string> transcript = ObservePresenter(presenter);
            ListenAtStage(presenter, stage, presenter.JoinLobby);

            _managerObject.SetActive(true);

            string[] expected = stage switch
            {
                "count" => new[] { "count:0", "status:Connecting..." },
                "countText" => new[] { "count:0", "countText:Players: 0", "status:Connecting..." },
                _ => new[] { "count:0", "countText:Players: 0", "ready:False", "status:Connecting..." }
            };
            Assert.That(NetworkClient.isConnecting, Is.True);
            AssertLobbyPrefabsRegistered(presenter.Manager);
            Assert.That(presenter.Manager.networkAddress, Is.EqualTo("127.0.0.1"));
            Assert.That(transcript, Is.EqualTo(expected), "Disconnected must not follow the nested Connecting snapshot");
            presenter.RefreshView();
            Assert.That(transcript, Is.EqualTo(expected), "an unchanged follow-up must leave the latest output intact");
        }

        [Test]
        public void Presenter_OrdinarySnapshot_PublishesCountCountTextReadyStatusInOrder()
        {
            NeoLobbyPresenter presenter = CreatePresenter();
            List<string> transcript = ObservePresenter(presenter);

            _managerObject.SetActive(true);

            string[] expected = { "count:0", "countText:Players: 0", "ready:False", "status:Disconnected" };
            Assert.That(transcript, Is.EqualTo(expected));
            presenter.RefreshView();
            Assert.That(transcript, Is.EqualTo(expected), "unchanged polling emits no duplicate outputs");
        }

        [TestCase("count")]
        [TestCase("countText")]
        [TestCase("ready")]
        [TestCase("status")]
        public void Presenter_UnchangedNestedRefresh_PreservesOrdinaryPendingStages(string stage)
        {
            NeoLobbyPresenter presenter = CreatePresenter();
            List<string> transcript = ObservePresenter(presenter);
            int nested = 0;
            ListenAtStage(presenter, stage, () =>
            {
                nested++;
                presenter.RefreshView();
            });

            _managerObject.SetActive(true);

            string[] expected = { "count:0", "countText:Players: 0", "ready:False", "status:Disconnected" };
            Assert.That(nested, Is.EqualTo(1));
            Assert.That(transcript, Is.EqualTo(expected), "unchanged reentry must not supersede pending stages");
            presenter.RefreshView();
            Assert.That(nested, Is.EqualTo(1));
            Assert.That(transcript, Is.EqualTo(expected));
        }

        [UnityTest]
        public IEnumerator QuickPlayTimeout_UsesRealtimeWhileGameplayIsPaused()
        {
            DummyTransport transport = _managerObject.AddComponent<DummyTransport>();
            TestNetworkManager manager = _managerObject.AddComponent<TestNetworkManager>();
            manager.transport = transport;
            manager.autoCreatePlayer = false;
            manager.dontDestroyOnLoad = false;
            Transport.active = transport;
            _managerObject.SetActive(true);
            GameObject discoveryObject = new GameObject("Audit paused discovery");
            discoveryObject.transform.SetParent(_managerObject.transform);
            NeoNetworkDiscovery discovery = discoveryObject.AddComponent<NeoNetworkDiscovery>();
            discovery.enabled = false;
            SetPrivate(discovery, "_autoAdvertiseOnHost", false);
            SetPrivate(discovery, "_autoDiscoverOnClient", false);
            SetPrivate(discovery, "_hostIfNoneFoundAfter", 0.01f);
            SetPrivate(discovery, "_quickPlayActive", true);
            SetPrivate(discovery, "_quickPlayStartedAt", Time.unscaledTimeAsDouble);
            int resolved = 0;
            discovery.OnQuickPlayResolved.AddListener(host => resolved += host ? 1 : 100);
            float previous = Time.timeScale;
            try
            {
                Time.timeScale = 0f;
                yield return new WaitForSecondsRealtime(0.03f);
                typeof(NeoNetworkDiscovery).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(discovery, null);
                Assert.That(resolved, Is.EqualTo(1));
                Assert.That(NetworkServer.active, Is.True);
            }
            finally
            {
                Time.timeScale = previous;
                manager.StopNetwork();
            }
        }

        private NeoLobbyPresenter CreatePresenter()
        {
            NeoLobbyManager manager = CreateLobbyManager();
            NeoLobbyPresenter presenter = _managerObject.AddComponent<NeoLobbyPresenter>();
            presenter.Manager = manager;
            return presenter;
        }

        private NeoLobbyManager CreateLobbyManager()
        {
            GameObject templates = new GameObject("Audit lobby prefab templates");
            templates.SetActive(false);
            templates.transform.SetParent(_managerObject.transform, false);
            NeoLobbyPlayer roomPlayer = CreatePlayerTemplate<NeoLobbyPlayer>(templates.transform,
                "Audit room player prefab", 0xA1022301u);
            NeoNetworkPlayer gamePlayer = CreatePlayerTemplate<NeoNetworkPlayer>(templates.transform,
                "Audit game player prefab", 0xA1022302u);

            DummyTransport transport = _managerObject.AddComponent<DummyTransport>();
            NeoLobbyManager manager = _managerObject.AddComponent<NeoLobbyManager>();
            manager.transport = transport;
            manager.autoCreatePlayer = false;
            manager.dontDestroyOnLoad = false;
            manager.roomPlayerPrefab = roomPlayer;
            manager.playerPrefab = gamePlayer.gameObject;
            Transport.active = transport;
            return manager;
        }

        private static T CreatePlayerTemplate<T>(Transform parent, string name, uint assetId) where T : NetworkBehaviour
        {
            GameObject template = new GameObject(name);
            template.SetActive(false);
            template.transform.SetParent(parent, false);
            NetworkIdentity identity = template.AddComponent<NetworkIdentity>();
            T player = template.AddComponent<T>();
            identity.sceneId = 0;
            // The public asset-bundle API assigns a nonzero id without private identity access.
            NetworkClient.RegisterPrefab(template, assetId);
            NetworkClient.UnregisterPrefab(template);
            Assert.That(identity.assetId, Is.EqualTo(assetId));
            Assert.That(identity.sceneId, Is.Zero);
            Assert.That(template.GetComponentsInChildren<NetworkIdentity>(true).Length, Is.EqualTo(1));
            // Active prefab roots stay under an inactive holder; clones can receive their ordinary Awake.
            template.SetActive(true);
            Assert.That(template.activeInHierarchy, Is.False);
            return player;
        }

        private static void AssertLobbyPrefabsRegistered(NeoLobbyManager manager)
        {
            Assert.That(manager.roomPlayerPrefab, Is.TypeOf<NeoLobbyPlayer>());
            Assert.That(manager.playerPrefab.GetComponent<NeoNetworkPlayer>(), Is.Not.Null);
            Assert.That(manager.roomPlayerPrefab.gameObject, Is.Not.SameAs(manager.playerPrefab));
            NetworkIdentity roomIdentity = manager.roomPlayerPrefab.GetComponent<NetworkIdentity>();
            NetworkIdentity gameIdentity = manager.playerPrefab.GetComponent<NetworkIdentity>();
            Assert.That(NetworkClient.prefabs.TryGetValue(roomIdentity.assetId, out GameObject room), Is.True);
            Assert.That(room, Is.SameAs(manager.roomPlayerPrefab.gameObject));
            Assert.That(NetworkClient.prefabs.TryGetValue(gameIdentity.assetId, out GameObject game), Is.True);
            Assert.That(game, Is.SameAs(manager.playerPrefab));
        }

        private static List<string> ObservePresenter(NeoLobbyPresenter presenter)
        {
            List<string> transcript = new List<string>();
            presenter.OnPlayerCountChanged.AddListener(count => transcript.Add($"count:{count}"));
            presenter.OnPlayerCountTextChanged.AddListener(text => transcript.Add($"countText:{text}"));
            presenter.OnReadyChanged.AddListener(ready => transcript.Add($"ready:{ready}"));
            presenter.OnStatusChanged.AddListener(status => transcript.Add($"status:{status}"));
            return transcript;
        }

        private static void ListenAtStage(NeoLobbyPresenter presenter, string stage, Action callback)
        {
            switch (stage)
            {
                case "count": presenter.OnPlayerCountChanged.AddListener(_ => callback()); break;
                case "countText": presenter.OnPlayerCountTextChanged.AddListener(_ => callback()); break;
                case "ready": presenter.OnReadyChanged.AddListener(_ => callback()); break;
                case "status": presenter.OnStatusChanged.AddListener(_ => callback()); break;
                default: Assert.Fail("Unknown presenter output stage: " + stage); break;
            }
        }

        private static void SetPrivate(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        [Test]
        public void LeaveLobby_CancelsAnAttemptBeforeTransportConnectionCompletes()
        {
            NeoLobbyManager manager = CreateLobbyManager();
            _managerObject.SetActive(true);
            manager.JoinLobby("127.0.0.1");
            Assert.That(NetworkClient.isConnecting, Is.True);
            Assert.That(NetworkClient.isConnected, Is.False);
            AssertLobbyPrefabsRegistered(manager);

            manager.LeaveLobby();

            Assert.That(NetworkClient.active, Is.False);
        }
    }
}
#endif
