#if MIRROR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mirror;
using Neo.Network;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     Focused server-core and initial-state replay regressions, not remote transport evidence.
    ///     The independent three-process matrix owns actual Command/Weaver/receiver acceptance.
    /// </summary>
    public sealed class Audit20261023NeoToolsViewNetworkTests
    {
        private readonly List<GameObject> _owned = new();
        private Transport _previousTransport;
        private DummyTransport _transport;
        private Random.State _previousRandom;
        private NetworkConnectionToClient _peerA;
        private NetworkConnectionToClient _peerB;
        private bool _ownsServer;

        [SetUp]
        public void SetUp()
        {
            _ownsServer = false;
            Assert.That(NetworkServer.active || NetworkClient.active, Is.False,
                "These regressions require the isolated, exclusive network runner.");
            Assert.That(NetworkManager.singleton == null, Is.True,
                "Do not borrow an application's network manager.");
            _previousTransport = Transport.active;
            _previousRandom = Random.state;
            GameObject transportObject = new("Audit20261023NeoToolsView.Transport");
            _owned.Add(transportObject);
            _transport = transportObject.AddComponent<DummyTransport>();
            Transport.active = _transport;
            _ownsServer = true;
            NetworkServer.Listen(2);
            _peerA = new NetworkConnectionToClient(6101);
            _peerB = new NetworkConnectionToClient(6102);
        }

        [TearDown]
        public void TearDown()
        {
            bool stillOwned = _ownsServer && Transport.active == _transport;
            try
            {
                if (_ownsServer)
                {
                    Assert.That(stillOwned, Is.True, "A replaced transport is not owned by this fixture.");
                    NetworkServer.Shutdown();
                }
            }
            finally
            {
                for (int i = _owned.Count - 1; i >= 0; i--)
                {
                    if (_owned[i] != null)
                    {
                        Object.DestroyImmediate(_owned[i]);
                    }
                }

                _owned.Clear();
                if (stillOwned)
                {
                    Transport.active = _previousTransport;
                    Random.state = _previousRandom;
                }

                _peerA = null;
                _peerB = null;
                _ownsServer = false;
            }
        }

        [Test]
        public void DeniedPeerB_ThenImmediateOwnerA_AdditiveRequestPreservesBudgetAndPayload()
        {
            Audit20261023NeoToolsViewAdmissionSelector selector = CreateSelector();
            selector.AuthorityMode = NetworkAuthorityMode.OwnerOnly;
            selector.ToggleIndex(2, true);
            selector.SetExcludedIndices(new[] { 0, 1, 2 });
            List<int> values = new();
            selector.OnSelectionChanged.AddListener(values.Add);

            Assert.That(selector.ReceiveSelection(1, false, true, _peerB), Is.False);
            Assert.That(selector.Value, Is.Zero);
            Assert.That(ActiveIndices(selector), Is.EqualTo(new[] { 0, 2 }));
            Assert.That(values, Is.Empty);

            Assert.That(selector.ReceiveRandom(false, _peerA), Is.True);
            Assert.That(selector.Value, Is.EqualTo(3));
            Assert.That(ActiveIndices(selector), Is.EqualTo(new[] { 0, 2, 3 }));
            Assert.That(selector.ExcludedIndices, Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(values, Is.EqualTo(new[] { 3 }));

            Assert.That(selector.ReceiveSelection(2, false, true, _peerA), Is.False,
                "A repeated accepted caller must retain its ordinary cooldown.");
            selector.AuthorityMode = NetworkAuthorityMode.None;
            Assert.That(selector.ReceiveSelection(1, false, true, _peerB), Is.True,
                "B's denied request must not consume B's own budget either.");
            Assert.That(ActiveIndices(selector), Is.EqualTo(new[] { 1 }));
            Assert.That(values, Is.EqualTo(new[] { 3, 1 }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LocalOnlyQueuedRequest_ThenImmediateSamePeerValidRequest_RetainsBudget(bool randomRequest)
        {
            Audit20261023NeoToolsViewAdmissionSelector selector = CreateSelector();
            selector.SetExcludedIndices(new[] { 0, 1, 2 });
            List<int> values = new();
            selector.OnSelectionChanged.AddListener(values.Add);
            selector.isNetworked = false;

            bool rejected = randomRequest
                ? selector.ReceiveRandom(false, _peerA)
                : selector.ReceiveSelection(2, false, true, _peerA);
            Assert.That(rejected, Is.False);
            Assert.That(ActiveIndices(selector), Is.EqualTo(new[] { 0 }));
            Assert.That(values, Is.Empty);

            selector.isNetworked = true;
            Assert.That(selector.ReceiveRandom(false, _peerA), Is.True);
            Assert.That(selector.Value, Is.EqualTo(3));
            Assert.That(ActiveIndices(selector), Is.EqualTo(new[] { 0, 3 }));
            Assert.That(values, Is.EqualTo(new[] { 3 }));
        }

        [Test]
        public void AuthorizedPeers_HaveIndependentBudgets_AndSamePeerRepeatIsRejected()
        {
            Audit20261023NeoToolsViewAdmissionSelector selector = CreateSelector();
            List<int> values = new();
            selector.OnSelectionChanged.AddListener(values.Add);

            Assert.That(selector.ReceiveSelection(1, false, true, _peerA), Is.True);
            Assert.That(selector.ReceiveSelection(2, false, true, _peerA), Is.False);
            Assert.That(selector.ReceiveSelection(2, false, true, _peerB), Is.True);
            Assert.That(selector.ReceiveSelection(3, false, true, _peerB), Is.False);

            Assert.That(values, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(ActiveIndices(selector), Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void InvalidSelection_ThenImmediateValidSelection_DoesNotConsumeBudget()
        {
            Audit20261023NeoToolsViewAdmissionSelector selector = CreateSelector();
            List<int> values = new();
            selector.OnSelectionChanged.AddListener(values.Add);

            Assert.That(selector.ReceiveSelection(int.MaxValue, false, true, _peerA), Is.False);
            Assert.That(values, Is.Empty);
            Assert.That(selector.ReceiveSelection(2, false, true, _peerA), Is.True);

            Assert.That(values, Is.EqualTo(new[] { 2 }));
            Assert.That(ActiveIndices(selector), Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void DisabledRandom_ThenImmediateValidSelection_DoesNotConsumeBudget()
        {
            Audit20261023NeoToolsViewAdmissionSelector selector = CreateSelector(randomEnabled: false);
            List<int> values = new();
            selector.OnSelectionChanged.AddListener(values.Add);

            Assert.That(selector.ReceiveRandom(false, _peerA), Is.False);
            Assert.That(selector.ReceiveSelection(2, false, true, _peerA), Is.True);

            Assert.That(values, Is.EqualTo(new[] { 2 }));
            Assert.That(ActiveIndices(selector), Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void UnknownPolicy_ThenImmediateKnownPolicy_DoesNotConsumeBudget()
        {
            Audit20261023NeoToolsViewAdmissionSelector selector = CreateSelector();
            selector.AuthorityMode = (NetworkAuthorityMode)12345;
            List<int> values = new();
            selector.OnSelectionChanged.AddListener(values.Add);

            Assert.That(selector.ReceiveSelection(1, false, true, _peerA), Is.False);
            Assert.That(selector.ReceiveRandom(false, _peerB), Is.False);
            Assert.That(selector.ReceiveSelection(1, false, true, null), Is.False);
            Assert.That(values, Is.Empty);
            selector.AuthorityMode = NetworkAuthorityMode.None;
            Assert.That(selector.ReceiveSelection(2, false, true, _peerA), Is.True);
            Assert.That(selector.ReceiveSelection(3, false, true, _peerB), Is.True);
            Assert.That(selector.ReceiveSelection(1, false, true, null), Is.True);
            Assert.That(values, Is.EqualTo(new[] { 2, 3, 1 }));
        }

        [TestCase(NetworkAuthorityMode.None)]
        [TestCase(NetworkAuthorityMode.ServerOnly)]
        [TestCase(NetworkAuthorityMode.OwnerOnly)]
        public void KnownPolicies_KeepTrustedNullServerCaller(NetworkAuthorityMode policy)
        {
            Audit20261023NeoToolsViewAdmissionSelector selector = CreateSelector();
            selector.AuthorityMode = policy;

            Assert.That(selector.ReceiveSelection(2, false, true, null), Is.True);
            Assert.That(selector.Value, Is.EqualTo(2));
            Assert.That(ActiveIndices(selector), Is.EqualTo(new[] { 2 }));
            Assert.That(selector.ReceiveSelection(3, false, true, null), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InitialSnapshot_AllSelectorCallbacksObserveFinalActiveAndExcludedSets(bool notifyItems)
        {
            Audit20261023NeoToolsViewAdmissionSelector selector = CreateSelector(notifyItems: notifyItems);
            foreach (GameObject item in selector.Items)
            {
                if (notifyItems)
                {
                    item.GetComponent<SelectorItem>().SetActive(false);
                }
                else
                {
                    item.SetActive(false);
                }
            }

            // These are incoming SyncVar payloads, not invocation of a private RPC/Command body.
            SetField(selector, "_syncIndex", 2);
            SetField(selector, "_syncFillMode", false);
            SetField(selector, "_syncDeactivateNonSelected", false);
            SetField(selector, "_syncActiveIndices", "0,2");
            SetField(selector, "_syncExcludedIndices", "1,3");
            List<string> order = new();
            selector.OnSelectionChanged.AddListener(value =>
            {
                order.Add("selection");
                Assert.That(value, Is.EqualTo(2));
                AssertSnapshot(selector);
            });
            selector.OnSelectionChangedGameObject.AddListener(item =>
            {
                order.Add("object");
                Assert.That(item, Is.SameAs(selector.Items[2]));
                AssertSnapshot(selector);
            });
            selector.OnCountActiveChanged.AddListener(count =>
            {
                order.Add("count");
                Assert.That(count, Is.EqualTo(2));
                AssertSnapshot(selector);
            });

            selector.ReplayInitialState();

            AssertSnapshot(selector);
            Assert.That(order, Is.EqualTo(new[] { "selection", "object", "count" }));
        }

        [Test]
        public void LocalFillSelection_PreservesSelectionObjectCountEventOrder()
        {
            Audit20261023NeoToolsViewAdmissionSelector selector = CreateSelector();
            selector.FillMode = true;
            List<string> order = new();
            selector.OnSelectionChanged.AddListener(_ => order.Add("selection"));
            selector.OnSelectionChangedGameObject.AddListener(_ => order.Add("object"));
            selector.OnCountActiveChanged.AddListener(_ => order.Add("count"));

            selector.Set(2);

            Assert.That(ActiveIndices(selector), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(order, Is.EqualTo(new[] { "selection", "object", "count" }));
        }

        private Audit20261023NeoToolsViewAdmissionSelector CreateSelector(bool randomEnabled = true,
            bool notifyItems = false)
        {
            GameObject root = new("Audit20261023NeoToolsView.NetworkSelector");
            _owned.Add(root);
            root.SetActive(false);
            GameObject[] items = new GameObject[4];
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = new GameObject("Item" + i);
                items[i].transform.SetParent(root.transform);
                if (notifyItems)
                {
                    items[i].AddComponent<SelectorItem>();
                }
            }

            root.AddComponent<NetworkIdentity>();
            Audit20261023NeoToolsViewAdmissionSelector selector =
                root.AddComponent<Audit20261023NeoToolsViewAdmissionSelector>();
            selector.startOnAwake = false;
            SetField(selector, "_autoUpdateFromChildren", false);
            SetField(selector, "_items", items);
            SetField(selector, "_useRandomSelection", randomEnabled);
            SetField(selector, "_notifySelectorItemsOnly", notifyItems);
            root.SetActive(true);
            NetworkServer.Spawn(root, 0x6100u + (uint)_owned.Count, _peerA);
            selector.isNetworked = true;
            selector.Set(0);
            return selector;
        }

        private static int[] ActiveIndices(Selector selector)
        {
            return Enumerable.Range(0, selector.Items.Length).Where(index =>
                selector.NotifySelectorItemsOnly
                    ? selector.Items[index].GetComponent<SelectorItem>().ActiveValue
                    : selector.Items[index].activeSelf).ToArray();
        }

        private static void AssertSnapshot(Selector selector)
        {
            Assert.That(selector.Value, Is.EqualTo(2));
            Assert.That(selector.CountActive, Is.EqualTo(2));
            Assert.That(ActiveIndices(selector), Is.EqualTo(new[] { 0, 2 }));
            Assert.That(selector.ExcludedIndices, Is.EqualTo(new[] { 1, 3 }));
        }

        private static void SetField(Selector selector, string name, object value)
        {
            typeof(Selector).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(selector, value);
        }
    }

    public sealed class Audit20261023NeoToolsViewAdmissionSelector : Selector
    {
        protected override float NetworkRateLimit => 1000f;

        public bool ReceiveSelection(int index, bool fillMode, bool deactivateOthers, NetworkConnectionToClient sender)
        {
            return TryApplyNetworkSelectionRequest(index, fillMode, deactivateOthers, sender);
        }

        public bool ReceiveRandom(bool deactivateOthers, NetworkConnectionToClient sender)
        {
            return TryApplyNetworkRandomRequest(deactivateOthers, sender);
        }

        public void ReplayInitialState()
        {
            ApplyNetworkState();
        }
    }
}
#endif
