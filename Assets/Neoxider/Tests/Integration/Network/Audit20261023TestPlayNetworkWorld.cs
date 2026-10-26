#if MIRROR && UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.Reflection;
using Mirror;
using Neo.Network;
using Neo.Reactive;
using Neo.Rpg;
using Neo.Rpg.Components;
using Neo.Rpg.Network;
using Neo.Shop;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Scripting;

namespace Neo.Tests.Network.Integration
{
    /// <summary>A long test-owned budget makes reject-before-budget probes independent of local RTT.</summary>
    public sealed class Audit20261023TestPlayNetworkRelay : NetworkActionRelay
    {
        protected override float NetworkRateLimit => 1000f;
    }

    public sealed class Audit20261023TestPlayNetworkSelector : Selector
    {
        protected override float NetworkRateLimit => 1000f;
    }

    [Preserve]
    public sealed class Audit20261023TestPlayNetworkSyncTarget : MonoBehaviour
    {
        public float RawValue = 3f;
        public int ReadCount;
        public ReactivePropertyFloat Reactive;

        [Preserve]
        public float PolledValue
        {
            get { ReadCount++; return RawValue; }
            set => RawValue = value;
        }
    }

    [DefaultExecutionOrder(10000)]
    public sealed class Audit20261023TestPlayNetworkWorld : NetworkBehaviour
    {
        internal const uint WorldAssetId = 1023100501;
        internal const uint PlayerAssetId = 1023100502;
        internal const string ResourceId = "Crystal;Mana=1/2|matrix";
        internal Counter Counter;
        internal Selector Selector;
        internal Money Money;
        internal RpgCharacter Character;
        internal RpgCharacterNetworkAdapter RpgAdapter;
        internal Audit20261023TestPlayNetworkSyncTarget SyncTarget;
        internal NetworkPropertySync PropertySync;
        internal NetworkReactiveSync ReactiveSync;
        internal NetworkContextActionRelay ContextRelay;
        internal PhysicsEvents3D PhysicsEvents;
        internal Vector3 TriggerPosition;
        internal bool StartCompleted;
        internal string LeasePayload;
        private RpgCharacterTemplate _template;
        private Audit20261023TestPlayNetworkPeerDriver _driver;
        private readonly Dictionary<string, List<float>> _events = new Dictionary<string, List<float>>();
        private readonly Dictionary<string, Audit20261023TestPlayNetworkRelay> _relays =
            new Dictionary<string, Audit20261023TestPlayNetworkRelay>();

        private void Start() => StartCompleted = true;

        private void OnDestroy()
        {
            if (_template != null) UnityEngine.Object.Destroy(_template);
        }

        internal void Record(string name, float value)
        {
            if (!_events.TryGetValue(name, out List<float> values))
            {
                values = new List<float>();
                _events.Add(name, values);
            }
            if (values.Count >= 64)
            {
                _driver.Fault("Unbounded/duplicate event stream: " + name);
                return;
            }
            values.Add(value);
        }

        internal bool HasPhysicsEvent(uint actorNetId) =>
            _events.TryGetValue("physics", out List<float> values) && values.Contains(actorNetId);

        internal void ClearEvents()
        {
            _events.Clear();
            _driver.ResetPrecisionPhase();
            _driver.ResetCounterInputPhase();
        }
        internal Audit20261023TestPlayNetworkRelay Relay(string key) => _relays[key];

        internal MatrixState Capture(uint playerA, uint playerB)
        {
            List<int> active = new List<int>();
            for (int i = 0; i < Selector.Items.Length; i++)
                if (Selector.Items[i].activeSelf) active.Add(i);
            List<int> excluded = new List<int>(Selector.ExcludedIndices);
            excluded.Sort();
            List<string> keys = new List<string>(_events.Keys);
            keys.Sort(StringComparer.Ordinal);
            List<MatrixEvents> samples = new List<MatrixEvents>();
            foreach (string key in keys)
                samples.Add(new MatrixEvents { name = key, values = _events[key].ToArray() });
            return new MatrixState
            {
                netId = netId,
                playerA = playerA,
                playerB = playerB,
                isOwned = isOwned,
                startCompleted = StartCompleted,
                counter = Counter.ValueInt,
                selector = Selector.Value,
                selectorFill = Selector.FillMode,
                activeIndices = active.ToArray(),
                excludedIndices = excluded.ToArray(),
                money = Money.money,
                hp = Character.HpValue,
                maxHp = Character.MaxHpValue,
                resource = Character.GetResource(ResourceId),
                property = SyncTarget.RawValue,
                reactiveExists = SyncTarget.Reactive != null,
                reactive = SyncTarget.Reactive == null ? 0f : SyncTarget.Reactive.CurrentValue,
                propertyReads = SyncTarget.ReadCount,
                bonusA = BonusActive(playerA),
                bonusB = BonusActive(playerB),
                leasePayload = LeasePayload,
                precision = _driver.CapturePrecisionWallet(),
                counterInput = _driver.CaptureCounterInput(),
                events = samples.ToArray()
            };
        }

