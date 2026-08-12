using Neo.UI;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tests.UI
{
    public sealed class UIMeshRigMotionTests
    {
        [Test]
        public void Evaluator_UsesIndependentCurvesAmplitudesPhaseAndScaleIdentity()
        {
            UIMeshRigMotionProfile profile = new UIMeshRigMotionProfile
            {
                Duration = 2f,
                PositionAmplitudePixels = new Vector2(10f, 20f),
                RotationAmplitudeDegrees = 30f,
                ScaleAmplitude = new Vector2(0.1f, 0.2f),
                PositionX = AnimationCurve.Linear(0f, 0f, 1f, 1f),
                PositionY = AnimationCurve.Linear(0f, 1f, 1f, 0f),
                Rotation = AnimationCurve.Linear(0f, -1f, 1f, 1f),
                ScaleX = AnimationCurve.Linear(0f, 0f, 1f, 1f),
                ScaleY = AnimationCurve.Linear(0f, 0f, 1f, -1f)
            };

            UIMeshRigProceduralPose pose = UIMeshRigMotionEvaluator.Evaluate(profile, 0.5f, 1f, 0f);

            Assert.That(pose.Position.x, Is.EqualTo(2.5f).Within(0.001f));
            Assert.That(pose.Position.y, Is.EqualTo(15f).Within(0.001f));
            Assert.That(pose.RotationDegrees, Is.EqualTo(-15f).Within(0.001f));
            Assert.That(pose.Scale.x, Is.EqualTo(1.025f).Within(0.001f));
            Assert.That(pose.Scale.y, Is.EqualTo(0.95f).Within(0.001f));
        }

        [TestCase(UIMeshRigMotionPreset.Float)]
        [TestCase(UIMeshRigMotionPreset.Breathe)]
        [TestCase(UIMeshRigMotionPreset.BodySway)]
        [TestCase(UIMeshRigMotionPreset.HeadSway)]
        [TestCase(UIMeshRigMotionPreset.SoftJiggle)]
        [TestCase(UIMeshRigMotionPreset.Pulse)]
        [TestCase(UIMeshRigMotionPreset.SquashStretch)]
        [TestCase(UIMeshRigMotionPreset.Wave)]
        [TestCase(UIMeshRigMotionPreset.Noise)]
        public void Preset_CreatesIndependentEditableCurves(UIMeshRigMotionPreset preset)
        {
            UIMeshRigMotionProfile first = UIMeshRigMotionPresets.Create(preset);
            UIMeshRigMotionProfile second = UIMeshRigMotionPresets.Create(preset);

            Assert.That(first.Duration, Is.GreaterThan(0f));
            Assert.That(HasVisibleMotion(first), Is.True);
            Assert.That(first.PositionX, Is.Not.SameAs(second.PositionX));

            Keyframe[] firstKeys = first.PositionX.keys;
            if (firstKeys.Length > 0)
            {
                firstKeys[0].value = 99f;
                first.PositionX.keys = firstKeys;
                Assert.That(second.PositionX.keys[0].value, Is.Not.EqualTo(99f));
            }
        }

        [Test]
        public void Wave_UsesPointPositionAsTravelPhase()
        {
            UIMeshRigMotionProfile profile = UIMeshRigMotionPresets.Create(UIMeshRigMotionPreset.Wave);
            Vector2 firstPosition = new Vector2(0.1f, 0.5f);
            Vector2 secondPosition = new Vector2(0.5f, 0.5f);

            UIMeshRigProceduralPose first = UIMeshRigMotionEvaluator.Evaluate(
                profile, 0.35f, 1f, 0f, firstPosition, 0);
            UIMeshRigProceduralPose second = UIMeshRigMotionEvaluator.Evaluate(
                profile, 0.35f, 1f, 0f, secondPosition, 0);
            float phaseDelta = Vector2.Dot(secondPosition - firstPosition, profile.SpatialPhaseCycles);
            UIMeshRigProceduralPose travelled = UIMeshRigMotionEvaluator.Evaluate(
                profile, 0.35f + phaseDelta * profile.Duration, 1f, 0f, firstPosition, 0);

            Assert.That(first.Position.y, Is.Not.EqualTo(second.Position.y).Within(0.01f));
            Assert.That(second.Position.y, Is.EqualTo(travelled.Position.y).Within(0.001f));
        }

        [Test]
        public void Noise_IsSmoothDeterministicSeededAndDifferentPerPoint()
        {
            UIMeshRigMotionProfile profile = UIMeshRigMotionPresets.Create(UIMeshRigMotionPreset.Noise);
            Vector2 firstPosition = new Vector2(0.2f, 0.35f);
            Vector2 secondPosition = new Vector2(0.8f, 0.7f);

            UIMeshRigProceduralPose first = UIMeshRigMotionEvaluator.Evaluate(
                profile, 1.25f, 1f, 0f, firstPosition, 42);
            UIMeshRigProceduralPose repeated = UIMeshRigMotionEvaluator.Evaluate(
                profile, 1.25f, 1f, 0f, firstPosition, 42);
            UIMeshRigProceduralPose otherSeed = UIMeshRigMotionEvaluator.Evaluate(
                profile, 1.25f, 1f, 0f, firstPosition, 43);
            UIMeshRigProceduralPose otherPoint = UIMeshRigMotionEvaluator.Evaluate(
                profile, 1.25f, 1f, 0f, secondPosition, 42);
            UIMeshRigProceduralPose nearby = UIMeshRigMotionEvaluator.Evaluate(
                profile, 1.251f, 1f, 0f, firstPosition, 42);

            Assert.That(repeated.Position, Is.EqualTo(first.Position));
            Assert.That(repeated.RotationDegrees, Is.EqualTo(first.RotationDegrees));
            Assert.That(otherSeed.Position, Is.Not.EqualTo(first.Position));
            Assert.That(otherPoint.Position, Is.Not.EqualTo(first.Position));
            Assert.That(Vector2.Distance(nearby.Position, first.Position), Is.LessThan(0.02f));
        }

        [Test]
        public void Evaluator_DoesNotAllocatePerSample()
        {
            UIMeshRigMotionProfile wave = UIMeshRigMotionPresets.Create(UIMeshRigMotionPreset.Wave);
            UIMeshRigMotionProfile noise = UIMeshRigMotionPresets.Create(UIMeshRigMotionPreset.Noise);
            Vector2 pointPosition = new Vector2(0.37f, 0.61f);
            UIMeshRigMotionEvaluator.Evaluate(wave, 0f, 1f, 0f, pointPosition, 12);
            UIMeshRigMotionEvaluator.Evaluate(noise, 0f, 1f, 0f, pointPosition, 12);

            long before = System.GC.GetAllocatedBytesForCurrentThread();
            int index;
            for (index = 0; index < 1000; index++)
            {
                float time = index * 0.01f;
                UIMeshRigMotionEvaluator.Evaluate(wave, time, 1f, 0f, pointPosition, 12);
                UIMeshRigMotionEvaluator.Evaluate(noise, time, 1f, 0f, pointPosition, 12);
            }
            long after = System.GC.GetAllocatedBytesForCurrentThread();

            Assert.That(after - before, Is.EqualTo(0L));
        }

        [Test]
        public void Stop_IsIdempotentAndRestoresIdentityPose()
        {
            GameObject pointObject = new GameObject(
                "Motion Point",
                typeof(RectTransform),
                typeof(UIMeshRigPoint),
                typeof(UIMeshRigPointMotion));
            UIMeshRigPointMotion motion = pointObject.GetComponent<UIMeshRigPointMotion>();

            try
            {
                motion.ApplyPreset(UIMeshRigMotionPreset.Float);
                motion.Restart();
                motion.SetTime(0.7f);
                motion.Stop();
                motion.Stop();

                Assert.That(motion.IsPlaying, Is.False);
                Assert.That(motion.IsPaused, Is.False);
                Assert.That(motion.CurrentTime, Is.EqualTo(0f));
                Assert.That(motion.CurrentPose.Position, Is.EqualTo(Vector2.zero));
                Assert.That(motion.CurrentPose.RotationDegrees, Is.EqualTo(0f));
                Assert.That(motion.CurrentPose.Scale, Is.EqualTo(Vector2.one));
            }
            finally
            {
                Object.DestroyImmediate(pointObject);
            }
        }

        [TestCase(UIMeshRigLayoutPreset.SimpleBounce, 1)]
        [TestCase(UIMeshRigLayoutPreset.Character, 4)]
        [TestCase(UIMeshRigLayoutPreset.FlagCloth, 4)]
        public void LayoutPreset_ProvidesCompleteAnimatedPointData(
            UIMeshRigLayoutPreset preset,
            int expectedPointCount)
        {
            Assert.That(UIMeshRigLayoutPresets.GetPointCount(preset), Is.EqualTo(expectedPointCount));

            int index;
            for (index = 0; index < expectedPointCount; index++)
            {
                UIMeshRigPointLayout point = UIMeshRigLayoutPresets.GetPoint(preset, index);
                Assert.That(point.Name, Is.Not.Empty);
                Assert.That(point.MotionPreset, Is.Not.EqualTo(UIMeshRigMotionPreset.Custom));
                Assert.That(point.OuterRadiusNormalized.x, Is.GreaterThanOrEqualTo(point.InnerRadiusNormalized.x));
                Assert.That(point.OuterRadiusNormalized.y, Is.GreaterThanOrEqualTo(point.InnerRadiusNormalized.y));
            }
        }

        [Test]
        public void LayoutBuilder_AppliesCharacterWithMotionComponents()
        {
            GameObject rigObject = new GameObject(
                "Layout Rig",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(UIMeshRigGraphic));
            UIMeshRigGraphic rig = rigObject.GetComponent<UIMeshRigGraphic>();

            try
            {
                UIMeshRigPoint[] points = UIMeshRigLayoutBuilder.Apply(
                    rig,
                    UIMeshRigLayoutPreset.Character,
                    true,
                    false);

                Assert.That(points.Length, Is.EqualTo(4));
                Assert.That(rig.Points.Count, Is.EqualTo(4));
                Assert.That(points[0].BindingKey, Is.EqualTo("Root Sway"));
                Assert.That(points[3].BindingKey, Is.EqualTo("Head"));
                int index;
                for (index = 0; index < points.Length; index++)
                {
                    Assert.That(points[index].GetComponent<UIMeshRigPointMotion>(), Is.Not.Null);
                }
            }
            finally
            {
                Object.DestroyImmediate(rigObject);
            }
        }

        private static bool HasVisibleMotion(UIMeshRigMotionProfile profile)
        {
            return profile.PositionAmplitudePixels.sqrMagnitude > 0f ||
                   Mathf.Abs(profile.RotationAmplitudeDegrees) > 0f ||
                   profile.ScaleAmplitude.sqrMagnitude > 0f;
        }
    }
}
