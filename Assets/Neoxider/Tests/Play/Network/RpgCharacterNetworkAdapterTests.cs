#if MIRROR
using System.Collections;
using System.Reflection;
using Mirror;
using Neo.Network;
using Neo.Rpg;
using Neo.Rpg.Components;
using Neo.Rpg.Network;
using Neo.Rpg.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     Bounded host-mode smoke coverage for the optional RPG/Mirror adapter boundary. These tests
    ///     deliberately avoid a second process: they cover the authoritative host path and replay the
    ///     serialized late-join seam directly.
    /// </summary>
    public sealed class RpgCharacterNetworkAdapterTests
    {
        private NetworkTestSession _session;
        private const float HostStartupTimeoutSeconds = 3f;

        private GameObject _managerObject;
        private GameObject _playerPrefab;
        private TestNetworkManager _networkManager;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _session = null;
            _session = new NetworkTestSession();
            _networkManager = _session.OwnManager(NetworkTestHelper.CreateTestNetworkManager(
                "NetworkManagerRpgAdapterTest", out _managerObject));
            _playerPrefab = _session.Own(new GameObject("RpgAdapterDummyPlayer"));
            NetworkIdentity playerIdentity = _playerPrefab.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(playerIdentity, 88301);
            _networkManager.playerPrefab = _playerPrefab;

            yield return null;

            _networkManager.StartHost();
            float deadline = Time.realtimeSinceStartup + HostStartupTimeoutSeconds;
            while ((!NetworkServer.active || !NetworkClient.isConnected)
                   && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(NetworkServer.active, Is.True, "Mirror test host did not start before the timeout.");
            Assert.That(NetworkClient.isConnected, Is.True, "Mirror test client did not connect before the timeout.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_session != null)
            {
                yield return _session.Cleanup();
                _session = null;
            }
        }

        [UnityTest]
        public IEnumerator Adapter_IsOptionalContractAndHostDamageExecutesExactlyOnce()
        {
            GameObject characterObject = new("RpgAdapterHostCharacter");
            characterObject.SetActive(false);
            RpgCharacterTemplate template = CreateTemplate("Crystal;Mana=1/2|late:join");

            try
            {
                RpgCharacter character = characterObject.AddComponent<RpgCharacter>();
                RpgCharacterNetworkAdapter adapter = characterObject.AddComponent<RpgCharacterNetworkAdapter>();
                character.isNetworked = true;
                adapter.isNetworked = true;
                characterObject.SetActive(true);
                Assert.That(adapter.netIdentity, Is.Not.Null);
                Assert.That(adapter.netIdentity, Is.SameAs(characterObject.GetComponent<NetworkIdentity>()),
                    "Native Awake must admit Mirror's identity cache before woven SyncVar access.");
                character.ApplyTemplate(template);

                int damageEventCount = 0;
                character.OnDamagedEvent.AddListener(_ => damageEventCount++);

                Assert.That(adapter, Is.InstanceOf<NetworkBehaviour>());
                Assert.That(adapter, Is.InstanceOf<IRpgCharacterNetworkAdapter>());
                Assert.That(NeoNetworkState.IsHost, Is.True);

                adapter.NetDamage(25f);
                yield return null;

                Assert.That(character.HpValue, Is.EqualTo(75f));
                Assert.That(damageEventCount, Is.EqualTo(1),
                    "A host-authoritative mutation must execute locally exactly once.");
            }
            finally
            {
                Object.DestroyImmediate(template);
                Object.DestroyImmediate(characterObject);
            }
        }

        [UnityTest]
        public IEnumerator Adapter_RateLimiterRejectsImmediateSecondCommand()
        {
            GameObject characterObject = new("RpgAdapterRateLimitCharacter");
            characterObject.SetActive(false);
            try
            {
                characterObject.AddComponent<RpgCharacter>();
                RpgCharacterNetworkAdapter adapter = characterObject.AddComponent<RpgCharacterNetworkAdapter>();
                characterObject.SetActive(true);
                Assert.That(adapter.netIdentity, Is.Not.Null);
                Assert.That(adapter.netIdentity, Is.SameAs(characterObject.GetComponent<NetworkIdentity>()));
                MethodInfo rateLimitCheck = typeof(NeoNetworkComponent).GetMethod(
                    "RateLimitCheck", BindingFlags.Instance | BindingFlags.NonPublic, null,
                    System.Type.EmptyTypes, null);

                Assert.That(rateLimitCheck, Is.Not.Null);
                bool firstRejected = (bool)rateLimitCheck.Invoke(adapter, null);
                bool secondRejected = (bool)rateLimitCheck.Invoke(adapter, null);

                Assert.That(firstRejected, Is.False);
                Assert.That(secondRejected, Is.True);
                yield return null;
            }
            finally
            {
                Object.DestroyImmediate(characterObject);
            }
        }

        [UnityTest]
        public IEnumerator Snapshot_CustomIdRoundTripsThroughLateJoinApplySeam()
        {
            const string customResourceId = "Crystal;Mana=1/2|late:join";
            GameObject sourceObject = new("RpgAdapterSnapshotSource");
            GameObject targetObject = new("RpgAdapterSnapshotTarget");
            sourceObject.SetActive(false);
            targetObject.SetActive(false);
            RpgCharacterTemplate template = CreateTemplate(customResourceId);

            try
            {
                RpgCharacter source = sourceObject.AddComponent<RpgCharacter>();
                RpgCharacterNetworkAdapter sourceAdapter = sourceObject.AddComponent<RpgCharacterNetworkAdapter>();
                source.isNetworked = true;
                sourceAdapter.isNetworked = true;
                sourceObject.SetActive(true);
                Assert.That(sourceAdapter.netIdentity, Is.Not.Null);
                Assert.That(sourceAdapter.netIdentity, Is.SameAs(sourceObject.GetComponent<NetworkIdentity>()));
                source.ApplyTemplate(template);

                RpgCharacter target = targetObject.AddComponent<RpgCharacter>();
                RpgCharacterNetworkAdapter targetAdapter = targetObject.AddComponent<RpgCharacterNetworkAdapter>();
                target.isNetworked = true;
                targetAdapter.isNetworked = true;
                targetObject.SetActive(true);
                Assert.That(targetAdapter.netIdentity, Is.Not.Null);
                Assert.That(targetAdapter.netIdentity, Is.SameAs(targetObject.GetComponent<NetworkIdentity>()));
                target.ApplyTemplate(template);

                Assert.That(source.Spend(customResourceId, 3f), Is.True);
                sourceAdapter.NotifyStateChanged();

                FieldInfo snapshotField = typeof(RpgCharacterNetworkAdapter).GetField(
                    "_syncSnapshot", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo applySnapshot = typeof(RpgCharacterNetworkAdapter).GetMethod(
                    "ApplySnapshot", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(snapshotField, Is.Not.Null);
                Assert.That(applySnapshot, Is.Not.Null);

                string snapshot = (string)snapshotField.GetValue(sourceAdapter);
                Assert.That(snapshot, Is.Not.Empty);
                applySnapshot.Invoke(targetAdapter, new object[] { snapshot });
                yield return null;

                Assert.That(target.GetResource(customResourceId), Is.EqualTo(7f));
                Assert.That(target.CaptureProfile().Resources,
                    Has.Some.Matches<RpgResourceSaveEntry>(entry => entry.Id == customResourceId));
            }
            finally
            {
                Object.DestroyImmediate(template);
                Object.DestroyImmediate(sourceObject);
                Object.DestroyImmediate(targetObject);
            }
        }

        private static RpgCharacterTemplate CreateTemplate(string customResourceId)
        {
            RpgCharacterTemplate template = ScriptableObject.CreateInstance<RpgCharacterTemplate>();
            template.resources = new[]
            {
                new RpgResourceDefinition
                {
                    id = new RpgStatId(RpgStatPreset.Hp),
                    startCurrent = 100f,
                    startMax = 100f,
                    restoreOnAwake = true,
                    restoreToFull = true
                },
                new RpgResourceDefinition
                {
                    id = new RpgStatId(customResourceId),
                    startCurrent = 10f,
                    startMax = 30f,
                    restoreOnAwake = true,
                    restoreToFull = false
                }
            };
            return template;
        }
    }
}
#endif
