using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Neo.Tests.Performance;
using Neo.Tools;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using Timer = Neo.Tools.Timer;

namespace Neo.Editor.Tests
{
    /// <summary>Clock seams, loop precision and allocation-free text of the Tools/Time module.</summary>
    public sealed class NeoToolsTimeClockSeamTests
    {
        private const int MaxPlayModeSteps = 2000;

        private GameObject _gameObject;
        private TimerObject _timer;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject(nameof(NeoToolsTimeClockSeamTests));
            _timer = _gameObject.AddComponent<TimerObject>();
            _timer.autoStart = false;
            _timer.updateInterval = 0f;
            _timer.useUnscaledTime = true;
            _timer.OnTimerCompleted = new UnityEvent();
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
            {
                Object.DestroyImmediate(_gameObject);
            }
        }

        [UnityTearDown]
        public IEnumerator LeavePlayModeAfterFailure()
        {
            if (Application.isPlaying)
            {
                yield return new ExitPlayMode();
            }
        }

        [Test]
        public void LoopingCountdown_CarriesOvershootIntoNextCycle()
        {
            int completed = 0;
            _timer.OnTimerCompleted.AddListener(() => completed++);
            _timer.duration = 1f;
            _timer.countUp = false;
            _timer.looping = true;
            _timer.CarryLoopOvershoot = true;
            _timer.Play();

            for (int i = 0; i < 4; i++)
            {
                _timer.Tick(0.3f);
            }

            Assert.That(completed, Is.EqualTo(1));
            Assert.That(_timer.CurrentTime, Is.EqualTo(0.8f).Within(0.0001f),
                "the 0.2 s that ran past zero must count toward the next cycle");
        }

