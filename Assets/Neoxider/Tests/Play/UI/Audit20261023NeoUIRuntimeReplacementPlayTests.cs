using System;
using System.Collections;
using System.Collections.Generic;
using Neo.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    public sealed class Audit20261023NeoUIRuntimeReplacementPlayTests
    {
        private readonly List<GameObject> _roots = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject root in _roots)
            {
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }
            }

            _roots.Clear();
        }

        [UnityTest]
        public IEnumerator ReplaceRootPoint_LeavesEveryAdapterAndPointAliveAfterDeferredDestroyBoundary()
        {
            for (int adapter = 0; adapter < 3; adapter++)
            {
                GameObject root = CreateRoot();
                IUIMeshRigOwner owner = AddOwner(root, adapter);
                owner.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
                UIMeshRigPoint rootPoint = root.AddComponent<UIMeshRigPoint>();
                rootPoint.SetBindingKey("Root binding");
                UIMeshRigPoint child = CreatePoint(root.transform, "Ordinary sibling");
                int children = root.transform.childCount;

                Assert.Throws<InvalidOperationException>(() => UIMeshRigLayoutBuilder.ApplyToOwner(
                    owner, UIMeshRigLayoutPreset.Character, true, false));
                Assert.That(root != null && rootPoint != null && child != null, Is.True);
                yield return null;

                Assert.That(root != null && rootPoint != null && child != null, Is.True);
                Assert.That(root.transform.childCount, Is.EqualTo(children));
                Assert.That(owner.RigPoints.Count, Is.EqualTo(2));
                Assert.That(owner.AuthoringMode, Is.EqualTo(UIMeshRigAuthoringMode.Pose));
                Assert.That(rootPoint.BindingKey, Is.EqualTo("Root binding"));
                Assert.That(child.BindingKey, Is.EqualTo("Ordinary sibling"));
            }
        }

        [UnityTest]
        public IEnumerator ReplaceProtectedBranch_LeavesInactiveForeignRigAndOrdinarySiblingAliveNextFrame()
        {
            for (int adapter = 0; adapter < 3; adapter++)
            {
                GameObject root = CreateRoot();
                IUIMeshRigOwner owner = AddOwner(root, adapter);
                owner.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
                UIMeshRigPoint safe = CreatePoint(root.transform, "Ordinary sibling");
                UIMeshRigPoint branch = CreatePoint(root.transform, "Protected branch");
                GameObject nestedRoot = new GameObject("Inactive foreign rig", typeof(RectTransform));
                nestedRoot.SetActive(false);
                nestedRoot.transform.SetParent(branch.transform, false);
                IUIMeshRigOwner nested = AddOwner(nestedRoot, adapter);
                nested.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
                UIMeshRigPoint nestedPoint = CreatePoint(nestedRoot.transform, "Foreign binding");
                int children = root.transform.childCount;

                Assert.Throws<InvalidOperationException>(() => UIMeshRigLayoutBuilder.ApplyToOwner(
                    owner, UIMeshRigLayoutPreset.FlagCloth, true, false));
                yield return null;

                Assert.That(root != null && safe != null && branch != null && nestedRoot != null &&
                            nestedPoint != null, Is.True);
                Assert.That(root.transform.childCount, Is.EqualTo(children));
                Assert.That(owner.RigPoints.Count, Is.EqualTo(2));
                Assert.That(nested.RigPoints.Count, Is.EqualTo(1));
                Assert.That(owner.AuthoringMode, Is.EqualTo(UIMeshRigAuthoringMode.Pose));
                Assert.That(nested.AuthoringMode, Is.EqualTo(UIMeshRigAuthoringMode.Pose));
                Assert.That(safe.BindingKey, Is.EqualTo("Ordinary sibling"));
                Assert.That(branch.BindingKey, Is.EqualTo("Protected branch"));
                Assert.That(nestedPoint.BindingKey, Is.EqualTo("Foreign binding"));
                Assert.That(nestedRoot.transform.parent, Is.EqualTo(branch.transform));
                Assert.That(nestedRoot.activeSelf, Is.False);
            }
        }

        [UnityTest]
        public IEnumerator ReplaceOrdinaryPoints_ReleasesOldPointsAndPreservesIndependentNestedRig()
        {
            for (int adapter = 0; adapter < 3; adapter++)
            {
                GameObject root = CreateRoot();
                IUIMeshRigOwner owner = AddOwner(root, adapter);
                owner.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
                UIMeshRigPoint old = CreatePoint(root.transform, "Old binding");
                GameObject nestedRoot = new GameObject("Independent inactive rig", typeof(RectTransform));
                nestedRoot.SetActive(false);
                nestedRoot.transform.SetParent(root.transform, false);
                IUIMeshRigOwner nested = AddOwner(nestedRoot, adapter);
                UIMeshRigPoint nestedPoint = CreatePoint(nestedRoot.transform, "Foreign binding");

                UIMeshRigPoint[] created = UIMeshRigLayoutBuilder.ApplyToOwner(
                    owner, UIMeshRigLayoutPreset.SimpleBounce, true, false);
                Assert.That(created.Length, Is.EqualTo(1));
                yield return null;

                Assert.That(root != null && created[0] != null && nestedRoot != null && nestedPoint != null, Is.True);
                Assert.That(old == null, Is.True);
                Assert.That(owner.RigPoints.Count, Is.EqualTo(1));
                Assert.That(owner.RigPoints[0], Is.SameAs(created[0]));
                Assert.That(created[0].BindingKey, Is.EqualTo("Bounce"));
                Assert.That(owner.AuthoringMode, Is.EqualTo(UIMeshRigAuthoringMode.Pose));
                Assert.That(nested.RigPoints.Count, Is.EqualTo(1));
                Assert.That(nested.RigPoints[0], Is.SameAs(nestedPoint));
                Assert.That(nestedPoint.BindingKey, Is.EqualTo("Foreign binding"));
                Assert.That(nestedRoot.transform.parent, Is.EqualTo(root.transform));
            }
        }

        private GameObject CreateRoot()
        {
            GameObject root = new GameObject("Runtime replacement owner", typeof(RectTransform));
            ((RectTransform)root.transform).sizeDelta = new Vector2(200f, 200f);
            _roots.Add(root);
            return root;
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
