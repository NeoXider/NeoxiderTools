#if MIRROR && UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using Mirror;

namespace Neo.Tests.Network.Integration
{
    public struct Audit20261023TestPlayNetworkSignal : NetworkMessage
    {
        public string RunId;
        public string SnapshotSha;
        public string PeerId;
        public string Kind;
        public int Epoch;
        public int Step;
        public string Json;
    }

    public struct Audit20261023TestPlayNetworkLeaseProbe : NetworkMessage
    {
        public int Step;
        public int Value;
        public string Text;
    }

    public struct Audit20261023TestPlayNetworkLeaseReply : NetworkMessage
    {
        public int Step;
        public int Value;
        public string Text;
    }

    [Flags]
    internal enum MatrixChecks
    {
        None = 0,
        Counter = 1,
        Selector = 2,
        Money = 4,
        Rpg = 8,
        Property = 16,
        Reactive = 32,
        Context = 64,
        Ownership = 128,
        PrecisionMoney = 256,
        PrecisionPersistenceStable = 512,
        CounterInput = 1024,
        CounterInputPersistenceStable = 2048
    }

    [Serializable]
    internal sealed class MatrixEvents
    {
        public string name;
        public float[] values = Array.Empty<float>();
        public bool unordered;
    }

    [Serializable]
    internal sealed class MatrixCounterCommand
    {
        public string operation;
        public string operandKind;
        public string operandBits;
        public float finiteOperand;
        public int frame;
        public double unscaledTime;
        public string beforeKind;
        public string beforeBits;
        public int valueEventsBefore;
        public int keyWritesBefore;
        public int rpcRecipientsBefore;
    }

    [Serializable]
    internal sealed class MatrixCounterInput
    {
        public bool exists;
        public bool startCompleted;
        public uint netId;
        public string caseKey;
        public bool integerMode;
        public string saveKey;
        public string valueKind;
        public string valueBits;
        public float finiteValue;
        public string storedKind;
        public string storedBits;
        public float finiteStoredValue;
        public string persistenceSha256;
        public bool persistenceFileUnchanged;
        public MatrixCounterCommand[] commands = Array.Empty<MatrixCounterCommand>();
    }

    [Serializable]
    internal sealed class MatrixPrecisionWallet
    {
        public bool exists;
        public bool startCompleted;
        public uint netId;
        public string saveKey;
        public float current;
        public float lifetime;
        public float level;
        public float lastChange;
        public float storedCurrent;
        public float storedLifetime;
        public float phaseBalanceBefore;
        public float storedDebit;
        public string persistenceSha256;
        public bool persistenceFileUnchanged;
        public int[] ingressFrames = Array.Empty<int>();
        public double[] ingressTimes = Array.Empty<double>();
    }

    [Serializable]
    internal sealed class MatrixSpendRequest
    {
        public float amount;
        public string status;
        public bool isConfirmed;
        public bool isPendingServerAuthority;
        public float balanceBefore;
        public float balanceAfter;
    }

    [Serializable]
    internal sealed class MatrixState
    {
        public uint netId;
        public uint playerA;
        public uint playerB;
        public bool isOwned;
        public bool startCompleted;
        public int counter;
        public int selector;
        public bool selectorFill;
        public int[] activeIndices = Array.Empty<int>();
        public int[] excludedIndices = Array.Empty<int>();
        public float money;
        public float hp;
        public float maxHp;
        public float resource;
        public float property;
        public float reactive;
        public bool reactiveExists;
        public bool bonusA;
        public bool bonusB;
        public int propertyReads;
        public string leasePayload;
        public MatrixPrecisionWallet precision;
        public MatrixCounterInput counterInput;
        public MatrixEvents[] events = Array.Empty<MatrixEvents>();
    }

    [Serializable]
    internal sealed class MatrixExpected
    {
        public MatrixChecks checks;
        public MatrixState state = new MatrixState();
        public MatrixEvents[] events = Array.Empty<MatrixEvents>();

