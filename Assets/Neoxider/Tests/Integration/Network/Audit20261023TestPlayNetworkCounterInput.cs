#if MIRROR && UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using Mirror;
using Neo.Save;
using Neo.Tools;
using UnityEngine;

namespace Neo.Tests.Network.Integration
{
    public sealed class Audit20261023TestPlayNetworkRemoteCounter : Counter
    {
        protected override float NetworkRateLimit => 1000f;
    }

    /// <summary>Observes actual Counter messages and effects. Nonfinite values are encoded as kind/bits in JSON.</summary>
    [DefaultExecutionOrder(10000)]
    public sealed class Audit20261023TestPlayNetworkCounterProbe : MonoBehaviour
    {
        internal Counter Counter;
        internal Audit20261023TestPlayNetworkPeerDriver Driver;
        internal string CaseKey;
        internal bool IntegerMode;
        internal string SaveKey;
        internal string PersistenceFile;
        internal bool StartCompleted;
        private string _baselineHash;
        private int _valueEvents;
        private int _keyWrites;
        private int _rpcRecipients;
        private readonly List<MatrixCounterCommand> _commands = new List<MatrixCounterCommand>();
        private static readonly ushort SetHash =
            "System.Void Neo.Tools.Counter::CmdSetValue(System.Single,Mirror.NetworkConnectionToClient)".GetStableHashCode16();
        private static readonly ushort OperationHash =
            "System.Void Neo.Tools.Counter::CmdApplyOperation(Neo.Tools.CounterModifyOperation,System.Single,Mirror.NetworkConnectionToClient)"
                .GetStableHashCode16();

        private void Start() => StartCompleted = true;

        private void OnEnable()
        {
            Counter.Value.AddListener(ReactiveChanged);
            Counter.OnValueChangedFloat.AddListener(FloatChanged);
            Counter.OnValueChangedInt.AddListener(IntChanged);
            SaveProvider.OnKeyChanged += KeyChanged;
            SaveProvider.OnDataSaved += Saved;
            Mirror.NetworkDiagnostics.InMessageEvent += Received;
            Mirror.NetworkDiagnostics.OutMessageEvent += Sent;
        }

        private void OnDisable()
        {
            if (Counter != null)
            {
                Counter.Value.RemoveListener(ReactiveChanged);
                Counter.OnValueChangedFloat.RemoveListener(FloatChanged);
                Counter.OnValueChangedInt.RemoveListener(IntChanged);
            }
            SaveProvider.OnKeyChanged -= KeyChanged;
            SaveProvider.OnDataSaved -= Saved;
            Mirror.NetworkDiagnostics.InMessageEvent -= Received;
            Mirror.NetworkDiagnostics.OutMessageEvent -= Sent;
        }

        private void ReactiveChanged(float value) => RecordValue("ni19-reactive", value);
        private void FloatChanged(float value)
        {
            _valueEvents++;
            RecordValue("ni19-value", value);
            Driver.RecordCounterInput("ni19-float-event", 1f);
        }

        private void IntChanged(int value)
        {
            _valueEvents++;
            RecordValue("ni19-value", value);
            Driver.RecordCounterInput("ni19-int-event", 1f);
        }

        private void KeyChanged(string key)
        {
            if (key != SaveKey) return;
            _keyWrites++;
            RecordValue("ni19-key-write", SaveProvider.GetFloat(key, float.NaN));
        }

        private void Saved() => Driver.RecordCounterInput("ni19-save", 1f);

        private void RecordValue(string name, float value)
        {
            if (Kind(value) == "Finite") Driver.RecordCounterInput(name, value);
            else Driver.RecordCounterInput(name + "-nonfinite", 1f);
        }

        private void Received(Mirror.NetworkDiagnostics.MessageInfo info)
        {
            if (NetworkServer.active && info.message is CommandMessage message
                && message.netId == Counter.netId && message.componentIndex == Counter.ComponentIndex)
            {
                using (NetworkReaderPooled reader = NetworkReaderPool.Get(message.payload))
                {
                    string operation;
                    if (message.functionHash == SetHash) operation = "Set";
                    else if (message.functionHash == OperationHash)
                    {
                        CounterModifyOperation decoded = reader.Read<CounterModifyOperation>();
                        operation = decoded.ToString();
                    }
                    else
                    {
                        Driver.Fault("NI19 observed an unexpected Counter Command hash.");
                        return;
                    }
                    float operand = reader.ReadFloat();
                    float before = Counter.ValueFloat;
                    // Installed Mirror emits InMessageEvent after decoding and BEFORE the handler.
                    _commands.Add(new MatrixCounterCommand
                    {
                        operation = operation, operandKind = Kind(operand), operandBits = Bits(operand),
                        finiteOperand = Kind(operand) == "Finite" ? operand : 0f,
                        frame = Time.frameCount, unscaledTime = Time.unscaledTimeAsDouble,
                        beforeKind = Kind(before), beforeBits = Bits(before), valueEventsBefore = _valueEvents,
                        keyWritesBefore = _keyWrites, rpcRecipientsBefore = _rpcRecipients
                    });
                }
            }
            else if (!NetworkServer.active && info.message is RpcMessage rpc
                && rpc.netId == Counter.netId && rpc.componentIndex == Counter.ComponentIndex)
                RecordRpc(rpc, info.count);
        }

