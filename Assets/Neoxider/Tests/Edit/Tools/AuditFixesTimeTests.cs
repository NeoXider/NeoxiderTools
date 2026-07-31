using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Covers TimerObject.Reset: a countdown must return to its full duration instead of 0,
    ///     which previously made a stopped timer report 100% progress.
    /// </summary>
    [TestFixture]
    public class AuditFixesTimeTests
    {
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("AuditFixesTimerObject");
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                Object.DestroyImmediate(_go);
                _go = null;
            }
        }

        private TimerObject CreateTimer(bool countUp, float duration, float initialProgress = 0f)
        {
            TimerObject timer = _go.AddComponent<TimerObject>();
            timer.autoStart = false;
            timer.countUp = countUp;
            timer.duration = duration;
            timer.initialProgress = initialProgress;
            return timer;
        }

        [Test]
        public void Reset_Countdown_ReturnsToFullDuration()
        {
            TimerObject timer = CreateTimer(false, 60f);

            timer.Reset();

            Assert.That(timer.CurrentTime, Is.EqualTo(60f).Within(0.001f), "a countdown resets to its full duration");
            Assert.That(timer.Progress, Is.EqualTo(0f).Within(0.001f));
            Assert.That(timer.IsCompleted, Is.False, "a reset countdown must not report completion");
        }

        [Test]
        public void Stop_Countdown_DoesNotReportFullProgress()
        {
            TimerObject timer = CreateTimer(false, 60f);
            timer.Play();

            // WHY: AddComponent does not run Unity's serialization pass, so every UnityEvent field on a
            // component created in code is null until something assigns it.
            timer.OnProgressChanged = new UnityEvent<float>();

            float reportedProgress = -1f;
            timer.OnProgressChanged.AddListener(progress => reportedProgress = progress);

            timer.Stop();

            Assert.That(reportedProgress, Is.EqualTo(0f).Within(0.001f),
                "a stopped countdown must not render as 100% complete");
            Assert.That(timer.CurrentTime, Is.EqualTo(60f).Within(0.001f));
        }

        [Test]
        public void Reset_CountUp_StartsAtZero()
        {
            TimerObject timer = CreateTimer(true, 60f);

            timer.Reset();

            Assert.That(timer.CurrentTime, Is.EqualTo(0f).Within(0.001f));
            Assert.That(timer.Progress, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void Reset_WithInitialProgress_KeepsConfiguredStartTime()
        {
            TimerObject countdown = CreateTimer(false, 60f, 0.25f);

            countdown.Reset();

            Assert.That(countdown.CurrentTime, Is.EqualTo(45f).Within(0.001f),
                "initial progress must still define the countdown start time");
        }
    }
}
