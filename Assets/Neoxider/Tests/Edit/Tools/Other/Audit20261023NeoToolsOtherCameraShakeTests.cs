using System.Collections;
using System.Reflection;
using DG.Tweening;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace Neo.Editor.Tests.Tools
{
    public sealed class Audit20261023NeoToolsOtherCameraShakeTests
    {
        private GameObject _gameObject;
        private CameraShake _shake;
        private Sequence _observedSequence;
        private Sequence _nativeClockProbe;
        private float _previousTimeScale;
        private bool _enteredPlayMode;

        [SetUp]
        public void SetUp()
        {
            _previousTimeScale = Time.timeScale;
        }

        [UnitySetUp]
        public IEnumerator EnterNativeLifecycle()
        {
            if (!Application.isPlaying)
            {
                yield return new EnterPlayMode();
                _enteredPlayMode = true;
            }

            Assert.That(Application.isPlaying, Is.True, "Shake contracts require the real Awake/disable lifecycle.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _nativeClockProbe?.Kill();
            _nativeClockProbe = null;
            Time.timeScale = _previousTimeScale;
            if (_shake != null)
            {
                _shake.OnShakeStart.RemoveAllListeners();
                _shake.OnShakeStop.RemoveAllListeners();
                _shake.OnShakeComplete.RemoveAllListeners();
                _shake.StopAndReset();
            }

            _observedSequence?.Kill();
            _observedSequence = null;
            if (_gameObject != null)
            {
                Object.DestroyImmediate(_gameObject);
            }

            if (_enteredPlayMode && Application.isPlaying)
            {
                _enteredPlayMode = false;
                yield return new ExitPlayMode();
            }
        }

        [Test]
        public void SmallPositionShake_WithFadeOut_ProducesFractionalMovement()
        {
            CameraShake shake = CreateShake();
            shake.shakeZ = true;
            shake.StartShake(1f, 0.1f);
            Sequence sequence = ObserveSequence();
            float displacement = 0f;
            for (int sample = 1; sample <= 7; sample++)
            {
                sequence.Goto(sample * 0.11f);
                displacement += shake.transform.localPosition.magnitude;
            }

            Assert.That(displacement, Is.GreaterThan(0.001f),
                "Fade Out must not round a sub-unit shake to integer positions.");
        }

        [Test]
        public void NonRelativeShake_UsesCapturedTransformForBothChannels()
        {
            CameraShake shake = CreateShake();
            shake.shakeType = CameraShake.ShakeType.Both;
            shake.relativeShake = false;
            shake.transform.localPosition = new Vector3(3f, 4f, 5f);
            shake.transform.localRotation = Quaternion.Euler(10f, 20f, 30f);

            shake.StartShake(1f, 0f);
            ObserveSequence().Complete(true);

            Assert.That(shake.transform.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(Quaternion.Angle(shake.transform.localRotation, Quaternion.identity), Is.LessThan(0.001f));
        }

        [TestCase(CameraShake.ShakeType.Position)]
        [TestCase(CameraShake.ShakeType.Rotation)]
        public void Stop_AfterChangingShakeType_RestoresTheChannelsThatWereStarted(CameraShake.ShakeType startedType)
        {
            CameraShake shake = CreateShake();
            shake.shakeType = startedType;
            shake.StartShake(1f, 0f);
            ObserveSequence();
            Quaternion rotation = Quaternion.Euler(10f, 20f, 30f);
            shake.transform.localPosition = Vector3.one;
            shake.transform.localRotation = rotation;
            shake.shakeType = startedType == CameraShake.ShakeType.Position
                ? CameraShake.ShakeType.Rotation
                : CameraShake.ShakeType.Position;

            shake.StopShake();

            Assert.That(shake.IsShaking, Is.False);
            Assert.That(shake.transform.localPosition,
                Is.EqualTo(startedType == CameraShake.ShakeType.Position ? Vector3.zero : Vector3.one));
            Quaternion expectedRotation = startedType == CameraShake.ShakeType.Rotation ? Quaternion.identity : rotation;
            Assert.That(Quaternion.Angle(shake.transform.localRotation, expectedRotation), Is.LessThan(0.001f));
        }

        [Test]
        public void StartListener_CanStopWithoutLeavingATweenThatWritesTheTransform()
        {
            CameraShake shake = CreateShake();
            int completed = 0;
            int stopped = 0;
            shake.OnShakeStart.AddListener(shake.StopShake);
            shake.OnShakeStop.AddListener(() => stopped++);
            shake.OnShakeComplete.AddListener(() => completed++);

            shake.StartShake(1f, 0.2f);
            Sequence sequence = ObserveSequence(false);
            Vector3 marker = new Vector3(7f, 8f, 9f);
            shake.transform.localPosition = marker;
            if (sequence != null && sequence.IsActive())
            {
                sequence.Goto(0.3f);
            }

            Assert.That(shake.transform.localPosition, Is.EqualTo(marker));
            Assert.That(sequence == null || !sequence.IsActive(), Is.True);
            Assert.That(shake.IsShaking, Is.False);
            Assert.That(stopped, Is.EqualTo(1));
            Assert.That(completed, Is.Zero);
        }

        [Test]
        public void StartListener_ReplacesRequest_AndOnlyTheReplacementCompletes()
        {
            CameraShake shake = CreateShake();
            int started = 0;
            int completed = 0;
            shake.OnShakeStart.AddListener(() =>
            {
                started++;
                if (started == 1)
                {
                    shake.StartShake(0.25f, 0f);
                }
            });
            shake.OnShakeComplete.AddListener(() => completed++);

            shake.StartShake(5f, 0f);
            Sequence sequence = ObserveSequence();
            Assert.That(sequence.Duration(), Is.EqualTo(0.25f).Within(0.0001f));
            sequence.Complete(true);

            Assert.That(started, Is.EqualTo(2));
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(shake.IsShaking, Is.False);
        }

        [Test]
        public void StopListener_ReplacesRequest_WithoutBeingOverwrittenByTheOuterStart()
        {
            CameraShake shake = CreateShake();
            shake.StartShake(1f, 0f);
            bool replace = true;
            shake.OnShakeStop.AddListener(() =>
            {
                if (replace)
                {
                    replace = false;
                    shake.StartShake(0.25f, 0f);
                }
            });

            shake.StartShake(5f, 0f);
            Sequence sequence = ObserveSequence();
            Assert.That(sequence.Duration(), Is.EqualTo(0.25f).Within(0.0001f));
            sequence.Complete(true);
            Assert.That(shake.IsShaking, Is.False);
        }

        [UnityTest]
        public IEnumerator UnscaledShake_CompletesWhilePaused_AndDisableCannotRestartIt()
        {
            float previousTimeScale = Time.timeScale;
            try
            {
                CameraShake shake = CreateShake();
                int completed = 0;
                int started = 0;
                int stopped = 0;
                shake.useUnscaledTime = true;
                shake.OnShakeComplete.AddListener(() => completed++);
                shake.OnShakeStart.AddListener(() => started++);
                shake.OnShakeStop.AddListener(() => stopped++);
                // The SDK retains its quitting flag until a later native frame after leaving Play.
                // Public initialization is only admitted after that boundary, never by changing SDK flags.
                double deadline = Time.realtimeSinceStartupAsDouble + 1d;
                int initializationFrame = Time.frameCount;
                MonoBehaviour driver = null;
                do
                {
                    yield return null;
                    if (Time.frameCount > initializationFrame)
                    {
                        driver = DOTween.Init() as MonoBehaviour;
                    }
                } while ((Time.frameCount <= initializationFrame || driver == null ||
                          !driver.isActiveAndEnabled || !driver.gameObject.activeInHierarchy) &&
                         Time.realtimeSinceStartupAsDouble < deadline);

                string initializationTrace = $"frame={Time.frameCount}, startFrame={initializationFrame}, " +
                    $"playing={Application.isPlaying}, driverAlive={driver != null}, " +
                    $"driverEnabled={driver != null && driver.enabled}, " +
                    $"driverHierarchy={driver != null && driver.gameObject.activeInHierarchy}.";
                TestContext.WriteLine("Native SDK admission: " + initializationTrace);
                Assert.That(Time.frameCount, Is.GreaterThan(initializationFrame), initializationTrace);
                Assert.That(driver != null && driver.isActiveAndEnabled && driver.gameObject.activeInHierarchy,
                    Is.True, "Public DOTween.Init must admit an active native driver. " + initializationTrace);
                Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThanOrEqualTo(deadline), initializationTrace);
                Time.timeScale = 0f;

                // Initialize through a separate owned control before testing CameraShake's root.
                // Neither a sequence seek nor a manual DOTween update supplies this clock.
                _nativeClockProbe = DOTween.Sequence().SetUpdate(UpdateType.Normal, true)
                    .SetRecyclable(false).SetAutoKill(false).AppendInterval(1f)
                    .SetLoops(-1, LoopType.Restart).Pause();
                Sequence probe = _nativeClockProbe;
                int probeUpdates = 0;
                float probeElapsed = 0f;
                bool pausedDuringUpdates = true;
                probe.OnUpdate(() =>
                {
                    probeUpdates++;
                    probeElapsed = probe.Elapsed(true);
                    pausedDuringUpdates &= Time.timeScale == 0f;
                });
                Assert.That(driver != null && driver.isActiveAndEnabled, Is.True,
                    "The isolated Play session must provide an active native DOTween driver.");
                Assert.That(UnityEditor.EditorApplication.isPaused, Is.False);
                Assert.That(DOTween.timeScale, Is.GreaterThan(0f).And.LessThan(float.PositiveInfinity));
                Assert.That(DOTween.unscaledTimeScale, Is.GreaterThan(0f).And.LessThan(float.PositiveInfinity));
                Assert.That(probe.timeScale, Is.GreaterThan(0f).And.LessThan(float.PositiveInfinity));

                // One unchanged one-second budget includes native readiness and the shake itself.
                int admissionFrame = Time.frameCount;
                double admissionUnscaledTime = Time.unscaledTimeAsDouble;
                probe.Play();
                while ((probeUpdates == 0 || Time.frameCount == admissionFrame) &&
                       Time.realtimeSinceStartupAsDouble < deadline)
                {
                    yield return null;
                }

                string admissionTrace = $"frames={Time.frameCount - admissionFrame}, " +
                    $"nativeUnscaled={Time.unscaledTimeAsDouble - admissionUnscaledTime}, " +
                    $"probeUpdates={probeUpdates}, probeElapsed={probeElapsed}, " +
                    $"driverActive={driver != null && driver.isActiveAndEnabled}, " +
                    $"DOTween clocks={DOTween.timeScale}/{DOTween.unscaledTimeScale}.";
                Assert.That(Time.frameCount, Is.GreaterThan(admissionFrame), admissionTrace);
                Assert.That(Time.unscaledTimeAsDouble, Is.GreaterThan(admissionUnscaledTime), admissionTrace);
                Assert.That(probeElapsed, Is.GreaterThan(0f), "Native independent-clock precondition: " + admissionTrace);

                shake.StartShake(0.05f, 0.1f);
                Sequence sequence = ObserveSequence();
                Assert.That(sequence.IsActive() && sequence.IsPlaying(), Is.True);
                float shakeClock = sequence.timeScale;
                Assert.That(shakeClock, Is.GreaterThan(0f).And.LessThan(float.PositiveInfinity));
                Assert.That(sequence.Duration(false), Is.EqualTo(0.05f).Within(0.0001f));
                int shakeUpdates = 0;
                float shakeElapsed = 0f;
                sequence.OnUpdate(() =>
                {
                    shakeUpdates++;
                    shakeElapsed = sequence.Elapsed(false);
                    pausedDuringUpdates &= Time.timeScale == 0f;
                });
                int shakeFrame = Time.frameCount;
                double shakeUnscaledTime = Time.unscaledTimeAsDouble;
                float probeAtShakeStart = probeElapsed;
                float requiredClockWindow = sequence.Duration(false) * 2f;
                while ((shake.IsShaking || probeElapsed - probeAtShakeStart < requiredClockWindow) &&
                       Time.realtimeSinceStartupAsDouble < deadline)
                {
                    yield return null;
                }

                string clockTrace = $"frames={Time.frameCount - shakeFrame}, " +
                    $"nativeUnscaled={Time.unscaledTimeAsDouble - shakeUnscaledTime}, " +
                    $"probeElapsedDelta={probeElapsed - probeAtShakeStart}, " +
                    $"rootUpdates={shakeUpdates}, rootElapsed={shakeElapsed}, " +
                    $"rootActive={sequence.IsActive()}, " +
                    $"driverActive={driver != null && driver.isActiveAndEnabled}, " +
                    $"Unity/DOTween clocks={Time.timeScale}/{DOTween.timeScale}/{DOTween.unscaledTimeScale}, " +
                    $"rootClock={shakeClock}, smooth={DOTween.useSmoothDeltaTime}/" +
                    $"{DOTween.maxSmoothUnscaledTime}.";
                Assert.That(Time.frameCount, Is.GreaterThan(shakeFrame), clockTrace);
                Assert.That(Time.unscaledTimeAsDouble, Is.GreaterThan(shakeUnscaledTime), clockTrace);
                Assert.That(driver != null && driver.isActiveAndEnabled, Is.True, clockTrace);
                Assert.That(probeElapsed - probeAtShakeStart, Is.GreaterThanOrEqualTo(requiredClockWindow),
                    "Native independent-clock eligibility precondition: " + clockTrace);
                Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThanOrEqualTo(deadline), clockTrace);
                Assert.That(pausedDuringUpdates, Is.True, clockTrace);
                Assert.That(Time.timeScale, Is.Zero, clockTrace);
                Assert.That(completed, Is.EqualTo(1), "The root sequence must advance using unscaled time. " + clockTrace);
                Assert.That(shake.IsShaking, Is.False);
                Assert.That(shakeUpdates, Is.GreaterThan(0), clockTrace);
                Assert.That(stopped, Is.Zero);

                shake.StartShake(1f, 0.1f);
                ObserveSequence();
                shake.OnShakeStop.AddListener(() => shake.StartShake(1f, 0.1f));
                shake.enabled = false;
                yield return null;

                Assert.That(shake.IsShaking, Is.False);
                Assert.That(started, Is.EqualTo(2), "A disable callback cannot start a new owned tween.");
                Assert.That(completed, Is.EqualTo(1));
                Assert.That(stopped, Is.EqualTo(1));
            }
            finally
            {
                _nativeClockProbe?.Kill();
                _nativeClockProbe = null;
                Time.timeScale = previousTimeScale;
            }
        }

        private CameraShake CreateShake()
        {
            Assert.That(Application.isPlaying, Is.True);
            _gameObject = new GameObject(nameof(Audit20261023NeoToolsOtherCameraShakeTests));
            _shake = _gameObject.AddComponent<CameraShake>();
            Assert.That(_shake.isActiveAndEnabled, Is.True);
            _shake.OnShakeStart = new UnityEvent();
            _shake.OnShakeStop = new UnityEvent();
            _shake.OnShakeComplete = new UnityEvent();
            return _shake;
        }

        private Sequence ObserveSequence(bool required = true)
        {
            // Only access needed for deterministic seeking; assertions observe transforms/events/public state.
            FieldInfo field = typeof(CameraShake).GetField("_sequence", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            _observedSequence = field.GetValue(_shake) as Sequence;
            if (required)
            {
                Assert.That(_observedSequence, Is.Not.Null);
            }

            return _observedSequence;
        }
    }
}
