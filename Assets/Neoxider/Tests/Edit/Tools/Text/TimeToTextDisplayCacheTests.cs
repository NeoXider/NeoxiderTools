using System;
using System.Reflection;
using Neo.Extensions;
using Neo.Tests.Performance;
using Neo.Tools;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.Tools
{
    public sealed class TimeToTextDisplayCacheTests
    {
        private GameObject _go;
        private TimeToText _view;
        private TextMeshProUGUI _label;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TimeToTextDisplayCache", typeof(RectTransform), typeof(TextMeshProUGUI));
            // WHY: an inactive label keeps TMP's own dirty/rebuild bookkeeping out of the allocation probe.
            _go.SetActive(false);
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
        public void GetDisplayText_SameVisibleSecond_ReturnsTheCachedInstance()
        {
            _view.TimeFormat = TimeFormat.MinutesSeconds;

            string first = GetDisplayText(75.05f);
            string sameSecond = GetDisplayText(75.95f);
            string nextSecond = GetDisplayText(76.0f);

            // WHY: identity is checked on the producer; TMP compares by value and keeps its first reference anyway.
            Assert.That(first, Is.EqualTo("01:15"));
            Assert.That(sameSecond, Is.SameAs(first), "The same visible second must reuse the formatted string.");
            Assert.That(nextSecond, Is.EqualTo("01:16"));
            Assert.That(nextSecond, Is.Not.SameAs(first));
        }

        [Test]
        public void Set_EveryFrameWithinTheSameSecond_KeepsTheProducedString()
        {
            _view.TimeFormat = TimeFormat.MinutesSeconds;
            _view.Set(75.05f);
            string cached = GetField<string>("_cachedDisplay");
            Assert.That(cached, Is.EqualTo("01:15"));

            for (int frame = 0; frame < 60; frame++)
            {
                _view.Set(75.15f + frame * 0.0125f);
                Assert.That(GetField<string>("_cachedDisplay"), Is.SameAs(cached), $"frame {frame}");
            }

            _view.Set(76.01f);
            Assert.That(GetField<string>("_cachedDisplay"), Is.EqualTo("01:16"));
        }

        [Test]
        public void Set_EveryFrameWithinTheSameSecond_MakesNoGcAllocCalls()
        {
            _view.TimeFormat = TimeFormat.MinutesSeconds;
            // WHY: warm TMP's same-string path and the UnityEvent's invocation list before measuring.
            _view.Set(75.05f);
            _view.Set(75.10f);
            long calls;
            using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
            {
                counter.Begin();
                for (int frame = 0; frame < 60; frame++)
                {
                    _view.Set(75.15f + frame * 0.0125f);
                }

                calls = counter.End();
            }

            Assert.That(calls, Is.Zero, "A per-frame timer must not allocate between visible changes.");
            Assert.That(_label.text, Is.EqualTo("01:15"));
        }

        [Test]
        public void SetText_StringWithoutAffixes_PassesTheSameInstanceAndMakesNoGcAllocCalls()
        {
            GameObject owner = new("SetTextNoAffixes", typeof(RectTransform), typeof(TextMeshProUGUI));
            owner.SetActive(false);
            try
            {
                SetText view = owner.AddComponent<SetText>();
                view.text = owner.GetComponent<TextMeshProUGUI>();
                view.OnTextUpdated = new UnityEvent<string>();
                string published = null;
                view.OnTextUpdated.AddListener(value => published = value);
                // WHY: a runtime-built string, so identity proves no affix copy was made (literals are interned).
                string value = new('7', 3);

                view.Set(value);
                Assert.That(published, Is.SameAs(value), "Empty affixes must not copy the string.");
                Assert.That(view.text.text, Is.SameAs(value));

                long calls;
                using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
                {
                    counter.Begin();
                    for (int i = 0; i < 60; i++)
                    {
                        view.Set(value);
                    }

                    calls = counter.End();
                }

                Assert.That(calls, Is.Zero, "Re-setting a string without affixes must not allocate.");
                Assert.That(published, Is.SameAs(value));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [TestCase(TimeDisplayMode.Clock, true)]
        [TestCase(TimeDisplayMode.Compact, true)]
        [TestCase(TimeDisplayMode.Compact, false)]
        public void UnitKey_WholeSecondFormats_ShareAKeyWithinTheSecond(TimeDisplayMode mode, bool useTimeFormat)
        {
            SetField("_displayMode", mode);
            SetField("_compactUseTimeFormat", useTimeFormat);
            SetField("timeFormat", TimeFormat.MinutesSeconds);

            Assert.That(GetUnitKey(1.2f, mode), Is.EqualTo(GetUnitKey(1.7f, mode)));
            Assert.That(GetUnitKey(1.7f, mode), Is.Not.EqualTo(GetUnitKey(2.1f, mode)));
        }

        [TestCase(TimeDisplayMode.Clock, TimeFormat.SecondsMilliseconds)]
        [TestCase(TimeDisplayMode.Compact, TimeFormat.SecondsMilliseconds)]
        [TestCase(TimeDisplayMode.Clock, (TimeFormat)999)]
        [TestCase(TimeDisplayMode.Compact, (TimeFormat)999)]
        public void UnitKey_SubSecondAndUnknownFormats_FallBackToTheExactValue(TimeDisplayMode mode,
            TimeFormat format)
        {
            SetField("_displayMode", mode);
            SetField("_compactUseTimeFormat", true);
            SetField("timeFormat", format);

            Assert.That(GetUnitKey(1.2f, mode), Is.Not.EqualTo(GetUnitKey(1.7f, mode)),
                "A format not known to show whole seconds must never reuse text across sub-second values.");
            Assert.That(GetUnitKey(1.2f, mode), Is.EqualTo(GetUnitKey(1.2f, mode)));
        }

        [TestCase(TimeDisplayMode.Clock, TimeFormat.MinutesSeconds, true)]
        [TestCase(TimeDisplayMode.Clock, TimeFormat.HoursMinutesSeconds, true)]
        [TestCase(TimeDisplayMode.Clock, TimeFormat.SecondsMilliseconds, true)]
        [TestCase(TimeDisplayMode.Clock, TimeFormat.MinutesSecondsMilliseconds, true)]
        [TestCase(TimeDisplayMode.Compact, TimeFormat.MinutesSeconds, true)]
        [TestCase(TimeDisplayMode.Compact, TimeFormat.SecondsMilliseconds, true)]
        [TestCase(TimeDisplayMode.Compact, TimeFormat.HoursMinutes, false)]
        public void Set_CountUpAndDown_MatchesTheUncachedFormatter(TimeDisplayMode mode, TimeFormat format,
            bool compactUseTimeFormat)
        {
            _view.DisplayMode = mode;
            _view.TimeFormat = format;
            _view.CompactUseTimeFormat = compactUseTimeFormat;
            _view.CompactIncludeSeconds = true;
            _view.CompactMaxParts = 3;

            for (int step = 0; step < 500; step++)
            {
                AssertMatches(step * 0.37f, mode, format, compactUseTimeFormat);
            }

            for (int step = 500; step >= 0; step--)
            {
                AssertMatches(step * 0.29f, mode, format, compactUseTimeFormat);
            }
        }

        [Test]
        public void Set_AffixesChangedThroughSerializedFields_RefreshTheCachedText()
        {
            _view.TimeFormat = TimeFormat.MinutesSeconds;
            _view.Set(30f);
            Assert.That(_label.text, Is.EqualTo("00:30"));

            SetField("startAddText", "T-");
            SetField("endAddText", "!");
            _view.Set(30.2f);

            Assert.That(_label.text, Is.EqualTo("T-00:30!"));
        }

        [Test]
        public void Set_NegativeWithinTheSameSecond_KeepsSignAndAffixes()
        {
            _view.AllowNegative = true;
            _view.TimeFormat = TimeFormat.MinutesSeconds;
            SetField("startAddText", "[");
            SetField("endAddText", "]");

            _view.Set(-65.5f);
            Assert.That(_label.text, Is.EqualTo("[-01:05]"));
            _view.Set(-65.2f);
            Assert.That(_label.text, Is.EqualTo("[-01:05]"));
            _view.Set(65.2f);
            Assert.That(_label.text, Is.EqualTo("[01:05]"), "Sign is part of the cache key.");
        }

        [Test]
        public void PrefixAndSuffix_RedrawTheCurrentTimeImmediately()
        {
            _view.TimeFormat = TimeFormat.MinutesSeconds;
            _view.Set(42f);

            _view.Prefix = "Time ";
            _view.Suffix = " left";

            Assert.That(_label.text, Is.EqualTo("Time 00:42 left"));
            Assert.That(_view.Prefix, Is.EqualTo("Time "));
            Assert.That(_view.Suffix, Is.EqualTo(" left"));
        }

        [Test]
        public void SetText_PrefixAndSuffix_RedrawRetainedNumbersAndApplyToLaterStrings()
        {
            GameObject owner = new("SetTextAffixes", typeof(RectTransform), typeof(TextMeshProUGUI));
            owner.SetActive(false);
            try
            {
                SetText view = owner.AddComponent<SetText>();
                view.text = owner.GetComponent<TextMeshProUGUI>();
                view.OnTextUpdated = new UnityEvent<string>();

                view.SetBigInteger(new System.Numerics.BigInteger(5));
                view.Prefix = "x";
                view.Suffix = "!";
                Assert.That(view.text.text, Is.EqualTo("x5!"), "A retained number redraws with the new affixes.");

                view.Set("ready");
                Assert.That(view.text.text, Is.EqualTo("xready!"));

                view.Prefix = string.Empty;
                view.Suffix = null;
                view.Set("plain");
                Assert.That(view.text.text, Is.EqualTo("plain"));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        private void AssertMatches(float seconds, TimeDisplayMode mode, TimeFormat format, bool compactUseTimeFormat)
        {
            _view.Set(seconds);
            string expected = mode == TimeDisplayMode.Compact
                ? compactUseTimeFormat
                    ? TimeSpan.FromSeconds(seconds).ToCompactString(format)
                    : TimeSpan.FromSeconds(seconds).ToCompactString(true, 3)
                : seconds.FormatTime(format, ":");
            Assert.That(_label.text, Is.EqualTo(expected), $"{mode}/{format} at {seconds:R}s");
        }

        private void SetField(string name, object value)
        {
            FieldInfo field = typeof(TimeToText).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(_view, value);
        }

        private T GetField<T>(string name)
        {
            FieldInfo field = typeof(TimeToText).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            return (T)field.GetValue(_view);
        }

        private string GetDisplayText(float seconds)
        {
            MethodInfo method = typeof(TimeToText).GetMethod("GetDisplayText",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (string)method.Invoke(_view, new object[] { seconds, false });
        }

        private long GetUnitKey(float seconds, TimeDisplayMode mode)
        {
            MethodInfo method = typeof(TimeToText).GetMethod("GetDisplayUnitKey",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            bool compact = mode == TimeDisplayMode.Compact;
            TimeSpan span = compact ? TimeSpan.FromSeconds(seconds) : default;
            return (long)method.Invoke(_view, new object[] { seconds, span, compact });
        }
    }
}
