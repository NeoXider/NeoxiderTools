using System;
using System.Reflection;
using Neo.Animations;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Manual clock contract of the animators: <c>Tick</c> advances only while playing and enabled, rejects
    ///     bad deltas, a disabled random start keeps the caller's clock, change events honour the threshold, and
    ///     warm ticks allocate nothing.
    /// </summary>
    public sealed class ValueAnimatorTickTests
    {
        private const int Iterations = 200;

        private GameObject _host;
        private Random.State _randomState;

        [SetUp]
        public void SetUp()
        {
            _randomState = Random.state;
            _host = new GameObject("ValueAnimatorTickTests");
        }

        [TearDown]
        public void TearDown()
        {
            if (_host != null)
            {
                Object.DestroyImmediate(_host);
            }

            Random.state = _randomState;
        }

        [Test]
        public void FloatAnimator_Tick_SamplesTheCurveAtTheAdvancedClock()
        {
            FloatAnimator animator = _host.AddComponent<FloatAnimator>();
            animator.animationType = AnimationType.SinWave;
            animator.minValue = 0f;
            animator.maxValue = 1f;
            animator.animationSpeed = 1f;
            animator.Play();

            animator.Tick(0.25f);
            Assert.That(animator.AnimationTime, Is.EqualTo(0.25f).Within(1e-6f));
            Assert.That(animator.CurrentValue, Is.EqualTo(1f).Within(1e-4f), "sin(2 pi * 0.25) maps to the max.");

            animator.Tick(0.25f);
            Assert.That(animator.CurrentValue, Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void Tick_DoesNotAdvanceWhileStoppedOrPaused()
        {
            FloatAnimator animator = _host.AddComponent<FloatAnimator>();

            animator.Tick(1f);
            Assert.That(animator.AnimationTime, Is.Zero, "A stopped animator ignores ticks.");

            animator.Play();
            animator.Pause();
            animator.Tick(1f);
            Assert.That(animator.AnimationTime, Is.Zero, "A paused animator ignores ticks.");

            animator.Resume();
            animator.Tick(1f);
            Assert.That(animator.AnimationTime, Is.EqualTo(1f).Within(1e-6f));
        }

        [Test]
        public void Tick_DoesNotAdvanceWhileDisabled()
        {
            FloatAnimator floatAnimator = _host.AddComponent<FloatAnimator>();
            ColorAnimator colorAnimator = _host.AddComponent<ColorAnimator>();
            Vector3Animator vectorAnimator = _host.AddComponent<Vector3Animator>();
            floatAnimator.Play();
            colorAnimator.Play();
            vectorAnimator.Play();
            InvokePrivate(floatAnimator, "OnDisable");
            InvokePrivate(colorAnimator, "OnDisable");
            InvokePrivate(vectorAnimator, "OnDisable");

            floatAnimator.Tick(1f);
            colorAnimator.Tick(1f);
            vectorAnimator.Tick(1f);
            Assert.That(floatAnimator.AnimationTime + colorAnimator.AnimationTime + vectorAnimator.AnimationTime,
                Is.Zero, "A manual clock must not drive a disabled animator, just as Update does not.");

            InvokePrivate(floatAnimator, "OnEnable");
            InvokePrivate(colorAnimator, "OnEnable");
            InvokePrivate(vectorAnimator, "OnEnable");
            floatAnimator.Tick(1f);
            colorAnimator.Tick(1f);
            vectorAnimator.Tick(1f);
            Assert.That(floatAnimator.AnimationTime, Is.EqualTo(1f).Within(1e-6f));
            Assert.That(colorAnimator.AnimationTime, Is.EqualTo(1f).Within(1e-6f));
            Assert.That(vectorAnimator.AnimationTime, Is.EqualTo(1f).Within(1e-6f));
        }

        [Test]
        public void Tick_NegativeOrNonFiniteDelta_DoesNotMoveTheClock()
        {
            Vector3Animator animator = _host.AddComponent<Vector3Animator>();
            animator.Play();
            animator.Tick(0.5f);

            animator.Tick(-1f);
            animator.Tick(float.NaN);
            animator.Tick(float.PositiveInfinity);

            Assert.That(animator.AnimationTime, Is.EqualTo(0.5f).Within(1e-6f));
            Assert.That(float.IsNaN(animator.CurrentVector.x), Is.False);
        }

        [Test]
        public void RandomizeStartTimeOff_KeepsTheClockSetBeforeStart()
        {
            FloatAnimator deterministic = _host.AddComponent<FloatAnimator>();
            deterministic.playOnStart = false;
            deterministic.RandomizeStartTime = false;
            // WHY a non-zero clock: zero is also the default, so it could not tell "kept" from "reset by Start".
            deterministic.Play();
            deterministic.Tick(5f);
            deterministic.Stop();
            Assert.That(deterministic.AnimationTime, Is.EqualTo(5f).Within(1e-6f));
            InvokeStart(deterministic);
            Assert.That(deterministic.AnimationTime, Is.EqualTo(5f).Within(1e-6f),
                "Start must not overwrite the caller's clock.");

            Random.InitState(20261102);
            ColorAnimator randomized = _host.AddComponent<ColorAnimator>();
            randomized.playOnStart = false;
            InvokeStart(randomized);
            Assert.That(randomized.AnimationTime, Is.GreaterThan(0f).And.LessThanOrEqualTo(1000f),
                "The default keeps the legacy randomized start.");
        }

        [Test]
        public void ColorAnimator_Tick_RaisesOnColorChangedOnlyAboveTheThreshold()
        {
            ColorAnimator animator = _host.AddComponent<ColorAnimator>();
            animator.animationType = AnimationType.CustomCurve;
            animator.customCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            animator.startColor = Color.black;
            animator.endColor = Color.white;
            animator.animationSpeed = 1f;
            animator.RandomizeStartTime = false;
            animator.playOnStart = true;
            InvokeStart(animator);
            int changes = 0;
            UnityEvent<Color> changed = animator.OnColorChanged ??= new UnityEvent<Color>();
            changed.AddListener(_ => changes++);

            animator.Tick(0.0001f);
            Assert.That(changes, Is.Zero,
                "A 0.0001 step per RGB channel (about 0.00017 color distance) is below the 0.001 threshold.");
            Assert.That(animator.CurrentColor.r, Is.EqualTo(0.0001f).Within(1e-6f), "The sample is still current.");

            animator.Tick(0.01f);
            Assert.That(changes, Is.EqualTo(1));
            Assert.That(animator.CurrentColor.r, Is.EqualTo(0.0101f).Within(1e-5f));
        }

        [Test]
        public void TransformAnimator_Tick_AppliesThePoseLikeTheEvaluator()
        {
            TransformAnimator animator = _host.AddComponent<TransformAnimator>();
            animator.RandomizeStartTime = false;
            TransformAnimationSettings settings = new TransformAnimationSettings();
            settings.RotateEnabled = true;
            settings.RotationSpeed = new Vector3(0f, 90f, 0f);
            animator.Settings = settings;
            animator.CaptureBase();
            animator.ResetTime();
            animator.Play();

            animator.Tick(0.5f);

            Assert.That(animator.Time, Is.EqualTo(0.5f).Within(1e-6f));
            Assert.That(Quaternion.Angle(_host.transform.localRotation, Quaternion.Euler(0f, 45f, 0f)),
                Is.LessThan(0.01f));
        }

        [Test]
        public void WarmTicks_DoNotAllocate()
        {
            FloatAnimator floatAnimator = _host.AddComponent<FloatAnimator>();
            ColorAnimator colorAnimator = _host.AddComponent<ColorAnimator>();
            Vector3Animator vectorAnimator = _host.AddComponent<Vector3Animator>();
            TransformAnimator transformAnimator = _host.AddComponent<TransformAnimator>();
            TransformAnimationSettings settings = new TransformAnimationSettings();
            settings.RotateEnabled = true;
            settings.ShakeEnabled = true;
            transformAnimator.Settings = settings;
            floatAnimator.Play();
            colorAnimator.Play();
            vectorAnimator.Play();
            transformAnimator.Play();
            for (int i = 0; i < 3; i++)
            {
                TickAll(floatAnimator, colorAnimator, vectorAnimator, transformAnimator);
            }

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                TickAll(floatAnimator, colorAnimator, vectorAnimator, transformAnimator);
            }

            long allocated = allocationCounter.End();

            Assert.That(allocated, Is.EqualTo(0L));
        }

        private static void TickAll(FloatAnimator floatAnimator, ColorAnimator colorAnimator,
            Vector3Animator vectorAnimator, TransformAnimator transformAnimator)
        {
            floatAnimator.Tick(0.016f);
            colorAnimator.Tick(0.016f);
            vectorAnimator.Tick(0.016f);
            transformAnimator.Tick(0.016f);
        }

        private static void InvokeStart(MonoBehaviour behaviour)
        {
            InvokePrivate(behaviour, "Start");
        }

        private static void InvokePrivate(MonoBehaviour behaviour, string methodName)
        {
            MethodInfo method = behaviour.GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(behaviour, null);
        }
    }
}
