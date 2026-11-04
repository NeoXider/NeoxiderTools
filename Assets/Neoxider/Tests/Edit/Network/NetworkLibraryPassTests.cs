using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Network;
using NUnit.Framework;
using UnityEngine;
#if MIRROR
using Mirror;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
#endif
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    /// <summary>Regressions for the Network library pass: relay caches, channel lookup, static resets.</summary>
    [TestFixture]
    public sealed class NetworkLibraryPassTests
    {
        private readonly List<GameObject> _objects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _objects.Count; i++)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
        }

        [Test]
        public void ContextRelay_ConfiguredMethod_FollowsNameAndSignatureChanges()
        {
            GameObject go = NewObject("Relay method cache");
            NetworkLibraryPassMethodReceiver receiver = go.AddComponent<NetworkLibraryPassMethodReceiver>();
            NetworkContextActionRelay relay = go.AddComponent<NetworkContextActionRelay>();
            relay.RootMode = NetworkContextRootMode.SourceObject;
            relay.Action = NetworkContextActionType.InvokeComponentMethod;
            relay.MethodComponentTypeName = typeof(NetworkLibraryPassMethodReceiver).AssemblyQualifiedName;
            relay.MethodName = nameof(NetworkLibraryPassMethodReceiver.Alpha);

            relay.Trigger();
            relay.Trigger();
            Assert.That(receiver.AlphaCalls, Is.EqualTo(2));

            relay.MethodName = nameof(NetworkLibraryPassMethodReceiver.Beta);
            relay.Trigger();
            Assert.That(receiver.BetaCalls, Is.EqualTo(1), "a renamed method must not reuse the cached one");
            Assert.That(receiver.AlphaCalls, Is.EqualTo(2));

            relay.MethodArgumentMode = NetworkContextMethodArgumentMode.Float;
            relay.ActionFloatValue = 4.5f;
            relay.Trigger();
            Assert.That(receiver.BetaCalls, Is.EqualTo(2), "a new argument mode selects the matching overload");
            Assert.That(receiver.LastValue, Is.EqualTo(4.5f));
        }

        [Test]
        public void ContextRelay_RepeatedApply_ReusesTheResolvedMethod()
        {
            GameObject go = NewObject("Relay method reuse");
            NetworkLibraryPassMethodReceiver receiver = go.AddComponent<NetworkLibraryPassMethodReceiver>();
            NetworkContextActionRelay relay = go.AddComponent<NetworkContextActionRelay>();
            relay.RootMode = NetworkContextRootMode.SourceObject;
            relay.Action = NetworkContextActionType.InvokeComponentMethod;
            relay.MethodComponentTypeName = typeof(NetworkLibraryPassMethodReceiver).AssemblyQualifiedName;
            relay.MethodName = nameof(NetworkLibraryPassMethodReceiver.Alpha);
            relay.Trigger();
            Assert.That(receiver.AlphaCalls, Is.EqualTo(1));

            // WHY: swap the cached entry behind the relay's back; only a relay that reuses its cache calls Beta.
            FieldInfo cached = typeof(NetworkContextActionRelay).GetField("_cachedMethod",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(cached, Is.Not.Null);
            cached.SetValue(relay, typeof(NetworkLibraryPassMethodReceiver).GetMethod(
                nameof(NetworkLibraryPassMethodReceiver.Beta), Type.EmptyTypes));
            relay.Trigger();

            Assert.That(receiver.BetaCalls, Is.EqualTo(1),
                "a repeated apply with an unchanged configuration must not reflect over the component again");
            Assert.That(receiver.AlphaCalls, Is.EqualTo(1));
        }

        [Test]
        public void ActionRelay_ChannelLookup_AndNamedPayloadTriggers()
        {
            GameObject go = NewObject("Action relay channels");
            NetworkActionRelay relay = go.AddComponent<NetworkActionRelay>();
            NetworkActionChannel alpha = new NetworkActionChannel { channelName = "alpha" };
            NetworkActionChannel beta = new NetworkActionChannel { channelName = "beta" };
            FieldInfo channels = typeof(NetworkActionRelay).GetField("_channels",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(channels, Is.Not.Null);
            channels.SetValue(relay, new[] { alpha, null, beta });

            Assert.That(relay.TryGetChannelIndex("beta", out int index), Is.True);
            Assert.That(index, Is.EqualTo(2));
            Assert.That(relay.GetChannel(index), Is.SameAs(beta));
            Assert.That(relay.GetChannel(1), Is.Null);
            Assert.That(relay.GetChannel(3), Is.Null);
            Assert.That(relay.GetChannel(-1), Is.Null);
            Assert.That(relay.TryGetChannelIndex("missing", out int missing), Is.False);
            Assert.That(missing, Is.EqualTo(-1));
            Assert.That(relay.TryGetChannelIndex(null, out _), Is.False);

            float number = 0f;
            string text = null;
            beta.onTriggeredFloat.AddListener(value => number = value);
            alpha.onTriggeredString.AddListener(value => text = value);
            relay.TriggerFloatByName("beta", 2.5f);
            relay.TriggerStringByName("alpha", "hello");
            relay.TriggerFloatByName("missing", 9f);
            relay.TriggerStringByName(null, "ignored");

            Assert.That(number, Is.EqualTo(2.5f));
            Assert.That(text, Is.EqualTo("hello"));
        }

        [Test]
        public void SingletonReset_WalksOnlyAssembliesThatCanDeclareANetworkSingleton()
        {
            Assembly network = typeof(NetworkSingleton<>).Assembly;
            List<Assembly> walked = NetworkSingletonRuntimeReset.CollectDependentAssemblies(network);

            Assert.That(walked, Does.Contain(network));
            Assert.That(walked, Does.Contain(typeof(Audit20261023NeoNetworkSingleton).Assembly),
                "an assembly declaring a NetworkSingleton is walked");
            Assert.That(walked, Does.Contain(typeof(NetworkLibraryPassTests).Assembly));
            Assert.That(walked, Has.No.Member(typeof(object).Assembly));
            Assert.That(walked, Has.No.Member(typeof(GameObject).Assembly));
            Assert.That(walked, Has.No.Member(typeof(TestAttribute).Assembly));
            Assert.That(new HashSet<Assembly>(walked).Count, Is.EqualTo(walked.Count), "each assembly once");
        }

#if MIRROR
        [Test]
        public void SceneReactivator_PlayStart_RestoresTheSafetyNet()
        {
            bool saved = NeoMirrorSceneReactivator.Enabled;
            MethodInfo init = typeof(NeoMirrorSceneReactivator).GetMethod("Init",
                BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo onSceneLoaded = typeof(NeoMirrorSceneReactivator).GetMethod("OnSceneLoaded",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(init, Is.Not.Null);
            Assert.That(onSceneLoaded, Is.Not.Null);
            UnityAction<Scene, LoadSceneMode> handler = (UnityAction<Scene, LoadSceneMode>)Delegate.CreateDelegate(
                typeof(UnityAction<Scene, LoadSceneMode>), onSceneLoaded);
            try
            {
                NeoMirrorSceneReactivator.Enabled = false;
                init.Invoke(null, null);
                Assert.That(NeoMirrorSceneReactivator.Enabled, Is.True,
                    "an opt-out from an earlier play session must not survive a disabled domain reload");
            }
            finally
            {
                SceneManager.sceneLoaded -= handler;
                NeoMirrorSceneReactivator.Enabled = saved;
            }
        }

        [Test]
        public void LobbyPresenter_LocalPlayerLookup_FollowsTheCurrentLocalPlayer()
        {
            GameObject presenterObject = new GameObject("Lobby presenter cache");
            _objects.Add(presenterObject);
            NeoLobbyPresenter presenter = presenterObject.AddComponent<NeoLobbyPresenter>();
            NeoLobbyPlayer first = NewObject("Local lobby player A").AddComponent<NeoLobbyPlayer>();
            NeoLobbyPlayer second = NewObject("Local lobby player B").AddComponent<NeoLobbyPlayer>();
            PropertyInfo localPlayer = typeof(NetworkClient).GetProperty(nameof(NetworkClient.localPlayer),
                BindingFlags.Static | BindingFlags.Public);
            MethodInfo lookup = typeof(NeoLobbyPresenter).GetMethod("LocalPlayer",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(localPlayer, Is.Not.Null);
            Assert.That(lookup, Is.Not.Null);
            NetworkIdentity saved = NetworkClient.localPlayer;
            try
            {
                localPlayer.SetValue(null, first.GetComponent<NetworkIdentity>());
                Assert.That(lookup.Invoke(presenter, null), Is.SameAs(first));
                Assert.That(lookup.Invoke(presenter, null), Is.SameAs(first));

                localPlayer.SetValue(null, second.GetComponent<NetworkIdentity>());
                Assert.That(lookup.Invoke(presenter, null), Is.SameAs(second),
                    "a new local player (rejoin, scene change) replaces the cached lookup");

                localPlayer.SetValue(null, null);
                Assert.That(lookup.Invoke(presenter, null), Is.Null);
            }
            finally
            {
                localPlayer.SetValue(null, saved);
            }
        }

        [Test]
        public void Telemetry_OverlayText_IsRemeasuredOnlyWhenItChanges()
        {
            GameObject go = new GameObject("Telemetry overlay");
            _objects.Add(go);
            NeoNetworkTelemetry telemetry = go.AddComponent<NeoNetworkTelemetry>();
            MethodInfo rebuild = typeof(NeoNetworkTelemetry).GetMethod("RebuildOverlayText",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo dirty = typeof(NeoNetworkTelemetry).GetField("_overlaySizeDirty",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(rebuild, Is.Not.Null);
            Assert.That(dirty, Is.Not.Null);

            dirty.SetValue(telemetry, false);
            rebuild.Invoke(telemetry, null);
            Assert.That(dirty.GetValue(telemetry), Is.True, "the first readout must be measured");

            dirty.SetValue(telemetry, false);
            rebuild.Invoke(telemetry, null);
            rebuild.Invoke(telemetry, null);

            Assert.That(dirty.GetValue(telemetry), Is.False,
                "an unchanged readout used to be re-measured on every rebuild, four times a second");
            Assert.That(telemetry.BuildSummary(), Does.StartWith("ping "));
        }
#endif

        private GameObject NewObject(string name)
        {
            GameObject go = new GameObject(name);
#if MIRROR
            // WHY: NetworkBehaviour.OnValidate logs an error when the host object has no NetworkIdentity.
            go.AddComponent<NetworkIdentity>();
#endif
            _objects.Add(go);
            return go;
        }
    }
}