        private static bool BonusActive(uint id)
        {
            Dictionary<uint, NetworkIdentity> identities = NetworkServer.active
                ? NetworkServer.spawned : NetworkClient.spawned;
            if (id == 0 || !identities.TryGetValue(id, out NetworkIdentity identity)) return false;
            Transform bonus = identity.transform.Find("Bonus");
            return bonus != null && bonus.gameObject.activeSelf;
        }

        internal static Audit20261023TestPlayNetworkWorld Create(Audit20261023TestPlayNetworkPeerDriver driver)
        {
            GameObject root = driver.Own(new GameObject("MatrixWorld"));
            root.SetActive(false);
            root.AddComponent<NetworkIdentity>().sceneId = 0;
            Audit20261023TestPlayNetworkWorld world = root.AddComponent<Audit20261023TestPlayNetworkWorld>();
            world._driver = driver;
            world.Counter = root.AddComponent<Counter>();
            world.Counter.isNetworked = true;
            Configure(world.Counter, "_saveEnabled", false);
            world.Counter.OnValueChangedInt.AddListener(value => world.Record("counter", value));

            GameObject selectorRoot = Child(root, "Selector");
            GameObject[] items = new GameObject[4];
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = Child(selectorRoot, "Item" + i);
                items[i].SetActive(false);
            }
            // OwnerOnly resolves the identity on the component's own GameObject.
            world.Selector = root.AddComponent<Audit20261023TestPlayNetworkSelector>();
            world.Selector.startOnAwake = false;
            world.Selector.isNetworked = true;
            Configure(world.Selector, "_autoUpdateFromChildren", false);
            Configure(world.Selector, "_items", items);
            Configure(world.Selector, "_useRandomSelection", true);
            Configure(world.Selector, "_saveEnabled", false);
            world.Selector.OnSelectionChanged.AddListener(value => world.Record("selector", value));

            world.Money = root.AddComponent<Money>();
            world.Money.isNetworked = true;
            Configure(world.Money, "_persistMoney", false);
            world.Money.CurrentMoney.AddListener(value => world.Record("money", value));

            world._template = driver.Own(ScriptableObject.CreateInstance<RpgCharacterTemplate>());
            world._template.resources = new[]
            {
                new RpgResourceDefinition
                {
                    id = new RpgStatId(RpgStatPreset.Hp),
                    startCurrent = driver.IsServer ? 120f : 9f, startMax = driver.IsServer ? 120f : 9f,
                    restoreOnAwake = true, restoreToFull = true
                },
                new RpgResourceDefinition
                {
                    id = new RpgStatId(ResourceId),
                    startCurrent = driver.IsServer ? 10f : 1f, startMax = driver.IsServer ? 30f : 3f,
                    restoreOnAwake = true, restoreToFull = false
                }
            };
            world.Character = root.AddComponent<RpgCharacter>();
            world.Character.isNetworked = true;
            world.Character.Template = world._template;
            Configure(world.Character, "_loadOnAwake", false);
            Configure(world.Character, "_autoSave", false);
            world.RpgAdapter = root.AddComponent<RpgCharacterNetworkAdapter>();
            world.RpgAdapter.isNetworked = true;
            world.Character.OnResourceChangedEvent.AddListener((id, value) =>
                world.Record(id == ResourceId ? "rpg-resource" : "rpg-hp", value));

