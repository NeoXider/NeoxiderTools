using System;
using System.Globalization;
using Neo.Save;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    public class Audit20261023NeoToolsTimeUtcHookTests
    {
        private Audit20261023NeoToolsTimePersistenceScope _scope;
        private GameObject _gameObject;
        private Audit20261023NeoToolsTimeTimerProbe _timer;
        private DateTime _utc;
        private string _key;

        [SetUp]
        public void SetUp()
        {
            _scope = new Audit20261023NeoToolsTimePersistenceScope();
            _gameObject = new GameObject(nameof(Audit20261023NeoToolsTimeUtcHookTests));
            _timer = _gameObject.AddComponent<Audit20261023NeoToolsTimeTimerProbe>();
            _timer.autoStart = false;
            _timer.duration = 600f;
            _timer.updateInterval = 0f;
            _timer.useUnscaledTime = true;
            _utc = new DateTime(2035, 4, 5, 12, 0, 0, DateTimeKind.Utc);
            _key = "Audit20261023NeoToolsTime.Utc." + Guid.NewGuid().ToString("N");
            _timer.UtcNowProvider = () => _utc;
            _timer.ConfigureRealTimeSave(_key);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (_timer != null)
                {
                    _timer.DisableSaving();
                }

                if (_gameObject != null)
                {
                    Object.DestroyImmediate(_gameObject);
                }
            }
            finally
            {
                _scope?.Dispose();
            }
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void RealTimeSave_UsesOverrideForEveryTimerMode(bool countUp, bool infinite)
        {
            _timer.countUp = countUp;
            _timer.infiniteDuration = infinite;
            _timer.Play();
            _timer.SetTime(120f);

            _timer.SaveForTest();

            DateTime anchor = ReadAnchor();
            Assert.That(anchor, Is.EqualTo(_utc.AddSeconds(countUp ? -120d : 120d)));
            Assert.That(anchor.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(SaveProvider.GetBool(_key + "_a"), Is.True);
        }

        [TestCase(false, false, 90f)]
        [TestCase(true, false, 150f)]
        [TestCase(true, true, 150f)]
        public void RealTimeRestore_UsesTheSameOverrideAsSave(bool countUp, bool infinite, float expected)
        {
            _timer.countUp = countUp;
            _timer.infiniteDuration = infinite;
            _timer.Play();
            _timer.SetTime(120f);
            _timer.SaveForTest();
            _utc = _utc.AddSeconds(30d);
            _timer.SetTime(0f);

            _timer.RestoreForTest();

            Assert.That(_timer.CurrentTime, Is.EqualTo(expected));
            Assert.That(_timer.TimeValue, Is.EqualTo(expected));
            Assert.That(_timer.IsRunning, Is.True);
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void PausedRealTimeRestore_RemainsFrozenWithAnOverride(bool countUp, bool infinite)
        {
            _timer.countUp = countUp;
            _timer.infiniteDuration = infinite;
            _timer.Play();
            _timer.SetTime(120f);
            _timer.Pause();
            _timer.SaveForTest();
            _utc = _utc.AddHours(1d);
            _timer.Play();

            _timer.RestoreForTest();

            Assert.That(_timer.CurrentTime, Is.EqualTo(120f));
            Assert.That(_timer.TimeValue, Is.EqualTo(120f));
            Assert.That(_timer.IsRunning, Is.False);
        }

        [Test]
        public void BaseUtcHook_StillSavesADeviceUtcDeadline()
        {
            _timer.UtcNowProvider = null;
            _timer.countUp = false;
            _timer.Play();
            _timer.SetTime(120f);
            DateTime before = DateTime.UtcNow;

            _timer.SaveForTest();

            DateTime after = DateTime.UtcNow;
            Assert.That(ReadAnchor(), Is.InRange(before.AddSeconds(120d), after.AddSeconds(120d)));
        }

        [Test]
        public void SecondsPersistence_DoesNotReadTheUtcHook()
        {
            _timer.ConfigureSave(_key, TimerSaveMode.Seconds);
            _timer.UtcNowProvider = () => throw new InvalidOperationException("Seconds mode must not read UTC.");
            _timer.Play();
            _timer.SetTime(120f);
            _timer.SaveForTest();
            _timer.SetTime(0f);

            _timer.RestoreForTest();

            Assert.That(_timer.CurrentTime, Is.EqualTo(120f));
            Assert.That(_timer.TimeValue, Is.EqualTo(120f));
            Assert.That(_timer.IsRunning, Is.True);
            Assert.That(SaveProvider.HasKey(_key + "_rt"), Is.False);
        }

        private DateTime ReadAnchor()
        {
            return DateTime.Parse(SaveProvider.GetString(_key + "_rt"), CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);
        }
    }
}
