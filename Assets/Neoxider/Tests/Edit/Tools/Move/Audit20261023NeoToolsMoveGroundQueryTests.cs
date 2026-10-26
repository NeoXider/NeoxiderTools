using System.Collections.Generic;
using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoToolsMoveGroundQueryTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private readonly Vector3 _probe = new Vector3(2345f, 3456f, 4567f);

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
        }

        [TestCase(16)]
        [TestCase(48)]
        [TestCase(80)]
        public void SaturatedSelfHits_KeepCompleteOverlapAndFindForeignGround(int childCount)
        {
            PlayerController3DPhysics controller = CreateController(childCount);
            BoxCollider floor = CreateObject().AddComponent<BoxCollider>();
            floor.transform.position = _probe;
            int landed = 0;
            UnityEvent onLanded = new UnityEvent();
            onLanded.AddListener(() => landed++);
            SetField(controller, "_onLanded", onLanded);
            Physics.SyncTransforms();
            Collider[] expected = Physics.OverlapSphere(_probe, 0.5f, ~0, QueryTriggerInteraction.Ignore);
            Assert.That(expected.Length, Is.GreaterThan(16), "Fixture must saturate the original buffer.");

            InvokeGroundCheck(controller);

            Assert.That(controller.IsGrounded, Is.True);
            Assert.That(landed, Is.EqualTo(1));
            FieldInfo bufferField = typeof(PlayerController3DPhysics).GetField("_groundHits",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(bufferField, Is.Not.Null);
            Collider[] complete = (Collider[])bufferField.GetValue(controller);
            // The complete native query is an ordering-independent oracle, even if a truncated prefix happened to include floor.
            foreach (Collider expectedHit in expected)
            {
                CollectionAssert.Contains(complete, expectedHit);
            }

            InvokeGroundCheck(controller);
            Assert.That(landed, Is.EqualTo(1));
            floor.transform.position = _probe + Vector3.down * 100f;
            Physics.SyncTransforms();
            InvokeGroundCheck(controller);
            Assert.That(controller.IsGrounded, Is.False, "Unused retained buffer entries must not count as current hits.");
        }

        [Test]
        public void CompleteQuery_PreservesMaskAndIgnoresTriggers()
        {
            PlayerController3DPhysics controller = CreateController(32);
            foreach (Transform child in controller.transform)
            {
                child.gameObject.layer = 28;
            }

            BoxCollider floor = CreateObject().AddComponent<BoxCollider>();
            floor.gameObject.layer = 29;
            floor.transform.position = _probe;
            SetField(controller, "_groundMask", (LayerMask)(1 << 28));
            Physics.SyncTransforms();
            InvokeGroundCheck(controller);
            Assert.That(controller.IsGrounded, Is.False, "Out-of-mask floor is not ground.");

            SetField(controller, "_groundMask", (LayerMask)(1 << 29));
            floor.isTrigger = true;
            Physics.SyncTransforms();
            InvokeGroundCheck(controller);
            Assert.That(controller.IsGrounded, Is.False, "Ground queries retain Ignore trigger policy.");

            floor.isTrigger = false;
            Physics.SyncTransforms();
            InvokeGroundCheck(controller);
            Assert.That(controller.IsGrounded, Is.True);
        }

        private PlayerController3DPhysics CreateController(int childCount)
        {
            GameObject owner = CreateObject();
            owner.SetActive(false);
            owner.transform.position = _probe;
            PlayerController3DPhysics controller = owner.AddComponent<PlayerController3DPhysics>();
            SetField(controller, "_groundCheck", owner.transform);
            SetField(controller, "_groundProbeOffset", 0f);
            SetField(controller, "_groundCheckRadius", 0.5f);
            SetField(controller, "_logSetupWarnings", false);
            controller.CursorControlEnabled = false;
            owner.GetComponent<Rigidbody>().useGravity = false;
            for (int i = 0; i < childCount; i++)
            {
                GameObject child = CreateObject();
                child.transform.SetParent(owner.transform, false);
                SphereCollider collider = child.AddComponent<SphereCollider>();
                collider.radius = 0.1f;
            }

            owner.SetActive(true);
            return controller;
        }

        private GameObject CreateObject()
        {
            GameObject owner = new GameObject("Audit20261023NeoToolsMoveGroundQuery");
            _objects.Add(owner);
            return owner;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private static void InvokeGroundCheck(PlayerController3DPhysics controller)
        {
            MethodInfo method = typeof(PlayerController3DPhysics).GetMethod("UpdateGroundState",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(controller, null);
        }
    }
}
