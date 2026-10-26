using System;
using Neo.UI;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoUIRuntimeReplacementTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void Replace_RootPoint_RejectsBeforeRemovingAnyOtherPoint(int adapter)
        {
            IUIMeshRigOwner owner = CreateOwner(adapter);
            owner.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
            UIMeshRigPoint rootPoint = _root.AddComponent<UIMeshRigPoint>();
            rootPoint.SetBindingKey("Root binding");
            UIMeshRigPoint child = CreatePoint(_root.transform, "Child binding");
            int childCount = _root.transform.childCount;
            Assert.That(owner.RigPoints.Count, Is.EqualTo(2));

            Assert.Throws<InvalidOperationException>(() => UIMeshRigLayoutBuilder.ApplyToOwner(
                owner, UIMeshRigLayoutPreset.Character, true, false));

            Assert.That(_root != null, Is.True);
            Assert.That(rootPoint != null, Is.True);
            Assert.That(child != null, Is.True);
            Assert.That(owner.AuthoringMode, Is.EqualTo(UIMeshRigAuthoringMode.Pose));
            Assert.That(owner.RigPoints.Count, Is.EqualTo(2));
            Assert.That(_root.transform.childCount, Is.EqualTo(childCount));
            Assert.That(rootPoint.BindingKey, Is.EqualTo("Root binding"));
            Assert.That(child.BindingKey, Is.EqualTo("Child binding"));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void Replace_InactiveNestedOwnerInPoint_RejectsTheWholeRemovalSet(int adapter)
        {
            IUIMeshRigOwner owner = CreateOwner(adapter);
            owner.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
            UIMeshRigPoint safe = CreatePoint(_root.transform, "Disposable sibling");
            UIMeshRigPoint branch = CreatePoint(_root.transform, "Protected branch");
            GameObject nestedObject = new GameObject("Inactive nested owner", typeof(RectTransform));
            nestedObject.SetActive(false);
            nestedObject.transform.SetParent(branch.transform, false);
            IUIMeshRigOwner nested = AddOwner(nestedObject, adapter);
            nested.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
            UIMeshRigPoint nestedPoint = CreatePoint(nestedObject.transform, "Nested binding");
            int children = _root.transform.childCount;
            Assert.That(owner.RigPoints.Count, Is.EqualTo(2));
            Assert.That(nested.RigPoints.Count, Is.EqualTo(1));

            Assert.Throws<InvalidOperationException>(() => UIMeshRigLayoutBuilder.ApplyToOwner(
                owner, UIMeshRigLayoutPreset.FlagCloth, true, false));

            Assert.That(safe != null && branch != null && nestedObject != null && nestedPoint != null, Is.True);
            Assert.That(_root.transform.childCount, Is.EqualTo(children));
            Assert.That(branch.transform.parent, Is.EqualTo(_root.transform));
            Assert.That(nestedObject.transform.parent, Is.EqualTo(branch.transform));
            Assert.That(nestedObject.activeSelf, Is.False);
            Assert.That(owner.RigPoints.Count, Is.EqualTo(2));
            Assert.That(nested.RigPoints.Count, Is.EqualTo(1));
            Assert.That(owner.AuthoringMode, Is.EqualTo(UIMeshRigAuthoringMode.Pose));
            Assert.That(nested.AuthoringMode, Is.EqualTo(UIMeshRigAuthoringMode.Pose));
            Assert.That(safe.BindingKey, Is.EqualTo("Disposable sibling"));
            Assert.That(branch.BindingKey, Is.EqualTo("Protected branch"));
            Assert.That(nestedPoint.BindingKey, Is.EqualTo("Nested binding"));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void Replace_OrdinaryPoints_PreservesIndependentNestedRig(int adapter)
        {
            IUIMeshRigOwner owner = CreateOwner(adapter);
            owner.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
            UIMeshRigPoint old = CreatePoint(_root.transform, "Old binding");
            GameObject grouping = new GameObject("Independent group", typeof(RectTransform));
            grouping.transform.SetParent(_root.transform, false);
            GameObject nestedObject = new GameObject("Independent nested rig", typeof(RectTransform));
            nestedObject.SetActive(false);
            nestedObject.transform.SetParent(grouping.transform, false);
            IUIMeshRigOwner nested = AddOwner(nestedObject, adapter);
            UIMeshRigPoint nestedPoint = CreatePoint(nestedObject.transform, "Foreign binding");

            UIMeshRigPoint[] created = UIMeshRigLayoutBuilder.ApplyToOwner(
                owner, UIMeshRigLayoutPreset.SimpleBounce, true, false);

            Assert.That(old == null, Is.True);
            Assert.That(created.Length, Is.EqualTo(1));
            Assert.That(owner.RigPoints.Count, Is.EqualTo(1));
            Assert.That(owner.RigPoints[0], Is.SameAs(created[0]));
            Assert.That(created[0].BindingKey, Is.EqualTo("Bounce"));
            Assert.That(created[0].transform.parent, Is.EqualTo(_root.transform));
            Assert.That(owner.AuthoringMode, Is.EqualTo(UIMeshRigAuthoringMode.Pose));
            Assert.That(nested.RigPoints.Count, Is.EqualTo(1));
            Assert.That(nested.RigPoints[0], Is.SameAs(nestedPoint));
            Assert.That(nestedPoint.BindingKey, Is.EqualTo("Foreign binding"));
            Assert.That(nestedObject.transform.parent, Is.EqualTo(grouping.transform));
        }

        [Test]
        public void Append_DoesNotRemoveRootOrProtectedBranches()
        {
            IUIMeshRigOwner owner = CreateOwner(0);
            UIMeshRigPoint rootPoint = _root.AddComponent<UIMeshRigPoint>();
            rootPoint.SetBindingKey("Root retained");
            UIMeshRigPoint branch = CreatePoint(_root.transform, "Branch retained");
            GameObject nestedObject = new GameObject("Nested retained", typeof(RectTransform));
            nestedObject.SetActive(false);
            nestedObject.transform.SetParent(branch.transform, false);
            IUIMeshRigOwner nested = AddOwner(nestedObject, 0);
            UIMeshRigPoint nestedPoint = CreatePoint(nestedObject.transform, "Nested retained");

            UIMeshRigPoint[] created = UIMeshRigLayoutBuilder.ApplyToOwner(
                owner, UIMeshRigLayoutPreset.SimpleBounce, false, false);

            Assert.That(created.Length, Is.EqualTo(1));
            Assert.That(owner.RigPoints.Count, Is.EqualTo(3));
            Assert.That(rootPoint.BindingKey, Is.EqualTo("Root retained"));
            Assert.That(branch.BindingKey, Is.EqualTo("Branch retained"));
            Assert.That(nested.RigPoints.Count, Is.EqualTo(1));
            Assert.That(nestedPoint.BindingKey, Is.EqualTo("Nested retained"));
        }

        private IUIMeshRigOwner CreateOwner(int adapter)
        {
            _root = new GameObject("Replacement owner", typeof(RectTransform));
            ((RectTransform)_root.transform).sizeDelta = new Vector2(200f, 200f);
            return AddOwner(_root, adapter);
        }

        private static IUIMeshRigOwner AddOwner(GameObject target, int adapter)
        {
            switch (adapter)
            {
                case 1:
                    return target.AddComponent<UIMeshRigWorldRenderer>();
                case 2:
                    return target.AddComponent<UIMeshRigSpriteRenderer>();
                default:
                    return target.AddComponent<UIMeshRigGraphic>();
            }
        }

        private static UIMeshRigPoint CreatePoint(Transform parent, string key)
        {
            GameObject target = new GameObject(key, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            UIMeshRigPoint point = target.AddComponent<UIMeshRigPoint>();
            point.SetBindingKey(key);
            return point;
        }
    }
}
