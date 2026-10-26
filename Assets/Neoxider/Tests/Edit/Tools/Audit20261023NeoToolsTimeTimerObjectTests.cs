using System;
using Neo.Save;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    public class Audit20261023NeoToolsTimeTimerObjectTests
    {
        private GameObject _gameObject;
        private Audit20261023NeoToolsTimeTimerProbe _timer;
        private string _saveKey;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject(nameof(Audit20261023NeoToolsTimeTimerObjectTests));
            _timer = _gameObject.AddComponent<Audit20261023NeoToolsTimeTimerProbe>();
            _timer.autoStart = false;
            _timer.duration = 10f;
            _timer.updateInterval = 0f;
            _timer.useUnscaledTime = true;
            _saveKey = "Audit20261023NeoToolsTime." + Guid.NewGuid().ToString("N");
        }

        [TearDown]
        public void TearDown()
        {
            _timer.DisableSaving();
            Object.DestroyImmediate(_gameObject);
            SaveProvider.DeleteKey(_saveKey + "_t");
            SaveProvider.DeleteKey(_saveKey + "_a");
            SaveProvider.DeleteKey(_saveKey + "_rt");
        }

        [Test]
        public void TogglePause_FreezesThenResumesCurrentTimeAndEvents()
        {
            int paused = 0;
            int resumed = 0;
            _timer.OnTimerPaused = new UnityEvent();
            _timer.OnTimerResumed = new UnityEvent();
            _timer.OnTimerPaused.AddListener(() => paused++);
            _timer.OnTimerResumed.AddListener(() => resumed++);
            _timer.Play();
            _timer.Tick(2f);

            _timer.TogglePause();
            Assert.That(_timer.IsRunning, Is.False);
            _timer.Tick(2f);
            Assert.That(_timer.CurrentTime, Is.EqualTo(2f));

            _timer.TogglePause();
            _timer.Tick(1f);
            Assert.That(_timer.IsRunning, Is.True);
            Assert.That(_timer.CurrentTime, Is.EqualTo(3f));
            Assert.That(paused, Is.EqualTo(1));
            Assert.That(resumed, Is.EqualTo(1));
        }

        [TestCase(true, 0f)]
        [TestCase(false, 10f)]
        public void StopAfterPause_ResetsTimeAndNotifies(bool countUp, float expectedTime)
        {
            _timer.countUp = countUp;
            int stopped = 0;
            _timer.OnTimerStopped = new UnityEvent();
            _timer.OnTimerStopped.AddListener(() => stopped++);
            _timer.Play();
            _timer.Tick(2f);
            _timer.Pause();

            _timer.Stop();

            Assert.That(_timer.CurrentTime, Is.EqualTo(expectedTime));
            Assert.That(_timer.Progress, Is.Zero);
            Assert.That(_timer.IsRunning, Is.False);
            Assert.That(stopped, Is.EqualTo(1));
        }

        [Test]
        public void Reset_DiscardsElapsedTimeBufferedBeforeReset()
        {
            _timer.updateInterval = 1f;
            _timer.Play();
            _timer.Tick(0.75f);

            _timer.Reset();
            _timer.Tick(0.25f);

            Assert.That(_timer.CurrentTime, Is.Zero);
            _timer.Tick(0.75f);
            Assert.That(_timer.CurrentTime, Is.EqualTo(1f));
        }

        [Test]
        public void RestartFromCompletion_DoesNotReceiveAnExtraAutomaticLoopRestart()
        {
            _timer.looping = true;
            int started = 0;
            _timer.OnTimerStarted = new UnityEvent();
            _timer.OnTimerCompleted = new UnityEvent();
            _timer.OnTimerStarted.AddListener(() => started++);
            _timer.OnTimerCompleted.AddListener(_timer.Play);
            _timer.Play();

            _timer.Tick(10f);

            Assert.That(started, Is.EqualTo(2));
            Assert.That(_timer.IsRunning, Is.True);
            Assert.That(_timer.CurrentTime, Is.Zero);
        }

        [Test]
        public void StopFromLoopCompletion_RemainsStopped()
        {
            _timer.looping = true;
            _timer.OnTimerCompleted = new UnityEvent();
            _timer.OnTimerCompleted.AddListener(_timer.Stop);
            _timer.Play();

            _timer.Tick(10f);

            Assert.That(_timer.IsRunning, Is.False);
            Assert.That(_timer.CurrentTime, Is.Zero);
        }

        [Test]
        public void CompletionHandlerTick_PreservesBufferedTimeForRestartedRun()
        {
            _timer.updateInterval = 1f;
            _timer.OnTimerCompleted = new UnityEvent();
            _timer.OnTimerCompleted.AddListener(() =>
            {
                _timer.Play();
                _timer.Tick(0.25f);
            });
            _timer.Play();

            _timer.Tick(10f);
            _timer.Tick(0.75f);

            Assert.That(_timer.CurrentTime, Is.EqualTo(1f));
            Assert.That(_timer.IsRunning, Is.True);
        }

        [Test]
        public void MilestonesConfiguredFromCode_FireAgainAfterPlay()
        {
            _timer.enableMilestones = true;
            _timer.milestonePercentages = new[] { 0.25f };
            _timer.OnMilestoneReached = new UnityEvent<float>();
            int milestones = 0;
            _timer.OnMilestoneReached.AddListener(value => milestones++);

            _timer.Play();
            _timer.Tick(3f);
            _timer.Play();
            _timer.Tick(3f);

            Assert.That(milestones, Is.EqualTo(2));
            Assert.That(_timer.CurrentTime, Is.EqualTo(3f));
        }

        [Test]
        public void MilestoneArrayGrowthFromCallback_RearmsTheNewConfiguration()
        {
            _timer.enableMilestones = true;
            _timer.milestonePercentages = new[] { 0.25f };
            _timer.OnMilestoneReached = new UnityEvent<float>();
            int milestones = 0;
            _timer.OnMilestoneReached.AddListener(value =>
            {
                milestones++;
                if (milestones == 1)
                {
                    _timer.milestonePercentages = new[] { 0.25f, 0.5f, 0.75f };
                }
            });
            _timer.Play();
            _timer.Tick(3f);

            _timer.Tick(3f);

            Assert.That(milestones, Is.EqualTo(3));
            Assert.That(_timer.CurrentTime, Is.EqualTo(6f));
        }

        [Test]
        public void RestartFromProgressHandler_DoesNotPublishStalePercentOrCompletion()
        {
            int lastPercent = -1;
            int completed = 0;
            _timer.OnProgressChanged = new UnityEvent<float>();
            _timer.OnProgressPercentChanged = new UnityEvent<int>();
            _timer.OnTimerCompleted = new UnityEvent();
            _timer.OnProgressChanged.AddListener(progress =>
            {
                if (progress >= 1f)
                {
                    _timer.Play();
                }
            });
            _timer.OnProgressPercentChanged.AddListener(percent => lastPercent = percent);
            _timer.OnTimerCompleted.AddListener(() => completed++);
            _timer.Play();

            _timer.Tick(10f);

            Assert.That(_timer.CurrentTime, Is.Zero);
            Assert.That(_timer.IsRunning, Is.True);
            Assert.That(lastPercent, Is.Zero);
            Assert.That(completed, Is.Zero);
        }

        [Test]
        public void InfiniteTimer_MutationsPreserveUnboundedElapsedSeconds()
        {
            _timer.infiniteDuration = true;
            _timer.Play();

            _timer.SetTime(25f);
            Assert.That(_timer.CurrentTime, Is.EqualTo(25f));
            _timer.AddTime(5f);
            Assert.That(_timer.CurrentTime, Is.EqualTo(30f));
            _timer.SetDuration(1f);
            Assert.That(_timer.CurrentTime, Is.EqualTo(30f));
            Assert.That(_timer.TimeValue, Is.EqualTo(30f));
            Assert.That(_timer.Progress, Is.Zero);
            _timer.AddTime(-100f);
            Assert.That(_timer.CurrentTime, Is.Zero);
        }

        [Test]
        public void DayCycle_AddTimeWrapsBothDirectionsLikeSetTime()
        {
            SerializedObject configuration = new(_timer);
            configuration.FindProperty("_dayLengthSeconds").floatValue = 100f;
            configuration.ApplyModifiedPropertiesWithoutUndo();
            _timer.duration = 100f;
            _timer.SetTime(95f);

            _timer.AddTime(10f);
            Assert.That(_timer.CurrentTime, Is.EqualTo(5f));
            Assert.That(_timer.TimeValue, Is.EqualTo(5f));

            _timer.AddTime(-10f);
            Assert.That(_timer.CurrentTime, Is.EqualTo(95f));
        }

        [Test]
        public void PausedInfiniteRealTimeSave_RestoresFrozenSecondsInsteadOfOfflineElapsedTime()
        {
            _timer.infiniteDuration = true;
            _timer.ConfigureRealTimeSave(_saveKey);
            _timer.Play();
            _timer.Tick(25f);
            _timer.Pause();
            _timer.SaveForTest();
            // Move the stored start into the past to model reopening after a long offline interval.
            SaveProvider.SetString(_saveKey + "_rt", DateTime.UtcNow.AddHours(-1).ToString("o"));
            _timer.Play();

            _timer.RestoreForTest();

            Assert.That(_timer.IsRunning, Is.False);
            Assert.That(_timer.CurrentTime, Is.EqualTo(25f));
            Assert.That(_timer.TimeValue, Is.EqualTo(25f));
            _timer.Tick(1f);
            Assert.That(_timer.CurrentTime, Is.EqualTo(25f));
        }

        [Test]
        public void LegacyActiveInfiniteRealTimeSave_StillUsesTimestampWithoutFrozenSeconds()
        {
            _timer.infiniteDuration = true;
            _timer.ConfigureRealTimeSave(_saveKey);
            SaveProvider.SetString(_saveKey + "_rt", DateTime.UtcNow.AddMinutes(-5).ToString("o"));

            _timer.RestoreForTest();

            Assert.That(_timer.IsRunning, Is.True);
            Assert.That(_timer.CurrentTime, Is.InRange(299f, 305f));
            Assert.That(_timer.TimeValue, Is.EqualTo(_timer.CurrentTime));
        }
    }
}
