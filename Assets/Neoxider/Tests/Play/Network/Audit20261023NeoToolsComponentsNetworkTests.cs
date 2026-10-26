#if MIRROR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Mirror;
using Mirror.RemoteCalls;
using Neo.Save;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    public class Audit20261023NeoToolsComponentsNetworkTests
    {
        private GameObject _managerObject;
        private GameObject _counterObject;
        private GameObject _receiverObject;
        private GameObject _rpcReceiverObject;
        private string _saveKey;
        private TestNetworkManager _manager;
        private Transport _previousTransport;
        private Action<NetworkDiagnostics.MessageInfo> _rpcObserver;
        private readonly List<float> _receivedRpcValues = new List<float>();
        private int _counterRpcBytes;
        private bool _ownsTransport;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _ownsTransport = false;
            _receivedRpcValues.Clear();
            _counterRpcBytes = 0;
            Assert.That(NetworkServer.active || NetworkClient.active, Is.False,
                "This fixture requires an idle Mirror session.");
            Assert.That(NetworkManager.singleton == null, Is.True,
                "This fixture cannot take over a live foreign manager.");
            Assert.That(Writer<CounterModifyOperation>.write, Is.Not.Null,
                "Counter's enum must use Mirror's registered woven serializer.");
            Assert.That(Writer<float>.write, Is.Not.Null);
            Assert.That(Reader<float>.read, Is.Not.Null);
            _previousTransport = Transport.active;
            _ownsTransport = true;
            _manager = NetworkTestHelper.CreateTestNetworkManager("AuditComponentsHost", out _managerObject);
            _manager.autoCreatePlayer = false;
            _manager.StartHost();
            yield return NetworkTestHelper.WaitForHost();
            if (!NetworkClient.ready)
            {
                NetworkClient.Ready();
            }

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_rpcObserver != null)
            {
                NetworkDiagnostics.OutMessageEvent -= _rpcObserver;
                _rpcObserver = null;
            }

            if (_manager != null)
            {
                _manager.StopHost();
            }

            yield return null;
            if (_counterObject != null)
            {
                Object.DestroyImmediate(_counterObject);
            }

            if (_managerObject != null)
            {
                Object.DestroyImmediate(_managerObject);
            }

            if (_receiverObject != null)
            {
                Object.DestroyImmediate(_receiverObject);
            }

            if (_rpcReceiverObject != null)
            {
                Object.DestroyImmediate(_rpcReceiverObject);
            }

            if (_saveKey != null)
            {
                SaveProvider.DeleteKey(_saveKey);
            }

            if (_ownsTransport)
            {
                Transport.active = _previousTransport;
                _ownsTransport = false;
            }
        }

        [UnityTest]
        public IEnumerator Counter_HostSet_InvokesValueAndRepeatEventsOnce()
        {
            _counterObject = new GameObject("AuditComponentsCounter");
            _counterObject.SetActive(false);
            NetworkIdentity identity = _counterObject.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(identity, 10230605);
            Counter counter = _counterObject.AddComponent<Counter>();
            counter.isNetworked = true;
            FieldInfo repeatSetting = typeof(Counter).GetField("_invokeRepeatEventOnValueChanged",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(repeatSetting, Is.Not.Null);
            repeatSetting.SetValue(counter, true);
            _counterObject.SetActive(true);
            Assert.That(counter.netIdentity, Is.SameAs(identity), "The complete graph must exist before native Awake.");
            NetworkServer.Spawn(_counterObject);
            yield return null;
            int valueEvents = 0;
            int repeats = 0;
            counter.OnValueChangedInt.AddListener(value => valueEvents++);
            counter.OnRepeatByCounterValue.AddListener(() => repeats++);

            counter.Set(3);
            yield return null;
            yield return null;

            Assert.That(counter.ValueInt, Is.EqualTo(3));
            Assert.That(valueEvents, Is.EqualTo(1), "The host must not apply its own RPC a second time.");
            Assert.That(repeats, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator Counter_InitialSnapshot_PreservesConfiguredValue()
        {
            Counter counter = CreateCounter(7f);
            yield return null;

            Counter receiver = ReadInitialSnapshot(counter);

            Assert.That(counter.ValueFloat, Is.EqualTo(7f));
            Assert.That(receiver.ValueFloat, Is.EqualTo(7f));
        }

        [UnityTest]
        public IEnumerator Counter_LoadFromSave_UpdatesTheInitialSnapshotForNewClients()
        {
            Counter counter = CreateCounter(3f);
            _saveKey = "Audit20261023NeoToolsComponents.Network." + System.Guid.NewGuid().ToString("N");
            typeof(Counter).GetField("_saveEnabled", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(counter, true);
            typeof(Counter).GetField("_saveKey", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(counter, _saveKey);
            SaveProvider.SetFloat(_saveKey, 9f);

            counter.LoadFromSave();
            yield return null;
            Counter receiver = ReadInitialSnapshot(counter);

            Assert.That(counter.ValueFloat, Is.EqualTo(9f));
            Assert.That(receiver.ValueFloat, Is.EqualTo(9f));
        }

        [UnityTest]
        public IEnumerator Counter_ReactiveReentry_PublishesOnlyLatestRpcAndSnapshot()
        {
            return CheckReentrantPublication(false, false);
        }

        [UnityTest]
        public IEnumerator Counter_TypedReentry_PublishesOnlyLatestRpcAndSnapshot()
        {
            return CheckReentrantPublication(false, true);
        }

        [UnityTest]
        public IEnumerator Counter_SetCommandReactiveReentry_PublishesOnlyLatestRpcAndSnapshot()
        {
            return CheckReentrantPublication(true, false);
        }

        [UnityTest]
        public IEnumerator Counter_SetCommandTypedReentry_PublishesOnlyLatestRpcAndSnapshot()
        {
            return CheckReentrantPublication(true, true);
        }

        [UnityTest]
        public IEnumerator Counter_ServerOperationHandlers_AccumulateSendersInServerOrder()
        {
            Counter counter = CreateCounter(10f);
            yield return null;
            Counter rpcReceiver = ObserveValueRpcs(counter);

            // Actual woven handlers with distinct sender identities, in process. This is not a remote session.
            DeliverOperation(counter, CounterModifyOperation.Add, 1f, new NetworkConnectionToClient(1023601));
            DeliverOperation(counter, CounterModifyOperation.Add, 1f, new NetworkConnectionToClient(1023602));
            Assert.That(counter.ValueInt, Is.EqualTo(12));
            Assert.That(rpcReceiver.ValueInt, Is.EqualTo(12));
            DeliverOperation(counter, CounterModifyOperation.Multiply, 2f, new NetworkConnectionToClient(1023603));
            DeliverOperation(counter, CounterModifyOperation.Add, 1f, new NetworkConnectionToClient(1023604));

            Counter lateReceiver = ReadInitialSnapshot(counter);
            Assert.That(counter.ValueInt, Is.EqualTo(25));
            Assert.That(rpcReceiver.ValueInt, Is.EqualTo(25));
            Assert.That(lateReceiver.ValueInt, Is.EqualTo(25));
            Assert.That(_receivedRpcValues, Is.EqualTo(new[] { 11f, 12f, 24f, 25f }));
        }

        [UnityTest]
        public IEnumerator Counter_ServerOperationHandlers_RejectLocalOnlyAndUnknownBeforeBudget()
        {
            Counter counter = CreateCounter(10f);
            yield return null;
            Counter rpcReceiver = ObserveValueRpcs(counter);
            NetworkConnectionToClient sender = new NetworkConnectionToClient(1023605);

            DeliverOperation(counter, (CounterModifyOperation)999, 100f, sender);
            counter.isNetworked = false;
            DeliverOperation(counter, CounterModifyOperation.Add, 100f, sender);
            counter.isNetworked = true;
            DeliverOperation(counter, CounterModifyOperation.Add, 1f, sender);

            Assert.That(counter.ValueInt, Is.EqualTo(11));
            Assert.That(rpcReceiver.ValueInt, Is.EqualTo(11));
            Assert.That(_receivedRpcValues, Is.EqualTo(new[] { 11f }));
            Assert.That(ReadInitialSnapshot(counter).ValueInt, Is.EqualTo(11));
        }

        [TestCase(CounterValueMode.Float, 0)]
        [TestCase(CounterValueMode.Float, 1)]
        [TestCase(CounterValueMode.Float, 2)]
        [TestCase(CounterValueMode.Int, 0)]
        [TestCase(CounterValueMode.Int, 1)]
        [TestCase(CounterValueMode.Int, 2)]
        public void Counter_SetReceive_RejectsNonfiniteBeforeMutationAndBudget(CounterValueMode mode, int input)
        {
            CheckRejectedReceive(mode, 10f, NonfiniteValue(input), null);
        }

        [TestCase(CounterValueMode.Float, CounterModifyOperation.Add, 0)]
        [TestCase(CounterValueMode.Float, CounterModifyOperation.Add, 1)]
        [TestCase(CounterValueMode.Float, CounterModifyOperation.Add, 2)]
        [TestCase(CounterValueMode.Float, CounterModifyOperation.Multiply, 0)]
        [TestCase(CounterValueMode.Float, CounterModifyOperation.Multiply, 1)]
        [TestCase(CounterValueMode.Float, CounterModifyOperation.Multiply, 2)]
        [TestCase(CounterValueMode.Int, CounterModifyOperation.Add, 0)]
        [TestCase(CounterValueMode.Int, CounterModifyOperation.Add, 1)]
        [TestCase(CounterValueMode.Int, CounterModifyOperation.Add, 2)]
        [TestCase(CounterValueMode.Int, CounterModifyOperation.Multiply, 0)]
        [TestCase(CounterValueMode.Int, CounterModifyOperation.Multiply, 1)]
        [TestCase(CounterValueMode.Int, CounterModifyOperation.Multiply, 2)]
        public void Counter_OperationReceive_RejectsNonfiniteOperandBeforeMutationAndBudget(
            CounterValueMode mode, CounterModifyOperation operation, int input)
        {
            CheckRejectedReceive(mode, 10f, NonfiniteValue(input), operation);
        }

        [TestCase(CounterValueMode.Float, CounterModifyOperation.Add, 1)]
        [TestCase(CounterValueMode.Float, CounterModifyOperation.Add, -1)]
        [TestCase(CounterValueMode.Float, CounterModifyOperation.Multiply, 1)]
        [TestCase(CounterValueMode.Float, CounterModifyOperation.Multiply, -1)]
        [TestCase(CounterValueMode.Int, CounterModifyOperation.Add, 1)]
        [TestCase(CounterValueMode.Int, CounterModifyOperation.Add, -1)]
        [TestCase(CounterValueMode.Int, CounterModifyOperation.Multiply, 1)]
        [TestCase(CounterValueMode.Int, CounterModifyOperation.Multiply, -1)]
        public void Counter_OperationReceive_RejectsFiniteRawOverflowBeforeIntRounding(
            CounterValueMode mode, CounterModifyOperation operation, int sign)
        {
            float initialValue = operation == CounterModifyOperation.Add ? sign * float.MaxValue : sign * 2f;
            float operand = operation == CounterModifyOperation.Add ? initialValue : float.MaxValue;
            CheckRejectedReceive(mode, initialValue, operand, operation);
        }

        [TestCase(CounterValueMode.Float, CounterModifyOperation.Add, 0)]
        [TestCase(CounterValueMode.Float, CounterModifyOperation.Add, 1)]
        [TestCase(CounterValueMode.Float, CounterModifyOperation.Add, 2)]
        [TestCase(CounterValueMode.Float, CounterModifyOperation.Multiply, 0)]
        [TestCase(CounterValueMode.Float, CounterModifyOperation.Multiply, 1)]
        [TestCase(CounterValueMode.Float, CounterModifyOperation.Multiply, 2)]
        [TestCase(CounterValueMode.Int, CounterModifyOperation.Add, 0)]
        [TestCase(CounterValueMode.Int, CounterModifyOperation.Add, 1)]
        [TestCase(CounterValueMode.Int, CounterModifyOperation.Add, 2)]
        [TestCase(CounterValueMode.Int, CounterModifyOperation.Multiply, 0)]
        [TestCase(CounterValueMode.Int, CounterModifyOperation.Multiply, 1)]
        [TestCase(CounterValueMode.Int, CounterModifyOperation.Multiply, 2)]
        public void Counter_OperationReceive_CorruptCurrentRejectsRelativeThenAcceptsFiniteRepair(
            CounterValueMode mode, CounterModifyOperation operation, int current)
        {
            float operand = operation == CounterModifyOperation.Multiply ? 0f : 1f;
            CheckRejectedReceive(mode, NonfiniteValue(current), operand, operation);
        }

        [TestCase(CounterValueMode.Float, CounterModifyOperation.Add, 0.75f, 2.75f, 3)]
        [TestCase(CounterValueMode.Int, CounterModifyOperation.Add, 0.75f, 3f, 3)]
        [TestCase(CounterValueMode.Float, CounterModifyOperation.Multiply, 1.75f, 3.5f, 4)]
        [TestCase(CounterValueMode.Int, CounterModifyOperation.Multiply, 1.75f, 4f, 4)]
        public void Counter_OperationReceive_PreservesFiniteArithmeticAndIntRounding(
            CounterValueMode mode, CounterModifyOperation operation, float operand, float expected, int repeats)
        {
            using (CounterAdmissionProbe probe = new CounterAdmissionProbe(this, mode, 2f))
            {
                DeliverOperation(probe.Counter, operation, operand, new NetworkConnectionToClient(1023610));

                AssertAcceptedSequence(probe, new[] { expected }, repeats);
                Assert.That(ReadInitialSnapshot(probe.Counter).ValueFloat, Is.EqualTo(expected));
            }
        }

        [TestCase(CounterValueMode.Float, 3.75f, 3.75f)]
        [TestCase(CounterValueMode.Int, 3.75f, 4f)]
        public void Counter_SetReceive_PreservesFiniteInputAndIntRounding(
            CounterValueMode mode, float input, float expected)
        {
            using (CounterAdmissionProbe probe = new CounterAdmissionProbe(this, mode, 2f))
            {
                DeliverAbsolute(probe.Counter, input, new NetworkConnectionToClient(1023611));

                AssertAcceptedSequence(probe, new[] { expected }, 4);
                Assert.That(ReadInitialSnapshot(probe.Counter).ValueFloat, Is.EqualTo(expected));
            }
        }

        [Test]
        public void Counter_CommandReceive_BudgetIsSharedByCommandsAndIndependentBetweenPeers()
        {
            using (CounterAdmissionProbe probe = new CounterAdmissionProbe(this, CounterValueMode.Float, 2f))
            {
                NetworkConnectionToClient first = new NetworkConnectionToClient(1023612);
                NetworkConnectionToClient second = new NetworkConnectionToClient(1023613);
                DeliverOperation(probe.Counter, CounterModifyOperation.Add, 0.5f, first);
                byte[] firstSnapshot = CaptureInitialState(probe.Counter);
                int firstBytes = _counterRpcBytes;
                double firstBudget = SenderBudgets(probe.Counter)[first.connectionId];

                // No yield: the production unscaled frame clock has not advanced between these receives.
                DeliverAbsolute(probe.Counter, 100f, first);

                AssertAcceptedSequence(probe, new[] { 2.5f }, 2);
                Assert.That(CaptureInitialState(probe.Counter), Is.EqualTo(firstSnapshot));
                Assert.That(_counterRpcBytes, Is.EqualTo(firstBytes));
                Assert.That(SenderBudgets(probe.Counter)[first.connectionId], Is.EqualTo(firstBudget));

                DeliverOperation(probe.Counter, CounterModifyOperation.Multiply, 2f, second);
                DeliverAbsolute(probe.Counter, 7f, new NetworkConnectionToClient(1023614));

                AssertAcceptedSequence(probe, new[] { 2.5f, 5f, 7f }, 14);
                Assert.That(ReadInitialSnapshot(probe.Counter).ValueFloat, Is.EqualTo(7f));
                Assert.That(SenderBudgets(probe.Counter).Count, Is.EqualTo(3));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Counter_DirectPublicCalls_RetainLegacyNonfiniteBehavior(bool networked)
        {
            using (CounterAdmissionProbe probe = new CounterAdmissionProbe(this, CounterValueMode.Float, 2f))
            {
                SetCounterField(probe.Counter, "_invokeRepeatEventOnValueChanged", false);
                probe.Counter.isNetworked = networked;

                probe.Counter.Set(float.NaN);
                Assert.That(float.IsNaN(probe.Counter.ValueFloat), Is.True);
                probe.Counter.Set(float.MaxValue);
                probe.Counter.Multiply(2f);

                Assert.That(float.IsPositiveInfinity(probe.Counter.ValueFloat), Is.True);
                Assert.That(float.IsPositiveInfinity(SaveProvider.GetFloat(probe.SaveKey)), Is.True);
                Assert.That(probe.SaveWrites, Is.EqualTo(3));
                Assert.That(probe.Reactive.Count, Is.EqualTo(3));
                Assert.That(float.IsNaN(probe.Reactive[0]), Is.True);
                Assert.That(probe.Reactive[1], Is.EqualTo(float.MaxValue));
                Assert.That(float.IsPositiveInfinity(probe.Reactive[2]), Is.True);
                Assert.That(probe.FloatValues, Is.EqualTo(probe.Reactive));
                Assert.That(probe.IntValues, Is.Empty);
                Assert.That(probe.Repeats, Is.Zero);
                if (networked)
                {
                    Assert.That(_receivedRpcValues, Is.EqualTo(probe.Reactive));
                    Assert.That(float.IsPositiveInfinity(probe.RpcReceiver.ValueFloat), Is.True);
                    Assert.That(_counterRpcBytes, Is.GreaterThan(0));
                    Assert.That(float.IsPositiveInfinity(ReadInitialSnapshot(probe.Counter).ValueFloat), Is.True);
                }
                else
                {
                    Assert.That(_receivedRpcValues, Is.Empty);
                    Assert.That(_counterRpcBytes, Is.Zero);
                    Assert.That(probe.RpcReceiver.ValueFloat, Is.EqualTo(2f));
                    Assert.That(ReadInitialSnapshot(probe.Counter).ValueFloat, Is.EqualTo(2f));
                }

                Assert.That(SenderBudgets(probe.Counter), Is.Null);
            }
        }

        private void CheckRejectedReceive(CounterValueMode mode, float initialValue, float input,
            CounterModifyOperation? operation)
        {
            using (CounterAdmissionProbe probe = new CounterAdmissionProbe(this, mode, initialValue))
            {
                NetworkConnectionToClient sender = new NetworkConnectionToClient(1023615);
                byte[] before = CaptureInitialState(probe.Counter);
                if (operation.HasValue)
                {
                    DeliverOperation(probe.Counter, operation.Value, input, sender);
                }
                else
                {
                    DeliverAbsolute(probe.Counter, input, sender);
                }

                Assert.That(probe.Counter.ValueFloat.Equals(initialValue), Is.True, "Stored balance must stay unchanged.");
                Assert.That(probe.RpcReceiver.ValueFloat.Equals(initialValue), Is.True);
                Assert.That(CaptureInitialState(probe.Counter), Is.EqualTo(before), "SyncVar wire state must be unchanged.");
                Assert.That(probe.Reactive, Is.Empty);
                Assert.That(probe.FloatValues, Is.Empty);
                Assert.That(probe.IntValues, Is.Empty);
                Assert.That(probe.Repeats, Is.Zero);
                Assert.That(SaveProvider.GetFloat(probe.SaveKey), Is.EqualTo(77f));
                Assert.That(probe.SaveWrites, Is.Zero, "Even an identical save write is forbidden on rejection.");
                Assert.That(_receivedRpcValues, Is.Empty);
                Assert.That(_counterRpcBytes, Is.Zero);
                Dictionary<int, double> budgets = SenderBudgets(probe.Counter);
                Assert.That(budgets == null || !budgets.ContainsKey(sender.connectionId), Is.True,
                    "A denied numeric request must not start this sender's window.");

                // Same sender, same frame: finite absolute Set also repairs a preexisting NaN/Infinity balance.
                DeliverAbsolute(probe.Counter, 3f, sender);

                AssertAcceptedSequence(probe, new[] { 3f }, 3);
                Assert.That(ReadInitialSnapshot(probe.Counter).ValueFloat, Is.EqualTo(3f));
                Assert.That(SenderBudgets(probe.Counter).ContainsKey(sender.connectionId), Is.True);
            }
        }

        private void AssertAcceptedSequence(CounterAdmissionProbe probe, float[] values, int repeats)
        {
            float finalValue = values[values.Length - 1];
            Assert.That(probe.Counter.ValueFloat, Is.EqualTo(finalValue));
            Assert.That(probe.RpcReceiver.ValueFloat, Is.EqualTo(finalValue));
            Assert.That(probe.Reactive, Is.EqualTo(values));
            if (probe.Mode == CounterValueMode.Int)
            {
                List<int> expected = new List<int>();
                foreach (float value in values)
                {
                    expected.Add((int)value);
                }

                Assert.That(probe.IntValues, Is.EqualTo(expected));
                Assert.That(probe.FloatValues, Is.Empty);
            }
            else
            {
                Assert.That(probe.FloatValues, Is.EqualTo(values));
                Assert.That(probe.IntValues, Is.Empty);
            }

            Assert.That(probe.Repeats, Is.EqualTo(repeats));
            Assert.That(probe.SaveWrites, Is.EqualTo(values.Length));
            Assert.That(SaveProvider.GetFloat(probe.SaveKey), Is.EqualTo(finalValue));
            Assert.That(_receivedRpcValues, Is.EqualTo(values));
            Assert.That(_counterRpcBytes, Is.GreaterThan(0));
        }

        private static float NonfiniteValue(int value)
        {
            switch (value)
            {
                case 0: return float.NaN;
                case 1: return float.PositiveInfinity;
                case 2: return float.NegativeInfinity;
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        private static byte[] CaptureInitialState(Counter counter)
        {
            using (NetworkWriterPooled writer = NetworkWriterPool.Get())
            {
                counter.OnSerialize(writer, true);
                return writer.ToArray();
            }
        }

        private static Dictionary<int, double> SenderBudgets(Counter counter)
        {
            FieldInfo field = typeof(Neo.Network.NeoNetworkComponent).GetField("_lastCmdTimePerConnection",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (Dictionary<int, double>)field.GetValue(counter);
        }

        private static void SetCounterField(Counter counter, string name, object value)
        {
            FieldInfo field = typeof(Counter).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(counter, value);
        }

        private sealed class CounterAdmissionProbe : IDisposable
        {
            private readonly ISaveProvider _previousProvider;
            private readonly Action<string> _keyChanged;
            private bool _disposed;

            public readonly Counter Counter;
            public readonly Counter RpcReceiver;
            public readonly CounterValueMode Mode;
            public readonly string SaveKey;
            public readonly List<float> Reactive = new List<float>();
            public readonly List<float> FloatValues = new List<float>();
            public readonly List<int> IntValues = new List<int>();
            public int Repeats { get; private set; }
            public int SaveWrites { get; private set; }

            public CounterAdmissionProbe(Audit20261023NeoToolsComponentsNetworkTests fixture,
                CounterValueMode mode, float initialValue)
            {
                Mode = mode;
                SaveKey = "Audit20261023NeoToolsComponents.Admission." + Guid.NewGuid().ToString("N");
                _previousProvider = SaveProvider.CurrentProvider;
                _keyChanged = key =>
                {
                    if (key == SaveKey)
                    {
                        SaveWrites++;
                    }
                };
                SaveProvider.SetProvider(new ShopPurchasePlayModeTests.MemorySaveProvider());
                try
                {
                    Counter = fixture.CreateCounter(initialValue);
                    SetCounterField(Counter, "_valueMode", mode);
                    SetCounterField(Counter, "_saveKey", SaveKey);
                    SetCounterField(Counter, "_saveEnabled", true);
                    SetCounterField(Counter, "_loadOnStart", false);
                    SetCounterField(Counter, "_invokeRepeatEventOnValueChanged", true);
                    SaveProvider.SetFloat(SaveKey, 77f);
                    // This memory provider raises OnKeyChanged for every SetFloat, even an identical write.
                    SaveProvider.OnKeyChanged += _keyChanged;
                    RpcReceiver = fixture.ObserveValueRpcs(Counter);
                    SetCounterField(RpcReceiver, "_valueMode", mode);
                    RpcReceiver.Value.SetValueWithoutNotify(initialValue);
                    Counter.Value.AddListener(value => Reactive.Add(value));
                    Counter.OnValueChangedInt.AddListener(value => IntValues.Add(value));
                    Counter.OnValueChangedFloat.AddListener(value => FloatValues.Add(value));
                    Counter.OnRepeatByCounterValue.AddListener(() =>
                    {
                        Repeats++;
                        if (Repeats > 32)
                        {
                            throw new InvalidOperationException("Unexpected unbounded Counter repeat delivery.");
                        }
                    });
                }
                catch
                {
                    Dispose();
                    throw;
                }
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                SaveProvider.OnKeyChanged -= _keyChanged;
                try
                {
                    if (Counter != null)
                    {
                        SetCounterField(Counter, "_saveEnabled", false);
                    }

                    SaveProvider.DeleteKey(SaveKey);
                }
                finally
                {
                    SaveProvider.SetProvider(_previousProvider);
                }
            }
        }

        private IEnumerator CheckReentrantPublication(bool command, bool typedReentry)
        {
            Counter counter = CreateCounter(0f);
            yield return null;
            Counter rpcReceiver = ObserveValueRpcs(counter);
            List<int> typed = new List<int>();
            counter.OnValueChangedInt.AddListener(value =>
            {
                typed.Add(value);
                if (typedReentry && value == 1)
                {
                    counter.Set(2);
                }
            });
            counter.Value.AddListener(value =>
            {
                if (!typedReentry && value == 1f)
                {
                    counter.Set(2);
                }
            });

            if (command)
            {
                NetworkWriter writer = new NetworkWriter();
                writer.Write<float>(1f);
                Deliver(counter, "CmdSetValue", "System.Single,Mirror.NetworkConnectionToClient", writer,
                    new NetworkConnectionToClient(1023606));
            }
            else
            {
                counter.Set(1);
            }

            Counter lateReceiver = ReadInitialSnapshot(counter);
            Assert.That(counter.ValueInt, Is.EqualTo(2));
            Assert.That(rpcReceiver.ValueInt, Is.EqualTo(2));
            Assert.That(lateReceiver.ValueInt, Is.EqualTo(2));
            Assert.That(_receivedRpcValues, Is.EqualTo(new[] { 2f }), "Never send obsolete outer value 1.");
            Assert.That(typed, Is.EqualTo(typedReentry ? new[] { 1, 2 } : new[] { 2 }));
        }

        private Counter ObserveValueRpcs(Counter source)
        {
            _rpcReceiverObject = new GameObject("AuditComponentsRpcReceiver");
            _rpcReceiverObject.SetActive(false);
            NetworkIdentity identity = _rpcReceiverObject.AddComponent<NetworkIdentity>();
            Counter receiver = _rpcReceiverObject.AddComponent<Counter>();
            receiver.isNetworked = true;
            _rpcReceiverObject.SetActive(true);
            Assert.That(receiver.netIdentity, Is.SameAs(identity));
            ushort hash = FunctionHash("RpcSetValue", "System.Single");
            RemoteCallDelegate handler = RemoteProcedureCalls.GetDelegate(hash);
            Assert.That(handler, Is.Not.Null, "Mirror must register Counter's woven RPC receiver.");
            _rpcObserver = info =>
            {
                if (info.message is RpcMessage message && message.netId == source.netId &&
                    message.functionHash == hash)
                {
                    _counterRpcBytes += info.bytes;
                    NetworkReader payload = new NetworkReader(message.payload);
                    _receivedRpcValues.Add(payload.Read<float>());
                    Assert.That(payload.Remaining, Is.Zero);
                    NetworkReader receiverReader = new NetworkReader(message.payload);
                    handler(receiver, receiverReader, null);
                    Assert.That(receiverReader.Remaining, Is.Zero);
                }
            };
            NetworkDiagnostics.OutMessageEvent += _rpcObserver;
            return receiver;
        }

        private static void DeliverAbsolute(Counter counter, float value, NetworkConnectionToClient sender)
        {
            NetworkWriter writer = new NetworkWriter();
            writer.Write<float>(value);
            Deliver(counter, "CmdSetValue", "System.Single,Mirror.NetworkConnectionToClient", writer, sender);
        }

        private static void DeliverOperation(Counter counter, CounterModifyOperation operation, float operand,
            NetworkConnectionToClient sender)
        {
            NetworkWriter writer = new NetworkWriter();
            writer.Write<CounterModifyOperation>(operation);
            writer.Write<float>(operand);
            Deliver(counter, "CmdApplyOperation",
                "Neo.Tools.CounterModifyOperation,System.Single,Mirror.NetworkConnectionToClient", writer, sender);
        }

        private static void Deliver(Counter counter, string method, string parameters, NetworkWriter payload,
            NetworkConnectionToClient sender)
        {
            RemoteCallDelegate handler = RemoteProcedureCalls.GetDelegate(FunctionHash(method, parameters));
            Assert.That(handler, Is.Not.Null, "Mirror must register the woven Command receiver: " + method);
            NetworkReader reader = new NetworkReader(payload.ToArraySegment());
            handler(counter, reader, sender);
            Assert.That(reader.Remaining, Is.Zero);
        }

        private static ushort FunctionHash(string method, string parameters)
        {
            string signature = $"System.Void Neo.Tools.Counter::{method}({parameters})";
            return signature.GetStableHashCode16();
        }

        private Counter CreateCounter(float initialValue)
        {
            _counterObject = new GameObject("AuditComponentsInitialCounter");
            _counterObject.SetActive(false);
            NetworkIdentity identity = _counterObject.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(identity, 10230606);
            Counter counter = _counterObject.AddComponent<Counter>();
            counter.isNetworked = true;
            counter.Value.SetValueWithoutNotify(initialValue);
            _counterObject.SetActive(true);
            Assert.That(counter.netIdentity, Is.SameAs(identity), "The complete graph must exist before native Awake.");
            NetworkServer.Spawn(_counterObject);
            return counter;
        }

        private Counter ReadInitialSnapshot(Counter source)
        {
            _receiverObject = new GameObject("AuditComponentsSnapshotReceiver");
            _receiverObject.SetActive(false);
            NetworkIdentity identity = _receiverObject.AddComponent<NetworkIdentity>();
            Counter receiver = _receiverObject.AddComponent<Counter>();
            receiver.isNetworked = true;
            _receiverObject.SetActive(true);
            Assert.That(receiver.netIdentity, Is.SameAs(identity));
            // Exercise Mirror's generated wire serialization without claiming a second client session.
            using (NetworkWriterPooled writer = NetworkWriterPool.Get())
            {
                source.OnSerialize(writer, true);
                using (NetworkReaderPooled reader = NetworkReaderPool.Get(writer.ToArraySegment()))
                {
                    receiver.OnDeserialize(reader, true);
                }
            }

            receiver.OnStartClient();
            return receiver;
        }
    }
}
#endif
