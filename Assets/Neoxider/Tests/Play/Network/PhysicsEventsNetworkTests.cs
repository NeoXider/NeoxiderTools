using System;
using System.Collections;
#if MIRROR
using Mirror;
#endif
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    public class PhysicsEventsNetworkTests
    {
#if MIRROR
        private NetworkTestSession _session;
#endif
        private Scene _physicsScene;
        private GameObject _triggerObject;
        private GameObject _actorObject;
        private PhysicsEvents3D _events;
        private Rigidbody _actorBody;
        private BoxCollider _actorCollider;
        private int _enterCount;
        private int _exitCount;
        private Collider _entered;
        private Collider _exited;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _physicsScene = default;
            _triggerObject = null;
            _actorObject = null;
            _enterCount = 0;
            _exitCount = 0;
            _entered = null;
            _exited = null;
#if MIRROR
            _session = null;
            if (TestContext.CurrentContext.Test.MethodName != nameof(PhysicsEvents3D_NotNetworked_FiresLocally))
            {
                _session = new NetworkTestSession();
                TestNetworkManager manager = _session.OwnManager(NetworkTestHelper.CreateTestNetworkManager(
                    "PhysicsEventsNetworkManager", out GameObject managerObject));
                GameObject playerPrefab = _session.Own(new GameObject("DummyPlayer"));
                NetworkTestHelper.SetAssetId(playerPrefab.AddComponent<NetworkIdentity>(), 99999);
                manager.playerPrefab = playerPrefab;
                manager.StartHost();
                yield return NetworkTestHelper.WaitForHost(requireLocalPlayer: true);
            }
#endif
            _physicsScene = SceneManager.CreateScene("NeoPhysics-" + Guid.NewGuid().ToString("N"),
                new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            _triggerObject = new GameObject("Trigger");
            _triggerObject.SetActive(false);
            SceneManager.MoveGameObjectToScene(_triggerObject, _physicsScene);
#if MIRROR
            NetworkIdentity triggerIdentity = _triggerObject.AddComponent<NetworkIdentity>();
#endif
            BoxCollider triggerCollider = _triggerObject.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            _events = _triggerObject.AddComponent<PhysicsEvents3D>();
            _events.isNetworked = false;
            _events.filterByLayer = false;
            _events.filterByTag = false;
            _events.onTriggerEnter.AddListener(collider => { _enterCount++; _entered = collider; });
            _events.onTriggerExit.AddListener(collider => { _exitCount++; _exited = collider; });
            _triggerObject.SetActive(true);

            _actorObject = new GameObject("Actor");
            _actorObject.SetActive(false);
            SceneManager.MoveGameObjectToScene(_actorObject, _physicsScene);
            _actorObject.transform.position = Vector3.up * 5f;
#if MIRROR
            NetworkIdentity actorIdentity = _actorObject.AddComponent<NetworkIdentity>();
#endif
            _actorCollider = _actorObject.AddComponent<BoxCollider>();
            _actorBody = _actorObject.AddComponent<Rigidbody>();
            _actorBody.isKinematic = true;
            _actorBody.useGravity = false;
            _actorObject.SetActive(true);
#if MIRROR
            if (_session != null)
            {
                _session.Own(_triggerObject);
                _session.Own(_actorObject);
                NetworkTestHelper.SetAssetId(triggerIdentity, 11111);
                NetworkTestHelper.SetAssetId(actorIdentity, 11112);
                NetworkClient.RegisterPrefab(_triggerObject);
                NetworkClient.RegisterPrefab(_actorObject);
                NetworkServer.Spawn(_triggerObject);
                NetworkServer.Spawn(_actorObject);
                yield return NetworkTestHelper.WaitForSpawn(triggerIdentity);
                yield return NetworkTestHelper.WaitForSpawn(actorIdentity);
            }
#endif
            yield break;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            AsyncOperation unloading = null;
            try
            {
#if MIRROR
                if (_session != null)
                {
                    yield return _session.Cleanup();
                    _session = null;
                }
#endif
            }
            finally
            {
                if (_actorObject != null) UnityEngine.Object.DestroyImmediate(_actorObject);
                if (_triggerObject != null) UnityEngine.Object.DestroyImmediate(_triggerObject);
                if (_physicsScene.IsValid() && _physicsScene.isLoaded)
                {
                    unloading = SceneManager.UnloadSceneAsync(_physicsScene);
                }
            }

            if (unloading != null)
            {
                yield return WaitForNative(() => unloading.isDone, "owned local physics scene unload", 10f);
            }
        }

#if MIRROR
        [UnityTest]
        public IEnumerator PhysicsEvents3D_Networked_FiresIfServer()
        {
            yield return MoveThroughTrigger(true);
        }
#endif

        [UnityTest]
        public IEnumerator PhysicsEvents3D_NotNetworked_FiresLocally()
        {
            yield return MoveThroughTrigger(false);
        }

        private IEnumerator MoveThroughTrigger(bool networked)
        {
            _events.isNetworked = networked;
            PhysicsScene physics = _physicsScene.GetPhysicsScene();
            Assert.That(physics.IsValid(), Is.True);
            Assert.That(_events.GetComponent<BoxCollider>().isTrigger, Is.True);
            Assert.That(_actorBody.isKinematic, Is.True);
#if MIRROR
            using NetworkTestHelper.RpcObservation envelopes = new NetworkTestHelper.RpcObservation(
                _events.GetComponent<NetworkIdentity>(), requireSpawned: networked);
#endif
            _actorBody.position = Vector3.zero;
            physics.Simulate(0.02f);
            yield return WaitForNative(() => _enterCount > 0, "native trigger enter");
            _actorBody.position = Vector3.up * 5f;
            physics.Simulate(0.02f);
            yield return WaitForNative(() => _exitCount > 0, "native trigger exit");
            physics.Simulate(0.02f);
#if MIRROR
            if (_session != null)
            {
                yield return NetworkTestHelper.ReliableBarrier("physics event receive barrier");
            }

            if (networked)
            {
                Assert.That(envelopes.Sent, Is.GreaterThanOrEqualTo(2), "Enter and exit must produce RPC envelopes.");
                Assert.That(envelopes.Received, Is.EqualTo(envelopes.Sent));
            }
            else
            {
                Assert.That(envelopes.Sent, Is.Zero);
                Assert.That(envelopes.Received, Is.Zero);
            }
#endif
            Assert.That(_enterCount, Is.EqualTo(1));
            Assert.That(_exitCount, Is.EqualTo(1));
            Assert.That(_entered, Is.SameAs(_actorCollider));
            Assert.That(_exited, Is.SameAs(_actorCollider));
        }

        private static IEnumerator WaitForNative(Func<bool> condition, string operation, float timeoutSeconds = 5f)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + timeoutSeconds;
            while (!condition())
            {
                Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThan(deadline), "Timed out: " + operation);
                yield return null;
            }
        }
    }
}