        [Test]
        public void LoopingCountdown_DefaultKeepsLegacyRestartAtFullDuration()
        {
            int completed = 0;
            _timer.OnTimerCompleted.AddListener(() => completed++);
            _timer.duration = 1f;
            _timer.countUp = false;
            _timer.looping = true;
            _timer.Play();

            for (int i = 0; i < 4; i++)
            {
                _timer.Tick(0.3f);
            }

            Assert.That(_timer.CarryLoopOvershoot, Is.False);
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(_timer.CurrentTime, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void LoopingCountUp_LongRunPeriodMatchesElapsedTime()
        {
            int completed = 0;
            _timer.OnTimerCompleted.AddListener(() => completed++);
            _timer.duration = 1f;
            _timer.countUp = true;
            _timer.looping = true;
            _timer.CarryLoopOvershoot = true;
            _timer.Play();

            for (int i = 0; i < 100; i++)
            {
                _timer.Tick(0.3f);
            }

            Assert.That(completed, Is.EqualTo(30), "30 seconds of 1-second cycles complete 30 times");
            Assert.That(_timer.CurrentTime, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void LoopOvershootSpanningSeveralCycles_KeepsPhaseWithOneCompletion()
        {
            int completed = 0;
            _timer.OnTimerCompleted.AddListener(() => completed++);
            _timer.duration = 1f;
            _timer.countUp = true;
            _timer.looping = true;
            _timer.CarryLoopOvershoot = true;
            _timer.Play();

            _timer.Tick(5.25f);

            Assert.That(completed, Is.EqualTo(1), "skipped whole cycles must not fire a burst");
            Assert.That(_timer.CurrentTime, Is.EqualTo(0.25f).Within(0.0001f), "the phase inside the cycle is kept");
            Assert.That(_timer.IsRunning, Is.True);
            Assert.That(_timer.IsCompleted, Is.False);
        }

        [Test]
        public void LoopOvershootOfWholeCycles_RestartsAtTheStartInsteadOfAtTheEnd()
        {
            int completed = 0;
            _timer.OnTimerCompleted.AddListener(() => completed++);
            _timer.duration = 1f;
            _timer.countUp = false;
            _timer.looping = true;
            _timer.CarryLoopOvershoot = true;
            _timer.Play();

            _timer.Tick(3f);

            Assert.That(completed, Is.EqualTo(1));
            Assert.That(_timer.CurrentTime, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(_timer.IsCompleted, Is.False, "a running cycle must not report completion");
        }

        [Test]
        public void LoopOvershoot_UsesTheCycleLengthAfterInitialProgress()
        {
            _timer.duration = 2f;
            _timer.countUp = true;
            _timer.looping = true;
            _timer.initialProgress = 0.5f;
            _timer.CarryLoopOvershoot = true;
            _timer.Play();
            Assert.That(_timer.CurrentTime, Is.EqualTo(1f).Within(0.0001f), "positive control: cycles start at 1 s");

            _timer.Tick(4.25f);

            Assert.That(_timer.CurrentTime, Is.EqualTo(1.25f).Within(0.0001f));
            Assert.That(_timer.IsCompleted, Is.False);
        }

        [Test]
        public void InfiniteTimer_KeepsFramePrecisionAtDayMagnitudes()
        {
            _timer.infiniteDuration = true;
            _timer.Play();
            _timer.SetTime(86400f);

            for (int i = 0; i < 600; i++)
            {
                _timer.Tick(1f / 60f);
            }

            Assert.That(_timer.CurrentTime, Is.EqualTo(86410f).Within(0.01f),
                "float rounding of each 16 ms step must not accumulate at 86400 s");
        }

        [Test]
        public void FrameClock_DrivesUpdateAndTimeScalePausePolicy()
        {
            ManualFrameClock clock = new(0.25f, 0f);
            _timer.useUnscaledTime = false;
            _timer.pauseOnTimeScaleZero = true;
            _timer.duration = 10f;
            _timer.countUp = true;
            _timer.FrameClock = clock;
            _timer.Play();

            _timer.Tick(1f);
            Assert.That(_timer.CurrentTime, Is.Zero, "the injected clock reports a paused game");

            clock.TimeScale = 1f;
            clock.SetFrame(0.25f);
            InvokeUpdate(_timer);

            Assert.That(_timer.CurrentTime, Is.EqualTo(0.25f).Within(0.0001f));
            _timer.FrameClock = null;
            Assert.That(_timer.FrameClock, Is.SameAs(UnityFrameClock.Instance));
        }

        [Test]
        public void UtcClock_DrivesRealTimePersistence()
        {
            using Audit20261023NeoToolsTimePersistenceScope scope = new();
            GameObject host = new("UtcClockTimer");
            try
            {
                Audit20261023NeoToolsTimeTimerProbe probe = host.AddComponent<Audit20261023NeoToolsTimeTimerProbe>();
                FakeClock clock = new() { UtcNow = new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc) };
                probe.UtcClock = clock;
                probe.autoStart = false;
                probe.updateInterval = 0f;
                probe.useUnscaledTime = true;
                probe.duration = 100f;
                probe.countUp = false;
                probe.ConfigureRealTimeSave("NeoToolsTimeClockSeamTests.UtcClock");
                probe.Play();
                probe.Tick(10f);
                probe.SaveForTest();

                clock.UtcNow = clock.UtcNow.AddSeconds(30d);
                probe.RestoreForTest();

                Assert.That(probe.CurrentTime, Is.EqualTo(60f).Within(0.01f));
                Assert.That(probe.IsRunning, Is.True);
                probe.DisableSaving();
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void TrustedClockView_NeverReturnsAnEarlierTime()
        {
            FakeClock source = new() { UtcNow = new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc) };
            TrustedClock trusted = new(source);
            INeoClock view = trusted.AsNeoClock();
            DateTime first = view.UtcNow;

            source.UtcNow = first.AddHours(-1d);

            Assert.That(view.UtcNow, Is.EqualTo(first));
            Assert.That(trusted.LastReadWasRewound, Is.True);
            Assert.That(trusted.AsNeoClock(), Is.SameAs(view));
        }

        [Test]
        public void NeoTrustedTimeAsClock_ReadsTheSharedTrustedClock()
        {
            using Audit20261023NeoToolsTimePersistenceScope scope = new();
            FakeClock source = new() { UtcNow = new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc) };
            NeoTrustedTime.SetClockForTests(new TrustedClock(source));
            DateTime first = NeoTrustedTime.AsClock.UtcNow;

            source.UtcNow = first.AddDays(-1d);

            Assert.That(NeoTrustedTime.AsClock.UtcNow, Is.EqualTo(first));
            Assert.That(NeoTrustedTime.AsClock, Is.SameAs(NeoTrustedTime.AsClock));
        }

        [Test]
        public void TimeText_IsWrittenThroughTheConfiguredFormat()
        {
            TextMeshProUGUI text = CreateHiddenLabel();
            _timer.timeText = text;
            _timer.timeFormat = "{0:F1}s {2:F0}%";
            _timer.duration = 10f;
            _timer.countUp = true;
            _timer.Play();

            _timer.Tick(2.5f);

            Assert.That(text.text, Is.EqualTo(string.Format("{0:F1}s {1:F0}%", 2.5f, 25f)));
        }

        [TestCase("{3}")]
        [TestCase("{time}")]
        public void InvalidTimeFormat_FallsBackToTheDefaultInsteadOfThrowing(string format)
        {
            TextMeshProUGUI text = CreateHiddenLabel();
            _timer.timeText = text;
            _timer.timeFormat = format;
            _timer.duration = 10f;
            _timer.countUp = true;

            Assert.DoesNotThrow(() => _timer.Play());
            Assert.DoesNotThrow(() => _timer.Tick(2.5f));

            Assert.That(text.text, Is.EqualTo(string.Format("{0:F1}s", 2.5f)));
            Assert.That(_timer.CurrentTime, Is.EqualTo(2.5f).Within(0.0001f));
        }

        [Test]
        public void InvalidDayTimeFormat_FallsBackToTheDefaultInsteadOfThrowing()
        {
            TextMeshProUGUI text = CreateHiddenLabel();
            _timer.timeText = text;
            _timer.duration = 86400f;
            _timer.countUp = true;
            _timer.looping = true;
            SetPrivateField(_timer, "_dayLengthSeconds", 86400f);
            SetPrivateField(_timer, "_dayTimeFormat", "{0:00}:{5}");

            Assert.DoesNotThrow(() => _timer.Play());
            Assert.DoesNotThrow(() => _timer.Tick(3661f));

            Assert.That(text.text, Is.EqualTo("01:01"));
        }

        [Test]
        public void WarmLoopingTicks_DoNotAllocate_AndLabelWritesCostNoMoreThanTmpItself()
        {
            int completed = 0;
            _timer.OnTimerCompleted.AddListener(() => completed++);
            _timer.duration = 2f;
            _timer.countUp = false;
            _timer.looping = true;
            _timer.CarryLoopOvershoot = true;
            _timer.Play();
            Action ticks = () =>
            {
                for (int i = 0; i < 400; i++)
                {
                    _timer.Tick(0.07f);
                }
            };
            ticks();

            int completedBefore = completed;
            long withoutLabel = GcAlloc.TryCount(ticks, out string notAdmitted);
            Assert.That(completed - completedBefore, Is.InRange(13, 15),
                "positive control: 28 s of ticks complete the 2 s loop about 14 times inside the window");

            TextMeshProUGUI text = CreateHiddenLabel();
            _timer.timeText = text;
            ticks();
            long withLabel = GcAlloc.TryCount(ticks, out _);
            Assert.That(text.text, Does.EndWith("s"), "positive control: the label is written");

            // WHY: in the Editor only, TMP_Text.SetText(char[]) mirrors the characters into a new string for its
            // inspector (UNITY_EDITOR m_text), so a label write allocates there and nowhere else. The timer may
            // cost at most that, one write per tick, and nothing of its own.
            char[] sample = "0.00s".ToCharArray();
            Action tmpWrites = () =>
            {
                for (int i = 0; i < 400; i++)
                {
                    sample[0] = (char)('0' + i % 10);
                    text.SetText(sample, 0, sample.Length);
                }
            };
            tmpWrites();
            long tmpCost = GcAlloc.TryCount(tmpWrites, out _);

            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(withoutLabel, Is.Zero,
                "a warm Tick (reactive Time, progress events and loop restart) must not allocate");
            Assert.That(withLabel, Is.LessThanOrEqualTo(tmpCost),
                "the label text is formatted into a reused buffer; only TMP's own Editor mirror may allocate");
        }

        [Test]
        public void OnValidate_DoesNotAutoBindALabelOnTheSameGameObject()
        {
            GameObject host = new("LabelledTimer", typeof(RectTransform));
            host.SetActive(false);
            try
            {
                host.AddComponent<TextMeshProUGUI>();
                TimerObject timer = host.AddComponent<TimerObject>();
                MethodInfo onValidate = typeof(TimerObject).GetMethod("OnValidate",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(onValidate, Is.Not.Null);

                onValidate.Invoke(timer, null);

                Assert.That(timer.timeText == null, Is.True,
                    "a label driven by TimeToText or game code must not be taken over by the timer");
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        // WHY: these Play Mode cases count the timer's own frames (one OnTimerUpdate per frame with a zero update
        // interval) instead of the test's `yield return null` steps. An EditMode-hosted [UnityTest] is stepped by the
        // editor loop, which in a batch run advanced about three times per player-loop frame, so a fixed number of
        // test steps did not give the timer a fixed number of frames.
        [UnityTest]
        public IEnumerator Timer_ManualFrameClock_LoopKeepsLongRunPeriod()
        {
            yield return new EnterPlayMode();
            ManualFrameClock clock = new(0.3f);
            Timer timer = new(1f, 0f, true, true) { FrameClock = clock, CarryLoopOvershoot = true };
            int completed = 0;
            int timerFrames = 0;
            timer.OnTimerEnd.AddListener(() => completed++);
            timer.OnTimerUpdate.AddListener((_, _) => timerFrames++);
            try
            {
                timer.Start();
                for (int step = 0; step < MaxPlayModeSteps && timerFrames < 40; step++)
                {
                    yield return null;
                }

                int observedFrames = timerFrames;
                int observedCompletions = completed;
                int carriedCycles = (int)(observedFrames * 0.3f);
                Assert.That(observedFrames, Is.GreaterThanOrEqualTo(40), "positive control: the timer ran its frames");
                Assert.That(timer.IsRunning, Is.True);
                Assert.That(observedCompletions, Is.InRange(carriedCycles - 1, carriedCycles + 1),
                    $"{observedFrames} frames of 0.3 s are about {carriedCycles} one-second cycles; dropping each " +
                    $"cycle's overshoot would complete only about {observedFrames / 4} times");
            }
            finally
            {
                timer.Dispose();
            }

            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator Timer_LoopFramesLongerThanACycle_CompleteOncePerFrame()
        {
            yield return new EnterPlayMode();
            ManualFrameClock clock = new(2.5f);
            Timer timer = new(1f, 0f, true, true) { FrameClock = clock, CarryLoopOvershoot = true };
            int timerFrames = 0;
            List<int> completionTimerFrames = new();
            timer.OnTimerUpdate.AddListener((_, _) => timerFrames++);
            timer.OnTimerEnd.AddListener(() => completionTimerFrames.Add(timerFrames));
            try
            {
                timer.Start();
                for (int step = 0; step < MaxPlayModeSteps && completionTimerFrames.Count < 5; step++)
                {
                    yield return null;
                }

                Assert.That(timer.IsRunning, Is.True);
                Assert.That(completionTimerFrames.Count, Is.GreaterThanOrEqualTo(5),
                    "positive control: the loop completes");
                Assert.That(completionTimerFrames, Is.Unique,
                    "a carried overshoot of a whole cycle must not start a cycle that ends in the same timer frame");
            }
            finally
            {
                timer.Dispose();
            }

            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator StartAnimation_DisableMidPulseRestoresOriginalScale()
        {
            yield return new EnterPlayMode();
            GameObject host = new("StartAnimationTimer");
            try
            {
                host.transform.localScale = new Vector3(2f, 2f, 2f);
                TimerObject timer = host.AddComponent<TimerObject>();
                timer.autoStart = false;
                timer.enableStartAnimation = true;
                timer.startAnimationScale = 1.5f;
                timer.startAnimationDuration = 10f;
                timer.useUnscaledTime = true;
                timer.FrameClock = new ManualFrameClock(0.5f);
                timer.Play();
                for (int step = 0; step < MaxPlayModeSteps && !(host.transform.localScale.x > 2f); step++)
                {
                    yield return null;
                }

                Assert.That(host.transform.localScale.x, Is.GreaterThan(2f), "positive control: the pulse is running");

                timer.Play();
                host.SetActive(false);

                Assert.That(host.transform.localScale, Is.EqualTo(new Vector3(2f, 2f, 2f)));
            }
            finally
            {
                Object.Destroy(host);
            }

            yield return new ExitPlayMode();
        }

        private TextMeshProUGUI CreateHiddenLabel()
        {
            GameObject label = new("Timer label", typeof(RectTransform));
            label.transform.SetParent(_gameObject.transform, false);
            label.SetActive(false);
            return label.AddComponent<TextMeshProUGUI>();
        }

        private static void SetPrivateField(TimerObject timer, string fieldName, object value)
        {
            FieldInfo field = typeof(TimerObject).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(timer, value);
        }

        private static void InvokeUpdate(TimerObject timer)
        {
            MethodInfo update = typeof(TimerObject).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(update, Is.Not.Null);
            update.Invoke(timer, null);
        }

        private sealed class FakeClock : INeoClock
        {
            public DateTime UtcNow { get; set; }

            public double MonotonicSeconds { get; set; }
        }
    }
}
