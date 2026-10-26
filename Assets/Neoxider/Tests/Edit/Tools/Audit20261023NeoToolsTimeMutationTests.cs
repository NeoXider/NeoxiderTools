using System;
using System.Collections.Generic;
using Neo.Tools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;
#if UNITY_TEXTMESHPRO
using TMPro;
#endif

namespace Neo.Editor.Tests
{
    public class Audit20261023NeoToolsTimeMutationTests
    {
        private GameObject _gameObject;
        private Audit20261023NeoToolsTimeTimerProbe _timer;
        private Image _image;
        private readonly List<int> _percents = new();
        private int _completed;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject(nameof(Audit20261023NeoToolsTimeMutationTests),
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _image = _gameObject.GetComponent<Image>();
            _image.type = Image.Type.Filled;
            _timer = _gameObject.AddComponent<Audit20261023NeoToolsTimeTimerProbe>();
            _timer.autoStart = false;
            _timer.countUp = true;
            _timer.duration = 10f;
            _timer.updateInterval = 0f;
            _timer.useUnscaledTime = true;
            _timer.progressImage = _image;
            _timer.fillImageNormal = true;
            _timer.OnProgressChanged = new UnityEvent<float>();
            _timer.OnProgressPercentChanged = new UnityEvent<int>();
            _timer.OnTimerCompleted = new UnityEvent();
            _timer.OnProgressPercentChanged.AddListener(percent => _percents.Add(percent));
            _timer.OnTimerCompleted.AddListener(() => _completed++);
            _percents.Clear();
            _completed = 0;
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
            {
                Object.DestroyImmediate(_gameObject);
            }
        }

        [TestCase(nameof(TimerObject.SetTime), 5f, 10f)]
        [TestCase(nameof(TimerObject.SetProgress), 5f, 10f)]
        [TestCase(nameof(TimerObject.AddTime), 5f, 10f)]
        [TestCase(nameof(TimerObject.SetDuration), 10f, 20f)]
        public void TypedMutationFromTerminalProgress_DropsOldPercentUiAndCompletion(
            string operation, float expectedTime, float expectedDuration)
        {
            bool changed = false;
            _timer.OnProgressChanged.AddListener(progress =>
            {
                if (changed || progress < 1f)
                {
                    return;
                }

                changed = true;
                switch (operation)
                {
                    case nameof(TimerObject.SetTime):
                        _timer.SetTime(5f);
                        break;
                    case nameof(TimerObject.SetProgress):
                        _timer.SetProgress(0.5f);
                        break;
                    case nameof(TimerObject.AddTime):
                        _timer.AddTime(-5f);
                        break;
                    case nameof(TimerObject.SetDuration):
                        _timer.SetDuration(20f, false);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(operation));
                }
            });
            _timer.Play();

            _timer.Tick(10f);

            Assert.That(changed, Is.True);
            Assert.That(_timer.CurrentTime, Is.EqualTo(expectedTime));
            Assert.That(_timer.TimeValue, Is.EqualTo(expectedTime));
            Assert.That(_timer.duration, Is.EqualTo(expectedDuration));
            Assert.That(_percents[_percents.Count - 1], Is.EqualTo(50));
            Assert.That(_image.fillAmount, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(_completed, Is.Zero);
            Assert.That(_timer.IsRunning, Is.False, "A setter must not start the stopped timer.");
        }

