#if MIRROR && UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Mirror;
using Neo.Network;
using Neo.Save;
using Neo.Shop;
using UnityEngine;

namespace Neo.Tests.Network.Integration
{
    /// <summary>Passive observations of one additional, persisted wallet; no private Money invocation.</summary>
    [DefaultExecutionOrder(10000)]
    public sealed class Audit20261023TestPlayNetworkMoneyProbe : MonoBehaviour
    {
        internal Money Wallet;
        internal Audit20261023TestPlayNetworkPeerDriver Driver;
        internal string PersistenceFile;
        internal bool StartCompleted;
        internal double LastSpendIngressTime = double.NegativeInfinity;
        private string _baselineHash;
        private float _phaseBalanceBefore;
        private readonly List<int> _ingressFrames = new List<int>();
        private readonly List<double> _ingressTimes = new List<double>();

        private void Start() => StartCompleted = true;

        private void OnEnable()
        {
            Wallet.CurrentMoney.AddListener(CurrentChanged);
            Wallet.AllMoney.AddListener(LifetimeChanged);
            Wallet.LevelMoney.AddListener(LevelChanged);
            Wallet.LastChangeMoney.AddListener(LastChanged);
            SaveProvider.OnKeyChanged += KeyChanged;
            SaveProvider.OnDataSaved += Saved;
            Mirror.NetworkDiagnostics.InMessageEvent += Received;
            Mirror.NetworkDiagnostics.OutMessageEvent += Sent;
        }

        private void OnDisable()
        {
            if (Wallet != null)
            {
                Wallet.CurrentMoney.RemoveListener(CurrentChanged);
                Wallet.AllMoney.RemoveListener(LifetimeChanged);
                Wallet.LevelMoney.RemoveListener(LevelChanged);
                Wallet.LastChangeMoney.RemoveListener(LastChanged);
            }
            SaveProvider.OnKeyChanged -= KeyChanged;
            SaveProvider.OnDataSaved -= Saved;
            Mirror.NetworkDiagnostics.InMessageEvent -= Received;
            Mirror.NetworkDiagnostics.OutMessageEvent -= Sent;
        }

        private void CurrentChanged(float value) => Driver.RecordPrecision("ni7-current", value);
        private void LifetimeChanged(float value) => Driver.RecordPrecision("ni7-lifetime", value);
        private void LevelChanged(float value) => Driver.RecordPrecision("ni7-level", value);
        private void LastChanged(float value) => Driver.RecordPrecision("ni7-last-change", value);
        private void Saved() => Driver.RecordPrecision("ni7-save", 1f);

        private void KeyChanged(string key)
        {
            if (key == Wallet.SaveKey)
                Driver.RecordPrecision("ni7-persist-current", SaveProvider.GetFloat(key));
            else if (key == Wallet.SaveKey + nameof(Money.AllMoney))
                Driver.RecordPrecision("ni7-persist-lifetime", SaveProvider.GetFloat(key));
        }

        private void Received(Mirror.NetworkDiagnostics.MessageInfo info)
        {
            if (NetworkServer.active && info.message is CommandMessage command
                && command.netId == Wallet.netId && command.componentIndex == Wallet.ComponentIndex)
            {
                using (NetworkReaderPooled reader = NetworkReaderPool.Get(command.payload))
                {
                    byte operation = reader.ReadByte();
                    float amount = reader.ReadFloat();
                    Driver.RecordPrecision("ni7-command-op", operation);
                    Driver.RecordPrecision("ni7-command-amount", amount);
                    if (operation == 1)
                    {
                        LastSpendIngressTime = Time.unscaledTimeAsDouble;
                        _ingressFrames.Add(Time.frameCount);
                        _ingressTimes.Add(LastSpendIngressTime);
                    }
                }
            }
            else if (!NetworkServer.active && info.message is RpcMessage rpc
                && rpc.netId == Wallet.netId && rpc.componentIndex == Wallet.ComponentIndex)
                RecordRpc(rpc, info.count);
        }

