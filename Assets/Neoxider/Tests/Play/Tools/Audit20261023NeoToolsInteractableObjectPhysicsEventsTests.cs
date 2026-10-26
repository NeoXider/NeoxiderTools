using System.Collections;
using System.Collections.Generic;
using Neo.Network;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    public class Audit20261023NeoToolsInteractableObjectPhysicsEventsTests
    {
        public enum FilterCase
        {
            Allowed,
            Disabled,
            LayerRejected,
            TagRejected
        }

        private GameObject _owner;
        private GameObject _actor;

        [SetUp]
        public void SetUp()
        {
            Assert.That(NeoNetworkState.IsNetworkActive, Is.False, "This fixture requires an offline session.");
            _owner = new GameObject("AuditInteractionRelay");
            _actor = new GameObject("AuditInteractionActor");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_owner);
            Object.DestroyImmediate(_actor);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Trigger3D_Offline_EmitsAllPhasesAndColliderToBothEventSurfaces(bool networked)
        {
            PhysicsEvents3D relay = _owner.AddComponent<PhysicsEvents3D>();
            relay.isNetworked = networked;
            Collider collider = _actor.AddComponent<BoxCollider>();
            List<Collider> inspectorPayloads = new();
            List<Collider> codePayloads = new();
            relay.onTriggerEnter.AddListener(inspectorPayloads.Add);
            relay.onTriggerStay.AddListener(inspectorPayloads.Add);
            relay.onTriggerExit.AddListener(inspectorPayloads.Add);
            relay.TriggerEnterOccurred += codePayloads.Add;
            relay.TriggerStayOccurred += codePayloads.Add;
            relay.TriggerExitOccurred += codePayloads.Add;

            // Drive Unity's callback boundary without starting a physics simulation or network session.
            _owner.SendMessage("OnTriggerEnter", collider);
            _owner.SendMessage("OnTriggerStay", collider);
            _owner.SendMessage("OnTriggerExit", collider);

            Collider[] expected = { collider, collider, collider };
            Assert.That(inspectorPayloads, Is.EqualTo(expected));
            Assert.That(codePayloads, Is.EqualTo(expected));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Trigger2D_Offline_EmitsAllPhasesAndColliderToBothEventSurfaces(bool networked)
        {
            PhysicsEvents2D relay = _owner.AddComponent<PhysicsEvents2D>();
            relay.isNetworked = networked;
            Collider2D collider = _actor.AddComponent<BoxCollider2D>();
            List<Collider2D> inspectorPayloads = new();
            List<Collider2D> codePayloads = new();
            relay.onTriggerEnter.AddListener(inspectorPayloads.Add);
            relay.onTriggerStay.AddListener(inspectorPayloads.Add);
            relay.onTriggerExit.AddListener(inspectorPayloads.Add);
            relay.TriggerEnterOccurred += codePayloads.Add;
            relay.TriggerStayOccurred += codePayloads.Add;
            relay.TriggerExitOccurred += codePayloads.Add;

            _owner.SendMessage("OnTriggerEnter2D", collider);
            _owner.SendMessage("OnTriggerStay2D", collider);
            _owner.SendMessage("OnTriggerExit2D", collider);

            Collider2D[] expected = { collider, collider, collider };
            Assert.That(inspectorPayloads, Is.EqualTo(expected));
            Assert.That(codePayloads, Is.EqualTo(expected));
        }

        [TestCase(false, FilterCase.Allowed)]
        [TestCase(false, FilterCase.Disabled)]
        [TestCase(false, FilterCase.LayerRejected)]
        [TestCase(false, FilterCase.TagRejected)]
        [TestCase(true, FilterCase.Allowed)]
        [TestCase(true, FilterCase.Disabled)]
        [TestCase(true, FilterCase.LayerRejected)]
        [TestCase(true, FilterCase.TagRejected)]
        public void OfflineFallback_StillAppliesInteractableLayerAndTagFilters(bool twoDimensions, FilterCase filter)
        {
            int inspectorCalls = 0;
            int codeCalls = 0;
            if (twoDimensions)
            {
                PhysicsEvents2D relay = _owner.AddComponent<PhysicsEvents2D>();
                relay.isNetworked = true;
                relay.interactable = filter != FilterCase.Disabled;
                relay.layers = filter == FilterCase.LayerRejected ? 0 : ~0;
                relay.filterByTag = filter == FilterCase.TagRejected;
                relay.requiredTag = "Player";
                relay.onTriggerEnter.AddListener(collider => inspectorCalls++);
                relay.TriggerEnterOccurred += collider => codeCalls++;

                _owner.SendMessage("OnTriggerEnter2D", _actor.AddComponent<BoxCollider2D>());
            }
            else
            {
                PhysicsEvents3D relay = _owner.AddComponent<PhysicsEvents3D>();
                relay.isNetworked = true;
                relay.interactable = filter != FilterCase.Disabled;
                relay.layers = filter == FilterCase.LayerRejected ? 0 : ~0;
                relay.filterByTag = filter == FilterCase.TagRejected;
                relay.requiredTag = "Player";
                relay.onTriggerEnter.AddListener(collider => inspectorCalls++);
                relay.TriggerEnterOccurred += collider => codeCalls++;

                _owner.SendMessage("OnTriggerEnter", _actor.AddComponent<BoxCollider>());
            }

            int expected = filter == FilterCase.Allowed ? 1 : 0;
            Assert.That(inspectorCalls, Is.EqualTo(expected));
            Assert.That(codeCalls, Is.EqualTo(expected));
        }
    }
}
