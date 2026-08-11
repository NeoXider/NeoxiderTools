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
                duration = 2f,
                positionAmplitudePixels = new Vector2(10f, 20f),
                rotationAmplitudeDegrees = 30f,
                scaleAmplitude = new Vector2(0.1f, 0.2f),
                positionX = AnimationCurve.Linear(0f, 0f, 1f, 1f),
                positionY = AnimationCurve.Linear(0f, 1f, 1f, 0f),
                rotation = AnimationCurve.Linear(0f, -1f, 1f, 1f),
                scaleX = AnimationCurve.Linear(0f, 0f, 1f, 1f),
                scaleY = AnimationCurve.Linear(0f, 0f, 1f, -1f)
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
        public void Preset_CreatesIndependentEditableCurves(UIMeshRigMotionPreset preset)
        {
            UIMeshRigMotionProfile first = UIMeshRigMotionPresets.Create(preset);
            UIMeshRigMotionProfile second = UIMeshRigMotionPresets.Create(preset);

            Assert.That(first.duration, Is.GreaterThan(0f));
            Assert.That(HasVisibleMotion(first), Is.True);
            Assert.That(first.positionX, Is.Not.SameAs(second.positionX));

            Keyframe[] firstKeys = first.positionX.keys;
            if (firstKeys.Length > 0)
            {
                firstKeys[0].value = 99f;
                first.positionX.keys = firstKeys;
                Assert.That(second.positionX.keys[0].value, Is.Not.EqualTo(99f));
            }
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

        private static bool HasVisibleMotion(UIMeshRigMotionProfile profile)
        {
            return profile.positionAmplitudePixels.sqrMagnitude > 0f ||
                   Mathf.Abs(profile.rotationAmplitudeDegrees) > 0f ||
                   profile.scaleAmplitude.sqrMagnitude > 0f;
        }
    }
}