        private void Sent(Mirror.NetworkDiagnostics.MessageInfo info)
        {
            if (NetworkServer.active && info.message is RpcMessage rpc
                && rpc.netId == Counter.netId && rpc.componentIndex == Counter.ComponentIndex)
                RecordRpc(rpc, info.count);
        }

        private void RecordRpc(RpcMessage message, int recipients)
        {
            using (NetworkReaderPooled reader = NetworkReaderPool.Get(message.payload))
            {
                float value = reader.ReadFloat();
                _rpcRecipients += recipients;
                for (int i = 0; i < recipients; i++) RecordValue("ni19-rpc", value);
            }
        }

        internal void BeginPhase()
        {
            _baselineHash = PersistenceHash();
            _commands.Clear();
            _valueEvents = 0;
            _keyWrites = 0;
            _rpcRecipients = 0;
        }

        internal MatrixCounterInput Capture()
        {
            float value = Counter.ValueFloat;
            float stored = SaveProvider.GetFloat(SaveKey, float.NaN);
            string hash = PersistenceHash();
            return new MatrixCounterInput
            {
                exists = true, startCompleted = StartCompleted, netId = Counter.netId,
                caseKey = CaseKey, integerMode = IntegerMode, saveKey = SaveKey,
                valueKind = Kind(value), valueBits = Bits(value), finiteValue = Kind(value) == "Finite" ? value : 0f,
                storedKind = Kind(stored), storedBits = Bits(stored), finiteStoredValue = Kind(stored) == "Finite" ? stored : 0f,
                persistenceSha256 = hash, persistenceFileUnchanged = hash == _baselineHash,
                commands = _commands.ToArray()
            };
        }

        internal static string Kind(float value) => float.IsNaN(value) ? "NaN"
            : float.IsInfinity(value) ? value > 0f ? "+Infinity" : "-Infinity" : "Finite";

        internal static string Bits(float value) => BitConverter.ToUInt32(BitConverter.GetBytes(value), 0)
            .ToString("x8", CultureInfo.InvariantCulture);

        private string PersistenceHash()
        {
            if (!File.Exists(PersistenceFile)) return "missing";
            using (SHA256 algorithm = SHA256.Create())
                return BitConverter.ToString(algorithm.ComputeHash(File.ReadAllBytes(PersistenceFile)))
                    .Replace("-", string.Empty).ToLowerInvariant();
        }
    }

    public sealed partial class Audit20261023TestPlayNetworkPeerDriver
    {
        private sealed class CounterInputCase
        {
            internal readonly string Key;
            internal readonly CounterModifyOperation Operation;
            internal readonly float Initial;
            internal readonly float[] Invalid;
            internal readonly float Valid;
            internal readonly float Result;
            internal readonly bool Integer;

            internal CounterInputCase(string key, CounterModifyOperation operation, float initial,
                float[] invalid, float valid, float result, bool integer = false)
            {
                Key = key; Operation = operation; Initial = initial; Invalid = invalid;
                Valid = valid; Result = result; Integer = integer;
            }
        }

        private const uint CounterInputAssetBase = 1023100600;
        private static readonly float[] NonfiniteCounterInputs = { float.NaN, float.PositiveInfinity, float.NegativeInfinity };
        private static readonly CounterInputCase[] CounterInputCases =
        {
            new CounterInputCase("set-nonfinite", CounterModifyOperation.Set, 10.5f, NonfiniteCounterInputs, 12.5f, 12.5f),
            new CounterInputCase("add-nonfinite", CounterModifyOperation.Add, 10.5f, NonfiniteCounterInputs, 0.25f, 10.75f),
            new CounterInputCase("multiply-nonfinite", CounterModifyOperation.Multiply, 10.5f, NonfiniteCounterInputs, 2f, 21f),
            new CounterInputCase("add-positive-overflow", CounterModifyOperation.Add, 2e38f,
                new[] { 2e38f }, -2e38f, 0f),
            new CounterInputCase("add-negative-overflow", CounterModifyOperation.Add, -2e38f,
                new[] { -2e38f }, 2e38f, 0f),
            new CounterInputCase("multiply-positive-overflow", CounterModifyOperation.Multiply, 2e38f,
                new[] { 2f }, 0.5f, 1e38f),
            new CounterInputCase("multiply-negative-overflow", CounterModifyOperation.Multiply, -2e38f,
                new[] { 2f }, 0.5f, -1e38f),
            new CounterInputCase("int-multiply-overflow", CounterModifyOperation.Multiply, 2f,
                new[] { float.MaxValue }, 3f, 6f, true),
            new CounterInputCase("finite-peers", CounterModifyOperation.Add, 10.5f, Array.Empty<float>(), 0.25f, 10.75f)
        };
        private Audit20261023TestPlayNetworkCounterProbe _counterInputProbe;

