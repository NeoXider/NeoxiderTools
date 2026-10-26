#if MIRROR
using System;
using System.Collections;
using System.Reflection;
using Mirror;
using Mirror.RemoteCalls;
using Neo.Network;
using Neo.Reactive;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    public sealed class Audit20261023NeoNetworkSyncTarget : MonoBehaviour
    {
        public float Number = 1f;
        public int Count = 2;
        public bool Flag;
        public string Text = "initial";
        public Vector3 Position;
        public ReactivePropertyFloat ReactiveNumber = new ReactivePropertyFloat(1f);
        public ReactivePropertyInt ReactiveCount = new ReactivePropertyInt(2);
        public ReactivePropertyBool ReactiveFlag = new ReactivePropertyBool(false);
        public readonly int ReadonlyField = 2;
        public int ReadonlyProperty => Count;
        public int WriteonlyProperty { set => Count = value; }
        public int this[int index] { get => Count; set => Count = value; }
        public string WrongType = "unchanged";
        public double DoubleNumber = 1.25d;
        public ReactivePropertyInt LateReactive;
        public int GetterReads;
        public int ReactiveGetterReads;
        public int ObservedCount
        {
            get { GetterReads++; return Count; }
            set => Count = value;
        }
        public ReactivePropertyInt ObservedReactive
        {
            get { ReactiveGetterReads++; return LateReactive; }
        }
    }

    public sealed class Audit20261023NeoNetworkBudgetProbe : NeoNetworkComponent
    {
        public int Accepted;
        public bool Accept(NetworkConnectionToClient peer)
        {
            if (RateLimitCheck(peer)) return false;
            Accepted++;
            return true;
        }
    }

    public sealed class Audit20261023NeoNetworkCommandTests
    {
        private GameObject _managerObject;
        private GameObject _subject;
        private TestNetworkManager _manager;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            NetworkClient.Shutdown();
            NetworkServer.Shutdown();
            _manager = NetworkTestHelper.CreateTestNetworkManager(
                nameof(Audit20261023NeoNetworkCommandTests), out _managerObject);
            _manager.dontDestroyOnLoad = false;
            _manager.autoCreatePlayer = false;
            _manager.StartAsServer();
            Assert.That(NetworkServer.active, Is.True);
            Assert.That(NetworkServer.activeHost, Is.False);
            _subject = new GameObject("Audit network command target");
            _subject.SetActive(false);
            _subject.AddComponent<NetworkIdentity>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_manager != null)
            {
                _manager.StopNetwork();
            }

            yield return null;
            if (_subject != null)
            {
                Object.DestroyImmediate(_subject);
            }

            if (_managerObject != null)
            {
                Object.DestroyImmediate(_managerObject);
            }

            NetworkClient.Shutdown();
            NetworkServer.Shutdown();
            NetworkManager.ResetStatics();
        }

        [TestCase("Void")]
        [TestCase("Float")]
        [TestCase("String")]
        public void ActionRelay_OnePeersRateLimitDoesNotConsumeAnotherPeersFirstAction(string kind)
        {
            NetworkActionRelay relay = _subject.AddComponent<NetworkActionRelay>();
            relay.isNetworked = true;
            NetworkActionChannel channel = new NetworkActionChannel { scope = NetworkActionScope.ServerOnly };
            SetField(relay, "_channels", new[] { channel });
            int received = 0;
            channel.onTriggered.AddListener(() => received++);
            channel.onTriggeredFloat.AddListener(value => received += value == 42f ? 1 : 100);
            channel.onTriggeredString.AddListener(value => received += value == "accepted" ? 1 : 100);
            _subject.SetActive(true);
            NetworkServer.Spawn(_subject);
            NetworkConnectionToClient first = new NetworkConnectionToClient(601);
            NetworkConnectionToClient second = new NetworkConnectionToClient(602);
            NetworkWriter payload = new NetworkWriter();
            // Follow the registered wire serializer; this Mirror version weaves int as varint.
            payload.Write<int>(0);
            string parameters = "System.Int32";
            if (kind == "Float")
            {
                payload.WriteFloat(42f);
                parameters += ",System.Single";
            }
            else if (kind == "String")
            {
                payload.WriteString("accepted");
                parameters += ",System.String";
            }

            parameters += ",Mirror.NetworkConnectionToClient";
            Deliver(relay, "CmdTrigger" + kind, parameters, payload, first);
            Deliver(relay, "CmdTrigger" + kind, parameters, payload, first);
            Assert.That(received, Is.EqualTo(1), "the same peer's immediate repeat must be dropped");
            Deliver(relay, "CmdTrigger" + kind, parameters, payload, second);
            Assert.That(received, Is.EqualTo(2), "another peer has an independent budget");
        }

        [TestCase(SyncValueType.Float)]
        [TestCase(SyncValueType.Int)]
        [TestCase(SyncValueType.Bool)]
        [TestCase(SyncValueType.String)]
        [TestCase(SyncValueType.Vector3)]
        public void PropertySync_OwnerCommandUpdatesTheDedicatedServersTargetAndEvent(SyncValueType valueType)
        {
            Audit20261023NeoNetworkSyncTarget target = _subject.AddComponent<Audit20261023NeoNetworkSyncTarget>();
            NetworkPropertySync sync = _subject.AddComponent<NetworkPropertySync>();
            sync.isNetworked = true;
            SetField(sync, "_targetComponent", target);
            SetField(sync, "_fieldName", FieldName(valueType));
            SetField(sync, "_valueType", valueType);
            SetField(sync, "_direction", SyncPropertyDirection.OwnerToServer);
            int notifications = 0;
            sync.onValueChanged.AddListener(() => notifications++);
            _subject.SetActive(true);
            NetworkServer.Spawn(_subject);

            NetworkWriter payload = MakePayload(valueType);
            Deliver(sync, "CmdSync" + valueType, ParameterType(valueType), payload, new NetworkConnectionToClient(603));

            Assert.That(ReadTarget(target, valueType), Is.EqualTo(ExpectedValue(valueType)));
            Assert.That(notifications, Is.EqualTo(1));
        }

        [TestCase(ReactiveSyncValueType.Float)]
        [TestCase(ReactiveSyncValueType.Int)]
        [TestCase(ReactiveSyncValueType.Bool)]
        public void ReactiveSync_OwnerCommandUpdatesDedicatedServerBindings(ReactiveSyncValueType valueType)
        {
            Audit20261023NeoNetworkSyncTarget target = _subject.AddComponent<Audit20261023NeoNetworkSyncTarget>();
            NetworkReactiveSync sync = _subject.AddComponent<NetworkReactiveSync>();
            sync.isNetworked = true;
            SyncValueType primitive = (SyncValueType)valueType;
            SetField(sync, "_targetComponent", target);
            SetField(sync, "_fieldName", "Reactive" + FieldName(primitive));
            SetField(sync, "_valueType", valueType);
            SetField(sync, "_direction", SyncPropertyDirection.OwnerToServer);
            int notifications = 0;
            target.ReactiveNumber.AddListener(_ => notifications++);
            target.ReactiveCount.AddListener(_ => notifications++);
            target.ReactiveFlag.AddListener(_ => notifications++);
            _subject.SetActive(true);
            NetworkServer.Spawn(_subject);

            Deliver(sync, "CmdSync" + valueType, ParameterType(primitive), MakePayload(primitive),
                new NetworkConnectionToClient(604));

            object value = valueType switch
            {
                ReactiveSyncValueType.Float => target.ReactiveNumber.CurrentValue,
                ReactiveSyncValueType.Int => target.ReactiveCount.CurrentValue,
                _ => target.ReactiveFlag.CurrentValue
            };
            Assert.That(value, Is.EqualTo(ExpectedValue(primitive)));
            Assert.That(notifications, Is.EqualTo(1));
        }

        [TestCase(false, SyncPropertyDirection.OwnerToServer, SyncValueType.Int)]
        [TestCase(true, SyncPropertyDirection.ServerToClients, SyncValueType.Int)]
        [TestCase(true, SyncPropertyDirection.OwnerToServer, SyncValueType.Float)]
        public void PropertySync_CommandCannotOverrideItsConfiguredWritePolicy(bool networked,
            SyncPropertyDirection direction, SyncValueType configuredType)
        {
            Audit20261023NeoNetworkSyncTarget target = _subject.AddComponent<Audit20261023NeoNetworkSyncTarget>();
            NetworkPropertySync sync = _subject.AddComponent<NetworkPropertySync>();
            sync.isNetworked = networked;
            SetField(sync, "_targetComponent", target);
            SetField(sync, "_fieldName", nameof(target.Count));
            SetField(sync, "_valueType", configuredType);
            SetField(sync, "_direction", direction);
            int notifications = 0;
            sync.onValueChanged.AddListener(() => notifications++);
            _subject.SetActive(true);
            NetworkServer.Spawn(_subject);
            byte[] before = CaptureInitialState(sync);
            Deliver(sync, "CmdSyncInt", "System.Int32", MakePayload(SyncValueType.Int),
                new NetworkConnectionToClient(605));

            Assert.That(target.Count, Is.EqualTo(2));
            Assert.That(notifications, Is.Zero);
            // Observe the outbound snapshot as well: a rejected write must not be replicated to other peers.
            Assert.That(CaptureInitialState(sync), Is.EqualTo(before));
        }

        [TestCase(false, SyncPropertyDirection.OwnerToServer, ReactiveSyncValueType.Int)]
        [TestCase(true, SyncPropertyDirection.ServerToClients, ReactiveSyncValueType.Int)]
        [TestCase(true, SyncPropertyDirection.OwnerToServer, ReactiveSyncValueType.Float)]
        public void ReactiveSync_CommandCannotOverrideItsConfiguredWritePolicy(bool networked,
            SyncPropertyDirection direction, ReactiveSyncValueType configuredType)
        {
            Audit20261023NeoNetworkSyncTarget target = _subject.AddComponent<Audit20261023NeoNetworkSyncTarget>();
            NetworkReactiveSync sync = _subject.AddComponent<NetworkReactiveSync>();
            sync.isNetworked = networked;
            SetField(sync, "_targetComponent", target);
            SetField(sync, "_fieldName", nameof(target.ReactiveCount));
            SetField(sync, "_valueType", configuredType);
            SetField(sync, "_direction", direction);
            int notifications = 0;
            target.ReactiveCount.AddListener(_ => notifications++);
            _subject.SetActive(true);
            NetworkServer.Spawn(_subject);
            byte[] before = CaptureInitialState(sync);
            Deliver(sync, "CmdSyncInt", "System.Int32", MakePayload(SyncValueType.Int),
                new NetworkConnectionToClient(606));

            Assert.That(target.ReactiveCount.CurrentValue, Is.EqualTo(2));
            Assert.That(notifications, Is.Zero);
            Assert.That(CaptureInitialState(sync), Is.EqualTo(before));
        }

        [TestCase("Item")]
        [TestCase("ReadonlyField")]
        [TestCase("ReadonlyProperty")]
        [TestCase("WriteonlyProperty")]
        [TestCase("WrongType")]
        [TestCase("Number")]
        [TestCase("DoubleNumber")]
        public void InvalidPropertyBinding_RejectsIngressWithoutMutationOrNotification(string member)
        {
            Audit20261023NeoNetworkSyncTarget target = _subject.AddComponent<Audit20261023NeoNetworkSyncTarget>();
            NetworkPropertySync sync = _subject.AddComponent<NetworkPropertySync>();
            sync.isNetworked = true;
            SetField(sync, "_targetComponent", target);
            SetField(sync, "_fieldName", member);
            SetField(sync, "_valueType", SyncValueType.Int);
            SetField(sync, "_direction", SyncPropertyDirection.OwnerToServer);
            int notifications = 0;
            sync.onValueChanged.AddListener(() => notifications++);
            _subject.SetActive(true);
            NetworkServer.Spawn(_subject);
            byte[] before = CaptureInitialState(sync);
            Deliver(sync, "CmdSyncInt", "System.Int32", MakePayload(SyncValueType.Int), new NetworkConnectionToClient(610));
            Deliver(sync, "CmdSyncInt", "System.Int32", MakePayload(SyncValueType.Int), new NetworkConnectionToClient(611));
            Assert.That(target.Count, Is.EqualTo(2));
            Assert.That(target.WrongType, Is.EqualTo("unchanged"));
            Assert.That(target.Number, Is.EqualTo(1f));
            Assert.That(target.DoubleNumber, Is.EqualTo(1.25d));
            Assert.That(notifications, Is.Zero);
            Assert.That(CaptureInitialState(sync), Is.EqualTo(before));
        }

        [UnityTest]
        public IEnumerator LateReactiveInstance_NextAcceptedUpdateBindsAndNotifiesOnce()
        {
            Audit20261023NeoNetworkSyncTarget target = _subject.AddComponent<Audit20261023NeoNetworkSyncTarget>();
            NetworkReactiveSync sync = _subject.AddComponent<NetworkReactiveSync>();
            sync.isNetworked = true;
            SetField(sync, "_targetComponent", target);
            SetField(sync, "_fieldName", nameof(target.LateReactive));
            SetField(sync, "_valueType", ReactiveSyncValueType.Int);
            SetField(sync, "_direction", SyncPropertyDirection.OwnerToServer);
            _subject.SetActive(true);
            NetworkServer.Spawn(_subject);
            Deliver(sync, "CmdSyncInt", "System.Int32", MakePayload(SyncValueType.Int), new NetworkConnectionToClient(612));
            target.LateReactive = new ReactivePropertyInt(3);
            int notifications = 0;
            target.LateReactive.AddListener(_ => notifications++);
            NetworkWriter next = new NetworkWriter();
            next.Write<int>(23);
            yield return new WaitForSecondsRealtime(0.06f);
            Deliver(sync, "CmdSyncInt", "System.Int32", next, new NetworkConnectionToClient(612));
            Assert.That(target.LateReactive.CurrentValue, Is.EqualTo(23));
            Assert.That(notifications, Is.EqualTo(1));
        }

        [Test]
        public void UnchangedProperty_PollsOnlyAtTheConfiguredReadCadence()
        {
            Audit20261023NeoNetworkSyncTarget target = _subject.AddComponent<Audit20261023NeoNetworkSyncTarget>();
            NetworkPropertySync sync = _subject.AddComponent<NetworkPropertySync>();
            sync.isNetworked = true;
            SetField(sync, "_targetComponent", target);
            SetField(sync, "_fieldName", nameof(target.ObservedCount));
            SetField(sync, "_valueType", SyncValueType.Int);
            _subject.SetActive(true);
            NetworkServer.Spawn(_subject);
            MakePollDue(sync);
            InvokeUpdate(sync);
            MakePollDue(sync);
            InvokeUpdate(sync);
            InvokeUpdate(sync);
            Assert.That(target.GetterReads, Is.EqualTo(2), "an unchanged eligible poll must close its own read window");
        }

        [Test]
        public void UnreadyReactiveProperty_RetriesAtTheConfiguredReadCadence()
        {
            Audit20261023NeoNetworkSyncTarget target = _subject.AddComponent<Audit20261023NeoNetworkSyncTarget>();
            NetworkReactiveSync sync = _subject.AddComponent<NetworkReactiveSync>();
            sync.isNetworked = true;
            SetField(sync, "_targetComponent", target);
            SetField(sync, "_fieldName", nameof(target.ObservedReactive));
            SetField(sync, "_valueType", ReactiveSyncValueType.Int);
            _subject.SetActive(true);
            NetworkServer.Spawn(_subject);
            MakePollDue(sync);
            InvokeUpdate(sync);
            InvokeUpdate(sync);
            Assert.That(target.ReactiveGetterReads, Is.EqualTo(1));
            MakePollDue(sync);
            InvokeUpdate(sync);
            Assert.That(target.ReactiveGetterReads, Is.EqualTo(2));
        }

        [TestCase("Void")]
        [TestCase("Float")]
        [TestCase("String")]
        public void RelayOptOut_RejectsQueuedIngressBeforeConsumingThePeerBudget(string kind)
        {
            NetworkActionRelay relay = _subject.AddComponent<NetworkActionRelay>();
            NetworkActionChannel channel = new NetworkActionChannel { scope = NetworkActionScope.ServerOnly };
            SetField(relay, "_channels", new[] { channel });
            int count = 0;
            channel.onTriggered.AddListener(() => count++);
            channel.onTriggeredFloat.AddListener(_ => count++);
            channel.onTriggeredString.AddListener(_ => count++);
            _subject.SetActive(true);
            NetworkServer.Spawn(_subject);
            NetworkWriter payload = new NetworkWriter();
            payload.Write<int>(0);
            string parameters = "System.Int32";
            if (kind == "Float") { payload.WriteFloat(42f); parameters += ",System.Single"; }
            if (kind == "String") { payload.WriteString("accepted"); parameters += ",System.String"; }
            parameters += ",Mirror.NetworkConnectionToClient";
            NetworkConnectionToClient peer = new NetworkConnectionToClient(613);
            relay.isNetworked = false;
            Deliver(relay, "CmdTrigger" + kind, parameters, payload, peer);
            Assert.That(count, Is.Zero);
            relay.isNetworked = true;
            Deliver(relay, "CmdTrigger" + kind, parameters, payload, peer);
            Assert.That(count, Is.EqualTo(1), "the opted-out request must not spend the enabled request's budget");
        }

        [TestCase(-1)]
        [TestCase(3)]
        [TestCase(int.MaxValue)]
        public void UnknownPolicy_RejectsEvenTrustedNullAndRemoteOwner(int policy)
        {
            NetworkConnectionToClient owner = new NetworkConnectionToClient(614);
            NetworkConnectionToClient foreign = new NetworkConnectionToClient(615);
            _subject.SetActive(true);
            NetworkServer.Spawn(_subject, owner);
            Assert.That(NeoNetworkState.IsAuthorized(_subject, owner, (NetworkAuthorityMode)policy), Is.False);
            Assert.That(NeoNetworkState.IsAuthorized(_subject, foreign, (NetworkAuthorityMode)policy), Is.False);
            Assert.That(NeoNetworkState.IsAuthorized(null, null, (NetworkAuthorityMode)policy), Is.False);
            Assert.That(NeoNetworkState.IsAuthorized(_subject, owner, NetworkAuthorityMode.OwnerOnly), Is.True);
            Assert.That(NeoNetworkState.IsAuthorized(_subject, foreign, NetworkAuthorityMode.OwnerOnly), Is.False);
            Assert.That(NeoNetworkState.IsAuthorized(null, null, NetworkAuthorityMode.ServerOnly), Is.True);
            Assert.That(NeoNetworkState.IsAuthorized(null, foreign, NetworkAuthorityMode.None), Is.True);
            Assert.That(NeoNetworkState.IsAuthorized(null, foreign, NetworkAuthorityMode.ServerOnly), Is.False);
            Assert.That(NeoNetworkState.IsAuthorized(null, owner, NetworkAuthorityMode.OwnerOnly), Is.False);
        }

        [TestCase("Item")]
        [TestCase("WriteonlyProperty")]
        [TestCase("WrongType")]
        public void InvalidReactiveMember_RejectsWithoutChangingOutboundState(string member)
        {
            Audit20261023NeoNetworkSyncTarget target = _subject.AddComponent<Audit20261023NeoNetworkSyncTarget>();
            NetworkReactiveSync sync = _subject.AddComponent<NetworkReactiveSync>();
            sync.isNetworked = true;
            SetField(sync, "_targetComponent", target);
            SetField(sync, "_fieldName", member);
            SetField(sync, "_valueType", ReactiveSyncValueType.Int);
            SetField(sync, "_direction", SyncPropertyDirection.OwnerToServer);
            _subject.SetActive(true);
            NetworkServer.Spawn(_subject);
            byte[] before = CaptureInitialState(sync);
            Deliver(sync, "CmdSyncInt", "System.Int32", MakePayload(SyncValueType.Int), new NetworkConnectionToClient(616));
            Assert.That(target.Count, Is.EqualTo(2));
            Assert.That(target.ReactiveCount.CurrentValue, Is.EqualTo(2));
            Assert.That(CaptureInitialState(sync), Is.EqualTo(before));
        }

        [UnityTest]
        public IEnumerator PausedBudget_ImmediateRepeatRejectsAndRealtimeWindowReopens()
        {
            Audit20261023NeoNetworkBudgetProbe probe = _subject.AddComponent<Audit20261023NeoNetworkBudgetProbe>();
            _subject.SetActive(true);
            float previous = Time.timeScale;
            try
            {
                Time.timeScale = 0f;
                NetworkConnectionToClient peer = new NetworkConnectionToClient(617);
                Assert.That(probe.Accept(peer), Is.True);
                Assert.That(probe.Accept(peer), Is.False);
                yield return new WaitForSecondsRealtime(0.06f);
                Assert.That(probe.Accept(peer), Is.True);
                Assert.That(probe.Accept(null), Is.True);
                Assert.That(probe.Accept(null), Is.False);
                yield return new WaitForSecondsRealtime(0.06f);
                Assert.That(probe.Accept(null), Is.True);
                Assert.That(probe.Accepted, Is.EqualTo(4));
            }
            finally
            {
                Time.timeScale = previous;
            }
        }

        [UnityTest]
        public IEnumerator HostAllowance_KnownPoliciesPreservedAndUnknownPoliciesRejected()
        {
            _manager.StopNetwork();
            yield return null;
            _manager.StartAsHost();
            yield return null;
            LocalConnectionToClient host = NetworkServer.localConnection;
            Assert.That(host, Is.Not.Null);
            Assert.That(NeoNetworkState.IsAuthorized(null, host, NetworkAuthorityMode.ServerOnly), Is.True);
            Assert.That(NeoNetworkState.IsAuthorized(null, host, NetworkAuthorityMode.OwnerOnly), Is.True);
            Assert.That(NeoNetworkState.IsAuthorized(null, host, NetworkAuthorityMode.None), Is.True);
            Assert.That(NeoNetworkState.IsAuthorized(null, host, (NetworkAuthorityMode)(-1)), Is.False);
            Assert.That(NeoNetworkState.IsAuthorized(null, host, (NetworkAuthorityMode)3), Is.False);
            Assert.That(NeoNetworkState.IsAuthorized(null, host, (NetworkAuthorityMode)int.MaxValue), Is.False);
        }

        private static void MakePollDue(object target)
        {
            SetField(target, "_lastSyncTime", Time.unscaledTimeAsDouble - 1d);
        }

        private static void InvokeUpdate(object target)
        {
            target.GetType().GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
        }

        private static byte[] CaptureInitialState(NetworkBehaviour component)
        {
            NetworkWriter writer = new NetworkWriter();
            component.OnSerialize(writer, true);
            return writer.ToArray();
        }

        private static void Deliver(NetworkBehaviour component, string method, string parameters,
            NetworkWriter payload, NetworkConnectionToClient sender)
        {
            // Use Mirror's public profiler API to dispatch the actual woven receive handler.
            // This bypasses admission; reflection helpers configure settings and schedule polling callbacks.
            string signature = $"System.Void {component.GetType().FullName}::{method}({parameters})";
            ushort hash = signature.GetStableHashCode16();
            RemoteCallDelegate handler = RemoteProcedureCalls.GetDelegate(hash);
            Assert.That(handler, Is.Not.Null, signature);
            NetworkReader reader = new NetworkReader(payload.ToArraySegment());
            handler(component, reader, sender);
            Assert.That(reader.Remaining, Is.Zero);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private static string FieldName(SyncValueType type)
        {
            return type switch
            {
                SyncValueType.Float => nameof(Audit20261023NeoNetworkSyncTarget.Number),
                SyncValueType.Int => nameof(Audit20261023NeoNetworkSyncTarget.Count),
                SyncValueType.Bool => nameof(Audit20261023NeoNetworkSyncTarget.Flag),
                SyncValueType.String => nameof(Audit20261023NeoNetworkSyncTarget.Text),
                _ => nameof(Audit20261023NeoNetworkSyncTarget.Position)
            };
        }

        private static string ParameterType(SyncValueType type)
        {
            return type switch
            {
                SyncValueType.Float => "System.Single",
                SyncValueType.Int => "System.Int32",
                SyncValueType.Bool => "System.Boolean",
                SyncValueType.String => "System.String",
                _ => "UnityEngine.Vector3"
            };
        }

        private static object ExpectedValue(SyncValueType type)
        {
            return type switch
            {
                SyncValueType.Float => 42.5f,
                SyncValueType.Int => 17,
                SyncValueType.Bool => true,
                SyncValueType.String => "accepted",
                _ => new Vector3(1f, 2f, 3f)
            };
        }

        private static object ReadTarget(Audit20261023NeoNetworkSyncTarget target, SyncValueType type)
        {
            return type switch
            {
                SyncValueType.Float => target.Number,
                SyncValueType.Int => target.Count,
                SyncValueType.Bool => target.Flag,
                SyncValueType.String => target.Text,
                _ => target.Position
            };
        }

        private static NetworkWriter MakePayload(SyncValueType type)
        {
            NetworkWriter writer = new NetworkWriter();
            switch (type)
            {
                case SyncValueType.Float: writer.WriteFloat(42.5f); break;
                case SyncValueType.Int: writer.Write<int>(17); break;
                case SyncValueType.Bool: writer.WriteBool(true); break;
                case SyncValueType.String: writer.WriteString("accepted"); break;
                case SyncValueType.Vector3: writer.WriteVector3(new Vector3(1f, 2f, 3f)); break;
            }

            return writer;
        }
    }
}
#endif
