#if MIRROR
using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Neo.Network;
using Neo.Network.Realtime;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     Shared helper for network play-mode tests.
    ///     Mirror's assetId setter is internal, so tests must use field reflection.
    /// </summary>
    internal static class NetworkTestHelper
    {
        private static int _barrierSequence;
        private static readonly System.Reflection.FieldInfo AssetIdField =
            typeof(NetworkIdentity).GetField("_assetId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        /// <summary>
        ///     Sets the assetId on a NetworkIdentity via private field reflection.
        ///     Mirror's assetId property has an internal setter inaccessible from Neo test assemblies.
        /// </summary>
        public static void SetAssetId(NetworkIdentity identity, uint id)
        {
            AssetIdField.SetValue(identity, id);
        }

        /// <summary>Fails a stalled network operation using real time, including while the game is paused.</summary>
        public static IEnumerator WaitUntil(Func<bool> condition, float timeoutSeconds = 5f,
            string description = "network condition")
        {
            if (condition == null)
            {
                throw new ArgumentNullException(nameof(condition));
            }

            if (timeoutSeconds < 0f || float.IsNaN(timeoutSeconds) || float.IsInfinity(timeoutSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));
            }

            double deadline = Time.realtimeSinceStartupAsDouble + timeoutSeconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartupAsDouble >= deadline)
                {
                    Assert.Fail($"Timed out waiting for {description} after {timeoutSeconds}s. " +
                                $"Server active: {NetworkServer.active}; client connected: {NetworkClient.isConnected}; " +
                                $"local player present: {NetworkClient.localPlayer != null}.");
                }

                yield return null;
            }
        }

        public static IEnumerator WaitForHost(bool requireLocalPlayer = false)
        {
            return WaitUntil(() => NetworkServer.active && NetworkClient.isConnected &&
                                   (!requireLocalPlayer || NetworkClient.localPlayer != null),
                description: requireLocalPlayer ? "host connection and local player" : "host connection");
        }

        public static IEnumerator WaitForSpawn(NetworkIdentity identity)
        {
            return WaitUntil(() => identity != null && identity.netId != 0 && identity.isServer && identity.isClient &&
                                   NetworkServer.localConnection != null &&
                                   identity.observers.ContainsKey(NetworkServer.localConnection.connectionId) &&
                                   NetworkClient.spawned.TryGetValue(identity.netId, out NetworkIdentity clientIdentity) &&
                                   clientIdentity == identity,
                description: "host spawn, client identity and observer");
        }

        /// <summary>Drains prior reliable host-to-client messages through an observed serialized control.</summary>
        public static IEnumerator ReliableBarrier(string description = "reliable host queue barrier")
        {
            Assert.That(NetworkServer.active && NetworkClient.isConnected && NetworkClient.ready &&
                        NetworkServer.localConnection != null && NetworkServer.localConnection.isReady, Is.True,
                "A reliable barrier requires a ready host loopback.");
            bool received = false;
            int sequence = unchecked(++_barrierSequence);
            using NetClientHandlers handlers = new NetClientHandlers();
            handlers.Add<Audit20261023NeoTestsPlayBarrierMessage>(message =>
            {
                if (message.Value == sequence)
                {
                    received = true;
                }
            });
            handlers.RegisterNow();
            NetworkServer.localConnection.Send(new Audit20261023NeoTestsPlayBarrierMessage { Value = sequence },
                Channels.Reliable);
            yield return WaitUntil(() => received, description: description);
        }

        /// <summary>Counts envelopes for one spawned identity; local callbacks cannot satisfy these counters.</summary>
        internal sealed class RpcObservation : IDisposable
        {
            private readonly uint _netId;
            public int Sent { get; private set; }
            public int Received { get; private set; }

            public RpcObservation(NetworkIdentity identity) : this(identity, true)
            {
            }

            public RpcObservation(NetworkIdentity identity, bool requireSpawned)
            {
                Assert.That(identity != null, Is.True);
                if (requireSpawned)
                {
                    Assert.That(identity.netId, Is.Not.EqualTo(0u));
                }
                _netId = identity.netId;
                Mirror.NetworkDiagnostics.OutMessageEvent += OnSent;
                Mirror.NetworkDiagnostics.InMessageEvent += OnReceived;
            }

            private void OnSent(Mirror.NetworkDiagnostics.MessageInfo info)
            {
                if (info.message is RpcMessage rpc && rpc.netId == _netId)
                {
                    Sent += info.count;
                }
            }

            private void OnReceived(Mirror.NetworkDiagnostics.MessageInfo info)
            {
                if (info.message is RpcMessage rpc && rpc.netId == _netId)
                {
                    Received += info.count;
                }
            }

            public void Dispose()
            {
                Mirror.NetworkDiagnostics.OutMessageEvent -= OnSent;
                Mirror.NetworkDiagnostics.InMessageEvent -= OnReceived;
            }
        }

        public static TestNetworkManager CreateTestNetworkManager(string name, out GameObject managerObject)
        {
            managerObject = new GameObject(name);
            managerObject.SetActive(false);

            Transport transport = managerObject.AddComponent<DummyTransport>();
            TestNetworkManager manager = managerObject.AddComponent<TestNetworkManager>();
            manager.transport = transport;
            Transport.active = transport;

            managerObject.SetActive(true);
            return manager;
        }

        public static DummyTransport UseDummyTransport(NetworkManager manager)
        {
            if (!manager.TryGetComponent(out DummyTransport transport))
            {
                transport = manager.gameObject.AddComponent<DummyTransport>();
            }

            manager.transport = transport;
            Transport.active = transport;
            return transport;
        }
    }

    public struct Audit20261023NeoTestsPlayBarrierMessage : NetworkMessage
    {
        public int Value;
    }

    /// <summary>Exclusive legacy-fixture ownership only; no gameplay or general-purpose test framework API.</summary>
    internal sealed class NetworkTestSession
    {
        private readonly List<UnityEngine.Object> _owned = new List<UnityEngine.Object>();
        private readonly Transport _previousTransport;
        private readonly bool _previousBackground;
        private readonly int _previousFrameRate;
        private readonly int _previousVSync;
        private readonly float _previousTimeScale;
        private readonly UnityEngine.Random.State _previousRandomState;
        private NeoNetworkManager _manager;
        private bool _cleanedUp;

        public NetworkTestSession()
        {
            Assert.That(NetworkServer.active || NetworkClient.active, Is.False,
                "A legacy network fixture must not take over a foreign session.");
            Assert.That(NetworkManager.singleton == null, Is.True,
                "Run legacy network fixtures in an isolated scene without a borrowed manager.");
            Assert.That(UnityEngine.Object.FindObjectsByType<NetworkManager>(
                FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty,
                "An inactive borrowed manager also requires a separate test world.");
            Assert.That(UnityEngine.Object.FindObjectsByType<NetworkIdentity>(
                FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty,
                "Host startup would claim borrowed scene identities; use an empty test world.");
            _previousTransport = Transport.active;
            _previousBackground = Application.runInBackground;
            _previousFrameRate = Application.targetFrameRate;
            _previousVSync = QualitySettings.vSyncCount;
            _previousTimeScale = Time.timeScale;
            _previousRandomState = UnityEngine.Random.state;
        }

        public T Own<T>(T value) where T : UnityEngine.Object
        {
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(nameof(NetworkTestSession));
            }

            if (value != null && !_owned.Contains(value))
            {
                _owned.Add(value);
            }

            return value;
        }

        public T OwnManager<T>(T manager) where T : NeoNetworkManager
        {
            Own(manager.gameObject);
            Assert.That(_manager == null || _manager == manager, Is.True, "One fixture owns one manager.");
            _manager = manager;
            Assert.That(NetworkManager.singleton, Is.SameAs(manager));
            return manager;
        }

        public IEnumerator Cleanup()
        {
            if (_cleanedUp)
            {
                yield break;
            }

            bool foreignSession = HasForeignSession();
            try
            {
                Assert.That(foreignSession, Is.False, "Cleanup refuses to stop a replacement/foreign session.");
                if (_manager != null)
                {
                    _manager.StopNetwork();
                }

                yield return NetworkTestHelper.WaitUntil(() => !NetworkServer.active && !NetworkClient.active,
                    description: "owned network shutdown");
                // Allow Mirror's deferred destruction before releasing the original templates.
                yield return null;
            }
            finally
            {
                foreignSession |= HasForeignSession();
                try
                {
                    // A timed-out graceful stop must still release this fixture's session before
                    // destroying its manager. Otherwise its remaining active flags resemble a foreign host.
                    if (!foreignSession)
                    {
                        NetworkServer.Shutdown();
                        foreignSession |= HasForeignSession();
                        if (!foreignSession)
                        {
                            NetworkClient.Shutdown();
                            NetworkClient.ClearSpawners();
                        }
                    }

                    for (int i = _owned.Count - 1; i >= 0; i--)
                    {
                        if (_owned[i] != null)
                        {
                            UnityEngine.Object.DestroyImmediate(_owned[i]);
                        }
                    }

                    foreignSession |= HasForeignSession();
                    if (!foreignSession)
                    {
                        NetworkManager.ResetStatics();
                    }
                }
                finally
                {
                    _owned.Clear();
                    _cleanedUp = true;
                    if (!foreignSession)
                    {
                        Transport.active = _previousTransport;
                        Application.runInBackground = _previousBackground;
                        Application.targetFrameRate = _previousFrameRate;
                        QualitySettings.vSyncCount = _previousVSync;
                        Time.timeScale = _previousTimeScale;
                        UnityEngine.Random.state = _previousRandomState;
                    }
                }
            }
        }

        private bool HasForeignSession()
        {
            return NetworkManager.singleton != null && NetworkManager.singleton != _manager ||
                   (NetworkServer.active || NetworkClient.active) &&
                   (_manager == null || NetworkManager.singleton != _manager);
        }
    }
}
#endif