        private void RegisterCounterInputFactories()
        {
            for (int i = 0; i < CounterInputCases.Length; i++)
            {
                int index = i;
                NetworkClient.RegisterSpawnHandler(CounterInputAssetBase + (uint)i,
                    (SpawnMessage message) => CreateCounterInput(index),
                    (GameObject value) => UnityEngine.Object.Destroy(value));
            }
        }

        private GameObject CreateCounterInput(int index)
        {
            Require(_counterInputProbe == null, "Previous NI19 fixture has not despawned.");
            CounterInputCase vector = CounterInputCases[index];
            string key = "matrix-ni19-" + _options.RunId + "-" + vector.Key;
            GameObject root = Own(new GameObject("MatrixCounterNI19_" + vector.Key));
            root.SetActive(false);
            root.AddComponent<NetworkIdentity>().sceneId = 0;
            Counter counter = root.AddComponent<Audit20261023TestPlayNetworkRemoteCounter>();
            counter.isNetworked = true;
            Audit20261023TestPlayNetworkWorld.Configure(counter, "_valueMode", vector.Integer ? CounterValueMode.Int : CounterValueMode.Float);
            Audit20261023TestPlayNetworkWorld.Configure(counter, "_saveEnabled", true);
            Audit20261023TestPlayNetworkWorld.Configure(counter, "_saveKey", key);
            Audit20261023TestPlayNetworkWorld.Configure(counter, "_loadOnStart", false);
            Audit20261023TestPlayNetworkWorld.Configure(counter, "_sendOnStart", false);
            Audit20261023TestPlayNetworkWorld.Configure(counter, "_invokeRepeatEventOnValueChanged", false);
            Audit20261023TestPlayNetworkWorld.Configure(counter, "_invokeRepeatEventOnSend", false);
            _counterInputProbe = root.AddComponent<Audit20261023TestPlayNetworkCounterProbe>();
            _counterInputProbe.Counter = counter;
            _counterInputProbe.Driver = this;
            _counterInputProbe.CaseKey = vector.Key;
            _counterInputProbe.IntegerMode = vector.Integer;
            _counterInputProbe.SaveKey = key;
            _counterInputProbe.PersistenceFile = Path.Combine(_options.SaveRoot, "matrix-save.json");
            root.SetActive(true);
            return root;
        }

        internal void RecordCounterInput(string name, float value)
        {
            if (_world != null) _world.Record(name, value);
        }

        internal void ResetCounterInputPhase()
        {
            if (_counterInputProbe != null) _counterInputProbe.BeginPhase();
        }

        internal MatrixCounterInput CaptureCounterInput() => _counterInputProbe == null
            ? new MatrixCounterInput() : _counterInputProbe.Capture();

        private void FlushCounterInput(string caseId)
        {
            SaveProvider.Save();
            FileSaveProvider reload = new FileSaveProvider("matrix-save.json",
                new FileSaveProviderOptions { PersistenceRoot = _options.SaveRoot });
            float stored = reload.GetFloat(_counterInputProbe.SaveKey, float.NaN);
            Require(Audit20261023TestPlayNetworkCounterProbe.Bits(stored)
                == Audit20261023TestPlayNetworkCounterProbe.Bits(_counterInputProbe.Counter.ValueFloat),
                "NI19 isolated persisted Counter value did not round-trip.");
            _artifacts.Write("counter-input-persistence-reload", "Actual own-provider file reload matches Counter float bits.",
                step: _step, caseId: caseId, state: _world.Capture(_playerA, _playerB), epoch: _epoch);
        }

