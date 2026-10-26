using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.UI;
using Neo.UI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Tests.Edit.UI
{
    public sealed class Audit20261023NeoUIEditorAuthoringTests
    {
        private readonly List<GameObject> _roots = new List<GameObject>();
        private readonly List<string> _assetPaths = new List<string>();
        private Object[] _selectionBefore;

        [SetUp]
        public void SetUp()
        {
            _selectionBefore = Selection.objects;
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = _roots.Count - 1; index >= 0; index--)
            {
                if (_roots[index] != null)
                {
                    Object.DestroyImmediate(_roots[index]);
                }
            }

            _roots.Clear();
            for (int index = 0; index < _assetPaths.Count; index++)
            {
                AssetDatabase.DeleteAsset(_assetPaths[index]);
            }

            _assetPaths.Clear();
            Selection.objects = _selectionBefore;
        }

        [TestCase(typeof(UIMeshRigGraphic))]
        [TestCase(typeof(UIMeshRigWorldRenderer))]
        [TestCase(typeof(UIMeshRigSpriteRenderer))]
        public void DuplicateNestedPoint_PreservesParentPoseAndRestPose(Type rigType)
        {
            IUIMeshRigOwner rig = CreateRig(rigType);
            rig.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
            GameObject group = new GameObject("Transformed group", typeof(RectTransform));
            group.transform.SetParent(rig.RigTransform, false);
            group.transform.localPosition = new Vector3(40f, -30f, 0f);
            group.transform.localRotation = Quaternion.Euler(0f, 0f, 17f);
            group.transform.localScale = new Vector3(1.5f, 0.8f, 1f);
            UIMeshRigPoint source = CreatePoint(group.transform, "Control");
            source.transform.localPosition = new Vector3(7f, 11f, 0f);
            source.transform.localRotation = Quaternion.Euler(0f, 0f, 12f);
            source.CaptureRestPose(rig);
            Vector3 restPosition = source.transform.position;
            Vector3 restLocalPosition = source.transform.localPosition;
            Quaternion restLocalRotation = source.transform.localRotation;
            Vector3 restLocalScale = source.transform.localScale;
            source.transform.localPosition += new Vector3(3f, -2f, 0f);
            source.transform.localRotation = Quaternion.Euler(0f, 0f, 29f);
            source.transform.localScale = new Vector3(1.2f, 0.7f, 1f);
            Vector3 posedLocalPosition = new Vector3(10f, 9f, 0f);
            Quaternion posedLocalRotation = Quaternion.Euler(0f, 0f, 29f);
            Vector3 posedLocalScale = new Vector3(1.2f, 0.7f, 1f);
            Vector3 posedWorldPosition = group.transform.TransformPoint(posedLocalPosition);
            Quaternion posedWorldRotation = source.transform.rotation;
            Vector3 posedWorldScale = source.transform.lossyScale;
            Vector2 restCenter = source.RestCenterNormalized;
            float restRotation = source.RestRotationDegrees;
            Vector3 restScale = source.RestScale;
            Vector3 parentPosition = group.transform.position;
            Quaternion parentRotation = group.transform.rotation;
            Vector3 parentScale = group.transform.localScale;
            Undo.IncrementCurrentGroup();
            int duplicateGroup = Undo.GetCurrentGroup();

            Assert.That(rig.AuthoringMode, Is.EqualTo(UIMeshRigAuthoringMode.Pose));
            Assert.That(source.transform.localPosition, Is.EqualTo(posedLocalPosition),
                "The native source must be in the authored pose before duplication.");
            Assert.That(Quaternion.Angle(source.transform.localRotation, posedLocalRotation), Is.LessThan(0.001f));
            Assert.That(source.transform.localScale, Is.EqualTo(posedLocalScale));
            Assert.That(Vector3.Distance(source.transform.position, posedWorldPosition), Is.LessThan(0.0001f));

            UIMeshRigPoint duplicate = UIMeshRigEditorUtility.DuplicatePoint(rig, source);

            Assert.That(duplicate.transform.parent, Is.SameAs(group.transform));
            Assert.That(rig.AuthoringMode, Is.EqualTo(UIMeshRigAuthoringMode.Pose));
            Assert.That(source.transform.localPosition, Is.EqualTo(posedLocalPosition));
            Assert.That(Quaternion.Angle(source.transform.localRotation, posedLocalRotation), Is.LessThan(0.001f));
            Assert.That(source.transform.localScale, Is.EqualTo(posedLocalScale));
            Assert.That(Vector3.Distance(source.transform.position, posedWorldPosition), Is.LessThan(0.0001f));
            Assert.That(Vector3.Distance(duplicate.transform.localPosition, posedLocalPosition), Is.LessThan(0.0001f));
            Assert.That(Vector3.Distance(duplicate.transform.position, posedWorldPosition), Is.LessThan(0.0001f));
            Assert.That(Vector3.Distance(duplicate.transform.position, source.transform.position), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(duplicate.transform.localRotation, posedLocalRotation), Is.LessThan(0.001f));
            Assert.That(duplicate.transform.localScale, Is.EqualTo(posedLocalScale));
            Assert.That(Quaternion.Angle(source.transform.rotation, posedWorldRotation), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(source.transform.lossyScale, posedWorldScale), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(duplicate.transform.rotation, source.transform.rotation), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(duplicate.transform.lossyScale, source.transform.lossyScale), Is.LessThan(0.0001f));
            Assert.That(duplicate.RestCenterNormalized, Is.EqualTo(restCenter));
            Assert.That(duplicate.RestCenterNormalized, Is.EqualTo(source.RestCenterNormalized));
            Assert.That(duplicate.RestRotationDegrees, Is.EqualTo(restRotation));
            Assert.That(duplicate.RestScale, Is.EqualTo(restScale));
            Assert.That(group.transform.parent, Is.SameAs(rig.RigTransform));
            Assert.That(group.transform.position, Is.EqualTo(parentPosition));
            Assert.That(Quaternion.Angle(group.transform.rotation, parentRotation), Is.LessThan(0.001f));
            Assert.That(group.transform.localScale, Is.EqualTo(parentScale));
            Assert.That(rig.RigPoints.Count, Is.EqualTo(2));

            duplicate.ResetPose(rig);

            Assert.That(Vector3.Distance(duplicate.transform.localPosition, restLocalPosition), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(duplicate.transform.localRotation, restLocalRotation), Is.LessThan(0.001f));
            Assert.That(duplicate.transform.localScale, Is.EqualTo(restLocalScale));
            Assert.That(Vector3.Distance(duplicate.transform.position, restPosition), Is.LessThan(0.0001f),
                "The copied rest pose must remain meaningful in the duplicate's parent space.");
            Assert.That(source.transform.localPosition, Is.EqualTo(posedLocalPosition));
            Assert.That(source.RestCenterNormalized, Is.EqualTo(restCenter));
            Assert.That(source.RestRotationDegrees, Is.EqualTo(restRotation));
            Assert.That(source.RestScale, Is.EqualTo(restScale));

            Undo.FlushUndoRecordObjects();
            Undo.CollapseUndoOperations(duplicateGroup);
            Undo.PerformUndo();
            rig.NotifyPointChanged();

            Assert.That(duplicate == null, Is.True);
            Assert.That(source.transform.parent, Is.SameAs(group.transform));
            Assert.That(rig.RigPoints.Count, Is.EqualTo(1));
        }

        [TestCase(typeof(UIMeshRigGraphic))]
        [TestCase(typeof(UIMeshRigWorldRenderer))]
        [TestCase(typeof(UIMeshRigSpriteRenderer))]
        public void DuplicatePoint_RetainsRemappedInternalAndOriginalExternalReferencesAcrossUndo(Type rigType)
        {
            IUIMeshRigOwner rig = CreateRig(rigType);
            rig.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
            UIMeshRigPoint source = CreatePoint(rig.RigTransform, "Referenced control");
            GameObject graphicObject = new GameObject("Owned graphic", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            graphicObject.transform.SetParent(source.transform, false);
            Image sourceGraphic = graphicObject.GetComponent<Image>();
            Button sourceButton = source.gameObject.AddComponent<Button>();
            sourceButton.targetGraphic = sourceGraphic;
            GameObject externalObject = new GameObject("External selectable", typeof(RectTransform), typeof(Button));
            _roots.Add(externalObject);
            Button externalButton = externalObject.GetComponent<Button>();
            Navigation navigation = sourceButton.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnRight = externalButton;
            sourceButton.navigation = navigation;
            Undo.IncrementCurrentGroup();
            int duplicateGroup = Undo.GetCurrentGroup();

            UIMeshRigPoint duplicate = UIMeshRigEditorUtility.DuplicatePoint(rig, source);

            Image duplicateGraphic = duplicate.transform.GetChild(0).GetComponent<Image>();
            Button duplicateButton = duplicate.GetComponent<Button>();
            Assert.That(duplicateGraphic, Is.Not.SameAs(sourceGraphic));
            Assert.That(duplicateButton.targetGraphic, Is.SameAs(duplicateGraphic));
            Assert.That(duplicateButton.navigation.selectOnRight, Is.SameAs(externalButton));
            Assert.That(sourceButton.targetGraphic, Is.SameAs(sourceGraphic));
            Assert.That(sourceButton.navigation.selectOnRight, Is.SameAs(externalButton));
            Assert.That(rig.RigPoints.Count, Is.EqualTo(2));

            Undo.FlushUndoRecordObjects();
            Undo.CollapseUndoOperations(duplicateGroup);
            Undo.PerformUndo();
            rig.NotifyPointChanged();

            Assert.That(duplicate == null, Is.True);
            Assert.That(duplicateGraphic == null, Is.True);
            Assert.That(sourceButton.targetGraphic, Is.SameAs(sourceGraphic));
            Assert.That(sourceButton.navigation.selectOnRight, Is.SameAs(externalButton));
            Assert.That(externalButton != null, Is.True);
            Assert.That(rig.RigPoints.Count, Is.EqualTo(1));
        }

        [TestCase(typeof(UIMeshRigGraphic), false)]
        [TestCase(typeof(UIMeshRigGraphic), true)]
        [TestCase(typeof(UIMeshRigWorldRenderer), false)]
        [TestCase(typeof(UIMeshRigWorldRenderer), true)]
        [TestCase(typeof(UIMeshRigSpriteRenderer), false)]
        [TestCase(typeof(UIMeshRigSpriteRenderer), true)]
        public void AddPoint_UsesUniqueKeyAcrossTheOwnersWholeHierarchy(Type rigType, bool useLayout)
        {
            IUIMeshRigOwner rig = CreateRig(rigType);
            GameObject group = new GameObject("Group", typeof(RectTransform));
            group.transform.SetParent(rig.RigTransform, false);
            UIMeshRigPoint existing = CreatePoint(group.transform, "Nested control");
            existing.SetBindingKey("Rig Point");
            rig.NotifyPointChanged();
            UIMeshRigPoint added;
            if (useLayout)
            {
                UIMeshRigPointLayout layout = new UIMeshRigPointLayout(
                    "Rig Point", new Vector2(0.5f, 0.5f), new Vector2(0.1f, 0.1f),
                    new Vector2(0.2f, 0.2f), UIMeshRigMotionPreset.Custom, 0f, 0);
                added = UIMeshRigEditorUtility.CreatePoint(rig, layout, false);
            }
            else
            {
                added = UIMeshRigEditorUtility.CreatePoint(rig);
            }

            Assert.That(existing.BindingKey, Is.EqualTo("Rig Point"));
            Assert.That(added.BindingKey, Is.Not.EqualTo(existing.BindingKey));
            Assert.That(added.BindingKey, Is.Not.Empty);
            Assert.That(rig.RigPoints.Count, Is.EqualTo(2));
        }

        [TestCase(typeof(UIMeshRigGraphic))]
        [TestCase(typeof(UIMeshRigWorldRenderer))]
        [TestCase(typeof(UIMeshRigSpriteRenderer))]
        public void DuplicatePoint_DoesNotReuseAnExistingCustomBindingKey(Type rigType)
        {
            IUIMeshRigOwner rig = CreateRig(rigType);
            UIMeshRigPoint source = CreatePoint(rig.RigTransform, "Control");
            source.SetBindingKey("Control (1)");
            rig.NotifyPointChanged();

            UIMeshRigPoint duplicate = UIMeshRigEditorUtility.DuplicatePoint(rig, source);

            Assert.That(source.BindingKey, Is.EqualTo("Control (1)"));
            Assert.That(duplicate.BindingKey, Is.Not.EqualTo(source.BindingKey));
            Assert.That(rig.RigPoints.Count, Is.EqualTo(2));
        }

        [Test]
        public void DuplicatePoint_RegeneratesKeysForInactiveDescendantPoints()
        {
            IUIMeshRigOwner rig = CreateRig(typeof(UIMeshRigGraphic));
            UIMeshRigPoint source = CreatePoint(rig.RigTransform, "Parent control");
            UIMeshRigPoint child = CreatePoint(source.transform, "Child control");
            child.SetBindingKey("Hand");
            child.gameObject.SetActive(false);
            rig.NotifyPointChanged();

            UIMeshRigPoint duplicate = UIMeshRigEditorUtility.DuplicatePoint(rig, source);

            UIMeshRigPoint copiedChild = duplicate.transform.GetChild(0).GetComponent<UIMeshRigPoint>();
            Assert.That(copiedChild.gameObject.activeSelf, Is.False);
            Assert.That(child.BindingKey, Is.EqualTo("Hand"));
            Assert.That(copiedChild.BindingKey, Is.Not.EqualTo(child.BindingKey));
            Assert.That(rig.RigPoints.Count, Is.EqualTo(4));
            HashSet<string> keys = new HashSet<string>();
            foreach (UIMeshRigPoint point in rig.RigPoints)
            {
                Assert.That(keys.Add(point.BindingKey), Is.True, "Duplicating a branch must not create ambiguous bindings.");
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DestructiveAuthoring_WithOrdinaryChildPoints_RemainsUndoable(bool applyLayout)
        {
            IUIMeshRigOwner rig = CreateRig(typeof(UIMeshRigGraphic));
            UIMeshRigPoint original = CreatePoint(rig.RigTransform, "Original control");
            original.SetBindingKey("Authored key");
            rig.NotifyPointChanged();
            Undo.IncrementCurrentGroup();
            int actionGroup = Undo.GetCurrentGroup();

            if (applyLayout)
            {
                UIMeshRigPoint created = UIMeshRigEditorUtility.ApplyLayout(rig, UIMeshRigLayoutPreset.Character, false);
                Assert.That(created, Is.Not.Null);
                Assert.That(rig.RigPoints.Count, Is.EqualTo(UIMeshRigLayoutPresets.GetPointCount(UIMeshRigLayoutPreset.Character)));
                Assert.That(rig.AuthoringMode, Is.EqualTo(UIMeshRigAuthoringMode.Pose));
            }
            else
            {
                UIMeshRigEditorUtility.DeletePoint(rig, original);
                Assert.That(rig.RigPoints.Count, Is.Zero);
            }

            Assert.That(original == null, Is.True);
            Undo.FlushUndoRecordObjects();
            Undo.CollapseUndoOperations(actionGroup);
            Undo.PerformUndo();
            rig.NotifyPointChanged();

            Assert.That(rig.RigPoints.Count, Is.EqualTo(1));
            Assert.That(rig.RigPoints[0].BindingKey, Is.EqualTo("Authored key"));
            Assert.That(rig.AuthoringMode, Is.EqualTo(UIMeshRigAuthoringMode.Setup));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DestructiveAuthoring_WithNestedRig_IsRejectedBeforeAnyPointIsRemoved(bool applyLayout)
        {
            IUIMeshRigOwner rig = CreateRig(typeof(UIMeshRigGraphic));
            UIMeshRigPoint safePoint = CreatePoint(rig.RigTransform, "Safe control");
            UIMeshRigPoint protectedPoint = CreatePoint(rig.RigTransform, "Nested rig parent");
            IUIMeshRigOwner nestedRig = CreateRig(typeof(UIMeshRigWorldRenderer));
            nestedRig.RigTransform.SetParent(protectedPoint.transform, false);
            UIMeshRigPoint nestedPoint = CreatePoint(nestedRig.RigTransform, "Nested control");
            rig.NotifyPointChanged();
            nestedRig.NotifyPointChanged();
            LogAssert.Expect(LogType.Warning, "[UI Mesh Rig] Cannot delete a point containing a nested rig. " +
                                              "Move the nested rig outside the point hierarchy first.");

            if (applyLayout)
            {
                Assert.That(UIMeshRigEditorUtility.ApplyLayout(rig, UIMeshRigLayoutPreset.Character, false), Is.Null);
            }
            else
            {
                UIMeshRigEditorUtility.DeletePoint(rig, protectedPoint);
            }

            Assert.That(safePoint != null, Is.True);
            Assert.That(protectedPoint != null, Is.True);
            Assert.That(nestedPoint != null, Is.True);
            Assert.That(nestedRig.RigTransform.parent, Is.SameAs(protectedPoint.transform));
            Assert.That(rig.RigPoints.Count, Is.EqualTo(2));
            Assert.That(nestedRig.RigPoints.Count, Is.EqualTo(1));
            Assert.That(rig.AuthoringMode, Is.EqualTo(UIMeshRigAuthoringMode.Setup));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DestructiveAuthoring_WithPointOnOwnerRoot_PreservesTheRig(bool applyLayout)
        {
            IUIMeshRigOwner rig = CreateRig(typeof(UIMeshRigGraphic));
            UIMeshRigPoint rootPoint = rig.RigTransform.gameObject.AddComponent<UIMeshRigPoint>();
            UIMeshRigPoint childPoint = CreatePoint(rig.RigTransform, "Child control");
            rig.NotifyPointChanged();
            LogAssert.Expect(LogType.Warning, "[UI Mesh Rig] Cannot delete a point on the rig root. " +
                                              "Move the point to a child GameObject first.");

            if (applyLayout)
            {
                Assert.That(UIMeshRigEditorUtility.ApplyLayout(rig, UIMeshRigLayoutPreset.Character, false), Is.Null);
            }
            else
            {
                UIMeshRigEditorUtility.DeletePoint(rig, rootPoint);
            }

            Assert.That((Object)rig != null, Is.True);
            Assert.That(rootPoint != null, Is.True);
            Assert.That(childPoint != null, Is.True);
            Assert.That(rig.RigPoints.Count, Is.EqualTo(2));
        }

        [Test]
        public void DuplicatePoint_OnOwnerRoot_PreservesTheRigWithoutCloningIt()
        {
            IUIMeshRigOwner rig = CreateRig(typeof(UIMeshRigGraphic));
            UIMeshRigPoint rootPoint = rig.RigTransform.gameObject.AddComponent<UIMeshRigPoint>();
            LogAssert.Expect(LogType.Warning, "[UI Mesh Rig] Cannot duplicate a point on the rig root. " +
                                              "Move the point to a child GameObject first.");

            UIMeshRigPoint duplicate = UIMeshRigEditorUtility.DuplicatePoint(rig, rootPoint);
            UIMeshRigEditorUtility.Focus(duplicate);

            Assert.That(duplicate, Is.Null);
            Assert.That(rig.RigTransform.childCount, Is.Zero);
            Assert.That(rootPoint != null, Is.True);
            Assert.That(rig.RigPoints.Count, Is.EqualTo(1));
        }

        [Test]
        public void ConvertImage_OnPrefabInstance_RecordsSelectableRetargetAsAnOverride()
        {
            GameObject original = new GameObject("Button prefab", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Button));
            _roots.Add(original);
            original.GetComponent<Button>().targetGraphic = original.GetComponent<Image>();
            string path = "Assets/Audit20261023NeoUIEditor_" + Guid.NewGuid().ToString("N") + ".prefab";
            _assetPaths.Add(path);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(original, path);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            _roots.Add(instance);
            MethodInfo convert = typeof(UIMeshRigMenu).GetMethod("ConvertImage", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(convert, Is.Not.Null);

            convert.Invoke(null, new object[] { new MenuCommand(instance.GetComponent<Image>()) });

            UIMeshRigGraphic rig = instance.GetComponent<UIMeshRigGraphic>();
            Assert.That(rig, Is.Not.Null);
            Assert.That(instance.GetComponent<Button>().targetGraphic, Is.SameAs(rig));
            PropertyModification[] modifications = PrefabUtility.GetPropertyModifications(instance);
            Assert.That(modifications, Is.Not.Null);
            bool hasRetargetOverride = false;
            for (int index = 0; index < modifications.Length; index++)
            {
                PropertyModification modification = modifications[index];
                if (modification.target == prefab.GetComponent<Button>() &&
                    modification.propertyPath == "m_TargetGraphic" && modification.objectReference == rig)
                {
                    hasRetargetOverride = true;
                    break;
                }
            }

            Assert.That(hasRetargetOverride, Is.True,
                "The visible rig reference must be stored in prefab instance overrides, not only live memory.");
        }

        private IUIMeshRigOwner CreateRig(Type rigType)
        {
            GameObject root = new GameObject("Audit rig", typeof(RectTransform), rigType);
            _roots.Add(root);
            ((RectTransform)root.transform).sizeDelta = new Vector2(400f, 300f);
            return (IUIMeshRigOwner)root.GetComponent(rigType);
        }

        private static UIMeshRigPoint CreatePoint(Transform parent, string name)
        {
            GameObject pointObject = new GameObject(name, typeof(RectTransform), typeof(UIMeshRigPoint));
            pointObject.transform.SetParent(parent, false);
            return pointObject.GetComponent<UIMeshRigPoint>();
        }
    }
}
