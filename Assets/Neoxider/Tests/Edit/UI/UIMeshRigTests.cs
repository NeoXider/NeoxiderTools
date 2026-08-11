using Neo.UI;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tests.UI
{
    public sealed class UIMeshRigTests
    {
        private GameObject _root;
        private UIMeshRigGraphic _rig;
        private UIMeshRigPoint _point;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject(
                "Rig",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(UIMeshRigGraphic));
            RectTransform rootRect = (RectTransform)_root.transform;
            rootRect.sizeDelta = new Vector2(200f, 100f);
            _rig = _root.GetComponent<UIMeshRigGraphic>();

            GameObject pointObject = new GameObject("Point", typeof(RectTransform), typeof(UIMeshRigPoint));
            RectTransform pointRect = (RectTransform)pointObject.transform;
            pointRect.SetParent(rootRect, false);
            pointRect.anchoredPosition = Vector2.zero;
            _point = pointObject.GetComponent<UIMeshRigPoint>();
            _point.RadiusNormalized = new Vector2(0.25f, 0.25f);
            _point.Falloff = 0.5f;
            _point.CaptureRestPose(_rig);
            _rig.NotifyPointChanged();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void InfluenceWeight_HasSolidCenterSmoothFalloffAndHardOuterBoundary()
        {
            Assert.That(_point.CalculateWeight(new Vector2(0.5f, 0.5f)), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(_point.CalculateWeight(new Vector2(0.7f, 0.5f)), Is.InRange(0f, 1f));
            Assert.That(_point.CalculateWeight(new Vector2(0.75f, 0.5f)), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(_point.CalculateWeight(Vector2.zero), Is.EqualTo(0f));
        }

        [Test]
        public void IndependentInnerAndOuterEllipses_DefineFullBlendAndZeroBoundaries()
        {
            _point.SetInfluenceRadii(new Vector2(0.05f, 0.15f), new Vector2(0.3f, 0.25f));
            _point.ApplyFalloffPreset(UIMeshRigFalloffPreset.Linear);
            Assert.That(_point.CalculateWeight(new Vector2(0.54f, 0.5f)), Is.EqualTo(1f).Within(0.001f));
            Assert.That(_point.CalculateWeight(new Vector2(0.5f, 0.64f)), Is.EqualTo(1f).Within(0.001f));
            Assert.That(_point.CalculateWeight(new Vector2(0.65f, 0.5f)), Is.InRange(0.01f, 0.99f));
            Assert.That(_point.CalculateWeight(new Vector2(0.8f, 0.5f)), Is.Zero.Within(0.001f));
        }

        [Test]
        public void FullSmoothPreset_FadesContinuouslyFromCenterWithoutSolidSeam()
        {
            _point.RadiusNormalized = new Vector2(0.25f, 0.25f);
            _point.UseFullSmoothFalloff();
            float center = _point.CalculateWeight(new Vector2(0.5f, 0.5f));
            float near = _point.CalculateWeight(new Vector2(0.52f, 0.5f));
            float middle = _point.CalculateWeight(new Vector2(0.62f, 0.5f));
            float edge = _point.CalculateWeight(new Vector2(0.74f, 0.5f));
            Assert.That(center, Is.EqualTo(1f).Within(0.001f));
            Assert.That(near, Is.LessThan(center).And.GreaterThan(middle));
            Assert.That(middle, Is.GreaterThan(edge));
            Assert.That(edge, Is.GreaterThan(0f));
        }

        [Test]
        public void InnerEllipse_IsClampedInsideOuterEllipse()
        {
            _point.SetInfluenceRadii(new Vector2(0.8f, 0.7f), new Vector2(0.2f, 0.3f));
            Assert.That(_point.InnerRadiusNormalized.x, Is.EqualTo(0.2f));
            Assert.That(_point.InnerRadiusNormalized.y, Is.EqualTo(0.3f));
        }

        [Test]
        public void IndependentRadii_KeepLegacyFalloffCoherent()
        {
            _point.SetInfluenceRadii(new Vector2(0.1f, 0.15f), new Vector2(0.2f, 0.3f));
            Assert.That(_point.Falloff, Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void VersionOneMigration_DerivesInnerRadiusWithoutRecapturingRestPose()
        {
            GameObject parentObject = new GameObject("Nested", typeof(RectTransform));
            parentObject.transform.SetParent(_root.transform, false);
            _point.transform.SetParent(parentObject.transform, false);
            UnityEditor.SerializedObject serializedPoint = new UnityEditor.SerializedObject(_point);
            serializedPoint.FindProperty("_serializedVersion").intValue = 1;
            serializedPoint.FindProperty("_restLocalPosition").vector3Value = new Vector3(3f, 4f, 0f);
            serializedPoint.FindProperty("_radiusNormalized").vector2Value = new Vector2(0.4f, 0.2f);
            serializedPoint.FindProperty("_falloff").floatValue = 0.25f;
            serializedPoint.ApplyModifiedPropertiesWithoutUndo();
            _point.transform.localPosition = new Vector3(40f, 50f, 0f);

            _point.CalculateWeight(_point.RestCenterNormalized);
            _point.ResetPose(_rig);

            Assert.That(_point.transform.localPosition.x, Is.EqualTo(3f).Within(0.001f));
            Assert.That(_point.transform.localPosition.y, Is.EqualTo(4f).Within(0.001f));
            Assert.That(_point.InnerRadiusNormalized, Is.EqualTo(new Vector2(0.3f, 0.15f)));
            Object.DestroyImmediate(parentObject);
        }

        [Test]
        public void SetupModeEditsBindPose_WhilePoseModeDeformsAndResetRestoresTransform()
        {
            Vector2 center = new Vector2(0.5f, 0.5f);
            Vector2 undeformed = _rig.CalculateDeformedLocalPoint(center);
            _rig.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
            ((RectTransform)_point.transform).anchoredPosition = new Vector2(20f, 0f);
            Vector2 deformed = _rig.CalculateDeformedLocalPoint(center);

            Assert.That(deformed.x - undeformed.x, Is.EqualTo(20f).Within(0.01f));
            Assert.That(deformed.y, Is.EqualTo(undeformed.y).Within(0.01f));

            _rig.ResetPose();
            Assert.That(((RectTransform)_point.transform).anchoredPosition.x, Is.EqualTo(0f).Within(0.01f));
            Assert.That(_rig.CalculateDeformedLocalPoint(center), Is.EqualTo(undeformed));
        }

        [Test]
        public void RotationAndScaleUseAnimatorFriendlyRectTransformDelta()
        {
            _rig.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
            RectTransform pointRect = (RectTransform)_point.transform;
            pointRect.localRotation = Quaternion.Euler(0f, 0f, 90f);
            pointRect.localScale = new Vector3(2f, 1f, 1f);

            Vector2 source = new Vector2(0.6f, 0.5f);
            Vector2 baseLocal = _rig.NormalizedToLocal(source);
            Vector2 result = _rig.CalculateDeformedLocalPoint(source);

            Assert.That(result.y, Is.GreaterThan(baseLocal.y + 5f));
            Assert.That(result.x, Is.LessThan(baseLocal.x));
        }

        [Test]
        public void DisabledPoint_HasNoInfluenceAndReenableRestoresIt()
        {
            _rig.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
            RectTransform pointRect = (RectTransform)_point.transform;
            pointRect.anchoredPosition += new Vector2(20f, 0f);

            Assert.That(_rig.CalculateDeformedLocalPoint(new Vector2(0.5f, 0.5f)).x, Is.EqualTo(20f).Within(0.01f));

            _point.enabled = false;
            Assert.That(_point.CalculateWeight(new Vector2(0.5f, 0.5f)), Is.Zero);
            Assert.That(_rig.CalculateDeformedLocalPoint(new Vector2(0.5f, 0.5f)).x, Is.Zero.Within(0.01f));

            _point.enabled = true;
            Assert.That(_point.CalculateWeight(new Vector2(0.5f, 0.5f)), Is.GreaterThan(0f));
        }

        [Test]
        public void ProceduralPose_ComposesOnTopOfAnimatorFriendlyTransform()
        {
            _rig.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
            RectTransform pointRect = (RectTransform)_point.transform;
            pointRect.anchoredPosition += new Vector2(10f, 0f);
            _point.SetProceduralPose(new Vector2(5f, 4f), 0f, Vector2.one);

            Vector2 result = _rig.CalculateDeformedLocalPoint(new Vector2(0.5f, 0.5f));
            Assert.That(result.x, Is.EqualTo(15f).Within(0.01f));
            Assert.That(result.y, Is.EqualTo(4f).Within(0.01f));

            _point.ClearProceduralPose();
            result = _rig.CalculateDeformedLocalPoint(new Vector2(0.5f, 0.5f));
            Assert.That(result.x, Is.EqualTo(10f).Within(0.01f));
            Assert.That(result.y, Is.Zero.Within(0.01f));
        }

        [Test]
        public void CapturedDirectPoint_UsesResponsiveNormalizedAnchors()
        {
            RectTransform rootRect = (RectTransform)_root.transform;
            RectTransform pointRect = (RectTransform)_point.transform;
            pointRect.anchoredPosition = new Vector2(60f, 20f);
            _point.CaptureRestPose(_rig);
            Vector2 restCenter = _point.RestCenterNormalized;

            Assert.That(pointRect.anchorMin, Is.EqualTo(restCenter));
            Assert.That(pointRect.anchorMax, Is.EqualTo(restCenter));
            Assert.That(pointRect.anchoredPosition.magnitude, Is.LessThan(0.01f));

            rootRect.sizeDelta = new Vector2(400f, 200f);
            Vector2 afterResize = _rig.WorldToNormalized(pointRect.position);
            Assert.That(afterResize.x, Is.EqualTo(restCenter.x).Within(0.001f));
            Assert.That(afterResize.y, Is.EqualTo(restCenter.y).Within(0.001f));
        }
    }
}
