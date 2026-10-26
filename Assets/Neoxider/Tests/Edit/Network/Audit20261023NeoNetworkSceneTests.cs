using System.Reflection;
using Neo.Network;
using NUnit.Framework;
using UnityEngine;
#if MIRROR
using Mirror;
#endif

namespace Neo.Editor.Tests
{






    public sealed class Audit20261023NeoNetworkSceneTests
    {
        private GameObject _object;

        [SetUp]
        public void SetUp()
        {
            _object = new GameObject(nameof(Audit20261023NeoNetworkSceneTests));
#if MIRROR
            _object.AddComponent<NetworkIdentity>();
#endif
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_object);
        }

        [TestCase(null)]
        [TestCase("payload")]
        public void ContextMethod_StringModeSelectsStringOverloadEvenForNull(string value)
        {
            Audit20261023NeoNetworkMethodReceiver receiver =
                _object.AddComponent<Audit20261023NeoNetworkMethodReceiver>();
            NetworkContextActionRelay relay = _object.AddComponent<NetworkContextActionRelay>();
            relay.RootMode = NetworkContextRootMode.SourceObject;
            relay.Action = NetworkContextActionType.InvokeComponentMethod;
            relay.MethodComponentTypeName = typeof(Audit20261023NeoNetworkMethodReceiver).AssemblyQualifiedName;
            relay.MethodName = null;
            relay.Trigger();
            Assert.That(receiver.StringCalls, Is.Zero);
            Assert.That(receiver.ParameterlessCalls, Is.Zero);
            relay.MethodName = nameof(Audit20261023NeoNetworkMethodReceiver.Record);
            relay.MethodArgumentMode = NetworkContextMethodArgumentMode.String;
            relay.ActionStringValue = value;
            relay.Trigger();

            Assert.That(receiver.StringCalls, Is.EqualTo(1));
            Assert.That(receiver.LastString, Is.EqualTo(value));
            Assert.That(receiver.ParameterlessCalls, Is.Zero);

            relay.MethodArgumentMode = NetworkContextMethodArgumentMode.None;
            relay.Trigger();
            Assert.That(receiver.ParameterlessCalls, Is.EqualTo(1));
            Assert.That(receiver.StringCalls, Is.EqualTo(1));
        }

        [Test]
        public void EmptyChannelSlotsDoNotPreventDeliveryThroughConfiguredSlots()
        {
            NetworkActionRelay relay = _object.AddComponent<NetworkActionRelay>();
            NetworkActionChannel channel = new NetworkActionChannel { channelName = "valid" };
            FieldInfo channels = typeof(NetworkActionRelay).GetField("_channels",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(channels, Is.Not.Null);
            channels.SetValue(relay, new NetworkActionChannel[] { null, channel });
            int count = 0;
            float number = 0f;
            string text = null;
            channel.onTriggered.AddListener(() => count++);
            channel.onTriggeredFloat.AddListener(value => number = value);
            channel.onTriggeredString.AddListener(value => text = value);

            relay.Trigger();
            relay.TriggerFloat(123f);
            relay.TriggerString("ignored");
            Assert.That(count, Is.Zero);
            Assert.That(number, Is.Zero);
            Assert.That(text, Is.Null);

            relay.TriggerByName("valid");
            relay.TriggerFloatAt(1, 42f);
            relay.TriggerStringAt(1, "delivered");
            Assert.That(count, Is.EqualTo(1));
            Assert.That(number, Is.EqualTo(42f));
            Assert.That(text, Is.EqualTo("delivered"));
        }

        [Test]
        public void RuntimeReset_ClearsTheActualSingletonBaseForAnInheritedType()
        {
            Audit20261023NeoNetworkDerivedSingleton component =
                _object.AddComponent<Audit20261023NeoNetworkDerivedSingleton>();
            Assert.That(Audit20261023NeoNetworkSingleton.I, Is.SameAs(component));
            Assert.That(Audit20261023NeoNetworkSingleton.HasInstance, Is.True);

            // Exercise the subsystem-registration callback; no public runtime reset API is needed.
            MethodInfo reset = typeof(NetworkSingleton<>).Assembly
                .GetType("Neo.Network.NetworkSingletonRuntimeReset")
                .GetMethod("ResetStaticState", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(reset, Is.Not.Null);
            reset.Invoke(null, null);

            Assert.That(Audit20261023NeoNetworkSingleton.HasInstance, Is.False);
            Assert.That(Audit20261023NeoNetworkSingleton.TryGetInstance(out _), Is.False);
            Assert.That(Audit20261023NeoNetworkSingleton.I, Is.SameAs(component));
        }

#if !MIRROR
        [Test]
        public void Bootstrap_WithoutMirrorDoesNotReportAStartedSession()
        {
            _object.AddComponent<NeoNetworkManager>();
            NeoNetworkBootstrap bootstrap = _object.AddComponent<NeoNetworkBootstrap>();
            int started = 0;
            bootstrap.OnNetworkStartedEvent.AddListener(() => started++);
            Assert.That(bootstrap.StartNetwork(NeoStartupCommandLine.Parse(new[] { "-host" })), Is.False);
            Assert.That(bootstrap.HasStartedNetwork, Is.False);
            Assert.That(started, Is.Zero);
        }
#endif
    }
}