            world.SyncTarget = root.AddComponent<Audit20261023TestPlayNetworkSyncTarget>();
            world.SyncTarget.Reactive = driver.IsServer ? null : new ReactivePropertyFloat();
            world.PropertySync = root.AddComponent<NetworkPropertySync>();
            world.PropertySync.isNetworked = true;
            Configure(world.PropertySync, "_targetComponent", world.SyncTarget);
            Configure(world.PropertySync, "_fieldName", nameof(Audit20261023TestPlayNetworkSyncTarget.PolledValue));
            Configure(world.PropertySync, "_syncInterval", 0.2f);
            world.PropertySync.onValueChanged.AddListener(() => world.Record("property", world.SyncTarget.RawValue));
            world.ReactiveSync = root.AddComponent<NetworkReactiveSync>();
            world.ReactiveSync.isNetworked = true;
            Configure(world.ReactiveSync, "_targetComponent", world.SyncTarget);
            Configure(world.ReactiveSync, "_fieldName", nameof(Audit20261023TestPlayNetworkSyncTarget.Reactive));
            Configure(world.ReactiveSync, "_syncInterval", 0.2f);

            foreach (string key in new[] { "owner", "unknown", "known", "local", "rate", "all", "server", "others" })
            {
                Audit20261023TestPlayNetworkRelay relay = root.AddComponent<Audit20261023TestPlayNetworkRelay>();
                relay.isNetworked = true;
                NetworkActionScope scope = key == "server" || key == "rate"
                    ? NetworkActionScope.ServerOnly
                    : key == "others" ? NetworkActionScope.OthersOnly : NetworkActionScope.AllClients;
                NetworkActionChannel channel = new NetworkActionChannel { channelName = key, scope = scope };
                string eventKey = key;
                channel.onTriggeredFloat.AddListener(value => world.Record(eventKey, value));
                Configure(relay, "_channels", new[] { channel });
                world._relays.Add(key, relay);
            }
            world.Relay("owner").AuthorityMode = NetworkAuthorityMode.OwnerOnly;

            world.ContextRelay = root.AddComponent<NetworkContextActionRelay>();
            world.ContextRelay.isNetworked = true;
            world.ContextRelay.ContextSource = NetworkContextSourceMode.EventArgument;
            world.ContextRelay.RootMode = NetworkContextRootMode.NetworkIdentityInParents;
            world.ContextRelay.TargetMode = NetworkContextTargetMode.ChildByName;
            world.ContextRelay.TargetChildName = "Bonus";
            world.ContextRelay.Action = NetworkContextActionType.SetActive;
            world.ContextRelay.ActionBoolValue = true;
            world.ContextRelay.Scope = NetworkActionScope.AllClients;
            world.ContextRelay.TriggerOnlyForLocalContext = true;

            GameObject trigger = Child(root, "Trigger");
            world.TriggerPosition = new Vector3(50f, 0f, 0f);
            trigger.transform.position = world.TriggerPosition;
            BoxCollider box = trigger.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = Vector3.one * 2f;
            box.enabled = driver.IsServer;
            world.PhysicsEvents = trigger.AddComponent<PhysicsEvents3D>();
            world.PhysicsEvents.isNetworked = true;
            world.PhysicsEvents.filterByLayer = false;
            world.PhysicsEvents.filterByTag = false;
            world.PhysicsEvents.onTriggerEnter.AddListener(collider =>
            {
                NetworkIdentity actor = collider == null ? null : collider.GetComponentInParent<NetworkIdentity>();
                world.Record("physics", actor == null ? 0 : actor.netId);
                if (driver.IsServer && collider != null) world.ContextRelay.Trigger(collider);
            });
            root.SetActive(true);
            return world;
        }

        internal static GameObject CreatePlayer(Audit20261023TestPlayNetworkPeerDriver driver, Vector3 position)
        {
            GameObject player = driver.Own(new GameObject("MatrixPlayer"));
            player.SetActive(false);
            player.transform.position = position;
            player.AddComponent<NetworkIdentity>().sceneId = 0;
            BoxCollider collider = player.AddComponent<BoxCollider>();
            collider.enabled = driver.IsServer;
            Rigidbody body = player.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            Child(player, "Bonus").SetActive(false);
            player.SetActive(true);
            return player;
        }

        private static GameObject Child(GameObject parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        /// <summary>Inspector-equivalent setup only; never invokes a private received method or Command body.</summary>
        internal static void Configure(object target, string fieldName, object value)
        {
            Type type = target.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic
                    | BindingFlags.Public | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }
                type = type.BaseType;
            }
            throw new MissingFieldException(target.GetType().FullName, fieldName);
        }
    }
}
#endif
