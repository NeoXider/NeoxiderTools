using System;
using System.Collections.Generic;
using System.Linq;
using Neo;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    public class NeoDiagnosticsTests
    {
        // WHY: a swapped-in log handler keeps the deliberate test messages out of the editor
        // console - real error entries there read as failures to a human, even when expected.
        private sealed class CapturingLogHandler : ILogHandler
        {
            public readonly List<(LogType Type, string Message)> Entries = new();

            public void LogFormat(LogType logType, Object context, string format, params object[] args)
            {
                Entries.Add((logType, string.Format(format, args)));
            }

            public void LogException(Exception exception, Object context)
            {
                Entries.Add((LogType.Exception, exception.Message));
            }
        }

        private CapturingLogHandler _capture;
        private ILogHandler _previousHandler;

        [SetUp]
        public void SetUp()
        {
            NeoDiagnostics.ResetStaticState();
            _capture = new CapturingLogHandler();
            _previousHandler = Debug.unityLogger.logHandler;
            Debug.unityLogger.logHandler = _capture;
        }

        [TearDown]
        public void TearDown()
        {
            Debug.unityLogger.logHandler = _previousHandler;
            NeoDiagnostics.ResetStaticState();
        }

        [Test]
        public void ResetStaticState_DisablesInfoAndWarnings_ButKeepsErrors()
        {
            Assert.False(NeoDiagnostics.RuntimeLogsEnabled);
            Assert.False(NeoDiagnostics.RuntimeWarningsEnabled);
            Assert.True(NeoDiagnostics.RuntimeErrorsEnabled);

            NeoDiagnostics.Log("hidden info");
            NeoDiagnostics.LogWarning("hidden warning");
            NeoDiagnostics.LogError("visible error");

            Assert.That(_capture.Entries, Has.Count.EqualTo(1));
            Assert.That(_capture.Entries[0].Type, Is.EqualTo(LogType.Error));
            Assert.That(_capture.Entries[0].Message, Is.EqualTo("visible error"));
        }

        [Test]
        public void Configure_EnablesRequestedChannels()
        {
            NeoDiagnostics.Configure(true, true, true);

            NeoDiagnostics.Log("visible info");
            NeoDiagnostics.LogWarning("visible warning");
            NeoDiagnostics.LogError("visible error");

            Assert.That(_capture.Entries.Select(e => e.Type),
                Is.EqualTo(new[] { LogType.Log, LogType.Warning, LogType.Error }));
            Assert.That(_capture.Entries.Select(e => e.Message),
                Is.EqualTo(new[] { "visible info", "visible warning", "visible error" }));
        }

        [Test]
        public void ThrottledWarning_EmitsOnceForSameKey()
        {
            NeoDiagnostics.Configure(warnings: true);

            NeoDiagnostics.LogWarningThrottled("same-key", "first warning", seconds: 60f);
            NeoDiagnostics.LogWarningThrottled("same-key", "second warning", seconds: 60f);

            Assert.That(_capture.Entries, Has.Count.EqualTo(1));
            Assert.That(_capture.Entries[0].Message, Is.EqualTo("first warning"));
        }

        [Test]
        public void StableId_IsStablePerObjectAndDistinctBetweenObjects()
        {
            GameObject first = new GameObject("first");
            GameObject second = new GameObject("second");
            try
            {
                Assert.That(NeoDiagnostics.StableId(first), Is.EqualTo(NeoDiagnostics.StableId(first)),
                    "A throttle key that changes between calls never throttles anything.");
                Assert.That(NeoDiagnostics.StableId(first), Is.Not.EqualTo(NeoDiagnostics.StableId(second)),
                    "Two objects sharing a key would silence each other's first warning.");
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void StableId_HandlesNullWithoutThrowing()
        {
            // WHY: diagnostics must not throw on a null reference; they are called exactly where something was not found.
            Assert.That(NeoDiagnostics.StableId(null), Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void ThrottledWarning_KeyedByStableId_SeparatesInstances()
        {
            NeoDiagnostics.Configure(warnings: true);

            GameObject first = new GameObject("first");
            GameObject second = new GameObject("second");
            try
            {
                NeoDiagnostics.LogWarningThrottled(
                    "MissingCamera." + NeoDiagnostics.StableId(first), "first warning", seconds: 60f);
                NeoDiagnostics.LogWarningThrottled(
                    "MissingCamera." + NeoDiagnostics.StableId(first), "repeat warning", seconds: 60f);
                NeoDiagnostics.LogWarningThrottled(
                    "MissingCamera." + NeoDiagnostics.StableId(second), "second warning", seconds: 60f);

                Assert.That(_capture.Entries.Select(e => e.Message),
                    Is.EqualTo(new[] { "first warning", "second warning" }),
                    "Each object reports its own problem once - that is the whole point of the id in the key.");
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void Force_BypassesDisabledChannels()
        {
            NeoDiagnostics.Log("forced info", force: true);
            NeoDiagnostics.LogWarning("forced warning", force: true);
            NeoDiagnostics.LogError("forced error", force: true);

            Assert.That(_capture.Entries.Select(e => e.Message),
                Is.EqualTo(new[] { "forced info", "forced warning", "forced error" }));
        }

        [Test]
        public void ThrottleTable_WhenFullOfRunningWindows_KeepsEveryWindowAndThrottlesNewKeysTogether()
        {
            NeoDiagnostics.Configure(warnings: true);
            NeoDiagnostics.LogWarningThrottled("bounded-0", "first", seconds: 60f);
            NeoDiagnostics.LogWarningThrottled("bounded-0", "throttled", seconds: 60f);
            Assert.That(_capture.Entries, Has.Count.EqualTo(1));

            for (int i = 1; i < NeoDiagnostics.MaxThrottleKeys; i++)
            {
                NeoDiagnostics.LogWarningThrottled("bounded-" + i, "unique", seconds: 60f);
            }

            NeoDiagnostics.LogWarningThrottled("overflow-a", "first overflow", seconds: 60f);
            NeoDiagnostics.LogWarningThrottled("overflow-b", "second overflow", seconds: 60f);
            NeoDiagnostics.LogWarningThrottled("bounded-512", "middle key again", seconds: 60f);
            NeoDiagnostics.LogWarningThrottled("bounded-0", "oldest key again", seconds: 60f);

            Assert.That(_capture.Entries, Has.Count.EqualTo(NeoDiagnostics.MaxThrottleKeys + 1),
                "No running window is forgotten; the keys beyond the bound share one overflow window.");
            Assert.That(_capture.Entries[_capture.Entries.Count - 1].Message, Is.EqualTo("first overflow"));
        }

        [TestCase(1)]
        [TestCase(2 * NeoDiagnostics.MaxThrottleKeys)]
        public void RepeatingBurstWiderThanTheBound_StaysThrottledOnEveryPass(int keysBeyondTheBound)
        {
            NeoDiagnostics.Configure(warnings: true);
            int keyCount = NeoDiagnostics.MaxThrottleKeys + keysBeyondTheBound;
            string[] keys = new string[keyCount];
            for (int i = 0; i < keyCount; i++)
            {
                keys[i] = "burst-" + i;
            }

            for (int pass = 0; pass < 5; pass++)
            {
                for (int i = 0; i < keyCount; i++)
                {
                    NeoDiagnostics.LogWarningThrottled(keys[i], "burst", seconds: 60f);
                }
            }

            Assert.That(_capture.Entries, Has.Count.EqualTo(NeoDiagnostics.MaxThrottleKeys + 1),
                "Every tracked key logs once and the whole overflow logs once per window, however often it cycles.");
        }

        [Test]
        public void OverflowSuppressedCount_IsAppendedToTheNextWarningThatLogs()
        {
            NeoDiagnostics.Configure(warnings: true);
            for (int i = 0; i < NeoDiagnostics.MaxThrottleKeys; i++)
            {
                NeoDiagnostics.LogWarningThrottled("running-" + i, "running", seconds: 60f);
            }

            NeoDiagnostics.LogWarningThrottled("overflow-a", "first overflow", seconds: 60f);
            NeoDiagnostics.LogWarningThrottled("overflow-a", "repeat", seconds: 60f);
            NeoDiagnostics.LogWarningThrottled("overflow-b", "other key", seconds: 60f);
            NeoDiagnostics.LogWarningThrottled("unthrottled-a", "no window", seconds: 0f);
            NeoDiagnostics.LogWarningThrottled("unthrottled-b", "no window again", seconds: 0f);

            Assert.That(_capture.Entries, Has.Count.EqualTo(NeoDiagnostics.MaxThrottleKeys + 3));
            Assert.That(_capture.Entries[NeoDiagnostics.MaxThrottleKeys].Message, Is.EqualTo("first overflow"));
            Assert.That(_capture.Entries[NeoDiagnostics.MaxThrottleKeys + 1].Message,
                Does.StartWith("no window").And.Contain("2 more throttled warning(s) were suppressed"));
            Assert.That(_capture.Entries[NeoDiagnostics.MaxThrottleKeys + 2].Message, Is.EqualTo("no window again"),
                "The count is reported once.");
        }

        [Test]
        public void ThrottleTable_WhenFull_ReusesElapsedWindowSlotsAndKeepsRunningOnes()
        {
            NeoDiagnostics.Configure(warnings: true);
            NeoDiagnostics.LogWarningThrottled("running", "running first", seconds: 60f);
            for (int i = 1; i < NeoDiagnostics.MaxThrottleKeys; i++)
            {
                NeoDiagnostics.LogWarningThrottled("elapsed-" + i, "unthrottled", seconds: 0f);
            }

            NeoDiagnostics.LogWarningThrottled("fresh", "fresh key", seconds: 60f);
            NeoDiagnostics.LogWarningThrottled("fresh", "fresh again", seconds: 60f);
            NeoDiagnostics.LogWarningThrottled("running", "running again", seconds: 60f);

            Assert.That(_capture.Entries, Has.Count.EqualTo(NeoDiagnostics.MaxThrottleKeys + 1),
                "The fresh key got an elapsed slot and its own window.");
            Assert.That(_capture.Entries[_capture.Entries.Count - 1].Message, Is.EqualTo("fresh key"));
            Assert.That(_capture.Entries.Select(e => e.Message), Has.No.Member("running again"),
                "The oldest key keeps its running window while elapsed ones are forgotten.");
        }

        [Test]
        public void SuppressedOverflowAndThrottledCalls_DoNotAllocate()
        {
            NeoDiagnostics.Configure(warnings: true);
            for (int i = 0; i < NeoDiagnostics.MaxThrottleKeys; i++)
            {
                NeoDiagnostics.LogWarningThrottled("running-" + i, "running", seconds: 60f);
            }

            NeoDiagnostics.LogWarningThrottled("overflow-a", "first overflow", seconds: 60f);
            Action suppressed = () =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    NeoDiagnostics.LogWarningThrottled("overflow-b", "suppressed", seconds: 60f);
                    NeoDiagnostics.LogWarningThrottled("running-7", "throttled", seconds: 60f);
                }
            };

            // WHY: the first pass JIT-compiles the delegate and the overflow branch; the claim is about warm calls.
            suppressed();
            long allocations = GcAlloc.TryCount(suppressed, out string notAdmitted);

            Assert.That(_capture.Entries, Has.Count.EqualTo(NeoDiagnostics.MaxThrottleKeys + 1));
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(allocations, Is.Zero,
                "A throttled or suppressed warning only does a lookup and a time comparison.");
        }
    }
}