        private IEnumerator CounterFiniteIngressCases()
        {
            for (int index = 0; index < CounterInputCases.Length; index++)
            {
                CounterInputCase vector = CounterInputCases[index];
                GameObject root = CreateCounterInput(index);
                NetworkServer.Spawn(root, CounterInputAssetBase + (uint)index);
                yield return Until(() => _counterInputProbe.StartCompleted, "NI19 server Counter Start");
                _counterInputProbe.Counter.Set(vector.Initial);
                yield return ExecutePeers("counter-ni19-" + vector.Key + "-prepare", "counter-input-prepare", index,
                    "counter-input-prepare", index);
                FlushCounterInput("counter-ni19-" + vector.Key + "-prepare");
                if (vector.Invalid.Length != 0)
                {
                    string denied = "counter-ni19-" + vector.Key + "-denied";
                    yield return ResetEvents(denied);
                    yield return ExecutePeers(denied, "counter-input-invalid", index);
                    AssertCounterInputCommands(vector, false);
                    SetCounterInputExpected(vector, vector.Initial);
                    yield return ObservePeers(denied, MatrixChecks.Counter | MatrixChecks.Money | MatrixChecks.CounterInput
                        | MatrixChecks.CounterInputPersistenceStable, CounterInputEvents(vector),
                        CounterInputEvents(vector), CounterInputEvents(vector));

                    string budget = "counter-ni19-" + vector.Key + "-budget";
                    yield return ResetEvents(budget);
                    yield return ExecutePeers(budget, "counter-input-invalid-valid", index);
                    AssertCounterInputCommands(vector, true);
                    SetCounterInputExpected(vector, vector.Result);
                    yield return ObservePeers(budget, MatrixChecks.Counter | MatrixChecks.Money | MatrixChecks.CounterInput,
                        CounterInputEvents(vector, vector.Result, 2), CounterInputEvents(vector, vector.Result, 1),
                        CounterInputEvents(vector, vector.Result, 1));
                    yield return ExecutePeers(budget, "counter-input-flush", index, "counter-input-flush", index);
                    FlushCounterInput(budget);
                }
                else
                {
                    const string first = "counter-ni19-finite-peer-a-rate";
                    yield return ResetEvents(first);
                    yield return ExecutePeers(first, "counter-input-finite-twice", index);
                    MatrixCounterCommand[] firstCommands = CaptureCounterInput().commands;
                    Require(firstCommands.Length == 2 && firstCommands[0].frame == firstCommands[1].frame,
                        "NI19 finite same-peer duplicate did not share its measured frame.");
                    double admittedAt = firstCommands[0].unscaledTime;
                    SetCounterInputExpected(vector, 10.75f);
                    yield return ObservePeers(first, MatrixChecks.Counter | MatrixChecks.Money | MatrixChecks.CounterInput,
                        CounterInputEvents(vector, 10.75f, 2), CounterInputEvents(vector, 10.75f, 1),
                        CounterInputEvents(vector, 10.75f, 1));
                    const string second = "counter-ni19-independent-peer-b";
                    yield return ResetEvents(second);
                    yield return ExecutePeers(second, "none", 0, "counter-input-finite-peer-b", index);
                    Require(Time.unscaledTimeAsDouble - admittedAt < 1000d,
                        "NI19 independent-peer observation exceeded the test-owned limiter window.");
                    SetCounterInputExpected(vector, 21.5f);
                    yield return ObservePeers(second, MatrixChecks.Counter | MatrixChecks.Money | MatrixChecks.CounterInput,
                        CounterInputEvents(vector, 21.5f, 2), CounterInputEvents(vector, 21.5f, 1),
                        CounterInputEvents(vector, 21.5f, 1));
                    yield return ExecutePeers(second, "counter-input-flush", index, "counter-input-flush", index);
                    FlushCounterInput(second);
                }
                NetworkServer.Destroy(root);
                yield return Until(() => _counterInputProbe == null, "NI19 additional server Counter released");
                yield return ExecutePeers("counter-ni19-" + vector.Key + "-release", "counter-input-released", index,
                    "counter-input-released", index);
            }
            _expectedState.counterInput = null;
            yield return ObservePeers("counter-ni19-original-oracles-preserved", MatrixChecks.Counter | MatrixChecks.Money);
        }