        private void Sent(Mirror.NetworkDiagnostics.MessageInfo info)
        {
            if (NetworkServer.active && info.message is RpcMessage rpc
                && rpc.netId == Wallet.netId && rpc.componentIndex == Wallet.ComponentIndex)
                RecordRpc(rpc, info.count);
        }

        private void RecordRpc(RpcMessage rpc, int count)
        {
            using (NetworkReaderPooled reader = NetworkReaderPool.Get(rpc.payload))
            {
                byte operation = reader.ReadByte();
                float amount = reader.ReadFloat();
                for (int i = 0; i < count; i++)
                {
                    Driver.RecordPrecision("ni7-rpc-op", operation);
                    Driver.RecordPrecision("ni7-rpc-amount", amount);
                }
            }
        }

        internal void BeginPhase()
        {
            _phaseBalanceBefore = Wallet.money;
            _baselineHash = PersistenceHash();
            _ingressFrames.Clear();
            _ingressTimes.Clear();
        }

        internal MatrixPrecisionWallet Capture()
        {
            string hash = PersistenceHash();
            return new MatrixPrecisionWallet
            {
                exists = true, startCompleted = StartCompleted, netId = Wallet.netId, saveKey = Wallet.SaveKey,
                current = Wallet.money, lifetime = Wallet.allMoney, level = Wallet.levelMoney,
                lastChange = Wallet.LastChangeMoneyValue,
                storedCurrent = SaveProvider.GetFloat(Wallet.SaveKey, float.NaN),
                storedLifetime = SaveProvider.GetFloat(Wallet.SaveKey + nameof(Money.AllMoney), float.NaN),
                phaseBalanceBefore = _phaseBalanceBefore, storedDebit = _phaseBalanceBefore - Wallet.money,
                persistenceSha256 = hash, persistenceFileUnchanged = hash == _baselineHash,
                ingressFrames = _ingressFrames.ToArray(), ingressTimes = _ingressTimes.ToArray()
            };
        }

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
        internal const uint PrecisionWalletAssetId = 1023100503;
        private Audit20261023TestPlayNetworkMoneyProbe _precisionProbe;

        internal void RecordPrecision(string name, float value)
        {
            if (_world != null) _world.Record(name, value);
        }

        internal void ResetPrecisionPhase()
        {
            if (_precisionProbe != null) _precisionProbe.BeginPhase();
        }

        internal MatrixPrecisionWallet CapturePrecisionWallet() => _precisionProbe == null
            ? new MatrixPrecisionWallet() : _precisionProbe.Capture();

        private GameObject CreatePrecisionWallet()
        {
            Require(_precisionProbe == null, "Duplicate NI7 wallet spawn.");
            GameObject root = Own(new GameObject("MatrixNI7Wallet"));
            root.SetActive(false);
            root.AddComponent<NetworkIdentity>().sceneId = 0;
            Money wallet = root.AddComponent<Money>();
            wallet.isNetworked = true;
            Audit20261023TestPlayNetworkWorld.Configure(wallet, "_setInstanceOnAwake", false);
            Audit20261023TestPlayNetworkWorld.Configure(wallet, "_moneySave", "matrix-ni7-" + _options.RunId);
            Audit20261023TestPlayNetworkWorld.Configure(wallet, "_persistMoney", true);
            _precisionProbe = root.AddComponent<Audit20261023TestPlayNetworkMoneyProbe>();
            _precisionProbe.Wallet = wallet;
            _precisionProbe.Driver = this;
            _precisionProbe.PersistenceFile = Path.Combine(_options.SaveRoot, "matrix-save.json");
            // Keep ServerOnly until both clients' Money.Start requests crossed their ingress fences.
            root.SetActive(true);
            return root;
        }

