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
