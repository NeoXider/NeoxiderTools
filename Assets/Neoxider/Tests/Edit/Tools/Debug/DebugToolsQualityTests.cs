using System;
using System.Reflection;
using System.Text;
using Neo.Audio;
using Neo.Tests.Performance;
using Neo.Tools;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.Tools
{
    public sealed class DebugToolsQualityTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const int OverlayRefreshes = 64;

        private GameObject _owner;
        private TextMeshProUGUI _text;

        [SetUp]
        public void SetUp()
        {
            _owner = new GameObject("DebugToolsQuality", typeof(RectTransform), typeof(TextMeshProUGUI));
            _owner.SetActive(false);
            _text = _owner.GetComponent<TextMeshProUGUI>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_owner);
        }

        [Test]
        public void ErrorLogger_MaxEntries_DropsTheOldestAndRebuildsTheLabel()
        {
            ErrorLogger logger = CreateLogger(2, true);

            Log(logger, "first");
            Log(logger, "second");
            Log(logger, "third");

            Assert.That(logger.Entries, Has.Count.EqualTo(2));
            Assert.That(_text.text, Does.Not.Contain("first"));
            Assert.That(_text.text, Does.Contain("second"));
            Assert.That(_text.text, Does.Contain("third"));
        }

        [Test]
        public void ErrorLogger_DroppedEntry_IsNoLongerTreatedAsADuplicate()
        {
            ErrorLogger logger = CreateLogger(2, true);
            logger.checkExistingErrors = true;

            Log(logger, "first");
            Log(logger, "first");
            Assert.That(logger.Entries, Has.Count.EqualTo(1), "Duplicates inside the history are suppressed.");

            Log(logger, "second");
            Log(logger, "third");
            Log(logger, "first");

            Assert.That(logger.Entries, Has.Count.EqualTo(2));
            Assert.That(logger.Entries[1], Does.Contain("first"));
        }

        [Test]
        public void ErrorLogger_LoweringMaxEntries_TrimsHistoryAndLabelImmediately()
        {
            ErrorLogger logger = CreateLogger(0, true);
            Log(logger, "first");
            Log(logger, "second");
            Log(logger, "third");
            Assert.That(logger.Entries, Has.Count.EqualTo(3), "0 keeps every entry.");

            logger.MaxEntries = 1;

            Assert.That(logger.Entries, Has.Count.EqualTo(1));
            Assert.That(_text.text, Does.Contain("third"));
            Assert.That(_text.text, Does.Not.Contain("second"));
        }

        [Test]
        public void ErrorLogger_InspectorLowersMaxEntries_OnValidateTrimsAtOnce()
        {
            ErrorLogger logger = CreateLogger(0, true);
            Log(logger, "first");
            Log(logger, "second");
            Log(logger, "third");

            SetField(logger, "_maxEntries", 2);
            Invoke(logger, "OnValidate");

            Assert.That(logger.Entries, Has.Count.EqualTo(2));
            Assert.That(_text.text, Does.Not.Contain("first"));
            Assert.That(_text.text, Does.Contain("third"));

            SetField(logger, "_maxEntries", -5);
            Invoke(logger, "OnValidate");
            Assert.That(logger.MaxEntries, Is.Zero, "Negative limits clamp to unlimited.");
        }

        [Test]
        public void ErrorLogger_Clear_ResetsHistoryDuplicateFilterAndLabel()
        {
            ErrorLogger logger = CreateLogger(10, true);
            logger.checkExistingErrors = true;
            Log(logger, "first");

            logger.Clear();

            Assert.That(logger.Entries, Is.Empty);
            Assert.That(_text.text, Is.Empty);
            Log(logger, "first");
            Assert.That(logger.Entries, Has.Count.EqualTo(1));
        }

        [Test]
        public void ErrorLogger_MaxCharacters_DropsTheOldestUntilTheHistoryFits()
        {
            ErrorLogger logger = CreateLogger(0, true);
            Log(logger, "first");
            int entryLength = logger.Entries[0].Length;
            Log(logger, "second");
            Log(logger, "third");
            Assert.That(logger.HistoryCharacterCount, Is.GreaterThan(2 * entryLength + 2),
                "positive control: three entries are over a two-entry budget");

            logger.MaxCharacters = 2 * entryLength + 2;

            Assert.That(logger.Entries, Has.Count.EqualTo(2));
            Assert.That(logger.HistoryCharacterCount, Is.LessThanOrEqualTo(logger.MaxCharacters));
            Assert.That(_text.text, Does.Not.Contain("first"));
            Assert.That(_text.text, Does.Contain("third"));

            Log(logger, "fourth");
            Assert.That(logger.Entries, Has.Count.EqualTo(2));
            Assert.That(_text.text, Does.Not.Contain("second"));
            Assert.That(_text.text, Does.Contain("fourth"));
        }

        [Test]
        public void ErrorLogger_MaxCharacters_KeepsTheNewestEntryEvenOverTheBudget()
        {
            ErrorLogger logger = CreateLogger(0, true);
            logger.MaxCharacters = 8;

            Log(logger, "first");
            Log(logger, "a much longer second message");

            Assert.That(logger.Entries, Has.Count.EqualTo(1));
            Assert.That(logger.Entries[0], Does.Contain("second"));
            Assert.That(logger.HistoryCharacterCount, Is.EqualTo(logger.Entries[0].Length));

            logger.Clear();
            Assert.That(logger.HistoryCharacterCount, Is.Zero);
            logger.MaxCharacters = -3;
            Assert.That(logger.MaxCharacters, Is.Zero, "Negative budgets clamp to unlimited.");
        }

        [Test]
        public void Fps_StableReading_ReusesTheDisplayedString()
        {
            FPS fps = _owner.AddComponent<FPS>();
            SetField(fps, "text", _text);
            Invoke(fps, "RebuildFpsBuffer");
            SetField(fps, "samplesAccumulated", 1);
            SetField(fps, "accumulatedFps", 59.8f);

            Invoke(fps, "UpdateFpsDisplay");
            string first = GetField<string>(fps, "_displayText");
            SetField(fps, "accumulatedFps", 60.3f);
            Invoke(fps, "UpdateFpsDisplay");

            // WHY: identity is checked on the producer; TMP compares by value and keeps its first reference anyway.
            Assert.That(first, Is.EqualTo("60 FPS"));
            Assert.That(GetField<string>(fps, "_displayText"), Is.SameAs(first),
                "Same rounded value must not format a new string.");
            Assert.That(_text.text, Is.EqualTo("60 FPS"));

            SetField(fps, "accumulatedFps", 61.2f);
            Invoke(fps, "UpdateFpsDisplay");
            Assert.That(GetField<string>(fps, "_displayText"), Is.EqualTo("61 FPS"));

            SetField(fps, "showDecimals", true);
            Invoke(fps, "UpdateFpsDisplay");
            Assert.That(_text.text, Is.EqualTo((612 / 10.0).ToString("F1") + " FPS"));
        }

        [Test]
        public void Fps_StableReading_WarmRefresh_DoesNotAllocatePerRefresh()
        {
            FPS fps = _owner.AddComponent<FPS>();
            SetField(fps, "text", _text);
            Invoke(fps, "RebuildFpsBuffer");
            SetField(fps, "samplesAccumulated", 1);
            SetField(fps, "accumulatedFps", 59.8f);
            MethodInfo refreshMethod = typeof(FPS).GetMethod("UpdateFpsDisplay", PrivateInstance);
            Assert.That(refreshMethod, Is.Not.Null, "UpdateFpsDisplay");
            Action refresh = (Action)Delegate.CreateDelegate(typeof(Action), fps, refreshMethod);
            refresh();
            Assert.That(_text.text, Is.EqualTo("60 FPS"));

            long calls;
            using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
            {
                counter.Begin();
                for (int i = 0; i < OverlayRefreshes; i++)
                {
                    refresh();
                }

                calls = counter.End();
            }

            // WHY: formatting the number and appending " FPS" costs at least two GC.Alloc calls per refresh.
            Assert.That(calls, Is.LessThan(OverlayRefreshes / 4),
                "An unchanged rounded FPS value must reuse its string instead of formatting a new one.");
            Assert.That(_text.text, Is.EqualTo("60 FPS"));
        }

        [Test]
        public void Overlay_ManagerReadout_ReadsTheClipNameOnlyWhenTheClipChanges_AndDoesNotAllocateWarm()
        {
            Assume.That(AM.HasInstance, Is.False, "This regression requires an isolated audio singleton.");
            Assume.That(Object.FindObjectsByType<AM>(FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty,
                "This regression requires an isolated scene without an audio manager.");
            bool originalCreateInstance = AM.CreateInstance;
            GameObject managerObject = new("QualityOverlayAudioManager");
            managerObject.SetActive(false);
            AudioClip first = AudioClip.Create("OverlayFirstClip", 64, 1, 8000, false);
            AudioClip second = AudioClip.Create("OverlaySecondClip", 64, 1, 8000, false);

            try
            {
                AM manager = managerObject.AddComponent<AM>();
                AudioSource music = managerObject.AddComponent<AudioSource>();
                music.clip = first;
                SetField(manager, "_music", music);
                AM.CreateInstance = true;
                Assert.That(AM.I, Is.SameAs(manager));

                NeoDebugOverlay overlay = _owner.AddComponent<NeoDebugOverlay>();
                MethodInfo appendMethod = typeof(NeoDebugOverlay).GetMethod("AppendManagerInfo", PrivateInstance);
                Assert.That(appendMethod, Is.Not.Null, "AppendManagerInfo");
                Action<StringBuilder> append =
                    (Action<StringBuilder>)Delegate.CreateDelegate(typeof(Action<StringBuilder>), overlay,
                        appendMethod);
                StringBuilder builder = new(1024);

                append(builder);
                Assert.That(builder.ToString(), Does.Contain("clip=OverlayFirstClip"));
                string cachedName = GetField<string>(overlay, "_cachedClipName");
                builder.Length = 0;
                append(builder);
                Assert.That(GetField<string>(overlay, "_cachedClipName"), Is.SameAs(cachedName),
                    "An unchanged clip must not read Object.name (a new native string) again.");

                music.clip = second;
                builder.Length = 0;
                append(builder);
                Assert.That(builder.ToString(), Does.Contain("clip=OverlaySecondClip"));

                long calls;
                using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
                {
                    counter.Begin();
                    for (int i = 0; i < OverlayRefreshes; i++)
                    {
                        builder.Length = 0;
                        append(builder);
                    }

                    calls = counter.End();
                }

                Assert.That(calls, Is.LessThan(OverlayRefreshes / 4),
                    "A warm manager readout must not allocate per refresh (reflection invoke, boxing or clip name).");
            }
            finally
            {
                AM.CreateInstance = originalCreateInstance;
                Object.DestroyImmediate(managerObject);
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }

        private ErrorLogger CreateLogger(int maxEntries, bool addText)
        {
            ErrorLogger logger = _owner.AddComponent<ErrorLogger>();
            logger.textMesh = _text;
            logger.logTypeFilter = LogTypeFilter.Warning;
            logger.addText = addText;
            logger.MaxEntries = maxEntries;
            return logger;
        }

        private static void Log(ErrorLogger logger, string message)
        {
            MethodInfo callback = typeof(ErrorLogger).GetMethod("HandleLog", PrivateInstance);
            Assert.That(callback, Is.Not.Null);
            callback.Invoke(logger, new object[] { message, "(at Assets/Quality.cs:1)", LogType.Warning });
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, PrivateInstance);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private static T GetField<T>(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, PrivateInstance);
            Assert.That(field, Is.Not.Null, name);
            return (T)field.GetValue(target);
        }

        private static void Invoke(object target, string name)
        {
            MethodInfo method = target.GetType().GetMethod(name, PrivateInstance);
            Assert.That(method, Is.Not.Null, name);
            method.Invoke(target, null);
        }
    }
}