        private void PreparePrecisionWallet(string caseId)
        {
            Money wallet = _precisionProbe.Wallet;
            wallet.AllMoney.Value = 123f;
            wallet.LevelMoney.Value = 7f;
            wallet.LastChangeMoney.Value = 17f;
            wallet.ClientMutationAuthority = NetworkAuthorityMode.None;
            SaveProvider.SetFloat(wallet.SaveKey, wallet.money);
            SaveProvider.SetFloat(wallet.SaveKey + nameof(Money.AllMoney), wallet.allMoney);
            FlushPrecisionWallet(caseId);
        }

        private void FlushPrecisionWallet(string caseId)
        {
            Money wallet = _precisionProbe.Wallet;
            SaveProvider.Save();
            FileSaveProvider reload = new FileSaveProvider("matrix-save.json",
                new FileSaveProviderOptions { PersistenceRoot = _options.SaveRoot });
            Require(reload.GetFloat(wallet.SaveKey, float.NaN) == wallet.money
                && reload.GetFloat(wallet.SaveKey + nameof(Money.AllMoney), float.NaN) == wallet.allMoney,
                "NI7 task-owned persisted current/lifetime did not round-trip.");
            _artifacts.Write("money-persistence-reload", "Actual isolated file reload matches the observed wallet.",
                step: _step, caseId: caseId, state: _world.Capture(_playerA, _playerB), epoch: _epoch);
        }

        private IEnumerator MoneyPrecisionCases()
        {
            GameObject walletObject = CreatePrecisionWallet();
            NetworkServer.Spawn(walletObject, PrecisionWalletAssetId);
            yield return Until(() => _precisionProbe.StartCompleted, "NI7 server wallet Start");
            _precisionProbe.Wallet.SetMoney(100000000f);
            yield return ExecutePeers("money-ni7-prepare", "precision-prepare", 0, "precision-prepare", 0);
            PreparePrecisionWallet("money-ni7-prepare");
            Require(_precisionProbe.Wallet.ClientMutationAuthority == NetworkAuthorityMode.None
                && _peers["client-a"].Connection.isAuthenticated && NetworkServer.localConnection == null,
                "NI7 requires an authorized cooperative remote client and dedicated server.");

            const string denied = "money-ni7-zero-debit-denied";
            yield return ResetEvents(denied);
            yield return ExecutePeers(denied, "precision-denied", 1);
            AssertPrecisionRequests(denied, 100000000f, 1f);
            SetPrecisionExpected(100000000f, 17f, 100000000f);
            yield return ObservePeers(denied, MatrixChecks.Money | MatrixChecks.PrecisionMoney
                | MatrixChecks.PrecisionPersistenceStable,
                PrecisionEvents(Array.Empty<float>(), Array.Empty<float>(), Array.Empty<float>(), new[] { 1f }, Array.Empty<float>()),
                PrecisionEvents(), PrecisionEvents());

            const string rounded = "money-ni7-rounded-debit-budget";
            yield return ResetEvents(rounded);
            yield return ExecutePeers(rounded, "precision-stale-pair", 0);
            AssertPrecisionRequests(rounded, 0f, 1f, 5f);
            MatrixPrecisionWallet ingress = CapturePrecisionWallet();
            Require(ingress.ingressFrames.Length == 2 && ingress.ingressTimes.Length == 2
                && ingress.ingressFrames[0] == ingress.ingressFrames[1]
                && ingress.ingressTimes[1] - ingress.ingressTimes[0] < 0.05d,
                "NI7 denied1/valid5 did not traverse the same server budget window.");
            SetPrecisionExpected(99999992f, -5f, 100000000f);
            yield return ObservePeers(rounded, MatrixChecks.Money | MatrixChecks.PrecisionMoney,
                PrecisionEvents(new[] { 99999992f }, new[] { -5f }, new[] { 99999992f }, new[] { 1f, 5f }, new[] { 5f, 5f }),
                PrecisionEvents(new[] { 0f, 100000000f, 99999992f }, new[] { -5f }, new[] { 99999992f }, null, new[] { 5f }),
                PrecisionEvents(new[] { 99999992f }, new[] { -5f }, new[] { 99999992f }, null, new[] { 5f }));
            yield return ExecutePeers(rounded, "precision-flush", 0, "precision-flush", 0);
            FlushPrecisionWallet(rounded);

            // Public reseed of the additional wallet; the original Money213 wallet is untouched.
            _precisionProbe.Wallet.SetMoney(10.5f);
            yield return ExecutePeers("money-ni7-fractional-prepare", "precision-prepare-fractional", 0,
                "precision-prepare-fractional", 0);
            PreparePrecisionWallet("money-ni7-fractional-prepare");
            yield return Until(() => Time.unscaledTimeAsDouble - _precisionProbe.LastSpendIngressTime >= 0.051d,
                "NI7 measured next eligible Money budget boundary");
            const string fractional = "money-ni7-fractional-control";
            yield return ResetEvents(fractional);
            yield return ExecutePeers(fractional, "precision-fractional", 0);
            AssertPrecisionRequests(fractional, 10.5f, 0.25f);
            SetPrecisionExpected(10.25f, -0.25f, 10.5f);
            yield return ObservePeers(fractional, MatrixChecks.Money | MatrixChecks.PrecisionMoney,
                PrecisionEvents(new[] { 10.25f }, new[] { -0.25f }, new[] { 10.25f }, new[] { 0.25f }, new[] { 0.25f, 0.25f }),
                PrecisionEvents(new[] { 10.25f }, new[] { -0.25f }, new[] { 10.25f }, null, new[] { 0.25f }),
                PrecisionEvents(new[] { 10.25f }, new[] { -0.25f }, new[] { 10.25f }, null, new[] { 0.25f }));
            yield return ExecutePeers(fractional, "precision-flush", 0, "precision-flush", 0);
            FlushPrecisionWallet(fractional);
            NetworkServer.Destroy(walletObject);
            yield return Until(() => _precisionProbe == null, "NI7 additional server wallet released");
            yield return ExecutePeers("money-ni7-release", "precision-released", 0, "precision-released", 0);
            _expectedState.precision = null;
            yield return ObservePeers("money-ni7-original-wallet-preserved", MatrixChecks.Money);
        }