        private IEnumerator ExecuteCounterInput(MatrixInstruction instruction)
        {
            if (instruction.action == "counter-input-released")
            {
                yield return Until(() => _counterInputProbe == null, "NI19 additional client Counter despawned");
                yield break;
            }
            CounterInputCase vector = CounterInputCases[instruction.value];
            yield return Until(() => _counterInputProbe != null && _counterInputProbe.StartCompleted
                && _counterInputProbe.CaseKey == vector.Key, "NI19 remote Counter ready");
            Counter counter = _counterInputProbe.Counter;
            if (instruction.action == "counter-input-prepare")
            {
                yield return Until(() => Audit20261023TestPlayNetworkCounterProbe.Bits(counter.ValueFloat)
                    == Audit20261023TestPlayNetworkCounterProbe.Bits(vector.Initial), "NI19 real baseline RPC received");
                FlushCounterInput(instruction.caseId);
            }
            else if (instruction.action == "counter-input-flush") FlushCounterInput(instruction.caseId);
            else if (instruction.action == "counter-input-finite-twice")
            {
                counter.Add(0.25f);
                counter.Add(0.5f);
            }
            else if (instruction.action == "counter-input-finite-peer-b") counter.Multiply(2f);
            else
            {
                Require(instruction.action == "counter-input-invalid" || instruction.action == "counter-input-invalid-valid",
                    "Unknown NI19 action: " + instruction.action);
                foreach (float operand in vector.Invalid) InvokeCounterPublic(counter, vector.Operation, operand);
                if (instruction.action == "counter-input-invalid-valid") InvokeCounterPublic(counter, vector.Operation, vector.Valid);
            }
        }

        private static void InvokeCounterPublic(Counter counter, CounterModifyOperation operation, float operand)
        {
            switch (operation)
            {
                case CounterModifyOperation.Set: counter.Set(operand); break;
                case CounterModifyOperation.Add: counter.Add(operand); break;
                case CounterModifyOperation.Multiply: counter.Multiply(operand); break;
                default: throw new InvalidOperationException("NI19 fixture uses only Set/Add/Multiply.");
            }
        }

        private void AssertCounterInputCommands(CounterInputCase vector, bool includesValid)
        {
            MatrixCounterCommand[] commands = CaptureCounterInput().commands;
            Require(commands.Length == vector.Invalid.Length + (includesValid ? 1 : 0), "NI19 actual ingress count differs.");
            for (int i = 0; i < commands.Length; i++)
            {
                float operand = i < vector.Invalid.Length ? vector.Invalid[i] : vector.Valid;
                MatrixCounterCommand command = commands[i];
                Require(command.operation == vector.Operation.ToString()
                    && command.operandKind == Audit20261023TestPlayNetworkCounterProbe.Kind(operand)
                    && command.operandBits == Audit20261023TestPlayNetworkCounterProbe.Bits(operand)
                    && command.beforeKind == "Finite"
                    && command.beforeBits == Audit20261023TestPlayNetworkCounterProbe.Bits(vector.Initial)
                    && command.valueEventsBefore == 0 && command.keyWritesBefore == 0 && command.rpcRecipientsBefore == 0,
                    "NI19 malformed ingress changed state/events/save/RPC before the next actual Command.");
            }
            if (includesValid)
                Require(commands[0].frame == commands[commands.Length - 1].frame
                    && commands[commands.Length - 1].unscaledTime - commands[0].unscaledTime < 0.05d,
                    "NI19 malformed/valid commands did not share the production-sized budget window.");
        }

        private void SetCounterInputExpected(CounterInputCase vector, float value)
        {
            string bits = Audit20261023TestPlayNetworkCounterProbe.Bits(value);
            _expectedState.counterInput = new MatrixCounterInput
                { caseKey = vector.Key, integerMode = vector.Integer, valueBits = bits, storedBits = bits };
        }

        private static MatrixEvents[] CounterInputEvents(CounterInputCase vector, float? value = null, int rpcRecipients = 0)
        {
            float[] changed = value.HasValue ? new[] { value.Value } : Array.Empty<float>();
            float[] rpc = new float[rpcRecipients];
            for (int i = 0; i < rpc.Length; i++) rpc[i] = value.Value;
            float[] marker = value.HasValue ? new[] { 1f } : Array.Empty<float>();
            return new[]
            {
                new MatrixEvents { name = "ni19-reactive", values = changed },
                new MatrixEvents { name = "ni19-value", values = changed },
                new MatrixEvents { name = "ni19-float-event", values = vector.Integer ? Array.Empty<float>() : marker },
                new MatrixEvents { name = "ni19-int-event", values = vector.Integer ? marker : Array.Empty<float>() },
                new MatrixEvents { name = "ni19-key-write", values = changed },
                new MatrixEvents { name = "ni19-rpc", values = rpc }, new MatrixEvents { name = "ni19-save" },
                new MatrixEvents { name = "ni19-reactive-nonfinite" }, new MatrixEvents { name = "ni19-value-nonfinite" },
                new MatrixEvents { name = "ni19-key-write-nonfinite" }, new MatrixEvents { name = "ni19-rpc-nonfinite" }
            };
        }
    }
}
#endif
