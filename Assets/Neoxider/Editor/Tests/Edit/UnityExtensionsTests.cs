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
        #region TransformExtensions

        [Test]
        public void Transform_SetPosition_SetsXYZ()
        {
            var go = new GameObject("T");
            go.transform.SetPosition(x: 5f, y: 10f, z: 15f);
            Assert.AreEqual(5f, go.transform.position.x, 0.001f);
            Assert.AreEqual(10f, go.transform.position.y, 0.001f);
            Assert.AreEqual(15f, go.transform.position.z, 0.001f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Transform_SetPosition_PartialOverride()
        {
            var go = new GameObject("T");
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
            var go = new GameObject("T");
            go.transform.position = new Vector3(1, 2, 3);
            go.transform.AddPosition(x: 10f);
            Assert.AreEqual(11f, go.transform.position.x, 0.001f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Transform_SetScale()
        {
            var go = new GameObject("T");
            go.transform.SetScale(x: 2f, y: 3f, z: 4f);
            Assert.AreEqual(new Vector3(2, 3, 4), go.transform.localScale);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Transform_ResetTransform()
        {
            var go = new GameObject("T");
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
            var origin = new GameObject("Origin");
            origin.transform.position = Vector3.zero;

            var near = new GameObject("Near");
            near.transform.position = new Vector3(1, 0, 0);

            var far = new GameObject("Far");
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
            var parent = new GameObject("Parent");
            var c1 = new GameObject("Child1");
            var c2 = new GameObject("Child2");
            c1.transform.SetParent(parent.transform);
            c2.transform.SetParent(parent.transform);

            Transform[] children = parent.transform.GetChildTransforms();
            Assert.AreEqual(2, children.Length);

            Object.DestroyImmediate(parent);
        }

        [Test]
        public void Transform_CopyFrom()
        {
            var src = new GameObject("Src");
            src.transform.position = new Vector3(5, 10, 15);
            src.transform.localScale = new Vector3(2, 2, 2);

            var dst = new GameObject("Dst");
            dst.transform.CopyFrom(src.transform);

            Assert.AreEqual(src.transform.position, dst.transform.position);
            Assert.AreEqual(src.transform.localScale, dst.transform.localScale);

            Object.DestroyImmediate(src);
            Object.DestroyImmediate(dst);
        }

        [Test]
        public void Transform_DestroyChildren()
        {
            var parent = new GameObject("Parent");
            new GameObject("C1").transform.SetParent(parent.transform);
            new GameObject("C2").transform.SetParent(parent.transform);
            Assert.AreEqual(2, parent.transform.childCount);

            parent.transform.DestroyChildren();
            Assert.AreEqual(0, parent.transform.childCount);

            Object.DestroyImmediate(parent);
        }

        [Test]
        public void Transform_SetRotation_Euler()
        {
            var go = new GameObject("T");
            go.transform.SetRotation(eulerAngles: new Vector3(0, 90, 0));
            Assert.AreEqual(90f, go.transform.eulerAngles.y, 0.1f);
            Object.DestroyImmediate(go);
        }

        #endregion

        #region ComponentExtensions

        [Test]
        public void Component_GetOrAdd_ExistingComponent()
        {
            var go = new GameObject("T");
            Rigidbody rb = go.AddComponent<Rigidbody>();
            Rigidbody result = go.transform.GetOrAdd<Rigidbody>();
            Assert.AreSame(rb, result);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Component_GetOrAdd_AddsIfMissing()
        {
            var go = new GameObject("T");
            Assert.IsNull(go.GetComponent<Rigidbody>());
            Rigidbody result = go.transform.GetOrAdd<Rigidbody>();
            Assert.IsNotNull(result);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Component_GetPath()
        {
            var parent = new GameObject("Parent");
            var child = new GameObject("Child");
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
            var go = new GameObject("Live");
            Assert.IsTrue(go.IsValid());
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Object_IsValid_FalseForDestroyed()
        {
            var go = new GameObject("ToDestroy");
            Object.DestroyImmediate(go);
            Assert.IsFalse(go.IsValid());
        }

        [Test]
        public void Object_GetName()
        {
            var go = new GameObject("TestName");
            Assert.AreEqual("TestName", go.GetName());
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Object_SetName()
        {
            var go = new GameObject("Old");
            go.SetName("New");
            Assert.AreEqual("New", go.name);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Object_SafeDestroy_EditMode()
        {
            var go = new GameObject("ToSafeDestroy");
            go.SafeDestroy();
            // In EditMode, SafeDestroy calls DestroyImmediate
            Assert.IsTrue(go == null);
        }

        #endregion

        #region GameObjectArrayExtensions

        [Test]
        public void GOArray_SetActiveAll()
        {
            var a = new GameObject("A");
            var b = new GameObject("B");
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
            var a = new GameObject("Active");
            var b = new GameObject("Inactive");
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
            var a = new GameObject("Near");
            a.transform.position = new Vector3(1, 0, 0);
            var b = new GameObject("Far");
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
            var a = new GameObject("Close");
            a.transform.position = new Vector3(1, 0, 0);
            var b = new GameObject("TooFar");
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
            var a = new GameObject("A");
            a.transform.position = new Vector3(0, 0, 0);
            var b = new GameObject("B");
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
            var a = new GameObject("A");
            var b = new GameObject("B");
            var c = new GameObject("C");
            var list = new List<GameObject> { a, b, c };

            list.SetActiveRange(2, false);
            Assert.IsFalse(a.activeSelf);
            Assert.IsFalse(b.activeSelf);
            Assert.IsTrue(c.activeSelf); // index 2 not included

            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
            Object.DestroyImmediate(c);
        }

        [Test]
        public void GOArray_SetParentAll()
        {
            var parent = new GameObject("Parent");
            var a = new GameObject("A");
            var b = new GameObject("B");
            var list = new List<GameObject> { a, b };

            list.SetParentAll(parent.transform);
            Assert.AreEqual(parent.transform, a.transform.parent);
            Assert.AreEqual(parent.transform, b.transform.parent);

            Object.DestroyImmediate(parent);
        }

        #endregion
    }
}