        private IEnumerator ExecutePrecision(MatrixInstruction instruction, int step, MatrixReply response)
        {
            if (instruction.action == "precision-released")
            {
                yield return Until(() => _precisionProbe == null, "NI7 additional client wallet released");
                yield break;
            }
            yield return Until(() => _precisionProbe != null && _precisionProbe.StartCompleted, "NI7 remote wallet Start");
            Money wallet = _precisionProbe.Wallet;
            if (instruction.action == "precision-prepare" || instruction.action == "precision-prepare-fractional")
            {
                float wanted = instruction.action == "precision-prepare" ? 100000000f : 10.5f;
                yield return Until(() => wallet.money == wanted, "NI7 real public SetMoney RPC received");
                PreparePrecisionWallet(instruction.caseId);
                yield break;
            }
            if (instruction.action == "precision-flush")
            {
                FlushPrecisionWallet(instruction.caseId);
                yield break;
            }
            List<MatrixSpendRequest> requests = new List<MatrixSpendRequest>();
            if (instruction.action == "precision-denied") requests.Add(RequestPrecisionSpend(wallet, 1f));
            else if (instruction.action == "precision-stale-pair")
            {
                // Public fixture setup makes affordability stale. Restore before yielding so the
                // existing delta-based RPC can be observed independently of stale snapshot repair.
                wallet.CurrentMoney.Value = 0f;
                requests.Add(RequestPrecisionSpend(wallet, 1f));
                requests.Add(RequestPrecisionSpend(wallet, 5f));
                Require(wallet.money == 0f, "Pending client requests mutated the stale local balance.");
                wallet.CurrentMoney.Value = 100000000f;
            }
            else if (instruction.action == "precision-fractional") requests.Add(RequestPrecisionSpend(wallet, 0.25f));
            else throw new InvalidOperationException("Unknown NI7 action: " + instruction.action);
            response.spendRequests = requests.ToArray();
            _artifacts.Write("money-request-status", "Return values from actual pure-client TrySpend; no server outcome is implied.",
                step: step, caseId: instruction.caseId, state: _world.Capture(_playerA, _playerB), epoch: _epoch,
                spendRequests: response.spendRequests);
        }

