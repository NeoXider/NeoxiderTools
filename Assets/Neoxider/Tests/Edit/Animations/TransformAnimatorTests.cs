using System.Reflection;
using Neo.Animations;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Animations
{
    public sealed class TransformAnimatorTests
    {
        private static readonly Vector3 BasePosition = new(1f, 2f, 3f);
        private static readonly Vector3 BaseEuler = new(10f, 20f, 30f);
        private static readonly Vector3 BaseScale = new(2f, 2f, 2f);

        private static TransformAnimationSettings AllDisabled()
        {
            return new TransformAnimationSettings
            {
                RotateEnabled = false,
                FloatEnabled = false,
                ScaleEnabled = false,
                ShakeEnabled = false
            };
        }

        private static void InvokeLifecycle(TransformAnimator animator, string methodName)
        {
            MethodInfo method = typeof(TransformAnimator).GetMethod(
                methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(animator, null);
        }

        [Test]
        public void Evaluate_AllChannelsDisabled_ReturnsBasePose()
        {
            TransformAnimationState state = TransformAnimationEvaluator.Evaluate(
                AllDisabled(), BasePosition, BaseEuler, BaseScale, time: 5f, randomSeed: 42f);

            Assert.That(state.LocalPosition, Is.EqualTo(BasePosition));
            Assert.That(state.LocalEulerAngles, Is.EqualTo(BaseEuler));
            Assert.That(state.LocalScale, Is.EqualTo(BaseScale));
        }

        [Test]
        public void Evaluate_NullSettings_ReturnsBasePose()
        {
            TransformAnimationState state = TransformAnimationEvaluator.Evaluate(
                null, BasePosition, BaseEuler, BaseScale, time: 5f, randomSeed: 0f);

            Assert.That(state.LocalPosition, Is.EqualTo(BasePosition));
            Assert.That(state.LocalEulerAngles, Is.EqualTo(BaseEuler));
            Assert.That(state.LocalScale, Is.EqualTo(BaseScale));
        }

        [Test]
        public void Evaluate_Rotate_AddsSpeedTimesTime()
        {
            TransformAnimationSettings settings = AllDisabled();
            settings.RotateEnabled = true;
            settings.RotationSpeed = new Vector3(0f, 90f, 0f);

            TransformAnimationState state = TransformAnimationEvaluator.Evaluate(
                settings, BasePosition, BaseEuler, BaseScale, time: 2f, randomSeed: 0f);

            Assert.That(state.LocalEulerAngles.y, Is.EqualTo(BaseEuler.y + 180f).Within(0.001f));
            Assert.That(state.LocalEulerAngles.x, Is.EqualTo(BaseEuler.x).Within(0.001f));
            Assert.That(state.LocalPosition, Is.EqualTo(BasePosition));
        }

        [Test]
        public void Evaluate_Float_FollowsCurveOverCyclePhase()
        {
            TransformAnimationSettings settings = AllDisabled();
            settings.FloatEnabled = true;
            settings.FloatHeight = 0.5f;
            settings.FloatDuration = 2f;
            settings.FloatCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

            TransformAnimationState state = TransformAnimationEvaluator.Evaluate(
                settings, BasePosition, BaseEuler, BaseScale, time: 0.5f, randomSeed: 0f);

            Assert.That(state.LocalPosition.y, Is.EqualTo(BasePosition.y + 0.25f).Within(0.001f));
            Assert.That(state.LocalPosition.x, Is.EqualTo(BasePosition.x).Within(0.001f));
        }

        [Test]
        public void Evaluate_Float_ZeroHeight_DoesNotMove()
        {
            TransformAnimationSettings settings = AllDisabled();
            settings.FloatEnabled = true;
            settings.FloatHeight = 0f;

            TransformAnimationState state = TransformAnimationEvaluator.Evaluate(
                settings, BasePosition, BaseEuler, BaseScale, time: 0.5f, randomSeed: 0f);

            Assert.That(state.LocalPosition, Is.EqualTo(BasePosition));
        }

        [Test]
        public void Evaluate_ScalePulse_IsSymmetricAroundBaseScale()
        {
            TransformAnimationSettings settings = AllDisabled();
            settings.ScaleEnabled = true;
            settings.ScaleAmplitude = 0.2f;
            settings.ScaleDuration = 2f;
            settings.ScaleCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

            TransformAnimationState minimum = TransformAnimationEvaluator.Evaluate(
                settings, BasePosition, BaseEuler, BaseScale, time: 0f, randomSeed: 0f);
            TransformAnimationState midpoint = TransformAnimationEvaluator.Evaluate(
                settings, BasePosition, BaseEuler, BaseScale, time: 0.5f, randomSeed: 0f);
            TransformAnimationState maximum = TransformAnimationEvaluator.Evaluate(
                settings, BasePosition, BaseEuler, BaseScale, time: 1f, randomSeed: 0f);

            Assert.That(minimum.LocalScale.x, Is.EqualTo(BaseScale.x * 0.8f).Within(0.001f));
            Assert.That(midpoint.LocalScale.x, Is.EqualTo(BaseScale.x).Within(0.001f));
            Assert.That(maximum.LocalScale.x, Is.EqualTo(BaseScale.x * 1.2f).Within(0.001f));
        }

        [Test]
        public void Evaluate_Shake_IsDeterministicForSameSeed_AndDiffersAcrossSeeds()
        {
            TransformAnimationSettings settings = AllDisabled();
            settings.ShakeEnabled = true;
            settings.ShakePositionStrength = 0.1f;

            TransformAnimationState first = TransformAnimationEvaluator.Evaluate(
                settings, BasePosition, BaseEuler, BaseScale, time: 1.234f, randomSeed: 7f);
            TransformAnimationState repeated = TransformAnimationEvaluator.Evaluate(
                settings, BasePosition, BaseEuler, BaseScale, time: 1.234f, randomSeed: 7f);
            TransformAnimationState otherSeed = TransformAnimationEvaluator.Evaluate(
                settings, BasePosition, BaseEuler, BaseScale, time: 1.234f, randomSeed: 8f);

            Assert.That(first.LocalPosition, Is.EqualTo(repeated.LocalPosition));
            Assert.That(first.LocalPosition, Is.Not.EqualTo(otherSeed.LocalPosition));
            Assert.That(first.LocalPosition, Is.Not.EqualTo(BasePosition));
        }

        [Test]
        public void Evaluate_Impulse_AfterDuration_HasNoEffect()
        {
            TransformAnimationSettings settings = AllDisabled();
            settings.ImpulseDuration = 0.4f;

            TransformAnimationState state = TransformAnimationEvaluator.Evaluate(
                settings, BasePosition, BaseEuler, BaseScale, time: 0f, randomSeed: 3f,
                impulseTime: 1f, impulseStrength: 1f);

            Assert.That(state.LocalPosition, Is.EqualTo(BasePosition));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        public void Evaluate_Impulse_NonPositiveDuration_IsIgnored(float duration)
        {
            TransformAnimationSettings settings = AllDisabled();
            settings.ImpulseDuration = duration;
            settings.ImpulsePositionStrength = 10f;
            settings.ImpulseDecayCurve = AnimationCurve.Constant(0f, 1f, 1f);

            TransformAnimationState state = TransformAnimationEvaluator.Evaluate(
                settings, BasePosition, BaseEuler, BaseScale, time: 0f, randomSeed: 3f,
                impulseTime: 0f, impulseStrength: 1f);

            Assert.That(state.LocalPosition, Is.EqualTo(BasePosition));
            Assert.That(state.LocalEulerAngles, Is.EqualTo(BaseEuler));
        }

        [Test]
        public void Evaluate_Impulse_AtStart_AppliesDecayingOffset()
        {
            TransformAnimationSettings settings = AllDisabled();
            settings.ImpulseDuration = 0.4f;
            settings.ImpulsePositionStrength = 0.3f;
            settings.ImpulseRotationStrength = 0f;
            settings.ImpulseDecayCurve = AnimationCurve.Constant(0f, 1f, 1f);

            TransformAnimationState state = TransformAnimationEvaluator.Evaluate(
                settings, BasePosition, BaseEuler, BaseScale, time: 0f, randomSeed: 3f,
                impulseTime: 0.1f, impulseStrength: 1f);

            float distance = Vector3.Distance(state.LocalPosition, BasePosition);
            Assert.That(distance, Is.GreaterThan(0.0001f));
            Assert.That(distance, Is.LessThanOrEqualTo(0.3f * Mathf.Sqrt(3f) + 0.0001f));
        }

        [Test]
        public void CyclePhase_PingPongsOverFullCycle()
        {
            Assert.That(TransformAnimationEvaluator.CyclePhase(0f, 2f), Is.EqualTo(0f).Within(0.001f));
            Assert.That(TransformAnimationEvaluator.CyclePhase(1f, 2f), Is.EqualTo(1f).Within(0.001f));
            Assert.That(TransformAnimationEvaluator.CyclePhase(3f, 2f), Is.EqualTo(1f).Within(0.001f));
            Assert.That(TransformAnimationEvaluator.CyclePhase(0.5f, 2f), Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(TransformAnimationEvaluator.CyclePhase(1f, 0f), Is.EqualTo(0f));
        }

        [Test]
        public void Component_Stop_RestoresBasePose()
        {
            GameObject gameObject = new("AnimTest");
            try
            {
                TransformAnimator animator = gameObject.AddComponent<TransformAnimator>();
                animator.Settings = AllDisabled();
                animator.Settings.RotateEnabled = true;
                animator.RandomizeStartTime = false;

                animator.CaptureBase();
                animator.RandomizeTime();
                animator.ApplyCurrentState();
                Assert.That(gameObject.transform.localEulerAngles, Is.Not.EqualTo(Vector3.zero));

                animator.Play();
                animator.Stop();
                Assert.That(gameObject.transform.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(gameObject.transform.localEulerAngles, Is.EqualTo(Vector3.zero));
                Assert.That(gameObject.transform.localScale, Is.EqualTo(Vector3.one));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Component_Shake_ImpulseInfluencesNextApply()
        {
            GameObject gameObject = new("AnimTest");
            try
            {
                TransformAnimator animator = gameObject.AddComponent<TransformAnimator>();
                animator.Settings = AllDisabled();
                animator.Settings.ImpulseDuration = 10f;
                animator.Settings.ImpulsePositionStrength = 0.5f;
                animator.Settings.ImpulseRotationStrength = 0f;
                animator.Settings.ImpulseDecayCurve = AnimationCurve.Constant(0f, 1f, 1f);
                animator.RandomizeStartTime = false;

                animator.CaptureBase();
                animator.RandomizeTime();
                animator.Shake();
                animator.ApplyCurrentState();

                Assert.That(gameObject.transform.localPosition, Is.Not.EqualTo(Vector3.zero));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Component_NullSettings_ShakeAndUpdateDoNotThrow()
        {
            GameObject gameObject = new("AnimTest");
            try
            {
                TransformAnimator animator = gameObject.AddComponent<TransformAnimator>();
                animator.Settings = null;
                animator.RandomizeStartTime = false;
                animator.CaptureBase();
                animator.Play();
                animator.Shake();

                Assert.DoesNotThrow(() => InvokeLifecycle(animator, "Update"));
                Assert.That(gameObject.transform.localPosition, Is.EqualTo(Vector3.zero));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Component_SetTarget_RestoresOldTargetAndCapturesNewBase()
        {
            GameObject host = new("Host");
            GameObject first = new("First");
            GameObject second = new("Second");
            try
            {
                first.transform.localPosition = new Vector3(1f, 2f, 3f);
                second.transform.localPosition = new Vector3(7f, 8f, 9f);

                TransformAnimator animator = host.AddComponent<TransformAnimator>();
                animator.Settings = AllDisabled();
                animator.Settings.FloatEnabled = true;
                animator.Settings.FloatHeight = 2f;
                animator.Settings.FloatDuration = 2f;
                animator.Settings.FloatCurve = AnimationCurve.Constant(0f, 1f, 1f);
                animator.RandomizeStartTime = false;

                animator.SetTarget(first.transform);
                animator.ApplyCurrentState();
                Assert.That(first.transform.localPosition, Is.EqualTo(new Vector3(1f, 4f, 3f)));

                animator.SetTarget(second.transform);

                Assert.That(first.transform.localPosition, Is.EqualTo(new Vector3(1f, 2f, 3f)));
                Assert.That(second.transform.localPosition, Is.EqualTo(new Vector3(7f, 8f, 9f)));
                animator.ApplyCurrentState();
                Assert.That(second.transform.localPosition, Is.EqualTo(new Vector3(7f, 10f, 9f)));
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void Component_PoolReenable_AutoPlaysOncePerEnable()
        {
            GameObject gameObject = new("AnimTest");
            try
            {
                TransformAnimator animator = gameObject.AddComponent<TransformAnimator>();
                animator.PlayOnEnable = true;
                animator.RandomizeStartTime = false;
                int startedCount = 0;
                animator.OnAnimationStarted.AddListener(() => startedCount++);

                InvokeLifecycle(animator, "Start");
                Assert.That(animator.IsPlaying, Is.True);
                Assert.That(startedCount, Is.EqualTo(1));

                InvokeLifecycle(animator, "OnDisable");
                Assert.That(animator.IsPlaying, Is.False);

                InvokeLifecycle(animator, "OnEnable");
                Assert.That(animator.IsPlaying, Is.True);
                Assert.That(startedCount, Is.EqualTo(2));

                InvokeLifecycle(animator, "OnEnable");
                Assert.That(startedCount, Is.EqualTo(2), "Play must be idempotent within one enable period");
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
