using Neo.Animations;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Animations
{
    public sealed class Audit20261023NeoAnimationsImpulseTests
    {
        [TestCase(0f)]
        [TestCase(0.1f)]
        [TestCase(0.3f)]
        [TestCase(0.4f)]
        public void NullDecayCurve_MatchesExplicitLinearDecay(float impulseTime)
        {
            Vector3 basePosition = new Vector3(1f, 2f, 3f);
            Vector3 baseEuler = new Vector3(10f, 20f, 30f);
            Vector3 baseScale = new Vector3(2f, 3f, 4f);
            TransformAnimationSettings settings = new TransformAnimationSettings
            {
                RotateEnabled = false,
                FloatEnabled = false,
                ScaleEnabled = false,
                ShakeEnabled = false,
                ImpulseDuration = 0.4f,
                ImpulsePositionStrength = 0.3f,
                ImpulseRotationStrength = 15f,
                ImpulseDecayCurve = null
            };

            TransformAnimationState actual = TransformAnimationEvaluator.Evaluate(
                settings, basePosition, baseEuler, baseScale, 0f, 3f, impulseTime);
            settings.ImpulseDecayCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            TransformAnimationState expected = TransformAnimationEvaluator.Evaluate(
                settings, basePosition, baseEuler, baseScale, 0f, 3f, impulseTime);

            Assert.That(Vector3.Distance(actual.LocalPosition, expected.LocalPosition), Is.LessThan(0.00001f));
            Assert.That(Vector3.Distance(actual.LocalEulerAngles, expected.LocalEulerAngles), Is.LessThan(0.00001f));
            Assert.That(actual.LocalScale, Is.EqualTo(baseScale));
            if (impulseTime == 0f)
            {
                Assert.That(Vector3.Distance(actual.LocalPosition, basePosition), Is.GreaterThan(0.0001f),
                    "An impulse must start with a jolt even when no decay curve is assigned.");
            }
            else if (impulseTime == settings.ImpulseDuration)
            {
                Assert.That(actual.LocalPosition, Is.EqualTo(basePosition));
                Assert.That(actual.LocalEulerAngles, Is.EqualTo(baseEuler));
            }
        }
    }
}
