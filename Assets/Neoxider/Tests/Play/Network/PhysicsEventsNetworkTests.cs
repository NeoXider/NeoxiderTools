using System.Collections;
using Mirror;
using Neo.Network;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    public class PhysicsEventsNetworkTests
    {
        private GameObject _objTrigger;
        private PhysicsEvents3D _physicsEvents3D;
        private BoxCollider _triggerCollider;

        private GameObject _objActor;
        private BoxCollider _actorCollider;

        private bool _eventFired;

        private GameObject _managerObj;
        private NeoNetworkManager _networkManager;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _eventFired = false;

            // Setup NetworkManager
            _managerObj = new GameObject("NetworkManager");
            Transport transport = _managerObj.AddComponent<DummyTransport>();
            _networkManager = _managerObj.AddComponent<TestNetworkManager>();

            GameObject dummyPlayer = new GameObject("DummyPlayer");
            NetworkIdentity dummyId = dummyPlayer.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(dummyId, 99999);
            _networkManager.playerPrefab = dummyPlayer;

            Transport.active = transport;
            yield return null;

            _networkManager.StartHost();
            while (!NetworkServer.active || !NetworkClient.isConnected) yield return null;
            yield return null;

            _objTrigger = new GameObject("Trigger");
            _objTrigger.transform.position = Vector3.zero;

            _physicsEvents3D = _objTrigger.AddComponent<PhysicsEvents3D>();
            _physicsEvents3D.onTriggerEnter.AddListener((c) => _eventFired = true);
            _physicsEvents3D.filterByLayer = false;
            _physicsEvents3D.filterByTag = false;

            _triggerCollider = _objTrigger.AddComponent<BoxCollider>();
            _triggerCollider.isTrigger = true;

            NetworkIdentity identity = _objTrigger.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(identity, 11111);
            NetworkClient.RegisterPrefab(_objTrigger);
            NetworkServer.Spawn(_objTrigger);
            if (!NetworkClient.ready) NetworkClient.Ready();

            _objActor = new GameObject("Actor");
            _objActor.transform.position = Vector3.up * 5f; // Away from trigger
            _actorCollider = _objActor.AddComponent<BoxCollider>();
            
            // Add Rigidbody to actor so movement triggers physics engine natively
            var rb = _objActor.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_networkManager != null) _networkManager.StopHost();
            yield return null;

            if (_managerObj != null) Object.DestroyImmediate(_managerObj);
            if (_objTrigger != null) Object.DestroyImmediate(_objTrigger);
            if (_objActor != null) Object.DestroyImmediate(_objActor);
        }

        [UnityTest]
        public IEnumerator PhysicsEvents3D_Networked_FiresIfServer()
        {
            _physicsEvents3D.isNetworked = true;
            
            var m = typeof(PhysicsEvents3D).GetMethod("OnTriggerEnter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            m.Invoke(_physicsEvents3D, new object[] { _actorCollider });

            yield return new WaitForSeconds(0.1f);

            Assert.IsTrue(_eventFired, "Event should fire when isNetworked is true and we are the Server.");
        }

        [UnityTest]
        public IEnumerator PhysicsEvents3D_NotNetworked_FiresLocally()
        {
            _physicsEvents3D.isNetworked = false;
            
            var m = typeof(PhysicsEvents3D).GetMethod("OnTriggerEnter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            m.Invoke(_physicsEvents3D, new object[] { _actorCollider });

            yield return null;

            Assert.IsTrue(_eventFired, "Event should fire locally when isNetworked is false.");
        }
    }
}
