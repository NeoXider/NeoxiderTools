using System.Collections.Generic;
using System.Linq;
using Neo.Extensions;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class UnityExtensionsTests
    {
        private readonly List<GameObject> _ownedObjects = new List<GameObject>();

        private GameObject CreateOwnedObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            _ownedObjects.Add(gameObject);
            return gameObject;
        }

        [TearDown]
        public void ReleaseOwnedObjects()
        {
            for (int index = _ownedObjects.Count - 1; index >= 0; index--)
                if (_ownedObjects[index] != null) Object.DestroyImmediate(_ownedObjects[index]);
            _ownedObjects.Clear();
        }

        #region TransformExtensions

        [Test]
        public void Transform_SetPosition_SetsXYZ()
        {
            GameObject go = CreateOwnedObject("T");
            go.transform.SetPosition(x: 5f, y: 10f, z: 15f);
            Assert.AreEqual(5f, go.transform.position.x, 0.001f);
            Assert.AreEqual(10f, go.transform.position.y, 0.001f);
            Assert.AreEqual(15f, go.transform.position.z, 0.001f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Transform_SetPosition_PartialOverride()
        {
            GameObject go = CreateOwnedObject("T");
            go.transform.position = new Vector3(1, 2, 3);
            go.transform.SetPosition(y: 99f);
            Assert.AreEqual(1f, go.transform.position.x, 0.001f);
            Assert.AreEqual(99f, go.transform.position.y, 0.001f);
            Assert.AreEqual(3f, go.transform.position.z, 0.001f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Transform_AddPosition_Additive()
        {
            GameObject go = CreateOwnedObject("T");
            go.transform.position = new Vector3(1, 2, 3);
            go.transform.AddPosition(x: 10f);
            Assert.AreEqual(11f, go.transform.position.x, 0.001f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Transform_SetScale()
        {
            GameObject go = CreateOwnedObject("T");
            go.transform.SetScale(x: 2f, y: 3f, z: 4f);
            Assert.AreEqual(new Vector3(2, 3, 4), go.transform.localScale);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Transform_ResetTransform()
        {
            GameObject go = CreateOwnedObject("T");
            go.transform.position = Vector3.one * 99;
            go.transform.localScale = Vector3.one * 5;
            go.transform.ResetTransform();
            Assert.AreEqual(Vector3.zero, go.transform.position);
            Assert.AreEqual(Vector3.one, go.transform.localScale);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Transform_GetClosest()
        {
            GameObject origin = CreateOwnedObject("Origin");
            origin.transform.position = Vector3.zero;

            GameObject near = CreateOwnedObject("Near");
            near.transform.position = new Vector3(1, 0, 0);

            GameObject far = CreateOwnedObject("Far");
            far.transform.position = new Vector3(100, 0, 0);

            var list = new List<Transform> { near.transform, far.transform };
            Transform closest = origin.transform.GetClosest(list);
            Assert.AreEqual(near.transform, closest);

            Object.DestroyImmediate(origin);
            Object.DestroyImmediate(near);
            Object.DestroyImmediate(far);
        }

        [Test]
        public void Transform_GetChildTransforms()
        {
            GameObject parent = CreateOwnedObject("Parent");
            GameObject c1 = CreateOwnedObject("Child1");
            GameObject c2 = CreateOwnedObject("Child2");
            c1.transform.SetParent(parent.transform);
            c2.transform.SetParent(parent.transform);

            Transform[] children = parent.transform.GetChildTransforms();
            Assert.AreEqual(2, children.Length);

            Object.DestroyImmediate(parent);
        }

        [Test]
        public void Transform_CopyFrom()
        {
            GameObject src = CreateOwnedObject("Src");
            src.transform.position = new Vector3(5, 10, 15);
            src.transform.localScale = new Vector3(2, 2, 2);

            GameObject dst = CreateOwnedObject("Dst");
            dst.transform.CopyFrom(src.transform);

            Assert.AreEqual(src.transform.position, dst.transform.position);
            Assert.AreEqual(src.transform.localScale, dst.transform.localScale);

            Object.DestroyImmediate(src);
            Object.DestroyImmediate(dst);
        }

        [Test]
        public void Transform_DestroyChildren()
        {
            GameObject parent = CreateOwnedObject("Parent");
            CreateOwnedObject("C1").transform.SetParent(parent.transform);
            CreateOwnedObject("C2").transform.SetParent(parent.transform);
            Assert.AreEqual(2, parent.transform.childCount);

            parent.transform.DestroyChildren();
            Assert.AreEqual(0, parent.transform.childCount);

            Object.DestroyImmediate(parent);
        }

        [Test]
        public void Transform_SetRotation_Euler()
        {
            GameObject go = CreateOwnedObject("T");
            go.transform.SetRotation(eulerAngles: new Vector3(0, 90, 0));
            Assert.AreEqual(90f, go.transform.eulerAngles.y, 0.1f);
            Object.DestroyImmediate(go);
        }

        #endregion

        #region ComponentExtensions

        [Test]
        public void Component_GetOrAdd_ExistingComponent()
        {
            GameObject go = CreateOwnedObject("T");
            Rigidbody rb = go.AddComponent<Rigidbody>();
            Rigidbody result = go.transform.GetOrAdd<Rigidbody>();
            Assert.AreSame(rb, result);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Component_GetOrAdd_AddsIfMissing()
        {
            GameObject go = CreateOwnedObject("T");
            Assert.IsNull(go.GetComponent<Rigidbody>());
            Rigidbody result = go.transform.GetOrAdd<Rigidbody>();
            Assert.IsNotNull(result);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Component_GetPath()
        {
            GameObject parent = CreateOwnedObject("Parent");
            GameObject child = CreateOwnedObject("Child");
            child.transform.SetParent(parent.transform);

            string path = child.transform.GetPath();
            Assert.AreEqual("Parent/Child", path);

            Object.DestroyImmediate(parent);
        }

        #endregion

        #region ObjectExtensions

        [Test]
        public void Object_IsValid_TrueForLive()
        {
            GameObject go = CreateOwnedObject("Live");
            Assert.IsTrue(go.IsValid());
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Object_IsValid_FalseForDestroyed()
        {
            GameObject go = CreateOwnedObject("ToDestroy");
            Object.DestroyImmediate(go);
            Assert.IsFalse(go.IsValid());
        }

        [Test]
        public void Object_GetName()
        {
            GameObject go = CreateOwnedObject("TestName");
            Assert.AreEqual("TestName", go.GetName());
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Object_SetName()
        {
            GameObject go = CreateOwnedObject("Old");
            go.SetName("New");
            Assert.AreEqual("New", go.name);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Object_SafeDestroy_EditMode()
        {
            GameObject go = CreateOwnedObject("ToSafeDestroy");
            go.SafeDestroy();
            // WHY: In EditMode, SafeDestroy calls DestroyImmediate
            Assert.IsTrue(go == null);
        }

        #endregion

        #region GameObjectArrayExtensions

        [Test]
        public void GOArray_SetActiveAll()
        {
            GameObject a = CreateOwnedObject("A");
            GameObject b = CreateOwnedObject("B");
            var list = new List<GameObject> { a, b };
            list.SetActiveAll(false);
            Assert.IsFalse(a.activeSelf);
            Assert.IsFalse(b.activeSelf);
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }

        [Test]
        public void GOArray_GetActiveObjects()
        {
            GameObject a = CreateOwnedObject("Active");
            GameObject b = CreateOwnedObject("Inactive");
            b.SetActive(false);
            var list = new List<GameObject> { a, b };
            var active = list.GetActiveObjects().ToList();
            Assert.AreEqual(1, active.Count);
            Assert.AreEqual(a, active[0]);
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }

        [Test]
        public void GOArray_FindClosest()
        {
            GameObject a = CreateOwnedObject("Near");
            a.transform.position = new Vector3(1, 0, 0);
            GameObject b = CreateOwnedObject("Far");
            b.transform.position = new Vector3(100, 0, 0);
            var list = new List<GameObject> { a, b };

            GameObject closest = list.FindClosest(Vector3.zero);
            Assert.AreEqual(a, closest);

            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }

        [Test]
        public void GOArray_WithinDistance()
        {
            GameObject a = CreateOwnedObject("Close");
            a.transform.position = new Vector3(1, 0, 0);
            GameObject b = CreateOwnedObject("TooFar");
            b.transform.position = new Vector3(50, 0, 0);
            var list = new List<GameObject> { a, b };

            var within = list.WithinDistance(Vector3.zero, 5f).ToList();
            Assert.AreEqual(1, within.Count);
            Assert.AreEqual(a, within[0]);

            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }

        [Test]
        public void GOArray_GetAveragePosition()
        {
            GameObject a = CreateOwnedObject("A");
            a.transform.position = new Vector3(0, 0, 0);
            GameObject b = CreateOwnedObject("B");
            b.transform.position = new Vector3(10, 0, 0);
            var list = new List<GameObject> { a, b };

            Vector3 avg = list.GetAveragePosition();
            Assert.AreEqual(5f, avg.x, 0.001f);

            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }

        [Test]
        public void GOArray_SetActiveRange()
        {
            GameObject a = CreateOwnedObject("A");
            GameObject b = CreateOwnedObject("B");
            GameObject c = CreateOwnedObject("C");
            var list = new List<GameObject> { a, b, c };

            list.SetActiveRange(2, false);
            Assert.IsFalse(a.activeSelf);
            Assert.IsFalse(b.activeSelf);
            Assert.IsTrue(c.activeSelf); // WHY: index 2 not included

            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
            Object.DestroyImmediate(c);
        }

        [Test]
        public void GOArray_SetParentAll()
        {
            GameObject parent = CreateOwnedObject("Parent");
            GameObject a = CreateOwnedObject("A");
            GameObject b = CreateOwnedObject("B");
            var list = new List<GameObject> { a, b };

            list.SetParentAll(parent.transform);
            Assert.AreEqual(parent.transform, a.transform.parent);
            Assert.AreEqual(parent.transform, b.transform.parent);

            Object.DestroyImmediate(parent);
        }

        #endregion
    }
}