        [Test]
        public void SetterFromStartedEvent_DoesNotPublishTwice()
        {
            _timer.OnTimerStarted = new UnityEvent();
            _timer.OnTimerStarted.AddListener(() => _timer.SetTime(5f));

            _timer.Play();

            Assert.That(_timer.CurrentTime, Is.EqualTo(5f));
            Assert.That(_percents, Is.EqualTo(new[] { 50 }));
            Assert.That(_image.fillAmount, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(_timer.IsRunning, Is.True);
        }

        [Test]
        public void SetterFromResetReactiveEvent_SupersedesOldResetNotifications()
        {
            _timer.Play();
            _timer.Tick(3f);
            _percents.Clear();
            bool changed = false;
            _timer.Time.AddListener(value =>
            {
                if (!changed && value == 0f)
                {
                    changed = true;
                    _timer.SetTime(5f);
                }
            });

            _timer.Reset();

            Assert.That(changed, Is.True);
            Assert.That(_timer.CurrentTime, Is.EqualTo(5f));
            Assert.That(_percents, Is.EqualTo(new[] { 50 }));
            Assert.That(_image.fillAmount, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void SetterFromStoppedEvent_IsNotOverwrittenByReset()
        {
            _timer.Play();
            _timer.Tick(3f);
            _percents.Clear();
            _timer.OnTimerStopped = new UnityEvent();
            _timer.OnTimerStopped.AddListener(() => _timer.SetTime(5f));

            _timer.Stop();

            Assert.That(_timer.CurrentTime, Is.EqualTo(5f));
            Assert.That(_percents, Is.EqualTo(new[] { 50 }));
            Assert.That(_timer.IsRunning, Is.False);
        }

        [Test]
        public void SetterFromStartTimerReset_PreventsAStalePlay()
        {
            bool changed = false;
            int started = 0;
            _timer.OnTimerStarted = new UnityEvent();
            _timer.OnTimerStarted.AddListener(() => started++);
            _timer.OnProgressChanged.AddListener(progress =>
            {
                if (!changed && progress == 0f)
                {
                    changed = true;
                    _timer.SetTime(5f);
                }
            });

            _timer.StartTimer();

            Assert.That(changed, Is.True);
            Assert.That(_timer.CurrentTime, Is.EqualTo(5f));
            Assert.That(_percents, Is.EqualTo(new[] { 50 }));
            Assert.That(started, Is.Zero);
            Assert.That(_timer.IsRunning, Is.False);
        }

        [Test]
        public void SetterFromDayTimeEvent_PreservesTheNewImageValue()
        {
            ConfigureDay();
            _timer.Play();
            bool changed = false;
            _timer.OnDayTimeChanged = new UnityEvent<float, float, float>();
            _timer.OnDayTimeChanged.AddListener((hours, minutes, seconds) =>
            {
                if (!changed)
                {
                    changed = true;
                    _timer.SetTime(25f);
                }
            });

            _timer.Tick(50f);

            Assert.That(changed, Is.True);
            Assert.That(_timer.CurrentTime, Is.EqualTo(25f));
            Assert.That(_image.fillAmount, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(_completed, Is.Zero);
            Assert.That(_timer.IsRunning, Is.True);
        }

        [Test]
        public void SetterFromNewDayEvent_PreventsTheOldStopAndCompletion()
        {
            ConfigureDay();
            _timer.Play();
            _timer.OnNewDay = new UnityEvent();
            _timer.OnNewDay.AddListener(() => _timer.SetTime(25f));

            _timer.Tick(100f);

            Assert.That(_timer.CurrentTime, Is.EqualTo(25f));
            Assert.That(_image.fillAmount, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(_timer.IsRunning, Is.True);
            Assert.That(_completed, Is.Zero);
        }

        [Test]
        public void SetterFromWrappedDayProgress_PreventsStaleCompletionAndUi()
        {
            ConfigureDay();
            _timer.Play();
            bool changed = false;
            _timer.OnProgressChanged.AddListener(progress =>
            {
                if (!changed && progress == 0f)
                {
                    changed = true;
                    _timer.SetTime(25f);
                }
            });

            _timer.Tick(100f);

            Assert.That(changed, Is.True);
            Assert.That(_timer.CurrentTime, Is.EqualTo(25f));
            Assert.That(_percents[_percents.Count - 1], Is.EqualTo(25));
            Assert.That(_image.fillAmount, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(_completed, Is.Zero);
            Assert.That(_timer.IsRunning, Is.False);
        }

#if UNITY_TEXTMESHPRO
        [Test]
        public void InfiniteSetterFromReactiveEvent_DoesNotRenderTheOldSeconds()
        {
            TextMeshProUGUI text = CreateInactiveText();
            _timer.infiniteDuration = true;
            bool changed = false;
            _timer.Time.AddListener(value =>
            {
                if (!changed && value == 10f)
                {
                    changed = true;
                    _timer.SetTime(5f);
                }
            });

            _timer.SetTime(10f);

            Assert.That(changed, Is.True);
            Assert.That(_timer.CurrentTime, Is.EqualTo(5f));
            Assert.That(_timer.TimeValue, Is.EqualTo(5f));
            Assert.That(text.text, Is.EqualTo("5"));
        }

        [Test]
        public void SetterFromImageDirtyCallback_DoesNotRenderTheOldSeconds()
        {
            _gameObject.AddComponent<Canvas>();
            TextMeshProUGUI text = CreateInactiveText();
            _timer.Play();
            bool changed = false;
            _image.RegisterDirtyVerticesCallback(() =>
            {
                if (!changed)
                {
                    changed = true;
                    _timer.SetTime(5f);
                }
            });

            _timer.SetTime(10f);

            Assert.That(changed, Is.True);
            Assert.That(_timer.CurrentTime, Is.EqualTo(5f));
            Assert.That(_image.fillAmount, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(text.text, Is.EqualTo("5"));
        }

        private TextMeshProUGUI CreateInactiveText()
        {
            GameObject label = new("Inactive timer text", typeof(RectTransform));
            label.transform.SetParent(_gameObject.transform, false);
            label.SetActive(false);
            TextMeshProUGUI text = label.AddComponent<TextMeshProUGUI>();
            _timer.timeText = text;
            _timer.timeFormat = "{0:F0}";
            return text;
        }
#endif

        private void ConfigureDay()
        {
            using SerializedObject configuration = new(_timer);
            configuration.FindProperty("_dayLengthSeconds").floatValue = 100f;
            configuration.ApplyModifiedPropertiesWithoutUndo();
            _timer.duration = 100f;
        }
    }
}
