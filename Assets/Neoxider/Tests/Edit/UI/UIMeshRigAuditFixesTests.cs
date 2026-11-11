using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.UI;
using Neo.UI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Tests.UI
{
    /// <summary>
    ///     Pins the fixes of the 2026-11-11 UI Mesh Rig audit: Animation previews never rewrite a bind, Preserve
    ///     Aspect resizes keep points on their binds, editor ticks rebuild canvas-free rigs, unchanged poses cost
    ///     nothing, and the UI Toolkit element has a code path for posing points.
    /// </summary>
    public sealed class UIMeshRigAuditFixesTests
    {
        private readonly List<Object> _objects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            if (AnimationMode.InAnimationMode())
            {
                AnimationMode.StopAnimationMode();
            }

            UIMeshRigClipPreviewDriver.StopPreview();
            for (int index = _objects.Count - 1; index >= 0; index--)
            {
                if (_objects[index] != null)
                {
                    Object.DestroyImmediate(_objects[index]);
                }
            }

            _objects.Clear();
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void SetupRebind_IsSkippedWhileAnimationModeSamplesAClip_AndRunsAgainOutsideIt(int adapter)
        {
            Assume.That(AnimationMode.InAnimationMode(), Is.False, "Another Animation preview is running.");
            IUIMeshRigOwner owner = CreateOwner(adapter, true);
            UIMeshRigPoint point = CreatePoint(owner, Vector2.zero);
            RectTransform pointRect = (RectTransform)point.transform;
            bool usesPosition = adapter == 0;
            string property = usesPosition ? "m_AnchoredPosition.x" : "m_LocalScale.x";
            float sampledValue = usesPosition ? 14f : 1.5f;
            AnimationClip clip = Track(new AnimationClip());
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve("Point", typeof(RectTransform), property),
                AnimationCurve.Constant(0f, 1f, sampledValue));
            Vector2 restCenter = point.RestCenterNormalized;
            Vector3 restScale = point.RestScale;
            Vector2 anchor = pointRect.anchorMin;

            try
            {
                AnimationMode.StartAnimationMode();
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(owner.RigTransform.gameObject, clip, 0.75f);
                AnimationMode.EndSampling();
                pointRect.hasChanged = true;
                InvokeSynchronize(owner);

                float sampled = usesPosition ? pointRect.anchoredPosition.x : pointRect.localScale.x;
                Assert.That(sampled, Is.EqualTo(sampledValue).Within(0.001f), "The clip sample must reach the point.");
                Assert.That(point.RestCenterNormalized, Is.EqualTo(restCenter),
                    "A sampled clip must not be captured as the bind centre.");
                Assert.That(point.RestScale, Is.EqualTo(restScale));
                Assert.That(pointRect.anchorMin, Is.EqualTo(anchor));
                if (owner is UIMeshRigGraphic graphic)
                {
                    Vector2 deformed = graphic.CalculateDeformedLocalPoint(restCenter);
                    Assert.That(deformed.x - graphic.NormalizedToLocal(restCenter).x, Is.EqualTo(14f).Within(0.05f),
                        "A previewed rig deforms as posed even in Setup.");
                }
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }

            if (usesPosition)
            {
                pointRect.anchoredPosition += new Vector2(14f, 0f);
            }
            else
            {
                pointRect.localScale = new Vector3(1.5f, 1f, 1f);
            }

            pointRect.hasChanged = true;
            InvokeSynchronize(owner);

            if (usesPosition)
            {
                Assert.That(point.RestCenterNormalized.x, Is.GreaterThan(restCenter.x + 0.01f),
                    "Outside a preview a Setup move still rebinds the point.");
            }
            else
            {
                Assert.That(point.RestScale.x, Is.EqualTo(1.5f).Within(0.001f),
                    "Outside a preview a Setup scale still rebinds the point.");
            }
        }

        [Test]
        public void ClipPreview_SamplesTheAnimatorClip_AndStopRestoresThePointAndBind()
        {
            Assume.That(AnimationMode.InAnimationMode(), Is.False, "Another Animation preview is running.");
            UIMeshRigGraphic rig = (UIMeshRigGraphic)CreateOwner(0, true);
            UIMeshRigPoint point = CreatePoint(rig, Vector2.zero);
            RectTransform pointRect = (RectTransform)point.transform;
            Vector2 restCenter = point.RestCenterNormalized;
            AnimationClip clip = Track(new AnimationClip());
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve("Point", typeof(RectTransform), "m_AnchoredPosition.y"),
                AnimationCurve.Linear(0f, 0f, 1f, 20f));

            try
            {
                Assert.That(UIMeshRigClipPreviewDriver.StartPreview(rig, clip), Is.True);
                Assert.That(UIMeshRigClipPreviewDriver.IsPreviewing(rig), Is.True);
                Assert.That(UIMeshRigClipPreviewDriver.IsUpdateSubscribed, Is.True);

                UIMeshRigClipPreviewDriver.TickForTests(0.5f);
                InvokeSynchronize(rig);

                Assert.That(pointRect.anchoredPosition.y, Is.EqualTo(10f).Within(0.05f));
                Assert.That(point.RestCenterNormalized, Is.EqualTo(restCenter));
            }
            finally
            {
                UIMeshRigClipPreviewDriver.StopPreview();
            }

            Assert.That(pointRect.anchoredPosition.y, Is.EqualTo(0f).Within(0.001f),
                "Stopping the preview must restore every sampled property.");
            Assert.That(AnimationMode.InAnimationMode(), Is.False);
            Assert.That(UIMeshRigClipPreviewDriver.IsUpdateSubscribed, Is.False);
            Assert.That(point.RestCenterNormalized, Is.EqualTo(restCenter));
        }

        [TestCase(UIMeshRigAuthoringMode.Setup)]
        [TestCase(UIMeshRigAuthoringMode.Pose)]
        public void PreserveAspectResize_KeepsDirectPointOnItsBindCentre(UIMeshRigAuthoringMode mode)
        {
            UIMeshRigGraphic rig = (UIMeshRigGraphic)CreateOwner(0, false);
            RectTransform rigRect = rig.rectTransform;
            rig.SetSource(CreateSprite(8, 8), Color.white);
            UIMeshRigPoint point = CreatePoint(rig, new Vector2(30f, 10f));
            RectTransform pointRect = (RectTransform)point.transform;
            Vector2 bind = point.RestCenterNormalized;
            Assert.That(bind.x, Is.EqualTo(0.8f).Within(0.0001f), "The bind lives in the letterboxed drawing rect.");
            Assert.That(bind.y, Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(pointRect.anchoredPosition.magnitude, Is.LessThan(0.001f),
                "A captured direct point rests on its anchor.");
            rig.SetAuthoringMode(mode);

            rigRect.sizeDelta = new Vector2(400f, 100f);
            rig.SynchronizeDirectChildAnchors();
            InvokeSynchronize(rig);

            Vector2 actual = rig.WorldToNormalized(pointRect.position);
            Assert.That(actual.x, Is.EqualTo(bind.x).Within(0.001f));
            Assert.That(actual.y, Is.EqualTo(bind.y).Within(0.001f));
            Assert.That(point.RestCenterNormalized.x, Is.EqualTo(bind.x).Within(0.001f),
                "Setup must not rebind a point the resize moved.");
            Vector2 rest = rig.NormalizedToLocal(bind);
            Vector2 deformed = rig.CalculateDeformedLocalPoint(bind);
            Assert.That(deformed.x, Is.EqualTo(rest.x).Within(0.01f), "A resized rig at rest must not deform.");
            Assert.That(deformed.y, Is.EqualTo(rest.y).Within(0.01f));
        }

        [Test]
        public void LegacyLetterboxAnchors_KeepTheirRestOffsetAcrossResize()
        {
            UIMeshRigGraphic rig = (UIMeshRigGraphic)CreateOwner(0, false);
            rig.SetSource(CreateSprite(8, 8), Color.white);
            rig.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
            GameObject pointObject = Track(new GameObject("Point", typeof(RectTransform), typeof(UIMeshRigPoint)));
            RectTransform pointRect = (RectTransform)pointObject.transform;
            pointRect.SetParent(rig.transform, false);
            UIMeshRigPoint point = pointObject.GetComponent<UIMeshRigPoint>();
            Vector2 bind = new Vector2(0.8f, 0.6f);
            // WHY: rigs captured before 12.3 wrote the bind centre itself into the anchors, which leaves a
            // letterbox-sized rest anchoredPosition that existing clips were keyed against.
            pointRect.anchorMin = bind;
            pointRect.anchorMax = bind;
            point.SetRestCenterNormalized(bind);
            pointRect.position = rig.NormalizedToWorld(bind);
            Vector2 restOffset = pointRect.anchoredPosition;
            Assert.That(restOffset.x, Is.EqualTo(-30f).Within(0.01f));

            rig.rectTransform.sizeDelta = new Vector2(400f, 100f);
            rig.SynchronizeDirectChildAnchors();

            Vector2 actual = rig.WorldToNormalized(pointRect.position);
            Assert.That(actual.x, Is.EqualTo(bind.x).Within(0.001f));
            Assert.That(actual.y, Is.EqualTo(bind.y).Within(0.001f));
            Assert.That(pointRect.anchoredPosition.x, Is.EqualTo(restOffset.x).Within(0.001f),
                "The anchors move; the keyed anchoredPosition stays.");
        }

        [Test]
        public void MotionPreviewTick_RebuildsTheWorldMeshWithoutLateUpdate()
        {
            UIMeshRigWorldRenderer rig = (UIMeshRigWorldRenderer)CreateOwner(1, false);
            rig.SetSource(CreateSprite(8, 8), Color.white);
            rig.SetGridResolution(6, 6);
            UIMeshRigPoint point = CreatePoint(rig, Vector2.zero);
            point.RadiusNormalized = new Vector2(0.4f, 0.4f);
            UIMeshRigPointMotion motion = point.gameObject.AddComponent<UIMeshRigPointMotion>();
            motion.Stop();
            motion.Speed = 1f;
            motion.Profile.Duration = 1f;
            motion.Profile.PositionAmplitudePixels = new Vector2(0f, 40f);
            motion.Profile.PositionY = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            rig.RebuildNow();
            MeshFilter filter = rig.GetComponent<MeshFilter>();
            Vector3[] before = filter.sharedMesh.vertices;

            try
            {
                UIMeshRigMotionPreviewDriver.StartPreview(motion);
                UIMeshRigMotionPreviewDriver.TickForTests(0.2f);

                Vector3[] after = filter.sharedMesh.vertices;
                float largest = 0f;
                for (int index = 0; index < before.Length; index++)
                {
                    largest = Mathf.Max(largest, Vector3.Distance(before[index], after[index]));
                }

                Assert.That(largest, Is.GreaterThan(0.01f),
                    "A preview tick must reach the world mesh without waiting for LateUpdate.");
            }
            finally
            {
                UIMeshRigMotionPreviewDriver.StopPreview(motion);
            }
        }

        [Test]
        public void InspectorPresetSelection_CopiesThePresetCurvesAndStartsThePreview()
        {
            GameObject pointObject = Track(new GameObject(
                "Preset Point",
                typeof(RectTransform),
                typeof(UIMeshRigPoint),
                typeof(UIMeshRigPointMotion)));
            UIMeshRigPointMotion motion = pointObject.GetComponent<UIMeshRigPointMotion>();
            motion.ApplyPreset(UIMeshRigMotionPreset.Breathe);
            SerializedObject serializedMotion = new SerializedObject(motion);
            serializedMotion.FindProperty(UIMeshRigPointMotionEditor.PresetPropertyPath).intValue =
                (int)UIMeshRigMotionPreset.Wave;

            try
            {
                UIMeshRigPointMotionEditor.ApplyInspectorPresetSelection(serializedMotion);

                Assert.That(motion.Preset, Is.EqualTo(UIMeshRigMotionPreset.Wave));
                UIMeshRigMotionProfile expected = UIMeshRigMotionPresets.Create(UIMeshRigMotionPreset.Wave);
                for (int step = 0; step <= 8; step++)
                {
                    float time = step * 0.23f;
                    UIMeshRigProceduralPose actualPose = UIMeshRigMotionEvaluator.Evaluate(
                        motion.Profile, time, 1f, 0f, new Vector2(0.3f, 0.6f), 4);
                    UIMeshRigProceduralPose expectedPose = UIMeshRigMotionEvaluator.Evaluate(
                        expected, time, 1f, 0f, new Vector2(0.3f, 0.6f), 4);
                    Assert.That(actualPose.Position, Is.EqualTo(expectedPose.Position));
                    Assert.That(actualPose.RotationDegrees, Is.EqualTo(expectedPose.RotationDegrees));
                    Assert.That(actualPose.Scale, Is.EqualTo(expectedPose.Scale));
                }

                Assert.That(motion.PreviewInEditMode, Is.True, "Picking a preset starts the live preview.");
            }
            finally
            {
                UIMeshRigMotionPreviewDriver.StopPreview(motion);
            }
        }

        [Test]
        public void Strength_WidensTheFullAreaButNeverMovesAVertexFurtherThanThePoint()
        {
            UIMeshRigCoordinateSpace space = new UIMeshRigCoordinateSpace(Vector2.zero, new Vector2(200f, 100f));
            Vector2 center = new Vector2(0.5f, 0.5f);
            Vector2 between = new Vector2(0.7f, 0.5f);

            float centerNormal = Displacement(space, center, 1f);
            float centerStrong = Displacement(space, center, 3f);
            float betweenNormal = Displacement(space, between, 1f);
            float betweenStrong = Displacement(space, between, 3f);

            Assert.That(centerNormal, Is.EqualTo(10f).Within(0.001f));
            Assert.That(centerStrong, Is.EqualTo(10f).Within(0.001f),
                "Strength above 1 does not move the centre further than the point moved.");
            Assert.That(betweenNormal, Is.EqualTo(4f).Within(0.01f));
            Assert.That(betweenStrong, Is.GreaterThan(betweenNormal), "Strength above 1 widens the full area.");
            Assert.That(betweenStrong, Is.LessThanOrEqualTo(10.001f));
        }

        [Test]
        public void Build_WithContentBounds_SpansTheInnerRectButTheWholeUvRect()
        {
            UIMeshRigGeometry geometry = UIMeshRigGeometryBuilder.Build(
                2,
                2,
                new UIMeshRigCoordinateSpace(Vector2.zero, new Vector2(100f, 50f)),
                new Rect(0.2f, 0.1f, 0.5f, 0.6f),
                Rect.MinMaxRect(0.1f, 0.2f, 0.9f, 0.6f),
                false,
                null,
                null);

            Assert.That(geometry.Vertices[0].x, Is.EqualTo(10f).Within(0.0001f));
            Assert.That(geometry.Vertices[0].y, Is.EqualTo(10f).Within(0.0001f));
            Assert.That(geometry.Vertices[8].x, Is.EqualTo(90f).Within(0.0001f));
            Assert.That(geometry.Vertices[8].y, Is.EqualTo(30f).Within(0.0001f));
            Assert.That(geometry.UV[0].x, Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(geometry.UV[0].y, Is.EqualTo(0.1f).Within(0.0001f));
            Assert.That(geometry.UV[8].x, Is.EqualTo(0.7f).Within(0.0001f));
            Assert.That(geometry.UV[8].y, Is.EqualTo(0.7f).Within(0.0001f));
        }

        [Test]
        public void Build_WithUnitBounds_MatchesTheOverloadWithoutBounds()
        {
            List<UIMeshRigPointState> points = new List<UIMeshRigPointState>
            {
                new UIMeshRigPointState(true, new Vector2(0.4f, 0.5f), new Vector2(0.1f, 0.1f),
                    new Vector2(0.3f, 0.4f), 1f, null, new Vector2(52f, 20f), 12f, new Vector2(1.1f, 0.9f))
            };
            UIMeshRigCoordinateSpace space = new UIMeshRigCoordinateSpace(new Vector2(-50f, -25f), new Vector2(100f, 50f));
            Rect uv = new Rect(0f, 0f, 1f, 1f);

            UIMeshRigGeometry plain = UIMeshRigGeometryBuilder.Build(5, 4, space, uv, true, points, null);
            UIMeshRigGeometry bounded = UIMeshRigGeometryBuilder.Build(
                5, 4, space, uv, UIMeshRigGeometryBuilder.UnitRect, true, points, null);

            CollectionAssert.AreEqual(plain.Vertices, bounded.Vertices);
            CollectionAssert.AreEqual(plain.UV, bounded.UV);
            CollectionAssert.AreEqual(plain.Indices, bounded.Indices);
        }

        [Test]
        public void UntrimmedSprite_HasUnitContentBoundsAndCanBeDeformedBySpriteRenderer()
        {
            Sprite sprite = CreateSprite(16, 8);

            Assert.That(UIMeshRigGeometryBuilder.GetSpriteContentBounds(sprite), Is.EqualTo(UIMeshRigGeometryBuilder.UnitRect));
            Assert.That(UIMeshRigGeometryBuilder.GetSpriteContentBounds(null), Is.EqualTo(UIMeshRigGeometryBuilder.UnitRect));
            Assert.That(UIMeshRigSpriteRenderer.CanDeform(sprite), Is.True);
            Assert.That(UIMeshRigSpriteRenderer.CanDeform(null), Is.False);
        }

        [Test]
        public void ToolkitElementPointPose_MovesItsPointInElementPixelsOnTopOfTheMotion()
        {
            UIMeshRigElement element = new UIMeshRigElement
            {
                Sprite = CreateSprite(8, 8),
                Columns = 4,
                Rows = 4,
                PreserveAspect = false,
                LayoutPreset = UIMeshRigLayoutPreset.SimpleBounce,
                MotionEnabled = false
            };
            Vector2 size = new Vector2(200f, 100f);
            const int centerVertex = 2 * 5 + 2;
            Assert.That(element.PointCount, Is.EqualTo(1));
            Vector3[] rest = (Vector3[])element.BuildGeometry(size, 0f).Vertices.Clone();

            element.SetPointPose(0, new Vector2(10f, 6f), 0f, Vector2.one);
            Vector3[] posed = element.BuildGeometry(size, 0f).Vertices;

            Assert.That(posed[centerVertex].x - rest[centerVertex].x, Is.EqualTo(10f).Within(0.001f));
            Assert.That(posed[centerVertex].y - rest[centerVertex].y, Is.EqualTo(-6f).Within(0.001f),
                "Offsets are y up; the panel is y down.");

            element.ClearPointPose(0);
            CollectionAssert.AreEqual(rest, element.BuildGeometry(size, 0f).Vertices);

            element.SetPointPose(0, new Vector2(10f, 6f), 0f, Vector2.one);
            element.LayoutPreset = UIMeshRigLayoutPreset.Character;
            element.LayoutPreset = UIMeshRigLayoutPreset.SimpleBounce;
            CollectionAssert.AreEqual(rest, element.BuildGeometry(size, 0f).Vertices,
                "Changing the layout clears every point pose.");
            Assert.Throws<ArgumentOutOfRangeException>(() => element.SetPointPose(1, Vector2.zero, 0f, Vector2.one));
        }

        [Test]
        public void ToolkitHost_AnimatedMotionFieldsReachTheElement_AndHostSettersSurviveOtherSetters()
        {
            GameObject hostObject = Track(new GameObject("Toolkit Host", typeof(UIMeshRigUIToolkitHost)));
            UIMeshRigUIToolkitHost host = hostObject.GetComponent<UIMeshRigUIToolkitHost>();
            Assert.That(host.Element, Is.Not.Null);

            FieldInfo speedField = typeof(UIMeshRigUIToolkitHost).GetField(
                "_motionSpeed", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo animationApplied = typeof(UIMeshRigUIToolkitHost).GetMethod(
                "OnDidApplyAnimationProperties", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(speedField, Is.Not.Null);
            Assert.That(animationApplied, Is.Not.Null);
            speedField.SetValue(host, 2.5f);
            animationApplied.Invoke(host, null);
            Assert.That(host.Element.MotionSpeed, Is.EqualTo(2.5f), "An Animator key on the host reaches the element.");

            host.SetMotionPhase(0.3f);
            host.SetMotionEnabled(false);
            host.SetSize(new Vector2(120f, 80f));
            Assert.That(host.Element.MotionPhase, Is.EqualTo(0.3f));
            Assert.That(host.Element.MotionEnabled, Is.False);
            Assert.That(host.Element.MotionSpeed, Is.EqualTo(2.5f));
            Assert.That(host.MotionSpeed, Is.EqualTo(2.5f));
        }

        [Test]
        public void UnchangedProceduralPose_DoesNotDirtyTheOwnerAgain()
        {
            UIMeshRigWorldRenderer rig = (UIMeshRigWorldRenderer)CreateOwner(1, false);
            rig.SetSource(CreateSprite(8, 8), Color.white);
            rig.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
            UIMeshRigPoint point = CreatePoint(rig, Vector2.zero);
            FieldInfo dirtyField = typeof(UIMeshRigWorldRenderer).GetField(
                "_geometryDirty", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(dirtyField, Is.Not.Null);
            rig.RebuildNow();

            point.SetProceduralPose(new Vector2(3f, 1f), 4f, Vector2.one);
            Assert.That((bool)dirtyField.GetValue(rig), Is.True);
            rig.RebuildNow();
            Assert.That((bool)dirtyField.GetValue(rig), Is.False);

            point.SetProceduralPose(new Vector2(3f, 1f), 4f, Vector2.one);
            Assert.That((bool)dirtyField.GetValue(rig), Is.False, "The same pose again must not rebuild the mesh.");

            point.SetProceduralPose(new Vector2(3f, 2f), 4f, Vector2.one);
            Assert.That((bool)dirtyField.GetValue(rig), Is.True);
            rig.RebuildNow();

            point.ClearProceduralPose();
            Assert.That((bool)dirtyField.GetValue(rig), Is.True);
            rig.RebuildNow();
            point.ClearProceduralPose();
            Assert.That((bool)dirtyField.GetValue(rig), Is.False, "Clearing an empty pose must not rebuild the mesh.");
        }

        [Test]
        public void MotionResetAndSetters_LeaveNoFrozenProceduralPoseOnAStoppedMotion()
        {
            GameObject pointObject = Track(new GameObject("Reset Point", typeof(RectTransform), typeof(UIMeshRigPoint)));
            UIMeshRigPoint point = pointObject.GetComponent<UIMeshRigPoint>();
            UIMeshRigPointMotion motion = pointObject.AddComponent<UIMeshRigPointMotion>();
            MethodInfo reset = typeof(UIMeshRigPointMotion).GetMethod("Reset", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(reset, Is.Not.Null);

            reset.Invoke(motion, null);
            Assert.That(motion.Preset, Is.EqualTo(UIMeshRigMotionPreset.Breathe));
            Assert.That(point.HasProceduralPose, Is.False, "Reset must not freeze a pose on the point.");

            motion.Phase = 0.4f;
            motion.Seed = 9;
            motion.ApplyPreset(UIMeshRigMotionPreset.Wave);
            Assert.That(point.HasProceduralPose, Is.False);

            motion.EvaluateAt(0.3f);
            Assert.That(point.HasProceduralPose, Is.True, "EvaluateAt stays the explicit way to apply a pose.");
            motion.Phase = 0.1f;
            Assert.That(point.HasProceduralPose, Is.True);
            motion.Stop();
            Assert.That(point.HasProceduralPose, Is.False);
        }

        [Test]
        public void PoseHandleMove_WritesOnlyTheDraggedAxes()
        {
            UIMeshRigGraphic rig = (UIMeshRigGraphic)CreateOwner(0, false);
            rig.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
            UIMeshRigPoint point = CreatePoint(rig, new Vector2(12.5f, -7.25f));
            RectTransform pointRect = (RectTransform)point.transform;
            pointRect.localRotation = Quaternion.Euler(0f, 0f, 20f);
            pointRect.localScale = new Vector3(1.2f, 0.9f, 1f);
            Vector2 anchored = pointRect.anchoredPosition;
            Quaternion rotation = pointRect.localRotation;
            Vector3 scale = pointRect.localScale;

            UIMeshRigSceneHandles.ApplyPoseHandle(
                rig,
                point,
                UIMeshRigSceneTool.Move,
                pointRect.position + rig.transform.right * 15f,
                pointRect.rotation,
                pointRect.localScale);

            Assert.That(pointRect.anchoredPosition.x, Is.EqualTo(anchored.x + 15f).Within(0.001f));
            Assert.That(pointRect.anchoredPosition.y, Is.EqualTo(anchored.y), "An untouched axis keeps its exact value.");
            Assert.That(pointRect.localRotation, Is.EqualTo(rotation));
            Assert.That(pointRect.localScale, Is.EqualTo(scale));
        }

        [Test]
        public void PoseHandleRotateAndScale_KeepOutOfPlaneChannelsUntouched()
        {
            UIMeshRigGraphic rig = (UIMeshRigGraphic)CreateOwner(0, false);
            rig.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
            UIMeshRigPoint point = CreatePoint(rig, new Vector2(20f, 5f));
            RectTransform pointRect = (RectTransform)point.transform;
            Vector2 anchored = pointRect.anchoredPosition;

            UIMeshRigSceneHandles.ApplyPoseHandle(
                rig,
                point,
                UIMeshRigSceneTool.Rotate,
                pointRect.position,
                Quaternion.AngleAxis(30f, rig.transform.forward) * pointRect.rotation,
                pointRect.localScale);

            Vector3 euler = pointRect.localEulerAngles;
            Assert.That(Mathf.DeltaAngle(0f, euler.x), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(Mathf.DeltaAngle(0f, euler.y), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(Mathf.DeltaAngle(0f, euler.z), Is.EqualTo(30f).Within(0.01f));
            Assert.That(pointRect.anchoredPosition, Is.EqualTo(anchored));

            Vector3 scale = pointRect.localScale;
            UIMeshRigSceneHandles.ApplyPoseHandle(
                rig,
                point,
                UIMeshRigSceneTool.Scale,
                pointRect.position,
                pointRect.rotation,
                new Vector3(scale.x * 1.5f, scale.y, 7f));

            Assert.That(pointRect.localScale.x, Is.EqualTo(scale.x * 1.5f).Within(0.0001f));
            Assert.That(pointRect.localScale.y, Is.EqualTo(scale.y));
            Assert.That(pointRect.localScale.z, Is.EqualTo(scale.z), "The rig ignores Z scale; the handle must not key it.");
        }

        [Test]
        public void EvaluateInfluence_MatchesTheFullEllipseTestEverywhere()
        {
            Vector2[] outerRadii = { new Vector2(0.2f, 0.3f), new Vector2(0.05f, 0.4f), new Vector2(0.005f, 0.005f) };
            Vector2[] innerRadii = { new Vector2(0.1f, 0.05f), Vector2.zero, new Vector2(0.002f, 0.004f) };
            AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            for (int radiusIndex = 0; radiusIndex < outerRadii.Length; radiusIndex++)
            {
                UIMeshRigPointState state = new UIMeshRigPointState(
                    true, new Vector2(0.45f, 0.55f), innerRadii[radiusIndex], outerRadii[radiusIndex], 1.5f, curve,
                    Vector2.zero, 0f, Vector2.one);
                for (int y = 0; y <= 40; y++)
                {
                    for (int x = 0; x <= 40; x++)
                    {
                        Vector2 position = new Vector2(x / 40f, y / 40f);
                        Assert.That(
                            UIMeshRigGeometryBuilder.EvaluateInfluence(state, position),
                            Is.EqualTo(ReferenceInfluence(state, position)).Within(0.000001f),
                            "Influence at " + position + " for outer " + outerRadii[radiusIndex]);
                    }
                }
            }
        }

        [Test]
        public void SpriteAlphaRaycast_RechecksAfterTheSpriteIsSwappedInTheInspector()
        {
            UIMeshRigGraphic rig = (UIMeshRigGraphic)CreateOwner(0, false);
            rig.SetGridResolution(4, 4);
            Sprite readable = CreateHalfTransparentSprite(true);
            Sprite unreadable = CreateHalfTransparentSprite(false);
            rig.SetSource(unreadable, Color.white);
            rig.SetPreserveAspect(false);
            rig.SetRaycastMode(UIMeshRigRaycastMode.SpriteAlpha, 0.5f);
            Vector2 transparentSide = new Vector2(-50f, 0f);
            Vector2 opaqueSide = new Vector2(50f, 0f);

            Assert.That(rig.Raycast(transparentSide, null), Is.True, "A non-readable texture stays clickable.");

            SerializedObject serializedRig = new SerializedObject(rig);
            serializedRig.FindProperty("_sprite").objectReferenceValue = readable;
            serializedRig.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(rig.Raycast(transparentSide, null), Is.False,
                "A readable sprite assigned in the Inspector must get the alpha test back.");
            Assert.That(rig.Raycast(opaqueSide, null), Is.True);
        }

        private static float Displacement(UIMeshRigCoordinateSpace space, Vector2 normalized, float strength)
        {
            UIMeshRigPointState state = new UIMeshRigPointState(
                true,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.125f, 0.125f),
                new Vector2(0.25f, 0.25f),
                strength,
                AnimationCurve.Linear(0f, 0f, 1f, 1f),
                space.NormalizedToPosition(new Vector2(0.5f, 0.5f)) + new Vector2(10f, 0f),
                0f,
                Vector2.one);
            List<UIMeshRigPointState> points = new List<UIMeshRigPointState> { state };
            Vector2 deformed = UIMeshRigGeometryBuilder.DeformPoint(normalized, space, true, points);
            return deformed.x - space.NormalizedToPosition(normalized).x;
        }

        private static float ReferenceInfluence(UIMeshRigPointState point, Vector2 normalizedPosition)
        {
            Vector2 delta = normalizedPosition - point.RestCenterNormalized;
            float distance = delta.magnitude;
            if (distance <= 0.000001f)
            {
                return point.Strength;
            }

            float outerBoundary = ReferenceBoundary(delta, point.OuterRadiusNormalized);
            if (distance >= outerBoundary)
            {
                return 0f;
            }

            float innerBoundary = ReferenceBoundary(delta, point.InnerRadiusNormalized);
            if (innerBoundary > 0f && distance <= innerBoundary)
            {
                return point.Strength;
            }

            float edgeT = Mathf.InverseLerp(outerBoundary, innerBoundary, distance);
            return Mathf.Clamp01(point.FalloffCurve.Evaluate(edgeT)) * point.Strength;
        }

        private static float ReferenceBoundary(Vector2 direction, Vector2 radius)
        {
            float length = direction.magnitude;
            if (length <= 0.000001f || radius.x <= 0.000001f || radius.y <= 0.000001f)
            {
                return 0f;
            }

            Vector2 unit = direction / length;
            float denominator = Mathf.Sqrt(
                unit.x * unit.x / (radius.x * radius.x) +
                unit.y * unit.y / (radius.y * radius.y));
            return denominator > 0.000001f ? 1f / denominator : 0f;
        }

        private IUIMeshRigOwner CreateOwner(int adapter, bool withAnimator)
        {
            GameObject root;
            switch (adapter)
            {
                case 1:
                    root = Track(new GameObject("World Rig", typeof(MeshFilter), typeof(MeshRenderer),
                        typeof(UIMeshRigWorldRenderer)));
                    break;
                case 2:
                    root = Track(new GameObject("Sprite Rig", typeof(SpriteRenderer), typeof(UIMeshRigSpriteRenderer)));
                    break;
                default:
                    root = Track(new GameObject("Rig", typeof(RectTransform), typeof(CanvasRenderer),
                        typeof(UIMeshRigGraphic)));
                    ((RectTransform)root.transform).sizeDelta = new Vector2(200f, 100f);
                    break;
            }

            if (withAnimator)
            {
                root.AddComponent<Animator>();
            }

            return root.GetComponent<IUIMeshRigOwner>();
        }

        private UIMeshRigPoint CreatePoint(IUIMeshRigOwner owner, Vector2 localPosition)
        {
            GameObject pointObject = Track(new GameObject("Point", typeof(RectTransform), typeof(UIMeshRigPoint)));
            RectTransform pointRect = (RectTransform)pointObject.transform;
            pointRect.SetParent(owner.RigTransform, false);
            pointRect.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
            UIMeshRigPoint point = pointObject.GetComponent<UIMeshRigPoint>();
            point.RadiusNormalized = new Vector2(0.3f, 0.3f);
            point.Falloff = 0.5f;
            point.CaptureRestPose(owner);
            owner.NotifyPointChanged();
            pointRect.hasChanged = false;
            return point;
        }

        private static void InvokeSynchronize(IUIMeshRigOwner owner)
        {
            MethodInfo method = owner.GetType().GetMethod(
                "SynchronizePointTransforms", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(owner, null);
        }

        private Sprite CreateSprite(int width, int height)
        {
            Texture2D texture = Track(new Texture2D(width, height));
            return Track(Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f));
        }

        private Sprite CreateHalfTransparentSprite(bool readable)
        {
            Texture2D texture = Track(new Texture2D(4, 4, TextureFormat.RGBA32, false));
            Color32[] pixels = new Color32[16];
            for (int index = 0; index < pixels.Length; index++)
            {
                byte alpha = index % 4 < 2 ? (byte)0 : (byte)255;
                pixels[index] = new Color32(255, 255, 255, alpha);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, !readable);
            return Track(Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f));
        }

        private T Track<T>(T item) where T : Object
        {
            _objects.Add(item);
            return item;
        }
    }
}