        public string Mismatch(MatrixState actual)
        {
            if (actual == null || !actual.startCompleted)
                return "world Start has not completed";
            if ((checks & MatrixChecks.Counter) != 0 && actual.counter != state.counter)
                return "counter: " + actual.counter + " != " + state.counter;
            if ((checks & MatrixChecks.Ownership) != 0 && actual.isOwned != state.isOwned)
                return "world ownership differs";
            if ((checks & MatrixChecks.Selector) != 0 && (actual.selector != state.selector
                || actual.selectorFill != state.selectorFill
                || !Equal(actual.activeIndices, state.activeIndices)
                || !Equal(actual.excludedIndices, state.excludedIndices)))
                return "selector index/fill/complete active/excluded snapshot differs";
            if ((checks & MatrixChecks.Money) != 0 && !Near(actual.money, state.money))
                return "wallet balance: " + actual.money + " != " + state.money;
            if ((checks & MatrixChecks.CounterInput) != 0)
            {
                MatrixCounterInput observed = actual.counterInput;
                MatrixCounterInput wanted = state.counterInput;
                if (observed == null || !observed.exists || !observed.startCompleted || wanted == null
                    || observed.caseKey != wanted.caseKey || observed.integerMode != wanted.integerMode
                    || observed.valueKind != "Finite" || observed.storedKind != "Finite"
                    || observed.valueBits != wanted.valueBits || observed.storedBits != wanted.storedBits)
                    return "NI19 Counter value/mode/persisted float bits differ";
            }
            if ((checks & MatrixChecks.CounterInputPersistenceStable) != 0
                && (actual.counterInput == null || !actual.counterInput.persistenceFileUnchanged))
                return "NI19 rejected Counter ingress changed the persisted file";
            if ((checks & MatrixChecks.PrecisionMoney) != 0)
            {
                MatrixPrecisionWallet observed = actual.precision;
                MatrixPrecisionWallet wanted = state.precision;
                if (observed == null || !observed.exists || !observed.startCompleted || wanted == null
                    || !Near(observed.current, wanted.current) || !Near(observed.lifetime, wanted.lifetime)
                    || !Near(observed.level, wanted.level) || !Near(observed.lastChange, wanted.lastChange)
                    || !Near(observed.storedCurrent, wanted.storedCurrent)
                    || !Near(observed.storedLifetime, wanted.storedLifetime)
                    || !Near(observed.phaseBalanceBefore, wanted.phaseBalanceBefore)
                    || !Near(observed.storedDebit, wanted.storedDebit))
                    return "NI7 wallet/current/lifetime/level/last change/persistence/stored debit differs";
            }
            if ((checks & MatrixChecks.PrecisionPersistenceStable) != 0
                && (actual.precision == null || !actual.precision.persistenceFileUnchanged))
                return "NI7 rejected spend changed the persisted file";
            if ((checks & MatrixChecks.Rpg) != 0 && (!Near(actual.hp, state.hp)
                || !Near(actual.maxHp, state.maxHp) || !Near(actual.resource, state.resource)))
                return "RPG final HP/max/custom resource differs";
            if ((checks & MatrixChecks.Property) != 0 && !Near(actual.property, state.property))
                return "property receiver value differs";
            if ((checks & MatrixChecks.Reactive) != 0 && (!actual.reactiveExists
                || !Near(actual.reactive, state.reactive)))
                return "reactive receiver instance/value differs";
            if ((checks & MatrixChecks.Context) != 0 && (actual.bonusA != state.bonusA
                || actual.bonusB != state.bonusB))
                return "context target/sibling state differs";
            foreach (MatrixEvents expected in events)
            {
                float[] observed = Array.Empty<float>();
                foreach (MatrixEvents sample in actual.events)
                    if (sample.name == expected.name) observed = sample.values;
                float[] wanted = expected.values;
                if (observed.Length != wanted.Length)
                    return expected.name + " event count: " + observed.Length + " != " + wanted.Length;
                if (expected.unordered)
                {
                    observed = (float[])observed.Clone();
                    wanted = (float[])wanted.Clone();
                    Array.Sort(observed);
                    Array.Sort(wanted);
                }
                for (int i = 0; i < wanted.Length; i++)
                    if (!Near(observed[i], wanted[i])) return expected.name + " received payload differs";
            }
            return null;
        }

        private static bool Near(float first, float second) => Math.Abs(first - second) <= 0.0001f;

        private static bool Equal(int[] first, int[] second)
        {
            if (first == null || second == null || first.Length != second.Length) return false;
            for (int i = 0; i < first.Length; i++) if (first[i] != second[i]) return false;
            return true;
        }
    }

    [Serializable]
    internal sealed class MatrixInstruction
    {
        public string caseId;
        public string action;
        public int value;
        public uint playerA;
        public uint playerB;
        public string rendezvous;
        public MatrixExpected expected;
    }

    [Serializable]
    internal sealed class MatrixEnvironment
    {
        public string company;
        public string product;
        public string persistentDataPath;
        public string playerPrefsNamespace;
        public string saveRoot;
        public string saveFile;
        public string saveProvider;
        public string platform;
        public string unityVersion;
        public string applicationVersion;
        public string mirrorAssemblyVersion;
        public string transport;
        public int serverPort;
        public int processId;
    }

    [Serializable]
    internal sealed class MatrixReply
    {
        public MatrixEnvironment environment;
        public MatrixState state;
        public int counterAtInvocation = -1;
        public MatrixSpendRequest[] spendRequests = Array.Empty<MatrixSpendRequest>();
        public string detail;
    }

    [Serializable]
    internal sealed class MatrixRecord
    {
        public int schema = 1;
        public string kind;
        public string runId;
        public string snapshotSha;
        public string peerId;
        public string fromPeer;
        public int processId;
        public int connectionId = -1;
        public int epoch;
        public int step;
        public string caseId;
        public bool passed;
        public double realtime;
        public string detail;
        public MatrixEnvironment environment;
        public MatrixState state;
        public MatrixSpendRequest[] spendRequests = Array.Empty<MatrixSpendRequest>();
    }
}
#endif
