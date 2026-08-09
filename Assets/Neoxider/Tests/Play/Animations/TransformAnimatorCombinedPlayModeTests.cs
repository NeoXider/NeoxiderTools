using System.Collections;
using System.Reflection;
using Neo.Animations;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play.Animations
{
    public sealed class TransformAnimatorCombinedPlayModeTests
    {
        private static readonly Vector3 BasePosition = new Vector3(1f, 2f, 3f);
        private static readonly Vector3 BaseEuler = new Vector3(12f, 24f, 36f);
        private static readonly Vector3 BaseScale = new Vector3(1.2f, 0.9f, 1.4f);

        [UnityTest]
        public IEnumerator CombinedChannels_PausedFrameMatchesEvaluator_AndEveryChannelContributes()
        {
            const float sampleTime = 0.75f;
            const float seed = 17.25f;
            const float impulseStrength = 0.8f;
            GameObject gameObject = new GameObject("TransformAnimatorCombinedPlayMode");
            gameObject.SetActive(false);
            try
            {
                gameObject.transform.localPosition = BasePosition;
                gameObject.transform.localEulerAngles = BaseEuler;
                gameObject.transform.localScale = BaseScale;

                TransformAnimator animator = gameObject.AddComponent<TransformAnimator>();
                TransformAnimationSettings settings = CreateCombinedSettings();
                animator.PlayOnEnable = false;
                animator.RandomizeStartTime = false;
                animator.Settings = settings;
                gameObject.SetActive(true);

                animator.CaptureBase();
                SetPrivateField(animator, "_time", sampleTime);
                SetPrivateField(animator, "_seed", seed);
                animator.Play();
                animator.Shake(impulseStrength);
                animator.ApplyCurrentState();
                animator.Pause();

                TransformAnimationState expected = TransformAnimationEvaluator.Evaluate(
                    settings, BasePosition, BaseEuler, BaseScale, sampleTime, seed, 0f, impulseStrength);
                Vector3 pausedPosition = gameObject.transform.localPosition;
                Vector3 pausedEuler = gameObject.transform.localEulerAngles;
                Vector3 pausedScale = gameObject.transform.localScale;

                yield return null;

                Assert.That(animator.IsPaused, Is.True);
                AssertVector(gameObject.transform.localPosition, pausedPosition, "paused position changed");
                AssertEuler(gameObject.transform.localEulerAngles, pausedEuler, "paused rotation changed");
                AssertVector(gameObject.transform.localScale, pausedScale, "paused scale changed");
                AssertVector(pausedPosition, expected.LocalPosition, "combined position");
                AssertEuler(pausedEuler, expected.LocalEulerAngles, "combined rotation");
                AssertVector(pausedScale, expected.LocalScale, "combined scale");

                AssertEveryChannelContributes(settings, expected, sampleTime, seed, impulseStrength);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        private static TransformAnimationSettings CreateCombinedSettings()
        {
            return new TransformAnimationSettings
            {
                RotateEnabled = true,
                RotationSpeed = new Vector3(10f, 20f, 30f),
                FloatEnabled = true,
                FloatDirection = new Vector3(1f, 2f, 0.5f),
                FloatHeight = 0.8f,
                FloatDuration = 2f,
                FloatCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f),
                ScaleEnabled = true,
                ScaleAmplitude = 0.2f,
                ScaleDuration = 2.5f,
                ScaleCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f),
                ShakeEnabled = true,
                ShakePositionStrength = 0.12f,
                ShakeRotationStrength = 2f,
                ShakeSpeed = 6f,
                ImpulseDuration = 1f,
                ImpulsePositionStrength = 0.35f,
                ImpulseRotationStrength = 4f,
                ImpulseDecayCurve = AnimationCurve.Constant(0f, 1f, 1f)
            };
        }

        private static void AssertEveryChannelContributes(TransformAnimationSettings settings,
            TransformAnimationState combined, float sampleTime, float seed, float impulseStrength)
        {
            TransformAnimationSettings withoutRotate = CreateCombinedSettings();
            withoutRotate.RotateEnabled = false;
            TransformAnimationState noRotate = Evaluate(withoutRotate, sampleTime, seed, impulseStrength);
            Assert.That(EulerDelta(combined.LocalEulerAngles, noRotate.LocalEulerAngles), Is.GreaterThan(0.001f),
                "rotation channel contributed no rotation");

            TransformAnimationSettings withoutFloat = CreateCombinedSettings();
            withoutFloat.FloatEnabled = false;
            TransformAnimationState noFloat = Evaluate(withoutFloat, sampleTime, seed, impulseStrength);
            Assert.That(Vector3.Distance(combined.LocalPosition, noFloat.LocalPosition), Is.GreaterThan(0.001f),
                "float channel contributed no position offset");

            TransformAnimationSettings withoutScale = CreateCombinedSettings();
            withoutScale.ScaleEnabled = false;
            TransformAnimationState noScale = Evaluate(withoutScale, sampleTime, seed, impulseStrength);
            Assert.That(Vector3.Distance(combined.LocalScale, noScale.LocalScale), Is.GreaterThan(0.001f),
                "scale channel contributed no scale offset");

            TransformAnimationSettings withoutShake = CreateCombinedSettings();
            withoutShake.ShakeEnabled = false;
            TransformAnimationState noShake = Evaluate(withoutShake, sampleTime, seed, impulseStrength);
            bool shakeContributed = Vector3.Distance(combined.LocalPosition, noShake.LocalPosition) > 0.001f ||
                                    EulerDelta(combined.LocalEulerAngles, noShake.LocalEulerAngles) > 0.001f;
            Assert.That(shakeContributed, Is.True, "continuous shake contributed no pose offset");

            TransformAnimationState noImpulse = TransformAnimationEvaluator.Evaluate(
                settings, BasePosition, BaseEuler, BaseScale, sampleTime, seed, -1f, impulseStrength);
            bool impulseContributed = Vector3.Distance(combined.LocalPosition, noImpulse.LocalPosition) > 0.001f ||
                                      EulerDelta(combined.LocalEulerAngles, noImpulse.LocalEulerAngles) > 0.001f;
            Assert.That(impulseContributed, Is.True, "impulse contributed no pose offset");
        }

        private static TransformAnimationState Evaluate(TransformAnimationSettings settings, float sampleTime,
            float seed, float impulseStrength)
        {
            return TransformAnimationEvaluator.Evaluate(
                settings, BasePosition, BaseEuler, BaseScale, sampleTime, seed, 0f, impulseStrength);
        }

        private static void SetPrivateField(TransformAnimator animator, string fieldName, float value)
        {
            FieldInfo field = typeof(TransformAnimator).GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(animator, value);
        }

        private static void AssertVector(Vector3 actual, Vector3 expected, string message)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(0.0001f), message);
        }

        private static void AssertEuler(Vector3 actual, Vector3 expected, string message)
        {
            Assert.That(EulerDelta(actual, expected), Is.LessThan(0.001f), message);
        }

        private static float EulerDelta(Vector3 first, Vector3 second)
        {
            Vector3 delta = new Vector3(
                Mathf.DeltaAngle(first.x, second.x),
                Mathf.DeltaAngle(first.y, second.y),
                Mathf.DeltaAngle(first.z, second.z));
            return delta.magnitude;
        }
    }
}