        private static MatrixSpendRequest RequestPrecisionSpend(Money wallet, float amount)
        {
            MoneySpendResult result = wallet.TrySpend(amount);
            Require(result.Status == MoneySpendStatus.RequestedServerAuthority && result.IsPendingServerAuthority
                && !result.IsConfirmed, "Pure-client TrySpend did not return pending before local affordability/precision checks.");
            return new MatrixSpendRequest
            {
                amount = result.Amount, status = result.Status.ToString(), isConfirmed = result.IsConfirmed,
                isPendingServerAuthority = result.IsPendingServerAuthority,
                balanceBefore = result.BalanceBefore, balanceAfter = result.BalanceAfter
            };
        }

        private void AssertPrecisionRequests(string caseId, float before, params float[] amounts)
        {
            MatrixSpendRequest[] requests = Response("client-a").spendRequests;
            Require(requests.Length == amounts.Length, caseId + ": missing returned client status.");
            for (int i = 0; i < amounts.Length; i++)
                Require(requests[i].amount == amounts[i] && requests[i].status == "RequestedServerAuthority"
                    && requests[i].isPendingServerAuthority && !requests[i].isConfirmed
                    && requests[i].balanceBefore == before && requests[i].balanceAfter == before,
                    caseId + ": returned client status/snapshot differs from the actual public call.");
        }

        private void SetPrecisionExpected(float current, float lastChange, float phaseBefore)
        {
            _expectedState.precision = new MatrixPrecisionWallet
            {
                current = current, lifetime = 123f, level = 7f, lastChange = lastChange,
                storedCurrent = current, storedLifetime = 123f,
                phaseBalanceBefore = phaseBefore, storedDebit = phaseBefore - current
            };
        }

        private static MatrixEvents[] PrecisionEvents(float[] current = null, float[] last = null,
            float[] stored = null, float[] commands = null, float[] rpcs = null)
        {
            current ??= Array.Empty<float>();
            last ??= Array.Empty<float>();
            stored ??= Array.Empty<float>();
            commands ??= Array.Empty<float>();
            rpcs ??= Array.Empty<float>();
            float[] lifetimeWrites = new float[stored.Length];
            float[] commandOps = new float[commands.Length];
            float[] rpcOps = new float[rpcs.Length];
            for (int i = 0; i < lifetimeWrites.Length; i++) lifetimeWrites[i] = 123f;
            for (int i = 0; i < commandOps.Length; i++) commandOps[i] = 1f;
            for (int i = 0; i < rpcOps.Length; i++) rpcOps[i] = 1f;
            return new[]
            {
                new MatrixEvents { name = "ni7-current", values = current },
                new MatrixEvents { name = "ni7-last-change", values = last },
                new MatrixEvents { name = "ni7-lifetime" }, new MatrixEvents { name = "ni7-level" },
                new MatrixEvents { name = "ni7-persist-current", values = stored },
                new MatrixEvents { name = "ni7-persist-lifetime", values = lifetimeWrites },
                new MatrixEvents { name = "ni7-save" },
                new MatrixEvents { name = "ni7-command-op", values = commandOps },
                new MatrixEvents { name = "ni7-command-amount", values = commands },
                new MatrixEvents { name = "ni7-rpc-op", values = rpcOps },
                new MatrixEvents { name = "ni7-rpc-amount", values = rpcs }
            };
        }
    }
}
#endif
