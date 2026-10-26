using System.Collections.Generic;
using Neo.Extensions;
using Neo.Tools;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tests.Edit
{
    public class Audit20261023NeoToolsTextTimeToTextTests
    {
        private GameObject _go;
        private TimeToText _view;
        private TextMeshProUGUI _label;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("AuditTimeToText", typeof(RectTransform), typeof(TextMeshProUGUI));
            _label = _go.GetComponent<TextMeshProUGUI>();
            _view = _go.AddComponent<TimeToText>();
            _view.text = _label;
            _view.OnTimeChanged = new UnityEvent<float>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void FormatUpdateInsideListener_DoesNotPublishTheSameTimeTwice()
        {
            int notifications = 0;
            _view.OnTimeChanged.AddListener(value =>
            {
                notifications++;
                if (notifications == 1)
                {
                    _view.Separator = ".";
                }
            });

            _view.Set(90f);

            Assert.AreEqual(1, notifications);
            Assert.AreEqual("01.30", _label.text);
            Assert.AreEqual(90f, _view.CurrentTime);
        }

        [Test]
        public void NestedSet_KeepsTheLatestValueAsTheNotificationBaseline()
        {
            List<float> notifications = new List<float>();
            _view.OnTimeChanged.AddListener(value =>
            {
                notifications.Add(value);
                if (notifications.Count == 1)
                {
                    _view.Set(120f);
                }
            });

            _view.Set(90f);
            _view.Set(120f);

            CollectionAssert.AreEqual(new[] { 90f, 120f }, notifications);
            Assert.AreEqual(120f, _view.CurrentTime);
            Assert.AreEqual("02:00", _label.text);
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(float.MaxValue)]
        [TestCase(-float.MaxValue)]
        [TestCase(2147483648f)]
        public void UnsupportedClockInput_PreservesTimeTextAndEvents(float invalid)
        {
            _view.AllowNegative = true;
            _view.Set(90f);
            int notifications = 0;
            _view.OnTimeChanged.AddListener(value => notifications++);

            _view.Set(invalid);

            Assert.AreEqual(90f, _view.CurrentTime);
            Assert.AreEqual("01:30", _label.text);
            Assert.AreEqual(0, notifications);
        }

        [TestCase(true, TimeFormat.MinutesSeconds, float.MaxValue)]
        [TestCase(false, TimeFormat.MinutesSeconds, float.MaxValue)]
        public void UnsupportedCompactInput_PreservesTheLastValidDisplay(
            bool useFormat, TimeFormat format, float invalid)
        {
            _view.DisplayMode = TimeDisplayMode.Compact;
            _view.CompactUseTimeFormat = useFormat;
            _view.TimeFormat = format;
            _view.Set(90f);
            string previousText = _label.text;
            int notifications = 0;
            _view.OnTimeChanged.AddListener(value => notifications++);

            _view.Set(invalid);

            Assert.AreEqual(90f, _view.CurrentTime);
            Assert.AreEqual(previousText, _label.text);
            Assert.AreEqual(0, notifications);
        }

        [Test]
        public void DynamicCompact_AllowsDurationsBeyondTheClockSecondsRange()
        {
            _view.DisplayMode = TimeDisplayMode.Compact;
            _view.CompactUseTimeFormat = false;

            _view.Set(3000000000f);

            Assert.AreEqual(3000000000f, _view.CurrentTime);
            Assert.AreEqual("34722d 05h 20m", _label.text);
        }

        [TestCase(3000000000f)]
        [TestCase(-3000000000f)]
        public void DisplayMode_RejectsUnsupportedTransitionAndAcceptsSupportedTime(float seconds)
        {
            _view.DisplayMode = TimeDisplayMode.Compact;
            _view.CompactUseTimeFormat = false;
            _view.AllowNegative = true;
            _view.Set(seconds);
            string previousText = _label.text;
            List<float> notifications = new List<float>();
            _view.OnTimeChanged.AddListener(value => notifications.Add(value));

            _view.DisplayMode = TimeDisplayMode.Clock;

            Assert.AreEqual(TimeDisplayMode.Compact, _view.DisplayMode);
            Assert.AreEqual(seconds, _view.CurrentTime);
            Assert.AreEqual(previousText, _label.text);
            Assert.IsEmpty(notifications);
            _view.Set(seconds);
            Assert.IsEmpty(notifications, "A rejected mode change must preserve the event baseline.");

            _view.Set(90f);
            notifications.Clear();
            _view.DisplayMode = TimeDisplayMode.Clock;

            Assert.AreEqual(TimeDisplayMode.Clock, _view.DisplayMode);
            Assert.AreEqual(90f, _view.CurrentTime);
            Assert.AreEqual("01:30", _label.text);
            Assert.IsEmpty(notifications);
        }

        [TestCase(TimeFormat.Milliseconds, 3000000f, "3000000000ms")]
        [TestCase(TimeFormat.Seconds, 3000000000f, "3000000000s")]
        public void FixedCompact_AllowsWideTotalUnits(TimeFormat format, float seconds, string expected)
        {
            _view.DisplayMode = TimeDisplayMode.Compact;
            _view.TimeFormat = format;

            _view.Set(seconds);

            Assert.AreEqual(seconds, _view.CurrentTime);
            Assert.AreEqual(expected, _label.text);
        }

        [TestCase(TimeDisplayMode.Clock, TimeFormat.MinutesSeconds, "2147483648")]
        [TestCase(TimeDisplayMode.Compact, TimeFormat.MinutesSeconds, "1e30")]
        public void TrySetFromString_RejectsParsedButUndisplayableDuration(
            TimeDisplayMode mode, TimeFormat format, string raw)
        {
            _view.DisplayMode = mode;
            _view.TimeFormat = format;
            _view.Set(90f);
            string previousText = _label.text;
            int notifications = 0;
            _view.OnTimeChanged.AddListener(value => notifications++);

            bool result = _view.TrySetFromString(raw);

            Assert.IsFalse(result);
            Assert.AreEqual(90f, _view.CurrentTime);
            Assert.AreEqual(previousText, _label.text);
            Assert.AreEqual(0, notifications);
        }

        [Test]
        public void TrySetFromString_WithMissingTarget_DoesNotReportAnUpdate()
        {
            _view.text = null;

            bool result = _view.TrySetFromString("01:30");

            Assert.IsFalse(result);
            Assert.AreEqual(0f, _view.CurrentTime);
        }
    }
}
